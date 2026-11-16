using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Collections.Generic;

public sealed partial class Game
{
    // Bit 0 accepts Asha's request; bits 1-3 mark the three restored lanterns;
    // bit 4 records the one-time reward. This lives in unused upper discovery bits.
    private int saltBasinProgress
    {
        get => (firstEchoDiscovered >> 7) & 31;
        set => firstEchoDiscovered = (firstEchoDiscovered & ~3968) | ((value & 31) << 7);
    }
    private int saltBasinSurveyProgress
    {
        get => (firstEchoDiscovered >> 16) & 31;
        set => firstEchoDiscovered = (firstEchoDiscovered & ~2031616) | ((value & 31) << 16);
    }
    private int saltBasinMarks => (saltBasinProgress >> 1) & 7;
    private bool saltBasinMissionAccepted => (saltBasinProgress & 1) != 0;
    private bool saltBasinRewardClaimed => (saltBasinProgress & 16) != 0;
    private int saltVaultLocation
    {
        get => (firstEchoDiscovered >> 21) & 3;
        set => firstEchoDiscovered = (firstEchoDiscovered & ~6291456) | ((value & 3) << 21);
    }
    private bool saltVaultAntechamberCleared { get => (firstEchoDiscovered & 8388608) != 0; set => firstEchoDiscovered = value ? firstEchoDiscovered | 8388608 : firstEchoDiscovered & ~8388608; }
    private bool saltVaultBossDefeated { get => (firstEchoDiscovered & 16777216) != 0; set => firstEchoDiscovered = value ? firstEchoDiscovered | 16777216 : firstEchoDiscovered & ~16777216; }
    private bool saltVaultReliquaryCleared;
    private static readonly SizeF SaltBasinSize = new(3600, 2100);
    private static readonly PointF SaltBasinEntrance = new(165, 1050);
    private static readonly PointF SaltBasinAsha = new(1690, 910);
    private static readonly PointF SaltBasinTavi = new(1830, 1035);
    private static readonly PointF[] SaltBasinLanterns = { new(640, 420), new(2010, 350), new(2890, 1450) };
    private static readonly PointF[] SaltBasinSurveyMarks = { new(930, 740), new(2260, 720), new(3130, 1570) };
    private static readonly PointF SaltVaultEntrance = new(3420, 1840);
    private readonly List<Enemy> saltBasinWaitingEnemies = new();
    private readonly List<Drop> saltBasinWaitingDrops = new();
    private bool saltBasinOutsideStored;

    private void SpawnSaltBasinEnemies()
    {
        foreach (var (position, kind, hp) in new[]
        {
            (new PointF(920, 890), EnemyKind.Grunt, 95f), (new PointF(1240, 650), EnemyKind.Archer, 82f),
            (new PointF(2440, 930), EnemyKind.NameMoth, 112f), (new PointF(3300, 610), EnemyKind.Grunt, 105f)
        })
        {
            float scaledHp = hp * DifficultyEnemyHealthMultiplier(difficultyTier);
            enemies.Add(new Enemy { Pos = position, Kind = kind, Hp = scaledHp, MaxHp = scaledHp, Cooldown = 0.8f + (float)random.NextDouble(), SummonCooldown = 6, SupportCooldown = 3 });
        }
        if (saltBasinMissionAccepted) SpawnSaltBasinLanternGuardians();
    }

    private void SpawnSaltBasinLanternGuardians()
    {
        var guardians = new (int Mark, PointF Position, EnemyKind Kind, float Hp)[][]
        {
            new[] { (2, new PointF(510, 390), EnemyKind.Archer, 96f), (2, new PointF(770, 455), EnemyKind.Grunt, 110f), (2, new PointF(655, 570), EnemyKind.NameMoth, 120f) },
            new[] { (4, new PointF(1850, 340), EnemyKind.Grunt, 118f), (4, new PointF(2130, 435), EnemyKind.Archer, 100f), (4, new PointF(1990, 170), EnemyKind.NameMoth, 128f) },
            new[] { (8, new PointF(2730, 1400), EnemyKind.Archer, 108f), (8, new PointF(3020, 1320), EnemyKind.NameMoth, 132f), (8, new PointF(2995, 1610), EnemyKind.Elite, 235f) }
        };
        foreach (var group in guardians)
        {
            if ((saltBasinProgress & group[0].Mark) != 0) continue;
            foreach (var (_, position, kind, hp) in group)
            {
                float scaledHp = hp * DifficultyEnemyHealthMultiplier(difficultyTier);
                enemies.Add(new Enemy { Pos = position, Kind = kind, Hp = scaledHp, MaxHp = scaledHp, Cooldown = 0.9f + (float)random.NextDouble(), SummonCooldown = 6, SupportCooldown = 3 });
            }
        }
    }

