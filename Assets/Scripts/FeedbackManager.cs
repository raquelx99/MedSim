using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using System.Text;

public class FeedbackManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject feedbackPanel;
    public TextMeshProUGUI resultMessageText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI metricsDetailText;
    public TextMeshProUGUI analysisText;
    public Button retryButton;

    [Header("Dependencies")]
    public ScoreManager scoreManager;
    public PhaseManager phaseManager;

    public void ShowFeedback()
    {
        feedbackPanel.SetActive(true);

        var diagEntry = scoreManager
            .GetAllEntries()
            .LastOrDefault(e => e.category == ScoreCategory.Diagnosis);

        bool diagCorrect = diagEntry != null && diagEntry.isCorrect;

        resultMessageText.gameObject.SetActive(true);
        resultMessageText.text = diagCorrect ? "ACERTOU!" : "ERROU!";
        resultMessageText.color = diagCorrect ? Color.green : Color.red;

        float finalScore = scoreManager.GetCurrentTotalPoints();
        scoreText.text = $"Pontuação: {finalScore:F0}";

        var entries = scoreManager.GetAllEntries();
        var sb = new StringBuilder();
        sb.AppendLine($"⏱ Tempo: {phaseManager.totalPhaseTime:F1}s\n");

        foreach (ScoreCategory cat in System.Enum.GetValues(typeof(ScoreCategory)))
        {
            var catEntries = entries.Where(e => e.category == cat).ToList();
            int hits = catEntries.Count(e => e.isCorrect);
            int misses = catEntries.Count(e => !e.isCorrect);
            sb.AppendLine($"{cat}: {hits} acertos, {misses} erros");
        }

        sb.AppendLine("\nErros detalhados:");
        foreach (var err in entries.Where(e => !e.isCorrect))
        {
            sb.AppendLine($"- [{err.category}] “{err.actionID}”: {err.justification}");
        }

        metricsDetailText.text = sb.ToString();

        // 3. Análise Clínica
        // Você pode gerar dinamicamente com base nas principais falhas,
        // ou usar um texto genérico predefinido:
        analysisText.text =
            "O paciente apresentou sintomas inespecíficos (tontura, cefaleia),\n" +
            "e a confirmação só ocorreu após exame físico correto.\n" +
            "Erros em solicitações de exames indicam necessidade de focar\n" +
            "no raciocínio clínico antes de pedir recursos adicionais.";

        // 5. Botões
        retryButton.onClick.RemoveAllListeners();
        retryButton.onClick.AddListener(() => {
            feedbackPanel.SetActive(false);
            phaseManager.RestartPhase();
        });
    }
}
