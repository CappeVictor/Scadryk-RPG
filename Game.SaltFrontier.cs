using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;

public sealed partial class Game
{
    // Bits 25–29: mission accepted, three tide bells restored, and the one-time report reward.
    private int saltFrontierProgress
    {
        get => (firstEchoDiscovered >> 25) & 31;
        set => firstEchoDiscovered = (firstEchoDiscovered & ~1040187392) | ((value & 31) << 25);
    }
    private bool saltFrontierReturnLocation;
    private bool saltFrontierBossAwakened;
    private bool saltFrontierBossDefeated;
    private static readonly SizeF SaltFrontierSize = new(4200, 2400);
    private static readonly PointF SaltFrontierEntrance = new(155, 1200);
    private static readonly PointF SaltFrontierCartographer = new(2110, 1175);
    private static readonly PointF[] SaltFrontierTideBells = { new(850, 475), new(2330, 405), new(3570, 1700) };
    private static readonly PointF SaltFrontierBellheart = new(3860, 1885);
    private static readonly PointF SaltBasinFrontierEntrance = new(3440, 1200);
    private static readonly RectangleF[] SaltFrontierWalls =
    {
        new(510, 215, 225, 110), new(1230, 880, 140, 220), new(1770, 1630, 280, 120),
        new(2710, 825, 260, 105), new(3170, 470, 150, 220), new(3860, 1320, 185, 160),
        new(980, 1790, 250, 125), new(2460, 1930, 190, 125)
    };
    private readonly List<Enemy> saltFrontierWaitingEnemies = new();
    private readonly List<Drop> saltFrontierWaitingDrops = new();
    private bool saltFrontierOutsideStored;

    private void SetupSaltFrontier()
    {
        player = SaltFrontierEntrance;
        if (saltFrontierOutsideStored)
        {
            enemies.AddRange(saltFrontierWaitingEnemies); drops.AddRange(saltFrontierWaitingDrops);
            saltFrontierWaitingEnemies.Clear(); saltFrontierWaitingDrops.Clear(); saltFrontierOutsideStored = false;
            return;
        }
        SpawnSaltFrontierBellGuards();
        SpawnSaltFrontierBellheart();
    }

    private void SpawnSaltFrontierBellGuards()
    {
        if ((saltFrontierProgress & 1) == 0) return;
        for (int i = 0; i < SaltFrontierTideBells.Length; i++)
        {
            if ((saltFrontierProgress & (1 << (i + 1))) != 0) continue;
            PointF bell = SaltFrontierTideBells[i];
            (PointF Offset, EnemyKind Kind)[] guardSet =
            {
                (new PointF(-105, -45), i == 1 ? EnemyKind.NameMoth : EnemyKind.SaltEcho),
                (new PointF(105, -35), EnemyKind.Archer),
                (new PointF(0, 105), i == 2 ? EnemyKind.Elite : EnemyKind.Mage)
            };
            foreach (var (offset, kind) in guardSet)
            {
                float hp = EnemyData(kind).HealthAt(42, townEnding) * DifficultyEnemyHealthMultiplier(difficultyTier);
                enemies.Add(new Enemy { Pos = Add(bell, offset), Kind = kind, Hp = hp, MaxHp = hp, Cooldown = 0.9f + (float)random.NextDouble(), SummonCooldown = 6, SupportCooldown = 3, EliteAffix = kind == EnemyKind.Elite ? EliteAffixForRoll(random.Next(6)) : EliteAffix.Armored });
            }
        }
    }

    private void SpawnSaltFrontierBellheart()
    {
        if (!saltFrontierBossAwakened || saltFrontierBossDefeated || enemies.Any(enemy => enemy.NameOverride == "CORO DA MARÉ FALSA")) return;
        float hp = EnemyData(EnemyKind.SaltJailer).HealthAt(42, townEnding) * DifficultyEnemyHealthMultiplier(difficultyTier) * 1.08f;
        enemies.Add(new Enemy { Pos = SaltFrontierBellheart, Kind = EnemyKind.SaltJailer, NameOverride = "CORO DA MARÉ FALSA", Hp = hp, MaxHp = hp, Cooldown = 1.4f, SummonCooldown = 6, SupportCooldown = 3 });
    }

