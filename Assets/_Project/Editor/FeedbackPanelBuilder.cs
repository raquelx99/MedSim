using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class FeedbackPanelBuilder
{
    private const float FallbackPanelWidth = 27.8f;

    private static readonly Color TextColor = new Color32(0x1F, 0x29, 0x37, 0xFF);
    private static readonly Color AccentColor = new Color32(0x15, 0x36, 0x67, 0xFF);
    private static readonly Color MutedColor = new Color32(0x4A, 0x55, 0x68, 0xFF);

    private readonly struct Layout
    {
        public readonly float Width;
        public readonly float Padding;
        public readonly float Gap;
        public readonly float TitleHeight;
        public readonly float FooterHeight;
        public readonly float RetryHeight;

        public Layout(float width)
        {
            Width = width;
            Padding = width * 0.07f;
            Gap = width * 0.03f;
            TitleHeight = width * 0.13f;
            FooterHeight = width * 0.1f;
            RetryHeight = width * 0.11f;
        }

        public float FontSize(float widthFraction) => Width * widthFraction;
    }

    private class PanelAssets
    {
        public TMP_FontAsset Font;
        public Sprite LeftArrow;
        public Sprite RightArrow;
        public Sprite RetryBackground;
    }

    [MenuItem("MedSim/UI/Recriar painel de feedback")]
    private static void Rebuild()
    {
        FeedbackManager manager = Object.FindObjectOfType<FeedbackManager>(true);
        if (manager == null || manager.feedbackPanel == null)
        {
            EditorUtility.DisplayDialog("Painel de feedback", "Nenhum FeedbackManager com Feedback Panel configurado na cena aberta.", "OK");
            return;
        }

        RectTransform panel = manager.feedbackPanel.GetComponent<RectTransform>();
        if (!ConfirmRebuild(panel))
            return;

        Undo.SetCurrentGroupName("Recriar painel de feedback");
        int undoGroup = Undo.GetCurrentGroup();

        PanelAssets assets = CollectExistingAssets(manager, panel);
        DestroyChildren(panel);

        var layout = new Layout(panel.rect.width > 0.01f ? panel.rect.width : FallbackPanelWidth);

        Undo.RecordObject(manager, "Ligar painel de feedback");
        manager.titleText = CreateTitle(panel, layout, assets);
        CreateDivider(panel, layout);
        manager.contentText = CreateContent(panel, layout, assets);
        manager.retryButton = CreateRetryButton(panel, layout, assets);
        manager.prevButton = CreateArrowButton("PrevButton", panel, assets.LeftArrow, layout, alignRight: false);
        manager.nextButton = CreateArrowButton("NextButton", panel, assets.RightArrow, layout, alignRight: true);
        manager.pageIndicatorText = CreatePageIndicator(panel, layout, assets);
        EditorUtility.SetDirty(manager);

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
        Selection.activeGameObject = panel.gameObject;
        Debug.Log("Painel de feedback recriado. Salve a cena para manter as mudanças.", panel);
    }

    private static bool ConfirmRebuild(RectTransform panel)
    {
        return EditorUtility.DisplayDialog(
            "Recriar painel de feedback",
            $"Isso apaga todos os filhos de '{panel.name}' e cria um layout novo, já ligado ao FeedbackManager.\n\nVocê pode desfazer com Ctrl+Z.",
            "Recriar",
            "Cancelar");
    }

    private static PanelAssets CollectExistingAssets(FeedbackManager manager, Transform panel)
    {
        return new PanelAssets
        {
            Font = FirstFontIn(panel),
            LeftArrow = SpriteOf(manager.prevButton) ?? FindSprite("setinha esquerda"),
            RightArrow = SpriteOf(manager.nextButton) ?? FindSprite("setinha direita"),
            RetryBackground = SpriteOf(manager.retryButton) ?? FindSprite("jogar novamente"),
        };
    }

    private static void DestroyChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(parent.GetChild(i).gameObject);
    }

    private static TextMeshProUGUI CreateTitle(Transform panel, Layout layout, PanelAssets assets)
    {
        TextMeshProUGUI title = CreateText("Title", panel, assets.Font, layout.FontSize(0.072f), AccentColor, TextAlignmentOptions.Center);
        title.fontStyle = FontStyles.Bold;
        title.enableAutoSizing = true;
        title.fontSizeMin = layout.FontSize(0.05f);
        title.fontSizeMax = layout.FontSize(0.072f);
        title.text = "Resumo do atendimento";
        AnchorToTop(title.rectTransform, layout.Padding, layout.Padding, layout.TitleHeight);
        return title;
    }

    private static void CreateDivider(Transform panel, Layout layout)
    {
        Color color = AccentColor;
        color.a = 0.25f;

        Image divider = CreateUIObject("Divider", panel).AddComponent<Image>();
        divider.color = color;
        divider.raycastTarget = false;

        float top = layout.Padding + layout.TitleHeight + layout.Gap * 0.3f;
        AnchorToTop(divider.rectTransform, layout.Padding, top, layout.Width * 0.006f);
    }

    private static TextMeshProUGUI CreateContent(Transform panel, Layout layout, PanelAssets assets)
    {
        TextMeshProUGUI content = CreateText("Content", panel, assets.Font, layout.FontSize(0.046f), TextColor, TextAlignmentOptions.TopLeft);
        content.overflowMode = TextOverflowModes.Page;
        content.text = "Conteúdo do feedback";

        float top = layout.Padding + layout.TitleHeight + layout.Gap;
        float bottom = layout.Padding + layout.FooterHeight + layout.Gap + layout.RetryHeight + layout.Gap;
        RectTransform rect = content.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(layout.Padding, bottom);
        rect.offsetMax = new Vector2(-layout.Padding, -top);
        return content;
    }

    private static Button CreateRetryButton(Transform panel, Layout layout, PanelAssets assets)
    {
        Button button = CreateButton("RetryButton", panel, assets.RetryBackground, Image.Type.Sliced);
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(layout.Width * 0.6f, layout.RetryHeight);
        rect.anchoredPosition = new Vector2(0f, layout.Padding + layout.FooterHeight + layout.Gap);

        TextMeshProUGUI label = CreateText("Label", rect, assets.Font, layout.FontSize(0.05f), Color.white, TextAlignmentOptions.Center);
        label.fontStyle = FontStyles.Bold;
        label.text = "Tentar novamente";
        Stretch(label.rectTransform);
        return button;
    }

    private static Button CreateArrowButton(string name, Transform panel, Sprite sprite, Layout layout, bool alignRight)
    {
        Button button = CreateButton(name, panel, sprite, Image.Type.Simple);
        ((Image)button.targetGraphic).preserveAspect = true;

        Vector2 corner = alignRight ? new Vector2(1f, 0f) : new Vector2(0f, 0f);
        float x = alignRight ? -layout.Padding : layout.Padding;

        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = corner;
        rect.sizeDelta = new Vector2(layout.FooterHeight, layout.FooterHeight);
        rect.anchoredPosition = new Vector2(x, layout.Padding);
        return button;
    }

    private static TextMeshProUGUI CreatePageIndicator(Transform panel, Layout layout, PanelAssets assets)
    {
        TextMeshProUGUI indicator = CreateText("PageIndicator", panel, assets.Font, layout.FontSize(0.042f), MutedColor, TextAlignmentOptions.Center);
        indicator.text = "1 / 1";

        float side = layout.Padding + layout.FooterHeight;
        RectTransform rect = indicator.rectTransform;
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.offsetMin = new Vector2(side, layout.Padding);
        rect.offsetMax = new Vector2(-side, layout.Padding + layout.FooterHeight);
        return indicator;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, TMP_FontAsset font, float size, Color color, TextAlignmentOptions alignment)
    {
        TextMeshProUGUI text = CreateUIObject(name, parent).AddComponent<TextMeshProUGUI>();
        if (font != null)
            text.font = font;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        text.richText = true;
        text.raycastTarget = false;
        return text;
    }

    private static Button CreateButton(string name, Transform parent, Sprite sprite, Image.Type imageType)
    {
        GameObject go = CreateUIObject(name, parent);
        Image image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.type = sprite != null ? imageType : Image.Type.Simple;

        Button button = go.AddComponent<Button>();
        button.targetGraphic = image;
        return button;
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Criar " + name);
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void AnchorToTop(RectTransform rect, float horizontalPadding, float top, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(horizontalPadding, -(top + height));
        rect.offsetMax = new Vector2(-horizontalPadding, -top);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static TMP_FontAsset FirstFontIn(Transform panel)
    {
        foreach (TextMeshProUGUI text in panel.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (text.font != null)
                return text.font;
        }

        return TMP_Settings.defaultFontAsset;
    }

    private static Sprite SpriteOf(Button button)
    {
        if (button == null || button.image == null || button.image.sprite == null)
            return null;

        return button.image.sprite;
    }

    private static Sprite FindSprite(string name)
    {
        foreach (string guid in AssetDatabase.FindAssets($"\"{name}\" t:Sprite"))
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guid));
            if (sprite != null)
                return sprite;
        }

        return null;
    }
}
