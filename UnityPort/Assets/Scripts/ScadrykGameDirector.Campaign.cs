using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class ScadrykGameDirector
{
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
        string deathSource = player != null ? player.LastDamageSource : null;
        if (player != null) player.ResetCombatCooldowns();
        BuildPhase(phaseIndex, roomIndex, false, true);
        resumeFinalGuardianInterlude = ShouldContinueFinalGuardianInterludeAfterRestart(interludeWasPending, resumeFinalGuardianInterlude);
        StartFinalGuardianInterlude();
        ShowStatus(DeathRestartMessage(deathSource), 2.5f);
    }

    internal static string DeathRestartMessage(string sourceLabel) => string.IsNullOrWhiteSpace(sourceLabel)
        ? "Você caiu. Vida restaurada; poderes e memórias preservados."
        : "Você caiu para " + sourceLabel.Trim() + ". Vida restaurada; poderes e memórias preservados.";

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

}
