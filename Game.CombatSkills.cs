using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

public sealed partial class Game
{
    private void Attack()
    {
        if (attackCd > 0) return;
        facing = lockedTarget is not null && enemies.Contains(lockedTarget) ? Normalize(Sub(lockedTarget.Pos, player), facing) : mouseAim;
        attackCd = BasicAttackCooldown();
        attackFlash = 0.16f;
        GameAudio.Play(GameAudio.Cue.Strike);
        bool cadenceTriggered = false, landed = false, landedCritical = false;
        foreach (Enemy enemy in enemies.ToArray())
        {
            PointF offset = Sub(enemy.Pos, player); float distance = Length(offset);
            if (distance > BasicAttackReach(enemy) || (distance >= 20 && !BasicAttackCoversDirection(Dot(facing, Normalize(offset, facing)), weapon)) || BlockedLine(player, enemy.Pos)) continue;
            landed = true;
            bool rangedCriticalResonance = UsesRangedCriticalResonance(enemy); bool critical = random.NextDouble() < BasicAttackCriticalChance(enemy); int dealt = (int)(damage * HeroBasicAttackMultiplier() * BasicAttackSpecializationMultiplier() * BasicAttackDamageMultiplier(enemy) * OutgoingDamageMultiplier(enemy) * (critical ? CriticalMultiplier() : 1));
            landedCritical |= critical;
            DamageEnemy(enemy, dealt, critical); enemy.Flash = 0.2f;
            ApplyWeaponWound(enemy, critical);
            if (critical && rangedCriticalResonance) AddFloatingText(new PointF(enemy.Pos.X, enemy.Pos.Y - enemy.Radius - 28), "RESSONÂNCIA DO CONTRACANTO", Color.LightCyan);
            if (critical && !cadenceTriggered && HasWeaponAffix(weapon, 7)) { cadenceTriggered = true; float reduction = WeaponCriticalCooldownAdvance(weapon); AdvanceTechniqueCooldowns(reduction); AddFloatingText(new PointF(player.X, player.Y - 38), $"CADÊNCIA −{reduction:0.00} S", Color.LightCyan); }
            if (weapon == 1 && !enemy.Boss) { enemy.Stagger = Math.Max(enemy.Stagger, 0.2f); enemy.Windup = 0; }
            if (enemy.Hp <= 0) Defeat(enemy);
        }
        if (landed) TriggerHitStop(landedCritical ? 0.075f : 0.045f);
        if (landed && heroClass == HeroClass.StoneGuardian && stoneFury >= 100) ReleaseStoneFury();
        else if (landed && heroClass == HeroClass.NameWeaver) { nameThread = Math.Min(100, nameThread + 18); AddFloatingText(new PointF(player.X, player.Y - 34), "FIO +18", Color.FromArgb(235, 211, 139, 234)); }
    }

    private float HeroBasicAttackMultiplier() => heroClass == HeroClass.StoneGuardian ? 1.2f : heroClass == HeroClass.NameWeaver ? 0.9f : 1f;
    private float HeroDamageTakenMultiplier() => heroClass == HeroClass.StoneGuardian ? 0.8f : heroClass == HeroClass.NameWeaver ? 1.15f : 1f;
    private float ClassMovementSpeed() => heroClass == HeroClass.StoneGuardian ? 180f : PlayerMoveSpeed;
    private string HeroClassTitle() => heroClass switch { HeroClass.StoneGuardian => "GUARDIÃO DA PEDRA", HeroClass.NameWeaver => "TECELÃ DE NOMES", _ => "ESCUTADOR" };

    private void ReleaseStoneFury()
    {
        stoneFury = 0;
        AddFloatingText(new PointF(player.X, player.Y - 44), "FÚRIA DE PEDRA", Color.FromArgb(255, 203, 116));
        GameAudio.Play(GameAudio.Cue.Impact);
        foreach (Enemy enemy in enemies.ToArray())
        {
            if (Distance(player, enemy.Pos) > 112) continue;
            DamageEnemy(enemy, damage * (enemy.Boss ? 0.55f : 0.9f) * OutgoingDamageMultiplier(enemy) * (IsArmored(enemy) ? 0.75f : 1f));
            enemy.Flash = 0.22f;
            if (!enemy.Boss) { enemy.Windup = 0; enemy.Stagger = Math.Max(enemy.Stagger, 0.85f); enemy.Cooldown = Math.Max(enemy.Cooldown, 1.1f); }
            if (enemy.Hp <= 0) Defeat(enemy);
        }
    }

    private void AdvanceTechniqueCooldowns(float seconds)
    {
        skillCd = Math.Max(0, skillCd - seconds); shardCd = Math.Max(0, shardCd - seconds); bellCd = Math.Max(0, bellCd - seconds); pullCd = Math.Max(0, pullCd - seconds); wardCd = Math.Max(0, wardCd - seconds); refrainCd = Math.Max(0, refrainCd - seconds); beaconCd = Math.Max(0, beaconCd - seconds); returnVoiceCd = Math.Max(0, returnVoiceCd - seconds); finalStrikeCd = Math.Max(0, finalStrikeCd - seconds); stoneBastionCd = Math.Max(0, stoneBastionCd - seconds); nameWeaverCd = Math.Max(0, nameWeaverCd - seconds);
    }

