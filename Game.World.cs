using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

public sealed partial class Game
{
    private static EnemyKind VigilBossKind(int floor) => ((Math.Max(1, floor) - 1) % 5) switch
    {
        0 => EnemyKind.DeepSilence,
        1 => EnemyKind.RouteScribe,
        2 => EnemyKind.LanternRegent,
        3 => EnemyKind.GlassHerald,
        _ => EnemyKind.RiftCantor
    };

    private EnemyKind RunVigilBoss(int floor) => vigilBossOrder[(Math.Max(1, floor) - 1) % vigilBossOrder.Length];
    private VigilAspect RunVigilAspect(int floor) => vigilAspectOrder[(Math.Max(1, floor) - 1) % vigilAspectOrder.Length];
    private void ShuffleVigilOrder()
    {
        vigilAspectOrder = new[] { VigilAspect.Hardened, VigilAspect.Hungry, VigilAspect.Swift, VigilAspect.Chorus, VigilAspect.Counterflow };
        vigilBossOrder = new[] { EnemyKind.DeepSilence, EnemyKind.RouteScribe, EnemyKind.LanternRegent, EnemyKind.GlassHerald, EnemyKind.RiftCantor };
        for (int i = vigilAspectOrder.Length - 1; i > 0; i--) { int j = random.Next(i + 1); (vigilAspectOrder[i], vigilAspectOrder[j]) = (vigilAspectOrder[j], vigilAspectOrder[i]); }
        for (int i = vigilBossOrder.Length - 1; i > 0; i--) { int j = random.Next(i + 1); (vigilBossOrder[i], vigilBossOrder[j]) = (vigilBossOrder[j], vigilBossOrder[i]); }
    }

    private static VigilAspect VigilAspectForFloor(int floor) => ((Math.Max(1, floor) - 1) % 5) switch
    {
        0 => VigilAspect.Hardened,
        1 => VigilAspect.Hungry,
        2 => VigilAspect.Swift,
        3 => VigilAspect.Chorus,
        _ => VigilAspect.Counterflow
    };

    private static string VigilAspectTitle(int floor) => VigilAspectForFloor(floor) switch
    {
        VigilAspect.Hardened => "CASCA VIVA",
        VigilAspect.Hungry => "FOME DO RIO",
        VigilAspect.Swift => "SUSSURRO VELOZ",
        VigilAspect.Chorus => "CORO ERRANTE",
        _ => "MARÉ INVERTIDA"
    };

    private static string VigilAspectRule(int floor) => VigilAspectForFloor(floor) switch
    {
        VigilAspect.Hardened => "O chefe recebe 25% menos dano",
        VigilAspect.Hungry => "Os ataques do chefe causam 30% mais dano",
        VigilAspect.Swift => "Os ataques começam 20% mais rápido",
        VigilAspect.Chorus => "Um Eco Salino surge a cada 20 segundos",
        _ => "A corrente puxa e repele a cada 6 segundos"
    };

    private static Color VigilAspectColor(int floor) => VigilAspectForFloor(floor) switch
    {
        VigilAspect.Hardened => Color.SteelBlue,
        VigilAspect.Hungry => Color.OrangeRed,
        VigilAspect.Swift => Color.Goldenrod,
        VigilAspect.Chorus => Color.MediumPurple,
        _ => Color.LightSeaGreen
    };
    private static string VigilAspectTitle(VigilAspect aspect) => aspect switch { VigilAspect.Hardened => "CASCA VIVA", VigilAspect.Hungry => "FOME DO RIO", VigilAspect.Swift => "SUSSURRO VELOZ", VigilAspect.Chorus => "CORO ERRANTE", _ => "MARÉ INVERTIDA" };
    private static string VigilAspectRule(VigilAspect aspect) => aspect switch { VigilAspect.Hardened => "O chefe recebe 25% menos dano", VigilAspect.Hungry => "Os ataques do chefe causam 30% mais dano", VigilAspect.Swift => "Os ataques começam 20% mais rápido", VigilAspect.Chorus => "Um Eco Salino surge a cada 20 segundos", _ => "A corrente puxa e repele a cada 6 segundos" };
    private static Color VigilAspectColor(VigilAspect aspect) => aspect switch { VigilAspect.Hardened => Color.SteelBlue, VigilAspect.Hungry => Color.OrangeRed, VigilAspect.Swift => Color.Goldenrod, VigilAspect.Chorus => Color.MediumPurple, _ => Color.LightSeaGreen };

    private static bool VigilTidePulling(float elapsed) => elapsed % 12f < 6f;

    private void ApplyVigilTide(float dt)
    {
        if (!vigilActive || room != 38 || RunVigilAspect(vigilFloor) != VigilAspect.Counterflow) return;
        vigilTideTime += dt;
        PointF outward = Normalize(Sub(player, new PointF(480, 330)), new PointF(1, 0));
        float direction = VigilTidePulling(vigilTideTime) ? -1 : 1;
        MovePlayer(Scale(outward, direction * 54 * dt));
    }

    private void TickVigilMinions(float dt)
    {
        if (!vigilActive || room != 38 || RunVigilAspect(vigilFloor) != VigilAspect.Chorus || drops.Count > 0 || enemies.Any(enemy => enemy.VigilMinion)) return;
        Enemy? boss = enemies.FirstOrDefault(enemy => enemy.Boss);
        if (boss is null) return;
        vigilMinionCooldown -= dt;
        if (vigilMinionCooldown > 0) return;
        PointF[] spawnPoints = { new(180, 185), new(780, 185), new(180, 465), new(780, 465) };
        PointF spawn = spawnPoints.Where(point => !Blocked(point, 14)).OrderByDescending(point => Distance(player, point)).FirstOrDefault(new PointF(780, 330));
        float hp = Math.Clamp(boss.MaxHp * 0.035f, 80, 220);
        enemies.Add(new Enemy { Pos = spawn, Kind = EnemyKind.SaltEcho, Hp = hp, MaxHp = hp, Cooldown = 0.8f, VigilMinion = true });
        vigilMinionCooldown = 20;
        notice = "CORO ERRANTE • UM ECO SALINO ENTROU NA LUTA"; noticeTime = 2.4f;
        GameAudio.Play(GameAudio.Cue.BossWarning);
    }

    private void StartVigil()
    {
        if (finalEnding == 0) return;
        if (vigilBestFloor > 0) ShuffleVigilOrder();
        vigilActive = true; vigilChoiceOpen = false; vigilFloor = vigilBestFloor + 1;
        room = 38; SetupRoom();
        ShowMemory($"VIGÍLIA ASCENDENTE • ANDAR {vigilFloor}", "Aven reúne ecos de antigos guardiões sem lhes devolver os nomes. Cada andar torna a lembrança mais intensa e deixa um prêmio para quem atravessar a prova. Derrote o eco, recolha o saque e interaja com o altar para subir ou encerrar a vigília.");
    }

    private void ResolveVigilFloor(bool ascend)
    {
        if (!vigilActive || enemies.Count > 0 || drops.Count > 0) return;
        vigilBestFloor = Math.Max(vigilBestFloor, vigilFloor);
        int goldReward = 100 + Math.Min(900, (vigilFloor - 1) * 25);
        int glassReward = 1 + Math.Min(5, (vigilFloor - 1) / 3);
        gold += goldReward; tideGlass = Math.Min(999, tideGlass + glassReward);
        if (vigilFloor % 3 == 0) potions++;
        if (ascend)
        {
            vigilFloor++; SetupRoom();
            notice = $"VIGÍLIA {vigilBestFloor} CONCLUÍDA • +{goldReward} OURO • +{glassReward} SAL VÍTREO"; noticeTime = 3;
            return;
        }
        vigilActive = false; vigilChoiceOpen = false; vigilFloor = 0; room = 18; SetupRoom(); health = maxHealth;
        ShowMemory("A VIGÍLIA SE ENCERRA", $"A comunidade registra sua melhor marca: andar {vigilBestFloor}. Aven recolhe os ecos para que possam repousar. Você recebe {goldReward} ouro e {glassReward} sal vítreo; a jornada pode ser retomada quando quiser.");
    }

