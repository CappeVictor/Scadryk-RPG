using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

public sealed partial class Game
{
    private void ReforgeWeapon(int affix)
    {
        if (!forgeMenuOpen || room != 18 || questStage < 27 || Distance(player, AvenPosition) >= 60) { forgeMenuOpen = secondaryForgeOpen = false; return; }
        if (!secondaryForgeOpen && affix == 7) { RespecTalents(); return; }
        if (affix == 0) affix = 7;
        if (affix < 1 || affix > EquipmentCatalog.WeaponAffixes.Count) return;
        if (EquipmentCatalog.WeaponAffix(affix).RelicOnly && (!secondaryForgeOpen || weaponRarities[weapon] != 2)) { notice = "NOME RISCADO É UM PODER ÚNICO DO AFIXO SECUNDÁRIO DE RELÍQUIAS"; noticeTime = 2.5f; return; }
        if (secondaryForgeOpen && weaponRarities[weapon] != 2) { notice = "O AFIXO SECUNDÁRIO SÓ PODE SER MUDADO EM UMA RELÍQUIA"; noticeTime = 2.5f; return; }
        int goldCost = secondaryForgeOpen ? 150 : 75, glassCost = secondaryForgeOpen ? 2 : 1;
        if (gold < goldCost || tideGlass < glassCost) { notice = secondaryForgeOpen ? "AFIXO SECUNDÁRIO CUSTA 150 OURO E 2 SAL VÍTREO" : "A TÊMPERA CUSTA 75 OURO E 1 SAL VÍTREO"; noticeTime = 2; return; }
        int currentAffix = secondaryForgeOpen ? weaponAffixes2[weapon] : weaponAffixes[weapon];
        if (currentAffix == affix || secondaryForgeOpen && weaponAffixes[weapon] == affix) { notice = secondaryForgeOpen ? "AFIXOS PRINCIPAL E SECUNDÁRIO PRECISAM SER DIFERENTES" : "ESSA ARMA JÁ GUARDA ESSE AFFIX"; noticeTime = 2; return; }
        int oldPower = WeaponPower(weapon);
        if (secondaryForgeOpen) weaponAffixes2[weapon] = affix;
        else
        {
            if (weaponRarities[weapon] == 0) { weaponRarities[weapon] = 1; weaponBonuses[weapon] = random.Next(3, 7); }
            if (weaponRarities[weapon] == 2 && weaponAffixes2[weapon] == affix) (weaponAffixes[weapon], weaponAffixes2[weapon]) = (weaponAffixes2[weapon], weaponAffixes[weapon]);
            else weaponAffixes[weapon] = affix;
        }
        damage += WeaponPower(weapon) - oldPower;
        gold -= goldCost; tideGlass -= glassCost; forgeMenuOpen = false; secondaryForgeOpen = false;
        notice = $"TÊMPERA: {EquipmentCatalog.Weapon(weapon).Name} {WeaponRarityName(weaponRarities[weapon])} {WeaponAffixDescription(weapon)}"; noticeTime = 3;
    }

    private void HandleDefenseForgeKey(Keys key)
    {
        if (!forgeMenuOpen || room != 18 || questStage < 27 || Distance(player, AvenPosition) >= 60) { forgeMenuOpen = defenseForgeOpen = false; return; }
        if (key is Keys.Escape or Keys.T) { defenseForgeOpen = false; return; }
        if (key is >= Keys.D1 and <= Keys.D3) SetDefenseTrait(true, (int)key - (int)Keys.D0);
        else if (key is >= Keys.D4 and <= Keys.D7) SetDefenseTrait(false, (int)key - (int)Keys.D3);
        else if (key == Keys.D0) SetDefenseTrait(true, 0);
        else if (key == Keys.D8) SetDefenseTrait(false, 0);
        else if (key == Keys.D9) SetDefenseTrait(true, 4);
        else if (key == Keys.F6) SetDefenseTrait(true, 5);
        else if (key == Keys.F7) SetDefenseTrait(false, 5);
    }

    private void SetDefenseTrait(bool forArmor, int trait)
    {
        int current = forArmor ? armorTrait : relicTrait;
        if ((forArmor && !armor) || (!forArmor && !relic)) { notice = forArmor ? "O PEITORAL AINDA NÃO FOI ENCONTRADO" : "O RELICÁRIO AINDA NÃO FOI ENCONTRADO"; noticeTime = 2; return; }
        if (trait == current) { notice = "ESSE TRAÇO JÁ ESTÁ GRAVADO"; noticeTime = 1.8f; return; }
        if (gold < 100 || tideGlass < 2) { notice = "GRAVAR UM TRAÇO CUSTA 100 OURO E 2 SAL VÍTREO"; noticeTime = 2.2f; return; }
        int previousGold = gold, previousGlass = tideGlass;
        gold -= 100; tideGlass -= 2;
        if (forArmor) armorTrait = trait; else relicTrait = trait;
        if (!SaveGame())
        {
            gold = previousGold; tideGlass = previousGlass;
            if (forArmor) armorTrait = current; else relicTrait = current;
            notice = "O TRAÇO NÃO MUDOU • NÃO FOI POSSÍVEL SALVAR"; noticeTime = 3; return;
        }
        notice = $"TRAÇO GRAVADO: {(forArmor ? ArmorTraitName(trait) : RelicTraitName(trait))}"; noticeTime = 2.5f;
    }

    private static string ArmorTraitName(int trait) => trait switch { 1 => "Laje Funda", 2 => "Passo Ancorado", 3 => "Último Lastro", 4 => "Riposta do Veio", 5 => "Gume de Vigília", _ => "Veio Bruto" };
    private static string ArmorTraitDescription(int trait) => trait switch { 1 => "Peitoral equipado: recebe 6% menos dano adicional.", 2 => "Peitoral equipado: esquiva recarrega 0,12 s mais rápido; com Ampola Viva, poções adiantam recargas.", 3 => "Com 35% da vida ou menos: recebe 15% menos dano; com Fio Nutrido, abates curam +7.", 4 => "Após esquivar: o próximo ataque básico causa +18% por 1,35 s; com Coro Restaurador, janela de 1,9 s.", 5 => "Inimigos marcados recebem +12% dano; com Eco de Retorno, +20% total.", _ => "Sem efeito adicional." };
    private static string RelicTraitName(int trait) => trait switch { 1 => "Fio Nutrido", 2 => "Ampola Viva", 3 => "Coro Restaurador", 4 => "Selo da Convergência", 5 => "Eco de Retorno", _ => "Eco Silente" };
    private static string RelicTraitDescription(int trait) => trait switch { 1 => "Relicário equipado: cura +3 por abate; com Último Lastro, abates curam +7 em vida crítica.", 2 => "Relicário equipado: poções curam +15 vida; com Passo Ancorado, também adiantam recargas.", 3 => "Com o conjunto: Pulso do Veio dura +1 s; com Riposta do Veio, janela pós-esquiva de 1,9 s.", 4 => "Técnicas +10%; recebe +6% dano (Laje Funda pode anular com ambas as peças equipadas).", 5 => "Abates de inimigos marcados adiantam recargas em 0,75 s; com Gume de Vigília, 1,25 s.", _ => "Sem efeito adicional." };

    private static float DifficultyEnemyHealthMultiplier(int tier) => tier switch { 1 => 1.2f, 2 => 1.45f, 3 => 1.8f, _ => 1f };
    private static float DifficultyIncomingDamageMultiplier(int tier) => tier switch { 1 => 1.1f, 2 => 1.22f, 3 => 1.4f, _ => 1f };
    private static float DifficultyRewardMultiplier(int tier) => tier switch { 1 => 1.15f, 2 => 1.3f, 3 => 1.5f, _ => 1f };
    private static int DifficultyEliteWeaponBonus(int tier) => Math.Clamp(tier, 0, 3) * 5;
    private static int DifficultyBossRarityBonus(int tier) => Math.Clamp(tier, 0, 3) * 5;

    private void CycleDifficultyTier()
    {
        if (!forgeMenuOpen || room != 18 || finalEnding == 0 || questStage < 56 || Distance(player, AvenPosition) >= 60) { notice = "RESSONÂNCIAS DESBLOQUEIAM APÓS O EPÍLOGO E A EXPEDIÇÃO XVII"; noticeTime = 2.5f; return; }
        int previousTier = difficultyTier;
        difficultyTier = (difficultyTier + 1) % 4;
        if (!SaveGame()) { difficultyTier = previousTier; notice = "A RESSONÂNCIA NÃO MUDOU • NÃO FOI POSSÍVEL SALVAR"; noticeTime = 3; return; }
        notice = $"RESSONÂNCIA {difficultyTier}: {DifficultyTierName(difficultyTier)} • VALE PARA AS PRÓXIMAS SALAS"; noticeTime = 3;
    }
    private static string DifficultyTierName(int tier) => tier switch { 1 => "DISSONANTE", 2 => "CONTRAVOZ", 3 => "CONTRATEMPO", _ => "SERENO" };

