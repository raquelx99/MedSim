# Arquitetura do MedSim — Mapeamento do Protótipo Atual

> Documento de mapeamento da arquitetura existente. Nenhum sistema foi removido ou alterado
> para produzir este documento — ele descreve o protótipo tal como está hoje no repositório.
> Última atualização: 2026-09-19.

## 1. Visão geral

MedSim é uma simulação clínica em VR (Unity 2022, URP, XR Interaction Toolkit / OpenXR) em que
o jogador conduz um atendimento médico completo com um paciente virtual. O protótipo já
implementa o fluxo clínico ponta a ponta:

```
Anamnese → Exame Físico → Exames Laboratoriais → (Transição) → Diagnóstico → Feedback
```

Esse fluxo é orquestrado por uma máquina de estados central (`PhaseManager`) e apoiado por
managers especializados por etapa, um sistema de pontuação central e uma camada de dados via
ScriptableObjects. A cena principal de jogo é `Assets/Scenes/Consultorio.unity`; há também
`MenuPrincipal.unity` (menu inicial) e `SampleScene.unity` (não faz parte do fluxo do jogo).

## 2. Sistemas principais identificados

| Sistema | Script(s) | Responsabilidade |
|---|---|---|
| **Orquestração de fases** | `PhaseManager.cs` | Máquina de estados (`enum Part`) que controla a ordem das etapas e aciona cada manager de fase. Singleton (`PhaseManager.Instance`). |
| **Anamnese** | `AnamnesisManager.cs` | Conduz diálogo inicial + perguntas de múltipla escolha ao paciente; registra pontuação por pergunta. |
| **Exame físico** | `PhysicalExamManager.cs`, `ExamItem.cs`, `InstrumentInteraction.cs`, `XRSocketDisplayInteraction.cs`, `XRSocketTagInteractor.cs` | Detecta uso de instrumentos via interação XR (trigger e socket) e pontua uso correto/incorreto/fora de fase. |
| **Exames laboratoriais** | `LabOrderManager.cs`, `ExamTypeSO.cs`, `PhaseExamResult.cs` | Solicitação de exames via toggles, exibição de resultados (tablet paginado ou negatoscópio para imagem). |
| **Transição / narrativa** | `TransitionManager.cs`, `PatientAnimationController.cs` | Diálogo de transição e sequência de animação do paciente (sai e volta, via `NavMeshAgent`). |
| **Diagnóstico** | `DiagnosisManager.cs` | Geração de opções de diagnóstico e confirmação com pontuação. |
| **Feedback** | `FeedbackManager.cs` | Relatório final paginado (pontuação, acertos, erros por categoria, análise clínica, dicas) e opção de reiniciar. |
| **Pontuação** | `ScoreManager.cs`, `ScoreEntry.cs` | Registro central de `ScoreEntry` por categoria/severidade e cálculo de pontuação total. |
| **Dados de conteúdo (ScriptableObjects)** | `PatientPhaseSO`, `DialogueStepSO`, `AnamnesisStepSO`, `QuestionSO`, `ExamTypeSO` | Camada de dados que define o caso clínico (perguntas, diálogos, exames, diagnósticos possíveis). |
| **Áudio** | `AudioManager.cs` | Wrapper simples sobre um único `AudioSource` para tocar falas/efeitos. |
| **Displays visuais** | `TVDisplay.cs`, `NegatoscopeDisplay.cs` | Exibição de imagens (instrumentos / exames de imagem) em painéis de UI. |
| **Menu e transição de cena** | `GameStartMenu.cs`, `SceneTransitionManager.cs`, `FadeScreen.cs` | Menu inicial, fade e carregamento de cena. |
| **Configurações XR/Jogador** | `SetOptionFromUI.cs`, `SetTurnTypeFromPlayerPref.cs` | Volume global e tipo de rotação (snap/contínua) via `PlayerPrefs`. |
| **Stub sem uso** | `UIManager.cs` | Classe vazia, sem lógica implementada. |
| **Ativos de terceiros** | `Assets/Stethoscope/*`, `Assets/SimpleHands/*`, `Assets/Lets Make a VR Game/*` | Modelos/prefabs e scripts de suporte (ex.: `SplineRope.cs`) de asset packs, usados como instrumentos/mãos, sem integração própria de lógica de jogo. |

## 3. Fluxo técnico documentado

### 3.1 Máquina de estados (`PhaseManager`)

```csharp
enum Part { Anamnese, Exam, Labs, Transition, Diagnosis }
```

- `Start()` chama `StartAnamnese()` diretamente (a primeira etapa não passa por `NextPart()`).
- Cada manager de etapa, ao concluir seu trabalho, chama `PhaseManager.Instance.FinishStep()`,
  que chama `NextPart()`, que incrementa `currentPart` e ativa a etapa seguinte via `switch`.
- A finalização usa aritmética sobre o enum (`case Part.Diagnosis + 1: FinishPhase();`).

### 3.2 Passo a passo do fluxo

