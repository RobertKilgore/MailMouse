using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// Displays the player's current delivery list as a text block and slides the list UI on and off screen
/// as the number of package addresses changes.
/// </summary>
public class DeliveryListController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InventoryDataHolder playerInventoryDataHolder;
    [SerializeField] private RectTransform listRoot;
    [SerializeField] private TMP_Text deliveryText;

    [Header("Sliding")]
    [SerializeField] private float offscreenY = -500f;
    [SerializeField] private float visibleY = -80f;
    [SerializeField] private float rowSlideAmount = 26f;
    [SerializeField] private float maxSlideOffset = 180f;
    [SerializeField] private float slideDuration = 0.2f;

    private Coroutine slideRoutine;
    private int lastAddressCount;

    private void Reset()
    {
        listRoot = GetComponent<RectTransform>();
        deliveryText = GetComponentInChildren<TMP_Text>(true);
    }

    private void Awake()
    {
        if (listRoot == null)
            listRoot = GetComponent<RectTransform>();

        if (deliveryText == null)
            deliveryText = GetComponentInChildren<TMP_Text>(true);

        if (listRoot != null)
            listRoot.anchoredPosition = new Vector2(listRoot.anchoredPosition.x, offscreenY);
    }

    private void OnEnable()
    {
        TryResolvePlayerInventory();
        RefreshFromPlayerInventory();
    }

    public void SetPlayerInventoryDataHolder(InventoryDataHolder holder)
    {
        playerInventoryDataHolder = holder;
        RefreshFromPlayerInventory();
    }

    private void TryResolvePlayerInventory()
    {
        if (playerInventoryDataHolder != null)
            return;

        InventoryDataHolder[] holders = Resources.FindObjectsOfTypeAll<InventoryDataHolder>();
        foreach (InventoryDataHolder holder in holders)
        {
            if (holder == null)
                continue;

            foreach (InventoryData inventory in holder.GetAllInventoryData())
            {
                if (inventory != null && inventory.inventoryType == InventoryType.Player)
                {
                    playerInventoryDataHolder = holder;
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Rebuilds the list from the player inventory and animates the panel to match the new item count.
    /// </summary>
    public void RefreshFromPlayerInventory()
    {
        TryResolvePlayerInventory();

        if (playerInventoryDataHolder == null)
        {
            if (deliveryText != null)
                deliveryText.text = string.Empty;

            SlideTo(offscreenY);
            lastAddressCount = 0;
            return;
        }

        List<KeyValuePair<string, int>> addressCounts = playerInventoryDataHolder.GetAllPackageDeliveryAddressCounts();
        BuildAddressText(addressCounts);

        int currentAddressCount = addressCounts.Count;
        float visibleOffset = currentAddressCount > 0 ? Mathf.Min(maxSlideOffset, currentAddressCount * rowSlideAmount) : 0f;
        float positionY = currentAddressCount > 0 ? visibleY + visibleOffset : offscreenY;

        SlideTo(positionY);
        lastAddressCount = currentAddressCount;
    }

    private void BuildAddressText(List<KeyValuePair<string, int>> addressCounts)
    {
        if (deliveryText == null)
            return;

        if (addressCounts == null || addressCounts.Count == 0)
        {
            deliveryText.text = string.Empty;
            return;
        }

        StringBuilder builder = new StringBuilder();
        foreach (KeyValuePair<string, int> addressCount in addressCounts)
        {
            builder.AppendLine($"{addressCount.Value}x  -  {addressCount.Key}");
        }

        deliveryText.text = builder.ToString().TrimEnd();
    }

    private void SlideTo(float targetY)
    {
        if (listRoot == null)
            return;

        if (slideRoutine != null)
            StopCoroutine(slideRoutine);

        Vector2 start = listRoot.anchoredPosition;
        slideRoutine = StartCoroutine(SlideRoutine(start, new Vector2(start.x, targetY)));
    }

    private IEnumerator SlideRoutine(Vector2 from, Vector2 to)
    {
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);
            listRoot.anchoredPosition = Vector2.Lerp(from, to, t);
            yield return null;
        }

        listRoot.anchoredPosition = to;
        slideRoutine = null;
    }
}
