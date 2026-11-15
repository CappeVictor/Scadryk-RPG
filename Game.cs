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
}
