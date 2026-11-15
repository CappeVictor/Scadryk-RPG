using System.Drawing;
using System.Text.Json;
using System.Windows.Forms;

public sealed partial class Game
{
    private SaveData? TitleSavePreview()
    {
        if (titleSavePreviewRead) return titleSavePreview;
        titleSavePreviewRead = true;
        try
        {
            if (!File.Exists(SavePath)) return null;
            SaveData? save = JsonSerializer.Deserialize<SaveData>(File.ReadAllText(SavePath));
            if (save is not null && ValidSaveData(save)) titleSavePreview = save;
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        catch (System.Text.Json.JsonException) { }
        return titleSavePreview;
    }

    private void HandleTitleKey(Keys key)
    {
        if (classSelectionOpen)
        {
            if (key is Keys.Escape or Keys.Back) classSelectionOpen = false;
            else if (key is Keys.Up or Keys.W or Keys.Down or Keys.S) classSelection = (classSelection + 1) % 3;
            else if (key == Keys.D1) ChooseHeroClass(0);
            else if (key == Keys.D2) ChooseHeroClass(1);
            else if (key == Keys.D3) ChooseHeroClass(2);
            else if (key is Keys.Enter or Keys.Space) ChooseHeroClass(classSelection);
            Invalidate(); return;
        }
        if (confirmNewJourney)
        {
            if (key is Keys.Y or Keys.Enter) BeginClassSelection();
            else if (key is Keys.N or Keys.Escape) confirmNewJourney = false;
            Invalidate(); return;
        }
        if (titlePage == TitlePage.Main)
        {
            if (key is Keys.Up or Keys.W) titleSelection = (titleSelection + 5) % 6;
            else if (key is Keys.Down or Keys.S) titleSelection = (titleSelection + 1) % 6;
            else if (key is >= Keys.D1 and <= Keys.D6) { titleSelection = (int)key - (int)Keys.D1; ActivateTitleSelection(); }
            else if (key == Keys.C) { if (HasTitleSave) LoadGame(); else notice = "AINDA NÃO HÁ UMA JORNADA SALVA"; }
            else if (key is Keys.Enter or Keys.Space) ActivateTitleSelection();
            Invalidate(); return;
        }
        if (key == Keys.Escape || key == Keys.Back)
        {
            titlePage = TitlePage.Main; titleSelection = 2; Invalidate(); return;
        }
        if (titlePage == TitlePage.Campaign && (key is Keys.Enter or Keys.Space))
        {
            if (HasTitleSave) LoadGame(); else BeginClassSelection();
            return;
        }
        if (titlePage == TitlePage.Campaign && (key is Keys.Up or Keys.W or Keys.Down or Keys.S))
        {
            int direction = key is Keys.Up or Keys.W ? -1 : 1;
            titleChapterSelection = Math.Clamp(titleChapterSelection + direction, 0, campaignChapters.Length - 1);
            Invalidate(); return;
        }
        if (titlePage == TitlePage.History)
        {
            if (key is Keys.Left or Keys.A) storyPage = Math.Max(0, storyPage - 1);
            else if (key is Keys.Right or Keys.D or Keys.Enter or Keys.Space) storyPage = Math.Min(prologue.Length - 1, storyPage + 1);
            Invalidate(); return;
        }
        if (titlePage == TitlePage.Settings)
        {
            if (key is Keys.Up or Keys.W) titleSelection = (titleSelection + 2) % 3;
            else if (key is Keys.Down or Keys.S) titleSelection = (titleSelection + 1) % 3;
            else if (titleSelection == 0 && (key is Keys.Enter or Keys.Space or Keys.N)) ToggleTitleAudio();
            else if (titleSelection == 1 && (key is Keys.Enter or Keys.Space or Keys.H)) highContrastTelegraphs = !highContrastTelegraphs;
            else if (titleSelection == 2 && (key is Keys.Enter or Keys.Space or Keys.M)) reducedScreenShake = !reducedScreenShake;
            Invalidate();
        }
    }

    private void ActivateTitleSelection()
    {
        switch (titleSelection)
        {
            case 0:
                if (HasTitleSave) LoadGame(); else notice = "AINDA NÃO HÁ UMA JORNADA SALVA";
                break;
            case 1: confirmNewJourney = HasTitleSave; if (!confirmNewJourney) BeginClassSelection(); break;
            case 2: titlePage = TitlePage.Campaign; { SaveData? preview = TitleSavePreview(); int index = campaignChapters.ToList().FindIndex(chapter => preview is not null && preview.Room >= chapter.FirstRoom && preview.Room <= chapter.LastRoom); titleChapterSelection = index < 0 ? 0 : index; } break;
            case 3: titlePage = TitlePage.History; storyPage = 0; break;
            case 4: titlePage = TitlePage.Settings; titleSelection = 0; break;
            case 5: Close(); break;
        }
        Invalidate();
    }

    private void BeginClassSelection()
    {
        confirmNewJourney = false; classSelectionOpen = true; classSelection = 0; Invalidate();
    }

    private void ChooseHeroClass(int selection)
    {
        heroClass = selection switch { 1 => HeroClass.StoneGuardian, 2 => HeroClass.NameWeaver, _ => HeroClass.Listener };
        classSelectionOpen = false; StartGame();
    }

    private string ProloguePage(int page) => page != 1 ? prologue[page] : heroClass switch
    {
        HeroClass.StoneGuardian => "Você é um Guardião da Pedra: protege os caminhos de Véspera e converte os golpes que recebe em Fúria de Pedra. Entre essas lembranças reconheceu Nara, sua irmã, desaparecida quando os sinos pararam.",
        HeroClass.NameWeaver => "Você é uma Tecelã de Nomes: lê os fios que prendem cada lembrança e pode torná-los frágeis. Ao ouvir os ecos da pedra, reconheceu Nara, sua irmã, desaparecida quando os sinos pararam.",
        _ => prologue[page]
    };

    private void ToggleTitleAudio()
    {
        bool muted = GameAudio.ToggleMute();
        notice = muted ? "ÁUDIO DESATIVADO" : "ÁUDIO ATIVADO";
        noticeTime = 1.5f;
    }

    private void HandleTitleClick(PointF point)
    {
        if (classSelectionOpen)
        {
            if (new RectangleF(25, 125, 290, 365).Contains(point)) ChooseHeroClass(0);
            else if (new RectangleF(335, 125, 290, 365).Contains(point)) ChooseHeroClass(1);
            else if (new RectangleF(645, 125, 290, 365).Contains(point)) ChooseHeroClass(2);
            else if (point.Y > 500) classSelectionOpen = false;
            Invalidate(); return;
        }
        if (confirmNewJourney)
        {
            if (new RectangleF(300, 365, 170, 48).Contains(point)) BeginClassSelection();
            else if (new RectangleF(490, 365, 170, 48).Contains(point)) confirmNewJourney = false;
            Invalidate(); return;
        }
        if (titlePage == TitlePage.Main)
        {
            int clicked = (int)((point.Y - 246) / 55);
            if (point.X >= 85 && point.X <= 470 && clicked >= 0 && clicked < 6)
            {
                titleSelection = clicked; ActivateTitleSelection();
            }
        }
        else if (point.Y >= 518 && point.Y <= 565)
        {
            if (titlePage == TitlePage.Campaign && point.X >= 190 && point.X <= 475)
            {
                if (HasTitleSave) LoadGame(); else BeginClassSelection();
            }
            else if (titlePage == TitlePage.Campaign && point.X >= 500 && point.X <= 770) { titlePage = TitlePage.Main; titleSelection = 2; }
            else { titlePage = TitlePage.Main; titleSelection = 2; }
        }
        else if (titlePage == TitlePage.Campaign && point.X < 405 && point.Y >= 165 && point.Y < 165 + campaignChapters.Length * 19)
        {
            titleChapterSelection = Math.Clamp((int)((point.Y - 165) / 19), 0, campaignChapters.Length - 1);
        }
        else if (titlePage == TitlePage.History)
        {
            if (point.X < 480) storyPage = Math.Max(0, storyPage - 1);
            else storyPage = Math.Min(prologue.Length - 1, storyPage + 1);
        }
        else if (titlePage == TitlePage.Settings)
        {
            if (point.Y is >= 250 and < 345) ToggleTitleAudio();
            else if (point.Y is >= 350 and < 445) highContrastTelegraphs = !highContrastTelegraphs;
            else if (point.Y is >= 450 and < 535) reducedScreenShake = !reducedScreenShake;
        }
        Invalidate();
    }

    private long PreviewKnownRooms(SaveData? save)
    {
        if (save is null) return HasTitleSave ? 0L : 1L;
        long known = save.ClearedRooms | (1L << save.Room);
        if (save.RoomCleared && save.Room != 3) known |= 1L << save.Room;
        if (save.QuestStage >= 3) known |= 1L << 3;
        if (save.QuestStage >= 22) known |= 1L << 18;
        if (save.QuestStage >= 57) known |= 1L << 41;
        if (save.QuestStage >= 58) known |= 1L << 42;
        if (save.FirstEchoDiscovered != 0 || save.FirstEchoQuest > 0 || save.FirstEchoCaveFound) known |= 1L << 43;
        if ((save.FirstEchoDiscovered & 2) != 0 || save.FirstEchoCaveFound) known |= 1L << 44;
        if ((save.FirstEchoDiscovered & 4) != 0 || save.FirstEchoQuest >= 2) known |= 1L << 45;
        return known;
    }

    private void DrawTitleScreen(Graphics g)
    {
        Bitmap? backdrop = TitleBackdrop.Value;
        if (backdrop is null)
        {
            using var fallback = new SolidBrush(Color.FromArgb(247, 7, 9, 11));
            g.FillRectangle(fallback, 0, 0, 960, 640);
        }
        else
        {
            g.DrawImage(backdrop, new Rectangle(0, 0, 960, 640));
            using var veil = new SolidBrush(Color.FromArgb(112, 7, 9, 11));
            g.FillRectangle(veil, 0, 0, 960, 640);
        }
        using var panel = new SolidBrush(Color.FromArgb(220, 17, 21, 22));
        using var panelEdge = new Pen(Color.FromArgb(170, 139, 108, 69), 2);
        using var gold = new SolidBrush(Color.FromArgb(230, 193, 129));
        using var pale = new SolidBrush(Color.FromArgb(227, 222, 205));
        using var muted = new SolidBrush(Color.FromArgb(153, 161, 151));
        using var accent = new Pen(Color.FromArgb(129, 105, 69), 1);
        g.DrawLine(accent, 90, 104, 870, 104); g.DrawLine(accent, 90, 111, 870, 111);

        if (titlePage == TitlePage.Main)
        {
            DrawCentered(g, "SCADRYK", 25, new Font("Georgia", 39, FontStyle.Bold), Color.FromArgb(230, 193, 129));
            DrawCentered(g, "O LUME GUARDA O QUE O MUNDO ESCOLHE ESQUECER", 79, new Font("Segoe UI", 11, FontStyle.Bold), Color.FromArgb(177, 183, 168));
            g.FillRectangle(panel, 82, 145, 398, 442); g.DrawRectangle(panelEdge, 82, 145, 398, 442);
            SaveData? preview = TitleSavePreview();
            string titleHint = titleSelection switch
            {
                0 => preview is not null ? $"NÍVEL {preview.Level} • {roomNames[preview.Room]}" : HasTitleSave ? "UMA JORNADA ANTERIOR ESTÁ DISPONÍVEL" : "AINDA NÃO HÁ UMA JORNADA SALVA",
                1 => "ESCOLHA SUA CLASSE E INICIE A HISTÓRIA",
                2 => "CONSULTE OS CAPÍTULOS E CAMINHOS",
                3 => "CONHEÇA O INÍCIO DA HISTÓRIA DE VÉSPERA",
                4 => "AJUSTE ÁUDIO, CONTRASTE E CÂMERA",
                _ => "ENCERRE O JOGO"
            };
            using (var hintFont = new Font("Segoe UI", 9, FontStyle.Bold))
            using (var hintFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter })
                g.DrawString(titleHint, hintFont, pale, new RectangleF(104, 165, 352, 56), hintFormat);
            string[] options = { "CONTINUAR", "NOVA JORNADA", "CAMPANHA E MAPAS", "HISTÓRIA", "CONFIGURAÇÕES", "SAIR" };
            for (int i = 0; i < options.Length; i++)
            {
                float y = 246 + i * 55;
                bool selected = i == titleSelection;
                bool disabled = i == 0 && !HasTitleSave;
                using var button = new SolidBrush(selected ? Color.FromArgb(150, 94, 74, 42) : Color.FromArgb(166, 33, 37, 37));
                using var outline = new Pen(selected ? Color.FromArgb(230, 193, 129) : Color.FromArgb(71, 74, 68), selected ? 2 : 1);
                g.FillRectangle(button, 105, y, 350, 45); g.DrawRectangle(outline, 105, y, 350, 45);
                if (selected) DrawCentered(g, "›", y + 9, new Font("Georgia", 20, FontStyle.Bold), Color.FromArgb(255, 220, 145), 126);
                DrawCentered(g, options[i], y + 13, new Font("Segoe UI", 12, FontStyle.Bold), disabled ? Color.FromArgb(104, 108, 101) : selected ? Color.FromArgb(255, 232, 180) : Color.FromArgb(211, 208, 193), 280);
            }
            g.FillRectangle(panel, 508, 145, 370, 412); g.DrawRectangle(panelEdge, 508, 145, 370, 412);
            DrawCentered(g, "UMA CAMPANHA, UM MUNDO EM MOVIMENTO", 169, new Font("Georgia", 13, FontStyle.Bold), Color.FromArgb(230, 193, 129));
            DrawCentered(g, $"{roomNames.Length} áreas • 18 capítulos • expedição opcional", 208, new Font("Segoe UI", 11, FontStyle.Bold), Color.FromArgb(225, 220, 204));
            using var routeLine = new Pen(Color.FromArgb(106, 95, 68), 3);
            g.DrawLine(routeLine, 553, 274, 553, 454);
            string[] stops = { "A cripta sob Véspera", "O Rio Cego e o Lume", "O mapa de Nara", "Rotas além do refúgio", "O destino das vozes" };
            string[] details = { "A primeira lembrança", "O farol esquecido", "Uma comunidade a oeste", "Regiões e chefes inéditos", "Escolhas que mudam o epílogo" };
            using var stopFont = new Font("Segoe UI", 10, FontStyle.Bold);
            using var detailFont = new Font("Segoe UI", 9);
            for (int i = 0; i < stops.Length; i++)
            {
                float y = 260 + i * 48;
                using var node = new SolidBrush(Color.FromArgb(178, 133, 101, 59)); g.FillEllipse(node, 544, y + 5, 18, 18);
                g.DrawString(stops[i], stopFont, pale, 578, y);
                g.DrawString(details[i], detailFont, muted, 578, y + 19);
            }
            DrawCentered(g, HasTitleSave ? "JORNADA SALVA • C PARA CONTINUAR" : "NOVA JORNADA • SAVE LOCAL EM JSON", 518, new Font("Segoe UI", 9, FontStyle.Bold), Color.FromArgb(174, 179, 163));
            DrawCentered(g, "↑ / ↓ NAVEGAR     ENTER ESCOLHER     ESC VOLTAR", 584, new Font("Segoe UI", 9), Color.FromArgb(158, 164, 154));
        }
        else if (titlePage == TitlePage.Campaign) DrawTitleCampaignMap(g, panel, panelEdge, gold, pale, muted);
        else if (titlePage == TitlePage.History)
        {
            g.FillRectangle(panel, 82, 145, 796, 412); g.DrawRectangle(panelEdge, 82, 145, 796, 412);
            DrawCentered(g, "PRÓLOGO • MEMÓRIAS NA PEDRA", 175, new Font("Georgia", 20, FontStyle.Bold), Color.FromArgb(230, 193, 129));
            DrawCentered(g, $"{storyPage + 1} / {prologue.Length}", 218, new Font("Segoe UI", 10, FontStyle.Bold), Color.FromArgb(166, 173, 158));
            using var storyFont = new Font("Georgia", 16);
            using var storyBrush = new SolidBrush(Color.FromArgb(235, 231, 215));
            g.DrawString(ProloguePage(storyPage), storyFont, storyBrush, new RectangleF(145, 265, 670, 150));
            DrawCentered(g, storyPage > 0 ? "← / A  ANTERIOR" : "A HISTÓRIA COMEÇA NA NOITE SEM SINOS", 460, new Font("Segoe UI", 10, FontStyle.Bold), Color.FromArgb(171, 177, 164), 300);
            DrawCentered(g, storyPage < prologue.Length - 1 ? "PRÓXIMA  → / D" : "O RESTO DA HISTÓRIA É DESCOBERTO JOGANDO", 460, new Font("Segoe UI", 10, FontStyle.Bold), Color.FromArgb(171, 177, 164), 670);
            DrawCentered(g, "ENTER AVANÇA • ← / → TROCA A PÁGINA • ESC VOLTA", 584, new Font("Segoe UI", 9), Color.FromArgb(158, 164, 154));
        }
        else
        {
            g.FillRectangle(panel, 82, 145, 796, 412); g.DrawRectangle(panelEdge, 82, 145, 796, 412);
            DrawCentered(g, "CONFIGURAÇÕES", 176, new Font("Georgia", 22, FontStyle.Bold), Color.FromArgb(230, 193, 129));
            bool mutedAudio = GameAudio.Muted;
            DrawTitleSetting(g, 0, "ÁUDIO", mutedAudio ? "DESATIVADO" : "ATIVADO", titleSelection == 0);
            DrawTitleSetting(g, 1, "AVISOS DE ATAQUE", highContrastTelegraphs ? "ALTO CONTRASTE" : "PADRÃO", titleSelection == 1);
            DrawTitleSetting(g, 2, "TREMOR DE CÂMERA", reducedScreenShake ? "REDUZIDO" : "NORMAL", titleSelection == 2);
            DrawCentered(g, "N / ENTER ÁUDIO     H / ENTER CONTRASTE     M / ENTER TREMOR", 548, new Font("Segoe UI", 10), Color.FromArgb(171, 177, 164));
            DrawCentered(g, "↑ / ↓ NAVEGAR     ESC VOLTAR", 584, new Font("Segoe UI", 9), Color.FromArgb(158, 164, 154));
        }
        if (confirmNewJourney)
        {
            using var veil = new SolidBrush(Color.FromArgb(205, 0, 0, 0)); using var confirm = new SolidBrush(Color.FromArgb(252, 22, 25, 25)); using var confirmEdge = new Pen(Color.FromArgb(230, 193, 129), 2);
            g.FillRectangle(veil, 0, 0, 960, 640); g.FillRectangle(confirm, 215, 225, 530, 190); g.DrawRectangle(confirmEdge, 215, 225, 530, 190);
            DrawCentered(g, "COMEÇAR OUTRA JORNADA?", 248, new Font("Georgia", 19, FontStyle.Bold), Color.FromArgb(230, 193, 129));
            DrawCentered(g, "O save existente será mantido até você salvar o novo progresso.", 300, new Font("Segoe UI", 10), Color.AntiqueWhite);
            using var yes = new SolidBrush(Color.FromArgb(94, 74, 42)); using var no = new SolidBrush(Color.FromArgb(37, 41, 40));
            g.FillRectangle(yes, 300, 365, 170, 48); g.FillRectangle(no, 490, 365, 170, 48);
            DrawCentered(g, "COMEÇAR • Y", 377, new Font("Segoe UI", 11, FontStyle.Bold), Color.FromArgb(255, 232, 180), 385);
            DrawCentered(g, "VOLTAR • N", 377, new Font("Segoe UI", 11, FontStyle.Bold), Color.AntiqueWhite, 575);
        }
        if (classSelectionOpen)
        {
            using var veil = new SolidBrush(Color.FromArgb(224, 0, 0, 0));
            using var cardFill = new SolidBrush(Color.FromArgb(252, 22, 25, 25));
            using var listenerEdge = new Pen(classSelection == 0 ? Color.Goldenrod : Color.FromArgb(87, 92, 82), classSelection == 0 ? 3 : 1);
            using var guardianEdge = new Pen(classSelection == 1 ? Color.Goldenrod : Color.FromArgb(87, 92, 82), classSelection == 1 ? 3 : 1);
            using var weaverEdge = new Pen(classSelection == 2 ? Color.MediumOrchid : Color.FromArgb(87, 92, 82), classSelection == 2 ? 3 : 1);
            g.FillRectangle(veil, 0, 0, 960, 640);
            DrawCentered(g, "ESCOLHA COMO ENFRENTAR O SILÊNCIO", 72, new Font("Georgia", 22, FontStyle.Bold), Color.FromArgb(230, 193, 129));
            g.FillRectangle(cardFill, 25, 125, 290, 365); g.DrawRectangle(listenerEdge, 25, 125, 290, 365);
            g.FillRectangle(cardFill, 335, 125, 290, 365); g.DrawRectangle(guardianEdge, 335, 125, 290, 365);
            g.FillRectangle(cardFill, 645, 125, 290, 365); g.DrawRectangle(weaverEdge, 645, 125, 290, 365);
            DrawCentered(g, "1 • ESCUTADOR", 155, new Font("Georgia", 17, FontStyle.Bold), Color.FromArgb(230, 193, 129), 170);
            DrawCentered(g, "HÍBRIDO • TÉCNICAS", 196, new Font("Segoe UI", 10, FontStyle.Bold), Color.AntiqueWhite, 170);
            DrawCentered(g, "Converte lembranças em", 252, new Font("Segoe UI", 10), Color.FromArgb(204, 207, 192), 170);
            DrawCentered(g, "alcance, controle e ressonância.", 274, new Font("Segoe UI", 10), Color.FromArgb(204, 207, 192), 170);
            DrawCentered(g, "Equilíbrio • mobilidade", 360, new Font("Segoe UI", 10, FontStyle.Bold), Color.LightSkyBlue, 170);
            DrawCentered(g, "2 • GUARDIÃO", 155, new Font("Georgia", 17, FontStyle.Bold), Color.FromArgb(230, 193, 129), 480);
            DrawCentered(g, "TANQUE • IMPACTO • FÚRIA", 196, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 480);
            DrawCentered(g, "Resiste mais e golpeia forte,", 252, new Font("Segoe UI", 10), Color.FromArgb(204, 207, 192), 480);
            DrawCentered(g, "mas ataca e se move devagar.", 274, new Font("Segoe UI", 10), Color.FromArgb(204, 207, 192), 480);
            DrawCentered(g, "Dano recebido gera Fúria.", 360, new Font("Segoe UI", 10, FontStyle.Bold), Color.FromArgb(255, 203, 116), 480);
            DrawCentered(g, "3 • TECELÃ DE NOMES", 155, new Font("Georgia", 16, FontStyle.Bold), Color.FromArgb(230, 193, 129), 790);
            DrawCentered(g, "CONJURADORA • ÁREA • FIO", 196, new Font("Segoe UI", 9, FontStyle.Bold), Color.AntiqueWhite, 790);
            DrawCentered(g, "Golpes rápidos geram Fio;", 252, new Font("Segoe UI", 10), Color.FromArgb(204, 207, 192), 790);
            DrawCentered(g, "suas técnicas causam mais dano.", 274, new Font("Segoe UI", 10), Color.FromArgb(204, 207, 192), 790);
            DrawCentered(g, "Mais frágil • marca grupos", 360, new Font("Segoe UI", 10, FontStyle.Bold), Color.Plum, 790);
            DrawCentered(g, "↑ / ↓ ESCOLHER • 1 / 2 / 3 • ENTER CONFIRMA • ESC VOLTA", 535, new Font("Segoe UI", 10, FontStyle.Bold), Color.AntiqueWhite);
        }
        if (noticeTime > 0 && !string.IsNullOrWhiteSpace(notice)) DrawCentered(g, notice, 612, new Font("Segoe UI", 9, FontStyle.Bold), Color.FromArgb(230, 193, 129));
    }

