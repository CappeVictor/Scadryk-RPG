using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Collections.Generic;

public sealed partial class Game
{
    // 0: undiscovered, 1: investigate, 2: voices released, 3: reported to Edda.
    private int firstEchoQuest;
    private bool firstEchoCaveFound;
    private readonly List<Enemy> firstEchoWaitingEnemies = new();
    private readonly List<Drop> firstEchoWaitingDrops = new();
    private bool firstEchoOutsideStored;
    // World-space geometry for the future continuous route from Véspera.
    private static readonly SizeF FirstEchoSize = new(2400, 1400);
    private static readonly PointF FirstEchoTownGate = new(120, 700);
    private static readonly PointF FirstEchoCrossroads = new(900, 700);
    private static readonly PointF FirstEchoCave = new(1050, 250);
    private static readonly PointF FirstEchoSluice = new(2250, 700);
    private static readonly PointF FirstEchoIvara = new(860, 590);
    private static readonly RectangleF[] FirstEchoCaveWalls = { new(285, 145, 70, 95), new(440, 395, 85, 105), new(610, 145, 70, 80) };
    private static readonly RectangleF[] FirstEchoSluiceWalls = { new(300, 145, 45, 120), new(300, 395, 45, 110), new(555, 145, 45, 95), new(555, 405, 45, 95) };
    private static readonly (PointF Position, EnemyKind Kind)[] FirstEchoEncounters =
    {
        (new(480, 650), EnemyKind.Grunt), (new(540, 760), EnemyKind.Archer),
        (new(1080, 520), EnemyKind.Grunt), (new(1110, 350), EnemyKind.Archer),
        (new(1320, 740), EnemyKind.Grunt), (new(1450, 650), EnemyKind.Archer),
        (new(1440, 1080), EnemyKind.Grunt), (new(1950, 1050), EnemyKind.Archer),
        (new(2050, 650), EnemyKind.Grunt), (new(2160, 760), EnemyKind.Elite)
    };

    private void SpawnFirstEchoEncounters()
    {
        foreach (var encounter in FirstEchoEncounters)
        {
            float hp = (encounter.Kind == EnemyKind.Elite ? 150 : encounter.Kind == EnemyKind.Archer ? 72 : 82)
                * DifficultyEnemyHealthMultiplier(difficultyTier);
            enemies.Add(new Enemy { Pos = encounter.Position, Kind = encounter.Kind, Hp = hp, MaxHp = hp,
                Cooldown = 0.7f + (float)random.NextDouble(), SummonCooldown = 6, SupportCooldown = 3 });
        }
    }

    private void InteractFirstEcho()
    {
        Drop? weaponDrop = drops.Where(drop => drop.Kind == "weapon" && Distance(player, drop.Pos) <= 75).OrderBy(drop => Distance(player, drop.Pos)).FirstOrDefault();
        if (weaponDrop is not null) { GameAudio.Play(GameAudio.Cue.Loot); Collect(weaponDrop); return; }
        if (Distance(player, FirstEchoTownGate) < 90)
        {
            StoreFirstEchoOutside();
            room = 3; SetupRoom();
            ShowMemory("REGRESSO A VÉSPERA", "A estrada permanece aberta. Os marcos conduzem de volta à encruzilhada.");
        }
        else if (Distance(player, FirstEchoIvara) < 90)
        {
            if (firstEchoQuest == 0) firstEchoQuest = 1;
            ShowMemory("IVARA, CARTÓGRAFA", firstEchoQuest == 3 ? "Edda contou que o sino respondeu aos marcos. Os viajantes podem escolher seu caminho e encontrar abrigo outra vez." : firstEchoQuest == 2 ? "Os marcos voltaram a responder. Edda precisa saber que a estrada está aberta." : "Os marcos perderam a voz. A estrada segue até a Casa das Comportas; a trilha ao norte leva à Gruta das Raízes. A margem alagada oferece outra travessia.");
        }
        else if (Distance(player, FirstEchoCave) < 90)
        {
            EnterFirstEchoInterior(44); ShowMemory("GRUTA DAS RAÍZES", "A trilha desce sob as raízes. Um antigo abrigo permanece no fundo da gruta.");
        }
        else if (Distance(player, FirstEchoSluice) < 90)
        {
            if (firstEchoQuest == 0) ShowMemory("CASA DAS COMPORTAS", "As marcas no mecanismo são desconhecidas. Ivara, na encruzilhada, pode ajudar a decifrá-las.");
            else { EnterFirstEchoInterior(45); ShowMemory("CASA DAS COMPORTAS", "O Vigia protege o mecanismo que prende as vozes. Observe os sinais no chão: ele alterna uma onda ao seu redor e uma marca sobre a posição do viajante."); }
        }
    }

