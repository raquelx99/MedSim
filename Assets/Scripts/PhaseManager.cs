using UnityEngine;
using System.Linq;
using TMPro;
using UnityEngine.UI;
using Unity.VisualScripting;
using UnityEditor.SearchService;
using UnityEngine.SceneManagement;

public class PhaseManager : MonoBehaviour
{
    public static PhaseManager Instance { get; private set; }
    private SceneManager sceneManager;

    public PatientPhaseSO phaseData;
    public ScoreManager scoreManager;
    public LabOrderManager labOrderManager;
    public DiagnosisManager diagnosisManager;

    public enum Part { Anamnese, Exam, Labs, Diagnosis }
    public Part currentPart { get; private set; } = Part.Anamnese;

    public TextMeshProUGUI pranchetaText;

    public Button pranchetaButton;
    public GameObject uIPedidoExames;
    public GameObject uIDiagnosis;
    public FeedbackManager feedbackManager;
    public float totalPhaseTime { get; private set; }
    private float startTime;

    void Awake()
    {
        Instance = this;
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
            case Part.Diagnosis: StartDiagnosisPhase(); break;
            case Part.Diagnosis + 1: FinishPhase(); break;
        }
    }

    void StartAnamnese()
    {
        var anamnesisManager = FindObjectOfType<AnamnesisManager>();
        anamnesisManager.Init(
            phaseData.dialogueSteps.ToList(),
            phaseData.anamnesisSteps.ToList()
        );
    }

    void StartExamPhase()
    {
        Debug.Log("Fase de exame físico iniciada.");

        pranchetaText.text = "Exame Físico: Use os itens corretos. Quando terminar, clique no botão confirmar.";
        pranchetaButton.gameObject.SetActive(true);
    }

    void StartLabsPhase()
    {
        pranchetaText.text = "Exames Laboratoriais: Selecione os exames necessários e clique em confirmar.";
        pranchetaButton.gameObject.SetActive(false);
        uIPedidoExames.SetActive(true);
        labOrderManager.gameObject.SetActive(true);

        Debug.Log("Fase de pedidos de exames iniciada.");
    }

    void StartTransitionPhase()
    {
        pranchetaText.text = "Fale com o paciente e informe os exames solicitados.";
    }

    void StartDiagnosisPhase()
    {
        uIPedidoExames.SetActive(false);
        pranchetaText.text = "Diagnóstico: Selecione o diagnóstico correto e confirme.";
        labOrderManager.gameObject.SetActive(false);
        diagnosisManager.gameObject.SetActive(true);
        uIDiagnosis.SetActive(true);

        Debug.Log("Fase de diagnóstico iniciada.");
    }

    public void FinishStep()
    {
        NextPart();
    }

    public void FinishPhase()
    {
        uIDiagnosis.SetActive(false);
        feedbackManager.gameObject.SetActive(true);
        string summary = scoreManager.GetScoreSummary();
        totalPhaseTime = Time.time - startTime;
        feedbackManager.ShowFeedback();
        Debug.Log($"Fim da fase.\n{summary}");
    }

    public void RestartPhase()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
}
