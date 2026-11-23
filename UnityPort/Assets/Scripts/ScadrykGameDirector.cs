using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>Builds the playable three-phase, nine-room Scadryk Unity vertical slice.</summary>
public sealed partial class ScadrykGameDirector : MonoBehaviour
{
    private const int RoomsPerPhase = 3;
    internal const float FinalSealPreparationWindowSeconds = 12f;
    private const float RoomSpacing = 23f;
    private const float ConnectorCenterOffset = 11.35f;
    private const string OpeningMemory = "Depois que a Grande Onda cobriu a costa, os três sinos emudeceram. Nara, a Ouvinte, ainda escutava os nomes perdidos e seguiu a maré imóvel.";
    private static readonly GUIContent OpeningTitleContent = new GUIContent("OS TRÊS SINOS EMUDECERAM");
    private static readonly GUIContent OpeningMemoryContent = new GUIContent(OpeningMemory);
    private static readonly GUIContent WorldMapTitleContent = new GUIContent("MAPA DA JORNADA");
    private const string EndingMemory = "O Arauto quis ocultar a catástrofe para poupar os vivos da dor, mas o silêncio aprisionou os mortos. Nara escolhe lembrar e abre o limiar para que todos possam atravessar.";
    private const string VictoryEndingText = "Os sinos respondem. Nara escolheu lembrar; o limiar já não prende ninguém.";
    private const string SessionCsvHeader = "session_id,utc,phase,room,seconds,deaths,unclear_attacks,crescent,salt,tide,enemies_left,completed,current_entries,current_seconds,input_mode,seals_revealed,shallow_water_seconds,resonance_activations\n";
    private const string AttackClarityCsvHeader = "session_id,utc,phase,room,attack_patterns\n";
    private const string StoryJournalKeySuffix = "StoryJournal.v1";
    [System.Serializable]
    private sealed class StoryJournalData { public List<string> entries = new List<string>(); }
    private static readonly string[] PhaseNames = { "Margens do Primeiro Eco", "Bacia do Sal Imóvel", "Costa das Badaladas" };
    private static readonly Color[] PhaseColors = { new Color(0.11f, 0.22f, 0.20f), new Color(0.27f, 0.20f, 0.12f), new Color(0.09f, 0.17f, 0.25f) };
    private static readonly Color[] PhaseWallColors = { new Color(0.17f, 0.29f, 0.26f), new Color(0.35f, 0.27f, 0.16f), new Color(0.13f, 0.28f, 0.36f) };
    private static readonly Color[] PhaseAccentColors = { new Color(0.36f, 0.62f, 0.48f), new Color(0.78f, 0.59f, 0.30f), new Color(0.30f, 0.68f, 0.76f) };
    private static Sprite SquareSprite;
    private static Sprite CircleSprite;
    private static readonly Dictionary<string, Sprite[][]> SpriteSheetRows = new Dictionary<string, Sprite[][]>();
    private static readonly Dictionary<string, Sprite[]> WholeSpriteSheets = new Dictionary<string, Sprite[]>();
    private Sprite[] environmentSprites;
    private Sprite[] phaseGroundSprites;
    private Sprite[] phaseEnemySprites;
    private Sprite fallenWillowLogSprite;
    private Sprite portalSprite;
    private Sprite witnessSprite;
    private Texture2D titleBackdropTexture;

    private int phaseIndex, roomIndex, enemiesRemaining;
    private static bool smokeRun;
    private bool isPaused, victory, transitioning, showTitle = true, showOptions, confirmNewJourney, showOpeningStory, showWorldMap, showMemoryJournal;
    private string sessionId;
    private int rebindingAction = -1;
    private int gamepadRebindAction = -1;
    private int optionsTab, optionsSelection, overlaySelection;
    private int gamepadRebindAfterFrame;
    private int keyboardRebindAfterFrame;
    private float nextGamepadNavigationAt;
    private float transitionFade;
    private Vector2 roomCenter, exitPosition;
    private ScadrykPlayerController player;
    private ScadrykCameraFollow follow;
    private GameObject levelRoot, gateObject, witnessObject;
    private int witnessLine;
    private bool resumeFinalGuardianInterlude;
    private Coroutine finalBossSpawnRoutine;
    private bool smokeRestoreFinalGuardianForRestart;
    private ScadrykEnemyHealth finalBoss;
    private ScadrykEnemyHealth phaseGuardian;
    private GameObject worldSecret;
    private bool worldSecretHintShown;
    private readonly GameObject[] roomAnchors = new GameObject[2];
    private int anchorsRequired, anchorsActivated, hintStage;
    private bool anchorsNeedPulse;
    private bool anchorsNeedSequence;
    private readonly HashSet<string> terrainHintsShown = new HashSet<string>();
    private bool smokeProjectileCoverPassed;
    private readonly bool[] clearedRooms = new bool[RoomsPerPhase];
    private readonly List<ScadrykEnemyHealth> activeEnemies = new List<ScadrykEnemyHealth>();
    private readonly string[][] roomNames = {
        new[] { "Margem Alagada", "Vau de Cinza", "Casa das Comportas" },
        new[] { "Juncal Raso", "Bacia Imóvel", "Câmara do Sal" },
        new[] { "Costa das Badaladas", "Fileira das Bóias", "Coro da Maré Falsa" }
    };
    private static readonly string[][] MapRoomLabels = {
        new[] { "Margem", "Vau cinza", "Comporta" },
        new[] { "Juncal", "Bacia", "Câmara sal" },
        new[] { "Badaladas", "Bóias", "Coro maré" }
    };
    private static readonly string[] RoomGoals = {
        "Libertar os Ecos da margem", "Abrir a antiga comporta", "Ouvir o guardião das raízes",
        "Escutar o vestígio do sal", "Ressoar dois cristais com o Pulso", "Libertar o guardião do sal",
        "Escutar o sino encalhado", "Ressoar farol e sino do recife nessa ordem", "Enfrentar o Arauto do Vazio"
    };
    private static readonly string[] RoomStories = {
        "A água conserva vozes que Nara pensava perdidas.", "A comporta espera uma Ouvinte; raízes sobre a água cruzam o bosque.", "O guardião prende nas raízes o primeiro sino.",
        "A bacia está imóvel, mas o sal ainda se recorda da maré.", "Dois cristais guardam a rota; uma prateleira de sal contorna a bacia.", "O zelador do sal protege uma lembrança que não é sua.",
        "Na costa, as badaladas cessaram antes da última onda.", "Farol primeiro, sino depois. A vereda das bóias cruza a costa.", "O Arauto reuniu os três silêncios para esconder a verdade."
    };
    private static readonly string[] GuardianNames = { "A Raiz-Mor", "A Vigia do Sal", "O Vigia do Coro" };
    private static readonly string[] GuardianIntroductions = {
        "A Raiz-Mor: sem mim, os nomes voltam à correnteza.",
        "A Vigia do Sal: a maré ainda não pode voltar.",
        "O Vigia do Coro: os sinos devolverão a dor."
    };
    private static readonly string[] GuardianMemories = {
        "A Raiz-Mor entrega o Crescente e revela que Nima tocou o primeiro sino; seu nome ficou guardado nas raízes. O arco do Crescente interrompe Ecos.",
        "A Vigia do Sal devolve o Estouro e revela que o Arauto deteve a última onda, deixando a costa presa no instante em que perdeu suas vozes. A explosão do Estouro expõe alvos a golpes seguintes.",
        "O Vigia do Coro entrega o Rompante e revela que o Arauto mandou calar os sinos para poupar os vivos. A onda do Rompante afasta Ecos comuns."
    };
    private static readonly string[] BasinWitnessLines = {
        "Sou Nima, a voz do primeiro sino. A Raiz-Mor guardou meu nome para que o silêncio não me apagasse. Ainda lembro o Arauto chamando esse silêncio de abrigo.",
        "Nara: Também tenho medo do que a lembrança traz. Mas não vou pedir que vocês desapareçam.",
        "O sal ainda segura a onda no instante em que ela partiu. Faça os cristais responderem; a água precisa voltar a seguir."
    };
    private static readonly string[] CoastWitnessLines = {
        "Você devolveu movimento ao sal. Agora os sinos lembram que cada voz tinha alguém esperando por ela.",
        "Nara: Posso lembrar e seguir em frente. Vou abrir a passagem para que ninguém fique preso.",
        "O Arauto chama o silêncio de abrigo. Pergunte a quem perdeu o próprio nome se isso parece paz."
    };
    private static string PowerAcquisitionHint(int powerIndex, string binding) => powerIndex switch
    {
        0 => "Crescente (" + binding + "): arco frontal interrompe; revela selos.",
        1 => "Estouro (" + binding + "): explosão circular expõe; revela selos.",
        _ => "Rompante (" + binding + "): onda longa afasta; revela selos."
    };
    private string statusMessage = "Derrote os ecos para abrir a passagem.";
    private float statusUntil;
    private string memoryText;
    private float memoryUntil;
    private bool memoryModal;
    private int memoryDismissedFrame = -1;
    private Vector3 memoryLastMousePosition;
    private float roomActiveSeconds;
    private float progressHintSeconds;
    private float nextOpeningAttackSlotAt;
    private int roomDeaths;
    private int unclearAttackMarks;
    private string sessionInputMode = "unknown";
    private bool roomSessionRecorded;
    private readonly int[] roomPowerUses = new int[3];
    private int coastalCurrentEntries;
    private int roomSecretsRevealed;
    private float coastalCurrentSeconds;
    private float shallowWaterSeconds;
    private int resonanceActivations;
    private GUIStyle titleStyle { get; set; }
    private GUIStyle bodyStyle { get; set; }
    private GUIStyle statusStyle { get; set; }
    private GUIStyle smallStyle { get; set; }
    private GUIStyle hudObjectiveStyle { get; set; }
    private GUIStyle openingTitleStyle { get; set; }
    private GUIStyle openingBodyStyle { get; set; }
    private GUIStyle offscreenTargetStyle { get; set; }
    private GUIStyle centeredTitleStyle { get; set; }
    private GUIStyle centeredBodyStyle { get; set; }
    private GUIStyle worldMapRoomStyle { get; set; }
    private GUIStyle worldMapStateStyle { get; set; }
    private GUIStyle worldMapObjectiveStyle { get; set; }
    private GUIStyle worldMapInstructionStyle { get; set; }
    private GUIStyle controlHintStyle { get; set; }
    private GUIStyle finalBossLabelStyle { get; set; }
    private GUIStyle memoryStyle { get; set; }
    private float cachedTextScale { get; set; } = -1f;

    public bool IsPaused => isPaused || showTitle || showOptions || victory || transitioning || showOpeningStory || showWorldMap || showMemoryJournal || memoryModal || MemoryDismissedThisFrame(memoryDismissedFrame, Time.frameCount);
    public int PhaseIndex => phaseIndex;
    public int RoomIndex => roomIndex;
    public int EnemiesRemaining => enemiesRemaining;
    public Vector2 CurrentRoomCenter => roomCenter;
    public Vector2 ExitPosition => exitPosition;
    public Transform ActiveRoomRoot => levelRoot != null ? levelRoot.transform : null;

    private void Start()
    {
#if DEBUG
        smokeRun = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-scadryk-smoke") >= 0;
#endif
        ScadrykSettings.ClearReservedBindings();
        sessionId = System.Guid.NewGuid().ToString("N");
        Debug.Log("SCADRYK_SESSION: " + sessionId);
        EnsureSprites();
        environmentSprites = LoadSheetRow("ScadrykEnvironment", 0, true);
        phaseGroundSprites = new[] { LoadSingleSprite("ForestClearing", 80f), LoadSingleSprite("SaltBasin", 80f), LoadSingleSprite("BellCoast", 80f) };
        phaseEnemySprites = new[] { LoadSingleSprite("Rootbound", 1200f), LoadSingleSprite("SaltCrawler", 1200f), LoadSingleSprite("BellWraith", 1200f) };
        fallenWillowLogSprite = LoadSingleSprite("FallenWillowLog", 300f);
        portalSprite = LoadSingleSprite("AncientPortal", 600f);
        witnessSprite = LoadSingleSprite("DrownedMemory", 280f);
        titleBackdropTexture = Resources.Load<Texture2D>("ScadrykArt/ScadrykTitleBackdrop");
        SetupCamera();
        gameObject.AddComponent<ScadrykAudio>();
        int savedPhase = 0, savedRoom = 0;
        if (!smokeRun) TryLoadCheckpoint("Scadryk.", out savedPhase, out savedRoom);
        BuildPhase(savedPhase, savedRoom, true);
        for (int i = 0; i < PhaseNames.Length; i++)
            if (ShouldRestoreAbility(i, savedPhase, !smokeRun && HasGuardianDefeatProgress("Scadryk.", i))) player.UnlockAbility(i);
        if (!smokeRun) RestorePowerEchoes("Scadryk.", player);
#if DEBUG
        if (smokeRun) { showTitle = false; StartCoroutine(SmokeCampaign()); }
#endif
        ApplyPauseTimeScale();
    }

