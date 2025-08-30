using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;        
using TMPro;

public class AnamnesisManager : MonoBehaviour
{
    [Header("Configuração")]
    private List<DialogueStepSO> dialogueSteps;
    private List<AnamnesisStepSO> questionSteps;

    public Canvas worldSpaceCanvas;
    public TextMeshProUGUI npcText;
    public Button[] optionButtons;                 
    public TextMeshProUGUI[] optionTexts;
    public ScoreManager scoreManager;
    
    [Header("Áudio")]
    public AudioManager audioManager;

    private int dialogueIndex = 0;
    private int questionIndex = 0;
    private List<(string q, string justification)> wrongLog = new();
    

    public void Init(List<DialogueStepSO> dialogues, List<AnamnesisStepSO> questions)
    {
        dialogueSteps = dialogues;
        questionSteps = questions;
        dialogueIndex = 0;
        questionIndex = 0;
        wrongLog.Clear();

        worldSpaceCanvas.gameObject.SetActive(true);
        ShowDialogueStep();
    }

    void Start()
    {
        ShowDialogueStep();
    }

    void ShowDialogueStep()
    {
        optionButtons[0].gameObject.SetActive(true);
        optionButtons[1].gameObject.SetActive(false);

        var dlg = dialogueSteps[dialogueIndex];

        if (dlg.npcLineClip != null)
            audioManager.Play(dlg.npcLineClip);

        optionTexts[0].text = dlg.playerPrompt;

        optionButtons[0].onClick.RemoveAllListeners();
        optionButtons[0].onClick.AddListener(OnDialogueOption);
    }

   void OnDialogueOption()
    {
        var dlg = dialogueSteps[dialogueIndex];

        if (dlg.npcResponseClip != null)
            audioManager.Play(dlg.npcResponseClip);

        dialogueIndex++;

        if (dialogueIndex < dialogueSteps.Count)
            Invoke(nameof(ShowDialogueStep), 0.5f);
        else
        {
            questionIndex = 0;
            ShowQuestionStep();
        }
    }

    void ShowQuestionStep()
    {

        optionButtons[0].gameObject.SetActive(true);
        optionButtons[1].gameObject.SetActive(true);

        var q = questionSteps[questionIndex].question;
        npcText.text = q.prompt;

        for (int i = 0; i < 2; i++)
        {
            optionTexts[i].text = q.options[i];
            optionButtons[i].onClick.RemoveAllListeners();
            int idx = i;
            optionButtons[i].onClick.AddListener(() => OnQuestionSelected(idx));
        }
    }

    void OnQuestionSelected(int chosen)
    {
        var q = questionSteps[questionIndex].question;

        var clips = q.patientResponses[chosen].responseClips;
        if (clips != null && clips.Length > 0)
        {
            AudioClip clip = clips[Random.Range(0, clips.Length)];
            audioManager.Play(clip);
        }

        bool acertou = (chosen == q.correctOptionIndex);
        int pointsToApply = scoreManager.GetPointsForAnamnesis(q.prompt, acertou);
        Debug.Log($"AVALIANDO PERGUNTA SELECIONADA: ID='{q.prompt}', Acertou?={acertou}, Pontos Aplicados={pointsToApply}");

        scoreManager.RegisterScoreEntry(new ScoreEntry {
            category      = ScoreCategory.Anamnese,
            severity      = acertou ? ErrorSeverity.Leve : ErrorSeverity.Moderado,
            actionID      = q.prompt,
            isCorrect     = acertou,
            justification = acertou ? "" : q.wrongJustification,
            points        = pointsToApply
        });

        questionIndex++;
        if (questionIndex < questionSteps.Count)
            ShowQuestionStep();
        else
            FinishAnamnesis();
    }

    void FinishAnamnesis()
    {
        foreach (var e in wrongLog)
            Debug.Log($"Pergunta errada: {e.q} — {e.justification}");

        worldSpaceCanvas.gameObject.SetActive(false);
        PhaseManager.Instance.FinishStep();
    }
}