    private void DrawTitleCampaignMap(Graphics g, Brush panel, Pen panelEdge, Brush gold, Brush pale, Brush muted)
    {
        g.FillRectangle(panel, 28, 104, 904, 488); g.DrawRectangle(panelEdge, 28, 104, 904, 488);
        SaveData? save = TitleSavePreview();
        long known = PreviewKnownRooms(save), cleared = save?.ClearedRooms ?? 0;
        DrawCentered(g, "ATLAS DA CAMPANHA", 115, new Font("Georgia", 19, FontStyle.Bold), Color.FromArgb(230, 193, 129));
        DrawCentered(g, $"{roomNames.Length} áreas • 18 capítulos • navegue pela campanha e veja o mapa de cada etapa", 145, new Font("Segoe UI", 9, FontStyle.Bold), Color.FromArgb(196, 196, 179));
        CampaignChapter selectedChapter = campaignChapters[Math.Clamp(titleChapterSelection, 0, campaignChapters.Length - 1)];
        string selectedState = CampaignChapterState(selectedChapter, save, known, cleared);

        using var listPanel = new SolidBrush(Color.FromArgb(230, 21, 24, 24)); using var mapPanel = new SolidBrush(Color.FromArgb(230, 21, 24, 24));
        using var panelLine = new Pen(Color.FromArgb(91, 86, 69), 1);
        g.FillRectangle(listPanel, 40, 164, 365, 344); g.DrawRectangle(panelLine, 40, 164, 365, 344);
        g.FillRectangle(mapPanel, 416, 164, 504, 344); g.DrawRectangle(panelLine, 416, 164, 504, 344);

        using var chapterFont = new Font("Segoe UI", 8, FontStyle.Bold);
        using var chapterFormat = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
        for (int i = 0; i < campaignChapters.Length; i++)
        {
            CampaignChapter chapter = campaignChapters[i]; float y = 166 + i * 19;
            string state = CampaignChapterState(chapter, save, known, cleared);
            bool selected = i == titleChapterSelection;
            using var chapterFill = new SolidBrush(selected ? Color.FromArgb(87, 67, 38) : Color.FromArgb(31, 35, 34));
            using var chapterEdge = new Pen(selected ? Color.FromArgb(222, 181, 105) : Color.FromArgb(56, 60, 56));
            using var chapterInk = new SolidBrush(state switch { "CONCLUÍDO" => Color.FromArgb(150, 210, 159), "ATUAL" or "INÍCIO" => Color.FromArgb(255, 224, 161), "BLOQUEADO" => Color.FromArgb(112, 116, 107), _ => Color.FromArgb(206, 207, 190) });
            g.FillRectangle(chapterFill, 45, y, 355, 18); g.DrawRectangle(chapterEdge, 45, y, 355, 18);
            g.DrawString($"{ToRoman(chapter.Number)}  {chapter.Title}", chapterFont, selected ? gold : pale, new RectangleF(51, y + 1, 276, 16), chapterFormat);
            using var stateFont = new Font("Segoe UI", 6.5f, FontStyle.Bold);
            g.DrawString(state, stateFont, chapterInk, new RectangleF(326, y + 1, 70, 16), chapterFormat);
        }

        DrawCentered(g, $"CAPÍTULO {ToRoman(selectedChapter.Number)} • {selectedChapter.Title.ToUpperInvariant()}", 170, new Font("Georgia", 11, FontStyle.Bold), Color.FromArgb(230, 193, 129), 668);
        DrawCentered(g, selectedState, 190, new Font("Segoe UI", 8, FontStyle.Bold), Color.FromArgb(179, 183, 170), 668);
        int roomCount = selectedChapter.LastRoom - selectedChapter.FirstRoom + 1;
        using var route = new Pen(Color.FromArgb(102, 89, 63), 2);
        const float cardX = 427, cardY = 218, cardWidth = 232, cardHeight = 67, cardGapX = 23, cardGapY = 16;
        for (int offset = 0; offset < roomCount - 1; offset++)
        {
            int row = offset / 2, col = row % 2 == 0 ? offset % 2 : 1 - offset % 2;
            int nextRow = (offset + 1) / 2, nextCol = nextRow % 2 == 0 ? (offset + 1) % 2 : 1 - (offset + 1) % 2;
            PointF a = new(cardX + col * (cardWidth + cardGapX) + cardWidth / 2, cardY + row * (cardHeight + cardGapY) + cardHeight / 2);
            PointF b = new(cardX + nextCol * (cardWidth + cardGapX) + cardWidth / 2, cardY + nextRow * (cardHeight + cardGapY) + cardHeight / 2);
            g.DrawLine(route, a, b);
        }
        using var roomNameFont = new Font("Segoe UI", 8, FontStyle.Bold);
        using var roomStatusFont = new Font("Segoe UI", 7);
        for (int offset = 0; offset < roomCount; offset++)
        {
            int index = selectedChapter.FirstRoom + offset;
            int row = offset / 2, col = row % 2 == 0 ? offset % 2 : 1 - offset % 2;
            float x = cardX + col * (cardWidth + cardGapX), y = cardY + row * (cardHeight + cardGapY);
            bool mapped = (known & (1L << index)) != 0, done = (cleared & (1L << index)) != 0;
            bool here = save is not null && save.Room == index;
            bool startingArea = save is null && !HasTitleSave && index == 0;
            Color background = here || startingArea ? Color.FromArgb(91, 67, 35) : done ? Color.FromArgb(34, 61, 44) : mapped ? Color.FromArgb(42, 47, 44) : Color.FromArgb(27, 30, 30);
            Color outline = here || startingArea ? Color.FromArgb(223, 181, 104) : done ? Color.FromArgb(85, 131, 94) : mapped ? Color.FromArgb(89, 95, 86) : Color.FromArgb(53, 57, 53);
            using var roomFill = new SolidBrush(background); using var roomEdge = new Pen(outline, here ? 2 : 1);
            g.FillRectangle(roomFill, x, y, cardWidth, cardHeight); g.DrawRectangle(roomEdge, x, y, cardWidth, cardHeight);
            using var roomInk = new SolidBrush(mapped ? Color.FromArgb(222, 218, 199) : Color.FromArgb(117, 121, 111));
            string roomState = here ? "SAVE ATUAL" : startingArea ? "INÍCIO DA CAMPANHA" : done ? "CONCLUÍDA" : mapped ? "DESCOBERTA" : "BLOQUEADA • ABRE PELA HISTÓRIA";
            g.DrawString($"ÁREA {index + 1:00}  •  {AtlasRoomName(index)}", roomNameFont, roomInk, new RectangleF(x + 10, y + 10, cardWidth - 20, 24));
            g.DrawString(roomState, roomStatusFont, here ? gold : done ? Brushes.LightGreen : muted, x + 10, y + 42);
        }
        int chapterMapped = Enumerable.Range(selectedChapter.FirstRoom, roomCount).Count(index => (known & (1L << index)) != 0);
        DrawCentered(g, $"MAPA DO CAPÍTULO  •  {chapterMapped}/{roomCount} ÁREAS DESCOBERTAS", 477, new Font("Segoe UI", 8, FontStyle.Bold), Color.FromArgb(178, 183, 170), 668);
        using var startButton = new SolidBrush(Color.FromArgb(77, 63, 40)); using var backButton = new SolidBrush(Color.FromArgb(33, 37, 37));
        using var startEdge = new Pen(Color.FromArgb(230, 193, 129), 1); using var backEdge = new Pen(Color.FromArgb(71, 74, 68), 1);
        g.FillRectangle(startButton, 190, 518, 285, 38); g.DrawRectangle(startEdge, 190, 518, 285, 38);
        g.FillRectangle(backButton, 500, 518, 270, 38); g.DrawRectangle(backEdge, 500, 518, 270, 38);
        DrawCentered(g, save is null ? HasTitleSave ? "CONTINUAR DO SAVE" : "INICIAR CAMPANHA" : "CONTINUAR DO SAVE", 528, new Font("Segoe UI", 10, FontStyle.Bold), Color.FromArgb(255, 232, 180), 332);
        DrawCentered(g, "VOLTAR AO MENU", 528, new Font("Segoe UI", 10, FontStyle.Bold), Color.FromArgb(211, 208, 193), 635);
        DrawCentered(g, "↑ / ↓ OU CLIQUE ESCOLHE CAPÍTULO • ENTER CONTINUA DO SAVE • ESC VOLTA", 565, new Font("Segoe UI", 8), Color.FromArgb(158, 164, 154));
    }

