using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>Builds the playable three-phase, nine-room Scadryk Unity vertical slice.</summary>
public sealed partial class ScadrykGameDirector : MonoBehaviour
{
    private const int RoomsPerPhase = 3;
    internal const float FinalSealPreparationWindowSeconds = 12f;
    internal const float NimaDepartureDuration = 0.64f;
    private const float RoomSpacing = 23f;
    private const float RoomHalfHeight = 8.5f;
    private const float RoomHeight = RoomHalfHeight * 2f;
    private const float ConnectorCenterOffset = 11.35f;
    private static readonly GUIContent OpeningTitleContent = new GUIContent("OS TRÊS SINOS EMUDECERAM");
    private static readonly GUIContent OpeningMemoryContent = new GUIContent(OpeningMemory);
    private static readonly GUIContent WorldMapTitleContent = new GUIContent("MAPA DA JORNADA");
    private const string SessionCsvHeader = "session_id,utc,phase,room,seconds,deaths,unclear_attacks,crescent,salt,tide,enemies_left,completed,current_entries,current_seconds,input_mode,seals_revealed,shallow_water_seconds,resonance_activations,salt_crosswind_seconds\n";
    private const string AttackClarityCsvHeader = "session_id,utc,phase,room,attack_patterns\n";
    private static readonly char[] CsvSpecialCharacters = { ',', '"', '\r', '\n' };
    private const string StoryJournalKeySuffix = "StoryJournal.v1";
    [System.Serializable]
    private sealed class StoryJournalData { public List<string> entries = new List<string>(); }
    private const string CheckpointKeySuffix = "Checkpoint.v1";
    [System.Serializable]
    private sealed class CheckpointData { public int version; public int phase; public int room; }
    private static readonly Color[] PhaseColors = { new Color(0.11f, 0.22f, 0.20f), new Color(0.27f, 0.20f, 0.12f), new Color(0.09f, 0.17f, 0.25f) };
    private static readonly Color[] PhaseWallColors = { new Color(0.17f, 0.29f, 0.26f), new Color(0.35f, 0.27f, 0.16f), new Color(0.13f, 0.28f, 0.36f) };
    private static readonly Color[] PhaseAccentColors = { new Color(0.36f, 0.62f, 0.48f), new Color(0.78f, 0.59f, 0.30f), new Color(0.30f, 0.68f, 0.76f) };
    private static Sprite SquareSprite;
    private static Sprite CircleSprite;
    private static readonly Dictionary<string, Sprite[][]> SpriteSheetRows = new Dictionary<string, Sprite[][]>();
    private static readonly Dictionary<string, Sprite[]> WholeSpriteSheets = new Dictionary<string, Sprite[]>();
    private Sprite[] environmentSprites;
    private Sprite[] phaseGroundSprites;
    private Sprite[] phaseApproachGroundSprites;
    private Sprite[] phaseCenterGroundSprites;
    private Sprite[] phaseGuardianGroundSprites;
    private Sprite[] phaseEnemySprites;
    private Sprite fallenWillowLogSprite;
    private Sprite portalSprite;
    private Sprite witnessSprite;
    private Sprite nimaMemoryPortraitSprite;
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
    private GameObject bossStagePulse;
    private int witnessLine;
#if UNITY_EDITOR || SCADRYK_SMOKE
    private bool nimaFarewellCompleted;
#endif
    private bool resumeFinalGuardianInterlude;
    private Coroutine finalBossSpawnRoutine;
    private bool smokeRestoreFinalGuardianForRestart;
    private ScadrykEnemyHealth finalBoss;
    private ScadrykEnemyHealth phaseGuardian;
    private GameObject worldSecret;
    private bool worldSecretHintShown;
    private bool worldSideRouteHintShown;
    private readonly GameObject[] roomAnchors = new GameObject[2];
    private int anchorsRequired, anchorsActivated, hintStage;
    private bool anchorsNeedPulse;
    private bool anchorsNeedSequence;
    private readonly HashSet<string> terrainHintsShown = new HashSet<string>();
    private bool smokeProjectileCoverPassed;
    private readonly bool[] clearedRooms = new bool[RoomsPerPhase];
    private readonly List<ScadrykEnemyHealth> activeEnemies = new List<ScadrykEnemyHealth>();
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
    private float nextEnemyAttackSlotAt;
    private int roomDeaths;
    private int unclearAttackMarks;
    private string sessionInputMode = "unknown";
    private bool roomSessionRecorded;
    private readonly int[] roomPowerUses = new int[3];
    private int coastalCurrentEntries;
    private int roomSecretsRevealed;
    private int victoryOptionalSealCount;
    private float coastalCurrentSeconds;
    private float shallowWaterSeconds;
    private float saltCrosswindSeconds;
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
    internal GameObject BossStagePulse => bossStagePulse;

