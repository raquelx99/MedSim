using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using Unity.VisualScripting;
using System.Collections;

public class LabOrderManager : MonoBehaviour
{
    public PatientPhaseSO phaseData;
    public ScoreManager scoreManager;

    private Dictionary<string, Toggle> toggleDict = new();
    private HashSet<string> selectedLabs = new();

    public Transform resultButtonContainer;
    public GameObject resultButtonPrefab;
    public Transform resultContainer;
    public TextMeshProUGUI resultText;
    public GameObject togglePrefab;
    public Transform toggleGrid;
    public NegatoscopeDisplay negatoscope;

    void Start()
    {
        StartCoroutine(DelayedToggleGeneration());
    }

    IEnumerator DelayedToggleGeneration()
    {
        yield return new WaitForEndOfFrame();
        GenerateExamToggles();
    }

    void GenerateExamToggles()
    {
        foreach (var exam in phaseData.examResults)
        {
            GameObject go = Instantiate(togglePrefab, toggleGrid);
            Toggle toggle = go.GetComponent<Toggle>();
            TextMeshProUGUI label = go.GetComponentInChildren<TextMeshProUGUI>();
            var currentExam = exam;

            toggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn) selectedLabs.Add(currentExam.examType.examID);
                else selectedLabs.Remove(currentExam.examType.examID);
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
        {
            bool acertou = System.Array.Exists(phaseData.correctLabTestIDs, id => id == lab);
            int pointsToApply = scoreManager.GetPointsForLabExam(lab, acertou);

            Debug.Log($"AVALIANDO EXAME SELECIONADO: ID='{lab}', Acertou?={acertou}, Pontos Aplicados={pointsToApply}");

            scoreManager.RegisterScoreEntry(new ScoreEntry
            {
                category = ScoreCategory.LabExams,
                severity = acertou ? ErrorSeverity.Leve : ErrorSeverity.Moderado,
                actionID = lab,
                isCorrect = acertou,
                justification = acertou ? "" : "Exame desnecessário para o quadro clínico.",
                points = pointsToApply
            });
        }

        foreach (var req in phaseData.correctLabTestIDs)
        {
            if (!selectedLabs.Contains(req))
            {
                int pointsToApply = scoreManager.GetPointsForLabExam(req, false);

                Debug.Log($"AVALIANDO EXAME ESQUECIDO: ID='{req}', Pontos Aplicados={pointsToApply}");

                scoreManager.RegisterScoreEntry(new ScoreEntry
                {
                    category = ScoreCategory.LabExams,
                    severity = ErrorSeverity.Moderado,
                    actionID = req,
                    isCorrect = false,
                    justification = "Exame essencial não solicitado.",
                    points = pointsToApply
                });
            }
        }

        ShowResultsUI();

        PhaseManager.Instance.FinishStep();

    }

    void ShowResultsUI()
    {
        foreach (Transform child in resultButtonContainer)
            Destroy(child.gameObject);

        foreach (string examID in selectedLabs)
        {
            var resultData = GetResultDataForExam(examID);

            GameObject btn = Instantiate(resultButtonPrefab, resultButtonContainer);
            Button button = btn.GetComponent<Button>();
            TextMeshProUGUI label = btn.GetComponentInChildren<TextMeshProUGUI>();

            label.text = resultData.examType.examName;

            button.onClick.AddListener(() =>
            {
                ShowSingleResult(resultData);
            });

            button.gameObject.SetActive(true);

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
        resultContainer.gameObject.SetActive(true);

        if (result.examType.isImaging)
        {
            var sprite = result.isAbnormal ?
            result.examType.abnormalImageResult :
            result.examType.normalImageResult;
            resultText.text = "Imagem aparecerá no negatoscópio.";

            Debug.Log($"Exibindo imagem para o exame: {result.examType.examName}");

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
    
    public void ResetOrders()
    {
        selectedLabs.Clear();
        foreach (var toggle in toggleDict.Values)
        {
            toggle.isOn = false;
        }
        resultContainer.gameObject.SetActive(false);
        negatoscope.Hide();
    }

}