    private void DelayTechniqueCooldowns(float seconds)
    {
        if (skillCd > 0) skillCd += seconds; if (shardCd > 0) shardCd += seconds; if (bellCd > 0) bellCd += seconds; if (pullCd > 0) pullCd += seconds; if (wardCd > 0) wardCd += seconds; if (refrainCd > 0) refrainCd += seconds; if (beaconCd > 0) beaconCd += seconds; if (returnVoiceCd > 0) returnVoiceCd += seconds; if (finalStrikeCd > 0) finalStrikeCd += seconds; if (stoneBastionCd > 0) stoneBastionCd += seconds; if (nameWeaverCd > 0) nameWeaverCd += seconds;
    }

    private void Whirlwind()
    {
        if (skillCd > 0 || talentChoice) return;
        skillCd = (Math.Max(1.5f, 6f - skillTalent * 0.5f) + WhirlwindCooldownPenalty()) * WeaponCooldownMultiplier();
        foreach (Enemy enemy in enemies.ToArray())
        {
            if (Distance(player, enemy.Pos) >= 110) continue;
            int dealt = (int)(damage * OutgoingDamageMultiplier(enemy) * WhirlwindMultiplier() * WeaponSkillMultiplier() * TechniqueDamageMultiplier() * (IsArmored(enemy) ? 0.75f : 1));
            DamageEnemy(enemy, dealt);
            if (enemy.Hp <= 0) Defeat(enemy);
        }
    }

    private void CastStoneBastion()
    {
        if (heroClass != HeroClass.StoneGuardian || stoneBastionCd > 0) return;
        if (stoneFury < 50)
        {
            notice = "BASTIÃO DE BASALTO • PRECISA DE 50 DE FÚRIA"; noticeTime = 1.6f; return;
        }
        stoneFury -= 50;
        stoneBastionTime = 3.5f;
        stoneBastionCd = 18f * WeaponCooldownMultiplier();
        notice = "BASTIÃO DE BASALTO • PEDRA LEMBRA"; noticeTime = 1.4f;
        AddFloatingText(new PointF(player.X, player.Y - 44), "BASTIÃO DE BASALTO", Color.FromArgb(255, 255, 218, 139));
        GameAudio.Play(GameAudio.Cue.Impact);
    }

    private void CastVoiceShard()
    {
        if (room == 3 || shardCd > 0 || enemies.Count == 0) return;
        facing = lockedTarget is not null && enemies.Contains(lockedTarget) ? Normalize(Sub(lockedTarget.Pos, player), facing) : Normalize(mouseAim, facing);
        shardCd = VoiceShardCooldown();
        echoShards.Add(new EchoShard { Pos = Add(player, Scale(facing, 23)), Direction = facing, Life = VoiceShardLifetime(), Damage = VoiceShardDamage() });
        notice = "ESTILHAÇO DE VOZ"; noticeTime = 0.75f;
    }

