using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;

public sealed partial class Game : Form
{
    private enum TitlePage { Main, Campaign, History, Settings }
    private enum HeroClass { Listener, StoneGuardian, NameWeaver }
    private const float PlayerMoveSpeed = 190f;
    private const float ConductorPulseRadius = 165f;
    private const float InteractionRadius = 72;
    private const float MemoryStoneInteractionRadius = 58;
    private enum EnemyKind { Grunt, Archer, Mage, Elite, Guardian, InkEcho, BlindScribe, DrownedMemory, RiverFerryman, EmberWisp, AshWarden, NameMoth, HollowName, NameWeaver, FirstKeeper, MudCantor, OathCartographer, PaperWraith, NameBroker, SaltEcho, TideWitness, ReedKeeper, FlowKeeper, CounterCurrent, RiftCantor, GlassHerald, LanternRegent, RouteScribe, DeepSilence, BorrowedChorus, LastTuner, CooldownLeech, SlagWeaver }
    private enum VigilAspect { Hardened, Hungry, Swift, Chorus, Counterflow }
    private enum EliteAffix { Armored, Swift, Bloodbound, Spearbound, Summoner, Conductor }
    private sealed class Enemy
    {
        public PointF Pos, Target, Target2, Target2Start;
        public EnemyKind Kind;
        public float Hp, MaxHp, Cooldown, Windup, WindupMax, Flash, AnimationTime, AttackPoseTime, SummonCooldown, SummonWindup, SupportCooldown, SupportWindup, BeaconMarkTime, BellMarkTime, NameWeaverMarkTime, WoundTime, WoundTickCooldown, WoundDamage;
        public int BossPattern;
        public bool Armored, Moving;
        public bool VigilMinion;
        public EliteAffix? EliteAffix2;
        public bool EliteMinion;
        public EliteAffix EliteAffix;
        public float Stagger;
        public bool Boss => Kind is EnemyKind.Guardian or EnemyKind.BlindScribe or EnemyKind.RiverFerryman or EnemyKind.AshWarden or EnemyKind.HollowName or EnemyKind.NameWeaver or EnemyKind.FirstKeeper or EnemyKind.OathCartographer or EnemyKind.NameBroker or EnemyKind.TideWitness or EnemyKind.ReedKeeper or EnemyKind.FlowKeeper or EnemyKind.CounterCurrent or EnemyKind.RiftCantor or EnemyKind.GlassHerald or EnemyKind.LanternRegent or EnemyKind.RouteScribe or EnemyKind.DeepSilence or EnemyKind.BorrowedChorus or EnemyKind.LastTuner;
        public float Radius => Boss ? 23 : Kind == EnemyKind.Elite ? 18 : 14;
    }
    private sealed class Drop { public PointF Pos; public string Kind = "gold"; public int Amount, Rarity, Bonus, Affix, Affix2; }
    private sealed class GroundHazard { public PointF Pos; public float Radius = 48, TimeLeft = 3.2f, TickCooldown = 0.8f; public int Damage = 9; }
    private sealed class EchoShard { public PointF Pos, Direction; public float Life, Damage; public HashSet<Enemy> Hit = new(); }
    private sealed class FloatingText { public PointF Pos; public string Text = ""; public Color Color; public float TimeLeft, Duration; }
    private readonly record struct ReadingOverlayLayout(float PanelY, float PanelHeight, float HeadingY, float BodyY, float BodyHeight, float HintY);
    private sealed record CampaignChapter(int Number, string Title, int FirstRoom, int LastRoom);

