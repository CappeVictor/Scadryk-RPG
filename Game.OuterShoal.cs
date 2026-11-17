using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;

public sealed partial class Game
{
    private bool outerShoalLocation;
    private int outerShoalProgress;
    private int outerShoalCaveProgress;
    private static readonly SizeF OuterShoalSize = new(5000, 3000);
    private static readonly PointF OuterShoalEntrance = new(165, 1320);
    private static readonly PointF OuterShoalEntranceFromCoast = new(4050, 2840);
    private static readonly PointF OuterShoalGuide = new(790, 1300);
    private static readonly PointF[] OuterShoalMirrors = { new(1120, 630), new(2380, 470), new(3160, 1740) };
    private static readonly PointF OuterShoalHeart = new(3710, 2100);
    private static readonly PointF OuterShoalCaveGuide = new(4160, 1510);
    private static readonly PointF OuterShoalCaveInterior = new(4335, 1510);
    private static readonly PointF OuterShoalCaveGuardCenter = new(4500, 1510);
    private static readonly PointF OuterShoalCaveAltar = new(4740, 1510);
    private static readonly RectangleF[] OuterShoalChannels = { new(1430, 480, 610, 220), new(2470, 1020, 590, 220) };
    private static readonly RectangleF[] OuterShoalWalls =
    {
        new(460, 300, 220, 115), new(1280, 980, 180, 135), new(1880, 300, 180, 110),
        new(2720, 980, 235, 140), new(3520, 1470, 190, 160), new(760, 2080, 240, 125),
        new(2100, 2060, 270, 145)
    };
    private static readonly RectangleF[] OuterShoalCaveWalls =
    {
        new(4200, 900, 800, 90), new(4200, 1960, 800, 90), new(4900, 900, 100, 1160),
        new(4200, 990, 90, 390), new(4200, 1650, 90, 310), new(4370, 1160, 100, 75),
        new(4610, 1755, 155, 80), new(4800, 1170, 70, 130)
    };
    private static readonly RectangleF[] OuterShoalOpenWalls = OuterShoalWalls.Concat(OuterShoalCaveWalls).ToArray();
    private static readonly RectangleF[] OuterShoalSealedWalls = OuterShoalOpenWalls.Append(new RectangleF(4200, 1370, 100, 280)).ToArray();
    private RectangleF[] OuterShoalCurrentWalls() => outerShoalCaveProgress >= 2 ? OuterShoalOpenWalls : OuterShoalSealedWalls;

    private void SetupOuterShoal()
    {
        player = OuterShoalEntrance;
        SpawnOuterShoalGuards();
        SpawnOuterShoalCaveEncounters();
    }

    private void SpawnOuterShoalCaveEncounters()
    {
        if (outerShoalCaveProgress is 1 or 3)
        {
            (PointF Pos, EnemyKind Kind)[] guards = outerShoalCaveProgress == 1
                ? new[] { (new PointF(3955, 1415), EnemyKind.SaltEcho), (new PointF(4050, 1435), EnemyKind.Mage), (new PointF(3995, 1570), EnemyKind.Elite) }
                : new[] { (new PointF(4420, 1320), EnemyKind.NameMoth), (new PointF(4605, 1450), EnemyKind.Archer), (new PointF(4455, 1740), EnemyKind.SaltEcho) };
            foreach (var (pos, kind) in guards)
            {
                float hp = EnemyData(kind).HealthAt(53, townEnding) * DifficultyEnemyHealthMultiplier(difficultyTier) * 1.2f;
                EliteAffix affix = kind == EnemyKind.Elite ? EliteAffixForRoll(random.Next(7)) : EliteAffix.Armored;
                enemies.Add(new Enemy { Pos = pos, Kind = kind, Hp = hp, MaxHp = hp, Cooldown = 0.8f + (float)random.NextDouble(), SummonCooldown = 5.5f, SupportCooldown = 3, EliteAffix = affix });
            }
        }
        if (outerShoalCaveProgress == 5)
        {
            float hp = EnemyData(EnemyKind.SaltJailer).HealthAt(53, townEnding) * DifficultyEnemyHealthMultiplier(difficultyTier) * 1.35f;
            enemies.Add(new Enemy { Pos = OuterShoalCaveAltar, Kind = EnemyKind.SaltJailer, NameOverride = "CANTOR DAS POÇAS SURDAS", Hp = hp, MaxHp = hp, Cooldown = 1.25f, SummonCooldown = 5.5f, SupportCooldown = 2.8f });
        }
    }

