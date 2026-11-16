using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Collections.Generic;

public sealed partial class Game
{
    // 0: undiscovered, 1: investigate, 2: voices released, 3: reported to Edda.
    private int firstEchoQuest;
    private bool firstEchoCaveFound, firstEchoReedCache;
    private bool firstEchoBeaconAccepted { get => (firstEchoDiscovered & 8) != 0; set => firstEchoDiscovered = value ? firstEchoDiscovered | 8 : firstEchoDiscovered & ~8; }
    private bool firstEchoBeaconRestored { get => (firstEchoDiscovered & 16) != 0; set => firstEchoDiscovered = value ? firstEchoDiscovered | 16 : firstEchoDiscovered & ~16; }
    private bool firstEchoFerryAccepted { get => (firstEchoDiscovered & 32) != 0; set => firstEchoDiscovered = value ? firstEchoDiscovered | 32 : firstEchoDiscovered & ~32; }
    private bool firstEchoFerryRestored { get => (firstEchoDiscovered & 64) != 0; set => firstEchoDiscovered = value ? firstEchoDiscovered | 64 : firstEchoDiscovered & ~64; }
    private int firstEchoDiscovered;
    private readonly List<Enemy> firstEchoWaitingEnemies = new();
    private readonly List<Drop> firstEchoWaitingDrops = new();
    private bool firstEchoOutsideStored;
    private float FirstEchoTerrainSpeed() => room == 43 && player.X > 1250 && player.Y > 900 && (player.X is > 1250 and < 1640 || player.X is > 1840 and < 2400) ? 0.8f : room == 43 && player.X > 2460 && player.Y is > 340 and < 1250 ? 0.88f : 1f;
    // World-space geometry for the future continuous route from Véspera.
    private static readonly SizeF FirstEchoSize = new(3200, 1400);
    private static readonly PointF FirstEchoTownGate = new(120, 700);
    private static readonly PointF FirstEchoCrossroads = new(900, 700);
    private static readonly PointF FirstEchoCave = new(1050, 250);
    private static readonly PointF FirstEchoSluice = new(2250, 700);
    private static readonly PointF FirstEchoIvara = new(860, 590);
    private static readonly PointF FirstEchoReedCache = new(1950, 1170);
    private static readonly PointF FirstEchoRoadSign = new(540, 800);
    private static readonly PointF FirstEchoBeacon = new(2110, 330);
    private static readonly PointF FirstEchoFerryman = new(2760, 700);
    private static readonly PointF FirstEchoAshSignal = new(3060, 1080);
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
        if (firstEchoBeaconAccepted && !firstEchoBeaconRestored) SpawnFirstEchoBeaconEncounters();
        if (firstEchoFerryAccepted && !firstEchoFerryRestored) SpawnFirstEchoFerryEncounters();
    }

    private void SpawnFirstEchoBeaconEncounters()
    {
        foreach (var (position, kind) in new[] { (new PointF(1900, 360), EnemyKind.Archer), (new PointF(2040, 470), EnemyKind.Grunt), (new PointF(2220, 390), EnemyKind.Elite) })
        {
            float hp = (kind == EnemyKind.Elite ? 175 : kind == EnemyKind.Archer ? 82 : 92) * DifficultyEnemyHealthMultiplier(difficultyTier);
            enemies.Add(new Enemy { Pos = position, Kind = kind, Hp = hp, MaxHp = hp, Cooldown = 0.8f + (float)random.NextDouble(), SummonCooldown = 6, SupportCooldown = 3 });
        }
    }

    private void SpawnFirstEchoFerryEncounters()
    {
        foreach (var (position, kind) in new[] { (new PointF(2700, 910), EnemyKind.Archer), (new PointF(2920, 1060), EnemyKind.NameMoth), (new PointF(3100, 900), EnemyKind.Elite) })
        {
            float hp = (kind == EnemyKind.Elite ? 205 : kind == EnemyKind.Archer ? 100 : 112) * DifficultyEnemyHealthMultiplier(difficultyTier);
            enemies.Add(new Enemy { Pos = position, Kind = kind, Hp = hp, MaxHp = hp, Cooldown = 0.8f + (float)random.NextDouble(), SummonCooldown = 6, SupportCooldown = 3 });
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
            if (firstEchoQuest >= 2 && enemies.Any(enemy => Distance(enemy.Pos, FirstEchoIvara) < 200))
            {
                ShowMemory("IVARA, CARTÓGRAFA", "Há ecos perto demais para descansar. Afaste-os e volte à encruzilhada.");
                return;
            }
            if (firstEchoQuest >= 2) health = maxHealth;
            string ivaraText = firstEchoQuest == 3
                ? firstEchoFerryRestored ? "Nilo voltou a conduzir viajantes pelo Vau de Cinza; as vozes já alcançam as duas margens. Ivara oferece descanso: vida restaurada."
                : firstEchoFerryAccepted ? "Nilo espera no Vau de Cinza. Afaste os ecos do desembarcadouro e reacenda o sinal de pedra; depois, conte a ele. Ivara oferece descanso: vida restaurada."
                : firstEchoBeaconRestored ? "O marco alto voltou a cantar. A trilha segue além dele até o Vau de Cinza; procure Nilo, o barqueiro, na margem de cinzas. Ivara oferece descanso: vida restaurada."
                : firstEchoBeaconAccepted ? "Os ecos cercam o marco da escarpa ao nordeste. Afaste-os e toque o sino de pedra; depois, volte para contar a Ivara. Ela oferece descanso: vida restaurada."
                : "Edda contou que os marcos responderam. Falta o marco alto da escarpa, a nordeste: ecos tomaram a trilha e o sino de pedra está mudo. Se puder, liberte-o e volte. Ivara oferece descanso: vida restaurada."
                : firstEchoQuest == 2 ? "Os marcos voltaram a responder. Edda precisa saber que a estrada está aberta. Ivara oferece um momento de descanso: vida restaurada."
                : "Os marcos perderam a voz. A estrada segue até a Casa das Comportas; a trilha ao norte leva à Gruta das Raízes. A margem alagada oferece outra travessia, mas a água atrasa os passos.";
            if (firstEchoQuest == 3 && !firstEchoBeaconAccepted && !firstEchoBeaconRestored) { firstEchoBeaconAccepted = true; SpawnFirstEchoBeaconEncounters(); }
            ShowMemory("IVARA, CARTÓGRAFA", ivaraText);
        }
        else if (Distance(player, FirstEchoFerryman) < 95)
        {
            if (!firstEchoBeaconRestored) ShowMemory("NILO, BARQUEIRO", "O caminho além da Casa das Comportas ainda não responde. Ivara talvez consiga despertar o marco alto da escarpa.");
            else if (firstEchoFerryRestored) ShowMemory("NILO, BARQUEIRO", "O sinal de pedra voltou a arder. A corrente está segura outra vez; a luz vai guiar quem chegar ao Vau sem deixá-lo se perder na cinza.");
            else if (firstEchoFerryAccepted) ShowMemory("NILO, BARQUEIRO", "Os ecos guardam o sinalizador no desembarcadouro ao sudeste. Reacenda a pedra quando o caminho estiver livre.");
            else
            {
                firstEchoFerryAccepted = true;
                if (enemies.All(enemy => Distance(enemy.Pos, FirstEchoAshSignal) >= 260)) SpawnFirstEchoFerryEncounters();
                ShowMemory("NILO, BARQUEIRO", "Sou Nilo, barqueiro do Vau de Cinza. As mariposas levaram o lume do sinalizador e os ecos tomaram o desembarcadouro. Limpe o caminho e reacenda a pedra; assim ninguém perderá a travessia.");
            }
        }
        else if (Distance(player, FirstEchoCave) < 90)
        {
            EnterFirstEchoInterior(44); ShowMemory("GRUTA DAS RAÍZES", "A trilha desce sob as raízes. Um antigo abrigo permanece no fundo da gruta.");
        }
        else if (Distance(player, FirstEchoReedCache) < 75)
        {
            if (!firstEchoReedCache)
            {
                firstEchoReedCache = true; gold += 35; AddXp(15);
                ShowMemory("CAIXA DOS JUNCAIS", "Uma caixa presa à ponte guarda moedas e um bilhete: ‘Se a estrada não responder, siga os juncos até a outra margem.’ Você encontrou 35 de ouro e 15 de experiência.");
            }
            else ShowMemory("CAIXA DOS JUNCAIS", "O bilhete continua preso à caixa para orientar outros viajantes.");
        }
        else if (Distance(player, FirstEchoRoadSign) < 75)
            ShowMemory("MARCO DA ESTRADA", firstEchoQuest >= 2 ? "A pedra responde ao toque com uma voz suave: ‘Abrigo adiante. Nenhum nome será pedido.’ A marca de Ivara aponta para a encruzilhada." : "As letras dizem: ‘Abrigo adiante. Nenhum nome será pedido.’ O marco deveria responder aos viajantes, mas permanece silencioso. Uma seta aponta para a encruzilhada.");
        else if (Distance(player, FirstEchoBeacon) < 85)
        {
            if (firstEchoQuest < 3) ShowMemory("MARCO DA ESCARPA", "O sino de pedra não responde. Ivara talvez saiba por que os ecos se reuniram nesta trilha.");
            else if (!firstEchoBeaconAccepted) ShowMemory("MARCO DA ESCARPA", "Uma inscrição aponta de volta para Ivara: ‘A última voz não se ergue sozinha.’");
            else if (firstEchoBeaconRestored) ShowMemory("MARCO DA ESCARPA", "O sino de pedra vibra com as vozes libertadas. A trilha da escarpa agora conduz viajantes até a margem sem se perderem.");
            else if (enemies.Any(enemy => Distance(enemy.Pos, FirstEchoBeacon) < 250)) ShowMemory("MARCO DA ESCARPA", "Os ecos interrompem cada tentativa de tocar o sino. Limpe a escarpa primeiro.");
            else
            {
                firstEchoBeaconRestored = true; gold += 90; AddXp(45);
                ShowMemory("O SINO DA ESCARPA", "A pedra responde às vozes das comportas. Um som atravessa a margem e desenha uma rota segura até a escarpa. Você encontrou 90 de ouro e 45 de experiência. Volte a Ivara.");
            }
        }
        else if (Distance(player, FirstEchoAshSignal) < 90)
        {
            if (!firstEchoFerryAccepted) ShowMemory("SINAL DO VAU", "A pedra está fria. Nilo, perto da trilha de cinzas, pode explicar como reacendê-la.");
            else if (firstEchoFerryRestored) ShowMemory("SINAL DO VAU", "A chama ciana guia a barca entre as margens. O caminho está aberto para quem vier depois.");
            else if (enemies.Any(enemy => Distance(enemy.Pos, FirstEchoAshSignal) < 260)) ShowMemory("SINAL DO VAU", "As mariposas de cinza apagam qualquer chama. Derrote os ecos no desembarcadouro primeiro.");
            else
            {
                firstEchoFerryRestored = true; gold += 120; AddXp(60);
                ShowMemory("A LUZ ENTRE AS MARGENS", "O sinalizador reacende e a barca de Nilo volta a cruzar o vau. Uma linha de luz ciana revela uma nova passagem entre os bancos de cinza. Você encontrou 120 de ouro e 60 de experiência.");
            }
        }
        else if (Distance(player, FirstEchoSluice) < 90)
        {
            if (firstEchoQuest == 0) ShowMemory("CASA DAS COMPORTAS", "As marcas no mecanismo são desconhecidas. Ivara, na encruzilhada, pode ajudar a decifrá-las.");
            else { EnterFirstEchoInterior(45); ShowMemory("CASA DAS COMPORTAS", "O Vigia protege o mecanismo que prende as vozes. Observe os sinais no chão: ele alterna uma onda ao seu redor e uma marca sobre a posição do viajante."); }
        }
    }

    private void EnterFirstEchoInterior(int destination)
    {
        firstEchoDiscovered |= destination == 44 ? 2 : 4;
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
            firstEchoCaveFound = true; gold += 45; potions++; AddXp(20);
            ShowMemory("REGISTRO ENTRE RAÍZES", "Ivara deixou provisões e um registro: os marcos foram erguidos para oferecer abrigo. Você encontrou 45 de ouro, uma poção e 20 de experiência.");
        }
        else if (room == 45 && firstEchoQuest == 1)
        {
            firstEchoQuest = 2; ShowMemory("AS VOZES DA ESTRADA", "Você abre as comportas e as vozes voltam aos marcos. Volte a Edda em Véspera.");
        }
        else if (room == 44)
            ShowMemory("ABRIGO ANTIGO", "O registro e as provisões foram recolhidos. Ivara deixou a gruta aberta para os próximos viajantes.");
        else
            ShowMemory("COMPORTAS ABERTAS", "As vozes seguem com a água e os marcos podem responder novamente.");
    }

    private void DrawFirstEchoInterior(Graphics g)
    {
        using var ground = new SolidBrush(room == 44 ? Color.FromArgb(55, 60, 48) : Color.FromArgb(59, 67, 67));
        using var accent = new Pen(room == 44 ? Color.FromArgb(112, 115, 76) : Color.FromArgb(113, 155, 153), 5);
        g.FillRectangle(ground, 65, 108, 830, 445);
        using var floorSeam = new Pen(room == 44 ? Color.FromArgb(69, 75, 59) : Color.FromArgb(77, 88, 86));
        for (int y = 125; y < 550; y += 54)
        {
            g.DrawLine(floorSeam, 70, y, 890, y);
            for (int x = 100 + ((y / 54) % 2) * 40; x < 890; x += 80) g.DrawLine(floorSeam, x, y, x, y + 48);
        }
        for (int x = 130; x < 880; x += 125) g.DrawLine(accent, x, 115, x - 35, 175);
        using var obstacle = new SolidBrush(room == 44 ? Color.FromArgb(78, 83, 63) : Color.FromArgb(92, 99, 92));
        foreach (RectangleF wall in room == 44 ? FirstEchoCaveWalls : FirstEchoSluiceWalls)
        {
            g.FillRectangle(obstacle, wall);
            g.DrawRectangle(accent, wall.X, wall.Y, wall.Width, wall.Height);
        }
        g.DrawEllipse(accent, 710, 280, 100, 100);
        if (room == 45 && firstEchoQuest >= 2)
        {
            using var released = new Pen(Color.FromArgb(181, 122, 221, 205), 4);
            g.DrawLine(released, 710, 330, 810, 330);
            g.DrawArc(released, 725, 295, 70, 70, 200, 140);
        }
        using var font = new Font("Segoe UI", 11, FontStyle.Bold);
        g.DrawString(room == 44 ? "ABRIGO ANTIGO" : "MECANISMO DAS VOZES", font, Brushes.AntiqueWhite, 660, 250);
        if (player.X < 180) g.DrawString("E  VOLTAR ÀS MARGENS", font, Brushes.AntiqueWhite, 90, 380);
        if (Distance(player, new PointF(760, 330)) < 100) g.DrawString("E  EXAMINAR", font, Brushes.AntiqueWhite, 700, 390);
    }

    private void DrawSluiceWarden(Graphics g, Enemy enemy)
    {
        float x = enemy.Pos.X, y = enemy.Pos.Y;
        using var shadow = new SolidBrush(Color.FromArgb(110, 5, 12, 12));
        using var cloak = new SolidBrush(enemy.Flash > 0 ? Color.White : Color.FromArgb(48, 84, 83));
        using var armor = new SolidBrush(Color.FromArgb(103, 121, 110));
        using var light = new SolidBrush(Color.FromArgb(159, 228, 210));
        using var edge = new Pen(enemy == lockedTarget ? Color.Gold : Color.FromArgb(186, 177, 133), 3);
        g.FillEllipse(shadow, x - 27, y - 6, 54, 13);
        float windup = enemy.Windup > 0 && enemy.WindupMax > 0 ? 1 - enemy.Windup / enemy.WindupMax : 0;
        float sway = enemy.Moving ? System.MathF.Sin(enemy.AnimationTime * 10) * 3 : System.MathF.Sin(windupPulse * 2) * 1.5f;
        var pose = g.Save();
        g.TranslateTransform(sway, -windup * 4);
        PointF[] mantle = { new(x - 22, y - 66), new(x + 22, y - 66), new(x + 31, y - 5), new(x - 31, y - 5) };
        g.FillPolygon(cloak, mantle); g.DrawPolygon(edge, mantle);
        g.FillEllipse(armor, x - 18, y - 95, 36, 38); g.DrawEllipse(edge, x - 18, y - 95, 36, 38);
        g.FillRectangle(light, x - 11, y - 82, 22, 5);
        g.DrawLine(edge, x - 29, y - 53, x - 43 - windup * 6, y - 11 - windup * 16);
        g.DrawLine(edge, x + 29, y - 53, x + 43 + windup * 6, y - 11 - windup * 16);
        g.DrawEllipse(edge, x - 12, y - 48, 24, 24);
        g.DrawLine(edge, x - 8, y - 36, x + 8, y - 36);
        g.DrawLine(edge, x, y - 44, x, y - 28);
        g.Restore(pose);
    }

    private void DrawFirstEchoMap(Graphics g)
    {
        using var shade = new SolidBrush(Color.FromArgb(240, 13, 18, 17));
        using var road = new Pen(Color.FromArgb(174, 150, 105), 4);
        using var font = new Font("Segoe UI", 10, FontStyle.Bold);
        using var titleFont = new Font("Georgia", 18, FontStyle.Bold);
        g.FillRectangle(shade, 35, 95, 890, 500);
        g.DrawString("MARGENS DO PRIMEIRO ECO • VAU DE CINZA", titleFont, Brushes.Goldenrod, 170, 115);
        PointF Map(PointF p) => new(100 + p.X * 0.25f, 175 + p.Y * 0.25f);
        PointF[] main = { FirstEchoTownGate, FirstEchoCrossroads, FirstEchoSluice };
        PointF[] north = { FirstEchoCrossroads, FirstEchoCave };
        PointF[] south = { FirstEchoCrossroads, new(1400, 1050), new(1900, 1050), FirstEchoSluice };
        foreach (PointF[] path in new[] { main, north, south }) g.DrawLines(road, path.Select(Map).ToArray());
        if (firstEchoBeaconRestored)
        {
            PointF[] ashRoad = { FirstEchoSluice, new(2520, 700), FirstEchoFerryman, new(2930, 850), FirstEchoAshSignal };
            using var ashLine = new Pen(Color.FromArgb(164, 151, 133, 114), 4);
            g.DrawLines(ashLine, ashRoad.Select(Map).ToArray());
        }
        foreach (var landmark in new[] {
            (FirstEchoTownGate, "VÉSPERA"), (FirstEchoCrossroads, firstEchoQuest >= 2 ? "IVARA • DESCANSO" : "IVARA"),
            (FirstEchoCave, firstEchoQuest > 0 || (firstEchoDiscovered & 2) != 0 || firstEchoCaveFound ? "GRUTA" : "???"),
            (FirstEchoSluice, firstEchoQuest > 0 || (firstEchoDiscovered & 4) != 0 ? "COMPORTAS" : "???"),
            (FirstEchoBeacon, firstEchoBeaconAccepted || firstEchoBeaconRestored ? "MARCO DA ESCARPA" : "???"),
            (FirstEchoFerryman, firstEchoBeaconRestored ? "NILO • VAU DE CINZA" : "???"),
            (FirstEchoAshSignal, firstEchoFerryAccepted || firstEchoFerryRestored ? "SINAL DO VAU" : "???") })
        {
            PointF p = Map(landmark.Item1);
            g.FillEllipse(Brushes.Goldenrod, p.X - 5, p.Y - 5, 10, 10);
            g.DrawString(landmark.Item2, font, Brushes.AntiqueWhite, p.X - 28, p.Y - 27);
        }
        if (firstEchoQuest < 3 || firstEchoQuest == 3 && !firstEchoBeaconRestored || firstEchoBeaconRestored && !firstEchoFerryRestored)
        {
            PointF target = firstEchoQuest < 3 ? firstEchoQuest == 0 ? FirstEchoCrossroads : firstEchoQuest == 1 ? FirstEchoSluice : FirstEchoTownGate
                : !firstEchoBeaconAccepted ? FirstEchoIvara
                : !firstEchoBeaconRestored ? FirstEchoBeacon
                : !firstEchoFerryAccepted ? FirstEchoFerryman : FirstEchoAshSignal;
            PointF objective = Map(target);
            using var objectiveRing = new Pen(Color.FromArgb(255, 215, 105), 3);
            g.DrawEllipse(objectiveRing, objective.X - 13, objective.Y - 13, 26, 26);
        }
        foreach (Drop drop in drops)
        {
            PointF loot = Map(drop.Pos);
            g.FillRectangle(Brushes.LightCyan, loot.X - 3, loot.Y - 3, 6, 6);
        }
        if (firstEchoReedCache)
        {
            PointF cachePoint = Map(FirstEchoReedCache);
            g.FillEllipse(Brushes.LightSteelBlue, cachePoint.X - 5, cachePoint.Y - 5, 10, 10);
            g.DrawString("CAIXA", font, Brushes.AntiqueWhite, cachePoint.X - 20, cachePoint.Y + 8);
        }
        PointF here = Map(player);
        g.FillEllipse(Brushes.LightGreen, here.X - 7, here.Y - 7, 14, 14);
        string status = firstEchoQuest == 0 ? "PROCURE IVARA" : firstEchoQuest == 1 ? "SIGA ÀS COMPORTAS" : firstEchoQuest == 2 ? "RETORNE A EDDA" : !firstEchoBeaconAccepted && !firstEchoBeaconRestored ? "RETORNE A IVARA" : firstEchoBeaconAccepted && !firstEchoBeaconRestored ? "MARCO DA ESCARPA" : !firstEchoFerryAccepted && !firstEchoFerryRestored ? "SIGA A NILO • VAU DE CINZA" : firstEchoFerryAccepted && !firstEchoFerryRestored ? "REACENDA O SINAL DO VAU" : "MARGENS RESTABELECIDAS";
        g.DrawString(status, font, Brushes.Goldenrod, 350, 520);
        g.DrawString("VERDE: VOCÊ  •  CÍRCULO: OBJETIVO  •  AZUL: SAQUE  •  M/ESC: FECHAR", font, Brushes.AntiqueWhite, 125, 555);
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
        new(1273, 390, 14, 40), new(2180, 495, 150, 100),
        new(2500, 170, 125, 85), new(2860, 230, 175, 110), new(3140, 420, 90, 165),
        new(2510, 1120, 140, 100), new(2840, 1220, 200, 95)
    };

    private void DrawFirstEchoEnvironment(Graphics g)
    {
        using var meadow = new SolidBrush(Color.FromArgb(56, 67, 54));
        using var grove = new SolidBrush(Color.FromArgb(40, 59, 49));
        using var flooded = new SolidBrush(Color.FromArgb(52, 79, 78));
        using var ridge = new SolidBrush(Color.FromArgb(70, 74, 67));
        using var ash = new SolidBrush(Color.FromArgb(61, 57, 52));
        using var road = new Pen(Color.FromArgb(112, 99, 75), 112) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        using var track = new Pen(Color.FromArgb(149, 130, 94), 2) { DashStyle = DashStyle.Dash };
        g.FillRectangle(meadow, 0, 0, FirstEchoSize.Width, FirstEchoSize.Height);
        g.FillRectangle(grove, 650, 0, 850, 490);
        g.FillRectangle(flooded, 1250, 900, 1150, 500);
        g.FillRectangle(ash, 2420, 0, 780, 1400);
        PointF[] ridgeLine = { new(1790, 80), new(2370, 90), new(2370, 520), new(2190, 560), new(2070, 495), new(1880, 520), new(1780, 430) };
        g.FillPolygon(ridge, ridgeLine);
        using var grass = new Pen(Color.FromArgb(83, 100, 71), 2);
        using var reed = new Pen(Color.FromArgb(126, 139, 91), 2);
        for (int i = 0; i < 210; i++)
        {
            int x = 55 + (i * 179 % 2280), y = 120 + (i * 113 % 1150);
            Pen vegetation = x > 1250 && y > 900 ? reed : grass;
            g.DrawLine(vegetation, x, y, x - 4, y - 8);
            g.DrawLine(vegetation, x, y, x + 4, y - 11);
        }
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
        using var basalt = new SolidBrush(Color.FromArgb(83, 78, 70));
        using var basaltEdge = new Pen(Color.FromArgb(125, 111, 96), 3);
        foreach (RectangleF slab in new[] { new RectangleF(2500, 170, 125, 85), new(2860, 230, 175, 110), new(3140, 420, 90, 165), new(2510, 1120, 140, 100), new(2840, 1220, 200, 95) })
        {
            g.FillRectangle(basalt, slab); g.DrawRectangle(basaltEdge, slab.X, slab.Y, slab.Width, slab.Height);
        }
        PointF[] main = { FirstEchoTownGate, new(450, 700), FirstEchoCrossroads, new(1500, 700), new(1870, 700), FirstEchoSluice };
        PointF[] branch = { FirstEchoCrossroads, new(900, 450), FirstEchoCave };
        PointF[] returnPath = { new(1200, 700), new(1400, 1050), new(1900, 1050), new(2100, 700) };
        PointF[] escarpment = { FirstEchoSluice, new(2300, 570), new(2220, 470), FirstEchoBeacon };
        PointF[] ashRoad = { FirstEchoSluice, new(2520, 700), FirstEchoFerryman, new(2930, 850), FirstEchoAshSignal };
        foreach (PointF[] path in new[] { main, branch, returnPath, escarpment, ashRoad }) { g.DrawLines(road, path); g.DrawLines(track, path); }
        using var ashTrack = new Pen(Color.FromArgb(145, 123, 112, 95), 3) { DashStyle = DashStyle.Dash };
        g.DrawLines(ashTrack, ashRoad);
        using var ridgeEdge = new Pen(Color.FromArgb(110, 129, 120, 99), 3);
        g.DrawLines(ridgeEdge, ridgeLine);
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
        using var beaconStone = new SolidBrush(Color.FromArgb(105, 111, 103));
        g.FillEllipse(beaconStone, FirstEchoBeacon.X - 26, FirstEchoBeacon.Y - 30, 52, 60);
        g.DrawEllipse(edge, FirstEchoBeacon.X - 26, FirstEchoBeacon.Y - 30, 52, 60);
        using var beaconLight = new SolidBrush(firstEchoBeaconRestored ? Color.FromArgb(210, 142, 220, 199) : Color.FromArgb(170, 176, 151, 104));
        g.FillEllipse(beaconLight, FirstEchoBeacon.X - 7, FirstEchoBeacon.Y - 17, 14, 14);
        g.FillEllipse(Brushes.Black, 1005, 185, 90, 50);
        g.DrawArc(edge, 1005, 165, 90, 70, 180, 180);
        foreach (PointF marker in new[] { FirstEchoTownGate, FirstEchoCrossroads, FirstEchoCave, FirstEchoSluice })
        {
            g.FillRectangle(stone, marker.X - 16, marker.Y - 28, 32, 56);
            g.DrawRectangle(edge, marker.X - 16, marker.Y - 28, 32, 56);
            if (firstEchoQuest >= 2)
            {
                using var light = new SolidBrush(Color.FromArgb(210, 154, 225, 205));
                g.FillEllipse(light, marker.X - 5, marker.Y - 17, 10, 10);
            }
        }
        using var font = new Font("Segoe UI", 14, FontStyle.Bold);
        using var label = new SolidBrush(Color.FromArgb(232, 218, 182));
        g.DrawString("VÉSPERA", font, label, 75, 625);
        g.DrawString("ENCRUZILHADA DOS MARCOS", font, label, 760, 610);
        g.DrawString("GRUTA DAS RAÍZES", font, label, 975, 170);
        g.DrawString("CASA DAS COMPORTAS", font, label, 2100, 620);
        g.DrawString("MARGEM ALAGADA", font, label, 1360, 915);
        if (firstEchoQuest >= 3) g.DrawString("ESCARPA DO SINO", font, label, 1980, 245);
        if (firstEchoBeaconRestored) g.DrawString("VAU DE CINZA", font, label, 2660, 575);
        using var sign = new SolidBrush(Color.FromArgb(109, 101, 78));
        g.FillRectangle(sign, FirstEchoRoadSign.X - 10, FirstEchoRoadSign.Y - 22, 20, 40);
        g.DrawRectangle(edge, FirstEchoRoadSign.X - 10, FirstEchoRoadSign.Y - 22, 20, 40);
        if (Distance(player, FirstEchoRoadSign) < 75) g.DrawString("E  LER", font, label, FirstEchoRoadSign.X - 35, FirstEchoRoadSign.Y + 25);
        using var cache = new SolidBrush(Color.FromArgb(121, 87, 57));
        g.FillRectangle(cache, FirstEchoReedCache.X - 14, FirstEchoReedCache.Y - 9, 28, 18);
        g.DrawRectangle(edge, FirstEchoReedCache.X - 14, FirstEchoReedCache.Y - 9, 28, 18);
        if (Distance(player, FirstEchoReedCache) < 75) g.DrawString("E  EXAMINAR", font, label, FirstEchoReedCache.X - 55, FirstEchoReedCache.Y + 25);
        DrawTownPerson(g, FirstEchoIvara, "IVARA", Color.FromArgb(128, 105, 78), Distance(player, FirstEchoIvara) < InteractionRadius);
        if (firstEchoQuest >= 2 && Distance(player, FirstEchoIvara) < InteractionRadius)
            DrawCentered(g, "DESCANSO", FirstEchoIvara.Y - 55, new Font("Segoe UI", 9, FontStyle.Bold), Color.LightGreen, FirstEchoIvara.X);
        if (firstEchoBeaconRestored) DrawTownPerson(g, FirstEchoFerryman, "NILO", Color.FromArgb(91, 101, 111), Distance(player, FirstEchoFerryman) < InteractionRadius);
        foreach (PointF point in new[] { FirstEchoTownGate, FirstEchoCave, FirstEchoSluice })
            if (Distance(player, point) < 90) g.DrawString("E  INTERAGIR", font, label, point.X - 65, point.Y + 40);
        if (Distance(player, FirstEchoBeacon) < 85) g.DrawString("E  TOCAR", font, label, FirstEchoBeacon.X - 40, FirstEchoBeacon.Y + 43);
        if (Distance(player, FirstEchoFerryman) < InteractionRadius && firstEchoBeaconRestored) g.DrawString("E  FALAR", font, label, FirstEchoFerryman.X - 44, FirstEchoFerryman.Y + 40);
        if (Distance(player, FirstEchoAshSignal) < 90) g.DrawString("E  REACENDER", font, label, FirstEchoAshSignal.X - 68, FirstEchoAshSignal.Y + 45);
        using var signalStone = new SolidBrush(firstEchoFerryRestored ? Color.FromArgb(134, 217, 208) : Color.FromArgb(111, 103, 88));
        g.FillEllipse(signalStone, FirstEchoAshSignal.X - 23, FirstEchoAshSignal.Y - 25, 46, 50);
        g.DrawEllipse(edge, FirstEchoAshSignal.X - 23, FirstEchoAshSignal.Y - 25, 46, 50);
    }
}