1. **Anamnese** (`AnamnesisManager.Init`) — recebe `dialogueSteps` e `anamnesisSteps` do
   `PatientPhaseSO` ativo (via `PhaseManager.StartAnamnese`). Primeiro percorre os diálogos
   introdutórios, depois as perguntas de múltipla escolha (2 opções cada). Cada resposta é
   pontuada via `ScoreManager.GetPointsForAnamnesis` e registrada como `ScoreEntry`. Ao final,
   chama `PhaseManager.Instance.FinishStep()`.
2. **Exame físico** — `PhaseManager.StartExamPhase()` apenas atualiza o texto da prancheta e
   exibe o botão de confirmação; a detecção real de uso de instrumentos acontece de forma
   assíncrona via colisão/interação XR (`InstrumentInteraction.OnTriggerEnter` ou
   `XRSocketDisplayInteraction.OnSelectEntered`), que chamam
   `PhysicalExamManager.RegisterExamItem(objectID)`. O avanço de fase depende do botão da
   prancheta chamar `PhysicalExamManager.FinishPhysicalExam()` — essa ligação é feita no
   Inspector da cena, não é visível apenas lendo o código.
3. **Exames laboratoriais** — `LabOrderManager` gera toggles a partir de
   `phaseData.examResults`; `ConfirmLabs()` compara exames selecionados com
   `phaseData.correctLabTestIDs`, pontua acertos/erros/esquecimentos e chama `FinishStep()`.
4. **Transição** — `PhaseManager.StartTransitionPhase()` ativa `TransitionManager`, que roda um
   diálogo; se um `DialogueStepSO.requiresTransition` for verdadeiro, dispara
   `PatientAnimationController.StartExitAndReturnSequence()` (paciente sai e volta via
   `NavMeshAgent` + `Animator`). Ao fim da sequência, `AnimationSequenceFinished()` retoma o
   diálogo; ao terminar, `EndTransition()` chama `FinishStep()`.
5. **Diagnóstico** — `PhaseManager.StartDiagnosisPhase()` chama
   `labOrderManager.ShowResultsUI()` (permite consultar resultados dos exames pedidos) e ativa
   `DiagnosisManager`, que gera botões a partir de `phaseData.possibleDiagnosis`.
   `ConfirmDiagnosis()` compara com `correctDiagnosisIndex`, pontua e chama `FinishStep()`.
6. **Feedback** — `PhaseManager.FinishPhase()` ativa `FeedbackManager.ShowFeedback()`, que monta
   páginas (pontuação final, acertos por categoria, erros por categoria com justificativa,
   análise clínica com sugestões, dicas gerais) a partir de `ScoreManager.GetAllEntries()`.
   `RestartPhase()` recarrega a cena inteira (`SceneManager.LoadScene`), o que reseta o estado
   de todos os managers (campos de instância) sem necessidade de reset manual.

### 3.3 Fluxo pré-jogo (paralelo, não faz parte da simulação clínica)

`MenuPrincipal` → `GameStartMenu` (iniciar/opções/sobre) → `SceneTransitionManager.GoToSceneAsync`
(com `FadeScreen`) → carrega `Consultorio`. `SetOptionFromUI` / `SetTurnTypeFromPlayerPref`
configuram volume e tipo de rotação via `PlayerPrefs`, lidos por providers do XR Interaction
Toolkit.

## 4. Dependências entre módulos

- **`PhaseManager` é o hub central**: mantém referências diretas (via Inspector) a
  `ScoreManager`, `LabOrderManager`, `TransitionManager`, `DiagnosisManager`, `FeedbackManager`.
  `AnamnesisManager`, porém, é localizado via `FindObjectOfType` em vez de referência de
  Inspector — padrão de acesso inconsistente dentro do próprio orquestrador.
- **Referências duplicadas manualmente**: `ScoreManager` e `PatientPhaseSO` (`phaseData`) são
  campos públicos repetidos em `AnamnesisManager`, `PhysicalExamManager`, `LabOrderManager`,
  `DiagnosisManager` — cada um precisa ser conectado no Inspector ao mesmo asset/instância.
  Não há injeção de dependência nem fonte única (single source of truth) em código.
- **Acesso a singletons/globais heterogêneo**: `PhaseManager.Instance` (singleton estático) é
  usado por `AnamnesisManager`, `PhysicalExamManager`, `TransitionManager`; já
  `DiagnosisManager.ConfirmDiagnosis()` usa `FindObjectOfType<PhaseManager>()` em vez do
  singleton; `InstrumentInteraction`, `XRSocketDisplayInteraction` e
  `PatientAnimationController` também usam `FindObjectOfType` para alcançar
  `PhysicalExamManager`/`TransitionManager`.
- **Camada de dados (ScriptableObjects) é compartilhada por leitura**: `PatientPhaseSO` agrega
  `DialogueStepSO[]`, `AnamnesisStepSO[]` (que por sua vez referencia `QuestionSO`),
  `PhaseExamResult[]` (que referencia `ExamTypeSO`), IDs de exames/diagnósticos corretos. Hoje
  existe apenas **um** asset de fase (`Assets/Data/Phases/Patient1.asset`), ou seja, só um caso
  clínico está configurado.
