# Scadryk

Scadryk é um RPG de ação 2D em fantasia sombria, feito em C# com .NET 8 e Windows Forms. O desenho usa GDI+; o projeto não depende de Godot nem de outro motor de jogo. Diablo é uma referência de ritmo, combate e progressão. O mundo, os nomes e os itens de Scadryk são próprios. A meta de longo prazo é alcançar uma aventura de ação e loot 2D rica e rejogável, construída em etapas e sem copiar a história ou os personagens de outros jogos.

O protótipo tem sete salas de combate e a praça segura de Véspera, com inimigos de funções distintas, três chefes com ataques em fases, loot, equipamentos, níveis e talentos. A campanha inclui o prólogo, inscrições exploráveis, as missões de Sileth e Edda, a expedição ao Arquivo Norte e o início da travessia pelo Rio Cego. O Selo da Vigília revela que o Lume escolhe nomes para consumir; o Sino de Nara mostra que ela cruzou o rio por vontade própria. A descida ao farol continua em desenvolvimento.

## História de Scadryk

**Premissa original:** a cidade de Véspera foi erguida sobre o Rio Cego, cujas pedras guardam as últimas palavras dos mortos. Por gerações, os habitantes usaram fragmentos desse mineral para alimentar o Lume de Vigília, um farol subterrâneo que protege a região de uma presença conhecida como o Silêncio de Baixo.

Na noite em que todos os sinos de Véspera se calaram, pessoas começaram a esquecer os nomes de quem amavam. Das lembranças apagadas surgiram ecos violentos, presos a lugares e gestos que já não compreendem. O protagonista é um Escutador, alguém capaz de ouvir as memórias presas na pedra. Ao descer à cripta da cidade, busca um fragmento da voz de sua irmã desaparecida e encontra o Guardião sem Nome, último protetor de um segredo que nem ele recorda por inteiro.

As pistas apontam para a Curadora Sileth, responsável pelos arquivos de Véspera. Ela apaga nomes de propósito: acredita que manter a história incompleta é o preço necessário para alimentar o farol e conter o Silêncio. A jornada colocará o protagonista diante de uma escolha difícil: preservar a proteção construída sobre esquecimentos ou devolver à cidade a verdade e enfrentar o que ela mantinha afastado.

O prólogo apresenta Véspera, o Escutador e a busca por Nara. Ao derrotar o Guardião e recolher o fragmento do relicário, o jogador ouve uma lembrança dela e segue até a praça. Fale com Sileth, investigue o Livro das Ausências e entre no Arquivo Norte pelo portão ao norte. Atravesse a galeria, derrote o Escriba Cego no cofre e leve o Selo da Vigília de volta a Sileth. Depois, desça pelo portão ao sul da praça, atravesse o leito do Rio Cego e enfrente o Barqueiro Oco na Capela Afogada. Leve o Sino de Nara a Edda para concluir essa etapa. A descida ao farol e as escolhas sobre o Lume ainda serão implementadas conforme o roadmap.

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

Para verificar o prólogo, as inscrições, o Arquivo Norte, o Rio Cego, os chefes, os itens de história, as missões, o save, movimento, avisos de ataque, esquiva e progressão:

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
| R ou clique direito | Redemoinho |
| Q | Usar poção |
| E | Escutar inscrições, falar/investigar em Véspera, abrir passagem ou interagir com o baú |
| I | Abrir ou fechar inventário |
| 1–3 no inventário | Equipar arma |
| 4–5 no inventário | Alternar armadura e relíquia |
| 1–3 ao subir de nível | Escolher talento |
| Tab | Trocar alvo |
| Enter, Espaço ou clique na introdução | Avançar o prólogo e a lembrança |
| F5 | Salvar depois de limpar a sala e recolher o loot |
| C na tela inicial | Carregar o save |
| Enter | Começar ou reiniciar |

Os ataques corpo a corpo comuns agora mostram a área de impacto antes do golpe. Saia do círculo para evitar dano. O movimento também desliza pela quina dos obstáculos quando uma direção fica bloqueada.

Depois de escolher a recompensa do baú da cripta, explore a praça de Véspera. Fale com Sileth, leia o Livro das Ausências e volte para conversar com ela. O portão ao norte leva ao Arquivo Norte: limpe a galeria e o cofre, recolha o selo e retorne a Sileth. Quando ela liberar a trilha, use o portão ao sul para descer ao rio. Na Capela Afogada, vença o Barqueiro Oco, recolha o sino e mostre-o a Edda. A passagem à esquerda da praça retorna à cripta. Use **F5** em uma área segura para salvar seu progresso.

## Salvamento

O save JSON fica em `%LOCALAPPDATA%\Scadryk\save.json` e guarda inscrições, etapas das missões, o Selo da Vigília e o Sino de Nara. Na tela inicial, pressione **C** para carregar. Se não houver um save novo, o jogo também procura o ConfigFile da versão anterior em `%APPDATA%\Godot\app_userdata\Scadryk RPG\scadryk-save.cfg` e migra os dados encontrados para o formato JSON.

## Estrutura

- `Program.cs`: ponto de entrada do aplicativo Windows.
- `Game.cs`: janela, desenho 2D, combate, inimigos, loot, progressão e save.
- `Scadryk.csproj`: projeto C#/.NET 8 Windows Forms.
- `STORY.md`: universo, personagens e arco narrativo original planejado.
- `ROADMAP.md`: meta de longo prazo e etapas de desenvolvimento.
- `.github/workflows/validate.yml`: compilação e smoke test no GitHub Actions.

## Limites atuais

Este é um protótipo com sete salas de combate e arte procedural desenhada em formas 2D. Ainda faltam a descida ao Lume, uma campanha longa, mais regiões, chefes e builds, arte e áudio originais, animações, balanceamento amplo e testes manuais em diferentes resoluções. Não há medição de desempenho em máquinas variadas.