    private void SetupSaltBasin()
    {
        player = SaltBasinEntrance;
        if (!saltBasinOutsideStored) SpawnSaltBasinEnemies();
    }

    private void StoreSaltBasinOutside()
    {
        saltBasinWaitingEnemies.Clear(); saltBasinWaitingEnemies.AddRange(enemies);
        saltBasinWaitingDrops.Clear(); saltBasinWaitingDrops.AddRange(drops);
        saltBasinOutsideStored = true;
    }

    private void RestoreSaltBasinOutside()
    {
        if (!saltBasinOutsideStored) return;
        enemies.Clear(); enemies.AddRange(saltBasinWaitingEnemies);
        drops.Clear(); drops.AddRange(saltBasinWaitingDrops);
        saltBasinWaitingEnemies.Clear(); saltBasinWaitingDrops.Clear();
        saltBasinOutsideStored = false;
    }

    private void InteractSaltBasin()
    {
        Drop? weaponDrop = drops.Where(drop => drop.Kind == "weapon" && Distance(player, drop.Pos) <= 75).OrderBy(drop => Distance(player, drop.Pos)).FirstOrDefault();
        if (weaponDrop is not null) { GameAudio.Play(GameAudio.Cue.Loot); Collect(weaponDrop); return; }
        if (Distance(player, SaltBasinFrontierEntrance) < 105)
        {
            if (!saltBasinRewardClaimed) { ShowMemory("TRILHA DAS CONCHAS", "A corrente ainda não alcança a costa. Ajude Asha a reacender as três lanternas do sal para abrir o caminho."); return; }
            StoreSaltBasinOutside(); saltFrontierReturnLocation = true; room = 49; SetupRoom();
            ShowMemory("A COSTA DAS BADALADAS", "A trilha atravessa o banco de sal e desce até uma costa onde a maré repete três notas. Um observatório permanece aceso entre o juncal e a maré recuada."); return;
        }
        if (Distance(player, SaltVaultEntrance) < 100)
        {
            if ((saltBasinSurveyProgress & 16) == 0) { ShowMemory("FENDA SOB OS MENIRES", "Os três sinais de Tavi formam um traçado que aponta para uma abertura sob a baixada. O caminho ainda está coberto de sal endurecido."); return; }
            StoreSaltBasinOutside(); saltVaultLocation = 1; room = 47; SetupRoom();
            ShowMemory("A FENDA DOS MENIRES", "As marcas registradas se alinham na pedra. Uma escada desce para as galerias soterradas sob o sal."); return;
        }
        if (Distance(player, SaltBasinEntrance) < 100)
        {
            StoreSaltBasinOutside(); saltBasinReturnLocation = false; room = 43; SetupRoom(); RestoreFirstEchoOutside(); player = FirstEchoAshSignal;
            ShowMemory("REGRESSO AO VAU", "A barca de Nilo aguarda no sinal ciano. O abrigo e as lanternas da bacia continuam no mapa."); return;
        }
        if (Distance(player, SaltBasinAsha) < 105)
        {
            if (!saltBasinMissionAccepted)
            {
                saltBasinProgress |= 1; SpawnSaltBasinLanternGuardians();
                ShowMemory("ASHA, GUARDIÃ DO ABRIGO", "A névoa de sal cobriu as trilhas quando as três lanternas se apagaram. Acenda os marcos do oeste, do bosque de vidro e da baixada; cada luz revela o caminho seguinte. O abrigo fica aqui, entre as rotas.");
            }
            else if (saltBasinMarks == 7 && !saltBasinRewardClaimed)
            {
                saltBasinProgress |= 16; gold += 160; AddXp(90); health = maxHealth;
                ShowMemory("AS TRÊS LANTERNAS", "Asha acompanha as luzes se encontrando sobre a bacia. A névoa se abre em trilhas seguras e viajantes voltam a encontrar o abrigo. Você recebe 160 de ouro, 90 de experiência e descansa por completo.");
            }
            else if (saltBasinRewardClaimed)
            {
                health = maxHealth;
                ShowMemory("ABRIGO DAS TRÊS LANTERNAS", "Asha mantém o abrigo aquecido e oferece descanso. As três rotas seguem iluminadas.");
            }
            else ShowMemory("ASHA, GUARDIÃ DO ABRIGO", $"A bacia ainda some na névoa. Lanternas restauradas: {System.Numerics.BitOperations.PopCount((uint)saltBasinMarks)}/3. Siga os caminhos marcados no mapa; cada marco está guardado por ecos.");
            return;
        }
        if (Distance(player, SaltBasinTavi) < 85)
        {
            if (!saltBasinMissionAccepted) ShowMemory("TAVI, APRENDIZ DE CARTÓGRAFA", "Sou Tavi. Asha cuida do abrigo e conhece as três rotas. As lanternas ficam em colinas, não dentro de masmorras.");
            else if (!saltBasinRewardClaimed) ShowMemory("TAVI, APRENDIZ DE CARTÓGRAFA", "As marcas de Asha foram talhadas para quem chega cansado. Reacenda cada uma e a própria bacia vai revelar o caminho.");
            else if ((saltBasinSurveyProgress & 1) == 0)
            {
                saltBasinSurveyProgress |= 1;
                ShowMemory("O MAPA INACABADO DE TAVI", "Asha reabriu as trilhas, mas três marcos antigos ainda faltam no meu mapa. Encontre os menires gravados no espigão, no bosque e na baixada; toque cada um para registrar o caminho.");
            }
            else if ((saltBasinSurveyProgress & 14) == 14 && (saltBasinSurveyProgress & 16) == 0)
            {
                saltBasinSurveyProgress |= 16; gold += 110; AddXp(70);
                ShowMemory("MAPA DAS TRÊS ROTAS", "Tavi liga os três traços num mapa seguro. Viajantes agora podem atravessar a bacia sem se perder na névoa. Você recebe 110 de ouro e 70 de experiência.");
            }
            else if ((saltBasinSurveyProgress & 16) != 0) ShowMemory("TAVI, CARTÓGRAFA", saltVaultBossDefeated ? "As rotas estão registradas e a fenda sob os menires foi silenciada. Deixo o mapa aberto para quem ainda não conhece a bacia." : "As rotas estão registradas. Três marcas também apontam para uma fenda sob a baixada; talvez algo tenha ficado preso lá embaixo.");
            else ShowMemory("O MAPA INACABADO DE TAVI", $"Marcos registrados: {System.Numerics.BitOperations.PopCount((uint)(saltBasinSurveyProgress & 14))}/3. Procure menires gravados longe da trilha principal.");
            return;
        }
        for (int i = 0; i < SaltBasinSurveyMarks.Length; i++)
        {
            if (Distance(player, SaltBasinSurveyMarks[i]) >= 75) continue;
            int mark = 1 << (i + 1);
            if ((saltBasinSurveyProgress & 1) == 0) ShowMemory("MENIR SEM REGISTRO", "Tavi procura marcos antigos para completar seu mapa. Volte ao abrigo e fale com ela depois de ajudar Asha.");
            else if ((saltBasinSurveyProgress & mark) != 0) ShowMemory("MARCO CARTOGRAFADO", "O sinal já está registrado no mapa de Tavi.");
            else
            {
                saltBasinSurveyProgress |= mark;
                ShowMemory(i switch { 0 => "MENIR DO ESPIGÃO", 1 => "MENIR DO BOSQUE DE VIDRO", _ => "MENIR DA BAIXADA" }, "Você registra os sulcos de orientação. O marco agora aparece no mapa da região.");
            }
            return;
        }
        for (int i = 0; i < SaltBasinLanterns.Length; i++)
        {
            PointF lantern = SaltBasinLanterns[i];
            if (Distance(player, lantern) >= 85) continue;
            int mark = 1 << (i + 1);
            if (!saltBasinMissionAccepted) ShowMemory("LANTERNA DO SAL", "A inscrição pede três vozes antes de reacender. Fale com Asha no abrigo.");
            else if ((saltBasinProgress & mark) != 0) ShowMemory("LANTERNA ACESA", "A luz atravessa a névoa e revela o próximo trecho da bacia.");
            else if (enemies.Any(enemy => Distance(enemy.Pos, lantern) < 205)) ShowMemory("LANTERNA DO SAL", "Os ecos sufocam a chama. Limpe o marco antes de tocá-lo.");
            else
            {
                saltBasinProgress |= mark; gold += 25; AddXp(25);
                ShowMemory(i switch { 0 => "LANTERNA DO ESPIGÃO", 1 => "LANTERNA DO BOSQUE DE VIDRO", _ => "LANTERNA DA BAIXADA BRANCA" }, "A chama pálida atravessa a névoa salgada. O mapa mostra a próxima rota. Você recebe 25 de ouro e 25 de experiência.");
            }
            return;
        }
    }

