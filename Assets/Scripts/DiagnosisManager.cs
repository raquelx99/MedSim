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
        bool correct = (chosenDiagnosisIndex == phaseData.correctDiagnosisIndex);
        scoreManager.Add(correct);
        Debug.Log($"Diagnóstico {phaseData.possibleDiagnosis[chosenDiagnosisIndex]} foi {(correct ? "correto" : "incorreto")}.");
        FindObjectOfType<PhaseManager>().FinishPhase();
    }

}
