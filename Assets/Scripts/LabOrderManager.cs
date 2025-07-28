using UnityEngine;
using System.Collections.Generic;

public class LabOrderManager : MonoBehaviour
{
    public PatientPhaseSO phaseData;
    public ScoreManager scoreManager;
    private HashSet<string> selectedLabs = new();

    public void OnLabToggle(string labID, bool isOn)
    {
        if (isOn) selectedLabs.Add(labID);
        else     selectedLabs.Remove(labID);
    }

    public void ConfirmLabs()
    {
        foreach (var lab in selectedLabs)
            scoreManager.Add(
                System.Array.Exists(phaseData.correctLabTestIDs, id => id == lab)
            );
        
        foreach (var req in phaseData.correctLabTestIDs)
            if (!selectedLabs.Contains(req))
                scoreManager.Add(false);

        FindObjectOfType<PhaseManager>().NextPart();
    }
}
