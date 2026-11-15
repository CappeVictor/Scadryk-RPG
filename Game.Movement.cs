using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

public sealed partial class Game
{
    private float DashDuration() => HasCharm(2) ? 0.29f : 0.17f;
    private PointF MovementInput() => new((down.Contains(Keys.D) || down.Contains(Keys.Right) ? 1 : 0) - (down.Contains(Keys.A) || down.Contains(Keys.Left) ? 1 : 0), (down.Contains(Keys.S) || down.Contains(Keys.Down) ? 1 : 0) - (down.Contains(Keys.W) || down.Contains(Keys.Up) ? 1 : 0));
    private void StartDash()
    {
        if (dashCd > 0) return;
        PointF movement = MovementInput();
        dashDirection = Normalize(movement, facing);
        if (Length(movement) > 0.001f) facing = dashDirection;
        dashTime = DashDuration(); dashCd = DashCooldown();
        GameAudio.Play(GameAudio.Cue.Dash);
    }
    private float DashCooldown() => Math.Max(0.35f, 1.15f - dodgeTalent * 0.12f + (combatMastery == 1 ? 0.2f : combatMastery == 2 ? -0.18f : 0f) - (armorOn && armorTrait == 2 ? 0.12f : 0f)) + (HasCharm(2) && !HasCharmPair(2, 3) ? 0.3f : 0f);

