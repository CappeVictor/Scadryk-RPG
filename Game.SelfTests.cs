using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;

public sealed partial class Game
{
    public static int SelfTest()
    {
        GameAudio.SilenceForAutomatedRun();
        if (!EquipmentCatalog.IsValid()) throw new InvalidOperationException("O catálogo de armas contém perfis incompletos ou duplicados.");
        if (!IsEnemyCatalogValid()) throw new InvalidOperationException("O catálogo de inimigos omite um tipo ou contém atributos inválidos.");
        if (EnemyData(EnemyKind.Grunt).HealthAt(0, 0) != 58 || EnemyData(EnemyKind.Grunt).HealthAt(42, 0) != 394
            || EnemyData(EnemyKind.Elite).HealthAt(42, 0) != 366
            || EnemyData(EnemyKind.NameWeaver).HealthAt(13, 1) != 410 || EnemyData(EnemyKind.NameWeaver).HealthAt(13, 2) != 455)
            throw new InvalidOperationException("A progressão de vida dos inimigos mudou ao migrar para o catálogo.");
        using (var enemyContactCatalogCheck = new Game())
        {
            enemyContactCatalogCheck.timer.Stop();
            foreach (EnemyKind kind in Enum.GetValues<EnemyKind>())
            {
                var dummy = new Enemy { Kind = kind };
                float expected = kind == EnemyKind.SlagWeaver ? 18 : kind == EnemyKind.Elite ? 20 : EnemyData(kind).ContactDamage;
                if (enemyContactCatalogCheck.EnemyContactDamage(dummy) != expected)
                    throw new InvalidOperationException($"O dano de contato catalogado para {kind} não corresponde ao combate.");
            }
            enemyContactCatalogCheck.vigilActive = true;
            if (enemyContactCatalogCheck.EnemyContactDamage(new Enemy { Kind = EnemyKind.DeepSilence }) != 112
                || enemyContactCatalogCheck.EnemyContactDamage(new Enemy { Kind = EnemyKind.SlagWeaver }) != 18)
                throw new InvalidOperationException("O escalonamento de dano da Vigília alterou as exceções catalogadas.");
        }
        using (var enemyBehaviorCatalogCheck = new Game())
        {
            enemyBehaviorCatalogCheck.timer.Stop(); enemyBehaviorCatalogCheck.StartGame(); enemyBehaviorCatalogCheck.enemies.Clear();
            EnemyKind[] rangedKinds = Enum.GetValues<EnemyKind>().Where(kind => EnemyData(kind).AiStyle == EnemyAiStyle.RangedKiter).ToArray();
            if (rangedKinds.Length != 8) throw new InvalidOperationException("O catálogo não contém os oito perfis de criaturas à distância.");
            foreach (EnemyKind kind in rangedKinds)
            {
                EnemyDefinition profile = EnemyData(kind);
                var kitingEnemy = new Enemy { Kind = kind, Pos = new PointF(350, 330), Cooldown = 1, Hp = 100, MaxHp = 100 };
                enemyBehaviorCatalogCheck.player = new PointF(300, 330);
                float distanceBeforeRetreat = Length(Sub(kitingEnemy.Pos, enemyBehaviorCatalogCheck.player));
                if (!enemyBehaviorCatalogCheck.UpdateDataDrivenEnemyBehavior(kitingEnemy, 0.1f, 50, new PointF(-50, 0))
                    || Length(Sub(kitingEnemy.Pos, enemyBehaviorCatalogCheck.player)) < distanceBeforeRetreat - 0.05f)
                    throw new InvalidOperationException($"O perfil à distância de {kind} não manteve sua faixa de combate.");
                kitingEnemy.Pos = new PointF(600, 330); kitingEnemy.Cooldown = 0;
                float midRange = profile.PreferredDistance + 10;
                if (!enemyBehaviorCatalogCheck.UpdateDataDrivenEnemyBehavior(kitingEnemy, 0, midRange, new PointF(-midRange, 0))
                    || Math.Abs(kitingEnemy.Windup - profile.AttackWindup) > 0.001f || Math.Abs(kitingEnemy.Cooldown - profile.AttackCooldown) > 0.001f)
                    throw new InvalidOperationException($"O perfil à distância de {kind} não preservou seu aviso ou intervalo de ataque.");
            }
            EnemyDefinition mageProfile = EnemyData(EnemyKind.Mage);
            var mage = new Enemy { Kind = EnemyKind.Mage, Pos = new PointF(700, 330), Cooldown = 0, Hp = 100, MaxHp = 100 };
            enemyBehaviorCatalogCheck.player = new PointF(300, 330);
            float mageFarDistance = Length(Sub(mage.Pos, enemyBehaviorCatalogCheck.player));
            if (!enemyBehaviorCatalogCheck.UpdateDataDrivenEnemyBehavior(mage, 0.1f, mageFarDistance, new PointF(-mageFarDistance, 0))
                || Length(Sub(mage.Pos, enemyBehaviorCatalogCheck.player)) >= mageFarDistance - 0.05f || mage.Windup > 0)
                throw new InvalidOperationException("O conjurador não avançou para dentro de seu alcance de combate.");
            mage.Pos = new PointF(550, 330); mage.Cooldown = 0;
            float mageAttackDistance = Length(Sub(mage.Pos, enemyBehaviorCatalogCheck.player));
            if (mageAttackDistance > mageProfile.AttackRange || !enemyBehaviorCatalogCheck.UpdateDataDrivenEnemyBehavior(mage, 0, mageAttackDistance, new PointF(-mageAttackDistance, 0))
                || Math.Abs(mage.Windup - mageProfile.AttackWindup) > 0.001f || Math.Abs(mage.Cooldown - mageProfile.AttackCooldown) > 0.001f)
                throw new InvalidOperationException("O conjurador não respeitou o perfil catalogado de ataque.");

            EnemyDefinition spearboundProfile = EnemyData(EnemyKind.Elite);
            var spearbound = new Enemy { Kind = EnemyKind.Elite, EliteAffix = EliteAffix.Spearbound, Pos = new PointF(600, 330), Cooldown = 0, Hp = 100, MaxHp = 100 };
            float spearboundAttackDistance = Length(Sub(spearbound.Pos, enemyBehaviorCatalogCheck.player));
            if (!enemyBehaviorCatalogCheck.UpdateDataDrivenEnemyBehavior(spearbound, 0, spearboundAttackDistance, new PointF(-spearboundAttackDistance, 0))
                || spearboundAttackDistance > spearboundProfile.SpearboundAttackRange || Math.Abs(spearbound.Windup - spearboundProfile.SpearboundAttackWindup) > 0.001f
                || Math.Abs(spearbound.Cooldown - spearboundProfile.SpearboundAttackCooldown) > 0.001f)
                throw new InvalidOperationException("O Lançador não respeitou seu alcance ou aviso de ataque catalogado.");
            spearbound = new Enemy { Kind = EnemyKind.Elite, EliteAffix = EliteAffix.Spearbound, Pos = new PointF(600, 330), Cooldown = 1, Hp = 100, MaxHp = 100 };
            float spearboundFarDistance = Length(Sub(spearbound.Pos, enemyBehaviorCatalogCheck.player));
            if (!enemyBehaviorCatalogCheck.UpdateDataDrivenEnemyBehavior(spearbound, 0.1f, spearboundFarDistance, new PointF(-spearboundFarDistance, 0))
                || Length(Sub(spearbound.Pos, enemyBehaviorCatalogCheck.player)) >= spearboundFarDistance - 0.05f || spearbound.Windup > 0)
                throw new InvalidOperationException("O Lançador não voltou à distância de combate catalogada.");
        }
        float[] expectedRareAffixes = { 3, 7, 12, 10, 8, 12, 0.15f, 6 };
        float[] expectedRelicAffixes = { 6, 11, 18, 16, 12, 20, 0.25f, 8 };
        for (int i = 0; i < expectedRareAffixes.Length; i++)
            if (Math.Abs(EquipmentCatalog.WeaponAffix(i + 1).ValueForRarity(1) - expectedRareAffixes[i]) > 0.001f
                || Math.Abs(EquipmentCatalog.WeaponAffix(i + 1).ValueForRarity(2) - expectedRelicAffixes[i]) > 0.001f)
                throw new InvalidOperationException($"O afixo {i + 1} perdeu seu valor balanceado entre Rara e Relíquia.");
        if (EquipmentCatalog.RandomlyRolledAffixCount != 8 || EquipmentCatalog.RolledWeaponAffix(7) != 8
            || EquipmentCatalog.IsValidPrimaryAffix(9) || EquipmentCatalog.IsValidSecondaryAffix(9, 1) || !EquipmentCatalog.IsValidSecondaryAffix(9, 2)
            || EquipmentCatalog.WeaponAffix(9).Description(1) != "" || EquipmentCatalog.WeaponAffix(9).Description(2) != "do Nome Riscado: ecos marcados explodem ao morrer")
            throw new InvalidOperationException("O Nome Riscado deixou de ser exclusivo do espaço secundário de Relíquias.");
        foreach ((string Name, Bitmap? Sheet) sheet in new[]
        {
            ("Guardião: movimento", StoneGuardianSpriteSheet.Value),
            ("Guardião: ataque", StoneGuardianAttackSpriteSheet.Value),
            ("Tecelã: movimento", NameWeaverSpriteSheet.Value),
            ("Tecelã: ataque", NameWeaverAttackSpriteSheet.Value)
        })
        {
            if (sheet.Sheet is null || sheet.Sheet.Width < 4 || sheet.Sheet.Height < 4)
                throw new InvalidOperationException($"A folha 4×4 da classe ({sheet.Name}) não foi carregada do pacote.");
        }
        if (StoneGuardianSpriteSheet.Value == PlayerSpriteSheet.Value || NameWeaverSpriteSheet.Value == PlayerSpriteSheet.Value
            || StoneGuardianAttackSpriteSheet.Value == PlayerAttackSpriteSheet.Value || NameWeaverAttackSpriteSheet.Value == PlayerAttackSpriteSheet.Value)
            throw new InvalidOperationException("Uma classe voltou a usar a folha visual do Escutador em vez de seu sprite próprio.");
        using (var classSpriteRenderCheck = new Game())
        {
            classSpriteRenderCheck.timer.Stop();
            Bitmap DrawClassSprite(HeroClass archetype, float attackTime)
            {
                var frame = new Bitmap(200, 200);
                using Graphics graphics = Graphics.FromImage(frame);
                graphics.Clear(Color.Transparent);
                classSpriteRenderCheck.heroClass = archetype;
                classSpriteRenderCheck.player = new PointF(100, 100);
                classSpriteRenderCheck.attackFlash = attackTime;
                classSpriteRenderCheck.DrawPlayer(graphics);
                return frame;
            }
            static int DifferentPixels(Bitmap left, Bitmap right)
            {
                int different = 0;
                for (int y = 0; y < left.Height; y++)
                    for (int x = 0; x < left.Width; x++)
                        if (left.GetPixel(x, y).ToArgb() != right.GetPixel(x, y).ToArgb()) different++;
                return different;
            }
            using Bitmap listenerIdle = DrawClassSprite(HeroClass.Listener, 0);
            using Bitmap guardianIdle = DrawClassSprite(HeroClass.StoneGuardian, 0);
            using Bitmap weaverIdle = DrawClassSprite(HeroClass.NameWeaver, 0);
            using Bitmap listenerAttack = DrawClassSprite(HeroClass.Listener, 0.08f);
            using Bitmap guardianAttack = DrawClassSprite(HeroClass.StoneGuardian, 0.08f);
            using Bitmap weaverAttack = DrawClassSprite(HeroClass.NameWeaver, 0.08f);
            if (DifferentPixels(listenerIdle, guardianIdle) < 500 || DifferentPixels(listenerIdle, weaverIdle) < 500
                || DifferentPixels(listenerAttack, guardianAttack) < 500 || DifferentPixels(listenerAttack, weaverAttack) < 500)
                throw new InvalidOperationException("O renderizador não desenhou silhuetas próprias para as três classes em movimento e ataque.");
        }
        using (var menuHitTest = new Game())
        {
            menuHitTest.timer.Stop(); menuHitTest.titleSelection = 2;
            menuHitTest.HandleTitleClick(new PointF(280, 220));
            if (menuHitTest.titleSelection != 2 || menuHitTest.titlePage != TitlePage.Main) throw new InvalidOperationException("Um clique fora dos botões acionou o menu principal.");
            menuHitTest.HandleTitleClick(new PointF(280, 292));
            if (menuHitTest.titleSelection != 2 || menuHitTest.titlePage != TitlePage.Main) throw new InvalidOperationException("Um clique entre botões acionou o menu principal.");
            menuHitTest.BeginClassSelection(); menuHitTest.HandleTitleKey(Keys.Right);
            if (menuHitTest.classSelection != 1) throw new InvalidOperationException("A seta direita não selecionou a próxima classe.");
            menuHitTest.HandleTitleKey(Keys.Left);
            if (menuHitTest.classSelection != 0) throw new InvalidOperationException("A seta esquerda não retornou à classe anterior.");
            menuHitTest.HandleTitleClick(new PointF(600, 190));
            if (!menuHitTest.classSelectionOpen || menuHitTest.classSelection != 1) throw new InvalidOperationException("O clique no cartão não selecionou a classe antes da confirmação.");
            menuHitTest.HandleTitleClick(new PointF(50, 540));
            if (!menuHitTest.classSelectionOpen) throw new InvalidOperationException("Um clique fora dos controles fechou a seleção de classe.");
            menuHitTest.classSelectionOpen = false; menuHitTest.titlePage = TitlePage.Settings;
            menuHitTest.HandleTitleClick(new PointF(480, 525));
            if (!menuHitTest.reducedScreenShake || menuHitTest.titlePage != TitlePage.Settings) throw new InvalidOperationException("O fim do botão de tremor não alterou a opção corretamente.");
            menuHitTest.HandleTitleClick(new PointF(100, 470));
            if (!menuHitTest.reducedScreenShake) throw new InvalidOperationException("Um clique fora das configurações alterou o tremor.");
            menuHitTest.titlePage = TitlePage.History; menuHitTest.storyPage = 0;
            menuHitTest.HandleTitleClick(new PointF(600, 300));
            if (menuHitTest.storyPage != 0) throw new InvalidOperationException("Um clique no texto avançou a história.");
            menuHitTest.HandleTitleClick(new PointF(650, 460));
            if (menuHitTest.storyPage != 1) throw new InvalidOperationException("O botão de próxima página não avançou a história.");
            menuHitTest.titlePage = TitlePage.Campaign; menuHitTest.titleChapterSelection = 0;
            menuHitTest.HandleTitleClick(new PointF(20, 190)); menuHitTest.HandleTitleClick(new PointF(100, 540));
            if (menuHitTest.titlePage != TitlePage.Campaign || menuHitTest.titleChapterSelection != 0) throw new InvalidOperationException("Um clique fora dos controles alterou o atlas da campanha.");
        }
        if (Math.Abs(MeleeChaseSpeed(0) - 74f) > 0.01f || Math.Abs(MeleeChaseSpeed(42) - 168f) > 0.01f
            || MeleeChaseSpeed(42) >= PlayerMoveSpeed || MeleeChaseSpeed(42, true) <= PlayerMoveSpeed
            || Math.Abs(MeleeChaseSpeed(42, true) - 226.8f) > 0.1f)
            throw new InvalidOperationException("A progressão da velocidade corpo a corpo não preservou o limite de posicionamento nem o papel do elite Veloz.");
        if (GameAudio.SoundscapeForRoom(41) != 7 || GameAudio.SoundscapeForRoom(42) != 7 || GameAudio.SoundscapeForRoom(40) != 6)
            throw new InvalidOperationException("As salas do Capítulo XVIII não receberam a paisagem sonora própria das badaladas.");
        using (var readingLayoutCheck = new Game())
        using (var readingLayoutBitmap = new Bitmap(960, 640))
        using (var readingLayoutGraphics = Graphics.FromImage(readingLayoutBitmap))
        using (var headingFont = new Font("Georgia", 13, FontStyle.Bold))
        using (var bodyFont = new Font("Segoe UI", 14))
        using (var hintFont = new Font("Segoe UI", 9, FontStyle.Bold))
        {
            readingLayoutCheck.timer.Stop();
            foreach (string body in new[] { readingLayoutCheck.prologue[1], string.Join(" ", Enumerable.Repeat("memória compartilhada", 80)) })
            {
                ReadingOverlayLayout layout = MeasureReadingOverlay(readingLayoutGraphics, body, headingFont, bodyFont, hintFont);
                if (layout.PanelY < 0 || layout.BodyY + layout.BodyHeight + 10 > layout.HintY || layout.HintY + hintFont.GetHeight(readingLayoutGraphics) > layout.PanelY + layout.PanelHeight - 12)
                    throw new InvalidOperationException("O texto da história ou memória encostou no comando do painel de leitura.");
            }
        }
        (Color normalFill, Color normalEdge) = AttackWarningPalette(false);
        (Color contrastFill, Color contrastEdge) = AttackWarningPalette(true);
        if (normalEdge == contrastEdge || contrastFill.R < 240 || contrastFill.G < 190 || contrastEdge.R < 240 || contrastEdge.G < 220) throw new InvalidOperationException("A paleta acessível dos avisos não tem contraste suficiente em relação ao modo padrão.");
        using (var warningAccessibilityCheck = new Game())
        {
            warningAccessibilityCheck.timer.Stop(); warningAccessibilityCheck.StartGame();
            warningAccessibilityCheck.OnKeyDown(warningAccessibilityCheck, new KeyEventArgs(Keys.H));
            SaveData accessibleSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(warningAccessibilityCheck.CaptureSaveData()))!;
            if (!accessibleSave.HighContrastTelegraphs) throw new InvalidOperationException("A preferência de alto contraste não foi ativada ou salva.");
            warningAccessibilityCheck.OnKeyDown(warningAccessibilityCheck, new KeyEventArgs(Keys.H));
            if (warningAccessibilityCheck.CaptureSaveData().HighContrastTelegraphs) throw new InvalidOperationException("A preferência de alto contraste não pôde ser desativada.");
        }
        using (var targetCheck = new Game())
        {
            targetCheck.timer.Stop(); targetCheck.StartGame(); targetCheck.enemies.Clear(); targetCheck.drops.Clear(); targetCheck.player = new PointF(150, 330);
            Enemy distant = new() { Kind = EnemyKind.Grunt, Pos = new PointF(520, 330), Hp = 1, MaxHp = 1 };
            Enemy nearest = new() { Kind = EnemyKind.Grunt, Pos = new PointF(230, 330), Hp = 1, MaxHp = 1 };
            Enemy next = new() { Kind = EnemyKind.Grunt, Pos = new PointF(320, 330), Hp = 1, MaxHp = 1 };
            targetCheck.enemies.AddRange(new[] { distant, nearest, next }); targetCheck.lockedTarget = distant; targetCheck.Defeat(distant);
            if (targetCheck.lockedTarget != nearest) throw new InvalidOperationException("O alvo marcado não mudou para o inimigo vivo mais próximo após ser derrotado.");
            targetCheck.Defeat(nearest);
            if (targetCheck.lockedTarget != next) throw new InvalidOperationException("O bloqueio não acompanhou a derrota sucessiva do alvo.");
            targetCheck.Defeat(next);
            if (targetCheck.lockedTarget is not null) throw new InvalidOperationException("O bloqueio manteve um alvo quando não restavam inimigos.");
        }
        using (var interactionRadiusCheck = new Game())
        {
            interactionRadiusCheck.timer.Stop(); interactionRadiusCheck.StartGame(); interactionRadiusCheck.storyOverlay = false; interactionRadiusCheck.enemies.Clear(); interactionRadiusCheck.drops.Clear();
            interactionRadiusCheck.room = 3; interactionRadiusCheck.questStage = 0; interactionRadiusCheck.player = Offset(CuratorPosition, 0, 68); interactionRadiusCheck.InteractTown();
            if (interactionRadiusCheck.questStage != 1) throw new InvalidOperationException("O aviso de conversa com Sileth apareceu antes de a interação funcionar.");
            interactionRadiusCheck.questStage = 1; interactionRadiusCheck.player = Offset(ArchiveBookPosition, 0, 68); interactionRadiusCheck.InteractTown();
            if (interactionRadiusCheck.questStage != 2) throw new InvalidOperationException("O aviso para ler o Livro das Ausências apareceu antes de a interação funcionar.");
            interactionRadiusCheck.questStage = 3; interactionRadiusCheck.player = new PointF(480, 203); interactionRadiusCheck.InteractTown();
            if (interactionRadiusCheck.room != 4) throw new InvalidOperationException("O aviso do portão do Arquivo Norte apareceu antes de a interação funcionar.");
            interactionRadiusCheck.room = 18; interactionRadiusCheck.questStage = 21; interactionRadiusCheck.player = Offset(AvenPosition, 68, 0); interactionRadiusCheck.InteractRefuge();
            if (interactionRadiusCheck.questStage != 22) throw new InvalidOperationException("O aviso para falar com Aven apareceu antes de a interação funcionar.");
            interactionRadiusCheck.room = 0; interactionRadiusCheck.memoriesFound = 0; interactionRadiusCheck.enemies.Clear(); interactionRadiusCheck.drops.Clear(); interactionRadiusCheck.player = Offset(MemoryStonePositions[0], 0, 55); interactionRadiusCheck.Interact();
            if ((interactionRadiusCheck.memoriesFound & 1) == 0) throw new InvalidOperationException("O aviso da inscrição apareceu antes de a interação funcionar.");
        }
        if (ScreenToGame(new PointF(500, 320), new Size(1000, 640)) != new PointF(480, 320)
            || ScreenToGame(new PointF(960, 540), new Size(1920, 1080)) != new PointF(480, 320)
            || ScreenToGame(new PointF(480, 400), new Size(960, 800)) != new PointF(480, 320)
            || ScreenToGame(new PointF(0, 0), new Size(1920, 1080)) != new PointF(0, 0))
            throw new InvalidOperationException("A ampliação da janela distorceu o campo de jogo ou deslocou a mira do mouse.");
        using (var attackArcCheck = new Game())
        {
            attackArcCheck.timer.Stop(); attackArcCheck.StartGame(); attackArcCheck.storyOverlay = false; attackArcCheck.player = new PointF(150, 330); attackArcCheck.mouseAim = attackArcCheck.facing = new PointF(1, 0);
            for (int weaponIndex = 0; weaponIndex < EquipmentCatalog.Weapons.Count; weaponIndex++)
            {
                if (EquipmentCatalog.Weapon(weaponIndex).IsRanged) continue;
                attackArcCheck.weapon = weaponIndex; float halfAngle = BasicAttackSweepAngle(weaponIndex) * MathF.PI / 360f;
                if (Math.Abs(BasicAttackArcThreshold(weaponIndex) - MathF.Cos(halfAngle)) > 0.0001f) throw new InvalidOperationException("O ângulo do golpe visual divergiu do limite do ataque.");
                var outside = new Enemy { Kind = EnemyKind.Grunt, Pos = new PointF(150 + MathF.Cos(halfAngle + 0.08f) * 40, 330 + MathF.Sin(halfAngle + 0.08f) * 40), Hp = 1000, MaxHp = 1000 };
                attackArcCheck.attackCd = 0; attackArcCheck.enemies.Clear(); attackArcCheck.enemies.Add(outside); attackArcCheck.Attack();
                if (outside.Hp != 1000) throw new InvalidOperationException($"A arma {weaponIndex} acertou fora do arco desenhado.");
                var inside = new Enemy { Kind = EnemyKind.Grunt, Pos = new PointF(150 + MathF.Cos(halfAngle - 0.08f) * 40, 330 + MathF.Sin(halfAngle - 0.08f) * 40), Hp = 1000, MaxHp = 1000 };
                attackArcCheck.attackCd = 0; attackArcCheck.enemies.Clear(); attackArcCheck.enemies.Add(inside); attackArcCheck.Attack();
                if (inside.Hp >= 1000) throw new InvalidOperationException($"A arma {weaponIndex} não acertou dentro do arco desenhado.");
            }
        }
        using (var objectiveCheck = new Game())
        {
            objectiveCheck.timer.Stop(); objectiveCheck.StartGame(); objectiveCheck.storyOverlay = false; objectiveCheck.room = 33; objectiveCheck.enemies.Clear(); objectiveCheck.drops.Clear(); objectiveCheck.enemies.Add(new Enemy());
            if (objectiveCheck.LanternObjective() != "OBJETIVO: ATRAVESSE AS LANTERNAS BAIXAS") throw new InvalidOperationException("O objetivo da Passagem das Lanternas não acompanhou o combate.");
            objectiveCheck.enemies.Clear();
            if (objectiveCheck.LanternObjective() != "OBJETIVO: SIGA ATÉ O OBSERVATÓRIO") throw new InvalidOperationException("O objetivo da Passagem das Lanternas não indicou a saída após a limpeza.");
            objectiveCheck.room = 34;
            if (objectiveCheck.LanternObjective() != "OBJETIVO: DERROTE O REGENTE E RECUPERE O SINAL") throw new InvalidOperationException("O objetivo do Observatório não indicou o confronto contra o Regente.");
            objectiveCheck.lanternSignal = true;
            if (objectiveCheck.LanternObjective() != "OBJETIVO: LEVE O SINAL DA LUZ A AVEN") throw new InvalidOperationException("O objetivo do Observatório não atualizou após recuperar o sinal.");
        }
        using (var focusCheck = new Game())
        {
            focusCheck.timer.Stop(); focusCheck.StartGame(); focusCheck.storyOverlay = false; focusCheck.room = 3; focusCheck.SetupRoom(); focusCheck.player = new PointF(500, 330); focusCheck.skillCd = 1; focusCheck.down.Add(Keys.W); focusCheck.dashTime = 0.1f;
            focusCheck.PauseForLostFocus(); focusCheck.TickGame();
            if (focusCheck.player != new PointF(500, 330) || focusCheck.skillCd != 1 || focusCheck.down.Count != 0 || focusCheck.dashTime != 0) throw new InvalidOperationException("A perda de foco não pausou a partida ou manteve uma entrada presa.");
            focusCheck.ResumeAfterFocus(); focusCheck.TickGame();
            if (focusCheck.player != new PointF(500, 330) || focusCheck.skillCd >= 1) throw new InvalidOperationException("A retomada de foco não preservou a posição ou não retomou o combate.");
        }
        foreach (EnemyKind kind in new[] { EnemyKind.Guardian, EnemyKind.BlindScribe, EnemyKind.RiverFerryman, EnemyKind.AshWarden, EnemyKind.HollowName, EnemyKind.NameWeaver, EnemyKind.FirstKeeper, EnemyKind.OathCartographer, EnemyKind.NameBroker, EnemyKind.TideWitness, EnemyKind.ReedKeeper, EnemyKind.FlowKeeper, EnemyKind.CounterCurrent, EnemyKind.RiftCantor, EnemyKind.GlassHerald, EnemyKind.LanternRegent, EnemyKind.RouteScribe, EnemyKind.DeepSilence, EnemyKind.LastTuner })
        {
            for (int pattern = 0; pattern < 3; pattern++)
            {
                var boss = new Enemy { Kind = kind, BossPattern = pattern, Pos = new PointF(300, 300), Target = new PointF(500, 300), Target2Start = new PointF(300, 400), Target2 = new PointF(500, 400) };
                if (BossUsesCone(boss))
                {
                    if (!BossAttackContains(boss, new PointF(480, 300)) || BossAttackContains(boss, new PointF(400, 450)) || BossAttackContains(boss, new PointF(560, 300))) throw new InvalidOperationException("O cone do Arauto de Vidro não respeitou seu alcance ou abertura lateral.");
                }
                else if (BossUsesRing(boss))
                {
                    float outsideRing = BossCircleRadius(boss) + 1;
                    if (BossAttackContains(boss, boss.Pos) || !BossAttackContains(boss, new PointF(400, 300)) || BossAttackContains(boss, new PointF(boss.Pos.X + outsideRing, boss.Pos.Y))) throw new InvalidOperationException($"A onda de {kind} não preservou o miolo seguro nem o limite externo.");
                }
                else if (BossUsesLine(boss))
                {
                    float width = kind == EnemyKind.FirstKeeper ? 25 : 24;
                    if (!BossAttackContains(boss, new PointF(400, 300 + width - 1)) || BossAttackContains(boss, new PointF(400, 300 + width + 1))) throw new InvalidOperationException($"A largura do ataque de {kind} não corresponde ao aviso.");
                    if (kind is EnemyKind.FirstKeeper or EnemyKind.DeepSilence or EnemyKind.LastTuner && !BossAttackContains(boss, new PointF(400, 400))) throw new InvalidOperationException($"A segunda linha de {kind} não causa dano.");
                }
                else
                {
                    float radius = BossCircleRadius(boss); PointF center = pattern == 0 ? boss.Pos : boss.Target;
                    if (!BossAttackContains(boss, new PointF(center.X + radius - 1, center.Y)) || BossAttackContains(boss, new PointF(center.X + radius + 1, center.Y))) throw new InvalidOperationException($"O círculo de {kind} não corresponde ao impacto.");
                }
            }
        }
        using (var tunerCheck = new Game())
        {
            tunerCheck.timer.Stop(); tunerCheck.StartGame(); tunerCheck.storyOverlay = false; tunerCheck.room = 42; tunerCheck.questStage = 57; tunerCheck.SetupRoom();
            Enemy lastTunerCheckEnemy = tunerCheck.enemies.Single(); lastTunerCheckEnemy.Pos = new PointF(480, 300); tunerCheck.player = new PointF(650, 300);
            lastTunerCheckEnemy.Cooldown = 0; tunerCheck.UpdateEnemy(lastTunerCheckEnemy, 0.01f);
            if (lastTunerCheckEnemy.BossPattern != 1 || BossAttackContains(lastTunerCheckEnemy, new PointF(480, 300)) || !BossAttackContains(lastTunerCheckEnemy, tunerCheck.player)) throw new InvalidOperationException("O Afinador do Contratempo não anunciou a marca de impacto no ponto escolhido.");
            lastTunerCheckEnemy.Windup = 0; lastTunerCheckEnemy.Cooldown = 0; tunerCheck.UpdateEnemy(lastTunerCheckEnemy, 0.01f);
            if (lastTunerCheckEnemy.BossPattern != 2 || !BossUsesLine(lastTunerCheckEnemy) || !BossAttackContains(lastTunerCheckEnemy, tunerCheck.player)) throw new InvalidOperationException("Os cortes cruzados do Afinador não cobriram o ponto marcado.");
            lastTunerCheckEnemy.Windup = 0; lastTunerCheckEnemy.Cooldown = 0; tunerCheck.UpdateEnemy(lastTunerCheckEnemy, 0.01f);
            if (lastTunerCheckEnemy.BossPattern != 0 || !BossUsesRing(lastTunerCheckEnemy) || BossAttackContains(lastTunerCheckEnemy, lastTunerCheckEnemy.Pos)) throw new InvalidOperationException("A sequência do Afinador não voltou à onda anular com centro seguro.");
            lastTunerCheckEnemy.Hp = lastTunerCheckEnemy.MaxHp * 0.49f; lastTunerCheckEnemy.Windup = 0; lastTunerCheckEnemy.Cooldown = 0; tunerCheck.UpdateEnemy(lastTunerCheckEnemy, 0.01f);
            if (lastTunerCheckEnemy.WindupMax >= 0.6f || lastTunerCheckEnemy.Cooldown >= 1.5f) throw new InvalidOperationException("A segunda fase do Afinador não acelerou seus ataques.");
        }
        using (var rangedCheck = new Game())
        {
            rangedCheck.timer.Stop(); rangedCheck.StartGame(); rangedCheck.storyOverlay = false; rangedCheck.enemies.Clear();
            foreach (EnemyKind kind in new[] { EnemyKind.Archer, EnemyKind.Mage, EnemyKind.InkEcho, EnemyKind.DrownedMemory, EnemyKind.EmberWisp, EnemyKind.NameMoth, EnemyKind.MudCantor, EnemyKind.PaperWraith, EnemyKind.SaltEcho, EnemyKind.CooldownLeech })
            {
                float radius = RangedImpactRadius(kind);
                var attacker = new Enemy { Kind = kind, Pos = new PointF(300, 330), Target = new PointF(500, 330), Windup = 0.01f, WindupMax = 0.5f, Hp = 100, MaxHp = 100 };
                rangedCheck.health = rangedCheck.maxHealth = 1000; rangedCheck.invulnerable = rangedCheck.dashTime = 0; rangedCheck.player = new PointF(500 + radius + 1, 330);
                rangedCheck.UpdateEnemy(attacker, 0.02f);
                if (rangedCheck.health != 1000) throw new InvalidOperationException($"{kind} atingiu fora do círculo anunciado.");
                attacker.Windup = 0.01f; rangedCheck.player = new PointF(500 + radius - 1, 330); rangedCheck.UpdateEnemy(attacker, 0.02f);
                if (rangedCheck.health >= 1000) throw new InvalidOperationException($"{kind} não atingiu dentro do círculo anunciado.");
            }
        }
        using (var cooldownLeechCheck = new Game())
        {
            cooldownLeechCheck.timer.Stop(); cooldownLeechCheck.StartGame(); cooldownLeechCheck.storyOverlay = false;
            foreach (int leechRoom in new[] { 14, 21 })
            {
                cooldownLeechCheck.room = leechRoom; cooldownLeechCheck.SetupRoom();
                if (cooldownLeechCheck.enemies.Count(enemy => enemy.Kind == EnemyKind.CooldownLeech) != 1 || EnemyName(cooldownLeechCheck.enemies.Single(enemy => enemy.Kind == EnemyKind.CooldownLeech)) != "MORDENTE DE COMPASSO") throw new InvalidOperationException($"A sala {leechRoom} não posicionou corretamente o Mordente de Compasso.");
            }
            cooldownLeechCheck.player = new PointF(200, 330); cooldownLeechCheck.health = cooldownLeechCheck.maxHealth = 100; cooldownLeechCheck.invulnerable = cooldownLeechCheck.dashTime = 0;
            var leech = new Enemy { Kind = EnemyKind.CooldownLeech, Pos = new PointF(430, 330), Cooldown = 0, Hp = 72, MaxHp = 72 };
            cooldownLeechCheck.UpdateEnemy(leech, 0.01f);
            if (Math.Abs(leech.WindupMax - 1.05f) > 0.001f || RangedImpactRadius(leech.Kind) != 42 || NonBossAttackRadius(leech) != 42) throw new InvalidOperationException("O Mordente não anunciou a área de recarga com antecedência e alcance definidos.");
            leech.Windup = 0.01f; leech.Target = cooldownLeechCheck.player; cooldownLeechCheck.skillCd = 2; cooldownLeechCheck.shardCd = 3;
            cooldownLeechCheck.UpdateEnemy(leech, 0.02f);
            if (cooldownLeechCheck.health != 86 || Math.Abs(cooldownLeechCheck.skillCd - 3.1f) > 0.001f || Math.Abs(cooldownLeechCheck.shardCd - 4.1f) > 0.001f || cooldownLeechCheck.bellCd != 0) throw new InvalidOperationException("O impacto do Mordente não causou dano e atraso apenas às técnicas em recarga.");
        }
        using (var slagWeaverCheck = new Game())
        {
            slagWeaverCheck.timer.Stop(); slagWeaverCheck.StartGame(); slagWeaverCheck.storyOverlay = false;
            foreach (int slagRoom in new[] { 27, 31, 41 })
            {
                slagWeaverCheck.room = slagRoom; slagWeaverCheck.SetupRoom();
                Enemy sower = slagWeaverCheck.enemies.SingleOrDefault(enemy => enemy.Kind == EnemyKind.SlagWeaver) ?? throw new InvalidOperationException($"A sala {slagRoom} não posicionou o Eco de Escória.");
                if (sower.Hp != 88 || EnemyName(sower) != "ECO DE ESCÓRIA") throw new InvalidOperationException("O Eco de Escória não preservou sua vida ou identificação.");
            }
            var sowerCheck = new Enemy { Kind = EnemyKind.SlagWeaver, Pos = new PointF(300, 330), Cooldown = 0, Hp = 88, MaxHp = 88 };
            slagWeaverCheck.enemies.Clear(); slagWeaverCheck.enemies.Add(sowerCheck); slagWeaverCheck.player = new PointF(500, 330);
            slagWeaverCheck.UpdateEnemy(sowerCheck, 0.01f);
            if (Math.Abs(sowerCheck.WindupMax - 0.95f) > 0.001f || RangedImpactRadius(sowerCheck.Kind) != 48 || NonBossAttackRadius(sowerCheck) != 48) throw new InvalidOperationException("O Eco de Escória não anunciou corretamente o círculo de 48 pixels.");
            slagWeaverCheck.player = new PointF(560, 330); slagWeaverCheck.health = slagWeaverCheck.maxHealth = 1000; slagWeaverCheck.invulnerable = slagWeaverCheck.dashTime = 0;
            sowerCheck.Windup = 0.01f; slagWeaverCheck.UpdateEnemy(sowerCheck, 0.02f);
            if (slagWeaverCheck.health != 1000 || slagWeaverCheck.groundHazards.Count != 1 || Distance(slagWeaverCheck.groundHazards[0].Pos, new PointF(500, 330)) > 0.01f) throw new InvalidOperationException("O Eco de Escória não deixou a área persistente no ponto marcado ou atingiu fora do círculo.");
            GroundHazard slagPool = slagWeaverCheck.groundHazards[0];
            if (slagPool.Radius != 48 || slagPool.Damage != 9 || Math.Abs(slagPool.TimeLeft - 3.2f) > 0.03f) throw new InvalidOperationException("A poça de escória não preservou raio, dano e duração esperados.");
            slagPool.TimeLeft = 4.5f;
            slagWeaverCheck.player = slagPool.Pos; slagWeaverCheck.TickGroundHazards(0.8f);
            if (slagWeaverCheck.health != 991) throw new InvalidOperationException("A poça de escória não causou dano ao jogador que permaneceu sobre ela.");
            slagWeaverCheck.invulnerable = 0; slagWeaverCheck.player = new PointF(600, 330); slagWeaverCheck.TickGroundHazards(0.86f);
            if (slagWeaverCheck.health != 991) throw new InvalidOperationException("A poça de escória atingiu o jogador depois que ele saiu da área.");
            slagWeaverCheck.invulnerable = 0; slagWeaverCheck.player = slagPool.Pos; slagWeaverCheck.dashTime = 1; slagWeaverCheck.TickGroundHazards(0.86f);
            if (slagWeaverCheck.health != 991) throw new InvalidOperationException("A esquiva não protegeu contra o dano periódico da poça.");
            slagWeaverCheck.invulnerable = 0; slagWeaverCheck.dashTime = 0; slagWeaverCheck.TickGroundHazards(0.86f);
            if (slagWeaverCheck.health != 982) throw new InvalidOperationException("A poça de escória não repetiu o dano depois da esquiva.");
            slagWeaverCheck.TickGroundHazards(1.6f);
            if (slagWeaverCheck.groundHazards.Count != 0) throw new InvalidOperationException("A poça de escória não desapareceu ao fim de sua duração.");
            slagWeaverCheck.player = new PointF(500, 330); slagWeaverCheck.health = slagWeaverCheck.maxHealth = 1000; slagWeaverCheck.invulnerable = slagWeaverCheck.dashTime = 0;
            sowerCheck.Target = slagWeaverCheck.player; sowerCheck.Windup = 0.01f; slagWeaverCheck.UpdateEnemy(sowerCheck, 0.02f);
            if (slagWeaverCheck.health != 982 || slagWeaverCheck.groundHazards.Count != 1) throw new InvalidOperationException("O impacto direto do Eco de Escória não causou 18 de dano e deixou sua poça.");
        }
        using (var meleeCheck = new Game())
        {
            meleeCheck.timer.Stop(); meleeCheck.StartGame(); meleeCheck.storyOverlay = false; meleeCheck.enemies.Clear();
            foreach (EnemyKind kind in new[] { EnemyKind.Grunt, EnemyKind.Elite, EnemyKind.Mage })
            {
                var attacker = new Enemy { Kind = kind, Pos = new PointF(300, 330), Target = new PointF(500, 330), Windup = 0.01f, WindupMax = 0.5f, Hp = 100, MaxHp = 100 };
                PointF center = NonBossAttackCenter(attacker); float radius = NonBossAttackRadius(attacker);
                meleeCheck.health = meleeCheck.maxHealth = 1000; meleeCheck.invulnerable = meleeCheck.dashTime = 0; meleeCheck.player = new PointF(center.X + radius + 1, center.Y);
                meleeCheck.UpdateEnemy(attacker, 0.02f);
                if (meleeCheck.health != 1000) throw new InvalidOperationException($"{kind} atingiu fora do círculo anunciado.");
                if (attacker.AttackPoseTime <= 0 || EnemyAttackSpriteFrame(attacker) != 3) throw new InvalidOperationException($"{kind} não manteve o quadro de impacto após o aviso.");
                attacker.Windup = 0.01f; meleeCheck.player = new PointF(center.X + radius - 1, center.Y); meleeCheck.UpdateEnemy(attacker, 0.02f);
                if (meleeCheck.health >= 1000) throw new InvalidOperationException($"{kind} não atingiu dentro do círculo anunciado.");
            }
        }
        string saveCheckDirectory = Path.Combine(Path.GetTempPath(), "Scadryk-save-check-" + Guid.NewGuid().ToString("N"));
        string saveCheckPath = Path.Combine(saveCheckDirectory, "save.json");
        try
        {
            WriteSaveFile(saveCheckPath, "primeiro");
            WriteSaveFile(saveCheckPath, "segundo");
            if (File.ReadAllText(saveCheckPath) != "segundo") throw new InvalidOperationException("O save não substituiu o progresso anterior.");
            using (var lockedSave = new FileStream(saveCheckPath, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                bool rejected = false;
                try { WriteSaveFile(saveCheckPath, "não deve substituir"); } catch (IOException) { rejected = true; } catch (UnauthorizedAccessException) { rejected = true; }
                if (!rejected) throw new InvalidOperationException("O save bloqueado não rejeitou a substituição.");
                using var failedSaveGame = new Game(); failedSaveGame.timer.Stop(); failedSaveGame.StartGame();
                if (failedSaveGame.SaveGame(saveCheckPath) || !failedSaveGame.notice.Contains("NÃO FOI POSSÍVEL SALVAR")) throw new InvalidOperationException("A falha de salvar não foi comunicada ao jogador.");
            }
            if (File.ReadAllText(saveCheckPath) != "segundo" || Directory.GetFiles(saveCheckDirectory).Length != 1) throw new InvalidOperationException("Uma falha de gravação alterou o save anterior ou deixou arquivo temporário.");
        }
        finally { if (File.Exists(saveCheckPath)) File.Delete(saveCheckPath); if (Directory.Exists(saveCheckDirectory)) Directory.Delete(saveCheckDirectory); }
        string spritePath = Path.Combine(AppContext.BaseDirectory, "Assets", "Listener.png");
        if (!File.Exists(spritePath)) throw new InvalidOperationException("A folha original de sprites de Aven não foi copiada para a saída do jogo.");
        using (var sprite = new Bitmap(spritePath))
            if (sprite.Width != sprite.Height || sprite.GetPixel(0, 0).A != 0) throw new InvalidOperationException("A folha de sprites do Escutador precisa ter células quadradas e fundo transparente.");
        string enemySpritePath = Path.Combine(AppContext.BaseDirectory, "Assets", "Enemies.png");
        if (!File.Exists(enemySpritePath)) throw new InvalidOperationException("A folha de inimigos não foi copiada para a saída do jogo.");
        using (var sprite = new Bitmap(enemySpritePath))
            if (sprite.Width != sprite.Height || sprite.GetPixel(0, 0).A != 0) throw new InvalidOperationException("A folha de inimigos precisa ter células quadradas e fundo transparente.");
        if (EnemySpriteRow(EnemyKind.Grunt) != 0 || EnemySpriteRow(EnemyKind.Archer) != 1 || EnemySpriteRow(EnemyKind.Mage) != 2 || EnemySpriteRow(EnemyKind.Elite) != 3 || EnemySpriteFrame(0.52f, false) != 0 || EnemySpriteFrame(0.52f, true) != 0 || EnemySpriteFrame(0.66f, true) != 1 || !EnemyFacesLeft(400, 300) || EnemyFacesLeft(300, 400))
            throw new InvalidOperationException("Os arquétipos e o ciclo de caminhada dos sprites de inimigos não foram mapeados corretamente.");
        var attackPoseShapes = new HashSet<(float Width, float Height, float OffsetX, float OffsetY)>();
        foreach (EnemyKind kind in new[] { EnemyKind.DrownedMemory, EnemyKind.EmberWisp, EnemyKind.NameMoth, EnemyKind.MudCantor, EnemyKind.PaperWraith, EnemyKind.SaltEcho })
        {
            var creature = new Enemy { Kind = kind, AnimationTime = 0.5f };
            var idlePose = EnemyAttackPose(creature, 0); var attackPose = EnemyAttackPose(creature, 1);
            if (idlePose != (1, 1, 0, 0) || attackPose == idlePose || !attackPoseShapes.Add(attackPose)) throw new InvalidOperationException($"A silhueta de {kind} não recebeu uma pose de ataque própria.");
        }
        foreach (var (kind, pattern) in new[] { (EnemyKind.GlassHerald, 2), (EnemyKind.RouteScribe, 0), (EnemyKind.LastTuner, 0), (EnemyKind.DeepSilence, 2) })
        {
            var boss = new Enemy { Kind = kind, BossPattern = pattern };
            var idlePose = EnemyAttackPose(boss, 0); var attackPose = EnemyAttackPose(boss, 1);
            if (idlePose != (1, 1, 0, 0) || attackPose == idlePose || !attackPoseShapes.Add(attackPose)) throw new InvalidOperationException($"O padrão {pattern} do chefe {kind} não recebeu uma pose própria.");
        }
        using (var proceduralArt = new Bitmap(96, 96))
        using (var proceduralGraphics = Graphics.FromImage(proceduralArt))
        {
            proceduralGraphics.SmoothingMode = SmoothingMode.AntiAlias;
            foreach (EnemyKind kind in new[] { EnemyKind.InkEcho, EnemyKind.DrownedMemory, EnemyKind.EmberWisp, EnemyKind.NameMoth, EnemyKind.MudCantor, EnemyKind.PaperWraith, EnemyKind.SaltEcho, EnemyKind.BorrowedChorus, EnemyKind.SlagWeaver })
            {
                proceduralGraphics.Clear(Color.Transparent);
                if (!DrawProceduralEnemy(proceduralGraphics, new Enemy { Kind = kind, Pos = new PointF(48, 48) }, Color.SlateGray)) throw new InvalidOperationException($"A silhueta procedural de {kind} não foi desenhada.");
                int visiblePixels = 0;
                for (int py = 0; py < proceduralArt.Height; py++) for (int px = 0; px < proceduralArt.Width; px++) if (proceduralArt.GetPixel(px, py).A > 0) visiblePixels++;
                if (visiblePixels < 25) throw new InvalidOperationException($"A silhueta de {kind} ficou invisível na renderização 2D.");
            }
        }
        string bossSpritePath = Path.Combine(AppContext.BaseDirectory, "Assets", "Bosses.png");
        if (!File.Exists(bossSpritePath)) throw new InvalidOperationException("A folha de chefes da campanha não foi copiada para a saída do jogo.");
        using (var sprite = new Bitmap(bossSpritePath))
            if (sprite.Width != sprite.Height || sprite.GetPixel(0, 0).A != 0) throw new InvalidOperationException("A folha de chefes precisa ter células quadradas e fundo transparente.");
        if (CampaignBossSpriteIndex(EnemyKind.Guardian) != 0 || CampaignBossSpriteIndex(EnemyKind.BlindScribe) != 1 || CampaignBossSpriteIndex(EnemyKind.RiverFerryman) != 2 || CampaignBossSpriteIndex(EnemyKind.AshWarden) != 3 || CampaignBossSpriteIndex(EnemyKind.HollowName) != 4 || CampaignBossSpriteIndex(EnemyKind.NameWeaver) != 5 || CampaignBossSpriteIndex(EnemyKind.FirstKeeper) != 6 || CampaignBossSpriteIndex(EnemyKind.OathCartographer) != 7 || CampaignBossSpriteIndex(EnemyKind.NameBroker) != 8 || CampaignBossSpriteIndex(EnemyKind.TideWitness) != 9 || CampaignBossSpriteIndex(EnemyKind.ReedKeeper) != 10 || CampaignBossSpriteIndex(EnemyKind.FlowKeeper) != 11 || CampaignBossSpriteIndex(EnemyKind.CounterCurrent) != 12 || CampaignBossSpriteIndex(EnemyKind.RiftCantor) != 13 || CampaignBossSpriteIndex(EnemyKind.GlassHerald) != 14 || CampaignBossSpriteIndex(EnemyKind.LanternRegent) != 15 || CampaignBossSpriteIndex(EnemyKind.RouteScribe) != 16 || CampaignBossSpriteIndex(EnemyKind.DeepSilence) != 17)
            throw new InvalidOperationException("As silhuetas dos chefes da campanha foram atribuídas incorretamente.");
        string midBossSpritePath = Path.Combine(AppContext.BaseDirectory, "Assets", "BossesMidgame.png");
        if (!File.Exists(midBossSpritePath)) throw new InvalidOperationException("A folha de chefes intermediários não foi copiada para a saída do jogo.");
        using (var sprite = new Bitmap(midBossSpritePath))
            if (sprite.Width != sprite.Height || sprite.GetPixel(0, 0).A != 0) throw new InvalidOperationException("A folha de chefes intermediários precisa ter células quadradas e fundo transparente.");
        string lateBossSpritePath = Path.Combine(AppContext.BaseDirectory, "Assets", "BossesLate.png");
        if (!File.Exists(lateBossSpritePath)) throw new InvalidOperationException("A folha de chefes avançados não foi copiada para a saída do jogo.");
        using (var sprite = new Bitmap(lateBossSpritePath))
            if (sprite.Width != sprite.Height || sprite.GetPixel(0, 0).A != 0) throw new InvalidOperationException("A folha de chefes avançados precisa ter células quadradas e fundo transparente.");
        string finalWaveBossSpritePath = Path.Combine(AppContext.BaseDirectory, "Assets", "BossesFinalWave.png");
        if (!File.Exists(finalWaveBossSpritePath)) throw new InvalidOperationException("A folha dos chefes finais não foi copiada para a saída do jogo.");
        using (var sprite = new Bitmap(finalWaveBossSpritePath))
            if (sprite.Width != sprite.Height || sprite.GetPixel(0, 0).A != 0) throw new InvalidOperationException("A folha dos chefes finais precisa ter células quadradas e fundo transparente.");
        string endgameBossSpritePath = Path.Combine(AppContext.BaseDirectory, "Assets", "BossesEndgame.png");
        if (!File.Exists(endgameBossSpritePath)) throw new InvalidOperationException("A folha do confronto final não foi copiada para a saída do jogo.");
        using (var sprite = new Bitmap(endgameBossSpritePath))
            if (sprite.Width != sprite.Height || sprite.GetPixel(0, 0).A != 0) throw new InvalidOperationException("A folha do confronto final precisa ter células quadradas e fundo transparente.");
        if (PlayerSpriteRow(new PointF(0, 1)) != 0 || PlayerSpriteRow(new PointF(-1, 0)) != 1 || PlayerSpriteRow(new PointF(1, 0)) != 2 || PlayerSpriteRow(new PointF(0, -1)) != 3 || PlayerSpriteFrame(0.5f, false) != 0 || PlayerSpriteFrame(0.5f, true) != 0 || PlayerSpriteFrame(0.61f, true) != 1)
            throw new InvalidOperationException("A orientação ou o ciclo de caminhada da folha de sprites não foi mapeado corretamente.");
        using (var weaponCombat = new Game())
        {
            weaponCombat.timer.Stop(); weaponCombat.StartGame(); weaponCombat.storyOverlay = false; weaponCombat.room = 0; weaponCombat.SetupRoom(); weaponCombat.enemies.Clear(); weaponCombat.damage = 100; weaponCombat.player = new PointF(150, 330); weaponCombat.mouseAim = new PointF(1, 0); weaponCombat.weapon = 1;
            Enemy platedEcho = new() { Pos = new PointF(245, 330), Kind = EnemyKind.Grunt, Hp = 1000, MaxHp = 1000, Armored = true };
            weaponCombat.enemies.Add(platedEcho); weaponCombat.Attack();
            if (platedEcho.Hp != 910 && platedEcho.Hp != 820 || platedEcho.Stagger <= 0 || weaponCombat.attackFlash <= 0 || weaponCombat.BasicAttackDamageMultiplier(platedEcho) != 0.9f || weaponCombat.BasicAttackCooldown() != 0.52f || weaponCombat.BasicAttackReach(platedEcho) != 108f) throw new InvalidOperationException("A Lâmina do Arquivo não alcançou, perfurou a armadura, interrompeu o eco e iniciou a animação do golpe.");
            string shownHit = weaponCombat.floatingTexts.Single().Text;
            if (shownHit != (platedEcho.Hp == 820 ? "CRÍTICO 180" : "90")) throw new InvalidOperationException("O número de dano não corresponde ao golpe nem identifica o crítico.");
            weaponCombat.attackCd = 0; weaponCombat.weapon = 0; platedEcho.Hp = 1000; platedEcho.Stagger = 0; platedEcho.Pos = new PointF(245, 330); weaponCombat.Attack();
            if (platedEcho.Hp != 1000 || weaponCombat.BasicAttackReach(platedEcho) != 78f) throw new InvalidOperationException("A Espada de Pedra não manteve seu alcance equilibrado.");
            weaponCombat.attackCd = 0; weaponCombat.weapon = 2; platedEcho.Pos = new PointF(215, 330); weaponCombat.Attack();
            if (platedEcho.Hp >= 1000 || weaponCombat.BasicAttackCooldown() != 0.23f || weaponCombat.CriticalChance() != 0.25f || weaponCombat.WeaponTraitDescription(2) != "FACA RÁPIDA • CURTO ALCANCE • 25% DE CRÍTICO") throw new InvalidOperationException("A Faca do Crepúsculo não manteve seu alcance curto, velocidade e crítico elevados.");
            weaponCombat.attackCd = 0; weaponCombat.weapon = 3; platedEcho.Hp = 1000; platedEcho.Pos = new PointF(260, 330); weaponCombat.Attack();
            if (platedEcho.Hp >= 1000 || weaponCombat.BasicAttackCooldown() != 0.43f || weaponCombat.BasicAttackReach(platedEcho) != 128f || weaponCombat.BasicAttackDamageMultiplier(platedEcho) != 1f || WeaponTraitSummary(3) != "alcance máximo • preciso • perfura") throw new InvalidOperationException("A Lança da Badalada não perfurou a armadura a longo alcance com seu perfil preciso.");
            weaponCombat.attackCd = 0; platedEcho.Hp = 1000; platedEcho.Pos = new PointF(235, 390); weaponCombat.Attack();
            if (platedEcho.Hp != 1000) throw new InvalidOperationException("A Lança da Badalada atingiu fora do arco estreito anunciado.");
            weaponCombat.attackCd = 0; weaponCombat.weapon = 4; platedEcho.Hp = 1000; platedEcho.Armored = false; platedEcho.Pos = new PointF(360, 330);
            var secondBowTarget = new Enemy { Kind = EnemyKind.Grunt, Pos = new PointF(390, 330), Hp = 1000, MaxHp = 1000 };
            weaponCombat.enemies.Clear(); weaponCombat.enemies.Add(platedEcho); weaponCombat.enemies.Add(secondBowTarget); weaponCombat.Attack();
            if (platedEcho.Hp != 1000 || secondBowTarget.Hp != 1000 || weaponCombat.bowArrows.Count != 1 || weaponCombat.rangedAttackTarget != platedEcho.Pos || weaponCombat.BasicAttackReach(platedEcho) != 238f || weaponCombat.BasicAttackCooldown() != 0.48f || WeaponTraitSummary(4) != "distância • preciso • fraco contra armadura") throw new InvalidOperationException("O Arco do Limiar não lançou um disparo contra o alvo mais próximo dentro do alcance.");
            weaponCombat.AdvanceBowArrows(0.1f);
            if (platedEcho.Hp != 1000 || weaponCombat.bowArrows.Count != 1) throw new InvalidOperationException("A flecha do Arco do Limiar não permaneceu em voo durante seu tempo de trajetória.");
            weaponCombat.AdvanceBowArrows(0.3f);
            if (platedEcho.Hp >= 1000 || secondBowTarget.Hp != 1000 || weaponCombat.bowArrows.Count != 0) throw new InvalidOperationException("A flecha não acertou apenas o alvo escolhido ao chegar ao destino.");
            weaponCombat.attackCd = 0; platedEcho.Hp = 1000; platedEcho.Pos = new PointF(380, 330); platedEcho.Armored = true; weaponCombat.enemies.Clear(); weaponCombat.enemies.Add(platedEcho); weaponCombat.Attack();
            weaponCombat.AdvanceBowArrows(0.4f);
            if (platedEcho.Hp is not (930 or 860) || weaponCombat.BasicAttackDamageMultiplier(platedEcho) != 0.7f || weaponCombat.rangedAttackTarget != platedEcho.Pos) throw new InvalidOperationException("O Arco do Limiar não aplicou seu dano reduzido contra armadura e seu alcance longo.");
            var dodgingTarget = new Enemy { Kind = EnemyKind.Grunt, Pos = new PointF(360, 330), Hp = 1000, MaxHp = 1000 };
            weaponCombat.attackCd = 0; weaponCombat.enemies.Clear(); weaponCombat.enemies.Add(dodgingTarget); weaponCombat.Attack(); dodgingTarget.Pos = new PointF(360, 410); weaponCombat.AdvanceBowArrows(0.4f);
            if (dodgingTarget.Hp != 1000 || weaponCombat.bowArrows.Count != 0) throw new InvalidOperationException("A flecha corrigiu o curso para perseguir um alvo que saiu da trajetória.");
            weaponCombat.enemies.Clear(); weaponCombat.enemies.Add(platedEcho);
            weaponCombat.attackCd = 0; platedEcho.Hp = 1000; platedEcho.Pos = new PointF(220, 390); weaponCombat.Attack();
            if (platedEcho.Hp != 1000) throw new InvalidOperationException("O Arco do Limiar atingiu fora do cone estreito anunciado.");
            weaponCombat.player = new PointF(150, 280); weaponCombat.facing = weaponCombat.mouseAim = new PointF(1, 0); platedEcho.Hp = 1000; platedEcho.Pos = new PointF(380, 280); weaponCombat.attackCd = 0; weaponCombat.Attack();
            if (!weaponCombat.BlockedLine(weaponCombat.player, platedEcho.Pos) || platedEcho.Hp != 1000 || weaponCombat.bowArrows.Count != 0) throw new InvalidOperationException("O Arco do Limiar atravessou uma parede ao escolher o alvo.");
            if (weaponCombat.BasicAttackReach(new Enemy { Kind = EnemyKind.Guardian }) != 260f) throw new InvalidOperationException("O Arco do Limiar não respeitou seu alcance especial contra chefes.");
            weaponCombat.weapon = 5; weaponCombat.player = new PointF(150, 330); weaponCombat.facing = weaponCombat.mouseAim = new PointF(1, 0); platedEcho.Pos = new PointF(225, 330); platedEcho.Hp = 1000; platedEcho.KnockbackTime = 0; platedEcho.KnockbackVelocity = PointF.Empty; weaponCombat.enemies.Clear(); weaponCombat.enemies.Add(platedEcho); weaponCombat.attackCd = 0; weaponCombat.Attack();
            if (platedEcho.Hp >= 1000 || platedEcho.KnockbackTime < 0.2f || platedEcho.KnockbackVelocity.X >= 0 || weaponCombat.BasicAttackCooldown() != 0.56f || weaponCombat.BasicAttackReach(platedEcho) != 92f || weaponCombat.WeaponTraitDescription(5) != "CORRENTE DE ALCANCE CURTO • ARCO AMPLO • PUXA ECOS COMUNS PARA PERTO") throw new InvalidOperationException("A Corrente do Vau não puxou o eco comum nem manteve o perfil amplo e lento.");
            var resistantBoss = new Enemy { Pos = new PointF(225, 330), Kind = EnemyKind.Guardian, Hp = 1000, MaxHp = 1000 }; weaponCombat.ApplyHitKnockback(resistantBoss, false, 5);
            if (resistantBoss.KnockbackVelocity.X <= 0) throw new InvalidOperationException("A Corrente do Vau puxou um chefe que deveria resistir ao controle.");
            if (!weaponCombat.WeaponTraitDescription(1).Contains("PERFURA ARMADURA") || !weaponCombat.WeaponTraitDescription(0).Contains("EQUILIBRADA")) throw new InvalidOperationException("A descrição das especializações de arma não corresponde ao combate.");
        }
        using (var floatingTextCheck = new Game())
        {
            floatingTextCheck.timer.Stop(); floatingTextCheck.StartGame(); floatingTextCheck.storyOverlay = false;
            var target = new Enemy { Pos = new PointF(500, 300), Kind = EnemyKind.Grunt, Hp = 100, MaxHp = 100 };
            floatingTextCheck.DamageEnemy(target, 37, true); floatingTextCheck.HitPlayer(12);
            if (target.Hp != 63 || floatingTextCheck.health != 88 || floatingTextCheck.floatingTexts.Count != 2 || floatingTextCheck.floatingTexts[0].Text != "CRÍTICO 37" || floatingTextCheck.floatingTexts[1].Text != "−12" || floatingTextCheck.floatingTexts[1].Color != Color.FromArgb(250, 116, 105))
                throw new InvalidOperationException("Os números flutuantes não distinguiram dano crítico de dano recebido.");
            if (PlayerDamageFlashAlpha(0, 0) != 0 || PlayerDamageFlashAlpha(floatingTextCheck.invulnerable, floatingTextCheck.windupPulse) is < 70 or > 125)
                throw new InvalidOperationException("O clarão de dano do jogador não acompanhou a janela de invulnerabilidade.");
            float initialY = floatingTextCheck.floatingTexts[0].Pos.Y; floatingTextCheck.TickFloatingTexts(0.3f);
            if (floatingTextCheck.floatingTexts.Count != 2 || floatingTextCheck.floatingTexts[0].Pos.Y >= initialY) throw new InvalidOperationException("Os números de dano não subiram e permaneceram visíveis durante o impacto.");
            floatingTextCheck.TickFloatingTexts(1f);
            if (floatingTextCheck.floatingTexts.Count != 0) throw new InvalidOperationException("Os números flutuantes não desapareceram depois da animação.");
        }
        using (var dodgeDirectionCheck = new Game())
        {
            dodgeDirectionCheck.timer.Stop(); dodgeDirectionCheck.StartGame(); dodgeDirectionCheck.storyOverlay = false; dodgeDirectionCheck.room = 3; dodgeDirectionCheck.SetupRoom(); dodgeDirectionCheck.player = new PointF(500, 330); dodgeDirectionCheck.down.Add(Keys.D); dodgeDirectionCheck.down.Add(Keys.W);
            dodgeDirectionCheck.StartDash(); PointF expectedDirection = Normalize(new PointF(1, -1), PointF.Empty); dodgeDirectionCheck.TickGame(); PointF diagonalDash = dodgeDirectionCheck.dashDirection, diagonalPosition = dodgeDirectionCheck.player;
            if (Math.Abs(diagonalDash.X - expectedDirection.X) > 0.001f || Math.Abs(diagonalDash.Y - expectedDirection.Y) > 0.001f || diagonalPosition.X <= 505 || diagonalPosition.Y >= 325)
                throw new InvalidOperationException("A esquiva diagonal não seguiu o movimento indicado pelo jogador.");
            dodgeDirectionCheck.down.Clear(); dodgeDirectionCheck.dashCd = 0; dodgeDirectionCheck.facing = new PointF(-1, 0); dodgeDirectionCheck.StartDash();
            PointF fallbackDash = dodgeDirectionCheck.dashDirection;
            if (Math.Abs(fallbackDash.X + 1) > 0.001f || Math.Abs(fallbackDash.Y) > 0.001f)
                throw new InvalidOperationException("A esquiva sem direção de movimento não usou a direção para a qual o personagem olha.");
        }
        using (var atlasCheck = new Game())
        {
            atlasCheck.timer.Stop(); atlasCheck.StartGame(); atlasCheck.storyOverlay = false; atlasCheck.room = 4; atlasCheck.questStage = 3;
            atlasCheck.enemies.Clear(); atlasCheck.clearedRooms = (1L << 0) | (1L << 1);
            var pausedEnemy = new Enemy { Pos = new PointF(700, 400), Kind = EnemyKind.Grunt, Hp = 100, MaxHp = 100, Stagger = 0.2f };
            atlasCheck.enemies.Add(pausedEnemy); PointF playerBeforeMap = atlasCheck.player; atlasCheck.skillCd = 1; atlasCheck.wardTime = 1;
            atlasCheck.OnKeyDown(atlasCheck, new KeyEventArgs(Keys.M)); long knownRooms = atlasCheck.KnownRoomMask(); atlasCheck.TickGame();
            atlasCheck.OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, 700, 400, 0));
            long expectedKnown = (1L << 0) | (1L << 1) | (1L << 3) | (1L << 4);
            if (!atlasCheck.mapOpen || (knownRooms & expectedKnown) != expectedKnown || (knownRooms & (1L << 5)) != 0 || atlasCheck.player != playerBeforeMap || pausedEnemy.Stagger != 0.2f || atlasCheck.skillCd != 1 || atlasCheck.wardTime != 1 || atlasCheck.attackCd != 0)
                throw new InvalidOperationException("O atlas não distinguiu câmaras conhecidas ou não pausou a exploração.");
            atlasCheck.OnKeyDown(atlasCheck, new KeyEventArgs(Keys.Escape));
            if (atlasCheck.mapOpen) throw new InvalidOperationException("ESC não fechou o atlas da campanha.");
            atlasCheck.TickGame(); if (atlasCheck.skillCd >= 1 || atlasCheck.wardTime >= 1) throw new InvalidOperationException("O combate não retomou os temporizadores ao fechar o atlas.");
        }
        using (var shardGame = new Game())
        {
            shardGame.timer.Stop(); shardGame.StartGame(); shardGame.storyOverlay = false; shardGame.room = 0; shardGame.SetupRoom(); shardGame.enemies.Clear(); shardGame.player = new PointF(150, 320); shardGame.mouseAim = new PointF(1, 0);
            var firstShardTarget = new Enemy { Pos = new PointF(230, 320), Kind = EnemyKind.Grunt, Hp = 300, MaxHp = 300 };
            var secondShardTarget = new Enemy { Pos = new PointF(300, 320), Kind = EnemyKind.Grunt, Hp = 300, MaxHp = 300 };
            shardGame.enemies.Add(firstShardTarget); shardGame.enemies.Add(secondShardTarget); shardGame.CastVoiceShard();
            if (shardGame.echoShards.Count != 1 || shardGame.shardCd != 7f) throw new InvalidOperationException("O Estilhaço de Voz não iniciou ou não recebeu recarga.");
            shardGame.AdvanceVoiceShards(0.4f);
            if (firstShardTarget.Hp >= 300 || secondShardTarget.Hp >= 300 || shardGame.echoShards.Count != 0) throw new InvalidOperationException($"O Estilhaço de Voz não atravessou os alvos até atingir uma parede: vida {firstShardTarget.Hp:0}/{secondShardTarget.Hp:0}, projéteis {shardGame.echoShards.Count}.");
            float shardDamage = shardGame.VoiceShardDamage(); shardGame.ownedCharms = 4; shardGame.equippedCharm = 3; shardGame.skillTalent = 2;
            if (shardGame.VoiceShardDamage() <= shardDamage || shardGame.VoiceShardCooldown() != 6f) throw new InvalidOperationException("O Vidro de Vigília ou o Domínio não alteraram a nova técnica.");
        }
        using (var skillLoadoutGame = new Game())
        {
            skillLoadoutGame.timer.Stop(); skillLoadoutGame.StartGame(); skillLoadoutGame.storyOverlay = false;
            skillLoadoutGame.skillMenuOpen = true; skillLoadoutGame.OnKeyDown(skillLoadoutGame, new KeyEventArgs(Keys.V)); skillLoadoutGame.OnKeyDown(skillLoadoutGame, new KeyEventArgs(Keys.D2));
            if (skillLoadoutGame.equippedSkills[1] != 0) throw new InvalidOperationException("Uma técnica bloqueada foi equipada antes de ser aprendida.");
            skillLoadoutGame.room = 6; skillLoadoutGame.riverBell = true; skillLoadoutGame.OnKeyDown(skillLoadoutGame, new KeyEventArgs(Keys.V)); skillLoadoutGame.OnKeyDown(skillLoadoutGame, new KeyEventArgs(Keys.D2)); skillLoadoutGame.skillMenuOpen = false;
            skillLoadoutGame.enemies.Clear(); var loadoutEcho = new Enemy { Pos = new PointF(210, 330), Kind = EnemyKind.Grunt, Hp = 1000, MaxHp = 1000 }; skillLoadoutGame.enemies.Add(loadoutEcho);
            skillLoadoutGame.player = new PointF(150, 330); skillLoadoutGame.CastEquippedSkill(1);
            if (skillLoadoutGame.equippedSkills[1] != 2 || skillLoadoutGame.bellCd <= 0 || loadoutEcho.Hp >= 1000) throw new InvalidOperationException("O espaço V não acionou a técnica equipada.");
            skillLoadoutGame.EquipSkill(2, 2);
            if (skillLoadoutGame.equippedSkills[2] == 2) throw new InvalidOperationException("Uma mesma técnica foi equipada em dois espaços.");
            SaveData loadoutSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(skillLoadoutGame.CaptureSaveData()))!;
            if (!ValidSkillLoadout(loadoutSave) || !loadoutSave.EquippedSkills.SequenceEqual(new[] { 1, 2, 0, 0 })) throw new InvalidOperationException("A configuração de técnicas não foi salva corretamente.");
            SaveData oldLoadoutSave = JsonSerializer.Deserialize<SaveData>("{}")!;
            if (oldLoadoutSave.EquippedSkills is null || !oldLoadoutSave.EquippedSkills.SequenceEqual(new[] { 1, 0, 0, 0 })) throw new InvalidOperationException("Um save antigo não recebeu a configuração padrão de técnicas.");
            loadoutSave.EquippedSkills[2] = 5;
            if (ValidSkillLoadout(loadoutSave)) throw new InvalidOperationException("Um save equipou uma técnica narrativa ainda bloqueada.");
            SaveData threeSlotSave = new() { EquippedSkills = new[] { 1, 2, 3 }, RiverBell = true, EbbSeal = true, QuestStage = 33 };
            if (!ValidSkillLoadout(threeSlotSave) || !NormalizeSkillLoadout(threeSlotSave.EquippedSkills).SequenceEqual(new[] { 1, 2, 3, 0 })) throw new InvalidOperationException("Um save anterior com três espaços não migrou para o quarto espaço vazio.");
            skillLoadoutGame.room = 26; skillLoadoutGame.ebbSeal = true; skillLoadoutGame.skillMenuOpen = true; skillLoadoutGame.OnKeyDown(skillLoadoutGame, new KeyEventArgs(Keys.C)); skillLoadoutGame.OnKeyDown(skillLoadoutGame, new KeyEventArgs(Keys.D3)); skillLoadoutGame.skillMenuOpen = false;
            skillLoadoutGame.enemies.Clear(); skillLoadoutGame.player = new PointF(300, 330); var fourthSlotEcho = new Enemy { Pos = new PointF(390, 330), Kind = EnemyKind.Grunt, Hp = 1000, MaxHp = 1000 }; skillLoadoutGame.enemies.Add(fourthSlotEcho); skillLoadoutGame.OnKeyDown(skillLoadoutGame, new KeyEventArgs(Keys.C));
            if (skillLoadoutGame.equippedSkills[3] != 3 || skillLoadoutGame.pullCd <= 0 || fourthSlotEcho.Hp >= 1000) throw new InvalidOperationException("O quarto espaço não equipou nem acionou a Vazante pela tecla C.");
            skillLoadoutGame.equippedSkills = new[] { 1, 2, 0, 0 }; skillLoadoutGame.beaconLight = true; skillLoadoutGame.alternateSkills = new[] { 3, 0, 0, 0 }; skillLoadoutGame.skillMenuOpen = true; skillLoadoutGame.OnKeyDown(skillLoadoutGame, new KeyEventArgs(Keys.Tab)); skillLoadoutGame.OnKeyDown(skillLoadoutGame, new KeyEventArgs(Keys.C)); skillLoadoutGame.OnKeyDown(skillLoadoutGame, new KeyEventArgs(Keys.D6));
            if (!skillLoadoutGame.alternateSkills.SequenceEqual(new[] { 3, 0, 0, 6 }) || !skillLoadoutGame.equippedSkills.SequenceEqual(new[] { 1, 2, 0, 0 })) throw new InvalidOperationException("A montagem alternativa não pôde ser editada sem alterar a principal.");
            skillLoadoutGame.skillMenuOpen = false; skillLoadoutGame.pullCd = 6; skillLoadoutGame.OnKeyDown(skillLoadoutGame, new KeyEventArgs(Keys.T));
            if (!skillLoadoutGame.alternateSkillsActive || !skillLoadoutGame.equippedSkills.SequenceEqual(new[] { 3, 0, 0, 6 }) || skillLoadoutGame.pullCd != 6) throw new InvalidOperationException("A troca de montagem não ativou a alternativa ou reiniciou recargas.");
            SaveData dualLoadoutSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(skillLoadoutGame.CaptureSaveData()))!;
            if (!ValidSkillLoadout(dualLoadoutSave) || !dualLoadoutSave.AlternateSkillsActive || !dualLoadoutSave.EquippedSkills.SequenceEqual(new[] { 1, 2, 0, 0 }) || !dualLoadoutSave.AlternateSkills.SequenceEqual(new[] { 3, 0, 0, 6 })) throw new InvalidOperationException("As duas montagens não foram salvas com a seleção ativa correta.");
            skillLoadoutGame.RestoreSkillLoadouts(dualLoadoutSave);
            if (!skillLoadoutGame.alternateSkillsActive || !skillLoadoutGame.equippedSkills.SequenceEqual(new[] { 3, 0, 0, 6 }) || !skillLoadoutGame.alternateSkills.SequenceEqual(new[] { 1, 2, 0, 0 })) throw new InvalidOperationException("O save não restaurou a montagem alternativa que estava ativa.");
            skillLoadoutGame.OnKeyDown(skillLoadoutGame, new KeyEventArgs(Keys.T));
            if (skillLoadoutGame.alternateSkillsActive || !skillLoadoutGame.equippedSkills.SequenceEqual(new[] { 1, 2, 0, 0 })) throw new InvalidOperationException("A tecla T não retornou à montagem principal.");
        }
        using (var ebbBeaconCombo = new Game())
        {
            ebbBeaconCombo.timer.Stop(); ebbBeaconCombo.StartGame(); ebbBeaconCombo.storyOverlay = false; ebbBeaconCombo.room = 26; ebbBeaconCombo.ebbSeal = ebbBeaconCombo.beaconLight = true; ebbBeaconCombo.equippedSkills = new[] { 3, 6, 0, 0 }; ebbBeaconCombo.player = new PointF(300, 330); ebbBeaconCombo.damage = 100; ebbBeaconCombo.enemies.Clear();
            var comboEcho = new Enemy { Pos = new PointF(400, 330), Kind = EnemyKind.Grunt, Hp = 1000, MaxHp = 1000 }; ebbBeaconCombo.enemies.Add(comboEcho);
            if (!ebbBeaconCombo.EbbBeaconSynergyActive()) throw new InvalidOperationException("Vazante + Farol não ativaram a sinergia da montagem.");
            ebbBeaconCombo.CastEbbPull(); float afterPull = comboEcho.Hp;
            if (comboEcho.BeaconMarkTime != 5f || !ebbBeaconCombo.notice.Contains("PREPARADOS PARA O FAROL")) throw new InvalidOperationException("A Vazante não marcou alvos ao combinar as duas técnicas.");
            ebbBeaconCombo.beaconPos = ebbBeaconCombo.player; ebbBeaconCombo.beaconTime = 6; ebbBeaconCombo.beaconPulseCd = 0.01f; ebbBeaconCombo.TickBeacon(0.02f);
            float expectedPulseDamage = 100 * 0.34f * 1.35f;
            if (Math.Abs(comboEcho.Hp - (afterPull - expectedPulseDamage)) > 0.01f || comboEcho.BeaconMarkTime != 0) throw new InvalidOperationException("O próximo pulso do Farol não consumiu a marca nem recebeu o bônus de 35%.");
            ebbBeaconCombo.equippedSkills = new[] { 3, 0, 0, 0 }; ebbBeaconCombo.pullCd = 0; var unlinkedEcho = new Enemy { Pos = new PointF(400, 330), Kind = EnemyKind.Grunt, Hp = 1000, MaxHp = 1000 }; ebbBeaconCombo.enemies.Clear(); ebbBeaconCombo.enemies.Add(unlinkedEcho); ebbBeaconCombo.CastEbbPull();
            if (unlinkedEcho.BeaconMarkTime != 0 || ebbBeaconCombo.notice.Contains("PREPARADOS PARA O FAROL")) throw new InvalidOperationException("A sinergia Vazante + Farol funcionou sem ambas as técnicas equipadas.");
        }
        using (var bellGame = new Game())
        {
            bellGame.timer.Stop(); bellGame.StartGame(); bellGame.storyOverlay = false; bellGame.room = 6; bellGame.riverBell = true; bellGame.skillTalent = 2; bellGame.equippedCharm = 2; bellGame.equippedCharm2 = 3; bellGame.player = new PointF(250, 320); bellGame.enemies.Clear();
            var nearbyEcho = new Enemy { Pos = new PointF(330, 320), Kind = EnemyKind.Grunt, Hp = 300, MaxHp = 300, Windup = 0.6f, Cooldown = 0.2f };
            var distantEcho = new Enemy { Pos = new PointF(455, 320), Kind = EnemyKind.Grunt, Hp = 300, MaxHp = 300 };
            var bellBoss = new Enemy { Pos = new PointF(350, 320), Kind = EnemyKind.NameBroker, Hp = 1000, MaxHp = 1000, Windup = 0.6f, WindupMax = 0.6f, BossPattern = 1 };
            bellGame.enemies.Add(nearbyEcho); bellGame.enemies.Add(distantEcho); bellGame.enemies.Add(bellBoss); bellGame.CastBellPulse();
            if (nearbyEcho.Hp >= 300 || nearbyEcho.Stagger <= 0 || nearbyEcho.Windup != 0 || distantEcho.Hp != 300 || bellBoss.Hp >= 1000 || bellBoss.Windup <= 0) throw new InvalidOperationException("A Ressonância do Sino não separou dano, alcance e interrupção de inimigos comuns e chefes.");
            if (Math.Abs(bellGame.bellCd - 14.1f) > 0.01f || bellGame.bellWaveTime <= 0 || bellGame.BellPulseRadius() != 190f) throw new InvalidOperationException("O Fio de Travessia, Domínio ou o aviso visual não alterou a Ressonância do Sino.");
            float echoHealth = nearbyEcho.Hp; bellGame.UpdateEnemy(nearbyEcho, 0.2f);
            if (nearbyEcho.Hp != echoHealth || nearbyEcho.Stagger >= 0.95f) throw new InvalidOperationException("O atordoamento da Ressonância não segurou o eco durante a janela prevista.");
            float cooldown = bellGame.bellCd; bellGame.CastBellPulse();
            if (bellGame.bellCd != cooldown) throw new InvalidOperationException("A Ressonância do Sino ignorou sua recarga.");
        }
        using (var bellShardSynergy = new Game())
        {
            bellShardSynergy.timer.Stop(); bellShardSynergy.StartGame(); bellShardSynergy.storyOverlay = false; bellShardSynergy.room = 6; bellShardSynergy.riverBell = true; bellShardSynergy.equippedSkills = new[] { 1, 2, 0, 0 }; bellShardSynergy.player = new PointF(250, 320); bellShardSynergy.mouseAim = new PointF(1, 0); bellShardSynergy.damage = 100; bellShardSynergy.enemies.Clear();
            var preparedEcho = new Enemy { Pos = new PointF(330, 320), Kind = EnemyKind.Grunt, Hp = 1000, MaxHp = 1000 }; bellShardSynergy.enemies.Add(preparedEcho);
            bellShardSynergy.CastBellPulse(); float healthBeforeShard = preparedEcho.Hp;
            if (preparedEcho.BellMarkTime != 5f || !bellShardSynergy.notice.Contains("PREPARADOS PARA O ESTILHAÇO")) throw new InvalidOperationException("Sino + Estilhaço não prepararam o alvo por cinco segundos.");
            bellShardSynergy.CastVoiceShard(); float expectedResonantDamage = bellShardSynergy.echoShards.Single().Damage * 1.35f;
            bellShardSynergy.AdvanceVoiceShards(0.2f);
            if (Math.Abs((healthBeforeShard - preparedEcho.Hp) - expectedResonantDamage) > 0.01f || preparedEcho.BellMarkTime != 0 || !bellShardSynergy.floatingTexts.Any(text => text.Text == "SINO + ESTILHAÇO")) throw new InvalidOperationException("Estilhaço não consumiu a marca do Sino com +35% de dano e aviso visual.");
            bellShardSynergy.equippedSkills = new[] { 1, 0, 0, 0 }; preparedEcho.BellMarkTime = 0; bellShardSynergy.bellCd = 0;
            bellShardSynergy.CastBellPulse();
            if (preparedEcho.BellMarkTime != 0 || bellShardSynergy.notice.Contains("PREPARADOS PARA O ESTILHAÇO")) throw new InvalidOperationException("Sino + Estilhaço funcionaram sem as duas técnicas equipadas.");
        }
        using (var bellRefrainSynergy = new Game())
        {
            bellRefrainSynergy.timer.Stop(); bellRefrainSynergy.StartGame(); bellRefrainSynergy.storyOverlay = false; bellRefrainSynergy.room = 6; bellRefrainSynergy.riverBell = bellRefrainSynergy.refrainSong = true; bellRefrainSynergy.equippedSkills = new[] { 2, 5, 0, 0 }; bellRefrainSynergy.player = new PointF(250, 320); bellRefrainSynergy.damage = 100; bellRefrainSynergy.enemies.Clear();
            var refrainedEcho = new Enemy { Pos = new PointF(330, 320), Kind = EnemyKind.Grunt, Hp = 1000, MaxHp = 1000 }; bellRefrainSynergy.enemies.Add(refrainedEcho);
            bellRefrainSynergy.CastBellPulse(); float beforeRefrain = refrainedEcho.Hp;
            if (refrainedEcho.BellMarkTime != 5f || !bellRefrainSynergy.notice.Contains("PREPARADOS PARA O CORO")) throw new InvalidOperationException("Sino + Coro não prepararam o alvo por cinco segundos.");
            bellRefrainSynergy.CastRefrain(); float expectedRefrain = 100 * 0.68f * bellRefrainSynergy.TechniqueDamageMultiplier() * bellRefrainSynergy.WeaponSkillMultiplier() * bellRefrainSynergy.SkillDamageVariant(5) * 1.35f * bellRefrainSynergy.OutgoingDamageMultiplier(refrainedEcho);
            if (Math.Abs((beforeRefrain - refrainedEcho.Hp) - expectedRefrain) > 0.01f || refrainedEcho.BellMarkTime != 0 || !bellRefrainSynergy.floatingTexts.Any(text => text.Text == "SINO + CORO") || !bellRefrainSynergy.notice.Contains("MARCAS DO SINO CONSUMIDAS")) throw new InvalidOperationException("O Coro não consumiu a marca do Sino com +35% de dano e aviso visual.");
            bellRefrainSynergy.equippedSkills = new[] { 5, 0, 0, 0 }; refrainedEcho.BellMarkTime = 0; bellRefrainSynergy.bellCd = 0; bellRefrainSynergy.CastBellPulse();
            if (refrainedEcho.BellMarkTime != 0 || bellRefrainSynergy.notice.Contains("PREPARADOS PARA O CORO")) throw new InvalidOperationException("Sino + Coro funcionaram sem as duas técnicas equipadas.");
        }
        using (var refrainBeaconSynergy = new Game())
        {
            refrainBeaconSynergy.timer.Stop(); refrainBeaconSynergy.StartGame(); refrainBeaconSynergy.storyOverlay = false; refrainBeaconSynergy.room = 6; refrainBeaconSynergy.refrainSong = refrainBeaconSynergy.beaconLight = true;
            refrainBeaconSynergy.equippedSkills = new[] { 5, 6, 0, 0 }; refrainBeaconSynergy.player = new PointF(250, 320); refrainBeaconSynergy.beaconPos = refrainBeaconSynergy.player; refrainBeaconSynergy.beaconTime = 4; refrainBeaconSynergy.beaconPulseCd = 0.8f; refrainBeaconSynergy.damage = 100;
            var beaconEcho = new Enemy { Pos = new PointF(330, 320), Kind = EnemyKind.Grunt, Hp = 10000, MaxHp = 10000 }; refrainBeaconSynergy.enemies.Add(beaconEcho);
            if (!refrainBeaconSynergy.RefrainBeaconSynergyActive()) throw new InvalidOperationException("Coro + Farol não ativou com as duas técnicas equipadas.");
            refrainBeaconSynergy.CastRefrain();
            if (Math.Abs(refrainBeaconSynergy.beaconPulseCd - 0.4f) > 0.001f || !refrainBeaconSynergy.floatingTexts.Any(text => text.Text == "CORO + FAROL") || !refrainBeaconSynergy.notice.Contains("PRÓXIMO PULSO ADIANTADO"))
                throw new InvalidOperationException("Coro não adiantou o pulso do Farol uma vez ao atingir um eco na área ativa.");
            refrainBeaconSynergy.refrainCd = 0; refrainBeaconSynergy.beaconPulseCd = 0.8f; refrainBeaconSynergy.beaconPos = new PointF(100, 320); refrainBeaconSynergy.CastRefrain();
            if (Math.Abs(refrainBeaconSynergy.beaconPulseCd - 0.8f) > 0.001f) throw new InvalidOperationException("Coro acelerou o Farol sem atingir um eco em seu alcance.");
            refrainBeaconSynergy.refrainCd = 0; refrainBeaconSynergy.beaconPos = refrainBeaconSynergy.player; refrainBeaconSynergy.equippedSkills = new[] { 5, 0, 0, 0 }; refrainBeaconSynergy.CastRefrain();
            if (Math.Abs(refrainBeaconSynergy.beaconPulseCd - 0.8f) > 0.001f) throw new InvalidOperationException("Coro + Farol funcionou sem as duas técnicas equipadas.");
        }
        using (var sparkShardSynergy = new Game())
        {
            sparkShardSynergy.timer.Stop(); sparkShardSynergy.StartGame(); sparkShardSynergy.storyOverlay = false; sparkShardSynergy.room = 0; sparkShardSynergy.SetupRoom(); sparkShardSynergy.enemies.Clear(); sparkShardSynergy.finalEnding = 2;
            sparkShardSynergy.equippedSkills = new[] { 1, 8, 0, 0 }; sparkShardSynergy.player = new PointF(250, 320); sparkShardSynergy.mouseAim = new PointF(1, 0); sparkShardSynergy.damage = 100;
            var sparkEcho = new Enemy { Pos = new PointF(340, 320), Kind = EnemyKind.Grunt, Hp = 10000, MaxHp = 10000 }; sparkShardSynergy.enemies.Add(sparkEcho); sparkShardSynergy.lockedTarget = sparkEcho;
            sparkShardSynergy.CastVoiceShard(); sparkShardSynergy.AdvanceVoiceShards(0.2f);
            if (sparkEcho.SparkMarkTime != 5f || !sparkShardSynergy.floatingTexts.Any(text => text.Text == "CENTELHA PREPARADA")) throw new InvalidOperationException("Estilhaço não preparou o alvo para Última Centelha.");
            float beforeSpark = sparkEcho.Hp;
            float expectedSparkDamage = 100 * 2.45f * sparkShardSynergy.SkillDamageVariant(8) * sparkShardSynergy.TechniqueDamageMultiplier() * sparkShardSynergy.WeaponSkillMultiplier() * 1.35f * sparkShardSynergy.OutgoingDamageMultiplier(sparkEcho);
            sparkShardSynergy.CastFinalSpark();
            if (Math.Abs((beforeSpark - sparkEcho.Hp) - expectedSparkDamage) > 0.02f || sparkEcho.SparkMarkTime != 0 || !sparkShardSynergy.floatingTexts.Any(text => text.Text == "ESTILHAÇO + CENTELHA")
                || !sparkShardSynergy.notice.Contains("ESTILHAÇO AMPLIFICADO")) throw new InvalidOperationException("Última Centelha não consumiu a preparação com +35% de dano e aviso visual.");
        }
        using (var unlinkedSpark = new Game())
        {
            unlinkedSpark.timer.Stop(); unlinkedSpark.StartGame(); unlinkedSpark.storyOverlay = false; unlinkedSpark.room = 0; unlinkedSpark.SetupRoom(); unlinkedSpark.enemies.Clear(); unlinkedSpark.finalEnding = 2;
            unlinkedSpark.equippedSkills = new[] { 1, 0, 0, 0 }; unlinkedSpark.player = new PointF(250, 320); unlinkedSpark.mouseAim = new PointF(1, 0); unlinkedSpark.damage = 100;
            var unlinkedSparkEcho = new Enemy { Pos = new PointF(340, 320), Kind = EnemyKind.Grunt, Hp = 10000, MaxHp = 10000 }; unlinkedSpark.enemies.Add(unlinkedSparkEcho); unlinkedSpark.lockedTarget = unlinkedSparkEcho;
            unlinkedSpark.CastVoiceShard(); unlinkedSpark.AdvanceVoiceShards(0.2f);
            if (unlinkedSparkEcho.SparkMarkTime != 0 || unlinkedSpark.floatingTexts.Any(text => text.Text == "CENTELHA PREPARADA")) throw new InvalidOperationException("Estilhaço preparou Centelha sem as duas técnicas equipadas.");
            float beforeUnlinkedSpark = unlinkedSparkEcho.Hp;
            float expectedUnlinkedDamage = 100 * 2.45f * unlinkedSpark.SkillDamageVariant(8) * unlinkedSpark.TechniqueDamageMultiplier() * unlinkedSpark.WeaponSkillMultiplier() * unlinkedSpark.OutgoingDamageMultiplier(unlinkedSparkEcho);
            unlinkedSpark.CastFinalSpark();
            if (Math.Abs((beforeUnlinkedSpark - unlinkedSparkEcho.Hp) - expectedUnlinkedDamage) > 0.02f || unlinkedSpark.floatingTexts.Any(text => text.Text == "ESTILHAÇO + CENTELHA"))
                throw new InvalidOperationException("Última Centelha recebeu o bônus sem a combinação equipada.");
        }
        using (var lockedBellGame = new Game())
        {
            lockedBellGame.timer.Stop(); lockedBellGame.StartGame(); lockedBellGame.storyOverlay = false; lockedBellGame.room = 6; lockedBellGame.enemies.Add(new Enemy { Pos = new PointF(260, 330), Kind = EnemyKind.Grunt, Hp = 100, MaxHp = 100 }); lockedBellGame.CastBellPulse();
            if (lockedBellGame.bellCd != 0 || lockedBellGame.memoryOverlay || !lockedBellGame.notice.Contains("SINO DE NARA")) throw new InvalidOperationException("A habilidade do Sino ficou disponível antes da descoberta narrativa.");
        }
        using (var wardGame = new Game())
        {
            wardGame.timer.Stop(); wardGame.StartGame(); wardGame.storyOverlay = false; wardGame.room = 6; wardGame.enemies.Clear();
            wardGame.enemies.Add(new Enemy { Pos = new PointF(300, 320), Kind = EnemyKind.Grunt, Hp = 100, MaxHp = 100 });
            wardGame.CastAnchorWard();
            if (wardGame.wardTime != 0 || !wardGame.notice.Contains("AINDA NÃO")) throw new InvalidOperationException("O Manto de Lastro ficou disponível antes do registro de Aven.");
            wardGame.anchorWard = true; wardGame.health = 100; wardGame.CastAnchorWard(); wardGame.HitPlayer(40);
            if (wardGame.wardTime != 2.5f || Math.Abs(wardGame.health - 82) > 0.01f || wardGame.wardCd != 20f) throw new InvalidOperationException("O Manto de Lastro não aplicou defesa ou recarga corretas.");
            wardGame.wardTime = 0; wardGame.health = 100; wardGame.HitPlayer(40);
            if (wardGame.health != 60) throw new InvalidOperationException("A proteção do Manto de Lastro continuou ativa após expirar.");
            wardGame.wardCd = 0; wardGame.skillTalent = 5; wardGame.CastAnchorWard();
            if (wardGame.wardCd != 16f) throw new InvalidOperationException("Domínio não reduziu a recarga do Manto de Lastro.");
            wardGame.skillTalent = 0; wardGame.combatMastery = 0; wardGame.weaponRarities[wardGame.weapon] = 2; wardGame.weaponAffixes[wardGame.weapon] = 4; wardGame.weaponAffixes2[wardGame.weapon] = 5; wardGame.wardCd = 0; wardGame.health = 100; wardGame.CastAnchorWard(); wardGame.HitPlayer(40);
            if (wardGame.wardTime != 3.25f || Math.Abs(wardGame.health - 84.16f) > 0.01f || Math.Abs(wardGame.wardCd - 16.8f) > 0.01f || !wardGame.WeaponAffixDescription(wardGame.weapon).Contains("Ressonância do Lastro: Manto de Lastro dura +0,75 s")) throw new InvalidOperationException("A combinação Passo Célere + Lastro não prolongou o Manto nem preservou seus bônus de Relíquia.");
            wardGame.questStage = 36; wardGame.counterTrace = true; wardGame.anchorWard = true;
            SaveData savedWard = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(wardGame.CaptureSaveData())) ?? throw new InvalidOperationException("O Manto de Lastro não foi serializado.");
            if (!savedWard.AnchorWard) throw new InvalidOperationException("O desbloqueio do Manto de Lastro não persistiu no save.");
        }
        using (var wardReturnSynergy = new Game())
        {
            wardReturnSynergy.timer.Stop(); wardReturnSynergy.StartGame(); wardReturnSynergy.storyOverlay = false; wardReturnSynergy.room = 6; wardReturnSynergy.anchorWard = true; wardReturnSynergy.finalEnding = 1; wardReturnSynergy.equippedSkills = new[] { 4, 7, 0, 0 }; wardReturnSynergy.maxHealth = 100; wardReturnSynergy.health = 40; wardReturnSynergy.enemies.Clear();
            wardReturnSynergy.enemies.Add(new Enemy { Pos = new PointF(300, 320), Kind = EnemyKind.Grunt, Hp = 1000, MaxHp = 1000 });
            wardReturnSynergy.CastAnchorWard();
            if (wardReturnSynergy.wardTime <= 0 || !wardReturnSynergy.notice.Contains("+35%")) throw new InvalidOperationException("Manto + Devolução não anunciaram a cura ampliada ao ativar a proteção.");
            wardReturnSynergy.CastReturnedVoices();
            if (Math.Abs(wardReturnSynergy.health - 46.075f) > 0.01f || !wardReturnSynergy.notice.Contains("MANTO AMPLIOU A CURA")) throw new InvalidOperationException("Devolução não ampliou a cura em 35% enquanto o Manto estava ativo.");
            wardReturnSynergy.wardTime = 0; wardReturnSynergy.health = 40; wardReturnSynergy.returnVoiceCd = 0; wardReturnSynergy.equippedSkills = new[] { 0, 7, 0, 0 };
            wardReturnSynergy.CastReturnedVoices();
            if (Math.Abs(wardReturnSynergy.health - 44.5f) > 0.01f) throw new InvalidOperationException("A cura ampliada do Manto persistiu após a proteção expirar ou sem o par equipado.");
        }
        using (var lootRollCheck = new Game())
        {
            lootRollCheck.timer.Stop(); lootRollCheck.StartGame(); lootRollCheck.storyOverlay = false;
            var common = RollWeaponStats(0, 4, 2); var rare = RollWeaponStats(62, 4, 2); var relic = RollWeaponStats(95, 4, 2); var swiftRelic = RollWeaponStats(95, 4, 3); var anchoredRelic = RollWeaponStats(95, 4, 4); var reachRare = RollWeaponStats(62, 4, 5); var reachRelic = RollWeaponStats(95, 4, 5, 4); var ruptureRelic = RollWeaponStats(95, 4, 7, 5);
            if (common.Rarity != 0 || common.Bonus != 1 || common.Affix != 0 || common.Affix2 != 0 || rare.Rarity != 1 || rare.Bonus != 3 || rare.Affix != 3 || rare.Affix2 != 0 || relic.Rarity != 2 || relic.Bonus != 11 || relic.Affix != 3 || relic.Affix2 is < 1 or > 8 || relic.Affix2 == relic.Affix || swiftRelic.Affix != 4 || anchoredRelic.Affix != 5 || reachRare.Affix != 6 || reachRelic.Affix != 6 || reachRelic.Affix2 != 5 || ruptureRelic.Affix != 8 || ruptureRelic.Affix2 != 6)
                throw new InvalidOperationException("As faixas de rolagem de raridade e afixo da arma mudaram.");
            lootRollCheck.weaponRarities[0] = 1; lootRollCheck.weaponBonuses[0] = 3; lootRollCheck.weaponAffixes[0] = 6;
            if (Math.Abs(lootRollCheck.BasicAttackReach(new Enemy { Kind = EnemyKind.Grunt }) - 87.36f) > 0.01f || !ValidWeaponData(lootRollCheck.CaptureSaveData()) || WeaponGroundAffix(6, 1) != "Fio Esticado • alcance +12%")
                throw new InvalidOperationException("O Fio Esticado não aumentou o alcance básico, não apareceu no loot ou foi recusado pelo save.");
            lootRollCheck.weaponRarities[0] = 2; lootRollCheck.weaponBonuses[0] = 7;
            if (Math.Abs(lootRollCheck.BasicAttackReach(new Enemy { Kind = EnemyKind.Grunt }) - 93.6f) > 0.01f || WeaponAffixDescription(6, 2) != "do Fio Esticado: alcance básico +20%")
                throw new InvalidOperationException("O Fio Esticado não escalou para o bônus de Relíquia.");
            lootRollCheck.weaponRarities[0] = lootRollCheck.weaponBonuses[0] = lootRollCheck.weaponAffixes[0] = lootRollCheck.weaponAffixes2[0] = 0;
            Drop drop = lootRollCheck.CreateWeaponDrop(lootRollCheck.player, 1); int rarity = drop.Rarity, bonus = drop.Bonus, affix = drop.Affix, affix2 = drop.Affix2;
            string label = lootRollCheck.DropLabel(drop); int expectedDropDamage = lootRollCheck.damage - lootRollCheck.WeaponPower(lootRollCheck.weapon) + EquipmentCatalog.Weapon(1).BaseDamage + bonus; lootRollCheck.Collect(drop);
            if (lootRollCheck.weaponRarities[1] != rarity || lootRollCheck.weaponBonuses[1] != bonus || lootRollCheck.weaponAffixes[1] != affix || lootRollCheck.weaponAffixes2[1] != affix2 || !label.Contains(WeaponRarityName(rarity)) || !label.Contains(WeaponGroundAffix(drop)) || !label.Contains($"DANO: 25 → {expectedDropDamage}") || !label.Contains("longo") || !label.Contains("ATUAL: sem afixo"))
                throw new InvalidOperationException("A arma mudou ao ser recolhida ou não foi identificada no chão.");
        }
        if (EliteAffixForRoll(0) != EliteAffix.Armored || EliteAffixForRoll(1) != EliteAffix.Swift || EliteAffixForRoll(2) != EliteAffix.Bloodbound || EliteAffixForRoll(4) != EliteAffix.Summoner || EliteAffixForRoll(5) != EliteAffix.Conductor || EliteAffixName(EliteAffix.Bloodbound) != "SANGRADOR" || EliteAffixName(EliteAffix.Conductor) != "CONDUTOR" || EliteSecondaryAffixForRoll(0, EliteAffix.Armored) != EliteAffix.Swift || EliteSecondaryAffixForRoll(1, EliteAffix.Armored) != EliteAffix.Bloodbound || EliteSecondaryAffixForRoll(0, EliteAffix.Summoner) == EliteAffix.Summoner || EliteSecondaryAffixForRoll(0, EliteAffix.Spearbound) == EliteAffix.Swift)
            throw new InvalidOperationException("As variantes dos elites não foram identificadas corretamente.");
        using (var eliteAffixCheck = new Game())
        {
            eliteAffixCheck.timer.Stop(); eliteAffixCheck.StartGame(); eliteAffixCheck.storyOverlay = false; eliteAffixCheck.player = new PointF(200, 330);
            Enemy swiftElite = new() { Pos = new PointF(250, 330), Kind = EnemyKind.Elite, EliteAffix = EliteAffix.Swift, Hp = 120, MaxHp = 120 };
            eliteAffixCheck.enemies.Add(swiftElite); eliteAffixCheck.UpdateEnemy(swiftElite, 0.01f);
            if (swiftElite.WindupMax != 0.38f || Math.Abs(swiftElite.Cooldown - 1.55f) > 0.001f || swiftElite.Armored) throw new InvalidOperationException("O elite Veloz não anunciou seu golpe curto com recarga própria.");
            eliteAffixCheck.enemies.Clear();
            Enemy swiftArmoredElite = new() { Pos = new PointF(250, 330), Kind = EnemyKind.Elite, EliteAffix = EliteAffix.Swift, EliteAffix2 = EliteAffix.Armored, Armored = true, Hp = 120, MaxHp = 120 };
            eliteAffixCheck.enemies.Add(swiftArmoredElite); eliteAffixCheck.UpdateEnemy(swiftArmoredElite, 0.01f);
            if (swiftArmoredElite.WindupMax != 0.48f || Math.Abs(swiftArmoredElite.Cooldown - 2f) > 0.001f || !eliteAffixCheck.IsArmored(swiftArmoredElite)) throw new InvalidOperationException("A combinação Veloz + Blindado perdeu um dos ritmos ou a defesa.");
            eliteAffixCheck.enemies.Clear(); eliteAffixCheck.health = 100;
            Enemy bloodboundElite = new() { Pos = new PointF(250, 330), Target = eliteAffixCheck.player, Kind = EnemyKind.Elite, EliteAffix = EliteAffix.Summoner, EliteAffix2 = EliteAffix.Bloodbound, Hp = 50, MaxHp = 120, Windup = 0.01f, SummonCooldown = 100 };
            eliteAffixCheck.enemies.Add(bloodboundElite); eliteAffixCheck.UpdateEnemy(bloodboundElite, 0.02f);
            if (eliteAffixCheck.health != 76 || bloodboundElite.Hp != 58) throw new InvalidOperationException("O elite Sangrador não causou o dano anunciado nem recuperou vida ao acertar.");
            Enemy armoredElite = new() { Kind = EnemyKind.Elite, EliteAffix = EliteAffix.Summoner, EliteAffix2 = EliteAffix.Armored, Armored = true, Hp = 120, MaxHp = 120 };
            if (!HasEliteAffix(armoredElite, EliteAffix.Armored) || EnemyName(armoredElite) != "ELITE • INVOCADOR + BLINDADO") throw new InvalidOperationException("O elite com afixo secundário não preservou sua redução nem anunciou os dois modificadores.");
            armoredElite.Pos = new PointF(250, 330);
            Enemy wardedGrunt = new() { Pos = new PointF(350, 330), Kind = EnemyKind.Grunt, Hp = 100, MaxHp = 100 };
            Enemy unwardedGrunt = new() { Pos = new PointF(400, 330), Kind = EnemyKind.Grunt, Hp = 100, MaxHp = 100 };
            eliteAffixCheck.enemies.Clear(); eliteAffixCheck.enemies.AddRange(new[] { armoredElite, wardedGrunt, unwardedGrunt });
            if (!eliteAffixCheck.IsArmored(wardedGrunt) || eliteAffixCheck.IsArmored(unwardedGrunt) || eliteAffixCheck.BasicAttackDamageMultiplier(wardedGrunt) != 0.75f)
                throw new InvalidOperationException("A aura do elite Blindado não protegeu apenas os ecos dentro do alcance.");
            eliteAffixCheck.enemies.Remove(armoredElite);
            if (eliteAffixCheck.IsArmored(wardedGrunt)) throw new InvalidOperationException("A proteção da aura permaneceu depois da queda do elite Blindado.");
            Enemy conductor = new() { Pos = new PointF(500, 330), Kind = EnemyKind.Elite, EliteAffix = EliteAffix.Conductor, Hp = 90, MaxHp = 120, SupportCooldown = 0 };
            Enemy supportedGrunt = new() { Pos = new PointF(620, 330), Kind = EnemyKind.Grunt, Hp = 50, MaxHp = 100 };
            Enemy distantGrunt = new() { Pos = new PointF(680, 330), Kind = EnemyKind.Grunt, Hp = 50, MaxHp = 100 };
            eliteAffixCheck.enemies.Clear(); eliteAffixCheck.enemies.AddRange(new[] { conductor, supportedGrunt, distantGrunt });
            if (!ConductorPulseCovers(conductor, supportedGrunt) || ConductorPulseCovers(conductor, distantGrunt)) throw new InvalidOperationException("O alcance do pulso do Condutor não distinguiu aliados próximos e distantes.");
            eliteAffixCheck.UpdateEnemy(conductor, 0.01f);
            if (conductor.SupportWindup <= 0 || Math.Abs(conductor.SupportWindup - 0.9f) > 0.001f) throw new InvalidOperationException("O Condutor não anunciou o pulso de cura antes de aplicá-lo.");
            conductor.SupportWindup = 0.01f; eliteAffixCheck.UpdateEnemy(conductor, 0.02f);
            if (supportedGrunt.Hp != 62 || distantGrunt.Hp != 50 || conductor.Hp != 105) throw new InvalidOperationException("O pulso do Condutor não curou somente os aliados dentro do alcance.");
            conductor.SupportWindup = 0.2f; conductor.Stagger = 0.1f; supportedGrunt.Hp = 40; eliteAffixCheck.UpdateEnemy(conductor, 0.01f);
            if (conductor.SupportWindup != 0 || supportedGrunt.Hp != 40 || conductor.SupportCooldown != 1.8f) throw new InvalidOperationException("O Condutor não teve o canal cancelado por interrupção.");
        }
        if (EliteWeaponDropRoll(-1) || !EliteWeaponDropRoll(0) || !EliteWeaponDropRoll(34) || EliteWeaponDropRoll(35) || EliteWeaponDropRoll(99)
            || !EliteWeaponDropRoll(99, 2) || !EliteWeaponDropRoll(0, 1) || EliteWeaponDropRoll(99, 1)
            || NextEliteWeaponMisses(0, false) != 1 || NextEliteWeaponMisses(1, false) != 2
            || NextEliteWeaponMisses(2, false) != 2 || NextEliteWeaponMisses(2, true) != 0)
            throw new InvalidOperationException("A chance de arma dos elites ou a proteção após duas falhas não respeitou os limites definidos.");
        using (var elitePitySaveCheck = new Game())
        {
            elitePitySaveCheck.timer.Stop(); elitePitySaveCheck.StartGame(); elitePitySaveCheck.eliteWeaponMisses = 2;
            SaveData pitySave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(elitePitySaveCheck.CaptureSaveData()))!;
            if (pitySave.EliteWeaponMisses != 2 || !ValidSaveData(pitySave) || new SaveData().EliteWeaponMisses != 0)
                throw new InvalidOperationException("O contador da proteção de loot não foi salvo, validado ou inicializado para saves antigos.");
            elitePitySaveCheck.room = 1;
            var guaranteedElite = new Enemy { Pos = new PointF(500, 330), Kind = EnemyKind.Elite, Hp = 1, MaxHp = 1 };
            elitePitySaveCheck.enemies.Add(guaranteedElite);
            elitePitySaveCheck.Defeat(guaranteedElite);
            if (elitePitySaveCheck.eliteWeaponMisses != 0 || !elitePitySaveCheck.drops.Any(drop => drop.Kind == "weapon"))
                throw new InvalidOperationException("A terceira elite não garantiu a arma nem reiniciou a proteção.");
        }
        foreach (var tier in new[] { (Room: 2, Floor: 62), (Room: 12, Floor: 75), (Room: 25, Floor: 85) })
        {
            int floor = BossWeaponRollFloor(tier.Room);
            int commonRolls = Enumerable.Range(floor, 100 - floor).Count(roll => RollWeaponStats(roll, 0, 0).Rarity == 0);
            int relicRolls = Enumerable.Range(floor, 100 - floor).Count(roll => RollWeaponStats(roll, 0, 0).Rarity == 2);
            if (floor != tier.Floor || commonRolls != 0 || relicRolls != 5)
                throw new InvalidOperationException("A raridade das armas de chefe não escalou com o avanço da campanha.");
        }
        using (var bossLootCheck = new Game())
        {
            bossLootCheck.timer.Stop(); bossLootCheck.StartGame(); bossLootCheck.storyOverlay = false; bossLootCheck.room = 40;
            var boss = new Enemy { Pos = new PointF(500, 330), Kind = EnemyKind.Guardian, Hp = 1, MaxHp = 1 };
            bossLootCheck.enemies.Add(boss); bossLootCheck.Defeat(boss);
            Drop bossWeapon = bossLootCheck.drops.Single(d => d.Kind == "weapon");
            if (bossWeapon.Amount is < 1 or > 2 || bossWeapon.Rarity is < 1 or > 2 || bossWeapon.Affix == 0 || bossWeapon.Rarity == 2 && (bossWeapon.Affix2 == 0 || bossWeapon.Affix2 == bossWeapon.Affix))
                throw new InvalidOperationException("A recompensa do chefe não gerou uma arma Rara ou Relíquia válida.");
            bossLootCheck.Collect(bossWeapon);
            SaveData bossLootSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(bossLootCheck.CaptureSaveData()))!;
            int bossWeaponIndex = bossWeapon.Amount;
            if ((bossLootCheck.ownedWeapons & (1 << bossWeaponIndex)) == 0 || bossLootSave.WeaponRarities[bossWeaponIndex] != bossWeapon.Rarity || bossLootSave.WeaponAffixes[bossWeaponIndex] != bossWeapon.Affix || !ValidWeaponData(bossLootSave))
                throw new InvalidOperationException("A arma do chefe não foi coletada e persistida corretamente.");
        }
        using (var salvageCheck = new Game())
        {
            salvageCheck.timer.Stop(); salvageCheck.StartGame(); salvageCheck.storyOverlay = false; salvageCheck.player = new PointF(500, 330); salvageCheck.tideGlass = 998;
            int originalDamage = salvageCheck.damage, originalWeapon = salvageCheck.weapon, originalOwnedWeapons = salvageCheck.ownedWeapons;
            var rareDrop = new Drop { Pos = new PointF(540, 330), Kind = "weapon", Amount = 1, Rarity = 1, Bonus = 3, Affix = 1 };
            var relicDrop = new Drop { Pos = new PointF(560, 330), Kind = "weapon", Amount = 2, Rarity = 2, Bonus = 7, Affix = 2, Affix2 = 4 };
            salvageCheck.drops.Add(rareDrop); salvageCheck.drops.Add(relicDrop);
            salvageCheck.OnKeyDown(salvageCheck, new KeyEventArgs(Keys.X));
            if (salvageCheck.gold != 40 || salvageCheck.tideGlass != 999 || salvageCheck.drops.Count != 1 || !ReferenceEquals(salvageCheck.drops[0], relicDrop) || salvageCheck.weapon != originalWeapon || salvageCheck.damage != originalDamage || salvageCheck.ownedWeapons != originalOwnedWeapons)
                throw new InvalidOperationException("Desmontar a arma Rara não converteu o item mais próximo sem alterar o equipamento.");
            salvageCheck.OnKeyDown(salvageCheck, new KeyEventArgs(Keys.X));
            SaveData salvageSave = salvageCheck.CaptureSaveData();
            if (salvageCheck.gold != 115 || salvageCheck.tideGlass != 999 || salvageCheck.drops.Count != 0 || salvageSave.Gold != 115 || salvageSave.TideGlass != 999)
                throw new InvalidOperationException("Desmontar a Relíquia não concedeu os materiais ou não os preservou no save.");
        }
        using (var activeWeaponDrop = new Game())
        {
            activeWeaponDrop.timer.Stop(); activeWeaponDrop.StartGame(); activeWeaponDrop.weapon = 1; activeWeaponDrop.ownedWeapons |= 2; activeWeaponDrop.weaponRarities[1] = 1; activeWeaponDrop.weaponBonuses[1] = 3; activeWeaponDrop.weaponAffixes[1] = 1; activeWeaponDrop.damage += activeWeaponDrop.WeaponPower(1); activeWeaponDrop.Collect(new Drop { Kind = "weapon", Amount = 1, Rarity = 2, Bonus = 9, Affix = 2, Affix2 = 5 });
            if (activeWeaponDrop.damage != 44 || activeWeaponDrop.weaponBonuses[1] != 9 || activeWeaponDrop.WeaponAffixDescription(1) != "do Contracanto: +11 p.p. de crítico • do Lastro: dano recebido -12%") throw new InvalidOperationException("Atualizar a arma equipada pelo saque não ajustou o dano nem comparou ambos os afixos.");
        }
        using (var buildGame = new Game())
        {
            buildGame.timer.Stop(); buildGame.StartGame(); buildGame.storyOverlay = false; buildGame.room = 6; buildGame.damage = 100; buildGame.enemies.Clear();
            float baseDamage = buildGame.VoiceShardDamage(); buildGame.pendingTalents = 1; buildGame.talentChoice = true; buildGame.SelectTalent(Keys.D4);
            float specializedDamage = buildGame.VoiceShardDamage();
            if (buildGame.techniqueTalent != 1 || buildGame.pendingTalents != 0 || buildGame.talentChoice || Math.Abs(specializedDamage / baseDamage - 1.08f) > 0.001f)
                throw new InvalidOperationException("A especialização de Sintonia não foi escolhida ou não fortaleceu as técnicas em 8%.");
            buildGame.player = new PointF(250, 330); buildGame.enemies.Add(new Enemy { Pos = new PointF(300, 330), Kind = EnemyKind.Grunt, Hp = 1000, MaxHp = 1000 }); buildGame.Whirlwind();
            if (buildGame.enemies[0].Hp >= 1000 - 100 * 1.6f * 1.08f + 1) throw new InvalidOperationException("A Sintonia não alterou o dano real do Redemoinho.");
            SaveData buildSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(buildGame.CaptureSaveData())) ?? throw new InvalidOperationException("A especialização não foi serializada.");
            if (buildSave.TechniqueTalent != 1 || JsonSerializer.Deserialize<SaveData>("{}")?.TechniqueTalent != 0) throw new InvalidOperationException("A especialização não persistiu ou invalidou saves antigos.");
        }
        using (var disciplineGame = new Game())
        {
            disciplineGame.timer.Stop(); disciplineGame.StartGame(); disciplineGame.level = 5; disciplineGame.pendingTalents = 1; disciplineGame.talentChoice = true;
            disciplineGame.SelectTalent(Keys.D4);
            if (disciplineGame.pendingTalents != 1 || disciplineGame.combatDiscipline != 0) throw new InvalidOperationException("O talento comum ignorou a escolha obrigatória de especialização do nível 5.");
            disciplineGame.SelectTalent(Keys.D5);
            if (disciplineGame.combatDiscipline != 1 || disciplineGame.pendingTalents != 0 || Math.Abs(disciplineGame.BasicAttackSpecializationMultiplier() - 1.12f) > 0.001f || Math.Abs(disciplineGame.TechniqueDamageMultiplier() - 0.9f) > 0.001f) throw new InvalidOperationException("A Forma do Gume não aplicou sua especialização e seu custo tático.");
            SaveData disciplineSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(disciplineGame.CaptureSaveData()))!;
            if (disciplineSave.CombatDiscipline != 1 || !ValidSaveData(disciplineSave) || JsonSerializer.Deserialize<SaveData>("{}")?.CombatDiscipline is not null || CombatDisciplineForSave(new SaveData { Level = 4 }) != 0 || CombatDisciplineForSave(new SaveData { Level = 5 }) != 1) throw new InvalidOperationException("A Forma do Gume não persistiu ou saves antigos não preservaram a migração de especialização.");
        }
        using (var echoDisciplineGame = new Game())
        {
            echoDisciplineGame.timer.Stop(); echoDisciplineGame.StartGame(); echoDisciplineGame.level = 5; echoDisciplineGame.pendingTalents = 1; echoDisciplineGame.talentChoice = true; echoDisciplineGame.SelectTalent(Keys.D6);
            if (echoDisciplineGame.combatDiscipline != 2 || Math.Abs(echoDisciplineGame.TechniqueDamageMultiplier() - 1.12f) > 0.001f || Math.Abs(echoDisciplineGame.BasicAttackSpecializationMultiplier() - 0.9f) > 0.001f) throw new InvalidOperationException("A Forma do Eco não aplicou sua especialização e seu custo tático.");
        }
        using (var masteryGame = new Game())
        {
            masteryGame.timer.Stop(); masteryGame.StartGame(); masteryGame.level = 10; masteryGame.combatDiscipline = 1; masteryGame.pendingTalents = 1; masteryGame.talentChoice = true;
            masteryGame.SelectTalent(Keys.D5);
            if (masteryGame.combatMastery != 1 || masteryGame.pendingTalents != 0 || Math.Abs(masteryGame.CombatMasteryDamageTakenMultiplier() - 0.9f) > 0.001f || Math.Abs(masteryGame.DashCooldown() - 1.35f) > 0.001f) throw new InvalidOperationException("A Guarda Serena não trocou uma esquiva mais lenta por redução de dano.");
            SaveData masterySave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(masteryGame.CaptureSaveData()))!;
            if (masterySave.CombatMastery != 1 || !ValidSaveData(masterySave) || CombatMasteryForSave(new SaveData { Level = 9 }) != 0 || CombatMasteryForSave(new SaveData { Level = 10 }) != 1) throw new InvalidOperationException("A maestria defensiva não persistiu ou saves antigos não receberam um padrão válido.");
            masteryGame.combatMastery = 0; masteryGame.pendingTalents = 1; masteryGame.talentChoice = true; masteryGame.SelectTalent(Keys.D6);
            if (masteryGame.combatMastery != 2 || Math.Abs(masteryGame.CombatMasteryDamageTakenMultiplier() - 1.08f) > 0.001f || Math.Abs(masteryGame.DashCooldown() - 0.97f) > 0.001f) throw new InvalidOperationException("O Passo Impetuoso não trocou proteção por uma esquiva mais frequente.");
        }
        using (var bossFocusGame = new Game())
        {
            bossFocusGame.timer.Stop(); bossFocusGame.StartGame(); bossFocusGame.level = 15; bossFocusGame.combatDiscipline = 1; bossFocusGame.combatMastery = 1; bossFocusGame.pendingTalents = 1; bossFocusGame.talentChoice = true;
            bossFocusGame.SelectTalent(Keys.D4);
            if (bossFocusGame.pendingTalents != 1 || bossFocusGame.combatFocus != 0) throw new InvalidOperationException("O nível 15 permitiu gastar o ponto antes da escolha de foco.");
            bossFocusGame.SelectTalent(Keys.D5); var bossFocusTarget = new Enemy { Kind = EnemyKind.Guardian }; var bossFocusCommon = new Enemy { Kind = EnemyKind.Grunt };
            if (bossFocusGame.combatFocus != 1 || bossFocusGame.pendingTalents != 0 || Math.Abs(bossFocusGame.OutgoingDamageMultiplier(bossFocusTarget) - 1.15f) > 0.001f || bossFocusGame.OutgoingDamageMultiplier(bossFocusCommon) != 1f) throw new InvalidOperationException("Caçador de Ecos não concentrou seu bônus nos chefes.");
            SaveData focusSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(bossFocusGame.CaptureSaveData()))!;
            if (focusSave.CombatFocus != 1 || !ValidSaveData(focusSave) || JsonSerializer.Deserialize<SaveData>("{}")?.CombatFocus != 0) throw new InvalidOperationException("O foco de nível 15 não persistiu ou invalidou saves antigos.");
            bossFocusGame.gold = 5000; bossFocusGame.tideGlass = 20; bossFocusGame.RespecTalents();
            if (bossFocusGame.pendingTalents != 3 || bossFocusGame.combatFocus != 0) throw new InvalidOperationException("Redistribuir talentos não devolveu o ponto do foco de nível 15.");
            bossFocusGame.SelectTalent(Keys.D5); bossFocusGame.SelectTalent(Keys.D6); bossFocusGame.SelectTalent(Keys.D5);
            if (bossFocusGame.pendingTalents != 0 || bossFocusGame.combatDiscipline != 1 || bossFocusGame.combatMastery != 2 || bossFocusGame.combatFocus != 1) throw new InvalidOperationException("A redistribuição não permitiu escolher novamente os três marcos de combate.");
        }
        using (var crowdFocusGame = new Game())
        {
            crowdFocusGame.timer.Stop(); crowdFocusGame.StartGame(); crowdFocusGame.enemies.Clear(); crowdFocusGame.level = 15; crowdFocusGame.combatDiscipline = 2; crowdFocusGame.combatMastery = 2; crowdFocusGame.pendingTalents = 1; crowdFocusGame.talentChoice = true; crowdFocusGame.SelectTalent(Keys.D6);
            var crowdTarget = new Enemy { Kind = EnemyKind.Grunt }; crowdFocusGame.enemies.Add(crowdTarget); crowdFocusGame.enemies.Add(new Enemy { Kind = EnemyKind.Archer }); crowdFocusGame.enemies.Add(new Enemy { Kind = EnemyKind.Mage });
            if (crowdFocusGame.combatFocus != 2 || Math.Abs(crowdFocusGame.OutgoingDamageMultiplier(crowdTarget) - 1.1f) > 0.001f || Math.Abs(crowdFocusGame.OutgoingDamageMultiplier(new Enemy { Kind = EnemyKind.Guardian }) - 1f) > 0.001f) throw new InvalidOperationException("Quebra-Formação não fortaleceu ataques contra grupos nem preservou o dano contra chefes.");
            crowdFocusGame.enemies.RemoveAt(2); crowdFocusGame.enemies.RemoveAt(1);
            if (crowdFocusGame.OutgoingDamageMultiplier(crowdTarget) != 1f) throw new InvalidOperationException("Quebra-Formação manteve seu bônus com menos de três inimigos ativos.");
        }
        using (var apexGame = new Game())
        {
            apexGame.timer.Stop(); apexGame.StartGame(); apexGame.level = 20; apexGame.combatDiscipline = 2; apexGame.combatMastery = 1; apexGame.combatFocus = 1; apexGame.pendingTalents = 1; apexGame.talentChoice = true;
            apexGame.equippedSkills = new[] { 1, 2, 3, 0 }; apexGame.riverBell = apexGame.ebbSeal = true; apexGame.shardCd = 2; apexGame.bellCd = 3;
            apexGame.SelectTalent(Keys.D4);
            if (apexGame.pendingTalents != 1 || apexGame.combatApex != 0) throw new InvalidOperationException("O nível 20 permitiu gastar o ponto antes da escolha de auge.");
            apexGame.SelectTalent(Keys.D5);
            if (apexGame.combatApex != 1 || apexGame.TechniquesOnCooldown() != 2 || Math.Abs(apexGame.TechniqueDamageMultiplier() - 1.3216f) > 0.001f) throw new InvalidOperationException("Coro Suspenso não fortaleceu técnicas enquanto duas técnicas equipadas recarregam.");
            apexGame.bellCd = 0;
            if (Math.Abs(apexGame.TechniqueDamageMultiplier() - 1.12f) > 0.001f) throw new InvalidOperationException("Coro Suspenso manteve o bônus com apenas uma técnica em recarga.");
            apexGame.shardCd = 0; apexGame.bellCd = 2; apexGame.enemies.Add(new Enemy { Kind = EnemyKind.Grunt, Pos = new PointF(400, 330), Hp = 500, MaxHp = 500 });
            apexGame.CastVoiceShard();
            if (apexGame.echoShards.Count != 1 || Math.Abs(apexGame.echoShards[0].Damage - apexGame.damage * 1.35f * 1.12f * 1.18f) > 0.01f) throw new InvalidOperationException("O Estilhaço não recebeu Coro Suspenso ao iniciar sua recarga.");
            apexGame.equippedSkills = new[] { 1, 0, 0, 0 }; apexGame.riverBell = apexGame.ebbSeal = false; apexGame.bellCd = 3; SaveData apexSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(apexGame.CaptureSaveData()))!;
            if (apexSave.CombatApex != 1 || !ValidSaveData(apexSave) || JsonSerializer.Deserialize<SaveData>("{}")?.CombatApex != 0) throw new InvalidOperationException("A escolha de auge não persistiu ou não preservou compatibilidade de saves antigos.");
            apexGame.equippedSkills = new[] { 1, 2, 3, 0 }; apexGame.riverBell = apexGame.ebbSeal = true;
            apexGame.gold = 10000; apexGame.tideGlass = 99; apexGame.RespecTalents();
            if (apexGame.pendingTalents != 4 || apexGame.combatApex != 0) throw new InvalidOperationException("Redistribuir talentos não devolveu os quatro marcos investidos.");
            apexGame.SelectTalent(Keys.D5); apexGame.SelectTalent(Keys.D5); apexGame.SelectTalent(Keys.D5); apexGame.SelectTalent(Keys.D6);
            if (apexGame.pendingTalents != 0 || apexGame.combatDiscipline != 1 || apexGame.combatMastery != 1 || apexGame.combatFocus != 1 || apexGame.combatApex != 2 || Math.Abs(apexGame.CombatApexDamageTakenMultiplier() - 0.88f) > 0.001f) throw new InvalidOperationException("A redistribuição não permitiu montar a Âncora de Eco no marco de nível 20.");
            apexGame.health = apexGame.maxHealth = 100; apexGame.HitPlayer(100);
            apexGame.equippedSkills = new[] { 1, 0, 0, 0 }; apexGame.riverBell = apexGame.ebbSeal = false;
            if (Math.Abs(apexGame.health - 20.8f) > 0.01f || !ValidSaveData(apexGame.CaptureSaveData())) throw new InvalidOperationException("Âncora de Eco não combinou sua redução condicional com a Guarda Serena ou não salvou a build.");
        }
        using (var tempoGame = new Game())
        {
            tempoGame.timer.Stop(); tempoGame.StartGame(); tempoGame.level = 25; tempoGame.combatDiscipline = 1; tempoGame.combatMastery = 1; tempoGame.combatFocus = 1; tempoGame.combatApex = 1;
            tempoGame.equippedSkills = new[] { 1, 2, 3, 0 }; tempoGame.pendingTalents = 1; tempoGame.talentChoice = true;
            float readyTechniqueDamage = tempoGame.TechniqueDamageMultiplier(); tempoGame.SelectTalent(Keys.D5);
            if (tempoGame.combatTempo != 1 || tempoGame.pendingTalents != 0 || Math.Abs(tempoGame.TechniqueDamageMultiplier() - readyTechniqueDamage * 1.15f) > 0.001f)
                throw new InvalidOperationException("Abertura da Vigília não fortaleceu técnicas quando todas estavam prontas.");
            tempoGame.bellCd = 1;
            if (Math.Abs(tempoGame.TechniqueDamageMultiplier() - readyTechniqueDamage) > 0.001f) throw new InvalidOperationException("Abertura da Vigília manteve o bônus durante uma recarga.");
            tempoGame.equippedSkills = new[] { 1, 0, 0, 0 };
            SaveData tempoSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(tempoGame.CaptureSaveData()))!;
            if (tempoSave.CombatTempo != 1) throw new InvalidOperationException("A escolha de ritmo não persistiu no save.");
            if (!ValidSaveData(tempoSave)) throw new InvalidOperationException("O save da escolha de ritmo foi rejeitado pela validação.");
            if (JsonSerializer.Deserialize<SaveData>("{}")?.CombatTempo != 0) throw new InvalidOperationException("Save antigo não recebeu o valor padrão de ritmo.");

            tempoGame.gold = 10000; tempoGame.tideGlass = 99; tempoGame.RespecTalents();
            if (tempoGame.pendingTalents != 5 || tempoGame.combatTempo != 0) throw new InvalidOperationException("Redistribuir no nível 25 não devolveu os cinco marcos da build.");
            tempoGame.equippedSkills = new[] { 1, 2, 3, 0 }; tempoGame.shardCd = 1; tempoGame.bellCd = 1; tempoGame.pullCd = 0;
            tempoGame.SelectTalent(Keys.D5); tempoGame.SelectTalent(Keys.D6); tempoGame.SelectTalent(Keys.D5); tempoGame.SelectTalent(Keys.D5); tempoGame.SelectTalent(Keys.D6);
            if (tempoGame.pendingTalents != 0 || tempoGame.combatTempo != 2 || tempoGame.TechniquesOnCooldown() != 2
                || Math.Abs(tempoGame.BasicAttackSpecializationMultiplier() - 1.2544f) > 0.001f)
                throw new InvalidOperationException("Resposta do Veio não fortaleceu ataques básicos com duas técnicas recarregando após redistribuir os cinco marcos.");
            tempoGame.bellCd = 0;
            if (Math.Abs(tempoGame.BasicAttackSpecializationMultiplier() - 1.12f) > 0.001f) throw new InvalidOperationException("Resposta do Veio manteve o bônus com uma técnica em recarga.");
        }
        using var game = new Game(); game.timer.Stop(); game.StartGame();
        if (!game.storyOverlay || game.storyPage != 0) throw new InvalidOperationException("O novo jogo não iniciou no prólogo da história.");
        for (int i = 0; i < game.prologue.Length; i++) game.AdvanceStory();
        if (game.storyOverlay) throw new InvalidOperationException("O prólogo não liberou o início da partida.");
        game.Collect(new Drop { Pos = game.player, Kind = "relic" });
        if (!game.relic || !game.memoryOverlay) throw new InvalidOperationException("O fragmento do relicário não revelou a lembrança de Nara.");
        game.memoryOverlay = false;
        game.player = MemoryStonePositions[0]; game.Interact();
        if (game.memoriesFound != 0) throw new InvalidOperationException("Uma pista foi liberada sem limpar os inimigos.");
        game.enemies.Clear(); game.Interact();
        if (game.memoriesFound != 1 || !game.memoryOverlay || game.memoryTitle != "INSCRIÇÃO NO VESTÍBULO") throw new InvalidOperationException("A pista do vestíbulo não foi registrada ao interagir.");
        game.memoryOverlay = false; game.room = 1; game.player = MemoryStonePositions[1]; game.Interact();
        if (game.memoriesFound != 3 || !game.memoryOverlay) throw new InvalidOperationException("A pista do Salão dos Ossos não foi registrada.");
        game.memoryOverlay = false; game.clearedRooms = 0; game.room = 0; game.SetupRoom(); game.storyOverlay = false;
        if (game.enemies.Count != 3) throw new InvalidOperationException("A primeira sala deve iniciar com três inimigos.");
        game.player = new PointF(320, 230); game.MovePlayer(new PointF(40, 40)); PointF movedPlayer = game.player;
        if (Math.Abs(movedPlayer.X - 320) > 0.01f || Math.Abs(movedPlayer.Y - 270) > 0.01f) throw new InvalidOperationException("O movimento não deslizou pela quina do obstáculo.");
        Enemy grunt = game.enemies[0]; game.player = new PointF(grunt.Pos.X - 22, grunt.Pos.Y); grunt.Cooldown = 0; game.UpdateEnemy(grunt, 0.01f);
        if (grunt.Windup <= 0 || game.health < game.maxHealth) throw new InvalidOperationException("Ataque corpo a corpo comum não foi anunciado.");
        game.player = new PointF(grunt.Pos.X - 100, grunt.Pos.Y); game.UpdateEnemy(grunt, 0.6f);
        if (game.health < game.maxHealth) throw new InvalidOperationException("A esquiva antes do golpe não evitou o dano.");
        game.AddXp(60);
        if (game.level != 2 || !game.talentChoice || game.pendingTalents != 1) throw new InvalidOperationException("A progressão por nível não concedeu uma escolha de talento.");
        game.talentChoice = false; int goldBeforeTown = game.gold, potionsBeforeTown = game.potions; game.EnterVesper(); game.memoryOverlay = false;
        if (game.room != 3 || game.enemies.Count != 0) throw new InvalidOperationException("A praça de Véspera não abriu como área segura explorável.");
        game.player = new PointF(350, 200); game.MovePlayer(new PointF(-40, 0));
        PointF townPosition = game.player;
        if (Math.Abs(townPosition.X - 350) > 0.01f) throw new InvalidOperationException("A colisão da ferraria não bloqueou a passagem pela parede.");
        game.player = CuratorPosition; game.Interact();
        if (game.questStage != 1 || !game.memoryOverlay) throw new InvalidOperationException("Sileth não iniciou a investigação do livro.");
        game.memoryOverlay = false; game.player = ArchiveBookPosition; game.Interact();
        if (game.questStage != 2 || !game.memoryOverlay) throw new InvalidOperationException("O Livro das Ausências não avançou a missão.");
        game.memoryOverlay = false; game.player = CuratorPosition; game.Interact();
        if (game.questStage != 3 || game.gold != goldBeforeTown + 40 || game.potions != potionsBeforeTown + 1) throw new InvalidOperationException("A conclusão da investigação não concedeu progresso e recompensa.");
        game.memoryOverlay = false; game.player = new PointF(480, 135); game.Interact();
        if (game.room != 4 || game.enemies.Count != 5 || !game.enemies.Any(e => e.Kind == EnemyKind.InkEcho)) throw new InvalidOperationException("A entrada e os inimigos originais da Galeria do Arquivo não foram carregados.");
        Enemy inkEcho = game.enemies.First(e => e.Kind == EnemyKind.InkEcho); inkEcho.Cooldown = 0; game.UpdateEnemy(inkEcho, 0.01f);
        if (inkEcho.Windup <= 0) throw new InvalidOperationException("O Eco de Tinta não anunciou o disparo à distância.");
        game.enemies.Clear(); game.drops.Clear(); game.player = new PointF(850, 330); game.Interact();
        if (game.room != 5 || !game.enemies.Any(e => e.Kind == EnemyKind.BlindScribe)) throw new InvalidOperationException("O cofre não apresentou o chefe Escriba Cego.");
        Enemy scribe = game.enemies.Single(e => e.Kind == EnemyKind.BlindScribe); game.enemies.Clear(); game.enemies.Add(scribe);
        scribe.Cooldown = 0; game.UpdateEnemy(scribe, 0.01f);
        if (scribe.Windup <= 0 || scribe.BossPattern != 1) throw new InvalidOperationException("A primeira fase do Escriba Cego não anunciou o golpe direcionado.");
        game.player = new PointF(840, 330); game.UpdateEnemy(scribe, 1f);
        scribe.Hp = 100; scribe.Cooldown = 0; game.UpdateEnemy(scribe, 0.01f);
        if (scribe.Windup <= 0 || scribe.BossPattern != 0) throw new InvalidOperationException("A segunda fase do Escriba Cego não alternou para a área de impacto.");
        game.Defeat(scribe); game.talentChoice = false;
        if (!game.drops.Any(d => d.Kind == "seal") || !game.drops.Any(d => d.Kind == "charm" && d.Amount == 3)) throw new InvalidOperationException("O Escriba Cego não deixou o Selo da Vigília e o Vidro de Vigília.");
        foreach (Drop drop in game.drops.ToArray()) game.Collect(drop);
        if (!game.archiveSeal || game.questStage != 4 || game.damage < 33 || !game.memoryOverlay) throw new InvalidOperationException("O selo não avançou a história nem concedeu seu bônus.");
        game.memoryOverlay = false; game.player = new PointF(850, 330); game.Interact();
        if (game.room != 3) throw new InvalidOperationException("A saída do cofre não retornou a Véspera.");
        game.player = CuratorPosition; game.Interact();
        if (game.questStage != 5 || game.gold != goldBeforeTown + 140 || game.potions != potionsBeforeTown + 2) throw new InvalidOperationException("Sileth não concluiu a nova etapa da investigação com sua recompensa.");
        game.memoryOverlay = false; game.player = new PointF(480, 520); game.Interact();
        if (game.room != 6 || game.enemies.Count != 6 || !game.enemies.Any(e => e.Kind == EnemyKind.DrownedMemory)) throw new InvalidOperationException("O leito do Rio Cego não abriu com sua nova criatura à distância.");
        Enemy drownedMemory = game.enemies.First(e => e.Kind == EnemyKind.DrownedMemory); drownedMemory.Cooldown = 0; game.UpdateEnemy(drownedMemory, 0.01f);
        if (drownedMemory.Windup <= 0) throw new InvalidOperationException("A Lembrança Afogada não anunciou o ataque à distância.");
        game.enemies.Clear(); game.drops.Clear(); game.player = new PointF(850, 330); game.Interact();
        if (game.room != 7 || !game.enemies.Any(e => e.Kind == EnemyKind.RiverFerryman)) throw new InvalidOperationException("A Capela Afogada não apresentou o Barqueiro Oco.");
        Enemy ferryman = game.enemies.Single(e => e.Kind == EnemyKind.RiverFerryman); game.enemies.Clear(); game.enemies.Add(ferryman);
        ferryman.Cooldown = 0; game.UpdateEnemy(ferryman, 0.01f);
        if (ferryman.Windup <= 0 || ferryman.BossPattern != 1) throw new InvalidOperationException("O Barqueiro Oco não anunciou seu ataque marcado.");
        game.player = new PointF(840, 330); game.UpdateEnemy(ferryman, 1f); ferryman.Hp = 100; ferryman.Cooldown = 0; game.UpdateEnemy(ferryman, 0.01f);
        if (ferryman.BossPattern != 2) throw new InvalidOperationException("O Barqueiro Oco não alternou para o golpe em linha.");
        game.player = new PointF(150, 330); game.UpdateEnemy(ferryman, 1f); ferryman.Cooldown = 0; game.UpdateEnemy(ferryman, 0.01f);
        if (ferryman.BossPattern != 0) throw new InvalidOperationException("O Barqueiro Oco não concluiu o ciclo de três ataques.");
        game.Defeat(ferryman); game.talentChoice = false;
        if (!game.drops.Any(d => d.Kind == "bell") || !game.drops.Any(d => d.Kind == "charm" && d.Amount == 2)) throw new InvalidOperationException("O Barqueiro Oco não deixou o sino de Nara e o Fio de Travessia.");
        foreach (Drop drop in game.drops.ToArray()) game.Collect(drop);
        if (!game.riverBell || game.questStage != 6 || game.maxHealth < 120 || !game.memoryOverlay) throw new InvalidOperationException("O sino não revelou a decisão de Nara nem concedeu vitalidade.");
        game.memoryOverlay = false; game.player = new PointF(850, 330); game.Interact();
        if (game.room != 3) throw new InvalidOperationException("A saída da Capela Afogada não retornou a Véspera.");
        game.player = BellKeeperPosition; game.Interact();
        if (game.questStage != 7 || game.gold != goldBeforeTown + 275 || game.potions != potionsBeforeTown + 3) throw new InvalidOperationException("Edda não reconheceu o sino e encerrou esta etapa da investigação.");
        game.memoryOverlay = false; game.player = new PointF(480, 520); game.Interact();
        if (game.room != 8 || game.enemies.Count != 5 || !game.enemies.Any(e => e.Kind == EnemyKind.EmberWisp)) throw new InvalidOperationException("A escada para o Lume não abriu sua galeria de cinzas.");
        Enemy emberWisp = game.enemies.First(e => e.Kind == EnemyKind.EmberWisp); emberWisp.Cooldown = 0; game.UpdateEnemy(emberWisp, 0.01f);
        if (emberWisp.Windup <= 0) throw new InvalidOperationException("O Vestígio de Brasas não anunciou seu disparo.");
        game.enemies.Clear(); game.drops.Clear(); game.player = new PointF(850, 330); game.Interact();
        if (game.room != 9 || !game.enemies.Any(e => e.Kind == EnemyKind.AshWarden)) throw new InvalidOperationException("A Câmara do Lume não apresentou o Guardião das Cinzas.");
        Enemy ashWarden = game.enemies.Single(e => e.Kind == EnemyKind.AshWarden); game.enemies.Clear(); game.enemies.Add(ashWarden);
        ashWarden.Cooldown = 0; game.UpdateEnemy(ashWarden, 0.01f);
        if (ashWarden.Windup <= 0 || ashWarden.BossPattern != 1) throw new InvalidOperationException("O Guardião das Cinzas não anunciou a marca de luz.");
        game.player = new PointF(840, 330); game.UpdateEnemy(ashWarden, 1f); ashWarden.Hp = 120; ashWarden.Cooldown = 0; game.UpdateEnemy(ashWarden, 0.01f);
        if (ashWarden.BossPattern != 2) throw new InvalidOperationException("O Guardião das Cinzas não alternou para a onda de brasas.");
        game.player = new PointF(150, 330); game.UpdateEnemy(ashWarden, 1f); ashWarden.Cooldown = 0; game.UpdateEnemy(ashWarden, 0.01f);
        if (ashWarden.BossPattern != 0) throw new InvalidOperationException("O Guardião das Cinzas não concluiu seu ciclo de ataques.");
        game.Defeat(ashWarden); game.talentChoice = false;
        if (!game.drops.Any(d => d.Kind == "lens")) throw new InvalidOperationException("O Guardião das Cinzas não deixou o Prisma da Primeira Vigília.");
        foreach (Drop drop in game.drops.ToArray()) game.Collect(drop);
        if (!game.lumeLens || game.questStage != 9 || game.damage < 50 || !game.memoryOverlay) throw new InvalidOperationException("O prisma não revelou a memória de Nara nem aprimorou o dano.");
        game.memoryOverlay = false; game.player = new PointF(850, 330); game.Interact();
        if (game.room != 3) throw new InvalidOperationException("A saída da Câmara do Lume não retornou a Véspera.");
        game.player = CuratorPosition; game.Interact();
        if (game.questStage != 10 || game.gold != goldBeforeTown + 430 || game.potions != potionsBeforeTown + 4) throw new InvalidOperationException("Sileth não registrou a nova memória com sua recompensa.");
        game.memoryOverlay = false; game.room = 9; game.SetupRoom(); game.player = new PointF(480, 500); game.Interact();
        if (game.room != 10 || game.enemies.Count != 6 || !game.enemies.Any(e => e.Kind == EnemyKind.NameMoth)) throw new InvalidOperationException("A passagem além do prisma não abriu a Margem sem Reflexo.");
        Enemy nameMoth = game.enemies.First(e => e.Kind == EnemyKind.NameMoth); nameMoth.Cooldown = 0; game.UpdateEnemy(nameMoth, 0.01f);
        if (nameMoth.Windup <= 0) throw new InvalidOperationException("A Mariposa de Nomes não anunciou seu ataque à distância.");
        game.enemies.Clear(); game.drops.Clear(); game.player = new PointF(850, 330); game.Interact();
        if (game.room != 11 || game.enemies.Count != 4 || !game.enemies.Any(e => e.Kind == EnemyKind.HollowName)) throw new InvalidOperationException("O Reservatório não apresentou O Nome Vazio.");
        Enemy hollowName = game.enemies.Single(e => e.Kind == EnemyKind.HollowName); game.enemies.Clear(); game.enemies.Add(hollowName);
        hollowName.Cooldown = 0; game.UpdateEnemy(hollowName, 0.01f);
        if (hollowName.Windup <= 0 || hollowName.BossPattern != 1) throw new InvalidOperationException("O Nome Vazio não anunciou o primeiro golpe marcado.");
        game.player = new PointF(840, 330); game.UpdateEnemy(hollowName, 1f); hollowName.Hp = 145; hollowName.Cooldown = 0; game.UpdateEnemy(hollowName, 0.01f);
        if (hollowName.BossPattern != 2) throw new InvalidOperationException("O Nome Vazio não alternou para seu golpe em linha.");
        game.player = new PointF(150, 330); game.UpdateEnemy(hollowName, 1f); hollowName.Cooldown = 0; game.UpdateEnemy(hollowName, 0.01f);
        if (hollowName.BossPattern != 0) throw new InvalidOperationException("O Nome Vazio não concluiu o ciclo de três ataques.");
        game.Defeat(hollowName); game.talentChoice = false;
        if (!game.drops.Any(d => d.Kind == "trace")) throw new InvalidOperationException("O Nome Vazio não deixou a lembrança preservada de Nara.");
        foreach (Drop drop in game.drops.ToArray()) game.Collect(drop);
        if (!game.naraTrace || game.questStage != 11 || !game.memoryOverlay) throw new InvalidOperationException("O rastro de Nara não foi recuperado no reservatório.");
        game.memoryOverlay = false; game.player = new PointF(850, 330); game.Interact();
        if (game.room != 3) throw new InvalidOperationException("A saída do Reservatório não retornou a Véspera.");
        game.player = CuratorPosition; game.Interact();
        if (game.questStage != 12 || game.gold != goldBeforeTown + 615 || game.potions != potionsBeforeTown + 5) throw new InvalidOperationException("Sileth não reconheceu a memória que escapou do Lume.");
        SaveData saved = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(game.CaptureSaveData()))!;
        if (saved.Room != 3 || saved.QuestStage != 12 || saved.MemoriesFound != 3 || !saved.ArchiveSeal || !saved.RiverBell || !saved.LumeLens || !saved.NaraTrace || saved.MaxHealth < 120 || (saved.ClearedRooms & (1 << 9)) == 0 || (saved.ClearedRooms & (1 << 11)) == 0) throw new InvalidOperationException("O progresso da campanha, as câmaras limpas e os itens não foram serializados no save.");
        using var lootGame = new Game(); lootGame.timer.Stop(); lootGame.StartGame(); lootGame.enemies.Clear(); lootGame.room = 1;
        using var rarityGame = new Game(); rarityGame.timer.Stop(); rarityGame.StartGame(); rarityGame.damage = 25; rarityGame.ownedWeapons = 7;
        rarityGame.RollWeapon(1, 0, 4, 2);
        if (rarityGame.weaponRarities[1] != 0 || rarityGame.weaponBonuses[1] != 1 || rarityGame.weaponAffixes[1] != 0 || rarityGame.weaponAffixes2[1] != 0) throw new InvalidOperationException("A arma comum recebeu valores fora da faixa esperada.");
        rarityGame.RollWeapon(1, 62, 3, 0);
        if (rarityGame.weaponRarities[1] != 1 || rarityGame.weaponBonuses[1] != 6 || rarityGame.weaponAffixes[1] != 1 || rarityGame.weaponAffixes2[1] != 0 || !ValidWeaponData(rarityGame.CaptureSaveData())) throw new InvalidOperationException("A arma rara não recebeu bônus e afixo válidos.");
        rarityGame.EquipWeapon(1);
        if (rarityGame.damage != 41) throw new InvalidOperationException("O bônus de dano da arma não foi aplicado ao equipar.");
        rarityGame.health = 50; var healingDummy = new Enemy { Pos = rarityGame.player, Kind = EnemyKind.Grunt, Hp = 1, MaxHp = 1 }; rarityGame.enemies.Add(healingDummy); rarityGame.Defeat(healingDummy);
        if (rarityGame.health != 53) throw new InvalidOperationException("O afixo da Trégua não recuperou vida ao derrotar um inimigo.");
        rarityGame.RollWeapon(2, 95, 4, 2, 1);
        rarityGame.EquipWeapon(2);
        if (rarityGame.weaponRarities[2] != 2 || rarityGame.weaponBonuses[2] != 11 || rarityGame.weaponAffixes[2] != 3 || rarityGame.weaponAffixes2[2] != 2 || Math.Abs(rarityGame.WeaponSkillMultiplier() - 1.18f) > 0.001f || !rarityGame.WeaponDisplayName(2).Contains("Maré + Contracanto")) throw new InvalidOperationException($"A relíquia não combinou os afixos da Maré e do Contracanto ({rarityGame.weaponRarities[2]}, {rarityGame.weaponBonuses[2]}, {rarityGame.weaponAffixes[2]} + {rarityGame.weaponAffixes2[2]}, arma {rarityGame.weapon}, x{rarityGame.WeaponSkillMultiplier()}).");
        if (rarityGame.damage != 40) throw new InvalidOperationException("A troca entre armas com bônus não preservou o dano corretamente.");
        rarityGame.damage = 100;
        if (Math.Abs(rarityGame.VoiceShardDamage() - 159.3f) > 0.01f) throw new InvalidOperationException("O afixo da Maré não fortaleceu o Estilhaço de Voz.");
        if (Math.Abs(rarityGame.CriticalChance() - 0.36f) > 0.001f) throw new InvalidOperationException("O afixo do Contracanto não aumentou o crítico da relíquia.");
        SaveData raritySave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(rarityGame.CaptureSaveData()))!;
        if (!raritySave.WeaponBonuses.SequenceEqual(rarityGame.weaponBonuses) || !raritySave.WeaponRarities.SequenceEqual(rarityGame.weaponRarities) || !raritySave.WeaponAffixes.SequenceEqual(rarityGame.weaponAffixes) || !raritySave.WeaponAffixes2.SequenceEqual(rarityGame.weaponAffixes2)) throw new InvalidOperationException("A raridade e os afixos não foram preservados no save.");
        using var restoredGear = new Game(); restoredGear.timer.Stop(); restoredGear.RestoreWeaponData(raritySave);
        if (!restoredGear.weaponBonuses.SequenceEqual(rarityGame.weaponBonuses) || !restoredGear.weaponRarities.SequenceEqual(rarityGame.weaponRarities) || !restoredGear.weaponAffixes.SequenceEqual(rarityGame.weaponAffixes) || !restoredGear.weaponAffixes2.SequenceEqual(rarityGame.weaponAffixes2)) throw new InvalidOperationException("O carregamento não restaurou as rolagens das armas.");
            SaveData legacyRelic = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(raritySave))!; legacyRelic.WeaponBonuses = legacyRelic.WeaponBonuses[..3]; legacyRelic.WeaponRarities = legacyRelic.WeaponRarities[..3]; legacyRelic.WeaponAffixes = legacyRelic.WeaponAffixes[..3]; legacyRelic.WeaponAffixes2 = new int[3];
        if (!ValidWeaponData(legacyRelic)) throw new InvalidOperationException("A estrutura de três armas de um save antigo deixou de ser aceita.");
        using var migratedRelic = new Game(); migratedRelic.timer.Stop(); migratedRelic.ownedWeapons = legacyRelic.OwnedWeapons; migratedRelic.RestoreWeaponData(legacyRelic);
        if (migratedRelic.weaponAffixes2[2] != 4 || !ValidWeaponData(migratedRelic.CaptureSaveData())) throw new InvalidOperationException("Uma relíquia antiga sem segundo afixo não foi migrada com segurança.");
        SaveData legacyLootSave = JsonSerializer.Deserialize<SaveData>("{\"Room\":0}")!;
        if (!ValidWeaponData(legacyLootSave) || legacyLootSave.WeaponBonuses.Length != 6 || legacyLootSave.WeaponRarities.Length != 6 || legacyLootSave.WeaponAffixes.Length != 6 || legacyLootSave.WeaponAffixes2.Length != 6) throw new InvalidOperationException("Um save novo não recebeu os seis espaços de arma padrão.");
        SaveData fourWeaponSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(raritySave))!; fourWeaponSave.WeaponBonuses = fourWeaponSave.WeaponBonuses[..4]; fourWeaponSave.WeaponRarities = fourWeaponSave.WeaponRarities[..4]; fourWeaponSave.WeaponAffixes = fourWeaponSave.WeaponAffixes[..4]; fourWeaponSave.WeaponAffixes2 = fourWeaponSave.WeaponAffixes2[..4];
        if (!ValidWeaponData(fourWeaponSave)) throw new InvalidOperationException("A estrutura de quatro armas de um save anterior deixou de ser aceita.");
        rarityGame.Collect(new Drop { Pos = rarityGame.player, Kind = "weapon", Amount = 1 });
        if ((rarityGame.ownedWeapons & 2) == 0 || rarityGame.weaponRarities[1] is < 0 or > 2 || !ValidWeaponData(rarityGame.CaptureSaveData())) throw new InvalidOperationException("A coleta de arma não gerou uma configuração de loot válida.");
        var elite = new Enemy { Pos = new PointF(500, 330), Kind = EnemyKind.Elite, Hp = 1, MaxHp = 1 };
        lootGame.enemies.Add(elite); lootGame.Defeat(elite);
        Drop firstCharm = lootGame.drops.Single(d => d.Kind == "charm");
        if (firstCharm.Amount != 1) throw new InvalidOperationException("O Elite não deixou o Nó de Brasa.");
        lootGame.Collect(firstCharm);
        if (lootGame.equippedCharm != 1 || lootGame.WhirlwindMultiplier() <= 1.6f) throw new InvalidOperationException("O Nó de Brasa não alterou o Redemoinho.");
        lootGame.Collect(new Drop { Pos = lootGame.player, Kind = "charm", Amount = 2 });
        if (lootGame.equippedCharm2 != 2 || lootGame.DashDuration() <= 0.17f || lootGame.DashCooldown() <= 1.15f || lootGame.WhirlwindMultiplier() <= 2.16f || lootGame.WhirlwindCooldownPenalty() >= 1f) throw new InvalidOperationException("A sintonia entre Brasa e Travessia não fortaleceu o Redemoinho.");
        lootGame.Collect(new Drop { Pos = lootGame.player, Kind = "charm", Amount = 3 }); lootGame.CycleCharm(2);
        if (lootGame.equippedCharm2 != 3 || lootGame.CriticalChance() < 0.29f || lootGame.CriticalMultiplier() <= 2.5f || lootGame.BasicAttackCooldown() >= 0.39f) throw new InvalidOperationException("A sintonia entre Brasa e Vidro não ampliou os críticos.");
        lootGame.CycleCharm(1);
        if (lootGame.equippedCharm != 2 || lootGame.equippedCharm2 != 3 || lootGame.DashCooldown() >= 1.45f) throw new InvalidOperationException("A sintonia entre Travessia e Vigília não removeu a recarga adicional da esquiva.");
        SaveData charmSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(lootGame.CaptureSaveData()))!;
        if (charmSave.OwnedCharms != 7 || charmSave.EquippedCharm != 2 || charmSave.EquippedCharm2 != 3) throw new InvalidOperationException("Os espaços de talismã não foram serializados no save.");
        using var advancedCharmGame = new Game(); advancedCharmGame.timer.Stop(); advancedCharmGame.StartGame(); advancedCharmGame.damage = 100; advancedCharmGame.ownedCharms = 24; advancedCharmGame.equippedCharm = 4; advancedCharmGame.equippedCharm2 = 5;
        var bossDummy = new Enemy { Kind = EnemyKind.OathCartographer, Hp = 500, MaxHp = 500 }; var commonDummy = new Enemy { Kind = EnemyKind.Grunt, Hp = 500, MaxHp = 500 };
        if (advancedCharmGame.OutgoingDamageMultiplier(bossDummy) != 1.25f || advancedCharmGame.OutgoingDamageMultiplier(commonDummy) != 0.8f || advancedCharmGame.VoiceShardLifetime() != 1.8f || advancedCharmGame.VoiceShardCooldown() != 8.25f) throw new InvalidOperationException("A Marca da Vigília ou o Fio da Margem não aplicou seus bônus e custo.");
        advancedCharmGame.equippedCharm = 3; advancedCharmGame.equippedCharm2 = 5;
        if (advancedCharmGame.VoiceShardDamage() != 205f || advancedCharmGame.VoiceShardCooldown() != 7.5f) throw new InvalidOperationException("A sintonia entre Vidro e Margem não fortaleceu o Estilhaço de Voz.");
        SaveData expandedCharmSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(advancedCharmGame.CaptureSaveData()))!;
        if (expandedCharmSave.OwnedCharms != 24 || expandedCharmSave.EquippedCharm != 3 || expandedCharmSave.EquippedCharm2 != 5) throw new InvalidOperationException("Os novos talismãs não foram mantidos no save.");
        using var restoreGame = new Game(); restoreGame.timer.Stop(); restoreGame.StartGame(); restoreGame.storyOverlay = false; restoreGame.room = 3; restoreGame.questStage = 12; restoreGame.naraTrace = true; restoreGame.player = CuratorPosition;
        restoreGame.InteractTown(); if (!restoreGame.endingChoiceOpen) throw new InvalidOperationException("Sileth não abriu a primeira decisão de Véspera.");
        restoreGame.OnKeyDown(null, new KeyEventArgs(Keys.D3)); if (restoreGame.endingChoiceOpen) throw new InvalidOperationException("A decisão não pôde ser adiada.");
        restoreGame.InteractTown(); restoreGame.OnKeyDown(null, new KeyEventArgs(Keys.D1));
        if (restoreGame.townEnding != 1 || restoreGame.questStage != 13 || restoreGame.maxHealth != 120 || !restoreGame.memoryOverlay) throw new InvalidOperationException("Restituir os nomes não alterou a praça e a progressão.");
        SaveData restoreSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(restoreGame.CaptureSaveData()))!;
        if (restoreSave.TownEnding != 1 || restoreSave.QuestStage != 13) throw new InvalidOperationException("A decisão de devolver os nomes não foi salva.");
        using var lumeGame = new Game(); lumeGame.timer.Stop(); lumeGame.StartGame(); lumeGame.storyOverlay = false; lumeGame.room = 3; lumeGame.questStage = 12; lumeGame.naraTrace = true; lumeGame.player = CuratorPosition;
        lumeGame.InteractTown(); lumeGame.OnKeyDown(null, new KeyEventArgs(Keys.D2));
        if (lumeGame.townEnding != 2 || lumeGame.questStage != 13 || lumeGame.damage != 35 || !lumeGame.memoryOverlay) throw new InvalidOperationException("Manter o Lume não criou seu ramo e seu bônus próprios.");
        SaveData lumeSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(lumeGame.CaptureSaveData()))!;
        if (lumeSave.TownEnding != 2 || lumeSave.QuestStage != 13) throw new InvalidOperationException("A decisão de manter o Lume não foi salva.");
        using var beyondGame = new Game(); beyondGame.timer.Stop(); beyondGame.StartGame(); beyondGame.storyOverlay = false; beyondGame.questStage = 13; beyondGame.townEnding = 1; beyondGame.naraTrace = true; beyondGame.room = 11; beyondGame.clearedRooms = 1 << 11; beyondGame.SetupRoom();
        beyondGame.player = new PointF(850, 330); beyondGame.Interact();
        if (beyondGame.room != 12 || beyondGame.enemies.Count != 6 || !beyondGame.enemies.Any(e => e.Kind == EnemyKind.NameMoth)) throw new InvalidOperationException("A decisão não abriu a Costa das Vozes Tardias.");
        beyondGame.enemies.Clear(); beyondGame.drops.Clear(); beyondGame.player = new PointF(850, 330); beyondGame.Interact();
        if (beyondGame.room != 13 || !beyondGame.enemies.Any(e => e.Kind == EnemyKind.NameWeaver)) throw new InvalidOperationException("O Claustro do Coro Partido não apresentou sua chefe.");
        Enemy weaver = beyondGame.enemies.Single(e => e.Kind == EnemyKind.NameWeaver);
        if (weaver.MaxHp != 410) throw new InvalidOperationException("Os nomes restaurados não enfraqueceram a Tecelã.");
        weaver.Cooldown = 0; beyondGame.UpdateEnemy(weaver, 0.01f);
        if (weaver.Windup <= 0 || weaver.BossPattern != 1) throw new InvalidOperationException("A Tecelã não anunciou a primeira marca dirigida.");
        weaver.Windup = 0; weaver.Cooldown = 0; beyondGame.UpdateEnemy(weaver, 0.01f);
        if (weaver.Windup <= 0 || weaver.BossPattern != 2) throw new InvalidOperationException("A Tecelã não anunciou seu fio em linha.");
        weaver.Windup = 0; weaver.Cooldown = 0; beyondGame.UpdateEnemy(weaver, 0.01f);
        if (weaver.Windup <= 0 || weaver.BossPattern != 0) throw new InvalidOperationException("A Tecelã não concluiu seu ciclo de três ataques.");
        beyondGame.enemies.Clear(); beyondGame.enemies.Add(weaver); beyondGame.drops.Clear(); beyondGame.Defeat(weaver);
        if (!beyondGame.drops.Any(d => d.Kind == "chorus")) throw new InvalidOperationException("A Tecelã não deixou o Coro dos nomes consumidos.");
        foreach (Drop drop in beyondGame.drops.ToArray()) beyondGame.Collect(drop);
        if (!beyondGame.chorusMemory || beyondGame.questStage != 14 || !beyondGame.memoryOverlay) throw new InvalidOperationException("O Coro não revelou a origem do Silêncio de Baixo.");
        SaveData chorusSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(beyondGame.CaptureSaveData()))!;
        if (chorusSave.Room != 13 || !chorusSave.ChorusMemory || (chorusSave.ClearedRooms & (1 << 13)) == 0) throw new InvalidOperationException("A nova expedição e o Coro não foram registrados no save.");
        beyondGame.memoryOverlay = false; beyondGame.player = new PointF(850, 330); beyondGame.Interact(); beyondGame.player = CuratorPosition; beyondGame.InteractTown();
        if (beyondGame.room != 3 || beyondGame.questStage != 15 || !beyondGame.chorusMemory) throw new InvalidOperationException("Sileth não recebeu o Coro ao voltar à praça.");
        using var lumeExpedition = new Game(); lumeExpedition.timer.Stop(); lumeExpedition.StartGame(); lumeExpedition.room = 13; lumeExpedition.townEnding = 2; lumeExpedition.questStage = 13; lumeExpedition.SetupRoom();
        if (lumeExpedition.enemies.Single(e => e.Kind == EnemyKind.NameWeaver).MaxHp != 455) throw new InvalidOperationException("Manter o Lume não alterou o perigo da expedição.");
        using var firstExpedition = new Game(); firstExpedition.timer.Stop(); firstExpedition.StartGame(); firstExpedition.storyOverlay = false; firstExpedition.room = 13; firstExpedition.townEnding = 1; firstExpedition.questStage = 15; firstExpedition.chorusMemory = true; firstExpedition.clearedRooms = 1 << 13; firstExpedition.SetupRoom();
        firstExpedition.player = new PointF(480, 500); firstExpedition.Interact();
        if (firstExpedition.room != 14 || firstExpedition.enemies.Count != 6) throw new InvalidOperationException("O Coro não abriu a Bacia do Primeiro Nome sob o Claustro.");
        firstExpedition.enemies.Clear(); firstExpedition.drops.Clear(); firstExpedition.player = new PointF(850, 330); firstExpedition.Interact();
        if (firstExpedition.room != 15 || firstExpedition.enemies.Single(e => e.Kind == EnemyKind.FirstKeeper).MaxHp != 560) throw new InvalidOperationException("A Sala da Primeira Vigília não respondeu à escolha de devolver os nomes.");
        Enemy firstKeeper = firstExpedition.enemies.Single(e => e.Kind == EnemyKind.FirstKeeper); firstKeeper.Cooldown = 0; firstExpedition.UpdateEnemy(firstKeeper, 0.01f);
        if (firstKeeper.BossPattern != 1 || firstKeeper.Windup <= 0) throw new InvalidOperationException("A Primeira Vigília não anunciou a marca de memória.");
        firstKeeper.Windup = 0; firstKeeper.Cooldown = 0; firstExpedition.UpdateEnemy(firstKeeper, 0.01f);
        if (firstKeeper.BossPattern != 2 || firstKeeper.Target2 == PointF.Empty || firstKeeper.Target2Start == PointF.Empty || DistanceToSegment(firstExpedition.player, firstKeeper.Target2Start, firstKeeper.Target2) >= 25) throw new InvalidOperationException("A Primeira Vigília não anunciou o cruzamento de vozes sobre o jogador.");
        firstKeeper.Windup = 0.01f; firstExpedition.health = 100; firstExpedition.player = new PointF(150, 330); firstExpedition.UpdateEnemy(firstKeeper, 0.02f);
        if (firstExpedition.health >= 100) throw new InvalidOperationException("O cruzamento anunciado não atingiu a posição marcada.");
        firstKeeper.Windup = 0.01f; firstExpedition.health = 100; firstExpedition.dead = false; firstExpedition.dashTime = 0.2f; firstExpedition.UpdateEnemy(firstKeeper, 0.02f);
        if (firstExpedition.health != 100) throw new InvalidOperationException("A esquiva não evitou o cruzamento da Primeira Vigília.");
        firstExpedition.dashTime = 0; firstKeeper.Windup = 0; firstKeeper.Cooldown = 0; firstExpedition.UpdateEnemy(firstKeeper, 0.01f);
        if (firstKeeper.BossPattern != 0 || firstKeeper.Windup <= 0) throw new InvalidOperationException("A Primeira Vigília não concluiu seus três ataques anunciados.");
        firstExpedition.enemies.Clear(); firstExpedition.enemies.Add(firstKeeper); firstKeeper.Hp = 1; firstKeeper.Windup = 0; firstExpedition.Defeat(firstKeeper);
        if (!firstExpedition.drops.Any(d => d.Kind == "first") || !firstExpedition.drops.Any(d => d.Kind == "charm" && d.Amount == 4)) throw new InvalidOperationException("A guardiã não deixou a memória de Ilyra e a Marca da Vigília.");
        foreach (Drop drop in firstExpedition.drops.ToArray()) firstExpedition.Collect(drop);
        if (!firstExpedition.firstMemory || (firstExpedition.ownedCharms & 8) == 0 || firstExpedition.questStage != 16 || !firstExpedition.memoryOverlay) throw new InvalidOperationException("A primeira memória ou a Marca da Vigília não foi recuperada.");
        SaveData firstSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(firstExpedition.CaptureSaveData()))!;
        if (!firstSave.FirstMemory || firstSave.Room != 15 || (firstSave.ClearedRooms & (1 << 15)) == 0) throw new InvalidOperationException("A nova região e a memória de Ilyra não foram salvas.");
        firstExpedition.memoryOverlay = false; firstExpedition.player = new PointF(850, 330); firstExpedition.Interact(); firstExpedition.player = CuratorPosition; firstExpedition.InteractTown();
        if (firstExpedition.room != 3 || firstExpedition.questStage != 17) throw new InvalidOperationException("Sileth não registrou o testemunho da Primeira Vigília.");
        firstExpedition.player = BellKeeperPosition; firstExpedition.InteractTown();
        if (firstExpedition.memoryTitle != "A TRAVESSIA DO CONTRA-RIO" || firstExpedition.questStage != 18 || firstExpedition.room != 16 || firstExpedition.enemies.Count != 6 || !firstExpedition.enemies.Any(e => e.Kind == EnemyKind.MudCantor)) throw new InvalidOperationException("Edda não iniciou a travessia jogável para oeste.");
        firstExpedition.enemies.Clear(); firstExpedition.drops.Clear(); firstExpedition.player = new PointF(850, 330); firstExpedition.Interact();
        if (firstExpedition.room != 17 || firstExpedition.enemies.Count != 4) throw new InvalidOperationException("O Vau não levou ao Pátio das Testemunhas.");
        Enemy cartographer = firstExpedition.enemies.Single(e => e.Kind == EnemyKind.OathCartographer);
        if (cartographer.MaxHp != 610) throw new InvalidOperationException("Devolver os nomes não enfraqueceu a Cartógrafa do Vão.");
        cartographer.Cooldown = 0; firstExpedition.UpdateEnemy(cartographer, 0.01f);
        if (cartographer.BossPattern != 1 || cartographer.Windup <= 0) throw new InvalidOperationException("A Cartógrafa não anunciou a marca de testemunho.");
        cartographer.Windup = 0; cartographer.Cooldown = 0; firstExpedition.UpdateEnemy(cartographer, 0.01f);
        if (cartographer.BossPattern != 2 || cartographer.Windup <= 0 || cartographer.Target == PointF.Empty) throw new InvalidOperationException("A Cartógrafa não anunciou o traço em linha.");
        firstExpedition.enemies.Clear(); firstExpedition.enemies.Add(cartographer); cartographer.Hp = 1; cartographer.Windup = 0; firstExpedition.Defeat(cartographer);
        if (!firstExpedition.drops.Any(d => d.Kind == "west") || !firstExpedition.drops.Any(d => d.Kind == "charm" && d.Amount == 5)) throw new InvalidOperationException("A Cartógrafa não deixou o mapa e o Fio da Margem.");
        foreach (Drop drop in firstExpedition.drops.ToArray()) firstExpedition.Collect(drop);
        if (!firstExpedition.westTrace || (firstExpedition.ownedCharms & 16) == 0 || firstExpedition.questStage != 19 || !firstExpedition.memoryOverlay) throw new InvalidOperationException("O mapa e o Fio da Margem não foram recuperados.");
        SaveData westSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(firstExpedition.CaptureSaveData()))!;
        if (!westSave.WestTrace || westSave.QuestStage != 19 || westSave.Room != 17 || (westSave.ClearedRooms & (1 << 17)) == 0) throw new InvalidOperationException("O progresso no Vau não foi registrado no save.");
        firstExpedition.memoryOverlay = false; firstExpedition.drops.Clear(); firstExpedition.player = new PointF(850, 330); firstExpedition.Interact();
        if (firstExpedition.room != 3) throw new InvalidOperationException("O caminho de volta do refúgio não foi aberto.");
        firstExpedition.player = BellKeeperPosition; firstExpedition.InteractTown();
        if (firstExpedition.questStage != 20 || firstExpedition.memoryTitle != "A CARTA DO REFÚGIO" || firstExpedition.gold < 375) throw new InvalidOperationException("Edda não reconheceu o mapa nem recompensou a travessia.");
        using var lumeFirstExpedition = new Game(); lumeFirstExpedition.timer.Stop(); lumeFirstExpedition.StartGame(); lumeFirstExpedition.room = 15; lumeFirstExpedition.townEnding = 2; lumeFirstExpedition.questStage = 15; lumeFirstExpedition.chorusMemory = true; lumeFirstExpedition.SetupRoom();
        if (lumeFirstExpedition.enemies.Single(e => e.Kind == EnemyKind.FirstKeeper).MaxHp != 620) throw new InvalidOperationException("Manter o Lume não alterou a força da Primeira Vigília.");
        using var lumeWestExpedition = new Game(); lumeWestExpedition.timer.Stop(); lumeWestExpedition.StartGame(); lumeWestExpedition.room = 17; lumeWestExpedition.townEnding = 2; lumeWestExpedition.questStage = 18; lumeWestExpedition.firstMemory = true; lumeWestExpedition.SetupRoom();
        if (lumeWestExpedition.enemies.Single(e => e.Kind == EnemyKind.OathCartographer).MaxHp != 690) throw new InvalidOperationException("Manter o Lume não alterou a força da Cartógrafa do Vão.");
        using var refugeExpedition = new Game(); refugeExpedition.timer.Stop(); refugeExpedition.StartGame(); refugeExpedition.storyOverlay = false; refugeExpedition.room = 3; refugeExpedition.questStage = 20; refugeExpedition.westTrace = true; refugeExpedition.townEnding = 1; refugeExpedition.player = BellKeeperPosition;
        refugeExpedition.InteractTown();
        if (refugeExpedition.room != 18 || refugeExpedition.questStage != 21 || refugeExpedition.enemies.Count != 0) throw new InvalidOperationException("Edda não abriu o refúgio seguro após a carta de Nara.");
        refugeExpedition.player = AvenPosition; refugeExpedition.Interact();
        if (refugeExpedition.questStage != 22 || refugeExpedition.memoryOverlay) { refugeExpedition.memoryOverlay = false; if (refugeExpedition.questStage != 22) throw new InvalidOperationException("Aven não abriu a trilha após explicar as regras da comunidade."); }
        refugeExpedition.player = new PointF(850, 330); refugeExpedition.Interact();
        if (refugeExpedition.room != 19 || refugeExpedition.enemies.Count != 6 || !refugeExpedition.enemies.Any(e => e.Kind == EnemyKind.PaperWraith)) throw new InvalidOperationException("A Trilha dos Luminários não abriu ou não apresentou os Espectros de Papel.");
        refugeExpedition.enemies.Clear(); refugeExpedition.drops.Clear(); refugeExpedition.player = new PointF(850, 330); refugeExpedition.Interact();
        if (refugeExpedition.room != 20 || refugeExpedition.enemies.Count != 4 || !refugeExpedition.enemies.Any(e => e.Kind == EnemyKind.NameBroker)) throw new InvalidOperationException("A cisterna não apresentou o Escrivão de Contrato e sua guarda.");
        Enemy broker = refugeExpedition.enemies.Single(e => e.Kind == EnemyKind.NameBroker);
        if (broker.MaxHp != 720 || refugeExpedition.Blocked(broker.Pos, broker.Radius)) throw new InvalidOperationException("O chefe do registro não respeitou o ramo de Véspera ou surgiu dentro de uma parede.");
        broker.Cooldown = 0; refugeExpedition.UpdateEnemy(broker, 0.01f);
        if (broker.BossPattern != 1 || broker.Windup <= 0) throw new InvalidOperationException("O Escrivão não anunciou a primeira marca contratual.");
        broker.Windup = 0; broker.Cooldown = 0; refugeExpedition.UpdateEnemy(broker, 0.01f);
        if (broker.BossPattern != 2 || broker.Windup <= 0 || broker.Target == PointF.Empty) throw new InvalidOperationException("O Escrivão não anunciou o traço de contrato em linha.");
        broker.Windup = 0; broker.Cooldown = 0; refugeExpedition.UpdateEnemy(broker, 0.01f);
        if (broker.BossPattern != 0 || broker.Windup <= 0) throw new InvalidOperationException("O Escrivão não concluiu seu ciclo de três ataques.");
        refugeExpedition.enemies.Clear(); refugeExpedition.enemies.Add(broker); refugeExpedition.drops.Clear(); broker.Hp = 1; broker.Windup = 0; refugeExpedition.Defeat(broker);
        if (!refugeExpedition.drops.Any(d => d.Kind == "register")) throw new InvalidOperationException("O Escrivão não deixou o registro de passagem.");
        foreach (Drop drop in refugeExpedition.drops.ToArray()) refugeExpedition.Collect(drop);
        if (refugeExpedition.questStage != 23 || !refugeExpedition.memoryOverlay || refugeExpedition.drops.Count != 0) throw new InvalidOperationException("O registro não respeitou o consentimento dos viajantes nem atualizou a missão.");
        SaveData refugeSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(refugeExpedition.CaptureSaveData()))!;
        if (refugeSave.Room != 20 || refugeSave.QuestStage != 23 || (refugeSave.ClearedRooms & (1 << 20)) == 0) throw new InvalidOperationException("A etapa nova e a cisterna não foram persistidas no save.");
        refugeExpedition.memoryOverlay = false; refugeExpedition.player = new PointF(90, 330); refugeExpedition.Interact();
        refugeExpedition.player = new PointF(90, 330); refugeExpedition.Interact();
        if (refugeExpedition.room != 18) throw new InvalidOperationException("O retorno da cisterna não levou de volta ao refúgio.");
        int refugeGold = refugeExpedition.gold; refugeExpedition.player = AvenPosition; refugeExpedition.Interact();
        if (refugeExpedition.questStage != 24 || refugeExpedition.gold != refugeGold + 240 || refugeExpedition.potions < 4) throw new InvalidOperationException("Aven não reconheceu o registro nem forneceu provisões.");
        using var lumeRefuge = new Game(); lumeRefuge.timer.Stop(); lumeRefuge.StartGame(); lumeRefuge.room = 20; lumeRefuge.townEnding = 2; lumeRefuge.questStage = 22; lumeRefuge.westTrace = true; lumeRefuge.firstMemory = true; lumeRefuge.chorusMemory = true; lumeRefuge.SetupRoom();
        if (lumeRefuge.enemies.Single(e => e.Kind == EnemyKind.NameBroker).MaxHp != 790) throw new InvalidOperationException("Manter o Lume não alterou a força do Escrivão de Contrato.");
        using var upstreamExpedition = new Game(); upstreamExpedition.timer.Stop(); upstreamExpedition.StartGame(); upstreamExpedition.storyOverlay = false; upstreamExpedition.room = 18; upstreamExpedition.questStage = 24; upstreamExpedition.westTrace = true; upstreamExpedition.townEnding = 1; upstreamExpedition.player = new PointF(850, 330); upstreamExpedition.Interact();
        if (upstreamExpedition.room != 21 || upstreamExpedition.questStage != 25 || upstreamExpedition.enemies.Count != 6 || !upstreamExpedition.enemies.Any(e => e.Kind == EnemyKind.SaltEcho)) throw new InvalidOperationException($"A nova trilha falhou: sala {upstreamExpedition.room}, etapa {upstreamExpedition.questStage}, inimigos {upstreamExpedition.enemies.Count}, salinos {upstreamExpedition.enemies.Count(e => e.Kind == EnemyKind.SaltEcho)}.");
        upstreamExpedition.enemies.Clear(); upstreamExpedition.drops.Clear(); upstreamExpedition.player = new PointF(850, 330); upstreamExpedition.Interact();
        if (upstreamExpedition.room != 22 || upstreamExpedition.enemies.Count != 5 || upstreamExpedition.memoryTitle != "PONTE DA MARÉ INVERTIDA") throw new InvalidOperationException("A Várzea não levou à Ponte da Maré Invertida.");
        Enemy tideWitness = upstreamExpedition.enemies.Single(e => e.Kind == EnemyKind.TideWitness);
        if (tideWitness.MaxHp != 820 || upstreamExpedition.Blocked(tideWitness.Pos, tideWitness.Radius)) throw new InvalidOperationException("A Testemunha da Maré não respeitou a escolha da cidade ou surgiu dentro da ponte.");
        tideWitness.Cooldown = 0; upstreamExpedition.player = new PointF(150, 330); upstreamExpedition.UpdateEnemy(tideWitness, 0.01f);
        if (tideWitness.BossPattern != 1 || tideWitness.Windup <= 0 || tideWitness.Target != upstreamExpedition.player) throw new InvalidOperationException("A Testemunha não anunciou a marca d'água.");
        tideWitness.Windup = 0; tideWitness.Cooldown = 0; upstreamExpedition.UpdateEnemy(tideWitness, 0.01f);
        if (tideWitness.BossPattern != 2 || tideWitness.Windup <= 0 || tideWitness.Target == PointF.Empty) throw new InvalidOperationException("A Testemunha não anunciou o corte ao longo da corrente.");
        tideWitness.Windup = 0; tideWitness.Cooldown = 0; upstreamExpedition.UpdateEnemy(tideWitness, 0.01f);
        if (tideWitness.BossPattern != 0 || tideWitness.Windup <= 0) throw new InvalidOperationException("A Testemunha não concluiu o ciclo de três ataques.");
        upstreamExpedition.enemies.Clear(); upstreamExpedition.enemies.Add(tideWitness); upstreamExpedition.drops.Clear(); tideWitness.Hp = 1; tideWitness.Windup = 0; upstreamExpedition.Defeat(tideWitness);
        if (!upstreamExpedition.drops.Any(d => d.Kind == "witness") || !upstreamExpedition.drops.Any(d => d.Kind == "glass" && d.Amount == 3)) throw new InvalidOperationException("A Testemunha da Maré não deixou a memória e o sal vítreo para a comunidade.");
        foreach (Drop drop in upstreamExpedition.drops.ToArray()) upstreamExpedition.Collect(drop);
        if (!upstreamExpedition.upstreamTrace || upstreamExpedition.questStage != 26 || upstreamExpedition.memoryTitle != "A ÚLTIMA CURVA DE NARA" || upstreamExpedition.tideGlass != 3) throw new InvalidOperationException("A lembrança da nascente alterou o destino de Nara ou perdeu o material de têmpera.");
        SaveData upstreamSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(upstreamExpedition.CaptureSaveData()))!;
        if (!upstreamSave.UpstreamTrace || upstreamSave.QuestStage != 26 || upstreamSave.Room != 22 || (upstreamSave.ClearedRooms & (1 << 22)) == 0) throw new InvalidOperationException("A nova região e a lembrança da nascente não foram salvas.");
        upstreamExpedition.memoryOverlay = false; upstreamExpedition.player = new PointF(850, 330); upstreamExpedition.Interact(); upstreamExpedition.player = AvenPosition; upstreamExpedition.Interact();
        if (upstreamExpedition.room != 18 || upstreamExpedition.questStage != 27 || upstreamExpedition.memoryTitle != "A TESTEMUNHA SEM REGISTRO" || upstreamExpedition.gold < 320) throw new InvalidOperationException("Aven não registrou o testemunho preservando a escolha de Nara.");
        upstreamExpedition.memoryOverlay = false; upstreamExpedition.Interact();
        if (!upstreamExpedition.forgeMenuOpen) throw new InvalidOperationException("Aven não abriu a têmpera comunitária após receber o testemunho.");
        int goldBeforeTempering = upstreamExpedition.gold, damageBeforeTempering = upstreamExpedition.damage;
        upstreamExpedition.OnKeyDown(upstreamExpedition, new KeyEventArgs(Keys.D3));
        if (upstreamExpedition.forgeMenuOpen || upstreamExpedition.gold != goldBeforeTempering - 75 || upstreamExpedition.tideGlass != 2 || upstreamExpedition.weaponRarities[0] != 1 || upstreamExpedition.weaponBonuses[0] is < 3 or > 6 || upstreamExpedition.weaponAffixes[0] != 3 || upstreamExpedition.damage != damageBeforeTempering + upstreamExpedition.weaponBonuses[0]) throw new InvalidOperationException("A têmpera não aprimorou a arma comum com o afixo escolhido e os custos corretos.");
        int temperedBonus = upstreamExpedition.weaponBonuses[0], goldAfterFirstTempering = upstreamExpedition.gold;
        upstreamExpedition.Interact(); upstreamExpedition.OnKeyDown(upstreamExpedition, new KeyEventArgs(Keys.D2));
        if (upstreamExpedition.weaponRarities[0] != 1 || upstreamExpedition.weaponBonuses[0] != temperedBonus || upstreamExpedition.weaponAffixes[0] != 2 || upstreamExpedition.gold != goldAfterFirstTempering - 75 || upstreamExpedition.tideGlass != 1) throw new InvalidOperationException("A têmpera não substituiu o afixo sem apagar a raridade já obtida.");
        int goldBeforeDuplicate = upstreamExpedition.gold;
        upstreamExpedition.Interact(); upstreamExpedition.OnKeyDown(upstreamExpedition, new KeyEventArgs(Keys.D2));
        if (upstreamExpedition.gold != goldBeforeDuplicate || upstreamExpedition.tideGlass != 1 || !upstreamExpedition.forgeMenuOpen) throw new InvalidOperationException("Um afixo repetido consumiu recursos sem alterar a arma.");
        upstreamExpedition.OnKeyDown(upstreamExpedition, new KeyEventArgs(Keys.Escape));
        SaveData temperedSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(upstreamExpedition.CaptureSaveData()))!;
        if (!ValidWeaponData(temperedSave) || temperedSave.TideGlass != 1 || temperedSave.WeaponAffixes[0] != 2 || temperedSave.WeaponRarities[0] != 1) throw new InvalidOperationException("A têmpera ou o sal vítreo não foram preservados no save.");
        using (var affixForge = new Game())
        {
            affixForge.timer.Stop(); affixForge.StartGame(); affixForge.storyOverlay = false; affixForge.room = 18; affixForge.questStage = 27; affixForge.player = AvenPosition; affixForge.gold = 150; affixForge.tideGlass = 2; affixForge.forgeMenuOpen = true;
            affixForge.OnKeyDown(affixForge, new KeyEventArgs(Keys.D4));
            if (affixForge.weaponAffixes[0] != 4 || Math.Abs(affixForge.VoiceShardCooldown() - 6.3f) > 0.01f || affixForge.WeaponAffixDescription(0) != "do Passo Célere: recarga de técnicas -10%") throw new InvalidOperationException("O Passo Célere não reduziu a recarga nem mostrou seu efeito raro.");
            affixForge.forgeMenuOpen = true; affixForge.OnKeyDown(affixForge, new KeyEventArgs(Keys.D5));
            float beforeGuardedHit = affixForge.health; affixForge.HitPlayer(100);
            if (affixForge.weaponAffixes[0] != 5 || Math.Abs((beforeGuardedHit - affixForge.health) - 92) > 0.01f || affixForge.WeaponAffixDescription(0) != "do Lastro: dano recebido -8%") throw new InvalidOperationException("O Lastro não reduziu o dano recebido nem mostrou seu efeito raro.");
            affixForge.weaponRarities[0] = 2; affixForge.weaponBonuses[0] = 7; affixForge.weaponAffixes[0] = 4; affixForge.weaponAffixes2[0] = 3;
            if (Math.Abs(affixForge.VoiceShardCooldown() - 5.88f) > 0.01f || affixForge.WeaponAffixDescription(0) != "do Passo Célere: recarga de técnicas -16% • da Maré: +18% de dano de técnica") throw new InvalidOperationException("A força do Passo Célere não cresceu na raridade Relíquia.");
            if (Math.Abs(affixForge.WeaponSkillMultiplier() - 1.18f) > 0.01f || !ValidWeaponData(affixForge.CaptureSaveData())) throw new InvalidOperationException("O segundo afixo da relíquia não fortaleceu a build nem passou pela validação.");
            affixForge.weaponAffixes[0] = 5; affixForge.health = 100; affixForge.HitPlayer(100);
            if (Math.Abs(affixForge.health - 12) > 0.01f || affixForge.WeaponAffixDescription(0) != "do Lastro: dano recebido -12% • da Maré: +18% de dano de técnica") throw new InvalidOperationException("Os dois afixos da relíquia não aplicaram ou não exibiram seus efeitos.");
            SaveData newAffixesSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(affixForge.CaptureSaveData()))!;
            if (!ValidWeaponData(newAffixesSave) || newAffixesSave.WeaponAffixes[0] != 5) throw new InvalidOperationException("Os novos afixos não passaram pela validação nem persistiram no save.");
            affixForge.gold = 75; affixForge.tideGlass = 1; affixForge.forgeMenuOpen = true; affixForge.ReforgeWeapon(3);
            if (affixForge.weaponAffixes[0] != 3 || affixForge.weaponAffixes2[0] != 5 || affixForge.gold != 0 || affixForge.tideGlass != 0 || !ValidWeaponData(affixForge.CaptureSaveData())) throw new InvalidOperationException("A têmpera não reordenou os dois afixos da Relíquia sem duplicá-los.");
            affixForge.gold = 75; affixForge.tideGlass = 1; affixForge.forgeMenuOpen = true; affixForge.OnKeyDown(affixForge, new KeyEventArgs(Keys.D6));
            if (affixForge.weaponAffixes[0] != 6 || affixForge.weaponAffixes2[0] != 5 || Math.Abs(affixForge.BasicAttackReach(new Enemy { Kind = EnemyKind.Grunt }) - 93.6f) > 0.01f || !ValidWeaponData(affixForge.CaptureSaveData()))
                throw new InvalidOperationException("A têmpera não permitiu escolher o Fio Esticado sem invalidar a Relíquia.");
            affixForge.weapon = 0; affixForge.player = new PointF(300, 300); affixForge.weaponRarities[0] = 2; affixForge.weaponAffixes[0] = 2; affixForge.weaponAffixes2[0] = 6;
            float baseCriticalChance = affixForge.CriticalChance(); var closeTarget = new Enemy { Pos = new PointF(350, 300), Kind = EnemyKind.Grunt }; var distantTarget = new Enemy { Pos = new PointF(380, 300), Kind = EnemyKind.Grunt };
            if (Math.Abs(affixForge.BasicAttackCriticalChance(closeTarget) - baseCriticalChance) > 0.001f || Math.Abs(affixForge.BasicAttackCriticalChance(distantTarget) - baseCriticalChance - 0.1f) > 0.001f || !affixForge.WeaponAffixDescription(0).Contains("Ressonância do Contracanto: +10 p.p. de crítico no alcance longo")) throw new InvalidOperationException("A Relíquia Contracanto + Fio Esticado não especializou os críticos de alcance longo.");
            affixForge.weaponAffixes[0] = 3; affixForge.weaponAffixes2[0] = 7; affixForge.skillCd = 1;
            if (Math.Abs(affixForge.WeaponCriticalCooldownAdvance(0) - 0.35f) > 0.001f || !affixForge.WeaponAffixDescription(0).Contains("Ressonância da Maré: Cadência adianta +0,10 s por crítico")) throw new InvalidOperationException("A combinação Maré + Cadência da Relíquia não desbloqueou sua sinergia documentada.");
            affixForge.AdvanceTechniqueCooldowns(affixForge.WeaponCriticalCooldownAdvance(0));
            if (Math.Abs(affixForge.skillCd - 0.65f) > 0.001f) throw new InvalidOperationException("A Ressonância da Maré não adiantou a recarga pelo valor combinado dos afixos.");
            affixForge.weaponRarities[0] = 2; affixForge.weaponAffixes[0] = 1; affixForge.weaponAffixes2[0] = 5; affixForge.health = 50; affixForge.maxHealth = 100;
            if (affixForge.WeaponKillHealing() != 12 || !affixForge.WeaponAffixDescription(0).Contains("Ressonância da Trégua: abaixo de metade da vida, abates curam +6")) throw new InvalidOperationException("A combinação Trégua + Lastro não revelou a cura de risco baixo.");
            var sustainDummy = new Enemy { Pos = affixForge.player, Kind = EnemyKind.Grunt, Hp = 1, MaxHp = 1 }; affixForge.enemies.Add(sustainDummy); affixForge.Defeat(sustainDummy);
            if (affixForge.health != 62) throw new InvalidOperationException("A Ressonância da Trégua não recuperou vida ao abater sob metade da vida.");
            affixForge.health = 51; var healthyDummy = new Enemy { Pos = affixForge.player, Kind = EnemyKind.Grunt, Hp = 1, MaxHp = 1 }; affixForge.enemies.Add(healthyDummy); affixForge.Defeat(healthyDummy);
            if (affixForge.health != 57) throw new InvalidOperationException("A cura adicional da Ressonância da Trégua foi aplicada acima do limiar de risco.");
            affixForge.weaponAffixes[0] = 1; affixForge.weaponAffixes2[0] = 4; affixForge.skillCd = 1f; affixForge.shardCd = 0.4f;
            var mercyDummy = new Enemy { Pos = affixForge.player, Kind = EnemyKind.Grunt, Hp = 1, MaxHp = 1 }; affixForge.enemies.Add(mercyDummy); affixForge.Defeat(mercyDummy);
            if (!affixForge.HasMercyTempoResonance(0) || Math.Abs(affixForge.skillCd - 0.75f) > 0.001f || Math.Abs(affixForge.shardCd - 0.15f) > 0.001f
                || !affixForge.WeaponAffixDescription(0).Contains("Ressonância da Misericórdia: abates adiantam recargas em 0,25 s"))
                throw new InvalidOperationException("Trégua + Passo Célere não adiantou recargas ao abater nem exibiu a nova ressonância.");
            affixForge.weaponRarities[0] = 1;
            if (affixForge.HasMercyTempoResonance(0) || affixForge.WeaponKillCooldownAdvance(0) != 0) throw new InvalidOperationException("A Ressonância da Misericórdia foi aplicada a uma arma Rara.");
            affixForge.weaponRarities[0] = 2;
            affixForge.player = AvenPosition; affixForge.weaponAffixes[0] = 4; affixForge.weaponAffixes2[0] = 3; affixForge.gold = 300; affixForge.tideGlass = 4; affixForge.forgeMenuOpen = true;
            affixForge.OnKeyDown(affixForge, new KeyEventArgs(Keys.A));
            if (!affixForge.secondaryForgeOpen || !affixForge.notice.Contains("AFIXO SECUNDÁRIO")) throw new InvalidOperationException("A forja não alternou para a escolha do afixo secundário.");
            affixForge.OnKeyDown(affixForge, new KeyEventArgs(Keys.D5)); SaveData targetedRelicSave = affixForge.CaptureSaveData();
            if (affixForge.weaponAffixes[0] != 4 || affixForge.weaponAffixes2[0] != 5 || affixForge.gold != 150 || affixForge.tideGlass != 2 || affixForge.secondaryForgeOpen || !ValidWeaponData(targetedRelicSave)) throw new InvalidOperationException("A têmpera secundária não preservou o afixo principal, cobrou o custo da Relíquia ou manteve o save válido.");
            affixForge.forgeMenuOpen = true; affixForge.secondaryForgeOpen = true; affixForge.OnKeyDown(affixForge, new KeyEventArgs(Keys.D4));
            if (affixForge.weaponAffixes[0] != 4 || affixForge.weaponAffixes2[0] != 5 || affixForge.gold != 150 || affixForge.tideGlass != 2 || !affixForge.forgeMenuOpen) throw new InvalidOperationException("A têmpera aceitou afixos iguais ou consumiu recursos ao recusar a escolha.");
        }
        using (var ruptureForge = new Game())
        {
            ruptureForge.timer.Stop(); ruptureForge.StartGame(); ruptureForge.storyOverlay = false; ruptureForge.room = 18; ruptureForge.questStage = 27; ruptureForge.player = AvenPosition; ruptureForge.gold = 75; ruptureForge.tideGlass = 1; ruptureForge.forgeMenuOpen = true;
            ruptureForge.OnKeyDown(ruptureForge, new KeyEventArgs(Keys.D8));
            if (ruptureForge.weaponRarities[0] != 1 || ruptureForge.weaponAffixes[0] != 8 || !ruptureForge.WeaponAffixDescription(0).Contains("da Ruptura: críticos ferem por 3 s (6% do dano da arma/s)")) throw new InvalidOperationException("A têmpera não aplicou nem explicou a Ruptura rara pelo atalho 8.");
            var woundTarget = new Enemy { Pos = new PointF(400, 330), Kind = EnemyKind.Grunt, Hp = 1000, MaxHp = 1000 }; ruptureForge.enemies.Add(woundTarget);
            ruptureForge.ApplyWeaponWound(woundTarget, false);
            if (woundTarget.WoundTime != 0) throw new InvalidOperationException("A Ruptura foi aplicada sem um acerto crítico.");
            ruptureForge.ApplyWeaponWound(woundTarget, true); float rareWoundTick = ruptureForge.damage * 0.06f;
            if (Math.Abs(woundTarget.WoundDamage - rareWoundTick) > 0.01f || woundTarget.WoundTime != 3f) throw new InvalidOperationException("A Ruptura rara não aplicou seu dano periódico e duração.");
            ruptureForge.TickEnemyWounds(0.5f); ruptureForge.ApplyWeaponWound(woundTarget, true);
            if (Math.Abs(woundTarget.WoundTickCooldown - 0.5f) > 0.01f || woundTarget.WoundTime != 3f) throw new InvalidOperationException("Um novo crítico reiniciou o relógio do próximo pulso da Ruptura.");
            ruptureForge.TickEnemyWounds(0.5f); ruptureForge.TickEnemyWounds(1); ruptureForge.TickEnemyWounds(1); ruptureForge.TickEnemyWounds(0.5f);
            if (Math.Abs(woundTarget.Hp - (1000 - rareWoundTick * 3)) > 0.02f || woundTarget.WoundTime != 0) throw new InvalidOperationException("A ferida renovada não causou exatamente três pulsos ou não expirou.");
            ruptureForge.weaponRarities[0] = 2; ruptureForge.weaponBonuses[0] = 7; ruptureForge.weaponAffixes[0] = 8; ruptureForge.weaponAffixes2[0] = 1; ruptureForge.damage = 100;
            var relicWoundTarget = new Enemy { Pos = new PointF(420, 330), Kind = EnemyKind.Grunt, Hp = 1000, MaxHp = 1000 }; ruptureForge.enemies.Add(relicWoundTarget); ruptureForge.ApplyWeaponWound(relicWoundTarget, true);
            if (Math.Abs(relicWoundTarget.WoundDamage - 8f) > 0.01f || !ruptureForge.WeaponAffixDescription(0).Contains("da Ruptura: críticos ferem por 3 s (8% do dano da arma/s)")) throw new InvalidOperationException("A Ruptura de Relíquia não aumentou o pulso nem mostrou seu valor correto.");
            ruptureForge.weaponAffixes[0] = 2; ruptureForge.weaponAffixes2[0] = 8;
            var scarTarget = new Enemy { Pos = new PointF(440, 330), Kind = EnemyKind.Grunt, Hp = 1000, MaxHp = 1000 }; ruptureForge.enemies.Add(scarTarget); ruptureForge.ApplyWeaponWound(scarTarget, true);
            if (Math.Abs(scarTarget.WoundDamage - 11.2f) > 0.01f || !ruptureForge.WeaponAffixDescription(0).Contains("Ressonância da Cicatriz: Ruptura causa +40% de dano") || !ValidWeaponData(ruptureForge.CaptureSaveData())) throw new InvalidOperationException("Contracanto + Ruptura não ativou sua ressonância ou não preservou a Relíquia no save.");
            ruptureForge.weaponAffixes[0] = 3; ruptureForge.weaponAffixes2[0] = 8;
            var tideWoundTarget = new Enemy { Pos = new PointF(460, 330), Kind = EnemyKind.Grunt, Hp = 1000, MaxHp = 1000, WoundTime = 2 };
            tideWoundTarget.WoundTime = 0; float unwoundedDamageMultiplier = ruptureForge.OutgoingDamageMultiplier(tideWoundTarget); tideWoundTarget.WoundTime = 2;
            if (!ruptureForge.HasEchoWoundResonance(0) || Math.Abs(ruptureForge.OutgoingDamageMultiplier(tideWoundTarget) - unwoundedDamageMultiplier * 1.15f) > 0.001f
                || !ruptureForge.WeaponAffixDescription(0).Contains("Ressonância do Eco Ferido: +15% de dano contra alvos sob Ruptura"))
                throw new InvalidOperationException("Maré + Ruptura não fortaleceu os golpes contra um alvo ferido ou não explicou a sinergia.");
            ruptureForge.weaponRarities[0] = 1;
            if (ruptureForge.HasEchoWoundResonance(0) || Math.Abs(ruptureForge.OutgoingDamageMultiplier(tideWoundTarget) - unwoundedDamageMultiplier) > 0.001f)
                throw new InvalidOperationException("A Ressonância do Eco Ferido também foi aplicada a uma arma Rara.");
        }
        using var lumeUpstream = new Game(); lumeUpstream.timer.Stop(); lumeUpstream.StartGame(); lumeUpstream.room = 22; lumeUpstream.questStage = 25; lumeUpstream.townEnding = 2; lumeUpstream.SetupRoom();
        if (lumeUpstream.enemies.Single(e => e.Kind == EnemyKind.TideWitness).MaxHp != 920) throw new InvalidOperationException("Manter o Lume não alterou a força da Testemunha da Maré.");
        using var reedExpedition = new Game(); reedExpedition.timer.Stop(); reedExpedition.StartGame(); reedExpedition.storyOverlay = false; reedExpedition.room = 22; reedExpedition.questStage = 27; reedExpedition.upstreamTrace = true; reedExpedition.townEnding = 1; reedExpedition.enemies.Clear(); reedExpedition.drops.Clear(); reedExpedition.player = new PointF(850, 330); reedExpedition.Interact();
        if (reedExpedition.room != 23 || reedExpedition.questStage != 28 || reedExpedition.enemies.Count != 6 || reedExpedition.memoryTitle != "COTOVELO DOS JUNCAIS") throw new InvalidOperationException("A ponte não abriu a nova curva do rio após registrar a nascente.");
        reedExpedition.enemies.Clear(); reedExpedition.drops.Clear(); reedExpedition.player = new PointF(850, 330); reedExpedition.Interact();
        if (reedExpedition.room != 24 || reedExpedition.enemies.Count != 5 || reedExpedition.memoryTitle != "PÁTIO DAS AMARRAS LIVRES") throw new InvalidOperationException("O Cotovelo não levou ao Pátio das Amarras Livres.");
        Enemy reedKeeper = reedExpedition.enemies.Single(e => e.Kind == EnemyKind.ReedKeeper);
        if (reedKeeper.MaxHp != 970 || reedExpedition.Blocked(reedKeeper.Pos, reedKeeper.Radius)) throw new InvalidOperationException("A Guardiã do Leito não respeitou a escolha da cidade ou surgiu dentro de um obstáculo.");
        reedKeeper.Cooldown = 0; reedExpedition.player = new PointF(150, 330); reedExpedition.UpdateEnemy(reedKeeper, 0.01f);
        if (reedKeeper.BossPattern != 1 || reedKeeper.Windup <= 0 || reedKeeper.Target != reedExpedition.player) throw new InvalidOperationException("A Guardiã não anunciou a marca que segue o Escutador.");
        reedKeeper.Windup = 0; reedKeeper.Cooldown = 0; reedExpedition.UpdateEnemy(reedKeeper, 0.01f);
        if (reedKeeper.BossPattern != 2 || reedKeeper.Windup <= 0 || reedKeeper.Target == PointF.Empty) throw new InvalidOperationException("A Guardiã não anunciou o corte pelo pátio.");
        reedKeeper.Windup = 0; reedKeeper.Cooldown = 0; reedExpedition.UpdateEnemy(reedKeeper, 0.01f);
        if (reedKeeper.BossPattern != 0 || reedKeeper.Windup <= 0) throw new InvalidOperationException("A Guardiã não concluiu o ciclo de três ataques.");
        reedExpedition.enemies.Clear(); reedExpedition.enemies.Add(reedKeeper); reedExpedition.drops.Clear(); reedKeeper.Hp = 1; reedKeeper.Windup = 0; reedExpedition.Defeat(reedKeeper);
        if (!reedExpedition.drops.Any(d => d.Kind == "beyond") || !reedExpedition.drops.Any(d => d.Kind == "glass" && d.Amount == 2)) throw new InvalidOperationException("A Guardiã não deixou o testemunho e o sal vítreo.");
        foreach (Drop drop in reedExpedition.drops.ToArray()) reedExpedition.Collect(drop);
        if (!reedExpedition.beyondTrace || reedExpedition.questStage != 29 || reedExpedition.memoryTitle != "NÓS QUE PODEM SER DESFEITOS" || reedExpedition.tideGlass != 2) throw new InvalidOperationException("O testemunho tornou-se uma ordem ou perdeu o material comunitário.");
        SaveData reedSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(reedExpedition.CaptureSaveData()))!;
        if (!reedSave.BeyondTrace || reedSave.QuestStage != 29 || reedSave.Room != 24 || (reedSave.ClearedRooms & (1 << 24)) == 0) throw new InvalidOperationException("A nova região e o testemunho não foram salvos.");
        reedExpedition.memoryOverlay = false; reedExpedition.player = new PointF(850, 330); reedExpedition.Interact(); reedExpedition.player = AvenPosition; int reedGold = reedExpedition.gold; reedExpedition.Interact();
        if (reedExpedition.room != 18 || reedExpedition.questStage != 30 || reedExpedition.gold != reedGold + 420 || reedExpedition.potions < 3 || reedExpedition.memoryTitle != "AVEN GUARDA O SINAL") throw new InvalidOperationException($"Aven não registrou a passagem coletiva e forneceu provisões: sala {reedExpedition.room}, etapa {reedExpedition.questStage}, ouro {reedExpedition.gold - reedGold}, poções {reedExpedition.potions}, memória {reedExpedition.memoryTitle}.");
        using var lumeReed = new Game(); lumeReed.timer.Stop(); lumeReed.StartGame(); lumeReed.room = 24; lumeReed.townEnding = 2; lumeReed.SetupRoom();
        if (lumeReed.enemies.Single(e => e.Kind == EnemyKind.ReedKeeper).MaxHp != 1060) throw new InvalidOperationException("Manter o Lume não alterou a força da Guardiã do Leito.");
        using var floodExpedition = new Game(); floodExpedition.timer.Stop(); floodExpedition.StartGame(); floodExpedition.storyOverlay = false; floodExpedition.room = 18; floodExpedition.questStage = 30; floodExpedition.beyondTrace = true; floodExpedition.upstreamTrace = true; floodExpedition.westTrace = true; floodExpedition.townEnding = 1; floodExpedition.player = new PointF(850, 330); floodExpedition.Interact();
        if (floodExpedition.room != 25 || floodExpedition.questStage != 31 || floodExpedition.enemies.Count != 6 || floodExpedition.memoryTitle != "MOINHOS DA VAZANTE") throw new InvalidOperationException("Aven não abriu a rota dos Moinhos da Vazante após registrar o testemunho.");
        floodExpedition.player = new PointF(480, 330); if (floodExpedition.CurrentDrift() != new PointF(58, 0)) throw new InvalidOperationException("A corrente dos moinhos não alterou o posicionamento no canal.");
        floodExpedition.player = new PointF(180, 330); if (floodExpedition.CurrentDrift() != PointF.Empty) throw new InvalidOperationException("A corrente afetou o caminho seco fora do canal.");
        floodExpedition.enemies.Clear(); floodExpedition.drops.Clear(); floodExpedition.player = new PointF(850, 330); floodExpedition.Interact();
        if (floodExpedition.room != 26 || floodExpedition.enemies.Count != 5 || floodExpedition.memoryTitle != "CÂMARA DO VEIO PRESO") throw new InvalidOperationException("Os moinhos não levaram à Câmara do Veio Preso.");
        Enemy flowKeeper = floodExpedition.enemies.Single(e => e.Kind == EnemyKind.FlowKeeper);
        if (flowKeeper.MaxHp != 1120 || floodExpedition.Blocked(flowKeeper.Pos, flowKeeper.Radius)) throw new InvalidOperationException("O Vigia do Veio Preso não respeitou o ramo de Véspera ou surgiu dentro de um obstáculo.");
        flowKeeper.Cooldown = 0; floodExpedition.player = new PointF(150, 330); floodExpedition.UpdateEnemy(flowKeeper, 0.01f);
        if (flowKeeper.BossPattern != 1 || flowKeeper.Windup <= 0 || flowKeeper.Target != floodExpedition.player) throw new InvalidOperationException("O Vigia não anunciou a marca de retorno.");
        flowKeeper.Windup = 0; flowKeeper.Cooldown = 0; floodExpedition.UpdateEnemy(flowKeeper, 0.01f);
        if (flowKeeper.BossPattern != 2 || flowKeeper.Windup <= 0 || flowKeeper.Target == PointF.Empty) throw new InvalidOperationException("O Vigia não anunciou o corte pela corrente.");
        flowKeeper.Windup = 0; flowKeeper.Cooldown = 0; floodExpedition.UpdateEnemy(flowKeeper, 0.01f);
        if (flowKeeper.BossPattern != 0 || flowKeeper.Windup <= 0) throw new InvalidOperationException("O Vigia não concluiu o ciclo de três ataques.");
        floodExpedition.enemies.Clear(); floodExpedition.enemies.Add(flowKeeper); floodExpedition.drops.Clear(); flowKeeper.Hp = 1; flowKeeper.Windup = 0; floodExpedition.Defeat(flowKeeper);
        if (!floodExpedition.drops.Any(d => d.Kind == "ebb") || !floodExpedition.drops.Any(d => d.Kind == "glass" && d.Amount == 3)) throw new InvalidOperationException("O Vigia não deixou a memória e o sal vítreo.");
        foreach (Drop drop in floodExpedition.drops.ToArray()) floodExpedition.Collect(drop);
        if (floodExpedition.questStage != 32 || floodExpedition.tideGlass != 3 || floodExpedition.memoryTitle != "A CORRENTE NÃO É UMA ROTA") throw new InvalidOperationException("A memória do veio ou o sal vítreo não foram recuperados.");
        SaveData flowSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(floodExpedition.CaptureSaveData()))!;
        if (flowSave.Room != 26 || flowSave.QuestStage != 32 || flowSave.EbbSeal || (flowSave.ClearedRooms & (1 << 26)) == 0) throw new InvalidOperationException("O progresso do veio não foi salvo antes da recompensa de Aven.");
        floodExpedition.memoryOverlay = false; floodExpedition.player = new PointF(850, 330); floodExpedition.Interact(); floodExpedition.player = AvenPosition; floodExpedition.gold = 0; floodExpedition.potions = 0; floodExpedition.Interact();
        if (floodExpedition.room != 18 || floodExpedition.questStage != 33 || !floodExpedition.ebbSeal || floodExpedition.gold != 480 || floodExpedition.potions != 2 || floodExpedition.memoryTitle != "A MARCA DA VAZANTE") throw new InvalidOperationException("Aven não liberou a Vazante e as provisões da comunidade.");
        SaveData ebbSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(floodExpedition.CaptureSaveData()))!;
        if (!ebbSave.EbbSeal || ebbSave.QuestStage != 33 || ebbSave.Room != 18) throw new InvalidOperationException("A habilidade Vazante não persistiu no save.");
        floodExpedition.room = 26; floodExpedition.enemies.Clear(); floodExpedition.drops.Clear(); floodExpedition.ebbSeal = true; floodExpedition.pullCd = 0; floodExpedition.player = new PointF(300, 330);
        Enemy pulledEcho = new() { Pos = new PointF(400, 330), Kind = EnemyKind.Grunt, Hp = 1000, MaxHp = 1000, Cooldown = 5 }; floodExpedition.enemies.Add(pulledEcho); floodExpedition.CastEbbPull();
        if (pulledEcho.Hp >= 1000 || pulledEcho.Pos.X >= 400 || pulledEcho.Stagger <= 0 || floodExpedition.pullCd != 12) throw new InvalidOperationException("A Vazante não reuniu, feriu e interrompeu o eco ou respeitou sua recarga.");
        using var ebbBuild = new Game(); ebbBuild.timer.Stop(); ebbBuild.StartGame(); ebbBuild.room = 26; ebbBuild.ebbSeal = true; ebbBuild.ownedCharms = 4; ebbBuild.equippedCharm = 3; ebbBuild.weaponRarities[0] = 1; ebbBuild.weaponAffixes[0] = 3; ebbBuild.enemies.Clear(); ebbBuild.player = new PointF(300, 330);
        Enemy tunedEcho = new() { Pos = new PointF(400, 330), Kind = EnemyKind.Grunt, Hp = 1000, MaxHp = 1000, Cooldown = 5 }; ebbBuild.enemies.Add(tunedEcho); ebbBuild.CastEbbPull();
        if (1000 - tunedEcho.Hp <= 15) throw new InvalidOperationException("O Vidro de Vigília e o afixo da Maré não fortaleceram a Vazante.");
        floodExpedition.pullCd = 0; floodExpedition.enemies.Clear(); Enemy pulledBoss = new() { Pos = new PointF(500, 230), Kind = EnemyKind.FlowKeeper, Hp = 1000, MaxHp = 1000 }; floodExpedition.enemies.Add(pulledBoss); floodExpedition.player = new PointF(400, 230); floodExpedition.CastEbbPull();
        if (pulledBoss.Hp >= 1000 || pulledBoss.Pos.X >= 500 || pulledBoss.Stagger > 0) throw new InvalidOperationException("A Vazante não teve efeito reduzido e sem interrupção contra chefes.");
        using var lumeFlow = new Game(); lumeFlow.timer.Stop(); lumeFlow.StartGame(); lumeFlow.room = 26; lumeFlow.questStage = 31; lumeFlow.beyondTrace = true; lumeFlow.upstreamTrace = true; lumeFlow.townEnding = 2; lumeFlow.SetupRoom();
        if (lumeFlow.enemies.Single(e => e.Kind == EnemyKind.FlowKeeper).MaxHp != 1230) throw new InvalidOperationException("Manter o Lume não alterou a força do Vigia do Veio Preso.");
        using var counterExpedition = new Game(); counterExpedition.timer.Stop(); counterExpedition.StartGame(); counterExpedition.storyOverlay = false; counterExpedition.room = 18; counterExpedition.questStage = 33; counterExpedition.ebbSeal = true; counterExpedition.player = new PointF(850, 330); counterExpedition.Interact();
        if (counterExpedition.room != 27 || counterExpedition.questStage != 34 || counterExpedition.enemies.Count != 6 || counterExpedition.memoryTitle != "ESCARPA DO CONTRAFLUXO") throw new InvalidOperationException("Aven não abriu o Capítulo XI após ensinar a Vazante.");
        counterExpedition.enemies.Clear(); counterExpedition.drops.Clear(); counterExpedition.player = new PointF(850, 330); counterExpedition.Interact();
        if (counterExpedition.room != 28 || counterExpedition.enemies.Count != 5 || !counterExpedition.enemies.Any(e => e.Kind == EnemyKind.CounterCurrent)) throw new InvalidOperationException("A escarpa não abriu o Coração das Duas Margens e seu chefe.");
        counterExpedition.player = new PointF(600, 330); counterExpedition.windupPulse = 0; PointF inwardDrift = counterExpedition.CurrentDrift(); counterExpedition.windupPulse = 3.5f / 1.35f; PointF outwardDrift = counterExpedition.CurrentDrift();
        if (inwardDrift.X >= 0 || outwardDrift.X <= 0 || inwardDrift.X != -outwardDrift.X) throw new InvalidOperationException("A corrente do Coração não alternou de atração para repulsão.");
        Enemy tuner = counterExpedition.enemies.Single(e => e.Kind == EnemyKind.CounterCurrent);
        tuner.Cooldown = 0; counterExpedition.player = new PointF(150, 330); counterExpedition.UpdateEnemy(tuner, 0.01f); if (tuner.BossPattern != 1 || tuner.Windup <= 0) throw new InvalidOperationException("O Afinador não anunciou a primeira marca de corrente.");
        tuner.Windup = 0; tuner.Cooldown = 0; counterExpedition.UpdateEnemy(tuner, 0.01f); if (tuner.BossPattern != 2 || tuner.Windup <= 0) throw new InvalidOperationException("O Afinador não anunciou seu corte em linha.");
        tuner.Windup = 0; tuner.Cooldown = 0; counterExpedition.UpdateEnemy(tuner, 0.01f); if (tuner.BossPattern != 0 || tuner.Windup <= 0) throw new InvalidOperationException("O Afinador não completou o ciclo de três ataques.");
        counterExpedition.enemies.Clear(); counterExpedition.enemies.Add(tuner); counterExpedition.drops.Clear(); tuner.Hp = 1; tuner.Windup = 0; counterExpedition.Defeat(tuner);
        if (!counterExpedition.drops.Any(d => d.Kind == "counter") || !counterExpedition.drops.Any(d => d.Kind == "glass" && d.Amount == 3)) throw new InvalidOperationException("O Afinador não deixou sua memória e o sal vítreo.");
        foreach (Drop drop in counterExpedition.drops.ToArray()) counterExpedition.Collect(drop);
        if (!counterExpedition.counterTrace || counterExpedition.questStage != 35 || counterExpedition.tideGlass != 3 || counterExpedition.memoryTitle != "O AFINADOR DAS MARGENS") throw new InvalidOperationException("A memória do Afinador ou o material não foram recuperados.");
        SaveData counterSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(counterExpedition.CaptureSaveData()))!;
        if (counterSave.Room != 28 || counterSave.QuestStage != 35 || !counterSave.CounterTrace || (counterSave.ClearedRooms & (1 << 28)) == 0) throw new InvalidOperationException("O novo capítulo não persistiu a sala, a memória e a conclusão do chefe.");
        counterExpedition.memoryOverlay = false; counterExpedition.player = new PointF(850, 330); counterExpedition.Interact(); counterExpedition.player = AvenPosition; int counterGold = counterExpedition.gold; counterExpedition.Interact();
        if (counterExpedition.room != 18 || counterExpedition.questStage != 36 || counterExpedition.gold != counterGold + 560 || counterExpedition.tideGlass != 6 || counterExpedition.potions < 2) throw new InvalidOperationException("Aven não registrou o novo relato nem entregou as provisões.");
        counterExpedition.memoryOverlay = false; counterExpedition.player = AvenPosition; counterExpedition.Interact();
        if (counterExpedition.questStage != 37 || counterExpedition.memoryTitle != "O VERTEDOURO ANTIGO") throw new InvalidOperationException("Aven não abriu o Capítulo XII nem apresentou a nova região.");
        counterExpedition.memoryOverlay = false; counterExpedition.player = new PointF(850, 330); counterExpedition.Interact();
        if (counterExpedition.room != 27 || counterExpedition.enemies.Count != 0) throw new InvalidOperationException("Aven não reabriu a passagem limpa da Escarpa para o Capítulo XII.");
        counterExpedition.player = new PointF(850, 330); counterExpedition.Interact();
        if (counterExpedition.room != 28 || counterExpedition.enemies.Count != 0) throw new InvalidOperationException("A Escarpa não retornou ao Coração das Duas Margens já limpo.");
        counterExpedition.player = new PointF(850, 330); counterExpedition.Interact();
        if (counterExpedition.room != 29 || counterExpedition.questStage != 38 || counterExpedition.enemies.Count != 6) throw new InvalidOperationException("A Cantaria das Vozes Livres não abriu como primeira sala do Capítulo XII.");
        counterExpedition.enemies.Clear(); counterExpedition.drops.Clear(); counterExpedition.player = new PointF(850, 330); counterExpedition.Interact();
        if (counterExpedition.room != 30 || counterExpedition.enemies.Count != 5 || !counterExpedition.enemies.Any(e => e.Kind == EnemyKind.RiftCantor)) throw new InvalidOperationException("A Câmara da Contravoz não abriu com seu chefe.");
        Enemy cantor = counterExpedition.enemies.Single(e => e.Kind == EnemyKind.RiftCantor); cantor.Cooldown = 0;
        for (int pattern = 1; pattern <= 3; pattern++) { counterExpedition.UpdateEnemy(cantor, 0.01f); if (cantor.BossPattern != pattern % 3 || cantor.Windup <= 0) throw new InvalidOperationException("A Contravoz não anunciou seu ciclo de três padrões."); cantor.Windup = 0; cantor.Cooldown = 0; }
        counterExpedition.enemies.Clear(); counterExpedition.enemies.Add(cantor); counterExpedition.drops.Clear(); cantor.Hp = 1; cantor.Windup = 0; counterExpedition.Defeat(cantor);
        if (!counterExpedition.drops.Any(d => d.Kind == "verse") || EnemyName(cantor) != "A CONTRAVOZ") throw new InvalidOperationException("A Contravoz não deixou sua nota única ou não recebeu seu nome.");
        foreach (Drop drop in counterExpedition.drops.ToArray()) counterExpedition.Collect(drop);
        if (!counterExpedition.riftVerse || counterExpedition.questStage != 39 || counterExpedition.memoryTitle != "A NOTA QUE NÃO MANDA") throw new InvalidOperationException("A nota da Contravoz não atualizou a memória e a missão.");
        SaveData verseSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(counterExpedition.CaptureSaveData()))!;
        if (verseSave.Room != 30 || verseSave.QuestStage != 39 || !verseSave.RiftVerse || (verseSave.ClearedRooms & (1 << 30)) == 0 || JsonSerializer.Deserialize<SaveData>("{}")?.RiftVerse != false) throw new InvalidOperationException("A progressão da Contravoz não persistiu ou invalidou saves antigos.");
        counterExpedition.memoryOverlay = false; counterExpedition.player = new PointF(850, 330); counterExpedition.Interact(); counterExpedition.player = AvenPosition; int refrainGold = counterExpedition.gold; counterExpedition.Interact();
        if (counterExpedition.room != 18 || counterExpedition.questStage != 40 || !counterExpedition.refrainSong || counterExpedition.gold != refrainGold + 650 || counterExpedition.tideGlass != 10) throw new InvalidOperationException("Aven não ensinou o Coro de Retorno nem entregou a recompensa.");
        counterExpedition.room = 29; counterExpedition.SetupRoom(); counterExpedition.enemies.Clear(); counterExpedition.player = new PointF(480, 330); Enemy chorusMob = new() { Pos = new PointF(520, 330), Kind = EnemyKind.Grunt, Hp = 1000, MaxHp = 1000 }; Enemy chorusBoss = new() { Pos = new PointF(550, 330), Kind = EnemyKind.RiftCantor, Hp = 1000, MaxHp = 1000 }; counterExpedition.enemies.AddRange(new[] { chorusMob, chorusBoss }); counterExpedition.refrainCd = 0; float mobHp = chorusMob.Hp, bossHp = chorusBoss.Hp; counterExpedition.CastRefrain();
        if (chorusMob.Hp >= mobHp || chorusMob.Stagger <= 0 || chorusBoss.Hp >= bossHp || chorusBoss.Stagger > 0 || counterExpedition.refrainCd <= 0) throw new InvalidOperationException("O Coro de Retorno não feriu ecos, reduziu dano em chefe ou respeitou sua recarga.");
        using var glassExpedition = new Game(); glassExpedition.timer.Stop(); glassExpedition.StartGame(); glassExpedition.storyOverlay = false; glassExpedition.room = 18; glassExpedition.questStage = 40; glassExpedition.riftVerse = true; glassExpedition.refrainSong = true; glassExpedition.player = AvenPosition; glassExpedition.Interact();
        if (glassExpedition.questStage != 41 || glassExpedition.memoryTitle != "A PEDRA ALÉM DO VERTEDOURO") throw new InvalidOperationException("Aven não abriu a jornada do Vidro Mudo após receber o Coro de Retorno.");
        glassExpedition.player = new PointF(850, 330); glassExpedition.Interact();
        if (glassExpedition.room != 31 || glassExpedition.enemies.Count != 6) throw new InvalidOperationException("A Garganta do Vidro Mudo não abriu com seus encontros.");
        glassExpedition.enemies.Clear(); glassExpedition.drops.Clear(); glassExpedition.player = new PointF(850, 330); glassExpedition.Interact();
        if (glassExpedition.room != 32 || glassExpedition.enemies.Count != 5) throw new InvalidOperationException("A Fonte sob a Pedra não abriu com o Arauto de Vidro.");
        Enemy herald = glassExpedition.enemies.Single(e => e.Kind == EnemyKind.GlassHerald); glassExpedition.player = herald.Pos;
        foreach (int pattern in new[] { 1, 2, 0 }) { herald.Windup = 0; herald.Cooldown = 0; glassExpedition.UpdateEnemy(herald, 0.01f); if (herald.BossPattern != pattern || herald.Windup <= 0) throw new InvalidOperationException("O Arauto de Vidro não alternou os três ataques anunciados."); }
        glassExpedition.Defeat(herald); Drop deepDrop = glassExpedition.drops.Single(d => d.Kind == "deep"); glassExpedition.Collect(deepDrop);
        if (!glassExpedition.deepTrace || glassExpedition.questStage != 42 || glassExpedition.drops.Any(d => d.Kind == "deep")) throw new InvalidOperationException("A lembrança sem nome não atualizou a missão ou não foi coletada.");
        glassExpedition.drops.Clear(); glassExpedition.enemies.Clear(); glassExpedition.MarkRoomCleared(); SaveData glassSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(glassExpedition.CaptureSaveData()))!;
        if (glassSave.Room != 32 || glassSave.QuestStage != 42 || !glassSave.DeepTrace || (glassSave.ClearedRooms & (1L << 32)) == 0 || JsonSerializer.Deserialize<SaveData>("{}")?.DeepTrace != false) throw new InvalidOperationException("O capítulo XIII ou a sala 32 não persistiu no save longo, ou incompatibilizou saves antigos.");
        glassExpedition.player = new PointF(850, 330); glassExpedition.Interact(); glassExpedition.player = AvenPosition; int glassRewardGold = glassExpedition.gold; glassExpedition.Interact();
        if (glassExpedition.room != 18 || glassExpedition.questStage != 43 || glassExpedition.gold != glassRewardGold + 820 || glassExpedition.tideGlass != 5) throw new InvalidOperationException("Aven não registrou o eco sem dono nem entregou a recompensa do capítulo XIII.");
        using var lanternExpedition = new Game(); lanternExpedition.timer.Stop(); lanternExpedition.StartGame(); lanternExpedition.storyOverlay = false; lanternExpedition.room = 18; lanternExpedition.questStage = 43; lanternExpedition.chorusMemory = lanternExpedition.firstMemory = lanternExpedition.westTrace = lanternExpedition.upstreamTrace = lanternExpedition.beyondTrace = lanternExpedition.ebbSeal = lanternExpedition.counterTrace = lanternExpedition.refrainSong = lanternExpedition.riftVerse = lanternExpedition.deepTrace = true; lanternExpedition.townEnding = 1; lanternExpedition.player = AvenPosition; lanternExpedition.Interact();
        if (lanternExpedition.questStage != 44 || lanternExpedition.memoryTitle != "AS LANTERNAS QUE OLHAM PARA DENTRO") throw new InvalidOperationException("Aven não abriu o Capítulo XIV depois de registrar o eco sem dono.");
        lanternExpedition.player = new PointF(850, 330); lanternExpedition.InteractRefuge();
        if (lanternExpedition.room != 33 || lanternExpedition.enemies.Count != 6 || lanternExpedition.roomNames[33] != "PASSAGEM DAS LANTERNAS BAIXAS") throw new InvalidOperationException("A Passagem das Lanternas Baixas não abriu com seus encontros.");
        lanternExpedition.enemies.Clear(); lanternExpedition.drops.Clear(); lanternExpedition.player = new PointF(850, 330); lanternExpedition.Interact();
        if (lanternExpedition.room != 34 || lanternExpedition.enemies.Count != 5 || !lanternExpedition.enemies.Any(e => e.Kind == EnemyKind.LanternRegent)) throw new InvalidOperationException("O Observatório da Maré Interior não abriu com o Regente das Lanternas.");
        Enemy regent = lanternExpedition.enemies.Single(e => e.Kind == EnemyKind.LanternRegent); lanternExpedition.player = regent.Pos;
        foreach (int pattern in new[] { 1, 2, 0 }) { regent.Windup = 0; regent.Cooldown = 0; lanternExpedition.UpdateEnemy(regent, 0.01f); if (regent.BossPattern != pattern || regent.Windup <= 0) throw new InvalidOperationException("O Regente das Lanternas não anunciou os três padrões de ataque."); }
        lanternExpedition.Defeat(regent); Drop signalDrop = lanternExpedition.drops.Single(d => d.Kind == "signal"); Drop lumenKnot = lanternExpedition.drops.Single(d => d.Kind == "charm" && d.Amount == 6); lanternExpedition.Collect(signalDrop); lanternExpedition.Collect(lumenKnot);
        if (!lanternExpedition.lanternSignal || lanternExpedition.questStage != 45 || lanternExpedition.memoryTitle != "A LUZ QUE ESPERA" || (lanternExpedition.ownedCharms & 32) == 0 || lanternExpedition.equippedCharm != 6) throw new InvalidOperationException("O sinal ou o Nó de Lume do Regente não foi recuperado.");
        lanternExpedition.enemies.Clear(); lanternExpedition.drops.Clear(); lanternExpedition.MarkRoomCleared(); SaveData lanternSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(lanternExpedition.CaptureSaveData()))!;
        if (lanternSave.Room != 34 || lanternSave.QuestStage != 45 || !lanternSave.LanternSignal || lanternSave.BeaconLight || lanternSave.OwnedCharms != 32 || lanternSave.EquippedCharm != 6 || (lanternSave.ClearedRooms & (1L << 34)) == 0 || !ValidSaveData(lanternSave) || JsonSerializer.Deserialize<SaveData>("{}")?.LanternSignal != false) throw new InvalidOperationException("A sala 34, o sinal ou o sexto talismã não persistiu ou falhou na validação de save.");
        lanternExpedition.player = new PointF(850, 330); lanternExpedition.Interact();
        if (lanternExpedition.room != 18) throw new InvalidOperationException("O Escutador não conseguiu retornar do Observatório ao Refúgio.");
        lanternExpedition.player = AvenPosition; int lanternRewardGold = lanternExpedition.gold; lanternExpedition.Interact();
        if (lanternExpedition.questStage != 46 || !lanternExpedition.beaconLight || lanternExpedition.gold != lanternRewardGold + 980 || lanternExpedition.tideGlass != 6 || lanternExpedition.potions != 5) throw new InvalidOperationException("Aven não ensinou o Farol de Vigília nem entregou as provisões do Capítulo XIV.");
        lanternExpedition.room = 34; lanternExpedition.SetupRoom(); lanternExpedition.enemies.Clear(); lanternExpedition.player = new PointF(430, 330); lanternExpedition.memoryOverlay = false; lanternExpedition.skillMenuOpen = true; lanternExpedition.OnKeyDown(lanternExpedition, new KeyEventArgs(Keys.D6)); lanternExpedition.skillMenuOpen = false; lanternExpedition.beaconCd = 0;
        Enemy beaconTarget = new() { Pos = new PointF(470, 330), Kind = EnemyKind.Grunt, Hp = 1000, MaxHp = 1000 }; lanternExpedition.enemies.Add(beaconTarget); lanternExpedition.CastEquippedSkill(0); float beaconHp = beaconTarget.Hp; lanternExpedition.TickBeacon(0.2f); float firstPulseHp = beaconTarget.Hp; lanternExpedition.TickBeacon(0.9f);
        SaveData beaconSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(lanternExpedition.CaptureSaveData()))!;
        if (!lanternExpedition.beaconLight || Math.Abs(lanternExpedition.beaconTime - 6.9f) > 0.001f || Math.Abs(lanternExpedition.beaconCd - 25.5f) > 0.001f || beaconTarget.Hp >= firstPulseHp || beaconTarget.Hp >= beaconHp || Math.Abs(lanternExpedition.beaconPulseCd - 0.85f) > 0.001f || !ValidSaveData(beaconSave) || !beaconSave.BeaconLight || !beaconSave.EquippedSkills.SequenceEqual(new[] { 6, 0, 0, 0 }) || beaconSave.OwnedCharms != 32 || beaconSave.EquippedCharm != 6 || !lanternSave.EquippedSkills.SequenceEqual(new[] { 1, 0, 0, 0 })) throw new InvalidOperationException("O Nó de Lume não estendeu a duração do Farol, acelerou os pulsos e preservou o talismã em um save válido.");
        lanternExpedition.enemies.Clear(); lanternExpedition.drops.Clear(); lanternExpedition.room = 18; lanternExpedition.SetupRoom(); lanternExpedition.memoryOverlay = false; lanternExpedition.player = AvenPosition; lanternExpedition.Interact();
        if (lanternExpedition.questStage != 47 || lanternExpedition.memoryTitle != "AS LINHAS QUE NÃO APONTAM") throw new InvalidOperationException("Aven não abriu o Capítulo XV após ensinar o Farol de Vigília.");
        lanternExpedition.player = new PointF(850, 330); lanternExpedition.Interact();
        if (lanternExpedition.room != 35 || lanternExpedition.enemies.Count != 6 || lanternExpedition.roomNames[35] != "TRILHA DAS LINHAS ABERTAS") throw new InvalidOperationException("A Trilha das Linhas Abertas não iniciou o Capítulo XV.");
        lanternExpedition.enemies.Clear(); lanternExpedition.drops.Clear(); lanternExpedition.player = new PointF(850, 330); lanternExpedition.Interact();
        if (lanternExpedition.room != 36 || lanternExpedition.enemies.Count != 5 || !lanternExpedition.enemies.Any(e => e.Kind == EnemyKind.RouteScribe)) throw new InvalidOperationException("A Galeria da Rota Não Escrita não abriu com o Escriba do Traçado.");
        Enemy routeBoss = lanternExpedition.enemies.Single(e => e.Kind == EnemyKind.RouteScribe); if (routeBoss.MaxHp != 2250) throw new InvalidOperationException("A decisão de devolver os nomes não enfraqueceu o Escriba do Traçado.");
        lanternExpedition.invulnerable = 10;
        foreach (int pattern in new[] { 1, 2, 0 }) { routeBoss.Cooldown = 0; lanternExpedition.UpdateEnemy(routeBoss, 0.01f); if (routeBoss.BossPattern != pattern || routeBoss.Windup <= 0) throw new InvalidOperationException("O Escriba do Traçado não anunciou os três ataques do ciclo."); routeBoss.Windup = 0; }
        lanternExpedition.enemies.Clear(); lanternExpedition.enemies.Add(routeBoss); lanternExpedition.drops.Clear(); routeBoss.Hp = 1; routeBoss.Windup = 0; lanternExpedition.Defeat(routeBoss);
        Drop routeDrop = lanternExpedition.drops.Single(d => d.Kind == "route"); lanternExpedition.Collect(routeDrop);
        if (!lanternExpedition.routeMemory || lanternExpedition.questStage != 48 || lanternExpedition.memoryTitle != "AS ROTAS QUE PERMANECEM ABERTAS") throw new InvalidOperationException("A memória das rotas não atualizou o Capítulo XV.");
        lanternExpedition.enemies.Clear(); lanternExpedition.drops.Clear(); lanternExpedition.MarkRoomCleared(); SaveData routeSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(lanternExpedition.CaptureSaveData()))!;
        if (routeSave.Room != 36 || routeSave.QuestStage != 48 || !routeSave.RouteMemory || (routeSave.ClearedRooms & (1L << 36)) == 0 || !ValidSaveData(routeSave) || JsonSerializer.Deserialize<SaveData>("{}")?.RouteMemory != false) throw new InvalidOperationException("O traçado da galeria não persistiu em um save válido nem preservou a compatibilidade antiga.");
        lanternExpedition.player = new PointF(850, 330); lanternExpedition.Interact(); lanternExpedition.memoryOverlay = false; lanternExpedition.player = AvenPosition; int routeRewardGold = lanternExpedition.gold; lanternExpedition.Interact(); SaveData finalRouteSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(lanternExpedition.CaptureSaveData()))!;
        if (lanternExpedition.room != 18 || lanternExpedition.questStage != 49 || lanternExpedition.gold != routeRewardGold + 1300 || !ValidSaveData(finalRouteSave)) throw new InvalidOperationException("Aven não recebeu o traçado nem validou a recompensa final do capítulo.");
        lanternExpedition.memoryOverlay = false; lanternExpedition.player = AvenPosition; lanternExpedition.Interact(); lanternExpedition.memoryOverlay = false;
        if (lanternExpedition.questStage != 50) throw new InvalidOperationException("Aven não abriu o Capítulo XVI após registrar as rotas.");
        lanternExpedition.player = new PointF(850, 330); lanternExpedition.Interact();
        if (lanternExpedition.room != 37 || lanternExpedition.enemies.Count != 6) throw new InvalidOperationException("A Soleira do Silêncio não abriu com seus encontros.");
        lanternExpedition.enemies.Clear(); lanternExpedition.drops.Clear(); lanternExpedition.player = new PointF(850, 330); lanternExpedition.Interact();
        if (lanternExpedition.room != 38 || lanternExpedition.enemies.Count != 1 || lanternExpedition.enemies[0].Kind != EnemyKind.DeepSilence) throw new InvalidOperationException("O Silêncio de Baixo não apareceu na Câmara do Último Eco.");
        Enemy silenceBoss = lanternExpedition.enemies[0]; silenceBoss.Cooldown = 0; lanternExpedition.player = new PointF(150, 330); lanternExpedition.UpdateEnemy(silenceBoss, 0.01f); int firstSilencePattern = silenceBoss.BossPattern;
        silenceBoss.Windup = 0; silenceBoss.Cooldown = 0; lanternExpedition.UpdateEnemy(silenceBoss, 0.01f); int secondSilencePattern = silenceBoss.BossPattern;
        PointF echoLineMidpoint = Scale(Add(silenceBoss.Target2Start, silenceBoss.Target2), 0.5f);
        if (secondSilencePattern != 2 || silenceBoss.Target2 == PointF.Empty || DistanceToSegment(echoLineMidpoint, silenceBoss.Pos, silenceBoss.Target) < 60 || DistanceToSegment(echoLineMidpoint, silenceBoss.Target2Start, silenceBoss.Target2) > 1)
            throw new InvalidOperationException("O terceiro padrão do Silêncio não abriu sua segunda linha paralela, visível e evitável.");
        lanternExpedition.player = echoLineMidpoint; lanternExpedition.health = lanternExpedition.maxHealth = 1000; lanternExpedition.invulnerable = lanternExpedition.dashTime = 0; silenceBoss.Windup = 0.01f; lanternExpedition.UpdateEnemy(silenceBoss, 0.02f);
        if (lanternExpedition.health >= 1000) throw new InvalidOperationException("A segunda linha do Rastro Duplo não atingiu quem permaneceu nela.");
        silenceBoss.Windup = 0; silenceBoss.Cooldown = 0; lanternExpedition.UpdateEnemy(silenceBoss, 0.01f);
        if (firstSilencePattern != 1 || secondSilencePattern != 2 || silenceBoss.BossPattern != 0) throw new InvalidOperationException("Os três padrões anunciados do Silêncio de Baixo não alternaram.");
        lanternExpedition.Defeat(silenceBoss); Drop silenceDrop = lanternExpedition.drops.Single(d => d.Kind == "silence"); lanternExpedition.Collect(silenceDrop); lanternExpedition.drops.Clear();
        SaveData silenceSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(lanternExpedition.CaptureSaveData()))!;
        if (!lanternExpedition.silenceMemory || lanternExpedition.questStage != 51 || !ValidSaveData(silenceSave) || JsonSerializer.Deserialize<SaveData>("{}")?.SilenceMemory != false) throw new InvalidOperationException("A memória final não foi coletada, salva ou migrada com segurança.");
        lanternExpedition.memoryOverlay = false; lanternExpedition.player = new PointF(850, 330); lanternExpedition.Interact(); lanternExpedition.memoryOverlay = false; lanternExpedition.player = AvenPosition; lanternExpedition.Interact(); lanternExpedition.OnKeyDown(lanternExpedition, new KeyEventArgs(Keys.D1));
        SaveData finalEndingSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(lanternExpedition.CaptureSaveData()))!;
        if (lanternExpedition.endingChoiceOpen || lanternExpedition.finalEnding != 1 || lanternExpedition.questStage != 52 || !ValidSaveData(finalEndingSave) || lanternExpedition.maxHealth <= 100) throw new InvalidOperationException("A decisão final não registrou o epílogo de memórias devolvidas nem o bônus correspondente.");
        lanternExpedition.room = 38; lanternExpedition.SetupRoom(); lanternExpedition.enemies.Clear(); lanternExpedition.drops.Clear(); lanternExpedition.player = new PointF(450, 330); lanternExpedition.health = 45; lanternExpedition.EquipSkill(1, 7); Enemy returnedEcho = new() { Pos = new PointF(510, 330), Kind = EnemyKind.Grunt, Hp = 1000, MaxHp = 1000 }; lanternExpedition.enemies.Add(returnedEcho); lanternExpedition.CastEquippedSkill(1);
        SaveData returnedVoicesSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(lanternExpedition.CaptureSaveData()))!;
        if (lanternExpedition.equippedSkills[1] != 7 || returnedEcho.Hp >= 1000 || lanternExpedition.health <= 45 || lanternExpedition.returnVoiceCd <= 0 || !ValidSaveData(returnedVoicesSave)) throw new InvalidOperationException("O epílogo de memórias devolvidas não liberou a técnica de cura em área, nem a salvou.");
        using var emberEnding = new Game(); emberEnding.timer.Stop(); emberEnding.StartGame(); emberEnding.storyOverlay = false; emberEnding.townEnding = 1; emberEnding.finalEnding = 2; emberEnding.questStage = 53; emberEnding.chorusMemory = emberEnding.firstMemory = emberEnding.westTrace = emberEnding.upstreamTrace = emberEnding.beyondTrace = emberEnding.ebbSeal = emberEnding.counterTrace = emberEnding.anchorWard = emberEnding.refrainSong = emberEnding.riftVerse = emberEnding.deepTrace = emberEnding.lanternSignal = emberEnding.beaconLight = emberEnding.routeMemory = emberEnding.silenceMemory = true; emberEnding.room = 38; emberEnding.SetupRoom(); emberEnding.enemies.Clear(); emberEnding.drops.Clear(); emberEnding.EquipSkill(2, 8); Enemy sparkTarget = new() { Pos = new PointF(520, 330), Kind = EnemyKind.DeepSilence, Hp = 1000, MaxHp = 1000 }; emberEnding.enemies.Add(sparkTarget); emberEnding.CastEquippedSkill(2);
        SaveData finalSparkSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(emberEnding.CaptureSaveData()))!;
        if (emberEnding.equippedSkills[2] != 8 || sparkTarget.Hp >= 1000 || emberEnding.finalStrikeCd <= 0 || !ValidSaveData(finalSparkSave)) throw new InvalidOperationException("O epílogo do Lume não liberou o golpe concentrado nem validou seu save.");
        emberEnding.room = 18; emberEnding.vigilBestFloor = 0; emberEnding.SetupRoom(); emberEnding.memoryOverlay = false; emberEnding.player = VigilAltarPosition; emberEnding.Interact(); emberEnding.memoryOverlay = false;
        Enemy firstVigilBoss = emberEnding.enemies.Single(); float firstVigilHp = firstVigilBoss.MaxHp;
        if (!emberEnding.vigilActive || emberEnding.vigilFloor != 1 || firstVigilBoss.Kind != EnemyKind.DeepSilence || firstVigilHp != 3450) throw new InvalidOperationException("O altar não abriu o primeiro andar da Vigília Ascendente após o epílogo.");
        Enemy hardenedDummy = new() { Pos = new PointF(500, 300), Kind = EnemyKind.DeepSilence, Hp = 1000, MaxHp = 1000 }; emberEnding.DamageEnemy(hardenedDummy, 100);
        if (VigilAspectForFloor(1) != VigilAspect.Hardened || hardenedDummy.Hp != 925 || VigilAspectTitle(1) != "CASCA VIVA") throw new InvalidOperationException("A Casca Viva não anunciou nem reduziu em 25% o dano recebido pelo chefe.");
        firstVigilBoss.Hp = 1; emberEnding.Defeat(firstVigilBoss);
        if (!emberEnding.drops.Any(d => d.Kind == "glass") || !emberEnding.drops.Any(d => d.Kind == "weapon")) throw new InvalidOperationException("O chefe da Vigília não deixou sal vítreo e arma.");
        foreach (Drop reward in emberEnding.drops.ToArray()) emberEnding.Collect(reward);
        emberEnding.memoryOverlay = false; emberEnding.player = VigilArenaAltarPosition; emberEnding.Interact(); int vigilGoldBefore = emberEnding.gold, vigilGlassBefore = emberEnding.tideGlass;
        if (!emberEnding.vigilChoiceOpen) throw new InvalidOperationException("O altar da arena não abriu a escolha após limpar o andar.");
        emberEnding.OnKeyDown(emberEnding, new KeyEventArgs(Keys.D1));
        if (emberEnding.vigilFloor != 2 || !emberEnding.vigilActive || emberEnding.enemies.Single().Kind != EnemyKind.RouteScribe || emberEnding.enemies[0].MaxHp <= firstVigilHp || emberEnding.gold <= vigilGoldBefore || emberEnding.tideGlass <= vigilGlassBefore) throw new InvalidOperationException("O segundo andar não elevou a dificuldade, alternou o chefe e concedeu a recompensa do primeiro.");
        emberEnding.health = emberEnding.maxHealth; float hungryHealth = emberEnding.health; emberEnding.HitPlayer(10);
        if (VigilAspectForFloor(2) != VigilAspect.Hungry || Math.Abs(hungryHealth - emberEnding.health - 14.04f) > 0.02f || VigilAspectTitle(2) != "FOME DO RIO") throw new InvalidOperationException("A Fome do Rio não aplicou a pressão de ataque anunciada no segundo andar.");
        emberEnding.health = emberEnding.maxHealth;
        Enemy secondVigilBoss = emberEnding.enemies.Single(); secondVigilBoss.Hp = 1; emberEnding.Defeat(secondVigilBoss);
        foreach (Drop reward in emberEnding.drops.ToArray()) emberEnding.Collect(reward);
        emberEnding.memoryOverlay = false; emberEnding.player = VigilArenaAltarPosition; emberEnding.Interact(); emberEnding.OnKeyDown(emberEnding, new KeyEventArgs(Keys.D2));
        SaveData vigilSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(emberEnding.CaptureSaveData()))!;
        if (emberEnding.room != 18 || emberEnding.vigilActive || emberEnding.vigilBestFloor != 2 || emberEnding.health != emberEnding.maxHealth || !ValidSaveData(vigilSave) || vigilSave.VigilBestFloor != 2 || JsonSerializer.Deserialize<SaveData>("{}")?.VigilBestFloor != 0) throw new InvalidOperationException("A Vigília não encerrou no Refúgio com vida restaurada, melhor andar persistente e compatibilidade com saves antigos.");
        emberEnding.memoryOverlay = false; emberEnding.player = VigilAltarPosition; emberEnding.Interact(); emberEnding.memoryOverlay = false;
        if (!emberEnding.vigilActive || emberEnding.vigilFloor != 3) throw new InvalidOperationException("Uma nova tentativa da Vigília não começou após o último andar concluído.");
        emberEnding.vigilAspectOrder = new[] { VigilAspect.Hardened, VigilAspect.Hungry, VigilAspect.Swift, VigilAspect.Chorus, VigilAspect.Counterflow }; emberEnding.vigilBossOrder = new[] { EnemyKind.DeepSilence, EnemyKind.RouteScribe, EnemyKind.LanternRegent, EnemyKind.GlassHerald, EnemyKind.RiftCantor }; emberEnding.SetupRoom();
        Enemy swiftBoss = emberEnding.enemies.Single(); swiftBoss.Cooldown = 0; emberEnding.UpdateEnemy(swiftBoss, 0.01f);
        if (VigilAspectForFloor(3) != VigilAspect.Swift || Math.Abs(swiftBoss.WindupMax - 0.624f) > 0.002f || VigilAspectTitle(3) != "SUSSURRO VELOZ") throw new InvalidOperationException("O Sussurro Veloz não encurtou o aviso de ataque do terceiro andar.");
        emberEnding.memoryOverlay = false; emberEnding.vigilActive = true; emberEnding.vigilFloor = 4; emberEnding.room = 38; emberEnding.SetupRoom(); emberEnding.vigilMinionCooldown = 0.01f;
        if (VigilAspectForFloor(4) != VigilAspect.Chorus || VigilAspectTitle(4) != "CORO ERRANTE" || !VigilAspectRule(4).Contains("20 segundos")) throw new InvalidOperationException("O Coro Errante não anunciou a regra do quarto andar.");
        emberEnding.TickVigilMinions(0.02f); Enemy errantEcho = emberEnding.enemies.Single(enemy => enemy.VigilMinion);
        if (errantEcho.Kind != EnemyKind.SaltEcho || EnemyName(errantEcho) != "ECO ERRANTE • SALINO" || errantEcho.Hp is < 80 or > 220) throw new InvalidOperationException("O Coro Errante não criou um eco salino identificável e escalado.");
        emberEnding.TickVigilMinions(40); if (emberEnding.enemies.Count(enemy => enemy.VigilMinion) != 1) throw new InvalidOperationException("O Coro Errante acumulou lacaios sem limite durante um mesmo ciclo.");
        emberEnding.drops.Clear(); int minionXp = emberEnding.level * (emberEnding.level - 1) / 2 * 60 + emberEnding.xp; emberEnding.Defeat(errantEcho);
        int minionXpAfter = emberEnding.level * (emberEnding.level - 1) / 2 * 60 + emberEnding.xp;
        if (minionXpAfter != minionXp + 12 || emberEnding.drops.Single(drop => drop.Kind == "gold").Amount != 16 || emberEnding.drops.Any(drop => drop.Kind is "weapon" or "charm" or "glass")) throw new InvalidOperationException("O Eco Errante não concedeu recompensa menor sem duplicar saques raros.");
        emberEnding.drops.Clear(); emberEnding.TickVigilMinions(20); if (emberEnding.enemies.Count(enemy => enemy.VigilMinion) != 1) throw new InvalidOperationException("O Coro Errante não retomou a invocação após o eco ser derrotado.");
        emberEnding.health = 1; emberEnding.HitPlayer(2); SaveData vigilFallSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(emberEnding.CaptureSaveData()))!;
        if (emberEnding.room != 18 || emberEnding.vigilActive || emberEnding.health != emberEnding.maxHealth * 0.5f || emberEnding.vigilBestFloor != 2 || !ValidSaveData(vigilFallSave)) throw new InvalidOperationException("A derrota na Vigília não retornou ao Refúgio com vida parcial preservando a melhor marca.");
        emberEnding.memoryOverlay = false; emberEnding.vigilActive = true; emberEnding.vigilFloor = 5; emberEnding.room = 38; emberEnding.SetupRoom(); Enemy milestoneBoss = emberEnding.enemies.Single(); milestoneBoss.Hp = 1; emberEnding.Defeat(milestoneBoss);
        Drop returnThread = emberEnding.drops.Single(d => d.Kind == "charm" && d.Amount == 7); emberEnding.Collect(returnThread);
        if ((emberEnding.ownedCharms & 64) == 0 || emberEnding.equippedCharm != 7 || emberEnding.charmNames[7] != "FIO DO RETORNO") throw new InvalidOperationException("O quinto andar não concedeu nem equipou o Fio do Retorno.");
        Enemy aspectDummy = new() { Pos = new PointF(500, 300), Kind = EnemyKind.DeepSilence, Hp = 1000, MaxHp = 1000 }; float threadDamage = emberEnding.OutgoingDamageMultiplier(aspectDummy); emberEnding.health = emberEnding.maxHealth; float threadHealth = emberEnding.health; emberEnding.HitPlayer(10);
        if (threadDamage != 1.4f || Math.Abs(threadHealth - emberEnding.health - 14.52f) > 0.03f) throw new InvalidOperationException("O Fio do Retorno não aplicou o bônus de dano e o risco de dano recebido na Vigília.");
        emberEnding.enemies.Clear(); emberEnding.drops.Clear(); emberEnding.vigilFloor = 6; emberEnding.health = 50; Enemy returnBoss = new() { Pos = new PointF(500, 300), Kind = EnemyKind.LanternRegent, Hp = 1, MaxHp = 1000 }; emberEnding.enemies.Add(returnBoss); emberEnding.Defeat(returnBoss);
        if (emberEnding.health != 62) throw new InvalidOperationException("O Fio do Retorno não recuperou 12% da vida ao abater um chefe da Vigília.");
        emberEnding.vigilActive = false; emberEnding.vigilFloor = 0; emberEnding.room = 18; emberEnding.SetupRoom(); emberEnding.drops.Clear(); emberEnding.health = emberEnding.maxHealth; emberEnding.vigilBestFloor = 5;
        SaveData returnThreadSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(emberEnding.CaptureSaveData()))!;
        if (!ValidSaveData(returnThreadSave) || returnThreadSave.OwnedCharms != 64 || returnThreadSave.EquippedCharm != 7 || returnThreadSave.VigilBestFloor != 5) throw new InvalidOperationException("O Fio do Retorno e a melhor marca do quinto andar não foram aceitos pelo save.");
        using var deepVigil = new Game(); deepVigil.timer.Stop(); deepVigil.StartGame(); deepVigil.storyOverlay = false; deepVigil.vigilActive = true; deepVigil.vigilFloor = 15; deepVigil.room = 38; deepVigil.SetupRoom(); Enemy fifteenthFloorBoss = deepVigil.enemies.Single(); fifteenthFloorBoss.Hp = 1; deepVigil.Defeat(fifteenthFloorBoss);
        Drop splitCompass = deepVigil.drops.Single(drop => drop.Kind == "charm" && drop.Amount == 9); deepVigil.Collect(splitCompass);
        if (deepVigil.charmNames[9] != "COMPASSO FENDIDO" || deepVigil.equippedCharm != 9 || Math.Abs(deepVigil.WeaponCooldownMultiplier() - 0.86f) > 0.001f || Math.Abs(deepVigil.CharmDamageTakenMultiplier() - 1.1f) > 0.001f) throw new InvalidOperationException("O 15º andar não concedeu a build ofensiva do Compasso Fendido.");
        deepVigil.room = 0; deepVigil.vigilBestFloor = 0; if (!ValidSaveData(deepVigil.CaptureSaveData())) throw new InvalidOperationException("O Compasso Fendido não foi aceito por um save válido.");
        deepVigil.drops.Clear(); deepVigil.room = 38; deepVigil.vigilFloor = 20; deepVigil.SetupRoom(); Enemy twentiethFloorBoss = deepVigil.enemies.Single(); twentiethFloorBoss.Hp = 1; deepVigil.Defeat(twentiethFloorBoss);
        Drop sharedAnchor = deepVigil.drops.Single(drop => drop.Kind == "charm" && drop.Amount == 10); deepVigil.Collect(sharedAnchor); Enemy buildDummy = new() { Kind = EnemyKind.DeepSilence, Hp = 1000, MaxHp = 1000 };
        if (deepVigil.charmNames[10] != "ÂNCORA COMUM" || deepVigil.equippedCharm2 != 10 || Math.Abs(deepVigil.OutgoingDamageMultiplier(buildDummy) - 0.9f) > 0.001f || Math.Abs(deepVigil.CharmDamageTakenMultiplier() - 0.946f) > 0.001f) throw new InvalidOperationException("O 20º andar não concedeu a build defensiva da Âncora Comum.");
        deepVigil.room = 0; deepVigil.vigilBestFloor = 0; if (!ValidSaveData(deepVigil.CaptureSaveData())) throw new InvalidOperationException("A Âncora Comum não foi aceita por um save válido.");
        deepVigil.drops.Clear(); deepVigil.vigilActive = true; deepVigil.vigilFloor = 25; deepVigil.room = 38; deepVigil.SetupRoom(); Enemy twentyFifthFloorBoss = deepVigil.enemies.Single(); twentyFifthFloorBoss.Hp = 1; deepVigil.Defeat(twentyFifthFloorBoss);
        Drop splitPulse = deepVigil.drops.Single(drop => drop.Kind == "charm" && drop.Amount == 11); deepVigil.Collect(splitPulse); deepVigil.equippedCharm = 11; deepVigil.equippedCharm2 = 0; deepVigil.equippedSkills = new[] { 1, 2, 3, 0 }; deepVigil.shardCd = deepVigil.bellCd = deepVigil.pullCd = 0;
        if (deepVigil.charmNames[11] != "PULSO BIFURCADO" || deepVigil.TechniquesReady() != 3 || Math.Abs(deepVigil.TechniqueDamageMultiplier() - 1.12f) > 0.001f || Math.Abs(deepVigil.BasicAttackDamageMultiplier(buildDummy) - 1f) > 0.001f) throw new InvalidOperationException("O Pulso Bifurcado do 25º andar não fortaleceu técnicas quando estavam prontas.");
        deepVigil.bellCd = 2;
        if (deepVigil.TechniquesReady() != 2 || Math.Abs(deepVigil.TechniqueDamageMultiplier() - 1.12f) > 0.001f || Math.Abs(deepVigil.BasicAttackDamageMultiplier(buildDummy) - 1f) > 0.001f) throw new InvalidOperationException("O Pulso Bifurcado não manteve o bônus técnico no limiar de duas técnicas prontas.");
        deepVigil.pullCd = 2;
        if (deepVigil.TechniquesOnCooldown() != 2 || Math.Abs(deepVigil.BasicAttackDamageMultiplier(buildDummy) - 1.12f) > 0.001f || Math.Abs(deepVigil.TechniqueDamageMultiplier() - 1f) > 0.001f) throw new InvalidOperationException("O Pulso Bifurcado não transferiu o bônus aos ataques básicos durante duas recargas.");
        deepVigil.bellCd = deepVigil.pullCd = 0; deepVigil.room = 0; deepVigil.vigilActive = false; deepVigil.vigilFloor = 0; deepVigil.equippedSkills = new[] { 1, 0, 0, 0 };
        if (!ValidSaveData(deepVigil.CaptureSaveData())) throw new InvalidOperationException("O talismã do 25º andar não foi preservado por um save válido.");
        deepVigil.vigilActive = true; deepVigil.vigilFloor = 30; deepVigil.room = 38; deepVigil.SetupRoom(); Enemy thirtiethFloorBoss = deepVigil.enemies.Single(); thirtiethFloorBoss.Hp = 1; deepVigil.Defeat(thirtiethFloorBoss);
        Drop undertowBell = deepVigil.drops.Single(drop => drop.Kind == "charm" && drop.Amount == 13); deepVigil.Collect(undertowBell); deepVigil.drops.Clear(); deepVigil.enemies.Clear();
        deepVigil.skillCd = deepVigil.shardCd = deepVigil.bellCd = deepVigil.pullCd = 8; Enemy markedForUndertow = new() { Pos = new PointF(510, 330), Kind = EnemyKind.Grunt, Hp = 1, MaxHp = 100, BellMarkTime = 4 }; deepVigil.enemies.Add(markedForUndertow); deepVigil.Defeat(markedForUndertow);
        if (deepVigil.charmNames[13] != "SINO DA CONTRAMARÉ" || deepVigil.equippedCharm2 != 13 || deepVigil.skillCd != 6.5f || deepVigil.shardCd != 6.5f || deepVigil.bellCd != 6.5f || deepVigil.pullCd != 6.5f || !deepVigil.floatingTexts.Any(text => text.Text == "CONTRAMARÉ −1.5 S")) throw new InvalidOperationException("O 30º andar não concedeu o Sino da Contramaré ou o abate marcado não adiantou as recargas.");
        deepVigil.drops.Clear(); Enemy unmarkedForUndertow = new() { Pos = new PointF(510, 330), Kind = EnemyKind.Grunt, Hp = 1, MaxHp = 100 }; deepVigil.enemies.Add(unmarkedForUndertow); deepVigil.Defeat(unmarkedForUndertow);
        if (deepVigil.skillCd != 6.5f || deepVigil.shardCd != 6.5f) throw new InvalidOperationException("O Sino da Contramaré adiantou recargas sem uma marca ativa.");
        deepVigil.drops.Clear(); deepVigil.room = 0; deepVigil.vigilActive = false; deepVigil.vigilFloor = 0; SaveData undertowSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(deepVigil.CaptureSaveData()))!;
        if ((undertowSave.OwnedCharms & (1 << 12)) == 0 || undertowSave.EquippedCharm2 != 13 || !ValidSaveData(undertowSave) || JsonSerializer.Deserialize<SaveData>("{}")?.OwnedCharms != 0) throw new InvalidOperationException("O talismã do 30º andar não foi preservado no save ou o formato antigo deixou de ser compatível.");
        using var openingInstinct = new Game(); openingInstinct.timer.Stop(); openingInstinct.StartGame(); openingInstinct.level = 30; openingInstinct.combatDiscipline = openingInstinct.combatMastery = openingInstinct.combatFocus = openingInstinct.combatApex = openingInstinct.combatTempo = 1; openingInstinct.pendingTalents = 1; openingInstinct.talentChoice = true; openingInstinct.SelectTalent(Keys.D5);
        Enemy openingTarget = new() { Pos = new PointF(500, 300), Kind = EnemyKind.Grunt, Hp = 100, MaxHp = 100 }; openingInstinct.DamageEnemy(openingTarget, 10);
        SaveData openingSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(openingInstinct.CaptureSaveData()))!;
        if (CombatMilestonesAtLevel(30) != 6 || openingInstinct.combatInstinct != 1 || Math.Abs(openingTarget.Hp - 88.2f) > 0.01f || openingInstinct.pendingTalents != 0 || openingSave.CombatInstinct != 1 || JsonSerializer.Deserialize<SaveData>("{}")?.CombatInstinct != 0 || !ValidSaveData(openingSave)) throw new InvalidOperationException("A Primeira Ressonância não foi escolhida, aplicada, salva ou contada na redistribuição do nível 30.");
        using var finishingInstinct = new Game(); finishingInstinct.timer.Stop(); finishingInstinct.StartGame(); finishingInstinct.level = 30; finishingInstinct.combatDiscipline = finishingInstinct.combatMastery = finishingInstinct.combatFocus = finishingInstinct.combatApex = finishingInstinct.combatTempo = 1; finishingInstinct.pendingTalents = 1; finishingInstinct.talentChoice = true; finishingInstinct.SelectTalent(Keys.D6);
        Enemy finishingTarget = new() { Pos = new PointF(500, 300), Kind = EnemyKind.Grunt, Hp = 30, MaxHp = 100 }; finishingInstinct.DamageEnemy(finishingTarget, 10);
        if (finishingInstinct.combatInstinct != 2 || Math.Abs(finishingTarget.Hp - 18.2f) > 0.01f) throw new InvalidOperationException("Último Eco não foi aplicado ao atingir o limiar de vida baixa.");
        openingInstinct.gold = 5000; openingInstinct.tideGlass = 20; openingInstinct.RespecTalents();
        if (openingInstinct.pendingTalents != 6 || !openingInstinct.talentChoice || openingInstinct.combatDiscipline != 0 || openingInstinct.combatMastery != 0 || openingInstinct.combatFocus != 0 || openingInstinct.combatApex != 0 || openingInstinct.combatTempo != 0 || openingInstinct.combatInstinct != 0) throw new InvalidOperationException("A redistribuição não devolveu os seis marcos até o nível 30.");
        game.room = 11; game.SetupRoom();
        if (game.enemies.Count != 0 || game.drops.Count != 0) throw new InvalidOperationException("O Reservatório foi recriado após ter sido limpo.");
        game.room = 3; game.SetupRoom();
        game.memoryOverlay = false; game.player = CryptGatePosition; game.Interact();
        if (game.room != 2 || game.enemies.Count != 4) throw new InvalidOperationException("A passagem da cidade não retornou à cripta.");
        using var borrowedChapter = new Game(); borrowedChapter.timer.Stop(); borrowedChapter.StartGame(); borrowedChapter.storyOverlay = false; borrowedChapter.finalEnding = 1; borrowedChapter.townEnding = 1; borrowedChapter.chorusMemory = borrowedChapter.firstMemory = borrowedChapter.westTrace = borrowedChapter.upstreamTrace = borrowedChapter.beyondTrace = borrowedChapter.ebbSeal = borrowedChapter.counterTrace = borrowedChapter.anchorWard = borrowedChapter.refrainSong = borrowedChapter.riftVerse = borrowedChapter.deepTrace = borrowedChapter.lanternSignal = borrowedChapter.beaconLight = borrowedChapter.routeMemory = borrowedChapter.silenceMemory = true; borrowedChapter.questStage = 52; borrowedChapter.room = 18; borrowedChapter.SetupRoom(); borrowedChapter.memoryOverlay = false; borrowedChapter.player = AvenPosition; borrowedChapter.Interact();
        if (borrowedChapter.questStage != 54) throw new InvalidOperationException("Aven não abriu a expedição opcional XVII após o epílogo.");
        borrowedChapter.memoryOverlay = false; borrowedChapter.player = new PointF(850, 330); borrowedChapter.InteractRefuge();
        if (borrowedChapter.room != 39 || borrowedChapter.enemies.Count != 5) throw new InvalidOperationException("O Cais dos Ecos Emprestados não iniciou com a horda esperada.");
        borrowedChapter.enemies.Clear(); borrowedChapter.drops.Clear(); borrowedChapter.player = new PointF(850, 330); borrowedChapter.Interact();
        if (borrowedChapter.room != 40 || borrowedChapter.enemies.Single().Kind != EnemyKind.BorrowedChorus) throw new InvalidOperationException("O Arquivo da Maré sem Nome não abriu seu chefe original.");
        Enemy borrowedBoss = borrowedChapter.enemies.Single(); borrowedBoss.Cooldown = 0; borrowedChapter.player = new PointF(150, 330); borrowedChapter.UpdateEnemy(borrowedBoss, 0.01f);
        if (borrowedBoss.BossPattern != 1 || borrowedBoss.Windup <= 0) throw new InvalidOperationException("O Arquivista das Marés não anunciou o primeiro golpe.");
        borrowedBoss.Windup = 0; borrowedBoss.Cooldown = 0; borrowedChapter.UpdateEnemy(borrowedBoss, 0.01f);
        if (borrowedBoss.BossPattern != 2 || !BossUsesLine(borrowedBoss)) throw new InvalidOperationException("O Arquivista das Marés não anunciou o corte em linha.");
        borrowedBoss.Windup = 0; borrowedBoss.Cooldown = 0; borrowedChapter.UpdateEnemy(borrowedBoss, 0.01f);
        if (borrowedBoss.BossPattern != 0) throw new InvalidOperationException("O Arquivista das Marés não concluiu seu ciclo de três ataques.");
        borrowedBoss.Hp = 1; borrowedChapter.Defeat(borrowedBoss);
        if (borrowedChapter.drops.Count(drop => drop.Kind == "weapon" && drop.Amount == 5 && drop.Rarity == 2) != 1) throw new InvalidOperationException("O Arquivista das Marés não garantiu a Corrente do Vau como Relíquia.");
        borrowedChapter.Collect(borrowedChapter.drops.Single(drop => drop.Kind == "weapon" && drop.Amount == 5));
        if ((borrowedChapter.ownedWeapons & 32) == 0 || borrowedChapter.WeaponTraitDescription(5) != "CORRENTE DE ALCANCE CURTO • ARCO AMPLO • PUXA ECOS COMUNS PARA PERTO" || !ValidSaveData(borrowedChapter.CaptureSaveData())) throw new InvalidOperationException("A Corrente do Vau não foi recolhida nem salva antes da memória do chefe.");
        borrowedChapter.EquipWeapon(5);
        SaveData chainSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(borrowedChapter.CaptureSaveData()))!;
        if (borrowedChapter.weapon != 5 || chainSave.Weapon != 5 || !ValidSaveData(chainSave)) throw new InvalidOperationException("A Corrente do Vau não pôde ser equipada e persistida.");
        borrowedChapter.Collect(borrowedChapter.drops.Single(drop => drop.Kind == "borrowed"));
        SaveData borrowedSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(borrowedChapter.CaptureSaveData()))!;
        if (!borrowedChapter.borrowedTrace || borrowedChapter.questStage != 55 || !ValidSaveData(borrowedSave) || JsonSerializer.Deserialize<SaveData>("{}")?.BorrowedTrace != false) throw new InvalidOperationException("A memória da expedição não persistiu nem preservou saves antigos.");
        borrowedChapter.memoryOverlay = false; borrowedChapter.room = 18; borrowedChapter.SetupRoom(); borrowedChapter.player = AvenPosition; int borrowedGold = borrowedChapter.gold, borrowedTalent = borrowedChapter.techniqueTalent; borrowedChapter.Interact();
        if (borrowedChapter.questStage != 56 || borrowedChapter.gold != borrowedGold + 1900 || borrowedChapter.techniqueTalent != borrowedTalent + 1 || !ValidSaveData(borrowedChapter.CaptureSaveData())) throw new InvalidOperationException("Aven não registrou a memória nem concedeu a recompensa permanente da expedição.");
        borrowedChapter.memoryOverlay = false; borrowedChapter.Interact();
        if (!borrowedChapter.forgeMenuOpen) throw new InvalidOperationException("Aven não reabriu a têmpera depois de concluída a expedição.");
        borrowedChapter.forgeMenuOpen = false; borrowedChapter.player = new PointF(850, 330); borrowedChapter.InteractRefuge();
        if (borrowedChapter.room != 41 || borrowedChapter.questStage != 57 || borrowedChapter.enemies.Count != 6 || !ValidSaveData(borrowedChapter.CaptureSaveData()) || (borrowedChapter.KnownRoomMask() & (1L << 41)) == 0) throw new InvalidOperationException("A Câmara das Badaladas não abriu como expedição opcional ou não validou o progresso salvo.");
        borrowedChapter.enemies.Clear(); borrowedChapter.drops.Clear(); borrowedChapter.player = new PointF(850, 330); borrowedChapter.Interact();
        if (borrowedChapter.room != 42 || borrowedChapter.enemies.Single().Kind != EnemyKind.LastTuner) throw new InvalidOperationException("O Poço do Contratempo não abriu com o Afinador original.");
        Enemy lastTuner = borrowedChapter.enemies.Single(); lastTuner.Hp = 1; borrowedChapter.Defeat(lastTuner);
        if (borrowedChapter.drops.Count(drop => drop.Kind == "weapon" && drop.Amount == 3 && drop.Rarity == 2) != 1) throw new InvalidOperationException("O Afinador do Contratempo não garantiu a Relíquia original de pós-jogo.");
        if (borrowedChapter.drops.Count(drop => drop.Kind == "weapon" && drop.Amount == 4 && drop.Rarity == 2) != 1) throw new InvalidOperationException("O Afinador do Contratempo não garantiu o Arco do Limiar como Relíquia.");
        if (borrowedChapter.drops.Count(drop => drop.Kind == "charm" && drop.Amount == 12) != 1) throw new InvalidOperationException("O Afinador do Contratempo não deixou o talismã original do pós-jogo.");
        Drop unsentMemory = borrowedChapter.drops.Single(drop => drop.Kind == "unstruck"); borrowedChapter.Collect(unsentMemory); borrowedChapter.memoryOverlay = false;
        foreach (Drop remainingDrop in borrowedChapter.drops.ToArray()) borrowedChapter.Collect(remainingDrop);
        SaveData unstruckSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(borrowedChapter.CaptureSaveData()))!;
        if (!borrowedChapter.unstruckTrace || borrowedChapter.questStage != 58 || (borrowedChapter.ownedWeapons & 8) == 0 || (borrowedChapter.ownedWeapons & 16) == 0 || (borrowedChapter.ownedCharms & (1 << 11)) == 0 || !ValidSaveData(unstruckSave) || JsonSerializer.Deserialize<SaveData>("{}")?.UnstruckTrace != false) throw new InvalidOperationException("A memória, a Lança da Badalada, o Arco do Limiar, o Nó do Contratempo ou o save do capítulo final não persistiram corretamente.");
        borrowedChapter.EquipWeapon(3);
        if (borrowedChapter.weapon != 3 || borrowedChapter.WeaponTraitDescription(3) != "LANÇA PRECISA • ALCANCE MÁXIMO • PERFURA ARMADURA • ARCO ESTREITO" || !ValidWeaponData(borrowedChapter.CaptureSaveData())) throw new InvalidOperationException("A arma de pós-jogo não pôde ser equipada ou salva após o desbloqueio.");
        borrowedChapter.EquipWeapon(4);
        SaveData bowSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(borrowedChapter.CaptureSaveData()))!;
        if (borrowedChapter.weapon != 4 || borrowedChapter.WeaponTraitDescription(4) != "ARCO DE LONGO ALCANCE • UM ALVO POR DISPARO • DANO REDUZIDO CONTRA ARMADURA" || !ValidWeaponData(bowSave) || bowSave.Weapon != 4 || (bowSave.OwnedWeapons & 16) == 0) throw new InvalidOperationException("O Arco do Limiar não pôde ser equipado e persistido no save.");
        SaveData prematureBowSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(bowSave))!; prematureBowSave.UnstruckTrace = false; prematureBowSave.QuestStage = 0; prematureBowSave.OwnedWeapons = 17; prematureBowSave.WeaponBonuses[3] = prematureBowSave.WeaponRarities[3] = prematureBowSave.WeaponAffixes[3] = prematureBowSave.WeaponAffixes2[3] = 0;
        if (ValidSaveData(prematureBowSave)) throw new InvalidOperationException("Um save aceitou o Arco do Limiar antes da expedição que o apresenta.");
        using (var intervalCharmCheck = new Game())
        {
            intervalCharmCheck.ownedCharms = 1 << 11; intervalCharmCheck.equippedCharm2 = 12;
            if (Math.Abs(intervalCharmCheck.TechniqueDamageMultiplier() - 1.2f) > 0.001f || Math.Abs(intervalCharmCheck.WeaponCooldownMultiplier() - 1.15f) > 0.001f || !ValidSaveData(intervalCharmCheck.CaptureSaveData())) throw new InvalidOperationException("O Nó do Contratempo não trocou recarga por dano técnico ou não passou pela validação de save.");
        }
        borrowedChapter.player = new PointF(850, 330); borrowedChapter.Interact(); borrowedChapter.memoryOverlay = false; borrowedChapter.player = AvenPosition;
        int chimeGold = borrowedChapter.gold, chimeGlass = borrowedChapter.tideGlass, chimePotions = borrowedChapter.potions, chimeTalent = borrowedChapter.techniqueTalent;
        borrowedChapter.Interact(); SaveData completedChimeSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(borrowedChapter.CaptureSaveData()))!;
        if (borrowedChapter.room != 18 || borrowedChapter.questStage != 59 || borrowedChapter.gold != chimeGold + 2200 || borrowedChapter.tideGlass != chimeGlass + 10 || borrowedChapter.potions != chimePotions + 3 || borrowedChapter.techniqueTalent != chimeTalent + 1 || !ValidSaveData(completedChimeSave)) throw new InvalidOperationException("Aven não concluiu a expedição XVIII com recompensa e save válidos.");
        borrowedChapter.memoryOverlay = false; borrowedChapter.Interact();
        if (!borrowedChapter.forgeMenuOpen) throw new InvalidOperationException("A forja pós-jogo não permaneceu acessível após a expedição XVIII.");
        borrowedChapter.equippedCharm = 0; borrowedChapter.equippedCharm2 = 0; borrowedChapter.level = 10; borrowedChapter.healthTalent = 2; borrowedChapter.skillTalent = 1; borrowedChapter.dodgeTalent = 1; borrowedChapter.techniqueTalent = 2; borrowedChapter.combatDiscipline = 1; borrowedChapter.combatMastery = 2;
        borrowedChapter.maxHealth = 180; borrowedChapter.health = 90; borrowedChapter.gold = 5000; borrowedChapter.tideGlass = 10; borrowedChapter.pendingTalents = 0; borrowedChapter.talentChoice = false;
        borrowedChapter.OnKeyDown(borrowedChapter, new KeyEventArgs(Keys.D7));
        if (borrowedChapter.forgeMenuOpen || borrowedChapter.gold != 3750 || borrowedChapter.tideGlass != 3 || borrowedChapter.healthTalent + borrowedChapter.skillTalent + borrowedChapter.dodgeTalent + borrowedChapter.techniqueTalent != 0 || borrowedChapter.combatDiscipline != 0 || borrowedChapter.combatMastery != 0 || borrowedChapter.pendingTalents != 8 || !borrowedChapter.talentChoice || borrowedChapter.maxHealth != 140 || borrowedChapter.health != 70)
            throw new InvalidOperationException("A redistribuição não devolveu os talentos, preservou os bônus permanentes de vida ou cobrou os recursos corretos.");
        borrowedChapter.SelectTalent(Keys.D6);
        borrowedChapter.SelectTalent(Keys.D6);
        if (borrowedChapter.combatDiscipline != 2 || borrowedChapter.combatMastery != 2 || borrowedChapter.pendingTalents != 6 || Math.Abs(borrowedChapter.TechniqueDamageMultiplier() - 1.12f) > 0.001f || !ValidSaveData(borrowedChapter.CaptureSaveData())) throw new InvalidOperationException("A redistribuição não permitiu escolher a Forma do Eco e a maestria agressiva nem salvou a build.");
        using var eliteCircuit = new Game(); eliteCircuit.timer.Stop(); eliteCircuit.StartGame(); eliteCircuit.storyOverlay = false;
        foreach (int eliteRoom in new[] { 23, 25, 27, 31, 33, 35, 37, 39 })
        {
            eliteCircuit.room = eliteRoom; eliteCircuit.SetupRoom(); Enemy[] roomElites = eliteCircuit.enemies.Where(enemy => enemy.Kind == EnemyKind.Elite).ToArray();
            EliteAffix? secondary = roomElites.Length == 1 ? roomElites[0].EliteAffix2 : null;
            bool validSecondary = !secondary.HasValue;
            if (secondary.HasValue) validSecondary = Enum.IsDefined(secondary.Value) && secondary.Value != roomElites[0].EliteAffix && (secondary.Value is EliteAffix.Armored or EliteAffix.Swift or EliteAffix.Bloodbound) && !(roomElites[0].EliteAffix == EliteAffix.Spearbound && secondary.Value == EliteAffix.Swift);
            if (roomElites.Length != 1 || roomElites[0].MaxHp != 120 + (eliteRoom - 1) * 6 || !Enum.IsDefined(roomElites[0].EliteAffix) || secondary.HasValue != (eliteRoom >= 25) || !validSecondary || roomElites[0].Armored != HasEliteAffix(roomElites[0], EliteAffix.Armored)) throw new InvalidOperationException($"A rota avançada {eliteRoom} não recebeu as combinações de afixos e a vida escalada esperadas.");
        }
        using (var field = new Game())
        {
            field.timer.Stop(); field.StartGame(); field.storyOverlay = false; field.room = 3; field.SetupRoom();
            field.player = new PointF(860, 330); field.Interact(); field.memoryOverlay = false;
            if (field.room != 43 || field.enemies.Count == 0) throw new InvalidOperationException("A saída de Véspera não abriu as Margens com encontros.");
            field.player = FirstEchoIvara; field.Interact(); field.memoryOverlay = false;
            if (field.firstEchoQuest != 1) throw new InvalidOperationException("Ivara não iniciou a investigação.");
            int goldBeforeCache = field.gold;
            field.player = FirstEchoReedCache; field.Interact(); field.memoryOverlay = false; field.Interact(); field.memoryOverlay = false;
            if (!field.firstEchoReedCache || field.gold != goldBeforeCache + 35) throw new InvalidOperationException("A caixa dos juncais não concedeu sua recompensa uma única vez.");
            int outdoorCount = field.enemies.Count;
            field.player = FirstEchoCave; field.Interact(); field.memoryOverlay = false;
            if (field.room != 44) throw new InvalidOperationException("A gruta não abriu.");
            field.enemies.Clear(); field.player = new PointF(760, 330); field.Interact(); field.memoryOverlay = false;
            if (!field.firstEchoCaveFound) throw new InvalidOperationException("O registro da gruta não foi reconhecido.");
            field.player = new PointF(150, 330); field.Interact();
            if (field.room != 43 || field.enemies.Count != outdoorCount) throw new InvalidOperationException("O retorno da gruta não preservou os encontros das Margens.");
            field.player = FirstEchoSluice; field.Interact(); field.memoryOverlay = false; field.enemies.Clear();
            field.player = new PointF(760, 330); field.Interact(); field.memoryOverlay = false;
            if (field.firstEchoQuest != 2) throw new InvalidOperationException("As comportas não libertaram as vozes.");
            field.player = new PointF(150, 330); field.Interact(); field.player = FirstEchoTownGate; field.Interact(); field.memoryOverlay = false;
            field.player = BellKeeperPosition; field.Interact();
            SaveData expeditionSave = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(field.CaptureSaveData()))!;
            if (field.firstEchoQuest != 3 || !expeditionSave.FirstEchoCaveFound || !expeditionSave.FirstEchoReedCache || expeditionSave.FirstEchoDiscovered != 7 || !ValidSaveData(expeditionSave)) throw new InvalidOperationException("A expedição não concluiu ou não preservou suas descobertas no save.");
        }
        using (var navigation = new Game())
        {
            navigation.timer.Stop(); navigation.StartGame(); navigation.room = 43; navigation.SetupRoom();
            navigation.player = new PointF(1500, 900);
            PointF camera = navigation.CameraOffset();
            if (camera != new PointF(1020, 580) || navigation.Blocked(new PointF(1740, 700)) || navigation.Blocked(new PointF(1740, 1050)) || !navigation.Blocked(new PointF(1740, 850))) throw new InvalidOperationException("A câmera ou as travessias das Margens não correspondem ao terreno.");
            navigation.player = new PointF(1500, 1050);
            if (Math.Abs(navigation.FirstEchoTerrainSpeed() - 0.8f) > 0.001f) throw new InvalidOperationException("A margem alagada não reduziu a velocidade de caminhada.");
            navigation.player = new PointF(1740, 1050);
            if (navigation.FirstEchoTerrainSpeed() != 1f) throw new InvalidOperationException("A ponte não manteve a caminhada livre.");
            navigation.room = 44; navigation.SetupRoom(); navigation.player = new PointF(450, 200);
            Enemy pursuer = new() { Pos = new PointF(200, 200), Kind = EnemyKind.Grunt };
            PointF waypoint = navigation.InteriorDetour(pursuer);
            if (!navigation.BlockedLine(pursuer.Pos, navigation.player) || waypoint == pursuer.Pos || navigation.BlockedEnemyPath(pursuer.Pos, waypoint, pursuer.Radius)) throw new InvalidOperationException("O inimigo não encontrou um primeiro trecho livre ao redor da parede da gruta.");
        }
        Console.WriteLine("TESTES OK: campanha e expedições, incluindo as Margens do Primeiro Eco, combate, loot, progressão e compatibilidade de saves antigos."); return 0;
    }
}
