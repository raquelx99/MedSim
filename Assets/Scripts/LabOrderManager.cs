using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using Unity.VisualScripting;

public class LabOrderManager : MonoBehaviour
{
    public PatientPhaseSO phaseData;
    public ScoreManager scoreManager;
    
    private Dictionary<string, Toggle> toggleDict = new();
    private HashSet<string> selectedLabs = new();

    public Transform resultButtonContainer;
    public GameObject resultButtonPrefab;
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;
    public GameObject togglePrefab;
    public Transform toggleGrid;
    public NegatoscopeDisplay negatoscope;

    void Start()
    {
        GenerateExamToggles();
        Debug.Log("Gerou toggles de exames.");
    }

    void GenerateExamToggles()
    {
        foreach (var exam in phaseData.examResults)
        {
            GameObject go = Instantiate(togglePrefab, toggleGrid);
            Toggle toggle = go.GetComponent<Toggle>();
            TextMeshProUGUI label = go.GetComponentInChildren<TextMeshProUGUI>();

            toggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn) selectedLabs.Add(exam.examType.examID);
                else selectedLabs.Remove(exam.examType.examID);
            });

            label.text = exam.examType.examName;
            toggleDict[exam.examType.examID] = toggle;
            
            go.SetActive(true);
        }
    }

    public void OnLabToggle(string labID, bool isOn)
    {
        if (isOn) selectedLabs.Add(labID);
        else selectedLabs.Remove(labID);
    }

    public void ConfirmLabs()
    {

        foreach (var lab in selectedLabs)
            scoreManager.Add(
                System.Array.Exists(phaseData.correctLabTestIDs, id => id == lab)
            );

        foreach (var req in phaseData.correctLabTestIDs)
            if (!selectedLabs.Contains(req))
                scoreManager.Add(false);

        ShowResultsUI();

        FindObjectOfType<PhaseManager>().NextPart();
    }
    
    void ShowResultsUI()
    {
        foreach (Transform child in resultButtonContainer)
            Destroy(child.gameObject);

        foreach (string examID in selectedLabs)
        {
            var resultData = GetResultDataForExam(examID);

            GameObject btn = Instantiate(resultButtonPrefab, resultButtonContainer);
            btn.GetComponentInChildren<TextMeshProUGUI>().text = resultData.examType.examName;

            btn.GetComponent<Button>().onClick.AddListener(() =>
            {
                ShowSingleResult(resultData);
            });
        }
    }

    PhaseExamResult GetResultDataForExam(string examID)
    {
        foreach (var r in phaseData.examResults)
            if (r.examType.examID == examID)
                return r;

        return null;
    }

    void ShowSingleResult(PhaseExamResult result)
    {
        resultPanel.SetActive(true);

        if (result.examType.isImaging)
        {
            var sprite = result.isAbnormal ?
            result.examType.abnormalImageResult :
            result.examType.normalImageResult;
            resultText.text = "Imagem aparecerá no negatoscópio.";

            negatoscope.ShowImage(sprite);
        }
        else
        {
            resultText.gameObject.SetActive(true);

            resultText.text = result.isAbnormal ?
                result.examType.defaultAbnormalResultText :
                result.examType.defaultNormalResultText;
        }
    }

}