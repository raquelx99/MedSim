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
    public Button optionButton;
    public TextMeshProUGUI optionText;

    [Header("Controladores")]
    public PatientAnimationController patientAnimationController;

    [Header("Audio Elements")]
    public AudioSource audioSource;
    public AudioManager audioManager;

    private int dialogueIndex = 0;

    public void Init()
    {
        worldSpaceCanvas.gameObject.SetActive(true);
    }

    void Start()
    {
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
        {
            Invoke(nameof(ShowDialogueStep), 0.5f);
        }
        else
        {
            if(worldSpaceCanvas != null) worldSpaceCanvas.gameObject.SetActive(false);
            patientAnimationController.StartExitAndReturnSequence();
        }
    }
 
    IEnumerator DialogueBreakSequence()
    {
        worldSpaceCanvas.gameObject.SetActive(false);
        patientAnimationController.patientAnimator.Play("PacienteSai");
        yield return new WaitForSeconds(patientAnimationController.patientAnimator.GetCurrentAnimatorStateInfo(0).length);

        audioSource.Play();

        patientAnimationController.patientAnimator.Play("PacienteVolta");
        yield return new WaitForSeconds(patientAnimationController.patientAnimator.GetCurrentAnimatorStateInfo(0).length);

        worldSpaceCanvas.gameObject.SetActive(true);
        optionButton.interactable = true;
        ShowDialogueStep();
    }
    
    public void AnimationSequenceFinished()
    {
        Debug.Log("Sequência de animação finalizada. Chamando EndTransition.");
        EndTransition();
    }

    void EndTransition()
    {
        if(worldSpaceCanvas != null) worldSpaceCanvas.gameObject.SetActive(false);
        if(PhaseManager.Instance != null) PhaseManager.Instance.FinishStep();
    }
}