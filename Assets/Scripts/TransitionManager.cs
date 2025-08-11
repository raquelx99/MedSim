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
            worldSpaceCanvas.gameObject.SetActive(false);
            // todo aquele sistema de animação do paciente e transição para a próxima fase
        }
    }

    void EndTransition()
    {
        worldSpaceCanvas.gameObject.SetActive(false);
        // todo iniciar a próxima fase
    }

}
