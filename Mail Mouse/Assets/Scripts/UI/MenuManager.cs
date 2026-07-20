using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance { get; private set; }

    private readonly List<MenuController> activeMenus = new List<MenuController>();
    private float previousTimeScale = 1f;

    public bool IsAnyMenuOpen => activeMenus.Count > 0;
    public bool AreWorldControlsEnabled { get; private set; } = true;
    public bool IsLookingEnabled { get; private set; } = true;
    public float CurrentTimeScale { get; private set; } = 1f;

    public static bool WorldControlsEnabled => Instance?.AreWorldControlsEnabled ?? true;
    public static bool LookingEnabled => Instance?.IsLookingEnabled ?? true;
    public static bool IsGamePaused => Instance != null && Instance.CurrentTimeScale <= 0f;
    public static bool AnyMenuOpen => Instance?.IsAnyMenuOpen ?? false;

    [Header("Menu UI References")]
    [Tooltip("Optional gameplay UI root container that menus can hide visually while open.")]
    public GameObject gameplayUIRoot;

    [Tooltip("Optional full-screen backdrop GameObject used for menu fade overlays.")]
    public GameObject backdropObject;

    private CanvasGroup gameplayUICanvasGroup;
    private float gameplayUIOriginalAlpha = 1f;
    private bool gameplayUIOriginalInteractable = true;
    private bool gameplayUIOriginalBlocksRaycasts = true;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.LogWarning("Multiple MenuManager instances were found. Destroying duplicate.", gameObject);
            Destroy(gameObject);
            return;
        }

        previousTimeScale = Time.timeScale;
        ApplyMenuState();
        Debug.Log($"[MenuManager] Initialized. Active menus count: {activeMenus.Count}, IsAnyMenuOpen: {IsAnyMenuOpen}");
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool OpenMenu(MenuController menu)
    {
        if (menu == null)
            return false;

        if (activeMenus.Contains(menu))
        {
            CloseMenu(menu);
            return false;
        }

        if (activeMenus.Count == 0)
            previousTimeScale = Time.timeScale;

        var samePriorityActive = activeMenus.Where(other => other != menu && other.priority == menu.priority).ToList();
        if (samePriorityActive.Count > 0)
        {
            Debug.Log($"[MenuManager] Same-priority menu already active; closing the competing menu and leaving {menu.gameObject.name} closed.");
            foreach (var otherMenu in samePriorityActive)
            {
                otherMenu.Close();
            }
            return false;
        }

        var menusToClose = activeMenus.Where(other => other != menu && ShouldCloseOtherMenu(menu, other)).ToList();
        foreach (var otherMenu in menusToClose)
        {
            otherMenu.Close();
        }

        if (activeMenus.Any(other => other != menu && other.priority > menu.priority))
        {
            Debug.Log($"[MenuManager] Blocked opening {menu.gameObject.name} because a higher-priority menu remains active.");
            return false;
        }

        activeMenus.Add(menu);
        Debug.Log($"[MenuManager] Opened menu: {menu.gameObject.name}. Total active menus: {activeMenus.Count}");
        ApplyMenuState();
        RefreshGameplayUIVisibility();
        RefreshBackdropVisibility();
        return true;
    }

    public bool ToggleMenu(MenuController menu)
    {
        if (menu == null)
            return false;

        if (activeMenus.Contains(menu) || menu.IsOpen)
        {
            menu.Close();
            return false;
        }

        return menu.Open();
    }

    private bool ShouldCloseOtherMenu(MenuController openingMenu, MenuController otherMenu)
    {
        if (otherMenu == null || otherMenu == openingMenu)
            return false;

        if (otherMenu.priority > openingMenu.priority)
            return false;

        if (openingMenu.forceExclusive)
            return true;

        if (otherMenu.priority < openingMenu.priority)
            return true;

        return otherMenu.priority == openingMenu.priority;
    }

    public void CloseMenu(MenuController menu)
    {
        if (menu == null)
            return;

        if (!activeMenus.Remove(menu))
            return;

        Debug.Log($"[MenuManager] Closed menu: {menu.gameObject.name}. Total active menus: {activeMenus.Count}");
        ApplyMenuState();
        RefreshGameplayUIVisibility();
        RefreshBackdropVisibility();
    }

    public void RefreshGameplayUIVisibility()
    {
        if (gameplayUIRoot == null)
            return;

        CanvasGroup canvasGroup = GetGameplayUICanvasGroup();
        if (canvasGroup == null)
            return;

        bool shouldHideGameplayUI = activeMenus.Any(m => m.hideGameplayUI);
        if (shouldHideGameplayUI)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            return;
        }

        canvasGroup.alpha = gameplayUIOriginalAlpha;
        canvasGroup.interactable = gameplayUIOriginalInteractable;
        canvasGroup.blocksRaycasts = gameplayUIOriginalBlocksRaycasts;
    }

    public void RefreshBackdropVisibility()
    {
        GameObject backdrop = GetBackdropObject();
        if (backdrop == null)
            return;

        bool shouldShowBackdrop = activeMenus.Any(m => m.useBackdrop);
        backdrop.SetActive(shouldShowBackdrop);
        if (shouldShowBackdrop)
            backdrop.transform.SetAsFirstSibling();
    }

    private GameObject GetBackdropObject()
    {
        if (backdropObject != null)
            return backdropObject;

        if (gameplayUIRoot == null)
            return null;

        Transform found = gameplayUIRoot.transform.Find("Backdrop");
        if (found != null)
            backdropObject = found.gameObject;

        return backdropObject;
    }

    private CanvasGroup GetGameplayUICanvasGroup()
    {
        if (gameplayUICanvasGroup != null)
            return gameplayUICanvasGroup;

        if (gameplayUIRoot == null)
            return null;

        gameplayUICanvasGroup = gameplayUIRoot.GetComponent<CanvasGroup>();
        if (gameplayUICanvasGroup == null)
            gameplayUICanvasGroup = gameplayUIRoot.AddComponent<CanvasGroup>();

        gameplayUIOriginalAlpha = gameplayUICanvasGroup.alpha;
        gameplayUIOriginalInteractable = gameplayUICanvasGroup.interactable;
        gameplayUIOriginalBlocksRaycasts = gameplayUICanvasGroup.blocksRaycasts;

        return gameplayUICanvasGroup;
    }

    public IReadOnlyList<MenuController> GetActiveMenus()
    {
        return activeMenus.AsReadOnly();
    }

    private void ApplyMenuState()
    {
        if (activeMenus.Count == 0)
        {
            SetTimeScale(previousTimeScale);
            AreWorldControlsEnabled = true;
            IsLookingEnabled = true;
            CurrentTimeScale = Time.timeScale;
            RefreshCinemachineInputState();
            UpdateCursorState();
            return;
        }

        float effectiveTimeScale = activeMenus.Min(m => m.menuTimeScale);
        bool effectiveControls = activeMenus.All(m => m.allowWorldControls);
        bool effectiveLooking = activeMenus.All(m => m.allowLooking);

        SetTimeScale(effectiveTimeScale);
        AreWorldControlsEnabled = effectiveControls;
        IsLookingEnabled = effectiveLooking;
        CurrentTimeScale = Time.timeScale;
        RefreshCinemachineInputState();
        UpdateCursorState();
    }

    private void RefreshCinemachineInputState()
    {
        foreach (var controller in UnityEngine.Object.FindObjectsByType<CinemachineInputAxisController>(FindObjectsSortMode.None))
        {
            if (controller != null)
                controller.enabled = IsLookingEnabled;
        }
    }

    private void UpdateCursorState()
    {
        if (activeMenus.Count > 0)
        {
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void SetTimeScale(float value)
    {
        Time.timeScale = value;
    }
}
