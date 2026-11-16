using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;

public sealed partial class Game
{
    private static float RangedImpactRadius(EnemyKind kind) => EnemyData(kind).RangedImpactRadius;

    private void CreateSlagHazard(PointF position) => groundHazards.Add(new GroundHazard { Pos = position });

    private void TickGroundHazards(float dt)
    {
        for (int i = groundHazards.Count - 1; i >= 0; i--)
        {
            GroundHazard hazard = groundHazards[i]; hazard.TimeLeft -= dt; hazard.WarningTime = Math.Max(0, hazard.WarningTime - dt); hazard.TickCooldown -= dt;
            if (hazard.TimeLeft <= 0) { groundHazards.RemoveAt(i); continue; }
            if (hazard.WarningTime > 0) continue;
            if (hazard.TickCooldown > 0) continue;
            hazard.TickCooldown = 0.85f;
            if (Distance(player, hazard.Pos) <= hazard.Radius && invulnerable <= 0 && dashTime <= 0) HitPlayer(hazard.Damage);
        }
    }

    private static float NonBossAttackRadius(Enemy e)
    {
        if (e.Kind == EnemyKind.Elite) return e.EliteAffix == EliteAffix.Spearbound ? 36 : 62;
        if (e.Kind == EnemyKind.Mage) return RangedImpactRadius(e.Kind);
        float ranged = RangedImpactRadius(e.Kind);
        return ranged > 0 ? ranged : 38;
    }
    private static PointF NonBossAttackCenter(Enemy e) => e.Kind is EnemyKind.Elite or EnemyKind.Mage || RangedImpactRadius(e.Kind) > 0 ? e.Target : e.Pos;
    private static bool NonBossAttackContains(Enemy e, PointF point) => Distance(point, NonBossAttackCenter(e)) < NonBossAttackRadius(e);

    private static bool BossUsesCone(Enemy e) => e.Kind == EnemyKind.GlassHerald && e.BossPattern == 2;
    private static bool BossUsesRing(Enemy e) => e.BossPattern == 0 && e.Kind is EnemyKind.RouteScribe or EnemyKind.LastTuner;
    private static float BossRingInnerRadius(Enemy e) => e.Kind == EnemyKind.LastTuner ? 96 : e.Kind == EnemyKind.RouteScribe ? 68 : 0;
    private static bool BossUsesLine(Enemy e) => e.BossPattern == 2 && e.Kind is EnemyKind.FirstKeeper or EnemyKind.NameWeaver or EnemyKind.OathCartographer or EnemyKind.NameBroker or EnemyKind.TideWitness or EnemyKind.ReedKeeper or EnemyKind.FlowKeeper or EnemyKind.CounterCurrent or EnemyKind.RiftCantor or EnemyKind.LanternRegent or EnemyKind.RouteScribe or EnemyKind.DeepSilence or EnemyKind.BorrowedChorus or EnemyKind.LastTuner;
    private static bool BossConeContains(Enemy e, PointF point)
    {
        PointF axis = Normalize(Sub(e.Target, e.Pos), new PointF(1, 0)), direction = Sub(point, e.Pos); float distance = Length(direction);
        return distance < 245 && distance > 0.01f && Dot(axis, Scale(direction, 1 / distance)) >= MathF.Cos(34 * MathF.PI / 180);
    }
    private static float BossCircleRadius(Enemy e) => e.NameOverride == "VIGIA DAS COMPORTAS" ? (e.BossPattern == 0 ? 105 : 55) : e.BossPattern == 0 ? e.Kind switch { EnemyKind.GlassHerald => 135, EnemyKind.LanternRegent => 150, EnemyKind.RouteScribe => 160, EnemyKind.LastTuner => 178, _ => 90 } : e.BossPattern == 2 ? e.Kind switch { EnemyKind.RiverFerryman => 85, EnemyKind.AshWarden => 68, EnemyKind.HollowName => 85, _ => 42 } : 42;
    private static bool BossAttackContains(Enemy e, PointF point)
    {
        if (BossUsesCone(e)) return BossConeContains(e, point);
        if (BossUsesRing(e)) { float distance = Distance(point, e.Pos); return distance > BossRingInnerRadius(e) && distance < BossCircleRadius(e); }
        if (!BossUsesLine(e)) return Distance(point, e.BossPattern == 0 ? e.Pos : e.Target) < BossCircleRadius(e);
        float width = e.Kind == EnemyKind.FirstKeeper ? 25 : 24;
        if (DistanceToSegment(point, e.Pos, e.Target) < width) return true;
        return e.Kind is EnemyKind.FirstKeeper or EnemyKind.DeepSilence or EnemyKind.BorrowedChorus or EnemyKind.LastTuner && DistanceToSegment(point, e.Target2Start, e.Target2) < width;
    }

