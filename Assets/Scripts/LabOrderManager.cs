using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using Unity.VisualScripting;
using System.Collections;
using System.Linq;

public class LabOrderManager : MonoBehaviour
{
    public PatientPhaseSO phaseData;
    public ScoreManager scoreManager;

    private Dictionary<string, Toggle> toggleDict = new();
    private HashSet<string> selectedLabs = new();

    public Transform resultButtonContainer;
    public GameObject resultButtonPrefab;
    public Transform resultContainer;

    public Image tabletImage;
    public Button tabletNextButton;
    public Button tabletPrevButton;

    public TextMeshProUGUI resultText;
    public GameObject togglePrefab;
    public Transform toggleGrid;
    public NegatoscopeDisplay negatoscope;

    private List<Sprite> currentResultPages = new List<Sprite>();
    private int currentResultPageIndex = 0;

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

            Image bg = toggle.GetComponent<Image>();

            toggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
            {
                selectedLabs.Add(currentExam.examType.examID);
                if (bg != null)
                    bg.color = new Color32(0x22, 0x9C, 0xBE, 0xFF); // #229CBE
            }
            else
            {
                selectedLabs.Remove(currentExam.examType.examID);
                if (bg != null)
                    bg.color = Color.white;
            }
            });

            if (bg != null)
            bg.color = toggle.isOn ? new Color32(0x22, 0x9C, 0xBE, 0xFF) : Color.white;

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

        PhaseManager.Instance.FinishStep();

    }

    public void ShowResultsUI()
    {
        foreach (Transform child in resultButtonContainer)
            Destroy(child.gameObject);

        // usar uma cópia da coleção para manter ordem estável (opcional)
        var labsList = selectedLabs.ToList();

        foreach (string examID in labsList)
        {
            var resultData = GetResultDataForExam(examID);
            if (resultData == null) continue;

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
        tabletImage.gameObject.SetActive(false);

        // limpa viewer anterior
        ClearTabletPages();

        if (result.examType.isImaging)
        {
            // imagem única no negatoscópio (com a mensagem informando)
            var sprite = result.isAbnormal ?
                result.examType.abnormalImageResult :
                result.examType.normalImageResult;

            resultText.gameObject.SetActive(true);
            resultText.text = "Imagem aparecerá no negatoscópio.";

            Debug.Log($"Exibindo imagem para o exame: {result.examType.examName} no negatoscópio");

            negatoscope.ShowImage(sprite);

            // esconder viewer do tablet
            HideTabletViewer();
        }
        else
        {
            tabletImage.color = Color.white;
            tabletImage.gameObject.SetActive(true);

            // usa os arrays de sprites (padrão) para resultados não-imagem
            Sprite[] pages = result.isAbnormal ?
                result.examType.defaultAbnormalResult :
                result.examType.defaultNormalResult;

            if (pages != null && pages.Length > 0)
            {
                currentResultPages = pages.ToList();
                currentResultPageIndex = 0;
                UpdateTabletImage();
                ShowTabletViewer();
                resultText.gameObject.SetActive(false); // esconder texto quando temos imagens
                Debug.Log($"Exibindo {currentResultPages.Count} páginas de resultados para o exame: {result.examType.examName}");
            }
            else
            {
                // fallback caso não haja sprites: mostra texto informativo
                resultText.gameObject.SetActive(true);
                resultText.text = "Resultado não disponível.";
                HideTabletViewer();
            }

            // esconder negatoscópio caso estivesse visível
            negatoscope.Hide();
        }
    }

    // ---------------- tablet paging ----------------

    void HookTabletButtons()
    {
        if (tabletNextButton != null)
        {
            tabletNextButton.onClick.RemoveAllListeners();
            tabletNextButton.onClick.AddListener(NextTabletPage);
        }

        if (tabletPrevButton != null)
        {
            tabletPrevButton.onClick.RemoveAllListeners();
            tabletPrevButton.onClick.AddListener(PrevTabletPage);
        }
    }

    void UpdateTabletImage()
    {
        if (tabletImage == null || currentResultPages == null || currentResultPages.Count == 0)
            return;

        tabletImage.gameObject.SetActive(true);
        tabletImage.sprite = currentResultPages[currentResultPageIndex];

        // atualiza estado dos botões
        if (tabletPrevButton != null)
            tabletPrevButton.gameObject.SetActive(currentResultPageIndex > 0);
        if (tabletNextButton != null)
            tabletNextButton.gameObject.SetActive(currentResultPageIndex < currentResultPages.Count - 1);
    }

    public void NextTabletPage()
    {
        if (currentResultPages == null || currentResultPages.Count == 0) return;
        if (currentResultPageIndex < currentResultPages.Count - 1)
        {
            currentResultPageIndex++;
            UpdateTabletImage();
        }
    }

    public void PrevTabletPage()
    {
        if (currentResultPages == null || currentResultPages.Count == 0) return;
        if (currentResultPageIndex > 0)
        {
            currentResultPageIndex--;
            UpdateTabletImage();
        }
    }

    void ClearTabletPages()
    {
        currentResultPages.Clear();
        currentResultPageIndex = 0;
    }

    void ShowTabletViewer()
    {
        if (tabletImage != null) tabletImage.gameObject.SetActive(true);
        if (tabletNextButton != null) tabletNextButton.gameObject.SetActive(true);
        if (tabletPrevButton != null) tabletPrevButton.gameObject.SetActive(true);
    }

    void HideTabletViewer()
    {
        if (tabletImage != null) tabletImage.gameObject.SetActive(false);
        if (tabletNextButton != null) tabletNextButton.gameObject.SetActive(false);
        if (tabletPrevButton != null) tabletPrevButton.gameObject.SetActive(false);
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
        HideTabletViewer();
        ClearTabletPages();
        resultText.gameObject.SetActive(false);
    }
}