    private void StoreSaltFrontierOutside()
    {
        saltFrontierWaitingEnemies.Clear(); saltFrontierWaitingEnemies.AddRange(enemies);
        saltFrontierWaitingDrops.Clear(); saltFrontierWaitingDrops.AddRange(drops);
        saltFrontierOutsideStored = true;
    }

    private void InteractSaltFrontier()
    {
        Drop? weaponDrop = drops.Where(drop => drop.Kind == "weapon" && Distance(player, drop.Pos) <= 75).OrderBy(drop => Distance(player, drop.Pos)).FirstOrDefault();
        if (weaponDrop is not null) { GameAudio.Play(GameAudio.Cue.Loot); Collect(weaponDrop); return; }
        if (Distance(player, SaltFrontierEntrance) < 105)
        {
            StoreSaltFrontierOutside(); saltFrontierReturnLocation = false; room = 46; SetupRoom(); RestoreSaltBasinOutside(); player = SaltBasinFrontierEntrance;
            ShowMemory("RETORNO À COSTA DAS BADALADAS", "A passagem entre o sal e a maré permanece aberta. As notas que você reuniu continuam marcadas no mapa de Nemi."); return;
        }
        if (Distance(player, SaltFrontierCartographer) < 100)
        {
            if (!saltBasinRewardClaimed) { ShowMemory("NEMI, A ESCUTADORA", "Asha ainda precisa das três lanternas. Quando a névoa abrir, siga a trilha de conchas até a costa."); return; }
            if ((saltFrontierProgress & 1) == 0)
            {
                saltFrontierProgress |= 1; SpawnSaltFrontierBellGuards();
                ShowMemory("NEMI, A ESCUTADORA", "Sou Nemi. A maré traz três badaladas de uma torre que afundou. Cada uma chama viajantes para um banco falso. Silencie os sinos de escuta e me conte o compasso inteiro; não precisa trazer nenhum nome de volta."); return;
            }
            if ((saltFrontierProgress & 14) == 14 && (saltFrontierProgress & 16) == 0)
            {
                saltFrontierProgress |= 16; gold += 175; AddXp(125); health = maxHealth;
                ShowMemory("O COMPASSO DA COSTA", "Nemi compara os três registros e reconhece uma pausa que não pertence à torre: alguém interrompeu a badalada para deixar os viajantes escolherem o próprio caminho. Ela copia a pausa no mapa, sem tentar preencher o silêncio. Sob o juncal, outra voz ainda pulsa no coração da torre submersa. Você recebe 175 de ouro, 125 de experiência e descansa por completo."); return;
            }
            ShowMemory("NEMI, A ESCUTADORA", (saltFrontierProgress & 16) != 0 ? saltFrontierBossDefeated ? "O Coro se desfez sem levar nenhum nome. As barcas agora usam a pausa entre as badaladas como referência, e cada viajante decide por onde seguir." : "As três notas já não chamam viajantes. Resta uma voz presa no coração da torre submersa, ao sul do juncal; ela precisa ser enfrentada para que a costa descanse." : $"Badaladas silenciadas: {System.Numerics.BitOperations.PopCount((uint)(saltFrontierProgress & 14))}/3. Aproxime-se de cada torre depois de limpar seus guardiões."); return;
        }
        for (int i = 0; i < SaltFrontierTideBells.Length; i++)
        {
            PointF bell = SaltFrontierTideBells[i]; if (Distance(player, bell) > 92) continue;
            int mark = 1 << (i + 1);
            if ((saltFrontierProgress & 1) == 0) ShowMemory("TORRE DE ESCUTA", "Nemi precisa registrar o compasso antes que alguém toque o sino.");
            else if ((saltFrontierProgress & mark) != 0) ShowMemory("BADALADA SILENCIADA", "A torre já não chama viajantes para a maré falsa.");
            else if (enemies.Any(enemy => Distance(enemy.Pos, bell) < 205)) ShowMemory("TORRE DE ESCUTA", "Os guardiões abafam o sinal. Limpe o banco antes de tocar o sino.");
            else if (drops.Any(drop => Distance(drop.Pos, bell) < 205)) ShowMemory("TORRE DE ESCUTA", "O saque ainda repousa junto ao sino. Recolha-o antes de registrar a badalada.");
            else
            {
                saltFrontierProgress |= mark; gold += 35; AddXp(35);
                ShowMemory(i switch { 0 => "PRIMEIRA BADALADA • BANCO DE BASALTO", 1 => "SEGUNDA BADALADA • JUNCAL DE VIDRO", _ => "TERCEIRA BADALADA • COSTA SUBMERSA" }, i switch
                {
                    0 => "O sino vibra sem som. A pedra devolve apenas o ritmo dos seus passos; nenhum viajante será atraído para o canal raso. Você recebe 35 de ouro e 35 de experiência.",
                    1 => "A concha rachada deixa passar o vento. Entre duas notas, você percebe um intervalo que Nemi não descreveu. A marca fica no mapa. Você recebe 35 de ouro e 35 de experiência.",
                    _ => "A torre submersa cede ao toque, mas uma pausa permanece entre as ondas. O caminho de volta continua aberto. Você recebe 35 de ouro e 35 de experiência."
                });
            }
            return;
        }
        if (Distance(player, SaltFrontierBellheart) < 125)
        {
            if ((saltFrontierProgress & 31) != 31) { ShowMemory("CORAÇÃO DA TORRE SUBMERSA", "As três badaladas ainda não foram registradas por Nemi."); return; }
            if (saltFrontierBossDefeated) { ShowMemory("O CORAÇÃO EM SILÊNCIO", "A torre não chama mais ninguém. Entre as pedras, resta apenas o intervalo que você escolheu preservar."); return; }
            if (!saltFrontierBossAwakened)
            {
                saltFrontierBossAwakened = true; SpawnSaltFrontierBellheart();
                ShowMemory("O CORO DA MARÉ FALSA", "As notas de Nemi se encontram sob a água. A torre responde com uma voz formada por todos os chamados que os viajantes recusaram. Ela tenta fechar os caminhos; sobreviva ao anel, abandone a marca e atravesse o corte cruzado."); return;
            }
            ShowMemory("O CORO DA MARÉ FALSA", "A voz ainda ecoa no coração submerso. Quebre o compasso e interrompa a chamada."); return;
        }
    }

