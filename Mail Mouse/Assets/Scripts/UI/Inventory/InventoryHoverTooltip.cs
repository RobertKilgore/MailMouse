using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class InventoryHoverTooltip : MonoBehaviour
{
    public static InventoryHoverTooltip Instance { get; private set; }

    [SerializeField]
    private TMP_Text tooltipText;

    [SerializeField]
    private RectTransform backgroundRect;

    [SerializeField]
    private Vector2 tooltipOffset = new Vector2(12f, -12f);

    [SerializeField]
    private bool clampToScreen = true;

    [SerializeField]
    private Vector2 screenMargin = new Vector2(8f, 8f);

    private CanvasGroup canvasGroup;
    private Canvas tooltipCanvas;
    private RectTransform tooltipRootRect;
    private RectTransform tooltipCanvasRect;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        tooltipRootRect = GetComponent<RectTransform>();
        if (tooltipRootRect == null)
            tooltipRootRect = gameObject.AddComponent<RectTransform>();

        tooltipCanvas = GetComponentInParent<Canvas>();
        if (tooltipCanvas == null)
        {
            tooltipCanvas = gameObject.AddComponent<Canvas>();
            tooltipCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            gameObject.AddComponent<GraphicRaycaster>();
        }

        tooltipCanvasRect = tooltipCanvas != null ? tooltipCanvas.transform as RectTransform : null;
        EnsureTooltipSetup();
        SetVisibility(false);
    }

    private void OnEnable()
    {
        if (Instance == null)
            Instance = this;

        SetVisibility(false);
    }

    private void OnDisable()
    {
        SetVisibility(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private Camera GetCanvasCamera()
    {
        if (tooltipCanvas == null)
            return null;

        return tooltipCanvas.renderMode == RenderMode.ScreenSpaceCamera || tooltipCanvas.renderMode == RenderMode.WorldSpace
            ? tooltipCanvas.worldCamera
            : null;
    }

    private void LateUpdate()
    {
        if (canvasGroup == null || backgroundRect == null)
            return;

        Vector2 pointerPosition = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;

        if (canvasGroup.alpha <= 0f)
            return;

        RectTransform targetRect = tooltipCanvasRect != null ? tooltipCanvasRect : tooltipRootRect;

        if (targetRect == null)
            return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            targetRect,
            pointerPosition,
            GetCanvasCamera(),
            out Vector2 anchoredPosition))
        {
            Vector2 finalPosition = anchoredPosition + tooltipOffset;

            if (clampToScreen)
            {
                Rect targetRectRect = targetRect.rect;
                Vector2 bgSize = backgroundRect.rect.size;
                float minX = targetRectRect.xMin + screenMargin.x + bgSize.x * backgroundRect.pivot.x;
                float maxX = targetRectRect.xMax - screenMargin.x - bgSize.x * (1f - backgroundRect.pivot.x);
                float minY = targetRectRect.yMin + screenMargin.y + bgSize.y * backgroundRect.pivot.y;
                float maxY = targetRectRect.yMax - screenMargin.y - bgSize.y * (1f - backgroundRect.pivot.y);
                finalPosition.x = Mathf.Clamp(finalPosition.x, minX, maxX);
                finalPosition.y = Mathf.Clamp(finalPosition.y, minY, maxY);
            }

            backgroundRect.anchoredPosition = finalPosition;
        }
    }

    private void ShowTooltipInstance(string text)
    {
        if (tooltipText == null || canvasGroup == null || backgroundRect == null)
            return;

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        BringToFrontInstance();
        backgroundRect.gameObject.SetActive(true);
        tooltipText.text = text;
        UpdatePosition(GetCurrentPointerPosition());
        SetVisibility(true);
    }

    private void HideTooltipInstance()
    {
        SetVisibility(false);
        if (backgroundRect != null)
        {
            backgroundRect.anchoredPosition = Vector2.zero;
            backgroundRect.gameObject.SetActive(false);
        }

        gameObject.SetActive(false);
    }

    private void BringToFrontInstance()
    {
        if (transform != null)
            transform.SetAsLastSibling();
    }

    private void EnsureTooltipSetup()
    {
        if (backgroundRect == null)
        {
            GameObject backgroundGo = transform.Find("Background")?.gameObject ?? new GameObject("Background");
            backgroundGo.transform.SetParent(transform, false);
            backgroundRect = backgroundGo.GetComponent<RectTransform>() ?? backgroundGo.AddComponent<RectTransform>();

            Image image = backgroundGo.GetComponent<Image>();
            if (image == null)
                image = backgroundGo.AddComponent<Image>();

            image.color = new Color(0.08f, 0.08f, 0.08f, 0.95f);
            image.raycastTarget = false;
            image.type = Image.Type.Sliced;
        }

        if (tooltipText == null)
        {
            TextMeshProUGUI existingText = transform.GetComponentInChildren<TextMeshProUGUI>(true);
            if (existingText != null && existingText != this.tooltipText)
                tooltipText = existingText;

            if (tooltipText == null)
            {
                GameObject textGo = transform.Find("Text")?.gameObject ?? new GameObject("Text");
                textGo.transform.SetParent(backgroundRect != null ? backgroundRect : transform, false);
                tooltipText = textGo.GetComponent<TextMeshProUGUI>() ?? textGo.AddComponent<TextMeshProUGUI>();
            }

            if (tooltipText != null)
            {
                tooltipText.text = string.Empty;
                tooltipText.fontSize = 14f;
                tooltipText.alignment = TextAlignmentOptions.Center;
                tooltipText.color = Color.white;
                tooltipText.raycastTarget = false;
                tooltipText.enableAutoSizing = false;

                RectTransform textRect = tooltipText.GetComponent<RectTransform>();
                if (textRect != null)
                {
                    textRect.anchorMin = Vector2.zero;
                    textRect.anchorMax = Vector2.one;
                    textRect.offsetMin = new Vector2(8f, 6f);
                    textRect.offsetMax = new Vector2(-8f, -6f);
                }
            }
        }

        if (backgroundRect != null)
        {
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.zero;
            backgroundRect.sizeDelta = new Vector2(180f, 38f);
            backgroundRect.pivot = new Vector2(0.5f, 0.5f);
        }
    }

    private Vector2 GetCurrentPointerPosition()
    {
        if (Mouse.current != null)
            return Mouse.current.position.ReadValue();

        return Vector2.zero;
    }

    private void UpdatePosition(Vector2 pointerPosition)
    {
        RectTransform targetRect = tooltipCanvasRect != null ? tooltipCanvasRect : tooltipRootRect;
        if (targetRect == null || backgroundRect == null)
            return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            targetRect,
            pointerPosition,
            GetCanvasCamera(),
            out Vector2 anchoredPosition))
        {
            Vector2 finalPosition = anchoredPosition + tooltipOffset;

            if (clampToScreen)
            {
                Rect targetRectRect = targetRect.rect;
                Vector2 bgSize = backgroundRect.rect.size;
                float minX = targetRectRect.xMin + screenMargin.x + bgSize.x * backgroundRect.pivot.x;
                float maxX = targetRectRect.xMax - screenMargin.x - bgSize.x * (1f - backgroundRect.pivot.x);
                float minY = targetRectRect.yMin + screenMargin.y + bgSize.y * backgroundRect.pivot.y;
                float maxY = targetRectRect.yMax - screenMargin.y - bgSize.y * (1f - backgroundRect.pivot.y);
                finalPosition.x = Mathf.Clamp(finalPosition.x, minX, maxX);
                finalPosition.y = Mathf.Clamp(finalPosition.y, minY, maxY);
            }

            backgroundRect.anchoredPosition = finalPosition;
        }
    }

    private void SetVisibility(bool visible)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private static InventoryHoverTooltip GetOrFindInstance()
    {
        if (Instance != null)
            return Instance;

        InventoryHoverTooltip found = FindFirstTooltipInScene();
        if (found != null)
        {
            Instance = found;
            return Instance;
        }

        GameObject tooltipGo = new GameObject("InventoryHoverTooltip", typeof(RectTransform), typeof(CanvasGroup), typeof(InventoryHoverTooltip));
        Instance = tooltipGo.GetComponent<InventoryHoverTooltip>();
        return Instance;
    }

    private static InventoryHoverTooltip FindFirstTooltipInScene()
    {
        InventoryHoverTooltip[] all = Resources.FindObjectsOfTypeAll<InventoryHoverTooltip>();
        foreach (InventoryHoverTooltip tooltip in all)
        {
            if (tooltip != null && tooltip.gameObject != null && tooltip.gameObject.scene.isLoaded)
                return tooltip;
        }

        return null;
    }

    public static void ShowTooltip(string text)
    {
        InventoryHoverTooltip instance = GetOrFindInstance();
        if (instance != null)
        {
            Debug.Log($"InventoryHoverTooltip.ShowTooltip: '{text}' active={instance.gameObject.activeSelf}", instance);
            instance.ShowTooltipInstance(text);
        }
        else
        {
            Debug.Log("InventoryHoverTooltip.ShowTooltip: no instance found");
        }
    }

    public static void HideTooltip()
    {
        InventoryHoverTooltip instance = GetOrFindInstance();
        if (instance != null)
            Debug.Log($"InventoryHoverTooltip.HideTooltip active={instance.gameObject.activeSelf}", instance);
        else
            Debug.Log("InventoryHoverTooltip.HideTooltip: no instance found");

        instance?.HideTooltipInstance();
    }

    public static void BringToFront()
    {
        GetOrFindInstance()?.BringToFrontInstance();
    }
}
