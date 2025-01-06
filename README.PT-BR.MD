# Demonstração de Event Sourcing com Capivaras

## Propósito

Esta demonstração apresenta uma implementação de event sourcing usando uma história fictícia sobre capivaras.
O objetivo é ilustrar como o event sourcing pode ser usado para rastrear mudanças ao longo do tempo em um sistema.
Vamos destacar as principais características, como:
1. Histórico completo de mudanças;
2. Auditoria e conformidade;
3. Escalabilidade e desempenho;
4. Reexecução e depuração;
5. Flexibilidade;
6. Integração com sistemas reativos;

## A História das Capivaras

### 1. Histórico completo de mudanças;

Cléo escreve tudo em um diário: quem chegou, quem saiu e quem trouxe comida.
Cada anotação é um evento registrado com data e hora.
Se alguém perguntar como foi a festa há 3 meses, Cléo pode contar tudo em detalhes.

### 2. Auditoria e conformidade;

Um dia, a capivara chefe quer saber quem comeu todas as cenouras.
Cléo revisa os registros e identifica os culpados (foi a Capivara Carlos!).
Sem brigas, apenas fatos.

### 3. Escalabilidade e Desempenho

CapybaraVille cresce, e Cléo começa a registrar eventos em vários cadernos organizados por setores.
Outras capivaras ajudam no processo, anotando eventos em paralelo e sincronizando tudo no final do dia.

### 4. Reexecução e Depuração

Durante a feira de frutas, ocorre um problema: o estoque desaparece!
Cléo abre os registros e reexecuta os eventos para descobrir o que deu errado.
Foi um erro de contagem no evento "Entrega de Melancias".

### 5. Flexibilidade para Novos Requisitos

A vila decide começar a registrar a quantidade de frutas, algo que não era feito antes.
Cléo não altera os registros antigos—ela apenas adiciona eventos futuros com essa nova informação.

### 6. Integração com Sistemas Reativos

Agora a vila quer alertas automáticos quando um evento importante acontecer—como a chegada de um caminhão de cenouras.
Cléo adiciona um sino que toca sempre que ela anota algo relevante.

## Executando a Aplicação

Para executar a aplicação, siga estes passos:

```sh
dotnet build
dotnet run --project CapybaraVille.AppHost
```

## Dependências

Esta é uma aplicação .NET Aspire. Você vai precisar de:
* [.NET SDK 9+](https://dotnet.microsoft.com/download)
* Um [runtime de contêiner compatível com Aspire](https://learn.microsoft.com/dotnet/aspire/fundamentals/setup-tooling#container-runtime)