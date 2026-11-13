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
    private enum EnemyKind { Grunt, Archer, Mage, Elite, Guardian, InkEcho, BlindScribe, DrownedMemory, RiverFerryman, EmberWisp, AshWarden, NameMoth, HollowName }
    private sealed class Enemy
    {
        public PointF Pos, Target;
        public EnemyKind Kind;
        public float Hp, MaxHp, Cooldown, Windup, WindupMax, Flash;
        public int BossPattern;
        public bool Armored;
        public bool Boss => Kind is EnemyKind.Guardian or EnemyKind.BlindScribe or EnemyKind.RiverFerryman or EnemyKind.AshWarden or EnemyKind.HollowName;
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
        public int ClearedRooms { get; set; }
        public bool ArchiveSeal { get; set; }
        public bool RiverBell { get; set; }
        public bool LumeLens { get; set; }
        public bool NaraTrace { get; set; }
        public int OwnedCharms { get; set; }
        public int EquippedCharm { get; set; }
        public int TownEnding { get; set; }
    }

    private static readonly RectangleF[] Walls = { new(345, 235, 48, 90), new(565, 375, 58, 80) };
    private static readonly RectangleF[] ArchiveWalls = { new(275, 155, 38, 155), new(437, 365, 48, 165), new(626, 150, 42, 165), new(765, 374, 42, 150) };
    private static readonly RectangleF[] VaultWalls = { new(280, 170, 58, 118), new(625, 170, 58, 118), new(280, 410, 58, 118), new(625, 410, 58, 118) };
    private static readonly RectangleF[] RiverWalls = { new(250, 175, 80, 48), new(410, 390, 96, 52), new(645, 170, 70, 48), new(760, 390, 75, 55) };
    private static readonly RectangleF[] BellChamberWalls = { new(285, 150, 46, 180), new(630, 345, 48, 180), new(423, 435, 112, 42) };
    private static readonly RectangleF[] AshGalleryWalls = { new(250, 165, 58, 145), new(440, 380, 78, 130), new(640, 165, 52, 145), new(770, 380, 55, 130) };
    private static readonly RectangleF[] LumeChamberWalls = { new(300, 150, 48, 140), new(612, 390, 48, 140), new(428, 260, 104, 44), new(428, 420, 104, 44) };
    private static readonly RectangleF[] NameShoreWalls = { new(248, 170, 54, 126), new(434, 395, 64, 128), new(640, 166, 56, 126), new(770, 394, 54, 126) };
    private static readonly RectangleF[] VoiceWellWalls = { new(270, 158, 64, 122), new(626, 158, 64, 122), new(270, 410, 64, 122), new(626, 410, 64, 122), new(438, 325, 84, 76) };
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
    private readonly string[] roomNames = { "VESTÍBULO EM RUÍNAS", "SALÃO DOS OSSOS", "CÂMARA DO GUARDIÃO", "VÉSPERA • PRAÇA DO FAROL", "ARQUIVO NORTE • GALERIA", "ARQUIVO NORTE • COFRE", "RIO CEGO • LEITO SECO", "RIO CEGO • CAPELA AFOGADA", "FAROL • ESCADA DE CINZAS", "FAROL • CÂMARA DO LUME", "ALÉM DO LUME • MARGEM SEM REFLEXO", "RESERVATÓRIO • VOZES RETIDAS" };
    private readonly string[] weaponNames = { "ESPADA COMUM", "LÂMINA RARA", "LÂMINA DO CREPÚSCULO" };
    private readonly string[] charmNames = { "", "NÓ DE BRASA", "FIO DE TRAVESSIA", "VIDRO DE VIGÍLIA" };
    private readonly string[] prologue = {
        "Na noite sem sinos, Véspera começou a esquecer os nomes de seus mortos. Sob as ruas, o Rio Cego repetiu uma voz que ninguém mais conseguia ouvir.",
        "Você é um Escutador: ouve as lembranças que a pedra não conseguiu guardar em silêncio. Entre elas reconheceu Nara, sua irmã, desaparecida quando os sinos pararam.",
        "A memória dela chega partida: “Desça. Ele ainda lembra.” O Guardião sem Nome espera na cripta, diante da única pista que pode explicar por que Nara foi até lá."
    };
    private readonly int[] weaponDamage = { 0, 10, 4 };
    private PointF player = new(150, 330), facing = new(1, 0), mouseAim = new(1, 0);
    private int room, level = 1, xp, gold, potions = 2, damage = 25, weapon, ownedWeapons = 1;
    private int healthTalent, skillTalent, dodgeTalent, pendingTalents, enemiesDefeated;
    private int memoriesFound, questStage, clearedRooms;
    private int ownedCharms, equippedCharm;
    private bool archiveSeal;
    private bool riverBell;
    private bool lumeLens;
    private bool naraTrace;
    private float health = 100, maxHealth = 100, attackCd, skillCd, dashCd, dashTime, invulnerable, noticeTime, windupPulse;
    private bool armor, armorOn, relic, relicOn, inventoryOpen, title = true, dead, victory, talentChoice, chestChoice, storyOverlay, memoryOverlay, endingChoiceOpen;
    private int townEnding;
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
        armor = armorOn = relic = relicOn = archiveSeal = riverBell = lumeLens = naraTrace = inventoryOpen = dead = victory = chestChoice = talentChoice = memoryOverlay = endingChoiceOpen = false;
        townEnding = 0;
        healthTalent = skillTalent = dodgeTalent = pendingTalents = enemiesDefeated = memoriesFound = questStage = 0;
        clearedRooms = 0;
        ownedCharms = equippedCharm = 0;
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
        if (room != 3 && (clearedRooms & (1 << room)) != 0) return;
        int count = room == 3 ? 0 : room == 0 ? 3 : room == 1 ? 5 : room == 4 ? 5 : room == 5 ? 4 : room == 6 ? 6 : (room is 7 or 9 or 11) ? 4 : room == 8 ? 5 : room == 10 ? 6 : 4;
        for (int i = 0; i < count; i++)
        {
            EnemyKind kind = room == 2 && i == 0 ? EnemyKind.Guardian : room == 5 && i == 0 ? EnemyKind.BlindScribe : room == 7 && i == 0 ? EnemyKind.RiverFerryman : room == 9 && i == 0 ? EnemyKind.AshWarden : room == 11 && i == 0 ? EnemyKind.HollowName : room == 1 && i == count - 1 ? EnemyKind.Elite : room == 4 && (i is 1 or 4) ? EnemyKind.InkEcho : room == 5 && i == 2 ? EnemyKind.InkEcho : room == 6 && (i is 1 or 4) ? EnemyKind.DrownedMemory : room == 7 && i == 2 ? EnemyKind.DrownedMemory : room == 8 && (i is 1 or 4) ? EnemyKind.EmberWisp : room == 9 && i == 2 ? EnemyKind.EmberWisp : room == 10 && (i is 1 or 4) ? EnemyKind.NameMoth : room == 11 && i == 2 ? EnemyKind.NameMoth : room > 0 && i == 1 ? EnemyKind.Archer : (room is 2 or 5 or 7 or 9 or 11) && i == 2 ? EnemyKind.Mage : EnemyKind.Grunt;
            float hp = kind switch { EnemyKind.Guardian => 160, EnemyKind.BlindScribe => 245, EnemyKind.RiverFerryman => 285, EnemyKind.AshWarden => 340, EnemyKind.HollowName => 390, EnemyKind.Elite => 120, EnemyKind.Archer or EnemyKind.Mage or EnemyKind.InkEcho or EnemyKind.DrownedMemory or EnemyKind.EmberWisp or EnemyKind.NameMoth => 56, _ => 58 + room * 8 };
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

    private void ChooseTownEnding(int choice)
    {
        if (questStage < 12 || townEnding != 0 || choice is < 1 or > 2) return;
        townEnding = choice; questStage = 13; endingChoiceOpen = false;
        if (choice == 1)
        {
            maxHealth += 20; health = maxHealth;
            ShowMemory("VÉSPERA DEVOLVE OS NOMES", "Sileth abre os arquivos e lê cada nome em voz alta. Os sinos respondem um a um. O Lume enfraquece, e o Silêncio volta a se mover sob o rio; ninguém mais enfrentará essa ameaça sozinho. Edda promete manter os nomes nos sinos. A praça oferece abrigo e coragem: vida máxima +20.");
        }
        else
        {
            damage += 10;
            ShowMemory("VÉSPERA MANTÉM O LUME", "Sileth conserva a barreira e registra o preço sem apagar o que foi perdido. Edda toca os sinos para que a cidade não confunda proteção com esquecimento. A luz do Lume marca a arma do Escutador: dano +10. Os arquivos ficam abertos, mesmo sob a luz antiga.");
        }
        notice = "DECISÃO DE VÉSPERA REGISTRADA • SALVE COM F5"; noticeTime = 4;
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
        if (endingChoiceOpen) { if (e.KeyCode == Keys.D1) ChooseTownEnding(1); else if (e.KeyCode == Keys.D2) ChooseTownEnding(2); else if (e.KeyCode == Keys.D3) endingChoiceOpen = false; return; }
        if (chestChoice) { if (e.KeyCode == Keys.D1) gold += 75; else if (e.KeyCode == Keys.D2) potions += 2; else if (e.KeyCode == Keys.D3) health = maxHealth; else return; chestChoice = false; EnterVesper(); return; }
        if (talentChoice) { SelectTalent(e.KeyCode); return; }
        if (e.KeyCode == Keys.I) { inventoryOpen = !inventoryOpen; return; }
        if (inventoryOpen) { if (e.KeyCode >= Keys.D1 && e.KeyCode <= Keys.D3) EquipWeapon((int)e.KeyCode - (int)Keys.D1); else if (e.KeyCode == Keys.D4 && armor) armorOn = !armorOn; else if (e.KeyCode == Keys.D5 && relic) relicOn = !relicOn; else if (e.KeyCode == Keys.D6) CycleCharm(); return; }
        if (e.KeyCode == Keys.Space || e.KeyCode == Keys.J) Attack();
        else if ((e.KeyCode == Keys.ShiftKey || e.KeyCode == Keys.K) && dashCd <= 0) { dashTime = DashDuration(); dashCd = DashCooldown(); }
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
        attackCd = BasicAttackCooldown();
        foreach (Enemy enemy in enemies.ToArray())
        {
            PointF offset = Sub(enemy.Pos, player); float distance = Length(offset);
            if (distance > (enemy.Boss ? 95 : 78) || (distance >= 20 && Dot(facing, Normalize(offset, facing)) <= 0.1f) || BlockedLine(player, enemy.Pos)) continue;
            bool critical = random.NextDouble() < CriticalChance(); int dealt = (int)(damage * (critical ? CriticalMultiplier() : 1)); if (enemy.Armored) dealt = (int)(dealt * 0.75f);
            enemy.Hp -= dealt; enemy.Flash = 0.2f; if (enemy.Hp <= 0) Defeat(enemy);
        }
    }

    private void Whirlwind()
    {
        if (skillCd > 0 || talentChoice) return;
        skillCd = Math.Max(1.5f, 6f - skillTalent * 0.5f) + (equippedCharm == 1 ? 1f : 0f);
        foreach (Enemy enemy in enemies.ToArray()) if (Distance(player, enemy.Pos) < 110) { enemy.Hp -= (int)(damage * WhirlwindMultiplier() * (enemy.Armored ? 0.75f : 1)); if (enemy.Hp <= 0) Defeat(enemy); }
    }

    private void Defeat(Enemy enemy)
    {
        if (!enemies.Remove(enemy)) return;
        enemiesDefeated++; drops.Add(new Drop { Pos = enemy.Pos, Kind = "gold", Amount = enemy.Boss ? 35 : enemy.Kind == EnemyKind.Elite ? 20 : 8 });
        AddXp(enemy.Boss ? 75 : enemy.Kind == EnemyKind.Elite ? 40 : 22);
        if (enemy.Kind == EnemyKind.Guardian && !relic) drops.Add(new Drop { Pos = Offset(enemy.Pos, -24, 0), Kind = "relic" });
        if (enemy.Kind == EnemyKind.BlindScribe && !archiveSeal) drops.Add(new Drop { Pos = Offset(enemy.Pos, -24, 0), Kind = "seal" });
        if (enemy.Kind == EnemyKind.RiverFerryman && !riverBell) drops.Add(new Drop { Pos = Offset(enemy.Pos, -24, 0), Kind = "bell" });
        if (enemy.Kind == EnemyKind.AshWarden && !lumeLens) drops.Add(new Drop { Pos = Offset(enemy.Pos, -24, 0), Kind = "lens" });
        if (enemy.Kind == EnemyKind.HollowName && !naraTrace) drops.Add(new Drop { Pos = Offset(enemy.Pos, -24, 0), Kind = "trace" });
        if (enemy.Kind == EnemyKind.Elite && (ownedCharms & 1) == 0) drops.Add(new Drop { Pos = Offset(enemy.Pos, 24, -12), Kind = "charm", Amount = 1 });
        if (enemy.Kind == EnemyKind.RiverFerryman && (ownedCharms & 2) == 0) drops.Add(new Drop { Pos = Offset(enemy.Pos, -24, -12), Kind = "charm", Amount = 2 });
        if (enemy.Kind == EnemyKind.BlindScribe && (ownedCharms & 4) == 0) drops.Add(new Drop { Pos = Offset(enemy.Pos, -24, -12), Kind = "charm", Amount = 3 });
        if (!enemy.Boss && enemy.Kind != EnemyKind.Elite && random.NextDouble() < 0.06)
            drops.Add(new Drop { Pos = Offset(enemy.Pos, 18, 14), Kind = "charm", Amount = random.Next(1, 4) });
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
        if (title || dead || victory || storyOverlay || memoryOverlay || inventoryOpen || talentChoice || chestChoice || endingChoiceOpen) { Invalidate(); return; }
        PointF dir = new((down.Contains(Keys.D) || down.Contains(Keys.Right) ? 1 : 0) - (down.Contains(Keys.A) || down.Contains(Keys.Left) ? 1 : 0), (down.Contains(Keys.S) || down.Contains(Keys.Down) ? 1 : 0) - (down.Contains(Keys.W) || down.Contains(Keys.Up) ? 1 : 0));
        dir = Normalize(dir, PointF.Empty); if (dashTime > 0) dir = facing;
        MovePlayer(Scale(dir, (dashTime > 0 ? 530 : 190) * dt));
        for (int i = drops.Count - 1; i >= 0; i--) if (Distance(player, drops[i].Pos) < 30) Collect(drops[i]);
        foreach (Enemy enemy in enemies.ToArray()) { UpdateEnemy(enemy, dt); if (dead) break; }
        if (enemies.Count == 0 && drops.Count == 0 && room == 2 && !memoryOverlay) chestChoice = true;
        MarkRoomCleared();
        Invalidate();
    }

    private void UpdateEnemy(Enemy e, float dt)
    {
        e.Cooldown -= dt; e.Flash = Math.Max(0, e.Flash - dt); PointF offset = Sub(player, e.Pos); float dist = Length(offset);
        if (e.Windup > 0) {
            e.Windup -= dt;
            float reach = e.Boss ? e.BossPattern == 0 ? 90 : e.Kind == EnemyKind.RiverFerryman && e.BossPattern == 2 ? 85 : e.Kind == EnemyKind.AshWarden && e.BossPattern == 2 ? 68 : e.Kind == EnemyKind.HollowName && e.BossPattern == 2 ? 85 : 42 : e.Kind == EnemyKind.Elite ? 62 : e.Kind == EnemyKind.Mage ? 52 : e.Kind is EnemyKind.Archer or EnemyKind.InkEcho ? 230 : e.Kind == EnemyKind.DrownedMemory ? 195 : e.Kind is EnemyKind.EmberWisp or EnemyKind.NameMoth ? 210 : 38;
            bool targeted = (e.Boss && (e.BossPattern is 1 or 2)) || (e.Kind is EnemyKind.Mage or EnemyKind.Elite or EnemyKind.Archer or EnemyKind.InkEcho or EnemyKind.DrownedMemory or EnemyKind.EmberWisp or EnemyKind.NameMoth);
            if (e.Windup <= 0 && (targeted ? Distance(player, e.Target) < reach : dist < reach) && invulnerable <= 0 && dashTime <= 0) HitPlayer(e.Boss ? e.Kind == EnemyKind.BlindScribe ? 23 : e.Kind == EnemyKind.RiverFerryman ? 28 : e.Kind == EnemyKind.AshWarden ? 31 : e.Kind == EnemyKind.HollowName ? 36 : 26 : e.Kind == EnemyKind.Elite ? 20 : e.Kind == EnemyKind.Mage ? 16 : e.Kind is EnemyKind.InkEcho or EnemyKind.DrownedMemory or EnemyKind.EmberWisp or EnemyKind.NameMoth ? 16 : 10);
            return;
        }
        if (e.Kind is EnemyKind.Archer or EnemyKind.InkEcho) { float preferred = e.Kind == EnemyKind.InkEcho ? 135 : 170; if (dist < preferred) e.Pos = MoveEnemy(e, Scale(Normalize(Sub(e.Pos, player), PointF.Empty), (e.Kind == EnemyKind.InkEcho ? 68 : 55) * dt)); else if (e.Cooldown <= 0) { BeginWindup(e, player, e.Kind == EnemyKind.InkEcho ? 0.75f : 0.6f); e.Cooldown = e.Kind == EnemyKind.InkEcho ? 2.0f : 1.8f; } SeparateFromPack(e, dt); return; }
        if (e.Kind is EnemyKind.DrownedMemory or EnemyKind.EmberWisp or EnemyKind.NameMoth) { float preferred = e.Kind == EnemyKind.EmberWisp ? 145 : e.Kind == EnemyKind.NameMoth ? 175 : 110; if (dist < preferred) e.Pos = MoveEnemy(e, Scale(Normalize(Sub(e.Pos, player), PointF.Empty), 72 * dt)); else if (e.Cooldown <= 0) { BeginWindup(e, player, e.Kind == EnemyKind.EmberWisp ? 0.7f : e.Kind == EnemyKind.NameMoth ? 0.62f : 0.9f); e.Cooldown = e.Kind == EnemyKind.EmberWisp ? 1.9f : e.Kind == EnemyKind.NameMoth ? 1.7f : 2.25f; } SeparateFromPack(e, dt); return; }
        if (e.Kind == EnemyKind.Mage && dist < 165) e.Pos = MoveEnemy(e, Scale(Normalize(Sub(e.Pos, player), PointF.Empty), 42 * dt));
        if (e.Kind == EnemyKind.Elite && e.Cooldown <= 0 && dist < 220) { BeginWindup(e, player, e.Armored ? 0.72f : 0.5f); e.Cooldown = e.Armored ? 2.5f : 2f; return; }
        if (e.Boss && e.Cooldown <= 0 && (dist < 135 || (e.Kind is EnemyKind.BlindScribe or EnemyKind.RiverFerryman or EnemyKind.AshWarden or EnemyKind.HollowName))) { e.BossPattern = (e.Kind is EnemyKind.RiverFerryman or EnemyKind.AshWarden or EnemyKind.HollowName) ? (e.BossPattern + 1) % 3 : 1 - e.BossPattern; bool aimed = (e.Kind == EnemyKind.BlindScribe && e.BossPattern == 1) || ((e.Kind is EnemyKind.RiverFerryman or EnemyKind.AshWarden) && (e.BossPattern is 1 or 2)) || (e.Kind == EnemyKind.HollowName && e.BossPattern is 1 or 2); BeginWindup(e, aimed ? player : e.Pos, e.Hp < e.MaxHp * 0.5f ? 0.52f : 0.78f); e.Cooldown = e.Hp < e.MaxHp * 0.5f ? 1.25f : 2.0f; return; }
        if (e.Kind == EnemyKind.Mage && e.Cooldown <= 0) { BeginWindup(e, player, 0.8f); e.Cooldown = 2.2f; SeparateFromPack(e, dt); return; }
        if (dist > (e.Boss ? 40 : 30)) e.Pos = MoveEnemy(e, Scale(Normalize(offset, PointF.Empty), (e.Boss ? 55 : 74 + room * 7) * dt));
        else if (e.Cooldown <= 0) { BeginWindup(e, player, 0.45f); e.Cooldown = 1.35f; }
        if (!e.Boss) SeparateFromPack(e, dt);
    }

    private static void BeginWindup(Enemy enemy, PointF target, float duration) { enemy.Target = target; enemy.Windup = enemy.WindupMax = duration; }
    private void HitPlayer(float value) { health = Math.Max(0, health - value * (armorOn ? 0.7f : 1)); invulnerable = 0.6f; if (health <= 0) dead = true; }
    private void Collect(Drop item)
    {
        switch (item.Kind) { case "gold": gold += item.Amount; notice = $"+{item.Amount} OURO"; break; case "potion": potions++; notice = "+1 POÇÃO"; break; case "weapon": ownedWeapons |= 1 << item.Amount; notice = $"ARMA: {weaponNames[item.Amount]}"; break; case "charm": if (item.Amount is < 1 or > 3) { drops.Remove(item); return; } bool newCharm = (ownedCharms & (1 << (item.Amount - 1))) == 0; ownedCharms |= 1 << (item.Amount - 1); if (equippedCharm == 0) equippedCharm = item.Amount; notice = newCharm ? $"TALISMÃ: {charmNames[item.Amount]}" : $"ECO DUPLICADO: {charmNames[item.Amount]}"; break; case "armor": armor = true; notice = "PEITORAL DE PEDRA ENCONTRADO"; break; case "relic": relic = true; notice = "FRAGMENTO DE VOZ ENCONTRADO"; ShowMemory("ECO PRESO NO RELICÁRIO", "Nara: “Eu vim por vontade própria. Se encontrou esta lembrança, procure o arquivo de Sileth.”"); break; case "seal": archiveSeal = true; damage += 8; questStage = 4; notice = "SELO DA VIGÍLIA • DANO +8"; ShowMemory("SELO DA VIGÍLIA", "O selo guarda a última gravação de Nara: “Sileth não apagou os nomes para alimentar o Lume. Ela tentou esconder que o próprio farol está aprendendo a escolher quem esquecer.” Leve esta prova de volta à Curadora."); break; case "bell": riverBell = true; maxHealth += 20; health = Math.Min(maxHealth, health + 20); questStage = 6; notice = "SINO DE NARA • VIDA MÁXIMA +20"; ShowMemory("O SINO SEM BADALO", "A marca no bronze pertence a Nara. A voz gravada é de uma mulher que atravessou o rio: “Não estou presa. O farol me escuta porque eu aceitei levar as memórias para longe da cidade. Se ainda me procura, não tente me trazer de volta antes de ouvir o que o rio guarda.”"); break; case "lens": lumeLens = true; damage += 12; questStage = 9; notice = "PRISMA DO LUME • DANO +12"; ShowMemory("O PRISMA DA PRIMEIRA VIGÍLIA", "A luz projeta Nara ao lado de Sileth, anos antes da noite dos sinos. Nara escolheu ser a portadora das lembranças que o Lume não conseguia guardar. A imagem se rompe antes de mostrar onde ela está agora."); break; case "trace": naraTrace = true; questStage = 11; notice = "RASTRO DE NARA • NOVA MEMÓRIA"; ShowMemory("A LEMBRANÇA QUE SOBROU", "O eco não mostra um rosto. Mostra Nara ensinando alguém, do outro lado do rio, a pronunciar um nome que Véspera apagou. Sua última frase não pede resgate: “Se a cidade ainda lembrar de nós, eu saberei voltar.”"); break; }
        drops.Remove(item); noticeTime = 1.7f;
    }

    private void UsePotion() { if (potions > 0 && health < maxHealth) { potions--; health = Math.Min(maxHealth, health + 45); notice = "+45 VIDA"; noticeTime = 1.3f; } else { notice = potions == 0 ? "SEM POÇÕES" : "VIDA JÁ ESTÁ CHEIA"; noticeTime = 1.3f; } }
    private void Interact()
    {
        if (room == 3) { InteractTown(); return; }
        if (enemies.Count != 0) { notice = "DERROTE OS INIMIGOS PRIMEIRO"; noticeTime = 1.2f; return; }
        if (drops.Count != 0) { notice = "RECOLHA O LOOT ANTES DE AVANÇAR"; noticeTime = 1.5f; return; }
        MarkRoomCleared();
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
        else if (room == 8 && player.X < 115) { room = 3; SetupRoom(); }
        else if (room == 8 && player.X > 790) { room = 9; SetupRoom(); ShowMemory("CÂMARA DO LUME", "O farol está aceso, mas sua luz não se espalha: ela converge para um prisma suspenso sobre um poço sem fundo."); }
        else if (room == 9 && player.X < 115) { room = 8; SetupRoom(); }
        else if (room == 9 && player.X > 790) { room = 3; SetupRoom(); ShowMemory("REGRESSO À PRAÇA", "A imagem de Nara no prisma não esclarece se ela ainda pode ser encontrada. Sileth precisa ver o que você descobriu."); }
        else if (room == 9 && questStage >= 10 && Distance(player, new PointF(480, 500)) < 64) { room = 10; SetupRoom(); ShowMemory("A MARGEM SEM REFLEXO", "Atrás do prisma existe um leito que não aparece em nenhum mapa. As pedras guardam vozes recusadas pelo Lume, mas nenhuma delas repete o nome de Nara."); }
        else if (room == 10 && player.X < 115) { room = 9; SetupRoom(); }
        else if (room == 10 && player.X > 790) { room = 11; SetupRoom(); ShowMemory("RESERVATÓRIO DAS VOZES", "As lembranças recusadas convergem para um poço. Algo se move entre elas, usando nomes esquecidos como armadura."); }
        else if (room == 11 && player.X < 115) { room = 10; SetupRoom(); }
        else if (room == 11 && player.X > 790 && naraTrace) { room = 3; SetupRoom(); ShowMemory("REGRESSO A VÉSPERA", "Você carrega uma lembrança que o Lume não conseguiu apagar. Sileth precisa ouvi-la antes de decidir o que fazer com os arquivos."); }
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
            else if (questStage == 9) { questStage = 10; gold += 120; potions++; ShowMemory("O NOME QUE O LUME GUARDA", "O prisma confirma que Nara aceitou ser portadora das lembranças recusadas pelo Lume. Os arquivos não dizem se ela está viva, morta ou fundida à luz. Não vamos decidir seu destino com base em mais uma página incompleta. Reúna as memórias que faltam; quando chegar a hora, Véspera precisa escolher sabendo o preço."); notice = "MISSÃO CONCLUÍDA: 120 OURO E 1 POÇÃO"; noticeTime = 3; }
            else if (questStage == 11 && naraTrace) { questStage = 12; gold += 150; potions++; ShowMemory("A VOZ DE QUEM AINDA LEMBRA", "Nara deixou uma memória fora do alcance do farol. Ela ensinava os esquecidos a dizer os nomes uns dos outros, sem pedir que alguém a salvasse. Sileth: “Passei a vida escolhendo o que Véspera podia lembrar. Agora a cidade terá de escolher por si. Ainda faltam vozes; não vou fechar o arquivo outra vez.”"); notice = "MISSÃO CONCLUÍDA: 150 OURO E 1 POÇÃO"; noticeTime = 3; }
            else if (questStage >= 12 && townEnding == 0) { endingChoiceOpen = true; notice = "VÉSPERA ESPERA SUA DECISÃO • 1 OU 2"; noticeTime = 3; }
            else if (townEnding == 1) ShowMemory("CURADORA SILETH", "Os nomes voltaram aos sinos. A praça está com você, mas as vozes além do reservatório ainda faltam. Vamos procurá-las sem fechar o arquivo outra vez.");
            else if (townEnding == 2) ShowMemory("CURADORA SILETH", "O Lume continua aceso e os arquivos permanecem abertos. O Silêncio não desapareceu; siga o rastro para além do reservatório antes que a cidade dependa de novo de nomes apagados.");
            else ShowMemory("CURADORA SILETH", "O rastro de Nara segue pelo leito do Rio Cego. Com o sino de Edda, a passagem abaixo da capela pode ser aberta.");
            return;
        }
        if (Distance(player, BellKeeperPosition) < 58)
        {
            if (questStage == 6 && riverBell) { questStage = 7; gold += 100; potions++; ShowMemory("EDDA RECONHECE O SINO", "Esse era o sino de travessia do meu pai. Nara o levou quando desceu ao rio; ela conhecia o caminho e decidiu cruzá-lo. Não foi capturada. Guarde o sino: se ela voltar, vai saber que ainda lembramos seu nome."); notice = "MISSÃO CONCLUÍDA: 100 OURO E 1 POÇÃO"; noticeTime = 3; }
            else if (townEnding == 1) ShowMemory("EDDA, TOCADORA DE SINOS", "Os nomes voltaram às badaladas. Vou ensinar as crianças a ler cada sino, para que ninguém precise adivinhar quem foi esquecido."); else if (townEnding == 2) ShowMemory("EDDA, TOCADORA DE SINOS", "Os sinos agora marcam as horas do Lume. Não vou deixar que a luz transforme proteção em silêncio outra vez."); else ShowMemory("EDDA, TOCADORA DE SINOS", "Os sinos continuam inteiros. O que sumiu foram os nomes gravados neles. Quando tento lembrar quem os fundiu, só escuto água debaixo da praça.");
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
            if (questStage >= 7) { room = 8; SetupRoom(); ShowMemory("ESCADA DE CINZAS", "O sino abre uma passagem abaixo da Capela Afogada. A cada degrau, o ar fica mais quente e as lembranças parecem chegar antes de você."); }
            else if (questStage >= 5) { room = 6; SetupRoom(); ShowMemory("LEITO DO RIO CEGO", "A água desapareceu da superfície, mas as pedras ainda guardam as últimas palavras de quem cruzou o rio. O sino de Nara aponta para baixo da corrente."); }
            else ShowMemory("PASSAGEM DO RIO", "Edda não reconhece o caminho para o leito seco. Conclua sua investigação com Sileth antes de descer.");
            return;
        }
        if (Distance(player, CryptGatePosition) < 58)
        {
            room = 2; SetupRoom(); ShowMemory("A CRIPTA", "A passagem continua aberta. O Guardião protege o relicário, mas a próxima pista está nos arquivos de Véspera.");
        }
    }
    private void ShowMemory(string titleText, string bodyText) { memoryTitle = titleText; memoryText = bodyText; memoryOverlay = true; }
    private void MarkRoomCleared() { if (room != 3 && enemies.Count == 0 && drops.Count == 0) clearedRooms |= 1 << room; }
    private Enemy? lockedTarget;
    private void CycleTarget() { if (enemies.Count == 0) { lockedTarget = null; return; } int i = lockedTarget is null ? -1 : enemies.IndexOf(lockedTarget); lockedTarget = enemies[(i + 1) % enemies.Count]; notice = $"ALVO: {EnemyName(lockedTarget)}"; noticeTime = 1.4f; }
    private void EquipWeapon(int index) { if ((ownedWeapons & (1 << index)) == 0) { notice = "ARMA AINDA NÃO ENCONTRADA"; noticeTime = 1.4f; return; } damage += weaponDamage[index] - weaponDamage[weapon]; weapon = index; }
    private void CycleCharm()
    {
        for (int i = 1; i <= 3; i++)
        {
            int candidate = (equippedCharm + i) % 4;
            if (candidate == 0 || (ownedCharms & (1 << (candidate - 1))) == 0) continue;
            equippedCharm = candidate; notice = $"TALISMÃ EQUIPADO: {charmNames[candidate]}"; noticeTime = 1.6f; return;
        }
        equippedCharm = 0; notice = "NENHUM TALISMÃ ENCONTRADO"; noticeTime = 1.6f;
    }
    private float WhirlwindMultiplier() => equippedCharm == 1 ? 2.16f : 1.6f;
    private float DashDuration() => equippedCharm == 2 ? 0.29f : 0.17f;
    private float DashCooldown() => Math.Max(0.35f, 1.15f - dodgeTalent * 0.12f) + (equippedCharm == 2 ? 0.3f : 0f);
    private float CriticalChance() => (weapon == 2 ? 0.25f : 0.12f) + (equippedCharm == 3 ? 0.12f : 0f);
    private float CriticalMultiplier() => equippedCharm == 3 ? 2.5f : 2f;
    private float BasicAttackCooldown() => (weapon == 2 ? 0.23f : 0.33f) + (equippedCharm == 3 ? 0.06f : 0f);
    private PointF MoveEnemy(Enemy e, PointF delta) { PointF p = e.Pos; PointF x = new(p.X + delta.X, p.Y), y = new(p.X, p.Y + delta.Y); if (!Blocked(x, e.Radius)) p.X = x.X; if (!Blocked(new PointF(p.X, y.Y), e.Radius)) p.Y = y.Y; return new PointF(Math.Clamp(p.X, 85, 875), Math.Clamp(p.Y, 128, 533)); }
    private void SeparateFromPack(Enemy enemy, float dt) { PointF push = PointF.Empty; foreach (Enemy other in enemies) { if (ReferenceEquals(enemy, other)) continue; PointF away = Sub(enemy.Pos, other.Pos); float d = Length(away); if (d > 0.01f && d < 40) push = Add(push, Scale(away, (40 - d) / (40 * d))); } if (Length(push) > 0.01f) enemy.Pos = MoveEnemy(enemy, Scale(Normalize(push, PointF.Empty), 52 * dt)); }
    private void MovePlayer(PointF delta) { PointF x = new(player.X + delta.X, player.Y), y = new(player.X, player.Y + delta.Y); if (!Blocked(x)) player.X = x.X; if (!Blocked(new PointF(player.X, y.Y))) player.Y = y.Y; player.X = Math.Clamp(player.X, 85, 875); player.Y = Math.Clamp(player.Y, 128, 533); }
    private bool Blocked(PointF p, float radius = 12) { RectangleF body = new(p.X - radius, p.Y - radius, radius * 2, radius * 2); RectangleF[] obstacles = room == 3 ? TownBuildings : room == 4 ? ArchiveWalls : room == 5 ? VaultWalls : room == 6 ? RiverWalls : room == 7 ? BellChamberWalls : room == 8 ? AshGalleryWalls : room == 9 ? LumeChamberWalls : room == 10 ? NameShoreWalls : room == 11 ? VoiceWellWalls : Walls; return obstacles.Any(w => w.IntersectsWith(body)); }
    private bool BlockedLine(PointF a, PointF b)
    {
        int steps = Math.Max(1, (int)(Distance(a, b) / 6));
        RectangleF[] obstacles = room == 4 ? ArchiveWalls : room == 5 ? VaultWalls : room == 6 ? RiverWalls : room == 7 ? BellChamberWalls : room == 8 ? AshGalleryWalls : room == 9 ? LumeChamberWalls : room == 10 ? NameShoreWalls : room == 11 ? VoiceWellWalls : Walls;
        for (int i = 0; i <= steps; i++) { float t = i / (float)steps; PointF p = new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t); if (obstacles.Any(w => w.Contains(p))) return true; }
        return false;
    }

    private SaveData CaptureSaveData() => new() { Room = room, Level = level, Xp = xp, Gold = gold, Potions = potions, Damage = damage, Health = health, MaxHealth = maxHealth, Weapon = weapon, OwnedWeapons = ownedWeapons, Armor = armor, Relic = relic, HealthTalent = healthTalent, SkillTalent = skillTalent, DodgeTalent = dodgeTalent, RoomCleared = room == 3 || enemies.Count == 0 && drops.Count == 0, Completed = victory, MemoriesFound = memoriesFound, QuestStage = questStage, ClearedRooms = clearedRooms | (room != 3 && enemies.Count == 0 && drops.Count == 0 ? 1 << room : 0), ArchiveSeal = archiveSeal, RiverBell = riverBell, LumeLens = lumeLens, NaraTrace = naraTrace, OwnedCharms = ownedCharms, EquippedCharm = equippedCharm, TownEnding = townEnding };
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
            if (s.Room is < 0 or > 11 || s.Level is < 1 or > 10000 || s.Xp < 0 || s.Xp >= s.Level * 60 || s.Weapon is < 0 or > 2 || s.OwnedWeapons < 1 || s.OwnedWeapons > 7 || (s.OwnedWeapons & (1 << s.Weapon)) == 0 || s.MaxHealth < 1 || s.Health <= 0 || s.Health > s.MaxHealth || s.Potions < 0 || s.Gold < 0 || s.Damage < 1 || s.MemoriesFound is < 0 or > 3 || s.QuestStage is < 0 or > 13 || s.ClearedRooms is < 0 or > 4095 || s.Completed && s.Room != 2 || s.OwnedCharms is < 0 or > 7 || s.EquippedCharm is < 0 or > 3 || s.EquippedCharm > 0 && (s.OwnedCharms & (1 << (s.EquippedCharm - 1))) == 0 || s.TownEnding is < 0 or > 2 || s.QuestStage == 13 && s.TownEnding == 0) throw new InvalidDataException();
            StartGame(); room = s.Room; level = s.Level; xp = s.Xp; gold = s.Gold; potions = s.Potions; damage = s.Damage; health = s.Health; maxHealth = s.MaxHealth; weapon = s.Weapon; ownedWeapons = s.OwnedWeapons; armor = s.Armor; relic = s.Relic; healthTalent = s.HealthTalent; skillTalent = s.SkillTalent; dodgeTalent = s.DodgeTalent; memoriesFound = s.MemoriesFound; questStage = s.QuestStage; clearedRooms = s.ClearedRooms | (s.RoomCleared && s.Room != 3 ? 1 << s.Room : 0); archiveSeal = s.ArchiveSeal; riverBell = s.RiverBell; lumeLens = s.LumeLens; naraTrace = s.NaraTrace; ownedCharms = s.OwnedCharms; equippedCharm = s.EquippedCharm; townEnding = s.TownEnding; SetupRoom(); if (s.RoomCleared) enemies.Clear(); victory = s.Completed; title = false; storyOverlay = false;
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
        else if (room is 6 or 7) DrawRiverEnvironment(g, floor, tile);
        else if (room >= 10) DrawNameShoreEnvironment(g, floor, tile);
        else if (room >= 8) DrawLumeEnvironment(g, floor, tile);
        else
        {
            g.FillRectangle(floor, 65, 108, 830, 445); for (int y = 0; y < 7; y++) for (int x = 0; x < 12; x++) g.DrawRectangle(tile, 70 + x * 68, 112 + y * 62, 67, 61);
            using var wallBrush = new SolidBrush(Color.FromArgb(90, 79, 62)); using var wallEdge = new Pen(Color.FromArgb(154, 132, 91), 3); foreach (RectangleF wall in Walls) { g.FillRectangle(wallBrush, wall); g.DrawRectangle(wallEdge, wall.X, wall.Y, wall.Width, wall.Height); }
            using var exitPen = new Pen(enemies.Count == 0 && drops.Count == 0 ? Color.LightGreen : Color.FromArgb(45, 48, 43), 8); g.DrawRectangle(exitPen, 882, 293, 3, 75);
        }
        foreach (Enemy enemy in enemies) DrawEnemy(g, enemy);
        foreach (Drop item in drops) DrawDrop(g, item);
        DrawPlayer(g);
        g.DrawString(room == 3 ? roomNames[room] : room < 3 ? $"{room + 1} / 3   {roomNames[room]}" : room < 6 ? $"{room - 3} / 2   {roomNames[room]}" : room < 8 ? $"{room - 5} / 2   {roomNames[room]}" : room < 10 ? $"{room - 7} / 2   {roomNames[room]}" : $"{room - 9} / 2   {roomNames[room]}", roomFont, goldBrush, 67, 572);
        int memoriesRead = (memoriesFound & 1) + ((memoriesFound >> 1) & 1);
        g.DrawString($"ECOS {memoriesRead} / 2", hudFont, goldBrush, 760, 572);
        if (room == 3) g.DrawString(TownObjective(), hudFont, goldBrush, 67, 592);
        else if (room >= 4 && room < 6) g.DrawString(ArchiveObjective(), hudFont, goldBrush, 67, 592);
        else if (room is 6 or 7) g.DrawString(RiverObjective(), hudFont, goldBrush, 67, 592);
        else if (room >= 8) g.DrawString(LumeObjective(), hudFont, goldBrush, 67, 592);
        g.DrawString(room == 3 ? "WASD mover   E falar/investigar   F5 salvar   I inventário" : "WASD mover   ESPAÇO/CLIQUE atacar   SHIFT esquivar   R redemoinho   Q poção   E interagir   I inventário", helpFont, textBrush, 67, 619);
        if (room < MemoryStonePositions.Length && (memoriesFound & (1 << room)) == 0) DrawMemoryStone(g, MemoryStonePositions[room], Distance(player, MemoryStonePositions[room]) < 58);
        if (noticeTime > 0) DrawCentered(g, notice, 170, new Font("Segoe UI", 13, FontStyle.Bold), Color.FromArgb(230, 193, 129));
        if (title || dead || victory) DrawOverlay(g, title ? "SCADRYK" : victory ? "VITÓRIA" : "VOCÊ CAIU", title ? "ENTER PARA COMEÇAR    •    C CARREGAR SAVE" : "ENTER PARA RECOMEÇAR    •    F5 SALVAR");
        if (inventoryOpen) DrawInventory(g);
        if (talentChoice) DrawChoice(g, $"SUBIU DE NÍVEL — ESCOLHA ({pendingTalents} PENDENTES)", "1. VIGOR  +20 VIDA MÁXIMA", "2. DOMÍNIO  Redemoinho recarrega mais rápido", "3. AGILIDADE  esquiva recarrega mais rápido");
        if (chestChoice) DrawChoice(g, "BAÚ DA CRIPTA — ESCOLHA UMA RECOMPENSA", "1. 75 OURO", "2. DUAS POÇÕES", "3. RECUPERAR TODA A VIDA");
        if (endingChoiceOpen) DrawChoice(g, "O QUE VÉSPERA DEVE LEMBRAR?", "1. DEVOLVER OS NOMES  •  +20 VIDA MÁXIMA", "2. MANTER O LUME  •  +10 DANO", "3. ADIAR A DECISÃO");
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
        7 => "OBJETIVO: DESÇA A ESCADA ABERTA PELO SINO",
        9 => "OBJETIVO: MOSTRE O PRISMA DO LUME A SILETH",
        10 => "OBJETIVO: DESÇA ALÉM DO PRISMA",
        11 => "OBJETIVO: MOSTRE A NOVA MEMÓRIA A SILETH",
        12 => "OBJETIVO: DECIDA COM SILETH O QUE VÉSPERA DEVE LEMBRAR",
        13 => townEnding == 1 ? "OS NOMES VOLTARAM • A BUSCA SEGUE ALÉM DO RESERVATÓRIO" : "O LUME PERMANECE • A BUSCA SEGUE ALÉM DO RESERVATÓRIO",
        _ => "PRÓXIMO DESTINO: REÚNA AS MEMÓRIAS QUE FALTAM"
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
        using var water = new SolidBrush(townEnding == 1 ? Color.FromArgb(123, 180, 153) : townEnding == 2 ? Color.FromArgb(183, 132, 77) : Color.FromArgb(88, 142, 145));
        g.FillEllipse(water, 458, 315, 60, 42); g.DrawEllipse(trim, 458, 315, 60, 42);
        if (townEnding != 0) DrawCentered(g, townEnding == 1 ? "OS NOMES VOLTARAM" : "O LUME PERMANECE", 402, new Font("Segoe UI", 9, FontStyle.Bold), townEnding == 1 ? Color.LightGreen : Color.PeachPuff);
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
        DrawCentered(g, questStage >= 7 ? "ESCADA DO LUME" : questStage >= 5 ? "RIO CEGO" : "LEITO SELADO", riverGate.Y - 4, new Font("Segoe UI", 8, FontStyle.Bold), Color.FromArgb(190, 211, 196), riverGate.X);
        if (Distance(player, riverGate) < 72) DrawCentered(g, "E  DESCER", riverGate.Y - 38, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, riverGate.X);
    }

    private string ArchiveObjective() => room == 4 ? "OBJETIVO: CRUZE A GALERIA E ENCONTRE O COFRE" : archiveSeal ? "OBJETIVO: VOLTE A VÉSPERA COM O SELO" : "OBJETIVO: RECUPERE O REGISTRO NO COFRE";
    private string RiverObjective() => room == 6 ? "OBJETIVO: SIGA A CORRENTE ATÉ A CAPELA" : riverBell ? "OBJETIVO: RETORNE E MOSTRE O SINO A EDDA" : "OBJETIVO: ENCONTRE O SINO DE TRAVESSIA";
    private string LumeObjective() => room == 8 ? "OBJETIVO: SIGA A ESCADA ATÉ A CÂMARA DO LUME" : room == 9 && questStage >= 10 && !naraTrace ? "OBJETIVO: INVESTIGUE A PASSAGEM SOB O PRISMA" : room == 10 ? "OBJETIVO: ATRAVESSE A MARGEM SEM REFLEXO" : room == 11 && !naraTrace ? "OBJETIVO: ENFRENTE O QUE DEVORA NOMES" : room >= 10 && naraTrace ? "OBJETIVO: LEVE O RASTRO DE NARA A SILETH" : lumeLens ? "OBJETIVO: RETORNE E MOSTRE O PRISMA A SILETH" : "OBJETIVO: RECUPERE O PRISMA DA PRIMEIRA VIGÍLIA";

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

    private void DrawLumeEnvironment(Graphics g, Brush ground, Pen tile)
    {
        using var ash = new SolidBrush(Color.FromArgb(43, 42, 40)); using var seam = new Pen(Color.FromArgb(96, 83, 64), 2); using var gold = new Pen(Color.FromArgb(171, 126, 67), 3);
        g.FillRectangle(ash, 65, 108, 830, 445);
        for (int y = 0; y < 7; y++) for (int x = 0; x < 12; x++) g.DrawRectangle(tile, 70 + x * 68, 112 + y * 62, 67, 61);
        RectangleF[] pillars = room == 8 ? AshGalleryWalls : LumeChamberWalls;
        using var pillarBrush = new SolidBrush(Color.FromArgb(73, 69, 59));
        foreach (RectangleF pillar in pillars) { g.FillRectangle(pillarBrush, pillar); g.DrawRectangle(gold, pillar.X, pillar.Y, pillar.Width, pillar.Height); }
        for (int i = 0; i < 5; i++) { float x = 170 + i * 150; g.DrawLine(seam, x, 115, x - 26, 212); g.DrawLine(seam, x - 26, 212, x + 14, 304); }
        if (room == 9)
        {
            using var glow = new SolidBrush(Color.FromArgb(42, 255, 178, 76)); using var core = new SolidBrush(Color.FromArgb(190, 248, 191, 104));
            g.FillEllipse(glow, 410, 205, 140, 140); g.FillEllipse(core, 462, 257, 36, 36); g.DrawEllipse(gold, 430, 225, 100, 100); g.DrawEllipse(gold, 445, 240, 70, 70);
        }
        using var gate = new Pen(enemies.Count == 0 && drops.Count == 0 ? Color.LightGreen : Color.FromArgb(66, 63, 51), 7);
        g.DrawLine(gate, 885, 290, 885, 370); g.DrawLine(gate, 75, 290, 75, 370);
        if (room == 9 && questStage >= 10) { DrawCentered(g, "PASSAGEM SOB O PRISMA", 480, new Font("Segoe UI", 9, FontStyle.Bold), Color.FromArgb(187, 218, 205)); if (Distance(player, new PointF(480, 500)) < 72) DrawCentered(g, "E  DESCER", 510, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite); }
    }

    private void DrawNameShoreEnvironment(Graphics g, Brush ground, Pen tile)
    {
        using var depth = new SolidBrush(Color.FromArgb(34, 43, 48)); using var stone = new SolidBrush(Color.FromArgb(63, 74, 74));
        using var edge = new Pen(Color.FromArgb(113, 143, 137), 2); using var seam = new Pen(Color.FromArgb(66, 111, 111), 2);
        g.FillRectangle(depth, 65, 108, 830, 445); g.FillRectangle(ground, 94, 132, 772, 397);
        for (int y = 0; y < 7; y++) for (int x = 0; x < 12; x++) g.DrawRectangle(tile, 70 + x * 68, 112 + y * 62, 67, 61);
        RectangleF[] pillars = room == 10 ? NameShoreWalls : VoiceWellWalls;
        foreach (RectangleF pillar in pillars) { g.FillRectangle(stone, pillar); g.DrawRectangle(edge, pillar.X, pillar.Y, pillar.Width, pillar.Height); }
        for (int i = 0; i < 6; i++) { float x = 155 + i * 126; g.DrawLine(seam, x, 128, x + 32, 254); g.DrawLine(seam, x + 32, 254, x - 20, 382); g.DrawLine(seam, x - 20, 382, x + 18, 520); }
        using var pool = new SolidBrush(Color.FromArgb(room == 11 ? 54 : 28, 125, 193, 185));
        g.FillEllipse(pool, room == 11 ? 370 : 345, room == 11 ? 235 : 185, room == 11 ? 220 : 270, room == 11 ? 190 : 280);
        if (room == 11) { using var core = new SolidBrush(Color.FromArgb(185, 210, 227, 188)); g.FillEllipse(core, 462, 338, 36, 36); }
        using var gate = new Pen(enemies.Count == 0 && drops.Count == 0 ? Color.LightGreen : Color.FromArgb(66, 82, 77), 7);
        g.DrawLine(gate, 885, 290, 885, 370); g.DrawLine(gate, 75, 290, 75, 370);
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
        float r = enemy.Radius; Color color = enemy.Kind == EnemyKind.BlindScribe ? Color.FromArgb(89, 128, 135) : enemy.Kind == EnemyKind.RiverFerryman ? Color.FromArgb(69, 120, 124) : enemy.Kind == EnemyKind.AshWarden ? Color.FromArgb(169, 91, 53) : enemy.Kind == EnemyKind.HollowName ? Color.FromArgb(120, 165, 155) : enemy.Boss ? Color.FromArgb(145, 87, 77) : enemy.Kind switch { EnemyKind.Elite => enemy.Armored ? Color.SlateGray : Color.IndianRed, EnemyKind.Archer => Color.FromArgb(128, 151, 106), EnemyKind.Mage => Color.MediumPurple, EnemyKind.InkEcho => Color.FromArgb(113, 98, 143), EnemyKind.DrownedMemory => Color.FromArgb(95, 145, 142), EnemyKind.EmberWisp => Color.FromArgb(212, 126, 61), EnemyKind.NameMoth => Color.FromArgb(126, 170, 156), _ => Color.FromArgb(183, 181, 166) };
        if (enemy.Windup > 0) { float baseWarn = enemy.Kind == EnemyKind.Elite ? 62 : enemy.Kind == EnemyKind.Mage ? 52 : enemy.Boss && enemy.BossPattern == 0 ? 90 : enemy.Kind == EnemyKind.Archer ? 26 : enemy.Kind == EnemyKind.InkEcho ? 30 : enemy.Kind == EnemyKind.DrownedMemory ? 28 : (enemy.Kind is EnemyKind.RiverFerryman or EnemyKind.AshWarden or EnemyKind.HollowName) && enemy.BossPattern == 2 ? 85 : enemy.Kind is EnemyKind.EmberWisp or EnemyKind.NameMoth ? 28 : 36; float progress = Math.Clamp(enemy.Windup / Math.Max(0.01f, enemy.WindupMax), 0, 1); float warn = baseWarn * (0.4f + progress * 0.6f); using var p = new Pen(Color.FromArgb(235, 115, 75), 3); using var b = new SolidBrush(Color.FromArgb(35, 235, 115, 75)); if ((enemy.Kind is EnemyKind.Archer or EnemyKind.InkEcho or EnemyKind.DrownedMemory or EnemyKind.EmberWisp or EnemyKind.NameMoth) || ((enemy.Kind is EnemyKind.RiverFerryman or EnemyKind.AshWarden or EnemyKind.HollowName) && enemy.BossPattern == 2)) g.DrawLine(p, enemy.Pos, enemy.Target); g.FillEllipse(b, enemy.Target.X - warn, enemy.Target.Y - warn, warn * 2, warn * 2); g.DrawEllipse(p, enemy.Target.X - warn, enemy.Target.Y - warn, warn * 2, warn * 2); }
        using var body = new SolidBrush(enemy.Flash > 0 ? Color.White : color); using var edge = new Pen(enemy == lockedTarget ? Color.Gold : enemy.Armored ? Color.LightSteelBlue : Color.FromArgb(35, 25, 22), enemy == lockedTarget ? 4 : 2);
        g.FillEllipse(body, enemy.Pos.X - r, enemy.Pos.Y - r, r * 2, r * 2); g.DrawEllipse(edge, enemy.Pos.X - r, enemy.Pos.Y - r, r * 2, r * 2);
        using var hpBack = new SolidBrush(Color.FromArgb(40, 20, 20)); using var hpBar = new SolidBrush(Color.FromArgb(188, 76, 70)); g.FillRectangle(hpBack, enemy.Pos.X - 22, enemy.Pos.Y - r - 10, 44, 5); g.FillRectangle(hpBar, enemy.Pos.X - 22, enemy.Pos.Y - r - 10, 44 * Math.Max(0, enemy.Hp / enemy.MaxHp), 5);
        string name = EnemyName(enemy); if (name.Length > 0) DrawCentered(g, name, enemy.Pos.Y - r - 28, new Font("Segoe UI", 8, FontStyle.Bold), Color.FromArgb(233, 198, 151), enemy.Pos.X);
        if (enemy.Boss) { using var bossBack = new SolidBrush(Color.FromArgb(40, 20, 20)); using var bossHp = new SolidBrush(enemy.Hp <= enemy.MaxHp * 0.5f ? Color.OrangeRed : enemy.Kind == EnemyKind.BlindScribe ? Color.Teal : enemy.Kind == EnemyKind.RiverFerryman ? Color.SteelBlue : enemy.Kind == EnemyKind.AshWarden ? Color.DarkOrange : enemy.Kind == EnemyKind.HollowName ? Color.LightSeaGreen : Color.Firebrick); g.FillRectangle(bossBack, 360, 112, 240, 8); g.FillRectangle(bossHp, 360, 112, 240 * Math.Clamp(enemy.Hp / enemy.MaxHp, 0, 1), 8); string bossName = enemy.Kind == EnemyKind.BlindScribe ? "ESCRIBA CEGO" : enemy.Kind == EnemyKind.RiverFerryman ? "BARQUEIRO OCO" : enemy.Kind == EnemyKind.AshWarden ? "GUARDIÃO DAS CINZAS" : enemy.Kind == EnemyKind.HollowName ? "O NOME VAZIO" : "GUARDIÃO DA CRIPTA"; DrawCentered(g, enemy.Hp <= enemy.MaxHp * 0.5f ? $"{bossName} • SEGUNDA FASE" : bossName, 91, new Font("Georgia", 11, FontStyle.Bold), enemy.Hp <= enemy.MaxHp * 0.5f ? Color.OrangeRed : Color.Goldenrod); }
    }
    private void DrawDrop(Graphics g, Drop item) { Color c = item.Kind switch { "relic" => Color.Violet, "seal" => Color.LightSkyBlue, "bell" => Color.FromArgb(162, 212, 192), "lens" => Color.FromArgb(248, 185, 95), "trace" => Color.FromArgb(185, 231, 215), "charm" => Color.Plum, "armor" => Color.MediumAquamarine, "weapon" => Color.CornflowerBlue, "potion" => Color.IndianRed, _ => Color.Goldenrod }; using var b = new SolidBrush(c); using var p = new Pen(Color.FromArgb(230, 220, 190), 1); g.FillEllipse(b, item.Pos.X - 8, item.Pos.Y - 8, 16, 16); g.DrawEllipse(p, item.Pos.X - 8, item.Pos.Y - 8, 16, 16); }
    private void DrawPlayer(Graphics g) { using var b = new SolidBrush(invulnerable > 0 ? Color.Salmon : Color.FromArgb(90, 143, 138)); using var face = new SolidBrush(Color.Bisque); g.FillEllipse(b, player.X - 16, player.Y - 16, 32, 32); g.FillEllipse(face, player.X - 8, player.Y - 11, 16, 16); using var p = new Pen(Color.AntiqueWhite, 4); g.DrawLine(p, player, Add(player, Scale(facing, 24))); if (skillCd > 0) { using var cooldownPen = new Pen(Color.FromArgb(190, 230, 193, 129), 3); g.DrawArc(cooldownPen, player.X - 25, player.Y - 25, 50, 50, -90, 360 * skillCd / Math.Max(1.5f, 6f - skillTalent * .5f)); } }
    private void DrawInventory(Graphics g)
    {
        using var b = new SolidBrush(Color.FromArgb(245, 17, 19, 20)); using var itemFont = new Font("Segoe UI", 11); g.FillRectangle(b, 150, 145, 660, 490);
        DrawCentered(g, "INVENTÁRIO   •   1–3 ARMAS   •   4 ARMADURA   •   5 RELÍQUIA   •   6 TALISMÃ   •   I FECHAR", 180, new Font("Segoe UI", 12, FontStyle.Bold), Color.Goldenrod);
        for (int i = 0; i < 3; i++) g.DrawString($"{i + 1}. {weaponNames[i]}    DANO {weaponDamage[i] - weaponDamage[weapon]:+#;-#;0}    {(weapon == i ? "EQUIPADA" : (ownedWeapons & (1 << i)) != 0 ? "DISPONÍVEL" : "NÃO ENCONTRADA")}", itemFont, (ownedWeapons & (1 << i)) != 0 ? Brushes.AntiqueWhite : Brushes.Gray, 205, 230 + i * 40);
        g.DrawString($"4. PEITORAL DE PEDRA {(armorOn ? "EQUIPADO" : armor ? "DISPONÍVEL" : "NÃO ENCONTRADO")}", itemFont, Brushes.AntiqueWhite, 205, 360);
        g.DrawString($"5. RELICÁRIO VITAL {(relicOn ? "EQUIPADO" : relic ? "DISPONÍVEL" : "NÃO ENCONTRADO")}", itemFont, Brushes.AntiqueWhite, 205, 400);
        g.DrawString($"SELO DA VIGÍLIA {(archiveSeal ? "ENCONTRADO • +8 DANO" : "AINDA NÃO ENCONTRADO")}", itemFont, Brushes.AntiqueWhite, 205, 440);
        g.DrawString($"SINO DE NARA {(riverBell ? "ENCONTRADO • +20 VIDA MÁXIMA" : "AINDA NÃO ENCONTRADO")}", itemFont, Brushes.AntiqueWhite, 205, 480);
        g.DrawString($"PRISMA DO LUME {(lumeLens ? "ENCONTRADO • +12 DANO" : "AINDA NÃO ENCONTRADO")}", itemFont, Brushes.AntiqueWhite, 205, 520);
        g.DrawString($"RASTRO DE NARA {(naraTrace ? "RECUPERADO • MEMÓRIA PRESERVADA" : "AINDA NÃO ENCONTRADO")}", itemFont, Brushes.AntiqueWhite, 205, 560);
        string availableCharms = string.Join(" • ", Enumerable.Range(1, 3).Where(i => (ownedCharms & (1 << (i - 1))) != 0).Select(i => charmNames[i]));
        g.DrawString($"TALISMÃS: {(availableCharms.Length == 0 ? "NENHUM" : availableCharms)}", itemFont, Brushes.Plum, 205, 582);
        string equippedName = equippedCharm == 0 ? "NENHUM" : charmNames[equippedCharm];
        g.DrawString($"6. EQUIPADO: {equippedName}", itemFont, Brushes.Plum, 205, 602);
        string effect = equippedCharm switch { 1 => "Redemoinho +35% dano • recarga +1 s", 2 => "Esquiva mais longa • recarga +0,3 s", 3 => "+12 p.p. crítico • ataque básico mais lento", _ => "Elites e chefes deixam talismãs especiais" };
        g.DrawString(effect, itemFont, Brushes.AntiqueWhite, 205, 622);
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
    private static string EnemyName(Enemy e) => e.Kind switch { EnemyKind.Guardian => "GUARDIÃO SEM NOME", EnemyKind.BlindScribe => "ESCRIBA CEGO", EnemyKind.RiverFerryman => "BARQUEIRO OCO", EnemyKind.AshWarden => "GUARDIÃO DAS CINZAS", EnemyKind.HollowName => "O NOME VAZIO", EnemyKind.DrownedMemory => "LEMBRANÇA AFOGADA", EnemyKind.EmberWisp => "VESTÍGIO DE BRASAS", EnemyKind.NameMoth => "MARIPOSA DE NOMES", EnemyKind.Elite => e.Armored ? "ELITE • BLINDADO" : "ELITE • FRENÉTICO", EnemyKind.Archer => "ARQUEIRO", EnemyKind.Mage => "CONJURADOR", EnemyKind.InkEcho => "ECO DE TINTA", _ => "" };
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
        game.memoryOverlay = false; game.clearedRooms = 0; game.room = 0; game.SetupRoom(); game.storyOverlay = false;
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
        PointF townPosition = game.player;
        if (Math.Abs(townPosition.X - 350) > 0.01f) throw new InvalidOperationException("A colisão da ferraria não bloqueou a passagem pela parede.");
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
        if (!game.drops.Any(d => d.Kind == "seal") || !game.drops.Any(d => d.Kind == "charm" && d.Amount == 3)) throw new InvalidOperationException("O Escriba Cego não deixou o Selo da Vigília e o Vidro de Vigília.");
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
        if (!game.drops.Any(d => d.Kind == "bell") || !game.drops.Any(d => d.Kind == "charm" && d.Amount == 2)) throw new InvalidOperationException("O Barqueiro Oco não deixou o sino de Nara e o Fio de Travessia.");
        foreach (Drop drop in game.drops.ToArray()) game.Collect(drop);
        if (!game.riverBell || game.questStage != 6 || game.maxHealth < 120 || !game.memoryOverlay) throw new InvalidOperationException("O sino não revelou a decisão de Nara nem concedeu vitalidade.");
        game.memoryOverlay = false; game.player = new PointF(850, 330); game.Interact();
        if (game.room != 3) throw new InvalidOperationException("A saída da Capela Afogada não retornou a Véspera.");
        game.player = BellKeeperPosition; game.Interact();
        if (game.questStage != 7 || game.gold != goldBeforeTown + 275 || game.potions != potionsBeforeTown + 3) throw new InvalidOperationException("Edda não reconheceu o sino e encerrou esta etapa da investigação.");
        game.memoryOverlay = false; game.player = new PointF(480, 520); game.Interact();
        if (game.room != 8 || game.enemies.Count != 5 || !game.enemies.Any(e => e.Kind == EnemyKind.EmberWisp)) throw new InvalidOperationException("A escada para o Lume não abriu sua galeria de cinzas.");
        Enemy emberWisp = game.enemies.First(e => e.Kind == EnemyKind.EmberWisp); emberWisp.Cooldown = 0; game.UpdateEnemy(emberWisp, 0.01f);
        if (emberWisp.Windup <= 0) throw new InvalidOperationException("O Vestígio de Brasas não anunciou seu disparo.");
        game.enemies.Clear(); game.drops.Clear(); game.player = new PointF(850, 330); game.Interact();
        if (game.room != 9 || !game.enemies.Any(e => e.Kind == EnemyKind.AshWarden)) throw new InvalidOperationException("A Câmara do Lume não apresentou o Guardião das Cinzas.");
        Enemy ashWarden = game.enemies.Single(e => e.Kind == EnemyKind.AshWarden); game.enemies.Clear(); game.enemies.Add(ashWarden);
        ashWarden.Cooldown = 0; game.UpdateEnemy(ashWarden, 0.01f);
        if (ashWarden.Windup <= 0 || ashWarden.BossPattern != 1) throw new InvalidOperationException("O Guardião das Cinzas não anunciou a marca de luz.");
        game.player = new PointF(840, 330); game.UpdateEnemy(ashWarden, 1f); ashWarden.Hp = 120; ashWarden.Cooldown = 0; game.UpdateEnemy(ashWarden, 0.01f);
        if (ashWarden.BossPattern != 2) throw new InvalidOperationException("O Guardião das Cinzas não alternou para a onda de brasas.");
        game.player = new PointF(150, 330); game.UpdateEnemy(ashWarden, 1f); ashWarden.Cooldown = 0; game.UpdateEnemy(ashWarden, 0.01f);
        if (ashWarden.BossPattern != 0) throw new InvalidOperationException("O Guardião das Cinzas não concluiu seu ciclo de ataques.");
        game.Defeat(ashWarden); game.talentChoice = false;
        if (!game.drops.Any(d => d.Kind == "lens")) throw new InvalidOperationException("O Guardião das Cinzas não deixou o Prisma da Primeira Vigília.");
        foreach (Drop drop in game.drops.ToArray()) game.Collect(drop);
        if (!game.lumeLens || game.questStage != 9 || game.damage < 50 || !game.memoryOverlay) throw new InvalidOperationException("O prisma não revelou a memória de Nara nem aprimorou o dano.");
        game.memoryOverlay = false; game.player = new PointF(850, 330); game.Interact();
        if (game.room != 3) throw new InvalidOperationException("A saída da Câmara do Lume não retornou a Véspera.");
        game.player = CuratorPosition; game.Interact();
        if (game.questStage != 10 || game.gold != goldBeforeTown + 430 || game.potions != potionsBeforeTown + 4) throw new InvalidOperationException("Sileth não registrou a nova memória com sua recompensa.");
        game.memoryOverlay = false; game.room = 9; game.SetupRoom(); game.player = new PointF(480, 500); game.Interact();
        if (game.room != 10 || game.enemies.Count != 6 || !game.enemies.Any(e => e.Kind == EnemyKind.NameMoth)) throw new InvalidOperationException("A passagem além do prisma não abriu a Margem sem Reflexo.");
        Enemy nameMoth = game.enemies.First(e => e.Kind == EnemyKind.NameMoth); nameMoth.Cooldown = 0; game.UpdateEnemy(nameMoth, 0.01f);
        if (nameMoth.Windup <= 0) throw new InvalidOperationException("A Mariposa de Nomes não anunciou seu ataque à distância.");
        game.enemies.Clear(); game.drops.Clear(); game.player = new PointF(850, 330); game.Interact();
        if (game.room != 11 || game.enemies.Count != 4 || !game.enemies.Any(e => e.Kind == EnemyKind.HollowName)) throw new InvalidOperationException("O Reservatório não apresentou O Nome Vazio.");
        Enemy hollowName = game.enemies.Single(e => e.Kind == EnemyKind.HollowName); game.enemies.Clear(); game.enemies.Add(hollowName);
        hollowName.Cooldown = 0; game.UpdateEnemy(hollowName, 0.01f);
        if (hollowName.Windup <= 0 || hollowName.BossPattern != 1) throw new InvalidOperationException("O Nome Vazio não anunciou o primeiro golpe marcado.");
        game.player = new PointF(840, 330); game.UpdateEnemy(hollowName, 1f); hollowName.Hp = 145; hollowName.Cooldown = 0; game.UpdateEnemy(hollowName, 0.01f);
        if (hollowName.BossPattern != 2) throw new InvalidOperationException("O Nome Vazio não alternou para seu golpe em linha.");
        game.player = new PointF(150, 330); game.UpdateEnemy(hollowName, 1f); hollowName.Cooldown = 0; game.UpdateEnemy(hollowName, 0.01f);
        if (hollowName.BossPattern != 0) throw new InvalidOperationException("O Nome Vazio não concluiu o ciclo de três ataques.");
        game.Defeat(hollowName); game.talentChoice = false;
        if (!game.drops.Any(d => d.Kind == "trace")) throw new InvalidOperationException("O Nome Vazio não deixou a lembrança preservada de Nara.");
        foreach (Drop drop in game.drops.ToArray()) game.Collect(drop);
        if (!game.naraTrace || game.questStage != 11 || !game.memoryOverlay) throw new InvalidOperationException("O rastro de Nara não foi recuperado no reservatório.");
        game.memoryOverlay = false; game.player = new PointF(850, 330); game.Interact();
        if (game.room != 3) throw new InvalidOperationException("A saída do Reservatório não retornou a Véspera.");
        game.player = CuratorPosition; game.Interact();
        if (game.questStage != 12 || game.gold != goldBeforeTown + 615 || game.potions != potionsBeforeTown + 5) throw new InvalidOperationException("Sileth não reconheceu a memória que escapou do Lume.");
        SaveData saved = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(game.CaptureSaveData()))!;
        if (saved.Room != 3 || saved.QuestStage != 12 || saved.MemoriesFound != 3 || !saved.ArchiveSeal || !saved.RiverBell || !saved.LumeLens || !saved.NaraTrace || saved.MaxHealth < 120 || (saved.ClearedRooms & (1 << 9)) == 0 || (saved.ClearedRooms & (1 << 11)) == 0) throw new InvalidOperationException("O progresso da campanha, as câmaras limpas e os itens não foram serializados no save.");
        using var lootGame = new Game(); lootGame.timer.Stop(); lootGame.StartGame(); lootGame.enemies.Clear(); lootGame.room = 1;
        var elite = new Enemy { Pos = new PointF(500, 330), Kind = EnemyKind.Elite, Hp = 1, MaxHp = 1 };
        lootGame.enemies.Add(elite); lootGame.Defeat(elite);
        Drop firstCharm = lootGame.drops.Single(d => d.Kind == "charm");
        if (firstCharm.Amount != 1) throw new InvalidOperationException("O Elite não deixou o Nó de Brasa.");
        lootGame.Collect(firstCharm);
        if (lootGame.equippedCharm != 1 || lootGame.WhirlwindMultiplier() <= 1.6f) throw new InvalidOperationException("O Nó de Brasa não alterou o Redemoinho.");
        lootGame.Collect(new Drop { Pos = lootGame.player, Kind = "charm", Amount = 2 }); lootGame.CycleCharm();
        if (lootGame.equippedCharm != 2 || lootGame.DashDuration() <= 0.17f || lootGame.DashCooldown() <= 1.15f) throw new InvalidOperationException("O Fio de Travessia não alterou a esquiva.");
        lootGame.Collect(new Drop { Pos = lootGame.player, Kind = "charm", Amount = 3 }); lootGame.CycleCharm();
        if (lootGame.equippedCharm != 3 || lootGame.CriticalChance() <= 0.12f || lootGame.CriticalMultiplier() <= 2f || lootGame.BasicAttackCooldown() <= 0.33f) throw new InvalidOperationException("O Vidro de Vigília não alterou os ataques críticos.");
        SaveData charmSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(lootGame.CaptureSaveData()))!;
        if (charmSave.OwnedCharms != 7 || charmSave.EquippedCharm != 3) throw new InvalidOperationException("Os talismãs não foram serializados no save.");
        using var restoreGame = new Game(); restoreGame.timer.Stop(); restoreGame.StartGame(); restoreGame.storyOverlay = false; restoreGame.room = 3; restoreGame.questStage = 12; restoreGame.naraTrace = true; restoreGame.player = CuratorPosition;
        restoreGame.InteractTown(); if (!restoreGame.endingChoiceOpen) throw new InvalidOperationException("Sileth não abriu a primeira decisão de Véspera.");
        restoreGame.OnKeyDown(null, new KeyEventArgs(Keys.D3)); if (restoreGame.endingChoiceOpen) throw new InvalidOperationException("A decisão não pôde ser adiada.");
        restoreGame.InteractTown(); restoreGame.OnKeyDown(null, new KeyEventArgs(Keys.D1));
        if (restoreGame.townEnding != 1 || restoreGame.questStage != 13 || restoreGame.maxHealth != 120 || !restoreGame.memoryOverlay) throw new InvalidOperationException("Restituir os nomes não alterou a praça e a progressão.");
        SaveData restoreSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(restoreGame.CaptureSaveData()))!;
        if (restoreSave.TownEnding != 1 || restoreSave.QuestStage != 13) throw new InvalidOperationException("A decisão de devolver os nomes não foi salva.");
        using var lumeGame = new Game(); lumeGame.timer.Stop(); lumeGame.StartGame(); lumeGame.storyOverlay = false; lumeGame.room = 3; lumeGame.questStage = 12; lumeGame.naraTrace = true; lumeGame.player = CuratorPosition;
        lumeGame.InteractTown(); lumeGame.OnKeyDown(null, new KeyEventArgs(Keys.D2));
        if (lumeGame.townEnding != 2 || lumeGame.questStage != 13 || lumeGame.damage != 35 || !lumeGame.memoryOverlay) throw new InvalidOperationException("Manter o Lume não criou seu ramo e seu bônus próprios.");
        SaveData lumeSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(lumeGame.CaptureSaveData()))!;
        if (lumeSave.TownEnding != 2 || lumeSave.QuestStage != 13) throw new InvalidOperationException("A decisão de manter o Lume não foi salva.");
        game.room = 11; game.SetupRoom();
        if (game.enemies.Count != 0 || game.drops.Count != 0) throw new InvalidOperationException("O Reservatório foi recriado após ter sido limpo.");
        game.room = 3; game.SetupRoom();
        game.memoryOverlay = false; game.player = CryptGatePosition; game.Interact();
        if (game.room != 2 || game.enemies.Count != 4) throw new InvalidOperationException("A passagem da cidade não retornou à cripta.");
        Console.WriteLine("TESTES OK: campanha, chefes, itens, talismãs e builds, progressão, save e retorno à cripta."); return 0;
    }
}