    private string CampaignChapterState(CampaignChapter chapter, SaveData? save, long known, long cleared)
    {
        if (save is null && !HasTitleSave && chapter.Number == 1) return "INÍCIO";
        bool current = save is not null && save.Room >= chapter.FirstRoom && save.Room <= chapter.LastRoom;
        if (current) return "ATUAL";
        bool discovered = Enumerable.Range(chapter.FirstRoom, chapter.LastRoom - chapter.FirstRoom + 1).Any(index => (known & (1L << index)) != 0);
        if (!discovered) return "BLOQUEADO";
        bool allCombatAreasCleared = Enumerable.Range(chapter.FirstRoom, chapter.LastRoom - chapter.FirstRoom + 1)
            .Where(index => index is not 3 and not 18)
            .All(index => (cleared & (1L << index)) != 0);
        return allCombatAreasCleared ? "CONCLUÍDO" : "DESCOBERTO";
    }

    private static string ToRoman(int number) => number switch
    {
        1 => "I", 2 => "II", 3 => "III", 4 => "IV", 5 => "V", 6 => "VI", 7 => "VII", 8 => "VIII", 9 => "IX",
        10 => "X", 11 => "XI", 12 => "XII", 13 => "XIII", 14 => "XIV", 15 => "XV", 16 => "XVI", 17 => "XVII", 18 => "XVIII", _ => number.ToString()
    };

    private void DrawTitleSetting(Graphics g, int row, string name, string value, bool selected)
    {
        float y = 255 + row * 100;
        using var nameFont = new Font("Segoe UI", 12, FontStyle.Bold);
        using var valueFont = new Font("Segoe UI", 11, FontStyle.Bold);
        using var fill = new SolidBrush(selected ? Color.FromArgb(77, 63, 40) : Color.FromArgb(33, 37, 37));
        using var outline = new Pen(selected ? Color.FromArgb(230, 193, 129) : Color.FromArgb(71, 74, 68), selected ? 2 : 1);
        g.FillRectangle(fill, 190, y, 580, 76); g.DrawRectangle(outline, 190, y, 580, 76);
        g.DrawString(name, nameFont, Brushes.AntiqueWhite, 220, y + 14);
        using var valueBrush = new SolidBrush(Color.FromArgb(230, 193, 129));
        g.DrawString(value, valueFont, valueBrush, 560, y + 15);
    }
}