    private static (Color Fill, Color Edge) AttackWarningPalette(bool highContrast) => highContrast
        ? (Color.FromArgb(78, 255, 217, 36), Color.FromArgb(255, 255, 244, 122))
        : (Color.FromArgb(45, 235, 115, 75), Color.FromArgb(235, 235, 115, 75));

    private void DrawBossWarning(Graphics g, Enemy e)
    {
        (Color fillColor, Color edgeColor) = AttackWarningPalette(highContrastTelegraphs);
        float urgency = Math.Clamp(1f - e.Windup / Math.Max(0.01f, e.WindupMax), 0f, 1f);
        float pulse = (0.5f + 0.5f * MathF.Sin(windupPulse * (7f + urgency * 11f))) * urgency;
        fillColor = Color.FromArgb(Math.Clamp(fillColor.A + (int)(urgency * 24), 0, 255), fillColor);
        edgeColor = Color.FromArgb(Math.Clamp(edgeColor.A + (int)(urgency * 18 + pulse * 35), 0, 255), edgeColor);
        using var outline = new Pen(edgeColor, highContrastTelegraphs ? 4 + urgency + pulse : 2.5f + urgency * 2 + pulse * 1.5f);
        using var fill = new SolidBrush(fillColor);
        if (BossUsesCone(e))
        {
            float radius = 245, angle = MathF.Atan2(e.Target.Y - e.Pos.Y, e.Target.X - e.Pos.X) * 180 / MathF.PI, sweep = 68;
            var cone = new RectangleF(e.Pos.X - radius, e.Pos.Y - radius, radius * 2, radius * 2);
            if (highContrastTelegraphs) { using var shadow = new Pen(Color.FromArgb(245, 12, 13, 15), 9); g.DrawPie(shadow, cone, angle - sweep / 2, sweep); }
            g.FillPie(fill, cone, angle - sweep / 2, sweep); g.DrawPie(outline, cone, angle - sweep / 2, sweep);
            return;
        }
        if (BossUsesRing(e))
        {
            float radius = BossCircleRadius(e), innerRadius = BossRingInnerRadius(e);
            var outer = new RectangleF(e.Pos.X - radius, e.Pos.Y - radius, radius * 2, radius * 2);
            var inner = new RectangleF(e.Pos.X - innerRadius, e.Pos.Y - innerRadius, innerRadius * 2, innerRadius * 2);
            using var ring = new GraphicsPath(FillMode.Alternate); ring.AddEllipse(outer); ring.AddEllipse(inner);
            if (highContrastTelegraphs) { using var shadow = new Pen(Color.FromArgb(245, 12, 13, 15), 9); g.DrawEllipse(shadow, outer); g.DrawEllipse(shadow, inner); }
            g.FillPath(fill, ring); g.DrawEllipse(outline, outer); g.DrawEllipse(outline, inner);
            return;
        }
        if (!BossUsesLine(e))
        {
            float radius = BossCircleRadius(e); PointF center = e.BossPattern == 0 ? e.Pos : e.Target;
            g.FillEllipse(fill, center.X - radius, center.Y - radius, radius * 2, radius * 2);
            if (highContrastTelegraphs) { using var shadow = new Pen(Color.FromArgb(245, 12, 13, 15), 8); g.DrawEllipse(shadow, center.X - radius, center.Y - radius, radius * 2, radius * 2); }
            g.DrawEllipse(outline, center.X - radius, center.Y - radius, radius * 2, radius * 2);
            if (e.NameOverride == "VIGIA DAS COMPORTAS")
                DrawCentered(g, e.BossPattern == 0 ? "ONDA" : "MARCA", center.Y - radius - 18, new Font("Segoe UI", 9, FontStyle.Bold), highContrastTelegraphs ? Color.White : Color.FromArgb(255, 211, 163), center.X);
            return;
        }
        float width = e.Kind == EnemyKind.FirstKeeper ? 25 : 24;
        using var area = new Pen(fillColor, width * 2) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        if (highContrastTelegraphs) { using var shadow = new Pen(Color.FromArgb(240, 12, 13, 15), width * 2 + 8) { StartCap = LineCap.Round, EndCap = LineCap.Round }; g.DrawLine(shadow, e.Pos, e.Target); if (e.Kind is EnemyKind.FirstKeeper or EnemyKind.DeepSilence or EnemyKind.BorrowedChorus or EnemyKind.LastTuner) g.DrawLine(shadow, e.Target2Start, e.Target2); }
        g.DrawLine(area, e.Pos, e.Target); g.DrawLine(outline, e.Pos, e.Target);
        if (e.Kind is EnemyKind.FirstKeeper or EnemyKind.DeepSilence or EnemyKind.BorrowedChorus or EnemyKind.LastTuner) { g.DrawLine(area, e.Target2Start, e.Target2); g.DrawLine(outline, e.Target2Start, e.Target2); }
    }

