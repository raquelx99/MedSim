using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public struct ScoreMapping
{
    public string id;
    public int correctPoints;
    public int incorrectPenalty;
}

public class ScoreManager : MonoBehaviour
{
    public List<ScoreMapping> anamnesisScores;
    public List<ScoreMapping> physicalExamScores;
    public List<ScoreMapping> labExamScores;
    public List<ScoreMapping> diagnosisScores;

    private readonly List<ScoreEntry> entries = new();

    public event Action<ScoreEntry> OnEntryRegistered;

    public int GetPointsForAnamnesis(string questionPrompt, bool isCorrect)
    {
        return ResolvePoints(anamnesisScores, questionPrompt, isCorrect, defaultReward: 2, defaultPenalty: 0);
    }

    public int GetPointsForPhysicalExam(string itemID, bool isCorrect)
    {
        return ResolvePoints(physicalExamScores, itemID, isCorrect, defaultReward: 20, defaultPenalty: -5);
    }

    public int GetPointsForLabExam(string examID, bool isCorrect)
    {
        return ResolvePoints(labExamScores, examID, isCorrect, defaultReward: 20, defaultPenalty: -1);
    }

    public int GetPointsForDiagnosis(string diagnosisText, bool isCorrect)
    {
        return ResolvePoints(diagnosisScores, diagnosisText, isCorrect, defaultReward: 40, defaultPenalty: -20);
    }

    public void RegisterScoreEntry(ScoreEntry entry)
    {
        entries.Add(entry);
        OnEntryRegistered?.Invoke(entry);
    }

    public float GetCurrentTotalPoints()
    {
        return entries.Sum(entry => entry.points);
    }

    public string GetScoreSummary()
    {
        var summary = new System.Text.StringBuilder();
        summary.Append($"Pontuação final: {Mathf.Max(0, GetCurrentTotalPoints())}\n\n");

        foreach (ScoreCategory category in Enum.GetValues(typeof(ScoreCategory)))
        {
            int hits = entries.Count(e => e.category == category && e.isCorrect);
            int misses = entries.Count(e => e.category == category && !e.isCorrect);
            summary.Append($"{category}: {hits} acertos, {misses} erros\n");
        }

        return summary.ToString();
    }

    public void ResetScores()
    {
        entries.Clear();
    }

    public IReadOnlyList<ScoreEntry> GetAllEntries()
    {
        return entries;
    }

    static int ResolvePoints(List<ScoreMapping> mappings, string id, bool isCorrect, int defaultReward, int defaultPenalty)
    {
        ScoreMapping rule = mappings.FirstOrDefault(mapping => mapping.id == id);
        if (rule.id == null)
            return isCorrect ? defaultReward : defaultPenalty;

        return isCorrect ? rule.correctPoints : -rule.incorrectPenalty;
    }
}
