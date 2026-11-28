using System.Collections.Generic;
using UnityEngine;

/// <summary>Narrative, naming, and per-room text content for the Scadryk campaign.</summary>
public sealed partial class ScadrykGameDirector
{
    private const string OpeningMemory = "Depois que a Grande Onda cobriu a costa, os três sinos emudeceram. Nara, a Ouvinte, seguiu os nomes que a água devolvia. No último, uma voz chamou por Nima — e o som veio das raízes, não do mar.";
    private const string EndingMemory = "O Arauto quis ocultar a catástrofe para poupar os vivos da dor, mas o silêncio aprisionou os mortos. Nara escolhe lembrar e abre o limiar para que todos possam atravessar.";
    internal const string VictoryEndingText = "Os três sinos respondem sem apagar o som da onda. O Arauto tentou poupar os vivos, mas seu silêncio aprisionou os mortos. Nara escolhe lembrar sem prender. Nima atravessa levando o próprio nome; cada voz pode seguir seu caminho.";

    private static readonly string[] PhaseNames = { "Margens do Primeiro Eco", "Bacia do Sal Imóvel", "Costa das Badaladas" };
    private static readonly string[][] roomNames = {
        new[] { "Margem Alagada", "Vau de Cinza", "Casa das Comportas" },
        new[] { "Juncal Raso", "Bacia Imóvel", "Câmara do Sal" },
        new[] { "Costa das Badaladas", "Fileira das Bóias", "Coro da Maré Falsa" }
    };
    private static readonly string[][] MapRoomLabels = {
        new[] { "Margem", "Vau cinza", "Comporta" },
        new[] { "Juncal", "Bacia", "Câmara sal" },
        new[] { "Badaladas", "Bóias", "Coro maré" }
    };
    private static readonly string[] RoomGoals = {
        "Libertar os Ecos da margem", "Abrir a antiga comporta", "Ouvir o guardião das raízes",
        "Escutar o vestígio do sal", "Ressoar dois cristais com o Pulso", "Libertar o guardião do sal",
        "Escutar o sino encalhado", "Ressoar farol e sino do recife nessa ordem", "Enfrentar o Arauto do Vazio"
    };
    private static readonly string[] RoomStories = {
        "Nas poças, nomes vêm em pares: alguém que partiu, alguém que ainda espera.",
        "Raízes prendem sinos pequenos à madeira. Um deles vibra sem vento; a comporta guarda marcas de quem tentou abri-la por dentro.",
        "A Raiz-Mor chama o cativeiro de abrigo. Nara reconhece entre as raízes o primeiro nome de Nima.",
        "Nima não foi levada pela onda: o sal reteve até o som que poderia anunciar seu retorno.",
        "Pegadas cruzam a prateleira e voltam sobre si mesmas. Dois cristais ainda respondem ao Pulso; um Eco espera sob o sal.",
        "A Vigia do Sal mede uma onda imóvel com a mão: ‘Enquanto ela não chega, ninguém se perde de novo.’",
        "O sino encalhado toca quando a água recua. Alguém gravou Nima na madeira e se recusou a apagar o nome.",
        "Farol primeiro, sino depois: a costa deixou a ordem marcada nas bóias. Um Eco antigo aponta para a passagem baixa.",
        "O Arauto reuniu os três silêncios para impedir outra perda. Nara veio descobrir quem pagou por essa promessa."
    };
    private static readonly string[] GuardianNames = { "A Raiz-Mor", "A Vigia do Sal", "O Vigia do Coro" };
    private static readonly string[] GuardianIntroductions = {
        "A Raiz-Mor: eu prendi a voz de Nima. Se soltar os nomes, a correnteza os leva outra vez.",
        "A Vigia do Sal: congelei a onda no instante da perda. Você chama de prisão o único segundo que ainda podemos guardar.",
        "O Vigia do Coro: fiz os sinos calarem. Cada toque devolvia a dor; o Arauto transformou meu medo em lei."
    };
    private static readonly string[] GuardianMemories = {
        "A Raiz-Mor entrega o Crescente: Nima tocou o primeiro sino para avisar a margem, e as raízes guardaram seu nome quando a onda levou a resposta. O arco interrompe Ecos.",
        "A Vigia do Sal devolve o Estouro: o Arauto deteve a última onda depois que a costa perdeu seus nomes, mas deixou tudo preso no instante da perda. A explosão expõe alvos a golpes seguintes.",
        "O Vigia do Coro entrega o Rompante: os guardiões aceitaram calar os sinos para poupar os vivos; o Arauto fez do medo uma regra sem fim. A onda afasta Ecos comuns."
    };
    private static readonly string[] BasinWitnessLines = {
        "Nima: Sou a voz do primeiro sino. Toquei para avisar a margem; a onda não respondeu, e meu nome voltou preso às raízes.",
        "Nara: A Raiz-Mor chamou isso de proteção. Guardar uma voz para sempre também é fazê-la desaparecer.",
        "Nima: O sal ainda segura a onda no instante em que partiu. Ressoem os cristais; a água precisa voltar a seguir."
    };
    private static readonly string[] CoastWitnessLines = {
        "Nima: Você devolveu movimento ao sal. Agora sabemos: o Arauto parou a onda depois que a costa perdeu seus nomes.",
        "Nara: Os guardiões aceitaram o silêncio por medo. Vou escutar cada um antes de decidir o que fazer com o Arauto.",
        "Nima: O Vigia do Coro ouviu o Arauto e nunca mais tocou. Faça o farol responder primeiro; o sino do recife vem depois."
    };
    private static readonly Dictionary<string, string> LegacyStoryJournalEntries = CreateLegacyStoryJournalEntries();

