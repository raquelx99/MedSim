using UnityEngine;

[CreateAssetMenu(menuName = "VRClinic/DialogueStep")]
public class DialogueStepSO : ScriptableObject
{
    public string npcLine;
    public string playerPrompt;
    public string npcResponse;
}