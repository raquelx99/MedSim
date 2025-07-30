using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "VRClinic/PatientPhase")]
public class PatientPhaseSO : ScriptableObject
{
    public string phaseName;
    public DialogueStepSO[] dialogueSteps;
    public AnamnesisStepSO[] anamnesisSteps;
    public string[] requiredExamObjectIDs;      
    public string[] correctLabTestIDs;
    public PhaseExamResult[] examResults;             
    public string[] possibleDiagnosis;            
    public int correctDiagnosisIndex;            
}