    private void Update()
    {
        ApplyPauseTimeScale();
        ScadrykAudio audio = GetComponent<ScadrykAudio>();
        if (audio != null) audio.SetCombatIntensity(ScadrykAudio.CombatIntensityFor(activeEnemies.Count,
            phaseGuardian != null || finalBoss != null, IsPaused));
        if (!showTitle && !isPaused && !showOptions && !victory && !transitioning && !showOpeningStory && !showWorldMap)
            roomActiveSeconds += Time.deltaTime;
        if (ShouldAccumulateProgressHint(IsPaused, enemiesRemaining, anchorsActivated, anchorsRequired, statusUntil, Time.time))
            progressHintSeconds += Time.deltaTime;
        else if (enemiesRemaining > 0 || anchorsActivated >= anchorsRequired) progressHintSeconds = 0f;
        if (transitioning) return;
        if (showOpeningStory)
        {
            bool keyboardActivity = Input.anyKeyDown || Input.GetMouseButtonDown(0);
            bool gamepadActivity = GamepadActivityThisFrame();
            sessionInputMode = MemoryInputModeAfterActivity(sessionInputMode, keyboardActivity, gamepadActivity);
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || ScadrykSettings.Down(6) ||
                ScadrykSettings.PadButtonDown(0) || ScadrykSettings.PadDown(6)) DismissOpeningStory();
            return;
        }
        if (showMemoryJournal)
        {
            bool closeJournal = Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Return) ||
                ScadrykSettings.PadButtonDown(1) || ScadrykSettings.PadButtonDown(7);
            if (closeJournal)
            {
                showMemoryJournal = false;
                ApplyPauseTimeScale();
                return;
            }
            float navigationY = ScadrykSettings.ReadGamepadStick("Scadryk Move X", "Scadryk Move Y").y;
            bool scrollUp = Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || navigationY > 0.45f;
            bool scrollDown = Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S) || navigationY < -0.45f;
            if (Time.unscaledTime >= nextMemoryJournalNavigationAt && (scrollUp || scrollDown))
            {
                memoryJournalScroll.y = Mathf.Max(0f, memoryJournalScroll.y + (scrollDown ? 72f : -72f));
                nextMemoryJournalNavigationAt = Time.unscaledTime + 0.18f;
            }
            if (Mathf.Abs(navigationY) < 0.2f) nextMemoryJournalNavigationAt = 0f;
            return;
        }
        if (showWorldMap)
        {
            if (Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.Escape) ||
                ScadrykSettings.PadButtonDown(8) || ScadrykSettings.PadButtonDown(7))
            { showWorldMap = false; ApplyPauseTimeScale(); }
            return;
        }
        if (memoryModal)
        {
            bool keyboardActivity = Input.anyKeyDown || Input.mousePosition != memoryLastMousePosition;
            bool gamepadActivity = GamepadActivityThisFrame();
            sessionInputMode = MemoryInputModeAfterActivity(sessionInputMode, keyboardActivity, gamepadActivity);
            memoryLastMousePosition = Input.mousePosition;
            if (MemoryDismissRequested(Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || ScadrykSettings.PadButtonDown(0),
                ScadrykSettings.Down(6) || ScadrykSettings.PadDown(6),
                Input.GetKeyDown(KeyCode.Escape) || ScadrykSettings.PadButtonDown(1) || ScadrykSettings.PadButtonDown(7)))
                DismissMemory();
            return;
        }
        if (!IsPaused && (Input.GetKeyDown(KeyCode.M) || ScadrykSettings.PadButtonDown(8)))
        {
            showWorldMap = true;
            ApplyPauseTimeScale();
            return;
        }
        if ((showTitle || isPaused) && !showOptions && Input.GetKeyDown(KeyCode.O))
        {
            showOptions = true; optionsTab = 0; optionsSelection = 0; gamepadRebindAction = rebindingAction = -1;
        }
        if (victory && (Input.GetKeyDown(KeyCode.Escape) || ScadrykSettings.PadButtonDown(1) || ScadrykSettings.PadButtonDown(7)))
            ReturnToTitle();
        else if (!victory && (Input.GetKeyDown(KeyCode.Escape) || ScadrykSettings.PadButtonDown(7)))
        {
            if (showOptions && RebindingCanBeCancelled(rebindingAction, gamepadRebindAction))
            {
                rebindingAction = gamepadRebindAction = -1;
                ShowStatus("Remapeamento cancelado.", 2f);
            }
            else if (showOptions) { showOptions = false; rebindingAction = gamepadRebindAction = -1; }
            else if (confirmNewJourney) { confirmNewJourney = false; overlaySelection = 0; }
            else if (!showTitle) isPaused = !isPaused;
        }
        if (showOptions || showTitle || isPaused || victory)
        {
            UpdateKeyboardMenus();
            UpdateGamepadMenus();
        }
        if (!IsPaused && gateObject == null && enemiesRemaining == 0 && anchorsActivated >= anchorsRequired)
            UnlockExit();
        if (ShouldShowProgressHint(IsPaused, enemiesRemaining, anchorsActivated, anchorsRequired, progressHintSeconds, hintStage, statusUntil, Time.time))
        {
            ShowStatus(ProgressHint(phaseIndex * RoomsPerPhase + roomIndex, anchorsActivated, hintStage), 7f);
            hintStage++;
            progressHintSeconds = 0f;
        }
        if (!IsPaused) TryShowWorldSecretHint();
        if (!IsPaused && Input.GetKeyDown(KeyCode.F8))
        {
            unclearAttackMarks++;
            string attackPatterns = RecentAttackReadabilityLabels();
            RecordAttackClarityEvent(attackPatterns);
            ShowStatus(attackPatterns == "não identificado"
                ? "Ataque difícil registrado; nenhum padrão recente pôde ser identificado."
                : "Ataque difícil registrado: " + attackPatterns, 4f);
        }
        ApplyPauseTimeScale();
    }

    private void ApplyPauseTimeScale()
    {
        Time.timeScale = showTitle || isPaused || showOptions || victory || showOpeningStory || showWorldMap || showMemoryJournal || memoryModal ||
            MemoryDismissedThisFrame(memoryDismissedFrame, Time.frameCount) ? 0f : 1f;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (smokeRun || showOpeningStory || showWorldMap || memoryModal || !FocusLossRequiresPause(hasFocus, showTitle, isPaused, showOptions, victory, transitioning)) return;
        isPaused = true;
        ApplyPauseTimeScale();
    }

    internal static bool FocusLossRequiresPause(bool hasFocus, bool title, bool paused, bool options, bool won, bool portalTransition) =>
        !hasFocus && !title && !paused && !options && !won && !portalTransition;

    internal static bool RebindingCanBeCancelled(int keyboardAction, int gamepadAction) =>
        keyboardAction >= 0 || gamepadAction >= 0;
    internal static bool CanCaptureRebindKey(int frame, int captureStartedFrame) => frame > captureStartedFrame;

    internal static bool MemoryDismissRequested(bool confirm, bool interact, bool cancel) => confirm || interact || cancel;
    internal static bool MemoryDismissedThisFrame(int dismissedFrame, int currentFrame) => dismissedFrame >= 0 && dismissedFrame == currentFrame;
    private static bool GamepadActivityThisFrame() => ScadrykSettings.PadDown(0) || ScadrykSettings.PadDown(1) || ScadrykSettings.PadButtonDown(7) ||
        ScadrykSettings.PadDown(2) || ScadrykSettings.PadDown(3) || ScadrykSettings.PadDown(4) || ScadrykSettings.PadDown(5) || ScadrykSettings.PadDown(6) ||
        ScadrykSettings.ReadGamepadStick("Scadryk Move X", "Scadryk Move Y").sqrMagnitude > 0.09f ||
        ScadrykSettings.ReadGamepadStick("Scadryk Aim X", "Scadryk Aim Y").sqrMagnitude > 0.09f;
    internal static string MemoryInputModeAfterActivity(string current, bool keyboardActivity, bool gamepadActivity) =>
        ResolveInputMode(ResolveInputMode(current, keyboardActivity ? "keyboard_mouse" : null), gamepadActivity ? "gamepad" : null);
    internal static string MemoryDismissHintText(bool gamepad, string interactBinding) => MemoryDismissHintText(gamepad, interactBinding, false);
    internal static string MemoryDismissHintText(bool gamepad, string interactBinding, bool compact) => gamepad
        ? compact ? "A / " + interactBinding + " / B" : "A / " + interactBinding + " / B para continuar"
        : compact ? interactBinding + " / Enter / Esc" : interactBinding + " / Enter / Espaço / Esc para continuar";
    internal static string FinalGuardianInterludeHint(string powerBinding) =>
        "Rompante da Maré (" + powerBinding + ") foi aprendido. Um selo opcional espera a nordeste; liberte-o antes que o Arauto desperte.";

    internal static int MovePauseGridSelection(int selection, int horizontalDirection, int verticalDirection)
    {
        int row = Mathf.Clamp(selection, 0, 5) / 2;
        int column = Mathf.Clamp(selection, 0, 5) % 2;
        if (verticalDirection != 0) row = Mathf.Clamp(row + System.Math.Sign(verticalDirection), 0, 2);
        if (horizontalDirection != 0) column = horizontalDirection > 0 ? 1 : 0;
        return row * 2 + column;
    }

    internal static int MoveVictoryMenuSelection(int selection, int verticalDirection) =>
        Mathf.Clamp(selection + System.Math.Sign(verticalDirection), 0, 2);

    internal static int TitleMenuMaximumSelection(bool hasCheckpoint) => hasCheckpoint ? 3 : 2;
    internal static int TitleMenuOptionsSelection(bool hasCheckpoint) => hasCheckpoint ? 2 : 1;
    internal static int TitleMenuJournalSelection(bool hasCheckpoint) => hasCheckpoint ? 3 : 2;

    private void UpdateKeyboardMenus()
    {
        if (rebindingAction >= 0 || gamepadRebindAction >= 0) return;
        bool up = Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W);
        bool down = Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S);
        bool left = Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A);
        bool right = Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D);

        if (showOptions)
        {
            if (optionsSelection < 3 && (left || right))
            {
                optionsTab = (optionsTab + (right ? 1 : 2)) % 3;
                optionsSelection = optionsTab;
            }
            else if (optionsTab == 0 && (left || right))
            {
                float step = right ? 0.05f : -0.05f;
                if (optionsSelection == 3) ScadrykSettings.MusicVolume += step;
                else if (optionsSelection == 4) ScadrykSettings.EffectsVolume += step;
                else if (optionsSelection == 5) { ScadrykSettings.TextScale += step; titleStyle = null; }
            }
            else if (optionsTab == 2 && optionsSelection == 11 && (left || right))
                ScadrykSettings.GamepadDeadzone += right ? 0.02f : -0.02f;
            if (up || down)
            {
                int max = optionsTab == 0 ? 9 : optionsTab == 1 ? 15 : 12;
                optionsSelection = Mathf.Clamp(optionsSelection + (down ? 1 : -1), 0, max);
            }
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
                SubmitGamepadOptions();
            return;
        }

        if (!showTitle && !isPaused && !victory && !confirmNewJourney) return;
        if (isPaused && !showTitle && !victory && !confirmNewJourney)
        {
            if (up || down)
            {
                overlaySelection = MovePauseGridSelection(overlaySelection, 0, down ? 1 : -1);
            }
            if (left || right)
                overlaySelection = MovePauseGridSelection(overlaySelection, right ? 1 : -1, 0);
        }
        else if (up || down)
        {
            int max = confirmNewJourney ? 1 : victory ? 2 : showTitle ? TitleMenuMaximumSelection(HasCheckpoint("Scadryk.")) : 0;
            overlaySelection = victory ? MoveVictoryMenuSelection(overlaySelection, down ? 1 : -1) :
                Mathf.Clamp(overlaySelection + (down ? 1 : -1), 0, max);
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            SubmitGamepadOverlay();
    }

    private void UpdateGamepadMenus()
    {
        if (gamepadRebindAction >= 0)
        {
            if (Time.frameCount > gamepadRebindAfterFrame)
            {
                for (int button = 0; button < 20; button++)
                {
                    if (!ScadrykSettings.CanBindPadButton(button)) continue; // Start pauses; L3 opens the map.
                    if (!ScadrykSettings.PadButtonDown(button)) continue;
                    ScadrykSettings.BindPad(gamepadRebindAction, button);
                    gamepadRebindAction = -1;
                    ShowStatus("Botão do controle remapeado.", 2f);
                    break;
                }
            }
            return;
        }

        if (rebindingAction >= 0)
        {
            if (ScadrykSettings.PadButtonDown(1)) rebindingAction = -1;
            return;
        }

        Vector2 navigation = ScadrykSettings.ReadGamepadStick("Scadryk Move X", "Scadryk Move Y");
        if (Time.unscaledTime >= nextGamepadNavigationAt && Mathf.Abs(navigation.y) > 0.55f)
        {
            if (showOptions)
            {
                int max = optionsTab == 0 ? 9 : optionsTab == 1 ? 15 : 12;
                optionsSelection = Mathf.Clamp(optionsSelection - (navigation.y > 0f ? 1 : -1), 0, max);
            }
            else if (!showTitle && !victory && !confirmNewJourney)
            {
                overlaySelection = MovePauseGridSelection(overlaySelection, 0, navigation.y > 0f ? -1 : 1);
            }
            else if (victory) overlaySelection = MoveVictoryMenuSelection(overlaySelection, navigation.y > 0f ? -1 : 1);
            else overlaySelection = Mathf.Clamp(overlaySelection - (navigation.y > 0f ? 1 : -1), 0,
                confirmNewJourney ? 1 : showTitle ? TitleMenuMaximumSelection(HasCheckpoint("Scadryk.")) : 4);
            nextGamepadNavigationAt = Time.unscaledTime + 0.2f;
        }
        else if (!showOptions && !showTitle && !victory && !confirmNewJourney && Time.unscaledTime >= nextGamepadNavigationAt && Mathf.Abs(navigation.x) > 0.55f)
        {
            overlaySelection = MovePauseGridSelection(overlaySelection, navigation.x > 0f ? 1 : -1, 0);
            nextGamepadNavigationAt = Time.unscaledTime + 0.2f;
        }
        else if (Mathf.Abs(navigation.y) < 0.2f && Mathf.Abs(navigation.x) < 0.2f) nextGamepadNavigationAt = 0f;

        if (showOptions && optionsSelection < 3 && Time.unscaledTime >= nextGamepadNavigationAt && Mathf.Abs(navigation.x) > 0.55f)
        {
            optionsTab = (optionsTab + (navigation.x > 0f ? 1 : 2)) % 3;
            optionsSelection = optionsTab;
            nextGamepadNavigationAt = Time.unscaledTime + 0.2f;
        }
        else if (showOptions && (optionsTab == 0 || optionsTab == 2) && Time.unscaledTime >= nextGamepadNavigationAt && Mathf.Abs(navigation.x) > 0.55f)
        {
            float step = navigation.x > 0f ? 0.05f : -0.05f;
            if (optionsSelection == 3) ScadrykSettings.MusicVolume += step;
            else if (optionsSelection == 4) ScadrykSettings.EffectsVolume += step;
            else if (optionsSelection == 5) { ScadrykSettings.TextScale += step; titleStyle = null; }
            else if (optionsTab == 2 && optionsSelection == 11) ScadrykSettings.GamepadDeadzone += navigation.x > 0f ? 0.02f : -0.02f;
            nextGamepadNavigationAt = Time.unscaledTime + 0.16f;
        }

        if (showOptions)
        {
            if (ScadrykSettings.PadButtonDown(0)) SubmitGamepadOptions();
            if (ScadrykSettings.PadButtonDown(1)) { showOptions = false; rebindingAction = gamepadRebindAction = -1; }
            return;
        }

        if (ScadrykSettings.PadButtonDown(0)) SubmitGamepadOverlay();
        if (ScadrykSettings.PadButtonDown(1) && confirmNewJourney) { confirmNewJourney = false; overlaySelection = 0; }
        else if (ScadrykSettings.PadButtonDown(1) && !showTitle && !victory) isPaused = false;
    }

    private void SubmitGamepadOverlay()
    {
        if (victory)
        {
            if (overlaySelection == 0) StartNewGame();
            else if (overlaySelection == 1) OpenMemoryJournal();
            else ReturnToTitle();
            return;
        }
        if (confirmNewJourney)
        {
            confirmNewJourney = false;
            if (overlaySelection == 0) StartNewGame();
            else overlaySelection = 0;
            return;
        }
        if (showTitle && overlaySelection == 0)
        {
            BeginJourney();
        }
        else if (showTitle)
        {
            bool hasCheckpoint = HasCheckpoint("Scadryk.");
            if (hasCheckpoint && overlaySelection == 1) RequestNewGame();
            else if (overlaySelection == TitleMenuOptionsSelection(hasCheckpoint))
            { showOptions = true; optionsTab = optionsSelection = 0; }
            else if (overlaySelection == TitleMenuJournalSelection(hasCheckpoint)) OpenMemoryJournal();
        }
        else if (overlaySelection == 0) isPaused = false;
        else if (overlaySelection == 1) { showWorldMap = true; isPaused = false; }
        else if (overlaySelection == 2) RequestNewGame();
        else if (overlaySelection == 3) { showOptions = true; optionsTab = 0; optionsSelection = 0; }
        else if (overlaySelection == 4) OpenMemoryJournal();
        else if (overlaySelection == 5) ReturnToTitle();
        else ReturnToTitle();
    }

    private void SubmitGamepadOptions()
    {
        if (optionsSelection < 3) { optionsTab = optionsSelection; return; }
        if (optionsTab == 0)
        {
            if (optionsSelection == 6) ScadrykSettings.ReduceFlash = !ScadrykSettings.ReduceFlash;
            else if (optionsSelection == 7) ScadrykSettings.ReduceShake = !ScadrykSettings.ReduceShake;
            else if (optionsSelection == 8) ScadrykSettings.HighContrastWarnings = !ScadrykSettings.HighContrastWarnings;
            else if (optionsSelection == 9) showOptions = false;
        }
        else if (optionsTab == 1)
        {
            if (optionsSelection is >= 3 and <= 13)
            {
                rebindingAction = optionsSelection - 3;
                keyboardRebindAfterFrame = Time.frameCount;
            }
            else if (optionsSelection == 14)
            {
                ScadrykSettings.ResetKeyboardBindings();
                rebindingAction = -1;
                ShowStatus("Teclas padrão restauradas.", 2f);
            }
            else if (optionsSelection == 15) showOptions = false;
        }
        else if (optionsTab == 2)
        {
            if (optionsSelection is >= 3 and <= 9)
            {
                gamepadRebindAction = optionsSelection - 3;
                gamepadRebindAfterFrame = Time.frameCount;
            }
            else if (optionsSelection == 10)
            {
                ScadrykSettings.ResetGamepadBindings();
                gamepadRebindAction = -1;
                ShowStatus("Controle padrão restaurado.", 2f);
            }
            else if (optionsSelection == 12) showOptions = false;
        }
    }

    private void StartNewGame()
    {
        resumeFinalGuardianInterlude = false;
        confirmNewJourney = false;
        showOptions = false;
        isPaused = false;
        showWorldMap = false;
        showMemoryJournal = false;
        showOpeningStory = true;
        ApplyPauseTimeScale();
        PlayerPrefs.DeleteKey("Scadryk.Phase");
        PlayerPrefs.DeleteKey("Scadryk.Room");
        ClearCampaignProgress("Scadryk.");
        ClearStoryJournal("Scadryk.");
        PlayerPrefs.Save();
        if (player != null) player.ResetAbilities();
        if (player != null) player.ResetCombatCooldowns();
        BuildPhase(0, 0, true);
        showTitle = false;
        victory = false;
        ApplyPauseTimeScale();
    }

    private void RequestNewGame()
    {
        RequestNewGame(HasCheckpoint("Scadryk."));
    }

    private void RequestNewGame(bool hasSavedProgress)
    {
        if (hasSavedProgress)
        {
            confirmNewJourney = true;
            overlaySelection = 0;
            return;
        }
        StartNewGame();
    }

    private void BeginJourney()
    {
        bool firstStart = showTitle && !HasCheckpoint("Scadryk.");
        confirmNewJourney = false;
        overlaySelection = 0;
        showTitle = false;
        isPaused = false;
        if (firstStart)
        {
            showOpeningStory = true;
            ApplyPauseTimeScale();
            return;
        }
        StartFinalGuardianInterlude();
        StartRoomTimer();
        ApplyPauseTimeScale();
    }

    private void ReturnToTitle()
    {
        if (player != null && !victory) SaveProgress(phaseIndex, roomIndex);
        RecordRoomSession();
        showTitle = true;
        victory = false;
        isPaused = false;
        showOptions = false;
        confirmNewJourney = false;
        showOpeningStory = false;
        showWorldMap = false;
        showMemoryJournal = false;
        rebindingAction = gamepadRebindAction = -1;
        overlaySelection = 0;
        ApplyPauseTimeScale();
    }

    private void DismissOpeningStory()
    {
        showOpeningStory = false;
        if (!smokeRun) RecordStoryJournalEntry("Scadryk.", OpeningMemory);
        memoryText = null;
        memoryUntil = 0f;
        memoryModal = false;
        memoryDismissedFrame = -1;
        StartRoomTimer(true);
        ApplyPauseTimeScale();
    }

    private void DismissMemory()
    {
        memoryText = null;
        memoryUntil = 0f;
        memoryModal = false;
        memoryDismissedFrame = Time.frameCount;
        memoryLastMousePosition = Input.mousePosition;
        ApplyPauseTimeScale();
    }

    private void StartRoomTimer(bool preserveInputMode = false)
    {
        string activeInputMode = sessionInputMode;
        roomActiveSeconds = 0f;
        progressHintSeconds = 0f;
        sessionInputMode = preserveInputMode ? activeInputMode : "unknown";
        roomSessionRecorded = false;
    }

    internal static string FormatInputBinding(KeyCode key, int padButton, bool useGamepad) =>
        useGamepad ? ScadrykSettings.PadButtonName(padButton) : ScadrykSettings.KeyName(key);
    private string InputBinding(int action) => FormatInputBinding(ScadrykSettings.Key(action), ScadrykSettings.PadButton(action), sessionInputMode == "gamepad");

    private string ProgressHint(int roomId, int activated, int stage)
    {
        if (roomId == 7)
        {
            if (activated > 0) return "Pista: o farol já respondeu. Vá ao sino do recife, à direita, e use o Pulso (" + InputBinding(2) + ") perto dele.";
            if (stage == 0) return "Pista: os dois sinais da costa respondem ao Pulso (" + InputBinding(2) + "). O farol precisa tocar primeiro.";
            if (stage == 1) return "Pista: procure o farol à esquerda do centro da sala e use o Pulso (" + InputBinding(2) + ") quando estiver perto.";
            return "Pista: acione primeiro o farol à esquerda; depois, o sino do recife fica à direita.";
        }

        if (roomId == 4)
        {
            if (stage == 0) return "Pista: dois cristais marcados guardam a travessia. Aproxime-se e use o Pulso (" + InputBinding(2) + ") em cada um.";
            if (stage == 1) return "Pista: o Pulso (" + InputBinding(2) + ") alcança os cristais quando Nara chega perto deles.";
            return "Pista: confira a contagem no HUD; falta ressoar um dos dois cristais.";
        }

        if (roomId == 1) return stage == 0
            ? "Pista: a comporta antiga está marcada no lado direito da sala. Aproxime-se e interaja (" + InputBinding(6) + ")."
            : "Pista: procure o vestígio brilhante junto à passagem da direita e interaja (" + InputBinding(6) + ") ao lado dele.";
        if (roomId == 3) return stage == 0
            ? "Pista: há uma lembrança marcada perto do centro da bacia. Aproxime-se e interaja (" + InputBinding(6) + ")."
            : "Pista: escute o vestígio junto ao centro da bacia usando interagir (" + InputBinding(6) + ") ao lado do brilho marcado.";
        if (roomId == 6) return stage == 0
            ? "Pista: o sino encalhado está marcado perto do centro da costa. Aproxime-se e interaja (" + InputBinding(6) + ")."
            : "Pista: a lembrança da costa responde a interagir (" + InputBinding(6) + ") quando Nara chega perto do sino marcado.";
        return "Pista: siga o objetivo marcado nesta sala e aproxime-se do sinal indicado.";
    }

    internal static float ProgressHintDelay(int stage) => stage <= 0 ? 24f : 18f;

    internal static bool OpeningStoryCoversRoomStatus(bool openingStoryVisible, int phase, int room) =>
        openingStoryVisible && phase == 0 && room == 0;

    internal static bool ShouldAccumulateProgressHint(bool paused, int enemiesRemaining, int activated, int required,
        float statusUntil, float now) =>
        !paused && enemiesRemaining <= 0 && activated < required && now >= statusUntil;

    internal static bool ShouldShowProgressHint(bool paused, int enemiesRemaining, int activated, int required,
        float activeSeconds, int stage, float statusUntil, float now) =>
        ShouldAccumulateProgressHint(paused, enemiesRemaining, activated, required, statusUntil, now) &&
        activeSeconds > ProgressHintDelay(stage);

    private void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            var cameraObject = new GameObject("Main Camera");
            cam = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
        }
        cam.orthographic = true;
        cam.orthographicSize = 5.8f;
        cam.backgroundColor = new Color(0.025f, 0.035f, 0.045f);
        follow = cam.GetComponent<ScadrykCameraFollow>();
        if (follow == null) follow = cam.gameObject.AddComponent<ScadrykCameraFollow>();
    }

    private void BuildPhase(int newPhase, int newRoom, bool newGame, bool preserveRoomSession = false)
    {
        phaseGuardian = null;
        nextOpeningAttackSlotAt = 0f;
        if (levelRoot != null && !preserveRoomSession) RecordRoomSession();
        ClearRoomProjectiles();
        if (levelRoot != null) Destroy(levelRoot);
        if (gateObject != null) Destroy(gateObject);
        gateObject = null;
        if (newGame || phaseIndex != newPhase)
            System.Array.Clear(clearedRooms, 0, clearedRooms.Length);
        activeEnemies.Clear();
        for (int i = 0; i < Mathf.Clamp(newRoom, 0, RoomsPerPhase); i++) clearedRooms[i] = true;
        phaseIndex = newPhase; roomIndex = newRoom; victory = false;
        roomCenter = new Vector2(roomIndex * RoomSpacing, 0f);
        exitPosition = roomCenter + Vector2.right * 8.05f;
        levelRoot = new GameObject("Phase " + (phaseIndex + 1) + " map");
        DrawPhaseMap(levelRoot.transform);
        BuildRoomAnchors();
        BuildWitness();
        if (player == null)
        {
            var playerObject = new GameObject("Nara — player");
            playerObject.transform.position = roomCenter + Vector2.left * 6.2f;
            playerObject.transform.localScale = new Vector3(0.88f, 0.88f, 1f);
            var playerShadow = new GameObject("Soft ground shadow"); playerShadow.transform.SetParent(playerObject.transform, false);
            playerShadow.transform.localPosition = new Vector3(0.08f, -0.19f, 0f); playerShadow.transform.localScale = new Vector3(0.68f, 0.39f, 1f);
            var playerShadowRenderer = playerShadow.AddComponent<SpriteRenderer>(); playerShadowRenderer.sprite = ScadrykEffects.SoftShadowSprite; playerShadowRenderer.color = new Color(0.04f, 0.06f, 0.08f, 0.78f); playerShadowRenderer.sortingOrder = 4;
            var playerVolume = new GameObject("Raised sprite shading"); playerVolume.transform.SetParent(playerObject.transform, false);
            playerVolume.transform.localPosition = new Vector3(0f, -0.1f, 0f); playerVolume.transform.localScale = new Vector3(1.015f, 1.015f, 1f);
            var playerVolumeRenderer = playerVolume.AddComponent<SpriteRenderer>(); playerVolumeRenderer.sprite = CircleSprite; playerVolumeRenderer.color = new Color(0.025f, 0.055f, 0.07f, 0.72f); playerVolumeRenderer.sortingOrder = 4;
            var playerVisual = new GameObject("Animated visual"); playerVisual.transform.SetParent(playerObject.transform, false);
            var renderer = playerVisual.AddComponent<SpriteRenderer>(); renderer.sprite = CircleSprite; renderer.color = new Color(0.85f, 0.83f, 0.71f); renderer.sortingOrder = 5;
            playerObject.AddComponent<CircleCollider2D>().radius = 0.42f;
            playerObject.AddComponent<Rigidbody2D>();
            playerObject.AddComponent<ScadrykDepthSort>().SetBias(5);
            playerObject.AddComponent<ScadrykActorAnimator>().SetVisual(playerVisual.transform, playerVolumeRenderer);
            player = playerObject.AddComponent<ScadrykPlayerController>();
            playerObject.AddComponent<ScadrykHealthBar>().Configure(true, false, 1.25f);
            player.SetSpriteFrames(LoadSheetRow("Listener", 0, true));
        }
        else player.ResetForRoom(roomCenter + Vector2.left * 6.2f);
        BuildWorldSecret();
        follow.SetTarget(player.transform);
        bool guardianAlreadyDefeated = smokeRun
            ? smokeRestoreFinalGuardianForRestart && phaseIndex == PhaseNames.Length - 1 && roomIndex == RoomsPerPhase - 1
            : ShouldRestoreDefeatedGuardian(phaseIndex, roomIndex, HasGuardianDefeatProgress("Scadryk.", phaseIndex));
        bool finalBossAlreadyDefeated = !smokeRun && ShouldRestoreFinalBossDefeat(phaseIndex, roomIndex,
            HasFinalBossDefeatProgress("Scadryk."));
        if (guardianAlreadyDefeated || finalBossAlreadyDefeated) enemiesRemaining = 0;
        else SpawnRoomEnemies();
        GetComponent<ScadrykAudio>().SetContext(phaseIndex, false);
        if (!preserveRoomSession) ResetRoomSessionMetrics();
        hintStage = 0;
        progressHintSeconds = 0f;
        bool openingCoversFirstRoom = OpeningStoryCoversRoomStatus(showOpeningStory, phaseIndex, roomIndex);
        statusMessage = roomIndex == RoomsPerPhase - 1 ? GuardianIntroductions[phaseIndex] : RoomStories[phaseIndex * RoomsPerPhase + roomIndex];
        statusUntil = Time.time + (openingCoversFirstRoom ? 0f : roomIndex == RoomsPerPhase - 1 ? 8f : 6f);
        if (finalBossAlreadyDefeated) UnlockExit();
        else if (ShouldResumeFinalGuardianInterlude(phaseIndex, roomIndex, guardianAlreadyDefeated, finalBossAlreadyDefeated))
        {
            resumeFinalGuardianInterlude = true;
            enemiesRemaining = 1;
            statusMessage = "O último guardião já foi vencido. A lembrança aguarda Nara.";
        }
        else if (guardianAlreadyDefeated) UnlockExit();
    }

    private void ResetRoomSessionMetrics()
    {
        roomActiveSeconds = 0f;
        roomDeaths = 0;
        unclearAttackMarks = 0;
        sessionInputMode = "unknown";
        roomSessionRecorded = false;
        System.Array.Clear(roomPowerUses, 0, roomPowerUses.Length);
        coastalCurrentEntries = 0;
        coastalCurrentSeconds = 0f;
        roomSecretsRevealed = 0;
        shallowWaterSeconds = 0f;
        resonanceActivations = 0;
    }
    internal static string ObjectiveText(int phase, int room, int enemiesLeft, int anchorsDone, int anchorsNeeded,
        bool anchorsNeedPulse, bool anchorsNeedSequence, bool portalOpen, string combatLabel, string interactBinding, string pulseBinding)
    {
        int index = Mathf.Clamp(phase, 0, PhaseNames.Length - 1) * RoomsPerPhase + Mathf.Clamp(room, 0, RoomsPerPhase - 1);
        if (enemiesLeft > 0)
            return combatLabel + " · " + enemiesLeft + (enemiesLeft == 1 ? " restante" : " restantes");
        if (anchorsDone < anchorsNeeded)
        {
            if (anchorsNeedSequence)
                return (anchorsDone == 0 ? "Ressoar o farol" : "Ressoar o sino do recife") + " (" + anchorsDone + "/" + anchorsNeeded + ")";
            if (anchorsNeedPulse)
                return "Ressoar os cristais com o Pulso (" + pulseBinding + ", " + anchorsDone + "/" + anchorsNeeded + ")";
            return "Interagir com o vestígio marcado (" + interactBinding + ", " + anchorsDone + "/" + anchorsNeeded + ")";
        }
        if (portalOpen)
            return phase == PhaseNames.Length - 1 && room == RoomsPerPhase - 1
                ? "Atravessar o portal colossal (" + interactBinding + ")"
                : room == RoomsPerPhase - 1
                    ? "Atravessar o portal para o próximo bioma (" + interactBinding + ")"
                    : "Atravessar o portal para a próxima sala (" + interactBinding + ")";
        return RoomGoals[index];
    }

    internal static string CombatObjectiveLabel(int phase, int room, string guardianName, bool finalBossAwake, bool awaitingFinalMemory)
    {
        if (finalBossAwake) return "Derrotar o Arauto do Vazio";
        if (!string.IsNullOrWhiteSpace(guardianName)) return "Derrotar " + guardianName;
        if (awaitingFinalMemory && phase == PhaseNames.Length - 1 && room == RoomsPerPhase - 1)
            return "Escutar a última lembrança antes do despertar";
        return "Derrotar os Ecos";
    }

    internal static bool PortalCanBeEntered(bool portalExists, bool portalIsOpen) => portalExists && portalIsOpen;

    internal static float PortalDiameterFor(int phase, int room)
    {
        if (phase == PhaseNames.Length - 1 && room == RoomsPerPhase - 1) return 8.5f;
        if (room == RoomsPerPhase - 1) return 3.8f;
        return 3.15f;
    }

    private string ActiveCombatObjectiveLabel() => CombatObjectiveLabel(phaseIndex, roomIndex,
        phaseGuardian != null ? phaseGuardian.name : null, finalBoss != null,
        phaseIndex == PhaseNames.Length - 1 && roomIndex == RoomsPerPhase - 1 && enemiesRemaining > 0 && phaseGuardian == null);

    private string ActiveObjectiveText() => ObjectiveText(phaseIndex, roomIndex, enemiesRemaining, anchorsActivated, anchorsRequired,
        anchorsNeedPulse, anchorsNeedSequence, gateObject != null, ActiveCombatObjectiveLabel(), InputBinding(6), InputBinding(2));

    internal float ScheduleOpeningAttack(float requestedAt) =>
        ScadrykEnemyHealth.ReserveOpeningAttackSlot(ref nextOpeningAttackSlotAt, requestedAt);

    public void OnEnemyDefeated(ScadrykEnemyHealth enemy)
    {
        activeEnemies.Remove(enemy);
        string acquiredPower = null;
        if (enemy == phaseGuardian) phaseGuardian = null;
        if (enemy.IsFinalBoss)
        {
            if (!smokeRun) WriteFinalBossDefeatProgress("Scadryk.");
            ClearRoomProjectiles();
            finalBoss = null;
            enemiesRemaining = Mathf.Max(0, enemiesRemaining - 1);
            if (anchorsActivated >= anchorsRequired) UnlockExit();
            statusMessage = "O Arauto do Vazio caiu! Um portal colossal se abriu.";
            statusUntil = Time.time + 9f;
            ShowMemory(EndingMemory, 10f);
            return;
        }

        if (enemy.IsBoss && player != null)
        {
            player.UnlockAbility(enemy.BiomeIndex);
            if (!smokeRun) WriteGuardianDefeatProgress("Scadryk.", enemy.BiomeIndex);
            string[] powers = { "Crescente Verdejante", "Estouro de Sal", "Rompante da Maré" };
            int powerIndex = Mathf.Clamp(enemy.BiomeIndex, 0, powers.Length - 1);
            if (worldSecret != null && RequiredWorldSecretPower() == powerIndex) worldSecretHintShown = false;
            acquiredPower = PowerAcquisitionHint(powerIndex, InputBinding(powerIndex + 3));
            ShowMemory(GuardianMemories[powerIndex], 7f);
        }

        enemiesRemaining = Mathf.Max(0, enemiesRemaining - 1);
        if (enemiesRemaining == 0)
        {
            ClearRoomProjectiles();
            if (phaseIndex == PhaseNames.Length - 1 && roomIndex == RoomsPerPhase - 1 && enemy.IsBoss)
            {
                if (player != null) player.RestoreHealth(player.MaxHealth);
                enemiesRemaining = 1;
                ShowStatus(acquiredPower != null ? FinalGuardianInterludeHint(InputBinding(5)) :
                    "Nara escuta a última lembrança antes que algo desperte além dos sinos.", 7f);
                QueueFinalBossSpawnAfterMemory();
                return;
            }
            statusMessage = anchorsActivated >= anchorsRequired ? "Sala limpa. Interaja com a passagem (" + InputBinding(6) + ")." :
                anchorsNeedPulse ? "Sala limpa. Faça os objetos marcados ressoarem com o Pulso (" + InputBinding(2) + ")." :
                "Sala limpa. Interaja com o vestígio marcado (" + InputBinding(6) + ").";
            statusUntil = Time.time + 8f;
            if (player != null) player.RestoreHealth(22f);
            UnlockExit();
            if (acquiredPower != null) ShowStatus(acquiredPower, 7f);
        }
    }

    public void RecordPowerUse(int index)
    {
        if (index >= 0 && index < roomPowerUses.Length) roomPowerUses[index]++;
    }

    public void RecordCoastalCurrentEntry()
    {
        if (!roomSessionRecorded)
            coastalCurrentEntries = AddCoastalCurrentEntry(coastalCurrentEntries, phaseIndex, roomIndex);
    }

    public void RecordCoastalCurrentExposure(float seconds)
    {
        if (!roomSessionRecorded)
            coastalCurrentSeconds = AddCoastalCurrentExposure(coastalCurrentSeconds, phaseIndex, roomIndex, seconds);
    }

    public void RecordShallowWaterExposure(float seconds)
    {
        if (!roomSessionRecorded)
            shallowWaterSeconds = AddShallowWaterExposure(shallowWaterSeconds, phaseIndex, seconds);
    }

    public void RecordResonanceActivation()
    {
        if (!roomSessionRecorded)
            resonanceActivations = AddResonanceActivation(resonanceActivations, phaseIndex, roomIndex);
    }

    internal static bool IsCoastalCurrentMetricsRoom(int phase, int room) => phase == 2 && room == 1;

    internal static int AddCoastalCurrentEntry(int currentEntries, int phase, int room) =>
        IsCoastalCurrentMetricsRoom(phase, room) ? currentEntries + 1 : currentEntries;

    internal static float AddCoastalCurrentExposure(float currentSeconds, int phase, int room, float seconds) =>
        IsCoastalCurrentMetricsRoom(phase, room) ? currentSeconds + Mathf.Max(0f, seconds) : currentSeconds;

    internal static bool IsShallowWaterMetricsBiome(int phase) => phase == 0;

    internal static float AddShallowWaterExposure(float currentSeconds, int phase, float seconds) =>
        IsShallowWaterMetricsBiome(phase) ? currentSeconds + Mathf.Max(0f, seconds) : currentSeconds;

    internal static bool IsResonanceMetricsRoom(int phase, int room) =>
        (phase == 1 && room == 1) || (phase == 2 && room == 1);

    internal static int AddResonanceActivation(int currentCount, int phase, int room) =>
        Mathf.Max(0, currentCount) + (IsResonanceMetricsRoom(phase, room) ? 1 : 0);

    internal static int SecretRevealCountAfterSuccess(int currentCount, bool smokeMode) =>
        Mathf.Max(0, currentCount) + (smokeMode ? 0 : 1);

    public void RecordInputMode(string mode)
    {
        sessionInputMode = ResolveInputMode(sessionInputMode, mode);
    }

    internal static string ResolveInputMode(string current, string candidate) =>
        candidate == "keyboard_mouse" || candidate == "gamepad" ? candidate : current;

    private string FormatSessionCsvRow(bool completed)
    {
        return sessionId + "," + System.DateTime.UtcNow.ToString("o") + "," + (phaseIndex + 1) + "," + (roomIndex + 1) + "," +
            Mathf.RoundToInt(Mathf.Max(0f, roomActiveSeconds)) + "," + roomDeaths + "," + unclearAttackMarks + "," + roomPowerUses[0] + "," +
            roomPowerUses[1] + "," + roomPowerUses[2] + "," + enemiesRemaining + "," + completed.ToString().ToLowerInvariant() + "," +
            coastalCurrentEntries + "," + Mathf.RoundToInt(Mathf.Max(0f, coastalCurrentSeconds)) + "," + sessionInputMode + "," + roomSecretsRevealed + "," +
            Mathf.RoundToInt(Mathf.Max(0f, shallowWaterSeconds)) + "," + resonanceActivations + "\n";
    }

    private void RecordRoomSession()
    {
        if (smokeRun || showTitle || roomSessionRecorded) return;
        roomSessionRecorded = true;
        try
        {
            string path = Path.Combine(Application.persistentDataPath, "scadryk-sessions-v8.csv");
            if (!File.Exists(path)) File.WriteAllText(path, SessionCsvHeader);
            bool completed = IsRoomMarkedComplete(clearedRooms, roomIndex);
            File.AppendAllText(path, FormatSessionCsvRow(completed));
        }
        catch (System.Exception exception) { Debug.LogWarning("Scadryk: não foi possível registrar a sessão: " + exception.Message); }
    }

    private string RecentAttackReadabilityLabels()
    {
        var labels = new List<string>();
        foreach (ScadrykEnemyHealth enemy in FindObjectsByType<ScadrykEnemyHealth>())
        {
            if (enemy == null || !enemy.TryGetRecentAttackLabel(Time.time, out string label) || labels.Contains(label)) continue;
            labels.Add(label);
        }
        if (labels.Count == 0) return "não identificado";
        labels.Sort(System.StringComparer.Ordinal);
        return string.Join(" | ", labels.ToArray());
    }

    private void RecordAttackClarityEvent(string patterns)
    {
        if (smokeRun) return;
        try
        {
            string path = Path.Combine(Application.persistentDataPath, "scadryk-attack-clarity-v1.csv");
            if (!File.Exists(path)) File.WriteAllText(path, AttackClarityCsvHeader);
            File.AppendAllText(path, FormatAttackClarityEventCsvRow(sessionId, System.DateTime.UtcNow.ToString("o"),
                phaseIndex + 1, roomIndex + 1, patterns));
        }
        catch (System.Exception exception) { Debug.LogWarning("Scadryk: não foi possível registrar o padrão do ataque: " + exception.Message); }
    }

    internal static string FormatAttackClarityEventCsvRow(string id, string utc, int phase, int room, string patterns) =>
        id + "," + utc + "," + phase + "," + room + "," + (string.IsNullOrWhiteSpace(patterns) ? "não identificado" : patterns) + "\n";

    private void OnApplicationQuit()
    {
        Time.timeScale = 1f;
        if (levelRoot != null && !showTitle && !showOpeningStory)
        {
            if (ShouldSaveCheckpointOnQuit(player != null, victory, showTitle, showOpeningStory, smokeRun))
                SaveProgress(phaseIndex, roomIndex);
            RecordRoomSession();
        }
    }

    internal static bool ShouldSaveCheckpointOnQuit(bool hasPlayer, bool hasVictory, bool titleVisible,
        bool openingStoryVisible, bool smokeMode) =>
        hasPlayer && !hasVictory && !titleVisible && !openingStoryVisible && !smokeMode;

    private static void SaveProgress(int phase, int room)
    {
        if (smokeRun) return;
        WriteCheckpoint("Scadryk.", phase, room);
    }

    internal static bool IsRoomMarkedComplete(bool[] cleared, int room) =>
        cleared != null && room >= 0 && room < cleared.Length && cleared[room];

    internal static bool ShouldRestoreAbility(int ability, int savedPhase, bool guardianDefeated) =>
        ability >= 0 && ability < PhaseNames.Length && savedPhase >= 0 && savedPhase < PhaseNames.Length &&
        (ability < savedPhase || guardianDefeated);

    internal static bool ShouldRestoreDefeatedGuardian(int phase, int room, bool guardianDefeated) =>
        guardianDefeated && phase >= 0 && phase < PhaseNames.Length && room == RoomsPerPhase - 1;

    internal static bool ShouldRestoreFinalBossDefeat(int phase, int room, bool finalBossDefeated) =>
        finalBossDefeated && phase == PhaseNames.Length - 1 && room == RoomsPerPhase - 1;

    internal static bool ShouldResumeFinalGuardianInterlude(int phase, int room, bool guardianDefeated, bool finalBossDefeated) =>
        ShouldRestoreDefeatedGuardian(phase, room, guardianDefeated) && phase == PhaseNames.Length - 1 &&
        !ShouldRestoreFinalBossDefeat(phase, room, finalBossDefeated);

    internal static bool ShouldContinueFinalGuardianInterludeAfterRestart(bool wasWaiting, bool rebuiltWaiting) =>
        wasWaiting || rebuiltWaiting;

    internal static string GuardianDefeatProgressKey(string keyPrefix, int guardian) =>
        keyPrefix + "GuardianDefeated." + Mathf.Clamp(guardian, 0, PhaseNames.Length - 1);

    internal static bool HasGuardianDefeatProgress(string keyPrefix, int guardian) =>
        PlayerPrefs.GetInt(GuardianDefeatProgressKey(keyPrefix, guardian), 0) == 1;

    internal static void WriteGuardianDefeatProgress(string keyPrefix, int guardian)
    {
        PlayerPrefs.SetInt(GuardianDefeatProgressKey(keyPrefix, guardian), 1);
        PlayerPrefs.Save();
    }

    internal static void ClearGuardianDefeatProgress(string keyPrefix)
    {
        for (int guardian = 0; guardian < PhaseNames.Length; guardian++)
            PlayerPrefs.DeleteKey(GuardianDefeatProgressKey(keyPrefix, guardian));
    }

    internal static string FinalBossDefeatProgressKey(string keyPrefix) => keyPrefix + "FinalBossDefeated";

    internal static bool HasFinalBossDefeatProgress(string keyPrefix) =>
        PlayerPrefs.GetInt(FinalBossDefeatProgressKey(keyPrefix), 0) == 1;

    internal static void WriteFinalBossDefeatProgress(string keyPrefix)
    {
        PlayerPrefs.SetInt(FinalBossDefeatProgressKey(keyPrefix), 1);
        PlayerPrefs.Save();
    }

    internal static void ClearFinalBossDefeatProgress(string keyPrefix) =>
        PlayerPrefs.DeleteKey(FinalBossDefeatProgressKey(keyPrefix));

    internal static void WriteCheckpoint(string keyPrefix, int phase, int room)
    {
        PlayerPrefs.SetInt(keyPrefix + "Phase", Mathf.Clamp(phase, 0, PhaseNames.Length - 1));
        PlayerPrefs.SetInt(keyPrefix + "Room", Mathf.Clamp(room, 0, RoomsPerPhase - 1));
        PlayerPrefs.Save();
    }

    internal static bool HasCheckpoint(string keyPrefix) =>
        PlayerPrefs.HasKey(keyPrefix + "Phase") && PlayerPrefs.HasKey(keyPrefix + "Room");

    internal static string WitnessProgressKey(string keyPrefix, int phase) =>
        keyPrefix + "Witness." + Mathf.Clamp(phase, 0, PhaseNames.Length - 1);

    internal static string WorldSecretProgressKey(string keyPrefix, int phase, int room) =>
        keyPrefix + "WorldSecret." + Mathf.Clamp(phase, 0, PhaseNames.Length - 1) + "." +
        Mathf.Clamp(room, 0, RoomsPerPhase - 1);

    internal static bool IsWorldSecretRevealed(string keyPrefix, int phase, int room) =>
        PlayerPrefs.GetInt(WorldSecretProgressKey(keyPrefix, phase, room), 0) == 1;

    internal static bool ShouldBuildWorldSecret(bool smokeMode, bool alreadyRevealed) =>
        smokeMode || !alreadyRevealed;

    internal static void MarkWorldSecretRevealed(string keyPrefix, int phase, int room)
    {
        PlayerPrefs.SetInt(WorldSecretProgressKey(keyPrefix, phase, room), 1);
        PlayerPrefs.Save();
    }

    internal static void ClearWorldSecretProgress(string keyPrefix)
    {
        for (int phase = 0; phase < PhaseNames.Length; phase++)
        for (int room = 0; room < RoomsPerPhase; room++)
            PlayerPrefs.DeleteKey(WorldSecretProgressKey(keyPrefix, phase, room));
    }

    internal static void ClearCampaignProgress(string keyPrefix)
    {
        ClearGuardianDefeatProgress(keyPrefix);
        ClearFinalBossDefeatProgress(keyPrefix);
        ClearWitnessProgress(keyPrefix);
        ClearWorldSecretProgress(keyPrefix);
    }

    internal static string StoryJournalProgressKey(string keyPrefix) => keyPrefix + StoryJournalKeySuffix;

    internal static string[] ReadStoryJournal(string keyPrefix)
    {
        string key = StoryJournalProgressKey(keyPrefix);
        if (!PlayerPrefs.HasKey(key)) return new string[0];
        try
        {
            StoryJournalData data = JsonUtility.FromJson<StoryJournalData>(PlayerPrefs.GetString(key));
            if (data == null || data.entries == null) return new string[0];
            var entries = new List<string>(data.entries.Count);
            foreach (string entry in data.entries)
                if (!string.IsNullOrWhiteSpace(entry) && !entries.Contains(entry)) entries.Add(entry);
            return entries.ToArray();
        }
        catch (System.Exception) { return new string[0]; }
    }

    internal static bool RecordStoryJournalEntry(string keyPrefix, string entry)
    {
        if (string.IsNullOrWhiteSpace(entry)) return false;
        var entries = new List<string>(ReadStoryJournal(keyPrefix));
        if (entries.Contains(entry)) return false;
        entries.Add(entry);
        PlayerPrefs.SetString(StoryJournalProgressKey(keyPrefix), JsonUtility.ToJson(new StoryJournalData { entries = entries }));
        PlayerPrefs.Save();
        return true;
    }

    internal static void ClearStoryJournal(string keyPrefix) => PlayerPrefs.DeleteKey(StoryJournalProgressKey(keyPrefix));

    internal static int ReadWitnessProgress(string keyPrefix, int phase) =>
        Mathf.Max(0, PlayerPrefs.GetInt(WitnessProgressKey(keyPrefix, phase), 0));

    internal static void WriteWitnessProgress(string keyPrefix, int phase, int nextLine)
    {
        PlayerPrefs.SetInt(WitnessProgressKey(keyPrefix, phase), Mathf.Max(0, nextLine));
        PlayerPrefs.Save();
    }

    internal static void ClearWitnessProgress(string keyPrefix)
    {
        for (int phase = 0; phase < PhaseNames.Length; phase++)
            PlayerPrefs.DeleteKey(WitnessProgressKey(keyPrefix, phase));
    }

    internal static bool TryLoadCheckpoint(string keyPrefix, out int phase, out int room)
    {
        if (!HasCheckpoint(keyPrefix))
        {
            phase = 0;
            room = 0;
            return false;
        }
        phase = Mathf.Clamp(PlayerPrefs.GetInt(keyPrefix + "Phase", 0), 0, PhaseNames.Length - 1);
        room = Mathf.Clamp(PlayerPrefs.GetInt(keyPrefix + "Room", 0), 0, RoomsPerPhase - 1);
        return true;
    }

    private void SpawnFinalBoss()
    {
        GetComponent<ScadrykAudio>().SetContext(phaseIndex, true);
        enemiesRemaining = 1;
        var bossObject = new GameObject("O Arauto do Vazio — superchefe final");
        bossObject.transform.SetParent(levelRoot.transform);
        bossObject.transform.position = roomCenter + new Vector2(1.2f, 0.2f);
        bossObject.transform.localScale = Vector3.one * 2.65f;
        Sprite[] frames = LoadSheetRow("BossesEndgame", 0, false);

        var shadow = new GameObject("Final boss ground shadow"); shadow.transform.SetParent(bossObject.transform, false);
        shadow.transform.localPosition = new Vector3(0.12f, -0.24f, 0f); shadow.transform.localScale = new Vector3(1.25f, 0.58f, 1f);
        var shadowRenderer = shadow.AddComponent<SpriteRenderer>(); shadowRenderer.sprite = ScadrykEffects.SoftShadowSprite; shadowRenderer.color = new Color(0.06f, 0.015f, 0.055f, 0.92f); shadowRenderer.sortingOrder = 3;

        var volume = new GameObject("Final boss depth layer"); volume.transform.SetParent(bossObject.transform, false);
        volume.transform.localPosition = new Vector3(0f, -0.14f, 0f); volume.transform.localScale = new Vector3(1.025f, 1.025f, 1f);
        var volumeRenderer = volume.AddComponent<SpriteRenderer>(); volumeRenderer.sprite = frames != null && frames.Length > 0 ? frames[0] : CircleSprite; volumeRenderer.color = new Color(0.12f, 0.015f, 0.08f, 0.88f); volumeRenderer.sortingOrder = 3;

        var visual = new GameObject("Animated visual"); visual.transform.SetParent(bossObject.transform, false);
        var renderer = visual.AddComponent<SpriteRenderer>(); renderer.sprite = frames != null && frames.Length > 0 ? frames[0] : CircleSprite; renderer.color = Color.white; renderer.sortingOrder = 4;
        bossObject.AddComponent<CircleCollider2D>().radius = 0.86f;
        bossObject.AddComponent<Rigidbody2D>();
        bossObject.AddComponent<ScadrykDepthSort>().SetBias(4);
        bossObject.AddComponent<ScadrykActorAnimator>().SetVisual(visual.transform, volumeRenderer);
        finalBoss = bossObject.AddComponent<ScadrykEnemyHealth>();
        finalBoss.ConfigureFinalBoss(frames);
        activeEnemies.Add(finalBoss);
        bossObject.AddComponent<ScadrykHealthBar>().Configure(false, true, 2.25f);
        statusMessage = PowerAcquisitionHint(2, InputBinding(5)) + " O Arauto desperta.";
        statusUntil = Time.time + 8f;
    }

    private IEnumerator SpawnFinalBossAfterMemory()
    {
        while (memoryModal || Time.time < memoryUntil) yield return null;
        if (player == null || victory || showTitle)
        {
            finalBossSpawnRoutine = null;
            yield break;
        }
        bool canOfferSealWindow = ShouldOfferFinalSealPreparation(phaseIndex, roomIndex,
            player.HasAbility(2), worldSecret != null);
        if (canOfferSealWindow)
        {
            ShowStatus("O Arauto ainda não despertou. Você tem 12 segundos para libertar o Selo de Eco a nordeste ou se preparar.",
                FinalSealPreparationWindowSeconds);
            float deadline = Time.time + FinalSealPreparationWindowSeconds;
            while (Time.time < deadline && worldSecret != null && finalBoss == null && !victory && !showTitle) yield return null;
            while ((memoryModal || Time.time < memoryUntil) && finalBoss == null && !victory && !showTitle) yield return null;
        }
        if (player == null || victory || showTitle || finalBoss != null)
        {
            finalBossSpawnRoutine = null;
            yield break;
        }
        finalBossSpawnRoutine = null;
        SpawnFinalBoss();
    }

    private void QueueFinalBossSpawnAfterMemory()
    {
        if (finalBossSpawnRoutine != null) StopCoroutine(finalBossSpawnRoutine);
        finalBossSpawnRoutine = StartCoroutine(SpawnFinalBossAfterMemory());
    }

    internal static bool ShouldOfferFinalSealPreparation(int phase, int room, bool powerUnlocked, bool sealAvailable) =>
        phase == PhaseNames.Length - 1 && room == RoomsPerPhase - 1 && powerUnlocked && sealAvailable;

    private void StartFinalGuardianInterlude()
    {
        if (!resumeFinalGuardianInterlude) return;
        resumeFinalGuardianInterlude = false;
        ShowMemory(GuardianMemories[PhaseNames.Length - 1], 7f);
        QueueFinalBossSpawnAfterMemory();
    }

    private void UnlockExit()
    {
        if (gateObject != null || anchorsActivated < anchorsRequired) return;
        exitPosition = roomCenter + Vector2.right * 8.05f;
        bool finalPortal = phaseIndex == PhaseNames.Length - 1 && roomIndex == RoomsPerPhase - 1;
        bool phasePortal = roomIndex == RoomsPerPhase - 1;
        Color portalColor = finalPortal ? new Color(0.8f, 0.26f, 1f) : PhaseAccentColors[phaseIndex];
        float portalSize = PortalDiameterFor(phaseIndex, roomIndex);
        gateObject = ScadrykPortalVisual.Spawn(levelRoot.transform, exitPosition, portalSprite, portalColor, portalSize);
        ScadrykAudio.Play(ScadrykCue.Portal);
        statusMessage = finalPortal
            ? "O portal colossal do Vazio surgiu. Aproxime-se e interaja (" + InputBinding(6) + ") para concluir a jornada."
            : phasePortal
                ? "O portal da próxima fase surgiu. Aproxime-se e interaja (" + InputBinding(6) + ") para atravessar."
                : "Portal aberto. Aproxime-se e interaja (" + InputBinding(6) + ") para seguir à próxima sala.";
        statusUntil = Time.time + 5f;
    }

    public void TryUseExit(Vector2 playerPosition)
    {
        if (isPaused || victory || transitioning) return;
        if (enemiesRemaining > 0) { ShowStatus("Ainda restam ecos nesta sala.", 1.5f); return; }
        if (anchorsActivated < anchorsRequired)
        {
            ShowStatus(anchorsNeedPulse ? "Faça os sinais ressoarem com o Pulso (" + InputBinding(2) + ")." :
                "Interaja com o vestígio marcado (" + InputBinding(6) + ").", 2.5f);
            return;
        }
        if (Vector2.Distance(playerPosition, exitPosition) > 1.9f) { ShowStatus("Aproxime-se da passagem para avançar.", 1.5f); return; }
        ScadrykPortalVisual portalVisual = gateObject != null ? gateObject.GetComponent<ScadrykPortalVisual>() : null;
        if (!PortalCanBeEntered(portalVisual != null, portalVisual != null && portalVisual.IsOpen))
        { ShowStatus("A passagem ainda está se abrindo.", 1.5f); return; }
        ClearRoomProjectiles();
        clearedRooms[roomIndex] = true;
        if (roomIndex < RoomsPerPhase - 1) StartCoroutine(TransitionThroughPortal(phaseIndex, roomIndex + 1));
        else if (phaseIndex < PhaseNames.Length - 1)
        {
            StartCoroutine(TransitionThroughPortal(phaseIndex + 1, 0));
        }
        else StartCoroutine(TransitionToVictory());
    }

    private IEnumerator TransitionThroughPortal(int nextPhase, int nextRoom)
    {
        transitioning = true;
        ScadrykPortalVisual exitPortal = gateObject != null ? gateObject.GetComponent<ScadrykPortalVisual>() : null;
        Vector2 start = player.transform.position;
        Vector2 target = exitPosition;
        float elapsed = 0f;
        const float enterDuration = 0.82f;
        ShowStatus("Nara atravessa o portal...", enterDuration + 0.5f);
        while (elapsed < enterDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / enterDuration);
            float eased = t * t * (3f - 2f * t);
            player.SetPortalTransitPose(Vector2.Lerp(start, target, eased), 1f - eased * 0.97f);
            if (elapsed >= enterDuration * 0.4f && exitPortal != null) exitPortal.BeginClose();
            yield return null;
        }
        player.SetPortalTransitPose(target, 0.025f);
        yield return FadeScreen(1f, 0.45f);

        BuildPhase(nextPhase, nextRoom, false);
        SaveProgress(nextPhase, nextRoom);
        Vector2 arrivalPosition = player.transform.position;
        var arrivalObject = ScadrykPortalVisual.Spawn(levelRoot.transform, arrivalPosition, portalSprite, PhaseAccentColors[phaseIndex], 3.3f);
        ScadrykPortalVisual arrivalPortal = arrivalObject.GetComponent<ScadrykPortalVisual>();
        player.SetPortalTransitPose(arrivalPosition, 0.025f);
        yield return new WaitForSeconds(0.12f);

        elapsed = 0f;
        const float emergeDuration = 0.82f;
        while (elapsed < emergeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / emergeDuration);
            float eased = t * t * (3f - 2f * t);
            player.SetPortalTransitPose(arrivalPosition, Mathf.Lerp(0.025f, 1f, eased));
            transitionFade = 1f - eased;
            yield return null;
        }
        player.SetPortalTransitPose(arrivalPosition, 1f);
        if (arrivalPortal != null) arrivalPortal.BeginClose();
        yield return new WaitForSeconds(0.35f);
        transitionFade = 0f;
        transitioning = false;
        if (roomIndex == RoomsPerPhase - 1) ShowStatus(GuardianIntroductions[phaseIndex], 8f);
        else ShowStatus("Você chegou a: " + PhaseNames[phaseIndex] + ".", 3f);
    }

    private IEnumerator TransitionToVictory()
    {
        ClearRoomProjectiles();
        transitioning = true;
        ScadrykPortalVisual exitPortal = gateObject != null ? gateObject.GetComponent<ScadrykPortalVisual>() : null;
        Vector2 start = player.transform.position;
        Vector2 target = exitPosition;
        float elapsed = 0f;
        const float duration = 0.82f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t * (3f - 2f * t);
            player.SetPortalTransitPose(Vector2.Lerp(start, target, eased), 1f - eased * 0.97f);
            if (elapsed >= duration * 0.4f && exitPortal != null) exitPortal.BeginClose();
            yield return null;
        }
        yield return FadeScreen(1f, 0.45f);
        RecordRoomSession();
        transitioning = false;
        transitionFade = 0f;
        victory = true;
        overlaySelection = 0;
        ScadrykAudio.Play(ScadrykCue.Victory);
        if (!smokeRun)
        {
            PlayerPrefs.DeleteKey("Scadryk.Phase");
            PlayerPrefs.DeleteKey("Scadryk.Room");
            ClearCampaignProgress("Scadryk.");
            PlayerPrefs.Save();
        }
        statusMessage = VictoryEndingText;
    }

    private IEnumerator FadeScreen(float targetAlpha, float duration)
    {
        float startAlpha = transitionFade;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transitionFade = Mathf.Lerp(startAlpha, targetAlpha, Mathf.SmoothStep(0f, 1f, elapsed / duration));
            yield return null;
        }
        transitionFade = targetAlpha;
    }

    public void RestartCurrentRoom()
    {
        bool interludeWasPending = resumeFinalGuardianInterlude;
        if (player != null) player.ResetCombatCooldowns();
        BuildPhase(phaseIndex, roomIndex, false, true);
        resumeFinalGuardianInterlude = ShouldContinueFinalGuardianInterludeAfterRestart(interludeWasPending, resumeFinalGuardianInterlude);
        StartFinalGuardianInterlude();
        ShowStatus("Você caiu e retornou ao começo da sala.", 2.5f);
    }

    public void RecordPlayerDeath() => roomDeaths++;

    private static void ClearRoomProjectiles()
    {
        foreach (ScadrykEnemyProjectile projectile in FindObjectsByType<ScadrykEnemyProjectile>())
            if (projectile != null)
            {
                projectile.gameObject.SetActive(false);
                Destroy(projectile.gameObject);
            }
    }

    private void ShowStatus(string message, float seconds) { statusMessage = message; statusUntil = Time.time + seconds; }

    private void ShowMemory(string message, float seconds, bool playCue = true)
    {
        memoryText = message;
        memoryUntil = Time.time + seconds;
        memoryModal = !smokeRun;
        memoryDismissedFrame = -1;
        memoryLastMousePosition = Input.mousePosition;
        if (!smokeRun)
        {
            RecordStoryJournalEntry("Scadryk.", message);
            if (playCue) ScadrykAudio.Play(ScadrykCue.Memory);
        }
        ApplyPauseTimeScale();
    }

    public void ShowAbilityLocked(int index)
    {
        string[] bosses = { "o guardião da primeira fase", "o guardião da segunda fase", "o guardião da terceira fase" };
        int safeIndex = Mathf.Clamp(index, 0, bosses.Length - 1);
        ShowStatus("Esse poder será aprendido ao derrotar " + bosses[safeIndex] + ".", 2.5f);
    }

    public void ShowBossStage(int stage)
    {
        string message = stage == 2 ? "O Arauto convoca o sal. Observe as linhas âmbar e a queda marcada." :
            "Os três silêncios se juntam. O anel interno indica abrigo da onda.";
        ScadrykEffects.Burst(finalBoss != null ? (Vector2)finalBoss.transform.position : roomCenter, new Color(0.95f, 0.48f, 0.75f), 2.8f);
        ShowStatus(message, 7f);
        ScadrykAudio.Play(ScadrykCue.FinalBossWarning);
    }

    private void MakeLabelMarker(Transform parent, Vector2 position, string text, Color color)
    {
        // A small glowing waypoint keeps the room numbering visible in the world.
        MakeBox(parent, "Room waypoint " + text, position, new Vector2(0.18f, 0.18f), color, false, 1);
    }

    private static void MakeBox(Transform parent, string name, Vector2 position, Vector2 size, Color color, bool solid, int order)
    {
        var go = new GameObject(name); go.transform.SetParent(parent); go.transform.position = position; go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = SquareSprite; renderer.color = color; renderer.sortingOrder = order;
        if (solid)
        {
            var collider = go.AddComponent<BoxCollider2D>(); collider.size = Vector2.one;
            if (order >= 2) go.AddComponent<ScadrykDepthSort>().SetBias(2);
        }
    }

    private static void EnsureSprites()
    {
        if (SquareSprite == null) SquareSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        if (CircleSprite != null) return;
        const int size = 48; var texture = new Texture2D(size, size, TextureFormat.RGBA32, false); texture.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
        {
            float dx = (x + 0.5f - size / 2f) / (size / 2f), dy = (y + 0.5f - size / 2f) / (size / 2f);
            texture.SetPixel(x, y, dx * dx + dy * dy <= 0.94f ? Color.white : Color.clear);
        }
        texture.Apply(); CircleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static Sprite[] LoadSheetRow(string name, int row, bool wholeSheet)
    {
        if (!SpriteSheetRows.TryGetValue(name, out Sprite[][] rows))
        {
            Texture2D texture = Resources.Load<Texture2D>("ScadrykArt/" + name);
            if (texture == null) return null;
            rows = new Sprite[4][];
            for (int sheetRow = 0; sheetRow < 4; sheetRow++)
            {
                rows[sheetRow] = new Sprite[4];
                for (int column = 0; column < 4; column++)
                {
                    int xMin = Mathf.RoundToInt(column * texture.width / 4f);
                    int xMax = Mathf.RoundToInt((column + 1) * texture.width / 4f);
                    int yMin = Mathf.RoundToInt(texture.height - (sheetRow + 1) * texture.height / 4f);
                    int yMax = Mathf.RoundToInt(texture.height - sheetRow * texture.height / 4f);
                    var rect = new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
                    var sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.06f), 220f);
                    sprite.name = name + "_" + (sheetRow * 4 + column).ToString("00");
                    rows[sheetRow][column] = sprite;
                }
            }
            SpriteSheetRows.Add(name, rows);
        }

        if (!wholeSheet) return rows[(row % 4 + 4) % 4];
        if (WholeSpriteSheets.TryGetValue(name, out Sprite[] cachedSheet)) return cachedSheet;
        var result = new Sprite[16];
        for (int sheetRow = 0; sheetRow < rows.Length; sheetRow++)
            for (int column = 0; column < rows[sheetRow].Length; column++)
                result[sheetRow * 4 + column] = rows[sheetRow][column];
        WholeSpriteSheets.Add(name, result);
        return result;
    }

    private static Sprite LoadSingleSprite(string name, float pixelsPerUnit)
    {
        Texture2D texture = Resources.Load<Texture2D>("ScadrykArt/" + name);
        if (texture == null) return null;
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
    }