- **Pontuação é desacoplada da UI**: `ScoreManager` não conhece nenhuma UI, apenas acumula
  `ScoreEntry`; é consumido por `FeedbackManager` só no fim da fase — bom ponto de design.
- **Dois caminhos paralelos de detecção de instrumento**: baseado em trigger/colisão
  (`InstrumentInteraction`) e baseado em socket XR (`XRSocketDisplayInteraction` +
  `XRSocketTagInteractor`). Ambos terminam chamando `PhysicalExamManager.RegisterExamItem`, mas
  não é possível confirmar apenas pelo código se os dois estão realmente em uso simultâneo na
  cena ou se um é resquício de uma iteração anterior.
- **Duplicação de exibição de imagem**: `TVDisplay` e `NegatoscopeDisplay` são implementações
  quase idênticas (Image + painel liga/desliga), sem uma classe base comum.
- **Nenhum mecanismo de eventos/EventBus.** Não há um único uso de `event`/`delegate` de C#, de
  `UnityEvent` customizado (fora do `onClick` de botões de UI) ou de um EventBus/Event Channel
  (padrão comum em Unity para desacoplar sistemas via ScriptableObject) em todo `Assets/Scripts`.
  Toda a comunicação entre `AnamnesisManager`, `PhysicalExamManager`, `LabOrderManager`,
  `DiagnosisManager`, `TransitionManager` e `PhaseManager` acontece por **chamada direta de
  método** (`FinishStep()`, `RegisterExamItem()`, `ConfirmLabs()` etc.), apoiada em singleton ou
  em `FindObjectOfType`. Essa ausência de uma camada de publish/subscribe é a causa raiz comum
  de vários dos outros pontos desta seção: é ela que obriga o wiring manual duplicado (cada
  manager precisa conhecer a referência concreta de quem ele avisa) e que torna o padrão de
  acesso a singleton/`FindObjectOfType` inconsistente (cada script resolve à sua maneira "como
  avisar o próximo" na ausência de um canal comum).

## 5. Pontos críticos destacados

1. **Wiring manual e duplicado no Inspector.** `ScoreManager` e `PatientPhaseSO` precisam ser
   arrastados manualmente para múltiplos componentes; um esquecimento gera bugs silenciosos
   (pontuação não registrada, dados desatualizados) sem erro em tempo de compilação.
2. **Contrato implícito entre UI da cena e código.** O botão de confirmação do exame físico
   (`pranchetaButton`) precisa estar ligado a `PhysicalExamManager.FinishPhysicalExam()` via
   Editor — não há nenhuma garantia em código; se o link quebrar, a fase trava sem erro visível.
3. **Padrão de acesso a singleton inconsistente** (`PhaseManager.Instance` vs
   `FindObjectOfType<...>()` espalhados). Funciona, mas é frágil a reffactors e mais custoso em
   performance.
4. **Máquina de estados por aritmética de enum** (`case Part.Diagnosis + 1`). Reordenar ou
   inserir uma nova etapa no meio do enum quebra o fluxo silenciosamente.
5. **Casamento por string sem validação.** `ScoreManager` casa pontuação por texto exato
   (`prompt` da pergunta, `objectID` do instrumento, `examID`, texto do diagnóstico). Renomear
   conteúdo em um ScriptableObject sem atualizar `ScoreMapping` correspondente quebra a
   pontuação silenciosamente (cai no valor default).
6. **Conteúdo clínico único.** Apenas uma fase/paciente (`Patient1.asset`) está configurada —
   ainda não há dados que comprovem que o fluxo generaliza para múltiplos casos.
7. **`AudioManager` de fonte única.** Um único `AudioSource` interrompe qualquer clipe em
   reprodução ao tocar outro (`if (source.isPlaying) source.Stop();`), o que pode cortar fala do
   NPC ou som de instrumento se disparados perto um do outro.
8. **Resultados de exame dependem de dupla configuração.** Um exame só aparece em
   `ShowResultsUI()` se estiver simultaneamente selecionado pelo jogador **e** presente em
   `phaseData.examResults`; se um autor de conteúdo esquecer de cadastrar o resultado, o exame
   é silenciosamente ignorado (`if (resultData == null) continue;`).
9. **Nenhum teste automatizado encontrado no projeto** (não há pasta/arquivos de teste), então
   qualquer regressão no fluxo só é detectada manualmente em Play Mode.
10. **`UIManager.cs` é uma classe vazia sem uso** — candidato a remoção ou implementação futura.
11. **Sistemas duplicados** (`TVDisplay`/`NegatoscopeDisplay`; dois caminhos de detecção de
    instrumento) aumentam a superfície de manutenção sem necessidade aparente.
12. **Ausência total de eventos/EventBus.** Nenhum sistema do jogo se comunica por evento — só
    por chamada direta de método, singleton ou `FindObjectOfType` (ver seção 4). Isso torna a
    ordem das etapas (`PhaseManager.Part`) e as referências entre managers fortemente acopladas:
    inserir uma nova etapa, trocar a ordem, ou permitir que dois sistemas reajam ao mesmo
    acontecimento (por exemplo, "erro registrado" disparando som **e** UI **e** log) exige editar
    várias classes concretas em vez de apenas assinar um evento existente.

## 6. O que já funciona

- Fluxo completo e sequencial das 5 etapas (anamnese → exame físico → exames → diagnóstico →
  feedback), com transição narrativa intermediária.
- Anamnese com diálogo + perguntas pontuadas.
- Exame físico com registro de instrumentos usados, penalização por instrumento essencial não
  usado e penalização por uso fora da fase correta.
- Exames laboratoriais com seleção via toggle e visualização de resultado (imagem/tablet
  paginado), incluindo caminho dedicado para exames de imagem via negatoscópio.
- Diagnóstico com geração dinâmica de opções e verificação de acerto.
- Feedback final navegável por páginas, com pontuação, acertos/erros por categoria, análise e
  dicas, e opção de reiniciar a fase.
- Sistema de pontuação central, por categoria e severidade, com fallback numérico quando não há
  mapeamento explícito.
- Fluxo de menu inicial e transição de cena com fade.

## 7. O que precisa de revisão

- Avaliar introduzir um sistema de eventos (C# `event`/`Action`, ou — mais idiomático em
  Unity — Event Channels via `ScriptableObject`) para que `PhaseManager` e os managers de cada
  etapa se comuniquem por publish/subscribe em vez de chamada direta + singleton/`FindObjectOfType`.
  Isso resolveria de uma vez boa parte do wiring manual duplicado e da inconsistência de acesso
  listados nesta seção e na seção 5 (item 12), sem exigir reescrever a lógica de cada fase.
- Consolidar o acesso a dependências compartilhadas (`ScoreManager`, `PatientPhaseSO`) em uma
  única fonte por cena, reduzindo wiring manual duplicado.
- Padronizar o acesso a managers (escolher entre singleton estático e referência de Inspector;
  evitar `FindObjectOfType` espalhado).
- Tornar explícita em código a transição de "Exame físico" → "Labs" (hoje depende de um botão
  configurado apenas no Editor).
- Revisar se os dois sistemas de detecção de instrumento (trigger vs. socket XR) ainda são
  ambos necessários, ou se um deles é legado.
- Unificar `TVDisplay` e `NegatoscopeDisplay` em um componente único reutilizável.
- Avaliar suporte a múltiplas fontes de áudio simultâneas no `AudioManager` para evitar corte de
  falas/efeitos sobrepostos.
- Definir uma validação (ainda que apenas em editor/ferramenta) que garanta que todo `examID`
  citado em `correctLabTestIDs` tenha um `PhaseExamResult` correspondente, e que todo texto de
  pontuação (`ScoreMapping.id`) corresponda a um dado real existente.
- Remover ou implementar `UIManager.cs`.
- Planejar criação de mais fases/pacientes (`PatientPhaseSO`) para validar se a arquitetura
  atual generaliza bem para múltiplos casos clínicos.
- Introduzir testes (mesmo que básicos) para o `ScoreManager` e para a máquina de estados do
  `PhaseManager`, hoje sem nenhuma cobertura automatizada.

## 8. Análise de performance (VR e build Windows) — 2026-09-19

Investigação focada em identificar causas de travamento ("stuttering"/baixo FPS). Nenhuma
alteração de código ou configuração foi feita — apenas leitura e diagnóstico. Ordenado por
impacto estimado.

**Atualização importante:** o travamento também foi confirmado na build de Windows, não só no
headset. Isso descarta explicações puramente "hardware móvel é fraco" e aponta para causas que
pesam em qualquer plataforma — reforça especialmente os itens 8.2 e 8.3 abaixo, e foi o motivo
de reverificar se algum script customizado ou de terceiros roda por frame na cena real (item
8.1 abaixo já havia sido descartado; a mesma verificação foi repetida para outros candidatos e
também deu negativo — ver 8.1.1).

### 8.1 Descartado após verificação — `SplineRope.cs` não é usado na cena de jogo

Arquivo: `Assets/Stethoscope/Scripts/SplineRope.cs` (script de terceiros do asset pack do
estetoscópio). Numa primeira leitura este script pareceu um forte suspeito, por ter
`[ExecuteInEditMode]` + `Update()` fazendo milhares de amostragens de spline por frame
(`for (float t = 0; t <= 1; t += .001f)` repetido por segmento de corda). **Essa suspeita foi
verificada e descartada**, então fica registrada aqui só para não ser levantada de novo sem
necessidade:

- O script está de fato configurado (com `spline` e ~20 `transforms`) no prefab
  `Assets/Stethoscope/Prefabs/Stethoscope.prefab`.
- Porém esse prefab **não aparece em `Assets/Scenes/Consultorio.unity`** (a cena real do jogo) —
  ele só é usado dentro da cena de demonstração do próprio asset,
  `Assets/Stethoscope/Scenes/Stethoscope.unity`, que também **não consta em
  `ProjectSettings/EditorBuildSettings.asset`** (a única cena listada lá é `SampleScene.unity`).
- O objeto que o jogador realmente usa como estetoscópio no exame físico (tag/`objectID`
  `estetoscopio`, visto em `Consultorio.unity`) é outro objeto, sem esse componente.
- Conclusão: `SplineRope` não roda durante o jogo real e **não é causa do travamento**. Ele só
  pesaria se, por engano, a cena de demo do asset fosse aberta/incluída em algum momento — vale
  como observação de organização de projeto (asset de terceiros com cena de exemplo solta
  dentro de `Assets/`), não como bug de performance.

### 8.1.1 Descartado após verificação — nenhum outro `Update()` roda na cena real

Como o travamento também aparece no Windows, foi feita uma varredura por todo `Assets/` (não só
código próprio) atrás de qualquer `Update()`/`FixedUpdate()`/`LateUpdate()` que pudesse explicar
um custo por frame comum às duas plataformas. Resultado:

- **Nenhum script do próprio projeto** (`Assets/Scripts/*.cs`) tem `Update()`, `FixedUpdate()`
  ou `LateUpdate()` — os únicos loops por frame do código do time estão dentro de corrotinas
  (`PatientAnimationController`, `LabOrderManager`), que só rodam durante uma transição pontual,
  não continuamente.
- Foram encontrados 15 arquivos de terceiros com algum desses métodos (exemplos do TextMesh Pro,
  `AnimateHandOnInput.cs` das mãos do "Lets Make a VR Game", `XRPokeFollowAffordance.cs` do XR
  Interaction Toolkit, e o já descartado `SplineRope.cs`). Verificando o GUID de cada script
  contra `Assets/Scenes/Consultorio.unity`, **nenhum deles está de fato instanciado na cena do
  jogo** — são exemplos/amostras dos pacotes que ficam soltos no projeto sem uso real.
- Conclusão: o custo por frame não vem de um loop de script "solto" rodando sem necessidade; é
  mais provável que venha do lado de renderização/GPU (item 8.2) ou de chamadas pontuais caras
  (item 8.3), não de lógica de jogo custosa rodando o tempo todo.

### 8.2 Crítico — pipeline de renderização pesado (afeta VR e a build Windows)

- `ProjectSettings/QualitySettings.asset`: a qualidade atualmente ativa (`m_CurrentQuality: 2`)
  é **"High Fidelity"**, e o asset de pipeline dela (`URP-HighFidelity.asset`) também é o
  **pipeline padrão global** (`GraphicsSettings.m_CustomRenderPipeline`) — ou seja, é o que
  efetivamente roda quando nada mais é definido explicitamente em tempo de execução (não há
  nenhuma chamada a `QualitySettings.SetQualityLevel` no código-fonte).
- Essa configuração inclui, simultaneamente:
  - **MSAA 4x** (`m_MSAA: 4`);
  - **SSAO ativado** (Screen Space Ambient Occlusion) — presente tanto no renderer
    "High Fidelity" quanto no "Balanced" (`URP-Balanced-Renderer.asset`), ou seja, mesmo a
    qualidade "intermediária" já paga o custo de SSAO;
  - **Sombras em resolução 4096×4096** para luz principal e para luzes adicionais
    (`m_MainLightShadowmapResolution: 4096`, `m_AdditionalLightsShadowmapResolution: 4096`);
  - **4 cascades de sombra** (`m_ShadowCascadeCount: 4`) com distância de sombra de 150 unidades;
  - **Soft shadows em qualidade alta** (`m_SoftShadowQuality: 2`) e cookies de luz em 4096;
  - Reflection probe blending/box projection ativados.
- **As 3 luzes de `Consultorio.unity` estão todas configuradas como "Realtime"**
  (`m_Lightmapping: 4` nas três, nenhuma Baked/Mixed) e o modo de GI é "On Demand"
  (`m_GIWorkflowMode: 1`) — ou seja, não há nenhum lightmap pré-calculado sendo aproveitado;
  toda a iluminação e todas as sombras dessas 3 luzes são recalculadas em tempo real, a cada
  frame, para qualquer objeto afetado por elas. Isso soma diretamente com os mapas de sombra em
  4096px e as 4 cascades: é trabalho de GPU pago em todo frame, em qualquer plataforma — não é
  um problema exclusivo de hardware móvel, o que explica o travamento também na build Windows.
- (Verificado e descartado: não há nenhum `Volume` de pós-processamento em `Consultorio.unity`,
  então efeitos como Bloom/Depth of Field/Motion Blur não são um fator aqui.)
- SSAO, MSAA e mapas de sombra em 4K são combinações típicas de jogos PC/console — mesmo numa
  build Windows, se a máquina de teste não tiver uma GPU dedicada forte (notebook, GPU
  integrada), esse conjunto de configurações é suficiente para derrubar o FPS sozinho; em uma
  GPU móvel de headset standalone (classe Snapdragon XR2) o efeito é ainda mais severo.
- A tabela `m_PerPlatformDefaultQuality` já define `Android: 1` (Balanced) e `Standalone: 2`
  (High Fidelity) — então, se o teste "no óculos" é feito via Quest Link/Air Link a partir do
  Editor (plataforma ativa = Standalone), o headset herda a qualidade **mais pesada** do
  projeto, mesmo que o build final para Android usasse a "Balanced". Isso pode explicar por que
  o travamento aparece especialmente ao testar direto do PC com o óculos conectado.

### 8.3 Alto — `FindObjectOfType` chamado dentro de callbacks de interação (hot path de XR)

- `InstrumentInteraction.OnTriggerEnter()` (`Assets/Scripts/InstrumentInteraction.cs:25`) e
  `XRSocketDisplayInteraction.OnSelectEntered()` (`Assets/Scripts/XRSocketDisplayInteraction.cs:34`)
  chamam `FindObjectOfType<PhysicalExamManager>()` **a cada colisão/seleção**, em vez de guardar
  uma referência em cache.
- `FindObjectOfType` varre os objetos carregados da cena e é uma das chamadas mais caras da API
  do Unity para se usar em um caminho executado com frequência. Como a detecção de instrumentos
  é justamente o tipo de interação que pode dispersar múltiplos eventos de trigger/seleção em
  sequência (o jogador manipulando o instrumento na mão), esses picos de custo tendem a
  acontecer bem no momento em que o jogador está se movendo/manipulando objetos em VR — o pior
  momento possível para um frame spike, pois pode gerar desconforto (motion sickness).
- O mesmo padrão aparece em `PatientAnimationController.FullTripRoutine()` (fim da corrotina) e
  em `DiagnosisManager.ConfirmDiagnosis()`, embora com menor impacto por serem chamados só uma
  vez por fase.

### 8.4 Médio — texturas de resultado de exame grandes e sem override para Android

- `Assets/UIAssets/ImagensExames/MAPAPag2.png` (1,3 MB), `MAPAPag4.png` (1,2 MB),
  `Hemograma.png` (820 KB), `MAPAPag3.png` (770 KB), `MAPAPag1.png` (673 KB) — importadas como
  Sprite com `maxTextureSize: 2048`, mas **sem nenhum override específico para a plataforma
  Android** nas configurações de importação (só há overrides para `DefaultTexturePlatform`,
  `Standalone` e `Server`). Em builds para Android/Quest, essas texturas usam a compressão
  automática padrão, o que é subótimo comparado a definir explicitamente ASTC e um tamanho
  máximo menor (essas imagens são exibidas em um "tablet" pequeno na cena, não precisam de 2048px).
- Isso pesa mais em uso de memória/VRAM e tempo de carregamento do que em FPS constante, mas
  contribui para picos de streaming de textura, especialmente ao abrir a tela de resultados de
  exames laboratoriais.

### 8.5 Observações de menor impacto

- `AudioManager` usa uma única `AudioSource` compartilhada; não afeta FPS diretamente, mas pode
  mascarar sobreposição de sons (já citado na seção 5).
- `LabOrderManager.ShowResultsUI()` destrói e recria botões de resultado toda vez que a tela de
  diagnóstico é aberta — `Instantiate`/`Destroy` geram lixo de coleta (GC) e pequenos picos, mas
  o volume é baixo (poucos exames por fase) e não deve ser a causa principal do travamento.

### 8.6 Recomendações (para validar antes de aplicar)

1. Trocar a qualidade padrão do projeto (`m_CurrentQuality`/pipeline global) de "High Fidelity"
   para "Balanced" ou "Performant", ou ajustar diretamente `URP-HighFidelity.asset`: desligar
   SSAO, reduzir/desligar MSAA, baixar a resolução dos shadow maps (4096 → 1024 ou menos) e o
   número de cascades (4 → 1-2). Isso deve reduzir o travamento tanto no Windows quanto no
   Quest, já que o custo identificado é de GPU/renderização, não específico de plataforma.
2. Fazer o bake (mixed/baked) das 3 luzes de `Consultorio.unity` sempre que a geometria for
   estática, em vez de deixá-las 100% Realtime — evita recalcular iluminação/sombra por frame
   para objetos que não se movem.
3. Cachear as referências de `PhysicalExamManager`/`TransitionManager`/`PhaseManager` (Inspector
   ou `Awake()`) em vez de `FindObjectOfType` dentro de callbacks de interação.
4. Adicionar overrides de importação de textura específicos para Android (formato ASTC, tamanho
   máximo menor) nas imagens de resultado de exame — relevante para memória/tempo de carregamento
   no build do Quest especificamente.
5. Confirmar se a ausência de `Consultorio.unity`/`MenuPrincipal.unity` em
   `EditorBuildSettings.asset` (só `SampleScene.unity` está listada — ver seção 8.1) é
   intencional; se builds estão sendo geradas a partir dessa lista, vale revisar antes de testar.

## 9. Escopo desta etapa

As seções 1 a 8 são o **mapeamento e diagnóstico** originais (2026-09-19): nenhum sistema foi
removido, refatorado ou alterado para produzi-las. A seção 10, abaixo, documenta uma etapa
posterior (2026-10-04) em que parte das recomendações passou a ser **implementada de fato** —
a partir daqui o relatório deixa de ser só diagnóstico.

## 10. Mudanças implementadas (2026-10-04)

Implementação das recomendações de performance (seção 8.6) e de parte dos itens de arquitetura
(seção 7), em ordem de risco (mais seguro primeiro). O Editor estava fechado durante as edições.

### 10.1 Performance

1. **Qualidade de renderização trocada de "High Fidelity" para "Performant"**
   (`ProjectSettings/QualitySettings.asset`, `GraphicsSettings.asset`) — zero SSAO, zero MSAA,
   sombra só da luz principal em 1024px, 1 cascade. Aplicado tanto para Standalone (Editor/Quest
   Link) quanto para Android, cobrindo os dois cenários de travamento relatados.
2. **Luzes de `Consultorio.unity` marcadas como "Mixed"** em vez de 100% Realtime. Isso sozinho
   não economiza nada ainda — falta rodar **Window → Rendering → Lighting → Generate Lighting**
   no Editor para gerar o lightmap de fato. Ação manual pendente.
3. **`FindObjectOfType` removido dos callbacks de interação XR** (`InstrumentInteraction.cs`,
   `XRSocketDisplayInteraction.cs`) e dos demais pontos que ainda usavam (`PatientAnimationController.cs`,
   `PhaseManager.cs`) — trocado por referência cacheada em `Awake()` com fallback, sem mudar
   comportamento.
4. **`DiagnosisManager.ConfirmDiagnosis()` passou a usar `PhaseManager.Instance`** em vez de
   `FindObjectOfType<PhaseManager>()`, resolvendo também o ponto crítico #3 (padrão de acesso a
   singleton inconsistente).
