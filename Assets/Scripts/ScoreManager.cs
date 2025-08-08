using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[System.Serializable]
public struct ScoreMapping
{
    public string id; 
    public int correctPoints;
    public int incorrectPenalty;
}
public class ScoreManager : MonoBehaviour
{
    private List<ScoreEntry> entries = new();
    [Header("CONFIGURAÇÃO DE PONTOS DA FASE")]
    [Tooltip("Pontos para cada pergunta da Anamnese. 'ID' deve ser o texto exato do 'prompt' da pergunta.")]
    public List<ScoreMapping> anamnesisScores;

    [Tooltip("Pontos para cada item do Exame Físico. 'ID' é o 'objectID' do item.")]
    public List<ScoreMapping> physicalExamScores;

    [Tooltip("Pontos para cada pedido de Exame Laboratorial. 'ID' é o 'examID' do exame.")]
    public List<ScoreMapping> labExamScores;

    [Tooltip("Pontos para o Diagnóstico Final. 'ID' é o texto exato do diagnóstico.")]
    public List<ScoreMapping> diagnosisScores;
    private const int maxScore = 100;

    public int GetPointsForAnamnesis(string questionPrompt, bool isCorrect)
    {
        ScoreMapping rule = anamnesisScores.FirstOrDefault(s => s.id == questionPrompt);
        if (rule.id != null)
        {
            return isCorrect ? rule.correctPoints : -rule.incorrectPenalty;
        }
        return isCorrect ? 2 : 0;
    }

    public int GetPointsForPhysicalExam(string itemID, bool isCorrect)
    {
        ScoreMapping rule = physicalExamScores.FirstOrDefault(s => s.id == itemID);
        if (rule.id != null)
        {
            return isCorrect ? rule.correctPoints : -rule.incorrectPenalty;
        }
        return isCorrect ? 20 : -1;
    }

    public int GetPointsForLabExam(string examID, bool isCorrect)
    {
        ScoreMapping rule = labExamScores.FirstOrDefault(s => s.id == examID);
        if (rule.id != null)
        {
            return isCorrect ? rule.correctPoints : -rule.incorrectPenalty;
        }
        return isCorrect ? 20 : -1;
    }

    public int GetPointsForDiagnosis(string diagnosisText, bool isCorrect)
    {
        ScoreMapping rule = diagnosisScores.FirstOrDefault(s => s.id == diagnosisText);
        if (rule.id != null)
        {
            return isCorrect ? rule.correctPoints : -rule.incorrectPenalty;
        }
        return isCorrect ? 40 : -20;
    }

    public void RegisterScoreEntry(ScoreEntry entry)
    {
        entries.Add(entry);
    }

    public float GetCurrentTotalPoints()
    {
        float totalPoints = 0f;
        foreach (var entry in entries)
        {
            totalPoints += entry.points;
        }
        return totalPoints;
    }

    public string GetScoreSummary()
    {
        Dictionary<ScoreCategory, int> acertosPorCategoria = new();
        Dictionary<ScoreCategory, int> errosPorCategoria = new();

        int totalAcertos = 0;
        int totalErros = 0;

        foreach (var entry in entries)
        {
            if (entry.isCorrect)
            {
                totalAcertos++;
                if (!acertosPorCategoria.ContainsKey(entry.category))
                    acertosPorCategoria[entry.category] = 0;
                acertosPorCategoria[entry.category]++;
            }
            else
            {
                totalErros++;
                if (!errosPorCategoria.ContainsKey(entry.category))
                    errosPorCategoria[entry.category] = 0;
                errosPorCategoria[entry.category]++;
            }
        }


        float finalScore = GetCurrentTotalPoints();
        finalScore = Mathf.Max(0, finalScore);

        string summary = $"Pontuação final: {finalScore}\n\n";
        foreach (ScoreCategory category in System.Enum.GetValues(typeof(ScoreCategory)))
        {
            int acertos = acertosPorCategoria.ContainsKey(category) ? acertosPorCategoria[category] : 0;
            int erros = errosPorCategoria.ContainsKey(category) ? errosPorCategoria[category] : 0;
            summary += $"{category}: {acertos} acertos, {erros} erros\n";
        }

        return summary;
    }

    public void ResetScores()
    {
        entries.Clear();
    }
    
    public IReadOnlyList<ScoreEntry> GetAllEntries()
    {
        return entries;
    }

}