    private bool SkillUnlocked(int skill) => skill switch { 1 => true, 2 => riverBell, 3 => ebbSeal, 4 => anchorWard, 5 => refrainSong, 6 => beaconLight, 7 => finalEnding == 1, 8 => finalEnding == 2, 9 => heroClass == HeroClass.StoneGuardian, 10 => heroClass == HeroClass.NameWeaver, _ => false };
    private int[] SkillMenuLoadout() => skillMenuAlternate == alternateSkillsActive ? equippedSkills : alternateSkills;
    private void ToggleSkillLoadout()
    {
        (equippedSkills, alternateSkills) = (alternateSkills, equippedSkills); alternateSkillsActive = !alternateSkillsActive;
        notice = alternateSkillsActive ? "MONTAGEM ALTERNATIVA ATIVA • RECARGAS PRESERVADAS" : "MONTAGEM PRINCIPAL ATIVA • RECARGAS PRESERVADAS"; noticeTime = 1.6f;
    }
    private static bool HasSkillPair(int[] loadout, int first, int second) => loadout.Contains(first) && loadout.Contains(second);
    private bool EbbBeaconSynergyActive() => equippedSkills.Contains(3) && equippedSkills.Contains(6);
    private bool BellShardSynergyActive() => equippedSkills.Contains(1) && equippedSkills.Contains(2);
    private bool BellRefrainSynergyActive() => equippedSkills.Contains(2) && equippedSkills.Contains(5);
    private bool WardReturnSynergyEquipped() => equippedSkills.Contains(4) && equippedSkills.Contains(7);
    private bool WardReturnSynergyActive() => WardReturnSynergyEquipped() && wardTime > 0;
    private void CycleSkillVariant(int slot, int direction)
    {
        int skill = SkillMenuLoadout()[slot];
        if (skill == 0 || !SkillUnlocked(skill)) { notice = "EQUIPE UMA TÉCNICA APRENDIDA PARA ESCOLHER SUA MEMÓRIA"; noticeTime = 2; return; }
        if (skill >= 9) { notice = skill == 9 ? "O BASTIÃO DE BASALTO NÃO POSSUI VARIAÇÕES" : "A SUTURA DO NOME NÃO POSSUI VARIAÇÕES"; noticeTime = 1.5f; return; }
        skillVariants[skill] = (skillVariants[skill] + direction + 3) % 3;
        notice = $"{skillNames[skill]} • {SkillVariantName(skill, skillVariants[skill])}"; noticeTime = 2;
    }
    private static string SkillVariantName(int skill, int variant) => (skill, variant) switch
    {
        (1, 1) => "Fio Fraturado", (1, 2) => "Fio Distante",
        (2, 1) => "Badalada Ampla", (2, 2) => "Nota Concentrada",
        (3, 1) => "Vazante Espalhada", (3, 2) => "Nó de Tração",
        (4, 1) => "Manto Persistente", (4, 2) => "Manto Reforçado",
        (5, 1) => "Coro Crescente", (5, 2) => "Coro Focado",
        (6, 1) => "Farol Duradouro", (6, 2) => "Pulsos Próximos",
        (7, 1) => "Partilha", (7, 2) => "Voz Afiada",
        (8, 1) => "Centelha Fatal", (8, 2) => "Centelha Breve",
        _ => "Forma Original"
    };
    private static string SkillVariantDescription(int skill, int variant) => (skill, variant) switch
    {
        (1, 1) => "+25% dano • recarga +20%", (1, 2) => "+35% alcance • dano -15% • recarga +10%",
        (2, 1) => "+25% raio • dano -15%", (2, 2) => "+25% dano • raio -15% • recarga +15%",
        (3, 1) => "+25% raio de atração • dano -15%", (3, 2) => "+25% força de atração • dano +15%",
        (4, 1) => "+40% duração • mitigação cai de 55% para 45%", (4, 2) => "-30% duração • mitigação sobe de 55% para 68%",
        (5, 1) => "+25% raio • dano -15%", (5, 2) => "+25% dano • raio -15%",
        (6, 1) => "+2 s de duração • pulso causa -15% dano", (6, 2) => "pulsos 25% mais rápidos • duração -20%",
        (7, 1) => "+35% cura • dano -20%", (7, 2) => "+25% dano • cura -30%",
        (8, 1) => "+30% dano • recarga +25%", (8, 2) => "-15% dano • recarga -25%",
        _ => "sem modificadores; a forma mais equilibrada"
    };
    private static bool ValidSkillVariants(SaveData s)
    {
        if (s.SkillVariants is null || s.SkillVariants.Length != 9 || s.SkillVariants[0] != 0 || s.SkillVariants.Any(variant => variant is < 0 or > 2)) return false;
        return Enumerable.Range(1, 8).All(skill => s.SkillVariants[skill] == 0 || skill switch { 1 => true, 2 => s.RiverBell, 3 => s.EbbSeal, 4 => s.AnchorWard || s.QuestStage >= 36, 5 => s.RefrainSong, 6 => s.BeaconLight, 7 => s.FinalEnding == 1, 8 => s.FinalEnding == 2, _ => false });
    }
    private void EquipSkill(int slot, int skill)
    {
        if (!SkillUnlocked(skill)) { notice = "ESSA TÉCNICA AINDA NÃO FOI APRENDIDA"; noticeTime = 1.5f; return; }
        int[] loadout = SkillMenuLoadout();
        if (loadout.Where((_, index) => index != slot).Contains(skill)) { notice = "CADA TÉCNICA SÓ PODE OCUPAR UM ESPAÇO"; noticeTime = 1.5f; return; }
        loadout[slot] = skill; notice = $"{(skillMenuAlternate ? "ALTERNATIVA" : "PRINCIPAL")} • ESPAÇO {slot + 1}: {skillNames[skill]}"; noticeTime = 1.5f;
    }
    private void CastEquippedSkill(int slot)
    {
        int skill = equippedSkills[slot];
        if (skill == 0) { notice = $"ESPAÇO {slot + 1} VAZIO • PRESSIONE B PARA MONTAR TÉCNICAS"; noticeTime = 1.5f; return; }
        if (!SkillUnlocked(skill)) { notice = "TÉCNICA AINDA NÃO APRENDIDA"; noticeTime = 1.5f; return; }
        GameAudio.Play(GameAudio.Cue.Skill);
        switch (skill) { case 1: CastVoiceShard(); break; case 2: CastBellPulse(); break; case 3: CastEbbPull(); break; case 4: CastAnchorWard(); break; case 5: CastRefrain(); break; case 6: CastBeacon(); break; case 7: CastReturnedVoices(); break; case 8: CastFinalSpark(); break; case 9: CastStoneBastion(); break; case 10: CastNameSuture(); break; }
    }
    private void CastNameSuture()
    {
        if (nameWeaverCd > 0 || nameThread < 40) { notice = nameWeaverCd > 0 ? "SUTURA DO NOME EM RECARGA" : "FIO INSUFICIENTE • ACERTE INIMIGOS"; noticeTime = 1.4f; return; }
        Enemy[] targets = enemies.Where(e => Distance(e.Pos, player) <= 210).ToArray();
        if (targets.Length == 0) { notice = "NENHUM ECO AO ALCANCE DA SUTURA"; noticeTime = 1.4f; return; }
        nameThread -= 40; nameWeaverCd = 14f;
        foreach (Enemy target in targets) target.NameWeaverMarkTime = 5f;
        notice = $"SUTURA DO NOME • {targets.Length} ECO(S) VULNERÁVEL(IS)"; noticeTime = 1.8f;
    }
    private static bool ValidSkillLoadout(SaveData s)
    {
        return ValidSkillArray(s.EquippedSkills, s) && ValidSkillArray(s.AlternateSkills, s);
    }
    private static bool ValidSkillArray(int[]? skills, SaveData s) => skills is not null && (skills.Length is 3 or 4) && !skills.Any(skill => skill is < 0 or > 10) && skills.Where(skill => skill != 0).Distinct().Count() == skills.Count(skill => skill != 0) && skills.All(skill => skill == 0 || skill switch { 1 => true, 2 => s.RiverBell, 3 => s.EbbSeal, 4 => s.AnchorWard || s.QuestStage >= 36, 5 => s.RefrainSong, 6 => s.BeaconLight, 7 => s.FinalEnding == 1, 8 => s.FinalEnding == 2, 9 => s.HeroClass == (int)HeroClass.StoneGuardian, 10 => s.HeroClass == (int)HeroClass.NameWeaver, _ => false });
    private static int[] NormalizeSkillLoadout(int[] skills) => skills.Length == 3 ? skills.Append(0).ToArray() : skills.ToArray();
    private void RestoreSkillLoadouts(SaveData s)
    {
        equippedSkills = NormalizeSkillLoadout(s.EquippedSkills); alternateSkills = NormalizeSkillLoadout(s.AlternateSkills); alternateSkillsActive = s.AlternateSkillsActive;
        if (alternateSkillsActive) (equippedSkills, alternateSkills) = (alternateSkills, equippedSkills);
    }

