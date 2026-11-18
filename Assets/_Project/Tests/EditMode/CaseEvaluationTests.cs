using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public class CaseEvaluationTests
{
    static ScoreEntry Hit(ScoreCategory category, string topic = "ok")
    {
        return new ScoreEntry { category = category, isCorrect = true, severity = ErrorSeverity.Leve, topic = topic };
    }

    static IEnumerable<ScoreEntry> Hits(ScoreCategory category, int count)
    {
        return Enumerable.Range(0, count).Select(_ => Hit(category));
    }

    static ScoreEntry Miss(ScoreCategory category, ErrorSeverity severity, ErrorKind kind = ErrorKind.WrongChoice, string topic = "erro")
    {
        return new ScoreEntry { category = category, isCorrect = false, severity = severity, kind = kind, topic = topic };
    }

    [Test]
    public void Competencies_AreComputedPerCategory()
    {
        var evaluation = new CaseEvaluation(new List<ScoreEntry>
        {
            Hit(ScoreCategory.Anamnese), Hit(ScoreCategory.Anamnese), Hit(ScoreCategory.Anamnese),
            Miss(ScoreCategory.Anamnese, ErrorSeverity.Leve),
            Hit(ScoreCategory.Diagnosis)
        });

        Assert.AreEqual(0.75f, evaluation.Find(ScoreCategory.Anamnese).Value.Ratio, 0.001f);
        Assert.AreEqual(1f, evaluation.Find(ScoreCategory.Diagnosis).Value.Ratio, 0.001f);
        Assert.IsNull(evaluation.Find(ScoreCategory.LabExams));
        Assert.AreEqual(0.875f, evaluation.OverallRatio, 0.001f);
    }

    [Test]
    public void PerfectAttempt_IsConsistente()
    {
        var evaluation = new CaseEvaluation(new List<ScoreEntry>
        {
            Hit(ScoreCategory.Anamnese), Hit(ScoreCategory.PhysicalExam), Hit(ScoreCategory.LabExams), Hit(ScoreCategory.Diagnosis)
        });

        Assert.AreEqual(MasteryLevel.Consistente, evaluation.Level);
        Assert.IsFalse(evaluation.IsCappedBySeverity);
    }

    [Test]
    public void SevereError_CapsLevelEvenWithManyHits()
    {
        var entries = new List<ScoreEntry>();
        entries.AddRange(Hits(ScoreCategory.Anamnese, 20));
        entries.AddRange(Hits(ScoreCategory.PhysicalExam, 20));
        entries.AddRange(Hits(ScoreCategory.LabExams, 20));
        entries.AddRange(Hits(ScoreCategory.Diagnosis, 9));
        entries.Add(Miss(ScoreCategory.Diagnosis, ErrorSeverity.Grave));

        var evaluation = new CaseEvaluation(entries);

        Assert.AreEqual(MasteryLevel.Consistente, evaluation.UncappedLevel);
        Assert.AreEqual(CaseEvaluation.MaxLevelWithSevereError, evaluation.Level);
        Assert.IsTrue(evaluation.IsCappedBySeverity);
        Assert.AreEqual(1, evaluation.SevereErrors);
    }

    [Test]
    public void RecurringModerateErrors_BlockConsistente()
    {
        Assert.AreEqual(MasteryLevel.Adequado,
            CaseEvaluation.ApplySeverityCap(MasteryLevel.Consistente, 0, CaseEvaluation.ModerateErrorsToBlockConsistente));
        Assert.AreEqual(MasteryLevel.Consistente, CaseEvaluation.ApplySeverityCap(MasteryLevel.Consistente, 0, 1));
    }

    [Test]
    public void LevelForRatio_UsesThresholds()
    {
        Assert.AreEqual(MasteryLevel.EmDesenvolvimento, CaseEvaluation.LevelForRatio(0.2f));
        Assert.AreEqual(MasteryLevel.EmProgresso, CaseEvaluation.LevelForRatio(CaseEvaluation.EmProgressoThreshold));
        Assert.AreEqual(MasteryLevel.Adequado, CaseEvaluation.LevelForRatio(CaseEvaluation.AdequadoThreshold));
        Assert.AreEqual(MasteryLevel.Consistente, CaseEvaluation.LevelForRatio(1f));
    }

    [Test]
    public void SeverityCounts_IgnoreCorrectEntries()
    {
        var evaluation = new CaseEvaluation(new List<ScoreEntry>
        {
            Hit(ScoreCategory.Anamnese),
            Miss(ScoreCategory.Anamnese, ErrorSeverity.Leve),
            Miss(ScoreCategory.LabExams, ErrorSeverity.Moderado),
            Miss(ScoreCategory.LabExams, ErrorSeverity.Moderado),
            Miss(ScoreCategory.Diagnosis, ErrorSeverity.Grave)
        });

        Assert.AreEqual(1, evaluation.MildErrors);
        Assert.AreEqual(2, evaluation.ModerateErrors);
        Assert.AreEqual(1, evaluation.SevereErrors);
    }

    [Test]
    public void NextGoal_FocusesOnMostSevereError()
    {
        var evaluation = new CaseEvaluation(new List<ScoreEntry>
        {
            Miss(ScoreCategory.Anamnese, ErrorSeverity.Leve),
            Miss(ScoreCategory.Diagnosis, ErrorSeverity.Grave)
        });

        StringAssert.Contains("diagnóstico", evaluation.NextGoal);
    }

    [Test]
    public void NextGoal_ListsMissingPhysicalExams()
    {
        var evaluation = new CaseEvaluation(new List<ScoreEntry>
        {
            Miss(ScoreCategory.PhysicalExam, ErrorSeverity.Moderado, ErrorKind.Omission, "Estetoscópio"),
            Miss(ScoreCategory.PhysicalExam, ErrorSeverity.Moderado, ErrorKind.Omission, "Oxímetro"),
            Miss(ScoreCategory.LabExams, ErrorSeverity.Moderado, ErrorKind.Unnecessary, "Raio-X")
        });

        StringAssert.Contains("Estetoscópio e Oxímetro", evaluation.NextGoal);
        StringAssert.DoesNotContain("Raio-X", evaluation.NextGoal);
    }

    [Test]
    public void NextGoal_WithoutErrors_DoesNotRewardSpeed()
    {
        var evaluation = new CaseEvaluation(new List<ScoreEntry> { Hit(ScoreCategory.Diagnosis) });

        StringAssert.DoesNotContain("agilidade", evaluation.NextGoal);
        StringAssert.DoesNotContain("rápid", evaluation.NextGoal);
    }

    [Test]
    public void QualitativeSummary_ContrastsStrengthsAndGaps()
    {
        var evaluation = new CaseEvaluation(new List<ScoreEntry>
        {
            Hit(ScoreCategory.Anamnese),
            Hit(ScoreCategory.PhysicalExam),
            Miss(ScoreCategory.PhysicalExam, ErrorSeverity.Moderado, ErrorKind.Omission)
        });

        Assert.AreEqual("Boa investigação na anamnese, mas exame físico incompleto.", evaluation.QualitativeSummary);
    }

    [Test]
    public void AttemptHistory_KeepsLastAttemptPerCase()
    {
        AttemptHistory.Clear();
        var first = new CaseEvaluation(new List<ScoreEntry> { Miss(ScoreCategory.Diagnosis, ErrorSeverity.Grave) });
        var second = new CaseEvaluation(new List<ScoreEntry> { Hit(ScoreCategory.Diagnosis) });

        AttemptHistory.Record("caso", first, 60f);
        AttemptRecord record = AttemptHistory.Record("caso", second, 50f);

        Assert.AreEqual(2, record.attemptNumber);
        Assert.AreSame(record, AttemptHistory.GetLast("caso"));
        Assert.IsNull(AttemptHistory.GetLast("outro"));
        AttemptHistory.Clear();
    }
}
