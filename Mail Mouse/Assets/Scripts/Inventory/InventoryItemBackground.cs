using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryItemBackground : MonoBehaviour, IPointerDownHandler
{
    public InventoryItem parentItem;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (parentItem == null)
            return;

        parentItem.TryBeginDragFromBackground(this, eventData);
    }
}