using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TransitionManager : MonoBehaviour
{
    private const float NextDialogueDelay = 0.5f;
    private const int DialogueButtonIndex = 1;

    public List<DialogueStepSO> dialogueSteps;
    public Canvas worldSpaceCanvas;
    public Button[] optionButtons;
    public TextMeshProUGUI optionText;
    public PatientAnimationController patientAnimationController;
    public AudioSource audioSource;
    public AudioManager audioManager;

    private int dialogueIndex;

    private Button DialogueButton => optionButtons[DialogueButtonIndex];

    void Start()
    {
        if (patientAnimationController == null)
        {
            Debug.LogError("A referência para PatientAnimationController não foi atribuída no TransitionManager!", gameObject);
            enabled = false;
            return;
        }

        worldSpaceCanvas.gameObject.SetActive(true);
        optionButtons[0].gameObject.SetActive(false);
        ShowDialogueStep();
    }

    public void AnimationSequenceFinished()
    {
        worldSpaceCanvas.gameObject.SetActive(true);
        ShowDialogueStep();
    }

    void ShowDialogueStep()
    {
        if (dialogueSteps == null || dialogueIndex >= dialogueSteps.Count)
        {
            EndTransition();
            return;
        }

        DialogueStepSO dialogue = dialogueSteps[dialogueIndex];
        if (dialogue.npcLineClip != null)
            audioManager.Play(dialogue.npcLineClip);

        optionText.text = dialogue.playerPrompt;

        DialogueButton.gameObject.SetActive(true);
        DialogueButton.interactable = true;
        DialogueButton.onClick.RemoveAllListeners();
        DialogueButton.onClick.AddListener(OnDialogueOption);
    }

    void OnDialogueOption()
    {
        DialogueStepSO dialogue = dialogueSteps[dialogueIndex];
        if (dialogue.npcResponseClip != null)
            audioManager.Play(dialogue.npcResponseClip);

        dialogueIndex++;

        if (dialogue.requiresTransition)
        {
            worldSpaceCanvas.gameObject.SetActive(false);
            patientAnimationController.StartExitAndReturnSequence();
        }
        else if (dialogueIndex < dialogueSteps.Count)
        {
            Invoke(nameof(ShowDialogueStep), NextDialogueDelay);
        }
        else
        {
            EndTransition();
        }
    }

    void EndTransition()
    {
        audioSource.Play();
        worldSpaceCanvas.gameObject.SetActive(false);
        PhaseEvents.RaiseStepFinished();
    }
}