    private static PointF OuterShoalCurrentAt(PointF position, float phase, bool bootsReduceCurrent)
    {
        float strength = bootsReduceCurrent ? 0.55f : 1f;
        float direction = MathF.Sin(phase * 0.45f) >= 0 ? 1 : -1;
        if (OuterShoalChannels[0].Contains(position)) return new PointF(direction * 34 * strength, 0);
        if (OuterShoalChannels[1].Contains(position)) return new PointF(0, -direction * 30 * strength);
        return PointF.Empty;
    }

    private void SpawnOuterShoalGuards()
    {
        if (outerShoalProgress == 0) return;
        for (int i = 0; i < OuterShoalMirrors.Length; i++)
        {
            if ((outerShoalProgress & (1 << (i + 1))) != 0) continue;
            if ((outerShoalProgress & (1 << (i + 7))) != 0) continue;
            PointF marker = OuterShoalMirrors[i];
            (PointF Offset, EnemyKind Kind)[] guards =
            {
                (new PointF(-112, -38), EnemyKind.SaltEcho),
                (new PointF(110, -26), EnemyKind.Archer),
                (new PointF(0, 116), i == 2 ? EnemyKind.Elite : EnemyKind.NameMoth)
            };
            foreach (var (offset, kind) in guards)
            {
                float hp = EnemyData(kind).HealthAt(53, townEnding) * DifficultyEnemyHealthMultiplier(difficultyTier) * 1.12f;
                EliteAffix affix = kind == EnemyKind.Elite ? EliteAffixForRoll(random.Next(7)) : EliteAffix.Armored;
                enemies.Add(new Enemy { Pos = Add(marker, offset), Kind = kind, Hp = hp, MaxHp = hp, Cooldown = 0.9f + (float)random.NextDouble(), SummonCooldown = 6, SupportCooldown = 3, EliteAffix = affix });
            }
        }
        if ((outerShoalProgress & 14) == 14 && (outerShoalProgress & 16) != 0 && (outerShoalProgress & 32) == 0)
        {
            float hp = EnemyData(EnemyKind.DeepSilence).HealthAt(53, townEnding) * DifficultyEnemyHealthMultiplier(difficultyTier) * 1.22f;
            enemies.Add(new Enemy { Pos = OuterShoalHeart, Kind = EnemyKind.DeepSilence, NameOverride = "VIGIA DO LEITO QUIETO", Hp = hp, MaxHp = hp, Cooldown = 1.2f, SummonCooldown = 6, SupportCooldown = 3 });
        }
    }

