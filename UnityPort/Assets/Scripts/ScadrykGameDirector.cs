using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>Builds the playable three-phase, nine-room Scadryk Unity vertical slice.</summary>
public sealed class ScadrykGameDirector : MonoBehaviour
{
    private const int RoomsPerPhase = 3;
    private const float RoomSpacing = 23f;
    private const string OpeningMemory = "Depois que a Grande Onda cobriu a costa, os três sinos emudeceram. Nara, a Ouvinte, ainda escutava os nomes perdidos e seguiu a maré imóvel.";
    private static readonly GUIContent OpeningTitleContent = new GUIContent("OS TRÊS SINOS EMUDECERAM");
    private static readonly GUIContent OpeningMemoryContent = new GUIContent(OpeningMemory);
    private static readonly GUIContent WorldMapTitleContent = new GUIContent("MAPA DA JORNADA");
    private const string EndingMemory = "O Arauto quis ocultar a catástrofe para poupar os vivos da dor, mas o silêncio aprisionou os mortos. Nara escolhe lembrar e abre o limiar para que todos possam atravessar.";
    private const string VictoryStatus = "As vozes atravessaram o limiar. Os sinos voltam a responder.";
    private const string SessionCsvHeader = "session_id,utc,phase,room,seconds,deaths,unclear_attacks,crescent,salt,tide,enemies_left,completed,input_mode\n";
    private static readonly string[] PhaseNames = { "Margens do Primeiro Eco", "Bacia do Sal Imóvel", "Costa das Badaladas" };
    private static readonly Color[] PhaseColors = { new Color(0.11f, 0.22f, 0.20f), new Color(0.27f, 0.20f, 0.12f), new Color(0.09f, 0.17f, 0.25f) };
    private static readonly Color[] PhaseWallColors = { new Color(0.17f, 0.29f, 0.26f), new Color(0.35f, 0.27f, 0.16f), new Color(0.13f, 0.28f, 0.36f) };
    private static readonly Color[] PhaseAccentColors = { new Color(0.36f, 0.62f, 0.48f), new Color(0.78f, 0.59f, 0.30f), new Color(0.30f, 0.68f, 0.76f) };
    private static Sprite SquareSprite;
    private static Sprite CircleSprite;
    private Sprite[] environmentSprites;
    private Sprite[] phaseGroundSprites;
    private Sprite[] phaseEnemySprites;
    private Sprite fallenWillowLogSprite;
    private Sprite portalSprite;
    private Sprite witnessSprite;
    private Texture2D titleBackdropTexture;

    private int phaseIndex, roomIndex, enemiesRemaining;
    private static bool smokeRun;
    private bool isPaused, victory, transitioning, showTitle = true, showOptions, confirmNewJourney, showOpeningStory, showWorldMap;
    private string sessionId;
    private int rebindingAction = -1;
    private int gamepadRebindAction = -1;
    private int optionsTab, optionsSelection, overlaySelection;
    private int gamepadRebindAfterFrame;
    private float nextGamepadNavigationAt;
    private float transitionFade;
    private Vector2 roomCenter, exitPosition;
    private ScadrykPlayerController player;
    private ScadrykCameraFollow follow;
    private GameObject levelRoot, gateObject, witnessObject;
    private int witnessLine;
    private ScadrykEnemyHealth finalBoss;
    private ScadrykEnemyHealth phaseGuardian;
    private GameObject worldSecret;
    private bool worldSecretHintShown;
    private readonly GameObject[] roomAnchors = new GameObject[2];
    private int anchorsRequired, anchorsActivated, hintStage;
    private bool anchorsNeedPulse;
    private bool anchorsNeedSequence;
    private bool shallowWaterHintShown;
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
        "A água conserva vozes que Nara pensava perdidas.", "A comporta ainda espera a mão de uma Ouvinte.", "O guardião prende nas raízes o primeiro sino.",
        "A bacia está imóvel, mas o sal ainda se recorda da maré.", "Dois cristais guardam a rota; o Pulso pode fazê-los cantar.", "O zelador do sal protege uma lembrança que não é sua.",
        "Na costa, as badaladas cessaram antes da última onda.", "Ressoar o farol primeiro; o sino do recife responderá depois.", "O Arauto reuniu os três silêncios para esconder a verdade."
    };
    private static readonly string[] GuardianNames = { "A Raiz-Mor", "A Vigia do Sal", "O Vigia do Coro" };
    private static readonly string[] GuardianIntroductions = {
        "A Raiz-Mor: sem mim, os nomes voltam à correnteza.",
        "A Vigia do Sal: a maré ainda não pode voltar.",
        "O Vigia do Coro: os sinos devolverão a dor."
    };
    private static readonly string[] GuardianMemories = {
        "A Raiz-Mor entrega o Crescente e revela que Nima tocou o primeiro sino; seu nome ficou guardado nas raízes.",
        "A Vigia do Sal devolve o Estouro e revela que o Arauto deteve a última onda, deixando a costa presa no instante em que perdeu suas vozes.",
        "O Vigia do Coro entrega o Rompante e revela que o Arauto mandou calar os sinos para poupar os vivos."
    };
    private static readonly string[] BasinWitnessLines = {
        "Sou Nima, a voz do primeiro sino. A Raiz-Mor guardou meu nome para que o silêncio não me apagasse. Ainda lembro o Arauto chamando esse silêncio de abrigo.",
        "O sal ainda segura a onda no instante em que ela partiu. Faça os cristais responderem; a água precisa voltar a seguir."
    };
    private static readonly string[] CoastWitnessLines = {
        "Você devolveu movimento ao sal. Agora os sinos lembram que cada voz tinha alguém esperando por ela.",
        "O Arauto chama o silêncio de abrigo. Pergunte a quem perdeu o próprio nome se isso parece paz."
    };
    private string statusMessage = "Derrote os ecos para abrir a passagem.";
    private float statusUntil;
    private string memoryText;
    private float memoryUntil;
    private float roomActiveSeconds;
    private float nextOpeningAttackSlotAt;
    private int roomDeaths;
    private int unclearAttackMarks;
    private string sessionInputMode = "unknown";
    private bool roomSessionRecorded;
    private readonly int[] roomPowerUses = new int[3];
    private GUIStyle titleStyle, bodyStyle, smallStyle, openingTitleStyle, openingBodyStyle, offscreenTargetStyle;
    private GUIStyle centeredTitleStyle, centeredBodyStyle, worldMapRoomStyle, worldMapStateStyle, worldMapObjectiveStyle, worldMapInstructionStyle, controlHintStyle;
    private GUIStyle finalBossLabelStyle, memoryStyle;
    private float cachedTextScale = -1f;

    public bool IsPaused => isPaused || showTitle || showOptions || victory || transitioning || showOpeningStory || showWorldMap;
    public int PhaseIndex => phaseIndex;
    public int RoomIndex => roomIndex;
    public int EnemiesRemaining => enemiesRemaining;
    public Vector2 CurrentRoomCenter => roomCenter;
    public Vector2 ExitPosition => exitPosition;
    public Transform ActiveRoomRoot => levelRoot != null ? levelRoot.transform : null;

    private void Start()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
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
        for (int i = 0; i < savedPhase; i++) player.UnlockAbility(i);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (smokeRun) { showTitle = false; StartCoroutine(SmokeCampaign()); }