    private void SalvageNearbyWeapon()
    {
        Drop? item = drops.Where(drop => drop.Kind == "weapon" && Distance(player, drop.Pos) <= 75).OrderBy(drop => Distance(player, drop.Pos)).FirstOrDefault();
        if (item is null) { notice = "NENHUMA ARMA POR PERTO PARA DESMONTAR"; noticeTime = 1.5f; return; }
        (int goldValue, int glassValue) = WeaponSalvageReward(item.Rarity);
        gold += goldValue;
        int receivedGlass = Math.Min(glassValue, 999 - tideGlass);
        tideGlass += receivedGlass;
        drops.Remove(item);
        notice = receivedGlass > 0 ? $"ARMA DESMONTADA: +{goldValue} OURO • +{receivedGlass} SAL" : $"ARMA DESMONTADA: +{goldValue} OURO";
        noticeTime = 2.2f;
    }

    private static (int Gold, int Glass) WeaponSalvageReward(int rarity) => rarity switch { 2 => (75, 2), 1 => (40, 1), _ => (15, 0) };

    private void Collect(Drop item)
    {
        if (item.Kind == "charm")
        {
            if (item.Amount is < 1 or > 14) { drops.Remove(item); return; }
            bool newCharm = (ownedCharms & (1 << (item.Amount - 1))) == 0; ownedCharms |= 1 << (item.Amount - 1);
            if (equippedCharm == 0 && equippedCharm2 != item.Amount && equippedCharm3 != item.Amount) equippedCharm = item.Amount;
            else if (equippedCharm2 == 0 && equippedCharm != item.Amount && equippedCharm3 != item.Amount) equippedCharm2 = item.Amount;
            else if (vigilBestFloor >= 30 && equippedCharm3 == 0 && equippedCharm != item.Amount && equippedCharm2 != item.Amount) equippedCharm3 = item.Amount;
            notice = item.Amount >= 7 ? $"TALISMÃ DE PÓS-JOGO: {charmNames[item.Amount]}" : newCharm ? $"TALISMÃ: {charmNames[item.Amount]}" : $"ECO DUPLICADO: {charmNames[item.Amount]}";
            drops.Remove(item); noticeTime = 1.7f; return;
        }
        if (item.Kind == "silence") { silenceMemory = true; questStage = 51; notice = "MEMÓRIA: AS VOZES DEVOLVIDAS"; ShowMemory("AS VOZES DEVOLVIDAS", "O Silêncio não era uma criatura que veio de fora. Era o nome dado ao peso de milhares de lembranças mantidas juntas pelo Lume. Quando a câmara se abre, as vozes não exigem que você as devolva nem que continue prendendo-as: cada uma pode escolher se quer falar, descansar ou partir. Entre elas não há resposta sobre Nara. O que resta é decidir como Véspera cuidará do Lume daqui em diante."); drops.Remove(item); return; }
        if (item.Kind == "borrowed") { borrowedTrace = true; questStage = 55; notice = "MEMÓRIA: UM ECO DEIXADO POR ESCOLHA"; ShowMemory("O ARQUIVO PERMANECE ABERTO", "O arquivista guardava os ecos para impedir que se perdessem. Quando cede, cada som volta a pertencer a quem o deixou. A gravação não traz nomes, vozes identificáveis ou destinos: apenas a decisão de compartilhar um instante."); drops.Remove(item); return; }
        if (item.Kind == "unstruck") { unstruckTrace = true; questStage = 58; notice = "MEMÓRIA: A BADALADA QUE NUNCA SOOU"; ShowMemory("O CONTRATEMPO ESCOLHIDO", "O mecanismo não guardava o silêncio de uma pessoa. Ele reunia pausas deixadas por muita gente: intervalos em que alguém decidiu não tocar o sino, não responder ou não registrar uma passagem. O Afinador tentava preencher cada espaço com uma nota obrigatória. Quando o eco se desfaz, as pausas voltam a pertencer a quem as escolheu. Nenhum som aponta uma rota ou fala por Nara."); drops.Remove(item); return; }
        if (item.Kind == "deep") { deepTrace = true; questStage = 42; notice = "MEMÓRIA: A PEDRA QUE NÃO GUARDA NOMES"; ShowMemory("A PEDRA QUE NÃO GUARDA NOMES", "A fonte não ecoa nenhuma voz antiga. Ela repete o som de passos que chegaram até aqui e escolheram partir sem deixar registro. O Guardião tentava reunir os sons numa única história; a pedra se abre quando cada passo volta a ter seu próprio ritmo. A lembrança não identifica quem passou nem para onde foi."); drops.Remove(item); return; }
        if (item.Kind == "signal") { lanternSignal = true; questStage = 45; notice = "SINAL: A LUZ QUE ESPERA"; ShowMemory("A LUZ QUE ESPERA", "O Regente mantinha as lanternas voltadas para dentro, prendendo cada viajante ao reflexo de quem já foi. Quando a luz se abre, elas não revelam rostos nem caminhos: apenas deixam uma chama acesa para quem decidir voltar. A fonte guarda a promessa, não a identidade nem o destino de quem passou."); drops.Remove(item); return; }
        if (item.Kind == "route") { routeMemory = true; questStage = 48; notice = "MEMÓRIA: AS ROTAS QUE PERMANECEM ABERTAS"; ShowMemory("AS ROTAS QUE PERMANECEM ABERTAS", "O Escriba traçava cada caminho de volta até uma única saída. Sob as linhas apagadas, a pedra guarda marcas de pessoas que escolheram retornar, seguir adiante ou permanecer onde estavam. Os traços pertencem a muitos viajantes; nenhum carrega o nome de Nara nem aponta para o lugar onde ela possa estar."); drops.Remove(item); return; }
        switch (item.Kind) { case "gold": gold += item.Amount; notice = $"+{item.Amount} OURO"; break; case "potion": potions++; notice = "+1 POÇÃO"; break; case "glass": tideGlass = Math.Min(999, tideGlass + item.Amount); notice = $"+{item.Amount} SAL VÍTREO"; break; case "weapon": if (item.Amount is < 1 or > 5) { drops.Remove(item); return; } int oldPower = WeaponPower(item.Amount); weaponRarities[item.Amount] = item.Rarity; weaponBonuses[item.Amount] = item.Bonus; weaponAffixes[item.Amount] = item.Affix; weaponAffixes2[item.Amount] = item.Affix2; ownedWeapons |= 1 << item.Amount; if (weapon == item.Amount) damage += WeaponPower(item.Amount) - oldPower; notice = $"ARMA {WeaponRarityName(weaponRarities[item.Amount])}: {WeaponDisplayName(item.Amount)}"; break; case "ebb": questStage = 32; notice = "MEMÓRIA: O VEIO LIVRE"; ShowMemory("A CORRENTE NÃO É UMA ROTA", "O vigia mantinha o fluxo preso num único giro. Quando o canal se abre, a água volta a mudar de direção conforme a cheia. Aven poderá ensinar o Escutador a chamar a vazante sem obrigar ninguém a segui-la. A gravação termina antes de indicar para onde foram os viajantes."); break; case "counter": counterTrace = true; questStage = 35; notice = "MEMÓRIA: O COMPASSO DAS MARGENS"; ShowMemory("O AFINADOR DAS MARGENS", "O Afinador não guardava um mapa. Regulava a força que mantém os dois lados do rio em equilíbrio; alguém o convenceu de que só um fluxo constante poderia proteger a comunidade. Ao ceder, o mecanismo volta a alternar entre impulso e retorno. A gravação não contém nome, rota nem notícia de Nara."); break; case "verse": riftVerse = true; questStage = 39; notice = "MEMÓRIA: A NOTA QUE NÃO MANDA"; ShowMemory("A NOTA QUE NÃO MANDA", "O eco mostra vozes diferentes sustentando a mesma ponte sonora sem se fundirem. Uma contravoz tenta fixá-las numa nota só; quando cede, a cantaria volta a responder com intervalos próprios. A lembrança não contém nome, rota nem notícia de Nara."); break; case "beyond": beyondTrace = true; questStage = 29; notice = "SINAL DE PASSAGEM RECUPERADO"; ShowMemory("NÓS QUE PODEM SER DESFEITOS", "O sinal foi deixado por viajantes que passaram pela margem durante a cheia. Ele confirma apenas que o grupo cruzou junto e escolheu para onde seguir ao chegar à outra margem. Não indica qual margem, nome ou destino pertence a Nara."); break; case "witness": upstreamTrace = true; questStage = 26; notice = "MEMÓRIA: A TESTEMUNHA DA MARÉ"; ShowMemory("A ÚLTIMA CURVA DE NARA", "A testemunha não guarda um mapa nem um nome. Guarda a passagem de um grupo que atravessou o rio contra a corrente. Quando alguém a chamou pelo nome escrito no registro de Véspera, Nara respondeu com outro nome e pediu que o grupo o aprendesse. Eles aceitaram e seguiram juntos para além das águas. O eco se encerra antes de revelar a palavra escolhida ou o lugar a que chegaram."); break; case "register": questStage = 23; notice = "REGISTRO DE PASSAGEM RECUPERADO"; ShowMemory("OS NOMES QUE NÃO SÃO POSSE", "O livro contém rotas e relatos escritos por viajantes diferentes. Aven lê em voz alta apenas o trecho assinado por quem escolheu compartilhá-lo. Uma anotação de Nara confirma que ela seguiu adiante com o grupo, mas não revela seu nome atual nem seu paradeiro. O registro fica com a comunidade, não com Véspera."); break; case "chorus": chorusMemory = true; questStage = 14; notice = "MEMÓRIA: CORO DOS NOMES CONSUMIDOS"; ShowMemory("A TECELÃ DE NOMES", "Quando o fio se rompe, as vozes não se calam. O Coro: “Não somos uma coisa que veio de fora. Somos todos os nomes que o Lume tomou e não conseguiu guardar. Nara carregou parte de nós para longe, mas o farol continua nos chamando.” O Silêncio de Baixo é a pressão dessas memórias tentando voltar juntas."); break; case "first": firstMemory = true; questStage = 16; notice = "MEMÓRIA: O PRIMEIRO NOME"; ShowMemory("A PRIMEIRA VIGÍLIA", "A guardiã recorda seu nome: Ilyra, a primeira Escutadora. Ela não acendeu o Lume para apagar as vozes, mas para que toda memória encontrasse um lugar onde ser ouvida. Séculos depois, os guardiões inverteram o propósito e transformaram lembrança em combustível. Ilyra entregou o próprio nome para impedir que o farol esquecesse sua origem. No último eco, viu Nara atravessar a corrente levando vozes que o Lume não comportava. Não sabe o que aconteceu com ela depois, mas deixa um aviso: se alguém a encontrar, não deve chamá-la pelo nome que o farol escolheu."); break; case "west": westTrace = true; questStage = 19; notice = "MEMÓRIA: MAPA DA MARGEM ESCOLHIDA"; ShowMemory("O MAPA DE NARA", "A Cartógrafa guardava um registro de quem atravessou o Vau. Entre os traços recentes, você reconhece a letra de Nara: “Aqui, ninguém precisa conservar o nome que o Lume escolheu. Ajudei a levantar um abrigo e segui com os que queriam continuar a oeste. Se veio atrás de mim, não use o parentesco como resposta. Pergunte primeiro quem escolhi ser.” A tinta e o caminho são seus, mas o mapa não diz quando ela partiu nem onde está agora."); break; case "charm": if (item.Amount is < 1 or > 13) { drops.Remove(item); return; } bool newCharm = (ownedCharms & (1 << (item.Amount - 1))) == 0; ownedCharms |= 1 << (item.Amount - 1); if (equippedCharm == 0 && equippedCharm2 != item.Amount) equippedCharm = item.Amount; else if (equippedCharm2 == 0 && equippedCharm != item.Amount) equippedCharm2 = item.Amount; notice = newCharm ? $"TALISMÃ: {charmNames[item.Amount]}" : $"ECO DUPLICADO: {charmNames[item.Amount]}"; break; case "armor": armor = true; notice = "PEITORAL DE PEDRA ENCONTRADO"; break; case "relic": relic = true; notice = "FRAGMENTO DE VOZ ENCONTRADO"; ShowMemory("ECO PRESO NO RELICÁRIO", "Nara: “Eu vim por vontade própria. Se encontrou esta lembrança, procure o arquivo de Sileth.”"); break; case "seal": archiveSeal = true; damage += 8; questStage = 4; notice = "SELO DA VIGÍLIA • DANO +8"; ShowMemory("SELO DA VIGÍLIA", "O selo guarda a última gravação de Nara: “Sileth não apagou os nomes para alimentar o Lume. Ela tentou esconder que o próprio farol está aprendendo a escolher quem esquecer.” Leve esta prova de volta à Curadora."); break; case "bell": riverBell = true; maxHealth += 20; health = Math.Min(maxHealth, health + 20); questStage = 6; notice = "SINO DE NARA • VIDA MÁXIMA +20"; ShowMemory("O SINO SEM BADALO", "A marca no bronze pertence a Nara. A voz gravada é de uma mulher que atravessou o rio: “Não estou presa. O farol me escuta porque eu aceitei levar as memórias para longe de Véspera. Se ainda me procura, não tente me trazer de volta antes de ouvir o que o rio guarda.”"); break; case "lens": lumeLens = true; damage += 12; questStage = 9; notice = "PRISMA DO LUME • DANO +12"; ShowMemory("O PRISMA DA PRIMEIRA VIGÍLIA", "A luz projeta Nara ao lado de Sileth, anos antes da noite dos sinos. Nara escolheu ser a portadora das lembranças que o Lume não conseguia guardar. A imagem se rompe antes de mostrar onde ela está agora."); break; case "trace": naraTrace = true; questStage = 11; notice = "RASTRO DE NARA • NOVA MEMÓRIA"; ShowMemory("A LEMBRANÇA QUE SOBROU", "O eco não mostra um rosto. Mostra Nara ensinando alguém, do outro lado do rio, a pronunciar um nome que Véspera apagou. Sua última frase não pede resgate: “Se a cidade ainda lembrar de nós, eu saberei voltar.”"); break; }
        drops.Remove(item); noticeTime = 1.7f;
    }

