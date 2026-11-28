using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed partial class ScadrykGameDirector
{
#if DEBUG || UNITY_EDITOR || SCADRYK_SMOKE
    private static bool SmokeBindingResetIsolation()
    {
        const int keyboardCount = 12, gamepadCount = 7;
        string testPrefix = "Scadryk.Smoke." + System.DateTime.UtcNow.Ticks + ".bindings.";
        string keyPrefix = testPrefix + "Key.";
        string padPrefix = testPrefix + "Pad.";
        string unclearPadKey = padPrefix + "UnclearAttack";
        try
        {
            KeyCode[] defaultKeys = { KeyCode.Space, KeyCode.LeftShift, KeyCode.Q, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.E, KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D, KeyCode.F8 };
            int[] defaultButtons = { 0, 1, 2, 4, 5, 6, 3 };
            PlayerPrefs.SetInt(keyPrefix + "0", (int)KeyCode.M);
            PlayerPrefs.SetInt(padPrefix + "0", 8);
            PlayerPrefs.SetInt(keyPrefix + "2", 987654);
            PlayerPrefs.SetInt(keyPrefix + "1", (int)KeyCode.G);
            PlayerPrefs.SetInt(padPrefix + "1", 19);
            PlayerPrefs.SetInt(unclearPadKey, 8);
            ScadrykSettings.ClearReservedBindings(keyPrefix, padPrefix, unclearPadKey);
            bool reservedBindingsCleared = ScadrykSettings.Key(0, keyPrefix) == defaultKeys[0] &&
                ScadrykSettings.Key(2, keyPrefix) == defaultKeys[2] &&
                ScadrykSettings.PadButton(0, padPrefix) == defaultButtons[0] &&
                ScadrykSettings.Key(1, keyPrefix) == KeyCode.G && ScadrykSettings.PadButton(1, padPrefix) == 19 &&
                ScadrykSettings.PadButton(ScadrykSettings.UnclearAttackAction, padPrefix) == 9;

            PlayerPrefs.SetInt(keyPrefix + "0", (int)KeyCode.F);
            PlayerPrefs.SetInt(padPrefix + "0", 19);
            ScadrykSettings.ResetKeyboardBindings(keyPrefix);
            bool keyboardDefaultsRestored = true;
            for (int i = 0; i < defaultKeys.Length; i++)
                keyboardDefaultsRestored &= ScadrykSettings.Key(i, keyPrefix) == defaultKeys[i];
            PlayerPrefs.SetInt(unclearPadKey, 18);
            bool gamepadSurvivedKeyboardReset = ScadrykSettings.PadButton(0, padPrefix) == 19 &&
                ScadrykSettings.PadButton(ScadrykSettings.UnclearAttackAction, padPrefix) == 18;

            PlayerPrefs.SetInt(keyPrefix + "1", (int)KeyCode.G);
            ScadrykSettings.ResetGamepadBindings(padPrefix);
            bool gamepadDefaultsRestored = true;
            for (int i = 0; i < defaultButtons.Length; i++)
                gamepadDefaultsRestored &= ScadrykSettings.PadButton(i, padPrefix) == defaultButtons[i];
            gamepadDefaultsRestored &= ScadrykSettings.PadButton(ScadrykSettings.UnclearAttackAction, padPrefix) == 9;
            bool keyboardSurvivedGamepadReset = ScadrykSettings.Key(1, keyPrefix) == KeyCode.G;
            return reservedBindingsCleared && keyboardDefaultsRestored && gamepadSurvivedKeyboardReset && gamepadDefaultsRestored && keyboardSurvivedGamepadReset;
        }
        finally
        {
            for (int i = 0; i < keyboardCount; i++) PlayerPrefs.DeleteKey(keyPrefix + i);
            for (int i = 0; i < gamepadCount; i++) PlayerPrefs.DeleteKey(padPrefix + i);
            PlayerPrefs.DeleteKey(unclearPadKey);
            PlayerPrefs.Save();
        }
    }

    private static bool SmokeSettingsPreferenceNormalization()
    {
        string[] keys = { "Scadryk.Music", "Scadryk.Effects", "Scadryk.GamepadDeadzone", "Scadryk.TextScale" };
        var existed = new bool[keys.Length];
        var values = new float[keys.Length];
        for (int i = 0; i < keys.Length; i++)
        {
            existed[i] = PlayerPrefs.HasKey(keys[i]);
            values[i] = PlayerPrefs.GetFloat(keys[i]);
        }

        try
        {
            PlayerPrefs.SetFloat(keys[0], float.NaN);
            PlayerPrefs.SetFloat(keys[1], float.PositiveInfinity);
            PlayerPrefs.SetFloat(keys[2], float.NegativeInfinity);
            PlayerPrefs.SetFloat(keys[3], float.PositiveInfinity);
            bool invalidReadsUseSafeValues =
                ScadrykSettings.MusicVolume == ScadrykSettings.DefaultMusicVolume &&
                ScadrykSettings.EffectsVolume == ScadrykSettings.DefaultEffectsVolume &&
                ScadrykSettings.GamepadDeadzone == ScadrykSettings.DefaultGamepadDeadzone &&
                ScadrykSettings.TextScale == 1f;

            ScadrykSettings.MusicVolume = 0.1f;
            ScadrykSettings.MusicVolume = float.NaN;
            ScadrykSettings.EffectsVolume = 0.2f;
            ScadrykSettings.EffectsVolume = float.PositiveInfinity;
            ScadrykSettings.GamepadDeadzone = 0.35f;
            ScadrykSettings.GamepadDeadzone = float.NegativeInfinity;
            ScadrykSettings.TextScale = 1.4f;
            ScadrykSettings.TextScale = float.NaN;
            return invalidReadsUseSafeValues &&
                Mathf.Abs(PlayerPrefs.GetFloat(keys[0]) - ScadrykSettings.DefaultMusicVolume) < 0.001f &&
                Mathf.Abs(PlayerPrefs.GetFloat(keys[1]) - ScadrykSettings.DefaultEffectsVolume) < 0.001f &&
                Mathf.Abs(PlayerPrefs.GetFloat(keys[2]) - ScadrykSettings.DefaultGamepadDeadzone) < 0.001f &&
                Mathf.Abs(PlayerPrefs.GetFloat(keys[3]) - 1f) < 0.001f;
        }
        finally
        {
            for (int i = 0; i < keys.Length; i++)
            {
                if (existed[i]) PlayerPrefs.SetFloat(keys[i], values[i]);
                else PlayerPrefs.DeleteKey(keys[i]);
            }
            PlayerPrefs.Save();
            ScadrykSettings.FlushPendingSave();
        }
    }

    private IEnumerator SmokeCampaign()
    {
        yield return null;
        if (!SmokeSettingsPreferenceNormalization())
        { SmokeFail("As preferências salvas não recuperam valores padrão ou aceitam gravar NaN/infinito"); yield break; }
        Debug.Log("SCADRYK_SMOKE: preferências corrompidas usam padrões seguros, entradas não finitas não são salvas e dados originais são restaurados");
        if (!ScadrykPlayerController.CanMeasureTerrainExposure(false, false) ||
            ScadrykPlayerController.CanMeasureTerrainExposure(true, false) ||
            ScadrykPlayerController.CanMeasureTerrainExposure(false, true))
        { SmokeFail("A exposição ambiental ignora pausa ou continua sendo contada após a derrota"); yield break; }
        Debug.Log("SCADRYK_SMOKE: telemetria ambiental continua ativa no jogo e para na pausa ou na derrota");
        Vector2 cappedFlow = ScadrykTerrainZone.CombineFlow(Vector2.right * 1.8f, Vector2.right * 1.8f);
        Vector2 opposingWalk = ScadrykPlayerController.ResolveTerrainTargetVelocity(Vector2.left, 6.2f, 1f, Vector2.right * 1.8f);
        Vector2 carriedIdle = ScadrykPlayerController.ResolveTerrainTargetVelocity(Vector2.zero, 6.2f, 1f, Vector2.right * 1.8f);
        Vector2 saltCrosswindWalk = ScadrykPlayerController.ResolveTerrainTargetVelocity(Vector2.right, 6.2f, 0.96f, new Vector2(-1.05f, -0.48f));
        if (Mathf.Abs(cappedFlow.magnitude - ScadrykTerrainZone.MaximumFlowSpeed) > 0.001f ||
            opposingWalk.x >= 0f || Mathf.Abs(opposingWalk.y) > 0.001f ||
            Mathf.Abs(carriedIdle.x - 1.8f) > 0.001f || Mathf.Abs(carriedIdle.y) > 0.001f ||
            saltCrosswindWalk.x <= 0f || saltCrosswindWalk.y >= 0f)
        { SmokeFail("Correntes ambientais não limitam fluxos, resistem à caminhada ou preservam o avanço na Bacia"); yield break; }
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
            ScadrykSettings.CanBindKey(KeyCode.Mouse0) || ScadrykSettings.CanBindKey(KeyCode.JoystickButton0) ||
            ScadrykSettings.CanBindKey((KeyCode)987654) ||
            !ScadrykSettings.CanBindKey(KeyCode.F8) || ScadrykSettings.CanBindKey(KeyCode.M) || !ScadrykSettings.CanBindKey(KeyCode.F) ||
            ScadrykSettings.CanBindPadButton(7) || ScadrykSettings.CanBindPadButton(8) || !ScadrykSettings.CanBindPadButton(9))
        { SmokeFail("Remapeamento permite tecla/botão de sistema ou rejeita um vínculo de QA normal"); yield break; }
        if (ScadrykSettings.PadOptionAction(7) != ScadrykSettings.UnclearAttackAction ||
            ScadrykSettings.PadButton(ScadrykSettings.UnclearAttackAction) != 9 ||
            ScadrykSettings.PadButtonName(9) != "R3")
        { SmokeFail("A ação de marcar ataque pouco claro não está disponível no controle com nome correto"); yield break; }
        Debug.Log("SCADRYK_SMOKE: F8 é remapeável para QA; R3 marca ataques pouco claros no controle; Esc, M, Start e L3 permanecem reservados");
        int[] keyboardConflict = { 32, 304, 113, 101 };
        int[] gamepadConflict = { 0, 1, 2, 4, 5, 6, 3 };
        int unclearAttackPadButton = 9;
        bool crossDeviceBindingsSwap = ScadrykSettings.SwapGamepadBinding(gamepadConflict, 0, 9, ref unclearAttackPadButton) &&
            unclearAttackPadButton == 0 && ScadrykSettings.BindingValuesAreUnique(gamepadConflict) &&
            ScadrykSettings.SwapUnclearAttackPadBinding(gamepadConflict, 1, ref unclearAttackPadButton) &&
            unclearAttackPadButton == 1 && gamepadConflict[1] == 0 && ScadrykSettings.BindingValuesAreUnique(gamepadConflict);
        if (!ScadrykSettings.SwapConflictingBinding(keyboardConflict, 2, 32) ||
            keyboardConflict[0] != 113 || keyboardConflict[2] != 32 || !ScadrykSettings.BindingValuesAreUnique(keyboardConflict) ||
            !ScadrykSettings.SwapConflictingBinding(gamepadConflict, 4, 0) ||
            !crossDeviceBindingsSwap || gamepadConflict[4] != 0 || !ScadrykSettings.BindingValuesAreUnique(gamepadConflict) ||
            ScadrykSettings.SwapConflictingBinding(gamepadConflict, 9, 1))
        { SmokeFail("Remapear uma tecla/botão ocupado deve trocar o vínculo sem duplicatas e rejeitar índices inválidos"); yield break; }
        Debug.Log("SCADRYK_SMOKE: remapeamento de teclado e controle troca conflitos sem duplicar comandos");
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
        if (Vector2.Dot(steeredHeading, Vector2.right) < 0.8f || enclosedHeading.sqrMagnitude > 0.001f ||
            ScadrykEnemyHealth.SteeringQueryIsSaturated(ScadrykEnemyHealth.SteeringHitCapacity - 1) ||
            !ScadrykEnemyHealth.SteeringQueryIsSaturated(ScadrykEnemyHealth.SteeringHitCapacity))
        { SmokeFail("A navegação dos Ecos não contorna um bloqueio ou tenta avançar quando todos os lados estão fechados"); yield break; }
        Debug.Log("SCADRYK_SMOKE: direção de contorno mantém avanço e inimigos param com segurança quando nenhuma rota está livre");
        Vector2 strafeClockwise = ScadrykEnemyHealth.RangedStrafeHeading(Vector2.right, 1);
        Vector2 strafeCounterclockwise = ScadrykEnemyHealth.RangedStrafeHeading(Vector2.right, -1);
        if (Vector2.Dot(strafeClockwise, Vector2.right) != 0f ||
            Vector2.Distance(strafeClockwise, Vector2.up) > 0.001f ||
            Vector2.Distance(strafeCounterclockwise, Vector2.down) > 0.001f ||
            ScadrykEnemyHealth.RangedStrafeHeading(Vector2.zero, 1) != Vector2.zero ||
            ScadrykEnemyHealth.RangedStrafeSpeedScale <= 0f || ScadrykEnemyHealth.RangedStrafeSpeedScale >= 1f)
        { SmokeFail("O Atirador deve circular em velocidade reduzida, sem componente radial, e tratar alvo coincidente com segurança"); yield break; }
        Debug.Log("SCADRYK_SMOKE: Atiradores mantêm distância com reposicionamento lateral de baixa velocidade");
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
        bool storyJournalRoundTrip = false, storyJournalMigrationRoundTrip = false;
        bool quitCheckpointStateIsValid = false, campaignProgressCleanupRoundTrip = false;
        bool storyJournalSurvivesCampaignCleanup = false;
        bool bossProgressRoundTrip = false, powerRestoreRoundTrip = false, powerEchoProgressRoundTrip = false;
        try
        {
            ClearCheckpoint(checkpointTestPrefix);
            bool newGameDefaults = !HasCheckpoint(checkpointTestPrefix) &&
                !TryLoadCheckpoint(checkpointTestPrefix, out int defaultPhase, out int defaultRoom) && defaultPhase == 0 && defaultRoom == 0;
            quitCheckpointStateIsValid = ShouldSaveCheckpointOnQuit(true, false, false, false, false) &&
                !ShouldSaveCheckpointOnQuit(false, false, false, false, false) &&
                !ShouldSaveCheckpointOnQuit(true, true, false, false, false) &&
                !ShouldSaveCheckpointOnQuit(true, false, true, false, false) &&
                !ShouldSaveCheckpointOnQuit(true, false, false, true, false) &&
                !ShouldSaveCheckpointOnQuit(true, false, false, false, true) &&
                ShouldSaveCheckpointOnFocusLoss(false, true, false, false, false, false, false, false, false) &&
                !ShouldSaveCheckpointOnFocusLoss(true, true, false, false, false, false, false, false, false) &&
                !ShouldSaveCheckpointOnFocusLoss(false, true, false, false, true, false, false, false, false) &&
                !ShouldSaveCheckpointOnFocusLoss(false, true, false, false, false, true, false, false, false) &&
                ShouldSaveCheckpointOnApplicationPause(true, true, false, false, false, false) &&
                !ShouldSaveCheckpointOnApplicationPause(false, true, false, false, false, false) &&
                !ShouldSaveCheckpointOnApplicationPause(true, false, false, false, false, false) &&
                !ShouldSaveCheckpointOnApplicationPause(true, true, true, false, false, false);
            WriteCheckpoint(checkpointTestPrefix, 2, 2);
            RecordStoryJournalEntry(checkpointTestPrefix, "Memórias permanecem no Caderno após a vitória.");
            WriteGuardianDefeatProgress(checkpointTestPrefix, 0);
            WriteFinalBossDefeatProgress(checkpointTestPrefix);
            WriteWitnessProgress(checkpointTestPrefix, 1, 2);
            MarkWorldSecretRevealed(checkpointTestPrefix, 2, 1);
            ClearProgressAfterVictory(checkpointTestPrefix);
            campaignProgressCleanupRoundTrip = !HasGuardianDefeatProgress(checkpointTestPrefix, 0) &&
                !HasFinalBossDefeatProgress(checkpointTestPrefix) && ReadWitnessProgress(checkpointTestPrefix, 1) == 0 &&
                !IsWorldSecretRevealed(checkpointTestPrefix, 2, 1) && !HasCheckpoint(checkpointTestPrefix);
            storyJournalSurvivesCampaignCleanup = System.Array.IndexOf(ReadStoryJournal(checkpointTestPrefix),
                "Memórias permanecem no Caderno após a vitória.") >= 0;
            ClearStoryJournal(checkpointTestPrefix);
            RecordStoryJournalEntry(checkpointTestPrefix, "Primeira lembrança.");
            RecordStoryJournalEntry(checkpointTestPrefix, "Primeira lembrança.");
            RecordStoryJournalEntry(checkpointTestPrefix, "A costa voltou a cantar.");
            string firstRoomStory = RoomStoryJournalEntry(roomNames[0][0], RoomStories[0]);
            RecordStoryJournalEntry(checkpointTestPrefix, firstRoomStory);
            RecordStoryJournalEntry(checkpointTestPrefix, firstRoomStory);
            string[] journalEntries = ReadStoryJournal(checkpointTestPrefix);
            storyJournalRoundTrip = journalEntries.Length == 3 && journalEntries[0] == "Primeira lembrança." &&
                journalEntries[1] == "A costa voltou a cantar." && journalEntries[2] == firstRoomStory;
            storyJournalRoundTrip &= storyJournalSurvivesCampaignCleanup;
            ClearStoryJournal(checkpointTestPrefix);
            storyJournalRoundTrip &= ReadStoryJournal(checkpointTestPrefix).Length == 0;
            string previousRoomStory = RoomStoryJournalEntry(roomNames[0][0], "A água conserva vozes que Nara pensava perdidas.");
            var legacyJournal = new StoryJournalData { entries = new List<string> { previousRoomStory, firstRoomStory } };
            PlayerPrefs.SetString(StoryJournalProgressKey(checkpointTestPrefix), JsonUtility.ToJson(legacyJournal));
            string[] migratedJournal = ReadStoryJournal(checkpointTestPrefix);
            StoryJournalData migratedStoredJournal = JsonUtility.FromJson<StoryJournalData>(PlayerPrefs.GetString(StoryJournalProgressKey(checkpointTestPrefix)));
            storyJournalMigrationRoundTrip = migratedJournal.Length == 1 && migratedJournal[0] == firstRoomStory &&
                migratedStoredJournal != null && migratedStoredJournal.entries != null && migratedStoredJournal.entries.Count == 1 &&
                migratedStoredJournal.entries[0] == firstRoomStory &&
                !PlayerPrefs.GetString(StoryJournalProgressKey(checkpointTestPrefix)).Contains("A água conserva vozes") &&
                MigrateLegacyStoryJournalEntry("Depois que a Grande Onda cobriu a costa, os três sinos emudeceram. Nara, a Ouvinte, ainda escutava os nomes perdidos e seguiu a maré imóvel.") == OpeningMemory &&
                MigrateLegacyStoryJournalEntry(RoomStoryJournalEntry(roomNames[0][2], "O guardião prende nas raízes o primeiro sino.")) ==
                    RoomStoryJournalEntry(roomNames[0][2], GuardianIntroductions[0]) &&
                MigrateLegacyStoryJournalEntry("A Raiz-Mor entrega o Crescente e revela que Nima tocou o primeiro sino; seu nome ficou guardado nas raízes. O arco do Crescente interrompe Ecos.") == GuardianMemories[0] &&
                MigrateLegacyStoryJournalEntry("Sou Nima, a voz do primeiro sino. A Raiz-Mor guardou meu nome para que o silêncio não me apagasse. Ainda lembro o Arauto chamando esse silêncio de abrigo.") == BasinWitnessLines[0] &&
                MigrateLegacyStoryJournalEntry("Sou Nima, a voz do primeiro sino. Toquei para avisar a margem; a onda não respondeu, e meu nome voltou preso às raízes.") == BasinWitnessLines[0] &&
                MigrateLegacyStoryJournalEntry("O sal ainda segura a onda no instante em que partiu. Ressoem os cristais; a água precisa voltar a seguir.") == BasinWitnessLines[2] &&
                MigrateLegacyStoryJournalEntry("Você devolveu movimento ao sal. Agora sabemos: o Arauto parou a onda depois que a costa perdeu seus nomes.") == CoastWitnessLines[0] &&
                MigrateLegacyStoryJournalEntry("O Vigia do Coro ouviu o Arauto e nunca mais tocou. Faça o farol responder primeiro; o sino do recife vem depois.") == CoastWitnessLines[2] &&
                MigrateLegacyStoryJournalEntry("O Arauto chama o silêncio de abrigo. Pergunte a quem perdeu o próprio nome se isso parece paz.") == CoastWitnessLines[2] &&
                MigrateLegacyStoryJournalEntry("Memória de outra versão.") == "Memória de outra versão.";
            RecordStoryJournalEntry(checkpointTestPrefix, firstRoomStory);
            storyJournalMigrationRoundTrip &= ReadStoryJournal(checkpointTestPrefix).Length == 1;
            ClearStoryJournal(checkpointTestPrefix);
            storyJournalMigrationRoundTrip &= ReadStoryJournal(checkpointTestPrefix).Length == 0;
            PlayerPrefs.SetInt(checkpointTestPrefix + "Room", 2);
            bool ignoresOrphanedRoom = !HasCheckpoint(checkpointTestPrefix) &&
                !TryLoadCheckpoint(checkpointTestPrefix, out int orphanPhase, out int orphanRoom) && orphanPhase == 0 && orphanRoom == 0;
            ClearCheckpoint(checkpointTestPrefix);
            PlayerPrefs.SetInt(checkpointTestPrefix + "Phase", 2);
            bool ignoresOrphanedPhase = !HasCheckpoint(checkpointTestPrefix) &&
                !TryLoadCheckpoint(checkpointTestPrefix, out int orphanedPhase, out int orphanedRoom) && orphanedPhase == 0 && orphanedRoom == 0;
            ClearCheckpoint(checkpointTestPrefix);
            PlayerPrefs.SetInt(checkpointTestPrefix + "Phase", 1);
            PlayerPrefs.SetInt(checkpointTestPrefix + "Room", 2);
            bool restoresLegacyCheckpoint = TryLoadCheckpoint(checkpointTestPrefix, out int legacyPhase, out int legacyRoom) &&
                legacyPhase == 1 && legacyRoom == 2;
            WriteCheckpoint(checkpointTestPrefix, 2, 2);
            bool restoresLastPortal = HasCheckpoint(checkpointTestPrefix) &&
                TryLoadCheckpoint(checkpointTestPrefix, out int savedPhase, out int savedRoom) && savedPhase == 2 && savedRoom == 2 &&
                PlayerPrefs.HasKey(CheckpointProgressKey(checkpointTestPrefix));
            PlayerPrefs.SetString(CheckpointProgressKey(checkpointTestPrefix), "{\"version\":1,\"phase\":0}");
            bool partialCheckpointUsesRecoveryPair = TryLoadCheckpoint(checkpointTestPrefix, out int partialPhase, out int partialRoom) &&
                partialPhase == 2 && partialRoom == 2;
            WriteCheckpoint(checkpointTestPrefix, 2, 2);
            PlayerPrefs.SetString(CheckpointProgressKey(checkpointTestPrefix), "{damaged checkpoint");
            bool damagedCheckpointUsesRecoveryPair = TryLoadCheckpoint(checkpointTestPrefix, out int recoveredPhase, out int recoveredRoom) &&
                recoveredPhase == 2 && recoveredRoom == 2;
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
            bool secretStartsHidden = !IsWorldSecretRevealed(checkpointTestPrefix, 1, 1) && CountWorldSecretsRevealed(checkpointTestPrefix) == 0;
            MarkWorldSecretRevealed(checkpointTestPrefix, 1, 1);
            bool secretSurvivesRoomRebuild = IsWorldSecretRevealed(checkpointTestPrefix, 1, 1) &&
                !ShouldBuildWorldSecret(false, IsWorldSecretRevealed(checkpointTestPrefix, 1, 1)) &&
                CountWorldSecretsRevealed(checkpointTestPrefix) == 1;
            bool smokeCanRebuildSecret = ShouldBuildWorldSecret(true, IsWorldSecretRevealed(checkpointTestPrefix, 1, 1));
            bool secretsAreRoomSpecific = !IsWorldSecretRevealed(checkpointTestPrefix, 2, 1);
            ClearWorldSecretProgress(checkpointTestPrefix);
            bool newJourneyClearsSecrets = !IsWorldSecretRevealed(checkpointTestPrefix, 1, 1);
            bool freshJourneyRevealsUnfoundSecret = ShouldBuildWorldSecret(false, IsWorldSecretRevealed(checkpointTestPrefix, 1, 1));
            worldSecretProgressRoundTrip = secretStartsHidden && secretSurvivesRoomRebuild && smokeCanRebuildSecret &&
                secretsAreRoomSpecific && newJourneyClearsSecrets && freshJourneyRevealsUnfoundSecret;
            MarkWorldSecretRevealed(checkpointTestPrefix, 1, 0);
            MarkWorldSecretRevealed(checkpointTestPrefix, 2, 0);
            bool legacySecretLocationsMigrated = MigrateLegacyWorldSecretProgress(checkpointTestPrefix) &&
                !IsWorldSecretRevealed(checkpointTestPrefix, 1, 0) &&
                !IsWorldSecretRevealed(checkpointTestPrefix, 2, 0) &&
                IsWorldSecretRevealed(checkpointTestPrefix, 1, 1) &&
                IsWorldSecretRevealed(checkpointTestPrefix, 2, 1) &&
                CountWorldSecretsRevealed(checkpointTestPrefix) == 2 &&
                ShouldRestorePowerEcho(checkpointTestPrefix, 0) && ShouldRestorePowerEcho(checkpointTestPrefix, 1);
            ClearWorldSecretProgress(checkpointTestPrefix);
            worldSecretProgressRoundTrip &= legacySecretLocationsMigrated;
            MarkWorldSecretRevealed(checkpointTestPrefix, 1, 1);
            MarkWorldSecretRevealed(checkpointTestPrefix, 2, 1);
            MarkWorldSecretRevealed(checkpointTestPrefix, 2, 2);
            powerEchoProgressRoundTrip = ShouldRestorePowerEcho(checkpointTestPrefix, 0) &&
                ShouldRestorePowerEcho(checkpointTestPrefix, 1) && ShouldRestorePowerEcho(checkpointTestPrefix, 2) &&
                !ShouldRestorePowerEcho(checkpointTestPrefix, -1) && !ShouldRestorePowerEcho(checkpointTestPrefix, 3) &&
                PowerGrantedBySecretAt(1, 1) == 0 && PowerGrantedBySecretAt(2, 1) == 1 &&
                PowerGrantedBySecretAt(2, 2) == 2 && PowerGrantedBySecretAt(0, 0) == -1 &&
                CountWorldSecretsRevealed(checkpointTestPrefix) == 3;
            ClearWorldSecretProgress(checkpointTestPrefix);
            powerEchoProgressRoundTrip &= !ShouldRestorePowerEcho(checkpointTestPrefix, 0) &&
                !ShouldRestorePowerEcho(checkpointTestPrefix, 1) && !ShouldRestorePowerEcho(checkpointTestPrefix, 2) &&
                CountWorldSecretsRevealed(checkpointTestPrefix) == 0;
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
                ignoresOrphanedPhase && restoresLegacyCheckpoint && restoresLastPortal && partialCheckpointUsesRecoveryPair &&
                damagedCheckpointUsesRecoveryPair && clampsInvalidData;
        }
        finally
        {
            ClearCheckpoint(checkpointTestPrefix);
            ClearGuardianDefeatProgress(checkpointTestPrefix);
            ClearFinalBossDefeatProgress(checkpointTestPrefix);
            ClearWitnessProgress(checkpointTestPrefix);
            ClearWorldSecretProgress(checkpointTestPrefix);
            ClearStoryJournal(checkpointTestPrefix);
            PlayerPrefs.Save();
        }
        if (!storyJournalRoundTrip || !storyJournalMigrationRoundTrip) { SmokeFail("O Caderno não preserva lembranças únicas após a vitória, migra textos antigos ou limpa os dados temporários"); yield break; }
        Debug.Log("SCADRYK_SMOKE: caderno persiste lembranças únicas em ordem e reinicia com nova jornada");
        if (!checkpointRoundTrip) { SmokeFail("Checkpoint parcial não foi ignorado, ou o portal não foi restaurado/limitado"); yield break; }
        Debug.Log("SCADRYK_SMOKE: checkpoint versionado grava, recupera versão antiga/danificada, retoma e limpa chaves temporárias");
        if (!bossProgressRoundTrip) { SmokeFail("O save não preserva chefes derrotados, restaura sala segura ou limpa a jornada nova"); yield break; }
        Debug.Log("SCADRYK_SMOKE: derrota de guardiões e Arauto persiste, retoma na arena e é limpa na jornada nova");
        if (!powerRestoreRoundTrip) { SmokeFail("Retomar um checkpoint não restaura somente os poderes já conquistados pelos guardiões"); yield break; }
        Debug.Log("SCADRYK_SMOKE: poderes restauram a partir do bioma e dos guardiões persistidos sem liberar poderes futuros");
        if (!witnessProgressRoundTrip) { SmokeFail("As conversas com Nima não preservam o ponto de leitura por bioma ou não reiniciam com a jornada"); yield break; }
        Debug.Log("SCADRYK_SMOKE: conversa de Nima preserva a próxima fala separada por bioma e pode ser reiniciada");
        if (!worldSecretProgressRoundTrip) { SmokeFail("Selo opcional falha ao retomar, migrar save antigo, isolar sala ou limpar nova jornada"); yield break; }
        Debug.Log("SCADRYK_SMOKE: selos persistem, migram locais antigos, isolam salas e limpam em nova jornada");
        if (!powerEchoProgressRoundTrip) { SmokeFail("Melhorias de poder dos Ecos não acompanham corretamente os selos no save ou na nova jornada"); yield break; }
        Debug.Log("SCADRYK_SMOKE: os três selos salvam melhorias distintas e a jornada nova as remove");
        if (!ScadrykCombatRules.IsValidDamageAmount(0.01f) || ScadrykCombatRules.IsValidDamageAmount(0f) ||
            ScadrykCombatRules.IsValidDamageAmount(-1f) || ScadrykCombatRules.IsValidDamageAmount(float.NaN) ||
            ScadrykCombatRules.IsValidDamageAmount(float.PositiveInfinity) || ScadrykCombatRules.IsValidDamageAmount(float.NegativeInfinity))
        { SmokeFail("Entrada de dano zero, negativa ou não finita não foi rejeitada antes de alterar vida/reações"); yield break; }
        Debug.Log("SCADRYK_SMOKE: dano zero, negativo e não finito é rejeitado antes da reação de impacto");
        if (!ScadrykEnemyProjectile.ProjectileArgumentsAreValid(Vector2.zero, Vector2.right, 8f, 12f, 0.34f, -1f) ||
            ScadrykEnemyProjectile.ProjectileArgumentsAreValid(new Vector2(float.PositiveInfinity, 0f), Vector2.right, 8f, 12f, 0.34f, -1f) ||
            ScadrykEnemyProjectile.ProjectileArgumentsAreValid(Vector2.zero, new Vector2(float.NaN, 1f), 8f, 12f, 0.34f, -1f) ||
            ScadrykEnemyProjectile.ProjectileArgumentsAreValid(Vector2.zero, Vector2.right, float.PositiveInfinity, 12f, 0.34f, -1f) ||
            ScadrykEnemyProjectile.ProjectileArgumentsAreValid(Vector2.zero, Vector2.right, 8f, float.NaN, 0.34f, -1f) ||
            ScadrykEnemyProjectile.ProjectileArgumentsAreValid(Vector2.zero, Vector2.right, 8f, 12f, 0f, -1f) ||
            ScadrykEnemyProjectile.ProjectileArgumentsAreValid(Vector2.zero, Vector2.right, 8f, 12f, 0.34f, float.PositiveInfinity) ||
            ScadrykEnemyProjectile.ProjectileArgumentsAreValid(Vector2.zero, Vector2.zero, 8f, 12f, 0.34f, -1f))
        { SmokeFail("Projétil aceita posição/direção inválida, dano não finito, velocidade sem expiração ou dimensões inválidas"); yield break; }
        Debug.Log("SCADRYK_SMOKE: configuração inválida de projétil é rejeitada antes de consumir o pool");
        if (ScadrykCombatRules.ApplyHealing(40f, 100f, 15f) != 55f ||
            ScadrykCombatRules.ApplyHealing(95f, 100f, 10f) != 100f ||
            ScadrykCombatRules.ApplyHealing(40f, 100f, 0f) != 40f ||
            ScadrykCombatRules.ApplyHealing(40f, 100f, -1f) != 40f ||
            ScadrykCombatRules.ApplyHealing(40f, 100f, float.NaN) != 40f ||
            ScadrykCombatRules.ApplyHealing(40f, 100f, float.PositiveInfinity) != 40f ||
            ScadrykCombatRules.ApplyHealing(40f, 100f, float.NegativeInfinity) != 40f ||
            ScadrykCombatRules.ApplyHealing(float.NaN, 100f, 10f) != 10f ||
            ScadrykCombatRules.ApplyHealing(40f, float.PositiveInfinity, 10f) != 0f)
        { SmokeFail("Cura ultrapassa o limite de vida, aceita entrada inválida ou preserva uma vida corrompida"); yield break; }
        Debug.Log("SCADRYK_SMOKE: cura é finita, positiva e limitada à vida máxima");
        if (!SmokeCoverBlocksOnlyWhenSolid())
        { SmokeFail("Cobertura não bloqueia poderes direcionais ou gatilhos bloqueiam poderes circulares"); yield break; }
        Debug.Log("SCADRYK_SMOKE: cobertura interrompe golpes direcionais, não o Estouro circular, e gatilhos não bloqueiam");
        if (!RoomObstacleLayoutsAreDistinctAndOpen())
        { SmokeFail("As nove salas repetem obstáculos, não preenchem as margens ampliadas, bloqueiam entrada/saída ou não têm rota até o portal"); yield break; }
        Debug.Log("SCADRYK_SMOKE: nove arenas ampliadas mantêm obstáculos nas margens, layouts distintos e rota ao portal");
        if (!SideRoutesAreDistinctAndNavigable())
        { SmokeFail("Os desvios laterais dos três biomas não conectam as duas aberturas ou não deixam largura segura"); yield break; }
        Debug.Log("SCADRYK_SMOKE: bosque, sal e costa têm desvios alternativos com aberturas e largura transitáveis");
        if (!SideRouteChallengesAreDistinctAndSafe())
        { SmokeFail("Os desvios não têm travessias ambientais próprias, ou o campo de movimento bloqueia o selo opcional"); yield break; }
        Debug.Log("SCADRYK_SMOKE: água, rajada e corrente dão desafios distintos sem cobrir os selos nem exceder o limite de fluxo");
        if (!RoomAnchorsAvoidCover())
        { SmokeFail("Um objetivo de ressonância está oculto pela cobertura ou perto demais da entrada/saída"); yield break; }
        Debug.Log("SCADRYK_SMOKE: objetivos das cinco salas de sinal ficam legíveis e acessíveis fora da cobertura");
        if (!EncounterSpawnsAvoidCover())
        { SmokeFail("Um Eco ou guardião nasce dentro da cobertura ou sobre outro inimigo"); yield break; }
        Debug.Log("SCADRYK_SMOKE: surgimento dos encontros evita cobertura sólida nas nove salas");
        if (Mathf.Abs(ScadrykEnemyHealth.OpeningAttackDelay(0, false, true) - 2.6f) > 0.001f ||
            Mathf.Abs(ScadrykEnemyHealth.OpeningAttackDelay(0, false) - 0.3f) > 0.001f ||
            Mathf.Abs(ScadrykEnemyHealth.OpeningAttackDelay(1, false) - 1.05f) > 0.001f ||
            ScadrykEnemyHealth.OpeningAttackSpacing < ScadrykEnemyHealth.MinimumAttackWarningDuration ||
            ScadrykEnemyHealth.OpeningAttackDelay(1, false) <= ScadrykEnemyHealth.OpeningAttackDelay(0, false) ||
            ScadrykEnemyHealth.OpeningAttackDelay(2, false) <= ScadrykEnemyHealth.OpeningAttackDelay(1, false) ||
            ScadrykEnemyHealth.OpeningAttackDelay(3, false) <= ScadrykEnemyHealth.OpeningAttackDelay(2, false) ||
            Mathf.Abs(ScadrykEnemyHealth.OpeningAttackDelay(0, true) - 1.25f) > 0.001f)
        { SmokeFail("Os primeiros avisos de ataque dos encontros não estão espaçados"); yield break; }
        float nextEnemySlot = 0f;
        float firstAttack = ScadrykEnemyHealth.ReserveEnemyAttackSlot(ref nextEnemySlot, 0.3f);
        float secondAttack = ScadrykEnemyHealth.ReserveEnemyAttackSlot(ref nextEnemySlot, 0.82f);
        float lateAttack = ScadrykEnemyHealth.ReserveEnemyAttackSlot(ref nextEnemySlot, 5f);
        float clusteredFollowup = ScadrykEnemyHealth.ReserveEnemyAttackSlot(ref nextEnemySlot, 5.1f);
        if (Mathf.Abs(firstAttack - 0.3f) > 0.001f || Mathf.Abs(secondAttack - 1.05f) > 0.001f ||
            Mathf.Abs(lateAttack - 5f) > 0.001f ||
            Mathf.Abs(clusteredFollowup - lateAttack - ScadrykEnemyHealth.OpeningAttackSpacing) > 0.001f)
        { SmokeFail("O escalonador não separou inimigos que voltaram a coincidir após o primeiro ciclo"); yield break; }
        Debug.Log("SCADRYK_SMOKE: primeiros avisos e ataques repetidos mantêm intervalo entre inimigos");
        if (!EncounterCompositionHasExpectedPacing() ||
            EnemyCountForRoom(0) != 1 || EnemyRoleForRoom(0, 0) != 0 ||
            EnemyCountForRoom(1) != 2 || EnemyRoleForRoom(1, 0) != 0 || EnemyRoleForRoom(1, 1) != 1 ||
            EnemyCountForRoom(4) != 3 ||
            EnemyRoleForRoom(4, 0) != 0 || EnemyRoleForRoom(4, 1) != 1 || EnemyRoleForRoom(4, 2) != 2 ||
            EnemyCountForRoom(7) != 4 ||
            EnemyRoleForRoom(7, 0) != 1 || EnemyRoleForRoom(7, 1) != 1 || EnemyRoleForRoom(7, 2) != 2 || EnemyRoleForRoom(7, 3) != 3 ||
            !GuardianAttackIsPreviewedBeforeBoss(0) || !GuardianAttackIsPreviewedBeforeBoss(1) || !GuardianAttackIsPreviewedBeforeBoss(2) ||
            EnemyArtworkRowForRole(0) != 0 || EnemyArtworkRowForRole(1) != 1 ||
            EnemyArtworkRowForRole(2) != 3 || EnemyArtworkRowForRole(3) != 2 || !ScadrykEnemyHealth.RoleTintPalettesAreLegible())
        { SmokeFail("A introdução gradual dos papéis de Eco ou a identidade tática dos encontros foi perdida"); yield break; }
        Debug.Log("SCADRYK_SMOKE: Caçador e Atirador aparecem primeiro, Ressonador entra na Bacia e Tecelão é acrescentado na Costa; chefes usam padrões previamente ensinados");
        if (GuardianNames.Length != 3 || GuardianIntroductions.Length != 3 || GuardianMemories.Length != 3)
        { SmokeFail("Cada bioma precisa de nome, fala e memória próprios para o guardião"); yield break; }
        float basinNimaPresence = WitnessPresenceForPhase(1), coastNimaPresence = WitnessPresenceForPhase(2);
        float basinNimaAfterMemories = WitnessPresenceForConversation(1, 3);
        if (WitnessPresenceForPhase(0) != 0f || basinNimaPresence <= 0f || coastNimaPresence <= basinNimaPresence || coastNimaPresence > 1f ||
            WitnessScaleForPhase(2) <= WitnessScaleForPhase(1) || WitnessTintForPhase(0) != Color.clear ||
            WitnessTintForPhase(1) == WitnessTintForPhase(2) ||
            Mathf.Abs(WitnessTintForPhase(1).a - basinNimaPresence) > 0.001f ||
            Mathf.Abs(WitnessTintForPhase(2).a - coastNimaPresence) > 0.001f ||
            basinNimaAfterMemories <= basinNimaPresence || basinNimaAfterMemories > coastNimaPresence ||
            Mathf.Abs(WitnessPresenceForConversation(1, 99) - basinNimaAfterMemories) > 0.001f ||
            Mathf.Abs(WitnessPresenceForConversation(2, 3) - coastNimaPresence) > 0.001f ||
            Mathf.Abs(WitnessTintForConversation(1, 3).a - basinNimaAfterMemories) > 0.001f ||
            WitnessScaleForConversation(1, 3) <= WitnessScaleForConversation(1, 0))
        { SmokeFail("A presença visível de Nima não cresce com as conversas salvas sem ultrapassar a forma restaurada na costa"); yield break; }
        if (!OpeningMemory.Contains("Grande Onda") || !OpeningMemory.Contains("Nara") ||
            !GuardianMemories[0].Contains("Nima") || !BasinWitnessLines[0].Contains("primeiro sino") ||
            !BasinWitnessLines[1].StartsWith("Nara:") || !CoastWitnessLines[1].StartsWith("Nara:") ||
            BasinWitnessLines.Length != 3 || !BasinWitnessLines[2].Contains("cristais") ||
            CoastWitnessLines.Length != 3 || !CoastWitnessLines[0].Contains("sal") || !CoastWitnessLines[2].Contains("Arauto") ||
            !GuardianMemories[1].Contains("última onda") || !GuardianMemories[2].Contains("poupar os vivos") ||
            !EndingMemory.Contains("Nara escolhe lembrar") || !EndingMemory.Contains("aprisionou os mortos"))
        { SmokeFail("A história perdeu a causa da maré imóvel, a ligação de Nima ou a resolução do conflito"); yield break; }
        Vector2 nimaStart = new Vector2(2f, 1f), nimaPortal = new Vector2(8f, 0f);
        if (NimaDepartureProgress(0f) != 0f || NimaDepartureProgress(NimaDepartureDuration) != 1f ||
            NimaDeparturePosition(nimaStart, nimaPortal, 0f) != nimaStart ||
            Vector2.Distance(NimaDeparturePosition(nimaStart, nimaPortal, 1f), nimaPortal) > 0.001f ||
            NimaDepartureScale(1f) <= 0f || NimaDepartureScale(1f) >= WitnessScaleForPhase(2) ||
            NimaDepartureAlpha(0f) != 1f || NimaDepartureAlpha(1f) != 0f)
        { SmokeFail("A travessia de Nima não chega ao portal, encolhe ou desaparece com a temporização esperada"); yield break; }
        var roomStoryJournalEntries = new HashSet<string>();
        for (int phase = 0; phase < PhaseNames.Length; phase++)
        for (int room = 0; room < RoomsPerPhase; room++)
        {
            string story = room == RoomsPerPhase - 1
                ? GuardianIntroductions[phase]
                : RoomStories[phase * RoomsPerPhase + room];
            string entry = RoomStoryJournalEntry(roomNames[phase][room], story);
            if (string.IsNullOrWhiteSpace(entry) || !entry.StartsWith(roomNames[phase][room] + "\n") || !roomStoryJournalEntries.Add(entry))
            { SmokeFail("Uma das nove apresentações de sala/guardião está ausente ou duplicada no caderno"); yield break; }
        }
        if (roomStoryJournalEntries.Count != PhaseNames.Length * RoomsPerPhase)
        { SmokeFail("O caderno não tem uma apresentação identificável para cada sala"); yield break; }
        Debug.Log("SCADRYK_SMOKE: abertura, falas, aparição crescente de Nima, memórias dos guardiões e resolução formam o arco da maré");
        string victoryOverlay = OverlayMessage(false, true, false);
        string victoryWithSomeSeals = VictoryEndingTextForSecrets(2);
        string victoryWithAllSeals = OverlayMessage(false, true, false, 3);
        if (victoryOverlay != VictoryEndingText || !victoryOverlay.Contains("Os três sinos respondem") ||
            !victoryOverlay.Contains("O Arauto tentou poupar os vivos") || !victoryOverlay.Contains("aprisionou os mortos") ||
            !victoryOverlay.Contains("Nima atravessa levando o próprio nome") || !victoryOverlay.Contains("Nara escolhe lembrar sem prender") ||
            !victoryWithSomeSeals.Contains("memórias libertadas acompanham Nara") || victoryWithSomeSeals == victoryOverlay ||
            !victoryWithAllSeals.Contains("três memórias ocultas") || victoryWithAllSeals == victoryWithSomeSeals ||
            VictoryEndingTextForSecrets(-5) != victoryOverlay || VictoryEndingTextForSecrets(99) != victoryWithAllSeals)
        { SmokeFail("O encerramento deve resolver o conflito, celebrar os selos opcionais encontrados e continuar correto após a limpeza do save"); yield break; }
        Debug.Log("SCADRYK_SMOKE: finais sem selos, com memórias parciais e com todos os selos reconhecem a escolha e resolvem a história");
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
        string forestVolleyPattern = ScadrykEnemyHealth.AttackReadabilityLabel(0, false, false, 1, 0, 0);
        string saltVolleyPattern = ScadrykEnemyHealth.AttackReadabilityLabel(1, false, false, 1, 0, 0);
        string coastalVolleyPattern = ScadrykEnemyHealth.AttackReadabilityLabel(2, false, false, 1, 0, 0);
        string forestThrustPattern = ScadrykEnemyHealth.AttackReadabilityLabel(0, false, false, 0, 0, 0);
        string saltThrustPattern = ScadrykEnemyHealth.AttackReadabilityLabel(1, false, false, 0, 0, 0);
        string coastalThrustPattern = ScadrykEnemyHealth.AttackReadabilityLabel(2, false, false, 0, 0, 0);
        string saltGuardianPattern = ScadrykEnemyHealth.AttackReadabilityLabel(1, true, false, 0, 0, 0);
        string finalVolleyPattern = ScadrykEnemyHealth.AttackReadabilityLabel(2, true, true, 0, 0, 1);
        string finalStarfallPattern = ScadrykEnemyHealth.AttackReadabilityLabel(2, true, true, 0, 1, 2);
        string finalPattern = ScadrykEnemyHealth.AttackReadabilityLabel(2, true, true, 0, 2, 3);
        string clarityRow = FormatAttackClarityEventCsvRow("session", "2026-11-22T00:00:00Z", 3, 2, finalPattern).TrimEnd('\n');
        string escapedClarityRow = FormatAttackClarityEventCsvRow("session", "2026-11-22T00:00:00Z", 3, 2, "Eco, \"salva\"").TrimEnd('\n');
        if (!commonPattern.Contains("Ressonador — pulso de sal") ||
            ScadrykEnemyHealth.AreaPulseLabelForBiome(0) == ScadrykEnemyHealth.AreaPulseLabelForBiome(1) ||
            ScadrykEnemyHealth.AreaPulseLabelForBiome(1) == ScadrykEnemyHealth.AreaPulseLabelForBiome(2) ||
            !forestVolleyPattern.Contains("Atirador — espinho lançado") || !saltVolleyPattern.Contains("Atirador — estilhaço de sal") ||
            !coastalVolleyPattern.Contains("Atirador — fragmento de sino") || forestVolleyPattern == saltVolleyPattern ||
            !forestThrustPattern.Contains("Caçador — avanço de raízes") || !saltThrustPattern.Contains("Caçador — estocada de sal") ||
            !coastalThrustPattern.Contains("Caçador — investida do recife") || forestThrustPattern == saltThrustPattern ||
            saltThrustPattern == coastalThrustPattern ||
            saltVolleyPattern == coastalVolleyPattern ||
            !ScadrykEnemyHealth.AttackReadabilityLabel(2, false, false, 3, 0, 0).Contains("Tecelão — cura dos Ecos") ||
            ScadrykEnemyProjectile.ImpactColor(0) == ScadrykEnemyProjectile.ImpactColor(1) ||
            ScadrykEnemyProjectile.ImpactColor(1) == ScadrykEnemyProjectile.ImpactColor(2) ||
            ScadrykEnemyProjectile.ImpactColor(-1) != new Color(0.93f, 0.36f, 0.67f, 0.82f) ||
            ScadrykEnemyProjectile.ForestHitSlowDuration <= 0f || ScadrykEnemyProjectile.SaltHitSlowDuration <= ScadrykEnemyProjectile.ForestHitSlowDuration ||
            ScadrykEnemyProjectile.CoastalHitKnockbackSpeed <= 0f || ScadrykEnemyProjectile.CoastalHitKnockbackDuration <= 0f ||
            !saltGuardianPattern.Contains("Vigia do Sal") ||
            !saltGuardianPattern.Contains("salva de estilhaços") || !finalVolleyPattern.Contains("entre as linhas") ||
            !finalStarfallPattern.Contains("saia da marca") || !finalPattern.Contains("Arauto fase 3") ||
            !finalPattern.Contains("onda de vazio") || !finalPattern.Contains("centro seguro") || clarityRow.Split(',').Length != 5 ||
            !clarityRow.EndsWith(finalPattern) ||
            escapedClarityRow != "session,2026-11-22T00:00:00Z,3,2,\"Eco, \"\"salva\"\"\"")
        { SmokeFail("O registro de legibilidade não identifica papéis, guardiões, fases do Arauto ou campos do CSV"); yield break; }
        Debug.Log("SCADRYK_SMOKE: registros de ataque pouco claro identificam padrões recentes e serializam fase/sala/ataque");
        ScadrykCue laneCue = ScadrykEnemyHealth.WarningCueFor(0, false, false, 0);
        ScadrykCue volleyCue = ScadrykEnemyHealth.WarningCueFor(0, false, false, 1);
        ScadrykCue areaCue = ScadrykEnemyHealth.WarningCueFor(0, false, false, 2);
        ScadrykCue supportCue = ScadrykEnemyHealth.WarningCueFor(0, false, false, 3);
        ScadrykAudio audio = GetComponent<ScadrykAudio>();
        if (audio == null || audio.BiomeLoopName(0) != "Roots remember" || audio.BiomeLoopName(1) != "Salt remembers" ||
            audio.BiomeLoopName(2) != "Bells remember" || audio.BiomeLoopName(3) != "The silence breaks" ||
            Mathf.Abs(audio.BiomeLoopDuration(0) - 8f) > 0.01f || Mathf.Abs(audio.BiomeLoopDuration(1) - 8f) > 0.01f ||
            Mathf.Abs(audio.BiomeLoopDuration(2) - 8f) > 0.01f || Mathf.Abs(audio.BiomeLoopDuration(3) - 8f) > 0.01f ||
            !string.IsNullOrEmpty(audio.BiomeLoopName(-1)) || !string.IsNullOrEmpty(audio.BiomeLoopName(4)))
        { SmokeFail("As quatro paisagens sonoras devem manter motivo, nome e duração válidos por contexto"); yield break; }
        Debug.Log("SCADRYK_SMOKE: bosque, bacia, costa e Arauto mantêm loops musicais distintos e válidos");
        int phaseOneLast = ScadrykEnemyHealth.SelectFinalBossPattern(1, 3, 0);
        int phaseTwoFirst = ScadrykEnemyHealth.SelectFinalBossPattern(2, 4, phaseOneLast);
        int phaseThreeFirst = ScadrykEnemyHealth.SelectFinalBossPattern(3, 7, phaseTwoFirst);
        if (phaseOneLast != 1 || phaseOneLast == phaseTwoFirst || phaseTwoFirst == phaseThreeFirst ||
            ScadrykEnemyHealth.SelectFinalBossPattern(1, 0, -1) != 0 ||
            ScadrykEnemyHealth.SelectFinalBossPattern(1, 1, 0) != 1 ||
            ScadrykEnemyHealth.SelectFinalBossPattern(2, 0, -1) > 2 ||
            ScadrykEnemyHealth.SelectFinalBossPattern(3, 0, 1) == 1)
        { SmokeFail("A sequência do Arauto deve apresentar padrões distintos sem repetir golpe ao trocar de fase"); yield break; }
        Debug.Log("SCADRYK_SMOKE: o Arauto alterna padrões e não repete o golpe na transição de fase");
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
            audio.CueDuration(laneCue) > ScadrykEnemyHealth.OpeningAttackSpacing ||
            audio.CueDuration(volleyCue) > ScadrykEnemyHealth.OpeningAttackSpacing ||
            audio.CueDuration(areaCue) > ScadrykEnemyHealth.OpeningAttackSpacing ||
            audio.CueDuration(supportCue) > ScadrykEnemyHealth.OpeningAttackSpacing ||
            Mathf.Abs(ScadrykAudio.CombatIntensityFor(0, false, false)) > 0.001f ||
            Mathf.Abs(ScadrykAudio.CombatIntensityFor(1, false, false) - 0.32f) > 0.001f ||
            Mathf.Abs(ScadrykAudio.CombatIntensityFor(4, false, false) - 0.78f) > 0.001f ||
            Mathf.Abs(ScadrykAudio.CombatIntensityFor(1, true, false) - 1f) > 0.001f ||
            Mathf.Abs(ScadrykAudio.CombatIntensityFor(4, true, true)) > 0.001f ||
            Mathf.Abs(ScadrykAudio.BlendCombatIntensity(0f, 1f, 0.425f) - 0.5f) > 0.001f ||
            Mathf.Abs(ScadrykAudio.BlendCombatIntensity(1f, 0f, 0.425f) - 0.5f) > 0.001f ||
            Mathf.Abs(ScadrykAudio.WarningMusicDuckLevel - 0.62f) > 0.001f ||
            Mathf.Abs(ScadrykAudio.BlendWarningMusicEnvelope(1f, true, 0.12f) - 0.62f) > 0.001f ||
            Mathf.Abs(ScadrykAudio.BlendWarningMusicEnvelope(0.62f, false, 0.12f) - 0.87f) > 0.001f ||
            Mathf.Abs(ScadrykAudio.BlendWarningMusicEnvelope(0.62f, false, 0.48f) - 1f) > 0.001f ||
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
            !audio.HasCue(ScadrykCue.PlayerPowerImpact) || !audio.HasCue(ScadrykCue.CoastalCurrent) ||
            !audio.HasCue(ScadrykCue.VerdantCrescentImpact) || !audio.HasCue(ScadrykCue.SaltburstImpact) || !audio.HasCue(ScadrykCue.TidebreakerImpact) ||
            audio.CueClipName(ScadrykCue.VerdantCrescentImpact) != "Verdant crescent impact" ||
            audio.CueClipName(ScadrykCue.SaltburstImpact) != "Saltburst impact" ||
            audio.CueClipName(ScadrykCue.TidebreakerImpact) != "Tidebreaker impact" ||
            Mathf.Abs(audio.CueDuration(ScadrykCue.VerdantCrescentImpact) - 0.25f) > 0.01f ||
            Mathf.Abs(audio.CueDuration(ScadrykCue.SaltburstImpact) - 0.32f) > 0.01f ||
            Mathf.Abs(audio.CueDuration(ScadrykCue.TidebreakerImpact) - 0.38f) > 0.01f ||
            ScadrykAudio.AbilityImpactCue(0) != ScadrykCue.VerdantCrescentImpact ||
            ScadrykAudio.AbilityImpactCue(1) != ScadrykCue.SaltburstImpact ||
            ScadrykAudio.AbilityImpactCue(2) != ScadrykCue.TidebreakerImpact ||
            ScadrykCue.VerdantCrescentImpact == ScadrykCue.SaltburstImpact ||
            ScadrykCue.SaltburstImpact == ScadrykCue.TidebreakerImpact ||
            ScadrykCue.VerdantCrescentImpact == ScadrykCue.TidebreakerImpact ||
            !audio.HasCue(ScadrykCue.SaltCrosswind) || audio.CueClipName(ScadrykCue.SaltCrosswind) != "Salt grains in the crosswind" ||
            !audio.HasCue(ScadrykCue.NimaFarewell) || audio.CueClipName(ScadrykCue.NimaFarewell) != "Nima — the last crossing" ||
            ScadrykCue.NimaFarewell == ScadrykCue.EchoReleased ||
            ScadrykCue.SaltCrosswind == ScadrykCue.CoastalCurrent || ScadrykCue.PlayerPowerImpact == ScadrykCue.Impact ||
            ScadrykCue.PlayerPowerImpact == ScadrykCue.Power || ScadrykCue.PlayerPowerImpact == ScadrykCue.PlayerStrike ||
            ScadrykCue.PlayerStrike == ScadrykCue.Impact || ScadrykCue.PlayerStrike == ScadrykCue.Sword ||
            ScadrykCue.PlayerFinisher == ScadrykCue.PlayerStrike || ScadrykCue.PlayerFinisher == ScadrykCue.Impact ||
            ScadrykCue.PlayerFinisher == ScadrykCue.Sword)
        { SmokeFail("Cues de perigo, música de combate ou sinais de impacto não distinguem os estados previstos"); yield break; }
        Debug.Log("SCADRYK_SMOKE: música escala em combate, recua em pausa e abre espaço para avisos de perigo");
        Debug.Log("SCADRYK_SMOKE: cada padrão do Arauto tem cue próprio; avisos sobrepostos perdem volume e têm teto de quatro vozes");
        Debug.Log("SCADRYK_SMOKE: impactos do Crescente, Estouro e Rompante usam assinaturas sonoras distintas");
        if (Vector2.Distance(ScadrykPlayerController.NormalizeGamepadAimStick(new Vector2(0.4f, 0.3f)), new Vector2(0.4f, -0.3f)) > 0.001f ||
            Vector2.Distance(ScadrykPlayerController.NormalizeGamepadAimStick(Vector2.up), Vector2.down) > 0.001f)
        { SmokeFail("A mira e a calibração não convertem o eixo vertical do analógico da mesma forma"); yield break; }
        Debug.Log("SCADRYK_SMOKE: a calibração exibe as direções úteis da mira twin-stick");
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
            Vector2.Distance(ScadrykPlayerController.ResolveGamepadFacing(Vector2.up, Vector2.left, Vector2.right, true), Vector2.left) > 0.001f ||
            Vector2.Distance(ScadrykPlayerController.ResolveGamepadFacing(Vector2.left,
                Vector2.up * (ScadrykPlayerController.GamepadAimActivationMagnitude * 0.9f), Vector2.right, false), Vector2.right) > 0.001f ||
            Vector2.Distance(ScadrykPlayerController.ResolveGamepadFacing(Vector2.left,
                Vector2.up * (ScadrykPlayerController.GamepadAimActivationMagnitude * 0.9f), Vector2.right, true), Vector2.left) > 0.001f ||
            Vector2.Distance(ScadrykPlayerController.ResolveGamepadFacing(Vector2.left,
                Vector2.up * (ScadrykPlayerController.GamepadAimActivationMagnitude * 1.1f), Vector2.right, true), Vector2.up) > 0.001f)
        { SmokeFail("A mira do controle não conserva direção nem prioriza o analógico direito"); yield break; }
        Debug.Log("SCADRYK_SMOKE: mira do controle conserva o twin-stick, ativa com 5% da faixa filtrada e usa fallback de movimento");
        Vector2 smallStickBelowThreshold = Vector2.right * (ScadrykPlayerController.GamepadAimActivationMagnitude * 0.9f);
        Vector2 smallStickAboveThreshold = Vector2.right * (ScadrykPlayerController.GamepadAimActivationMagnitude * 1.1f);
        if (GamepadStickActivity(smallStickBelowThreshold) ||
            !GamepadStickActivity(smallStickAboveThreshold) || GamepadStickActivity(Vector2.zero) ||
            MemoryInputModeAfterActivity("keyboard_mouse", false, GamepadStickActivity(smallStickBelowThreshold)) != "keyboard_mouse" ||
            MemoryInputModeAfterActivity("keyboard_mouse", false, GamepadStickActivity(smallStickAboveThreshold)) != "gamepad")
        { SmokeFail("A troca de dispositivo pelo analógico não acompanha o limiar de mira"); yield break; }
        Debug.Log("SCADRYK_SMOKE: atividade dos analógicos troca o dispositivo no mesmo limiar responsivo da mira");
        Vector2 stickDrift = ScadrykSettings.ApplyRadialDeadzone(new Vector2(0.12f, 0.03f), 0.19f);
        Vector2 intentionalDiagonal = ScadrykSettings.ApplyRadialDeadzone(new Vector2(0.15f, 0.15f), 0.19f);
        Vector2 intentionalAim = ScadrykSettings.ApplyRadialDeadzone(new Vector2(-0.4f, 0.3f), 0.19f);
        Vector2 fullStick = ScadrykSettings.ApplyRadialDeadzone(Vector2.right, 0.19f);
        Vector2 halfwayStick = ScadrykSettings.ApplyRadialDeadzone(Vector2.right * 0.595f, 0.19f);
        Vector2 invalidStick = ScadrykSettings.ApplyRadialDeadzone(new Vector2(float.NaN, 0f), 0.19f);
        Vector2 infiniteStick = ScadrykSettings.ApplyRadialDeadzone(new Vector2(0f, float.PositiveInfinity), 0.19f);
        string driftReadout = ScadrykSettings.GamepadStickReadout(new Vector2(0.12f, 0.03f), 0.19f);
        string movingReadout = ScadrykSettings.GamepadStickReadout(Vector2.right * 0.595f, 0.19f);
        if (stickDrift != Vector2.zero || invalidStick != Vector2.zero || infiniteStick != Vector2.zero ||
            ScadrykSettings.ClampFiniteSetting(float.NaN, 0f, 1f, ScadrykSettings.DefaultMusicVolume) != ScadrykSettings.DefaultMusicVolume ||
            ScadrykSettings.ClampFiniteSetting(float.PositiveInfinity, 0f, 1f, ScadrykSettings.DefaultEffectsVolume) != ScadrykSettings.DefaultEffectsVolume ||
            Mathf.Abs(ScadrykSettings.ClampFiniteSetting(-10f, 0f, 1f, 0.5f)) > 0.001f ||
            Mathf.Abs(ScadrykSettings.ClampFiniteSetting(2f, 0f, 1f, 0.5f) - 1f) > 0.001f ||
            intentionalDiagonal == Vector2.zero || intentionalAim == Vector2.zero ||
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
        bool enemyWarningsMeetReactionFloor = true;
        for (int biome = 0; biome < PhaseNames.Length; biome++)
        {
            for (int role = 0; role < 4; role++)
                enemyWarningsMeetReactionFloor &= ScadrykEnemyHealth.AttackWindupDuration(false, biome, role) >= ScadrykEnemyHealth.MinimumAttackWarningDuration;
            enemyWarningsMeetReactionFloor &= ScadrykEnemyHealth.AttackWindupDuration(true, biome, 0) >= ScadrykEnemyHealth.MinimumAttackWarningDuration;
        }
        for (int stage = 1; stage <= 3; stage++)
        {
            float expectedStarfallWarning = stage == 3 ? 1.64f : 1.81f;
            int expectedStarfallWarnings = stage == 1 ? 2 : stage == 2 ? 3 : 5;
            enemyWarningsMeetReactionFloor &= ScadrykEnemyHealth.FinalBossWindupDuration(stage) >= ScadrykEnemyHealth.MinimumAttackWarningDuration &&
                Mathf.Abs(ScadrykEnemyHealth.FinalStarfallWarningDuration(stage) - expectedStarfallWarning) <= 0.001f &&
                ScadrykEnemyHealth.FinalVolleyWarningCount(stage) == stage * 2 + 1 &&
                ScadrykEnemyHealth.FinalStarfallWarningCount(stage) == expectedStarfallWarnings &&
                ScadrykEnemyHealth.MaximumFinalBossWarningCount(stage) <= ScadrykEnemyHealth.AttackWarningCapacity &&
                ScadrykEnemyHealth.FinalBossWarningsFit(stage);
        }
        if (!enemyWarningsMeetReactionFloor ||
            ScadrykEnemyHealth.ThreatDirectionArrow(Vector2.zero, Vector2.right) != "→" ||
            ScadrykEnemyHealth.ThreatDirectionArrow(Vector2.zero, new Vector2(1f, 1f)) != "↗" ||
            ScadrykEnemyHealth.ThreatDirectionArrow(Vector2.zero, Vector2.up) != "↑" ||
            ScadrykEnemyHealth.ThreatDirectionArrow(Vector2.zero, new Vector2(-1f, 1f)) != "↖" ||
            ScadrykEnemyHealth.ThreatDirectionArrow(Vector2.zero, Vector2.left) != "←" ||
            ScadrykEnemyHealth.ThreatDirectionArrow(Vector2.zero, new Vector2(-1f, -1f)) != "↙" ||
            ScadrykEnemyHealth.ThreatDirectionArrow(Vector2.zero, Vector2.down) != "↓" ||
            ScadrykEnemyHealth.ThreatDirectionArrow(Vector2.zero, new Vector2(1f, -1f)) != "↘" ||
            ScadrykEnemyHealth.ThreatDirectionArrow(Vector2.zero, Vector2.zero) != "•")
        { SmokeFail("Um padrão inimigo anuncia dano por menos que o tempo de reação definido ou as setas de direção falharam"); yield break; }
        Debug.Log("SCADRYK_SMOKE: avisos preservam 0,7 s e setas identificam a ameaça nas oito direções");
        if (ScadrykPlayerController.InputBufferCanStart(1.1f, 1.3f, 1.2f) ||
            !ScadrykPlayerController.InputBufferCanStart(1.21f, 1.3f, 1.2f) ||
            ScadrykPlayerController.InputBufferCanStart(1.31f, 1.3f, 1.2f) ||
            ScadrykPlayerController.CanUpdateAim(1.1f, 1.2f) ||
            !ScadrykPlayerController.CanUpdateAim(1.2f, 1.2f) ||
            ScadrykPlayerController.AbilityBufferWindow <= 0f || ScadrykPlayerController.AbilityBufferWindow > 0.25f ||
            ScadrykPlayerController.ShockwaveBufferWindow <= 0f || ScadrykPlayerController.ShockwaveBufferWindow > ScadrykPlayerController.AbilityBufferWindow ||
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
            Mathf.Abs(ScadrykEnemyHealth.BasicFinisherKnockbackVelocity(new Vector2(0f, 2f), Vector2.zero, Vector2.right).y - ScadrykEnemyHealth.BasicFinisherKnockbackSpeed) > 0.001f ||
            Mathf.Abs(ScadrykEnemyHealth.BasicFinisherKnockbackVelocity(Vector2.zero, Vector2.zero, Vector2.left).x + ScadrykEnemyHealth.BasicFinisherKnockbackSpeed) > 0.001f ||
            ScadrykEnemyHealth.BasicFinisherKnockbackVelocity(Vector2.zero, Vector2.zero, Vector2.zero).sqrMagnitude > 0.001f ||
            ScadrykEnemyHealth.BasicFinisherKnockbackDuration <= 0f ||
            ScadrykPlayerController.DashBufferCanStart(1.1f, 1.3f, 1.2f) ||
            !ScadrykPlayerController.DashBufferCanStart(1.21f, 1.3f, 1.2f) ||
            ScadrykPlayerController.DashBufferCanStart(1.31f, 1.3f, 1.2f) ||
            Mathf.Abs(ScadrykPlayerController.DashInvulnerabilityUntil(1.62f, 1.4f,
                ScadrykPlayerController.DashPostInvulnerabilityGrace) - 1.62f) > 0.001f ||
            Mathf.Abs(ScadrykPlayerController.DashInvulnerabilityUntil(1.2f, 1.4f,
                ScadrykPlayerController.DashPostInvulnerabilityGrace) - 1.48f) > 0.001f ||
            Mathf.Abs(ScadrykPlayerController.DashBufferWindow - 0.12f) > 0.001f)
        { SmokeFail("Buffers, proteção de invulnerabilidade do dash ou combo perderam suas janelas válidas"); yield break; }
        Debug.Log("SCADRYK_SMOKE: combo, buffers de ataque/esquiva/poder e invulnerabilidade preservam as janelas previstas");
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
            Mathf.Abs(ScadrykEnemyHealth.AreaPulseRadiusForBiome(0) - 3.2f) > 0.001f ||
            Mathf.Abs(ScadrykEnemyHealth.AreaPulseRadiusForBiome(1) - ScadrykEnemyHealth.SaltPulseRadius) > 0.001f ||
            Mathf.Abs(ScadrykEnemyHealth.AreaPulseRadiusForBiome(2) - ScadrykEnemyHealth.CoastalPulseRadius) > 0.001f ||
            ScadrykEnemyHealth.AreaPulseContains(Vector2.zero, new Vector2(3.64f, 0f), 1) == false ||
            ScadrykEnemyHealth.AreaPulseContains(Vector2.zero, new Vector2(3.66f, 0f), 1) ||
            Mathf.Abs(ScadrykPlayerController.SaltCrustSlowScale - 0.74f) > 0.001f ||
            ScadrykEnemyHealth.SaltPulseSlowDuration <= 0f || ScadrykEnemyHealth.ForestPulseRootDuration <= 0f ||
            ScadrykEnemyHealth.CoastalPulseKnockbackSpeed <= 0f || ScadrykEnemyHealth.CoastalPulseKnockbackDuration <= 0f ||
            Vector2.Distance(ScadrykPlayerController.KnockbackVelocityFrom(Vector2.right, Vector2.zero, 4.2f), new Vector2(4.2f, 0f)) > 0.001f ||
            !ScadrykEnemyHealth.BellPulseContains(Vector2.zero, new Vector2(5.29f, 0f)) ||
            ScadrykEnemyHealth.BellPulseContains(Vector2.zero, new Vector2(5.31f, 0f)) ||
            Mathf.Abs(ScadrykEnemyHealth.BellPulseDiameter - ScadrykEnemyHealth.BellPulseRadius * 2f) > 0.001f ||
            !ScadrykEnemyHealth.SupportPulseContains(Vector2.zero, new Vector2(2.49f, 0f)) ||
            ScadrykEnemyHealth.SupportPulseContains(Vector2.zero, new Vector2(2.51f, 0f)) ||
            Mathf.Abs(ScadrykEnemyHealth.SupportPulseDiameter - ScadrykEnemyHealth.SupportPulseRadius * 2f) > 0.001f ||
            !ScadrykEnemyHealth.SupportPulseWouldHeal(85f, 100f, ScadrykEnemyHealth.SupportPulseRadius) ||
            ScadrykEnemyHealth.SupportPulseWouldHeal(100f, 100f, 0f) ||
            ScadrykEnemyHealth.SupportPulseWouldHeal(85f, 100f, ScadrykEnemyHealth.SupportPulseRadius + 0.01f) ||
            ScadrykEnemyHealth.SupportPulseWouldHeal(10f, 0f, 0f))
        { SmokeFail("A área, o raio anunciado ou o efeito de controle do pulso diverge do bioma"); yield break; }
        Debug.Log("SCADRYK_SMOKE: pulsos de bosque, sal e costa conferem raio e efeito regional");
        if (ScadrykPlayerController.StatusTint(false, true, false, false) == ScadrykPlayerController.StatusTint(false, false, true, false) ||
            ScadrykPlayerController.StatusTint(false, false, true, false) == ScadrykPlayerController.StatusTint(false, false, false, true) ||
            ScadrykPlayerController.StatusTint(true, true, true, true) != ScadrykPlayerController.StatusTint(true, false, false, false) ||
            ScadrykPlayerController.StatusTint(false, false, false, false) != Color.white)
        { SmokeFail("A cor de Nara não distingue enraizamento, crosta de sal e empurrão, ou não prioriza invulnerabilidade"); yield break; }
        Debug.Log("SCADRYK_SMOKE: Nara sinaliza visualmente cada controle regional e mantém prioridade da invulnerabilidade");
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
        bool worldArtLoaded = HasAllSprites(environmentSprites, 16) && nimaMemoryPortraitSprite != null && HasAllSprites(phaseGroundSprites, 3) &&
            HasAllSprites(phaseApproachGroundSprites, 3) &&
            HasAllSprites(phaseCenterGroundSprites, 3) && HasAllSprites(phaseGuardianGroundSprites, 3) && HasAllSprites(phaseEnemySprites, 3) &&
            fallenWillowLogSprite != null && portalSprite != null && witnessSprite != null;
        bool distinctRoomGroundsSelected = true;
        for (int biome = 0; biome < 3; biome++)
        {
            distinctRoomGroundsSelected &= GroundSpriteForRoom(biome, 0, 0f, phaseGroundSprites, phaseCenterGroundSprites, phaseGuardianGroundSprites, phaseApproachGroundSprites) == phaseApproachGroundSprites[biome];
            distinctRoomGroundsSelected &= GroundSpriteForRoom(biome, 1, RoomSpacing, phaseGroundSprites, phaseCenterGroundSprites, phaseGuardianGroundSprites, phaseApproachGroundSprites) == phaseCenterGroundSprites[biome];
            distinctRoomGroundsSelected &= GroundSpriteForRoom(biome, 2, RoomSpacing * (RoomsPerPhase - 1), phaseGroundSprites, phaseCenterGroundSprites, phaseGuardianGroundSprites, phaseApproachGroundSprites) == phaseGuardianGroundSprites[biome];
            distinctRoomGroundsSelected &= GroundSpriteForRoom(biome, 0, -RoomSpacing, phaseGroundSprites, phaseCenterGroundSprites, phaseGuardianGroundSprites, phaseApproachGroundSprites) == phaseGroundSprites[biome];
            distinctRoomGroundsSelected &= GroundSpriteForRoom(biome, 1, -RoomSpacing, phaseGroundSprites, phaseCenterGroundSprites, phaseGuardianGroundSprites, phaseApproachGroundSprites) == phaseGroundSprites[biome];
            distinctRoomGroundsSelected &= GroundSpriteForRoom(biome, 2, 0f, phaseGroundSprites, phaseCenterGroundSprites, phaseGuardianGroundSprites, phaseApproachGroundSprites) == phaseGroundSprites[biome];
        }
        bool enemyArtLoaded = HasAllSprites(LoadSheetRow("Enemies", 0, false), 4) &&
            HasAllSprites(LoadSheetRow("Enemies", 1, false), 4) && HasAllSprites(LoadSheetRow("Enemies", 2, false), 4) &&
            HasAllSprites(LoadSheetRow("Enemies", 3, false), 4) && HasAllSprites(LoadSheetRow("Listener", 0, true), 16) &&
            HasAllSprites(LoadSheetRow("Bosses", 0, false), 4) && HasAllSprites(LoadSheetRow("BossesMidgame", 1, false), 4) &&
            HasAllSprites(LoadSheetRow("BossesFinalWave", 2, false), 4) && HasAllSprites(LoadSheetRow("BossesEndgame", 0, false), 4);
        if (!worldArtLoaded || !distinctRoomGroundsSelected || !enemyArtLoaded || !ScadrykEffects.RequiredCombatArtIsLoaded)
        { SmokeFail("Uma arte obrigatória de sala, personagem, guardião ou combate não carregou ou foi aplicada à rota errada"); yield break; }
        Debug.Log("SCADRYK_SMOKE: cenários de aproximação, salas centrais e arenas dos três biomas, personagens, guardiões e efeitos de combate carregados");
        if (!AllArtTexturesAreGpuOnly(out int artTextureCount))
        { SmokeFail("A biblioteca de arte contém textura legível pela CPU ou está vazia"); yield break; }
        Debug.Log("SCADRYK_SMOKE: " + artTextureCount + " texturas de arte carregadas sem cópia legível pela CPU");
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
        RecordPowerUse(0);
        RecordPowerUse(0);
        RecordPowerUse(1);
        RecordPowerUse(2);
        RecordPowerUse(-1);
        RecordPowerUse(roomPowerUses.Length);
        int recordedUnclearAttackMarks = RecordUnclearAttackMark();
        roomSecretsRevealed = 1;
        shallowWaterSeconds = 2.2f;
        saltCrosswindSeconds = AddSaltCrosswindExposure(0f, 1, 1, 2.8f);
        resonanceActivations = 2;
        if (!IsCoastalCurrentMetricsRoom(2, 1) || IsCoastalCurrentMetricsRoom(1, 1) || IsCoastalCurrentMetricsRoom(2, 2) ||
            AddCoastalCurrentEntry(4, 1, 1) != 4 || AddCoastalCurrentExposure(3f, 2, 0, 0.8f) != 3f ||
            Mathf.Abs(AddCoastalCurrentExposure(0f, 2, 1, -0.2f)) > 0.001f ||
            !IsShallowWaterMetricsBiome(0) || IsShallowWaterMetricsBiome(1) ||
            Mathf.Abs(AddShallowWaterExposure(1f, 0, 1.2f) - 2.2f) > 0.001f ||
            Mathf.Abs(AddShallowWaterExposure(1f, 1, 1.2f) - 1f) > 0.001f ||
            Mathf.Abs(AddShallowWaterExposure(1f, 0, -0.2f) - 1f) > 0.001f ||
            !IsSaltCrosswindMetricsRoom(1, 1) || IsSaltCrosswindMetricsRoom(2, 1) || IsSaltCrosswindMetricsRoom(1, 0) ||
            Mathf.Abs(AddSaltCrosswindExposure(1f, 1, 1, 1.5f) - 2.5f) > 0.001f ||
            Mathf.Abs(AddSaltCrosswindExposure(1f, 2, 1, 1.5f) - 1f) > 0.001f ||
            Mathf.Abs(AddSaltCrosswindExposure(1f, 1, 1, -0.2f) - 1f) > 0.001f ||
            !IsResonanceMetricsRoom(1, 1) || !IsResonanceMetricsRoom(2, 1) ||
            IsResonanceMetricsRoom(1, 0) || IsResonanceMetricsRoom(2, 2) ||
            AddResonanceActivation(1, 1, 1) != 2 || AddResonanceActivation(1, 1, 0) != 1)
        { SmokeFail("A telemetria ambiental contou bioma/sala errados ou exposição negativa"); yield break; }
        coastalCurrentEntries = AddCoastalCurrentEntry(AddCoastalCurrentEntry(0, 2, 1), 2, 1);
        coastalCurrentSeconds = AddCoastalCurrentExposure(AddCoastalCurrentExposure(0f, 2, 1, 2.1f), 2, 1, 2.5f);
        if (coastalCurrentEntries != 2 || Mathf.Abs(coastalCurrentSeconds - 4.6f) > 0.001f ||
            recordedUnclearAttackMarks != 1 || roomPowerUses[0] != 2 || roomPowerUses[1] != 1 || roomPowerUses[2] != 1)
        { SmokeFail("A telemetria não somou uso de poderes, marcas de ataque difícil ou entradas/segundos da corrente"); yield break; }
        if (SecretRevealCountAfterSuccess(0, false) != 1 || SecretRevealCountAfterSuccess(3, false) != 4 ||
            SecretRevealCountAfterSuccess(3, true) != 3)
        { SmokeFail("A telemetria de selos não conta descobertas ou interfere no smoke"); yield break; }
        var abilityBuffersField = typeof(ScadrykPlayerController).GetField("abilityBufferedUntil", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var abilityReadyField = typeof(ScadrykPlayerController).GetField("abilityReadyAt", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var aimLockField = typeof(ScadrykPlayerController).GetField("aimLockedUntil", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (player == null || abilityBuffersField == null || abilityReadyField == null || aimLockField == null)
        { SmokeFail("O smoke não encontrou os estados de mira e fila de poder para a troca de sala"); yield break; }
        float[] abilityBuffers = (float[])abilityBuffersField.GetValue(player);
        float[] abilityReadyAt = (float[])abilityReadyField.GetValue(player);
        float[] savedAbilityBuffers = (float[])abilityBuffers.Clone();
        float[] savedAbilityReadyAt = (float[])abilityReadyAt.Clone();
        float savedAimLock = (float)aimLockField.GetValue(player);
        Vector2 savedFacing = player.Facing;
        abilityBuffers[0] = Time.time + 1f;
        abilityReadyAt[0] = Time.time + 2f;
        aimLockField.SetValue(player, Time.time + 1f);
        var facingField = typeof(ScadrykPlayerController).GetField("facing", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (facingField == null) { SmokeFail("O smoke não encontrou a direção de mira para a troca de sala"); yield break; }
        facingField.SetValue(player, Vector2.left);
        player.ResetForRoom(player.transform.position);
        bool roomEntryClearsInput = System.Array.TrueForAll(abilityBuffers, value => value == 0f) &&
            (float)aimLockField.GetValue(player) == 0f && (player.Facing - Vector2.right).sqrMagnitude < 0.001f;
        bool roomEntryKeepsCooldown = Mathf.Abs(player.AbilityCooldownLeft(0) - 2f) < 0.05f;
        System.Array.Copy(savedAbilityBuffers, abilityBuffers, abilityBuffers.Length);
        System.Array.Copy(savedAbilityReadyAt, abilityReadyAt, abilityReadyAt.Length);
        aimLockField.SetValue(player, savedAimLock);
        facingField.SetValue(player, savedFacing);
        if (!roomEntryClearsInput || !roomEntryKeepsCooldown)
        { SmokeFail("A entrada de sala manteve um poder/mira em espera ou apagou a recarga ativa"); yield break; }
        Debug.Log("SCADRYK_SMOKE: entrada de sala limpa mira/comandos bufferizados, mantém cooldown e orienta ao portal");
        RecordInputMode("gamepad");
        RecordPlayerDeath();
        if (roomDeaths != 1) { SmokeFail("A derrota não foi contabilizada no instante da morte"); yield break; }
        string[] deathCsvColumns = FormatSessionCsvRow(false).TrimEnd('\n').Split(',');
        if (!System.Guid.TryParseExact(sessionId, "N", out _) ||
            deathCsvColumns.Length != SessionCsvHeader.TrimEnd('\n').Split(',').Length || deathCsvColumns[0] != sessionId ||
            deathCsvColumns[4] != "17" || deathCsvColumns[5] != "1" || deathCsvColumns[6] != "1" ||
            deathCsvColumns[7] != "2" || deathCsvColumns[8] != "1" || deathCsvColumns[9] != "1" ||
            deathCsvColumns[12] != "2" || deathCsvColumns[13] != "5" ||
            deathCsvColumns[15] != "1" || deathCsvColumns[16] != "2" || deathCsvColumns[17] != "2" || deathCsvColumns[18] != "3")
        { SmokeFail("O CSV de sessão perdeu duração, morte, marca de clareza, usos dos três poderes ou métricas ambientais"); yield break; }
        RestartCurrentRoom();
        if (statusMessage != "Você caiu. Vida restaurada; poderes e memórias preservados." ||
            player == null || (player.Facing - Vector2.right).sqrMagnitude > 0.001f ||
            roomActiveSeconds != 17f || roomDeaths != 1 || unclearAttackMarks != 1 ||
            roomPowerUses[0] != 2 || roomPowerUses[1] != 1 || roomPowerUses[2] != 1 || roomSecretsRevealed != 1 ||
            coastalCurrentEntries != 2 || Mathf.Abs(coastalCurrentSeconds - 4.6f) > 0.001f ||
            Mathf.Abs(shallowWaterSeconds - 2.2f) > 0.001f || Mathf.Abs(saltCrosswindSeconds - 2.8f) > 0.001f ||
            resonanceActivations != 2 || sessionInputMode != "gamepad")
        { SmokeFail("Morte reiniciou a sala mas perdeu tempo, mortes, poderes, uso da corrente ou dispositivo da sessão"); yield break; }
        string[] csvHeaderColumns = SessionCsvHeader.TrimEnd('\n').Split(',');
        string[] csvRowColumns = FormatSessionCsvRow(true).TrimEnd('\n').Split(',');
        if (csvRowColumns.Length != csvHeaderColumns.Length || csvHeaderColumns[4] != "seconds" ||
            csvHeaderColumns[5] != "deaths" || csvHeaderColumns[6] != "unclear_attacks" ||
            csvHeaderColumns[7] != "crescent" || csvHeaderColumns[8] != "salt" || csvHeaderColumns[9] != "tide" ||
            csvRowColumns[4] != "17" || csvRowColumns[5] != "1" || csvRowColumns[6] != "1" ||
            csvRowColumns[7] != "2" || csvRowColumns[8] != "1" || csvRowColumns[9] != "1" ||
            csvHeaderColumns[12] != "current_entries" ||
            csvHeaderColumns[13] != "current_seconds" || csvHeaderColumns[14] != "input_mode" ||
            csvHeaderColumns[15] != "seals_revealed" || csvHeaderColumns[16] != "shallow_water_seconds" ||
            csvHeaderColumns[17] != "resonance_activations" || csvHeaderColumns[18] != "salt_crosswind_seconds" || csvRowColumns[14] != "gamepad" ||
            csvRowColumns[15] != "1" || csvRowColumns[16] != "2" || csvRowColumns[17] != "2" || csvRowColumns[18] != "3")
        { SmokeFail("A linha CSV da sessão não corresponde ao cabeçalho ou omite o dispositivo"); yield break; }
        Debug.Log("SCADRYK_SMOKE: CSV v9 preserva correntes, água, ressonâncias e selos após morte e serializa cabeçalho/valores");
        ResetRoomSessionMetrics();
        if (roomActiveSeconds != 0f || roomDeaths != 0 || unclearAttackMarks != 0 || sessionInputMode != "unknown" ||
            roomPowerUses[0] != 0 || roomPowerUses[1] != 0 || roomPowerUses[2] != 0 ||
            coastalCurrentEntries != 0 || coastalCurrentSeconds != 0f || roomSecretsRevealed != 0 ||
            shallowWaterSeconds != 0f || saltCrosswindSeconds != 0f || resonanceActivations != 0)
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
        string minimapHeader = MiniMapHeaderText("Bosque");
        int compactMapHeaderSize = FitMiniMapHeaderFontSize(14, 500f, 260f);
        if (!minimapHeader.Contains("↑/↓ DESVIO") || !minimapHeader.Contains("◇ PEND.") ||
            !minimapHeader.Contains("◆ FEITO") || compactMapHeaderSize < 9 || compactMapHeaderSize >= 14)
        { SmokeFail("O minimapa não explica o significado dos desvios e dos estados do selo em espaço compacto"); yield break; }
        Debug.Log("SCADRYK_SMOKE: cabeçalho do minimapa explica desvios e selos pendentes/liberados");
        string[] routeMarkers = { " ↑", " ↓ ◇", " ↑ ◇" };
        for (int biome = 0; biome < PhaseNames.Length; biome++)
        {
            if (!MapRoomRouteLabel(biome, 1).EndsWith(routeMarkers[biome], System.StringComparison.Ordinal) ||
                MapRoomRouteLabel(biome, 0) != MapRoomLabel(biome, 0) ||
                MapRoomRouteLabel(biome, 2) != MapRoomLabel(biome, 2) ||
                (biome > 0 && PowerGrantedBySecretAt(biome, 0) != -1) ||
                PowerGrantedBySecretAt(biome, 1) != (biome == 1 ? 0 : biome == 2 ? 1 : -1))
            { SmokeFail("O mapa não sinaliza a direção correta do desvio de cada bioma"); yield break; }
        }
        Vector2 saltRouteSeal = WorldSecretPosition(Vector2.zero, 1, 1, 0);
        Vector2 coastRouteSeal = WorldSecretPosition(Vector2.zero, 2, 1, 1);
        if (Vector2.Distance(saltRouteSeal, new Vector2(0f, -9.3f)) > 0.001f ||
            Vector2.Distance(coastRouteSeal, new Vector2(0f, 9.3f)) > 0.001f ||
            !MapRoomRouteLabel(1, 1, false).EndsWith(" ◇", System.StringComparison.Ordinal) ||
            !MapRoomRouteLabel(1, 1, true).EndsWith(" ◆", System.StringComparison.Ordinal) ||
            !MapRoomRouteLabel(2, 1, true).EndsWith(" ◆", System.StringComparison.Ordinal) ||
            MapRoomRouteLabel(0, 1, true).EndsWith(" ◆", System.StringComparison.Ordinal))
        { SmokeFail("Os selos opcionais não estão posicionados dentro dos desvios que os anunciam"); yield break; }
        Debug.Log("SCADRYK_SMOKE: mapa orienta os desvios e distingue selos pendentes dos já liberados");
        if (!ShouldShowSideRouteHint(false, true, false, false, SideRouteHintRadius) ||
            ShouldShowSideRouteHint(false, true, false, false, SideRouteHintRadius + 0.01f) ||
            ShouldShowSideRouteHint(true, true, false, false, 0f) ||
            ShouldShowSideRouteHint(false, false, false, false, 0f) ||
            ShouldShowSideRouteHint(false, true, true, false, 0f) ||
            ShouldShowSideRouteHint(false, true, false, true, 0f) ||
            !SideRouteHintText(1).Contains("norte") || !SideRouteHintText(-1).Contains("sul"))
        { SmokeFail("A dica de desvio aparece fora de alcance, durante combate/status ou sem direção correta"); yield break; }
        Debug.Log("SCADRYK_SMOKE: dica de desvio contextual respeita alcance, combate, avisos ativos e direção");
        bool[] mapCleared = { true, false, false };
        if (WorldMapRoomStatus(0, 2, 1, 1, mapCleared) != 2 ||
            WorldMapRoomStatus(1, 0, 1, 1, mapCleared) != 2 ||
            WorldMapRoomStatus(1, 1, 1, 1, mapCleared) != 1 ||
            WorldMapRoomStatus(1, 2, 1, 1, mapCleared) != 0 ||
            WorldMapRoomStatus(2, 0, 1, 1, mapCleared) != 3 ||
            !WorldMapLayoutFits(400f, 360f) || !WorldMapLayoutFits(700f, 600f))
        { SmokeFail("O mapa geral perde estados de progresso ou sai da janela compacta"); yield break; }
        Debug.Log("SCADRYK_SMOKE: mapa separa bioma selado, sala adiante, atual e já concluída em duas resoluções");
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
            !PulseAnchorPromptText(4, 1, 0, "Q").Contains("Cristal marcado") ||
            !PulseAnchorPromptText(7, 0, 0, "Q").Contains("Farol") ||
            !PulseAnchorPromptText(7, 1, 0, "Q").Contains("primeiro acione o farol") ||
            !PulseAnchorPromptText(7, 1, 1, "Q").Contains("use Pulso (Q) agora") ||
            !AnchorActivationFeedback(7, 1, 2, "Q").Contains("O farol respondeu") ||
            !AnchorActivationFeedback(7, 1, 2, "Q").Contains("à direita, até o sino do recife") ||
            !AnchorActivationFeedback(7, 1, 2, "Q").Contains("Pulso (Q)") ||
            AnchorActivationFeedback(7, 2, 2, "Q") != "O vestígio respondeu. A passagem pode se abrir." ||
            AnchorActivationFeedback(4, 1, 2, "Q") != "Um sinal respondeu. Falta mais um." ||
            Mathf.Abs(ProgressHintDelay(0) - 16f) > 0.001f || Mathf.Abs(ProgressHintDelay(1) - 11f) > 0.001f ||
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
        Rect wideThreatCaption = ThreatCaptionPanelRect(1920f, 1080f, 1f, 2);
        Rect narrowThreatCaption = ThreatCaptionPanelRect(700f, 600f, 1.5f, 2);
        Rect compactThreatCaption = ThreatCaptionPanelRect(400f, 360f, 1.5f, 2);
        Rect narrowLegend = MiniMapLegendRect(narrowMap, 1.5f);
        Rect shortStatus = StatusLabelRect(400f, 360f, 24f);
        Rect wrappedStatus = StatusLabelRect(400f, 360f, 72f);
        var threatCaptionContent = new GUIContent();
        string longestCompactThreatText = ThreatCaptionText("Arauto fase 3 — queda de memória · saia da marca", 14);
        int compactThreatFontSize = ThreatCaptionFontSizeToFit(1.5f,
            (compactThreatCaption.height - 6f) * 0.5f, compactThreatCaption.width - 16f, longestCompactThreatText.Length);
        string cachedThreatCaptionLabel = null;
        RefreshThreatCaptionContent(threatCaptionContent, ref cachedThreatCaptionLabel, "Arauto fase 3 — onda de vazio", 0.81f);
        string firstThreatCaptionText = threatCaptionContent.text;
        RefreshThreatCaptionContent(threatCaptionContent, ref cachedThreatCaptionLabel, "Arauto fase 3 — onda de vazio", 0.89f);
        bool unchangedCaptionReusesText = object.ReferenceEquals(firstThreatCaptionText, threatCaptionContent.text);
        RefreshThreatCaptionContent(threatCaptionContent, ref cachedThreatCaptionLabel, "Bosque — Eco avanço de raízes", 0.21f);
        string orderedThreatFirst = "Ressonador — pulso";
        string orderedThreatSecond = "Atirador — estilhaço";
        float orderedThreatFirstImpact = 0.8f;
        float orderedThreatSecondImpact = 0.2f;
        OrderThreatCaptionPair(ref orderedThreatFirst, ref orderedThreatFirstImpact,
            ref orderedThreatSecond, ref orderedThreatSecondImpact);
        int cachedCooldownTenths = int.MinValue;
        string cachedCooldownLabel = null;
        string firstCooldownLabel = HudCooldownLabel(0.81f, ref cachedCooldownTenths, ref cachedCooldownLabel);
        string unchangedCooldownLabel = HudCooldownLabel(0.84f, ref cachedCooldownTenths, ref cachedCooldownLabel);
        bool unchangedCooldownReusesText = object.ReferenceEquals(firstCooldownLabel, unchangedCooldownLabel);
        string nextCooldownLabel = HudCooldownLabel(0.69f, ref cachedCooldownTenths, ref cachedCooldownLabel);
        bool changedCooldownRefreshesText = nextCooldownLabel == "0.7s" && !object.ReferenceEquals(firstCooldownLabel, nextCooldownLabel);
        string readyCooldownLabel = HudCooldownLabel(0.02f, ref cachedCooldownTenths, ref cachedCooldownLabel);
        string nearReadyCooldownLabel = HudCooldownLabel(0.04f, ref cachedCooldownTenths, ref cachedCooldownLabel);
        var cachedMapRoomContent = new GUIContent();
        bool cachedMapSecretFound = false, cachedMapRoomInitialized = false;
        bool firstMapRoomRefresh = RefreshMapRoomContent(cachedMapRoomContent, ref cachedMapSecretFound, ref cachedMapRoomInitialized, 1, 1, false);
        string firstMapRoomLabel = cachedMapRoomContent.text;
        bool repeatedMapRoomRefresh = RefreshMapRoomContent(cachedMapRoomContent, ref cachedMapSecretFound, ref cachedMapRoomInitialized, 1, 1, false);
        bool repeatedMapRoomReusesText = object.ReferenceEquals(firstMapRoomLabel, cachedMapRoomContent.text);
        bool revealedMapRoomRefresh = RefreshMapRoomContent(cachedMapRoomContent, ref cachedMapSecretFound, ref cachedMapRoomInitialized, 1, 1, true);
        var cachedHeaderContent = new GUIContent();
        int cachedHeaderBiome = -1;
        bool initialHeaderRefresh = RefreshMiniMapHeaderContent(cachedHeaderContent, ref cachedHeaderBiome, 0);
        string firstMiniMapHeader = cachedHeaderContent.text;
        bool repeatedHeaderRefresh = RefreshMiniMapHeaderContent(cachedHeaderContent, ref cachedHeaderBiome, 0);
        bool repeatedHeaderReusesText = object.ReferenceEquals(firstMiniMapHeader, cachedHeaderContent.text);
        bool changedHeaderRefresh = RefreshMiniMapHeaderContent(cachedHeaderContent, ref cachedHeaderBiome, 1);
        var cachedBossStageContent = new GUIContent();
        int cachedBossStage = -1;
        bool initialBossStageRefresh = RefreshFinalBossStageContent(cachedBossStageContent, ref cachedBossStage, 1);
        string firstBossStageLabel = cachedBossStageContent.text;
        bool repeatedBossStageRefresh = RefreshFinalBossStageContent(cachedBossStageContent, ref cachedBossStage, 1);
        bool repeatedBossStageReusesText = object.ReferenceEquals(firstBossStageLabel, cachedBossStageContent.text);
        bool changedBossStageRefresh = RefreshFinalBossStageContent(cachedBossStageContent, ref cachedBossStage, 2);
        string singleExposureCaption = ExposureCaption(1);
        string groupExposureCaption = ExposureCaption(3);
        if (HudHealthLabel(68.2f, 100f) != "VIDA 69/100" ||
            HudHealthLabel(0f, 100f) != "VIDA 0/100" ||
            HudHealthLabel(140f, 100f) != "VIDA 100/100" ||
            HudHealthTrackWidth(344f, 1.5f) > 236.001f ||
            344f - HudHealthTrackWidth(344f, 1.5f) - 8f < 99.999f ||
            AbilityHudLabel(0, "Q", "pronto") != "Q  Crescente · interrompe  ·  pronto" ||
            AbilityHudLabel(0, "Q", "pronto", true) != "Q  Crescente · interrompe  ·  pronto  ·  eco" ||
            AbilityHudLabel(1, "E", "4.2s") != "E  Estouro · expõe  ·  4.2s" ||
            AbilityHudLabel(2, "R", "pronto") != "R  Rompante · afasta  ·  pronto" ||
            !string.IsNullOrEmpty(AbilityHudLabel(3, "X", "pronto")) ||
            !unchangedCooldownReusesText || !changedCooldownRefreshesText || readyCooldownLabel != "pronto" || nearReadyCooldownLabel != "0.0s" ||
            !firstMapRoomRefresh || repeatedMapRoomRefresh || !repeatedMapRoomReusesText || !revealedMapRoomRefresh ||
            firstMapRoomLabel != "2. Bacia ↓ ◇" || cachedMapRoomContent.text != "2. Bacia ↓ ◆" ||
            !initialHeaderRefresh || repeatedHeaderRefresh || !repeatedHeaderReusesText || !changedHeaderRefresh ||
            firstMiniMapHeader != "MAPA · Bosque · ↑/↓ DESVIO · ◇ PEND. · ◆ FEITO" || cachedHeaderContent.text != "MAPA · Sal · ↑/↓ DESVIO · ◇ PEND. · ◆ FEITO" ||
            !initialBossStageRefresh || repeatedBossStageRefresh || !repeatedBossStageReusesText || !changedBossStageRefresh ||
            firstBossStageLabel != "O ARAUTO DO VAZIO  ·  FASE 1/3" || cachedBossStageContent.text != "O ARAUTO DO VAZIO  ·  FASE 2/3" ||
            !HudPanelFits(compactHud, 400f, 360f) || MiniMapPanelFits(compactMap, compactHud, 400f, 360f) ||
            !HudPanelFits(narrowHud, 700f, 600f) || !MiniMapPanelFits(narrowMap, narrowHud, 700f, 600f) ||
            wideThreatCaption.yMin < 72f || wideThreatCaption.Overlaps(HudPanelRect(1920f, 1080f, 1f)) ||
            wideThreatCaption.Overlaps(MiniMapPanelRect(1920f, 1080f, 1f)) ||
            narrowThreatCaption.xMin < 8f || narrowThreatCaption.yMin < 8f ||
            narrowThreatCaption.xMax > 692f || narrowThreatCaption.yMax > 592f || narrowThreatCaption.Overlaps(narrowHud) ||
            narrowThreatCaption.Overlaps(narrowMap) || narrowThreatCaption.yMax > 556f ||
            compactThreatCaption.xMin < 28f || compactThreatCaption.yMin < 196f ||
            compactThreatCaption.yMax > compactHud.yMax - 10f || compactThreatCaption.xMax > compactHud.xMax - 8f ||
            ThreatCaptionFontSize(1.5f, (compactThreatCaption.height - 6f) * 0.5f) > 14 ||
            compactThreatFontSize < 9 || compactThreatFontSize > 14 ||
            compactThreatFontSize * longestCompactThreatText.Length * 0.52f > compactThreatCaption.width - 16f + 0.001f ||
            !unchangedCaptionReusesText || ThreatCaptionTenthsUntilImpact(0.71f) != 8 ||
            orderedThreatFirst != "Atirador — estilhaço" || orderedThreatSecond != "Ressonador — pulso" ||
            Mathf.Abs(orderedThreatFirstImpact - 0.2f) > 0.001f || Mathf.Abs(orderedThreatSecondImpact - 0.8f) > 0.001f ||
            ThreatCaptionTenthsUntilImpact(-0.1f) != 0 ||
            ThreatCaptionText("Bosque — Eco avanço de raízes", 3) != "0,3 s · Bosque — Eco avanço de raízes" ||
            threatCaptionContent.text != "0,3 s · Bosque — Eco avanço de raízes" ||
            !string.IsNullOrEmpty(ExposureCaption(0)) || singleExposureCaption != "ESTOURO ATIVO · 1 ECO EXPOSTO · +30% DANO" ||
            groupExposureCaption != "ESTOURO ATIVO · 3 ECOS EXPOSTOS · +30% DANO" ||
            narrowLegend.yMin < narrowMap.yMin || narrowLegend.yMax > narrowMap.yMax ||
            wrappedStatus.height <= shortStatus.height || wrappedStatus.xMin < 0f || wrappedStatus.xMax > 400f ||
            wrappedStatus.yMin < 0f || wrappedStatus.yMax > 360f)
        { SmokeFail("HUD, minimapa ou aviso de status sai da tela numa janela compacta com texto ampliado"); yield break; }
        Debug.Log("SCADRYK_SMOKE: cache de recargas, HUD, legenda do impacto e funções táticas cabem em 400x360 com texto ampliado");
        if (!OptionsLayoutFits(600f) || !OptionsResetRowFits(400f))
        { SmokeFail("O menu de opções corta as teclas remapeáveis ou os controles de retorno a 600px de altura"); yield break; }
        Debug.Log("SCADRYK_SMOKE: menu de opções e restauração de comandos cabem em janela compacta");
        float shortOptionsViewport = CompactOptionsViewportHeight(360f);
        Rect lastCompactKey = CompactOptionsSelectionRect(1, 14);
        Rect lastCompactPad = CompactOptionsSelectionRect(2, 10);
        Rect compactPadDeadzone = CompactOptionsSelectionRect(2, 12);
        Rect compactFullscreen = CompactOptionsSelectionRect(0, 9);
        Rect compactFrameRate = CompactOptionsSelectionRect(0, 10);
        float keyListScroll = ScrollOptionItemIntoView(0f, shortOptionsViewport, lastCompactKey.y, lastCompactKey.height, CompactOptionsContentHeight(1));
        float padListScroll = ScrollOptionItemIntoView(0f, shortOptionsViewport, lastCompactPad.y, lastCompactPad.height, CompactOptionsContentHeight(2));
        float padDeadzoneScroll = ScrollOptionItemIntoView(0f, shortOptionsViewport, compactPadDeadzone.y, compactPadDeadzone.height, CompactOptionsContentHeight(2));
        float fullscreenScroll = ScrollOptionItemIntoView(0f, shortOptionsViewport, compactFullscreen.y, compactFullscreen.height, CompactOptionsContentHeight(0));
        float frameRateScroll = ScrollOptionItemIntoView(0f, shortOptionsViewport, compactFrameRate.y, compactFrameRate.height, CompactOptionsContentHeight(0));
        if (!CompactOptionsLayoutFits(400f, 360f) || !CompactOptionsLayoutFits(700f, 600f) || keyListScroll <= 0f || padListScroll <= 0f ||
            lastCompactKey.y + lastCompactKey.height > keyListScroll + shortOptionsViewport + 0.001f ||
            lastCompactPad.y + lastCompactPad.height > padListScroll + shortOptionsViewport + 0.001f ||
            compactPadDeadzone.y + compactPadDeadzone.height > padDeadzoneScroll + shortOptionsViewport + 0.001f ||
            compactFullscreen.y + compactFullscreen.height > fullscreenScroll + shortOptionsViewport + 0.001f ||
            compactFrameRate.y + compactFrameRate.height > frameRateScroll + shortOptionsViewport + 0.001f ||
            Mathf.Abs(compactPadDeadzone.height - 108f) > 0.001f ||
            keyListScroll > CompactOptionsContentHeight(1) - shortOptionsViewport ||
            padListScroll > CompactOptionsContentHeight(2) - shortOptionsViewport ||
            padDeadzoneScroll > CompactOptionsContentHeight(2) - shortOptionsViewport ||
            fullscreenScroll > CompactOptionsContentHeight(0) - shortOptionsViewport ||
            frameRateScroll > CompactOptionsContentHeight(0) - shortOptionsViewport)
        { SmokeFail("As opções roláveis não mantêm as últimas ações, FPS, tela cheia ou zona morta visíveis em 400x360"); yield break; }
        Debug.Log("SCADRYK_SMOKE: opções roláveis preservam remapeamento, FPS, tela cheia e zona morta em 400x360");
        if (!SoundOptionsLayoutFits(600f))
        { SmokeFail("O controle de alto contraste ou as instruções de avisos saem do menu em janela baixa"); yield break; }
        bool frameRateModesWork = ScadrykSettings.FrameRateTargetForMode(ScadrykSettings.FrameRateMonitor) == -1 &&
            ScadrykSettings.VSyncForFrameRateMode(ScadrykSettings.FrameRateMonitor) == 1 &&
            ScadrykSettings.FrameRateTargetForMode(ScadrykSettings.FrameRate30) == 30 &&
            ScadrykSettings.VSyncForFrameRateMode(ScadrykSettings.FrameRate30) == 0 &&
            ScadrykSettings.FrameRateTargetForMode(ScadrykSettings.FrameRate60) == 60 &&
            ScadrykSettings.VSyncForFrameRateMode(ScadrykSettings.FrameRate60) == 0 &&
            ScadrykSettings.NextFrameRateMode(ScadrykSettings.FrameRate60, 1) == ScadrykSettings.FrameRateMonitor &&
            ScadrykSettings.NextFrameRateMode(ScadrykSettings.FrameRateMonitor, -1) == ScadrykSettings.FrameRate60 &&
            ScadrykSettings.FrameRateModeLabel(ScadrykSettings.FrameRateMonitor) == "Auto FPS" &&
            ScadrykSettings.FrameRateModeLabel(ScadrykSettings.FrameRate30) == "30 FPS" &&
            ScadrykSettings.FrameRateModeLabel(ScadrykSettings.FrameRate60) == "60 FPS";
        bool mouseOptionFocusWorks = OptionSelectionAfterClick(true, 3, 10) == 10 &&
            OptionSelectionAfterClick(false, 3, 10) == 3;
        if (!mouseOptionFocusWorks)
        { SmokeFail("Um clique no menu não atualiza a seleção ativa, ou uma moldura sem clique mudou o foco"); yield break; }
        Debug.Log("SCADRYK_SMOKE: seleção visual do menu acompanha cliques sem mover o foco em repinturas");
        Vector2 aimMarkerRight = GamepadAimMarkerWorldPosition(new Vector2(2f, -1f), Vector2.right * 2f);
        Vector2 aimMarkerFallback = GamepadAimMarkerWorldPosition(new Vector2(2f, -1f), Vector2.zero);
        if (Vector2.Distance(aimMarkerRight, new Vector2(3.45f, -1f)) > 0.001f ||
            Vector2.Distance(aimMarkerFallback, new Vector2(3.45f, -1f)) > 0.001f)
        { SmokeFail("O marcador de mira do controle não segue a direção normalizada ou a direção padrão"); yield break; }
        Debug.Log("SCADRYK_SMOKE: marcador de mira do controle segue a direção de ataque");
        if (!frameRateModesWork)
        { SmokeFail("Os limites de FPS, a sincronização com o monitor ou a rotação das opções estão inconsistentes"); yield break; }
        Debug.Log("SCADRYK_SMOKE: 30/60 FPS usam limite fixo e Auto FPS acompanha a atualização do monitor");
        bool oldHighContrast = ScadrykSettings.HighContrastWarnings;
        Color testWarning = new Color(0.2f, 0.4f, 0.8f, 0.6f);
        ScadrykSettings.HighContrastWarnings = false;
        float standardWarningWidth = ScadrykEffects.DangerWarningWidth(1f);
        Color standardDangerTint = ScadrykEffects.DangerWarningTint(testWarning, true);
        Color standardAmbientTint = ScadrykEffects.DangerWarningTint(testWarning, false);
        ScadrykSettings.HighContrastWarnings = true;
        float highContrastWarningWidth = ScadrykEffects.DangerWarningWidth(1f);
        Color highContrastDangerTint = ScadrykEffects.DangerWarningTint(testWarning, true);
        Color highContrastAmbientTint = ScadrykEffects.DangerWarningTint(testWarning, false);
        bool highContrastBrightensSignal = highContrastDangerTint.r > testWarning.r && highContrastAmbientTint == standardAmbientTint;
        Vector3 expandedWarningHighContrastScale = ScadrykTimedVisual.DangerWarningVisualScale(Vector3.one, 1.2f, true, true);
        Vector3 expandedWarningStandardScale = ScadrykTimedVisual.DangerWarningVisualScale(Vector3.one, 1.2f, false, true);
        ScadrykSettings.HighContrastWarnings = oldHighContrast;
        if (Mathf.Abs(standardWarningWidth - 1f) > 0.001f || Mathf.Abs(highContrastWarningWidth - 1.4f) > 0.001f ||
            standardDangerTint != testWarning || standardAmbientTint != testWarning || !highContrastBrightensSignal ||
            Mathf.Abs(expandedWarningHighContrastScale.y - 1.68f) > 0.001f || Mathf.Abs(expandedWarningHighContrastScale.x - 1.2f) > 0.001f ||
            Mathf.Abs(expandedWarningStandardScale.y - 1.2f) > 0.001f)
        { SmokeFail("A opção de alto contraste não amplia e clareia os avisos de perigo"); yield break; }
        Debug.Log("SCADRYK_SMOKE: alto contraste clareia e amplia avisos lineares, em sprite e em expansão");
        float savedEffectsVolume = ScadrykSettings.EffectsVolume;
        bool savedWarningContrast = ScadrykSettings.HighContrastWarnings;
        bool savedReduceFlash = ScadrykSettings.ReduceFlash;
        GameObject mutedWarningProbe = null;
        GameObject spriteWarningProbe = null;
        bool warningVisibleWithoutSound = false, spriteWarningRemainsLegible = false;
        try
        {
            ScadrykSettings.EffectsVolume = 0f;
            ScadrykSettings.HighContrastWarnings = false;
            ScadrykSettings.ReduceFlash = true;
            mutedWarningProbe = ScadrykEffects.TelegraphLine(Vector2.zero, Vector2.right * 3f, new Color(0.2f, 0.4f, 0.8f, 0.6f), 0.1f, 1f);
            SpriteRenderer warningRenderer = mutedWarningProbe != null ? mutedWarningProbe.GetComponent<SpriteRenderer>() : null;
            ScadrykTimedVisual lineWarningVisual = mutedWarningProbe != null ? mutedWarningProbe.GetComponent<ScadrykTimedVisual>() : null;
            SpriteRenderer[] warningRenderers = mutedWarningProbe != null ? mutedWarningProbe.GetComponentsInChildren<SpriteRenderer>(true) : System.Array.Empty<SpriteRenderer>();
            bool warningHasDarkOutline = warningRenderers.Length == 2 && warningRenderers[0] != null && warningRenderers[1] != null &&
                warningRenderers[0] != warningRenderers[1] &&
                (warningRenderers[0].color.r < 0.08f || warningRenderers[1].color.r < 0.08f);
            ScadrykSettings.HighContrastWarnings = true;
            if (lineWarningVisual != null) lineWarningVisual.RefreshDangerWarningColor(1f);
            warningVisibleWithoutSound = warningRenderer != null && warningRenderer.sprite != null &&
                warningRenderer.color.a >= 0.98f && Mathf.Abs(mutedWarningProbe.transform.localScale.y - 0.14f) < 0.001f && warningHasDarkOutline;
            ScadrykSettings.HighContrastWarnings = false;
            spriteWarningProbe = ScadrykEffects.CreateSpriteEffect(ScadrykEffects.WhiteSprite, Vector2.zero,
                Vector2.one, new Color(0.2f, 0.4f, 0.8f, 0.6f), 1f, "Smoke sprite attack warning", false, true, true);
            SpriteRenderer spriteWarningRenderer = spriteWarningProbe != null ? spriteWarningProbe.GetComponent<SpriteRenderer>() : null;
            ScadrykTimedVisual spriteWarningVisual = spriteWarningProbe != null ? spriteWarningProbe.GetComponent<ScadrykTimedVisual>() : null;
            Color spriteWarningBeforeContrast = spriteWarningRenderer != null ? spriteWarningRenderer.color : Color.clear;
            ScadrykSettings.HighContrastWarnings = true;
            if (spriteWarningVisual != null) spriteWarningVisual.RefreshDangerWarningColor(0f);
            Color spriteWarningAfterContrast = spriteWarningRenderer != null ? spriteWarningRenderer.color : Color.clear;
            SpriteRenderer spriteWarningOutline = spriteWarningProbe != null ?
                spriteWarningProbe.transform.Find("Danger warning contrast edge")?.GetComponent<SpriteRenderer>() : null;
            spriteWarningRemainsLegible = spriteWarningRenderer != null && spriteWarningRenderer.sprite != null &&
                spriteWarningVisual != null && spriteWarningVisual.IsDangerWarning &&
                spriteWarningAfterContrast.r > spriteWarningBeforeContrast.r && spriteWarningAfterContrast.a > spriteWarningBeforeContrast.a &&
                spriteWarningOutline != null && spriteWarningOutline.color.a >= 0.5f;
        }
        finally
        {
            if (mutedWarningProbe != null) Destroy(mutedWarningProbe);
            if (spriteWarningProbe != null) Destroy(spriteWarningProbe);
            ScadrykSettings.EffectsVolume = savedEffectsVolume;
            ScadrykSettings.HighContrastWarnings = savedWarningContrast;
            ScadrykSettings.ReduceFlash = savedReduceFlash;
        }
        if (!warningVisibleWithoutSound || !spriteWarningRemainsLegible)
        { SmokeFail("Um aviso de perigo visual sumiu, piscou ou ficou fraco com áudio silenciado, alto contraste e redução de flashes"); yield break; }
        Debug.Log("SCADRYK_SMOKE: avisos lineares e sprite mantêm borda escura, ampliam/clareiam ao mudar alto contraste, mesmo sem áudio e com flashes reduzidos; preferências restauradas");
        Color reducedFlashColor = ScadrykEffects.ApplyReducedFlash(Color.white, true);
        Color normalFlashColor = ScadrykEffects.ApplyReducedFlash(Color.white, false);
        Color reducedFallbackSlash = ScadrykEffects.EffectFlashColor(Color.white, true, false);
        Color preservedWarningColor = ScadrykEffects.EffectFlashColor(Color.white, true, true);
        if (reducedFlashColor.a >= normalFlashColor.a || reducedFallbackSlash.a >= normalFlashColor.a ||
            preservedWarningColor.a < normalFlashColor.a || ScadrykEffects.FlashAlphaScale(true) >= 1f || ScadrykEffects.FlashDiameterScale(true) >= 1f ||
            ScadrykEffects.FlashExpansionScale(1f, true) >= ScadrykEffects.FlashExpansionScale(1f, false) ||
            ScadrykEffects.DeathBurstMoteCount(true, true) >= ScadrykEffects.DeathBurstMoteCount(true, false) ||
            ScadrykEffects.DashAfterimageInterval(true) <= ScadrykEffects.DashAfterimageInterval(false) ||
            ScadrykEffects.AfterimageLifetime(true) >= ScadrykEffects.AfterimageLifetime(false) ||
            !ScadrykEffects.ShouldReduceEffectFlash(true, false) || ScadrykEffects.ShouldReduceEffectFlash(true, true) ||
            ScadrykEffects.ShouldReduceEffectFlash(false, false))
        { SmokeFail("Reduzir flashes ainda deixa efeitos comuns sem atenuação ou enfraquece avisos de perigo"); yield break; }
        Debug.Log("SCADRYK_SMOKE: reduzir flashes atenua impactos e rastros sem remover a leitura de perigo");
        bool settingsSaveIsThrottled = !ScadrykSettings.ShouldFlushDeferredSave(true, 2f, 2.45f) &&
            ScadrykSettings.ShouldFlushDeferredSave(true, 2.45f, 2.45f) &&
            !ScadrykSettings.ShouldFlushDeferredSave(false, 3f, 2.45f) &&
            Mathf.Abs(ScadrykSettings.DeferredSaveDeadline(false, 2f, 0f) - 2.45f) < 0.001f &&
            Mathf.Abs(ScadrykSettings.DeferredSaveDeadline(true, 3f, 2.45f) - 2.45f) < 0.001f;
        if (!settingsSaveIsThrottled)
        { SmokeFail("As opções não limitam a frequência de gravação dos sliders"); yield break; }
        Debug.Log("SCADRYK_SMOKE: sliders salvam periodicamente durante ajustes longos sem gravar a cada frame");
        Vector2Int largeWindow = ScadrykSettings.WindowedResolutionForDisplay(1920, 1080);
        Vector2Int laptopWindow = ScadrykSettings.WindowedResolutionForDisplay(1366, 768);
        Vector2Int smallWindow = ScadrykSettings.WindowedResolutionForDisplay(800, 600);
        bool windowedSizesFit = largeWindow.x <= 1280 && largeWindow.y <= 720 && largeWindow.x == 1280 && largeWindow.y == 720 &&
            laptopWindow.x <= 1366 * 0.9f + 1f && laptopWindow.y <= 768 * 0.9f + 1f &&
            smallWindow.x <= 800 * 0.9f + 1f && smallWindow.y <= 600 * 0.9f + 1f &&
            Mathf.Abs((float)laptopWindow.x / laptopWindow.y - 1366f / 768f) < 0.002f;
        if (!windowedSizesFit) { SmokeFail("A janela não cabe no monitor ou distorce a proporção da imagem"); yield break; }
        Debug.Log("SCADRYK_SMOKE: janela ajustada mantém a proporção em monitor 1080p, notebook e tela menor");
        if (!OverlayLayoutFits(400f, 360f) || !OverlayLayoutFits(1280f, 520f) || !OverlayLayoutFits(1280f, 720f) ||
            !VictoryOverlayLayoutFits(400f, 360f, 112f) || !VictoryOverlayLayoutFits(700f, 600f, 62f) ||
            !VictoryOverlayLayoutFits(1280f, 720f, 62f) ||
            !PauseMapOverlayLayoutFits(400f, 360f) || !PauseMapOverlayLayoutFits(1280f, 520f) ||
            !PauseMapOverlayLayoutFits(1280f, 720f))
        { SmokeFail("Um menu ou o desfecho de vitória não cabem com texto ampliado em janela compacta"); yield break; }
        Debug.Log("SCADRYK_SMOKE: menus e desfecho cabem em 400×360, 700×600 e 1280×720 com texto ampliado");
        if (TitleMenuMaximumSelection(false) != 3 || TitleMenuOptionsSelection(false) != 1 || TitleMenuJournalSelection(false) != 2 ||
            TitleMenuQuitSelection(false) != 3 || TitleMenuMaximumSelection(true) != 4 || TitleMenuOptionsSelection(true) != 2 ||
            TitleMenuJournalSelection(true) != 3 || TitleMenuQuitSelection(true) != 4)
        { SmokeFail("As opções de título ficam redundantes ou recebem índices incorretos conforme o save"); yield break; }
        Debug.Log("SCADRYK_SMOKE: menu inicial omite Nova jornada sem checkpoint e reindexa opções/diário corretamente");
        bool pauseGridNavigationWorks = MovePauseGridSelection(0, 0, 1) == 2 &&
            MovePauseGridSelection(3, 0, 1) == 5 && MovePauseGridSelection(4, 0, -1) == 2 &&
            MovePauseGridSelection(5, 0, -1) == 3 && MovePauseGridSelection(0, 1, 0) == 1 &&
            MovePauseGridSelection(1, -1, 0) == 0 && MovePauseGridSelection(4, 1, 0) == 5 &&
            MovePauseGridSelection(5, -1, 0) == 4 && MovePauseGridSelection(5, 0, 1) == 6 &&
            MovePauseGridSelection(6, -1, 0) == 6 && MovePauseGridSelection(6, 0, -1) == 4;
        if (!pauseGridNavigationWorks) { SmokeFail("A navegação da grade da pausa perde a seleção nas bordas"); yield break; }
        Debug.Log("SCADRYK_SMOKE: grade de sete opções da pausa mantém coluna e respeita as bordas");
        bool victoryMenuNavigationWorks = MoveVictoryMenuSelection(0, 1) == 1 && MoveVictoryMenuSelection(1, 1) == 2 &&
            MoveVictoryMenuSelection(2, 1) == 2 && MoveVictoryMenuSelection(2, -1) == 1 &&
            MoveVictoryMenuSelection(1, -1) == 0 && MoveVictoryMenuSelection(0, -1) == 0 && MoveVictoryMenuSelection(1, 0) == 1;
        if (!victoryMenuNavigationWorks) { SmokeFail("A seleção linear do menu de vitória pula opções ou sai dos limites"); yield break; }
        Debug.Log("SCADRYK_SMOKE: menu final percorre nova jornada, caderno e título em ordem e respeita as bordas");
        if (!StoryJournalLayoutFits(400f, 360f) || !StoryJournalLayoutFits(700f, 600f) ||
            !MemoryJournalPortraitLayoutFits(314f, 64f) || !MemoryJournalPortraitLayoutFits(614f, 64f) ||
            MemoryJournalCardTextWidth(290f, true) != 230f || MemoryJournalCardTextWidth(290f, false) != 290f)
        { SmokeFail("O caderno de memórias não cabe em janela compacta"); yield break; }
        Debug.Log("SCADRYK_SMOKE: caderno de memórias cabe em janela compacta e padrão");
        GameObject warningProbe = new GameObject("QA enemy attack warning");
        GameObject[] warningProbeList = { warningProbe };
        int remainingWarnings = ScadrykEnemyHealth.ClearTrackedAttackWarnings(warningProbeList, 1);
        if (remainingWarnings != 0 || warningProbe.activeSelf || warningProbeList[0] != null)
        { SmokeFail("Interromper um ataque não removeu seus avisos visuais pendentes"); yield break; }
        Debug.Log("SCADRYK_SMOKE: interromper um golpe desativa todos os telegráficos associados");
        if (!MemoryPanelFits(400f, 360f, 120f) || !MemoryPanelFits(700f, 600f, 90f) ||
            !MemoryPanelFits(400f, 440f, 180f, true) || !MemoryPanelFits(700f, 600f, 120f, true) ||
            !MemoryPortraitPanelDoesNotOverlap(400f, 440f, 180f) || !MemoryPortraitPanelDoesNotOverlap(700f, 600f, 120f) ||
            !MemoryHasNimaPortrait(BasinWitnessLines[0]) || !MemoryHasNimaPortrait(BasinWitnessLines[2]) ||
            !MemoryHasNimaPortrait(CoastWitnessLines[0]) || !MemoryHasNimaPortrait(CoastWitnessLines[2]) ||
            MemoryHasNimaPortrait(GuardianMemories[0]) ||
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
        Debug.Log("SCADRYK_SMOKE: memórias modais têm comandos por dispositivo, retrato de Nima só nas falas dela e layout compacto sem sobreposição");
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
            if (room == 4 || room == 7)
            {
                int requiredPower = room == 4 ? 0 : 1;
                Vector2 expectedSealPosition = WorldSecretPosition(roomCenter, phaseIndex, roomIndex, requiredPower);
                if (worldSecret == null || !player.HasAbility(requiredPower) ||
                    PowerGrantedBySecretAt(phaseIndex, roomIndex) != requiredPower ||
                    Vector2.Distance(worldSecret.transform.position, expectedSealPosition) > 0.01f ||
                    !ActiveRoomHasWalkableRoute(roomCenter, worldSecret.transform.position))
                { SmokeFail("O selo opcional da sala " + (room + 1) + " não está no desvio alcançável com o poder conquistado"); yield break; }
                ScadrykAmbientProp sealVisual = worldSecret.GetComponent<ScadrykAmbientProp>();
                if (sealVisual == null || !sealVisual.RevealsFor(player.transform))
                { SmokeFail("O selo opcional não vinculou sua revelação ao jogador na sala " + (room + 1)); yield break; }
                if (!SmokeRevealWorldSecret(requiredPower))
                { SmokeFail("O selo do desvio não exigiu o poder correto ou não concedeu cura na sala " + (room + 1)); yield break; }
            }
            if (room < RoomsPerPhase)
            {
                ScadrykTerrainZone[] waterZones = FindObjectsByType<ScadrykTerrainZone>();
                if (waterZones.Length == 0) { SmokeFail("Águas rasas sem zonas de terreno na sala " + (room + 1)); yield break; }
                foreach (ScadrykTerrainZone zone in waterZones)
                    if (zone.MovementScale >= 1f) { SmokeFail("Zona de água não reduz o movimento"); yield break; }
                if (phaseIndex == 0)
                {
                    Vector2 northPool = ForestOuterPoolPosition(roomCenter, 1);
                    Vector2 southPool = ForestOuterPoolPosition(roomCenter, -1);
                    bool northBankWet = false, southBankWet = false;
                    foreach (ScadrykTerrainZone zone in waterZones)
                    {
                        if (zone.HintKey != ScadrykTerrainZone.ShallowWaterKey) continue;
                        bool northMatch = Vector2.Distance((Vector2)zone.transform.position, northPool) < 0.05f;
                        bool southMatch = Vector2.Distance((Vector2)zone.transform.position, southPool) < 0.05f;
                        if (northMatch) northBankWet = HasAnimatedWaterAt(zone.transform.parent, northPool);
                        if (southMatch) southBankWet = HasAnimatedWaterAt(zone.transform.parent, southPool);
                    }
                    if (!northBankWet || !southBankWet)
                    { SmokeFail("As margens do bosque precisam manter poças lentas com animação de água"); yield break; }
                }
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
                const float routeDepth = 5.4f;
                float routeLaneY = routeSide * (RoomHalfHeight + routeDepth * 0.5f);
                Vector2 entryInside = roomCenter + new Vector2(routeEntry, routeSide * (RoomHalfHeight - 1f));
                Vector2 entryOutside = roomCenter + new Vector2(routeEntry, routeLaneY);
                Vector2 exitOutside = roomCenter + new Vector2(routeExit, routeLaneY);
                Vector2 exitInside = roomCenter + new Vector2(routeExit, routeSide * (RoomHalfHeight - 1f));
                if (!ActiveRoomHasWalkableRoute(roomCenter, entryInside, entryOutside) ||
                    !ActiveRoomHasWalkableRoute(roomCenter, entryOutside, exitOutside) ||
                    !ActiveRoomHasWalkableRoute(roomCenter, exitOutside, exitInside))
                { SmokeFail("O desvio alternativo não conecta a sala, o corredor externo e o retorno na sala " + (room + 1)); yield break; }
                Debug.Log("SCADRYK_SMOKE: entrada, corredor lateral e retorno navegáveis na sala " + (room + 1));
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
                var stageBreatherPendingField = bossType.GetField("pendingFinalStageBreather", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var stageBreatherUntilField = bossType.GetField("finalStageBreatherUntil", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var nextBossAttackField = bossType.GetField("nextAttackAt", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var beginBossAttack = bossType.GetMethod("BeginFinalBossAttack", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var resolveBossAttack = bossType.GetMethod("ResolveAttack", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (queuedStageField == null || attackIndexField == null || attackTargetField == null || stageBreatherPendingField == null ||
                    stageBreatherUntilField == null || nextBossAttackField == null || beginBossAttack == null || resolveBossAttack == null)
                { SmokeFail("Não foi possível preparar a checagem de fase do Arauto"); yield break; }
                foreach (ScadrykEnemyProjectile projectile in FindObjectsByType<ScadrykEnemyProjectile>())
                    if (projectile != null) { projectile.gameObject.SetActive(false); Destroy(projectile.gameObject); }
                ClearSmokeAttackWarnings();
                attackIndexField.SetValue(finalBoss, 0);
                attackTargetField.SetValue(finalBoss, (Vector2)player.transform.position);
                Vector2 volleyForward = ((Vector2)player.transform.position - (Vector2)finalBoss.transform.position).normalized;
                beginBossAttack.Invoke(finalBoss, null);
                if (!finalBoss.TryGetWindupAttackLabel(out string windupCaption, out float captionSecondsUntilImpact) ||
                    !windupCaption.Contains("Arauto fase 1") || !windupCaption.Contains("salva de Ecos") ||
                    captionSecondsUntilImpact <= 0f || captionSecondsUntilImpact > 1.36f)
                { SmokeFail("A legenda textual do Arauto não identificou o golpe durante a preparação"); yield break; }
                int announcedStage = (int)queuedStageField.GetValue(finalBoss);
                finalBoss.TakeDamage(400f);
                GameObject transitionPulse = BossStagePulse;
                ScadrykTimedVisual transitionSignal = transitionPulse != null ? transitionPulse.GetComponent<ScadrykTimedVisual>() : null;
                bool transitionPulseReadable = transitionPulse != null && transitionPulse.activeInHierarchy && transitionSignal != null &&
                    !transitionSignal.IsDangerWarning && Mathf.Abs(ScadrykEffects.RingVisualDiameter(transitionPulse) - 5.2f) < 0.02f &&
                    Vector2.Distance(transitionPulse.transform.position, finalBoss.transform.position) < 0.02f;
                float volleyWidth = ScadrykEnemyProjectile.HitboxDiameter(0.58f);
                if (announcedStage != 1 || finalBoss.FinalStage != 2 || finalBoss.DisplayStage != 1 ||
                    !SmokeAttackWarningsMatch((Vector2)finalBoss.transform.position, volleyForward, 7.2f, volleyWidth, 0.19f, 1) ||
                    !(bool)stageBreatherPendingField.GetValue(finalBoss) || !transitionPulseReadable)
                { SmokeFail("A transição não preservou o ataque anunciado ou o pulso inofensivo ficou ilegível"); yield break; }
                resolveBossAttack.Invoke(finalBoss, null);
                if (finalBoss.TryGetWindupAttackLabel(out _))
                { SmokeFail("A legenda textual do Arauto permaneceu ativa após o impacto do golpe"); yield break; }
                bool transitionBreatherApplied = !(bool)stageBreatherPendingField.GetValue(finalBoss) &&
                    (float)stageBreatherUntilField.GetValue(finalBoss) >= Time.time + ScadrykEnemyHealth.FinalStageBreatherDuration - 0.01f &&
                    (float)nextBossAttackField.GetValue(finalBoss) >= Time.time + ScadrykEnemyHealth.FinalStageAttackDelay - 0.01f;
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
                if (announcedVolleyCount != 3 || !volleyRangeMatchesWarning || !transitionBreatherApplied)
                { SmokeFail("A salva anunciada excedeu o aviso ou a transição não abriu a pausa segura após o golpe"); yield break; }
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

                attackTargetField.SetValue(finalBoss, (Vector2)player.transform.position);
                attackIndexField.SetValue(finalBoss, 0);
                beginBossAttack.Invoke(finalBoss, null);
                finalBoss.TakeDamage(600f);
                bool canceledStageAttackPending = finalBoss.FinalStage == 3 && finalBoss.DisplayStage == 2 &&
                    (bool)stageBreatherPendingField.GetValue(finalBoss);
                finalBoss.ApplyCrescentControl();
                bool interruptedTransitionBreatherApplied = canceledStageAttackPending && finalBoss.DisplayStage == 3 &&
                    !(bool)stageBreatherPendingField.GetValue(finalBoss) &&
                    (float)stageBreatherUntilField.GetValue(finalBoss) >= Time.time + ScadrykEnemyHealth.FinalStageBreatherDuration - 0.01f &&
                    (float)nextBossAttackField.GetValue(finalBoss) >= Time.time + ScadrykEnemyHealth.FinalStageAttackDelay - 0.01f;
                if (!interruptedTransitionBreatherApplied)
                { SmokeFail("Crescente cancelou o aviso do Arauto sem iniciar a pausa segura da nova fase"); yield break; }
                Debug.Log("SCADRYK_SMOKE: interrupção de Crescente conclui a mudança de fase e preserva a janela segura");
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
            ScadrykEnemyProjectile.Launch(roomCenter + Vector2.up * 6f, Vector2.right, 0.1f, 0f, roomRoot: ActiveRoomRoot);
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
        if (!nimaFarewellCompleted) { SmokeFail("Nima não atravessou visualmente o portal final antes de Nara"); yield break; }
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

    private static bool HasAnimatedWaterAt(Transform room, Vector2 position)
    {
        if (room == null) return false;
        for (int child = 0; child < room.childCount; child++)
        {
            Transform candidate = room.GetChild(child);
            if (candidate.name != "Outer tide pool" || Vector2.Distance((Vector2)candidate.position, position) >= 0.05f) continue;
            return candidate.TryGetComponent<ScadrykAmbientProp>(out var motion) && motion.Motion == ScadrykAmbientMotion.Water;
        }
        return false;
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
        var distantCover = new GameObject("Smoke-only second cover probe");
        try
        {
            Vector2 origin = roomCenter + Vector2.up * 25f + Vector2.left * 2f;
            Vector2 target = roomCenter + Vector2.up * 25f + Vector2.right * 2f;
            cover.transform.position = roomCenter + Vector2.up * 25f;
            BoxCollider2D collider = cover.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.35f, 1f);
            distantCover.transform.position = roomCenter + Vector2.up * 25f + Vector2.right * 0.8f;
            distantCover.AddComponent<BoxCollider2D>().size = new Vector2(0.35f, 1f);
            Physics2D.SyncTransforms();
            bool blocksWhenSolid = ScadrykCombatRules.WorldBlocks(origin, target);
            Vector2 clippedWarning = ScadrykCombatRules.ClipWarningLineEnd(origin, target, 0.34f);
            bool warningStopsBeforeNearestCover = clippedWarning.x < -0.34f && clippedWarning.x > -0.36f &&
                Mathf.Abs(clippedWarning.y - origin.y) < 0.001f;
            Vector2 clippedThrust = ScadrykCombatRules.ClipWarningLineEnd(origin, target,
                ScadrykEnemyHealth.RootLaneVisualWidth(0.86f, 0.28f));
            bool thrustVisualAndHitLaneStopAtCover = !ScadrykEnemyHealth.RootLaneHits(target, origin, clippedThrust, 0.86f, 0.28f) &&
                ScadrykEnemyHealth.RootLaneHits(origin + Vector2.right * 0.5f, origin, clippedThrust, 0.86f, 0.28f);
            bool coverStopsDirectionalPowers = ScadrykPlayerController.AbilityPathIsBlocked(0, origin, target) &&
                !ScadrykPlayerController.AbilityPathIsBlocked(1, origin, target) &&
                ScadrykPlayerController.AbilityPathIsBlocked(2, origin, target);
            distantCover.GetComponent<BoxCollider2D>().enabled = false;
            collider.isTrigger = true;
            Physics2D.SyncTransforms();
            bool ignoresTrigger = !ScadrykCombatRules.WorldBlocks(origin, target);
            Vector2 triggerOnlyWarning = ScadrykCombatRules.ClipWarningLineEnd(origin, target, 0.34f);
            bool warningIgnoresTrigger = (triggerOnlyWarning - target).sqrMagnitude < 0.001f;
            bool powersIgnoreTrigger = !ScadrykPlayerController.AbilityPathIsBlocked(0, origin, target) &&
                !ScadrykPlayerController.AbilityPathIsBlocked(1, origin, target) &&
                !ScadrykPlayerController.AbilityPathIsBlocked(2, origin, target);
            distantCover.GetComponent<BoxCollider2D>().enabled = false;
            collider.enabled = false;
            Physics2D.SyncTransforms();
            bool openLineRemainsClear = !ScadrykCombatRules.WorldBlocks(origin, target);
            Vector2 clearWarning = ScadrykCombatRules.ClipWarningLineEnd(origin, target, 0.34f);
            bool clearWarningKeepsFullLength = (clearWarning - target).sqrMagnitude < 0.001f;
            return blocksWhenSolid && warningStopsBeforeNearestCover && thrustVisualAndHitLaneStopAtCover && coverStopsDirectionalPowers &&
                ignoresTrigger && warningIgnoresTrigger && powersIgnoreTrigger && openLineRemainsClear && clearWarningKeepsFullLength;
        }
        finally
        {
            Destroy(cover);
            Destroy(distantCover);
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
            ScadrykEnemyProjectile solidProjectile = ScadrykEnemyProjectile.Launch(origin, Vector2.right, 25f, 0f, size: 0.34f, projectileName: projectileName, travelDistance: 10f, roomRoot: ActiveRoomRoot);
            for (int step = 0; step < 12 && FindSmokeProjectile(projectileName) != null; step++) yield return new WaitForFixedUpdate();
            bool solidStopsProjectile = FindSmokeProjectile(projectileName) == null;

            cover.GetComponent<BoxCollider2D>().isTrigger = true;
            Physics2D.SyncTransforms();
            ScadrykEnemyProjectile triggerProjectile = ScadrykEnemyProjectile.Launch(origin, Vector2.right, 25f, 0f, size: 0.34f, projectileName: projectileName, travelDistance: 10f, roomRoot: ActiveRoomRoot);
            for (int step = 0; step < 4; step++) yield return new WaitForFixedUpdate();
            bool triggerAllowsProjectile = FindSmokeProjectile(projectileName) != null;
            bool impactReusedProjectile = solidProjectile != null && solidProjectile == triggerProjectile;
            ScadrykEnemyProjectile cleanupProbe = FindSmokeProjectile(projectileName);
            ClearRoomProjectiles();
            bool deactivatedImmediately = cleanupProbe != null && !cleanupProbe.gameObject.activeSelf;
            yield return null;
            bool destroyedByCleanup = FindSmokeProjectile(projectileName) == null;
            smokeProjectileCoverPassed = solidStopsProjectile && triggerAllowsProjectile && impactReusedProjectile && deactivatedImmediately && destroyedByCleanup;
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
