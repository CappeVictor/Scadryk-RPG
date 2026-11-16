using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

public sealed partial class Game
{
    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; g.Clear(Color.FromArgb(16, 19, 23));
        RectangleF viewport = GameViewport(ClientSize); g.TranslateTransform(viewport.X, viewport.Y); g.ScaleTransform(viewport.Width / 960, viewport.Height / 640);
        using var goldBrush = new SolidBrush(Color.FromArgb(230, 193, 129)); using var textBrush = new SolidBrush(Color.FromArgb(222, 216, 201));
        using var panel = new SolidBrush(Color.FromArgb(28, 32, 35)); using var floor = new SolidBrush(Color.FromArgb(54, 60, 53)); using var tile = new Pen(Color.FromArgb(70, 78, 68));
        using var headerFont = new Font("Georgia", 22, FontStyle.Bold); using var hudFont = new Font("Segoe UI", 12, FontStyle.Bold); using var roomFont = new Font("Georgia", 13, FontStyle.Bold); using var helpFont = new Font("Segoe UI", 10);
        g.FillRectangle(panel, 0, 0, 960, 83); g.DrawString("SCADRYK", headerFont, goldBrush, 24, 18);
        g.DrawString($"VIDA {health:0}/{maxHealth:0}     NÍVEL {level}     XP {xp}/{level * 60}     OURO {gold}     POÇÕES {potions}", hudFont, textBrush, 255, 29);
        g.DrawString($"M{(alternateSkillsActive ? 2 : 1)}  •  F  {ShortSkillName(0)}  •  V  {ShortSkillName(1)}  •  G  {ShortSkillName(2)}  •  C  {ShortSkillName(3)}", helpFont, goldBrush, 255, 61);
        g.DrawString($"RESSONÂNCIA {difficultyTier} • {DifficultyTierName(difficultyTier)}", helpFont, Brushes.Orange, 690, 61);
        if (armorOn && relicOn)
        {
            using var setBadge = new SolidBrush(Color.FromArgb(210, 31, 52, 53));
            using var setText = new SolidBrush(Color.FromArgb(236, 174, 215, 202));
            using var setFont = new Font("Segoe UI", 9, FontStyle.Bold);
            g.FillRectangle(setBadge, 604, 85, 291, 19);
            g.DrawString(veinResonanceTime > 0 ? $"VIGÍLIA  •  -35%  •  PULSO +15% ({veinResonanceTime:0.0} s)" : "VIGÍLIA DO VEIO  •  -35% DANO  •  CURA 8/ABATE", setFont, setText, 610, 87);
        }
        using var healthBack = new SolidBrush(Color.FromArgb(52, 56, 51)); using var healthBar = new SolidBrush(health / maxHealth < .25f ? Color.IndianRed : Color.FromArgb(112, 171, 123));
        g.FillRectangle(healthBack, 25, 58, 190, 11); g.FillRectangle(healthBar, 25, 58, 190 * health / maxHealth, 11);
        if (heroClass == HeroClass.StoneGuardian)
        {
            using var furyBack = new SolidBrush(Color.FromArgb(52, 48, 40)); using var furyBar = new SolidBrush(stoneFury >= 100 ? Color.Gold : Color.FromArgb(206, 128, 71)); using var furyLabel = new SolidBrush(Color.FromArgb(226, 214, 190)); using var furyFont = new Font("Segoe UI", 8, FontStyle.Bold);
            g.DrawString(stoneFury >= 100 ? "FÚRIA DE PEDRA • PRONTA" : $"FÚRIA DE PEDRA • {stoneFury:0}%", furyFont, furyLabel, 25, 69);
            g.FillRectangle(furyBack, 25, 80, 190, 3); g.FillRectangle(furyBar, 25, 80, 190 * Math.Clamp(stoneFury / 100f, 0, 1), 3);
        }
        else if (heroClass == HeroClass.NameWeaver)
        {
            using var threadBack = new SolidBrush(Color.FromArgb(52, 48, 40)); using var threadBar = new SolidBrush(nameThread >= 40 ? Color.FromArgb(210, 150, 226) : Color.FromArgb(135, 104, 164)); using var threadLabel = new SolidBrush(Color.FromArgb(226, 214, 190)); using var threadFont = new Font("Segoe UI", 8, FontStyle.Bold);
            g.DrawString($"FIO DE NOMES • {nameThread:0}%{(nameThread >= 40 ? " • SUTURA PRONTA" : "")}", threadFont, threadLabel, 25, 69);
            g.FillRectangle(threadBack, 25, 80, 190, 3); g.FillRectangle(threadBar, 25, 80, 190 * Math.Clamp(nameThread / 100f, 0, 1), 3);
        }
        else if (heroClass == HeroClass.FordCartographer)
        {
            using var routeBack = new SolidBrush(Color.FromArgb(52, 48, 40)); using var routeBar = new SolidBrush(fordRoute >= 100 ? Color.FromArgb(125, 213, 199) : Color.FromArgb(73, 156, 152)); using var routeLabel = new SolidBrush(Color.FromArgb(226, 214, 190)); using var routeFont = new Font("Segoe UI", 8, FontStyle.Bold);
            g.DrawString(fordRoute >= 100 ? "ROTA • PRONTA PARA O PRÓXIMO GOLPE" : $"ROTA • {fordRoute:0}% EM MOVIMENTO", routeFont, routeLabel, 25, 69);
            g.FillRectangle(routeBack, 25, 80, 190, 3); g.FillRectangle(routeBar, 25, 80, 190 * Math.Clamp(fordRoute / 100f, 0, 1), 3);
        }
        GraphicsState worldTransform = g.Save();
        if (room == 43) g.SetClip(new RectangleF(0, 108, 960, 445));
        if (screenShakeStrength > 0.05f)
            g.TranslateTransform(MathF.Sin(screenShakePhase) * screenShakeStrength, MathF.Cos(screenShakePhase * 1.37f) * screenShakeStrength * 0.72f);
        PointF camera = CameraOffset();
        g.TranslateTransform(-camera.X, -camera.Y);
        if (room == 43) DrawFirstEchoEnvironment(g);
        else if (room is 44 or 45) DrawFirstEchoInterior(g);
        else if (room == 3) DrawTownEnvironment(g, floor, tile);
        else if (room == 18) DrawRefugeEnvironment(g, floor, tile);
        else if (room is 19 or 20) DrawLedgerTrailEnvironment(g, floor, tile);
        else if (room is 21 or 22) DrawUpstreamEnvironment(g, floor, tile);
        else if (room is 23 or 24) DrawReedBankEnvironment(g, floor, tile);
        else if (room is 25 or 26) DrawFloodMillEnvironment(g, floor, tile);
        else if (room is 27 or 28) DrawCounterflowEnvironment(g, floor, tile);
        else if (room is 29 or 30) DrawCantoriaEnvironment(g, floor, tile);
        else if (room is 35 or 36) DrawRouteEnvironment(g, floor, tile);
        else if (room is 37 or 38) DrawFinalEnvironment(g, floor, tile);
        else if (room is 39 or 40) DrawBorrowedArchiveEnvironment(g, floor, tile);
        else if (room is 41 or 42) DrawUnsentChimeEnvironment(g, floor, tile);
        else if (room is 31 or 32) DrawGlassEnvironment(g, floor, tile);
        else if (room is 4 or 5) DrawArchiveEnvironment(g, floor, tile);
        else if (room is 6 or 7) DrawRiverEnvironment(g, floor, tile);
        else if (room is 16 or 17) DrawWestWitnessEnvironment(g, floor, tile);
        else if (room is 14 or 15) DrawFirstVigilEnvironment(g, floor, tile);
        else if (room is 12 or 13) DrawFarReachEnvironment(g, floor, tile);
        else if (room >= 10) DrawNameShoreEnvironment(g, floor, tile);
        else if (room >= 8) DrawLumeEnvironment(g, floor, tile);
        else
        {
            g.FillRectangle(floor, 65, 108, 830, 445); for (int y = 0; y < 7; y++) for (int x = 0; x < 12; x++) g.DrawRectangle(tile, 70 + x * 68, 112 + y * 62, 67, 61);
            using var wallBrush = new SolidBrush(Color.FromArgb(90, 79, 62)); using var wallEdge = new Pen(Color.FromArgb(154, 132, 91), 3); foreach (RectangleF wall in Walls) { g.FillRectangle(wallBrush, wall); g.DrawRectangle(wallEdge, wall.X, wall.Y, wall.Width, wall.Height); }
            using var exitPen = new Pen(enemies.Count == 0 && drops.Count == 0 ? Color.LightGreen : Color.FromArgb(45, 48, 43), 8); g.DrawRectangle(exitPen, 882, 293, 3, 75);
        }
        DrawGroundHazards(g);
        DrawNameRiftBursts(g);
        if (bellWaveTime > 0) DrawBellWave(g);
        if (fordBurstTime > 0) DrawFordBurst(g);
        if (pullWaveTime > 0) DrawEbbWave(g);
        if (refrainWaveTime > 0) DrawRefrainWave(g); if (beaconTime > 0) DrawBeacon(g);
        foreach (EchoShard shard in echoShards) DrawEchoShard(g, shard);
        foreach (Enemy enemy in enemies) DrawEnemy(g, enemy);
        foreach (BowArrow arrow in bowArrows) DrawBowArrow(g, arrow);
        foreach (Drop item in drops) DrawDrop(g, item);
        DrawPlayer(g);
        if (attackFlash > 0) DrawAttackSwing(g);
        if (returnVoiceWaveTime > 0) DrawReturnedVoicesWave(g);
        if (finalStrikeFlash > 0) DrawFinalSparkFlash(g);
        DrawFloatingTexts(g);
        g.Restore(worldTransform);
        string roomTitle = vigilActive ? $"VIGÍLIA ASCENDENTE • ANDAR {vigilFloor}" : room == 3 || room == 18 || room >= 43 ? roomNames[room] : room < 3 ? $"{room + 1} / 3   {roomNames[room]}" : room < 6 ? $"{room - 3} / 2   {roomNames[room]}" : room < 8 ? $"{room - 5} / 2   {roomNames[room]}" : room < 10 ? $"{room - 7} / 2   {roomNames[room]}" : room < 12 ? $"{room - 9} / 2   {roomNames[room]}" : room < 14 ? $"{room - 11} / 2   {roomNames[room]}" : room < 16 ? $"{room - 13} / 2   {roomNames[room]}" : room < 18 ? $"{room - 15} / 2   {roomNames[room]}" : room < 21 ? $"{room - 18} / 2   {roomNames[room]}" : room < 23 ? $"{room - 20} / 2   {roomNames[room]}" : room < 25 ? $"{room - 22} / 2   {roomNames[room]}" : room < 27 ? $"{room - 24} / 2   {roomNames[room]}" : room < 29 ? $"{room - 26} / 2   {roomNames[room]}" : room < 31 ? $"{room - 28} / 2   {roomNames[room]}" : room < 33 ? $"{room - 30} / 2   {roomNames[room]}" : room < 35 ? $"{room - 32} / 2   {roomNames[room]}" : room < 37 ? $"{room - 34} / 2   {roomNames[room]}" : room < 39 ? $"{room - 36} / 2   {roomNames[room]}" : room < 41 ? $"{room - 38} / 2   {roomNames[room]}" : $"{room - 40} / 2   {roomNames[room]}";
        g.DrawString(roomTitle, roomFont, goldBrush, 67, 572);
        int memoriesRead = (memoriesFound & 1) + ((memoriesFound >> 1) & 1);
        g.DrawString($"ECOS {memoriesRead} / 2", hudFont, goldBrush, 760, 572);
        string currentObjective = CurrentObjective();
        if (currentObjective.Length > 0) g.DrawString(currentObjective, hudFont, goldBrush, 67, 592);
        g.DrawString(room is 3 or 18 ? "WASD mover   E falar/investigar   F5 salvar   B técnicas   I inventário   L diário" : "WASD mover   ESPAÇO atacar   SHIFT esquivar   R redemoinho   F/V/G/C técnicas   B montar técnicas   Q poção   E interagir   I inventário   L diário", helpFont, textBrush, 67, 619);
        if (room < MemoryStonePositions.Length && (memoriesFound & (1 << room)) == 0) DrawMemoryStone(g, MemoryStonePositions[room], Distance(player, MemoryStonePositions[room]) < MemoryStoneInteractionRadius);
        if (noticeTime > 0) DrawCentered(g, notice, 170, new Font("Segoe UI", 13, FontStyle.Bold), Color.FromArgb(230, 193, 129));
        if (vigilChoiceOpen) DrawChoice(g, $"VIGÍLIA ASCENDENTE • ANDAR {vigilFloor}", $"1. SUBIR AO ANDAR {vigilFloor + 1} • RECOMPENSA CRESCENTE", $"2. ENCERRAR • MELHOR MARCA: {Math.Max(vigilBestFloor, vigilFloor)}", "3. FICAR AQUI");
        if (title) DrawTitleScreen(g);
        else if (dead || victory) DrawOverlay(g, victory ? "VITÓRIA" : "VOCÊ CAIU", "ENTER PARA RECOMEÇAR    •    F5 SALVAR");
        if (inventoryOpen) DrawInventory(g);
        if (skillMenuOpen) DrawSkillMenu(g);
        if (characterSheetOpen) DrawCharacterSheet(g);
        if (talentChoice) DrawTalentChoice(g, pendingTalents);
        if (chestChoice) DrawChoice(g, "BAÚ DA CRIPTA — ESCOLHA UMA RECOMPENSA", "1. 75 OURO", "2. DUAS POÇÕES", "3. RECUPERAR TODA A VIDA");
        if (endingChoiceOpen) DrawChoice(g, questStage == 51 ? "O QUE VÉSPERA FARÁ COM O LUME?" : "O QUE VÉSPERA DEVE LEMBRAR?", questStage == 51 ? "1. DEVOLVER AS MEMÓRIAS  •  +20 VIDA MÁXIMA" : "1. DEVOLVER OS NOMES  •  +20 VIDA MÁXIMA", questStage == 51 ? "2. OFERECER UMA LEMBRANÇA PRÓPRIA  •  +10 DANO" : "2. MANTER O LUME  •  +10 DANO", "3. ADIAR A DECISÃO");
        if (forgeMenuOpen) DrawForgeMenu(g);
        if (storyOverlay) DrawStoryOverlay(g);
        if (memoryOverlay) DrawMemoryOverlay(g);
        if (mapOpen) DrawMapOverlay(g);
        if (journalOpen) DrawQuestJournal(g);
        if (focusPaused && !title && !dead && !victory) DrawOverlay(g, "PAUSADO", "VOLTE À JANELA PARA CONTINUAR");
    }

    private string CurrentObjective()
    {
        if (room == 43) return firstEchoQuest == 0 ? "OBJETIVO: ENCONTRE IVARA NA ENCRUZILHADA" : firstEchoQuest == 1 ? "OBJETIVO: SIGA À CASA DAS COMPORTAS" : firstEchoQuest == 2 ? "OBJETIVO: RETORNE A VÉSPERA E FALE COM EDDA" : firstEchoBeaconAccepted && !firstEchoBeaconRestored ? "MISSÃO OPCIONAL: LIMPE A ESCARPA E RESTAURE O MARCO" : "EXPLORE AS MARGENS DO PRIMEIRO ECO";
        if (room is 44 or 45) return room == 44 ? "OBJETIVO: EXPLORE O ABRIGO NAS RAÍZES" : firstEchoQuest < 2 ? "OBJETIVO: LIBERTE AS VOZES DAS COMPORTAS" : "OBJETIVO: RETORNE ÀS MARGENS";
        if (room == 3) return firstEchoQuest == 2 ? "MISSÃO OPCIONAL: CONTE A EDDA SOBRE AS COMPORTAS" : TownObjective();
        if (room == 18) return "OBJETIVO: " + (questStage == 46 ? "FALE COM AVEN SOBRE AS LINHAS" : questStage == 47 ? "SIGA AS LINHAS ATÉ A GALERIA" : questStage == 48 ? "LEVE O TRAÇADO DE VOLTA A AVEN" : questStage == 49 ? "FALE COM AVEN PARA ABRIR O ÚLTIMO CAPÍTULO" : questStage == 50 ? "DESÇA À SOLEIRA DO SILÊNCIO" : questStage == 51 ? "FALE COM AVEN SOBRE O DESTINO DO LUME" : questStage >= 59 ? "EXPEDIÇÃO XVIII CONCLUÍDA • EXPLORAÇÃO ABERTA" : questStage == 58 ? "LEVE A BADALADA RECUPERADA A AVEN" : questStage == 57 ? "SIGA À CÂMARA DAS BADALADAS" : questStage == 56 ? "EXPEDIÇÃO XVII CONCLUÍDA • NOVA EXPEDIÇÃO DISPONÍVEL A LESTE" : questStage >= 56 ? "EXPEDIÇÃO XVII CONCLUÍDA • EXPLORAÇÃO ABERTA" : questStage == 55 ? "LEVE A MEMÓRIA DO ARQUIVO A AVEN" : questStage == 54 ? "SIGA AO CAIS DOS ECOS EMPRESTADOS" : questStage >= 52 ? "EPÍLOGO CONCLUÍDO • EXPLORAÇÃO ABERTA" : questStage < 22 ? "CONHEÇA A COMUNIDADE E FALE COM AVEN" : questStage == 22 ? "SIGA A TRILHA E RECUPERE O REGISTRO COMPARTILHADO" : questStage == 23 ? "LEVE O REGISTRO DE VOLTA A AVEN" : questStage == 32 ? "LEVE A MEMÓRIA DA VAZANTE A AVEN" : questStage == 30 ? "SIGA O SOM DOS MOINHOS PARA OESTE" : questStage == 35 ? "LEVE A MEMÓRIA DO COMPASSO A AVEN" : questStage == 39 ? "LEVE A NOTA DA CONTRAVOZ A AVEN" : questStage == 40 ? "FALE COM AVEN SOBRE A PRÓXIMA FISSURA" : questStage == 42 ? "LEVE O ECO SEM DONO A AVEN" : questStage >= 41 ? "SIGA OS PASSOS ATÉ A FONTE SOB A PEDRA" : questStage >= 36 ? "MANTO DE LASTRO APRENDIDO • SIGA AS ÁGUAS" : questStage >= 33 ? "SIGA A CORRENTE AO CORAÇÃO DAS DUAS MARGENS" : questStage >= 27 ? "FALE COM AVEN PARA REFORJAR UMA ARMA" : "SIGA A PRÓXIMA CURVA A OESTE");
        if (room is 27 or 28) return room == 27 ? "OBJETIVO: ATRAVESSE A ESCARPA DO CONTRAFLUXO" : questStage < 35 ? "OBJETIVO: DERROTE O AFINADOR E RECOLHA A MEMÓRIA" : "OBJETIVO: RETORNE AO REFÚGIO E FALE COM AVEN";
        if (room is 29 or 30) return room == 29 ? (enemies.Count > 0 || drops.Count > 0 ? "OBJETIVO: ABRA A CANTARIA E LIMPE OS ECOS" : "OBJETIVO: SIGA A CONTRAVOZ ATÉ A CÂMARA") : riftVerse ? "OBJETIVO: LEVE A NOTA RECUPERADA A AVEN" : "OBJETIVO: INTERROMPA A CONTRAVOZ E RECOLHA A NOTA";
        if (room is 33 or 34) return LanternObjective();
        if (room is 35 or 36) return room == 35 ? (enemies.Count > 0 || drops.Count > 0 ? "OBJETIVO: ATRAVESSE AS LINHAS ABERTAS" : "OBJETIVO: SIGA ATÉ A GALERIA") : routeMemory ? "OBJETIVO: LEVE O TRAÇADO A AVEN" : "OBJETIVO: DERROTE O ESCRIBA E RECUPERE O TRAÇADO";
        if (room is 41 or 42) return room == 41 ? enemies.Count > 0 || drops.Count > 0 ? "OBJETIVO: ATRAVESSE A CÂMARA DOS SONS PAUSADOS" : "OBJETIVO: DESÇA AO POÇO DO CONTRATEMPO" : unstruckTrace ? "OBJETIVO: RETORNE A AVEN COM A LEMBRANÇA" : "OBJETIVO: INTERROMPA O AFINADOR E RECUPERE A BADALADA";
        if (room is 39 or 40) return room == 39 ? enemies.Count > 0 || drops.Count > 0 ? "OBJETIVO: ATRAVESSE O CAIS DOS ECOS" : "OBJETIVO: ENTRE NO ARQUIVO DA MARÉ" : borrowedTrace ? "OBJETIVO: RETORNE A AVEN COM A MEMÓRIA" : "OBJETIVO: DERROTE O ARQUIVISTA E RECOLHA O ECO";
        if (room is 37 or 38) return vigilActive ? enemies.Count > 0 ? $"VIGÍLIA {vigilFloor}: DERROTE O ECO E RECOLHA O SAQUE" : drops.Count > 0 ? $"VIGÍLIA {vigilFloor}: RECOLHA O SAQUE" : $"VIGÍLIA {vigilFloor}: INTERAJA COM O ALTAR PARA SUBIR OU ENCERRAR" : room == 37 ? enemies.Count > 0 || drops.Count > 0 ? "OBJETIVO: ATRAVESSE A SOLEIRA DO SILÊNCIO" : "OBJETIVO: DESÇA À CÂMARA DO ÚLTIMO ECO" : silenceMemory ? "OBJETIVO: RETORNE A AVEN E DECIDA O DESTINO DO LUME" : "OBJETIVO: DERROTE O SILÊNCIO E RECOLHA A MEMÓRIA";
        if (room is 31 or 32) return room == 31 ? (enemies.Count > 0 || drops.Count > 0 ? "OBJETIVO: ATRAVESSE A GARGANTA DO VIDRO MUDO" : "OBJETIVO: SIGA OS PASSOS ATÉ A FONTE") : deepTrace ? "OBJETIVO: LEVE O ECO SEM DONO A AVEN" : "OBJETIVO: DERROTE O ARAUTO E RECOLHA A LEMBRANÇA";
        if (room is >= 4 and < 6) return ArchiveObjective();
        if (room is 6 or 7) return RiverObjective();
        if (room >= 8) return LumeObjective();
        return "";
    }

    private string TownObjective() => questStage switch
    {
        0 => "OBJETIVO: ENCONTRE A CURADORA SILETH",
        1 => "OBJETIVO: LEIA O LIVRO DAS AUSÊNCIAS",
        2 => "OBJETIVO: VOLTE E FALE COM SILETH",
        3 => "OBJETIVO: ENTRE NO ARQUIVO NORTE AO NORTE",
        4 => "OBJETIVO: MOSTRE O SELO DA VIGÍLIA A SILETH",
        5 => "OBJETIVO: DESÇA AO LEITO DO RIO CEGO AO SUL",
        6 => "OBJETIVO: MOSTRE A EDDA O SINO DE NARA",
        7 => "OBJETIVO: DESÇA A ESCADA ABERTA PELO SINO",
        9 => "OBJETIVO: MOSTRE O PRISMA DO LUME A SILETH",
        10 => "OBJETIVO: DESÇA ALÉM DO PRISMA",
        11 => "OBJETIVO: MOSTRE A NOVA MEMÓRIA A SILETH",
        12 => "OBJETIVO: DECIDA COM SILETH O QUE VÉSPERA DEVE LEMBRAR",
        13 => townEnding == 1 ? "OS NOMES VOLTARAM • ATRAVESSE O RESERVATÓRIO" : "O LUME PERMANECE • ATRAVESSE O RESERVATÓRIO",
        14 => "OBJETIVO: LEVE O CORO DOS NOMES A SILETH",
        15 => "OBJETIVO: INVESTIGUE A PASSAGEM SOB O CLAUSTRO",
        16 => "OBJETIVO: LEVE A MEMÓRIA DE ILYRA A SILETH",
        17 => "OBJETIVO: PEÇA A EDDA PARA CRUZAR O RIO",
        18 => "OBJETIVO: ATRAVESSE O VAU DOS NOMES ESCOLHIDOS",
        19 => "OBJETIVO: LEVE O MAPA DE NARA A EDDA",
        20 => "OBJETIVO: FALE COM EDDA PARA SEGUIR A OESTE",
        21 => "OBJETIVO: FALE COM AVEN NO REFÚGIO",
        22 => "OBJETIVO: ATRAVESSE A TRILHA E RECUPERE O REGISTRO",
        23 => "OBJETIVO: DEVOLVA O REGISTRO À COMUNIDADE",
        24 => "A PRÓXIMA CURVA SEGUE A OESTE • SEM RASTRO ATUAL",
        _ => "PRÓXIMO DESTINO: REÚNA AS MEMÓRIAS QUE FALTAM"
    };

    private void DrawTownEnvironment(Graphics g, Brush ground, Pen tile)
    {
        g.FillRectangle(ground, 65, 108, 830, 445);
        for (int y = 0; y < 7; y++) for (int x = 0; x < 12; x++) g.DrawRectangle(tile, 70 + x * 68, 112 + y * 62, 67, 61);
        using var road = new SolidBrush(Color.FromArgb(94, 84, 68));
        using var plaza = new SolidBrush(Color.FromArgb(105, 96, 78));
        using var trim = new Pen(Color.FromArgb(159, 139, 101), 2);
        g.FillRectangle(road, 345, 274, 315, 118); g.FillRectangle(road, 430, 242, 116, 210);
        g.FillEllipse(plaza, 424, 290, 128, 92); g.DrawEllipse(trim, 424, 290, 128, 92);
        using var water = new SolidBrush(townEnding == 1 ? Color.FromArgb(123, 180, 153) : townEnding == 2 ? Color.FromArgb(183, 132, 77) : Color.FromArgb(88, 142, 145));
        g.FillEllipse(water, 458, 315, 60, 42); g.DrawEllipse(trim, 458, 315, 60, 42);
        if (townEnding != 0) DrawCentered(g, townEnding == 1 ? "OS NOMES VOLTARAM" : "O LUME PERMANECE", 402, new Font("Segoe UI", 9, FontStyle.Bold), townEnding == 1 ? Color.LightGreen : Color.PeachPuff);
        DrawTownHouse(g, TownBuildings[0], "FERRARIA"); DrawTownHouse(g, TownBuildings[1], "ARQUIVO");
        DrawTownHouse(g, TownBuildings[2], "CASAS"); DrawTownHouse(g, TownBuildings[3], "ARMAZÉM");
        using var gateBrush = new SolidBrush(Color.FromArgb(116, 100, 70)); using var gatePen = new Pen(Color.FromArgb(220, 189, 125), 3);
        g.FillRectangle(gateBrush, 70, 298, 30, 64); g.DrawRectangle(gatePen, 70, 298, 30, 64);
        g.FillRectangle(gateBrush, 860, 298, 30, 64); g.DrawRectangle(gatePen, 860, 298, 30, 64);
        DrawCentered(g, "MARGENS", 267, new Font("Segoe UI", 8, FontStyle.Bold), Color.FromArgb(230, 193, 129), 838);
        if (player.X > 810 && player.Y > 280 && player.Y < 380) DrawCentered(g, "E  MARGENS DO PRIMEIRO ECO", 385, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 775);
        using var bookBrush = new SolidBrush(Color.FromArgb(107, 56, 43)); using var bookEdge = new Pen(Color.FromArgb(234, 204, 133), 2);
        g.FillRectangle(bookBrush, ArchiveBookPosition.X - 13, ArchiveBookPosition.Y - 7, 26, 18); g.DrawRectangle(bookEdge, ArchiveBookPosition.X - 13, ArchiveBookPosition.Y - 7, 26, 18);
        DrawTownPerson(g, CuratorPosition, "SILETH", Color.FromArgb(117, 73, 104), Distance(player, CuratorPosition) < InteractionRadius);
        DrawTownPerson(g, BellKeeperPosition, "EDDA", Color.FromArgb(111, 137, 124), Distance(player, BellKeeperPosition) < InteractionRadius);
        if (firstEchoQuest == 2) DrawCentered(g, "!", BellKeeperPosition.Y - 62, new Font("Segoe UI", 18, FontStyle.Bold), Color.Gold, BellKeeperPosition.X);
        if (Distance(player, ArchiveBookPosition) < InteractionRadius) DrawCentered(g, "E  LER", ArchiveBookPosition.Y + 22, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, ArchiveBookPosition.X);
        if (Distance(player, CryptGatePosition) < InteractionRadius) DrawCentered(g, "E  CRIPTA", CryptGatePosition.Y + 42, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, CryptGatePosition.X + 15);
        PointF archiveGate = new(480, 135);
        using var portal = new Pen(questStage >= 3 ? Color.FromArgb(219, 178, 112) : Color.FromArgb(88, 75, 61), 5);
        g.DrawArc(portal, archiveGate.X - 28, archiveGate.Y - 24, 56, 52, 180, 180);
        DrawCentered(g, questStage >= 3 ? "ARQUIVO NORTE" : "PORTÃO SELADO", archiveGate.Y - 38, new Font("Segoe UI", 8, FontStyle.Bold), Color.FromArgb(230, 193, 129), archiveGate.X);
        if (Distance(player, archiveGate) < InteractionRadius) DrawCentered(g, "E  ENTRAR", archiveGate.Y + 28, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, archiveGate.X);
        PointF riverGate = new(480, 520);
        using var riverPortal = new Pen(questStage >= 5 ? Color.FromArgb(112, 179, 185) : Color.FromArgb(72, 85, 82), 5);
        g.DrawArc(riverPortal, riverGate.X - 28, riverGate.Y - 22, 56, 50, 0, 180);
        DrawCentered(g, questStage >= 7 ? "ESCADA DO LUME" : questStage >= 5 ? "RIO CEGO" : "LEITO SELADO", riverGate.Y - 4, new Font("Segoe UI", 8, FontStyle.Bold), Color.FromArgb(190, 211, 196), riverGate.X);
        if (Distance(player, riverGate) < InteractionRadius) DrawCentered(g, "E  DESCER", riverGate.Y - 38, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, riverGate.X);
    }

    private string ArchiveObjective() => room == 4 ? "OBJETIVO: CRUZE A GALERIA E ENCONTRE O COFRE" : archiveSeal ? "OBJETIVO: VOLTE A VÉSPERA COM O SELO" : "OBJETIVO: RECUPERE O REGISTRO NO COFRE";
    private string RiverObjective() => room == 6 ? "OBJETIVO: SIGA A CORRENTE ATÉ A CAPELA" : riverBell ? "OBJETIVO: RETORNE E MOSTRE O SINO A EDDA" : "OBJETIVO: ENCONTRE O SINO DE TRAVESSIA";
    private string LanternObjective() => room == 33 ? enemies.Count > 0 || drops.Count > 0 ? "OBJETIVO: ATRAVESSE AS LANTERNAS BAIXAS" : "OBJETIVO: SIGA ATÉ O OBSERVATÓRIO" : lanternSignal ? "OBJETIVO: LEVE O SINAL DA LUZ A AVEN" : "OBJETIVO: DERROTE O REGENTE E RECUPERE O SINAL";
    private string LumeObjective() => room == 8 ? "OBJETIVO: SIGA A ESCADA ATÉ A CÂMARA DO LUME" : room == 9 && questStage >= 10 && !naraTrace ? "OBJETIVO: INVESTIGUE A PASSAGEM SOB O PRISMA" : room == 10 ? "OBJETIVO: ATRAVESSE A MARGEM SEM REFLEXO" : room == 11 && !naraTrace ? "OBJETIVO: ENFRENTE O QUE DEVORA NOMES" : room == 11 && questStage >= 13 ? "OBJETIVO: CRUZE A COSTA ALÉM DO RESERVATÓRIO" : room == 12 ? "OBJETIVO: SIGA O CORO ATÉ O CLAUSTRO" : room == 13 && !chorusMemory ? "OBJETIVO: DERROTE A TECELÃ E RECUPERE AS VOZES" : room == 13 && questStage < 15 ? "OBJETIVO: LEVE O CORO DE VOLTA A SILETH" : room == 13 ? "OBJETIVO: DESÇA PELA BACIA SOB O CORO" : room == 14 ? "OBJETIVO: ATRAVESSE A BACIA DO PRIMEIRO NOME" : room == 15 ? "OBJETIVO: DERROTE A PRIMEIRA VIGÍLIA" : room == 16 ? "OBJETIVO: CRUZE O VAU DOS NOMES ESCOLHIDOS" : room == 17 ? "OBJETIVO: DERROTE A CARTÓGRAFA E RECUPERE O MAPA" : room == 19 ? "OBJETIVO: SIGA AS LANTERNAS ATÉ A CISTERNA" : room == 20 ? "OBJETIVO: DERROTE O ESCRIVÃO E RECOLHA O REGISTRO" : room == 21 ? "OBJETIVO: SIGA A VÁRZEA ATÉ A PONTE" : room == 22 && !upstreamTrace ? "OBJETIVO: DERROTE A TESTEMUNHA DA MARÉ" : room == 22 ? "OBJETIVO: LEVE A LEMBRANÇA DE VOLTA A AVEN" : room == 23 ? "OBJETIVO: CRUZE O COTOVELO DOS JUNCAIS" : room == 24 && !beyondTrace ? "OBJETIVO: DERROTE A GUARDIÃ E RECUPERE O SINAL" : room == 24 ? "OBJETIVO: LEVE O SINAL DE PASSAGEM A AVEN" : room == 25 ? "OBJETIVO: ATRAVESSE O CANAL CONTRA A CORRENTE" : room == 26 && questStage < 32 ? "OBJETIVO: DERROTE O VIGIA DO VEIO PRESO" : room == 26 ? "OBJETIVO: LEVE A MEMÓRIA DA VAZANTE A AVEN" : room >= 10 && naraTrace ? "OBJETIVO: LEVE O RASTRO DE NARA A SILETH" : lumeLens ? "OBJETIVO: RETORNE E MOSTRE O PRISMA A SILETH" : "OBJETIVO: RECUPERE O PRISMA DA PRIMEIRA VIGÍLIA";

    private void DrawArchiveEnvironment(Graphics g, Brush ground, Pen tile)
    {
        g.FillRectangle(ground, 65, 108, 830, 445);
        for (int y = 0; y < 7; y++) for (int x = 0; x < 12; x++) g.DrawRectangle(tile, 70 + x * 68, 112 + y * 62, 67, 61);
        RectangleF[] shelves = room == 4 ? ArchiveWalls : VaultWalls;
        using var shelf = new SolidBrush(Color.FromArgb(69, 52, 43)); using var spine = new Pen(Color.FromArgb(162, 126, 79), 2); using var edge = new Pen(Color.FromArgb(184, 145, 94), 3);
        foreach (RectangleF area in shelves)
        {
            g.FillRectangle(shelf, area); g.DrawRectangle(edge, area.X, area.Y, area.Width, area.Height);
            for (float y = area.Y + 18; y < area.Bottom - 8; y += 25) { g.DrawLine(spine, area.X + 5, y, area.Right - 5, y); for (float x = area.X + 8; x < area.Right - 6; x += 12) g.DrawLine(Pens.SaddleBrown, x, y - 15, x, y - 2); }
        }
        using var threshold = new Pen(enemies.Count == 0 && drops.Count == 0 ? Color.LightGreen : Color.FromArgb(60, 71, 66), 7);
        g.DrawLine(threshold, 885, 290, 885, 370); g.DrawLine(threshold, 75, 290, 75, 370);
        using var glow = new SolidBrush(Color.FromArgb(34, 144, 194, 181));
        g.FillEllipse(glow, 464, 290, 64, 64);
    }

    private void DrawRiverEnvironment(Graphics g, Brush ground, Pen tile)
    {
        using var riverbed = new SolidBrush(Color.FromArgb(71, 76, 65)); using var stones = new SolidBrush(Color.FromArgb(91, 96, 80)); using var bank = new Pen(Color.FromArgb(124, 130, 105), 3);
        g.FillRectangle(ground, 65, 108, 830, 445);
        for (int y = 0; y < 7; y++) for (int x = 0; x < 12; x++) g.DrawRectangle(tile, 70 + x * 68, 112 + y * 62, 67, 61);
        g.FillPolygon(riverbed, new[] { new PointF(65, 272), new PointF(330, 306), new PointF(570, 285), new PointF(895, 317), new PointF(895, 407), new PointF(580, 374), new PointF(315, 408), new PointF(65, 380) });
        g.DrawLine(bank, 70, 272, 330, 306); g.DrawLine(bank, 330, 306, 570, 285); g.DrawLine(bank, 570, 285, 890, 317);
        g.DrawLine(bank, 70, 380, 315, 408); g.DrawLine(bank, 315, 408, 580, 374); g.DrawLine(bank, 580, 374, 890, 407);
        using var rock = new SolidBrush(Color.FromArgb(103, 103, 86)); using var slate = new SolidBrush(Color.FromArgb(72, 86, 87));
        for (int i = 0; i < 14; i++) { float x = 105 + i * 57; float y = 321 + (i % 3) * 18; g.FillEllipse(i % 4 == 0 ? slate : stones, x, y, 22 + i % 3 * 5, 12 + i % 2 * 4); }
        RectangleF[] obstacles = room == 6 ? RiverWalls : BellChamberWalls;
        using var edge = new Pen(Color.FromArgb(159, 146, 112), 3);
        foreach (RectangleF area in obstacles) { g.FillEllipse(rock, area); g.DrawEllipse(edge, area); }
        using var gate = new Pen(enemies.Count == 0 && drops.Count == 0 ? Color.LightGreen : Color.FromArgb(53, 70, 72), 7);
        g.DrawLine(gate, 885, 290, 885, 370); g.DrawLine(gate, 75, 290, 75, 370);
        if (room == 7) { using var glow = new SolidBrush(Color.FromArgb(44, 116, 190, 197)); g.FillEllipse(glow, 444, 250, 80, 80); }
    }

    private void DrawLumeEnvironment(Graphics g, Brush ground, Pen tile)
    {
        using var ash = new SolidBrush(Color.FromArgb(43, 42, 40)); using var seam = new Pen(Color.FromArgb(96, 83, 64), 2); using var gold = new Pen(Color.FromArgb(171, 126, 67), 3);
        g.FillRectangle(ash, 65, 108, 830, 445);
        for (int y = 0; y < 7; y++) for (int x = 0; x < 12; x++) g.DrawRectangle(tile, 70 + x * 68, 112 + y * 62, 67, 61);
        RectangleF[] pillars = room == 8 ? AshGalleryWalls : LumeChamberWalls;
        using var pillarBrush = new SolidBrush(Color.FromArgb(73, 69, 59));
        foreach (RectangleF pillar in pillars) { g.FillRectangle(pillarBrush, pillar); g.DrawRectangle(gold, pillar.X, pillar.Y, pillar.Width, pillar.Height); }
        for (int i = 0; i < 5; i++) { float x = 170 + i * 150; g.DrawLine(seam, x, 115, x - 26, 212); g.DrawLine(seam, x - 26, 212, x + 14, 304); }
        if (room == 9)
        {
            using var glow = new SolidBrush(Color.FromArgb(42, 255, 178, 76)); using var core = new SolidBrush(Color.FromArgb(190, 248, 191, 104));
            g.FillEllipse(glow, 410, 205, 140, 140); g.FillEllipse(core, 462, 257, 36, 36); g.DrawEllipse(gold, 430, 225, 100, 100); g.DrawEllipse(gold, 445, 240, 70, 70);
        }
        using var gate = new Pen(enemies.Count == 0 && drops.Count == 0 ? Color.LightGreen : Color.FromArgb(66, 63, 51), 7);
        g.DrawLine(gate, 885, 290, 885, 370); g.DrawLine(gate, 75, 290, 75, 370);
        if (room == 9 && questStage >= 10) { DrawCentered(g, "PASSAGEM SOB O PRISMA", 480, new Font("Segoe UI", 9, FontStyle.Bold), Color.FromArgb(187, 218, 205)); if (Distance(player, new PointF(480, 500)) < 72) DrawCentered(g, "E  DESCER", 510, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite); }
    }

    private void DrawNameShoreEnvironment(Graphics g, Brush ground, Pen tile)
    {
        using var depth = new SolidBrush(Color.FromArgb(34, 43, 48)); using var stone = new SolidBrush(Color.FromArgb(63, 74, 74));
        using var edge = new Pen(Color.FromArgb(113, 143, 137), 2); using var seam = new Pen(Color.FromArgb(66, 111, 111), 2);
        g.FillRectangle(depth, 65, 108, 830, 445); g.FillRectangle(ground, 94, 132, 772, 397);
        for (int y = 0; y < 7; y++) for (int x = 0; x < 12; x++) g.DrawRectangle(tile, 70 + x * 68, 112 + y * 62, 67, 61);
        RectangleF[] pillars = room == 10 ? NameShoreWalls : VoiceWellWalls;
        foreach (RectangleF pillar in pillars) { g.FillRectangle(stone, pillar); g.DrawRectangle(edge, pillar.X, pillar.Y, pillar.Width, pillar.Height); }
        for (int i = 0; i < 6; i++) { float x = 155 + i * 126; g.DrawLine(seam, x, 128, x + 32, 254); g.DrawLine(seam, x + 32, 254, x - 20, 382); g.DrawLine(seam, x - 20, 382, x + 18, 520); }
        using var pool = new SolidBrush(Color.FromArgb(room == 11 ? 54 : 28, 125, 193, 185));
        g.FillEllipse(pool, room == 11 ? 370 : 345, room == 11 ? 235 : 185, room == 11 ? 220 : 270, room == 11 ? 190 : 280);
        if (room == 11) { using var core = new SolidBrush(Color.FromArgb(185, 210, 227, 188)); g.FillEllipse(core, 462, 338, 36, 36); }
        using var gate = new Pen(enemies.Count == 0 && drops.Count == 0 ? Color.LightGreen : Color.FromArgb(66, 82, 77), 7);
        g.DrawLine(gate, 885, 290, 885, 370); g.DrawLine(gate, 75, 290, 75, 370);
        if (room == 11 && questStage >= 13) DrawCentered(g, "COSTA DAS VOZES TARDIAS", 382, new Font("Segoe UI", 9, FontStyle.Bold), townEnding == 1 ? Color.LightGreen : Color.PeachPuff);
    }

    private void DrawFarReachEnvironment(Graphics g, Brush ground, Pen tile)
    {
        using var deep = new SolidBrush(room == 12 ? Color.FromArgb(29, 46, 51) : Color.FromArgb(38, 35, 48));
        using var stone = new SolidBrush(room == 12 ? Color.FromArgb(70, 83, 78) : Color.FromArgb(78, 67, 83));
        using var edge = new Pen(room == 12 ? Color.FromArgb(142, 177, 155) : Color.FromArgb(179, 147, 190), 2);
        using var current = new Pen(Color.FromArgb(70, 160, 201, 188), 3);
        g.FillRectangle(deep, 65, 108, 830, 445); g.FillRectangle(ground, 94, 132, 772, 397);
        for (int y = 0; y < 7; y++) for (int x = 0; x < 12; x++) g.DrawRectangle(tile, 70 + x * 68, 112 + y * 62, 67, 61);
        RectangleF[] walls = room == 12 ? FarReachWalls : NameCloisterWalls;
        foreach (RectangleF wall in walls) { g.FillRectangle(stone, wall); g.DrawRectangle(edge, wall.X, wall.Y, wall.Width, wall.Height); }
        for (int i = 0; i < 5; i++)
        {
            float x = 152 + i * 152;
            g.DrawBezier(current, x, 155, x + 58, 210 + i % 2 * 18, x - 52, 268 + i % 2 * 14, x + 12, 320);
            g.DrawBezier(current, x + 12, 320, x + 68, 374, x - 34, 444, x + 18, 520);
        }
        using var pool = new SolidBrush(Color.FromArgb(room == 13 ? 56 : 32, 128, 181, 177));
        g.FillEllipse(pool, room == 13 ? 377 : 327, room == 13 ? 224 : 193, room == 13 ? 206 : 306, room == 13 ? 200 : 268);
        if (room == 13)
        {
            using var knot = new SolidBrush(Color.FromArgb(180, 176, 119, 188));
            using var thread = new Pen(Color.FromArgb(210, 213, 178, 218), 3);
            g.FillEllipse(knot, 451, 292, 58, 58);
            for (int i = 0; i < 8; i++) { double a = i * Math.PI / 4; g.DrawLine(thread, 480, 321, 480 + (float)Math.Cos(a) * 82, 321 + (float)Math.Sin(a) * 82); }
        }
        using var gate = new Pen(enemies.Count == 0 && drops.Count == 0 ? Color.LightGreen : Color.FromArgb(76, 78, 85), 7);
        g.DrawLine(gate, 885, 290, 885, 370); g.DrawLine(gate, 75, 290, 75, 370);
        if (room == 12 && Distance(player, new PointF(842, 330)) < 80) DrawCentered(g, "E  ENTRAR NO CLAUSTRO", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
        if (room == 13 && chorusMemory && Distance(player, new PointF(842, 330)) < 80) DrawCentered(g, "E  VOLTAR A VÉSPERA", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
        if (room == 13 && questStage >= 15) { using var stair = new Pen(Color.FromArgb(220, 208, 178, 123), 5); g.DrawArc(stair, 447, 459, 66, 66, 15, 150); if (Distance(player, new PointF(480, 500)) < 78) DrawCentered(g, "E  DESCER À BACIA", 535, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 480); }
    }

    private void DrawFirstVigilEnvironment(Graphics g, Brush ground, Pen tile)
    {
        using var deep = new SolidBrush(Color.FromArgb(24, 30, 39)); using var stone = new SolidBrush(Color.FromArgb(72, 70, 64));
        using var edge = new Pen(room == 14 ? Color.FromArgb(133, 172, 166) : Color.FromArgb(196, 159, 106), 2);
        using var echo = new Pen(Color.FromArgb(88, 191, 178, 133), 3); using var basin = new SolidBrush(Color.FromArgb(42, 115, 155, 160));
        g.FillRectangle(deep, 65, 108, 830, 445); g.FillRectangle(ground, 94, 132, 772, 397);
        for (int y = 0; y < 7; y++) for (int x = 0; x < 12; x++) g.DrawRectangle(tile, 70 + x * 68, 112 + y * 62, 67, 61);
        RectangleF[] walls = room == 14 ? FirstBasinWalls : FirstVigilWalls;
        foreach (RectangleF wall in walls) { g.FillRectangle(stone, wall); g.DrawRectangle(edge, wall.X, wall.Y, wall.Width, wall.Height); }
        for (int i = 0; i < 4; i++) { int inset = i * 19; g.DrawEllipse(echo, 383 + inset, 232 + inset, 194 - inset * 2, 194 - inset * 2); }
        if (room == 14) { g.FillEllipse(basin, 415, 278, 130, 104); DrawCentered(g, "ILYRA", 319, new Font("Georgia", 10, FontStyle.Bold), Color.FromArgb(202, 187, 148)); }
        else { using var firstLight = new SolidBrush(Color.FromArgb(120, 208, 174, 111)); g.FillEllipse(firstLight, 454, 292, 52, 52); }
        using var gate = new Pen(enemies.Count == 0 && drops.Count == 0 ? Color.LightGreen : Color.FromArgb(76, 78, 85), 7);
        g.DrawLine(gate, 885, 290, 885, 370); g.DrawLine(gate, 75, 290, 75, 370);
        if (room == 14 && Distance(player, new PointF(842, 330)) < 80) DrawCentered(g, "E  IR À PRIMEIRA VIGÍLIA", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
        if (room == 15 && firstMemory && Distance(player, new PointF(842, 330)) < 80) DrawCentered(g, "E  VOLTAR A VÉSPERA", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
    }

    private void DrawWestWitnessEnvironment(Graphics g, Brush ground, Pen tile)
    {
        using var deep = new SolidBrush(Color.FromArgb(22, 34, 39)); using var wood = new SolidBrush(Color.FromArgb(91, 77, 57));
        using var edge = new Pen(room == 16 ? Color.FromArgb(119, 160, 153) : Color.FromArgb(193, 172, 127), 2);
        using var water = new SolidBrush(Color.FromArgb(62, 70, 135, 139)); using var chalk = new Pen(Color.FromArgb(132, 218, 201, 160), 3);
        g.FillRectangle(deep, 65, 108, 830, 445); g.FillRectangle(ground, 94, 132, 772, 397);
        for (int y = 0; y < 7; y++) for (int x = 0; x < 12; x++) g.DrawRectangle(tile, 70 + x * 68, 112 + y * 62, 67, 61);
        RectangleF[] walls = room == 16 ? WestFordWalls : WitnessYardWalls;
        foreach (RectangleF wall in walls) { g.FillRectangle(wood, wall); g.DrawRectangle(edge, wall.X, wall.Y, wall.Width, wall.Height); }
        if (room == 16)
        {
            g.FillRectangle(water, 328, 125, 80, 424); g.FillRectangle(water, 558, 125, 78, 424);
            for (int y = 148; y < 520; y += 52) { g.DrawLine(edge, 408, y, 558, y + 12); g.DrawLine(edge, 408, y + 14, 558, y + 26); }
            for (int i = 0; i < 6; i++) g.DrawLine(chalk, 138 + i * 126, 145, 138 + i * 126, 191);
        }
        else
        {
            using var table = new SolidBrush(Color.FromArgb(110, 125, 112, 83)); g.FillEllipse(table, 401, 262, 158, 114);
            for (int i = 0; i < 5; i++) { float x = 435 + i * 22; g.DrawLine(chalk, x, 286, 466 + (i % 2) * 19, 350); }
        }
        using var gate = new Pen(enemies.Count == 0 && drops.Count == 0 ? Color.LightGreen : Color.FromArgb(76, 78, 85), 7);
        g.DrawLine(gate, 885, 290, 885, 370); g.DrawLine(gate, 75, 290, 75, 370);
        if (room == 16 && Distance(player, new PointF(118, 330)) < 78) DrawCentered(g, "E  VOLTAR A VÉSPERA", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 146);
        if (room == 16 && Distance(player, new PointF(842, 330)) < 80) DrawCentered(g, "E  IR AO PÁTIO", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
        if (room == 17 && westTrace && Distance(player, new PointF(842, 330)) < 80) DrawCentered(g, "E  VOLTAR A VÉSPERA", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
    }

    private void DrawRefugeEnvironment(Graphics g, Brush ground, Pen tile)
    {
        using var deep = new SolidBrush(finalEnding == 1 ? Color.FromArgb(25, 48, 43) : finalEnding == 2 ? Color.FromArgb(35, 39, 46) : Color.FromArgb(24, 37, 39)); using var wood = new SolidBrush(Color.FromArgb(93, 83, 61));
        using var edge = new Pen(Color.FromArgb(165, 153, 116), 2); using var path = new Pen(Color.FromArgb(121, 135, 103), 8);
        g.FillRectangle(deep, 65, 108, 830, 445); g.FillRectangle(ground, 94, 132, 772, 397);
        for (int y = 0; y < 7; y++) for (int x = 0; x < 12; x++) g.DrawRectangle(tile, 70 + x * 68, 112 + y * 62, 67, 61);
        foreach (RectangleF wall in RefugeWalls) { g.FillRectangle(wood, wall); g.DrawRectangle(edge, wall.X, wall.Y, wall.Width, wall.Height); }
        g.DrawLine(path, 302, 330, 412, 330); g.DrawLine(path, 550, 330, 662, 330);
        using var table = new SolidBrush(Color.FromArgb(119, 99, 73)); using var ledger = new SolidBrush(Color.FromArgb(47, 62, 58));
        g.FillEllipse(table, 427, 292, 106, 75); g.FillRectangle(ledger, 458, 309, 44, 27); g.DrawEllipse(edge, 427, 292, 106, 75);
        DrawTownPerson(g, AvenPosition, "AVEN", Color.FromArgb(88, 112, 102), Distance(player, AvenPosition) < InteractionRadius);
        DrawTownPerson(g, SeraPosition, "SERA", Color.FromArgb(144, 103, 78), Distance(player, SeraPosition) < InteractionRadius);
        DrawTownPerson(g, NoaPosition, "NOA", Color.FromArgb(102, 111, 145), Distance(player, NoaPosition) < InteractionRadius);
        using var gate = new Pen(finalEnding == 1 ? Color.FromArgb(170, 211, 145) : finalEnding == 2 ? Color.FromArgb(210, 174, 106) : Color.FromArgb(111, 191, 163), 5); g.DrawLine(gate, 75, 296, 75, 364);
        using var altarGlow = new SolidBrush(finalEnding == 0 ? Color.FromArgb(65, 89, 94) : Color.FromArgb(105, 157, 186, 165)); using var altarEdge = new Pen(finalEnding == 0 ? Color.FromArgb(105, 114, 112) : Color.FromArgb(212, 188, 129), 3);
        g.FillEllipse(altarGlow, VigilAltarPosition.X - 30, VigilAltarPosition.Y - 30, 60, 60); g.DrawEllipse(altarEdge, VigilAltarPosition.X - 30, VigilAltarPosition.Y - 30, 60, 60); g.DrawEllipse(altarEdge, VigilAltarPosition.X - 17, VigilAltarPosition.Y - 17, 34, 34);
        if (Distance(player, VigilAltarPosition) < InteractionRadius) DrawCentered(g, finalEnding == 0 ? "VIGÍLIA BLOQUEADA • CONCLUA A CAMPANHA" : $"E  VIGÍLIA ASCENDENTE • MELHOR ANDAR {vigilBestFloor}", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, (int)VigilAltarPosition.X);
        using var trail = new Pen(questStage >= 22 ? Color.FromArgb(179, 208, 151) : Color.FromArgb(70, 83, 77), 5); g.DrawLine(trail, 885, 296, 885, 364);
        if (questStage >= 27 && Distance(player, AvenPosition) < InteractionRadius) DrawCentered(g, "E  TÊMPERA • 75 OURO + 1 SAL VÍTREO", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.LightCyan, 490);
        if (player.X < 150) DrawCentered(g, "E  VOLTAR A VÉSPERA", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 146);
        if (questStage >= 22 && player.X > 790) DrawCentered(g, questStage >= 57 ? "E  OUVIR AS BADALADAS" : questStage >= 54 ? "E  OUVIR O ARQUIVO DAS MARÉS" : questStage >= 50 ? "E  DESCER AO SILÊNCIO" : questStage >= 47 ? "E  SEGUIR AS LINHAS" : questStage >= 41 ? "E  OUVIR O VIDRO" : questStage >= 30 ? "E  SEGUIR AS ÁGUAS" : "E  SEGUIR AS LANTERNAS", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
    }

    private void DrawLedgerTrailEnvironment(Graphics g, Brush ground, Pen tile)
    {
        using var deep = new SolidBrush(Color.FromArgb(21, 32, 37)); using var stone = new SolidBrush(Color.FromArgb(78, 79, 70));
        using var edge = new Pen(room == 19 ? Color.FromArgb(172, 179, 133) : Color.FromArgb(183, 145, 119), 2);
        using var water = new SolidBrush(Color.FromArgb(54, 56, 113, 118));
        g.FillRectangle(deep, 65, 108, 830, 445); g.FillRectangle(ground, 94, 132, 772, 397);
        for (int y = 0; y < 7; y++) for (int x = 0; x < 12; x++) g.DrawRectangle(tile, 70 + x * 68, 112 + y * 62, 67, 61);
        RectangleF[] walls = room == 19 ? RidgeWalls : LedgerWalls;
        foreach (RectangleF wall in walls) { g.FillRectangle(stone, wall); g.DrawRectangle(edge, wall.X, wall.Y, wall.Width, wall.Height); }
        if (room == 19) { g.FillRectangle(water, 330, 128, 74, 420); g.FillRectangle(water, 558, 128, 74, 420); for (int i = 0; i < 8; i++) { float x = 150 + i * 94; g.DrawLine(edge, x, 152, x, 187); g.FillEllipse(Brushes.PaleGoldenrod, x - 4, 148, 8, 8); } }
        else { using var basin = new SolidBrush(Color.FromArgb(60, 104, 131, 125)); using var ink = new Pen(Color.FromArgb(205, 181, 128, 109), 3); g.FillEllipse(basin, 350, 206, 260, 260); g.DrawEllipse(edge, 350, 206, 260, 260); for (int i = 0; i < 7; i++) g.DrawLine(ink, 382 + i * 27, 245, 410 + i * 20, 425); }
        using var gate = new Pen(enemies.Count == 0 && drops.Count == 0 ? Color.LightGreen : Color.FromArgb(76, 78, 85), 7); g.DrawLine(gate, 885, 290, 885, 370); g.DrawLine(gate, 75, 290, 75, 370);
        if (player.X < 150) DrawCentered(g, "E  VOLTAR", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 146);
        if (room == 19 && player.X > 790 && enemies.Count == 0 && drops.Count == 0) DrawCentered(g, "E  ENTRAR NA CISTERNA", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
        if (room == 20 && player.X > 790 && questStage >= 23 && enemies.Count == 0 && drops.Count == 0) DrawCentered(g, "E  VOLTAR AO REFÚGIO", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
    }

    private void DrawUpstreamEnvironment(Graphics g, Brush ground, Pen tile)
    {
        using var deep = new SolidBrush(Color.FromArgb(22, 35, 42)); using var water = new SolidBrush(Color.FromArgb(74, 58, 118, 127));
        using var stone = new SolidBrush(Color.FromArgb(78, 88, 82)); using var edge = new Pen(Color.FromArgb(170, 186, 166, 134), 2);
        g.FillRectangle(deep, 65, 108, 830, 445); g.FillRectangle(ground, 94, 132, 772, 397);
        for (int y = 0; y < 7; y++) for (int x = 0; x < 12; x++) g.DrawRectangle(tile, 70 + x * 68, 112 + y * 62, 67, 61);
        RectangleF[] walls = room == 21 ? SaltMarshWalls : TideBridgeWalls;
        foreach (RectangleF wall in walls) { g.FillRectangle(stone, wall); g.DrawRectangle(edge, wall.X, wall.Y, wall.Width, wall.Height); }
        if (room == 21)
        {
            g.FillPolygon(water, new[] { new PointF(92, 284), new PointF(336, 308), new PointF(570, 278), new PointF(868, 313), new PointF(868, 371), new PointF(572, 346), new PointF(332, 376), new PointF(92, 350) });
            using var current = new Pen(Color.FromArgb(150, 173, 208, 198), 2);
            for (int i = 0; i < 8; i++) { float y = 300 + i * 7; g.DrawLine(current, 112 + i * 23, y, 208 + i * 23, y + 7); }
            using var reed = new Pen(Color.FromArgb(172, 155, 173, 142), 2);
            for (int i = 0; i < 12; i++) { float x = 122 + i * 62; g.DrawLine(reed, x, 212 + i % 3 * 11, x + (i % 2 == 0 ? 5 : -4), 180 + i % 3 * 11); }
        }
        else
        {
            g.FillRectangle(water, 94, 132, 772, 137); g.FillRectangle(water, 94, 411, 772, 118);
            using var current = new Pen(Color.FromArgb(150, 173, 208, 198), 2);
            for (int i = 0; i < 9; i++) { float x = 108 + i * 88; g.DrawArc(current, x, 155 + i % 2 * 35, 60, 22, 0, 160); g.DrawArc(current, x + 28, 438 + i % 2 * 28, 60, 22, 180, 160); }
            using var deck = new SolidBrush(Color.FromArgb(113, 93, 66)); using var plank = new Pen(Color.FromArgb(192, 158, 111), 2);
            g.FillRectangle(deck, 94, 270, 772, 143); g.DrawRectangle(edge, 94, 270, 772, 143);
            for (int y = 285; y < 410; y += 20) g.DrawLine(plank, 98, y, 862, y);
            g.DrawEllipse(edge, 397, 292, 166, 100); g.DrawEllipse(plank, 418, 309, 124, 66);
        }
        using var gate = new Pen(enemies.Count == 0 && drops.Count == 0 ? Color.LightGreen : Color.FromArgb(73, 82, 79), 7);
        g.DrawLine(gate, 885, 290, 885, 370); g.DrawLine(gate, 75, 290, 75, 370);
        if (player.X < 150) DrawCentered(g, room == 21 ? "E  VOLTAR AO REFÚGIO" : "E  VOLTAR À VÁRZEA", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 146);
        if (room == 21 && player.X > 790 && enemies.Count == 0 && drops.Count == 0) DrawCentered(g, "E  CRUZAR A PONTE", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
        if (room == 22 && player.X > 790 && questStage >= 27 && enemies.Count == 0 && drops.Count == 0) DrawCentered(g, "E  CRUZAR O COTOVELO", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
        else if (room == 22 && player.X > 790 && questStage >= 26 && enemies.Count == 0 && drops.Count == 0) DrawCentered(g, "E  VOLTAR À MESA", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
    }

    private void DrawReedBankEnvironment(Graphics g, Brush ground, Pen tile)
    {
        using var deep = new SolidBrush(Color.FromArgb(21, 34, 35)); using var mud = new SolidBrush(Color.FromArgb(78, 83, 67));
        using var water = new SolidBrush(Color.FromArgb(68, 59, 114, 116)); using var reed = new Pen(Color.FromArgb(153, 166, 132), 3);
        using var edge = new Pen(Color.FromArgb(169, 158, 117), 2); using var trail = new Pen(Color.FromArgb(194, 165, 124, 91), 6);
        g.FillRectangle(deep, 65, 108, 830, 445); g.FillRectangle(ground, 94, 132, 772, 397);
        for (int y = 0; y < 7; y++) for (int x = 0; x < 12; x++) g.DrawRectangle(tile, 70 + x * 68, 112 + y * 62, 67, 61);
        if (room == 23)
        {
            g.FillPolygon(water, new[] { new PointF(90, 288), new PointF(315, 310), new PointF(525, 280), new PointF(870, 317), new PointF(870, 375), new PointF(525, 347), new PointF(315, 384), new PointF(90, 350) });
            g.DrawLine(trail, 95, 274, 330, 298); g.DrawLine(trail, 330, 298, 544, 268); g.DrawLine(trail, 545, 390, 332, 410); g.DrawLine(trail, 332, 410, 95, 383);
            for (int i = 0; i < 13; i++) { float x = 116 + i * 57; g.DrawLine(reed, x, 250 + i % 3 * 9, x + (i % 2 == 0 ? 5 : -4), 211 + i % 3 * 9); g.DrawLine(reed, x + 23, 420 - i % 3 * 8, x + 19, 455 - i % 3 * 8); }
        }
        else
        {
            g.FillRectangle(water, 94, 132, 772, 118); g.FillRectangle(water, 94, 411, 772, 118); g.FillRectangle(mud, 94, 251, 772, 160);
            g.DrawLine(trail, 98, 258, 862, 258); g.DrawLine(trail, 98, 404, 862, 404);
            for (int i = 0; i < 11; i++) { float x = 115 + i * 70; g.DrawLine(reed, x, 242, x + 3, 211 - i % 3 * 7); g.DrawLine(reed, x + 25, 422, x + 20, 455 + i % 3 * 7); }
            using var knot = new SolidBrush(Color.FromArgb(210, 195, 169, 122));
            for (int i = 0; i < 7; i++) { float x = 160 + i * 104; g.DrawLine(edge, x, 285, x + 15, 314); g.DrawLine(edge, x + 15, 314, x, 343); g.FillEllipse(knot, x + 8, 308, 12, 12); }
        }
        RectangleF[] walls = room == 23 ? ReedRunWalls : ReedYardWalls;
        foreach (RectangleF wall in walls) { g.FillEllipse(mud, wall); g.DrawEllipse(edge, wall); }
        using var gate = new Pen(enemies.Count == 0 && drops.Count == 0 ? Color.LightGreen : Color.FromArgb(73, 82, 79), 7);
        g.DrawLine(gate, 885, 290, 885, 370); g.DrawLine(gate, 75, 290, 75, 370);
        if (player.X < 150) DrawCentered(g, room == 23 ? "E  VOLTAR À PONTE" : "E  VOLTAR AO COTOVELO", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 146);
        if (room == 23 && player.X > 790 && enemies.Count == 0 && drops.Count == 0) DrawCentered(g, "E  ENTRAR NO PÁTIO", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
        if (room == 24 && player.X > 790 && questStage >= 29 && enemies.Count == 0 && drops.Count == 0) DrawCentered(g, "E  VOLTAR AO REFÚGIO", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
    }

    private void DrawCantoriaEnvironment(Graphics g, Brush ground, Pen tile)
    {
        using var deep = new SolidBrush(Color.FromArgb(24, 21, 31)); using var stone = new SolidBrush(Color.FromArgb(86, 77, 83));
        using var echo = new SolidBrush(Color.FromArgb(70, 167, 107, 197)); using var edge = new Pen(Color.FromArgb(177, 172, 145), 2);
        using var chord = new Pen(Color.FromArgb(150, 201, 164, 221), 3); using var path = new Pen(Color.FromArgb(188, 165, 142, 116), 5);
        g.FillRectangle(deep, 65, 108, 830, 445); g.FillRectangle(ground, 94, 132, 772, 397);
        for (int y = 0; y < 7; y++) for (int x = 0; x < 12; x++) g.DrawRectangle(tile, 70 + x * 68, 112 + y * 62, 67, 61);
        if (room == 29)
        {
            g.DrawLine(path, 95, 365, 356, 365); g.DrawLine(path, 602, 277, 865, 277);
            for (int i = 0; i < 9; i++) { float x = 152 + i * 79, sway = MathF.Sin(windupPulse * 1.7f + i) * 12; g.DrawLine(chord, x, 200 + sway, x, 298 + sway); g.DrawLine(chord, x + 24, 385 - sway, x + 24, 478 - sway); }
            using var stoneEdge = new Pen(Color.FromArgb(198, 192, 162), 2);
            for (int i = 0; i < 6; i++) { float x = 164 + i * 116, y = 295 + MathF.Sin(windupPulse + i) * 4; g.DrawEllipse(stoneEdge, x - 11, y - 11, 22, 22); }
        }
        else
        {
            float pulse = 1 + MathF.Sin(windupPulse * 2.2f) * .04f;
            g.FillEllipse(echo, 480 - 150 * pulse, 330 - 150 * pulse, 300 * pulse, 300 * pulse);
            g.DrawEllipse(chord, 330, 180, 300, 300); g.DrawEllipse(edge, 375, 225, 210, 210);
            for (int i = 0; i < 3; i++) { float y = 228 + i * 102; g.DrawLine(chord, 385, y, 575, y); g.DrawLine(chord, 385, y + 12, 575, y + 12); }
            DrawCentered(g, "TRÊS MARCAS • ATAQUE EM LINHA", 494, new Font("Segoe UI", 9, FontStyle.Bold), Color.FromArgb(222, 202, 226));
        }
        RectangleF[] walls = room == 29 ? CantoriaWalls : RiftChamberWalls;
        foreach (RectangleF wall in walls) { g.FillRectangle(stone, wall); g.DrawRectangle(edge, wall.X, wall.Y, wall.Width, wall.Height); }
        using var gate = new Pen(enemies.Count == 0 && drops.Count == 0 ? Color.LightGreen : Color.FromArgb(73, 82, 79), 7);
        g.DrawLine(gate, 885, 290, 885, 370); g.DrawLine(gate, 75, 290, 75, 370);
        if (player.X < 150) DrawCentered(g, room == 29 ? "E  VOLTAR À ESCARPA" : "E  VOLTAR À CANTARIA", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 146);
        if (room == 29 && player.X > 790 && enemies.Count == 0 && drops.Count == 0) DrawCentered(g, "E  ENTRAR NA CÂMARA", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
        if (room == 30 && player.X > 790 && questStage >= 39) DrawCentered(g, "E  VOLTAR A AVEN", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
    }

    private void DrawGlassEnvironment(Graphics g, Brush ground, Pen tile)
    {
        using var deep = new SolidBrush(Color.FromArgb(18, 26, 34));
        using var glass = new SolidBrush(Color.FromArgb(70, 103, 119));
        using var edge = new Pen(Color.FromArgb(165, 178, 184), 2);
        using var seam = new Pen(Color.FromArgb(130, 174, 207, 216), 2);
        g.FillRectangle(deep, 65, 108, 830, 445); g.FillRectangle(ground, 94, 132, 772, 397);
        for (int y = 0; y < 7; y++) for (int x = 0; x < 12; x++) g.DrawRectangle(tile, 70 + x * 68, 112 + y * 62, 67, 61);
        RectangleF[] walls = room == 31 ? GlassGorgeWalls : MirrorSourceWalls;
        foreach (RectangleF wall in walls) { g.FillRectangle(glass, wall); g.DrawRectangle(edge, wall.X, wall.Y, wall.Width, wall.Height); }
        for (int i = 0; i < 7; i++) { float x = 150 + i * 105; float wave = MathF.Sin(windupPulse * 1.1f + i) * 8; g.DrawLine(seam, x, 175 + wave, x + 24, 224 + wave); g.DrawLine(seam, x + 24, 224 + wave, x + 6, 277 + wave); }
        if (room == 32) { using var spring = new Pen(Color.FromArgb(175, 154, 224, 225), 4); g.DrawEllipse(spring, 365, 213, 230, 230); g.DrawEllipse(seam, 402, 250, 156, 156); DrawCentered(g, "PASSOS SEM NOME • SEM DESTINO", 481, new Font("Segoe UI", 9, FontStyle.Bold), Color.LightCyan); }
        using var gate = new Pen(enemies.Count == 0 && drops.Count == 0 ? Color.LightGreen : Color.FromArgb(73, 82, 79), 7);
        g.DrawLine(gate, 885, 290, 885, 370); g.DrawLine(gate, 75, 290, 75, 370);
        if (player.X < 150) DrawCentered(g, room == 31 ? "E  VOLTAR AO REFÚGIO" : "E  VOLTAR À GARGANTA", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 146);
        if (room == 31 && player.X > 790 && enemies.Count == 0 && drops.Count == 0) DrawCentered(g, "E  SEGUIR ATÉ A FONTE", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
        if (room == 32 && player.X > 790 && questStage >= 42) DrawCentered(g, "E  VOLTAR A AVEN", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
    }

    private void DrawRouteEnvironment(Graphics g, Brush ground, Pen tile)
    {
        using var deep = new SolidBrush(Color.FromArgb(17, 26, 31)); using var stone = new SolidBrush(Color.FromArgb(74, 82, 77));
        using var edge = new Pen(Color.FromArgb(181, 179, 157, 129), 2); using var route = new Pen(Color.FromArgb(155, 195, 176, 135), 3);
        using var pale = new Pen(Color.FromArgb(120, 155, 195, 186), 2); using var ink = new SolidBrush(Color.FromArgb(88, 44, 55, 61));
        g.FillRectangle(deep, 65, 108, 830, 445); g.FillRectangle(ground, 94, 132, 772, 397);
        for (int y = 0; y < 7; y++) for (int x = 0; x < 12; x++) g.DrawRectangle(tile, 70 + x * 68, 112 + y * 62, 67, 61);
        RectangleF[] walls = room == 35 ? OpenRouteWalls : UnwrittenHallWalls;
        foreach (RectangleF wall in walls) { g.FillRectangle(stone, wall); g.DrawRectangle(edge, wall.X, wall.Y, wall.Width, wall.Height); }
        if (room == 35)
        {
            g.DrawLine(route, 96, 330, 250, 330); g.DrawLine(route, 250, 330, 340, 226); g.DrawLine(route, 250, 330, 340, 440);
            g.DrawLine(pale, 340, 226, 496, 226); g.DrawLine(pale, 340, 440, 496, 440); g.DrawLine(route, 496, 226, 624, 330); g.DrawLine(route, 496, 440, 624, 330); g.DrawLine(pale, 624, 330, 862, 330);
            g.DrawLine(edge, 340, 226, 340, 257); g.DrawLine(edge, 340, 440, 340, 409); g.DrawLine(edge, 624, 330, 624, 298); g.DrawLine(edge, 624, 330, 624, 362);
            DrawCentered(g, "MUITOS CAMINHOS • NENHUM NOME", 479, new Font("Segoe UI", 9, FontStyle.Bold), Color.FromArgb(212, 205, 179));
        }
        else
        {
            g.FillEllipse(ink, 333, 183, 294, 294); g.DrawEllipse(route, 333, 183, 294, 294); g.DrawEllipse(pale, 370, 220, 220, 220); g.DrawEllipse(edge, 414, 264, 132, 132);
            for (int i = 0; i < 8; i++) { float angle = i * MathF.PI / 4 + windupPulse * 0.08f; float x1 = 480 + MathF.Cos(angle) * 80, y1 = 330 + MathF.Sin(angle) * 80; float x2 = 480 + MathF.Cos(angle + 0.2f) * 142, y2 = 330 + MathF.Sin(angle + 0.2f) * 142; g.DrawLine(pale, x1, y1, x2, y2); }
            DrawCentered(g, "A ROTA NÃO É UMA ORDEM", 487, new Font("Segoe UI", 9, FontStyle.Bold), Color.LightCyan);
        }
        using var gate = new Pen(enemies.Count == 0 && drops.Count == 0 ? Color.LightGreen : Color.FromArgb(73, 82, 79), 7);
        g.DrawLine(gate, 885, 290, 885, 370); g.DrawLine(gate, 75, 290, 75, 370);
        if (player.X < 150) DrawCentered(g, room == 35 ? "E  VOLTAR AO OBSERVATÓRIO" : "E  VOLTAR À TRILHA", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 146);
        if (room == 35 && player.X > 790 && enemies.Count == 0 && drops.Count == 0) DrawCentered(g, "E  ENTRAR NA GALERIA", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
        if (room == 36 && player.X > 790 && questStage >= 48 && enemies.Count == 0 && drops.Count == 0) DrawCentered(g, "E  LEVAR O TRAÇADO A AVEN", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
    }

    private void DrawFinalEnvironment(Graphics g, Brush ground, Pen tile)
    {
        using var deep = new SolidBrush(Color.FromArgb(12, 17, 24)); using var stone = new SolidBrush(Color.FromArgb(54, 63, 67));
        using var edge = new Pen(Color.FromArgb(162, 132, 157, 165), 2); using var fissure = new Pen(Color.FromArgb(170, 129, 170, 180), 3);
        Color ringColor = vigilActive ? VigilAspectColor(RunVigilAspect(vigilFloor)) : Color.FromArgb(173, 193, 194);
        using var hush = new Pen(Color.FromArgb(105, ringColor), 2); using var warning = new Pen(Color.FromArgb(220, 202, 104, 93), 3);
        g.FillRectangle(deep, 65, 108, 830, 445); g.FillRectangle(ground, 94, 132, 772, 397);
        for (int y = 0; y < 7; y++) for (int x = 0; x < 12; x++) g.DrawRectangle(tile, 70 + x * 68, 112 + y * 62, 67, 61);
        RectangleF[] walls = room == 37 ? SilenceThresholdWalls : SilenceChamberWalls;
        foreach (RectangleF wall in walls) { g.FillRectangle(stone, wall); g.DrawRectangle(edge, wall.X, wall.Y, wall.Width, wall.Height); }
        if (room == 37)
        {
            for (int i = 0; i < 7; i++) { float y = 178 + i * 47; g.DrawLine(fissure, 104, y, 250 + i * 17, y + ((i % 2) == 0 ? 30 : -30)); }
            DrawCentered(g, "AS VOZES NÃO PRECISAM RESPONDER", 492, new Font("Segoe UI", 9, FontStyle.Bold), Color.FromArgb(202, 207, 196));
        }
        else
        {
            float pulse = 1 + 0.04f * MathF.Sin(windupPulse * 3);
            g.DrawEllipse(hush, 480 - 180 * pulse, 330 - 180 * pulse, 360 * pulse, 360 * pulse);
            g.DrawEllipse(fissure, 365, 215, 230, 230); g.DrawEllipse(edge, 420, 270, 120, 120);
            for (int i = 0; i < 8; i++) { float angle = i * MathF.PI / 4 + windupPulse * 0.12f; float x = 480 + MathF.Cos(angle) * 168, y = 330 + MathF.Sin(angle) * 168; g.DrawLine(fissure, x, y, x - MathF.Cos(angle) * 34, y - MathF.Sin(angle) * 34); }
            if (vigilActive)
            {
                using var altar = new Pen(Color.FromArgb(180, 202, 170, 112), 3);
                g.DrawEllipse(altar, VigilArenaAltarPosition.X - 36, VigilArenaAltarPosition.Y - 36, 72, 72);
                g.DrawEllipse(altar, VigilArenaAltarPosition.X - 24, VigilArenaAltarPosition.Y - 24, 48, 48);
                if (RunVigilAspect(vigilFloor) == VigilAspect.Chorus)
                {
                    using var driftingEcho = new Pen(Color.FromArgb(135, 194, 133, 222), 3);
                    for (int i = 0; i < 3; i++) { float angle = windupPulse * 0.42f + i * MathF.PI * 2 / 3; float x = 480 + MathF.Cos(angle) * 222, y = 330 + MathF.Sin(angle) * 148; g.DrawEllipse(driftingEcho, x - 9, y - 9, 18, 18); }
                }
                if (RunVigilAspect(vigilFloor) == VigilAspect.Counterflow)
                {
                    bool pulling = VigilTidePulling(vigilTideTime);
                    using var current = new Pen(Color.FromArgb(185, VigilAspectColor(RunVigilAspect(vigilFloor))), 3) { EndCap = LineCap.Triangle };
                    for (int i = 0; i < 8; i++)
                    {
                        float angle = i * MathF.PI / 4 + windupPulse * 0.04f;
                        PointF inner = new(480 + MathF.Cos(angle) * 112, 330 + MathF.Sin(angle) * 94);
                        PointF outer = new(480 + MathF.Cos(angle) * 184, 330 + MathF.Sin(angle) * 154);
                        g.DrawLine(current, pulling ? outer : inner, pulling ? inner : outer);
                    }
                    float changeIn = 6f - vigilTideTime % 6f;
                    string flow = pulling ? "PUXANDO PARA O CENTRO" : "REPELINDO PARA A BORDA";
                    Color flowColor = changeIn <= 1f && ((int)(windupPulse * 5) % 2 == 0) ? Color.PaleGoldenrod : VigilAspectColor(RunVigilAspect(vigilFloor));
                    DrawCentered(g, $"CORRENTE: {flow} • MUDA EM {changeIn:0.0} S", 530, new Font("Segoe UI", 9, FontStyle.Bold), flowColor);
                }
                DrawCentered(g, enemies.Count == 0 && drops.Count == 0 ? "E  SUBIR OU ENCERRAR A VIGÍLIA" : $"ECO DA VIGÍLIA • ANDAR {vigilFloor}", 494, new Font("Segoe UI", 9, FontStyle.Bold), Color.FromArgb(222, 202, 226));
                DrawCentered(g, $"{VigilAspectTitle(RunVigilAspect(vigilFloor))} • {VigilAspectRule(RunVigilAspect(vigilFloor)).ToUpperInvariant()}", 511, new Font("Segoe UI", 9, FontStyle.Bold), VigilAspectColor(RunVigilAspect(vigilFloor)));
            }
            if (!vigilActive && enemies.FirstOrDefault(e => e.Kind == EnemyKind.DeepSilence) is Enemy boss && boss.Windup > 0)
            {
                if (boss.BossPattern == 0) { g.DrawEllipse(warning, boss.Pos.X - 90, boss.Pos.Y - 90, 180, 180); }
                else if (boss.BossPattern == 1) { g.DrawEllipse(warning, boss.Target.X - 100, boss.Target.Y - 100, 200, 200); }
                else g.DrawLine(warning, boss.Pos, boss.Target);
            }
            if (!vigilActive) DrawCentered(g, "O SILÊNCIO DEVOLVE O QUE O LUME PRENDEU", 493, new Font("Segoe UI", 9, FontStyle.Bold), Color.LightCyan);
        }
        using var gate = new Pen(enemies.Count == 0 && drops.Count == 0 ? Color.LightGreen : Color.FromArgb(73, 82, 79), 7);
        g.DrawLine(gate, 885, 290, 885, 370); g.DrawLine(gate, 75, 290, 75, 370);
        if (player.X < 150) DrawCentered(g, room == 37 ? "E  VOLTAR AO REFÚGIO" : "E  VOLTAR À SOLEIRA", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 146);
        if (room == 37 && player.X > 790 && enemies.Count == 0 && drops.Count == 0) DrawCentered(g, "E  ENTRAR NA CÂMARA", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
        if (room == 38 && player.X > 790 && silenceMemory) DrawCentered(g, "E  VOLTAR A AVEN", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
    }

    private void DrawBorrowedArchiveEnvironment(Graphics g, Brush ground, Pen tile)
    {
        using var deep = new SolidBrush(Color.FromArgb(9, 23, 27)); using var stone = new SolidBrush(Color.FromArgb(39, 66, 68));
        using var edge = new Pen(Color.FromArgb(170, 117, 183, 176), 2); using var current = new Pen(Color.FromArgb(92, 119, 203, 188), 3);
        g.FillRectangle(deep, 65, 108, 830, 445); g.FillRectangle(ground, 94, 132, 772, 397);
        for (int y = 0; y < 7; y++) for (int x = 0; x < 12; x++) g.DrawRectangle(tile, 70 + x * 68, 112 + y * 62, 67, 61);
        RectangleF[] walls = room == 39 ? BorrowedDockWalls : BorrowedArchiveWalls;
        foreach (RectangleF wall in walls) { g.FillRectangle(stone, wall); g.DrawRectangle(edge, wall.X, wall.Y, wall.Width, wall.Height); }
        for (int i = 0; i < 5; i++) { float y = 190 + i * 68; g.DrawArc(current, 250, y - 24, 460, 48, 190, 160); }
        g.DrawEllipse(edge, 414, 264, 132, 132); g.DrawEllipse(current, 390, 240, 180, 180);
        using var gate = new Pen(enemies.Count == 0 && drops.Count == 0 ? Color.LightGreen : Color.FromArgb(73, 82, 79), 7);
        g.DrawLine(gate, 885, 290, 885, 370); g.DrawLine(gate, 75, 290, 75, 370);
        if (room == 39 && player.X < 150) DrawCentered(g, "E  VOLTAR AO REFÚGIO", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 146);
        if (room == 39 && player.X > 790 && enemies.Count == 0 && drops.Count == 0) DrawCentered(g, "E  ENTRAR NO ARQUIVO", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
        if (room == 40 && player.X < 150) DrawCentered(g, "E  VOLTAR AO CAIS", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 146);
        if (room == 40 && player.X > 790 && questStage >= 55) DrawCentered(g, "E  VOLTAR A AVEN", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
    }

    private void DrawUnsentChimeEnvironment(Graphics g, Brush ground, Pen tile)
    {
        using var deep = new SolidBrush(Color.FromArgb(20, 18, 23)); using var stone = new SolidBrush(Color.FromArgb(67, 57, 49));
        using var bronze = new Pen(Color.FromArgb(191, 157, 103), 3); using var pale = new Pen(Color.FromArgb(126, 211, 196, 142), 2);
        g.FillRectangle(deep, 65, 108, 830, 445); g.FillRectangle(ground, 94, 132, 772, 397);
        for (int y = 0; y < 7; y++) for (int x = 0; x < 12; x++) g.DrawRectangle(tile, 70 + x * 68, 112 + y * 62, 67, 61);
        RectangleF[] walls = room == 41 ? UnsentChamberWalls : OffbeatWellWalls;
        foreach (RectangleF wall in walls) { g.FillRectangle(stone, wall); g.DrawRectangle(bronze, wall.X, wall.Y, wall.Width, wall.Height); }
        if (room == 41)
        {
            for (int i = 0; i < 7; i++) { float x = 180 + i * 95, length = 42 + (i % 3) * 18; g.DrawLine(bronze, x, 142, x, 142 + length); g.DrawArc(bronze, x - 15, 142 + length - 8, 30, 26, 0, 180); g.DrawLine(pale, x - 8, 144 + length, x + 8, 144 + length); }
            g.DrawEllipse(pale, 387, 263, 184, 118); g.DrawEllipse(bronze, 417, 282, 124, 80);
        }
        else
        {
            g.FillEllipse(stone, 316, 166, 328, 328); g.DrawEllipse(bronze, 316, 166, 328, 328);
            for (int i = 0; i < 5; i++) { int inset = i * 22; g.DrawEllipse(i % 2 == 0 ? bronze : pale, 344 + inset, 194 + inset, 272 - inset * 2, 272 - inset * 2); }
            g.DrawLine(pale, 480, 205, 480, 455); g.DrawLine(pale, 355, 330, 605, 330);
        }
        using var gate = new Pen(enemies.Count == 0 && drops.Count == 0 ? Color.LightGreen : Color.FromArgb(73, 82, 79), 7);
        g.DrawLine(gate, 885, 290, 885, 370); g.DrawLine(gate, 75, 290, 75, 370);
        if (player.X < 150) DrawCentered(g, room == 41 ? "E  VOLTAR AO REFÚGIO" : "E  SUBIR À CÂMARA", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 146);
        if (room == 41 && player.X > 790 && enemies.Count == 0 && drops.Count == 0) DrawCentered(g, "E  DESCER AO POÇO", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
        if (room == 42 && player.X > 790 && questStage >= 58) DrawCentered(g, "E  VOLTAR A AVEN", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
    }

    private void DrawCounterflowEnvironment(Graphics g, Brush ground, Pen tile)
    {
        using var deep = new SolidBrush(Color.FromArgb(16, 25, 31)); using var stone = new SolidBrush(Color.FromArgb(79, 77, 71));
        using var water = new SolidBrush(Color.FromArgb(94, 43, 108, 121)); using var edge = new Pen(Color.FromArgb(174, 166, 136), 2);
        using var current = new Pen(Color.FromArgb(205, 161, 211, 207), 3); using var path = new Pen(Color.FromArgb(171, 157, 122), 5);
        g.FillRectangle(deep, 65, 108, 830, 445); g.FillRectangle(ground, 94, 132, 772, 397);
        for (int y = 0; y < 7; y++) for (int x = 0; x < 12; x++) g.DrawRectangle(tile, 70 + x * 68, 112 + y * 62, 67, 61);
        if (room == 27)
        {
            g.FillPolygon(water, new[] { new PointF(94, 365), new PointF(310, 308), new PointF(528, 350), new PointF(868, 266), new PointF(868, 319), new PointF(528, 405), new PointF(310, 359), new PointF(94, 416) });
            g.DrawLine(path, 95, 245, 352, 309); g.DrawLine(path, 605, 366, 865, 425);
            for (int i = 0; i < 7; i++) { float x = 152 + i * 94, y = 350 - i * 12; g.DrawLine(current, x, y, x + 28, y - 8); g.DrawLine(current, x + 28, y - 8, x + 15, y - 18); g.DrawLine(current, x + 28, y - 8, x + 12, y); }
        }
        else
        {
            g.FillEllipse(water, 304, 154, 352, 352); g.FillEllipse(stone, 402, 252, 156, 156); g.DrawEllipse(edge, 304, 154, 352, 352);
            float direction = MathF.Sin(windupPulse * 1.35f) >= 0 ? -1 : 1;
            for (int i = 0; i < 12; i++) { float angle = i * MathF.PI / 6 + windupPulse * 0.14f, dx = MathF.Cos(angle) * direction, dy = MathF.Sin(angle) * direction; float x = 480 + MathF.Cos(angle) * 188, y = 330 + MathF.Sin(angle) * 188; g.DrawLine(current, x, y, x + dx * 31, y + dy * 31); g.DrawLine(current, x + dx * 31, y + dy * 31, x + dx * 17 - dy * 8, y + dy * 17 + dx * 8); g.DrawLine(current, x + dx * 31, y + dy * 31, x + dx * 17 + dy * 8, y + dy * 17 - dx * 8); }
            DrawCentered(g, direction < 0 ? "PUXO" : "REPULSO", 495, new Font("Segoe UI", 9, FontStyle.Bold), Color.FromArgb(204, 205, 188));
        }
        RectangleF[] walls = room == 27 ? CounterSlopeWalls : CounterHeartWalls;
        foreach (RectangleF wall in walls) { g.FillRectangle(stone, wall); g.DrawRectangle(edge, wall.X, wall.Y, wall.Width, wall.Height); }
        using var gate = new Pen(enemies.Count == 0 && drops.Count == 0 ? Color.LightGreen : Color.FromArgb(73, 82, 79), 7);
        g.DrawLine(gate, 885, 290, 885, 370); g.DrawLine(gate, 75, 290, 75, 370);
        if (player.X < 150) DrawCentered(g, room == 27 ? "E  VOLTAR AO REFÚGIO" : "E  VOLTAR À ESCARPA", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 146);
        if (room == 27 && player.X > 790 && enemies.Count == 0 && drops.Count == 0) DrawCentered(g, "E  DESCER AO CORAÇÃO", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
        if (room == 28 && player.X > 790 && questStage >= 35 && enemies.Count == 0 && drops.Count == 0) DrawCentered(g, "E  VOLTAR A AVEN", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
    }

    private void DrawFloodMillEnvironment(Graphics g, Brush ground, Pen tile)
    {
        using var deep = new SolidBrush(Color.FromArgb(18, 30, 34)); using var mud = new SolidBrush(Color.FromArgb(76, 79, 68));
        using var water = new SolidBrush(Color.FromArgb(88, 43, 102, 111)); using var edge = new Pen(Color.FromArgb(163, 167, 137), 2);
        using var current = new Pen(Color.FromArgb(180, 154, 218, 208), 3); using var path = new Pen(Color.FromArgb(177, 156, 119, 88), 6);
        g.FillRectangle(deep, 65, 108, 830, 445); g.FillRectangle(ground, 94, 132, 772, 397);
        for (int y = 0; y < 7; y++) for (int x = 0; x < 12; x++) g.DrawRectangle(tile, 70 + x * 68, 112 + y * 62, 67, 61);
        if (room == 25)
        {
            g.FillRectangle(water, 300, 286, 372, 98); g.DrawLine(path, 95, 263, 285, 263); g.DrawLine(path, 685, 405, 865, 405);
            for (int x = 344; x <= 604; x += 86) { g.DrawLine(current, x, 322, x + 36, 322); g.DrawLine(current, x + 36, 322, x + 23, 311); g.DrawLine(current, x + 36, 322, x + 23, 333); }
            using var wheel = new Pen(Color.FromArgb(185, 147, 123, 81), 5); g.DrawEllipse(wheel, 112, 163, 108, 108); g.DrawEllipse(wheel, 740, 392, 108, 108);
            g.DrawRectangle(edge, 111, 162, 110, 110); g.DrawRectangle(edge, 739, 391, 110, 110);
        }
        else
        {
            g.FillEllipse(water, 316, 166, 328, 328); g.FillEllipse(mud, 393, 243, 174, 174); g.DrawEllipse(edge, 316, 166, 328, 328);
            for (int i = 0; i < 8; i++) { float angle = i * MathF.PI / 4; float dx = MathF.Cos(angle), dy = MathF.Sin(angle), x = 480 + dx * 194, y = 330 + dy * 194; g.DrawLine(current, x, y, x - dx * 27, y - dy * 27); }
            using var ring = new Pen(Color.FromArgb(170, 167, 145, 111), 4); g.DrawEllipse(ring, 408, 258, 144, 144);
        }
        RectangleF[] walls = room == 25 ? FloodMillWalls : SiltChamberWalls;
        foreach (RectangleF wall in walls) { g.FillRectangle(mud, wall); g.DrawRectangle(edge, wall.X, wall.Y, wall.Width, wall.Height); }
        using var gate = new Pen(enemies.Count == 0 && drops.Count == 0 ? Color.LightGreen : Color.FromArgb(73, 82, 79), 7);
        g.DrawLine(gate, 885, 290, 885, 370); g.DrawLine(gate, 75, 290, 75, 370);
        if (player.X < 150) DrawCentered(g, room == 25 ? "E  VOLTAR AO REFÚGIO" : "E  VOLTAR AOS MOINHOS", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 146);
        if (room == 25 && player.X > 790 && enemies.Count == 0 && drops.Count == 0) DrawCentered(g, "E  DESCER À CÂMARA", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
        if (room == 26 && player.X > 790 && questStage >= 32 && enemies.Count == 0 && drops.Count == 0) DrawCentered(g, "E  VOLTAR A AVEN", 382, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 820);
    }

    private static void DrawTownHouse(Graphics g, RectangleF area, string sign)
    {
        using var stone = new SolidBrush(Color.FromArgb(74, 69, 60)); using var roof = new SolidBrush(Color.FromArgb(67, 48, 43));
        using var edge = new Pen(Color.FromArgb(148, 125, 86), 3); using var door = new SolidBrush(Color.FromArgb(39, 31, 27));
        g.FillRectangle(stone, area); g.FillRectangle(roof, area.X, area.Y, area.Width, 22); g.DrawRectangle(edge, area.X, area.Y, area.Width, area.Height);
        g.FillRectangle(door, area.X + area.Width / 2 - 16, area.Y + area.Height - 46, 32, 46);
        using var font = new Font("Segoe UI", 8, FontStyle.Bold); using var brush = new SolidBrush(Color.FromArgb(220, 197, 151));
        SizeF size = g.MeasureString(sign, font); g.DrawString(sign, font, brush, area.X + (area.Width - size.Width) / 2, area.Y + 31);
    }

    private static void DrawTownPerson(Graphics g, PointF pos, string name, Color robe, bool nearby)
    {
        using var body = new SolidBrush(robe); using var face = new SolidBrush(Color.Bisque);
        using var edge = new Pen(nearby ? Color.Gold : Color.FromArgb(34, 25, 20), nearby ? 3 : 2);
        g.FillEllipse(body, pos.X - 16, pos.Y - 7, 32, 35); g.DrawEllipse(edge, pos.X - 16, pos.Y - 7, 32, 35);
        g.FillEllipse(face, pos.X - 9, pos.Y - 20, 18, 18); g.DrawEllipse(edge, pos.X - 9, pos.Y - 20, 18, 18);
        DrawCentered(g, name, pos.Y - 39, new Font("Segoe UI", 9, FontStyle.Bold), Color.FromArgb(240, 220, 176), pos.X);
        if (nearby) DrawCentered(g, "E  FALAR", pos.Y + 34, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, pos.X);
    }

    private void DrawEnemy(Graphics g, Enemy enemy)
    {
        float r = enemy.Radius; Color color = enemy.Kind == EnemyKind.LastTuner ? Color.FromArgb(189, 153, 98) : enemy.Kind == EnemyKind.BlindScribe ? Color.FromArgb(89, 128, 135) : enemy.Kind == EnemyKind.RiverFerryman ? Color.FromArgb(69, 120, 124) : enemy.Kind == EnemyKind.AshWarden ? Color.FromArgb(169, 91, 53) : enemy.Kind == EnemyKind.HollowName ? Color.FromArgb(120, 165, 155) : enemy.Kind == EnemyKind.NameWeaver ? Color.FromArgb(139, 108, 162) : enemy.Kind == EnemyKind.FirstKeeper ? Color.FromArgb(202, 173, 112) : enemy.Kind == EnemyKind.OathCartographer ? Color.FromArgb(96, 148, 144) : enemy.Kind == EnemyKind.NameBroker ? Color.FromArgb(126, 82, 132) : enemy.Kind == EnemyKind.ReedKeeper ? Color.FromArgb(158, 149, 98) : enemy.Kind == EnemyKind.FlowKeeper ? Color.FromArgb(71, 148, 164) : enemy.Kind == EnemyKind.TideWitness ? Color.FromArgb(63, 157, 168) : enemy.Kind == EnemyKind.GlassHerald ? Color.FromArgb(124, 204, 218) : enemy.Kind == EnemyKind.LanternRegent ? Color.FromArgb(194, 195, 144) : enemy.Kind == EnemyKind.RouteScribe ? Color.FromArgb(159, 182, 211) : enemy.Kind == EnemyKind.DeepSilence ? Color.FromArgb(119, 183, 190) : enemy.Kind == EnemyKind.SaltEcho ? Color.LightSteelBlue : enemy.Kind == EnemyKind.PaperWraith ? Color.FromArgb(201, 190, 161) : enemy.Kind == EnemyKind.MudCantor ? Color.FromArgb(165, 135, 104) : enemy.Boss ? Color.FromArgb(145, 87, 77) : enemy.Kind switch { EnemyKind.Elite => enemy.EliteAffix switch { EliteAffix.Armored => Color.SlateGray, EliteAffix.Swift => Color.Goldenrod, EliteAffix.Bloodbound => Color.MediumVioletRed, EliteAffix.Conductor => Color.MediumAquamarine, EliteAffix.Summoner => Color.MediumOrchid, _ => Color.IndianRed }, EnemyKind.Archer => Color.FromArgb(128, 151, 106), EnemyKind.Mage => Color.MediumPurple, EnemyKind.InkEcho => Color.FromArgb(113, 98, 143), EnemyKind.DrownedMemory => Color.FromArgb(95, 145, 142), EnemyKind.EmberWisp => Color.FromArgb(212, 126, 61), EnemyKind.NameMoth => Color.FromArgb(126, 170, 156), _ => Color.FromArgb(183, 181, 166) };
        if (enemy.Kind == EnemyKind.CooldownLeech) color = Color.MediumOrchid;
        if (enemy.Kind == EnemyKind.SlagWeaver) color = Color.FromArgb(189, 123, 75);
        if (enemy.Stagger > 0) { using var staggerPen = new Pen(Color.FromArgb(200, 125, 211, 203), 2); float pulse = 1 + (0.95f - enemy.Stagger) * 7; g.DrawEllipse(staggerPen, enemy.Pos.X - r - pulse, enemy.Pos.Y - r - pulse, (r + pulse) * 2, (r + pulse) * 2); }
        if (enemy.Windup > 0 && enemy.Boss) DrawBossWarning(g, enemy);
        else if (enemy.Windup > 0)
        {
            PointF center = NonBossAttackCenter(enemy); float radius = NonBossAttackRadius(enemy);
            bool spearboundElite = enemy.Kind == EnemyKind.Elite && enemy.EliteAffix == EliteAffix.Spearbound;
            (Color fillColor, Color edgeColor) = spearboundElite && !highContrastTelegraphs
                ? (Color.FromArgb(42, 76, 211, 245), Color.FromArgb(235, 76, 211, 245))
                : AttackWarningPalette(highContrastTelegraphs);
            if (enemy.Kind == EnemyKind.CooldownLeech && !highContrastTelegraphs) { fillColor = Color.FromArgb(52, 185, 104, 220); edgeColor = Color.FromArgb(238, 205, 139, 245); }
            if (enemy.Kind == EnemyKind.SlagWeaver && !highContrastTelegraphs) { fillColor = Color.FromArgb(45, 209, 126, 65); edgeColor = Color.FromArgb(238, 245, 164, 101); }
            float urgency = Math.Clamp(1f - enemy.Windup / Math.Max(0.01f, enemy.WindupMax), 0f, 1f);
            float pulse = (0.5f + 0.5f * MathF.Sin(windupPulse * (7f + urgency * 11f))) * urgency;
            fillColor = Color.FromArgb(Math.Clamp(fillColor.A + (int)(urgency * 24), 0, 255), fillColor);
            edgeColor = Color.FromArgb(Math.Clamp(edgeColor.A + (int)(urgency * 18 + pulse * 35), 0, 255), edgeColor);
            using var p = new Pen(edgeColor, highContrastTelegraphs ? 4 + urgency + pulse : 2.5f + urgency * 2 + pulse * 1.5f); using var b = new SolidBrush(fillColor);
            if (RangedImpactRadius(enemy.Kind) > 0 || enemy.Kind == EnemyKind.Elite && enemy.EliteAffix == EliteAffix.Spearbound)
            {
                if (highContrastTelegraphs) { using var shadow = new Pen(Color.FromArgb(230, 12, 13, 15), 7); g.DrawLine(shadow, enemy.Pos, enemy.Target); }
                g.DrawLine(p, enemy.Pos, enemy.Target);
            }
            g.FillEllipse(b, center.X - radius, center.Y - radius, radius * 2, radius * 2);
            if (highContrastTelegraphs) { using var shadow = new Pen(Color.FromArgb(240, 12, 13, 15), 7); g.DrawEllipse(shadow, center.X - radius, center.Y - radius, radius * 2, radius * 2); }
            g.DrawEllipse(p, center.X - radius, center.Y - radius, radius * 2, radius * 2);
            if (enemy.Kind == EnemyKind.CooldownLeech) DrawCentered(g, "RECARGAS", center.Y - radius - 14, new Font("Segoe UI", 8, FontStyle.Bold), highContrastTelegraphs ? Color.White : Color.Plum, center.X);
            if (enemy.Kind == EnemyKind.SlagWeaver) DrawCentered(g, "SAL VIVO", center.Y - radius - 14, new Font("Segoe UI", 8, FontStyle.Bold), highContrastTelegraphs ? Color.White : Color.FromArgb(255, 191, 126), center.X);
        }
        if (enemy.WoundTime > 0) { int alpha = (int)(110 + 100 * enemy.WoundTime / 3f); using var woundRing = new Pen(Color.FromArgb(alpha, 237, 111, 105), 2.5f); g.DrawEllipse(woundRing, enemy.Pos.X - r - 5, enemy.Pos.Y - r - 5, (r + 5) * 2, (r + 5) * 2); }
        if (enemy.NameOverride == "VIGIA DAS COMPORTAS")
        {
            using var waterRing = new Pen(Color.FromArgb(190, 97, 190, 186), 3);
            g.DrawEllipse(waterRing, enemy.Pos.X - 34, enemy.Pos.Y - 15, 68, 30);
        }
        using var body = new SolidBrush(enemy.Flash > 0 ? Color.White : color); using var edge = new Pen(enemy == lockedTarget ? Color.Gold : IsArmored(enemy) ? Color.LightSteelBlue : Color.FromArgb(35, 25, 22), enemy == lockedTarget ? 4 : 2);
        int bossSpriteIndex = CampaignBossSpriteIndex(enemy.Kind), enemySpriteRow = EnemySpriteRow(enemy.Kind);
        Bitmap? individualSprite = enemy.Kind == EnemyKind.Elite && enemy.EliteAffix == EliteAffix.Spearbound ? SpearboundEliteSprite.Value : AdvancedEnemySprites.Value.GetValueOrDefault(enemy.Kind);
        bool individualSpriteLoaded = individualSprite is not null;
        bool bossSprite = bossSpriteIndex >= 0;
        int spriteRow = individualSpriteLoaded ? 0 : bossSprite ? bossSpriteIndex % 4 : enemySpriteRow;
        Bitmap? enemySprites = individualSpriteLoaded ? individualSprite : bossSprite ? bossSpriteIndex switch { < 4 => BossSpriteSheet.Value, < 8 => MidBossSpriteSheet.Value, < 12 => LateBossSpriteSheet.Value, < 16 => FinalWaveBossSpriteSheet.Value, _ => EndgameBossSpriteSheet.Value } : enemySpriteRow >= 0 ? EnemySpriteSheet.Value : null;
        int attackFrame = EnemyAttackSpriteFrame(enemy);
        if (!individualSpriteLoaded && !bossSprite && enemySpriteRow >= 0 && attackFrame >= 0)
            enemySprites = EnemyAttackSpriteSheet.Value ?? enemySprites;
        float spriteHeight = individualSpriteLoaded ? enemy.Kind switch { EnemyKind.Elite => 118, EnemyKind.EmberWisp => 72, EnemyKind.MudCantor => 86, EnemyKind.NameMoth => 84, EnemyKind.PaperWraith => 86, EnemyKind.SaltEcho => 82, _ => 80 } : bossSprite ? 132 : enemy.Kind == EnemyKind.Elite ? 82 : 70;
        if (enemy.NameOverride == "VIGIA DAS COMPORTAS")
            DrawSluiceWarden(g, enemy);
        else if (spriteRow >= 0 && enemySprites is not null)
        {
            float cellWidth = individualSpriteLoaded ? enemySprites.Width : enemySprites.Width / 4f, cellHeight = individualSpriteLoaded ? enemySprites.Height : enemySprites.Height / 4f;
            float windupProgress = enemy.WindupMax > 0 && enemy.Windup > 0 ? Math.Clamp(1f - enemy.Windup / enemy.WindupMax, 0f, 1f) : enemy.AttackPoseTime > 0 ? 1f : 0f;
            int frame = individualSpriteLoaded ? 0 : attackFrame >= 0 ? attackFrame : EnemySpriteFrame(enemy.AnimationTime, enemy.Moving && enemy.Stagger <= 0);
            RectangleF source = new(frame * cellWidth, spriteRow * cellHeight, cellWidth, cellHeight);
            float size = spriteHeight;
            float width = individualSpriteLoaded ? size * cellWidth / cellHeight : size;
            var uniqueAttackPose = individualSpriteLoaded || bossSprite ? EnemyAttackPose(enemy, windupProgress) : (Width: 1f, Height: 1f, OffsetX: 0f, OffsetY: 0f);
            bool freelyMoving = enemy.Moving && enemy.Windup <= 0 && enemy.AttackPoseTime <= 0 && enemy.Stagger <= 0;
            bool animatedCreature = individualSpriteLoaded && enemy.Kind != EnemyKind.Elite;
            float motionPhase = freelyMoving ? enemy.AnimationTime * 15 : windupPulse * 2.2f;
            float bob = animatedCreature ? MathF.Sin(motionPhase) * (freelyMoving ? 2f : 1.1f) : 0;
            float breath = animatedCreature && !freelyMoving && enemy.Windup <= 0 && enemy.Stagger <= 0 ? MathF.Sin(windupPulse * 2.4f) * 0.009f : 0;
            float posedWidth = width * (1f + windupProgress * 0.07f + breath) * uniqueAttackPose.Width, posedHeight = size * (1f - windupProgress * 0.035f + breath) * uniqueAttackPose.Height;
            float lunge = windupProgress * (bossSprite ? 5f : 7f) + uniqueAttackPose.OffsetX;
            RectangleF target = new(enemy.Pos.X - posedWidth / 2 + lunge, enemy.Pos.Y - posedHeight + bob + uniqueAttackPose.OffsetY, posedWidth, posedHeight);
            using var shadow = new SolidBrush(Color.FromArgb(110, 5, 7, 9));
            g.FillEllipse(shadow, enemy.Pos.X - r, enemy.Pos.Y - 5, r * 2, 10);
            var spriteTransform = g.Save();
            float facingX = enemy.Windup > 0 || enemy.AttackPoseTime > 0 ? enemy.Target.X : player.X;
            if (EnemyFacesLeft(enemy.Pos.X, facingX)) { g.TranslateTransform(enemy.Pos.X * 2, 0); g.ScaleTransform(-1, 1); }
            g.DrawImage(enemySprites, target, source, GraphicsUnit.Pixel);
            g.Restore(spriteTransform);
            if (enemy == lockedTarget || IsArmored(enemy))
                g.DrawEllipse(edge, enemy.Pos.X - r - 3, enemy.Pos.Y - 7, (r + 3) * 2, 12);
            if (enemy.Flash > 0)
            {
                using var hitFlash = new Pen(Color.FromArgb(170, 245, 235, 213), 2);
                g.DrawEllipse(hitFlash, enemy.Pos.X - r - 4, enemy.Pos.Y - 10, (r + 4) * 2, 16);
                int alpha = Math.Clamp((int)(190 * enemy.Flash / 0.3f), 0, 190);
                using var spriteFlash = new Pen(Color.FromArgb(alpha, 255, 233, 183), Math.Max(2, posedHeight / 32));
                using var spriteGlow = new SolidBrush(Color.FromArgb(alpha / 4, 255, 233, 183));
                RectangleF flashBounds = new(target.X + posedWidth * 0.2f, target.Y + posedHeight * 0.12f, posedWidth * 0.6f, posedHeight * 0.7f);
                g.FillEllipse(spriteGlow, flashBounds); g.DrawEllipse(spriteFlash, flashBounds);
            }
        }
        else
        {
            if (!DrawProceduralEnemy(g, enemy, enemy.Flash > 0 ? Color.White : color))
            {
                g.FillEllipse(body, enemy.Pos.X - r, enemy.Pos.Y - r, r * 2, r * 2); g.DrawEllipse(edge, enemy.Pos.X - r, enemy.Pos.Y - r, r * 2, r * 2);
            }
        }
        if (IsArmored(enemy) && !enemy.Armored)
        {
            using var wardRing = new Pen(Color.FromArgb(190, 186, 218, 231), 2) { DashStyle = DashStyle.Dash };
            g.DrawEllipse(wardRing, enemy.Pos.X - r - 4, enemy.Pos.Y - 9, (r + 4) * 2, 18);
        }
        if (enemy.Kind == EnemyKind.Elite)
        {
            Color affixColor = EliteAffixColor(enemy.EliteAffix);
            DashStyle dash = enemy.EliteAffix is EliteAffix.Swift or EliteAffix.Spearbound ? DashStyle.Dash : enemy.EliteAffix == EliteAffix.Bloodbound ? DashStyle.Dot : DashStyle.Solid;
            using var affixRing = new Pen(Color.FromArgb(225, affixColor), 3) { DashStyle = dash };
            g.DrawEllipse(affixRing, enemy.Pos.X - r - 6, enemy.Pos.Y - 12, (r + 6) * 2, 24);
            if (enemy.EliteAffix2 is EliteAffix secondAffix)
            {
                using var secondaryRing = new Pen(Color.FromArgb(230, EliteAffixColor(secondAffix)), 2.5f) { DashStyle = secondAffix == EliteAffix.Swift ? DashStyle.Dash : secondAffix == EliteAffix.Bloodbound ? DashStyle.Dot : DashStyle.Solid };
                g.DrawEllipse(secondaryRing, enemy.Pos.X - r - 11, enemy.Pos.Y - 17, (r + 11) * 2, 34);
            }
            if (enemy.SummonWindup > 0)
            {
                float progress = 1 - enemy.SummonWindup / 0.95f, radius = 18 + progress * 50;
                using var summonRing = new Pen(Color.FromArgb(220, 192, 112, 220), 3);
                g.DrawEllipse(summonRing, enemy.Pos.X - radius, enemy.Pos.Y - radius * 0.55f, radius * 2, radius * 1.1f);
            }
            if (enemy.SupportWindup > 0)
            {
                float progress = 1 - enemy.SupportWindup / 0.9f;
                using var supportRing = new Pen(Color.FromArgb(215, 127, 255, 198), highContrastTelegraphs ? 4 : 2.5f) { DashStyle = DashStyle.Dash };
                using var supportShadow = new Pen(Color.FromArgb(245, 10, 13, 15), 8);
                foreach (Enemy ally in enemies.Where(ally => ConductorPulseCovers(enemy, ally)))
                {
                    if (highContrastTelegraphs) { g.DrawLine(supportShadow, enemy.Pos, ally.Pos); g.DrawEllipse(supportShadow, ally.Pos.X - ally.Radius - 7, ally.Pos.Y - ally.Radius - 7, (ally.Radius + 7) * 2, (ally.Radius + 7) * 2); }
                    g.DrawLine(supportRing, enemy.Pos, ally.Pos);
                    g.DrawEllipse(supportRing, ally.Pos.X - ally.Radius - 7, ally.Pos.Y - ally.Radius - 7, (ally.Radius + 7) * 2, (ally.Radius + 7) * 2);
                }
                float radius = 24 + progress * 28;
                if (highContrastTelegraphs) g.DrawEllipse(supportShadow, enemy.Pos.X - radius, enemy.Pos.Y - radius * 0.55f, radius * 2, radius * 1.1f);
                g.DrawEllipse(supportRing, enemy.Pos.X - radius, enemy.Pos.Y - radius * 0.55f, radius * 2, radius * 1.1f);
                DrawCentered(g, "CANTANDO • INTERROMPA", enemy.Pos.Y - r - 44, new Font("Segoe UI", 8, FontStyle.Bold), Color.LightGreen, enemy.Pos.X);
            }
        }
        if (enemy.VigilMinion) { using var chorusRing = new Pen(Color.FromArgb(235, 194, 133, 230), 3) { DashStyle = DashStyle.Dot }; g.DrawEllipse(chorusRing, enemy.Pos.X - r - 6, enemy.Pos.Y - 12, (r + 6) * 2, 24); }
        if (enemy.EliteMinion) { using var summonedRing = new Pen(Color.FromArgb(235, 192, 112, 220), 3) { DashStyle = DashStyle.Dash }; g.DrawEllipse(summonedRing, enemy.Pos.X - r - 6, enemy.Pos.Y - 12, (r + 6) * 2, 24); }
        if (enemy.BeaconMarkTime > 0) { using var beaconMark = new Pen(Color.FromArgb(235, 165, 229, 238), 3) { DashStyle = DashStyle.Dot }; g.DrawEllipse(beaconMark, enemy.Pos.X - r - 8, enemy.Pos.Y - 15, (r + 8) * 2, 30); }
        if (enemy.BellMarkTime > 0) { using var bellMark = new Pen(Color.FromArgb(240, 246, 207, 119), 2) { DashStyle = DashStyle.Dot }; g.DrawEllipse(bellMark, enemy.Pos.X - r - 14, enemy.Pos.Y - 20, (r + 14) * 2, 40); }
        if (enemy.NameWeaverMarkTime > 0) { using var nameWeaverMark = new Pen(Color.FromArgb(235, 211, 139, 234), 3) { DashStyle = DashStyle.Dash }; g.DrawEllipse(nameWeaverMark, enemy.Pos.X - r - 20, enemy.Pos.Y - 25, (r + 20) * 2, 50); }
        if (enemy.ListenerMarkTime > 0) { using var listenerMark = new Pen(Color.FromArgb(235, 133, 221, 232), 3) { DashStyle = DashStyle.Dot }; g.DrawEllipse(listenerMark, enemy.Pos.X - r - 20, enemy.Pos.Y - 25, (r + 20) * 2, 50); }
        if (enemy.SparkMarkTime > 0) { using var sparkMark = new Pen(Color.FromArgb(240, 115, 221, 242), 2.5f) { DashStyle = DashStyle.DashDot }; g.DrawEllipse(sparkMark, enemy.Pos.X - r - 26, enemy.Pos.Y - 30, (r + 26) * 2, 60); }
        if (enemy.Kind == EnemyKind.ReedKeeper) { using var stalk = new Pen(Color.FromArgb(220, 205, 193, 133), 3); for (int i = -1; i <= 1; i++) g.DrawLine(stalk, enemy.Pos.X + i * 9, enemy.Pos.Y + 13, enemy.Pos.X + i * 5, enemy.Pos.Y - 23); }
        if (enemy.Kind == EnemyKind.FlowKeeper) { using var ring = new Pen(Color.FromArgb(210, 157, 215, 216), 2); float spin = windupPulse * 40; g.DrawArc(ring, enemy.Pos.X - 18, enemy.Pos.Y - 18, 36, 36, spin, 235); g.DrawArc(ring, enemy.Pos.X - 13, enemy.Pos.Y - 13, 26, 26, spin + 170, 115); }
        if (enemy.Kind == EnemyKind.GlassHerald) { using var shard = new Pen(Color.FromArgb(225, 186, 224, 231), 3); float spin = windupPulse * 24; for (int i = 0; i < 3; i++) { float angle = spin + i * 120; float radians = angle * MathF.PI / 180; PointF tip = new(enemy.Pos.X + MathF.Cos(radians) * 27, enemy.Pos.Y + MathF.Sin(radians) * 27); PointF tail = new(enemy.Pos.X - MathF.Cos(radians) * 18, enemy.Pos.Y - MathF.Sin(radians) * 18); g.DrawLine(shard, tail, tip); } }
        if (enemy.Kind == EnemyKind.RouteScribe) { using var folio = new Pen(Color.FromArgb(225, 196, 216, 226), 2); float spin = windupPulse * 28; for (int i = 0; i < 4; i++) { float angle = (spin + i * 90) * MathF.PI / 180; float x = enemy.Pos.X + MathF.Cos(angle) * 28, y = enemy.Pos.Y + MathF.Sin(angle) * 22; g.DrawRectangle(folio, x - 6, y - 8, 12, 16); } }
        float hpY = spriteRow >= 0 && enemySprites is not null ? enemy.Pos.Y - (bossSprite ? 141 : individualSpriteLoaded ? spriteHeight + 8 : enemy.Kind == EnemyKind.Elite ? 88 : 77) : enemy.Pos.Y - r - 10;
        using var hpBack = new SolidBrush(Color.FromArgb(40, 20, 20)); using var hpBar = new SolidBrush(Color.FromArgb(188, 76, 70)); g.FillRectangle(hpBack, enemy.Pos.X - 22, hpY, 44, 5); g.FillRectangle(hpBar, enemy.Pos.X - 22, hpY, 44 * Math.Max(0, enemy.Hp / enemy.MaxHp), 5);
        string name = enemy.EliteMinion ? "ECO INVOCADO" : EnemyName(enemy); if (name.Length > 0) DrawCentered(g, name, spriteRow >= 0 && enemySprites is not null ? hpY - 17 : enemy.Pos.Y - r - 28, new Font("Segoe UI", 8, FontStyle.Bold), Color.FromArgb(233, 198, 151), enemy.Pos.X);
        if (enemy.Boss) { using var bossBack = new SolidBrush(Color.FromArgb(40, 20, 20)); using var bossHp = new SolidBrush(enemy.Hp <= enemy.MaxHp * 0.5f ? Color.OrangeRed : enemy.Kind == EnemyKind.BlindScribe ? Color.Teal : enemy.Kind == EnemyKind.RiverFerryman ? Color.SteelBlue : enemy.Kind == EnemyKind.AshWarden ? Color.DarkOrange : enemy.Kind == EnemyKind.HollowName ? Color.LightSeaGreen : enemy.Kind == EnemyKind.NameWeaver ? Color.MediumPurple : enemy.Kind == EnemyKind.FirstKeeper ? Color.Goldenrod : enemy.Kind == EnemyKind.OathCartographer ? Color.DarkCyan : enemy.Kind == EnemyKind.ReedKeeper ? Color.DarkOliveGreen : enemy.Kind == EnemyKind.CounterCurrent ? Color.CadetBlue : enemy.Kind == EnemyKind.RiftCantor ? Color.MediumOrchid : enemy.Kind == EnemyKind.FlowKeeper ? Color.DeepSkyBlue : enemy.Kind == EnemyKind.GlassHerald ? Color.LightCyan : enemy.Kind is EnemyKind.NameBroker or EnemyKind.TideWitness ? Color.MediumPurple : enemy.Kind == EnemyKind.RouteScribe ? Color.LightSteelBlue : enemy.Kind == EnemyKind.LastTuner ? Color.PaleGoldenrod : Color.Firebrick); g.FillRectangle(bossBack, 360, 112, 240, 8); g.FillRectangle(bossHp, 360, 112, 240 * Math.Clamp(enemy.Hp / enemy.MaxHp, 0, 1), 8); string bossName = enemy.Kind == EnemyKind.BlindScribe ? "ESCRIBA CEGO" : enemy.Kind == EnemyKind.RiverFerryman ? "BARQUEIRO OCO" : enemy.Kind == EnemyKind.AshWarden ? "GUARDIÃO DAS CINZAS" : enemy.Kind == EnemyKind.HollowName ? "O NOME VAZIO" : enemy.Kind == EnemyKind.NameWeaver ? "A TECELÃ DE NOMES" : enemy.Kind == EnemyKind.FirstKeeper ? "A PRIMEIRA VIGÍLIA" : enemy.Kind == EnemyKind.OathCartographer ? "CARTÓGRAFA DO VÃO" : enemy.Kind == EnemyKind.NameBroker ? "ESCRIVÃO DE CONTRATO" : enemy.Kind == EnemyKind.TideWitness ? "TESTEMUNHA DA MARÉ" : enemy.Kind == EnemyKind.ReedKeeper ? "GUARDIÃ DO LEITO" : enemy.Kind == EnemyKind.CounterCurrent ? "AFINADOR DAS MARGENS" : enemy.Kind == EnemyKind.RiftCantor ? "A CONTRAVOZ" : enemy.Kind == EnemyKind.FlowKeeper ? "VIGIA DO VEIO PRESO" : enemy.Kind == EnemyKind.GlassHerald ? "ARAUTO DE VIDRO" : enemy.Kind == EnemyKind.LanternRegent ? "REGENTE DAS LANTERNAS" : enemy.Kind == EnemyKind.RouteScribe ? "ESCRIBA DO TRAÇADO" : enemy.Kind == EnemyKind.DeepSilence ? "SILÊNCIO DE BAIXO" : enemy.Kind == EnemyKind.BorrowedChorus ? "ARQUIVISTA DAS MARÉS" : enemy.Kind == EnemyKind.LastTuner ? "AFINADOR DO CONTRATEMPO" : "GUARDIÃO DA CRIPTA"; DrawCentered(g, enemy.Hp <= enemy.MaxHp * 0.5f ? $"{bossName} • SEGUNDA FASE" : bossName, 91, new Font("Georgia", 11, FontStyle.Bold), enemy.Hp <= enemy.MaxHp * 0.5f ? Color.OrangeRed : Color.Goldenrod); }
    }
    private static bool DrawProceduralEnemy(Graphics g, Enemy enemy, Color color)
    {
        float x = enemy.Pos.X, y = enemy.Pos.Y, r = Math.Max(13, enemy.Radius);
        using var fill = new SolidBrush(color);
        using var outline = new Pen(Color.FromArgb(225, 28, 25, 27), 2.2f);
        using var accent = new Pen(Color.FromArgb(205, color), 2.4f);
        switch (enemy.Kind)
        {
            case EnemyKind.SlagWeaver:
            {
                float rotation = enemy.AnimationTime * 0.6f;
                using var cinderGlow = new SolidBrush(Color.FromArgb(42, 237, 133, 69)); g.FillEllipse(cinderGlow, x - r * 1.5f, y - r * 1.5f, r * 3f, r * 3f);
                for (int i = 0; i < 4; i++)
                {
                    float angle = rotation + i * MathF.PI / 2;
                    PointF tip = new(x + MathF.Cos(angle) * r * 1.05f, y + MathF.Sin(angle) * r * 1.05f);
                    PointF left = new(x + MathF.Cos(angle + 1.1f) * r * 0.5f, y + MathF.Sin(angle + 1.1f) * r * 0.5f);
                    PointF right = new(x + MathF.Cos(angle - 1.1f) * r * 0.5f, y + MathF.Sin(angle - 1.1f) * r * 0.5f);
                    using var shard = new GraphicsPath(); shard.AddPolygon(new[] { tip, left, new PointF(x, y), right }); g.FillPath(fill, shard); g.DrawPath(outline, shard);
                }
                g.FillEllipse(fill, x - r * 0.48f, y - r * 0.48f, r * 0.96f, r * 0.96f); g.DrawEllipse(outline, x - r * 0.48f, y - r * 0.48f, r * 0.96f, r * 0.96f);
                using var core = new SolidBrush(Color.FromArgb(245, 250, 193, 126)); g.FillEllipse(core, x - r * 0.14f, y - r * 0.14f, r * 0.28f, r * 0.28f);
                return true;
            }
            case EnemyKind.CooldownLeech:
            {
                float rotation = enemy.AnimationTime * 34f;
                g.DrawArc(accent, x - r * 1.45f, y - r * 1.45f, r * 2.9f, r * 2.9f, rotation, 115);
                g.DrawArc(accent, x - r * 1.45f, y - r * 1.45f, r * 2.9f, r * 2.9f, rotation + 180, 115);
                g.FillEllipse(fill, x - r * .58f, y - r * .58f, r * 1.16f, r * 1.16f);
                g.DrawEllipse(outline, x - r * .58f, y - r * .58f, r * 1.16f, r * 1.16f);
                using (var core = new SolidBrush(Color.FromArgb(240, 238, 205, 255))) g.FillEllipse(core, x - r * .17f, y - r * .17f, r * .34f, r * .34f);
                return true;
            }
            case EnemyKind.LastTuner:
                g.DrawEllipse(accent, x - r * 1.65f, y - r * 1.65f, r * 3.3f, r * 3.3f);
                g.DrawEllipse(accent, x - r * 1.18f, y - r * 1.18f, r * 2.36f, r * 2.36f);
                g.DrawLine(accent, x, y - r * 1.25f, x, y - r * .62f);
                g.FillEllipse(fill, x - r * .54f, y - r * .63f, r * 1.08f, r * 1.28f);
                g.DrawEllipse(outline, x - r * .54f, y - r * .63f, r * 1.08f, r * 1.28f);
                using (var bell = new GraphicsPath()) { bell.AddPolygon(new[] { new PointF(x - r * .34f, y - r * .12f), new PointF(x - r * .25f, y + r * .45f), new PointF(x + r * .25f, y + r * .45f), new PointF(x + r * .34f, y - r * .12f) }); g.DrawPath(accent, bell); }
                g.DrawLine(accent, x + r * .65f, y - r * .2f, x + r * 1.15f, y + r * .55f);
                return true;
            case EnemyKind.BorrowedChorus:
                g.DrawEllipse(accent, x - r * 1.5f, y - r * 1.5f, r * 3, r * 3);
                g.DrawEllipse(accent, x - r, y - r, r * 2, r * 2);
                for (int i = 0; i < 3; i++)
                {
                    float angle = enemy.AnimationTime * 0.7f + i * MathF.PI * 2 / 3;
                    float sx = x + MathF.Cos(angle) * r * 1.3f, sy = y + MathF.Sin(angle) * r * 1.3f;
                    g.FillEllipse(fill, sx - r * .22f, sy - r * .22f, r * .44f, r * .44f);
                }
                g.FillEllipse(fill, x - r * .42f, y - r * .52f, r * .84f, r * 1.04f);
                g.DrawEllipse(outline, x - r * .42f, y - r * .52f, r * .84f, r * 1.04f);
                return true;
            case EnemyKind.InkEcho:
                using (var shape = new GraphicsPath())
                {
                    shape.AddPolygon(new[] { new PointF(x - r * .72f, y - r * .7f), new PointF(x + r * .5f, y - r), new PointF(x + r * .8f, y - r * .1f), new PointF(x + r * .22f, y + r * .9f), new PointF(x - r * .05f, y + r * .34f), new PointF(x - r * .72f, y + r * .72f) });
                    g.FillPath(fill, shape); g.DrawPath(outline, shape);
                }
                using (var eye = new SolidBrush(Color.FromArgb(235, 223, 195, 247))) g.FillEllipse(eye, x + r * .12f, y - r * .45f, r * .28f, r * .15f);
                g.DrawLine(accent, x - r * 1.15f, y + r * .72f, x - r * .42f, y + r * .15f);
                return true;
            case EnemyKind.DrownedMemory:
                g.DrawEllipse(accent, x - r * .86f, y - r * .86f, r * 1.72f, r * 1.72f);
                g.DrawEllipse(accent, x - r * .56f, y - r * .56f, r * 1.12f, r * 1.12f);
                g.FillEllipse(fill, x - r * .27f, y - r * .37f, r * .54f, r * .74f);
                using (var eye = new SolidBrush(Color.FromArgb(220, 220, 244, 230))) g.FillEllipse(eye, x - r * .11f, y - r * .12f, r * .22f, r * .15f);
                return true;
            case EnemyKind.EmberWisp:
                using (var halo = new SolidBrush(Color.FromArgb(48, color))) g.FillEllipse(halo, x - r * 1.3f, y - r * 1.3f, r * 2.6f, r * 2.6f);
                using (var star = new GraphicsPath())
                {
                    PointF[] points = Enumerable.Range(0, 8).Select(i => { float a = (float)(i * Math.PI / 4 - Math.PI / 2); float radius = i % 2 == 0 ? r : r * .48f; return new PointF(x + MathF.Cos(a) * radius, y + MathF.Sin(a) * radius); }).ToArray();
                    star.AddPolygon(points); g.FillPath(fill, star); g.DrawPath(outline, star);
                }
                using (var core = new SolidBrush(Color.FromArgb(240, 255, 226, 166))) g.FillEllipse(core, x - r * .18f, y - r * .18f, r * .36f, r * .36f);
                return true;
            case EnemyKind.NameMoth:
                using (var wings = new GraphicsPath())
                {
                    wings.AddPolygon(new[] { new PointF(x - r * .18f, y - r * .05f), new PointF(x - r * 1.25f, y - r), new PointF(x - r * .9f, y + r * .78f), new PointF(x - r * .12f, y + r * .34f) });
                    wings.AddPolygon(new[] { new PointF(x + r * .18f, y - r * .05f), new PointF(x + r * 1.25f, y - r), new PointF(x + r * .9f, y + r * .78f), new PointF(x + r * .12f, y + r * .34f) });
                    g.FillPath(fill, wings); g.DrawPath(outline, wings);
                }
                g.DrawLine(outline, x - 3, y - r * .15f, x - r * .35f, y - r * .9f); g.DrawLine(outline, x + 3, y - r * .15f, x + r * .35f, y - r * .9f);
                using (var body = new SolidBrush(Color.FromArgb(240, 224, 224, 191))) g.FillEllipse(body, x - r * .17f, y - r * .25f, r * .34f, r * .85f);
                using (var eye = new SolidBrush(Color.FromArgb(235, 204, 255, 229))) g.FillEllipse(eye, x - r * .08f, y - r * .17f, r * .16f, r * .12f);
                return true;
            case EnemyKind.MudCantor:
                g.FillEllipse(fill, x - r * .8f, y - r * .72f, r * 1.6f, r * 1.45f); g.DrawEllipse(outline, x - r * .8f, y - r * .72f, r * 1.6f, r * 1.45f);
                g.DrawLine(outline, x - r * .66f, y - r * .42f, x + r * .66f, y - r * .42f);
                using (var eye = new SolidBrush(Color.FromArgb(235, 247, 210, 148))) g.FillEllipse(eye, x - r * .38f, y - r * .19f, r * .2f, r * .12f);
                g.DrawArc(accent, x - r * 1.18f, y - r * .48f, r * 2.36f, r * 1.7f, 300, 120);
                g.DrawArc(accent, x - r * 1.48f, y - r * .7f, r * 2.96f, r * 2.1f, 300, 120);
                return true;
            case EnemyKind.PaperWraith:
                var transform = g.Save(); g.TranslateTransform(x, y); g.RotateTransform(MathF.Sin(enemy.AnimationTime * 4f) * 9f);
                using (var page = new GraphicsPath())
                {
                    page.AddPolygon(new[] { new PointF(-r * .62f, -r), new PointF(r * .3f, -r * .86f), new PointF(r * .68f, -r * .2f), new PointF(r * .42f, r * .34f), new PointF(r * .66f, r), new PointF(-r * .32f, r * .78f), new PointF(-r * .72f, r * .08f) });
                    g.FillPath(fill, page); g.DrawPath(outline, page);
                }
                g.DrawLine(accent, -r * .34f, -r * .32f, r * .35f, -r * .18f); g.DrawLine(accent, -r * .25f, r * .03f, r * .22f, r * .16f);
                g.Restore(transform);
                return true;
            case EnemyKind.SaltEcho:
                using (var crystal = new GraphicsPath())
                {
                    crystal.AddPolygon(new[] { new PointF(x, y - r), new PointF(x + r * .72f, y - r * .42f), new PointF(x + r * .62f, y + r * .48f), new PointF(x, y + r), new PointF(x - r * .68f, y + r * .48f), new PointF(x - r * .72f, y - r * .42f) });
                    g.FillPath(fill, crystal); g.DrawPath(outline, crystal);
                }
                g.DrawLine(accent, x, y - r * .78f, x, y + r * .73f); g.DrawLine(accent, x, y - r * .05f, x + r * .5f, y - r * .38f); g.DrawLine(accent, x, y + r * .08f, x - r * .5f, y - r * .34f);
                return true;
            default:
                return false;
        }
    }

    private void DrawFloatingTexts(Graphics g)
    {
        if (floatingTexts.Count == 0) return;
        using var font = new Font("Segoe UI", 10, FontStyle.Bold);
        foreach (FloatingText text in floatingTexts)
        {
            int alpha = Math.Clamp((int)(255 * text.TimeLeft / text.Duration), 0, 255);
            using var brush = new SolidBrush(Color.FromArgb(alpha, text.Color));
            using var outline = new SolidBrush(Color.FromArgb(alpha, 8, 10, 12));
            SizeF size = g.MeasureString(text.Text, font);
            float x = text.Pos.X - size.Width / 2, y = text.Pos.Y - size.Height / 2;
            g.DrawString(text.Text, font, outline, x + 1.5f, y + 1.5f); g.DrawString(text.Text, font, brush, x, y);
        }
    }

    private long KnownRoomMask()
    {
        long known = clearedRooms | (1L << room);
        if (questStage >= 3) known |= 1L << 3;
        if (questStage >= 22) known |= 1L << 18;
        if (questStage >= 57) known |= 1L << 41;
        if (questStage >= 58) known |= 1L << 42;
        if (room >= 43 || firstEchoDiscovered != 0 || firstEchoQuest > 0 || firstEchoCaveFound) known |= 1L << 43;
        if ((firstEchoDiscovered & 2) != 0 || firstEchoCaveFound) known |= 1L << 44;
        if ((firstEchoDiscovered & 4) != 0 || firstEchoQuest >= 2) known |= 1L << 45;
        return known;
    }

    private string AtlasRoomName(int index)
    {
        string name = roomNames[index];
        int separator = name.LastIndexOf(" • ", StringComparison.Ordinal);
        return separator >= 0 ? name[(separator + 3)..] : name;
    }

    private void DrawMapOverlay(Graphics g)
    {
        if (room == 43) { DrawFirstEchoMap(g); return; }
        using var shade = new SolidBrush(Color.FromArgb(225, 5, 7, 9));
        using var panel = new SolidBrush(Color.FromArgb(248, 15, 17, 18));
        using var border = new Pen(Color.FromArgb(190, 143, 112, 73), 2);
        using var nodeFont = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.FillRectangle(shade, 0, 0, 960, 640);
        g.FillRectangle(panel, 28, 105, 904, 488);
        g.DrawRectangle(border, 28, 105, 904, 488);
        DrawCentered(g, "ATLAS DAS CÂMARAS", 120, new Font("Georgia", 17, FontStyle.Bold), Color.Goldenrod);
        DrawCentered(g, roomNames[room], 145, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite);

        long known = KnownRoomMask();
        const int columns = 5, nodeWidth = 168, nodeHeight = 37, gapX = 8, gapY = 3;
        for (int index = 0; index < roomNames.Length; index++)
        {
            int column = index % columns, row = index / columns;
            float x = 36 + column * (nodeWidth + gapX), y = 165 + row * (nodeHeight + gapY);
            bool current = index == room, visited = (known & (1L << index)) != 0, cleared = (clearedRooms & (1L << index)) != 0;
            Color fill = current ? Color.FromArgb(110, 103, 48) : cleared ? Color.FromArgb(39, 67, 53) : visited ? Color.FromArgb(52, 58, 57) : Color.FromArgb(29, 32, 33);
            Color line = current ? Color.Goldenrod : cleared ? Color.FromArgb(89, 139, 105) : visited ? Color.FromArgb(95, 103, 96) : Color.FromArgb(54, 58, 56);
            Color ink = visited ? Color.AntiqueWhite : Color.FromArgb(108, 112, 105);
            using var nodeBrush = new SolidBrush(fill);
            using var nodeBorder = new Pen(line, current ? 2 : 1);
            g.FillRectangle(nodeBrush, x, y, nodeWidth, nodeHeight);
            g.DrawRectangle(nodeBorder, x, y, nodeWidth, nodeHeight);
            string label = visited ? $"{index + 1:00}\n{AtlasRoomName(index)}" : $"{index + 1:00}\n???";
            using var textBrush = new SolidBrush(current ? Color.FromArgb(255, 224, 166) : ink);
            g.DrawString(label, nodeFont, textBrush, new RectangleF(x + 4, y + 2, nodeWidth - 8, nodeHeight - 4), format);
        }
        using var legendFont = new Font("Segoe UI", 9);
        using var legendBrush = new SolidBrush(Color.FromArgb(180, 185, 176));
        g.DrawString("DOURADO: AQUI   VERDE: LIMPA   CINZA: VISITADA   ???: DESCONHECIDA   M/ESC FECHA", legendFont, legendBrush, 54, 578);
    }

    private void DrawPlayer(Graphics g)
    {
        PointF animationDirection = attackFlash > 0 ? facing : dashTime > 0 ? dashDirection : MovementInput();
        if (Length(animationDirection) <= 0.001f) animationDirection = CurrentDrift();
        if (Length(animationDirection) <= 0.001f) animationDirection = facing;
        Bitmap? attackSprites = attackFlash > 0 ? heroClass switch
        {
            HeroClass.StoneGuardian => StoneGuardianAttackSpriteSheet.Value ?? PlayerAttackSpriteSheet.Value,
            HeroClass.NameWeaver => NameWeaverAttackSpriteSheet.Value ?? PlayerAttackSpriteSheet.Value,
            HeroClass.FordCartographer => FordCartographerAttackSpriteSheet.Value ?? PlayerAttackSpriteSheet.Value,
            _ => PlayerAttackSpriteSheet.Value
        } : null;
        Bitmap? sprites = attackSprites ?? heroClass switch
        {
            HeroClass.StoneGuardian => StoneGuardianSpriteSheet.Value ?? PlayerSpriteSheet.Value,
            HeroClass.NameWeaver => NameWeaverSpriteSheet.Value ?? PlayerSpriteSheet.Value,
            HeroClass.FordCartographer => FordCartographerSpriteSheet.Value ?? PlayerSpriteSheet.Value,
            _ => PlayerSpriteSheet.Value
        };
        if (sprites is not null)
        {
            int row = PlayerSpriteRow(animationDirection);
            bool moving = dashTime > 0 || Length(MovementInput()) > 0.001f || Length(CurrentDrift()) > 0.001f;
            int frame = attackSprites is not null ? Math.Clamp((int)((0.16f - attackFlash) / 0.16f * 4), 0, 3) : PlayerSpriteFrame(playerAnimationTime, moving);
            float cellWidth = sprites.Width / 4f, cellHeight = sprites.Height / 4f;
            RectangleF source = new(frame * cellWidth, row * cellHeight, cellWidth, cellHeight);
            RectangleF target = new(player.X - 38, player.Y - 73, 76, 76);
            using var shadow = new SolidBrush(Color.FromArgb(100, 5, 7, 9));
            g.FillEllipse(shadow, player.X - 17, player.Y - 5, 34, 10);
            g.DrawImage(sprites, target, source, GraphicsUnit.Pixel);
            if (invulnerable > 0)
            {
                int flashAlpha = PlayerDamageFlashAlpha(invulnerable, windupPulse);
                using var damageGlow = new SolidBrush(Color.FromArgb(flashAlpha / 3, 239, 83, 76));
                using var damageEdge = new Pen(Color.FromArgb(flashAlpha, 255, 126, 105), 3);
                RectangleF damageBounds = new(target.X + 18, target.Y + 14, 40, 50);
                g.FillEllipse(damageGlow, damageBounds); g.DrawEllipse(damageEdge, damageBounds);
            }
        }
        else
        {
            using var fallback = new SolidBrush(invulnerable > 0 ? Color.Salmon : Color.FromArgb(90, 143, 138));
            using var fallbackFace = new SolidBrush(Color.Bisque);
            g.FillEllipse(fallback, player.X - 16, player.Y - 16, 32, 32);
            g.FillEllipse(fallbackFace, player.X - 8, player.Y - 11, 16, 16);
            using var facingLine = new Pen(Color.AntiqueWhite, 4);
            g.DrawLine(facingLine, player, Add(player, Scale(facing, 24)));
        }
        if (wardTime > 0)
        {
            int alpha = Math.Clamp((int)(112 + 70 * MathF.Sin(windupPulse * 8)), 35, 190);
            using var wardGlow = new Pen(Color.FromArgb(alpha, 179, 215, 215), 4);
            using var wardCore = new Pen(Color.FromArgb(Math.Min(235, alpha + 45), 215, 231, 213), 1);
            g.DrawEllipse(wardGlow, player.X - 25, player.Y - 25, 50, 50);
            g.DrawEllipse(wardCore, player.X - 20, player.Y - 20, 40, 40);
        }
        if (stoneBastionTime > 0)
        {
            int alpha = Math.Clamp((int)(145 + 80 * MathF.Sin(windupPulse * 11)), 55, 230);
            using var bastionGlow = new Pen(Color.FromArgb(alpha, 226, 137, 76), 5);
            using var bastionCore = new Pen(Color.FromArgb(Math.Min(245, alpha + 25), 255, 218, 139), 1.5f);
            g.DrawEllipse(bastionGlow, player.X - 31, player.Y - 31, 62, 62);
            g.DrawEllipse(bastionCore, player.X - 25, player.Y - 25, 50, 50);
        }
        if (skillCd > 0) { using var cooldownPen = new Pen(Color.FromArgb(190, 230, 193, 129), 3); g.DrawArc(cooldownPen, player.X - 25, player.Y - 25, 50, 50, -90, 360 * skillCd / Math.Max(1.5f, 6f - skillTalent * .5f)); }
        if (shardCd > 0) { using var shardCooldownPen = new Pen(Color.FromArgb(205, 107, 202, 196), 3); g.DrawArc(shardCooldownPen, player.X - 33, player.Y - 33, 66, 66, 90, 360 * shardCd / VoiceShardCooldown()); }
        if (riverBell && bellCd > 0) { using var bellCooldownPen = new Pen(Color.FromArgb(210, 205, 169, 118), 3); g.DrawArc(bellCooldownPen, player.X - 41, player.Y - 41, 82, 82, 180, 360 * bellCd / BellPulseCooldown()); }
        if (ebbSeal && pullCd > 0) { using var ebbCooldownPen = new Pen(Color.FromArgb(210, 115, 185, 211), 3); g.DrawArc(ebbCooldownPen, player.X - 49, player.Y - 49, 98, 98, 270, 360 * pullCd / EbbPullCooldown()); }
        if (anchorWard && wardCd > 0) { using var wardCooldownPen = new Pen(Color.FromArgb(210, 179, 215, 215), 3); g.DrawArc(wardCooldownPen, player.X - 57, player.Y - 57, 114, 114, 0, 360 * wardCd / AnchorWardCooldown()); }
        if (refrainSong && refrainCd > 0) { using var refrainCooldownPen = new Pen(Color.FromArgb(215, 205, 139, 225), 3); g.DrawArc(refrainCooldownPen, player.X - 65, player.Y - 65, 130, 130, 90, 360 * refrainCd / RefrainCooldown()); }
        if (beaconLight && beaconCd > 0) { using var beaconCooldownPen = new Pen(Color.FromArgb(220, 182, 238, 220), 3); g.DrawArc(beaconCooldownPen, player.X - 73, player.Y - 73, 146, 146, 180, 360 * beaconCd / BeaconTotalCooldown()); }
        if (finalEnding == 1 && returnVoiceCd > 0) { using var returnedVoicesCooldownPen = new Pen(Color.FromArgb(215, 205, 221, 165), 3); g.DrawArc(returnedVoicesCooldownPen, player.X - 81, player.Y - 81, 162, 162, 270, 360 * returnVoiceCd / (Math.Max(14f, 23f - skillTalent * 0.7f) * WeaponCooldownMultiplier())); }
        if (finalEnding == 2 && finalStrikeCd > 0) { using var finalSparkCooldownPen = new Pen(Color.FromArgb(225, 255, 191, 115), 3); g.DrawArc(finalSparkCooldownPen, player.X - 89, player.Y - 89, 178, 178, 0, 360 * finalStrikeCd / (Math.Max(12f, 19f - skillTalent * 0.65f) * (skillVariants[8] == 1 ? 1.25f : skillVariants[8] == 2 ? 0.75f : 1f) * WeaponCooldownMultiplier())); }
        if (heroClass == HeroClass.StoneGuardian && stoneBastionCd > 0) { using var bastionCooldownPen = new Pen(Color.FromArgb(225, 226, 137, 76), 3); g.DrawArc(bastionCooldownPen, player.X - 97, player.Y - 97, 194, 194, 90, 360 * stoneBastionCd / (18f * WeaponCooldownMultiplier())); }
        if (heroClass == HeroClass.NameWeaver && nameWeaverCd > 0) { using var threadCooldownPen = new Pen(Color.FromArgb(225, 193, 151, 224), 3); g.DrawArc(threadCooldownPen, player.X - 97, player.Y - 97, 194, 194, 90, 360 * nameWeaverCd / (14f * WeaponCooldownMultiplier())); }
        if (heroClass == HeroClass.Listener && listenerMarkCd > 0) { using var listenerCooldownPen = new Pen(Color.FromArgb(230, 133, 221, 232), 3); g.DrawArc(listenerCooldownPen, player.X - 97, player.Y - 97, 194, 194, 90, 360 * listenerMarkCd / (16f * (skillVariants[12] == 1 ? 1.2f : 1f) * WeaponCooldownMultiplier())); }
        if (heroClass == HeroClass.Listener && listenerMarkCd > 0) { using var listenerCooldownPen = new Pen(Color.FromArgb(230, 133, 221, 232), 3); g.DrawArc(listenerCooldownPen, player.X - 97, player.Y - 97, 194, 194, 90, 360 * listenerMarkCd / (16f * (skillVariants[12] == 1 ? 1.2f : 1f) * WeaponCooldownMultiplier())); }
        if (heroClass == HeroClass.FordCartographer && fordShortcutCd > 0) { using var shortcutCooldownPen = new Pen(Color.FromArgb(230, 125, 213, 199), 3); g.DrawArc(shortcutCooldownPen, player.X - 105, player.Y - 105, 210, 210, 90, 360 * fordShortcutCd / (13f * WeaponCooldownMultiplier())); }
    }
    private static int PlayerSpriteRow(PointF direction)
    {
        if (MathF.Abs(direction.X) > MathF.Abs(direction.Y)) return direction.X < 0 ? 1 : 2;
        return direction.Y < 0 ? 3 : 0;
    }
    private static int PlayerDamageFlashAlpha(float remaining, float pulse) => remaining <= 0 ? 0 : (int)(70 + 55 * (0.5f + 0.5f * MathF.Sin(pulse * 42)));
    private static int PlayerSpriteFrame(float animationTime, bool moving) => moving ? (int)(Math.Max(0, animationTime) / 0.12f) % 4 : 0;
    private void DrawAttackSwing(Graphics g)
    {
        float progress = Math.Clamp(attackFlash / 0.16f, 0, 1);
        WeaponDefinition weaponStyle = EquipmentCatalog.Weapon(weapon);
        if (weaponStyle.IsRanged)
        {
            PointF bowCenter = Add(player, Scale(facing, 17)), bowPerpendicular = new(-facing.Y, facing.X);
            int bowAlpha = (int)(progress * 230); using var bowFlash = new Pen(Color.FromArgb(bowAlpha, weaponStyle.AttackColor), 2.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLine(bowFlash, Add(bowCenter, Scale(bowPerpendicular, -9)), Add(bowCenter, Scale(bowPerpendicular, 9)));
            g.DrawLine(bowFlash, Add(player, Scale(facing, 11)), Add(player, Scale(facing, 23)));
            return;
        }
        if (weaponStyle.IsThrust)
        {
            float reach = 20 + 108 * MathF.Sin(MathF.PI * (1 - progress));
            PointF tip = Add(player, Scale(facing, reach));
            PointF basePoint = Add(player, Scale(facing, 16));
            PointF cross = new(-facing.Y * 9, facing.X * 9);
            int spearAlpha = (int)(progress * 230);
            using var spearGlow = new Pen(Color.FromArgb(spearAlpha / 3, 137, 221, 209), 12) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            using var spearEdge = new Pen(Color.FromArgb(spearAlpha, 208, 231, 202), 3) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLine(spearGlow, basePoint, tip); g.DrawLine(spearEdge, basePoint, tip);
            g.DrawLine(spearEdge, Add(basePoint, cross), Add(basePoint, Scale(cross, -1)));
            using var pointGlow = new SolidBrush(Color.FromArgb(spearAlpha / 2, 150, 238, 224));
            g.FillEllipse(pointGlow, tip.X - 5, tip.Y - 5, 10, 10);
            return;
        }
        float elapsed = 1 - progress;
        float angle = MathF.Atan2(facing.Y, facing.X) * 180 / MathF.PI;
        float radius = weaponStyle.VisualRadius;
        float sweep = BasicAttackSweepAngle(weapon);
        float activeSweep = sweep * weaponStyle.ActiveSweepFraction;
        float startAngle = angle - sweep / 2 + (sweep - activeSweep) * elapsed;
        var bounds = new RectangleF(player.X - radius, player.Y - radius, radius * 2, radius * 2);
        Color color = weaponStyle.AttackColor;
        int alpha = (int)(progress * (115 + 110 * MathF.Sin(MathF.PI * elapsed)));
        using var glow = new Pen(Color.FromArgb(alpha / 3, color), weaponStyle.GlowWidth) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var edge = new Pen(Color.FromArgb(alpha, color), weaponStyle.EdgeWidth) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawArc(glow, bounds, startAngle, activeSweep);
        g.DrawArc(edge, bounds, startAngle, activeSweep);
        float tipAngle = (startAngle + activeSweep) * MathF.PI / 180;
        PointF tipDirection = new(MathF.Cos(tipAngle), MathF.Sin(tipAngle));
        PointF bladeTip = Add(player, Scale(tipDirection, radius + 7));
        PointF bladeBase = Add(player, Scale(tipDirection, radius - weaponStyle.BaseInset));
        g.DrawLine(glow, bladeBase, bladeTip); g.DrawLine(edge, bladeBase, bladeTip);
        using var tipGlow = new SolidBrush(Color.FromArgb(alpha / 2, color));
        g.FillEllipse(tipGlow, bladeTip.X - 5, bladeTip.Y - 5, 10, 10);
    }
    private void DrawGroundHazards(Graphics g)
    {
        foreach (GroundHazard hazard in groundHazards)
        {
            float fade = Math.Clamp(hazard.TimeLeft / 0.45f, 0.2f, 1f), pulse = 0.5f + 0.5f * MathF.Sin(windupPulse * 5f);
            if (hazard.WarningTime > 0)
            {
                int warningAlpha = (int)(120 + pulse * 100);
                using var warningFill = new SolidBrush(Color.FromArgb(30 + (int)(pulse * 22), 255, 206, 94));
                using var warningRim = new Pen(Color.FromArgb(warningAlpha, 255, 224, 140), 3) { DashStyle = DashStyle.Dash };
                g.FillEllipse(warningFill, hazard.Pos.X - hazard.Radius, hazard.Pos.Y - hazard.Radius, hazard.Radius * 2, hazard.Radius * 2);
                g.DrawEllipse(warningRim, hazard.Pos.X - hazard.Radius, hazard.Pos.Y - hazard.Radius, hazard.Radius * 2, hazard.Radius * 2);
                continue;
            }
            int fillAlpha = (int)(34 * fade + pulse * 12), edgeAlpha = (int)(190 * fade);
            using var fill = new SolidBrush(Color.FromArgb(fillAlpha, 181, 89, 42));
            using var rim = new Pen(Color.FromArgb(edgeAlpha, 244, 155, 87), 2.5f) { DashStyle = DashStyle.Dash };
            using var fissure = new Pen(Color.FromArgb((int)(150 * fade), 238, 177, 101), 2);
            g.FillEllipse(fill, hazard.Pos.X - hazard.Radius, hazard.Pos.Y - hazard.Radius, hazard.Radius * 2, hazard.Radius * 2);
            g.DrawEllipse(rim, hazard.Pos.X - hazard.Radius, hazard.Pos.Y - hazard.Radius, hazard.Radius * 2, hazard.Radius * 2);
            g.DrawLine(fissure, hazard.Pos.X - 20, hazard.Pos.Y - 10, hazard.Pos.X - 5, hazard.Pos.Y + 4);
            g.DrawLine(fissure, hazard.Pos.X - 5, hazard.Pos.Y + 4, hazard.Pos.X + 12, hazard.Pos.Y - 6);
            g.DrawLine(fissure, hazard.Pos.X + 12, hazard.Pos.Y - 6, hazard.Pos.X + 24, hazard.Pos.Y + 11);
        }
    }

    private void DrawEbbWave(Graphics g) { float progress = 1f - pullWaveTime / 0.45f; float radius = EbbPullRadius() * progress; using var wave = new Pen(Color.FromArgb((int)(210 * pullWaveTime / 0.45f), 98, 190, 220), 4); using var glow = new Pen(Color.FromArgb((int)(65 * pullWaveTime / 0.45f), 98, 190, 220), 12); g.DrawEllipse(glow, player.X - radius, player.Y - radius, radius * 2, radius * 2); g.DrawEllipse(wave, player.X - radius, player.Y - radius, radius * 2, radius * 2); }
    private void DrawBellWave(Graphics g) { float progress = 1f - bellWaveTime / 0.42f; float radius = bellWaveRadius * progress; using var wave = new Pen(Color.FromArgb((int)(210 * bellWaveTime / 0.42f), 115, 208, 197), 4); using var glow = new Pen(Color.FromArgb((int)(65 * bellWaveTime / 0.42f), 115, 208, 197), 12); g.DrawEllipse(glow, player.X - radius, player.Y - radius, radius * 2, radius * 2); g.DrawEllipse(wave, player.X - radius, player.Y - radius, radius * 2, radius * 2); }
    private void DrawFordBurst(Graphics g) { float duration = Math.Max(0.01f, fordBurstDuration); float progress = 1f - fordBurstTime / duration; float radius = fordBurstRadius * progress; int alpha = (int)(220 * fordBurstTime / duration); using var wave = new Pen(Color.FromArgb(alpha, 125, 213, 199), 4); using var glow = new Pen(Color.FromArgb(alpha / 3, 125, 213, 199), 14); g.DrawEllipse(glow, fordBurstPosition.X - radius, fordBurstPosition.Y - radius, radius * 2, radius * 2); g.DrawEllipse(wave, fordBurstPosition.X - radius, fordBurstPosition.Y - radius, radius * 2, radius * 2); }
    private static Bitmap CreateFordCartographerSpriteSheet(bool attack)
    {
        const int cell = 80;
        var sheet = new Bitmap(cell * 4, cell * 4);
        using Graphics g = Graphics.FromImage(sheet);
        g.Clear(Color.Transparent);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.CompositingQuality = CompositingQuality.HighQuality;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        for (int row = 0; row < 4; row++)
        for (int frame = 0; frame < 4; frame++)
        {
            GraphicsState state = g.Save();
            g.SetClip(new Rectangle(frame * cell, row * cell, cell, cell));
            g.TranslateTransform(frame * cell + 40, row * cell + 49);
            g.ScaleTransform(0.88f, 0.88f);
            g.RotateTransform(row switch { 0 => 90, 1 => 180, 2 => 0, _ => 270 });
            DrawFordCartographerFrame(g, frame, attack);
            g.Restore(state);
        }
        return sheet;
    }

    private static void DrawFordCartographerFrame(Graphics g, int frame, bool attack)
    {
        float stride = attack ? 0 : MathF.Sin(frame * MathF.PI / 2) * 4;
        float bob = attack ? frame switch { 0 => 0, 1 => -2, 2 => 1, _ => 0 } : MathF.Abs(MathF.Sin(frame * MathF.PI / 2)) * 1.7f;
        float capeFlick = attack ? frame switch { 0 => -1, 1 => 4, 2 => 2, _ => 0 } : MathF.Sin(frame * MathF.PI / 2 + 0.9f) * 3;
        float attackReach = attack ? frame switch { 0 => 12, 1 => 32, 2 => 26, _ => 16 } : 23;
        var state = g.Save(); g.TranslateTransform(0, -bob);
        using var cloak = new SolidBrush(Color.FromArgb(45, 126, 121));
        using var cloakEdge = new Pen(Color.FromArgb(235, 180, 218, 193), 2);
        using var hood = new SolidBrush(Color.FromArgb(29, 51, 54));
        using var face = new SolidBrush(Color.FromArgb(226, 205, 167));
        using var brass = new SolidBrush(Color.FromArgb(226, 192, 119));
        using var boots = new SolidBrush(Color.FromArgb(230, 42, 37, 32));
        using var line = new Pen(Color.FromArgb(230, 221, 208), 3) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(line, -5, 13, -7 - stride, 26 + Math.Max(0, stride));
        g.DrawLine(line, 5, 13, 7 + stride, 26 + Math.Max(0, -stride));
        g.FillEllipse(boots, -12 - stride, 23 + Math.Max(0, stride), 10, 5);
        g.FillEllipse(boots, 3 + stride, 23 + Math.Max(0, -stride), 10, 5);
        PointF[] mantle = { new(-12, -5), new(10, -8), new(20, 22 + capeFlick), new(7, 29 + capeFlick), new(-19, 21) };
        g.FillPolygon(cloak, mantle); g.DrawPolygon(cloakEdge, mantle);
        g.FillEllipse(hood, -12, -27, 24, 23); g.FillEllipse(face, -6, -22, 12, 12);
        g.DrawLine(line, 5, 1, attackReach, 14);
        g.FillEllipse(brass, 13, -3, 12, 12); g.FillEllipse(hood, 16, 0, 6, 6);
        if (attack)
        {
            int alpha = frame is 1 or 2 ? 225 : 130;
            using var slashGlow = new Pen(Color.FromArgb(alpha / 3, 125, 213, 199), 10);
            using var slash = new Pen(Color.FromArgb(alpha, 179, 241, 219), 3.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            float start = frame switch { 0 => -160, 1 => -125, 2 => -90, _ => -55 };
            g.DrawArc(slashGlow, 6, -27, 48, 48, start, 90); g.DrawArc(slash, 6, -27, 48, 48, start, 90);
        }
        g.Restore(state);
    }
    private void DrawRefrainWave(Graphics g) { float progress = 1f - refrainWaveTime / 0.48f; float radius = refrainWaveRadius * progress; int alpha = (int)(220 * refrainWaveTime / 0.48f); using var wave = new Pen(Color.FromArgb(alpha, 207, 147, 227), 4); using var glow = new Pen(Color.FromArgb(alpha / 3, 207, 147, 227), 14); g.DrawEllipse(glow, player.X - radius, player.Y - radius, radius * 2, radius * 2); g.DrawEllipse(wave, player.X - radius, player.Y - radius, radius * 2, radius * 2); }
    private void DrawReturnedVoicesWave(Graphics g) { float progress = 1f - returnVoiceWaveTime / 0.46f; float radius = returnVoiceWaveRadius * progress; int alpha = (int)(225 * returnVoiceWaveTime / 0.46f); using var wave = new Pen(Color.FromArgb(alpha, 205, 221, 165), 4); using var glow = new Pen(Color.FromArgb(alpha / 3, 205, 221, 165), 15); g.DrawEllipse(glow, player.X - radius, player.Y - radius, radius * 2, radius * 2); g.DrawEllipse(wave, player.X - radius, player.Y - radius, radius * 2, radius * 2); }
    private void DrawFinalSparkFlash(Graphics g) { using var spark = new Pen(Color.FromArgb((int)(220 * finalStrikeFlash / 0.24f), 255, 225, 145), 7); using var glow = new Pen(Color.FromArgb((int)(85 * finalStrikeFlash / 0.24f), 255, 177, 98), 18); PointF start = Add(player, Scale(facing, 20)); g.DrawLine(glow, start, finalStrikeTarget); g.DrawLine(spark, start, finalStrikeTarget); }
    private void DrawNameRiftBursts(Graphics g)
    {
        foreach (NameRiftBurst burst in nameRiftBursts)
        {
            float progress = 1f - burst.TimeLeft / 0.3f, radius = 108f * progress;
            int alpha = Math.Clamp((int)(225 * (1f - progress)), 0, 225);
            using var outer = new Pen(Color.FromArgb(alpha, 219, 151, 242), 3.5f);
            using var inner = new Pen(Color.FromArgb(alpha / 2, 239, 211, 248), 1.5f);
            using var core = new SolidBrush(Color.FromArgb(alpha / 3, 220, 151, 244));
            g.DrawEllipse(outer, burst.Pos.X - radius, burst.Pos.Y - radius * 0.72f, radius * 2, radius * 1.44f);
            g.DrawEllipse(inner, burst.Pos.X - radius * 0.72f, burst.Pos.Y - radius * 0.52f, radius * 1.44f, radius * 1.04f);
            g.FillEllipse(core, burst.Pos.X - 12, burst.Pos.Y - 8, 24, 16);
            for (int ray = 0; ray < 8; ray++)
            {
                float angle = (ray * 45 + 22) * MathF.PI / 180f;
                PointF start = new(burst.Pos.X + MathF.Cos(angle) * radius * 0.48f, burst.Pos.Y + MathF.Sin(angle) * radius * 0.35f);
                PointF end = new(burst.Pos.X + MathF.Cos(angle) * radius * 0.78f, burst.Pos.Y + MathF.Sin(angle) * radius * 0.58f);
                g.DrawLine(inner, start, end);
            }
        }
    }

    private void DrawBeacon(Graphics g)
    {
        int alpha = Math.Clamp((int)(165 + 45 * MathF.Sin(windupPulse * 6)), 70, 220);
        using var glow = new SolidBrush(Color.FromArgb(42, 173, 226, 224)); using var core = new SolidBrush(Color.FromArgb(alpha, 167, 221, 218));
        using var ring = new Pen(Color.FromArgb(alpha, 212, 235, 207), 2); using var ray = new Pen(Color.FromArgb(alpha, 201, 224, 211), 3);
        g.FillEllipse(glow, beaconPos.X - 29, beaconPos.Y - 29, 58, 58); g.DrawEllipse(ring, beaconPos.X - 19, beaconPos.Y - 19, 38, 38);
        g.FillPolygon(core, new[] { new PointF(beaconPos.X, beaconPos.Y - 15), new PointF(beaconPos.X + 9, beaconPos.Y), new PointF(beaconPos.X, beaconPos.Y + 15), new PointF(beaconPos.X - 9, beaconPos.Y) });
        g.DrawLine(ray, beaconPos.X, beaconPos.Y - 29, beaconPos.X, beaconPos.Y - 20); g.DrawLine(ray, beaconPos.X + 20, beaconPos.Y, beaconPos.X + 29, beaconPos.Y); g.DrawLine(ray, beaconPos.X, beaconPos.Y + 20, beaconPos.X, beaconPos.Y + 29); g.DrawLine(ray, beaconPos.X - 29, beaconPos.Y, beaconPos.X - 20, beaconPos.Y);
        if (beaconWaveTime > 0) { float progress = 1 - beaconWaveTime / 0.38f, radius = 128 * progress; int waveAlpha = (int)(210 * beaconWaveTime / 0.38f); using var wave = new Pen(Color.FromArgb(waveAlpha, 182, 238, 220), 3); g.DrawEllipse(wave, beaconPos.X - radius, beaconPos.Y - radius, radius * 2, radius * 2); }
    }
    private static void DrawEchoShard(Graphics g, EchoShard shard)
    {
        PointF tail = Sub(shard.Pos, Scale(shard.Direction, 24));
        using var trail = new Pen(Color.FromArgb(150, 73, 193, 183), 6); using var core = new Pen(Color.FromArgb(235, 211, 225, 180), 2);
        using var glow = new SolidBrush(Color.FromArgb(82, 91, 225, 211)); using var crystal = new SolidBrush(Color.FromArgb(235, 115, 211, 195));
        g.DrawLine(trail, tail, shard.Pos); g.FillEllipse(glow, shard.Pos.X - 12, shard.Pos.Y - 12, 24, 24);
        PointF[] points = { Add(shard.Pos, Scale(shard.Direction, 10)), Add(shard.Pos, new PointF(-shard.Direction.Y * 5, shard.Direction.X * 5)), Add(shard.Pos, Scale(shard.Direction, -8)), Add(shard.Pos, new PointF(shard.Direction.Y * 5, -shard.Direction.X * 5)) };
        g.FillPolygon(crystal, points); g.DrawPolygon(core, points);
    }
    private static void DrawBowArrow(Graphics g, BowArrow arrow)
    {
        PointF tail = Sub(arrow.Pos, Scale(arrow.Direction, 16)), perpendicular = new(-arrow.Direction.Y, arrow.Direction.X), basePoint = Sub(arrow.Pos, Scale(arrow.Direction, 5));
        Color color = arrow.Critical ? Color.FromArgb(255, 227, 143) : Color.FromArgb(126, 220, 235);
        using var glow = new Pen(Color.FromArgb(90, color), 7) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var shaft = new Pen(Color.FromArgb(240, color), 2) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(glow, tail, arrow.Pos); g.DrawLine(shaft, tail, arrow.Pos);
        g.DrawLine(shaft, Add(basePoint, Scale(perpendicular, 4)), arrow.Pos); g.DrawLine(shaft, Add(basePoint, Scale(perpendicular, -4)), arrow.Pos);
    }
    private string ShortSkillName(int slot) => ShortSkillName(equippedSkills, slot);
    private string ShortSkillName(int[] loadout, int slot) => loadout[slot] == 0 ? "VAZIO" : loadout[slot] == 7 ? "DEVOLUÇÃO" : loadout[slot] == 8 ? "CENTELHA" : skillNames[loadout[slot]].ToUpperInvariant();
    private void DrawSkillMenu(Graphics g)
    {
        using var shade = new SolidBrush(Color.FromArgb(185, 5, 7, 9)); using var panelBrush = new SolidBrush(Color.FromArgb(248, 17, 19, 20)); using var edge = new Pen(Color.FromArgb(175, 143, 112, 73), 2);
        using var heading = new Font("Georgia", 17, FontStyle.Bold); using var body = new Font("Segoe UI", 11); using var small = new Font("Segoe UI", 9);
        g.FillRectangle(shade, 0, 83, 960, 557); g.FillRectangle(panelBrush, 105, 132, 750, 475); g.DrawRectangle(edge, 105, 132, 750, 475);
        DrawCentered(g, skillMenuAlternate ? "MONTAGEM ALTERNATIVA" : "MONTAGEM PRINCIPAL", 153, heading, Color.FromArgb(230, 193, 129));
        g.DrawString("TAB alterna a montagem; T ativa a alternativa. F/V/G/C escolhem espaços; 1–0, F11 e F12 equipam técnicas.", small, Brushes.DarkGray, 140, 188);
        g.DrawString("ESPAÇOS DA MONTAGEM", body, Brushes.Goldenrod, 150, 224);
        string[] slotKeys = { "F", "V", "G", "C" };
        int[] menuSkills = SkillMenuLoadout();
        for (int slot = 0; slot < 4; slot++)
        {
            bool selected = slot == skillMenuSlot; Brush slotBrush = selected ? Brushes.Goldenrod : Brushes.AntiqueWhite;
            g.DrawString($"{slotKeys[slot]}  •  {ShortSkillName(menuSkills, slot)}{(selected ? "  ◀" : "")}", body, slotBrush, 160, 258 + slot * 32);
        }
        int selectedSkill = menuSkills[skillMenuSlot];
        int selectedVariant = selectedSkill is >= 1 and <= 12 ? skillVariants[selectedSkill] : 0;
        g.DrawString(selectedSkill == 0 ? "MEMÓRIA DA TÉCNICA: ESPAÇO VAZIO" : selectedSkill >= 9 ? $"FORMA: {SkillVariantName(selectedSkill, selectedVariant).ToUpperInvariant()}" : $"MEMÓRIA: {SkillVariantName(selectedSkill, selectedVariant)}", body, Brushes.Goldenrod, new RectangleF(150, 402, 310, 24));
        g.DrawString(selectedSkill == 0 ? "Equipe uma técnica e use ↑/↓ para mudar sua forma." : SkillVariantDescription(selectedSkill, selectedVariant), small, Brushes.DarkGray, new RectangleF(150, 430, 310, 36));
        g.DrawString("TÉCNICAS APRENDIDAS", body, Brushes.Goldenrod, 480, 224);
        for (int skill = 1; skill <= 12; skill++)
        {
            bool unlocked = SkillUnlocked(skill); bool equippedElsewhere = menuSkills.Where((_, index) => index != skillMenuSlot).Contains(skill);
            string state = !unlocked ? "  •  NÃO APRENDIDA" : equippedElsewhere ? "  •  EM OUTRO ESPAÇO" : menuSkills[skillMenuSlot] == skill ? "  •  EQUIPADA" : "";
            int rowY = 258 + (skill - 1) * 27;
            g.DrawString($"{skill}. {skillNames[skill].ToUpperInvariant()}{state}", body, unlocked ? Brushes.AntiqueWhite : Brushes.Gray, 480, rowY);
            g.DrawString(skillDescriptions[skill], small, Brushes.DarkGray, 503, rowY + 16);
        }
        g.DrawString("SINTONIAS DE TÉCNICAS", small, Brushes.Goldenrod, 150, 472);
        g.DrawString("1+2 • Sino prepara alvos; Estilhaço causa +35% e consome a marca", small, HasSkillPair(menuSkills, 1, 2) ? Brushes.LightGoldenrodYellow : Brushes.DarkGray, new RectangleF(150, 488, 310, 16));
        g.DrawString("3+6 • Vazante prepara alvos; Farol causa +35% no próximo pulso", small, HasSkillPair(menuSkills, 3, 6) ? Brushes.LightCyan : Brushes.DarkGray, new RectangleF(150, 504, 310, 16));
        g.DrawString("2+5 • Sino prepara alvos; Coro causa +35% e consome a marca", small, HasSkillPair(menuSkills, 2, 5) ? Brushes.LightGoldenrodYellow : Brushes.DarkGray, new RectangleF(150, 520, 310, 16));
        Brush wardSynergyBrush = HasSkillPair(menuSkills, 4, 7) ? skillMenuAlternate == alternateSkillsActive && wardTime > 0 ? Brushes.LightGreen : Brushes.Goldenrod : Brushes.DarkGray;
        g.DrawString("4+7 • Manto ativo amplia em 35% a cura da Devolução", small, wardSynergyBrush, new RectangleF(150, 536, 310, 16));
        g.DrawString("5+6 • Coro acerta ecos no Farol ativo; próximo pulso adianta 0,4 s", small, HasSkillPair(menuSkills, 5, 6) ? Brushes.LightCyan : Brushes.DarkGray, new RectangleF(150, 552, 385, 16));
        g.DrawString("1+8 Estilhaço / 8+12 Chamado • Centelha +35%", small, HasSkillPair(menuSkills, 1, 8) || HasSkillPair(menuSkills, 8, 12) ? Brushes.LightCyan : Brushes.DarkGray, new RectangleF(150, 568, 310, 16));
        g.DrawString("↑/↓ altera a memória da técnica equipada • 1–0/F11/F12 equipa • BACKSPACE esvazia • B/ESC fecha", small, Brushes.DarkGray, new RectangleF(150, 587, 660, 12));
    }

    private void DrawChoice(Graphics g, string titleText, string a, string b, string c)
    {
        using var bg = new SolidBrush(Color.FromArgb(248, 15, 17, 18)); using var optionFont = new Font("Segoe UI", 12); g.FillRectangle(bg, 170, 205, 620, 225);
        DrawCentered(g, titleText, 238, new Font("Georgia", 15, FontStyle.Bold), Color.Goldenrod);
        g.DrawString(a, optionFont, Brushes.AntiqueWhite, 220, 286); g.DrawString(b, optionFont, Brushes.AntiqueWhite, 220, 330); g.DrawString(c, optionFont, Brushes.AntiqueWhite, 220, 374);
    }

    private void DrawTalentChoice(Graphics g, int pending)
    {
        using var background = new SolidBrush(Color.FromArgb(248, 15, 17, 18));
        using var optionFont = new Font("Segoe UI", 12);
        using var hintBrush = new SolidBrush(Color.FromArgb(165, 174, 168));
        g.FillRectangle(background, 170, level >= 30 ? 140 : 155, 620, level >= 30 ? 490 : level >= 25 ? 465 : 420);
        DrawCentered(g, $"SUBIU DE NÍVEL — ESCOLHA ({pending} PENDENTES)", 183, new Font("Georgia", 15, FontStyle.Bold), Color.Goldenrod);
        g.DrawString("1. VIGOR  +20 VIDA MÁXIMA", optionFont, Brushes.AntiqueWhite, 220, 225);
        g.DrawString("2. DOMÍNIO  técnicas recarregam mais rápido", optionFont, Brushes.AntiqueWhite, 220, 260);
        g.DrawString("3. AGILIDADE  esquiva recarrega mais rápido", optionFont, Brushes.AntiqueWhite, 220, 295);
        g.DrawString("4. SINTONIA  +8% de dano de técnica por ponto", optionFont, Brushes.AntiqueWhite, 220, 330);
        if (level < 5) g.DrawString("Especializações 5 e 6 desbloqueiam no nível 5.", optionFont, hintBrush, 220, 374);
        else if (combatDiscipline == 0)
        {
            g.DrawString("5. FORMA DO GUME  +12% básicos • -10% técnicas", optionFont, Brushes.Goldenrod, 220, 374);
            g.DrawString("6. FORMA DO ECO  +12% técnicas • -10% básicos", optionFont, Brushes.Plum, 220, 408);
        }
        else g.DrawString($"Especialização: {(combatDiscipline == 1 ? "Forma do Gume" : "Forma do Eco")}", optionFont, hintBrush, 220, 374);

        if (level >= 10 && combatDiscipline > 0 && combatMastery == 0)
        {
            g.DrawString("5. GUARDA SERENA  -10% dano recebido • esquiva +0,20 s", optionFont, Brushes.Goldenrod, 220, 408);
            g.DrawString("6. PASSO IMPETUOSO  -0,18 s esquiva • +8% dano recebido", optionFont, Brushes.Plum, 220, 442);
        }
        else if (combatMastery > 0) g.DrawString($"Maestria: {(combatMastery == 1 ? "Guarda Serena" : "Passo Impetuoso")}", optionFont, hintBrush, 220, 408);
        else if (combatDiscipline > 0) g.DrawString("Maestria desbloqueia no nível 10.", optionFont, hintBrush, 220, 408);

        if (level >= 15 && combatDiscipline > 0 && combatMastery > 0 && combatFocus == 0)
        {
            g.DrawString("5. CAÇADOR DE ECOS  +15% dano contra chefes", optionFont, Brushes.Goldenrod, 220, 442);
            g.DrawString("6. QUEBRA-FORMAÇÃO  +10% contra comuns com 3+ inimigos", optionFont, Brushes.Plum, 220, 476);
        }
        else if (combatFocus > 0) g.DrawString($"Foco: {(combatFocus == 1 ? "Caçador de Ecos" : "Quebra-Formação")} • redistribuível na forja.", optionFont, hintBrush, 220, 442);
        else if (combatDiscipline > 0 && combatMastery > 0) g.DrawString("Foco de combate desbloqueia no nível 15.", optionFont, hintBrush, 220, 442);

        if (level >= 20 && combatDiscipline > 0 && combatMastery > 0 && combatFocus > 0 && combatApex == 0)
        {
            g.DrawString("5. CORO SUSPENSO  +18% técnicas com 2+ recargas ativas", optionFont, Brushes.Goldenrod, 220, 476);
            g.DrawString("6. ÂNCORA DE ECO  -12% dano recebido com 2+ recargas", optionFont, Brushes.Plum, 220, 510);
        }
        else if (combatApex > 0) g.DrawString($"Auge: {(combatApex == 1 ? "Coro Suspenso" : "Âncora de Eco")} • redistribuível na forja.", optionFont, hintBrush, 220, 476);

        if (level >= 25 && combatApex > 0 && combatTempo == 0)
        {
            g.DrawString("5. ABERTURA DA VIGÍLIA  técnicas +15% se todas prontas", optionFont, Brushes.Goldenrod, 220, 510);
            g.DrawString("6. RESPOSTA DO VEIO  básicos +12% com 2+ recargas", optionFont, Brushes.Plum, 220, 544);
        }
        else if (combatTempo > 0) g.DrawString($"Ritmo: {(combatTempo == 1 ? "Abertura da Vigília" : "Resposta do Veio")} • redistribuível na forja.", optionFont, hintBrush, 220, 510);
        else if (combatApex > 0) g.DrawString("Ritmo de combate desbloqueia no nível 25.", optionFont, hintBrush, 220, 510);
        if (level >= 30 && combatTempo > 0 && combatInstinct == 0)
        {
            g.DrawString("5. PRIMEIRA RESSONÂNCIA  +18% contra alvos com 80%+ de vida", optionFont, Brushes.Goldenrod, 220, 578);
            g.DrawString("6. ÚLTIMO ECO  +18% contra alvos com 30% ou menos de vida", optionFont, Brushes.Plum, 220, 612);
        }
        else if (combatInstinct > 0) g.DrawString($"Instinto: {(combatInstinct == 1 ? "Primeira Ressonância" : "Último Eco")} • redistribuível na forja.", optionFont, hintBrush, 220, 578);
        else if (combatTempo > 0) g.DrawString("Instinto de combate desbloqueia no nível 30.", optionFont, hintBrush, 220, 552);
        if (level < 30) g.DrawString("Talentos comuns podem se repetir; cada marco escolhe um caminho.", optionFont, hintBrush, 220, level >= 25 ? 580 : level >= 20 ? 560 : 520);
    }
    private void DrawStoryOverlay(Graphics g)
    {
        DrawReadingOverlay(g, $"MEMÓRIA NA PEDRA   •   {storyPage + 1} / {prologue.Length}", ProloguePage(storyPage), "ENTER, ESPAÇO OU CLIQUE  •  CONTINUAR", FontStyle.Regular);
    }
    private void DrawReadingOverlay(Graphics g, string heading, string body, string action, FontStyle bodyStyle)
    {
        using var shade = new SolidBrush(Color.FromArgb(145, 5, 7, 9));
        using var panelBrush = new SolidBrush(Color.FromArgb(246, 19, 21, 22));
        using var border = new Pen(Color.FromArgb(175, 143, 112, 73), 2);
        using var headingFont = new Font("Georgia", 13, FontStyle.Bold);
        using var bodyFont = new Font("Segoe UI", 14, bodyStyle);
        using var hintFont = new Font("Segoe UI", 9, FontStyle.Bold);
        using var headingBrush = new SolidBrush(Color.FromArgb(230, 193, 129));
        using var bodyBrush = new SolidBrush(Color.AntiqueWhite);
        using var hintBrush = new SolidBrush(Color.FromArgb(170, 174, 168));
        ReadingOverlayLayout layout = MeasureReadingOverlay(g, body, headingFont, bodyFont, hintFont);
        g.FillRectangle(shade, 0, 0, 960, 640);
        g.FillRectangle(panelBrush, 86, layout.PanelY, 788, layout.PanelHeight);
        g.DrawRectangle(border, 86, layout.PanelY, 788, layout.PanelHeight);
        g.DrawString(heading, headingFont, headingBrush, 112, layout.HeadingY);
        g.DrawString(body, bodyFont, bodyBrush, new RectangleF(112, layout.BodyY, 736, layout.BodyHeight));
        g.DrawString(action, hintFont, hintBrush, 112, layout.HintY);
    }
    private static ReadingOverlayLayout MeasureReadingOverlay(Graphics g, string body, Font headingFont, Font bodyFont, Font hintFont)
    {
        float bodyHeight = Math.Max(bodyFont.GetHeight(g), g.MeasureString(body, bodyFont, new SizeF(736, 360)).Height + 2);
        float panelHeight = Math.Max(154, 20 + headingFont.GetHeight(g) + 8 + bodyHeight + 10 + hintFont.GetHeight(g) + 16);
        float panelY = 574 - panelHeight;
        float headingY = panelY + 20;
        float bodyY = headingY + headingFont.GetHeight(g) + 8;
        float hintY = bodyY + bodyHeight + 10;
        return new ReadingOverlayLayout(panelY, panelHeight, headingY, bodyY, bodyHeight, hintY);
    }
    private void DrawMemoryOverlay(Graphics g)
    {
        DrawReadingOverlay(g, memoryTitle, memoryText, "ENTER, ESPAÇO OU CLIQUE  •  FECHAR", FontStyle.Italic);
    }
    private static void DrawMemoryStone(Graphics g, PointF pos, bool nearby)
    {
        float pulse = nearby ? 17 : 14;
        PointF[] shard = { new(pos.X, pos.Y - pulse), new(pos.X + pulse * .7f, pos.Y), new(pos.X, pos.Y + pulse), new(pos.X - pulse * .7f, pos.Y) };
        using var glow = new SolidBrush(Color.FromArgb(nearby ? 100 : 55, 119, 186, 188));
        using var stone = new SolidBrush(Color.FromArgb(nearby ? 245 : 210, 120, 179, 181));
        using var edge = new Pen(Color.FromArgb(235, 207, 224, 193), 2);
        g.FillEllipse(glow, pos.X - 24, pos.Y - 24, 48, 48);
        g.FillPolygon(stone, shard); g.DrawPolygon(edge, shard);
        if (nearby) DrawCentered(g, "E  ESCUTAR", pos.Y + 23, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, pos.X);
    }
    private void DrawOverlay(Graphics g, string titleText, string instruction) { using var bg = new SolidBrush(Color.FromArgb(225, 8, 10, 12)); g.FillRectangle(bg, 0, 0, 960, 640); DrawCentered(g, titleText, 250, new Font("Georgia", 36, FontStyle.Bold), Color.FromArgb(230, 193, 129)); DrawCentered(g, instruction, 340, new Font("Segoe UI", 15, FontStyle.Bold), Color.AntiqueWhite); if (noticeTime > 0) DrawCentered(g, notice, 410, new Font("Segoe UI", 12), Color.Goldenrod); }
    private static void DrawCentered(Graphics g, string s, float y, Font f, Color color, float x = 480) { using (f) using (var brush = new SolidBrush(color)) { SizeF size = g.MeasureString(s, f); g.DrawString(s, f, brush, x - size.Width / 2, y); } }
    private static int EnemySpriteRow(EnemyKind kind) => kind switch { EnemyKind.Grunt => 0, EnemyKind.Archer => 1, EnemyKind.Mage => 2, EnemyKind.Elite => 3, _ => -1 };
    private static int CampaignBossSpriteIndex(EnemyKind kind) => kind switch { EnemyKind.Guardian => 0, EnemyKind.BlindScribe => 1, EnemyKind.RiverFerryman => 2, EnemyKind.AshWarden => 3, EnemyKind.HollowName => 4, EnemyKind.NameWeaver => 5, EnemyKind.FirstKeeper => 6, EnemyKind.OathCartographer => 7, EnemyKind.NameBroker => 8, EnemyKind.TideWitness => 9, EnemyKind.ReedKeeper => 10, EnemyKind.FlowKeeper => 11, EnemyKind.CounterCurrent => 12, EnemyKind.RiftCantor => 13, EnemyKind.GlassHerald => 14, EnemyKind.LanternRegent => 15, EnemyKind.RouteScribe => 16, EnemyKind.DeepSilence => 17, _ => -1 };
    private static int EnemySpriteFrame(float animationTime, bool walking) => walking ? (int)(Math.Max(0, animationTime) / 0.13f) % 4 : 0;
    private static int EnemyAttackSpriteFrame(Enemy enemy) => enemy.Windup > 0 ? Math.Min(3, (int)(Math.Clamp(1f - enemy.Windup / Math.Max(0.01f, enemy.WindupMax), 0f, 1f) * 4f)) : enemy.AttackPoseTime > 0 ? 3 : -1;
    private static (float Width, float Height, float OffsetX, float OffsetY) EnemyAttackPose(Enemy enemy, float progress)
    {
        float p = Math.Clamp(progress, 0f, 1f);
        return enemy.Kind switch
        {
            EnemyKind.DrownedMemory => (1 + 0.16f * p, 1 + 0.05f * p, 0, -p),
            EnemyKind.EmberWisp => (1 + 0.22f * p, 1 + 0.25f * p, 0, -5 * p),
            EnemyKind.NameMoth => (1 + 0.28f * p, 1 + 0.02f * p, 0, 0),
            EnemyKind.MudCantor => (1 + 0.12f * p, 1 + 0.20f * p, 0, 2 * p),
            EnemyKind.PaperWraith => (1 + 0.05f * p, 1 + 0.05f * p, MathF.Sin(enemy.AnimationTime * 18f) * 6f * p, -2 * p),
            EnemyKind.SaltEcho => (1 - 0.08f * p, 1 + 0.18f * p, 0, -3 * p),
            EnemyKind.GlassHerald when enemy.BossPattern == 2 => (1 + 0.19f * p, 1 - 0.07f * p, 0, -3 * p),
            EnemyKind.RouteScribe when enemy.BossPattern == 0 => (1 + 0.12f * p, 1 + 0.16f * p, 0, -4 * p),
            EnemyKind.LastTuner when enemy.BossPattern == 0 => (1 + 0.22f * p, 1 + 0.11f * p, 0, -5 * p),
            EnemyKind.DeepSilence when enemy.BossPattern == 2 => (1 + 0.04f * p, 1 + 0.21f * p, 0, -4 * p),
            _ => (1, 1, 0, 0)
        };
    }
    private static bool EnemyFacesLeft(float enemyX, float playerX) => playerX < enemyX;
    private static string EnemyName(Enemy e) => e.NameOverride ?? (e.VigilMinion ? "ECO ERRANTE • SALINO" : e.Kind == EnemyKind.Elite ? $"ELITE • {EliteAffixName(e)}" : EnemyData(e.Kind).Name);
    private static PointF Normalize(PointF p, PointF fallback) { float n = Length(p); return n > 0.001f ? new PointF(p.X / n, p.Y / n) : fallback; }
    private static float Length(PointF p) => MathF.Sqrt(p.X * p.X + p.Y * p.Y);
    private static float Distance(PointF a, PointF b) => Length(Sub(a, b));
    private static float DistanceToSegment(PointF point, PointF start, PointF end) { PointF line = Sub(end, start); float lengthSquared = Dot(line, line); float t = lengthSquared > 0.001f ? Math.Clamp(Dot(Sub(point, start), line) / lengthSquared, 0, 1) : 0; return Distance(point, Add(start, Scale(line, t))); }
    private static PointF Sub(PointF a, PointF b) => new(a.X - b.X, a.Y - b.Y);
    private static PointF Offset(PointF a, float x, float y) => new(a.X + x, a.Y + y);
    private static PointF Add(PointF a, PointF b) => new(a.X + b.X, a.Y + b.Y);
    private static PointF Scale(PointF a, float m) => new(a.X * m, a.Y * m);
    private static float Dot(PointF a, PointF b) => a.X * b.X + a.Y * b.Y;
}