    private float SkillDamageVariant(int skill) => (skill, skillVariants[skill]) switch { (1, 1) => 1.25f, (1, 2) => 0.85f, (2, 1) => 0.85f, (2, 2) => 1.25f, (3, 1) => 0.85f, (3, 2) => 1.15f, (5, 1) => 0.85f, (5, 2) => 1.25f, (6, 1) => 0.85f, (7, 1) => 0.8f, (7, 2) => 1.25f, (8, 1) => 1.3f, (8, 2) => 0.85f, _ => 1f };
    private float BellPulseRadius() => (HasCharm(2) ? 190f : 155f) * (skillVariants[2] == 1 ? 1.25f : skillVariants[2] == 2 ? 0.85f : 1f);
    private float BellPulseCooldown() => (Math.Max(7f, 14f - skillTalent * 0.7f) + (HasCharm(2) ? 1.5f : 0f)) * (skillVariants[2] == 2 ? 1.15f : 1f) * WeaponCooldownMultiplier();
    private float EbbPullRadius() => 160f * (skillVariants[3] == 1 ? 1.25f : 1f);
    private float WeaponSkillMultiplier() => HasWeaponAffix(weapon, 3) ? weaponRarities[weapon] == 2 ? 1.18f : 1.12f : 1f;
    private float EquippedSkillCooldown(int skill) => skill switch { 1 => shardCd, 2 => bellCd, 3 => pullCd, 4 => wardCd, 5 => refrainCd, 6 => beaconCd, 7 => returnVoiceCd, 8 => finalStrikeCd, 9 => stoneBastionCd, 10 => nameWeaverCd, _ => 0 };
    private int TechniquesOnCooldown() => equippedSkills.Count(skill => skill > 0 && EquippedSkillCooldown(skill) > 0);
    private int TechniquesReady() => equippedSkills.Count(skill => skill > 0 && EquippedSkillCooldown(skill) <= 0);
    private float TechniqueDamageMultiplier() => (heroClass == HeroClass.NameWeaver ? 1.18f : 1f) * (1f + techniqueTalent * 0.08f) * (combatDiscipline == 2 ? 1.12f : combatDiscipline == 1 ? 0.9f : 1f) * (combatApex == 1 && TechniquesOnCooldown() >= 2 ? 1.18f : 1f) * (vigilActive && HasCharm(8) ? 1.18f : 1f) * (HasCharm(11) && TechniquesReady() >= 2 ? 1.12f : 1f) * (HasCharm(12) ? 1.2f : 1f);
    private float BasicAttackCycleMultiplier() => HasCharm(11) && TechniquesOnCooldown() >= 2 ? 1.12f : 1f;
    private float BasicAttackSpecializationMultiplier() => combatDiscipline == 1 ? 1.12f : combatDiscipline == 2 ? 0.9f : 1f;
    private int WeaponPower(int index) => weaponDamage[index] + weaponBonuses[index];
    private int WeaponKillHealing()
    {
        if (!HasWeaponAffix(weapon, 1)) return 0;
        int healing = weaponRarities[weapon] == 2 ? 6 : 3;
        if (weaponRarities[weapon] == 2 && health <= maxHealth * 0.5f && HasWeaponAffix(weapon, 5)) healing += 6;
        return healing;
    }
    private static int CombatDisciplineForSave(SaveData data) => data.CombatDiscipline ?? (data.Level >= 5 ? 1 : 0);
    private static int CombatMasteryForSave(SaveData data) => data.CombatMastery ?? (data.Level >= 10 ? 1 : 0);
    private static string WeaponRarityName(int index) => index == 0 ? "COMUM" : index == 1 ? "RARA" : "RELÍQUIA";
    private bool HasWeaponAffix(int index, int affix) => weaponAffixes[index] == affix || weaponAffixes2[index] == affix;
    private bool HasAnchorWardResonance(int index) => weaponRarities[index] == 2 && HasWeaponAffix(index, 4) && HasWeaponAffix(index, 5);
    private bool HasRangedCriticalResonance(int index) => weaponRarities[index] == 2 && HasWeaponAffix(index, 2) && HasWeaponAffix(index, 6);
    private bool HasWoundResonance(int index) => weaponRarities[index] == 2 && HasWeaponAffix(index, 2) && HasWeaponAffix(index, 8);
    private bool HasNameRiftPower(int index) => weaponRarities[index] == 2 && weaponAffixes2[index] == 9;
    private static bool IsMarkedForRift(Enemy enemy) => enemy.BeaconMarkTime > 0 || enemy.BellMarkTime > 0 || enemy.NameWeaverMarkTime > 0;
    private bool UsesRangedCriticalResonance(Enemy enemy) => HasRangedCriticalResonance(weapon) && Distance(player, enemy.Pos) >= BasicAttackReach(enemy) * 0.7f;
    private float BasicAttackCriticalChance(Enemy enemy) => CriticalChance() + (UsesRangedCriticalResonance(enemy) ? 0.1f : 0f);
    private float AnchorWardDuration() => Math.Max(1f, (2.5f + (HasAnchorWardResonance(weapon) ? 0.75f : 0f)) * (skillVariants[4] == 1 ? 1.4f : skillVariants[4] == 2 ? 0.7f : 1f));
    private float AnchorWardDamageTakenMultiplier() => skillVariants[4] == 1 ? 0.55f : skillVariants[4] == 2 ? 0.32f : 0.45f;
    private float WeaponCriticalCooldownAdvance(int index) => !HasWeaponAffix(index, 7) ? 0 : (weaponRarities[index] == 2 ? 0.25f : 0.15f) + (HasWeaponAffix(index, 3) && HasWeaponAffix(index, 7) ? 0.1f : 0f);
    private string WeaponAffixName(int index) => string.Join(" + ", new[] { WeaponAffixLabel(weaponAffixes[index]), WeaponAffixLabel(weaponAffixes2[index]) }.Where(label => label.Length > 0));
    private static string WeaponAffixLabel(int affix) => affix switch { 1 => "Trégua", 2 => "Contracanto", 3 => "Maré", 4 => "Passo Célere", 5 => "Lastro", 6 => "Fio Esticado", 7 => "Cadência", 8 => "Ruptura", 9 => "Nome Riscado", _ => "" };
    private string WeaponDisplayName(int index) => weaponRarities[index] == 0 ? $"{weaponNames[index]} [COMUM]" : $"{weaponNames[index]} • {WeaponRarityName(index)} {WeaponAffixName(index)}";
    private string WeaponAffixDescription(int index) => string.Join(" • ", new[] { WeaponAffixDescription(weaponAffixes[index], weaponRarities[index]), WeaponAffixDescription(weaponAffixes2[index], weaponRarities[index]), weaponRarities[index] == 2 && HasWeaponAffix(index, 3) && HasWeaponAffix(index, 7) ? "Ressonância da Maré: Cadência adianta +0,10 s por crítico" : "", weaponRarities[index] == 2 && HasWeaponAffix(index, 1) && HasWeaponAffix(index, 5) ? "Ressonância da Trégua: abaixo de metade da vida, abates curam +6" : "", HasAnchorWardResonance(index) ? "Ressonância do Lastro: Manto de Lastro dura +0,75 s" : "", HasRangedCriticalResonance(index) ? "Ressonância do Contracanto: +10 p.p. de crítico no alcance longo" : "", HasWoundResonance(index) ? "Ressonância da Cicatriz: Ruptura causa +40% de dano" : "", HasNameRiftPower(index) ? "Poder único • Nome Riscado: ecos marcados explodem ao morrer" : "" }.Where(description => description.Length > 0));
    private static string WeaponAffixDescription(int affix, int rarity) => affix switch { 1 => $"da Trégua: recupera {(rarity == 2 ? 6 : 3)} vida por abate", 2 => $"do Contracanto: +{(rarity == 2 ? 11 : 7)} p.p. de crítico", 3 => $"da Maré: +{(rarity == 2 ? 18 : 12)}% de dano de técnica", 4 => $"do Passo Célere: recarga de técnicas -{(rarity == 2 ? 16 : 10)}%", 5 => $"do Lastro: dano recebido -{(rarity == 2 ? 12 : 8)}%", 6 => $"do Fio Esticado: alcance básico +{(rarity == 2 ? 20 : 12)}%", 7 => $"da Cadência: crítico adianta recargas em {(rarity == 2 ? 0.25f : 0.15f):0.00} s por golpe", 8 => $"da Ruptura: críticos ferem por 3 s ({(rarity == 2 ? 8 : 6)}% do dano da arma/s)", 9 when rarity == 2 => "do Nome Riscado: ecos marcados explodem ao morrer", _ => "" };

