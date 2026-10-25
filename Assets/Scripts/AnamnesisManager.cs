using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AnamnesisManager : MonoBehaviour
{
    private const float NextDialogueDelay = 0.5f;
    private const int QuestionOptionCount = 2;

    public Canvas worldSpaceCanvas;
    public TextMeshProUGUI npcText;
    public Button[] optionButtons;
    public TextMeshProUGUI[] optionTexts;
    public ScoreManager scoreManager;
    public AudioManager audioManager;

    private List<DialogueStepSO> dialogueSteps;
    private List<AnamnesisStepSO> questionSteps;
    private int dialogueIndex;
    private int questionIndex;

    void Start()
    {
        if (scoreManager == null && PhaseManager.Instance != null)
            scoreManager = PhaseManager.Instance.scoreManager;
    }

    public void Init(List<DialogueStepSO> dialogues, List<AnamnesisStepSO> questions)
    {
        dialogueSteps = dialogues;
        questionSteps = questions;
        dialogueIndex = 0;
        questionIndex = 0;

        worldSpaceCanvas.gameObject.SetActive(true);
        ShowDialogueStep();
    }

    void ShowDialogueStep()
    {
        optionButtons[0].gameObject.SetActive(true);
        optionButtons[1].gameObject.SetActive(false);

        DialogueStepSO dialogue = dialogueSteps[dialogueIndex];
        PlayClip(dialogue.npcLineClip);
        optionTexts[0].text = dialogue.playerPrompt;

        optionButtons[0].onClick.RemoveAllListeners();
        optionButtons[0].onClick.AddListener(OnDialogueOption);
    }

    void OnDialogueOption()
    {
        PlayClip(dialogueSteps[dialogueIndex].npcResponseClip);
        dialogueIndex++;

        if (dialogueIndex < dialogueSteps.Count)
        {
            Invoke(nameof(ShowDialogueStep), NextDialogueDelay);
            return;
        }

        questionIndex = 0;
        ShowQuestionStep();
    }

    void ShowQuestionStep()
    {
        foreach (Button button in optionButtons)
            button.gameObject.SetActive(true);

        QuestionSO question = questionSteps[questionIndex].question;
        npcText.text = question.prompt;

        for (int i = 0; i < QuestionOptionCount; i++)
        {
            int optionIndex = i;
            optionTexts[i].text = question.options[i];
            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() => OnQuestionSelected(optionIndex));
        }
    }

    void OnQuestionSelected(int chosenIndex)
    {
        QuestionSO question = questionSteps[questionIndex].question;
        PlayRandomClip(question.patientResponses[chosenIndex].responseClips);

        bool isCorrect = chosenIndex == question.correctOptionIndex;
        scoreManager.RegisterScoreEntry(new ScoreEntry
        {
            category = ScoreCategory.Anamnese,
            severity = isCorrect ? ErrorSeverity.Leve : ErrorSeverity.Moderado,
            actionID = question.prompt,
            isCorrect = isCorrect,
            justification = isCorrect ? "" : question.wrongJustification,
            points = scoreManager.GetPointsForAnamnesis(question.prompt, isCorrect)
        });

        questionIndex++;
        if (questionIndex < questionSteps.Count)
            ShowQuestionStep();
        else
            FinishAnamnesis();
    }

    void FinishAnamnesis()
    {
        worldSpaceCanvas.gameObject.SetActive(false);
        PhaseEvents.RaiseStepFinished();
    }

    void PlayClip(AudioClip clip)
    {
        if (clip != null)
            audioManager.Play(clip);
    }

    void PlayRandomClip(AudioClip[] clips)
    {
        if (clips != null && clips.Length > 0)
            audioManager.Play(clips[Random.Range(0, clips.Length)]);
    }
}
