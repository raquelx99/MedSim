using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class ScoreManagerTests
{
    private readonly List<GameObject> spawned = new List<GameObject>();

    private ScoreManager CreateScoreManager()
    {
        var go = new GameObject("ScoreManager");
        spawned.Add(go);
        var scoreManager = go.AddComponent<ScoreManager>();
        scoreManager.anamnesisScores = new List<ScoreMapping>();
        scoreManager.physicalExamScores = new List<ScoreMapping>();
        scoreManager.labExamScores = new List<ScoreMapping>();
        scoreManager.diagnosisScores = new List<ScoreMapping>();
        return scoreManager;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var go in spawned)
            Object.DestroyImmediate(go);
        spawned.Clear();
    }

    [Test]
    public void GetPointsForAnamnesis_UsesConfiguredMapping_WhenPresent()
    {
        var scoreManager = CreateScoreManager();
        scoreManager.anamnesisScores.Add(new ScoreMapping
        {
            id = "Qual a idade?",
            correctPoints = 10,
            incorrectPenalty = 3
        });

        Assert.AreEqual(10, scoreManager.GetPointsForAnamnesis("Qual a idade?", true));
        Assert.AreEqual(-3, scoreManager.GetPointsForAnamnesis("Qual a idade?", false));
    }

    [Test]
    public void GetPointsForAnamnesis_FallsBackToDefault_WhenNoMapping()
    {
        var scoreManager = CreateScoreManager();

        Assert.AreEqual(2, scoreManager.GetPointsForAnamnesis("Pergunta sem mapeamento", true));
        Assert.AreEqual(0, scoreManager.GetPointsForAnamnesis("Pergunta sem mapeamento", false));
    }

    [Test]
    public void GetPointsForPhysicalExam_FallsBackToDefault_WhenNoMapping()
    {
        var scoreManager = CreateScoreManager();

        Assert.AreEqual(20, scoreManager.GetPointsForPhysicalExam("estetoscopio", true));
        Assert.AreEqual(-5, scoreManager.GetPointsForPhysicalExam("estetoscopio", false));
    }

    [Test]
    public void GetPointsForDiagnosis_FallsBackToDefault_WhenNoMapping()
    {
        var scoreManager = CreateScoreManager();

        Assert.AreEqual(40, scoreManager.GetPointsForDiagnosis("Hipertensão", true));
        Assert.AreEqual(-20, scoreManager.GetPointsForDiagnosis("Hipertensão", false));
    }

    [Test]
    public void RegisterScoreEntry_AccumulatesIntoTotalPoints()
    {
        var scoreManager = CreateScoreManager();

        scoreManager.RegisterScoreEntry(new ScoreEntry { category = ScoreCategory.Anamnese, isCorrect = true, points = 10 });
        scoreManager.RegisterScoreEntry(new ScoreEntry { category = ScoreCategory.PhysicalExam, isCorrect = false, points = -5 });

        Assert.AreEqual(5, scoreManager.GetCurrentTotalPoints());
    }

    [Test]
    public void GetAllEntries_ReturnsEveryRegisteredEntry_InOrder()
    {
        var scoreManager = CreateScoreManager();
        var first = new ScoreEntry { actionID = "a", points = 1 };
        var second = new ScoreEntry { actionID = "b", points = 2 };

        scoreManager.RegisterScoreEntry(first);
        scoreManager.RegisterScoreEntry(second);

        var entries = scoreManager.GetAllEntries();

        Assert.AreEqual(2, entries.Count);
        Assert.AreEqual("a", entries[0].actionID);
        Assert.AreEqual("b", entries[1].actionID);
    }

    [Test]
    public void ResetScores_ClearsAllEntries()
    {
        var scoreManager = CreateScoreManager();
        scoreManager.RegisterScoreEntry(new ScoreEntry { points = 10 });

        scoreManager.ResetScores();

        Assert.AreEqual(0, scoreManager.GetAllEntries().Count);
        Assert.AreEqual(0, scoreManager.GetCurrentTotalPoints());
    }
}