    private void RollWeapon(int index)
    {
        RollWeapon(index, random.Next(100), random.Next(5), random.Next(8), random.Next(7));
    }

    private void RollWeapon(int index, int rarityRoll, int bonusRoll, int affixRoll, int secondAffixRoll = 0)
    {
        var roll = RollWeaponStats(rarityRoll, bonusRoll, affixRoll, secondAffixRoll);
        weaponRarities[index] = roll.Rarity; weaponBonuses[index] = roll.Bonus; weaponAffixes[index] = roll.Affix; weaponAffixes2[index] = roll.Affix2;
    }

    private static (int Rarity, int Bonus, int Affix, int Affix2) RollWeaponStats(int rarityRoll, int bonusRoll, int affixRoll, int secondAffixRoll = 0)
    {
        int rarity = rarityRoll < 62 ? 0 : rarityRoll < 95 ? 1 : 2;
        int bonus = rarity switch { 0 => bonusRoll % 3, 1 => 3 + bonusRoll % 4, _ => 7 + bonusRoll % 5 };
        int affix = rarity == 0 ? 0 : EquipmentCatalog.RolledWeaponAffix(affixRoll % EquipmentCatalog.RandomlyRolledAffixCount);
        int affix2 = rarity == 2 ? EquipmentCatalog.RolledSecondaryWeaponAffix(secondAffixRoll, affix) : 0;
        return (rarity, bonus, affix, affix2);
    }

    private static bool EliteWeaponDropRoll(int roll, int consecutiveMisses = 0, int bonusChance = 0) => consecutiveMisses >= 2 || roll >= 0 && roll < 35 + Math.Clamp(bonusChance, 0, 15);
    private static int NextEliteWeaponMisses(int misses, bool dropped) => dropped ? 0 : Math.Min(2, Math.Max(0, misses) + 1);
    private static int BossWeaponRollFloor(int room, int tier = 0) => Math.Min(99, (room >= 25 ? 85 : room >= 12 ? 75 : 62) + DifficultyBossRarityBonus(tier));

