using System.Collections.Generic;
using UnityEngine;

public class AttemptRecord
{
    public string caseID;
    public int attemptNumber;
    public MasteryLevel level;
    public int severeErrors;
    public string nextGoal;
    public float totalSeconds;
    public readonly Dictionary<ScoreCategory, float> competencyRatios = new();
}

public static class AttemptHistory
{
    private static readonly Dictionary<string, AttemptRecord> lastByCase = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        lastByCase.Clear();
    }

    public static AttemptRecord GetLast(string caseID)
    {
        if (string.IsNullOrEmpty(caseID))
            return null;

        return lastByCase.TryGetValue(caseID, out AttemptRecord record) ? record : null;
    }

    public static AttemptRecord Record(string caseID, CaseEvaluation evaluation, float totalSeconds)
    {
        AttemptRecord previous = GetLast(caseID);
        var record = new AttemptRecord
        {
            caseID = caseID,
            attemptNumber = (previous?.attemptNumber ?? 0) + 1,
            level = evaluation.Level,
            severeErrors = evaluation.SevereErrors,
            nextGoal = evaluation.NextGoal,
            totalSeconds = totalSeconds
        };

        foreach (CompetencyResult competency in evaluation.Competencies)
            record.competencyRatios[competency.Category] = competency.Ratio;

        if (!string.IsNullOrEmpty(caseID))
            lastByCase[caseID] = record;

        return record;
    }

    public static void Clear()
    {
        lastByCase.Clear();
    }
}
