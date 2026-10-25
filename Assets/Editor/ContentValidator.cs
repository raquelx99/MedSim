using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ContentValidator
{
    [MenuItem("VRClinic/Validar Conteúdo das Fases")]
    public static void ValidateAll()
    {
        var scoreManager = UnityEngine.Object.FindObjectOfType<ScoreManager>();
        if (scoreManager == null)
        {
            Debug.LogError("Nenhum ScoreManager encontrado na cena aberta. Abra a cena Consultorio antes de validar.");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:PatientPhaseSO");
        var phases = guids
            .Select(g => AssetDatabase.LoadAssetAtPath<PatientPhaseSO>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(p => p != null)
            .ToList();

        if (phases.Count == 0)
        {
            Debug.LogWarning("Nenhum PatientPhaseSO encontrado no projeto.");
            return;
        }

        int warnings = 0;

        // Exame laboratorial pedido como correto mas sem resultado configurado para exibição.
        foreach (var phase in phases)
        {
            foreach (var labID in phase.correctLabTestIDs ?? Array.Empty<string>())
            {
                bool hasResult = (phase.examResults ?? Array.Empty<PhaseExamResult>())
                    .Any(r => r.examType != null && r.examType.examID == labID);
                if (!hasResult)
                {
                    Debug.LogWarning($"[{phase.name}] examID '{labID}' está em correctLabTestIDs mas não tem " +
                        $"PhaseExamResult correspondente em examResults — o resultado não vai aparecer na tela de diagnóstico.");
                    warnings++;
                }
            }
        }

        // ScoreMapping cujo id não corresponde a nenhum dado existente em nenhuma fase conhecida
        // (provável texto desatualizado/erro de digitação — cai silenciosamente no valor default).
        var allPrompts = phases
            .SelectMany(p => p.anamnesisSteps ?? Array.Empty<AnamnesisStepSO>())
            .Where(s => s != null && s.question != null)
            .Select(s => s.question.prompt)
            .ToHashSet();
        CheckMappings(scoreManager.anamnesisScores, allPrompts, "Anamnese", ref warnings);

        var allExamObjectIDs = phases
            .SelectMany(p => p.requiredExamObjectIDs ?? Array.Empty<string>())
            .ToHashSet();
        CheckMappings(scoreManager.physicalExamScores, allExamObjectIDs, "Exame Físico", ref warnings);

        var allLabIDs = phases
            .SelectMany(p => p.correctLabTestIDs ?? Array.Empty<string>())
            .ToHashSet();
        CheckMappings(scoreManager.labExamScores, allLabIDs, "Exame Laboratorial", ref warnings);

        var allDiagnoses = phases
            .SelectMany(p => p.possibleDiagnosis ?? Array.Empty<string>())
            .ToHashSet();
        CheckMappings(scoreManager.diagnosisScores, allDiagnoses, "Diagnóstico", ref warnings);

        if (warnings == 0)
            Debug.Log($"Validação concluída: nenhum problema encontrado em {phases.Count} fase(s).");
        else
            Debug.Log($"Validação concluída: {warnings} problema(s) encontrado(s) em {phases.Count} fase(s). Veja os avisos acima.");
    }

    private static void CheckMappings(List<ScoreMapping> mappings, HashSet<string> validIDs, string categoria, ref int warnings)
    {
        if (mappings == null) return;

        foreach (var mapping in mappings)
        {
            if (string.IsNullOrEmpty(mapping.id)) continue;

            if (!validIDs.Contains(mapping.id))
            {
                Debug.LogWarning($"ScoreMapping de {categoria} com id '{mapping.id}' não corresponde a nenhum dado " +
                    $"existente em nenhuma fase — provável texto desatualizado ou erro de digitação.");
                warnings++;
            }
        }
    }
}
