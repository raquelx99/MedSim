using UnityEngine;
using System.Linq;
using TMPro;
using UnityEngine.UI;

public class PhaseManager : MonoBehaviour
{
    public static PhaseManager Instance { get; private set; }

    public PatientPhaseSO phaseData;
    public ScoreManager scoreManager;
    public LabOrderManager labOrderManager;

    public enum Part { Anamnese, Exam, Labs, Diagnosis }
    public Part currentPart { get; private set; } = Part.Anamnese;

    public TextMeshProUGUI pranchetaText;

    public Button pranchetaButton;
    public GameObject uIPedidoExames;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        StartAnamnese();
    }

    public void NextPart()
    {
        currentPart++;
        switch (currentPart)
        {
            case Part.Exam: StartExamPhase(); break;
            case Part.Labs: StartLabsPhase(); break;
            case Part.Diagnosis: StartDiagnosisPhase(); break;
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

    }

    void StartDiagnosisPhase()
    {
        Debug.Log("Fase de diagnóstico iniciada.");
    }

    public void FinishPhase()
    {
        Debug.Log($"Fim da fase. Score: {scoreManager.score}");
        NextPart();
    }
}