    private void CastBellPulse()
    {
        if (!riverBell) { notice = "O SINO DE NARA AINDA NÃO FOI ENCONTRADO"; noticeTime = 1.6f; return; }
        if (room is 3 or 18 || enemies.Count == 0 || bellCd > 0) return;
        bellCd = BellPulseCooldown(); bellWaveTime = 0.42f; bellWaveRadius = BellPulseRadius();
        foreach (Enemy enemy in enemies.ToArray())
        {
            if (Distance(player, enemy.Pos) > bellWaveRadius) continue;
            float multiplier = SkillDamageVariant(2) * (HasCharm(3) ? 1.25f : 1f) * WeaponSkillMultiplier() * TechniqueDamageMultiplier();
            float power = enemy.Boss ? 0.55f : 0.95f;
            DamageEnemy(enemy, (int)(damage * power * multiplier * OutgoingDamageMultiplier(enemy) * (IsArmored(enemy) ? 0.75f : 1)));
            enemy.Flash = 0.2f;
            if ((BellShardSynergyActive() || BellRefrainSynergyActive()) && enemy.Hp > 0) enemy.BellMarkTime = 5f;
            if (!enemy.Boss) { enemy.Stagger = 0.95f; enemy.Windup = 0; enemy.Cooldown = Math.Max(enemy.Cooldown, 1.8f); }
            if (enemy.Hp <= 0) Defeat(enemy);
        }
        notice = BellShardSynergyActive() ? "SINO DE NARA • ECOS PREPARADOS PARA O ESTILHAÇO" : BellRefrainSynergyActive() ? "SINO DE NARA • ECOS PREPARADOS PARA O CORO" : HasCharm(2) ? "SINO DE NARA • ONDA AMPLIADA" : "SINO DE NARA • RESSONÂNCIA"; noticeTime = 1.2f;
    }