    private void UpdateEnemy(Enemy e, float dt)
    {
        e.AttackPoseTime = Math.Max(0, e.AttackPoseTime - dt); e.BeaconMarkTime = Math.Max(0, e.BeaconMarkTime - dt); e.BellMarkTime = Math.Max(0, e.BellMarkTime - dt); e.NameWeaverMarkTime = Math.Max(0, e.NameWeaverMarkTime - dt); e.ListenerMarkTime = Math.Max(0, e.ListenerMarkTime - dt); e.SparkMarkTime = Math.Max(0, e.SparkMarkTime - dt);
        if (e.KnockbackTime > 0)
        {
            float step = Math.Min(dt, e.KnockbackTime);
            e.Pos = MoveEnemy(e, Scale(e.KnockbackVelocity, step));
            e.KnockbackTime = Math.Max(0, e.KnockbackTime - step);
            e.KnockbackVelocity = Scale(e.KnockbackVelocity, MathF.Pow(0.025f, step / 0.16f));
            if (e.KnockbackTime <= 0) e.KnockbackVelocity = PointF.Empty;
        }
        if (HasEliteAffix(e, EliteAffix.Summoner))
        {
            if (e.SummonWindup > 0 && e.Stagger > 0) { e.SummonWindup = 0; e.SummonCooldown = 2.5f; }
            bool hasEliteEcho = enemies.Any(enemy => enemy.EliteMinion);
            if (e.SummonWindup > 0)
            {
                e.SummonWindup -= dt;
                if (e.SummonWindup <= 0) SummonEliteEcho(e);
                return;
            }
            if (!hasEliteEcho && e.Stagger <= 0 && e.SummonCooldown > 0) e.SummonCooldown -= dt;
            if (!hasEliteEcho && e.SummonCooldown <= 0 && e.Windup <= 0 && e.Stagger <= 0)
            {
                e.SummonWindup = 0.95f;
                GameAudio.Play(GameAudio.Cue.EliteWarning);
                return;
            }
        }
        if (HasEliteAffix(e, EliteAffix.Conductor) && UpdateConductor(e, dt)) return;
        float attackTempo = vigilActive && e.Boss && RunVigilAspect(vigilFloor) == VigilAspect.Swift ? 1.25f : 1;
        e.Cooldown -= dt * attackTempo; e.Flash = Math.Max(0, e.Flash - dt); PointF offset = Sub(player, e.Pos); float dist = Length(offset);
        if (e.Stagger > 0) { e.Stagger = Math.Max(0, e.Stagger - dt); return; }
        if (e.Windup > 0) {
            e.Windup -= dt * attackTempo;
            if (e.Windup <= 0) e.AttackPoseTime = 0.16f;
            bool inAttack = e.Boss ? BossAttackContains(e, player) : NonBossAttackContains(e, player);
            if (e.Windup <= 0 && inAttack && invulnerable <= 0 && dashTime <= 0 && (room < 43 || !BlockedLine(e.Pos, player)))
            {
                HitPlayer(EnemyContactDamage(e));
                if (e.Kind == EnemyKind.CooldownLeech) { DelayTechniqueCooldowns(1.1f); AddFloatingText(new PointF(player.X, player.Y - 46), "RECARGAS +1,1 S", Color.MediumOrchid); }
                if (HasEliteAffix(e, EliteAffix.Bloodbound)) e.Hp = Math.Min(e.MaxHp, e.Hp + 8);
            }
            if (e.Windup <= 0 && e.Kind == EnemyKind.SlagWeaver)
            {
                CreateSlagHazard(e.Target);
                AddFloatingText(new PointF(e.Target.X, e.Target.Y - 46), "ESCÓRIA VIVA", Color.FromArgb(241, 171, 106));
            }
            return;
        }
        if (room == 43 && (e.Pos.X < 1820 && player.X > 1820 || e.Pos.X > 1660 && player.X < 1660) && BlockedLine(e.Pos, player))
        {
            float bridgeY = Math.Abs(e.Pos.Y - 700) <= Math.Abs(e.Pos.Y - 1050) ? 700 : 1050;
            PointF waypoint = Math.Abs(e.Pos.Y - bridgeY) > 15 ? new PointF(e.Pos.X, bridgeY) : new PointF(player.X > 1820 ? 1850 : 1630, bridgeY);
            e.Pos = MoveEnemy(e, Scale(Normalize(Sub(waypoint, e.Pos), PointF.Empty), 70 * dt));
            return;
        }
        if (room is 44 or 45 && BlockedLine(e.Pos, player))
        {
            e.PathRefreshTime -= dt;
            if (e.PathRefreshTime <= 0)
            {
                e.PathWaypoint = InteriorDetour(e);
                e.PathRefreshTime = 0.35f;
            }
            e.Pos = MoveEnemy(e, Scale(Normalize(Sub(e.PathWaypoint, e.Pos), PointF.Empty), 70 * dt));
            return;
        }
        if (UpdateDataDrivenEnemyBehavior(e, dt, dist, offset)) return;
        EnemyDefinition behavior = EnemyData(e.Kind);
        if (e.Kind == EnemyKind.Elite && e.Cooldown <= 0 && dist < behavior.AttackRange) { bool swift = HasEliteAffix(e, EliteAffix.Swift), armored = HasEliteAffix(e, EliteAffix.Armored); BeginWindup(e, player, swift ? armored ? 0.48f : 0.38f : armored ? 0.72f : behavior.AttackWindup); e.Cooldown = swift ? armored ? 2f : 1.55f : armored ? 2.5f : behavior.AttackCooldown; return; }
        if (e.Kind == EnemyKind.LastTuner && e.Cooldown <= 0)
        {
            e.BossPattern = (e.BossPattern + 1) % 3;
            float windup = e.Hp < e.MaxHp * 0.5f ? 0.54f : 0.82f;
            if (vigilActive && RunVigilAspect(vigilFloor) == VigilAspect.Swift) windup *= 0.8f;
            BeginWindup(e, e.BossPattern is 1 or 2 ? player : e.Pos, windup); e.Cooldown = e.Hp < e.MaxHp * 0.5f ? 1.35f : 2.1f; return;
        }
        if (e.Boss && e.Cooldown <= 0 && (dist < 135 || (e.Kind is EnemyKind.BlindScribe or EnemyKind.RiverFerryman or EnemyKind.AshWarden or EnemyKind.HollowName or EnemyKind.NameWeaver or EnemyKind.FirstKeeper or EnemyKind.OathCartographer or EnemyKind.NameBroker or EnemyKind.TideWitness or EnemyKind.ReedKeeper or EnemyKind.FlowKeeper or EnemyKind.CounterCurrent or EnemyKind.RiftCantor or EnemyKind.GlassHerald or EnemyKind.LanternRegent or EnemyKind.RouteScribe or EnemyKind.DeepSilence or EnemyKind.BorrowedChorus))) { e.BossPattern = (e.Kind is EnemyKind.RiverFerryman or EnemyKind.AshWarden or EnemyKind.HollowName or EnemyKind.NameWeaver or EnemyKind.FirstKeeper or EnemyKind.OathCartographer or EnemyKind.NameBroker or EnemyKind.TideWitness or EnemyKind.ReedKeeper or EnemyKind.FlowKeeper or EnemyKind.CounterCurrent or EnemyKind.RiftCantor or EnemyKind.GlassHerald or EnemyKind.LanternRegent or EnemyKind.RouteScribe or EnemyKind.DeepSilence or EnemyKind.BorrowedChorus) ? (e.BossPattern + 1) % 3 : 1 - e.BossPattern; bool aimed = (e.Kind == EnemyKind.BlindScribe && e.BossPattern == 1) || ((e.Kind is EnemyKind.RiverFerryman or EnemyKind.AshWarden) && (e.BossPattern is 1 or 2)) || (e.Kind is EnemyKind.HollowName or EnemyKind.NameWeaver or EnemyKind.FirstKeeper or EnemyKind.OathCartographer or EnemyKind.NameBroker or EnemyKind.TideWitness or EnemyKind.ReedKeeper or EnemyKind.FlowKeeper or EnemyKind.CounterCurrent or EnemyKind.RiftCantor or EnemyKind.GlassHerald or EnemyKind.LanternRegent or EnemyKind.RouteScribe or EnemyKind.DeepSilence or EnemyKind.BorrowedChorus) && e.BossPattern is 1 or 2; float windup = e.Hp < e.MaxHp * 0.5f ? 0.52f : 0.78f; if (vigilActive && RunVigilAspect(vigilFloor) == VigilAspect.Swift) windup *= 0.8f; if (e.Kind == EnemyKind.FirstKeeper && e.BossPattern == 2) { PointF axis = Normalize(Sub(player, e.Pos), new PointF(1, 0)); PointF side = new(-axis.Y, axis.X); e.Target = Add(e.Pos, Scale(axis, 920)); e.Target2Start = Add(player, Scale(side, -460)); e.Target2 = Add(player, Scale(side, 460)); e.Windup = e.WindupMax = windup; GameAudio.Play(GameAudio.Cue.BossWarning); } else BeginWindup(e, aimed || e.NameOverride == "VIGIA DAS COMPORTAS" && e.BossPattern == 1 ? player : e.Pos, windup); e.Cooldown = e.Hp < e.MaxHp * 0.5f ? 1.25f : 2.0f; return; }
        if (dist > (e.Boss ? 40 : 30)) e.Pos = MoveEnemy(e, Scale(Normalize(offset, PointF.Empty), (e.Boss ? 55 : MeleeChaseSpeed(room >= 43 ? 4 : room, HasEliteAffix(e, EliteAffix.Swift))) * dt));
        else if (e.Cooldown <= 0) { BeginWindup(e, player, 0.45f); e.Cooldown = 1.35f; }
        if (!e.Boss) SeparateFromPack(e, dt);
    }