    private string SaltBasinObjective()
    {
        if (!saltBasinMissionAccepted) return "OBJETIVO: FALE COM ASHA NO ABRIGO DAS TRÊS LANTERNAS";
        if (saltBasinMarks != 7) return $"OBJETIVO: REACENDA AS LANTERNAS DA BACIA • {System.Numerics.BitOperations.PopCount((uint)saltBasinMarks)}/3";
        if (!saltBasinRewardClaimed) return "OBJETIVO: VOLTE AO ABRIGO E CONTE A ASHA";
        if ((saltBasinSurveyProgress & 1) == 0) return "OPCIONAL: FALE COM TAVI SOBRE O MAPA INACABADO";
        if ((saltBasinSurveyProgress & 14) != 14) return $"OPCIONAL: REGISTRE OS MENIRES DE TAVI • {System.Numerics.BitOperations.PopCount((uint)(saltBasinSurveyProgress & 14))}/3";
        if ((saltBasinSurveyProgress & 16) == 0) return "OPCIONAL: VOLTE A TAVI COM O MAPA COMPLETO";
        if (!saltVaultBossDefeated) return "OPCIONAL: INVESTIGUE A FENDA SOB A BAIXADA";
        if ((saltFrontierProgress & 16) == 0) return "OPCIONAL: SIGA A TRILHA DAS CONCHAS ATÉ A COSTA";
        return "BACIA DO SAL IMÓVEL • ROTAS ABERTAS • EXPLORAÇÃO LIVRE";
    }

