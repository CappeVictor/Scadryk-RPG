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
    private enum EnemyKind { Grunt, Archer, Mage, Elite, Guardian, InkEcho, BlindScribe, DrownedMemory, RiverFerryman, EmberWisp, AshWarden, NameMoth, HollowName, NameWeaver, FirstKeeper, MudCantor, OathCartographer, PaperWraith, NameBroker, SaltEcho, TideWitness, ReedKeeper, FlowKeeper, CounterCurrent }
    private sealed class Enemy
    {
        public PointF Pos, Target, Target2, Target2Start;
        public EnemyKind Kind;
        public float Hp, MaxHp, Cooldown, Windup, WindupMax, Flash;
        public int BossPattern;
        public bool Armored;
        public float Stagger;
        public bool Boss => Kind is EnemyKind.Guardian or EnemyKind.BlindScribe or EnemyKind.RiverFerryman or EnemyKind.AshWarden or EnemyKind.HollowName or EnemyKind.NameWeaver or EnemyKind.FirstKeeper or EnemyKind.OathCartographer or EnemyKind.NameBroker or EnemyKind.TideWitness or EnemyKind.ReedKeeper or EnemyKind.FlowKeeper or EnemyKind.CounterCurrent;
        public float Radius => Boss ? 23 : Kind == EnemyKind.Elite ? 18 : 14;
    }
    private sealed class Drop { public PointF Pos; public string Kind = "gold"; public int Amount; }
    private sealed class EchoShard { public PointF Pos, Direction; public float Life, Damage; public HashSet<Enemy> Hit = new(); }
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
        public int[] WeaponBonuses { get; set; } = new int[3];
        public int[] WeaponRarities { get; set; } = new int[3];
        public int[] WeaponAffixes { get; set; } = new int[3];
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
        public int EquippedCharm2 { get; set; }
        public int TownEnding { get; set; }
        public bool ChorusMemory { get; set; }
        public bool FirstMemory { get; set; }
        public bool WestTrace { get; set; }
        public bool UpstreamTrace { get; set; }
        public int TideGlass { get; set; }
        public bool BeyondTrace { get; set; }
        public bool EbbSeal { get; set; }
        public bool CounterTrace { get; set; }
        public bool AnchorWard { get; set; }
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
    private static readonly RectangleF[] FarReachWalls = { new(242, 166, 62, 122), new(420, 390, 74, 130), new(650, 164, 58, 124), new(772, 391, 58, 128) };
    private static readonly RectangleF[] NameCloisterWalls = { new(260, 155, 58, 145), new(646, 155, 58, 145), new(260, 405, 58, 145), new(646, 405, 58, 145), new(448, 296, 64, 105) };
    private static readonly RectangleF[] FirstBasinWalls = { new(262, 158, 56, 138), new(645, 158, 56, 138), new(262, 405, 56, 138), new(645, 405, 56, 138), new(448, 188, 64, 78) };
    private static readonly RectangleF[] FirstVigilWalls = { new(246, 168, 52, 138), new(662, 168, 52, 138), new(246, 400, 52, 138), new(662, 400, 52, 138), new(425, 310, 110, 32), new(425, 378, 110, 32) };
    private static readonly RectangleF[] WestFordWalls = { new(244, 158, 58, 126), new(656, 158, 58, 126), new(244, 410, 58, 126), new(656, 410, 58, 126), new(433, 291, 94, 38) };
    private static readonly RectangleF[] WitnessYardWalls = { new(258, 154, 60, 136), new(642, 154, 60, 136), new(258, 405, 60, 136), new(642, 405, 60, 136), new(428, 255, 40, 64), new(492, 341, 40, 64) };
    private static readonly RectangleF[] RefugeWalls = { new(114, 145, 185, 93), new(662, 145, 185, 93), new(121, 430, 176, 85), new(666, 430, 176, 85), new(412, 254, 36, 88), new(514, 370, 36, 82) };
    private static readonly RectangleF[] RidgeWalls = { new(246, 158, 58, 126), new(656, 158, 58, 126), new(246, 410, 58, 126), new(656, 410, 58, 126), new(429, 289, 96, 42) };
    private static readonly RectangleF[] LedgerWalls = { new(256, 154, 54, 146), new(650, 154, 54, 146), new(256, 399, 54, 146), new(650, 399, 54, 146), new(425, 154, 110, 30), new(425, 416, 110, 30) };
    private static readonly RectangleF[] SaltMarshWalls = { new(240, 155, 56, 132), new(652, 155, 56, 132), new(240, 408, 56, 132), new(652, 408, 56, 132), new(438, 285, 84, 96) };
    private static readonly RectangleF[] TideBridgeWalls = { new(250, 155, 50, 115), new(660, 155, 50, 115), new(250, 425, 50, 115), new(660, 425, 50, 115) };
    private static readonly RectangleF[] ReedRunWalls = { new(238, 155, 58, 135), new(664, 155, 58, 135), new(238, 406, 58, 135), new(664, 406, 58, 135), new(430, 274, 58, 112) };
    private static readonly RectangleF[] ReedYardWalls = { new(240, 154, 56, 128), new(664, 154, 56, 128), new(240, 412, 56, 128), new(664, 412, 56, 128), new(430, 154, 96, 34), new(430, 506, 96, 34) };
    private static readonly RectangleF[] FloodMillWalls = { new(238, 150, 52, 132), new(664, 150, 52, 132), new(238, 414, 52, 132), new(664, 414, 52, 132), new(428, 240, 104, 42), new(428, 390, 104, 42) };
    private static readonly RectangleF[] SiltChamberWalls = { new(242, 158, 54, 124), new(664, 158, 54, 124), new(242, 410, 54, 124), new(664, 410, 54, 124), new(428, 282, 40, 72), new(492, 338, 40, 72) };
    private static readonly RectangleF[] CounterSlopeWalls = { new(238, 150, 52, 132), new(664, 150, 52, 132), new(238, 414, 52, 132), new(664, 414, 52, 132), new(358, 236, 48, 88), new(556, 356, 48, 88) };
    private static readonly RectangleF[] CounterHeartWalls = { new(246, 158, 54, 124), new(660, 158, 54, 124), new(246, 410, 54, 124), new(660, 410, 54, 124), new(430, 190, 100, 34), new(430, 436, 100, 34) };
    private static readonly RectangleF[] TownBuildings = { new(115, 128, 208, 128), new(677, 128, 208, 128), new(115, 426, 208, 120), new(677, 426, 208, 120) };
    private static readonly PointF CuratorPosition = new(625, 315), BellKeeperPosition = new(352, 347), ArchiveBookPosition = new(785, 355), CryptGatePosition = new(92, 330);
    private static readonly PointF AvenPosition = new(490, 335), SeraPosition = new(350, 340), NoaPosition = new(640, 360);
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
    private readonly List<EchoShard> echoShards = new();
    private readonly string[] roomNames = { "VESTÍBULO EM RUÍNAS", "SALÃO DOS OSSOS", "CÂMARA DO GUARDIÃO", "VÉSPERA • PRAÇA DO FAROL", "ARQUIVO NORTE • GALERIA", "ARQUIVO NORTE • COFRE", "RIO CEGO • LEITO SECO", "RIO CEGO • CAPELA AFOGADA", "FAROL • ESCADA DE CINZAS", "FAROL • CÂMARA DO LUME", "ALÉM DO LUME • MARGEM SEM REFLEXO", "RESERVATÓRIO • VOZES RETIDAS", "COSTA DAS VOZES TARDIAS", "CLAUSTRO DO CORO PARTIDO", "BACIA DO PRIMEIRO NOME", "SALA DA PRIMEIRA VIGÍLIA", "VAU DOS NOMES ESCOLHIDOS", "PÁTIO DAS TESTEMUNHAS", "REFÚGIO DO VEIO CALMO", "TRILHA DOS LUMINÁRIOS", "CISTERNA DO REGISTRO VAZIO", "VÁRZEA DAS VOZES SOPRADAS", "PONTE DA MARÉ INVERTIDA", "COTOVELO DOS JUNCAIS", "PÁTIO DAS AMARRAS LIVRES", "MOINHOS DA VAZANTE", "CÂMARA DO VEIO PRESO", "ESCARPA DO CONTRAFLUXO", "CORAÇÃO DAS DUAS MARGENS" };
    private readonly string[] weaponNames = { "ESPADA DE PEDRA", "LÂMINA DO ARQUIVO", "FACA DO CREPÚSCULO" };
    private readonly string[] charmNames = { "", "NÓ DE BRASA", "FIO DE TRAVESSIA", "VIDRO DE VIGÍLIA", "MARCA DA VIGÍLIA", "FIO DA MARGEM" };
    private readonly string[] prologue = {
        "Na noite sem sinos, Véspera começou a esquecer os nomes de seus mortos. Sob as ruas, o Rio Cego repetiu uma voz que ninguém mais conseguia ouvir.",
        "Você é um Escutador: ouve as lembranças que a pedra não conseguiu guardar em silêncio. Entre elas reconheceu Nara, sua irmã, desaparecida quando os sinos pararam.",
        "A memória dela chega partida: “Desça. Ele ainda lembra.” O Guardião sem Nome espera na cripta, diante da única pista que pode explicar por que Nara foi até lá."
    };
    private readonly int[] weaponDamage = { 0, 10, 4 };
    private readonly int[] weaponBonuses = new int[3], weaponRarities = new int[3], weaponAffixes = new int[3];
    private PointF player = new(150, 330), facing = new(1, 0), mouseAim = new(1, 0);
    private int room, level = 1, xp, gold, potions = 2, damage = 25, weapon, ownedWeapons = 1;
    private int healthTalent, skillTalent, dodgeTalent, pendingTalents, enemiesDefeated;
    private int memoriesFound, questStage, clearedRooms, tideGlass;
    private bool chorusMemory, firstMemory, westTrace, upstreamTrace, forgeMenuOpen;
    private bool beyondTrace, ebbSeal, counterTrace, anchorWard;
    private int ownedCharms, equippedCharm, equippedCharm2;
    private bool archiveSeal;
    private bool riverBell;
    private bool lumeLens;
    private bool naraTrace;
    private float health = 100, maxHealth = 100, attackCd, skillCd, shardCd, bellCd, bellWaveTime, bellWaveRadius, pullCd, pullWaveTime, wardCd, wardTime, dashCd, dashTime, invulnerable, noticeTime, windupPulse;
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
        room = 0; level = 1; xp = gold = damage = 0; damage = 25; potions = 2; weapon = 0; ownedWeapons = 1; Array.Clear(weaponBonuses); Array.Clear(weaponRarities); Array.Clear(weaponAffixes);
        armor = armorOn = relic = relicOn = archiveSeal = riverBell = lumeLens = naraTrace = chorusMemory = firstMemory = westTrace = upstreamTrace = beyondTrace = ebbSeal = counterTrace = anchorWard = inventoryOpen = dead = victory = chestChoice = talentChoice = memoryOverlay = endingChoiceOpen = forgeMenuOpen = false;
        townEnding = 0;
        healthTalent = skillTalent = dodgeTalent = pendingTalents = enemiesDefeated = memoriesFound = questStage = tideGlass = 0;
        clearedRooms = 0;
        ownedCharms = equippedCharm = equippedCharm2 = 0;
        health = maxHealth = 100; attackCd = skillCd = shardCd = bellCd = bellWaveTime = bellWaveRadius = pullCd = pullWaveTime = wardCd = wardTime = dashCd = dashTime = invulnerable = 0; echoShards.Clear(); title = false; storyPage = 0; storyOverlay = true; SetupRoom();
    }

    private void AdvanceStory()
    {
        if (!storyOverlay) return;
        storyPage++;
        if (storyPage >= prologue.Length) storyOverlay = false;
    }

    private void SetupRoom()
    {
        player = new PointF(150, 330); enemies.Clear(); drops.Clear(); echoShards.Clear(); lockedTarget = null;
        if (room != 3 && (clearedRooms & (1 << room)) != 0) return;
        int count = room is 3 or 18 ? 0 : room == 0 ? 3 : room == 1 ? 5 : room == 4 ? 5 : room == 5 ? 4 : room == 6 ? 6 : room == 21 ? 6 : room == 22 ? 5 : room == 23 ? 6 : room == 24 ? 5 : room == 25 ? 6 : room is 26 or 28 ? 5 : room == 27 ? 6 : (room is 7 or 9 or 11 or 15 or 17 or 20) ? 4 : room == 8 ? 5 : room is 10 or 12 or 14 or 16 or 19 ? 6 : room == 13 ? 4 : 4;
        for (int i = 0; i < count; i++)
        {
            EnemyKind kind = room == 2 && i == 0 ? EnemyKind.Guardian : room == 5 && i == 0 ? EnemyKind.BlindScribe : room == 7 && i == 0 ? EnemyKind.RiverFerryman : room == 9 && i == 0 ? EnemyKind.AshWarden : room == 13 && i == 0 ? EnemyKind.NameWeaver : room == 15 && i == 0 ? EnemyKind.FirstKeeper : room == 17 && i == 0 ? EnemyKind.OathCartographer : room == 20 && i == 0 ? EnemyKind.NameBroker : room == 26 && i == 0 ? EnemyKind.FlowKeeper : room == 24 && i == 0 ? EnemyKind.ReedKeeper : room == 22 && i == 0 ? EnemyKind.TideWitness : room == 11 && i == 0 ? EnemyKind.HollowName : room == 1 && i == count - 1 ? EnemyKind.Elite : room == 4 && (i is 1 or 4) ? EnemyKind.InkEcho : room == 5 && i == 2 ? EnemyKind.InkEcho : room == 6 && (i is 1 or 4) ? EnemyKind.DrownedMemory : room == 7 && i == 2 ? EnemyKind.DrownedMemory : room == 8 && (i is 1 or 4) ? EnemyKind.EmberWisp : room == 9 && i == 2 ? EnemyKind.EmberWisp : room == 10 && (i is 1 or 4) ? EnemyKind.NameMoth : room is 12 && (i is 1 or 4) ? EnemyKind.NameMoth : (room is 11 or 13 or 14 or 15 or 16 or 17 or 19 or 20) && i == 2 ? EnemyKind.NameMoth : (room is 12 or 13 or 14 or 15 or 16 or 17 or 19 or 20) && i == 1 ? EnemyKind.NameMoth : room == 16 && i == 4 ? EnemyKind.MudCantor : room == 17 && i == 3 ? EnemyKind.MudCantor : (room is 19 or 20) && i == 3 ? EnemyKind.PaperWraith : (room is 21 or 22 or 23 or 24 or 25 or 26) && i == 2 ? EnemyKind.SaltEcho : (room is 23 or 24 or 25 or 26) && i == 3 ? EnemyKind.MudCantor : room > 0 && i == 1 ? EnemyKind.Archer : (room is 2 or 5 or 7 or 9 or 11 or 14 or 16 or 19) && i == 2 ? EnemyKind.Mage : EnemyKind.Grunt;
            if (room == 28 && i == 0) kind = EnemyKind.CounterCurrent;
            if (room >= 27 && i == 2) kind = EnemyKind.SaltEcho;
            if (room >= 27 && i == 3) kind = EnemyKind.MudCantor;
            float hp = kind switch { EnemyKind.Guardian => 160, EnemyKind.BlindScribe => 245, EnemyKind.RiverFerryman => 285, EnemyKind.AshWarden => 340, EnemyKind.HollowName => 390, EnemyKind.NameWeaver => townEnding == 1 ? 410 : 455, EnemyKind.FirstKeeper => townEnding == 1 ? 560 : 620, EnemyKind.OathCartographer => townEnding == 1 ? 610 : 690, EnemyKind.NameBroker => townEnding == 1 ? 720 : 790, EnemyKind.TideWitness => townEnding == 1 ? 820 : 920, EnemyKind.ReedKeeper => townEnding == 1 ? 970 : 1060, EnemyKind.FlowKeeper => townEnding == 1 ? 1120 : 1230, EnemyKind.CounterCurrent => townEnding == 1 ? 1320 : 1440, EnemyKind.Elite => 120, EnemyKind.Archer or EnemyKind.Mage or EnemyKind.InkEcho or EnemyKind.DrownedMemory or EnemyKind.EmberWisp or EnemyKind.NameMoth or EnemyKind.MudCantor or EnemyKind.PaperWraith or EnemyKind.SaltEcho => 72, _ => 58 + room * 8 };
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
        if (forgeMenuOpen) { if (e.KeyCode is Keys.D1 or Keys.D2 or Keys.D3) ReforgeWeapon((int)e.KeyCode - (int)Keys.D0); else if (e.KeyCode == Keys.Escape) forgeMenuOpen = false; return; }
        if (endingChoiceOpen) { if (e.KeyCode == Keys.D1) ChooseTownEnding(1); else if (e.KeyCode == Keys.D2) ChooseTownEnding(2); else if (e.KeyCode == Keys.D3) endingChoiceOpen = false; return; }
        if (chestChoice) { if (e.KeyCode == Keys.D1) gold += 75; else if (e.KeyCode == Keys.D2) potions += 2; else if (e.KeyCode == Keys.D3) health = maxHealth; else return; chestChoice = false; EnterVesper(); return; }
        if (talentChoice) { SelectTalent(e.KeyCode); return; }
        if (e.KeyCode == Keys.I) { inventoryOpen = !inventoryOpen; return; }
        if (inventoryOpen) { if (e.KeyCode >= Keys.D1 && e.KeyCode <= Keys.D3) EquipWeapon((int)e.KeyCode - (int)Keys.D1); else if (e.KeyCode == Keys.D4 && armor) armorOn = !armorOn; else if (e.KeyCode == Keys.D5 && relic) relicOn = !relicOn; else if (e.KeyCode == Keys.D6) CycleCharm(1); else if (e.KeyCode == Keys.D7) CycleCharm(2); return; }
        if (e.KeyCode == Keys.Space || e.KeyCode == Keys.J) Attack();
        else if ((e.KeyCode == Keys.ShiftKey || e.KeyCode == Keys.K) && dashCd <= 0) { dashTime = DashDuration(); dashCd = DashCooldown(); }
        else if (e.KeyCode == Keys.R) Whirlwind();
        else if (e.KeyCode == Keys.F) CastVoiceShard();
        else if (e.KeyCode == Keys.V) CastBellPulse();
        else if (e.KeyCode == Keys.G) CastEbbPull();
        else if (e.KeyCode == Keys.H) CastAnchorWard();
        else if (e.KeyCode == Keys.Q) UsePotion();
        else if (e.KeyCode == Keys.E) Interact();
        else if (e.KeyCode == Keys.F5) { if (enemies.Count == 0 && drops.Count == 0) SaveGame(); else { notice = "LIMPE A SALA E RECOLHA O LOOT ANTES DE SALVAR"; noticeTime = 2; } }
        else if (e.KeyCode == Keys.Tab) CycleTarget();
    }

    private void ReforgeWeapon(int affix)
    {
        if (!forgeMenuOpen || room != 18 || questStage < 27 || Distance(player, AvenPosition) >= 60) { forgeMenuOpen = false; return; }
        if (affix is < 1 or > 3) return;
        if (gold < 75 || tideGlass < 1) { notice = "A TÊMPERA CUSTA 75 OURO E 1 SAL VÍTREO"; noticeTime = 2; return; }
        if (weaponAffixes[weapon] == affix) { notice = "ESSA ARMA JÁ GUARDA ESSE AFFIX"; noticeTime = 2; return; }
        int oldPower = WeaponPower(weapon);
        if (weaponRarities[weapon] == 0) { weaponRarities[weapon] = 1; weaponBonuses[weapon] = random.Next(3, 7); }
        weaponAffixes[weapon] = affix;
        damage += WeaponPower(weapon) - oldPower;
        gold -= 75; tideGlass--; forgeMenuOpen = false;
        notice = $"TÊMPERA: {weaponNames[weapon]} {WeaponRarityName(weaponRarities[weapon])} {WeaponAffixDescription(weapon)}"; noticeTime = 3;
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
            bool critical = random.NextDouble() < CriticalChance(); int dealt = (int)(damage * OutgoingDamageMultiplier(enemy) * (critical ? CriticalMultiplier() : 1)); if (enemy.Armored) dealt = (int)(dealt * 0.75f);
            enemy.Hp -= dealt; enemy.Flash = 0.2f; if (enemy.Hp <= 0) Defeat(enemy);
        }
    }

    private void Whirlwind()
    {
        if (skillCd > 0 || talentChoice) return;
        skillCd = Math.Max(1.5f, 6f - skillTalent * 0.5f) + WhirlwindCooldownPenalty();
        foreach (Enemy enemy in enemies.ToArray()) if (Distance(player, enemy.Pos) < 110) { enemy.Hp -= (int)(damage * OutgoingDamageMultiplier(enemy) * WhirlwindMultiplier() * WeaponSkillMultiplier() * (enemy.Armored ? 0.75f : 1)); if (enemy.Hp <= 0) Defeat(enemy); }
    }

    private void CastVoiceShard()
    {
        if (room == 3 || shardCd > 0 || enemies.Count == 0) return;
        facing = lockedTarget is not null && enemies.Contains(lockedTarget) ? Normalize(Sub(lockedTarget.Pos, player), facing) : Normalize(mouseAim, facing);
        echoShards.Add(new EchoShard { Pos = Add(player, Scale(facing, 23)), Direction = facing, Life = VoiceShardLifetime(), Damage = VoiceShardDamage() });
        shardCd = VoiceShardCooldown(); notice = "ESTILHAÇO DE VOZ"; noticeTime = 0.75f;
    }

    private float BellPulseRadius() => HasCharm(2) ? 190f : 155f;
    private float BellPulseCooldown() => Math.Max(7f, 14f - skillTalent * 0.7f) + (HasCharm(2) ? 1.5f : 0f);
    private float WeaponSkillMultiplier() => weaponAffixes[weapon] == 3 ? weaponRarities[weapon] == 2 ? 1.18f : 1.12f : 1f;
    private int WeaponPower(int index) => weaponDamage[index] + weaponBonuses[index];
    private static string WeaponRarityName(int index) => index == 0 ? "COMUM" : index == 1 ? "RARA" : "RELÍQUIA";
    private string WeaponAffixName(int index) => weaponAffixes[index] switch { 1 => "da Trégua", 2 => "do Contracanto", 3 => "da Maré", _ => "sem afixo" };
    private string WeaponDisplayName(int index) => weaponRarities[index] == 0 ? $"{weaponNames[index]} [COMUM]" : $"{weaponNames[index]} • {WeaponRarityName(index)} {WeaponAffixName(index)}";
    private string WeaponAffixDescription(int index) => weaponAffixes[index] switch { 1 => $"da Trégua: recupera {(weaponRarities[index] == 2 ? 6 : 3)} vida por abate", 2 => $"do Contracanto: +{(weaponRarities[index] == 2 ? 11 : 7)} p.p. de crítico", 3 => $"da Maré: +{(weaponRarities[index] == 2 ? 18 : 12)}% de dano de técnica", _ => "Sem afixo" };

    private void CastBellPulse()
    {
        if (!riverBell) { notice = "O SINO DE NARA AINDA NÃO FOI ENCONTRADO"; noticeTime = 1.6f; return; }
        if (room is 3 or 18 || enemies.Count == 0 || bellCd > 0) return;
        bellCd = BellPulseCooldown(); bellWaveTime = 0.42f; bellWaveRadius = BellPulseRadius();
        foreach (Enemy enemy in enemies.ToArray())
        {
            if (Distance(player, enemy.Pos) > bellWaveRadius) continue;
            float multiplier = (HasCharm(3) ? 1.25f : 1f) * WeaponSkillMultiplier();
            float power = enemy.Boss ? 0.55f : 0.95f;
            enemy.Hp -= (int)(damage * power * multiplier * OutgoingDamageMultiplier(enemy) * (enemy.Armored ? 0.75f : 1));
            enemy.Flash = 0.2f;
            if (!enemy.Boss) { enemy.Stagger = 0.95f; enemy.Windup = 0; enemy.Cooldown = Math.Max(enemy.Cooldown, 1.8f); }
            if (enemy.Hp <= 0) Defeat(enemy);
        }
        notice = HasCharm(2) ? "SINO DE NARA • ONDA AMPLIADA" : "SINO DE NARA • RESSONÂNCIA"; noticeTime = 1.2f;
    }

    private float EbbPullCooldown() => Math.Max(8f, 12f - skillTalent * 0.5f);
    private void CastEbbPull()
    {
        if (!ebbSeal) { notice = "A VAZANTE AINDA NÃO FOI APRENDIDA"; noticeTime = 1.5f; return; }
        if (room is 3 or 18 || enemies.Count == 0 || pullCd > 0) return;
        pullCd = EbbPullCooldown(); pullWaveTime = 0.45f;
        float techniqueBoost = (HasCharm(3) ? 1.25f : 1f) * WeaponSkillMultiplier();
        foreach (Enemy enemy in enemies.ToArray())
        {
            float distance = Distance(player, enemy.Pos); if (distance > 160) continue;
            enemy.Hp -= damage * (enemy.Boss ? 0.3f : 0.48f) * techniqueBoost * OutgoingDamageMultiplier(enemy) * (enemy.Armored ? 0.75f : 1f);
            enemy.Flash = 0.22f;
            if (enemy.Boss) enemy.Pos = MoveEnemy(enemy, Scale(Normalize(Sub(player, enemy.Pos), PointF.Empty), 22));
            else { enemy.Pos = MoveEnemy(enemy, Scale(Normalize(Sub(player, enemy.Pos), PointF.Empty), Math.Min(85, Math.Max(0, distance - 36)))); enemy.Windup = 0; enemy.Stagger = 0.55f; enemy.Cooldown = Math.Max(enemy.Cooldown, 1.2f); }
            if (enemy.Hp <= 0) Defeat(enemy);
        }
        notice = "VAZANTE • ECOS REUNIDOS"; noticeTime = 1.2f;
    }

    private float AnchorWardCooldown() => Math.Max(13f, 20f - skillTalent * 0.8f);
    private void CastAnchorWard()
    {
        if (!anchorWard) { notice = "O MANTO DE LASTRO AINDA NÃO FOI APRENDIDO"; noticeTime = 1.5f; return; }
        if (room is 3 or 18 || enemies.Count == 0 || wardCd > 0) return;
        wardTime = 2.5f; wardCd = AnchorWardCooldown();
        notice = "MANTO DE LASTRO • PROTEÇÃO ATIVA"; noticeTime = 1.2f;
    }

    private PointF CurrentDrift()
    {
        if (room == 25 && player.X is > 300 and < 670 && player.Y is > 286 and < 384) return new PointF(58, 0);
        if (room == 26)
        {
            PointF center = new(480, 330); float distance = Distance(player, center);
            if (distance is > 90 and < 260) return Scale(Normalize(Sub(center, player), PointF.Empty), 34);
        }
        if (room == 28)
        {
            PointF center = new(480, 330); float distance = Distance(player, center);
            if (distance is > 100 and < 250)
            {
                PointF radial = Normalize(Sub(center, player), PointF.Empty);
                float direction = MathF.Sin(windupPulse * 1.35f) >= 0 ? 1 : -1;
                return Scale(radial, 42 * direction);
            }
        }
        return PointF.Empty;
    }

    private void AdvanceVoiceShards(float dt)
    {
        for (int i = echoShards.Count - 1; i >= 0; i--)
        {
            EchoShard shard = echoShards[i]; shard.Life -= dt;
            float distance = 520 * dt; int steps = Math.Max(1, (int)MathF.Ceiling(distance / 6)); float step = distance / steps; bool blocked = false;
            for (int n = 0; n < steps; n++)
            {
                PointF next = Add(shard.Pos, Scale(shard.Direction, step));
                if (Blocked(next, 4)) { blocked = true; break; }
                shard.Pos = next;
                foreach (Enemy enemy in enemies.ToArray())
                {
                    if (shard.Hit.Contains(enemy) || DistanceToSegment(enemy.Pos, Sub(shard.Pos, Scale(shard.Direction, step)), shard.Pos) >= enemy.Radius + 8) continue;
                    shard.Hit.Add(enemy); enemy.Hp -= shard.Damage * OutgoingDamageMultiplier(enemy) * (enemy.Armored ? 0.75f : 1); enemy.Flash = 0.16f;
                    if (enemy.Hp <= 0) Defeat(enemy);
                }
            }
            if (blocked || shard.Life <= 0 || shard.Pos.X < 75 || shard.Pos.X > 885 || shard.Pos.Y < 110 || shard.Pos.Y > 550) echoShards.RemoveAt(i);
        }
    }

    private void Defeat(Enemy enemy)
    {
        if (!enemies.Remove(enemy)) return;
        enemiesDefeated++; drops.Add(new Drop { Pos = enemy.Pos, Kind = "gold", Amount = enemy.Boss ? 35 : enemy.Kind == EnemyKind.Elite ? 20 : 8 });
        AddXp(enemy.Boss ? 75 : enemy.Kind == EnemyKind.Elite ? 40 : 22);
        if (weaponAffixes[weapon] == 1) health = Math.Min(maxHealth, health + (weaponRarities[weapon] == 2 ? 6 : 3));
        if (enemy.Kind == EnemyKind.Guardian && !relic) drops.Add(new Drop { Pos = Offset(enemy.Pos, -24, 0), Kind = "relic" });
        if (enemy.Kind == EnemyKind.BlindScribe && !archiveSeal) drops.Add(new Drop { Pos = Offset(enemy.Pos, -24, 0), Kind = "seal" });
        if (enemy.Kind == EnemyKind.RiverFerryman && !riverBell) drops.Add(new Drop { Pos = Offset(enemy.Pos, -24, 0), Kind = "bell" });
        if (enemy.Kind == EnemyKind.AshWarden && !lumeLens) drops.Add(new Drop { Pos = Offset(enemy.Pos, -24, 0), Kind = "lens" });
        if (enemy.Kind == EnemyKind.HollowName && !naraTrace) drops.Add(new Drop { Pos = Offset(enemy.Pos, -24, 0), Kind = "trace" });
        if (enemy.Kind == EnemyKind.NameWeaver && !chorusMemory) drops.Add(new Drop { Pos = Offset(enemy.Pos, -24, 0), Kind = "chorus" });
        if (enemy.Kind == EnemyKind.FirstKeeper && !firstMemory) drops.Add(new Drop { Pos = Offset(enemy.Pos, -24, 0), Kind = "first" });
        if (enemy.Kind == EnemyKind.OathCartographer && !westTrace) drops.Add(new Drop { Pos = Offset(enemy.Pos, -24, 0), Kind = "west" });
        if (enemy.Kind == EnemyKind.NameBroker && questStage == 22) drops.Add(new Drop { Pos = Offset(enemy.Pos, -24, 0), Kind = "register" });
        if (enemy.Kind == EnemyKind.TideWitness && !upstreamTrace)
        {
            drops.Add(new Drop { Pos = Offset(enemy.Pos, -24, 0), Kind = "witness" });
            drops.Add(new Drop { Pos = Offset(enemy.Pos, 24, 0), Kind = "glass", Amount = 3 });
        }
        if (enemy.Kind == EnemyKind.FlowKeeper && questStage < 32)
        {
            drops.Add(new Drop { Pos = Offset(enemy.Pos, -24, 0), Kind = "ebb" });
            drops.Add(new Drop { Pos = Offset(enemy.Pos, 24, 0), Kind = "glass", Amount = 3 });
        }        if (enemy.Kind == EnemyKind.ReedKeeper && !beyondTrace)
        {
            drops.Add(new Drop { Pos = Offset(enemy.Pos, -24, 0), Kind = "beyond" });
            drops.Add(new Drop { Pos = Offset(enemy.Pos, 24, 0), Kind = "glass", Amount = 2 });
        }
        if (enemy.Kind == EnemyKind.CounterCurrent && !counterTrace)
        {
            drops.Add(new Drop { Pos = Offset(enemy.Pos, -24, 0), Kind = "counter" });
            drops.Add(new Drop { Pos = Offset(enemy.Pos, 24, 0), Kind = "glass", Amount = 3 });
        }
        if (enemy.Kind == EnemyKind.SaltEcho && random.NextDouble() < 0.2) drops.Add(new Drop { Pos = Offset(enemy.Pos, 18, 14), Kind = "glass", Amount = 1 });
        if (enemy.Kind == EnemyKind.FirstKeeper && (ownedCharms & 8) == 0) drops.Add(new Drop { Pos = Offset(enemy.Pos, 24, -12), Kind = "charm", Amount = 4 });
        if (enemy.Kind == EnemyKind.OathCartographer && (ownedCharms & 16) == 0) drops.Add(new Drop { Pos = Offset(enemy.Pos, 24, -12), Kind = "charm", Amount = 5 });
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
        float dt = timer.Interval / 1000f; noticeTime = Math.Max(0, noticeTime - dt); attackCd = Math.Max(0, attackCd - dt); skillCd = Math.Max(0, skillCd - dt); shardCd = Math.Max(0, shardCd - dt); bellCd = Math.Max(0, bellCd - dt); bellWaveTime = Math.Max(0, bellWaveTime - dt); pullCd = Math.Max(0, pullCd - dt); pullWaveTime = Math.Max(0, pullWaveTime - dt); wardCd = Math.Max(0, wardCd - dt); wardTime = Math.Max(0, wardTime - dt); dashCd = Math.Max(0, dashCd - dt); dashTime = Math.Max(0, dashTime - dt); invulnerable = Math.Max(0, invulnerable - dt); windupPulse += dt;
        if (title || dead || victory || storyOverlay || memoryOverlay || inventoryOpen || talentChoice || chestChoice || endingChoiceOpen) { Invalidate(); return; }
        PointF dir = new((down.Contains(Keys.D) || down.Contains(Keys.Right) ? 1 : 0) - (down.Contains(Keys.A) || down.Contains(Keys.Left) ? 1 : 0), (down.Contains(Keys.S) || down.Contains(Keys.Down) ? 1 : 0) - (down.Contains(Keys.W) || down.Contains(Keys.Up) ? 1 : 0));
        dir = Normalize(dir, PointF.Empty); if (dashTime > 0) dir = facing;
        MovePlayer(Scale(dir, (dashTime > 0 ? 530 : 190) * dt));
        if (dashTime <= 0) MovePlayer(Scale(CurrentDrift(), dt));
        for (int i = drops.Count - 1; i >= 0; i--) if (Distance(player, drops[i].Pos) < 30) Collect(drops[i]);
        foreach (Enemy enemy in enemies.ToArray()) { UpdateEnemy(enemy, dt); if (dead) break; }
        AdvanceVoiceShards(dt);
        if (enemies.Count == 0 && drops.Count == 0 && room == 2 && !memoryOverlay) chestChoice = true;
        MarkRoomCleared();
        Invalidate();
    }

    private void UpdateEnemy(Enemy e, float dt)
    {
        e.Cooldown -= dt; e.Flash = Math.Max(0, e.Flash - dt); PointF offset = Sub(player, e.Pos); float dist = Length(offset);
        if (e.Stagger > 0) { e.Stagger = Math.Max(0, e.Stagger - dt); return; }
        if (e.Windup > 0) {
            e.Windup -= dt;
            float reach = e.Boss ? e.BossPattern == 0 ? 90 : e.Kind == EnemyKind.RiverFerryman && e.BossPattern == 2 ? 85 : e.Kind == EnemyKind.AshWarden && e.BossPattern == 2 ? 68 : e.Kind == EnemyKind.HollowName && e.BossPattern == 2 ? 85 : (e.Kind is EnemyKind.NameWeaver or EnemyKind.OathCartographer or EnemyKind.NameBroker or EnemyKind.TideWitness or EnemyKind.ReedKeeper or EnemyKind.FlowKeeper or EnemyKind.CounterCurrent) && e.BossPattern == 2 ? 230 : 42 : e.Kind == EnemyKind.Elite ? 62 : e.Kind == EnemyKind.Mage ? 52 : e.Kind is EnemyKind.Archer or EnemyKind.InkEcho or EnemyKind.PaperWraith ? 230 : e.Kind == EnemyKind.DrownedMemory ? 195 : e.Kind is EnemyKind.EmberWisp or EnemyKind.NameMoth or EnemyKind.MudCantor ? 210 : 38;
            bool targeted = (e.Boss && (e.BossPattern is 1 or 2)) || (e.Kind is EnemyKind.Mage or EnemyKind.Elite or EnemyKind.Archer or EnemyKind.InkEcho or EnemyKind.DrownedMemory or EnemyKind.EmberWisp or EnemyKind.NameMoth or EnemyKind.MudCantor or EnemyKind.PaperWraith or EnemyKind.SaltEcho);
            bool inFirstVoiceLine = e.Kind == EnemyKind.FirstKeeper && e.BossPattern == 2 && (DistanceToSegment(player, e.Pos, e.Target) < 25 || DistanceToSegment(player, e.Target2Start, e.Target2) < 25);
            bool inAttack = inFirstVoiceLine || ((e.Kind is EnemyKind.NameWeaver or EnemyKind.OathCartographer or EnemyKind.NameBroker or EnemyKind.TideWitness or EnemyKind.ReedKeeper or EnemyKind.FlowKeeper or EnemyKind.CounterCurrent) && e.BossPattern == 2 ? DistanceToSegment(player, e.Pos, e.Target) < 24 : targeted ? Distance(player, e.Target) < reach : dist < reach);
            if (e.Windup <= 0 && inAttack && invulnerable <= 0 && dashTime <= 0) HitPlayer(e.Boss ? e.Kind == EnemyKind.BlindScribe ? 23 : e.Kind == EnemyKind.RiverFerryman ? 28 : e.Kind == EnemyKind.AshWarden ? 31 : e.Kind == EnemyKind.HollowName ? 36 : e.Kind == EnemyKind.NameWeaver ? 39 : e.Kind == EnemyKind.FirstKeeper ? 44 : e.Kind == EnemyKind.OathCartographer ? 42 : e.Kind == EnemyKind.NameBroker ? 48 : e.Kind == EnemyKind.CounterCurrent ? 68 : e.Kind == EnemyKind.FlowKeeper ? 62 : e.Kind == EnemyKind.ReedKeeper ? 56 : e.Kind == EnemyKind.TideWitness ? 52 : 26 : e.Kind == EnemyKind.Elite ? 20 : e.Kind == EnemyKind.Mage ? 16 : e.Kind is EnemyKind.InkEcho or EnemyKind.DrownedMemory or EnemyKind.EmberWisp or EnemyKind.NameMoth or EnemyKind.MudCantor or EnemyKind.PaperWraith or EnemyKind.SaltEcho ? 18 : 10);
            return;
        }
        if (e.Kind is EnemyKind.Archer or EnemyKind.InkEcho or EnemyKind.SaltEcho) { float preferred = e.Kind == EnemyKind.InkEcho ? 135 : e.Kind == EnemyKind.SaltEcho ? 155 : 170; if (dist < preferred) e.Pos = MoveEnemy(e, Scale(Normalize(Sub(e.Pos, player), PointF.Empty), (e.Kind == EnemyKind.InkEcho ? 68 : e.Kind == EnemyKind.SaltEcho ? 62 : 55) * dt)); else if (e.Cooldown <= 0) { BeginWindup(e, player, e.Kind == EnemyKind.InkEcho ? 0.75f : e.Kind == EnemyKind.SaltEcho ? 0.68f : 0.6f); e.Cooldown = e.Kind == EnemyKind.InkEcho ? 2.0f : e.Kind == EnemyKind.SaltEcho ? 1.85f : 1.8f; } SeparateFromPack(e, dt); return; }
        if (e.Kind is EnemyKind.DrownedMemory or EnemyKind.EmberWisp or EnemyKind.NameMoth or EnemyKind.MudCantor or EnemyKind.PaperWraith) { float preferred = e.Kind == EnemyKind.EmberWisp ? 145 : e.Kind == EnemyKind.NameMoth ? 175 : e.Kind == EnemyKind.MudCantor ? 215 : e.Kind == EnemyKind.PaperWraith ? 185 : 110; if (dist < preferred) e.Pos = MoveEnemy(e, Scale(Normalize(Sub(e.Pos, player), PointF.Empty), (e.Kind is EnemyKind.MudCantor or EnemyKind.PaperWraith ? 52 : 72) * dt)); else if (e.Cooldown <= 0) { BeginWindup(e, player, e.Kind == EnemyKind.EmberWisp ? 0.7f : e.Kind == EnemyKind.NameMoth ? 0.62f : e.Kind == EnemyKind.MudCantor ? 0.82f : e.Kind == EnemyKind.PaperWraith ? 0.68f : 0.9f); e.Cooldown = e.Kind == EnemyKind.EmberWisp ? 1.9f : e.Kind == EnemyKind.NameMoth ? 1.7f : e.Kind == EnemyKind.MudCantor ? 2.15f : e.Kind == EnemyKind.PaperWraith ? 1.95f : 2.25f; } SeparateFromPack(e, dt); return; }
        if (e.Kind == EnemyKind.Mage && dist < 165) e.Pos = MoveEnemy(e, Scale(Normalize(Sub(e.Pos, player), PointF.Empty), 42 * dt));
        if (e.Kind == EnemyKind.Elite && e.Cooldown <= 0 && dist < 220) { BeginWindup(e, player, e.Armored ? 0.72f : 0.5f); e.Cooldown = e.Armored ? 2.5f : 2f; return; }
        if (e.Boss && e.Cooldown <= 0 && (dist < 135 || (e.Kind is EnemyKind.BlindScribe or EnemyKind.RiverFerryman or EnemyKind.AshWarden or EnemyKind.HollowName or EnemyKind.NameWeaver or EnemyKind.FirstKeeper or EnemyKind.OathCartographer or EnemyKind.NameBroker or EnemyKind.TideWitness or EnemyKind.ReedKeeper or EnemyKind.FlowKeeper or EnemyKind.CounterCurrent))) { e.BossPattern = (e.Kind is EnemyKind.RiverFerryman or EnemyKind.AshWarden or EnemyKind.HollowName or EnemyKind.NameWeaver or EnemyKind.FirstKeeper or EnemyKind.OathCartographer or EnemyKind.NameBroker or EnemyKind.TideWitness or EnemyKind.ReedKeeper or EnemyKind.FlowKeeper or EnemyKind.CounterCurrent) ? (e.BossPattern + 1) % 3 : 1 - e.BossPattern; bool aimed = (e.Kind == EnemyKind.BlindScribe && e.BossPattern == 1) || ((e.Kind is EnemyKind.RiverFerryman or EnemyKind.AshWarden) && (e.BossPattern is 1 or 2)) || (e.Kind is EnemyKind.HollowName or EnemyKind.NameWeaver or EnemyKind.FirstKeeper or EnemyKind.OathCartographer or EnemyKind.NameBroker or EnemyKind.TideWitness or EnemyKind.ReedKeeper or EnemyKind.FlowKeeper or EnemyKind.CounterCurrent) && e.BossPattern is 1 or 2; float windup = e.Hp < e.MaxHp * 0.5f ? 0.52f : 0.78f; if (e.Kind == EnemyKind.FirstKeeper && e.BossPattern == 2) { PointF axis = Normalize(Sub(player, e.Pos), new PointF(1, 0)); PointF side = new(-axis.Y, axis.X); e.Target = Add(e.Pos, Scale(axis, 920)); e.Target2Start = Add(player, Scale(side, -460)); e.Target2 = Add(player, Scale(side, 460)); e.Windup = e.WindupMax = windup; } else BeginWindup(e, aimed ? player : e.Pos, windup); e.Cooldown = e.Hp < e.MaxHp * 0.5f ? 1.25f : 2.0f; return; }
        if (e.Kind == EnemyKind.Mage && e.Cooldown <= 0) { BeginWindup(e, player, 0.8f); e.Cooldown = 2.2f; SeparateFromPack(e, dt); return; }
        if (dist > (e.Boss ? 40 : 30)) e.Pos = MoveEnemy(e, Scale(Normalize(offset, PointF.Empty), (e.Boss ? 55 : 74 + room * 7) * dt));
        else if (e.Cooldown <= 0) { BeginWindup(e, player, 0.45f); e.Cooldown = 1.35f; }
        if (!e.Boss) SeparateFromPack(e, dt);
    }

    private static void BeginWindup(Enemy enemy, PointF target, float duration) { enemy.Target = target; enemy.Windup = enemy.WindupMax = duration; }
    private void HitPlayer(float value) { float damageTaken = value * (armorOn ? 0.7f : 1) * (wardTime > 0 ? 0.45f : 1); health = Math.Max(0, health - damageTaken); invulnerable = 0.6f; if (health <= 0) dead = true; }
    private void Collect(Drop item)
    {
        switch (item.Kind) { case "gold": gold += item.Amount; notice = $"+{item.Amount} OURO"; break; case "potion": potions++; notice = "+1 POÇÃO"; break; case "glass": tideGlass = Math.Min(999, tideGlass + item.Amount); notice = $"+{item.Amount} SAL VÍTREO"; break; case "weapon": if (item.Amount is < 1 or > 2) { drops.Remove(item); return; } RollWeapon(item.Amount); ownedWeapons |= 1 << item.Amount; notice = $"ARMA {WeaponRarityName(weaponRarities[item.Amount])}: {WeaponDisplayName(item.Amount)}"; break; case "ebb": questStage = 32; notice = "MEMÓRIA: O VEIO LIVRE"; ShowMemory("A CORRENTE NÃO É UMA ROTA", "O vigia mantinha o fluxo preso num único giro. Quando o canal se abre, a água volta a mudar de direção conforme a cheia. Aven poderá ensinar o Escutador a chamar a vazante sem obrigar ninguém a segui-la. A gravação termina antes de indicar para onde foram os viajantes."); break; case "counter": counterTrace = true; questStage = 35; notice = "MEMÓRIA: O COMPASSO DAS MARGENS"; ShowMemory("O AFINADOR DAS MARGENS", "O Afinador não guardava um mapa. Regulava a força que mantém os dois lados do rio em equilíbrio; alguém o convenceu de que só um fluxo constante poderia proteger a comunidade. Ao ceder, o mecanismo volta a alternar entre impulso e retorno. A gravação não contém nome, rota nem notícia de Nara."); break; case "beyond": beyondTrace = true; questStage = 29; notice = "SINAL DE PASSAGEM RECUPERADO"; ShowMemory("NÓS QUE PODEM SER DESFEITOS", "O sinal foi deixado por viajantes que passaram pela margem durante a cheia. Ele confirma apenas que o grupo cruzou junto e escolheu para onde seguir ao chegar à outra margem. Não indica qual margem, nome ou destino pertence a Nara."); break; case "witness": upstreamTrace = true; questStage = 26; notice = "MEMÓRIA: A TESTEMUNHA DA MARÉ"; ShowMemory("A ÚLTIMA CURVA DE NARA", "A testemunha não guarda um mapa nem um nome. Guarda a passagem de um grupo que atravessou o rio contra a corrente. Quando alguém a chamou pelo nome escrito no registro de Véspera, Nara respondeu com outro nome e pediu que o grupo o aprendesse. Eles aceitaram e seguiram juntos para além das águas. O eco se encerra antes de revelar a palavra escolhida ou o lugar a que chegaram."); break; case "register": questStage = 23; notice = "REGISTRO DE PASSAGEM RECUPERADO"; ShowMemory("OS NOMES QUE NÃO SÃO POSSE", "O livro contém rotas e relatos escritos por viajantes diferentes. Aven lê em voz alta apenas o trecho assinado por quem escolheu compartilhá-lo. Uma anotação de Nara confirma que ela seguiu adiante com o grupo, mas não revela seu nome atual nem seu paradeiro. O registro fica com a comunidade, não com Véspera."); break; case "chorus": chorusMemory = true; questStage = 14; notice = "MEMÓRIA: CORO DOS NOMES CONSUMIDOS"; ShowMemory("A TECELÃ DE NOMES", "Quando o fio se rompe, as vozes não se calam. O Coro: “Não somos uma coisa que veio de fora. Somos todos os nomes que o Lume tomou e não conseguiu guardar. Nara carregou parte de nós para longe, mas o farol continua nos chamando.” O Silêncio de Baixo é a pressão dessas memórias tentando voltar juntas."); break; case "first": firstMemory = true; questStage = 16; notice = "MEMÓRIA: O PRIMEIRO NOME"; ShowMemory("A PRIMEIRA VIGÍLIA", "A guardiã recorda seu nome: Ilyra, a primeira Escutadora. Ela não acendeu o Lume para apagar as vozes, mas para que toda memória encontrasse um lugar onde ser ouvida. Séculos depois, os guardiões inverteram o propósito e transformaram lembrança em combustível. Ilyra entregou o próprio nome para impedir que o farol esquecesse sua origem. No último eco, viu Nara atravessar a corrente levando vozes que o Lume não comportava. Não sabe o que aconteceu com ela depois, mas deixa um aviso: se alguém a encontrar, não deve chamá-la pelo nome que o farol escolheu."); break; case "west": westTrace = true; questStage = 19; notice = "MEMÓRIA: MAPA DA MARGEM ESCOLHIDA"; ShowMemory("O MAPA DE NARA", "A Cartógrafa guardava um registro de quem atravessou o Vau. Entre os traços recentes, você reconhece a letra de Nara: “Aqui, ninguém precisa conservar o nome que o Lume escolheu. Ajudei a levantar um abrigo e segui com os que queriam continuar a oeste. Se veio atrás de mim, não use o parentesco como resposta. Pergunte primeiro quem escolhi ser.” A tinta e o caminho são seus, mas o mapa não diz quando ela partiu nem onde está agora."); break; case "charm": if (item.Amount is < 1 or > 5) { drops.Remove(item); return; } bool newCharm = (ownedCharms & (1 << (item.Amount - 1))) == 0; ownedCharms |= 1 << (item.Amount - 1); if (equippedCharm == 0 && equippedCharm2 != item.Amount) equippedCharm = item.Amount; else if (equippedCharm2 == 0 && equippedCharm != item.Amount) equippedCharm2 = item.Amount; notice = newCharm ? $"TALISMÃ: {charmNames[item.Amount]}" : $"ECO DUPLICADO: {charmNames[item.Amount]}"; break; case "armor": armor = true; notice = "PEITORAL DE PEDRA ENCONTRADO"; break; case "relic": relic = true; notice = "FRAGMENTO DE VOZ ENCONTRADO"; ShowMemory("ECO PRESO NO RELICÁRIO", "Nara: “Eu vim por vontade própria. Se encontrou esta lembrança, procure o arquivo de Sileth.”"); break; case "seal": archiveSeal = true; damage += 8; questStage = 4; notice = "SELO DA VIGÍLIA • DANO +8"; ShowMemory("SELO DA VIGÍLIA", "O selo guarda a última gravação de Nara: “Sileth não apagou os nomes para alimentar o Lume. Ela tentou esconder que o próprio farol está aprendendo a escolher quem esquecer.” Leve esta prova de volta à Curadora."); break; case "bell": riverBell = true; maxHealth += 20; health = Math.Min(maxHealth, health + 20); questStage = 6; notice = "SINO DE NARA • VIDA MÁXIMA +20"; ShowMemory("O SINO SEM BADALO", "A marca no bronze pertence a Nara. A voz gravada é de uma mulher que atravessou o rio: “Não estou presa. O farol me escuta porque eu aceitei levar as memórias para longe da cidade. Se ainda me procura, não tente me trazer de volta antes de ouvir o que o rio guarda.”"); break; case "lens": lumeLens = true; damage += 12; questStage = 9; notice = "PRISMA DO LUME • DANO +12"; ShowMemory("O PRISMA DA PRIMEIRA VIGÍLIA", "A luz projeta Nara ao lado de Sileth, anos antes da noite dos sinos. Nara escolheu ser a portadora das lembranças que o Lume não conseguia guardar. A imagem se rompe antes de mostrar onde ela está agora."); break; case "trace": naraTrace = true; questStage = 11; notice = "RASTRO DE NARA • NOVA MEMÓRIA"; ShowMemory("A LEMBRANÇA QUE SOBROU", "O eco não mostra um rosto. Mostra Nara ensinando alguém, do outro lado do rio, a pronunciar um nome que Véspera apagou. Sua última frase não pede resgate: “Se a cidade ainda lembrar de nós, eu saberei voltar.”"); break; }
        drops.Remove(item); noticeTime = 1.7f;
    }

    private void RollWeapon(int index)
    {
        int rarityRoll = random.Next(100);
        int bonusRoll = random.Next(5);
        int affixRoll = random.Next(3);
        weaponRarities[index] = rarityRoll < 62 ? 0 : rarityRoll < 95 ? 1 : 2;
        weaponBonuses[index] = weaponRarities[index] switch { 0 => bonusRoll % 3, 1 => 3 + bonusRoll % 4, _ => 7 + bonusRoll % 5 };
        weaponAffixes[index] = weaponRarities[index] == 0 ? 0 : 1 + affixRoll;
    }

    private void RollWeapon(int index, int rarityRoll, int bonusRoll, int affixRoll)
    {
        weaponRarities[index] = rarityRoll < 62 ? 0 : rarityRoll < 95 ? 1 : 2;
        weaponBonuses[index] = weaponRarities[index] switch { 0 => bonusRoll % 3, 1 => 3 + bonusRoll % 4, _ => 7 + bonusRoll % 5 };
        weaponAffixes[index] = weaponRarities[index] == 0 ? 0 : 1 + affixRoll % 3;
    }

    private void UsePotion() { if (potions > 0 && health < maxHealth) { potions--; health = Math.Min(maxHealth, health + 45); notice = "+45 VIDA"; noticeTime = 1.3f; } else { notice = potions == 0 ? "SEM POÇÕES" : "VIDA JÁ ESTÁ CHEIA"; noticeTime = 1.3f; } }
    private void Interact()
    {
        if (room == 3) { InteractTown(); return; }
        if (room == 18) { InteractRefuge(); return; }
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
        else if (room == 12 && player.X < 115) { room = 11; SetupRoom(); }
        else if (room == 12 && player.X > 790) { room = 13; SetupRoom(); ShowMemory("CLAUSTRO DO CORO PARTIDO", "As vozes da costa convergem para uma abadia sem teto. No centro, uma figura costura nomes apagados com fios de água escura."); }
        else if (room == 13 && player.X < 115) { room = 12; SetupRoom(); }
        else if (room == 13 && questStage >= 15 && Distance(player, new PointF(480, 500)) < 64) { room = 14; SetupRoom(); ShowMemory("BACIA DO PRIMEIRO NOME", townEnding == 1 ? "Os nomes devolvidos aos sinos fazem a pedra responder. Uma escadaria escondida desce até uma bacia construída antes do Lume." : "A luz do Lume atravessa o piso do Claustro e revela degraus para uma bacia que não aparece nos mapas."); }
        else if (room == 13 && player.X > 790 && chorusMemory) { room = 3; SetupRoom(); ShowMemory("REGRESSO A VÉSPERA", "Você traz um coro inteiro, mas não uma voz única. Sileth precisa ouvi-lo para descobrir por que os nomes consumidos continuam chamando do fundo."); }
        else if (room == 14 && player.X < 115) { room = 13; SetupRoom(); }
        else if (room == 14 && player.X > 790) { room = 15; SetupRoom(); ShowMemory("SALA DA PRIMEIRA VIGÍLIA", "A bacia dá lugar a uma câmara circular. Uma guardiã sem rosto protege o primeiro nome que o Lume recebeu."); }
        else if (room == 15 && player.X < 115) { room = 14; SetupRoom(); }
        else if (room == 15 && player.X > 790 && firstMemory) { room = 3; SetupRoom(); ShowMemory("REGRESSO A VÉSPERA", "A guardiã não era a origem do Lume, mas guardava a lembrança de quem o acendeu. Sileth precisa ouvir o primeiro nome antes que a história avance."); }
        else if (room == 16 && player.X < 115) { room = 3; SetupRoom(); ShowMemory("REGRESSO A VÉSPERA", "Edda espera no cais da praça. O Vau pode ser cruzado novamente quando quiser seguir o mapa."); }
        else if (room == 16 && player.X > 790) { room = 17; SetupRoom(); ShowMemory("PÁTIO DAS TESTEMUNHAS", "As margens do Vau formam um pátio de tábuas. Uma cartógrafa apagou as placas dos caminhos; ela observa quem chega antes de decidir se a passagem continua aberta."); }
        else if (room == 17 && player.X < 115) { room = 16; SetupRoom(); }
        else if (room == 17 && player.X > 790 && westTrace) { room = 3; SetupRoom(); ShowMemory("REGRESSO A VÉSPERA", "O mapa de Nara aponta além do abrigo, mas a letra não revela quando ela partiu. Edda saberá levar vocês contra a corrente."); }
        else if (room == 19 && player.X < 115) { room = 18; SetupRoom(); }
        else if (room == 19 && player.X > 790) { room = 20; SetupRoom(); ShowMemory("CISTERNA DO REGISTRO VAZIO", "A cisterna foi construída para guardar água, mas alguém a cobriu de contratos. Um Escrivão de Contrato protege o volume e ataca qualquer pessoa cujo nome consiga ler."); }
        else if (room == 20 && player.X < 115) { room = 19; SetupRoom(); }
        else if (room == 20 && player.X > 790 && questStage >= 23) { room = 18; SetupRoom(); }
        else if (room == 21 && player.X < 115) { room = 18; SetupRoom(); }
        else if (room == 21 && player.X > 790) { room = 22; SetupRoom(); ShowMemory("PONTE DA MARÉ INVERTIDA", "A ponte antiga cruza a nascente por onde as vozes seguem contra a corrente. Uma figura cobre as tábuas com marcas de água salgada. Ela não repete nenhum nome; espera para ouvir o que você veio perguntar."); }
        else if (room == 22 && player.X < 115) { room = 21; SetupRoom(); }
        else if (room == 22 && player.X > 790 && questStage >= 27) { if (questStage == 27) questStage = 28; room = 23; SetupRoom(); ShowMemory("COTOVELO DOS JUNCAIS", "Depois da ponte, o rio se abre num banco baixo de juncos. Pares de nós indicam onde a água ainda permite passagem; nenhum aponta para uma pessoa ou destino."); }
        else if (room == 22 && player.X > 790 && questStage >= 26) { room = 18; SetupRoom(); ShowMemory("VOLTA À MESA COMUNITÁRIA", "A lembrança da nascente cabe numa frase curta. Aven pode guardá-la sem escrever por Nara."); }
        else if (room == 23 && player.X < 115) { room = 22; SetupRoom(); }
        else if (room == 23 && player.X > 790) { room = 24; SetupRoom(); ShowMemory("PÁTIO DAS AMARRAS LIVRES", "Nós de junco formam sinais que podem ser desfeitos pela própria pessoa que os amarrou. A Guardiã do Leito os prende todos numa única rota, como se manter um caminho aberto significasse escolher por quem passa."); }
        else if (room == 24 && player.X < 115) { room = 23; SetupRoom(); }
        else if (room == 24 && player.X > 790 && questStage >= 29) { room = 18; SetupRoom(); ShowMemory("O LEITO SEGUE ABERTO", "A margem não deixa um mapa de Nara. Deixa um sinal comum: o grupo atravessou durante a cheia e decidiu qual margem seguir quando chegou ao outro lado."); }
        else if (room == 25 && player.X < 115) { room = 18; SetupRoom(); }
        else if (room == 25 && player.X > 790) { room = 26; SetupRoom(); ShowMemory("CÂMARA DO VEIO PRESO", "Debaixo dos moinhos, as rodas pararam sem quebrar. Um vigia mantém a água girando num círculo fechado; cada golpe muda o sentido da corrente."); }
        else if (room == 26 && player.X < 115) { room = 25; SetupRoom(); }
        else if (room == 26 && player.X > 790 && questStage >= 32) { room = 18; SetupRoom(); ShowMemory("A CORRENTE DEVOLVIDA", "O fluxo volta a encontrar saída. Aven pode ouvir o relato e guardar o que foi aprendido sem transformar a margem em mapa."); }
        else if (room == 27 && player.X < 115) { room = 18; SetupRoom(); }
        else if (room == 27 && player.X > 790) { room = 28; SetupRoom(); ShowMemory("CORAÇÃO DAS DUAS MARGENS", "O círculo de pedra alterna entre puxar e repelir. No centro, o Afinador das Margens prepara um golpe diferente para cada sentido da corrente. Observe os sinais no chão e atravesse a bacia quando o fluxo se inverter."); }
        else if (room == 28 && player.X < 115) { room = 27; SetupRoom(); }
        else if (room == 28 && player.X > 790 && questStage >= 35) { room = 18; SetupRoom(); ShowMemory("A ESCARPA RESPIRA", "O fluxo já não se fecha sobre o próprio centro. A memória do Afinador pode ser levada a Aven para que a comunidade registre o que aprendeu sem transformar o caminho em ordem."); }
        else if (room == 11 && player.X > 790 && questStage >= 13) { room = 12; SetupRoom(); ShowMemory("COSTA DAS VOZES TARDIAS", townEnding == 1 ? "As badaladas com os nomes de Véspera atravessam o rio. Ecos se afastam da trilha; a cidade devolveu força a estas vozes." : "O Lume permanece aceso atrás de você. Sua luz atravessa as águas sem reflexo e atrai para a costa tudo o que foi esquecido."); }
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
            else if (questStage == 14 && chorusMemory) { questStage = 15; gold += 200; potions++; ShowMemory("O CORO CHEGA À PRAÇA", "As vozes não são uma criatura que possa ser destruída. Cada nome que o Lume consome aumenta a pressão sob a cidade. Sileth registra o Coro sem apagar nenhuma versão: “Se Nara ainda está entre essas vozes, temos de escutar antes de decidir como chamá-la de volta.” O rastro aponta para além dos mapas de Véspera."); notice = "MISSÃO CONCLUÍDA: 200 OURO E 1 POÇÃO"; noticeTime = 3; }
            else if (questStage == 16 && firstMemory) { questStage = 17; gold += 260; potions++; ShowMemory("O NOME DE ILYRA", "A primeira memória devolve um propósito ao Lume, mas não desfaz séculos de escolhas. Sileth anota a versão de Ilyra ao lado das demais, sem fechar o arquivo: “Nara atravessou o rio por vontade própria. Se a encontrarmos, teremos de escutar o nome que ela escolheu para si.” Edda ouviu o Coro seguir para o oeste, contra a corrente, em direção a terras que Véspera nunca registrou."); notice = "MISSÃO CONCLUÍDA: 260 OURO E 1 POÇÃO"; noticeTime = 3; }
            else if (questStage >= 17) ShowMemory("CURADORA SILETH", "O arquivo permanece aberto. Ilyra aponta para o oeste, contra a corrente do Rio Cego. Edda prepara um barco; quando a trilha estiver pronta, seguiremos para além das terras conhecidas.");
            else if (questStage >= 12 && townEnding == 0) { endingChoiceOpen = true; notice = "VÉSPERA ESPERA SUA DECISÃO • 1 OU 2"; noticeTime = 3; }
            else if (townEnding == 1) ShowMemory("CURADORA SILETH", "Os nomes voltaram aos sinos. A praça está com você, mas as vozes além do reservatório ainda faltam. Vamos procurá-las sem fechar o arquivo outra vez.");
            else if (townEnding == 2) ShowMemory("CURADORA SILETH", "O Lume continua aceso e os arquivos permanecem abertos. O Silêncio não desapareceu; siga o rastro para além do reservatório antes que a cidade dependa de novo de nomes apagados.");
            else ShowMemory("CURADORA SILETH", "O rastro de Nara segue pelo leito do Rio Cego. Com o sino de Edda, a passagem abaixo da capela pode ser aberta.");
            return;
        }
        if (Distance(player, BellKeeperPosition) < 58)
        {
            if (questStage == 6 && riverBell) { questStage = 7; gold += 100; potions++; ShowMemory("EDDA RECONHECE O SINO", "Esse era o sino de travessia do meu pai. Nara o levou quando desceu ao rio; ela conhecia o caminho e decidiu cruzá-lo. Não foi capturada. Guarde o sino: se ela voltar, vai saber que ainda lembramos seu nome."); notice = "MISSÃO CONCLUÍDA: 100 OURO E 1 POÇÃO"; noticeTime = 3; }
            else if (questStage == 19 && westTrace) { questStage = 20; gold += 375; potions++; ShowMemory("A CARTA DO REFÚGIO", "Nara construiu um lugar onde cada pessoa escolhe como será chamada e partiu para oeste com quem pediu para atravessar. Edda reconhece as marcas do mapa: “Consigo levar vocês até a próxima curva, mas dali em diante será preciso seguir a água.” Sileth deixa o arquivo aberto para a volta do grupo. A notícia não prova onde Nara está agora."); notice = "RECOMPENSA: 375 OURO E 1 POÇÃO"; noticeTime = 3; }
            else if (questStage is 17 or 18) { questStage = 18; room = 16; SetupRoom(); ShowMemory("A TRAVESSIA DO CONTRA-RIO", "Edda solta o barco de fundo largo. Os nomes devolvidos aos sinos ajudam a encontrar a passagem entre as pedras; o Lume mantido aceso ilumina os marcos submersos, mas chama ecos para o Vau. A cartógrafa da margem guarda a entrada do refúgio de Nara."); }
            else if (questStage == 20) { questStage = 21; room = 18; SetupRoom(); ShowMemory("REFÚGIO DO VEIO CALMO", "O barco de Edda segue as lanternas até uma embarcação antiga, encalhada entre as pedras. Pessoas que partiram de Véspera construíram ali um abrigo. Nara ajudou a traçar as primeiras rotas, mas continuou viagem com quem escolheu partir. Aven, o antigo barqueiro, espera junto à mesa comunitária."); }
            else if (questStage >= 21) ShowMemory("EDDA, NO CAIS", "O mapa de Nara marca outra curva a oeste, além do refúgio. Ainda não sabemos onde ela está; a trilha continua aberta.");
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
    private void InteractRefuge()
    {
        if (Distance(player, AvenPosition) < 60)
        {
            if (questStage == 21) { questStage = 22; ShowMemory("AVEN, O BARQUEIRO", "Não mantemos um livro de nomes. Cada viajante decide o que quer deixar na mesa e quem pode ler. Nara ajudou a marcar a Trilha dos Luminários e seguiu adiante com outras pessoas. Ela pediu que não contássemos o nome que escolheu nem prometêssemos que voltaria. Se quiser continuar, proteja os avisos que cada pessoa decidiu compartilhar."); }
            else if (questStage == 23) { questStage = 24; gold += 240; potions += 2; ShowMemory("A MESA CONTINUA ABERTA", "A comunidade escolhe guardar os registros de passagem aqui. A nota de Nara termina antes da próxima curva; ninguém vai preencher a lacuna por ela. Aven oferece provisões para a viagem e aponta as lanternas mais recentes para oeste. Se um dia ela voltar, será ela quem decidirá o que contar."); notice = "REFÚGIO: 240 OURO E 2 POÇÕES"; noticeTime = 3; }
            else if (questStage == 26 && upstreamTrace) { questStage = 27; gold += 320; potions++; ShowMemory("A TESTEMUNHA SEM REGISTRO", "Aven ouve a lembrança sem perguntar qual era o nome anterior nem o escolhido por Nara. Ele registra apenas que um grupo chegou junto à nascente e continuou viagem por vontade própria. “Se você ainda quer encontrá-la, a próxima pergunta precisa ser feita a ela. A nossa comunidade não vai responder em seu lugar.” A trilha segue além do alcance das lanternas."); notice = "RECOMPENSA: 320 OURO E 1 POÇÃO"; noticeTime = 3; }
            else if (questStage == 29 && beyondTrace) { questStage = 30; gold += 420; potions++; ShowMemory("AVEN GUARDA O SINAL", "A marca confirma que o grupo de Nara cruzou a cheia e escolheu o próprio rumo ao chegar à margem. Aven a prende à mesa comunitária sem copiar o trajeto: “Esse relato diz que escolheram. Não diz que alguém precisa segui-los.” Ele oferece provisões e mantém o caminho aberto para quem vier depois."); notice = "RECOMPENSA: 420 OURO E 1 POÇÃO"; noticeTime = 3; }
        else if (questStage == 32) { questStage = 33; ebbSeal = true; gold += 480; potions += 2; ShowMemory("A MARCA DA VAZANTE", "Aven entrega um selo de bronze que abre uma passagem curta na água ao redor do Escutador. Ele não controla a corrente: por um instante, reúne os ecos próximos para que possam ser enfrentados juntos. “Use para abrir espaço, não para escolher o rumo de alguém.” A habilidade Vazante foi liberada."); notice = "RECOMPENSA: 480 OURO • VAZANTE LIBERADA"; noticeTime = 3; }
            else if (questStage == 35 && counterTrace) { questStage = 36; anchorWard = true; gold += 560; tideGlass += 3; potions += 2; ShowMemory("A CORRENTE TEM DUAS MARGENS", "Aven escuta o relato do Coração e acrescenta ao mapa comunitário apenas isto: a passagem pode mudar de sentido sem deixar de ser passagem. Ele ensina o Manto de Lastro, que fecha a corrente ao redor do Escutador por um instante e reduz o dano recebido. Entrega também sal vítreo e provisões; nenhuma direção é marcada em nome de quem não está aqui."); notice = "RECOMPENSA: MANTO DE LASTRO • 560 OURO • 3 SAL"; noticeTime = 3; }
            else if (questStage >= 27) { forgeMenuOpen = true; return; }
            else if (questStage >= 24) ShowMemory("AVEN, O BARQUEIRO", "A mesa continua aberta a quem escolhe deixar um relato. A próxima curva fica a oeste; os viajantes que passaram por aqui decidiram compartilhar apenas o início da trilha.");
            else ShowMemory("AVEN, O BARQUEIRO", "Este barco encalhado virou abrigo. Ninguém precisa escrever um nome para ficar aqui.");
            return;
        }
        if (Distance(player, SeraPosition) < 58) { ShowMemory("SERA, REPARADORA DE LANTERNAS", "As lanternas não marcam uma rota para tomar posse dela. Cada pessoa acende uma só quando quer deixar um aviso para quem vier depois."); return; }
        if (Distance(player, NoaPosition) < 58) { ShowMemory("NOA, APRENDIZ DE CARTÓGRAFA", "Aprendi a desenhar os trechos que meus amigos me deixam ver. O resto do mapa fica em branco por escolha deles."); return; }
        if (player.X < 150) { room = 3; SetupRoom(); return; }
        if (player.X > 790 && questStage >= 33) { if (questStage == 33) questStage = 34; room = 27; SetupRoom(); ShowMemory("ESCARPA DO CONTRAFLUXO", "A água que saiu dos moinhos sobe por uma escarpa de pedra negra. As marcas nas paredes se alternam: umas apontam para dentro, outras para fora. Mais adiante, o Coração das Duas Margens pulsa sem escolher qual delas deve receber a corrente."); return; }
        if (player.X > 790 && questStage >= 30) { if (questStage == 30) questStage = 31; room = 25; SetupRoom(); ShowMemory("MOINHOS DA VAZANTE", "Aven reconhece o ruído das rodas de água: elas não moíam grão, mas regulavam a cheia para que a comunidade pudesse atravessar. O canal foi fechado por dentro. A água ainda empurra em direção à câmara."); return; }
        if (player.X > 790 && questStage >= 24) { if (questStage == 24) questStage = 25; room = 21; SetupRoom(); ShowMemory("A VÁRZEA DAS VOZES SOPRADAS", "Além da cisterna, o rio se divide entre campos de sal e pedra lisa. As lanternas acabam aqui. Marcas recentes mostram que viajantes cruzaram a água sem deixar um destino escrito."); return; }
        if (player.X > 790 && questStage >= 22) { room = 19; SetupRoom(); ShowMemory("TRILHA DOS LUMINÁRIOS", "Pequenas lanternas indicam os pontos onde viajantes aceitaram deixar um aviso. As marcas não formam um mapa completo: cada pessoa decidiu quanto do caminho compartilhar."); return; }
        notice = questStage < 22 ? "FALE COM AVEN PARA CONHECER A COMUNIDADE" : "E  SEGUIR A TRILHA DOS LUMINÁRIOS"; noticeTime = 1.5f;
    }
    private void ShowMemory(string titleText, string bodyText) { memoryTitle = titleText; memoryText = bodyText; memoryOverlay = true; }
    private void MarkRoomCleared() { if (room != 3 && enemies.Count == 0 && drops.Count == 0) clearedRooms |= 1 << room; }
    private Enemy? lockedTarget;
    private void CycleTarget() { if (enemies.Count == 0) { lockedTarget = null; return; } int i = lockedTarget is null ? -1 : enemies.IndexOf(lockedTarget); lockedTarget = enemies[(i + 1) % enemies.Count]; notice = $"ALVO: {EnemyName(lockedTarget)}"; noticeTime = 1.4f; }
    private void EquipWeapon(int index) { if ((ownedWeapons & (1 << index)) == 0) { notice = "ARMA AINDA NÃO ENCONTRADA"; noticeTime = 1.4f; return; } damage += WeaponPower(index) - WeaponPower(weapon); weapon = index; }
    private void CycleCharm(int slot)
    {
        int current = slot == 1 ? equippedCharm : equippedCharm2;
        int other = slot == 1 ? equippedCharm2 : equippedCharm;
        for (int i = 1; i <= 5; i++)
        {
            int candidate = ((current - 1 + i + 5) % 5) + 1;
            if (candidate == current || candidate == other || (ownedCharms & (1 << (candidate - 1))) == 0) continue;
            if (slot == 1) equippedCharm = candidate; else equippedCharm2 = candidate;
            notice = $"ESPAÇO {slot}: {charmNames[candidate]}";
            noticeTime = 1.6f; return;
        }
        if (current != 0) { if (slot == 1) equippedCharm = 0; else equippedCharm2 = 0; notice = $"ESPAÇO {slot} DE TALISMÃ VAZIO"; }
        else notice = "NENHUM TALISMÃ DISPONÍVEL PARA ESTE ESPAÇO";
        noticeTime = 1.6f;
    }
    private bool HasCharm(int charm) => equippedCharm == charm || equippedCharm2 == charm;
    private bool HasCharmPair(int first, int second) => equippedCharm == first && equippedCharm2 == second || equippedCharm == second && equippedCharm2 == first;
    private float WhirlwindMultiplier() => HasCharmPair(1, 2) ? 2.52f : HasCharm(1) ? 2.16f : 1.6f;
    private float WhirlwindCooldownPenalty() => !HasCharm(1) ? 0f : HasCharmPair(1, 2) ? 0.5f : 1f;
    private float VoiceShardDamage() => damage * (HasCharmPair(3, 5) ? 2.05f : HasCharm(3) ? 1.8f : 1.35f) * WeaponSkillMultiplier();
    private float VoiceShardLifetime() => HasCharm(5) ? 1.8f : 1.25f;
    private float VoiceShardCooldown() => Math.Max(3.5f, 7f - skillTalent * 0.5f) + (HasCharm(5) ? HasCharmPair(3, 5) ? 0.5f : 1.25f : 0f);
    private float OutgoingDamageMultiplier(Enemy enemy) => !HasCharm(4) ? 1f : enemy.Boss ? 1.25f : 0.8f;
    private float DashDuration() => HasCharm(2) ? 0.29f : 0.17f;
    private float DashCooldown() => Math.Max(0.35f, 1.15f - dodgeTalent * 0.12f) + (HasCharm(2) && !HasCharmPair(2, 3) ? 0.3f : 0f);
    private float CriticalChance() => (weapon == 2 ? 0.25f : 0.12f) + (HasCharm(3) ? HasCharmPair(1, 3) ? 0.17f : 0.12f : 0f) + (weaponAffixes[weapon] == 2 ? weaponRarities[weapon] == 2 ? 0.11f : 0.07f : 0f);

    private static bool ValidWeaponData(SaveData s)
    {
        if (s.WeaponBonuses is null || s.WeaponBonuses.Length != 3 || s.WeaponRarities is null || s.WeaponRarities.Length != 3 || s.WeaponAffixes is null || s.WeaponAffixes.Length != 3) return false;
        for (int i = 0; i < 3; i++)
        {
            int rarity = s.WeaponRarities[i], bonus = s.WeaponBonuses[i], affix = s.WeaponAffixes[i];
            if (rarity == 0 ? bonus is < 0 or > 2 || affix != 0 : rarity == 1 ? bonus is < 3 or > 6 || affix is < 1 or > 3 : rarity == 2 ? bonus is < 7 or > 11 || affix is < 1 or > 3 : true) return false;
            if ((s.OwnedWeapons & (1 << i)) == 0 && (rarity != 0 || bonus != 0 || affix != 0)) return false;
        }
        return true;
    }
    private void RestoreWeaponData(SaveData s) { Array.Copy(s.WeaponBonuses, weaponBonuses, 3); Array.Copy(s.WeaponRarities, weaponRarities, 3); Array.Copy(s.WeaponAffixes, weaponAffixes, 3); }
    private float CriticalMultiplier() => HasCharm(3) ? HasCharmPair(1, 3) ? 2.75f : 2.5f : 2f;
    private float BasicAttackCooldown() => (weapon == 2 ? 0.23f : 0.33f) + (HasCharm(3) ? HasCharmPair(1, 3) ? 0.03f : 0.06f : 0f);
    private PointF MoveEnemy(Enemy e, PointF delta) { PointF p = e.Pos; PointF x = new(p.X + delta.X, p.Y), y = new(p.X, p.Y + delta.Y); if (!Blocked(x, e.Radius)) p.X = x.X; if (!Blocked(new PointF(p.X, y.Y), e.Radius)) p.Y = y.Y; return new PointF(Math.Clamp(p.X, 85, 875), Math.Clamp(p.Y, 128, 533)); }
    private void SeparateFromPack(Enemy enemy, float dt) { PointF push = PointF.Empty; foreach (Enemy other in enemies) { if (ReferenceEquals(enemy, other)) continue; PointF away = Sub(enemy.Pos, other.Pos); float d = Length(away); if (d > 0.01f && d < 40) push = Add(push, Scale(away, (40 - d) / (40 * d))); } if (Length(push) > 0.01f) enemy.Pos = MoveEnemy(enemy, Scale(Normalize(push, PointF.Empty), 52 * dt)); }
    private void MovePlayer(PointF delta) { PointF x = new(player.X + delta.X, player.Y), y = new(player.X, player.Y + delta.Y); if (!Blocked(x)) player.X = x.X; if (!Blocked(new PointF(player.X, y.Y))) player.Y = y.Y; player.X = Math.Clamp(player.X, 85, 875); player.Y = Math.Clamp(player.Y, 128, 533); }
    private bool Blocked(PointF p, float radius = 12) { RectangleF body = new(p.X - radius, p.Y - radius, radius * 2, radius * 2); RectangleF[] obstacles = room == 3 ? TownBuildings : room == 18 ? RefugeWalls : room == 19 ? RidgeWalls : room == 20 ? LedgerWalls : room == 21 ? SaltMarshWalls : room == 22 ? TideBridgeWalls : room == 23 ? ReedRunWalls : room == 24 ? ReedYardWalls : room == 25 ? FloodMillWalls : room == 26 ? SiltChamberWalls : room == 27 ? CounterSlopeWalls : room == 28 ? CounterHeartWalls : room == 4 ? ArchiveWalls : room == 5 ? VaultWalls : room == 6 ? RiverWalls : room == 7 ? BellChamberWalls : room == 8 ? AshGalleryWalls : room == 9 ? LumeChamberWalls : room == 10 ? NameShoreWalls : room == 11 ? VoiceWellWalls : room == 12 ? FarReachWalls : room == 13 ? NameCloisterWalls : room == 14 ? FirstBasinWalls : room == 15 ? FirstVigilWalls : room == 16 ? WestFordWalls : room == 17 ? WitnessYardWalls : Walls; return obstacles.Any(w => w.IntersectsWith(body)); }
    private bool BlockedLine(PointF a, PointF b)
    {
        int steps = Math.Max(1, (int)(Distance(a, b) / 6));
        RectangleF[] obstacles = room == 18 ? RefugeWalls : room == 19 ? RidgeWalls : room == 20 ? LedgerWalls : room == 21 ? SaltMarshWalls : room == 22 ? TideBridgeWalls : room == 23 ? ReedRunWalls : room == 24 ? ReedYardWalls : room == 25 ? FloodMillWalls : room == 26 ? SiltChamberWalls : room == 27 ? CounterSlopeWalls : room == 28 ? CounterHeartWalls : room == 4 ? ArchiveWalls : room == 5 ? VaultWalls : room == 6 ? RiverWalls : room == 7 ? BellChamberWalls : room == 8 ? AshGalleryWalls : room == 9 ? LumeChamberWalls : room == 10 ? NameShoreWalls : room == 11 ? VoiceWellWalls : room == 12 ? FarReachWalls : room == 13 ? NameCloisterWalls : Walls;
        for (int i = 0; i <= steps; i++) { float t = i / (float)steps; PointF p = new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t); if (obstacles.Any(w => w.Contains(p))) return true; }
        return false;
    }

    private SaveData CaptureSaveData() => new() { Room = room, Level = level, Xp = xp, Gold = gold, Potions = potions, Damage = damage, Health = health, MaxHealth = maxHealth, Weapon = weapon, OwnedWeapons = ownedWeapons, Armor = armor, Relic = relic, HealthTalent = healthTalent, SkillTalent = skillTalent, DodgeTalent = dodgeTalent, RoomCleared = room == 3 || enemies.Count == 0 && drops.Count == 0, Completed = victory, MemoriesFound = memoriesFound, QuestStage = questStage, ClearedRooms = clearedRooms | (room != 3 && enemies.Count == 0 && drops.Count == 0 ? 1 << room : 0), ArchiveSeal = archiveSeal, RiverBell = riverBell, LumeLens = lumeLens, NaraTrace = naraTrace, ChorusMemory = chorusMemory, FirstMemory = firstMemory, WestTrace = westTrace, UpstreamTrace = upstreamTrace, TideGlass = tideGlass, BeyondTrace = beyondTrace, EbbSeal = ebbSeal, CounterTrace = counterTrace, AnchorWard = anchorWard, OwnedCharms = ownedCharms, EquippedCharm = equippedCharm, EquippedCharm2 = equippedCharm2, TownEnding = townEnding, WeaponBonuses = weaponBonuses.ToArray(), WeaponRarities = weaponRarities.ToArray(), WeaponAffixes = weaponAffixes.ToArray() };
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
            if (!ValidWeaponData(s) || s.Room is < 0 or > 28 || s.Level is < 1 or > 10000 || s.Xp < 0 || s.Xp >= s.Level * 60 || s.Weapon is < 0 or > 2 || s.OwnedWeapons < 1 || s.OwnedWeapons > 7 || (s.OwnedWeapons & (1 << s.Weapon)) == 0 || s.MaxHealth < 1 || s.Health <= 0 || s.Health > s.MaxHealth || s.Potions < 0 || s.Gold < 0 || s.TideGlass is < 0 or > 999 || s.Damage < 1 || s.MemoriesFound is < 0 or > 3 || s.QuestStage is < 0 or > 36 || s.ClearedRooms is < 0 or > 536870911 || s.Completed && s.Room != 2 || s.OwnedCharms is < 0 or > 31 || s.EquippedCharm is < 0 or > 5 || s.EquippedCharm2 is < 0 or > 5 || s.EquippedCharm > 0 && (s.OwnedCharms & (1 << (s.EquippedCharm - 1))) == 0 || s.EquippedCharm2 > 0 && (s.OwnedCharms & (1 << (s.EquippedCharm2 - 1))) == 0 || s.EquippedCharm > 0 && s.EquippedCharm == s.EquippedCharm2 || s.TownEnding is < 0 or > 2 || s.QuestStage == 13 && s.TownEnding == 0 || s.QuestStage >= 14 && !s.ChorusMemory || s.QuestStage >= 16 && !s.FirstMemory || s.QuestStage >= 19 && !s.WestTrace || s.WestTrace && s.QuestStage < 19 || s.UpstreamTrace && s.QuestStage < 26 || s.QuestStage >= 26 && !s.UpstreamTrace || s.BeyondTrace && s.QuestStage < 29 || s.QuestStage >= 29 && !s.BeyondTrace || s.EbbSeal && s.QuestStage < 33 || s.QuestStage >= 33 && !s.EbbSeal || s.CounterTrace && s.QuestStage < 35 || s.QuestStage >= 35 && !s.CounterTrace || s.AnchorWard && s.QuestStage < 36 || s.QuestStage >= 36 && !s.AnchorWard || s.Room >= 27 && s.QuestStage < 34 || s.Room >= 25 && s.QuestStage < 31 || s.Room >= 23 && s.QuestStage < 28 || s.Room >= 21 && s.QuestStage < 25 || s.Room >= 14 && s.QuestStage < 15 || s.Room >= 16 && s.QuestStage < 18 || s.Room >= 18 && s.QuestStage < 21 || s.Room >= 19 && s.QuestStage < 22 || s.Room == 20 && s.QuestStage < 22) throw new InvalidDataException();
            StartGame(); room = s.Room; level = s.Level; xp = s.Xp; gold = s.Gold; potions = s.Potions; damage = s.Damage; health = s.Health; maxHealth = s.MaxHealth; weapon = s.Weapon; ownedWeapons = s.OwnedWeapons; RestoreWeaponData(s); armor = s.Armor; relic = s.Relic; healthTalent = s.HealthTalent; skillTalent = s.SkillTalent; dodgeTalent = s.DodgeTalent; memoriesFound = s.MemoriesFound; questStage = s.QuestStage; clearedRooms = s.ClearedRooms | (s.RoomCleared && s.Room != 3 ? 1 << s.Room : 0); archiveSeal = s.ArchiveSeal; riverBell = s.RiverBell; lumeLens = s.LumeLens; naraTrace = s.NaraTrace; chorusMemory = s.ChorusMemory; firstMemory = s.FirstMemory; westTrace = s.WestTrace; upstreamTrace = s.UpstreamTrace; tideGlass = s.TideGlass; beyondTrace = s.BeyondTrace; ebbSeal = s.EbbSeal; counterTrace = s.CounterTrace; anchorWard = s.AnchorWard; ownedCharms = s.OwnedCharms; equippedCharm = s.EquippedCharm; equippedCharm2 = s.EquippedCharm2; townEnding = s.TownEnding; SetupRoom(); if (s.RoomCleared) enemies.Clear(); victory = s.Completed; title = false; storyOverlay = false;
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
        else if (room == 18) DrawRefugeEnvironment(g, floor, tile);
        else if (room is 19 or 20) DrawLedgerTrailEnvironment(g, floor, tile);
        else if (room is 21 or 22) DrawUpstreamEnvironment(g, floor, tile);
        else if (room is 23 or 24) DrawReedBankEnvironment(g, floor, tile);
        else if (room is 25 or 26) DrawFloodMillEnvironment(g, floor, tile);
        else if (room is 27 or 28) DrawCounterflowEnvironment(g, floor, tile);
        else if (room is 4 or 5) DrawArchiveEnvironment(g, floor, tile);
        else if (room is 6 or 7) DrawRiverEnvironment(g, floor, tile);
        else if (room is 16 or 17) DrawWestWitnessEnvironment(g, floor, tile);
        else if (room is 14 or 15) DrawFirstVigilEnvironment(g, floor, tile);
        else if (room is 12 or 13) DrawFarReachEnvironment(g, floor, tile);
        else if (room >= 10) DrawNameShoreEnvironment(g, floor, tile);
        else if (room >= 8) DrawLumeEnvironment(g, floor, tile);
        else
        {
            g.FillRectangle(floor, 65, 108, 830, 445); for (int y = 0; y < 7; y++) for (int x = 0; x < 12; x++) g.DrawRectangle(tile, 70 + x * 68, 112 + y * 62, 67, 61);
            using var wallBrush = new SolidBrush(Color.FromArgb(90, 79, 62)); using var wallEdge = new Pen(Color.FromArgb(154, 132, 91), 3); foreach (RectangleF wall in Walls) { g.FillRectangle(wallBrush, wall); g.DrawRectangle(wallEdge, wall.X, wall.Y, wall.Width, wall.Height); }
            using var exitPen = new Pen(enemies.Count == 0 && drops.Count == 0 ? Color.LightGreen : Color.FromArgb(45, 48, 43), 8); g.DrawRectangle(exitPen, 882, 293, 3, 75);
        }
        if (bellWaveTime > 0) DrawBellWave(g);
        if (pullWaveTime > 0) DrawEbbWave(g);
        foreach (EchoShard shard in echoShards) DrawEchoShard(g, shard);
        foreach (Enemy enemy in enemies) DrawEnemy(g, enemy);
        foreach (Drop item in drops) DrawDrop(g, item);
        DrawPlayer(g);
        g.DrawString(room == 3 || room == 18 ? roomNames[room] : room < 3 ? $"{room + 1} / 3   {roomNames[room]}" : room < 6 ? $"{room - 3} / 2   {roomNames[room]}" : room < 8 ? $"{room - 5} / 2   {roomNames[room]}" : room < 10 ? $"{room - 7} / 2   {roomNames[room]}" : room < 12 ? $"{room - 9} / 2   {roomNames[room]}" : room < 14 ? $"{room - 11} / 2   {roomNames[room]}" : room < 16 ? $"{room - 13} / 2   {roomNames[room]}" : room < 18 ? $"{room - 15} / 2   {roomNames[room]}" : room < 21 ? $"{room - 18} / 2   {roomNames[room]}" : room < 23 ? $"{room - 20} / 2   {roomNames[room]}" : room < 25 ? $"{room - 22} / 2   {roomNames[room]}" : room < 27 ? $"{room - 24} / 2   {roomNames[room]}" : $"{room - 26} / 2   {roomNames[room]}", roomFont, goldBrush, 67, 572);
        int memoriesRead = (memoriesFound & 1) + ((memoriesFound >> 1) & 1);
        g.DrawString($"ECOS {memoriesRead} / 2", hudFont, goldBrush, 760, 572);
        if (room == 3) g.DrawString(TownObjective(), hudFont, goldBrush, 67, 592);
        else if (room == 18) g.DrawString("OBJETIVO: " + (questStage < 22 ? "CONHEÇA A COMUNIDADE E FALE COM AVEN" : questStage == 22 ? "SIGA A TRILHA E RECUPERE O REGISTRO COMPARTILHADO" : questStage == 23 ? "LEVE O REGISTRO DE VOLTA A AVEN" : questStage == 32 ? "LEVE A MEMÓRIA DA VAZANTE A AVEN" : questStage == 30 ? "SIGA O SOM DOS MOINHOS PARA OESTE" : questStage == 35 ? "LEVE A MEMÓRIA DO COMPASSO A AVEN" : questStage >= 36 ? "MANTO DE LASTRO APRENDIDO • SIGA AS ÁGUAS" : questStage >= 33 ? "SIGA A CORRENTE AO CORAÇÃO DAS DUAS MARGENS" : questStage >= 27 ? "FALE COM AVEN PARA REFORJAR UMA ARMA" : "SIGA A PRÓXIMA CURVA A OESTE"), hudFont, goldBrush, 67, 592);
        else if (room is 27 or 28) g.DrawString(room == 27 ? "OBJETIVO: ATRAVESSE A ESCARPA DO CONTRAFLUXO" : questStage < 35 ? "OBJETIVO: DERROTE O AFINADOR E RECOLHA A MEMÓRIA" : "OBJETIVO: RETORNE AO REFÚGIO E FALE COM AVEN", hudFont, goldBrush, 67, 592);
        else if (room >= 4 && room < 6) g.DrawString(ArchiveObjective(), hudFont, goldBrush, 67, 592);
        else if (room is 6 or 7) g.DrawString(RiverObjective(), hudFont, goldBrush, 67, 592);
        else if (room >= 8) g.DrawString(LumeObjective(), hudFont, goldBrush, 67, 592);
        g.DrawString(room is 3 or 18 ? "WASD mover   E falar/investigar   F5 salvar   I inventário" : "WASD mover   ESPAÇO/CLIQUE atacar   SHIFT esquivar   R redemoinho   F estilhaço   V sino   Q poção   E interagir   I inventário" + (ebbSeal ? "   G vazante" : "") + (anchorWard ? "   H lastro" : ""), helpFont, textBrush, 67, 619);
        if (room < MemoryStonePositions.Length && (memoriesFound & (1 << room)) == 0) DrawMemoryStone(g, MemoryStonePositions[room], Distance(player, MemoryStonePositions[room]) < 58);
        if (noticeTime > 0) DrawCentered(g, notice, 170, new Font("Segoe UI", 13, FontStyle.Bold), Color.FromArgb(230, 193, 129));
        if (title || dead || victory) DrawOverlay(g, title ? "SCADRYK" : victory ? "VITÓRIA" : "VOCÊ CAIU", title ? "ENTER PARA COMEÇAR    •    C CARREGAR SAVE" : "ENTER PARA RECOMEÇAR    •    F5 SALVAR");
        if (inventoryOpen) DrawInventory(g);
        if (talentChoice) DrawChoice(g, $"SUBIU DE NÍVEL — ESCOLHA ({pendingTalents} PENDENTES)", "1. VIGOR  +20 VIDA MÁXIMA", "2. DOMÍNIO  técnicas recarregam mais rápido", "3. AGILIDADE  esquiva recarrega mais rápido");
        if (chestChoice) DrawChoice(g, "BAÚ DA CRIPTA — ESCOLHA UMA RECOMPENSA", "1. 75 OURO", "2. DUAS POÇÕES", "3. RECUPERAR TODA A VIDA");
        if (endingChoiceOpen) DrawChoice(g, "O QUE VÉSPERA DEVE LEMBRAR?", "1. DEVOLVER OS NOMES  •  +20 VIDA MÁXIMA", "2. MANTER O LUME  •  +10 DANO", "3. ADIAR A DECISÃO");
        if (forgeMenuOpen) DrawChoice(g, weaponRarities[weapon] == 0 ? $"TÊMPERA DO VEIO • {weaponNames[weapon]} VIRA RARA" : $"TÊMPERA DO VEIO • {weaponNames[weapon]}", $"1. TRÉGUA • CURA AO ABATER   ({gold} OURO / {tideGlass} SAL)", "2. CONTRACANTO • AUMENTA O CRÍTICO", "3. MARÉ • AUMENTA O DANO DE TÉCNICA  •  ESC PARA FECHAR");
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
        13 => townEnding == 1 ? "OS NOMES VOLTARAM • ATRAVESSE O RESERVATÓRIO" : "O LUME PERMANECE • ATRAVESSE O RESERVATÓRIO",
        14 => "OBJETIVO: LEVE O CORO DOS NOMES A SILETH",
        15 => "OBJETIVO: INVESTIGUE A PASSAGEM SOB O CLAUSTRO",
        16 => "OBJETIVO: LEVE A MEMÓRIA DE ILYRA A SILETH",
        17 => "OBJETIVO: PEÇA A EDDA PARA CRUZAR O RIO",
        18 => "OBJETIVO: ATRAVESSE O VAU DOS NOMES ESCOLHIDOS",
        19 => "OBJETIVO: LEVE O MAPA DE NARA A EDDA",
        20 => "OBJETIVO: FALE COM EDDA PARA SEGUIR A OESTE",
        21 => "OBJETIVO: FALE COM AVEN NO REFÚGIO",
        22 => "OBJETIVO: ATRAVESSE A TRILHA E RECUPERE O REGISTRO",
        23 => "OBJETIVO: DEVOLVA O REGISTRO À COMUNIDADE",
        24 => "A PRÓXIMA CURVA SEGUE A OESTE • SEM RASTRO ATUAL",
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
    private string LumeObjective() => room == 8 ? "OBJETIVO: SIGA A ESCADA ATÉ A CÂMARA DO LUME" : room == 9 && questStage >= 10 && !naraTrace ? "OBJETIVO: INVESTIGUE A PASSAGEM SOB O PRISMA" : room == 10 ? "OBJETIVO: ATRAVESSE A MARGEM SEM REFLEXO" : room == 11 && !naraTrace ? "OBJETIVO: ENFRENTE O QUE DEVORA NOMES" : room == 11 && questStage >= 13 ? "OBJETIVO: CRUZE A COSTA ALÉM DO RESERVATÓRIO" : room == 12 ? "OBJETIVO: SIGA O CORO ATÉ O CLAUSTRO" : room == 13 && !chorusMemory ? "OBJETIVO: DERROTE A TECELÃ E RECUPERE AS VOZES" : room == 13 && questStage < 15 ? "OBJETIVO: LEVE O CORO DE VOLTA A SILETH" : room == 13 ? "OBJETIVO: DESÇA PELA BACIA SOB O CORO" : room == 14 ? "OBJETIVO: ATRAVESSE A BACIA DO PRIMEIRO NOME" : room == 15 ? "OBJETIVO: DERROTE A PRIMEIRA VIGÍLIA" : room == 16 ? "OBJETIVO: CRUZE O VAU DOS NOMES ESCOLHIDOS" : room == 17 ? "OBJETIVO: DERROTE A CARTÓGRAFA E RECUPERE O MAPA" : room == 19 ? "OBJETIVO: SIGA AS LANTERNAS ATÉ A CISTERNA" : room == 20 ? "OBJETIVO: DERROTE O ESCRIVÃO E RECOLHA O REGISTRO" : room == 21 ? "OBJETIVO: SIGA A VÁRZEA ATÉ A PONTE" : room == 22 && !upstreamTrace ? "OBJETIVO: DERROTE A TESTEMUNHA DA MARÉ" : room == 22 ? "OBJETIVO: LEVE A LEMBRANÇA DE VOLTA A AVEN" : room == 23 ? "OBJETIVO: CRUZE O COTOVELO DOS JUNCAIS" : room == 24 && !beyondTrace ? "OBJETIVO: DERROTE A GUARDIÃ E RECUPERE O SINAL" : room == 24 ? "OBJETIVO: LEVE O SINAL DE PASSAGEM A AVEN" : room == 25 ? "OBJETIVO: ATRAVESSE O CANAL CONTRA A CORRENTE" : room == 26 && questStage < 32 ? "OBJETIVO: DERROTE O VIGIA DO VEIO PRESO" : room == 26 ? "OBJETIVO: LEVE A MEMÓRIA DA VAZANTE A AVEN" : room >= 10 && naraTrace ? "OBJETIVO: LEVE O RASTRO DE NARA A SILETH" : lumeLens ? "OBJETIVO: RETORNE E MOSTRE O PRISMA A SILETH" : "OBJETIVO: RECUPERE O PRISMA DA PRIMEIRA VIGÍLIA";

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
        if (room == 11 && questStage >= 13) DrawCentered(g, "COSTA DAS VOZES TARDIAS", 382, new Font("Segoe UI", 9, FontStyle.Bold), townEnding == 1 ? Color.LightGreen : Color.PeachPuff);
    }

    private void DrawFarReachEnvironment(Graphics g, Brush ground, Pen tile)
    {
        using var deep = new SolidBrush(room == 12 ? Color.FromArgb(29, 46, 51) : Color.FromArgb(38, 35, 48));
        using var stone = new SolidBrush(room == 12 ? Color.FromArgb(70, 83, 78) : Color.FromArgb(78, 67, 83));
        using var edge = new Pen(room == 12 ? Color.FromArgb(142, 177, 155) : Color.FromArgb(179, 147, 190), 2);
        using var current = new Pen(Color.FromArgb(70, 160, 201, 188), 3);
        g.FillRectangle(deep, 65, 108, 830, 445); g.FillRectangle(ground, 94, 132, 772, 397);
        for (int y = 0; y < 7; y++) for (int x = 0; x < 12; x++) g.DrawRectangle(tile, 70 + x * 68, 112 + y * 62, 67, 61);
        RectangleF[] walls = room == 12 ? FarReachWalls : NameCloisterWalls;
        foreach (RectangleF wall in walls) { g.FillRectangle(stone, wall); g.DrawRectangle(edge, wall.X, wall.Y, wall.Width, wall.Height); }
        for (int i = 0; i < 5; i++)
        {
            float x = 152 + i * 152;
            g.DrawBezier(current, x, 155, x + 58, 210 + i % 2 * 18, x - 52, 268 + i % 2 * 14, x + 12, 320);
            g.DrawBezier(current, x + 12, 320, x + 68, 374, x - 34, 444, x + 18, 520);
        }
        using var pool = new SolidBrush(Color.FromArgb(room == 13 ? 56 : 32, 128, 181, 177));
        g.FillEllipse(pool, room == 13 ? 377 : 327, room == 13 ? 224 : 193, room == 13 ? 206 : 306, room == 13 ? 200 : 268);
        if (room == 13)
        {
            using var knot = new SolidBrush(Color.FromArgb(180, 176, 119, 188));
            using var thread = new Pen(Color.FromArgb(210, 213, 178, 218), 3);
            g.FillEllipse(knot, 451, 292, 58, 58);
            for (int i = 0; i < 8; i++) { double a = i * Math.PI / 4; g.DrawLine(thread, 480, 321, 480 + (float)Math.Cos(a) * 82, 321 + (float)Math.Sin(a) * 82); }
        }
        using var gate = new Pen(enemies.Count == 0 && drops.Count == 0 ? Color.LightGreen : Color.FromArgb(76, 78, 85), 7);
        g.DrawLine(gate, 885, 290, 885, 370); g.DrawLine(gate, 75, 290, 75, 370);
        if (room == 12 && Distance(player, new PointF(842, 330)) < 80) DrawCentered(g, "E  ENTRAR NO CLAUSTRO", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
        if (room == 13 && chorusMemory && Distance(player, new PointF(842, 330)) < 80) DrawCentered(g, "E  VOLTAR A VÉSPERA", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
        if (room == 13 && questStage >= 15) { using var stair = new Pen(Color.FromArgb(220, 208, 178, 123), 5); g.DrawArc(stair, 447, 459, 66, 66, 15, 150); if (Distance(player, new PointF(480, 500)) < 78) DrawCentered(g, "E  DESCER À BACIA", 535, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 480); }
    }

    private void DrawFirstVigilEnvironment(Graphics g, Brush ground, Pen tile)
    {
        using var deep = new SolidBrush(Color.FromArgb(24, 30, 39)); using var stone = new SolidBrush(Color.FromArgb(72, 70, 64));
        using var edge = new Pen(room == 14 ? Color.FromArgb(133, 172, 166) : Color.FromArgb(196, 159, 106), 2);
        using var echo = new Pen(Color.FromArgb(88, 191, 178, 133), 3); using var basin = new SolidBrush(Color.FromArgb(42, 115, 155, 160));
        g.FillRectangle(deep, 65, 108, 830, 445); g.FillRectangle(ground, 94, 132, 772, 397);
        for (int y = 0; y < 7; y++) for (int x = 0; x < 12; x++) g.DrawRectangle(tile, 70 + x * 68, 112 + y * 62, 67, 61);
        RectangleF[] walls = room == 14 ? FirstBasinWalls : FirstVigilWalls;
        foreach (RectangleF wall in walls) { g.FillRectangle(stone, wall); g.DrawRectangle(edge, wall.X, wall.Y, wall.Width, wall.Height); }
        for (int i = 0; i < 4; i++) { int inset = i * 19; g.DrawEllipse(echo, 383 + inset, 232 + inset, 194 - inset * 2, 194 - inset * 2); }
        if (room == 14) { g.FillEllipse(basin, 415, 278, 130, 104); DrawCentered(g, "ILYRA", 319, new Font("Georgia", 10, FontStyle.Bold), Color.FromArgb(202, 187, 148)); }
        else { using var firstLight = new SolidBrush(Color.FromArgb(120, 208, 174, 111)); g.FillEllipse(firstLight, 454, 292, 52, 52); }
        using var gate = new Pen(enemies.Count == 0 && drops.Count == 0 ? Color.LightGreen : Color.FromArgb(76, 78, 85), 7);
        g.DrawLine(gate, 885, 290, 885, 370); g.DrawLine(gate, 75, 290, 75, 370);
        if (room == 14 && Distance(player, new PointF(842, 330)) < 80) DrawCentered(g, "E  IR À PRIMEIRA VIGÍLIA", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
        if (room == 15 && firstMemory && Distance(player, new PointF(842, 330)) < 80) DrawCentered(g, "E  VOLTAR A VÉSPERA", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
    }

    private void DrawWestWitnessEnvironment(Graphics g, Brush ground, Pen tile)
    {
        using var deep = new SolidBrush(Color.FromArgb(22, 34, 39)); using var wood = new SolidBrush(Color.FromArgb(91, 77, 57));
        using var edge = new Pen(room == 16 ? Color.FromArgb(119, 160, 153) : Color.FromArgb(193, 172, 127), 2);
        using var water = new SolidBrush(Color.FromArgb(62, 70, 135, 139)); using var chalk = new Pen(Color.FromArgb(132, 218, 201, 160), 3);
        g.FillRectangle(deep, 65, 108, 830, 445); g.FillRectangle(ground, 94, 132, 772, 397);
        for (int y = 0; y < 7; y++) for (int x = 0; x < 12; x++) g.DrawRectangle(tile, 70 + x * 68, 112 + y * 62, 67, 61);
        RectangleF[] walls = room == 16 ? WestFordWalls : WitnessYardWalls;
        foreach (RectangleF wall in walls) { g.FillRectangle(wood, wall); g.DrawRectangle(edge, wall.X, wall.Y, wall.Width, wall.Height); }
        if (room == 16)
        {
            g.FillRectangle(water, 328, 125, 80, 424); g.FillRectangle(water, 558, 125, 78, 424);
            for (int y = 148; y < 520; y += 52) { g.DrawLine(edge, 408, y, 558, y + 12); g.DrawLine(edge, 408, y + 14, 558, y + 26); }
            for (int i = 0; i < 6; i++) g.DrawLine(chalk, 138 + i * 126, 145, 138 + i * 126, 191);
        }
        else
        {
            using var table = new SolidBrush(Color.FromArgb(110, 125, 112, 83)); g.FillEllipse(table, 401, 262, 158, 114);
            for (int i = 0; i < 5; i++) { float x = 435 + i * 22; g.DrawLine(chalk, x, 286, 466 + (i % 2) * 19, 350); }
        }
        using var gate = new Pen(enemies.Count == 0 && drops.Count == 0 ? Color.LightGreen : Color.FromArgb(76, 78, 85), 7);
        g.DrawLine(gate, 885, 290, 885, 370); g.DrawLine(gate, 75, 290, 75, 370);
        if (room == 16 && Distance(player, new PointF(118, 330)) < 78) DrawCentered(g, "E  VOLTAR A VÉSPERA", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 146);
        if (room == 16 && Distance(player, new PointF(842, 330)) < 80) DrawCentered(g, "E  IR AO PÁTIO", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
        if (room == 17 && westTrace && Distance(player, new PointF(842, 330)) < 80) DrawCentered(g, "E  VOLTAR A VÉSPERA", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
    }

    private void DrawRefugeEnvironment(Graphics g, Brush ground, Pen tile)
    {
        using var deep = new SolidBrush(Color.FromArgb(24, 37, 39)); using var wood = new SolidBrush(Color.FromArgb(93, 83, 61));
        using var edge = new Pen(Color.FromArgb(165, 153, 116), 2); using var path = new Pen(Color.FromArgb(121, 135, 103), 8);
        g.FillRectangle(deep, 65, 108, 830, 445); g.FillRectangle(ground, 94, 132, 772, 397);
        for (int y = 0; y < 7; y++) for (int x = 0; x < 12; x++) g.DrawRectangle(tile, 70 + x * 68, 112 + y * 62, 67, 61);
        foreach (RectangleF wall in RefugeWalls) { g.FillRectangle(wood, wall); g.DrawRectangle(edge, wall.X, wall.Y, wall.Width, wall.Height); }
        g.DrawLine(path, 302, 330, 412, 330); g.DrawLine(path, 550, 330, 662, 330);
        using var table = new SolidBrush(Color.FromArgb(119, 99, 73)); using var ledger = new SolidBrush(Color.FromArgb(47, 62, 58));
        g.FillEllipse(table, 427, 292, 106, 75); g.FillRectangle(ledger, 458, 309, 44, 27); g.DrawEllipse(edge, 427, 292, 106, 75);
        DrawTownPerson(g, AvenPosition, "AVEN", Color.FromArgb(88, 112, 102), Distance(player, AvenPosition) < 72);
        DrawTownPerson(g, SeraPosition, "SERA", Color.FromArgb(144, 103, 78), Distance(player, SeraPosition) < 72);
        DrawTownPerson(g, NoaPosition, "NOA", Color.FromArgb(102, 111, 145), Distance(player, NoaPosition) < 72);
        using var gate = new Pen(Color.FromArgb(111, 191, 163), 5); g.DrawLine(gate, 75, 296, 75, 364);
        using var trail = new Pen(questStage >= 22 ? Color.FromArgb(179, 208, 151) : Color.FromArgb(70, 83, 77), 5); g.DrawLine(trail, 885, 296, 885, 364);
        if (questStage >= 27 && Distance(player, AvenPosition) < 72) DrawCentered(g, "E  TÊMPERA • 75 OURO + 1 SAL VÍTREO", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.LightCyan, 490);
        if (player.X < 150) DrawCentered(g, "E  VOLTAR A VÉSPERA", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 146);
        if (questStage >= 22 && player.X > 790) DrawCentered(g, questStage >= 30 ? "E  SEGUIR AS ÁGUAS" : "E  SEGUIR AS LANTERNAS", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
    }

    private void DrawLedgerTrailEnvironment(Graphics g, Brush ground, Pen tile)
    {
        using var deep = new SolidBrush(Color.FromArgb(21, 32, 37)); using var stone = new SolidBrush(Color.FromArgb(78, 79, 70));
        using var edge = new Pen(room == 19 ? Color.FromArgb(172, 179, 133) : Color.FromArgb(183, 145, 119), 2);
        using var water = new SolidBrush(Color.FromArgb(54, 56, 113, 118));
        g.FillRectangle(deep, 65, 108, 830, 445); g.FillRectangle(ground, 94, 132, 772, 397);
        for (int y = 0; y < 7; y++) for (int x = 0; x < 12; x++) g.DrawRectangle(tile, 70 + x * 68, 112 + y * 62, 67, 61);
        RectangleF[] walls = room == 19 ? RidgeWalls : LedgerWalls;
        foreach (RectangleF wall in walls) { g.FillRectangle(stone, wall); g.DrawRectangle(edge, wall.X, wall.Y, wall.Width, wall.Height); }
        if (room == 19) { g.FillRectangle(water, 330, 128, 74, 420); g.FillRectangle(water, 558, 128, 74, 420); for (int i = 0; i < 8; i++) { float x = 150 + i * 94; g.DrawLine(edge, x, 152, x, 187); g.FillEllipse(Brushes.PaleGoldenrod, x - 4, 148, 8, 8); } }
        else { using var basin = new SolidBrush(Color.FromArgb(60, 104, 131, 125)); using var ink = new Pen(Color.FromArgb(205, 181, 128, 109), 3); g.FillEllipse(basin, 350, 206, 260, 260); g.DrawEllipse(edge, 350, 206, 260, 260); for (int i = 0; i < 7; i++) g.DrawLine(ink, 382 + i * 27, 245, 410 + i * 20, 425); }
        using var gate = new Pen(enemies.Count == 0 && drops.Count == 0 ? Color.LightGreen : Color.FromArgb(76, 78, 85), 7); g.DrawLine(gate, 885, 290, 885, 370); g.DrawLine(gate, 75, 290, 75, 370);
        if (player.X < 150) DrawCentered(g, "E  VOLTAR", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 146);
        if (room == 19 && player.X > 790 && enemies.Count == 0 && drops.Count == 0) DrawCentered(g, "E  ENTRAR NA CISTERNA", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
        if (room == 20 && player.X > 790 && questStage >= 23 && enemies.Count == 0 && drops.Count == 0) DrawCentered(g, "E  VOLTAR AO REFÚGIO", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
    }

    private void DrawUpstreamEnvironment(Graphics g, Brush ground, Pen tile)
    {
        using var deep = new SolidBrush(Color.FromArgb(22, 35, 42)); using var water = new SolidBrush(Color.FromArgb(74, 58, 118, 127));
        using var stone = new SolidBrush(Color.FromArgb(78, 88, 82)); using var edge = new Pen(Color.FromArgb(170, 186, 166, 134), 2);
        g.FillRectangle(deep, 65, 108, 830, 445); g.FillRectangle(ground, 94, 132, 772, 397);
        for (int y = 0; y < 7; y++) for (int x = 0; x < 12; x++) g.DrawRectangle(tile, 70 + x * 68, 112 + y * 62, 67, 61);
        RectangleF[] walls = room == 21 ? SaltMarshWalls : TideBridgeWalls;
        foreach (RectangleF wall in walls) { g.FillRectangle(stone, wall); g.DrawRectangle(edge, wall.X, wall.Y, wall.Width, wall.Height); }
        if (room == 21)
        {
            g.FillPolygon(water, new[] { new PointF(92, 284), new PointF(336, 308), new PointF(570, 278), new PointF(868, 313), new PointF(868, 371), new PointF(572, 346), new PointF(332, 376), new PointF(92, 350) });
            using var current = new Pen(Color.FromArgb(150, 173, 208, 198), 2);
            for (int i = 0; i < 8; i++) { float y = 300 + i * 7; g.DrawLine(current, 112 + i * 23, y, 208 + i * 23, y + 7); }
            using var reed = new Pen(Color.FromArgb(172, 155, 173, 142), 2);
            for (int i = 0; i < 12; i++) { float x = 122 + i * 62; g.DrawLine(reed, x, 212 + i % 3 * 11, x + (i % 2 == 0 ? 5 : -4), 180 + i % 3 * 11); }
        }
        else
        {
            g.FillRectangle(water, 94, 132, 772, 137); g.FillRectangle(water, 94, 411, 772, 118);
            using var current = new Pen(Color.FromArgb(150, 173, 208, 198), 2);
            for (int i = 0; i < 9; i++) { float x = 108 + i * 88; g.DrawArc(current, x, 155 + i % 2 * 35, 60, 22, 0, 160); g.DrawArc(current, x + 28, 438 + i % 2 * 28, 60, 22, 180, 160); }
            using var deck = new SolidBrush(Color.FromArgb(113, 93, 66)); using var plank = new Pen(Color.FromArgb(192, 158, 111), 2);
            g.FillRectangle(deck, 94, 270, 772, 143); g.DrawRectangle(edge, 94, 270, 772, 143);
            for (int y = 285; y < 410; y += 20) g.DrawLine(plank, 98, y, 862, y);
            g.DrawEllipse(edge, 397, 292, 166, 100); g.DrawEllipse(plank, 418, 309, 124, 66);
        }
        using var gate = new Pen(enemies.Count == 0 && drops.Count == 0 ? Color.LightGreen : Color.FromArgb(73, 82, 79), 7);
        g.DrawLine(gate, 885, 290, 885, 370); g.DrawLine(gate, 75, 290, 75, 370);
        if (player.X < 150) DrawCentered(g, room == 21 ? "E  VOLTAR AO REFÚGIO" : "E  VOLTAR À VÁRZEA", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 146);
        if (room == 21 && player.X > 790 && enemies.Count == 0 && drops.Count == 0) DrawCentered(g, "E  CRUZAR A PONTE", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
        if (room == 22 && player.X > 790 && questStage >= 27 && enemies.Count == 0 && drops.Count == 0) DrawCentered(g, "E  CRUZAR O COTOVELO", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
        else if (room == 22 && player.X > 790 && questStage >= 26 && enemies.Count == 0 && drops.Count == 0) DrawCentered(g, "E  VOLTAR À MESA", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
    }

    private void DrawReedBankEnvironment(Graphics g, Brush ground, Pen tile)
    {
        using var deep = new SolidBrush(Color.FromArgb(21, 34, 35)); using var mud = new SolidBrush(Color.FromArgb(78, 83, 67));
        using var water = new SolidBrush(Color.FromArgb(68, 59, 114, 116)); using var reed = new Pen(Color.FromArgb(153, 166, 132), 3);
        using var edge = new Pen(Color.FromArgb(169, 158, 117), 2); using var trail = new Pen(Color.FromArgb(194, 165, 124, 91), 6);
        g.FillRectangle(deep, 65, 108, 830, 445); g.FillRectangle(ground, 94, 132, 772, 397);
        for (int y = 0; y < 7; y++) for (int x = 0; x < 12; x++) g.DrawRectangle(tile, 70 + x * 68, 112 + y * 62, 67, 61);
        if (room == 23)
        {
            g.FillPolygon(water, new[] { new PointF(90, 288), new PointF(315, 310), new PointF(525, 280), new PointF(870, 317), new PointF(870, 375), new PointF(525, 347), new PointF(315, 384), new PointF(90, 350) });
            g.DrawLine(trail, 95, 274, 330, 298); g.DrawLine(trail, 330, 298, 544, 268); g.DrawLine(trail, 545, 390, 332, 410); g.DrawLine(trail, 332, 410, 95, 383);
            for (int i = 0; i < 13; i++) { float x = 116 + i * 57; g.DrawLine(reed, x, 250 + i % 3 * 9, x + (i % 2 == 0 ? 5 : -4), 211 + i % 3 * 9); g.DrawLine(reed, x + 23, 420 - i % 3 * 8, x + 19, 455 - i % 3 * 8); }
        }
        else
        {
            g.FillRectangle(water, 94, 132, 772, 118); g.FillRectangle(water, 94, 411, 772, 118); g.FillRectangle(mud, 94, 251, 772, 160);
            g.DrawLine(trail, 98, 258, 862, 258); g.DrawLine(trail, 98, 404, 862, 404);
            for (int i = 0; i < 11; i++) { float x = 115 + i * 70; g.DrawLine(reed, x, 242, x + 3, 211 - i % 3 * 7); g.DrawLine(reed, x + 25, 422, x + 20, 455 + i % 3 * 7); }
            using var knot = new SolidBrush(Color.FromArgb(210, 195, 169, 122));
            for (int i = 0; i < 7; i++) { float x = 160 + i * 104; g.DrawLine(edge, x, 285, x + 15, 314); g.DrawLine(edge, x + 15, 314, x, 343); g.FillEllipse(knot, x + 8, 308, 12, 12); }
        }
        RectangleF[] walls = room == 23 ? ReedRunWalls : ReedYardWalls;
        foreach (RectangleF wall in walls) { g.FillEllipse(mud, wall); g.DrawEllipse(edge, wall); }
        using var gate = new Pen(enemies.Count == 0 && drops.Count == 0 ? Color.LightGreen : Color.FromArgb(73, 82, 79), 7);
        g.DrawLine(gate, 885, 290, 885, 370); g.DrawLine(gate, 75, 290, 75, 370);
        if (player.X < 150) DrawCentered(g, room == 23 ? "E  VOLTAR À PONTE" : "E  VOLTAR AO COTOVELO", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 146);
        if (room == 23 && player.X > 790 && enemies.Count == 0 && drops.Count == 0) DrawCentered(g, "E  ENTRAR NO PÁTIO", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
        if (room == 24 && player.X > 790 && questStage >= 29 && enemies.Count == 0 && drops.Count == 0) DrawCentered(g, "E  VOLTAR AO REFÚGIO", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
    }

    private void DrawCounterflowEnvironment(Graphics g, Brush ground, Pen tile)
    {
        using var deep = new SolidBrush(Color.FromArgb(16, 25, 31)); using var stone = new SolidBrush(Color.FromArgb(79, 77, 71));
        using var water = new SolidBrush(Color.FromArgb(94, 43, 108, 121)); using var edge = new Pen(Color.FromArgb(174, 166, 136), 2);
        using var current = new Pen(Color.FromArgb(205, 161, 211, 207), 3); using var path = new Pen(Color.FromArgb(171, 157, 122), 5);
        g.FillRectangle(deep, 65, 108, 830, 445); g.FillRectangle(ground, 94, 132, 772, 397);
        for (int y = 0; y < 7; y++) for (int x = 0; x < 12; x++) g.DrawRectangle(tile, 70 + x * 68, 112 + y * 62, 67, 61);
        if (room == 27)
        {
            g.FillPolygon(water, new[] { new PointF(94, 365), new PointF(310, 308), new PointF(528, 350), new PointF(868, 266), new PointF(868, 319), new PointF(528, 405), new PointF(310, 359), new PointF(94, 416) });
            g.DrawLine(path, 95, 245, 352, 309); g.DrawLine(path, 605, 366, 865, 425);
            for (int i = 0; i < 7; i++) { float x = 152 + i * 94, y = 350 - i * 12; g.DrawLine(current, x, y, x + 28, y - 8); g.DrawLine(current, x + 28, y - 8, x + 15, y - 18); g.DrawLine(current, x + 28, y - 8, x + 12, y); }
        }
        else
        {
            g.FillEllipse(water, 304, 154, 352, 352); g.FillEllipse(stone, 402, 252, 156, 156); g.DrawEllipse(edge, 304, 154, 352, 352);
            float direction = MathF.Sin(windupPulse * 1.35f) >= 0 ? -1 : 1;
            for (int i = 0; i < 12; i++) { float angle = i * MathF.PI / 6 + windupPulse * 0.14f, dx = MathF.Cos(angle) * direction, dy = MathF.Sin(angle) * direction; float x = 480 + MathF.Cos(angle) * 188, y = 330 + MathF.Sin(angle) * 188; g.DrawLine(current, x, y, x + dx * 31, y + dy * 31); g.DrawLine(current, x + dx * 31, y + dy * 31, x + dx * 17 - dy * 8, y + dy * 17 + dx * 8); g.DrawLine(current, x + dx * 31, y + dy * 31, x + dx * 17 + dy * 8, y + dy * 17 - dx * 8); }
            DrawCentered(g, direction < 0 ? "PUXO" : "REPULSO", 495, new Font("Segoe UI", 9, FontStyle.Bold), Color.FromArgb(204, 205, 188));
        }
        RectangleF[] walls = room == 27 ? CounterSlopeWalls : CounterHeartWalls;
        foreach (RectangleF wall in walls) { g.FillRectangle(stone, wall); g.DrawRectangle(edge, wall.X, wall.Y, wall.Width, wall.Height); }
        using var gate = new Pen(enemies.Count == 0 && drops.Count == 0 ? Color.LightGreen : Color.FromArgb(73, 82, 79), 7);
        g.DrawLine(gate, 885, 290, 885, 370); g.DrawLine(gate, 75, 290, 75, 370);
        if (player.X < 150) DrawCentered(g, room == 27 ? "E  VOLTAR AO REFÚGIO" : "E  VOLTAR À ESCARPA", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 146);
        if (room == 27 && player.X > 790 && enemies.Count == 0 && drops.Count == 0) DrawCentered(g, "E  DESCER AO CORAÇÃO", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
        if (room == 28 && player.X > 790 && questStage >= 35 && enemies.Count == 0 && drops.Count == 0) DrawCentered(g, "E  VOLTAR A AVEN", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
    }

    private void DrawFloodMillEnvironment(Graphics g, Brush ground, Pen tile)
    {
        using var deep = new SolidBrush(Color.FromArgb(18, 30, 34)); using var mud = new SolidBrush(Color.FromArgb(76, 79, 68));
        using var water = new SolidBrush(Color.FromArgb(88, 43, 102, 111)); using var edge = new Pen(Color.FromArgb(163, 167, 137), 2);
        using var current = new Pen(Color.FromArgb(180, 154, 218, 208), 3); using var path = new Pen(Color.FromArgb(177, 156, 119, 88), 6);
        g.FillRectangle(deep, 65, 108, 830, 445); g.FillRectangle(ground, 94, 132, 772, 397);
        for (int y = 0; y < 7; y++) for (int x = 0; x < 12; x++) g.DrawRectangle(tile, 70 + x * 68, 112 + y * 62, 67, 61);
        if (room == 25)
        {
            g.FillRectangle(water, 300, 286, 372, 98); g.DrawLine(path, 95, 263, 285, 263); g.DrawLine(path, 685, 405, 865, 405);
            for (int x = 344; x <= 604; x += 86) { g.DrawLine(current, x, 322, x + 36, 322); g.DrawLine(current, x + 36, 322, x + 23, 311); g.DrawLine(current, x + 36, 322, x + 23, 333); }
            using var wheel = new Pen(Color.FromArgb(185, 147, 123, 81), 5); g.DrawEllipse(wheel, 112, 163, 108, 108); g.DrawEllipse(wheel, 740, 392, 108, 108);
            g.DrawRectangle(edge, 111, 162, 110, 110); g.DrawRectangle(edge, 739, 391, 110, 110);
        }
        else
        {
            g.FillEllipse(water, 316, 166, 328, 328); g.FillEllipse(mud, 393, 243, 174, 174); g.DrawEllipse(edge, 316, 166, 328, 328);
            for (int i = 0; i < 8; i++) { float angle = i * MathF.PI / 4; float dx = MathF.Cos(angle), dy = MathF.Sin(angle), x = 480 + dx * 194, y = 330 + dy * 194; g.DrawLine(current, x, y, x - dx * 27, y - dy * 27); }
            using var ring = new Pen(Color.FromArgb(170, 167, 145, 111), 4); g.DrawEllipse(ring, 408, 258, 144, 144);
        }
        RectangleF[] walls = room == 25 ? FloodMillWalls : SiltChamberWalls;
        foreach (RectangleF wall in walls) { g.FillRectangle(mud, wall); g.DrawRectangle(edge, wall.X, wall.Y, wall.Width, wall.Height); }
        using var gate = new Pen(enemies.Count == 0 && drops.Count == 0 ? Color.LightGreen : Color.FromArgb(73, 82, 79), 7);
        g.DrawLine(gate, 885, 290, 885, 370); g.DrawLine(gate, 75, 290, 75, 370);
        if (player.X < 150) DrawCentered(g, room == 25 ? "E  VOLTAR AO REFÚGIO" : "E  VOLTAR AOS MOINHOS", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 146);
        if (room == 25 && player.X > 790 && enemies.Count == 0 && drops.Count == 0) DrawCentered(g, "E  DESCER À CÂMARA", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
        if (room == 26 && player.X > 790 && questStage >= 32 && enemies.Count == 0 && drops.Count == 0) DrawCentered(g, "E  VOLTAR A AVEN", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
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
        float r = enemy.Radius; Color color = enemy.Kind == EnemyKind.BlindScribe ? Color.FromArgb(89, 128, 135) : enemy.Kind == EnemyKind.RiverFerryman ? Color.FromArgb(69, 120, 124) : enemy.Kind == EnemyKind.AshWarden ? Color.FromArgb(169, 91, 53) : enemy.Kind == EnemyKind.HollowName ? Color.FromArgb(120, 165, 155) : enemy.Kind == EnemyKind.NameWeaver ? Color.FromArgb(139, 108, 162) : enemy.Kind == EnemyKind.FirstKeeper ? Color.FromArgb(202, 173, 112) : enemy.Kind == EnemyKind.OathCartographer ? Color.FromArgb(96, 148, 144) : enemy.Kind == EnemyKind.NameBroker ? Color.FromArgb(126, 82, 132) : enemy.Kind == EnemyKind.ReedKeeper ? Color.FromArgb(158, 149, 98) : enemy.Kind == EnemyKind.FlowKeeper ? Color.FromArgb(71, 148, 164) : enemy.Kind == EnemyKind.TideWitness ? Color.FromArgb(63, 157, 168) : enemy.Kind == EnemyKind.SaltEcho ? Color.LightSteelBlue : enemy.Kind == EnemyKind.PaperWraith ? Color.FromArgb(201, 190, 161) : enemy.Kind == EnemyKind.MudCantor ? Color.FromArgb(165, 135, 104) : enemy.Boss ? Color.FromArgb(145, 87, 77) : enemy.Kind switch { EnemyKind.Elite => enemy.Armored ? Color.SlateGray : Color.IndianRed, EnemyKind.Archer => Color.FromArgb(128, 151, 106), EnemyKind.Mage => Color.MediumPurple, EnemyKind.InkEcho => Color.FromArgb(113, 98, 143), EnemyKind.DrownedMemory => Color.FromArgb(95, 145, 142), EnemyKind.EmberWisp => Color.FromArgb(212, 126, 61), EnemyKind.NameMoth => Color.FromArgb(126, 170, 156), _ => Color.FromArgb(183, 181, 166) };
        if (enemy.Stagger > 0) { using var staggerPen = new Pen(Color.FromArgb(200, 125, 211, 203), 2); float pulse = 1 + (0.95f - enemy.Stagger) * 7; g.DrawEllipse(staggerPen, enemy.Pos.X - r - pulse, enemy.Pos.Y - r - pulse, (r + pulse) * 2, (r + pulse) * 2); }
        if (enemy.Windup > 0) { float baseWarn = enemy.Kind == EnemyKind.Elite ? 62 : enemy.Kind == EnemyKind.Mage ? 52 : enemy.Boss && enemy.BossPattern == 0 ? 90 : enemy.Kind == EnemyKind.Archer ? 26 : enemy.Kind == EnemyKind.InkEcho ? 30 : enemy.Kind == EnemyKind.DrownedMemory ? 28 : (enemy.Kind is EnemyKind.RiverFerryman or EnemyKind.AshWarden or EnemyKind.HollowName or EnemyKind.OathCartographer or EnemyKind.NameBroker or EnemyKind.TideWitness or EnemyKind.ReedKeeper or EnemyKind.FlowKeeper or EnemyKind.CounterCurrent) && enemy.BossPattern == 2 ? 85 : enemy.Kind == EnemyKind.NameWeaver && enemy.BossPattern == 2 ? 38 : enemy.Kind is EnemyKind.EmberWisp or EnemyKind.NameMoth ? 28 : 36; float progress = Math.Clamp(enemy.Windup / Math.Max(0.01f, enemy.WindupMax), 0, 1); float warn = baseWarn * (0.4f + progress * 0.6f); using var p = new Pen(Color.FromArgb(235, 115, 75), 3); using var b = new SolidBrush(Color.FromArgb(35, 235, 115, 75)); if ((enemy.Kind is EnemyKind.Archer or EnemyKind.InkEcho or EnemyKind.DrownedMemory or EnemyKind.EmberWisp or EnemyKind.NameMoth or EnemyKind.PaperWraith or EnemyKind.SaltEcho) || ((enemy.Kind is EnemyKind.RiverFerryman or EnemyKind.AshWarden or EnemyKind.HollowName or EnemyKind.NameWeaver or EnemyKind.OathCartographer or EnemyKind.NameBroker or EnemyKind.TideWitness or EnemyKind.ReedKeeper or EnemyKind.FlowKeeper or EnemyKind.CounterCurrent) && enemy.BossPattern == 2)) g.DrawLine(p, enemy.Pos, enemy.Target); g.FillEllipse(b, enemy.Target.X - warn, enemy.Target.Y - warn, warn * 2, warn * 2); g.DrawEllipse(p, enemy.Target.X - warn, enemy.Target.Y - warn, warn * 2, warn * 2); }
        if (enemy.Kind == EnemyKind.FirstKeeper && enemy.Windup > 0 && enemy.BossPattern == 2) { using var fork = new Pen(Color.FromArgb(235, 214, 184, 124), 4); g.DrawLine(fork, enemy.Pos, enemy.Target); g.DrawLine(fork, enemy.Target2Start, enemy.Target2); }
        using var body = new SolidBrush(enemy.Flash > 0 ? Color.White : color); using var edge = new Pen(enemy == lockedTarget ? Color.Gold : enemy.Armored ? Color.LightSteelBlue : Color.FromArgb(35, 25, 22), enemy == lockedTarget ? 4 : 2);
        g.FillEllipse(body, enemy.Pos.X - r, enemy.Pos.Y - r, r * 2, r * 2); g.DrawEllipse(edge, enemy.Pos.X - r, enemy.Pos.Y - r, r * 2, r * 2);
        if (enemy.Kind == EnemyKind.ReedKeeper) { using var stalk = new Pen(Color.FromArgb(220, 205, 193, 133), 3); for (int i = -1; i <= 1; i++) g.DrawLine(stalk, enemy.Pos.X + i * 9, enemy.Pos.Y + 13, enemy.Pos.X + i * 5, enemy.Pos.Y - 23); }
        if (enemy.Kind == EnemyKind.FlowKeeper) { using var ring = new Pen(Color.FromArgb(210, 157, 215, 216), 2); float spin = windupPulse * 40; g.DrawArc(ring, enemy.Pos.X - 18, enemy.Pos.Y - 18, 36, 36, spin, 235); g.DrawArc(ring, enemy.Pos.X - 13, enemy.Pos.Y - 13, 26, 26, spin + 170, 115); }
        using var hpBack = new SolidBrush(Color.FromArgb(40, 20, 20)); using var hpBar = new SolidBrush(Color.FromArgb(188, 76, 70)); g.FillRectangle(hpBack, enemy.Pos.X - 22, enemy.Pos.Y - r - 10, 44, 5); g.FillRectangle(hpBar, enemy.Pos.X - 22, enemy.Pos.Y - r - 10, 44 * Math.Max(0, enemy.Hp / enemy.MaxHp), 5);
        string name = EnemyName(enemy); if (name.Length > 0) DrawCentered(g, name, enemy.Pos.Y - r - 28, new Font("Segoe UI", 8, FontStyle.Bold), Color.FromArgb(233, 198, 151), enemy.Pos.X);
        if (enemy.Boss) { using var bossBack = new SolidBrush(Color.FromArgb(40, 20, 20)); using var bossHp = new SolidBrush(enemy.Hp <= enemy.MaxHp * 0.5f ? Color.OrangeRed : enemy.Kind == EnemyKind.BlindScribe ? Color.Teal : enemy.Kind == EnemyKind.RiverFerryman ? Color.SteelBlue : enemy.Kind == EnemyKind.AshWarden ? Color.DarkOrange : enemy.Kind == EnemyKind.HollowName ? Color.LightSeaGreen : enemy.Kind == EnemyKind.NameWeaver ? Color.MediumPurple : enemy.Kind == EnemyKind.FirstKeeper ? Color.Goldenrod : enemy.Kind == EnemyKind.OathCartographer ? Color.DarkCyan : enemy.Kind == EnemyKind.ReedKeeper ? Color.DarkOliveGreen : enemy.Kind == EnemyKind.CounterCurrent ? Color.CadetBlue : enemy.Kind == EnemyKind.FlowKeeper ? Color.DeepSkyBlue : enemy.Kind is EnemyKind.NameBroker or EnemyKind.TideWitness ? Color.MediumPurple : Color.Firebrick); g.FillRectangle(bossBack, 360, 112, 240, 8); g.FillRectangle(bossHp, 360, 112, 240 * Math.Clamp(enemy.Hp / enemy.MaxHp, 0, 1), 8); string bossName = enemy.Kind == EnemyKind.BlindScribe ? "ESCRIBA CEGO" : enemy.Kind == EnemyKind.RiverFerryman ? "BARQUEIRO OCO" : enemy.Kind == EnemyKind.AshWarden ? "GUARDIÃO DAS CINZAS" : enemy.Kind == EnemyKind.HollowName ? "O NOME VAZIO" : enemy.Kind == EnemyKind.NameWeaver ? "A TECELÃ DE NOMES" : enemy.Kind == EnemyKind.FirstKeeper ? "A PRIMEIRA VIGÍLIA" : enemy.Kind == EnemyKind.OathCartographer ? "CARTÓGRAFA DO VÃO" : enemy.Kind == EnemyKind.NameBroker ? "ESCRIVÃO DE CONTRATO" : enemy.Kind == EnemyKind.TideWitness ? "TESTEMUNHA DA MARÉ" : enemy.Kind == EnemyKind.ReedKeeper ? "GUARDIÃ DO LEITO" : enemy.Kind == EnemyKind.CounterCurrent ? "AFINADOR DAS MARGENS" : enemy.Kind == EnemyKind.FlowKeeper ? "VIGIA DO VEIO PRESO" : "GUARDIÃO DA CRIPTA"; DrawCentered(g, enemy.Hp <= enemy.MaxHp * 0.5f ? $"{bossName} • SEGUNDA FASE" : bossName, 91, new Font("Georgia", 11, FontStyle.Bold), enemy.Hp <= enemy.MaxHp * 0.5f ? Color.OrangeRed : Color.Goldenrod); }
    }
    private void DrawDrop(Graphics g, Drop item) { Color c = item.Kind switch { "relic" => Color.Violet, "seal" => Color.LightSkyBlue, "bell" => Color.FromArgb(162, 212, 192), "lens" => Color.FromArgb(248, 185, 95), "trace" => Color.FromArgb(185, 231, 215), "chorus" => Color.Khaki, "first" => Color.Goldenrod, "west" => Color.LightSkyBlue, "register" => Color.LightSteelBlue, "glass" => Color.LightCyan, "beyond" => Color.PaleGreen, "ebb" => Color.DodgerBlue, "counter" => Color.LightGoldenrodYellow, "charm" => item.Amount == 4 ? Color.Goldenrod : item.Amount == 5 ? Color.LightSeaGreen : Color.Plum, "armor" => Color.MediumAquamarine, "weapon" => Color.CornflowerBlue, "potion" => Color.IndianRed, _ => Color.Goldenrod }; using var b = new SolidBrush(c); using var p = new Pen(Color.FromArgb(230, 220, 190), 1); g.FillEllipse(b, item.Pos.X - 8, item.Pos.Y - 8, 16, 16); g.DrawEllipse(p, item.Pos.X - 8, item.Pos.Y - 8, 16, 16); }
    private void DrawPlayer(Graphics g)
    {
        using var body = new SolidBrush(invulnerable > 0 ? Color.Salmon : Color.FromArgb(90, 143, 138)); using var face = new SolidBrush(Color.Bisque);
        g.FillEllipse(body, player.X - 16, player.Y - 16, 32, 32); g.FillEllipse(face, player.X - 8, player.Y - 11, 16, 16);
        using var facingLine = new Pen(Color.AntiqueWhite, 4); g.DrawLine(facingLine, player, Add(player, Scale(facing, 24)));
        if (wardTime > 0)
        {
            int alpha = Math.Clamp((int)(112 + 70 * MathF.Sin(windupPulse * 8)), 35, 190);
            using var wardGlow = new Pen(Color.FromArgb(alpha, 179, 215, 215), 4); using var wardCore = new Pen(Color.FromArgb(Math.Min(235, alpha + 45), 215, 231, 213), 1);
            g.DrawEllipse(wardGlow, player.X - 25, player.Y - 25, 50, 50); g.DrawEllipse(wardCore, player.X - 20, player.Y - 20, 40, 40);
        }
        if (skillCd > 0) { using var cooldownPen = new Pen(Color.FromArgb(190, 230, 193, 129), 3); g.DrawArc(cooldownPen, player.X - 25, player.Y - 25, 50, 50, -90, 360 * skillCd / Math.Max(1.5f, 6f - skillTalent * .5f)); }
        if (shardCd > 0) { using var shardCooldownPen = new Pen(Color.FromArgb(205, 107, 202, 196), 3); g.DrawArc(shardCooldownPen, player.X - 33, player.Y - 33, 66, 66, 90, 360 * shardCd / VoiceShardCooldown()); }
        if (riverBell && bellCd > 0) { using var bellCooldownPen = new Pen(Color.FromArgb(210, 205, 169, 118), 3); g.DrawArc(bellCooldownPen, player.X - 41, player.Y - 41, 82, 82, 180, 360 * bellCd / BellPulseCooldown()); }
        if (ebbSeal && pullCd > 0) { using var ebbCooldownPen = new Pen(Color.FromArgb(210, 115, 185, 211), 3); g.DrawArc(ebbCooldownPen, player.X - 49, player.Y - 49, 98, 98, 270, 360 * pullCd / EbbPullCooldown()); }
        if (anchorWard && wardCd > 0) { using var wardCooldownPen = new Pen(Color.FromArgb(210, 179, 215, 215), 3); g.DrawArc(wardCooldownPen, player.X - 57, player.Y - 57, 114, 114, 0, 360 * wardCd / AnchorWardCooldown()); }
    }
    private void DrawEbbWave(Graphics g) { float progress = 1f - pullWaveTime / 0.45f; float radius = 160 * progress; using var wave = new Pen(Color.FromArgb((int)(210 * pullWaveTime / 0.45f), 98, 190, 220), 4); using var glow = new Pen(Color.FromArgb((int)(65 * pullWaveTime / 0.45f), 98, 190, 220), 12); g.DrawEllipse(glow, player.X - radius, player.Y - radius, radius * 2, radius * 2); g.DrawEllipse(wave, player.X - radius, player.Y - radius, radius * 2, radius * 2); }
    private void DrawBellWave(Graphics g) { float progress = 1f - bellWaveTime / 0.42f; float radius = bellWaveRadius * progress; using var wave = new Pen(Color.FromArgb((int)(210 * bellWaveTime / 0.42f), 115, 208, 197), 4); using var glow = new Pen(Color.FromArgb((int)(65 * bellWaveTime / 0.42f), 115, 208, 197), 12); g.DrawEllipse(glow, player.X - radius, player.Y - radius, radius * 2, radius * 2); g.DrawEllipse(wave, player.X - radius, player.Y - radius, radius * 2, radius * 2); }
    private static void DrawEchoShard(Graphics g, EchoShard shard)
    {
        PointF tail = Sub(shard.Pos, Scale(shard.Direction, 24));
        using var trail = new Pen(Color.FromArgb(150, 73, 193, 183), 6); using var core = new Pen(Color.FromArgb(235, 211, 225, 180), 2);
        using var glow = new SolidBrush(Color.FromArgb(82, 91, 225, 211)); using var crystal = new SolidBrush(Color.FromArgb(235, 115, 211, 195));
        g.DrawLine(trail, tail, shard.Pos); g.FillEllipse(glow, shard.Pos.X - 12, shard.Pos.Y - 12, 24, 24);
        PointF[] points = { Add(shard.Pos, Scale(shard.Direction, 10)), Add(shard.Pos, new PointF(-shard.Direction.Y * 5, shard.Direction.X * 5)), Add(shard.Pos, Scale(shard.Direction, -8)), Add(shard.Pos, new PointF(shard.Direction.Y * 5, -shard.Direction.X * 5)) };
        g.FillPolygon(crystal, points); g.DrawPolygon(core, points);
    }
    private void DrawInventory(Graphics g)
    {
        using var b = new SolidBrush(Color.FromArgb(245, 17, 19, 20)); using var itemFont = new Font("Segoe UI", 11); using var effectFont = new Font("Segoe UI", 9); g.FillRectangle(b, 150, 145, 660, 490);
        DrawCentered(g, "INVENTÁRIO   •   1–3 ARMAS   •   4 ARMADURA   •   5 RELÍQUIA   •   6/7 TALISMÃS   •   I FECHAR", 180, new Font("Segoe UI", 12, FontStyle.Bold), Color.Goldenrod);
        for (int i = 0; i < 3; i++) { bool found = (ownedWeapons & (1 << i)) != 0; Brush rarityBrush = !found ? Brushes.Gray : weaponRarities[i] == 2 ? Brushes.MediumTurquoise : weaponRarities[i] == 1 ? Brushes.Goldenrod : Brushes.AntiqueWhite; g.DrawString($"{i + 1}. {WeaponDisplayName(i)}    DANO {WeaponPower(i) - WeaponPower(weapon):+#;-#;0}    {(weapon == i ? "EQUIPADA" : found ? "DISPONÍVEL" : "NÃO ENCONTRADA")}", itemFont, rarityBrush, 205, 225 + i * 54); if (found) g.DrawString(WeaponAffixDescription(i), effectFont, Brushes.DarkGray, 232, 248 + i * 54); }
        g.DrawString($"4. PEITORAL DE PEDRA {(armorOn ? "EQUIPADO" : armor ? "DISPONÍVEL" : "NÃO ENCONTRADO")}", itemFont, Brushes.AntiqueWhite, 205, 385);
        g.DrawString($"5. RELICÁRIO VITAL {(relicOn ? "EQUIPADO" : relic ? "DISPONÍVEL" : "NÃO ENCONTRADO")}", itemFont, Brushes.AntiqueWhite, 205, 420);
        g.DrawString($"SELO DA VIGÍLIA {(archiveSeal ? "ENCONTRADO • +8 DANO" : "AINDA NÃO ENCONTRADO")}", itemFont, Brushes.AntiqueWhite, 205, 455);
        g.DrawString($"SINO DE NARA {(riverBell ? "ENCONTRADO • +20 VIDA MÁXIMA" : "AINDA NÃO ENCONTRADO")}", itemFont, Brushes.AntiqueWhite, 205, 490);
        g.DrawString($"PRISMA DO LUME {(lumeLens ? "ENCONTRADO • +12 DANO" : "AINDA NÃO ENCONTRADO")}", itemFont, Brushes.AntiqueWhite, 205, 525);
        g.DrawString($"RASTRO DE NARA {(naraTrace ? "RECUPERADO • MEMÓRIA PRESERVADA" : "AINDA NÃO ENCONTRADO")}", itemFont, Brushes.AntiqueWhite, 205, 560);
        string availableCharms = string.Join(" • ", Enumerable.Range(1, 5).Where(i => (ownedCharms & (1 << (i - 1))) != 0).Select(i => charmNames[i]));
        g.DrawString($"TALISMÃS: {(availableCharms.Length == 0 ? "NENHUM" : availableCharms)}   •   SAL VÍTREO: {tideGlass}", effectFont, Brushes.Plum, 205, 580);
        string firstCharm = equippedCharm == 0 ? "VAZIO" : charmNames[equippedCharm]; string secondCharm = equippedCharm2 == 0 ? "VAZIO" : charmNames[equippedCharm2];
        g.DrawString($"6. ESPAÇO I: {firstCharm}    7. ESPAÇO II: {secondCharm}", effectFont, Brushes.Plum, 205, 596);
        string effect = HasCharmPair(1, 2) ? "SINTONIA: Redemoinho +57% dano • penalidade cai para 0,5 s" : HasCharmPair(1, 3) ? "SINTONIA: +17 p.p. crítico • multiplicador 2,75x • ataque básico menos lento" : HasCharmPair(2, 3) ? "SINTONIA: esquiva longa sem acréscimo de recarga" : "Equipar dois talismãs únicos cria uma sintonia entre seus efeitos.";
        if (HasCharmPair(3, 5)) effect = "SINTONIA: Estilhaço +52% dano • alcance +44% • recarga +0,5 s";
        else if (HasCharm(5)) effect += " • Fio da Margem: alcance +44%, recarga +1,25 s";
        if (HasCharm(3) && !HasCharmPair(3, 5)) effect += " • Estilhaço +33% dano";
        if (HasCharm(4)) effect += " • Marca: +25% contra chefes, -20% contra outros inimigos";
        g.DrawString(effect, effectFont, Brushes.AntiqueWhite, new RectangleF(205, 612, 560, 22));
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
    private static string EnemyName(Enemy e) => e.Kind switch { EnemyKind.Guardian => "GUARDIÃO SEM NOME", EnemyKind.BlindScribe => "ESCRIBA CEGO", EnemyKind.RiverFerryman => "BARQUEIRO OCO", EnemyKind.AshWarden => "GUARDIÃO DAS CINZAS", EnemyKind.HollowName => "O NOME VAZIO", EnemyKind.NameWeaver => "A TECELÃ DE NOMES", EnemyKind.FirstKeeper => "A PRIMEIRA VIGÍLIA", EnemyKind.OathCartographer => "CARTÓGRAFA DO VÃO", EnemyKind.NameBroker => "ESCRIVÃO DE CONTRATO", EnemyKind.SaltEcho => "ECO SALINO", EnemyKind.TideWitness => "TESTEMUNHA DA MARÉ", EnemyKind.ReedKeeper => "GUARDIÃ DO LEITO", EnemyKind.FlowKeeper => "VIGIA DO VEIO PRESO", EnemyKind.CounterCurrent => "AFINADOR DAS MARGENS", EnemyKind.PaperWraith => "ESPECTRO DE PAPEL", EnemyKind.MudCantor => "CANTOR DO LODO", EnemyKind.DrownedMemory => "LEMBRANÇA AFOGADA", EnemyKind.EmberWisp => "VESTÍGIO DE BRASAS", EnemyKind.NameMoth => "MARIPOSA DE NOMES", EnemyKind.Elite => e.Armored ? "ELITE • BLINDADO" : "ELITE • FRENÉTICO", EnemyKind.Archer => "ARQUEIRO", EnemyKind.Mage => "CONJURADOR", EnemyKind.InkEcho => "ECO DE TINTA", _ => "" };
    private static PointF Normalize(PointF p, PointF fallback) { float n = Length(p); return n > 0.001f ? new PointF(p.X / n, p.Y / n) : fallback; }
    private static float Length(PointF p) => MathF.Sqrt(p.X * p.X + p.Y * p.Y);
    private static float Distance(PointF a, PointF b) => Length(Sub(a, b));
    private static float DistanceToSegment(PointF point, PointF start, PointF end) { PointF line = Sub(end, start); float lengthSquared = Dot(line, line); float t = lengthSquared > 0.001f ? Math.Clamp(Dot(Sub(point, start), line) / lengthSquared, 0, 1) : 0; return Distance(point, Add(start, Scale(line, t))); }
    private static PointF Sub(PointF a, PointF b) => new(a.X - b.X, a.Y - b.Y);
    private static PointF Offset(PointF a, float x, float y) => new(a.X + x, a.Y + y);
    private static PointF Add(PointF a, PointF b) => new(a.X + b.X, a.Y + b.Y);
    private static PointF Scale(PointF a, float m) => new(a.X * m, a.Y * m);
    private static float Dot(PointF a, PointF b) => a.X * b.X + a.Y * b.Y;

    public static int SelfTest()
    {
        using (var shardGame = new Game())
        {
            shardGame.timer.Stop(); shardGame.StartGame(); shardGame.storyOverlay = false; shardGame.room = 0; shardGame.SetupRoom(); shardGame.enemies.Clear(); shardGame.player = new PointF(150, 320); shardGame.mouseAim = new PointF(1, 0);
            var firstShardTarget = new Enemy { Pos = new PointF(230, 320), Kind = EnemyKind.Grunt, Hp = 300, MaxHp = 300 };
            var secondShardTarget = new Enemy { Pos = new PointF(300, 320), Kind = EnemyKind.Grunt, Hp = 300, MaxHp = 300 };
            shardGame.enemies.Add(firstShardTarget); shardGame.enemies.Add(secondShardTarget); shardGame.CastVoiceShard();
            if (shardGame.echoShards.Count != 1 || shardGame.shardCd != 7f) throw new InvalidOperationException("O Estilhaço de Voz não iniciou ou não recebeu recarga.");
            shardGame.AdvanceVoiceShards(0.4f);
            if (firstShardTarget.Hp >= 300 || secondShardTarget.Hp >= 300 || shardGame.echoShards.Count != 0) throw new InvalidOperationException($"O Estilhaço de Voz não atravessou os alvos até atingir uma parede: vida {firstShardTarget.Hp:0}/{secondShardTarget.Hp:0}, projéteis {shardGame.echoShards.Count}.");
            float shardDamage = shardGame.VoiceShardDamage(); shardGame.ownedCharms = 4; shardGame.equippedCharm = 3; shardGame.skillTalent = 2;
            if (shardGame.VoiceShardDamage() <= shardDamage || shardGame.VoiceShardCooldown() != 6f) throw new InvalidOperationException("O Vidro de Vigília ou o Domínio não alteraram a nova técnica.");
        }
        using (var bellGame = new Game())
        {
            bellGame.timer.Stop(); bellGame.StartGame(); bellGame.storyOverlay = false; bellGame.room = 6; bellGame.riverBell = true; bellGame.skillTalent = 2; bellGame.equippedCharm = 2; bellGame.equippedCharm2 = 3; bellGame.player = new PointF(250, 320); bellGame.enemies.Clear();
            var nearbyEcho = new Enemy { Pos = new PointF(330, 320), Kind = EnemyKind.Grunt, Hp = 300, MaxHp = 300, Windup = 0.6f, Cooldown = 0.2f };
            var distantEcho = new Enemy { Pos = new PointF(455, 320), Kind = EnemyKind.Grunt, Hp = 300, MaxHp = 300 };
            var bellBoss = new Enemy { Pos = new PointF(350, 320), Kind = EnemyKind.NameBroker, Hp = 1000, MaxHp = 1000, Windup = 0.6f, WindupMax = 0.6f, BossPattern = 1 };
            bellGame.enemies.Add(nearbyEcho); bellGame.enemies.Add(distantEcho); bellGame.enemies.Add(bellBoss); bellGame.CastBellPulse();
            if (nearbyEcho.Hp >= 300 || nearbyEcho.Stagger <= 0 || nearbyEcho.Windup != 0 || distantEcho.Hp != 300 || bellBoss.Hp >= 1000 || bellBoss.Windup <= 0) throw new InvalidOperationException("A Ressonância do Sino não separou dano, alcance e interrupção de inimigos comuns e chefes.");
            if (Math.Abs(bellGame.bellCd - 14.1f) > 0.01f || bellGame.bellWaveTime <= 0 || bellGame.BellPulseRadius() != 190f) throw new InvalidOperationException("O Fio de Travessia, Domínio ou o aviso visual não alterou a Ressonância do Sino.");
            float echoHealth = nearbyEcho.Hp; bellGame.UpdateEnemy(nearbyEcho, 0.2f);
            if (nearbyEcho.Hp != echoHealth || nearbyEcho.Stagger >= 0.95f) throw new InvalidOperationException("O atordoamento da Ressonância não segurou o eco durante a janela prevista.");
            float cooldown = bellGame.bellCd; bellGame.CastBellPulse();
            if (bellGame.bellCd != cooldown) throw new InvalidOperationException("A Ressonância do Sino ignorou sua recarga.");
        }
        using (var lockedBellGame = new Game())
        {
            lockedBellGame.timer.Stop(); lockedBellGame.StartGame(); lockedBellGame.storyOverlay = false; lockedBellGame.room = 6; lockedBellGame.enemies.Add(new Enemy { Pos = new PointF(260, 330), Kind = EnemyKind.Grunt, Hp = 100, MaxHp = 100 }); lockedBellGame.CastBellPulse();
            if (lockedBellGame.bellCd != 0 || lockedBellGame.memoryOverlay || !lockedBellGame.notice.Contains("SINO DE NARA")) throw new InvalidOperationException("A habilidade do Sino ficou disponível antes da descoberta narrativa.");
        }
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
        if (nameMoth