    private void SetupRoom()
    {
        player = new PointF(150, 330); enemies.Clear(); drops.Clear(); groundHazards.Clear(); echoShards.Clear(); bowArrows.Clear(); lockedTarget = null;
        if (room == 43) { player = new PointF(180, 700); SpawnFirstEchoEncounters(); return; }
        if (room is 44 or 45) { SetupFirstEchoInterior(); return; }
        if (vigilActive && room == 38) { vigilMinionCooldown = 20; vigilTideTime = 0; }
        if (room != 3 && !vigilActive && (clearedRooms & (1L << room)) != 0) return;
        int count = room is 3 or 18 ? 0 : room == 0 ? 3 : room == 1 ? 5 : room == 4 ? 5 : room == 5 ? 4 : room == 6 ? 6 : room is 21 or 29 or 31 or 33 or 35 or 37 or 41 ? 6 : room == 22 ? 5 : room == 23 ? 6 : room == 24 ? 5 : room == 25 ? 6 : room is 26 or 28 or 30 or 32 or 34 or 36 ? 5 : room is 38 or 40 or 42 ? 1 : room == 39 ? 5 : room == 27 ? 6 : (room is 7 or 9 or 11 or 15 or 17 or 20) ? 4 : room == 8 ? 5 : room is 10 or 12 or 14 or 16 or 19 ? 6 : room == 13 ? 4 : 4;
        for (int i = 0; i < count; i++)
        {
            EnemyKind kind = room == 2 && i == 0 ? EnemyKind.Guardian : room == 5 && i == 0 ? EnemyKind.BlindScribe : room == 7 && i == 0 ? EnemyKind.RiverFerryman : room == 9 && i == 0 ? EnemyKind.AshWarden : room == 13 && i == 0 ? EnemyKind.NameWeaver : room == 15 && i == 0 ? EnemyKind.FirstKeeper : room == 17 && i == 0 ? EnemyKind.OathCartographer : room == 20 && i == 0 ? EnemyKind.NameBroker : room == 26 && i == 0 ? EnemyKind.FlowKeeper : room == 24 && i == 0 ? EnemyKind.ReedKeeper : room == 22 && i == 0 ? EnemyKind.TideWitness : room == 11 && i == 0 ? EnemyKind.HollowName : room == 1 && i == count - 1 ? EnemyKind.Elite : room == 4 && (i is 1 or 4) ? EnemyKind.InkEcho : room == 5 && i == 2 ? EnemyKind.InkEcho : room == 6 && (i is 1 or 4) ? EnemyKind.DrownedMemory : room == 7 && i == 2 ? EnemyKind.DrownedMemory : room == 8 && (i is 1 or 4) ? EnemyKind.EmberWisp : room == 9 && i == 2 ? EnemyKind.EmberWisp : room == 10 && (i is 1 or 4) ? EnemyKind.NameMoth : room is 12 && (i is 1 or 4) ? EnemyKind.NameMoth : (room is 11 or 13 or 14 or 15 or 16 or 17 or 19 or 20) && i == 2 ? EnemyKind.NameMoth : (room is 12 or 13 or 14 or 15 or 16 or 17 or 19 or 20) && i == 1 ? EnemyKind.NameMoth : room == 16 && i == 4 ? EnemyKind.MudCantor : room == 17 && i == 3 ? EnemyKind.MudCantor : (room is 19 or 20) && i == 3 ? EnemyKind.PaperWraith : (room is 21 or 22 or 23 or 24 or 25 or 26) && i == 2 ? EnemyKind.SaltEcho : (room is 23 or 24 or 25 or 26) && i == 3 ? EnemyKind.MudCantor : room > 0 && i == 1 ? EnemyKind.Archer : (room is 2 or 5 or 7 or 9 or 11 or 14 or 16 or 19) && i == 2 ? EnemyKind.Mage : EnemyKind.Grunt;
            if (room == 28 && i == 0) kind = EnemyKind.CounterCurrent;
            if (room == 29 && i == 0) kind = EnemyKind.Elite;
            if (room == 29 && i == 4) kind = EnemyKind.NameMoth;
            if (room == 30 && i == 0) kind = EnemyKind.RiftCantor;
            if (room == 32 && i == 0) kind = EnemyKind.GlassHerald;
            if (room == 34 && i == 0) kind = EnemyKind.LanternRegent;
            if (room == 36 && i == 0) kind = EnemyKind.RouteScribe;
            if (room == 38 && i == 0) kind = EnemyKind.DeepSilence;
            if (room == 40 && i == 0) kind = EnemyKind.BorrowedChorus;
            if (room == 42 && i == 0) kind = EnemyKind.LastTuner;
            if ((room is 14 or 21) && i == count - 1) kind = EnemyKind.CooldownLeech;
            if (vigilActive && i == 0) kind = RunVigilBoss(vigilFloor);
            if (room >= 27 && i == 2) kind = EnemyKind.SaltEcho;
            if (room >= 27 && i == 3) kind = EnemyKind.MudCantor;
            if ((room is 27 or 31 or 41) && i == 4) kind = EnemyKind.SlagWeaver;
            if (!vigilActive && i == count - 1 && room is 23 or 25 or 27 or 31 or 33 or 35 or 37 or 39) kind = EnemyKind.Elite;
            float hp = EnemyData(kind).HealthAt(room, townEnding);
            if (vigilActive) hp = 3450 * (1 + Math.Max(0, vigilFloor - 1) * 0.28f);
            else hp *= DifficultyEnemyHealthMultiplier(difficultyTier);
            EliteAffix eliteAffix = kind == EnemyKind.Elite ? EliteAffixForRoll(random.Next(6)) : EliteAffix.Armored;
            EliteAffix? eliteAffix2 = kind == EnemyKind.Elite && !vigilActive && (room >= 25 || difficultyTier >= 2) ? EliteSecondaryAffixForRoll(random.Next(3), eliteAffix) : null;
            bool armoredElite = kind == EnemyKind.Elite && (eliteAffix == EliteAffix.Armored || eliteAffix2 == EliteAffix.Armored);
            var enemy = new Enemy { Pos = new PointF(485 + i % 3 * 120, 215 + i / 3 * 165), Kind = kind, Hp = hp, MaxHp = hp, Cooldown = 0.7f + (float)random.NextDouble(), SummonCooldown = 5.5f + (float)random.NextDouble() * 2, SupportCooldown = 2.5f + (float)random.NextDouble() * 2, EliteAffix = eliteAffix, EliteAffix2 = eliteAffix2, Armored = armoredElite };
            if (Blocked(enemy.Pos, enemy.Radius)) enemy.Pos = new PointF(660 + i * 12, 480 - i * 8);
            enemies.Add(enemy);
        }
    }

    private void EnterVesper()
    {
        room = 3; title = false; victory = false; SetupRoom();
        ShowMemory("VÉSPERA", "A praça do farol ainda vive, embora seus moradores evitem falar dos sinos. Sileth espera perto do arquivo; talvez reconheça a voz de Nara.");
        notice = questStage == 0 ? "NOVO OBJETIVO: FALE COM SILETH" : "OBJETIVO ATUALIZADO"; noticeTime = 3.5f;
    }

    private void ChooseTownEnding(int choice)
    {
        if (questStage == 51 && silenceMemory && finalEnding == 0 && choice is 1 or 2)
        {
            finalEnding = choice; questStage = choice == 1 ? 52 : 53; endingChoiceOpen = false;
            if (choice == 1)
            {
                maxHealth += 20; health = maxHealth;
                ShowMemory("EPÍLOGO — NOMES DEVOLVIDOS", "O Escutador abre o arquivo e devolve as lembranças às vozes que as reconheceram. O Lume perde sua antiga força; agora a comunidade precisa proteger Véspera em conjunto. Sileth mantém o arquivo aberto, Edda toca os sinos sem exigir resposta. Nara continua além do mapa, livre para voltar ou seguir seu caminho. O Silêncio já não é uma prisão única: cada memória pode ser ouvida por quem a reivindicar. Vida máxima +20.");
            }
            else
            {
                damage += 10;
                ShowMemory("EPÍLOGO — A LUZ QUE FICA", "O Escutador oferece ao Lume uma lembrança própria: o primeiro som que o trouxe a Véspera. O farol continua protegendo a cidade, mas deixa de chamar as vozes perdidas de volta à força. Sileth registra o preço e mantém o arquivo aberto; Edda toca os sinos para lembrar a cidade de escutar quem chega. Nara continua além do mapa, livre para escolher se algum dia retorna. Dano +10.");
            }
            notice = "DESTINO DO LUME REGISTRADO • EPÍLOGO CONCLUÍDO"; noticeTime = 4; return;
        }
        if (questStage < 12 || townEnding != 0 || choice is < 1 or > 2) return;
        townEnding = choice; questStage = 13; endingChoiceOpen = false;
        if (choice == 1)
        {
            maxHealth += 20; health = maxHealth;
            ShowMemory("VÉSPERA DEVOLVE OS NOMES", "Sileth abre os arquivos e lê cada nome em voz alta. Os sinos respondem um a um. O Lume enfraquece, e o Silêncio volta a se mover sob o rio; ninguém mais enfrentará essa ameaça sozinho. Edda promete manter os nomes nos sinos. A praça oferece abrigo e coragem: vida máxima +20.");
        }
        else
        {
            damage += 10;
            ShowMemory("VÉSPERA MANTÉM O LUME", "Sileth conserva a barreira e registra o preço sem apagar o que foi perdido. Edda toca os sinos para que a cidade não confunda proteção com esquecimento. A luz do Lume marca a arma do Escutador: dano +10. Os arquivos ficam abertos, mesmo sob a luz antiga.");
        }
        notice = "DECISÃO DE VÉSPERA REGISTRADA • SALVE COM F5"; noticeTime = 4;
    }

