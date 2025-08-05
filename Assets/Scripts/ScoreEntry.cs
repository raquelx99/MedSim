using UnityEngine;

public enum ScoreCategory
{
    Anamnese,
    PhysicalExam,
    LabExams,
    Diagnosis
}

public enum ErrorSeverity
{
    Leve,
    Moderado,
    Grave
}
public class ScoreEntry
{
    public ScoreCategory category;
    public ErrorSeverity severity;
    public string actionID;
    public bool isCorrect;
    public string justification;
    public int points;
}
