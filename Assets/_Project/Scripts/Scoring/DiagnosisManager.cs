using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DiagnosisManager : MonoBehaviour
{
    public PatientPhaseSO phaseData;
    public ScoreManager scoreManager;
    public Transform diagnosisButtonContainer;
    public GameObject diagnosisButtonPrefab;

    private int chosenDiagnosisIndex;

    void Start()
    {
        if (scoreManager == null && PhaseManager.Instance != null)
            scoreManager = PhaseManager.Instance.scoreManager;
        if (phaseData == null && PhaseManager.Instance != null)
            phaseData = PhaseManager.Instance.phaseData;

        GenerateDiagnosisButtons();
    }

    public void GenerateDiagnosisButtons()
    {
        for (int i = 0; i < phaseData.possibleDiagnosis.Length; i++)
        {
            int diagnosisIndex = i;
            GameObject buttonObject = Instantiate(diagnosisButtonPrefab, diagnosisButtonContainer);
            buttonObject.SetActive(true);
            buttonObject.GetComponentInChildren<TextMeshProUGUI>().text = phaseData.possibleDiagnosis[i];
            buttonObject.GetComponent<Button>().onClick.AddListener(() => OnDiagnosisSelected(diagnosisIndex));
        }
    }

    public void OnDiagnosisSelected(int index)
    {
        chosenDiagnosisIndex = index;
    }

    public void ConfirmDiagnosis()
    {
        string chosenDiagnosis = phaseData.possibleDiagnosis[chosenDiagnosisIndex];
        bool isCorrect = chosenDiagnosisIndex == phaseData.correctDiagnosisIndex;

        scoreManager.RegisterScoreEntry(new ScoreEntry
        {
            category = ScoreCategory.Diagnosis,
            severity = ErrorSeverity.Grave,
            actionID = chosenDiagnosis,
            isCorrect = isCorrect,
            justification = isCorrect ? "" : "Diagnóstico incorreto.",
            points = scoreManager.GetPointsForDiagnosis(chosenDiagnosis, isCorrect)
        });

        PhaseEvents.RaiseStepFinished();
    }

    public void ResetDiagnosis()
    {
        chosenDiagnosisIndex = -1;
        foreach (Transform child in diagnosisButtonContainer)
            Destroy(child.gameObject);
    }
}
