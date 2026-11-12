using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;

public sealed class Game : Form
{
    private enum EnemyKind { Grunt, Archer, Mage, Elite, Guardian }
    private sealed class Enemy
    {
        public PointF Pos, Target;
        public EnemyKind Kind;
        public float Hp, MaxHp, Cooldown, Windup, WindupMax, Flash;
        public int BossPattern;
        public bool Armored;
        public bool Boss => Kind == EnemyKind.Guardian;
        public float Radius => Boss ? 23 : Kind == EnemyKind.Elite ? 18 : 14;
    }
    private sealed class Drop { public PointF Pos; public string Kind = "gold"; public int Amount; }
    private sealed class SaveData
    {
        public int Room { get; set; }
        public int Level { get; set; } = 1;
        public int Xp { get; set; }
        public int Gold { get; set; }
        public int Potions { get; set; } = 2;
        public int Damage { get; set; } = 25;
        public float Health { get; set; } = 100;
        public float MaxHealth { get; set; } = 100;
        public int Weapon { get; set; }
        public int OwnedWeapons { get; set; } = 1;
        public bool Armor { get; set; }
        public bool Relic { get; set; }
        public int HealthTalent { get; set; }
        public int SkillTalent { get; set; }
        public int DodgeTalent { get; set; }
        public bool RoomCleared { get; set; }
        public bool Completed { get; set; }
    }

