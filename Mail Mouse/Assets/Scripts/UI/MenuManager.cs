using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance { get; private set; }

    private readonly List<MenuController> activeMenus = new List<MenuController>();
    private float previousTimeScale = 1f;

    public bool IsAnyMenuOpen => activeMenus.Count > 0;
    public bool AreWorldControlsEnabled { get; private set; } = true;
    public float CurrentTimeScale { get; private set; } = 1f;

    public static bool WorldControlsEnabled => Instance?.AreWorldControlsEnabled ?? true;
    public static bool IsGamePaused => Instance != null && Instance.CurrentTimeScale <= 0f;

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
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void OpenMenu(MenuController menu)
    {
        if (menu == null)
            return;

        if (activeMenus.Contains(menu))
            return;

        if (activeMenus.Count == 0)
            previousTimeScale = Time.timeScale;

        // If this menu is exclusive, close all other menus first
        if (menu.forceExclusive)
        {
            var menusToClose = new List<MenuController>(activeMenus);
            foreach (var otherMenu in menusToClose)
            {
                otherMenu.Close();
            }
        }

        activeMenus.Add(menu);
        ApplyMenuState();
    }

    public void CloseMenu(MenuController menu)
    {
        if (menu == null)
            return;

        if (!activeMenus.Remove(menu))
            return;

        ApplyMenuState();
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
            CurrentTimeScale = Time.timeScale;
            return;
        }

        float effectiveTimeScale = activeMenus.Min(m => m.menuTimeScale);
        bool effectiveControls = activeMenus.All(m => m.allowWorldControls);

        SetTimeScale(effectiveTimeScale);
        AreWorldControlsEnabled = effectiveControls;
        CurrentTimeScale = Time.timeScale;
    }

    private void SetTimeScale(float value)
    {
        Time.timeScale = value;
    }
}
