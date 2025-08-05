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

        bool acertou = phaseData.requiredExamObjectIDs.Contains(objectID);
        int pointsToApply = scoreManager.GetPointsForPhysicalExam(objectID, acertou);

        scoreManager.RegisterScoreEntry(new ScoreEntry {
            category = ScoreCategory.PhysicalExam,
            severity = acertou ? ErrorSeverity.Leve : ErrorSeverity.Moderado,
            actionID = objectID,
            isCorrect = acertou,
            justification = acertou ? "" : "Instrumento inadequado para a situação.",
            points = pointsToApply
        });

        Debug.Log(acertou ? $"Usou corretamente: {objectID}" : $"Usou incorretamente: {objectID}");
    }

    public void FinishPhysicalExam()
    {
        foreach (var req in phaseData.requiredExamObjectIDs)
        {
            if (!usedObjectIDs.Contains(req))
            {
                int pointsToApply = scoreManager.GetPointsForPhysicalExam(req, false);
                scoreManager.RegisterScoreEntry(new ScoreEntry
                {
                    category = ScoreCategory.PhysicalExam,
                    severity = ErrorSeverity.Moderado,
                    actionID = req,
                    isCorrect = false,
                    justification = "Instrumento essencial não foi utilizado.",
                    points = pointsToApply
                });
            }
        }

        PhaseManager.Instance.FinishPhase();
        
    }
}
