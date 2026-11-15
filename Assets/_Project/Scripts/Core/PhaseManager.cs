using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PhaseManager : MonoBehaviour
{
    public enum Part { Anamnese, Exam, Labs, Transition, Diagnosis }

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

    private float startTime;

    public Part currentPart { get; private set; } = Part.Anamnese;
    public float totalPhaseTime { get; private set; }

    void Awake()
    {
        Instance = this;

        if (anamnesisManager == null)
            anamnesisManager = FindObjectOfType<AnamnesisManager>();
        if (physicalExamManager == null)
            physicalExamManager = FindObjectOfType<PhysicalExamManager>();
    }

    void OnEnable()
    {
        PhaseEvents.StepFinished += FinishStep;
    }

    void OnDisable()
    {
        PhaseEvents.StepFinished -= FinishStep;
    }

    void Start()
    {
        StartAnamnese();
        startTime = Time.time;
    }

    public void NextPart()
    {
        currentPart++;

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
        uIDiagnosis.SetActive(false);
        feedbackManager.gameObject.SetActive(true);
        totalPhaseTime = Time.time - startTime;
        feedbackManager.ShowFeedback();
    }

    public void RestartPhase()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void StartAnamnese()
    {
        anamnesisManager.Init(phaseData.dialogueSteps.ToList(), phaseData.anamnesisSteps.ToList());
    }

    void StartExamPhase()
    {
        pranchetaText.text = "Exame Físico: Use os itens corretos. Quando terminar, clique no botão confirmar.";
        pranchetaButton.gameObject.SetActive(true);

        pranchetaButton.onClick.RemoveAllListeners();
        if (physicalExamManager != null)
            pranchetaButton.onClick.AddListener(physicalExamManager.FinishPhysicalExam);
    }

    void StartLabsPhase()
    {
        pranchetaText.text = "Exames Laboratoriais: Selecione os exames necessários e clique em confirmar.";
        pranchetaButton.gameObject.SetActive(false);
        uIPedidoExames.SetActive(true);
        labOrderManager.gameObject.SetActive(true);
    }

    void StartTransitionPhase()
    {
        uIPedidoExames.SetActive(false);
        pranchetaText.text = "Fale com o paciente e informe os exames solicitados.";
        transitionManager.gameObject.SetActive(true);
    }

    void StartDiagnosisPhase()
    {
        labOrderManager.ShowResultsUI();
        uIPedidoExames.SetActive(false);
        labOrderManager.gameObject.SetActive(false);
        pranchetaText.text = "Diagnóstico: Selecione o diagnóstico correto e confirme.";
        diagnosisManager.gameObject.SetActive(true);
        uIDiagnosis.SetActive(true);
    }
}
