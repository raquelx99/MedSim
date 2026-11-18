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

public enum ErrorKind
{
    None,
    WrongChoice,
    Omission,
    Unnecessary,
    OutOfPhase
}

public class ScoreEntry
{
    public ScoreCategory category;
    public ErrorSeverity severity;
    public ErrorKind kind;
    public string actionID;
    public bool isCorrect;
    public string justification;
    public int points;

    public string topic;
    public string chosenAnswer;
    public string expectedAnswer;
}