    private PointF MoveEnemy(Enemy e, PointF delta) { PointF p = e.Pos; PointF x = new(p.X + delta.X, p.Y), y = new(p.X, p.Y + delta.Y); if (!Blocked(x, e.Radius)) p.X = x.X; if (!Blocked(new PointF(p.X, y.Y), e.Radius)) p.Y = y.Y; SizeF size = WorldSize(); return new PointF(Math.Clamp(p.X, 85, size.Width - 85), Math.Clamp(p.Y, 128, size.Height - 107)); }
    private static float MeleeChaseSpeed(int room, bool swiftElite = false) => Math.Min(168f, 74f + Math.Max(0, room) * 4f) * (swiftElite ? 1.35f : 1f);
    private void SeparateFromPack(Enemy enemy, float dt) { PointF push = PointF.Empty; foreach (Enemy other in enemies) { if (ReferenceEquals(enemy, other)) continue; PointF away = Sub(enemy.Pos, other.Pos); float d = Length(away); if (d > 0.01f && d < 40) push = Add(push, Scale(away, (40 - d) / (40 * d))); } if (Length(push) > 0.01f) enemy.Pos = MoveEnemy(enemy, Scale(Normalize(push, PointF.Empty), 52 * dt)); }
    private void MovePlayer(PointF delta) { PointF x = new(player.X + delta.X, player.Y), y = new(player.X, player.Y + delta.Y); if (!Blocked(x)) player.X = x.X; if (!Blocked(new PointF(player.X, y.Y))) player.Y = y.Y; SizeF size = WorldSize(); player.X = Math.Clamp(player.X, 85, size.Width - 85); player.Y = Math.Clamp(player.Y, 128, size.Height - 107); }
    private RectangleF[] CurrentRoomWalls() => room switch
    {
        3 => TownBuildings, 4 => ArchiveWalls, 5 => VaultWalls, 6 => RiverWalls, 7 => BellChamberWalls, 8 => AshGalleryWalls, 9 => LumeChamberWalls, 10 => NameShoreWalls, 11 => VoiceWellWalls, 12 => FarReachWalls, 13 => NameCloisterWalls, 14 => FirstBasinWalls, 15 => FirstVigilWalls, 16 => WestFordWalls, 17 => WitnessYardWalls,
        18 => RefugeWalls, 19 => RidgeWalls, 20 => LedgerWalls, 21 => SaltMarshWalls, 22 => TideBridgeWalls, 23 => ReedRunWalls, 24 => ReedYardWalls, 25 => FloodMillWalls, 26 => SiltChamberWalls, 27 => CounterSlopeWalls, 28 => CounterHeartWalls, 29 => CantoriaWalls, 30 => RiftChamberWalls, 31 => GlassGorgeWalls, 32 => MirrorSourceWalls, 33 => LanternSpanWalls, 34 => LowTideObservatoryWalls, 35 => OpenRouteWalls, 36 => UnwrittenHallWalls, 37 => SilenceThresholdWalls, 38 => SilenceChamberWalls, 39 => BorrowedDockWalls, 40 => BorrowedArchiveWalls, 41 => UnsentChamberWalls, 42 => OffbeatWellWalls,
        43 => FirstEchoWalls, 44 => FirstEchoCaveWalls, 45 => FirstEchoSluiceWalls, _ => Walls
    };
    private bool Blocked(PointF p, float radius = 12) { RectangleF body = new(p.X - radius, p.Y - radius, radius * 2, radius * 2); return CurrentRoomWalls().Any(w => w.IntersectsWith(body)); }
    private PointF InteriorDetour(Enemy enemy)
    {
        float margin = enemy.Radius + 12;
        PointF destination = player;
        if (Blocked(destination, enemy.Radius))
        {
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < 8; i++)
            {
                float angle = i * MathF.PI / 4;
                PointF candidate = new(player.X + MathF.Cos(angle) * margin * 2, player.Y + MathF.Sin(angle) * margin * 2);
                if (candidate.X < 85 || candidate.X > 875 || candidate.Y < 128 || candidate.Y > 533 || Blocked(candidate, enemy.Radius)) continue;
                float distanceToEnemy = Distance(enemy.Pos, candidate);
                if (distanceToEnemy < bestDistance) { bestDistance = distanceToEnemy; destination = candidate; }
            }
        }
        var nodes = new System.Collections.Generic.List<PointF> { enemy.Pos, destination };
        foreach (RectangleF wall in CurrentRoomWalls())
        {
            PointF[] corners = { new(wall.Left - margin, wall.Top - margin), new(wall.Right + margin, wall.Top - margin), new(wall.Left - margin, wall.Bottom + margin), new(wall.Right + margin, wall.Bottom + margin) };
            foreach (PointF corner in corners)
                if (corner.X >= 85 && corner.X <= 875 && corner.Y >= 128 && corner.Y <= 533 && !Blocked(corner, enemy.Radius)) nodes.Add(corner);
        }
        float[] distance = Enumerable.Repeat(float.PositiveInfinity, nodes.Count).ToArray();
        int[] previous = Enumerable.Repeat(-1, nodes.Count).ToArray();
        bool[] visited = new bool[nodes.Count]; distance[0] = 0;
        for (int pass = 0; pass < nodes.Count; pass++)
        {
            int current = -1;
            for (int i = 0; i < nodes.Count; i++) if (!visited[i] && (current < 0 || distance[i] < distance[current])) current = i;
            if (current < 0 || float.IsPositiveInfinity(distance[current]) || current == 1) break;
            visited[current] = true;
            for (int next = 1; next < nodes.Count; next++)
            {
                if (visited[next] || BlockedEnemyPath(nodes[current], nodes[next], enemy.Radius)) continue;
                float cost = distance[current] + Distance(nodes[current], nodes[next]);
                if (cost < distance[next]) { distance[next] = cost; previous[next] = current; }
            }
        }
        if (previous[1] < 0) return enemy.Pos;
        int step = 1;
        while (previous[step] > 0) step = previous[step];
        return nodes[step];
    }
    private bool BlockedEnemyPath(PointF a, PointF b, float radius)
    {
        int steps = Math.Max(1, (int)(Distance(a, b) / 6));
        for (int i = 0; i <= steps; i++) { float t = i / (float)steps; if (Blocked(new PointF(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t), radius)) return true; }
        return false;
    }
    private bool BlockedLine(PointF a, PointF b)
    {
        int steps = Math.Max(1, (int)(Distance(a, b) / 6));
        RectangleF[] obstacles = CurrentRoomWalls();
        for (int i = 0; i <= steps; i++) { float t = i / (float)steps; PointF p = new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t); if (obstacles.Any(w => w.Contains(p))) return true; }
        return false;
    }
}
