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
    private enum EnemyKind { Grunt, Archer, Mage, Elite, Guardian, InkEcho, BlindScribe, DrownedMemory, RiverFerryman }
    private sealed class Enemy
    {
        public PointF Pos, Target;
        public EnemyKind Kind;
        public float Hp, MaxHp, Cooldown, Windup, WindupMax, Flash;
        public int BossPattern;
        public bool Armored;
        public bool Boss => Kind is EnemyKind.Guardian or EnemyKind.BlindScribe or EnemyKind.RiverFerryman;
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
        public int MemoriesFound { get; set; }
        public int QuestStage { get; set; }
        public bool ArchiveSeal { get; set; }
        public bool RiverBell { get; set; }
    }

    private static readonly RectangleF[] Walls = { new(345, 235, 48, 90), new(565, 375, 58, 80) };
    private static readonly RectangleF[] ArchiveWalls = { new(275, 155, 38, 155), new(437, 365, 48, 165), new(626, 150, 42, 165), new(765, 374, 42, 150) };
    private static readonly RectangleF[] VaultWalls = { new(280, 170, 58, 118), new(625, 170, 58, 118), new(280, 410, 58, 118), new(625, 410, 58, 118) };
    private static readonly RectangleF[] RiverWalls = { new(250, 175, 80, 48), new(410, 390, 96, 52), new(645, 170, 70, 48), new(760, 390, 75, 55) };
    private static readonly RectangleF[] BellChamberWalls = { new(285, 150, 46, 180), new(630, 345, 48, 180), new(423, 435, 112, 42) };
    private static readonly RectangleF[] TownBuildings = { new(115, 128, 208, 128), new(677, 128, 208, 128), new(115, 426, 208, 120), new(677, 426, 208, 120) };
    private static readonly PointF CuratorPosition = new(625, 315), BellKeeperPosition = new(352, 347), ArchiveBookPosition = new(785, 355), CryptGatePosition = new(92, 330);
    private static readonly PointF[] MemoryStonePositions = { new(260, 165), new(270, 485) };
    private static readonly string[] MemoryStoneTexts = {
        "Nara: “Se os sinos calarem, siga o leito do Rio Cego. A Curadora fechou os arquivos antes da noite começar.”",
        "Registro de Vigília: “O Guardião entregou seu nome ao Lume. A Curadora apagou o registro para que a cidade esquecesse o preço.”"
    };
    private readonly List<Enemy> enemies = new();
    private readonly List<Drop> drops = new();
    private readonly HashSet<Keys> down = new();
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 16 };
    private readonly Random random = new();
    private readonly string[] roomNames = { "VESTÍBULO EM RUÍNAS", "SALÃO DOS OSSOS", "CÂMARA DO GUARDIÃO", "VÉSPERA • PRAÇA DO FAROL", "ARQUIVO NORTE • GALERIA", "ARQUIVO NORTE • COFRE", "RIO CEGO • LEITO SECO", "RIO CEGO • CAPELA AFOGADA" };
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
    private int memoriesFound, questStage;
    private bool archiveSeal;
    private bool riverBell;
    private float health = 100, maxHealth = 100, attackCd, skillCd, dashCd, dashTime, invulnerable, noticeTime, windupPulse;
    private bool armor, armorOn, relic, relicOn, inventoryOpen, title = true, dead, victory, talentChoice, chestChoice, storyOverlay, memoryOverlay;
    private int storyPage;
    private string notice = "";
    private string memoryTitle = "";
    private string memoryText = "";
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
        armor = armorOn = relic = relicOn = archiveSeal = riverBell = inventoryOpen = dead = victory = chestChoice = talentChoice = memoryOverlay = false;
        healthTalent = skillTalent = dodgeTalent = pendingTalents = enemiesDefeated = memoriesFound = questStage = 0;
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
        int count = room == 3 ? 0 : room == 0 ? 3 : room == 1 ? 5 : room == 4 ? 5 : room == 5 ? 4 : room == 6 ? 6 : room == 7 ? 4 : 4;
        for (int i = 0; i < count; i++)
        {
            EnemyKind kind = room == 2 && i == 0 ? EnemyKind.Guardian : room == 5 && i == 0 ? EnemyKind.BlindScribe : room == 7 && i == 0 ? EnemyKind.RiverFerryman : room == 1 && i == count - 1 ? EnemyKind.Elite : room == 4 && i is 1 or 4 ? EnemyKind.InkEcho : room == 5 && i == 2 ? EnemyKind.InkEcho : room == 6 && i is 1 or 4 ? EnemyKind.DrownedMemory : room == 7 && i == 2 ? EnemyKind.DrownedMemory : room > 0 && i == 1 ? EnemyKind.Archer : room is 2 or 5 or 7 && i == 2 ? EnemyKind.Mage : EnemyKind.Grunt;
            float hp = kind switch { EnemyKind.Guardian => 160, EnemyKind.BlindScribe => 245, EnemyKind.RiverFerryman => 285, EnemyKind.Elite => 120, EnemyKind.Archer or EnemyKind.Mage or EnemyKind.InkEcho or EnemyKind.DrownedMemory => 48, _ => 58 + room * 8 };
            var enemy = new Enemy { Pos = new PointF(485 + i % 3 * 120, 215 + i / 3 * 165), Kind = kind, Hp = hp, MaxHp = hp, Cooldown = 0.7f + (float)random.NextDouble(), Armored = kind == EnemyKind.Elite && random.Next(2) == 0 };
            if (Blocked(enemy.Pos, enemy.Radius)) enemy.Pos = new PointF(660 + i * 12, 480 - i * 8);
            enemies.Add(enemy);
        }
    }

    private void EnterVesper()
    {
        room = 3; title = false; victory = false; SetupRoom();
        ShowMemory("VÉSPERA", "A praça do farol ainda vive, embora seus moradores evitem falar dos sinos. Sileth espera perto do arquivo; talvez reconheça a voz de Nara.");
        notice = questStage == 0 ? "NOVO OBJETIVO: FALE COM SILETH" : "OBJETIVO ATUALIZADO"; noticeTime = 3.5f;
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
        if (chestChoice) { if (e.KeyCode == Keys.D1) gold += 75; else if (e.KeyCode == Keys.D2) potions += 2; else if (e.KeyCode == Keys.D3) health = maxHealth; else return; chestChoice = false; EnterVesper(); return; }
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
        if (enemy.Kind == EnemyKind.Guardian && !relic) drops.Add(new Drop { Pos = Offset(enemy.Pos, -24, 0), Kind = "relic" });
        if (enemy.Kind == EnemyKind.BlindScribe && !archiveSeal) drops.Add(new Drop { Pos = Offset(enemy.Pos, -24, 0), Kind = "seal" });
        if (enemy.Kind == EnemyKind.RiverFerryman && !riverBell) drops.Add(new Drop { Pos = Offset(enemy.Pos, -24, 0), Kind = "bell" });
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
            float reach = e.Boss ? e.BossPattern == 0 ? 90 : e.Kind == EnemyKind.RiverFerryman && e.BossPattern == 2 ? 85 : 42 : e.Kind == EnemyKind.Elite ? 62 : e.Kind == EnemyKind.Mage ? 52 : e.Kind is EnemyKind.Archer or EnemyKind.InkEcho ? 230 : e.Kind == EnemyKind.DrownedMemory ? 195 : 38;
            bool targeted = (e.Boss && (e.BossPattern is 1 or 2)) || (e.Kind is EnemyKind.Mage or EnemyKind.Elite or EnemyKind.Archer or EnemyKind.InkEcho or EnemyKind.DrownedMemory);
            if (e.Windup <= 0 && (targeted ? Distance(player, e.Target) < reach : dist < reach) && invulnerable <= 0 && dashTime <= 0) HitPlayer(e.Boss ? e.Kind == EnemyKind.BlindScribe ? 23 : e.Kind == EnemyKind.RiverFerryman ? 28 : 26 : e.Kind == EnemyKind.Elite ? 20 : e.Kind == EnemyKind.Mage ? 16 : e.Kind is EnemyKind.InkEcho or EnemyKind.DrownedMemory ? 15 : 10);
            return;
        }
        if (e.Kind is EnemyKind.Archer or EnemyKind.InkEcho) { float preferred = e.Kind == EnemyKind.InkEcho ? 135 : 170; if (dist < preferred) e.Pos = MoveEnemy(e, Scale(Normalize(Sub(e.Pos, player), PointF.Empty), (e.Kind == EnemyKind.InkEcho ? 68 : 55) * dt)); else if (e.Cooldown <= 0) { BeginWindup(e, player, e.Kind == EnemyKind.InkEcho ? 0.75f : 0.6f); e.Cooldown = e.Kind == EnemyKind.InkEcho ? 2.0f : 1.8f; } SeparateFromPack(e, dt); return; }
        if (e.Kind == EnemyKind.DrownedMemory) { if (dist < 110) e.Pos = MoveEnemy(e, Scale(Normalize(Sub(e.Pos, player), PointF.Empty), 72 * dt)); else if (e.Cooldown <= 0) { BeginWindup(e, player, 0.9f); e.Cooldown = 2.25f; } SeparateFromPack(e, dt); return; }
        if (e.Kind == EnemyKind.Mage && dist < 165) e.Pos = MoveEnemy(e, Scale(Normalize(Sub(e.Pos, player), PointF.Empty), 42 * dt));
        if (e.Kind == EnemyKind.Elite && e.Cooldown <= 0 && dist < 220) { BeginWindup(e, player, e.Armored ? 0.72f : 0.5f); e.Cooldown = e.Armored ? 2.5f : 2f; return; }
        if (e.Boss && e.Cooldown <= 0 && (dist < 135 || (e.Kind is EnemyKind.BlindScribe or EnemyKind.RiverFerryman))) { e.BossPattern = e.Kind == EnemyKind.RiverFerryman ? (e.BossPattern + 1) % 3 : 1 - e.BossPattern; bool aimed = (e.Kind == EnemyKind.BlindScribe && e.BossPattern == 1) || (e.Kind == EnemyKind.RiverFerryman && (e.BossPattern is 1 or 2)); BeginWindup(e, aimed ? player : e.Pos, e.Hp < e.MaxHp * 0.5f ? 0.55f : 0.82f); e.Cooldown = e.Hp < e.MaxHp * 0.5f ? 1.35f : 2.2f; return; }
        if (e.Kind == EnemyKind.Mage && e.Cooldown <= 0) { BeginWindup(e, player, 0.8f); e.Cooldown = 2.2f; SeparateFromPack(e, dt); return; }
        if (dist > (e.Boss ? 40 : 30)) e.Pos = MoveEnemy(e, Scale(Normalize(offset, PointF.Empty), (e.Boss ? 55 : 74 + room * 7) * dt));
        else if (e.Cooldown <= 0) { BeginWindup(e, player, 0.45f); e.Cooldown = 1.35f; }
        if (!e.Boss) SeparateFromPack(e, dt);
    }

    private static void BeginWindup(Enemy enemy, PointF target, float duration) { enemy.Target = target; enemy.Windup = enemy.WindupMax = duration; }
    private void HitPlayer(float value) { health = Math.Max(0, health - value * (armorOn ? 0.7f : 1)); invulnerable = 0.6f; if (health <= 0) dead = true; }
    private void Collect(Drop item)
    {
        switch (item.Kind) { case "gold": gold += item.Amount; notice = $"+{item.Amount} OURO"; break; case "potion": potions++; notice = "+1 POÇÃO"; break; case "weapon": ownedWeapons |= 1 << item.Amount; notice = $"ARMA: {weaponNames[item.Amount]}"; break; case "armor": armor = true; notice = "PEITORAL DE PEDRA ENCONTRADO"; break; case "relic": relic = true; notice = "FRAGMENTO DE VOZ ENCONTRADO"; ShowMemory("ECO PRESO NO RELICÁRIO", "Nara: “Eu vim por vontade própria. Se encontrou esta lembrança, procure o arquivo de Sileth.”"); break; case "seal": archiveSeal = true; damage += 8; questStage = 4; notice = "SELO DA VIGÍLIA • DANO +8"; ShowMemory("SELO DA VIGÍLIA", "O selo guarda a última gravação de Nara: “Sileth não apagou os nomes para alimentar o Lume. Ela tentou esconder que o próprio farol está aprendendo a escolher quem esquecer.” Leve esta prova de volta à Curadora."); break; case "bell": riverBell = true; maxHealth += 20; health = Math.Min(maxHealth, health + 20); questStage = 6; notice = "SINO DE NARA • VIDA MÁXIMA +20"; ShowMemory("O SINO SEM BADALO", "A marca no bronze pertence a Nara. A voz gravada é de uma mulher que atravessou o rio: “Não estou presa. O farol me escuta porque eu aceitei levar as memórias para longe da cidade. Se ainda me procura, não tente me trazer de volta antes de ouvir o que o rio guarda.”"); break; }
        drops.Remove(item); noticeTime = 1.7f;
    }

    private void UsePotion() { if (potions > 0 && health < maxHealth) { potions--; health = Math.Min(maxHealth, health + 45); notice = "+45 VIDA"; noticeTime = 1.3f; } else { notice = potions == 0 ? "SEM POÇÕES" : "VIDA JÁ ESTÁ CHEIA"; noticeTime = 1.3f; } }
    private void Interact()
    {
        if (room == 3) { InteractTown(); return; }
        if (enemies.Count != 0) { notice = "DERROTE OS INIMIGOS PRIMEIRO"; noticeTime = 1.2f; return; }
        if (drops.Count != 0) { notice = "RECOLHA O LOOT ANTES DE AVANÇAR"; noticeTime = 1.5f; return; }
        if (room < MemoryStonePositions.Length && (memoriesFound & (1 << room)) == 0 && Distance(player, MemoryStonePositions[room]) < 52)
        {
            memoriesFound |= 1 << room;
            ShowMemory(room == 0 ? "INSCRIÇÃO NO VESTÍBULO" : "REGISTRO NO SALÃO DOS OSSOS", MemoryStoneTexts[room]);
            return;
        }
        if (room < 2 && player.X > 790) { room++; SetupRoom(); }
        else if (room == 2 && Distance(player, new PointF(750, 330)) < 110) chestChoice = true;
        else if (room == 4 && player.X > 790) { room = 5; SetupRoom(); ShowMemory("COFRE DO ARQUIVO", "As páginas finais foram seladas por dentro. O guardião que as protege já não distingue o nome de quem jurou salvar."); }
        else if (room == 4 && player.X < 115) { room = 3; SetupRoom(); }
        else if (room == 5 && player.X < 115) { room = 4; SetupRoom(); }
        else if (room == 5 && player.X > 790) { room = 3; SetupRoom(); ShowMemory("REGRESSO A VÉSPERA", "O selo do Arquivo Norte pesa na mão. Sileth precisa ver a gravação de Nara."); }
        else if (room == 6 && player.X > 790) { room = 7; SetupRoom(); ShowMemory("CAPELA AFOGADA", "Sob as pedras do rio, um sino toca sem badalo. Uma figura bloqueia a passagem e repete os nomes que a corrente levou."); }
        else if (room == 6 && player.X < 115) { room = 3; SetupRoom(); }
        else if (room == 7 && player.X < 115) { room = 6; SetupRoom(); }
        else if (room == 7 && player.X > 790) { room = 3; SetupRoom(); ShowMemory("REGRESSO A VÉSPERA", "O bronze marcado com o nome de Nara ainda vibra. Edda reconhecerá o sino."); }
    }
    private void InteractTown()
    {
        if (Distance(player, CuratorPosition) < 58)
        {
            if (questStage == 0)
            {
                questStage = 1;
                ShowMemory("CURADORA SILETH", "Você trouxe uma lembrança de Nara. Os registros oficiais não contam por que ela desceu à cripta. Leia o Livro das Ausências no posto antigo da praça; depois conversaremos sobre o que ele prova.");
            }
            else if (questStage == 1) ShowMemory("CURADORA SILETH", "O Livro das Ausências ficava no posto junto ao poço. Procure a mesa marcada com o selo do farol.");
            else if (questStage == 2)
            {
                questStage = 3; gold += 40; potions++;
                ShowMemory("A CONFISSÃO DE SILETH", "Nara descobriu que o Lume consumia nomes, não apenas lembranças. Eu fechei o arquivo para impedir o pânico. Ela procurava uma prova que pudesse convencer a cidade. Leve o registro ao Arquivo Norte, além do rio.");
                notice = "RECOMPENSA: 40 OURO E 1 POÇÃO"; noticeTime = 3;
            }
            else if (questStage == 3) ShowMemory("CURADORA SILETH", "O Arquivo Norte fica além do Rio Cego, pelo portão de pedra ao norte da praça. Traga o selo que Nara encontrou.");
            else if (questStage == 4) { questStage = 5; gold += 65; potions++; ShowMemory("A VOZ QUE ESCOLHE", "Nara deixou uma prova de que o Lume não é apenas uma defesa. Ele começou a escolher quais nomes consome. Não posso mais esconder isso. Vou reunir a cidade; você precisa seguir o rastro dela até o leito do Rio Cego."); notice = "MISSÃO CONCLUÍDA: 65 OURO E 1 POÇÃO"; noticeTime = 3; }
            else ShowMemory("CURADORA SILETH", "O selo confirmou o que Nara temia. O rastro dela segue pelo leito do Rio Cego, abaixo do farol.");
            return;
        }
        if (Distance(player, BellKeeperPosition) < 58)
        {
            if (questStage == 6 && riverBell) { questStage = 7; gold += 100; potions++; ShowMemory("EDDA RECONHECE O SINO", "Esse era o sino de travessia do meu pai. Nara o levou quando desceu ao rio; ela conhecia o caminho e decidiu cruzá-lo. Não foi capturada. Guarde o sino: se ela voltar, vai saber que ainda lembramos seu nome."); notice = "MISSÃO CONCLUÍDA: 100 OURO E 1 POÇÃO"; noticeTime = 3; }
            else ShowMemory("EDDA, TOCADORA DE SINOS", "Os sinos continuam inteiros. O que sumiu foram os nomes gravados neles. Quando tento lembrar quem os fundiu, só escuto água debaixo da praça.");
            return;
        }
        if (Distance(player, ArchiveBookPosition) < 58)
        {
            if (questStage == 0) ShowMemory("LIVRO DAS AUSÊNCIAS", "O volume está lacrado com o selo da Curadora. Fale com Sileth antes de tentar abri-lo.");
            else if (questStage == 1)
            {
                questStage = 2;
                ShowMemory("LIVRO DAS AUSÊNCIAS", "Registro de Vigília: o Guardião entregou seu nome ao Lume. Quatorze nomes foram usados para mantê-lo aceso. A ordem de encerramento do arquivo leva a assinatura de Sileth.");
            }
            else ShowMemory("LIVRO DAS AUSÊNCIAS", "As páginas registram os nomes entregues ao Lume. A assinatura de Sileth fecha o último volume.");
            return;
        }
        if (Distance(player, new PointF(480, 135)) < 64)
        {
            if (questStage >= 3) { room = 4; SetupRoom(); ShowMemory("ARQUIVO NORTE", "O portão cede. Entre as estantes, ecos repetem trechos de depoimentos que foram removidos do registro oficial."); }
            else ShowMemory("PORTÃO DO ARQUIVO NORTE", "O selo da Curadora mantém o portão fechado. Descubra o que o Livro das Ausências prova e fale com Sileth.");
            return;
        }
        if (Distance(player, new PointF(480, 520)) < 64)
        {
            if (questStage >= 5) { room = 6; SetupRoom(); ShowMemory("LEITO DO RIO CEGO", "A água desapareceu da superfície, mas as pedras ainda guardam as últimas palavras de quem cruzou o rio. O sino de Nara aponta para baixo da corrente."); }
            else ShowMemory("PASSAGEM DO RIO", "Edda não reconhece o caminho para o leito seco. Conclua sua investigação com Sileth antes de descer.");
            return;
        }
        if (Distance(player, CryptGatePosition) < 58)
        {
            room = 2; SetupRoom(); ShowMemory("A CRIPTA", "A passagem continua aberta. O Guardião protege o relicário, mas a próxima pista está nos arquivos de Véspera.");
        }
    }
    private void ShowMemory(string titleText, string bodyText) { memoryTitle = titleText; memoryText = bodyText; memoryOverlay = true; }
    private Enemy? lockedTarget;
    private void CycleTarget() { if (enemies.Count == 0) { lockedTarget = null; return; } int i = lockedTarget is null ? -1 : enemies.IndexOf(lockedTarget); lockedTarget = enemies[(i + 1) % enemies.Count]; notice = $"ALVO: {EnemyName(lockedTarget)}"; noticeTime = 1.4f; }
    private void EquipWeapon(int index) { if ((ownedWeapons & (1 << index)) == 0) { notice = "ARMA AINDA NÃO ENCONTRADA"; noticeTime = 1.4f; return; } damage += weaponDamage[index] - weaponDamage[weapon]; weapon = index; }
    private PointF MoveEnemy(Enemy e, PointF delta) { PointF p = e.Pos; PointF x = new(p.X + delta.X, p.Y), y = new(p.X, p.Y + delta.Y); if (!Blocked(x, e.Radius)) p.X = x.X; if (!Blocked(new PointF(p.X, y.Y), e.Radius)) p.Y = y.Y; return new PointF(Math.Clamp(p.X, 85, 875), Math.Clamp(p.Y, 128, 533)); }
    private void SeparateFromPack(Enemy enemy, float dt) { PointF push = PointF.Empty; foreach (Enemy other in enemies) { if (ReferenceEquals(enemy, other)) continue; PointF away = Sub(enemy.Pos, other.Pos); float d = Length(away); if (d > 0.01f && d < 40) push = Add(push, Scale(away, (40 - d) / (40 * d))); } if (Length(push) > 0.01f) enemy.Pos = MoveEnemy(enemy, Scale(Normalize(push, PointF.Empty), 52 * dt)); }
    private void MovePlayer(PointF delta) { PointF x = new(player.X + delta.X, player.Y), y = new(player.X, player.Y + delta.Y); if (!Blocked(x)) player.X = x.X; if (!Blocked(new PointF(player.X, y.Y))) player.Y = y.Y; player.X = Math.Clamp(player.X, 85, 875); player.Y = Math.Clamp(player.Y, 128, 533); }
    private bool Blocked(PointF p, float radius = 12) { RectangleF body = new(p.X - radius, p.Y - radius, radius * 2, radius * 2); RectangleF[] obstacles = room == 3 ? TownBuildings : room == 4 ? ArchiveWalls : room == 5 ? VaultWalls : room == 6 ? RiverWalls : room == 7 ? BellChamberWalls : Walls; return obstacles.Any(w => w.IntersectsWith(body)); }
    private bool BlockedLine(PointF a, PointF b)
    {
        int steps = Math.Max(1, (int)(Distance(a, b) / 6));
        RectangleF[] obstacles = room == 4 ? ArchiveWalls : room == 5 ? VaultWalls : room == 6 ? RiverWalls : room == 7 ? BellChamberWalls : Walls;
        for (int i = 0; i <= steps; i++) { float t = i / (float)steps; PointF p = new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t); if (obstacles.Any(w => w.Contains(p))) return true; }
        return false;
    }

    private SaveData CaptureSaveData() => new() { Room = room, Level = level, Xp = xp, Gold = gold, Potions = potions, Damage = damage, Health = health, MaxHealth = maxHealth, Weapon = weapon, OwnedWeapons = ownedWeapons, Armor = armor, Relic = relic, HealthTalent = healthTalent, SkillTalent = skillTalent, DodgeTalent = dodgeTalent, RoomCleared = true, Completed = victory, MemoriesFound = memoriesFound, QuestStage = questStage, ArchiveSeal = archiveSeal, RiverBell = riverBell };
    private void SaveGame() { Directory.CreateDirectory(Path.GetDirectoryName(SavePath)!); File.WriteAllText(SavePath, JsonSerializer.Serialize(CaptureSaveData())); notice = "PROGRESSO SALVO"; noticeTime = 2; }
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
            if (s.Room is < 0 or > 7 || s.Level is < 1 or > 10000 || s.Xp < 0 || s.Xp >= s.Level * 60 || s.Weapon is < 0 or > 2 || s.OwnedWeapons < 1 || s.OwnedWeapons > 7 || (s.OwnedWeapons & (1 << s.Weapon)) == 0 || s.MaxHealth < 1 || s.Health <= 0 || s.Health > s.MaxHealth || s.Potions < 0 || s.Gold < 0 || s.Damage < 1 || s.MemoriesFound is < 0 or > 3 || s.QuestStage is < 0 or > 7 || s.Completed && s.Room != 2) throw new InvalidDataException();
            StartGame(); room = s.Room; level = s.Level; xp = s.Xp; gold = s.Gold; potions = s.Potions; damage = s.Damage; health = s.Health; maxHealth = s.MaxHealth; weapon = s.Weapon; ownedWeapons = s.OwnedWeapons; armor = s.Armor; relic = s.Relic; healthTalent = s.HealthTalent; skillTalent = s.SkillTalent; dodgeTalent = s.DodgeTalent; memoriesFound = s.MemoriesFound; questStage = s.QuestStage; archiveSeal = s.ArchiveSeal; riverBell = s.RiverBell; SetupRoom(); if (s.RoomCleared) enemies.Clear(); victory = s.Completed; title = false; storyOverlay = false;
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
        if (room == 3) DrawTownEnvironment(g, floor, tile);
        else if (room is 4 or 5) DrawArchiveEnvironment(g, floor, tile);
        else if (room >= 6) DrawRiverEnvironment(g, floor, tile);
        else
        {
            g.FillRectangle(floor, 65, 108, 830, 445); for (int y = 0; y < 7; y++) for (int x = 0; x < 12; x++) g.DrawRectangle(tile, 70 + x * 68, 112 + y * 62, 67, 61);
            using var wallBrush = new SolidBrush(Color.FromArgb(90, 79, 62)); using var wallEdge = new Pen(Color.FromArgb(154, 132, 91), 3); foreach (RectangleF wall in Walls) { g.FillRectangle(wallBrush, wall); g.DrawRectangle(wallEdge, wall.X, wall.Y, wall.Width, wall.Height); }
            using var exitPen = new Pen(enemies.Count == 0 && drops.Count == 0 ? Color.LightGreen : Color.FromArgb(45, 48, 43), 8); g.DrawRectangle(exitPen, 882, 293, 3, 75);
        }
        foreach (Enemy enemy in enemies) DrawEnemy(g, enemy);
        foreach (Drop item in drops) DrawDrop(g, item);
        DrawPlayer(g);
        g.DrawString(room == 3 ? roomNames[room] : room < 3 ? $"{room + 1} / 3   {roomNames[room]}" : room < 6 ? $"{room - 3} / 2   {roomNames[room]}" : $"{room - 5} / 2   {roomNames[room]}", roomFont, goldBrush, 67, 572);
        int memoriesRead = (memoriesFound & 1) + ((memoriesFound >> 1) & 1);
        g.DrawString($"ECOS {memoriesRead} / 2", hudFont, goldBrush, 760, 572);
        if (room == 3) g.DrawString(TownObjective(), hudFont, goldBrush, 67, 592);
        else if (room >= 4 && room < 6) g.DrawString(ArchiveObjective(), hudFont, goldBrush, 67, 592);
        else if (room >= 6) g.DrawString(RiverObjective(), hudFont, goldBrush, 67, 592);
        g.DrawString(room == 3 ? "WASD mover   E falar/investigar   F5 salvar   I inventário" : "WASD mover   ESPAÇO/CLIQUE atacar   SHIFT esquivar   R redemoinho   Q poção   E interagir   I inventário", helpFont, textBrush, 67, 619);
        if (room < MemoryStonePositions.Length && (memoriesFound & (1 << room)) == 0) DrawMemoryStone(g, MemoryStonePositions[room], Distance(player, MemoryStonePositions[room]) < 58);
        if (noticeTime > 0) DrawCentered(g, notice, 170, new Font("Segoe UI", 13, FontStyle.Bold), Color.FromArgb(230, 193, 129));
        if (title || dead || victory) DrawOverlay(g, title ? "SCADRYK" : victory ? "VITÓRIA" : "VOCÊ CAIU", title ? "ENTER PARA COMEÇAR    •    C CARREGAR SAVE" : "ENTER PARA RECOMEÇAR    •    F5 SALVAR");
        if (inventoryOpen) DrawInventory(g);
        if (talentChoice) DrawChoice(g, $"SUBIU DE NÍVEL — ESCOLHA ({pendingTalents} PENDENTES)", "1. VIGOR  +20 VIDA MÁXIMA", "2. DOMÍNIO  Redemoinho recarrega mais rápido", "3. AGILIDADE  esquiva recarrega mais rápido");
        if (chestChoice) DrawChoice(g, "BAÚ DA CRIPTA — ESCOLHA UMA RECOMPENSA", "1. 75 OURO", "2. DUAS POÇÕES", "3. RECUPERAR TODA A VIDA");
        if (storyOverlay) DrawStoryOverlay(g);
        if (memoryOverlay) DrawMemoryOverlay(g);
    }

    private string TownObjective() => questStage switch
    {
        0 => "OBJETIVO: ENCONTRE A CURADORA SILETH",
        1 => "OBJETIVO: LEIA O LIVRO DAS AUSÊNCIAS",
        2 => "OBJETIVO: VOLTE E FALE COM SILETH",
        3 => "OBJETIVO: ENTRE NO ARQUIVO NORTE AO NORTE",
        4 => "OBJETIVO: MOSTRE O SELO DA VIGÍLIA A SILETH",
        5 => "OBJETIVO: DESÇA AO LEITO DO RIO CEGO AO SUL",
        6 => "OBJETIVO: MOSTRE A EDDA O SINO DE NARA",
        _ => "PRÓXIMO DESTINO: SIGA A CORRENTE SOB O FAROL"
    };

    private void DrawTownEnvironment(Graphics g, Brush ground, Pen tile)
    {
        g.FillRectangle(ground, 65, 108, 830, 445);
        for (int y = 0; y < 7; y++) for (int x = 0; x < 12; x++) g.DrawRectangle(tile, 70 + x * 68, 112 + y * 62, 67, 61);
        using var road = new SolidBrush(Color.FromArgb(94, 84, 68));
        using var plaza = new SolidBrush(Color.FromArgb(105, 96, 78));
        using var trim = new Pen(Color.FromArgb(159, 139, 101), 2);
        g.FillRectangle(road, 345, 274, 315, 118); g.FillRectangle(road, 430, 242, 116, 210);
        g.FillEllipse(plaza, 424, 290, 128, 92); g.DrawEllipse(trim, 424, 290, 128, 92);
        using var water = new SolidBrush(Color.FromArgb(88, 142, 145));
        g.FillEllipse(water, 458, 315, 60, 42); g.DrawEllipse(trim, 458, 315, 60, 42);
        DrawTownHouse(g, TownBuildings[0], "FERRARIA"); DrawTownHouse(g, TownBuildings[1], "ARQUIVO");
        DrawTownHouse(g, TownBuildings[2], "CASAS"); DrawTownHouse(g, TownBuildings[3], "ARMAZÉM");
        using var gateBrush = new SolidBrush(Color.FromArgb(116, 100, 70)); using var gatePen = new Pen(Color.FromArgb(220, 189, 125), 3);
        g.FillRectangle(gateBrush, 70, 298, 30, 64); g.DrawRectangle(gatePen, 70, 298, 30, 64);
        using var bookBrush = new SolidBrush(Color.FromArgb(107, 56, 43)); using var bookEdge = new Pen(Color.FromArgb(234, 204, 133), 2);
        g.FillRectangle(bookBrush, ArchiveBookPosition.X - 13, ArchiveBookPosition.Y - 7, 26, 18); g.DrawRectangle(bookEdge, ArchiveBookPosition.X - 13, ArchiveBookPosition.Y - 7, 26, 18);
        DrawTownPerson(g, CuratorPosition, "SILETH", Color.FromArgb(117, 73, 104), Distance(player, CuratorPosition) < 72);
        DrawTownPerson(g, BellKeeperPosition, "EDDA", Color.FromArgb(111, 137, 124), Distance(player, BellKeeperPosition) < 72);
        if (Distance(player, ArchiveBookPosition) < 72) DrawCentered(g, "E  LER", ArchiveBookPosition.Y + 22, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, ArchiveBookPosition.X);
        if (Distance(player, CryptGatePosition) < 72) DrawCentered(g, "E  CRIPTA", CryptGatePosition.Y + 42, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, CryptGatePosition.X + 15);
        PointF archiveGate = new(480, 135);
        using var portal = new Pen(questStage >= 3 ? Color.FromArgb(219, 178, 112) : Color.FromArgb(88, 75, 61), 5);
        g.DrawArc(portal, archiveGate.X - 28, archiveGate.Y - 24, 56, 52, 180, 180);
        DrawCentered(g, questStage >= 3 ? "ARQUIVO NORTE" : "PORTÃO SELADO", archiveGate.Y - 38, new Font("Segoe UI", 8, FontStyle.Bold), Color.FromArgb(230, 193, 129), archiveGate.X);
        if (Distance(player, archiveGate) < 72) DrawCentered(g, "E  ENTRAR", archiveGate.Y + 28, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, archiveGate.X);
        PointF riverGate = new(480, 520);
        using var riverPortal = new Pen(questStage >= 5 ? Color.FromArgb(112, 179, 185) : Color.FromArgb(72, 85, 82), 5);
        g.DrawArc(riverPortal, riverGate.X - 28, riverGate.Y - 22, 56, 50, 0, 180);
        DrawCentered(g, questStage >= 5 ? "RIO CEGO" : "LEITO SELADO", riverGate.Y - 4, new Font("Segoe UI", 8, FontStyle.Bold), Color.FromArgb(190, 211, 196), riverGate.X);
        if (Distance(player, riverGate) < 72) DrawCentered(g, "E  DESCER", riverGate.Y - 38, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, riverGate.X);
    }

    private string ArchiveObjective() => room == 4 ? "OBJETIVO: CRUZE A GALERIA E ENCONTRE O COFRE" : archiveSeal ? "OBJETIVO: VOLTE A VÉSPERA COM O SELO" : "OBJETIVO: RECUPERE O REGISTRO NO COFRE";
    private string RiverObjective() => room == 6 ? "OBJETIVO: SIGA A CORRENTE ATÉ A CAPELA" : riverBell ? "OBJETIVO: RETORNE E MOSTRE O SINO A EDDA" : "OBJETIVO: ENCONTRE O SINO DE TRAVESSIA";

    private void DrawArchiveEnvironment(Graphics g, Brush ground, Pen tile)
    {
        g.FillRectangle(ground, 65, 108, 830, 445);
        for (int y = 0; y < 7; y++) for (int x = 0; x < 12; x++) g.DrawRectangle(tile, 70 + x * 68, 112 + y * 62, 67, 61);
        RectangleF[] shelves = room == 4 ? ArchiveWalls : VaultWalls;
        using var shelf = new SolidBrush(Color.FromArgb(69, 52, 43)); using var spine = new Pen(Color.FromArgb(162, 126, 79), 2); using var edge = new Pen(Color.FromArgb(184, 145, 94), 3);
        foreach (RectangleF area in shelves)
        {
            g.FillRectangle(shelf, area); g.DrawRectangle(edge, area.X, area.Y, area.Width, area.Height);
            for (float y = area.Y + 18; y < area.Bottom - 8; y += 25) { g.DrawLine(spine, area.X + 5, y, area.Right - 5, y); for (float x = area.X + 8; x < area.Right - 6; x += 12) g.DrawLine(Pens.SaddleBrown, x, y - 15, x, y - 2); }
        }
        using var threshold = new Pen(enemies.Count == 0 && drops.Count == 0 ? Color.LightGreen : Color.FromArgb(60, 71, 66), 7);
        g.DrawLine(threshold, 885, 290, 885, 370); g.DrawLine(threshold, 75, 290, 75, 370);
        using var glow = new SolidBrush(Color.FromArgb(34, 144, 194, 181));
        g.FillEllipse(glow, 464, 290, 64, 64);
    }

    private void DrawRiverEnvironment(Graphics g, Brush ground, Pen tile)
    {
        using var riverbed = new SolidBrush(Color.FromArgb(71, 76, 65)); using var stones = new SolidBrush(Color.FromArgb(91, 96, 80)); using var bank = new Pen(Color.FromArgb(124, 130, 105), 3);
        g.FillRectangle(ground, 65, 108, 830, 445);
        for (int y = 0; y < 7; y++) for (int x = 0; x < 12; x++) g.DrawRectangle(tile, 70 + x * 68, 112 + y * 62, 67, 61);
        g.FillPolygon(riverbed, new[] { new PointF(65, 272), new PointF(330, 306), new PointF(570, 285), new PointF(895, 317), new PointF(895, 407), new PointF(580, 374), new PointF(315, 408), new PointF(65, 380) });
        g.DrawLine(bank, 70, 272, 330, 306); g.DrawLine(bank, 330, 306, 570, 285); g.DrawLine(bank, 570, 285, 890, 317);
        g.DrawLine(bank, 70, 380, 315, 408); g.DrawLine(bank, 315, 408, 580, 374); g.DrawLine(bank, 580, 374, 890, 407);
        using var rock = new SolidBrush(Color.FromArgb(103, 103, 86)); using var slate = new SolidBrush(Color.FromArgb(72, 86, 87));
        for (int i = 0; i < 14; i++) { float x = 105 + i * 57; float y = 321 + (i % 3) * 18; g.FillEllipse(i % 4 == 0 ? slate : stones, x, y, 22 + i % 3 * 5, 12 + i % 2 * 4); }
        RectangleF[] obstacles = room == 6 ? RiverWalls : BellChamberWalls;
        using var edge = new Pen(Color.FromArgb(159, 146, 112), 3);
        foreach (RectangleF area in obstacles) { g.FillEllipse(rock, area); g.DrawEllipse(edge, area); }
        using var gate = new Pen(enemies.Count == 0 && drops.Count == 0 ? Color.LightGreen : Color.FromArgb(53, 70, 72), 7);
        g.DrawLine(gate, 885, 290, 885, 370); g.DrawLine(gate, 75, 290, 75, 370);
        if (room == 7) { using var glow = new SolidBrush(Color.FromArgb(44, 116, 190, 197)); g.FillEllipse(glow, 444, 250, 80, 80); }
    }

    private static void DrawTownHouse(Graphics g, RectangleF area, string sign)
    {
        using var stone = new SolidBrush(Color.FromArgb(74, 69, 60)); using var roof = new SolidBrush(Color.FromArgb(67, 48, 43));
        using var edge = new Pen(Color.FromArgb(148, 125, 86), 3); using var door = new SolidBrush(Color.FromArgb(39, 31, 27));
        g.FillRectangle(stone, area); g.FillRectangle(roof, area.X, area.Y, area.Width, 22); g.DrawRectangle(edge, area.X, area.Y, area.Width, area.Height);
        g.FillRectangle(door, area.X + area.Width / 2 - 16, area.Y + area.Height - 46, 32, 46);
        using var font = new Font("Segoe UI", 8, FontStyle.Bold); using var brush = new SolidBrush(Color.FromArgb(220, 197, 151));
        SizeF size = g.MeasureString(sign, font); g.DrawString(sign, font, brush, area.X + (area.Width - size.Width) / 2, area.Y + 31);
    }

    private static void DrawTownPerson(Graphics g, PointF pos, string name, Color robe, bool nearby)
    {
        using var body = new SolidBrush(robe); using var face = new SolidBrush(Color.Bisque);
        using var edge = new Pen(nearby ? Color.Gold : Color.FromArgb(34, 25, 20), nearby ? 3 : 2);
        g.FillEllipse(body, pos.X - 16, pos.Y - 7, 32, 35); g.DrawEllipse(edge, pos.X - 16, pos.Y - 7, 32, 35);
        g.FillEllipse(face, pos.X - 9, pos.Y - 20, 18, 18); g.DrawEllipse(edge, pos.X - 9, pos.Y - 20, 18, 18);
        DrawCentered(g, name, pos.Y - 39, new Font("Segoe UI", 9, FontStyle.Bold), Color.FromArgb(240, 220, 176), pos.X);
        if (nearby) DrawCentered(g, "E  FALAR", pos.Y + 34, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, pos.X);
    }

    private void DrawEnemy(Graphics g, Enemy enemy)
    {
        float r = enemy.Radius; Color color = enemy.Kind == EnemyKind.BlindScribe ? Color.FromArgb(89, 128, 135) : enemy.Kind == EnemyKind.RiverFerryman ? Color.FromArgb(69, 120, 124) : enemy.Boss ? Color.FromArgb(145, 87, 77) : enemy.Kind switch { EnemyKind.Elite => enemy.Armored ? Color.SlateGray : Color.IndianRed, EnemyKind.Archer => Color.FromArgb(128, 151, 106), EnemyKind.Mage => Color.MediumPurple, EnemyKind.InkEcho => Color.FromArgb(113, 98, 143), EnemyKind.DrownedMemory => Color.FromArgb(95, 145, 142), _ => Color.FromArgb(183, 181, 166) };
        if (enemy.Windup > 0) { float baseWarn = enemy.Kind == EnemyKind.Elite ? 62 : enemy.Kind == EnemyKind.Mage ? 52 : enemy.Boss && enemy.BossPattern == 0 ? 90 : enemy.Kind == EnemyKind.Archer ? 26 : enemy.Kind == EnemyKind.InkEcho ? 30 : enemy.Kind == EnemyKind.DrownedMemory ? 28 : enemy.Kind == EnemyKind.RiverFerryman && enemy.BossPattern == 2 ? 85 : 36; float progress = Math.Clamp(enemy.Windup / Math.Max(0.01f, enemy.WindupMax), 0, 1); float warn = baseWarn * (0.4f + progress * 0.6f); using var p = new Pen(Color.FromArgb(235, 115, 75), 3); using var b = new SolidBrush(Color.FromArgb(35, 235, 115, 75)); if ((enemy.Kind is EnemyKind.Archer or EnemyKind.InkEcho or EnemyKind.DrownedMemory) || (enemy.Kind == EnemyKind.RiverFerryman && enemy.BossPattern == 2)) g.DrawLine(p, enemy.Pos, enemy.Target); g.FillEllipse(b, enemy.Target.X - warn, enemy.Target.Y - warn, warn * 2, warn * 2); g.DrawEllipse(p, enemy.Target.X - warn, enemy.Target.Y - warn, warn * 2, warn * 2); }
        using var body = new SolidBrush(enemy.Flash > 0 ? Color.White : color); using var edge = new Pen(enemy == lockedTarget ? Color.Gold : enemy.Armored ? Color.LightSteelBlue : Color.FromArgb(35, 25, 22), enemy == lockedTarget ? 4 : 2);
        g.FillEllipse(body, enemy.Pos.X - r, enemy.Pos.Y - r, r * 2, r * 2); g.DrawEllipse(edge, enemy.Pos.X - r, enemy.Pos.Y - r, r * 2, r * 2);
        using var hpBack = new SolidBrush(Color.FromArgb(40, 20, 20)); using var hpBar = new SolidBrush(Color.FromArgb(188, 76, 70)); g.FillRectangle(hpBack, enemy.Pos.X - 22, enemy.Pos.Y - r - 10, 44, 5); g.FillRectangle(hpBar, enemy.Pos.X - 22, enemy.Pos.Y - r - 10, 44 * Math.Max(0, enemy.Hp / enemy.MaxHp), 5);
        string name = EnemyName(enemy); if (name.Length > 0) DrawCentered(g, name, enemy.Pos.Y - r - 28, new Font("Segoe UI", 8, FontStyle.Bold), Color.FromArgb(233, 198, 151), enemy.Pos.X);
        if (enemy.Boss) { using var bossBack = new SolidBrush(Color.FromArgb(40, 20, 20)); using var bossHp = new SolidBrush(enemy.Hp <= enemy.MaxHp * 0.5f ? Color.OrangeRed : enemy.Kind == EnemyKind.BlindScribe ? Color.Teal : enemy.Kind == EnemyKind.RiverFerryman ? Color.SteelBlue : Color.Firebrick); g.FillRectangle(bossBack, 360, 112, 240, 8); g.FillRectangle(bossHp, 360, 112, 240 * Math.Clamp(enemy.Hp / enemy.MaxHp, 0, 1), 8); string bossName = enemy.Kind == EnemyKind.BlindScribe ? "ESCRIBA CEGO" : enemy.Kind == EnemyKind.RiverFerryman ? "BARQUEIRO OCO" : "GUARDIÃO DA CRIPTA"; DrawCentered(g, enemy.Hp <= enemy.MaxHp * 0.5f ? $"{bossName} • SEGUNDA FASE" : bossName, 91, new Font("Georgia", 11, FontStyle.Bold), enemy.Hp <= enemy.MaxHp * 0.5f ? Color.OrangeRed : Color.Goldenrod); }
    }
    private void DrawDrop(Graphics g, Drop item) { Color c = item.Kind switch { "relic" => Color.Violet, "seal" => Color.LightSkyBlue, "bell" => Color.FromArgb(162, 212, 192), "armor" => Color.MediumAquamarine, "weapon" => Color.CornflowerBlue, "potion" => Color.IndianRed, _ => Color.Goldenrod }; using var b = new SolidBrush(c); using var p = new Pen(Color.FromArgb(230, 220, 190), 1); g.FillEllipse(b, item.Pos.X - 8, item.Pos.Y - 8, 16, 16); g.DrawEllipse(p, item.Pos.X - 8, item.Pos.Y - 8, 16, 16); }
    private void DrawPlayer(Graphics g) { using var b = new SolidBrush(invulnerable > 0 ? Color.Salmon : Color.FromArgb(90, 143, 138)); using var face = new SolidBrush(Color.Bisque); g.FillEllipse(b, player.X - 16, player.Y - 16, 32, 32); g.FillEllipse(face, player.X - 8, player.Y - 11, 16, 16); using var p = new Pen(Color.AntiqueWhite, 4); g.DrawLine(p, player, Add(player, Scale(facing, 24))); if (skillCd > 0) { using var cooldownPen = new Pen(Color.FromArgb(190, 230, 193, 129), 3); g.DrawArc(cooldownPen, player.X - 25, player.Y - 25, 50, 50, -90, 360 * skillCd / Math.Max(1.5f, 6f - skillTalent * .5f)); } }
    private void DrawInventory(Graphics g)
    {
        using var b = new SolidBrush(Color.FromArgb(245, 17, 19, 20)); using var itemFont = new Font("Segoe UI", 12); g.FillRectangle(b, 150, 145, 660, 355);
        DrawCentered(g, "INVENTÁRIO   •   1–3 ARMAS   •   4 ARMADURA   •   5 RELÍQUIA   •   I FECHAR", 180, new Font("Segoe UI", 12, FontStyle.Bold), Color.Goldenrod);
        for (int i = 0; i < 3; i++) g.DrawString($"{i + 1}. {weaponNames[i]}    DANO {weaponDamage[i] - weaponDamage[weapon]:+#;-#;0}    {(weapon == i ? "EQUIPADA" : (ownedWeapons & (1 << i)) != 0 ? "DISPONÍVEL" : "NÃO ENCONTRADA")}", itemFont, (ownedWeapons & (1 << i)) != 0 ? Brushes.AntiqueWhite : Brushes.Gray, 205, 230 + i * 40);
        g.DrawString($"4. PEITORAL DE PEDRA {(armorOn ? "EQUIPADO" : armor ? "DISPONÍVEL" : "NÃO ENCONTRADO")}", itemFont, Brushes.AntiqueWhite, 205, 360);
        g.DrawString($"5. RELICÁRIO VITAL {(relicOn ? "EQUIPADO" : relic ? "DISPONÍVEL" : "NÃO ENCONTRADO")}", itemFont, Brushes.AntiqueWhite, 205, 400);
        g.DrawString($"SELO DA VIGÍLIA {(archiveSeal ? "ENCONTRADO • +8 DANO" : "AINDA NÃO ENCONTRADO")}", itemFont, Brushes.AntiqueWhite, 205, 440);
        g.DrawString($"SINO DE NARA {(riverBell ? "ENCONTRADO • +20 VIDA MÁXIMA" : "AINDA NÃO ENCONTRADO")}", itemFont, Brushes.AntiqueWhite, 205, 480);
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
        g.DrawString(memoryTitle, headingFont, headingBrush, 112, 440);
        g.DrawString(memoryText, bodyFont, bodyBrush, new RectangleF(112, 472, 736, 64));
        g.DrawString("ENTER, ESPAÇO OU CLIQUE  •  FECHAR", hintFont, hintBrush, 112, 548);
    }
    private static void DrawMemoryStone(Graphics g, PointF pos, bool nearby)
    {
        float pulse = nearby ? 17 : 14;
        PointF[] shard = { new(pos.X, pos.Y - pulse), new(pos.X + pulse * .7f, pos.Y), new(pos.X, pos.Y + pulse), new(pos.X - pulse * .7f, pos.Y) };
        using var glow = new SolidBrush(Color.FromArgb(nearby ? 100 : 55, 119, 186, 188));
        using var stone = new SolidBrush(Color.FromArgb(nearby ? 245 : 210, 120, 179, 181));
        using var edge = new Pen(Color.FromArgb(235, 207, 224, 193), 2);
        g.FillEllipse(glow, pos.X - 24, pos.Y - 24, 48, 48);
        g.FillPolygon(stone, shard); g.DrawPolygon(edge, shard);
        if (nearby) DrawCentered(g, "E  ESCUTAR", pos.Y + 23, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, pos.X);
    }
    private void DrawOverlay(Graphics g, string titleText, string instruction) { using var bg = new SolidBrush(Color.FromArgb(225, 8, 10, 12)); g.FillRectangle(bg, 0, 0, 960, 640); DrawCentered(g, titleText, 250, new Font("Georgia", 36, FontStyle.Bold), Color.FromArgb(230, 193, 129)); DrawCentered(g, instruction, 340, new Font("Segoe UI", 15, FontStyle.Bold), Color.AntiqueWhite); if (noticeTime > 0) DrawCentered(g, notice, 410, new Font("Segoe UI", 12), Color.Goldenrod); }
    private static void DrawCentered(Graphics g, string s, float y, Font f, Color color, float x = 480) { using (f) using (var brush = new SolidBrush(color)) { SizeF size = g.MeasureString(s, f); g.DrawString(s, f, brush, x - size.Width / 2, y); } }
    private static string EnemyName(Enemy e) => e.Kind switch { EnemyKind.Guardian => "GUARDIÃO SEM NOME", EnemyKind.BlindScribe => "ESCRIBA CEGO", EnemyKind.RiverFerryman => "BARQUEIRO OCO", EnemyKind.DrownedMemory => "LEMBRANÇA AFOGADA", EnemyKind.Elite => e.Armored ? "ELITE • BLINDADO" : "ELITE • FRENÉTICO", EnemyKind.Archer => "ARQUEIRO", EnemyKind.Mage => "CONJURADOR", EnemyKind.InkEcho => "ECO DE TINTA", _ => "" };
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
        game.player = MemoryStonePositions[0]; game.Interact();
        if (game.memoriesFound != 0) throw new InvalidOperationException("Uma pista foi liberada sem limpar os inimigos.");
        game.enemies.Clear(); game.Interact();
        if (game.memoriesFound != 1 || !game.memoryOverlay || game.memoryTitle != "INSCRIÇÃO NO VESTÍBULO") throw new InvalidOperationException("A pista do vestíbulo não foi registrada ao interagir.");
        game.memoryOverlay = false; game.room = 1; game.player = MemoryStonePositions[1]; game.Interact();
        if (game.memoriesFound != 3 || !game.memoryOverlay) throw new InvalidOperationException("A pista do Salão dos Ossos não foi registrada.");
        game.memoryOverlay = false; game.room = 0; game.SetupRoom(); game.storyOverlay = false;
        if (game.enemies.Count != 3) throw new InvalidOperationException("A primeira sala deve iniciar com três inimigos.");
        game.player = new PointF(320, 230); game.MovePlayer(new PointF(40, 40)); PointF movedPlayer = game.player;
        if (Math.Abs(movedPlayer.X - 320) > 0.01f || Math.Abs(movedPlayer.Y - 270) > 0.01f) throw new InvalidOperationException("O movimento não deslizou pela quina do obstáculo.");
        Enemy grunt = game.enemies[0]; game.player = new PointF(grunt.Pos.X - 22, grunt.Pos.Y); grunt.Cooldown = 0; game.UpdateEnemy(grunt, 0.01f);
        if (grunt.Windup <= 0 || game.health < game.maxHealth) throw new InvalidOperationException("Ataque corpo a corpo comum não foi anunciado.");
        game.player = new PointF(grunt.Pos.X - 100, grunt.Pos.Y); game.UpdateEnemy(grunt, 0.6f);
        if (game.health < game.maxHealth) throw new InvalidOperationException("A esquiva antes do golpe não evitou o dano.");
        game.AddXp(60);
        if (game.level != 2 || !game.talentChoice || game.pendingTalents != 1) throw new InvalidOperationException("A progressão por nível não concedeu uma escolha de talento.");
        game.talentChoice = false; int goldBeforeTown = game.gold, potionsBeforeTown = game.potions; game.EnterVesper(); game.memoryOverlay = false;
        if (game.room != 3 || game.enemies.Count != 0) throw new InvalidOperationException("A praça de Véspera não abriu como área segura explorável.");
        game.player = new PointF(350, 200); game.MovePlayer(new PointF(-40, 0));
        if (Math.Abs(game.player.X - 350) > 0.01f) throw new InvalidOperationException("A colisão da ferraria não bloqueou a passagem pela parede.");
        game.player = CuratorPosition; game.Interact();
        if (game.questStage != 1 || !game.memoryOverlay) throw new InvalidOperationException("Sileth não iniciou a investigação do livro.");
        game.memoryOverlay = false; game.player = ArchiveBookPosition; game.Interact();
        if (game.questStage != 2 || !game.memoryOverlay) throw new InvalidOperationException("O Livro das Ausências não avançou a missão.");
        game.memoryOverlay = false; game.player = CuratorPosition; game.Interact();
        if (game.questStage != 3 || game.gold != goldBeforeTown + 40 || game.potions != potionsBeforeTown + 1) throw new InvalidOperationException("A conclusão da investigação não concedeu progresso e recompensa.");
        game.memoryOverlay = false; game.player = new PointF(480, 135); game.Interact();
        if (game.room != 4 || game.enemies.Count != 5 || !game.enemies.Any(e => e.Kind == EnemyKind.InkEcho)) throw new InvalidOperationException("A entrada e os inimigos originais da Galeria do Arquivo não foram carregados.");
        Enemy inkEcho = game.enemies.First(e => e.Kind == EnemyKind.InkEcho); inkEcho.Cooldown = 0; game.UpdateEnemy(inkEcho, 0.01f);
        if (inkEcho.Windup <= 0) throw new InvalidOperationException("O Eco de Tinta não anunciou o disparo à distância.");
        game.enemies.Clear(); game.drops.Clear(); game.player = new PointF(850, 330); game.Interact();
        if (game.room != 5 || !game.enemies.Any(e => e.Kind == EnemyKind.BlindScribe)) throw new InvalidOperationException("O cofre não apresentou o chefe Escriba Cego.");
        Enemy scribe = game.enemies.Single(e => e.Kind == EnemyKind.BlindScribe); game.enemies.Clear(); game.enemies.Add(scribe);
        scribe.Cooldown = 0; game.UpdateEnemy(scribe, 0.01f);
        if (scribe.Windup <= 0 || scribe.BossPattern != 1) throw new InvalidOperationException("A primeira fase do Escriba Cego não anunciou o golpe direcionado.");
        game.player = new PointF(840, 330); game.UpdateEnemy(scribe, 1f);
        scribe.Hp = 100; scribe.Cooldown = 0; game.UpdateEnemy(scribe, 0.01f);
        if (scribe.Windup <= 0 || scribe.BossPattern != 0) throw new InvalidOperationException("A segunda fase do Escriba Cego não alternou para a área de impacto.");
        game.Defeat(scribe); game.talentChoice = false;
        if (!game.drops.Any(d => d.Kind == "seal")) throw new InvalidOperationException("O Escriba Cego não deixou o Selo da Vigília.");
        foreach (Drop drop in game.drops.ToArray()) game.Collect(drop);
        if (!game.archiveSeal || game.questStage != 4 || game.damage < 33 || !game.memoryOverlay) throw new InvalidOperationException("O selo não avançou a história nem concedeu seu bônus.");
        game.memoryOverlay = false; game.player = new PointF(850, 330); game.Interact();
        if (game.room != 3) throw new InvalidOperationException("A saída do cofre não retornou a Véspera.");
        game.player = CuratorPosition; game.Interact();
        if (game.questStage != 5 || game.gold != goldBeforeTown + 140 || game.potions != potionsBeforeTown + 2) throw new InvalidOperationException("Sileth não concluiu a nova etapa da investigação com sua recompensa.");
        game.memoryOverlay = false; game.player = new PointF(480, 520); game.Interact();
        if (game.room != 6 || game.enemies.Count != 6 || !game.enemies.Any(e => e.Kind == EnemyKind.DrownedMemory)) throw new InvalidOperationException("O leito do Rio Cego não abriu com sua nova criatura à distância.");
        Enemy drownedMemory = game.enemies.First(e => e.Kind == EnemyKind.DrownedMemory); drownedMemory.Cooldown = 0; game.UpdateEnemy(drownedMemory, 0.01f);
        if (drownedMemory.Windup <= 0) throw new InvalidOperationException("A Lembrança Afogada não anunciou o ataque à distância.");
        game.enemies.Clear(); game.drops.Clear(); game.player = new PointF(850, 330); game.Interact();
        if (game.room != 7 || !game.enemies.Any(e => e.Kind == EnemyKind.RiverFerryman)) throw new InvalidOperationException("A Capela Afogada não apresentou o Barqueiro Oco.");
        Enemy ferryman = game.enemies.Single(e => e.Kind == EnemyKind.RiverFerryman); game.enemies.Clear(); game.enemies.Add(ferryman);
        ferryman.Cooldown = 0; game.UpdateEnemy(ferryman, 0.01f);
        if (ferryman.Windup <= 0 || ferryman.BossPattern != 1) throw new InvalidOperationException("O Barqueiro Oco não anunciou seu ataque marcado.");
        game.player = new PointF(840, 330); game.UpdateEnemy(ferryman, 1f); ferryman.Hp = 100; ferryman.Cooldown = 0; game.UpdateEnemy(ferryman, 0.01f);
        if (ferryman.BossPattern != 2) throw new InvalidOperationException("O Barqueiro Oco não alternou para o golpe em linha.");
        game.player = new PointF(150, 330); game.UpdateEnemy(ferryman, 1f); ferryman.Cooldown = 0; game.UpdateEnemy(ferryman, 0.01f);
        if (ferryman.BossPattern != 0) throw new InvalidOperationException("O Barqueiro Oco não concluiu o ciclo de três ataques.");
        game.Defeat(ferryman); game.talentChoice = false;
        if (!game.drops.Any(d => d.Kind == "bell")) throw new InvalidOperationException("O Barqueiro Oco não deixou o sino de Nara.");
        foreach (Drop drop in game.drops.ToArray()) game.Collect(drop);
        if (!game.riverBell || game.questStage != 6 || game.maxHealth < 120 || !game.memoryOverlay) throw new InvalidOperationException("O sino não revelou a decisão de Nara nem concedeu vitalidade.");
        game.memoryOverlay = false; game.player = new PointF(850, 330); game.Interact();
        if (game.room != 3) throw new InvalidOperationException("A saída da Capela Afogada não retornou a Véspera.");
        game.player = BellKeeperPosition; game.Interact();
        if (game.questStage != 7 || game.gold != goldBeforeTown + 275 || game.potions != potionsBeforeTown + 3) throw new InvalidOperationException("Edda não reconheceu o sino e encerrou esta etapa da investigação.");
        SaveData saved = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(game.CaptureSaveData()))!;
        if (saved.Room != 3 || saved.QuestStage != 7 || saved.MemoriesFound != 3 || !saved.ArchiveSeal || !saved.RiverBell || saved.MaxHealth < 120) throw new InvalidOperationException("O progresso da campanha e os itens não foram serializados no save.");
        game.memoryOverlay = false; game.player = CryptGatePosition; game.Interact();
        if (game.room != 2 || game.enemies.Count != 4) throw new InvalidOperationException("A passagem da cidade não retornou à cripta.");
        Console.WriteLine("TESTES OK: prólogo, pistas, combate, Véspera, Arquivo Norte, Rio Cego, chefes, itens, progressão, save e retorno à cripta."); return 0;
    }
}







