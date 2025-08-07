using UnityEngine;

[CreateAssetMenu(menuName = "VRClinic/DialogueStep")]
public class DialogueStepSO : ScriptableObject
{
    [Header("Áudio do NPC falando primeiro")]
    public AudioClip npcLineClip;

    [Header("Texto que o jogador vê/opta")]
    public string playerPrompt;

    [Header("Áudios do NPC em resposta ao jogador")]
    public AudioClip npcResponseClip;
}