5. **Overrides de textura Android** (`maxTextureSize: 1024`) adicionados nas 5 imagens de
   resultado de exame (`MAPAPag1-4.png`, `Hemograma.png`).
6. **`EditorBuildSettings.asset` corrigido**: agora lista `MenuPrincipal.unity` (índice 0) e
   `Consultorio.unity` (índice 1), consistente com `GoToSceneAsync(1)` no código; `SampleScene.unity`
   foi removida da lista por não fazer parte do fluxo do jogo.

### 10.2 Arquitetura

1. **`UIManager.cs` removido** — classe vazia, zero referências em cenas/prefabs, confirmado
   antes de apagar.
2. **`TVDisplay` e `NegatoscopeDisplay` unificados**: ambos agora herdam de uma nova classe base
   `ImagePanelDisplay.cs` com a lógica compartilhada. Os dois scripts originais foram mantidos
   (mesmo nome de arquivo e GUID) só como subclasses vazias, então nenhuma referência existente
   no Inspector precisa ser reconectada.
3. **Transição "Exame físico" → "Labs" tornada explícita em código**: `PhaseManager.StartExamPhase()`
   agora liga o `onClick` do botão da prancheta a `PhysicalExamManager.FinishPhysicalExam()`
   diretamente em código (com `RemoveAllListeners()` antes, para não duplicar se também houver
   algo configurado no Editor). O contrato deixou de ser invisível no código-fonte.