    private static readonly RectangleF[] Walls = { new(345, 235, 48, 90), new(565, 375, 58, 80) };
    private static readonly Lazy<Bitmap?> TitleBackdrop = new(() =>
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "TitleBackdrop.png");
        return File.Exists(path) ? new Bitmap(path) : null;
    });
    private static readonly Lazy<Bitmap?> PlayerSpriteSheet = new(() =>
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "Listener.png");
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
    private static readonly RectangleF[] UnsentChamberWalls = { new(230, 154, 54, 128), new(650, 408, 54, 128), new(372, 206, 48, 76), new(540, 408, 48, 76) };
    private static readonly RectangleF[] OffbeatWellWalls = { new(238, 154, 54, 130), new(668, 408, 54, 130), new(392, 160, 40, 84), new(526, 408, 40, 84) };
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
    private readonly List<GroundHazard> groundHazards = new();
    private readonly HashSet<Keys> down = new();
    private bool focusPaused;
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 16 };
    private readonly Random random = new();
    private readonly List<EchoShard> echoShards = new();
    private readonly List<FloatingText> floatingTexts = new();
    private PointF beaconPos;
    private readonly string[] roomNames = { "VESTÍBULO EM RUÍNAS", "SALÃO DOS OSSOS", "CÂMARA DO GUARDIÃO", "VÉSPERA • PRAÇA DO FAROL", "ARQUIVO NORTE • GALERIA", "ARQUIVO NORTE • COFRE", "RIO CEGO • LEITO SECO", "RIO CEGO • CAPELA AFOGADA", "FAROL • ESCADA DE CINZAS", "FAROL • CÂMARA DO LUME", "ALÉM DO LUME • MARGEM SEM REFLEXO", "RESERVATÓRIO • VOZES RETIDAS", "COSTA DAS VOZES TARDIAS", "CLAUSTRO DO CORO PARTIDO", "BACIA DO PRIMEIRO NOME", "SALA DA PRIMEIRA VIGÍLIA", "VAU DOS NOMES ESCOLHIDOS", "PÁTIO DAS TESTEMUNHAS", "REFÚGIO DO VEIO CALMO", "TRILHA DOS LUMINÁRIOS", "CISTERNA DO REGISTRO VAZIO", "VÁRZEA DAS VOZES SOPRADAS", "PONTE DA MARÉ INVERTIDA", "COTOVELO DOS JUNCAIS", "PÁTIO DAS AMARRAS LIVRES", "MOINHOS DA VAZANTE", "CÂMARA DO VEIO PRESO", "ESCARPA DO CONTRAFLUXO", "CORAÇÃO DAS DUAS MARGENS", "CANTARIA DAS VOZES LIVRES", "CÂMARA DA CONTRAVOZ", "GARGANTA DO VIDRO MUDO", "FONTE SOB A PEDRA", "PASSAGEM DAS LANTERNAS BAIXAS", "OBSERVATÓRIO DA MARÉ INTERIOR", "TRILHA DAS LINHAS ABERTAS", "GALERIA DA ROTA NÃO ESCRITA", "SOLEIRA DO SILÊNCIO", "CÂMARA DO ÚLTIMO ECO", "CAIS DOS ECOS EMPRESTADOS", "ARQUIVO DA MARÉ SEM NOME", "CÂMARA DAS BADALADAS NÃO ENVIADAS", "POÇO DO CONTRATEMPO" };
    private readonly CampaignChapter[] campaignChapters = {
        new(1, "A cripta que ainda escuta", 0, 2), new(2, "O arquivo das vidas ausentes", 3, 5),
        new(3, "A corrente sob o farol", 6, 11), new(4, "A costa das vozes tardias", 12, 13),
        new(5, "A Bacia do Primeiro Nome", 14, 15), new(6, "O refúgio da margem escolhida", 16, 17),
        new(7, "O refúgio do Veio Calmo", 18, 20), new(8, "A nascente que não registra nomes", 21, 22),
        new(9, "As amarras que podem ser desfeitas", 23, 24), new(10, "A vazante não pertence a ninguém", 25, 26),
        new(11, "O compasso das duas margens", 27, 28), new(12, "A nota que não manda", 29, 30),
        new(13, "Os passos que o vidro não guarda", 31, 32), new(14, "A luz que espera", 33, 34),
        new(15, "As linhas que não apontam", 35, 36), new(16, "O silêncio que escolhe", 37, 38),
        new(17, "As marés emprestadas • opcional", 39, 40), new(18, "A badalada não enviada • opcional", 41, 42)
    };
    private readonly string[] weaponNames = { "ESPADA DE PEDRA", "LÂMINA DO ARQUIVO", "FACA DO CREPÚSCULO", "LANÇA DA BADALADA" };
    private readonly string[] charmNames = { "", "NÓ DE BRASA", "FIO DE TRAVESSIA", "VIDRO DE VIGÍLIA", "MARCA DA VIGÍLIA", "FIO DA MARGEM", "NÓ DE LUME", "FIO DO RETORNO", "ECO CONVERGENTE", "COMPASSO FENDIDO", "ÂNCORA COMUM", "PULSO BIFURCADO", "NÓ DO CONTRATEMPO" };
    private readonly string[] skillNames = { "Vazio", "Estilhaço de Voz", "Ressonância do Sino", "Vazante", "Manto de Lastro", "Coro de Retorno", "Farol de Vigília", "Devolução das Vozes", "Última Centelha", "Bastião de Basalto", "Sutura do Nome" };
    private readonly string[] skillDescriptions = { "", "Projétil perfurante de longo alcance", "Onda que fere e interrompe ecos", "Agrupa ecos próximos", "Reduz o dano recebido por 2,5 s", "Onda que fere e interrompe ecos", "Planta um farol que pulsa contra inimigos próximos", "Onda ampla que fere e restaura vida por inimigo atingido", "Golpe concentrado que causa dano elevado a um alvo", "Gasta 50 de Fúria: reduz dano em 55% por 3,5 s", "Gasta 40 de Fio: marca ecos próximos por 5 s" };
    private readonly string[] prologue = {
        "Na noite sem sinos, Véspera começou a esquecer os nomes de seus mortos. Sob as ruas, o Rio Cego repetiu uma voz que ninguém mais conseguia ouvir.",
        "Você é um Escutador: ouve as lembranças que a pedra não conseguiu guardar em silêncio. Entre elas reconheceu Nara, sua irmã, desaparecida quando os sinos pararam.",
        "A memória dela chega partida: “Desça. Ele ainda lembra.” O Guardião sem Nome espera na cripta, diante da única pista que pode explicar por que Nara foi até lá."
    };
    private readonly int[] weaponDamage = { 0, 10, 4, 6 };
    private readonly int[] weaponBonuses = new int[4], weaponRarities = new int[4], weaponAffixes = new int[4], weaponAffixes2 = new int[4];
    private PointF player = new(150, 330), facing = new(1, 0), mouseAim = new(1, 0), dashDirection = new(1, 0);
    private float playerAnimationTime;
    private int room, level = 1, xp, gold, potions = 2, damage = 25, weapon, ownedWeapons = 1, difficultyTier;
    private HeroClass heroClass;
    private float stoneFury, nameThread;
    private int armorTrait, relicTrait;
    private int healthTalent, skillTalent, dodgeTalent, techniqueTalent, combatDiscipline, combatMastery, combatFocus, combatApex, pendingTalents, enemiesDefeated;
    private int memoriesFound, questStage, tideGlass, finalEnding, eliteWeaponMisses;
    private int vigilBestFloor, vigilFloor;
    private VigilAspect[] vigilAspectOrder = { VigilAspect.Hardened, VigilAspect.Hungry, VigilAspect.Swift, VigilAspect.Chorus, VigilAspect.Counterflow };
    private EnemyKind[] vigilBossOrder = { EnemyKind.DeepSilence, EnemyKind.RouteScribe, EnemyKind.LanternRegent, EnemyKind.GlassHerald, EnemyKind.RiftCantor };
    private float vigilMinionCooldown, vigilTideTime;
    private bool vigilActive, vigilChoiceOpen;
    private bool highContrastTelegraphs;
    private bool silenceMemory;
    private bool borrowedTrace, unstruckTrace;
    private long clearedRooms;
    private bool chorusMemory, firstMemory, westTrace, upstreamTrace, forgeMenuOpen, defenseForgeOpen, secondaryForgeOpen;
    private bool beyondTrace, ebbSeal, counterTrace, anchorWard, refrainSong, riftVerse, deepTrace, lanternSignal, beaconLight, routeMemory;
    private int ownedCharms, equippedCharm, equippedCharm2;
    private int[] equippedSkills = { 1, 0, 0, 0 };
    private int[] alternateSkills = new int[4];
    private int[] skillVariants = new int[9];
    private bool archiveSeal;
    private bool riverBell;
    private bool lumeLens;
    private bool naraTrace;
    private float health = 100, maxHealth = 100, attackCd, attackFlash, skillCd, shardCd, bellCd, bellWaveTime, bellWaveRadius, pullCd, pullWaveTime, wardCd, wardTime, refrainCd, refrainWaveTime, refrainWaveRadius, beaconCd, beaconTime, beaconPulseCd, beaconWaveTime, returnVoiceCd, returnVoiceWaveTime, returnVoiceWaveRadius, finalStrikeCd, finalStrikeFlash, stoneBastionCd, stoneBastionTime, nameWeaverCd, dashCd, dashTime, invulnerable, noticeTime, windupPulse, veinResonanceTime;
    private PointF finalStrikeTarget;
    private bool armor, armorOn, relic, relicOn, inventoryOpen, skillMenuOpen, characterSheetOpen, mapOpen, journalOpen, title = true, dead, victory, talentChoice, chestChoice, storyOverlay, memoryOverlay, endingChoiceOpen;
    private int skillMenuSlot;
    private bool alternateSkillsActive, skillMenuAlternate;
    private int townEnding;
    private int storyPage;
    private TitlePage titlePage;
    private int titleSelection;
    private int titleChapterSelection;
    private bool confirmNewJourney;
    private bool classSelectionOpen;
    private int classSelection;
    private SaveData? titleSavePreview;
    private bool titleSavePreviewRead;
    private bool titleSaveDetected;
    private bool titleSaveDetectedRead;
    private string notice = "";
    private string memoryTitle = "";
    private string memoryText = "";

    public Game()
    {
        Text = "Scadryk — RPG de ação 2D"; ClientSize = new Size(960, 640); MinimumSize = new Size(816, 579);
        FormBorderStyle = FormBorderStyle.Sizable; MaximizeBox = true; StartPosition = FormStartPosition.CenterScreen;
        DoubleBuffered = true; KeyPreview = true; BackColor = Color.FromArgb(16, 19, 23);
        timer.Tick += (_, _) => TickGame(); timer.Start();
        KeyDown += OnKeyDown; KeyUp += (_, e) => down.Remove(e.KeyCode);
        Deactivate += (_, _) => PauseForLostFocus();
        Activated += (_, _) => ResumeAfterFocus();
        MouseMove += (_, e) => { PointF point = ScreenToGame(new PointF(e.X, e.Y), ClientSize); mouseAim = Normalize(new PointF(point.X - player.X, point.Y - player.Y), facing); if (title && titlePage == TitlePage.Main && point.X >= 85 && point.X <= 470) { int hover = (int)((point.Y - 246) / 55); if (hover >= 0 && hover < 6 && hover != titleSelection) { titleSelection = hover; Invalidate(); } } };
        MouseDown += (_, e) =>
        {
            if (title)
            {
                if (e.Button == MouseButtons.Left) HandleTitleClick(ScreenToGame(new PointF(e.X, e.Y), ClientSize));
                return;
            }
            if (storyOverlay) { if (e.Button == MouseButtons.Left) AdvanceStory(); return; }
            if (memoryOverlay) { if (e.Button == MouseButtons.Left) memoryOverlay = false; return; }
            if (journalOpen) { if (e.Button == MouseButtons.Left) journalOpen = false; return; }
            if (GameplayPaused()) return;
            PointF aim = ScreenToGame(new PointF(e.X, e.Y), ClientSize); mouseAim = Normalize(new PointF(aim.X - player.X, aim.Y - player.Y), facing);
            if (e.Button == MouseButtons.Left) Attack();
            if (e.Button == MouseButtons.Right) Whirlwind();
        };
    }

    private bool HasTitleSave
    {
        get
        {
            if (!titleSaveDetectedRead)
            {
                titleSaveDetected = File.Exists(SavePath) || File.Exists(LegacySavePath);
                titleSaveDetectedRead = true;
            }
            return titleSaveDetected;
        }
    }

    private void StartGame()
    {
        room = 0; level = 1; xp = gold = damage = 0; damage = 25; potions = 2; weapon = 0; ownedWeapons = 1; Array.Clear(weaponBonuses); Array.Clear(weaponRarities); Array.Clear(weaponAffixes); Array.Clear(weaponAffixes2);
        armor = armorOn = relic = relicOn = archiveSeal = riverBell = lumeLens = naraTrace = chorusMemory = firstMemory = westTrace = upstreamTrace = beyondTrace = ebbSeal = counterTrace = anchorWard = refrainSong = riftVerse = deepTrace = lanternSignal = beaconLight = routeMemory = silenceMemory = inventoryOpen = skillMenuOpen = characterSheetOpen = mapOpen = journalOpen = dead = victory = chestChoice = talentChoice = memoryOverlay = endingChoiceOpen = forgeMenuOpen = defenseForgeOpen = secondaryForgeOpen = vigilActive = vigilChoiceOpen = classSelectionOpen = false;
        armorTrait = relicTrait = 0;
        borrowedTrace = unstruckTrace = false;
        finalEnding = 0;
        vigilBestFloor = vigilFloor = 0;
        townEnding = 0;
        healthTalent = skillTalent = dodgeTalent = techniqueTalent = combatDiscipline = combatMastery = combatFocus = combatApex = pendingTalents = enemiesDefeated = memoriesFound = questStage = tideGlass = eliteWeaponMisses = 0;
        clearedRooms = 0;
        ownedCharms = equippedCharm = equippedCharm2 = 0;
        equippedSkills = new[] { 1, 0, 0, 0 }; alternateSkills = new int[4]; alternateSkillsActive = skillMenuAlternate = false; Array.Clear(skillVariants); skillMenuOpen = false; skillMenuSlot = 0;
        health = maxHealth = 100; attackCd = attackFlash = skillCd = shardCd = bellCd = bellWaveTime = bellWaveRadius = pullCd = pullWaveTime = wardCd = wardTime = refrainCd = refrainWaveTime = refrainWaveRadius = beaconCd = beaconTime = beaconPulseCd = beaconWaveTime = returnVoiceCd = returnVoiceWaveTime = returnVoiceWaveRadius = finalStrikeCd = finalStrikeFlash = stoneBastionCd = stoneBastionTime = nameWeaverCd = dashCd = dashTime = invulnerable = veinResonanceTime = stoneFury = nameThread = 0; vigilMinionCooldown = vigilTideTime = 0; beaconPos = finalStrikeTarget = PointF.Empty; facing = mouseAim = dashDirection = new PointF(1, 0); playerAnimationTime = 0; echoShards.Clear(); floatingTexts.Clear(); title = false; storyPage = 0; storyOverlay = true; SetupRoom();
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

    private EnemyKind RunVigilBoss(int floor) => vigilBossOrder[(Math.Max(1, floor) - 1) % vigilBossOrder.Length];
    private VigilAspect RunVigilAspect(int floor) => vigilAspectOrder[(Math.Max(1, floor) - 1) % vigilAspectOrder.Length];
    private void ShuffleVigilOrder()
    {
        vigilAspectOrder = new[] { VigilAspect.Hardened, VigilAspect.Hungry, VigilAspect.Swift, VigilAspect.Chorus, VigilAspect.Counterflow };
        vigilBossOrder = new[] { EnemyKind.DeepSilence, EnemyKind.RouteScribe, EnemyKind.LanternRegent, EnemyKind.GlassHerald, EnemyKind.RiftCantor };
        for (int i = vigilAspectOrder.Length - 1; i > 0; i--) { int j = random.Next(i + 1); (vigilAspectOrder[i], vigilAspectOrder[j]) = (vigilAspectOrder[j], vigilAspectOrder[i]); }
        for (int i = vigilBossOrder.Length - 1; i > 0; i--) { int j = random.Next(i + 1); (vigilBossOrder[i], vigilBossOrder[j]) = (vigilBossOrder[j], vigilBossOrder[i]); }
    }

    private static VigilAspect VigilAspectForFloor(int floor) => ((Math.Max(1, floor) - 1) % 5) switch
    {
        0 => VigilAspect.Hardened,
        1 => VigilAspect.Hungry,
        2 => VigilAspect.Swift,
        3 => VigilAspect.Chorus,
        _ => VigilAspect.Counterflow
    };

    private static string VigilAspectTitle(int floor) => VigilAspectForFloor(floor) switch
    {
        VigilAspect.Hardened => "CASCA VIVA",
        VigilAspect.Hungry => "FOME DO RIO",
        VigilAspect.Swift => "SUSSURRO VELOZ",
        VigilAspect.Chorus => "CORO ERRANTE",
        _ => "MARÉ INVERTIDA"
    };

    private static string VigilAspectRule(int floor) => VigilAspectForFloor(floor) switch
    {
        VigilAspect.Hardened => "O chefe recebe 25% menos dano",
        VigilAspect.Hungry => "Os ataques do chefe causam 30% mais dano",
        VigilAspect.Swift => "Os ataques começam 20% mais rápido",
        VigilAspect.Chorus => "Um Eco Salino surge a cada 20 segundos",
        _ => "A corrente puxa e repele a cada 6 segundos"
    };

    private static Color VigilAspectColor(int floor) => VigilAspectForFloor(floor) switch
    {
        VigilAspect.Hardened => Color.SteelBlue,
        VigilAspect.Hungry => Color.OrangeRed,
        VigilAspect.Swift => Color.Goldenrod,
        VigilAspect.Chorus => Color.MediumPurple,
        _ => Color.LightSeaGreen
    };
    private static string VigilAspectTitle(VigilAspect aspect) => aspect switch { VigilAspect.Hardened => "CASCA VIVA", VigilAspect.Hungry => "FOME DO RIO", VigilAspect.Swift => "SUSSURRO VELOZ", VigilAspect.Chorus => "CORO ERRANTE", _ => "MARÉ INVERTIDA" };
    private static string VigilAspectRule(VigilAspect aspect) => aspect switch { VigilAspect.Hardened => "O chefe recebe 25% menos dano", VigilAspect.Hungry => "Os ataques do chefe causam 30% mais dano", VigilAspect.Swift => "Os ataques começam 20% mais rápido", VigilAspect.Chorus => "Um Eco Salino surge a cada 20 segundos", _ => "A corrente puxa e repele a cada 6 segundos" };
    private static Color VigilAspectColor(VigilAspect aspect) => aspect switch { VigilAspect.Hardened => Color.SteelBlue, VigilAspect.Hungry => Color.OrangeRed, VigilAspect.Swift => Color.Goldenrod, VigilAspect.Chorus => Color.MediumPurple, _ => Color.LightSeaGreen };

    private static bool VigilTidePulling(float elapsed) => elapsed % 12f < 6f;

    private void ApplyVigilTide(float dt)
    {
        if (!vigilActive || room != 38 || RunVigilAspect(vigilFloor) != VigilAspect.Counterflow) return;
        vigilTideTime += dt;
        PointF outward = Normalize(Sub(player, new PointF(480, 330)), new PointF(1, 0));
        float direction = VigilTidePulling(vigilTideTime) ? -1 : 1;
        MovePlayer(Scale(outward, direction * 54 * dt));
    }

    private void TickVigilMinions(float dt)
    {
        if (!vigilActive || room != 38 || RunVigilAspect(vigilFloor) != VigilAspect.Chorus || drops.Count > 0 || enemies.Any(enemy => enemy.VigilMinion)) return;
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
        if (vigilBestFloor > 0) ShuffleVigilOrder();
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
        player = new PointF(150, 330); enemies.Clear(); drops.Clear(); groundHazards.Clear(); echoShards.Clear(); lockedTarget = null;
        if (vigilActive && room == 38) { vigilMinionCooldown = 20; vigilTideTime = 0; }
        if (room != 3 && !vigilActive && (clearedRooms & (1L << room)) != 0) return;
        int count = room is 3 or 18 ? 0 : room == 0 ? 3 : room == 1 ? 5 : room == 4 ? 5 : room == 5 ? 4 : room == 6 ? 6 : room is 21 or 29 or 31 or 33 or 35 or 37 or 41 ? 6 : room == 22 ? 5 : room == 23 ? 6 : room == 24 ? 5 : room == 25 ? 6 : room is 26 or 28 or 30 or 32 or 34 or 36 ? 5 : room is 38 or 40 or 42 ? 1 : room == 39 ? 5 : room == 27 ? 6 : (room is 7 or 9 or 11 or 15 or 17 or 20) ? 4 : room == 8 ? 5 : room is 10 or 12 or 14 or 16 or 19 ? 6 : room == 13 ? 4 : 4;
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
            if (room == 42 && i == 0) kind = EnemyKind.LastTuner;
            if ((room is 14 or 21) && i == count - 1) kind = EnemyKind.CooldownLeech;
            if (vigilActive && i == 0) kind = RunVigilBoss(vigilFloor);
            if (room >= 27 && i == 2) kind = EnemyKind.SaltEcho;
            if (room >= 27 && i == 3) kind = EnemyKind.MudCantor;
            if ((room is 27 or 31 or 41) && i == 4) kind = EnemyKind.SlagWeaver;
            if (!vigilActive && i == count - 1 && room is 23 or 25 or 27 or 31 or 33 or 35 or 37 or 39) kind = EnemyKind.Elite;
            float hp = kind switch { EnemyKind.Guardian => 160, EnemyKind.BlindScribe => 245, EnemyKind.RiverFerryman => 285, EnemyKind.AshWarden => 340, EnemyKind.HollowName => 390, EnemyKind.NameWeaver => townEnding == 1 ? 410 : 455, EnemyKind.FirstKeeper => townEnding == 1 ? 560 : 620, EnemyKind.OathCartographer => townEnding == 1 ? 610 : 690, EnemyKind.NameBroker => townEnding == 1 ? 720 : 790, EnemyKind.TideWitness => townEnding == 1 ? 820 : 920, EnemyKind.ReedKeeper => townEnding == 1 ? 970 : 1060, EnemyKind.FlowKeeper => townEnding == 1 ? 1120 : 1230, EnemyKind.CounterCurrent => townEnding == 1 ? 1320 : 1440, EnemyKind.RiftCantor => townEnding == 1 ? 1480 : 1600, EnemyKind.GlassHerald => townEnding == 1 ? 1740 : 1880, EnemyKind.LanternRegent => townEnding == 1 ? 1920 : 2080, EnemyKind.RouteScribe => townEnding == 1 ? 2250 : 2450, EnemyKind.DeepSilence => townEnding == 1 ? 3450 : 3750, EnemyKind.BorrowedChorus => townEnding == 1 ? 4050 : 4450, EnemyKind.LastTuner => townEnding == 1 ? 4700 : 5150, EnemyKind.Elite => 120 + Math.Max(0, room - 1) * 6, EnemyKind.SlagWeaver => 88, EnemyKind.Archer or EnemyKind.Mage or EnemyKind.InkEcho or EnemyKind.DrownedMemory or EnemyKind.EmberWisp or EnemyKind.NameMoth or EnemyKind.MudCantor or EnemyKind.PaperWraith or EnemyKind.SaltEcho or EnemyKind.CooldownLeech => 72, _ => 58 + room * 8 };
            if (vigilActive) hp = 3450 * (1 + Math.Max(0, vigilFloor - 1) * 0.28f);
            else hp *= DifficultyEnemyHealthMultiplier(difficultyTier);
            EliteAffix eliteAffix = kind == EnemyKind.Elite ? EliteAffixForRoll(random.Next(6)) : EliteAffix.Armored;
            EliteAffix? eliteAffix2 = kind == EnemyKind.Elite && !vigilActive && (room >= 25 || difficultyTier >= 2) ? EliteSecondaryAffixForRoll(random.Next(3), eliteAffix) : null;
            bool armoredElite = kind == EnemyKind.Elite && (eliteAffix == EliteAffix.Armored || eliteAffix2 == EliteAffix.Armored);
            var enemy = new Enemy { Pos = new PointF(485 + i % 3 * 120, 215 + i / 3 * 165), Kind = kind, Hp = hp, MaxHp = hp, Cooldown = 0.7f + (float)random.NextDouble(), SummonCooldown = 5.5f + (float)random.NextDouble() * 2, SupportCooldown = 2.5f + (float)random.NextDouble() * 2, EliteAffix = eliteAffix, EliteAffix2 = eliteAffix2, Armored = armoredElite };
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
        if (title)
        {
            HandleTitleKey(e.KeyCode);
            return;
        }
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
        if (e.KeyCode == Keys.Enter && (dead || victory)) { StartGame(); return; }
        if (e.KeyCode == Keys.F5 && victory) { SaveGame(); return; }
        if (title || dead || victory) return;
        if (storyOverlay) { if (e.KeyCode is Keys.Enter or Keys.Space) AdvanceStory(); return; }
        if (memoryOverlay) { if (e.KeyCode is Keys.Enter or Keys.Space) memoryOverlay = false; return; }
        if (mapOpen) { if (e.KeyCode is Keys.M or Keys.Escape) mapOpen = false; return; }
        if (journalOpen) { if (e.KeyCode is Keys.L or Keys.Escape) journalOpen = false; Invalidate(); return; }
        if (characterSheetOpen) { if (e.KeyCode is Keys.P or Keys.Escape) characterSheetOpen = false; return; }
        if (skillMenuOpen)
        {
            if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.B) { skillMenuOpen = false; return; }
            if (e.KeyCode == Keys.Tab) { skillMenuAlternate = !skillMenuAlternate; return; }
            if (e.KeyCode is Keys.F or Keys.V or Keys.G or Keys.C) { skillMenuSlot = e.KeyCode switch { Keys.F => 0, Keys.V => 1, Keys.G => 2, _ => 3 }; return; }
            if (e.KeyCode is Keys.Up or Keys.Right) { CycleSkillVariant(skillMenuSlot, 1); return; }
            if (e.KeyCode is Keys.Down or Keys.Left) { CycleSkillVariant(skillMenuSlot, -1); return; }
            if (e.KeyCode == Keys.Back) { SkillMenuLoadout()[skillMenuSlot] = 0; return; }
            if (e.KeyCode == Keys.D0) { EquipSkill(skillMenuSlot, 10); return; }
            if (e.KeyCode is >= Keys.D1 and <= Keys.D9) { EquipSkill(skillMenuSlot, (int)e.KeyCode - (int)Keys.D0); return; }
            return;
        }
        if (forgeMenuOpen) { if (defenseForgeOpen) HandleDefenseForgeKey(e.KeyCode); else if (e.KeyCode == Keys.T) defenseForgeOpen = true; else if (e.KeyCode == Keys.A) { secondaryForgeOpen = !secondaryForgeOpen; notice = secondaryForgeOpen ? weaponRarities[weapon] == 2 ? "AFIXO SECUNDÁRIO • ESCOLHA UMA RESSONÂNCIA DE RELÍQUIA" : "AFIXO SECUNDÁRIO • DISPONÍVEL APENAS PARA RELÍQUIAS" : "AFIXO PRINCIPAL • TÊMPERA NORMAL"; noticeTime = 2.5f; } else if (e.KeyCode == Keys.D9) CycleDifficultyTier(); else if (e.KeyCode == Keys.D0) ReforgeWeapon(0); else if (e.KeyCode is >= Keys.D1 and <= Keys.D6 or Keys.D8) ReforgeWeapon((int)e.KeyCode - (int)Keys.D0); else if (e.KeyCode == Keys.D7 && secondaryForgeOpen) { notice = "USE 0 PARA CADÊNCIA • O AFIXO PRINCIPAL NÃO PODE SER REPETIDO"; noticeTime = 2.5f; } else if (e.KeyCode == Keys.D7) ReforgeWeapon(7); else if (e.KeyCode == Keys.Escape) { forgeMenuOpen = false; secondaryForgeOpen = false; } return; }
        if (endingChoiceOpen) { if (e.KeyCode == Keys.D1) ChooseTownEnding(1); else if (e.KeyCode == Keys.D2) ChooseTownEnding(2); else if (e.KeyCode == Keys.D3) endingChoiceOpen = false; return; }
        if (vigilChoiceOpen) { if (e.KeyCode == Keys.D1) ResolveVigilFloor(true); else if (e.KeyCode == Keys.D2) ResolveVigilFloor(false); else if (e.KeyCode == Keys.D3 || e.KeyCode == Keys.Escape) vigilChoiceOpen = false; return; }
        if (chestChoice) { if (e.KeyCode == Keys.D1) gold += 75; else if (e.KeyCode == Keys.D2) potions += 2; else if (e.KeyCode == Keys.D3) health = maxHealth; else return; chestChoice = false; EnterVesper(); return; }
        if (talentChoice) { SelectTalent(e.KeyCode); return; }
        if (e.KeyCode == Keys.P) { characterSheetOpen = true; inventoryOpen = false; skillMenuOpen = false; return; }
        if (e.KeyCode == Keys.M) { mapOpen = true; inventoryOpen = false; skillMenuOpen = false; return; }
        if (e.KeyCode == Keys.L) { journalOpen = true; inventoryOpen = skillMenuOpen = characterSheetOpen = mapOpen = false; return; }
        if (e.KeyCode == Keys.I) { inventoryOpen = !inventoryOpen; return; }
        if (e.KeyCode == Keys.B) { skillMenuOpen = true; inventoryOpen = false; return; }
        if (inventoryOpen) { if (e.KeyCode >= Keys.D1 && e.KeyCode <= Keys.D4) EquipWeapon((int)e.KeyCode - (int)Keys.D1); else if (e.KeyCode == Keys.D5 && armor) armorOn = !armorOn; else if (e.KeyCode == Keys.D6 && relic) relicOn = !relicOn; else if (e.KeyCode == Keys.D7) CycleCharm(1); else if (e.KeyCode == Keys.D8) CycleCharm(2); return; }
        if (e.KeyCode == Keys.T) { ToggleSkillLoadout(); return; }
        if (e.KeyCode == Keys.Space || e.KeyCode == Keys.J) Attack();
        else if (e.KeyCode == Keys.ShiftKey || e.KeyCode == Keys.K) StartDash();
        else if (e.KeyCode == Keys.R) Whirlwind();
        else if (e.KeyCode == Keys.F) CastEquippedSkill(0);
        else if (e.KeyCode == Keys.V) CastEquippedSkill(1);
        else if (e.KeyCode == Keys.G) CastEquippedSkill(2);
        else if (e.KeyCode == Keys.C) CastEquippedSkill(3);
        else if (e.KeyCode == Keys.Q) UsePotion();
        else if (e.KeyCode == Keys.E) Interact();
        else if (e.KeyCode == Keys.X) SalvageNearbyWeapon();
        else if (e.KeyCode == Keys.F5) { if (vigilActive) { notice = "TERMINE A VIGÍLIA E SALVE NO REFÚGIO"; noticeTime = 2; } else if (enemies.Count == 0 && drops.Count == 0) SaveGame(); else { notice = "LIMPE A SALA E RECOLHA O LOOT ANTES DE SALVAR"; noticeTime = 2; } }
        else if (e.KeyCode == Keys.Tab) CycleTarget();
    }

    private static int TalentRespecGoldCost(int level) => 250 + Math.Max(1, level) * 100;
    private static int TalentRespecGlassCost(int level) => 5 + Math.Max(1, level) / 5;

    private void RespecTalents()
    {
        int spent = healthTalent + skillTalent + dodgeTalent + techniqueTalent + (combatDiscipline > 0 ? 1 : 0) + (combatMastery > 0 ? 1 : 0) + (combatFocus > 0 ? 1 : 0) + (combatApex > 0 ? 1 : 0);
        int goldCost = TalentRespecGoldCost(level), glassCost = TalentRespecGlassCost(level);
        if (pendingTalents > 0 || talentChoice) { notice = "GASTE OS PONTOS DE TALENTO PENDENTES PRIMEIRO"; noticeTime = 2; return; }
        if (spent == 0) { notice = "NÃO HÁ TALENTOS PARA REDISTRIBUIR"; noticeTime = 2; return; }
        if (gold < goldCost || tideGlass < glassCost) { notice = $"REDISTRIBUIR CUSTA {goldCost} OURO E {glassCost} SAL VÍTREO"; noticeTime = 2.5f; return; }
        float healthRatio = maxHealth > 0 ? health / maxHealth : 1;
        gold -= goldCost; tideGlass -= glassCost; maxHealth = Math.Max(1, maxHealth - healthTalent * 20); healthTalent = skillTalent = dodgeTalent = techniqueTalent = combatDiscipline = combatMastery = combatFocus = combatApex = 0; health = Math.Clamp(maxHealth * healthRatio, 1, maxHealth);
        pendingTalents = spent; talentChoice = pendingTalents > 0; forgeMenuOpen = false; notice = $"TALENTOS REDISTRIBUÍDOS: {spent} PONTOS DEVOLVIDOS"; noticeTime = 3;
    }

    private void SelectTalent(Keys key)
    {
        if (level >= 5 && combatDiscipline == 0)
        {
            if (key == Keys.D5) combatDiscipline = 1;
            else if (key == Keys.D6) combatDiscipline = 2;
            else return;
            pendingTalents--; talentChoice = pendingTalents > 0; return;
        }
        if (level >= 10 && combatMastery == 0)
        {
            if (key == Keys.D5) combatMastery = 1;
            else if (key == Keys.D6) combatMastery = 2;
            else return;
            pendingTalents--; talentChoice = pendingTalents > 0; return;
        }
        if (level >= 15 && combatFocus == 0)
        {
            if (key == Keys.D5) combatFocus = 1;
            else if (key == Keys.D6) combatFocus = 2;
            else return;
            pendingTalents--; talentChoice = pendingTalents > 0; return;
        }
        if (level >= 20 && combatApex == 0)
        {
            if (key == Keys.D5) combatApex = 1;
            else if (key == Keys.D6) combatApex = 2;
            else return;
            pendingTalents--; talentChoice = pendingTalents > 0; return;
        }
        if (key == Keys.D1) { healthTalent++; maxHealth += 20; health = Math.Min(maxHealth, health + 20); }
        else if (key == Keys.D2) skillTalent++;
        else if (key == Keys.D3) dodgeTalent++;
        else if (key == Keys.D4) techniqueTalent++;
        else return;
        pendingTalents--; talentChoice = pendingTalents > 0;
    }

    private void Defeat(Enemy enemy)
    {
        if (!enemies.Remove(enemy)) return;
        if (ReferenceEquals(lockedTarget, enemy)) lockedTarget = enemies.OrderBy(candidate => Distance(player, candidate.Pos)).FirstOrDefault();
        if (enemy.EliteMinion) return;
        enemiesDefeated++; int goldDrop = enemy.VigilMinion ? 8 + Math.Min(20, vigilFloor * 2) : vigilActive ? 35 + Math.Min(500, vigilFloor * 15) : enemy.Boss ? 35 : enemy.Kind == EnemyKind.Elite ? 20 : 8;
        int xpReward = enemy.VigilMinion ? 12 : enemy.Boss ? 75 : enemy.Kind == EnemyKind.Elite ? 40 : 22;
        if (!vigilActive) { goldDrop = (int)Math.Round(goldDrop * DifficultyRewardMultiplier(difficultyTier)); xpReward = (int)Math.Round(xpReward * DifficultyRewardMultiplier(difficultyTier)); }
        drops.Add(new Drop { Pos = enemy.Pos, Kind = "gold", Amount = goldDrop }); AddXp(xpReward);
        if (enemy.Kind == EnemyKind.Elite)
        {
            drops.Add(new Drop { Pos = Offset(enemy.Pos, 24, -20), Kind = "glass", Amount = Math.Clamp(1 + room / 12, 1, 4) });
            if (EliteWeaponDropRoll(random.Next(100), eliteWeaponMisses, vigilActive ? 0 : DifficultyEliteWeaponBonus(difficultyTier)))
            {
                drops.Add(CreateWeaponDrop(Offset(enemy.Pos, -24, 28), random.Next(1, 3)));
                eliteWeaponMisses = NextEliteWeaponMisses(eliteWeaponMisses, true);
            }
            else eliteWeaponMisses = NextEliteWeaponMisses(eliteWeaponMisses, false);
        }
        health = Math.Min(maxHealth, health + WeaponKillHealing());
        if (enemy.Boss && vigilActive) drops.Add(new Drop { Pos = Offset(enemy.Pos, 0, -28), Kind = "glass", Amount = 1 + Math.Min(5, (vigilFloor - 1) / 3) });
        if (enemy.Boss) drops.Add(CreateWeaponDrop(Offset(enemy.Pos, 0, 28), random.Next(1, 3), true));
        if (enemy.Boss && vigilActive && vigilFloor % 5 == 0 && (ownedCharms & 64) == 0) drops.Add(new Drop { Pos = Offset(enemy.Pos, 0, -48), Kind = "charm", Amount = 7 });
        if (enemy.Boss && vigilActive && vigilFloor % 10 == 0 && (ownedCharms & 128) == 0) drops.Add(new Drop { Pos = Offset(enemy.Pos, 0, -68), Kind = "charm", Amount = 8 });
        if (enemy.Boss && vigilActive && vigilFloor % 15 == 0 && (ownedCharms & 256) == 0) drops.Add(new Drop { Pos = Offset(enemy.Pos, 0, -88), Kind = "charm", Amount = 9 });
        if (enemy.Boss && vigilActive && vigilFloor % 20 == 0 && (ownedCharms & 512) == 0) drops.Add(new Drop { Pos = Offset(enemy.Pos, 0, -108), Kind = "charm", Amount = 10 });
        if (enemy.Boss && vigilActive && vigilFloor % 25 == 0 && (ownedCharms & 1024) == 0) drops.Add(new Drop { Pos = Offset(enemy.Pos, 0, -128), Kind = "charm", Amount = 11 });
        if (enemy.Boss && vigilActive && HasCharm(7)) health = Math.Min(maxHealth, health + maxHealth * 0.12f);
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
        if (enemy.Kind == EnemyKind.LastTuner && !unstruckTrace) drops.Add(new Drop { Pos = Offset(enemy.Pos, -24, 0), Kind = "unstruck" });
        if (enemy.Kind == EnemyKind.LastTuner && (ownedWeapons & 8) == 0) drops.Add(CreateRelicWeaponDrop(Offset(enemy.Pos, 26, 0), 3));
        if (enemy.Kind == EnemyKind.LastTuner && (ownedCharms & (1 << 11)) == 0) drops.Add(new Drop { Pos = Offset(enemy.Pos, 0, 30), Kind = "charm", Amount = 12 });
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
            if (armorOn) veinResonanceTime = 2.5f + (relicTrait == 3 ? 1f : 0f);
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
    private bool GameplayPaused() => focusPaused || title || dead || victory || storyOverlay || memoryOverlay || mapOpen || journalOpen || inventoryOpen || skillMenuOpen || characterSheetOpen || forgeMenuOpen || talentChoice || chestChoice || endingChoiceOpen || vigilChoiceOpen;

    private void TickGame()
    {
        float dt = timer.Interval / 1000f; noticeTime = Math.Max(0, noticeTime - dt);
        GameAudio.SetAmbientActive(!GameplayPaused(), room);
        if (GameplayPaused()) { Invalidate(); return; }
        attackCd = Math.Max(0, attackCd - dt); attackFlash = Math.Max(0, attackFlash - dt); skillCd = Math.Max(0, skillCd - dt); shardCd = Math.Max(0, shardCd - dt); bellCd = Math.Max(0, bellCd - dt); bellWaveTime = Math.Max(0, bellWaveTime - dt); pullCd = Math.Max(0, pullCd - dt); pullWaveTime = Math.Max(0, pullWaveTime - dt); wardCd = Math.Max(0, wardCd - dt); wardTime = Math.Max(0, wardTime - dt); refrainCd = Math.Max(0, refrainCd - dt); refrainWaveTime = Math.Max(0, refrainWaveTime - dt); beaconCd = Math.Max(0, beaconCd - dt); returnVoiceCd = Math.Max(0, returnVoiceCd - dt); returnVoiceWaveTime = Math.Max(0, returnVoiceWaveTime - dt); finalStrikeCd = Math.Max(0, finalStrikeCd - dt); finalStrikeFlash = Math.Max(0, finalStrikeFlash - dt); stoneBastionCd = Math.Max(0, stoneBastionCd - dt); stoneBastionTime = Math.Max(0, stoneBastionTime - dt); nameWeaverCd = Math.Max(0, nameWeaverCd - dt); dashCd = Math.Max(0, dashCd - dt); dashTime = Math.Max(0, dashTime - dt); invulnerable = Math.Max(0, invulnerable - dt); veinResonanceTime = Math.Max(0, veinResonanceTime - dt); windupPulse += dt; TickFloatingTexts(dt);
        PointF dir = Normalize(MovementInput(), PointF.Empty); if (dashTime > 0) dir = dashDirection;
        MovePlayer(Scale(dir, (dashTime > 0 ? 530 : ClassMovementSpeed()) * dt));
        PointF drift = CurrentDrift();
        if (dashTime <= 0) MovePlayer(Scale(drift, dt));
        ApplyVigilTide(dt);
        PointF animationDirection = Length(dir) > 0.001f ? dir : drift;
        if (Length(animationDirection) > 0.001f) playerAnimationTime += dt;
        else playerAnimationTime = 0;
        for (int i = drops.Count - 1; i >= 0; i--) if (drops[i].Kind != "weapon" && Distance(player, drops[i].Pos) < 30) { GameAudio.Play(GameAudio.Cue.Loot); Collect(drops[i]); }
        TickBeacon(dt); int roomBeingUpdated = room; foreach (Enemy enemy in enemies.ToArray()) { PointF previousPos = enemy.Pos; UpdateEnemy(enemy, dt); enemy.Moving = Distance(previousPos, enemy.Pos) > 0.01f; if (enemy.Moving) enemy.AnimationTime += dt; if (dead || room != roomBeingUpdated) break; }
        TickVigilMinions(dt); TickGroundHazards(dt); TickEnemyWounds(dt);
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
        EnemyKind.CooldownLeech => 42,
        EnemyKind.SlagWeaver => 48,
        _ => 0
    };

    private void CreateSlagHazard(PointF position) => groundHazards.Add(new GroundHazard { Pos = position });

    private void TickGroundHazards(float dt)
    {
        for (int i = groundHazards.Count - 1; i >= 0; i--)
        {
            GroundHazard hazard = groundHazards[i]; hazard.TimeLeft -= dt; hazard.TickCooldown -= dt;
            if (hazard.TimeLeft <= 0) { groundHazards.RemoveAt(i); continue; }
            if (hazard.TickCooldown > 0) continue;
            hazard.TickCooldown = 0.85f;
            if (Distance(player, hazard.Pos) <= hazard.Radius && invulnerable <= 0 && dashTime <= 0) HitPlayer(hazard.Damage);
        }
    }

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
    private static bool BossUsesRing(Enemy e) => e.BossPattern == 0 && e.Kind is EnemyKind.RouteScribe or EnemyKind.LastTuner;
    private static float BossRingInnerRadius(Enemy e) => e.Kind == EnemyKind.LastTuner ? 96 : e.Kind == EnemyKind.RouteScribe ? 68 : 0;
    private static bool BossUsesLine(Enemy e) => e.BossPattern == 2 && e.Kind is EnemyKind.FirstKeeper or EnemyKind.NameWeaver or EnemyKind.OathCartographer or EnemyKind.NameBroker or EnemyKind.TideWitness or EnemyKind.ReedKeeper or EnemyKind.FlowKeeper or EnemyKind.CounterCurrent or EnemyKind.RiftCantor or EnemyKind.LanternRegent or EnemyKind.RouteScribe or EnemyKind.DeepSilence or EnemyKind.BorrowedChorus or EnemyKind.LastTuner;
    private static bool BossConeContains(Enemy e, PointF point)
    {
        PointF axis = Normalize(Sub(e.Target, e.Pos), new PointF(1, 0)), direction = Sub(point, e.Pos); float distance = Length(direction);
        return distance < 245 && distance > 0.01f && Dot(axis, Scale(direction, 1 / distance)) >= MathF.Cos(34 * MathF.PI / 180);
    }
    private static float BossCircleRadius(Enemy e) => e.BossPattern == 0 ? e.Kind switch { EnemyKind.GlassHerald => 135, EnemyKind.LanternRegent => 150, EnemyKind.RouteScribe => 160, EnemyKind.LastTuner => 178, _ => 90 } : e.BossPattern == 2 ? e.Kind switch { EnemyKind.RiverFerryman => 85, EnemyKind.AshWarden => 68, EnemyKind.HollowName => 85, _ => 42 } : 42;
    private static bool BossAttackContains(Enemy e, PointF point)
    {
        if (BossUsesCone(e)) return BossConeContains(e, point);
        if (BossUsesRing(e)) { float distance = Distance(point, e.Pos); return distance > BossRingInnerRadius(e) && distance < BossCircleRadius(e); }
        if (!BossUsesLine(e)) return Distance(point, e.BossPattern == 0 ? e.Pos : e.Target) < BossCircleRadius(e);
        float width = e.Kind == EnemyKind.FirstKeeper ? 25 : 24;
        if (DistanceToSegment(point, e.Pos, e.Target) < width) return true;
        return e.Kind is EnemyKind.FirstKeeper or EnemyKind.DeepSilence or EnemyKind.BorrowedChorus or EnemyKind.LastTuner && DistanceToSegment(point, e.Target2Start, e.Target2) < width;
    }

    private static (Color Fill, Color Edge) AttackWarningPalette(bool highContrast) => highContrast
        ? (Color.FromArgb(78, 255, 217, 36), Color.FromArgb(255, 255, 244, 122))
        : (Color.FromArgb(45, 235, 115, 75), Color.FromArgb(235, 235, 115, 75));

    private void DrawBossWarning(Graphics g, Enemy e)
    {
        (Color fillColor, Color edgeColor) = AttackWarningPalette(highContrastTelegraphs);
        float urgency = Math.Clamp(1f - e.Windup / Math.Max(0.01f, e.WindupMax), 0f, 1f);
        float pulse = (0.5f + 0.5f * MathF.Sin(windupPulse * (7f + urgency * 11f))) * urgency;
        fillColor = Color.FromArgb(Math.Clamp(fillColor.A + (int)(urgency * 24), 0, 255), fillColor);
        edgeColor = Color.FromArgb(Math.Clamp(edgeColor.A + (int)(urgency * 18 + pulse * 35), 0, 255), edgeColor);
        using var outline = new Pen(edgeColor, highContrastTelegraphs ? 4 + urgency + pulse : 2.5f + urgency * 2 + pulse * 1.5f);
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
        if (highContrastTelegraphs) { using var shadow = new Pen(Color.FromArgb(240, 12, 13, 15), width * 2 + 8) { StartCap = LineCap.Round, EndCap = LineCap.Round }; g.DrawLine(shadow, e.Pos, e.Target); if (e.Kind is EnemyKind.FirstKeeper or EnemyKind.DeepSilence or EnemyKind.BorrowedChorus or EnemyKind.LastTuner) g.DrawLine(shadow, e.Target2Start, e.Target2); }
        g.DrawLine(area, e.Pos, e.Target); g.DrawLine(outline, e.Pos, e.Target);
        if (e.Kind is EnemyKind.FirstKeeper or EnemyKind.DeepSilence or EnemyKind.BorrowedChorus or EnemyKind.LastTuner) { g.DrawLine(area, e.Target2Start, e.Target2); g.DrawLine(outline, e.Target2Start, e.Target2); }
    }

    private void UpdateEnemy(Enemy e, float dt)
    {
        e.AttackPoseTime = Math.Max(0, e.AttackPoseTime - dt); e.BeaconMarkTime = Math.Max(0, e.BeaconMarkTime - dt); e.BellMarkTime = Math.Max(0, e.BellMarkTime - dt); e.NameWeaverMarkTime = Math.Max(0, e.NameWeaverMarkTime - dt);
        if (HasEliteAffix(e, EliteAffix.Summoner))
        {
            if (e.SummonWindup > 0 && e.Stagger > 0) { e.SummonWindup = 0; e.SummonCooldown = 2.5f; }
            bool hasEliteEcho = enemies.Any(enemy => enemy.EliteMinion);
            if (e.SummonWindup > 0)
            {
                e.SummonWindup -= dt;
                if (e.SummonWindup <= 0) SummonEliteEcho(e);
                return;
            }
            if (!hasEliteEcho && e.Stagger <= 0 && e.SummonCooldown > 0) e.SummonCooldown -= dt;
            if (!hasEliteEcho && e.SummonCooldown <= 0 && e.Windup <= 0 && e.Stagger <= 0)
            {
                e.SummonWindup = 0.95f;
                GameAudio.Play(GameAudio.Cue.EliteWarning);
                return;
            }
        }
        if (HasEliteAffix(e, EliteAffix.Conductor) && UpdateConductor(e, dt)) return;
        float attackTempo = vigilActive && e.Boss && RunVigilAspect(vigilFloor) == VigilAspect.Swift ? 1.25f : 1;
        e.Cooldown -= dt * attackTempo; e.Flash = Math.Max(0, e.Flash - dt); PointF offset = Sub(player, e.Pos); float dist = Length(offset);
        if (e.Stagger > 0) { e.Stagger = Math.Max(0, e.Stagger - dt); return; }
        if (e.Windup > 0) {
            e.Windup -= dt * attackTempo;
            if (e.Windup <= 0) e.AttackPoseTime = 0.16f;
            bool inAttack = e.Boss ? BossAttackContains(e, player) : NonBossAttackContains(e, player);
            if (e.Windup <= 0 && inAttack && invulnerable <= 0 && dashTime <= 0)
            {
                if (e.Kind == EnemyKind.SlagWeaver) HitPlayer(18); else HitPlayer(vigilActive ? 112 : e.Boss ? e.Kind == EnemyKind.BlindScribe ? 23 : e.Kind == EnemyKind.RiverFerryman ? 28 : e.Kind == EnemyKind.AshWarden ? 31 : e.Kind == EnemyKind.HollowName ? 36 : e.Kind == EnemyKind.NameWeaver ? 39 : e.Kind == EnemyKind.FirstKeeper ? 44 : e.Kind == EnemyKind.OathCartographer ? 42 : e.Kind == EnemyKind.NameBroker ? 48 : e.Kind == EnemyKind.CounterCurrent ? 68 : e.Kind == EnemyKind.RiftCantor ? 74 : e.Kind == EnemyKind.GlassHerald ? 82 : e.Kind == EnemyKind.LanternRegent ? 88 : e.Kind == EnemyKind.RouteScribe ? 96 : e.Kind == EnemyKind.DeepSilence ? 112 : e.Kind == EnemyKind.LastTuner ? 126 : e.Kind == EnemyKind.BorrowedChorus ? 118 : e.Kind == EnemyKind.FlowKeeper ? 62 : e.Kind == EnemyKind.ReedKeeper ? 56 : e.Kind == EnemyKind.TideWitness ? 52 : 26 : e.Kind == EnemyKind.Elite ? HasEliteAffix(e, EliteAffix.Bloodbound) ? 24 : e.EliteAffix == EliteAffix.Spearbound ? 18 : 20 : e.Kind == EnemyKind.CooldownLeech ? 14 : e.Kind == EnemyKind.Mage ? 16 : e.Kind is EnemyKind.InkEcho or EnemyKind.DrownedMemory or EnemyKind.EmberWisp or EnemyKind.NameMoth or EnemyKind.MudCantor or EnemyKind.PaperWraith or EnemyKind.SaltEcho ? 18 : 10);
                if (e.Kind == EnemyKind.CooldownLeech) { DelayTechniqueCooldowns(1.1f); AddFloatingText(new PointF(player.X, player.Y - 46), "RECARGAS +1,1 S", Color.MediumOrchid); }
                if (HasEliteAffix(e, EliteAffix.Bloodbound)) e.Hp = Math.Min(e.MaxHp, e.Hp + 8);
            }
            if (e.Windup <= 0 && e.Kind == EnemyKind.SlagWeaver)
            {
                CreateSlagHazard(e.Target);
                AddFloatingText(new PointF(e.Target.X, e.Target.Y - 46), "ESCÓRIA VIVA", Color.FromArgb(241, 171, 106));
            }
            return;
        }
        if (e.Kind == EnemyKind.CooldownLeech)
        {
            if (dist < 150) e.Pos = MoveEnemy(e, Scale(Normalize(Sub(e.Pos, player), PointF.Empty), 58 * dt));
            else if (dist > 285) e.Pos = MoveEnemy(e, Scale(Normalize(offset, PointF.Empty), 36 * dt));
            else if (e.Cooldown <= 0) { BeginWindup(e, player, 1.05f); e.Cooldown = 3.25f; }
            SeparateFromPack(e, dt); return;
        }
        if (e.Kind == EnemyKind.SlagWeaver)
        {
            if (dist < 185) e.Pos = MoveEnemy(e, Scale(Normalize(Sub(e.Pos, player), PointF.Empty), 54 * dt));
            else if (dist > 260) e.Pos = MoveEnemy(e, Scale(Normalize(offset, PointF.Empty), 36 * dt));
            else if (e.Cooldown <= 0) { BeginWindup(e, player, 0.95f); e.Cooldown = 3.75f; }
            SeparateFromPack(e, dt); return;
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
        if (e.Kind == EnemyKind.Elite && e.Cooldown <= 0 && dist < 220) { bool swift = HasEliteAffix(e, EliteAffix.Swift), armored = HasEliteAffix(e, EliteAffix.Armored); BeginWindup(e, player, swift ? armored ? 0.48f : 0.38f : armored ? 0.72f : 0.58f); e.Cooldown = swift ? armored ? 2f : 1.55f : armored ? 2.5f : 2.1f; return; }
        if (e.Kind == EnemyKind.LastTuner && e.Cooldown <= 0)
        {
            e.BossPattern = (e.BossPattern + 1) % 3;
            float windup = e.Hp < e.MaxHp * 0.5f ? 0.54f : 0.82f;
            if (vigilActive && RunVigilAspect(vigilFloor) == VigilAspect.Swift) windup *= 0.8f;
            BeginWindup(e, e.BossPattern is 1 or 2 ? player : e.Pos, windup); e.Cooldown = e.Hp < e.MaxHp * 0.5f ? 1.35f : 2.1f; return;
        }
        if (e.Boss && e.Cooldown <= 0 && (dist < 135 || (e.Kind is EnemyKind.BlindScribe or EnemyKind.RiverFerryman or EnemyKind.AshWarden or EnemyKind.HollowName or EnemyKind.NameWeaver or EnemyKind.FirstKeeper or EnemyKind.OathCartographer or EnemyKind.NameBroker or EnemyKind.TideWitness or EnemyKind.ReedKeeper or EnemyKind.FlowKeeper or EnemyKind.CounterCurrent or EnemyKind.RiftCantor or EnemyKind.GlassHerald or EnemyKind.LanternRegent or EnemyKind.RouteScribe or EnemyKind.DeepSilence or EnemyKind.BorrowedChorus))) { e.BossPattern = (e.Kind is EnemyKind.RiverFerryman or EnemyKind.AshWarden or EnemyKind.HollowName or EnemyKind.NameWeaver or EnemyKind.FirstKeeper or EnemyKind.OathCartographer or EnemyKind.NameBroker or EnemyKind.TideWitness or EnemyKind.ReedKeeper or EnemyKind.FlowKeeper or EnemyKind.CounterCurrent or EnemyKind.RiftCantor or EnemyKind.GlassHerald or EnemyKind.LanternRegent or EnemyKind.RouteScribe or EnemyKind.DeepSilence or EnemyKind.BorrowedChorus) ? (e.BossPattern + 1) % 3 : 1 - e.BossPattern; bool aimed = (e.Kind == EnemyKind.BlindScribe && e.BossPattern == 1) || ((e.Kind is EnemyKind.RiverFerryman or EnemyKind.AshWarden) && (e.BossPattern is 1 or 2)) || (e.Kind is EnemyKind.HollowName or EnemyKind.NameWeaver or EnemyKind.FirstKeeper or EnemyKind.OathCartographer or EnemyKind.NameBroker or EnemyKind.TideWitness or EnemyKind.ReedKeeper or EnemyKind.FlowKeeper or EnemyKind.CounterCurrent or EnemyKind.RiftCantor or EnemyKind.GlassHerald or EnemyKind.LanternRegent or EnemyKind.RouteScribe or EnemyKind.DeepSilence or EnemyKind.BorrowedChorus) && e.BossPattern is 1 or 2; float windup = e.Hp < e.MaxHp * 0.5f ? 0.52f : 0.78f; if (vigilActive && RunVigilAspect(vigilFloor) == VigilAspect.Swift) windup *= 0.8f; if (e.Kind == EnemyKind.FirstKeeper && e.BossPattern == 2) { PointF axis = Normalize(Sub(player, e.Pos), new PointF(1, 0)); PointF side = new(-axis.Y, axis.X); e.Target = Add(e.Pos, Scale(axis, 920)); e.Target2Start = Add(player, Scale(side, -460)); e.Target2 = Add(player, Scale(side, 460)); e.Windup = e.WindupMax = windup; GameAudio.Play(GameAudio.Cue.BossWarning); } else BeginWindup(e, aimed ? player : e.Pos, windup); e.Cooldown = e.Hp < e.MaxHp * 0.5f ? 1.25f : 2.0f; return; }
        if (e.Kind == EnemyKind.Mage && e.Cooldown <= 0) { BeginWindup(e, player, 0.8f); e.Cooldown = 2.2f; SeparateFromPack(e, dt); return; }
        if (dist > (e.Boss ? 40 : 30)) e.Pos = MoveEnemy(e, Scale(Normalize(offset, PointF.Empty), (e.Boss ? 55 : MeleeChaseSpeed(room, HasEliteAffix(e, EliteAffix.Swift))) * dt));
        else if (e.Cooldown <= 0) { BeginWindup(e, player, 0.45f); e.Cooldown = 1.35f; }
        if (!e.Boss) SeparateFromPack(e, dt);
    }

    private bool UpdateConductor(Enemy conductor, float dt)
    {
        if (conductor.SupportWindup > 0)
        {
            if (conductor.Stagger > 0) { conductor.SupportWindup = 0; conductor.SupportCooldown = 1.8f; return true; }
            conductor.SupportWindup -= dt;
            if (conductor.SupportWindup <= 0)
            {
                foreach (Enemy ally in enemies.Where(ally => ConductorPulseCovers(conductor, ally)).ToArray())
                {
                    float healed = Math.Min(ally.MaxHp - ally.Hp, MathF.Ceiling(ally.MaxHp * 0.12f));
                    if (healed <= 0) continue;
                    ally.Hp += healed; ally.Flash = 0.18f;
                    AddFloatingText(new PointF(ally.Pos.X, ally.Pos.Y - ally.Radius - 10), $"+{Math.Max(1, (int)Math.Round(healed))}", Color.LightGreen);
                }
            }
            return true;
        }
        if (conductor.Stagger > 0) return false;
        conductor.SupportCooldown = Math.Max(0, conductor.SupportCooldown - dt);
        if (conductor.SupportCooldown <= 0 && conductor.Windup <= 0)
        {
            conductor.SupportWindup = 0.9f; conductor.SupportCooldown = 5.8f;
            GameAudio.Play(GameAudio.Cue.EliteWarning); return true;
        }
        return false;
    }

    private static bool ConductorPulseCovers(Enemy conductor, Enemy ally) => Distance(conductor.Pos, ally.Pos) <= ConductorPulseRadius;

    private void SummonEliteEcho(Enemy summoner)
    {
        if (!enemies.Contains(summoner)) return;
        PointF spawn = Add(summoner.Pos, new PointF(34, -26));
        if (Blocked(spawn, 14)) spawn = Add(summoner.Pos, new PointF(-34, 26));
        Enemy echo = new() { Pos = spawn, Kind = EnemyKind.InkEcho, Hp = 42 + Math.Max(0, room) * 2, MaxHp = 42 + Math.Max(0, room) * 2, Cooldown = 0.8f, EliteMinion = true };
        enemies.Add(echo);
        summoner.SummonCooldown = 7.5f;
        AddFloatingText(new PointF(summoner.Pos.X, summoner.Pos.Y - 36), "ECO INVOCADO", Color.MediumOrchid);
    }

    private static void BeginWindup(Enemy enemy, PointF target, float duration)
    {
        enemy.Target = target; enemy.Windup = enemy.WindupMax = duration; enemy.AttackPoseTime = 0;
        if (enemy.Boss) GameAudio.Play(GameAudio.Cue.BossWarning);
        else if (enemy.Kind == EnemyKind.Elite) GameAudio.Play(GameAudio.Cue.EliteWarning);
        if (enemy.Kind == EnemyKind.DeepSilence && enemy.BossPattern == 2)
        {
            PointF axis = Normalize(Sub(target, enemy.Pos), new PointF(1, 0)); PointF side = new(-axis.Y, axis.X);
            enemy.Target2Start = Add(enemy.Pos, Scale(side, 74)); enemy.Target2 = Add(target, Scale(side, 74));
        }
        if (enemy.Kind == EnemyKind.LastTuner && enemy.BossPattern == 2)
        {
            PointF axis = Normalize(Sub(target, enemy.Pos), new PointF(1, 0)); PointF side = new(-axis.Y, axis.X);
            PointF marked = target; enemy.Target = Add(enemy.Pos, Scale(axis, 920));
            enemy.Target2Start = Add(marked, Scale(side, -460)); enemy.Target2 = Add(marked, Scale(side, 460));
        }
    }
    private void DamageEnemy(Enemy enemy, float amount, bool critical = false)
    {
        if (vigilActive && enemy.Boss && RunVigilAspect(vigilFloor) == VigilAspect.Hardened) amount *= 0.75f;
        if (enemy.NameWeaverMarkTime > 0) amount *= 1.25f;
        enemy.Hp -= amount;
        if (amount <= 0) return;
        string value = Math.Max(1, (int)Math.Round(amount)).ToString();
        AddFloatingText(new PointF(enemy.Pos.X, enemy.Pos.Y - enemy.Radius - 10), critical ? $"CRÍTICO {value}" : value, critical ? Color.FromArgb(255, 211, 123) : Color.AntiqueWhite);
    }

    private void ApplyWeaponWound(Enemy enemy, bool critical)
    {
        if (!critical || enemy.Hp <= 0 || !HasWeaponAffix(weapon, 8)) return;
        float perSecond = weaponRarities[weapon] == 2 ? 0.08f : 0.06f;
        float resonance = HasWoundResonance(weapon) ? 1.4f : 1f;
        if (enemy.WoundTime <= 0) enemy.WoundTickCooldown = 1f;
        enemy.WoundTime = 3f;
        enemy.WoundDamage = damage * perSecond * resonance * OutgoingDamageMultiplier(enemy) * (IsArmored(enemy) ? 0.75f : 1f);
        AddFloatingText(new PointF(enemy.Pos.X, enemy.Pos.Y - enemy.Radius - 26), "RUPTURA", Color.Salmon);
    }

    private void TickEnemyWounds(float dt)
    {
        foreach (Enemy enemy in enemies.ToArray())
        {
            if (enemy.WoundTime <= 0) continue;
            enemy.WoundTickCooldown -= dt;
            if (enemy.WoundTickCooldown <= 0)
            {
                enemy.WoundTickCooldown += 1f;
                DamageEnemy(enemy, enemy.WoundDamage);
                if (enemy.Hp <= 0) { Defeat(enemy); continue; }
            }
            enemy.WoundTime = Math.Max(0, enemy.WoundTime - dt);
            if (enemy.WoundTime <= 0) { enemy.WoundDamage = 0; enemy.WoundTickCooldown = 0; }
        }
    }

    private void HitPlayer(float value)
    {
        GameAudio.Play(GameAudio.Cue.Impact);
        float vigilPressure = vigilActive ? 1 + Math.Min(1.5f, Math.Max(0, vigilFloor - 1) * 0.08f) : 1;
        float aspectPressure = vigilActive && RunVigilAspect(vigilFloor) == VigilAspect.Hungry ? 1.3f : 1;
        if (vigilActive && HasCharm(7)) aspectPressure *= 1.1f;
        float campaignPressure = vigilActive ? 1f : DifficultyIncomingDamageMultiplier(difficultyTier);
        float damageTaken = value * vigilPressure * aspectPressure * campaignPressure * HeroDamageTakenMultiplier() * ArmorDamageTakenMultiplier() * (wardTime > 0 ? AnchorWardDamageTakenMultiplier() : 1) * (stoneBastionTime > 0 ? 0.45f : 1f) * WeaponDamageTakenMultiplier() * CharmDamageTakenMultiplier() * CombatMasteryDamageTakenMultiplier() * CombatApexDamageTakenMultiplier();
        health = Math.Max(0, health - damageTaken); invulnerable = 0.6f;
        if (heroClass == HeroClass.StoneGuardian && stoneFury < 100)
        {
            stoneFury = Math.Min(100, stoneFury + damageTaken * 2.25f);
            if (stoneFury >= 100) AddFloatingText(new PointF(player.X, player.Y - 46), "FÚRIA PRONTA • ATAQUE BÁSICO", Color.FromArgb(255, 203, 116));
        }
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
    private static EliteAffix EliteAffixForRoll(int roll) => roll switch { 0 => EliteAffix.Armored, 1 => EliteAffix.Swift, 2 => EliteAffix.Bloodbound, 3 => EliteAffix.Spearbound, 5 => EliteAffix.Conductor, _ => EliteAffix.Summoner };
    private static EliteAffix EliteSecondaryAffixForRoll(int roll, EliteAffix primary)
    {
        EliteAffix[] options = { EliteAffix.Armored, EliteAffix.Swift, EliteAffix.Bloodbound };
        EliteAffix[] eligible = options.Where(affix => affix != primary && !(primary == EliteAffix.Spearbound && affix == EliteAffix.Swift)).ToArray();
        return eligible[Math.Abs(roll % eligible.Length)];
    }
    private static bool HasEliteAffix(Enemy enemy, EliteAffix affix) => enemy.Kind == EnemyKind.Elite && (enemy.EliteAffix == affix || enemy.EliteAffix2 == affix);
    private static string EliteAffixName(EliteAffix affix) => affix switch { EliteAffix.Armored => "BLINDADO", EliteAffix.Swift => "VELOZ", EliteAffix.Bloodbound => "SANGRADOR", EliteAffix.Spearbound => "LANÇADOR", EliteAffix.Summoner => "INVOCADOR", EliteAffix.Conductor => "CONDUTOR", _ => "ELITE" };
    private static string EliteAffixName(Enemy enemy) => enemy.EliteAffix2 is EliteAffix second ? $"{EliteAffixName(enemy.EliteAffix)} + {EliteAffixName(second)}" : EliteAffixName(enemy.EliteAffix);
    private static Color EliteAffixColor(EliteAffix affix) => affix switch { EliteAffix.Armored => Color.LightSteelBlue, EliteAffix.Swift => Color.Gold, EliteAffix.Bloodbound => Color.HotPink, EliteAffix.Spearbound => Color.DeepSkyBlue, EliteAffix.Summoner => Color.MediumOrchid, EliteAffix.Conductor => Color.MediumAquamarine, _ => Color.IndianRed };
    private void UsePotion() { if (potions > 0 && health < maxHealth) { int healing = 45 + (relicOn && relicTrait == 2 ? 15 : 0); potions--; health = Math.Min(maxHealth, health + healing); notice = $"+{healing} VIDA"; noticeTime = 1.3f; } else { notice = potions == 0 ? "SEM POÇÕES" : "VIDA JÁ ESTÁ CHEIA"; noticeTime = 1.3f; } }
    private void Interact()
    {
        if (room == 3) { InteractTown(); return; }
        if (room == 18) { InteractRefuge(); return; }
        Drop? weaponDrop = drops.Where(drop => drop.Kind == "weapon" && Distance(player, drop.Pos) <= 75).OrderBy(drop => Distance(player, drop.Pos)).FirstOrDefault();
        if (weaponDrop is not null) { GameAudio.Play(GameAudio.Cue.Loot); Collect(weaponDrop); return; }
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
        else if (room == 41 && player.X < 115) { room = 18; SetupRoom(); }
        else if (room == 41 && player.X > 790 && enemies.Count == 0 && drops.Count == 0) { room = 42; SetupRoom(); ShowMemory("POÇO DO CONTRATEMPO", "A oficina desce até um poço onde ondas de som se movem fora do ritmo. O Afinador do Contratempo reúne as pausas e alterna uma onda anular com centro seguro, uma marca sob seus passos e dois cortes cruzados; a segunda fase acelera os avisos."); }
        else if (room == 42 && player.X < 115) { room = 41; SetupRoom(); }
        else if (room == 42 && player.X > 790 && questStage >= 58) { room = 18; SetupRoom(); ShowMemory("REGRESSO AO REFÚGIO", "O sino permanece sem badalo. As pausas voltaram a pertencer a quem escolheu deixá-las, sem nome, rota ou notícia de Nara."); }
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
            else if (questStage == 58 && unstruckTrace) { questStage = 59; gold += 2200; tideGlass += 10; potions += 3; techniqueTalent++; ShowMemory("AS PAUSAS VOLTAM A SER ESCOLHIDAS", "Aven guarda o relato sem registrar quem deixou o sino ou quem escolheu não tocá-lo. A comunidade oferece provisões e reforça sua sintonia com as técnicas. O desenho fica na mesa como convite, não como ordem para preencher o silêncio."); notice = "RECOMPENSA: +2200 OURO • +10 SAL • +3 POÇÕES • TÉCNICA +1"; noticeTime = 4; }
            else if (finalEnding > 0 && questStage >= 56) { forgeMenuOpen = true; secondaryForgeOpen = false; return; }
            else if (finalEnding > 0) ShowMemory(finalEnding == 1 ? "VÉSPERA APRENDE A OUVIR" : "O FAROL SEM CORRENTE", finalEnding == 1 ? "Os arquivos estão abertos e a comunidade divide a proteção da cidade. As memórias devolvidas podem ser ouvidas por quem as reivindicar. Nara permanece livre para escolher quando e se quer voltar." : "O Lume protege Véspera sem chamar as vozes perdidas à força. A comunidade sabe o preço da luz e pode decidir, em conjunto, como mantê-la. Nara permanece livre para escolher quando e se quer voltar.");
            else if (questStage >= 27) { forgeMenuOpen = true; secondaryForgeOpen = false; return; }
            else if (questStage >= 24) ShowMemory("AVEN, O BARQUEIRO", "A mesa continua aberta a quem escolhe deixar um relato. A próxima curva fica a oeste; os viajantes que passaram por aqui decidiram compartilhar apenas o início da trilha.");
            else ShowMemory("AVEN, O BARQUEIRO", "Este barco encalhado virou abrigo. Ninguém precisa escrever um nome para ficar aqui.");
            return;
        }
        if (Distance(player, SeraPosition) < InteractionRadius) { ShowMemory("SERA, REPARADORA DE LANTERNAS", finalEnding == 1 ? "Agora que as memórias foram devolvidas, vou cuidar das lanternas junto com quem quiser ajudar. Nenhuma chama precisa guardar tudo sozinha." : finalEnding == 2 ? "O farol ainda ilumina o rio, mas não chama ninguém de volta. Vou manter uma lanterna baixa para quem chegar por vontade própria." : "As lanternas não marcam uma rota para tomar posse dela. Cada pessoa acende uma só quando quer deixar um aviso para quem vier depois."); return; }
        if (Distance(player, NoaPosition) < InteractionRadius) { ShowMemory("NOA, APRENDIZ DE CARTÓGRAFA", finalEnding > 0 ? "Deixei um espaço em branco no mapa para as viagens que ainda não começaram. Aven diz que um caminho pode existir sem pertencer a quem o desenha." : "Aprendi a desenhar os trechos que meus amigos me deixam ver. O resto do mapa fica em branco por escolha deles."); return; }
        if (player.X < 150) { room = 3; SetupRoom(); return; }
        if (player.X > 790 && questStage >= 56) { questStage = Math.Max(57, questStage); room = 41; SetupRoom(); ShowMemory("CÂMARA DAS BADALADAS NÃO ENVIADAS", "A trilha termina numa oficina onde sinos sem badalo foram deixados para não soar. As marcas descrevem pausas escolhidas, não nomes nem rotas. Ecos tentam fechar o caminho até o poço do contratempo."); return; }
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
    private float WhirlwindMultiplier() => HasCharmPair(1, 2) ? 2.52f : HasCharm(1) ? 2.16f : 1.6f;
    private float WhirlwindCooldownPenalty() => !HasCharm(1) ? 0f : HasCharmPair(1, 2) ? 0.5f : 1f;
    private float VoiceShardDamage() => damage * (HasCharmPair(3, 5) ? 2.05f : HasCharm(3) ? 1.8f : 1.35f) * SkillDamageVariant(1) * WeaponSkillMultiplier() * TechniqueDamageMultiplier();
    private float VoiceShardLifetime() => (HasCharm(5) ? 1.8f : 1.25f) * (skillVariants[1] == 2 ? 1.35f : 1f);
    private float VoiceShardCooldown() => (Math.Max(3.5f, 7f - skillTalent * 0.5f) + (HasCharm(5) ? HasCharmPair(3, 5) ? 0.5f : 1.25f : 0f)) * (skillVariants[1] == 1 ? 1.2f : skillVariants[1] == 2 ? 1.1f : 1f) * WeaponCooldownMultiplier();
    private float WeaponCooldownMultiplier() => (HasWeaponAffix(weapon, 4) ? weaponRarities[weapon] == 2 ? 0.84f : 0.9f : 1f) * (HasCharm(9) ? 0.86f : 1f) * (HasCharm(12) ? 1.15f : 1f);
    private float WeaponDamageTakenMultiplier() => HasWeaponAffix(weapon, 5) ? weaponRarities[weapon] == 2 ? 0.88f : 0.92f : 1f;
    private float WeaponReachMultiplier() => HasWeaponAffix(weapon, 6) ? weaponRarities[weapon] == 2 ? 1.2f : 1.12f : 1f;
    private float OutgoingDamageMultiplier(Enemy enemy)
    {
        float bossHunter = !HasCharm(4) ? 1f : enemy.Boss ? 1.25f : 0.8f;
        float combatFocusMultiplier = combatFocus == 1 ? enemy.Boss ? 1.15f : 1f : combatFocus == 2 && !enemy.Boss && enemies.Count >= 3 ? 1.1f : 1f;
        return bossHunter * combatFocusMultiplier * (vigilActive && HasCharm(7) ? 1.4f : 1f) * (HasCharm(10) ? 0.9f : 1f) * VeinResonanceMultiplier();
    }
    private float CharmDamageTakenMultiplier() => (HasCharm(9) ? 1.1f : 1f) * (HasCharm(10) ? 0.86f : 1f);
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
    private float DashCooldown() => Math.Max(0.35f, 1.15f - dodgeTalent * 0.12f + (combatMastery == 1 ? 0.2f : combatMastery == 2 ? -0.18f : 0f) - (armorOn && armorTrait == 2 ? 0.12f : 0f)) + (HasCharm(2) && !HasCharmPair(2, 3) ? 0.3f : 0f);
    private float CombatMasteryDamageTakenMultiplier() => combatMastery == 1 ? 0.9f : combatMastery == 2 ? 1.08f : 1f;
    private float CombatApexDamageTakenMultiplier() => combatApex == 2 && TechniquesOnCooldown() >= 2 ? 0.88f : 1f;
    private float CriticalChance() => (weapon == 2 ? 0.25f : 0.12f) + (HasCharm(3) ? HasCharmPair(1, 3) ? 0.17f : 0.12f : 0f) + (HasWeaponAffix(weapon, 2) ? weaponRarities[weapon] == 2 ? 0.11f : 0.07f : 0f);

    private float CriticalMultiplier() => HasCharm(3) ? HasCharmPair(1, 3) ? 2.75f : 2.5f : 2f;
    private float BasicAttackCooldown() => ((weapon == 1 ? 0.52f : weapon == 2 ? 0.23f : weapon == 3 ? 0.43f : 0.33f) + (HasCharm(3) ? HasCharmPair(1, 3) ? 0.03f : 0.06f : 0f)) * (heroClass == HeroClass.StoneGuardian ? 1.18f : heroClass == HeroClass.NameWeaver ? 0.94f : 1f);
    private float BasicAttackReach(Enemy enemy) => (weapon == 1 ? (enemy.Boss ? 122f : 108f) : weapon == 2 ? (enemy.Boss ? 80f : 68f) : weapon == 3 ? (enemy.Boss ? 148f : 128f) : (enemy.Boss ? 95f : 78f)) * WeaponReachMultiplier();
    private static float BasicAttackSweepAngle(int weapon) => weapon == 1 ? 142 : weapon == 2 ? 112 : weapon == 3 ? 58 : 128;
    private static float BasicAttackArcThreshold(int weapon) => MathF.Cos(BasicAttackSweepAngle(weapon) * MathF.PI / 360f);
    private static bool BasicAttackCoversDirection(float facingDot, int weapon) => facingDot > BasicAttackArcThreshold(weapon);
    private bool IsArmored(Enemy enemy) => enemy.Armored || (!enemy.Boss && enemy.Kind != EnemyKind.Elite && enemies.Any(elite => HasEliteAffix(elite, EliteAffix.Armored) && Distance(enemy.Pos, elite.Pos) <= 132));
    private float BasicAttackDamageMultiplier(Enemy enemy) => (IsArmored(enemy) ? weapon == 1 ? 0.9f : weapon == 3 ? 1f : 0.75f : 1f) * BasicAttackCycleMultiplier();
    private PointF MoveEnemy(Enemy e, PointF delta) { PointF p = e.Pos; PointF x = new(p.X + delta.X, p.Y), y = new(p.X, p.Y + delta.Y); if (!Blocked(x, e.Radius)) p.X = x.X; if (!Blocked(new PointF(p.X, y.Y), e.Radius)) p.Y = y.Y; return new PointF(Math.Clamp(p.X, 85, 875), Math.Clamp(p.Y, 128, 533)); }
    private static float MeleeChaseSpeed(int room, bool swiftElite = false) => Math.Min(168f, 74f + Math.Max(0, room) * 4f) * (swiftElite ? 1.35f : 1f);
    private void SeparateFromPack(Enemy enemy, float dt) { PointF push = PointF.Empty; foreach (Enemy other in enemies) { if (ReferenceEquals(enemy, other)) continue; PointF away = Sub(enemy.Pos, other.Pos); float d = Length(away); if (d > 0.01f && d < 40) push = Add(push, Scale(away, (40 - d) / (40 * d))); } if (Length(push) > 0.01f) enemy.Pos = MoveEnemy(enemy, Scale(Normalize(push, PointF.Empty), 52 * dt)); }
    private void MovePlayer(PointF delta) { PointF x = new(player.X + delta.X, player.Y), y = new(player.X, player.Y + delta.Y); if (!Blocked(x)) player.X = x.X; if (!Blocked(new PointF(player.X, y.Y))) player.Y = y.Y; player.X = Math.Clamp(player.X, 85, 875); player.Y = Math.Clamp(player.Y, 128, 533); }
    private RectangleF[] CurrentRoomWalls() => room switch
    {
        3 => TownBuildings, 4 => ArchiveWalls, 5 => VaultWalls, 6 => RiverWalls, 7 => BellChamberWalls, 8 => AshGalleryWalls, 9 => LumeChamberWalls, 10 => NameShoreWalls, 11 => VoiceWellWalls, 12 => FarReachWalls, 13 => NameCloisterWalls, 14 => FirstBasinWalls, 15 => FirstVigilWalls, 16 => WestFordWalls, 17 => WitnessYardWalls,
        18 => RefugeWalls, 19 => RidgeWalls, 20 => LedgerWalls, 21 => SaltMarshWalls, 22 => TideBridgeWalls, 23 => ReedRunWalls, 24 => ReedYardWalls, 25 => FloodMillWalls, 26 => SiltChamberWalls, 27 => CounterSlopeWalls, 28 => CounterHeartWalls, 29 => CantoriaWalls, 30 => RiftChamberWalls, 31 => GlassGorgeWalls, 32 => MirrorSourceWalls, 33 => LanternSpanWalls, 34 => LowTideObservatoryWalls, 35 => OpenRouteWalls, 36 => UnwrittenHallWalls, 37 => SilenceThresholdWalls, 38 => SilenceChamberWalls, 39 => BorrowedDockWalls, 40 => BorrowedArchiveWalls, 41 => UnsentChamberWalls, 42 => OffbeatWellWalls,
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
        g.DrawString($"M{(alternateSkillsActive ? 2 : 1)}  •  F  {ShortSkillName(0)}  •  V  {ShortSkillName(1)}  •  G  {ShortSkillName(2)}  •  C  {ShortSkillName(3)}", helpFont, goldBrush, 255, 61);
        g.DrawString($"RESSONÂNCIA {difficultyTier} • {DifficultyTierName(difficultyTier)}", helpFont, Brushes.Orange, 690, 61);
        if (armorOn && relicOn)
        {
            using var setBadge = new SolidBrush(Color.FromArgb(210, 31, 52, 53));
            using var setText = new SolidBrush(Color.FromArgb(236, 174, 215, 202));
            using var setFont = new Font("Segoe UI", 9, FontStyle.Bold);
            g.FillRectangle(setBadge, 604, 85, 291, 19);
            g.DrawString(veinResonanceTime > 0 ? $"VIGÍLIA  •  -35%  •  PULSO +15% ({veinResonanceTime:0.0} s)" : "VIGÍLIA DO VEIO  •  -35% DANO  •  CURA 8/ABATE", setFont, setText, 610, 87);
        }
        using var healthBack = new SolidBrush(Color.FromArgb(52, 56, 51)); using var healthBar = new SolidBrush(health / maxHealth < .25f ? Color.IndianRed : Color.FromArgb(112, 171, 123));
        g.FillRectangle(healthBack, 25, 58, 190, 11); g.FillRectangle(healthBar, 25, 58, 190 * health / maxHealth, 11);
        if (heroClass == HeroClass.StoneGuardian)
        {
            using var furyBack = new SolidBrush(Color.FromArgb(52, 48, 40)); using var furyBar = new SolidBrush(stoneFury >= 100 ? Color.Gold : Color.FromArgb(206, 128, 71)); using var furyLabel = new SolidBrush(Color.FromArgb(226, 214, 190)); using var furyFont = new Font("Segoe UI", 8, FontStyle.Bold);
            g.DrawString(stoneFury >= 100 ? "FÚRIA DE PEDRA • PRONTA" : $"FÚRIA DE PEDRA • {stoneFury:0}%", furyFont, furyLabel, 25, 69);
            g.FillRectangle(furyBack, 25, 80, 190, 3); g.FillRectangle(furyBar, 25, 80, 190 * Math.Clamp(stoneFury / 100f, 0, 1), 3);
        }
        else if (heroClass == HeroClass.NameWeaver)
        {
            using var threadBack = new SolidBrush(Color.FromArgb(52, 48, 40)); using var threadBar = new SolidBrush(nameThread >= 40 ? Color.FromArgb(210, 150, 226) : Color.FromArgb(135, 104, 164)); using var threadLabel = new SolidBrush(Color.FromArgb(226, 214, 190)); using var threadFont = new Font("Segoe UI", 8, FontStyle.Bold);
            g.DrawString($"FIO DE NOMES • {nameThread:0}%{(nameThread >= 40 ? " • SUTURA PRONTA" : "")}", threadFont, threadLabel, 25, 69);
            g.FillRectangle(threadBack, 25, 80, 190, 3); g.FillRectangle(threadBar, 25, 80, 190 * Math.Clamp(nameThread / 100f, 0, 1), 3);
        }
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
        else if (room is 41 or 42) DrawUnsentChimeEnvironment(g, floor, tile);
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
        DrawGroundHazards(g);
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
        string roomTitle = vigilActive ? $"VIGÍLIA ASCENDENTE • ANDAR {vigilFloor}" : room == 3 || room == 18 ? roomNames[room] : room < 3 ? $"{room + 1} / 3   {roomNames[room]}" : room < 6 ? $"{room - 3} / 2   {roomNames[room]}" : room < 8 ? $"{room - 5} / 2   {roomNames[room]}" : room < 10 ? $"{room - 7} / 2   {roomNames[room]}" : room < 12 ? $"{room - 9} / 2   {roomNames[room]}" : room < 14 ? $"{room - 11} / 2   {roomNames[room]}" : room < 16 ? $"{room - 13} / 2   {roomNames[room]}" : room < 18 ? $"{room - 15} / 2   {roomNames[room]}" : room < 21 ? $"{room - 18} / 2   {roomNames[room]}" : room < 23 ? $"{room - 20} / 2   {roomNames[room]}" : room < 25 ? $"{room - 22} / 2   {roomNames[room]}" : room < 27 ? $"{room - 24} / 2   {roomNames[room]}" : room < 29 ? $"{room - 26} / 2   {roomNames[room]}" : room < 31 ? $"{room - 28} / 2   {roomNames[room]}" : room < 33 ? $"{room - 30} / 2   {roomNames[room]}" : room < 35 ? $"{room - 32} / 2   {roomNames[room]}" : room < 37 ? $"{room - 34} / 2   {roomNames[room]}" : room < 39 ? $"{room - 36} / 2   {roomNames[room]}" : room < 41 ? $"{room - 38} / 2   {roomNames[room]}" : $"{room - 40} / 2   {roomNames[room]}";
        g.DrawString(roomTitle, roomFont, goldBrush, 67, 572);
        int memoriesRead = (memoriesFound & 1) + ((memoriesFound >> 1) & 1);
        g.DrawString($"ECOS {memoriesRead} / 2", hudFont, goldBrush, 760, 572);
        string currentObjective = CurrentObjective();
        if (currentObjective.Length > 0) g.DrawString(currentObjective, hudFont, goldBrush, 67, 592);
        g.DrawString(room is 3 or 18 ? "WASD mover   E falar/investigar   F5 salvar   B técnicas   I inventário   L diário" : "WASD mover   ESPAÇO atacar   SHIFT esquivar   R redemoinho   F/V/G/C técnicas   B montar técnicas   Q poção   E interagir   I inventário   L diário", helpFont, textBrush, 67, 619);
        if (room < MemoryStonePositions.Length && (memoriesFound & (1 << room)) == 0) DrawMemoryStone(g, MemoryStonePositions[room], Distance(player, MemoryStonePositions[room]) < MemoryStoneInteractionRadius);
        if (noticeTime > 0) DrawCentered(g, notice, 170, new Font("Segoe UI", 13, FontStyle.Bold), Color.FromArgb(230, 193, 129));
        if (vigilChoiceOpen) DrawChoice(g, $"VIGÍLIA ASCENDENTE • ANDAR {vigilFloor}", $"1. SUBIR AO ANDAR {vigilFloor + 1} • RECOMPENSA CRESCENTE", $"2. ENCERRAR • MELHOR MARCA: {Math.Max(vigilBestFloor, vigilFloor)}", "3. FICAR AQUI");
        if (title) DrawTitleScreen(g);
        else if (dead || victory) DrawOverlay(g, victory ? "VITÓRIA" : "VOCÊ CAIU", "ENTER PARA RECOMEÇAR    •    F5 SALVAR");
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
        if (journalOpen) DrawQuestJournal(g);
        if (focusPaused && !title && !dead && !victory) DrawOverlay(g, "PAUSADO", "VOLTE À JANELA PARA CONTINUAR");
    }

    private string CurrentObjective()
    {
        if (room == 3) return TownObjective();
        if (room == 18) return "OBJETIVO: " + (questStage == 46 ? "FALE COM AVEN SOBRE AS LINHAS" : questStage == 47 ? "SIGA AS LINHAS ATÉ A GALERIA" : questStage == 48 ? "LEVE O TRAÇADO DE VOLTA A AVEN" : questStage == 49 ? "FALE COM AVEN PARA ABRIR O ÚLTIMO CAPÍTULO" : questStage == 50 ? "DESÇA À SOLEIRA DO SILÊNCIO" : questStage == 51 ? "FALE COM AVEN SOBRE O DESTINO DO LUME" : questStage >= 59 ? "EXPEDIÇÃO XVIII CONCLUÍDA • EXPLORAÇÃO ABERTA" : questStage == 58 ? "LEVE A BADALADA RECUPERADA A AVEN" : questStage == 57 ? "SIGA À CÂMARA DAS BADALADAS" : questStage == 56 ? "EXPEDIÇÃO XVII CONCLUÍDA • NOVA EXPEDIÇÃO DISPONÍVEL A LESTE" : questStage >= 56 ? "EXPEDIÇÃO XVII CONCLUÍDA • EXPLORAÇÃO ABERTA" : questStage == 55 ? "LEVE A MEMÓRIA DO ARQUIVO A AVEN" : questStage == 54 ? "SIGA AO CAIS DOS ECOS EMPRESTADOS" : questStage >= 52 ? "EPÍLOGO CONCLUÍDO • EXPLORAÇÃO ABERTA" : questStage < 22 ? "CONHEÇA A COMUNIDADE E FALE COM AVEN" : questStage == 22 ? "SIGA A TRILHA E RECUPERE O REGISTRO COMPARTILHADO" : questStage == 23 ? "LEVE O REGISTRO DE VOLTA A AVEN" : questStage == 32 ? "LEVE A MEMÓRIA DA VAZANTE A AVEN" : questStage == 30 ? "SIGA O SOM DOS MOINHOS PARA OESTE" : questStage == 35 ? "LEVE A MEMÓRIA DO COMPASSO A AVEN" : questStage == 39 ? "LEVE A NOTA DA CONTRAVOZ A AVEN" : questStage == 40 ? "FALE COM AVEN SOBRE A PRÓXIMA FISSURA" : questStage == 42 ? "LEVE O ECO SEM DONO A AVEN" : questStage >= 41 ? "SIGA OS PASSOS ATÉ A FONTE SOB A PEDRA" : questStage >= 36 ? "MANTO DE LASTRO APRENDIDO • SIGA AS ÁGUAS" : questStage >= 33 ? "SIGA A CORRENTE AO CORAÇÃO DAS DUAS MARGENS" : questStage >= 27 ? "FALE COM AVEN PARA REFORJAR UMA ARMA" : "SIGA A PRÓXIMA CURVA A OESTE");
        if (room is 27 or 28) return room == 27 ? "OBJETIVO: ATRAVESSE A ESCARPA DO CONTRAFLUXO" : questStage < 35 ? "OBJETIVO: DERROTE O AFINADOR E RECOLHA A MEMÓRIA" : "OBJETIVO: RETORNE AO REFÚGIO E FALE COM AVEN";
        if (room is 29 or 30) return room == 29 ? (enemies.Count > 0 || drops.Count > 0 ? "OBJETIVO: ABRA A CANTARIA E LIMPE OS ECOS" : "OBJETIVO: SIGA A CONTRAVOZ ATÉ A CÂMARA") : riftVerse ? "OBJETIVO: LEVE A NOTA RECUPERADA A AVEN" : "OBJETIVO: INTERROMPA A CONTRAVOZ E RECOLHA A NOTA";
        if (room is 33 or 34) return LanternObjective();
        if (room is 35 or 36) return room == 35 ? (enemies.Count > 0 || drops.Count > 0 ? "OBJETIVO: ATRAVESSE AS LINHAS ABERTAS" : "OBJETIVO: SIGA ATÉ A GALERIA") : routeMemory ? "OBJETIVO: LEVE O TRAÇADO A AVEN" : "OBJETIVO: DERROTE O ESCRIBA E RECUPERE O TRAÇADO";
        if (room is 41 or 42) return room == 41 ? enemies.Count > 0 || drops.Count > 0 ? "OBJETIVO: ATRAVESSE A CÂMARA DOS SONS PAUSADOS" : "OBJETIVO: DESÇA AO POÇO DO CONTRATEMPO" : unstruckTrace ? "OBJETIVO: RETORNE A AVEN COM A LEMBRANÇA" : "OBJETIVO: INTERROMPA O AFINADOR E RECUPERE A BADALADA";
        if (room is 39 or 40) return room == 39 ? enemies.Count > 0 || drops.Count > 0 ? "OBJETIVO: ATRAVESSE O CAIS DOS ECOS" : "OBJETIVO: ENTRE NO ARQUIVO DA MARÉ" : borrowedTrace ? "OBJETIVO: RETORNE A AVEN COM A MEMÓRIA" : "OBJETIVO: DERROTE O ARQUIVISTA E RECOLHA O ECO";
        if (room is 37 or 38) return vigilActive ? enemies.Count > 0 ? $"VIGÍLIA {vigilFloor}: DERROTE O ECO E RECOLHA O SAQUE" : drops.Count > 0 ? $"VIGÍLIA {vigilFloor}: RECOLHA O SAQUE" : $"VIGÍLIA {vigilFloor}: INTERAJA COM O ALTAR PARA SUBIR OU ENCERRAR" : room == 37 ? enemies.Count > 0 || drops.Count > 0 ? "OBJETIVO: ATRAVESSE A SOLEIRA DO SILÊNCIO" : "OBJETIVO: DESÇA À CÂMARA DO ÚLTIMO ECO" : silenceMemory ? "OBJETIVO: RETORNE A AVEN E DECIDA O DESTINO DO LUME" : "OBJETIVO: DERROTE O SILÊNCIO E RECOLHA A MEMÓRIA";
        if (room is 31 or 32) return room == 31 ? (enemies.Count > 0 || drops.Count > 0 ? "OBJETIVO: ATRAVESSE A GARGANTA DO VIDRO MUDO" : "OBJETIVO: SIGA OS PASSOS ATÉ A FONTE") : deepTrace ? "OBJETIVO: LEVE O ECO SEM DONO A AVEN" : "OBJETIVO: DERROTE O ARAUTO E RECOLHA A LEMBRANÇA";
        if (room is >= 4 and < 6) return ArchiveObjective();
        if (room is 6 or 7) return RiverObjective();
        if (room >= 8) return LumeObjective();
        return "";
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
        if (questStage >= 22 && player.X > 790) DrawCentered(g, questStage >= 57 ? "E  OUVIR AS BADALADAS" : questStage >= 54 ? "E  OUVIR O ARQUIVO DAS MARÉS" : questStage >= 50 ? "E  DESCER AO SILÊNCIO" : questStage >= 47 ? "E  SEGUIR AS LINHAS" : questStage >= 41 ? "E  OUVIR O VIDRO" : questStage >= 30 ? "E  SEGUIR AS ÁGUAS" : "E  SEGUIR AS LANTERNAS", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
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
        Color ringColor = vigilActive ? VigilAspectColor(RunVigilAspect(vigilFloor)) : Color.FromArgb(173, 193, 194);
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
                if (RunVigilAspect(vigilFloor) == VigilAspect.Chorus)
                {
                    using var driftingEcho = new Pen(Color.FromArgb(135, 194, 133, 222), 3);
                    for (int i = 0; i < 3; i++) { float angle = windupPulse * 0.42f + i * MathF.PI * 2 / 3; float x = 480 + MathF.Cos(angle) * 222, y = 330 + MathF.Sin(angle) * 148; g.DrawEllipse(driftingEcho, x - 9, y - 9, 18, 18); }
                }
                if (RunVigilAspect(vigilFloor) == VigilAspect.Counterflow)
                {
                    bool pulling = VigilTidePulling(vigilTideTime);
                    using var current = new Pen(Color.FromArgb(185, VigilAspectColor(RunVigilAspect(vigilFloor))), 3) { EndCap = LineCap.Triangle };
                    for (int i = 0; i < 8; i++)
                    {
                        float angle = i * MathF.PI / 4 + windupPulse * 0.04f;
                        PointF inner = new(480 + MathF.Cos(angle) * 112, 330 + MathF.Sin(angle) * 94);
                        PointF outer = new(480 + MathF.Cos(angle) * 184, 330 + MathF.Sin(angle) * 154);
                        g.DrawLine(current, pulling ? outer : inner, pulling ? inner : outer);
                    }
                    float changeIn = 6f - vigilTideTime % 6f;
                    string flow = pulling ? "PUXANDO PARA O CENTRO" : "REPELINDO PARA A BORDA";
                    Color flowColor = changeIn <= 1f && ((int)(windupPulse * 5) % 2 == 0) ? Color.PaleGoldenrod : VigilAspectColor(RunVigilAspect(vigilFloor));
                    DrawCentered(g, $"CORRENTE: {flow} • MUDA EM {changeIn:0.0} S", 530, new Font("Segoe UI", 9, FontStyle.Bold), flowColor);
                }
                DrawCentered(g, enemies.Count == 0 && drops.Count == 0 ? "E  SUBIR OU ENCERRAR A VIGÍLIA" : $"ECO DA VIGÍLIA • ANDAR {vigilFloor}", 494, new Font("Segoe UI", 9, FontStyle.Bold), Color.FromArgb(222, 202, 226));
                DrawCentered(g, $"{VigilAspectTitle(RunVigilAspect(vigilFloor))} • {VigilAspectRule(RunVigilAspect(vigilFloor)).ToUpperInvariant()}", 511, new Font("Segoe UI", 9, FontStyle.Bold), VigilAspectColor(RunVigilAspect(vigilFloor)));
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

    private void DrawUnsentChimeEnvironment(Graphics g, Brush ground, Pen tile)
    {
        using var deep = new SolidBrush(Color.FromArgb(20, 18, 23)); using var stone = new SolidBrush(Color.FromArgb(67, 57, 49));
        using var bronze = new Pen(Color.FromArgb(191, 157, 103), 3); using var pale = new Pen(Color.FromArgb(126, 211, 196, 142), 2);
        g.FillRectangle(deep, 65, 108, 830, 445); g.FillRectangle(ground, 94, 132, 772, 397);
        for (int y = 0; y < 7; y++) for (int x = 0; x < 12; x++) g.DrawRectangle(tile, 70 + x * 68, 112 + y * 62, 67, 61);
        RectangleF[] walls = room == 41 ? UnsentChamberWalls : OffbeatWellWalls;
        foreach (RectangleF wall in walls) { g.FillRectangle(stone, wall); g.DrawRectangle(bronze, wall.X, wall.Y, wall.Width, wall.Height); }
        if (room == 41)
        {
            for (int i = 0; i < 7; i++) { float x = 180 + i * 95, length = 42 + (i % 3) * 18; g.DrawLine(bronze, x, 142, x, 142 + length); g.DrawArc(bronze, x - 15, 142 + length - 8, 30, 26, 0, 180); g.DrawLine(pale, x - 8, 144 + length, x + 8, 144 + length); }
            g.DrawEllipse(pale, 387, 263, 184, 118); g.DrawEllipse(bronze, 417, 282, 124, 80);
        }
        else
        {
            g.FillEllipse(stone, 316, 166, 328, 328); g.DrawEllipse(bronze, 316, 166, 328, 328);
            for (int i = 0; i < 5; i++) { int inset = i * 22; g.DrawEllipse(i % 2 == 0 ? bronze : pale, 344 + inset, 194 + inset, 272 - inset * 2, 272 - inset * 2); }
            g.DrawLine(pale, 480, 205, 480, 455); g.DrawLine(pale, 355, 330, 605, 330);
        }
        using var gate = new Pen(enemies.Count == 0 && drops.Count == 0 ? Color.LightGreen : Color.FromArgb(73, 82, 79), 7);
        g.DrawLine(gate, 885, 290, 885, 370); g.DrawLine(gate, 75, 290, 75, 370);
        if (player.X < 150) DrawCentered(g, room == 41 ? "E  VOLTAR AO REFÚGIO" : "E  SUBIR À CÂMARA", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 146);
        if (room == 41 && player.X > 790 && enemies.Count == 0 && drops.Count == 0) DrawCentered(g, "E  DESCER AO POÇO", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
        if (room == 42 && player.X > 790 && questStage >= 58) DrawCentered(g, "E  VOLTAR A AVEN", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
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
        float r = enemy.Radius; Color color = enemy.Kind == EnemyKind.LastTuner ? Color.FromArgb(189, 153, 98) : enemy.Kind == EnemyKind.BlindScribe ? Color.FromArgb(89, 128, 135) : enemy.Kind == EnemyKind.RiverFerryman ? Color.FromArgb(69, 120, 124) : enemy.Kind == EnemyKind.AshWarden ? Color.FromArgb(169, 91, 53) : enemy.Kind == EnemyKind.HollowName ? Color.FromArgb(120, 165, 155) : enemy.Kind == EnemyKind.NameWeaver ? Color.FromArgb(139, 108, 162) : enemy.Kind == EnemyKind.FirstKeeper ? Color.FromArgb(202, 173, 112) : enemy.Kind == EnemyKind.OathCartographer ? Color.FromArgb(96, 148, 144) : enemy.Kind == EnemyKind.NameBroker ? Color.FromArgb(126, 82, 132) : enemy.Kind == EnemyKind.ReedKeeper ? Color.FromArgb(158, 149, 98) : enemy.Kind == EnemyKind.FlowKeeper ? Color.FromArgb(71, 148, 164) : enemy.Kind == EnemyKind.TideWitness ? Color.FromArgb(63, 157, 168) : enemy.Kind == EnemyKind.GlassHerald ? Color.FromArgb(124, 204, 218) : enemy.Kind == EnemyKind.LanternRegent ? Color.FromArgb(194, 195, 144) : enemy.Kind == EnemyKind.RouteScribe ? Color.FromArgb(159, 182, 211) : enemy.Kind == EnemyKind.DeepSilence ? Color.FromArgb(119, 183, 190) : enemy.Kind == EnemyKind.SaltEcho ? Color.LightSteelBlue : enemy.Kind == EnemyKind.PaperWraith ? Color.FromArgb(201, 190, 161) : enemy.Kind == EnemyKind.MudCantor ? Color.FromArgb(165, 135, 104) : enemy.Boss ? Color.FromArgb(145, 87, 77) : enemy.Kind switch { EnemyKind.Elite => enemy.EliteAffix switch { EliteAffix.Armored => Color.SlateGray, EliteAffix.Swift => Color.Goldenrod, EliteAffix.Bloodbound => Color.MediumVioletRed, EliteAffix.Conductor => Color.MediumAquamarine, EliteAffix.Summoner => Color.MediumOrchid, _ => Color.IndianRed }, EnemyKind.Archer => Color.FromArgb(128, 151, 106), EnemyKind.Mage => Color.MediumPurple, EnemyKind.InkEcho => Color.FromArgb(113, 98, 143), EnemyKind.DrownedMemory => Color.FromArgb(95, 145, 142), EnemyKind.EmberWisp => Color.FromArgb(212, 126, 61), EnemyKind.NameMoth => Color.FromArgb(126, 170, 156), _ => Color.FromArgb(183, 181, 166) };
        if (enemy.Kind == EnemyKind.CooldownLeech) color = Color.MediumOrchid;
        if (enemy.Kind == EnemyKind.SlagWeaver) color = Color.FromArgb(189, 123, 75);
        if (enemy.Stagger > 0) { using var staggerPen = new Pen(Color.FromArgb(200, 125, 211, 203), 2); float pulse = 1 + (0.95f - enemy.Stagger) * 7; g.DrawEllipse(staggerPen, enemy.Pos.X - r - pulse, enemy.Pos.Y - r - pulse, (r + pulse) * 2, (r + pulse) * 2); }
        if (enemy.Windup > 0 && enemy.Boss) DrawBossWarning(g, enemy);
        else if (enemy.Windup > 0)
        {
            PointF center = NonBossAttackCenter(enemy); float radius = NonBossAttackRadius(enemy);
            bool spearboundElite = enemy.Kind == EnemyKind.Elite && enemy.EliteAffix == EliteAffix.Spearbound;
            (Color fillColor, Color edgeColor) = spearboundElite && !highContrastTelegraphs
                ? (Color.FromArgb(42, 76, 211, 245), Color.FromArgb(235, 76, 211, 245))
                : AttackWarningPalette(highContrastTelegraphs);
            if (enemy.Kind == EnemyKind.CooldownLeech && !highContrastTelegraphs) { fillColor = Color.FromArgb(52, 185, 104, 220); edgeColor = Color.FromArgb(238, 205, 139, 245); }
            if (enemy.Kind == EnemyKind.SlagWeaver && !highContrastTelegraphs) { fillColor = Color.FromArgb(45, 209, 126, 65); edgeColor = Color.FromArgb(238, 245, 164, 101); }
            float urgency = Math.Clamp(1f - enemy.Windup / Math.Max(0.01f, enemy.WindupMax), 0f, 1f);
            float pulse = (0.5f + 0.5f * MathF.Sin(windupPulse * (7f + urgency * 11f))) * urgency;
            fillColor = Color.FromArgb(Math.Clamp(fillColor.A + (int)(urgency * 24), 0, 255), fillColor);
            edgeColor = Color.FromArgb(Math.Clamp(edgeColor.A + (int)(urgency * 18 + pulse * 35), 0, 255), edgeColor);
            using var p = new Pen(edgeColor, highContrastTelegraphs ? 4 + urgency + pulse : 2.5f + urgency * 2 + pulse * 1.5f); using var b = new SolidBrush(fillColor);
            if (RangedImpactRadius(enemy.Kind) > 0 || enemy.Kind == EnemyKind.Elite && enemy.EliteAffix == EliteAffix.Spearbound)
            {
                if (highContrastTelegraphs) { using var shadow = new Pen(Color.FromArgb(230, 12, 13, 15), 7); g.DrawLine(shadow, enemy.Pos, enemy.Target); }
                g.DrawLine(p, enemy.Pos, enemy.Target);
            }
            g.FillEllipse(b, center.X - radius, center.Y - radius, radius * 2, radius * 2);
            if (highContrastTelegraphs) { using var shadow = new Pen(Color.FromArgb(240, 12, 13, 15), 7); g.DrawEllipse(shadow, center.X - radius, center.Y - radius, radius * 2, radius * 2); }
            g.DrawEllipse(p, center.X - radius, center.Y - radius, radius * 2, radius * 2);
            if (enemy.Kind == EnemyKind.CooldownLeech) DrawCentered(g, "RECARGAS", center.Y - radius - 14, new Font("Segoe UI", 8, FontStyle.Bold), highContrastTelegraphs ? Color.White : Color.Plum, center.X);
            if (enemy.Kind == EnemyKind.SlagWeaver) DrawCentered(g, "SAL VIVO", center.Y - radius - 14, new Font("Segoe UI", 8, FontStyle.Bold), highContrastTelegraphs ? Color.White : Color.FromArgb(255, 191, 126), center.X);
        }
        if (enemy.WoundTime > 0) { int alpha = (int)(110 + 100 * enemy.WoundTime / 3f); using var woundRing = new Pen(Color.FromArgb(alpha, 237, 111, 105), 2.5f); g.DrawEllipse(woundRing, enemy.Pos.X - r - 5, enemy.Pos.Y - r - 5, (r + 5) * 2, (r + 5) * 2); }
        using var body = new SolidBrush(enemy.Flash > 0 ? Color.White : color); using var edge = new Pen(enemy == lockedTarget ? Color.Gold : IsArmored(enemy) ? Color.LightSteelBlue : Color.FromArgb(35, 25, 22), enemy == lockedTarget ? 4 : 2);
        int bossSpriteIndex = CampaignBossSpriteIndex(enemy.Kind), enemySpriteRow = EnemySpriteRow(enemy.Kind);
        Bitmap? individualSprite = enemy.Kind == EnemyKind.Elite && enemy.EliteAffix == EliteAffix.Spearbound ? SpearboundEliteSprite.Value : AdvancedEnemySprites.Value.GetValueOrDefault(enemy.Kind);
        bool individualSpriteLoaded = individualSprite is not null;
        bool bossSprite = bossSpriteIndex >= 0;
        int spriteRow = individualSpriteLoaded ? 0 : bossSprite ? bossSpriteIndex % 4 : enemySpriteRow;
        Bitmap? enemySprites = individualSpriteLoaded ? individualSprite : bossSprite ? bossSpriteIndex switch { < 4 => BossSpriteSheet.Value, < 8 => MidBossSpriteSheet.Value, < 12 => LateBossSpriteSheet.Value, < 16 => FinalWaveBossSpriteSheet.Value, _ => EndgameBossSpriteSheet.Value } : enemySpriteRow >= 0 ? EnemySpriteSheet.Value : null;
        int attackFrame = EnemyAttackSpriteFrame(enemy);
        if (!individualSpriteLoaded && !bossSprite && enemySpriteRow >= 0 && attackFrame >= 0)
            enemySprites = EnemyAttackSpriteSheet.Value ?? enemySprites;
        float spriteHeight = individualSpriteLoaded ? enemy.Kind switch { EnemyKind.Elite => 118, EnemyKind.EmberWisp => 72, EnemyKind.MudCantor => 86, EnemyKind.NameMoth => 84, EnemyKind.PaperWraith => 86, EnemyKind.SaltEcho => 82, _ => 80 } : bossSprite ? 132 : enemy.Kind == EnemyKind.Elite ? 82 : 70;
        if (spriteRow >= 0 && enemySprites is not null)
        {
            float cellWidth = individualSpriteLoaded ? enemySprites.Width : enemySprites.Width / 4f, cellHeight = individualSpriteLoaded ? enemySprites.Height : enemySprites.Height / 4f;
            float windupProgress = enemy.WindupMax > 0 && enemy.Windup > 0 ? Math.Clamp(1f - enemy.Windup / enemy.WindupMax, 0f, 1f) : enemy.AttackPoseTime > 0 ? 1f : 0f;
            int frame = individualSpriteLoaded ? 0 : attackFrame >= 0 ? attackFrame : EnemySpriteFrame(enemy.AnimationTime, enemy.Moving && enemy.Stagger <= 0);
            RectangleF source = new(frame * cellWidth, spriteRow * cellHeight, cellWidth, cellHeight);
            float size = spriteHeight;
            float width = individualSpriteLoaded ? size * cellWidth / cellHeight : size;
            var uniqueAttackPose = individualSpriteLoaded || bossSprite ? EnemyAttackPose(enemy, windupProgress) : (Width: 1f, Height: 1f, OffsetX: 0f, OffsetY: 0f);
            bool freelyMoving = enemy.Moving && enemy.Windup <= 0 && enemy.AttackPoseTime <= 0 && enemy.Stagger <= 0;
            bool animatedCreature = individualSpriteLoaded && enemy.Kind != EnemyKind.Elite;
            float motionPhase = freelyMoving ? enemy.AnimationTime * 15 : windupPulse * 2.2f;
            float bob = animatedCreature ? MathF.Sin(motionPhase) * (freelyMoving ? 2f : 1.1f) : 0;
            float breath = animatedCreature && !freelyMoving && enemy.Windup <= 0 && enemy.Stagger <= 0 ? MathF.Sin(windupPulse * 2.4f) * 0.009f : 0;
            float posedWidth = width * (1f + windupProgress * 0.07f + breath) * uniqueAttackPose.Width, posedHeight = size * (1f - windupProgress * 0.035f + breath) * uniqueAttackPose.Height;
            float lunge = windupProgress * (bossSprite ? 5f : 7f) + uniqueAttackPose.OffsetX;
            RectangleF target = new(enemy.Pos.X - posedWidth / 2 + lunge, enemy.Pos.Y - posedHeight + bob + uniqueAttackPose.OffsetY, posedWidth, posedHeight);
            using var shadow = new SolidBrush(Color.FromArgb(110, 5, 7, 9));
            g.FillEllipse(shadow, enemy.Pos.X - r, enemy.Pos.Y - 5, r * 2, 10);
            var spriteTransform = g.Save();
            float facingX = enemy.Windup > 0 || enemy.AttackPoseTime > 0 ? enemy.Target.X : player.X;
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
            Color affixColor = EliteAffixColor(enemy.EliteAffix);
            DashStyle dash = enemy.EliteAffix is EliteAffix.Swift or EliteAffix.Spearbound ? DashStyle.Dash : enemy.EliteAffix == EliteAffix.Bloodbound ? DashStyle.Dot : DashStyle.Solid;
            using var affixRing = new Pen(Color.FromArgb(225, affixColor), 3) { DashStyle = dash };
            g.DrawEllipse(affixRing, enemy.Pos.X - r - 6, enemy.Pos.Y - 12, (r + 6) * 2, 24);
            if (enemy.EliteAffix2 is EliteAffix secondAffix)
            {
                using var secondaryRing = new Pen(Color.FromArgb(230, EliteAffixColor(secondAffix)), 2.5f) { DashStyle = secondAffix == EliteAffix.Swift ? DashStyle.Dash : secondAffix == EliteAffix.Bloodbound ? DashStyle.Dot : DashStyle.Solid };
                g.DrawEllipse(secondaryRing, enemy.Pos.X - r - 11, enemy.Pos.Y - 17, (r + 11) * 2, 34);
            }
            if (enemy.SummonWindup > 0)
            {
                float progress = 1 - enemy.SummonWindup / 0.95f, radius = 18 + progress * 50;
                using var summonRing = new Pen(Color.FromArgb(220, 192, 112, 220), 3);
                g.DrawEllipse(summonRing, enemy.Pos.X - radius, enemy.Pos.Y - radius * 0.55f, radius * 2, radius * 1.1f);
            }
            if (enemy.SupportWindup > 0)
            {
                float progress = 1 - enemy.SupportWindup / 0.9f;
                using var supportRing = new Pen(Color.FromArgb(215, 127, 255, 198), highContrastTelegraphs ? 4 : 2.5f) { DashStyle = DashStyle.Dash };
                using var supportShadow = new Pen(Color.FromArgb(245, 10, 13, 15), 8);
                foreach (Enemy ally in enemies.Where(ally => ConductorPulseCovers(enemy, ally)))
                {
                    if (highContrastTelegraphs) { g.DrawLine(supportShadow, enemy.Pos, ally.Pos); g.DrawEllipse(supportShadow, ally.Pos.X - ally.Radius - 7, ally.Pos.Y - ally.Radius - 7, (ally.Radius + 7) * 2, (ally.Radius + 7) * 2); }
                    g.DrawLine(supportRing, enemy.Pos, ally.Pos);
                    g.DrawEllipse(supportRing, ally.Pos.X - ally.Radius - 7, ally.Pos.Y - ally.Radius - 7, (ally.Radius + 7) * 2, (ally.Radius + 7) * 2);
                }
                float radius = 24 + progress * 28;
                if (highContrastTelegraphs) g.DrawEllipse(supportShadow, enemy.Pos.X - radius, enemy.Pos.Y - radius * 0.55f, radius * 2, radius * 1.1f);
                g.DrawEllipse(supportRing, enemy.Pos.X - radius, enemy.Pos.Y - radius * 0.55f, radius * 2, radius * 1.1f);
                DrawCentered(g, "CANTANDO • INTERROMPA", enemy.Pos.Y - r - 44, new Font("Segoe UI", 8, FontStyle.Bold), Color.LightGreen, enemy.Pos.X);
            }
        }
        if (enemy.VigilMinion) { using var chorusRing = new Pen(Color.FromArgb(235, 194, 133, 230), 3) { DashStyle = DashStyle.Dot }; g.DrawEllipse(chorusRing, enemy.Pos.X - r - 6, enemy.Pos.Y - 12, (r + 6) * 2, 24); }
        if (enemy.EliteMinion) { using var summonedRing = new Pen(Color.FromArgb(235, 192, 112, 220), 3) { DashStyle = DashStyle.Dash }; g.DrawEllipse(summonedRing, enemy.Pos.X - r - 6, enemy.Pos.Y - 12, (r + 6) * 2, 24); }
        if (enemy.BeaconMarkTime > 0) { using var beaconMark = new Pen(Color.FromArgb(235, 165, 229, 238), 3) { DashStyle = DashStyle.Dot }; g.DrawEllipse(beaconMark, enemy.Pos.X - r - 8, enemy.Pos.Y - 15, (r + 8) * 2, 30); }
        if (enemy.BellMarkTime > 0) { using var bellMark = new Pen(Color.FromArgb(240, 246, 207, 119), 2) { DashStyle = DashStyle.Dot }; g.DrawEllipse(bellMark, enemy.Pos.X - r - 14, enemy.Pos.Y - 20, (r + 14) * 2, 40); }
        if (enemy.NameWeaverMarkTime > 0) { using var nameWeaverMark = new Pen(Color.FromArgb(235, 211, 139, 234), 3) { DashStyle = DashStyle.Dash }; g.DrawEllipse(nameWeaverMark, enemy.Pos.X - r - 20, enemy.Pos.Y - 25, (r + 20) * 2, 50); }
        if (enemy.Kind == EnemyKind.ReedKeeper) { using var stalk = new Pen(Color.FromArgb(220, 205, 193, 133), 3); for (int i = -1; i <= 1; i++) g.DrawLine(stalk, enemy.Pos.X + i * 9, enemy.Pos.Y + 13, enemy.Pos.X + i * 5, enemy.Pos.Y - 23); }
        if (enemy.Kind == EnemyKind.FlowKeeper) { using var ring = new Pen(Color.FromArgb(210, 157, 215, 216), 2); float spin = windupPulse * 40; g.DrawArc(ring, enemy.Pos.X - 18, enemy.Pos.Y - 18, 36, 36, spin, 235); g.DrawArc(ring, enemy.Pos.X - 13, enemy.Pos.Y - 13, 26, 26, spin + 170, 115); }
        if (enemy.Kind == EnemyKind.GlassHerald) { using var shard = new Pen(Color.FromArgb(225, 186, 224, 231), 3); float spin = windupPulse * 24; for (int i = 0; i < 3; i++) { float angle = spin + i * 120; float radians = angle * MathF.PI / 180; PointF tip = new(enemy.Pos.X + MathF.Cos(radians) * 27, enemy.Pos.Y + MathF.Sin(radians) * 27); PointF tail = new(enemy.Pos.X - MathF.Cos(radians) * 18, enemy.Pos.Y - MathF.Sin(radians) * 18); g.DrawLine(shard, tail, tip); } }
        if (enemy.Kind == EnemyKind.RouteScribe) { using var folio = new Pen(Color.FromArgb(225, 196, 216, 226), 2); float spin = windupPulse * 28; for (int i = 0; i < 4; i++) { float angle = (spin + i * 90) * MathF.PI / 180; float x = enemy.Pos.X + MathF.Cos(angle) * 28, y = enemy.Pos.Y + MathF.Sin(angle) * 22; g.DrawRectangle(folio, x - 6, y - 8, 12, 16); } }
        float hpY = spriteRow >= 0 && enemySprites is not null ? enemy.Pos.Y - (bossSprite ? 141 : individualSpriteLoaded ? spriteHeight + 8 : enemy.Kind == EnemyKind.Elite ? 88 : 77) : enemy.Pos.Y - r - 10;
        using var hpBack = new SolidBrush(Color.FromArgb(40, 20, 20)); using var hpBar = new SolidBrush(Color.FromArgb(188, 76, 70)); g.FillRectangle(hpBack, enemy.Pos.X - 22, hpY, 44, 5); g.FillRectangle(hpBar, enemy.Pos.X - 22, hpY, 44 * Math.Max(0, enemy.Hp / enemy.MaxHp), 5);
        string name = enemy.EliteMinion ? "ECO INVOCADO" : EnemyName(enemy); if (name.Length > 0) DrawCentered(g, name, spriteRow >= 0 && enemySprites is not null ? hpY - 17 : enemy.Pos.Y - r - 28, new Font("Segoe UI", 8, FontStyle.Bold), Color.FromArgb(233, 198, 151), enemy.Pos.X);
        if (enemy.Boss) { using var bossBack = new SolidBrush(Color.FromArgb(40, 20, 20)); using var bossHp = new SolidBrush(enemy.Hp <= enemy.MaxHp * 0.5f ? Color.OrangeRed : enemy.Kind == EnemyKind.BlindScribe ? Color.Teal : enemy.Kind == EnemyKind.RiverFerryman ? Color.SteelBlue : enemy.Kind == EnemyKind.AshWarden ? Color.DarkOrange : enemy.Kind == EnemyKind.HollowName ? Color.LightSeaGreen : enemy.Kind == EnemyKind.NameWeaver ? Color.MediumPurple : enemy.Kind == EnemyKind.FirstKeeper ? Color.Goldenrod : enemy.Kind == EnemyKind.OathCartographer ? Color.DarkCyan : enemy.Kind == EnemyKind.ReedKeeper ? Color.DarkOliveGreen : enemy.Kind == EnemyKind.CounterCurrent ? Color.CadetBlue : enemy.Kind == EnemyKind.RiftCantor ? Color.MediumOrchid : enemy.Kind == EnemyKind.FlowKeeper ? Color.DeepSkyBlue : enemy.Kind == EnemyKind.GlassHerald ? Color.LightCyan : enemy.Kind is EnemyKind.NameBroker or EnemyKind.TideWitness ? Color.MediumPurple : enemy.Kind == EnemyKind.RouteScribe ? Color.LightSteelBlue : enemy.Kind == EnemyKind.LastTuner ? Color.PaleGoldenrod : Color.Firebrick); g.FillRectangle(bossBack, 360, 112, 240, 8); g.FillRectangle(bossHp, 360, 112, 240 * Math.Clamp(enemy.Hp / enemy.MaxHp, 0, 1), 8); string bossName = enemy.Kind == EnemyKind.BlindScribe ? "ESCRIBA CEGO" : enemy.Kind == EnemyKind.RiverFerryman ? "BARQUEIRO OCO" : enemy.Kind == EnemyKind.AshWarden ? "GUARDIÃO DAS CINZAS" : enemy.Kind == EnemyKind.HollowName ? "O NOME VAZIO" : enemy.Kind == EnemyKind.NameWeaver ? "A TECELÃ DE NOMES" : enemy.Kind == EnemyKind.FirstKeeper ? "A PRIMEIRA VIGÍLIA" : enemy.Kind == EnemyKind.OathCartographer ? "CARTÓGRAFA DO VÃO" : enemy.Kind == EnemyKind.NameBroker ? "ESCRIVÃO DE CONTRATO" : enemy.Kind == EnemyKind.TideWitness ? "TESTEMUNHA DA MARÉ" : enemy.Kind == EnemyKind.ReedKeeper ? "GUARDIÃ DO LEITO" : enemy.Kind == EnemyKind.CounterCurrent ? "AFINADOR DAS MARGENS" : enemy.Kind == EnemyKind.RiftCantor ? "A CONTRAVOZ" : enemy.Kind == EnemyKind.FlowKeeper ? "VIGIA DO VEIO PRESO" : enemy.Kind == EnemyKind.GlassHerald ? "ARAUTO DE VIDRO" : enemy.Kind == EnemyKind.LanternRegent ? "REGENTE DAS LANTERNAS" : enemy.Kind == EnemyKind.RouteScribe ? "ESCRIBA DO TRAÇADO" : enemy.Kind == EnemyKind.DeepSilence ? "SILÊNCIO DE BAIXO" : enemy.Kind == EnemyKind.BorrowedChorus ? "ARQUIVISTA DAS MARÉS" : enemy.Kind == EnemyKind.LastTuner ? "AFINADOR DO CONTRATEMPO" : "GUARDIÃO DA CRIPTA"; DrawCentered(g, enemy.Hp <= enemy.MaxHp * 0.5f ? $"{bossName} • SEGUNDA FASE" : bossName, 91, new Font("Georgia", 11, FontStyle.Bold), enemy.Hp <= enemy.MaxHp * 0.5f ? Color.OrangeRed : Color.Goldenrod); }
    }
    private static bool DrawProceduralEnemy(Graphics g, Enemy enemy, Color color)
    {
        float x = enemy.Pos.X, y = enemy.Pos.Y, r = Math.Max(13, enemy.Radius);
        using var fill = new SolidBrush(color);
        using var outline = new Pen(Color.FromArgb(225, 28, 25, 27), 2.2f);
        using var accent = new Pen(Color.FromArgb(205, color), 2.4f);
        switch (enemy.Kind)
        {
            case EnemyKind.SlagWeaver:
            {
                float rotation = enemy.AnimationTime * 0.6f;
                using var cinderGlow = new SolidBrush(Color.FromArgb(42, 237, 133, 69)); g.FillEllipse(cinderGlow, x - r * 1.5f, y - r * 1.5f, r * 3f, r * 3f);
                for (int i = 0; i < 4; i++)
                {
                    float angle = rotation + i * MathF.PI / 2;
                    PointF tip = new(x + MathF.Cos(angle) * r * 1.05f, y + MathF.Sin(angle) * r * 1.05f);
                    PointF left = new(x + MathF.Cos(angle + 1.1f) * r * 0.5f, y + MathF.Sin(angle + 1.1f) * r * 0.5f);
                    PointF right = new(x + MathF.Cos(angle - 1.1f) * r * 0.5f, y + MathF.Sin(angle - 1.1f) * r * 0.5f);
                    using var shard = new GraphicsPath(); shard.AddPolygon(new[] { tip, left, new PointF(x, y), right }); g.FillPath(fill, shard); g.DrawPath(outline, shard);
                }
                g.FillEllipse(fill, x - r * 0.48f, y - r * 0.48f, r * 0.96f, r * 0.96f); g.DrawEllipse(outline, x - r * 0.48f, y - r * 0.48f, r * 0.96f, r * 0.96f);
                using var core = new SolidBrush(Color.FromArgb(245, 250, 193, 126)); g.FillEllipse(core, x - r * 0.14f, y - r * 0.14f, r * 0.28f, r * 0.28f);
                return true;
            }
            case EnemyKind.CooldownLeech:
            {
                float rotation = enemy.AnimationTime * 34f;
                g.DrawArc(accent, x - r * 1.45f, y - r * 1.45f, r * 2.9f, r * 2.9f, rotation, 115);
                g.DrawArc(accent, x - r * 1.45f, y - r * 1.45f, r * 2.9f, r * 2.9f, rotation + 180, 115);
                g.FillEllipse(fill, x - r * .58f, y - r * .58f, r * 1.16f, r * 1.16f);
                g.DrawEllipse(outline, x - r * .58f, y - r * .58f, r * 1.16f, r * 1.16f);
                using (var core = new SolidBrush(Color.FromArgb(240, 238, 205, 255))) g.FillEllipse(core, x - r * .17f, y - r * .17f, r * .34f, r * .34f);
                return true;
            }
            case EnemyKind.LastTuner:
                g.DrawEllipse(accent, x - r * 1.65f, y - r * 1.65f, r * 3.3f, r * 3.3f);
                g.DrawEllipse(accent, x - r * 1.18f, y - r * 1.18f, r * 2.36f, r * 2.36f);
                g.DrawLine(accent, x, y - r * 1.25f, x, y - r * .62f);
                g.FillEllipse(fill, x - r * .54f, y - r * .63f, r * 1.08f, r * 1.28f);
                g.DrawEllipse(outline, x - r * .54f, y - r * .63f, r * 1.08f, r * 1.28f);
                using (var bell = new GraphicsPath()) { bell.AddPolygon(new[] { new PointF(x - r * .34f, y - r * .12f), new PointF(x - r * .25f, y + r * .45f), new PointF(x + r * .25f, y + r * .45f), new PointF(x + r * .34f, y - r * .12f) }); g.DrawPath(accent, bell); }
                g.DrawLine(accent, x + r * .65f, y - r * .2f, x + r * 1.15f, y + r * .55f);
                return true;
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
        if (questStage >= 57) known |= 1L << 41;
        if (questStage >= 58) known |= 1L << 42;
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

    private void DrawPlayer(Graphics g)
    {
        PointF animationDirection = attackFlash > 0 ? facing : dashTime > 0 ? dashDirection : MovementInput();
        if (Length(animationDirection) <= 0.001f) animationDirection = CurrentDrift();
        if (Length(animationDirection) <= 0.001f) animationDirection = facing;
        Bitmap? attackSprites = attackFlash > 0 ? PlayerAttackSpriteSheet.Value : null;
        Bitmap? sprites = attackSprites ?? PlayerSpriteSheet.Value;
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
        if (stoneBastionTime > 0)
        {
            int alpha = Math.Clamp((int)(145 + 80 * MathF.Sin(windupPulse * 11)), 55, 230);
            using var bastionGlow = new Pen(Color.FromArgb(alpha, 226, 137, 76), 5);
            using var bastionCore = new Pen(Color.FromArgb(Math.Min(245, alpha + 25), 255, 218, 139), 1.5f);
            g.DrawEllipse(bastionGlow, player.X - 31, player.Y - 31, 62, 62);
            g.DrawEllipse(bastionCore, player.X - 25, player.Y - 25, 50, 50);
        }
        if (skillCd > 0) { using var cooldownPen = new Pen(Color.FromArgb(190, 230, 193, 129), 3); g.DrawArc(cooldownPen, player.X - 25, player.Y - 25, 50, 50, -90, 360 * skillCd / Math.Max(1.5f, 6f - skillTalent * .5f)); }
        if (shardCd > 0) { using var shardCooldownPen = new Pen(Color.FromArgb(205, 107, 202, 196), 3); g.DrawArc(shardCooldownPen, player.X - 33, player.Y - 33, 66, 66, 90, 360 * shardCd / VoiceShardCooldown()); }
        if (riverBell && bellCd > 0) { using var bellCooldownPen = new Pen(Color.FromArgb(210, 205, 169, 118), 3); g.DrawArc(bellCooldownPen, player.X - 41, player.Y - 41, 82, 82, 180, 360 * bellCd / BellPulseCooldown()); }
        if (ebbSeal && pullCd > 0) { using var ebbCooldownPen = new Pen(Color.FromArgb(210, 115, 185, 211), 3); g.DrawArc(ebbCooldownPen, player.X - 49, player.Y - 49, 98, 98, 270, 360 * pullCd / EbbPullCooldown()); }
        if (anchorWard && wardCd > 0) { using var wardCooldownPen = new Pen(Color.FromArgb(210, 179, 215, 215), 3); g.DrawArc(wardCooldownPen, player.X - 57, player.Y - 57, 114, 114, 0, 360 * wardCd / AnchorWardCooldown()); }
        if (refrainSong && refrainCd > 0) { using var refrainCooldownPen = new Pen(Color.FromArgb(215, 205, 139, 225), 3); g.DrawArc(refrainCooldownPen, player.X - 65, player.Y - 65, 130, 130, 90, 360 * refrainCd / RefrainCooldown()); }
        if (beaconLight && beaconCd > 0) { using var beaconCooldownPen = new Pen(Color.FromArgb(220, 182, 238, 220), 3); g.DrawArc(beaconCooldownPen, player.X - 73, player.Y - 73, 146, 146, 180, 360 * beaconCd / BeaconTotalCooldown()); }
        if (finalEnding == 1 && returnVoiceCd > 0) { using var returnedVoicesCooldownPen = new Pen(Color.FromArgb(215, 205, 221, 165), 3); g.DrawArc(returnedVoicesCooldownPen, player.X - 81, player.Y - 81, 162, 162, 270, 360 * returnVoiceCd / (Math.Max(14f, 23f - skillTalent * 0.7f) * WeaponCooldownMultiplier())); }
        if (finalEnding == 2 && finalStrikeCd > 0) { using var finalSparkCooldownPen = new Pen(Color.FromArgb(225, 255, 191, 115), 3); g.DrawArc(finalSparkCooldownPen, player.X - 89, player.Y - 89, 178, 178, 0, 360 * finalStrikeCd / (Math.Max(12f, 19f - skillTalent * 0.65f) * (skillVariants[8] == 1 ? 1.25f : skillVariants[8] == 2 ? 0.75f : 1f) * WeaponCooldownMultiplier())); }
        if (heroClass == HeroClass.StoneGuardian && stoneBastionCd > 0) { using var bastionCooldownPen = new Pen(Color.FromArgb(225, 226, 137, 76), 3); g.DrawArc(bastionCooldownPen, player.X - 97, player.Y - 97, 194, 194, 90, 360 * stoneBastionCd / (18f * WeaponCooldownMultiplier())); }
        if (heroClass == HeroClass.NameWeaver && nameWeaverCd > 0) { using var threadCooldownPen = new Pen(Color.FromArgb(225, 193, 151, 224), 3); g.DrawArc(threadCooldownPen, player.X - 97, player.Y - 97, 194, 194, 90, 360 * nameWeaverCd / (14f * WeaponCooldownMultiplier())); }
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
        if (weapon == 3)
        {
            float reach = 20 + 108 * MathF.Sin(MathF.PI * (1 - progress));
            PointF tip = Add(player, Scale(facing, reach));
            PointF basePoint = Add(player, Scale(facing, 16));
            PointF cross = new(-facing.Y * 9, facing.X * 9);
            int spearAlpha = (int)(progress * 230);
            using var spearGlow = new Pen(Color.FromArgb(spearAlpha / 3, 137, 221, 209), 12) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            using var spearEdge = new Pen(Color.FromArgb(spearAlpha, 208, 231, 202), 3) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLine(spearGlow, basePoint, tip); g.DrawLine(spearEdge, basePoint, tip);
            g.DrawLine(spearEdge, Add(basePoint, cross), Add(basePoint, Scale(cross, -1)));
            using var pointGlow = new SolidBrush(Color.FromArgb(spearAlpha / 2, 150, 238, 224));
            g.FillEllipse(pointGlow, tip.X - 5, tip.Y - 5, 10, 10);
            return;
        }
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
    private void DrawGroundHazards(Graphics g)
    {
        foreach (GroundHazard hazard in groundHazards)
        {
            float fade = Math.Clamp(hazard.TimeLeft / 0.45f, 0.2f, 1f), pulse = 0.5f + 0.5f * MathF.Sin(windupPulse * 5f);
            int fillAlpha = (int)(34 * fade + pulse * 12), edgeAlpha = (int)(190 * fade);
            using var fill = new SolidBrush(Color.FromArgb(fillAlpha, 181, 89, 42));
            using var rim = new Pen(Color.FromArgb(edgeAlpha, 244, 155, 87), 2.5f) { DashStyle = DashStyle.Dash };
            using var fissure = new Pen(Color.FromArgb((int)(150 * fade), 238, 177, 101), 2);
            g.FillEllipse(fill, hazard.Pos.X - hazard.Radius, hazard.Pos.Y - hazard.Radius, hazard.Radius * 2, hazard.Radius * 2);
            g.DrawEllipse(rim, hazard.Pos.X - hazard.Radius, hazard.Pos.Y - hazard.Radius, hazard.Radius * 2, hazard.Radius * 2);
            g.DrawLine(fissure, hazard.Pos.X - 20, hazard.Pos.Y - 10, hazard.Pos.X - 5, hazard.Pos.Y + 4);
            g.DrawLine(fissure, hazard.Pos.X - 5, hazard.Pos.Y + 4, hazard.Pos.X + 12, hazard.Pos.Y - 6);
            g.DrawLine(fissure, hazard.Pos.X + 12, hazard.Pos.Y - 6, hazard.Pos.X + 24, hazard.Pos.Y + 11);
        }
    }

    private void DrawEbbWave(Graphics g) { float progress = 1f - pullWaveTime / 0.45f; float radius = EbbPullRadius() * progress; using var wave = new Pen(Color.FromArgb((int)(210 * pullWaveTime / 0.45f), 98, 190, 220), 4); using var glow = new Pen(Color.FromArgb((int)(65 * pullWaveTime / 0.45f), 98, 190, 220), 12); g.DrawEllipse(glow, player.X - radius, player.Y - radius, radius * 2, radius * 2); g.DrawEllipse(wave, player.X - radius, player.Y - radius, radius * 2, radius * 2); }
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
    private string ShortSkillName(int slot) => ShortSkillName(equippedSkills, slot);
    private string ShortSkillName(int[] loadout, int slot) => loadout[slot] == 0 ? "VAZIO" : loadout[slot] == 7 ? "DEVOLUÇÃO" : loadout[slot] == 8 ? "CENTELHA" : skillNames[loadout[slot]].ToUpperInvariant();
    private void DrawSkillMenu(Graphics g)
    {
        using var shade = new SolidBrush(Color.FromArgb(185, 5, 7, 9)); using var panelBrush = new SolidBrush(Color.FromArgb(248, 17, 19, 20)); using var edge = new Pen(Color.FromArgb(175, 143, 112, 73), 2);
        using var heading = new Font("Georgia", 17, FontStyle.Bold); using var body = new Font("Segoe UI", 11); using var small = new Font("Segoe UI", 9);
        g.FillRectangle(shade, 0, 83, 960, 557); g.FillRectangle(panelBrush, 105, 132, 750, 435); g.DrawRectangle(edge, 105, 132, 750, 435);
        DrawCentered(g, skillMenuAlternate ? "MONTAGEM ALTERNATIVA" : "MONTAGEM PRINCIPAL", 153, heading, Color.FromArgb(230, 193, 129));
        g.DrawString("TAB alterna qual montagem editar; T ativa a alternativa durante o combate. F/V/G/C escolhem espaços; 1–0 equipam.", small, Brushes.DarkGray, 140, 188);
        g.DrawString("ESPAÇOS DA MONTAGEM", body, Brushes.Goldenrod, 150, 224);
        string[] slotKeys = { "F", "V", "G", "C" };
        int[] menuSkills = SkillMenuLoadout();
        for (int slot = 0; slot < 4; slot++)
        {
            bool selected = slot == skillMenuSlot; Brush slotBrush = selected ? Brushes.Goldenrod : Brushes.AntiqueWhite;
            g.DrawString($"{slotKeys[slot]}  •  {ShortSkillName(menuSkills, slot)}{(selected ? "  ◀" : "")}", body, slotBrush, 160, 258 + slot * 32);
        }
        int selectedSkill = menuSkills[skillMenuSlot];
        int selectedVariant = selectedSkill is >= 1 and <= 8 ? skillVariants[selectedSkill] : 0;
        g.DrawString(selectedSkill == 0 ? "MEMÓRIA DA TÉCNICA: ESPAÇO VAZIO" : selectedSkill >= 9 ? $"TÉCNICA DE CLASSE: {skillNames[selectedSkill].ToUpperInvariant()}" : $"MEMÓRIA: {SkillVariantName(selectedSkill, selectedVariant)}", body, Brushes.Goldenrod, new RectangleF(150, 402, 310, 24));
        g.DrawString(selectedSkill == 0 ? "Equipe uma técnica e use ↑/↓ para mudar sua forma." : selectedSkill == 9 ? "Consome 50 de Fúria para reduzir dano em 55% por 3,5 s." : selectedSkill == 10 ? "Consome 40 de Fio; ecos marcados recebem 25% mais dano por 5 s." : SkillVariantDescription(selectedSkill, selectedVariant), small, Brushes.DarkGray, new RectangleF(150, 430, 310, 36));
        g.DrawString("TÉCNICAS APRENDIDAS", body, Brushes.Goldenrod, 480, 224);
        for (int skill = 1; skill <= 10; skill++)
        {
            bool unlocked = SkillUnlocked(skill); bool equippedElsewhere = menuSkills.Where((_, index) => index != skillMenuSlot).Contains(skill);
            string state = !unlocked ? "  •  NÃO APRENDIDA" : equippedElsewhere ? "  •  EM OUTRO ESPAÇO" : menuSkills[skillMenuSlot] == skill ? "  •  EQUIPADA" : "";
            int rowY = 258 + (skill - 1) * 27;
            g.DrawString($"{skill}. {skillNames[skill].ToUpperInvariant()}{state}", body, unlocked ? Brushes.AntiqueWhite : Brushes.Gray, 480, rowY);
            g.DrawString(skillDescriptions[skill], small, Brushes.DarkGray, 503, rowY + 16);
        }
        g.DrawString("SINTONIAS DE TÉCNICAS", small, Brushes.Goldenrod, 150, 472);
        g.DrawString("1+2 • Sino prepara alvos; Estilhaço causa +35% e consome a marca", small, HasSkillPair(menuSkills, 1, 2) ? Brushes.LightGoldenrodYellow : Brushes.DarkGray, new RectangleF(150, 488, 310, 16));
        g.DrawString("3+6 • Vazante prepara alvos; Farol causa +35% no próximo pulso", small, HasSkillPair(menuSkills, 3, 6) ? Brushes.LightCyan : Brushes.DarkGray, new RectangleF(150, 504, 310, 16));
        g.DrawString("2+5 • Sino prepara alvos; Coro causa +35% e consome a marca", small, HasSkillPair(menuSkills, 2, 5) ? Brushes.LightGoldenrodYellow : Brushes.DarkGray, new RectangleF(150, 520, 310, 16));
        Brush wardSynergyBrush = HasSkillPair(menuSkills, 4, 7) ? skillMenuAlternate == alternateSkillsActive && wardTime > 0 ? Brushes.LightGreen : Brushes.Goldenrod : Brushes.DarkGray;
        g.DrawString("4+7 • Manto ativo amplia em 35% a cura da Devolução", small, wardSynergyBrush, new RectangleF(150, 536, 310, 16));
        g.DrawString("↑/↓ altera a memória da técnica equipada • BACKSPACE esvazia • B/ESC fecha • R: Redemoinho", small, Brushes.DarkGray, new RectangleF(150, 555, 660, 12));
    }

    private void DrawChoice(Graphics g, string titleText, string a, string b, string c)
    {
        using var bg = new SolidBrush(Color.FromArgb(248, 15, 17, 18)); using var optionFont = new Font("Segoe UI", 12); g.FillRectangle(bg, 170, 205, 620, 225);
        DrawCentered(g, titleText, 238, new Font("Georgia", 15, FontStyle.Bold), Color.Goldenrod);
        g.DrawString(a, optionFont, Brushes.AntiqueWhite, 220, 286); g.DrawString(b, optionFont, Brushes.AntiqueWhite, 220, 330); g.DrawString(c, optionFont, Brushes.AntiqueWhite, 220, 374);
    }

    private void DrawTalentChoice(Graphics g, int pending)
    {
        using var background = new SolidBrush(Color.FromArgb(248, 15, 17, 18));
        using var optionFont = new Font("Segoe UI", 12);
        using var hintBrush = new SolidBrush(Color.FromArgb(165, 174, 168));
        g.FillRectangle(background, 170, 155, 620, 420);
        DrawCentered(g, $"SUBIU DE NÍVEL — ESCOLHA ({pending} PENDENTES)", 183, new Font("Georgia", 15, FontStyle.Bold), Color.Goldenrod);
        g.DrawString("1. VIGOR  +20 VIDA MÁXIMA", optionFont, Brushes.AntiqueWhite, 220, 225);
        g.DrawString("2. DOMÍNIO  técnicas recarregam mais rápido", optionFont, Brushes.AntiqueWhite, 220, 260);
        g.DrawString("3. AGILIDADE  esquiva recarrega mais rápido", optionFont, Brushes.AntiqueWhite, 220, 295);
        g.DrawString("4. SINTONIA  +8% de dano de técnica por ponto", optionFont, Brushes.AntiqueWhite, 220, 330);
        if (level < 5) g.DrawString("Especializações 5 e 6 desbloqueiam no nível 5.", optionFont, hintBrush, 220, 374);
        else if (combatDiscipline == 0)
        {
            g.DrawString("5. FORMA DO GUME  +12% básicos • -10% técnicas", optionFont, Brushes.Goldenrod, 220, 374);
            g.DrawString("6. FORMA DO ECO  +12% técnicas • -10% básicos", optionFont, Brushes.Plum, 220, 408);
        }
        else g.DrawString($"Especialização: {(combatDiscipline == 1 ? "Forma do Gume" : "Forma do Eco")}", optionFont, hintBrush, 220, 374);

        if (level >= 10 && combatDiscipline > 0 && combatMastery == 0)
        {
            g.DrawString("5. GUARDA SERENA  -10% dano recebido • esquiva +0,20 s", optionFont, Brushes.Goldenrod, 220, 408);
            g.DrawString("6. PASSO IMPETUOSO  -0,18 s esquiva • +8% dano recebido", optionFont, Brushes.Plum, 220, 442);
        }
        else if (combatMastery > 0) g.DrawString($"Maestria: {(combatMastery == 1 ? "Guarda Serena" : "Passo Impetuoso")}", optionFont, hintBrush, 220, 408);
        else if (combatDiscipline > 0) g.DrawString("Maestria desbloqueia no nível 10.", optionFont, hintBrush, 220, 408);

        if (level >= 15 && combatDiscipline > 0 && combatMastery > 0 && combatFocus == 0)
        {
            g.DrawString("5. CAÇADOR DE ECOS  +15% dano contra chefes", optionFont, Brushes.Goldenrod, 220, 442);
            g.DrawString("6. QUEBRA-FORMAÇÃO  +10% contra comuns com 3+ inimigos", optionFont, Brushes.Plum, 220, 476);
        }
        else if (combatFocus > 0) g.DrawString($"Foco: {(combatFocus == 1 ? "Caçador de Ecos" : "Quebra-Formação")} • redistribuível na forja.", optionFont, hintBrush, 220, 442);
        else if (combatDiscipline > 0 && combatMastery > 0) g.DrawString("Foco de combate desbloqueia no nível 15.", optionFont, hintBrush, 220, 442);

        if (level >= 20 && combatDiscipline > 0 && combatMastery > 0 && combatFocus > 0 && combatApex == 0)
        {
            g.DrawString("5. CORO SUSPENSO  +18% técnicas com 2+ recargas ativas", optionFont, Brushes.Goldenrod, 220, 493);
            g.DrawString("6. ÂNCORA DE ECO  -12% dano recebido com 2+ recargas", optionFont, Brushes.Plum, 220, 527);
        }
        else if (combatApex > 0) g.DrawString($"Auge: {(combatApex == 1 ? "Coro Suspenso" : "Âncora de Eco")} • redistribuível na forja.", optionFont, hintBrush, 220, 493);
        g.DrawString("Talentos comuns podem se repetir; cada marco escolhe um caminho.", optionFont, hintBrush, 220, level >= 20 ? 560 : 520);
    }
    private void DrawStoryOverlay(Graphics g)
    {
        DrawReadingOverlay(g, $"MEMÓRIA NA PEDRA   •   {storyPage + 1} / {prologue.Length}", ProloguePage(storyPage), "ENTER, ESPAÇO OU CLIQUE  •  CONTINUAR", FontStyle.Regular);
    }
    private void DrawReadingOverlay(Graphics g, string heading, string body, string action, FontStyle bodyStyle)
    {
        using var shade = new SolidBrush(Color.FromArgb(145, 5, 7, 9));
        using var panelBrush = new SolidBrush(Color.FromArgb(246, 19, 21, 22));
        using var border = new Pen(Color.FromArgb(175, 143, 112, 73), 2);
        using var headingFont = new Font("Georgia", 13, FontStyle.Bold);
        using var bodyFont = new Font("Segoe UI", 14, bodyStyle);
        using var hintFont = new Font("Segoe UI", 9, FontStyle.Bold);
        using var headingBrush = new SolidBrush(Color.FromArgb(230, 193, 129));
        using var bodyBrush = new SolidBrush(Color.AntiqueWhite);
        using var hintBrush = new SolidBrush(Color.FromArgb(170, 174, 168));
        ReadingOverlayLayout layout = MeasureReadingOverlay(g, body, headingFont, bodyFont, hintFont);
        g.FillRectangle(shade, 0, 0, 960, 640);
        g.FillRectangle(panelBrush, 86, layout.PanelY, 788, layout.PanelHeight);
        g.DrawRectangle(border, 86, layout.PanelY, 788, layout.PanelHeight);
        g.DrawString(heading, headingFont, headingBrush, 112, layout.HeadingY);
        g.DrawString(body, bodyFont, bodyBrush, new RectangleF(112, layout.BodyY, 736, layout.BodyHeight));
        g.DrawString(action, hintFont, hintBrush, 112, layout.HintY);
    }
    private static ReadingOverlayLayout MeasureReadingOverlay(Graphics g, string body, Font headingFont, Font bodyFont, Font hintFont)
    {
        float bodyHeight = Math.Max(bodyFont.GetHeight(g), g.MeasureString(body, bodyFont, new SizeF(736, 360)).Height + 2);
        float panelHeight = Math.Max(154, 20 + headingFont.GetHeight(g) + 8 + bodyHeight + 10 + hintFont.GetHeight(g) + 16);
        float panelY = 574 - panelHeight;
        float headingY = panelY + 20;
        float bodyY = headingY + headingFont.GetHeight(g) + 8;
        float hintY = bodyY + bodyHeight + 10;
        return new ReadingOverlayLayout(panelY, panelHeight, headingY, bodyY, bodyHeight, hintY);
    }
    private void DrawMemoryOverlay(Graphics g)
    {
        DrawReadingOverlay(g, memoryTitle, memoryText, "ENTER, ESPAÇO OU CLIQUE  •  FECHAR", FontStyle.Italic);
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
    private static int EnemyAttackSpriteFrame(Enemy enemy) => enemy.Windup > 0 ? Math.Min(3, (int)(Math.Clamp(1f - enemy.Windup / Math.Max(0.01f, enemy.WindupMax), 0f, 1f) * 4f)) : enemy.AttackPoseTime > 0 ? 3 : -1;
    private static (float Width, float Height, float OffsetX, float OffsetY) EnemyAttackPose(Enemy enemy, float progress)
    {
        float p = Math.Clamp(progress, 0f, 1f);
        return enemy.Kind switch
        {
            EnemyKind.DrownedMemory => (1 + 0.16f * p, 1 + 0.05f * p, 0, -p),
            EnemyKind.EmberWisp => (1 + 0.22f * p, 1 + 0.25f * p, 0, -5 * p),
            EnemyKind.NameMoth => (1 + 0.28f * p, 1 + 0.02f * p, 0, 0),
            EnemyKind.MudCantor => (1 + 0.12f * p, 1 + 0.20f * p, 0, 2 * p),
            EnemyKind.PaperWraith => (1 + 0.05f * p, 1 + 0.05f * p, MathF.Sin(enemy.AnimationTime * 18f) * 6f * p, -2 * p),
            EnemyKind.SaltEcho => (1 - 0.08f * p, 1 + 0.18f * p, 0, -3 * p),
            EnemyKind.GlassHerald when enemy.BossPattern == 2 => (1 + 0.19f * p, 1 - 0.07f * p, 0, -3 * p),
            EnemyKind.RouteScribe when enemy.BossPattern == 0 => (1 + 0.12f * p, 1 + 0.16f * p, 0, -4 * p),
            EnemyKind.LastTuner when enemy.BossPattern == 0 => (1 + 0.22f * p, 1 + 0.11f * p, 0, -5 * p),
            EnemyKind.DeepSilence when enemy.BossPattern == 2 => (1 + 0.04f * p, 1 + 0.21f * p, 0, -4 * p),
            _ => (1, 1, 0, 0)
        };
    }
    private static bool EnemyFacesLeft(float enemyX, float playerX) => playerX < enemyX;
    private static string EnemyName(Enemy e) => e.VigilMinion ? "ECO ERRANTE • SALINO" : e.Kind switch { EnemyKind.Guardian => "GUARDIÃO SEM NOME", EnemyKind.BlindScribe => "ESCRIBA CEGO", EnemyKind.RiverFerryman => "BARQUEIRO OCO", EnemyKind.AshWarden => "GUARDIÃO DAS CINZAS", EnemyKind.HollowName => "O NOME VAZIO", EnemyKind.NameWeaver => "A TECELÃ DE NOMES", EnemyKind.FirstKeeper => "A PRIMEIRA VIGÍLIA", EnemyKind.OathCartographer => "CARTÓGRAFA DO VÃO", EnemyKind.NameBroker => "ESCRIVÃO DE CONTRATO", EnemyKind.SaltEcho => "ECO SALINO", EnemyKind.TideWitness => "TESTEMUNHA DA MARÉ", EnemyKind.ReedKeeper => "GUARDIÃ DO LEITO", EnemyKind.FlowKeeper => "VIGIA DO VEIO PRESO", EnemyKind.CounterCurrent => "AFINADOR DAS MARGENS", EnemyKind.RiftCantor => "A CONTRAVOZ", EnemyKind.GlassHerald => "ARAUTO DE VIDRO", EnemyKind.LanternRegent => "REGENTE DAS LANTERNAS", EnemyKind.RouteScribe => "ESCRIBA DO TRAÇADO", EnemyKind.DeepSilence => "SILÊNCIO DE BAIXO", EnemyKind.BorrowedChorus => "ARQUIVISTA DAS MARÉS", EnemyKind.LastTuner => "AFINADOR DO CONTRATEMPO", EnemyKind.PaperWraith => "ESPECTRO DE PAPEL", EnemyKind.MudCantor => "CANTOR DO LODO", EnemyKind.DrownedMemory => "LEMBRANÇA AFOGADA", EnemyKind.EmberWisp => "VESTÍGIO DE BRASAS", EnemyKind.NameMoth => "MARIPOSA DE NOMES", EnemyKind.Elite => $"ELITE • {EliteAffixName(e)}", EnemyKind.Archer => "ARQUEIRO", EnemyKind.Mage => "CONJURADOR", EnemyKind.InkEcho => "ECO DE TINTA", EnemyKind.CooldownLeech => "MORDENTE DE COMPASSO", EnemyKind.SlagWeaver => "ECO DE ESCÓRIA", _ => "" };
    private static PointF Normalize(PointF p, PointF fallback) { float n = Length(p); return n > 0.001f ? new PointF(p.X / n, p.Y / n) : fallback; }
    private static float Length(PointF p) => MathF.Sqrt(p.X * p.X + p.Y * p.Y);
    private static float Distance(PointF a, PointF b) => Length(Sub(a, b));
    private static float DistanceToSegment(PointF point, PointF start, PointF end) { PointF line = Sub(end, start); float lengthSquared = Dot(line, line); float t = lengthSquared > 0.001f ? Math.Clamp(Dot(Sub(point, start), line) / lengthSquared, 0, 1) : 0; return Distance(point, Add(start, Scale(line, t))); }
    private static PointF Sub(PointF a, PointF b) => new(a.X - b.X, a.Y - b.Y);
    private static PointF Offset(PointF a, float x, float y) => new(a.X + x, a.Y + y);
    private static PointF Add(PointF a, PointF b) => new(a.X + b.X, a.Y + b.Y);
    private static PointF Scale(PointF a, float m) => new(a.X * m, a.Y * m);
    private static float Dot(PointF a, PointF b) => a.X * b.X + a.Y * b.Y;
}
