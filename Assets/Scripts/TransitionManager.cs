using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TransitionManager : MonoBehaviour
{
    [Header("UI Elements")]
    public List<DialogueStepSO> dialogueSteps;
    public Canvas worldSpaceCanvas;
    public Button optionButton;
    public TextMeshProUGUI optionText;

    [Header("Animation Elements")]
    public Animator patientAnimator;

    [Header("Audio Elements")]
    public AudioSource audioSource;
    public AudioManager audioManager;

    private int dialogueIndex = 0;

    void Start()
    {
        ShowDialogueStep();
    }

    void ShowDialogueStep()
    {
        optionButton.gameObject.SetActive(true);

        var dlg = dialogueSteps[dialogueIndex];

        if (dlg.npcLineClip != null)
            audioManager.Play(dlg.npcLineClip);

        optionText.text = dlg.playerPrompt;

        optionButton.onClick.RemoveAllListeners();
        optionButton.onClick.AddListener(OnDialogueOption);

        if (dlg.requiresTransition)
        {
            optionButton.interactable = false;
            StartCoroutine(DialogueBreakSequence());
        }
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
            EndTransition();
        }
    }

    IEnumerator DialogueBreakSequence()
    {
        worldSpaceCanvas.gameObject.SetActive(false);

        patientAnimator.Play("PacienteSai");
        yield return new WaitForSeconds(patientAnimator.GetCurrentAnimatorStateInfo(0).length);

        audioSource.Play();

        patientAnimator.Play("PacienteVolta");
        yield return new WaitForSeconds(patientAnimator.GetCurrentAnimatorStateInfo(0).length);

        worldSpaceCanvas.gameObject.SetActive(true);
        optionButton.interactable = true;
        ShowDialogueStep();
    }

    void EndTransition()
    {
        worldSpaceCanvas.gameObject.SetActive(false);
        PhaseManager.Instance.FinishStep();
    }

}
