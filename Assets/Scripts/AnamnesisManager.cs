using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;        
using TMPro;

public class AnamnesisManager : MonoBehaviour
{
    [Header("Configuração")]
    public List<DialogueStepSO> dialogueSteps;  
    public List<AnamnesisStepSO> questionSteps; 
    public TextMeshProUGUI npcText;
    public Button[] optionButtons;                 
    public TextMeshProUGUI[] optionTexts;

    private int dialogueIndex = 0;
    private int questionIndex = 0;
    private bool inDialoguePhase = true;
    private int score = 0;
    private List<(string q, string justification)> wrongLog = new();

    void Start()
    {
        ShowDialogueStep();
    }

    void ShowDialogueStep()
    {
        optionButtons[0].gameObject.SetActive(true);
        optionButtons[1].gameObject.SetActive(false);

        var dlg = dialogueSteps[dialogueIndex];
        npcText.text = dlg.npcLine;
        optionTexts[0].text = dlg.playerPrompt;

        optionButtons[0].onClick.RemoveAllListeners();
        optionButtons[0].onClick.AddListener(OnDialogueOption);
    }

    void OnDialogueOption()
    {

        var dlg = dialogueSteps[dialogueIndex];
        if (!string.IsNullOrEmpty(dlg.npcResponse))
        {
            npcText.text = dlg.npcResponse;
        }

        dialogueIndex++;

        if (dialogueIndex < dialogueSteps.Count)
        {
            Invoke(nameof(ShowDialogueStep), 0.5f);
        }
        else
        {
            inDialoguePhase = false;
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
        if (chosen == q.correctOptionIndex)
            score++;
        else
            wrongLog.Add((q.prompt, q.wrongJustification));

        questionIndex++;
        if (questionIndex < questionSteps.Count)
            ShowQuestionStep();
        else
            FinishAnamnesis();
    }

    void FinishAnamnesis()
    {
        Debug.Log($"Score: {score}/{questionSteps.Count}");
        foreach (var e in wrongLog)
            Debug.Log($"Pergunta errada: {e.q} — {e.justification}");
        
    }
}
