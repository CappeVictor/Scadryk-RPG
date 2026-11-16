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
    private int saltFrontierSideProgress;
    private static readonly SizeF SaltFrontierSize = new(4200, 2400);
    private static readonly PointF SaltFrontierEntrance = new(155, 1200);
    private static readonly PointF SaltFrontierCartographer = new(2110, 1175);
    private static readonly PointF[] SaltFrontierTideBells = { new(850, 475), new(2330, 405), new(3570, 1700) };
    private static readonly PointF SaltFrontierBellheart = new(3860, 1885);
    private static readonly PointF SaltFrontierTeren = new(3550, 425);
    private static readonly PointF SaltFrontierWreck = new(3930, 675);
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
        SpawnSaltFrontierWreckGuardians();
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

    private void SpawnSaltFrontierWreckGuardians()
    {
        if (!saltFrontierBossDefeated || saltFrontierSideProgress != 1 || enemies.Count > 0) return;
        (PointF Pos, EnemyKind Kind)[] group =
        {
            (new PointF(3790, 595), EnemyKind.SaltEcho),
            (new PointF(4070, 610), EnemyKind.Archer),
            (new PointF(3940, 825), EnemyKind.Elite)
        };
        foreach (var (position, kind) in group)
        {
            float hp = EnemyData(kind).HealthAt(42, townEnding) * DifficultyEnemyHealthMultiplier(difficultyTier);
            EliteAffix affix = kind == EnemyKind.Elite ? EliteAffixForRoll(random.Next(6)) : EliteAffix.Armored;
            enemies.Add(new Enemy { Pos = position, Kind = kind, Hp = hp, MaxHp = hp, Cooldown = 0.9f + (float)random.NextDouble(), SummonCooldown = 6, SupportCooldown = 3, EliteAffix = affix, Armored = kind == EnemyKind.Elite && affix == EliteAffix.Armored });
        }
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
            if (saltFrontierSideProgress == 2)
            {
                saltFrontierSideProgress = 3; gold += 240; AddXp(160); tideGlass += 2; potions++; health = Math.Min(maxHealth, health + 35);
                ShowMemory("A PAUSA QUE GUIA", "Nemi compara o desenho recuperado no destroço com os três intervalos da costa. A rota nunca chamou ninguém: ela servia para que barqueiros reconhecessem onde a água recua e escolhessem se queriam atravessar. Nemi acrescenta a marca ao mapa sem atribuir a passagem a qualquer viajante. A comunidade oferece 240 de ouro, 160 de experiência, 2 sais vítreos e uma poção."); return;
            }
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
            ShowMemory("NEMI, A ESCUTADORA", (saltFrontierProgress & 16) != 0 ? saltFrontierBossDefeated ? saltFrontierSideProgress switch { 0 => "O Coro se desfez sem levar nenhum nome. Teren está examinando um destroço ao norte da costa; talvez aquele mapa de corrente ajude os barqueiros a escolher uma travessia segura.", 1 => "Teren espera no caminho ao norte. Os guardiões do destroço ainda protegem o registro da maré.", 2 => "Você recuperou o desenho do destroço. Nemi pode comparar a marca com os intervalos das três badaladas.", _ => "O Coro se desfez, o destroço foi registrado e as barcas usam a pausa entre as badaladas como referência. Cada viajante decide por onde seguir." } : "As três notas já não chamam viajantes. Resta uma voz presa no coração da torre submersa, ao sul do juncal; ela precisa ser enfrentada para que a costa descanse." : $"Badaladas silenciadas: {System.Numerics.BitOperations.PopCount((uint)(saltFrontierProgress & 14))}/3. Aproxime-se de cada torre depois de limpar seus guardiões."); return;
        }
        if (saltFrontierBossDefeated && Distance(player, SaltFrontierTeren) < 95)
        {
            if (saltFrontierSideProgress == 0)
            {
                saltFrontierSideProgress = 1; SpawnSaltFrontierWreckGuardians();
                ShowMemory("TEREN, PILOTO DE MARÉ", "Quando o Coro silenciou, o banco ao norte cedeu e revelou um destroço. Meu mapa ainda vibra, mas não marca nomes: mostra um trecho onde a água recua antes de virar. Os ecos cercaram o casco. Se quiser ajudar, limpe a área e recupere o desenho; Nemi saberá registrar a passagem."); return;
            }
            ShowMemory("TEREN, PILOTO DE MARÉ", saltFrontierSideProgress == 1 ? "Os guardiões ainda rondam o destroço. Aproxime-se pelo caminho de basalto e recolha qualquer saque antes de examinar o desenho." : saltFrontierSideProgress == 2 ? "O desenho está com você. Leve-o a Nemi para comparar os intervalos; ele não aponta quem passou nem para onde." : "O mapa agora mostra apenas onde a água recua. Nenhuma rota é atribuída a quem escolheu atravessar."); return;
        }
        if (saltFrontierBossDefeated && Distance(player, SaltFrontierWreck) < 105)
        {
            if (saltFrontierSideProgress == 0) { ShowMemory("DESTROÇO ENTRE AS CONCHAS", "Teren está acampado mais a oeste. Os ecos ainda protegem o registro preso ao casco."); return; }
            if (saltFrontierSideProgress >= 2) { ShowMemory("REGISTRO DA MARÉ", "O desenho já foi recolhido. Ele marca o recuo da água, sem nomes nem destinos."); return; }
            if (enemies.Any(enemy => Distance(enemy.Pos, SaltFrontierWreck) < 270)) { ShowMemory("DESTROÇO ENTRE AS CONCHAS", "Os guardiões ainda cercam o casco. Limpe o local antes de examinar o registro."); return; }
            if (drops.Any(drop => Distance(drop.Pos, SaltFrontierWreck) < 270)) { ShowMemory("DESTROÇO ENTRE AS CONCHAS", "O saque repousa junto ao casco. Recolha-o antes de abrir o registro."); return; }
            saltFrontierSideProgress = 2;
            ShowMemory("REGISTRO DA MARÉ", "Entre as conchas há um desenho do banco de areia, marcado com três intervalos. Não há nome, assinatura ou destino: apenas o ponto em que a corrente baixa o bastante para cruzar. Leve o registro a Nemi."); return;
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
        if (saltFrontierSideProgress == 0) return "OPCIONAL: FALE COM TEREN NO CAMINHO AO NORTE";
        if (saltFrontierSideProgress == 1) return "OPCIONAL: LIMPE O DESTROÇO E RECUPERE O REGISTRO";
        if (saltFrontierSideProgress == 2) return "OPCIONAL: LEVE O REGISTRO DA MARÉ A NEMI";
        return "COSTA DAS BADALADAS • ROTA REGISTRADA • EXPLORAÇÃO LIVRE";
    }

    private string SaltFrontierJournalStatus() => saltFrontierSideProgress switch
    {
        0 => saltFrontierBossDefeated ? "Opcional: fale com Teren no caminho ao norte" : "Coro ainda guarda o coração submerso",
        1 => "Destroço das Conchas: guardiões ativos",
        2 => "Registro recuperado; relate a Nemi",
        _ => "Rota do recuo registrada por Nemi"
    };

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
        if (saltFrontierBossDefeated)
        {
            PointF[] wreckPath = { new(3000, 790), new(3400, 760), SaltFrontierTeren, new(3770, 520), SaltFrontierWreck };
            g.DrawLines(road, wreckPath); g.DrawLines(trail, wreckPath);
        }
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
        using var wreckGlow = new SolidBrush(Color.FromArgb(46, 183, 211, 194));
        g.FillEllipse(observatory, 1935, 1035, 350, 255); g.DrawEllipse(rim, 1935, 1035, 350, 255);
        g.FillRectangle(wood, 2028, 1060, 160, 88); g.DrawRectangle(rim, 2028, 1060, 160, 88);
        g.FillPolygon(wood, new[] { new PointF(2005, 1062), new(2110, 995), new(2210, 1062) });
        g.DrawPolygon(rim, new[] { new PointF(2005, 1062), new(2110, 995), new(2210, 1062) });
        g.FillRectangle(canvas, 2078, 1081, 60, 43); g.DrawRectangle(rim, 2078, 1081, 60, 43);
        g.DrawLine(rim, 2155, 1054, 2235, 1010); g.DrawEllipse(rim, 2218, 994, 34, 34);
        g.DrawString("BANCO DE BASALTO", labelFont, ink, 380, 270);
        g.DrawString("JUNCAL DE VIDRO", labelFont, ink, 1540, 155);
        g.DrawString("COSTA SUBMERSA", labelFont, ink, 3230, 1430);
        if (saltFrontierBossDefeated)
        {
            using var driftwood = new SolidBrush(Color.FromArgb(109, 88, 67));
            using var shell = new SolidBrush(Color.FromArgb(181, 205, 192));
            PointF wreck = SaltFrontierWreck;
            g.FillEllipse(wreckGlow, wreck.X - 100, wreck.Y - 64, 200, 128);
            g.FillPolygon(driftwood, new[] { new PointF(wreck.X - 82, wreck.Y + 22), new PointF(wreck.X - 42, wreck.Y - 38), new PointF(wreck.X + 45, wreck.Y - 30), new PointF(wreck.X + 90, wreck.Y + 22), new PointF(wreck.X + 55, wreck.Y + 44) });
            g.DrawLine(rim, wreck.X - 48, wreck.Y - 26, wreck.X + 48, wreck.Y + 26); g.DrawLine(rim, wreck.X + 36, wreck.Y - 31, wreck.X - 30, wreck.Y + 31);
            g.FillEllipse(shell, wreck.X - 6, wreck.Y - 5, 12, 10);
            g.DrawString("DESTROÇO DAS CONCHAS", smallFont, ink, wreck.X - 112, wreck.Y + 54);
            if (Distance(player, wreck) < 110) g.DrawString("E  EXAMINAR REGISTRO", smallFont, ink, wreck.X - 102, wreck.Y + 73);
            DrawTownPerson(g, SaltFrontierTeren, "TEREN", Color.FromArgb(151, 129, 96), Distance(player, SaltFrontierTeren) < InteractionRadius);
            if (Distance(player, SaltFrontierTeren) < InteractionRadius) g.DrawString("E  FALAR", smallFont, ink, SaltFrontierTeren.X - 40, SaltFrontierTeren.Y + 45);
        }
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
        if (saltFrontierBossDefeated) g.DrawLines(line, new[] { new PointF(3000, 790), new PointF(3400, 760), SaltFrontierTeren, new PointF(3770, 520), SaltFrontierWreck }.Select(Map).ToArray());
        if ((saltFrontierProgress & 31) == 31) g.DrawLines(line, new[] { SaltFrontierTideBells[2], new PointF(3750, 1770), SaltFrontierBellheart }.Select(Map).ToArray());
        PointF[] positions = { SaltFrontierEntrance, SaltFrontierCartographer, SaltFrontierTideBells[0], SaltFrontierTideBells[1], SaltFrontierTideBells[2], SaltFrontierBellheart, SaltFrontierTeren, SaltFrontierWreck };
        string[] names = { "BACIA", "NEMI", "BASALTO", "JUNCAL", "COSTA", "CORAÇÃO", "TEREN", "DESTROÇO" };
        for (int i = 0; i < positions.Length; i++)
        {
            PointF p = Map(positions[i]);
            bool visible = i switch { 0 or 1 => true, 5 => (saltFrontierProgress & 31) == 31, 6 or 7 => saltFrontierBossDefeated, _ => (saltFrontierProgress & (1 << (i - 1))) != 0 };
            if (!visible) { g.FillEllipse(Brushes.DimGray, p.X - 5, p.Y - 5, 10, 10); continue; }
            bool marked = i >= 2 && i <= 4 && (saltFrontierProgress & (1 << (i - 1))) != 0;
            Brush marker = i == 1 || i == 6 ? Brushes.LightSkyBlue : i == 5 ? saltFrontierBossDefeated ? Brushes.LightCyan : Brushes.MediumPurple : i == 7 ? saltFrontierSideProgress == 2 ? Brushes.LightGreen : Brushes.Goldenrod : marked ? Brushes.LightGreen : Brushes.Goldenrod;
            g.FillEllipse(marker, p.X - 6, p.Y - 6, 12, 12);
            g.DrawString(names[i], font, Brushes.AntiqueWhite, p.X - 32, p.Y - 22);
        }
        PointF here = Map(player); g.FillEllipse(Brushes.LightGreen, here.X - 7, here.Y - 7, 14, 14);
        g.DrawString("VERDE: VOCÊ/ROTA RECUPERADA  •  OURO: A DESCOBRIR  •  CIANO: NEMI/TEREN  •  ROXO: CORAÇÃO  •  M/ESC: FECHAR", font, Brushes.AntiqueWhite, 125, 547);
    }
}