    private bool UpdateConductor(Enemy conductor, float dt)
    {
        if (conductor.SupportWindup > 0)
        {
            if (conductor.Stagger > 0) { conductor.SupportWindup = 0; conductor.SupportCooldown = 1.8f; return true; }
            conductor.SupportWindup -= dt;
            if (conductor.SupportWindup <= 0)
            {
                foreach (Enemy ally in enemies.Where(ally => ConductorPulseCovers(conductor, ally)).ToArray())
                {
                    float healed = Math.Min(ally.MaxHp - ally.Hp, MathF.Ceiling(ally.MaxHp * 0.12f));
                    if (healed <= 0) continue;
                    ally.Hp += healed; ally.Flash = 0.18f;
                    AddFloatingText(new PointF(ally.Pos.X, ally.Pos.Y - ally.Radius - 10), $"+{Math.Max(1, (int)Math.Round(healed))}", Color.LightGreen);
                }
            }
            return true;
        }
        if (conductor.Stagger > 0) return false;
        conductor.SupportCooldown = Math.Max(0, conductor.SupportCooldown - dt);
        if (conductor.SupportCooldown <= 0 && conductor.Windup <= 0)
        {
            conductor.SupportWindup = 0.9f; conductor.SupportCooldown = 5.8f;
            GameAudio.Play(GameAudio.Cue.EliteWarning); return true;
        }
        return false;
    }

