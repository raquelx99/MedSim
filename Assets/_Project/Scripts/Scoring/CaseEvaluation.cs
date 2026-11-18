using System.Collections.Generic;
using System.Linq;

public enum MasteryLevel
{
    EmDesenvolvimento,
    EmProgresso,
    Adequado,
    Consistente
}

public readonly struct CompetencyResult
{
    public readonly ScoreCategory Category;
    public readonly int Hits;
    public readonly int Total;

    public CompetencyResult(ScoreCategory category, int hits, int total)
    {
        Category = category;
        Hits = hits;
        Total = total;
    }

    public float Ratio => Total > 0 ? (float)Hits / Total : 0f;
}

public class CaseEvaluation
{
    public const float ConsistenteThreshold = 0.9f;
    public const float AdequadoThreshold = 0.7f;
    public const float EmProgressoThreshold = 0.5f;
    public const float StrongCompetencyThreshold = 0.8f;

    public const MasteryLevel MaxLevelWithSevereError = MasteryLevel.EmProgresso;
    public const int ModerateErrorsToBlockConsistente = 2;

    private const int MaxTopicsInGoal = 3;

    public IReadOnlyList<CompetencyResult> Competencies { get; }
    public float OverallRatio { get; }
    public int SevereErrors { get; }
    public int ModerateErrors { get; }
    public int MildErrors { get; }
    public MasteryLevel UncappedLevel { get; }
    public MasteryLevel Level { get; }
    public string QualitativeSummary { get; }
    public string NextGoal { get; }

    public bool IsCappedBySeverity => Level < UncappedLevel;

    public CaseEvaluation(IReadOnlyList<ScoreEntry> entries)
    {
        List<ScoreEntry> errors = entries.Where(e => !e.isCorrect).ToList();

        Competencies = entries
            .GroupBy(e => e.category)
            .OrderBy(group => group.Key)
            .Select(group => new CompetencyResult(group.Key, group.Count(e => e.isCorrect), group.Count()))
            .ToList();

        OverallRatio = Competencies.Count > 0 ? Competencies.Average(c => c.Ratio) : 0f;

        SevereErrors = errors.Count(e => e.severity == ErrorSeverity.Grave);
        ModerateErrors = errors.Count(e => e.severity == ErrorSeverity.Moderado);
        MildErrors = errors.Count(e => e.severity == ErrorSeverity.Leve);

        UncappedLevel = LevelForRatio(OverallRatio);
        Level = ApplySeverityCap(UncappedLevel, SevereErrors, ModerateErrors);

        QualitativeSummary = BuildQualitativeSummary(Competencies);
        NextGoal = BuildNextGoal(errors);
    }

    public CompetencyResult? Find(ScoreCategory category)
    {
        return Competencies.Where(c => c.Category == category).Cast<CompetencyResult?>().FirstOrDefault();
    }

    public static MasteryLevel LevelForRatio(float ratio)
    {
        if (ratio >= ConsistenteThreshold) return MasteryLevel.Consistente;
        if (ratio >= AdequadoThreshold) return MasteryLevel.Adequado;
        if (ratio >= EmProgressoThreshold) return MasteryLevel.EmProgresso;
        return MasteryLevel.EmDesenvolvimento;
    }

    public static MasteryLevel ApplySeverityCap(MasteryLevel level, int severeErrors, int moderateErrors)
    {
        if (severeErrors > 0 && level > MaxLevelWithSevereError)
            return MaxLevelWithSevereError;

        if (moderateErrors >= ModerateErrorsToBlockConsistente && level == MasteryLevel.Consistente)
            return MasteryLevel.Adequado;

        return level;
    }

    public static string LevelName(MasteryLevel level)
    {
        return level switch
        {
            MasteryLevel.EmDesenvolvimento => "Em desenvolvimento",
            MasteryLevel.EmProgresso => "Em progresso",
            MasteryLevel.Adequado => "Adequado",
            MasteryLevel.Consistente => "Consistente",
            _ => level.ToString()
        };
    }