    private string SaltFrontierObjective()
    {
        if (!saltBasinRewardClaimed) return "COSTA DAS BADALADAS • A ROTA ABRE APÓS AS TRÊS LANTERNAS";
        if ((saltFrontierProgress & 1) == 0) return "OBJETIVO: ENCONTRE NEMI NO OBSERVATÓRIO DA MARÉ";
        if ((saltFrontierProgress & 14) != 14) return $"OBJETIVO: SILENCIE AS TORRES DE ESCUTA • {System.Numerics.BitOperations.PopCount((uint)(saltFrontierProgress & 14))}/3";
        if ((saltFrontierProgress & 16) == 0) return "OBJETIVO: LEVE O COMPASSO COMPLETO A NEMI";
        if (!saltFrontierBossDefeated) return "OBJETIVO: ENFRENTE O CORO NO CORAÇÃO DA TORRE SUBMERSA";
        return "COSTA DAS BADALADAS • COMPASSO REGISTRADO • EXPLORAÇÃO LIVRE";
    }

    private void DrawSaltFrontierEnvironment(Graphics g)
    {
        using var ground = new SolidBrush(Color.FromArgb(43, 60, 66));
        using var basalt = new SolidBrush(Color.FromArgb(55, 67, 70));
        using var marsh = new SolidBrush(Color.FromArgb(41, 78, 77));
        using var tidalFlat = new SolidBrush(Color.FromArgb(111, 120, 106));
        using var road = new Pen(Color.FromArgb(119, 113, 92), 94) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        using var trail = new Pen(Color.FromArgb(202, 179, 130), 2) { DashStyle = DashStyle.Dash };
        using var stone = new SolidBrush(Color.FromArgb(83, 101, 99));
        using var rim = new Pen(Color.FromArgb(143, 169, 156), 3);
        g.FillRectangle(ground, 0, 0, SaltFrontierSize.Width, SaltFrontierSize.Height);
        g.FillPolygon(basalt, new[] { new PointF(0, 0), new(1350, 0), new(1110, 680), new(0, 920) });
        g.FillPolygon(marsh, new[] { new PointF(1250, 0), new(2890, 0), new(2670, 870), new(1330, 1210), new(1080, 730) });
        g.FillPolygon(tidalFlat, new[] { new PointF(2530, 860), new(4200, 650), new(4200, 2400), new(2720, 2400), new(2450, 1720) });
        for (int i = 0; i < 340; i++)
        {
            int x = 29 + i * 127 % 4140, y = 70 + i * 191 % 2260;
            using var fleck = new Pen(x < 1200 ? Color.FromArgb(88, 128, 146, 142) : Color.FromArgb(88, 187, 183, 148), 2);
            g.DrawLine(fleck, x - 7, y + 3, x + 8, y - 5);
        }
        foreach (RectangleF wall in SaltFrontierWalls)
        {
            g.FillEllipse(stone, wall.X - 13, wall.Y - 10, wall.Width + 26, wall.Height + 20);
            g.DrawEllipse(rim, wall.X - 13, wall.Y - 10, wall.Width + 26, wall.Height + 20);
        }
        PointF[][] paths =
        {
            new[] { SaltFrontierEntrance, new PointF(500, 980), SaltFrontierTideBells[0], new PointF(1380, 800), SaltFrontierCartographer },
            new[] { SaltFrontierCartographer, new PointF(2140, 760), SaltFrontierTideBells[1], new PointF(3000, 790), SaltFrontierTideBells[2] }
        };
        foreach (PointF[] path in paths) { g.DrawLines(road, path); g.DrawLines(trail, path); }
        if ((saltFrontierProgress & 31) == 31)
        {
            PointF[] heartPath = { SaltFrontierTideBells[2], new(3750, 1770), SaltFrontierBellheart };
            g.DrawLines(road, heartPath); g.DrawLines(trail, heartPath);
        }
        using var labelFont = new Font("Segoe UI", 14, FontStyle.Bold);
        using var smallFont = new Font("Segoe UI", 10, FontStyle.Bold);
        using var ink = new SolidBrush(Color.FromArgb(231, 224, 198));
        using var observatory = new SolidBrush(Color.FromArgb(56, 68, 68));
        using var wood = new SolidBrush(Color.FromArgb(105, 81, 58));
        using var canvas = new SolidBrush(Color.FromArgb(70, 93, 91));
        g.FillEllipse(observatory, 1935, 1035, 350, 255); g.DrawEllipse(rim, 1935, 1035, 350, 255);
        g.FillRectangle(wood, 2028, 1060, 160, 88); g.DrawRectangle(rim, 2028, 1060, 160, 88);
        g.FillPolygon(wood, new[] { new PointF(2005, 1062), new(2110, 995), new(2210, 1062) });
        g.DrawPolygon(rim, new[] { new PointF(2005, 1062), new(2110, 995), new(2210, 1062) });
        g.FillRectangle(canvas, 2078, 1081, 60, 43); g.DrawRectangle(rim, 2078, 1081, 60, 43);
        g.DrawLine(rim, 2155, 1054, 2235, 1010); g.DrawEllipse(rim, 2218, 994, 34, 34);
        g.DrawString("BANCO DE BASALTO", labelFont, ink, 380, 270);
        g.DrawString("JUNCAL DE VIDRO", labelFont, ink, 1540, 155);
        g.DrawString("COSTA SUBMERSA", labelFont, ink, 3230, 1430);
        for (int i = 0; i < SaltFrontierTideBells.Length; i++)
        {
            PointF p = SaltFrontierTideBells[i];
            using var glow = new SolidBrush(Color.FromArgb(44, 177, 211, 194));
            g.FillEllipse(glow, p.X - 46, p.Y - 46, 92, 92);
            g.DrawEllipse(rim, p.X - 18, p.Y - 26, 36, 52);
            g.DrawArc(rim, p.X - 28, p.Y - 35, 56, 60, 190, 160);
            g.FillEllipse((saltFrontierProgress & (1 << (i + 1))) != 0 ? Brushes.LightCyan : Brushes.Goldenrod, p.X - 5, p.Y - 5, 10, 10);
            if (Distance(player, p) < 95) g.DrawString("E  EXAMINAR BADALADA", smallFont, ink, p.X - 90, p.Y + 38);
        }
        if ((saltFrontierProgress & 31) == 31)
        {
            PointF heart = SaltFrontierBellheart;
            using var tower = new SolidBrush(Color.FromArgb(56, 65, 68));
            PointF[] silhouette = { new(heart.X - 78, heart.Y + 62), new(heart.X - 34, heart.Y - 102), new(heart.X + 17, heart.Y - 142), new(heart.X + 61, heart.Y - 78), new(heart.X + 82, heart.Y + 62) };
            g.FillPolygon(tower, silhouette); g.DrawPolygon(rim, silhouette); g.DrawEllipse(rim, heart.X - 30, heart.Y - 35, 60, 70);
            g.FillEllipse(saltFrontierBossDefeated ? Brushes.LightCyan : Brushes.MediumPurple, heart.X - 8, heart.Y - 8, 16, 16);
            g.DrawString("CORAÇÃO DA TORRE", labelFont, ink, heart.X - 93, heart.Y + 76);
            if (Distance(player, heart) < 120) g.DrawString("E  INVESTIGAR O SINAL", smallFont, ink, heart.X - 94, heart.Y + 102);
        }
        DrawTownPerson(g, SaltFrontierCartographer, "NEMI", Color.FromArgb(77, 133, 139), Distance(player, SaltFrontierCartographer) < InteractionRadius);
        if (Distance(player, SaltFrontierEntrance) < 105) g.DrawString("E  VOLTAR À BACIA", smallFont, ink, 80, 1255);
        if (Distance(player, SaltFrontierCartographer) < InteractionRadius) g.DrawString("E  FALAR", smallFont, ink, SaltFrontierCartographer.X - 40, SaltFrontierCartographer.Y + 45);
    }

