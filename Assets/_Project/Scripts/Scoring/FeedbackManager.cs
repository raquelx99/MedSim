using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FeedbackManager : MonoBehaviour
{
    private const string Gap = "<line-height=60%>\n</line-height>\n";

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

        BuildSections(scoreManager.GetAllEntries().ToList());
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

    void BuildSections(List<ScoreEntry> entries)
    {
        sections.Clear();
        sections.Add(new Section("Resumo do atendimento", BuildSummary(entries)));

        List<ScoreEntry> hits = entries.Where(e => e.isCorrect).ToList();
        if (hits.Count > 0)
            sections.Add(new Section("O que você fez bem", BuildStrengths(hits)));

        foreach (IGrouping<ScoreCategory, ScoreEntry> errors in GroupByCategory(entries.Where(e => !e.isCorrect)))
            sections.Add(new Section($"Para revisar: {DisplayNames.Category(errors.Key)}", BuildReview(errors)));

        sections.Add(new Section("Próximos passos", BuildNextSteps(entries)));
    }

    string BuildSummary(List<ScoreEntry> entries)
    {
        int total = entries.Count;
        int correct = entries.Count(e => e.isCorrect);
        float ratio = Ratio(correct, total);
        float score = Mathf.Max(0f, scoreManager.GetCurrentTotalPoints());

        var text = new StringBuilder();
        text.Append("<align=center>");
        text.Append(Size(Bold(Colored(score.ToString("F0"), accentColor)), 240)).Append('\n');
        text.Append(Colored("pontos", mutedColor)).Append('\n');
        text.Append(Gap);
        text.Append(Size(Bold(Colored(GradeLabel(ratio), GradeColor(ratio))), 120)).Append('\n');
        text.Append(Colored($"{correct} de {total} ações corretas ({ratio * 100f:F0}%)", mutedColor)).Append('\n');
        text.Append("</align>");
        text.Append(Gap);

        text.Append(Bold(Colored("Desempenho por etapa", accentColor))).Append('\n');
        foreach (IGrouping<ScoreCategory, ScoreEntry> category in GroupByCategory(entries))
        {
            int categoryHits = category.Count(e => e.isCorrect);
            int categoryTotal = category.Count();
            string result = Bold(Colored($"{categoryHits}/{categoryTotal}", GradeColor(Ratio(categoryHits, categoryTotal))));
            text.Append($"• {DisplayNames.Category(category.Key)}: {result}\n");
        }

        return text.ToString();
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
            .Select(repeated => (repeated.First(), repeated.Count()))
            .ToList();

        bool answersAreSpeech = errors.Key == ScoreCategory.Anamnese;

        var text = new StringBuilder();
        string intro = uniqueErrors.Count == 1 ? "1 ponto para revisar nesta etapa." : $"{uniqueErrors.Count} pontos para revisar nesta etapa.";
        text.Append(Colored(intro, mutedColor)).Append('\n');
        text.Append(Gap);

        foreach ((ScoreEntry error, int count) in uniqueErrors)
        {
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

    string BuildNextSteps(List<ScoreEntry> entries)
    {
        List<ScoreCategory> categoriesWithErrors = entries
            .Where(e => !e.isCorrect)
            .Select(e => e.category)
            .Distinct()
            .OrderBy(category => category)
            .ToList();

        var text = new StringBuilder();
        if (categoriesWithErrors.Count == 0)
        {
            text.Append(Bold(Colored("Excelente atendimento!", correctColor))).Append('\n');
            text.Append(Gap);
            text.Append("Você conduziu todas as etapas corretamente. Tente refazer o caso buscando a mesma precisão com mais agilidade.\n");
        }
        else
        {
            text.Append("No próximo atendimento, foque nestes pontos:\n");
            text.Append(Gap);
            foreach (ScoreCategory category in categoriesWithErrors)
            {
                text.Append(Bold(Colored(DisplayNames.Category(category), warningColor))).Append('\n');
                text.Append($"<indent=4%>{StudySuggestion(category)}</indent>\n");
                text.Append(Gap);
            }
        }

        text.Append(Colored($"Toque em {Bold("Tentar novamente")} para refazer o caso.", mutedColor));
        return text.ToString();
    }

    static string StudySuggestion(ScoreCategory category)
    {
        return category switch
        {
            ScoreCategory.Anamnese => "Siga a ordem da anamnese: queixa principal, história da doença atual, antecedentes, medicações, história familiar e hábitos. Prefira perguntas abertas antes das específicas.",
            ScoreCategory.PhysicalExam => "Use os instrumentos apenas na etapa de exame físico e confira se mediu os sinais vitais essenciais para o quadro do paciente.",
            ScoreCategory.LabExams => "Peça apenas os exames que ajudam a confirmar ou descartar suas hipóteses. Exames desnecessários também contam como erro.",
            ScoreCategory.Diagnosis => "Antes de confirmar, relacione os achados da anamnese, do exame físico e dos exames com os critérios de cada hipótese.",
            _ => "Revise essa etapa para fortalecer seu raciocínio clínico."
        };
    }

    static string GradeLabel(float ratio)
    {
        if (ratio >= 0.85f) return "Excelente";
        if (ratio >= 0.65f) return "Bom desempenho";
        if (ratio >= 0.4f) return "Regular";
        return "Precisa revisar";
    }

    Color GradeColor(float ratio)
    {
        if (ratio >= 0.65f) return correctColor;
        if (ratio >= 0.4f) return warningColor;
        return wrongColor;
    }

    static IEnumerable<IGrouping<ScoreCategory, ScoreEntry>> GroupByCategory(IEnumerable<ScoreEntry> entries)
    {
        return entries.GroupBy(e => e.category).OrderBy(group => group.Key);
    }

    static float Ratio(int part, int total)
    {
        return total > 0 ? (float)part / total : 0f;
    }

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

    static string Size(string text, int percent) => $"<size={percent}%>{text}</size>";

    static string Colored(string text, Color color) => $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";

    static string EscapeRichText(string text) => string.IsNullOrEmpty(text) ? text : $"<noparse>{text}</noparse>";
}
