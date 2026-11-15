using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

public sealed partial class Game
{
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
        if (ReferenceEquals(lockedTarget, enemy)) lockedTarget = enemies.Where(TargetVisible).OrderBy(candidate => Distance(player, candidate.Pos)).FirstOrDefault();
        TriggerNameRiftBurst(enemy);
        if (enemy.EliteMinion) return;
        enemiesDefeated++; int goldDrop = enemy.VigilMinion ? 8 + Math.Min(20, vigilFloor * 2) : vigilActive ? 35 + Math.Min(500, vigilFloor * 15) : enemy.Boss ? 35 : enemy.Kind == EnemyKind.Elite ? 20 : 8;
        int xpReward = enemy.VigilMinion ? 12 : enemy.Boss ? 75 : enemy.Kind == EnemyKind.Elite ? 40 : 22;
        if (!vigilActive) { goldDrop = (int)Math.Round(goldDrop * DifficultyRewardMultiplier(difficultyTier)); xpReward = (int)Math.Round(xpReward * DifficultyRewardMultiplier(difficultyTier)); }
        drops.Add(new Drop { Pos = enemy.Pos, Kind = "gold", Amount = goldDrop }); AddXp(xpReward);
        if (enemy.Kind == EnemyKind.Elite)
        {
            drops.Add(new Drop { Pos = Offset(enemy.Pos, 24, -20), Kind = "glass", Amount = Math.Clamp(1 + (room >= 43 ? 4 : room) / 12, 1, 4) });
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
        if (room == 2 && enemy.Kind == EnemyKind.Guardian && !relic) drops.Add(new Drop { Pos = Offset(enemy.Pos, -24, 0), Kind = "relic" });
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
}
