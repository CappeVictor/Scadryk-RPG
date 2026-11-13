# Scadryk

Scadryk é um RPG de ação 2D em fantasia sombria, feito em C# com .NET 8 e Windows Forms. O desenho usa GDI+; o projeto não depende de Godot nem de outro motor de jogo. Diablo é uma referência de ritmo, combate e progressão. O mundo, os nomes e os itens de Scadryk são próprios. A meta de longo prazo é alcançar uma aventura de ação e loot 2D rica e rejogável, construída em etapas e sem copiar a história ou os personagens de outros jogos.

O protótipo tem dezenove salas de combate e duas áreas seguras — a praça de Véspera e o Refúgio do Veio Calmo —, com inimigos de funções distintas, nove chefes com ataques em fases, loot, equipamentos, níveis e talentos. A campanha jogável inclui o prólogo, inscrições exploráveis, missões de Sileth e Edda, o Arquivo Norte, o Rio Cego, a Câmara do Lume, o Reservatório das Vozes, a Costa das Vozes Tardias, a Bacia do Primeiro Nome, o Vau dos Nomes Escolhidos e a trilha até o refúgio. A memória da primeira Escutadora, Ilyra, revela que o Lume nasceu como abrigo para lembranças e depois foi transformado em combustível. Um mapa escrito por Nara revela que ela ajudou a criar um refúgio a oeste onde cada pessoa escolhe o próprio nome e partiu novamente com quem queria continuar a viagem; sua localização atual permanece desconhecida. As decisões de Véspera afetam a praça, a progressão e a força de chefes.

## História de Scadryk

**Premissa original:** a cidade de Véspera foi erguida sobre o Rio Cego, cujas pedras guardam as últimas palavras dos mortos. Por gerações, os habitantes usaram fragmentos desse mineral para alimentar o Lume de Vigília, um farol subterrâneo que protege a região de uma presença conhecida como o Silêncio de Baixo.

Na noite em que todos os sinos de Véspera se calaram, pessoas começaram a esquecer os nomes de quem amavam. Das lembranças apagadas surgiram ecos violentos, presos a lugares e gestos que já não compreendem. O protagonista é um Escutador, alguém capaz de ouvir as memórias presas na pedra. Ao descer à cripta da cidade, busca um fragmento da voz de sua irmã desaparecida e encontra o Guardião sem Nome, último protetor de um segredo que nem ele recorda por inteiro.

As pistas apontam para a Curadora Sileth, responsável pelos arquivos de Véspera. Ela apaga nomes de propósito: acredita que manter a história incompleta é o preço necessário para alimentar o farol e conter o Silêncio. A jornada colocará o protagonista diante de uma escolha difícil: preservar a proteção construída sobre esquecimentos ou devolver à cidade a verdade e enfrentar o que ela mantinha afastado.

O prólogo apresenta Véspera, o Escutador e a busca por Nara. Ao derrotar o Guardião e recolher o fragmento do relicário, siga até a praça. Fale com Sileth, investigue o Livro das Ausências e entre no Arquivo Norte pelo portão ao norte. Derrote o Escriba Cego e leve o selo a Sileth. Depois, desça pelo portão ao sul, vença o Barqueiro Oco e mostre o Sino de Nara a Edda. Volte à mesma passagem para descer pela escada de cinzas. Atravesse a galeria, enfrente o Guardião das Cinzas na Câmara do Lume e leve o prisma de volta a Sileth. Retorne à câmara e procure a passagem no chão abaixo do prisma. Cruze a Margem sem Reflexo, derrote O Nome Vazio e recolha a lembrança. Volte a Véspera pelo portão à direita e conte a Sileth o que Nara deixou. Depois, fale com ela novamente para escolher o futuro imediato da cidade. Escolha **1** para devolver os nomes à cidade ou **2** para manter o Lume; **3** adia a decisão. Cada caminho altera a praça, concede um bônus próprio e fica registrado no save. Volte ao Reservatório e siga pela saída leste até a Costa das Vozes Tardias. Atravesse a costa, derrote a Tecelã no Claustro e leve o Coro dos Nomes Consumidos a Sileth. Após receber a recompensa, retorne ao Claustro e interaja com a escadaria no centro sul. Cruze a Bacia do Primeiro Nome, derrote a Primeira Vigília e leve a memória de Ilyra de volta a Sileth. A memória aponta para a trilha oeste, mas ainda não revela o destino de Nara.

O universo, os personagens e o arco planejado estão detalhados em [STORY.md](STORY.md).

## Requisitos