    private static readonly RectangleF[] Walls = { new(345, 235, 48, 90), new(565, 375, 58, 80) };
    private readonly List<Enemy> enemies = new();
    private readonly List<Drop> drops = new();
    private readonly HashSet<Keys> down = new();
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 16 };
    private readonly Random random = new();
    private readonly string[] roomNames = { "VESTÍBULO EM RUÍNAS", "SALÃO DOS OSSOS", "CÂMARA DO GUARDIÃO" };
    private readonly string[] weaponNames = { "ESPADA COMUM", "LÂMINA RARA", "LÂMINA DO CREPÚSCULO" };
    private readonly string[] prologue = {
        "Na noite sem sinos, Véspera começou a esquecer os nomes de seus mortos. Sob as ruas, o Rio Cego repetiu uma voz que ninguém mais conseguia ouvir.",
        "Você é um Escutador: ouve as lembranças que a pedra não conseguiu guardar em silêncio. Entre elas reconheceu Nara, sua irmã, desaparecida quando os sinos pararam.",
        "A memória dela chega partida: “Desça. Ele ainda lembra.” O Guardião sem Nome espera na cripta, diante da única pista que pode explicar por que Nara foi até lá."
    };
    private readonly int[] weaponDamage = { 0, 10, 4 };
    private PointF player = new(150, 330), facing = new(1, 0), mouseAim = new(1, 0);
    private int room, level = 1, xp, gold, potions = 2, damage = 25, weapon, ownedWeapons = 1;
    private int healthTalent, skillTalent, dodgeTalent, pendingTalents, enemiesDefeated;
    private float health = 100, maxHealth = 100, attackCd, skillCd, dashCd, dashTime, invulnerable, noticeTime, windupPulse;
    private bool armor, armorOn, relic, relicOn, inventoryOpen, title = true, dead, victory, talentChoice, chestChoice, storyOverlay, memoryOverlay;
    private int storyPage;
    private string notice = "";
    private string SavePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Scadryk", "save.json");

    public Game()
    {
        Text = "Scadryk — RPG de ação 2D"; ClientSize = new Size(960, 640); MinimumSize = MaximumSize = new Size(976, 679);
        FormBorderStyle = FormBorderStyle.FixedSingle; MaximizeBox = false; StartPosition = FormStartPosition.CenterScreen;
        DoubleBuffered = true; KeyPreview = true; BackColor = Color.FromArgb(16, 19, 23);
        timer.Tick += (_, _) => TickGame(); timer.Start();
        KeyDown += OnKeyDown; KeyUp += (_, e) => down.Remove(e.KeyCode);
        MouseMove += (_, e) => { mouseAim = Normalize(new PointF(e.X - player.X, e.Y - player.Y), facing); };
        MouseDown += (_, e) =>
        {
            if (storyOverlay) { if (e.Button == MouseButtons.Left) AdvanceStory(); return; }
            if (memoryOverlay) { if (e.Button == MouseButtons.Left) memoryOverlay = false; return; }
            if (title || dead || victory || chestChoice || talentChoice || inventoryOpen) return;
            mouseAim = Normalize(new PointF(e.X - player.X, e.Y - player.Y), facing);
            if (e.Button == MouseButtons.Left) Attack();
            if (e.Button == MouseButtons.Right) Whirlwind();
        };
    }

    private void StartGame()
    {
        room = 0; level = 1; xp = gold = damage = 0; damage = 25; potions = 2; weapon = 0; ownedWeapons = 1;
        armor = armorOn = relic = relicOn = inventoryOpen = dead = victory = chestChoice = talentChoice = memoryOverlay = false;
        healthTalent = skillTalent = dodgeTalent = pendingTalents = enemiesDefeated = 0;
        health = maxHealth = 100; attackCd = skillCd = dashCd = dashTime = invulnerable = 0; title = false; storyPage = 0; storyOverlay = true; SetupRoom();
    }

    private void AdvanceStory()
    {
        if (!storyOverlay) return;
        storyPage++;
        if (storyPage >= prologue.Length) storyOverlay = false;
    }

    private void SetupRoom()
    {
        player = new PointF(150, 330); enemies.Clear(); drops.Clear(); lockedTarget = null;
        int count = room == 0 ? 3 : room == 1 ? 5 : 4;
        for (int i = 0; i < count; i++)
        {
            EnemyKind kind = room == 2 && i == 0 ? EnemyKind.Guardian : room == 1 && i == count - 1 ? EnemyKind.Elite : room > 0 && i == 1 ? EnemyKind.Archer : room == 2 && i == 2 ? EnemyKind.Mage : EnemyKind.Grunt;
            float hp = kind switch { EnemyKind.Guardian => 160, EnemyKind.Elite => 120, EnemyKind.Archer or EnemyKind.Mage => 48, _ => 58 + room * 8 };
            var enemy = new Enemy { Pos = new PointF(485 + i % 3 * 120, 215 + i / 3 * 165), Kind = kind, Hp = hp, MaxHp = hp, Cooldown = 0.7f + (float)random.NextDouble(), Armored = kind == EnemyKind.Elite && random.Next(2) == 0 };
            if (Blocked(enemy.Pos, enemy.Radius)) enemy.Pos = new PointF(660 + i * 12, 480 - i * 8);
            enemies.Add(enemy);
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        down.Add(e.KeyCode);
        if (e.KeyCode == Keys.Enter && (title || dead || victory)) { StartGame(); return; }
        if (e.KeyCode == Keys.C && title) { LoadGame(); return; }
        if (e.KeyCode == Keys.F5 && victory) { SaveGame(); return; }
        if (title || dead || victory) return;
        if (storyOverlay) { if (e.KeyCode is Keys.Enter or Keys.Space) AdvanceStory(); return; }
        if (memoryOverlay) { if (e.KeyCode is Keys.Enter or Keys.Space) memoryOverlay = false; return; }
        if (chestChoice) { if (e.KeyCode == Keys.D1) gold += 75; else if (e.KeyCode == Keys.D2) potions += 2; else if (e.KeyCode == Keys.D3) health = maxHealth; else return; chestChoice = false; victory = true; return; }
        if (talentChoice) { SelectTalent(e.KeyCode); return; }
        if (e.KeyCode == Keys.I) { inventoryOpen = !inventoryOpen; return; }
        if (inventoryOpen) { if (e.KeyCode >= Keys.D1 && e.KeyCode <= Keys.D3) EquipWeapon((int)e.KeyCode - (int)Keys.D1); else if (e.KeyCode == Keys.D4 && armor) armorOn = !armorOn; else if (e.KeyCode == Keys.D5 && relic) relicOn = !relicOn; return; }
        if (e.KeyCode == Keys.Space || e.KeyCode == Keys.J) Attack();
        else if ((e.KeyCode == Keys.ShiftKey || e.KeyCode == Keys.K) && dashCd <= 0) { dashTime = 0.17f; dashCd = Math.Max(0.35f, 1.15f - dodgeTalent * 0.12f); }
        else if (e.KeyCode == Keys.R) Whirlwind();
        else if (e.KeyCode == Keys.Q) UsePotion();
        else if (e.KeyCode == Keys.E) Interact();
        else if (e.KeyCode == Keys.F5) { if (enemies.Count == 0 && drops.Count == 0) SaveGame(); else { notice = "LIMPE A SALA E RECOLHA O LOOT ANTES DE SALVAR"; noticeTime = 2; } }
        else if (e.KeyCode == Keys.Tab) CycleTarget();
    }

    private void SelectTalent(Keys key)
    {
        if (key == Keys.D1) { healthTalent++; maxHealth += 20; health = Math.Min(maxHealth, health + 20); }
        else if (key == Keys.D2) skillTalent++;
        else if (key == Keys.D3) dodgeTalent++;
        else return;
        pendingTalents--; talentChoice = pendingTalents > 0;
    }

    private void Attack()
    {
        if (attackCd > 0) return;
        facing = lockedTarget is not null && enemies.Contains(lockedTarget) ? Normalize(Sub(lockedTarget.Pos, player), facing) : mouseAim;
        attackCd = weapon == 2 ? 0.23f : 0.33f;
        foreach (Enemy enemy in enemies.ToArray())
        {
            PointF offset = Sub(enemy.Pos, player); float distance = Length(offset);
            if (distance > (enemy.Boss ? 95 : 78) || (distance >= 20 && Dot(facing, Normalize(offset, facing)) <= 0.1f) || BlockedLine(player, enemy.Pos)) continue;
            bool critical = random.NextDouble() < (weapon == 2 ? 0.25 : 0.12); int dealt = damage * (critical ? 2 : 1); if (enemy.Armored) dealt = (int)(dealt * 0.75f);
            enemy.Hp -= dealt; enemy.Flash = 0.2f; if (enemy.Hp <= 0) Defeat(enemy);
        }
    }

    private void Whirlwind()
    {
        if (skillCd > 0 || talentChoice) return;
        skillCd = Math.Max(1.5f, 6f - skillTalent * 0.5f);
        foreach (Enemy enemy in enemies.ToArray()) if (Distance(player, enemy.Pos) < 110) { enemy.Hp -= (int)(damage * 1.6f * (enemy.Armored ? 0.75f : 1)); if (enemy.Hp <= 0) Defeat(enemy); }
    }

    private void Defeat(Enemy enemy)
    {
        if (!enemies.Remove(enemy)) return;
        enemiesDefeated++; drops.Add(new Drop { Pos = enemy.Pos, Kind = "gold", Amount = enemy.Boss ? 35 : enemy.Kind == EnemyKind.Elite ? 20 : 8 });
        AddXp(enemy.Boss ? 75 : enemy.Kind == EnemyKind.Elite ? 40 : 22);
        if (enemy.Boss && !relic) drops.Add(new Drop { Pos = Offset(enemy.Pos, -24, 0), Kind = "relic" });
        if (enemy.Kind == EnemyKind.Elite && !armor) drops.Add(new Drop { Pos = Offset(enemy.Pos, 24, 0), Kind = "armor" });
        else if (!enemy.Boss && random.NextDouble() < 0.2) drops.Add(new Drop { Pos = Offset(enemy.Pos, 16, 12), Kind = "potion" });
        if (relicOn) health = Math.Min(maxHealth, health + 5);
        if (enemies.Count == 0 && room < 2) { int next = room == 0 ? 2 : 1; if ((ownedWeapons & (1 << next)) == 0) drops.Add(new Drop { Pos = Offset(enemy.Pos, -16, -8), Kind = "weapon", Amount = next }); }
        if (enemies.Count == 0 && drops.Count == 0 && room == 2 && !memoryOverlay) chestChoice = true;
    }

    private void AddXp(int amount)
    {
        xp += amount;
        while (xp >= level * 60) { xp -= level * 60; level++; damage += 5; pendingTalents++; }
        if (pendingTalents > 0) talentChoice = true;
    }

    private void TickGame()
    {
        float dt = timer.Interval / 1000f; noticeTime = Math.Max(0, noticeTime - dt); attackCd = Math.Max(0, attackCd - dt); skillCd = Math.Max(0, skillCd - dt); dashCd = Math.Max(0, dashCd - dt); dashTime = Math.Max(0, dashTime - dt); invulnerable = Math.Max(0, invulnerable - dt); windupPulse += dt;
        if (title || dead || victory || storyOverlay || memoryOverlay || inventoryOpen || talentChoice || chestChoice) { Invalidate(); return; }
        PointF dir = new((down.Contains(Keys.D) || down.Contains(Keys.Right) ? 1 : 0) - (down.Contains(Keys.A) || down.Contains(Keys.Left) ? 1 : 0), (down.Contains(Keys.S) || down.Contains(Keys.Down) ? 1 : 0) - (down.Contains(Keys.W) || down.Contains(Keys.Up) ? 1 : 0));
        dir = Normalize(dir, PointF.Empty); if (dashTime > 0) dir = facing;
        MovePlayer(Scale(dir, (dashTime > 0 ? 530 : 190) * dt));
        for (int i = drops.Count - 1; i >= 0; i--) if (Distance(player, drops[i].Pos) < 30) Collect(drops[i]);
        foreach (Enemy enemy in enemies.ToArray()) { UpdateEnemy(enemy, dt); if (dead) break; }
        if (enemies.Count == 0 && drops.Count == 0 && room == 2 && !memoryOverlay) chestChoice = true;
        Invalidate();
    }

    private void UpdateEnemy(Enemy e, float dt)
    {
        e.Cooldown -= dt; e.Flash = Math.Max(0, e.Flash - dt); PointF offset = Sub(player, e.Pos); float dist = Length(offset);
        if (e.Windup > 0) {
            e.Windup -= dt;
            float reach = e.Boss ? e.BossPattern == 0 ? 90 : 42 : e.Kind == EnemyKind.Elite ? 62 : e.Kind == EnemyKind.Mage ? 52 : e.Kind == EnemyKind.Archer ? 230 : 38;
            bool targeted = e.Boss && e.BossPattern == 1 || e.Kind is EnemyKind.Mage or EnemyKind.Elite or EnemyKind.Archer;
            if (e.Windup <= 0 && (targeted ? Distance(player, e.Target) < reach : dist < reach) && invulnerable <= 0 && dashTime <= 0) HitPlayer(e.Boss ? 26 : e.Kind == EnemyKind.Elite ? 20 : e.Kind == EnemyKind.Mage ? 16 : 10);
            return;
        }
        if (e.Kind == EnemyKind.Archer) { if (dist < 170) e.Pos = MoveEnemy(e, Scale(Normalize(Sub(e.Pos, player), PointF.Empty), 55 * dt)); else if (e.Cooldown <= 0) { BeginWindup(e, player, 0.6f); e.Cooldown = 1.8f; } SeparateFromPack(e, dt); return; }
        if (e.Kind == EnemyKind.Mage && dist < 165) e.Pos = MoveEnemy(e, Scale(Normalize(Sub(e.Pos, player), PointF.Empty), 42 * dt));
        if (e.Kind == EnemyKind.Elite && e.Cooldown <= 0 && dist < 220) { BeginWindup(e, player, e.Armored ? 0.72f : 0.5f); e.Cooldown = e.Armored ? 2.5f : 2f; return; }
        if (e.Boss && e.Cooldown <= 0 && dist < 135) { e.BossPattern = 1 - e.BossPattern; BeginWindup(e, player, e.Hp < e.MaxHp * 0.5f ? 0.52f : 0.8f); e.Cooldown = e.Hp < e.MaxHp * 0.5f ? 1.3f : 2.1f; return; }
        if (e.Kind == EnemyKind.Mage && e.Cooldown <= 0) { BeginWindup(e, player, 0.8f); e.Cooldown = 2.2f; SeparateFromPack(e, dt); return; }
        if (dist > (e.Boss ? 40 : 30)) e.Pos = MoveEnemy(e, Scale(Normalize(offset, PointF.Empty), (e.Boss ? 55 : 74 + room * 7) * dt));
        else if (e.Cooldown <= 0) { BeginWindup(e, player, 0.45f); e.Cooldown = 1.35f; }
        if (!e.Boss) SeparateFromPack(e, dt);
    }

    private static void BeginWindup(Enemy enemy, PointF target, float duration) { enemy.Target = target; enemy.Windup = enemy.WindupMax = duration; }
    private void HitPlayer(float value) { health = Math.Max(0, health - value * (armorOn ? 0.7f : 1)); invulnerable = 0.6f; if (health <= 0) dead = true; }
    private void Collect(Drop item)
    {
        switch (item.Kind) { case "gold": gold += item.Amount; notice = $"+{item.Amount} OURO"; break; case "potion": potions++; notice = "+1 POÇÃO"; break; case "weapon": ownedWeapons |= 1 << item.Amount; notice = $"ARMA: {weaponNames[item.Amount]}"; break; case "armor": armor = true; notice = "PEITORAL DE PEDRA ENCONTRADO"; break; case "relic": relic = true; notice = "FRAGMENTO DE VOZ ENCONTRADO"; memoryOverlay = true; break; }
        drops.Remove(item); noticeTime = 1.7f;
    }

    private void UsePotion() { if (potions > 0 && health < maxHealth) { potions--; health = Math.Min(maxHealth, health + 45); notice = "+45 VIDA"; noticeTime = 1.3f; } else { notice = potions == 0 ? "SEM POÇÕES" : "VIDA JÁ ESTÁ CHEIA"; noticeTime = 1.3f; } }
    private void Interact() { if (enemies.Count != 0) { notice = "DERROTE OS INIMIGOS PRIMEIRO"; noticeTime = 1.2f; } else if (drops.Count != 0) { notice = "RECOLHA O LOOT ANTES DE AVANÇAR"; noticeTime = 1.5f; } else if (room < 2 && player.X > 790) { room++; SetupRoom(); } else if (room == 2 && Distance(player, new PointF(750, 330)) < 110) chestChoice = true; }
    private Enemy? lockedTarget;
    private void CycleTarget() { if (enemies.Count == 0) { lockedTarget = null; return; } int i = lockedTarget is null ? -1 : enemies.IndexOf(lockedTarget); lockedTarget = enemies[(i + 1) % enemies.Count]; notice = lockedTarget.Boss ? "ALVO: GUARDIÃO DA CRIPTA" : $"ALVO: {EnemyName(lockedTarget)}"; noticeTime = 1.4f; }
    private void EquipWeapon(int index) { if ((ownedWeapons & (1 << index)) == 0) { notice = "ARMA AINDA NÃO ENCONTRADA"; noticeTime = 1.4f; return; } damage += weaponDamage[index] - weaponDamage[weapon]; weapon = index; }
    private PointF MoveEnemy(Enemy e, PointF delta) { PointF p = e.Pos; PointF x = new(p.X + delta.X, p.Y), y = new(p.X, p.Y + delta.Y); if (!Blocked(x, e.Radius)) p.X = x.X; if (!Blocked(new PointF(p.X, y.Y), e.Radius)) p.Y = y.Y; return new PointF(Math.Clamp(p.X, 85, 875), Math.Clamp(p.Y, 128, 533)); }
    private void SeparateFromPack(Enemy enemy, float dt) { PointF push = PointF.Empty; foreach (Enemy other in enemies) { if (ReferenceEquals(enemy, other)) continue; PointF away = Sub(enemy.Pos, other.Pos); float d = Length(away); if (d > 0.01f && d < 40) push = Add(push, Scale(away, (40 - d) / (40 * d))); } if (Length(push) > 0.01f) enemy.Pos = MoveEnemy(enemy, Scale(Normalize(push, PointF.Empty), 52 * dt)); }
    private void MovePlayer(PointF delta) { PointF x = new(player.X + delta.X, player.Y), y = new(player.X, player.Y + delta.Y); if (!Blocked(x)) player.X = x.X; if (!Blocked(new PointF(player.X, y.Y))) player.Y = y.Y; player.X = Math.Clamp(player.X, 85, 875); player.Y = Math.Clamp(player.Y, 128, 533); }
    private static bool Blocked(PointF p, float radius = 12) { RectangleF body = new(p.X - radius, p.Y - radius, radius * 2, radius * 2); return Walls.Any(w => w.IntersectsWith(body)); }
    private static bool BlockedLine(PointF a, PointF b)
    {
        int steps = Math.Max(1, (int)(Distance(a, b) / 6));
        for (int i = 0; i <= steps; i++) { float t = i / (float)steps; PointF p = new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t); if (Walls.Any(w => w.Contains(p))) return true; }
        return false;
    }

    private void SaveGame() { Directory.CreateDirectory(Path.GetDirectoryName(SavePath)!); var data = new SaveData { Room = room, Level = level, Xp = xp, Gold = gold, Potions = potions, Damage = damage, Health = health, MaxHealth = maxHealth, Weapon = weapon, OwnedWeapons = ownedWeapons, Armor = armor, Relic = relic, HealthTalent = healthTalent, SkillTalent = skillTalent, DodgeTalent = dodgeTalent, RoomCleared = true, Completed = victory }; File.WriteAllText(SavePath, JsonSerializer.Serialize(data)); notice = "PROGRESSO SALVO"; noticeTime = 2; }
    private string LegacySavePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Godot", "app_userdata", "Scadryk RPG", "scadryk-save.cfg");
    private SaveData ReadLegacySave()
    {
        if (!File.Exists(LegacySavePath)) throw new FileNotFoundException();
        string section = ""; var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string raw in File.ReadLines(LegacySavePath)) { string line = raw.Trim(); if (line.StartsWith('[') && line.EndsWith(']')) { section = line[1..^1]; continue; } int split = line.IndexOf('='); if (split > 0) values[section + "." + line[..split].Trim()] = line[(split + 1)..].Trim().Trim('"'); }
        string Get(string key, string fallback = "") => values.GetValueOrDefault("player." + key, fallback);
        int Int(string key, int fallback = 0) => int.TryParse(Get(key), out int n) ? n : fallback;
        float Float(string key, float fallback) => float.TryParse(Get(key), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float n) ? n : fallback;
        bool Bool(string key, bool fallback = false) => bool.TryParse(Get(key), out bool b) ? b : fallback;
        int savedWeapon = Int("weapon"); int savedRoom = Int("room");
        return new SaveData { Room = savedRoom, Level = Int("level", 1), Xp = Int("xp"), Gold = Int("gold"), Potions = Int("potions", 2), Damage = Int("damage", 25), Health = Float("health", 100), MaxHealth = Float("max_health", 100), Weapon = savedWeapon, OwnedWeapons = Int("owned_weapons", savedWeapon == 1 ? 3 : savedRoom > 0 ? 5 : 1), Armor = Bool("has_armor"), Relic = Bool("has_relic"), HealthTalent = Int("health_talent"), SkillTalent = Int("skill_talent"), DodgeTalent = Int("dodge_talent"), RoomCleared = Bool("room_cleared", true), Completed = Bool("completed") };
    }
    private void LoadGame()
    {
        try
        {
            bool migrated = !File.Exists(SavePath); SaveData s = migrated ? ReadLegacySave() : JsonSerializer.Deserialize<SaveData>(File.ReadAllText(SavePath)) ?? throw new InvalidDataException();
            if (s.Room is < 0 or > 2 || s.Level is < 1 or > 10000 || s.Xp < 0 || s.Xp >= s.Level * 60 || s.Weapon is < 0 or > 2 || s.OwnedWeapons < 1 || s.OwnedWeapons > 7 || (s.OwnedWeapons & (1 << s.Weapon)) == 0 || s.MaxHealth < 1 || s.Health <= 0 || s.Health > s.MaxHealth || s.Potions < 0 || s.Gold < 0 || s.Damage < 1 || s.Completed && s.Room != 2) throw new InvalidDataException();
            StartGame(); room = s.Room; level = s.Level; xp = s.Xp; gold = s.Gold; potions = s.Potions; damage = s.Damage; health = s.Health; maxHealth = s.MaxHealth; weapon = s.Weapon; ownedWeapons = s.OwnedWeapons; armor = s.Armor; relic = s.Relic; healthTalent = s.HealthTalent; skillTalent = s.SkillTalent; dodgeTalent = s.DodgeTalent; SetupRoom(); if (s.RoomCleared) enemies.Clear(); victory = s.Completed; title = false; storyOverlay = false;
            if (migrated) SaveGame(); notice = migrated ? "SAVE ANTIGO MIGRADO" : "PROGRESSO CARREGADO"; noticeTime = 2;
        }
        catch { notice = "NENHUM SALVAMENTO VÁLIDO"; noticeTime = 2; }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; g.Clear(Color.FromArgb(16, 19, 23));
        using var goldBrush = new SolidBrush(Color.FromArgb(230, 193, 129)); using var textBrush = new SolidBrush(Color.FromArgb(222, 216, 201));
        using var panel = new SolidBrush(Color.FromArgb(28, 32, 35)); using var floor = new SolidBrush(Color.FromArgb(54, 60, 53)); using var tile = new Pen(Color.FromArgb(70, 78, 68));
        using var headerFont = new Font("Georgia", 22, FontStyle.Bold); using var hudFont = new Font("Segoe UI", 12, FontStyle.Bold); using var roomFont = new Font("Georgia", 13, FontStyle.Bold); using var helpFont = new Font("Segoe UI", 10);
        g.FillRectangle(panel, 0, 0, 960, 83); g.DrawString("SCADRYK", headerFont, goldBrush, 24, 18);
        g.DrawString($"VIDA {health:0}/{maxHealth:0}     NÍVEL {level}     XP {xp}/{level * 60}     OURO {gold}     POÇÕES {potions}", hudFont, textBrush, 255, 29);
        using var healthBack = new SolidBrush(Color.FromArgb(52, 56, 51)); using var healthBar = new SolidBrush(health / maxHealth < .25f ? Color.IndianRed : Color.FromArgb(112, 171, 123));
        g.FillRectangle(healthBack, 25, 58, 190, 11); g.FillRectangle(healthBar, 25, 58, 190 * health / maxHealth, 11);
        g.FillRectangle(floor, 65, 108, 830, 445); for (int y = 0; y < 7; y++) for (int x = 0; x < 12; x++) g.DrawRectangle(tile, 70 + x * 68, 112 + y * 62, 67, 61);
        using var wallBrush = new SolidBrush(Color.FromArgb(90, 79, 62)); using var wallEdge = new Pen(Color.FromArgb(154, 132, 91), 3); foreach (RectangleF wall in Walls) { g.FillRectangle(wallBrush, wall); g.DrawRectangle(wallEdge, wall.X, wall.Y, wall.Width, wall.Height); }
        using var exitPen = new Pen(enemies.Count == 0 && drops.Count == 0 ? Color.LightGreen : Color.FromArgb(45, 48, 43), 8); g.DrawRectangle(exitPen, 882, 293, 3, 75);
        foreach (Enemy enemy in enemies) DrawEnemy(g, enemy);
        foreach (Drop item in drops) DrawDrop(g, item);
        DrawPlayer(g);
        g.DrawString($"{room + 1} / 3   {roomNames[room]}", roomFont, goldBrush, 67, 572);
        g.DrawString("WASD mover   ESPAÇO/CLIQUE atacar   SHIFT esquivar   R redemoinho   Q poção   E interagir   I inventário", helpFont, textBrush, 67, 606);
        if (noticeTime > 0) DrawCentered(g, notice, 170, new Font("Segoe UI", 13, FontStyle.Bold), Color.FromArgb(230, 193, 129));
        if (title || dead || victory) DrawOverlay(g, title ? "SCADRYK" : victory ? "VITÓRIA" : "VOCÊ CAIU", title ? "ENTER PARA COMEÇAR    •    C CARREGAR SAVE" : "ENTER PARA RECOMEÇAR    •    F5 SALVAR");
        if (storyOverlay) DrawStoryOverlay(g);
        if (memoryOverlay) DrawMemoryOverlay(g);
        if (inventoryOpen) DrawInventory(g);
        if (talentChoice) DrawChoice(g, $"SUBIU DE NÍVEL — ESCOLHA ({pendingTalents} PENDENTES)", "1. VIGOR  +20 VIDA MÁXIMA", "2. DOMÍNIO  Redemoinho recarrega mais rápido", "3. AGILIDADE  esquiva recarrega mais rápido");
        if (chestChoice) DrawChoice(g, "BAÚ DA CRIPTA — ESCOLHA UMA RECOMPENSA", "1. 75 OURO", "2. DUAS POÇÕES", "3. RECUPERAR TODA A VIDA");
    }

    private void DrawEnemy(Graphics g, Enemy enemy)
    {
        float r = enemy.Radius; Color color = enemy.Boss ? Color.FromArgb(145, 87, 77) : enemy.Kind switch { EnemyKind.Elite => enemy.Armored ? Color.SlateGray : Color.IndianRed, EnemyKind.Archer => Color.FromArgb(128, 151, 106), EnemyKind.Mage => Color.MediumPurple, _ => Color.FromArgb(183, 181, 166) };
        if (enemy.Windup > 0) { float baseWarn = enemy.Kind == EnemyKind.Elite ? 62 : enemy.Kind == EnemyKind.Mage ? 52 : enemy.Boss && enemy.BossPattern == 0 ? 90 : enemy.Kind == EnemyKind.Archer ? 26 : 36; float progress = Math.Clamp(enemy.Windup / Math.Max(0.01f, enemy.WindupMax), 0, 1); float warn = baseWarn * (0.4f + progress * 0.6f); using var p = new Pen(Color.FromArgb(235, 115, 75), 3); using var b = new SolidBrush(Color.FromArgb(35, 235, 115, 75)); if (enemy.Kind == EnemyKind.Archer) g.DrawLine(p, enemy.Pos, enemy.Target); g.FillEllipse(b, enemy.Target.X - warn, enemy.Target.Y - warn, warn * 2, warn * 2); g.DrawEllipse(p, enemy.Target.X - warn, enemy.Target.Y - warn, warn * 2, warn * 2); }
        using var body = new SolidBrush(enemy.Flash > 0 ? Color.White : color); using var edge = new Pen(enemy == lockedTarget ? Color.Gold : enemy.Armored ? Color.LightSteelBlue : Color.FromArgb(35, 25, 22), enemy == lockedTarget ? 4 : 2);
        g.FillEllipse(body, enemy.Pos.X - r, enemy.Pos.Y - r, r * 2, r * 2); g.DrawEllipse(edge, enemy.Pos.X - r, enemy.Pos.Y - r, r * 2, r * 2);
        using var hpBack = new SolidBrush(Color.FromArgb(40, 20, 20)); using var hpBar = new SolidBrush(Color.FromArgb(188, 76, 70)); g.FillRectangle(hpBack, enemy.Pos.X - 22, enemy.Pos.Y - r - 10, 44, 5); g.FillRectangle(hpBar, enemy.Pos.X - 22, enemy.Pos.Y - r - 10, 44 * Math.Max(0, enemy.Hp / enemy.MaxHp), 5);
        string name = EnemyName(enemy); if (name.Length > 0) DrawCentered(g, name, enemy.Pos.Y - r - 28, new Font("Segoe UI", 8, FontStyle.Bold), Color.FromArgb(233, 198, 151), enemy.Pos.X);
        if (enemy.Boss) { using var bossBack = new SolidBrush(Color.FromArgb(40, 20, 20)); using var bossHp = new SolidBrush(enemy.Hp <= enemy.MaxHp * 0.5f ? Color.OrangeRed : Color.Firebrick); g.FillRectangle(bossBack, 360, 112, 240, 8); g.FillRectangle(bossHp, 360, 112, 240 * Math.Clamp(enemy.Hp / enemy.MaxHp, 0, 1), 8); DrawCentered(g, enemy.Hp <= enemy.MaxHp * 0.5f ? "GUARDIÃO • ENFURECIDO" : "GUARDIÃO DA CRIPTA", 91, new Font("Georgia", 11, FontStyle.Bold), enemy.Hp <= enemy.MaxHp * 0.5f ? Color.OrangeRed : Color.Goldenrod); }
    }
    private void DrawDrop(Graphics g, Drop item) { Color c = item.Kind switch { "relic" => Color.Violet, "armor" => Color.MediumAquamarine, "weapon" => Color.CornflowerBlue, "potion" => Color.IndianRed, _ => Color.Goldenrod }; using var b = new SolidBrush(c); using var p = new Pen(Color.FromArgb(230, 220, 190), 1); g.FillEllipse(b, item.Pos.X - 8, item.Pos.Y - 8, 16, 16); g.DrawEllipse(p, item.Pos.X - 8, item.Pos.Y - 8, 16, 16); }
    private void DrawPlayer(Graphics g) { using var b = new SolidBrush(invulnerable > 0 ? Color.Salmon : Color.FromArgb(90, 143, 138)); using var face = new SolidBrush(Color.Bisque); g.FillEllipse(b, player.X - 16, player.Y - 16, 32, 32); g.FillEllipse(face, player.X - 8, player.Y - 11, 16, 16); using var p = new Pen(Color.AntiqueWhite, 4); g.DrawLine(p, player, Add(player, Scale(facing, 24))); if (skillCd > 0) { using var cooldownPen = new Pen(Color.FromArgb(190, 230, 193, 129), 3); g.DrawArc(cooldownPen, player.X - 25, player.Y - 25, 50, 50, -90, 360 * skillCd / Math.Max(1.5f, 6f - skillTalent * .5f)); } }
    private void DrawInventory(Graphics g)
    {
        using var b = new SolidBrush(Color.FromArgb(245, 17, 19, 20)); using var itemFont = new Font("Segoe UI", 12); g.FillRectangle(b, 150, 145, 660, 355);
        DrawCentered(g, "INVENTÁRIO   •   1–3 ARMAS   •   4 ARMADURA   •   5 RELÍQUIA   •   I FECHAR", 180, new Font("Segoe UI", 12, FontStyle.Bold), Color.Goldenrod);
        for (int i = 0; i < 3; i++) g.DrawString($"{i + 1}. {weaponNames[i]}    DANO {weaponDamage[i] - weaponDamage[weapon]:+#;-#;0}    {(weapon == i ? "EQUIPADA" : (ownedWeapons & (1 << i)) != 0 ? "DISPONÍVEL" : "NÃO ENCONTRADA")}", itemFont, (ownedWeapons & (1 << i)) != 0 ? Brushes.AntiqueWhite : Brushes.Gray, 205, 230 + i * 40);
        g.DrawString($"4. PEITORAL DE PEDRA {(armorOn ? "EQUIPADO" : armor ? "DISPONÍVEL" : "NÃO ENCONTRADO")}", itemFont, Brushes.AntiqueWhite, 205, 360);
        g.DrawString($"5. RELICÁRIO VITAL {(relicOn ? "EQUIPADO" : relic ? "DISPONÍVEL" : "NÃO ENCONTRADO")}", itemFont, Brushes.AntiqueWhite, 205, 400);
    }
    private void DrawChoice(Graphics g, string titleText, string a, string b, string c)
    {
        using var bg = new SolidBrush(Color.FromArgb(248, 15, 17, 18)); using var optionFont = new Font("Segoe UI", 12); g.FillRectangle(bg, 170, 205, 620, 225);
        DrawCentered(g, titleText, 238, new Font("Georgia", 15, FontStyle.Bold), Color.Goldenrod);
        g.DrawString(a, optionFont, Brushes.AntiqueWhite, 220, 286); g.DrawString(b, optionFont, Brushes.AntiqueWhite, 220, 330); g.DrawString(c, optionFont, Brushes.AntiqueWhite, 220, 374);
    }
    private void DrawStoryOverlay(Graphics g)
    {
        using var shade = new SolidBrush(Color.FromArgb(145, 5, 7, 9));
        using var panelBrush = new SolidBrush(Color.FromArgb(246, 19, 21, 22));
        using var border = new Pen(Color.FromArgb(175, 143, 112, 73), 2);
        using var headingFont = new Font("Georgia", 13, FontStyle.Bold);
        using var bodyFont = new Font("Segoe UI", 14);
        using var hintFont = new Font("Segoe UI", 9, FontStyle.Bold);
        using var headingBrush = new SolidBrush(Color.FromArgb(230, 193, 129));
        using var bodyBrush = new SolidBrush(Color.AntiqueWhite);
        using var hintBrush = new SolidBrush(Color.FromArgb(170, 174, 168));
        g.FillRectangle(shade, 0, 0, 960, 640);
        g.FillRectangle(panelBrush, 86, 420, 788, 154);
        g.DrawRectangle(border, 86, 420, 788, 154);
        g.DrawString($"MEMÓRIA NA PEDRA   •   {storyPage + 1} / {prologue.Length}", headingFont, headingBrush, 112, 440);
        g.DrawString(prologue[storyPage], bodyFont, bodyBrush, new RectangleF(112, 472, 736, 64));
        g.DrawString("ENTER, ESPAÇO OU CLIQUE  •  CONTINUAR", hintFont, hintBrush, 112, 548);
    }
    private void DrawMemoryOverlay(Graphics g)
    {
        using var shade = new SolidBrush(Color.FromArgb(165, 5, 7, 9));
        using var panelBrush = new SolidBrush(Color.FromArgb(246, 19, 21, 22));
        using var border = new Pen(Color.FromArgb(175, 143, 112, 73), 2);
        using var headingFont = new Font("Georgia", 13, FontStyle.Bold);
        using var bodyFont = new Font("Segoe UI", 14, FontStyle.Italic);
        using var hintFont = new Font("Segoe UI", 9, FontStyle.Bold);
        using var headingBrush = new SolidBrush(Color.FromArgb(230, 193, 129));
        using var bodyBrush = new SolidBrush(Color.AntiqueWhite);
        using var hintBrush = new SolidBrush(Color.FromArgb(170, 174, 168));
        g.FillRectangle(shade, 0, 0, 960, 640);
        g.FillRectangle(panelBrush, 86, 420, 788, 154);
        g.DrawRectangle(border, 86, 420, 788, 154);
        g.DrawString("ECO PRESO NO RELICÁRIO", headingFont, headingBrush, 112, 440);
        g.DrawString("Nara: “Eu vim por vontade própria. Se encontrou esta lembrança, procure o arquivo de Sileth.”", bodyFont, bodyBrush, new RectangleF(112, 472, 736, 64));
        g.DrawString("ENTER, ESPAÇO OU CLIQUE  •  FECHAR", hintFont, hintBrush, 112, 548);
    }
    private void DrawOverlay(Graphics g, string titleText, string instruction) { using var bg = new SolidBrush(Color.FromArgb(225, 8, 10, 12)); g.FillRectangle(bg, 0, 0, 960, 640); DrawCentered(g, titleText, 250, new Font("Georgia", 36, FontStyle.Bold), Color.FromArgb(230, 193, 129)); DrawCentered(g, instruction, 340, new Font("Segoe UI", 15, FontStyle.Bold), Color.AntiqueWhite); if (noticeTime > 0) DrawCentered(g, notice, 410, new Font("Segoe UI", 12), Color.Goldenrod); }
    private static void DrawCentered(Graphics g, string s, float y, Font f, Color color, float x = 480) { using (f) using (var brush = new SolidBrush(color)) { SizeF size = g.MeasureString(s, f); g.DrawString(s, f, brush, x - size.Width / 2, y); } }
    private static string EnemyName(Enemy e) => e.Boss ? "GUARDIÃO" : e.Kind switch { EnemyKind.Elite => e.Armored ? "ELITE • BLINDADO" : "ELITE • FRENÉTICO", EnemyKind.Archer => "ARQUEIRO", EnemyKind.Mage => "CONJURADOR", _ => "" };
    private static PointF Normalize(PointF p, PointF fallback) { float n = Length(p); return n > 0.001f ? new PointF(p.X / n, p.Y / n) : fallback; }
    private static float Length(PointF p) => MathF.Sqrt(p.X * p.X + p.Y * p.Y);
    private static float Distance(PointF a, PointF b) => Length(Sub(a, b));
    private static PointF Sub(PointF a, PointF b) => new(a.X - b.X, a.Y - b.Y);
    private static PointF Offset(PointF a, float x, float y) => new(a.X + x, a.Y + y);
    private static PointF Add(PointF a, PointF b) => new(a.X + b.X, a.Y + b.Y);
    private static PointF Scale(PointF a, float m) => new(a.X * m, a.Y * m);
    private static float Dot(PointF a, PointF b) => a.X * b.X + a.Y * b.Y;

    public static int SelfTest()
    {
        using var game = new Game(); game.timer.Stop(); game.StartGame();
        if (!game.storyOverlay || game.storyPage != 0) throw new InvalidOperationException("O novo jogo não iniciou no prólogo da história.");
        for (int i = 0; i < game.prologue.Length; i++) game.AdvanceStory();
        if (game.storyOverlay) throw new InvalidOperationException("O prólogo não liberou o início da partida.");
        game.Collect(new Drop { Pos = game.player, Kind = "relic" });
        if (!game.relic || !game.memoryOverlay) throw new InvalidOperationException("O fragmento do relicário não revelou a lembrança de Nara.");
        game.memoryOverlay = false;
        if (game.enemies.Count != 3) throw new InvalidOperationException("A primeira sala deve iniciar com três inimigos.");
        game.player = new PointF(320, 230); game.MovePlayer(new PointF(40, 40)); PointF movedPlayer = game.player;
        if (Math.Abs(movedPlayer.X - 320) > 0.01f || Math.Abs(movedPlayer.Y - 270) > 0.01f) throw new InvalidOperationException("O movimento não deslizou pela quina do obstáculo.");
        Enemy grunt = game.enemies[0]; game.player = new PointF(grunt.Pos.X - 22, grunt.Pos.Y); grunt.Cooldown = 0; game.UpdateEnemy(grunt, 0.01f);
        if (grunt.Windup <= 0 || game.health < game.maxHealth) throw new InvalidOperationException("Ataque corpo a corpo comum não foi anunciado.");
        game.player = new PointF(grunt.Pos.X - 100, grunt.Pos.Y); game.UpdateEnemy(grunt, 0.6f);
        if (game.health < game.maxHealth) throw new InvalidOperationException("A esquiva antes do golpe não evitou o dano.");
        game.AddXp(60);
        if (game.level != 2 || !game.talentChoice || game.pendingTalents != 1) throw new InvalidOperationException("A progressão por nível não concedeu uma escolha de talento.");
        Console.WriteLine("TESTES OK: movimento, aviso de ataque, esquiva e progressão."); return 0;
    }
}



