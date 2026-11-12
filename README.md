# Scadryk

Scadryk é um RPG de ação 2D em fantasia sombria, feito em C# com .NET 8 e Windows Forms. O desenho usa GDI+; o projeto não depende de Godot nem de outro motor de jogo. Diablo é uma referência de ritmo, combate e progressão. O mundo, os nomes e os itens de Scadryk são próprios.

O jogo atual é um protótipo curto com três salas, inimigos de funções distintas, um elite com afixação, um Guardião com duas fases, loot, equipamentos, níveis e talentos. A base está preparada para crescer por etapas.

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

Para executar as verificações automatizadas incluídas:

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
| E | Abrir passagem ou interagir com o baú |
| I | Abrir ou fechar inventário |
| 1–3 no inventário | Equipar arma |
| 4–5 no inventário | Alternar armadura e relíquia |
| 1–3 ao subir de nível | Escolher talento |
| Tab | Trocar alvo |
| F5 | Salvar depois de limpar a sala e recolher o loot |
| C na tela inicial | Carregar o save |
| Enter | Começar ou reiniciar |

Os ataques corpo a corpo comuns agora mostram a área de impacto antes do golpe. Saia do círculo para evitar dano. O movimento também desliza pela quina dos obstáculos quando uma direção fica bloqueada.

## Salvamento

O save JSON fica em `%LOCALAPPDATA%\Scadryk\save.json`. Na tela inicial, pressione **C** para carregar. Se não houver um save novo, o jogo também procura o ConfigFile da versão anterior em `%APPDATA%\Godot\app_userdata\Scadryk RPG\scadryk-save.cfg` e migra os dados encontrados para o formato JSON.

## Estrutura

- `Program.cs`: ponto de entrada do aplicativo Windows.
- `Game.cs`: janela, desenho 2D, combate, inimigos, loot, progressão e save.
- `Scadryk.csproj`: projeto C#/.NET 8 Windows Forms.
- `.github/workflows/validate.yml`: compilação e smoke test no GitHub Actions.

## Limites atuais

Este é um protótipo com três salas e arte procedural desenhada em formas 2D. Ainda faltam campanha longa, mais chefes e builds, arte e áudio originais, animações, balanceamento amplo e testes manuais em diferentes resoluções. Não há medição de desempenho em máquinas variadas.