    private void EnterFirstEchoInterior(int destination)
    {
        StoreFirstEchoOutside();
        room = destination; SetupRoom();
    }

    private void StoreFirstEchoOutside()
    {
        firstEchoWaitingEnemies.Clear(); firstEchoWaitingEnemies.AddRange(enemies);
        firstEchoWaitingDrops.Clear(); firstEchoWaitingDrops.AddRange(drops);
        firstEchoOutsideStored = true;
    }

    private void RestoreFirstEchoOutside()
    {
        if (!firstEchoOutsideStored) return;
        enemies.Clear(); enemies.AddRange(firstEchoWaitingEnemies);
        drops.Clear(); drops.AddRange(firstEchoWaitingDrops);
        firstEchoWaitingEnemies.Clear(); firstEchoWaitingDrops.Clear();
        firstEchoOutsideStored = false;
    }

    private void SetupFirstEchoInterior()
    {
        player = new PointF(150, 330);
        if (room == 44 && firstEchoCaveFound || room == 45 && firstEchoQuest >= 2) return;
        int count = room == 44 ? 3 : 4;
        PointF[] spawnPoints = { new(480, 250), new(660, 270), new(480, 340), new(680, 400) };
        for (int i = 0; i < count; i++)
        {
            EnemyKind kind = room == 45 && i == 0 ? EnemyKind.Guardian : i == 1 ? EnemyKind.Archer : EnemyKind.Grunt;
            float hp = (kind == EnemyKind.Guardian ? 180 : kind == EnemyKind.Archer ? 72 : 82) * DifficultyEnemyHealthMultiplier(difficultyTier);
            enemies.Add(new Enemy { Pos = spawnPoints[i], Kind = kind, NameOverride = kind == EnemyKind.Guardian ? "VIGIA DAS COMPORTAS" : null, Hp = hp, MaxHp = hp, Cooldown = 1, SummonCooldown = 6, SupportCooldown = 3 });
        }
    }

    private void InteractFirstEchoInterior()
    {
        Drop? item = drops.Where(d => d.Kind == "weapon" && Distance(player, d.Pos) <= 75).OrderBy(d => Distance(player, d.Pos)).FirstOrDefault();
        if (item is not null) { GameAudio.Play(GameAudio.Cue.Loot); Collect(item); return; }
        if (player.X < 180)
        {
            if (drops.Count > 0) { notice = "RECOLHA O SAQUE ANTES DE SAIR"; noticeTime = 2; return; }
            bool cave = room == 44; room = 43; SetupRoom();
            RestoreFirstEchoOutside();
            player = cave ? new PointF(1050, 340) : new PointF(2150, 700); return;
        }
        if (Distance(player, new PointF(760, 330)) > 100) return;
        if (enemies.Count > 0) { notice = "DERROTE OS ECOS PRÓXIMOS"; noticeTime = 2; return; }
        if (room == 44 && !firstEchoCaveFound)
        {
            firstEchoCaveFound = true; gold += 45; potions++;
            ShowMemory("REGISTRO ENTRE RAÍZES", "Ivara deixou provisões e um registro: os marcos foram erguidos para oferecer abrigo. Você encontrou 45 de ouro e uma poção.");
        }
        else if (room == 45 && firstEchoQuest == 1)
        {
            firstEchoQuest = 2; ShowMemory("AS VOZES DA ESTRADA", "Você abre as comportas e as vozes voltam aos marcos. Volte a Edda em Véspera.");
        }
    }