    private float EbbPullCooldown() => Math.Max(8f, 12f - skillTalent * 0.5f) * WeaponCooldownMultiplier();
    private void CastEbbPull()
    {
        if (!ebbSeal) { notice = "A VAZANTE AINDA NÃO FOI APRENDIDA"; noticeTime = 1.5f; return; }
        if (room is 3 or 18 || enemies.Count == 0 || pullCd > 0) return;
        pullCd = EbbPullCooldown(); pullWaveTime = 0.45f;
        float techniqueBoost = (HasCharm(3) ? 1.25f : 1f) * WeaponSkillMultiplier() * TechniqueDamageMultiplier();
        float pullRadius = EbbPullRadius(), pullStrength = skillVariants[3] == 2 ? 1.25f : 1f;
        foreach (Enemy enemy in enemies.ToArray())
        {
            float distance = Distance(player, enemy.Pos); if (distance > pullRadius) continue;
            if (EbbBeaconSynergyActive()) enemy.BeaconMarkTime = 5f;
            DamageEnemy(enemy, damage * (enemy.Boss ? 0.3f : 0.48f) * techniqueBoost * SkillDamageVariant(3) * OutgoingDamageMultiplier(enemy) * (IsArmored(enemy) ? 0.75f : 1f));
            enemy.Flash = 0.22f;
            if (enemy.Boss) enemy.Pos = MoveEnemy(enemy, Scale(Normalize(Sub(player, enemy.Pos), PointF.Empty), 22 * pullStrength));
            else { enemy.Pos = MoveEnemy(enemy, Scale(Normalize(Sub(player, enemy.Pos), PointF.Empty), Math.Min(85, Math.Max(0, distance - 36)) * pullStrength)); enemy.Windup = 0; enemy.Stagger = 0.55f; enemy.Cooldown = Math.Max(enemy.Cooldown, 1.2f); }
            if (enemy.Hp <= 0) Defeat(enemy);
        }
        notice = EbbBeaconSynergyActive() ? "VAZANTE • ECOS PREPARADOS PARA O FAROL" : "VAZANTE • ECOS REUNIDOS"; noticeTime = 1.2f;
    }

    private float AnchorWardCooldown() => Math.Max(13f, 20f - skillTalent * 0.8f) * WeaponCooldownMultiplier();
    private void CastAnchorWard()
    {
        if (!anchorWard) { notice = "O MANTO DE LASTRO AINDA NÃO FOI APRENDIDO"; noticeTime = 1.5f; return; }
        if (room is 3 or 18 || enemies.Count == 0 || wardCd > 0) return;
        wardTime = AnchorWardDuration(); wardCd = AnchorWardCooldown();
        notice = WardReturnSynergyEquipped() ? "MANTO DE LASTRO • A DEVOLUÇÃO PODE CURAR +35%" : "MANTO DE LASTRO • PROTEÇÃO ATIVA"; noticeTime = 1.2f;
    }