    private void DrawSaltBasinMap(Graphics g)
    {
        using var shade = new SolidBrush(Color.FromArgb(240, 12, 16, 17));
        using var route = new Pen(Color.FromArgb(176, 153, 139, 111), 4);
        using var thin = new Pen(Color.FromArgb(140, 121, 148, 147), 3);
        using var font = new Font("Segoe UI", 9, FontStyle.Bold);
        using var title = new Font("Georgia", 17, FontStyle.Bold);
        g.FillRectangle(shade, 35, 95, 890, 500);
        g.DrawString("BACIA DO SAL IMÓVEL", title, Brushes.Goldenrod, 330, 111);
        PointF Map(PointF p) => new(70 + p.X * 0.24f, 155 + p.Y * 0.195f);
        PointF[][] trails =
        {
            new[] { SaltBasinEntrance, new PointF(430, 850), SaltBasinLanterns[0], new PointF(1120, 490), SaltBasinAsha },
            new[] { SaltBasinEntrance, new PointF(490, 1350), new PointF(1000, 1530), new PointF(1450, 1420), SaltBasinAsha },
            new[] { SaltBasinAsha, new PointF(1900, 650), SaltBasinLanterns[1], new PointF(2430, 520), SaltBasinLanterns[2] },
            new[] { SaltBasinAsha, new PointF(2240, 1110), new PointF(2590, 1320), SaltBasinLanterns[2] }
        };
        for (int i = 0; i < trails.Length; i++)
        {
            if (i >= 2 && !saltBasinMissionAccepted) continue;
            g.DrawLines(i == 0 || i == 3 ? route : thin, trails[i].Select(Map).ToArray());
        }
        if ((saltBasinSurveyProgress & 16) != 0) g.DrawLines(route, new[] { Map(SaltBasinLanterns[2]), Map(new PointF(3200, 1670)), Map(SaltVaultEntrance) });
        if (saltBasinRewardClaimed) g.DrawLines(thin, new[] { Map(SaltBasinLanterns[2]), Map(new PointF(3180, 1540)), Map(SaltBasinFrontierEntrance) });
        foreach (var (position, label, visible) in new[]
        {
            (SaltBasinEntrance, "VAU DE CINZA", true), (SaltBasinAsha, "ASHA • ABRIGO", true),
            (SaltBasinTavi, "TAVI", true),
            (SaltBasinLanterns[0], (saltBasinProgress & 2) != 0 ? "ESPIGÃO" : "???", saltBasinMissionAccepted),
            (SaltBasinLanterns[1], (saltBasinProgress & 4) != 0 ? "BOSQUE DE VIDRO" : "???", saltBasinMissionAccepted),
            (SaltBasinLanterns[2], (saltBasinProgress & 8) != 0 ? "BAIXADA" : "???", saltBasinMissionAccepted)
        })
        {
            PointF p = Map(position);
            bool isLantern = position == SaltBasinLanterns[0] || position == SaltBasinLanterns[1] || position == SaltBasinLanterns[2];
            bool lit = isLantern && (saltBasinProgress & (position == SaltBasinLanterns[0] ? 2 : position == SaltBasinLanterns[1] ? 4 : 8)) != 0;
            if (visible) { g.FillEllipse(lit ? Brushes.LightCyan : Brushes.Goldenrod, p.X - 5, p.Y - 5, 10, 10); g.DrawString(label, font, Brushes.AntiqueWhite, p.X - 29, p.Y - 22); }
        }
        for (int i = 0; i < SaltBasinSurveyMarks.Length; i++)
        {
            if ((saltBasinSurveyProgress & (1 << (i + 1))) == 0) continue;
            PointF p = Map(SaltBasinSurveyMarks[i]);
            g.FillRectangle(Brushes.LightSkyBlue, p.X - 4, p.Y - 4, 8, 8);
            g.DrawString($"MENIR {i + 1}", font, Brushes.LightSkyBlue, p.X + 7, p.Y - 7);
        }
        if ((saltBasinSurveyProgress & 16) != 0)
        {
            PointF p = Map(SaltVaultEntrance);
            g.FillRectangle(saltVaultBossDefeated ? Brushes.SlateGray : Brushes.MediumPurple, p.X - 5, p.Y - 5, 10, 10);
            g.DrawString(saltVaultBossDefeated ? "FENDA SILENCIOSA" : "FENDA SOB A BAIXADA", font, Brushes.Plum, p.X - 73, p.Y + 8);
        }
        if (saltBasinRewardClaimed)
        {
            PointF p = Map(SaltBasinFrontierEntrance);
            g.FillEllipse((saltFrontierProgress & 16) != 0 ? Brushes.LightGreen : Brushes.LightSkyBlue, p.X - 5, p.Y - 5, 10, 10);
            g.DrawString((saltFrontierProgress & 16) != 0 ? "COSTA MAPEADA" : "TRILHA DAS CONCHAS", font, Brushes.LightSkyBlue, p.X - 62, p.Y - 24);
        }
        bool showObjective = !saltBasinMissionAccepted || saltBasinMarks != 7 || !saltBasinRewardClaimed || (saltBasinSurveyProgress & 16) == 0 || !saltVaultBossDefeated || (saltFrontierProgress & 16) == 0;
        PointF objective = !saltBasinMissionAccepted ? Map(SaltBasinAsha)
            : saltBasinMarks != 7 ? Map(SaltBasinLanterns[Enumerable.Range(0, 3).First(i => (saltBasinProgress & (1 << (i + 1))) == 0)])
            : !saltBasinRewardClaimed ? Map(SaltBasinAsha)
            : !saltVaultBossDefeated ? Map(SaltVaultEntrance)
            : (saltFrontierProgress & 16) == 0 ? Map(SaltBasinFrontierEntrance)
            : (saltBasinSurveyProgress & 16) != 0 ? Map(player)
            : (saltBasinSurveyProgress & 1) == 0 || (saltBasinSurveyProgress & 14) == 14 ? Map(SaltBasinTavi)
            : Map(SaltBasinSurveyMarks[Enumerable.Range(0, 3).First(i => (saltBasinSurveyProgress & (1 << (i + 1))) == 0)]);
        using var ring = new Pen(Color.FromArgb(255, 218, 111), 3);
        if (showObjective) g.DrawEllipse(ring, objective.X - 12, objective.Y - 12, 24, 24);
        PointF here = Map(player);
        g.FillEllipse(Brushes.LightGreen, here.X - 7, here.Y - 7, 14, 14);
        string status = !saltBasinMissionAccepted ? "PROCURE ASHA"
            : $"LANTERNAS {System.Numerics.BitOperations.PopCount((uint)saltBasinMarks)}/3" + ((saltBasinSurveyProgress & 1) != 0 ? $" • MENIRES {System.Numerics.BitOperations.PopCount((uint)(saltBasinSurveyProgress & 14))}/3" : "");
        g.DrawString(status, font, Brushes.Goldenrod, 397, 531);
        g.DrawString("VERDE: VOCÊ  •  CÍRCULO: OBJETIVO  •  AZUL: MENIR REGISTRADO  •  M/ESC: FECHAR", font, Brushes.AntiqueWhite, 123, 562);
    }

