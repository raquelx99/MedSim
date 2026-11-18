using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FeedbackManager : MonoBehaviour
{
    private const string Gap = "<line-height=60%>\n</line-height>\n";
    private const string ValueColumn = "<pos=36%>";
    private const string DeltaColumn = "<pos=78%>";
    private const string DurationColumn = "<pos=50%>";
    private const int BarSegments = 12;

    [Header("UI References")]
    public GameObject feedbackPanel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI contentText;
    public TextMeshProUGUI pageIndicatorText;
    public Button nextButton;
    public Button prevButton;
    public Button retryButton;

    [Header("Dependencies")]
    public ScoreManager scoreManager;
    public PhaseManager phaseManager;

    [Header("Cores")]
    public Color accentColor = new Color32(0x15, 0x36, 0x67, 0xFF);
    public Color correctColor = new Color32(0x2F, 0x85, 0x5A, 0xFF);
    public Color wrongColor = new Color32(0xC5, 0x30, 0x30, 0xFF);
    public Color warningColor = new Color32(0xB7, 0x79, 0x1F, 0xFF);
    public Color mutedColor = new Color32(0x4A, 0x55, 0x68, 0xFF);
    public Color barTrackColor = new Color32(0xD9, 0xDE, 0xE5, 0xFF);

    private readonly List<Section> sections = new();
    private readonly List<Page> pages = new();
    private int currentPage;

    private readonly struct Section
    {
        public readonly string Title;
        public readonly string Content;

        public Section(string title, string content)
        {
            Title = title;
            Content = content;
        }
    }

    private readonly struct Page
    {
        public readonly int SectionIndex;
        public readonly int TextPage;

        public Page(int sectionIndex, int textPage)
        {
            SectionIndex = sectionIndex;
            TextPage = textPage;
        }
    }

    public void ShowFeedback()
    {
        feedbackPanel.SetActive(true);

        List<ScoreEntry> entries = scoreManager.GetAllEntries().ToList();
        var evaluation = new CaseEvaluation(entries);

        AttemptRecord previous = AttemptHistory.GetLast(phaseManager.CaseID);
        AttemptRecord current = AttemptHistory.Record(phaseManager.CaseID, evaluation, phaseManager.totalPhaseTime);

        BuildSections(entries, evaluation, previous, current);
        SplitSectionsIntoPages();
        BindButtons();

        currentPage = 0;
        ShowCurrentPage();
    }

    void BindButtons()
    {
        nextButton.onClick.RemoveAllListeners();
        prevButton.onClick.RemoveAllListeners();
        retryButton.onClick.RemoveAllListeners();

        nextButton.onClick.AddListener(OnNext);
        prevButton.onClick.AddListener(OnPrev);
        retryButton.onClick.AddListener(OnRetry);
    }

    void OnNext()
    {
        if (currentPage >= pages.Count - 1) return;

        currentPage++;
        ShowCurrentPage();
    }

    void OnPrev()
    {
        if (currentPage <= 0) return;

        currentPage--;
        ShowCurrentPage();
    }

    void OnRetry()
    {
        feedbackPanel.SetActive(false);
        phaseManager.RestartPhase();
    }

    void SplitSectionsIntoPages()
    {
        pages.Clear();
        contentText.overflowMode = TextOverflowModes.Page;
        Canvas.ForceUpdateCanvases();

        for (int sectionIndex = 0; sectionIndex < sections.Count; sectionIndex++)
        {
            contentText.text = sections[sectionIndex].Content;
            contentText.pageToDisplay = 1;
            contentText.ForceMeshUpdate();

            int textPages = Mathf.Max(1, contentText.textInfo.pageCount);
            for (int textPage = 1; textPage <= textPages; textPage++)
                pages.Add(new Page(sectionIndex, textPage));
        }
    }

    void ShowCurrentPage()
    {
        Page page = pages[currentPage];
        Section section = sections[page.SectionIndex];
        bool isFirst = currentPage == 0;
        bool isLast = currentPage == pages.Count - 1;

        titleText.text = section.Title;
        contentText.text = section.Content;
        contentText.pageToDisplay = page.TextPage;

        prevButton.gameObject.SetActive(!isFirst);
        nextButton.gameObject.SetActive(!isLast);
        retryButton.gameObject.SetActive(isLast);

        if (pageIndicatorText != null)
            pageIndicatorText.text = $"{currentPage + 1} / {pages.Count}";
    }

    void BuildSections(List<ScoreEntry> entries, CaseEvaluation evaluation, AttemptRecord previous, AttemptRecord current)
    {
        sections.Clear();
        sections.Add(new Section("Resumo do atendimento", BuildSummary(evaluation, current)));

        if (previous != null)
            sections.Add(new Section("Evolução desde a última tentativa", BuildEvolution(evaluation, previous)));

        List<ScoreEntry> hits = entries.Where(e => e.isCorrect).ToList();
        if (hits.Count > 0)
            sections.Add(new Section("O que você fez bem", BuildStrengths(hits)));

        foreach (IGrouping<ScoreCategory, ScoreEntry> errors in GroupByCategory(entries.Where(e => !e.isCorrect)))
            sections.Add(new Section($"Para revisar: {DisplayNames.Category(errors.Key)}", BuildReview(errors)));

        sections.Add(new Section("Tempo de atendimento", BuildTime(evaluation, previous)));
        sections.Add(new Section("Próximo objetivo", BuildNextSteps(entries, evaluation)));
    }

    string BuildSummary(CaseEvaluation evaluation, AttemptRecord current)
    {
        var text = new StringBuilder();
        AppendMasteryHeader(text, evaluation, current);
        AppendQualitativeAnalysis(text, evaluation);
        AppendCompetencyBars(text, evaluation);
        AppendSeverityCounts(text, evaluation);
        return text.ToString();
    }

    void AppendMasteryHeader(StringBuilder text, CaseEvaluation evaluation, AttemptRecord current)
    {
        float score = Mathf.Max(0f, scoreManager.GetCurrentTotalPoints());

        text.Append("<align=center>");
        text.Append(Colored("Domínio do caso", mutedColor)).Append('\n');
        text.Append(Size(Bold(Colored(CaseEvaluation.LevelName(evaluation.Level), LevelColor(evaluation.Level))), 170)).Append('\n');
        text.Append(Colored($"{score:F0} pontos  ·  tentativa {current.attemptNumber}", mutedColor)).Append('\n');
        text.Append("</align>");
        text.Append(Gap);
    }

    void AppendQualitativeAnalysis(StringBuilder text, CaseEvaluation evaluation)
    {
        text.Append(Italic(evaluation.QualitativeSummary)).Append('\n');

        if (evaluation.IsCappedBySeverity)
        {
            string reason = evaluation.SevereErrors > 0 ? "Um erro grave" : "Erros moderados recorrentes";
            string level = CaseEvaluation.LevelName(evaluation.Level);
            text.Append(Colored($"{reason} limitou a classificação a {level}.", wrongColor)).Append('\n');
        }

        text.Append(Gap);
    }

    void AppendCompetencyBars(StringBuilder text, CaseEvaluation evaluation)
    {
        text.Append(Heading("Desempenho por competência"));
        foreach (CompetencyResult competency in evaluation.Competencies)
        {
            Color color = RatioColor(competency.Ratio);
            text.Append(DisplayNames.CategoryShort(competency.Category));
            text.Append(ValueColumn).Append(Bar(competency.Ratio, color));
            text.Append(DeltaColumn).Append(Bold(Colored(Percent(competency.Ratio), color))).Append('\n');
        }

        text.Append(Gap);
    }

    void AppendSeverityCounts(StringBuilder text, CaseEvaluation evaluation)
    {
        text.Append(Heading("Gravidade dos erros"));
        text.Append(SeverityCount(evaluation.SevereErrors, "grave", "graves", wrongColor)).Append("   ");
        text.Append(SeverityCount(evaluation.ModerateErrors, "moderado", "moderados", warningColor)).Append("   ");
        text.Append(SeverityCount(evaluation.MildErrors, "leve", "leves", mutedColor)).Append('\n');
    }

    string SeverityCount(int count, string singular, string plural, Color colorWhenPresent)
    {
        string label = $"{count} {(count == 1 ? singular : plural)}";
        return Colored(label, count > 0 ? colorWhenPresent : mutedColor);
    }

    string BuildEvolution(CaseEvaluation evaluation, AttemptRecord previous)
    {
        var text = new StringBuilder();
        text.Append(Colored($"Comparação com a tentativa {previous.attemptNumber}.", mutedColor)).Append('\n');
        text.Append(Gap);

        text.Append(Heading("Domínio do caso"));
        string currentLevel = Bold(Colored(CaseEvaluation.LevelName(evaluation.Level), LevelColor(evaluation.Level)));
        text.Append($"{CaseEvaluation.LevelName(previous.level)} para {currentLevel}\n");
        text.Append(Gap);

        text.Append(Heading("Competências"));
        foreach (CompetencyResult competency in evaluation.Competencies)
            AppendCompetencyEvolution(text, competency, previous);
        text.Append(Gap);

        text.Append(Heading("Erros graves"));
        Color severeColor = LowerIsBetterColor(evaluation.SevereErrors - previous.severeErrors);
        text.Append($"{previous.severeErrors} para {Bold(Colored(evaluation.SevereErrors.ToString(), severeColor))}\n");

        if (!string.IsNullOrWhiteSpace(previous.nextGoal))
        {
            text.Append(Gap);
            text.Append(Heading("Meta desta tentativa"));
            text.Append(Colored(previous.nextGoal, mutedColor)).Append('\n');
        }

        return text.ToString();
    }

    void AppendCompetencyEvolution(StringBuilder text, CompetencyResult competency, AttemptRecord previous)
    {
        text.Append(DisplayNames.CategoryShort(competency.Category)).Append(ValueColumn);

        if (previous.competencyRatios.TryGetValue(competency.Category, out float before))
            text.Append($"{Percent(before)} para {Bold(Percent(competency.Ratio))}{DeltaColumn}{Delta(competency.Ratio - before)}");
        else
            text.Append(Bold(Percent(competency.Ratio)));

        text.Append('\n');
    }

    string BuildStrengths(List<ScoreEntry> hits)
    {
        var text = new StringBuilder();
        foreach (IGrouping<ScoreCategory, ScoreEntry> category in GroupByCategory(hits))
        {
            text.Append(Bold(Colored(DisplayNames.Category(category.Key), correctColor))).Append('\n');
            foreach (string topic in category.Select(TopicOf).Distinct())
                text.Append($"• {EscapeRichText(topic)}\n");
            text.Append(Gap);
        }

        return text.ToString();
    }

    string BuildReview(IGrouping<ScoreCategory, ScoreEntry> errors)
    {
        List<(ScoreEntry error, int count)> uniqueErrors = errors
            .GroupBy(e => (TopicOf(e), e.chosenAnswer, e.expectedAnswer, e.justification))
            .Select(repeated => (error: repeated.OrderByDescending(e => e.severity).First(), count: repeated.Count()))
            .OrderByDescending(item => item.error.severity)
            .ToList();

        bool answersAreSpeech = errors.Key == ScoreCategory.Anamnese;

        var text = new StringBuilder();
        string intro = uniqueErrors.Count == 1 ? "1 ponto para revisar nesta etapa." : $"{uniqueErrors.Count} pontos para revisar nesta etapa.";
        text.Append(Colored(intro, mutedColor)).Append('\n');
        text.Append(Gap);

        foreach ((ScoreEntry error, int count) in uniqueErrors)
        {
            text.Append(SeverityTag(error.severity)).Append(' ');
            text.Append(Bold(Colored(EscapeRichText(TopicOf(error)), accentColor)));
            if (count > 1)
                text.Append(Colored($"  ({count}x)", mutedColor));
            text.Append('\n');

            text.Append("<indent=4%>");
            AppendLabeledLine(text, "Você:", wrongColor, FormatAnswer(error.chosenAnswer, answersAreSpeech));
            AppendLabeledLine(text, "Esperado:", correctColor, FormatAnswer(error.expectedAnswer, answersAreSpeech));
            if (!string.IsNullOrWhiteSpace(error.justification))
                text.Append(Colored($"{Bold("Por quê:")} {EscapeRichText(error.justification)}", mutedColor)).Append('\n');
            text.Append("</indent>");
            text.Append(Gap);
        }

        return text.ToString();
    }

    string BuildTime(CaseEvaluation evaluation, AttemptRecord previous)
    {
        var text = new StringBuilder();
        text.Append("<align=center>");
        text.Append(Size(Bold(Colored(FormatDuration(phaseManager.totalPhaseTime), accentColor)), 170)).Append('\n');
        text.Append(Colored("sem penalização por tempo nesta modalidade", mutedColor)).Append('\n');
        text.Append("</align>");
        text.Append(Gap);

        text.Append(Heading("Por etapa"));
        AppendDuration(text, "Anamnese", PhaseManager.Part.Anamnese);
        AppendDuration(text, "Exame físico", PhaseManager.Part.Exam);
        AppendDuration(text, "Exames", PhaseManager.Part.Labs, PhaseManager.Part.Transition);
        AppendDuration(text, "Diagnóstico", PhaseManager.Part.Diagnosis);

        if (previous != null && previous.totalSeconds > 0f)
            text.Append(Colored($"Tentativa anterior: {FormatDuration(previous.totalSeconds)}", mutedColor)).Append('\n');
        text.Append(Gap);

        text.Append(Colored(TimeReflection(evaluation.Level), mutedColor));
        return text.ToString();
    }

    void AppendDuration(StringBuilder text, string label, params PhaseManager.Part[] parts)
    {
        text.Append($"{label}{DurationColumn}{FormatDuration(phaseManager.DurationOf(parts))}\n");
    }

    static string TimeReflection(MasteryLevel level)
    {
        return level >= MasteryLevel.Adequado
            ? "Com desempenho clínico adequado, use o tempo para refletir sobre a fluidez do atendimento ao longo das tentativas."
            : "Priorize a qualidade das decisões. O tempo só é um bom indicador quando o desempenho clínico já está adequado.";
    }

    string BuildNextSteps(List<ScoreEntry> entries, CaseEvaluation evaluation)
    {
        var text = new StringBuilder();
        text.Append(Bold(Colored(evaluation.NextGoal, warningColor))).Append('\n');
        text.Append(Gap);

        List<ScoreCategory> categoriesWithErrors = entries
            .Where(e => !e.isCorrect)
            .Select(e => e.category)
            .Distinct()
            .OrderBy(category => category)
            .ToList();

        if (categoriesWithErrors.Count == 0)
        {
            text.Append(Bold(Colored("Excelente atendimento!", correctColor))).Append('\n');
            text.Append("Você conduziu todas as etapas corretamente.\n");
        }
        else
        {
            text.Append(Heading("Para estudar"));
            foreach (ScoreCategory category in categoriesWithErrors)
                text.Append($"• {Bold(DisplayNames.Category(category))}: {StudySuggestion(category)}\n");
        }
        text.Append(Gap);

        text.Append(Colored($"Toque em {Bold("Tentar novamente")} para refazer o caso. A meta ficará visível na prancheta.", mutedColor));
        return text.ToString();
    }

    static string StudySuggestion(ScoreCategory category)
    {
        return category switch
        {
            ScoreCategory.Anamnese => "siga a ordem da anamnese: queixa principal, história da doença atual, antecedentes, medicações, história familiar e hábitos.",
            ScoreCategory.PhysicalExam => "use os instrumentos apenas na etapa de exame físico e confira os sinais vitais essenciais para o quadro.",
            ScoreCategory.LabExams => "peça apenas os exames que ajudam a confirmar ou descartar suas hipóteses.",
            ScoreCategory.Diagnosis => "relacione os achados da anamnese, do exame físico e dos exames com os critérios de cada hipótese.",
            _ => "revise essa etapa para fortalecer seu raciocínio clínico."
        };
    }

    string SeverityTag(ErrorSeverity severity)
    {
        Color color = severity switch
        {
            ErrorSeverity.Grave => wrongColor,
            ErrorSeverity.Moderado => warningColor,
            _ => mutedColor
        };
        return Size(Bold(Colored($"[{DisplayNames.Severity(severity)}]", color)), 80);
    }

    string Bar(float ratio, Color fillColor)
    {
        int filled = Mathf.Clamp(Mathf.RoundToInt(ratio * BarSegments), 0, BarSegments);
        int empty = BarSegments - filled;

        return "<mspace=0.55em><color=#00000000>" +
               Highlighted(new string('0', filled), fillColor) +
               Highlighted(new string('0', empty), barTrackColor) +
               "</color></mspace>";
    }

    string Delta(float ratioDelta)
    {
        int points = Mathf.RoundToInt(ratioDelta * 100f);
        if (points == 0)
            return Colored("=", mutedColor);

        string label = points > 0 ? $"+{points}" : points.ToString();
        return Bold(Colored(label, points > 0 ? correctColor : wrongColor));
    }

    Color LowerIsBetterColor(int delta)
    {
        if (delta < 0) return correctColor;
        if (delta > 0) return wrongColor;
        return mutedColor;
    }

    Color LevelColor(MasteryLevel level)
    {
        if (level >= MasteryLevel.Adequado) return correctColor;
        if (level == MasteryLevel.EmProgresso) return warningColor;
        return wrongColor;
    }

    Color RatioColor(float ratio)
    {
        if (ratio >= CaseEvaluation.AdequadoThreshold) return correctColor;
        if (ratio >= CaseEvaluation.EmProgressoThreshold) return warningColor;
        return wrongColor;
    }

    string Heading(string title) => Bold(Colored(title, accentColor)) + "\n";

    static IEnumerable<IGrouping<ScoreCategory, ScoreEntry>> GroupByCategory(IEnumerable<ScoreEntry> entries)
    {
        return entries.GroupBy(e => e.category).OrderBy(group => group.Key);
    }

    static string FormatDuration(float seconds)
    {
        int total = Mathf.Max(0, Mathf.RoundToInt(seconds));
        return $"{total / 60}m{total % 60:00}s";
    }

    static string Percent(float ratio) => $"{ratio * 100f:F0}%";

    static string TopicOf(ScoreEntry entry)
    {
        return string.IsNullOrWhiteSpace(entry.topic) ? entry.actionID : entry.topic;
    }

    static void AppendLabeledLine(StringBuilder text, string label, Color labelColor, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            text.Append($"{Bold(Colored(label, labelColor))} {value}\n");
    }

    static string FormatAnswer(string answer, bool isSpeech)
    {
        if (string.IsNullOrWhiteSpace(answer))
            return null;

        string escaped = EscapeRichText(answer.Trim());
        return isSpeech ? $"<i>\"{escaped}\"</i>" : escaped;
    }

    static string Bold(string text) => $"<b>{text}</b>";

    static string Italic(string text) => $"<i>{text}</i>";

    static string Size(string text, int percent) => $"<size={percent}%>{text}</size>";

    static string Colored(string text, Color color) => $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";

    static string Highlighted(string text, Color color) => $"<mark=#{ColorUtility.ToHtmlStringRGBA(color)}>{text}</mark>";

    static string EscapeRichText(string text) => string.IsNullOrEmpty(text) ? text : $"<noparse>{text}</noparse>";
}