#endif
        ApplyPauseTimeScale();
    }

    private void Update()
    {
        ApplyPauseTimeScale();
        if (!showTitle && !isPaused && !showOptions && !victory && !transitioning && !showOpeningStory && !showWorldMap)
            roomActiveSeconds += Time.deltaTime;
        if (transitioning) return;
        if (showOpeningStory)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || ScadrykSettings.Down(6) ||
                ScadrykSettings.PadButtonDown(0) || ScadrykSettings.PadDown(6)) DismissOpeningStory();
            return;
        }
        if (showWorldMap)
        {
            if (Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.Escape) ||
                ScadrykSettings.PadButtonDown(8) || ScadrykSettings.PadButtonDown(7))
            { showWorldMap = false; ApplyPauseTimeScale(); }
            return;
        }
        if (!IsPaused && (Input.GetKeyDown(KeyCode.M) || ScadrykSettings.PadButtonDown(8)))
        {
            showWorldMap = true;
            ApplyPauseTimeScale();
            return;
        }
        if (showTitle && !showOptions && Input.GetKeyDown(KeyCode.Return))
        {
            if (confirmNewJourney) SubmitGamepadOverlay();
            else BeginJourney();
        }
        if ((showTitle || isPaused) && !showOptions && Input.GetKeyDown(KeyCode.O))
        {
            showOptions = true; optionsTab = 0; optionsSelection = 0; gamepadRebindAction = rebindingAction = -1;
        }
        if (!victory && (Input.GetKeyDown(KeyCode.Escape) || ScadrykSettings.PadButtonDown(7)))
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
        if (showOptions || showTitle || isPaused || victory) UpdateGamepadMenus();
        if (!IsPaused && gateObject == null && enemiesRemaining == 0 && anchorsActivated >= anchorsRequired)
            UnlockExit();
        if (!IsPaused && anchorsActivated < anchorsRequired && roomActiveSeconds > 24f + hintStage * 18f)
        {
            ShowStatus(ProgressHint(phaseIndex * RoomsPerPhase + roomIndex, anchorsActivated, hintStage), 7f);
            hintStage++;
        }
        if (!IsPaused) TryShowWorldSecretHint();
        if (!IsPaused && Input.GetKeyDown(KeyCode.F8))
        {
            unclearAttackMarks++;
            ShowStatus("Ataque difícil de perceber registrado nesta sala.", 3f);
        }
        ApplyPauseTimeScale();
    }

    private void ApplyPauseTimeScale()
    {
        Time.timeScale = showTitle || isPaused || showOptions || victory || showOpeningStory || showWorldMap ? 0f : 1f;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (smokeRun || showOpeningStory || showWorldMap || !FocusLossRequiresPause(hasFocus, showTitle, isPaused, showOptions, victory, transitioning)) return;
        isPaused = true;
        ApplyPauseTimeScale();
    }

    internal static bool FocusLossRequiresPause(bool hasFocus, bool title, bool paused, bool options, bool won, bool portalTransition) =>
        !hasFocus && !title && !paused && !options && !won && !portalTransition;

    internal static bool RebindingCanBeCancelled(int keyboardAction, int gamepadAction) =>
        keyboardAction >= 0 || gamepadAction >= 0;

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

        Vector2 navigation = new Vector2(Input.GetAxisRaw("Scadryk Move X"), Input.GetAxisRaw("Scadryk Move Y"));
        if (Time.unscaledTime >= nextGamepadNavigationAt && Mathf.Abs(navigation.y) > 0.55f)
        {
            if (showOptions)
            {
                int max = optionsTab == 0 ? 9 : optionsTab == 1 ? 15 : 11;
                optionsSelection = Mathf.Clamp(optionsSelection - (navigation.y > 0f ? 1 : -1), 0, max);
            }
            else overlaySelection = Mathf.Clamp(overlaySelection - (navigation.y > 0f ? 1 : -1), victory ? 1 : 0,
                confirmNewJourney || victory ? 1 : showTitle ? 2 : 3);
            nextGamepadNavigationAt = Time.unscaledTime + 0.2f;
        }
        else if (Mathf.Abs(navigation.y) < 0.2f) nextGamepadNavigationAt = 0f;

        if (showOptions && optionsSelection < 3 && Time.unscaledTime >= nextGamepadNavigationAt && Mathf.Abs(navigation.x) > 0.55f)
        {
            optionsTab = (optionsTab + (navigation.x > 0f ? 1 : 2)) % 3;
            optionsSelection = optionsTab;
            nextGamepadNavigationAt = Time.unscaledTime + 0.2f;
        }
        else if (showOptions && optionsTab == 0 && Time.unscaledTime >= nextGamepadNavigationAt && Mathf.Abs(navigation.x) > 0.55f)
        {
            float step = navigation.x > 0f ? 0.05f : -0.05f;
            if (optionsSelection == 3) ScadrykSettings.MusicVolume += step;
            else if (optionsSelection == 4) ScadrykSettings.EffectsVolume += step;
            else if (optionsSelection == 5) { ScadrykSettings.TextScale += step; titleStyle = null; }
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
        if (victory) { StartNewGame(); return; }
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
        else if (showTitle && overlaySelection == 1) RequestNewGame();
        else if (showTitle) { showOptions = true; optionsTab = 0; optionsSelection = 0; }
        else if (overlaySelection == 0) isPaused = false;
        else if (overlaySelection == 1) { showWorldMap = true; isPaused = false; }
        else if (overlaySelection == 2) RequestNewGame();
        else { showOptions = true; optionsTab = 0; optionsSelection = 0; }
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
            if (optionsSelection is >= 3 and <= 13) rebindingAction = optionsSelection - 3;
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
            else if (optionsSelection == 11) showOptions = false;
        }
    }

    private void StartNewGame()
    {
        confirmNewJourney = false;
        showOptions = false;
        isPaused = false;
        showWorldMap = false;
        showOpeningStory = true;
        ApplyPauseTimeScale();
        PlayerPrefs.DeleteKey("Scadryk.Phase");
        PlayerPrefs.DeleteKey("Scadryk.Room");
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
        StartRoomTimer();
        ApplyPauseTimeScale();
    }

    private void DismissOpeningStory()
    {
        showOpeningStory = false;
        memoryText = null;
        memoryUntil = 0f;
        StartRoomTimer();
        ApplyPauseTimeScale();
    }

    private void StartRoomTimer()
    {
        roomActiveSeconds = 0f;
        sessionInputMode = "unknown";
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
            ? "Pista: há uma lembrança marcada no lado sul da bacia. Aproxime-se e interaja (" + InputBinding(6) + ")."
            : "Pista: escute o vestígio no lado sul usando interagir (" + InputBinding(6) + ") quando estiver ao lado do brilho marcado.";
        if (roomId == 6) return stage == 0
            ? "Pista: o sino encalhado está marcado perto do centro da costa. Aproxime-se e interaja (" + InputBinding(6) + ")."
            : "Pista: a lembrança da costa responde a interagir (" + InputBinding(6) + ") quando Nara chega perto do sino marcado.";
        return "Pista: siga o objetivo marcado nesta sala e aproxime-se do sinal indicado.";
    }

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
        cam.orthographicSize = 7.3f;
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
        BuildWorldSecret();
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
        follow.SetTarget(player.transform);
        SpawnRoomEnemies();
        GetComponent<ScadrykAudio>().SetContext(phaseIndex, false);
        if (!preserveRoomSession)
        {
            roomActiveSeconds = 0f;
            roomDeaths = 0;
            unclearAttackMarks = 0;
            sessionInputMode = "unknown";
            roomSessionRecorded = false;
            System.Array.Clear(roomPowerUses, 0, roomPowerUses.Length);
        }
        hintStage = 0;
        statusMessage = roomIndex == RoomsPerPhase - 1 ? GuardianIntroductions[phaseIndex] : RoomStories[phaseIndex * RoomsPerPhase + roomIndex];
        statusUntil = Time.time + (roomIndex == RoomsPerPhase - 1 ? 8f : 6f);
    }

    private void BuildRoomAnchors()
    {
        roomAnchors[0] = roomAnchors[1] = null;
        anchorsActivated = 0;
        int id = phaseIndex * RoomsPerPhase + roomIndex;
        anchorsNeedSequence = id == 7;
        anchorsRequired = id == 1 || id == 3 || id == 6 ? 1 : id == 4 || id == 7 ? 2 : 0;
        anchorsNeedPulse = id == 4 || id == 7;
        if (anchorsRequired == 0) return;
        for (int i = 0; i < anchorsRequired; i++)
        {
            Vector2 offset = id == 1 ? new Vector2(5.1f, 2.8f) : id == 3 ? new Vector2(3.8f, -2.9f) :
                id == 6 ? new Vector2(3.8f, 1.3f) : anchorsNeedSequence ?
                    (i == 0 ? new Vector2(-3.2f, -1.45f) : new Vector2(5.4f, 2.15f)) :
                    i == 0 ? new Vector2(-2.2f, 1.25f) : new Vector2(4.2f, 2.65f);
            string anchorName = anchorsNeedSequence ? (i == 0 ? "Coast beacon resonator" : "Reef bell resonator") :
                anchorsNeedPulse ? "Resonant world object" : "Memory world object";
            var anchor = new GameObject(anchorName);
            anchor.transform.SetParent(levelRoot.transform);
            anchor.transform.position = roomCenter + offset;
            var renderer = anchor.AddComponent<SpriteRenderer>();
            renderer.sprite = environmentSprites != null && environmentSprites.Length > (phaseIndex == 2 ? 14 : 8) ?
                environmentSprites[phaseIndex == 0 ? 6 : phaseIndex == 1 ? 8 : 14] : CircleSprite;
            renderer.color = new Color(0.8f, 0.9f, 0.88f, 0.95f);
            renderer.sortingOrder = 3;
            anchor.transform.localScale = Vector3.one * 1.35f;
            roomAnchors[i] = anchor;
            MakeDecoration(levelRoot.transform, "Resonance mark", anchor.transform.position, new Vector2(1.5f, 1.5f), PhaseAccentColors[phaseIndex] * 0.48f, true, 2);
        }
    }

    private void BuildWitness()
    {
        witnessObject = null;
        witnessLine = 0;
        // Nima returns in the two quiet arrival rooms after the first and second guardians.
        if (roomIndex != 0 || phaseIndex == 0) return;

        Vector2 position = roomCenter + new Vector2(-5.4f, 1.1f);
        witnessObject = new GameObject("Nima — voz libertada das raízes");
        witnessObject.transform.SetParent(levelRoot.transform);
        witnessObject.transform.position = position;

        MakeDecoration(levelRoot.transform, "Nima's reflected pool", position + Vector2.down * 0.52f,
            new Vector2(0.9f, 0.28f), new Color(PhaseAccentColors[phaseIndex].r, PhaseAccentColors[phaseIndex].g,
                PhaseAccentColors[phaseIndex].b, 0.42f), true, 2);
        var visual = new GameObject("Nima — Echo witness");
        visual.transform.SetParent(witnessObject.transform, false);
        var renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = witnessSprite != null ? witnessSprite : CircleSprite;
        renderer.color = Color.white;
        renderer.sortingOrder = 5;
        visual.transform.localScale = Vector3.one * 0.36f;
        witnessObject.AddComponent<ScadrykDepthSort>().SetBias(4);
    }

    public void TryInteract(Vector2 position)
    {
        if (IsPaused) return;
        if (witnessObject != null && Vector2.Distance(position, witnessObject.transform.position) <= 1.9f)
        {
            string[] lines = phaseIndex == 1 ? BasinWitnessLines : CoastWitnessLines;
            ShowMemory(lines[witnessLine % lines.Length], 8f);
            witnessLine++;
            ScadrykAudio.Play(ScadrykCue.Memory);
            return;
        }
        for (int i = 0; i < anchorsRequired; i++)
        {
            if (roomAnchors[i] == null || Vector2.Distance(position, roomAnchors[i].transform.position) > 1.8f) continue;
            if (anchorsNeedPulse) { ShowStatus("Este objeto responde ao Pulso (" + InputBinding(2) + ").", 3f); return; }
            ActivateAnchor(i);
            return;
        }
        TryUseExit(position);
    }

    public void TryPulseAnchor(Vector2 position)
    {
        if (!anchorsNeedPulse || IsPaused) return;
        for (int i = 0; i < anchorsRequired; i++)
        {
            float pulseRadius = player != null ? player.ShockwaveRadius : 3.2f;
            if (roomAnchors[i] == null || Vector2.Distance(position, roomAnchors[i].transform.position) > pulseRadius) continue;
            if (anchorsNeedSequence && i != anchorsActivated)
            {
                ShowStatus(i > anchorsActivated ? "Primeiro ressoe o farol na entrada da costa." : "Esse sinal já respondeu. Siga até o sino do recife.", 3f);
                return;
            }
            ActivateAnchor(i);
            return;
        }
    }

    private void ActivateAnchor(int index)
    {
        GameObject anchor = roomAnchors[index];
        if (anchor == null) return;
        roomAnchors[index] = null;
        anchorsActivated++;
        ScadrykEffects.Burst(anchor.transform.position, PhaseAccentColors[phaseIndex], 1.8f);
        ScadrykAudio.Play(ScadrykCue.Portal);
        Destroy(anchor);
        if (anchorsActivated >= anchorsRequired)
        {
            ShowStatus("O vestígio respondeu. A passagem pode se abrir.", 5f);
            int id = phaseIndex * RoomsPerPhase + roomIndex;
            if (id == 3) ShowMemory("O sal guardava uma maré inteira, suspensa no instante em que o Arauto ordenou silêncio. Nara reconhece a voz de quem tentou avisar a costa.", 8f);
            if (id == 6) ShowMemory("Um sino encalhado repete o pedido dos navegantes: não apagar a lembrança da onda. Eles não pedem vingança; pedem que alguém a escute.", 8f);
            if (enemiesRemaining == 0) UnlockExit();
        }
        else ShowStatus("Um sinal respondeu. Falta mais um.", 4f);
    }

    private void DrawPhaseMap(Transform parent)
    {
        Color floor = PhaseColors[phaseIndex];
        Color wall = PhaseWallColors[phaseIndex];
        // The camera can see beyond the first and last traversable rooms. Painted
        // non-colliding outskirts keep that view within the biome instead of void.
        MakeBox(parent, "Biome backdrop", new Vector2(RoomSpacing, 0f), new Vector2(RoomSpacing * 6f, 40f), floor * 0.72f, false, -11);
        PlacePhaseGround(parent, -RoomSpacing, 1);
        PlacePhaseGround(parent, RoomsPerPhase * RoomSpacing, 1);
        for (int i = 0; i < RoomsPerPhase; i++)
        {
            float x = i * RoomSpacing;
            Color roomFloor = floor * (i == 1 ? 1.08f : 0.96f);
            MakeBox(parent, "Room floor " + (i + 1), new Vector2(x, 0), new Vector2(RoomSpacing + 0.2f, 13f), roomFloor, false, -10);
            PlacePhaseGround(parent, x, i);
            DrawRoomScenery(parent, i, x, roomFloor);
            MakeBox(parent, "North wall", new Vector2(x, 6.6f), new Vector2(19.5f, 0.5f), wall, true, 0);
            MakeBox(parent, "South wall", new Vector2(x, -6.6f), new Vector2(19.5f, 0.5f), wall, true, 0);
            if (i == 0) MakeBox(parent, "West outer wall", new Vector2(x - 9.7f, 0), new Vector2(0.5f, 13f), wall, true, 0);
            else
            {
                MakeBox(parent, "West wall upper", new Vector2(x - 9.7f, 4.7f), new Vector2(0.5f, 4.3f), wall, true, 0);
                MakeBox(parent, "West wall lower", new Vector2(x - 9.7f, -4.7f), new Vector2(0.5f, 4.3f), wall, true, 0);
            }
            if (i == RoomsPerPhase - 1)
            {
                MakeBox(parent, "East wall upper", new Vector2(x + 9.7f, 4.7f), new Vector2(0.5f, 4.3f), wall, true, 0);
                MakeBox(parent, "East wall lower", new Vector2(x + 9.7f, -4.7f), new Vector2(0.5f, 4.3f), wall, true, 0);
            }
            MakeBox(parent, "Floor inlay", new Vector2(x, 0), new Vector2(0.1f, 11.8f), new Color(1f, 1f, 1f, 0.055f), false, -9);
            if (i < RoomsPerPhase - 1)
            {
                MakeBox(parent, "Connector floor", new Vector2(x + 11.35f, 0), new Vector2(3.3f, 3.4f), floor * 1.12f, false, -9);
                MakeBox(parent, "Connector north rail", new Vector2(x + 11.35f, 1.92f), new Vector2(3.4f, 0.28f), wall, true, 0);
                MakeBox(parent, "Connector south rail", new Vector2(x + 11.35f, -1.92f), new Vector2(3.4f, 0.28f), wall, true, 0);
            }
            DrawRoomObstacles(parent, i, x);
            MakeLabelMarker(parent, new Vector2(x, 5.35f), (i + 1).ToString(), PhaseAccentColors[phaseIndex]);
        }
    }

    private void BuildWorldSecret()
    {
        worldSecret = null;
        worldSecretHintShown = false;
        int requiredPower = RequiredWorldSecretPower();
        if (requiredPower < 0) return;
        Vector2 spot = requiredPower == 0 ? roomCenter + new Vector2(6.8f, 4.15f) :
            requiredPower == 1 ? roomCenter + new Vector2(-7.2f, -4.1f) :
            roomCenter + new Vector2(7.2f, 4.15f);
        worldSecret = new GameObject("Hidden echo seal — power " + (requiredPower + 1));
        worldSecret.transform.SetParent(levelRoot.transform);
        worldSecret.transform.position = spot;
        var marker = worldSecret.AddComponent<SpriteRenderer>();
        marker.sprite = CircleSprite;
        marker.color = new Color(PhaseAccentColors[phaseIndex].r, PhaseAccentColors[phaseIndex].g, PhaseAccentColors[phaseIndex].b, 0.72f);
        marker.sortingOrder = 2;
        worldSecret.transform.localScale = Vector3.one * 0.75f;
        worldSecret.AddComponent<ScadrykAmbientProp>().Configure(ScadrykAmbientMotion.Glow, 1.2f);
        MakeDecoration(worldSecret.transform, "Seal center", spot, new Vector2(0.2f, 0.2f), Color.white, true, 3);
    }

    private int RequiredWorldSecretPower() => phaseIndex == 1 && roomIndex == 0 ? 0 :
        phaseIndex == 2 && roomIndex == 0 ? 1 : phaseIndex == 2 && roomIndex == 2 ? 2 : -1;

    private static string WorldSecretMemory(int power) => power switch
    {
        0 => "Uma voz recorda a comporta se abrindo pela primeira vez, quando as raízes ainda conheciam todos os nomes.",
        1 => "Uma lembrança solta devolve ao sal o instante da última maré antes de o tempo parar.",
        _ => "Um Eco reconhece sua própria voz no coro dos três sinos. A lembrança já não precisa ficar presa."
    };

    private string WorldSecretHint(int power)
    {
        string[] names = { "Crescente Verdejante", "Estouro de Sal", "Rompante da Maré" };
        if (player == null || !player.HasAbility(power))
            return "Selo de Eco: esta lembrança só poderá ser libertada após derrotar o guardião deste lugar.";
        string binding = InputBinding(power + 3);
        return "Selo de Eco: use " + names[power] + " (" + binding + ") perto dele para libertar uma memória.";
    }

    private bool TryShowWorldSecretHint()
    {
        if (worldSecretHintShown || worldSecret == null || player == null ||
            Vector2.Distance(player.transform.position, worldSecret.transform.position) > 3.2f) return false;
        int power = RequiredWorldSecretPower();
        if (power < 0) return false;
        ShowStatus(WorldSecretHint(power), 6f);
        worldSecretHintShown = true;
        return true;
    }

    public void TryRevealSecret(int power, Vector2 source)
    {
        if (worldSecret == null) return;
        int requiredPower = RequiredWorldSecretPower();
        Vector2 spot = worldSecret.transform.position;
        if (power != requiredPower || player == null || !player.AbilityFootprintContainsPoint(requiredPower, spot, source) ||
            ScadrykPlayerController.AbilityPathIsBlocked(requiredPower, source, spot)) return;
        ScadrykEffects.Burst(spot, PhaseAccentColors[phaseIndex], 1.5f);
        ScadrykEffects.ReleaseEcho(witnessSprite != null ? witnessSprite : CircleSprite, spot, PhaseAccentColors[phaseIndex]);
        ScadrykAudio.Play(ScadrykCue.Memory);
        Destroy(worldSecret);
        worldSecret = null;
        if (player != null) player.RestoreHealth(35f);
        ShowMemory(WorldSecretMemory(requiredPower), 7f);
        ShowStatus("Uma memória foi libertada. Nara recuperou 35 pontos de vida.", 5f);
    }

    private void PlacePhaseGround(Transform parent, float x, int room)
    {
        if (phaseGroundSprites == null || phaseGroundSprites[phaseIndex] == null) return;
        var ground = new GameObject("Painted top-down biome — " + PhaseNames[phaseIndex]);
        ground.transform.SetParent(parent);
        ground.transform.position = new Vector3(x, 0f, 0f);
        var renderer = ground.AddComponent<SpriteRenderer>();
        renderer.sprite = phaseGroundSprites[phaseIndex];
        renderer.sortingOrder = -8;
        renderer.flipX = GroundFlipX(room);
        renderer.flipY = GroundFlipY(room);
        Vector2 size = renderer.sprite.bounds.size;
        // Extend the painted ground through the portal corridor so the camera never reveals void strips.
        ground.transform.localScale = new Vector3((RoomSpacing + 0.2f) / size.x, 13f / size.y, 1f);
    }

    private static bool GroundFlipX(int room) => room == 1;
    private static bool GroundFlipY(int room) => room == 2;

    internal static bool RoomGroundOrientationsAreUnique()
    {
        for (int first = 0; first < RoomsPerPhase; first++)
        for (int second = first + 1; second < RoomsPerPhase; second++)
            if (GroundFlipX(first) == GroundFlipX(second) && GroundFlipY(first) == GroundFlipY(second)) return false;
        return true;
    }

    private void DrawRoomScenery(Transform parent, int room, float x, Color floor)
    {
        Color accent = PhaseAccentColors[phaseIndex];
        Color dim = new Color(accent.r, accent.g, accent.b, 0.19f);
        Color glow = new Color(accent.r, accent.g, accent.b, 0.38f);

        if (phaseIndex == 0)
        {
            // Shallow tide pools and reed banks mark the flooded shore without blocking movement.
            for (int side = -1; side <= 1; side += 2)
            {
                float y = side * (room == 1 ? 4.65f : 4.2f);
                MakeDecoration(parent, "Tide pool", new Vector2(x - 5.9f, y), new Vector2(3.0f, 1.05f), new Color(0.08f, 0.30f, 0.30f, 0.52f), true, -9);
                MakeShallowWaterZone(parent, new Vector2(x - 5.9f, y), new Vector2(3.0f, 1.05f));
                MakeDecoration(parent, "Tide pool", new Vector2(x + 5.6f, -y * 0.78f), new Vector2(2.5f, 0.82f), new Color(0.07f, 0.26f, 0.27f, 0.48f), true, -9);
                MakeShallowWaterZone(parent, new Vector2(x + 5.6f, -y * 0.78f), new Vector2(2.5f, 0.82f));
                for (int reed = 0; reed < 3; reed++)
                {
                    float rx = x - 7.2f + reed * 0.36f;
                    MakeDecoration(parent, "Reed", new Vector2(rx, y - side * 0.6f), new Vector2(0.12f, 0.88f), glow, false, -8, -18f + reed * 16f);
                }
            }
            DrawRune(parent, new Vector2(x + (room - 1) * 1.15f, 4.1f), 1.8f, dim);
        }
        else if (phaseIndex == 1)
        {
            // Pale salt seams and crystal clusters distinguish the dry basin from the shore.
            for (int seam = 0; seam < 5; seam++)
            {
                float sx = x - 5.4f + seam * 2.7f;
                float sy = ((seam + room) % 2 == 0) ? -3.95f : 3.95f;
                MakeDecoration(parent, "Salt vein", new Vector2(sx, sy), new Vector2(2.5f, 0.075f), dim, false, -9, (seam % 2 == 0) ? 12f : -12f);
                MakeDecoration(parent, "Salt vein branch", new Vector2(sx + 0.45f, sy + 0.35f), new Vector2(0.95f, 0.06f), dim, false, -9, -36f);
            }
            for (int cluster = 0; cluster < 3; cluster++)
            {
                float cx = x + (cluster - 1) * 5.8f;
                float cy = cluster == 1 ? 4.65f : -4.55f;
                MakeDecoration(parent, "Salt crystal base", new Vector2(cx, cy), new Vector2(1.55f, 0.68f), new Color(0.12f, 0.09f, 0.055f, 0.45f), true, -8);
                for (int shard = 0; shard < 3; shard++)
                    MakeDecoration(parent, "Salt crystal", new Vector2(cx + (shard - 1) * 0.28f, cy + 0.18f), new Vector2(0.34f, 0.78f - Mathf.Abs(shard - 1) * 0.18f), new Color(0.82f, 0.68f, 0.39f, 0.66f), false, -7, -18f + shard * 18f);
            }
            DrawRune(parent, new Vector2(x, 4.15f), 1.55f, new Color(0.9f, 0.69f, 0.36f, 0.22f));
        }
        else
        {
            // Tidal bands, buoy lamps and bell marks create a colder coastal palette.
            for (int band = 0; band < 3; band++)
            {
                float y = -4.85f + band * 4.85f;
                for (int segment = 0; segment < 4; segment++)
                {
                    float sx = x - 6.4f + segment * 4.25f + ((band + room) % 2) * 0.55f;
                    MakeDecoration(parent, "Tidal line", new Vector2(sx, y), new Vector2(2.8f, 0.075f), dim, false, -9, (segment % 2 == 0) ? -4f : 4f);
                    MakeDecoration(parent, "Tidal foam", new Vector2(sx + 0.5f, y + 0.18f), new Vector2(1.3f, 0.045f), glow, false, -9, -2f);
                }
            }
            for (int buoy = -1; buoy <= 1; buoy += 2)
            {
                MakeDecoration(parent, "Bell buoy lamp", new Vector2(x + buoy * 7.15f, 0), new Vector2(0.72f, 0.72f), new Color(0.78f, 0.61f, 0.30f, 0.82f), true, -7);
                MakeDecoration(parent, "Bell buoy glow", new Vector2(x + buoy * 7.15f, 0), new Vector2(1.55f, 1.55f), new Color(0.24f, 0.72f, 0.78f, 0.16f), true, -8);
            }
            DrawRune(parent, new Vector2(x + (room - 1) * 1.25f, 4.25f), 1.75f, glow);
        }

        // Add fine, low-contrast ground flecks so each room has more depth than a flat fill.
        for (int fleck = 0; fleck < 8; fleck++)
        {
            int seed = room * 17 + phaseIndex * 31 + fleck * 13;
            float fx = x - 7.3f + (seed % 15) * 0.98f;
            float fy = -3.5f + ((seed * 7) % 70) * 0.1f;
            MakeDecoration(parent, "Ground fleck", new Vector2(fx, fy), new Vector2(0.12f, 0.12f), new Color(accent.r, accent.g, accent.b, 0.16f), true, -9);
        }

        DrawPhaseProps(parent, room, x);
    }

    private static void MakeShallowWaterZone(Transform parent, Vector2 position, Vector2 size)
    {
        var zoneObject = new GameObject("Shallow water · slows movement");
        zoneObject.transform.SetParent(parent);
        zoneObject.transform.position = position;
        BoxCollider2D trigger = zoneObject.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = size;
        zoneObject.AddComponent<ScadrykTerrainZone>().Configure(0.78f);
    }

    public void ShowShallowWaterHint()
    {
        if (shallowWaterHintShown) return;
        shallowWaterHintShown = true;
        ShowStatus("A água rasa reduz seu passo. A esquiva mantém o impulso.", 4f);
    }

    private void DrawPhaseProps(Transform parent, int room, float x)
    {
        if (environmentSprites == null || environmentSprites.Length < 16) return;

        if (phaseIndex == 0)
        {
            // Flooded first-echo shore: willow canopy, exposed roots, reeds, moss and old stones.
            PlaceProp(parent, 0, "Echo willow", new Vector2(x - 7.15f, 3.2f), 2.15f, true, new Vector2(0.24f, 0.22f), new Vector2(0f, 0.16f), ScadrykAmbientMotion.Sway, 0.6f);
            PlaceProp(parent, 1, "Root-twisted willow", new Vector2(x + 7.1f, -6.05f), 1.9f, true, new Vector2(0.27f, 0.2f), new Vector2(0f, 0.18f), ScadrykAmbientMotion.Sway, 0.65f);
            PlaceProp(parent, 2, "Cattails", new Vector2(x - 5.8f, 3.55f), 1.45f, true, new Vector2(0.28f, 0.18f), new Vector2(0f, 0.15f), ScadrykAmbientMotion.Sway, 1.25f);
            PlaceProp(parent, 2, "Cattails", new Vector2(x + 5.65f, -6.0f), 1.35f, true, new Vector2(0.27f, 0.18f), new Vector2(0f, 0.15f), ScadrykAmbientMotion.Sway, 1.2f);
            PlaceProp(parent, 3, "Mossy stump", new Vector2(x - 4.2f, -5.65f), 1.15f, true, new Vector2(0.32f, 0.23f), new Vector2(0f, 0.18f));
            PlaceProp(parent, 4, "Mossy shoreline rocks", new Vector2(x + 4.0f, 3.85f), 1.2f, true, new Vector2(0.55f, 0.28f), new Vector2(0f, 0.18f));
            if (room == 2) PlaceProp(parent, 6, "Flooded arch ruin", new Vector2(x - 0.3f, 3.15f), 1.5f, false, Vector2.zero, Vector2.zero);
        }
        else if (phaseIndex == 1)
        {
            // Salt basin: amber crystal growths, blanched rock and shallow mineral water.
            PlaceProp(parent, 8, "Salt crystal cluster", new Vector2(x - 6.0f, -5.95f), 1.55f, true, new Vector2(0.34f, 0.22f), new Vector2(0f, 0.18f), ScadrykAmbientMotion.Glow, 0.65f);
            PlaceProp(parent, 9, "Salt spire", new Vector2(x - 5.3f, 3.7f), 1.55f, true, new Vector2(0.26f, 0.2f), new Vector2(0f, 0.16f), ScadrykAmbientMotion.Glow, 0.8f);
            PlaceProp(parent, 10, "Salt-crusted boulder", new Vector2(x + 5.1f, -5.85f), 1.45f, true, new Vector2(0.46f, 0.28f), new Vector2(0f, 0.18f));
            PlaceProp(parent, 11, "Brine pool", new Vector2(x + 1.0f, 3.65f), 1.8f, false, Vector2.zero, Vector2.zero, ScadrykAmbientMotion.Water, 1f);
            PlaceProp(parent, 4, "Salt-buried boulders", new Vector2(x + 5.8f, 3.95f), 1.18f, true, new Vector2(0.52f, 0.28f), new Vector2(0f, 0.18f));
            if (room == 2) PlaceProp(parent, 7, "Basin's standing stone", new Vector2(x - 1.25f, 3.25f), 1.35f, true, new Vector2(0.25f, 0.22f), new Vector2(0f, 0.18f), ScadrykAmbientMotion.Glow, 0.45f);
        }
        else
        {
            // Bell coast: wind-shaped pines, wave-worn rocks, lantern buoys and a broken shrine.
            PlaceProp(parent, 12, "Wind-shaped pine", new Vector2(x - 7.0f, 3.2f), 1.9f, true, new Vector2(0.25f, 0.22f), new Vector2(0f, 0.16f), ScadrykAmbientMotion.Sway, 0.9f);
            PlaceProp(parent, 12, "Coastal pine", new Vector2(x + 7.0f, -6.05f), 1.7f, true, new Vector2(0.25f, 0.2f), new Vector2(0f, 0.16f), ScadrykAmbientMotion.Sway, 0.75f);
            PlaceProp(parent, 13, "Wave-worn rocks", new Vector2(x - 4.0f, -5.9f), 1.35f, true, new Vector2(0.52f, 0.28f), new Vector2(0f, 0.16f));
            PlaceProp(parent, 14, "Lantern buoy posts", new Vector2(x + 4.7f, 3.75f), 1.35f, true, new Vector2(0.23f, 0.22f), new Vector2(0f, 0.16f), ScadrykAmbientMotion.Flicker, 0.8f);
            if (room == 2) PlaceProp(parent, 15, "Bell shrine", new Vector2(x - 0.5f, 3.1f), 1.6f, false, Vector2.zero, Vector2.zero, ScadrykAmbientMotion.Glow, 0.55f);
        }
    }

    private void PlaceProp(Transform parent, int spriteIndex, string name, Vector2 position, float scale, bool solid, Vector2 hitboxSize, Vector2 hitboxOffset, ScadrykAmbientMotion motion = ScadrykAmbientMotion.None, float motionStrength = 1f, float rotation = 0f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = position;
        go.transform.localScale = Vector3.one * scale;
        go.transform.rotation = Quaternion.Euler(0f, 0f, rotation);
        var groundShadow = new GameObject("Soft contact shadow"); groundShadow.transform.SetParent(go.transform, false);
        groundShadow.transform.localPosition = new Vector3(0.08f, -0.16f, 0f); groundShadow.transform.localScale = new Vector3(1.3f, 0.62f, 1f);
        var shadowRenderer = groundShadow.AddComponent<SpriteRenderer>(); shadowRenderer.sprite = ScadrykEffects.SoftShadowSprite; shadowRenderer.color = new Color(0.035f, 0.05f, 0.07f, 0.8f); shadowRenderer.sortingOrder = 0;
        var visual = new GameObject("Visual");
        visual.transform.SetParent(go.transform, false);
        var renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = environmentSprites[spriteIndex];
        renderer.sortingOrder = 1;
        if (motion != ScadrykAmbientMotion.None) visual.AddComponent<ScadrykAmbientProp>().Configure(motion, motionStrength);
        go.AddComponent<ScadrykDepthSort>().SetBias(2);
        if (solid)
        {
            var collider = go.AddComponent<BoxCollider2D>();
            collider.size = hitboxSize;
            collider.offset = hitboxOffset;
        }
    }

    private static void DrawRune(Transform parent, Vector2 center, float radius, Color color)
    {
        MakeDecoration(parent, "Ancient mark", center + Vector2.up * radius, new Vector2(0.12f, radius * 0.62f), color, false, -8, 0f);
        MakeDecoration(parent, "Ancient mark", center + Vector2.down * radius, new Vector2(0.12f, radius * 0.62f), color, false, -8, 0f);
        MakeDecoration(parent, "Ancient mark", center + Vector2.left * radius, new Vector2(0.12f, radius * 0.62f), color, false, -8, 90f);
        MakeDecoration(parent, "Ancient mark", center + Vector2.right * radius, new Vector2(0.12f, radius * 0.62f), color, false, -8, 90f);
        MakeDecoration(parent, "Ancient mark", center, new Vector2(0.36f, 0.36f), color, true, -8);
    }

    private void DrawRoomObstacles(Transform parent, int index, float x)
    {
        foreach (var offset in RoomObstacleOffsets(phaseIndex, index))
        {
            Vector2 position = new Vector2(x + offset.x, offset.y);
            int variation = Mathf.Abs(Mathf.RoundToInt(offset.x * 3f + offset.y * 5f)) % 3;
            if (phaseIndex == 0 && variation == 0 && fallenWillowLogSprite != null)
                PlaceFallenLog(parent, position, index, variation);
            else if (phaseIndex == 0)
                PlaceProp(parent, variation == 1 ? 3 : 4, "Forest boulder and stump", position, variation == 1 ? 0.9f : 0.78f, true, new Vector2(0.85f, 0.62f), new Vector2(0f, 0.12f), ScadrykAmbientMotion.None, 1f, ((index + variation) % 2 == 0) ? 18f : -22f);
            else if (phaseIndex == 1)
                PlaceProp(parent, variation == 0 ? 8 : variation == 1 ? 10 : 4, "Salt crystal and crusted rock", position, variation == 0 ? 0.9f : 0.82f, true, new Vector2(0.76f, 0.62f), new Vector2(0f, 0.12f), ScadrykAmbientMotion.None, 1f, ((index + variation) % 2 == 0) ? 12f : -16f);
            else
                PlaceProp(parent, variation == 0 ? 13 : variation == 1 ? 14 : 12, "Tide-worn rock and post", position, variation == 1 ? 0.8f : 0.88f, true, new Vector2(0.78f, 0.64f), new Vector2(0f, 0.12f), ScadrykAmbientMotion.None, 1f, ((index + variation) % 2 == 0) ? 14f : -18f);
        }
    }

    internal static Vector2[] RoomObstacleOffsets(int biome, int room)
    {
        biome = Mathf.Clamp(biome, 0, 2);
        room = Mathf.Clamp(room, 0, 2);
        if (biome == 0)
        {
            if (room == 0) return new[] { new Vector2(-3.4f, 2.9f), new Vector2(3.1f, -2.8f), new Vector2(4.4f, 2.1f) };
            if (room == 1) return new[] { new Vector2(-4.0f, -2.5f), new Vector2(1.8f, 3.5f), new Vector2(4.3f, -2.8f), new Vector2(-0.4f, -3.7f) };
            return new[] { new Vector2(-4.3f, 2.8f), new Vector2(-2.2f, -3.4f), new Vector2(3.8f, 1.9f) };
        }
        if (biome == 1)
        {
            if (room == 0) return new[] { new Vector2(-3.7f, -2.7f), new Vector2(-1.2f, 3.6f), new Vector2(4.2f, -1.9f) };
            if (room == 1) return new[] { new Vector2(-4.1f, 0.8f), new Vector2(-1.7f, 3.7f), new Vector2(1.5f, -3.6f), new Vector2(4.4f, -0.2f) };
            return new[] { new Vector2(-4.1f, 2.7f), new Vector2(-4.2f, -2.6f), new Vector2(4.0f, -0.5f) };
        }
        if (room == 0) return new[] { new Vector2(-3.9f, 2.9f), new Vector2(-0.8f, -3.5f), new Vector2(3.9f, 2.5f) };
        if (room == 1) return new[] { new Vector2(-4.3f, 3.1f), new Vector2(-4.0f, -3.2f), new Vector2(0.2f, 2.6f), new Vector2(4.2f, -2.9f) };
        return new[] { new Vector2(-3.8f, 2.7f), new Vector2(-3.6f, -2.8f), new Vector2(4.2f, 0.8f) };
    }

    private static bool RoomHasWalkableRoute(int biome, int room)
    {
        const float step = 0.35f;
        const float playerRadius = 0.42f;
        float minX = -9.7f + 0.25f + playerRadius;
        float maxX = 9.7f - 0.25f - playerRadius;
        float minY = -6.6f + 0.25f + playerRadius;
        float maxY = 6.6f - 0.25f - playerRadius;
        int columns = Mathf.FloorToInt((maxX - minX) / step) + 1;
        int rows = Mathf.FloorToInt((maxY - minY) / step) + 1;
        var blocked = new bool[columns, rows];
        Vector2[] obstacles = RoomObstacleOffsets(biome, room);
        for (int x = 0; x < columns; x++)
        for (int y = 0; y < rows; y++)
        {
            Vector2 point = new Vector2(minX + x * step, minY + y * step);
            foreach (Vector2 obstacle in obstacles)
            {
                float clearance = ObstacleSpawnCollisionRadius(biome, obstacle, true) + playerRadius + 0.12f;
                if (Vector2.Distance(point, obstacle) < clearance) { blocked[x, y] = true; break; }
            }
        }

        int startX = Mathf.RoundToInt((-6.2f - minX) / step);
        int startY = Mathf.RoundToInt(-minY / step);
        int exitX = Mathf.RoundToInt((8.05f - minX) / step);
        int exitY = startY;
        if (blocked[startX, startY] || blocked[exitX, exitY]) return false;

        var visited = new bool[columns, rows];
        var queue = new int[columns * rows];
        int read = 0, write = 0;
        int startIndex = startY * columns + startX;
        queue[write++] = startIndex;
        visited[startX, startY] = true;
        while (read < write)
        {
            int current = queue[read++];
            int x = current % columns;
            int y = current / columns;
            if (x == exitX && y == exitY) return true;
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nextX = x + dx, nextY = y + dy;
                if (nextX < 0 || nextX >= columns || nextY < 0 || nextY >= rows ||
                    visited[nextX, nextY] || blocked[nextX, nextY]) continue;
                if (dx != 0 && dy != 0 && (blocked[x + dx, y] || blocked[x, y + dy])) continue;
                visited[nextX, nextY] = true;
                queue[write++] = nextY * columns + nextX;
            }
        }
        return false;
    }

    private bool ActiveRoomHasWalkableRoute(Vector2 center)
    {
        if (levelRoot == null) return false;
        const float step = 0.35f;
        const float playerRadius = 0.42f;
        float minX = -9.7f + 0.25f + playerRadius;
        float maxX = 9.7f - 0.25f - playerRadius;
        float minY = -6.6f + 0.25f + playerRadius;
        float maxY = 6.6f - 0.25f - playerRadius;
        int columns = Mathf.FloorToInt((maxX - minX) / step) + 1;
        int rows = Mathf.FloorToInt((maxY - minY) / step) + 1;
        var solids = new System.Collections.Generic.List<Collider2D>();
        foreach (Collider2D collider in levelRoot.GetComponentsInChildren<Collider2D>())
        {
            if (collider == null || !collider.enabled || collider.isTrigger ||
                collider.GetComponentInParent<ScadrykPlayerController>() != null ||
                collider.GetComponentInParent<ScadrykEnemyHealth>() != null) continue;
            solids.Add(collider);
        }

        var blocked = new bool[columns, rows];
        for (int x = 0; x < columns; x++)
        for (int y = 0; y < rows; y++)
        {
            Vector2 point = center + new Vector2(minX + x * step, minY + y * step);
            foreach (Collider2D solid in solids)
            {
                if ((solid.ClosestPoint(point) - point).sqrMagnitude < playerRadius * playerRadius)
                { blocked[x, y] = true; break; }
            }
        }

        int startX = Mathf.RoundToInt((-6.2f - minX) / step);
        int startY = Mathf.RoundToInt(-minY / step);
        int exitX = Mathf.RoundToInt((8.05f - minX) / step);
        int exitY = startY;
        if (blocked[startX, startY] || blocked[exitX, exitY]) return false;

        var visited = new bool[columns, rows];
        var queue = new int[columns * rows];
        int read = 0, write = 0;
        queue[write++] = startY * columns + startX;
        visited[startX, startY] = true;
        while (read < write)
        {
            int current = queue[read++];
            int x = current % columns;
            int y = current / columns;
            if (x == exitX && y == exitY) return true;
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nextX = x + dx, nextY = y + dy;
                if (nextX < 0 || nextX >= columns || nextY < 0 || nextY >= rows ||
                    visited[nextX, nextY] || blocked[nextX, nextY]) continue;
                if (dx != 0 && dy != 0 && (blocked[x + dx, y] || blocked[x, y + dy])) continue;
                visited[nextX, nextY] = true;
                queue[write++] = nextY * columns + nextX;
            }
        }
        return false;
    }

    internal static bool RoomObstacleLayoutsAreDistinctAndOpen()
    {
        for (int biome = 0; biome < 3; biome++)
        for (int room = 0; room < 3; room++)
        {
            Vector2[] layout = RoomObstacleOffsets(biome, room);
            if (layout.Length < 3) return false;
            foreach (Vector2 offset in layout)
                if (Mathf.Abs(offset.x + 6.2f) < 1.25f || Mathf.Abs(offset.x - 8.05f) < 1.25f || Mathf.Abs(offset.y) > 5.2f)
                    return false;
            if (!RoomHasWalkableRoute(biome, room)) return false;

            for (int priorBiome = 0; priorBiome <= biome; priorBiome++)
            for (int priorRoom = 0; priorRoom < 3; priorRoom++)
            {
                if (priorBiome == biome && priorRoom >= room) break;
                Vector2[] prior = RoomObstacleOffsets(priorBiome, priorRoom);
                if (prior.Length != layout.Length) continue;
                bool same = true;
                foreach (Vector2 offset in layout)
                {
                    bool found = false;
                    foreach (Vector2 candidate in prior) if (candidate == offset) { found = true; break; }
                    if (!found) { same = false; break; }
                }
                if (same) return false;
            }
        }
        return true;
    }

    private void PlaceFallenLog(Transform parent, Vector2 position, int room, int variant)
    {
        var obstacle = new GameObject("Fallen willow — solid obstacle");
        obstacle.transform.SetParent(parent);
        obstacle.transform.position = position;
        obstacle.transform.rotation = Quaternion.Euler(0f, 0f, ((room + variant) % 2 == 0) ? 13f : -24f);
        var shadow = new GameObject("Long contact shadow"); shadow.transform.SetParent(obstacle.transform, false);
        shadow.transform.localPosition = new Vector3(0.1f, -0.18f, 0f); shadow.transform.localScale = new Vector3(1.85f, 0.68f, 1f);
        var shadowRenderer = shadow.AddComponent<SpriteRenderer>(); shadowRenderer.sprite = ScadrykEffects.SoftShadowSprite; shadowRenderer.color = new Color(0.025f, 0.04f, 0.045f, 0.82f); shadowRenderer.sortingOrder = 0;
        var visual = new GameObject("Fallen tree art");
        visual.transform.SetParent(obstacle.transform, false);
        var renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = fallenWillowLogSprite;
        renderer.sortingOrder = 1;
        Vector2 spriteSize = fallenWillowLogSprite.bounds.size;
        visual.transform.localScale = new Vector3(3.05f / spriteSize.x, 1.12f / spriteSize.y, 1f);
        obstacle.AddComponent<ScadrykDepthSort>().SetBias(2);
        var collider = obstacle.AddComponent<CapsuleCollider2D>();
        collider.direction = CapsuleDirection2D.Horizontal;
        collider.size = new Vector2(2.75f, 0.72f);
    }

    private static void MakeDecoration(Transform parent, string name, Vector2 position, Vector2 size, Color color, bool circle, int order, float rotation = 0f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = position;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        go.transform.rotation = Quaternion.Euler(0f, 0f, rotation);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = circle ? CircleSprite : SquareSprite;
        renderer.color = color;
        renderer.sortingOrder = order;
    }

    private void SpawnRoomEnemies()
    {
        activeEnemies.Clear();
        int roomId = phaseIndex * RoomsPerPhase + roomIndex;
        int count = EnemyCountForRoom(roomId);
        enemiesRemaining = count;
        var placedOffsets = new Vector2[count];
        var placedRadii = new float[count];
        for (int i = 0; i < count; i++)
        {
            bool boss = roomIndex == RoomsPerPhase - 1;
            int role = EnemyRoleForRoom(roomId, i);
            string[] roleNames = { "Caçador", "Atirador", "Ressonador", "Tecelão" };
            float collisionRadius = EnemySpawnCollisionRadius(role, boss);
            Vector2 offset = FindEnemySpawnOffset(phaseIndex, roomIndex, i, count, boss, role,
                fallenWillowLogSprite != null, placedOffsets, placedRadii, i);
            placedOffsets[i] = offset;
            placedRadii[i] = collisionRadius;
            Vector2 position = roomCenter + offset;
            var enemy = new GameObject(boss ? GuardianNames[phaseIndex] : roleNames[role] + " — " + PhaseNames[phaseIndex]);
            enemy.transform.SetParent(levelRoot.transform);
            enemy.transform.position = position;
            enemy.transform.localScale = Vector3.one * (boss ? 1.85f : role == 0 ? 0.94f : role == 1 ? 0.78f : role == 2 ? 1.08f : 0.86f);
            string sheet = boss ? (phaseIndex == 0 ? "Bosses" : phaseIndex == 1 ? "BossesMidgame" : "BossesFinalWave") : "Enemies";
            Sprite[] frames = LoadSheetRow(sheet, boss ? phaseIndex : i % 4, false);
            if (phaseEnemySprites != null && phaseEnemySprites[phaseIndex] != null)
                frames = new[] { phaseEnemySprites[phaseIndex], phaseEnemySprites[phaseIndex], phaseEnemySprites[phaseIndex], phaseEnemySprites[phaseIndex] };
            var enemyShadow = new GameObject("Soft ground shadow"); enemyShadow.transform.SetParent(enemy.transform, false);
            enemyShadow.transform.localPosition = new Vector3(0.09f, -0.2f, 0f); enemyShadow.transform.localScale = new Vector3(boss ? 0.92f : 0.65f, boss ? 0.52f : 0.38f, 1f);
            var enemyShadowRenderer = enemyShadow.AddComponent<SpriteRenderer>(); enemyShadowRenderer.sprite = ScadrykEffects.SoftShadowSprite; enemyShadowRenderer.color = new Color(0.04f, 0.055f, 0.07f, 0.82f); enemyShadowRenderer.sortingOrder = 3;
            var enemyVolume = new GameObject("Raised sprite shading"); enemyVolume.transform.SetParent(enemy.transform, false);
            enemyVolume.transform.localPosition = new Vector3(0f, -0.11f, 0f); enemyVolume.transform.localScale = new Vector3(1.02f, 1.02f, 1f);
            var enemyVolumeRenderer = enemyVolume.AddComponent<SpriteRenderer>(); enemyVolumeRenderer.sprite = frames != null && frames.Length > 0 ? frames[0] : CircleSprite; enemyVolumeRenderer.color = new Color(0.02f, 0.05f, 0.065f, 0.74f); enemyVolumeRenderer.sortingOrder = 3;
            var enemyVisual = new GameObject("Animated visual"); enemyVisual.transform.SetParent(enemy.transform, false);
            var renderer = enemyVisual.AddComponent<SpriteRenderer>(); renderer.sprite = frames != null && frames.Length > 0 ? frames[0] : CircleSprite; renderer.color = Color.white; renderer.sortingOrder = 4;
            var collider = enemy.AddComponent<CircleCollider2D>(); collider.radius = 0.48f;
            enemy.AddComponent<Rigidbody2D>();
            enemy.AddComponent<ScadrykDepthSort>().SetBias(4);
            enemy.AddComponent<ScadrykActorAnimator>().SetVisual(enemyVisual.transform, enemyVolumeRenderer);
            var health = enemy.AddComponent<ScadrykEnemyHealth>();
            health.Configure(boss ? 320f + phaseIndex * 80f : 72f + phaseIndex * 12f, boss ? 1.55f : 1.9f + phaseIndex * 0.1f,
                boss, phaseIndex, frames, role, ScadrykEnemyHealth.OpeningAttackDelay(i, boss));
            activeEnemies.Add(health);
            if (boss) phaseGuardian = health;
            enemy.AddComponent<ScadrykHealthBar>().Configure(false, boss, boss ? 1.45f : 1.22f);
        }
    }

    internal static int EnemyCountForRoom(int roomId)
    {
        int room = Mathf.Abs(roomId) % RoomsPerPhase;
        if (room == RoomsPerPhase - 1) return 1;
        if (roomId == 3 || roomId == 6) return 0;
        return room == 1 ? 4 : 3;
    }

    internal static float EnemySpawnCollisionRadius(int role, bool boss)
    {
        if (boss) return 0.58f * 1.85f;
        float scale = role == 0 ? 0.94f : role == 1 ? 0.78f : role == 2 ? 1.08f : 0.86f;
        return 0.34f * scale;
    }

    private static float ObstacleSpawnCollisionRadius(int biome, Vector2 offset, bool hasFallenLog)
    {
        int variation = Mathf.Abs(Mathf.RoundToInt(offset.x * 3f + offset.y * 5f)) % 3;
        if (biome == 0 && variation == 0 && hasFallenLog) return 1.48f;
        if (biome == 0) return variation == 1 ? 0.49f : 0.43f;
        if (biome == 1) return variation == 0 ? 0.45f : 0.42f;
        return variation == 1 ? 0.42f : 0.46f;
    }

    internal static Vector2 FindEnemySpawnOffset(int biome, int room, int enemyIndex, int enemyCount, bool boss, int role,
        bool hasFallenLog, Vector2[] previousOffsets, float[] previousRadii, int previousCount)
    {
        int safeCount = Mathf.Max(1, enemyCount);
        float angle = Mathf.PI * 2f * enemyIndex / safeCount + 0.3f;
        float orbitRadius = boss ? 2.5f : 3.1f + (enemyIndex % 2) * 0.7f;
        float ownRadius = EnemySpawnCollisionRadius(role, boss);
        float bestMargin = float.NegativeInfinity;
        int availablePrevious = previousOffsets == null || previousRadii == null ? 0 : Mathf.Min(previousCount, Mathf.Min(previousOffsets.Length, previousRadii.Length));
        Vector2 best = new Vector2(Mathf.Cos(angle) * orbitRadius, Mathf.Sin(angle) * orbitRadius);
        Vector2[] obstacles = RoomObstacleOffsets(biome, room);
        for (int attempt = 0; attempt < 32; attempt++)
        {
            int step = (attempt + 1) / 2;
            float direction = attempt == 0 || attempt % 2 == 1 ? 1f : -1f;
            if (attempt == 0) step = 0;
            float candidateAngle = angle + direction * step * (Mathf.PI * 2f / 32f);
            Vector2 candidate = new Vector2(Mathf.Cos(candidateAngle) * orbitRadius, Mathf.Sin(candidateAngle) * orbitRadius);
            float margin = float.PositiveInfinity;
            foreach (Vector2 obstacle in obstacles)
                margin = Mathf.Min(margin, Vector2.Distance(candidate, obstacle) - ownRadius - ObstacleSpawnCollisionRadius(biome, obstacle, hasFallenLog) - 0.12f);
            for (int placed = 0; placed < availablePrevious; placed++)
                margin = Mathf.Min(margin, Vector2.Distance(candidate, previousOffsets[placed]) - ownRadius - previousRadii[placed] - 0.12f);
            if (margin > bestMargin) { bestMargin = margin; best = candidate; }
            if (margin >= 0f) return candidate;
        }
        return best;
    }

    private bool EncounterSpawnsAvoidCover()
    {
        for (int biome = 0; biome < PhaseNames.Length; biome++)
        for (int room = 0; room < RoomsPerPhase; room++)
        {
            int roomId = biome * RoomsPerPhase + room;
            int count = EnemyCountForRoom(roomId);
            var offsets = new Vector2[count];
            var radii = new float[count];
            bool boss = room == RoomsPerPhase - 1;
            for (int i = 0; i < count; i++)
            {
                int role = EnemyRoleForRoom(roomId, i);
                float ownRadius = EnemySpawnCollisionRadius(role, boss);
                Vector2 offset = FindEnemySpawnOffset(biome, room, i, count, boss, role, fallenWillowLogSprite != null, offsets, radii, i);
                foreach (Vector2 obstacle in RoomObstacleOffsets(biome, room))
                    if (Vector2.Distance(offset, obstacle) < ownRadius + ObstacleSpawnCollisionRadius(biome, obstacle, fallenWillowLogSprite != null) + 0.119f)
                        return false;
                for (int prior = 0; prior < i; prior++)
                    if (Vector2.Distance(offset, offsets[prior]) < ownRadius + radii[prior] + 0.119f)
                        return false;
                offsets[i] = offset;
                radii[i] = ownRadius;
            }
        }
        return true;
    }

    internal static int EnemyRoleForRoom(int roomId, int enemyIndex)
    {
        if (enemyIndex < 0) return 0;
        if (roomId == 4) return enemyIndex switch { 0 => 0, 1 => 0, 2 => 2, _ => 3 };
        if (roomId == 7) return enemyIndex switch { 0 => 1, 1 => 1, 2 => 2, _ => 3 };
        return enemyIndex % 4;
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
            string[] powers = { "Crescente Verdejante", "Estouro de Sal", "Rompante da Maré" };
            int powerIndex = Mathf.Clamp(enemy.BiomeIndex, 0, powers.Length - 1);
            if (worldSecret != null && RequiredWorldSecretPower() == powerIndex) worldSecretHintShown = false;
            acquiredPower = "Novo poder adquirido: " + powers[powerIndex] + " (" + InputBinding(powerIndex + 3) + ").";
            ShowMemory(GuardianMemories[powerIndex], 7f);
        }

        enemiesRemaining = Mathf.Max(0, enemiesRemaining - 1);
        if (enemiesRemaining == 0)
        {
            if (phaseIndex == PhaseNames.Length - 1 && roomIndex == RoomsPerPhase - 1 && enemy.IsBoss)
            {
                if (player != null) player.RestoreHealth(player.MaxHealth);
                enemiesRemaining = 1;
                ShowStatus("Nara escuta a última lembrança antes que algo desperte além dos sinos.", 7f);
                StartCoroutine(SpawnFinalBossAfterMemory());
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

    public void RecordInputMode(string mode)
    {
        if (mode == "keyboard_mouse" || mode == "gamepad") sessionInputMode = mode;
    }

    private string FormatSessionCsvRow(bool completed)
    {
        return sessionId + "," + System.DateTime.UtcNow.ToString("o") + "," + (phaseIndex + 1) + "," + (roomIndex + 1) + "," +
            Mathf.RoundToInt(Mathf.Max(0f, roomActiveSeconds)) + "," + roomDeaths + "," + unclearAttackMarks + "," + roomPowerUses[0] + "," +
            roomPowerUses[1] + "," + roomPowerUses[2] + "," + enemiesRemaining + "," + completed.ToString().ToLowerInvariant() + "," + sessionInputMode + "\n";
    }

    private void RecordRoomSession()
    {
        if (smokeRun || showTitle || roomSessionRecorded) return;
        roomSessionRecorded = true;
        try
        {
            string path = Path.Combine(Application.persistentDataPath, "scadryk-sessions-v5.csv");
            if (!File.Exists(path)) File.WriteAllText(path, SessionCsvHeader);
            bool completed = roomIndex >= 0 && roomIndex < clearedRooms.Length && clearedRooms[roomIndex];
            File.AppendAllText(path, FormatSessionCsvRow(completed));
        }
        catch (System.Exception exception) { Debug.LogWarning("Scadryk: não foi possível registrar a sessão: " + exception.Message); }
    }

    private void OnApplicationQuit()
    {
        Time.timeScale = 1f;
        if (levelRoot != null) RecordRoomSession();
    }

    private static void SaveProgress(int phase, int room)
    {
        if (smokeRun) return;
        WriteCheckpoint("Scadryk.", phase, room);
    }

    internal static void WriteCheckpoint(string keyPrefix, int phase, int room)
    {
        PlayerPrefs.SetInt(keyPrefix + "Phase", Mathf.Clamp(phase, 0, PhaseNames.Length - 1));
        PlayerPrefs.SetInt(keyPrefix + "Room", Mathf.Clamp(room, 0, RoomsPerPhase - 1));
        PlayerPrefs.Save();
    }

    internal static bool HasCheckpoint(string keyPrefix) =>
        PlayerPrefs.HasKey(keyPrefix + "Phase") && PlayerPrefs.HasKey(keyPrefix + "Room");

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
        statusMessage = "Rompante da Maré (" + InputBinding(5) + ") aprendido! O Arauto do Vazio despertou com três ataques.";
        statusUntil = Time.time + 8f;
    }

    private IEnumerator SpawnFinalBossAfterMemory()
    {
        while (Time.time < memoryUntil) yield return null;
        if (player == null || victory || showTitle) yield break;
        SpawnFinalBoss();
    }

    private void UnlockExit()
    {
        if (gateObject != null || anchorsActivated < anchorsRequired) return;
        exitPosition = roomCenter + Vector2.right * 8.05f;
        bool finalPortal = phaseIndex == PhaseNames.Length - 1 && roomIndex == RoomsPerPhase - 1;
        bool phasePortal = roomIndex == RoomsPerPhase - 1;
        Color portalColor = finalPortal ? new Color(0.8f, 0.26f, 1f) : PhaseAccentColors[phaseIndex];
        float portalSize = finalPortal ? 8.5f : phasePortal ? 3.8f : 3.15f;
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
        ScadrykAudio.Play(ScadrykCue.Victory);
        if (!smokeRun)
        {
            PlayerPrefs.DeleteKey("Scadryk.Phase");
            PlayerPrefs.DeleteKey("Scadryk.Room");
            PlayerPrefs.Save();
        }
        statusMessage = VictoryStatus;
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
        if (player != null) player.ResetCombatCooldowns();
        BuildPhase(phaseIndex, roomIndex, false, true);
        ShowStatus("Você caiu e retornou ao começo da sala.", 2.5f);
    }

    public void RecordPlayerDeath() => roomDeaths++;

    private static void ClearRoomProjectiles()
    {
        foreach (ScadrykEnemyProjectile projectile in FindObjectsByType<ScadrykEnemyProjectile>(FindObjectsSortMode.None))
            if (projectile != null) Destroy(projectile.gameObject);
    }

    private void ShowStatus(string message, float seconds) { statusMessage = message; statusUntil = Time.time + seconds; }

    private void ShowMemory(string message, float seconds) { memoryText = message; memoryUntil = Time.time + seconds; }

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
        Texture2D texture = Resources.Load<Texture2D>("ScadrykArt/" + name);
        if (texture == null) return null;
        int frameCount = wholeSheet ? 16 : 4;
        var result = new Sprite[frameCount];
        for (int frame = 0; frame < frameCount; frame++)
        {
            int sheetRow = wholeSheet ? frame / 4 : row % 4;
            int column = frame % 4;
            int xMin = Mathf.RoundToInt(column * texture.width / 4f);
            int xMax = Mathf.RoundToInt((column + 1) * texture.width / 4f);
            int yMin = Mathf.RoundToInt(texture.height - (sheetRow + 1) * texture.height / 4f);
            int yMax = Mathf.RoundToInt(texture.height - sheetRow * texture.height / 4f);
            var rect = new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
            var sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.06f), 220f);
            sprite.name = name + "_" + (sheetRow * 4 + column).ToString("00");
            result[frame] = sprite;
        }
        return result;
    }

    private static Sprite LoadSingleSprite(string name, float pixelsPerUnit)
    {
        Texture2D texture = Resources.Load<Texture2D>("ScadrykArt/" + name);
        if (texture == null) return null;
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
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
        if (Vector2.Dot(steeredHeading, Vector2.right) < 0.8f || Vector2.Distance(enclosedHeading, Vector2.right) > 0.001f)
        { SmokeFail("A navegação dos Ecos não contorna um bloqueio ou não mantém avanço quando todos os lados estão fechados"); yield break; }
        Debug.Log("SCADRYK_SMOKE: direção de contorno mantém avanço e tem fallback quando não há passagem livre");
        if (ScadrykEnemyHealth.AttackRecoveryDuration(false, false) <= 0f ||
            ScadrykEnemyHealth.AttackRecoveryDuration(true, false) <= ScadrykEnemyHealth.AttackRecoveryDuration(false, false) ||
            ScadrykEnemyHealth.AttackRecoveryDuration(false, true) <= ScadrykEnemyHealth.AttackRecoveryDuration(true, false))
        { SmokeFail("A recuperação após impacto perdeu duração ou gradação por porte do inimigo"); yield break; }
        Debug.Log("SCADRYK_SMOKE: recuperação de impacto aumenta de Eco comum a guardião e Arauto");
        if (ScadrykActorAnimator.WindupStrength(1f, 1f, 1f) != 0f ||
            Mathf.Abs(ScadrykActorAnimator.WindupStrength(1.5f, 1f, 1f) - 0.5f) > 0.001f ||
            ScadrykActorAnimator.WindupStrength(2f, 1f, 1f) != 1f)
        { SmokeFail("A preparação animada não percorre o início, meio e fim do aviso inimigo"); yield break; }
        Debug.Log("SCADRYK_SMOKE: pose de preparação acompanha o aviso até a resolução do ataque");
        if (!RoomGroundOrientationsAreUnique())
        { SmokeFail("As três salas repetem a mesma orientação do cenário de fundo"); yield break; }
        Debug.Log("SCADRYK_SMOKE: fundos pintados variam entre as três salas de cada bioma");
        Color warningTint = ScadrykEnemyHealth.StateTint(true, false, Color.white);
        Color exposedTint = ScadrykEnemyHealth.StateTint(false, true, Color.white);
        Color interruptedExposeTint = ScadrykEnemyHealth.StateTint(true, true, Color.white);
        if (warningTint == exposedTint || warningTint != interruptedExposeTint)
        { SmokeFail("O aviso de ataque se confunde com a vulnerabilidade ou perde prioridade durante a exposição"); yield break; }
        Debug.Log("SCADRYK_SMOKE: aviso âmbar e vulnerabilidade azul são distintos; aviso permanece prioritário");
        yield return SmokeProjectileCover();
        if (!smokeProjectileCoverPassed) { SmokeFail("Projéteis em movimento não param na cobertura sólida ou atravessam o gatilho de teste"); yield break; }
        Debug.Log("SCADRYK_SMOKE: projétil colide com cobertura sólida e atravessa gatilho sem parar");
        if (!SmokeBindingResetIsolation()) { SmokeFail("Restaurar controles alterou outro dispositivo ou preferências"); yield break; }
        Debug.Log("SCADRYK_SMOKE: padrões de teclado e controle são independentes e preservam preferências");
        string checkpointTestPrefix = "Scadryk.Smoke." + System.DateTime.UtcNow.Ticks + ".";
        bool checkpointRoundTrip = false;
        try
        {
            bool newGameDefaults = !HasCheckpoint(checkpointTestPrefix) &&
                !TryLoadCheckpoint(checkpointTestPrefix, out int defaultPhase, out int defaultRoom) && defaultPhase == 0 && defaultRoom == 0;
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
            checkpointRoundTrip = newGameDefaults && ignoresOrphanedRoom && ignoresOrphanedPhase && restoresLastPortal && clampsInvalidData;
        }
        finally
        {
            PlayerPrefs.DeleteKey(checkpointTestPrefix + "Phase");
            PlayerPrefs.DeleteKey(checkpointTestPrefix + "Room");
            PlayerPrefs.Save();
        }
        if (!checkpointRoundTrip) { SmokeFail("Checkpoint parcial não foi ignorado, ou o portal não foi restaurado/limitado"); yield break; }
        Debug.Log("SCADRYK_SMOKE: checkpoint grava, carrega, limita e limpa chaves temporárias");
        if (!SmokeCoverBlocksOnlyWhenSolid())
        { SmokeFail("Cobertura não bloqueia poderes direcionais ou gatilhos bloqueiam poderes circulares"); yield break; }
        Debug.Log("SCADRYK_SMOKE: cobertura interrompe golpes direcionais, não o Estouro circular, e gatilhos não bloqueiam");
        if (!RoomObstacleLayoutsAreDistinctAndOpen())
        { SmokeFail("As nove salas repetem obstáculos, bloqueiam entrada/saída ou não têm rota caminhável até o portal"); yield break; }
        Debug.Log("SCADRYK_SMOKE: nove layouts distintos têm corredores caminháveis entre entrada e portal");
        if (!EncounterSpawnsAvoidCover())
        { SmokeFail("Um Eco ou guardião nasce dentro da cobertura ou sobre outro inimigo"); yield break; }
        Debug.Log("SCADRYK_SMOKE: surgimento dos encontros evita cobertura sólida nas nove salas");
        if (Mathf.Abs(ScadrykEnemyHealth.OpeningAttackDelay(0, false) - 0.3f) > 0.001f ||
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
        if (EnemyRoleForRoom(1, 0) != 0 || EnemyRoleForRoom(1, 1) != 1 || EnemyRoleForRoom(1, 2) != 2 || EnemyRoleForRoom(1, 3) != 3 ||
            EnemyRoleForRoom(4, 0) != 0 || EnemyRoleForRoom(4, 1) != 0 || EnemyRoleForRoom(4, 2) != 2 || EnemyRoleForRoom(4, 3) != 3 ||
            EnemyRoleForRoom(7, 0) != 1 || EnemyRoleForRoom(7, 1) != 1 || EnemyRoleForRoom(7, 2) != 2 || EnemyRoleForRoom(7, 3) != 3)
        { SmokeFail("As composições dos encontros de quatro inimigos perderam sua identidade tática"); yield break; }
        Debug.Log("SCADRYK_SMOKE: equipes de bosque, sal e costa têm papéis próprios");
        if (GuardianNames.Length != 3 || GuardianIntroductions.Length != 3 || GuardianMemories.Length != 3)
        { SmokeFail("Cada bioma precisa de nome, fala e memória próprios para o guardião"); yield break; }
        if (!OpeningMemory.Contains("Grande Onda") || !OpeningMemory.Contains("Nara") ||
            !GuardianMemories[0].Contains("Nima") || !BasinWitnessLines[0].Contains("primeiro sino") ||
            !GuardianMemories[1].Contains("última onda") || !GuardianMemories[2].Contains("poupar os vivos") ||
            !EndingMemory.Contains("Nara escolhe lembrar") || !EndingMemory.Contains("aprisionou os mortos"))
        { SmokeFail("A história perdeu a causa da maré imóvel, a ligação de Nima ou a resolução do conflito"); yield break; }
        Debug.Log("SCADRYK_SMOKE: abertura, memórias de Nima e dos guardiões e resolução final formam o arco da maré");
        for (int guardian = 0; guardian < GuardianNames.Length; guardian++)
            if (string.IsNullOrWhiteSpace(GuardianNames[guardian]) || !GuardianIntroductions[guardian].StartsWith(GuardianNames[guardian]) ||
                !GuardianMemories[guardian].Contains(GuardianNames[guardian]))
            { SmokeFail("Apresentação e lembrança não correspondem ao guardião " + guardian); yield break; }
        if (GuardianNames[0] == GuardianNames[1] || GuardianNames[1] == GuardianNames[2] || GuardianNames[0] == GuardianNames[2])
        { SmokeFail("Os guardiões dos três biomas precisam de identidades distintas"); yield break; }
        Debug.Log("SCADRYK_SMOKE: guardiões recebem identidades e memórias próprias");
        ScadrykCue laneCue = ScadrykEnemyHealth.WarningCueFor(0, false, false, 0);
        ScadrykCue volleyCue = ScadrykEnemyHealth.WarningCueFor(0, false, false, 1);
        ScadrykCue areaCue = ScadrykEnemyHealth.WarningCueFor(0, false, false, 2);
        ScadrykCue supportCue = ScadrykEnemyHealth.WarningCueFor(0, false, false, 3);
        ScadrykAudio audio = GetComponent<ScadrykAudio>();
        if (laneCue == volleyCue || laneCue == areaCue || laneCue == supportCue || volleyCue == areaCue || volleyCue == supportCue || areaCue == supportCue ||
            ScadrykEnemyHealth.WarningCueFor(0, true, false, 0) != laneCue ||
            ScadrykEnemyHealth.WarningCueFor(1, true, false, 0) != volleyCue ||
            ScadrykEnemyHealth.WarningCueFor(2, true, false, 0) != areaCue ||
            ScadrykEnemyHealth.WarningCueFor(2, true, true, 0) != ScadrykCue.FinalBossWarning ||
            audio == null || !audio.HasCue(laneCue) || !audio.HasCue(volleyCue) || !audio.HasCue(areaCue) ||
            !audio.HasCue(supportCue) || !audio.HasCue(ScadrykCue.FinalBossWarning) || !audio.HasCue(ScadrykCue.PlayerStrike) ||
            ScadrykCue.PlayerStrike == ScadrykCue.Impact || ScadrykCue.PlayerStrike == ScadrykCue.Sword)
        { SmokeFail("Os sinais sonoros de aviso não diferenciam papéis e guardiões"); yield break; }
        Debug.Log("SCADRYK_SMOKE: sinais distinguem golpe confirmado de Nara, papéis inimigos e Arauto");
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
        if (ScadrykPlayerController.InputBufferCanStart(1.1f, 1.3f, 1.2f) ||
            !ScadrykPlayerController.InputBufferCanStart(1.21f, 1.3f, 1.2f) ||
            ScadrykPlayerController.InputBufferCanStart(1.31f, 1.3f, 1.2f) ||
            Mathf.Abs(ScadrykPlayerController.BasicAttackBufferWindow - 0.2f) > 0.001f ||
            ScadrykPlayerController.DashBufferCanStart(1.1f, 1.3f, 1.2f) ||
            !ScadrykPlayerController.DashBufferCanStart(1.21f, 1.3f, 1.2f) ||
            ScadrykPlayerController.DashBufferCanStart(1.31f, 1.3f, 1.2f) ||
            Mathf.Abs(ScadrykPlayerController.DashBufferWindow - 0.12f) > 0.001f)
        { SmokeFail("Os buffers curtos de ataque e esquiva iniciam fora da janela válida"); yield break; }
        Debug.Log("SCADRYK_SMOKE: ataque e esquiva respeitam recarga e janelas de buffer");
        if (!RebindingCanBeCancelled(-1, 2) || !RebindingCanBeCancelled(4, -1) || RebindingCanBeCancelled(-1, -1))
        { SmokeFail("Cancelar captura de controle não distingue remapeamento ativo do menu normal"); yield break; }
        Debug.Log("SCADRYK_SMOKE: Escape/Start cancela captura sem fechar opções");
        if (!ScadrykEnemyHealth.SupportPulseContains(Vector2.zero, new Vector2(2.49f, 0f)) ||
            ScadrykEnemyHealth.SupportPulseContains(Vector2.zero, new Vector2(2.51f, 0f)) ||
            Mathf.Abs(ScadrykEnemyHealth.SupportPulseDiameter - ScadrykEnemyHealth.SupportPulseRadius * 2f) > 0.001f)
        { SmokeFail("A cura do Tecelão ultrapassa o círculo verde anunciado"); yield break; }
        Debug.Log("SCADRYK_SMOKE: cura do Tecelão respeita o limite do círculo anunciado");
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
            FormatInputBinding(KeyCode.F, 4, true) != "LB")
        { SmokeFail("Pistas e HUD não refletem teclas e botões remapeados do dispositivo ativo"); yield break; }
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
        RequestNewGame(true);
        if (!confirmNewJourney) { SmokeFail("Uma jornada nova não pediu confirmação diante de progresso salvo"); yield break; }
        overlaySelection = 1;
        SubmitGamepadOverlay();
        if (confirmNewJourney || overlaySelection != 0) { SmokeFail("Cancelar a jornada nova não restaurou o menu sem apagar o checkpoint"); yield break; }
        Debug.Log("SCADRYK_SMOKE: nova jornada com save pode ser cancelada");
        roomActiveSeconds = 17f;
        roomDeaths = 0;
        roomPowerUses[0] = 2;
        RecordInputMode("gamepad");
        RecordPlayerDeath();
        if (roomDeaths != 1) { SmokeFail("A derrota não foi contabilizada no instante da morte"); yield break; }
        string[] deathCsvColumns = FormatSessionCsvRow(false).TrimEnd('\n').Split(',');
        if (!System.Guid.TryParseExact(sessionId, "N", out _) ||
            deathCsvColumns.Length != SessionCsvHeader.TrimEnd('\n').Split(',').Length || deathCsvColumns[0] != sessionId || deathCsvColumns[5] != "1")
        { SmokeFail("O CSV não registraria uma morte antes de terminar a animação"); yield break; }
        RestartCurrentRoom();
        if (roomActiveSeconds != 17f || roomDeaths != 1 || roomPowerUses[0] != 2 || sessionInputMode != "gamepad")
        { SmokeFail("Morte reiniciou a sala mas perdeu tempo, mortes, usos de poderes ou dispositivo da sessão"); yield break; }
        string[] csvHeaderColumns = SessionCsvHeader.TrimEnd('\n').Split(',');
        string[] csvRowColumns = FormatSessionCsvRow(true).TrimEnd('\n').Split(',');
        if (csvRowColumns.Length != csvHeaderColumns.Length || csvRowColumns[csvRowColumns.Length - 1] != "gamepad")
        { SmokeFail("A linha CSV da sessão não corresponde ao cabeçalho ou omite o dispositivo"); yield break; }
        Debug.Log("SCADRYK_SMOKE: telemetria preserva dados e dispositivo e serializa o CSV corretamente");
        float laneRadius = ScadrykEnemyHealth.PlayerCollisionRadius(player);
        float laneWidth = ScadrykEnemyHealth.RootLaneVisualWidth(0.86f, laneRadius);
        if (!ScadrykEnemyHealth.RootLaneHits(new Vector2(1.5f, laneWidth * 0.5f - 0.01f), Vector2.zero, new Vector2(3.3f, 0f), 0.86f, laneRadius) ||
            ScadrykEnemyHealth.RootLaneHits(new Vector2(1.5f, laneWidth * 0.5f + 0.01f), Vector2.zero, new Vector2(3.3f, 0f), 0.86f, laneRadius))
        { SmokeFail("A borda de dano do golpe de raízes não corresponde à largura anunciada"); yield break; }
        Debug.Log("SCADRYK_SMOKE: largura do aviso de raízes corresponde à área de dano");
        if (!ScadrykPlayerController.WithinVisualFootprint(new Vector2(3.09f, 0f), Vector2.zero, Vector2.right, 1.05f, new Vector2(4.1f, 2.8f)) ||
            ScadrykPlayerController.WithinVisualFootprint(new Vector2(3.11f, 0f), Vector2.zero, Vector2.right, 1.05f, new Vector2(4.1f, 2.8f)) ||
            !ScadrykPlayerController.WithinVisualFootprint(new Vector2(6.39f, 0f), Vector2.zero, Vector2.right, 2.7f, new Vector2(7.4f, 4.4f)) ||
            ScadrykPlayerController.WithinVisualFootprint(new Vector2(6.41f, 0f), Vector2.zero, Vector2.right, 2.7f, new Vector2(7.4f, 4.4f)) ||
            !ScadrykPlayerController.WithinVisualFootprint(new Vector2(1.5f, 1.39f), Vector2.zero, Vector2.right, 1.05f, new Vector2(4.1f, 2.8f)) ||
            ScadrykPlayerController.WithinVisualFootprint(new Vector2(1.5f, 1.41f), Vector2.zero, Vector2.right, 1.05f, new Vector2(4.1f, 2.8f)))
        { SmokeFail("As áreas de acerto dos poderes Crescente e Rompante divergem das dimensões dos efeitos visuais"); yield break; }
        Debug.Log("SCADRYK_SMOKE: áreas do Crescente e Rompante respeitam as dimensões anunciadas");
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
        Rect markerCell = new Rect(0f, 0f, 75f, 57f), largeMarkerCell = new Rect(0f, 0f, 90f, 67f);
        if (!MiniMapPositionMarkerFits(markerCell, new Vector2(-100f, -100f), 1f) ||
            !MiniMapPositionMarkerFits(markerCell, new Vector2(100f, 100f), 1f) ||
            !MiniMapPositionMarkerFits(largeMarkerCell, new Vector2(-100f, -100f), 1.5f) ||
            !MiniMapPositionMarkerFits(largeMarkerCell, new Vector2(100f, 100f), 1.5f))
        { SmokeFail("Os marcadores do jogador ou alvo escapam do cartão do minimapa"); yield break; }
        Debug.Log("SCADRYK_SMOKE: marcadores de jogador e alvo cabem no minimapa em limites e escala ampliada");
        if (ProgressHint(1, 0, 0) == ProgressHint(1, 0, 1) ||
            ProgressHint(3, 0, 0) == ProgressHint(3, 0, 1) ||
            ProgressHint(4, 0, 0) == ProgressHint(4, 0, 1) || ProgressHint(4, 0, 1) == ProgressHint(4, 0, 2) ||
            ProgressHint(6, 0, 0) == ProgressHint(6, 0, 1) ||
            ProgressHint(7, 0, 0) == ProgressHint(7, 0, 1) || ProgressHint(7, 0, 1) == ProgressHint(7, 0, 2) ||
            !ProgressHint(7, 1, 0).Contains("sino do recife"))
        { SmokeFail("As pistas graduais de exploração não diferenciam as salas e etapas"); yield break; }
        Debug.Log("SCADRYK_SMOKE: pistas graduais específicas e ordem da costa verificadas");
        Rect narrowHud = new Rect(14f, 14f, 325f + 0.5f * 130f, 190f + 0.5f * 86f);
        Rect narrowMap = MiniMapPanelRect(700f, 600f, 1.5f);
        Rect narrowLegend = MiniMapLegendRect(narrowMap, 1.5f);
        if (narrowHud.Overlaps(narrowMap) || narrowMap.xMin < 0f || narrowMap.yMin < 0f || narrowMap.xMax > 700f || narrowMap.yMax > 600f ||
            narrowLegend.yMin < narrowMap.yMin || narrowLegend.yMax > narrowMap.yMax)
        { SmokeFail("Minimapa sobrepõe o HUD ou sai da janela estreita com texto ampliado"); yield break; }
        Debug.Log("SCADRYK_SMOKE: minimapa e legenda cabem em janela estreita");
        if (!OptionsLayoutFits(600f) || !OptionsResetRowFits(400f))
        { SmokeFail("O menu de opções corta as teclas remapeáveis ou os controles de retorno a 600px de altura"); yield break; }
        Debug.Log("SCADRYK_SMOKE: menu de opções e restauração de comandos cabem em janela compacta");
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
        if (ScadrykEffects.FlashAlphaScale(true) >= 1f || ScadrykEffects.FlashDiameterScale(true) >= 1f ||
            ScadrykEffects.FlashExpansionScale(1f, true) >= ScadrykEffects.FlashExpansionScale(1f, false) ||
            ScadrykEffects.DeathBurstMoteCount(true, true) >= ScadrykEffects.DeathBurstMoteCount(true, false))
        { SmokeFail("Reduzir flashes não atenua explosões, expansões ou partículas de derrota"); yield break; }
        Debug.Log("SCADRYK_SMOKE: reduzir flashes atenua explosões e expansões sem remover a leitura de perigo");
        if (!OverlayLayoutFits(400f, 360f) || !PauseMapOverlayLayoutFits(400f, 360f))
        { SmokeFail("O menu inicial ou o menu de pausa com acesso ao mapa escapam de uma janela compacta"); yield break; }
        Debug.Log("SCADRYK_SMOKE: menus inicial e de pausa cabem em janela 400×360");
        if (!MemoryPanelFits(400f, 360f, 120f) || !MemoryPanelFits(700f, 600f, 90f))
        { SmokeFail("O texto ampliado dos vestígios é cortado ou cobre os comandos inferiores"); yield break; }
        Debug.Log("SCADRYK_SMOKE: painel de memória cresce com o texto sem cobrir status e controles");
        for (int effect = 0; effect < 40; effect++)
            ScadrykEffects.Burst(roomCenter + Vector2.up * (20f + effect), Color.clear, 0.05f);
        if (ScadrykEffects.SecondaryVisualCount > ScadrykEffects.SecondaryVisualLimit) { SmokeFail("O limite de efeitos secundários não foi respeitado"); yield break; }
        Debug.Log("SCADRYK_SMOKE: efeitos secundários respeitam o limite; avisos prioritários preservados");
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
                if (!SmokeRevealWorldSecret(requiredPower))
                { SmokeFail("O selo opcional não exigiu o poder correto ou não concedeu cura na sala " + (room + 1)); yield break; }
            }
            if (room < RoomsPerPhase)
            {
                ScadrykTerrainZone[] waterZones = FindObjectsByType<ScadrykTerrainZone>(FindObjectsSortMode.None);
                if (waterZones.Length == 0) { SmokeFail("Águas rasas sem zonas de terreno na sala " + (room + 1)); yield break; }
                foreach (ScadrykTerrainZone zone in waterZones)
                    if (zone.MovementScale >= 1f) { SmokeFail("Zona de água não reduz o movimento"); yield break; }
            }
            for (int previousRoom = 0; previousRoom < roomIndex; previousRoom++)
                if (!clearedRooms[previousRoom]) { SmokeFail("O mapa não marcou a sala anterior como concluída: " + previousRoom); yield break; }
            foreach (ScadrykEnemyHealth enemy in FindObjectsByType<ScadrykEnemyHealth>(FindObjectsSortMode.None))
                if (enemy != null && !enemy.IsFinalBoss) enemy.TakeDamage(99999f);
            if (!ActiveRoomHasWalkableRoute(roomCenter))
            { SmokeFail("A geometria sólida ativa bloqueou a rota caminhável da sala " + (room + 1)); yield break; }
            Debug.Log("SCADRYK_SMOKE: rota da sala " + (room + 1) + "/9 livre dos colisores sólidos do bioma");
            if (room == 8 && finalBoss != null)
            { SmokeFail("O Arauto apareceu antes de Nara concluir a memória do terceiro guardião"); yield break; }
            if (room == 8 && !statusMessage.Contains("última lembrança"))
            { SmokeFail("O intervalo seguro antes do Arauto não explicou a última lembrança"); yield break; }
            if (room == 8 && (worldSecretHintShown || !player.HasAbility(2) ||
                !WorldSecretHint(2).Contains("Rompante da Maré")))
            { SmokeFail("A pista do selo não foi atualizada após aprender Rompante com o guardião"); yield break; }
            if (room == 8 && !SmokeRevealWorldSecret(2))
            { SmokeFail("O selo opcional da costa não exigiu Rompante ou não concedeu cura após o guardião"); yield break; }
            if (room == 8)
            {
                float finalBossSpawnDeadline = Time.time + 8f;
                while (finalBoss == null && Time.time < finalBossSpawnDeadline) yield return null;
                if (finalBoss == null) { SmokeFail("Arauto não surgiu após o terceiro guardião"); yield break; }
                if (!TryGetMiniMapTarget(out Vector2 bossMapTarget) ||
                    Vector2.Distance(bossMapTarget, finalBoss.transform.position) > 0.001f)
                { SmokeFail("O minimapa não acompanhou o Arauto após a última memória"); yield break; }
                if (string.IsNullOrEmpty(InputBinding(5)) || !statusMessage.Contains(InputBinding(5)))
                { SmokeFail("O aviso do terceiro poder não mostrou o vínculo atual antes da luta final"); yield break; }
                var bossType = typeof(ScadrykEnemyHealth);
                var queuedStageField = bossType.GetField("queuedFinalStage", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var attackIndexField = bossType.GetField("finalAttackIndex", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var attackTargetField = bossType.GetField("attackTarget", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var beginBossAttack = bossType.GetMethod("BeginFinalBossAttack", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var resolveBossAttack = bossType.GetMethod("ResolveFinalBossAttack", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (queuedStageField == null || attackIndexField == null || attackTargetField == null || beginBossAttack == null || resolveBossAttack == null)
                { SmokeFail("Não foi possível preparar a checagem de fase do Arauto"); yield break; }
                foreach (ScadrykEnemyProjectile projectile in FindObjectsByType<ScadrykEnemyProjectile>(FindObjectsSortMode.None))
                    if (projectile != null) { projectile.gameObject.SetActive(false); Destroy(projectile.gameObject); }
                ClearSmokeAttackWarnings();
                attackIndexField.SetValue(finalBoss, 0);
                attackTargetField.SetValue(finalBoss, (Vector2)player.transform.position);
                Vector2 volleyForward = ((Vector2)player.transform.position - (Vector2)finalBoss.transform.position).normalized;
                beginBossAttack.Invoke(finalBoss, null);
                int announcedStage = (int)queuedStageField.GetValue(finalBoss);
                finalBoss.TakeDamage(400f);
                float volleyWidth = ScadrykEnemyProjectile.HitboxDiameter(0.58f);
                if (announcedStage != 1 || finalBoss.FinalStage != 2 || !SmokeAttackWarningsMatch((Vector2)finalBoss.transform.position, volleyForward, 7.2f, volleyWidth, 0.19f, 1))
                { SmokeFail("O teste não cruzou o limiar de fase durante o aviso do Arauto"); yield break; }
                resolveBossAttack.Invoke(finalBoss, null);
                int announcedVolleyCount = 0;
                bool volleyRangeMatchesWarning = true;
                foreach (ScadrykEnemyProjectile projectile in FindObjectsByType<ScadrykEnemyProjectile>(FindObjectsSortMode.None))
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
            }
            if (roomIndex == 2 && (player == null || !player.HasAbility(phaseIndex)))
            { SmokeFail("Poder não recebido após guardião da fase " + phaseIndex); yield break; }
            if (roomIndex == 2)
            {
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
                bool tacticalEffect = phaseIndex == 0 ? testHealth.IsStaggered : phaseIndex == 1 ? testHealth.IsExposed : testHealth.IsKnockedBack;
                if (testHealth.Health >= 1000f || !tacticalEffect)
                { SmokeFail("Poder da fase " + phaseIndex + " não causou dano ou não aplicou seu efeito tático"); yield break; }
                Debug.Log("SCADRYK_SMOKE: poder " + (phaseIndex + 1) + " causou dano e aplicou seu efeito tático");
                Destroy(testTarget);
            }
            if (room == 3 || room == 6)
            {
                if (witnessObject == null) { SmokeFail("Nima não apareceu na sala tranquila " + room); yield break; }
                string expectedMemory = room == 3 ? BasinWitnessLines[0] : CoastWitnessLines[0];
                TryInteract((Vector2)witnessObject.transform.position);
                if (memoryText != expectedMemory) { SmokeFail("Nima não respondeu à interação na sala " + room); yield break; }
                Debug.Log("SCADRYK_SMOKE: Nima respondeu na sala " + (room + 1) + "/9");
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
            if (enemiesRemaining != 0 || anchorsActivated != anchorsRequired || gateObject == null)
            { SmokeFail("Objetivo não abriu portal na sala " + room); yield break; }
            if (!TryGetMiniMapTarget(out Vector2 portalTarget) || Vector2.Distance(portalTarget, exitPosition) > 0.001f)
            { SmokeFail("O minimapa não marcou o portal aberto na sala " + (room + 1)); yield break; }
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
        if (statusMessage != VictoryStatus) { SmokeFail("O encerramento não confirmou o retorno das vozes e dos sinos"); yield break; }
        Debug.Log("SCADRYK_SMOKE: SUCCESS nove salas, três poderes, Arauto e vitória");
        Application.Quit(0);
    }

    private static void ClearSmokeAttackWarnings()
    {
        foreach (ScadrykTimedVisual visual in FindObjectsByType<ScadrykTimedVisual>(FindObjectsSortMode.None))
            if (visual != null && visual.gameObject.name == "Enemy attack warning")
            { visual.gameObject.SetActive(false); Destroy(visual.gameObject); }
    }

    private static int SmokeEffectCount(string objectName)
    {
        int count = 0;
        foreach (ScadrykTimedVisual visual in FindObjectsByType<ScadrykTimedVisual>(FindObjectsSortMode.None))
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
            smokeProjectileCoverPassed = solidStopsProjectile && FindSmokeProjectile(projectileName) != null;
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
        foreach (ScadrykEnemyProjectile projectile in FindObjectsByType<ScadrykEnemyProjectile>(FindObjectsSortMode.None))
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
        bool grantedReward = worldSecret == null && Mathf.Abs(player.Health - player.MaxHealth) <= 0.01f;
        bool echoVisualShown = SmokeEffectCount("Liberated echo memory") > echoCountBeforeReveal;
        bool echoStoryShown = memoryText == WorldSecretMemory(requiredPower);
        playerHealthField.SetValue(player, originalHealth);
        playerFacingField.SetValue(player, originalFacing);
        if (!rejectedInvalidUses || !grantedReward || !echoVisualShown || !echoStoryShown) return false;
        Debug.Log("SCADRYK_SMOKE: selo opcional exige o poder correto e restaura vida (poder " + (requiredPower + 1) + ")");
        return true;
    }

    private static bool SmokeAttackWarningsMatch(Vector2 origin, Vector2 forward, float length, float width, float angleStep, int halfCount)
    {
        int count = 0;
        bool[] seenAngles = new bool[halfCount * 2 + 1];
        foreach (ScadrykTimedVisual visual in FindObjectsByType<ScadrykTimedVisual>(FindObjectsSortMode.None))
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
        foreach (ScadrykEnemyProjectile projectile in FindObjectsByType<ScadrykEnemyProjectile>(FindObjectsSortMode.None))
            if (projectile != null && projectile.gameObject.activeInHierarchy && projectile.gameObject.name == projectileName) count++;
        return count;
    }

    private static bool SmokeProjectileRangeMatches(string projectileName, float expectedRange)
    {
        int count = 0;
        foreach (ScadrykEnemyProjectile projectile in FindObjectsByType<ScadrykEnemyProjectile>(FindObjectsSortMode.None))
        {
            if (projectile == null || !projectile.gameObject.activeInHierarchy || projectile.gameObject.name != projectileName) continue;
            count++;
            if (Mathf.Abs(projectile.TravelDistanceLimit - expectedRange) > 0.01f) return false;
        }
        return count > 0;
    }

    private static void ClearSmokeProjectiles()
    {
        foreach (ScadrykEnemyProjectile projectile in FindObjectsByType<ScadrykEnemyProjectile>(FindObjectsSortMode.None))
            if (projectile != null) { projectile.gameObject.SetActive(false); Destroy(projectile.gameObject); }
    }

    private static void SmokeFail(string message)
    {
        Debug.LogError("SCADRYK_SMOKE: FAIL " + message);
        Application.Quit(1);
    }
#endif

    private void OnGUI()
    {
        EnsureStyles();
        if (showOpeningStory) { DrawOpeningStory(); return; }
        if (showWorldMap) { DrawWorldMap(); return; }
        if (showOptions) { DrawOptions(); return; }
        if (showTitle || isPaused || victory) { DrawOverlay(); return; }
        DrawHud(); DrawMiniMap(); DrawOffscreenTargetHint();
        DrawWitnessPrompt();
        if (phaseGuardian != null) DrawPhaseGuardianTitle();
        if (finalBoss != null) DrawFinalBossBar();
        if (Time.time < statusUntil) GUI.Label(new Rect(15, Screen.height - 64, Screen.width - 30, 44), statusMessage, bodyStyle);
        if (Time.time < memoryUntil && !transitioning)
        {
            float width = Mathf.Max(0f, Mathf.Min(740f, Screen.width - 36f));
            float bodyHeight = Mathf.Max(62f, memoryStyle.CalcHeight(new GUIContent(memoryText), Mathf.Max(32f, width - 36f)));
            Rect panel = MemoryPanelRect(Screen.width, Screen.height, bodyHeight);
            GUI.color = new Color(0.025f, 0.04f, 0.045f, 0.94f); GUI.DrawTexture(panel, Texture2D.whiteTexture); GUI.color = Color.white;
            GUI.Label(new Rect(panel.x + 18f, panel.y + 10f, panel.width - 36f, 24f), "VESTÍGIO ESCUTADO", titleStyle);
            GUI.Label(MemoryTextRect(panel), memoryText, memoryStyle);
        }
        string controlHint = Screen.width < 840f
            ? "M/L3 mapa · Esc/Start pausa · " + InputBinding(0) + " atacar · " + InputBinding(1) + " esquivar"
            : "Mover: teclas/controle  ·  Mouse/analógico mirar  ·  " + InputBinding(0) + " atacar  ·  " + InputBinding(1) + " esquivar  ·  " + InputBinding(2) + " pulso  ·  " + InputBinding(6) + " interagir  ·  M/L3 mapa  ·  Esc pausa";
        Rect controlHintRect = new Rect(14, Screen.height - 34, Screen.width - 28, 30);
        controlHintStyle.fontSize = smallStyle.fontSize;
        float measuredControlHintWidth = controlHintStyle.CalcSize(new GUIContent(controlHint)).x;
        if (measuredControlHintWidth > controlHintRect.width && measuredControlHintWidth > 0f)
            controlHintStyle.fontSize = Mathf.Max(9, Mathf.FloorToInt(controlHintStyle.fontSize * controlHintRect.width / measuredControlHintWidth));
        GUI.Label(controlHintRect, controlHint, controlHintStyle);
        if (transitionFade > 0f)
        {
            GUI.color = new Color(0f, 0f, 0f, transitionFade);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            if (transitioning && transitionFade > 0.35f)
                GUI.Label(new Rect(0, Screen.height / 2f - 20, Screen.width, 40), "ATRAVESSANDO O LIMIAR", centeredTitleStyle);
        }
    }

    private void DrawHud()
    {
        float extra = ScadrykSettings.TextScale - 1f;
        float textWidth = 300f + extra * 130f, trackWidth = 275f + extra * 120f;
        GUI.color = new Color(0.035f, 0.05f, 0.06f, 0.92f); GUI.Box(new Rect(14, 14, 325f + extra * 130f, 190f + extra * 86f), GUIContent.none); GUI.color = Color.white;
        GUI.Label(new Rect(28, 22, textWidth, 25f + extra * 15f), PhaseNames[phaseIndex] + "  ·  Fase " + (phaseIndex + 1) + "/3", titleStyle);
        GUI.Label(new Rect(28, 51f + extra * 8f, textWidth, 23f + extra * 12f), roomNames[phaseIndex][roomIndex] + "  ·  Sala " + (roomIndex + 1) + "/3", bodyStyle);
        GUI.color = new Color(0.18f, 0.19f, 0.2f); GUI.DrawTexture(new Rect(28, 82f + extra * 18f, trackWidth, 12), Texture2D.whiteTexture);
        GUI.color = new Color(0.79f, 0.25f, 0.22f); GUI.DrawTexture(new Rect(28, 82f + extra * 18f, trackWidth * (player != null ? player.Health / player.MaxHealth : 1f), 12), Texture2D.whiteTexture); GUI.color = Color.white;
        GUI.Label(new Rect(28, 98f + extra * 23f, textWidth, 19f + extra * 10f), "Ecos: " + enemiesRemaining + "    Esquiva: " + Cooldown(player != null ? player.DashCooldownLeft : 0) + "    Pulso: " + Cooldown(player != null ? player.ShockwaveCooldownLeft : 0), smallStyle);
        string[] abilities = { "Crescente Verdejante", "Estouro de Sal", "Rompante da Maré" };
        for (int i = 0; i < abilities.Length; i++)
        {
            bool learned = player != null && player.HasAbility(i);
            string state = !learned ? "bloqueado" : player.AbilityCooldownLeft(i) <= 0.02f ? "pronto" : Cooldown(player.AbilityCooldownLeft(i));
            GUI.color = learned ? new Color(0.72f, 0.9f, 0.79f) : new Color(0.48f, 0.53f, 0.51f);
            GUI.Label(new Rect(28, 116f + extra * 37f + i * (15f + extra * 9f), textWidth, 16f + extra * 9f), InputBinding(i + 3) + "  " + abilities[i] + "  ·  " + state, smallStyle);
        }
        GUI.color = Color.white;
        string objective = ActiveObjectiveText();
        GUI.Label(new Rect(28, 164f + extra * 64f, textWidth, 28f + extra * 10f), "Objetivo: " + objective, smallStyle);
    }

    private void DrawWitnessPrompt()
    {
        if (witnessObject == null || player == null || Vector2.Distance(player.transform.position, witnessObject.transform.position) > 2.2f) return;
        Camera sceneCamera = Camera.main;
        if (sceneCamera == null) return;
        Vector3 screen = sceneCamera.WorldToScreenPoint(witnessObject.transform.position + Vector3.up * 1.35f);
        if (screen.z <= 0f) return;
        Rect prompt = new Rect(screen.x - 130f, Screen.height - screen.y - 16f, 260f, 30f);
        GUI.color = new Color(0.025f, 0.04f, 0.045f, 0.9f); GUI.DrawTexture(prompt, Texture2D.whiteTexture); GUI.color = Color.white;
        GUI.Label(prompt, WitnessPromptText(), centeredBodyStyle);
    }

    private string WitnessPromptText() => InputBinding(6) + "  ·  FALAR COM NIMA";

    private void DrawPhaseGuardianTitle()
    {
        Camera sceneCamera = Camera.main;
        if (sceneCamera == null || phaseGuardian == null) return;
        Vector3 screen = sceneCamera.WorldToScreenPoint(phaseGuardian.transform.position + Vector3.up * 2f);
        if (screen.z <= 0f || screen.x < 8f || screen.x > Screen.width - 8f || screen.y < 8f || screen.y > Screen.height - 8f) return;
        float width = Mathf.Min(240f, Screen.width - 16f);
        Rect panel = new Rect(Mathf.Clamp(screen.x - width * 0.5f, 8f, Screen.width - width - 8f),
            Mathf.Clamp(Screen.height - screen.y - 28f, 8f, Screen.height - 36f), width, 28f);
        GUI.color = new Color(0.025f, 0.035f, 0.03f, 0.92f);
        GUI.DrawTexture(panel, Texture2D.whiteTexture);
        GUI.color = PhaseAccentColors[phaseIndex];
        GUI.DrawTexture(new Rect(panel.x, panel.y, panel.width, 2f), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(panel, GuardianNames[phaseIndex], centeredBodyStyle);
    }

    private void DrawFinalBossBar()
    {
        float width = Mathf.Min(600f, Mathf.Max(250f, Screen.width - 660f));
        float x = (Screen.width - width) * 0.5f;
        Rect panel = new Rect(x, 13f, width, 57f);
        GUI.color = new Color(0.025f, 0.008f, 0.035f, 0.96f); GUI.DrawTexture(panel, Texture2D.whiteTexture);
        GUI.color = new Color(0.78f, 0.19f, 0.62f, 0.95f); GUI.DrawTexture(new Rect(panel.x, panel.y, panel.width, 2f), Texture2D.whiteTexture);
        GUI.color = new Color(0.94f, 0.63f, 0.31f, 0.9f); GUI.DrawTexture(new Rect(panel.x, panel.yMax - 2f, panel.width, 2f), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(panel.x + 10f, panel.y + 3f, panel.width - 20f, 20f), "O ARAUTO DO VAZIO  ·  FASE " + finalBoss.FinalStage + "/3", finalBossLabelStyle);
        Rect track = new Rect(panel.x + 12f, panel.y + 31f, panel.width - 24f, 15f);
        GUI.color = new Color(0.17f, 0.06f, 0.15f, 1f); GUI.DrawTexture(track, Texture2D.whiteTexture);
        GUI.color = new Color(0.91f, 0.22f, 0.58f, 1f); GUI.DrawTexture(new Rect(track.x, track.y, track.width * finalBoss.Health01, track.height), Texture2D.whiteTexture);
        GUI.color = new Color(1f, 0.88f, 0.67f, 0.9f);
        GUI.DrawTexture(new Rect(track.x + track.width * 0.35f - 1f, track.y, 2f, track.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(track.x + track.width * 0.7f - 1f, track.y, 2f, track.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    private void DrawMiniMap()
    {
        float extra = ScadrykSettings.TextScale - 1f;
        float width = 286f + extra * 120f;
        Rect panel = MiniMapPanelRect(Screen.width, Screen.height, ScadrykSettings.TextScale);
        GUI.color = new Color(0.035f, 0.05f, 0.06f, 0.9f); GUI.Box(panel, GUIContent.none); GUI.color = Color.white;
        GUI.Label(new Rect(panel.x + 13, panel.y + 8, width - 26f, 21f + extra * 8f), "MAPA · " + PhaseNames[phaseIndex], smallStyle);
        float mapX = panel.x + 15, mapY = panel.y + 43 + extra * 5f, roomW = 75f + extra * 30f, roomH = 57f + extra * 10f, gap = 8;
        for (int i = 0; i < RoomsPerPhase; i++)
        {
            Rect cell = new Rect(mapX + i * (roomW + gap), mapY, roomW, roomH);
            GUI.color = MiniMapRoomColor(i == roomIndex, clearedRooms[i]);
            GUI.Box(cell, GUIContent.none); GUI.color = new Color(0.75f, 0.74f, 0.67f);
            GUI.Label(new Rect(cell.x + 3, cell.y + 2, roomW - 6, 18), (i + 1) + ". " + MapRoomLabel(phaseIndex, i), smallStyle);
            if (i == roomIndex && gateObject != null)
            {
                GUI.color = PhaseAccentColors[phaseIndex];
                GUI.Label(MiniMapPortalLabelRect(cell, ScadrykSettings.TextScale), "PORTAL", centeredBodyStyle);
                GUI.color = Color.white;
            }
            if (i < RoomsPerPhase - 1) GUI.Label(new Rect(cell.xMax, cell.y + 20, gap + 2, 20), "›", bodyStyle);
        }
        Rect currentCell = new Rect(mapX + roomIndex * (roomW + gap), mapY, roomW, roomH);
        if (TryGetMiniMapTarget(out Vector2 target))
        {
            Rect targetMarker = MiniMapPositionMarkerRect(currentCell, target - roomCenter, ScadrykSettings.TextScale);
            GUI.color = Color.white;
            DrawMiniMapDiamond(new Rect(targetMarker.x - 1f, targetMarker.y - 1f, targetMarker.width + 2f, targetMarker.height + 2f));
            GUI.color = new Color(0.28f, 0.88f, 1f, 1f);
            DrawMiniMapDiamond(targetMarker);
        }
        if (player != null)
        {
            Vector2 relative = (Vector2)player.transform.position - roomCenter;
            Rect dot = MiniMapPositionMarkerRect(currentCell, relative, ScadrykSettings.TextScale);
            GUI.color = new Color(1f, 0.82f, 0.38f); GUI.DrawTexture(dot, Texture2D.whiteTexture);
        }
        Rect legend = MiniMapLegendRect(panel, ScadrykSettings.TextScale);
        float columnWidth = legend.width / 4f;
        DrawMiniMapLegendItem(legend.x, legend.y, columnWidth, MiniMapRoomColor(true, false), "Você");
        DrawMiniMapLegendItem(legend.x + columnWidth, legend.y, columnWidth, MiniMapRoomColor(false, true), "Feita");
        DrawMiniMapLegendItem(legend.x + columnWidth * 2f, legend.y, columnWidth, MiniMapRoomColor(false, false), "Futura");
        DrawMiniMapLegendItem(legend.x + columnWidth * 3f, legend.y, columnWidth, new Color(0.28f, 0.88f, 1f), "Alvo", true);
        GUI.color = Color.white;
    }

    private void DrawOffscreenTargetHint()
    {
        if (player == null || Time.time < statusUntil || Time.time < memoryUntil || !TryGetMiniMapTarget(out Vector2 target)) return;
        Camera sceneCamera = Camera.main;
        if (sceneCamera == null) return;
        Vector3 viewport = sceneCamera.WorldToViewportPoint(target);
        if (viewport.z <= 0f) return;
        string arrow = OffscreenTargetArrow(new Vector2(viewport.x, viewport.y));
        if (arrow == null) return;

        float width = Mathf.Min(280f, Screen.width - 32f);
        Rect hint = new Rect((Screen.width - width) * 0.5f, Screen.height - 74f, width, 28f);
        GUI.color = new Color(0.025f, 0.04f, 0.045f, 0.9f);
        GUI.DrawTexture(hint, Texture2D.whiteTexture);
        GUI.color = new Color(0.28f, 0.88f, 1f, 1f);
        GUI.Label(hint, arrow + "  ALVO FORA DA TELA", offscreenTargetStyle);
        GUI.color = Color.white;
    }

    internal static string OffscreenTargetArrow(Vector2 viewportPosition)
    {
        if (viewportPosition.x >= 0f && viewportPosition.x <= 1f && viewportPosition.y >= 0f && viewportPosition.y <= 1f) return null;
        Vector2 offset = viewportPosition - new Vector2(0.5f, 0.5f);
        if (Mathf.Abs(offset.x) > Mathf.Abs(offset.y)) return offset.x >= 0f ? "→" : "←";
        return offset.y >= 0f ? "↑" : "↓";
    }

    internal static Rect MiniMapPositionMarkerRect(Rect roomCell, Vector2 roomOffset, float textScale)
    {
        float scale = Mathf.Clamp(textScale, 1f, 1.5f);
        float dx = Mathf.Clamp(roomOffset.x / 22f, -0.42f, 0.42f) * (roomCell.width - 10f * scale);
        float dy = Mathf.Clamp(roomOffset.y / 15f, -0.35f, 0.35f) * (roomCell.height - 12f * scale);
        float size = 7f * scale;
        return new Rect(roomCell.center.x + dx - size * 0.5f, roomCell.center.y - dy - size * 0.5f, size, size);
    }

    internal static bool MiniMapPositionMarkerFits(Rect roomCell, Vector2 roomOffset, float textScale)
    {
        Rect marker = MiniMapPositionMarkerRect(roomCell, roomOffset, textScale);
        return marker.xMin >= roomCell.xMin && marker.xMax <= roomCell.xMax &&
            marker.yMin >= roomCell.yMin && marker.yMax <= roomCell.yMax;
    }

    private bool TryGetMiniMapTarget(out Vector2 target)
    {
        ScadrykEnemyHealth nearestEnemy = null;
        float nearestDistance = float.MaxValue;
        if (player != null)
        {
            for (int i = activeEnemies.Count - 1; i >= 0; i--)
            {
                ScadrykEnemyHealth enemy = activeEnemies[i];
                if (enemy == null || enemy.Health <= 0f) { activeEnemies.RemoveAt(i); continue; }
                float distance = ((Vector2)enemy.transform.position - (Vector2)player.transform.position).sqrMagnitude;
                if (distance < nearestDistance) { nearestDistance = distance; nearestEnemy = enemy; }
            }
        }
        if (nearestEnemy != null) { target = nearestEnemy.transform.position; return true; }
        for (int i = 0; i < anchorsRequired; i++)
            if (roomAnchors[i] != null) { target = roomAnchors[i].transform.position; return true; }
        if (gateObject != null) { target = exitPosition; return true; }
        target = Vector2.zero;
        return false;
    }

    private void DrawMiniMapLegendItem(float x, float y, float width, Color swatch, string label, bool diamond = false)
    {
        float scale = ScadrykSettings.TextScale;
        GUI.color = swatch;
        Rect icon = new Rect(x, y + 4f * scale, 9f * scale, 9f * scale);
        if (diamond) DrawMiniMapDiamond(icon);
        else GUI.DrawTexture(icon, Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(x + 13f * scale, y, width - 13f * scale, 18f * scale), label, smallStyle);
    }

    private static void DrawMiniMapDiamond(Rect rect)
    {
        float unitX = rect.width / 5f, unitY = rect.height / 5f;
        GUI.DrawTexture(new Rect(rect.x + unitX * 2f, rect.y, unitX, unitY), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x + unitX, rect.y + unitY, unitX * 3f, unitY), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y + unitY * 2f, rect.width, unitY), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x + unitX, rect.y + unitY * 3f, unitX * 3f, unitY), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x + unitX * 2f, rect.y + unitY * 4f, unitX, unitY), Texture2D.whiteTexture);
    }

    private static Color MiniMapRoomColor(bool current, bool cleared) =>
        current ? new Color(0.19f, 0.36f, 0.35f) : cleared ? new Color(0.16f, 0.25f, 0.22f) : new Color(0.12f, 0.15f, 0.16f);

    private static Rect MiniMapLegendRect(Rect panel, float textScale)
    {
        float scale = Mathf.Clamp(textScale, 1f, 1.5f);
        float height = 22f * scale;
        return new Rect(panel.x + 15f, panel.yMax - height, panel.width - 30f, height);
    }

    private static Rect MiniMapPortalLabelRect(Rect roomCell, float textScale)
    {
        float scale = Mathf.Clamp(textScale, 1f, 1.5f);
        float inset = 3f * scale;
        float height = 18f * scale;
        return new Rect(roomCell.x + inset, roomCell.yMax - height - inset, roomCell.width - inset * 2f, height);
    }

    private static bool MiniMapPortalLabelFits(Rect roomCell, float textScale)
    {
        Rect label = MiniMapPortalLabelRect(roomCell, textScale);
        return label.xMin >= roomCell.xMin && label.xMax <= roomCell.xMax &&
            label.yMin >= roomCell.yMin && label.yMax <= roomCell.yMax;
    }

    private static Rect MiniMapPanelRect(float screenWidth, float screenHeight, float textScale)
    {
        float extra = Mathf.Clamp(textScale, 1f, 1.5f) - 1f;
        float width = 286f + extra * 120f;
        float height = 126f + extra * 24f;
        float hudWidth = 325f + extra * 130f;
        float hudHeight = 190f + extra * 86f;
        float x = screenWidth - width - 16f;
        float y = 14f;
        if (x < 14f + hudWidth + 16f)
        {
            y = 14f + hudHeight + 8f;
            if (y + height > screenHeight - 8f) y = Mathf.Max(8f, screenHeight - height - 8f);
        }
        return new Rect(x, y, width, height);
    }

    private static string MapRoomLabel(int phase, int room) => MapRoomLabels[phase][room];
    private static string Cooldown(float value) => value <= 0.02f ? "pronto" : value.ToString("0.0") + "s";

    private void DrawOverlay()
    {
        Rect screenRect = new Rect(0, 0, Screen.width, Screen.height);
        if (showTitle && titleBackdropTexture != null)
        {
            GUI.color = Color.white;
            GUI.DrawTexture(screenRect, titleBackdropTexture, ScaleMode.ScaleAndCrop);
            GUI.color = new Color(0.01f, 0.02f, 0.025f, 0.44f);
            GUI.DrawTexture(screenRect, Texture2D.whiteTexture);
        }
        else
        {
            GUI.color = new Color(0.015f, 0.02f, 0.025f, 0.91f);
            GUI.DrawTexture(screenRect, Texture2D.whiteTexture);
        }
        GUI.color = Color.white;
        string heading = confirmNewJourney ? "NOVA JORNADA?" : victory ? "AS VOZES VOLTAM" : showTitle ? "SCADRYK" : "PAUSADO";
        float center = Screen.width * 0.5f;
        bool compact = Screen.height < 520f;
        bool pauseMapMenu = !showTitle && !victory && !confirmNewJourney;
        float top = compact ? 18f : Screen.height * 0.29f;
        float titleWidth = Mathf.Min(600f, Screen.width - 32f);
        float messageWidth = Mathf.Min(680f, Screen.width - 32f);
        float buttonWidth = Mathf.Min(230f, Screen.width - 32f);
        float titleHeight = compact ? 42f : 52f;
        float messageTop = top + (compact ? 42f : 48f);
        float messageHeight = compact ? (pauseMapMenu ? 68f : 86f) : 62f;
        float buttonY = top + (compact ? (pauseMapMenu ? 118f : 136f) : 132f);
        float buttonHeight = compact ? (pauseMapMenu ? 42f : 40f) : 44f;
        float buttonStep = compact ? (pauseMapMenu ? 44f : 48f) : 54f;
        float footerWidth = Mathf.Min(600f, Screen.width - 32f), footerHeight = compact ? 28f : 35f;
        float footerTop = Screen.height - (compact ? 42f : 58f);
        GUI.Label(new Rect(center - titleWidth * 0.5f, top, titleWidth, titleHeight), heading, centeredTitleStyle);
        string message = confirmNewJourney ? "Uma jornada nova apaga o progresso salvo. Quer substituir seu checkpoint?" :
            victory ? "Nara devolveu as vozes aos três lugares. A verdade da onda pode ser lembrada sem manter ninguém preso. O limiar agora é uma passagem, não uma prisão." :
            showTitle ? "Nara, a Ouvinte, segue os Ecos até a origem do silêncio." : "A jornada pode esperar por você.";
        GUI.Label(new Rect(center - messageWidth * 0.5f, messageTop, messageWidth, messageHeight), message, centeredBodyStyle);
        if (confirmNewJourney)
        {
            if (OverlayButton(new Rect(center - buttonWidth * 0.5f, buttonY, buttonWidth, buttonHeight), "Apagar e começar", 0)) StartNewGame();
            if (OverlayButton(new Rect(center - buttonWidth * 0.5f, buttonY + buttonStep, buttonWidth, buttonHeight), "Manter progresso", 1)) { confirmNewJourney = false; overlaySelection = 0; }
            GUI.Label(new Rect(center - footerWidth * 0.5f, footerTop, footerWidth, footerHeight), "Esc, B ou Start cancela", centeredBodyStyle);
            return;
        }
        if (showTitle)
        {
            if (!victory && OverlayButton(new Rect(center - buttonWidth * 0.5f, buttonY, buttonWidth, buttonHeight), HasCheckpoint("Scadryk.") ? "Continuar jornada" : "Começar jornada", 0)) BeginJourney();
            if (OverlayButton(new Rect(center - buttonWidth * 0.5f, buttonY + buttonStep, buttonWidth, buttonHeight), "Nova jornada", 1)) RequestNewGame();
            if (!victory && OverlayButton(new Rect(center - buttonWidth * 0.5f, buttonY + buttonStep * 2f, buttonWidth, buttonHeight), "Opções e controles", 2)) { showOptions = true; optionsTab = optionsSelection = 0; }
        }
        else if (victory)
        {
            if (OverlayButton(new Rect(center - buttonWidth * 0.5f, buttonY, buttonWidth, buttonHeight), "Nova jornada", 1)) StartNewGame();
        }
        else
        {
            if (OverlayButton(new Rect(center - buttonWidth * 0.5f, buttonY, buttonWidth, buttonHeight), "Continuar", 0)) isPaused = false;
            if (OverlayButton(new Rect(center - buttonWidth * 0.5f, buttonY + buttonStep, buttonWidth, buttonHeight), "Mapa da jornada", 1))
            { isPaused = false; showWorldMap = true; ApplyPauseTimeScale(); }
            if (OverlayButton(new Rect(center - buttonWidth * 0.5f, buttonY + buttonStep * 2f, buttonWidth, buttonHeight), "Nova jornada", 2)) RequestNewGame();
            if (OverlayButton(new Rect(center - buttonWidth * 0.5f, buttonY + buttonStep * 3f, buttonWidth, buttonHeight), "Opções e controles", 3)) { showOptions = true; optionsTab = optionsSelection = 0; }
        }
        GUI.Label(new Rect(center - footerWidth * 0.5f, footerTop, footerWidth, footerHeight), "Três lugares · nove salas · memórias a libertar", centeredBodyStyle);
    }

    private void DrawOpeningStory()
    {
        Rect screen = new Rect(0f, 0f, Screen.width, Screen.height);
        if (titleBackdropTexture != null) GUI.DrawTexture(screen, titleBackdropTexture, ScaleMode.ScaleAndCrop);
        GUI.color = new Color(0.008f, 0.015f, 0.02f, 0.76f);
        GUI.DrawTexture(screen, Texture2D.whiteTexture);
        float width = Mathf.Max(0f, Mathf.Min(740f, Screen.width - 32f));
        float height = Mathf.Max(0f, Mathf.Min(310f, Screen.height - 24f));
        Rect panel = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
        GUI.color = new Color(0.025f, 0.045f, 0.05f, 0.97f);
        GUI.DrawTexture(panel, Texture2D.whiteTexture);
        GUI.color = new Color(0.74f, 0.63f, 0.34f, 1f);
        GUI.DrawTexture(new Rect(panel.x, panel.y, panel.width, 3f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(panel.x, panel.yMax - 3f, panel.width, 3f), Texture2D.whiteTexture);
        GUI.color = Color.white;
        float titleWidth = Mathf.Max(0f, panel.width - 36f);
        openingTitleStyle.fontSize = centeredTitleStyle.fontSize;
        float measuredTitleWidth = openingTitleStyle.CalcSize(OpeningTitleContent).x;
        if (measuredTitleWidth > titleWidth && measuredTitleWidth > 0f)
            openingTitleStyle.fontSize = Mathf.Max(12, Mathf.FloorToInt(openingTitleStyle.fontSize * titleWidth / measuredTitleWidth));
        GUI.Label(new Rect(panel.x + 18f, panel.y + 16f, titleWidth, 40f), OpeningTitleContent, openingTitleStyle);
        Rect storyBody = new Rect(panel.x + 28f, panel.y + 68f, panel.width - 56f, Mathf.Max(48f, panel.height - 150f));
        openingBodyStyle.fontSize = centeredBodyStyle.fontSize;
        float measuredBodyHeight = openingBodyStyle.CalcHeight(OpeningMemoryContent, storyBody.width);
        if (measuredBodyHeight > storyBody.height && measuredBodyHeight > 0f)
            openingBodyStyle.fontSize = Mathf.Max(12, Mathf.FloorToInt(openingBodyStyle.fontSize * storyBody.height / measuredBodyHeight));
        GUI.Label(storyBody, OpeningMemoryContent, openingBodyStyle);
        Rect begin = new Rect(panel.center.x - Mathf.Min(150f, panel.width * 0.38f), panel.yMax - 68f,
            Mathf.Min(300f, panel.width * 0.76f), 38f);
        if (GUI.Button(begin, "COMEÇAR A JORNADA")) DismissOpeningStory();
        GUI.Label(new Rect(panel.x + 12f, panel.yMax - 27f, panel.width - 24f, 20f),
            "Enter · Espaço · A · " + InputBinding(6) + " para continuar", smallStyle);
        GUI.color = Color.white;
    }

    private void DrawWorldMap()
    {
        Rect screen = new Rect(0f, 0f, Screen.width, Screen.height);
        GUI.color = new Color(0.008f, 0.015f, 0.02f, 0.9f);
        GUI.DrawTexture(screen, Texture2D.whiteTexture);
        Rect panel = WorldMapPanelRect(Screen.width, Screen.height);
        GUI.color = new Color(0.025f, 0.045f, 0.05f, 0.98f);
        GUI.DrawTexture(panel, Texture2D.whiteTexture);
        GUI.color = new Color(0.74f, 0.63f, 0.34f, 1f);
        GUI.DrawTexture(new Rect(panel.x, panel.y, panel.width, 3f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(panel.x, panel.yMax - 3f, panel.width, 3f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float titleWidth = Mathf.Max(0f, panel.width - 150f);
        openingTitleStyle.fontSize = centeredTitleStyle.fontSize;
        float measuredTitleWidth = openingTitleStyle.CalcSize(WorldMapTitleContent).x;
        if (measuredTitleWidth > titleWidth && measuredTitleWidth > 0f)
            openingTitleStyle.fontSize = Mathf.Max(12, Mathf.FloorToInt(openingTitleStyle.fontSize * titleWidth / measuredTitleWidth));
        GUI.Label(new Rect(panel.center.x - titleWidth * 0.5f, panel.y + 4f, titleWidth, 40f), WorldMapTitleContent, openingTitleStyle);
        worldMapInstructionStyle.fontSize = Mathf.Min(smallStyle.fontSize, 12);
        GUI.Label(new Rect(panel.x + 16f, panel.y + 45f, panel.width - 32f, 17f),
            "TRÊS BIOMAS · NOVE SALAS · O CAMINHO DE NARA", worldMapInstructionStyle);
        if (GUI.Button(new Rect(panel.xMax - 92f, panel.y + 14f, 76f, 28f), "FECHAR")) CloseWorldMap();

        float rowHeight = WorldMapRowHeight(panel.height);
        float contentX = panel.x + 16f;
        float contentWidth = panel.width - 32f;
        float cardGap = 8f;
        float cardWidth = (contentWidth - cardGap * 2f) / 3f;
        for (int phase = 0; phase < PhaseNames.Length; phase++)
        {
            float rowY = panel.y + 62f + phase * rowHeight;
            GUI.color = PhaseAccentColors[phase];
            GUI.Label(new Rect(contentX, rowY, contentWidth, 18f), PhaseNames[phase], smallStyle);
            GUI.color = Color.white;
            for (int room = 0; room < RoomsPerPhase; room++)
            {
                int state = WorldMapRoomStatus(phase, room, phaseIndex, roomIndex, clearedRooms);
                Rect card = new Rect(contentX + room * (cardWidth + cardGap), rowY + 19f, cardWidth, rowHeight - 21f);
                GUI.color = WorldMapRoomColor(state);
                GUI.DrawTexture(card, Texture2D.whiteTexture);
                GUI.color = state == 1 ? new Color(0.45f, 0.95f, 0.9f) : state == 2 ? new Color(0.42f, 0.78f, 0.53f) : new Color(0.45f, 0.49f, 0.48f);
                GUI.DrawTexture(new Rect(card.x, card.y, card.width, 2f), Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(new Rect(card.x + 3f, card.y + 3f, card.width - 6f, card.height * 0.5f),
                    (room + 1) + ". " + MapRoomLabel(phase, room), worldMapRoomStyle);
                string stateLabel = state == 1 ? "VOCÊ ESTÁ AQUI" : state == 2 ? "CONCLUÍDA" : "ADIANTE";
                GUI.color = state == 1 ? new Color(0.55f, 1f, 0.94f) : state == 2 ? new Color(0.62f, 0.9f, 0.68f) : new Color(0.7f, 0.73f, 0.71f);
                GUI.Label(new Rect(card.x + 2f, card.yMax - 18f, card.width - 4f, 15f), stateLabel, worldMapStateStyle);
                GUI.color = Color.white;
            }
        }

        string currentObjective = "Objetivo: " + ActiveObjectiveText();
        GUIContent objectiveContent = new GUIContent(currentObjective);
        float objectiveWidth = panel.width - 32f;
        worldMapObjectiveStyle.fontSize = Mathf.RoundToInt(11f * Mathf.Clamp(ScadrykSettings.TextScale, 1f, 1.5f));
        float measuredObjectiveWidth = worldMapObjectiveStyle.CalcSize(objectiveContent).x;
        if (measuredObjectiveWidth > objectiveWidth && measuredObjectiveWidth > 0f)
            worldMapObjectiveStyle.fontSize = Mathf.Max(9, Mathf.FloorToInt(worldMapObjectiveStyle.fontSize * objectiveWidth / measuredObjectiveWidth));
        GUI.Label(new Rect(panel.x + 16f, panel.yMax - 55f, objectiveWidth, 22f), objectiveContent, worldMapObjectiveStyle);
        GUI.Label(new Rect(panel.x + 16f, panel.yMax - 29f, panel.width - 32f, 18f),
            "M · L3 · Esc · Start para voltar", worldMapInstructionStyle);
        GUI.color = Color.white;
    }

    private void CloseWorldMap()
    {
        showWorldMap = false;
        ApplyPauseTimeScale();
    }

    private static Rect WorldMapPanelRect(float screenWidth, float screenHeight)
    {
        float width = Mathf.Max(0f, Mathf.Min(1100f, screenWidth - 32f));
        float height = Mathf.Max(0f, Mathf.Min(700f, screenHeight - 32f));
        return new Rect((screenWidth - width) * 0.5f, (screenHeight - height) * 0.5f, width, height);
    }

    private static float WorldMapRowHeight(float panelHeight) => Mathf.Max(60f, (panelHeight - 142f) / PhaseNames.Length);

    internal static int WorldMapRoomStatus(int phase, int room, int currentPhase, int currentRoom, bool[] currentPhaseCleared)
    {
        if (phase < currentPhase) return 2;
        if (phase > currentPhase) return 0;
        if (room == currentRoom) return 1;
        return currentPhaseCleared != null && room >= 0 && room < currentPhaseCleared.Length && currentPhaseCleared[room] ? 2 : 0;
    }

    private static Color WorldMapRoomColor(int state) => state == 1 ? new Color(0.1f, 0.24f, 0.25f, 0.98f) :
        state == 2 ? new Color(0.1f, 0.2f, 0.14f, 0.98f) : new Color(0.065f, 0.085f, 0.09f, 0.98f);

    internal static bool WorldMapLayoutFits(float screenWidth, float screenHeight)
    {
        Rect panel = WorldMapPanelRect(screenWidth, screenHeight);
        float contentWidth = panel.width - 32f;
        float cardWidth = (contentWidth - 16f) / 3f;
        float rowHeight = WorldMapRowHeight(panel.height);
        float finalCardBottom = panel.y + 62f + (PhaseNames.Length - 1) * rowHeight + rowHeight - 2f;
        float objectiveTop = panel.yMax - 55f;
        float footerBottom = panel.yMax - 11f;
        return screenWidth >= 160f && screenHeight >= 180f && cardWidth > 0f &&
            finalCardBottom <= objectiveTop && objectiveTop + 22f <= panel.yMax - 29f && footerBottom <= panel.yMax;
    }

    private static bool OverlayLayoutFits(float screenWidth, float screenHeight)
    {
        bool compact = screenHeight < 520f;
        float top = compact ? 18f : screenHeight * 0.29f;
        float messageTop = top + (compact ? 42f : 48f);
        float messageHeight = compact ? 86f : 62f;
        float buttonY = top + (compact ? 136f : 132f);
        float buttonHeight = compact ? 40f : 44f;
        float buttonStep = compact ? 48f : 54f;
        float footerTop = screenHeight - (compact ? 42f : 58f);
        float footerHeight = compact ? 28f : 35f;
        float horizontalWidth = Mathf.Min(680f, screenWidth - 32f);
        float thirdButtonBottom = buttonY + buttonStep * 2f + buttonHeight;
        return screenWidth >= 64f && horizontalWidth <= screenWidth - 32f &&
               messageTop + messageHeight <= buttonY && thirdButtonBottom <= footerTop && footerTop + footerHeight <= screenHeight;
    }

    private static bool PauseMapOverlayLayoutFits(float screenWidth, float screenHeight)
    {
        bool compact = screenHeight < 520f;
        float top = compact ? 18f : screenHeight * 0.29f;
        float messageBottom = top + (compact ? 42f + 68f : 48f + 62f);
        float buttonY = top + (compact ? 118f : 132f);
        float buttonHeight = compact ? 42f : 44f;
        float buttonStep = compact ? 44f : 54f;
        float footerTop = screenHeight - (compact ? 42f : 58f);
        float fourthButtonBottom = buttonY + buttonStep * 3f + buttonHeight;
        return screenWidth >= 64f && messageBottom <= buttonY && fourthButtonBottom <= footerTop && footerTop + (compact ? 28f : 35f) <= screenHeight;
    }

    private static Rect MemoryPanelRect(float screenWidth, float screenHeight, float bodyHeight)
    {
        float width = Mathf.Max(0f, Mathf.Min(740f, screenWidth - 36f));
        float panelHeight = 40f + Mathf.Max(62f, bodyHeight);
        float y = Mathf.Max(12f, Mathf.Min(screenHeight - 180f, screenHeight - 78f - panelHeight));
        return new Rect((screenWidth - width) * 0.5f, y, width, panelHeight);
    }

    private static Rect MemoryTextRect(Rect panel) =>
        new Rect(panel.x + 18f, panel.y + 34f, panel.width - 36f, panel.height - 40f);

    private static bool MemoryPanelFits(float screenWidth, float screenHeight, float bodyHeight)
    {
        float requiredBodyHeight = Mathf.Max(62f, bodyHeight);
        Rect panel = MemoryPanelRect(screenWidth, screenHeight, requiredBodyHeight);
        Rect text = MemoryTextRect(panel);
        return screenWidth >= 240f && screenHeight >= requiredBodyHeight + 130f &&
            panel.xMin >= 0f && panel.xMax <= screenWidth && panel.yMin >= 0f && panel.yMax <= screenHeight - 78f &&
            text.yMin >= panel.yMin && text.yMax <= panel.yMax - 6f && text.height >= requiredBodyHeight;
    }

    private bool OverlayButton(Rect rect, string label, int index)
    {
        if (overlaySelection == index) DrawSelection(rect);
        return GUI.Button(rect, label);
    }

    private static void DrawSelection(Rect rect)
    {
        GUI.color = new Color(1f, 0.78f, 0.28f, 0.95f);
        GUI.DrawTexture(new Rect(rect.x - 3f, rect.y - 3f, rect.width + 6f, 3f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x - 3f, rect.yMax, rect.width + 6f, 3f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x - 3f, rect.y, 3f, rect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMax, rect.y, 3f, rect.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    private void DrawOptions()
    {
        GUI.color = new Color(0.01f, 0.02f, 0.025f, 0.97f); GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture); GUI.color = Color.white;
        bool compact = Screen.height < 720f;
        float width = Mathf.Min(620f, Screen.width - 36f), x = (Screen.width - width) * 0.5f;
        float y = compact ? 8f : Mathf.Max(18f, (Screen.height - 690f) * 0.5f);
        float keyRowStart = compact ? 130f : 135f, keyRowStep = compact ? 34f : 39f, keyRowHeight = compact ? 30f : 32f;
        float padRowStart = compact ? 130f : 135f, padRowStep = compact ? 40f : 45f, padRowHeight = compact ? 30f : 32f;
        float backOffset = compact ? 512f : 585f, footerOffset = compact ? 560f : 637f;
        GUI.Label(new Rect(x, y, width, 42f), "OPÇÕES", centeredTitleStyle);
        float tabWidth = (width - 46f) / 3f;
        if (OptionButton(new Rect(x + 15f, y + 48f, tabWidth, 38f), "Som", 0)) { optionsTab = 0; optionsSelection = 0; }
        if (OptionButton(new Rect(x + 23f + tabWidth, y + 48f, tabWidth, 38f), "Teclas", 1)) { optionsTab = 1; optionsSelection = 1; }
        if (OptionButton(new Rect(x + 31f + tabWidth * 2f, y + 48f, tabWidth, 38f), "Controle", 2)) { optionsTab = 2; optionsSelection = 2; }
        if (optionsTab == 0)
        {
            if (optionsSelection == 3) DrawSelection(new Rect(x + 10f, y + 116f, width - 20f, 48f));
            if (optionsSelection == 4) DrawSelection(new Rect(x + 10f, y + 166f, width - 20f, 48f));
            if (optionsSelection == 5) DrawSelection(new Rect(x + 10f, y + 216f, width - 20f, 48f));
            if (optionsSelection == 6) DrawSelection(new Rect(x + 10f, y + 278f, width - 20f, 42f));
            if (optionsSelection == 7) DrawSelection(new Rect(x + 10f, y + 318f, width - 20f, 42f));
            if (optionsSelection == 8) DrawSelection(new Rect(x + 10f, y + 358f, width - 20f, 42f));
            GUI.Label(new Rect(x + 15f, y + 125f, 170f, 28f), "Música", bodyStyle);
            ScadrykSettings.MusicVolume = GUI.HorizontalSlider(new Rect(x + 195f, y + 135f, width - 215f, 24f), ScadrykSettings.MusicVolume, 0f, 1f);
            GUI.Label(new Rect(x + 15f, y + 175f, 170f, 28f), "Efeitos", bodyStyle);
            ScadrykSettings.EffectsVolume = GUI.HorizontalSlider(new Rect(x + 195f, y + 185f, width - 215f, 24f), ScadrykSettings.EffectsVolume, 0f, 1f);
            GUI.Label(new Rect(x + 15f, y + 225f, 170f, 28f), "Tamanho do texto", bodyStyle);
            float scale = GUI.HorizontalSlider(new Rect(x + 195f, y + 235f, width - 215f, 24f), ScadrykSettings.TextScale, 1f, 1.5f);
            if (Mathf.Abs(scale - ScadrykSettings.TextScale) > 0.01f) { ScadrykSettings.TextScale = scale; titleStyle = null; }
            ScadrykSettings.ReduceFlash = GUI.Toggle(new Rect(x + 15f, y + 285f, width - 30f, 34f), ScadrykSettings.ReduceFlash, "Reduzir flashes de dano");
            ScadrykSettings.ReduceShake = GUI.Toggle(new Rect(x + 15f, y + 325f, width - 30f, 34f), ScadrykSettings.ReduceShake, "Desligar tremor de câmera");
            ScadrykSettings.HighContrastWarnings = GUI.Toggle(new Rect(x + 15f, y + 365f, width - 30f, 34f), ScadrykSettings.HighContrastWarnings, "Alto contraste nos avisos de perigo");
            GUI.Label(new Rect(x + 15f, y + 410f, width - 30f, 70f), "Avisos permanecem visíveis com os efeitos em silêncio. F8 marca um ataque pouco claro durante testes.", memoryStyle);
        }
        else if (optionsTab == 1)
        {
            GUI.Label(new Rect(x + 15f, y + 99f, width - 185f, 31f), "Clique para trocar; M mapa; F8 QA.", smallStyle);
            if (OptionButton(new Rect(x + width - 155f, y + 95f, 140f, 32f), "Padrões", 14))
            {
                ScadrykSettings.ResetKeyboardBindings();
                rebindingAction = -1;
                ShowStatus("Teclas padrão restauradas.", 2f);
            }
            for (int i = 0; i < ScadrykSettings.Actions.Length; i++)
            {
                float row = y + keyRowStart + i * keyRowStep;
                GUI.Label(new Rect(x + 15f, row, 220f, keyRowHeight), ScadrykSettings.Actions[i], bodyStyle);
                string label = rebindingAction == i ? "Pressione uma tecla…" : ScadrykSettings.KeyName(ScadrykSettings.Key(i));
                Rect keyButton = new Rect(x + 240f, row, width - 255f, keyRowHeight);
                if (OptionButton(keyButton, label, i + 3)) rebindingAction = i;
            }
            if (rebindingAction >= 0 && Event.current.type == EventType.KeyDown && ScadrykSettings.CanBindKey(Event.current.keyCode))
            {
                ScadrykSettings.Bind(rebindingAction, Event.current.keyCode);
                rebindingAction = -1;
                Event.current.Use();
            }
        }
        else if (optionsTab == 2)
        {
            GUI.Label(new Rect(x + 15f, y + 99f, width - 185f, 31f), "Selecione ação e botão.", smallStyle);
            if (OptionButton(new Rect(x + width - 155f, y + 95f, 140f, 32f), "Padrões", 10))
            {
                ScadrykSettings.ResetGamepadBindings();
                gamepadRebindAction = -1;
                ShowStatus("Controle padrão restaurado.", 2f);
            }
            for (int i = 0; i < 7; i++)
            {
                float row = y + padRowStart + i * padRowStep;
                GUI.Label(new Rect(x + 15f, row, 220f, padRowHeight), ScadrykSettings.Actions[i], bodyStyle);
                string label = gamepadRebindAction == i ? "Pressione um botão…" : ScadrykSettings.PadButtonName(ScadrykSettings.PadButton(i));
                if (OptionButton(new Rect(x + 240f, row, width - 255f, padRowHeight), label, i + 3)) { gamepadRebindAction = i; gamepadRebindAfterFrame = Time.frameCount; }
            }
        }
        if (OptionButton(new Rect(x + (width - 180f) * 0.5f, y + backOffset, 180f, 42f), "Voltar", optionsTab == 0 ? 9 : optionsTab == 1 ? 15 : 11)) { showOptions = false; rebindingAction = gamepadRebindAction = -1; }
        GUI.Label(new Rect(x, y + footerOffset, width, 28f), "Controle: analógico navega · A confirma · B volta · Start pausa · L3 mapa.", smallStyle);
    }

    private static bool OptionsLayoutFits(float screenHeight)
    {
        bool compact = screenHeight < 720f;
        float y = compact ? 8f : Mathf.Max(18f, (screenHeight - 690f) * 0.5f);
        float rowStart = compact ? 130f : 135f, rowStep = compact ? 34f : 39f, rowHeight = compact ? 30f : 32f;
        float backOffset = compact ? 512f : 585f, footerOffset = compact ? 560f : 637f;
        float lastKeyRowBottom = y + rowStart + (ScadrykSettings.Actions.Length - 1) * rowStep + rowHeight;
        return lastKeyRowBottom <= y + backOffset && y + backOffset + 42f <= screenHeight && y + footerOffset + 28f <= screenHeight;
    }

    private static bool OptionsResetRowFits(float screenWidth)
    {
        float width = Mathf.Min(620f, screenWidth - 36f);
        float x = (screenWidth - width) * 0.5f;
        Rect instructions = new Rect(x + 15f, 8f + 99f, width - 185f, 31f);
        Rect reset = new Rect(x + width - 155f, 8f + 95f, 140f, 32f);
        return width >= 175f && reset.xMin >= x && reset.xMax <= x + width && !instructions.Overlaps(reset);
    }

    private static bool SoundOptionsLayoutFits(float screenHeight)
    {
        bool compact = screenHeight < 720f;
        float y = compact ? 8f : Mathf.Max(18f, (screenHeight - 690f) * 0.5f);
        float warningToggleBottom = y + 365f + 34f;
        float warningHelpBottom = y + 410f + 70f;
        float backTop = y + (compact ? 512f : 585f);
        float footerBottom = y + (compact ? 560f : 637f) + 28f;
        return warningToggleBottom <= warningHelpBottom && warningHelpBottom <= backTop && footerBottom <= screenHeight;
    }

    private bool OptionButton(Rect rect, string label, int selection)
    {
        if (optionsSelection == selection) DrawSelection(rect);
        return GUI.Button(rect, label);
    }

    private void EnsureStyles()
    {
        float scale = Mathf.Clamp(ScadrykSettings.TextScale, 1f, 1.5f);
        if (titleStyle != null && Mathf.Approximately(cachedTextScale, scale)) return;
        cachedTextScale = scale;
        GUI.skin.button.fontSize = Mathf.RoundToInt(14f * scale);
        GUI.skin.toggle.fontSize = Mathf.RoundToInt(14f * scale);
        titleStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(17f * scale), fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.91f, 0.81f, 0.57f) }, alignment = TextAnchor.MiddleLeft };
        bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(14f * scale), normal = { textColor = new Color(0.88f, 0.89f, 0.83f) }, alignment = TextAnchor.MiddleLeft };
        smallStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(11f * scale), normal = { textColor = new Color(0.72f, 0.76f, 0.72f) }, alignment = TextAnchor.MiddleLeft };
        centeredTitleStyle = new GUIStyle(titleStyle) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(25f * scale) };
        openingTitleStyle = new GUIStyle(centeredTitleStyle);
        centeredBodyStyle = new GUIStyle(bodyStyle) { alignment = TextAnchor.MiddleCenter, wordWrap = true, fontSize = Mathf.RoundToInt(16f * scale) };
        openingBodyStyle = new GUIStyle(centeredBodyStyle);
        worldMapRoomStyle = new GUIStyle(centeredBodyStyle) { fontSize = Mathf.RoundToInt(10f * scale), wordWrap = true, fontStyle = FontStyle.Bold };
        worldMapStateStyle = new GUIStyle(smallStyle) { fontSize = Mathf.RoundToInt(9f * scale), alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        worldMapObjectiveStyle = new GUIStyle(centeredBodyStyle) { fontSize = Mathf.RoundToInt(11f * scale), wordWrap = false };
        worldMapInstructionStyle = new GUIStyle(smallStyle) { alignment = TextAnchor.MiddleCenter };
        controlHintStyle = new GUIStyle(smallStyle) { alignment = TextAnchor.MiddleCenter };
        offscreenTargetStyle = new GUIStyle(centeredBodyStyle) { fontSize = Mathf.RoundToInt(13f * scale), fontStyle = FontStyle.Bold };
        finalBossLabelStyle = new GUIStyle(titleStyle) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(14f * scale), normal = { textColor = new Color(1f, 0.73f, 0.86f) } };
        memoryStyle = new GUIStyle(bodyStyle) { wordWrap = true, alignment = TextAnchor.UpperLeft };
    }
}
