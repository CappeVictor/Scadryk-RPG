using UnityEngine;

public sealed partial class ScadrykGameDirector
{
    private Vector2 compactOptionsScroll;
    private int lastCompactOptionsTab = -1;
    private int lastCompactOptionsSelection = -1;
    private Vector2 memoryJournalScroll;
    private float nextMemoryJournalNavigationAt;
    private string[] memoryJournalEntries;
    private bool memoryJournalLoaded;

    private void OpenMemoryJournal()
    {
        memoryJournalEntries = ReadStoryJournal("Scadryk.");
        memoryJournalLoaded = true;
        memoryJournalScroll = Vector2.zero;
        showMemoryJournal = true;
    }

    private void OnGUI()
    {
        EnsureStyles();
        if (showOpeningStory) { DrawOpeningStory(); return; }
        if (showMemoryJournal) { DrawMemoryJournal(); return; }
        if (showWorldMap) { DrawWorldMap(); return; }
        if (showOptions) { DrawOptions(); return; }
        if (memoryModal) { DrawMemoryModal(); return; }
        if (showTitle || isPaused || victory) { DrawOverlay(); return; }
        DrawHud(); DrawMiniMap(); DrawOffscreenTargetHint();
        DrawWitnessPrompt();
        if (phaseGuardian != null) DrawPhaseGuardianTitle();
        if (finalBoss != null) DrawFinalBossBar();
        DrawThreatCaptions();
        bool showStatus = Time.time < statusUntil;
        bool showMemory = Time.time < memoryUntil && !transitioning;
        if (showStatus)
        {
            float statusWidth = Mathf.Max(0f, Screen.width - 30f);
            float statusTextHeight = statusStyle.CalcHeight(new GUIContent(statusMessage), Mathf.Max(32f, statusWidth - 20f));
            Rect statusRect = StatusLabelRect(Screen.width, Screen.height, statusTextHeight);
            GUI.color = new Color(0.025f, 0.04f, 0.045f, 0.88f);
            GUI.DrawTexture(statusRect, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(statusRect.x + 10f, statusRect.y + 4f, statusRect.width - 20f, statusRect.height - 8f), statusMessage, statusStyle);
        }
        if (showMemory)
        {
            float width = Mathf.Max(0f, Mathf.Min(740f, Screen.width - 36f));
            float bodyHeight = Mathf.Max(62f, memoryStyle.CalcHeight(new GUIContent(memoryText), Mathf.Max(32f, width - 36f)));
            Rect panel = MemoryPanelRect(Screen.width, Screen.height, bodyHeight);
            GUI.color = new Color(0.025f, 0.04f, 0.045f, 0.94f); GUI.DrawTexture(panel, Texture2D.whiteTexture); GUI.color = Color.white;
            GUI.Label(new Rect(panel.x + 18f, panel.y + 10f, panel.width - 36f, 24f), "VESTÍGIO ESCUTADO", titleStyle);
            GUI.Label(MemoryTextRect(panel), memoryText, memoryStyle);
        }
        bool compactControlHint = Screen.width < 840f;
        if (!showStatus && !(compactControlHint && showMemory))
        {
            string controlHint = compactControlHint
                ? CompactControlHintText("teclado/LS", "mouse/RS", InputBinding(0), InputBinding(1), InputBinding(2), InputBinding(6))
                : "Mover: teclas/controle  ·  Mouse/analógico mirar  ·  " + InputBinding(0) + " atacar  ·  " + InputBinding(1) + " esquivar  ·  " + InputBinding(2) + " pulso  ·  " + InputBinding(6) + " interagir  ·  M/L3 mapa  ·  Esc pausa";
            controlHintStyle.fontSize = smallStyle.fontSize;
            controlHintStyle.wordWrap = compactControlHint;
            float hintWidth = Mathf.Max(0f, Screen.width - 28f);
            float hintHeight = compactControlHint
                ? controlHintStyle.CalcHeight(new GUIContent(controlHint), hintWidth)
                : 30f;
            Rect controlHintRect = new Rect(14f, Screen.height - hintHeight - 8f, hintWidth, hintHeight);
            if (!compactControlHint)
            {
                float measuredControlHintWidth = controlHintStyle.CalcSize(new GUIContent(controlHint)).x;
                if (measuredControlHintWidth > controlHintRect.width && measuredControlHintWidth > 0f)
                    controlHintStyle.fontSize = Mathf.Max(9, Mathf.FloorToInt(controlHintStyle.fontSize * controlHintRect.width / measuredControlHintWidth));
            }
            GUI.Label(controlHintRect, controlHint, controlHintStyle);
        }
        if (transitionFade > 0f)
        {
            GUI.color = new Color(0f, 0f, 0f, transitionFade);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            if (transitioning && transitionFade > 0.35f)
                GUI.Label(new Rect(0, Screen.height / 2f - 20, Screen.width, 40), "ATRAVESSANDO O LIMIAR", centeredTitleStyle);
        }
    }

