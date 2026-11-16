using System.Drawing;

public sealed partial class Game
{
    private void DrawQuestJournal(Graphics g)
    {
        using var shade = new SolidBrush(Color.FromArgb(232, 5, 7, 9));
        using var panel = new SolidBrush(Color.FromArgb(248, 17, 20, 21));
        using var edge = new Pen(Color.FromArgb(184, 143, 112, 73), 2);
        using var column = new SolidBrush(Color.FromArgb(230, 23, 27, 27));
        using var divider = new Pen(Color.FromArgb(93, 91, 73), 1);
        g.FillRectangle(shade, 0, 0, 960, 640);
        g.FillRectangle(panel, 58, 86, 844, 490); g.DrawRectangle(edge, 58, 86, 844, 490);
        DrawCentered(g, "DIÁRIO DA JORNADA", 96, new Font("Georgia", 20, FontStyle.Bold), Color.FromArgb(230, 193, 129));
        int chapterIndex = Array.FindIndex(campaignChapters, chapter => room >= chapter.FirstRoom && room <= chapter.LastRoom);
        chapterIndex = Math.Max(0, chapterIndex);
        CampaignChapter currentChapter = campaignChapters[chapterIndex];
        SaveData progress = new() { Room = room, QuestStage = questStage, ClearedRooms = clearedRooms, RoomCleared = (clearedRooms & (1L << room)) != 0 };
        long known = PreviewKnownRooms(progress);
        using var chapterFont = new Font("Segoe UI", 8, FontStyle.Bold);
        using var sectionFont = new Font("Segoe UI", 9, FontStyle.Bold);
        using var detailFont = new Font("Segoe UI", 10);
        using var metaFont = new Font("Segoe UI", 9);
        using var resonanceFont = new Font("Segoe UI", 9, FontStyle.Bold);
        using var chapterFormat = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
        for (int i = 0; i < campaignChapters.Length; i++)
        {
            CampaignChapter chapter = campaignChapters[i]; float y = 141 + i * 20;
            bool selected = i == chapterIndex;
            string state = CampaignChapterState(chapter, progress, known, clearedRooms);
            using var fill = new SolidBrush(selected ? Color.FromArgb(91, 69, 38) : Color.FromArgb(29, 33, 32));
            using var line = new Pen(selected ? Color.FromArgb(224, 181, 104) : Color.FromArgb(58, 62, 57));
            using var ink = new SolidBrush(state switch { "CONCLUÍDO" => Color.FromArgb(145, 206, 153), "ATUAL" or "INÍCIO" => Color.FromArgb(255, 224, 161), "BLOQUEADO" => Color.FromArgb(111, 115, 106), _ => Color.FromArgb(204, 204, 188) });
            g.FillRectangle(fill, 76, y, 347, 19); g.DrawRectangle(line, 76, y, 347, 19);
            g.DrawString($"{ToRoman(chapter.Number)}  {chapter.Title}", chapterFont, selected ? Brushes.Goldenrod : Brushes.AntiqueWhite, new RectangleF(82, y + 1, 270, 17), chapterFormat);
            using var stateFont = new Font("Segoe UI", 6.5f, FontStyle.Bold);
            g.DrawString(state, stateFont, ink, new RectangleF(352, y + 1, 66, 17), chapterFormat);
        }

        g.FillRectangle(column, 440, 141, 440, 384); g.DrawRectangle(divider, 440, 141, 440, 384);
        bool firstEchoArea = room >= 43;
        DrawCentered(g, firstEchoArea ? "EXPEDIÇÃO OPCIONAL" : $"CAPÍTULO {ToRoman(currentChapter.Number)}", 159, new Font("Georgia", 16, FontStyle.Bold), Color.FromArgb(230, 193, 129), 660);
        DrawCentered(g, firstEchoArea ? "MARGENS DO PRIMEIRO ECO" : currentChapter.Title, 190, new Font("Segoe UI", 10, FontStyle.Bold), Color.FromArgb(224, 220, 203), 660);
        DrawCentered(g, vigilActive ? $"VIGÍLIA ASCENDENTE • ANDAR {vigilFloor}" : roomNames[room], 221, new Font("Segoe UI", 9, FontStyle.Bold), Color.FromArgb(174, 183, 169), 660);
        g.DrawLine(divider, 464, 247, 856, 247);
        g.DrawString("OBJETIVO ATUAL", sectionFont, Brushes.Goldenrod, 466, 263);
        string objective = CurrentObjective();
        if (objective.Length == 0) objective = enemies.Count > 0 || drops.Count > 0 ? "Derrote os ecos e recolha o saque para abrir a passagem." : "Explore esta área e siga pela saída para continuar a jornada.";
        using var objectiveFont = new Font("Segoe UI", 13, FontStyle.Bold);
        using var objectiveBrush = new SolidBrush(Color.FromArgb(237, 232, 213));
        g.DrawString(objective, objectiveFont, objectiveBrush, new RectangleF(466, 285, 388, 68));

        int totalCombatRooms = Enumerable.Range(0, 43).Count(index => index is not 3 and not 18);
        int clearedCombatRooms = Enumerable.Range(0, 43).Count(index => index is not 3 and not 18 && (clearedRooms & (1L << index)) != 0);
        int inscriptionCount = (memoriesFound & 1) + ((memoriesFound >> 1) & 1);
        g.DrawLine(divider, 464, 367, 856, 367);
        g.DrawString(firstEchoArea ? "CAMINHOS DAS MARGENS" : "PROGRESSO DA CAMPANHA", sectionFont, Brushes.Goldenrod, 466, 381);
        g.DrawString(firstEchoArea ? (firstEchoQuest == 3 ? "Expedição concluída com Edda" : firstEchoQuest == 2 ? "Vozes livres: relate a Edda" : "Comportas: vozes retidas") : $"Câmaras limpas: {clearedCombatRooms}/{totalCombatRooms}", detailFont, Brushes.AntiqueWhite, 466, 409);
        int fieldDiscoveries = (firstEchoCaveFound ? 1 : 0) + (firstEchoReedCache ? 1 : 0) + (firstEchoBeaconRestored ? 1 : 0) + (firstEchoFerryRestored ? 1 : 0);
        g.DrawString(firstEchoArea ? $"Descobertas opcionais: {fieldDiscoveries}/4" : $"Inscrições opcionais: {inscriptionCount}/2", detailFont, Brushes.AntiqueWhite, 466, 435);
        g.DrawString($"Ecos nesta área: {enemies.Count}   •   Saques no chão: {drops.Count}", metaFont, Brushes.LightGray, 466, 461);
        if (room == 3 && firstEchoQuest > 0)
            g.DrawString(firstEchoQuest == 1 ? "Margens: siga às comportas" : firstEchoQuest == 2 ? "Margens: relate a Edda" : firstEchoFerryAccepted && !firstEchoFerryRestored ? "Vau: reacenda o sinalizador" : firstEchoBeaconAccepted && !firstEchoBeaconRestored ? "Margens: restaure o marco da escarpa" : firstEchoBeaconRestored && !firstEchoFerryAccepted ? "Vau de Cinza: procure Nilo" : "Expedição das Margens concluída", resonanceFont, Brushes.Goldenrod, 466, 489);
        else
            g.DrawString($"Ressonância {difficultyTier} • {DifficultyTierName(difficultyTier)}", resonanceFont, Brushes.Orange, 466, 489);
        DrawCentered(g, "L / ESC FECHA • CLIQUE FECHA • M ABRE O MAPA", 546, new Font("Segoe UI", 9, FontStyle.Bold), Color.FromArgb(167, 173, 163));
    }
}