    private Drop CreateWeaponDrop(PointF pos, int index, bool bossReward = false)
    {
        int rarityFloor = vigilActive ? Math.Min(99, 85 + vigilFloor / 2) : BossWeaponRollFloor(room >= 43 ? 4 : room, bossReward ? difficultyTier : 0);
        var roll = RollWeaponStats(bossReward ? random.Next(rarityFloor, 100) : random.Next(100), random.Next(5), random.Next(EquipmentCatalog.RandomlyRolledAffixCount), random.Next(EquipmentCatalog.RandomlyRolledAffixCount - 1));
        return new Drop { Pos = pos, Kind = "weapon", Amount = index, Rarity = roll.Rarity, Bonus = roll.Bonus, Affix = roll.Affix, Affix2 = roll.Affix2 };
    }

    private Drop CreateRelicWeaponDrop(PointF pos, int index)
    {
        var roll = RollWeaponStats(95, random.Next(5), random.Next(EquipmentCatalog.RandomlyRolledAffixCount), random.Next(EquipmentCatalog.RandomlyRolledAffixCount - 1));
        return new Drop { Pos = pos, Kind = "weapon", Amount = index, Rarity = roll.Rarity, Bonus = roll.Bonus, Affix = roll.Affix, Affix2 = roll.Affix2 };
    }

    private string DropLabel(Drop item)
    {
        return item.Kind switch
        {
            "gold" => item.Amount + " OURO",
            "potion" => "POÇÃO DE VÉSPERA",
            "glass" => item.Amount + " SAL VÍTREO",
            "weapon" => WeaponDropComparison(item),
            "charm" => charmNames[item.Amount],
            "armor" => "PEITORAL DE PEDRA",
            "relic" => "FRAGMENTO DO RELICÁRIO",
            "seal" => "SELO DA VIGÍLIA",
            "bell" => "SINO DE NARA",
            "lens" => "PRISMA DO LUME",
            "trace" => "LEMBRANÇA DE NARA",
            "chorus" => "CORO DOS NOMES",
            "first" => "MEMÓRIA DE ILYRA",
            "west" => "MAPA DE NARA",
            "register" => "REGISTRO COMPARTILHADO",
            "witness" => "TESTEMUNHO DA MARÉ",
            "ebb" => "MEMÓRIA DA VAZANTE",
            "counter" => "COMPASSO DAS MARGENS",
            "verse" => "NOTA DA CONTRAVOZ",
            "deep" => "ECO DOS PASSOS SEM NOME",
            "signal" => "SINAL: A LUZ QUE ESPERA",
            "route" => "TRAÇADO DAS ROTAS ABERTAS",
            "silence" => "MEMÓRIA DAS VOZES DEVOLVIDAS", "borrowed" => "ECO COMPARTILHADO", "unstruck" => "BADALADA NÃO ENVIADA",
            "beyond" => "SINAL DE PASSAGEM",
            _ => "ECO"
        };
    }

    private string WeaponDropComparison(Drop item)
    {
        int proposedDamage = damage - WeaponPower(weapon) + EquipmentCatalog.Weapon(item.Amount).BaseDamage + item.Bonus;
        int damageChange = proposedDamage - damage;
        string currentAffixes = WeaponGroundAffix(weaponAffixes[weapon], weaponAffixes2[weapon], weaponRarities[weapon]);
        if (currentAffixes.Length == 0) currentAffixes = "sem afixo";
        string profile = weapon == item.Amount
            ? WeaponTraitSummary(item.Amount)
            : $"{WeaponTraitSummary(weapon)} → {WeaponTraitSummary(item.Amount)}";
        return $"{WeaponRarityName(item.Rarity)} {EquipmentCatalog.Weapon(item.Amount).Name}\nDANO: {damage} → {proposedDamage} ({damageChange:+#;-#;0})\nPERFIL: {profile}\nAFIXOS NOVOS: {WeaponGroundAffix(item)}\nATUAL: {currentAffixes}";
    }

    private static string WeaponTraitSummary(int index) => EquipmentCatalog.Weapon(index).TraitSummary;

    private static string WeaponGroundAffix(Drop item) => WeaponGroundAffix(item.Affix, item.Affix2, item.Rarity);
    private static string WeaponGroundAffix(int affix, int affix2, int rarity)
    {
        string affixes = string.Join(" + ", new[] { WeaponGroundAffix(affix, rarity), WeaponGroundAffix(affix2, rarity) }.Where(description => description.Length > 0));
        return affixes.Length == 0 ? "sem afixo" : affixes;
    }
    private static string WeaponGroundAffix(int affix, int rarity) => EquipmentCatalog.WeaponAffix(affix).GroundText(rarity);

    private void DrawDrop(Graphics g, Drop item)
    {
        Color color = item.Kind switch
        {
            "relic" => Color.Violet, "seal" => Color.LightSkyBlue, "bell" => Color.FromArgb(162, 212, 192),
            "lens" => Color.FromArgb(248, 185, 95), "trace" => Color.FromArgb(185, 231, 215), "chorus" => Color.Khaki,
            "first" => Color.Goldenrod, "west" => Color.LightSkyBlue, "register" => Color.LightSteelBlue,
            "deep" => Color.FromArgb(190, 225, 235), "borrowed" => Color.FromArgb(130, 210, 204), "unstruck" => Color.FromArgb(223, 190, 117), "glass" => Color.LightCyan, "beyond" => Color.PaleGreen, "ebb" => Color.DodgerBlue,
            "counter" => Color.LightGoldenrodYellow, "verse" => Color.Orchid, "charm" => item.Amount is 7 or 8 ? Color.MediumTurquoise : item.Amount == 4 ? Color.Goldenrod : item.Amount == 5 ? Color.LightSeaGreen : item.Amount == 6 ? Color.PaleGreen : Color.Plum,
            "armor" => Color.MediumAquamarine, "weapon" => item.Rarity == 2 ? Color.Turquoise : item.Rarity == 1 ? Color.Goldenrod : Color.CornflowerBlue,
            "potion" => Color.IndianRed, _ => Color.Goldenrod
        };
        using var icon = new SolidBrush(color);
        using var edge = new Pen(Color.FromArgb(230, 220, 190), 1);
        g.FillEllipse(icon, item.Pos.X - 8, item.Pos.Y - 8, 16, 16);
        g.DrawEllipse(edge, item.Pos.X - 8, item.Pos.Y - 8, 16, 16);

        if (Distance(player, item.Pos) > 150) return;
        using var font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        using var labelBrush = new SolidBrush(item.Kind == "weapon" && item.Rarity == 0 ? Color.AntiqueWhite : color);
        using var background = new SolidBrush(Color.FromArgb(225, 14, 17, 18));
        using var border = new Pen(Color.FromArgb(210, color), 1);
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        string label = DropLabel(item);
        float dropDistance = Distance(player, item.Pos);
        if (item.Kind == "weapon" && dropDistance <= 75)
        {
            (int salvageGold, int salvageGlass) = WeaponSalvageReward(item.Rarity);
            label += $"\nE recolhe • X desmonta • +{salvageGold} ouro{(salvageGlass > 0 ? $" / +{salvageGlass} sal" : "")}";
        }
        float maxWidth = item.Kind == "weapon" ? 310 : 196;
        SizeF measured = g.MeasureString(label, font, (int)maxWidth - 12);
        float width = Math.Clamp(measured.Width + 12, 94, maxWidth);
        float height = measured.Height + 5;
        float x = Math.Clamp(item.Pos.X - width / 2, 6, 954 - width);
        float y = Math.Clamp(item.Pos.Y > 180 ? item.Pos.Y - height - 15 : item.Pos.Y + 16, 108, 550 - height);
        var bounds = new RectangleF(x, y, width, height);
        g.FillRectangle(background, bounds);
        g.DrawRectangle(border, bounds.X, bounds.Y, bounds.Width, bounds.Height);
        g.DrawString(label, font, labelBrush, bounds, format);
    }

