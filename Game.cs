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
    private const float InteractionRadius = 72;
    private const float MemoryStoneInteractionRadius = 58;
    private enum EnemyKind { Grunt, Archer, Mage, Elite, Guardian, InkEcho, BlindScribe, DrownedMemory, RiverFerryman, EmberWisp, AshWarden, NameMoth, HollowName, NameWeaver, FirstKeeper, MudCantor, OathCartographer, PaperWraith, NameBroker, SaltEcho, TideWitness, ReedKeeper, FlowKeeper, CounterCurrent, RiftCantor, GlassHerald, LanternRegent, RouteScribe, DeepSilence, BorrowedChorus }
    private enum VigilAspect { Hardened, Hungry, Swift, Chorus }
    private enum EliteAffix { Armored, Swift, Bloodbound, Spearbound }
    private sealed class Enemy
    {
        public PointF Pos, Target, Target2, Target2Start;
        public EnemyKind Kind;
        public float Hp, MaxHp, Cooldown, Windup, WindupMax, Flash, AnimationTime;
        public int BossPattern;
        public bool Armored, Moving;
        public bool VigilMinion;
        public EliteAffix EliteAffix;
        public float Stagger;
        public bool Boss => Kind is EnemyKind.Guardian or EnemyKind.BlindScribe or EnemyKind.RiverFerryman or EnemyKind.AshWarden or EnemyKind.HollowName or EnemyKind.NameWeaver or EnemyKind.FirstKeeper or EnemyKind.OathCartographer or EnemyKind.NameBroker or EnemyKind.TideWitness or EnemyKind.ReedKeeper or EnemyKind.FlowKeeper or EnemyKind.CounterCurrent or EnemyKind.RiftCantor or EnemyKind.GlassHerald or EnemyKind.LanternRegent or EnemyKind.RouteScribe or EnemyKind.DeepSilence or EnemyKind.BorrowedChorus;
        public float Radius => Boss ? 23 : Kind == EnemyKind.Elite ? 18 : 14;
    }
    private sealed class Drop { public PointF Pos; public string Kind = "gold"; public int Amount, Rarity, Bonus, Affix, Affix2; }
    private sealed class EchoShard { public PointF Pos, Direction; public float Life, Damage; public HashSet<Enemy> Hit = new(); }
    private sealed class FloatingText { public PointF Pos; public string Text = ""; public Color Color; public float TimeLeft, Duration; }
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
        public int[] WeaponAffixes2 { get; set; } = new int[3];
        public int[] EquippedSkills { get; set; } = new[] { 1, 0, 0 };
        public bool Armor { get; set; }
        public bool Relic { get; set; }
        public int HealthTalent { get; set; }
        public int SkillTalent { get; set; }
        public int DodgeTalent { get; set; }
        public int TechniqueTalent { get; set; }
        public bool RoomCleared { get; set; }
        public bool Completed { get; set; }
        public int MemoriesFound { get; set; }
        public int QuestStage { get; set; }
        public long ClearedRooms { get; set; }
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
        public bool RefrainSong { get; set; }
        public bool RiftVerse { get; set; }
        public bool DeepTrace { get; set; }
        public bool LanternSignal { get; set; }
        public bool BeaconLight { get; set; }
        public bool RouteMemory { get; set; }
        public bool SilenceMemory { get; set; }
        public bool BorrowedTrace { get; set; }
        public int FinalEnding { get; set; }
        public int VigilBestFloor { get; set; }
        public bool HighContrastTelegraphs { get; set; }
    }

    private static readonly RectangleF[] Walls = { new(345, 235, 48, 90), new(565, 375, 58, 80) };
    private static readonly Lazy<Bitmap?> AvenSpriteSheet = new(() =>
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "Aven.png");
        return File.Exists(path) ? new Bitmap(path) : null;
    });
    private static readonly Lazy<Bitmap?> PlayerAttackSpriteSheet = new(() =>
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "PlayerAttack.png");
        return File.Exists(path) ? new Bitmap(path) : null;
    });
    private static readonly Lazy<Bitmap?> EnemySpriteSheet = new(() =>
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "Enemies.png");
        return File.Exists(path) ? new Bitmap(path) : null;
    });
    private static readonly Lazy<Bitmap?> EnemyAttackSpriteSheet = new(() =>
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "EnemyAttacks.png");
        return File.Exists(path) ? new Bitmap(path) : null;
    });
    private static readonly Lazy<IReadOnlyDictionary<EnemyKind, Bitmap>> AdvancedEnemySprites = new(() =>
    {
        var sprites = new Dictionary<EnemyKind, Bitmap>();
        (EnemyKind Kind, string File)[] entries =
        {
            (EnemyKind.InkEcho, "InkEcho.png"), (EnemyKind.DrownedMemory, "DrownedMemory.png"),
            (EnemyKind.EmberWisp, "EmberWisp.png"), (EnemyKind.NameMoth, "NameMoth.png"),
            (EnemyKind.MudCantor, "MudCantor.png"), (EnemyKind.PaperWraith, "PaperWraith.png"),
            (EnemyKind.SaltEcho, "SaltEcho.png")
        };
        foreach (var entry in entries)
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Assets", entry.File);
            if (File.Exists(path)) sprites[entry.Kind] = new Bitmap(path);
        }
        return sprites;
    });
    private static readonly Lazy<Bitmap?> SpearboundEliteSprite = new(() =>
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "SpearboundElite.png");
        return File.Exists(path) ? new Bitmap(path) : null;
    });
    private static readonly Lazy<Bitmap?> BossSpriteSheet = new(() =>
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "Bosses.png");
        return File.Exists(path) ? new Bitmap(path) : null;
    });
    private static readonly Lazy<Bitmap?> MidBossSpriteSheet = new(() =>
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "BossesMidgame.png");
        return File.Exists(path) ? new Bitmap(path) : null;
    });
    private static readonly Lazy<Bitmap?> LateBossSpriteSheet = new(() =>
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "BossesLate.png");
        return File.Exists(path) ? new Bitmap(path) : null;
    });
    private static readonly Lazy<Bitmap?> FinalWaveBossSpriteSheet = new(() =>
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "BossesFinalWave.png");
        return File.Exists(path) ? new Bitmap(path) : null;
    });
    private static readonly Lazy<Bitmap?> EndgameBossSpriteSheet = new(() =>
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "BossesEndgame.png");
        return File.Exists(path) ? new Bitmap(path) : null;
    });
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
    private static readonly RectangleF[] CantoriaWalls = { new(250, 156, 52, 132), new(660, 156, 52, 132), new(250, 410, 52, 132), new(660, 410, 52, 132), new(420, 232, 48, 74), new(500, 350, 48, 74) };
    private static readonly RectangleF[] RiftChamberWalls = { new(244, 160, 58, 130), new(658, 160, 58, 130), new(244, 408, 58, 130), new(658, 408, 58, 130), new(432, 154, 96, 32), new(432, 472, 96, 32) };
    private static readonly RectangleF[] GlassGorgeWalls = { new(230, 154, 54, 125), new(650, 410, 55, 126), new(382, 205, 46, 92), new(535, 367, 50, 94), new(750, 160, 64, 92) };
    private static readonly RectangleF[] MirrorSourceWalls = { new(248, 155, 58, 135), new(654, 405, 58, 135), new(396, 246, 62, 54), new(502, 362, 62, 54) };
    private static readonly RectangleF[] LanternSpanWalls = { new(230, 154, 58, 126), new(648, 408, 58, 128), new(384, 203, 50, 92), new(530, 367, 54, 94), new(752, 160, 60, 92) };
    private static readonly RectangleF[] LowTideObservatoryWalls = { new(245, 158, 60, 128), new(655, 408, 60, 128), new(398, 244, 60, 56), new(502, 362, 60, 56) };
    private static readonly RectangleF[] OpenRouteWalls = { new(242, 162, 58, 132), new(660, 404, 58, 132), new(382, 210, 52, 82), new(530, 365, 54, 94) };
    private static readonly RectangleF[] UnwrittenHallWalls = { new(246, 155, 58, 132), new(656, 407, 58, 132), new(388, 247, 58, 72), new(514, 339, 58, 72) };
    private static readonly RectangleF[] SilenceThresholdWalls = { new(248, 154, 58, 132), new(654, 408, 58, 132), new(390, 238, 48, 80), new(520, 342, 48, 80) };
    private static readonly RectangleF[] SilenceChamberWalls = { new(238, 154, 56, 132), new(666, 408, 56, 132), new(328, 206, 50, 74), new(582, 380, 50, 74) };
    private static readonly RectangleF[] BorrowedDockWalls = { new(225, 146, 54, 138), new(681, 405, 54, 138), new(397, 202, 54, 68), new(511, 370, 54, 68) };
    private static readonly RectangleF[] BorrowedArchiveWalls = { new(222, 146, 56, 146), new(682, 404, 56, 140), new(394, 154, 42, 106), new(524, 390, 42, 106) };
    private static readonly RectangleF[] TownBuildings = { new(115, 128, 208, 128), new(677, 128, 208, 128), new(115, 426, 208, 120), new(677, 426, 208, 120) };
    private static readonly PointF CuratorPosition = new(625, 315), BellKeeperPosition = new(352, 347), ArchiveBookPosition = new(785, 355), CryptGatePosition = new(92, 330);
    private static readonly PointF AvenPosition = new(490, 335), SeraPosition = new(350, 340), NoaPosition = new(640, 360), VigilAltarPosition = new(775, 325), VigilArenaAltarPosition = new(480, 500);
    private static readonly PointF[] MemoryStonePositions = { new(260, 165), new(270, 485) };
    private static readonly string[] MemoryStoneTexts = {
        "Nara: “Se os sinos calarem, siga o leito do Rio Cego. A Curadora fechou os arquivos antes da noite começar.”",
        "Registro de Vigília: “O Guardião entregou seu nome ao Lume. A Curadora apagou o registro para que a cidade esquecesse o preço.”"
    };
    private readonly List<Enemy> enemies = new();
    private readonly List<Drop> drops = new();
    private readonly HashSet<Keys> down = new();
    private bool focusPaused;
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 16 };
    private readonly Random random = new();
    private readonly List<EchoShard> echoShards = new();
    private readonly List<FloatingText> floatingTexts = new();
    private PointF beaconPos;
    private readonly string[] roomNames = { "VESTÍBULO EM RUÍNAS", "SALÃO DOS OSSOS", "CÂMARA DO GUARDIÃO", "VÉSPERA • PRAÇA DO FAROL", "ARQUIVO NORTE • GALERIA", "ARQUIVO NORTE • COFRE", "RIO CEGO • LEITO SECO", "RIO CEGO • CAPELA AFOGADA", "FAROL • ESCADA DE CINZAS", "FAROL • CÂMARA DO LUME", "ALÉM DO LUME • MARGEM SEM REFLEXO", "RESERVATÓRIO • VOZES RETIDAS", "COSTA DAS VOZES TARDIAS", "CLAUSTRO DO CORO PARTIDO", "BACIA DO PRIMEIRO NOME", "SALA DA PRIMEIRA VIGÍLIA", "VAU DOS NOMES ESCOLHIDOS", "PÁTIO DAS TESTEMUNHAS", "REFÚGIO DO VEIO CALMO", "TRILHA DOS LUMINÁRIOS", "CISTERNA DO REGISTRO VAZIO", "VÁRZEA DAS VOZES SOPRADAS", "PONTE DA MARÉ INVERTIDA", "COTOVELO DOS JUNCAIS", "PÁTIO DAS AMARRAS LIVRES", "MOINHOS DA VAZANTE", "CÂMARA DO VEIO PRESO", "ESCARPA DO CONTRAFLUXO", "CORAÇÃO DAS DUAS MARGENS", "CANTARIA DAS VOZES LIVRES", "CÂMARA DA CONTRAVOZ", "GARGANTA DO VIDRO MUDO", "FONTE SOB A PEDRA", "PASSAGEM DAS LANTERNAS BAIXAS", "OBSERVATÓRIO DA MARÉ INTERIOR", "TRILHA DAS LINHAS ABERTAS", "GALERIA DA ROTA NÃO ESCRITA", "SOLEIRA DO SILÊNCIO", "CÂMARA DO ÚLTIMO ECO", "CAIS DOS ECOS EMPRESTADOS", "ARQUIVO DA MARÉ SEM NOME" };
    private readonly string[] weaponNames = { "ESPADA DE PEDRA", "LÂMINA DO ARQUIVO", "FACA DO CREPÚSCULO" };
    private readonly string[] charmNames = { "", "NÓ DE BRASA", "FIO DE TRAVESSIA", "VIDRO DE VIGÍLIA", "MARCA DA VIGÍLIA", "FIO DA MARGEM", "NÓ DE LUME", "FIO DO RETORNO", "ECO CONVERGENTE" };
    private readonly string[] skillNames = { "Vazio", "Estilhaço de Voz", "Ressonância do Sino", "Vazante", "Manto de Lastro", "Coro de Retorno", "Farol de Vigília", "Devolução das Vozes", "Última Centelha" };
    private readonly string[] skillDescriptions = { "", "Projétil perfurante de longo alcance", "Onda que fere e interrompe ecos", "Agrupa ecos próximos", "Reduz o dano recebido por 2,5 s", "Onda que fere e interrompe ecos", "Planta um farol que pulsa contra inimigos próximos", "Onda ampla que fere e restaura vida por inimigo atingido", "Golpe concentrado que causa dano elevado a um alvo" };
    private readonly string[] prologue = {
        "Na noite sem sinos, Véspera começou a esquecer os nomes de seus mortos. Sob as ruas, o Rio Cego repetiu uma voz que ninguém mais conseguia ouvir.",
        "Você é um Escutador: ouve as lembranças que a pedra não conseguiu guardar em silêncio. Entre elas reconheceu Nara, sua irmã, desaparecida quando os sinos pararam.",
        "A memória dela chega partida: “Desça. Ele ainda lembra.” O Guardião sem Nome espera na cripta, diante da única pista que pode explicar por que Nara foi até lá."
    };
    private readonly int[] weaponDamage = { 0, 10, 4 };
    private readonly int[] weaponBonuses = new int[3], weaponRarities = new int[3], weaponAffixes = new int[3], weaponAffixes2 = new int[3];
    private PointF player = new(150, 330), facing = new(1, 0), mouseAim = new(1, 0), dashDirection = new(1, 0);
    private float playerAnimationTime;
    private int room, level = 1, xp, gold, potions = 2, damage = 25, weapon, ownedWeapons = 1;
    private int healthTalent, skillTalent, dodgeTalent, techniqueTalent, pendingTalents, enemiesDefeated;
    private int memoriesFound, questStage, tideGlass, finalEnding;
    private int vigilBestFloor, vigilFloor;
    private float vigilMinionCooldown;
    private bool vigilActive, vigilChoiceOpen;
    private bool highContrastTelegraphs;
    private bool silenceMemory;
    private bool borrowedTrace;
    private long clearedRooms;
    private bool chorusMemory, firstMemory, westTrace, upstreamTrace, forgeMenuOpen;
    private bool beyondTrace, ebbSeal, counterTrace, anchorWard, refrainSong, riftVerse, deepTrace, lanternSignal, beaconLight, routeMemory;
    private int ownedCharms, equippedCharm, equippedCharm2;
    private int[] equippedSkills = { 1, 0, 0 };
    private bool archiveSeal;
    private bool riverBell;
    private bool lumeLens;
    private bool naraTrace;
    private float health = 100, maxHealth = 100, attackCd, attackFlash, skillCd, shardCd, bellCd, bellWaveTime, bellWaveRadius, pullCd, pullWaveTime, wardCd, wardTime, refrainCd, refrainWaveTime, refrainWaveRadius, beaconCd, beaconTime, beaconPulseCd, beaconWaveTime, returnVoiceCd, returnVoiceWaveTime, returnVoiceWaveRadius, finalStrikeCd, finalStrikeFlash, dashCd, dashTime, invulnerable, noticeTime, windupPulse;
    private PointF finalStrikeTarget;
    private bool armor, armorOn, relic, relicOn, inventoryOpen, skillMenuOpen, characterSheetOpen, mapOpen, title = true, dead, victory, talentChoice, chestChoice, storyOverlay, memoryOverlay, endingChoiceOpen;
    private int skillMenuSlot;
    private int townEnding;
    private int storyPage;
    private string notice = "";
    private string memoryTitle = "";
    private string memoryText = "";
    private string SavePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Scadryk", "save.json");

    public Game()
    {
        Text = "Scadryk — RPG de ação 2D"; ClientSize = new Size(960, 640); MinimumSize = new Size(816, 579);
        FormBorderStyle = FormBorderStyle.Sizable; MaximizeBox = true; StartPosition = FormStartPosition.CenterScreen;
        DoubleBuffered = true; KeyPreview = true; BackColor = Color.FromArgb(16, 19, 23);
        timer.Tick += (_, _) => TickGame(); timer.Start();
        KeyDown += OnKeyDown; KeyUp += (_, e) => down.Remove(e.KeyCode);
        Deactivate += (_, _) => PauseForLostFocus();
        Activated += (_, _) => ResumeAfterFocus();
        MouseMove += (_, e) => { PointF aim = ScreenToGame(new PointF(e.X, e.Y), ClientSize); mouseAim = Normalize(new PointF(aim.X - player.X, aim.Y - player.Y), facing); };
        MouseDown += (_, e) =>
        {
            if (storyOverlay) { if (e.Button == MouseButtons.Left) AdvanceStory(); return; }
            if (memoryOverlay) { if (e.Button == MouseButtons.Left) memoryOverlay = false; return; }
            if (GameplayPaused()) return;
            PointF aim = ScreenToGame(new PointF(e.X, e.Y), ClientSize); mouseAim = Normalize(new PointF(aim.X - player.X, aim.Y - player.Y), facing);
            if (e.Button == MouseButtons.Left) Attack();
            if (e.Button == MouseButtons.Right) Whirlwind();
        };
    }

    private void StartGame()
    {
        room = 0; level = 1; xp = gold = damage = 0; damage = 25; potions = 2; weapon = 0; ownedWeapons = 1; Array.Clear(weaponBonuses); Array.Clear(weaponRarities); Array.Clear(weaponAffixes); Array.Clear(weaponAffixes2);
        armor = armorOn = relic = relicOn = archiveSeal = riverBell = lumeLens = naraTrace = chorusMemory = firstMemory = westTrace = upstreamTrace = beyondTrace = ebbSeal = counterTrace = anchorWard = refrainSong = riftVerse = deepTrace = lanternSignal = beaconLight = routeMemory = silenceMemory = inventoryOpen = skillMenuOpen = characterSheetOpen = mapOpen = dead = victory = chestChoice = talentChoice = memoryOverlay = endingChoiceOpen = forgeMenuOpen = vigilActive = vigilChoiceOpen = false;
        finalEnding = 0;
        vigilBestFloor = vigilFloor = 0;
        townEnding = 0;
        healthTalent = skillTalent = dodgeTalent = techniqueTalent = pendingTalents = enemiesDefeated = memoriesFound = questStage = tideGlass = 0;
        clearedRooms = 0;
        ownedCharms = equippedCharm = equippedCharm2 = 0;
        equippedSkills = new[] { 1, 0, 0 }; skillMenuOpen = false; skillMenuSlot = 0;
        health = maxHealth = 100; attackCd = attackFlash = skillCd = shardCd = bellCd = bellWaveTime = bellWaveRadius = pullCd = pullWaveTime = wardCd = wardTime = refrainCd = refrainWaveTime = refrainWaveRadius = beaconCd = beaconTime = beaconPulseCd = beaconWaveTime = returnVoiceCd = returnVoiceWaveTime = returnVoiceWaveRadius = finalStrikeCd = finalStrikeFlash = dashCd = dashTime = invulnerable = 0; vigilMinionCooldown = 0; beaconPos = finalStrikeTarget = PointF.Empty; facing = mouseAim = dashDirection = new PointF(1, 0); playerAnimationTime = 0; echoShards.Clear(); floatingTexts.Clear(); title = false; storyPage = 0; storyOverlay = true; SetupRoom();
    }

    private void AdvanceStory()
    {
        if (!storyOverlay) return;
        storyPage++;
        if (storyPage >= prologue.Length) storyOverlay = false;
    }

    private static EnemyKind VigilBossKind(int floor) => ((Math.Max(1, floor) - 1) % 5) switch
    {
        0 => EnemyKind.DeepSilence,
        1 => EnemyKind.RouteScribe,
        2 => EnemyKind.LanternRegent,
        3 => EnemyKind.GlassHerald,
        _ => EnemyKind.RiftCantor
    };

    private static VigilAspect VigilAspectForFloor(int floor) => (Math.Max(1, floor) % 4) switch
    {
        1 => VigilAspect.Hardened,
        2 => VigilAspect.Hungry,
        3 => VigilAspect.Swift,
        _ => VigilAspect.Chorus
    };

    private static string VigilAspectTitle(int floor) => VigilAspectForFloor(floor) switch
    {
        VigilAspect.Hardened => "CASCA VIVA",
        VigilAspect.Hungry => "FOME DO RIO",
        VigilAspect.Swift => "SUSSURRO VELOZ",
        _ => "CORO ERRANTE"
    };

    private static string VigilAspectRule(int floor) => VigilAspectForFloor(floor) switch
    {
        VigilAspect.Hardened => "O chefe recebe 25% menos dano",
        VigilAspect.Hungry => "Os ataques do chefe causam 30% mais dano",
        VigilAspect.Swift => "Os ataques começam 20% mais rápido",
        _ => "Um Eco Salino surge a cada 20 segundos"
    };

    private static Color VigilAspectColor(int floor) => VigilAspectForFloor(floor) switch
    {
        VigilAspect.Hardened => Color.SteelBlue,
        VigilAspect.Hungry => Color.OrangeRed,
        VigilAspect.Swift => Color.Goldenrod,
        _ => Color.MediumPurple
    };

    private void TickVigilMinions(float dt)
    {
        if (!vigilActive || room != 38 || VigilAspectForFloor(vigilFloor) != VigilAspect.Chorus || drops.Count > 0 || enemies.Any(enemy => enemy.VigilMinion)) return;
        Enemy? boss = enemies.FirstOrDefault(enemy => enemy.Boss);
        if (boss is null) return;
        vigilMinionCooldown -= dt;
        if (vigilMinionCooldown > 0) return;
        PointF[] spawnPoints = { new(180, 185), new(780, 185), new(180, 465), new(780, 465) };
        PointF spawn = spawnPoints.Where(point => !Blocked(point, 14)).OrderByDescending(point => Distance(player, point)).FirstOrDefault(new PointF(780, 330));
        float hp = Math.Clamp(boss.MaxHp * 0.035f, 80, 220);
        enemies.Add(new Enemy { Pos = spawn, Kind = EnemyKind.SaltEcho, Hp = hp, MaxHp = hp, Cooldown = 0.8f, VigilMinion = true });
        vigilMinionCooldown = 20;
        notice = "CORO ERRANTE • UM ECO SALINO ENTROU NA LUTA"; noticeTime = 2.4f;
        GameAudio.Play(GameAudio.Cue.BossWarning);
    }

    private void StartVigil()
    {
        if (finalEnding == 0) return;
        vigilActive = true; vigilChoiceOpen = false; vigilFloor = vigilBestFloor + 1;
        room = 38; SetupRoom();
        ShowMemory($"VIGÍLIA ASCENDENTE • ANDAR {vigilFloor}", "Aven reúne ecos de antigos guardiões sem lhes devolver os nomes. Cada andar torna a lembrança mais intensa e deixa um prêmio para quem atravessar a prova. Derrote o eco, recolha o saque e interaja com o altar para subir ou encerrar a vigília.");
    }

    private void ResolveVigilFloor(bool ascend)
    {
        if (!vigilActive || enemies.Count > 0 || drops.Count > 0) return;
        vigilBestFloor = Math.Max(vigilBestFloor, vigilFloor);
        int goldReward = 100 + Math.Min(900, (vigilFloor - 1) * 25);
        int glassReward = 1 + Math.Min(5, (vigilFloor - 1) / 3);
        gold += goldReward; tideGlass = Math.Min(999, tideGlass + glassReward);
        if (vigilFloor % 3 == 0) potions++;
        if (ascend)
        {
            vigilFloor++; SetupRoom();
            notice = $"VIGÍLIA {vigilBestFloor} CONCLUÍDA • +{goldReward} OURO • +{glassReward} SAL VÍTREO"; noticeTime = 3;
            return;
        }
        vigilActive = false; vigilChoiceOpen = false; vigilFloor = 0; room = 18; SetupRoom(); health = maxHealth;
        ShowMemory("A VIGÍLIA SE ENCERRA", $"A comunidade registra sua melhor marca: andar {vigilBestFloor}. Aven recolhe os ecos para que possam repousar. Você recebe {goldReward} ouro e {glassReward} sal vítreo; a jornada pode ser retomada quando quiser.");
    }

    private void SetupRoom()
    {
        player = new PointF(150, 330); enemies.Clear(); drops.Clear(); echoShards.Clear(); lockedTarget = null;
        if (vigilActive && room == 38) vigilMinionCooldown = 20;
        if (room != 3 && !vigilActive && (clearedRooms & (1L << room)) != 0) return;
        int count = room is 3 or 18 ? 0 : room == 0 ? 3 : room == 1 ? 5 : room == 4 ? 5 : room == 5 ? 4 : room == 6 ? 6 : room is 21 or 29 or 31 or 33 or 35 or 37 ? 6 : room == 22 ? 5 : room == 23 ? 6 : room == 24 ? 5 : room == 25 ? 6 : room is 26 or 28 or 30 or 32 or 34 or 36 ? 5 : room is 38 or 40 ? 1 : room == 39 ? 5 : room == 27 ? 6 : (room is 7 or 9 or 11 or 15 or 17 or 20) ? 4 : room == 8 ? 5 : room is 10 or 12 or 14 or 16 or 19 ? 6 : room == 13 ? 4 : 4;
        for (int i = 0; i < count; i++)
        {
            EnemyKind kind = room == 2 && i == 0 ? EnemyKind.Guardian : room == 5 && i == 0 ? EnemyKind.BlindScribe : room == 7 && i == 0 ? EnemyKind.RiverFerryman : room == 9 && i == 0 ? EnemyKind.AshWarden : room == 13 && i == 0 ? EnemyKind.NameWeaver : room == 15 && i == 0 ? EnemyKind.FirstKeeper : room == 17 && i == 0 ? EnemyKind.OathCartographer : room == 20 && i == 0 ? EnemyKind.NameBroker : room == 26 && i == 0 ? EnemyKind.FlowKeeper : room == 24 && i == 0 ? EnemyKind.ReedKeeper : room == 22 && i == 0 ? EnemyKind.TideWitness : room == 11 && i == 0 ? EnemyKind.HollowName : room == 1 && i == count - 1 ? EnemyKind.Elite : room == 4 && (i is 1 or 4) ? EnemyKind.InkEcho : room == 5 && i == 2 ? EnemyKind.InkEcho : room == 6 && (i is 1 or 4) ? EnemyKind.DrownedMemory : room == 7 && i == 2 ? EnemyKind.DrownedMemory : room == 8 && (i is 1 or 4) ? EnemyKind.EmberWisp : room == 9 && i == 2 ? EnemyKind.EmberWisp : room == 10 && (i is 1 or 4) ? EnemyKind.NameMoth : room is 12 && (i is 1 or 4) ? EnemyKind.NameMoth : (room is 11 or 13 or 14 or 15 or 16 or 17 or 19 or 20) && i == 2 ? EnemyKind.NameMoth : (room is 12 or 13 or 14 or 15 or 16 or 17 or 19 or 20) && i == 1 ? EnemyKind.NameMoth : room == 16 && i == 4 ? EnemyKind.MudCantor : room == 17 && i == 3 ? EnemyKind.MudCantor : (room is 19 or 20) && i == 3 ? EnemyKind.PaperWraith : (room is 21 or 22 or 23 or 24 or 25 or 26) && i == 2 ? EnemyKind.SaltEcho : (room is 23 or 24 or 25 or 26) && i == 3 ? EnemyKind.MudCantor : room > 0 && i == 1 ? EnemyKind.Archer : (room is 2 or 5 or 7 or 9 or 11 or 14 or 16 or 19) && i == 2 ? EnemyKind.Mage : EnemyKind.Grunt;
            if (room == 28 && i == 0) kind = EnemyKind.CounterCurrent;
            if (room == 29 && i == 0) kind = EnemyKind.Elite;
            if (room == 29 && i == 4) kind = EnemyKind.NameMoth;
            if (room == 30 && i == 0) kind = EnemyKind.RiftCantor;
            if (room == 32 && i == 0) kind = EnemyKind.GlassHerald;
            if (room == 34 && i == 0) kind = EnemyKind.LanternRegent;
            if (room == 36 && i == 0) kind = EnemyKind.RouteScribe;
            if (room == 38 && i == 0) kind = EnemyKind.DeepSilence;
            if (room == 40 && i == 0) kind = EnemyKind.BorrowedChorus;
            if (vigilActive && i == 0) kind = VigilBossKind(vigilFloor);
            if (room >= 27 && i == 2) kind = EnemyKind.SaltEcho;
            if (room >= 27 && i == 3) kind = EnemyKind.MudCantor;
            if (!vigilActive && i == count - 1 && room is 23 or 25 or 27 or 31 or 33 or 35 or 37 or 39) kind = EnemyKind.Elite;
            float hp = kind switch { EnemyKind.Guardian => 160, EnemyKind.BlindScribe => 245, EnemyKind.RiverFerryman => 285, EnemyKind.AshWarden => 340, EnemyKind.HollowName => 390, EnemyKind.NameWeaver => townEnding == 1 ? 410 : 455, EnemyKind.FirstKeeper => townEnding == 1 ? 560 : 620, EnemyKind.OathCartographer => townEnding == 1 ? 610 : 690, EnemyKind.NameBroker => townEnding == 1 ? 720 : 790, EnemyKind.TideWitness => townEnding == 1 ? 820 : 920, EnemyKind.ReedKeeper => townEnding == 1 ? 970 : 1060, EnemyKind.FlowKeeper => townEnding == 1 ? 1120 : 1230, EnemyKind.CounterCurrent => townEnding == 1 ? 1320 : 1440, EnemyKind.RiftCantor => townEnding == 1 ? 1480 : 1600, EnemyKind.GlassHerald => townEnding == 1 ? 1740 : 1880, EnemyKind.LanternRegent => townEnding == 1 ? 1920 : 2080, EnemyKind.RouteScribe => townEnding == 1 ? 2250 : 2450, EnemyKind.DeepSilence => townEnding == 1 ? 3450 : 3750, EnemyKind.BorrowedChorus => townEnding == 1 ? 4050 : 4450, EnemyKind.Elite => 120 + Math.Max(0, room - 1) * 6, EnemyKind.Archer or EnemyKind.Mage or EnemyKind.InkEcho or EnemyKind.DrownedMemory or EnemyKind.EmberWisp or EnemyKind.NameMoth or EnemyKind.MudCantor or EnemyKind.PaperWraith or EnemyKind.SaltEcho => 72, _ => 58 + room * 8 };
            if (vigilActive) hp = 3450 * (1 + Math.Max(0, vigilFloor - 1) * 0.28f);
            EliteAffix eliteAffix = kind == EnemyKind.Elite ? EliteAffixForRoll(random.Next(4)) : EliteAffix.Armored;
            var enemy = new Enemy { Pos = new PointF(485 + i % 3 * 120, 215 + i / 3 * 165), Kind = kind, Hp = hp, MaxHp = hp, Cooldown = 0.7f + (float)random.NextDouble(), EliteAffix = eliteAffix, Armored = kind == EnemyKind.Elite && eliteAffix == EliteAffix.Armored };
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
        if (questStage == 51 && silenceMemory && finalEnding == 0 && choice is 1 or 2)
        {
            finalEnding = choice; questStage = choice == 1 ? 52 : 53; endingChoiceOpen = false;
            if (choice == 1)
            {
                maxHealth += 20; health = maxHealth;
                ShowMemory("EPÍLOGO — NOMES DEVOLVIDOS", "O Escutador abre o arquivo e devolve as lembranças às vozes que as reconheceram. O Lume perde sua antiga força; agora a comunidade precisa proteger Véspera em conjunto. Sileth mantém o arquivo aberto, Edda toca os sinos sem exigir resposta. Nara continua além do mapa, livre para voltar ou seguir seu caminho. O Silêncio já não é uma prisão única: cada memória pode ser ouvida por quem a reivindicar. Vida máxima +20.");
            }
            else
            {
                damage += 10;
                ShowMemory("EPÍLOGO — A LUZ QUE FICA", "O Escutador oferece ao Lume uma lembrança própria: o primeiro som que o trouxe a Véspera. O farol continua protegendo a cidade, mas deixa de chamar as vozes perdidas de volta à força. Sileth registra o preço e mantém o arquivo aberto; Edda toca os sinos para lembrar a cidade de escutar quem chega. Nara continua além do mapa, livre para escolher se algum dia retorna. Dano +10.");
            }
            notice = "DESTINO DO LUME REGISTRADO • EPÍLOGO CONCLUÍDO"; noticeTime = 4; return;
        }
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
        if (focusPaused) return;
        if (e.KeyCode == Keys.N)
        {
            bool nowMuted = GameAudio.ToggleMute();
            notice = nowMuted ? "ÁUDIO DESATIVADO" : "ÁUDIO ATIVADO"; noticeTime = 1.5f;
            Invalidate(); return;
        }
        if (e.KeyCode == Keys.H)
        {
            highContrastTelegraphs = !highContrastTelegraphs;
            notice = highContrastTelegraphs ? "AVISOS DE ATAQUE EM ALTO CONTRASTE" : "AVISOS DE ATAQUE PADRÃO"; noticeTime = 1.8f;
            Invalidate(); return;
        }
        down.Add(e.KeyCode);
        if (e.KeyCode == Keys.Enter && (title || dead || victory)) { StartGame(); return; }
        if (e.KeyCode == Keys.C && title) { LoadGame(); return; }
        if (e.KeyCode == Keys.F5 && victory) { SaveGame(); return; }
        if (title || dead || victory) return;
        if (storyOverlay) { if (e.KeyCode is Keys.Enter or Keys.Space) AdvanceStory(); return; }
        if (memoryOverlay) { if (e.KeyCode is Keys.Enter or Keys.Space) memoryOverlay = false; return; }
        if (mapOpen) { if (e.KeyCode is Keys.M or Keys.Escape) mapOpen = false; return; }
        if (characterSheetOpen) { if (e.KeyCode is Keys.P or Keys.Escape) characterSheetOpen = false; return; }
        if (skillMenuOpen)
        {
            if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.B) { skillMenuOpen = false; return; }
            if (e.KeyCode is Keys.F or Keys.V or Keys.G) { skillMenuSlot = e.KeyCode == Keys.F ? 0 : e.KeyCode == Keys.V ? 1 : 2; return; }
            if (e.KeyCode == Keys.Back) { equippedSkills[skillMenuSlot] = 0; return; }
            if (e.KeyCode is >= Keys.D1 and <= Keys.D8) { EquipSkill(skillMenuSlot, (int)e.KeyCode - (int)Keys.D0); return; }
            return;
        }
        if (forgeMenuOpen) { if (e.KeyCode is >= Keys.D1 and <= Keys.D7) ReforgeWeapon((int)e.KeyCode - (int)Keys.D0); else if (e.KeyCode == Keys.Escape) forgeMenuOpen = false; return; }
        if (endingChoiceOpen) { if (e.KeyCode == Keys.D1) ChooseTownEnding(1); else if (e.KeyCode == Keys.D2) ChooseTownEnding(2); else if (e.KeyCode == Keys.D3) endingChoiceOpen = false; return; }
        if (vigilChoiceOpen) { if (e.KeyCode == Keys.D1) ResolveVigilFloor(true); else if (e.KeyCode == Keys.D2) ResolveVigilFloor(false); else if (e.KeyCode == Keys.D3 || e.KeyCode == Keys.Escape) vigilChoiceOpen = false; return; }
        if (chestChoice) { if (e.KeyCode == Keys.D1) gold += 75; else if (e.KeyCode == Keys.D2) potions += 2; else if (e.KeyCode == Keys.D3) health = maxHealth; else return; chestChoice = false; EnterVesper(); return; }
        if (talentChoice) { SelectTalent(e.KeyCode); return; }
        if (e.KeyCode == Keys.P) { characterSheetOpen = true; inventoryOpen = false; skillMenuOpen = false; return; }
        if (e.KeyCode == Keys.M) { mapOpen = true; inventoryOpen = false; skillMenuOpen = false; return; }
        if (e.KeyCode == Keys.I) { inventoryOpen = !inventoryOpen; return; }
        if (e.KeyCode == Keys.B) { skillMenuOpen = true; inventoryOpen = false; return; }
        if (inventoryOpen) { if (e.KeyCode >= Keys.D1 && e.KeyCode <= Keys.D3) EquipWeapon((int)e.KeyCode - (int)Keys.D1); else if (e.KeyCode == Keys.D4 && armor) armorOn = !armorOn; else if (e.KeyCode == Keys.D5 && relic) relicOn = !relicOn; else if (e.KeyCode == Keys.D6) CycleCharm(1); else if (e.KeyCode == Keys.D7) CycleCharm(2); return; }
        if (e.KeyCode == Keys.Space || e.KeyCode == Keys.J) Attack();
        else if (e.KeyCode == Keys.ShiftKey || e.KeyCode == Keys.K) StartDash();
        else if (e.KeyCode == Keys.R) Whirlwind();
        else if (e.KeyCode == Keys.F) CastEquippedSkill(0);
        else if (e.KeyCode == Keys.V) CastEquippedSkill(1);
        else if (e.KeyCode == Keys.G) CastEquippedSkill(2);
        else if (e.KeyCode == Keys.Q) UsePotion();
        else if (e.KeyCode == Keys.E) Interact();
        else if (e.KeyCode == Keys.X) SalvageNearbyWeapon();
        else if (e.KeyCode == Keys.F5) { if (vigilActive) { notice = "TERMINE A VIGÍLIA E SALVE NO REFÚGIO"; noticeTime = 2; } else if (enemies.Count == 0 && drops.Count == 0) SaveGame(); else { notice = "LIMPE A SALA E RECOLHA O LOOT ANTES DE SALVAR"; noticeTime = 2; } }
        else if (e.KeyCode == Keys.Tab) CycleTarget();
    }

    private void ReforgeWeapon(int affix)
    {
        if (!forgeMenuOpen || room != 18 || questStage < 27 || Distance(player, AvenPosition) >= 60) { forgeMenuOpen = false; return; }
        if (affix == 7) { RespecTalents(); return; }
        if (affix is < 1 or > 6) return;
        if (gold < 75 || tideGlass < 1) { notice = "A TÊMPERA CUSTA 75 OURO E 1 SAL VÍTREO"; noticeTime = 2; return; }
        if (weaponAffixes[weapon] == affix) { notice = "ESSA ARMA JÁ GUARDA ESSE AFFIX"; noticeTime = 2; return; }
        int oldPower = WeaponPower(weapon);
        if (weaponRarities[weapon] == 0) { weaponRarities[weapon] = 1; weaponBonuses[weapon] = random.Next(3, 7); }
        if (weaponRarities[weapon] == 2 && weaponAffixes2[weapon] == affix) (weaponAffixes[weapon], weaponAffixes2[weapon]) = (weaponAffixes2[weapon], weaponAffixes[weapon]);
        else weaponAffixes[weapon] = affix;
        damage += WeaponPower(weapon) - oldPower;
        gold -= 75; tideGlass--; forgeMenuOpen = false;
        notice = $"TÊMPERA: {weaponNames[weapon]} {WeaponRarityName(weaponRarities[weapon])} {WeaponAffixDescription(weapon)}"; noticeTime = 3;
    }

    private static int TalentRespecGoldCost(int level) => 250 + Math.Max(1, level) * 100;
    private static int TalentRespecGlassCost(int level) => 5 + Math.Max(1, level) / 5;

    private void RespecTalents()
    {
        int spent = healthTalent + skillTalent + dodgeTalent + techniqueTalent;
        int goldCost = TalentRespecGoldCost(level), glassCost = TalentRespecGlassCost(level);
        if (pendingTalents > 0 || talentChoice) { notice = "GASTE OS PONTOS DE TALENTO PENDENTES PRIMEIRO"; noticeTime = 2; return; }
        if (spent == 0) { notice = "NÃO HÁ TALENTOS PARA REDISTRIBUIR"; noticeTime = 2; return; }
        if (gold < goldCost || tideGlass < glassCost) { notice = $"REDISTRIBUIR CUSTA {goldCost} OURO E {glassCost} SAL VÍTREO"; noticeTime = 2.5f; return; }
        float healthRatio = maxHealth > 0 ? health / maxHealth : 1;
        gold -= goldCost; tideGlass -= glassCost; maxHealth = Math.Max(1, maxHealth - healthTalent * 20); healthTalent = skillTalent = dodgeTalent = techniqueTalent = 0; health = Math.Clamp(maxHealth * healthRatio, 1, maxHealth);
        pendingTalents = 0; talentChoice = false; forgeMenuOpen = false; notice = $"TALENTOS REDISTRIBUÍDOS: {spent} PONTOS DEVOLVIDOS"; noticeTime = 3;
    }

    private void SelectTalent(Keys key)
    {
        if (key == Keys.D1) { healthTalent++; maxHealth += 20; health = Math.Min(maxHealth, health + 20); }
        else if (key == Keys.D2) skillTalent++;
        else if (key == Keys.D3) dodgeTalent++;
        else if (key == Keys.D4) techniqueTalent++;
        else return;
        pendingTalents--; talentChoice = pendingTalents > 0;
    }

    private void Attack()
    {
        if (attackCd > 0) return;
        facing = lockedTarget is not null && enemies.Contains(lockedTarget) ? Normalize(Sub(lockedTarget.Pos, player), facing) : mouseAim;
        attackCd = BasicAttackCooldown();
        attackFlash = 0.16f;
        GameAudio.Play(GameAudio.Cue.Strike);
        foreach (Enemy enemy in enemies.ToArray())
        {
            PointF offset = Sub(enemy.Pos, player); float distance = Length(offset);
            if (distance > BasicAttackReach(enemy) || (distance >= 20 && !BasicAttackCoversDirection(Dot(facing, Normalize(offset, facing)), weapon)) || BlockedLine(player, enemy.Pos)) continue;
            bool critical = random.NextDouble() < CriticalChance(); int dealt = (int)(damage * BasicAttackDamageMultiplier(enemy) * OutgoingDamageMultiplier(enemy) * (critical ? CriticalMultiplier() : 1));
            DamageEnemy(enemy, dealt, critical); enemy.Flash = 0.2f;
            if (weapon == 1 && !enemy.Boss) { enemy.Stagger = Math.Max(enemy.Stagger, 0.2f); enemy.Windup = 0; }
            if (enemy.Hp <= 0) Defeat(enemy);
        }
    }

    private void Whirlwind()
    {
        if (skillCd > 0 || talentChoice) return;
        skillCd = (Math.Max(1.5f, 6f - skillTalent * 0.5f) + WhirlwindCooldownPenalty()) * WeaponCooldownMultiplier();
        foreach (Enemy enemy in enemies.ToArray())
        {
            if (Distance(player, enemy.Pos) >= 110) continue;
            int dealt = (int)(damage * OutgoingDamageMultiplier(enemy) * WhirlwindMultiplier() * WeaponSkillMultiplier() * TechniqueDamageMultiplier() * (IsArmored(enemy) ? 0.75f : 1));
            DamageEnemy(enemy, dealt);
            if (enemy.Hp <= 0) Defeat(enemy);
        }
    }

    private void CastVoiceShard()
    {
        if (room == 3 || shardCd > 0 || enemies.Count == 0) return;
        facing = lockedTarget is not null && enemies.Contains(lockedTarget) ? Normalize(Sub(lockedTarget.Pos, player), facing) : Normalize(mouseAim, facing);
        echoShards.Add(new EchoShard { Pos = Add(player, Scale(facing, 23)), Direction = facing, Life = VoiceShardLifetime(), Damage = VoiceShardDamage() });
        shardCd = VoiceShardCooldown(); notice = "ESTILHAÇO DE VOZ"; noticeTime = 0.75f;
    }

    private bool SkillUnlocked(int skill) => skill switch { 1 => true, 2 => riverBell, 3 => ebbSeal, 4 => anchorWard, 5 => refrainSong, 6 => beaconLight, 7 => finalEnding == 1, 8 => finalEnding == 2, _ => false };
    private void EquipSkill(int slot, int skill)
    {
        if (!SkillUnlocked(skill)) { notice = "ESSA TÉCNICA AINDA NÃO FOI APRENDIDA"; noticeTime = 1.5f; return; }
        if (equippedSkills.Where((_, index) => index != slot).Contains(skill)) { notice = "CADA TÉCNICA SÓ PODE OCUPAR UM ESPAÇO"; noticeTime = 1.5f; return; }
        equippedSkills[slot] = skill; notice = $"ESPAÇO {slot + 1}: {skillNames[skill]}"; noticeTime = 1.5f;
    }
    private void CastEquippedSkill(int slot)
    {
        int skill = equippedSkills[slot];
        if (skill == 0) { notice = $"ESPAÇO {slot + 1} VAZIO • PRESSIONE B PARA MONTAR TÉCNICAS"; noticeTime = 1.5f; return; }
        if (!SkillUnlocked(skill)) { notice = "TÉCNICA AINDA NÃO APRENDIDA"; noticeTime = 1.5f; return; }
        GameAudio.Play(GameAudio.Cue.Skill);
        switch (skill) { case 1: CastVoiceShard(); break; case 2: CastBellPulse(); break; case 3: CastEbbPull(); break; case 4: CastAnchorWard(); break; case 5: CastRefrain(); break; case 6: CastBeacon(); break; case 7: CastReturnedVoices(); break; case 8: CastFinalSpark(); break; }
    }
    private static bool ValidSkillLoadout(SaveData s)
    {
        if (s.EquippedSkills is null || s.EquippedSkills.Length != 3 || s.EquippedSkills.Any(skill => skill is < 0 or > 8) || s.EquippedSkills.Where(skill => skill != 0).Distinct().Count() != s.EquippedSkills.Count(skill => skill != 0)) return false;
        return s.EquippedSkills.All(skill => skill == 0 || skill switch { 1 => true, 2 => s.RiverBell, 3 => s.EbbSeal, 4 => s.AnchorWard || s.QuestStage >= 36, 5 => s.RefrainSong, 6 => s.BeaconLight, 7 => s.FinalEnding == 1, 8 => s.FinalEnding == 2, _ => false });
    }

    private float BellPulseRadius() => HasCharm(2) ? 190f : 155f;
    private float BellPulseCooldown() => (Math.Max(7f, 14f - skillTalent * 0.7f) + (HasCharm(2) ? 1.5f : 0f)) * WeaponCooldownMultiplier();
    private float WeaponSkillMultiplier() => HasWeaponAffix(weapon, 3) ? weaponRarities[weapon] == 2 ? 1.18f : 1.12f : 1f;
    private float TechniqueDamageMultiplier() => (1f + techniqueTalent * 0.08f) * (vigilActive && HasCharm(8) ? 1.18f : 1f);
    private int WeaponPower(int index) => weaponDamage[index] + weaponBonuses[index];
    private static string WeaponRarityName(int index) => index == 0 ? "COMUM" : index == 1 ? "RARA" : "RELÍQUIA";
    private bool HasWeaponAffix(int index, int affix) => weaponAffixes[index] == affix || weaponAffixes2[index] == affix;
    private string WeaponAffixName(int index) => string.Join(" + ", new[] { WeaponAffixLabel(weaponAffixes[index]), WeaponAffixLabel(weaponAffixes2[index]) }.Where(label => label.Length > 0));
    private static string WeaponAffixLabel(int affix) => affix switch { 1 => "Trégua", 2 => "Contracanto", 3 => "Maré", 4 => "Passo Célere", 5 => "Lastro", 6 => "Fio Esticado", _ => "" };
    private string WeaponDisplayName(int index) => weaponRarities[index] == 0 ? $"{weaponNames[index]} [COMUM]" : $"{weaponNames[index]} • {WeaponRarityName(index)} {WeaponAffixName(index)}";
    private string WeaponAffixDescription(int index) => string.Join(" • ", new[] { WeaponAffixDescription(weaponAffixes[index], weaponRarities[index]), WeaponAffixDescription(weaponAffixes2[index], weaponRarities[index]) }.Where(description => description.Length > 0));
    private static string WeaponAffixDescription(int affix, int rarity) => affix switch { 1 => $"da Trégua: recupera {(rarity == 2 ? 6 : 3)} vida por abate", 2 => $"do Contracanto: +{(rarity == 2 ? 11 : 7)} p.p. de crítico", 3 => $"da Maré: +{(rarity == 2 ? 18 : 12)}% de dano de técnica", 4 => $"do Passo Célere: recarga de técnicas -{(rarity == 2 ? 16 : 10)}%", 5 => $"do Lastro: dano recebido -{(rarity == 2 ? 12 : 8)}%", 6 => $"do Fio Esticado: alcance básico +{(rarity == 2 ? 20 : 12)}%", _ => "" };

    private void CastBellPulse()
    {
        if (!riverBell) { notice = "O SINO DE NARA AINDA NÃO FOI ENCONTRADO"; noticeTime = 1.6f; return; }
        if (room is 3 or 18 || enemies.Count == 0 || bellCd > 0) return;
        bellCd = BellPulseCooldown(); bellWaveTime = 0.42f; bellWaveRadius = BellPulseRadius();
        foreach (Enemy enemy in enemies.ToArray())
        {
            if (Distance(player, enemy.Pos) > bellWaveRadius) continue;
            float multiplier = (HasCharm(3) ? 1.25f : 1f) * WeaponSkillMultiplier() * TechniqueDamageMultiplier();
            float power = enemy.Boss ? 0.55f : 0.95f;
            DamageEnemy(enemy, (int)(damage * power * multiplier * OutgoingDamageMultiplier(enemy) * (IsArmored(enemy) ? 0.75f : 1)));
            enemy.Flash = 0.2f;
            if (!enemy.Boss) { enemy.Stagger = 0.95f; enemy.Windup = 0; enemy.Cooldown = Math.Max(enemy.Cooldown, 1.8f); }
            if (enemy.Hp <= 0) Defeat(enemy);
        }
        notice = HasCharm(2) ? "SINO DE NARA • ONDA AMPLIADA" : "SINO DE NARA • RESSONÂNCIA"; noticeTime = 1.2f;
    }

    private float EbbPullCooldown() => Math.Max(8f, 12f - skillTalent * 0.5f) * WeaponCooldownMultiplier();
    private void CastEbbPull()
    {
        if (!ebbSeal) { notice = "A VAZANTE AINDA NÃO FOI APRENDIDA"; noticeTime = 1.5f; return; }
        if (room is 3 or 18 || enemies.Count == 0 || pullCd > 0) return;
        pullCd = EbbPullCooldown(); pullWaveTime = 0.45f;
        float techniqueBoost = (HasCharm(3) ? 1.25f : 1f) * WeaponSkillMultiplier() * TechniqueDamageMultiplier();
        foreach (Enemy enemy in enemies.ToArray())
        {
            float distance = Distance(player, enemy.Pos); if (distance > 160) continue;
            DamageEnemy(enemy, damage * (enemy.Boss ? 0.3f : 0.48f) * techniqueBoost * OutgoingDamageMultiplier(enemy) * (IsArmored(enemy) ? 0.75f : 1f));
            enemy.Flash = 0.22f;
            if (enemy.Boss) enemy.Pos = MoveEnemy(enemy, Scale(Normalize(Sub(player, enemy.Pos), PointF.Empty), 22));
            else { enemy.Pos = MoveEnemy(enemy, Scale(Normalize(Sub(player, enemy.Pos), PointF.Empty), Math.Min(85, Math.Max(0, distance - 36)))); enemy.Windup = 0; enemy.Stagger = 0.55f; enemy.Cooldown = Math.Max(enemy.Cooldown, 1.2f); }
            if (enemy.Hp <= 0) Defeat(enemy);
        }
        notice = "VAZANTE • ECOS REUNIDOS"; noticeTime = 1.2f;
    }

    private float AnchorWardCooldown() => Math.Max(13f, 20f - skillTalent * 0.8f) * WeaponCooldownMultiplier();
    private void CastAnchorWard()
    {
        if (!anchorWard) { notice = "O MANTO DE LASTRO AINDA NÃO FOI APRENDIDO"; noticeTime = 1.5f; return; }
        if (room is 3 or 18 || enemies.Count == 0 || wardCd > 0) return;
        wardTime = 2.5f; wardCd = AnchorWardCooldown();
        notice = "MANTO DE LASTRO • PROTEÇÃO ATIVA"; noticeTime = 1.2f;
    }

    private float RefrainCooldown() => Math.Max(10f, 17f - skillTalent * 0.6f) * WeaponCooldownMultiplier();
    private void CastRefrain()
    {
        if (!refrainSong) { notice = "O CORO DE RETORNO AINDA NÃO FOI APRENDIDO"; noticeTime = 1.5f; return; }
        if (room is 3 or 18 || enemies.Count == 0 || refrainCd > 0) return;
        refrainCd = RefrainCooldown(); refrainWaveTime = 0.48f; refrainWaveRadius = 170;
        float boost = TechniqueDamageMultiplier() * WeaponSkillMultiplier();
        foreach (Enemy enemy in enemies.ToArray())
        {
            if (Distance(player, enemy.Pos) > refrainWaveRadius) continue;
            DamageEnemy(enemy, damage * (enemy.Boss ? 0.28f : 0.68f) * boost * OutgoingDamageMultiplier(enemy) * (IsArmored(enemy) ? 0.75f : 1f));
            enemy.Flash = 0.24f;
            if (!enemy.Boss) { enemy.Stagger = Math.Max(enemy.Stagger, 0.38f); enemy.Windup = 0; enemy.Cooldown = Math.Max(enemy.Cooldown, 0.8f); }
            if (enemy.Hp <= 0) Defeat(enemy);
        }
        notice = "CORO DE RETORNO • ECO REBATIDO"; noticeTime = 1.2f;
    }

    private float BeaconCooldown() => Math.Max(13f, 22f - skillTalent * 0.9f) * WeaponCooldownMultiplier();
    private float BeaconTotalCooldown() => BeaconCooldown() + (HasCharm(6) ? 3.5f : 0f);
    private void CastBeacon()
    {
        if (!beaconLight) { notice = "O FAROL DE VIGÍLIA AINDA NÃO FOI APRENDIDO"; noticeTime = 1.5f; return; }
        if (room is 3 or 18 || enemies.Count == 0 || beaconCd > 0) return;
        beaconPos = player; beaconTime = HasCharm(6) ? 8f : 6f; beaconPulseCd = 0.15f; beaconWaveTime = 0;
        beaconCd = BeaconTotalCooldown(); notice = HasCharm(6) ? "FAROL DE VIGÍLIA • PAVIO REFORÇADO" : "FAROL DE VIGÍLIA • LUZ PORTÁTIL ATIVA"; noticeTime = 1.6f;
    }
    private void CastReturnedVoices()
    {
        if (finalEnding != 1) { notice = "A DEVOLUÇÃO DAS VOZES AINDA NÃO FOI APRENDIDA"; noticeTime = 1.5f; return; }
        if (room is 3 or 18 || enemies.Count == 0 || returnVoiceCd > 0) return;
        returnVoiceCd = Math.Max(14f, 23f - skillTalent * 0.7f) * WeaponCooldownMultiplier(); returnVoiceWaveTime = 0.46f; returnVoiceWaveRadius = 188;
        float boost = TechniqueDamageMultiplier() * WeaponSkillMultiplier(); float restored = 0;
        foreach (Enemy enemy in enemies.ToArray())
        {
            if (Distance(player, enemy.Pos) > returnVoiceWaveRadius) continue;
            DamageEnemy(enemy, damage * (enemy.Boss ? 0.34f : 0.52f) * boost * OutgoingDamageMultiplier(enemy) * (IsArmored(enemy) ? 0.75f : 1f));
            enemy.Flash = 0.2f;
            if (!enemy.Boss) { enemy.Windup = 0; enemy.Stagger = Math.Max(enemy.Stagger, 0.32f); enemy.Cooldown = Math.Max(enemy.Cooldown, 0.8f); }
            restored += maxHealth * (enemy.Boss ? 0.025f : 0.045f);
            if (enemy.Hp <= 0) Defeat(enemy);
        }
        health = Math.Min(maxHealth, health + Math.Min(maxHealth * 0.24f, restored)); notice = "DEVOLUÇÃO DAS VOZES • MEMÓRIAS PARTILHADAS"; noticeTime = 1.4f;
    }
    private void CastFinalSpark()
    {
        if (finalEnding != 2) { notice = "A ÚLTIMA CENTELHA AINDA NÃO FOI APRENDIDA"; noticeTime = 1.5f; return; }
        if (room is 3 or 18 || enemies.Count == 0 || finalStrikeCd > 0) return;
        Enemy target = lockedTarget is not null && enemies.Contains(lockedTarget) ? lockedTarget : enemies.OrderBy(enemy => Distance(player, enemy.Pos)).First();
        facing = Normalize(Sub(target.Pos, player), facing); finalStrikeTarget = target.Pos; finalStrikeFlash = 0.24f;
        DamageEnemy(target, damage * (target.Boss ? 2.15f : 2.45f) * TechniqueDamageMultiplier() * WeaponSkillMultiplier() * OutgoingDamageMultiplier(target) * (IsArmored(target) ? 0.75f : 1f));
        target.Flash = 0.3f; if (!target.Boss) { target.Windup = 0; target.Stagger = Math.Max(target.Stagger, 0.72f); target.Cooldown = Math.Max(target.Cooldown, 1.25f); }
        if (target.Hp <= 0) Defeat(target);
        finalStrikeCd = Math.Max(12f, 19f - skillTalent * 0.65f) * WeaponCooldownMultiplier(); notice = "ÚLTIMA CENTELHA • IMPACTO CONCENTRADO"; noticeTime = 1.4f;
    }
    private void TickBeacon(float dt)
    {
        if (beaconTime <= 0) return;
        beaconTime = Math.Max(0, beaconTime - dt); beaconPulseCd -= dt; beaconWaveTime = Math.Max(0, beaconWaveTime - dt);
        if (beaconPulseCd > 0) return;
        beaconPulseCd = HasCharm(6) ? 0.85f : 1.1f; beaconWaveTime = 0.38f;
        foreach (Enemy enemy in enemies.ToArray())
        {
            if (Distance(beaconPos, enemy.Pos) > 128) continue;
            float multiplier = TechniqueDamageMultiplier() * WeaponSkillMultiplier();
            DamageEnemy(enemy, damage * (enemy.Boss ? 0.18f : 0.34f) * multiplier * OutgoingDamageMultiplier(enemy) * (IsArmored(enemy) ? 0.75f : 1f));
            enemy.Flash = 0.16f;
            if (enemy.Hp <= 0) Defeat(enemy);
        }
        if (beaconTime <= 0) beaconWaveTime = 0;
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
                    shard.Hit.Add(enemy); DamageEnemy(enemy, shard.Damage * OutgoingDamageMultiplier(enemy) * (IsArmored(enemy) ? 0.75f : 1)); enemy.Flash = 0.16f;
                    if (enemy.Hp <= 0) Defeat(enemy);
                }
            }
            if (blocked || shard.Life <= 0 || shard.Pos.X < 75 || shard.Pos.X > 885 || shard.Pos.Y < 110 || shard.Pos.Y > 550) echoShards.RemoveAt(i);
        }
    }

    private void Defeat(Enemy enemy)
    {
        if (!enemies.Remove(enemy)) return;
        if (ReferenceEquals(lockedTarget, enemy)) lockedTarget = enemies.OrderBy(candidate => Distance(player, candidate.Pos)).FirstOrDefault();
        enemiesDefeated++; int goldDrop = enemy.VigilMinion ? 8 + Math.Min(20, vigilFloor * 2) : vigilActive ? 35 + Math.Min(500, vigilFloor * 15) : enemy.Boss ? 35 : enemy.Kind == EnemyKind.Elite ? 20 : 8; drops.Add(new Drop { Pos = enemy.Pos, Kind = "gold", Amount = goldDrop });
        AddXp(enemy.VigilMinion ? 12 : enemy.Boss ? 75 : enemy.Kind == EnemyKind.Elite ? 40 : 22);
        if (HasWeaponAffix(weapon, 1)) health = Math.Min(maxHealth, health + (weaponRarities[weapon] == 2 ? 6 : 3));
        if (enemy.Boss && vigilActive) drops.Add(new Drop { Pos = Offset(enemy.Pos, 0, -28), Kind = "glass", Amount = 1 + Math.Min(5, (vigilFloor - 1) / 3) });
        if (enemy.Boss) drops.Add(CreateWeaponDrop(Offset(enemy.Pos, 0, 28), random.Next(1, 3), true));
        if (enemy.Boss && vigilActive && vigilFloor % 5 == 0 && (ownedCharms & 64) == 0) drops.Add(new Drop { Pos = Offset(enemy.Pos, 0, -48), Kind = "charm", Amount = 7 });
        if (enemy.Boss && vigilActive && vigilFloor % 10 == 0 && (ownedCharms & 128) == 0) drops.Add(new Drop { Pos = Offset(enemy.Pos, 0, -68), Kind = "charm", Amount = 8 });
        if (enemy.Boss && vigilActive && HasCharm(7)) health = Math.Min(maxHealth, health + maxHealth * 0.12f);
        else if (enemy.Kind == EnemyKind.Elite && EliteWeaponDropRoll(random.Next(100))) drops.Add(CreateWeaponDrop(Offset(enemy.Pos, -24, 28), random.Next(1, 3)));
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
        if (enemy.Kind == EnemyKind.RiftCantor && !riftVerse) drops.Add(new Drop { Pos = Offset(enemy.Pos, -24, 0), Kind = "verse" });
        if (enemy.Kind == EnemyKind.GlassHerald && !deepTrace) { drops.Add(new Drop { Pos = Offset(enemy.Pos, -24, 0), Kind = "deep" }); drops.Add(new Drop { Pos = Offset(enemy.Pos, 24, 0), Kind = "glass", Amount = 5 }); }
        if (enemy.Kind == EnemyKind.LanternRegent && !lanternSignal) { drops.Add(new Drop { Pos = Offset(enemy.Pos, -24, 0), Kind = "signal" }); drops.Add(new Drop { Pos = Offset(enemy.Pos, 24, 0), Kind = "glass", Amount = 6 }); }
        if (enemy.Kind == EnemyKind.LanternRegent && (ownedCharms & 32) == 0) drops.Add(new Drop { Pos = Offset(enemy.Pos, 0, -26), Kind = "charm", Amount = 6 });
        if (enemy.Kind == EnemyKind.RouteScribe && !routeMemory) drops.Add(new Drop { Pos = Offset(enemy.Pos, -24, 0), Kind = "route" });
        if (enemy.Kind == EnemyKind.DeepSilence && !silenceMemory) drops.Add(new Drop { Pos = Offset(enemy.Pos, -24, 0), Kind = "silence" });
        if (enemy.Kind == EnemyKind.BorrowedChorus && !borrowedTrace) drops.Add(new Drop { Pos = Offset(enemy.Pos, -24, 0), Kind = "borrowed" });
        if (enemy.Kind == EnemyKind.SaltEcho && !enemy.VigilMinion && random.NextDouble() < 0.2) drops.Add(new Drop { Pos = Offset(enemy.Pos, 18, 14), Kind = "glass", Amount = 1 });
        if (enemy.Kind == EnemyKind.FirstKeeper && (ownedCharms & 8) == 0) drops.Add(new Drop { Pos = Offset(enemy.Pos, 24, -12), Kind = "charm", Amount = 4 });
        if (enemy.Kind == EnemyKind.OathCartographer && (ownedCharms & 16) == 0) drops.Add(new Drop { Pos = Offset(enemy.Pos, 24, -12), Kind = "charm", Amount = 5 });
        if (enemy.Kind == EnemyKind.Elite && (ownedCharms & 1) == 0) drops.Add(new Drop { Pos = Offset(enemy.Pos, 24, -12), Kind = "charm", Amount = 1 });
        if (enemy.Kind == EnemyKind.RiverFerryman && (ownedCharms & 2) == 0) drops.Add(new Drop { Pos = Offset(enemy.Pos, -24, -12), Kind = "charm", Amount = 2 });
        if (enemy.Kind == EnemyKind.BlindScribe && (ownedCharms & 4) == 0) drops.Add(new Drop { Pos = Offset(enemy.Pos, -24, -12), Kind = "charm", Amount = 3 });
        if (!enemy.Boss && enemy.Kind != EnemyKind.Elite && !enemy.VigilMinion && random.NextDouble() < 0.06)
            drops.Add(new Drop { Pos = Offset(enemy.Pos, 18, 14), Kind = "charm", Amount = random.Next(1, 4) });
        if (enemy.Kind == EnemyKind.Elite && !armor) drops.Add(new Drop { Pos = Offset(enemy.Pos, 24, 0), Kind = "armor" });
        else if (!enemy.Boss && random.NextDouble() < 0.2) drops.Add(new Drop { Pos = Offset(enemy.Pos, 16, 12), Kind = "potion" });
        if (relicOn)
        {
            float healthBeforeRelic = health;
            health = Math.Min(maxHealth, health + RelicKillHealing());
            int recovered = (int)Math.Round(health - healthBeforeRelic);
            if (recovered > 0)
                AddFloatingText(new PointF(player.X, player.Y - 30), armorOn ? $"VIGÍLIA +{recovered} VIDA" : $"RELICÁRIO +{recovered} VIDA", Color.FromArgb(145, 225, 204));
        }
        if (enemies.Count == 0 && room < 2) { int next = room == 0 ? 2 : 1; if ((ownedWeapons & (1 << next)) == 0) drops.Add(CreateWeaponDrop(Offset(enemy.Pos, -16, -8), next)); }
        if (enemies.Count == 0 && drops.Count == 0 && room == 2 && !memoryOverlay) chestChoice = true;
    }

    private void AddXp(int amount)
    {
        xp += amount;
        while (xp >= level * 60) { xp -= level * 60; level++; damage += 5; pendingTalents++; }
        if (pendingTalents > 0) talentChoice = true;
    }

    private void PauseForLostFocus() { focusPaused = true; down.Clear(); dashTime = 0; Invalidate(); }
    private void ResumeAfterFocus() { focusPaused = false; Invalidate(); }
    private bool GameplayPaused() => focusPaused || title || dead || victory || storyOverlay || memoryOverlay || mapOpen || inventoryOpen || skillMenuOpen || characterSheetOpen || forgeMenuOpen || talentChoice || chestChoice || endingChoiceOpen || vigilChoiceOpen;

    private void TickGame()
    {
        float dt = timer.Interval / 1000f; noticeTime = Math.Max(0, noticeTime - dt);
        GameAudio.SetAmbientActive(!GameplayPaused());
        if (GameplayPaused()) { Invalidate(); return; }
        attackCd = Math.Max(0, attackCd - dt); attackFlash = Math.Max(0, attackFlash - dt); skillCd = Math.Max(0, skillCd - dt); shardCd = Math.Max(0, shardCd - dt); bellCd = Math.Max(0, bellCd - dt); bellWaveTime = Math.Max(0, bellWaveTime - dt); pullCd = Math.Max(0, pullCd - dt); pullWaveTime = Math.Max(0, pullWaveTime - dt); wardCd = Math.Max(0, wardCd - dt); wardTime = Math.Max(0, wardTime - dt); refrainCd = Math.Max(0, refrainCd - dt); refrainWaveTime = Math.Max(0, refrainWaveTime - dt); beaconCd = Math.Max(0, beaconCd - dt); returnVoiceCd = Math.Max(0, returnVoiceCd - dt); returnVoiceWaveTime = Math.Max(0, returnVoiceWaveTime - dt); finalStrikeCd = Math.Max(0, finalStrikeCd - dt); finalStrikeFlash = Math.Max(0, finalStrikeFlash - dt); dashCd = Math.Max(0, dashCd - dt); dashTime = Math.Max(0, dashTime - dt); invulnerable = Math.Max(0, invulnerable - dt); windupPulse += dt; TickFloatingTexts(dt);
        PointF dir = Normalize(MovementInput(), PointF.Empty); if (dashTime > 0) dir = dashDirection;
        MovePlayer(Scale(dir, (dashTime > 0 ? 530 : 190) * dt));
        PointF drift = CurrentDrift();
        if (dashTime <= 0) MovePlayer(Scale(drift, dt));
        PointF animationDirection = Length(dir) > 0.001f ? dir : drift;
        if (Length(animationDirection) > 0.001f) playerAnimationTime += dt;
        else playerAnimationTime = 0;
        for (int i = drops.Count - 1; i >= 0; i--) if (Distance(player, drops[i].Pos) < 30) { GameAudio.Play(GameAudio.Cue.Loot); Collect(drops[i]); }
        TickBeacon(dt); int roomBeingUpdated = room; foreach (Enemy enemy in enemies.ToArray()) { PointF previousPos = enemy.Pos; UpdateEnemy(enemy, dt); enemy.Moving = Distance(previousPos, enemy.Pos) > 0.01f; if (enemy.Moving) enemy.AnimationTime += dt; if (dead || room != roomBeingUpdated) break; }
        TickVigilMinions(dt);
        AdvanceVoiceShards(dt);
        if (enemies.Count == 0 && drops.Count == 0 && room == 2 && !memoryOverlay) chestChoice = true;
        MarkRoomCleared();
        Invalidate();
    }

    private static float RangedImpactRadius(EnemyKind kind) => kind switch
    {
        EnemyKind.Archer => 26,
        EnemyKind.InkEcho => 30,
        EnemyKind.DrownedMemory or EnemyKind.EmberWisp or EnemyKind.NameMoth => 28,
        EnemyKind.MudCantor or EnemyKind.PaperWraith or EnemyKind.SaltEcho => 36,
        _ => 0
    };

    private static float NonBossAttackRadius(Enemy e)
    {
        if (e.Kind == EnemyKind.Elite) return e.EliteAffix == EliteAffix.Spearbound ? 36 : 62;
        if (e.Kind == EnemyKind.Mage) return 52;
        float ranged = RangedImpactRadius(e.Kind);
        return ranged > 0 ? ranged : 38;
    }
    private static PointF NonBossAttackCenter(Enemy e) => e.Kind is EnemyKind.Elite or EnemyKind.Mage || RangedImpactRadius(e.Kind) > 0 ? e.Target : e.Pos;
    private static bool NonBossAttackContains(Enemy e, PointF point) => Distance(point, NonBossAttackCenter(e)) < NonBossAttackRadius(e);

    private static bool BossUsesCone(Enemy e) => e.Kind == EnemyKind.GlassHerald && e.BossPattern == 2;
    private static bool BossUsesRing(Enemy e) => e.Kind == EnemyKind.RouteScribe && e.BossPattern == 0;
    private static float BossRingInnerRadius(Enemy e) => e.Kind == EnemyKind.RouteScribe ? 68 : 0;
    private static bool BossUsesLine(Enemy e) => e.BossPattern == 2 && e.Kind is EnemyKind.FirstKeeper or EnemyKind.NameWeaver or EnemyKind.OathCartographer or EnemyKind.NameBroker or EnemyKind.TideWitness or EnemyKind.ReedKeeper or EnemyKind.FlowKeeper or EnemyKind.CounterCurrent or EnemyKind.RiftCantor or EnemyKind.LanternRegent or EnemyKind.RouteScribe or EnemyKind.DeepSilence or EnemyKind.BorrowedChorus;
    private static bool BossConeContains(Enemy e, PointF point)
    {
        PointF axis = Normalize(Sub(e.Target, e.Pos), new PointF(1, 0)), direction = Sub(point, e.Pos); float distance = Length(direction);
        return distance < 245 && distance > 0.01f && Dot(axis, Scale(direction, 1 / distance)) >= MathF.Cos(34 * MathF.PI / 180);
    }
    private static float BossCircleRadius(Enemy e) => e.BossPattern == 0 ? e.Kind switch { EnemyKind.GlassHerald => 135, EnemyKind.LanternRegent => 150, EnemyKind.RouteScribe => 160, _ => 90 } : e.BossPattern == 2 ? e.Kind switch { EnemyKind.RiverFerryman => 85, EnemyKind.AshWarden => 68, EnemyKind.HollowName => 85, _ => 42 } : 42;
    private static bool BossAttackContains(Enemy e, PointF point)
    {
        if (BossUsesCone(e)) return BossConeContains(e, point);
        if (BossUsesRing(e)) { float distance = Distance(point, e.Pos); return distance > BossRingInnerRadius(e) && distance < BossCircleRadius(e); }
        if (!BossUsesLine(e)) return Distance(point, e.BossPattern == 0 ? e.Pos : e.Target) < BossCircleRadius(e);
        float width = e.Kind == EnemyKind.FirstKeeper ? 25 : 24;
        if (DistanceToSegment(point, e.Pos, e.Target) < width) return true;
        return e.Kind is EnemyKind.FirstKeeper or EnemyKind.DeepSilence or EnemyKind.BorrowedChorus && DistanceToSegment(point, e.Target2Start, e.Target2) < width;
    }

    private static (Color Fill, Color Edge) AttackWarningPalette(bool highContrast) => highContrast
        ? (Color.FromArgb(78, 255, 217, 36), Color.FromArgb(255, 255, 244, 122))
        : (Color.FromArgb(45, 235, 115, 75), Color.FromArgb(235, 235, 115, 75));

    private void DrawBossWarning(Graphics g, Enemy e)
    {
        (Color fillColor, Color edgeColor) = AttackWarningPalette(highContrastTelegraphs);
        using var outline = new Pen(edgeColor, highContrastTelegraphs ? 4 : 3);
        using var fill = new SolidBrush(fillColor);
        if (BossUsesCone(e))
        {
            float radius = 245, angle = MathF.Atan2(e.Target.Y - e.Pos.Y, e.Target.X - e.Pos.X) * 180 / MathF.PI, sweep = 68;
            var cone = new RectangleF(e.Pos.X - radius, e.Pos.Y - radius, radius * 2, radius * 2);
            if (highContrastTelegraphs) { using var shadow = new Pen(Color.FromArgb(245, 12, 13, 15), 9); g.DrawPie(shadow, cone, angle - sweep / 2, sweep); }
            g.FillPie(fill, cone, angle - sweep / 2, sweep); g.DrawPie(outline, cone, angle - sweep / 2, sweep);
            return;
        }
        if (BossUsesRing(e))
        {
            float radius = BossCircleRadius(e), innerRadius = BossRingInnerRadius(e);
            var outer = new RectangleF(e.Pos.X - radius, e.Pos.Y - radius, radius * 2, radius * 2);
            var inner = new RectangleF(e.Pos.X - innerRadius, e.Pos.Y - innerRadius, innerRadius * 2, innerRadius * 2);
            using var ring = new GraphicsPath(FillMode.Alternate); ring.AddEllipse(outer); ring.AddEllipse(inner);
            if (highContrastTelegraphs) { using var shadow = new Pen(Color.FromArgb(245, 12, 13, 15), 9); g.DrawEllipse(shadow, outer); g.DrawEllipse(shadow, inner); }
            g.FillPath(fill, ring); g.DrawEllipse(outline, outer); g.DrawEllipse(outline, inner);
            return;
        }
        if (!BossUsesLine(e))
        {
            float radius = BossCircleRadius(e); PointF center = e.BossPattern == 0 ? e.Pos : e.Target;
            g.FillEllipse(fill, center.X - radius, center.Y - radius, radius * 2, radius * 2);
            if (highContrastTelegraphs) { using var shadow = new Pen(Color.FromArgb(245, 12, 13, 15), 8); g.DrawEllipse(shadow, center.X - radius, center.Y - radius, radius * 2, radius * 2); }
            g.DrawEllipse(outline, center.X - radius, center.Y - radius, radius * 2, radius * 2);
            return;
        }
        float width = e.Kind == EnemyKind.FirstKeeper ? 25 : 24;
        using var area = new Pen(fillColor, width * 2) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        if (highContrastTelegraphs) { using var shadow = new Pen(Color.FromArgb(240, 12, 13, 15), width * 2 + 8) { StartCap = LineCap.Round, EndCap = LineCap.Round }; g.DrawLine(shadow, e.Pos, e.Target); if (e.Kind is EnemyKind.FirstKeeper or EnemyKind.DeepSilence or EnemyKind.BorrowedChorus) g.DrawLine(shadow, e.Target2Start, e.Target2); }
        g.DrawLine(area, e.Pos, e.Target); g.DrawLine(outline, e.Pos, e.Target);
        if (e.Kind is EnemyKind.FirstKeeper or EnemyKind.DeepSilence or EnemyKind.BorrowedChorus) { g.DrawLine(area, e.Target2Start, e.Target2); g.DrawLine(outline, e.Target2Start, e.Target2); }
    }

    private void UpdateEnemy(Enemy e, float dt)
    {
        float attackTempo = vigilActive && e.Boss && VigilAspectForFloor(vigilFloor) == VigilAspect.Swift ? 1.25f : 1;
        e.Cooldown -= dt * attackTempo; e.Flash = Math.Max(0, e.Flash - dt); PointF offset = Sub(player, e.Pos); float dist = Length(offset);
        if (e.Stagger > 0) { e.Stagger = Math.Max(0, e.Stagger - dt); return; }
        if (e.Windup > 0) {
            e.Windup -= dt * attackTempo;
            bool inAttack = e.Boss ? BossAttackContains(e, player) : NonBossAttackContains(e, player);
            if (e.Windup <= 0 && inAttack && invulnerable <= 0 && dashTime <= 0)
            {
                HitPlayer(vigilActive ? 112 : e.Boss ? e.Kind == EnemyKind.BlindScribe ? 23 : e.Kind == EnemyKind.RiverFerryman ? 28 : e.Kind == EnemyKind.AshWarden ? 31 : e.Kind == EnemyKind.HollowName ? 36 : e.Kind == EnemyKind.NameWeaver ? 39 : e.Kind == EnemyKind.FirstKeeper ? 44 : e.Kind == EnemyKind.OathCartographer ? 42 : e.Kind == EnemyKind.NameBroker ? 48 : e.Kind == EnemyKind.CounterCurrent ? 68 : e.Kind == EnemyKind.RiftCantor ? 74 : e.Kind == EnemyKind.GlassHerald ? 82 : e.Kind == EnemyKind.LanternRegent ? 88 : e.Kind == EnemyKind.RouteScribe ? 96 : e.Kind == EnemyKind.DeepSilence ? 112 : e.Kind == EnemyKind.BorrowedChorus ? 118 : e.Kind == EnemyKind.FlowKeeper ? 62 : e.Kind == EnemyKind.ReedKeeper ? 56 : e.Kind == EnemyKind.TideWitness ? 52 : 26 : e.Kind == EnemyKind.Elite ? e.EliteAffix == EliteAffix.Bloodbound ? 24 : e.EliteAffix == EliteAffix.Spearbound ? 18 : 20 : e.Kind == EnemyKind.Mage ? 16 : e.Kind is EnemyKind.InkEcho or EnemyKind.DrownedMemory or EnemyKind.EmberWisp or EnemyKind.NameMoth or EnemyKind.MudCantor or EnemyKind.PaperWraith or EnemyKind.SaltEcho ? 18 : 10);
                if (e.Kind == EnemyKind.Elite && e.EliteAffix == EliteAffix.Bloodbound) e.Hp = Math.Min(e.MaxHp, e.Hp + 8);
            }
            return;
        }
        if (e.Kind is EnemyKind.Archer or EnemyKind.InkEcho or EnemyKind.SaltEcho) { float preferred = e.Kind == EnemyKind.InkEcho ? 135 : e.Kind == EnemyKind.SaltEcho ? 155 : 170; if (dist < preferred) e.Pos = MoveEnemy(e, Scale(Normalize(Sub(e.Pos, player), PointF.Empty), (e.Kind == EnemyKind.InkEcho ? 68 : e.Kind == EnemyKind.SaltEcho ? 62 : 55) * dt)); else if (e.Cooldown <= 0) { BeginWindup(e, player, e.Kind == EnemyKind.InkEcho ? 0.75f : e.Kind == EnemyKind.SaltEcho ? 0.68f : 0.6f); e.Cooldown = e.Kind == EnemyKind.InkEcho ? 2.0f : e.Kind == EnemyKind.SaltEcho ? 1.85f : 1.8f; } SeparateFromPack(e, dt); return; }
        if (e.Kind is EnemyKind.DrownedMemory or EnemyKind.EmberWisp or EnemyKind.NameMoth or EnemyKind.MudCantor or EnemyKind.PaperWraith) { float preferred = e.Kind == EnemyKind.EmberWisp ? 145 : e.Kind == EnemyKind.NameMoth ? 175 : e.Kind == EnemyKind.MudCantor ? 215 : e.Kind == EnemyKind.PaperWraith ? 185 : 110; if (dist < preferred) e.Pos = MoveEnemy(e, Scale(Normalize(Sub(e.Pos, player), PointF.Empty), (e.Kind is EnemyKind.MudCantor or EnemyKind.PaperWraith ? 52 : 72) * dt)); else if (e.Cooldown <= 0) { BeginWindup(e, player, e.Kind == EnemyKind.EmberWisp ? 0.7f : e.Kind == EnemyKind.NameMoth ? 0.62f : e.Kind == EnemyKind.MudCantor ? 0.82f : e.Kind == EnemyKind.PaperWraith ? 0.68f : 0.9f); e.Cooldown = e.Kind == EnemyKind.EmberWisp ? 1.9f : e.Kind == EnemyKind.NameMoth ? 1.7f : e.Kind == EnemyKind.MudCantor ? 2.15f : e.Kind == EnemyKind.PaperWraith ? 1.95f : 2.25f; } SeparateFromPack(e, dt); return; }
        if (e.Kind == EnemyKind.Mage && dist < 165) e.Pos = MoveEnemy(e, Scale(Normalize(Sub(e.Pos, player), PointF.Empty), 42 * dt));
        if (e.Kind == EnemyKind.Elite && e.EliteAffix == EliteAffix.Spearbound)
        {
            if (dist < 150) e.Pos = MoveEnemy(e, Scale(Normalize(Sub(e.Pos, player), PointF.Empty), 66 * dt));
            else if (dist < 380 && e.Cooldown <= 0) { BeginWindup(e, player, 0.82f); e.Cooldown = 2.45f; }
            else if (dist > 210) e.Pos = MoveEnemy(e, Scale(Normalize(offset, PointF.Empty), 48 * dt));
            SeparateFromPack(e, dt); return;
        }
        if (e.Kind == EnemyKind.Elite && e.Cooldown <= 0 && dist < 220) { bool swift = e.EliteAffix == EliteAffix.Swift; BeginWindup(e, player, swift ? 0.38f : e.EliteAffix == EliteAffix.Armored ? 0.72f : 0.58f); e.Cooldown = swift ? 1.55f : e.EliteAffix == EliteAffix.Armored ? 2.5f : 2.1f; return; }
        if (e.Boss && e.Cooldown <= 0 && (dist < 135 || (e.Kind is EnemyKind.BlindScribe or EnemyKind.RiverFerryman or EnemyKind.AshWarden or EnemyKind.HollowName or EnemyKind.NameWeaver or EnemyKind.FirstKeeper or EnemyKind.OathCartographer or EnemyKind.NameBroker or EnemyKind.TideWitness or EnemyKind.ReedKeeper or EnemyKind.FlowKeeper or EnemyKind.CounterCurrent or EnemyKind.RiftCantor or EnemyKind.GlassHerald or EnemyKind.LanternRegent or EnemyKind.RouteScribe or EnemyKind.DeepSilence or EnemyKind.BorrowedChorus))) { e.BossPattern = (e.Kind is EnemyKind.RiverFerryman or EnemyKind.AshWarden or EnemyKind.HollowName or EnemyKind.NameWeaver or EnemyKind.FirstKeeper or EnemyKind.OathCartographer or EnemyKind.NameBroker or EnemyKind.TideWitness or EnemyKind.ReedKeeper or EnemyKind.FlowKeeper or EnemyKind.CounterCurrent or EnemyKind.RiftCantor or EnemyKind.GlassHerald or EnemyKind.LanternRegent or EnemyKind.RouteScribe or EnemyKind.DeepSilence or EnemyKind.BorrowedChorus) ? (e.BossPattern + 1) % 3 : 1 - e.BossPattern; bool aimed = (e.Kind == EnemyKind.BlindScribe && e.BossPattern == 1) || ((e.Kind is EnemyKind.RiverFerryman or EnemyKind.AshWarden) && (e.BossPattern is 1 or 2)) || (e.Kind is EnemyKind.HollowName or EnemyKind.NameWeaver or EnemyKind.FirstKeeper or EnemyKind.OathCartographer or EnemyKind.NameBroker or EnemyKind.TideWitness or EnemyKind.ReedKeeper or EnemyKind.FlowKeeper or EnemyKind.CounterCurrent or EnemyKind.RiftCantor or EnemyKind.GlassHerald or EnemyKind.LanternRegent or EnemyKind.RouteScribe or EnemyKind.DeepSilence or EnemyKind.BorrowedChorus) && e.BossPattern is 1 or 2; float windup = e.Hp < e.MaxHp * 0.5f ? 0.52f : 0.78f; if (vigilActive && VigilAspectForFloor(vigilFloor) == VigilAspect.Swift) windup *= 0.8f; if (e.Kind == EnemyKind.FirstKeeper && e.BossPattern == 2) { PointF axis = Normalize(Sub(player, e.Pos), new PointF(1, 0)); PointF side = new(-axis.Y, axis.X); e.Target = Add(e.Pos, Scale(axis, 920)); e.Target2Start = Add(player, Scale(side, -460)); e.Target2 = Add(player, Scale(side, 460)); e.Windup = e.WindupMax = windup; GameAudio.Play(GameAudio.Cue.BossWarning); } else BeginWindup(e, aimed ? player : e.Pos, windup); e.Cooldown = e.Hp < e.MaxHp * 0.5f ? 1.25f : 2.0f; return; }
        if (e.Kind == EnemyKind.Mage && e.Cooldown <= 0) { BeginWindup(e, player, 0.8f); e.Cooldown = 2.2f; SeparateFromPack(e, dt); return; }
        if (dist > (e.Boss ? 40 : 30)) e.Pos = MoveEnemy(e, Scale(Normalize(offset, PointF.Empty), (e.Boss ? 55 : (74 + room * 7) * (e.Kind == EnemyKind.Elite && e.EliteAffix == EliteAffix.Swift ? 1.35f : 1)) * dt));
        else if (e.Cooldown <= 0) { BeginWindup(e, player, 0.45f); e.Cooldown = 1.35f; }
        if (!e.Boss) SeparateFromPack(e, dt);
    }

    private static void BeginWindup(Enemy enemy, PointF target, float duration)
    {
        enemy.Target = target; enemy.Windup = enemy.WindupMax = duration;
        if (enemy.Boss) GameAudio.Play(GameAudio.Cue.BossWarning);
        if (enemy.Kind == EnemyKind.DeepSilence && enemy.BossPattern == 2)
        {
            PointF axis = Normalize(Sub(target, enemy.Pos), new PointF(1, 0)); PointF side = new(-axis.Y, axis.X);
            enemy.Target2Start = Add(enemy.Pos, Scale(side, 74)); enemy.Target2 = Add(target, Scale(side, 74));
        }
    }
    private void DamageEnemy(Enemy enemy, float amount, bool critical = false)
    {
        if (vigilActive && enemy.Boss && VigilAspectForFloor(vigilFloor) == VigilAspect.Hardened) amount *= 0.75f;
        enemy.Hp -= amount;
        if (amount <= 0) return;
        string value = Math.Max(1, (int)Math.Round(amount)).ToString();
        AddFloatingText(new PointF(enemy.Pos.X, enemy.Pos.Y - enemy.Radius - 10), critical ? $"CRÍTICO {value}" : value, critical ? Color.FromArgb(255, 211, 123) : Color.AntiqueWhite);
    }

    private void HitPlayer(float value)
    {
        GameAudio.Play(GameAudio.Cue.Impact);
        float vigilPressure = vigilActive ? 1 + Math.Min(1.5f, Math.Max(0, vigilFloor - 1) * 0.08f) : 1;
        float aspectPressure = vigilActive && VigilAspectForFloor(vigilFloor) == VigilAspect.Hungry ? 1.3f : 1;
        if (vigilActive && HasCharm(7)) aspectPressure *= 1.1f;
        float damageTaken = value * vigilPressure * aspectPressure * ArmorDamageTakenMultiplier() * (wardTime > 0 ? 0.45f : 1) * WeaponDamageTakenMultiplier();
        health = Math.Max(0, health - damageTaken); invulnerable = 0.6f;
        AddFloatingText(new PointF(player.X, player.Y - 30), $"−{Math.Max(1, (int)Math.Round(damageTaken))}", Color.FromArgb(250, 116, 105));
        if (health <= 0)
        {
            if (vigilActive)
            {
                int failedFloor = vigilFloor; vigilActive = false; vigilChoiceOpen = false; vigilFloor = 0; room = 18; SetupRoom(); health = Math.Max(1, maxHealth * 0.5f);
                ShowMemory("O ECO SE DESFAZ", $"Aven recolhe as lembranças antes que a Vigília possa prendê-las. Você retorna ao Refúgio com metade da vida. Melhor andar registrado: {vigilBestFloor}; a tentativa terminou no andar {failedFloor}.");
            }
            else dead = true;
        }
    }

    private void AddFloatingText(PointF pos, string text, Color color)
    {
        if (floatingTexts.Count >= 48) floatingTexts.RemoveAt(0);
        float spread = floatingTexts.Count % 5 * 12 - 24;
        floatingTexts.Add(new FloatingText { Pos = new PointF(pos.X + spread, pos.Y), Text = text, Color = color, TimeLeft = 0.85f, Duration = 0.85f });
    }

    private void TickFloatingTexts(float dt)
    {
        for (int i = floatingTexts.Count - 1; i >= 0; i--)
        {
            FloatingText text = floatingTexts[i]; text.TimeLeft -= dt; text.Pos = new PointF(text.Pos.X, text.Pos.Y - 28 * dt);
            if (text.TimeLeft <= 0) floatingTexts.RemoveAt(i);
        }
    }
    private void SalvageNearbyWeapon()
    {
        Drop? item = drops.Where(drop => drop.Kind == "weapon" && Distance(player, drop.Pos) <= 75).OrderBy(drop => Distance(player, drop.Pos)).FirstOrDefault();
        if (item is null) { notice = "NENHUMA ARMA POR PERTO PARA DESMONTAR"; noticeTime = 1.5f; return; }
        (int goldValue, int glassValue) = WeaponSalvageReward(item.Rarity);
        gold += goldValue;
        int receivedGlass = Math.Min(glassValue, 999 - tideGlass);
        tideGlass += receivedGlass;
        drops.Remove(item);
        notice = receivedGlass > 0 ? $"ARMA DESMONTADA: +{goldValue} OURO • +{receivedGlass} SAL" : $"ARMA DESMONTADA: +{goldValue} OURO";
        noticeTime = 2.2f;
    }

    private static (int Gold, int Glass) WeaponSalvageReward(int rarity) => rarity switch { 2 => (75, 2), 1 => (40, 1), _ => (15, 0) };

    private void Collect(Drop item)
    {
        if (item.Kind == "charm")
        {
            if (item.Amount is < 1 or > 8) { drops.Remove(item); return; }
            bool newCharm = (ownedCharms & (1 << (item.Amount - 1))) == 0; ownedCharms |= 1 << (item.Amount - 1);
            if (equippedCharm == 0 && equippedCharm2 != item.Amount) equippedCharm = item.Amount;
            else if (equippedCharm2 == 0 && equippedCharm != item.Amount) equippedCharm2 = item.Amount;
            notice = item.Amount is 7 or 8 ? $"TALISMÃ DE PÓS-JOGO: {charmNames[item.Amount]}" : newCharm ? $"TALISMÃ: {charmNames[item.Amount]}" : $"ECO DUPLICADO: {charmNames[item.Amount]}";
            drops.Remove(item); noticeTime = 1.7f; return;
        }
        if (item.Kind == "silence") { silenceMemory = true; questStage = 51; notice = "MEMÓRIA: AS VOZES DEVOLVIDAS"; ShowMemory("AS VOZES DEVOLVIDAS", "O Silêncio não era uma criatura que veio de fora. Era o nome dado ao peso de milhares de lembranças mantidas juntas pelo Lume. Quando a câmara se abre, as vozes não exigem que você as devolva nem que continue prendendo-as: cada uma pode escolher se quer falar, descansar ou partir. Entre elas não há resposta sobre Nara. O que resta é decidir como Véspera cuidará do Lume daqui em diante."); drops.Remove(item); return; }
        if (item.Kind == "borrowed") { borrowedTrace = true; questStage = 55; notice = "MEMÓRIA: UM ECO DEIXADO POR ESCOLHA"; ShowMemory("O ARQUIVO PERMANECE ABERTO", "O arquivista guardava os ecos para impedir que se perdessem. Quando cede, cada som volta a pertencer a quem o deixou. A gravação não traz nomes, vozes identificáveis ou destinos: apenas a decisão de compartilhar um instante."); drops.Remove(item); return; }
        if (item.Kind == "deep") { deepTrace = true; questStage = 42; notice = "MEMÓRIA: A PEDRA QUE NÃO GUARDA NOMES"; ShowMemory("A PEDRA QUE NÃO GUARDA NOMES", "A fonte não ecoa nenhuma voz antiga. Ela repete o som de passos que chegaram até aqui e escolheram partir sem deixar registro. O Guardião tentava reunir os sons numa única história; a pedra se abre quando cada passo volta a ter seu próprio ritmo. A lembrança não identifica quem passou nem para onde foi."); drops.Remove(item); return; }
        if (item.Kind == "signal") { lanternSignal = true; questStage = 45; notice = "SINAL: A LUZ QUE ESPERA"; ShowMemory("A LUZ QUE ESPERA", "O Regente mantinha as lanternas voltadas para dentro, prendendo cada viajante ao reflexo de quem já foi. Quando a luz se abre, elas não revelam rostos nem caminhos: apenas deixam uma chama acesa para quem decidir voltar. A fonte guarda a promessa, não a identidade nem o destino de quem passou."); drops.Remove(item); return; }
        if (item.Kind == "route") { routeMemory = true; questStage = 48; notice = "MEMÓRIA: AS ROTAS QUE PERMANECEM ABERTAS"; ShowMemory("AS ROTAS QUE PERMANECEM ABERTAS", "O Escriba traçava cada caminho de volta até uma única saída. Sob as linhas apagadas, a pedra guarda marcas de pessoas que escolheram retornar, seguir adiante ou permanecer onde estavam. Os traços pertencem a muitos viajantes; nenhum carrega o nome de Nara nem aponta para o lugar onde ela possa estar."); drops.Remove(item); return; }
        switch (item.Kind) { case "gold": gold += item.Amount; notice = $"+{item.Amount} OURO"; break; case "potion": potions++; notice = "+1 POÇÃO"; break; case "glass": tideGlass = Math.Min(999, tideGlass + item.Amount); notice = $"+{item.Amount} SAL VÍTREO"; break; case "weapon": if (item.Amount is < 1 or > 2) { drops.Remove(item); return; } int oldPower = WeaponPower(item.Amount); weaponRarities[item.Amount] = item.Rarity; weaponBonuses[item.Amount] = item.Bonus; weaponAffixes[item.Amount] = item.Affix; weaponAffixes2[item.Amount] = item.Affix2; ownedWeapons |= 1 << item.Amount; if (weapon == item.Amount) damage += WeaponPower(item.Amount) - oldPower; notice = $"ARMA {WeaponRarityName(weaponRarities[item.Amount])}: {WeaponDisplayName(item.Amount)}"; break; case "ebb": questStage = 32; notice = "MEMÓRIA: O VEIO LIVRE"; ShowMemory("A CORRENTE NÃO É UMA ROTA", "O vigia mantinha o fluxo preso num único giro. Quando o canal se abre, a água volta a mudar de direção conforme a cheia. Aven poderá ensinar o Escutador a chamar a vazante sem obrigar ninguém a segui-la. A gravação termina antes de indicar para onde foram os viajantes."); break; case "counter": counterTrace = true; questStage = 35; notice = "MEMÓRIA: O COMPASSO DAS MARGENS"; ShowMemory("O AFINADOR DAS MARGENS", "O Afinador não guardava um mapa. Regulava a força que mantém os dois lados do rio em equilíbrio; alguém o convenceu de que só um fluxo constante poderia proteger a comunidade. Ao ceder, o mecanismo volta a alternar entre impulso e retorno. A gravação não contém nome, rota nem notícia de Nara."); break; case "verse": riftVerse = true; questStage = 39; notice = "MEMÓRIA: A NOTA QUE NÃO MANDA"; ShowMemory("A NOTA QUE NÃO MANDA", "O eco mostra vozes diferentes sustentando a mesma ponte sonora sem se fundirem. Uma contravoz tenta fixá-las numa nota só; quando cede, a cantaria volta a responder com intervalos próprios. A lembrança não contém nome, rota nem notícia de Nara."); break; case "beyond": beyondTrace = true; questStage = 29; notice = "SINAL DE PASSAGEM RECUPERADO"; ShowMemory("NÓS QUE PODEM SER DESFEITOS", "O sinal foi deixado por viajantes que passaram pela margem durante a cheia. Ele confirma apenas que o grupo cruzou junto e escolheu para onde seguir ao chegar à outra margem. Não indica qual margem, nome ou destino pertence a Nara."); break; case "witness": upstreamTrace = true; questStage = 26; notice = "MEMÓRIA: A TESTEMUNHA DA MARÉ"; ShowMemory("A ÚLTIMA CURVA DE NARA", "A testemunha não guarda um mapa nem um nome. Guarda a passagem de um grupo que atravessou o rio contra a corrente. Quando alguém a chamou pelo nome escrito no registro de Véspera, Nara respondeu com outro nome e pediu que o grupo o aprendesse. Eles aceitaram e seguiram juntos para além das águas. O eco se encerra antes de revelar a palavra escolhida ou o lugar a que chegaram."); break; case "register": questStage = 23; notice = "REGISTRO DE PASSAGEM RECUPERADO"; ShowMemory("OS NOMES QUE NÃO SÃO POSSE", "O livro contém rotas e relatos escritos por viajantes diferentes. Aven lê em voz alta apenas o trecho assinado por quem escolheu compartilhá-lo. Uma anotação de Nara confirma que ela seguiu adiante com o grupo, mas não revela seu nome atual nem seu paradeiro. O registro fica com a comunidade, não com Véspera."); break; case "chorus": chorusMemory = true; questStage = 14; notice = "MEMÓRIA: CORO DOS NOMES CONSUMIDOS"; ShowMemory("A TECELÃ DE NOMES", "Quando o fio se rompe, as vozes não se calam. O Coro: “Não somos uma coisa que veio de fora. Somos todos os nomes que o Lume tomou e não conseguiu guardar. Nara carregou parte de nós para longe, mas o farol continua nos chamando.” O Silêncio de Baixo é a pressão dessas memórias tentando voltar juntas."); break; case "first": firstMemory = true; questStage = 16; notice = "MEMÓRIA: O PRIMEIRO NOME"; ShowMemory("A PRIMEIRA VIGÍLIA", "A guardiã recorda seu nome: Ilyra, a primeira Escutadora. Ela não acendeu o Lume para apagar as vozes, mas para que toda memória encontrasse um lugar onde ser ouvida. Séculos depois, os guardiões inverteram o propósito e transformaram lembrança em combustível. Ilyra entregou o próprio nome para impedir que o farol esquecesse sua origem. No último eco, viu Nara atravessar a corrente levando vozes que o Lume não comportava. Não sabe o que aconteceu com ela depois, mas deixa um aviso: se alguém a encontrar, não deve chamá-la pelo nome que o farol escolheu."); break; case "west": westTrace = true; questStage = 19; notice = "MEMÓRIA: MAPA DA MARGEM ESCOLHIDA"; ShowMemory("O MAPA DE NARA", "A Cartógrafa guardava um registro de quem atravessou o Vau. Entre os traços recentes, você reconhece a letra de Nara: “Aqui, ninguém precisa conservar o nome que o Lume escolheu. Ajudei a levantar um abrigo e segui com os que queriam continuar a oeste. Se veio atrás de mim, não use o parentesco como resposta. Pergunte primeiro quem escolhi ser.” A tinta e o caminho são seus, mas o mapa não diz quando ela partiu nem onde está agora."); break; case "charm": if (item.Amount is < 1 or > 7) { drops.Remove(item); return; } bool newCharm = (ownedCharms & (1 << (item.Amount - 1))) == 0; ownedCharms |= 1 << (item.Amount - 1); if (equippedCharm == 0 && equippedCharm2 != item.Amount) equippedCharm = item.Amount; else if (equippedCharm2 == 0 && equippedCharm != item.Amount) equippedCharm2 = item.Amount; notice = newCharm ? $"TALISMÃ: {charmNames[item.Amount]}" : $"ECO DUPLICADO: {charmNames[item.Amount]}"; break; case "armor": armor = true; notice = "PEITORAL DE PEDRA ENCONTRADO"; break; case "relic": relic = true; notice = "FRAGMENTO DE VOZ ENCONTRADO"; ShowMemory("ECO PRESO NO RELICÁRIO", "Nara: “Eu vim por vontade própria. Se encontrou esta lembrança, procure o arquivo de Sileth.”"); break; case "seal": archiveSeal = true; damage += 8; questStage = 4; notice = "SELO DA VIGÍLIA • DANO +8"; ShowMemory("SELO DA VIGÍLIA", "O selo guarda a última gravação de Nara: “Sileth não apagou os nomes para alimentar o Lume. Ela tentou esconder que o próprio farol está aprendendo a escolher quem esquecer.” Leve esta prova de volta à Curadora."); break; case "bell": riverBell = true; maxHealth += 20; health = Math.Min(maxHealth, health + 20); questStage = 6; notice = "SINO DE NARA • VIDA MÁXIMA +20"; ShowMemory("O SINO SEM BADALO", "A marca no bronze pertence a Nara. A voz gravada é de uma mulher que atravessou o rio: “Não estou presa. O farol me escuta porque eu aceitei levar as memórias para longe de Véspera. Se ainda me procura, não tente me trazer de volta antes de ouvir o que o rio guarda.”"); break; case "lens": lumeLens = true; damage += 12; questStage = 9; notice = "PRISMA DO LUME • DANO +12"; ShowMemory("O PRISMA DA PRIMEIRA VIGÍLIA", "A luz projeta Nara ao lado de Sileth, anos antes da noite dos sinos. Nara escolheu ser a portadora das lembranças que o Lume não conseguia guardar. A imagem se rompe antes de mostrar onde ela está agora."); break; case "trace": naraTrace = true; questStage = 11; notice = "RASTRO DE NARA • NOVA MEMÓRIA"; ShowMemory("A LEMBRANÇA QUE SOBROU", "O eco não mostra um rosto. Mostra Nara ensinando alguém, do outro lado do rio, a pronunciar um nome que Véspera apagou. Sua última frase não pede resgate: “Se a cidade ainda lembrar de nós, eu saberei voltar.”"); break; }
        drops.Remove(item); noticeTime = 1.7f;
    }

    private void RollWeapon(int index)
    {
        RollWeapon(index, random.Next(100), random.Next(5), random.Next(6), random.Next(5));
    }

    private void RollWeapon(int index, int rarityRoll, int bonusRoll, int affixRoll, int secondAffixRoll = 0)
    {
        var roll = RollWeaponStats(rarityRoll, bonusRoll, affixRoll, secondAffixRoll);
        weaponRarities[index] = roll.Rarity; weaponBonuses[index] = roll.Bonus; weaponAffixes[index] = roll.Affix; weaponAffixes2[index] = roll.Affix2;
    }

    private static (int Rarity, int Bonus, int Affix, int Affix2) RollWeaponStats(int rarityRoll, int bonusRoll, int affixRoll, int secondAffixRoll = 0)
    {
        int rarity = rarityRoll < 62 ? 0 : rarityRoll < 95 ? 1 : 2;
        int bonus = rarity switch { 0 => bonusRoll % 3, 1 => 3 + bonusRoll % 4, _ => 7 + bonusRoll % 5 };
        int affix = rarity == 0 ? 0 : 1 + affixRoll % 6;
        int affix2 = rarity == 2 ? 1 + secondAffixRoll % 5 : 0;
        if (affix2 >= affix && affix2 != 0) affix2++;
        return (rarity, bonus, affix, affix2);
    }

    private static EliteAffix EliteAffixForRoll(int roll) => roll switch { 0 => EliteAffix.Armored, 1 => EliteAffix.Swift, 2 => EliteAffix.Bloodbound, _ => EliteAffix.Spearbound };
    private static string EliteAffixName(EliteAffix affix) => affix switch { EliteAffix.Armored => "BLINDADO", EliteAffix.Swift => "VELOZ", EliteAffix.Bloodbound => "SANGRADOR", EliteAffix.Spearbound => "LANÇADOR", _ => "ELITE" };
    private static bool EliteWeaponDropRoll(int roll) => roll is >= 0 and < 35;
    private static int BossWeaponRollFloor(int room) => room >= 25 ? 85 : room >= 12 ? 75 : 62;

    private Drop CreateWeaponDrop(PointF pos, int index, bool bossReward = false)
    {
        int rarityFloor = vigilActive ? Math.Min(99, 85 + vigilFloor / 2) : BossWeaponRollFloor(room);
        var roll = RollWeaponStats(bossReward ? random.Next(rarityFloor, 100) : random.Next(100), random.Next(5), random.Next(6), random.Next(5));
        return new Drop { Pos = pos, Kind = "weapon", Amount = index, Rarity = roll.Rarity, Bonus = roll.Bonus, Affix = roll.Affix, Affix2 = roll.Affix2 };
    }

    private void UsePotion() { if (potions > 0 && health < maxHealth) { potions--; health = Math.Min(maxHealth, health + 45); notice = "+45 VIDA"; noticeTime = 1.3f; } else { notice = potions == 0 ? "SEM POÇÕES" : "VIDA JÁ ESTÁ CHEIA"; noticeTime = 1.3f; } }
    private void Interact()
    {
        if (room == 3) { InteractTown(); return; }
        if (room == 18) { InteractRefuge(); return; }
        if (enemies.Count != 0) { notice = "DERROTE OS INIMIGOS PRIMEIRO"; noticeTime = 1.2f; return; }
        if (drops.Count != 0) { notice = "RECOLHA O LOOT ANTES DE AVANÇAR"; noticeTime = 1.5f; return; }
        MarkRoomCleared();
        if (vigilActive && room == 38 && (Distance(player, VigilArenaAltarPosition) < 76 || player.X > 790)) { vigilChoiceOpen = true; return; }
        if (room < MemoryStonePositions.Length && (memoriesFound & (1 << room)) == 0 && Distance(player, MemoryStonePositions[room]) < MemoryStoneInteractionRadius)
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
        else if (room == 28 && player.X > 790 && questStage >= 39) { room = 18; SetupRoom(); ShowMemory("A ESCARPA RESPIRA", "O fluxo já não se fecha sobre o próprio centro. A memória do Afinador pode ser levada a Aven para que a comunidade registre o que aprendeu sem transformar o caminho em ordem."); }
        else if (room == 28 && player.X > 790 && questStage >= 37) { room = 29; SetupRoom(); if (questStage == 37) { questStage = 38; ShowMemory("CANTARIA DAS VOZES LIVRES", "Abaixo do antigo vertedouro, cada pedra responde num tom diferente. Nenhuma voz conduz as outras; ainda assim, um ruído tenta obrigá-las a repetir a mesma nota. Os riscos no chão levam à câmara onde o som se parte."); } }
        else if (room == 28 && player.X > 790 && questStage >= 35) { room = 18; SetupRoom(); ShowMemory("A ESCARPA RESPIRA", "O fluxo já não se fecha sobre o próprio centro. A memória do Afinador pode ser levada a Aven para que a comunidade registre o que aprendeu sem transformar o caminho em ordem."); }
        else if (room == 29 && player.X < 115) { room = 28; SetupRoom(); }
        else if (room == 29 && player.X > 790 && enemies.Count == 0 && drops.Count == 0) { room = 30; SetupRoom(); ShowMemory("CÂMARA DA CONTRAVOZ", "Um eco imóvel ocupa o centro da câmara. Ele anuncia três ataques em sequência, mas cada golpe parte de uma nota diferente: baixe a guarda no intervalo, não no som."); }
        else if (room == 30 && player.X < 115) { room = 29; SetupRoom(); }
        else if (room == 30 && player.X > 790 && questStage >= 39) { room = 18; SetupRoom(); ShowMemory("A NOTA QUE FALTAVA", "A câmara volta a sustentar acordes diferentes. A lembrança recolhida não contém nome nem rota; guarda apenas a forma como as vozes escolheram responder umas às outras."); }
        else if (room == 31 && player.X < 115) { room = 18; SetupRoom(); }
        else if (room == 31 && player.X > 790 && enemies.Count == 0 && drops.Count == 0) { room = 32; SetupRoom(); ShowMemory("FONTE SOB A PEDRA", "A garganta desemboca numa nascente soterrada. O arauto de vidro guarda passos sem dono: não sabe quem os deu nem para onde levaram."); }
        else if (room == 32 && player.X < 115) { room = 31; SetupRoom(); }
        else if (room == 32 && player.X > 790 && questStage >= 42) { room = 18; SetupRoom(); ShowMemory("O ECO SEM DONO", "A fonte devolve o som dos passos, mas não lhes atribui nome, origem ou destino. A lembrança pode ser compartilhada sem decidir o caminho de ninguém."); }
        else if (room == 33 && player.X < 115) { room = 18; SetupRoom(); }
        else if (room == 33 && player.X > 790 && enemies.Count == 0 && drops.Count == 0) { room = 34; SetupRoom(); ShowMemory("OBSERVATÓRIO DA MARÉ INTERIOR", "Sob a fonte, lanternas antigas giram em torno de um espelho de água. O Regente não guarda a passagem: tenta fazer cada reflexo permanecer voltado para trás. Três sinais de luz se formam no chão antes de cada golpe."); }
        else if (room == 34 && player.X < 115) { room = 33; SetupRoom(); }
        else if (room == 34 && player.X > 790 && questStage >= 45) { room = 18; SetupRoom(); ShowMemory("REGRESSO À MESA DE AVEN", "O sinal não mostra quem atravessou nem em que direção. Ainda assim, a chama permanece acesa sem exigir que alguém retorne. Aven pode guardar essa promessa para qualquer viajante que escolha aceitá-la."); }
        else if (room == 35 && player.X < 115) { room = 33; SetupRoom(); }
        else if (room == 35 && player.X > 790 && enemies.Count == 0 && drops.Count == 0) { room = 36; SetupRoom(); ShowMemory("GALERIA DA ROTA NÃO ESCRITA", "As linhas das paredes convergem para uma câmara sem saída desenhada. O Escriba do Traçado move as marcas para fechar os caminhos antes que alguém possa escolher por onde seguir."); }
        else if (room == 36 && player.X < 115) { room = 35; SetupRoom(); }
        else if (room == 36 && player.X > 790 && questStage >= 48) { room = 18; SetupRoom(); ShowMemory("REGRESSO À MESA DE AVEN", "O traçado guardado não aponta para Nara. Leve a lembrança a Aven sem transformá-la em uma pista que ela nunca escolheu deixar."); }
        else if (room == 37 && player.X < 115) { room = 18; SetupRoom(); }
        else if (room == 37 && player.X > 790 && enemies.Count == 0 && drops.Count == 0 && questStage >= 50) { room = 38; SetupRoom(); ShowMemory("CÂMARA DO ÚLTIMO ECO", "O corredor termina onde o rio encontra a pedra sob Véspera. O Silêncio não tem corpo próprio: veste fragmentos das vozes que o Lume reteve. Seus três golpes são anunciados pelo chão — uma onda ao redor, um eco que persegue seus passos e uma linha que cruza a câmara."); }
        else if (room == 38 && player.X < 115 && vigilActive) { notice = "A VIGÍLIA SÓ TERMINA NO ALTAR CENTRAL"; noticeTime = 2; }
        else if (room == 38 && player.X < 115) { room = 37; SetupRoom(); }
        else if (room == 38 && player.X > 790 && silenceMemory) { room = 18; SetupRoom(); ShowMemory("REGRESSO AO REFÚGIO", "A memória final não revela o paradeiro de Nara. Ela devolve ao Escutador a responsabilidade de decidir o destino do Lume com a comunidade, sem falar em nome de quem partiu."); }
        else if (room == 39 && player.X < 115) { room = 18; SetupRoom(); }
        else if (room == 39 && player.X > 790 && enemies.Count == 0 && drops.Count == 0) { room = 40; SetupRoom(); ShowMemory("ARQUIVO DA MARÉ SEM NOME", "O arquivo está sob a linha da maré. Uma figura feita de ecos sobrepostos tenta manter todos os sons presos no mesmo compasso."); }
        else if (room == 40 && player.X < 115) { room = 39; SetupRoom(); }
        else if (room == 40 && player.X > 790 && questStage >= 55) { room = 18; SetupRoom(); ShowMemory("REGRESSO À MESA COMUNITÁRIA", "O arquivo permanece aberto. Os sons podem ser ouvidos por quem os deixou, sem revelar identidade ou destino."); }
        else if (room == 11 && player.X > 790 && questStage >= 13) { room = 12; SetupRoom(); ShowMemory("COSTA DAS VOZES TARDIAS", townEnding == 1 ? "As badaladas com os nomes de Véspera atravessam o rio. Ecos se afastam da trilha; a cidade devolveu força a estas vozes." : "O Lume permanece aceso atrás de você. Sua luz atravessa as águas sem reflexo e atrai para a costa tudo o que foi esquecido."); }
        else if (room == 11 && player.X > 790 && naraTrace) { room = 3; SetupRoom(); ShowMemory("REGRESSO A VÉSPERA", "Você carrega uma lembrança que o Lume não conseguiu apagar. Sileth precisa ouvi-la antes de decidir o que fazer com os arquivos."); }
    }
    private void InteractTown()
    {
        if (Distance(player, CuratorPosition) < InteractionRadius)
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
        if (Distance(player, BellKeeperPosition) < InteractionRadius)
        {
            if (questStage == 6 && riverBell) { questStage = 7; gold += 100; potions++; ShowMemory("EDDA RECONHECE O SINO", "Esse era o sino de travessia do meu pai. Nara o levou quando desceu ao rio; ela conhecia o caminho e decidiu cruzá-lo. Não foi capturada. Guarde o sino: se ela voltar, vai saber que ainda lembramos seu nome."); notice = "MISSÃO CONCLUÍDA: 100 OURO E 1 POÇÃO"; noticeTime = 3; }
            else if (questStage == 19 && westTrace) { questStage = 20; gold += 375; potions++; ShowMemory("A CARTA DO REFÚGIO", "Nara construiu um lugar onde cada pessoa escolhe como será chamada e partiu para oeste com quem pediu para atravessar. Edda reconhece as marcas do mapa: “Consigo levar vocês até a próxima curva, mas dali em diante será preciso seguir a água.” Sileth deixa o arquivo aberto para a volta do grupo. A notícia não prova onde Nara está agora."); notice = "RECOMPENSA: 375 OURO E 1 POÇÃO"; noticeTime = 3; }
            else if (questStage is 17 or 18) { questStage = 18; room = 16; SetupRoom(); ShowMemory("A TRAVESSIA DO CONTRA-RIO", "Edda solta o barco de fundo largo. Os nomes devolvidos aos sinos ajudam a encontrar a passagem entre as pedras; o Lume mantido aceso ilumina os marcos submersos, mas chama ecos para o Vau. A cartógrafa da margem guarda a entrada do refúgio de Nara."); }
            else if (questStage == 20) { questStage = 21; room = 18; SetupRoom(); ShowMemory("REFÚGIO DO VEIO CALMO", "O barco de Edda segue as lanternas até uma embarcação antiga, encalhada entre as pedras. Pessoas que partiram de Véspera construíram ali um abrigo. Nara ajudou a traçar as primeiras rotas, mas continuou viagem com quem escolheu partir. Aven, o antigo barqueiro, espera junto à mesa comunitária."); }
            else if (questStage >= 21) ShowMemory("EDDA, NO CAIS", "O mapa de Nara marca outra curva a oeste, além do refúgio. Ainda não sabemos onde ela está; a trilha continua aberta.");
            else if (townEnding == 1) ShowMemory("EDDA, TOCADORA DE SINOS", "Os nomes voltaram às badaladas. Vou ensinar as crianças a ler cada sino, para que ninguém precise adivinhar quem foi esquecido."); else if (townEnding == 2) ShowMemory("EDDA, TOCADORA DE SINOS", "Os sinos agora marcam as horas do Lume. Não vou deixar que a luz transforme proteção em silêncio outra vez."); else ShowMemory("EDDA, TOCADORA DE SINOS", "Os sinos continuam inteiros. O que sumiu foram os nomes gravados neles. Quando tento lembrar quem os fundiu, só escuto água debaixo da praça.");
            return;
        }
        if (Distance(player, ArchiveBookPosition) < InteractionRadius)
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
        if (Distance(player, new PointF(480, 135)) < InteractionRadius)
        {
            if (questStage >= 3) { room = 4; SetupRoom(); ShowMemory("ARQUIVO NORTE", "O portão cede. Entre as estantes, ecos repetem trechos de depoimentos que foram removidos do registro oficial."); }
            else ShowMemory("PORTÃO DO ARQUIVO NORTE", "O selo da Curadora mantém o portão fechado. Descubra o que o Livro das Ausências prova e fale com Sileth.");
            return;
        }
        if (Distance(player, new PointF(480, 520)) < InteractionRadius)
        {
            if (questStage >= 7) { room = 8; SetupRoom(); ShowMemory("ESCADA DE CINZAS", "O sino abre uma passagem abaixo da Capela Afogada. A cada degrau, o ar fica mais quente e as lembranças parecem chegar antes de você."); }
            else if (questStage >= 5) { room = 6; SetupRoom(); ShowMemory("LEITO DO RIO CEGO", "A água desapareceu da superfície, mas as pedras ainda guardam as últimas palavras de quem cruzou o rio. O sino de Nara aponta para baixo da corrente."); }
            else ShowMemory("PASSAGEM DO RIO", "Edda não reconhece o caminho para o leito seco. Conclua sua investigação com Sileth antes de descer.");
            return;
        }
        if (Distance(player, CryptGatePosition) < InteractionRadius)
        {
            room = 2; SetupRoom(); ShowMemory("A CRIPTA", "A passagem continua aberta. O Guardião protege o relicário, mas a próxima pista está nos arquivos de Véspera.");
        }
    }
    private void InteractRefuge()
    {
        if (Distance(player, VigilAltarPosition) < InteractionRadius)
        {
            if (finalEnding == 0) ShowMemory("A VIGÍLIA AINDA NÃO ESTÁ ABERTA", "Aven só consegue reunir estes ecos depois que Véspera decide o destino do Lume. Conclua a campanha e volte quando estiver pronto para uma prova sem fim.");
            else StartVigil();
            return;
        }
        if (Distance(player, AvenPosition) < InteractionRadius)
        {
            if (questStage == 21) { questStage = 22; ShowMemory("AVEN, O BARQUEIRO", "Não mantemos um livro de nomes. Cada viajante decide o que quer deixar na mesa e quem pode ler. Nara ajudou a marcar a Trilha dos Luminários e seguiu adiante com outras pessoas. Ela pediu que não contássemos o nome que escolheu nem prometêssemos que voltaria. Se quiser continuar, proteja os avisos que cada pessoa decidiu compartilhar."); }
            else if (questStage == 23) { questStage = 24; gold += 240; potions += 2; ShowMemory("A MESA CONTINUA ABERTA", "A comunidade escolhe guardar os registros de passagem aqui. A nota de Nara termina antes da próxima curva; ninguém vai preencher a lacuna por ela. Aven oferece provisões para a viagem e aponta as lanternas mais recentes para oeste. Se um dia ela voltar, será ela quem decidirá o que contar."); notice = "REFÚGIO: 240 OURO E 2 POÇÕES"; noticeTime = 3; }
            else if (questStage == 26 && upstreamTrace) { questStage = 27; gold += 320; potions++; ShowMemory("A TESTEMUNHA SEM REGISTRO", "Aven ouve a lembrança sem perguntar qual era o nome anterior nem o escolhido por Nara. Ele registra apenas que um grupo chegou junto à nascente e continuou viagem por vontade própria. “Se você ainda quer encontrá-la, a próxima pergunta precisa ser feita a ela. A nossa comunidade não vai responder em seu lugar.” A trilha segue além do alcance das lanternas."); notice = "RECOMPENSA: 320 OURO E 1 POÇÃO"; noticeTime = 3; }
            else if (questStage == 29 && beyondTrace) { questStage = 30; gold += 420; potions++; ShowMemory("AVEN GUARDA O SINAL", "A marca confirma que o grupo de Nara cruzou a cheia e escolheu o próprio rumo ao chegar à margem. Aven a prende à mesa comunitária sem copiar o trajeto: “Esse relato diz que escolheram. Não diz que alguém precisa segui-los.” Ele oferece provisões e mantém o caminho aberto para quem vier depois."); notice = "RECOMPENSA: 420 OURO E 1 POÇÃO"; noticeTime = 3; }
        else if (questStage == 32) { questStage = 33; ebbSeal = true; gold += 480; potions += 2; ShowMemory("A MARCA DA VAZANTE", "Aven entrega um selo de bronze que abre uma passagem curta na água ao redor do Escutador. Ele não controla a corrente: por um instante, reúne os ecos próximos para que possam ser enfrentados juntos. “Use para abrir espaço, não para escolher o rumo de alguém.” A habilidade Vazante foi liberada."); notice = "RECOMPENSA: 480 OURO • VAZANTE LIBERADA"; noticeTime = 3; }
            else if (questStage == 35 && counterTrace) { questStage = 36; anchorWard = true; gold += 560; tideGlass += 3; potions += 2; ShowMemory("A CORRENTE TEM DUAS MARGENS", "Aven escuta o relato do Coração e acrescenta ao mapa comunitário apenas isto: a passagem pode mudar de sentido sem deixar de ser passagem. Ele ensina o Manto de Lastro: por um breve instante, o corpo acompanha a firmeza das duas margens e amortece os golpes. Ele entrega sal vítreo e provisões para a próxima jornada; nenhuma direção é marcada em nome de quem não está aqui."); notice = "RECOMPENSA: MANTO DE LASTRO • 560 OURO • 3 SAL VÍTREO • 2 POÇÕES"; noticeTime = 3; }
            else if (questStage == 36) { questStage = 37; ShowMemory("O VERTEDOURO ANTIGO", "Aven reconhece um intervalo novo entre os sons da margem. Sob o velho vertedouro existe uma cantaria onde viajantes deixavam notas de passagem sem registrar quem as tocou. O eco que agora responde não pertence a nenhuma das vozes guardadas. “Vá ouvir o que ele tenta calar; depois, conte apenas o que a pedra realmente disser.”"); notice = "NOVO DESTINO: CANTARIA DAS VOZES LIVRES"; noticeTime = 3; }
            else if (questStage == 39 && riftVerse) { questStage = 40; refrainSong = true; gold += 650; tideGlass += 4; potions += 2; ShowMemory("O CORO DE RETORNO", "Aven ouve a nota sem atribuí-la a uma pessoa. Ele ensina o Escutador a devolver um eco à pedra: o Coro de Retorno fere e interrompe ecos próximos, mas não cala os ataques dos chefes. O relato fica na mesa com as palavras que a própria cantaria permitiu compartilhar."); notice = "RECOMPENSA: CORO DE RETORNO • 650 OURO • 4 SAL VÍTREO • 2 POÇÕES"; noticeTime = 3; }
            else if (questStage == 40 && refrainSong) { questStage = 41; ShowMemory("A PEDRA ALÉM DO VERTEDOURO", "A nota da cantaria encontra uma fissura sob a corrente. Aven nunca ouviu um nome vindo dali, apenas passos que passam sem deixar registro. “Não é a trilha de Nara, pelo menos nada aqui permite afirmar isso. Se quiser escutar, leve somente o que a pedra disser.” Uma garganta de vidro se abriu a oeste do refúgio."); notice = "NOVO DESTINO: GARGANTA DO VIDRO MUDO"; noticeTime = 3; }
            else if (questStage == 42 && deepTrace) { questStage = 43; gold += 820; tideGlass += 5; potions += 2; ShowMemory("UM ECO SEM DONO", "Aven registra apenas que a fonte repetiu passos de viajantes que escolheram partir sem escrever a rota. Ele não acrescenta o som ao mapa de Nara: “Uma passagem pode lembrar que alguém esteve aqui sem dizer quem era, nem para onde foi.” A comunidade deixa a fonte aberta para qualquer pessoa que queira atravessar."); notice = "RECOMPENSA: 820 OURO • 5 SAL VÍTREO • 2 POÇÕES"; noticeTime = 3; }
            else if (questStage == 43) { questStage = 44; ShowMemory("AS LANTERNAS QUE OLHAM PARA DENTRO", "Aven reconhece uma vibração sob a fonte, distante do caminho de Nara. “As lanternas antigas não foram feitas para apontar uma direção. Elas guardavam o calor de quem escolhia parar; alguém as virou para dentro e transformou lembrança em espelho.” Ele abre a descida até o observatório. O que houver lá não pode responder por quem partiu.”"); notice = "NOVO DESTINO: PASSAGEM DAS LANTERNAS BAIXAS"; noticeTime = 3; }
            else if (questStage == 45 && lanternSignal) { questStage = 46; beaconLight = true; gold += 980; tideGlass += 6; potions += 3; ShowMemory("O FAROL DE VIGÍLIA", "Aven prende o sinal na mesa sem escrever nome ou rumo. A chama pode acompanhar o Escutador por alguns instantes e pulsar contra ecos próximos: uma luz de apoio, não um guia. “Que ela permaneça acesa para quem quiser voltar, e não como promessa de que alguém voltará.” Você aprendeu Farol de Vigília."); notice = "TÉCNICA: FAROL DE VIGÍLIA • 980 OURO • 6 SAL VÍTREO"; noticeTime = 3; }
            else if (questStage == 46 && beaconLight) { questStage = 47; ShowMemory("AS LINHAS QUE NÃO APONTAM", "O Farol ilumina marcas antigas sob a mesa de Aven. Elas não formam uma rota para Nara: são rastros de pessoas que voltaram a Véspera, partiram para outros lugares ou escolheram ficar sem deixar nome. Aven abre o caminho até a Galeria da Rota Não Escrita para descobrir por que alguém tentou apagar todas essas opções."); notice = "NOVO DESTINO: TRILHA DAS LINHAS ABERTAS"; noticeTime = 3; }
            else if (questStage == 48 && routeMemory) { questStage = 49; gold += 1300; tideGlass += 7; potions += 3; ShowMemory("A MESA GUARDA ESPAÇO", "Aven escuta o traçado sem copiá-lo para o mapa de Nara. As linhas registram escolhas de muitas pessoas e terminam em direções diferentes; nenhuma confirma quem voltou, quem partiu ou quem ainda está a caminho. A comunidade oferece provisões e mantém a mesa aberta para o próximo relato, sem preencher os espaços deixados em branco."); notice = "RECOMPENSA: 1300 OURO • 7 SAL VÍTREO • 3 POÇÕES"; noticeTime = 3; }
            else if (questStage == 49 && routeMemory) { questStage = 50; ShowMemory("CAPÍTULO XVI — O SILÊNCIO DE BAIXO", "As linhas abertas não apontam para Nara. Apontam para o lugar sob Véspera onde o Lume começou a reter lembranças. Aven não pede que você a encontre nem que a traga de volta: “Se quiser descer, vá por você. O que encontrar lá pertence às vozes que escolherem responder.” Uma soleira se abre além da Galeria da Rota Não Escrita."); notice = "NOVO DESTINO: SOLEIRA DO SILÊNCIO"; noticeTime = 3; }
            else if (questStage == 51 && silenceMemory && finalEnding == 0) { endingChoiceOpen = true; notice = "O DESTINO DO LUME AGUARDA SUA DECISÃO • 1 OU 2"; noticeTime = 3; }
            else if (finalEnding > 0 && questStage is 52 or 53) { questStage = 54; ShowMemory("O ARQUIVO DAS MARÉS EMPRESTADAS", "Aven encontrou uma nota sem assinatura na mesa comunitária. Ela descreve um cais onde viajantes deixavam sons que aceitavam compartilhar, sem nomes ou destinos. O registro foi levado para um arquivo sob a maré; algo lá tenta transformar muitas vozes numa só. A expedição é sua escolha."); notice = "EXPEDIÇÃO OPCIONAL XVII: CAIS DOS ECOS EMPRESTADOS"; noticeTime = 3; }
            else if (questStage == 55 && borrowedTrace) { questStage = 56; gold += 1900; tideGlass += 8; potions += 3; techniqueTalent++; ShowMemory("UM LUGAR PARA DEIXAR ECO", "Aven guarda a memória com o consentimento de quem a deixou. Não escreve nomes nem rotas. Como agradecimento, a comunidade oferece provisões e ensina uma nova técnica: o Escutador agora causa mais dano com todas as habilidades."); notice = "RECOMPENSA: +1900 OURO • +8 SAL • +3 POÇÕES • TÉCNICA +1"; noticeTime = 4; }
            else if (finalEnding > 0 && questStage >= 56) { forgeMenuOpen = true; return; }
            else if (finalEnding > 0) ShowMemory(finalEnding == 1 ? "VÉSPERA APRENDE A OUVIR" : "O FAROL SEM CORRENTE", finalEnding == 1 ? "Os arquivos estão abertos e a comunidade divide a proteção da cidade. As memórias devolvidas podem ser ouvidas por quem as reivindicar. Nara permanece livre para escolher quando e se quer voltar." : "O Lume protege Véspera sem chamar as vozes perdidas à força. A comunidade sabe o preço da luz e pode decidir, em conjunto, como mantê-la. Nara permanece livre para escolher quando e se quer voltar.");
            else if (questStage >= 27) { forgeMenuOpen = true; return; }
            else if (questStage >= 24) ShowMemory("AVEN, O BARQUEIRO", "A mesa continua aberta a quem escolhe deixar um relato. A próxima curva fica a oeste; os viajantes que passaram por aqui decidiram compartilhar apenas o início da trilha.");
            else ShowMemory("AVEN, O BARQUEIRO", "Este barco encalhado virou abrigo. Ninguém precisa escrever um nome para ficar aqui.");
            return;
        }
        if (Distance(player, SeraPosition) < InteractionRadius) { ShowMemory("SERA, REPARADORA DE LANTERNAS", finalEnding == 1 ? "Agora que as memórias foram devolvidas, vou cuidar das lanternas junto com quem quiser ajudar. Nenhuma chama precisa guardar tudo sozinha." : finalEnding == 2 ? "O farol ainda ilumina o rio, mas não chama ninguém de volta. Vou manter uma lanterna baixa para quem chegar por vontade própria." : "As lanternas não marcam uma rota para tomar posse dela. Cada pessoa acende uma só quando quer deixar um aviso para quem vier depois."); return; }
        if (Distance(player, NoaPosition) < InteractionRadius) { ShowMemory("NOA, APRENDIZ DE CARTÓGRAFA", finalEnding > 0 ? "Deixei um espaço em branco no mapa para as viagens que ainda não começaram. Aven diz que um caminho pode existir sem pertencer a quem o desenha." : "Aprendi a desenhar os trechos que meus amigos me deixam ver. O resto do mapa fica em branco por escolha deles."); return; }
        if (player.X < 150) { room = 3; SetupRoom(); return; }
        if (player.X > 790 && questStage >= 54) { room = 39; SetupRoom(); ShowMemory("CAIS DOS ECOS EMPRESTADOS", "O cais fica numa enseada protegida por pedras cobertas de marcas. Cada viajante escolheu um som curto para deixar no arquivo; nenhum registro guarda um nome ou aponta um destino."); return; }
        if (player.X > 790 && questStage >= 50) { room = 37; SetupRoom(); ShowMemory("SOLEIRA DO SILÊNCIO", "Sob a galeria, uma descida feita de pedras sem inscrição atravessa a rocha. A cada passo, o Lume perde força e as vozes deixam de repetir a mesma frase. Nenhuma marca indica que Nara esteve aqui."); return; }
        if (player.X > 790 && questStage >= 47) { room = 35; SetupRoom(); ShowMemory("TRILHA DAS LINHAS ABERTAS", "A leste da fonte, uma trilha de pedra clara se divide muitas vezes. Algumas marcas indicam retornos, outras terminam sem destino. Nenhuma leva o nome de Nara. Ecos de tinta se movem sobre os traços, tentando obrigá-los a apontar todos para o mesmo lugar."); return; }
        if (player.X > 790 && questStage >= 44 && questStage < 47) { room = 33; SetupRoom(); ShowMemory("PASSAGEM DAS LANTERNAS BAIXAS", "Atrás do Refúgio, uma escadaria desce sob a fonte. As lanternas foram cobertas para que sua luz não revelasse rostos nem traçasse caminhos. Um eco pesado guarda a descida para o espelho interior."); return; }
        if (player.X > 790 && questStage >= 41) { room = 31; SetupRoom(); ShowMemory("GARGANTA DO VIDRO MUDO", "A fissura sob o vertedouro desce até uma garganta de cristal escuro. As paredes devolvem o som dos seus passos, mas não guardam nenhum nome. À frente, a pedra se estreita numa fonte antiga."); return; }
        if (player.X > 790 && questStage >= 33) { if (questStage == 33) questStage = 34; room = 27; SetupRoom(); ShowMemory("ESCARPA DO CONTRAFLUXO", "A água que saiu dos moinhos sobe por uma escarpa de pedra negra. As marcas nas paredes se alternam: umas apontam para dentro, outras para fora. Mais adiante, o Coração das Duas Margens pulsa sem escolher qual delas deve receber a corrente."); return; }
        if (player.X > 790 && questStage >= 30) { if (questStage == 30) questStage = 31; room = 25; SetupRoom(); ShowMemory("MOINHOS DA VAZANTE", "Aven reconhece o ruído das rodas de água: elas não moíam grão, mas regulavam a cheia para que a comunidade pudesse atravessar. O canal foi fechado por dentro. A água ainda empurra em direção à câmara."); return; }
        if (player.X > 790 && questStage >= 24) { if (questStage == 24) questStage = 25; room = 21; SetupRoom(); ShowMemory("A VÁRZEA DAS VOZES SOPRADAS", "Além da cisterna, o rio se divide entre campos de sal e pedra lisa. As lanternas acabam aqui. Marcas recentes mostram que viajantes cruzaram a água sem deixar um destino escrito."); return; }
        if (player.X > 790 && questStage >= 22) { room = 19; SetupRoom(); ShowMemory("TRILHA DOS LUMINÁRIOS", "Pequenas lanternas indicam os pontos onde viajantes aceitaram deixar um aviso. As marcas não formam um mapa completo: cada pessoa decidiu quanto do caminho compartilhar."); return; }
        notice = questStage < 22 ? "FALE COM AVEN PARA CONHECER A COMUNIDADE" : "E  SEGUIR A TRILHA DOS LUMINÁRIOS"; noticeTime = 1.5f;
    }
    private void ShowMemory(string titleText, string bodyText) { memoryTitle = titleText; memoryText = bodyText; memoryOverlay = true; }
    private void MarkRoomCleared() { if (room != 3 && enemies.Count == 0 && drops.Count == 0) clearedRooms |= 1L << room; }
    private Enemy? lockedTarget;
    private void CycleTarget() { if (enemies.Count == 0) { lockedTarget = null; return; } int i = lockedTarget is null ? -1 : enemies.IndexOf(lockedTarget); lockedTarget = enemies[(i + 1) % enemies.Count]; notice = $"ALVO: {EnemyName(lockedTarget)}"; noticeTime = 1.4f; }
    private void EquipWeapon(int index) { if ((ownedWeapons & (1 << index)) == 0) { notice = "ARMA AINDA NÃO ENCONTRADA"; noticeTime = 1.4f; return; } damage += WeaponPower(index) - WeaponPower(weapon); weapon = index; }
    private void CycleCharm(int slot)
    {
        int current = slot == 1 ? equippedCharm : equippedCharm2;
        int other = slot == 1 ? equippedCharm2 : equippedCharm;
        int charmCount = charmNames.Length - 1;
        for (int i = 1; i <= charmCount; i++)
        {
            int candidate = ((current - 1 + i + charmCount) % charmCount) + 1;
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
    private float ArmorDamageTakenMultiplier() => !armorOn ? 1f : relicOn ? 0.65f : 0.7f;
    private int RelicKillHealing() => !relicOn ? 0 : armorOn ? 8 : 5;
    private float WhirlwindMultiplier() => HasCharmPair(1, 2) ? 2.52f : HasCharm(1) ? 2.16f : 1.6f;
    private float WhirlwindCooldownPenalty() => !HasCharm(1) ? 0f : HasCharmPair(1, 2) ? 0.5f : 1f;
    private float VoiceShardDamage() => damage * (HasCharmPair(3, 5) ? 2.05f : HasCharm(3) ? 1.8f : 1.35f) * WeaponSkillMultiplier() * TechniqueDamageMultiplier();
    private float VoiceShardLifetime() => HasCharm(5) ? 1.8f : 1.25f;
    private float VoiceShardCooldown() => (Math.Max(3.5f, 7f - skillTalent * 0.5f) + (HasCharm(5) ? HasCharmPair(3, 5) ? 0.5f : 1.25f : 0f)) * WeaponCooldownMultiplier();
    private float WeaponCooldownMultiplier() => HasWeaponAffix(weapon, 4) ? weaponRarities[weapon] == 2 ? 0.84f : 0.9f : 1f;
    private float WeaponDamageTakenMultiplier() => HasWeaponAffix(weapon, 5) ? weaponRarities[weapon] == 2 ? 0.88f : 0.92f : 1f;
    private float WeaponReachMultiplier() => HasWeaponAffix(weapon, 6) ? weaponRarities[weapon] == 2 ? 1.2f : 1.12f : 1f;
    private float OutgoingDamageMultiplier(Enemy enemy)
    {
        float bossHunter = !HasCharm(4) ? 1f : enemy.Boss ? 1.25f : 0.8f;
        return bossHunter * (vigilActive && HasCharm(7) ? 1.4f : 1f);
    }
    private float DashDuration() => HasCharm(2) ? 0.29f : 0.17f;
    private PointF MovementInput() => new((down.Contains(Keys.D) || down.Contains(Keys.Right) ? 1 : 0) - (down.Contains(Keys.A) || down.Contains(Keys.Left) ? 1 : 0), (down.Contains(Keys.S) || down.Contains(Keys.Down) ? 1 : 0) - (down.Contains(Keys.W) || down.Contains(Keys.Up) ? 1 : 0));
    private void StartDash()
    {
        if (dashCd > 0) return;
        PointF movement = MovementInput();
        dashDirection = Normalize(movement, facing);
        if (Length(movement) > 0.001f) facing = dashDirection;
        dashTime = DashDuration(); dashCd = DashCooldown();
        GameAudio.Play(GameAudio.Cue.Dash);
    }
    private float DashCooldown() => Math.Max(0.35f, 1.15f - dodgeTalent * 0.12f) + (HasCharm(2) && !HasCharmPair(2, 3) ? 0.3f : 0f);
    private float CriticalChance() => (weapon == 2 ? 0.25f : 0.12f) + (HasCharm(3) ? HasCharmPair(1, 3) ? 0.17f : 0.12f : 0f) + (HasWeaponAffix(weapon, 2) ? weaponRarities[weapon] == 2 ? 0.11f : 0.07f : 0f);

    private static bool ValidWeaponData(SaveData s)
    {
        if (s.WeaponBonuses is null || s.WeaponBonuses.Length != 3 || s.WeaponRarities is null || s.WeaponRarities.Length != 3 || s.WeaponAffixes is null || s.WeaponAffixes.Length != 3 || s.WeaponAffixes2 is null || s.WeaponAffixes2.Length != 3) return false;
        for (int i = 0; i < 3; i++)
        {
            int rarity = s.WeaponRarities[i], bonus = s.WeaponBonuses[i], affix = s.WeaponAffixes[i], affix2 = s.WeaponAffixes2[i];
            if (rarity == 0 ? bonus is < 0 or > 2 || affix != 0 || affix2 != 0 : rarity == 1 ? bonus is < 3 or > 6 || affix is < 1 or > 6 || affix2 != 0 : rarity == 2 ? bonus is < 7 or > 11 || affix is < 1 or > 6 || affix2 is < 0 or > 6 || affix2 == affix : true) return false;
            if ((s.OwnedWeapons & (1 << i)) == 0 && (rarity != 0 || bonus != 0 || affix != 0 || affix2 != 0)) return false;
        }
        return true;
    }
    private void RestoreWeaponData(SaveData s) { Array.Copy(s.WeaponBonuses, weaponBonuses, 3); Array.Copy(s.WeaponRarities, weaponRarities, 3); Array.Copy(s.WeaponAffixes, weaponAffixes, 3); Array.Copy(s.WeaponAffixes2, weaponAffixes2, 3); for (int i = 0; i < 3; i++) if (weaponRarities[i] == 2 && weaponAffixes2[i] == 0) weaponAffixes2[i] = weaponAffixes[i] % 5 + 1; }
    private float CriticalMultiplier() => HasCharm(3) ? HasCharmPair(1, 3) ? 2.75f : 2.5f : 2f;
    private float BasicAttackCooldown() => (weapon == 1 ? 0.52f : weapon == 2 ? 0.23f : 0.33f) + (HasCharm(3) ? HasCharmPair(1, 3) ? 0.03f : 0.06f : 0f);
    private float BasicAttackReach(Enemy enemy) => (weapon == 1 ? (enemy.Boss ? 122f : 108f) : weapon == 2 ? (enemy.Boss ? 80f : 68f) : (enemy.Boss ? 95f : 78f)) * WeaponReachMultiplier();
    private static float BasicAttackSweepAngle(int weapon) => weapon == 1 ? 142 : weapon == 2 ? 112 : 128;
    private static float BasicAttackArcThreshold(int weapon) => MathF.Cos(BasicAttackSweepAngle(weapon) * MathF.PI / 360f);
    private static bool BasicAttackCoversDirection(float facingDot, int weapon) => facingDot > BasicAttackArcThreshold(weapon);
    private bool IsArmored(Enemy enemy) => enemy.Armored || (!enemy.Boss && enemy.Kind != EnemyKind.Elite && enemies.Any(elite => elite.Kind == EnemyKind.Elite && elite.EliteAffix == EliteAffix.Armored && Distance(enemy.Pos, elite.Pos) <= 132));
    private float BasicAttackDamageMultiplier(Enemy enemy) => IsArmored(enemy) ? weapon == 1 ? 0.9f : 0.75f : 1f;
    private string WeaponTraitDescription(int index) => index switch
    {
        1 => "LÂMINA PESADA • ALCANCE LONGO • PERFURA ARMADURA • INTERROMPE ECOS",
        2 => "FACA RÁPIDA • CURTO ALCANCE • 25% DE CRÍTICO",
        _ => "ESPADA EQUILIBRADA • ALCANCE MÉDIO"
    };
    private PointF MoveEnemy(Enemy e, PointF delta) { PointF p = e.Pos; PointF x = new(p.X + delta.X, p.Y), y = new(p.X, p.Y + delta.Y); if (!Blocked(x, e.Radius)) p.X = x.X; if (!Blocked(new PointF(p.X, y.Y), e.Radius)) p.Y = y.Y; return new PointF(Math.Clamp(p.X, 85, 875), Math.Clamp(p.Y, 128, 533)); }
    private void SeparateFromPack(Enemy enemy, float dt) { PointF push = PointF.Empty; foreach (Enemy other in enemies) { if (ReferenceEquals(enemy, other)) continue; PointF away = Sub(enemy.Pos, other.Pos); float d = Length(away); if (d > 0.01f && d < 40) push = Add(push, Scale(away, (40 - d) / (40 * d))); } if (Length(push) > 0.01f) enemy.Pos = MoveEnemy(enemy, Scale(Normalize(push, PointF.Empty), 52 * dt)); }
    private void MovePlayer(PointF delta) { PointF x = new(player.X + delta.X, player.Y), y = new(player.X, player.Y + delta.Y); if (!Blocked(x)) player.X = x.X; if (!Blocked(new PointF(player.X, y.Y))) player.Y = y.Y; player.X = Math.Clamp(player.X, 85, 875); player.Y = Math.Clamp(player.Y, 128, 533); }
    private RectangleF[] CurrentRoomWalls() => room switch
    {
        3 => TownBuildings, 4 => ArchiveWalls, 5 => VaultWalls, 6 => RiverWalls, 7 => BellChamberWalls, 8 => AshGalleryWalls, 9 => LumeChamberWalls, 10 => NameShoreWalls, 11 => VoiceWellWalls, 12 => FarReachWalls, 13 => NameCloisterWalls, 14 => FirstBasinWalls, 15 => FirstVigilWalls, 16 => WestFordWalls, 17 => WitnessYardWalls,
        18 => RefugeWalls, 19 => RidgeWalls, 20 => LedgerWalls, 21 => SaltMarshWalls, 22 => TideBridgeWalls, 23 => ReedRunWalls, 24 => ReedYardWalls, 25 => FloodMillWalls, 26 => SiltChamberWalls, 27 => CounterSlopeWalls, 28 => CounterHeartWalls, 29 => CantoriaWalls, 30 => RiftChamberWalls, 31 => GlassGorgeWalls, 32 => MirrorSourceWalls, 33 => LanternSpanWalls, 34 => LowTideObservatoryWalls, 35 => OpenRouteWalls, 36 => UnwrittenHallWalls, 37 => SilenceThresholdWalls, 38 => SilenceChamberWalls, 39 => BorrowedDockWalls, 40 => BorrowedArchiveWalls,
        _ => Walls
    };
    private bool Blocked(PointF p, float radius = 12) { RectangleF body = new(p.X - radius, p.Y - radius, radius * 2, radius * 2); return CurrentRoomWalls().Any(w => w.IntersectsWith(body)); }
    private bool BlockedLine(PointF a, PointF b)
    {
        int steps = Math.Max(1, (int)(Distance(a, b) / 6));
        RectangleF[] obstacles = CurrentRoomWalls();
        for (int i = 0; i <= steps; i++) { float t = i / (float)steps; PointF p = new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t); if (obstacles.Any(w => w.Contains(p))) return true; }
        return false;
    }

    private SaveData CaptureSaveData() => new() { Room = room, Level = level, Xp = xp, Gold = gold, Potions = potions, Damage = damage, Health = health, MaxHealth = maxHealth, Weapon = weapon, OwnedWeapons = ownedWeapons, Armor = armor, Relic = relic, HealthTalent = healthTalent, SkillTalent = skillTalent, DodgeTalent = dodgeTalent, TechniqueTalent = techniqueTalent, RoomCleared = room == 3 || enemies.Count == 0 && drops.Count == 0, Completed = victory, MemoriesFound = memoriesFound, QuestStage = questStage, ClearedRooms = clearedRooms | (room != 3 && enemies.Count == 0 && drops.Count == 0 ? 1L << room : 0), ArchiveSeal = archiveSeal, RiverBell = riverBell, LumeLens = lumeLens, NaraTrace = naraTrace, ChorusMemory = chorusMemory, FirstMemory = firstMemory, WestTrace = westTrace, UpstreamTrace = upstreamTrace, TideGlass = tideGlass, BeyondTrace = beyondTrace, EbbSeal = ebbSeal, CounterTrace = counterTrace, AnchorWard = anchorWard, RefrainSong = refrainSong, RiftVerse = riftVerse, DeepTrace = deepTrace, LanternSignal = lanternSignal, BeaconLight = beaconLight, RouteMemory = routeMemory, SilenceMemory = silenceMemory, BorrowedTrace = borrowedTrace, FinalEnding = finalEnding, VigilBestFloor = vigilBestFloor, HighContrastTelegraphs = highContrastTelegraphs, OwnedCharms = ownedCharms, EquippedCharm = equippedCharm, EquippedCharm2 = equippedCharm2, EquippedSkills = equippedSkills.ToArray(), TownEnding = townEnding, WeaponBonuses = weaponBonuses.ToArray(), WeaponRarities = weaponRarities.ToArray(), WeaponAffixes = weaponAffixes.ToArray(), WeaponAffixes2 = weaponAffixes2.ToArray() };
    private static void WriteSaveFile(string path, string contents)
    {
        string directory = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(directory);
        string temporary = Path.Combine(directory, $".save-{Guid.NewGuid():N}.tmp");
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream)) { writer.Write(contents); writer.Flush(); stream.Flush(true); }
            File.Move(temporary, path, true);
        }
        finally
        {
            try { File.Delete(temporary); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }
    private bool SaveGame(string? destination = null)
    {
        try { WriteSaveFile(destination ?? SavePath, JsonSerializer.Serialize(CaptureSaveData())); notice = "PROGRESSO SALVO"; noticeTime = 3; return true; }
        catch (IOException) { notice = "NÃO FOI POSSÍVEL SALVAR • VERIFIQUE O ARQUIVO"; }
        catch (UnauthorizedAccessException) { notice = "NÃO FOI POSSÍVEL SALVAR • VERIFIQUE AS PERMISSÕES"; }
        noticeTime = 3; return false;
    }
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
    private static bool ValidSaveData(SaveData s) => !(
            !ValidWeaponData(s)
            || !ValidSkillLoadout(s)
            || s.Room is < 0 or > 40
            || s.Level is < 1 or > 10000
            || s.Xp < 0
            || s.Xp >= s.Level * 60
            || s.Weapon is < 0 or > 2
            || s.OwnedWeapons < 1
            || s.OwnedWeapons > 7
            || (s.OwnedWeapons & (1 << s.Weapon)) == 0
            || s.MaxHealth < 1
            || s.Health <= 0
            || s.Health > s.MaxHealth
            || s.Potions < 0
            || s.Gold < 0
            || s.TideGlass is < 0 or > 999
            || s.Damage < 1
            || s.TechniqueTalent is < 0 or > 10000
            || s.MemoriesFound is < 0 or > 3
            || s.QuestStage is < 0 or > 56
            || s.ClearedRooms < 0
            || s.ClearedRooms > 2199023255551L
            || s.Completed && s.Room != 2
            || s.OwnedCharms is < 0 or > 255
            || s.EquippedCharm is < 0 or > 8
            || s.EquippedCharm2 is < 0 or > 8
            || s.EquippedCharm > 0 && (s.OwnedCharms & (1 << (s.EquippedCharm - 1))) == 0
            || s.EquippedCharm2 > 0 && (s.OwnedCharms & (1 << (s.EquippedCharm2 - 1))) == 0
            || s.EquippedCharm > 0 && s.EquippedCharm == s.EquippedCharm2
            || s.TownEnding is < 0 or > 2
            || s.QuestStage == 13 && s.TownEnding == 0
            || s.QuestStage >= 14 && !s.ChorusMemory
            || s.QuestStage >= 16 && !s.FirstMemory
            || s.QuestStage >= 19 && !s.WestTrace
            || s.WestTrace && s.QuestStage < 19
            || s.UpstreamTrace && s.QuestStage < 26
            || s.QuestStage >= 26 && !s.UpstreamTrace
            || s.BeyondTrace && s.QuestStage < 29
            || s.QuestStage >= 29 && !s.BeyondTrace
            || s.EbbSeal && s.QuestStage < 33
            || s.QuestStage >= 33 && !s.EbbSeal
            || s.CounterTrace && s.QuestStage < 35
            || s.QuestStage >= 35 && !s.CounterTrace
            || s.AnchorWard && s.QuestStage < 36
            || s.RiftVerse && s.QuestStage < 39
            || s.QuestStage >= 39 && !s.RiftVerse
            || s.RefrainSong && s.QuestStage < 40
            || s.QuestStage >= 40 && !s.RefrainSong
            || s.DeepTrace && s.QuestStage < 42
            || s.QuestStage >= 42 && !s.DeepTrace
            || s.LanternSignal && s.QuestStage < 45
            || s.QuestStage >= 45 && !s.LanternSignal
            || s.BeaconLight && s.QuestStage < 46
            || s.QuestStage >= 46 && !s.BeaconLight
            || s.RouteMemory && s.QuestStage < 48
            || s.QuestStage >= 48 && !s.RouteMemory
            || s.SilenceMemory && s.QuestStage < 51
            || s.QuestStage >= 51 && !s.SilenceMemory
            || s.BorrowedTrace && s.QuestStage < 55
            || s.QuestStage >= 55 && !s.BorrowedTrace
            || s.FinalEnding is < 0 or > 2
            || s.VigilBestFloor is < 0 or > 100000
            || s.VigilBestFloor > 0 && s.FinalEnding == 0
            || s.QuestStage == 52 && s.FinalEnding != 1
            || s.QuestStage == 53 && s.FinalEnding != 2
            || s.FinalEnding > 0 && s.QuestStage < 52
            || s.QuestStage >= 54 && s.FinalEnding == 0
            || s.Room >= 39 && s.QuestStage < 54
            || s.Room >= 37 && s.QuestStage < 50
            || s.Room >= 35 && s.QuestStage < 47
            || s.Room >= 33 && s.QuestStage < 44
            || s.Room >= 31 && s.QuestStage < 41
            || s.Room >= 29 && s.QuestStage < 37
            || s.Room >= 27 && s.QuestStage < 34
            || s.Room >= 25 && s.QuestStage < 31
            || s.Room >= 23 && s.QuestStage < 28
            || s.Room >= 21 && s.QuestStage < 25
            || s.Room >= 14 && s.QuestStage < 15
            || s.Room >= 16 && s.QuestStage < 18
            || s.Room >= 18 && s.QuestStage < 21
            || s.Room >= 19 && s.QuestStage < 22
            || s.Room == 20 && s.QuestStage < 22
        );

    private void LoadGame()
    {
        try
        {
            bool migrated = !File.Exists(SavePath); SaveData s = migrated ? ReadLegacySave() : JsonSerializer.Deserialize<SaveData>(File.ReadAllText(SavePath)) ?? throw new InvalidDataException();


            if (!ValidSaveData(s)) throw new InvalidDataException();
            StartGame(); room = s.Room; level = s.Level; xp = s.Xp; gold = s.Gold; potions = s.Potions; damage = s.Damage; health = s.Health; maxHealth = s.MaxHealth; weapon = s.Weapon; ownedWeapons = s.OwnedWeapons; RestoreWeaponData(s); armor = s.Armor; relic = s.Relic; healthTalent = s.HealthTalent; skillTalent = s.SkillTalent; dodgeTalent = s.DodgeTalent; techniqueTalent = s.TechniqueTalent; memoriesFound = s.MemoriesFound; questStage = s.QuestStage; clearedRooms = s.ClearedRooms | (s.RoomCleared && s.Room != 3 ? 1L << s.Room : 0); archiveSeal = s.ArchiveSeal; riverBell = s.RiverBell; lumeLens = s.LumeLens; naraTrace = s.NaraTrace; chorusMemory = s.ChorusMemory; firstMemory = s.FirstMemory; westTrace = s.WestTrace; upstreamTrace = s.UpstreamTrace; tideGlass = s.TideGlass; beyondTrace = s.BeyondTrace; ebbSeal = s.EbbSeal; counterTrace = s.CounterTrace; anchorWard = s.AnchorWard || s.QuestStage >= 36; riftVerse = s.RiftVerse; deepTrace = s.DeepTrace; lanternSignal = s.LanternSignal; beaconLight = s.BeaconLight; routeMemory = s.RouteMemory; silenceMemory = s.SilenceMemory; borrowedTrace = s.BorrowedTrace; finalEnding = s.FinalEnding; vigilBestFloor = s.VigilBestFloor; highContrastTelegraphs = s.HighContrastTelegraphs; refrainSong = s.RefrainSong; ownedCharms = s.OwnedCharms; equippedCharm = s.EquippedCharm; equippedCharm2 = s.EquippedCharm2; equippedSkills = s.EquippedSkills.ToArray(); townEnding = s.TownEnding; SetupRoom(); if (s.RoomCleared) enemies.Clear(); victory = s.Completed; title = false; storyOverlay = false;
            if (migrated && !SaveGame()) { notice = "SAVE ANTIGO CARREGADO • NÃO FOI POSSÍVEL MIGRAR"; noticeTime = 5; }
            else { notice = migrated ? "SAVE ANTIGO MIGRADO" : "PROGRESSO CARREGADO"; noticeTime = 2; }
        }
        catch { notice = "NENHUM SALVAMENTO VÁLIDO"; noticeTime = 2; }
    }

    private static RectangleF GameViewport(Size clientSize)
    {
        float scale = Math.Min(Math.Max(1, clientSize.Width) / 960f, Math.Max(1, clientSize.Height) / 640f);
        float width = 960 * scale, height = 640 * scale;
        return new RectangleF((clientSize.Width - width) / 2, (clientSize.Height - height) / 2, width, height);
    }
    private static PointF ScreenToGame(PointF point, Size clientSize)
    {
        RectangleF viewport = GameViewport(clientSize);
        return new PointF(Math.Clamp((point.X - viewport.X) * 960 / viewport.Width, 0, 960), Math.Clamp((point.Y - viewport.Y) * 640 / viewport.Height, 0, 640));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; g.Clear(Color.FromArgb(16, 19, 23));
        RectangleF viewport = GameViewport(ClientSize); g.TranslateTransform(viewport.X, viewport.Y); g.ScaleTransform(viewport.Width / 960, viewport.Height / 640);
        using var goldBrush = new SolidBrush(Color.FromArgb(230, 193, 129)); using var textBrush = new SolidBrush(Color.FromArgb(222, 216, 201));
        using var panel = new SolidBrush(Color.FromArgb(28, 32, 35)); using var floor = new SolidBrush(Color.FromArgb(54, 60, 53)); using var tile = new Pen(Color.FromArgb(70, 78, 68));
        using var headerFont = new Font("Georgia", 22, FontStyle.Bold); using var hudFont = new Font("Segoe UI", 12, FontStyle.Bold); using var roomFont = new Font("Georgia", 13, FontStyle.Bold); using var helpFont = new Font("Segoe UI", 10);
        g.FillRectangle(panel, 0, 0, 960, 83); g.DrawString("SCADRYK", headerFont, goldBrush, 24, 18);
        g.DrawString($"VIDA {health:0}/{maxHealth:0}     NÍVEL {level}     XP {xp}/{level * 60}     OURO {gold}     POÇÕES {potions}", hudFont, textBrush, 255, 29);
        g.DrawString($"F  {ShortSkillName(0)}     V  {ShortSkillName(1)}     G  {ShortSkillName(2)}", helpFont, goldBrush, 255, 61);
        if (armorOn && relicOn)
        {
            using var setBadge = new SolidBrush(Color.FromArgb(210, 31, 52, 53));
            using var setText = new SolidBrush(Color.FromArgb(236, 174, 215, 202));
            using var setFont = new Font("Segoe UI", 9, FontStyle.Bold);
            g.FillRectangle(setBadge, 604, 85, 291, 19);
            g.DrawString("VIGÍLIA DO VEIO  •  -35% DANO  •  CURA 8/ABATE", setFont, setText, 610, 87);
        }
        using var healthBack = new SolidBrush(Color.FromArgb(52, 56, 51)); using var healthBar = new SolidBrush(health / maxHealth < .25f ? Color.IndianRed : Color.FromArgb(112, 171, 123));
        g.FillRectangle(healthBack, 25, 58, 190, 11); g.FillRectangle(healthBar, 25, 58, 190 * health / maxHealth, 11);
        if (room == 3) DrawTownEnvironment(g, floor, tile);
        else if (room == 18) DrawRefugeEnvironment(g, floor, tile);
        else if (room is 19 or 20) DrawLedgerTrailEnvironment(g, floor, tile);
        else if (room is 21 or 22) DrawUpstreamEnvironment(g, floor, tile);
        else if (room is 23 or 24) DrawReedBankEnvironment(g, floor, tile);
        else if (room is 25 or 26) DrawFloodMillEnvironment(g, floor, tile);
        else if (room is 27 or 28) DrawCounterflowEnvironment(g, floor, tile);
        else if (room is 29 or 30) DrawCantoriaEnvironment(g, floor, tile);
        else if (room is 35 or 36) DrawRouteEnvironment(g, floor, tile);
        else if (room is 37 or 38) DrawFinalEnvironment(g, floor, tile);
        else if (room is 39 or 40) DrawBorrowedArchiveEnvironment(g, floor, tile);
        else if (room is 31 or 32) DrawGlassEnvironment(g, floor, tile);
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
        if (refrainWaveTime > 0) DrawRefrainWave(g); if (beaconTime > 0) DrawBeacon(g);
        foreach (EchoShard shard in echoShards) DrawEchoShard(g, shard);
        foreach (Enemy enemy in enemies) DrawEnemy(g, enemy);
        foreach (Drop item in drops) DrawDrop(g, item);
        DrawPlayer(g);
        if (attackFlash > 0) DrawAttackSwing(g);
        if (returnVoiceWaveTime > 0) DrawReturnedVoicesWave(g);
        if (finalStrikeFlash > 0) DrawFinalSparkFlash(g);
        DrawFloatingTexts(g);
        string roomTitle = vigilActive ? $"VIGÍLIA ASCENDENTE • ANDAR {vigilFloor}" : room == 3 || room == 18 ? roomNames[room] : room < 3 ? $"{room + 1} / 3   {roomNames[room]}" : room < 6 ? $"{room - 3} / 2   {roomNames[room]}" : room < 8 ? $"{room - 5} / 2   {roomNames[room]}" : room < 10 ? $"{room - 7} / 2   {roomNames[room]}" : room < 12 ? $"{room - 9} / 2   {roomNames[room]}" : room < 14 ? $"{room - 11} / 2   {roomNames[room]}" : room < 16 ? $"{room - 13} / 2   {roomNames[room]}" : room < 18 ? $"{room - 15} / 2   {roomNames[room]}" : room < 21 ? $"{room - 18} / 2   {roomNames[room]}" : room < 23 ? $"{room - 20} / 2   {roomNames[room]}" : room < 25 ? $"{room - 22} / 2   {roomNames[room]}" : room < 27 ? $"{room - 24} / 2   {roomNames[room]}" : room < 29 ? $"{room - 26} / 2   {roomNames[room]}" : room < 31 ? $"{room - 28} / 2   {roomNames[room]}" : room < 33 ? $"{room - 30} / 2   {roomNames[room]}" : room < 35 ? $"{room - 32} / 2   {roomNames[room]}" : room < 37 ? $"{room - 34} / 2   {roomNames[room]}" : room < 39 ? $"{room - 36} / 2   {roomNames[room]}" : $"{room - 38} / 2   {roomNames[room]}";
        g.DrawString(roomTitle, roomFont, goldBrush, 67, 572);
        int memoriesRead = (memoriesFound & 1) + ((memoriesFound >> 1) & 1);
        g.DrawString($"ECOS {memoriesRead} / 2", hudFont, goldBrush, 760, 572);
        if (room == 3) g.DrawString(TownObjective(), hudFont, goldBrush, 67, 592);
        else if (room == 18) g.DrawString("OBJETIVO: " + (questStage == 46 ? "FALE COM AVEN SOBRE AS LINHAS" : questStage == 47 ? "SIGA AS LINHAS ATÉ A GALERIA" : questStage == 48 ? "LEVE O TRAÇADO DE VOLTA A AVEN" : questStage == 49 ? "FALE COM AVEN PARA ABRIR O ÚLTIMO CAPÍTULO" : questStage == 50 ? "DESÇA À SOLEIRA DO SILÊNCIO" : questStage == 51 ? "FALE COM AVEN SOBRE O DESTINO DO LUME" : questStage >= 56 ? "EXPEDIÇÃO XVII CONCLUÍDA • EXPLORAÇÃO ABERTA" : questStage == 55 ? "LEVE A MEMÓRIA DO ARQUIVO A AVEN" : questStage == 54 ? "SIGA AO CAIS DOS ECOS EMPRESTADOS" : questStage >= 52 ? "EPÍLOGO CONCLUÍDO • EXPLORAÇÃO ABERTA" : questStage < 22 ? "CONHEÇA A COMUNIDADE E FALE COM AVEN" : questStage == 22 ? "SIGA A TRILHA E RECUPERE O REGISTRO COMPARTILHADO" : questStage == 23 ? "LEVE O REGISTRO DE VOLTA A AVEN" : questStage == 32 ? "LEVE A MEMÓRIA DA VAZANTE A AVEN" : questStage == 30 ? "SIGA O SOM DOS MOINHOS PARA OESTE" : questStage == 35 ? "LEVE A MEMÓRIA DO COMPASSO A AVEN" : questStage == 39 ? "LEVE A NOTA DA CONTRAVOZ A AVEN" : questStage == 40 ? "FALE COM AVEN SOBRE A PRÓXIMA FISSURA" : questStage == 42 ? "LEVE O ECO SEM DONO A AVEN" : questStage >= 41 ? "SIGA OS PASSOS ATÉ A FONTE SOB A PEDRA" : questStage >= 36 ? "MANTO DE LASTRO APRENDIDO • SIGA AS ÁGUAS" : questStage >= 33 ? "SIGA A CORRENTE AO CORAÇÃO DAS DUAS MARGENS" : questStage >= 27 ? "FALE COM AVEN PARA REFORJAR UMA ARMA" : "SIGA A PRÓXIMA CURVA A OESTE"), hudFont, goldBrush, 67, 592);
        else if (room is 27 or 28) g.DrawString(room == 27 ? "OBJETIVO: ATRAVESSE A ESCARPA DO CONTRAFLUXO" : questStage < 35 ? "OBJETIVO: DERROTE O AFINADOR E RECOLHA A MEMÓRIA" : "OBJETIVO: RETORNE AO REFÚGIO E FALE COM AVEN", hudFont, goldBrush, 67, 592);
        else if (room is 29 or 30) g.DrawString(room == 29 ? (enemies.Count > 0 || drops.Count > 0 ? "OBJETIVO: ABRA A CANTARIA E LIMPE OS ECOS" : "OBJETIVO: SIGA A CONTRAVOZ ATÉ A CÂMARA") : riftVerse ? "OBJETIVO: LEVE A NOTA RECUPERADA A AVEN" : "OBJETIVO: INTERROMPA A CONTRAVOZ E RECOLHA A NOTA", hudFont, goldBrush, 67, 592);
        else if (room is 33 or 34) g.DrawString(LanternObjective(), hudFont, goldBrush, 67, 592);
        else if (room is 35 or 36) g.DrawString(room == 35 ? (enemies.Count > 0 || drops.Count > 0 ? "OBJETIVO: ATRAVESSE AS LINHAS ABERTAS" : "OBJETIVO: SIGA ATÉ A GALERIA") : routeMemory ? "OBJETIVO: LEVE O TRAÇADO A AVEN" : "OBJETIVO: DERROTE O ESCRIBA E RECUPERE O TRAÇADO", hudFont, goldBrush, 67, 592);
        else if (room is 39 or 40) g.DrawString(room == 39 ? enemies.Count > 0 || drops.Count > 0 ? "OBJETIVO: ATRAVESSE O CAIS DOS ECOS" : "OBJETIVO: ENTRE NO ARQUIVO DA MARÉ" : borrowedTrace ? "OBJETIVO: RETORNE A AVEN COM A MEMÓRIA" : "OBJETIVO: DERROTE O ARQUIVISTA E RECOLHA O ECO", hudFont, goldBrush, 67, 592);
        else if (room is 37 or 38) g.DrawString(vigilActive ? enemies.Count > 0 ? $"VIGÍLIA {vigilFloor}: DERROTE O ECO E RECOLHA O SAQUE" : drops.Count > 0 ? $"VIGÍLIA {vigilFloor}: RECOLHA O SAQUE" : $"VIGÍLIA {vigilFloor}: INTERAJA COM O ALTAR PARA SUBIR OU ENCERRAR" : room == 37 ? enemies.Count > 0 || drops.Count > 0 ? "OBJETIVO: ATRAVESSE A SOLEIRA DO SILÊNCIO" : "OBJETIVO: DESÇA À CÂMARA DO ÚLTIMO ECO" : silenceMemory ? "OBJETIVO: RETORNE A AVEN E DECIDA O DESTINO DO LUME" : "OBJETIVO: DERROTE O SILÊNCIO E RECOLHA A MEMÓRIA", hudFont, goldBrush, 67, 592);
        else if (room is 31 or 32) g.DrawString(room == 31 ? (enemies.Count > 0 || drops.Count > 0 ? "OBJETIVO: ATRAVESSE A GARGANTA DO VIDRO MUDO" : "OBJETIVO: SIGA OS PASSOS ATÉ A FONTE") : deepTrace ? "OBJETIVO: LEVE O ECO SEM DONO A AVEN" : "OBJETIVO: DERROTE O ARAUTO E RECOLHA A LEMBRANÇA", hudFont, goldBrush, 67, 592);
        else if (room >= 4 && room < 6) g.DrawString(ArchiveObjective(), hudFont, goldBrush, 67, 592);
        else if (room is 6 or 7) g.DrawString(RiverObjective(), hudFont, goldBrush, 67, 592);
        else if (room >= 8) g.DrawString(LumeObjective(), hudFont, goldBrush, 67, 592);
        g.DrawString(room is 3 or 18 ? "WASD mover   E falar/investigar   F5 salvar   B técnicas   I inventário" : "WASD mover   ESPAÇO atacar   SHIFT esquivar   R redemoinho   F/V/G técnicas   B montar técnicas   Q poção   E interagir   I inventário", helpFont, textBrush, 67, 619);
        if (room < MemoryStonePositions.Length && (memoriesFound & (1 << room)) == 0) DrawMemoryStone(g, MemoryStonePositions[room], Distance(player, MemoryStonePositions[room]) < MemoryStoneInteractionRadius);
        if (noticeTime > 0) DrawCentered(g, notice, 170, new Font("Segoe UI", 13, FontStyle.Bold), Color.FromArgb(230, 193, 129));
        if (vigilChoiceOpen) DrawChoice(g, $"VIGÍLIA ASCENDENTE • ANDAR {vigilFloor}", $"1. SUBIR AO ANDAR {vigilFloor + 1} • RECOMPENSA CRESCENTE", $"2. ENCERRAR • MELHOR MARCA: {Math.Max(vigilBestFloor, vigilFloor)}", "3. FICAR AQUI");
        if (title || dead || victory) DrawOverlay(g, title ? "SCADRYK" : victory ? "VITÓRIA" : "VOCÊ CAIU", title ? "ENTER PARA COMEÇAR    •    C CARREGAR SAVE" : "ENTER PARA RECOMEÇAR    •    F5 SALVAR");
        if (inventoryOpen) DrawInventory(g);
        if (skillMenuOpen) DrawSkillMenu(g);
        if (characterSheetOpen) DrawCharacterSheet(g);
        if (talentChoice) DrawTalentChoice(g, pendingTalents);
        if (chestChoice) DrawChoice(g, "BAÚ DA CRIPTA — ESCOLHA UMA RECOMPENSA", "1. 75 OURO", "2. DUAS POÇÕES", "3. RECUPERAR TODA A VIDA");
        if (endingChoiceOpen) DrawChoice(g, questStage == 51 ? "O QUE VÉSPERA FARÁ COM O LUME?" : "O QUE VÉSPERA DEVE LEMBRAR?", questStage == 51 ? "1. DEVOLVER AS MEMÓRIAS  •  +20 VIDA MÁXIMA" : "1. DEVOLVER OS NOMES  •  +20 VIDA MÁXIMA", questStage == 51 ? "2. OFERECER UMA LEMBRANÇA PRÓPRIA  •  +10 DANO" : "2. MANTER O LUME  •  +10 DANO", "3. ADIAR A DECISÃO");
        if (forgeMenuOpen) DrawForgeMenu(g);
        if (storyOverlay) DrawStoryOverlay(g);
        if (memoryOverlay) DrawMemoryOverlay(g);
        if (mapOpen) DrawMapOverlay(g);
        if (focusPaused && !title && !dead && !victory) DrawOverlay(g, "PAUSADO", "VOLTE À JANELA PARA CONTINUAR");
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
        DrawTownPerson(g, CuratorPosition, "SILETH", Color.FromArgb(117, 73, 104), Distance(player, CuratorPosition) < InteractionRadius);
        DrawTownPerson(g, BellKeeperPosition, "EDDA", Color.FromArgb(111, 137, 124), Distance(player, BellKeeperPosition) < InteractionRadius);
        if (Distance(player, ArchiveBookPosition) < InteractionRadius) DrawCentered(g, "E  LER", ArchiveBookPosition.Y + 22, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, ArchiveBookPosition.X);
        if (Distance(player, CryptGatePosition) < InteractionRadius) DrawCentered(g, "E  CRIPTA", CryptGatePosition.Y + 42, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, CryptGatePosition.X + 15);
        PointF archiveGate = new(480, 135);
        using var portal = new Pen(questStage >= 3 ? Color.FromArgb(219, 178, 112) : Color.FromArgb(88, 75, 61), 5);
        g.DrawArc(portal, archiveGate.X - 28, archiveGate.Y - 24, 56, 52, 180, 180);
        DrawCentered(g, questStage >= 3 ? "ARQUIVO NORTE" : "PORTÃO SELADO", archiveGate.Y - 38, new Font("Segoe UI", 8, FontStyle.Bold), Color.FromArgb(230, 193, 129), archiveGate.X);
        if (Distance(player, archiveGate) < InteractionRadius) DrawCentered(g, "E  ENTRAR", archiveGate.Y + 28, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, archiveGate.X);
        PointF riverGate = new(480, 520);
        using var riverPortal = new Pen(questStage >= 5 ? Color.FromArgb(112, 179, 185) : Color.FromArgb(72, 85, 82), 5);
        g.DrawArc(riverPortal, riverGate.X - 28, riverGate.Y - 22, 56, 50, 0, 180);
        DrawCentered(g, questStage >= 7 ? "ESCADA DO LUME" : questStage >= 5 ? "RIO CEGO" : "LEITO SELADO", riverGate.Y - 4, new Font("Segoe UI", 8, FontStyle.Bold), Color.FromArgb(190, 211, 196), riverGate.X);
        if (Distance(player, riverGate) < InteractionRadius) DrawCentered(g, "E  DESCER", riverGate.Y - 38, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, riverGate.X);
    }

    private string ArchiveObjective() => room == 4 ? "OBJETIVO: CRUZE A GALERIA E ENCONTRE O COFRE" : archiveSeal ? "OBJETIVO: VOLTE A VÉSPERA COM O SELO" : "OBJETIVO: RECUPERE O REGISTRO NO COFRE";
    private string RiverObjective() => room == 6 ? "OBJETIVO: SIGA A CORRENTE ATÉ A CAPELA" : riverBell ? "OBJETIVO: RETORNE E MOSTRE O SINO A EDDA" : "OBJETIVO: ENCONTRE O SINO DE TRAVESSIA";
    private string LanternObjective() => room == 33 ? enemies.Count > 0 || drops.Count > 0 ? "OBJETIVO: ATRAVESSE AS LANTERNAS BAIXAS" : "OBJETIVO: SIGA ATÉ O OBSERVATÓRIO" : lanternSignal ? "OBJETIVO: LEVE O SINAL DA LUZ A AVEN" : "OBJETIVO: DERROTE O REGENTE E RECUPERE O SINAL";
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
        using var deep = new SolidBrush(finalEnding == 1 ? Color.FromArgb(25, 48, 43) : finalEnding == 2 ? Color.FromArgb(35, 39, 46) : Color.FromArgb(24, 37, 39)); using var wood = new SolidBrush(Color.FromArgb(93, 83, 61));
        using var edge = new Pen(Color.FromArgb(165, 153, 116), 2); using var path = new Pen(Color.FromArgb(121, 135, 103), 8);
        g.FillRectangle(deep, 65, 108, 830, 445); g.FillRectangle(ground, 94, 132, 772, 397);
        for (int y = 0; y < 7; y++) for (int x = 0; x < 12; x++) g.DrawRectangle(tile, 70 + x * 68, 112 + y * 62, 67, 61);
        foreach (RectangleF wall in RefugeWalls) { g.FillRectangle(wood, wall); g.DrawRectangle(edge, wall.X, wall.Y, wall.Width, wall.Height); }
        g.DrawLine(path, 302, 330, 412, 330); g.DrawLine(path, 550, 330, 662, 330);
        using var table = new SolidBrush(Color.FromArgb(119, 99, 73)); using var ledger = new SolidBrush(Color.FromArgb(47, 62, 58));
        g.FillEllipse(table, 427, 292, 106, 75); g.FillRectangle(ledger, 458, 309, 44, 27); g.DrawEllipse(edge, 427, 292, 106, 75);
        DrawTownPerson(g, AvenPosition, "AVEN", Color.FromArgb(88, 112, 102), Distance(player, AvenPosition) < InteractionRadius);
        DrawTownPerson(g, SeraPosition, "SERA", Color.FromArgb(144, 103, 78), Distance(player, SeraPosition) < InteractionRadius);
        DrawTownPerson(g, NoaPosition, "NOA", Color.FromArgb(102, 111, 145), Distance(player, NoaPosition) < InteractionRadius);
        using var gate = new Pen(finalEnding == 1 ? Color.FromArgb(170, 211, 145) : finalEnding == 2 ? Color.FromArgb(210, 174, 106) : Color.FromArgb(111, 191, 163), 5); g.DrawLine(gate, 75, 296, 75, 364);
        using var altarGlow = new SolidBrush(finalEnding == 0 ? Color.FromArgb(65, 89, 94) : Color.FromArgb(105, 157, 186, 165)); using var altarEdge = new Pen(finalEnding == 0 ? Color.FromArgb(105, 114, 112) : Color.FromArgb(212, 188, 129), 3);
        g.FillEllipse(altarGlow, VigilAltarPosition.X - 30, VigilAltarPosition.Y - 30, 60, 60); g.DrawEllipse(altarEdge, VigilAltarPosition.X - 30, VigilAltarPosition.Y - 30, 60, 60); g.DrawEllipse(altarEdge, VigilAltarPosition.X - 17, VigilAltarPosition.Y - 17, 34, 34);
        if (Distance(player, VigilAltarPosition) < InteractionRadius) DrawCentered(g, finalEnding == 0 ? "VIGÍLIA BLOQUEADA • CONCLUA A CAMPANHA" : $"E  VIGÍLIA ASCENDENTE • MELHOR ANDAR {vigilBestFloor}", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, (int)VigilAltarPosition.X);
        using var trail = new Pen(questStage >= 22 ? Color.FromArgb(179, 208, 151) : Color.FromArgb(70, 83, 77), 5); g.DrawLine(trail, 885, 296, 885, 364);
        if (questStage >= 27 && Distance(player, AvenPosition) < InteractionRadius) DrawCentered(g, "E  TÊMPERA • 75 OURO + 1 SAL VÍTREO", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.LightCyan, 490);
        if (player.X < 150) DrawCentered(g, "E  VOLTAR A VÉSPERA", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 146);
        if (questStage >= 22 && player.X > 790) DrawCentered(g, questStage >= 54 ? "E  OUVIR O ARQUIVO DAS MARÉS" : questStage >= 50 ? "E  DESCER AO SILÊNCIO" : questStage >= 47 ? "E  SEGUIR AS LINHAS" : questStage >= 41 ? "E  OUVIR O VIDRO" : questStage >= 30 ? "E  SEGUIR AS ÁGUAS" : "E  SEGUIR AS LANTERNAS", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
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

    private void DrawCantoriaEnvironment(Graphics g, Brush ground, Pen tile)
    {
        using var deep = new SolidBrush(Color.FromArgb(24, 21, 31)); using var stone = new SolidBrush(Color.FromArgb(86, 77, 83));
        using var echo = new SolidBrush(Color.FromArgb(70, 167, 107, 197)); using var edge = new Pen(Color.FromArgb(177, 172, 145), 2);
        using var chord = new Pen(Color.FromArgb(150, 201, 164, 221), 3); using var path = new Pen(Color.FromArgb(188, 165, 142, 116), 5);
        g.FillRectangle(deep, 65, 108, 830, 445); g.FillRectangle(ground, 94, 132, 772, 397);
        for (int y = 0; y < 7; y++) for (int x = 0; x < 12; x++) g.DrawRectangle(tile, 70 + x * 68, 112 + y * 62, 67, 61);
        if (room == 29)
        {
            g.DrawLine(path, 95, 365, 356, 365); g.DrawLine(path, 602, 277, 865, 277);
            for (int i = 0; i < 9; i++) { float x = 152 + i * 79, sway = MathF.Sin(windupPulse * 1.7f + i) * 12; g.DrawLine(chord, x, 200 + sway, x, 298 + sway); g.DrawLine(chord, x + 24, 385 - sway, x + 24, 478 - sway); }
            using var stoneEdge = new Pen(Color.FromArgb(198, 192, 162), 2);
            for (int i = 0; i < 6; i++) { float x = 164 + i * 116, y = 295 + MathF.Sin(windupPulse + i) * 4; g.DrawEllipse(stoneEdge, x - 11, y - 11, 22, 22); }
        }
        else
        {
            float pulse = 1 + MathF.Sin(windupPulse * 2.2f) * .04f;
            g.FillEllipse(echo, 480 - 150 * pulse, 330 - 150 * pulse, 300 * pulse, 300 * pulse);
            g.DrawEllipse(chord, 330, 180, 300, 300); g.DrawEllipse(edge, 375, 225, 210, 210);
            for (int i = 0; i < 3; i++) { float y = 228 + i * 102; g.DrawLine(chord, 385, y, 575, y); g.DrawLine(chord, 385, y + 12, 575, y + 12); }
            DrawCentered(g, "TRÊS MARCAS • ATAQUE EM LINHA", 494, new Font("Segoe UI", 9, FontStyle.Bold), Color.FromArgb(222, 202, 226));
        }
        RectangleF[] walls = room == 29 ? CantoriaWalls : RiftChamberWalls;
        foreach (RectangleF wall in walls) { g.FillRectangle(stone, wall); g.DrawRectangle(edge, wall.X, wall.Y, wall.Width, wall.Height); }
        using var gate = new Pen(enemies.Count == 0 && drops.Count == 0 ? Color.LightGreen : Color.FromArgb(73, 82, 79), 7);
        g.DrawLine(gate, 885, 290, 885, 370); g.DrawLine(gate, 75, 290, 75, 370);
        if (player.X < 150) DrawCentered(g, room == 29 ? "E  VOLTAR À ESCARPA" : "E  VOLTAR À CANTARIA", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 146);
        if (room == 29 && player.X > 790 && enemies.Count == 0 && drops.Count == 0) DrawCentered(g, "E  ENTRAR NA CÂMARA", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
        if (room == 30 && player.X > 790 && questStage >= 39) DrawCentered(g, "E  VOLTAR A AVEN", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
    }

    private void DrawGlassEnvironment(Graphics g, Brush ground, Pen tile)
    {
        using var deep = new SolidBrush(Color.FromArgb(18, 26, 34));
        using var glass = new SolidBrush(Color.FromArgb(70, 103, 119));
        using var edge = new Pen(Color.FromArgb(165, 178, 184), 2);
        using var seam = new Pen(Color.FromArgb(130, 174, 207, 216), 2);
        g.FillRectangle(deep, 65, 108, 830, 445); g.FillRectangle(ground, 94, 132, 772, 397);
        for (int y = 0; y < 7; y++) for (int x = 0; x < 12; x++) g.DrawRectangle(tile, 70 + x * 68, 112 + y * 62, 67, 61);
        RectangleF[] walls = room == 31 ? GlassGorgeWalls : MirrorSourceWalls;
        foreach (RectangleF wall in walls) { g.FillRectangle(glass, wall); g.DrawRectangle(edge, wall.X, wall.Y, wall.Width, wall.Height); }
        for (int i = 0; i < 7; i++) { float x = 150 + i * 105; float wave = MathF.Sin(windupPulse * 1.1f + i) * 8; g.DrawLine(seam, x, 175 + wave, x + 24, 224 + wave); g.DrawLine(seam, x + 24, 224 + wave, x + 6, 277 + wave); }
        if (room == 32) { using var spring = new Pen(Color.FromArgb(175, 154, 224, 225), 4); g.DrawEllipse(spring, 365, 213, 230, 230); g.DrawEllipse(seam, 402, 250, 156, 156); DrawCentered(g, "PASSOS SEM NOME • SEM DESTINO", 481, new Font("Segoe UI", 9, FontStyle.Bold), Color.LightCyan); }
        using var gate = new Pen(enemies.Count == 0 && drops.Count == 0 ? Color.LightGreen : Color.FromArgb(73, 82, 79), 7);
        g.DrawLine(gate, 885, 290, 885, 370); g.DrawLine(gate, 75, 290, 75, 370);
        if (player.X < 150) DrawCentered(g, room == 31 ? "E  VOLTAR AO REFÚGIO" : "E  VOLTAR À GARGANTA", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 146);
        if (room == 31 && player.X > 790 && enemies.Count == 0 && drops.Count == 0) DrawCentered(g, "E  SEGUIR ATÉ A FONTE", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
        if (room == 32 && player.X > 790 && questStage >= 42) DrawCentered(g, "E  VOLTAR A AVEN", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
    }

    private void DrawRouteEnvironment(Graphics g, Brush ground, Pen tile)
    {
        using var deep = new SolidBrush(Color.FromArgb(17, 26, 31)); using var stone = new SolidBrush(Color.FromArgb(74, 82, 77));
        using var edge = new Pen(Color.FromArgb(181, 179, 157, 129), 2); using var route = new Pen(Color.FromArgb(155, 195, 176, 135), 3);
        using var pale = new Pen(Color.FromArgb(120, 155, 195, 186), 2); using var ink = new SolidBrush(Color.FromArgb(88, 44, 55, 61));
        g.FillRectangle(deep, 65, 108, 830, 445); g.FillRectangle(ground, 94, 132, 772, 397);
        for (int y = 0; y < 7; y++) for (int x = 0; x < 12; x++) g.DrawRectangle(tile, 70 + x * 68, 112 + y * 62, 67, 61);
        RectangleF[] walls = room == 35 ? OpenRouteWalls : UnwrittenHallWalls;
        foreach (RectangleF wall in walls) { g.FillRectangle(stone, wall); g.DrawRectangle(edge, wall.X, wall.Y, wall.Width, wall.Height); }
        if (room == 35)
        {
            g.DrawLine(route, 96, 330, 250, 330); g.DrawLine(route, 250, 330, 340, 226); g.DrawLine(route, 250, 330, 340, 440);
            g.DrawLine(pale, 340, 226, 496, 226); g.DrawLine(pale, 340, 440, 496, 440); g.DrawLine(route, 496, 226, 624, 330); g.DrawLine(route, 496, 440, 624, 330); g.DrawLine(pale, 624, 330, 862, 330);
            g.DrawLine(edge, 340, 226, 340, 257); g.DrawLine(edge, 340, 440, 340, 409); g.DrawLine(edge, 624, 330, 624, 298); g.DrawLine(edge, 624, 330, 624, 362);
            DrawCentered(g, "MUITOS CAMINHOS • NENHUM NOME", 479, new Font("Segoe UI", 9, FontStyle.Bold), Color.FromArgb(212, 205, 179));
        }
        else
        {
            g.FillEllipse(ink, 333, 183, 294, 294); g.DrawEllipse(route, 333, 183, 294, 294); g.DrawEllipse(pale, 370, 220, 220, 220); g.DrawEllipse(edge, 414, 264, 132, 132);
            for (int i = 0; i < 8; i++) { float angle = i * MathF.PI / 4 + windupPulse * 0.08f; float x1 = 480 + MathF.Cos(angle) * 80, y1 = 330 + MathF.Sin(angle) * 80; float x2 = 480 + MathF.Cos(angle + 0.2f) * 142, y2 = 330 + MathF.Sin(angle + 0.2f) * 142; g.DrawLine(pale, x1, y1, x2, y2); }
            DrawCentered(g, "A ROTA NÃO É UMA ORDEM", 487, new Font("Segoe UI", 9, FontStyle.Bold), Color.LightCyan);
        }
        using var gate = new Pen(enemies.Count == 0 && drops.Count == 0 ? Color.LightGreen : Color.FromArgb(73, 82, 79), 7);
        g.DrawLine(gate, 885, 290, 885, 370); g.DrawLine(gate, 75, 290, 75, 370);
        if (player.X < 150) DrawCentered(g, room == 35 ? "E  VOLTAR AO OBSERVATÓRIO" : "E  VOLTAR À TRILHA", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 146);
        if (room == 35 && player.X > 790 && enemies.Count == 0 && drops.Count == 0) DrawCentered(g, "E  ENTRAR NA GALERIA", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
        if (room == 36 && player.X > 790 && questStage >= 48 && enemies.Count == 0 && drops.Count == 0) DrawCentered(g, "E  LEVAR O TRAÇADO A AVEN", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
    }

    private void DrawFinalEnvironment(Graphics g, Brush ground, Pen tile)
    {
        using var deep = new SolidBrush(Color.FromArgb(12, 17, 24)); using var stone = new SolidBrush(Color.FromArgb(54, 63, 67));
        using var edge = new Pen(Color.FromArgb(162, 132, 157, 165), 2); using var fissure = new Pen(Color.FromArgb(170, 129, 170, 180), 3);
        Color ringColor = vigilActive ? VigilAspectColor(vigilFloor) : Color.FromArgb(173, 193, 194);
        using var hush = new Pen(Color.FromArgb(105, ringColor), 2); using var warning = new Pen(Color.FromArgb(220, 202, 104, 93), 3);
        g.FillRectangle(deep, 65, 108, 830, 445); g.FillRectangle(ground, 94, 132, 772, 397);
        for (int y = 0; y < 7; y++) for (int x = 0; x < 12; x++) g.DrawRectangle(tile, 70 + x * 68, 112 + y * 62, 67, 61);
        RectangleF[] walls = room == 37 ? SilenceThresholdWalls : SilenceChamberWalls;
        foreach (RectangleF wall in walls) { g.FillRectangle(stone, wall); g.DrawRectangle(edge, wall.X, wall.Y, wall.Width, wall.Height); }
        if (room == 37)
        {
            for (int i = 0; i < 7; i++) { float y = 178 + i * 47; g.DrawLine(fissure, 104, y, 250 + i * 17, y + ((i % 2) == 0 ? 30 : -30)); }
            DrawCentered(g, "AS VOZES NÃO PRECISAM RESPONDER", 492, new Font("Segoe UI", 9, FontStyle.Bold), Color.FromArgb(202, 207, 196));
        }
        else
        {
            float pulse = 1 + 0.04f * MathF.Sin(windupPulse * 3);
            g.DrawEllipse(hush, 480 - 180 * pulse, 330 - 180 * pulse, 360 * pulse, 360 * pulse);
            g.DrawEllipse(fissure, 365, 215, 230, 230); g.DrawEllipse(edge, 420, 270, 120, 120);
            for (int i = 0; i < 8; i++) { float angle = i * MathF.PI / 4 + windupPulse * 0.12f; float x = 480 + MathF.Cos(angle) * 168, y = 330 + MathF.Sin(angle) * 168; g.DrawLine(fissure, x, y, x - MathF.Cos(angle) * 34, y - MathF.Sin(angle) * 34); }
            if (vigilActive)
            {
                using var altar = new Pen(Color.FromArgb(180, 202, 170, 112), 3);
                g.DrawEllipse(altar, VigilArenaAltarPosition.X - 36, VigilArenaAltarPosition.Y - 36, 72, 72);
                g.DrawEllipse(altar, VigilArenaAltarPosition.X - 24, VigilArenaAltarPosition.Y - 24, 48, 48);
                if (VigilAspectForFloor(vigilFloor) == VigilAspect.Chorus)
                {
                    using var driftingEcho = new Pen(Color.FromArgb(135, 194, 133, 222), 3);
                    for (int i = 0; i < 3; i++) { float angle = windupPulse * 0.42f + i * MathF.PI * 2 / 3; float x = 480 + MathF.Cos(angle) * 222, y = 330 + MathF.Sin(angle) * 148; g.DrawEllipse(driftingEcho, x - 9, y - 9, 18, 18); }
                }
                DrawCentered(g, enemies.Count == 0 && drops.Count == 0 ? "E  SUBIR OU ENCERRAR A VIGÍLIA" : $"ECO DA VIGÍLIA • ANDAR {vigilFloor}", 494, new Font("Segoe UI", 9, FontStyle.Bold), Color.FromArgb(222, 202, 226));
                DrawCentered(g, $"{VigilAspectTitle(vigilFloor)} • {VigilAspectRule(vigilFloor).ToUpperInvariant()}", 511, new Font("Segoe UI", 9, FontStyle.Bold), VigilAspectColor(vigilFloor));
            }
            if (!vigilActive && enemies.FirstOrDefault(e => e.Kind == EnemyKind.DeepSilence) is Enemy boss && boss.Windup > 0)
            {
                if (boss.BossPattern == 0) { g.DrawEllipse(warning, boss.Pos.X - 90, boss.Pos.Y - 90, 180, 180); }
                else if (boss.BossPattern == 1) { g.DrawEllipse(warning, boss.Target.X - 100, boss.Target.Y - 100, 200, 200); }
                else g.DrawLine(warning, boss.Pos, boss.Target);
            }
            if (!vigilActive) DrawCentered(g, "O SILÊNCIO DEVOLVE O QUE O LUME PRENDEU", 493, new Font("Segoe UI", 9, FontStyle.Bold), Color.LightCyan);
        }
        using var gate = new Pen(enemies.Count == 0 && drops.Count == 0 ? Color.LightGreen : Color.FromArgb(73, 82, 79), 7);
        g.DrawLine(gate, 885, 290, 885, 370); g.DrawLine(gate, 75, 290, 75, 370);
        if (player.X < 150) DrawCentered(g, room == 37 ? "E  VOLTAR AO REFÚGIO" : "E  VOLTAR À SOLEIRA", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 146);
        if (room == 37 && player.X > 790 && enemies.Count == 0 && drops.Count == 0) DrawCentered(g, "E  ENTRAR NA CÂMARA", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
        if (room == 38 && player.X > 790 && silenceMemory) DrawCentered(g, "E  VOLTAR A AVEN", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
    }

    private void DrawBorrowedArchiveEnvironment(Graphics g, Brush ground, Pen tile)
    {
        using var deep = new SolidBrush(Color.FromArgb(9, 23, 27)); using var stone = new SolidBrush(Color.FromArgb(39, 66, 68));
        using var edge = new Pen(Color.FromArgb(170, 117, 183, 176), 2); using var current = new Pen(Color.FromArgb(92, 119, 203, 188), 3);
        g.FillRectangle(deep, 65, 108, 830, 445); g.FillRectangle(ground, 94, 132, 772, 397);
        for (int y = 0; y < 7; y++) for (int x = 0; x < 12; x++) g.DrawRectangle(tile, 70 + x * 68, 112 + y * 62, 67, 61);
        RectangleF[] walls = room == 39 ? BorrowedDockWalls : BorrowedArchiveWalls;
        foreach (RectangleF wall in walls) { g.FillRectangle(stone, wall); g.DrawRectangle(edge, wall.X, wall.Y, wall.Width, wall.Height); }
        for (int i = 0; i < 5; i++) { float y = 190 + i * 68; g.DrawArc(current, 250, y - 24, 460, 48, 190, 160); }
        g.DrawEllipse(edge, 414, 264, 132, 132); g.DrawEllipse(current, 390, 240, 180, 180);
        using var gate = new Pen(enemies.Count == 0 && drops.Count == 0 ? Color.LightGreen : Color.FromArgb(73, 82, 79), 7);
        g.DrawLine(gate, 885, 290, 885, 370); g.DrawLine(gate, 75, 290, 75, 370);
        if (room == 39 && player.X < 150) DrawCentered(g, "E  VOLTAR AO REFÚGIO", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 146);
        if (room == 39 && player.X > 790 && enemies.Count == 0 && drops.Count == 0) DrawCentered(g, "E  ENTRAR NO ARQUIVO", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
        if (room == 40 && player.X < 150) DrawCentered(g, "E  VOLTAR AO CAIS", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 146);
        if (room == 40 && player.X > 790 && questStage >= 55) DrawCentered(g, "E  VOLTAR A AVEN", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
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
        float r = enemy.Radius; Color color = enemy.Kind == EnemyKind.BlindScribe ? Color.FromArgb(89, 128, 135) : enemy.Kind == EnemyKind.RiverFerryman ? Color.FromArgb(69, 120, 124) : enemy.Kind == EnemyKind.AshWarden ? Color.FromArgb(169, 91, 53) : enemy.Kind == EnemyKind.HollowName ? Color.FromArgb(120, 165, 155) : enemy.Kind == EnemyKind.NameWeaver ? Color.FromArgb(139, 108, 162) : enemy.Kind == EnemyKind.FirstKeeper ? Color.FromArgb(202, 173, 112) : enemy.Kind == EnemyKind.OathCartographer ? Color.FromArgb(96, 148, 144) : enemy.Kind == EnemyKind.NameBroker ? Color.FromArgb(126, 82, 132) : enemy.Kind == EnemyKind.ReedKeeper ? Color.FromArgb(158, 149, 98) : enemy.Kind == EnemyKind.FlowKeeper ? Color.FromArgb(71, 148, 164) : enemy.Kind == EnemyKind.TideWitness ? Color.FromArgb(63, 157, 168) : enemy.Kind == EnemyKind.GlassHerald ? Color.FromArgb(124, 204, 218) : enemy.Kind == EnemyKind.LanternRegent ? Color.FromArgb(194, 195, 144) : enemy.Kind == EnemyKind.RouteScribe ? Color.FromArgb(159, 182, 211) : enemy.Kind == EnemyKind.DeepSilence ? Color.FromArgb(119, 183, 190) : enemy.Kind == EnemyKind.SaltEcho ? Color.LightSteelBlue : enemy.Kind == EnemyKind.PaperWraith ? Color.FromArgb(201, 190, 161) : enemy.Kind == EnemyKind.MudCantor ? Color.FromArgb(165, 135, 104) : enemy.Boss ? Color.FromArgb(145, 87, 77) : enemy.Kind switch { EnemyKind.Elite => enemy.EliteAffix switch { EliteAffix.Armored => Color.SlateGray, EliteAffix.Swift => Color.Goldenrod, EliteAffix.Bloodbound => Color.MediumVioletRed, _ => Color.IndianRed }, EnemyKind.Archer => Color.FromArgb(128, 151, 106), EnemyKind.Mage => Color.MediumPurple, EnemyKind.InkEcho => Color.FromArgb(113, 98, 143), EnemyKind.DrownedMemory => Color.FromArgb(95, 145, 142), EnemyKind.EmberWisp => Color.FromArgb(212, 126, 61), EnemyKind.NameMoth => Color.FromArgb(126, 170, 156), _ => Color.FromArgb(183, 181, 166) };
        if (enemy.Stagger > 0) { using var staggerPen = new Pen(Color.FromArgb(200, 125, 211, 203), 2); float pulse = 1 + (0.95f - enemy.Stagger) * 7; g.DrawEllipse(staggerPen, enemy.Pos.X - r - pulse, enemy.Pos.Y - r - pulse, (r + pulse) * 2, (r + pulse) * 2); }
        if (enemy.Windup > 0 && enemy.Boss) DrawBossWarning(g, enemy);
        else if (enemy.Windup > 0)
        {
            PointF center = NonBossAttackCenter(enemy); float radius = NonBossAttackRadius(enemy);
            bool spearboundElite = enemy.Kind == EnemyKind.Elite && enemy.EliteAffix == EliteAffix.Spearbound;
            (Color fillColor, Color edgeColor) = spearboundElite && !highContrastTelegraphs
                ? (Color.FromArgb(42, 76, 211, 245), Color.FromArgb(235, 76, 211, 245))
                : AttackWarningPalette(highContrastTelegraphs);
            using var p = new Pen(edgeColor, highContrastTelegraphs ? 4 : 3); using var b = new SolidBrush(fillColor);
            if (RangedImpactRadius(enemy.Kind) > 0 || enemy.Kind == EnemyKind.Elite && enemy.EliteAffix == EliteAffix.Spearbound)
            {
                if (highContrastTelegraphs) { using var shadow = new Pen(Color.FromArgb(230, 12, 13, 15), 7); g.DrawLine(shadow, enemy.Pos, enemy.Target); }
                g.DrawLine(p, enemy.Pos, enemy.Target);
            }
            g.FillEllipse(b, center.X - radius, center.Y - radius, radius * 2, radius * 2);
            if (highContrastTelegraphs) { using var shadow = new Pen(Color.FromArgb(240, 12, 13, 15), 7); g.DrawEllipse(shadow, center.X - radius, center.Y - radius, radius * 2, radius * 2); }
            g.DrawEllipse(p, center.X - radius, center.Y - radius, radius * 2, radius * 2);
        }
        using var body = new SolidBrush(enemy.Flash > 0 ? Color.White : color); using var edge = new Pen(enemy == lockedTarget ? Color.Gold : IsArmored(enemy) ? Color.LightSteelBlue : Color.FromArgb(35, 25, 22), enemy == lockedTarget ? 4 : 2);
        int bossSpriteIndex = CampaignBossSpriteIndex(enemy.Kind), enemySpriteRow = EnemySpriteRow(enemy.Kind);
        Bitmap? individualSprite = enemy.Kind == EnemyKind.Elite && enemy.EliteAffix == EliteAffix.Spearbound ? SpearboundEliteSprite.Value : AdvancedEnemySprites.Value.GetValueOrDefault(enemy.Kind);
        bool individualSpriteLoaded = individualSprite is not null;
        bool bossSprite = bossSpriteIndex >= 0;
        int spriteRow = individualSpriteLoaded ? 0 : bossSprite ? bossSpriteIndex % 4 : enemySpriteRow;
        Bitmap? enemySprites = individualSpriteLoaded ? individualSprite : bossSprite ? bossSpriteIndex switch { < 4 => BossSpriteSheet.Value, < 8 => MidBossSpriteSheet.Value, < 12 => LateBossSpriteSheet.Value, < 16 => FinalWaveBossSpriteSheet.Value, _ => EndgameBossSpriteSheet.Value } : enemySpriteRow >= 0 ? EnemySpriteSheet.Value : null;
        if (!individualSpriteLoaded && !bossSprite && enemySpriteRow >= 0 && enemy.Windup > 0)
            enemySprites = EnemyAttackSpriteSheet.Value ?? enemySprites;
        float spriteHeight = individualSpriteLoaded ? enemy.Kind switch { EnemyKind.Elite => 118, EnemyKind.EmberWisp => 72, EnemyKind.MudCantor => 86, EnemyKind.NameMoth => 84, EnemyKind.PaperWraith => 86, EnemyKind.SaltEcho => 82, _ => 80 } : bossSprite ? 132 : enemy.Kind == EnemyKind.Elite ? 82 : 70;
        if (spriteRow >= 0 && enemySprites is not null)
        {
            float cellWidth = individualSpriteLoaded ? enemySprites.Width : enemySprites.Width / 4f, cellHeight = individualSpriteLoaded ? enemySprites.Height : enemySprites.Height / 4f;
            float windupProgress = enemy.WindupMax > 0 && enemy.Windup > 0 ? Math.Clamp(1f - enemy.Windup / enemy.WindupMax, 0f, 1f) : 0f;
            int windupFrame = Math.Min(3, (int)(windupProgress * 4f));
            int frame = individualSpriteLoaded ? 0 : enemy.Windup > 0 ? windupFrame : EnemySpriteFrame(enemy.AnimationTime, enemy.Moving && enemy.Stagger <= 0);
            RectangleF source = new(frame * cellWidth, spriteRow * cellHeight, cellWidth, cellHeight);
            float size = spriteHeight;
            float width = individualSpriteLoaded ? size * cellWidth / cellHeight : size;
            bool freelyMoving = enemy.Moving && enemy.Windup <= 0 && enemy.Stagger <= 0;
            bool animatedCreature = individualSpriteLoaded && enemy.Kind != EnemyKind.Elite;
            float motionPhase = freelyMoving ? enemy.AnimationTime * 15 : windupPulse * 2.2f;
            float bob = animatedCreature ? MathF.Sin(motionPhase) * (freelyMoving ? 2f : 1.1f) : 0;
            float breath = animatedCreature && !freelyMoving && enemy.Windup <= 0 && enemy.Stagger <= 0 ? MathF.Sin(windupPulse * 2.4f) * 0.009f : 0;
            float posedWidth = width * (1f + windupProgress * 0.07f + breath), posedHeight = size * (1f - windupProgress * 0.035f + breath);
            float lunge = windupProgress * (bossSprite ? 5f : 7f);
            RectangleF target = new(enemy.Pos.X - posedWidth / 2 + lunge, enemy.Pos.Y - posedHeight + bob, posedWidth, posedHeight);
            using var shadow = new SolidBrush(Color.FromArgb(110, 5, 7, 9));
            g.FillEllipse(shadow, enemy.Pos.X - r, enemy.Pos.Y - 5, r * 2, 10);
            var spriteTransform = g.Save();
            float facingX = enemy.Windup > 0 ? enemy.Target.X : player.X;
            if (EnemyFacesLeft(enemy.Pos.X, facingX)) { g.TranslateTransform(enemy.Pos.X * 2, 0); g.ScaleTransform(-1, 1); }
            g.DrawImage(enemySprites, target, source, GraphicsUnit.Pixel);
            g.Restore(spriteTransform);
            if (enemy == lockedTarget || IsArmored(enemy))
                g.DrawEllipse(edge, enemy.Pos.X - r - 3, enemy.Pos.Y - 7, (r + 3) * 2, 12);
            if (enemy.Flash > 0)
            {
                using var hitFlash = new Pen(Color.FromArgb(170, 245, 235, 213), 2);
                g.DrawEllipse(hitFlash, enemy.Pos.X - r - 4, enemy.Pos.Y - 10, (r + 4) * 2, 16);
                int alpha = Math.Clamp((int)(190 * enemy.Flash / 0.3f), 0, 190);
                using var spriteFlash = new Pen(Color.FromArgb(alpha, 255, 233, 183), Math.Max(2, posedHeight / 32));
                using var spriteGlow = new SolidBrush(Color.FromArgb(alpha / 4, 255, 233, 183));
                RectangleF flashBounds = new(target.X + posedWidth * 0.2f, target.Y + posedHeight * 0.12f, posedWidth * 0.6f, posedHeight * 0.7f);
                g.FillEllipse(spriteGlow, flashBounds); g.DrawEllipse(spriteFlash, flashBounds);
            }
        }
        else
        {
            if (!DrawProceduralEnemy(g, enemy, enemy.Flash > 0 ? Color.White : color))
            {
                g.FillEllipse(body, enemy.Pos.X - r, enemy.Pos.Y - r, r * 2, r * 2); g.DrawEllipse(edge, enemy.Pos.X - r, enemy.Pos.Y - r, r * 2, r * 2);
            }
        }
        if (IsArmored(enemy) && !enemy.Armored)
        {
            using var wardRing = new Pen(Color.FromArgb(190, 186, 218, 231), 2) { DashStyle = DashStyle.Dash };
            g.DrawEllipse(wardRing, enemy.Pos.X - r - 4, enemy.Pos.Y - 9, (r + 4) * 2, 18);
        }
        if (enemy.Kind == EnemyKind.Elite)
        {
            Color affixColor = enemy.EliteAffix switch { EliteAffix.Armored => Color.LightSteelBlue, EliteAffix.Swift => Color.Gold, EliteAffix.Bloodbound => Color.HotPink, EliteAffix.Spearbound => Color.DeepSkyBlue, _ => Color.IndianRed };
            DashStyle dash = enemy.EliteAffix is EliteAffix.Swift or EliteAffix.Spearbound ? DashStyle.Dash : enemy.EliteAffix == EliteAffix.Bloodbound ? DashStyle.Dot : DashStyle.Solid;
            using var affixRing = new Pen(Color.FromArgb(225, affixColor), 3) { DashStyle = dash };
            g.DrawEllipse(affixRing, enemy.Pos.X - r - 6, enemy.Pos.Y - 12, (r + 6) * 2, 24);
        }
        if (enemy.VigilMinion) { using var chorusRing = new Pen(Color.FromArgb(235, 194, 133, 230), 3) { DashStyle = DashStyle.Dot }; g.DrawEllipse(chorusRing, enemy.Pos.X - r - 6, enemy.Pos.Y - 12, (r + 6) * 2, 24); }
        if (enemy.Kind == EnemyKind.ReedKeeper) { using var stalk = new Pen(Color.FromArgb(220, 205, 193, 133), 3); for (int i = -1; i <= 1; i++) g.DrawLine(stalk, enemy.Pos.X + i * 9, enemy.Pos.Y + 13, enemy.Pos.X + i * 5, enemy.Pos.Y - 23); }
        if (enemy.Kind == EnemyKind.FlowKeeper) { using var ring = new Pen(Color.FromArgb(210, 157, 215, 216), 2); float spin = windupPulse * 40; g.DrawArc(ring, enemy.Pos.X - 18, enemy.Pos.Y - 18, 36, 36, spin, 235); g.DrawArc(ring, enemy.Pos.X - 13, enemy.Pos.Y - 13, 26, 26, spin + 170, 115); }
        if (enemy.Kind == EnemyKind.GlassHerald) { using var shard = new Pen(Color.FromArgb(225, 186, 224, 231), 3); float spin = windupPulse * 24; for (int i = 0; i < 3; i++) { float angle = spin + i * 120; float radians = angle * MathF.PI / 180; PointF tip = new(enemy.Pos.X + MathF.Cos(radians) * 27, enemy.Pos.Y + MathF.Sin(radians) * 27); PointF tail = new(enemy.Pos.X - MathF.Cos(radians) * 18, enemy.Pos.Y - MathF.Sin(radians) * 18); g.DrawLine(shard, tail, tip); } }
        if (enemy.Kind == EnemyKind.RouteScribe) { using var folio = new Pen(Color.FromArgb(225, 196, 216, 226), 2); float spin = windupPulse * 28; for (int i = 0; i < 4; i++) { float angle = (spin + i * 90) * MathF.PI / 180; float x = enemy.Pos.X + MathF.Cos(angle) * 28, y = enemy.Pos.Y + MathF.Sin(angle) * 22; g.DrawRectangle(folio, x - 6, y - 8, 12, 16); } }
        float hpY = spriteRow >= 0 && enemySprites is not null ? enemy.Pos.Y - (bossSprite ? 141 : individualSpriteLoaded ? spriteHeight + 8 : enemy.Kind == EnemyKind.Elite ? 88 : 77) : enemy.Pos.Y - r - 10;
        using var hpBack = new SolidBrush(Color.FromArgb(40, 20, 20)); using var hpBar = new SolidBrush(Color.FromArgb(188, 76, 70)); g.FillRectangle(hpBack, enemy.Pos.X - 22, hpY, 44, 5); g.FillRectangle(hpBar, enemy.Pos.X - 22, hpY, 44 * Math.Max(0, enemy.Hp / enemy.MaxHp), 5);
        string name = EnemyName(enemy); if (name.Length > 0) DrawCentered(g, name, spriteRow >= 0 && enemySprites is not null ? hpY - 17 : enemy.Pos.Y - r - 28, new Font("Segoe UI", 8, FontStyle.Bold), Color.FromArgb(233, 198, 151), enemy.Pos.X);
        if (enemy.Boss) { using var bossBack = new SolidBrush(Color.FromArgb(40, 20, 20)); using var bossHp = new SolidBrush(enemy.Hp <= enemy.MaxHp * 0.5f ? Color.OrangeRed : enemy.Kind == EnemyKind.BlindScribe ? Color.Teal : enemy.Kind == EnemyKind.RiverFerryman ? Color.SteelBlue : enemy.Kind == EnemyKind.AshWarden ? Color.DarkOrange : enemy.Kind == EnemyKind.HollowName ? Color.LightSeaGreen : enemy.Kind == EnemyKind.NameWeaver ? Color.MediumPurple : enemy.Kind == EnemyKind.FirstKeeper ? Color.Goldenrod : enemy.Kind == EnemyKind.OathCartographer ? Color.DarkCyan : enemy.Kind == EnemyKind.ReedKeeper ? Color.DarkOliveGreen : enemy.Kind == EnemyKind.CounterCurrent ? Color.CadetBlue : enemy.Kind == EnemyKind.RiftCantor ? Color.MediumOrchid : enemy.Kind == EnemyKind.FlowKeeper ? Color.DeepSkyBlue : enemy.Kind == EnemyKind.GlassHerald ? Color.LightCyan : enemy.Kind is EnemyKind.NameBroker or EnemyKind.TideWitness ? Color.MediumPurple : enemy.Kind == EnemyKind.RouteScribe ? Color.LightSteelBlue : Color.Firebrick); g.FillRectangle(bossBack, 360, 112, 240, 8); g.FillRectangle(bossHp, 360, 112, 240 * Math.Clamp(enemy.Hp / enemy.MaxHp, 0, 1), 8); string bossName = enemy.Kind == EnemyKind.BlindScribe ? "ESCRIBA CEGO" : enemy.Kind == EnemyKind.RiverFerryman ? "BARQUEIRO OCO" : enemy.Kind == EnemyKind.AshWarden ? "GUARDIÃO DAS CINZAS" : enemy.Kind == EnemyKind.HollowName ? "O NOME VAZIO" : enemy.Kind == EnemyKind.NameWeaver ? "A TECELÃ DE NOMES" : enemy.Kind == EnemyKind.FirstKeeper ? "A PRIMEIRA VIGÍLIA" : enemy.Kind == EnemyKind.OathCartographer ? "CARTÓGRAFA DO VÃO" : enemy.Kind == EnemyKind.NameBroker ? "ESCRIVÃO DE CONTRATO" : enemy.Kind == EnemyKind.TideWitness ? "TESTEMUNHA DA MARÉ" : enemy.Kind == EnemyKind.ReedKeeper ? "GUARDIÃ DO LEITO" : enemy.Kind == EnemyKind.CounterCurrent ? "AFINADOR DAS MARGENS" : enemy.Kind == EnemyKind.RiftCantor ? "A CONTRAVOZ" : enemy.Kind == EnemyKind.FlowKeeper ? "VIGIA DO VEIO PRESO" : enemy.Kind == EnemyKind.GlassHerald ? "ARAUTO DE VIDRO" : enemy.Kind == EnemyKind.LanternRegent ? "REGENTE DAS LANTERNAS" : enemy.Kind == EnemyKind.RouteScribe ? "ESCRIBA DO TRAÇADO" : enemy.Kind == EnemyKind.DeepSilence ? "SILÊNCIO DE BAIXO" : enemy.Kind == EnemyKind.BorrowedChorus ? "ARQUIVISTA DAS MARÉS" : "GUARDIÃO DA CRIPTA"; DrawCentered(g, enemy.Hp <= enemy.MaxHp * 0.5f ? $"{bossName} • SEGUNDA FASE" : bossName, 91, new Font("Georgia", 11, FontStyle.Bold), enemy.Hp <= enemy.MaxHp * 0.5f ? Color.OrangeRed : Color.Goldenrod); }
    }
    private static bool DrawProceduralEnemy(Graphics g, Enemy enemy, Color color)
    {
        float x = enemy.Pos.X, y = enemy.Pos.Y, r = Math.Max(13, enemy.Radius);
        using var fill = new SolidBrush(color);
        using var outline = new Pen(Color.FromArgb(225, 28, 25, 27), 2.2f);
        using var accent = new Pen(Color.FromArgb(205, color), 2.4f);
        switch (enemy.Kind)
        {
            case EnemyKind.BorrowedChorus:
                g.DrawEllipse(accent, x - r * 1.5f, y - r * 1.5f, r * 3, r * 3);
                g.DrawEllipse(accent, x - r, y - r, r * 2, r * 2);
                for (int i = 0; i < 3; i++)
                {
                    float angle = enemy.AnimationTime * 0.7f + i * MathF.PI * 2 / 3;
                    float sx = x + MathF.Cos(angle) * r * 1.3f, sy = y + MathF.Sin(angle) * r * 1.3f;
                    g.FillEllipse(fill, sx - r * .22f, sy - r * .22f, r * .44f, r * .44f);
                }
                g.FillEllipse(fill, x - r * .42f, y - r * .52f, r * .84f, r * 1.04f);
                g.DrawEllipse(outline, x - r * .42f, y - r * .52f, r * .84f, r * 1.04f);
                return true;
            case EnemyKind.InkEcho:
                using (var shape = new GraphicsPath())
                {
                    shape.AddPolygon(new[] { new PointF(x - r * .72f, y - r * .7f), new PointF(x + r * .5f, y - r), new PointF(x + r * .8f, y - r * .1f), new PointF(x + r * .22f, y + r * .9f), new PointF(x - r * .05f, y + r * .34f), new PointF(x - r * .72f, y + r * .72f) });
                    g.FillPath(fill, shape); g.DrawPath(outline, shape);
                }
                using (var eye = new SolidBrush(Color.FromArgb(235, 223, 195, 247))) g.FillEllipse(eye, x + r * .12f, y - r * .45f, r * .28f, r * .15f);
                g.DrawLine(accent, x - r * 1.15f, y + r * .72f, x - r * .42f, y + r * .15f);
                return true;
            case EnemyKind.DrownedMemory:
                g.DrawEllipse(accent, x - r * .86f, y - r * .86f, r * 1.72f, r * 1.72f);
                g.DrawEllipse(accent, x - r * .56f, y - r * .56f, r * 1.12f, r * 1.12f);
                g.FillEllipse(fill, x - r * .27f, y - r * .37f, r * .54f, r * .74f);
                using (var eye = new SolidBrush(Color.FromArgb(220, 220, 244, 230))) g.FillEllipse(eye, x - r * .11f, y - r * .12f, r * .22f, r * .15f);
                return true;
            case EnemyKind.EmberWisp:
                using (var halo = new SolidBrush(Color.FromArgb(48, color))) g.FillEllipse(halo, x - r * 1.3f, y - r * 1.3f, r * 2.6f, r * 2.6f);
                using (var star = new GraphicsPath())
                {
                    PointF[] points = Enumerable.Range(0, 8).Select(i => { float a = (float)(i * Math.PI / 4 - Math.PI / 2); float radius = i % 2 == 0 ? r : r * .48f; return new PointF(x + MathF.Cos(a) * radius, y + MathF.Sin(a) * radius); }).ToArray();
                    star.AddPolygon(points); g.FillPath(fill, star); g.DrawPath(outline, star);
                }
                using (var core = new SolidBrush(Color.FromArgb(240, 255, 226, 166))) g.FillEllipse(core, x - r * .18f, y - r * .18f, r * .36f, r * .36f);
                return true;
            case EnemyKind.NameMoth:
                using (var wings = new GraphicsPath())
                {
                    wings.AddPolygon(new[] { new PointF(x - r * .18f, y - r * .05f), new PointF(x - r * 1.25f, y - r), new PointF(x - r * .9f, y + r * .78f), new PointF(x - r * .12f, y + r * .34f) });
                    wings.AddPolygon(new[] { new PointF(x + r * .18f, y - r * .05f), new PointF(x + r * 1.25f, y - r), new PointF(x + r * .9f, y + r * .78f), new PointF(x + r * .12f, y + r * .34f) });
                    g.FillPath(fill, wings); g.DrawPath(outline, wings);
                }
                g.DrawLine(outline, x - 3, y - r * .15f, x - r * .35f, y - r * .9f); g.DrawLine(outline, x + 3, y - r * .15f, x + r * .35f, y - r * .9f);
                using (var body = new SolidBrush(Color.FromArgb(240, 224, 224, 191))) g.FillEllipse(body, x - r * .17f, y - r * .25f, r * .34f, r * .85f);
                using (var eye = new SolidBrush(Color.FromArgb(235, 204, 255, 229))) g.FillEllipse(eye, x - r * .08f, y - r * .17f, r * .16f, r * .12f);
                return true;
            case EnemyKind.MudCantor:
                g.FillEllipse(fill, x - r * .8f, y - r * .72f, r * 1.6f, r * 1.45f); g.DrawEllipse(outline, x - r * .8f, y - r * .72f, r * 1.6f, r * 1.45f);
                g.DrawLine(outline, x - r * .66f, y - r * .42f, x + r * .66f, y - r * .42f);
                using (var eye = new SolidBrush(Color.FromArgb(235, 247, 210, 148))) g.FillEllipse(eye, x - r * .38f, y - r * .19f, r * .2f, r * .12f);
                g.DrawArc(accent, x - r * 1.18f, y - r * .48f, r * 2.36f, r * 1.7f, 300, 120);
                g.DrawArc(accent, x - r * 1.48f, y - r * .7f, r * 2.96f, r * 2.1f, 300, 120);
                return true;
            case EnemyKind.PaperWraith:
                var transform = g.Save(); g.TranslateTransform(x, y); g.RotateTransform(MathF.Sin(enemy.AnimationTime * 4f) * 9f);
                using (var page = new GraphicsPath())
                {
                    page.AddPolygon(new[] { new PointF(-r * .62f, -r), new PointF(r * .3f, -r * .86f), new PointF(r * .68f, -r * .2f), new PointF(r * .42f, r * .34f), new PointF(r * .66f, r), new PointF(-r * .32f, r * .78f), new PointF(-r * .72f, r * .08f) });
                    g.FillPath(fill, page); g.DrawPath(outline, page);
                }
                g.DrawLine(accent, -r * .34f, -r * .32f, r * .35f, -r * .18f); g.DrawLine(accent, -r * .25f, r * .03f, r * .22f, r * .16f);
                g.Restore(transform);
                return true;
            case EnemyKind.SaltEcho:
                using (var crystal = new GraphicsPath())
                {
                    crystal.AddPolygon(new[] { new PointF(x, y - r), new PointF(x + r * .72f, y - r * .42f), new PointF(x + r * .62f, y + r * .48f), new PointF(x, y + r), new PointF(x - r * .68f, y + r * .48f), new PointF(x - r * .72f, y - r * .42f) });
                    g.FillPath(fill, crystal); g.DrawPath(outline, crystal);
                }
                g.DrawLine(accent, x, y - r * .78f, x, y + r * .73f); g.DrawLine(accent, x, y - r * .05f, x + r * .5f, y - r * .38f); g.DrawLine(accent, x, y + r * .08f, x - r * .5f, y - r * .34f);
                return true;
            default:
                return false;
        }
    }

    private string DropLabel(Drop item)
    {
        return item.Kind switch
        {
            "gold" => item.Amount + " OURO",
            "potion" => "POÇÃO DE VÉSPERA",
            "glass" => item.Amount + " SAL VÍTREO",
            "weapon" => WeaponDropComparison(item),
            "charm" => charmNames[item.Amount],
            "armor" => "PEITORAL DE PEDRA",
            "relic" => "FRAGMENTO DO RELICÁRIO",
            "seal" => "SELO DA VIGÍLIA",
            "bell" => "SINO DE NARA",
            "lens" => "PRISMA DO LUME",
            "trace" => "LEMBRANÇA DE NARA",
            "chorus" => "CORO DOS NOMES",
            "first" => "MEMÓRIA DE ILYRA",
            "west" => "MAPA DE NARA",
            "register" => "REGISTRO COMPARTILHADO",
            "witness" => "TESTEMUNHO DA MARÉ",
            "ebb" => "MEMÓRIA DA VAZANTE",
            "counter" => "COMPASSO DAS MARGENS",
            "verse" => "NOTA DA CONTRAVOZ",
            "deep" => "ECO DOS PASSOS SEM NOME",
            "signal" => "SINAL: A LUZ QUE ESPERA",
            "route" => "TRAÇADO DAS ROTAS ABERTAS",
            "silence" => "MEMÓRIA DAS VOZES DEVOLVIDAS", "borrowed" => "ECO COMPARTILHADO",
            "beyond" => "SINAL DE PASSAGEM",
            _ => "ECO"
        };
    }

    private string WeaponDropComparison(Drop item)
    {
        int proposedDamage = damage - WeaponPower(weapon) + weaponDamage[item.Amount] + item.Bonus;
        int damageChange = proposedDamage - damage;
        string currentAffixes = WeaponGroundAffix(weaponAffixes[weapon], weaponAffixes2[weapon], weaponRarities[weapon]);
        if (currentAffixes.Length == 0) currentAffixes = "sem afixo";
        string profile = weapon == item.Amount
            ? WeaponTraitSummary(item.Amount)
            : $"{WeaponTraitSummary(weapon)} → {WeaponTraitSummary(item.Amount)}";
        return $"{WeaponRarityName(item.Rarity)} {weaponNames[item.Amount]}\nDANO: {damage} → {proposedDamage} ({damageChange:+#;-#;0})\nPERFIL: {profile}\nAFIXOS NOVOS: {WeaponGroundAffix(item)}\nATUAL: {currentAffixes}";
    }

    private static string WeaponTraitSummary(int index) => index switch
    {
        1 => "longo • perfura • interrompe",
        2 => "curto • veloz • crítico",
        _ => "médio • equilibrado"
    };

    private static string WeaponGroundAffix(Drop item) => WeaponGroundAffix(item.Affix, item.Affix2, item.Rarity);
    private static string WeaponGroundAffix(int affix, int affix2, int rarity)
    {
        string affixes = string.Join(" + ", new[] { WeaponGroundAffix(affix, rarity), WeaponGroundAffix(affix2, rarity) }.Where(description => description.Length > 0));
        return affixes.Length == 0 ? "sem afixo" : affixes;
    }
    private static string WeaponGroundAffix(int affix, int rarity) => affix switch
    {
        1 => "Trégua • cura " + (rarity == 2 ? 6 : 3) + " por abate",
        2 => "Contracanto • crítico +" + (rarity == 2 ? 11 : 7) + " p.p.",
        3 => "Maré • técnica +" + (rarity == 2 ? 18 : 12) + "%",
        4 => "Passo Célere • recarga -" + (rarity == 2 ? 16 : 10) + "%",
        5 => "Lastro • dano recebido -" + (rarity == 2 ? 12 : 8) + "%",
        6 => "Fio Esticado • alcance +" + (rarity == 2 ? 20 : 12) + "%",
        _ => ""
    };

    private void DrawFloatingTexts(Graphics g)
    {
        if (floatingTexts.Count == 0) return;
        using var font = new Font("Segoe UI", 10, FontStyle.Bold);
        foreach (FloatingText text in floatingTexts)
        {
            int alpha = Math.Clamp((int)(255 * text.TimeLeft / text.Duration), 0, 255);
            using var brush = new SolidBrush(Color.FromArgb(alpha, text.Color));
            using var outline = new SolidBrush(Color.FromArgb(alpha, 8, 10, 12));
            SizeF size = g.MeasureString(text.Text, font);
            float x = text.Pos.X - size.Width / 2, y = text.Pos.Y - size.Height / 2;
            g.DrawString(text.Text, font, outline, x + 1.5f, y + 1.5f); g.DrawString(text.Text, font, brush, x, y);
        }
    }

    private long KnownRoomMask()
    {
        long known = clearedRooms | (1L << room);
        if (questStage >= 3) known |= 1L << 3;
        if (questStage >= 22) known |= 1L << 18;
        return known;
    }

    private string AtlasRoomName(int index)
    {
        string name = roomNames[index];
        int separator = name.LastIndexOf(" • ", StringComparison.Ordinal);
        return separator >= 0 ? name[(separator + 3)..] : name;
    }

    private void DrawMapOverlay(Graphics g)
    {
        using var shade = new SolidBrush(Color.FromArgb(225, 5, 7, 9));
        using var panel = new SolidBrush(Color.FromArgb(248, 15, 17, 18));
        using var border = new Pen(Color.FromArgb(190, 143, 112, 73), 2);
        using var nodeFont = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.FillRectangle(shade, 0, 0, 960, 640);
        g.FillRectangle(panel, 28, 105, 904, 488);
        g.DrawRectangle(border, 28, 105, 904, 488);
        DrawCentered(g, "ATLAS DAS CÂMARAS", 120, new Font("Georgia", 17, FontStyle.Bold), Color.Goldenrod);
        DrawCentered(g, roomNames[room], 145, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite);

        long known = KnownRoomMask();
        const int columns = 5, nodeWidth = 168, nodeHeight = 42, gapX = 8, gapY = 4;
        for (int index = 0; index < roomNames.Length; index++)
        {
            int column = index % columns, row = index / columns;
            float x = 36 + column * (nodeWidth + gapX), y = 165 + row * (nodeHeight + gapY);
            bool current = index == room, visited = (known & (1L << index)) != 0, cleared = (clearedRooms & (1L << index)) != 0;
            Color fill = current ? Color.FromArgb(110, 103, 48) : cleared ? Color.FromArgb(39, 67, 53) : visited ? Color.FromArgb(52, 58, 57) : Color.FromArgb(29, 32, 33);
            Color line = current ? Color.Goldenrod : cleared ? Color.FromArgb(89, 139, 105) : visited ? Color.FromArgb(95, 103, 96) : Color.FromArgb(54, 58, 56);
            Color ink = visited ? Color.AntiqueWhite : Color.FromArgb(108, 112, 105);
            using var nodeBrush = new SolidBrush(fill);
            using var nodeBorder = new Pen(line, current ? 2 : 1);
            g.FillRectangle(nodeBrush, x, y, nodeWidth, nodeHeight);
            g.DrawRectangle(nodeBorder, x, y, nodeWidth, nodeHeight);
            string label = visited ? $"{index + 1:00}\n{AtlasRoomName(index)}" : $"{index + 1:00}\n???";
            using var textBrush = new SolidBrush(current ? Color.FromArgb(255, 224, 166) : ink);
            g.DrawString(label, nodeFont, textBrush, new RectangleF(x + 4, y + 2, nodeWidth - 8, nodeHeight - 4), format);
        }
        using var legendFont = new Font("Segoe UI", 9);
        using var legendBrush = new SolidBrush(Color.FromArgb(180, 185, 176));
        g.DrawString("DOURADO: AQUI   VERDE: LIMPA   CINZA: VISITADA   ???: DESCONHECIDA   M/ESC FECHA", legendFont, legendBrush, 54, 578);
    }

    private void DrawDrop(Graphics g, Drop item)
    {
        Color color = item.Kind switch
        {
            "relic" => Color.Violet, "seal" => Color.LightSkyBlue, "bell" => Color.FromArgb(162, 212, 192),
            "lens" => Color.FromArgb(248, 185, 95), "trace" => Color.FromArgb(185, 231, 215), "chorus" => Color.Khaki,
            "first" => Color.Goldenrod, "west" => Color.LightSkyBlue, "register" => Color.LightSteelBlue,
            "deep" => Color.FromArgb(190, 225, 235), "borrowed" => Color.FromArgb(130, 210, 204), "glass" => Color.LightCyan, "beyond" => Color.PaleGreen, "ebb" => Color.DodgerBlue,
            "counter" => Color.LightGoldenrodYellow, "verse" => Color.Orchid, "charm" => item.Amount is 7 or 8 ? Color.MediumTurquoise : item.Amount == 4 ? Color.Goldenrod : item.Amount == 5 ? Color.LightSeaGreen : item.Amount == 6 ? Color.PaleGreen : Color.Plum,
            "armor" => Color.MediumAquamarine, "weapon" => item.Rarity == 2 ? Color.Turquoise : item.Rarity == 1 ? Color.Goldenrod : Color.CornflowerBlue,
            "potion" => Color.IndianRed, _ => Color.Goldenrod
        };
        using var icon = new SolidBrush(color);
        using var edge = new Pen(Color.FromArgb(230, 220, 190), 1);
        g.FillEllipse(icon, item.Pos.X - 8, item.Pos.Y - 8, 16, 16);
        g.DrawEllipse(edge, item.Pos.X - 8, item.Pos.Y - 8, 16, 16);

        if (Distance(player, item.Pos) > 150) return;
        using var font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        using var labelBrush = new SolidBrush(item.Kind == "weapon" && item.Rarity == 0 ? Color.AntiqueWhite : color);
        using var background = new SolidBrush(Color.FromArgb(225, 14, 17, 18));
        using var border = new Pen(Color.FromArgb(210, color), 1);
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        string label = DropLabel(item);
        float dropDistance = Distance(player, item.Pos);
        if (item.Kind == "weapon" && dropDistance <= 75)
        {
            (int salvageGold, int salvageGlass) = WeaponSalvageReward(item.Rarity);
            label += $"\nX desmonta • +{salvageGold} ouro{(salvageGlass > 0 ? $" / +{salvageGlass} sal" : "")}";
        }
        float maxWidth = item.Kind == "weapon" ? 310 : 196;
        SizeF measured = g.MeasureString(label, font, (int)maxWidth - 12);
        float width = Math.Clamp(measured.Width + 12, 94, maxWidth);
        float height = measured.Height + 5;
        float x = Math.Clamp(item.Pos.X - width / 2, 6, 954 - width);
        float y = Math.Clamp(item.Pos.Y > 180 ? item.Pos.Y - height - 15 : item.Pos.Y + 16, 108, 550 - height);
        var bounds = new RectangleF(x, y, width, height);
        g.FillRectangle(background, bounds);
        g.DrawRectangle(border, bounds.X, bounds.Y, bounds.Width, bounds.Height);
        g.DrawString(label, font, labelBrush, bounds, format);
    }
    private void DrawPlayer(Graphics g)
    {
        PointF animationDirection = attackFlash > 0 ? facing : dashTime > 0 ? dashDirection : MovementInput();
        if (Length(animationDirection) <= 0.001f) animationDirection = CurrentDrift();
        if (Length(animationDirection) <= 0.001f) animationDirection = facing;
        Bitmap? attackSprites = attackFlash > 0 ? PlayerAttackSpriteSheet.Value : null;
        Bitmap? sprites = attackSprites ?? AvenSpriteSheet.Value;
        if (sprites is not null)
        {
            int row = PlayerSpriteRow(animationDirection);
            bool moving = dashTime > 0 || Length(MovementInput()) > 0.001f || Length(CurrentDrift()) > 0.001f;
            int frame = attackSprites is not null ? Math.Clamp((int)((0.16f - attackFlash) / 0.16f * 4), 0, 3) : PlayerSpriteFrame(playerAnimationTime, moving);
            float cellWidth = sprites.Width / 4f, cellHeight = sprites.Height / 4f;
            RectangleF source = new(frame * cellWidth, row * cellHeight, cellWidth, cellHeight);
            RectangleF target = new(player.X - 38, player.Y - 73, 76, 76);
            using var shadow = new SolidBrush(Color.FromArgb(100, 5, 7, 9));
            g.FillEllipse(shadow, player.X - 17, player.Y - 5, 34, 10);
            g.DrawImage(sprites, target, source, GraphicsUnit.Pixel);
            if (invulnerable > 0)
            {
                int flashAlpha = PlayerDamageFlashAlpha(invulnerable, windupPulse);
                using var damageGlow = new SolidBrush(Color.FromArgb(flashAlpha / 3, 239, 83, 76));
                using var damageEdge = new Pen(Color.FromArgb(flashAlpha, 255, 126, 105), 3);
                RectangleF damageBounds = new(target.X + 18, target.Y + 14, 40, 50);
                g.FillEllipse(damageGlow, damageBounds); g.DrawEllipse(damageEdge, damageBounds);
            }
        }
        else
        {
            using var fallback = new SolidBrush(invulnerable > 0 ? Color.Salmon : Color.FromArgb(90, 143, 138));
            using var fallbackFace = new SolidBrush(Color.Bisque);
            g.FillEllipse(fallback, player.X - 16, player.Y - 16, 32, 32);
            g.FillEllipse(fallbackFace, player.X - 8, player.Y - 11, 16, 16);
            using var facingLine = new Pen(Color.AntiqueWhite, 4);
            g.DrawLine(facingLine, player, Add(player, Scale(facing, 24)));
        }
        if (wardTime > 0)
        {
            int alpha = Math.Clamp((int)(112 + 70 * MathF.Sin(windupPulse * 8)), 35, 190);
            using var wardGlow = new Pen(Color.FromArgb(alpha, 179, 215, 215), 4);
            using var wardCore = new Pen(Color.FromArgb(Math.Min(235, alpha + 45), 215, 231, 213), 1);
            g.DrawEllipse(wardGlow, player.X - 25, player.Y - 25, 50, 50);
            g.DrawEllipse(wardCore, player.X - 20, player.Y - 20, 40, 40);
        }
        if (skillCd > 0) { using var cooldownPen = new Pen(Color.FromArgb(190, 230, 193, 129), 3); g.DrawArc(cooldownPen, player.X - 25, player.Y - 25, 50, 50, -90, 360 * skillCd / Math.Max(1.5f, 6f - skillTalent * .5f)); }
        if (shardCd > 0) { using var shardCooldownPen = new Pen(Color.FromArgb(205, 107, 202, 196), 3); g.DrawArc(shardCooldownPen, player.X - 33, player.Y - 33, 66, 66, 90, 360 * shardCd / VoiceShardCooldown()); }
        if (riverBell && bellCd > 0) { using var bellCooldownPen = new Pen(Color.FromArgb(210, 205, 169, 118), 3); g.DrawArc(bellCooldownPen, player.X - 41, player.Y - 41, 82, 82, 180, 360 * bellCd / BellPulseCooldown()); }
        if (ebbSeal && pullCd > 0) { using var ebbCooldownPen = new Pen(Color.FromArgb(210, 115, 185, 211), 3); g.DrawArc(ebbCooldownPen, player.X - 49, player.Y - 49, 98, 98, 270, 360 * pullCd / EbbPullCooldown()); }
        if (anchorWard && wardCd > 0) { using var wardCooldownPen = new Pen(Color.FromArgb(210, 179, 215, 215), 3); g.DrawArc(wardCooldownPen, player.X - 57, player.Y - 57, 114, 114, 0, 360 * wardCd / AnchorWardCooldown()); }
        if (refrainSong && refrainCd > 0) { using var refrainCooldownPen = new Pen(Color.FromArgb(215, 205, 139, 225), 3); g.DrawArc(refrainCooldownPen, player.X - 65, player.Y - 65, 130, 130, 90, 360 * refrainCd / RefrainCooldown()); }
        if (beaconLight && beaconCd > 0) { using var beaconCooldownPen = new Pen(Color.FromArgb(220, 182, 238, 220), 3); g.DrawArc(beaconCooldownPen, player.X - 73, player.Y - 73, 146, 146, 180, 360 * beaconCd / BeaconTotalCooldown()); }
        if (finalEnding == 1 && returnVoiceCd > 0) { using var returnedVoicesCooldownPen = new Pen(Color.FromArgb(215, 205, 221, 165), 3); g.DrawArc(returnedVoicesCooldownPen, player.X - 81, player.Y - 81, 162, 162, 270, 360 * returnVoiceCd / (Math.Max(14f, 23f - skillTalent * 0.7f) * WeaponCooldownMultiplier())); }
        if (finalEnding == 2 && finalStrikeCd > 0) { using var finalSparkCooldownPen = new Pen(Color.FromArgb(225, 255, 191, 115), 3); g.DrawArc(finalSparkCooldownPen, player.X - 89, player.Y - 89, 178, 178, 0, 360 * finalStrikeCd / (Math.Max(12f, 19f - skillTalent * 0.65f) * WeaponCooldownMultiplier())); }
    }
    private static int PlayerSpriteRow(PointF direction)
    {
        if (MathF.Abs(direction.X) > MathF.Abs(direction.Y)) return direction.X < 0 ? 1 : 2;
        return direction.Y < 0 ? 3 : 0;
    }
    private static int PlayerDamageFlashAlpha(float remaining, float pulse) => remaining <= 0 ? 0 : (int)(70 + 55 * (0.5f + 0.5f * MathF.Sin(pulse * 42)));
    private static int PlayerSpriteFrame(float animationTime, bool moving) => moving ? (int)(Math.Max(0, animationTime) / 0.12f) % 4 : 0;
    private void DrawAttackSwing(Graphics g)
    {
        float progress = Math.Clamp(attackFlash / 0.16f, 0, 1);
        float elapsed = 1 - progress;
        float angle = MathF.Atan2(facing.Y, facing.X) * 180 / MathF.PI;
        float radius = weapon == 1 ? 72 : weapon == 2 ? 46 : 57;
        float sweep = BasicAttackSweepAngle(weapon);
        float activeSweep = sweep * (weapon == 1 ? 0.42f : weapon == 2 ? 0.28f : 0.35f);
        float startAngle = angle - sweep / 2 + (sweep - activeSweep) * elapsed;
        var bounds = new RectangleF(player.X - radius, player.Y - radius, radius * 2, radius * 2);
        Color color = weapon == 1 ? Color.FromArgb(255, 220, 155) : weapon == 2 ? Color.FromArgb(163, 222, 223) : Color.FromArgb(224, 213, 181);
        int alpha = (int)(progress * (115 + 110 * MathF.Sin(MathF.PI * elapsed)));
        using var glow = new Pen(Color.FromArgb(alpha / 3, color), weapon == 1 ? 13 : 9) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var edge = new Pen(Color.FromArgb(alpha, color), weapon == 1 ? 5 : 3) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawArc(glow, bounds, startAngle, activeSweep);
        g.DrawArc(edge, bounds, startAngle, activeSweep);
        float tipAngle = (startAngle + activeSweep) * MathF.PI / 180;
        PointF tipDirection = new(MathF.Cos(tipAngle), MathF.Sin(tipAngle));
        PointF bladeTip = Add(player, Scale(tipDirection, radius + 7));
        PointF bladeBase = Add(player, Scale(tipDirection, radius - (weapon == 1 ? 16 : 11)));
        g.DrawLine(glow, bladeBase, bladeTip); g.DrawLine(edge, bladeBase, bladeTip);
        using var tipGlow = new SolidBrush(Color.FromArgb(alpha / 2, color));
        g.FillEllipse(tipGlow, bladeTip.X - 5, bladeTip.Y - 5, 10, 10);
    }
    private void DrawEbbWave(Graphics g) { float progress = 1f - pullWaveTime / 0.45f; float radius = 160 * progress; using var wave = new Pen(Color.FromArgb((int)(210 * pullWaveTime / 0.45f), 98, 190, 220), 4); using var glow = new Pen(Color.FromArgb((int)(65 * pullWaveTime / 0.45f), 98, 190, 220), 12); g.DrawEllipse(glow, player.X - radius, player.Y - radius, radius * 2, radius * 2); g.DrawEllipse(wave, player.X - radius, player.Y - radius, radius * 2, radius * 2); }
    private void DrawBellWave(Graphics g) { float progress = 1f - bellWaveTime / 0.42f; float radius = bellWaveRadius * progress; using var wave = new Pen(Color.FromArgb((int)(210 * bellWaveTime / 0.42f), 115, 208, 197), 4); using var glow = new Pen(Color.FromArgb((int)(65 * bellWaveTime / 0.42f), 115, 208, 197), 12); g.DrawEllipse(glow, player.X - radius, player.Y - radius, radius * 2, radius * 2); g.DrawEllipse(wave, player.X - radius, player.Y - radius, radius * 2, radius * 2); }
    private void DrawRefrainWave(Graphics g) { float progress = 1f - refrainWaveTime / 0.48f; float radius = refrainWaveRadius * progress; int alpha = (int)(220 * refrainWaveTime / 0.48f); using var wave = new Pen(Color.FromArgb(alpha, 207, 147, 227), 4); using var glow = new Pen(Color.FromArgb(alpha / 3, 207, 147, 227), 14); g.DrawEllipse(glow, player.X - radius, player.Y - radius, radius * 2, radius * 2); g.DrawEllipse(wave, player.X - radius, player.Y - radius, radius * 2, radius * 2); }
    private void DrawReturnedVoicesWave(Graphics g) { float progress = 1f - returnVoiceWaveTime / 0.46f; float radius = returnVoiceWaveRadius * progress; int alpha = (int)(225 * returnVoiceWaveTime / 0.46f); using var wave = new Pen(Color.FromArgb(alpha, 205, 221, 165), 4); using var glow = new Pen(Color.FromArgb(alpha / 3, 205, 221, 165), 15); g.DrawEllipse(glow, player.X - radius, player.Y - radius, radius * 2, radius * 2); g.DrawEllipse(wave, player.X - radius, player.Y - radius, radius * 2, radius * 2); }
    private void DrawFinalSparkFlash(Graphics g) { using var spark = new Pen(Color.FromArgb((int)(220 * finalStrikeFlash / 0.24f), 255, 225, 145), 7); using var glow = new Pen(Color.FromArgb((int)(85 * finalStrikeFlash / 0.24f), 255, 177, 98), 18); PointF start = Add(player, Scale(facing, 20)); g.DrawLine(glow, start, finalStrikeTarget); g.DrawLine(spark, start, finalStrikeTarget); }
    private void DrawBeacon(Graphics g)
    {
        int alpha = Math.Clamp((int)(165 + 45 * MathF.Sin(windupPulse * 6)), 70, 220);
        using var glow = new SolidBrush(Color.FromArgb(42, 173, 226, 224)); using var core = new SolidBrush(Color.FromArgb(alpha, 167, 221, 218));
        using var ring = new Pen(Color.FromArgb(alpha, 212, 235, 207), 2); using var ray = new Pen(Color.FromArgb(alpha, 201, 224, 211), 3);
        g.FillEllipse(glow, beaconPos.X - 29, beaconPos.Y - 29, 58, 58); g.DrawEllipse(ring, beaconPos.X - 19, beaconPos.Y - 19, 38, 38);
        g.FillPolygon(core, new[] { new PointF(beaconPos.X, beaconPos.Y - 15), new PointF(beaconPos.X + 9, beaconPos.Y), new PointF(beaconPos.X, beaconPos.Y + 15), new PointF(beaconPos.X - 9, beaconPos.Y) });
        g.DrawLine(ray, beaconPos.X, beaconPos.Y - 29, beaconPos.X, beaconPos.Y - 20); g.DrawLine(ray, beaconPos.X + 20, beaconPos.Y, beaconPos.X + 29, beaconPos.Y); g.DrawLine(ray, beaconPos.X, beaconPos.Y + 20, beaconPos.X, beaconPos.Y + 29); g.DrawLine(ray, beaconPos.X - 29, beaconPos.Y, beaconPos.X - 20, beaconPos.Y);
        if (beaconWaveTime > 0) { float progress = 1 - beaconWaveTime / 0.38f, radius = 128 * progress; int waveAlpha = (int)(210 * beaconWaveTime / 0.38f); using var wave = new Pen(Color.FromArgb(waveAlpha, 182, 238, 220), 3); g.DrawEllipse(wave, beaconPos.X - radius, beaconPos.Y - radius, radius * 2, radius * 2); }
    }
    private static void DrawEchoShard(Graphics g, EchoShard shard)
    {
        PointF tail = Sub(shard.Pos, Scale(shard.Direction, 24));
        using var trail = new Pen(Color.FromArgb(150, 73, 193, 183), 6); using var core = new Pen(Color.FromArgb(235, 211, 225, 180), 2);
        using var glow = new SolidBrush(Color.FromArgb(82, 91, 225, 211)); using var crystal = new SolidBrush(Color.FromArgb(235, 115, 211, 195));
        g.DrawLine(trail, tail, shard.Pos); g.FillEllipse(glow, shard.Pos.X - 12, shard.Pos.Y - 12, 24, 24);
        PointF[] points = { Add(shard.Pos, Scale(shard.Direction, 10)), Add(shard.Pos, new PointF(-shard.Direction.Y * 5, shard.Direction.X * 5)), Add(shard.Pos, Scale(shard.Direction, -8)), Add(shard.Pos, new PointF(shard.Direction.Y * 5, -shard.Direction.X * 5)) };
        g.FillPolygon(crystal, points); g.DrawPolygon(core, points);
    }
    private string ShortSkillName(int slot) => equippedSkills[slot] == 0 ? "VAZIO" : equippedSkills[slot] == 7 ? "DEVOLUÇÃO" : equippedSkills[slot] == 8 ? "CENTELHA" : skillNames[equippedSkills[slot]].ToUpperInvariant();
    private void DrawSkillMenu(Graphics g)
    {
        using var shade = new SolidBrush(Color.FromArgb(185, 5, 7, 9)); using var panelBrush = new SolidBrush(Color.FromArgb(248, 17, 19, 20)); using var edge = new Pen(Color.FromArgb(175, 143, 112, 73), 2);
        using var heading = new Font("Georgia", 17, FontStyle.Bold); using var body = new Font("Segoe UI", 11); using var small = new Font("Segoe UI", 9);
        g.FillRectangle(shade, 0, 83, 960, 557); g.FillRectangle(panelBrush, 105, 132, 750, 435); g.DrawRectangle(edge, 105, 132, 750, 435);
        DrawCentered(g, "TÉCNICAS DO ESCUTADOR", 153, heading, Color.FromArgb(230, 193, 129));
        g.DrawString("ESCOLHA UM ESPAÇO COM F, V OU G; DEPOIS EQUIPE UMA TÉCNICA COM 1–8", small, Brushes.DarkGray, 140, 188);
        g.DrawString("ESPAÇOS ATIVOS", body, Brushes.Goldenrod, 150, 224);
        string[] slotKeys = { "F", "V", "G" };
        for (int slot = 0; slot < 3; slot++)
        {
            bool selected = slot == skillMenuSlot; Brush slotBrush = selected ? Brushes.Goldenrod : Brushes.AntiqueWhite;
            g.DrawString($"{slotKeys[slot]}  •  {ShortSkillName(slot)}{(selected ? "  ◀" : "")}", body, slotBrush, 160, 262 + slot * 44);
        }
        g.DrawString("TÉCNICAS APRENDIDAS", body, Brushes.Goldenrod, 480, 224);
        for (int skill = 1; skill <= 8; skill++)
        {
            bool unlocked = SkillUnlocked(skill); bool equippedElsewhere = equippedSkills.Where((_, index) => index != skillMenuSlot).Contains(skill);
            string state = !unlocked ? "  •  NÃO APRENDIDA" : equippedElsewhere ? "  •  EM OUTRO ESPAÇO" : equippedSkills[skillMenuSlot] == skill ? "  •  EQUIPADA" : "";
            g.DrawString($"{skill}. {skillNames[skill].ToUpperInvariant()}{state}", body, unlocked ? Brushes.AntiqueWhite : Brushes.Gray, 480, 262 + (skill - 1) * 32);
            g.DrawString(skillDescriptions[skill], small, Brushes.DarkGray, 503, 280 + (skill - 1) * 32);
        }
        g.DrawString("BACKSPACE esvazia o espaço • B ou ESC fecha • R continua sendo o Redemoinho", small, Brushes.DarkGray, 150, 544);
    }

    private void DrawInventory(Graphics g)
    {
        using var b = new SolidBrush(Color.FromArgb(245, 17, 19, 20)); using var itemFont = new Font("Segoe UI", 11); using var effectFont = new Font("Segoe UI", 9); g.FillRectangle(b, 150, 145, 660, 490);
        DrawCentered(g, "INVENTÁRIO   •   1–3 ARMAS   •   4 ARMADURA   •   5 RELÍQUIA   •   6/7 TALISMÃS   •   I FECHAR", 180, new Font("Segoe UI", 12, FontStyle.Bold), Color.Goldenrod);
        for (int i = 0; i < 3; i++) { bool found = (ownedWeapons & (1 << i)) != 0; Brush rarityBrush = !found ? Brushes.Gray : weaponRarities[i] == 2 ? Brushes.MediumTurquoise : weaponRarities[i] == 1 ? Brushes.Goldenrod : Brushes.AntiqueWhite; g.DrawString($"{i + 1}. {WeaponDisplayName(i)}    DANO {WeaponPower(i) - WeaponPower(weapon):+#;-#;0}    {(weapon == i ? "EQUIPADA" : found ? "DISPONÍVEL" : "NÃO ENCONTRADA")}", itemFont, rarityBrush, 205, 225 + i * 54); if (found) g.DrawString($"{WeaponAffixDescription(i)}\n{WeaponTraitDescription(i)}", effectFont, Brushes.DarkGray, new RectangleF(232, 246 + i * 54, 570, 32)); }
        g.DrawString($"4. PEITORAL DE PEDRA {(armorOn ? relicOn ? "EQUIPADO • -35% DANO • VIGÍLIA DO VEIO" : "EQUIPADO • -30% DANO" : armor ? "DISPONÍVEL" : "NÃO ENCONTRADO")}", itemFont, Brushes.AntiqueWhite, 205, 385);
        g.DrawString($"5. RELICÁRIO VITAL {(relicOn ? armorOn ? "EQUIPADO • CURA 8 POR ABATE • VIGÍLIA DO VEIO" : "EQUIPADO • CURA 5 POR ABATE" : relic ? "DISPONÍVEL" : "NÃO ENCONTRADO")}", itemFont, Brushes.AntiqueWhite, 205, 420);
        g.DrawString($"SELO DA VIGÍLIA {(archiveSeal ? "ENCONTRADO • +8 DANO" : "AINDA NÃO ENCONTRADO")}", itemFont, Brushes.AntiqueWhite, 205, 455);
        g.DrawString($"SINO DE NARA {(riverBell ? "ENCONTRADO • +20 VIDA MÁXIMA" : "AINDA NÃO ENCONTRADO")}", itemFont, Brushes.AntiqueWhite, 205, 490);
        g.DrawString($"PRISMA DO LUME {(lumeLens ? "ENCONTRADO • +12 DANO" : "AINDA NÃO ENCONTRADO")}", itemFont, Brushes.AntiqueWhite, 205, 525);
        g.DrawString($"RASTRO DE NARA {(naraTrace ? "RECUPERADO • MEMÓRIA PRESERVADA" : "AINDA NÃO ENCONTRADO")}", itemFont, Brushes.AntiqueWhite, 205, 560);
        string[] availableCharms = Enumerable.Range(1, charmNames.Length - 1).Where(i => (ownedCharms & (1 << (i - 1))) != 0).Select(i => charmNames[i]).ToArray();
        string charmList = availableCharms.Length == 0 ? "NENHUM" : string.Join(" • ", availableCharms.Take(3)) + (availableCharms.Length > 3 ? "\n" + string.Join(" • ", availableCharms.Skip(3)) : "");
        g.DrawString($"TALISMÃS: {charmList}   •   SAL VÍTREO: {tideGlass}", effectFont, Brushes.Plum, new RectangleF(205, 574, 560, 27));
        string firstCharm = equippedCharm == 0 ? "VAZIO" : charmNames[equippedCharm]; string secondCharm = equippedCharm2 == 0 ? "VAZIO" : charmNames[equippedCharm2];
        g.DrawString($"6. ESPAÇO I: {firstCharm}    7. ESPAÇO II: {secondCharm}", effectFont, Brushes.Plum, 205, 601);
        string effect = HasCharmPair(1, 2) ? "SINTONIA: Redemoinho +57% dano • penalidade cai para 0,5 s" : HasCharmPair(1, 3) ? "SINTONIA: +17 p.p. crítico • multiplicador 2,75x • ataque básico menos lento" : HasCharmPair(2, 3) ? "SINTONIA: esquiva longa sem acréscimo de recarga" : "Equipar dois talismãs únicos cria uma sintonia entre seus efeitos.";
        if (HasCharmPair(3, 5)) effect = "SINTONIA: Estilhaço +52% dano • alcance +44% • recarga +0,5 s";
        else if (HasCharm(5)) effect += " • Fio da Margem: alcance +44%, recarga +1,25 s";
        if (HasCharm(3) && !HasCharmPair(3, 5)) effect += " • Estilhaço +33% dano";
        if (HasCharm(4)) effect += " • Marca: +25% contra chefes, -20% contra outros inimigos";
        if (HasCharm(6)) effect = "Nó de Lume: Farol dura 8 s e pulsa a cada 0,85 s; recarga +3,5 s";
        if (HasCharm(7)) effect = "Fio do Retorno (Vigília): +40% dano e cura 12% ao abater chefe; dano recebido +10%";
        if (HasCharm(8)) effect = (HasCharm(7) ? effect + " • " : "") + "Eco Convergente (Vigília): +18% de dano de técnica";
        g.DrawString(effect, effectFont, Brushes.AntiqueWhite, new RectangleF(205, 617, 560, 18));
    }
    private void DrawCharacterSheet(Graphics g)
    {
        using var shade = new SolidBrush(Color.FromArgb(190, 5, 7, 9)); using var background = new SolidBrush(Color.FromArgb(248, 15, 17, 18)); using var border = new Pen(Color.FromArgb(175, 143, 112, 73), 2);
        using var heading = new Font("Georgia", 17, FontStyle.Bold); using var section = new Font("Segoe UI", 11, FontStyle.Bold); using var body = new Font("Segoe UI", 10); using var hint = new Font("Segoe UI", 9);
        g.FillRectangle(shade, 0, 83, 960, 557); g.FillRectangle(background, 105, 125, 750, 455); g.DrawRectangle(border, 105, 125, 750, 455);
        DrawCentered(g, "FICHA DO ESCUTADOR", 151, heading, Color.FromArgb(230, 193, 129));
        float critical = CriticalChance(), multiplier = CriticalMultiplier(), cooldown = BasicAttackCooldown();
        float expectedDps = damage * (1 + critical * (multiplier - 1)) / cooldown;
        float mitigation = 1 - ArmorDamageTakenMultiplier() * WeaponDamageTakenMultiplier();
        if (wardTime > 0) mitigation = 1 - (1 - mitigation) * 0.45f;
        g.DrawString("ATAQUE", section, Brushes.Goldenrod, 145, 205);
        g.DrawString($"Arma: {WeaponDisplayName(weapon)}", body, Brushes.AntiqueWhite, 145, 232);
        g.DrawString($"Golpe: {damage:0} dano • intervalo {cooldown:0.00} s • alcance {BasicAttackReach(new Enemy { Kind = EnemyKind.Grunt }):0}", body, Brushes.AntiqueWhite, 145, 258);
        g.DrawString($"Crítico: {critical:P0} de chance • {multiplier:0.00}× dano", body, Brushes.AntiqueWhite, 145, 284);
        g.DrawString($"Dano esperado por segundo: {expectedDps:0.0} (alvo sem armadura)", body, Brushes.AntiqueWhite, 145, 310);
        g.DrawString($"Estilhaço de Voz: {VoiceShardDamage():0} dano • recarga {VoiceShardCooldown():0.0} s", body, Brushes.AntiqueWhite, 145, 336);
        g.DrawString($"Talentos: vigor {healthTalent} • domínio {skillTalent} • agilidade {dodgeTalent} • sintonia {techniqueTalent}", body, Brushes.AntiqueWhite, 145, 362);
        g.DrawString("DEFESA E EQUIPAMENTO", section, Brushes.Goldenrod, 515, 205);
        g.DrawString($"Vida máxima: {maxHealth:0} • poção: +45", body, Brushes.AntiqueWhite, 515, 232);
        g.DrawString($"Redução de dano equipada: {mitigation:P0}{(wardTime > 0 ? " • Manto ativo" : "")}", body, Brushes.AntiqueWhite, 515, 258);
        g.DrawString($"Relicário Vital: cura {RelicKillHealing()} por abate{(armorOn && relicOn ? " • conjunto Vigília do Veio" : "")}", body, Brushes.AntiqueWhite, 145, 388);
        g.DrawString($"Recarga da esquiva: {DashCooldown():0.00} s • duração {DashDuration():0.00} s", body, Brushes.AntiqueWhite, 515, 284);
        g.DrawString($"Afixos: {WeaponAffixDescription(weapon)}", body, Brushes.AntiqueWhite, new RectangleF(515, 310, 315, 48));
        string charmOne = equippedCharm == 0 ? "vazio" : charmNames[equippedCharm]; string charmTwo = equippedCharm2 == 0 ? "vazio" : charmNames[equippedCharm2];
        g.DrawString($"Talismãs: {charmOne} + {charmTwo}", body, Brushes.Plum, new RectangleF(515, 362, 315, 45));
        g.DrawString($"Técnicas: F {ShortSkillName(0)}  •  V {ShortSkillName(1)}  •  G {ShortSkillName(2)}", body, Brushes.AntiqueWhite, new RectangleF(145, 424, 670, 40));
        g.DrawString("Dano esperado considera apenas acerto básico e críticos; não inclui armadura inimiga, chefes ou efeitos temporários.", hint, Brushes.DarkGray, 145, 510);
        g.DrawString("P ou ESC fecha", hint, Brushes.DarkGray, 145, 540);
    }
    private void DrawChoice(Graphics g, string titleText, string a, string b, string c)
    {
        using var bg = new SolidBrush(Color.FromArgb(248, 15, 17, 18)); using var optionFont = new Font("Segoe UI", 12); g.FillRectangle(bg, 170, 205, 620, 225);
        DrawCentered(g, titleText, 238, new Font("Georgia", 15, FontStyle.Bold), Color.Goldenrod);
        g.DrawString(a, optionFont, Brushes.AntiqueWhite, 220, 286); g.DrawString(b, optionFont, Brushes.AntiqueWhite, 220, 330); g.DrawString(c, optionFont, Brushes.AntiqueWhite, 220, 374);
    }

    private void DrawForgeMenu(Graphics g)
    {
        using var shade = new SolidBrush(Color.FromArgb(190, 5, 7, 9)); using var background = new SolidBrush(Color.FromArgb(248, 15, 17, 18)); using var border = new Pen(Color.FromArgb(175, 143, 112, 73), 2);
        using var optionFont = new Font("Segoe UI", 11); using var hintFont = new Font("Segoe UI", 9);
        g.FillRectangle(shade, 0, 83, 960, 557); g.FillRectangle(background, 120, 135, 720, 455); g.DrawRectangle(border, 120, 135, 720, 455);
        DrawCentered(g, weaponRarities[weapon] == 0 ? $"TÊMPERA DO VEIO • {weaponNames[weapon]} VIRA RARA" : $"TÊMPERA DO VEIO • {weaponNames[weapon]}", 170, new Font("Georgia", 16, FontStyle.Bold), Color.Goldenrod);
        g.DrawString("1. TRÉGUA • recupera 3/6 vida por abate", optionFont, Brushes.AntiqueWhite, 165, 225);
        g.DrawString("2. CONTRACANTO • crítico +7/+11 pontos percentuais", optionFont, Brushes.AntiqueWhite, 165, 265);
        g.DrawString("3. MARÉ • dano de técnica +12%/+18%", optionFont, Brushes.AntiqueWhite, 165, 305);
        g.DrawString("4. PASSO CÉLERE • recarga de técnicas -10%/-16%", optionFont, Brushes.AntiqueWhite, 165, 345);
        g.DrawString("5. LASTRO • dano recebido -8%/-12%", optionFont, Brushes.AntiqueWhite, 165, 385);
        g.DrawString("6. FIO ESTICADO • alcance básico +12%/+20%", optionFont, Brushes.AntiqueWhite, 165, 425);
        if (finalEnding > 0 && questStage >= 56)
        {
            int goldCost = TalentRespecGoldCost(level), glassCost = TalentRespecGlassCost(level);
            g.DrawString("7. REDISTRIBUIR TALENTOS • devolve todos os pontos", optionFont, Brushes.AntiqueWhite, 165, 460);
            g.DrawString($"CUSTO: {goldCost} OURO E {glassCost} SAL VÍTREO   •   DISPONÍVEL: {gold} OURO / {tideGlass} SAL", hintFont, gold < goldCost || tideGlass < glassCost ? Brushes.IndianRed : Brushes.Goldenrod, 165, 490);
            g.DrawString("Valores: RARA / RELÍQUIA • 1–6 afixos • 7 redistribui • ESC fecha", hintFont, Brushes.DarkGray, 165, 520);
        }
        else
        {
            g.DrawString("Valores: RARA / RELÍQUIA.", hintFont, Brushes.DarkGray, 165, 460);
            g.DrawString($"CUSTO: 75 OURO E 1 SAL VÍTREO   •   DISPONÍVEL: {gold} OURO / {tideGlass} SAL", hintFont, gold < 75 || tideGlass < 1 ? Brushes.IndianRed : Brushes.Goldenrod, 165, 485);
            g.DrawString("1–6 escolhe o afixo • ESC fecha", hintFont, Brushes.DarkGray, 165, 515);
        }
    }

    private void DrawTalentChoice(Graphics g, int pending)
    {
        using var background = new SolidBrush(Color.FromArgb(248, 15, 17, 18));
        using var optionFont = new Font("Segoe UI", 12);
        using var hintBrush = new SolidBrush(Color.FromArgb(165, 174, 168));
        g.FillRectangle(background, 170, 185, 620, 270);
        DrawCentered(g, $"SUBIU DE NÍVEL — ESCOLHA ({pending} PENDENTES)", 212, new Font("Georgia", 15, FontStyle.Bold), Color.Goldenrod);
        g.DrawString("1. VIGOR  +20 VIDA MÁXIMA", optionFont, Brushes.AntiqueWhite, 220, 258);
        g.DrawString("2. DOMÍNIO  técnicas recarregam mais rápido", optionFont, Brushes.AntiqueWhite, 220, 300);
        g.DrawString("3. AGILIDADE  esquiva recarrega mais rápido", optionFont, Brushes.AntiqueWhite, 220, 342);
        g.DrawString("4. SINTONIA  +8% de dano de técnica por ponto", optionFont, Brushes.AntiqueWhite, 220, 384);
        g.DrawString("Cada escolha é permanente e pode ser repetida.", optionFont, hintBrush, 220, 424);
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
    private static int EnemySpriteRow(EnemyKind kind) => kind switch { EnemyKind.Grunt => 0, EnemyKind.Archer => 1, EnemyKind.Mage => 2, EnemyKind.Elite => 3, _ => -1 };
    private static int CampaignBossSpriteIndex(EnemyKind kind) => kind switch { EnemyKind.Guardian => 0, EnemyKind.BlindScribe => 1, EnemyKind.RiverFerryman => 2, EnemyKind.AshWarden => 3, EnemyKind.HollowName => 4, EnemyKind.NameWeaver => 5, EnemyKind.FirstKeeper => 6, EnemyKind.OathCartographer => 7, EnemyKind.NameBroker => 8, EnemyKind.TideWitness => 9, EnemyKind.ReedKeeper => 10, EnemyKind.FlowKeeper => 11, EnemyKind.CounterCurrent => 12, EnemyKind.RiftCantor => 13, EnemyKind.GlassHerald => 14, EnemyKind.LanternRegent => 15, EnemyKind.RouteScribe => 16, EnemyKind.DeepSilence => 17, _ => -1 };
    private static int EnemySpriteFrame(float animationTime, bool walking) => walking ? (int)(Math.Max(0, animationTime) / 0.13f) % 4 : 0;
    private static bool EnemyFacesLeft(float enemyX, float playerX) => playerX < enemyX;
    private static string EnemyName(Enemy e) => e.VigilMinion ? "ECO ERRANTE • SALINO" : e.Kind switch { EnemyKind.Guardian => "GUARDIÃO SEM NOME", EnemyKind.BlindScribe => "ESCRIBA CEGO", EnemyKind.RiverFerryman => "BARQUEIRO OCO", EnemyKind.AshWarden => "GUARDIÃO DAS CINZAS", EnemyKind.HollowName => "O NOME VAZIO", EnemyKind.NameWeaver => "A TECELÃ DE NOMES", EnemyKind.FirstKeeper => "A PRIMEIRA VIGÍLIA", EnemyKind.OathCartographer => "CARTÓGRAFA DO VÃO", EnemyKind.NameBroker => "ESCRIVÃO DE CONTRATO", EnemyKind.SaltEcho => "ECO SALINO", EnemyKind.TideWitness => "TESTEMUNHA DA MARÉ", EnemyKind.ReedKeeper => "GUARDIÃ DO LEITO", EnemyKind.FlowKeeper => "VIGIA DO VEIO PRESO", EnemyKind.CounterCurrent => "AFINADOR DAS MARGENS", EnemyKind.RiftCantor => "A CONTRAVOZ", EnemyKind.GlassHerald => "ARAUTO DE VIDRO", EnemyKind.LanternRegent => "REGENTE DAS LANTERNAS", EnemyKind.RouteScribe => "ESCRIBA DO TRAÇADO", EnemyKind.DeepSilence => "SILÊNCIO DE BAIXO", EnemyKind.BorrowedChorus => "ARQUIVISTA DAS MARÉS", EnemyKind.PaperWraith => "ESPECTRO DE PAPEL", EnemyKind.MudCantor => "CANTOR DO LODO", EnemyKind.DrownedMemory => "LEMBRANÇA AFOGADA", EnemyKind.EmberWisp => "VESTÍGIO DE BRASAS", EnemyKind.NameMoth => "MARIPOSA DE NOMES", EnemyKind.Elite => $"ELITE • {EliteAffixName(e.EliteAffix)}", EnemyKind.Archer => "ARQUEIRO", EnemyKind.Mage => "CONJURADOR", EnemyKind.InkEcho => "ECO DE TINTA", _ => "" };
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
        GameAudio.SilenceForAutomatedRun();
        (Color normalFill, Color normalEdge) = AttackWarningPalette(false);
        (Color contrastFill, Color contrastEdge) = AttackWarningPalette(true);
        if (normalEdge == contrastEdge || contrastFill.R < 240 || contrastFill.G < 190 || contrastEdge.R < 240 || contrastEdge.G < 220) throw new InvalidOperationException("A paleta acessível dos avisos não tem contraste suficiente em relação ao modo padrão.");
        using (var warningAccessibilityCheck = new Game())
        {
            warningAccessibilityCheck.timer.Stop(); warningAccessibilityCheck.StartGame();
            warningAccessibilityCheck.OnKeyDown(warningAccessibilityCheck, new KeyEventArgs(Keys.H));
            SaveData accessibleSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(warningAccessibilityCheck.CaptureSaveData()))!;
            if (!accessibleSave.HighContrastTelegraphs) throw new InvalidOperationException("A preferência de alto contraste não foi ativada ou salva.");
            warningAccessibilityCheck.OnKeyDown(warningAccessibilityCheck, new KeyEventArgs(Keys.H));
            if (warningAccessibilityCheck.CaptureSaveData().HighContrastTelegraphs) throw new InvalidOperationException("A preferência de alto contraste não pôde ser desativada.");
        }
        using (var targetCheck = new Game())
        {
            targetCheck.timer.Stop(); targetCheck.StartGame(); targetCheck.enemies.Clear(); targetCheck.drops.Clear(); targetCheck.player = new PointF(150, 330);
            Enemy distant = new() { Kind = EnemyKind.Grunt, Pos = new PointF(520, 330), Hp = 1, MaxHp = 1 };
            Enemy nearest = new() { Kind = EnemyKind.Grunt, Pos = new PointF(230, 330), Hp = 1, MaxHp = 1 };
            Enemy next = new() { Kind = EnemyKind.Grunt, Pos = new PointF(320, 330), Hp = 1, MaxHp = 1 };
            targetCheck.enemies.AddRange(new[] { distant, nearest, next }); targetCheck.lockedTarget = distant; targetCheck.Defeat(distant);
            if (targetCheck.lockedTarget != nearest) throw new InvalidOperationException("O alvo marcado não mudou para o inimigo vivo mais próximo após ser derrotado.");
            targetCheck.Defeat(nearest);
            if (targetCheck.lockedTarget != next) throw new InvalidOperationException("O bloqueio não acompanhou a derrota sucessiva do alvo.");
            targetCheck.Defeat(next);
            if (targetCheck.lockedTarget is not null) throw new InvalidOperationException("O bloqueio manteve um alvo quando não restavam inimigos.");
        }
        using (var interactionRadiusCheck = new Game())
        {
            interactionRadiusCheck.timer.Stop(); interactionRadiusCheck.StartGame(); interactionRadiusCheck.storyOverlay = false; interactionRadiusCheck.enemies.Clear(); interactionRadiusCheck.drops.Clear();
            interactionRadiusCheck.room = 3; interactionRadiusCheck.questStage = 0; interactionRadiusCheck.player = Offset(CuratorPosition, 0, 68); interactionRadiusCheck.InteractTown();
            if (interactionRadiusCheck.questStage != 1) throw new InvalidOperationException("O aviso de conversa com Sileth apareceu antes de a interação funcionar.");
            interactionRadiusCheck.questStage = 1; interactionRadiusCheck.player = Offset(ArchiveBookPosition, 0, 68); interactionRadiusCheck.InteractTown();
            if (interactionRadiusCheck.questStage != 2) throw new InvalidOperationException("O aviso para ler o Livro das Ausências apareceu antes de a interação funcionar.");
            interactionRadiusCheck.questStage = 3; interactionRadiusCheck.player = new PointF(480, 203); interactionRadiusCheck.InteractTown();
            if (interactionRadiusCheck.room != 4) throw new InvalidOperationException("O aviso do portão do Arquivo Norte apareceu antes de a interação funcionar.");
            interactionRadiusCheck.room = 18; interactionRadiusCheck.questStage = 21; interactionRadiusCheck.player = Offset(AvenPosition, 68, 0); interactionRadiusCheck.InteractRefuge();
            if (interactionRadiusCheck.questStage != 22) throw new InvalidOperationException("O aviso para falar com Aven apareceu antes de a interação funcionar.");
            interactionRadiusCheck.room = 0; interactionRadiusCheck.memoriesFound = 0; interactionRadiusCheck.enemies.Clear(); interactionRadiusCheck.drops.Clear(); interactionRadiusCheck.player = Offset(MemoryStonePositions[0], 0, 55); interactionRadiusCheck.Interact();
            if ((interactionRadiusCheck.memoriesFound & 1) == 0) throw new InvalidOperationException("O aviso da inscrição apareceu antes de a interação funcionar.");
        }
        if (ScreenToGame(new PointF(500, 320), new Size(1000, 640)) != new PointF(480, 320)
            || ScreenToGame(new PointF(960, 540), new Size(1920, 1080)) != new PointF(480, 320)
            || ScreenToGame(new PointF(480, 400), new Size(960, 800)) != new PointF(480, 320)
            || ScreenToGame(new PointF(0, 0), new Size(1920, 1080)) != new PointF(0, 0))
            throw new InvalidOperationException("A ampliação da janela distorceu o campo de jogo ou deslocou a mira do mouse.");
        using (var attackArcCheck = new Game())
        {
            attackArcCheck.timer.Stop(); attackArcCheck.StartGame(); attackArcCheck.storyOverlay = false; attackArcCheck.player = new PointF(150, 330); attackArcCheck.mouseAim = attackArcCheck.facing = new PointF(1, 0);
            for (int weaponIndex = 0; weaponIndex < 3; weaponIndex++)
            {
                attackArcCheck.weapon = weaponIndex; float halfAngle = BasicAttackSweepAngle(weaponIndex) * MathF.PI / 360f;
                if (Math.Abs(BasicAttackArcThreshold(weaponIndex) - MathF.Cos(halfAngle)) > 0.0001f) throw new InvalidOperationException("O ângulo do golpe visual divergiu do limite do ataque.");
                var outside = new Enemy { Kind = EnemyKind.Grunt, Pos = new PointF(150 + MathF.Cos(halfAngle + 0.08f) * 40, 330 + MathF.Sin(halfAngle + 0.08f) * 40), Hp = 1000, MaxHp = 1000 };
                attackArcCheck.attackCd = 0; attackArcCheck.enemies.Clear(); attackArcCheck.enemies.Add(outside); attackArcCheck.Attack();
                if (outside.Hp != 1000) throw new InvalidOperationException($"A arma {weaponIndex} acertou fora do arco desenhado.");
                var inside = new Enemy { Kind = EnemyKind.Grunt, Pos = new PointF(150 + MathF.Cos(halfAngle - 0.08f) * 40, 330 + MathF.Sin(halfAngle - 0.08f) * 40), Hp = 1000, MaxHp = 1000 };
                attackArcCheck.attackCd = 0; attackArcCheck.enemies.Clear(); attackArcCheck.enemies.Add(inside); attackArcCheck.Attack();
                if (inside.Hp >= 1000) throw new InvalidOperationException($"A arma {weaponIndex} não acertou dentro do arco desenhado.");
            }
        }
        using (var objectiveCheck = new Game())
        {
            objectiveCheck.timer.Stop(); objectiveCheck.StartGame(); objectiveCheck.storyOverlay = false; objectiveCheck.room = 33; objectiveCheck.enemies.Clear(); objectiveCheck.drops.Clear(); objectiveCheck.enemies.Add(new Enemy());
            if (objectiveCheck.LanternObjective() != "OBJETIVO: ATRAVESSE AS LANTERNAS BAIXAS") throw new InvalidOperationException("O objetivo da Passagem das Lanternas não acompanhou o combate.");
            objectiveCheck.enemies.Clear();
            if (objectiveCheck.LanternObjective() != "OBJETIVO: SIGA ATÉ O OBSERVATÓRIO") throw new InvalidOperationException("O objetivo da Passagem das Lanternas não indicou a saída após a limpeza.");
            objectiveCheck.room = 34;
            if (objectiveCheck.LanternObjective() != "OBJETIVO: DERROTE O REGENTE E RECUPERE O SINAL") throw new InvalidOperationException("O objetivo do Observatório não indicou o confronto contra o Regente.");
            objectiveCheck.lanternSignal = true;
            if (objectiveCheck.LanternObjective() != "OBJETIVO: LEVE O SINAL DA LUZ A AVEN") throw new InvalidOperationException("O objetivo do Observatório não atualizou após recuperar o sinal.");
        }
        using (var focusCheck = new Game())
        {
            focusCheck.timer.Stop(); focusCheck.StartGame(); focusCheck.storyOverlay = false; focusCheck.room = 3; focusCheck.SetupRoom(); focusCheck.player = new PointF(500, 330); focusCheck.skillCd = 1; focusCheck.down.Add(Keys.W); focusCheck.dashTime = 0.1f;
            focusCheck.PauseForLostFocus(); focusCheck.TickGame();
            if (focusCheck.player != new PointF(500, 330) || focusCheck.skillCd != 1 || focusCheck.down.Count != 0 || focusCheck.dashTime != 0) throw new InvalidOperationException("A perda de foco não pausou a partida ou manteve uma entrada presa.");
            focusCheck.ResumeAfterFocus(); focusCheck.TickGame();
            if (focusCheck.player != new PointF(500, 330) || focusCheck.skillCd >= 1) throw new InvalidOperationException("A retomada de foco não preservou a posição ou não retomou o combate.");
        }
        foreach (EnemyKind kind in new[] { EnemyKind.Guardian, EnemyKind.BlindScribe, EnemyKind.RiverFerryman, EnemyKind.AshWarden, EnemyKind.HollowName, EnemyKind.NameWeaver, EnemyKind.FirstKeeper, EnemyKind.OathCartographer, EnemyKind.NameBroker, EnemyKind.TideWitness, EnemyKind.ReedKeeper, EnemyKind.FlowKeeper, EnemyKind.CounterCurrent, EnemyKind.RiftCantor, EnemyKind.GlassHerald, EnemyKind.LanternRegent, EnemyKind.RouteScribe, EnemyKind.DeepSilence })
        {
            for (int pattern = 0; pattern < 3; pattern++)
            {
                var boss = new Enemy { Kind = kind, BossPattern = pattern, Pos = new PointF(300, 300), Target = new PointF(500, 300), Target2Start = new PointF(300, 400), Target2 = new PointF(500, 400) };
                if (BossUsesCone(boss))
                {
                    if (!BossAttackContains(boss, new PointF(480, 300)) || BossAttackContains(boss, new PointF(400, 450)) || BossAttackContains(boss, new PointF(560, 300))) throw new InvalidOperationException("O cone do Arauto de Vidro não respeitou seu alcance ou abertura lateral.");
                }
                else if (BossUsesRing(boss))
                {
                    if (BossAttackContains(boss, boss.Pos) || !BossAttackContains(boss, new PointF(400, 300)) || BossAttackContains(boss, new PointF(461, 300))) throw new InvalidOperationException("A onda do Escriba do Traçado não preservou o miolo seguro nem o limite externo.");
                }
                else if (BossUsesLine(boss))
                {
                    float width = kind == EnemyKind.FirstKeeper ? 25 : 24;
                    if (!BossAttackContains(boss, new PointF(400, 300 + width - 1)) || BossAttackContains(boss, new PointF(400, 300 + width + 1))) throw new InvalidOperationException($"A largura do ataque de {kind} não corresponde ao aviso.");
                    if (kind is EnemyKind.FirstKeeper or EnemyKind.DeepSilence && !BossAttackContains(boss, new PointF(400, 400))) throw new InvalidOperationException($"A segunda linha de {kind} não causa dano.");
                }
                else
                {
                    float radius = BossCircleRadius(boss); PointF center = pattern == 0 ? boss.Pos : boss.Target;
                    if (!BossAttackContains(boss, new PointF(center.X + radius - 1, center.Y)) || BossAttackContains(boss, new PointF(center.X + radius + 1, center.Y))) throw new InvalidOperationException($"O círculo de {kind} não corresponde ao impacto.");
                }
            }
        }
        using (var rangedCheck = new Game())
        {
            rangedCheck.timer.Stop(); rangedCheck.StartGame(); rangedCheck.storyOverlay = false; rangedCheck.enemies.Clear();
            foreach (EnemyKind kind in new[] { EnemyKind.Archer, EnemyKind.InkEcho, EnemyKind.DrownedMemory, EnemyKind.EmberWisp, EnemyKind.NameMoth, EnemyKind.MudCantor, EnemyKind.PaperWraith, EnemyKind.SaltEcho })
            {
                float radius = RangedImpactRadius(kind);
                var attacker = new Enemy { Kind = kind, Pos = new PointF(300, 330), Target = new PointF(500, 330), Windup = 0.01f, WindupMax = 0.5f, Hp = 100, MaxHp = 100 };
                rangedCheck.health = rangedCheck.maxHealth = 1000; rangedCheck.invulnerable = rangedCheck.dashTime = 0; rangedCheck.player = new PointF(500 + radius + 1, 330);
                rangedCheck.UpdateEnemy(attacker, 0.02f);
                if (rangedCheck.health != 1000) throw new InvalidOperationException($"{kind} atingiu fora do círculo anunciado.");
                attacker.Windup = 0.01f; rangedCheck.player = new PointF(500 + radius - 1, 330); rangedCheck.UpdateEnemy(attacker, 0.02f);
                if (rangedCheck.health >= 1000) throw new InvalidOperationException($"{kind} não atingiu dentro do círculo anunciado.");
            }
        }
        using (var meleeCheck = new Game())
        {
            meleeCheck.timer.Stop(); meleeCheck.StartGame(); meleeCheck.storyOverlay = false; meleeCheck.enemies.Clear();
            foreach (EnemyKind kind in new[] { EnemyKind.Grunt, EnemyKind.Elite, EnemyKind.Mage })
            {
                var attacker = new Enemy { Kind = kind, Pos = new PointF(300, 330), Target = new PointF(500, 330), Windup = 0.01f, WindupMax = 0.5f, Hp = 100, MaxHp = 100 };
                PointF center = NonBossAttackCenter(attacker); float radius = NonBossAttackRadius(attacker);
                meleeCheck.health = meleeCheck.maxHealth = 1000; meleeCheck.invulnerable = meleeCheck.dashTime = 0; meleeCheck.player = new PointF(center.X + radius + 1, center.Y);
                meleeCheck.UpdateEnemy(attacker, 0.02f);
                if (meleeCheck.health != 1000) throw new InvalidOperationException($"{kind} atingiu fora do círculo anunciado.");
                attacker.Windup = 0.01f; meleeCheck.player = new PointF(center.X + radius - 1, center.Y); meleeCheck.UpdateEnemy(attacker, 0.02f);
                if (meleeCheck.health >= 1000) throw new InvalidOperationException($"{kind} não atingiu dentro do círculo anunciado.");
            }
        }
        string saveCheckDirectory = Path.Combine(Path.GetTempPath(), "Scadryk-save-check-" + Guid.NewGuid().ToString("N"));
        string saveCheckPath = Path.Combine(saveCheckDirectory, "save.json");
        try
        {
            WriteSaveFile(saveCheckPath, "primeiro");
            WriteSaveFile(saveCheckPath, "segundo");
            if (File.ReadAllText(saveCheckPath) != "segundo") throw new InvalidOperationException("O save não substituiu o progresso anterior.");
            using (var lockedSave = new FileStream(saveCheckPath, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                bool rejected = false;
                try { WriteSaveFile(saveCheckPath, "não deve substituir"); } catch (IOException) { rejected = true; } catch (UnauthorizedAccessException) { rejected = true; }
                if (!rejected) throw new InvalidOperationException("O save bloqueado não rejeitou a substituição.");
                using var failedSaveGame = new Game(); failedSaveGame.timer.Stop(); failedSaveGame.StartGame();
                if (failedSaveGame.SaveGame(saveCheckPath) || !failedSaveGame.notice.Contains("NÃO FOI POSSÍVEL SALVAR")) throw new InvalidOperationException("A falha de salvar não foi comunicada ao jogador.");
            }
            if (File.ReadAllText(saveCheckPath) != "segundo" || Directory.GetFiles(saveCheckDirectory).Length != 1) throw new InvalidOperationException("Uma falha de gravação alterou o save anterior ou deixou arquivo temporário.");
        }
        finally { if (File.Exists(saveCheckPath)) File.Delete(saveCheckPath); if (Directory.Exists(saveCheckDirectory)) Directory.Delete(saveCheckDirectory); }
        string spritePath = Path.Combine(AppContext.BaseDirectory, "Assets", "Aven.png");
        if (!File.Exists(spritePath)) throw new InvalidOperationException("A folha original de sprites de Aven não foi copiada para a saída do jogo.");
        using (var sprite = new Bitmap(spritePath))
            if (sprite.Width != sprite.Height || sprite.GetPixel(0, 0).A != 0) throw new InvalidOperationException("A folha de sprites de Aven precisa ter células quadradas e fundo transparente.");
        string enemySpritePath = Path.Combine(AppContext.BaseDirectory, "Assets", "Enemies.png");
        if (!File.Exists(enemySpritePath)) throw new InvalidOperationException("A folha de inimigos não foi copiada para a saída do jogo.");
        using (var sprite = new Bitmap(enemySpritePath))
            if (sprite.Width != sprite.Height || sprite.GetPixel(0, 0).A != 0) throw new InvalidOperationException("A folha de inimigos precisa ter células quadradas e fundo transparente.");
        if (EnemySpriteRow(EnemyKind.Grunt) != 0 || EnemySpriteRow(EnemyKind.Archer) != 1 || EnemySpriteRow(EnemyKind.Mage) != 2 || EnemySpriteRow(EnemyKind.Elite) != 3 || EnemySpriteFrame(0.52f, false) != 0 || EnemySpriteFrame(0.52f, true) != 0 || EnemySpriteFrame(0.66f, true) != 1 || !EnemyFacesLeft(400, 300) || EnemyFacesLeft(300, 400))
            throw new InvalidOperationException("Os arquétipos e o ciclo de caminhada dos sprites de inimigos não foram mapeados corretamente.");
        using (var proceduralArt = new Bitmap(96, 96))
        using (var proceduralGraphics = Graphics.FromImage(proceduralArt))
        {
            proceduralGraphics.SmoothingMode = SmoothingMode.AntiAlias;
            foreach (EnemyKind kind in new[] { EnemyKind.InkEcho, EnemyKind.DrownedMemory, EnemyKind.EmberWisp, EnemyKind.NameMoth, EnemyKind.MudCantor, EnemyKind.PaperWraith, EnemyKind.SaltEcho, EnemyKind.BorrowedChorus })
            {
                proceduralGraphics.Clear(Color.Transparent);
                if (!DrawProceduralEnemy(proceduralGraphics, new Enemy { Kind = kind, Pos = new PointF(48, 48) }, Color.SlateGray)) throw new InvalidOperationException($"A silhueta procedural de {kind} não foi desenhada.");
                int visiblePixels = 0;
                for (int py = 0; py < proceduralArt.Height; py++) for (int px = 0; px < proceduralArt.Width; px++) if (proceduralArt.GetPixel(px, py).A > 0) visiblePixels++;
                if (visiblePixels < 25) throw new InvalidOperationException($"A silhueta de {kind} ficou invisível na renderização 2D.");
            }
        }
        string bossSpritePath = Path.Combine(AppContext.BaseDirectory, "Assets", "Bosses.png");
        if (!File.Exists(bossSpritePath)) throw new InvalidOperationException("A folha de chefes da campanha não foi copiada para a saída do jogo.");
        using (var sprite = new Bitmap(bossSpritePath))
            if (sprite.Width != sprite.Height || sprite.GetPixel(0, 0).A != 0) throw new InvalidOperationException("A folha de chefes precisa ter células quadradas e fundo transparente.");
        if (CampaignBossSpriteIndex(EnemyKind.Guardian) != 0 || CampaignBossSpriteIndex(EnemyKind.BlindScribe) != 1 || CampaignBossSpriteIndex(EnemyKind.RiverFerryman) != 2 || CampaignBossSpriteIndex(EnemyKind.AshWarden) != 3 || CampaignBossSpriteIndex(EnemyKind.HollowName) != 4 || CampaignBossSpriteIndex(EnemyKind.NameWeaver) != 5 || CampaignBossSpriteIndex(EnemyKind.FirstKeeper) != 6 || CampaignBossSpriteIndex(EnemyKind.OathCartographer) != 7 || CampaignBossSpriteIndex(EnemyKind.NameBroker) != 8 || CampaignBossSpriteIndex(EnemyKind.TideWitness) != 9 || CampaignBossSpriteIndex(EnemyKind.ReedKeeper) != 10 || CampaignBossSpriteIndex(EnemyKind.FlowKeeper) != 11 || CampaignBossSpriteIndex(EnemyKind.CounterCurrent) != 12 || CampaignBossSpriteIndex(EnemyKind.RiftCantor) != 13 || CampaignBossSpriteIndex(EnemyKind.GlassHerald) != 14 || CampaignBossSpriteIndex(EnemyKind.LanternRegent) != 15 || CampaignBossSpriteIndex(EnemyKind.RouteScribe) != 16 || CampaignBossSpriteIndex(EnemyKind.DeepSilence) != 17)
            throw new InvalidOperationException("As silhuetas dos chefes da campanha foram atribuídas incorretamente.");
        string midBossSpritePath = Path.Combine(AppContext.BaseDirectory, "Assets", "BossesMidgame.png");
        if (!File.Exists(midBossSpritePath)) throw new InvalidOperationException("A folha de chefes intermediários não foi copiada para a saída do jogo.");
        using (var sprite = new Bitmap(midBossSpritePath))
            if (sprite.Width != sprite.Height || sprite.GetPixel(0, 0).A != 0) throw new InvalidOperationException("A folha de chefes intermediários precisa ter células quadradas e fundo transparente.");
        string lateBossSpritePath = Path.Combine(AppContext.BaseDirectory, "Assets", "BossesLate.png");
        if (!File.Exists(lateBossSpritePath)) throw new InvalidOperationException("A folha de chefes avançados não foi copiada para a saída do jogo.");
        using (var sprite = new Bitmap(lateBossSpritePath))
            if (sprite.Width != sprite.Height || sprite.GetPixel(0, 0).A != 0) throw new InvalidOperationException("A folha de chefes avançados precisa ter células quadradas e fundo transparente.");
        string finalWaveBossSpritePath = Path.Combine(AppContext.BaseDirectory, "Assets", "BossesFinalWave.png");
        if (!File.Exists(finalWaveBossSpritePath)) throw new InvalidOperationException("A folha dos chefes finais não foi copiada para a saída do jogo.");
        using (var sprite = new Bitmap(finalWaveBossSpritePath))
            if (sprite.Width != sprite.Height || sprite.GetPixel(0, 0).A != 0) throw new InvalidOperationException("A folha dos chefes finais precisa ter células quadradas e fundo transparente.");
        string endgameBossSpritePath = Path.Combine(AppContext.BaseDirectory, "Assets", "BossesEndgame.png");
        if (!File.Exists(endgameBossSpritePath)) throw new InvalidOperationException("A folha do confronto final não foi copiada para a saída do jogo.");
        using (var sprite = new Bitmap(endgameBossSpritePath))
            if (sprite.Width != sprite.Height || sprite.GetPixel(0, 0).A != 0) throw new InvalidOperationException("A folha do confronto final precisa ter células quadradas e fundo transparente.");
        if (PlayerSpriteRow(new PointF(0, 1)) != 0 || PlayerSpriteRow(new PointF(-1, 0)) != 1 || PlayerSpriteRow(new PointF(1, 0)) != 2 || PlayerSpriteRow(new PointF(0, -1)) != 3 || PlayerSpriteFrame(0.5f, false) != 0 || PlayerSpriteFrame(0.5f, true) != 0 || PlayerSpriteFrame(0.61f, true) != 1)
            throw new InvalidOperationException("A orientação ou o ciclo de caminhada da folha de sprites não foi mapeado corretamente.");
        using (var weaponCombat = new Game())
        {
            weaponCombat.timer.Stop(); weaponCombat.StartGame(); weaponCombat.storyOverlay = false; weaponCombat.room = 0; weaponCombat.SetupRoom(); weaponCombat.enemies.Clear(); weaponCombat.damage = 100; weaponCombat.player = new PointF(150, 330); weaponCombat.mouseAim = new PointF(1, 0); weaponCombat.weapon = 1;
            Enemy platedEcho = new() { Pos = new PointF(245, 330), Kind = EnemyKind.Grunt, Hp = 1000, MaxHp = 1000, Armored = true };
            weaponCombat.enemies.Add(platedEcho); weaponCombat.Attack();
            if (platedEcho.Hp != 910 && platedEcho.Hp != 820 || platedEcho.Stagger <= 0 || weaponCombat.attackFlash <= 0 || weaponCombat.BasicAttackDamageMultiplier(platedEcho) != 0.9f || weaponCombat.BasicAttackCooldown() != 0.52f || weaponCombat.BasicAttackReach(platedEcho) != 108f) throw new InvalidOperationException("A Lâmina do Arquivo não alcançou, perfurou a armadura, interrompeu o eco e iniciou a animação do golpe.");
            string shownHit = weaponCombat.floatingTexts.Single().Text;
            if (shownHit != (platedEcho.Hp == 820 ? "CRÍTICO 180" : "90")) throw new InvalidOperationException("O número de dano não corresponde ao golpe nem identifica o crítico.");
            weaponCombat.attackCd = 0; weaponCombat.weapon = 0; platedEcho.Hp = 1000; platedEcho.Stagger = 0; platedEcho.Pos = new PointF(245, 330); weaponCombat.Attack();
            if (platedEcho.Hp != 1000 || weaponCombat.BasicAttackReach(platedEcho) != 78f) throw new InvalidOperationException("A Espada de Pedra não manteve seu alcance equilibrado.");
            weaponCombat.attackCd = 0; weaponCombat.weapon = 2; platedEcho.Pos = new PointF(215, 330); weaponCombat.Attack();
            if (platedEcho.Hp >= 1000 || weaponCombat.BasicAttackCooldown() != 0.23f || weaponCombat.CriticalChance() != 0.25f || weaponCombat.WeaponTraitDescription(2) != "FACA RÁPIDA • CURTO ALCANCE • 25% DE CRÍTICO") throw new InvalidOperationException("A Faca do Crepúsculo não manteve seu alcance curto, velocidade e crítico elevados.");
            if (!weaponCombat.WeaponTraitDescription(1).Contains("PERFURA ARMADURA") || !weaponCombat.WeaponTraitDescription(0).Contains("EQUILIBRADA")) throw new InvalidOperationException("A descrição das especializações de arma não corresponde ao combate.");
        }
        using (var floatingTextCheck = new Game())
        {
            floatingTextCheck.timer.Stop(); floatingTextCheck.StartGame(); floatingTextCheck.storyOverlay = false;
            var target = new Enemy { Pos = new PointF(500, 300), Kind = EnemyKind.Grunt, Hp = 100, MaxHp = 100 };
            floatingTextCheck.DamageEnemy(target, 37, true); floatingTextCheck.HitPlayer(12);
            if (target.Hp != 63 || floatingTextCheck.health != 88 || floatingTextCheck.floatingTexts.Count != 2 || floatingTextCheck.floatingTexts[0].Text != "CRÍTICO 37" || floatingTextCheck.floatingTexts[1].Text != "−12" || floatingTextCheck.floatingTexts[1].Color != Color.FromArgb(250, 116, 105))
                throw new InvalidOperationException("Os números flutuantes não distinguiram dano crítico de dano recebido.");
            if (PlayerDamageFlashAlpha(0, 0) != 0 || PlayerDamageFlashAlpha(floatingTextCheck.invulnerable, floatingTextCheck.windupPulse) is < 70 or > 125)
                throw new InvalidOperationException("O clarão de dano do jogador não acompanhou a janela de invulnerabilidade.");
            float initialY = floatingTextCheck.floatingTexts[0].Pos.Y; floatingTextCheck.TickFloatingTexts(0.3f);
            if (floatingTextCheck.floatingTexts.Count != 2 || floatingTextCheck.floatingTexts[0].Pos.Y >= initialY) throw new InvalidOperationException("Os números de dano não subiram e permaneceram visíveis durante o impacto.");
            floatingTextCheck.TickFloatingTexts(1f);
            if (floatingTextCheck.floatingTexts.Count != 0) throw new InvalidOperationException("Os números flutuantes não desapareceram depois da animação.");
        }
        using (var dodgeDirectionCheck = new Game())
        {
            dodgeDirectionCheck.timer.Stop(); dodgeDirectionCheck.StartGame(); dodgeDirectionCheck.storyOverlay = false; dodgeDirectionCheck.room = 3; dodgeDirectionCheck.SetupRoom(); dodgeDirectionCheck.player = new PointF(500, 330); dodgeDirectionCheck.down.Add(Keys.D); dodgeDirectionCheck.down.Add(Keys.W);
            dodgeDirectionCheck.StartDash(); PointF expectedDirection = Normalize(new PointF(1, -1), PointF.Empty); dodgeDirectionCheck.TickGame(); PointF diagonalDash = dodgeDirectionCheck.dashDirection, diagonalPosition = dodgeDirectionCheck.player;
            if (Math.Abs(diagonalDash.X - expectedDirection.X) > 0.001f || Math.Abs(diagonalDash.Y - expectedDirection.Y) > 0.001f || diagonalPosition.X <= 505 || diagonalPosition.Y >= 325)
                throw new InvalidOperationException("A esquiva diagonal não seguiu o movimento indicado pelo jogador.");
            dodgeDirectionCheck.down.Clear(); dodgeDirectionCheck.dashCd = 0; dodgeDirectionCheck.facing = new PointF(-1, 0); dodgeDirectionCheck.StartDash();
            PointF fallbackDash = dodgeDirectionCheck.dashDirection;
            if (Math.Abs(fallbackDash.X + 1) > 0.001f || Math.Abs(fallbackDash.Y) > 0.001f)
                throw new InvalidOperationException("A esquiva sem direção de movimento não usou a direção para a qual o personagem olha.");
        }
        using (var atlasCheck = new Game())
        {
            atlasCheck.timer.Stop(); atlasCheck.StartGame(); atlasCheck.storyOverlay = false; atlasCheck.room = 4; atlasCheck.questStage = 3;
            atlasCheck.enemies.Clear(); atlasCheck.clearedRooms = (1L << 0) | (1L << 1);
            var pausedEnemy = new Enemy { Pos = new PointF(700, 400), Kind = EnemyKind.Grunt, Hp = 100, MaxHp = 100, Stagger = 0.2f };
            atlasCheck.enemies.Add(pausedEnemy); PointF playerBeforeMap = atlasCheck.player; atlasCheck.skillCd = 1; atlasCheck.wardTime = 1;
            atlasCheck.OnKeyDown(atlasCheck, new KeyEventArgs(Keys.M)); long knownRooms = atlasCheck.KnownRoomMask(); atlasCheck.TickGame();
            atlasCheck.OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, 700, 400, 0));
            long expectedKnown = (1L << 0) | (1L << 1) | (1L << 3) | (1L << 4);
            if (!atlasCheck.mapOpen || (knownRooms & expectedKnown) != expectedKnown || (knownRooms & (1L << 5)) != 0 || atlasCheck.player != playerBeforeMap || pausedEnemy.Stagger != 0.2f || atlasCheck.skillCd != 1 || atlasCheck.wardTime != 1 || atlasCheck.attackCd != 0)
                throw new InvalidOperationException("O atlas não distinguiu câmaras conhecidas ou não pausou a exploração.");
            atlasCheck.OnKeyDown(atlasCheck, new KeyEventArgs(Keys.Escape));
            if (atlasCheck.mapOpen) throw new InvalidOperationException("ESC não fechou o atlas da campanha.");
            atlasCheck.TickGame(); if (atlasCheck.skillCd >= 1 || atlasCheck.wardTime >= 1) throw new InvalidOperationException("O combate não retomou os temporizadores ao fechar o atlas.");
        }
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
        using (var skillLoadoutGame = new Game())
        {
            skillLoadoutGame.timer.Stop(); skillLoadoutGame.StartGame(); skillLoadoutGame.storyOverlay = false;
            skillLoadoutGame.skillMenuOpen = true; skillLoadoutGame.OnKeyDown(skillLoadoutGame, new KeyEventArgs(Keys.V)); skillLoadoutGame.OnKeyDown(skillLoadoutGame, new KeyEventArgs(Keys.D2));
            if (skillLoadoutGame.equippedSkills[1] != 0) throw new InvalidOperationException("Uma técnica bloqueada foi equipada antes de ser aprendida.");
            skillLoadoutGame.room = 6; skillLoadoutGame.riverBell = true; skillLoadoutGame.OnKeyDown(skillLoadoutGame, new KeyEventArgs(Keys.V)); skillLoadoutGame.OnKeyDown(skillLoadoutGame, new KeyEventArgs(Keys.D2)); skillLoadoutGame.skillMenuOpen = false;
            skillLoadoutGame.enemies.Clear(); var loadoutEcho = new Enemy { Pos = new PointF(210, 330), Kind = EnemyKind.Grunt, Hp = 1000, MaxHp = 1000 }; skillLoadoutGame.enemies.Add(loadoutEcho);
            skillLoadoutGame.player = new PointF(150, 330); skillLoadoutGame.CastEquippedSkill(1);
            if (skillLoadoutGame.equippedSkills[1] != 2 || skillLoadoutGame.bellCd <= 0 || loadoutEcho.Hp >= 1000) throw new InvalidOperationException("O espaço V não acionou a técnica equipada.");
            skillLoadoutGame.EquipSkill(2, 2);
            if (skillLoadoutGame.equippedSkills[2] == 2) throw new InvalidOperationException("Uma mesma técnica foi equipada em dois espaços.");
            SaveData loadoutSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(skillLoadoutGame.CaptureSaveData()))!;
            if (!ValidSkillLoadout(loadoutSave) || !loadoutSave.EquippedSkills.SequenceEqual(new[] { 1, 2, 0 })) throw new InvalidOperationException("A configuração de técnicas não foi salva corretamente.");
            SaveData oldLoadoutSave = JsonSerializer.Deserialize<SaveData>("{}")!;
            if (oldLoadoutSave.EquippedSkills is null || !oldLoadoutSave.EquippedSkills.SequenceEqual(new[] { 1, 0, 0 })) throw new InvalidOperationException("Um save antigo não recebeu a configuração padrão de técnicas.");
            loadoutSave.EquippedSkills[2] = 5;
            if (ValidSkillLoadout(loadoutSave)) throw new InvalidOperationException("Um save equipou uma técnica narrativa ainda bloqueada.");
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
        using (var wardGame = new Game())
        {
            wardGame.timer.Stop(); wardGame.StartGame(); wardGame.storyOverlay = false; wardGame.room = 6; wardGame.enemies.Clear();
            wardGame.enemies.Add(new Enemy { Pos = new PointF(300, 320), Kind = EnemyKind.Grunt, Hp = 100, MaxHp = 100 });
            wardGame.CastAnchorWard();
            if (wardGame.wardTime != 0 || !wardGame.notice.Contains("AINDA NÃO")) throw new InvalidOperationException("O Manto de Lastro ficou disponível antes do registro de Aven.");
            wardGame.anchorWard = true; wardGame.health = 100; wardGame.CastAnchorWard(); wardGame.HitPlayer(40);
            if (wardGame.wardTime != 2.5f || Math.Abs(wardGame.health - 82) > 0.01f || wardGame.wardCd != 20f) throw new InvalidOperationException("O Manto de Lastro não aplicou defesa ou recarga corretas.");
            wardGame.wardTime = 0; wardGame.health = 100; wardGame.HitPlayer(40);
            if (wardGame.health != 60) throw new InvalidOperationException("A proteção do Manto de Lastro continuou ativa após expirar.");
            wardGame.wardCd = 0; wardGame.skillTalent = 5; wardGame.CastAnchorWard();
            if (wardGame.wardCd != 16f) throw new InvalidOperationException("Domínio não reduziu a recarga do Manto de Lastro.");
            wardGame.questStage = 36; wardGame.counterTrace = true; wardGame.anchorWard = true;
            SaveData savedWard = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(wardGame.CaptureSaveData())) ?? throw new InvalidOperationException("O Manto de Lastro não foi serializado.");
            if (!savedWard.AnchorWard) throw new InvalidOperationException("O desbloqueio do Manto de Lastro não persistiu no save.");
        }
        using (var lootRollCheck = new Game())
        {
            lootRollCheck.timer.Stop(); lootRollCheck.StartGame(); lootRollCheck.storyOverlay = false;
            var common = RollWeaponStats(0, 4, 2); var rare = RollWeaponStats(62, 4, 2); var relic = RollWeaponStats(95, 4, 2); var swiftRelic = RollWeaponStats(95, 4, 3); var anchoredRelic = RollWeaponStats(95, 4, 4); var reachRare = RollWeaponStats(62, 4, 5); var reachRelic = RollWeaponStats(95, 4, 5, 4);
            if (common.Rarity != 0 || common.Bonus != 1 || common.Affix != 0 || common.Affix2 != 0 || rare.Rarity != 1 || rare.Bonus != 3 || rare.Affix != 3 || rare.Affix2 != 0 || relic.Rarity != 2 || relic.Bonus != 11 || relic.Affix != 3 || relic.Affix2 is < 1 or > 6 || relic.Affix2 == relic.Affix || swiftRelic.Affix != 4 || anchoredRelic.Affix != 5 || reachRare.Affix != 6 || reachRelic.Affix != 6 || reachRelic.Affix2 != 5)
                throw new InvalidOperationException("As faixas de rolagem de raridade e afixo da arma mudaram.");
            lootRollCheck.weaponRarities[0] = 1; lootRollCheck.weaponBonuses[0] = 3; lootRollCheck.weaponAffixes[0] = 6;
            if (Math.Abs(lootRollCheck.BasicAttackReach(new Enemy { Kind = EnemyKind.Grunt }) - 87.36f) > 0.01f || !ValidWeaponData(lootRollCheck.CaptureSaveData()) || WeaponGroundAffix(6, 1) != "Fio Esticado • alcance +12%")
                throw new InvalidOperationException("O Fio Esticado não aumentou o alcance básico, não apareceu no loot ou foi recusado pelo save.");
            lootRollCheck.weaponRarities[0] = 2; lootRollCheck.weaponBonuses[0] = 7;
            if (Math.Abs(lootRollCheck.BasicAttackReach(new Enemy { Kind = EnemyKind.Grunt }) - 93.6f) > 0.01f || WeaponAffixDescription(6, 2) != "do Fio Esticado: alcance básico +20%")
                throw new InvalidOperationException("O Fio Esticado não escalou para o bônus de Relíquia.");
            lootRollCheck.weaponRarities[0] = lootRollCheck.weaponBonuses[0] = lootRollCheck.weaponAffixes[0] = lootRollCheck.weaponAffixes2[0] = 0;
            Drop drop = lootRollCheck.CreateWeaponDrop(lootRollCheck.player, 1); int rarity = drop.Rarity, bonus = drop.Bonus, affix = drop.Affix, affix2 = drop.Affix2;
            string label = lootRollCheck.DropLabel(drop); int expectedDropDamage = lootRollCheck.damage - lootRollCheck.WeaponPower(lootRollCheck.weapon) + lootRollCheck.weaponDamage[1] + bonus; lootRollCheck.Collect(drop);
            if (lootRollCheck.weaponRarities[1] != rarity || lootRollCheck.weaponBonuses[1] != bonus || lootRollCheck.weaponAffixes[1] != affix || lootRollCheck.weaponAffixes2[1] != affix2 || !label.Contains(WeaponRarityName(rarity)) || !label.Contains(WeaponGroundAffix(drop)) || !label.Contains($"DANO: 25 → {expectedDropDamage}") || !label.Contains("longo") || !label.Contains("ATUAL: sem afixo"))
                throw new InvalidOperationException("A arma mudou ao ser recolhida ou não foi identificada no chão.");
        }
        if (EliteAffixForRoll(0) != EliteAffix.Armored || EliteAffixForRoll(1) != EliteAffix.Swift || EliteAffixForRoll(2) != EliteAffix.Bloodbound || EliteAffixName(EliteAffix.Bloodbound) != "SANGRADOR")
            throw new InvalidOperationException("As variantes dos elites não foram identificadas corretamente.");
        using (var eliteAffixCheck = new Game())
        {
            eliteAffixCheck.timer.Stop(); eliteAffixCheck.StartGame(); eliteAffixCheck.storyOverlay = false; eliteAffixCheck.player = new PointF(200, 330);
            Enemy swiftElite = new() { Pos = new PointF(250, 330), Kind = EnemyKind.Elite, EliteAffix = EliteAffix.Swift, Hp = 120, MaxHp = 120 };
            eliteAffixCheck.enemies.Add(swiftElite); eliteAffixCheck.UpdateEnemy(swiftElite, 0.01f);
            if (swiftElite.WindupMax != 0.38f || Math.Abs(swiftElite.Cooldown - 1.55f) > 0.001f || swiftElite.Armored) throw new InvalidOperationException("O elite Veloz não anunciou seu golpe curto com recarga própria.");
            eliteAffixCheck.enemies.Clear(); eliteAffixCheck.health = 100;
            Enemy bloodboundElite = new() { Pos = new PointF(250, 330), Target = eliteAffixCheck.player, Kind = EnemyKind.Elite, EliteAffix = EliteAffix.Bloodbound, Hp = 50, MaxHp = 120, Windup = 0.01f };
            eliteAffixCheck.enemies.Add(bloodboundElite); eliteAffixCheck.UpdateEnemy(bloodboundElite, 0.02f);
            if (eliteAffixCheck.health != 76 || bloodboundElite.Hp != 58) throw new InvalidOperationException("O elite Sangrador não causou o dano anunciado nem recuperou vida ao acertar.");
            Enemy armoredElite = new() { Kind = EnemyKind.Elite, EliteAffix = EliteAffix.Armored, Armored = true, Hp = 120, MaxHp = 120 };
            if (!armoredElite.Armored || EnemyName(armoredElite) != "ELITE • BLINDADO") throw new InvalidOperationException("O elite Blindado não preservou sua redução nem seu identificador.");
            armoredElite.Pos = new PointF(250, 330);
            Enemy wardedGrunt = new() { Pos = new PointF(350, 330), Kind = EnemyKind.Grunt, Hp = 100, MaxHp = 100 };
            Enemy unwardedGrunt = new() { Pos = new PointF(400, 330), Kind = EnemyKind.Grunt, Hp = 100, MaxHp = 100 };
            eliteAffixCheck.enemies.Clear(); eliteAffixCheck.enemies.AddRange(new[] { armoredElite, wardedGrunt, unwardedGrunt });
            if (!eliteAffixCheck.IsArmored(wardedGrunt) || eliteAffixCheck.IsArmored(unwardedGrunt) || eliteAffixCheck.BasicAttackDamageMultiplier(wardedGrunt) != 0.75f)
                throw new InvalidOperationException("A aura do elite Blindado não protegeu apenas os ecos dentro do alcance.");
            eliteAffixCheck.enemies.Remove(armoredElite);
            if (eliteAffixCheck.IsArmored(wardedGrunt)) throw new InvalidOperationException("A proteção da aura permaneceu depois da queda do elite Blindado.");
        }
        if (EliteWeaponDropRoll(-1) || !EliteWeaponDropRoll(0) || !EliteWeaponDropRoll(34) || EliteWeaponDropRoll(35) || EliteWeaponDropRoll(99))
            throw new InvalidOperationException("A chance de arma dos elites não respeitou os 35% definidos.");
        foreach (var tier in new[] { (Room: 2, Floor: 62), (Room: 12, Floor: 75), (Room: 25, Floor: 85) })
        {
            int floor = BossWeaponRollFloor(tier.Room);
            int commonRolls = Enumerable.Range(floor, 100 - floor).Count(roll => RollWeaponStats(roll, 0, 0).Rarity == 0);
            int relicRolls = Enumerable.Range(floor, 100 - floor).Count(roll => RollWeaponStats(roll, 0, 0).Rarity == 2);
            if (floor != tier.Floor || commonRolls != 0 || relicRolls != 5)
                throw new InvalidOperationException("A raridade das armas de chefe não escalou com o avanço da campanha.");
        }
        using (var bossLootCheck = new Game())
        {
            bossLootCheck.timer.Stop(); bossLootCheck.StartGame(); bossLootCheck.storyOverlay = false; bossLootCheck.room = 40;
            var boss = new Enemy { Pos = new PointF(500, 330), Kind = EnemyKind.Guardian, Hp = 1, MaxHp = 1 };
            bossLootCheck.enemies.Add(boss); bossLootCheck.Defeat(boss);
            Drop bossWeapon = bossLootCheck.drops.Single(d => d.Kind == "weapon");
            if (bossWeapon.Amount is < 1 or > 2 || bossWeapon.Rarity is < 1 or > 2 || bossWeapon.Affix == 0 || bossWeapon.Rarity == 2 && (bossWeapon.Affix2 == 0 || bossWeapon.Affix2 == bossWeapon.Affix))
                throw new InvalidOperationException("A recompensa do chefe não gerou uma arma Rara ou Relíquia válida.");
            bossLootCheck.Collect(bossWeapon);
            SaveData bossLootSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(bossLootCheck.CaptureSaveData()))!;
            int bossWeaponIndex = bossWeapon.Amount;
            if ((bossLootCheck.ownedWeapons & (1 << bossWeaponIndex)) == 0 || bossLootSave.WeaponRarities[bossWeaponIndex] != bossWeapon.Rarity || bossLootSave.WeaponAffixes[bossWeaponIndex] != bossWeapon.Affix || !ValidWeaponData(bossLootSave))
                throw new InvalidOperationException("A arma do chefe não foi coletada e persistida corretamente.");
        }
        using (var salvageCheck = new Game())
        {
            salvageCheck.timer.Stop(); salvageCheck.StartGame(); salvageCheck.storyOverlay = false; salvageCheck.player = new PointF(500, 330); salvageCheck.tideGlass = 998;
            int originalDamage = salvageCheck.damage, originalWeapon = salvageCheck.weapon, originalOwnedWeapons = salvageCheck.ownedWeapons;
            var rareDrop = new Drop { Pos = new PointF(540, 330), Kind = "weapon", Amount = 1, Rarity = 1, Bonus = 3, Affix = 1 };
            var relicDrop = new Drop { Pos = new PointF(560, 330), Kind = "weapon", Amount = 2, Rarity = 2, Bonus = 7, Affix = 2, Affix2 = 4 };
            salvageCheck.drops.Add(rareDrop); salvageCheck.drops.Add(relicDrop);
            salvageCheck.OnKeyDown(salvageCheck, new KeyEventArgs(Keys.X));
            if (salvageCheck.gold != 40 || salvageCheck.tideGlass != 999 || salvageCheck.drops.Count != 1 || !ReferenceEquals(salvageCheck.drops[0], relicDrop) || salvageCheck.weapon != originalWeapon || salvageCheck.damage != originalDamage || salvageCheck.ownedWeapons != originalOwnedWeapons)
                throw new InvalidOperationException("Desmontar a arma Rara não converteu o item mais próximo sem alterar o equipamento.");
            salvageCheck.OnKeyDown(salvageCheck, new KeyEventArgs(Keys.X));
            SaveData salvageSave = salvageCheck.CaptureSaveData();
            if (salvageCheck.gold != 115 || salvageCheck.tideGlass != 999 || salvageCheck.drops.Count != 0 || salvageSave.Gold != 115 || salvageSave.TideGlass != 999)
                throw new InvalidOperationException("Desmontar a Relíquia não concedeu os materiais ou não os preservou no save.");
        }
        using (var activeWeaponDrop = new Game())
        {
            activeWeaponDrop.timer.Stop(); activeWeaponDrop.StartGame(); activeWeaponDrop.weapon = 1; activeWeaponDrop.ownedWeapons |= 2; activeWeaponDrop.weaponRarities[1] = 1; activeWeaponDrop.weaponBonuses[1] = 3; activeWeaponDrop.weaponAffixes[1] = 1; activeWeaponDrop.damage += activeWeaponDrop.WeaponPower(1); activeWeaponDrop.Collect(new Drop { Kind = "weapon", Amount = 1, Rarity = 2, Bonus = 9, Affix = 2, Affix2 = 5 });
            if (activeWeaponDrop.damage != 44 || activeWeaponDrop.weaponBonuses[1] != 9 || activeWeaponDrop.WeaponAffixDescription(1) != "do Contracanto: +11 p.p. de crítico • do Lastro: dano recebido -12%") throw new InvalidOperationException("Atualizar a arma equipada pelo saque não ajustou o dano nem comparou ambos os afixos.");
        }
        using (var buildGame = new Game())
        {
            buildGame.timer.Stop(); buildGame.StartGame(); buildGame.storyOverlay = false; buildGame.room = 6; buildGame.damage = 100; buildGame.enemies.Clear();
            float baseDamage = buildGame.VoiceShardDamage(); buildGame.pendingTalents = 1; buildGame.talentChoice = true; buildGame.SelectTalent(Keys.D4);
            float specializedDamage = buildGame.VoiceShardDamage();
            if (buildGame.techniqueTalent != 1 || buildGame.pendingTalents != 0 || buildGame.talentChoice || Math.Abs(specializedDamage / baseDamage - 1.08f) > 0.001f)
                throw new InvalidOperationException("A especialização de Sintonia não foi escolhida ou não fortaleceu as técnicas em 8%.");
            buildGame.player = new PointF(250, 330); buildGame.enemies.Add(new Enemy { Pos = new PointF(300, 330), Kind = EnemyKind.Grunt, Hp = 1000, MaxHp = 1000 }); buildGame.Whirlwind();
            if (buildGame.enemies[0].Hp >= 1000 - 100 * 1.6f * 1.08f + 1) throw new InvalidOperationException("A Sintonia não alterou o dano real do Redemoinho.");
            SaveData buildSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(buildGame.CaptureSaveData())) ?? throw new InvalidOperationException("A especialização não foi serializada.");
            if (buildSave.TechniqueTalent != 1 || JsonSerializer.Deserialize<SaveData>("{}")?.TechniqueTalent != 0) throw new InvalidOperationException("A especialização não persistiu ou invalidou saves antigos.");
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
        using var rarityGame = new Game(); rarityGame.timer.Stop(); rarityGame.StartGame(); rarityGame.damage = 25; rarityGame.ownedWeapons = 7;
        rarityGame.RollWeapon(1, 0, 4, 2);
        if (rarityGame.weaponRarities[1] != 0 || rarityGame.weaponBonuses[1] != 1 || rarityGame.weaponAffixes[1] != 0 || rarityGame.weaponAffixes2[1] != 0) throw new InvalidOperationException("A arma comum recebeu valores fora da faixa esperada.");
        rarityGame.RollWeapon(1, 62, 3, 0);
        if (rarityGame.weaponRarities[1] != 1 || rarityGame.weaponBonuses[1] != 6 || rarityGame.weaponAffixes[1] != 1 || rarityGame.weaponAffixes2[1] != 0 || !ValidWeaponData(rarityGame.CaptureSaveData())) throw new InvalidOperationException("A arma rara não recebeu bônus e afixo válidos.");
        rarityGame.EquipWeapon(1);
        if (rarityGame.damage != 41) throw new InvalidOperationException("O bônus de dano da arma não foi aplicado ao equipar.");
        rarityGame.health = 50; var healingDummy = new Enemy { Pos = rarityGame.player, Kind = EnemyKind.Grunt, Hp = 1, MaxHp = 1 }; rarityGame.enemies.Add(healingDummy); rarityGame.Defeat(healingDummy);
        if (rarityGame.health != 53) throw new InvalidOperationException("O afixo da Trégua não recuperou vida ao derrotar um inimigo.");
        rarityGame.RollWeapon(2, 95, 4, 2, 1);
        rarityGame.EquipWeapon(2);
        if (rarityGame.weaponRarities[2] != 2 || rarityGame.weaponBonuses[2] != 11 || rarityGame.weaponAffixes[2] != 3 || rarityGame.weaponAffixes2[2] != 2 || Math.Abs(rarityGame.WeaponSkillMultiplier() - 1.18f) > 0.001f || !rarityGame.WeaponDisplayName(2).Contains("Maré + Contracanto")) throw new InvalidOperationException($"A relíquia não combinou os afixos da Maré e do Contracanto ({rarityGame.weaponRarities[2]}, {rarityGame.weaponBonuses[2]}, {rarityGame.weaponAffixes[2]} + {rarityGame.weaponAffixes2[2]}, arma {rarityGame.weapon}, x{rarityGame.WeaponSkillMultiplier()}).");
        if (rarityGame.damage != 40) throw new InvalidOperationException("A troca entre armas com bônus não preservou o dano corretamente.");
        rarityGame.damage = 100;
        if (Math.Abs(rarityGame.VoiceShardDamage() - 159.3f) > 0.01f) throw new InvalidOperationException("O afixo da Maré não fortaleceu o Estilhaço de Voz.");
        if (Math.Abs(rarityGame.CriticalChance() - 0.36f) > 0.001f) throw new InvalidOperationException("O afixo do Contracanto não aumentou o crítico da relíquia.");
        SaveData raritySave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(rarityGame.CaptureSaveData()))!;
        if (!raritySave.WeaponBonuses.SequenceEqual(rarityGame.weaponBonuses) || !raritySave.WeaponRarities.SequenceEqual(rarityGame.weaponRarities) || !raritySave.WeaponAffixes.SequenceEqual(rarityGame.weaponAffixes) || !raritySave.WeaponAffixes2.SequenceEqual(rarityGame.weaponAffixes2)) throw new InvalidOperationException("A raridade e os afixos não foram preservados no save.");
        using var restoredGear = new Game(); restoredGear.timer.Stop(); restoredGear.RestoreWeaponData(raritySave);
        if (!restoredGear.weaponBonuses.SequenceEqual(rarityGame.weaponBonuses) || !restoredGear.weaponRarities.SequenceEqual(rarityGame.weaponRarities) || !restoredGear.weaponAffixes.SequenceEqual(rarityGame.weaponAffixes) || !restoredGear.weaponAffixes2.SequenceEqual(rarityGame.weaponAffixes2)) throw new InvalidOperationException("O carregamento não restaurou as rolagens das armas.");
        SaveData legacyRelic = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(raritySave))!; legacyRelic.WeaponAffixes2 = new int[3];
        using var migratedRelic = new Game(); migratedRelic.timer.Stop(); migratedRelic.ownedWeapons = legacyRelic.OwnedWeapons; migratedRelic.RestoreWeaponData(legacyRelic);
        if (migratedRelic.weaponAffixes2[2] != 4 || !ValidWeaponData(migratedRelic.CaptureSaveData())) throw new InvalidOperationException("Uma relíquia antiga sem segundo afixo não foi migrada com segurança.");
        SaveData legacyLootSave = JsonSerializer.Deserialize<SaveData>("{\"Room\":0}")!;
        if (!ValidWeaponData(legacyLootSave) || legacyLootSave.WeaponBonuses.Length != 3 || legacyLootSave.WeaponRarities.Length != 3 || legacyLootSave.WeaponAffixes.Length != 3 || legacyLootSave.WeaponAffixes2.Length != 3) throw new InvalidOperationException("Um save antigo sem raridade não recebeu valores padrão compatíveis.");
        rarityGame.Collect(new Drop { Pos = rarityGame.player, Kind = "weapon", Amount = 1 });
        if ((rarityGame.ownedWeapons & 2) == 0 || rarityGame.weaponRarities[1] is < 0 or > 2 || !ValidWeaponData(rarityGame.CaptureSaveData())) throw new InvalidOperationException("A coleta de arma não gerou uma configuração de loot válida.");
        var elite = new Enemy { Pos = new PointF(500, 330), Kind = EnemyKind.Elite, Hp = 1, MaxHp = 1 };
        lootGame.enemies.Add(elite); lootGame.Defeat(elite);
        Drop firstCharm = lootGame.drops.Single(d => d.Kind == "charm");
        if (firstCharm.Amount != 1) throw new InvalidOperationException("O Elite não deixou o Nó de Brasa.");
        lootGame.Collect(firstCharm);
        if (lootGame.equippedCharm != 1 || lootGame.WhirlwindMultiplier() <= 1.6f) throw new InvalidOperationException("O Nó de Brasa não alterou o Redemoinho.");
        lootGame.Collect(new Drop { Pos = lootGame.player, Kind = "charm", Amount = 2 });
        if (lootGame.equippedCharm2 != 2 || lootGame.DashDuration() <= 0.17f || lootGame.DashCooldown() <= 1.15f || lootGame.WhirlwindMultiplier() <= 2.16f || lootGame.WhirlwindCooldownPenalty() >= 1f) throw new InvalidOperationException("A sintonia entre Brasa e Travessia não fortaleceu o Redemoinho.");
        lootGame.Collect(new Drop { Pos = lootGame.player, Kind = "charm", Amount = 3 }); lootGame.CycleCharm(2);
        if (lootGame.equippedCharm2 != 3 || lootGame.CriticalChance() < 0.29f || lootGame.CriticalMultiplier() <= 2.5f || lootGame.BasicAttackCooldown() >= 0.39f) throw new InvalidOperationException("A sintonia entre Brasa e Vidro não ampliou os críticos.");
        lootGame.CycleCharm(1);
        if (lootGame.equippedCharm != 2 || lootGame.equippedCharm2 != 3 || lootGame.DashCooldown() >= 1.45f) throw new InvalidOperationException("A sintonia entre Travessia e Vigília não removeu a recarga adicional da esquiva.");
        SaveData charmSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(lootGame.CaptureSaveData()))!;
        if (charmSave.OwnedCharms != 7 || charmSave.EquippedCharm != 2 || charmSave.EquippedCharm2 != 3) throw new InvalidOperationException("Os espaços de talismã não foram serializados no save.");
        using var advancedCharmGame = new Game(); advancedCharmGame.timer.Stop(); advancedCharmGame.StartGame(); advancedCharmGame.damage = 100; advancedCharmGame.ownedCharms = 24; advancedCharmGame.equippedCharm = 4; advancedCharmGame.equippedCharm2 = 5;
        var bossDummy = new Enemy { Kind = EnemyKind.OathCartographer, Hp = 500, MaxHp = 500 }; var commonDummy = new Enemy { Kind = EnemyKind.Grunt, Hp = 500, MaxHp = 500 };
        if (advancedCharmGame.OutgoingDamageMultiplier(bossDummy) != 1.25f || advancedCharmGame.OutgoingDamageMultiplier(commonDummy) != 0.8f || advancedCharmGame.VoiceShardLifetime() != 1.8f || advancedCharmGame.VoiceShardCooldown() != 8.25f) throw new InvalidOperationException("A Marca da Vigília ou o Fio da Margem não aplicou seus bônus e custo.");
        advancedCharmGame.equippedCharm = 3; advancedCharmGame.equippedCharm2 = 5;
        if (advancedCharmGame.VoiceShardDamage() != 205f || advancedCharmGame.VoiceShardCooldown() != 7.5f) throw new InvalidOperationException("A sintonia entre Vidro e Margem não fortaleceu o Estilhaço de Voz.");
        SaveData expandedCharmSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(advancedCharmGame.CaptureSaveData()))!;
        if (expandedCharmSave.OwnedCharms != 24 || expandedCharmSave.EquippedCharm != 3 || expandedCharmSave.EquippedCharm2 != 5) throw new InvalidOperationException("Os novos talismãs não foram mantidos no save.");
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
        using var beyondGame = new Game(); beyondGame.timer.Stop(); beyondGame.StartGame(); beyondGame.storyOverlay = false; beyondGame.questStage = 13; beyondGame.townEnding = 1; beyondGame.naraTrace = true; beyondGame.room = 11; beyondGame.clearedRooms = 1 << 11; beyondGame.SetupRoom();
        beyondGame.player = new PointF(850, 330); beyondGame.Interact();
        if (beyondGame.room != 12 || beyondGame.enemies.Count != 6 || !beyondGame.enemies.Any(e => e.Kind == EnemyKind.NameMoth)) throw new InvalidOperationException("A decisão não abriu a Costa das Vozes Tardias.");
        beyondGame.enemies.Clear(); beyondGame.drops.Clear(); beyondGame.player = new PointF(850, 330); beyondGame.Interact();
        if (beyondGame.room != 13 || !beyondGame.enemies.Any(e => e.Kind == EnemyKind.NameWeaver)) throw new InvalidOperationException("O Claustro do Coro Partido não apresentou sua chefe.");
        Enemy weaver = beyondGame.enemies.Single(e => e.Kind == EnemyKind.NameWeaver);
        if (weaver.MaxHp != 410) throw new InvalidOperationException("Os nomes restaurados não enfraqueceram a Tecelã.");
        weaver.Cooldown = 0; beyondGame.UpdateEnemy(weaver, 0.01f);
        if (weaver.Windup <= 0 || weaver.BossPattern != 1) throw new InvalidOperationException("A Tecelã não anunciou a primeira marca dirigida.");
        weaver.Windup = 0; weaver.Cooldown = 0; beyondGame.UpdateEnemy(weaver, 0.01f);
        if (weaver.Windup <= 0 || weaver.BossPattern != 2) throw new InvalidOperationException("A Tecelã não anunciou seu fio em linha.");
        weaver.Windup = 0; weaver.Cooldown = 0; beyondGame.UpdateEnemy(weaver, 0.01f);
        if (weaver.Windup <= 0 || weaver.BossPattern != 0) throw new InvalidOperationException("A Tecelã não concluiu seu ciclo de três ataques.");
        beyondGame.enemies.Clear(); beyondGame.enemies.Add(weaver); beyondGame.drops.Clear(); beyondGame.Defeat(weaver);
        if (!beyondGame.drops.Any(d => d.Kind == "chorus")) throw new InvalidOperationException("A Tecelã não deixou o Coro dos nomes consumidos.");
        foreach (Drop drop in beyondGame.drops.ToArray()) beyondGame.Collect(drop);
        if (!beyondGame.chorusMemory || beyondGame.questStage != 14 || !beyondGame.memoryOverlay) throw new InvalidOperationException("O Coro não revelou a origem do Silêncio de Baixo.");
        SaveData chorusSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(beyondGame.CaptureSaveData()))!;
        if (chorusSave.Room != 13 || !chorusSave.ChorusMemory || (chorusSave.ClearedRooms & (1 << 13)) == 0) throw new InvalidOperationException("A nova expedição e o Coro não foram registrados no save.");
        beyondGame.memoryOverlay = false; beyondGame.player = new PointF(850, 330); beyondGame.Interact(); beyondGame.player = CuratorPosition; beyondGame.InteractTown();
        if (beyondGame.room != 3 || beyondGame.questStage != 15 || !beyondGame.chorusMemory) throw new InvalidOperationException("Sileth não recebeu o Coro ao voltar à praça.");
        using var lumeExpedition = new Game(); lumeExpedition.timer.Stop(); lumeExpedition.StartGame(); lumeExpedition.room = 13; lumeExpedition.townEnding = 2; lumeExpedition.questStage = 13; lumeExpedition.SetupRoom();
        if (lumeExpedition.enemies.Single(e => e.Kind == EnemyKind.NameWeaver).MaxHp != 455) throw new InvalidOperationException("Manter o Lume não alterou o perigo da expedição.");
        using var firstExpedition = new Game(); firstExpedition.timer.Stop(); firstExpedition.StartGame(); firstExpedition.storyOverlay = false; firstExpedition.room = 13; firstExpedition.townEnding = 1; firstExpedition.questStage = 15; firstExpedition.chorusMemory = true; firstExpedition.clearedRooms = 1 << 13; firstExpedition.SetupRoom();
        firstExpedition.player = new PointF(480, 500); firstExpedition.Interact();
        if (firstExpedition.room != 14 || firstExpedition.enemies.Count != 6) throw new InvalidOperationException("O Coro não abriu a Bacia do Primeiro Nome sob o Claustro.");
        firstExpedition.enemies.Clear(); firstExpedition.drops.Clear(); firstExpedition.player = new PointF(850, 330); firstExpedition.Interact();
        if (firstExpedition.room != 15 || firstExpedition.enemies.Single(e => e.Kind == EnemyKind.FirstKeeper).MaxHp != 560) throw new InvalidOperationException("A Sala da Primeira Vigília não respondeu à escolha de devolver os nomes.");
        Enemy firstKeeper = firstExpedition.enemies.Single(e => e.Kind == EnemyKind.FirstKeeper); firstKeeper.Cooldown = 0; firstExpedition.UpdateEnemy(firstKeeper, 0.01f);
        if (firstKeeper.BossPattern != 1 || firstKeeper.Windup <= 0) throw new InvalidOperationException("A Primeira Vigília não anunciou a marca de memória.");
        firstKeeper.Windup = 0; firstKeeper.Cooldown = 0; firstExpedition.UpdateEnemy(firstKeeper, 0.01f);
        if (firstKeeper.BossPattern != 2 || firstKeeper.Target2 == PointF.Empty || firstKeeper.Target2Start == PointF.Empty || DistanceToSegment(firstExpedition.player, firstKeeper.Target2Start, firstKeeper.Target2) >= 25) throw new InvalidOperationException("A Primeira Vigília não anunciou o cruzamento de vozes sobre o jogador.");
        firstKeeper.Windup = 0.01f; firstExpedition.health = 100; firstExpedition.player = new PointF(150, 330); firstExpedition.UpdateEnemy(firstKeeper, 0.02f);
        if (firstExpedition.health >= 100) throw new InvalidOperationException("O cruzamento anunciado não atingiu a posição marcada.");
        firstKeeper.Windup = 0.01f; firstExpedition.health = 100; firstExpedition.dead = false; firstExpedition.dashTime = 0.2f; firstExpedition.UpdateEnemy(firstKeeper, 0.02f);
        if (firstExpedition.health != 100) throw new InvalidOperationException("A esquiva não evitou o cruzamento da Primeira Vigília.");
        firstExpedition.dashTime = 0; firstKeeper.Windup = 0; firstKeeper.Cooldown = 0; firstExpedition.UpdateEnemy(firstKeeper, 0.01f);
        if (firstKeeper.BossPattern != 0 || firstKeeper.Windup <= 0) throw new InvalidOperationException("A Primeira Vigília não concluiu seus três ataques anunciados.");
        firstExpedition.enemies.Clear(); firstExpedition.enemies.Add(firstKeeper); firstKeeper.Hp = 1; firstKeeper.Windup = 0; firstExpedition.Defeat(firstKeeper);
        if (!firstExpedition.drops.Any(d => d.Kind == "first") || !firstExpedition.drops.Any(d => d.Kind == "charm" && d.Amount == 4)) throw new InvalidOperationException("A guardiã não deixou a memória de Ilyra e a Marca da Vigília.");
        foreach (Drop drop in firstExpedition.drops.ToArray()) firstExpedition.Collect(drop);
        if (!firstExpedition.firstMemory || (firstExpedition.ownedCharms & 8) == 0 || firstExpedition.questStage != 16 || !firstExpedition.memoryOverlay) throw new InvalidOperationException("A primeira memória ou a Marca da Vigília não foi recuperada.");
        SaveData firstSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(firstExpedition.CaptureSaveData()))!;
        if (!firstSave.FirstMemory || firstSave.Room != 15 || (firstSave.ClearedRooms & (1 << 15)) == 0) throw new InvalidOperationException("A nova região e a memória de Ilyra não foram salvas.");
        firstExpedition.memoryOverlay = false; firstExpedition.player = new PointF(850, 330); firstExpedition.Interact(); firstExpedition.player = CuratorPosition; firstExpedition.InteractTown();
        if (firstExpedition.room != 3 || firstExpedition.questStage != 17) throw new InvalidOperationException("Sileth não registrou o testemunho da Primeira Vigília.");
        firstExpedition.player = BellKeeperPosition; firstExpedition.InteractTown();
        if (firstExpedition.memoryTitle != "A TRAVESSIA DO CONTRA-RIO" || firstExpedition.questStage != 18 || firstExpedition.room != 16 || firstExpedition.enemies.Count != 6 || !firstExpedition.enemies.Any(e => e.Kind == EnemyKind.MudCantor)) throw new InvalidOperationException("Edda não iniciou a travessia jogável para oeste.");
        firstExpedition.enemies.Clear(); firstExpedition.drops.Clear(); firstExpedition.player = new PointF(850, 330); firstExpedition.Interact();
        if (firstExpedition.room != 17 || firstExpedition.enemies.Count != 4) throw new InvalidOperationException("O Vau não levou ao Pátio das Testemunhas.");
        Enemy cartographer = firstExpedition.enemies.Single(e => e.Kind == EnemyKind.OathCartographer);
        if (cartographer.MaxHp != 610) throw new InvalidOperationException("Devolver os nomes não enfraqueceu a Cartógrafa do Vão.");
        cartographer.Cooldown = 0; firstExpedition.UpdateEnemy(cartographer, 0.01f);
        if (cartographer.BossPattern != 1 || cartographer.Windup <= 0) throw new InvalidOperationException("A Cartógrafa não anunciou a marca de testemunho.");
        cartographer.Windup = 0; cartographer.Cooldown = 0; firstExpedition.UpdateEnemy(cartographer, 0.01f);
        if (cartographer.BossPattern != 2 || cartographer.Windup <= 0 || cartographer.Target == PointF.Empty) throw new InvalidOperationException("A Cartógrafa não anunciou o traço em linha.");
        firstExpedition.enemies.Clear(); firstExpedition.enemies.Add(cartographer); cartographer.Hp = 1; cartographer.Windup = 0; firstExpedition.Defeat(cartographer);
        if (!firstExpedition.drops.Any(d => d.Kind == "west") || !firstExpedition.drops.Any(d => d.Kind == "charm" && d.Amount == 5)) throw new InvalidOperationException("A Cartógrafa não deixou o mapa e o Fio da Margem.");
        foreach (Drop drop in firstExpedition.drops.ToArray()) firstExpedition.Collect(drop);
        if (!firstExpedition.westTrace || (firstExpedition.ownedCharms & 16) == 0 || firstExpedition.questStage != 19 || !firstExpedition.memoryOverlay) throw new InvalidOperationException("O mapa e o Fio da Margem não foram recuperados.");
        SaveData westSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(firstExpedition.CaptureSaveData()))!;
        if (!westSave.WestTrace || westSave.QuestStage != 19 || westSave.Room != 17 || (westSave.ClearedRooms & (1 << 17)) == 0) throw new InvalidOperationException("O progresso no Vau não foi registrado no save.");
        firstExpedition.memoryOverlay = false; firstExpedition.drops.Clear(); firstExpedition.player = new PointF(850, 330); firstExpedition.Interact();
        if (firstExpedition.room != 3) throw new InvalidOperationException("O caminho de volta do refúgio não foi aberto.");
        firstExpedition.player = BellKeeperPosition; firstExpedition.InteractTown();
        if (firstExpedition.questStage != 20 || firstExpedition.memoryTitle != "A CARTA DO REFÚGIO" || firstExpedition.gold < 375) throw new InvalidOperationException("Edda não reconheceu o mapa nem recompensou a travessia.");
        using var lumeFirstExpedition = new Game(); lumeFirstExpedition.timer.Stop(); lumeFirstExpedition.StartGame(); lumeFirstExpedition.room = 15; lumeFirstExpedition.townEnding = 2; lumeFirstExpedition.questStage = 15; lumeFirstExpedition.chorusMemory = true; lumeFirstExpedition.SetupRoom();
        if (lumeFirstExpedition.enemies.Single(e => e.Kind == EnemyKind.FirstKeeper).MaxHp != 620) throw new InvalidOperationException("Manter o Lume não alterou a força da Primeira Vigília.");
        using var lumeWestExpedition = new Game(); lumeWestExpedition.timer.Stop(); lumeWestExpedition.StartGame(); lumeWestExpedition.room = 17; lumeWestExpedition.townEnding = 2; lumeWestExpedition.questStage = 18; lumeWestExpedition.firstMemory = true; lumeWestExpedition.SetupRoom();
        if (lumeWestExpedition.enemies.Single(e => e.Kind == EnemyKind.OathCartographer).MaxHp != 690) throw new InvalidOperationException("Manter o Lume não alterou a força da Cartógrafa do Vão.");
        using var refugeExpedition = new Game(); refugeExpedition.timer.Stop(); refugeExpedition.StartGame(); refugeExpedition.storyOverlay = false; refugeExpedition.room = 3; refugeExpedition.questStage = 20; refugeExpedition.westTrace = true; refugeExpedition.townEnding = 1; refugeExpedition.player = BellKeeperPosition;
        refugeExpedition.InteractTown();
        if (refugeExpedition.room != 18 || refugeExpedition.questStage != 21 || refugeExpedition.enemies.Count != 0) throw new InvalidOperationException("Edda não abriu o refúgio seguro após a carta de Nara.");
        refugeExpedition.player = AvenPosition; refugeExpedition.Interact();
        if (refugeExpedition.questStage != 22 || refugeExpedition.memoryOverlay) { refugeExpedition.memoryOverlay = false; if (refugeExpedition.questStage != 22) throw new InvalidOperationException("Aven não abriu a trilha após explicar as regras da comunidade."); }
        refugeExpedition.player = new PointF(850, 330); refugeExpedition.Interact();
        if (refugeExpedition.room != 19 || refugeExpedition.enemies.Count != 6 || !refugeExpedition.enemies.Any(e => e.Kind == EnemyKind.PaperWraith)) throw new InvalidOperationException("A Trilha dos Luminários não abriu ou não apresentou os Espectros de Papel.");
        refugeExpedition.enemies.Clear(); refugeExpedition.drops.Clear(); refugeExpedition.player = new PointF(850, 330); refugeExpedition.Interact();
        if (refugeExpedition.room != 20 || refugeExpedition.enemies.Count != 4 || !refugeExpedition.enemies.Any(e => e.Kind == EnemyKind.NameBroker)) throw new InvalidOperationException("A cisterna não apresentou o Escrivão de Contrato e sua guarda.");
        Enemy broker = refugeExpedition.enemies.Single(e => e.Kind == EnemyKind.NameBroker);
        if (broker.MaxHp != 720 || refugeExpedition.Blocked(broker.Pos, broker.Radius)) throw new InvalidOperationException("O chefe do registro não respeitou o ramo de Véspera ou surgiu dentro de uma parede.");
        broker.Cooldown = 0; refugeExpedition.UpdateEnemy(broker, 0.01f);
        if (broker.BossPattern != 1 || broker.Windup <= 0) throw new InvalidOperationException("O Escrivão não anunciou a primeira marca contratual.");
        broker.Windup = 0; broker.Cooldown = 0; refugeExpedition.UpdateEnemy(broker, 0.01f);
        if (broker.BossPattern != 2 || broker.Windup <= 0 || broker.Target == PointF.Empty) throw new InvalidOperationException("O Escrivão não anunciou o traço de contrato em linha.");
        broker.Windup = 0; broker.Cooldown = 0; refugeExpedition.UpdateEnemy(broker, 0.01f);
        if (broker.BossPattern != 0 || broker.Windup <= 0) throw new InvalidOperationException("O Escrivão não concluiu seu ciclo de três ataques.");
        refugeExpedition.enemies.Clear(); refugeExpedition.enemies.Add(broker); refugeExpedition.drops.Clear(); broker.Hp = 1; broker.Windup = 0; refugeExpedition.Defeat(broker);
        if (!refugeExpedition.drops.Any(d => d.Kind == "register")) throw new InvalidOperationException("O Escrivão não deixou o registro de passagem.");
        foreach (Drop drop in refugeExpedition.drops.ToArray()) refugeExpedition.Collect(drop);
        if (refugeExpedition.questStage != 23 || !refugeExpedition.memoryOverlay || refugeExpedition.drops.Count != 0) throw new InvalidOperationException("O registro não respeitou o consentimento dos viajantes nem atualizou a missão.");
        SaveData refugeSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(refugeExpedition.CaptureSaveData()))!;
        if (refugeSave.Room != 20 || refugeSave.QuestStage != 23 || (refugeSave.ClearedRooms & (1 << 20)) == 0) throw new InvalidOperationException("A etapa nova e a cisterna não foram persistidas no save.");
        refugeExpedition.memoryOverlay = false; refugeExpedition.player = new PointF(90, 330); refugeExpedition.Interact();
        refugeExpedition.player = new PointF(90, 330); refugeExpedition.Interact();
        if (refugeExpedition.room != 18) throw new InvalidOperationException("O retorno da cisterna não levou de volta ao refúgio.");
        int refugeGold = refugeExpedition.gold; refugeExpedition.player = AvenPosition; refugeExpedition.Interact();
        if (refugeExpedition.questStage != 24 || refugeExpedition.gold != refugeGold + 240 || refugeExpedition.potions < 4) throw new InvalidOperationException("Aven não reconheceu o registro nem forneceu provisões.");
        using var lumeRefuge = new Game(); lumeRefuge.timer.Stop(); lumeRefuge.StartGame(); lumeRefuge.room = 20; lumeRefuge.townEnding = 2; lumeRefuge.questStage = 22; lumeRefuge.westTrace = true; lumeRefuge.firstMemory = true; lumeRefuge.chorusMemory = true; lumeRefuge.SetupRoom();
        if (lumeRefuge.enemies.Single(e => e.Kind == EnemyKind.NameBroker).MaxHp != 790) throw new InvalidOperationException("Manter o Lume não alterou a força do Escrivão de Contrato.");
        using var upstreamExpedition = new Game(); upstreamExpedition.timer.Stop(); upstreamExpedition.StartGame(); upstreamExpedition.storyOverlay = false; upstreamExpedition.room = 18; upstreamExpedition.questStage = 24; upstreamExpedition.westTrace = true; upstreamExpedition.townEnding = 1; upstreamExpedition.player = new PointF(850, 330); upstreamExpedition.Interact();
        if (upstreamExpedition.room != 21 || upstreamExpedition.questStage != 25 || upstreamExpedition.enemies.Count != 6 || !upstreamExpedition.enemies.Any(e => e.Kind == EnemyKind.SaltEcho)) throw new InvalidOperationException($"A nova trilha falhou: sala {upstreamExpedition.room}, etapa {upstreamExpedition.questStage}, inimigos {upstreamExpedition.enemies.Count}, salinos {upstreamExpedition.enemies.Count(e => e.Kind == EnemyKind.SaltEcho)}.");
        upstreamExpedition.enemies.Clear(); upstreamExpedition.drops.Clear(); upstreamExpedition.player = new PointF(850, 330); upstreamExpedition.Interact();
        if (upstreamExpedition.room != 22 || upstreamExpedition.enemies.Count != 5 || upstreamExpedition.memoryTitle != "PONTE DA MARÉ INVERTIDA") throw new InvalidOperationException("A Várzea não levou à Ponte da Maré Invertida.");
        Enemy tideWitness = upstreamExpedition.enemies.Single(e => e.Kind == EnemyKind.TideWitness);
        if (tideWitness.MaxHp != 820 || upstreamExpedition.Blocked(tideWitness.Pos, tideWitness.Radius)) throw new InvalidOperationException("A Testemunha da Maré não respeitou a escolha da cidade ou surgiu dentro da ponte.");
        tideWitness.Cooldown = 0; upstreamExpedition.player = new PointF(150, 330); upstreamExpedition.UpdateEnemy(tideWitness, 0.01f);
        if (tideWitness.BossPattern != 1 || tideWitness.Windup <= 0 || tideWitness.Target != upstreamExpedition.player) throw new InvalidOperationException("A Testemunha não anunciou a marca d'água.");
        tideWitness.Windup = 0; tideWitness.Cooldown = 0; upstreamExpedition.UpdateEnemy(tideWitness, 0.01f);
        if (tideWitness.BossPattern != 2 || tideWitness.Windup <= 0 || tideWitness.Target == PointF.Empty) throw new InvalidOperationException("A Testemunha não anunciou o corte ao longo da corrente.");
        tideWitness.Windup = 0; tideWitness.Cooldown = 0; upstreamExpedition.UpdateEnemy(tideWitness, 0.01f);
        if (tideWitness.BossPattern != 0 || tideWitness.Windup <= 0) throw new InvalidOperationException("A Testemunha não concluiu o ciclo de três ataques.");
        upstreamExpedition.enemies.Clear(); upstreamExpedition.enemies.Add(tideWitness); upstreamExpedition.drops.Clear(); tideWitness.Hp = 1; tideWitness.Windup = 0; upstreamExpedition.Defeat(tideWitness);
        if (!upstreamExpedition.drops.Any(d => d.Kind == "witness") || !upstreamExpedition.drops.Any(d => d.Kind == "glass" && d.Amount == 3)) throw new InvalidOperationException("A Testemunha da Maré não deixou a memória e o sal vítreo para a comunidade.");
        foreach (Drop drop in upstreamExpedition.drops.ToArray()) upstreamExpedition.Collect(drop);
        if (!upstreamExpedition.upstreamTrace || upstreamExpedition.questStage != 26 || upstreamExpedition.memoryTitle != "A ÚLTIMA CURVA DE NARA" || upstreamExpedition.tideGlass != 3) throw new InvalidOperationException("A lembrança da nascente alterou o destino de Nara ou perdeu o material de têmpera.");
        SaveData upstreamSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(upstreamExpedition.CaptureSaveData()))!;
        if (!upstreamSave.UpstreamTrace || upstreamSave.QuestStage != 26 || upstreamSave.Room != 22 || (upstreamSave.ClearedRooms & (1 << 22)) == 0) throw new InvalidOperationException("A nova região e a lembrança da nascente não foram salvas.");
        upstreamExpedition.memoryOverlay = false; upstreamExpedition.player = new PointF(850, 330); upstreamExpedition.Interact(); upstreamExpedition.player = AvenPosition; upstreamExpedition.Interact();
        if (upstreamExpedition.room != 18 || upstreamExpedition.questStage != 27 || upstreamExpedition.memoryTitle != "A TESTEMUNHA SEM REGISTRO" || upstreamExpedition.gold < 320) throw new InvalidOperationException("Aven não registrou o testemunho preservando a escolha de Nara.");
        upstreamExpedition.memoryOverlay = false; upstreamExpedition.Interact();
        if (!upstreamExpedition.forgeMenuOpen) throw new InvalidOperationException("Aven não abriu a têmpera comunitária após receber o testemunho.");
        int goldBeforeTempering = upstreamExpedition.gold, damageBeforeTempering = upstreamExpedition.damage;
        upstreamExpedition.OnKeyDown(upstreamExpedition, new KeyEventArgs(Keys.D3));
        if (upstreamExpedition.forgeMenuOpen || upstreamExpedition.gold != goldBeforeTempering - 75 || upstreamExpedition.tideGlass != 2 || upstreamExpedition.weaponRarities[0] != 1 || upstreamExpedition.weaponBonuses[0] is < 3 or > 6 || upstreamExpedition.weaponAffixes[0] != 3 || upstreamExpedition.damage != damageBeforeTempering + upstreamExpedition.weaponBonuses[0]) throw new InvalidOperationException("A têmpera não aprimorou a arma comum com o afixo escolhido e os custos corretos.");
        int temperedBonus = upstreamExpedition.weaponBonuses[0], goldAfterFirstTempering = upstreamExpedition.gold;
        upstreamExpedition.Interact(); upstreamExpedition.OnKeyDown(upstreamExpedition, new KeyEventArgs(Keys.D2));
        if (upstreamExpedition.weaponRarities[0] != 1 || upstreamExpedition.weaponBonuses[0] != temperedBonus || upstreamExpedition.weaponAffixes[0] != 2 || upstreamExpedition.gold != goldAfterFirstTempering - 75 || upstreamExpedition.tideGlass != 1) throw new InvalidOperationException("A têmpera não substituiu o afixo sem apagar a raridade já obtida.");
        int goldBeforeDuplicate = upstreamExpedition.gold;
        upstreamExpedition.Interact(); upstreamExpedition.OnKeyDown(upstreamExpedition, new KeyEventArgs(Keys.D2));
        if (upstreamExpedition.gold != goldBeforeDuplicate || upstreamExpedition.tideGlass != 1 || !upstreamExpedition.forgeMenuOpen) throw new InvalidOperationException("Um afixo repetido consumiu recursos sem alterar a arma.");
        upstreamExpedition.OnKeyDown(upstreamExpedition, new KeyEventArgs(Keys.Escape));
        SaveData temperedSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(upstreamExpedition.CaptureSaveData()))!;
        if (!ValidWeaponData(temperedSave) || temperedSave.TideGlass != 1 || temperedSave.WeaponAffixes[0] != 2 || temperedSave.WeaponRarities[0] != 1) throw new InvalidOperationException("A têmpera ou o sal vítreo não foram preservados no save.");
        using (var affixForge = new Game())
        {
            affixForge.timer.Stop(); affixForge.StartGame(); affixForge.storyOverlay = false; affixForge.room = 18; affixForge.questStage = 27; affixForge.player = AvenPosition; affixForge.gold = 150; affixForge.tideGlass = 2; affixForge.forgeMenuOpen = true;
            affixForge.OnKeyDown(affixForge, new KeyEventArgs(Keys.D4));
            if (affixForge.weaponAffixes[0] != 4 || Math.Abs(affixForge.VoiceShardCooldown() - 6.3f) > 0.01f || affixForge.WeaponAffixDescription(0) != "do Passo Célere: recarga de técnicas -10%") throw new InvalidOperationException("O Passo Célere não reduziu a recarga nem mostrou seu efeito raro.");
            affixForge.forgeMenuOpen = true; affixForge.OnKeyDown(affixForge, new KeyEventArgs(Keys.D5));
            float beforeGuardedHit = affixForge.health; affixForge.HitPlayer(100);
            if (affixForge.weaponAffixes[0] != 5 || Math.Abs((beforeGuardedHit - affixForge.health) - 92) > 0.01f || affixForge.WeaponAffixDescription(0) != "do Lastro: dano recebido -8%") throw new InvalidOperationException("O Lastro não reduziu o dano recebido nem mostrou seu efeito raro.");
            affixForge.weaponRarities[0] = 2; affixForge.weaponBonuses[0] = 7; affixForge.weaponAffixes[0] = 4; affixForge.weaponAffixes2[0] = 3;
            if (Math.Abs(affixForge.VoiceShardCooldown() - 5.88f) > 0.01f || affixForge.WeaponAffixDescription(0) != "do Passo Célere: recarga de técnicas -16% • da Maré: +18% de dano de técnica") throw new InvalidOperationException("A força do Passo Célere não cresceu na raridade Relíquia.");
            if (Math.Abs(affixForge.WeaponSkillMultiplier() - 1.18f) > 0.01f || !ValidWeaponData(affixForge.CaptureSaveData())) throw new InvalidOperationException("O segundo afixo da relíquia não fortaleceu a build nem passou pela validação.");
            affixForge.weaponAffixes[0] = 5; affixForge.health = 100; affixForge.HitPlayer(100);
            if (Math.Abs(affixForge.health - 12) > 0.01f || affixForge.WeaponAffixDescription(0) != "do Lastro: dano recebido -12% • da Maré: +18% de dano de técnica") throw new InvalidOperationException("Os dois afixos da relíquia não aplicaram ou não exibiram seus efeitos.");
            SaveData newAffixesSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(affixForge.CaptureSaveData()))!;
            if (!ValidWeaponData(newAffixesSave) || newAffixesSave.WeaponAffixes[0] != 5) throw new InvalidOperationException("Os novos afixos não passaram pela validação nem persistiram no save.");
            affixForge.gold = 75; affixForge.tideGlass = 1; affixForge.forgeMenuOpen = true; affixForge.ReforgeWeapon(3);
            if (affixForge.weaponAffixes[0] != 3 || affixForge.weaponAffixes2[0] != 5 || affixForge.gold != 0 || affixForge.tideGlass != 0 || !ValidWeaponData(affixForge.CaptureSaveData())) throw new InvalidOperationException("A têmpera não reordenou os dois afixos da Relíquia sem duplicá-los.");
            affixForge.gold = 75; affixForge.tideGlass = 1; affixForge.forgeMenuOpen = true; affixForge.OnKeyDown(affixForge, new KeyEventArgs(Keys.D6));
            if (affixForge.weaponAffixes[0] != 6 || affixForge.weaponAffixes2[0] != 5 || Math.Abs(affixForge.BasicAttackReach(new Enemy { Kind = EnemyKind.Grunt }) - 93.6f) > 0.01f || !ValidWeaponData(affixForge.CaptureSaveData()))
                throw new InvalidOperationException("A têmpera não permitiu escolher o Fio Esticado sem invalidar a Relíquia.");
        }
        using var lumeUpstream = new Game(); lumeUpstream.timer.Stop(); lumeUpstream.StartGame(); lumeUpstream.room = 22; lumeUpstream.questStage = 25; lumeUpstream.townEnding = 2; lumeUpstream.SetupRoom();
        if (lumeUpstream.enemies.Single(e => e.Kind == EnemyKind.TideWitness).MaxHp != 920) throw new InvalidOperationException("Manter o Lume não alterou a força da Testemunha da Maré.");
        using var reedExpedition = new Game(); reedExpedition.timer.Stop(); reedExpedition.StartGame(); reedExpedition.storyOverlay = false; reedExpedition.room = 22; reedExpedition.questStage = 27; reedExpedition.upstreamTrace = true; reedExpedition.townEnding = 1; reedExpedition.enemies.Clear(); reedExpedition.drops.Clear(); reedExpedition.player = new PointF(850, 330); reedExpedition.Interact();
        if (reedExpedition.room != 23 || reedExpedition.questStage != 28 || reedExpedition.enemies.Count != 6 || reedExpedition.memoryTitle != "COTOVELO DOS JUNCAIS") throw new InvalidOperationException("A ponte não abriu a nova curva do rio após registrar a nascente.");
        reedExpedition.enemies.Clear(); reedExpedition.drops.Clear(); reedExpedition.player = new PointF(850, 330); reedExpedition.Interact();
        if (reedExpedition.room != 24 || reedExpedition.enemies.Count != 5 || reedExpedition.memoryTitle != "PÁTIO DAS AMARRAS LIVRES") throw new InvalidOperationException("O Cotovelo não levou ao Pátio das Amarras Livres.");
        Enemy reedKeeper = reedExpedition.enemies.Single(e => e.Kind == EnemyKind.ReedKeeper);
        if (reedKeeper.MaxHp != 970 || reedExpedition.Blocked(reedKeeper.Pos, reedKeeper.Radius)) throw new InvalidOperationException("A Guardiã do Leito não respeitou a escolha da cidade ou surgiu dentro de um obstáculo.");
        reedKeeper.Cooldown = 0; reedExpedition.player = new PointF(150, 330); reedExpedition.UpdateEnemy(reedKeeper, 0.01f);
        if (reedKeeper.BossPattern != 1 || reedKeeper.Windup <= 0 || reedKeeper.Target != reedExpedition.player) throw new InvalidOperationException("A Guardiã não anunciou a marca que segue o Escutador.");
        reedKeeper.Windup = 0; reedKeeper.Cooldown = 0; reedExpedition.UpdateEnemy(reedKeeper, 0.01f);
        if (reedKeeper.BossPattern != 2 || reedKeeper.Windup <= 0 || reedKeeper.Target == PointF.Empty) throw new InvalidOperationException("A Guardiã não anunciou o corte pelo pátio.");
        reedKeeper.Windup = 0; reedKeeper.Cooldown = 0; reedExpedition.UpdateEnemy(reedKeeper, 0.01f);
        if (reedKeeper.BossPattern != 0 || reedKeeper.Windup <= 0) throw new InvalidOperationException("A Guardiã não concluiu o ciclo de três ataques.");
        reedExpedition.enemies.Clear(); reedExpedition.enemies.Add(reedKeeper); reedExpedition.drops.Clear(); reedKeeper.Hp = 1; reedKeeper.Windup = 0; reedExpedition.Defeat(reedKeeper);
        if (!reedExpedition.drops.Any(d => d.Kind == "beyond") || !reedExpedition.drops.Any(d => d.Kind == "glass" && d.Amount == 2)) throw new InvalidOperationException("A Guardiã não deixou o testemunho e o sal vítreo.");
        foreach (Drop drop in reedExpedition.drops.ToArray()) reedExpedition.Collect(drop);
        if (!reedExpedition.beyondTrace || reedExpedition.questStage != 29 || reedExpedition.memoryTitle != "NÓS QUE PODEM SER DESFEITOS" || reedExpedition.tideGlass != 2) throw new InvalidOperationException("O testemunho tornou-se uma ordem ou perdeu o material comunitário.");
        SaveData reedSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(reedExpedition.CaptureSaveData()))!;
        if (!reedSave.BeyondTrace || reedSave.QuestStage != 29 || reedSave.Room != 24 || (reedSave.ClearedRooms & (1 << 24)) == 0) throw new InvalidOperationException("A nova região e o testemunho não foram salvos.");
        reedExpedition.memoryOverlay = false; reedExpedition.player = new PointF(850, 330); reedExpedition.Interact(); reedExpedition.player = AvenPosition; int reedGold = reedExpedition.gold; reedExpedition.Interact();
        if (reedExpedition.room != 18 || reedExpedition.questStage != 30 || reedExpedition.gold != reedGold + 420 || reedExpedition.potions < 3 || reedExpedition.memoryTitle != "AVEN GUARDA O SINAL") throw new InvalidOperationException($"Aven não registrou a passagem coletiva e forneceu provisões: sala {reedExpedition.room}, etapa {reedExpedition.questStage}, ouro {reedExpedition.gold - reedGold}, poções {reedExpedition.potions}, memória {reedExpedition.memoryTitle}.");
        using var lumeReed = new Game(); lumeReed.timer.Stop(); lumeReed.StartGame(); lumeReed.room = 24; lumeReed.townEnding = 2; lumeReed.SetupRoom();
        if (lumeReed.enemies.Single(e => e.Kind == EnemyKind.ReedKeeper).MaxHp != 1060) throw new InvalidOperationException("Manter o Lume não alterou a força da Guardiã do Leito.");
        using var floodExpedition = new Game(); floodExpedition.timer.Stop(); floodExpedition.StartGame(); floodExpedition.storyOverlay = false; floodExpedition.room = 18; floodExpedition.questStage = 30; floodExpedition.beyondTrace = true; floodExpedition.upstreamTrace = true; floodExpedition.westTrace = true; floodExpedition.townEnding = 1; floodExpedition.player = new PointF(850, 330); floodExpedition.Interact();
        if (floodExpedition.room != 25 || floodExpedition.questStage != 31 || floodExpedition.enemies.Count != 6 || floodExpedition.memoryTitle != "MOINHOS DA VAZANTE") throw new InvalidOperationException("Aven não abriu a rota dos Moinhos da Vazante após registrar o testemunho.");
        floodExpedition.player = new PointF(480, 330); if (floodExpedition.CurrentDrift() != new PointF(58, 0)) throw new InvalidOperationException("A corrente dos moinhos não alterou o posicionamento no canal.");
        floodExpedition.player = new PointF(180, 330); if (floodExpedition.CurrentDrift() != PointF.Empty) throw new InvalidOperationException("A corrente afetou o caminho seco fora do canal.");
        floodExpedition.enemies.Clear(); floodExpedition.drops.Clear(); floodExpedition.player = new PointF(850, 330); floodExpedition.Interact();
        if (floodExpedition.room != 26 || floodExpedition.enemies.Count != 5 || floodExpedition.memoryTitle != "CÂMARA DO VEIO PRESO") throw new InvalidOperationException("Os moinhos não levaram à Câmara do Veio Preso.");
        Enemy flowKeeper = floodExpedition.enemies.Single(e => e.Kind == EnemyKind.FlowKeeper);
        if (flowKeeper.MaxHp != 1120 || floodExpedition.Blocked(flowKeeper.Pos, flowKeeper.Radius)) throw new InvalidOperationException("O Vigia do Veio Preso não respeitou o ramo de Véspera ou surgiu dentro de um obstáculo.");
        flowKeeper.Cooldown = 0; floodExpedition.player = new PointF(150, 330); floodExpedition.UpdateEnemy(flowKeeper, 0.01f);
        if (flowKeeper.BossPattern != 1 || flowKeeper.Windup <= 0 || flowKeeper.Target != floodExpedition.player) throw new InvalidOperationException("O Vigia não anunciou a marca de retorno.");
        flowKeeper.Windup = 0; flowKeeper.Cooldown = 0; floodExpedition.UpdateEnemy(flowKeeper, 0.01f);
        if (flowKeeper.BossPattern != 2 || flowKeeper.Windup <= 0 || flowKeeper.Target == PointF.Empty) throw new InvalidOperationException("O Vigia não anunciou o corte pela corrente.");
        flowKeeper.Windup = 0; flowKeeper.Cooldown = 0; floodExpedition.UpdateEnemy(flowKeeper, 0.01f);
        if (flowKeeper.BossPattern != 0 || flowKeeper.Windup <= 0) throw new InvalidOperationException("O Vigia não concluiu o ciclo de três ataques.");
        floodExpedition.enemies.Clear(); floodExpedition.enemies.Add(flowKeeper); floodExpedition.drops.Clear(); flowKeeper.Hp = 1; flowKeeper.Windup = 0; floodExpedition.Defeat(flowKeeper);
        if (!floodExpedition.drops.Any(d => d.Kind == "ebb") || !floodExpedition.drops.Any(d => d.Kind == "glass" && d.Amount == 3)) throw new InvalidOperationException("O Vigia não deixou a memória e o sal vítreo.");
        foreach (Drop drop in floodExpedition.drops.ToArray()) floodExpedition.Collect(drop);
        if (floodExpedition.questStage != 32 || floodExpedition.tideGlass != 3 || floodExpedition.memoryTitle != "A CORRENTE NÃO É UMA ROTA") throw new InvalidOperationException("A memória do veio ou o sal vítreo não foram recuperados.");
        SaveData flowSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(floodExpedition.CaptureSaveData()))!;
        if (flowSave.Room != 26 || flowSave.QuestStage != 32 || flowSave.EbbSeal || (flowSave.ClearedRooms & (1 << 26)) == 0) throw new InvalidOperationException("O progresso do veio não foi salvo antes da recompensa de Aven.");
        floodExpedition.memoryOverlay = false; floodExpedition.player = new PointF(850, 330); floodExpedition.Interact(); floodExpedition.player = AvenPosition; floodExpedition.gold = 0; floodExpedition.potions = 0; floodExpedition.Interact();
        if (floodExpedition.room != 18 || floodExpedition.questStage != 33 || !floodExpedition.ebbSeal || floodExpedition.gold != 480 || floodExpedition.potions != 2 || floodExpedition.memoryTitle != "A MARCA DA VAZANTE") throw new InvalidOperationException("Aven não liberou a Vazante e as provisões da comunidade.");
        SaveData ebbSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(floodExpedition.CaptureSaveData()))!;
        if (!ebbSave.EbbSeal || ebbSave.QuestStage != 33 || ebbSave.Room != 18) throw new InvalidOperationException("A habilidade Vazante não persistiu no save.");
        floodExpedition.room = 26; floodExpedition.enemies.Clear(); floodExpedition.drops.Clear(); floodExpedition.ebbSeal = true; floodExpedition.pullCd = 0; floodExpedition.player = new PointF(300, 330);
        Enemy pulledEcho = new() { Pos = new PointF(400, 330), Kind = EnemyKind.Grunt, Hp = 1000, MaxHp = 1000, Cooldown = 5 }; floodExpedition.enemies.Add(pulledEcho); floodExpedition.CastEbbPull();
        if (pulledEcho.Hp >= 1000 || pulledEcho.Pos.X >= 400 || pulledEcho.Stagger <= 0 || floodExpedition.pullCd != 12) throw new InvalidOperationException("A Vazante não reuniu, feriu e interrompeu o eco ou respeitou sua recarga.");
        using var ebbBuild = new Game(); ebbBuild.timer.Stop(); ebbBuild.StartGame(); ebbBuild.room = 26; ebbBuild.ebbSeal = true; ebbBuild.ownedCharms = 4; ebbBuild.equippedCharm = 3; ebbBuild.weaponRarities[0] = 1; ebbBuild.weaponAffixes[0] = 3; ebbBuild.enemies.Clear(); ebbBuild.player = new PointF(300, 330);
        Enemy tunedEcho = new() { Pos = new PointF(400, 330), Kind = EnemyKind.Grunt, Hp = 1000, MaxHp = 1000, Cooldown = 5 }; ebbBuild.enemies.Add(tunedEcho); ebbBuild.CastEbbPull();
        if (1000 - tunedEcho.Hp <= 15) throw new InvalidOperationException("O Vidro de Vigília e o afixo da Maré não fortaleceram a Vazante.");
        floodExpedition.pullCd = 0; floodExpedition.enemies.Clear(); Enemy pulledBoss = new() { Pos = new PointF(500, 230), Kind = EnemyKind.FlowKeeper, Hp = 1000, MaxHp = 1000 }; floodExpedition.enemies.Add(pulledBoss); floodExpedition.player = new PointF(400, 230); floodExpedition.CastEbbPull();
        if (pulledBoss.Hp >= 1000 || pulledBoss.Pos.X >= 500 || pulledBoss.Stagger > 0) throw new InvalidOperationException("A Vazante não teve efeito reduzido e sem interrupção contra chefes.");
        using var lumeFlow = new Game(); lumeFlow.timer.Stop(); lumeFlow.StartGame(); lumeFlow.room = 26; lumeFlow.questStage = 31; lumeFlow.beyondTrace = true; lumeFlow.upstreamTrace = true; lumeFlow.townEnding = 2; lumeFlow.SetupRoom();
        if (lumeFlow.enemies.Single(e => e.Kind == EnemyKind.FlowKeeper).MaxHp != 1230) throw new InvalidOperationException("Manter o Lume não alterou a força do Vigia do Veio Preso.");
        using var counterExpedition = new Game(); counterExpedition.timer.Stop(); counterExpedition.StartGame(); counterExpedition.storyOverlay = false; counterExpedition.room = 18; counterExpedition.questStage = 33; counterExpedition.ebbSeal = true; counterExpedition.player = new PointF(850, 330); counterExpedition.Interact();
        if (counterExpedition.room != 27 || counterExpedition.questStage != 34 || counterExpedition.enemies.Count != 6 || counterExpedition.memoryTitle != "ESCARPA DO CONTRAFLUXO") throw new InvalidOperationException("Aven não abriu o Capítulo XI após ensinar a Vazante.");
        counterExpedition.enemies.Clear(); counterExpedition.drops.Clear(); counterExpedition.player = new PointF(850, 330); counterExpedition.Interact();
        if (counterExpedition.room != 28 || counterExpedition.enemies.Count != 5 || !counterExpedition.enemies.Any(e => e.Kind == EnemyKind.CounterCurrent)) throw new InvalidOperationException("A escarpa não abriu o Coração das Duas Margens e seu chefe.");
        counterExpedition.player = new PointF(600, 330); counterExpedition.windupPulse = 0; PointF inwardDrift = counterExpedition.CurrentDrift(); counterExpedition.windupPulse = 3.5f / 1.35f; PointF outwardDrift = counterExpedition.CurrentDrift();
        if (inwardDrift.X >= 0 || outwardDrift.X <= 0 || inwardDrift.X != -outwardDrift.X) throw new InvalidOperationException("A corrente do Coração não alternou de atração para repulsão.");
        Enemy tuner = counterExpedition.enemies.Single(e => e.Kind == EnemyKind.CounterCurrent);
        tuner.Cooldown = 0; counterExpedition.player = new PointF(150, 330); counterExpedition.UpdateEnemy(tuner, 0.01f); if (tuner.BossPattern != 1 || tuner.Windup <= 0) throw new InvalidOperationException("O Afinador não anunciou a primeira marca de corrente.");
        tuner.Windup = 0; tuner.Cooldown = 0; counterExpedition.UpdateEnemy(tuner, 0.01f); if (tuner.BossPattern != 2 || tuner.Windup <= 0) throw new InvalidOperationException("O Afinador não anunciou seu corte em linha.");
        tuner.Windup = 0; tuner.Cooldown = 0; counterExpedition.UpdateEnemy(tuner, 0.01f); if (tuner.BossPattern != 0 || tuner.Windup <= 0) throw new InvalidOperationException("O Afinador não completou o ciclo de três ataques.");
        counterExpedition.enemies.Clear(); counterExpedition.enemies.Add(tuner); counterExpedition.drops.Clear(); tuner.Hp = 1; tuner.Windup = 0; counterExpedition.Defeat(tuner);
        if (!counterExpedition.drops.Any(d => d.Kind == "counter") || !counterExpedition.drops.Any(d => d.Kind == "glass" && d.Amount == 3)) throw new InvalidOperationException("O Afinador não deixou sua memória e o sal vítreo.");
        foreach (Drop drop in counterExpedition.drops.ToArray()) counterExpedition.Collect(drop);
        if (!counterExpedition.counterTrace || counterExpedition.questStage != 35 || counterExpedition.tideGlass != 3 || counterExpedition.memoryTitle != "O AFINADOR DAS MARGENS") throw new InvalidOperationException("A memória do Afinador ou o material não foram recuperados.");
        SaveData counterSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(counterExpedition.CaptureSaveData()))!;
        if (counterSave.Room != 28 || counterSave.QuestStage != 35 || !counterSave.CounterTrace || (counterSave.ClearedRooms & (1 << 28)) == 0) throw new InvalidOperationException("O novo capítulo não persistiu a sala, a memória e a conclusão do chefe.");
        counterExpedition.memoryOverlay = false; counterExpedition.player = new PointF(850, 330); counterExpedition.Interact(); counterExpedition.player = AvenPosition; int counterGold = counterExpedition.gold; counterExpedition.Interact();
        if (counterExpedition.room != 18 || counterExpedition.questStage != 36 || counterExpedition.gold != counterGold + 560 || counterExpedition.tideGlass != 6 || counterExpedition.potions < 2) throw new InvalidOperationException("Aven não registrou o novo relato nem entregou as provisões.");
        counterExpedition.memoryOverlay = false; counterExpedition.player = AvenPosition; counterExpedition.Interact();
        if (counterExpedition.questStage != 37 || counterExpedition.memoryTitle != "O VERTEDOURO ANTIGO") throw new InvalidOperationException("Aven não abriu o Capítulo XII nem apresentou a nova região.");
        counterExpedition.memoryOverlay = false; counterExpedition.player = new PointF(850, 330); counterExpedition.Interact();
        if (counterExpedition.room != 27 || counterExpedition.enemies.Count != 0) throw new InvalidOperationException("Aven não reabriu a passagem limpa da Escarpa para o Capítulo XII.");
        counterExpedition.player = new PointF(850, 330); counterExpedition.Interact();
        if (counterExpedition.room != 28 || counterExpedition.enemies.Count != 0) throw new InvalidOperationException("A Escarpa não retornou ao Coração das Duas Margens já limpo.");
        counterExpedition.player = new PointF(850, 330); counterExpedition.Interact();
        if (counterExpedition.room != 29 || counterExpedition.questStage != 38 || counterExpedition.enemies.Count != 6) throw new InvalidOperationException("A Cantaria das Vozes Livres não abriu como primeira sala do Capítulo XII.");
        counterExpedition.enemies.Clear(); counterExpedition.drops.Clear(); counterExpedition.player = new PointF(850, 330); counterExpedition.Interact();
        if (counterExpedition.room != 30 || counterExpedition.enemies.Count != 5 || !counterExpedition.enemies.Any(e => e.Kind == EnemyKind.RiftCantor)) throw new InvalidOperationException("A Câmara da Contravoz não abriu com seu chefe.");
        Enemy cantor = counterExpedition.enemies.Single(e => e.Kind == EnemyKind.RiftCantor); cantor.Cooldown = 0;
        for (int pattern = 1; pattern <= 3; pattern++) { counterExpedition.UpdateEnemy(cantor, 0.01f); if (cantor.BossPattern != pattern % 3 || cantor.Windup <= 0) throw new InvalidOperationException("A Contravoz não anunciou seu ciclo de três padrões."); cantor.Windup = 0; cantor.Cooldown = 0; }
        counterExpedition.enemies.Clear(); counterExpedition.enemies.Add(cantor); counterExpedition.drops.Clear(); cantor.Hp = 1; cantor.Windup = 0; counterExpedition.Defeat(cantor);
        if (!counterExpedition.drops.Any(d => d.Kind == "verse") || EnemyName(cantor) != "A CONTRAVOZ") throw new InvalidOperationException("A Contravoz não deixou sua nota única ou não recebeu seu nome.");
        foreach (Drop drop in counterExpedition.drops.ToArray()) counterExpedition.Collect(drop);
        if (!counterExpedition.riftVerse || counterExpedition.questStage != 39 || counterExpedition.memoryTitle != "A NOTA QUE NÃO MANDA") throw new InvalidOperationException("A nota da Contravoz não atualizou a memória e a missão.");
        SaveData verseSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(counterExpedition.CaptureSaveData()))!;
        if (verseSave.Room != 30 || verseSave.QuestStage != 39 || !verseSave.RiftVerse || (verseSave.ClearedRooms & (1 << 30)) == 0 || JsonSerializer.Deserialize<SaveData>("{}")?.RiftVerse != false) throw new InvalidOperationException("A progressão da Contravoz não persistiu ou invalidou saves antigos.");
        counterExpedition.memoryOverlay = false; counterExpedition.player = new PointF(850, 330); counterExpedition.Interact(); counterExpedition.player = AvenPosition; int refrainGold = counterExpedition.gold; counterExpedition.Interact();
        if (counterExpedition.room != 18 || counterExpedition.questStage != 40 || !counterExpedition.refrainSong || counterExpedition.gold != refrainGold + 650 || counterExpedition.tideGlass != 10) throw new InvalidOperationException("Aven não ensinou o Coro de Retorno nem entregou a recompensa.");
        counterExpedition.room = 29; counterExpedition.SetupRoom(); counterExpedition.enemies.Clear(); counterExpedition.player = new PointF(480, 330); Enemy chorusMob = new() { Pos = new PointF(520, 330), Kind = EnemyKind.Grunt, Hp = 1000, MaxHp = 1000 }; Enemy chorusBoss = new() { Pos = new PointF(550, 330), Kind = EnemyKind.RiftCantor, Hp = 1000, MaxHp = 1000 }; counterExpedition.enemies.AddRange(new[] { chorusMob, chorusBoss }); counterExpedition.refrainCd = 0; float mobHp = chorusMob.Hp, bossHp = chorusBoss.Hp; counterExpedition.CastRefrain();
        if (chorusMob.Hp >= mobHp || chorusMob.Stagger <= 0 || chorusBoss.Hp >= bossHp || chorusBoss.Stagger > 0 || counterExpedition.refrainCd <= 0) throw new InvalidOperationException("O Coro de Retorno não feriu ecos, reduziu dano em chefe ou respeitou sua recarga.");
        using var glassExpedition = new Game(); glassExpedition.timer.Stop(); glassExpedition.StartGame(); glassExpedition.storyOverlay = false; glassExpedition.room = 18; glassExpedition.questStage = 40; glassExpedition.riftVerse = true; glassExpedition.refrainSong = true; glassExpedition.player = AvenPosition; glassExpedition.Interact();
        if (glassExpedition.questStage != 41 || glassExpedition.memoryTitle != "A PEDRA ALÉM DO VERTEDOURO") throw new InvalidOperationException("Aven não abriu a jornada do Vidro Mudo após receber o Coro de Retorno.");
        glassExpedition.player = new PointF(850, 330); glassExpedition.Interact();
        if (glassExpedition.room != 31 || glassExpedition.enemies.Count != 6) throw new InvalidOperationException("A Garganta do Vidro Mudo não abriu com seus encontros.");
        glassExpedition.enemies.Clear(); glassExpedition.drops.Clear(); glassExpedition.player = new PointF(850, 330); glassExpedition.Interact();
        if (glassExpedition.room != 32 || glassExpedition.enemies.Count != 5) throw new InvalidOperationException("A Fonte sob a Pedra não abriu com o Arauto de Vidro.");
        Enemy herald = glassExpedition.enemies.Single(e => e.Kind == EnemyKind.GlassHerald); glassExpedition.player = herald.Pos;
        foreach (int pattern in new[] { 1, 2, 0 }) { herald.Windup = 0; herald.Cooldown = 0; glassExpedition.UpdateEnemy(herald, 0.01f); if (herald.BossPattern != pattern || herald.Windup <= 0) throw new InvalidOperationException("O Arauto de Vidro não alternou os três ataques anunciados."); }
        glassExpedition.Defeat(herald); Drop deepDrop = glassExpedition.drops.Single(d => d.Kind == "deep"); glassExpedition.Collect(deepDrop);
        if (!glassExpedition.deepTrace || glassExpedition.questStage != 42 || glassExpedition.drops.Any(d => d.Kind == "deep")) throw new InvalidOperationException("A lembrança sem nome não atualizou a missão ou não foi coletada.");
        glassExpedition.drops.Clear(); glassExpedition.enemies.Clear(); glassExpedition.MarkRoomCleared(); SaveData glassSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(glassExpedition.CaptureSaveData()))!;
        if (glassSave.Room != 32 || glassSave.QuestStage != 42 || !glassSave.DeepTrace || (glassSave.ClearedRooms & (1L << 32)) == 0 || JsonSerializer.Deserialize<SaveData>("{}")?.DeepTrace != false) throw new InvalidOperationException("O capítulo XIII ou a sala 32 não persistiu no save longo, ou incompatibilizou saves antigos.");
        glassExpedition.player = new PointF(850, 330); glassExpedition.Interact(); glassExpedition.player = AvenPosition; int glassRewardGold = glassExpedition.gold; glassExpedition.Interact();
        if (glassExpedition.room != 18 || glassExpedition.questStage != 43 || glassExpedition.gold != glassRewardGold + 820 || glassExpedition.tideGlass != 5) throw new InvalidOperationException("Aven não registrou o eco sem dono nem entregou a recompensa do capítulo XIII.");
        using var lanternExpedition = new Game(); lanternExpedition.timer.Stop(); lanternExpedition.StartGame(); lanternExpedition.storyOverlay = false; lanternExpedition.room = 18; lanternExpedition.questStage = 43; lanternExpedition.chorusMemory = lanternExpedition.firstMemory = lanternExpedition.westTrace = lanternExpedition.upstreamTrace = lanternExpedition.beyondTrace = lanternExpedition.ebbSeal = lanternExpedition.counterTrace = lanternExpedition.refrainSong = lanternExpedition.riftVerse = lanternExpedition.deepTrace = true; lanternExpedition.townEnding = 1; lanternExpedition.player = AvenPosition; lanternExpedition.Interact();
        if (lanternExpedition.questStage != 44 || lanternExpedition.memoryTitle != "AS LANTERNAS QUE OLHAM PARA DENTRO") throw new InvalidOperationException("Aven não abriu o Capítulo XIV depois de registrar o eco sem dono.");
        lanternExpedition.player = new PointF(850, 330); lanternExpedition.InteractRefuge();
        if (lanternExpedition.room != 33 || lanternExpedition.enemies.Count != 6 || lanternExpedition.roomNames[33] != "PASSAGEM DAS LANTERNAS BAIXAS") throw new InvalidOperationException("A Passagem das Lanternas Baixas não abriu com seus encontros.");
        lanternExpedition.enemies.Clear(); lanternExpedition.drops.Clear(); lanternExpedition.player = new PointF(850, 330); lanternExpedition.Interact();
        if (lanternExpedition.room != 34 || lanternExpedition.enemies.Count != 5 || !lanternExpedition.enemies.Any(e => e.Kind == EnemyKind.LanternRegent)) throw new InvalidOperationException("O Observatório da Maré Interior não abriu com o Regente das Lanternas.");
        Enemy regent = lanternExpedition.enemies.Single(e => e.Kind == EnemyKind.LanternRegent); lanternExpedition.player = regent.Pos;
        foreach (int pattern in new[] { 1, 2, 0 }) { regent.Windup = 0; regent.Cooldown = 0; lanternExpedition.UpdateEnemy(regent, 0.01f); if (regent.BossPattern != pattern || regent.Windup <= 0) throw new InvalidOperationException("O Regente das Lanternas não anunciou os três padrões de ataque."); }
        lanternExpedition.Defeat(regent); Drop signalDrop = lanternExpedition.drops.Single(d => d.Kind == "signal"); Drop lumenKnot = lanternExpedition.drops.Single(d => d.Kind == "charm" && d.Amount == 6); lanternExpedition.Collect(signalDrop); lanternExpedition.Collect(lumenKnot);
        if (!lanternExpedition.lanternSignal || lanternExpedition.questStage != 45 || lanternExpedition.memoryTitle != "A LUZ QUE ESPERA" || (lanternExpedition.ownedCharms & 32) == 0 || lanternExpedition.equippedCharm != 6) throw new InvalidOperationException("O sinal ou o Nó de Lume do Regente não foi recuperado.");
        lanternExpedition.enemies.Clear(); lanternExpedition.drops.Clear(); lanternExpedition.MarkRoomCleared(); SaveData lanternSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(lanternExpedition.CaptureSaveData()))!;
        if (lanternSave.Room != 34 || lanternSave.QuestStage != 45 || !lanternSave.LanternSignal || lanternSave.BeaconLight || lanternSave.OwnedCharms != 32 || lanternSave.EquippedCharm != 6 || (lanternSave.ClearedRooms & (1L << 34)) == 0 || !ValidSaveData(lanternSave) || JsonSerializer.Deserialize<SaveData>("{}")?.LanternSignal != false) throw new InvalidOperationException("A sala 34, o sinal ou o sexto talismã não persistiu ou falhou na validação de save.");
        lanternExpedition.player = new PointF(850, 330); lanternExpedition.Interact();
        if (lanternExpedition.room != 18) throw new InvalidOperationException("O Escutador não conseguiu retornar do Observatório ao Refúgio.");
        lanternExpedition.player = AvenPosition; int lanternRewardGold = lanternExpedition.gold; lanternExpedition.Interact();
        if (lanternExpedition.questStage != 46 || !lanternExpedition.beaconLight || lanternExpedition.gold != lanternRewardGold + 980 || lanternExpedition.tideGlass != 6 || lanternExpedition.potions != 5) throw new InvalidOperationException("Aven não ensinou o Farol de Vigília nem entregou as provisões do Capítulo XIV.");
        lanternExpedition.room = 34; lanternExpedition.SetupRoom(); lanternExpedition.enemies.Clear(); lanternExpedition.player = new PointF(430, 330); lanternExpedition.memoryOverlay = false; lanternExpedition.skillMenuOpen = true; lanternExpedition.OnKeyDown(lanternExpedition, new KeyEventArgs(Keys.D6)); lanternExpedition.skillMenuOpen = false; lanternExpedition.beaconCd = 0;
        Enemy beaconTarget = new() { Pos = new PointF(470, 330), Kind = EnemyKind.Grunt, Hp = 1000, MaxHp = 1000 }; lanternExpedition.enemies.Add(beaconTarget); lanternExpedition.CastEquippedSkill(0); float beaconHp = beaconTarget.Hp; lanternExpedition.TickBeacon(0.2f); float firstPulseHp = beaconTarget.Hp; lanternExpedition.TickBeacon(0.9f);
        SaveData beaconSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(lanternExpedition.CaptureSaveData()))!;
        if (!lanternExpedition.beaconLight || Math.Abs(lanternExpedition.beaconTime - 6.9f) > 0.001f || Math.Abs(lanternExpedition.beaconCd - 25.5f) > 0.001f || beaconTarget.Hp >= firstPulseHp || beaconTarget.Hp >= beaconHp || Math.Abs(lanternExpedition.beaconPulseCd - 0.85f) > 0.001f || !ValidSaveData(beaconSave) || !beaconSave.BeaconLight || !beaconSave.EquippedSkills.SequenceEqual(new[] { 6, 0, 0 }) || beaconSave.OwnedCharms != 32 || beaconSave.EquippedCharm != 6 || !lanternSave.EquippedSkills.SequenceEqual(new[] { 1, 0, 0 })) throw new InvalidOperationException("O Nó de Lume não estendeu a duração do Farol, acelerou os pulsos e preservou o talismã em um save válido.");
        lanternExpedition.enemies.Clear(); lanternExpedition.drops.Clear(); lanternExpedition.room = 18; lanternExpedition.SetupRoom(); lanternExpedition.memoryOverlay = false; lanternExpedition.player = AvenPosition; lanternExpedition.Interact();
        if (lanternExpedition.questStage != 47 || lanternExpedition.memoryTitle != "AS LINHAS QUE NÃO APONTAM") throw new InvalidOperationException("Aven não abriu o Capítulo XV após ensinar o Farol de Vigília.");
        lanternExpedition.player = new PointF(850, 330); lanternExpedition.Interact();
        if (lanternExpedition.room != 35 || lanternExpedition.enemies.Count != 6 || lanternExpedition.roomNames[35] != "TRILHA DAS LINHAS ABERTAS") throw new InvalidOperationException("A Trilha das Linhas Abertas não iniciou o Capítulo XV.");
        lanternExpedition.enemies.Clear(); lanternExpedition.drops.Clear(); lanternExpedition.player = new PointF(850, 330); lanternExpedition.Interact();
        if (lanternExpedition.room != 36 || lanternExpedition.enemies.Count != 5 || !lanternExpedition.enemies.Any(e => e.Kind == EnemyKind.RouteScribe)) throw new InvalidOperationException("A Galeria da Rota Não Escrita não abriu com o Escriba do Traçado.");
        Enemy routeBoss = lanternExpedition.enemies.Single(e => e.Kind == EnemyKind.RouteScribe); if (routeBoss.MaxHp != 2250) throw new InvalidOperationException("A decisão de devolver os nomes não enfraqueceu o Escriba do Traçado.");
        lanternExpedition.invulnerable = 10;
        foreach (int pattern in new[] { 1, 2, 0 }) { routeBoss.Cooldown = 0; lanternExpedition.UpdateEnemy(routeBoss, 0.01f); if (routeBoss.BossPattern != pattern || routeBoss.Windup <= 0) throw new InvalidOperationException("O Escriba do Traçado não anunciou os três ataques do ciclo."); routeBoss.Windup = 0; }
        lanternExpedition.enemies.Clear(); lanternExpedition.enemies.Add(routeBoss); lanternExpedition.drops.Clear(); routeBoss.Hp = 1; routeBoss.Windup = 0; lanternExpedition.Defeat(routeBoss);
        Drop routeDrop = lanternExpedition.drops.Single(d => d.Kind == "route"); lanternExpedition.Collect(routeDrop);
        if (!lanternExpedition.routeMemory || lanternExpedition.questStage != 48 || lanternExpedition.memoryTitle != "AS ROTAS QUE PERMANECEM ABERTAS") throw new InvalidOperationException("A memória das rotas não atualizou o Capítulo XV.");
        lanternExpedition.enemies.Clear(); lanternExpedition.drops.Clear(); lanternExpedition.MarkRoomCleared(); SaveData routeSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(lanternExpedition.CaptureSaveData()))!;
        if (routeSave.Room != 36 || routeSave.QuestStage != 48 || !routeSave.RouteMemory || (routeSave.ClearedRooms & (1L << 36)) == 0 || !ValidSaveData(routeSave) || JsonSerializer.Deserialize<SaveData>("{}")?.RouteMemory != false) throw new InvalidOperationException("O traçado da galeria não persistiu em um save válido nem preservou a compatibilidade antiga.");
        lanternExpedition.player = new PointF(850, 330); lanternExpedition.Interact(); lanternExpedition.memoryOverlay = false; lanternExpedition.player = AvenPosition; int routeRewardGold = lanternExpedition.gold; lanternExpedition.Interact(); SaveData finalRouteSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(lanternExpedition.CaptureSaveData()))!;
        if (lanternExpedition.room != 18 || lanternExpedition.questStage != 49 || lanternExpedition.gold != routeRewardGold + 1300 || !ValidSaveData(finalRouteSave)) throw new InvalidOperationException("Aven não recebeu o traçado nem validou a recompensa final do capítulo.");
        lanternExpedition.memoryOverlay = false; lanternExpedition.player = AvenPosition; lanternExpedition.Interact(); lanternExpedition.memoryOverlay = false;
        if (lanternExpedition.questStage != 50) throw new InvalidOperationException("Aven não abriu o Capítulo XVI após registrar as rotas.");
        lanternExpedition.player = new PointF(850, 330); lanternExpedition.Interact();
        if (lanternExpedition.room != 37 || lanternExpedition.enemies.Count != 6) throw new InvalidOperationException("A Soleira do Silêncio não abriu com seus encontros.");
        lanternExpedition.enemies.Clear(); lanternExpedition.drops.Clear(); lanternExpedition.player = new PointF(850, 330); lanternExpedition.Interact();
        if (lanternExpedition.room != 38 || lanternExpedition.enemies.Count != 1 || lanternExpedition.enemies[0].Kind != EnemyKind.DeepSilence) throw new InvalidOperationException("O Silêncio de Baixo não apareceu na Câmara do Último Eco.");
        Enemy silenceBoss = lanternExpedition.enemies[0]; silenceBoss.Cooldown = 0; lanternExpedition.player = new PointF(150, 330); lanternExpedition.UpdateEnemy(silenceBoss, 0.01f); int firstSilencePattern = silenceBoss.BossPattern;
        silenceBoss.Windup = 0; silenceBoss.Cooldown = 0; lanternExpedition.UpdateEnemy(silenceBoss, 0.01f); int secondSilencePattern = silenceBoss.BossPattern;
        PointF echoLineMidpoint = Scale(Add(silenceBoss.Target2Start, silenceBoss.Target2), 0.5f);
        if (secondSilencePattern != 2 || silenceBoss.Target2 == PointF.Empty || DistanceToSegment(echoLineMidpoint, silenceBoss.Pos, silenceBoss.Target) < 60 || DistanceToSegment(echoLineMidpoint, silenceBoss.Target2Start, silenceBoss.Target2) > 1)
            throw new InvalidOperationException("O terceiro padrão do Silêncio não abriu sua segunda linha paralela, visível e evitável.");
        lanternExpedition.player = echoLineMidpoint; lanternExpedition.health = lanternExpedition.maxHealth = 1000; lanternExpedition.invulnerable = lanternExpedition.dashTime = 0; silenceBoss.Windup = 0.01f; lanternExpedition.UpdateEnemy(silenceBoss, 0.02f);
        if (lanternExpedition.health >= 1000) throw new InvalidOperationException("A segunda linha do Rastro Duplo não atingiu quem permaneceu nela.");
        silenceBoss.Windup = 0; silenceBoss.Cooldown = 0; lanternExpedition.UpdateEnemy(silenceBoss, 0.01f);
        if (firstSilencePattern != 1 || secondSilencePattern != 2 || silenceBoss.BossPattern != 0) throw new InvalidOperationException("Os três padrões anunciados do Silêncio de Baixo não alternaram.");
        lanternExpedition.Defeat(silenceBoss); Drop silenceDrop = lanternExpedition.drops.Single(d => d.Kind == "silence"); lanternExpedition.Collect(silenceDrop); lanternExpedition.drops.Clear();
        SaveData silenceSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(lanternExpedition.CaptureSaveData()))!;
        if (!lanternExpedition.silenceMemory || lanternExpedition.questStage != 51 || !ValidSaveData(silenceSave) || JsonSerializer.Deserialize<SaveData>("{}")?.SilenceMemory != false) throw new InvalidOperationException("A memória final não foi coletada, salva ou migrada com segurança.");
        lanternExpedition.memoryOverlay = false; lanternExpedition.player = new PointF(850, 330); lanternExpedition.Interact(); lanternExpedition.memoryOverlay = false; lanternExpedition.player = AvenPosition; lanternExpedition.Interact(); lanternExpedition.OnKeyDown(lanternExpedition, new KeyEventArgs(Keys.D1));
        SaveData finalEndingSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(lanternExpedition.CaptureSaveData()))!;
        if (lanternExpedition.endingChoiceOpen || lanternExpedition.finalEnding != 1 || lanternExpedition.questStage != 52 || !ValidSaveData(finalEndingSave) || lanternExpedition.maxHealth <= 100) throw new InvalidOperationException("A decisão final não registrou o epílogo de memórias devolvidas nem o bônus correspondente.");
        lanternExpedition.room = 38; lanternExpedition.SetupRoom(); lanternExpedition.enemies.Clear(); lanternExpedition.drops.Clear(); lanternExpedition.player = new PointF(450, 330); lanternExpedition.health = 45; lanternExpedition.EquipSkill(1, 7); Enemy returnedEcho = new() { Pos = new PointF(510, 330), Kind = EnemyKind.Grunt, Hp = 1000, MaxHp = 1000 }; lanternExpedition.enemies.Add(returnedEcho); lanternExpedition.CastEquippedSkill(1);
        SaveData returnedVoicesSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(lanternExpedition.CaptureSaveData()))!;
        if (lanternExpedition.equippedSkills[1] != 7 || returnedEcho.Hp >= 1000 || lanternExpedition.health <= 45 || lanternExpedition.returnVoiceCd <= 0 || !ValidSaveData(returnedVoicesSave)) throw new InvalidOperationException("O epílogo de memórias devolvidas não liberou a técnica de cura em área, nem a salvou.");
        using var emberEnding = new Game(); emberEnding.timer.Stop(); emberEnding.StartGame(); emberEnding.storyOverlay = false; emberEnding.townEnding = 1; emberEnding.finalEnding = 2; emberEnding.questStage = 53; emberEnding.chorusMemory = emberEnding.firstMemory = emberEnding.westTrace = emberEnding.upstreamTrace = emberEnding.beyondTrace = emberEnding.ebbSeal = emberEnding.counterTrace = emberEnding.anchorWard = emberEnding.refrainSong = emberEnding.riftVerse = emberEnding.deepTrace = emberEnding.lanternSignal = emberEnding.beaconLight = emberEnding.routeMemory = emberEnding.silenceMemory = true; emberEnding.room = 38; emberEnding.SetupRoom(); emberEnding.enemies.Clear(); emberEnding.drops.Clear(); emberEnding.EquipSkill(2, 8); Enemy sparkTarget = new() { Pos = new PointF(520, 330), Kind = EnemyKind.DeepSilence, Hp = 1000, MaxHp = 1000 }; emberEnding.enemies.Add(sparkTarget); emberEnding.CastEquippedSkill(2);
        SaveData finalSparkSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(emberEnding.CaptureSaveData()))!;
        if (emberEnding.equippedSkills[2] != 8 || sparkTarget.Hp >= 1000 || emberEnding.finalStrikeCd <= 0 || !ValidSaveData(finalSparkSave)) throw new InvalidOperationException("O epílogo do Lume não liberou o golpe concentrado nem validou seu save.");
        emberEnding.room = 18; emberEnding.vigilBestFloor = 0; emberEnding.SetupRoom(); emberEnding.memoryOverlay = false; emberEnding.player = VigilAltarPosition; emberEnding.Interact(); emberEnding.memoryOverlay = false;
        Enemy firstVigilBoss = emberEnding.enemies.Single(); float firstVigilHp = firstVigilBoss.MaxHp;
        if (!emberEnding.vigilActive || emberEnding.vigilFloor != 1 || firstVigilBoss.Kind != EnemyKind.DeepSilence || firstVigilHp != 3450) throw new InvalidOperationException("O altar não abriu o primeiro andar da Vigília Ascendente após o epílogo.");
        Enemy hardenedDummy = new() { Pos = new PointF(500, 300), Kind = EnemyKind.DeepSilence, Hp = 1000, MaxHp = 1000 }; emberEnding.DamageEnemy(hardenedDummy, 100);
        if (VigilAspectForFloor(1) != VigilAspect.Hardened || hardenedDummy.Hp != 925 || VigilAspectTitle(1) != "CASCA VIVA") throw new InvalidOperationException("A Casca Viva não anunciou nem reduziu em 25% o dano recebido pelo chefe.");
        firstVigilBoss.Hp = 1; emberEnding.Defeat(firstVigilBoss);
        if (!emberEnding.drops.Any(d => d.Kind == "glass") || !emberEnding.drops.Any(d => d.Kind == "weapon")) throw new InvalidOperationException("O chefe da Vigília não deixou sal vítreo e arma.");
        foreach (Drop reward in emberEnding.drops.ToArray()) emberEnding.Collect(reward);
        emberEnding.memoryOverlay = false; emberEnding.player = VigilArenaAltarPosition; emberEnding.Interact(); int vigilGoldBefore = emberEnding.gold, vigilGlassBefore = emberEnding.tideGlass;
        if (!emberEnding.vigilChoiceOpen) throw new InvalidOperationException("O altar da arena não abriu a escolha após limpar o andar.");
        emberEnding.OnKeyDown(emberEnding, new KeyEventArgs(Keys.D1));
        if (emberEnding.vigilFloor != 2 || !emberEnding.vigilActive || emberEnding.enemies.Single().Kind != EnemyKind.RouteScribe || emberEnding.enemies[0].MaxHp <= firstVigilHp || emberEnding.gold <= vigilGoldBefore || emberEnding.tideGlass <= vigilGlassBefore) throw new InvalidOperationException("O segundo andar não elevou a dificuldade, alternou o chefe e concedeu a recompensa do primeiro.");
        emberEnding.health = emberEnding.maxHealth; float hungryHealth = emberEnding.health; emberEnding.HitPlayer(10);
        if (VigilAspectForFloor(2) != VigilAspect.Hungry || Math.Abs(hungryHealth - emberEnding.health - 14.04f) > 0.02f || VigilAspectTitle(2) != "FOME DO RIO") throw new InvalidOperationException("A Fome do Rio não aplicou a pressão de ataque anunciada no segundo andar.");
        emberEnding.health = emberEnding.maxHealth;
        Enemy secondVigilBoss = emberEnding.enemies.Single(); secondVigilBoss.Hp = 1; emberEnding.Defeat(secondVigilBoss);
        foreach (Drop reward in emberEnding.drops.ToArray()) emberEnding.Collect(reward);
        emberEnding.memoryOverlay = false; emberEnding.player = VigilArenaAltarPosition; emberEnding.Interact(); emberEnding.OnKeyDown(emberEnding, new KeyEventArgs(Keys.D2));
        SaveData vigilSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(emberEnding.CaptureSaveData()))!;
        if (emberEnding.room != 18 || emberEnding.vigilActive || emberEnding.vigilBestFloor != 2 || emberEnding.health != emberEnding.maxHealth || !ValidSaveData(vigilSave) || vigilSave.VigilBestFloor != 2 || JsonSerializer.Deserialize<SaveData>("{}")?.VigilBestFloor != 0) throw new InvalidOperationException("A Vigília não encerrou no Refúgio com vida restaurada, melhor andar persistente e compatibilidade com saves antigos.");
        emberEnding.memoryOverlay = false; emberEnding.player = VigilAltarPosition; emberEnding.Interact(); emberEnding.memoryOverlay = false;
        if (!emberEnding.vigilActive || emberEnding.vigilFloor != 3) throw new InvalidOperationException("Uma nova tentativa da Vigília não começou após o último andar concluído.");
        Enemy swiftBoss = emberEnding.enemies.Single(); swiftBoss.Cooldown = 0; emberEnding.UpdateEnemy(swiftBoss, 0.01f);
        if (VigilAspectForFloor(3) != VigilAspect.Swift || Math.Abs(swiftBoss.WindupMax - 0.624f) > 0.002f || VigilAspectTitle(3) != "SUSSURRO VELOZ") throw new InvalidOperationException("O Sussurro Veloz não encurtou o aviso de ataque do terceiro andar.");
        emberEnding.memoryOverlay = false; emberEnding.vigilActive = true; emberEnding.vigilFloor = 4; emberEnding.room = 38; emberEnding.SetupRoom(); emberEnding.vigilMinionCooldown = 0.01f;
        if (VigilAspectForFloor(4) != VigilAspect.Chorus || VigilAspectTitle(4) != "CORO ERRANTE" || !VigilAspectRule(4).Contains("20 segundos")) throw new InvalidOperationException("O Coro Errante não anunciou a regra do quarto andar.");
        emberEnding.TickVigilMinions(0.02f); Enemy errantEcho = emberEnding.enemies.Single(enemy => enemy.VigilMinion);
        if (errantEcho.Kind != EnemyKind.SaltEcho || EnemyName(errantEcho) != "ECO ERRANTE • SALINO" || errantEcho.Hp is < 80 or > 220) throw new InvalidOperationException("O Coro Errante não criou um eco salino identificável e escalado.");
        emberEnding.TickVigilMinions(40); if (emberEnding.enemies.Count(enemy => enemy.VigilMinion) != 1) throw new InvalidOperationException("O Coro Errante acumulou lacaios sem limite durante um mesmo ciclo.");
        emberEnding.drops.Clear(); int minionXp = emberEnding.level * (emberEnding.level - 1) / 2 * 60 + emberEnding.xp; emberEnding.Defeat(errantEcho);
        int minionXpAfter = emberEnding.level * (emberEnding.level - 1) / 2 * 60 + emberEnding.xp;
        if (minionXpAfter != minionXp + 12 || emberEnding.drops.Single(drop => drop.Kind == "gold").Amount != 16 || emberEnding.drops.Any(drop => drop.Kind is "weapon" or "charm" or "glass")) throw new InvalidOperationException("O Eco Errante não concedeu recompensa menor sem duplicar saques raros.");
        emberEnding.drops.Clear(); emberEnding.TickVigilMinions(20); if (emberEnding.enemies.Count(enemy => enemy.VigilMinion) != 1) throw new InvalidOperationException("O Coro Errante não retomou a invocação após o eco ser derrotado.");
        emberEnding.health = 1; emberEnding.HitPlayer(2); SaveData vigilFallSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(emberEnding.CaptureSaveData()))!;
        if (emberEnding.room != 18 || emberEnding.vigilActive || emberEnding.health != emberEnding.maxHealth * 0.5f || emberEnding.vigilBestFloor != 2 || !ValidSaveData(vigilFallSave)) throw new InvalidOperationException("A derrota na Vigília não retornou ao Refúgio com vida parcial preservando a melhor marca.");
        emberEnding.memoryOverlay = false; emberEnding.vigilActive = true; emberEnding.vigilFloor = 5; emberEnding.room = 38; emberEnding.SetupRoom(); Enemy milestoneBoss = emberEnding.enemies.Single(); milestoneBoss.Hp = 1; emberEnding.Defeat(milestoneBoss);
        Drop returnThread = emberEnding.drops.Single(d => d.Kind == "charm" && d.Amount == 7); emberEnding.Collect(returnThread);
        if ((emberEnding.ownedCharms & 64) == 0 || emberEnding.equippedCharm != 7 || emberEnding.charmNames[7] != "FIO DO RETORNO") throw new InvalidOperationException("O quinto andar não concedeu nem equipou o Fio do Retorno.");
        Enemy aspectDummy = new() { Pos = new PointF(500, 300), Kind = EnemyKind.DeepSilence, Hp = 1000, MaxHp = 1000 }; float threadDamage = emberEnding.OutgoingDamageMultiplier(aspectDummy); emberEnding.health = emberEnding.maxHealth; float threadHealth = emberEnding.health; emberEnding.HitPlayer(10);
        if (threadDamage != 1.4f || Math.Abs(threadHealth - emberEnding.health - 14.52f) > 0.03f) throw new InvalidOperationException("O Fio do Retorno não aplicou o bônus de dano e o risco de dano recebido na Vigília.");
        emberEnding.enemies.Clear(); emberEnding.drops.Clear(); emberEnding.vigilFloor = 6; emberEnding.health = 50; Enemy returnBoss = new() { Pos = new PointF(500, 300), Kind = EnemyKind.LanternRegent, Hp = 1, MaxHp = 1000 }; emberEnding.enemies.Add(returnBoss); emberEnding.Defeat(returnBoss);
        if (emberEnding.health != 62) throw new InvalidOperationException("O Fio do Retorno não recuperou 12% da vida ao abater um chefe da Vigília.");
        emberEnding.vigilActive = false; emberEnding.vigilFloor = 0; emberEnding.room = 18; emberEnding.SetupRoom(); emberEnding.drops.Clear(); emberEnding.health = emberEnding.maxHealth; emberEnding.vigilBestFloor = 5;
        SaveData returnThreadSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(emberEnding.CaptureSaveData()))!;
        if (!ValidSaveData(returnThreadSave) || returnThreadSave.OwnedCharms != 64 || returnThreadSave.EquippedCharm != 7 || returnThreadSave.VigilBestFloor != 5) throw new InvalidOperationException("O Fio do Retorno e a melhor marca do quinto andar não foram aceitos pelo save.");
        game.room = 11; game.SetupRoom();
        if (game.enemies.Count != 0 || game.drops.Count != 0) throw new InvalidOperationException("O Reservatório foi recriado após ter sido limpo.");
        game.room = 3; game.SetupRoom();
        game.memoryOverlay = false; game.player = CryptGatePosition; game.Interact();
        if (game.room != 2 || game.enemies.Count != 4) throw new InvalidOperationException("A passagem da cidade não retornou à cripta.");
        using var borrowedChapter = new Game(); borrowedChapter.timer.Stop(); borrowedChapter.StartGame(); borrowedChapter.storyOverlay = false; borrowedChapter.finalEnding = 1; borrowedChapter.townEnding = 1; borrowedChapter.chorusMemory = borrowedChapter.firstMemory = borrowedChapter.westTrace = borrowedChapter.upstreamTrace = borrowedChapter.beyondTrace = borrowedChapter.ebbSeal = borrowedChapter.counterTrace = borrowedChapter.anchorWard = borrowedChapter.refrainSong = borrowedChapter.riftVerse = borrowedChapter.deepTrace = borrowedChapter.lanternSignal = borrowedChapter.beaconLight = borrowedChapter.routeMemory = borrowedChapter.silenceMemory = true; borrowedChapter.questStage = 52; borrowedChapter.room = 18; borrowedChapter.SetupRoom(); borrowedChapter.memoryOverlay = false; borrowedChapter.player = AvenPosition; borrowedChapter.Interact();
        if (borrowedChapter.questStage != 54) throw new InvalidOperationException("Aven não abriu a expedição opcional XVII após o epílogo.");
        borrowedChapter.memoryOverlay = false; borrowedChapter.player = new PointF(850, 330); borrowedChapter.InteractRefuge();
        if (borrowedChapter.room != 39 || borrowedChapter.enemies.Count != 5) throw new InvalidOperationException("O Cais dos Ecos Emprestados não iniciou com a horda esperada.");
        borrowedChapter.enemies.Clear(); borrowedChapter.drops.Clear(); borrowedChapter.player = new PointF(850, 330); borrowedChapter.Interact();
        if (borrowedChapter.room != 40 || borrowedChapter.enemies.Single().Kind != EnemyKind.BorrowedChorus) throw new InvalidOperationException("O Arquivo da Maré sem Nome não abriu seu chefe original.");
        Enemy borrowedBoss = borrowedChapter.enemies.Single(); borrowedBoss.Cooldown = 0; borrowedChapter.player = new PointF(150, 330); borrowedChapter.UpdateEnemy(borrowedBoss, 0.01f);
        if (borrowedBoss.BossPattern != 1 || borrowedBoss.Windup <= 0) throw new InvalidOperationException("O Arquivista das Marés não anunciou o primeiro golpe.");
        borrowedBoss.Windup = 0; borrowedBoss.Cooldown = 0; borrowedChapter.UpdateEnemy(borrowedBoss, 0.01f);
        if (borrowedBoss.BossPattern != 2 || !BossUsesLine(borrowedBoss)) throw new InvalidOperationException("O Arquivista das Marés não anunciou o corte em linha.");
        borrowedBoss.Windup = 0; borrowedBoss.Cooldown = 0; borrowedChapter.UpdateEnemy(borrowedBoss, 0.01f);
        if (borrowedBoss.BossPattern != 0) throw new InvalidOperationException("O Arquivista das Marés não concluiu seu ciclo de três ataques.");
        borrowedBoss.Hp = 1; borrowedChapter.Defeat(borrowedBoss); Drop borrowedDrop = borrowedChapter.drops.Single(d => d.Kind == "borrowed"); borrowedChapter.Collect(borrowedDrop);
        SaveData borrowedSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(borrowedChapter.CaptureSaveData()))!;
        if (!borrowedChapter.borrowedTrace || borrowedChapter.questStage != 55 || !ValidSaveData(borrowedSave) || JsonSerializer.Deserialize<SaveData>("{}")?.BorrowedTrace != false) throw new InvalidOperationException("A memória da expedição não persistiu nem preservou saves antigos.");
        borrowedChapter.memoryOverlay = false; borrowedChapter.room = 18; borrowedChapter.SetupRoom(); borrowedChapter.player = AvenPosition; int borrowedGold = borrowedChapter.gold, borrowedTalent = borrowedChapter.techniqueTalent; borrowedChapter.Interact();
        if (borrowedChapter.questStage != 56 || borrowedChapter.gold != borrowedGold + 1900 || borrowedChapter.techniqueTalent != borrowedTalent + 1 || !ValidSaveData(borrowedChapter.CaptureSaveData())) throw new InvalidOperationException("Aven não registrou a memória nem concedeu a recompensa permanente da expedição.");
        borrowedChapter.memoryOverlay = false; borrowedChapter.Interact();
        if (!borrowedChapter.forgeMenuOpen) throw new InvalidOperationException("Aven não reabriu a têmpera depois de concluída a expedição.");
        borrowedChapter.level = 10; borrowedChapter.healthTalent = 2; borrowedChapter.skillTalent = 1; borrowedChapter.dodgeTalent = 1; borrowedChapter.techniqueTalent = 2;
        borrowedChapter.maxHealth = 180; borrowedChapter.health = 90; borrowedChapter.gold = 5000; borrowedChapter.tideGlass = 10; borrowedChapter.pendingTalents = 0; borrowedChapter.talentChoice = false;
        borrowedChapter.OnKeyDown(borrowedChapter, new KeyEventArgs(Keys.D7));
        if (borrowedChapter.forgeMenuOpen || borrowedChapter.gold != 3750 || borrowedChapter.tideGlass != 3 || borrowedChapter.healthTalent + borrowedChapter.skillTalent + borrowedChapter.dodgeTalent + borrowedChapter.techniqueTalent != 0 || borrowedChapter.maxHealth != 140 || borrowedChapter.health != 70)
            throw new InvalidOperationException("A redistribuição não devolveu os talentos, preservou os bônus permanentes de vida ou cobrou os recursos corretos.");
        if (!ValidSaveData(borrowedChapter.CaptureSaveData())) throw new InvalidOperationException("A redistribuição produziu um estado de save inválido.");
        using var eliteCircuit = new Game(); eliteCircuit.timer.Stop(); eliteCircuit.StartGame(); eliteCircuit.storyOverlay = false;
        foreach (int eliteRoom in new[] { 23, 25, 27, 31, 33, 35, 37, 39 })
        {
            eliteCircuit.room = eliteRoom; eliteCircuit.SetupRoom(); Enemy[] roomElites = eliteCircuit.enemies.Where(enemy => enemy.Kind == EnemyKind.Elite).ToArray();
            if (roomElites.Length != 1 || roomElites[0].MaxHp != 120 + (eliteRoom - 1) * 6 || !Enum.IsDefined(roomElites[0].EliteAffix)) throw new InvalidOperationException($"A rota avançada {eliteRoom} não recebeu um elite com afixo e vida escalada.");
        }
        Console.WriteLine("TESTES OK: campanha até o Capítulo XVII, expedição opcional, afixos de elite, Arquivista das Marés, epílogos, Vigília Ascendente, chefes, combate, loot, progressão e compatibilidade de saves antigos."); return 0;
    }
}