4. **`AudioManager` agora suporta múltiplos sons simultâneos**: em vez de uma única `AudioSource`
   que interrompe qualquer som em reprodução, mantém um pool de fontes (até `maxSimultaneousSounds`,
   padrão 4) e só reaproveita a mais antiga quando o pool está cheio. API pública (`Play`/`Stop`)
   não mudou, então nenhum chamador precisou ser alterado.
5. **Introduzido um evento estático (`PhaseEvents.cs`)** para a transição de fases — ataca
   diretamente o ponto crítico #12 (ausência total de eventos/EventBus). `AnamnesisManager`,
   `PhysicalExamManager`, `LabOrderManager`, `TransitionManager` e `DiagnosisManager`, ao
   concluir a etapa, chamam `PhaseEvents.RaiseStepFinished()` em vez de
   `PhaseManager.Instance.FinishStep()` diretamente — nenhum desses managers precisa mais
   conhecer `PhaseManager`. `PhaseManager` assina o evento (`PhaseEvents.StepFinished +=
   FinishStep` em `OnEnable`, `-=` em `OnDisable`) e reage chamando `FinishStep()`.
   (Optamos por uma classe estática simples em vez de um Event Channel via `ScriptableObject`:
   para este projeto, com um único fluxo de fases e sem necessidade de múltiplas instâncias de
   canal, a classe estática dá o mesmo desacoplamento sem exigir criar e arrastar um asset no
   Inspector — já funciona assim que o código compila, **sem nenhum passo manual no Editor**.)