    private void DrawInventory(Graphics g)
    {
        using var b = new SolidBrush(Color.FromArgb(245, 17, 19, 20)); using var itemFont = new Font("Segoe UI", 10.5f); using var effectFont = new Font("Segoe UI", 8.5f); g.FillRectangle(b, 120, 120, 720, 520);
        DrawCentered(g, "INVENTÁRIO • 1–4/0/9 ARMAS • 5 PEITORAL • 6 RELICÁRIO • F8/F9 GREVAS • TALISMÃS 7/8/F7 • I FECHAR", 150, new Font("Segoe UI", 10.5f, FontStyle.Bold), Color.Goldenrod);
        for (int i = 0; i < 6; i++) { int x = i % 2 == 0 ? 150 : 500, y = 188 + i / 2 * 48; string key = i < 4 ? (i + 1).ToString() : i == 4 ? "0" : "9"; bool found = (ownedWeapons & (1 << i)) != 0; Brush rarityBrush = !found ? Brushes.Gray : weaponRarities[i] == 2 ? Brushes.MediumTurquoise : weaponRarities[i] == 1 ? Brushes.Goldenrod : Brushes.AntiqueWhite; g.DrawString($"{key}. {WeaponDisplayName(i)} {(weapon == i ? "• EQUIPADA" : found ? "• DISPONÍVEL" : "• NÃO ENCONTRADA")}", itemFont, rarityBrush, x, y); g.DrawString(found ? $"DANO {WeaponPower(i) - WeaponPower(weapon):+#;-#;0} • {WeaponTraitSummary(i)}" : WeaponTraitSummary(i), effectFont, Brushes.DarkGray, new RectangleF(x + 16, y + 19, 315, 18)); }
        g.DrawString($"5. PEITORAL DE PEDRA {(armorOn ? relicOn ? "EQUIPADO • -35% DANO • VIGÍLIA DO VEIO" : "EQUIPADO • -30% DANO" : armor ? "DISPONÍVEL" : "NÃO ENCONTRADO")}", itemFont, Brushes.AntiqueWhite, 150, 316);
        g.DrawString($"   Traço: {ArmorTraitName(armorTrait)} — {ArmorTraitDescription(armorTrait)}", effectFont, Brushes.DarkGray, new RectangleF(150, 341, 650, 17));
        g.DrawString($"6. RELICÁRIO VITAL {(relicOn ? armorOn ? $"EQUIPADO • CURA {RelicKillHealing()}/ABATE • PULSO +15%" : $"EQUIPADO • CURA {RelicKillHealing()} POR ABATE" : relic ? "DISPONÍVEL" : "NÃO ENCONTRADO")}", itemFont, Brushes.AntiqueWhite, 150, 360);
        g.DrawString($"   Traço: {RelicTraitName(relicTrait)} — {RelicTraitDescription(relicTrait)}", effectFont, Brushes.DarkGray, new RectangleF(150, 377, 650, 17));
        if (HasStoneConvergenceResonance() || HasStepAmpouleResonance() || HasLastStandNourishResonance() || HasMarkingResonance())
        {
            string resonance = string.Join(" • ", new[] { HasStoneConvergenceResonance() ? "LAJE: anula a penalidade do Selo" : null, HasStepAmpouleResonance() ? "PASSO: poções adiantam recargas em 1,25 s" : null, HasLastStandNourishResonance() ? "LASTRO: abates curam +7 com vida crítica" : null, HasMarkingResonance() ? "MARCA: dano +20% e abates adiantam recargas em 1,25 s" : null }.Where(value => value is not null));
            g.DrawString(resonance, effectFont, Brushes.LightSteelBlue, new RectangleF(150, 394, 650, 17));
        }
        g.DrawString($"SELO DA VIGÍLIA {(archiveSeal ? "ENCONTRADO • +8 DANO" : "AINDA NÃO ENCONTRADO")}", itemFont, Brushes.AntiqueWhite, 150, 407);
        g.DrawString($"SINO DE NARA {(riverBell ? "ENCONTRADO • +20 VIDA MÁXIMA" : "AINDA NÃO ENCONTRADO")}", itemFont, Brushes.AntiqueWhite, 150, 433);
        g.DrawString($"PRISMA DO LUME {(lumeLens ? "ENCONTRADO • +12 DANO" : "AINDA NÃO ENCONTRADO")}", itemFont, Brushes.AntiqueWhite, 150, 459);
        g.DrawString($"RASTRO DE NARA {(naraTrace ? "RECUPERADO • MEMÓRIA PRESERVADA" : "AINDA NÃO ENCONTRADO")}", itemFont, Brushes.AntiqueWhite, 150, 485);
        g.DrawString($"GREAVAS DE RESSACA {(tideBootsOn ? "EQUIPADAS" : tideBoots ? "DISPONÍVEIS" : "NÃO ENCONTRADAS")}", itemFont, tideBoots ? Brushes.LightCyan : Brushes.Gray, 500, 407);
        string bootAspect = tideBootsAspect switch { 1 => "Passo leve • movimento +8%", 2 => "Arranque • recarga da esquiva −0,12 s", _ => "Contramaré • corrente costeira −45%" };
        g.DrawString(tideBoots ? $"F8 equipa • F9 muda: {bootAspect}" : "Estojo do Intervalo • Costa das Badaladas", effectFont, Brushes.DarkGray, new RectangleF(500, 431, 315, 30));
        string[] availableCharms = Enumerable.Range(1, charmNames.Length - 1).Where(i => (ownedCharms & (1 << (i - 1))) != 0).Select(i => charmNames[i]).ToArray();
        string charmList = availableCharms.Length == 0 ? "NENHUM" : string.Join("\n", availableCharms.Chunk(5).Select(group => string.Join(" • ", group)));
        g.DrawString($"TALISMÃS: {charmList}   •   SAL VÍTREO: {tideGlass}", effectFont, Brushes.Plum, new RectangleF(150, 506, 650, 45));
        string firstCharm = equippedCharm == 0 ? "VAZIO" : charmNames[equippedCharm]; string secondCharm = equippedCharm2 == 0 ? "VAZIO" : charmNames[equippedCharm2];
        string thirdCharm = equippedCharm3 == 0 ? vigilBestFloor >= 30 ? "VAZIO" : "BLOQUEADO • ANDAR 30" : charmNames[equippedCharm3];
        g.DrawString($"7/I: {firstCharm}    8/II: {secondCharm}    F7/III: {thirdCharm}", effectFont, Brushes.Plum, new RectangleF(150, 553, 650, 16));
        var effects = new List<string>();
        if (HasCharm(1)) effects.Add(HasCharmPair(1, 2) ? "SINTONIA BRASA+TRAVESSIA: Redemoinho +57% dano; recarga +0,5 s" : "Nó de Brasa: Redemoinho +35% dano; recarga +1 s");
        if (HasCharm(2)) effects.Add($"Fio de Travessia: esquiva 0,29 s{(HasCharmPair(2, 3) ? " sem custo extra" : ", recarga +0,3 s")}; Sino +35 alcance e +1,5 s de recarga");
        if (HasCharm(3)) effects.Add($"Vidro de Vigília: crítico +{(HasCharmPair(1, 3) ? 17 : 12)} p.p. / {(HasCharmPair(1, 3) ? "2,75" : "2,5")}×; Sino/Vazante +25%; Estilhaço +{(HasCharmPair(3, 5) ? 52 : 33)}% dano{(HasCharmPair(1, 3) ? "; ataque +0,03 s" : "; ataque +0,06 s")}");
        if (HasCharm(4)) effects.Add("Marca da Vigília: +25% dano contra chefes e -20% contra outros ecos");
        if (HasCharm(5)) effects.Add($"Fio da Margem: Estilhaço +44% alcance e 1,8 s de duração; recarga +{(HasCharmPair(3, 5) ? "0,5" : "1,25")} s");
        if (HasCharm(6)) effects.Add("Nó de Lume: Farol +2 s, pulsos a cada 0,85 s; recarga +3,5 s");
        if (HasCharm(7)) effects.Add("Fio do Retorno (Vigília): +40% dano, cura 12% ao abater chefe e +10% dano recebido");
        if (HasCharm(8)) effects.Add("Eco Convergente (Vigília): +18% dano de técnica");
        if (HasCharm(9)) effects.Add("Compasso Fendido: recargas de técnicas -14%; dano recebido +10%");
        if (HasCharm(10)) effects.Add("Âncora Comum: dano recebido -14%; dano causado -10%");
        if (HasCharm(11)) effects.Add("Pulso Bifurcado: técnicas +12% com 2 prontas; ataques básicos +12% com 2 em recarga");
        if (HasCharm(12)) effects.Add("Nó do Contratempo: técnicas +20% de dano; recarga +15%");
        if (HasCharm(13)) effects.Add("Sino da Contramaré: abates de ecos marcados adiantam recargas de técnica em 1,5 s");
        if (HasCharm(14)) effects.Add(HasCharmPair(2, 14) ? "Ritmo das Bóias + Fio de Travessia: esquiva de golpe anunciado adianta recargas em 1 s (intervalo: 1,1 s)" : "Ritmo das Bóias: esquivar de golpe anunciado de chefe adianta recargas em 0,8 s (recarga interna: 1,5 s)");
        if (HasCharm(15)) effects.Add("Eco do Casco: zonas de perigo no chão causam 35% menos dano");
        string effect = effects.Count > 0 ? string.Join(" • ", effects) : "Equipe talismãs para ativar efeitos e sintonias.";
        g.DrawString(effect, effectFont, Brushes.AntiqueWhite, new RectangleF(150, 570, 650, 59));
    }
    private void DrawCharacterSheet(Graphics g)
    {
        using var shade = new SolidBrush(Color.FromArgb(190, 5, 7, 9)); using var background = new SolidBrush(Color.FromArgb(248, 15, 17, 18)); using var border = new Pen(Color.FromArgb(175, 143, 112, 73), 2);
        using var heading = new Font("Georgia", 17, FontStyle.Bold); using var section = new Font("Segoe UI", 11, FontStyle.Bold); using var body = new Font("Segoe UI", 10); using var hint = new Font("Segoe UI", 9);
        g.FillRectangle(shade, 0, 83, 960, 557); g.FillRectangle(background, 105, 125, 750, 500); g.DrawRectangle(border, 105, 125, 750, 500);
        DrawCentered(g, $"FICHA DO {HeroClassTitle()}", 151, heading, Color.FromArgb(230, 193, 129));
        float critical = CriticalChance(), multiplier = CriticalMultiplier(), cooldown = BasicAttackCooldown();
        float expectedDps = damage * HeroBasicAttackMultiplier() * BasicAttackSpecializationMultiplier() * (1 + critical * (multiplier - 1)) / cooldown;
        float mitigation = 1 - HeroDamageTakenMultiplier() * ArmorDamageTakenMultiplier() * RelicDamageTakenMultiplier() * WeaponDamageTakenMultiplier() * CharmDamageTakenMultiplier() * CombatMasteryDamageTakenMultiplier() * CombatApexDamageTakenMultiplier();
        if (wardTime > 0) mitigation = 1 - (1 - mitigation) * AnchorWardDamageTakenMultiplier();
        if (stoneBastionTime > 0) mitigation = 1 - (1 - mitigation) * StoneBastionDamageTakenMultiplier();
        g.DrawString("ATAQUE", section, Brushes.Goldenrod, 145, 205);
        g.DrawString($"Arma: {WeaponDisplayName(weapon)}", body, Brushes.AntiqueWhite, 145, 232);
        g.DrawString($"Golpe: {damage:0} dano • intervalo {cooldown:0.00} s • alcance {BasicAttackReach(new Enemy { Kind = EnemyKind.Grunt }):0}", body, Brushes.AntiqueWhite, 145, 258);
        g.DrawString($"Crítico: {critical:P0} de chance • {multiplier:0.00}× dano", body, Brushes.AntiqueWhite, 145, 284);
        g.DrawString($"Dano esperado por segundo: {expectedDps:0.0} (alvo sem armadura)", body, Brushes.AntiqueWhite, 145, 310);
        g.DrawString($"Estilhaço de Voz: {VoiceShardDamage():0} dano • recarga {VoiceShardCooldown():0.0} s", body, Brushes.AntiqueWhite, 145, 336);
        g.DrawString($"Talentos: vigor {healthTalent} • domínio {skillTalent} • agilidade {dodgeTalent} • sintonia {techniqueTalent}", body, Brushes.AntiqueWhite, 145, 362);
        g.DrawString("DEFESA E EQUIPAMENTO", section, Brushes.Goldenrod, 515, 205);
        g.DrawString($"Vida máxima: {maxHealth:0} • poção: +45", body, Brushes.AntiqueWhite, 515, 232);
        g.DrawString($"Redução de dano: {mitigation:P0}{(stoneBastionTime > 0 ? " • Bastião ativo" : combatApex == 2 && TechniquesOnCooldown() >= 2 ? " • Âncora ativa" : wardTime > 0 ? " • Manto ativo" : "")}", body, Brushes.AntiqueWhite, 515, 258);
        g.DrawString($"Relicário Vital: cura {RelicKillHealing()} por abate{(armorOn && relicOn ? $" • Pulso +15% por 2,5 s ({veinResonanceTime:0.0} s restantes)" : "")}", body, Brushes.AntiqueWhite, new RectangleF(145, 388, 670, 38));
        g.DrawString($"Traços: {ArmorTraitName(armorTrait)} / {RelicTraitName(relicTrait)}", hint, Brushes.DarkGray, new RectangleF(145, 410, 670, 16));
        g.DrawString($"Recarga da esquiva: {DashCooldown():0.00} s • duração {DashDuration():0.00} s", body, Brushes.AntiqueWhite, 515, 284);
        g.DrawString($"Afixos: {WeaponAffixDescription(weapon)}", body, Brushes.AntiqueWhite, new RectangleF(515, 310, 315, 48));
        string charmOne = equippedCharm == 0 ? "vazio" : charmNames[equippedCharm]; string charmTwo = equippedCharm2 == 0 ? "vazio" : charmNames[equippedCharm2];
        g.DrawString($"Talismãs: {charmOne} + {charmTwo}", body, Brushes.Plum, new RectangleF(515, 362, 315, 45));
        g.DrawString($"Técnicas: F {ShortSkillName(0)}  •  V {ShortSkillName(1)}  •  G {ShortSkillName(2)}  •  C {ShortSkillName(3)}", body, Brushes.AntiqueWhite, new RectangleF(145, 424, 670, 40));
        g.DrawString($"Especialização: {(combatDiscipline == 1 ? "Forma do Gume • básicos +12%, técnicas -10%" : combatDiscipline == 2 ? "Forma do Eco • técnicas +12%, básicos -10%" : "a escolher no nível 5")}", body, combatDiscipline == 2 ? Brushes.Plum : Brushes.Goldenrod, 145, 458);
        g.DrawString($"Maestria: {(combatMastery == 1 ? "Guarda Serena • dano recebido -10%, esquiva +0,20 s" : combatMastery == 2 ? "Passo Impetuoso • esquiva -0,18 s, dano recebido +8%" : level >= 10 ? "a escolher" : "desbloqueia no nível 10")}", body, combatMastery == 2 ? Brushes.Plum : Brushes.Goldenrod, 145, 482);
        g.DrawString($"Foco: {(combatFocus == 1 ? "Caçador de Ecos • chefes +15%" : combatFocus == 2 ? "Quebra-Formação • comuns +10% com 3+ inimigos" : level >= 15 ? "a escolher" : "desbloqueia no nível 15")}", body, combatFocus == 2 ? Brushes.Plum : Brushes.Goldenrod, 145, 506);
        g.DrawString($"Auge: {(combatApex == 1 ? "Coro Suspenso • técnicas +18% com 2+ recargas" : combatApex == 2 ? "Âncora de Eco • dano recebido -12% com 2+ recargas" : level >= 20 ? "a escolher" : "desbloqueia no nível 20")}", body, combatApex == 2 ? Brushes.Plum : Brushes.Goldenrod, new RectangleF(145, 530, 670, 20));
        string tempoDescription = combatTempo == 1 ? "Abertura da Vigília • técnicas +15% se todas prontas" : combatTempo == 2 ? "Resposta do Veio • básicos +12% com 2+ recargas" : level >= 25 ? "a escolher" : "desbloqueia no nível 25";
        g.DrawString($"Ritmo: {tempoDescription}", body, combatTempo == 2 ? Brushes.Plum : Brushes.Goldenrod, new RectangleF(145, 550, 670, 20));
        string instinctDescription = combatInstinct == 1 ? "Primeira Ressonância • +18% contra alvos com 80%+ de vida" : combatInstinct == 2 ? "Último Eco • +18% contra alvos com 30% ou menos de vida" : level >= 30 ? "a escolher" : "desbloqueia no nível 30";
        g.DrawString($"Instinto: {instinctDescription}", body, combatInstinct == 2 ? Brushes.Plum : Brushes.Goldenrod, new RectangleF(145, 570, 670, 20));
        string capstoneDescription = combatCapstone == 1 ? "Maré de Retorno" : combatCapstone == 2 ? "Último Abrigo" : level >= 40 ? "a escolher" : "nível 40";
        string paragonDescription = combatParagon == 1 ? "Pacto da Aurora (+12% dano com vida ≥80%)" : combatParagon == 2 ? "Pacto do Abismo (+20% dano com vida ≤30%)" : level >= 50 ? "a escolher" : "nível 50";
        g.DrawString($"Legado: {capstoneDescription}  •  Paragon: {paragonDescription}", hint, combatParagon == 2 ? Brushes.Plum : Brushes.Goldenrod, new RectangleF(145, 590, 670, 20));
        g.DrawString("P ou ESC fecha", hint, Brushes.DarkGray, 145, 612);
    }

