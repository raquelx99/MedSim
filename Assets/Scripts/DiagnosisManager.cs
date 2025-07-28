using UnityEngine;

public class DiagnosisManager : MonoBehaviour
{
    public PatientPhaseSO phaseData;
    public ScoreManager scoreManager;
    public int chosenDiagnosisIndex;

    public void OnDiagnosisSelected(int idx)
    {
        chosenDiagnosisIndex = idx;
    }

    public void ConfirmDiagnosis()
    {
        bool correct = (chosenDiagnosisIndex == phaseData.correctDiagnosisIndex);
        scoreManager.Add(correct);
        FindObjectOfType<PhaseManager>().FinishPhase();
    }
}
