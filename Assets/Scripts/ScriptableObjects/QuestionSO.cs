using UnityEngine;

[CreateAssetMenu(menuName = "VRClinic/Question")]
public class QuestionSO : ScriptableObject
{
    [Header("Pergunta do médico")]
    public string prompt;                    
    
    [Header("Opções de perguntas")]
    public string[] options = new string[2];

    [Header("Índice da opção “correta”")]
    public int correctOptionIndex;       

    [Header("Falas do paciente para cada pergunta")]
    public PatientResponses[] patientResponses = new PatientResponses[2];

    [Header("Justificativa textual para resposta “errada”")]
    [TextArea] public string wrongJustification;
}

[System.Serializable]
public class PatientResponses
{
    public AudioClip[] responseClips;
}