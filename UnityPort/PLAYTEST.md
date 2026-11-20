# Ficha de sessão de teste — Scadryk

Use este roteiro em uma build identificada por commit. Não ensine objetivos, segredos ou estratégias durante a primeira tentativa; apresente apenas os controles. Peça autorização antes de registrar comentários e use um apelido ou código, sem dados pessoais. O jogo grava duração, mortes, ataques difíceis de perceber e usos de poderes por sala em `scadryk-sessions-v5.csv` dentro de `Application.persistentDataPath`. O mesmo `session_id` agrupa as salas gravadas em uma execução; ele também aparece no log como `SCADRYK_SESSION`.

## Participante e build

- Código/apelido: `____________________`
- Experiência declarada: `iniciante / intermediária / experiente`
- Familiaridade com jogos de ação top-down/ARPG: `____________________`
- Build/commit: `____________________`
- Data, `session_id` do CSV/log e dispositivo de entrada: `____________________`
- Observador: `____________________`

Convide ao menos duas pessoas por nível de experiência (seis sessões) antes de comparar padrões. Cada pessoa deve começar sem save anterior e jogar até concluir ou decidir parar. Não combine métricas de builds diferentes como se fossem a mesma versão.

## Registro por sala

Depois da sessão, filtre o CSV pelo `session_id` e copie os valores automáticos abaixo. O jogo preserva mortes e usos de poderes ao reiniciar a sala, então os valores da linha já cobrem as tentativas daquela sala na campanha. Se a pessoa repetir a campanha em outra execução, o ID muda; mantenha cada tentativa separada. Anote cada marca F8 com uma descrição do ataque e do que o jogador percebeu.

| Fase / sala | Segundos ativos | Mortes | F8 / sinais pouco claros | Crescente | Estouro | Rompante | Objetivo compreendido sem ajuda? | Comentário curto |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | --- | --- |
| Bosque alagado 1 | | | | | | | | |
| Bosque alagado 2 | | | | | | | | |
| Bosque alagado 3 / guardião | | | | | | | | |
| Bacia de sal 1 | | | | | | | | |
| Bacia de sal 2 | | | | | | | | |
| Bacia de sal 3 / guardião | | | | | | | | |
| Costa das badaladas 1 | | | | | | | | |
| Costa das badaladas 2 | | | | | | | | |
| Costa das badaladas 3 / guardião e Arauto | | | | | | | | |

## Perguntas após a sessão

Respostas de 1 (discordo) a 5 (concordo), com observações:

- Eu sabia para onde ir na maior parte do tempo: `1  2  3  4  5` — nota: `____________________`
- Consegui reconhecer os avisos antes de receber dano: `1  2  3  4  5` — nota: `____________________`
- A esquiva e os poderes responderam como eu esperava: `1  2  3  4  5` — nota: `____________________`
- As mortes pareceram evitáveis quando aconteceram: `1  2  3  4  5` — nota: `____________________`
- Qual sala foi mais interessante e por quê? `________________________________________`
- Qual sala foi mais confusa ou repetitiva? `________________________________________`
- Que ataque causou dano sem você entender o motivo? `________________________________________`
- Que poder você usou menos e por quê? `________________________________________`
- O que entendeu sobre Nara, Nima e o silêncio dos sinos? `________________________________________`
- Onde parou, se não terminou, e o que tentou fazer? `________________________________________`

## Revisão e ação

1. Reúna os seis registros e compare primeiro cada sala entre os três níveis de experiência.
2. Investigue toda marca F8 com a gravação ou descrição do observador. Corrija qualquer dano reproduzível sem aviso legível e qualquer bloqueio que impeça concluir a sala.
3. Se duas pessoas ou mais ficarem presas no mesmo objetivo, ajuste a pista ou o mapa antes de aumentar a dificuldade.
4. Se um poder ficar sem uso por um grupo, procure a causa na sala, na recarga ou na clareza do efeito; não aumente o dano automaticamente.
5. Registre a mudança, a build seguinte e repita a sala afetada. Não preencha esta ficha com resultados hipotéticos: os registros começam vazios até que pessoas joguem a build.
