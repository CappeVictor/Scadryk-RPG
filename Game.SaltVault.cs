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
    private static readonly RectangleF[] SaltVaultReliquaryWalls =
    {
        new(205, 152, 78, 122), new(690, 152, 78, 122), new(205, 390, 78, 122), new(690, 390, 78, 122),
        new(425, 196, 108, 40), new(425, 428, 108, 40)
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
                enemies.Add(new Enemy { Pos = position, Kind = kind, Hp = hp, MaxHp = hp, Cooldown = 0.85f + (float)random.NextDouble(), SummonCooldown = 6, SupportCooldown = 3, EliteAffix = kind == EnemyKind.Elite ? EliteAffixForRoll(random.Next(7)) : EliteAffix.Armored });
            }
        }
        else if (room == 48 && !saltVaultBossDefeated)
        {
            float hp = EnemyData(EnemyKind.SaltJailer).HealthAt(48, townEnding) * DifficultyEnemyHealthMultiplier(difficultyTier);
            enemies.Add(new Enemy { Pos = new PointF(640, 330), Kind = EnemyKind.SaltJailer, Hp = hp, MaxHp = hp, Cooldown = 1.1f, SummonCooldown = 6, SupportCooldown = 3 });
        }
        else if (room == 50 && !saltVaultReliquaryCleared)
        {
            (PointF Position, EnemyKind Kind)[] guardians =
            {
                (new PointF(350, 250), EnemyKind.SaltEcho), (new PointF(630, 250), EnemyKind.NameMoth),
                (new PointF(550, 340), EnemyKind.Elite)
            };
            foreach (var (position, kind) in guardians)
            {
                float hp = EnemyData(kind).HealthAt(50, townEnding) * DifficultyEnemyHealthMultiplier(difficultyTier) * 1.12f;
                EliteAffix affix = kind == EnemyKind.Elite ? EliteAffixForRoll(random.Next(7)) : EliteAffix.Armored;
                enemies.Add(new Enemy { Pos = position, Kind = kind, NameOverride = kind == EnemyKind.Elite ? "SENTINELA DO COFRE FUNDO" : null, Hp = hp, MaxHp = hp, Cooldown = 0.85f + (float)random.NextDouble(), SummonCooldown = 6, SupportCooldown = 3, EliteAffix = affix, Armored = kind == EnemyKind.Elite && affix == EliteAffix.Armored });
            }
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
            if (drops.Count > 0) { notice = "RECOLHA O SAQUE DO CARCEREIRO ANTES DE AVANÇAR"; noticeTime = 2; return; }
            saltVaultLocation = 3; room = 50; SetupRoom();
            ShowMemory("COFRE FUNDO", "A queda do Carcereiro soltou uma trava sob a galeria. Atrás dela, uma sentinela guarda o arquivo que os menires tentaram manter seco."); return;
        }
        if (room == 50 && player.X < 165)
        {
            saltVaultLocation = 2; room = 48; SetupRoom(); return;
        }
        if (room == 50 && player.X > 795)
        {
            if (enemies.Count > 0 || drops.Count > 0) { notice = "LIMPE O COFRE E RECOLHA O SAQUE ANTES DE SAIR"; noticeTime = 2; return; }
            if (!saltVaultReliquaryCleared)
            {
                saltVaultReliquaryCleared = true; gold += 260; AddXp(190); tideGlass = Math.Min(999, tideGlass + 3); potions++;
                ShowMemory("ARQUIVO DOS MENIRES", "A sentinela cede e o sal deixa aparecer registros de barqueiros que usavam o leito apenas quando a corrente baixava. Nenhum nome foi gravado. Você recebe 260 de ouro, 190 de experiência, 3 sais vítreos e uma poção.");
            }
            saltVaultLocation = 0; room = 46; SetupRoom(); RestoreSaltBasinOutside(); player = SaltVaultEntrance; return;
        }
    }

    private void DrawSaltVaultEnvironment(Graphics g)
    {
        bool bossRoom = room == 48, reliquaryRoom = room == 50;
        using var floor = new SolidBrush(bossRoom ? Color.FromArgb(39, 43, 48) : reliquaryRoom ? Color.FromArgb(37, 48, 47) : Color.FromArgb(43, 47, 43));
        using var seam = new Pen(bossRoom ? Color.FromArgb(67, 91, 100) : reliquaryRoom ? Color.FromArgb(61, 107, 101) : Color.FromArgb(77, 79, 65), 2);
        using var wall = new SolidBrush(Color.FromArgb(76, 80, 72));
        using var edge = new Pen(Color.FromArgb(158, 143, 108), 3);
        g.FillRectangle(floor, 65, 108, 830, 445);
        for (int y = 124; y < 548; y += 54)
        {
            g.DrawLine(seam, 70, y, 890, y);
            for (int x = 110 + ((y / 54) % 2) * 36; x < 880; x += 78) g.DrawLine(seam, x, y, x + 18, y + 46);
        }
        RectangleF[] obstacles = bossRoom ? SaltVaultBossRoomWalls : reliquaryRoom ? SaltVaultReliquaryWalls : SaltVaultAntechamberWalls;
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
        if (reliquaryRoom)
        {
            using var seal = new Pen(Color.FromArgb(130, 158, 211, 194), 4);
            using var vein = new Pen(Color.FromArgb(65, 174, 207, 174), 2);
            g.DrawEllipse(seal, 345, 184, 270, 280); g.DrawEllipse(vein, 382, 220, 196, 205);
            g.DrawLine(vein, 480, 190, 480, 460); g.DrawLine(vein, 350, 324, 610, 324);
        }
        using var label = new Font("Segoe UI", 11, FontStyle.Bold);
        string name = bossRoom ? "CÂMARA DO SAL EM SUSPENSO" : reliquaryRoom ? "ARQUIVO DOS MENIRES" : "GALERIA DOS MENIRES";
        g.DrawString(name, label, Brushes.AntiqueWhite, bossRoom ? 340 : reliquaryRoom ? 370 : 370, 125);
        if (player.X < 165) g.DrawString(room == 47 ? "E  SUBIR À BACIA" : bossRoom ? "E  VOLTAR À GALERIA" : "E  VOLTAR À CÂMARA", label, Brushes.AntiqueWhite, 72, 385);
        if (player.X > 795) g.DrawString(room == 47 ? "E  DESCER À CÂMARA" : bossRoom ? "E  AVANÇAR AO COFRE" : "E  RETORNAR À BACIA", label, Brushes.AntiqueWhite, 720, 385);
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
        PointF upper = new(190, 335), middle = new(480, 335), lower = new(770, 335);
        g.DrawLine(line, upper, middle);
        if (saltVaultBossDefeated) g.DrawLine(line, middle, lower);
        g.DrawLine(thin, 280, 298, 680, 298); g.DrawLine(thin, 280, 370, 680, 370);
        g.FillEllipse(room == 47 ? Brushes.LightGreen : saltVaultAntechamberCleared ? Brushes.SlateGray : Brushes.DimGray, upper.X - 9, upper.Y - 9, 18, 18);
        g.FillEllipse(room == 48 ? Brushes.LightGreen : saltVaultBossDefeated ? Brushes.SlateGray : Brushes.MediumPurple, middle.X - 9, middle.Y - 9, 18, 18);
        g.FillEllipse(room == 50 ? Brushes.LightGreen : saltVaultReliquaryCleared ? Brushes.SlateGray : saltVaultBossDefeated ? Brushes.Goldenrod : Brushes.DimGray, lower.X - 9, lower.Y - 9, 18, 18);
        g.DrawString("47 • GALERIA DOS MENIRES", font, Brushes.AntiqueWhite, 120, 250);
        g.DrawString("48 • CÂMARA DO SAL", font, Brushes.AntiqueWhite, 360, 250);
        if (saltVaultBossDefeated || room == 50 || saltVaultReliquaryCleared) g.DrawString("50 • ARQUIVO DOS MENIRES", font, Brushes.AntiqueWhite, 630, 250);
        else g.DrawString("???", font, Brushes.DimGray, lower.X - 10, 250);
        g.DrawString("VERDE: VOCÊ   •   CINZA: LIMPA   •   M/ESC: FECHAR", font, Brushes.AntiqueWhite, 285, 540);
    }
}