    private static bool ConductorPulseCovers(Enemy conductor, Enemy ally) => Distance(conductor.Pos, ally.Pos) <= ConductorPulseRadius;

    private void SummonEliteEcho(Enemy summoner)
    {
        if (!enemies.Contains(summoner)) return;
        PointF spawn = Add(summoner.Pos, new PointF(34, -26));
        if (Blocked(spawn, 14)) spawn = Add(summoner.Pos, new PointF(-34, 26));
        Enemy echo = new() { Pos = spawn, Kind = EnemyKind.InkEcho, Hp = 42 + Math.Max(0, room) * 2, MaxHp = 42 + Math.Max(0, room) * 2, Cooldown = 0.8f, EliteMinion = true };
        enemies.Add(echo);
        summoner.SummonCooldown = 7.5f;
        AddFloatingText(new PointF(summoner.Pos.X, summoner.Pos.Y - 36), "ECO INVOCADO", Color.MediumOrchid);
    }

    private static void BeginWindup(Enemy enemy, PointF target, float duration)
    {
        enemy.Target = target; enemy.Windup = enemy.WindupMax = duration; enemy.AttackPoseTime = 0;
        if (enemy.Boss) GameAudio.Play(GameAudio.Cue.BossWarning);
        else if (enemy.Kind == EnemyKind.Elite) GameAudio.Play(GameAudio.Cue.EliteWarning);
        if (enemy.Kind == EnemyKind.DeepSilence && enemy.BossPattern == 2)
        {
            PointF axis = Normalize(Sub(target, enemy.Pos), new PointF(1, 0)); PointF side = new(-axis.Y, axis.X);
            enemy.Target2Start = Add(enemy.Pos, Scale(side, 74)); enemy.Target2 = Add(target, Scale(side, 74));
        }
        if (enemy.Kind == EnemyKind.LastTuner && enemy.BossPattern == 2)
        {
            PointF axis = Normalize(Sub(target, enemy.Pos), new PointF(1, 0)); PointF side = new(-axis.Y, axis.X);
            PointF marked = target; enemy.Target = Add(enemy.Pos, Scale(axis, 920));
            enemy.Target2Start = Add(marked, Scale(side, -460)); enemy.Target2 = Add(marked, Scale(side, 460));
        }
    }
    private void DamageEnemy(Enemy enemy, float amount, bool critical = false)
    {
        if ((combatInstinct == 1 && enemy.Hp >= enemy.MaxHp * 0.8f) || (combatInstinct == 2 && enemy.Hp <= enemy.MaxHp * 0.3f)) amount *= 1.18f;
        if (vigilActive && enemy.Boss && RunVigilAspect(vigilFloor) == VigilAspect.Hardened) amount *= 0.75f;
        if (enemy.NameWeaverMarkTime > 0) amount *= enemy.NameWeaverMarkMultiplier;
        if (enemy.ListenerMarkTime > 0) amount *= enemy.ListenerMarkMultiplier;
        enemy.Hp -= amount;
        if (amount <= 0) return;
        TriggerScreenShake(critical ? 3.2f : amount >= damage * 1.5f ? 2.2f : 0.8f, critical ? 0.14f : 0.1f);
        string value = Math.Max(1, (int)Math.Round(amount)).ToString();
        AddFloatingText(new PointF(enemy.Pos.X, enemy.Pos.Y - enemy.Radius - 10), critical ? $"CRÍTICO {value}" : value, critical ? Color.FromArgb(255, 211, 123) : Color.AntiqueWhite);
    }