    private void DrawFirstEchoInterior(Graphics g)
    {
        using var ground = new SolidBrush(room == 44 ? Color.FromArgb(55, 60, 48) : Color.FromArgb(59, 67, 67));
        using var accent = new Pen(room == 44 ? Color.FromArgb(112, 115, 76) : Color.FromArgb(113, 155, 153), 5);
        g.FillRectangle(ground, 65, 108, 830, 445);
        for (int x = 130; x < 880; x += 125) g.DrawLine(accent, x, 115, x - 35, 175);
        using var obstacle = new SolidBrush(room == 44 ? Color.FromArgb(78, 83, 63) : Color.FromArgb(92, 99, 92));
        foreach (RectangleF wall in room == 44 ? FirstEchoCaveWalls : FirstEchoSluiceWalls)
        {
            g.FillRectangle(obstacle, wall);
            g.DrawRectangle(accent, wall.X, wall.Y, wall.Width, wall.Height);
        }
        g.DrawEllipse(accent, 710, 280, 100, 100);
        using var font = new Font("Segoe UI", 11, FontStyle.Bold);
        g.DrawString(room == 44 ? "ABRIGO ANTIGO" : "MECANISMO DAS VOZES", font, Brushes.AntiqueWhite, 660, 250);
        if (player.X < 180) g.DrawString("E  VOLTAR ÀS MARGENS", font, Brushes.AntiqueWhite, 90, 380);
        if (Distance(player, new PointF(760, 330)) < 100) g.DrawString("E  EXAMINAR", font, Brushes.AntiqueWhite, 700, 390);
    }

    private void DrawFirstEchoMap(Graphics g)
    {
        using var shade = new SolidBrush(Color.FromArgb(240, 13, 18, 17));
        using var road = new Pen(Color.FromArgb(174, 150, 105), 4);
        using var font = new Font("Segoe UI", 10, FontStyle.Bold);
        using var titleFont = new Font("Georgia", 18, FontStyle.Bold);
        g.FillRectangle(shade, 35, 95, 890, 500);
        g.DrawString("MARGENS DO PRIMEIRO ECO", titleFont, Brushes.Goldenrod, 260, 115);
        PointF Map(PointF p) => new(100 + p.X * 0.31f, 175 + p.Y * 0.25f);
        PointF[] main = { FirstEchoTownGate, FirstEchoCrossroads, FirstEchoSluice };
        PointF[] north = { FirstEchoCrossroads, FirstEchoCave };
        PointF[] south = { FirstEchoCrossroads, new(1400, 1050), new(1900, 1050), FirstEchoSluice };
        foreach (PointF[] path in new[] { main, north, south }) g.DrawLines(road, path.Select(Map).ToArray());
        foreach (var landmark in new[] { (FirstEchoTownGate, "VÉSPERA"), (FirstEchoCrossroads, "IVARA"), (FirstEchoCave, "GRUTA"), (FirstEchoSluice, "COMPORTAS") })
        {
            PointF p = Map(landmark.Item1);
            g.FillEllipse(Brushes.Goldenrod, p.X - 5, p.Y - 5, 10, 10);
            g.DrawString(landmark.Item2, font, Brushes.AntiqueWhite, p.X - 28, p.Y - 27);
        }
        PointF here = Map(player);
        g.FillEllipse(Brushes.LightGreen, here.X - 7, here.Y - 7, 14, 14);
        string status = firstEchoQuest == 0 ? "PROCURE IVARA" : firstEchoQuest == 1 ? "SIGA ÀS COMPORTAS" : firstEchoQuest == 2 ? "RETORNE A EDDA" : "MARCOS RESTABELECIDOS";
        g.DrawString(status, font, Brushes.Goldenrod, 350, 520);
        g.DrawString("VERDE: VOCÊ  •  M/ESC: FECHAR", font, Brushes.AntiqueWhite, 280, 555);
    }
    // Water blocks passage except at the two bridges, with enough clearance
    // for both the player and the larger enemies.
    private static readonly RectangleF[] FirstEchoWalls =
    {
        new(1660, 0, 160, 640),
        new(1660, 760, 160, 230),
        new(1660, 1110, 160, 290),
        new(693, 110, 14, 40), new(783, 200, 14, 40),
        new(933, 110, 14, 40), new(1163, 120, 14, 40),
        new(1313, 260, 14, 40), new(743, 350, 14, 40),
        new(1273, 390, 14, 40), new(2180, 495, 150, 100)
    };

