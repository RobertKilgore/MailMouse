using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if TMP_PRESENT
using TMPro;
#endif

/// <summary>
/// Shows a sprite indicator next to a selected button.
/// Attach to the button GameObject and assign the indicator image.
/// </summary>
[RequireComponent(typeof(Button))]
public class ButtonSelectionIndicator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("Optional graphic to show when this button is hovered.")]
    public GameObject selectionIcon;

    private void Awake()
    {
        if (selectionIcon != null)
            selectionIcon.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (selectionIcon != null)
            selectionIcon.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (selectionIcon != null)
            selectionIcon.SetActive(false);
    }

    private void OnDisable()
    {
        if (selectionIcon != null)
            selectionIcon.SetActive(false);
    }
}
