using UnityEngine;

[CreateAssetMenu(menuName = "VRClinic/Question")]
public class QuestionSO : ScriptableObject
{
    public string prompt;                
    public string[] options = new string[2];
    public int correctOptionIndex;       
    [TextArea] public string wrongJustification;
}