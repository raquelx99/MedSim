using System.Collections.Generic;
using UnityEngine;

public class PhysicalExamManager : MonoBehaviour
{
    private const int OutOfPhasePenalty = -2;

    public PatientPhaseSO phaseData;
    public ScoreManager scoreManager;

    private readonly HashSet<string> usedObjectIDs = new();

    void OnEnable()
    {
        InstrumentEvents.InstrumentUsed += RegisterExamItem;
    }

    void OnDisable()
    {
        InstrumentEvents.InstrumentUsed -= RegisterExamItem;
    }

    void Start()
    {
        if (scoreManager == null && PhaseManager.Instance != null)
            scoreManager = PhaseManager.Instance.scoreManager;
        if (phaseData == null && PhaseManager.Instance != null)
            phaseData = PhaseManager.Instance.phaseData;
    }

    public void RegisterExamItem(string objectID)
    {
        if (PhaseManager.Instance.currentPart != PhaseManager.Part.Exam)
        {
            RegisterEntry(objectID, false, "Tentativa de uso de objeto fora da fase de exame físico.", OutOfPhasePenalty);
            return;
        }

        if (!usedObjectIDs.Add(objectID)) return;

        RegisterEntry(objectID, true, "", scoreManager.GetPointsForPhysicalExam(objectID, true));
    }

    public void FinishPhysicalExam()
    {
        foreach (string requiredID in phaseData.requiredExamObjectIDs)
        {
            if (usedObjectIDs.Contains(requiredID)) continue;

            RegisterEntry(requiredID, false, "Instrumento essencial não foi utilizado.",
                scoreManager.GetPointsForPhysicalExam(requiredID, false));
        }

        PhaseEvents.RaiseStepFinished();
    }

    void RegisterEntry(string objectID, bool isCorrect, string justification, int points)
    {
        scoreManager.RegisterScoreEntry(new ScoreEntry
        {
            category = ScoreCategory.PhysicalExam,
            severity = ErrorSeverity.Moderado,
            actionID = objectID,
            isCorrect = isCorrect,
            justification = justification,
            points = points
        });
    }
}