    private void DrawSaltFrontierMap(Graphics g)
    {
        using var shade = new SolidBrush(Color.FromArgb(240, 12, 16, 17));
        using var panel = new SolidBrush(Color.FromArgb(247, 21, 24, 24));
        using var line = new Pen(Color.FromArgb(202, 153, 139, 111), 6) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var font = new Font("Segoe UI", 9, FontStyle.Bold);
        using var title = new Font("Georgia", 17, FontStyle.Bold);
        g.FillRectangle(shade, 0, 0, 960, 640); g.FillRectangle(panel, 45, 105, 870, 485); g.DrawRectangle(Pens.DarkGoldenrod, 45, 105, 870, 485);
        g.DrawString("COSTA DAS BADALADAS", title, Brushes.Goldenrod, 327, 121);
        PointF Map(PointF p) => new(100 + p.X * 0.18f, 170 + p.Y * 0.15f);
        PointF[][] paths = { new[] { SaltFrontierEntrance, new PointF(500, 980), SaltFrontierTideBells[0], new PointF(1380, 800), SaltFrontierCartographer }, new[] { SaltFrontierCartographer, new PointF(2140, 760), SaltFrontierTideBells[1], new PointF(3000, 790), SaltFrontierTideBells[2] } };
        foreach (PointF[] path in paths) g.DrawLines(line, path.Select(Map).ToArray());
        if ((saltFrontierProgress & 31) == 31) g.DrawLines(line, new[] { SaltFrontierTideBells[2], new PointF(3750, 1770), SaltFrontierBellheart }.Select(Map).ToArray());
        PointF[] positions = { SaltFrontierEntrance, SaltFrontierCartographer, SaltFrontierTideBells[0], SaltFrontierTideBells[1], SaltFrontierTideBells[2], SaltFrontierBellheart };
        string[] names = { "BACIA", "NEMI", "BASALTO", "JUNCAL", "COSTA", "CORAÇÃO" };
        for (int i = 0; i < positions.Length; i++)
        {
            PointF p = Map(positions[i]);
            bool visible = i switch { 0 or 1 => true, 5 => (saltFrontierProgress & 31) == 31, _ => (saltFrontierProgress & (1 << (i - 1))) != 0 };
            if (!visible) { g.FillEllipse(Brushes.DimGray, p.X - 5, p.Y - 5, 10, 10); continue; }
            bool marked = i >= 2 && i <= 4 && (saltFrontierProgress & (1 << (i - 1))) != 0;
            g.FillEllipse(i == 1 ? Brushes.LightSkyBlue : i == 5 ? saltFrontierBossDefeated ? Brushes.LightCyan : Brushes.MediumPurple : marked ? Brushes.LightGreen : Brushes.Goldenrod, p.X - 6, p.Y - 6, 12, 12);
            g.DrawString(names[i], font, Brushes.AntiqueWhite, p.X - 24, p.Y - 22);
        }
        PointF here = Map(player); g.FillEllipse(Brushes.LightGreen, here.X - 7, here.Y - 7, 14, 14);
        g.DrawString("VERDE: VOCÊ  •  OURO: A DESCOBRIR  •  CIANO: NEMI  •  ROXO: CORAÇÃO  •  M/ESC: FECHAR", font, Brushes.AntiqueWhite, 125, 547);
    }
}
