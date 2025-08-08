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

    int currentPage = 0;
    const int totalPages = 5;

    public void ShowFeedback()
    {
        feedbackPanel.SetActive(true);
        currentPage = 0;
        retryButton.gameObject.SetActive(false);
        UpdatePage();

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
    }

    void OnNext()
    {
        if (currentPage < totalPages - 1)
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
        // controle de visibilidade dos botões
        prevButton.gameObject.SetActive(currentPage > 0);
        nextButton.gameObject.SetActive(currentPage < totalPages - 1);
        retryButton.gameObject.SetActive(currentPage == totalPages - 1);

        // prepara os dados comuns
        var entries = scoreManager.GetAllEntries();
        var diagEntry = entries.LastOrDefault(e => e.category == ScoreCategory.Diagnosis);
        bool diagCorrect = diagEntry != null && diagEntry.isCorrect;
        float finalScore = scoreManager.GetCurrentTotalPoints();

        switch (currentPage)
        {
            case 0:
                titleText.text = "Pontuação Final";
                contentText.text = $"Pontuação: {finalScore:F0}";
                break;

            case 1:
                titleText.text = "Acertos";
                var hitsByCat = entries
                    .Where(e => e.isCorrect)
                    .GroupBy(e => e.category)
                    .Select(g => $"{g.Key}: {g.Count()} acertos");
                contentText.text = string.Join("\n", hitsByCat);
                break;

            case 2:
                titleText.text = "Erros";
                var missesByCat = entries
                    .Where(e => !e.isCorrect)
                    .Select(e => $"- [{e.category}] \"{e.actionID}\": {e.justification}");
                contentText.text = string.Join("\n", missesByCat);
                break;

            case 3:
                titleText.text = "Análise Clínica";
                contentText.text =
                    "O paciente apresentou sintomas inespecíficos (tontura, cefaleia),\n" +
                    "e a confirmação só ocorreu após exame físico correto.\n" +
                    "Erros em solicitações de exames indicam necessidade de focar\n" +
                    "no raciocínio clínico antes de pedir recursos adicionais.";
                break;

            case 4:
                titleText.text = "Dicas para Melhorar";
                contentText.text =
                    "- Revise sempre o histórico completo do paciente antes de solicitar exames.\n" +
                    "- Pratique fluxogramas de decisão diagnóstica.\n" +
                    "- Treine a comunicação empática para obter informações mais precisas.\n" +
                    "- Use casos clínicos simulados para reforçar o raciocínio.";
                break;
        }
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