    private static readonly RectangleF[] SaltBasinWalls =
    {
        new(345, 270, 135, 155), new(1020, 180, 180, 135), new(2370, 170, 190, 205),
        new(850, 1055, 170, 190), new(2300, 1350, 220, 135), new(3190, 870, 210, 185),
        new(1590, 810, 105, 80)
    };

    private void DrawSaltBasinEnvironment(Graphics g)
    {
        using var ground = new SolidBrush(Color.FromArgb(76, 81, 75));
        using var saltFlat = new SolidBrush(Color.FromArgb(117, 119, 103));
        using var glassGrove = new SolidBrush(Color.FromArgb(45, 71, 68));
        using var copper = new SolidBrush(Color.FromArgb(80, 62, 49));
        using var road = new Pen(Color.FromArgb(116, 104, 82), 94) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        using var trailLine = new Pen(Color.FromArgb(149, 141, 113), 2) { DashStyle = DashStyle.Dash };
        using var rock = new SolidBrush(Color.FromArgb(88, 92, 78));
        using var rim = new Pen(Color.FromArgb(174, 157, 122), 3);
        g.FillRectangle(ground, 0, 0, SaltBasinSize.Width, SaltBasinSize.Height);
        g.FillEllipse(copper, 740, 890, 1500, 1050);
        g.FillRectangle(glassGrove, 1140, 0, 1150, 760);
        g.FillPolygon(saltFlat, new[] { new PointF(2250, 500), new(3330, 280), new(3600, 470), new(3600, 1850), new(2870, 1780), new(2470, 1360) });
        for (int i = 0; i < 290; i++)
        {
            int x = 34 + i * 137 % 3520, y = 90 + i * 193 % 1900;
            using var fleck = new Pen(x > 2300 ? Color.FromArgb(92, 155, 157, 139) : Color.FromArgb(72, 105, 119, 94), 2);
            g.DrawLine(fleck, x - 5, y, x + 5, y - 8);
        }
        foreach (RectangleF wall in SaltBasinWalls)
        {
            g.FillEllipse(rock, wall.X - 12, wall.Y - 8, wall.Width + 24, wall.Height + 16);
            g.DrawEllipse(rim, wall.X - 12, wall.Y - 8, wall.Width + 24, wall.Height + 16);
        }
        PointF[][] trails =
        {
            new[] { SaltBasinEntrance, new PointF(430, 850), SaltBasinLanterns[0], new PointF(1120, 490), SaltBasinAsha },
            new[] { SaltBasinEntrance, new PointF(490, 1350), new PointF(1000, 1530), new PointF(1450, 1420), SaltBasinAsha },
            new[] { SaltBasinAsha, new PointF(1900, 650), SaltBasinLanterns[1], new PointF(2430, 520), SaltBasinLanterns[2] },
            new[] { SaltBasinAsha, new PointF(2240, 1110), new PointF(2590, 1320), SaltBasinLanterns[2] }
        };
        foreach (PointF[] trail in trails) { g.DrawLines(road, trail); g.DrawLines(trailLine, trail); }
        if ((saltBasinSurveyProgress & 16) != 0)
        {
            PointF[] caveTrail = { SaltBasinLanterns[2], new(3200, 1670), SaltVaultEntrance };
            g.DrawLines(road, caveTrail); g.DrawLines(trailLine, caveTrail);
        }
        if (saltBasinRewardClaimed)
        {
            PointF[] coastTrail = { SaltBasinLanterns[2], new(3180, 1540), SaltBasinFrontierEntrance };
            g.DrawLines(road, coastTrail); g.DrawLines(trailLine, coastTrail);
        }
        using var timber = new SolidBrush(Color.FromArgb(88, 73, 54));
        using var shelter = new SolidBrush(Color.FromArgb(73, 73, 63));
        g.FillRectangle(shelter, 1570, 820, 195, 130); g.DrawRectangle(rim, 1570, 820, 195, 130);
        g.FillPolygon(timber, new[] { new PointF(1550, 820), new(1680, 735), new(1785, 820) }); g.DrawPolygon(rim, new[] { new PointF(1550, 820), new(1680, 735), new(1785, 820) });
        using var lanternLight = new SolidBrush(Color.FromArgb(180, 117, 132, 112));
        for (int i = 0; i < SaltBasinLanterns.Length; i++)
        {
            PointF point = SaltBasinLanterns[i]; bool lit = (saltBasinProgress & (1 << (i + 1))) != 0;
            using var glow = new SolidBrush(lit ? Color.FromArgb(58, 175, 221, 208) : Color.FromArgb(42, 226, 183, 112));
            g.FillEllipse(glow, point.X - 42, point.Y - 42, 84, 84);
            g.FillRectangle(lanternLight, point.X - 12, point.Y - 23, 24, 46); g.DrawRectangle(rim, point.X - 12, point.Y - 23, 24, 46);
            g.FillEllipse(lit ? Brushes.LightCyan : Brushes.Goldenrod, point.X - 6, point.Y - 17, 12, 12);
        }
        using var menhir = new SolidBrush(Color.FromArgb(111, 122, 116));
        using var menhirEdge = new Pen(Color.FromArgb(188, 176, 141), 2);
        using var menhirPromptFont = new Font("Segoe UI", 10, FontStyle.Bold);
        using var menhirPromptInk = new SolidBrush(Color.FromArgb(231, 221, 193));
        for (int i = 0; i < SaltBasinSurveyMarks.Length; i++)
        {
            PointF point = SaltBasinSurveyMarks[i];
            PointF[] stone = { new(point.X, point.Y - 23), new(point.X + 15, point.Y - 10), new(point.X + 12, point.Y + 18), new(point.X - 12, point.Y + 18), new(point.X - 15, point.Y - 10) };
            g.FillPolygon(menhir, stone); g.DrawPolygon(menhirEdge, stone);
            if (Distance(player, point) < 82) g.DrawString((saltBasinSurveyProgress & (1 << (i + 1))) != 0 ? "E  VER REGISTRO" : "E  REGISTRAR MARCO", menhirPromptFont, menhirPromptInk, point.X - 76, point.Y + 27);
        }
        if ((saltBasinSurveyProgress & 16) != 0 && Distance(player, SaltVaultEntrance) < 100) g.DrawString("E  DESCER À FENDA", menhirPromptFont, menhirPromptInk, SaltVaultEntrance.X - 76, SaltVaultEntrance.Y + 28);
        if (saltBasinRewardClaimed && Distance(player, SaltBasinFrontierEntrance) < 105) g.DrawString("E  SEGUIR À COSTA", menhirPromptFont, menhirPromptInk, SaltBasinFrontierEntrance.X - 76, SaltBasinFrontierEntrance.Y + 28);
        using var font = new Font("Segoe UI", 14, FontStyle.Bold);
        using var ink = new SolidBrush(Color.FromArgb(231, 221, 193));
        g.DrawString("ESPIGÃO DE SAL", font, ink, 500, 340);
        g.DrawString("BOSQUE DE VIDRO", font, ink, 1800, 245);
        g.DrawString("BAIXADA BRANCA", font, ink, 2700, 1350);
        g.DrawString("ABRIGO DAS TRÊS LANTERNAS", font, ink, 1510, 990);
        DrawTownPerson(g, SaltBasinAsha, "ASHA", Color.FromArgb(143, 113, 79), Distance(player, SaltBasinAsha) < InteractionRadius);
        DrawTownPerson(g, SaltBasinTavi, "TAVI", Color.FromArgb(95, 122, 115), Distance(player, SaltBasinTavi) < InteractionRadius);
        if (Distance(player, SaltBasinEntrance) < 100) g.DrawString("E  VOLTAR AO VAU", font, ink, SaltBasinEntrance.X + 35, SaltBasinEntrance.Y + 24);
        if (Distance(player, SaltBasinAsha) < InteractionRadius) g.DrawString("E  FALAR", font, ink, SaltBasinAsha.X - 38, SaltBasinAsha.Y + 43);
        if (Distance(player, SaltBasinTavi) < InteractionRadius) g.DrawString("E  FALAR", font, ink, SaltBasinTavi.X - 38, SaltBasinTavi.Y + 43);
        foreach (PointF lantern in SaltBasinLanterns) if (Distance(player, lantern) < 85) g.DrawString("E  ACENDER", font, ink, lantern.X - 48, lantern.Y + 34);
    }
}