    private void Start()
    {
#if DEBUG || UNITY_EDITOR || SCADRYK_SMOKE
        smokeRun = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-scadryk-smoke") >= 0;
#endif
        ScadrykSettings.ClearReservedBindings();
        ScadrykSettings.ApplyFullscreenPreference();
        ScadrykSettings.ApplyFrameRatePreference();
        sessionId = System.Guid.NewGuid().ToString("N");
        Debug.Log("SCADRYK_SESSION: " + sessionId);
        EnsureSprites();
        environmentSprites = LoadSheetRow("ScadrykEnvironment", 0, true);
        phaseGroundSprites = new[] { LoadSingleSprite("ForestClearing", 80f), LoadSingleSprite("SaltBasin", 80f), LoadSingleSprite("BellCoast", 80f) };
        phaseApproachGroundSprites = new[] {
            LoadSingleSprite("ForestCausewayApproachGround", 80f),
            LoadSingleSprite("SaltFlatsApproachGround", 80f),
            LoadSingleSprite("BellTidewayApproachGround", 80f)
        };
        phaseCenterGroundSprites = new[] {
            LoadSingleSprite("ForestAshFordGround", 80f),
            LoadSingleSprite("BasinCrosswindGround", 80f),
            LoadSingleSprite("BellwakeShelfGround", 80f)
        };
        phaseGuardianGroundSprites = new[] {
            LoadSingleSprite("ForestGuardianArenaGround", 80f),
            LoadSingleSprite("SaltGuardianArenaGround", 80f),
            LoadSingleSprite("BellGuardianArenaGround", 80f)
        };
        phaseEnemySprites = new[] { LoadSingleSprite("Rootbound", 1200f), LoadSingleSprite("SaltCrawler", 1200f), LoadSingleSprite("BellWraith", 1200f) };
        fallenWillowLogSprite = LoadSingleSprite("FallenWillowLog", 300f);
        portalSprite = LoadSingleSprite("AncientPortal", 600f);
        witnessSprite = LoadSingleSprite("DrownedMemory", 280f);
        nimaMemoryPortraitSprite = LoadSingleSprite("NimaMemoryPortrait", 420f);
        titleBackdropTexture = Resources.Load<Texture2D>("ScadrykArt/ScadrykTitleBackdrop");
        SetupCamera();
        gameObject.AddComponent<ScadrykAudio>();
        if (!smokeRun) MigrateLegacyWorldSecretProgress("Scadryk.");
        int savedPhase = 0, savedRoom = 0;
        if (!smokeRun) TryLoadCheckpoint("Scadryk.", out savedPhase, out savedRoom);
        BuildPhase(savedPhase, savedRoom, true);
        for (int i = 0; i < PhaseNames.Length; i++)
            if (ShouldRestoreAbility(i, savedPhase, !smokeRun && HasGuardianDefeatProgress("Scadryk.", i))) player.UnlockAbility(i);
        if (!smokeRun) RestorePowerEchoes("Scadryk.", player);
#if DEBUG || UNITY_EDITOR || SCADRYK_SMOKE
        if (smokeRun) { showTitle = false; StartCoroutine(SmokeCampaign()); }
#endif
        ApplyPauseTimeScale();
    }