6. **`ScoreManager`/`PatientPhaseSO` consolidados**: `PhysicalExamManager`, `LabOrderManager`,
   `DiagnosisManager` e `AnamnesisManager` ganharam, no início do `Start()`, um fallback que
   puxa `scoreManager`/`phaseData` de `PhaseManager.Instance` quando o campo não foi wireado no
   próprio componente. `PhaseManager` passa a ser a fonte única de fato — esquecer de arrastar a
   referência num componente individual deixou de quebrar silenciosamente.
7. **Investigados os dois sistemas de detecção de instrumento** (ponto crítico antigo). Conferindo
   a cena, `InstrumentInteraction` (trigger) é usado por estetoscópio/otoscópio/oftalmoscópio
   (instrumentos que o jogador aproxima do paciente), enquanto `XRSocketDisplayInteraction` +
   `XRSocketTagInteractor` (socket) é usado especificamente por oxímetro e esfigmomanômetro
   (`targetTag: oximetro` / `targetTag: esfigmo` — instrumentos que se encaixam). **Não são
   redundantes** — são duas mecânicas de interação diferentes para instrumentos diferentes, então
   nenhum código foi alterado aqui; o ponto crítico foi fechado como "não é um bug", não como
   "corrigido".
8. **Ferramenta de validação de conteúdo** (`Assets/Editor/ContentValidator.cs`, menu
   `VRClinic → Validar Conteúdo das Fases`): varre todos os `PatientPhaseSO` do projeto e avisa
   quando (a) um `examID` em `correctLabTestIDs` não tem `PhaseExamResult` correspondente
   (resultado ficaria invisível na tela de diagnóstico), ou (b) um `id` de `ScoreMapping` no
   `ScoreManager` não corresponde a nenhuma pergunta/instrumento/exame/diagnóstico real de
   nenhuma fase conhecida (provável texto desatualizado). Resolve o ponto crítico #8 e parte do #5
   de forma preventiva, sem travar o fluxo em tempo de execução.