    private void InteractOuterShoal()
    {
        Drop? weaponDrop = drops.Where(drop => drop.Kind == "weapon" && Distance(player, drop.Pos) <= 75).OrderBy(drop => Distance(player, drop.Pos)).FirstOrDefault();
        if (weaponDrop is not null) { GameAudio.Play(GameAudio.Cue.Loot); Collect(weaponDrop); return; }
        if (Distance(player, OuterShoalEntrance) < 110)
        {
            if (enemies.Count > 0 || drops.Count > 0) { notice = "LIMPE O TRECHO E RECOLHA O SAQUE ANTES DE VOLTAR"; noticeTime = 2; return; }
            outerShoalLocation = false; saltFrontierReturnLocation = true; room = 49; SetupRoom();
            ShowMemory("RETORNO À COSTA", "As ilhas permanecem no mapa de Nemi. A corrente entre a costa e o leito raso continua aberta para a próxima travessia."); return;
        }
        if (Distance(player, OuterShoalGuide) < 105)
        {
            if (outerShoalProgress == 0)
            {
                outerShoalProgress = 1; SpawnOuterShoalGuards();
                ShowMemory("MAIRA, GUARDIÃ DO CAIS RASO", "A vazante trouxe três espelhos de pedra até estas ilhas. Cada um guarda uma nota que ajuda os barcos a encontrar água funda sem seguir os bancos falsos. Os ecos cercam os marcos; limpe cada margem e registre a nota. Não procure nomes: os espelhos só guardam o som da corrente.");
            }
            else if ((outerShoalProgress & 14) == 14 && (outerShoalProgress & 16) == 0)
                ShowMemory("MAIRA, GUARDIÃ DO CAIS RASO", "As três notas apontam para o leito quieto a leste. O Vigia mantém a corrente presa; desperte-o no marco de basalto quando estiver pronto.");
            else if ((outerShoalProgress & 32) != 0 && (outerShoalProgress & 64) == 0)
            {
                outerShoalProgress |= 64; gold += 360; AddXp(280); tideGlass = Math.Min(999, tideGlass + 3); potions++;
                ShowMemory("O LEITO DEVOLVE A PASSAGEM", "Maira registra as três notas como aviso de profundidade, não como ordem de viagem. A passagem fica aberta para qualquer barco que precise esperar a maré. Você recebe 360 de ouro, 280 de experiência, 3 sais vítreos e uma poção.");
            }
            else if ((outerShoalProgress & 64) != 0 && outerShoalCaveProgress == 0)
            {
                outerShoalCaveProgress = 1; SpawnOuterShoalCaveEncounters();
                ShowMemory("A GRUTA DA CORRENTE MORTA", "Quando o Vigia caiu, Maira ouviu um pulso sob o basalto. Uma gruta a leste guarda uma câmara que não acompanha a maré. Os ecos já se reuniram no caminho; limpe a entrada e descubra o que mantém o veio parado.");
            }
            else if (outerShoalCaveProgress is 1 or 3) ShowMemory("A GRUTA DA CORRENTE MORTA", "Os guardiões ainda ocupam a passagem. Limpe a entrada e, depois, o interior da gruta antes de tocar o altar.");
            else if (outerShoalCaveProgress == 2) ShowMemory("A GRUTA DA CORRENTE MORTA", "A sentinela externa caiu. O vão entre as pedras está livre; atravesse a abertura e siga até o altar no fundo.");
            else if (outerShoalCaveProgress == 4) ShowMemory("ALTAR DO VEIO PARADO", "A câmara está limpa. Pressione E junto ao altar para despertar o Cantor das Poças Surdas.");
            else if (outerShoalCaveProgress == 5) ShowMemory("CANTOR DAS POÇAS SURDAS", "A luta ainda ecoa no fundo da gruta. Aguarde a pausa entre as ondas e ataque quando o anel de basalto se abrir.");
            else if (outerShoalCaveProgress == 6)
            {
                outerShoalCaveProgress = 7; gold += 420; AddXp(300); tideGlass = Math.Min(999, tideGlass + 5); potions++; health = maxHealth;
                ShowMemory("A CÂMARA RESPIRA", "O altar solta o veio que mantinha a gruta imóvel. Maira registra a câmara como um abrigo de maré, e a comunidade oferece 420 de ouro, 300 de experiência, 5 sais vítreos e uma poção. A passagem permanece aberta para exploração.");
            }
            else if (outerShoalCaveProgress == 7) ShowMemory("GRUTA DA CORRENTE MORTA", "A corrente interior voltou a correr. Maira deixa a passagem aberta como abrigo; o altar não desperta o Cantor outra vez.");
            else if ((outerShoalProgress & 64) != 0) ShowMemory("CAIS RASO DAS ILHAS", "Os espelhos seguem audíveis quando a água recua. Maira deixa a travessia aberta para quem escolher voltar às ilhas.");
            else ShowMemory("MAIRA, GUARDIÃ DO CAIS RASO", $"Espelhos registrados: {System.Numerics.BitOperations.PopCount((uint)(outerShoalProgress & 14))}/3. Limpe os ecos em cada margem antes de tocar a pedra.");
            return;
        }
        if (Distance(player, OuterShoalCaveGuide) < 115)
        {
            if (outerShoalCaveProgress == 0) ShowMemory("FENDA SOB O BASALTO", "A abertura está selada por pressão. Maira talvez reconheça o pulso que vem de dentro.");
            else if (outerShoalCaveProgress == 1) ShowMemory("VÃO DA GRUTA", "Os ecos ainda guardam a entrada. Derrote-os para liberar a passagem.");
            else if (outerShoalCaveProgress == 2)
            {
                outerShoalCaveProgress = 3; player = OuterShoalCaveInterior; SpawnOuterShoalCaveEncounters();
                ShowMemory("CÂMARA DAS POÇAS SURDAS", "O som da maré desaparece atrás das pedras. Três ecos protegem o altar de basalto ao fundo.");
            }
            else if (outerShoalCaveProgress == 4) { player = OuterShoalCaveInterior; ShowMemory("CÂMARA DAS POÇAS SURDAS", "O caminho está livre. O altar profundo aguarda uma interação para despertar o Cantor."); }
            else ShowMemory("VÃO DA GRUTA", "A abertura leva à câmara que Maira mantém marcada no mapa das ilhas.");
            return;
        }
        for (int i = 0; i < OuterShoalMirrors.Length; i++)
        {
            PointF marker = OuterShoalMirrors[i];
            if (Distance(player, marker) >= 88) continue;
            int bit = 1 << (i + 1);
            if (outerShoalProgress == 0) ShowMemory("ESPELHO DE VAZANTE", "Maira está no cais raso. Fale com ela antes de registrar os três marcos.");
            else if ((outerShoalProgress & bit) != 0) ShowMemory("NOTA REGISTRADA", "O espelho devolve uma pausa curta entre duas correntes. O mapa marca esta margem como segura durante a vazante.");
            else if (enemies.Any(enemy => Distance(enemy.Pos, marker) < 205)) ShowMemory("ESPELHO GUARDADO", "Os ecos ainda cercam o marco. Limpe esta margem antes de tocar o espelho.");
            else
            {
                outerShoalProgress |= bit; gold += 55; AddXp(45);
                ShowMemory(i switch { 0 => "ESPELHO DO CANAL NORTE", 1 => "ESPELHO DA PRAIA DE VIDRO", _ => "ESPELHO DO LEITO FUNDO" }, "A nota se prende ao mapa sem indicar para onde alguém deve ir. Você recebe 55 de ouro e 45 de experiência.");
            }
            return;
        }
        if (Distance(player, OuterShoalHeart) < 115)
        {
            if ((outerShoalProgress & 14) != 14) { ShowMemory("MARCO DO LEITO QUIETO", "As três notas dos espelhos ainda não formam um compasso completo."); return; }
            if ((outerShoalProgress & 32) != 0) { ShowMemory("LEITO QUIETO", "A corrente voltou a mudar de altura. O Vigia não retorna quando a maré baixa."); return; }
            if ((outerShoalProgress & 16) == 0)
            {
                if (enemies.Count > 0 || drops.Count > 0) { ShowMemory("MARCO DO LEITO QUIETO", "Recolha o saque e afaste os ecos restantes antes de despertar o Vigia."); return; }
                outerShoalProgress |= 16; SpawnOuterShoalGuards();
                ShowMemory("O VIGIA DO LEITO QUIETO", "As três notas abrem uma fenda entre os bancos de basalto. O Vigia ergue a corrente contra você. Saia da marca, atravesse as ondas e ataque durante a pausa."); return;
            }
        }
        if (Distance(player, OuterShoalCaveAltar) < 115)
        {
            if (outerShoalCaveProgress == 4)
            {
                if (enemies.Count > 0 || drops.Count > 0) { ShowMemory("ALTAR DO VEIO PARADO", "Recolha o saque e afaste os ecos da câmara antes de tocar o altar."); return; }
                outerShoalCaveProgress = 5; SpawnOuterShoalCaveEncounters();
                ShowMemory("O CANTOR DAS POÇAS SURDAS", "O altar conserva o ritmo sem corrente. O Cantor ergue ondas circulares e cortes que atravessam a câmara; saia dos anéis, avance durante a pausa e ataque de lado."); return;
            }
            if (outerShoalCaveProgress == 5) ShowMemory("ALTAR DO VEIO PARADO", "O Cantor ainda protege a câmara.");
            else if (outerShoalCaveProgress >= 6) ShowMemory("ALTAR DO VEIO PARADO", "As poças voltaram a acompanhar a maré. Nenhum outro eco responde ao altar.");
            else ShowMemory("ALTAR DO VEIO PARADO", "Maira precisa abrir a trilha e os ecos da câmara precisam ser derrotados primeiro.");
            return;
        }
    }

