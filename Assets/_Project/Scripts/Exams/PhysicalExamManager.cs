using System;
using System.Collections.Generic;
using UnityEngine;

public class PhysicalExamManager : MonoBehaviour
{
    private const int OutOfPhasePenalty = -2;

    public PatientPhaseSO phaseData;
    public ScoreManager scoreManager;

    private readonly HashSet<string> usedObjectIDs = new();

    public event Action ExamItemsChanged;

    public bool WasUsed(string objectID) => usedObjectIDs.Contains(objectID);

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
            RegisterEntry(objectID, false, ErrorSeverity.Leve, ErrorKind.OutOfPhase,
                "Tentativa de uso de objeto fora da fase de exame físico.", OutOfPhasePenalty,
                $"Usou o {DisplayNames.Instrument(objectID)} antes do exame físico", "Usar os instrumentos somente na etapa de exame físico");
            return;
        }

        if (!usedObjectIDs.Add(objectID)) return;

        RegisterEntry(objectID, true, ErrorSeverity.Leve, ErrorKind.None, "", scoreManager.GetPointsForPhysicalExam(objectID, true));
        ExamItemsChanged?.Invoke();
    }

    public void FinishPhysicalExam()
    {
        foreach (string requiredID in phaseData.requiredExamObjectIDs)
        {
            if (usedObjectIDs.Contains(requiredID)) continue;

            RegisterEntry(requiredID, false, phaseData.missingPhysicalExamSeverity, ErrorKind.Omission,
                "Instrumento essencial não foi utilizado.",
                scoreManager.GetPointsForPhysicalExam(requiredID, false),
                "Não utilizado", $"Examinar o paciente com o {DisplayNames.Instrument(requiredID)}");
        }

        PhaseEvents.RaiseStepFinished();
    }

    void RegisterEntry(string objectID, bool isCorrect, ErrorSeverity severity, ErrorKind kind, string justification, int points,
        string chosenAnswer = null, string expectedAnswer = null)
    {
        scoreManager.RegisterScoreEntry(new ScoreEntry
        {
            category = ScoreCategory.PhysicalExam,
            severity = severity,
            kind = kind,
            actionID = objectID,
            isCorrect = isCorrect,
            justification = justification,
            points = points,
            topic = DisplayNames.Instrument(objectID),
            chosenAnswer = chosenAnswer,
            expectedAnswer = expectedAnswer
        });
    }
}