    private void DrawFirstEchoEnvironment(Graphics g)
    {
        using var meadow = new SolidBrush(Color.FromArgb(56, 67, 54));
        using var grove = new SolidBrush(Color.FromArgb(40, 59, 49));
        using var flooded = new SolidBrush(Color.FromArgb(52, 79, 78));
        using var road = new Pen(Color.FromArgb(112, 99, 75), 112) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        using var track = new Pen(Color.FromArgb(149, 130, 94), 2) { DashStyle = DashStyle.Dash };
        g.FillRectangle(meadow, 0, 0, FirstEchoSize.Width, FirstEchoSize.Height);
        g.FillRectangle(grove, 650, 0, 850, 490);
        g.FillRectangle(flooded, 1250, 900, 1150, 500);
        using var trunk = new SolidBrush(Color.FromArgb(75, 66, 49));
        using var leaves = new SolidBrush(Color.FromArgb(44, 77, 58));
        using var rock = new SolidBrush(Color.FromArgb(91, 99, 83));
        // Fixed landmarks make the road readable each time the region is visited.
        foreach (PointF tree in new[] { new PointF(700, 110), new(790, 200), new(940, 110), new(1170, 120), new(1320, 260), new(750, 350), new(1280, 390) })
        {
            g.FillRectangle(trunk, tree.X - 7, tree.Y, 14, 40);
            g.FillEllipse(leaves, tree.X - 30, tree.Y - 25, 60, 48);
        }
        foreach (PointF stonePos in new[] { new PointF(300, 480), new(620, 850), new(1200, 840), new(1500, 1180), new(1940, 850), new(2220, 980) })
            g.FillEllipse(rock, stonePos.X - 20, stonePos.Y - 12, 40, 24);
        PointF[] main = { FirstEchoTownGate, new(450, 700), FirstEchoCrossroads, new(1500, 700), new(1870, 700), FirstEchoSluice };
        PointF[] branch = { FirstEchoCrossroads, new(900, 450), FirstEchoCave };
        PointF[] returnPath = { new(1200, 700), new(1400, 1050), new(1900, 1050), new(2100, 700) };
        foreach (PointF[] path in new[] { main, branch, returnPath }) { g.DrawLines(road, path); g.DrawLines(track, path); }
        using var water = new SolidBrush(Color.FromArgb(48, 91, 95));
        g.FillRectangle(water, 1660, 0, 160, 1400);
        using var bridge = new SolidBrush(Color.FromArgb(127, 111, 81));
        g.FillRectangle(bridge, 1640, 640, 200, 120);
        g.FillRectangle(bridge, 1640, 990, 200, 120);
        using var stone = new SolidBrush(Color.FromArgb(111, 108, 92));
        using var edge = new Pen(Color.FromArgb(193, 174, 128), 3);
        using var building = new SolidBrush(Color.FromArgb(86, 79, 66));
        g.FillRectangle(building, 2180, 495, 150, 100);
        g.DrawRectangle(edge, 2180, 495, 150, 100);
        g.FillEllipse(Brushes.Black, 1005, 185, 90, 50);
        g.DrawArc(edge, 1005, 165, 90, 70, 180, 180);
        foreach (PointF marker in new[] { FirstEchoTownGate, FirstEchoCrossroads, FirstEchoCave, FirstEchoSluice })
        {
            g.FillRectangle(stone, marker.X - 16, marker.Y - 28, 32, 56);
            g.DrawRectangle(edge, marker.X - 16, marker.Y - 28, 32, 56);
        }
        using var font = new Font("Segoe UI", 14, FontStyle.Bold);
        using var label = new SolidBrush(Color.FromArgb(232, 218, 182));
        g.DrawString("VÉSPERA", font, label, 75, 625);
        g.DrawString("ENCRUZILHADA DOS MARCOS", font, label, 760, 610);
        g.DrawString("GRUTA DAS RAÍZES", font, label, 975, 170);
        g.DrawString("CASA DAS COMPORTAS", font, label, 2100, 620);
        DrawTownPerson(g, FirstEchoIvara, "IVARA", Color.FromArgb(128, 105, 78), Distance(player, FirstEchoIvara) < InteractionRadius);
        foreach (PointF point in new[] { FirstEchoTownGate, FirstEchoCave, FirstEchoSluice })
            if (Distance(player, point) < 90) g.DrawString("E  INTERAGIR", font, label, point.X - 65, point.Y + 40);
    }
}
