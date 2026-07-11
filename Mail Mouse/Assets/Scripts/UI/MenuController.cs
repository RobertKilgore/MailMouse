using System;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class MenuController : MonoBehaviour
{
    [Header("Menu Behavior")]
    [Tooltip("The time scale to apply while this menu is open. Set to 0 to pause the game.")]
    [Range(0f, 1f)]
    [SerializeField] private float timeScaleWhenOpen = 1f;

    [Tooltip("Whether world controls should remain enabled while this menu is open.")]
    [SerializeField] private bool allowWorldControlsWhileOpen = false;

    [Tooltip("Whether player looking should remain enabled while this menu is open.")]
    [SerializeField] private bool allowLookingWhileOpen = false;

    [Header("Menu Priority")]
    [Tooltip("If true, opening this menu closes all other menus first.")]
    [SerializeField] private bool closeOtherMenusOnOpen = true;

    [Tooltip("Higher values can dismiss lower-priority menus. Same-priority menus are treated as competing.")]
    [SerializeField] private int menuPriority = 0;

    [Header("Visual Behavior")]
    [Tooltip("If true, this menu will open with a full-screen backdrop image behind its content.")]
    [SerializeField] private bool showBackdropWhenOpen = false;

    [Tooltip("If true, this menu will hide the gameplay UI root while the menu is open.")]
    [SerializeField] private bool hideGameplayUiWhenOpen = false;

    public event Action<MenuController> Opened;
    public event Action<MenuController> Closed;

    public bool IsOpen { get; private set; }

    public System.Action<MenuController> OnOpenRequested { get; set; }
    public System.Action<MenuController> OnCloseRequested { get; set; }

    public float menuTimeScale => timeScaleWhenOpen;
    public bool allowWorldControls => allowWorldControlsWhileOpen;
    public bool allowLooking => allowLookingWhileOpen;
    public bool forceExclusive => closeOtherMenusOnOpen;
    public int priority => menuPriority;
    public bool useBackdrop => showBackdropWhenOpen;
    public bool hideGameplayUI => hideGameplayUiWhenOpen;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        IsOpen = gameObject.activeSelf;
    }

    private void OnEnable()
    {
        Debug.Log($"[MenuController] OnEnable: {gameObject.name}, activeInHierarchy: {gameObject.activeInHierarchy}");
        IsOpen = gameObject.activeSelf;
        // Don't automatically register with MenuManager on enable
        // Only register when explicitly opened via Open()
    }

    private void SetVisualState(bool visible)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }

    private void OnDisable()
    {
        Debug.Log($"[MenuController] OnDisable: {gameObject.name}");
        IsOpen = false;
        // Only close if we were actually registered as an open menu
        if (MenuManager.Instance != null && MenuManager.Instance.GetActiveMenus().Any(m => m == this))
            MenuManager.Instance.CloseMenu(this);
    }

    public bool Open()
    {
        Debug.Log($"[MenuController.Open] {gameObject.name}");
        if (IsOpen && gameObject.activeSelf)
        {
            if (MenuManager.Instance != null && !MenuManager.Instance.GetActiveMenus().Any(m => m == this))
                MenuManager.Instance.OpenMenu(this);
            return true;
        }

        bool opened = true;
        if (MenuManager.Instance != null)
        {
            opened = MenuManager.Instance.OpenMenu(this);
            Debug.Log($"[MenuController.Open] Registered with MenuManager: {opened}");
        }

        if (!opened)
        {
            Debug.Log($"[MenuController.Open] Open request ignored for {gameObject.name}");
            return false;
        }

        gameObject.SetActive(true);
        Debug.Log($"[MenuController.Open] SetActive(true), now active: {gameObject.activeSelf}");

        if (canvasGroup != null)
        {
            SetVisualState(true);
            Debug.Log($"[MenuController.Open] CanvasGroup: interactable={canvasGroup.interactable}, blocksRaycasts={canvasGroup.blocksRaycasts}, alpha={canvasGroup.alpha}");
        }

        IsOpen = true;
        OnOpenRequested?.Invoke(this);
        Opened?.Invoke(this);
        return true;
    }

    public bool Close()
    {
        Debug.Log($"[MenuController.Close] {gameObject.name}");
        InventoryHoverTooltip.HideTooltip();

        bool wasOpen = IsOpen || gameObject.activeSelf;
        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.CloseMenu(this);
        }

        if (canvasGroup != null)
        {
            SetVisualState(false);
        }

        gameObject.SetActive(false);

        IsOpen = false;
        OnCloseRequested?.Invoke(this);
        if (wasOpen)
            Closed?.Invoke(this);
        return true;
    }
}

