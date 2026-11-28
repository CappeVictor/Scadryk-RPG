using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed partial class ScadrykGameDirector
{
    public void RecordPowerUse(int index)
    {
        if (index >= 0 && index < roomPowerUses.Length) roomPowerUses[index]++;
    }

    internal int RecordUnclearAttackMark() => ++unclearAttackMarks;

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

    public void RecordSaltCrosswindExposure(float seconds)
    {
        if (!roomSessionRecorded)
            saltCrosswindSeconds = AddSaltCrosswindExposure(saltCrosswindSeconds, phaseIndex, roomIndex, seconds);
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

    internal static bool IsSaltCrosswindMetricsRoom(int phase, int room) => phase == 1 && room == 1;

    internal static float AddSaltCrosswindExposure(float currentSeconds, int phase, int room, float seconds) =>
        IsSaltCrosswindMetricsRoom(phase, room) ? currentSeconds + Mathf.Max(0f, seconds) : currentSeconds;

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
            Mathf.RoundToInt(Mathf.Max(0f, shallowWaterSeconds)) + "," + resonanceActivations + "," +
            Mathf.RoundToInt(Mathf.Max(0f, saltCrosswindSeconds)) + "\n";
    }

    private void RecordRoomSession()
    {
        if (smokeRun || showTitle || roomSessionRecorded) return;
        roomSessionRecorded = true;
        try
        {
            string path = Path.Combine(Application.persistentDataPath, "scadryk-sessions-v9.csv");
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

    internal static string EscapeCsvField(string value)
    {
        value = value ?? string.Empty;
        if (value.IndexOfAny(CsvSpecialCharacters) < 0) return value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    internal static string FormatAttackClarityEventCsvRow(string id, string utc, int phase, int room, string patterns) =>
        EscapeCsvField(id) + "," + EscapeCsvField(utc) + "," + phase + "," + room + "," +
        EscapeCsvField(string.IsNullOrWhiteSpace(patterns) ? "não identificado" : patterns) + "\n";

    private void OnApplicationQuit()
    {
        ScadrykSettings.FlushPendingSave();
        Time.timeScale = 1f;
        if (levelRoot != null && !showTitle && !showOpeningStory)
        {
            if (ShouldSaveCheckpointOnQuit(player != null, victory, showTitle, showOpeningStory, smokeRun))
                SaveProgress(phaseIndex, roomIndex);
            RecordRoomSession();
        }
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) ScadrykSettings.FlushPendingSave();
        if (ShouldSaveCheckpointOnApplicationPause(paused, player != null, victory, showTitle, showOpeningStory, smokeRun))
            SaveProgress(phaseIndex, roomIndex);
    }

    internal static bool ShouldSaveCheckpointOnQuit(bool hasPlayer, bool hasVictory, bool titleVisible,
        bool openingStoryVisible, bool smokeMode) =>
        hasPlayer && !hasVictory && !titleVisible && !openingStoryVisible && !smokeMode;

    internal static bool ShouldSaveCheckpointOnApplicationPause(bool appPaused, bool hasPlayer,
        bool hasVictory, bool titleVisible, bool openingStoryVisible, bool smokeMode) =>
        appPaused && ShouldSaveCheckpointOnQuit(hasPlayer, hasVictory, titleVisible, openingStoryVisible, smokeMode);

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
        int safePhase = Mathf.Clamp(phase, 0, PhaseNames.Length - 1);
        int safeRoom = Mathf.Clamp(room, 0, RoomsPerPhase - 1);
        PlayerPrefs.SetString(CheckpointProgressKey(keyPrefix), JsonUtility.ToJson(new CheckpointData
        { version = 1, phase = safePhase, room = safeRoom }));
        // Keep the old scalar pair as a recovery path for damaged or older checkpoint records.
        PlayerPrefs.SetInt(keyPrefix + "Phase", safePhase);
        PlayerPrefs.SetInt(keyPrefix + "Room", safeRoom);
        PlayerPrefs.Save();
    }

    internal static string CheckpointProgressKey(string keyPrefix) => keyPrefix + CheckpointKeySuffix;

    internal static void ClearCheckpoint(string keyPrefix)
    {
        PlayerPrefs.DeleteKey(CheckpointProgressKey(keyPrefix));
        PlayerPrefs.DeleteKey(keyPrefix + "Phase");
        PlayerPrefs.DeleteKey(keyPrefix + "Room");
    }

    internal static bool HasCheckpoint(string keyPrefix) => TryLoadCheckpoint(keyPrefix, out _, out _);

    internal static string WitnessProgressKey(string keyPrefix, int phase) =>
        keyPrefix + "Witness." + Mathf.Clamp(phase, 0, PhaseNames.Length - 1);

    internal static string WorldSecretProgressKey(string keyPrefix, int phase, int room) =>
        keyPrefix + "WorldSecret." + Mathf.Clamp(phase, 0, PhaseNames.Length - 1) + "." +
        Mathf.Clamp(room, 0, RoomsPerPhase - 1);

    internal static bool IsWorldSecretRevealed(string keyPrefix, int phase, int room) =>
        PlayerPrefs.GetInt(WorldSecretProgressKey(keyPrefix, phase, room), 0) == 1;

    internal static int CountWorldSecretsRevealed(string keyPrefix)
    {
        if (string.IsNullOrEmpty(keyPrefix)) return 0;
        int count = 0;
        for (int phase = 0; phase < PhaseNames.Length; phase++)
        for (int room = 0; room < RoomsPerPhase; room++)
            if (PowerGrantedBySecretAt(phase, room) >= 0 && IsWorldSecretRevealed(keyPrefix, phase, room)) count++;
        return count;
    }

    internal static bool MigrateLegacyWorldSecretProgress(string keyPrefix)
    {
        bool migrated = MigrateLegacyWorldSecret(keyPrefix, 1, 0, 1, 1);
        migrated |= MigrateLegacyWorldSecret(keyPrefix, 2, 0, 2, 1);
        if (migrated) PlayerPrefs.Save();
        return migrated;
    }

    private static bool MigrateLegacyWorldSecret(string keyPrefix, int oldPhase, int oldRoom, int newPhase, int newRoom)
    {
        string oldKey = WorldSecretProgressKey(keyPrefix, oldPhase, oldRoom);
        if (!PlayerPrefs.HasKey(oldKey)) return false;
        if (PlayerPrefs.GetInt(oldKey, 0) == 1)
            PlayerPrefs.SetInt(WorldSecretProgressKey(keyPrefix, newPhase, newRoom), 1);
        PlayerPrefs.DeleteKey(oldKey);
        return true;
    }

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

    internal static void ClearProgressAfterVictory(string keyPrefix)
    {
        ClearCheckpoint(keyPrefix);
        ClearCampaignProgress(keyPrefix);
        PlayerPrefs.Save();
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
            bool changed = false;
            foreach (string entry in data.entries)
            {
                if (string.IsNullOrWhiteSpace(entry)) { changed = true; continue; }
                string migratedEntry = MigrateLegacyStoryJournalEntry(entry);
                if (migratedEntry != entry) changed = true;
                if (entries.Contains(migratedEntry)) { changed = true; continue; }
                entries.Add(migratedEntry);
            }
            if (changed)
            {
                PlayerPrefs.SetString(key, JsonUtility.ToJson(new StoryJournalData { entries = entries }));
                PlayerPrefs.Save();
            }
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

    internal static string RoomStoryJournalEntry(string roomName, string story) =>
        string.IsNullOrWhiteSpace(roomName) || string.IsNullOrWhiteSpace(story)
            ? string.Empty
            : roomName.Trim() + "\n" + story.Trim();

    private void RecordCurrentRoomStory()
    {
        if (smokeRun || showTitle || showOpeningStory) return;
        string story = roomIndex == RoomsPerPhase - 1
            ? GuardianIntroductions[phaseIndex]
            : RoomStories[phaseIndex * RoomsPerPhase + roomIndex];
        RecordStoryJournalEntry("Scadryk.", RoomStoryJournalEntry(roomNames[phaseIndex][roomIndex], story));
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
        string recordKey = CheckpointProgressKey(keyPrefix);
        if (PlayerPrefs.HasKey(recordKey))
        {
            try
            {
                const int missingField = int.MinValue;
                var record = new CheckpointData { version = missingField, phase = missingField, room = missingField };
                JsonUtility.FromJsonOverwrite(PlayerPrefs.GetString(recordKey), record);
                if (record.version == 1 && record.phase != missingField && record.room != missingField)
                {
                    phase = Mathf.Clamp(record.phase, 0, PhaseNames.Length - 1);
                    room = Mathf.Clamp(record.room, 0, RoomsPerPhase - 1);
                    return true;
                }
            }
            catch (System.Exception) { }
        }

        bool hasLegacyPair = PlayerPrefs.HasKey(keyPrefix + "Phase") && PlayerPrefs.HasKey(keyPrefix + "Room");
        phase = hasLegacyPair ? Mathf.Clamp(PlayerPrefs.GetInt(keyPrefix + "Phase", 0), 0, PhaseNames.Length - 1) : 0;
        room = hasLegacyPair ? Mathf.Clamp(PlayerPrefs.GetInt(keyPrefix + "Room", 0), 0, RoomsPerPhase - 1) : 0;
        return hasLegacyPair;
    }
}