    private string OuterShoalObjective()
    {
        if ((outerShoalProgress & 64) != 0 && outerShoalCaveProgress is > 0 and < 7) return outerShoalCaveProgress switch { 1 => "OPCIONAL: LIMPE OS GUARDIÕES DIANTE DA GRUTA", 2 => "OPCIONAL: ATRAVESSE O VÃO NO EXTREMO LESTE", 3 => "OPCIONAL: LIMPE A CÂMARA DAS POÇAS SURDAS", 4 => "OPCIONAL: DESPERTE O CANTOR NO ALTAR", 5 => "OPCIONAL: DERROTE O CANTOR DAS POÇAS SURDAS", _ => "OPCIONAL: RELATE A CÂMARA A MAIRA" };
        if (outerShoalProgress == 0) return "OBJETIVO: FALE COM MAIRA NO CAIS RASO";
        if ((outerShoalProgress & 14) != 14) return $"OBJETIVO: LIMPE AS MARGENS E REGISTRE OS ESPELHOS • {System.Numerics.BitOperations.PopCount((uint)(outerShoalProgress & 14))}/3";
        if ((outerShoalProgress & 32) == 0) return "OBJETIVO: DESPERTE O VIGIA NO LEITO QUIETO";
        if ((outerShoalProgress & 64) == 0) return "OBJETIVO: LEVE O COMPASSO DE VOLTA A MAIRA";
        if ((outerShoalProgress & 64) != 0 && outerShoalCaveProgress == 0) return "OPCIONAL: FALE COM MAIRA SOBRE O PULSO SOB O BASALTO";
        return "ILHAS DO ENTREFLUXO • PASSAGEM ABERTA • EXPLORAÇÃO LIVRE";
    }