9. **Testes automatizados para `ScoreManager`** (`Assets/Tests/EditMode/ScoreManagerTests.cs`,
   com `Assets/Tests/EditMode/MedSim.Tests.EditMode.asmdef`): cobrem mapeamento configurado vs.
   fallback default para as quatro categorias de pontuação, acumulação de pontos totais, ordem de
   `GetAllEntries()` e `ResetScores()`. Rodam no Test Runner do Editor (Window → General → Test
   Runner → EditMode). Testes para a máquina de estados do `PhaseManager` ficaram de fora desta
   leva — `PhaseManager` ainda depende fortemente de referências de cena (UI, outros managers),
   então testá-lo exigiria refatorar para extrair a lógica do `switch`/`enum Part` de forma
   isolada, ou escrever testes PlayMode mais pesados; não foi feito agora para não aumentar o
   escopo desta passada.
10. **`ScoreManager.OnEntryRegistered` (evento de instância)**: disparado a cada
    `RegisterScoreEntry()`, puramente aditivo (nenhum chamador existente precisou mudar). Hoje
    não existe nenhum feedback em tempo real de acerto/erro — tudo só aparece junto na tela final
    do `FeedbackManager` — então este evento fica disponível como ponto de extensão para uma
    futura UI/som/háptico de feedback imediato, sem acoplar o `ScoreManager` a quem vai reagir.