    static string BuildQualitativeSummary(IReadOnlyList<CompetencyResult> competencies)
    {
        if (competencies.Count == 0)
            return "Nenhuma ação registrada nesta tentativa.";

        List<string> strengths = competencies
            .Where(IsStrong)
            .Select(c => StrengthPhrase(c.Category))
            .ToList();

        List<string> gaps = competencies
            .Where(c => !IsStrong(c))
            .OrderBy(c => c.Ratio)
            .Select(c => GapPhrase(c.Category))
            .ToList();

        if (gaps.Count == 0)
            return Sentence(JoinList(strengths));

        if (strengths.Count == 0)
            return Sentence($"Revise: {JoinList(gaps)}");

        return Sentence($"{JoinList(strengths)}, mas {JoinList(gaps)}");
    }

    static bool IsStrong(CompetencyResult competency) => competency.Ratio >= StrongCompetencyThreshold;

    static string StrengthPhrase(ScoreCategory category)
    {
        return category switch
        {
            ScoreCategory.Anamnese => "boa investigação na anamnese",
            ScoreCategory.PhysicalExam => "exame físico completo",
            ScoreCategory.LabExams => "solicitação de exames criteriosa",
            ScoreCategory.Diagnosis => "diagnóstico correto",
            _ => "bom desempenho"
        };
    }

    static string GapPhrase(ScoreCategory category)
    {
        return category switch
        {
            ScoreCategory.Anamnese => "anamnese com lacunas",
            ScoreCategory.PhysicalExam => "exame físico incompleto",
            ScoreCategory.LabExams => "seleção de exames a revisar",
            ScoreCategory.Diagnosis => "diagnóstico incorreto",
            _ => "etapa a revisar"
        };
    }

    static string BuildNextGoal(List<ScoreEntry> errors)
    {
        if (errors.Count == 0)
            return "Na próxima tentativa, mantenha a mesma sequência de raciocínio e confirme cada decisão com os achados do caso.";

        ScoreEntry focus = MostImportantError(errors);
        string topics = JoinList(TopicsLike(focus, errors));

        return $"Na próxima tentativa, {GoalFor(focus, topics)}.";
    }

    static ScoreEntry MostImportantError(List<ScoreEntry> errors)
    {
        return errors
            .OrderByDescending(e => e.severity)
            .ThenBy(e => e.category)
            .First();
    }

    static List<string> TopicsLike(ScoreEntry focus, List<ScoreEntry> errors)
    {
        return errors
            .Where(e => e.category == focus.category && e.kind == focus.kind)
            .Select(TopicOf)
            .Where(topic => !string.IsNullOrWhiteSpace(topic))
            .Distinct()
            .Take(MaxTopicsInGoal)
            .ToList();
    }

    static string GoalFor(ScoreEntry focus, string topics)
    {
        return (focus.category, focus.kind) switch
        {
            (ScoreCategory.Diagnosis, _) =>
                "antes de confirmar o diagnóstico, relacione os achados da anamnese, do exame físico e dos exames com os critérios de cada hipótese",
            (ScoreCategory.PhysicalExam, ErrorKind.OutOfPhase) =>
                "use os instrumentos somente na etapa de exame físico",
            (ScoreCategory.PhysicalExam, _) =>
                $"realize todos os exames físicos essenciais ({topics}) antes de solicitar exames complementares",
            (ScoreCategory.LabExams, ErrorKind.Unnecessary) =>
                $"evite solicitar exames que não ajudam a confirmar ou descartar suas hipóteses ({topics})",
            (ScoreCategory.LabExams, _) =>
                $"solicite os exames essenciais para este quadro ({topics})",
            (ScoreCategory.Anamnese, _) =>
                $"na anamnese, escolha as perguntas mais pertinentes em: {topics}",
            _ => "revise a etapa em que houve mais erros"
        };
    }

    static string TopicOf(ScoreEntry entry)
    {
        return string.IsNullOrWhiteSpace(entry.topic) ? entry.actionID : entry.topic;
    }

    static string JoinList(IReadOnlyList<string> items)
    {
        if (items.Count <= 1)
            return items.FirstOrDefault() ?? "";

        return $"{string.Join(", ", items.Take(items.Count - 1))} e {items[items.Count - 1]}";
    }

    static string Sentence(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return char.ToUpperInvariant(text[0]) + text.Substring(1) + ".";
    }
}