    private void Update()
    {
        ScadrykSettings.TickDeferredSave();
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
        if (!IsPaused)
        {
            TryShowSideRouteHint();
            TryShowWorldSecretHint();
        }
        if (!IsPaused && (ScadrykSettings.Down(ScadrykSettings.UnclearAttackAction) ||
            ScadrykSettings.PadDown(ScadrykSettings.UnclearAttackAction)))
        {
            RecordUnclearAttackMark();
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
        if (!hasFocus) ScadrykSettings.FlushPendingSave();
        if (ShouldSaveCheckpointOnFocusLoss(hasFocus, player != null, victory, showTitle, isPaused, showOptions,
            showOpeningStory, transitioning, smokeRun))
            SaveProgress(phaseIndex, roomIndex);
        if (smokeRun || showOpeningStory || showWorldMap || memoryModal || !FocusLossRequiresPause(hasFocus, showTitle, isPaused, showOptions, victory, transitioning)) return;
        isPaused = true;
        ApplyPauseTimeScale();
    }

    internal static bool FocusLossRequiresPause(bool hasFocus, bool title, bool paused, bool options, bool won, bool portalTransition) =>
        !hasFocus && !title && !paused && !options && !won && !portalTransition;

    internal static bool ShouldSaveCheckpointOnFocusLoss(bool hasFocus, bool hasPlayer, bool hasVictory,
        bool titleVisible, bool paused, bool optionsVisible, bool openingStoryVisible, bool transitioning, bool smokeMode) =>
        FocusLossRequiresPause(hasFocus, titleVisible, paused, optionsVisible, hasVictory, transitioning) &&
        ShouldSaveCheckpointOnQuit(hasPlayer, hasVictory, titleVisible, openingStoryVisible, smokeMode);

    internal static bool RebindingCanBeCancelled(int keyboardAction, int gamepadAction) =>
        keyboardAction >= 0 || gamepadAction >= 0;
    internal static bool CanCaptureRebindKey(int frame, int captureStartedFrame) => frame > captureStartedFrame;

    internal static bool MemoryDismissRequested(bool confirm, bool interact, bool cancel) => confirm || interact || cancel;
    internal static bool MemoryDismissedThisFrame(int dismissedFrame, int currentFrame) => dismissedFrame >= 0 && dismissedFrame == currentFrame;
    private static bool GamepadActivityThisFrame() => ScadrykSettings.PadDown(0) || ScadrykSettings.PadDown(1) || ScadrykSettings.PadButtonDown(7) ||
        ScadrykSettings.PadDown(2) || ScadrykSettings.PadDown(3) || ScadrykSettings.PadDown(4) || ScadrykSettings.PadDown(5) || ScadrykSettings.PadDown(6) ||
        ScadrykSettings.PadDown(ScadrykSettings.UnclearAttackAction) ||
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
        int safeSelection = Mathf.Clamp(selection, 0, 6);
        int row = safeSelection < 6 ? safeSelection / 2 : 3;
        int column = safeSelection < 6 ? safeSelection % 2 : 0;
        if (verticalDirection != 0) row = Mathf.Clamp(row + System.Math.Sign(verticalDirection), 0, 3);
        if (horizontalDirection != 0 && row < 3) column = horizontalDirection > 0 ? 1 : 0;
        if (row == 3) return 6;
        return row * 2 + column;
    }

    internal static int MoveVictoryMenuSelection(int selection, int verticalDirection) =>
        Mathf.Clamp(selection + System.Math.Sign(verticalDirection), 0, 2);

