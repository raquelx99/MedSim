using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public static class DisplayNames
{
    private static readonly Regex LeadingNumber = new(@"^\s*\d+\s*\.?\s*");

    private static readonly Dictionary<string, string> Instruments = new()
    {
        { "esfigmomanometro", "Esfigmomanômetro" },
        { "estetoscopio", "Estetoscópio" },
        { "oftalmoscopio", "Oftalmoscópio" },
        { "otoscopio", "Otoscópio" },
        { "oximetro", "Oxímetro" },
    };

    public static string Category(ScoreCategory category)
    {
        return category switch
        {
            ScoreCategory.Anamnese => "Anamnese",
            ScoreCategory.PhysicalExam => "Exame físico",
            ScoreCategory.LabExams => "Exames complementares",
            ScoreCategory.Diagnosis => "Diagnóstico",
            _ => category.ToString()
        };
    }

    public static string Instrument(string id)
    {
        if (string.IsNullOrEmpty(id))
            return id;

        return Instruments.TryGetValue(id.ToLowerInvariant(), out string name) ? name : Capitalize(id);
    }

    public static string AssetNameWithoutNumber(Object asset)
    {
        return asset == null ? "" : LeadingNumber.Replace(asset.name, "").Trim();
    }

    public static string Capitalize(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return char.ToUpperInvariant(text[0]) + text.Substring(1);
    }
}
