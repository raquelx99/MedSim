using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using System.Text;
using System.Collections.Generic;

public class FeedbackManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject feedbackPanel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI contentText;
    public Button nextButton;
    public Button prevButton;
    public Button retryButton;

    [Header("Dependencies")]
    public ScoreManager scoreManager;
    public PhaseManager phaseManager;

    private int currentPage = 0;
    private List<(string title, string content)> pages = new List<(string, string)>();

    public void ShowFeedback()
    {
        feedbackPanel.SetActive(true);
        currentPage = 0;
        retryButton.gameObject.SetActive(false);

        pages.Clear();

        var entries = scoreManager.GetAllEntries().ToList();
        float finalScore = scoreManager.GetCurrentTotalPoints();

        pages.Add(("Pontuação Final", $"Pontuação: {finalScore:F0}"));

        var hitsByCat = entries
            .Where(e => e.isCorrect)
            .GroupBy(e => e.category)
            .Select(g => $"{g.Key}: {g.Count()} acertos");
        pages.Add(("Acertos", string.Join("\n", hitsByCat)));

        var errorsByCat = entries
            .Where(e => !e.isCorrect)
            .GroupBy(e => e.category);

        foreach (var group in errorsByCat)
        {
            var sb = new StringBuilder();
            foreach (var e in group)
                sb.AppendLine($"- \"{e.actionID}\": {e.justification}");
            pages.Add(($"Erros - {group.Key}", sb.ToString()));
        }

        pages.Add(("Análise Clínica", GenerateAnalysis(entries)));

        pages.Add(("Dicas para Melhorar", GenerateTips(entries)));

        nextButton.onClick.RemoveAllListeners();
        prevButton.onClick.RemoveAllListeners();
        retryButton.onClick.RemoveAllListeners();

        nextButton.onClick.AddListener(OnNext);
        prevButton.onClick.AddListener(OnPrev);
        retryButton.onClick.AddListener(() =>
        {
            feedbackPanel.SetActive(false);
            phaseManager.RestartPhase();
        });

        UpdatePage();
    }

    void OnNext()
    {
        if (currentPage < pages.Count - 1)
        {
            currentPage++;
            UpdatePage();
        }
    }

    void OnPrev()
    {
        if (currentPage > 0)
        {
            currentPage--;
            UpdatePage();
        }
    }

    void UpdatePage()
    {
        prevButton.gameObject.SetActive(currentPage > 0);
        nextButton.gameObject.SetActive(currentPage < pages.Count - 1);
        retryButton.gameObject.SetActive(currentPage == pages.Count - 1);

        titleText.text = pages[currentPage].title;
        contentText.text = pages[currentPage].content;
    }

    private string GenerateAnalysis(List<ScoreEntry> entries)
    {
        var errors = entries.Where(e => !e.isCorrect).ToList();
        if (errors.Count == 0)
            return "Parabéns! Você respondeu corretamente todas as etapas da fase de anamnese e diagnóstico.";

        var sb = new StringBuilder();
        sb.AppendLine("Na fase de anamnese e diagnóstico, foram identificados problemas nos seguintes pontos:");

        var errorGroups = errors.GroupBy(e => e.category);
        foreach (var group in errorGroups)
        {
            sb.AppendLine($"- {group.Count()} erro(s) em {group.Key}");
            sb.AppendLine($"  Sugestão: {MapSuggestion(group.Key)}");
        }

        return sb.ToString();
    }

    private string MapSuggestion(ScoreCategory category)
    {
        switch (category)
        {
            case ScoreCategory.Anamnese:
                return "Revise como formular perguntas abertas para obter informações clínicas completas.";
            case ScoreCategory.PhysicalExam:
                return "Garanta que todos os principais sinais vitais e exames físicos sejam verificados.";
            case ScoreCategory.LabExams:
                return "Considere quais exames laboratoriais são mais indicados e seus critérios de solicitação.";
            case ScoreCategory.Diagnosis:
                return "Verifique se todos os critérios diagnósticos estão atendidos antes de confirmar.";
            default:
                return "Revisite essa etapa para melhorar seu raciocínio clínico.";
        }
    }

    private string GenerateTips(List<ScoreEntry> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("- Pratique casos clínicos com foco na coleta de anamnese completa.");
        sb.AppendLine("- Use checklist de exame físico para não esquecer nenhum passo.");
        sb.AppendLine("- Consulte protocolos laboratoriais antes de solicitar exames.");
        sb.AppendLine("- Compare seu diagnóstico com especialistas para receber feedback.");
        return sb.ToString();
    }
}
