using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class MenuController : MonoBehaviour
{
    [Tooltip("The time scale to apply while this menu is open. Set to 0 to pause the game.")]
    [Range(0f, 1f)]
    public float menuTimeScale = 1f;

    [Tooltip("Whether world controls should remain enabled while this menu is open.")]
    public bool allowWorldControls = false;

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
        if (MenuManager.Instance != null && gameObject.activeInHierarchy)
            MenuManager.Instance.OpenMenu(this);
    }

    private void OnDisable()
    {
        if (MenuManager.Instance != null)
            MenuManager.Instance.CloseMenu(this);
    }

    public void Open()
    {
        if (activateOnOpen)
            gameObject.SetActive(true);

        if (!activateOnOpen && MenuManager.Instance != null)
            MenuManager.Instance.OpenMenu(this);

        if (canvasGroup != null)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }

    public void Close()
    {
        if (!deactivateOnClose && MenuManager.Instance != null)
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
