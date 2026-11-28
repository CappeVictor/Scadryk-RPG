using UnityEngine;

public sealed partial class ScadrykGameDirector
{
    private void DrawOptions()
    {
        GUI.color = new Color(0.01f, 0.02f, 0.025f, 0.97f); GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture); GUI.color = Color.white;
        bool compact = Screen.height < 720f;
        float width = Mathf.Min(620f, Screen.width - 36f), x = (Screen.width - width) * 0.5f;
        float y = compact ? 8f : Mathf.Max(18f, (Screen.height - 690f) * 0.5f);
        float keyRowStart = compact ? 130f : 135f, keyRowStep = compact ? 32f : 38f, keyRowHeight = compact ? 30f : 32f;
        float padRowStart = compact ? 130f : 135f, padRowStep = compact ? 40f : 45f, padRowHeight = compact ? 30f : 32f;
        float backOffset = compact ? 512f : 585f, footerOffset = compact ? 560f : 637f;
        GUI.Label(new Rect(x, y, width, 42f), "OPÇÕES", centeredTitleStyle);
        float tabWidth = (width - 46f) / 3f;
        if (OptionButton(new Rect(x + 15f, y + 48f, tabWidth, 38f), "Tela e som", 0)) { optionsTab = 0; optionsSelection = 0; }
        if (OptionButton(new Rect(x + 23f + tabWidth, y + 48f, tabWidth, 38f), "Teclas", 1)) { optionsTab = 1; optionsSelection = 1; }
        if (OptionButton(new Rect(x + 31f + tabWidth * 2f, y + 48f, tabWidth, 38f), "Controle", 2)) { optionsTab = 2; optionsSelection = 2; }
        if (Screen.height < 720f)
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
            float displayRowX = x + 15f;
            float displayRowWidth = width - 30f;
            float sliderReadoutWidth = width >= 330f ? 44f : 0f;
            float optionSliderWidth = width - 215f - (sliderReadoutWidth > 0f ? sliderReadoutWidth + 10f : 0f);
            float fullscreenWidth = (displayRowWidth - 8f) * 0.56f;
            Rect fullscreenRect = new Rect(displayRowX, y + 402f, fullscreenWidth, 36f);
            Rect frameRateRect = new Rect(displayRowX + fullscreenWidth + 8f, y + 402f,
                displayRowWidth - fullscreenWidth - 8f, 36f);
            if (optionsSelection == 9) DrawSelection(new Rect(fullscreenRect.x - 5f, fullscreenRect.y - 4f, fullscreenRect.width + 10f, fullscreenRect.height + 8f));
            GUI.Label(new Rect(x + 15f, y + 125f, 170f, 28f), "Música", bodyStyle);
            ScadrykSettings.MusicVolume = GUI.HorizontalSlider(new Rect(x + 195f, y + 135f, optionSliderWidth, 24f), ScadrykSettings.MusicVolume, 0f, 1f);
            if (sliderReadoutWidth > 0f) GUI.Label(new Rect(x + width - sliderReadoutWidth - 12f, y + 125f, sliderReadoutWidth, 28f), Mathf.RoundToInt(ScadrykSettings.MusicVolume * 100f) + "%", smallStyle);
            GUI.Label(new Rect(x + 15f, y + 175f, 170f, 28f), "Efeitos", bodyStyle);
            ScadrykSettings.EffectsVolume = GUI.HorizontalSlider(new Rect(x + 195f, y + 185f, optionSliderWidth, 24f), ScadrykSettings.EffectsVolume, 0f, 1f);
            if (sliderReadoutWidth > 0f) GUI.Label(new Rect(x + width - sliderReadoutWidth - 12f, y + 175f, sliderReadoutWidth, 28f), Mathf.RoundToInt(ScadrykSettings.EffectsVolume * 100f) + "%", smallStyle);
            GUI.Label(new Rect(x + 15f, y + 225f, 170f, 28f), "Tamanho do texto", bodyStyle);
            float scale = GUI.HorizontalSlider(new Rect(x + 195f, y + 235f, optionSliderWidth, 24f), ScadrykSettings.TextScale, 1f, 1.5f);
            if (Mathf.Abs(scale - ScadrykSettings.TextScale) > 0.01f) { ScadrykSettings.TextScale = scale; titleStyle = null; }
            if (sliderReadoutWidth > 0f) GUI.Label(new Rect(x + width - sliderReadoutWidth - 12f, y + 225f, sliderReadoutWidth, 28f), Mathf.RoundToInt(ScadrykSettings.TextScale * 100f) + "%", smallStyle);
            ScadrykSettings.ReduceFlash = GUI.Toggle(new Rect(x + 15f, y + 285f, width - 30f, 34f), ScadrykSettings.ReduceFlash, "Reduzir flashes e rastros");
            ScadrykSettings.ReduceShake = GUI.Toggle(new Rect(x + 15f, y + 325f, width - 30f, 34f), ScadrykSettings.ReduceShake, "Desligar tremor de câmera");
            ScadrykSettings.HighContrastWarnings = GUI.Toggle(new Rect(x + 15f, y + 365f, width - 30f, 34f), ScadrykSettings.HighContrastWarnings, "Alto contraste nos avisos de perigo");
            ScadrykSettings.Fullscreen = GUI.Toggle(fullscreenRect, ScadrykSettings.Fullscreen, "Tela cheia");
            if (OptionButton(frameRateRect, ScadrykSettings.FrameRateModeLabel(ScadrykSettings.FrameRateMode), 10))
                ScadrykSettings.FrameRateMode = ScadrykSettings.NextFrameRateMode(ScadrykSettings.FrameRateMode, 1);
            GUI.Label(new Rect(x + 15f, y + 444f, width - 30f, 60f), "Avisos permanecem visíveis com os efeitos em silêncio. " +
                InputBinding(ScadrykSettings.UnclearAttackAction) + " registra um ataque pouco claro durante testes.", memoryStyle);
        }
        else if (optionsTab == 1)
        {
            GUI.Label(new Rect(x + 15f, y + 99f, width - 185f, 31f), "Clique numa ação para remapear; M abre o mapa.", smallStyle);
            if (OptionButton(new Rect(x + width - 155f, y + 95f, 140f, 32f), "Padrões", 15))
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
            if (OptionButton(new Rect(x + width - 155f, y + 95f, 140f, 32f), "Padrões", 11))
            {
                ScadrykSettings.ResetGamepadBindings();
                gamepadRebindAction = -1;
                ShowStatus("Controle padrão restaurado.", 2f);
            }
            for (int i = 0; i < 8; i++)
            {
                float row = y + padRowStart + i * padRowStep;
                int action = ScadrykSettings.PadOptionAction(i);
                GUI.Label(new Rect(x + 15f, row, 220f, padRowHeight), ScadrykSettings.Actions[action], bodyStyle);
                string label = gamepadRebindAction == action ? "Pressione um botão…" : ScadrykSettings.PadButtonName(ScadrykSettings.PadButton(action));
                if (OptionButton(new Rect(x + 240f, row, width - 255f, padRowHeight), label, i + 3)) { gamepadRebindAction = action; gamepadRebindAfterFrame = Time.frameCount; }
            }
            GUI.Label(new Rect(x + 15f, y + 490f, 210f, 30f), "Zona morta: " + Mathf.RoundToInt(ScadrykSettings.GamepadDeadzone * 100f) + "%", bodyStyle);
            ScadrykSettings.GamepadDeadzone = GUI.HorizontalSlider(new Rect(x + 240f, y + 495f, width - 255f, 24f),
                ScadrykSettings.GamepadDeadzone, ScadrykSettings.MinimumGamepadDeadzone, ScadrykSettings.MaximumGamepadDeadzone);
            if (optionsSelection == 12) DrawSelection(new Rect(x + 10f, y + 484f, width - 20f, 88f));
            GUI.Label(new Rect(x + 15f, y + 525f, width - 30f, 48f), GamepadAnalogReadout(false), smallStyle);
        }
        if (OptionButton(new Rect(x + (width - 180f) * 0.5f, y + backOffset, 180f, 42f), "Voltar", optionsTab == 0 ? 11 : optionsTab == 1 ? 16 : 13)) { showOptions = false; rebindingAction = gamepadRebindAction = -1; }
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
        int backSelection = optionsTab == 0 ? 11 : optionsTab == 1 ? 16 : 13;
        if (OptionButton(back, "Voltar", backSelection)) { showOptions = false; rebindingAction = gamepadRebindAction = -1; }
        GUI.Label(new Rect(x + 4f, Screen.height - footerHeight - 2f, width - 8f, footerHeight),
            "Analógico: navegar · A: confirmar · B: voltar", smallStyle);
    }

    private void DrawCompactSoundOptions(float width)
    {
        const float left = 12f;
        float labelWidth = Mathf.Clamp(width * 0.42f, 88f, 146f);
        float sliderX = left + labelWidth + 10f;
        float valueWidth = width >= 270f ? 44f : 0f;
        float sliderWidth = Mathf.Max(48f, width - sliderX - 16f - (valueWidth > 0f ? valueWidth + 10f : 0f));
        DrawCompactOptionSelection(3, 10f, 48f, width);
        GUI.Label(new Rect(left, 16f, labelWidth, 28f), "Música", bodyStyle);
        ScadrykSettings.MusicVolume = GUI.HorizontalSlider(new Rect(sliderX, 18f, sliderWidth, 24f), ScadrykSettings.MusicVolume, 0f, 1f);
        if (valueWidth > 0f) GUI.Label(new Rect(sliderX + sliderWidth + 6f, 16f, valueWidth, 28f), Mathf.RoundToInt(ScadrykSettings.MusicVolume * 100f) + "%", smallStyle);
        DrawCompactOptionSelection(4, 62f, 48f, width);
        GUI.Label(new Rect(left, 68f, labelWidth, 28f), "Efeitos", bodyStyle);
        ScadrykSettings.EffectsVolume = GUI.HorizontalSlider(new Rect(sliderX, 70f, sliderWidth, 24f), ScadrykSettings.EffectsVolume, 0f, 1f);
        if (valueWidth > 0f) GUI.Label(new Rect(sliderX + sliderWidth + 6f, 68f, valueWidth, 28f), Mathf.RoundToInt(ScadrykSettings.EffectsVolume * 100f) + "%", smallStyle);
        DrawCompactOptionSelection(5, 114f, 48f, width);
        GUI.Label(new Rect(left, 120f, labelWidth, 28f), "Escala do texto", bodyStyle);
        float textScale = GUI.HorizontalSlider(new Rect(sliderX, 122f, sliderWidth, 24f), ScadrykSettings.TextScale, 1f, 1.5f);
        if (Mathf.Abs(textScale - ScadrykSettings.TextScale) > 0.01f) { ScadrykSettings.TextScale = textScale; titleStyle = null; }
        if (valueWidth > 0f) GUI.Label(new Rect(sliderX + sliderWidth + 6f, 120f, valueWidth, 28f), Mathf.RoundToInt(ScadrykSettings.TextScale * 100f) + "%", smallStyle);

        DrawCompactOptionSelection(6, 176f, 40f, width);
        ScadrykSettings.ReduceFlash = GUI.Toggle(new Rect(left, 179f, width - 24f, 34f), ScadrykSettings.ReduceFlash, "Reduzir flashes e rastros");
        DrawCompactOptionSelection(7, 220f, 40f, width);
        ScadrykSettings.ReduceShake = GUI.Toggle(new Rect(left, 223f, width - 24f, 34f), ScadrykSettings.ReduceShake, "Desligar tremor de câmera");
        DrawCompactOptionSelection(8, 264f, 40f, width);
        ScadrykSettings.HighContrastWarnings = GUI.Toggle(new Rect(left, 267f, width - 24f, 34f), ScadrykSettings.HighContrastWarnings, "Alto contraste nos avisos");
        GUI.Label(new Rect(left, 310f, width - 24f, 78f),
            "Avisos continuam visíveis sem efeitos sonoros. " + InputBinding(ScadrykSettings.UnclearAttackAction) +
            " marca um sinal difícil de ler durante os testes.", memoryStyle);
        DrawCompactOptionSelection(9, 392f, 40f, width);
        DrawCompactOptionSelection(10, 392f, 40f, width);
        float toggleWidth = Mathf.Max(80f, (width - 36f) * 0.52f);
        ScadrykSettings.Fullscreen = GUI.Toggle(new Rect(left, 395f, toggleWidth, 34f), ScadrykSettings.Fullscreen, "Tela cheia");
        Rect frameRateRect = new Rect(left + toggleWidth + 8f, 395f, Mathf.Max(72f, width - toggleWidth - left - 20f), 34f);
        if (OptionButton(frameRateRect, ScadrykSettings.FrameRateModeLabel(ScadrykSettings.FrameRateMode), 10))
            ScadrykSettings.FrameRateMode = ScadrykSettings.NextFrameRateMode(ScadrykSettings.FrameRateMode, 1);
    }

    private void DrawCompactKeyboardOptions(float width)
    {
        GUI.Label(new Rect(12f, 5f, width - 150f, 32f), "Selecione uma ação para trocar a tecla.", smallStyle);
        if (OptionButton(new Rect(width - 142f, 3f, 130f, 34f), "Padrões", 15))
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
        if (OptionButton(new Rect(width - 142f, 3f, 130f, 34f), "Padrões", 11))
        {
            ScadrykSettings.ResetGamepadBindings();
            gamepadRebindAction = -1;
            ShowStatus("Controle padrão restaurado.", 2f);
        }
        float labelWidth = Mathf.Clamp(width * 0.38f, 95f, 138f);
        for (int i = 0; i < 8; i++)
        {
            float row = 44f + i * 40f;
            int action = ScadrykSettings.PadOptionAction(i);
            GUI.Label(new Rect(12f, row, labelWidth, 32f), ScadrykSettings.Actions[action], bodyStyle);
            string label = gamepadRebindAction == action ? "Aperte…" : ScadrykSettings.PadButtonName(ScadrykSettings.PadButton(action));
            if (OptionButton(new Rect(labelWidth + 20f, row, width - labelWidth - 32f, 32f), label, i + 3))
            { gamepadRebindAction = action; gamepadRebindAfterFrame = Time.frameCount; }
        }
        DrawCompactOptionSelection(12, 374f, 108f, width);
        GUI.Label(new Rect(12f, 380f, labelWidth, 28f), "Zona morta: " + Mathf.RoundToInt(ScadrykSettings.GamepadDeadzone * 100f) + "%", bodyStyle);
        ScadrykSettings.GamepadDeadzone = GUI.HorizontalSlider(new Rect(labelWidth + 20f, 384f, width - labelWidth - 32f, 24f),
            ScadrykSettings.GamepadDeadzone, ScadrykSettings.MinimumGamepadDeadzone, ScadrykSettings.MaximumGamepadDeadzone);
        GUI.Label(new Rect(12f, 418f, width - 24f, 62f), GamepadAnalogReadout(true), smallStyle);
    }

    private static string GamepadAnalogReadout(bool compact)
    {
        float deadzone = ScadrykSettings.GamepadDeadzone;
        Vector2 movement = new Vector2(Input.GetAxisRaw("Scadryk Move X"), Input.GetAxisRaw("Scadryk Move Y"));
        Vector2 aim = ScadrykPlayerController.NormalizeGamepadAimStick(
            new Vector2(Input.GetAxisRaw("Scadryk Aim X"), Input.GetAxisRaw("Scadryk Aim Y")));
        string movementText = "Mover " + ScadrykSettings.GamepadStickReadout(movement, deadzone);
        string aimText = "Mirar " + ScadrykSettings.GamepadStickReadout(aim, deadzone);
        return compact ? "Eixos X/Y · força bruta → útil\n" + movementText + "\n" + aimText :
            "Eixos X/Y · força bruta → útil\n" + movementText + " · " + aimText;
    }

    private void DrawCompactOptionSelection(int selection, float top, float height, float width)
    {
        if (optionsSelection == selection) DrawSelection(new Rect(4f, top, width - 8f, height));
    }

    internal static float CompactOptionsContentHeight(int tab) => tab == 0 ? 438f : tab == 1 ? 474f : 482f;

    internal static Rect CompactOptionsSelectionRect(int tab, int selection)
    {
        if (tab == 0 && selection is >= 3 and <= 8)
        {
            float[] tops = { 10f, 62f, 114f, 176f, 220f, 264f };
            int index = selection - 3;
            return new Rect(0f, tops[index], 1f, index < 3 ? 48f : 40f);
        }
        if (tab == 0 && selection is 9 or 10) return new Rect(0f, 392f, 1f, 40f);
        if (tab == 1)
        {
            if (selection == 15) return new Rect(0f, 3f, 1f, 34f);
            if (selection is >= 3 and <= 14) return new Rect(0f, 44f + (selection - 3) * 36f, 1f, 30f);
        }
        if (tab == 2)
        {
            if (selection == 11) return new Rect(0f, 3f, 1f, 34f);
            if (selection is >= 3 and <= 10) return new Rect(0f, 44f + (selection - 3) * 40f, 1f, 32f);
            if (selection == 12) return new Rect(0f, 374f, 1f, 108f);
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
            Mathf.Max(CompactOptionsContentHeight(1), CompactOptionsContentHeight(2)) > viewportHeight;
    }

    private static bool OptionsLayoutFits(float screenHeight)
    {
        bool compact = screenHeight < 720f;
        float y = compact ? 8f : Mathf.Max(18f, (screenHeight - 690f) * 0.5f);
        float rowStart = compact ? 130f : 135f, rowStep = compact ? 32f : 38f, rowHeight = compact ? 30f : 32f;
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
        float fullscreenToggleBottom = y + 402f + 36f;
        float frameRateButtonBottom = y + 402f + 36f;
        float warningHelpBottom = y + 444f + 60f;
        float backTop = y + (compact ? 512f : 585f);
        float footerBottom = y + (compact ? 560f : 637f) + 28f;
        return fullscreenToggleBottom <= warningHelpBottom && frameRateButtonBottom <= warningHelpBottom &&
            warningHelpBottom <= backTop && footerBottom <= screenHeight;
    }

    private bool OptionButton(Rect rect, string label, int selection)
    {
        if (optionsSelection == selection) DrawSelection(rect);
        bool clicked = GUI.Button(rect, label);
        optionsSelection = OptionSelectionAfterClick(clicked, optionsSelection, selection);
        return clicked;
    }

    internal static int OptionSelectionAfterClick(bool clicked, int previousSelection, int clickedSelection) =>
        clicked ? clickedSelection : previousSelection;

}
