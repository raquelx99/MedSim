using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class TransitionManager : MonoBehaviour
{
    [Header("UI Elements")]
    public List<DialogueStepSO> dialogueSteps;
    public Canvas worldSpaceCanvas;
    public Button[] optionButtons;
    public TextMeshProUGUI optionText;

    [Header("Controladores")]
    public PatientAnimationController patientAnimationController;

    [Header("Audio Elements")]
    public AudioSource audioSource;
    public AudioManager audioManager;

    private int dialogueIndex = 0;

    void Start()
    {
        worldSpaceCanvas.gameObject.SetActive(true);
        optionButtons[1].gameObject.SetActive(false);

        Debug.Log("Entrou na transição");

        if (patientAnimationController == null)
        {
            Debug.LogError("A referência para PatientAnimationController não foi atribuída no TransitionManager!", this.gameObject);
            this.enabled = false;
            return;
        }
        ShowDialogueStep();
    }

    void ShowDialogueStep()
    {
        Debug.Log("Começou o dialogo");
        if (dialogueSteps == null || dialogueSteps.Count == 0 || dialogueIndex >= dialogueSteps.Count)
        {
            Debug.Log("parou a transição");
            EndTransition();
            return;
        }

        optionButtons[0].gameObject.SetActive(true);
        var dlg = dialogueSteps[dialogueIndex];

        if (dlg.npcLineClip != null)
            audioManager.Play(dlg.npcLineClip);

        optionText.text = dlg.playerPrompt;

        optionButtons[0].onClick.RemoveAllListeners();
        optionButtons[0].onClick.AddListener(OnDialogueOption);

        optionButtons[0].interactable = true;
    }

    void OnDialogueOption()
    {
        var dlg = dialogueSteps[dialogueIndex];

        if (dlg.npcResponseClip != null)
            audioManager.Play(dlg.npcResponseClip);

        if (dlg.requiresTransition)
        {
            worldSpaceCanvas.gameObject.SetActive(false);
            patientAnimationController.StartExitAndReturnSequence();
            dialogueIndex++;
            return;
        }

        dialogueIndex++;

        if (dialogueIndex < dialogueSteps.Count)
        {
            Invoke(nameof(ShowDialogueStep), 0.5f);
        }
        else
        {
            EndTransition();
        }
    }
    
    public void AnimationSequenceFinished()
    {
        audioSource.Play();

        worldSpaceCanvas.gameObject.SetActive(true);
        optionButtons[0].interactable = true;
        ShowDialogueStep();
    }

    void EndTransition()
    {
        if(worldSpaceCanvas != null) worldSpaceCanvas.gameObject.SetActive(false);
        if(PhaseManager.Instance != null) PhaseManager.Instance.FinishStep();
    }
}