    private void ApplyHitKnockback(Enemy enemy, bool critical, int? weaponIndex = null)
    {
        int index = weaponIndex ?? weapon;
        if (index == 5 && !enemy.Boss && enemy.Kind != EnemyKind.Elite)
        {
            enemy.KnockbackVelocity = Scale(Normalize(Sub(player, enemy.Pos), facing), critical ? 108f : 88f);
            enemy.KnockbackTime = Math.Max(enemy.KnockbackTime, 0.2f);
            return;
        }
        float speed = index switch { 1 => 220f, 2 => 105f, 3 => 175f, 5 => 70f, _ => 150f };
        if (critical) speed *= 1.22f;
        if (enemy.Boss) speed *= 0.32f;
        enemy.KnockbackVelocity = Scale(Normalize(Sub(enemy.Pos, player), facing), speed);
        enemy.KnockbackTime = Math.Max(enemy.KnockbackTime, 0.16f);
    }

    private void ApplyWeaponWound(Enemy enemy, bool critical, int? weaponIndex = null, float? baseDamage = null)
    {
        int index = weaponIndex ?? weapon;
        if (!critical || enemy.Hp <= 0 || !HasWeaponAffix(index, 8)) return;
        float perSecond = WeaponAffixValue(index, 8) / 100f;
        float resonance = HasWoundResonance(index) ? 1.4f : 1f;
        if (enemy.WoundTime <= 0) enemy.WoundTickCooldown = 1f;
        enemy.WoundTime = 3f;
        enemy.WoundDamage = (baseDamage ?? damage) * perSecond * resonance * OutgoingDamageMultiplier(enemy, index) * (IsArmored(enemy) ? 0.75f : 1f);
        AddFloatingText(new PointF(enemy.Pos.X, enemy.Pos.Y - enemy.Radius - 26), "RUPTURA", Color.Salmon);
    }