    internal static string VictoryEndingTextForSecrets(int revealedSecrets) => Mathf.Clamp(revealedSecrets, 0, 3) switch
    {
        0 => VictoryEndingText,
        1 or 2 => "Os três sinos respondem sem apagar o som da onda. O Arauto tentou poupar os vivos, mas seu silêncio aprisionou os mortos. As memórias libertadas acompanham Nara. Nima atravessa levando o próprio nome; cada voz pode seguir seu caminho.",
        _ => "Os três sinos respondem sem apagar o som da onda. O Arauto tentou poupar os vivos, mas seu silêncio aprisionou os mortos. Nara devolveu as três memórias ocultas ao coro. Nima atravessa levando o próprio nome; nenhuma voz permanece presa."
    };

    private static Dictionary<string, string> CreateLegacyStoryJournalEntries()
    {
        var replacements = new Dictionary<string, string>
        {
            ["Depois que a Grande Onda cobriu a costa, os três sinos emudeceram. Nara, a Ouvinte, ainda escutava os nomes perdidos e seguiu a maré imóvel."] = OpeningMemory,
            ["A Raiz-Mor: sem mim, os nomes voltam à correnteza."] = GuardianIntroductions[0],
            ["A Vigia do Sal: a maré ainda não pode voltar."] = GuardianIntroductions[1],
            ["O Vigia do Coro: os sinos devolverão a dor."] = GuardianIntroductions[2],
            ["A Raiz-Mor entrega o Crescente e revela que Nima tocou o primeiro sino; seu nome ficou guardado nas raízes. O arco do Crescente interrompe Ecos."] = GuardianMemories[0],
            ["A Vigia do Sal devolve o Estouro e revela que o Arauto deteve a última onda, deixando a costa presa no instante em que perdeu suas vozes. A explosão do Estouro expõe alvos a golpes seguintes."] = GuardianMemories[1],
            ["O Vigia do Coro entrega o Rompante e revela que o Arauto mandou calar os sinos para poupar os vivos. A onda do Rompante afasta Ecos comuns."] = GuardianMemories[2],
            ["Sou Nima, a voz do primeiro sino. A Raiz-Mor guardou meu nome para que o silêncio não me apagasse. Ainda lembro o Arauto chamando esse silêncio de abrigo."] = BasinWitnessLines[0],
            ["Nara: Também tenho medo do que a lembrança traz. Mas não vou pedir que vocês desapareçam."] = BasinWitnessLines[1],
            ["O sal ainda segura a onda no instante em que ela partiu. Faça os cristais responderem; a água precisa voltar a seguir."] = BasinWitnessLines[2],
            ["Você devolveu movimento ao sal. Agora os sinos lembram que cada voz tinha alguém esperando por ela."] = CoastWitnessLines[0],
            ["Nara: Posso lembrar e seguir em frente. Vou abrir a passagem para que ninguém fique preso."] = CoastWitnessLines[1],
            ["O Arauto chama o silêncio de abrigo. Pergunte a quem perdeu o próprio nome se isso parece paz."] = CoastWitnessLines[2]
        };

        replacements["Sou Nima, a voz do primeiro sino. Toquei para avisar a margem; a onda não respondeu, e meu nome voltou preso às raízes."] = BasinWitnessLines[0];
        replacements["O sal ainda segura a onda no instante em que partiu. Ressoem os cristais; a água precisa voltar a seguir."] = BasinWitnessLines[2];
        replacements["Você devolveu movimento ao sal. Agora sabemos: o Arauto parou a onda depois que a costa perdeu seus nomes."] = CoastWitnessLines[0];
        replacements["O Vigia do Coro ouviu o Arauto e nunca mais tocou. Faça o farol responder primeiro; o sino do recife vem depois."] = CoastWitnessLines[2];

        string[] previousRoomStories = {
            "A água conserva vozes que Nara pensava perdidas.", "A comporta espera uma Ouvinte; raízes sobre a água cruzam o bosque.", "O guardião prende nas raízes o primeiro sino.",
            "A bacia está imóvel, mas o sal ainda se recorda da maré.", "Dois cristais guardam a rota; a prateleira de sal esconde uma lembrança.", "O zelador do sal protege uma lembrança que não é sua.",
            "Na costa, as badaladas cessaram antes da última onda.", "Farol primeiro, sino depois. A vereda das bóias guarda um Eco antigo.", "O Arauto reuniu os três silêncios para esconder a verdade."
        };
        for (int index = 0; index < previousRoomStories.Length; index++)
        {
            int phase = index / RoomsPerPhase;
            int room = index % RoomsPerPhase;
            string currentStory = room == RoomsPerPhase - 1 ? GuardianIntroductions[phase] : RoomStories[index];
            replacements[RoomStoryJournalEntry(roomNames[phase][room], previousRoomStories[index])] =
                RoomStoryJournalEntry(roomNames[phase][room], currentStory);
        }

        replacements[RoomStoryJournalEntry(roomNames[0][2], "A Raiz-Mor: sem mim, os nomes voltam à correnteza.")] =
            RoomStoryJournalEntry(roomNames[0][2], GuardianIntroductions[0]);
        replacements[RoomStoryJournalEntry(roomNames[1][2], "A Vigia do Sal: a maré ainda não pode voltar.")] =
            RoomStoryJournalEntry(roomNames[1][2], GuardianIntroductions[1]);
        replacements[RoomStoryJournalEntry(roomNames[2][2], "O Vigia do Coro: os sinos devolverão a dor.")] =
            RoomStoryJournalEntry(roomNames[2][2], GuardianIntroductions[2]);
        return replacements;
    }

    internal static string MigrateLegacyStoryJournalEntry(string entry) =>
        !string.IsNullOrEmpty(entry) && LegacyStoryJournalEntries.TryGetValue(entry, out string replacement) ? replacement : entry;
}