    private void DrawForgeMenu(Graphics g)
    {
        using var shade = new SolidBrush(Color.FromArgb(190, 5, 7, 9)); using var background = new SolidBrush(Color.FromArgb(248, 15, 17, 18)); using var border = new Pen(Color.FromArgb(175, 143, 112, 73), 2);
        using var optionFont = new Font("Segoe UI", 11); using var hintFont = new Font("Segoe UI", 9);
        g.FillRectangle(shade, 0, 83, 960, 557); g.FillRectangle(background, 120, 135, 720, 455); g.DrawRectangle(border, 120, 135, 720, 455);
        if (defenseForgeOpen)
        {
            DrawCentered(g, "GRAVURA DO VEIO • TRAÇOS DE EQUIPAMENTO", 170, new Font("Georgia", 16, FontStyle.Bold), Color.Goldenrod);
            g.DrawString($"PEITORAL • ATUAL: {ArmorTraitName(armorTrait)}", optionFont, armor ? Brushes.AntiqueWhite : Brushes.Gray, 155, 208);
            g.DrawString("1. LAJE FUNDA • peitoral: recebe 6% menos dano adicional", hintFont, Brushes.AntiqueWhite, 155, 238);
            g.DrawString("2. PASSO ANCORADO • esquiva recarrega 0,12 s mais rápido", hintFont, Brushes.AntiqueWhite, 155, 266);
            g.DrawString("3. ÚLTIMO LASTRO • com até 35% de vida, recebe 15% menos dano", hintFont, Brushes.AntiqueWhite, new RectangleF(155, 294, 305, 36));
            g.DrawString("9. RIPOSTA DO VEIO • esquiva prepara +18% no próximo básico", hintFont, Brushes.LightCyan, new RectangleF(155, 330, 305, 34));
            g.DrawString("F6. GUME DE VIGÍLIA • +12% dano contra marcados", hintFont, Brushes.LightCyan, new RectangleF(155, 365, 305, 30));
            g.DrawString("0. VEIO BRUTO • remover o traço", hintFont, Brushes.DarkGray, 155, 393);
            g.DrawString($"RELICÁRIO • ATUAL: {RelicTraitName(relicTrait)}", optionFont, relic ? Brushes.AntiqueWhite : Brushes.Gray, 500, 208);
            g.DrawString("4. FIO NUTRIDO • cura +3 por abate", hintFont, Brushes.AntiqueWhite, 500, 238);
            g.DrawString("5. AMPOLA VIVA • poções curam +15 vida", hintFont, Brushes.AntiqueWhite, 500, 266);
            g.DrawString("6. CORO RESTAURADOR • Pulso do Veio dura +1 s", hintFont, Brushes.AntiqueWhite, new RectangleF(500, 294, 310, 36));
            g.DrawString("7. SELO DA CONVERGÊNCIA • técnicas +10%; dano recebido +6%", hintFont, Brushes.AntiqueWhite, new RectangleF(500, 338, 310, 36));
            g.DrawString("F7. ECO DE RETORNO • abate marcado adianta recargas", hintFont, Brushes.LightCyan, new RectangleF(500, 374, 310, 30));
            g.DrawString("8. ECO SILENTE • remover o traço", hintFont, Brushes.DarkGray, 500, 404);
            g.DrawString($"CADA ALTERAÇÃO CUSTA 100 OURO E 2 SAL VÍTREO   •   DISPONÍVEL: {gold} OURO / {tideGlass} SAL", hintFont, gold < 100 || tideGlass < 2 ? Brushes.IndianRed : Brushes.Goldenrod, new RectangleF(155, 438, 650, 22));
            g.DrawString("1–3 peitoral • 9 quarto traço • F6 quinto traço • 4–7 relicário • F7 quinto • 0/8 removem • T volta", hintFont, Brushes.DarkGray, new RectangleF(155, 464, 650, 24));
            return;
        }
        DrawCentered(g, weaponRarities[weapon] == 0 ? $"TÊMPERA DO VEIO • {EquipmentCatalog.Weapon(weapon).Name} VIRA RARA" : $"TÊMPERA DO VEIO • {EquipmentCatalog.Weapon(weapon).Name}", 170, new Font("Georgia", 16, FontStyle.Bold), Color.Goldenrod);
        string mainAffix = WeaponAffixLabel(weaponAffixes[weapon]); if (mainAffix.Length == 0) mainAffix = "SEM AFIXO";
        string secondaryAffix = weaponRarities[weapon] != 2 ? "NÃO DISPONÍVEL" : WeaponAffixLabel(weaponAffixes2[weapon]); if (secondaryAffix.Length == 0) secondaryAffix = "SEM AFIXO";
        g.DrawString(secondaryForgeOpen ? $"AFIXO SECUNDÁRIO • PRINCIPAL PRESERVADO: {mainAffix} • ATUAL: {secondaryAffix}" : $"AFIXO PRINCIPAL • ATUAL: {mainAffix}{(weaponRarities[weapon] == 2 ? $" • SECUNDÁRIO: {secondaryAffix}" : "")}", hintFont, secondaryForgeOpen ? Brushes.Plum : Brushes.Goldenrod, 165, 190);
        g.DrawString("1. TRÉGUA • recupera 3/6 vida por abate", optionFont, Brushes.AntiqueWhite, 165, 205);
        g.DrawString("2. CONTRACANTO • crítico +7/+11 pontos percentuais", optionFont, Brushes.AntiqueWhite, 165, 230);
        g.DrawString("3. MARÉ • dano de técnica +12%/+18%", optionFont, Brushes.AntiqueWhite, 165, 255);
        g.DrawString("4. PASSO CÉLERE • recarga de técnicas -10%/-16%", optionFont, Brushes.AntiqueWhite, 165, 280);
        g.DrawString("5. LASTRO • dano recebido -8%/-12%", optionFont, Brushes.AntiqueWhite, 165, 305);
        g.DrawString("6. FIO ESTICADO • alcance básico +12%/+20%", optionFont, Brushes.AntiqueWhite, 165, 330);
        g.DrawString("0. CADÊNCIA • críticos adiantam recargas em 0,15/0,25 s por golpe", optionFont, Brushes.AntiqueWhite, 165, 355);
        g.DrawString("8. RUPTURA • críticos ferem por 3 s (6%/8% do dano da arma por segundo)", optionFont, Brushes.Salmon, 165, 380);
        g.DrawString("9. NOME RISCADO • poder único; exclusivo do afixo secundário de Relíquias", optionFont, Brushes.Plum, 165, 405);
        if (finalEnding > 0 && questStage >= 56)
        {
            int goldCost = TalentRespecGoldCost(level), glassCost = TalentRespecGlassCost(level);
            g.DrawString("9. FENDA • ataques básicos causam +12%/+18% contra alvos blindados", optionFont, Brushes.LightSteelBlue, 165, 430);
            g.DrawString("7. REDISTRIBUIR TALENTOS • devolve todos os pontos", optionFont, Brushes.AntiqueWhite, 165, 455);
            int affixGoldCost = secondaryForgeOpen ? 150 : 75, affixGlassCost = secondaryForgeOpen ? 2 : 1;
            g.DrawString(secondaryForgeOpen && weaponRarities[weapon] != 2 ? "AFIXO SECUNDÁRIO EXIGE UMA RELÍQUIA." : $"CUSTO: {affixGoldCost} OURO E {affixGlassCost} SAL VÍTREO   •   DISPONÍVEL: {gold} OURO / {tideGlass} SAL", hintFont, secondaryForgeOpen && weaponRarities[weapon] != 2 ? Brushes.IndianRed : gold < affixGoldCost || tideGlass < affixGlassCost ? Brushes.IndianRed : Brushes.Goldenrod, 165, 485);
            g.DrawString("A alterna principal/secundário • 1–6/8–9 afixo • I Inscrição • Ctrl+9 Nome Riscado ou dificuldade • 0 Cadência • 7 redistribui • T traços • ESC fecha", hintFont, Brushes.DarkGray, 165, 508);
            g.DrawString($"RESSONÂNCIA {difficultyTier} • {DifficultyTierName(difficultyTier)} • HP +{(DifficultyEnemyHealthMultiplier(difficultyTier) - 1) * 100:0}% • dano +{(DifficultyIncomingDamageMultiplier(difficultyTier) - 1) * 100:0}% • recompensas +{(DifficultyRewardMultiplier(difficultyTier) - 1) * 100:0}%", hintFont, Brushes.Orange, 165, 531);
        }
        else
        {
            g.DrawString("9. FENDA • ataques básicos causam +12%/+18% contra alvos blindados", optionFont, Brushes.LightSteelBlue, 165, 430);
            g.DrawString("Valores: RARA / RELÍQUIA.", hintFont, Brushes.DarkGray, 165, 455);
            int affixGoldCost = secondaryForgeOpen ? 150 : 75, affixGlassCost = secondaryForgeOpen ? 2 : 1;
            g.DrawString(secondaryForgeOpen && weaponRarities[weapon] != 2 ? "AFIXO SECUNDÁRIO EXIGE UMA RELÍQUIA." : $"CUSTO: {affixGoldCost} OURO E {affixGlassCost} SAL VÍTREO   •   DISPONÍVEL: {gold} OURO / {tideGlass} SAL", hintFont, secondaryForgeOpen && weaponRarities[weapon] != 2 ? Brushes.IndianRed : gold < affixGoldCost || tideGlass < affixGlassCost ? Brushes.IndianRed : Brushes.Goldenrod, 165, 485);
            g.DrawString("A alterna principal/secundário • 1–6/8–9 afixo • I Inscrição • Ctrl+9 Nome Riscado ou dificuldade • 0 Cadência • T traços • ESC fecha", hintFont, Brushes.DarkGray, 165, 508);
            g.DrawString("RESSONÂNCIAS de desafio desbloqueiam após o epílogo e a expedição XVII.", hintFont, Brushes.DarkGray, 165, 531);
        }
    }