11. **`InstrumentEvents.cs` (evento estático, mesmo padrão do `PhaseEvents`)**: `InstrumentInteraction`
    e `XRSocketDisplayInteraction` paravam de conhecer `PhysicalExamManager` diretamente — cada
    um só dispara `InstrumentEvents.RaiseInstrumentUsed(objectID)` ao detectar o uso; o campo
    `examManager` e a busca em `Awake()` que eles tinham antes foram removidos por completo.
    `PhysicalExamManager` assina o evento (`OnEnable`/`OnDisable`) e reage chamando
    `RegisterExamItem`. Importante: a exibição na TV e o áudio de cada instrumento **não**
    viraram evento — são comportamento local de cada peça (sprite/som específicos daquele
    GameObject), então transformar isso em evento só adicionaria indireção sem reduzir
    acoplamento real.

### 10.3 Pendências que só dá para resolver no Editor Unity

- **Gerar o lightmap de verdade** (Window → Rendering → Lighting → Generate Lighting) depois de
  conferir que a geometria estática da sala está marcada como "Contribute GI".
- Opcionalmente, arrastar as novas referências (`examManager`, `transitionManager`,
  `anamnesisManager`, `physicalExamManager`) nos componentes correspondentes, para pular até a
  busca única feita em `Awake()` (não é obrigatório — já funciona sem isso).
- Abrir o projeto e testar o fluxo completo (anamnese → exame físico → labs → transição →
  diagnóstico → feedback) para confirmar que nada quebrou com as mudanças de código.

### 10.4 Itens de arquitetura ainda não implementados

- Testes automatizados para a máquina de estados do `PhaseManager` (exige extrair a lógica do
  `switch`/`enum Part` do componente de cena, ou escrever testes PlayMode — ver 10.2 item 9).
- Criar mais fases/pacientes (`PatientPhaseSO`) para validar a generalização da arquitetura —
  trabalho de conteúdo clínico, não de código.
