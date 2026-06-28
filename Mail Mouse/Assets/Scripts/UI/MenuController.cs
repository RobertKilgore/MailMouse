using System.Linq;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class MenuController : MonoBehaviour
{
    [Tooltip("The time scale to apply while this menu is open. Set to 0 to pause the game.")]
    [Range(0f, 1f)]
    public float menuTimeScale = 1f;

    [Tooltip("Whether world controls should remain enabled while this menu is open.")]
    public bool allowWorldControls = false;

    [Tooltip("Whether player looking should remain enabled while this menu is open.")]
    public bool allowLooking = false;

    [Tooltip("If true, opening this menu also enables its UI GameObject.")]
    public bool activateOnOpen = true;

    [Tooltip("If true, closing this menu also disables its UI GameObject.")]
    public bool deactivateOnClose = true;

    [Tooltip("If true, this menu will close all other menus when opened.")]
    public bool forceExclusive = true;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        Debug.Log($"[MenuController] OnEnable: {gameObject.name}, activeInHierarchy: {gameObject.activeInHierarchy}");
        // Don't automatically register with MenuManager on enable
        // Only register when explicitly opened via Open()
    }

    private void OnDisable()
    {
        Debug.Log($"[MenuController] OnDisable: {gameObject.name}");
        // Only close if we were actually registered as an open menu
        if (MenuManager.Instance != null && MenuManager.Instance.GetActiveMenus().Any(m => m == this))
            MenuManager.Instance.CloseMenu(this);
    }

    public void Open()
    {
        Debug.Log($"[MenuController.Open] {gameObject.name}, activateOnOpen: {activateOnOpen}");
        if (activateOnOpen)
        {
            gameObject.SetActive(true);
            Debug.Log($"[MenuController.Open] SetActive(true), now active: {gameObject.activeSelf}");
        }

        // Always register with MenuManager when explicitly opened
        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.OpenMenu(this);
            Debug.Log($"[MenuController.Open] Registered with MenuManager");
        }

        if (canvasGroup != null)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            Debug.Log($"[MenuController.Open] CanvasGroup: interactable={canvasGroup.interactable}, blocksRaycasts={canvasGroup.blocksRaycasts}, alpha={canvasGroup.alpha}");
        }
    }

    public void Close()
    {
        Debug.Log($"[MenuController.Close] {gameObject.name}");
        InventoryHoverTooltip.HideTooltip();

        if (MenuManager.Instance != null)
            MenuManager.Instance.CloseMenu(this);

        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (deactivateOnClose)
            gameObject.SetActive(false);
    }
}
