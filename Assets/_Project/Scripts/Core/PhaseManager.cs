using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PhaseManager : MonoBehaviour
{
    public enum Part { Anamnese, Exam, Labs, Transition, Diagnosis }

    private const string CurrentStepColor = "#153667";
    private const string DoneStepColor = "#2F855A";
    private const string PendingStepColor = "#8A94A6";
    private const string GoalColor = "#B7791F";
    private const float MinPranchetaFontScale = 0.5f;

    private static readonly (string label, Part[] parts)[] ProgressSteps =
    {
        ("Anamnese", new[] { Part.Anamnese }),
        ("Exame físico", new[] { Part.Exam }),
        ("Exames", new[] { Part.Labs, Part.Transition }),
        ("Diagnóstico", new[] { Part.Diagnosis }),
        ("Feedback", new Part[0]),
    };

    public static PhaseManager Instance { get; private set; }

    public PatientPhaseSO phaseData;
    public ScoreManager scoreManager;
    public AnamnesisManager anamnesisManager;
    public PhysicalExamManager physicalExamManager;
    public LabOrderManager labOrderManager;
    public TransitionManager transitionManager;
    public DiagnosisManager diagnosisManager;
    public FeedbackManager feedbackManager;

    public TextMeshProUGUI pranchetaText;
    public Button pranchetaButton;
    public GameObject uIPedidoExames;
    public GameObject uIDiagnosis;

    [Header("Modo de treino")]
    public bool trainingMode;

    private float startTime;
    private float partStartTime;
    private bool finished;
    private string currentInstruction = "";
    private readonly Dictionary<Part, float> partDurations = new();

    public Part currentPart { get; private set; } = Part.Anamnese;
    public float totalPhaseTime { get; private set; }

    public string CaseID
    {
        get
        {
            if (phaseData == null) return "";
            return string.IsNullOrWhiteSpace(phaseData.phaseName) ? phaseData.name : phaseData.phaseName;
        }
    }

    void Awake()
    {
        Instance = this;

        if (anamnesisManager == null)
            anamnesisManager = FindObjectOfType<AnamnesisManager>();
        if (physicalExamManager == null)
            physicalExamManager = FindObjectOfType<PhysicalExamManager>();

        EnablePranchetaAutoSizing();
    }

    void OnEnable()
    {
        PhaseEvents.StepFinished += FinishStep;
        if (physicalExamManager != null)
            physicalExamManager.ExamItemsChanged += RefreshPrancheta;
    }

    void OnDisable()
    {
        PhaseEvents.StepFinished -= FinishStep;
        if (physicalExamManager != null)
            physicalExamManager.ExamItemsChanged -= RefreshPrancheta;
    }

    void Start()
    {
        startTime = Time.time;
        partStartTime = startTime;
        StartAnamnese();
    }

    public void NextPart()
    {
        RecordPartDuration();
        currentPart++;
        partStartTime = Time.time;

        switch (currentPart)
        {
            case Part.Exam: StartExamPhase(); break;
            case Part.Labs: StartLabsPhase(); break;
            case Part.Transition: StartTransitionPhase(); break;
            case Part.Diagnosis: StartDiagnosisPhase(); break;
            default: FinishPhase(); break;
        }
    }

    public void FinishStep()
    {
        NextPart();
    }

    public void FinishPhase()
    {
        if (finished) return;
        finished = true;

        uIDiagnosis.SetActive(false);
        totalPhaseTime = Time.time - startTime;
        SetPrancheta("Atendimento concluído. Veja o seu desempenho no painel de feedback.");
        feedbackManager.gameObject.SetActive(true);
        feedbackManager.ShowFeedback();
    }

    public void RestartPhase()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public float DurationOf(params Part[] parts)
    {
        return parts.Sum(part => partDurations.TryGetValue(part, out float seconds) ? seconds : 0f);
    }

    void RecordPartDuration()
    {
        partDurations.TryGetValue(currentPart, out float previous);
        partDurations[currentPart] = previous + (Time.time - partStartTime);
    }

    void StartAnamnese()
    {
        SetPrancheta("Anamnese: fale com o paciente primeiro!");
        anamnesisManager.Init(phaseData.dialogueSteps.ToList(), phaseData.anamnesisSteps.ToList());
    }

    void StartExamPhase()
    {
        SetPrancheta("Exame Físico: Use os itens corretos. Quando terminar, clique no botão confirmar.");
        pranchetaButton.gameObject.SetActive(true);

        pranchetaButton.onClick.RemoveAllListeners();
        if (physicalExamManager != null)
            pranchetaButton.onClick.AddListener(physicalExamManager.FinishPhysicalExam);
    }

    void StartLabsPhase()
    {
        SetPrancheta("Exames Laboratoriais: Selecione os exames necessários e clique em confirmar.");
        pranchetaButton.gameObject.SetActive(false);
        uIPedidoExames.SetActive(true);
        labOrderManager.gameObject.SetActive(true);
    }

    void StartTransitionPhase()
    {
        uIPedidoExames.SetActive(false);
        SetPrancheta("Fale com o paciente e informe os exames solicitados.");
        transitionManager.gameObject.SetActive(true);
    }

    void StartDiagnosisPhase()
    {
        labOrderManager.ShowResultsUI();
        uIPedidoExames.SetActive(false);
        labOrderManager.gameObject.SetActive(false);
        SetPrancheta("Diagnóstico: Selecione o diagnóstico correto e confirme.");
        diagnosisManager.gameObject.SetActive(true);
        uIDiagnosis.SetActive(true);
    }

    void EnablePranchetaAutoSizing()
    {
        if (pranchetaText == null) return;

        pranchetaText.enableAutoSizing = true;
        pranchetaText.fontSizeMax = pranchetaText.fontSize;
        pranchetaText.fontSizeMin = pranchetaText.fontSize * MinPranchetaFontScale;
    }

    void SetPrancheta(string instruction)
    {
        currentInstruction = instruction;
        RefreshPrancheta();
    }

    void RefreshPrancheta()
    {
        if (pranchetaText == null) return;

        var text = new StringBuilder();
        text.Append($"<size=70%>{ProgressLine()}</size>\n\n");
        text.Append(currentInstruction);

        if (ShowsChecklist)
            text.Append("\n\n").Append(ExamChecklist());

        string goal = AttemptHistory.GetLast(CaseID)?.nextGoal;
        if (!finished && !string.IsNullOrWhiteSpace(goal))
            text.Append($"\n\n<size=75%><color={GoalColor}><b>Meta desta tentativa:</b></color> {goal}</size>");

        pranchetaText.text = text.ToString();
    }

    bool ShowsChecklist => trainingMode && currentPart == Part.Exam && !finished;

    string ProgressLine()
    {
        int currentStep = finished
            ? ProgressSteps.Length - 1
            : Array.FindIndex(ProgressSteps, step => step.parts.Contains(currentPart));

        IEnumerable<string> steps = ProgressSteps.Select((step, index) => FormatStep(step.label, index, currentStep));
        return string.Join($"<color={PendingStepColor}>  >  </color>", steps);
    }

    static string FormatStep(string label, int index, int currentStep)
    {
        if (index == currentStep)
            return $"<color={CurrentStepColor}><b><u>{label}</u></b></color>";

        string color = index < currentStep ? DoneStepColor : PendingStepColor;
        return $"<color={color}>{label}</color>";
    }

    string ExamChecklist()
    {
        var text = new StringBuilder("<size=80%><b>Checklist de treino</b>\n");
        foreach (string id in phaseData.requiredExamObjectIDs)
        {
            bool done = physicalExamManager != null && physicalExamManager.WasUsed(id);
            string mark = done ? $"<color={DoneStepColor}>[x]</color>" : "[  ]";
            text.Append($"{mark} {DisplayNames.Instrument(id)}\n");
        }

        return text.Append("</size>").ToString();
    }
}
