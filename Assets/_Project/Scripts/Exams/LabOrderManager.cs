using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LabOrderManager : MonoBehaviour
{
    private static readonly Color SelectedToggleColor = new Color32(0x22, 0x9C, 0xBE, 0xFF);
    private static readonly Color UnselectedToggleColor = Color.white;

    public PatientPhaseSO phaseData;
    public ScoreManager scoreManager;

    public GameObject togglePrefab;
    public Transform toggleGrid;

    public Transform resultButtonContainer;
    public GameObject resultButtonPrefab;
    public Transform resultContainer;
    public TextMeshProUGUI resultText;

    public Image tabletImage;
    public Button tabletNextButton;
    public Button tabletPrevButton;
    public NegatoscopeDisplay negatoscope;

    private readonly Dictionary<string, Toggle> toggleByExamID = new();
    private readonly HashSet<string> selectedLabs = new();
    private List<Sprite> currentResultPages = new();
    private int currentResultPageIndex;

    void Start()
    {
        if (scoreManager == null && PhaseManager.Instance != null)
            scoreManager = PhaseManager.Instance.scoreManager;
        if (phaseData == null && PhaseManager.Instance != null)
            phaseData = PhaseManager.Instance.phaseData;

        StartCoroutine(GenerateExamTogglesAtEndOfFrame());
    }

    public void OnLabToggle(string labID, bool isOn)
    {
        if (isOn) selectedLabs.Add(labID);
        else selectedLabs.Remove(labID);
    }

    public void ConfirmLabs()
    {
        foreach (string labID in selectedLabs)
        {
            bool isCorrect = phaseData.correctLabTestIDs.Contains(labID);
            RegisterLabEntry(
                labID,
                isCorrect,
                isCorrect ? ErrorSeverity.Leve : ErrorSeverity.Moderado,
                isCorrect ? "" : "Exame desnecessário para o quadro clínico.");
        }

        foreach (string requiredID in phaseData.correctLabTestIDs.Where(id => !selectedLabs.Contains(id)))
            RegisterLabEntry(requiredID, false, ErrorSeverity.Moderado, "Exame essencial não solicitado.");

        PhaseEvents.RaiseStepFinished();
    }

    public void ShowResultsUI()
    {
        foreach (Transform child in resultButtonContainer)
            Destroy(child.gameObject);

        foreach (string examID in selectedLabs.ToList())
        {
            PhaseExamResult result = FindResult(examID);
            if (result == null) continue;

            GameObject buttonObject = Instantiate(resultButtonPrefab, resultButtonContainer);
            buttonObject.GetComponentInChildren<TextMeshProUGUI>().text = result.examType.examName;
            buttonObject.GetComponent<Button>().onClick.AddListener(() => ShowSingleResult(result));
            buttonObject.SetActive(true);
        }
    }

    public void NextTabletPage()
    {
        if (currentResultPageIndex >= currentResultPages.Count - 1) return;

        currentResultPageIndex++;
        UpdateTabletImage();
    }

    public void PrevTabletPage()
    {
        if (currentResultPageIndex <= 0) return;

        currentResultPageIndex--;
        UpdateTabletImage();
    }

    public void ResetOrders()
    {
        selectedLabs.Clear();
        foreach (Toggle toggle in toggleByExamID.Values)
            toggle.isOn = false;

        resultContainer.gameObject.SetActive(false);
        resultText.gameObject.SetActive(false);
        negatoscope.Hide();
        HideTabletViewer();
        ClearTabletPages();
    }

    IEnumerator GenerateExamTogglesAtEndOfFrame()
    {
        yield return new WaitForEndOfFrame();
        GenerateExamToggles();
    }

    void GenerateExamToggles()
    {
        foreach (PhaseExamResult exam in phaseData.examResults)
        {
            string examID = exam.examType.examID;
            GameObject toggleObject = Instantiate(togglePrefab, toggleGrid);
            Toggle toggle = toggleObject.GetComponent<Toggle>();
            Image background = toggle.GetComponent<Image>();

            toggle.onValueChanged.AddListener(isOn =>
            {
                OnLabToggle(examID, isOn);
                SetToggleColor(background, isOn);
            });

            SetToggleColor(background, toggle.isOn);
            toggleObject.GetComponentInChildren<TextMeshProUGUI>().text = exam.examType.examName;
            toggleByExamID[examID] = toggle;
            toggleObject.SetActive(true);
        }
    }

    static void SetToggleColor(Image background, bool isOn)
    {
        if (background != null)
            background.color = isOn ? SelectedToggleColor : UnselectedToggleColor;
    }

    void RegisterLabEntry(string labID, bool isCorrect, ErrorSeverity severity, string justification)
    {
        scoreManager.RegisterScoreEntry(new ScoreEntry
        {
            category = ScoreCategory.LabExams,
            severity = severity,
            actionID = labID,
            isCorrect = isCorrect,
            justification = justification,
            points = scoreManager.GetPointsForLabExam(labID, isCorrect)
        });
    }

    PhaseExamResult FindResult(string examID)
    {
        return phaseData.examResults.FirstOrDefault(result => result.examType.examID == examID);
    }

    void ShowSingleResult(PhaseExamResult result)
    {
        resultContainer.gameObject.SetActive(true);
        ClearTabletPages();

        if (result.examType.isImaging)
            ShowImagingResult(result);
        else
            ShowDocumentResult(result);
    }

    void ShowImagingResult(PhaseExamResult result)
    {
        Sprite image = result.isAbnormal ? result.examType.abnormalImageResult : result.examType.normalImageResult;

        ShowMessage("Imagem aparecerá no negatoscópio.");
        HideTabletViewer();
        negatoscope.ShowImage(image);
    }

    void ShowDocumentResult(PhaseExamResult result)
    {
        Sprite[] pages = result.isAbnormal ? result.examType.defaultAbnormalResult : result.examType.defaultNormalResult;
        negatoscope.Hide();

        if (pages == null || pages.Length == 0)
        {
            ShowMessage("Resultado não disponível.");
            HideTabletViewer();
            return;
        }

        currentResultPages = pages.ToList();
        tabletImage.color = Color.white;
        resultText.gameObject.SetActive(false);
        ShowTabletViewer();
        UpdateTabletImage();
    }

    void ShowMessage(string message)
    {
        resultText.gameObject.SetActive(true);
        resultText.text = message;
    }

    void UpdateTabletImage()
    {
        if (currentResultPages.Count == 0) return;

        tabletImage.gameObject.SetActive(true);
        tabletImage.sprite = currentResultPages[currentResultPageIndex];
        tabletPrevButton.gameObject.SetActive(currentResultPageIndex > 0);
        tabletNextButton.gameObject.SetActive(currentResultPageIndex < currentResultPages.Count - 1);
    }

    void ClearTabletPages()
    {
        currentResultPages.Clear();
        currentResultPageIndex = 0;
    }

    void ShowTabletViewer()
    {
        SetTabletViewerActive(true);
    }

    void HideTabletViewer()
    {
        SetTabletViewerActive(false);
    }

    void SetTabletViewerActive(bool isActive)
    {
        tabletImage.gameObject.SetActive(isActive);
        tabletNextButton.gameObject.SetActive(isActive);
        tabletPrevButton.gameObject.SetActive(isActive);
    }
}