    private void TickEnemyWounds(float dt)
    {
        foreach (Enemy enemy in enemies.ToArray())
        {
            if (enemy.WoundTime <= 0) continue;
            enemy.WoundTickCooldown -= dt;
            if (enemy.WoundTickCooldown <= 0)
            {
                enemy.WoundTickCooldown += 1f;
                DamageEnemy(enemy, enemy.WoundDamage);
                if (enemy.Hp <= 0) { Defeat(enemy); continue; }
            }
            enemy.WoundTime = Math.Max(0, enemy.WoundTime - dt);
            if (enemy.WoundTime <= 0) { enemy.WoundDamage = 0; enemy.WoundTickCooldown = 0; }
        }
    }

    private void HitPlayer(float value)
    {
        GameAudio.Play(GameAudio.Cue.Impact);
        float vigilPressure = vigilActive ? 1 + Math.Min(1.5f, Math.Max(0, vigilFloor - 1) * 0.08f) : 1;
        float aspectPressure = vigilActive && RunVigilAspect(vigilFloor) == VigilAspect.Hungry ? 1.3f : 1;
        if (vigilActive && HasCharm(7)) aspectPressure *= 1.1f;
        float campaignPressure = vigilActive ? 1f : DifficultyIncomingDamageMultiplier(difficultyTier);
        float damageTaken = value * vigilPressure * aspectPressure * campaignPressure * HeroDamageTakenMultiplier() * ArmorDamageTakenMultiplier() * RelicDamageTakenMultiplier() * (wardTime > 0 ? AnchorWardDamageTakenMultiplier() : 1) * (stoneBastionTime > 0 ? StoneBastionDamageTakenMultiplier() : 1f) * WeaponDamageTakenMultiplier() * CharmDamageTakenMultiplier() * CombatMasteryDamageTakenMultiplier() * CombatApexDamageTakenMultiplier();
        health = Math.Max(0, health - damageTaken); invulnerable = 0.6f;
        TriggerScreenShake(Math.Clamp(damageTaken * 0.13f, 2f, 7f), 0.24f);
        TriggerHitStop(0.055f);
        if (heroClass == HeroClass.StoneGuardian && stoneFury < 100)
        {
            stoneFury = Math.Min(100, stoneFury + damageTaken * 2.25f);
            if (stoneFury >= 100) AddFloatingText(new PointF(player.X, player.Y - 46), "FÚRIA PRONTA • ATAQUE BÁSICO", Color.FromArgb(255, 203, 116));
        }
        AddFloatingText(new PointF(player.X, player.Y - 30), $"−{Math.Max(1, (int)Math.Round(damageTaken))}", Color.FromArgb(250, 116, 105));
        if (health <= 0)
        {
            if (vigilActive)
            {
                int failedFloor = vigilFloor; vigilActive = false; vigilChoiceOpen = false; vigilFloor = 0; room = 18; SetupRoom(); health = Math.Max(1, maxHealth * 0.5f);
                ShowMemory("O ECO SE DESFAZ", $"Aven recolhe as lembranças antes que a Vigília possa prendê-las. Você retorna ao Refúgio com metade da vida. Melhor andar registrado: {vigilBestFloor}; a tentativa terminou no andar {failedFloor}.");
            }
            else dead = true;
        }
    }

