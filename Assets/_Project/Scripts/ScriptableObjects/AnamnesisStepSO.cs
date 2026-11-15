using UnityEngine;

[CreateAssetMenu(menuName = "VRClinic/AnamnesisStep")]
public class AnamnesisStepSO : ScriptableObject
{
    public string stepName;              
    public QuestionSO question;           
}