    private void EquipWeapon(int index) { if (index is < 0 or > 5) return; if ((ownedWeapons & (1 << index)) == 0) { notice = "ARMA AINDA NÃO ENCONTRADA"; noticeTime = 1.4f; return; } damage += WeaponPower(index) - WeaponPower(weapon); weapon = index; }
    private void CycleCharm(int slot)
    {
        if (slot == 3 && vigilBestFloor < 30) { notice = "O TERCEIRO ESPAÇO ABRE AO CONCLUIR O 30º ANDAR DA VIGÍLIA"; noticeTime = 2.2f; return; }
        if (slot is < 1 or > 3) return;
        int current = slot switch { 1 => equippedCharm, 2 => equippedCharm2, _ => equippedCharm3 };
        int charmCount = charmNames.Length - 1;
        for (int i = 1; i <= charmCount; i++)
        {
            int candidate = ((current - 1 + i + charmCount) % charmCount) + 1;
            if (candidate == current || candidate == equippedCharm && slot != 1 || candidate == equippedCharm2 && slot != 2 || candidate == equippedCharm3 && slot != 3 || (ownedCharms & (1 << (candidate - 1))) == 0) continue;
            SetEquippedCharm(slot, candidate);
            notice = $"ESPAÇO {slot}: {charmNames[candidate]}";
            noticeTime = 1.6f; return;
        }
        if (current != 0) { SetEquippedCharm(slot, 0); notice = $"ESPAÇO {slot} DE TALISMÃ VAZIO"; }
        else notice = "NENHUM TALISMÃ DISPONÍVEL PARA ESTE ESPAÇO";
        noticeTime = 1.6f;
    }
    private void SetEquippedCharm(int slot, int charm) { if (slot == 1) equippedCharm = charm; else if (slot == 2) equippedCharm2 = charm; else equippedCharm3 = charm; }
    private bool HasCharm(int charm) => equippedCharm == charm || equippedCharm2 == charm || vigilBestFloor >= 30 && equippedCharm3 == charm;
    private bool HasCharmPair(int first, int second) => HasCharm(first) && HasCharm(second);
    private float BuoyRhythmCooldownAdvance() => HasCharmPair(2, 14) ? 1f : 0.8f;
    private float BuoyRhythmInternalCooldown() => HasCharmPair(2, 14) ? 1.1f : 1.5f;
    private float ArmorDamageTakenMultiplier() => !armorOn ? 1f : (relicOn ? 0.65f : 0.7f) * (armorTrait == 1 ? 0.94f : armorTrait == 3 && health <= maxHealth * 0.35f ? 0.85f : 1f);
    private bool HasStoneConvergenceResonance() => armorOn && armorTrait == 1 && relicOn && relicTrait == 4;
    private bool HasMarkingResonance() => armorOn && armorTrait == 5 && relicOn && relicTrait == 5;
    private bool HasStepAmpouleResonance() => armorOn && armorTrait == 2 && relicOn && relicTrait == 2;
    private bool HasLastStandNourishResonance() => armorOn && armorTrait == 3 && relicOn && relicTrait == 1 && health <= maxHealth * 0.35f;
    private bool HasVeinReturnResonance() => armorOn && armorTrait == 4 && relicOn && relicTrait == 3;
    private float MarkedKillCooldownAdvance() => !relicOn || relicTrait != 5 ? 0f : HasMarkingResonance() ? 1.25f : 0.75f;
    private float VeinReturnWindow() => HasVeinReturnResonance() ? 1.9f : 1.35f;
    private float VeinReturnAttackMultiplier() => armorOn && armorTrait == 4 && dashAttackTime > 0 ? 1.18f : 1f;
    private float RelicDamageTakenMultiplier() => relicOn && relicTrait == 4 && !HasStoneConvergenceResonance() ? 1.06f : 1f;
    private int RelicKillHealing() => !relicOn ? 0 : (armorOn ? 8 : 5) + (relicTrait == 1 ? 3 : 0) + (HasLastStandNourishResonance() ? 7 : 0);
    private float VeinResonanceMultiplier() => armorOn && relicOn && veinResonanceTime > 0 ? 1.15f : 1f;