    private float RefrainCooldown() => Math.Max(10f, 17f - skillTalent * 0.6f) * WeaponCooldownMultiplier();
    private void CastRefrain()
    {
        if (!refrainSong) { notice = "O CORO DE RETORNO AINDA NÃO FOI APRENDIDO"; noticeTime = 1.5f; return; }
        if (room is 3 or 18 || enemies.Count == 0 || refrainCd > 0) return;
        refrainCd = RefrainCooldown(); refrainWaveTime = 0.48f; refrainWaveRadius = 170 * (skillVariants[5] == 1 ? 1.25f : skillVariants[5] == 2 ? 0.85f : 1f);
        float boost = TechniqueDamageMultiplier() * WeaponSkillMultiplier();
        foreach (Enemy enemy in enemies.ToArray())
        {
            if (Distance(player, enemy.Pos) > refrainWaveRadius) continue;
            bool bellResonantTarget = BellRefrainSynergyActive() && enemy.BellMarkTime > 0;
            DamageEnemy(enemy, damage * (enemy.Boss ? 0.28f : 0.68f) * boost * SkillDamageVariant(5) * (bellResonantTarget ? 1.35f : 1f) * OutgoingDamageMultiplier(enemy) * (IsArmored(enemy) ? 0.75f : 1f));
            if (bellResonantTarget) { enemy.BellMarkTime = 0; AddFloatingText(new PointF(enemy.Pos.X, enemy.Pos.Y - enemy.Radius - 26), "SINO + CORO", Color.LightGoldenrodYellow); }
            enemy.Flash = 0.24f;
            if (!enemy.Boss) { enemy.Stagger = Math.Max(enemy.Stagger, 0.38f); enemy.Windup = 0; enemy.Cooldown = Math.Max(enemy.Cooldown, 0.8f); }
            if (enemy.Hp <= 0) Defeat(enemy);
        }
        notice = BellRefrainSynergyActive() ? "CORO DE RETORNO • MARCAS DO SINO CONSUMIDAS" : "CORO DE RETORNO • ECO REBATIDO"; noticeTime = 1.2f;
    }

    private float BeaconCooldown() => Math.Max(13f, 22f - skillTalent * 0.9f) * WeaponCooldownMultiplier();
    private float BeaconTotalCooldown() => BeaconCooldown() + (HasCharm(6) ? 3.5f : 0f);
    private void CastBeacon()
    {
        if (!beaconLight) { notice = "O FAROL DE VIGÍLIA AINDA NÃO FOI APRENDIDO"; noticeTime = 1.5f; return; }
        if (room is 3 or 18 || enemies.Count == 0 || beaconCd > 0) return;
        beaconPos = player; float beaconDuration = HasCharm(6) ? 8f : 6f; beaconTime = skillVariants[6] == 1 ? beaconDuration + 2f : skillVariants[6] == 2 ? beaconDuration * 0.8f : beaconDuration; beaconPulseCd = 0.15f; beaconWaveTime = 0;
        beaconCd = BeaconTotalCooldown(); notice = HasCharm(6) ? "FAROL DE VIGÍLIA • PAVIO REFORÇADO" : "FAROL DE VIGÍLIA • LUZ PORTÁTIL ATIVA"; noticeTime = 1.6f;
    }
    private void CastReturnedVoices()
    {
        if (finalEnding != 1) { notice = "A DEVOLUÇÃO DAS VOZES AINDA NÃO FOI APRENDIDA"; noticeTime = 1.5f; return; }
        if (room is 3 or 18 || enemies.Count == 0 || returnVoiceCd > 0) return;
        returnVoiceCd = Math.Max(14f, 23f - skillTalent * 0.7f) * WeaponCooldownMultiplier(); returnVoiceWaveTime = 0.46f; returnVoiceWaveRadius = 188;
        float boost = TechniqueDamageMultiplier() * WeaponSkillMultiplier() * SkillDamageVariant(7); float restored = 0;
        foreach (Enemy enemy in enemies.ToArray())
        {
            if (Distance(player, enemy.Pos) > returnVoiceWaveRadius) continue;
            DamageEnemy(enemy, damage * (enemy.Boss ? 0.34f : 0.52f) * boost * OutgoingDamageMultiplier(enemy) * (IsArmored(enemy) ? 0.75f : 1f));
            enemy.Flash = 0.2f;
            if (!enemy.Boss) { enemy.Windup = 0; enemy.Stagger = Math.Max(enemy.Stagger, 0.32f); enemy.Cooldown = Math.Max(enemy.Cooldown, 0.8f); }
            restored += maxHealth * (enemy.Boss ? 0.025f : 0.045f) * (skillVariants[7] == 1 ? 1.35f : skillVariants[7] == 2 ? 0.7f : 1f) * (WardReturnSynergyActive() ? 1.35f : 1f);
            if (enemy.Hp <= 0) Defeat(enemy);
        }
        health = Math.Min(maxHealth, health + Math.Min(maxHealth * 0.24f, restored)); notice = WardReturnSynergyActive() ? "DEVOLUÇÃO DAS VOZES • MANTO AMPLIOU A CURA" : "DEVOLUÇÃO DAS VOZES • MEMÓRIAS PARTILHADAS"; noticeTime = 1.4f;
    }
    private void CastFinalSpark()
    {
        if (finalEnding != 2) { notice = "A ÚLTIMA CENTELHA AINDA NÃO FOI APRENDIDA"; noticeTime = 1.5f; return; }
        if (room is 3 or 18 || enemies.Count == 0 || finalStrikeCd > 0) return;
        Enemy target = lockedTarget is not null && enemies.Contains(lockedTarget) ? lockedTarget : enemies.OrderBy(enemy => Distance(player, enemy.Pos)).First();
        facing = Normalize(Sub(target.Pos, player), facing); finalStrikeTarget = target.Pos; finalStrikeFlash = 0.24f;
        DamageEnemy(target, damage * (target.Boss ? 2.15f : 2.45f) * SkillDamageVariant(8) * TechniqueDamageMultiplier() * WeaponSkillMultiplier() * OutgoingDamageMultiplier(target) * (IsArmored(target) ? 0.75f : 1f));
        target.Flash = 0.3f; if (!target.Boss) { target.Windup = 0; target.Stagger = Math.Max(target.Stagger, 0.72f); target.Cooldown = Math.Max(target.Cooldown, 1.25f); }
        if (target.Hp <= 0) Defeat(target);
        finalStrikeCd = Math.Max(12f, 19f - skillTalent * 0.65f) * (skillVariants[8] == 1 ? 1.25f : skillVariants[8] == 2 ? 0.75f : 1f) * WeaponCooldownMultiplier(); notice = "ÚLTIMA CENTELHA • IMPACTO CONCENTRADO"; noticeTime = 1.4f;
    }
    private void TickBeacon(float dt)
    {
        if (beaconTime <= 0) return;
        beaconTime = Math.Max(0, beaconTime - dt); beaconPulseCd -= dt; beaconWaveTime = Math.Max(0, beaconWaveTime - dt);
        if (beaconPulseCd > 0) return;
        beaconPulseCd = (HasCharm(6) ? 0.85f : 1.1f) * (skillVariants[6] == 2 ? 0.75f : 1f); beaconWaveTime = 0.38f;
        foreach (Enemy enemy in enemies.ToArray())
        {
            if (Distance(beaconPos, enemy.Pos) > 128) continue;
            bool resonantTarget = enemy.BeaconMarkTime > 0;
            float multiplier = TechniqueDamageMultiplier() * SkillDamageVariant(6) * WeaponSkillMultiplier() * (resonantTarget ? 1.35f : 1f);
            DamageEnemy(enemy, damage * (enemy.Boss ? 0.18f : 0.34f) * multiplier * OutgoingDamageMultiplier(enemy) * (IsArmored(enemy) ? 0.75f : 1f));
            if (resonantTarget) { enemy.BeaconMarkTime = 0; AddFloatingText(new PointF(enemy.Pos.X, enemy.Pos.Y - enemy.Radius - 26), "FAROL SINTONIZADO", Color.LightCyan); }
            enemy.Flash = 0.16f;
            if (enemy.Hp <= 0) Defeat(enemy);
        }
        if (beaconTime <= 0) beaconWaveTime = 0;
    }

