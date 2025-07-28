using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class PhysicalExamManager : MonoBehaviour
{
    public PatientPhaseSO phaseData;
    public ScoreManager scoreManager;
    private HashSet<string> usedObjectIDs = new();

    public void RegisterExamItem(string objectID)
    {
        if (PhaseManager.Instance.currentPart != PhaseManager.Part.Exam)
        {
            Debug.Log("Objeto usado fora da fase de exame físico — ignorado.");
            return;
        }

        if (usedObjectIDs.Contains(objectID)) return;

        usedObjectIDs.Add(objectID);

        if (phaseData.requiredExamObjectIDs.Contains(objectID))
        {
            scoreManager.Add(true);
            Debug.Log($"Usou corretamente: {objectID}");
        }
        else
        {
            scoreManager.Add(false);
            Debug.Log($"Usou incorretamente: {objectID}");
        }
    }
    
    public void FinishPhysicalExam()
    {
        foreach (var req in phaseData.requiredExamObjectIDs)
            if (!usedObjectIDs.Contains(req))
                scoreManager.Add(false);

        foreach (var used in usedObjectIDs)
            if (!System.Array.Exists(phaseData.requiredExamObjectIDs, id => id == used))
                scoreManager.Add(false);

        
        PhaseManager.Instance.FinishPhase();
    }
}
