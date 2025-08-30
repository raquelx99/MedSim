using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DiagnosisManager : MonoBehaviour
{
    public PatientPhaseSO phaseData;
    public ScoreManager scoreManager;
    private int chosenDiagnosisIndex;

    public Transform diagnosisButtonContainer;
    public GameObject diagnosisButtonPrefab;

    void Start()
    {
        GenerateDiagnosisButtons();
        Debug.Log("Gerou botões de diagnóstico.");
    }

    public void GenerateDiagnosisButtons()
    {
        for (int i = 0; i < phaseData.possibleDiagnosis.Length; i++)
        {
            int index = i;
            GameObject buttonObj = Instantiate(diagnosisButtonPrefab, diagnosisButtonContainer);
            buttonObj.SetActive(true);
            Button button = buttonObj.GetComponent<Button>();
            TextMeshProUGUI label = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            label.text = phaseData.possibleDiagnosis[i];

            button.onClick.AddListener(() =>
            {
                OnDiagnosisSelected(index);
            });
        }
    }

    public void OnDiagnosisSelected(int idx)
    {
        chosenDiagnosisIndex = idx;
    }

    public void ConfirmDiagnosis()
    {
        bool acertou = (chosenDiagnosisIndex == phaseData.correctDiagnosisIndex);
        string chosenDiagnosisText = phaseData.possibleDiagnosis[chosenDiagnosisIndex];
        int pointsToApply = scoreManager.GetPointsForDiagnosis(chosenDiagnosisText, acertou);

        scoreManager.RegisterScoreEntry(new ScoreEntry
        {
            category = ScoreCategory.Diagnosis,
            severity = ErrorSeverity.Grave,
            actionID = chosenDiagnosisText,
            isCorrect = acertou,
            justification = acertou ? "" : "Diagnóstico incorreto.",
            points = pointsToApply
        });
        Debug.Log($"Diagnóstico {phaseData.possibleDiagnosis[chosenDiagnosisIndex]} foi {(acertou ? "correto" : "incorreto")}.");
        FindObjectOfType<PhaseManager>().FinishStep();
    }

    public void ResetDiagnosis()
    {
        chosenDiagnosisIndex = -1;
        foreach (Transform child in diagnosisButtonContainer)
        {
            Destroy(child.gameObject);
        }    
    }
}
