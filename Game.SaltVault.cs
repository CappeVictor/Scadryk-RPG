using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;

public sealed partial class Game
{
    private static readonly RectangleF[] SaltVaultAntechamberWalls =
    {
        new(250, 150, 85, 100), new(440, 480, 85, 35), new(595, 150, 85, 65), new(720, 475, 65, 40)
    };
    private static readonly RectangleF[] SaltVaultBossRoomWalls =
    {
        new(235, 155, 80, 100), new(645, 155, 80, 100), new(235, 405, 80, 100), new(645, 405, 80, 100)
    };

    private void SetupSaltVaultRoom()
    {
        player = new PointF(150, 330);
        if (room == 47 && !saltVaultAntechamberCleared)
        {
            (PointF Position, EnemyKind Kind)[] foes =
            {
                (new PointF(360, 230), EnemyKind.SaltEcho), (new PointF(500, 430), EnemyKind.Archer),
                (new PointF(650, 230), EnemyKind.NameMoth), (new PointF(770, 420), EnemyKind.Elite)
            };
            foreach (var (position, kind) in foes)
            {
                float hp = EnemyData(kind).HealthAt(47, townEnding) * DifficultyEnemyHealthMultiplier(difficultyTier);
                enemies.Add(new Enemy { Pos = position, Kind = kind, Hp = hp, MaxHp = hp, Cooldown = 0.85f + (float)random.NextDouble(), SummonCooldown = 6, SupportCooldown = 3, EliteAffix = kind == EnemyKind.Elite ? EliteAffixForRoll(random.Next(6)) : EliteAffix.Armored });
            }
        }
        else if (room == 48 && !saltVaultBossDefeated)
        {
            float hp = EnemyData(EnemyKind.SaltJailer).HealthAt(48, townEnding) * DifficultyEnemyHealthMultiplier(difficultyTier);
            enemies.Add(new Enemy { Pos = new PointF(640, 330), Kind = EnemyKind.SaltJailer, Hp = hp, MaxHp = hp, Cooldown = 1.1f, SummonCooldown = 6, SupportCooldown = 3 });
        }
    }

    private void InteractSaltVault()
    {
        Drop? weaponDrop = drops.Where(drop => drop.Kind == "weapon" && Distance(player, drop.Pos) <= 75).OrderBy(drop => Distance(player, drop.Pos)).FirstOrDefault();
        if (weaponDrop is not null) { GameAudio.Play(GameAudio.Cue.Loot); Collect(weaponDrop); return; }
        if (room == 47 && player.X < 165)
        {
            if (enemies.Count > 0 || drops.Count > 0) { notice = "LIMPE A GALERIA E RECOLHA O SAQUE"; noticeTime = 2; return; }
            saltVaultAntechamberCleared = true; saltVaultLocation = 0; room = 46; SetupRoom(); RestoreSaltBasinOutside(); player = SaltVaultEntrance; return;
        }
        if (room == 47 && player.X > 795)
        {
            if (enemies.Count > 0 || drops.Count > 0) { notice = "A PASSAGEM SÓ ABRE APÓS LIMPAR A GALERIA E RECOLHER O SAQUE"; noticeTime = 2; return; }
            saltVaultAntechamberCleared = true; saltVaultLocation = 2; room = 48; SetupRoom(); return;
        }
        if (room == 48 && player.X < 165)
        {
            if (drops.Count > 0) { notice = "RECOLHA O SAQUE ANTES DE VOLTAR"; noticeTime = 2; return; }
            saltVaultLocation = 1; room = 47; SetupRoom(); return;
        }
        if (room == 48 && player.X > 795)
        {
            if (!saltVaultBossDefeated) { notice = "A FENDA PERMANECE SELADA PELO CARCEREIRO"; noticeTime = 2; return; }
            if (drops.Count > 0) { notice = "RECOLHA O SAQUE DO CARCEREIRO ANTES DE SAIR"; noticeTime = 2; return; }
            saltVaultLocation = 0; room = 46; SetupRoom(); RestoreSaltBasinOutside(); player = SaltVaultEntrance; return;
        }
    }