    private string OuterShoalJournalStatus()
    {
        if (outerShoalCaveProgress == 7) return "Gruta registrada; altar silenciado e passagem aberta";
        if (outerShoalCaveProgress == 6) return "Cantor derrotado; leve o relato a Maira";
        if (outerShoalCaveProgress is > 0 and < 6) return $"Gruta da Corrente Morta • etapa {outerShoalCaveProgress}/5";
        if ((outerShoalProgress & 64) != 0) return "Maira ouviu outro pulso; fale com ela sobre a fenda";
        if ((outerShoalProgress & 32) != 0) return "Vigia derrotado; volte a Maira no cais raso";
        if ((outerShoalProgress & 14) == 14) return "Três notas registradas; desperte o Vigia";
        if (outerShoalProgress == 0) return "Nova rota; fale com Maira para começar";
        return $"Espelhos: {System.Numerics.BitOperations.PopCount((uint)(outerShoalProgress & 14))}/3 • correntes reversíveis";
    }

    private void DrawOuterShoalEnvironment(Graphics g)
    {
        using var sand = new SolidBrush(Color.FromArgb(44, 65, 63));
        using var wet = new SolidBrush(Color.FromArgb(41, 87, 91));
        using var stone = new SolidBrush(Color.FromArgb(70, 87, 84));
        using var rim = new Pen(Color.FromArgb(158, 166, 145, 105), 4);
        using var waterline = new Pen(Color.FromArgb(110, 129, 205, 199), 3) { DashStyle = DashStyle.Dash };
        using var currentLine = new Pen(Color.FromArgb(180, 151, 224, 220), 4);
        using var caveGround = new SolidBrush(Color.FromArgb(29, 39, 42));
        using var caveShade = new SolidBrush(Color.FromArgb(48, 12, 18, 21));
        using var caveRim = new Pen(Color.FromArgb(173, 126, 150, 137), 7);
        using var title = new Font("Georgia", 15, FontStyle.Bold);
        using var small = new Font("Segoe UI", 10, FontStyle.Bold);
        using var ink = new SolidBrush(Color.FromArgb(235, 229, 207));
        g.FillRectangle(sand, 70, 112, OuterShoalSize.Width - 140, OuterShoalSize.Height - 224);
        for (int y = 230; y < OuterShoalSize.Height - 180; y += 305)
            for (int x = 180 + ((y / 305) % 2) * 150; x < OuterShoalSize.Width - 200; x += 520)
            {
                g.FillEllipse(wet, x, y, 270, 135); g.DrawEllipse(waterline, x + 18, y + 15, 230, 105);
            }
        bool currentEast = MathF.Sin(windupPulse * 0.45f) >= 0;
        for (int lane = 0; lane < OuterShoalChannels.Length; lane++)
        {
            RectangleF channel = OuterShoalChannels[lane];
            for (int i = 0; i < 5; i++)
            {
                PointF start = lane == 0 ? new(channel.Left + 45 + i * 125, channel.Top + 78) : new(channel.Left + 155, channel.Top + 18 + i * 42);
                PointF end = lane == 0 ? new(start.X + (currentEast ? 52 : -52), start.Y) : new(start.X, start.Y + (currentEast ? 42 : -42));
                g.DrawLine(currentLine, start, end);
                float angle = MathF.Atan2(end.Y - start.Y, end.X - start.X);
                PointF tip = new(end.X + MathF.Cos(angle) * 10, end.Y + MathF.Sin(angle) * 10);
                PointF back = new(end.X - MathF.Cos(angle) * 10, end.Y - MathF.Sin(angle) * 10);
                PointF normal = new(-MathF.Sin(angle) * 6, MathF.Cos(angle) * 6);
                g.FillPolygon(Brushes.LightCyan, new[] { tip, Add(back, normal), Sub(back, normal) });
            }
        }
        foreach (RectangleF wall in OuterShoalWalls)
        {
            g.FillEllipse(stone, wall.X - 16, wall.Y - 10, wall.Width + 32, wall.Height + 20);
            g.DrawEllipse(rim, wall.X - 16, wall.Y - 10, wall.Width + 32, wall.Height + 20);
        }
        PointF[] caveShape = { new(4200, 990), new(4930, 1020), new(4880, 1240), new(4970, 1470), new(4860, 1800), new(4920, 1950), new(4200, 1960) };
        g.FillPolygon(caveGround, caveShape); g.DrawPolygon(caveRim, caveShape);
        g.FillEllipse(caveShade, 4260, 1100, 250, 180); g.FillEllipse(caveShade, 4520, 1680, 265, 170); g.FillEllipse(caveShade, 4760, 1240, 130, 230);
        for (int i = 0; i < 9; i++) { float x = 4320 + i * 67; g.DrawLine(caveRim, x, 1050 + (i % 3) * 14, x + 22, 1110 + (i % 3) * 14); }
        g.DrawString("GRUTA DA CORRENTE MORTA", small, ink, 4300, 1080);
        g.DrawString("ILHAS DO ENTREFLUXO", title, ink, 1650, 135);
        g.DrawString(currentEast ? "MARÉ • CORRENTE PARA LESTE / NORTE" : "MARÉ • CORRENTE PARA OESTE / SUL", small, ink, 1640, 170);
        for (int i = 0; i < OuterShoalMirrors.Length; i++)
        {
            PointF p = OuterShoalMirrors[i];
            bool recorded = (outerShoalProgress & (1 << (i + 1))) != 0;
            using var glow = new SolidBrush(recorded ? Color.FromArgb(100, 160, 221, 203) : Color.FromArgb(70, 125, 177, 174));
            g.FillEllipse(glow, p.X - 64, p.Y - 64, 128, 128); g.DrawEllipse(rim, p.X - 40, p.Y - 40, 80, 80);
            g.DrawLine(rim, p.X, p.Y - 34, p.X, p.Y + 34); g.DrawLine(rim, p.X - 26, p.Y, p.X + 26, p.Y);
            g.DrawString(recorded ? "NOTA REGISTRADA" : "ESPELHO DE VAZANTE", small, ink, p.X - 82, p.Y + 48);
        }
        DrawTownPerson(g, OuterShoalGuide, "MAIRA", Color.FromArgb(133, 159, 147), Distance(player, OuterShoalGuide) < InteractionRadius);
        g.FillEllipse(stone, OuterShoalHeart.X - 88, OuterShoalHeart.Y - 70, 176, 140); g.DrawEllipse(rim, OuterShoalHeart.X - 88, OuterShoalHeart.Y - 70, 176, 140);
        g.DrawString("LEITO QUIETO", small, ink, OuterShoalHeart.X - 52, OuterShoalHeart.Y + 82);
        if (Distance(player, OuterShoalEntrance) < 120) g.DrawString("E  VOLTAR À COSTA", small, ink, 80, 1375);
        if (Distance(player, OuterShoalGuide) < InteractionRadius) g.DrawString("E  FALAR", small, ink, OuterShoalGuide.X - 35, OuterShoalGuide.Y + 43);
        if (Distance(player, OuterShoalHeart) < 115 && (outerShoalProgress & 14) == 14 && (outerShoalProgress & 32) == 0) g.DrawString("E  DESPERTAR O VIGIA", small, ink, OuterShoalHeart.X - 80, OuterShoalHeart.Y + 104);
        if (outerShoalCaveProgress > 0 && Distance(player, OuterShoalCaveGuide) < InteractionRadius) g.DrawString("E  ATRAVESSAR A GRUTA", small, ink, OuterShoalCaveGuide.X - 95, OuterShoalCaveGuide.Y + 65);
        if (outerShoalCaveProgress is 4 or 5 && Distance(player, OuterShoalCaveAltar) < InteractionRadius) g.DrawString("E  TOCAR O ALTAR", small, ink, OuterShoalCaveAltar.X - 66, OuterShoalCaveAltar.Y + 58);
    }