    private PointF CurrentDrift()
    {
        if (room == 25 && player.X is > 300 and < 670 && player.Y is > 286 and < 384) return new PointF(58, 0);
        if (room == 26)
        {
            PointF center = new(480, 330); float distance = Distance(player, center);
            if (distance is > 90 and < 260) return Scale(Normalize(Sub(center, player), PointF.Empty), 34);
        }
        if (room == 28)
        {
            PointF center = new(480, 330); float distance = Distance(player, center);
            if (distance is > 100 and < 250)
            {
                PointF radial = Normalize(Sub(center, player), PointF.Empty);
                float direction = MathF.Sin(windupPulse * 1.35f) >= 0 ? 1 : -1;
                return Scale(radial, 42 * direction);
            }
        }
        return PointF.Empty;
    }

    private void AdvanceVoiceShards(float dt)
    {
        for (int i = echoShards.Count - 1; i >= 0; i--)
        {
            EchoShard shard = echoShards[i]; shard.Life -= dt;
            float distance = 520 * dt; int steps = Math.Max(1, (int)MathF.Ceiling(distance / 6)); float step = distance / steps; bool blocked = false;
            for (int n = 0; n < steps; n++)
            {
                PointF next = Add(shard.Pos, Scale(shard.Direction, step));
                if (Blocked(next, 4)) { blocked = true; break; }
                shard.Pos = next;
                foreach (Enemy enemy in enemies.ToArray())
                {
                    if (shard.Hit.Contains(enemy) || DistanceToSegment(enemy.Pos, Sub(shard.Pos, Scale(shard.Direction, step)), shard.Pos) >= enemy.Radius + 8) continue;
                    shard.Hit.Add(enemy);
                    bool bellResonantTarget = BellShardSynergyActive() && enemy.BellMarkTime > 0;
                    DamageEnemy(enemy, shard.Damage * (bellResonantTarget ? 1.35f : 1f) * OutgoingDamageMultiplier(enemy) * (IsArmored(enemy) ? 0.75f : 1)); enemy.Flash = 0.16f;
                    if (bellResonantTarget) { enemy.BellMarkTime = 0; AddFloatingText(new PointF(enemy.Pos.X, enemy.Pos.Y - enemy.Radius - 26), "SINO + ESTILHAÇO", Color.LightGoldenrodYellow); }
                    if (enemy.Hp <= 0) Defeat(enemy);
                }
            }
            if (blocked || shard.Life <= 0 || shard.Pos.X < 75 || shard.Pos.X > 885 || shard.Pos.Y < 110 || shard.Pos.Y > 550) echoShards.RemoveAt(i);
        }
    }
}