    private void DrawThreatCaptions()
    {
        string first = null;
        string second = null;
        for (int i = 0; i < activeEnemies.Count; i++)
        {
            ScadrykEnemyHealth enemy = activeEnemies[i];
            if (enemy == null || !enemy.TryGetWindupAttackLabel(out string label)) continue;
            if (label == first || label == second) continue;
            if (first == null) first = label;
            else { second = label; break; }
        }
        if (first == null) return;

        float width = Mathf.Min(620f, Screen.width - 28f);
        float height = second == null ? 30f : 52f;
        Rect panel = new Rect((Screen.width - width) * 0.5f, Mathf.Max(92f, Screen.height * 0.12f), width, height);
        GUI.color = new Color(0.035f, 0.025f, 0.035f, 0.9f);
        GUI.DrawTexture(panel, Texture2D.whiteTexture);
        GUI.color = new Color(1f, 0.72f, 0.38f, 1f);
        GUI.DrawTexture(new Rect(panel.x, panel.y, panel.width, 2f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        int oldSize = centeredBodyStyle.fontSize;
        centeredBodyStyle.fontSize = Mathf.Max(10, Mathf.RoundToInt(13f * Mathf.Clamp(ScadrykSettings.TextScale, 1f, 1.5f)));
        GUI.Label(new Rect(panel.x + 8f, panel.y + 2f, panel.width - 16f, 24f), "PREPARANDO: " + first, centeredBodyStyle);
        if (second != null)
            GUI.Label(new Rect(panel.x + 8f, panel.y + 26f, panel.width - 16f, 23f), "PREPARANDO: " + second, centeredBodyStyle);
        centeredBodyStyle.fontSize = oldSize;
    }

    private void DrawMemoryJournal()
    {
        GUI.color = new Color(0f, 0f, 0f, 0.76f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
        Rect panel = StoryJournalPanelRect(Screen.width, Screen.height);
        GUI.color = new Color(0.025f, 0.04f, 0.045f, 0.98f);
        GUI.DrawTexture(panel, Texture2D.whiteTexture);
        GUI.color = new Color(0.74f, 0.63f, 0.34f, 1f);
        GUI.DrawTexture(new Rect(panel.x, panel.y, panel.width, 3f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        if (!memoryJournalLoaded) OpenMemoryJournal();
        string[] entries = memoryJournalEntries ?? System.Array.Empty<string>();
        GUI.Label(new Rect(panel.x + 18f, panel.y + 10f, panel.width - 36f, 30f), "CADERNO DE MEMÓRIAS", titleStyle);
        GUI.Label(new Rect(panel.x + 18f, panel.y + 39f, panel.width - 36f, 22f),
            entries.Length + (entries.Length == 1 ? " lembrança recuperada" : " lembranças recuperadas"), smallStyle);

        Rect viewport = StoryJournalViewportRect(panel);
        if (entries.Length == 0)
        {
            TextAnchor oldAlignment = memoryStyle.alignment;
            memoryStyle.alignment = TextAnchor.MiddleCenter;
            GUI.Label(viewport, "As lembranças surgirão aqui quando Nara encontrar os Ecos.", memoryStyle);
            memoryStyle.alignment = oldAlignment;
        }
        float textWidth = Mathf.Max(40f, viewport.width - 30f);
        float contentHeight = 8f;
        float[] cardHeights = new float[entries.Length];
        for (int i = 0; i < entries.Length; i++)
        {
            cardHeights[i] = Mathf.Max(52f, memoryStyle.CalcHeight(new GUIContent(entries[i]), textWidth) + 22f);
            contentHeight += cardHeights[i] + 10f;
        }
        contentHeight = Mathf.Max(contentHeight, viewport.height);
        Rect content = new Rect(0f, 0f, viewport.width - 18f, contentHeight);
        memoryJournalScroll = GUI.BeginScrollView(viewport, memoryJournalScroll, content);
        float y = 8f;
        for (int i = 0; i < entries.Length; i++)
        {
            Rect card = new Rect(2f, y, content.width - 4f, cardHeights[i]);
            GUI.color = new Color(0.055f, 0.08f, 0.085f, 0.96f);
            GUI.DrawTexture(card, Texture2D.whiteTexture);
            GUI.color = new Color(0.74f, 0.63f, 0.34f, 0.72f);
            GUI.DrawTexture(new Rect(card.x, card.y, 2f, card.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(card.x + 12f, card.y + 8f, card.width - 24f, card.height - 16f), entries[i], memoryStyle);
            y += card.height + 10f;
        }
        GUI.EndScrollView();
        smallStyle.alignment = TextAnchor.MiddleCenter;
        GUI.Label(new Rect(panel.x + 14f, panel.yMax - 31f, panel.width - 28f, 22f),
            "Esc / Start / B voltar · setas, analógico ou roda para ler", smallStyle);
        smallStyle.alignment = TextAnchor.MiddleLeft;
    }

    internal static Rect StoryJournalPanelRect(float screenWidth, float screenHeight)
    {
        float width = Mathf.Max(0f, Mathf.Min(860f, screenWidth - 32f));
        float height = Mathf.Max(0f, Mathf.Min(760f, screenHeight - 32f));
        return new Rect((screenWidth - width) * 0.5f, (screenHeight - height) * 0.5f, width, height);
    }

    internal static Rect StoryJournalViewportRect(Rect panel) =>
        new Rect(panel.x + 16f, panel.y + 66f, panel.width - 32f, panel.height - 111f);

    internal static bool StoryJournalLayoutFits(float screenWidth, float screenHeight)
    {
        Rect panel = StoryJournalPanelRect(screenWidth, screenHeight);
        Rect viewport = StoryJournalViewportRect(panel);
        return screenWidth >= 220f && screenHeight >= 160f && panel.width >= 188f && panel.height >= 128f &&
            viewport.width >= 156f && viewport.height >= 45f && panel.y >= 0f && panel.yMax <= screenHeight;
    }

    private void DrawMemoryModal()
    {
        GUI.color = new Color(0f, 0f, 0f, 0.68f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
        float width = Mathf.Max(0f, Mathf.Min(740f, Screen.width - 36f));
        float bodyHeight = Mathf.Max(62f, memoryStyle.CalcHeight(new GUIContent(memoryText), Mathf.Max(32f, width - 36f)));
        Rect panel = MemoryPanelRect(Screen.width, Screen.height, bodyHeight);
        GUI.color = new Color(0.025f, 0.04f, 0.045f, 0.98f); GUI.DrawTexture(panel, Texture2D.whiteTexture); GUI.color = Color.white;
        Rect titleRect = new Rect(panel.x + 18f, panel.y + 10f, panel.width - 36f, 24f);
        int titleFontSize = titleStyle.fontSize;
        titleStyle.fontSize = FitLabelFontSize(titleFontSize, titleStyle.CalcSize(new GUIContent("VESTÍGIO ESCUTADO")).x, titleRect.width);
        GUI.Label(titleRect, "VESTÍGIO ESCUTADO", titleStyle);
        titleStyle.fontSize = titleFontSize;
        GUI.Label(MemoryTextRect(panel), memoryText, memoryStyle);
        smallStyle.alignment = TextAnchor.MiddleCenter;
        Rect hintRect = MemoryDismissHintRect(panel);
        int hintFontSize = smallStyle.fontSize;
        bool gamepad = sessionInputMode == "gamepad";
        string hint = MemoryDismissHintText(gamepad, InputBinding(6));
        if (smallStyle.CalcSize(new GUIContent(hint)).x > hintRect.width)
            hint = MemoryDismissHintText(gamepad, InputBinding(6), true);
        smallStyle.fontSize = FitLabelFontSize(hintFontSize, smallStyle.CalcSize(new GUIContent(hint)).x, hintRect.width);
        GUI.Label(hintRect, hint, smallStyle);
        smallStyle.fontSize = hintFontSize;
        smallStyle.alignment = TextAnchor.MiddleLeft;
    }

    private static Rect MemoryDismissHintRect(Rect panel) =>
        new Rect(panel.x + 12f, panel.yMax - 32f, panel.width - 24f, 24f);

    internal static Rect StatusLabelRect(float screenWidth, float screenHeight, float measuredTextHeight)
    {
        float width = Mathf.Max(0f, screenWidth - 30f);
        float availableHeight = Mathf.Max(0f, screenHeight - 16f);
        float height = Mathf.Min(availableHeight, Mathf.Max(44f, Mathf.Max(0f, measuredTextHeight) + 12f));
        return new Rect(15f, Mathf.Max(8f, screenHeight - height - 8f), width, height);
    }

    internal static string AbilityHudLabel(int index, string binding, string state)
    {
        string[] labels = { "Crescente · interrompe", "Estouro · expõe", "Rompante · afasta" };
        if (index < 0 || index >= labels.Length) return string.Empty;
        return binding + "  " + labels[index] + "  ·  " + state;
    }

    internal static string AbilityHudLabel(int index, string binding, string state, bool hasEcho)
    {
        string label = AbilityHudLabel(index, binding, state);
        return hasEcho && label.Length > 0 ? label + "  ·  eco" : label;
    }

    private void DrawHud()
    {
        float extra = ScadrykSettings.TextScale - 1f;
        string objective = ActiveObjectiveText();
        Rect panel = HudPanelRect(Screen.width, Screen.height, ScadrykSettings.TextScale);
        float textWidth = Mathf.Max(0f, panel.width - 28f);
        float trackWidth = Mathf.Min(275f + extra * 120f, textWidth);
        GUI.color = new Color(0.035f, 0.05f, 0.06f, 0.92f); GUI.Box(panel, GUIContent.none); GUI.color = Color.white;
        GUI.Label(new Rect(28, 22, textWidth, 25f + extra * 15f), PhaseNames[phaseIndex] + "  ·  Fase " + (phaseIndex + 1) + "/3", titleStyle);
        GUI.Label(new Rect(28, 51f + extra * 8f, textWidth, 23f + extra * 12f), roomNames[phaseIndex][roomIndex] + "  ·  Sala " + (roomIndex + 1) + "/3", bodyStyle);
        GUI.color = new Color(0.18f, 0.19f, 0.2f); GUI.DrawTexture(new Rect(28, 82f + extra * 18f, trackWidth, 12), Texture2D.whiteTexture);
        GUI.color = new Color(0.79f, 0.25f, 0.22f); GUI.DrawTexture(new Rect(28, 82f + extra * 18f, trackWidth * (player != null ? player.Health / player.MaxHealth : 1f), 12), Texture2D.whiteTexture); GUI.color = Color.white;
        GUI.Label(new Rect(28, 98f + extra * 23f, textWidth, 19f + extra * 10f), "Ecos: " + enemiesRemaining + "    Esquiva: " + Cooldown(player != null ? player.DashCooldownLeft : 0) + "    Pulso: " + Cooldown(player != null ? player.ShockwaveCooldownLeft : 0), smallStyle);
        for (int i = 0; i < 3; i++)
        {
            bool learned = player != null && player.HasAbility(i);
            string state = !learned ? "bloqueado" : player.AbilityCooldownLeft(i) <= 0.02f ? "pronto" : Cooldown(player.AbilityCooldownLeft(i));
            GUI.color = learned ? new Color(0.72f, 0.9f, 0.79f) : new Color(0.48f, 0.53f, 0.51f);
            GUI.Label(new Rect(28, 116f + extra * 37f + i * (15f + extra * 9f), textWidth, 16f + extra * 9f),
                AbilityHudLabel(i, InputBinding(i + 3), state, player != null && player.HasPowerEcho(i)), smallStyle);
        }
        GUI.color = Color.white;
        float objectiveY = 164f + extra * 64f;
        float objectiveHeight = Mathf.Max(0f, panel.yMax - objectiveY - 10f);
        GUI.Label(new Rect(28, objectiveY, textWidth, objectiveHeight), "Objetivo: " + objective, hudObjectiveStyle);
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
        GUI.Label(new Rect(panel.x + 10f, panel.y + 3f, panel.width - 20f, 20f), "O ARAUTO DO VAZIO  ·  FASE " + finalBoss.DisplayStage + "/3", finalBossLabelStyle);
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
        Rect hud = HudPanelRect(Screen.width, Screen.height, ScadrykSettings.TextScale);
        if (!MiniMapPanelFits(panel, hud, Screen.width, Screen.height)) return;
        GUI.color = new Color(0.035f, 0.05f, 0.06f, 0.9f); GUI.Box(panel, GUIContent.none); GUI.color = Color.white;
        string[] mapBiomeNames = { "Bosque", "Sal", "Costa" };
        GUI.Label(new Rect(panel.x + 13, panel.y + 8, width - 26f, 21f + extra * 8f), "MAPA · " + mapBiomeNames[phaseIndex] + " · ↑/↓ · ◇/◆ SELO", smallStyle);
        float mapX = panel.x + 15, mapY = panel.y + 43 + extra * 5f, roomW = 75f + extra * 30f, roomH = 57f + extra * 10f, gap = 8;
        for (int i = 0; i < RoomsPerPhase; i++)
        {
            Rect cell = new Rect(mapX + i * (roomW + gap), mapY, roomW, roomH);
            GUI.color = MiniMapRoomColor(i == roomIndex, clearedRooms[i]);
            GUI.Box(cell, GUIContent.none); GUI.color = new Color(0.75f, 0.74f, 0.67f);
            GUI.Label(new Rect(cell.x + 3, cell.y + 2, roomW - 6, 18), (i + 1) + ". " + MapRoomRouteLabel(phaseIndex, i, IsWorldSecretRevealed("Scadryk.", phaseIndex, i)), smallStyle);
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
        Rect hud = HudPanelRect(screenWidth, screenHeight, textScale);
        float x = screenWidth - width - 16f;
        float y = 14f;
        if (x < hud.xMax + 16f)
        {
            y = hud.yMax + 8f;
            if (y + height > screenHeight - 8f) y = Mathf.Max(8f, screenHeight - height - 8f);
        }
        return new Rect(x, y, width, height);
    }

    private static Rect HudPanelRect(float screenWidth, float screenHeight, float textScale)
    {
        float extra = Mathf.Clamp(textScale, 1f, 1.5f) - 1f;
        float preferredWidth = 325f + extra * 130f;
        float width = Mathf.Min(preferredWidth, Mathf.Max(0f, screenWidth - 28f));
        return new Rect(14f, 14f, width, 190f + extra * 86f);
    }

    private static bool MiniMapPanelFits(Rect panel, Rect hud, float screenWidth, float screenHeight) =>
        panel.width > 0f && panel.height > 0f && panel.xMin >= 8f && panel.yMin >= 8f &&
        panel.xMax <= screenWidth - 8f && panel.yMax <= screenHeight - 8f && !panel.Overlaps(hud);

    private static bool HudPanelFits(Rect panel, float screenWidth, float screenHeight) =>
        panel.width > 0f && panel.height > 0f && panel.xMin >= 8f && panel.yMin >= 8f &&
        panel.xMax <= screenWidth - 8f && panel.yMax <= screenHeight - 8f;

    private static string MapRoomLabel(int phase, int room) => MapRoomLabels[phase][room];
    internal static string MapRoomRouteLabel(int phase, int room) => MapRoomRouteLabel(phase, room, false);
    internal static string MapRoomRouteLabel(int phase, int room, bool secretFound) =>
        TryGetSideRoute(phase, room, out int side, out _, out _) ? MapRoomLabel(phase, room) + (side > 0 ? " ↑" : " ↓") +
            (PowerGrantedBySecretAt(phase, room) >= 0 ? (secretFound ? " ◆" : " ◇") : string.Empty) : MapRoomLabel(phase, room);
    private static string Cooldown(float value) => value <= 0.02f ? "pronto" : value.ToString("0.0") + "s";

    internal static string OverlayMessage(bool confirmNewJourney, bool victory, bool showTitle) =>
        confirmNewJourney ? "Uma jornada nova apaga o progresso salvo. Quer substituir seu checkpoint?" :
        victory ? VictoryEndingText :
        showTitle ? "Nara, a Ouvinte, segue os Ecos até a origem do silêncio." :
        "A jornada pode esperar por você.";

    internal static string CheckpointSummary(int phase, int room, bool guardianDefeated = false, bool finalBossDefeated = false)
    {
        phase = Mathf.Clamp(phase, 0, PhaseNames.Length - 1);
        room = Mathf.Clamp(room, 0, RoomsPerPhase - 1);
        string location = "Retomar: " + PhaseNames[phase] + " · " + MapRoomLabel(phase, room) + " · sala " + (room + 1) + "/" + RoomsPerPhase;
        if (ShouldRestoreFinalBossDefeat(phase, room, finalBossDefeated)) return location + " · portal colossal aberto";
        if (ShouldRestoreDefeatedGuardian(phase, room, guardianDefeated))
            return location + (phase == PhaseNames.Length - 1 ? " · último guardião vencido; Arauto aguarda" : " · guardião vencido; portal aberto");
        return location;
    }

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
        bool compactTitleMenu = showTitle && !victory && !confirmNewJourney;
        bool compressedTitleMenu = compactTitleMenu && Screen.height < 600f;
        float buttonY = top + (compact ? (pauseMapMenu ? 118f : compactTitleMenu ? 130f : 136f) : compressedTitleMenu ? 120f : 132f);
        float buttonHeight = compact ? (pauseMapMenu ? 42f : compactTitleMenu ? 36f : 40f) : compressedTitleMenu ? 40f : 44f;
        float buttonStep = compact ? (pauseMapMenu ? 44f : compactTitleMenu ? 40f : 48f) : compressedTitleMenu ? 42f : 54f;
        float footerWidth = Mathf.Min(600f, Screen.width - 32f), footerHeight = compact ? 28f : 35f;
        float footerTop = Screen.height - (compact ? 42f : 58f);
        GUI.Label(new Rect(center - titleWidth * 0.5f, top, titleWidth, titleHeight), heading, centeredTitleStyle);
        string message = showTitle && !victory && !confirmNewJourney &&
            TryLoadCheckpoint("Scadryk.", out int savedPhase, out int savedRoom)
                ? CheckpointSummary(savedPhase, savedRoom,
                    HasGuardianDefeatProgress("Scadryk.", savedPhase), HasFinalBossDefeatProgress("Scadryk."))
                : OverlayMessage(confirmNewJourney, victory, showTitle);
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
            bool hasCheckpoint = HasCheckpoint("Scadryk.");
            float titleButtonStep = buttonY + buttonStep;
            if (OverlayButton(new Rect(center - buttonWidth * 0.5f, buttonY, buttonWidth, buttonHeight), hasCheckpoint ? "Continuar jornada" : "Começar jornada", 0)) BeginJourney();
            if (hasCheckpoint)
            {
                if (OverlayButton(new Rect(center - buttonWidth * 0.5f, titleButtonStep, buttonWidth, buttonHeight), "Nova jornada", 1)) RequestNewGame();
                titleButtonStep += buttonStep;
            }
            int optionsIndex = TitleMenuOptionsSelection(hasCheckpoint);
            int journalIndex = TitleMenuJournalSelection(hasCheckpoint);
            if (OverlayButton(new Rect(center - buttonWidth * 0.5f, titleButtonStep, buttonWidth, buttonHeight), "Opções e controles", optionsIndex))
            { showOptions = true; optionsTab = optionsSelection = 0; }
            if (OverlayButton(new Rect(center - buttonWidth * 0.5f, titleButtonStep + buttonStep, buttonWidth, buttonHeight), "Caderno de memórias", journalIndex)) OpenMemoryJournal();
        }
        else if (victory)
        {
            if (OverlayButton(new Rect(center - buttonWidth * 0.5f, buttonY, buttonWidth, buttonHeight), "Nova jornada", 0)) StartNewGame();
            if (OverlayButton(new Rect(center - buttonWidth * 0.5f, buttonY + buttonStep, buttonWidth, buttonHeight), "Caderno de memórias", 1))
                OpenMemoryJournal();
            if (OverlayButton(new Rect(center - buttonWidth * 0.5f, buttonY + buttonStep * 2f, buttonWidth, buttonHeight), "Voltar ao título", 2)) ReturnToTitle();
        }
        else
        {
            float gap = 8f;
            float columnWidth = Mathf.Min((buttonWidth - gap) * 0.5f, (Screen.width - 32f - gap) * 0.5f);
            float left = center - columnWidth - gap * 0.5f;
            float right = center + gap * 0.5f;
            float row0 = buttonY;
            float row1 = buttonY + buttonStep;
            float row2 = buttonY + buttonStep * 2f;

            if (OverlayButton(new Rect(left, row0, columnWidth, buttonHeight), "Continuar", 0)) isPaused = false;
            if (OverlayButton(new Rect(right, row0, columnWidth, buttonHeight), "Mapa", 1))
            { isPaused = false; showWorldMap = true; ApplyPauseTimeScale(); }
            if (OverlayButton(new Rect(left, row1, columnWidth, buttonHeight), "Nova jornada", 2)) RequestNewGame();
            if (OverlayButton(new Rect(right, row1, columnWidth, buttonHeight), "Opções", 3)) { showOptions = true; optionsTab = optionsSelection = 0; }
            if (OverlayButton(new Rect(left, row2, columnWidth, buttonHeight), "Memórias", 4)) OpenMemoryJournal();
            if (OverlayButton(new Rect(right, row2, columnWidth, buttonHeight), "Salvar e sair", 5)) ReturnToTitle();
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
            OpeningStoryContinueHint(sessionInputMode == "gamepad", InputBinding(6)), smallStyle);
        GUI.color = Color.white;
    }

    internal static string OpeningStoryContinueHint(bool gamepad, string interactBinding)
    {
        if (gamepad) return interactBinding == "A" ? "A para continuar" : "A / " + interactBinding + " para continuar";
        string keyboardBinding = interactBinding == "Enter" || interactBinding == "Espaço"
            ? interactBinding == "Enter" ? "Enter / Espaço" : "Espaço / Enter"
            : interactBinding + " / Enter / Espaço";
        return keyboardBinding + " para continuar";
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
            "TRÊS BIOMAS · NOVE SALAS · ↑/↓ DESVIO · ◇ PENDENTE · ◆ LIBERADO", worldMapInstructionStyle);
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
                    (room + 1) + ". " + MapRoomRouteLabel(phase, room, IsWorldSecretRevealed("Scadryk.", phase, room)), worldMapRoomStyle);
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
        bool compressedTitleMenu = screenHeight < 600f;
        float buttonY = top + (compact ? 130f : compressedTitleMenu ? 120f : 132f);
        float buttonHeight = compact ? 36f : compressedTitleMenu ? 40f : 44f;
        float buttonStep = compact ? 40f : compressedTitleMenu ? 42f : 54f;
        float footerTop = screenHeight - (compact ? 42f : 58f);
        float footerHeight = compact ? 28f : 35f;
        float horizontalWidth = Mathf.Min(680f, screenWidth - 32f);
        float fourthButtonBottom = buttonY + buttonStep * 3f + buttonHeight;
        return screenWidth >= 64f && horizontalWidth <= screenWidth - 32f &&
               messageTop + messageHeight <= buttonY && fourthButtonBottom <= footerTop && footerTop + footerHeight <= screenHeight;
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
        float thirdRowBottom = buttonY + buttonStep * 2f + buttonHeight;
        return screenWidth >= 256f && messageBottom <= buttonY && thirdRowBottom <= footerTop && footerTop + (compact ? 28f : 35f) <= screenHeight;
    }

    private static Rect MemoryPanelRect(float screenWidth, float screenHeight, float bodyHeight)
    {
        float width = Mathf.Max(0f, Mathf.Min(740f, screenWidth - 36f));
        float panelHeight = 82f + Mathf.Max(62f, bodyHeight);
        float y = Mathf.Max(12f, (screenHeight - panelHeight) * 0.5f);
        return new Rect((screenWidth - width) * 0.5f, y, width, panelHeight);
    }

    private static Rect MemoryTextRect(Rect panel) =>
        new Rect(panel.x + 18f, panel.y + 34f, panel.width - 36f, panel.height - 82f);

    internal static string CompactControlHintText(string movement, string aim, string attack, string dodge, string pulse, string interact) =>
        "Mover: " + movement + " · mirar: " + aim + "\n" +
        attack + " atacar · " + dodge + " esquivar · " + pulse + " pulso\n" +
        interact + " interagir · M/L3 mapa · Esc/Start pausa";

    private static bool MemoryPanelFits(float screenWidth, float screenHeight, float bodyHeight)
    {
        float requiredBodyHeight = Mathf.Max(62f, bodyHeight);
        Rect panel = MemoryPanelRect(screenWidth, screenHeight, requiredBodyHeight);
        Rect text = MemoryTextRect(panel);
        Rect hint = MemoryDismissHintRect(panel);
        return screenWidth >= 240f && screenHeight >= requiredBodyHeight + 106f &&
            panel.xMin >= 0f && panel.xMax <= screenWidth && panel.yMin >= 12f && panel.yMax <= screenHeight - 12f &&
            text.yMin >= panel.yMin && text.yMax <= hint.yMin && text.height >= requiredBodyHeight &&
            hint.yMin >= text.yMax && hint.yMax <= panel.yMax;
    }

    internal static int FitLabelFontSize(int fontSize, float measuredWidth, float availableWidth, int minimum = 9)
    {
        int baseSize = Mathf.Max(1, fontSize);
        int minSize = Mathf.Clamp(minimum, 1, baseSize);
        if (measuredWidth <= 0f || measuredWidth <= availableWidth) return baseSize;
        return Mathf.Max(minSize, Mathf.FloorToInt(baseSize * Mathf.Max(0f, availableWidth) / measuredWidth));
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
        if (Screen.height < 600f)
        {
            DrawCompactOptionsContent(x, y, width);
            return;
        }
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
                if (OptionButton(keyButton, label, i + 3))
                {
                    rebindingAction = i;
                    keyboardRebindAfterFrame = Time.frameCount;
                }
            }
            if (rebindingAction >= 0 && CanCaptureRebindKey(Time.frameCount, keyboardRebindAfterFrame) &&
                Event.current.type == EventType.KeyDown && ScadrykSettings.CanBindKey(Event.current.keyCode))
            {
                ScadrykSettings.Bind(rebindingAction, Event.current.keyCode);
                rebindingAction = -1;
                Event.current.Use();
            }
        }
        else if (optionsTab == 2)
        {
            GUI.Label(new Rect(x + 15f, y + 99f, width - 185f, 31f), "Botões e zona morta do analógico.", smallStyle);
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
            GUI.Label(new Rect(x + 15f, y + 445f, 210f, 30f), "Zona morta: " + Mathf.RoundToInt(ScadrykSettings.GamepadDeadzone * 100f) + "%", bodyStyle);
            ScadrykSettings.GamepadDeadzone = GUI.HorizontalSlider(new Rect(x + 240f, y + 450f, width - 255f, 24f),
                ScadrykSettings.GamepadDeadzone, ScadrykSettings.MinimumGamepadDeadzone, ScadrykSettings.MaximumGamepadDeadzone);
            if (optionsSelection == 11) DrawSelection(new Rect(x + 10f, y + 438f, width - 20f, 72f));
            GUI.Label(new Rect(x + 15f, y + 480f, width - 30f, 46f), GamepadAnalogReadout(false), smallStyle);
        }
        if (OptionButton(new Rect(x + (width - 180f) * 0.5f, y + backOffset, 180f, 42f), "Voltar", optionsTab == 0 ? 9 : optionsTab == 1 ? 15 : 12)) { showOptions = false; rebindingAction = gamepadRebindAction = -1; }
        GUI.Label(new Rect(x, y + footerOffset, width, 28f), "Controle: analógico navega · A confirma · B volta · Start pausa · L3 mapa.", smallStyle);
    }

    private void DrawCompactOptionsContent(float x, float y, float width)
    {
        const float backHeight = 42f;
        const float footerHeight = 28f;
        float backY = Screen.height - 78f;
        float viewportY = y + 94f;
        float viewportHeight = Mathf.Max(76f, backY - viewportY - 8f);
        float viewportWidth = Mathf.Max(120f, width - 16f);
        float contentHeight = CompactOptionsContentHeight(optionsTab);
        if (lastCompactOptionsTab != optionsTab || lastCompactOptionsSelection != optionsSelection)
        {
            Rect selected = CompactOptionsSelectionRect(optionsTab, optionsSelection);
            compactOptionsScroll.y = ScrollOptionItemIntoView(compactOptionsScroll.y, viewportHeight,
                selected.y, selected.height, contentHeight);
            lastCompactOptionsTab = optionsTab;
            lastCompactOptionsSelection = optionsSelection;
        }
        compactOptionsScroll.x = 0f;

        Rect viewport = new Rect(x + 8f, viewportY, viewportWidth, viewportHeight);
        Rect content = new Rect(0f, 0f, viewportWidth, contentHeight);
        compactOptionsScroll = GUI.BeginScrollView(viewport, compactOptionsScroll, content);
        if (optionsTab == 0) DrawCompactSoundOptions(viewportWidth);
        else if (optionsTab == 1) DrawCompactKeyboardOptions(viewportWidth);
        else DrawCompactGamepadOptions(viewportWidth);
        GUI.EndScrollView();

        float backWidth = Mathf.Min(180f, width - 24f);
        Rect back = new Rect(x + (width - backWidth) * 0.5f, backY, backWidth, backHeight);
        int backSelection = optionsTab == 0 ? 9 : optionsTab == 1 ? 15 : 12;
        if (OptionButton(back, "Voltar", backSelection)) { showOptions = false; rebindingAction = gamepadRebindAction = -1; }
        GUI.Label(new Rect(x + 4f, Screen.height - footerHeight - 2f, width - 8f, footerHeight),
            "Analógico: navegar · A: confirmar · B: voltar", smallStyle);
    }

    private void DrawCompactSoundOptions(float width)
    {
        const float left = 12f;
        float labelWidth = Mathf.Clamp(width * 0.42f, 88f, 146f);
        float sliderX = left + labelWidth + 10f;
        float sliderWidth = Mathf.Max(48f, width - sliderX - 16f);
        DrawCompactOptionSelection(3, 10f, 48f, width);
        GUI.Label(new Rect(left, 16f, labelWidth, 28f), "Música", bodyStyle);
        ScadrykSettings.MusicVolume = GUI.HorizontalSlider(new Rect(sliderX, 18f, sliderWidth, 24f), ScadrykSettings.MusicVolume, 0f, 1f);
        DrawCompactOptionSelection(4, 62f, 48f, width);
        GUI.Label(new Rect(left, 68f, labelWidth, 28f), "Efeitos", bodyStyle);
        ScadrykSettings.EffectsVolume = GUI.HorizontalSlider(new Rect(sliderX, 70f, sliderWidth, 24f), ScadrykSettings.EffectsVolume, 0f, 1f);
        DrawCompactOptionSelection(5, 114f, 48f, width);
        GUI.Label(new Rect(left, 120f, labelWidth, 28f), "Escala do texto", bodyStyle);
        float textScale = GUI.HorizontalSlider(new Rect(sliderX, 122f, sliderWidth, 24f), ScadrykSettings.TextScale, 1f, 1.5f);
        if (Mathf.Abs(textScale - ScadrykSettings.TextScale) > 0.01f) { ScadrykSettings.TextScale = textScale; titleStyle = null; }

        DrawCompactOptionSelection(6, 176f, 40f, width);
        ScadrykSettings.ReduceFlash = GUI.Toggle(new Rect(left, 179f, width - 24f, 34f), ScadrykSettings.ReduceFlash, "Reduzir flashes de dano");
        DrawCompactOptionSelection(7, 220f, 40f, width);
        ScadrykSettings.ReduceShake = GUI.Toggle(new Rect(left, 223f, width - 24f, 34f), ScadrykSettings.ReduceShake, "Desligar tremor de câmera");
        DrawCompactOptionSelection(8, 264f, 40f, width);
        ScadrykSettings.HighContrastWarnings = GUI.Toggle(new Rect(left, 267f, width - 24f, 34f), ScadrykSettings.HighContrastWarnings, "Alto contraste nos avisos");
        GUI.Label(new Rect(left, 310f, width - 24f, 78f),
            "Avisos continuam visíveis sem efeitos sonoros. F8 marca um sinal difícil de ler durante QA.", memoryStyle);
    }

    private void DrawCompactKeyboardOptions(float width)
    {
        GUI.Label(new Rect(12f, 5f, width - 150f, 32f), "Selecione uma ação para trocar a tecla.", smallStyle);
        if (OptionButton(new Rect(width - 142f, 3f, 130f, 34f), "Padrões", 14))
        {
            ScadrykSettings.ResetKeyboardBindings();
            rebindingAction = -1;
            ShowStatus("Teclas padrão restauradas.", 2f);
        }
        float labelWidth = Mathf.Clamp(width * 0.38f, 95f, 138f);
        for (int i = 0; i < ScadrykSettings.Actions.Length; i++)
        {
            float row = 44f + i * 36f;
            GUI.Label(new Rect(12f, row, labelWidth, 30f), ScadrykSettings.Actions[i], bodyStyle);
            string label = rebindingAction == i ? "Aperte…" : ScadrykSettings.KeyName(ScadrykSettings.Key(i));
            if (OptionButton(new Rect(labelWidth + 20f, row, width - labelWidth - 32f, 30f), label, i + 3))
            {
                rebindingAction = i;
                keyboardRebindAfterFrame = Time.frameCount;
            }
        }
        if (rebindingAction >= 0 && CanCaptureRebindKey(Time.frameCount, keyboardRebindAfterFrame) &&
            Event.current.type == EventType.KeyDown && ScadrykSettings.CanBindKey(Event.current.keyCode))
        {
            ScadrykSettings.Bind(rebindingAction, Event.current.keyCode);
            rebindingAction = -1;
            Event.current.Use();
        }
    }

    private void DrawCompactGamepadOptions(float width)
    {
        GUI.Label(new Rect(12f, 5f, width - 150f, 32f), "Selecione a ação e pressione o botão.", smallStyle);
        if (OptionButton(new Rect(width - 142f, 3f, 130f, 34f), "Padrões", 10))
        {
            ScadrykSettings.ResetGamepadBindings();
            gamepadRebindAction = -1;
            ShowStatus("Controle padrão restaurado.", 2f);
        }
        float labelWidth = Mathf.Clamp(width * 0.38f, 95f, 138f);
        for (int i = 0; i < 7; i++)
        {
            float row = 44f + i * 40f;
            GUI.Label(new Rect(12f, row, labelWidth, 32f), ScadrykSettings.Actions[i], bodyStyle);
            string label = gamepadRebindAction == i ? "Aperte…" : ScadrykSettings.PadButtonName(ScadrykSettings.PadButton(i));
            if (OptionButton(new Rect(labelWidth + 20f, row, width - labelWidth - 32f, 32f), label, i + 3))
            { gamepadRebindAction = i; gamepadRebindAfterFrame = Time.frameCount; }
        }
        DrawCompactOptionSelection(11, 328f, 108f, width);
        GUI.Label(new Rect(12f, 334f, labelWidth, 28f), "Zona morta: " + Mathf.RoundToInt(ScadrykSettings.GamepadDeadzone * 100f) + "%", bodyStyle);
        ScadrykSettings.GamepadDeadzone = GUI.HorizontalSlider(new Rect(labelWidth + 20f, 338f, width - labelWidth - 32f, 24f),
            ScadrykSettings.GamepadDeadzone, ScadrykSettings.MinimumGamepadDeadzone, ScadrykSettings.MaximumGamepadDeadzone);
        GUI.Label(new Rect(12f, 372f, width - 24f, 62f), GamepadAnalogReadout(true), smallStyle);
    }

    private static string GamepadAnalogReadout(bool compact)
    {
        float deadzone = ScadrykSettings.GamepadDeadzone;
        Vector2 movement = new Vector2(Input.GetAxisRaw("Scadryk Move X"), Input.GetAxisRaw("Scadryk Move Y"));
        Vector2 aim = new Vector2(Input.GetAxisRaw("Scadryk Aim X"), Input.GetAxisRaw("Scadryk Aim Y"));
        string movementText = "Mover " + ScadrykSettings.GamepadStickReadout(movement, deadzone);
        string aimText = "Mirar " + ScadrykSettings.GamepadStickReadout(aim, deadzone);
        return compact ? "Eixos X/Y · força bruta → útil\n" + movementText + "\n" + aimText :
            "Eixos X/Y · força bruta → útil\n" + movementText + " · " + aimText;
    }

    private void DrawCompactOptionSelection(int selection, float top, float height, float width)
    {
        if (optionsSelection == selection) DrawSelection(new Rect(4f, top, width - 8f, height));
    }

    internal static float CompactOptionsContentHeight(int tab) => tab == 0 ? 390f : tab == 1 ? 474f : 436f;

    internal static Rect CompactOptionsSelectionRect(int tab, int selection)
    {
        if (tab == 0 && selection is >= 3 and <= 8)
        {
            float[] tops = { 10f, 62f, 114f, 176f, 220f, 264f };
            int index = selection - 3;
            return new Rect(0f, tops[index], 1f, index < 3 ? 48f : 40f);
        }
        if (tab == 1)
        {
            if (selection == 14) return new Rect(0f, 3f, 1f, 34f);
            if (selection is >= 3 and <= 13) return new Rect(0f, 44f + (selection - 3) * 36f, 1f, 30f);
        }
        if (tab == 2)
        {
            if (selection == 10) return new Rect(0f, 3f, 1f, 34f);
            if (selection is >= 3 and <= 9) return new Rect(0f, 44f + (selection - 3) * 40f, 1f, 32f);
            if (selection == 11) return new Rect(0f, 328f, 1f, 108f);
        }
        return new Rect(0f, 0f, 1f, 1f);
    }

    internal static float ScrollOptionItemIntoView(float current, float viewportHeight, float itemTop, float itemHeight, float contentHeight)
    {
        if (itemTop < current) current = itemTop;
        else if (itemTop + itemHeight > current + viewportHeight) current = itemTop + itemHeight - viewportHeight;
        return Mathf.Clamp(current, 0f, Mathf.Max(0f, contentHeight - viewportHeight));
    }

    internal static float CompactOptionsViewportHeight(float screenHeight) =>
        Mathf.Max(76f, screenHeight - 78f - 8f - 94f - 8f);

    internal static bool CompactOptionsLayoutFits(float screenWidth, float screenHeight)
    {
        float width = Mathf.Min(620f, screenWidth - 36f);
        float backY = screenHeight - 78f;
        float footerY = screenHeight - 30f;
        float viewportHeight = CompactOptionsViewportHeight(screenHeight);
        return width >= 240f && viewportHeight >= 76f && backY + 42f <= footerY && footerY + 28f <= screenHeight &&
            CompactOptionsContentHeight(1) > viewportHeight;
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
        statusStyle = new GUIStyle(bodyStyle) { wordWrap = true, alignment = TextAnchor.MiddleCenter };
        smallStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(11f * scale), normal = { textColor = new Color(0.72f, 0.76f, 0.72f) }, alignment = TextAnchor.MiddleLeft };
        hudObjectiveStyle = new GUIStyle(smallStyle) { wordWrap = true, alignment = TextAnchor.UpperLeft };
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