    private void DrawOuterShoalMap(Graphics g)
    {
        using var shade = new SolidBrush(Color.FromArgb(238, 11, 16, 18));
        using var panel = new SolidBrush(Color.FromArgb(247, 19, 25, 26));
        using var line = new Pen(Color.FromArgb(188, 137, 172, 162), 4);
        using var thin = new Pen(Color.FromArgb(120, 92, 146, 142), 2);
        using var font = new Font("Segoe UI", 9, FontStyle.Bold);
        using var title = new Font("Georgia", 17, FontStyle.Bold);
        g.FillRectangle(shade, 0, 0, 960, 640); g.FillRectangle(panel, 46, 105, 868, 485); g.DrawRectangle(Pens.DarkGoldenrod, 46, 105, 868, 485);
        DrawCentered(g, "ILHAS DO ENTREFLUXO", 125, title, Color.Goldenrod);
        PointF Map(PointF p) => new(100 + p.X * 0.15f, 174 + p.Y * 0.15f);
        PointF[] path = { OuterShoalEntrance, new(520, 1240), OuterShoalGuide, new(1450, 920), OuterShoalMirrors[0], new(1750, 520), OuterShoalMirrors[1], new(2860, 960), OuterShoalMirrors[2], new(3500, 1900), OuterShoalHeart };
        g.DrawLines(line, path.Select(Map).ToArray());
        g.DrawLine(thin, Map(new PointF(400, 1000)), Map(new PointF(780, 1540)));
        g.DrawLine(thin, Map(new PointF(1800, 820)), Map(new PointF(2200, 1250)));
        g.DrawLine(thin, Map(new PointF(2650, 1340)), Map(new PointF(3060, 2220)));
        PointF[] marks = { OuterShoalEntrance, OuterShoalGuide, OuterShoalMirrors[0], OuterShoalMirrors[1], OuterShoalMirrors[2], OuterShoalHeart };
        string[] labels = { "COSTA", "MAIRA", "CANAL NORTE", "PRAIA DE VIDRO", "LEITO FUNDO", "VIGIA" };
        for (int i = 0; i < marks.Length; i++)
        {
            PointF p = Map(marks[i]); bool done = i is 2 or 3 or 4 ? (outerShoalProgress & (1 << (i - 1))) != 0 : i == 5 && (outerShoalProgress & 32) != 0;
            g.FillEllipse(i == 0 ? Brushes.LightCyan : done ? Brushes.SlateGray : i == 5 && (outerShoalProgress & 14) == 14 ? Brushes.IndianRed : Brushes.Goldenrod, p.X - 7, p.Y - 7, 14, 14);
            g.DrawString(labels[i], font, Brushes.AntiqueWhite, p.X - 36, p.Y + 10);
        }
        if ((outerShoalProgress & 64) != 0)
        {
            PointF cave = Map(OuterShoalCaveGuide);
            g.DrawLines(line, new[] { OuterShoalHeart, new PointF(3980, 1820), OuterShoalCaveGuide }.Select(Map).ToArray());
            g.FillEllipse(outerShoalCaveProgress == 7 ? Brushes.LightGreen : Brushes.MediumPurple, cave.X - 7, cave.Y - 7, 14, 14);
            g.DrawString("GRUTA", font, Brushes.AntiqueWhite, cave.X - 28, cave.Y + 10);
        }
        using var flow = new Pen(Color.FromArgb(235, 119, 221, 215), 3);
        foreach (RectangleF channel in OuterShoalChannels)
        {
            PointF center = new(channel.Left + channel.Width / 2, channel.Top + channel.Height / 2);
            PointF drift = OuterShoalCurrentAt(center, windupPulse, false);
            PointF start = Map(Sub(center, Scale(Normalize(drift, new PointF(1, 0)), 115)));
            PointF end = Map(Add(center, Scale(Normalize(drift, new PointF(1, 0)), 115)));
            g.DrawLine(flow, start, end);
            float angle = MathF.Atan2(end.Y - start.Y, end.X - start.X);
            PointF tip = Add(end, Scale(new PointF(MathF.Cos(angle), MathF.Sin(angle)), 9));
            PointF back = Sub(end, Scale(new PointF(MathF.Cos(angle), MathF.Sin(angle)), 9));
            PointF normal = new(-MathF.Sin(angle) * 5, MathF.Cos(angle) * 5);
            g.FillPolygon(Brushes.LightCyan, new[] { tip, Add(back, normal), Sub(back, normal) });
        }
        PointF here = Map(player); g.FillEllipse(Brushes.LightGreen, here.X - 7, here.Y - 7, 14, 14); g.DrawEllipse(Pens.Black, here.X - 7, here.Y - 7, 14, 14);
        g.DrawString("JOGADOR", font, Brushes.LightGreen, here.X + 8, here.Y - 8);
        g.DrawString("SETAS: SENTIDO ATUAL DAS CORRENTES • GREVAS CONTRAMARÉ REDUZEM O EMPURRÃO", font, Brushes.LightCyan, 115, 530);
        g.DrawString("E  FECHAR MAPA", font, Brushes.DarkGray, 390, 555);
    }
}