    internal static int TitleMenuMaximumSelection(bool hasCheckpoint) => hasCheckpoint ? 4 : 3;
    internal static int TitleMenuOptionsSelection(bool hasCheckpoint) => hasCheckpoint ? 2 : 1;
    internal static int TitleMenuJournalSelection(bool hasCheckpoint) => hasCheckpoint ? 3 : 2;
    internal static int TitleMenuQuitSelection(bool hasCheckpoint) => hasCheckpoint ? 4 : 3;

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
                else if (optionsSelection == 10) ScadrykSettings.FrameRateMode = ScadrykSettings.NextFrameRateMode(ScadrykSettings.FrameRateMode, right ? 1 : -1);
            }
            else if (optionsTab == 2 && optionsSelection == 12 && (left || right))
                ScadrykSettings.GamepadDeadzone += right ? 0.02f : -0.02f;
            if (up || down)
            {
                int max = optionsTab == 0 ? 11 : optionsTab == 1 ? 16 : 13;
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
                int max = optionsTab == 0 ? 11 : optionsTab == 1 ? 16 : 13;
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
            else if (optionsTab == 0 && optionsSelection == 10)
                ScadrykSettings.FrameRateMode = ScadrykSettings.NextFrameRateMode(ScadrykSettings.FrameRateMode, navigation.x > 0f ? 1 : -1);
            else if (optionsTab == 2 && optionsSelection == 12) ScadrykSettings.GamepadDeadzone += navigation.x > 0f ? 0.02f : -0.02f;
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
            else if (overlaySelection == TitleMenuQuitSelection(hasCheckpoint)) Application.Quit();
        }
        else if (overlaySelection == 0) isPaused = false;
        else if (overlaySelection == 1) { showWorldMap = true; isPaused = false; }
        else if (overlaySelection == 2) RequestNewGame();
        else if (overlaySelection == 3) { showOptions = true; optionsTab = 0; optionsSelection = 0; }
        else if (overlaySelection == 4) OpenMemoryJournal();
        else if (overlaySelection == 5) ReturnToTitle();
        else if (overlaySelection == 6) Application.Quit();
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
            else if (optionsSelection == 9) ScadrykSettings.Fullscreen = !ScadrykSettings.Fullscreen;
            else if (optionsSelection == 10) ScadrykSettings.FrameRateMode = ScadrykSettings.NextFrameRateMode(ScadrykSettings.FrameRateMode, 1);
            else if (optionsSelection == 11) showOptions = false;
        }
        else if (optionsTab == 1)
        {
            if (optionsSelection is >= 3 and <= 14)
            {
                rebindingAction = optionsSelection - 3;
                keyboardRebindAfterFrame = Time.frameCount;
            }
            else if (optionsSelection == 15)
            {
                ScadrykSettings.ResetKeyboardBindings();
                rebindingAction = -1;
                ShowStatus("Teclas padrão restauradas.", 2f);
            }
            else if (optionsSelection == 16) showOptions = false;
        }
        else if (optionsTab == 2)
        {
            if (optionsSelection is >= 3 and <= 10)
            {
                gamepadRebindAction = ScadrykSettings.PadOptionAction(optionsSelection - 3);
                gamepadRebindAfterFrame = Time.frameCount;
            }
            else if (optionsSelection == 11)
            {
                ScadrykSettings.ResetGamepadBindings();
                gamepadRebindAction = -1;
                ShowStatus("Controle padrão restaurado.", 2f);
            }
            else if (optionsSelection == 13) showOptions = false;
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
        ClearCheckpoint("Scadryk.");
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
        RecordCurrentRoomStory();
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
        if (!smokeRun)
        {
            RecordStoryJournalEntry("Scadryk.", OpeningMemory);
            RecordCurrentRoomStory();
        }
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

    internal static float ProgressHintDelay(int stage) => stage <= 0 ? 16f : 11f;

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
        nextEnemyAttackSlotAt = 0f;
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
        RefreshMapRoomContents();
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
        RecordCurrentRoomStory();
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
        saltCrosswindSeconds = 0f;
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

    internal float ScheduleEnemyAttackStart(float requestedAt) =>
        ScadrykEnemyHealth.ReserveEnemyAttackSlot(ref nextEnemyAttackSlotAt, requestedAt);

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
        int revealedSecrets = smokeRun ? 0 : CountWorldSecretsRevealed("Scadryk.");
        transitioning = true;
        ScadrykPortalVisual exitPortal = gateObject != null ? gateObject.GetComponent<ScadrykPortalVisual>() : null;
        Vector2 start = player.transform.position;
        Vector2 target = exitPosition;
        Vector2 nimaStart = target + new Vector2(-1.55f, 1.05f);
        var nimaObject = new GameObject("Nima crosses the restored threshold");
        if (levelRoot != null) nimaObject.transform.SetParent(levelRoot.transform, true);
        nimaObject.transform.position = nimaStart;
        nimaObject.transform.localScale = Vector3.one * WitnessScaleForPhase(2);
        var nimaRenderer = nimaObject.AddComponent<SpriteRenderer>();
        nimaRenderer.sprite = witnessSprite != null ? witnessSprite : CircleSprite;
        nimaRenderer.color = WitnessTintForPhase(2);
        nimaRenderer.sortingOrder = 16;
        if (!smokeRun) ScadrykAudio.Play(ScadrykCue.NimaFarewell);
        float elapsed = 0f;
        const float duration = 0.82f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t * (3f - 2f * t);
            player.SetPortalTransitPose(Vector2.Lerp(start, target, eased), 1f - eased * 0.97f);
            float nimaProgress = NimaDepartureProgress(elapsed);
            nimaObject.transform.position = NimaDeparturePosition(nimaStart, target, nimaProgress);
            nimaObject.transform.localScale = Vector3.one * NimaDepartureScale(nimaProgress);
            Color nimaTint = WitnessTintForPhase(2);
            nimaTint.a *= NimaDepartureAlpha(nimaProgress);
            nimaRenderer.color = nimaTint;
            if (elapsed >= duration * 0.4f && exitPortal != null) exitPortal.BeginClose();
            yield return null;
        }
        Destroy(nimaObject);
