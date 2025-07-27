using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "VRClinic/PatientPhase")]
public class PatientPhaseSO : ScriptableObject
{
    public string phaseName;
    public DialogueStepSO[] dialogueSteps;          // parte da anamnese
    public AnamnesisStepSO[] anamnesisSteps;        // op��es de perguntas
    public string[] requiredExamObjectIDs;         // IDs dos instrumentos f�sicos
    public string[] correctLabTestIDs;             // IDs dos exames corretos
    public string[] possibleDiagnosis;             // op��es de diagn�stico
    public int correctDiagnosisIndex;              // �ndice certo em possibleDiagnosis
}