    private void DrawSaltVaultEnvironment(Graphics g)
    {
        bool bossRoom = room == 48;
        using var floor = new SolidBrush(bossRoom ? Color.FromArgb(39, 43, 48) : Color.FromArgb(43, 47, 43));
        using var seam = new Pen(bossRoom ? Color.FromArgb(67, 91, 100) : Color.FromArgb(77, 79, 65), 2);
        using var wall = new SolidBrush(Color.FromArgb(76, 80, 72));
        using var edge = new Pen(Color.FromArgb(158, 143, 108), 3);
        g.FillRectangle(floor, 65, 108, 830, 445);
        for (int y = 124; y < 548; y += 54)
        {
            g.DrawLine(seam, 70, y, 890, y);
            for (int x = 110 + ((y / 54) % 2) * 36; x < 880; x += 78) g.DrawLine(seam, x, y, x + 18, y + 46);
        }
        RectangleF[] obstacles = bossRoom ? SaltVaultBossRoomWalls : SaltVaultAntechamberWalls;
        foreach (RectangleF obstacle in obstacles)
        {
            g.FillEllipse(wall, obstacle.X - 10, obstacle.Y - 8, obstacle.Width + 20, obstacle.Height + 16);
            g.DrawEllipse(edge, obstacle.X - 10, obstacle.Y - 8, obstacle.Width + 20, obstacle.Height + 16);
        }
        if (bossRoom)
        {
            using var ring = new Pen(Color.FromArgb(95, 165, 205, 199), 5);
            using var inner = new Pen(Color.FromArgb(65, 189, 156, 111), 2) { DashStyle = DashStyle.Dash };
            g.DrawEllipse(ring, 360, 190, 240, 280); g.DrawEllipse(inner, 390, 220, 180, 220);
        }
        using var label = new Font("Segoe UI", 11, FontStyle.Bold);
        string name = bossRoom ? "CÂMARA DO SAL EM SUSPENSO" : "GALERIA DOS MENIRES";
        g.DrawString(name, label, Brushes.AntiqueWhite, bossRoom ? 340 : 370, 125);
        if (player.X < 165) g.DrawString(bossRoom ? "E  VOLTAR À GALERIA" : "E  SUBIR À BACIA", label, Brushes.AntiqueWhite, 72, 385);
        if (player.X > 795) g.DrawString(bossRoom ? "E  SAIR PARA A BACIA" : "E  DESCER À CÂMARA", label, Brushes.AntiqueWhite, 720, 385);
    }

    private void DrawSaltVaultMap(Graphics g)
    {
        using var shade = new SolidBrush(Color.FromArgb(240, 12, 16, 17));
        using var panel = new SolidBrush(Color.FromArgb(247, 21, 24, 24));
        using var line = new Pen(Color.FromArgb(202, 153, 139, 111), 6) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var thin = new Pen(Color.FromArgb(110, 115, 122, 112), 2);
        using var font = new Font("Segoe UI", 10, FontStyle.Bold);
        using var title = new Font("Georgia", 17, FontStyle.Bold);
        g.FillRectangle(shade, 0, 0, 960, 640); g.FillRectangle(panel, 45, 105, 870, 485);
        g.DrawRectangle(Pens.DarkGoldenrod, 45, 105, 870, 485);
        g.DrawString("FENDA DOS MENIRES", title, Brushes.Goldenrod, 342, 121);
        PointF upper = new(250, 335), lower = new(710, 335);
        g.DrawLine(line, upper, lower);
        g.DrawLine(thin, 280, 298, 680, 298); g.DrawLine(thin, 280, 370, 680, 370);
        g.FillEllipse(room == 47 ? Brushes.LightGreen : Brushes.DimGray, upper.X - 9, upper.Y - 9, 18, 18);
        g.FillEllipse(room == 48 ? Brushes.LightGreen : saltVaultBossDefeated ? Brushes.SlateGray : Brushes.MediumPurple, lower.X - 9, lower.Y - 9, 18, 18);
        g.DrawString("47 • GALERIA DOS MENIRES", font, Brushes.AntiqueWhite, 120, 250);
        g.DrawString("48 • CÂMARA DO SAL", font, Brushes.AntiqueWhite, 590, 250);
        g.DrawString("VERDE: VOCÊ   •   CINZA: LIMPA   •   M/ESC: FECHAR", font, Brushes.AntiqueWhite, 285, 540);
    }
}
