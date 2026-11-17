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
    private bool saltFrontierWardenDefeated;
    private int saltFrontierSideProgress;
    private bool saltFrontierCacheFound;
    private static readonly SizeF SaltFrontierSize = new(4200, 3000);
    private static readonly PointF SaltFrontierEntrance = new(155, 1200);
    private static readonly PointF SaltFrontierCartographer = new(2110, 1175);
    private static readonly PointF[] SaltFrontierTideBells = { new(850, 475), new(2330, 405), new(3570, 1700) };
    private static readonly PointF SaltFrontierBellheart = new(3860, 1885);
    private static readonly PointF SaltFrontierTeren = new(3550, 425);
    private static readonly PointF SaltFrontierWreck = new(3930, 675);
    private static readonly PointF SaltFrontierBuoys = new(3150, 2140);
    private static readonly PointF SaltFrontierCache = new(1380, 2035);
    private static readonly PointF SaltFrontierOrva = new(2700, 2580);
    private static readonly PointF SaltFrontierAnchorMarker = new(3540, 2740);
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
        SpawnSaltFrontierBuoyGuardians();
        SpawnSaltFrontierAnchorEncounter();
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

    private void SpawnSaltFrontierBuoyGuardians()
    {
        if (!saltFrontierBossDefeated || saltFrontierSideProgress != 4 || saltFrontierWardenDefeated || enemies.Count > 0) return;
        (PointF Pos, EnemyKind Kind)[] guards =
        {
            (new PointF(3030, 2090), EnemyKind.SaltEcho),
            (new PointF(3270, 2070), EnemyKind.Archer),
            (new PointF(3060, 2260), EnemyKind.Mage)
        };
        foreach (var (position, kind) in guards)
        {
            float hp = EnemyData(kind).HealthAt(42, townEnding) * DifficultyEnemyHealthMultiplier(difficultyTier);
            enemies.Add(new Enemy { Pos = position, Kind = kind, Hp = hp, MaxHp = hp, Cooldown = 1, SummonCooldown = 6, SupportCooldown = 3, EliteAffix = EliteAffix.Armored });
        }
        float bossHp = EnemyData(EnemyKind.TideWitness).HealthAt(42, townEnding) * DifficultyEnemyHealthMultiplier(difficultyTier) * 1.18f;
        enemies.Add(new Enemy { Pos = SaltFrontierBuoys, Kind = EnemyKind.TideWitness, NameOverride = "VIGIA DAS BÓIAS", Hp = bossHp, MaxHp = bossHp, Cooldown = 1.35f, SummonCooldown = 6, SupportCooldown = 3 });
    }

    private void SpawnSaltFrontierAnchorEncounter()
    {
        if (saltFrontierSideProgress == 7 && enemies.Count == 0)
        {
            (PointF Pos, EnemyKind Kind)[] guards =
            {
                (new PointF(3370, 2640), EnemyKind.SaltEcho),
                (new PointF(3710, 2630), EnemyKind.Archer),
                (new PointF(3550, 2870), EnemyKind.Elite)
            };
            foreach (var (position, kind) in guards)
            {
                float hp = EnemyData(kind).HealthAt(42, townEnding) * DifficultyEnemyHealthMultiplier(difficultyTier);
                EliteAffix affix = kind == EnemyKind.Elite ? EliteAffixForRoll(random.Next(6)) : EliteAffix.Armored;
                enemies.Add(new Enemy { Pos = position, Kind = kind, Hp = hp, MaxHp = hp, Cooldown = 0.9f + (float)random.NextDouble(), SummonCooldown = 6, SupportCooldown = 3, EliteAffix = affix, Armored = kind == EnemyKind.Elite && affix == EliteAffix.Armored });
            }
        }
        if (saltFrontierSideProgress == 8 && enemies.Count == 0)
        {
            float hp = EnemyData(EnemyKind.TideWitness).HealthAt(42, townEnding) * DifficultyEnemyHealthMultiplier(difficultyTier) * 1.25f;
            enemies.Add(new Enemy { Pos = SaltFrontierAnchorMarker, Kind = EnemyKind.TideWitness, NameOverride = "GUARDIÃO DO LASTRO", Hp = hp, MaxHp = hp, Cooldown = 1.3f, SummonCooldown = 6, SupportCooldown = 3 });
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
            if (drops.Count > 0) { notice = "RECOLHA TODO O SAQUE ANTES DE VOLTAR À BACIA"; noticeTime = 2.2f; return; }
            StoreSaltFrontierOutside(); saltFrontierReturnLocation = false; room = 46; SetupRoom(); RestoreSaltBasinOutside(); player = SaltBasinFrontierEntrance;
            ShowMemory("RETORNO À COSTA DAS BADALADAS", "A passagem entre o sal e a maré permanece aberta. As notas que você reuniu continuam marcadas no mapa de Nemi."); return;
        }
        if (Distance(player, SaltFrontierCartographer) < 100)
        {
            if (saltFrontierSideProgress == 10)
            {
                saltFrontierSideProgress = 11; gold += 420; AddXp(320); tideGlass += 3; potions++; health = Math.Min(maxHealth, health + 60);
                ShowMemory("O CAMPO QUE CEDE", "Nemi compara o entalhe do baixio com as três badaladas. As âncoras não prendiam barcos: indicavam onde a corrente perde força e devolve a escolha a quem atravessa. Orva vai manter o marco visível para quem precisar esperar. O registro costeiro recebe uma nova rota; você recebe 420 de ouro, 320 de experiência, 3 sais vítreos e uma poção."); return;
            }
            if (saltFrontierSideProgress == 9) { ShowMemory("NEMI, A ESCUTADORA", "O marco do baixio está pronto para ser registrado. Traga o entalhe até o observatório para que outras pessoas encontrem a enseada sem seguir uma ordem de travessia."); return; }
            if (saltFrontierSideProgress == 5)
            {
                saltFrontierSideProgress = 6; gold += 320; AddXp(240); tideGlass += 3; potions++; health = Math.Min(maxHealth, health + 45);
                ShowMemory("AS BÓIAS QUE NÃO CHAMAM", "O Vigia não guardava um caminho. As bóias marcavam apenas o momento em que a corrente deixava de empurrar os barcos contra os recifes; quem chegava podia escolher atravessar, esperar ou voltar. Nemi registra o ritmo sem desenhar uma rota obrigatória. A comunidade oferece 320 de ouro, 240 de experiência, 3 sais vítreos e uma poção."); return;
            }
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
                ShowMemory("O COMPASSO DA COSTA", "Nemi compara os três registros e reconhece uma pausa que não pertence à torre: alguém interrompeu a badalada para deixar os viajantes escolherem o próprio caminho. Ela copia a pausa no mapa, sem tentar preencher o silêncio. Também marca um estojo de provisões sob uma laje na borda sudoeste; ele foi deixado para qualquer pessoa que precisasse esperar a maré. Sob o juncal, outra voz ainda pulsa no coração da torre submersa. Você recebe 175 de ouro, 125 de experiência e descansa por completo."); return;
            }
            ShowMemory("NEMI, A ESCUTADORA", (saltFrontierProgress & 16) != 0 ? saltFrontierBossDefeated ? saltFrontierSideProgress switch { 0 => "O Coro se desfez sem levar nenhum nome. Teren está examinando um destroço ao norte da costa; talvez aquele mapa de corrente ajude os barqueiros a escolher uma travessia segura.", 1 => "Teren espera no caminho ao norte. Os guardiões do destroço ainda protegem o registro da maré.", 2 => "Você recuperou o desenho do destroço. Nemi pode comparar a marca com os intervalos das três badaladas.", 3 => "O destroço foi registrado. Teren encontrou uma fileira de bóias ao sul; fale com ele se quiser investigar a corrente que as move.", 4 => saltFrontierWardenDefeated ? "O Vigia caiu. Recolha o talismã e examine o marcador junto às bóias." : "As bóias ainda estão cercadas. O Vigia usa o ritmo da maré para cortar a passagem.", 5 => "O ritmo está registrado; volte ao observatório para concluir o relato.", 6 => "As bóias foram registradas. Orva está no baixio ao sul, onde um campo de âncoras antigas aparece na vazante.", 7 => "Orva espera no Campo das Âncoras. Os guardiões cercam o marco no extremo sul.", 8 => "Os guardiões cederam. Examine o marco no extremo sul; algo ainda o mantém preso ao leito.", 9 => "O marco do baixio está pronto para ser registrado aqui.", 10 => "O Campo das Âncoras está no atlas. A enseada fica aberta para quem quiser explorar a costa no próprio ritmo.", _ => "O Coro e o Vigia foram silenciados; as marcas mostram quando esperar, atravessar ou retornar, sem decidir por ninguém." } : "As três notas já não chamam viajantes. Resta uma voz presa no coração da torre submersa, ao sul do juncal; ela precisa ser enfrentada para que a costa descanse." : $"Badaladas silenciadas: {System.Numerics.BitOperations.PopCount((uint)(saltFrontierProgress & 14))}/3. Aproxime-se de cada torre depois de limpar seus guardiões."); return;
        }
        if (Distance(player, SaltFrontierCache) < 90)
        {
            if ((saltFrontierProgress & 16) == 0) { ShowMemory("BORDA SUDOESTE", "Nemi ainda não marcou esta margem no compasso da costa."); return; }
            if (saltFrontierCacheFound) { ShowMemory("ESTOJO DO INTERVALO", "O estojo já foi aberto. As provisões foram divididas entre os barqueiros que escolheram permanecer na costa."); return; }
            saltFrontierCacheFound = true; tideBoots = tideBootsOn = true; gold += 180; AddXp(120); tideGlass++;
            ShowMemory("ESTOJO DO INTERVALO", "Sob uma laje de basalto, você encontra um estojo deixado por barqueiros que precisavam esperar a maré: 180 de ouro, 120 de experiência, 1 sal vítreo e as Grevas de Ressaca. Equipadas, elas reduzem em 45% o empurrão das correntes costeiras; ajuste-as com F8 no inventário. Nemi registra o achado no atlas."); return;
        }
        if (saltFrontierBossDefeated && Distance(player, SaltFrontierTeren) < 95)
        {
            if (saltFrontierSideProgress == 0)
            {
                saltFrontierSideProgress = 1; SpawnSaltFrontierWreckGuardians();
                ShowMemory("TEREN, PILOTO DE MARÉ", "Quando o Coro silenciou, o banco ao norte cedeu e revelou um destroço. Meu mapa ainda vibra, mas não marca nomes: mostra um trecho onde a água recua antes de virar. Os ecos cercaram o casco. Se quiser ajudar, limpe a área e recupere o desenho; Nemi saberá registrar a passagem."); return;
            }
            if (saltFrontierSideProgress == 3)
            {
                saltFrontierSideProgress = 4; SpawnSaltFrontierBuoyGuardians();
                ShowMemory("TEREN, PILOTO DE MARÉ", "Ao sul do juncal há três bóias de ferro que pararam de oscilar. A corrente mudou desde a última estação; uma coisa enorme mantém as amarras tensas. Se quiser conferir o ritmo, siga a trilha até o banco baixo e limpe a área."); return;
            }
            if (saltFrontierSideProgress == 4) { ShowMemory("TEREN, PILOTO DE MARÉ", saltFrontierWardenDefeated ? "O Vigia caiu. Recolha o talismã que deixou e examine o marcador de pedra junto às bóias." : "As bóias continuam sob guarda. A fileira fica ao sul, onde a costa submersa encontra o banco de basalto."); return; }
            if (saltFrontierSideProgress == 5) { ShowMemory("TEREN, PILOTO DE MARÉ", "Nemi pode comparar o ritmo das bóias com as badaladas da torre. Leve o registro ao observatório."); return; }
            if (saltFrontierSideProgress == 6) { ShowMemory("TEREN, PILOTO DE MARÉ", "Agora os barqueiros sabem quando a corrente cede, mas cada barco escolhe seu próprio momento de partir."); return; }
            ShowMemory("TEREN, PILOTO DE MARÉ", saltFrontierSideProgress == 1 ? "Os guardiões ainda rondam o destroço. Aproxime-se pelo caminho de basalto e recolha qualquer saque antes de examinar o desenho." : saltFrontierSideProgress == 2 ? "O desenho está com você. Leve-o a Nemi para comparar os intervalos; ele não aponta quem passou nem para onde." : "O mapa agora mostra apenas onde a água recua. Nenhuma rota é atribuída a quem escolheu atravessar."); return;
        }
        if (saltFrontierSideProgress >= 6 && Distance(player, SaltFrontierOrva) < 105)
        {
            if (saltFrontierSideProgress == 6)
            {
                saltFrontierSideProgress = 7; SpawnSaltFrontierAnchorEncounter();
                ShowMemory("ORVA, GUARDIÃ DO BAIXIO", "As âncoras aparecem só quando a água baixa. Não são de barcos: alguém as fincou para marcar onde a corrente larga o fundo. Os ecos prenderam um guardião ao marco. Se quiser, abra caminho até lá; depois decidiremos se essa enseada merece entrar no atlas."); return;
            }
            ShowMemory("ORVA, GUARDIÃ DO BAIXIO", saltFrontierSideProgress switch { 7 => "Os guardiões ainda cercam o campo ao sul. O caminho de pedra clara acompanha as piscinas rasas até o marco.", 8 => "O Guardião do Lastro ainda protege o marco. A enseada fica além das três âncoras menores.", 9 => "O campo está livre. Examine o entalhe central e leve o registro a Nemi.", 10 => "Nemi está esperando o registro no observatório.", _ => "O baixio está no atlas. Os barcos podem chegar, esperar ou voltar quando quiserem." }); return;
        }
        if (saltFrontierSideProgress is >= 7 and <= 10 && Distance(player, SaltFrontierAnchorMarker) < 120)
        {
            if (saltFrontierSideProgress == 7)
            {
                if (enemies.Count > 0 || drops.Count > 0) { ShowMemory("CAMPO DAS ÂNCORAS", "As três âncoras menores ainda estão guardadas. Limpe o baixio e recolha o saque antes de tocar o marco central."); return; }
                saltFrontierSideProgress = 8; SpawnSaltFrontierAnchorEncounter();
                ShowMemory("O MARCO SOB A ÁGUA", "Quando a última sentinela cai, a corrente recua por um instante. Uma figura de ferro se ergue do leito, ainda presa à âncora maior."); return;
            }
            if (saltFrontierSideProgress == 8) { ShowMemory("CAMPO DAS ÂNCORAS", "O Guardião do Lastro ainda está de pé. Leia o ritmo das ondas, saia da área marcada e atravesse o corte anunciado."); return; }
            if (saltFrontierSideProgress == 9)
            {
                saltFrontierSideProgress = 10;
                ShowMemory("O ENTALHE DO BAIXIO", "Sob a ferrugem, três linhas mostram a corrente soltando o leito, recuando e descansando. O marco não aponta uma direção. Ele deixa qualquer barco escolher quando atravessar. Leve o entalhe a Nemi para registrá-lo no atlas."); return;
            }
            ShowMemory("O ENTALHE DO BAIXIO", "O registro já está com Nemi. O marco permanece visível quando a água baixa."); return;
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
        if (saltFrontierSideProgress >= 4 && Distance(player, SaltFrontierBuoys) < 115)
        {
            if (saltFrontierSideProgress >= 5) { ShowMemory("AS BÓIAS DE FERRO", "O registro das bóias já foi recolhido. Ele marca a mudança da corrente, não uma direção a seguir."); return; }
            if (!saltFrontierWardenDefeated) { ShowMemory("FILEIRA DAS BÓIAS", "Derrote o Vigia das Bóias e recolha o talismã que guarda o ritmo antes de examinar o marcador."); return; }
            if (enemies.Any(enemy => Distance(enemy.Pos, SaltFrontierBuoys) < 300)) { ShowMemory("FILEIRA DAS BÓIAS", "O Vigia mantém as amarras sob tensão. Derrote-o e limpe o banco antes de examinar o marcador."); return; }
            if (drops.Any(drop => Distance(drop.Pos, SaltFrontierBuoys) < 300)) { ShowMemory("FILEIRA DAS BÓIAS", "O saque ainda está entre as amarras. Recolha-o antes de examinar o marcador de pedra."); return; }
            saltFrontierSideProgress = 5;
            ShowMemory("O INTERVALO ENTRE AS BÓIAS", "O marcador não indica norte ou sul. Três entalhes mostram como a água afrouxa, volta a puxar e enfim descansa. Ninguém precisa obedecer a esse ritmo; ele apenas ajuda cada barqueiro a escolher quando quer sair."); return;
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
        if (saltFrontierSideProgress == 3) return "OPCIONAL: FALE COM TEREN SOBRE A FILEIRA DAS BÓIAS";
        if (saltFrontierSideProgress == 4) return saltFrontierWardenDefeated ? "OPCIONAL: RECOLHA O TALISMÃ E EXAMINE O MARCADOR DAS BÓIAS" : "OPCIONAL: DERROTE O VIGIA DAS BÓIAS E RECUPERE O RITMO";
        if (saltFrontierSideProgress == 5) return "OPCIONAL: LEVE O RITMO DAS BÓIAS A NEMI";
        if (saltFrontierSideProgress == 6) return "OPCIONAL: FALE COM ORVA NO BAIXIO AO SUL";
        if (saltFrontierSideProgress == 7) return enemies.Count > 0 ? "OPCIONAL: LIMPE O CAMPO DAS ÂNCORAS AO SUL" : "OPCIONAL: EXAMINE O MARCO NO EXTREMO SUL";
        if (saltFrontierSideProgress == 8) return "OPCIONAL: DERROTE O GUARDIÃO DO LASTRO";
        if (saltFrontierSideProgress == 9) return "OPCIONAL: EXAMINE O MARCO DO BAIXIO";
        if (saltFrontierSideProgress == 10) return "OPCIONAL: LEVE O ENTALHE A NEMI";
        return "COSTA DAS BADALADAS • ROTAS REGISTRADAS • EXPLORAÇÃO LIVRE";
    }

    private string SaltFrontierJournalStatus() => saltFrontierSideProgress switch
    {
        0 => saltFrontierBossDefeated ? "Opcional: fale com Teren no caminho ao norte" : "Coro ainda guarda o coração submerso",
        1 => "Destroço das Conchas: guardiões ativos",
        2 => "Registro recuperado; relate a Nemi",
        3 => "Rota do destroço registrada; fale com Teren sobre as bóias",
        4 => saltFrontierWardenDefeated ? "Vigia derrotado; recupere o talismã e examine o marcador" : "Fileira das Bóias: guardada pelo Vigia",
        5 => "Ritmo das bóias recuperado; relate a Nemi",
        6 => "Bóias registradas; Orva aguarda no baixio ao sul",
        7 => "Campo das Âncoras: três sentinelas no baixio",
        8 => "Sentinelas vencidas; Guardião do Lastro desperto",
        9 => "Guardião vencido; examine o marco do baixio",
        10 => "Entalhe recuperado; relate a Nemi",
        _ => "Campo das Âncoras registrado por Nemi"
    };

    private void DrawSaltFrontierEnvironment(Graphics g)
    {
        using var ground = new SolidBrush(Color.FromArgb(43, 60, 66));
        using var basalt = new SolidBrush(Color.FromArgb(55, 67, 70));
        using var marsh = new SolidBrush(Color.FromArgb(41, 78, 77));
        using var tidalFlat = new SolidBrush(Color.FromArgb(111, 120, 106));
        using var shoal = new SolidBrush(Color.FromArgb(74, 106, 101));
        using var saltPool = new SolidBrush(Color.FromArgb(133, 158, 148));
        using var road = new Pen(Color.FromArgb(119, 113, 92), 94) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        using var trail = new Pen(Color.FromArgb(202, 179, 130), 2) { DashStyle = DashStyle.Dash };
        using var current = new Pen(Color.FromArgb(170, 132, 202, 206), 5);
        using var flowFont = new Font("Segoe UI", 10, FontStyle.Bold);
        using var flowInk = new SolidBrush(Color.FromArgb(231, 224, 198));
        using var stone = new SolidBrush(Color.FromArgb(83, 101, 99));
        using var rim = new Pen(Color.FromArgb(143, 169, 156), 3);
        g.FillRectangle(ground, 0, 0, SaltFrontierSize.Width, SaltFrontierSize.Height);
        g.FillPolygon(basalt, new[] { new PointF(0, 0), new(1350, 0), new(1110, 680), new(0, 920) });
        g.FillPolygon(marsh, new[] { new PointF(1250, 0), new(2890, 0), new(2670, 870), new(1330, 1210), new(1080, 730) });
        g.FillPolygon(tidalFlat, new[] { new PointF(2530, 860), new(4200, 650), new(4200, 2400), new(2720, 2400), new(2450, 1720) });
        g.FillPolygon(shoal, new[] { new PointF(840, 2370), new(4200, 2370), new(4200, 3000), new(930, 3000), new(1140, 2730) });
        g.FillEllipse(saltPool, 1290, 2470, 320, 180); g.FillEllipse(saltPool, 1820, 2735, 245, 125); g.FillEllipse(saltPool, 3020, 2440, 370, 195);
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
        bool currentToCoast = MathF.Sin(windupPulse * 0.45f) >= 0;
        PointF[] currentMarks = { new(2840, 2025), new(3020, 1980), new(3210, 1995), new(3400, 1935), new(3560, 1815) };
        for (int i = 0; i < currentMarks.Length - 1; i++)
        {
            PointF a = currentMarks[i], b = currentMarks[i + 1];
            if (!currentToCoast) (a, b) = (b, a);
            g.DrawLine(current, a, b);
            PointF mid = new((a.X + b.X) * 0.5f, (a.Y + b.Y) * 0.5f);
            float angle = MathF.Atan2(b.Y - a.Y, b.X - a.X), side = 11;
            PointF tip = Add(mid, Scale(new PointF(MathF.Cos(angle), MathF.Sin(angle)), side));
            PointF back = Add(mid, Scale(new PointF(MathF.Cos(angle), MathF.Sin(angle)), -side));
            PointF normal = new(-MathF.Sin(angle) * 7, MathF.Cos(angle) * 7);
            g.FillPolygon(Brushes.LightCyan, new[] { tip, Add(back, normal), Sub(back, normal) });
        }
        g.DrawString(currentToCoast ? "CORRENTE RUMO À COSTA" : "CORRENTE RUMO AO JUNCAL", flowFont, flowInk, 3040, 1865);
        if (saltFrontierBossDefeated)
        {
            PointF[] wreckPath = { new(3000, 790), new(3400, 760), SaltFrontierTeren, new(3770, 520), SaltFrontierWreck };
            g.DrawLines(road, wreckPath); g.DrawLines(trail, wreckPath);
        }
        if (saltFrontierSideProgress >= 4)
        {
            PointF[] buoyPath = { SaltFrontierTideBells[2], new(3430, 1920), SaltFrontierBuoys };
            g.DrawLines(road, buoyPath); g.DrawLines(trail, buoyPath);
        }
        if (saltFrontierSideProgress >= 6)
        {
            PointF[] anchorPath = { SaltFrontierTideBells[1], new(2210, 1780), new(2370, 2230), SaltFrontierOrva, new(3130, 2660), SaltFrontierAnchorMarker };
            g.DrawLines(road, anchorPath); g.DrawLines(trail, anchorPath);
            g.DrawString("BAIXIO DAS ÂNCORAS", labelFont, ink, 1260, 2410);
            DrawTownPerson(g, SaltFrontierOrva, "ORVA", Color.FromArgb(139, 159, 128), Distance(player, SaltFrontierOrva) < InteractionRadius);
            if (Distance(player, SaltFrontierOrva) < InteractionRadius) g.DrawString("E  FALAR", smallFont, ink, SaltFrontierOrva.X - 40, SaltFrontierOrva.Y + 45);
            using var anchorPen = new Pen(Color.FromArgb(102, 131, 132), 9);
            using var anchorGlow = new SolidBrush(saltFrontierSideProgress >= 10 ? Color.LightCyan : saltFrontierSideProgress >= 7 ? Color.Goldenrod : Color.FromArgb(90, 124, 119));
            foreach (PointF anchor in new[] { new PointF(3370, 2640), new PointF(3710, 2630), new PointF(3550, 2870) })
            {
                g.DrawLine(anchorPen, anchor.X, anchor.Y + 27, anchor.X, anchor.Y - 23); g.DrawArc(anchorPen, anchor.X - 15, anchor.Y - 28, 30, 35, 175, 190);
                g.FillEllipse(anchorGlow, anchor.X - 4, anchor.Y - 22, 8, 8);
            }
            g.DrawEllipse(rim, SaltFrontierAnchorMarker.X - 38, SaltFrontierAnchorMarker.Y - 27, 76, 54);
            g.DrawLine(rim, SaltFrontierAnchorMarker.X - 25, SaltFrontierAnchorMarker.Y, SaltFrontierAnchorMarker.X + 25, SaltFrontierAnchorMarker.Y);
            g.DrawLine(rim, SaltFrontierAnchorMarker.X, SaltFrontierAnchorMarker.Y - 17, SaltFrontierAnchorMarker.X, SaltFrontierAnchorMarker.Y + 17);
            g.DrawString("MARCO DO LASTRO", smallFont, ink, SaltFrontierAnchorMarker.X - 70, SaltFrontierAnchorMarker.Y + 35);
            if (Distance(player, SaltFrontierAnchorMarker) < 125 && saltFrontierSideProgress is 7 or 9) g.DrawString("E  EXAMINAR MARCO", smallFont, ink, SaltFrontierAnchorMarker.X - 78, SaltFrontierAnchorMarker.Y + 54);
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
        if ((saltFrontierProgress & 16) != 0 || saltFrontierCacheFound)
        {
            PointF cache = SaltFrontierCache;
            using var cacheStone = new SolidBrush(Color.FromArgb(79, 89, 82));
            using var cacheWood = new SolidBrush(saltFrontierCacheFound ? Color.FromArgb(83, 80, 68) : Color.FromArgb(111, 88, 58));
            g.FillEllipse(cacheStone, cache.X - 48, cache.Y - 25, 96, 48);
            g.FillRectangle(cacheWood, cache.X - 20, cache.Y - 14, 40, 27);
            g.DrawRectangle(rim, cache.X - 20, cache.Y - 14, 40, 27);
            g.DrawLine(rim, cache.X, cache.Y - 13, cache.X, cache.Y + 12);
            g.DrawString(saltFrontierCacheFound ? "ESTOJO ABERTO" : "ESTOJO SOB A LAJE", smallFont, ink, cache.X - 62, cache.Y + 22);
            if (Distance(player, cache) < 105) g.DrawString(saltFrontierCacheFound ? "ESTOJO VAZIO" : "E  EXAMINAR ESTOJO", smallFont, ink, cache.X - 62, cache.Y + 42);
        }
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
        if (saltFrontierSideProgress >= 4)
        {
            using var iron = new Pen(Color.FromArgb(101, 123, 125), 9);
            using var buoyLight = new SolidBrush(saltFrontierSideProgress >= 5 ? Color.LightCyan : Color.Goldenrod);
            foreach (PointF buoy in new[] { new PointF(SaltFrontierBuoys.X - 74, SaltFrontierBuoys.Y + 6), new PointF(SaltFrontierBuoys.X + 72, SaltFrontierBuoys.Y - 14) })
            {
                g.DrawLine(iron, buoy.X, buoy.Y + 38, buoy.X, buoy.Y - 35);
                g.DrawEllipse(rim, buoy.X - 17, buoy.Y - 38, 34, 25);
                g.FillEllipse(buoyLight, buoy.X - 4, buoy.Y - 30, 8, 8);
            }
            g.DrawLine(rim, SaltFrontierBuoys.X - 70, SaltFrontierBuoys.Y + 49, SaltFrontierBuoys.X + 72, SaltFrontierBuoys.Y + 49);
            g.DrawString("FILEIRA DAS BÓIAS", smallFont, ink, SaltFrontierBuoys.X - 100, SaltFrontierBuoys.Y + 58);
            if (Distance(player, SaltFrontierBuoys) < 120) g.DrawString("E  EXAMINAR MARCADOR", smallFont, ink, SaltFrontierBuoys.X - 106, SaltFrontierBuoys.Y + 78);
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
        if (Distance(player, SaltFrontierEntrance) < 105) g.DrawString(drops.Count > 0 ? "RECOLHA O SAQUE PARA RETORNAR" : "E  VOLTAR À BACIA", smallFont, ink, 80, 1255);
        if (Distance(player, SaltFrontierCartographer) < InteractionRadius) g.DrawString("E  FALAR", smallFont, ink, SaltFrontierCartographer.X - 40, SaltFrontierCartographer.Y + 45);
    }

    private void DrawSaltFrontierMap(Graphics g)
    {
        using var shade = new SolidBrush(Color.FromArgb(240, 12, 16, 17));
        using var panel = new SolidBrush(Color.FromArgb(247, 21, 24, 24));
        using var line = new Pen(Color.FromArgb(202, 153, 139, 111), 6) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var tideLine = new Pen(Color.FromArgb(194, 125, 207, 212), 3) { DashStyle = DashStyle.Dash, StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var font = new Font("Segoe UI", 9, FontStyle.Bold);
        using var title = new Font("Georgia", 17, FontStyle.Bold);
        g.FillRectangle(shade, 0, 0, 960, 640); g.FillRectangle(panel, 45, 105, 870, 485); g.DrawRectangle(Pens.DarkGoldenrod, 45, 105, 870, 485);
        g.DrawString("COSTA DAS BADALADAS", title, Brushes.Goldenrod, 327, 121);
        PointF Map(PointF p) => new(100 + p.X * 0.18f, 170 + p.Y * 0.12f);
        PointF[][] paths = { new[] { SaltFrontierEntrance, new PointF(500, 980), SaltFrontierTideBells[0], new PointF(1380, 800), SaltFrontierCartographer }, new[] { SaltFrontierCartographer, new PointF(2140, 760), SaltFrontierTideBells[1], new PointF(3000, 790), SaltFrontierTideBells[2] } };
        foreach (PointF[] path in paths) g.DrawLines(line, path.Select(Map).ToArray());
        PointF[] tidePath = { new(2840, 2025), new(3020, 1980), new(3210, 1995), new(3400, 1935), new(3560, 1815) };
        if (MathF.Sin(windupPulse * 0.45f) < 0) Array.Reverse(tidePath);
        g.DrawLines(tideLine, tidePath.Select(Map).ToArray());
        if (saltFrontierBossDefeated) g.DrawLines(line, new[] { new PointF(3000, 790), new PointF(3400, 760), SaltFrontierTeren, new PointF(3770, 520), SaltFrontierWreck }.Select(Map).ToArray());
        if (saltFrontierSideProgress >= 4) g.DrawLines(line, new[] { SaltFrontierTideBells[2], new PointF(3430, 1920), SaltFrontierBuoys }.Select(Map).ToArray());
        if (saltFrontierSideProgress >= 6) g.DrawLines(line, new[] { SaltFrontierTideBells[1], new PointF(2210, 1780), new PointF(2370, 2230), SaltFrontierOrva, new PointF(3130, 2660), SaltFrontierAnchorMarker }.Select(Map).ToArray());
        if ((saltFrontierProgress & 31) == 31) g.DrawLines(line, new[] { SaltFrontierTideBells[2], new PointF(3750, 1770), SaltFrontierBellheart }.Select(Map).ToArray());
        PointF[] positions = { SaltFrontierEntrance, SaltFrontierCartographer, SaltFrontierTideBells[0], SaltFrontierTideBells[1], SaltFrontierTideBells[2], SaltFrontierBellheart, SaltFrontierTeren, SaltFrontierWreck, SaltFrontierBuoys, SaltFrontierCache, SaltFrontierOrva, SaltFrontierAnchorMarker };
        string[] names = { "BACIA", "NEMI", "BASALTO", "JUNCAL", "COSTA", "CORAÇÃO", "TEREN", "DESTROÇO", "BÓIAS", "ESTOJO", "ORVA", "LASTRO" };
        for (int i = 0; i < positions.Length; i++)
        {
            PointF p = Map(positions[i]);
            bool visible = i switch { 0 or 1 => true, 5 => (saltFrontierProgress & 31) == 31, 6 or 7 => saltFrontierBossDefeated, 8 => saltFrontierSideProgress >= 4, 9 => (saltFrontierProgress & 16) != 0 || saltFrontierCacheFound, 10 or 11 => saltFrontierSideProgress >= 6, _ => (saltFrontierProgress & (1 << (i - 1))) != 0 };
            if (!visible) { g.FillEllipse(Brushes.DimGray, p.X - 5, p.Y - 5, 10, 10); continue; }
            bool marked = i >= 2 && i <= 4 && (saltFrontierProgress & (1 << (i - 1))) != 0;
            Brush marker = i == 1 || i == 6 || i == 10 ? Brushes.LightSkyBlue : i == 5 ? saltFrontierBossDefeated ? Brushes.LightCyan : Brushes.MediumPurple : i == 7 ? saltFrontierSideProgress == 2 ? Brushes.LightGreen : Brushes.Goldenrod : i == 8 ? saltFrontierSideProgress >= 5 ? Brushes.LightGreen : Brushes.Goldenrod : i == 9 ? saltFrontierCacheFound ? Brushes.LightGreen : Brushes.Goldenrod : i == 11 ? saltFrontierSideProgress >= 10 ? Brushes.LightGreen : Brushes.Goldenrod : marked ? Brushes.LightGreen : Brushes.Goldenrod;
            g.FillEllipse(marker, p.X - 6, p.Y - 6, 12, 12);
            g.DrawString(names[i], font, Brushes.AntiqueWhite, p.X - 32, p.Y - 22);
        }
        PointF here = Map(player); g.FillEllipse(Brushes.LightGreen, here.X - 7, here.Y - 7, 14, 14);
        g.DrawString("VERDE: VOCÊ/ROTA RECUPERADA  •  OURO: A DESCOBRIR  •  CIANO: NEMI/TEREN E CORRENTE  •  ROXO: CORAÇÃO  •  M/ESC: FECHAR", font, Brushes.AntiqueWhite, 95, 547);
    }
}