    private void TriggerScreenShake(float strength, float duration)
    {
        if (reducedScreenShake) strength *= 0.3f;
        screenShakeStrength = Math.Max(screenShakeStrength, strength);
        screenShakeTime = Math.Max(screenShakeTime, duration);
    }

    private void TriggerHitStop(float duration) => hitStopTime = Math.Max(hitStopTime, duration);

    private void AddFloatingText(PointF pos, string text, Color color)
    {
        if (floatingTexts.Count >= 48) floatingTexts.RemoveAt(0);
        float spread = floatingTexts.Count % 5 * 12 - 24;
        floatingTexts.Add(new FloatingText { Pos = new PointF(pos.X + spread, pos.Y), Text = text, Color = color, TimeLeft = 0.85f, Duration = 0.85f });
    }

    private void TickFloatingTexts(float dt)
    {
        for (int i = floatingTexts.Count - 1; i >= 0; i--)
        {
            FloatingText text = floatingTexts[i]; text.TimeLeft -= dt; text.Pos = new PointF(text.Pos.X, text.Pos.Y - 28 * dt);
            if (text.TimeLeft <= 0) floatingTexts.RemoveAt(i);
        }
    }
    private static EliteAffix EliteAffixForRoll(int roll) => roll switch { 0 => EliteAffix.Armored, 1 => EliteAffix.Swift, 2 => EliteAffix.Bloodbound, 3 => EliteAffix.Spearbound, 5 => EliteAffix.Conductor, _ => EliteAffix.Summoner };
    private static EliteAffix EliteSecondaryAffixForRoll(int roll, EliteAffix primary)
    {
        EliteAffix[] options = { EliteAffix.Armored, EliteAffix.Swift, EliteAffix.Bloodbound };
        EliteAffix[] eligible = options.Where(affix => affix != primary && !(primary == EliteAffix.Spearbound && affix == EliteAffix.Swift)).ToArray();
        return eligible[Math.Abs(roll % eligible.Length)];
    }
    private static bool HasEliteAffix(Enemy enemy, EliteAffix affix) => enemy.Kind == EnemyKind.Elite && (enemy.EliteAffix == affix || enemy.EliteAffix2 == affix);
    private static string EliteAffixName(EliteAffix affix) => affix switch { EliteAffix.Armored => "BLINDADO", EliteAffix.Swift => "VELOZ", EliteAffix.Bloodbound => "SANGRADOR", EliteAffix.Spearbound => "LANÇADOR", EliteAffix.Summoner => "INVOCADOR", EliteAffix.Conductor => "CONDUTOR", _ => "ELITE" };
    private static string EliteAffixName(Enemy enemy) => enemy.EliteAffix2 is EliteAffix second ? $"{EliteAffixName(enemy.EliteAffix)} + {EliteAffixName(second)}" : EliteAffixName(enemy.EliteAffix);
    private static Color EliteAffixColor(EliteAffix affix) => affix switch { EliteAffix.Armored => Color.LightSteelBlue, EliteAffix.Swift => Color.Gold, EliteAffix.Bloodbound => Color.HotPink, EliteAffix.Spearbound => Color.DeepSkyBlue, EliteAffix.Summoner => Color.MediumOrchid, EliteAffix.Conductor => Color.MediumAquamarine, _ => Color.IndianRed };
}
