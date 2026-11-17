using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

public sealed partial class Game
{
    private sealed class SaveData
    {
        public int Room { get; set; }
        public int Level { get; set; } = 1;
        public int Xp { get; set; }
        public int Gold { get; set; }
        public int Potions { get; set; } = 2;
        public int Damage { get; set; } = 25;
        public int HeroClass { get; set; }
        public float Health { get; set; } = 100;
        public float MaxHealth { get; set; } = 100;
        public int Weapon { get; set; }
        public int OwnedWeapons { get; set; } = 1;
        public int[] WeaponBonuses { get; set; } = new int[6];
        public int[] WeaponRarities { get; set; } = new int[6];
        public int[] WeaponAffixes { get; set; } = new int[6];
        public int[] WeaponAffixes2 { get; set; } = new int[6];
        public int[] EquippedSkills { get; set; } = new[] { 1, 0, 0, 0 };
        public int[] AlternateSkills { get; set; } = new int[4];
        public bool AlternateSkillsActive { get; set; }
        public int[] SkillVariants { get; set; } = new int[13];
        public bool Armor { get; set; }
        public bool Relic { get; set; }
        public int ArmorTrait { get; set; }
        public int RelicTrait { get; set; }
        public int HealthTalent { get; set; }
        public int SkillTalent { get; set; }
        public int DodgeTalent { get; set; }
        public int TechniqueTalent { get; set; }
        public int? CombatDiscipline { get; set; }
        public int? CombatMastery { get; set; }
        public int CombatFocus { get; set; }
        public int CombatApex { get; set; }
        public int CombatTempo { get; set; }
        public int CombatInstinct { get; set; }
        public int CombatCapstone { get; set; }
        public int CombatParagon { get; set; }
        public bool RoomCleared { get; set; }
        public bool Completed { get; set; }
        public int MemoriesFound { get; set; }
        public int QuestStage { get; set; }
        public long ClearedRooms { get; set; }
        public bool ArchiveSeal { get; set; }
        public bool RiverBell { get; set; }
        public bool LumeLens { get; set; }
        public bool NaraTrace { get; set; }
        public int OwnedCharms { get; set; }
        public int EquippedCharm { get; set; }
        public int EquippedCharm2 { get; set; }
        public int EquippedCharm3 { get; set; }
        public int TownEnding { get; set; }
        public bool ChorusMemory { get; set; }
        public bool FirstMemory { get; set; }
        public bool WestTrace { get; set; }
        public bool UpstreamTrace { get; set; }
        public int TideGlass { get; set; }
        public bool BeyondTrace { get; set; }
        public bool EbbSeal { get; set; }
        public bool CounterTrace { get; set; }
        public bool AnchorWard { get; set; }
        public bool RefrainSong { get; set; }
        public bool RiftVerse { get; set; }
        public bool DeepTrace { get; set; }
        public bool LanternSignal { get; set; }
        public bool BeaconLight { get; set; }
        public bool RouteMemory { get; set; }
        public bool SilenceMemory { get; set; }
        public bool BorrowedTrace { get; set; }
        public bool UnstruckTrace { get; set; }
        public int FinalEnding { get; set; }
        public int VigilBestFloor { get; set; }
        public bool HighContrastTelegraphs { get; set; }
        public bool ReducedScreenShake { get; set; }
        public int EliteWeaponMisses { get; set; }
        public int DifficultyTier { get; set; }
        public int FirstEchoQuest { get; set; }
        public bool FirstEchoCaveFound { get; set; }
        public bool FirstEchoReedCache { get; set; }
        public int FirstEchoDiscovered { get; set; }
        public bool SaltFrontierLocation { get; set; }
        public bool SaltFrontierBossAwakened { get; set; }
        public bool SaltFrontierBossDefeated { get; set; }
        public bool SaltFrontierWardenDefeated { get; set; }
        public int SaltFrontierSideProgress { get; set; }
        public bool SaltFrontierCacheFound { get; set; }
        public bool TideBoots { get; set; }
        public bool TideBootsOn { get; set; }
        public int TideBootsAspect { get; set; }
        public bool SaltVaultReliquaryCleared { get; set; }
    }

