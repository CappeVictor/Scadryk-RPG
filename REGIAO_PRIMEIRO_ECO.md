# Margens do Primeiro Eco

Proposta de expansão para a primeira jornada a partir de Véspera. A região deve ser uma área contínua explorável, com câmera acompanhando o personagem e caminhos que possam ser percorridos nos dois sentidos. Os nomes e a história são próprios de Scadryk.

## Estado atual

O código já conecta Véspera às Margens contínuas e aos dois interiores. Ivara orienta a missão; a gruta contém um registro opcional; o Vigia protege o mecanismo das comportas; Edda recebe o relato. O save guarda a missão e as descobertas, retomando em Véspera quando for feito na região. O mapa regional indica posição, destino e saque deixado durante a sessão.

A navegação, a leitura visual, o ritmo dos encontros e o equilíbrio das recompensas ainda precisam ser observados em uma partida. O Vigia tem uma silhueta desenhada em código e ataques próprios; sua arte e animações ainda precisam de refinamento. A região é uma primeira etapa da expansão e não representa todo o escopo de um ARPG da escala desejada.

## Percurso

```text
 Véspera ── Estrada do Farol ── Encruzilhada dos Marcos ── Canal Partido ── Casa das Comportas
                                      │                    │
                               Bosque Salgado        Margem Alagada
                                      │                    │
                              Gruta das Raízes       Passagem de retorno
```

A estrada começa junto à praça de Véspera. Marcos e lanternas orientam o caminho principal sem impedir que o jogador explore. O Bosque Salgado leva a uma gruta opcional; a Margem Alagada oferece um caminho mais perigoso que reencontra o canal. A Casa das Comportas é o destino da missão principal. Cada lugar precisa de silhueta, paleta e pontos de referência próprios para que o jogador reconheça onde está.

## História e encontros

Edda pede que o Escutador descubra por que os marcos da estrada deixaram de responder aos sinos. Na encruzilhada, a cartógrafa Ivara explica que as marcas estão sendo arrancadas e indica duas rotas. Ela permanece ali como ponto de orientação, com falas diferentes antes e depois da missão.

Na Casa das Comportas, o jogador encontra um mecanismo que retém as vozes usadas para guiar viajantes. O guardião da comporta tenta mantê-lo fechado. Após o confronto, libertar as vozes restabelece os marcos e permite informar Edda em Véspera. A gruta abriga um registro sobre os antigos construtores e uma recompensa opcional; não é necessária para concluir a missão principal.

## Requisitos de implementação

- Integrar a saída de Véspera sem renumerar as áreas existentes nem invalidar saves antigos.
- Criar limites e obstáculos em coordenadas do mundo, câmera com limites e conversão correta da mira do mouse.
- Distribuir inimigos em grupos ao longo do percurso; não exigir que o jogador limpe toda a região para caminhar entre seus pontos.
- Permitir retorno entre Véspera, região, gruta e casa; ao voltar de interiores, posicionar o jogador junto à entrada correspondente.
- Registrar missão, descoberta, recompensa e estado do guardião no save, com valores compatíveis com saves antigos.
- Mostrar objetivo, nomes de lugares e saídas descobertas no mapa e no diário. Dar indicações legíveis junto aos cruzamentos.
- Construir a área com passagens navegáveis e colisões coerentes com os elementos visuais. Conferir combate, interações, retorno, morte e carregamento em cada trecho.

## Referências de estrutura

O primeiro ato de Diablo II usa uma cidade central, estradas com ramificações, áreas opcionais e missões ligadas a personagens. Essa organização é uma referência para a variedade do percurso; a geografia, as missões e os personagens acima são originais.
