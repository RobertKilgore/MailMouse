using UnityEngine;
using UnityEngine.EventSystems;

public class UIClickTest : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("UI CLICK DETECTED");
    }
}