    private string SavePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Scadryk", "save.json");

    private bool IsCurrentRoomClearedForSave()
    {
        if (room == 3) return true;
        if (room == 49) return (saltFrontierProgress & 14) == 14 && enemies.Count == 0 && drops.Count == 0 && (!saltFrontierBossAwakened || saltFrontierBossDefeated);
        return enemies.Count == 0 && drops.Count == 0;
    }

    private void RestoreSaltFrontierCacheGear(SaveData s)
    {
        saltFrontierCacheFound = s.SaltFrontierCacheFound;
        tideBoots = s.TideBoots || s.SaltFrontierCacheFound;
        tideBootsOn = s.TideBootsOn || s.SaltFrontierCacheFound;
        tideBootsAspect = Math.Clamp(s.TideBootsAspect, 0, 2);
    }

    private SaveData CaptureSaveData() => new() { FirstEchoQuest = firstEchoQuest, FirstEchoCaveFound = firstEchoCaveFound, FirstEchoReedCache = firstEchoReedCache, FirstEchoDiscovered = firstEchoDiscovered, SaltFrontierLocation = saltFrontierReturnLocation, SaltFrontierBossAwakened = saltFrontierBossAwakened, SaltFrontierBossDefeated = saltFrontierBossDefeated, SaltFrontierWardenDefeated = saltFrontierWardenDefeated, SaltFrontierSideProgress = saltFrontierSideProgress, SaltFrontierCacheFound = saltFrontierCacheFound, TideBoots = tideBoots, TideBootsOn = tideBootsOn, TideBootsAspect = tideBootsAspect, SaltVaultReliquaryCleared = saltVaultReliquaryCleared, Room = room >= 43 ? 3 : room, Level = level, Xp = xp, Gold = gold, Potions = potions, Damage = damage, HeroClass = (int)heroClass, Health = health, MaxHealth = maxHealth, Weapon = weapon, OwnedWeapons = ownedWeapons, Armor = armor, Relic = relic, ArmorTrait = armorTrait, RelicTrait = relicTrait, HealthTalent = healthTalent, SkillTalent = skillTalent, DodgeTalent = dodgeTalent, TechniqueTalent = techniqueTalent, CombatDiscipline = combatDiscipline, CombatMastery = combatMastery, CombatFocus = combatFocus, CombatApex = combatApex, CombatTempo = combatTempo, CombatInstinct = combatInstinct, CombatCapstone = combatCapstone, CombatParagon = combatParagon, RoomCleared = IsCurrentRoomClearedForSave(), Completed = victory, MemoriesFound = memoriesFound, QuestStage = questStage, ClearedRooms = clearedRooms | (room != 3 && room < 43 && enemies.Count == 0 && drops.Count == 0 ? 1L << room : 0), ArchiveSeal = archiveSeal, RiverBell = riverBell, LumeLens = lumeLens, NaraTrace = naraTrace, ChorusMemory = chorusMemory, FirstMemory = firstMemory, WestTrace = westTrace, UpstreamTrace = upstreamTrace, TideGlass = tideGlass, BeyondTrace = beyondTrace, EbbSeal = ebbSeal, CounterTrace = counterTrace, AnchorWard = anchorWard, RefrainSong = refrainSong, RiftVerse = riftVerse, DeepTrace = deepTrace, LanternSignal = lanternSignal, BeaconLight = beaconLight, RouteMemory = routeMemory, SilenceMemory = silenceMemory, BorrowedTrace = borrowedTrace, UnstruckTrace = unstruckTrace, FinalEnding = finalEnding, VigilBestFloor = vigilBestFloor, HighContrastTelegraphs = highContrastTelegraphs, ReducedScreenShake = reducedScreenShake, EliteWeaponMisses = eliteWeaponMisses, DifficultyTier = difficultyTier, OwnedCharms = ownedCharms, EquippedCharm = equippedCharm, EquippedCharm2 = equippedCharm2, EquippedCharm3 = equippedCharm3, EquippedSkills = (alternateSkillsActive ? alternateSkills : equippedSkills).ToArray(), AlternateSkills = (alternateSkillsActive ? equippedSkills : alternateSkills).ToArray(), AlternateSkillsActive = alternateSkillsActive, SkillVariants = skillVariants.ToArray(), TownEnding = townEnding, WeaponBonuses = weaponBonuses.ToArray(), WeaponRarities = weaponRarities.ToArray(), WeaponAffixes = weaponAffixes.ToArray(), WeaponAffixes2 = weaponAffixes2.ToArray() };
    private static void WriteSaveFile(string path, string contents)
    {
        string directory = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(directory);
        string temporary = Path.Combine(directory, $".save-{Guid.NewGuid():N}.tmp");
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream)) { writer.Write(contents); writer.Flush(); stream.Flush(true); }
            File.Move(temporary, path, true);
        }
        finally
        {
            try { File.Delete(temporary); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }
    private bool SaveGame(string? destination = null)
    {
        try { WriteSaveFile(destination ?? SavePath, JsonSerializer.Serialize(CaptureSaveData())); notice = "PROGRESSO SALVO"; noticeTime = 3; return true; }
        catch (IOException) { notice = "NÃO FOI POSSÍVEL SALVAR • VERIFIQUE O ARQUIVO"; }
        catch (UnauthorizedAccessException) { notice = "NÃO FOI POSSÍVEL SALVAR • VERIFIQUE AS PERMISSÕES"; }
        noticeTime = 3; return false;
    }
    private string LegacySavePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Godot", "app_userdata", "Scadryk RPG", "scadryk-save.cfg");
    private SaveData ReadLegacySave()
    {
        if (!File.Exists(LegacySavePath)) throw new FileNotFoundException();
        string section = ""; var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string raw in File.ReadLines(LegacySavePath)) { string line = raw.Trim(); if (line.StartsWith('[') && line.EndsWith(']')) { section = line[1..^1]; continue; } int split = line.IndexOf('='); if (split > 0) values[section + "." + line[..split].Trim()] = line[(split + 1)..].Trim().Trim('"'); }
        string Get(string key, string fallback = "") => values.GetValueOrDefault("player." + key, fallback);
        int Int(string key, int fallback = 0) => int.TryParse(Get(key), out int n) ? n : fallback;
        float Float(string key, float fallback) => float.TryParse(Get(key), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float n) ? n : fallback;
        bool Bool(string key, bool fallback = false) => bool.TryParse(Get(key), out bool b) ? b : fallback;
        int savedWeapon = Int("weapon"); int savedRoom = Int("room");
        return new SaveData { Room = savedRoom, Level = Int("level", 1), Xp = Int("xp"), Gold = Int("gold"), Potions = Int("potions", 2), Damage = Int("damage", 25), Health = Float("health", 100), MaxHealth = Float("max_health", 100), Weapon = savedWeapon, OwnedWeapons = Int("owned_weapons", savedWeapon == 1 ? 3 : savedRoom > 0 ? 5 : 1), Armor = Bool("has_armor"), Relic = Bool("has_relic"), HealthTalent = Int("health_talent"), SkillTalent = Int("skill_talent"), DodgeTalent = Int("dodge_talent"), RoomCleared = Bool("room_cleared", true), Completed = Bool("completed") };
    }
    private static bool ValidSaveData(SaveData s) => !(
            !ValidWeaponData(s)
            || !ValidSkillLoadout(s)
            || !ValidSkillVariants(s)
            || s.Room is < 0 or > 42
            || s.FirstEchoQuest is < 0 or > 3
            || s.FirstEchoDiscovered is < 0 or > 1073741823
            || (s.FirstEchoDiscovered & 4096) != 0 && (s.FirstEchoDiscovered & 64) == 0
            || (s.FirstEchoDiscovered & 6291456) == 6291456 && (s.FirstEchoDiscovered & 16777216) == 0
            || (s.FirstEchoDiscovered & 6291456) != 0 && ((s.FirstEchoDiscovered & 1048576) == 0 || (s.FirstEchoDiscovered & 64) == 0)
            || (s.FirstEchoDiscovered & 16777216) != 0 && (s.FirstEchoDiscovered & 8388608) == 0
            || (s.FirstEchoDiscovered & 1040187392) != 0 && ((s.FirstEchoDiscovered & 64) == 0 || (s.FirstEchoDiscovered & 2048) == 0)
            || s.SaltFrontierLocation && ((s.FirstEchoDiscovered & 64) == 0 || (s.FirstEchoDiscovered & 2048) == 0 || (s.FirstEchoDiscovered & 6291456) != 0)
            || s.SaltFrontierBossAwakened && ((s.FirstEchoDiscovered & (31 << 25)) != (31 << 25) || (s.FirstEchoDiscovered & 2048) == 0)
            || s.SaltFrontierBossDefeated && !s.SaltFrontierBossAwakened
            || s.SaltFrontierSideProgress is < 0 or > 11
            || s.SaltFrontierSideProgress > 0 && !s.SaltFrontierBossDefeated
            || s.SaltFrontierSideProgress >= 7 && !s.SaltFrontierWardenDefeated
            || s.SaltFrontierWardenDefeated && (!s.SaltFrontierBossDefeated || s.SaltFrontierSideProgress < 4)
            || s.SaltVaultReliquaryCleared && (s.FirstEchoDiscovered & 16777216) == 0
            || s.Level is < 1 or > 10000
            || s.Xp < 0
            || s.Xp >= s.Level * 60
            || s.Weapon is < 0 or > 5
            || s.OwnedWeapons < 1
            || s.OwnedWeapons > 63
            || (s.OwnedWeapons & 8) != 0 && (!s.UnstruckTrace || s.QuestStage < 58)
            || (s.OwnedWeapons & 16) != 0 && (!s.UnstruckTrace || s.QuestStage < 58)
            || (s.OwnedWeapons & 32) != 0 && s.QuestStage < 55 && (s.Room != 40 || s.QuestStage < 54)
            || (s.OwnedWeapons & (1 << s.Weapon)) == 0
            || s.MaxHealth < 1
            || s.Health <= 0
            || s.Health > s.MaxHealth
            || s.Potions < 0
            || s.Gold < 0
            || s.TideGlass is < 0 or > 999
            || s.EliteWeaponMisses is < 0 or > 2
            || s.DifficultyTier is < 0 or > 3
            || s.ArmorTrait is < 0 or > 4
            || s.RelicTrait is < 0 or > 4
            || s.ArmorTrait > 0 && !s.Armor
            || s.RelicTrait > 0 && !s.Relic
            || s.Damage < 1
            || s.HeroClass is < 0 or > 3
            || s.TechniqueTalent is < 0 or > 10000
            || s.CombatDiscipline is < 0 or > 2
            || s.CombatMastery is < 0 or > 2
            || s.CombatFocus is < 0 or > 2
            || s.CombatApex is < 0 or > 2
            || s.CombatTempo is < 0 or > 2
            || s.CombatInstinct is < 0 or > 2
            || s.CombatCapstone is < 0 or > 2
            || s.CombatParagon is < 0 or > 2
            || s.CombatParagon > 0 && s.Level < 50
            || s.MemoriesFound is < 0 or > 3
            || s.QuestStage is < 0 or > 59
            || s.ClearedRooms < 0
            || s.ClearedRooms > 8796093022207L
            || s.Completed && s.Room != 2
            || s.OwnedCharms is < 0 or > 16383
            || s.EquippedCharm is < 0 or > 14
            || s.EquippedCharm2 is < 0 or > 14
            || s.EquippedCharm3 is < 0 or > 14
            || s.EquippedCharm > 0 && (s.OwnedCharms & (1 << (s.EquippedCharm - 1))) == 0
            || s.EquippedCharm2 > 0 && (s.OwnedCharms & (1 << (s.EquippedCharm2 - 1))) == 0
            || s.EquippedCharm3 > 0 && (s.OwnedCharms & (1 << (s.EquippedCharm3 - 1))) == 0
            || s.EquippedCharm > 0 && s.EquippedCharm == s.EquippedCharm2
            || s.EquippedCharm3 > 0 && (s.EquippedCharm3 == s.EquippedCharm || s.EquippedCharm3 == s.EquippedCharm2)
            || s.EquippedCharm3 > 0 && s.VigilBestFloor < 30
            || s.TideBoots && !s.SaltFrontierCacheFound
            || s.TideBootsOn && !s.TideBoots
            || s.TideBootsAspect is < 0 or > 2
            || s.TownEnding is < 0 or > 2
            || s.QuestStage == 13 && s.TownEnding == 0
            || s.QuestStage >= 14 && !s.ChorusMemory
            || s.QuestStage >= 16 && !s.FirstMemory
            || s.QuestStage >= 19 && !s.WestTrace
            || s.WestTrace && s.QuestStage < 19
            || s.UpstreamTrace && s.QuestStage < 26
            || s.QuestStage >= 26 && !s.UpstreamTrace
            || s.BeyondTrace && s.QuestStage < 29
            || s.QuestStage >= 29 && !s.BeyondTrace
            || s.EbbSeal && s.QuestStage < 33
            || s.QuestStage >= 33 && !s.EbbSeal
            || s.CounterTrace && s.QuestStage < 35
            || s.QuestStage >= 35 && !s.CounterTrace
            || s.AnchorWard && s.QuestStage < 36
            || s.RiftVerse && s.QuestStage < 39
            || s.QuestStage >= 39 && !s.RiftVerse
            || s.RefrainSong && s.QuestStage < 40
            || s.QuestStage >= 40 && !s.RefrainSong
            || s.DeepTrace && s.QuestStage < 42
            || s.QuestStage >= 42 && !s.DeepTrace
            || s.LanternSignal && s.QuestStage < 45
            || s.QuestStage >= 45 && !s.LanternSignal
            || s.BeaconLight && s.QuestStage < 46
            || s.QuestStage >= 46 && !s.BeaconLight
            || s.RouteMemory && s.QuestStage < 48
            || s.QuestStage >= 48 && !s.RouteMemory
            || s.SilenceMemory && s.QuestStage < 51
            || s.QuestStage >= 51 && !s.SilenceMemory
            || s.BorrowedTrace && s.QuestStage < 55
            || s.QuestStage >= 55 && !s.BorrowedTrace
            || s.UnstruckTrace && s.QuestStage < 58
            || s.QuestStage >= 58 && !s.UnstruckTrace
            || s.FinalEnding is < 0 or > 2
            || s.VigilBestFloor is < 0 or > 100000
            || s.VigilBestFloor > 0 && s.FinalEnding == 0
            || s.QuestStage == 52 && s.FinalEnding != 1
            || s.QuestStage == 53 && s.FinalEnding != 2
            || s.FinalEnding > 0 && s.QuestStage < 52
            || s.QuestStage >= 54 && s.FinalEnding == 0
            || s.Room >= 41 && s.QuestStage < 57
            || s.Room >= 39 && s.QuestStage < 54
            || s.Room >= 37 && s.QuestStage < 50
            || s.Room >= 35 && s.QuestStage < 47
            || s.Room >= 33 && s.QuestStage < 44
            || s.Room >= 31 && s.QuestStage < 41
            || s.Room >= 29 && s.QuestStage < 37
            || s.Room >= 27 && s.QuestStage < 34
            || s.Room >= 25 && s.QuestStage < 31
            || s.Room >= 23 && s.QuestStage < 28
            || s.Room >= 21 && s.QuestStage < 25
            || s.Room >= 14 && s.QuestStage < 15
            || s.Room >= 16 && s.QuestStage < 18
            || s.Room >= 18 && s.QuestStage < 21
            || s.Room >= 19 && s.QuestStage < 22
            || s.Room == 20 && s.QuestStage < 22
        );

    private void LoadGame()
    {
        try
        {
            bool migrated = !File.Exists(SavePath); SaveData s = migrated ? ReadLegacySave() : JsonSerializer.Deserialize<SaveData>(File.ReadAllText(SavePath)) ?? throw new InvalidDataException();


            if (!ValidSaveData(s)) throw new InvalidDataException();
            StartGame(); firstEchoQuest = s.FirstEchoQuest; firstEchoCaveFound = s.FirstEchoCaveFound; firstEchoReedCache = s.FirstEchoReedCache; firstEchoDiscovered = s.FirstEchoDiscovered; saltFrontierReturnLocation = s.SaltFrontierLocation; saltFrontierBossAwakened = s.SaltFrontierBossAwakened; saltFrontierBossDefeated = s.SaltFrontierBossDefeated; saltFrontierWardenDefeated = s.SaltFrontierWardenDefeated || s.SaltFrontierSideProgress >= 5; saltFrontierSideProgress = s.SaltFrontierSideProgress; RestoreSaltFrontierCacheGear(s); saltVaultReliquaryCleared = s.SaltVaultReliquaryCleared; heroClass = (HeroClass)s.HeroClass; room = s.Room; level = s.Level; xp = s.Xp; gold = s.Gold; potions = s.Potions; damage = s.Damage; health = s.Health; maxHealth = s.MaxHealth; weapon = s.Weapon; ownedWeapons = s.OwnedWeapons; RestoreWeaponData(s); armor = s.Armor; relic = s.Relic; armorTrait = s.ArmorTrait; relicTrait = s.RelicTrait; healthTalent = s.HealthTalent; skillTalent = s.SkillTalent; dodgeTalent = s.DodgeTalent; techniqueTalent = s.TechniqueTalent; combatDiscipline = CombatDisciplineForSave(s); combatMastery = CombatMasteryForSave(s); combatFocus = s.CombatFocus; combatApex = s.CombatApex; combatTempo = s.CombatTempo; combatInstinct = s.CombatInstinct; combatCapstone = s.CombatCapstone; combatParagon = s.CombatParagon; memoriesFound = s.MemoriesFound; questStage = s.QuestStage; clearedRooms = s.ClearedRooms | (s.RoomCleared && s.Room != 3 ? 1L << s.Room : 0); archiveSeal = s.ArchiveSeal; riverBell = s.RiverBell; lumeLens = s.LumeLens; naraTrace = s.NaraTrace; chorusMemory = s.ChorusMemory; firstMemory = s.FirstMemory; westTrace = s.WestTrace; upstreamTrace = s.UpstreamTrace; tideGlass = s.TideGlass; beyondTrace = s.BeyondTrace; ebbSeal = s.EbbSeal; counterTrace = s.CounterTrace; anchorWard = s.AnchorWard || s.QuestStage >= 36; riftVerse = s.RiftVerse; deepTrace = s.DeepTrace; lanternSignal = s.LanternSignal; beaconLight = s.BeaconLight; routeMemory = s.RouteMemory; silenceMemory = s.SilenceMemory; borrowedTrace = s.BorrowedTrace; unstruckTrace = s.UnstruckTrace; finalEnding = s.FinalEnding; difficultyTier = s.DifficultyTier; vigilBestFloor = s.VigilBestFloor; highContrastTelegraphs = s.HighContrastTelegraphs; reducedScreenShake = s.ReducedScreenShake; eliteWeaponMisses = s.EliteWeaponMisses; refrainSong = s.RefrainSong; ownedCharms = s.OwnedCharms; equippedCharm = s.EquippedCharm; equippedCharm2 = s.EquippedCharm2; equippedCharm3 = s.EquippedCharm3; RestoreSkillLoadouts(s); Array.Copy(s.SkillVariants, skillVariants, Math.Min(s.SkillVariants.Length, skillVariants.Length)); townEnding = s.TownEnding; SetupRoom(); if (s.RoomCleared) enemies.Clear(); victory = s.Completed; title = false; storyOverlay = false;
            if (migrated && !SaveGame()) { notice = "SAVE ANTIGO CARREGADO • NÃO FOI POSSÍVEL MIGRAR"; noticeTime = 5; }
            else { notice = migrated ? "SAVE ANTIGO MIGRADO" : "PROGRESSO CARREGADO"; noticeTime = 2; }
        }
        catch { notice = "NENHUM SALVAMENTO VÁLIDO"; noticeTime = 2; }
    }
}