    private void Interact()
    {
        if (room == 43) { InteractFirstEcho(); return; }
        if (room is 44 or 45) { InteractFirstEchoInterior(); return; }
        if (room == 3) { InteractTown(); return; }
        if (room == 18) { InteractRefuge(); return; }
        Drop? weaponDrop = drops.Where(drop => drop.Kind == "weapon" && Distance(player, drop.Pos) <= 75).OrderBy(drop => Distance(player, drop.Pos)).FirstOrDefault();
        if (weaponDrop is not null) { GameAudio.Play(GameAudio.Cue.Loot); Collect(weaponDrop); return; }
        if (enemies.Count != 0) { notice = "DERROTE OS INIMIGOS PRIMEIRO"; noticeTime = 1.2f; return; }
        if (drops.Count != 0) { notice = "RECOLHA O LOOT ANTES DE AVANÇAR"; noticeTime = 1.5f; return; }
        MarkRoomCleared();
        if (vigilActive && room == 38 && (Distance(player, VigilArenaAltarPosition) < 76 || player.X > 790)) { vigilChoiceOpen = true; return; }
        if (room < MemoryStonePositions.Length && (memoriesFound & (1 << room)) == 0 && Distance(player, MemoryStonePositions[room]) < MemoryStoneInteractionRadius)
        {
            memoriesFound |= 1 << room;
            ShowMemory(room == 0 ? "INSCRIÇÃO NO VESTÍBULO" : "REGISTRO NO SALÃO DOS OSSOS", MemoryStoneTexts[room]);
            return;
        }
        if (room < 2 && player.X > 790) { room++; SetupRoom(); }
        else if (room == 2 && Distance(player, new PointF(750, 330)) < 110) chestChoice = true;
        else if (room == 4 && player.X > 790) { room = 5; SetupRoom(); ShowMemory("COFRE DO ARQUIVO", "As páginas finais foram seladas por dentro. O guardião que as protege já não distingue o nome de quem jurou salvar."); }
        else if (room == 4 && player.X < 115) { room = 3; SetupRoom(); }
        else if (room == 5 && player.X < 115) { room = 4; SetupRoom(); }
        else if (room == 5 && player.X > 790) { room = 3; SetupRoom(); ShowMemory("REGRESSO A VÉSPERA", "O selo do Arquivo Norte pesa na mão. Sileth precisa ver a gravação de Nara."); }
        else if (room == 6 && player.X > 790) { room = 7; SetupRoom(); ShowMemory("CAPELA AFOGADA", "Sob as pedras do rio, um sino toca sem badalo. Uma figura bloqueia a passagem e repete os nomes que a corrente levou."); }
        else if (room == 6 && player.X < 115) { room = 3; SetupRoom(); }
        else if (room == 7 && player.X < 115) { room = 6; SetupRoom(); }
        else if (room == 7 && player.X > 790) { room = 3; SetupRoom(); ShowMemory("REGRESSO A VÉSPERA", "O bronze marcado com o nome de Nara ainda vibra. Edda reconhecerá o sino."); }
        else if (room == 8 && player.X < 115) { room = 3; SetupRoom(); }
        else if (room == 8 && player.X > 790) { room = 9; SetupRoom(); ShowMemory("CÂMARA DO LUME", "O farol está aceso, mas sua luz não se espalha: ela converge para um prisma suspenso sobre um poço sem fundo."); }
        else if (room == 9 && player.X < 115) { room = 8; SetupRoom(); }
        else if (room == 9 && player.X > 790) { room = 3; SetupRoom(); ShowMemory("REGRESSO À PRAÇA", "A imagem de Nara no prisma não esclarece se ela ainda pode ser encontrada. Sileth precisa ver o que você descobriu."); }
        else if (room == 9 && questStage >= 10 && Distance(player, new PointF(480, 500)) < 64) { room = 10; SetupRoom(); ShowMemory("A MARGEM SEM REFLEXO", "Atrás do prisma existe um leito que não aparece em nenhum mapa. As pedras guardam vozes recusadas pelo Lume, mas nenhuma delas repete o nome de Nara."); }
        else if (room == 10 && player.X < 115) { room = 9; SetupRoom(); }
        else if (room == 10 && player.X > 790) { room = 11; SetupRoom(); ShowMemory("RESERVATÓRIO DAS VOZES", "As lembranças recusadas convergem para um poço. Algo se move entre elas, usando nomes esquecidos como armadura."); }
        else if (room == 11 && player.X < 115) { room = 10; SetupRoom(); }
        else if (room == 12 && player.X < 115) { room = 11; SetupRoom(); }
        else if (room == 12 && player.X > 790) { room = 13; SetupRoom(); ShowMemory("CLAUSTRO DO CORO PARTIDO", "As vozes da costa convergem para uma abadia sem teto. No centro, uma figura costura nomes apagados com fios de água escura."); }
        else if (room == 13 && player.X < 115) { room = 12; SetupRoom(); }
        else if (room == 13 && questStage >= 15 && Distance(player, new PointF(480, 500)) < 64) { room = 14; SetupRoom(); ShowMemory("BACIA DO PRIMEIRO NOME", townEnding == 1 ? "Os nomes devolvidos aos sinos fazem a pedra responder. Uma escadaria escondida desce até uma bacia construída antes do Lume." : "A luz do Lume atravessa o piso do Claustro e revela degraus para uma bacia que não aparece nos mapas."); }
        else if (room == 13 && player.X > 790 && chorusMemory) { room = 3; SetupRoom(); ShowMemory("REGRESSO A VÉSPERA", "Você traz um coro inteiro, mas não uma voz única. Sileth precisa ouvi-lo para descobrir por que os nomes consumidos continuam chamando do fundo."); }
        else if (room == 14 && player.X < 115) { room = 13; SetupRoom(); }
        else if (room == 14 && player.X > 790) { room = 15; SetupRoom(); ShowMemory("SALA DA PRIMEIRA VIGÍLIA", "A bacia dá lugar a uma câmara circular. Uma guardiã sem rosto protege o primeiro nome que o Lume recebeu."); }
        else if (room == 15 && player.X < 115) { room = 14; SetupRoom(); }
        else if (room == 15 && player.X > 790 && firstMemory) { room = 3; SetupRoom(); ShowMemory("REGRESSO A VÉSPERA", "A guardiã não era a origem do Lume, mas guardava a lembrança de quem o acendeu. Sileth precisa ouvir o primeiro nome antes que a história avance."); }
        else if (room == 16 && player.X < 115) { room = 3; SetupRoom(); ShowMemory("REGRESSO A VÉSPERA", "Edda espera no cais da praça. O Vau pode ser cruzado novamente quando quiser seguir o mapa."); }
        else if (room == 16 && player.X > 790) { room = 17; SetupRoom(); ShowMemory("PÁTIO DAS TESTEMUNHAS", "As margens do Vau formam um pátio de tábuas. Uma cartógrafa apagou as placas dos caminhos; ela observa quem chega antes de decidir se a passagem continua aberta."); }
        else if (room == 17 && player.X < 115) { room = 16; SetupRoom(); }
        else if (room == 17 && player.X > 790 && westTrace) { room = 3; SetupRoom(); ShowMemory("REGRESSO A VÉSPERA", "O mapa de Nara aponta além do abrigo, mas a letra não revela quando ela partiu. Edda saberá levar vocês contra a corrente."); }
        else if (room == 19 && player.X < 115) { room = 18; SetupRoom(); }
        else if (room == 19 && player.X > 790) { room = 20; SetupRoom(); ShowMemory("CISTERNA DO REGISTRO VAZIO", "A cisterna foi construída para guardar água, mas alguém a cobriu de contratos. Um Escrivão de Contrato protege o volume e ataca qualquer pessoa cujo nome consiga ler."); }
        else if (room == 20 && player.X < 115) { room = 19; SetupRoom(); }
        else if (room == 20 && player.X > 790 && questStage >= 23) { room = 18; SetupRoom(); }
        else if (room == 21 && player.X < 115) { room = 18; SetupRoom(); }
        else if (room == 21 && player.X > 790) { room = 22; SetupRoom(); ShowMemory("PONTE DA MARÉ INVERTIDA", "A ponte antiga cruza a nascente por onde as vozes seguem contra a corrente. Uma figura cobre as tábuas com marcas de água salgada. Ela não repete nenhum nome; espera para ouvir o que você veio perguntar."); }
        else if (room == 22 && player.X < 115) { room = 21; SetupRoom(); }
        else if (room == 22 && player.X > 790 && questStage >= 27) { if (questStage == 27) questStage = 28; room = 23; SetupRoom(); ShowMemory("COTOVELO DOS JUNCAIS", "Depois da ponte, o rio se abre num banco baixo de juncos. Pares de nós indicam onde a água ainda permite passagem; nenhum aponta para uma pessoa ou destino."); }
        else if (room == 22 && player.X > 790 && questStage >= 26) { room = 18; SetupRoom(); ShowMemory("VOLTA À MESA COMUNITÁRIA", "A lembrança da nascente cabe numa frase curta. Aven pode guardá-la sem escrever por Nara."); }
        else if (room == 23 && player.X < 115) { room = 22; SetupRoom(); }
        else if (room == 23 && player.X > 790) { room = 24; SetupRoom(); ShowMemory("PÁTIO DAS AMARRAS LIVRES", "Nós de junco formam sinais que podem ser desfeitos pela própria pessoa que os amarrou. A Guardiã do Leito os prende todos numa única rota, como se manter um caminho aberto significasse escolher por quem passa."); }
        else if (room == 24 && player.X < 115) { room = 23; SetupRoom(); }
        else if (room == 24 && player.X > 790 && questStage >= 29) { room = 18; SetupRoom(); ShowMemory("O LEITO SEGUE ABERTO", "A margem não deixa um mapa de Nara. Deixa um sinal comum: o grupo atravessou durante a cheia e decidiu qual margem seguir quando chegou ao outro lado."); }
        else if (room == 25 && player.X < 115) { room = 18; SetupRoom(); }
        else if (room == 25 && player.X > 790) { room = 26; SetupRoom(); ShowMemory("CÂMARA DO VEIO PRESO", "Debaixo dos moinhos, as rodas pararam sem quebrar. Um vigia mantém a água girando num círculo fechado; cada golpe muda o sentido da corrente."); }
        else if (room == 26 && player.X < 115) { room = 25; SetupRoom(); }
        else if (room == 26 && player.X > 790 && questStage >= 32) { room = 18; SetupRoom(); ShowMemory("A CORRENTE DEVOLVIDA", "O fluxo volta a encontrar saída. Aven pode ouvir o relato e guardar o que foi aprendido sem transformar a margem em mapa."); }
        else if (room == 27 && player.X < 115) { room = 18; SetupRoom(); }
        else if (room == 27 && player.X > 790) { room = 28; SetupRoom(); ShowMemory("CORAÇÃO DAS DUAS MARGENS", "O círculo de pedra alterna entre puxar e repelir. No centro, o Afinador das Margens prepara um golpe diferente para cada sentido da corrente. Observe os sinais no chão e atravesse a bacia quando o fluxo se inverter."); }
        else if (room == 28 && player.X < 115) { room = 27; SetupRoom(); }
        else if (room == 28 && player.X > 790 && questStage >= 39) { room = 18; SetupRoom(); ShowMemory("A ESCARPA RESPIRA", "O fluxo já não se fecha sobre o próprio centro. A memória do Afinador pode ser levada a Aven para que a comunidade registre o que aprendeu sem transformar o caminho em ordem."); }
        else if (room == 28 && player.X > 790 && questStage >= 37) { room = 29; SetupRoom(); if (questStage == 37) { questStage = 38; ShowMemory("CANTARIA DAS VOZES LIVRES", "Abaixo do antigo vertedouro, cada pedra responde num tom diferente. Nenhuma voz conduz as outras; ainda assim, um ruído tenta obrigá-las a repetir a mesma nota. Os riscos no chão levam à câmara onde o som se parte."); } }
        else if (room == 28 && player.X > 790 && questStage >= 35) { room = 18; SetupRoom(); ShowMemory("A ESCARPA RESPIRA", "O fluxo já não se fecha sobre o próprio centro. A memória do Afinador pode ser levada a Aven para que a comunidade registre o que aprendeu sem transformar o caminho em ordem."); }
        else if (room == 29 && player.X < 115) { room = 28; SetupRoom(); }
        else if (room == 29 && player.X > 790 && enemies.Count == 0 && drops.Count == 0) { room = 30; SetupRoom(); ShowMemory("CÂMARA DA CONTRAVOZ", "Um eco imóvel ocupa o centro da câmara. Ele anuncia três ataques em sequência, mas cada golpe parte de uma nota diferente: baixe a guarda no intervalo, não no som."); }
        else if (room == 30 && player.X < 115) { room = 29; SetupRoom(); }
        else if (room == 30 && player.X > 790 && questStage >= 39) { room = 18; SetupRoom(); ShowMemory("A NOTA QUE FALTAVA", "A câmara volta a sustentar acordes diferentes. A lembrança recolhida não contém nome nem rota; guarda apenas a forma como as vozes escolheram responder umas às outras."); }
        else if (room == 31 && player.X < 115) { room = 18; SetupRoom(); }
        else if (room == 31 && player.X > 790 && enemies.Count == 0 && drops.Count == 0) { room = 32; SetupRoom(); ShowMemory("FONTE SOB A PEDRA", "A garganta desemboca numa nascente soterrada. O arauto de vidro guarda passos sem dono: não sabe quem os deu nem para onde levaram."); }
        else if (room == 32 && player.X < 115) { room = 31; SetupRoom(); }
        else if (room == 32 && player.X > 790 && questStage >= 42) { room = 18; SetupRoom(); ShowMemory("O ECO SEM DONO", "A fonte devolve o som dos passos, mas não lhes atribui nome, origem ou destino. A lembrança pode ser compartilhada sem decidir o caminho de ninguém."); }
        else if (room == 33 && player.X < 115) { room = 18; SetupRoom(); }
        else if (room == 33 && player.X > 790 && enemies.Count == 0 && drops.Count == 0) { room = 34; SetupRoom(); ShowMemory("OBSERVATÓRIO DA MARÉ INTERIOR", "Sob a fonte, lanternas antigas giram em torno de um espelho de água. O Regente não guarda a passagem: tenta fazer cada reflexo permanecer voltado para trás. Três sinais de luz se formam no chão antes de cada golpe."); }
        else if (room == 34 && player.X < 115) { room = 33; SetupRoom(); }
        else if (room == 34 && player.X > 790 && questStage >= 45) { room = 18; SetupRoom(); ShowMemory("REGRESSO À MESA DE AVEN", "O sinal não mostra quem atravessou nem em que direção. Ainda assim, a chama permanece acesa sem exigir que alguém retorne. Aven pode guardar essa promessa para qualquer viajante que escolha aceitá-la."); }
        else if (room == 35 && player.X < 115) { room = 33; SetupRoom(); }
        else if (room == 35 && player.X > 790 && enemies.Count == 0 && drops.Count == 0) { room = 36; SetupRoom(); ShowMemory("GALERIA DA ROTA NÃO ESCRITA", "As linhas das paredes convergem para uma câmara sem saída desenhada. O Escriba do Traçado move as marcas para fechar os caminhos antes que alguém possa escolher por onde seguir."); }
        else if (room == 36 && player.X < 115) { room = 35; SetupRoom(); }
        else if (room == 36 && player.X > 790 && questStage >= 48) { room = 18; SetupRoom(); ShowMemory("REGRESSO À MESA DE AVEN", "O traçado guardado não aponta para Nara. Leve a lembrança a Aven sem transformá-la em uma pista que ela nunca escolheu deixar."); }
        else if (room == 37 && player.X < 115) { room = 18; SetupRoom(); }
        else if (room == 37 && player.X > 790 && enemies.Count == 0 && drops.Count == 0 && questStage >= 50) { room = 38; SetupRoom(); ShowMemory("CÂMARA DO ÚLTIMO ECO", "O corredor termina onde o rio encontra a pedra sob Véspera. O Silêncio não tem corpo próprio: veste fragmentos das vozes que o Lume reteve. Seus três golpes são anunciados pelo chão — uma onda ao redor, um eco que persegue seus passos e uma linha que cruza a câmara."); }
        else if (room == 38 && player.X < 115 && vigilActive) { notice = "A VIGÍLIA SÓ TERMINA NO ALTAR CENTRAL"; noticeTime = 2; }
        else if (room == 38 && player.X < 115) { room = 37; SetupRoom(); }
        else if (room == 38 && player.X > 790 && silenceMemory) { room = 18; SetupRoom(); ShowMemory("REGRESSO AO REFÚGIO", "A memória final não revela o paradeiro de Nara. Ela devolve ao Escutador a responsabilidade de decidir o destino do Lume com a comunidade, sem falar em nome de quem partiu."); }
        else if (room == 41 && player.X < 115) { room = 18; SetupRoom(); }
        else if (room == 41 && player.X > 790 && enemies.Count == 0 && drops.Count == 0) { room = 42; SetupRoom(); ShowMemory("POÇO DO CONTRATEMPO", "A oficina desce até um poço onde ondas de som se movem fora do ritmo. O Afinador do Contratempo reúne as pausas e alterna uma onda anular com centro seguro, uma marca sob seus passos e dois cortes cruzados; a segunda fase acelera os avisos."); }
        else if (room == 42 && player.X < 115) { room = 41; SetupRoom(); }
        else if (room == 42 && player.X > 790 && questStage >= 58) { room = 18; SetupRoom(); ShowMemory("REGRESSO AO REFÚGIO", "O sino permanece sem badalo. As pausas voltaram a pertencer a quem escolheu deixá-las, sem nome, rota ou notícia de Nara."); }
        else if (room == 39 && player.X < 115) { room = 18; SetupRoom(); }
        else if (room == 39 && player.X > 790 && enemies.Count == 0 && drops.Count == 0) { room = 40; SetupRoom(); ShowMemory("ARQUIVO DA MARÉ SEM NOME", "O arquivo está sob a linha da maré. Uma figura feita de ecos sobrepostos tenta manter todos os sons presos no mesmo compasso."); }
        else if (room == 40 && player.X < 115) { room = 39; SetupRoom(); }
        else if (room == 40 && player.X > 790 && questStage >= 55) { room = 18; SetupRoom(); ShowMemory("REGRESSO À MESA COMUNITÁRIA", "O arquivo permanece aberto. Os sons podem ser ouvidos por quem os deixou, sem revelar identidade ou destino."); }
        else if (room == 11 && player.X > 790 && questStage >= 13) { room = 12; SetupRoom(); ShowMemory("COSTA DAS VOZES TARDIAS", townEnding == 1 ? "As badaladas com os nomes de Véspera atravessam o rio. Ecos se afastam da trilha; a cidade devolveu força a estas vozes." : "O Lume permanece aceso atrás de você. Sua luz atravessa as águas sem reflexo e atrai para a costa tudo o que foi esquecido."); }
        else if (room == 11 && player.X > 790 && naraTrace) { room = 3; SetupRoom(); ShowMemory("REGRESSO A VÉSPERA", "Você carrega uma lembrança que o Lume não conseguiu apagar. Sileth precisa ouvi-la antes de decidir o que fazer com os arquivos."); }
    }
    private void InteractTown()
    {
        if (player.X > 835 && player.Y > 280 && player.Y < 380)
        {
            firstEchoDiscovered |= 1; room = 43; SetupRoom(); RestoreFirstEchoOutside();
            ShowMemory("MARGENS DO PRIMEIRO ECO", "A estrada sai de Véspera em direção aos marcos silenciosos. Procure Ivara na encruzilhada.");
            return;
        }
        if (Distance(player, CuratorPosition) < InteractionRadius)
        {
            if (questStage == 0)
            {
                questStage = 1;
                ShowMemory("CURADORA SILETH", "Você trouxe uma lembrança de Nara. Os registros oficiais não contam por que ela desceu à cripta. Leia o Livro das Ausências no posto antigo da praça; depois conversaremos sobre o que ele prova.");
            }
            else if (questStage == 1) ShowMemory("CURADORA SILETH", "O Livro das Ausências ficava no posto junto ao poço. Procure a mesa marcada com o selo do farol.");
            else if (questStage == 2)
            {
                questStage = 3; gold += 40; potions++;
                ShowMemory("A CONFISSÃO DE SILETH", "Nara descobriu que o Lume consumia nomes, não apenas lembranças. Eu fechei o arquivo para impedir o pânico. Ela procurava uma prova que pudesse convencer a cidade. Leve o registro ao Arquivo Norte, além do rio.");
                notice = "RECOMPENSA: 40 OURO E 1 POÇÃO"; noticeTime = 3;
            }
            else if (questStage == 3) ShowMemory("CURADORA SILETH", "O Arquivo Norte fica além do Rio Cego, pelo portão de pedra ao norte da praça. Traga o selo que Nara encontrou.");
            else if (questStage == 4) { questStage = 5; gold += 65; potions++; ShowMemory("A VOZ QUE ESCOLHE", "Nara deixou uma prova de que o Lume não é apenas uma defesa. Ele começou a escolher quais nomes consome. Não posso mais esconder isso. Vou reunir a cidade; você precisa seguir o rastro dela até o leito do Rio Cego."); notice = "MISSÃO CONCLUÍDA: 65 OURO E 1 POÇÃO"; noticeTime = 3; }
            else if (questStage == 9) { questStage = 10; gold += 120; potions++; ShowMemory("O NOME QUE O LUME GUARDA", "O prisma confirma que Nara aceitou ser portadora das lembranças recusadas pelo Lume. Os arquivos não dizem se ela está viva, morta ou fundida à luz. Não vamos decidir seu destino com base em mais uma página incompleta. Reúna as memórias que faltam; quando chegar a hora, Véspera precisa escolher sabendo o preço."); notice = "MISSÃO CONCLUÍDA: 120 OURO E 1 POÇÃO"; noticeTime = 3; }
            else if (questStage == 11 && naraTrace) { questStage = 12; gold += 150; potions++; ShowMemory("A VOZ DE QUEM AINDA LEMBRA", "Nara deixou uma memória fora do alcance do farol. Ela ensinava os esquecidos a dizer os nomes uns dos outros, sem pedir que alguém a salvasse. Sileth: “Passei a vida escolhendo o que Véspera podia lembrar. Agora a cidade terá de escolher por si. Ainda faltam vozes; não vou fechar o arquivo outra vez.”"); notice = "MISSÃO CONCLUÍDA: 150 OURO E 1 POÇÃO"; noticeTime = 3; }
            else if (questStage == 14 && chorusMemory) { questStage = 15; gold += 200; potions++; ShowMemory("O CORO CHEGA À PRAÇA", "As vozes não são uma criatura que possa ser destruída. Cada nome que o Lume consome aumenta a pressão sob a cidade. Sileth registra o Coro sem apagar nenhuma versão: “Se Nara ainda está entre essas vozes, temos de escutar antes de decidir como chamá-la de volta.” O rastro aponta para além dos mapas de Véspera."); notice = "MISSÃO CONCLUÍDA: 200 OURO E 1 POÇÃO"; noticeTime = 3; }
            else if (questStage == 16 && firstMemory) { questStage = 17; gold += 260; potions++; ShowMemory("O NOME DE ILYRA", "A primeira memória devolve um propósito ao Lume, mas não desfaz séculos de escolhas. Sileth anota a versão de Ilyra ao lado das demais, sem fechar o arquivo: “Nara atravessou o rio por vontade própria. Se a encontrarmos, teremos de escutar o nome que ela escolheu para si.” Edda ouviu o Coro seguir para o oeste, contra a corrente, em direção a terras que Véspera nunca registrou."); notice = "MISSÃO CONCLUÍDA: 260 OURO E 1 POÇÃO"; noticeTime = 3; }
            else if (questStage >= 17) ShowMemory("CURADORA SILETH", "O arquivo permanece aberto. Ilyra aponta para o oeste, contra a corrente do Rio Cego. Edda prepara um barco; quando a trilha estiver pronta, seguiremos para além das terras conhecidas.");
            else if (questStage >= 12 && townEnding == 0) { endingChoiceOpen = true; notice = "VÉSPERA ESPERA SUA DECISÃO • 1 OU 2"; noticeTime = 3; }
            else if (townEnding == 1) ShowMemory("CURADORA SILETH", "Os nomes voltaram aos sinos. A praça está com você, mas as vozes além do reservatório ainda faltam. Vamos procurá-las sem fechar o arquivo outra vez.");
            else if (townEnding == 2) ShowMemory("CURADORA SILETH", "O Lume continua aceso e os arquivos permanecem abertos. O Silêncio não desapareceu; siga o rastro para além do reservatório antes que a cidade dependa de novo de nomes apagados.");
            else ShowMemory("CURADORA SILETH", "O rastro de Nara segue pelo leito do Rio Cego. Com o sino de Edda, a passagem abaixo da capela pode ser aberta.");
            return;
        }
        if (Distance(player, BellKeeperPosition) < InteractionRadius)
        {
            if (firstEchoQuest == 2) { firstEchoQuest = 3; gold += 100; potions++; AddXp(60); ShowMemory("OS MARCOS RESPONDEM", "Edda escuta o relato e toca o sino para os viajantes que regressam. As vozes estão livres, e a estrada volta a oferecer abrigo. Você recebe 100 de ouro, uma poção e 60 de experiência."); }
            else if (questStage == 6 && riverBell) { questStage = 7; gold += 100; potions++; ShowMemory("EDDA RECONHECE O SINO", "Esse era o sino de travessia do meu pai. Nara o levou quando desceu ao rio; ela conhecia o caminho e decidiu cruzá-lo. Não foi capturada. Guarde o sino: se ela voltar, vai saber que ainda lembramos seu nome."); notice = "MISSÃO CONCLUÍDA: 100 OURO E 1 POÇÃO"; noticeTime = 3; }
            else if (questStage == 19 && westTrace) { questStage = 20; gold += 375; potions++; ShowMemory("A CARTA DO REFÚGIO", "Nara construiu um lugar onde cada pessoa escolhe como será chamada e partiu para oeste com quem pediu para atravessar. Edda reconhece as marcas do mapa: “Consigo levar vocês até a próxima curva, mas dali em diante será preciso seguir a água.” Sileth deixa o arquivo aberto para a volta do grupo. A notícia não prova onde Nara está agora."); notice = "RECOMPENSA: 375 OURO E 1 POÇÃO"; noticeTime = 3; }
            else if (questStage is 17 or 18) { questStage = 18; room = 16; SetupRoom(); ShowMemory("A TRAVESSIA DO CONTRA-RIO", "Edda solta o barco de fundo largo. Os nomes devolvidos aos sinos ajudam a encontrar a passagem entre as pedras; o Lume mantido aceso ilumina os marcos submersos, mas chama ecos para o Vau. A cartógrafa da margem guarda a entrada do refúgio de Nara."); }
            else if (questStage == 20) { questStage = 21; room = 18; SetupRoom(); ShowMemory("REFÚGIO DO VEIO CALMO", "O barco de Edda segue as lanternas até uma embarcação antiga, encalhada entre as pedras. Pessoas que partiram de Véspera construíram ali um abrigo. Nara ajudou a traçar as primeiras rotas, mas continuou viagem com quem escolheu partir. Aven, o antigo barqueiro, espera junto à mesa comunitária."); }
            else if (questStage >= 21) ShowMemory("EDDA, NO CAIS", "O mapa de Nara marca outra curva a oeste, além do refúgio. Ainda não sabemos onde ela está; a trilha continua aberta.");
            else if (townEnding == 1) ShowMemory("EDDA, TOCADORA DE SINOS", "Os nomes voltaram às badaladas. Vou ensinar as crianças a ler cada sino, para que ninguém precise adivinhar quem foi esquecido."); else if (townEnding == 2) ShowMemory("EDDA, TOCADORA DE SINOS", "Os sinos agora marcam as horas do Lume. Não vou deixar que a luz transforme proteção em silêncio outra vez."); else ShowMemory("EDDA, TOCADORA DE SINOS", "Os sinos continuam inteiros. O que sumiu foram os nomes gravados neles. Quando tento lembrar quem os fundiu, só escuto água debaixo da praça. Ivara está na estrada a leste, tentando entender por que os marcos pararam de responder.");
            return;
        }
        if (Distance(player, ArchiveBookPosition) < InteractionRadius)
        {
            if (questStage == 0) ShowMemory("LIVRO DAS AUSÊNCIAS", "O volume está lacrado com o selo da Curadora. Fale com Sileth antes de tentar abri-lo.");
            else if (questStage == 1)
            {
                questStage = 2;
                ShowMemory("LIVRO DAS AUSÊNCIAS", "Registro de Vigília: o Guardião entregou seu nome ao Lume. Quatorze nomes foram usados para mantê-lo aceso. A ordem de encerramento do arquivo leva a assinatura de Sileth.");
            }
            else ShowMemory("LIVRO DAS AUSÊNCIAS", "As páginas registram os nomes entregues ao Lume. A assinatura de Sileth fecha o último volume.");
            return;
        }
        if (Distance(player, new PointF(480, 135)) < InteractionRadius)
        {
            if (questStage >= 3) { room = 4; SetupRoom(); ShowMemory("ARQUIVO NORTE", "O portão cede. Entre as estantes, ecos repetem trechos de depoimentos que foram removidos do registro oficial."); }
            else ShowMemory("PORTÃO DO ARQUIVO NORTE", "O selo da Curadora mantém o portão fechado. Descubra o que o Livro das Ausências prova e fale com Sileth.");
            return;
        }
        if (Distance(player, new PointF(480, 520)) < InteractionRadius)
        {
            if (questStage >= 7) { room = 8; SetupRoom(); ShowMemory("ESCADA DE CINZAS", "O sino abre uma passagem abaixo da Capela Afogada. A cada degrau, o ar fica mais quente e as lembranças parecem chegar antes de você."); }
            else if (questStage >= 5) { room = 6; SetupRoom(); ShowMemory("LEITO DO RIO CEGO", "A água desapareceu da superfície, mas as pedras ainda guardam as últimas palavras de quem cruzou o rio. O sino de Nara aponta para baixo da corrente."); }
            else ShowMemory("PASSAGEM DO RIO", "Edda não reconhece o caminho para o leito seco. Conclua sua investigação com Sileth antes de descer.");
            return;
        }
        if (Distance(player, CryptGatePosition) < InteractionRadius)
        {
            room = 2; SetupRoom(); ShowMemory("A CRIPTA", "A passagem continua aberta. O Guardião protege o relicário, mas a próxima pista está nos arquivos de Véspera.");
        }
    }
    private void InteractRefuge()
    {
        if (Distance(player, VigilAltarPosition) < InteractionRadius)
        {
            if (finalEnding == 0) ShowMemory("A VIGÍLIA AINDA NÃO ESTÁ ABERTA", "Aven só consegue reunir estes ecos depois que Véspera decide o destino do Lume. Conclua a campanha e volte quando estiver pronto para uma prova sem fim.");
            else StartVigil();
            return;
        }
        if (Distance(player, AvenPosition) < InteractionRadius)
        {
            if (questStage == 21) { questStage = 22; ShowMemory("AVEN, O BARQUEIRO", "Não mantemos um livro de nomes. Cada viajante decide o que quer deixar na mesa e quem pode ler. Nara ajudou a marcar a Trilha dos Luminários e seguiu adiante com outras pessoas. Ela pediu que não contássemos o nome que escolheu nem prometêssemos que voltaria. Se quiser continuar, proteja os avisos que cada pessoa decidiu compartilhar."); }
            else if (questStage == 23) { questStage = 24; gold += 240; potions += 2; ShowMemory("A MESA CONTINUA ABERTA", "A comunidade escolhe guardar os registros de passagem aqui. A nota de Nara termina antes da próxima curva; ninguém vai preencher a lacuna por ela. Aven oferece provisões para a viagem e aponta as lanternas mais recentes para oeste. Se um dia ela voltar, será ela quem decidirá o que contar."); notice = "REFÚGIO: 240 OURO E 2 POÇÕES"; noticeTime = 3; }
            else if (questStage == 26 && upstreamTrace) { questStage = 27; gold += 320; potions++; ShowMemory("A TESTEMUNHA SEM REGISTRO", "Aven ouve a lembrança sem perguntar qual era o nome anterior nem o escolhido por Nara. Ele registra apenas que um grupo chegou junto à nascente e continuou viagem por vontade própria. “Se você ainda quer encontrá-la, a próxima pergunta precisa ser feita a ela. A nossa comunidade não vai responder em seu lugar.” A trilha segue além do alcance das lanternas."); notice = "RECOMPENSA: 320 OURO E 1 POÇÃO"; noticeTime = 3; }
            else if (questStage == 29 && beyondTrace) { questStage = 30; gold += 420; potions++; ShowMemory("AVEN GUARDA O SINAL", "A marca confirma que o grupo de Nara cruzou a cheia e escolheu o próprio rumo ao chegar à margem. Aven a prende à mesa comunitária sem copiar o trajeto: “Esse relato diz que escolheram. Não diz que alguém precisa segui-los.” Ele oferece provisões e mantém o caminho aberto para quem vier depois."); notice = "RECOMPENSA: 420 OURO E 1 POÇÃO"; noticeTime = 3; }
        else if (questStage == 32) { questStage = 33; ebbSeal = true; gold += 480; potions += 2; ShowMemory("A MARCA DA VAZANTE", "Aven entrega um selo de bronze que abre uma passagem curta na água ao redor do Escutador. Ele não controla a corrente: por um instante, reúne os ecos próximos para que possam ser enfrentados juntos. “Use para abrir espaço, não para escolher o rumo de alguém.” A habilidade Vazante foi liberada."); notice = "RECOMPENSA: 480 OURO • VAZANTE LIBERADA"; noticeTime = 3; }
            else if (questStage == 35 && counterTrace) { questStage = 36; anchorWard = true; gold += 560; tideGlass += 3; potions += 2; ShowMemory("A CORRENTE TEM DUAS MARGENS", "Aven escuta o relato do Coração e acrescenta ao mapa comunitário apenas isto: a passagem pode mudar de sentido sem deixar de ser passagem. Ele ensina o Manto de Lastro: por um breve instante, o corpo acompanha a firmeza das duas margens e amortece os golpes. Ele entrega sal vítreo e provisões para a próxima jornada; nenhuma direção é marcada em nome de quem não está aqui."); notice = "RECOMPENSA: MANTO DE LASTRO • 560 OURO • 3 SAL VÍTREO • 2 POÇÕES"; noticeTime = 3; }
            else if (questStage == 36) { questStage = 37; ShowMemory("O VERTEDOURO ANTIGO", "Aven reconhece um intervalo novo entre os sons da margem. Sob o velho vertedouro existe uma cantaria onde viajantes deixavam notas de passagem sem registrar quem as tocou. O eco que agora responde não pertence a nenhuma das vozes guardadas. “Vá ouvir o que ele tenta calar; depois, conte apenas o que a pedra realmente disser.”"); notice = "NOVO DESTINO: CANTARIA DAS VOZES LIVRES"; noticeTime = 3; }
            else if (questStage == 39 && riftVerse) { questStage = 40; refrainSong = true; gold += 650; tideGlass += 4; potions += 2; ShowMemory("O CORO DE RETORNO", "Aven ouve a nota sem atribuí-la a uma pessoa. Ele ensina o Escutador a devolver um eco à pedra: o Coro de Retorno fere e interrompe ecos próximos, mas não cala os ataques dos chefes. O relato fica na mesa com as palavras que a própria cantaria permitiu compartilhar."); notice = "RECOMPENSA: CORO DE RETORNO • 650 OURO • 4 SAL VÍTREO • 2 POÇÕES"; noticeTime = 3; }
            else if (questStage == 40 && refrainSong) { questStage = 41; ShowMemory("A PEDRA ALÉM DO VERTEDOURO", "A nota da cantaria encontra uma fissura sob a corrente. Aven nunca ouviu um nome vindo dali, apenas passos que passam sem deixar registro. “Não é a trilha de Nara, pelo menos nada aqui permite afirmar isso. Se quiser escutar, leve somente o que a pedra disser.” Uma garganta de vidro se abriu a oeste do refúgio."); notice = "NOVO DESTINO: GARGANTA DO VIDRO MUDO"; noticeTime = 3; }
            else if (questStage == 42 && deepTrace) { questStage = 43; gold += 820; tideGlass += 5; potions += 2; ShowMemory("UM ECO SEM DONO", "Aven registra apenas que a fonte repetiu passos de viajantes que escolheram partir sem escrever a rota. Ele não acrescenta o som ao mapa de Nara: “Uma passagem pode lembrar que alguém esteve aqui sem dizer quem era, nem para onde foi.” A comunidade deixa a fonte aberta para qualquer pessoa que queira atravessar."); notice = "RECOMPENSA: 820 OURO • 5 SAL VÍTREO • 2 POÇÕES"; noticeTime = 3; }
            else if (questStage == 43) { questStage = 44; ShowMemory("AS LANTERNAS QUE OLHAM PARA DENTRO", "Aven reconhece uma vibração sob a fonte, distante do caminho de Nara. “As lanternas antigas não foram feitas para apontar uma direção. Elas guardavam o calor de quem escolhia parar; alguém as virou para dentro e transformou lembrança em espelho.” Ele abre a descida até o observatório. O que houver lá não pode responder por quem partiu.”"); notice = "NOVO DESTINO: PASSAGEM DAS LANTERNAS BAIXAS"; noticeTime = 3; }
            else if (questStage == 45 && lanternSignal) { questStage = 46; beaconLight = true; gold += 980; tideGlass += 6; potions += 3; ShowMemory("O FAROL DE VIGÍLIA", "Aven prende o sinal na mesa sem escrever nome ou rumo. A chama pode acompanhar o Escutador por alguns instantes e pulsar contra ecos próximos: uma luz de apoio, não um guia. “Que ela permaneça acesa para quem quiser voltar, e não como promessa de que alguém voltará.” Você aprendeu Farol de Vigília."); notice = "TÉCNICA: FAROL DE VIGÍLIA • 980 OURO • 6 SAL VÍTREO"; noticeTime = 3; }
            else if (questStage == 46 && beaconLight) { questStage = 47; ShowMemory("AS LINHAS QUE NÃO APONTAM", "O Farol ilumina marcas antigas sob a mesa de Aven. Elas não formam uma rota para Nara: são rastros de pessoas que voltaram a Véspera, partiram para outros lugares ou escolheram ficar sem deixar nome. Aven abre o caminho até a Galeria da Rota Não Escrita para descobrir por que alguém tentou apagar todas essas opções."); notice = "NOVO DESTINO: TRILHA DAS LINHAS ABERTAS"; noticeTime = 3; }
            else if (questStage == 48 && routeMemory) { questStage = 49; gold += 1300; tideGlass += 7; potions += 3; ShowMemory("A MESA GUARDA ESPAÇO", "Aven escuta o traçado sem copiá-lo para o mapa de Nara. As linhas registram escolhas de muitas pessoas e terminam em direções diferentes; nenhuma confirma quem voltou, quem partiu ou quem ainda está a caminho. A comunidade oferece provisões e mantém a mesa aberta para o próximo relato, sem preencher os espaços deixados em branco."); notice = "RECOMPENSA: 1300 OURO • 7 SAL VÍTREO • 3 POÇÕES"; noticeTime = 3; }
            else if (questStage == 49 && routeMemory) { questStage = 50; ShowMemory("CAPÍTULO XVI — O SILÊNCIO DE BAIXO", "As linhas abertas não apontam para Nara. Apontam para o lugar sob Véspera onde o Lume começou a reter lembranças. Aven não pede que você a encontre nem que a traga de volta: “Se quiser descer, vá por você. O que encontrar lá pertence às vozes que escolherem responder.” Uma soleira se abre além da Galeria da Rota Não Escrita."); notice = "NOVO DESTINO: SOLEIRA DO SILÊNCIO"; noticeTime = 3; }
            else if (questStage == 51 && silenceMemory && finalEnding == 0) { endingChoiceOpen = true; notice = "O DESTINO DO LUME AGUARDA SUA DECISÃO • 1 OU 2"; noticeTime = 3; }
            else if (finalEnding > 0 && questStage is 52 or 53) { questStage = 54; ShowMemory("O ARQUIVO DAS MARÉS EMPRESTADAS", "Aven encontrou uma nota sem assinatura na mesa comunitária. Ela descreve um cais onde viajantes deixavam sons que aceitavam compartilhar, sem nomes ou destinos. O registro foi levado para um arquivo sob a maré; algo lá tenta transformar muitas vozes numa só. A expedição é sua escolha."); notice = "EXPEDIÇÃO OPCIONAL XVII: CAIS DOS ECOS EMPRESTADOS"; noticeTime = 3; }
            else if (questStage == 55 && borrowedTrace) { questStage = 56; gold += 1900; tideGlass += 8; potions += 3; techniqueTalent++; ShowMemory("UM LUGAR PARA DEIXAR ECO", "Aven guarda a memória com o consentimento de quem a deixou. Não escreve nomes nem rotas. Como agradecimento, a comunidade oferece provisões e ensina uma nova técnica: o Escutador agora causa mais dano com todas as habilidades."); notice = "RECOMPENSA: +1900 OURO • +8 SAL • +3 POÇÕES • TÉCNICA +1"; noticeTime = 4; }
            else if (questStage == 58 && unstruckTrace) { questStage = 59; gold += 2200; tideGlass += 10; potions += 3; techniqueTalent++; ShowMemory("AS PAUSAS VOLTAM A SER ESCOLHIDAS", "Aven guarda o relato sem registrar quem deixou o sino ou quem escolheu não tocá-lo. A comunidade oferece provisões e reforça sua sintonia com as técnicas. O desenho fica na mesa como convite, não como ordem para preencher o silêncio."); notice = "RECOMPENSA: +2200 OURO • +10 SAL • +3 POÇÕES • TÉCNICA +1"; noticeTime = 4; }
            else if (finalEnding > 0 && questStage >= 56) { forgeMenuOpen = true; secondaryForgeOpen = false; return; }
            else if (finalEnding > 0) ShowMemory(finalEnding == 1 ? "VÉSPERA APRENDE A OUVIR" : "O FAROL SEM CORRENTE", finalEnding == 1 ? "Os arquivos estão abertos e a comunidade divide a proteção da cidade. As memórias devolvidas podem ser ouvidas por quem as reivindicar. Nara permanece livre para escolher quando e se quer voltar." : "O Lume protege Véspera sem chamar as vozes perdidas à força. A comunidade sabe o preço da luz e pode decidir, em conjunto, como mantê-la. Nara permanece livre para escolher quando e se quer voltar.");
            else if (questStage >= 27) { forgeMenuOpen = true; secondaryForgeOpen = false; return; }
            else if (questStage >= 24) ShowMemory("AVEN, O BARQUEIRO", "A mesa continua aberta a quem escolhe deixar um relato. A próxima curva fica a oeste; os viajantes que passaram por aqui decidiram compartilhar apenas o início da trilha.");
            else ShowMemory("AVEN, O BARQUEIRO", "Este barco encalhado virou abrigo. Ninguém precisa escrever um nome para ficar aqui.");
            return;
        }
        if (Distance(player, SeraPosition) < InteractionRadius) { ShowMemory("SERA, REPARADORA DE LANTERNAS", finalEnding == 1 ? "Agora que as memórias foram devolvidas, vou cuidar das lanternas junto com quem quiser ajudar. Nenhuma chama precisa guardar tudo sozinha." : finalEnding == 2 ? "O farol ainda ilumina o rio, mas não chama ninguém de volta. Vou manter uma lanterna baixa para quem chegar por vontade própria." : "As lanternas não marcam uma rota para tomar posse dela. Cada pessoa acende uma só quando quer deixar um aviso para quem vier depois."); return; }
        if (Distance(player, NoaPosition) < InteractionRadius) { ShowMemory("NOA, APRENDIZ DE CARTÓGRAFA", finalEnding > 0 ? "Deixei um espaço em branco no mapa para as viagens que ainda não começaram. Aven diz que um caminho pode existir sem pertencer a quem o desenha." : "Aprendi a desenhar os trechos que meus amigos me deixam ver. O resto do mapa fica em branco por escolha deles."); return; }
        if (player.X < 150) { room = 3; SetupRoom(); return; }
        if (player.X > 790 && questStage >= 56) { questStage = Math.Max(57, questStage); room = 41; SetupRoom(); ShowMemory("CÂMARA DAS BADALADAS NÃO ENVIADAS", "A trilha termina numa oficina onde sinos sem badalo foram deixados para não soar. As marcas descrevem pausas escolhidas, não nomes nem rotas. Ecos tentam fechar o caminho até o poço do contratempo."); return; }
        if (player.X > 790 && questStage >= 54) { room = 39; SetupRoom(); ShowMemory("CAIS DOS ECOS EMPRESTADOS", "O cais fica numa enseada protegida por pedras cobertas de marcas. Cada viajante escolheu um som curto para deixar no arquivo; nenhum registro guarda um nome ou aponta um destino."); return; }
        if (player.X > 790 && questStage >= 50) { room = 37; SetupRoom(); ShowMemory("SOLEIRA DO SILÊNCIO", "Sob a galeria, uma descida feita de pedras sem inscrição atravessa a rocha. A cada passo, o Lume perde força e as vozes deixam de repetir a mesma frase. Nenhuma marca indica que Nara esteve aqui."); return; }
        if (player.X > 790 && questStage >= 47) { room = 35; SetupRoom(); ShowMemory("TRILHA DAS LINHAS ABERTAS", "A leste da fonte, uma trilha de pedra clara se divide muitas vezes. Algumas marcas indicam retornos, outras terminam sem destino. Nenhuma leva o nome de Nara. Ecos de tinta se movem sobre os traços, tentando obrigá-los a apontar todos para o mesmo lugar."); return; }
        if (player.X > 790 && questStage >= 44 && questStage < 47) { room = 33; SetupRoom(); ShowMemory("PASSAGEM DAS LANTERNAS BAIXAS", "Atrás do Refúgio, uma escadaria desce sob a fonte. As lanternas foram cobertas para que sua luz não revelasse rostos nem traçasse caminhos. Um eco pesado guarda a descida para o espelho interior."); return; }
        if (player.X > 790 && questStage >= 41) { room = 31; SetupRoom(); ShowMemory("GARGANTA DO VIDRO MUDO", "A fissura sob o vertedouro desce até uma garganta de cristal escuro. As paredes devolvem o som dos seus passos, mas não guardam nenhum nome. À frente, a pedra se estreita numa fonte antiga."); return; }
        if (player.X > 790 && questStage >= 33) { if (questStage == 33) questStage = 34; room = 27; SetupRoom(); ShowMemory("ESCARPA DO CONTRAFLUXO", "A água que saiu dos moinhos sobe por uma escarpa de pedra negra. As marcas nas paredes se alternam: umas apontam para dentro, outras para fora. Mais adiante, o Coração das Duas Margens pulsa sem escolher qual delas deve receber a corrente."); return; }
        if (player.X > 790 && questStage >= 30) { if (questStage == 30) questStage = 31; room = 25; SetupRoom(); ShowMemory("MOINHOS DA VAZANTE", "Aven reconhece o ruído das rodas de água: elas não moíam grão, mas regulavam a cheia para que a comunidade pudesse atravessar. O canal foi fechado por dentro. A água ainda empurra em direção à câmara."); return; }
        if (player.X > 790 && questStage >= 24) { if (questStage == 24) questStage = 25; room = 21; SetupRoom(); ShowMemory("A VÁRZEA DAS VOZES SOPRADAS", "Além da cisterna, o rio se divide entre campos de sal e pedra lisa. As lanternas acabam aqui. Marcas recentes mostram que viajantes cruzaram a água sem deixar um destino escrito."); return; }
        if (player.X > 790 && questStage >= 22) { room = 19; SetupRoom(); ShowMemory("TRILHA DOS LUMINÁRIOS", "Pequenas lanternas indicam os pontos onde viajantes aceitaram deixar um aviso. As marcas não formam um mapa completo: cada pessoa decidiu quanto do caminho compartilhar."); return; }
        notice = questStage < 22 ? "FALE COM AVEN PARA CONHECER A COMUNIDADE" : "E  SEGUIR A TRILHA DOS LUMINÁRIOS"; noticeTime = 1.5f;
    }
    private void ShowMemory(string titleText, string bodyText) { memoryTitle = titleText; memoryText = bodyText; memoryOverlay = true; }
    private void MarkRoomCleared() { if (room != 3 && room < 43 && enemies.Count == 0 && drops.Count == 0) clearedRooms |= 1L << room; }
    private Enemy? lockedTarget;
    private bool TargetVisible(Enemy enemy)
    {
        if (room != 43) return true;
        PointF camera = CameraOffset();
        return enemy.Pos.X >= camera.X && enemy.Pos.X <= camera.X + 960 && enemy.Pos.Y >= camera.Y + 108 && enemy.Pos.Y <= camera.Y + 553;
    }
    private void CycleTarget()
    {
        var candidates = enemies.Where(TargetVisible).ToList();
        if (candidates.Count == 0) { lockedTarget = null; notice = "NENHUM ALVO VISÍVEL"; noticeTime = 1.4f; return; }
        int index = lockedTarget is null ? -1 : candidates.IndexOf(lockedTarget);
        lockedTarget = candidates[(index + 1) % candidates.Count]; notice = $"ALVO: {EnemyName(lockedTarget)}"; noticeTime = 1.4f;
    }
}