    private static bool ValidWeaponData(SaveData s)
    {
        if (s.WeaponBonuses is null || s.WeaponBonuses.Length is < 3 or > 6 || s.WeaponRarities is null || s.WeaponRarities.Length != s.WeaponBonuses.Length || s.WeaponAffixes is null || s.WeaponAffixes.Length != s.WeaponBonuses.Length || s.WeaponAffixes2 is null || s.WeaponAffixes2.Length != s.WeaponBonuses.Length || s.Weapon >= s.WeaponBonuses.Length || (s.OwnedWeapons & 8) != 0 && s.WeaponBonuses.Length < 4 || (s.OwnedWeapons & 16) != 0 && s.WeaponBonuses.Length < 5 || (s.OwnedWeapons & 32) != 0 && s.WeaponBonuses.Length != 6) return false;
        for (int i = 0; i < s.WeaponBonuses.Length; i++)
        {
            int rarity = s.WeaponRarities[i], bonus = s.WeaponBonuses[i], affix = s.WeaponAffixes[i], affix2 = s.WeaponAffixes2[i];
            if (rarity == 0 ? bonus is < 0 or > 2 || affix != 0 || affix2 != 0 : rarity == 1 ? bonus is < 3 or > 6 || !EquipmentCatalog.IsValidPrimaryAffix(affix) || affix2 != 0 : rarity == 2 ? bonus is < 7 or > 11 || !EquipmentCatalog.IsValidPrimaryAffix(affix) || !EquipmentCatalog.IsValidSecondaryAffix(affix2, rarity) || affix2 == affix : true) return false;
            if ((s.OwnedWeapons & (1 << i)) == 0 && (rarity != 0 || bonus != 0 || affix != 0 || affix2 != 0)) return false;
        }
        return true;
    }
    private void RestoreWeaponData(SaveData s) { Array.Copy(s.WeaponBonuses, weaponBonuses, s.WeaponBonuses.Length); Array.Copy(s.WeaponRarities, weaponRarities, s.WeaponRarities.Length); Array.Copy(s.WeaponAffixes, weaponAffixes, s.WeaponAffixes.Length); Array.Copy(s.WeaponAffixes2, weaponAffixes2, s.WeaponAffixes2.Length); for (int i = 0; i < weaponRarities.Length; i++) if (weaponRarities[i] == 2 && weaponAffixes2[i] == 0) weaponAffixes2[i] = weaponAffixes[i] % 5 + 1; }

    private string WeaponTraitDescription(int index) => EquipmentCatalog.Weapon(index).TraitDescription;
}