- Windows 10 ou 11.
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
- [Visual Studio Code](https://code.visualstudio.com/).

Não é preciso instalar Godot ou um motor de jogo. A extensão C# Dev Kit do VS Code é opcional; o terminal do .NET basta para compilar e iniciar.

## Abrir e iniciar no VS Code

1. Instale o .NET 8 SDK e o VS Code.
2. Baixe o projeto pelo botão **Code → Download ZIP** no GitHub ou clone o repositório.
3. Extraia o ZIP, se necessário, e abra a pasta do projeto no VS Code (**File → Open Folder**).
4. Abra o terminal integrado (**Terminal → New Terminal**).
5. Na pasta que contém `Scadryk.csproj`, execute:

   ```powershell
   dotnet run
   ```

Para compilar uma versão de produção:

```powershell
dotnet build --configuration Release
```

Para verificar o prólogo, as inscrições, as nove regiões, as áreas seguras, os chefes, os itens de história, as missões, o save, movimento, avisos de ataque, esquiva e progressão:

```powershell
dotnet run -- --self-test
```

O executável compilado aparece em `bin/Release/net8.0-windows/`. Para criar uma pasta executável independente de uma instalação do .NET no computador de destino:

```powershell
dotnet publish --configuration Release --runtime win-x64 --self-contained true
```

## Controles

| Tecla ou ação | Função |
| --- | --- |
| WASD ou setas | Mover |
| Espaço, J ou clique esquerdo | Atacar |
| Shift ou K | Esquivar |
| R ou clique direito | Redemoinho, dano em área ao redor do personagem |
| F | Estilhaço de Voz, projétil que atravessa inimigos e para em paredes; mira na direção do cursor ou do alvo marcado |
| Q | Usar poção |
| E | Escutar inscrições, falar/investigar em Véspera, abrir passagem ou interagir com o baú |
| I | Abrir ou fechar inventário |
| 1–3 no inventário | Equipar arma |
| 4–5 no inventário | Alternar armadura e relíquia |
| 6 no inventário | Alternar o talismã do espaço I |
| 7 no inventário | Alternar o talismã do espaço II |
| 1–3 ao subir de nível | Escolher talento ou responder quando Sileth apresentar uma decisão |
| Tab | Trocar alvo |
| Enter, Espaço ou clique na introdução | Avançar o prólogo e a lembrança |
| F5 | Salvar depois de limpar a sala e recolher o loot |
| C na tela inicial | Carregar o save |
| Enter | Começar ou reiniciar |

Os ataques corpo a corpo comuns agora mostram a área de impacto antes do golpe. Saia do círculo para evitar dano. O movimento também desliza pela quina dos obstáculos quando uma direção fica bloqueada. O Estilhaço de Voz oferece uma opção de ataque à distância que atravessa uma linha de inimigos, para na geometria da sala e recebe bônus do Vidro de Vigília; Domínio reduz a recarga das duas técnicas.

Elites deixam o Nó de Brasa; o Escriba Cego deixa o Vidro de Vigília; o Barqueiro Oco deixa o Fio de Travessia. Ilyra deixa a Marca da Vigília, que causa mais dano a chefes e menos dano a inimigos comuns. A Cartógrafa do Vão deixa o Fio da Margem, que amplia o alcance do Estilhaço de Voz à custa de uma recarga maior. Combine-o com o Vidro para fortalecer o Estilhaço e reduzir essa penalidade. Equipe dois dos cinco talismãs pelo inventário (**I**; teclas **6** e **7**). O Nó fortalece o Redemoinho; o Fio de Travessia prolonga a esquiva; o Vidro melhora os críticos e o Estilhaço. As sintonias, a coleção e os dois espaços equipados são salvos. Inimigos comuns também podem deixar os três talismãs encontrados nas primeiras regiões.

Depois de escolher a recompensa do baú da cripta, explore a praça de Véspera. Fale com Sileth, leia o Livro das Ausências e volte para conversar com ela. Limpe a galeria e o cofre do Arquivo Norte, leve o selo a Sileth, derrote o Barqueiro Oco no Rio Cego e mostre o sino a Edda. Volte à passagem sul para alcançar o Lume. Derrote o Guardião das Cinzas e leve o prisma a Sileth. Depois, reentre na Câmara do Lume e procure a passagem abaixo do prisma. Atravesse a Margem sem Reflexo, vença O Nome Vazio e retorne a Sileth com a lembrança de Nara. Após as decisões de Véspera e do testemunho do Coro, explore o Claustro, desça à Bacia e leve a memória de Ilyra de volta a Sileth. Converse com Edda para iniciar a travessia oeste; vença a Cartógrafa no Pátio das Testemunhas e recolha o mapa de Nara. Volte a Edda para chegar ao Refúgio do Veio Calmo e fale com Aven. Cruze a Trilha dos Luminários, derrote o Escrivão de Contrato e recolha o registro. Devolva-o à comunidade para receber provisões e abrir a continuação da trilha. A passagem à esquerda da praça retorna à cripta. Use **F5** em uma área segura para salvar seu progresso.

## Salvamento

O save JSON fica em `%LOCALAPPDATA%\Scadryk\save.json` e guarda inscrições, etapas das missões, o Selo da Vigília, o Sino de Nara, o Prisma do Lume, os talismãs encontrados, os dois espaços equipados, a decisão de Véspera, o Coro dos Nomes Consumidos, a memória de Ilyra, o mapa de Nara, a etapa do Refúgio do Veio Calmo, o registro comunitário e as câmaras já limpas. Saves anteriores sem os campos mais recentes carregam com valores vazios. Na tela inicial, pressione **C** para carregar. Se não houver um save novo, o jogo também procura o ConfigFile da versão anterior em `%APPDATA%\Godot\app_userdata\Scadryk RPG\scadryk-save.cfg` e migra os dados encontrados para o formato JSON.

## Estrutura

- `Program.cs`: ponto de entrada do aplicativo Windows.
- `Game.cs`: janela, desenho 2D, combate, inimigos, loot, progressão e save.
- `Scadryk.csproj`: projeto C#/.NET 8 Windows Forms.
- `STORY.md`: universo, personagens e arco narrativo original planejado.
- `ROADMAP.md`: meta de longo prazo e etapas de desenvolvimento.
- `.github/workflows/validate.yml`: compilação e smoke test no GitHub Actions.

## Limites atuais

Este é um protótipo com dezenove salas de combate e arte procedural desenhada em formas 2D. Ainda faltam seguir além da próxima curva, conhecer a vontade atual de Nara, as escolhas e os finais da campanha, mais regiões, raridades e builds, arte e áudio originais, animações, balanceamento amplo e testes manuais em diferentes resoluções. Não há medição de desempenho em máquinas variadas.