#if UNITY_EDITOR || SCADRYK_SMOKE
        nimaFarewellCompleted = true;
#endif
        yield return FadeScreen(1f, 0.45f);
        RecordRoomSession();
        transitioning = false;
        transitionFade = 0f;
        victory = true;
        victoryOptionalSealCount = revealedSecrets;
        overlaySelection = 0;
        ScadrykAudio.Play(ScadrykCue.Victory);
        if (!smokeRun) ClearProgressAfterVictory("Scadryk.");
        statusMessage = VictoryEndingTextForSecrets(victoryOptionalSealCount);
    }

    internal static float NimaDepartureProgress(float elapsed) =>
        Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / NimaDepartureDuration));

    internal static Vector2 NimaDeparturePosition(Vector2 start, Vector2 target, float progress) =>
        Vector2.Lerp(start, target, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress)));

    internal static float NimaDepartureScale(float progress) =>
        WitnessScaleForPhase(2) * Mathf.Lerp(1f, 0.12f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress)));

    internal static float NimaDepartureAlpha(float progress) =>
        1f - Mathf.SmoothStep(0.38f, 1f, Mathf.Clamp01(progress));

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
        ShowStatus("Você caiu. Vida restaurada; poderes e memórias preservados.", 2.5f);
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
        string message = stage == 2 ? "Transição: o Arauto convoca o sal. Linhas âmbar anunciam cada trajetória." :
            "Transição final: os três silêncios se juntam. O anel interno marca o abrigo da onda.";
        Vector2 center = finalBoss != null ? (Vector2)finalBoss.transform.position : roomCenter;
        ScadrykEffects.Burst(center, new Color(0.95f, 0.48f, 0.75f), 2.8f);
        bossStagePulse = ScadrykEffects.Ring(center, 5.2f, new Color(0.78f, 0.72f, 1f, 0.82f), ScadrykEnemyHealth.FinalStageBreatherDuration, false);
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

    private static bool HasAllSprites(Sprite[] sprites, int expectedCount)
    {
        if (sprites == null || sprites.Length != expectedCount) return false;
        for (int i = 0; i < sprites.Length; i++)
            if (sprites[i] == null) return false;
        return true;
    }

    private static bool AllArtTexturesAreGpuOnly(out int textureCount)
    {
        Texture2D[] textures = Resources.LoadAll<Texture2D>("ScadrykArt");
        textureCount = textures != null ? textures.Length : 0;
        if (textureCount == 0) return false;
        for (int i = 0; i < textureCount; i++)
            if (textures[i] == null || textures[i].isReadable) return false;
        return true;
    }

    private static Sprite LoadSingleSprite(string name, float pixelsPerUnit)
    {
        Texture2D texture = Resources.Load<Texture2D>("ScadrykArt/" + name);
        if (texture == null) return null;
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
    }



}
