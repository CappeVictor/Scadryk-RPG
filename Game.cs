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
        public PointF Pos, Target, Target2, Target2Start, KnockbackVelocity;
        public EnemyKind Kind;
        public float Hp, MaxHp, Cooldown, Windup, WindupMax, Flash, AnimationTime, AttackPoseTime, SummonCooldown, SummonWindup, SupportCooldown, SupportWindup, BeaconMarkTime, BellMarkTime, NameWeaverMarkTime, WoundTime, WoundTickCooldown, WoundDamage;
        public int BossPattern;
        public bool Armored, Moving;
        public bool VigilMinion;
        public EliteAffix? EliteAffix2;
        public bool EliteMinion;
        public EliteAffix EliteAffix;
        public float Stagger, KnockbackTime;
        public bool Boss => Kind is EnemyKind.Guardian or EnemyKind.BlindScribe or EnemyKind.RiverFerryman or EnemyKind.AshWarden or EnemyKind.HollowName or EnemyKind.NameWeaver or EnemyKind.FirstKeeper or EnemyKind.OathCartographer or EnemyKind.NameBroker or EnemyKind.TideWitness or EnemyKind.ReedKeeper or EnemyKind.FlowKeeper or EnemyKind.CounterCurrent or EnemyKind.RiftCantor or EnemyKind.GlassHerald or EnemyKind.LanternRegent or EnemyKind.RouteScribe or EnemyKind.DeepSilence or EnemyKind.BorrowedChorus or EnemyKind.LastTuner;
        public float Radius => Boss ? 23 : Kind == EnemyKind.Elite ? 18 : 14;
    }
    private sealed class Drop { public PointF Pos; public string Kind = "gold"; public int Amount, Rarity, Bonus, Affix, Affix2; }
    private sealed class GroundHazard { public PointF Pos; public float Radius = 48, TimeLeft = 3.2f, TickCooldown = 0.8f; public int Damage = 9; }
    private sealed class NameRiftBurst { public PointF Pos; public float TimeLeft = 0.3f; }
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
    private readonly List<NameRiftBurst> nameRiftBursts = new();
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
    private float stoneFury, nameThread, screenShakeTime, screenShakeStrength, screenShakePhase, hitStopTime;
    private int armorTrait, relicTrait;
    private int healthTalent, skillTalent, dodgeTalent, techniqueTalent, combatDiscipline, combatMastery, combatFocus, combatApex, pendingTalents, enemiesDefeated;
    private int memoriesFound, questStage, tideGlass, finalEnding, eliteWeaponMisses;
    private int vigilBestFloor, vigilFloor;
    private VigilAspect[] vigilAspectOrder = { VigilAspect.Hardened, VigilAspect.Hungry, VigilAspect.Swift, VigilAspect.Chorus, VigilAspect.Counterflow };
    private EnemyKind[] vigilBossOrder = { EnemyKind.DeepSilence, EnemyKind.RouteScribe, EnemyKind.LanternRegent, EnemyKind.GlassHerald, EnemyKind.RiftCantor };
    private float vigilMinionCooldown, vigilTideTime;
    private bool vigilActive, vigilChoiceOpen;
    private bool highContrastTelegraphs;
    private bool reducedScreenShake;
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
        nameRiftBursts.Clear();
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
        health = maxHealth = 100; attackCd = attackFlash = skillCd = shardCd = bellCd = bellWaveTime = bellWaveRadius = pullCd = pullWaveTime = wardCd = wardTime = refrainCd = refrainWaveTime = refrainWaveRadius = beaconCd = beaconTime = beaconPulseCd = beaconWaveTime = returnVoiceCd = returnVoiceWaveTime = returnVoiceWaveRadius = finalStrikeCd = finalStrikeFlash = stoneBastionCd = stoneBastionTime = nameWeaverCd = dashCd = dashTime = invulnerable = veinResonanceTime = stoneFury = nameThread = screenShakeTime = screenShakeStrength = screenShakePhase = hitStopTime = 0; vigilMinionCooldown = vigilTideTime = 0; beaconPos = finalStrikeTarget = PointF.Empty; facing = mouseAim = dashDirection = new PointF(1, 0); playerAnimationTime = 0; echoShards.Clear(); floatingTexts.Clear(); title = false; storyPage = 0; storyOverlay = true; SetupRoom();
    }

    private void AdvanceStory()
    {
        if (!storyOverlay) return;
        storyPage++;
        if (storyPage >= prologue.Length) storyOverlay = false;
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
        if (forgeMenuOpen) { if (defenseForgeOpen) HandleDefenseForgeKey(e.KeyCode); else if (e.KeyCode == Keys.T) defenseForgeOpen = true; else if (e.KeyCode == Keys.A) { secondaryForgeOpen = !secondaryForgeOpen; notice = secondaryForgeOpen ? weaponRarities[weapon] == 2 ? "AFIXO SECUNDÁRIO • ESCOLHA UMA RESSONÂNCIA DE RELÍQUIA" : "AFIXO SECUNDÁRIO • DISPONÍVEL APENAS PARA RELÍQUIAS" : "AFIXO PRINCIPAL • TÊMPERA NORMAL"; noticeTime = 2.5f; } else if (e.KeyCode == Keys.D9 && secondaryForgeOpen) ReforgeWeapon(9); else if (e.KeyCode == Keys.D9) CycleDifficultyTier(); else if (e.KeyCode == Keys.D0) ReforgeWeapon(0); else if (e.KeyCode is >= Keys.D1 and <= Keys.D6 or Keys.D8) ReforgeWeapon((int)e.KeyCode - (int)Keys.D0); else if (e.KeyCode == Keys.D7 && secondaryForgeOpen) ReforgeWeapon(7); else if (e.KeyCode == Keys.D7) ReforgeWeapon(7); else if (e.KeyCode == Keys.Escape) { forgeMenuOpen = false; secondaryForgeOpen = false; } return; }
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

    private void TriggerNameRiftBurst(Enemy defeated)
    {
        if (!HasNameRiftPower(weapon) || !IsMarkedForRift(defeated)) return;
        const float radius = 108f;
        AddFloatingText(new PointF(defeated.Pos.X, defeated.Pos.Y - defeated.Radius - 18), "NOME RISCADO", Color.FromArgb(255, 218, 154, 242));
        TriggerScreenShake(2.6f, 0.16f);
        if (nameRiftBursts.Count >= 16) nameRiftBursts.RemoveAt(0);
        nameRiftBursts.Add(new NameRiftBurst { Pos = defeated.Pos });
        foreach (Enemy target in enemies.Where(candidate => Distance(candidate.Pos, defeated.Pos) <= radius).ToArray())
        {
            float power = WeaponPower(weapon) * (target.Boss ? 0.28f : 0.72f) * OutgoingDamageMultiplier(target) * (IsArmored(target) ? 0.75f : 1f);
            DamageEnemy(target, power);
            if (target.Hp <= 0) Defeat(target);
        }
    }

    private void Defeat(Enemy enemy)
    {
        if (!enemies.Remove(enemy)) return;
        if (ReferenceEquals(lockedTarget, enemy)) lockedTarget = enemies.OrderBy(candidate => Distance(player, candidate.Pos)).FirstOrDefault();
        TriggerNameRiftBurst(enemy);
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
        float dt = timer.Interval / 1000f;
        GameAudio.SetAmbientActive(!GameplayPaused(), room);
        if (GameplayPaused()) { Invalidate(); return; }
        if (hitStopTime > 0) { hitStopTime = Math.Max(0, hitStopTime - dt); Invalidate(); return; }
        noticeTime = Math.Max(0, noticeTime - dt);
        screenShakeTime = Math.Max(0, screenShakeTime - dt); screenShakeStrength = screenShakeTime > 0 ? screenShakeStrength * Math.Max(0, 1 - dt * 5.5f) : 0; screenShakePhase += dt * 55;
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
        TickVigilMinions(dt); TickGroundHazards(dt); TickEnemyWounds(dt); nameRiftBursts.RemoveAll(burst => (burst.TimeLeft -= dt) <= 0);
        AdvanceVoiceShards(dt);
        if (enemies.Count == 0 && drops.Count == 0 && room == 2 && !memoryOverlay) chestChoice = true;
        MarkRoomCleared();
        Invalidate();
    }

    private void UsePotion() { if (potions > 0 && health < maxHealth) { int healing = 45 + (relicOn && relicTrait == 2 ? 15 : 0); potions--; health = Math.Min(maxHealth, health + healing); notice = $"+{healing} VIDA"; noticeTime = 1.3f; } else { notice = potions == 0 ? "SEM POÇÕES" : "VIDA JÁ ESTÁ CHEIA"; noticeTime = 1.3f; } }
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
}