#if DEBUG
    private static bool SmokeBindingResetIsolation()
    {
        const int keyboardCount = 11, gamepadCount = 7;
        string testPrefix = "Scadryk.Smoke." + System.DateTime.UtcNow.Ticks + ".bindings.";
        string keyPrefix = testPrefix + "Key.";
        string padPrefix = testPrefix + "Pad.";
        try
        {
            KeyCode[] defaultKeys = { KeyCode.Space, KeyCode.LeftShift, KeyCode.Q, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.E, KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D };
            int[] defaultButtons = { 0, 1, 2, 4, 5, 6, 3 };
            PlayerPrefs.SetInt(keyPrefix + "0", (int)KeyCode.M);
            PlayerPrefs.SetInt(padPrefix + "0", 8);
            PlayerPrefs.SetInt(keyPrefix + "1", (int)KeyCode.G);
            PlayerPrefs.SetInt(padPrefix + "1", 19);
            ScadrykSettings.ClearReservedBindings(keyPrefix, padPrefix);
            bool reservedBindingsCleared = ScadrykSettings.Key(0, keyPrefix) == defaultKeys[0] &&
                ScadrykSettings.PadButton(0, padPrefix) == defaultButtons[0] &&
                ScadrykSettings.Key(1, keyPrefix) == KeyCode.G && ScadrykSettings.PadButton(1, padPrefix) == 19;

            PlayerPrefs.SetInt(keyPrefix + "0", (int)KeyCode.F);
            PlayerPrefs.SetInt(padPrefix + "0", 19);
            ScadrykSettings.ResetKeyboardBindings(keyPrefix);
            bool keyboardDefaultsRestored = true;
            for (int i = 0; i < defaultKeys.Length; i++)
                keyboardDefaultsRestored &= ScadrykSettings.Key(i, keyPrefix) == defaultKeys[i];
            bool gamepadSurvivedKeyboardReset = ScadrykSettings.PadButton(0, padPrefix) == 19;

            PlayerPrefs.SetInt(keyPrefix + "1", (int)KeyCode.G);
            ScadrykSettings.ResetGamepadBindings(padPrefix);
            bool gamepadDefaultsRestored = true;
            for (int i = 0; i < defaultButtons.Length; i++)
                gamepadDefaultsRestored &= ScadrykSettings.PadButton(i, padPrefix) == defaultButtons[i];
            bool keyboardSurvivedGamepadReset = ScadrykSettings.Key(1, keyPrefix) == KeyCode.G;
            return reservedBindingsCleared && keyboardDefaultsRestored && gamepadSurvivedKeyboardReset && gamepadDefaultsRestored && keyboardSurvivedGamepadReset;
        }
        finally
        {
            for (int i = 0; i < keyboardCount; i++) PlayerPrefs.DeleteKey(keyPrefix + i);
            for (int i = 0; i < gamepadCount; i++) PlayerPrefs.DeleteKey(padPrefix + i);
            PlayerPrefs.Save();
        }
    }

    private IEnumerator SmokeCampaign()
    {
        yield return null;
        if (!ScadrykPlayerController.CanMeasureTerrainExposure(false, false) ||
            ScadrykPlayerController.CanMeasureTerrainExposure(true, false) ||
            ScadrykPlayerController.CanMeasureTerrainExposure(false, true))
        { SmokeFail("A exposição ambiental ignora pausa ou continua sendo contada após a derrota"); yield break; }
        Debug.Log("SCADRYK_SMOKE: telemetria ambiental continua ativa no jogo e para na pausa ou na derrota");
        Vector2 cappedFlow = ScadrykTerrainZone.CombineFlow(Vector2.right * 1.8f, Vector2.right * 1.8f);
        Vector2 opposingWalk = ScadrykPlayerController.ResolveTerrainTargetVelocity(Vector2.left, 6.2f, 1f, Vector2.right * 1.8f);
        Vector2 carriedIdle = ScadrykPlayerController.ResolveTerrainTargetVelocity(Vector2.zero, 6.2f, 1f, Vector2.right * 1.8f);
        if (Mathf.Abs(cappedFlow.magnitude - ScadrykTerrainZone.MaximumFlowSpeed) > 0.001f ||
            opposingWalk.x >= 0f || Mathf.Abs(opposingWalk.y) > 0.001f ||
            Mathf.Abs(carriedIdle.x - 1.8f) > 0.001f || Mathf.Abs(carriedIdle.y) > 0.001f)
        { SmokeFail("A corrente não conserva o impulso, não limita fluxos somados ou vence a caminhada contra o fluxo"); yield break; }
        Debug.Log("SCADRYK_SMOKE: corrente move em repouso, permite contrafluxo e limita a velocidade somada");
        float markerStart = ScadrykAmbientProp.FlowMarkerOffset(0f, 0.5f, 1f, 0.34f);
        float markerAdvance = ScadrykAmbientProp.FlowMarkerOffset(0.25f, 0.5f, 1f, 0.34f);
        float markerCycle = ScadrykAmbientProp.FlowMarkerOffset(1f, 0.5f, 1f, 0.34f);
        float[] markerSpacing = new float[4];
        for (int marker = 0; marker < markerSpacing.Length; marker++)
            markerSpacing[marker] = ScadrykAmbientProp.FlowMarkerOffset(0f, marker / 4f, 1f, 0.34f);
        System.Array.Sort(markerSpacing);
        bool evenlySpaced = true;
        for (int marker = 0; marker < markerSpacing.Length; marker++)
        {
            float next = marker + 1 < markerSpacing.Length ? markerSpacing[marker + 1] : markerSpacing[0] + 0.34f;
            if (Mathf.Abs((next - markerSpacing[marker]) - 0.085f) > 0.001f) evenlySpaced = false;
        }
        if (markerAdvance <= markerStart || Mathf.Abs(markerCycle - markerStart) > 0.001f ||
            Mathf.Abs(markerStart) > 0.001f || !evenlySpaced)
        { SmokeFail("As setas da corrente não avançam a leste, fecham o ciclo ou mantêm espaçamento regular"); yield break; }
        Debug.Log("SCADRYK_SMOKE: espuma da corrente avança em ciclo regular e mantém setas uniformemente espaçadas");
        float sealHidden = ScadrykAmbientProp.RevealAlpha(8f, 4.2f, 0.16f);
        float sealNearby = ScadrykAmbientProp.RevealAlpha(1f, 4.2f, 0.16f);
        float sealContact = ScadrykAmbientProp.RevealAlpha(0f, 4.2f, 0.16f);
        if (Mathf.Abs(sealHidden - 0.16f) > 0.001f || sealNearby <= sealHidden || Mathf.Abs(sealContact - 1f) > 0.001f)
        { SmokeFail("O selo opcional não permanece sutil ao longe e legível ao se aproximar"); yield break; }
        Debug.Log("SCADRYK_SMOKE: selos do Eco surgem em intensidade conforme Nara se aproxima");
        if (ScadrykSettings.CanBindKey(KeyCode.None) || ScadrykSettings.CanBindKey(KeyCode.Escape) ||
            ScadrykSettings.CanBindKey(KeyCode.F8) || ScadrykSettings.CanBindKey(KeyCode.M) || !ScadrykSettings.CanBindKey(KeyCode.F) ||
            ScadrykSettings.CanBindPadButton(7) || ScadrykSettings.CanBindPadButton(8) || !ScadrykSettings.CanBindPadButton(9))
        { SmokeFail("Remapeamento permite tecla/botão reservado ou rejeita um vínculo normal"); yield break; }
        Debug.Log("SCADRYK_SMOKE: Esc, F8, M, Start e L3 reservados; vínculos normais disponíveis");
        if (!ObjectiveText(0, 0, 3, 0, 0, false, false, false, "Derrotar os Ecos", "E", "Q").Contains("3 restantes") ||
            !ObjectiveText(0, 0, 1, 0, 0, false, false, false, "Derrotar os Ecos", "E", "Q").Contains("1 restante") ||
            !ObjectiveText(1, 1, 0, 1, 2, true, false, false, "Derrotar os Ecos", "E", "Q").Contains("1/2") ||
            !ObjectiveText(2, 1, 0, 1, 2, true, true, false, "Derrotar os Ecos", "E", "Q").StartsWith("Ressoar o sino do recife") ||
            !ObjectiveText(2, 2, 0, 0, 0, false, false, true, "Derrotar o Arauto", "E", "Q").StartsWith("Atravessar o portal colossal") ||
            CombatObjectiveLabel(0, 2, "A Raiz-Mor", false, false) != "Derrotar A Raiz-Mor" ||
            CombatObjectiveLabel(2, 2, null, false, true) != "Escutar a última lembrança antes do despertar" ||
            CombatObjectiveLabel(2, 2, null, true, false) != "Derrotar o Arauto do Vazio")
        { SmokeFail("O objetivo ativo não acompanha combate, sinais, sequência e abertura do portal"); yield break; }
        Debug.Log("SCADRYK_SMOKE: objetivo ativo avança de combate para sinais e travessia do portal");
        bool[] blockedHeadings = new bool[10];
        blockedHeadings[0] = true;
        blockedHeadings[1] = true;
        Vector2 steeredHeading = ScadrykEnemyHealth.SelectSteeringHeading(Vector2.right, blockedHeadings);
        for (int i = 0; i < blockedHeadings.Length; i++) blockedHeadings[i] = true;
        Vector2 enclosedHeading = ScadrykEnemyHealth.SelectSteeringHeading(Vector2.right, blockedHeadings);
        if (Vector2.Dot(steeredHeading, Vector2.right) < 0.8f || Vector2.Distance(enclosedHeading, Vector2.right) > 0.001f ||
            ScadrykEnemyHealth.SteeringQueryIsSaturated(ScadrykEnemyHealth.SteeringHitCapacity - 1) ||
            !ScadrykEnemyHealth.SteeringQueryIsSaturated(ScadrykEnemyHealth.SteeringHitCapacity))
        { SmokeFail("A navegação dos Ecos não contorna um bloqueio ou não mantém avanço quando todos os lados estão fechados"); yield break; }
        Debug.Log("SCADRYK_SMOKE: direção de contorno mantém avanço, satura com segurança e tem fallback quando não há passagem livre");
        if (ScadrykEnemyHealth.AttackRecoveryDuration(false, false) <= 0f ||
            ScadrykEnemyHealth.AttackRecoveryDuration(true, false) <= ScadrykEnemyHealth.AttackRecoveryDuration(false, false) ||
            ScadrykEnemyHealth.AttackRecoveryDuration(false, true) <= ScadrykEnemyHealth.AttackRecoveryDuration(true, false))
        { SmokeFail("A recuperação após impacto perdeu duração ou gradação por porte do inimigo"); yield break; }
        Debug.Log("SCADRYK_SMOKE: recuperação de impacto aumenta de Eco comum a guardião e Arauto");
        if (!ScadrykEnemyHealth.TidebreakerCancelsWindup(false, true) ||
            ScadrykEnemyHealth.TidebreakerCancelsWindup(true, true) ||
            ScadrykEnemyHealth.TidebreakerCancelsWindup(false, false))
        { SmokeFail("Rompante não interrompe apenas a preparação de Ecos comuns"); yield break; }
        Debug.Log("SCADRYK_SMOKE: Rompante interrompe preparação de Eco comum, sem cancelar guardião ou Arauto");
        if (ScadrykActorAnimator.WindupStrength(1f, 1f, 1f) != 0f ||
            Mathf.Abs(ScadrykActorAnimator.WindupStrength(1.5f, 1f, 1f) - 0.5f) > 0.001f ||
            ScadrykActorAnimator.WindupStrength(2f, 1f, 1f) != 1f)
        { SmokeFail("A preparação animada não percorre o início, meio e fim do aviso inimigo"); yield break; }
        Debug.Log("SCADRYK_SMOKE: pose de preparação acompanha o aviso até a resolução do ataque");
        Vector2 dashUp = ScadrykActorAnimator.DashVisualOffset(Vector2.up, 0.5f);
        Vector2 dashDown = ScadrykActorAnimator.DashVisualOffset(Vector2.down, 0.5f);
        Vector2 dashLeft = ScadrykActorAnimator.DashVisualOffset(Vector2.left, 0.5f);
        Vector2 dashRight = ScadrykActorAnimator.DashVisualOffset(Vector2.right, 0.5f);
        if (dashUp.y <= 0f || dashDown.y >= 0f || dashLeft.x >= 0f || dashRight.x <= 0f ||
            ScadrykActorAnimator.DashVisualOffset(Vector2.zero, 1f) != Vector2.zero ||
            Vector2.Distance(dashUp, Vector2.up * (ScadrykActorAnimator.DashVisualLungeDistance * 0.5f)) > 0.001f ||
            Vector2.Distance(ScadrykActorAnimator.DashVisualOffset(Vector2.up, 2f), Vector2.up * ScadrykActorAnimator.DashVisualLungeDistance) > 0.001f)
        { SmokeFail("A silhueta da esquiva precisa indicar as quatro direções e limitar seu avanço visual"); yield break; }
        Debug.Log("SCADRYK_SMOKE: silhueta da esquiva avança na direção escolhida nos quatro sentidos");
        int[] staggeredFrames = new int[4];
        for (int actor = 0; actor < staggeredFrames.Length; actor++)
            staggeredFrames[actor] = ScadrykEnemyHealth.AnimationFrameIndex(0f, 7f, 4, actor / 4f);
        if (staggeredFrames[0] == staggeredFrames[1] || staggeredFrames[1] == staggeredFrames[2] ||
            staggeredFrames[2] == staggeredFrames[3] || Mathf.Abs(ScadrykActorAnimator.StrideWave(0f, 1f, 0f) -
            ScadrykActorAnimator.StrideWave(0f, 1f, Mathf.PI * 0.5f)) < 0.5f)
        { SmokeFail("As animações dos Ecos e seus passos ainda começam sincronizados"); yield break; }
        Debug.Log("SCADRYK_SMOKE: Ecos usam fases individuais para passos e quadros de movimento");
        if (!RoomGroundOrientationsAreUnique())
        { SmokeFail("As três salas repetem a mesma orientação do cenário de fundo"); yield break; }
        Debug.Log("SCADRYK_SMOKE: fundos pintados variam entre as três salas de cada bioma");
        Color warningTint = ScadrykEnemyHealth.StateTint(true, false, Color.white);
        Color exposedTint = ScadrykEnemyHealth.StateTint(false, true, Color.white);
        Color interruptedExposeTint = ScadrykEnemyHealth.StateTint(true, true, Color.white);
        float warningEarlyAlpha = ScadrykTimedVisual.DangerWarningAlpha(0.05f);
        float warningMidAlpha = ScadrykTimedVisual.DangerWarningAlpha(0.35f);
        float warningImpactAlpha = ScadrykTimedVisual.DangerWarningAlpha(0.95f);
        if (warningTint == exposedTint || warningTint != interruptedExposeTint ||
            warningEarlyAlpha <= 0f || warningMidAlpha <= warningEarlyAlpha || warningImpactAlpha <= warningMidAlpha)
        { SmokeFail("Aviso e vulnerabilidade se confundem ou o telegráfico perde destaque perto do impacto"); yield break; }
        Debug.Log("SCADRYK_SMOKE: aviso âmbar permanece visível e ganha destaque até o impacto; vulnerabilidade azul é distinta");
        yield return SmokeProjectileCover();
        if (!smokeProjectileCoverPassed) { SmokeFail("Projéteis em movimento não param na cobertura sólida ou atravessam o gatilho de teste"); yield break; }
        Debug.Log("SCADRYK_SMOKE: projétil respeita cobertura, atravessa gatilho e é limpo no fim do encontro");
        if (!SmokeBindingResetIsolation()) { SmokeFail("Restaurar controles alterou outro dispositivo ou preferências"); yield break; }
        Debug.Log("SCADRYK_SMOKE: padrões de teclado e controle são independentes e preservam preferências");
        string checkpointTestPrefix = "Scadryk.Smoke." + System.DateTime.UtcNow.Ticks + ".";
        bool checkpointRoundTrip = false, witnessProgressRoundTrip = false, worldSecretProgressRoundTrip = false;
        bool storyJournalRoundTrip = false;
        bool quitCheckpointStateIsValid = false, campaignProgressCleanupRoundTrip = false;
        bool bossProgressRoundTrip = false, powerRestoreRoundTrip = false, powerEchoProgressRoundTrip = false;
        try
        {
            bool newGameDefaults = !HasCheckpoint(checkpointTestPrefix) &&
                !TryLoadCheckpoint(checkpointTestPrefix, out int defaultPhase, out int defaultRoom) && defaultPhase == 0 && defaultRoom == 0;
            quitCheckpointStateIsValid = ShouldSaveCheckpointOnQuit(true, false, false, false, false) &&
                !ShouldSaveCheckpointOnQuit(false, false, false, false, false) &&
                !ShouldSaveCheckpointOnQuit(true, true, false, false, false) &&
                !ShouldSaveCheckpointOnQuit(true, false, true, false, false) &&
                !ShouldSaveCheckpointOnQuit(true, false, false, true, false) &&
                !ShouldSaveCheckpointOnQuit(true, false, false, false, true);
            WriteGuardianDefeatProgress(checkpointTestPrefix, 0);
            WriteFinalBossDefeatProgress(checkpointTestPrefix);
            WriteWitnessProgress(checkpointTestPrefix, 1, 2);
            MarkWorldSecretRevealed(checkpointTestPrefix, 2, 1);
            ClearCampaignProgress(checkpointTestPrefix);
            campaignProgressCleanupRoundTrip = !HasGuardianDefeatProgress(checkpointTestPrefix, 0) &&
                !HasFinalBossDefeatProgress(checkpointTestPrefix) && ReadWitnessProgress(checkpointTestPrefix, 1) == 0 &&
                !IsWorldSecretRevealed(checkpointTestPrefix, 2, 1);
            RecordStoryJournalEntry(checkpointTestPrefix, "Primeira lembrança.");
            RecordStoryJournalEntry(checkpointTestPrefix, "Primeira lembrança.");
            RecordStoryJournalEntry(checkpointTestPrefix, "A costa voltou a cantar.");
            string[] journalEntries = ReadStoryJournal(checkpointTestPrefix);
            storyJournalRoundTrip = journalEntries.Length == 2 && journalEntries[0] == "Primeira lembrança." &&
                journalEntries[1] == "A costa voltou a cantar.";
            ClearStoryJournal(checkpointTestPrefix);
            storyJournalRoundTrip &= ReadStoryJournal(checkpointTestPrefix).Length == 0;
            PlayerPrefs.SetInt(checkpointTestPrefix + "Room", 2);
            bool ignoresOrphanedRoom = !HasCheckpoint(checkpointTestPrefix) &&
                !TryLoadCheckpoint(checkpointTestPrefix, out int orphanPhase, out int orphanRoom) && orphanPhase == 0 && orphanRoom == 0;
            PlayerPrefs.DeleteKey(checkpointTestPrefix + "Room");
            PlayerPrefs.SetInt(checkpointTestPrefix + "Phase", 2);
            bool ignoresOrphanedPhase = !HasCheckpoint(checkpointTestPrefix) &&
                !TryLoadCheckpoint(checkpointTestPrefix, out int orphanedPhase, out int orphanedRoom) && orphanedPhase == 0 && orphanedRoom == 0;
            PlayerPrefs.DeleteKey(checkpointTestPrefix + "Phase");
            WriteCheckpoint(checkpointTestPrefix, 2, 2);
            bool restoresLastPortal = HasCheckpoint(checkpointTestPrefix) &&
                TryLoadCheckpoint(checkpointTestPrefix, out int savedPhase, out int savedRoom) && savedPhase == 2 && savedRoom == 2;
            WriteCheckpoint(checkpointTestPrefix, 99, -4);
            bool clampsInvalidData = TryLoadCheckpoint(checkpointTestPrefix, out int clampedPhase, out int clampedRoom) &&
                clampedPhase == PhaseNames.Length - 1 && clampedRoom == 0;
            bool witnessStartsAtFirstLine = ReadWitnessProgress(checkpointTestPrefix, 1) == 0;
            WriteWitnessProgress(checkpointTestPrefix, 1, 2);
            WriteWitnessProgress(checkpointTestPrefix, 2, 1);
            bool restoresWitnessLine = ReadWitnessProgress(checkpointTestPrefix, 1) == 2 && ReadWitnessProgress(checkpointTestPrefix, 2) == 1;
            WriteWitnessProgress(checkpointTestPrefix, 1, -5);
            bool clampsWitnessLine = ReadWitnessProgress(checkpointTestPrefix, 1) == 0;
            ClearWitnessProgress(checkpointTestPrefix);
            bool clearsWitnessLines = ReadWitnessProgress(checkpointTestPrefix, 1) == 0 && ReadWitnessProgress(checkpointTestPrefix, 2) == 0;
            bool secretStartsHidden = !IsWorldSecretRevealed(checkpointTestPrefix, 1, 0);
            MarkWorldSecretRevealed(checkpointTestPrefix, 1, 0);
            bool secretSurvivesRoomRebuild = IsWorldSecretRevealed(checkpointTestPrefix, 1, 0) &&
                !ShouldBuildWorldSecret(false, IsWorldSecretRevealed(checkpointTestPrefix, 1, 0));
            bool smokeCanRebuildSecret = ShouldBuildWorldSecret(true, IsWorldSecretRevealed(checkpointTestPrefix, 1, 0));
            bool secretsAreRoomSpecific = !IsWorldSecretRevealed(checkpointTestPrefix, 2, 0);
            ClearWorldSecretProgress(checkpointTestPrefix);
            bool newJourneyClearsSecrets = !IsWorldSecretRevealed(checkpointTestPrefix, 1, 0);
            bool freshJourneyRevealsUnfoundSecret = ShouldBuildWorldSecret(false, IsWorldSecretRevealed(checkpointTestPrefix, 1, 0));
            worldSecretProgressRoundTrip = secretStartsHidden && secretSurvivesRoomRebuild && smokeCanRebuildSecret &&
                secretsAreRoomSpecific && newJourneyClearsSecrets && freshJourneyRevealsUnfoundSecret;
            MarkWorldSecretRevealed(checkpointTestPrefix, 1, 0);
            MarkWorldSecretRevealed(checkpointTestPrefix, 2, 0);
            MarkWorldSecretRevealed(checkpointTestPrefix, 2, 2);
            powerEchoProgressRoundTrip = ShouldRestorePowerEcho(checkpointTestPrefix, 0) &&
                ShouldRestorePowerEcho(checkpointTestPrefix, 1) && ShouldRestorePowerEcho(checkpointTestPrefix, 2) &&
                !ShouldRestorePowerEcho(checkpointTestPrefix, -1) && !ShouldRestorePowerEcho(checkpointTestPrefix, 3) &&
                PowerGrantedBySecretAt(1, 0) == 0 && PowerGrantedBySecretAt(2, 0) == 1 &&
                PowerGrantedBySecretAt(2, 2) == 2 && PowerGrantedBySecretAt(0, 0) == -1;
            ClearWorldSecretProgress(checkpointTestPrefix);
            powerEchoProgressRoundTrip &= !ShouldRestorePowerEcho(checkpointTestPrefix, 0) &&
                !ShouldRestorePowerEcho(checkpointTestPrefix, 1) && !ShouldRestorePowerEcho(checkpointTestPrefix, 2);
            bool guardianStartsUndefeated = !HasGuardianDefeatProgress(checkpointTestPrefix, 0);
            WriteGuardianDefeatProgress(checkpointTestPrefix, 0);
            WriteGuardianDefeatProgress(checkpointTestPrefix, 2);
            bool guardianDefeatsPersistIndependently = HasGuardianDefeatProgress(checkpointTestPrefix, 0) &&
                HasGuardianDefeatProgress(checkpointTestPrefix, 2) && !HasGuardianDefeatProgress(checkpointTestPrefix, 1);
            WriteGuardianDefeatProgress(checkpointTestPrefix, 1);
            bool powersRestoreAtCheckpoint =
                !ShouldRestoreAbility(0, 0, false) && ShouldRestoreAbility(0, 0, HasGuardianDefeatProgress(checkpointTestPrefix, 0)) &&
                !ShouldRestoreAbility(1, 1, false) && ShouldRestoreAbility(0, 1, false) &&
                ShouldRestoreAbility(1, 1, HasGuardianDefeatProgress(checkpointTestPrefix, 1)) &&
                ShouldRestoreAbility(0, 2, false) && ShouldRestoreAbility(1, 2, false) &&
                !ShouldRestoreAbility(2, 2, false) && ShouldRestoreAbility(2, 2, HasGuardianDefeatProgress(checkpointTestPrefix, 2)) &&
                !ShouldRestoreAbility(-1, 2, true) && !ShouldRestoreAbility(3, 2, true) && !ShouldRestoreAbility(1, -1, true);
            powerRestoreRoundTrip = powersRestoreAtCheckpoint;
            bool defeatedGuardianRestoresOnlyAtItsArena = ShouldRestoreDefeatedGuardian(0, 2, true) &&
                !ShouldRestoreDefeatedGuardian(0, 1, true) && !ShouldRestoreDefeatedGuardian(0, 2, false);
            bool finalGuardianResumeWaitsForArauto = ShouldResumeFinalGuardianInterlude(2, 2, true, false) &&
                !ShouldResumeFinalGuardianInterlude(2, 2, false, false) &&
                !ShouldResumeFinalGuardianInterlude(2, 2, true, true) && !ShouldResumeFinalGuardianInterlude(1, 2, true, false);
            bool finalInterludeSurvivesDeath = ShouldContinueFinalGuardianInterludeAfterRestart(false, true) &&
                ShouldContinueFinalGuardianInterludeAfterRestart(true, false) &&
                !ShouldContinueFinalGuardianInterludeAfterRestart(false, false);
            ClearGuardianDefeatProgress(checkpointTestPrefix);
            bool newJourneyClearsGuardianProgress = !HasGuardianDefeatProgress(checkpointTestPrefix, 0) &&
                !HasGuardianDefeatProgress(checkpointTestPrefix, 1) && !HasGuardianDefeatProgress(checkpointTestPrefix, 2);
            WriteFinalBossDefeatProgress(checkpointTestPrefix);
            bool defeatedFinalBossRestoresOnlyAtEnding = HasFinalBossDefeatProgress(checkpointTestPrefix) &&
                ShouldRestoreFinalBossDefeat(2, 2, true) && !ShouldRestoreFinalBossDefeat(2, 1, true) &&
                !ShouldRestoreFinalBossDefeat(1, 2, true);
            ClearFinalBossDefeatProgress(checkpointTestPrefix);
            bool newJourneyClearsFinalBossProgress = !HasFinalBossDefeatProgress(checkpointTestPrefix);
            bossProgressRoundTrip = guardianStartsUndefeated && guardianDefeatsPersistIndependently &&
                defeatedGuardianRestoresOnlyAtItsArena && finalGuardianResumeWaitsForArauto && finalInterludeSurvivesDeath && newJourneyClearsGuardianProgress &&
                defeatedFinalBossRestoresOnlyAtEnding && newJourneyClearsFinalBossProgress;
            witnessProgressRoundTrip = witnessStartsAtFirstLine && restoresWitnessLine && clampsWitnessLine && clearsWitnessLines;
            checkpointRoundTrip = newGameDefaults && quitCheckpointStateIsValid && campaignProgressCleanupRoundTrip && ignoresOrphanedRoom &&
                ignoresOrphanedPhase && restoresLastPortal && clampsInvalidData;
        }
        finally
        {
            PlayerPrefs.DeleteKey(checkpointTestPrefix + "Phase");
            PlayerPrefs.DeleteKey(checkpointTestPrefix + "Room");
            ClearGuardianDefeatProgress(checkpointTestPrefix);
            ClearFinalBossDefeatProgress(checkpointTestPrefix);
            ClearWitnessProgress(checkpointTestPrefix);
            ClearWorldSecretProgress(checkpointTestPrefix);
            ClearStoryJournal(checkpointTestPrefix);
            PlayerPrefs.Save();
        }
        if (!storyJournalRoundTrip) { SmokeFail("O caderno não preserva lembranças únicas ou não limpa os dados temporários"); yield break; }
        Debug.Log("SCADRYK_SMOKE: caderno persiste lembranças únicas em ordem e reinicia com nova jornada");
        if (!checkpointRoundTrip) { SmokeFail("Checkpoint parcial não foi ignorado, ou o portal não foi restaurado/limitado"); yield break; }
        Debug.Log("SCADRYK_SMOKE: checkpoint grava, retoma sem estado de título/abertura, limita e limpa chaves temporárias");
        if (!bossProgressRoundTrip) { SmokeFail("O save não preserva chefes derrotados, restaura sala segura ou limpa a jornada nova"); yield break; }
        Debug.Log("SCADRYK_SMOKE: derrota de guardiões e Arauto persiste, retoma na arena e é limpa na jornada nova");
        if (!powerRestoreRoundTrip) { SmokeFail("Retomar um checkpoint não restaura somente os poderes já conquistados pelos guardiões"); yield break; }
        Debug.Log("SCADRYK_SMOKE: poderes restauram a partir do bioma e dos guardiões persistidos sem liberar poderes futuros");
        if (!witnessProgressRoundTrip) { SmokeFail("As conversas com Nima não preservam o ponto de leitura por bioma ou não reiniciam com a jornada"); yield break; }
        Debug.Log("SCADRYK_SMOKE: conversa de Nima preserva a próxima fala separada por bioma e pode ser reiniciada");
        if (!worldSecretProgressRoundTrip) { SmokeFail("Selo opcional volta após reconstruir a sala, mistura salas ou não é limpo ao iniciar nova jornada"); yield break; }
        Debug.Log("SCADRYK_SMOKE: selos opcionais persistem por sala, sobrevivem à reconstrução e são limpos na nova jornada");
        if (!powerEchoProgressRoundTrip) { SmokeFail("Melhorias de poder dos Ecos não acompanham corretamente os selos no save ou na nova jornada"); yield break; }
        Debug.Log("SCADRYK_SMOKE: os três selos salvam melhorias distintas e a jornada nova as remove");
        if (!SmokeCoverBlocksOnlyWhenSolid())
        { SmokeFail("Cobertura não bloqueia poderes direcionais ou gatilhos bloqueiam poderes circulares"); yield break; }
        Debug.Log("SCADRYK_SMOKE: cobertura interrompe golpes direcionais, não o Estouro circular, e gatilhos não bloqueiam");
        if (!RoomObstacleLayoutsAreDistinctAndOpen())
        { SmokeFail("As nove salas repetem obstáculos, bloqueiam entrada/saída ou não têm rota caminhável até o portal"); yield break; }
        Debug.Log("SCADRYK_SMOKE: nove layouts distintos têm corredores caminháveis entre entrada e portal");
        if (!SideRoutesAreDistinctAndNavigable())
        { SmokeFail("Os desvios laterais dos três biomas não conectam as duas aberturas ou não deixam largura segura"); yield break; }
        Debug.Log("SCADRYK_SMOKE: bosque, sal e costa têm desvios alternativos com aberturas e largura transitáveis");
        if (!RoomAnchorsAvoidCover())
        { SmokeFail("Um objetivo de ressonância está oculto pela cobertura ou perto demais da entrada/saída"); yield break; }
        Debug.Log("SCADRYK_SMOKE: objetivos das cinco salas de sinal ficam legíveis e acessíveis fora da cobertura");
        if (!EncounterSpawnsAvoidCover())
        { SmokeFail("Um Eco ou guardião nasce dentro da cobertura ou sobre outro inimigo"); yield break; }
        Debug.Log("SCADRYK_SMOKE: surgimento dos encontros evita cobertura sólida nas nove salas");
        if (Mathf.Abs(ScadrykEnemyHealth.OpeningAttackDelay(0, false, true) - 2.6f) > 0.001f ||
            Mathf.Abs(ScadrykEnemyHealth.OpeningAttackDelay(0, false) - 0.3f) > 0.001f ||
            ScadrykEnemyHealth.OpeningAttackDelay(1, false) <= ScadrykEnemyHealth.OpeningAttackDelay(0, false) ||
            ScadrykEnemyHealth.OpeningAttackDelay(2, false) <= ScadrykEnemyHealth.OpeningAttackDelay(1, false) ||
            ScadrykEnemyHealth.OpeningAttackDelay(3, false) <= ScadrykEnemyHealth.OpeningAttackDelay(2, false) ||
            Mathf.Abs(ScadrykEnemyHealth.OpeningAttackDelay(0, true) - 1.25f) > 0.001f)
        { SmokeFail("Os primeiros avisos de ataque dos encontros não estão espaçados"); yield break; }
        float nextOpeningSlot = 0f;
        float firstOpeningAttack = ScadrykEnemyHealth.ReserveOpeningAttackSlot(ref nextOpeningSlot, 0.3f);
        float secondOpeningAttack = ScadrykEnemyHealth.ReserveOpeningAttackSlot(ref nextOpeningSlot, 0.82f);
        float lateOpeningAttack = ScadrykEnemyHealth.ReserveOpeningAttackSlot(ref nextOpeningSlot, 5f);
        float followingLateAttack = ScadrykEnemyHealth.ReserveOpeningAttackSlot(ref nextOpeningSlot, 5.1f);
        if (Mathf.Abs(firstOpeningAttack - 0.3f) > 0.001f || Mathf.Abs(secondOpeningAttack - 0.82f) > 0.001f ||
            Mathf.Abs(lateOpeningAttack - 5f) > 0.001f ||
            Mathf.Abs(followingLateAttack - lateOpeningAttack - ScadrykEnemyHealth.OpeningAttackSpacing) > 0.001f)
        { SmokeFail("O escalonador não mantém intervalo entre avisos quando Ecos chegam ao alcance depois"); yield break; }
        Debug.Log("SCADRYK_SMOKE: escalonamento preserva intervalo mesmo se um Eco chega ao alcance atrasado");
        if (EnemyCountForRoom(0) != 1 || EnemyRoleForRoom(0, 0) != 0 ||
            EnemyCountForRoom(1) != 2 || EnemyRoleForRoom(1, 0) != 0 || EnemyRoleForRoom(1, 1) != 1 ||
            EnemyCountForRoom(4) != 4 ||
            EnemyRoleForRoom(4, 0) != 0 || EnemyRoleForRoom(4, 1) != 1 || EnemyRoleForRoom(4, 2) != 2 || EnemyRoleForRoom(4, 3) != 3 ||
            EnemyCountForRoom(7) != 4 ||
            EnemyRoleForRoom(7, 0) != 1 || EnemyRoleForRoom(7, 1) != 1 || EnemyRoleForRoom(7, 2) != 2 || EnemyRoleForRoom(7, 3) != 3 ||
            !GuardianAttackIsPreviewedBeforeBoss(0) || !GuardianAttackIsPreviewedBeforeBoss(1) || !GuardianAttackIsPreviewedBeforeBoss(2) ||
            EnemyArtworkRowForRole(0) != 0 || EnemyArtworkRowForRole(1) != 1 ||
            EnemyArtworkRowForRole(2) != 3 || EnemyArtworkRowForRole(3) != 2)
        { SmokeFail("As composições dos encontros de quatro inimigos perderam sua identidade tática"); yield break; }
        Debug.Log("SCADRYK_SMOKE: equipes de bosque, sal e costa têm papéis próprios e cada chefe é anunciado por um padrão previamente ensinado");
        if (GuardianNames.Length != 3 || GuardianIntroductions.Length != 3 || GuardianMemories.Length != 3)
        { SmokeFail("Cada bioma precisa de nome, fala e memória próprios para o guardião"); yield break; }
        float basinNimaPresence = WitnessPresenceForPhase(1), coastNimaPresence = WitnessPresenceForPhase(2);
        if (WitnessPresenceForPhase(0) != 0f || basinNimaPresence <= 0f || coastNimaPresence <= basinNimaPresence || coastNimaPresence > 1f ||
            WitnessScaleForPhase(2) <= WitnessScaleForPhase(1) || WitnessTintForPhase(0) != Color.clear ||
            WitnessTintForPhase(1) == WitnessTintForPhase(2) ||
            Mathf.Abs(WitnessTintForPhase(1).a - basinNimaPresence) > 0.001f ||
            Mathf.Abs(WitnessTintForPhase(2).a - coastNimaPresence) > 0.001f)
        { SmokeFail("A presença visível de Nima não cresce após libertar as lembranças dos guardiões"); yield break; }
        if (!OpeningMemory.Contains("Grande Onda") || !OpeningMemory.Contains("Nara") ||
            !GuardianMemories[0].Contains("Nima") || !BasinWitnessLines[0].Contains("primeiro sino") ||
            !BasinWitnessLines[1].StartsWith("Nara:") || !CoastWitnessLines[1].StartsWith("Nara:") ||
            BasinWitnessLines.Length != 3 || !BasinWitnessLines[2].Contains("cristais") ||
            CoastWitnessLines.Length != 3 || !CoastWitnessLines[0].Contains("sal") || !CoastWitnessLines[2].Contains("Arauto") ||
            !GuardianMemories[1].Contains("última onda") || !GuardianMemories[2].Contains("poupar os vivos") ||
            !EndingMemory.Contains("Nara escolhe lembrar") || !EndingMemory.Contains("aprisionou os mortos"))
        { SmokeFail("A história perdeu a causa da maré imóvel, a ligação de Nima ou a resolução do conflito"); yield break; }
        Debug.Log("SCADRYK_SMOKE: abertura, falas, aparição crescente de Nima, memórias dos guardiões e resolução formam o arco da maré");
        string victoryOverlay = OverlayMessage(false, true, false);
        if (!victoryOverlay.Contains("Os sinos respondem") || !victoryOverlay.Contains("Nara escolheu lembrar") ||
            !victoryOverlay.Contains("não prende ninguém"))
        { SmokeFail("O painel de vitória precisa confirmar a escolha de Nara, o retorno dos sinos e o fim do aprisionamento"); yield break; }
        Debug.Log("SCADRYK_SMOKE: o painel de vitória seleciona o encerramento da história");
        string savedLocation = CheckpointSummary(2, 1);
        string clampedLocation = CheckpointSummary(-1, 99);
        string defeatedGuardianLocation = CheckpointSummary(2, 2, true);
        string defeatedArautoLocation = CheckpointSummary(2, 2, true, true);
        if (!savedLocation.Contains("Costa das Badaladas") || !savedLocation.Contains("Bóias") || !savedLocation.Contains("2/3") ||
            !clampedLocation.Contains("Margens do Primeiro Eco") || !clampedLocation.Contains("3/3") ||
            !defeatedGuardianLocation.Contains("último guardião vencido") || !defeatedGuardianLocation.Contains("Arauto aguarda") ||
            !defeatedArautoLocation.Contains("portal colossal aberto"))
        { SmokeFail("O resumo do checkpoint não identifica ou limita corretamente bioma e sala salvos"); yield break; }
        Debug.Log("SCADRYK_SMOKE: resumo do checkpoint identifica bioma e sala e limita dados fora da faixa");
        for (int guardian = 0; guardian < GuardianNames.Length; guardian++)
            if (string.IsNullOrWhiteSpace(GuardianNames[guardian]) || !GuardianIntroductions[guardian].StartsWith(GuardianNames[guardian]) ||
                !GuardianMemories[guardian].Contains(GuardianNames[guardian]))
            { SmokeFail("Apresentação e lembrança não correspondem ao guardião " + guardian); yield break; }
        if (GuardianNames[0] == GuardianNames[1] || GuardianNames[1] == GuardianNames[2] || GuardianNames[0] == GuardianNames[2])
        { SmokeFail("Os guardiões dos três biomas precisam de identidades distintas"); yield break; }
        Debug.Log("SCADRYK_SMOKE: guardiões recebem identidades e memórias próprias");
        string[] abilityLessons = { "arco frontal", "explosão circular", "onda longa" };
        for (int ability = 0; ability < abilityLessons.Length; ability++)
        {
            string lesson = PowerAcquisitionHint(ability, "TESTE");
            if (!lesson.Contains("TESTE") || !lesson.Contains(abilityLessons[ability]) || !lesson.Contains("revela selos") || lesson.Length > 60)
            { SmokeFail("A apresentação do poder " + ability + " não explica os usos de combate e exploração numa frase curta"); yield break; }
        }
        Debug.Log("SCADRYK_SMOKE: cada guardião apresenta comando, efeito tático e uso dos selos do poder recebido");
        string commonPattern = ScadrykEnemyHealth.AttackReadabilityLabel(1, false, false, 2, 0, 0);
        string saltGuardianPattern = ScadrykEnemyHealth.AttackReadabilityLabel(1, true, false, 0, 0, 0);
        string finalPattern = ScadrykEnemyHealth.AttackReadabilityLabel(2, true, true, 0, 2, 3);
        string clarityRow = FormatAttackClarityEventCsvRow("session", "2026-11-22T00:00:00Z", 3, 2, finalPattern).TrimEnd('\n');
        if (!commonPattern.Contains("pulso de área") || !saltGuardianPattern.Contains("Vigia do Sal") ||
            !saltGuardianPattern.Contains("salva de estilhaços") || !finalPattern.Contains("Arauto fase 3") ||
            !finalPattern.Contains("onda de vazio") || clarityRow.Split(',').Length != 5 ||
            !clarityRow.EndsWith(finalPattern))
        { SmokeFail("O registro de legibilidade não identifica papéis, guardiões, fases do Arauto ou campos do CSV"); yield break; }
        Debug.Log("SCADRYK_SMOKE: registros F8 identificam padrão recente e serializam fase, sala e ataque");
        ScadrykCue laneCue = ScadrykEnemyHealth.WarningCueFor(0, false, false, 0);
        ScadrykCue volleyCue = ScadrykEnemyHealth.WarningCueFor(0, false, false, 1);
        ScadrykCue areaCue = ScadrykEnemyHealth.WarningCueFor(0, false, false, 2);
        ScadrykCue supportCue = ScadrykEnemyHealth.WarningCueFor(0, false, false, 3);
        ScadrykAudio audio = GetComponent<ScadrykAudio>();
        if (laneCue == volleyCue || laneCue == areaCue || laneCue == supportCue || volleyCue == areaCue || volleyCue == supportCue || areaCue == supportCue ||
            ScadrykEnemyHealth.WarningCueFor(0, true, false, 0) != laneCue ||
            ScadrykEnemyHealth.WarningCueFor(1, true, false, 0) != volleyCue ||
            ScadrykEnemyHealth.WarningCueFor(2, true, false, 0) != areaCue ||
            ScadrykEnemyHealth.WarningCueFor(2, true, true, 0) != ScadrykCue.FinalVolleyWarning ||
            ScadrykEnemyHealth.FinalBossPatternWarningCue(1) != ScadrykCue.FinalStarfallWarning ||
            ScadrykEnemyHealth.FinalBossPatternWarningCue(2) != ScadrykCue.FinalWaveWarning ||
            ScadrykEnemyHealth.FinalBossPatternWarningCue(-1) != ScadrykCue.FinalVolleyWarning ||
            ScadrykEnemyHealth.FinalBossPatternWarningCue(9) != ScadrykCue.FinalWaveWarning ||
            ScadrykCue.FinalVolleyWarning == ScadrykCue.FinalStarfallWarning ||
            ScadrykCue.FinalVolleyWarning == ScadrykCue.FinalWaveWarning ||
            ScadrykCue.FinalStarfallWarning == ScadrykCue.FinalWaveWarning ||
            audio == null || !audio.HasCombatLayer ||
            Mathf.Abs(ScadrykAudio.CombatIntensityFor(0, false, false)) > 0.001f ||
            Mathf.Abs(ScadrykAudio.CombatIntensityFor(1, false, false) - 0.32f) > 0.001f ||
            Mathf.Abs(ScadrykAudio.CombatIntensityFor(4, false, false) - 0.78f) > 0.001f ||
            Mathf.Abs(ScadrykAudio.CombatIntensityFor(1, true, false) - 1f) > 0.001f ||
            Mathf.Abs(ScadrykAudio.CombatIntensityFor(4, true, true)) > 0.001f ||
            Mathf.Abs(ScadrykAudio.BlendCombatIntensity(0f, 1f, 0.425f) - 0.5f) > 0.001f ||
            Mathf.Abs(ScadrykAudio.BlendCombatIntensity(1f, 0f, 0.425f) - 0.5f) > 0.001f ||
            Mathf.Abs(ScadrykAudio.WarningCueVolumeScale(0) - 0.8f) > 0.001f ||
            ScadrykAudio.WarningCueVolumeScale(1) >= ScadrykAudio.WarningCueVolumeScale(0) ||
            ScadrykAudio.WarningCueVolumeScale(4) <= 0f ||
            ScadrykAudio.MaxConcurrentWarningCues != 4 ||
            !ScadrykAudio.CanPlayWarningCue(ScadrykAudio.MaxConcurrentWarningCues - 1) ||
            ScadrykAudio.CanPlayWarningCue(ScadrykAudio.MaxConcurrentWarningCues) ||
            !audio.HasCue(laneCue) || !audio.HasCue(volleyCue) || !audio.HasCue(areaCue) ||
            !audio.HasCue(supportCue) || !audio.HasCue(ScadrykCue.FinalBossWarning) ||
            !audio.HasCue(ScadrykCue.FinalVolleyWarning) || !audio.HasCue(ScadrykCue.FinalStarfallWarning) ||
            !audio.HasCue(ScadrykCue.FinalWaveWarning) || !audio.HasCue(ScadrykCue.Memory) ||
            audio.CueClipName(ScadrykCue.FinalVolleyWarning) != "Herald remembered volley" ||
            audio.CueClipName(ScadrykCue.FinalStarfallWarning) != "Herald falling memory" ||
            audio.CueClipName(ScadrykCue.FinalWaveWarning) != "Herald void wave" ||
            !audio.HasCue(ScadrykCue.EchoReleased) || !audio.HasCue(ScadrykCue.PlayerStrike) || !audio.HasCue(ScadrykCue.PlayerFinisher) ||
            !audio.HasCue(ScadrykCue.PlayerPowerImpact) || !audio.HasCue(ScadrykCue.CoastalCurrent) || ScadrykCue.PlayerPowerImpact == ScadrykCue.Impact ||
            ScadrykCue.PlayerPowerImpact == ScadrykCue.Power || ScadrykCue.PlayerPowerImpact == ScadrykCue.PlayerStrike ||
            ScadrykCue.PlayerStrike == ScadrykCue.Impact || ScadrykCue.PlayerStrike == ScadrykCue.Sword ||
            ScadrykCue.PlayerFinisher == ScadrykCue.PlayerStrike || ScadrykCue.PlayerFinisher == ScadrykCue.Impact ||
            ScadrykCue.PlayerFinisher == ScadrykCue.Sword)
        { SmokeFail("Cues de perigo, música de combate ou sinais de impacto não distinguem os estados previstos"); yield break; }
        Debug.Log("SCADRYK_SMOKE: música escala de salas limpas a grupos e chefes e recua em pausas");
        Debug.Log("SCADRYK_SMOKE: cada padrão do Arauto tem cue próprio; avisos sobrepostos perdem volume e têm teto de quatro vozes");
        if (!FocusLossRequiresPause(false, false, false, false, false, false) ||
            FocusLossRequiresPause(true, false, false, false, false, false) ||
            FocusLossRequiresPause(false, true, false, false, false, false) ||
            FocusLossRequiresPause(false, false, true, false, false, false) ||
            FocusLossRequiresPause(false, false, false, true, false, false) ||
            FocusLossRequiresPause(false, false, false, false, true, false) ||
            FocusLossRequiresPause(false, false, false, false, false, true))
        { SmokeFail("A pausa por perda de foco foi aplicada em estado incorreto"); yield break; }
        Debug.Log("SCADRYK_SMOKE: perda de foco pausa só o jogo ativo");
        if (Vector2.Distance(ScadrykPlayerController.ResolveGamepadFacing(Vector2.up, Vector2.zero, Vector2.zero, false), Vector2.up) > 0.001f ||
            Vector2.Distance(ScadrykPlayerController.ResolveGamepadFacing(Vector2.up, Vector2.zero, Vector2.right, false), Vector2.right) > 0.001f ||
            Vector2.Distance(ScadrykPlayerController.ResolveGamepadFacing(Vector2.up, Vector2.zero, Vector2.right, true), Vector2.up) > 0.001f ||
            Vector2.Distance(ScadrykPlayerController.ResolveGamepadFacing(Vector2.up, Vector2.left, Vector2.right, true), Vector2.left) > 0.001f)
        { SmokeFail("A mira do controle não conserva direção nem prioriza o analógico direito"); yield break; }
        Debug.Log("SCADRYK_SMOKE: mira do controle preserva o twin-stick e tem fallback de movimento");
        Vector2 stickDrift = ScadrykSettings.ApplyRadialDeadzone(new Vector2(0.12f, 0.03f), 0.19f);
        Vector2 intentionalDiagonal = ScadrykSettings.ApplyRadialDeadzone(new Vector2(0.15f, 0.15f), 0.19f);
        Vector2 intentionalAim = ScadrykSettings.ApplyRadialDeadzone(new Vector2(-0.4f, 0.3f), 0.19f);
        Vector2 fullStick = ScadrykSettings.ApplyRadialDeadzone(Vector2.right, 0.19f);
        Vector2 halfwayStick = ScadrykSettings.ApplyRadialDeadzone(Vector2.right * 0.595f, 0.19f);
        string driftReadout = ScadrykSettings.GamepadStickReadout(new Vector2(0.12f, 0.03f), 0.19f);
        string movingReadout = ScadrykSettings.GamepadStickReadout(Vector2.right * 0.595f, 0.19f);
        if (stickDrift != Vector2.zero || intentionalDiagonal == Vector2.zero || intentionalAim == Vector2.zero ||
            Mathf.Abs(intentionalAim.magnitude - ((0.5f - 0.19f) / 0.81f)) > 0.001f ||
            Mathf.Abs(fullStick.magnitude - 1f) > 0.001f || Mathf.Abs(halfwayStick.magnitude - 0.5f) > 0.001f ||
            driftReadout != "X+12 Y+03 12→00%" || movingReadout != "X+60 Y00 60→50%" ||
            ScadrykSettings.DefaultGamepadDeadzone < ScadrykSettings.MinimumGamepadDeadzone ||
            ScadrykSettings.DefaultGamepadDeadzone > ScadrykSettings.MaximumGamepadDeadzone)
        { SmokeFail("A zona morta radial não filtra drift, preserva diagonal/mira ou remapeia progressivamente a faixa útil"); yield break; }
        Debug.Log("SCADRYK_SMOKE: zona morta radial filtra drift e remapeia progressivamente a faixa útil mantendo direção");
        if (ResolveInputMode("unknown", "keyboard_mouse") != "keyboard_mouse" ||
            ResolveInputMode("keyboard_mouse", "gamepad") != "gamepad" ||
            ResolveInputMode("gamepad", "keyboard_mouse") != "keyboard_mouse" ||
            ResolveInputMode("keyboard_mouse", "touch") != "keyboard_mouse" ||
            ResolveInputMode("unknown", null) != "unknown")
        { SmokeFail("A telemetria não acompanha trocas reais de dispositivo ou aceita modos inválidos"); yield break; }
        Debug.Log("SCADRYK_SMOKE: telemetria alterna teclado/controle e ignora modos inválidos");
        if (ScadrykPlayerController.InputBufferCanStart(1.1f, 1.3f, 1.2f) ||
            !ScadrykPlayerController.InputBufferCanStart(1.21f, 1.3f, 1.2f) ||
            ScadrykPlayerController.InputBufferCanStart(1.31f, 1.3f, 1.2f) ||
            Mathf.Abs(ScadrykPlayerController.BasicAttackBufferWindow - 0.2f) > 0.001f ||
            ScadrykPlayerController.NextBasicAttackStep(-1, false) != 0 ||
            ScadrykPlayerController.NextBasicAttackStep(0, true) != 1 ||
            ScadrykPlayerController.NextBasicAttackStep(1, true) != 2 ||
            ScadrykPlayerController.NextBasicAttackStep(2, true) != 0 ||
            ScadrykPlayerController.NextBasicAttackStep(1, false) != 0 ||
            ScadrykPlayerController.BasicAttackDamageScale(2) <= ScadrykPlayerController.BasicAttackDamageScale(1) ||
            ScadrykPlayerController.BasicAttackRangeScale(2) <= ScadrykPlayerController.BasicAttackRangeScale(1) ||
            ScadrykPlayerController.BasicAttackCooldownScale(2) <= ScadrykPlayerController.BasicAttackCooldownScale(1) ||
            ScadrykPlayerController.BasicAttackWindupForStep(2) <= ScadrykPlayerController.BasicAttackWindupForStep(1) ||
            ScadrykPlayerController.DashBufferCanStart(1.1f, 1.3f, 1.2f) ||
            !ScadrykPlayerController.DashBufferCanStart(1.21f, 1.3f, 1.2f) ||
            ScadrykPlayerController.DashBufferCanStart(1.31f, 1.3f, 1.2f) ||
            Mathf.Abs(ScadrykPlayerController.DashBufferWindow - 0.12f) > 0.001f)
        { SmokeFail("Os buffers curtos de ataque e esquiva iniciam fora da janela válida"); yield break; }
        Debug.Log("SCADRYK_SMOKE: combo de três golpes e buffers de ataque/esquiva respeitam as janelas configuradas");
        if (ScadrykPlayerController.AbilityWindupFor(0) <= 0f ||
            Mathf.Abs(ScadrykPlayerController.AbilityCooldownFor(0, true) - 5.6f) > 0.001f ||
            ScadrykPlayerController.AbilityCooldownFor(0, true) >= ScadrykPlayerController.AbilityCooldownFor(0, false) ||
            ScadrykPlayerController.AbilityCooldownFor(1, true) != ScadrykPlayerController.AbilityCooldownFor(1, false) ||
            ScadrykPlayerController.AbilityCooldownFor(2, true) != ScadrykPlayerController.AbilityCooldownFor(2, false) ||
            ScadrykEnemyHealth.SaltExposureDuration(false, true) <= ScadrykEnemyHealth.SaltExposureDuration(false, false) ||
            ScadrykEnemyHealth.SaltExposureDuration(true, true) <= ScadrykEnemyHealth.SaltExposureDuration(true, false) ||
            ScadrykEnemyHealth.TidePushStrength(true) <= ScadrykEnemyHealth.TidePushStrength(false) ||
            ScadrykEnemyHealth.TidePushDuration(true) <= ScadrykEnemyHealth.TidePushDuration(false) ||
            ScadrykPlayerController.ShockwaveWindup <= 0f ||
            ScadrykPlayerController.ShockwaveRecovery <= ScadrykPlayerController.ShockwaveWindup ||
            ScadrykPlayerController.AbilityWindupFor(1) < ScadrykPlayerController.AbilityWindupFor(0) ||
            ScadrykPlayerController.AbilityWindupFor(2) <= ScadrykPlayerController.AbilityWindupFor(1) ||
            ScadrykPlayerController.AbilityRecoveryFor(0) <= ScadrykPlayerController.AbilityWindupFor(0) ||
            ScadrykPlayerController.AbilityRecoveryFor(1) <= ScadrykPlayerController.AbilityWindupFor(1) ||
            ScadrykPlayerController.AbilityRecoveryFor(2) <= ScadrykPlayerController.AbilityWindupFor(2) ||
            ScadrykActorAnimator.AttackImpactStrength(0f, 0.26f) > 0.001f ||
            ScadrykActorAnimator.AttackImpactStrength(ScadrykPlayerController.BasicAttackWindupForStep(0), 0.26f) < 0.95f ||
            ScadrykActorAnimator.AttackImpactStrength(ScadrykPlayerController.BasicAttackWindupForStep(1), 0.26f) < 0.95f ||
            ScadrykActorAnimator.AttackImpactStrength(ScadrykPlayerController.BasicAttackWindupForStep(2), 0.38f) < 0.95f ||
            ScadrykActorAnimator.AttackImpactStrength(0.26f, 0.26f) > 0.001f ||
            ScadrykActorAnimator.AttackImpactStrength(0.38f, 0.38f) > 0.001f ||
            Mathf.Abs(ScadrykActorAnimator.AbilityImpactStrength(0f, 0.3f) - 1f) > 0.001f ||
            Mathf.Abs(ScadrykActorAnimator.AbilityImpactStrength(0.15f, 0.3f) - Mathf.Sqrt(0.5f)) > 0.001f ||
            ScadrykActorAnimator.AbilityImpactStrength(0.3f, 0.3f) > 0.001f ||
            !ScadrykPlayerController.AbilityFootprintContainsPoint(0, new Vector2(3f, 0f), Vector2.zero, Vector2.right) ||
            ScadrykPlayerController.AbilityFootprintContainsPoint(0, new Vector2(3f, 0f), Vector2.zero, Vector2.up))
        { SmokeFail("Os poderes não anunciam início, mantêm recuperação ou atingem segundo a mira fixada no começo da ação"); yield break; }
        Debug.Log("SCADRYK_SMOKE: poderes têm preparação/recuperação, mira fixa e aprimoramentos opcionais distintos");
        if (!RebindingCanBeCancelled(-1, 2) || !RebindingCanBeCancelled(4, -1) || RebindingCanBeCancelled(-1, -1) ||
            CanCaptureRebindKey(10, 10) || !CanCaptureRebindKey(11, 10))
        { SmokeFail("Cancelar remapeamento ou proteger a tecla de confirmação deixou de funcionar"); yield break; }
        Debug.Log("SCADRYK_SMOKE: Escape/Start cancela captura e a tecla que inicia remapeamento não é capturada no mesmo frame");
        if (!ScadrykEnemyHealth.AreaPulseContains(Vector2.zero, new Vector2(3.19f, 0f)) ||
            ScadrykEnemyHealth.AreaPulseContains(Vector2.zero, new Vector2(3.21f, 0f)) ||
            Mathf.Abs(ScadrykEnemyHealth.AreaPulseDiameter - ScadrykEnemyHealth.AreaPulseRadius * 2f) > 0.001f ||
            !ScadrykEnemyHealth.BellPulseContains(Vector2.zero, new Vector2(5.29f, 0f)) ||
            ScadrykEnemyHealth.BellPulseContains(Vector2.zero, new Vector2(5.31f, 0f)) ||
            Mathf.Abs(ScadrykEnemyHealth.BellPulseDiameter - ScadrykEnemyHealth.BellPulseRadius * 2f) > 0.001f ||
            !ScadrykEnemyHealth.SupportPulseContains(Vector2.zero, new Vector2(2.49f, 0f)) ||
            ScadrykEnemyHealth.SupportPulseContains(Vector2.zero, new Vector2(2.51f, 0f)) ||
            Mathf.Abs(ScadrykEnemyHealth.SupportPulseDiameter - ScadrykEnemyHealth.SupportPulseRadius * 2f) > 0.001f)
        { SmokeFail("Uma área circular de dano ou cura diverge do raio e do diâmetro anunciados"); yield break; }
        Debug.Log("SCADRYK_SMOKE: áreas de ataque e cura respeitam os círculos anunciados");
        string previousHintInputMode = sessionInputMode;
        sessionInputMode = "keyboard_mouse";
        string keyboardPulseHint = ProgressHint(4, 0, 0);
        string keyboardInteractHint = ProgressHint(1, 0, 0);
        string keyboardWitnessPrompt = WitnessPromptText();
        sessionInputMode = "gamepad";
        string gamepadPulseHint = ProgressHint(4, 0, 0);
        string gamepadInteractHint = ProgressHint(1, 0, 0);
        string gamepadWitnessPrompt = WitnessPromptText();
        sessionInputMode = previousHintInputMode;
        string compactControls = CompactControlHintText("teclado/LS", "mouse/RS", "F", "Space", "Q", "E");
        if (!keyboardPulseHint.Contains("(" + ScadrykSettings.KeyName(ScadrykSettings.Key(2)) + ")") ||
            !keyboardInteractHint.Contains("(" + ScadrykSettings.KeyName(ScadrykSettings.Key(6)) + ")") ||
            keyboardWitnessPrompt != ScadrykSettings.KeyName(ScadrykSettings.Key(6)) + "  ·  FALAR COM NIMA" ||
            !gamepadPulseHint.Contains("(" + ScadrykSettings.PadButtonName(ScadrykSettings.PadButton(2)) + ")") ||
            !gamepadInteractHint.Contains("(" + ScadrykSettings.PadButtonName(ScadrykSettings.PadButton(6)) + ")") ||
            gamepadWitnessPrompt != ScadrykSettings.PadButtonName(ScadrykSettings.PadButton(6)) + "  ·  FALAR COM NIMA" ||
            FormatInputBinding(KeyCode.F, 4, false) != "F" ||
            FormatInputBinding(KeyCode.Alpha1, 4, false) != "1" ||
            FormatInputBinding(KeyCode.Keypad1, 4, false) != "Num 1" ||
            FormatInputBinding(KeyCode.Space, 4, false) != "Espaço" ||
            FormatInputBinding(KeyCode.F, 4, true) != "LB" ||
            !compactControls.Contains("Mover: teclado/LS · mirar: mouse/RS") ||
            !compactControls.Contains("F atacar · Space esquivar · Q pulso") ||
            !compactControls.Contains("E interagir · M/L3 mapa · Esc/Start pausa") ||
            compactControls.Split('\n').Length != 3)
        { SmokeFail("Pistas e HUD não refletem os controles remapeados ou omitem ações na tela compacta"); yield break; }
        Debug.Log("SCADRYK_SMOKE: pistas dinâmicas refletem dispositivo e botões de jogo");
        bool previousPauseState = isPaused;
        float beforePauseClock = Time.time;
        isPaused = true;
        ApplyPauseTimeScale();
        yield return new WaitForSecondsRealtime(0.1f);
        bool pauseClockStopped = Time.timeScale == 0f && Mathf.Abs(Time.time - beforePauseClock) < 0.001f;
        isPaused = previousPauseState;
        ApplyPauseTimeScale();
        if (!pauseClockStopped) { SmokeFail("A pausa deixou o relógio de combate avançar"); yield break; }
        Debug.Log("SCADRYK_SMOKE: pausa congela o relógio e os avisos temporizados");
        bool previousOpeningStory = showOpeningStory;
        float beforeOpeningStoryClock = Time.time;
        showOpeningStory = true;
        ApplyPauseTimeScale();
        yield return new WaitForSecondsRealtime(0.1f);
        bool openingStoryPausesCombat = IsPaused && Time.timeScale == 0f &&
            Mathf.Abs(Time.time - beforeOpeningStoryClock) < 0.001f;
        showOpeningStory = previousOpeningStory;
        ApplyPauseTimeScale();
        if (!openingStoryPausesCombat) { SmokeFail("A abertura narrativa deixou o combate ou o cronômetro avançar"); yield break; }
        Debug.Log("SCADRYK_SMOKE: abertura narrativa pausa combate e cronômetro até confirmação");
        bool previousReduceShake = ScadrykSettings.ReduceShake;
        if (follow == null) { SmokeFail("A câmera não foi criada para verificar redução de tremor"); yield break; }
        ScadrykSettings.ReduceShake = false;
        follow.Shake(0.1f, 0.4f);
        ScadrykSettings.ReduceShake = true;
        follow.ApplyReducedMotion();
        bool shakeStopped = !follow.IsShaking;
        ScadrykSettings.ReduceShake = previousReduceShake;
        if (!shakeStopped) { SmokeFail("Reduzir tremor não interrompeu a sacudida já ativa"); yield break; }
        Debug.Log("SCADRYK_SMOKE: redução de tremor interrompe sacudida ativa");
        if (titleBackdropTexture == null) { SmokeFail("A arte de abertura ScadrykTitleBackdrop não foi carregada"); yield break; }
        Debug.Log("SCADRYK_SMOKE: arte original da tela inicial carregada");
        Sprite[] firstEnemyRow = LoadSheetRow("Enemies", 0, false);
        Sprite[] repeatedEnemyRow = LoadSheetRow("Enemies", 0, false);
        Sprite[] otherEnemyRow = LoadSheetRow("Enemies", 1, false);
        Sprite[] fullListenerSheet = LoadSheetRow("Listener", 0, true);
        if (firstEnemyRow == null || !object.ReferenceEquals(firstEnemyRow, repeatedEnemyRow) ||
            firstEnemyRow.Length != 4 || otherEnemyRow == null || object.ReferenceEquals(firstEnemyRow, otherEnemyRow) ||
            fullListenerSheet == null || fullListenerSheet.Length != 16 ||
            !object.ReferenceEquals(fullListenerSheet, LoadSheetRow("Listener", 3, true)))
        { SmokeFail("As folhas de animação não reutilizaram as linhas e quadros já criados"); yield break; }
        Debug.Log("SCADRYK_SMOKE: sprites de animação reutilizados entre atores e salas");
        RequestNewGame(true);
        if (!confirmNewJourney) { SmokeFail("Uma jornada nova não pediu confirmação diante de progresso salvo"); yield break; }
        overlaySelection = 1;
        SubmitGamepadOverlay();
        if (confirmNewJourney || overlaySelection != 0) { SmokeFail("Cancelar a jornada nova não restaurou o menu sem apagar o checkpoint"); yield break; }
        Debug.Log("SCADRYK_SMOKE: nova jornada com save pode ser cancelada");
        roomActiveSeconds = 17f;
        roomDeaths = 0;
        roomPowerUses[0] = 2;
        roomSecretsRevealed = 1;
        shallowWaterSeconds = 2.2f;
        resonanceActivations = 2;
        if (!IsCoastalCurrentMetricsRoom(2, 1) || IsCoastalCurrentMetricsRoom(1, 1) || IsCoastalCurrentMetricsRoom(2, 2) ||
            AddCoastalCurrentEntry(4, 1, 1) != 4 || AddCoastalCurrentExposure(3f, 2, 0, 0.8f) != 3f ||
            Mathf.Abs(AddCoastalCurrentExposure(0f, 2, 1, -0.2f)) > 0.001f ||
            !IsShallowWaterMetricsBiome(0) || IsShallowWaterMetricsBiome(1) ||
            Mathf.Abs(AddShallowWaterExposure(1f, 0, 1.2f) - 2.2f) > 0.001f ||
            Mathf.Abs(AddShallowWaterExposure(1f, 1, 1.2f) - 1f) > 0.001f ||
            Mathf.Abs(AddShallowWaterExposure(1f, 0, -0.2f) - 1f) > 0.001f ||
            !IsResonanceMetricsRoom(1, 1) || !IsResonanceMetricsRoom(2, 1) ||
            IsResonanceMetricsRoom(1, 0) || IsResonanceMetricsRoom(2, 2) ||
            AddResonanceActivation(1, 1, 1) != 2 || AddResonanceActivation(1, 1, 0) != 1)
        { SmokeFail("A telemetria ambiental contou bioma/sala errados ou exposição negativa"); yield break; }
        coastalCurrentEntries = AddCoastalCurrentEntry(AddCoastalCurrentEntry(0, 2, 1), 2, 1);
        coastalCurrentSeconds = AddCoastalCurrentExposure(AddCoastalCurrentExposure(0f, 2, 1, 2.1f), 2, 1, 2.5f);
        if (coastalCurrentEntries != 2 || Mathf.Abs(coastalCurrentSeconds - 4.6f) > 0.001f)
        { SmokeFail("A telemetria não somou entradas e segundos ativos da corrente"); yield break; }
        if (SecretRevealCountAfterSuccess(0, false) != 1 || SecretRevealCountAfterSuccess(3, false) != 4 ||
            SecretRevealCountAfterSuccess(3, true) != 3)
        { SmokeFail("A telemetria de selos não conta descobertas ou interfere no smoke"); yield break; }
        RecordInputMode("gamepad");
        RecordPlayerDeath();
        if (roomDeaths != 1) { SmokeFail("A derrota não foi contabilizada no instante da morte"); yield break; }
        string[] deathCsvColumns = FormatSessionCsvRow(false).TrimEnd('\n').Split(',');
        if (!System.Guid.TryParseExact(sessionId, "N", out _) ||
            deathCsvColumns.Length != SessionCsvHeader.TrimEnd('\n').Split(',').Length || deathCsvColumns[0] != sessionId ||
            deathCsvColumns[5] != "1" || deathCsvColumns[12] != "2" || deathCsvColumns[13] != "5" ||
            deathCsvColumns[15] != "1" || deathCsvColumns[16] != "2" || deathCsvColumns[17] != "2")
        { SmokeFail("O CSV não registraria uma morte antes de terminar a animação"); yield break; }
        RestartCurrentRoom();
        if (roomActiveSeconds != 17f || roomDeaths != 1 || roomPowerUses[0] != 2 || roomSecretsRevealed != 1 ||
            coastalCurrentEntries != 2 || Mathf.Abs(coastalCurrentSeconds - 4.6f) > 0.001f ||
            Mathf.Abs(shallowWaterSeconds - 2.2f) > 0.001f || resonanceActivations != 2 || sessionInputMode != "gamepad")
        { SmokeFail("Morte reiniciou a sala mas perdeu tempo, mortes, poderes, uso da corrente ou dispositivo da sessão"); yield break; }
        string[] csvHeaderColumns = SessionCsvHeader.TrimEnd('\n').Split(',');
        string[] csvRowColumns = FormatSessionCsvRow(true).TrimEnd('\n').Split(',');
        if (csvRowColumns.Length != csvHeaderColumns.Length || csvHeaderColumns[12] != "current_entries" ||
            csvHeaderColumns[13] != "current_seconds" || csvHeaderColumns[14] != "input_mode" ||
            csvHeaderColumns[15] != "seals_revealed" || csvHeaderColumns[16] != "shallow_water_seconds" ||
            csvHeaderColumns[17] != "resonance_activations" || csvRowColumns[14] != "gamepad" ||
            csvRowColumns[15] != "1" || csvRowColumns[16] != "2" || csvRowColumns[17] != "2")
        { SmokeFail("A linha CSV da sessão não corresponde ao cabeçalho ou omite o dispositivo"); yield break; }
        Debug.Log("SCADRYK_SMOKE: CSV v8 preserva corrente, água, ressonâncias e selos após morte e serializa cabeçalho/valores");
        ResetRoomSessionMetrics();
        if (roomActiveSeconds != 0f || roomDeaths != 0 || unclearAttackMarks != 0 || sessionInputMode != "unknown" ||
            roomPowerUses[0] != 0 || coastalCurrentEntries != 0 || coastalCurrentSeconds != 0f || roomSecretsRevealed != 0 ||
            shallowWaterSeconds != 0f || resonanceActivations != 0)
        { SmokeFail("O início de uma nova sala manteve métricas da sala anterior"); yield break; }
        Debug.Log("SCADRYK_SMOKE: métricas ambientais são por sala e reiniciam na nova tentativa");
        float laneRadius = ScadrykEnemyHealth.PlayerCollisionRadius(player);
        float laneWidth = ScadrykEnemyHealth.RootLaneVisualWidth(0.86f, laneRadius);
        if (!ScadrykEnemyHealth.RootLaneHits(new Vector2(1.5f, laneWidth * 0.5f - 0.01f), Vector2.zero, new Vector2(3.3f, 0f), 0.86f, laneRadius) ||
            ScadrykEnemyHealth.RootLaneHits(new Vector2(1.5f, laneWidth * 0.5f + 0.01f), Vector2.zero, new Vector2(3.3f, 0f), 0.86f, laneRadius))
        { SmokeFail("A borda de dano do golpe de raízes não corresponde à largura anunciada"); yield break; }
        Debug.Log("SCADRYK_SMOKE: largura do aviso de raízes corresponde à área de dano");
        Vector2 telegraphDirection = ScadrykEnemyHealth.AnnouncedAttackDirection(new Vector2(1f, 1f), new Vector2(4f, 5f));
        if ((telegraphDirection - new Vector2(0.6f, 0.8f)).sqrMagnitude > 0.001f ||
            (ScadrykEnemyHealth.AnnouncedAttackDirection(Vector2.one, Vector2.one) - Vector2.up).sqrMagnitude > 0.001f)
        { SmokeFail("A pose de impacto não segue a mira anunciada ou não resolve a direção nula"); yield break; }
        Debug.Log("SCADRYK_SMOKE: pose de impacto aponta para a mesma posição anunciada no telegráfico");
        if (!ScadrykPlayerController.WithinVisualFootprint(new Vector2(3.09f, 0f), Vector2.zero, Vector2.right, 1.05f, new Vector2(4.1f, 2.8f)) ||
            ScadrykPlayerController.WithinVisualFootprint(new Vector2(3.11f, 0f), Vector2.zero, Vector2.right, 1.05f, new Vector2(4.1f, 2.8f)) ||
            !ScadrykPlayerController.WithinVisualFootprint(new Vector2(6.39f, 0f), Vector2.zero, Vector2.right, 2.7f, new Vector2(7.4f, 4.4f)) ||
            ScadrykPlayerController.WithinVisualFootprint(new Vector2(6.41f, 0f), Vector2.zero, Vector2.right, 2.7f, new Vector2(7.4f, 4.4f)) ||
            !ScadrykPlayerController.WithinVisualFootprint(new Vector2(1.5f, 1.39f), Vector2.zero, Vector2.right, 1.05f, new Vector2(4.1f, 2.8f)) ||
            ScadrykPlayerController.WithinVisualFootprint(new Vector2(1.5f, 1.41f), Vector2.zero, Vector2.right, 1.05f, new Vector2(4.1f, 2.8f)))
        { SmokeFail("As áreas de acerto dos poderes Crescente e Rompante divergem das dimensões dos efeitos visuais"); yield break; }
        Debug.Log("SCADRYK_SMOKE: áreas do Crescente e Rompante respeitam as dimensões anunciadas");
        float saltburstRadius = ScadrykPlayerController.SaltburstAbilityRadius;
        if (!ScadrykPlayerController.AbilityFootprintContainsPoint(1, new Vector2(saltburstRadius - 0.01f, 0f), Vector2.zero, Vector2.right) ||
            ScadrykPlayerController.AbilityFootprintContainsPoint(1, new Vector2(saltburstRadius + 0.01f, 0f), Vector2.zero, Vector2.right) ||
            Mathf.Abs(ScadrykEffects.AbilitySaltburstVisualDiameter(saltburstRadius) * 0.5f - saltburstRadius) > 0.001f)
        { SmokeFail("O alcance e o sprite do Estouro de Sal divergem da área efetiva"); yield break; }
        Debug.Log("SCADRYK_SMOKE: sprite e alcance do Estouro de Sal correspondem ao raio efetivo");
        Vector2 slashFootprint = ScadrykEffects.PlayerSlashFootprint(1.45f);
        float slashTargetRadius = 0.5f;
        if (!ScadrykPlayerController.WithinVisualFootprint(new Vector2(ScadrykEffects.PlayerSlashForwardOffset + slashFootprint.x * 0.5f + slashTargetRadius - 0.01f, 0f), Vector2.zero, Vector2.right, ScadrykEffects.PlayerSlashForwardOffset, slashFootprint, slashTargetRadius) ||
            ScadrykPlayerController.WithinVisualFootprint(new Vector2(ScadrykEffects.PlayerSlashForwardOffset + slashFootprint.x * 0.5f + slashTargetRadius + 0.01f, 0f), Vector2.zero, Vector2.right, ScadrykEffects.PlayerSlashForwardOffset, slashFootprint, slashTargetRadius) ||
            !ScadrykPlayerController.WithinVisualFootprint(new Vector2(ScadrykEffects.PlayerSlashForwardOffset, slashFootprint.y * 0.5f + slashTargetRadius - 0.01f), Vector2.zero, Vector2.right, ScadrykEffects.PlayerSlashForwardOffset, slashFootprint, slashTargetRadius) ||
            ScadrykPlayerController.WithinVisualFootprint(new Vector2(ScadrykEffects.PlayerSlashForwardOffset, slashFootprint.y * 0.5f + slashTargetRadius + 0.01f), Vector2.zero, Vector2.right, ScadrykEffects.PlayerSlashForwardOffset, slashFootprint, slashTargetRadius))
        { SmokeFail("A borda do ataque básico não corresponde ao arco de espada visível"); yield break; }
        Debug.Log("SCADRYK_SMOKE: área do ataque básico corresponde ao arco de espada");
        if (Mathf.Abs(ScadrykEffects.PlayerPulseDiameter(player.ShockwaveRadius) * 0.5f - player.ShockwaveRadius) > 0.001f)
        { SmokeFail("O anel do Pulso não alcança o raio que pode causar dano"); yield break; }
        Debug.Log("SCADRYK_SMOKE: anel do Pulso cobre o raio efetivo de dano");
        if (Mathf.Abs(ScadrykEnemyHealth.FinalStarfallVisualDiameter * 0.5f - ScadrykEnemyHealth.FinalStarfallRadius) > 0.001f ||
            !ScadrykEnemyHealth.FinalStarfallHits(new Vector2(ScadrykEnemyHealth.FinalStarfallRadius - 0.01f, 0f), Vector2.zero) ||
            ScadrykEnemyHealth.FinalStarfallHits(new Vector2(ScadrykEnemyHealth.FinalStarfallRadius + 0.01f, 0f), Vector2.zero))
        { SmokeFail("Aviso e impacto da queda de estrelas não correspondem à área de dano"); yield break; }
        Debug.Log("SCADRYK_SMOKE: raio da queda de estrelas corresponde ao dano");
        float waveSafeRadius = ScadrykEnemyHealth.FinalWaveSafeRadius;
        float waveOuterRadius = ScadrykEnemyHealth.FinalWaveOuterRadius;
        if (Mathf.Abs(ScadrykEnemyHealth.FinalWaveSafeDiameter * 0.5f - waveSafeRadius) > 0.001f ||
            Mathf.Abs(ScadrykEnemyHealth.FinalWaveOuterDiameter * 0.5f - waveOuterRadius) > 0.001f ||
            ScadrykEnemyHealth.FinalWaveHitsAtDistance(waveSafeRadius - 0.01f) ||
            ScadrykEnemyHealth.FinalWaveHitsAtDistance(waveSafeRadius) ||
            !ScadrykEnemyHealth.FinalWaveHitsAtDistance(waveSafeRadius + 0.01f) ||
            !ScadrykEnemyHealth.FinalWaveHitsAtDistance(waveOuterRadius - 0.01f) ||
            ScadrykEnemyHealth.FinalWaveHitsAtDistance(waveOuterRadius) ||
            ScadrykEnemyHealth.FinalWaveHitsAtDistance(waveOuterRadius + 0.01f))
        { SmokeFail("Onda final do Arauto não preserva o anel seguro ou excede a borda externa anunciada"); yield break; }
        Debug.Log("SCADRYK_SMOKE: onda final do Arauto atinge só o anel entre as bordas anunciadas");
        var mapLabels = new System.Collections.Generic.HashSet<string>();
        for (int phase = 0; phase < PhaseNames.Length; phase++)
            for (int room = 0; room < RoomsPerPhase; room++)
                if (!mapLabels.Add(MapRoomLabel(phase, room))) { SmokeFail("O minimapa repete o nome de uma das nove salas"); yield break; }
        if (mapLabels.Count != PhaseNames.Length * RoomsPerPhase) { SmokeFail("O minimapa não identifica as nove salas"); yield break; }
        Debug.Log("SCADRYK_SMOKE: minimapa diferencia as nove salas");
        bool[] mapCleared = { true, false, false };
        if (WorldMapRoomStatus(0, 2, 1, 1, mapCleared) != 2 ||
            WorldMapRoomStatus(1, 0, 1, 1, mapCleared) != 2 ||
            WorldMapRoomStatus(1, 1, 1, 1, mapCleared) != 1 ||
            WorldMapRoomStatus(1, 2, 1, 1, mapCleared) != 0 ||
            WorldMapRoomStatus(2, 0, 1, 1, mapCleared) != 0 ||
            !WorldMapLayoutFits(400f, 360f) || !WorldMapLayoutFits(700f, 600f))
        { SmokeFail("O mapa geral perde estados de progresso ou sai da janela compacta"); yield break; }
        Debug.Log("SCADRYK_SMOKE: mapa geral marca bioma anterior, salas concluídas, atual e futuras em duas resoluções");
        if (OffscreenTargetArrow(new Vector2(0.5f, 0.5f)) != null ||
            OffscreenTargetArrow(new Vector2(1.2f, 0.5f)) != "→" ||
            OffscreenTargetArrow(new Vector2(-0.2f, 0.5f)) != "←" ||
            OffscreenTargetArrow(new Vector2(0.5f, 1.2f)) != "↑" ||
            OffscreenTargetArrow(new Vector2(0.5f, -0.2f)) != "↓")
        { SmokeFail("A bússola de tela não reconhece alvo visível nem as quatro direções fora da câmera"); yield break; }
        Debug.Log("SCADRYK_SMOKE: direção para alvo fora da câmera aponta para o quadrante correto");
        if (!MiniMapPortalLabelFits(new Rect(0f, 0f, 75f, 57f), 1f) ||
            !MiniMapPortalLabelFits(new Rect(0f, 0f, 90f, 67f), 1.5f))
        { SmokeFail("O marcador do portal sai do cartão da sala no minimapa"); yield break; }
        Debug.Log("SCADRYK_SMOKE: marcador PORTAL cabe no minimapa com texto padrão e ampliado");
        float ordinaryPortalDiameter = PortalDiameterFor(0, 0);
        float guardianPortalDiameter = PortalDiameterFor(0, RoomsPerPhase - 1);
        float endingPortalDiameter = PortalDiameterFor(PhaseNames.Length - 1, RoomsPerPhase - 1);
        if (Mathf.Abs(ordinaryPortalDiameter - 3.15f) > 0.001f ||
            Mathf.Abs(guardianPortalDiameter - 3.8f) > 0.001f ||
            Mathf.Abs(endingPortalDiameter - 8.5f) > 0.001f ||
            endingPortalDiameter < guardianPortalDiameter * 2f || endingPortalDiameter < ordinaryPortalDiameter * 2f)
        { SmokeFail("O portal final não se destaca dos portais das salas e dos guardiões"); yield break; }
        Debug.Log("SCADRYK_SMOKE: portal do encerramento tem diâmetro colossal, mais que o dobro das outras passagens");
        Rect markerCell = new Rect(0f, 0f, 75f, 57f), largeMarkerCell = new Rect(0f, 0f, 90f, 67f);
        if (!MiniMapPositionMarkerFits(markerCell, new Vector2(-100f, -100f), 1f) ||
            !MiniMapPositionMarkerFits(markerCell, new Vector2(100f, 100f), 1f) ||
            !MiniMapPositionMarkerFits(largeMarkerCell, new Vector2(-100f, -100f), 1.5f) ||
            !MiniMapPositionMarkerFits(largeMarkerCell, new Vector2(100f, 100f), 1.5f))
        { SmokeFail("Os marcadores do jogador ou alvo escapam do cartão do minimapa"); yield break; }
        Debug.Log("SCADRYK_SMOKE: marcadores de jogador e alvo cabem no minimapa em limites e escala ampliada");
        if (ProgressHint(1, 0, 0) == ProgressHint(1, 0, 1) ||
            ProgressHint(3, 0, 0) == ProgressHint(3, 0, 1) ||
            !ProgressHint(3, 0, 0).Contains("centro da bacia") ||
            !ProgressHint(3, 0, 1).Contains("centro da bacia") ||
            RoomAnchorOffsets(1, 0).Length != 1 || RoomAnchorOffsets(1, 0)[0].magnitude > 1f ||
            ProgressHint(4, 0, 0) == ProgressHint(4, 0, 1) || ProgressHint(4, 0, 1) == ProgressHint(4, 0, 2) ||
            ProgressHint(6, 0, 0) == ProgressHint(6, 0, 1) ||
            ProgressHint(7, 0, 0) == ProgressHint(7, 0, 1) || ProgressHint(7, 0, 1) == ProgressHint(7, 0, 2) ||
            !ProgressHint(7, 1, 0).Contains("sino do recife") ||
            Mathf.Abs(ProgressHintDelay(0) - 24f) > 0.001f || Mathf.Abs(ProgressHintDelay(1) - 18f) > 0.001f ||
            !ShouldAccumulateProgressHint(false, 0, 0, 2, 0f, 1f) ||
            ShouldAccumulateProgressHint(false, 0, 0, 2, 2f, 1f) ||
            ShouldAccumulateProgressHint(false, 1, 0, 2, 0f, 1f) ||
            ShouldAccumulateProgressHint(true, 0, 0, 2, 0f, 1f) ||
            ShouldAccumulateProgressHint(false, 0, 2, 2, 0f, 1f) ||
            !ShouldShowProgressHint(false, 0, 0, 2, 24.1f, 0, 0f, 25f) ||
            ShouldShowProgressHint(false, 0, 0, 2, 24.1f, 0, 26f, 25f) ||
            !ShouldShowProgressHint(false, 0, 0, 2, 24.1f, 0, 26f, 26f) ||
            ShouldShowProgressHint(false, 0, 0, 2, 17.9f, 1, 0f, 50f) ||
            !ShouldShowProgressHint(false, 0, 0, 2, 18.1f, 1, 0f, 50f) ||
            ShouldShowProgressHint(true, 0, 0, 2, 60f, 2, 0f, 60f) ||
            ShouldShowProgressHint(false, 0, 2, 2, 60f, 2, 0f, 60f) ||
            ShouldShowProgressHint(false, 1, 0, 2, 60f, 2, 0f, 60f))
        { SmokeFail("As pistas graduais de exploração não diferenciam as salas e etapas"); yield break; }
        Debug.Log("SCADRYK_SMOKE: pistas graduais específicas esperam o combate, progresso e mensagens ativas");
        Rect compactHud = HudPanelRect(400f, 360f, 1.5f);
        Rect compactMap = MiniMapPanelRect(400f, 360f, 1.5f);
        Rect narrowHud = HudPanelRect(700f, 600f, 1.5f);
        Rect narrowMap = MiniMapPanelRect(700f, 600f, 1.5f);
        Rect narrowLegend = MiniMapLegendRect(narrowMap, 1.5f);
        Rect shortStatus = StatusLabelRect(400f, 360f, 24f);
        Rect wrappedStatus = StatusLabelRect(400f, 360f, 72f);
        if (AbilityHudLabel(0, "Q", "pronto") != "Q  Crescente · interrompe  ·  pronto" ||
            AbilityHudLabel(0, "Q", "pronto", true) != "Q  Crescente · interrompe  ·  pronto  ·  eco" ||
            AbilityHudLabel(1, "E", "4.2s") != "E  Estouro · expõe  ·  4.2s" ||
            AbilityHudLabel(2, "R", "pronto") != "R  Rompante · afasta  ·  pronto" ||
            !string.IsNullOrEmpty(AbilityHudLabel(3, "X", "pronto")) ||
            !HudPanelFits(compactHud, 400f, 360f) || MiniMapPanelFits(compactMap, compactHud, 400f, 360f) ||
            !HudPanelFits(narrowHud, 700f, 600f) || !MiniMapPanelFits(narrowMap, narrowHud, 700f, 600f) ||
            narrowLegend.yMin < narrowMap.yMin || narrowLegend.yMax > narrowMap.yMax ||
            wrappedStatus.height <= shortStatus.height || wrappedStatus.xMin < 0f || wrappedStatus.xMax > 400f ||
            wrappedStatus.yMin < 0f || wrappedStatus.yMax > 360f)
        { SmokeFail("HUD, minimapa ou aviso de status sai da tela numa janela compacta com texto ampliado"); yield break; }
        Debug.Log("SCADRYK_SMOKE: HUD mostra a função tática dos três poderes e cabe em 400x360 com texto ampliado");
        if (!OptionsLayoutFits(600f) || !OptionsResetRowFits(400f))
        { SmokeFail("O menu de opções corta as teclas remapeáveis ou os controles de retorno a 600px de altura"); yield break; }
        Debug.Log("SCADRYK_SMOKE: menu de opções e restauração de comandos cabem em janela compacta");
        float shortOptionsViewport = CompactOptionsViewportHeight(360f);
        Rect lastCompactKey = CompactOptionsSelectionRect(1, 13);
        Rect lastCompactPad = CompactOptionsSelectionRect(2, 9);
        Rect compactPadDeadzone = CompactOptionsSelectionRect(2, 11);
        float keyListScroll = ScrollOptionItemIntoView(0f, shortOptionsViewport, lastCompactKey.y, lastCompactKey.height, CompactOptionsContentHeight(1));
        float padListScroll = ScrollOptionItemIntoView(0f, shortOptionsViewport, lastCompactPad.y, lastCompactPad.height, CompactOptionsContentHeight(2));
        float padDeadzoneScroll = ScrollOptionItemIntoView(0f, shortOptionsViewport, compactPadDeadzone.y, compactPadDeadzone.height, CompactOptionsContentHeight(2));
        if (!CompactOptionsLayoutFits(400f, 360f) || keyListScroll <= 0f || padListScroll <= 0f ||
            lastCompactKey.y + lastCompactKey.height > keyListScroll + shortOptionsViewport + 0.001f ||
            lastCompactPad.y + lastCompactPad.height > padListScroll + shortOptionsViewport + 0.001f ||
            compactPadDeadzone.y + compactPadDeadzone.height > padDeadzoneScroll + shortOptionsViewport + 0.001f ||
            Mathf.Abs(compactPadDeadzone.height - 108f) > 0.001f ||
            keyListScroll > CompactOptionsContentHeight(1) - shortOptionsViewport ||
            padListScroll > CompactOptionsContentHeight(2) - shortOptionsViewport ||
            padDeadzoneScroll > CompactOptionsContentHeight(2) - shortOptionsViewport)
        { SmokeFail("As opções roláveis não mantêm as últimas ações ou a zona morta visíveis em 400x360"); yield break; }
        Debug.Log("SCADRYK_SMOKE: opções roláveis preservam remapeamento e zona morta em 400x360");
        if (!SoundOptionsLayoutFits(600f))
        { SmokeFail("O controle de alto contraste ou as instruções de avisos saem do menu em janela baixa"); yield break; }
        bool oldHighContrast = ScadrykSettings.HighContrastWarnings;
        ScadrykSettings.HighContrastWarnings = false;
        float standardWarningWidth = ScadrykEffects.DangerWarningWidth(1f);
        ScadrykSettings.HighContrastWarnings = true;
        float highContrastWarningWidth = ScadrykEffects.DangerWarningWidth(1f);
        bool highContrastBrightensSignal = ScadrykEffects.DangerWarningColor(new Color(0.2f, 0.4f, 0.8f, 0.6f)).r > 0.2f;
        ScadrykSettings.HighContrastWarnings = oldHighContrast;
        if (Mathf.Abs(standardWarningWidth - 1f) > 0.001f || Mathf.Abs(highContrastWarningWidth - 1.4f) > 0.001f || !highContrastBrightensSignal)
        { SmokeFail("A opção de alto contraste não amplia e clareia os avisos de perigo"); yield break; }
        Debug.Log("SCADRYK_SMOKE: alto contraste clareia e amplia os avisos de perigo");
        float savedEffectsVolume = ScadrykSettings.EffectsVolume;
        bool savedWarningContrast = ScadrykSettings.HighContrastWarnings;
        GameObject mutedWarningProbe = null;
        bool warningVisibleWithoutSound = false;
        try
        {
            ScadrykSettings.EffectsVolume = 0f;
            ScadrykSettings.HighContrastWarnings = true;
            mutedWarningProbe = ScadrykEffects.TelegraphLine(Vector2.zero, Vector2.right * 3f, new Color(0.2f, 0.4f, 0.8f, 0.6f), 0.1f, 1f);
            SpriteRenderer warningRenderer = mutedWarningProbe != null ? mutedWarningProbe.GetComponent<SpriteRenderer>() : null;
            warningVisibleWithoutSound = warningRenderer != null && warningRenderer.sprite != null &&
                warningRenderer.color.a >= 0.98f && Mathf.Abs(mutedWarningProbe.transform.localScale.y - 0.14f) < 0.001f;
        }
        finally
        {
            if (mutedWarningProbe != null) Destroy(mutedWarningProbe);
            ScadrykSettings.EffectsVolume = savedEffectsVolume;
            ScadrykSettings.HighContrastWarnings = savedWarningContrast;
        }
        if (!warningVisibleWithoutSound)
        { SmokeFail("Um aviso visual de perigo ficou ausente ou fraco com o volume de efeitos zerado"); yield break; }
        Debug.Log("SCADRYK_SMOKE: aviso de perigo real continua visível em alto contraste com efeitos silenciados; preferências restauradas");
        if (ScadrykEffects.FlashAlphaScale(true) >= 1f || ScadrykEffects.FlashDiameterScale(true) >= 1f ||
            ScadrykEffects.FlashExpansionScale(1f, true) >= ScadrykEffects.FlashExpansionScale(1f, false) ||
            ScadrykEffects.DeathBurstMoteCount(true, true) >= ScadrykEffects.DeathBurstMoteCount(true, false))
        { SmokeFail("Reduzir flashes não atenua explosões, expansões ou partículas de derrota"); yield break; }
        Debug.Log("SCADRYK_SMOKE: reduzir flashes atenua explosões e expansões sem remover a leitura de perigo");
        if (!OverlayLayoutFits(400f, 360f) || !OverlayLayoutFits(1280f, 520f) || !OverlayLayoutFits(1280f, 720f) ||
            !PauseMapOverlayLayoutFits(400f, 360f))
        { SmokeFail("O menu inicial ou o menu de pausa com acesso ao mapa escapam de uma janela compacta"); yield break; }
        Debug.Log("SCADRYK_SMOKE: menu inicial cabe em 400×360, 1280×520 e 1280×720; menu de pausa cabe em 400×360");
        if (TitleMenuMaximumSelection(false) != 2 || TitleMenuOptionsSelection(false) != 1 || TitleMenuJournalSelection(false) != 2 ||
            TitleMenuMaximumSelection(true) != 3 || TitleMenuOptionsSelection(true) != 2 || TitleMenuJournalSelection(true) != 3)
        { SmokeFail("As opções de título ficam redundantes ou recebem índices incorretos conforme o save"); yield break; }
        Debug.Log("SCADRYK_SMOKE: menu inicial omite Nova jornada sem checkpoint e reindexa opções/diário corretamente");
        bool pauseGridNavigationWorks = MovePauseGridSelection(0, 0, 1) == 2 &&
            MovePauseGridSelection(3, 0, 1) == 5 && MovePauseGridSelection(4, 0, -1) == 2 &&
            MovePauseGridSelection(5, 0, -1) == 3 && MovePauseGridSelection(0, 1, 0) == 1 &&
            MovePauseGridSelection(1, -1, 0) == 0 && MovePauseGridSelection(4, 1, 0) == 5 &&
            MovePauseGridSelection(5, -1, 0) == 4;
        if (!pauseGridNavigationWorks) { SmokeFail("A navegação da grade da pausa perde a seleção nas bordas"); yield break; }
        Debug.Log("SCADRYK_SMOKE: grade de seis opções da pausa mantém coluna e respeita as bordas");
        bool victoryMenuNavigationWorks = MoveVictoryMenuSelection(0, 1) == 1 && MoveVictoryMenuSelection(1, 1) == 2 &&
            MoveVictoryMenuSelection(2, 1) == 2 && MoveVictoryMenuSelection(2, -1) == 1 &&
            MoveVictoryMenuSelection(1, -1) == 0 && MoveVictoryMenuSelection(0, -1) == 0 && MoveVictoryMenuSelection(1, 0) == 1;
        if (!victoryMenuNavigationWorks) { SmokeFail("A seleção linear do menu de vitória pula opções ou sai dos limites"); yield break; }
        Debug.Log("SCADRYK_SMOKE: menu final percorre nova jornada, caderno e título em ordem e respeita as bordas");
        if (!StoryJournalLayoutFits(400f, 360f) || !StoryJournalLayoutFits(700f, 600f))
        { SmokeFail("O caderno de memórias não cabe em janela compacta"); yield break; }
        Debug.Log("SCADRYK_SMOKE: caderno de memórias cabe em janela compacta e padrão");
        GameObject warningProbe = new GameObject("QA enemy attack warning");
        GameObject[] warningProbeList = { warningProbe };
        int remainingWarnings = ScadrykEnemyHealth.ClearTrackedAttackWarnings(warningProbeList, 1);
        if (remainingWarnings != 0 || warningProbe.activeSelf || warningProbeList[0] != null)
        { SmokeFail("Interromper um ataque não removeu seus avisos visuais pendentes"); yield break; }
        Debug.Log("SCADRYK_SMOKE: interromper um golpe desativa todos os telegráficos associados");
        if (!MemoryPanelFits(400f, 360f, 120f) || !MemoryPanelFits(700f, 600f, 90f) ||
            !MemoryDismissRequested(true, false, false) || !MemoryDismissRequested(false, true, false) ||
            !MemoryDismissRequested(false, false, true) || MemoryDismissRequested(false, false, false) ||
            !MemoryDismissedThisFrame(12, 12) || MemoryDismissedThisFrame(12, 13) ||
            MemoryInputModeAfterActivity("unknown", true, false) != "keyboard_mouse" ||
            MemoryInputModeAfterActivity("keyboard_mouse", false, true) != "gamepad" ||
            MemoryInputModeAfterActivity("gamepad", false, false) != "gamepad" ||
            ScadrykGameDirector.FitLabelFontSize(24, 240f, 160f) != 16 ||
            ScadrykGameDirector.FitLabelFontSize(24, 120f, 160f) != 24 ||
            MemoryDismissHintText(true, "Y") != "A / Y / B para continuar" ||
            MemoryDismissHintText(false, "E") != "E / Enter / Espaço / Esc para continuar" ||
            MemoryDismissHintText(true, "Y", true) != "A / Y / B" ||
            MemoryDismissHintText(false, "E", true) != "E / Enter / Esc" ||
            !FinalGuardianInterludeHint("Y").Contains("Rompante da Maré (Y)") ||
            !FinalGuardianInterludeHint("Y").Contains("selo opcional espera a nordeste") ||
            !FinalGuardianInterludeHint("Y").Contains("antes que o Arauto desperte") ||
            !ShouldOfferFinalSealPreparation(2, 2, true, true) ||
            ShouldOfferFinalSealPreparation(2, 2, false, true) ||
            ShouldOfferFinalSealPreparation(2, 2, true, false) ||
            ShouldOfferFinalSealPreparation(1, 2, true, true) ||
            FinalSealPreparationWindowSeconds < 8f ||
            OpeningStoryContinueHint(true, "Y") != "A / Y para continuar" ||
            OpeningStoryContinueHint(true, "A") != "A para continuar" ||
            OpeningStoryContinueHint(false, "E") != "E / Enter / Espaço para continuar" ||
            OpeningStoryContinueHint(false, "Enter") != "Enter / Espaço para continuar" ||
            !OpeningStoryCoversRoomStatus(true, 0, 0) || OpeningStoryCoversRoomStatus(false, 0, 0) ||
            OpeningStoryCoversRoomStatus(true, 0, 1) || OpeningStoryCoversRoomStatus(true, 1, 0))
        { SmokeFail("Memórias, comandos de saída ou a janela opcional do Selo final falharam"); yield break; }
        Debug.Log("SCADRYK_SMOKE: memórias modais têm comandos de saída válidos e cabem em janela compacta");
        int secondaryCountBeforeFallback = ScadrykEffects.SecondaryVisualCount;
        GameObject fallbackWarning = ScadrykEffects.CreateGroundLaneFallback(roomCenter + Vector2.up * 12f, Vector2.right,
            3f, 0.2f, new Color(1f, 0.2f, 0.2f, 1f), 10f);
        ScadrykTimedVisual fallbackTimedVisual = fallbackWarning != null ? fallbackWarning.GetComponent<ScadrykTimedVisual>() : null;
        bool fallbackProtected = fallbackWarning != null && fallbackWarning.activeInHierarchy &&
            fallbackTimedVisual != null && fallbackTimedVisual.IsDangerWarning &&
            ScadrykEffects.SecondaryVisualCount == secondaryCountBeforeFallback;
        GameObject ringFallbackWarning = ScadrykEffects.CreateRingFallback(roomCenter + Vector2.up * 18f, 3f,
            new Color(0.2f, 0.5f, 1f, 0.8f), 10f, true);
        ScadrykTimedVisual ringTimedVisual = ringFallbackWarning != null ? ringFallbackWarning.GetComponent<ScadrykTimedVisual>() : null;
        LineRenderer ringFallbackLine = ringFallbackWarning != null ? ringFallbackWarning.GetComponent<LineRenderer>() : null;
        float expectedRingLineWidth = ScadrykEffects.DangerWarningWidth(0.075f);
        bool ringFallbackProtected = ringFallbackWarning != null && ringFallbackWarning.activeInHierarchy &&
            ringTimedVisual != null && ringTimedVisual.IsDangerWarning && ringFallbackLine != null &&
            Mathf.Abs(ringFallbackLine.startWidth - expectedRingLineWidth) < 0.001f &&
            ScadrykEffects.SecondaryVisualCount == secondaryCountBeforeFallback;
        GameObject[] supportSignals = ScadrykEffects.SupportPulseSignal(roomCenter + Vector2.up * 24f,
            new Color(0.38f, 1f, 0.6f, 0.8f), 10f, true);
        ScadrykTimedVisual outerSupportSignal = supportSignals.Length > 0 && supportSignals[0] != null
            ? supportSignals[0].GetComponent<ScadrykTimedVisual>() : null;
        ScadrykTimedVisual innerSupportSignal = supportSignals.Length > 1 && supportSignals[1] != null
            ? supportSignals[1].GetComponent<ScadrykTimedVisual>() : null;
        bool supportSignalDistinct = supportSignals.Length == 2 && supportSignals[0] != null && supportSignals[1] != null &&
            outerSupportSignal != null && outerSupportSignal.IsDangerWarning && innerSupportSignal != null && innerSupportSignal.IsDangerWarning &&
            Mathf.Abs(ScadrykEffects.RingVisualDiameter(supportSignals[0]) - ScadrykEnemyHealth.SupportPulseDiameter) < 0.01f &&
            Mathf.Abs(ScadrykEffects.RingVisualDiameter(supportSignals[1]) - ScadrykEnemyHealth.SupportPulseInnerDiameter) < 0.01f &&
            ScadrykEffects.SecondaryVisualCount == secondaryCountBeforeFallback;
        GameObject priorityWarning = ScadrykEffects.TelegraphLine(roomCenter, roomCenter + Vector2.right * 3f,
            new Color(1f, 0.2f, 0.2f, 1f), 0.2f, 10f);
        for (int effect = 0; effect < 40; effect++)
            ScadrykEffects.Burst(roomCenter + Vector2.up * (20f + effect), Color.clear, 0.05f);
        bool effectBudgetRespected = ScadrykEffects.SecondaryVisualCount <= ScadrykEffects.SecondaryVisualLimit;
        bool warningProtected = priorityWarning != null && priorityWarning.activeInHierarchy;
        bool fallbackRemainsActive = fallbackWarning != null && fallbackWarning.activeInHierarchy;
        bool ringFallbackRemainsActive = ringFallbackWarning != null && ringFallbackWarning.activeInHierarchy;
        bool supportSignalsRemainActive = supportSignalDistinct && supportSignals[0].activeInHierarchy && supportSignals[1].activeInHierarchy;
        if (priorityWarning != null) Destroy(priorityWarning);
        if (fallbackWarning != null) Destroy(fallbackWarning);
        if (ringFallbackWarning != null) Destroy(ringFallbackWarning);
        if (supportSignals.Length > 0 && supportSignals[0] != null) Destroy(supportSignals[0]);
        if (supportSignals.Length > 1 && supportSignals[1] != null) Destroy(supportSignals[1]);
        if (!effectBudgetRespected || !warningProtected || !fallbackProtected || !fallbackRemainsActive ||
            !ringFallbackProtected || !ringFallbackRemainsActive || !supportSignalsRemainActive)
        { SmokeFail("O orçamento excedeu o teto, removeu aviso prioritário ou desprotegeu fallback de investida/anel/sinal de apoio"); yield break; }
        Debug.Log("SCADRYK_SMOKE: efeitos comuns respeitam o teto; fallbacks de investida/anel e contorno duplo do apoio continuam ativos");
        for (int room = 0; room < 9; room++)
        {
            if (phaseIndex * RoomsPerPhase + roomIndex != room) { SmokeFail("Sala inesperada: " + room); yield break; }
            if (!TryGetMiniMapTarget(out _)) { SmokeFail("O minimapa não identificou um alvo ativo na sala " + (room + 1)); yield break; }
            if (room % RoomsPerPhase == RoomsPerPhase - 1 &&
                (phaseGuardian == null || phaseGuardian.gameObject.name != GuardianNames[room / RoomsPerPhase]))
            { SmokeFail("O nome persistente não corresponde ao guardião da sala " + (room + 1)); yield break; }
            if (room == 8)
            {
                if (player.HasAbility(2) || !WorldSecretHint(2).Contains("após derrotar o guardião"))
                { SmokeFail("A pista do selo da arena final presume Rompante antes de derrotar o guardião"); yield break; }
                worldSecretHintShown = true;
            }
            if (room == 3 || room == 6)
            {
                int requiredPower = room == 3 ? 0 : 1;
                if (worldSecret == null || !ActiveRoomHasWalkableRoute(roomCenter, worldSecret.transform.position))
                { SmokeFail("O selo opcional da sala " + (room + 1) + " não pode ser alcançado pelo caminho livre dos colisores sólidos"); yield break; }
                ScadrykAmbientProp sealVisual = worldSecret.GetComponent<ScadrykAmbientProp>();
                if (sealVisual == null || !sealVisual.RevealsFor(player.transform))
                { SmokeFail("O selo opcional não vinculou sua revelação ao jogador na sala " + (room + 1)); yield break; }
                if (!SmokeRevealWorldSecret(requiredPower))
                { SmokeFail("O selo opcional não exigiu o poder correto ou não concedeu cura na sala " + (room + 1)); yield break; }
            }
            if (room < RoomsPerPhase)
            {
                ScadrykTerrainZone[] waterZones = FindObjectsByType<ScadrykTerrainZone>();
                if (waterZones.Length == 0) { SmokeFail("Águas rasas sem zonas de terreno na sala " + (room + 1)); yield break; }
                foreach (ScadrykTerrainZone zone in waterZones)
                    if (zone.MovementScale >= 1f) { SmokeFail("Zona de água não reduz o movimento"); yield break; }
            }
            for (int previousRoom = 0; previousRoom < roomIndex; previousRoom++)
                if (!clearedRooms[previousRoom]) { SmokeFail("O mapa não marcou a sala anterior como concluída: " + previousRoom); yield break; }
            foreach (ScadrykEnemyHealth enemy in FindObjectsByType<ScadrykEnemyHealth>())
                if (enemy != null && !enemy.IsFinalBoss) enemy.TakeDamage(99999f);
            if (!ActiveRoomHasSealedBoundaries())
            { SmokeFail("A sala " + (room + 1) + " não isolou seus limites de progressão"); yield break; }
            if (!ActiveRoomHasWalkableRoute(roomCenter))
            { SmokeFail("A geometria sólida ativa bloqueou a rota caminhável da sala " + (room + 1)); yield break; }
            if (roomIndex == 1 && TryGetSideRoute(phaseIndex, roomIndex, out int routeSide, out float routeEntry, out float routeExit))
            {
                Vector2 routeStart = roomCenter + new Vector2(routeEntry, routeSide * 5.4f);
                Vector2 routeTarget = roomCenter + new Vector2(routeExit, routeSide * 5.4f);
                if (!ActiveRoomHasWalkableRoute(roomCenter, routeStart, routeTarget))
                { SmokeFail("O desvio alternativo não conecta as duas aberturas na sala " + (room + 1)); yield break; }
                Debug.Log("SCADRYK_SMOKE: rota principal e desvio alternativo abertos na sala " + (room + 1));
            }
            Debug.Log("SCADRYK_SMOKE: sala " + (room + 1) + "/9 isolada e rota ao portal livre dos colisores sólidos do bioma");
            for (int anchorIndex = 0; anchorIndex < roomAnchors.Length; anchorIndex++)
            {
                if (roomAnchors[anchorIndex] != null && !ActiveRoomHasWalkableRoute(roomCenter, roomAnchors[anchorIndex].transform.position))
                { SmokeFail("O sinal de ressonância " + (anchorIndex + 1) + " da sala " + (room + 1) + " está isolado pela geometria sólida"); yield break; }
            }
            if (room == 8 && finalBoss != null)
            { SmokeFail("O Arauto apareceu antes de Nara concluir a memória do terceiro guardião"); yield break; }
            if (room == 8 && !statusMessage.Contains("última lembrança"))
            { SmokeFail("O intervalo seguro antes do Arauto não explicou a última lembrança"); yield break; }
            if (room == 8 && (worldSecret == null || !ActiveRoomHasWalkableRoute(roomCenter, worldSecret.transform.position)))
            { SmokeFail("O selo opcional da arena final não pode ser alcançado pelo caminho livre dos colisores sólidos"); yield break; }
            if (room == 8 && (worldSecret.GetComponent<ScadrykAmbientProp>() == null ||
                !worldSecret.GetComponent<ScadrykAmbientProp>().RevealsFor(player.transform)))
            { SmokeFail("O selo final não vinculou sua revelação ao jogador após retomar a fase"); yield break; }
            if (room == 8 && (worldSecretHintShown || !player.HasAbility(2) ||
                !WorldSecretHint(2).Contains("Rompante da Maré")))
            { SmokeFail("A pista do selo não foi atualizada após aprender Rompante com o guardião"); yield break; }
            if (room == 8)
            {
                float finalSealWindowDeadline = Time.time + 9f;
                while (!statusMessage.Contains("Você tem 12 segundos") && Time.time < finalSealWindowDeadline) yield return null;
                if (finalBoss != null || worldSecret == null || !statusMessage.Contains("Você tem 12 segundos"))
                { SmokeFail("O Arauto despertou antes da janela segura para alcançar o selo final"); yield break; }
                Debug.Log("SCADRYK_SMOKE: selo final permanece alcançável durante a janela segura antes do Arauto");
            }
            if (room == 8 && !SmokeRevealWorldSecret(2))
            { SmokeFail("O selo opcional da costa não exigiu Rompante ou não concedeu cura após o guardião"); yield break; }
            if (room == 8)
            {
                if (finalBoss != null || memoryUntil <= Time.time || memoryText != WorldSecretMemory(2))
                { SmokeFail("O Arauto surgiu antes da lembrança do Selo final terminar"); yield break; }
                float finalBossSpawnDeadline = Time.time + 8f;
                while (finalBoss == null && Time.time < finalBossSpawnDeadline) yield return null;
                if (finalBoss == null) { SmokeFail("Arauto não surgiu após o terceiro guardião"); yield break; }
                if (!TryGetMiniMapTarget(out Vector2 bossMapTarget) ||
                    Vector2.Distance(bossMapTarget, finalBoss.transform.position) > 0.001f)
                { SmokeFail("O minimapa não acompanhou o Arauto após a última memória"); yield break; }
                if (string.IsNullOrEmpty(InputBinding(5)) || !statusMessage.Contains(PowerAcquisitionHint(2, InputBinding(5))))
                { SmokeFail("O aviso do terceiro poder não explicou seu efeito e vínculo antes da luta final"); yield break; }
                smokeRestoreFinalGuardianForRestart = true;
                player.TakeDamage(player.MaxHealth + 1f);
                float deathRestartDeadline = Time.time + 3f;
                while (player.Health <= 0f && Time.time < deathRestartDeadline) yield return null;
                smokeRestoreFinalGuardianForRestart = false;
                if (player.Health <= 0f || finalBoss != null || resumeFinalGuardianInterlude || enemiesRemaining != 1 ||
                    memoryText != GuardianMemories[PhaseNames.Length - 1] || memoryUntil <= Time.time)
                { SmokeFail("Morrer diante do Arauto não retomou a última lembrança com a arena reconstruída e o chefe aguardando"); yield break; }
                float resumedBossDeadline = Time.time + 8f;
                while (finalBoss == null && Time.time < resumedBossDeadline) yield return null;
                if (finalBoss == null || enemiesRemaining != 1 || phaseGuardian != null)
                { SmokeFail("O Arauto não reapareceu após a lembrança reaberta na retomada por morte"); yield break; }
                if (!TryGetMiniMapTarget(out bossMapTarget) || Vector2.Distance(bossMapTarget, finalBoss.transform.position) > 0.001f)
                { SmokeFail("O minimapa não voltou a apontar para o Arauto após morrer na arena final"); yield break; }
                Debug.Log("SCADRYK_SMOKE: morte na arena final reconstrói a espera, relê a lembrança e desperta o Arauto novamente");
                var bossType = typeof(ScadrykEnemyHealth);
                var queuedStageField = bossType.GetField("queuedFinalStage", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var attackIndexField = bossType.GetField("finalAttackIndex", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var attackTargetField = bossType.GetField("attackTarget", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var beginBossAttack = bossType.GetMethod("BeginFinalBossAttack", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var resolveBossAttack = bossType.GetMethod("ResolveFinalBossAttack", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (queuedStageField == null || attackIndexField == null || attackTargetField == null || beginBossAttack == null || resolveBossAttack == null)
                { SmokeFail("Não foi possível preparar a checagem de fase do Arauto"); yield break; }
                foreach (ScadrykEnemyProjectile projectile in FindObjectsByType<ScadrykEnemyProjectile>())
                    if (projectile != null) { projectile.gameObject.SetActive(false); Destroy(projectile.gameObject); }
                ClearSmokeAttackWarnings();
                attackIndexField.SetValue(finalBoss, 0);
                attackTargetField.SetValue(finalBoss, (Vector2)player.transform.position);
                Vector2 volleyForward = ((Vector2)player.transform.position - (Vector2)finalBoss.transform.position).normalized;
                beginBossAttack.Invoke(finalBoss, null);
                int announcedStage = (int)queuedStageField.GetValue(finalBoss);
                finalBoss.TakeDamage(400f);
                float volleyWidth = ScadrykEnemyProjectile.HitboxDiameter(0.58f);
                if (announcedStage != 1 || finalBoss.FinalStage != 2 || finalBoss.DisplayStage != 1 ||
                    !SmokeAttackWarningsMatch((Vector2)finalBoss.transform.position, volleyForward, 7.2f, volleyWidth, 0.19f, 1))
                { SmokeFail("O teste não cruzou o limiar de fase durante o aviso do Arauto"); yield break; }
                resolveBossAttack.Invoke(finalBoss, null);
                int announcedVolleyCount = 0;
                bool volleyRangeMatchesWarning = true;
                foreach (ScadrykEnemyProjectile projectile in FindObjectsByType<ScadrykEnemyProjectile>())
                {
                    if (projectile == null) continue;
                    if (projectile.gameObject.name == "Arauto — remembered volley")
                    {
                        announcedVolleyCount++;
                        if (Mathf.Abs(projectile.TravelDistanceLimit - (7.2f - 0.75f - ScadrykEnemyProjectile.HitboxRadius(0.58f))) > 0.01f) volleyRangeMatchesWarning = false;
                    }
                    projectile.gameObject.SetActive(false);
                    Destroy(projectile.gameObject);
                }
                if (announcedVolleyCount != 3 || !volleyRangeMatchesWarning)
                { SmokeFail("A salva do Arauto mudou de fase ou seu alcance excedeu o aviso"); yield break; }
                if (finalBoss.DisplayStage != 2)
                { SmokeFail("A barra do Arauto não avançou após resolver a salva anunciada na fase anterior"); yield break; }
                Debug.Log("SCADRYK_SMOKE: salva do Arauto manteve fase e alcance dentro do aviso");

                attackTargetField.SetValue(finalBoss, (Vector2)player.transform.position);
                attackIndexField.SetValue(finalBoss, 0);
                ClearSmokeAttackWarnings();
                volleyForward = ((Vector2)player.transform.position - (Vector2)finalBoss.transform.position).normalized;
                beginBossAttack.Invoke(finalBoss, null);
                float expectedCrystalRange = Mathf.Clamp(Vector2.Distance(finalBoss.transform.position, player.transform.position) + 1.25f, 2.8f, 7.2f);
                if ((int)queuedStageField.GetValue(finalBoss) != 2 ||
                    !SmokeAttackWarningsMatch((Vector2)finalBoss.transform.position, volleyForward, 7.2f, ScadrykEnemyProjectile.HitboxDiameter(0.58f), 0.19f, 2))
                { SmokeFail("A salva da fase 2 não anuncia as cinco trajetórias"); yield break; }
                resolveBossAttack.Invoke(finalBoss, null);
                if (SmokeProjectileCount("Arauto — remembered volley") != 5 ||
                    !SmokeProjectileRangeMatches("Arauto — remembered volley", expectedCrystalRange - 0.55f - ScadrykEnemyProjectile.HitboxRadius(0.34f)))
                { SmokeFail("As cinco trajetórias da fase 2 não respeitam a faixa anunciada"); yield break; }
                ClearSmokeProjectiles();
                ClearSmokeAttackWarnings();

                attackIndexField.SetValue(finalBoss, 1);
                Vector2 starfallForward = ((Vector2)player.transform.position - (Vector2)finalBoss.transform.position).normalized;
                beginBossAttack.Invoke(finalBoss, null);
                float starfallProjectileWidth = ScadrykEnemyProjectile.HitboxDiameter(0.48f);
                if ((int)queuedStageField.GetValue(finalBoss) != 2 || !SmokeAttackWarningsMatch((Vector2)finalBoss.transform.position, starfallForward, 10f, starfallProjectileWidth, 0.48f, 0))
                { SmokeFail("A queda de estrelas da fase 2 não anuncia seu projétil lateral com a largura correta"); yield break; }
                resolveBossAttack.Invoke(finalBoss, null);
                if (SmokeProjectileCount("Arauto — falling memory") != 1 || !SmokeProjectileRangeMatches("Arauto — falling memory", 10f - ScadrykEnemyProjectile.HitboxRadius(0.48f)))
                { SmokeFail("A queda de estrelas da fase 2 não correspondeu ao aviso ou excedeu seu alcance"); yield break; }
                ClearSmokeProjectiles();
                ClearSmokeAttackWarnings();
                Debug.Log("SCADRYK_SMOKE: projétil lateral da queda de estrelas anunciado na fase 2");

                finalBoss.TakeDamage(600f);
                attackIndexField.SetValue(finalBoss, 0);
                ClearSmokeAttackWarnings();
                volleyForward = ((Vector2)player.transform.position - (Vector2)finalBoss.transform.position).normalized;
                beginBossAttack.Invoke(finalBoss, null);
                expectedCrystalRange = Mathf.Clamp(Vector2.Distance(finalBoss.transform.position, player.transform.position) + 1.25f, 2.8f, 7.2f);
                if ((int)queuedStageField.GetValue(finalBoss) != 3 ||
                    !SmokeAttackWarningsMatch((Vector2)finalBoss.transform.position, volleyForward, 7.2f, ScadrykEnemyProjectile.HitboxDiameter(0.58f), 0.19f, 3))
                { SmokeFail("A salva da fase 3 não anuncia as sete trajetórias"); yield break; }
                resolveBossAttack.Invoke(finalBoss, null);
                if (SmokeProjectileCount("Arauto — remembered volley") != 7 ||
                    !SmokeProjectileRangeMatches("Arauto — remembered volley", expectedCrystalRange - 0.55f - ScadrykEnemyProjectile.HitboxRadius(0.34f)))
                { SmokeFail("As sete trajetórias da fase 3 não respeitam a faixa anunciada"); yield break; }
                ClearSmokeProjectiles();
                ClearSmokeAttackWarnings();

                attackIndexField.SetValue(finalBoss, 1);
                starfallForward = ((Vector2)player.transform.position - (Vector2)finalBoss.transform.position).normalized;
                beginBossAttack.Invoke(finalBoss, null);
                if ((int)queuedStageField.GetValue(finalBoss) != 3 || !SmokeAttackWarningsMatch((Vector2)finalBoss.transform.position, starfallForward, 10f, starfallProjectileWidth, 0.48f, 1))
                { SmokeFail("A queda de estrelas da fase 3 não anuncia os três projéteis laterais"); yield break; }
                resolveBossAttack.Invoke(finalBoss, null);
                if (SmokeProjectileCount("Arauto — falling memory") != 3 || !SmokeProjectileRangeMatches("Arauto — falling memory", 10f - ScadrykEnemyProjectile.HitboxRadius(0.48f)))
                { SmokeFail("A queda de estrelas da fase 3 não correspondeu aos avisos ou excedeu seu alcance"); yield break; }
                ClearSmokeProjectiles();
                ClearSmokeAttackWarnings();
                Debug.Log("SCADRYK_SMOKE: três projéteis da queda de estrelas anunciados na fase 3");
                finalBoss.TakeDamage(99999f);
                ScadrykPortalVisual endingPortal = gateObject != null ? gateObject.GetComponent<ScadrykPortalVisual>() : null;
                if (finalBoss != null || endingPortal == null ||
                    Mathf.Abs(endingPortal.Diameter - PortalDiameterFor(phaseIndex, roomIndex)) > 0.001f ||
                    !statusMessage.Contains("portal colossal") ||
                    FindAnyObjectByType<ScadrykEnemyProjectile>() != null)
                { SmokeFail("Derrotar o Arauto não abriu o portal colossal esperado ou deixou projéteis ativos"); yield break; }
                Debug.Log("SCADRYK_SMOKE: derrota do Arauto limpa projéteis e abre portal de 8,5 unidades");
            }
            if (roomIndex == 2 && (player == null || !player.HasAbility(phaseIndex)))
            { SmokeFail("Poder não recebido após guardião da fase " + phaseIndex); yield break; }
            if (roomIndex == 2)
            {
                if (phaseIndex < 2 && !statusMessage.Contains(PowerAcquisitionHint(phaseIndex, InputBinding(phaseIndex + 3))))
                { SmokeFail("O aviso de aquisição não ensinou o efeito do poder da fase " + phaseIndex); yield break; }
                Vector2 testDirection = player.Facing.sqrMagnitude > 0.01f ? player.Facing.normalized : Vector2.right;
                float targetDistance = phaseIndex == 2 ? 5.5f : 2.75f;
                var testTarget = new GameObject("Smoke target for newly learned power");
                testTarget.transform.SetParent(levelRoot.transform);
                testTarget.transform.position = (Vector2)player.transform.position + testDirection * targetDistance;
                testTarget.AddComponent<CircleCollider2D>();
                var testBody = testTarget.AddComponent<Rigidbody2D>();
                testBody.gravityScale = 0f;
                var testHealth = testTarget.AddComponent<ScadrykEnemyHealth>();
                testHealth.Configure(1000f, 0f, false, phaseIndex, null, 0);
                Physics2D.SyncTransforms();
                var useAbility = typeof(ScadrykPlayerController).GetMethod("UseAbility", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (useAbility == null) { SmokeFail("Ação de poder indisponível"); yield break; }
                useAbility.Invoke(player, new object[] { phaseIndex });
                if (player.AbilityCooldownLeft(phaseIndex) <= 0f)
                { SmokeFail("Poder recebido não pôde ser usado imediatamente na fase " + phaseIndex); yield break; }
                yield return new WaitForSeconds(ScadrykPlayerController.AbilityWindupFor(phaseIndex) + 0.03f);
                bool tacticalEffect = phaseIndex == 0 ? testHealth.IsStaggered : phaseIndex == 1 ? testHealth.IsExposed : (testHealth.IsKnockedBack || testHealth.IsStaggered);
                if (testHealth.Health >= 1000f || !tacticalEffect)
                { SmokeFail("Poder da fase " + phaseIndex + " não causou dano ou não aplicou seu efeito tático"); yield break; }
                Debug.Log("SCADRYK_SMOKE: poder " + (phaseIndex + 1) + " causou dano e aplicou seu efeito tático");
                Destroy(testTarget);
            }
            if (room == 3 || room == 6)
            {
                if (witnessObject == null) { SmokeFail("Nima não apareceu na sala tranquila " + room); yield break; }
                string[] witnessLines = room == 3 ? BasinWitnessLines : CoastWitnessLines;
                for (int line = 0; line < witnessLines.Length; line++)
                {
                    TryInteract((Vector2)witnessObject.transform.position);
                    if (memoryText != witnessLines[line])
                    { SmokeFail("A conversa de Nima perdeu a fala " + (line + 1) + " na sala " + room); yield break; }
                }
                Debug.Log("SCADRYK_SMOKE: as três falas de Nima avançam em ordem na sala " + (room + 1) + "/9");
            }
            GameObject nextAnchor = null;
            for (int i = 0; i < anchorsRequired; i++)
                if (roomAnchors[i] != null) { nextAnchor = roomAnchors[i]; break; }
            if (nextAnchor != null && (!TryGetMiniMapTarget(out Vector2 anchorMapTarget) ||
                Vector2.Distance(anchorMapTarget, nextAnchor.transform.position) > 0.001f))
            { SmokeFail("O minimapa não apontou para o próximo sinal na sala " + (room + 1)); yield break; }
            if (room == 7 && anchorsNeedSequence && roomAnchors[1] != null)
            {
                TryPulseAnchor(roomAnchors[1].transform.position);
                if (anchorsActivated != 0 || roomAnchors[1] == null)
                { SmokeFail("O sino do recife respondeu antes do farol"); yield break; }
                if (!TryGetMiniMapTarget(out anchorMapTarget) || Vector2.Distance(anchorMapTarget, roomAnchors[0].transform.position) > 0.001f)
                { SmokeFail("A indicação do minimapa mudou após uma ordem de sinais inválida"); yield break; }
                Debug.Log("SCADRYK_SMOKE: sala 8 exige farol antes do sino do recife");
            }
            for (int i = 0; i < anchorsRequired; i++)
            {
                GameObject anchor = roomAnchors[i];
                if (anchor == null) continue;
                Vector2 spot = anchor.transform.position;
                if (anchorsNeedPulse)
                {
                    int activatedBefore = anchorsActivated;
                    TryPulseAnchor(spot + Vector2.right * (player.ShockwaveRadius + 0.02f));
                    if (anchorsActivated != activatedBefore || roomAnchors[i] == null)
                    { SmokeFail("O sinal respondeu fora do alcance visual do Pulso na sala " + room); yield break; }
                    TryPulseAnchor(spot);
                }
                else TryInteract(spot);
            }
            int expectedResonances = IsResonanceMetricsRoom(phaseIndex, roomIndex) ? anchorsRequired : 0;
            if (enemiesRemaining != 0 || anchorsActivated != anchorsRequired || gateObject == null || resonanceActivations != expectedResonances)
            { SmokeFail("Objetivo não abriu portal na sala " + room); yield break; }
            if (!TryGetMiniMapTarget(out Vector2 portalTarget) || Vector2.Distance(portalTarget, exitPosition) > 0.001f)
            { SmokeFail("O minimapa não marcou o portal aberto na sala " + (room + 1)); yield break; }
            ScadrykPortalVisual activePortal = gateObject.GetComponent<ScadrykPortalVisual>();
            if (activePortal == null || !PortalCanBeEntered(false, true) || PortalCanBeEntered(true, false))
            { SmokeFail("A porta do portal aceitou um estado ausente ou ainda fechado"); yield break; }
            if (!activePortal.IsOpen)
            {
                TryUseExit(exitPosition);
                if (transitioning) { SmokeFail("Nara atravessou o portal antes da animação terminar"); yield break; }
                float portalOpenDeadline = Time.time + 1.2f;
                while (!activePortal.IsOpen && Time.time < portalOpenDeadline) yield return null;
                if (!PortalCanBeEntered(gateObject != null && activePortal != null, activePortal != null && activePortal.IsOpen))
                { SmokeFail("O portal não concluiu a animação de abertura"); yield break; }
            }
            ScadrykEnemyProjectile.Launch(roomCenter + Vector2.up * 6f, Vector2.right, 0.1f, 0f);
            ScadrykEnemyProjectile smokeProjectile = FindAnyObjectByType<ScadrykEnemyProjectile>();
            if (smokeProjectile == null || !smokeProjectile.transform.IsChildOf(levelRoot.transform))
            { SmokeFail("Projétil de inimigo não foi associado à sala " + room); yield break; }
            int oldRoom = room;
            TryUseExit(exitPosition);
            float timeout = Time.time + 8f;
            while (transitioning && Time.time < timeout) yield return null;
            yield return null;
            if (transitioning || (room < 8 && phaseIndex * RoomsPerPhase + roomIndex == oldRoom))
            { SmokeFail("Transição falhou após sala " + room); yield break; }
            if (FindAnyObjectByType<ScadrykEnemyProjectile>() != null)
            { SmokeFail("Projétil de inimigo sobreviveu à transição da sala " + (room + 1)); yield break; }
            Debug.Log("SCADRYK_SMOKE: sala " + (room + 1) + "/9 concluída");
        }
        if (!victory) { SmokeFail("Portal final não encerrou a campanha"); yield break; }
        if (statusMessage != VictoryEndingText) { SmokeFail("O encerramento não confirmou o retorno das vozes e dos sinos"); yield break; }
        if (overlaySelection != 0) { SmokeFail("A tela final não selecionou a primeira opção ao abrir"); yield break; }
        string[] victoryColumns = FormatSessionCsvRow(IsRoomMarkedComplete(clearedRooms, roomIndex)).TrimEnd('\n').Split(',');
        string[] sessionColumns = SessionCsvHeader.TrimEnd('\n').Split(',');
        int completedColumn = System.Array.IndexOf(sessionColumns, "completed");
        int enemiesColumn = System.Array.IndexOf(sessionColumns, "enemies_left");
        if (completedColumn < 0 || enemiesColumn < 0 || completedColumn >= victoryColumns.Length || enemiesColumn >= victoryColumns.Length ||
            victoryColumns[completedColumn] != "true" || victoryColumns[enemiesColumn] != "0")
        { SmokeFail("A sessão da vitória não registrou a última sala concluída e o Arauto derrotado"); yield break; }
        ReturnToTitle();
        if (!showTitle || victory || showOpeningStory)
        { SmokeFail("Voltar ao título manteve o estado de vitória ou abriu uma cena de história fora de ordem"); yield break; }
        BeginJourney();
        if (showTitle || !showOpeningStory || victory)
        { SmokeFail("Uma nova jornada iniciada após a vitória ficou bloqueada pelo estado de encerramento"); yield break; }
        Debug.Log("SCADRYK_SMOKE: SUCCESS nove salas, três poderes, Arauto e vitória");
        Application.Quit(0);
    }

    private static void ClearSmokeAttackWarnings()
    {
        foreach (ScadrykTimedVisual visual in FindObjectsByType<ScadrykTimedVisual>())
            if (visual != null && visual.gameObject.name == "Enemy attack warning")
            { visual.gameObject.SetActive(false); Destroy(visual.gameObject); }
    }

    private static int SmokeEffectCount(string objectName)
    {
        int count = 0;
        foreach (ScadrykTimedVisual visual in FindObjectsByType<ScadrykTimedVisual>())
            if (visual != null && visual.gameObject.name == objectName) count++;
        return count;
    }

    private bool SmokeCoverBlocksOnlyWhenSolid()
    {
        var cover = new GameObject("Smoke-only cover probe");
        try
        {
            Vector2 origin = roomCenter + Vector2.up * 25f + Vector2.left * 2f;
            Vector2 target = roomCenter + Vector2.up * 25f + Vector2.right * 2f;
            cover.transform.position = roomCenter + Vector2.up * 25f;
            BoxCollider2D collider = cover.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.35f, 1f);
            Physics2D.SyncTransforms();
            bool blocksWhenSolid = ScadrykCombatRules.WorldBlocks(origin, target);
            bool coverStopsDirectionalPowers = ScadrykPlayerController.AbilityPathIsBlocked(0, origin, target) &&
                !ScadrykPlayerController.AbilityPathIsBlocked(1, origin, target) &&
                ScadrykPlayerController.AbilityPathIsBlocked(2, origin, target);
            collider.isTrigger = true;
            Physics2D.SyncTransforms();
            bool ignoresTrigger = !ScadrykCombatRules.WorldBlocks(origin, target);
            bool powersIgnoreTrigger = !ScadrykPlayerController.AbilityPathIsBlocked(0, origin, target) &&
                !ScadrykPlayerController.AbilityPathIsBlocked(1, origin, target) &&
                !ScadrykPlayerController.AbilityPathIsBlocked(2, origin, target);
            collider.enabled = false;
            Physics2D.SyncTransforms();
            bool openLineRemainsClear = !ScadrykCombatRules.WorldBlocks(origin, target);
            return blocksWhenSolid && coverStopsDirectionalPowers && ignoresTrigger && powersIgnoreTrigger && openLineRemainsClear;
        }
        finally
        {
            Destroy(cover);
            Physics2D.SyncTransforms();
        }
    }

    private IEnumerator SmokeProjectileCover()
    {
        smokeProjectileCoverPassed = false;
        string projectileName = "Smoke projectile cover probe";
        Vector2 origin = roomCenter + Vector2.up * 25f + Vector2.left * 2f;
        Vector2 wallPosition = roomCenter + Vector2.up * 25f;
        var cover = new GameObject("Smoke projectile solid cover");
        try
        {
            cover.transform.position = wallPosition;
            cover.AddComponent<BoxCollider2D>().size = new Vector2(0.35f, 1f);
            Physics2D.SyncTransforms();
            ScadrykEnemyProjectile.Launch(origin, Vector2.right, 25f, 0f, size: 0.34f, projectileName: projectileName, travelDistance: 10f);
            for (int step = 0; step < 12 && FindSmokeProjectile(projectileName) != null; step++) yield return new WaitForFixedUpdate();
            bool solidStopsProjectile = FindSmokeProjectile(projectileName) == null;

            cover.GetComponent<BoxCollider2D>().isTrigger = true;
            Physics2D.SyncTransforms();
            ScadrykEnemyProjectile.Launch(origin, Vector2.right, 25f, 0f, size: 0.34f, projectileName: projectileName, travelDistance: 10f);
            for (int step = 0; step < 4; step++) yield return new WaitForFixedUpdate();
            bool triggerAllowsProjectile = FindSmokeProjectile(projectileName) != null;
            ScadrykEnemyProjectile cleanupProbe = FindSmokeProjectile(projectileName);
            ClearRoomProjectiles();
            bool deactivatedImmediately = cleanupProbe != null && !cleanupProbe.gameObject.activeSelf;
            yield return null;
            bool destroyedByCleanup = FindSmokeProjectile(projectileName) == null;
            smokeProjectileCoverPassed = solidStopsProjectile && triggerAllowsProjectile && deactivatedImmediately && destroyedByCleanup;
        }
        finally
        {
            ScadrykEnemyProjectile projectile = FindSmokeProjectile(projectileName);
            if (projectile != null) Destroy(projectile.gameObject);
            Destroy(cover);
            Physics2D.SyncTransforms();
        }
    }

    private static ScadrykEnemyProjectile FindSmokeProjectile(string projectileName)
    {
        foreach (ScadrykEnemyProjectile projectile in FindObjectsByType<ScadrykEnemyProjectile>())
            if (projectile != null && projectile.gameObject.name == projectileName) return projectile;
        return null;
    }

    private bool SmokeRevealWorldSecret(int requiredPower)
    {
        if (worldSecret == null || player == null || !player.HasAbility(requiredPower)) return false;
        string[] powerNames = { "Crescente Verdejante", "Estouro de Sal", "Rompante da Maré" };
        if (requiredPower < 0 || requiredPower >= powerNames.Length || !WorldSecretHint(requiredPower).Contains(powerNames[requiredPower])) return false;
        Vector2 originalPosition = player.transform.position;
        bool originalHintState = worldSecretHintShown;
        player.transform.position = worldSecret.transform.position + Vector3.down * 2f;
        worldSecretHintShown = false;
        bool hintShown = TryShowWorldSecretHint() && statusMessage.Contains(powerNames[requiredPower]) &&
            statusMessage.Contains("(" + InputBinding(requiredPower + 3) + ")");
        player.transform.position = originalPosition;
        worldSecretHintShown = originalHintState;
        if (!hintShown) return false;
        var playerHealthField = typeof(ScadrykPlayerController).GetField("health", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var playerFacingField = typeof(ScadrykPlayerController).GetField("facing", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (playerHealthField == null || playerFacingField == null) return false;
        float originalHealth = player.Health;
        Vector2 originalFacing = player.Facing;
        Vector2 sealPosition = worldSecret.transform.position;
        Vector2 direction = Vector2.up;
        float validDistance = requiredPower == 0 ? 2.5f : requiredPower == 1 ? 4f : 6f;
        float invalidDistance = requiredPower == 0 ? 3.12f : requiredPower == 1 ? 4.26f : 6.41f;
        Vector2 validSource = sealPosition - direction * validDistance;
        Vector2 invalidSource = sealPosition - direction * invalidDistance;
        int echoCountBeforeReveal = SmokeEffectCount("Liberated echo memory");
        playerHealthField.SetValue(player, player.MaxHealth - 35f);
        playerFacingField.SetValue(player, direction);
        TryRevealSecret((requiredPower + 1) % 3, validSource);
        TryRevealSecret(requiredPower, invalidSource);
        bool rejectedInvalidUses = worldSecret != null && Mathf.Abs(player.Health - (player.MaxHealth - 35f)) <= 0.01f;
        if (rejectedInvalidUses) TryRevealSecret(requiredPower, validSource);
        bool grantedReward = worldSecret == null && Mathf.Abs(player.Health - player.MaxHealth) <= 0.01f &&
            player.HasPowerEcho(requiredPower) && statusMessage.Contains(PowerEchoRewardText(requiredPower));
        bool echoVisualShown = SmokeEffectCount("Liberated echo memory") > echoCountBeforeReveal;
        bool echoStoryShown = memoryText == WorldSecretMemory(requiredPower);
        playerHealthField.SetValue(player, originalHealth);
        playerFacingField.SetValue(player, originalFacing);
        if (!rejectedInvalidUses || !grantedReward || !echoVisualShown || !echoStoryShown) return false;
        Debug.Log("SCADRYK_SMOKE: selo exige o poder certo, restaura vida e concede melhoria tática salva (poder " + (requiredPower + 1) + ")");
        return true;
    }

    private static bool SmokeAttackWarningsMatch(Vector2 origin, Vector2 forward, float length, float width, float angleStep, int halfCount)
    {
        int count = 0;
        bool[] seenAngles = new bool[halfCount * 2 + 1];
        foreach (ScadrykTimedVisual visual in FindObjectsByType<ScadrykTimedVisual>())
        {
            if (visual == null || !visual.gameObject.activeInHierarchy || visual.gameObject.name != "Enemy attack warning") continue;
            if (count > halfCount * 2) return false;
            Vector2 actualDirection = visual.transform.right;
            float angleDegrees = angleStep * Mathf.Rad2Deg;
            int offset = angleStep == 0f ? 0 : Mathf.RoundToInt(Vector2.SignedAngle(forward, actualDirection) / angleDegrees);
            if (offset < -halfCount || offset > halfCount || seenAngles[offset + halfCount]) return false;
            seenAngles[offset + halfCount] = true;
            Vector2 expectedDirection = ((Vector2)(Quaternion.Euler(0f, 0f, angleDegrees * offset) * (Vector3)forward)).normalized;
            Vector2 expectedCenter = origin + expectedDirection * (length * 0.5f);
            if (Mathf.Abs(visual.transform.localScale.x - length) > 0.01f ||
                Mathf.Abs(visual.transform.localScale.y - ScadrykEffects.DangerWarningWidth(width)) > 0.001f ||
                Vector2.Angle(expectedDirection, actualDirection) > 0.5f ||
                Vector2.Distance((Vector2)visual.transform.position, expectedCenter) > 0.02f) return false;
            count++;
        }
        return count == halfCount * 2 + 1;
    }

    private static int SmokeProjectileCount(string projectileName)
    {
        int count = 0;
        foreach (ScadrykEnemyProjectile projectile in FindObjectsByType<ScadrykEnemyProjectile>())
            if (projectile != null && projectile.gameObject.activeInHierarchy && projectile.gameObject.name == projectileName) count++;
        return count;
    }

    private static bool SmokeProjectileRangeMatches(string projectileName, float expectedRange)
    {
        int count = 0;
        foreach (ScadrykEnemyProjectile projectile in FindObjectsByType<ScadrykEnemyProjectile>())
        {
            if (projectile == null || !projectile.gameObject.activeInHierarchy || projectile.gameObject.name != projectileName) continue;
            count++;
            if (Mathf.Abs(projectile.TravelDistanceLimit - expectedRange) > 0.01f) return false;
        }
        return count > 0;
    }

    private static void ClearSmokeProjectiles()
    {
        foreach (ScadrykEnemyProjectile projectile in FindObjectsByType<ScadrykEnemyProjectile>())
            if (projectile != null) { projectile.gameObject.SetActive(false); Destroy(projectile.gameObject); }
    }

    private static void SmokeFail(string message)
    {
        Debug.LogError("SCADRYK_SMOKE: FAIL " + message);
        Application.Quit(1);
    }
#endif

}
