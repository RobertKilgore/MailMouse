using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Computes delivery status and scoring for scene mail packages.
/// Skips inventories marked with IgnoreForValidation and reports package delivery details.
/// </summary>
[DisallowMultipleComponent]
public class InventoryValidationController : MonoBehaviour
{
    [Header("Delivery Options")]
    [Tooltip("Skip inventories whose InventoryDataHolder.ignoreForValidation flag is set.")]
    public bool skipIgnoredInventories = true;

    [Header("Logging")]
    [Tooltip("Log delivery summary to the Unity console.")]
    public bool logDeliveryResults = true;

    [Tooltip("Log details for unaccepted or incorrectly delivered packages.")]
    public bool logPackageDetails = true;

    public enum MailDeliveryStatus
    {
        Undelivered,
        DeliveredCorrect,
        DeliveredIncorrect,
        Unaccepted
    }

    public class PackageDeliveryResult
    {
        public InventoryDataHolder container;
        public InventoryData inventoryData;
        public InventoryItemData itemData;
        public MailDeliveryStatus status;
        public bool playerPlaced;
        public float score;
    }

    public class DeliveryScoreSummary
    {
        public int totalPackages;
        public int correctPackages;
        public int incorrectPackages;
        public int undeliveredPackages;
        public int unacceptedPackages;
        public float totalScore;
    }

    /// <summary>
    /// Validates every InventoryDataHolder in the active scene.
    /// </summary>
    [ContextMenu("Log Scene Delivery Summary")]
    public void LogSceneDeliverySummary()
    {
        List<PackageDeliveryResult> results = GetAllPackageDeliveryResults(skipIgnoredInventories);
        DeliveryScoreSummary summary = GetDeliveryScoreSummary(results);

        if (!logDeliveryResults)
            return;

        Debug.Log($"Scene delivery summary complete. Total packages={summary.totalPackages}, correct={summary.correctPackages}, incorrect={summary.incorrectPackages}, undelivered={summary.undeliveredPackages}, unaccepted={summary.unacceptedPackages}, total score={summary.totalScore}.");

        if (logPackageDetails)
            LogPackageDetails(results);
    }

    public List<PackageDeliveryResult> GetAllPackageDeliveryResults(bool skipIgnored = true)
    {
        List<PackageDeliveryResult> results = new List<PackageDeliveryResult>();
        InventoryDataHolder[] holders = Object.FindObjectsByType<InventoryDataHolder>(FindObjectsSortMode.None);

        foreach (InventoryDataHolder holder in holders)
        {
            if (holder == null)
                continue;

            if (skipIgnored && holder.ignoreForValidation)
                continue;

            InventoryData inventoryData = holder.inventoryData;
            if (inventoryData == null || inventoryData.items == null)
                continue;

            for (int i = 0; i < inventoryData.items.Count; i++)
            {
                InventoryItemData itemData = inventoryData.items[i];
                results.Add(CreatePackageResult(holder, inventoryData, itemData));
            }
        }

        return results;
    }

    public List<PackageDeliveryResult> GetDeliveredCorrectPackages(bool skipIgnored = true)
    {
        return FilterByStatus(MailDeliveryStatus.DeliveredCorrect, skipIgnored);
    }

    public List<PackageDeliveryResult> GetDeliveredIncorrectPackages(bool skipIgnored = true)
    {
        return FilterByStatus(MailDeliveryStatus.DeliveredIncorrect, skipIgnored);
    }

    public List<PackageDeliveryResult> GetUndeliveredPackages(bool skipIgnored = true)
    {
        return FilterByStatus(MailDeliveryStatus.Undelivered, skipIgnored);
    }

    public List<PackageDeliveryResult> GetUnacceptedPackages(bool skipIgnored = true)
    {
        return FilterByStatus(MailDeliveryStatus.Unaccepted, skipIgnored);
    }

    public DeliveryScoreSummary GetDeliveryScoreSummary(List<PackageDeliveryResult> results)
    {
        // Delegate to ScoringSystem for summary calculation
        return ScoringSystem.GetDeliveryScoreSummary(results);
    }

    private PackageDeliveryResult CreatePackageResult(InventoryDataHolder holder, InventoryData inventoryData, InventoryItemData itemData)
    {
        PackageDeliveryResult result = new PackageDeliveryResult
        {
            container = holder,
            inventoryData = inventoryData,
            itemData = itemData,
            playerPlaced = itemData?.mailData != null && itemData.mailData.placedByPlayer
        };

        result.status = CalculateDeliveryStatus(result);
        result.score = ScoringSystem.CalculatePackageScore(result);

        return result;
    }

    private List<PackageDeliveryResult> FilterByStatus(MailDeliveryStatus status, bool skipIgnored)
    {
        List<PackageDeliveryResult> results = GetAllPackageDeliveryResults(skipIgnored);
        return results.FindAll(result => result != null && result.status == status);
    }

    private MailDeliveryStatus CalculateDeliveryStatus(PackageDeliveryResult result)
    {
        if (result?.itemData?.mailData == null)
            return MailDeliveryStatus.Unaccepted;

        InventoryDataHolder holder = result.container;
        InventoryData inventoryData = result.inventoryData;
        MailData mailData = result.itemData.mailData;

        if (IsPlayerInventory(inventoryData))
            return MailDeliveryStatus.Undelivered;

        if (!mailData.placedByPlayer)
            return MailDeliveryStatus.Unaccepted;

        string inventoryAddress = inventoryData?.address;
        string itemAddress = mailData.address;

        if (!string.IsNullOrWhiteSpace(inventoryAddress) && inventoryAddress == itemAddress)
            return MailDeliveryStatus.DeliveredCorrect;

        return MailDeliveryStatus.DeliveredIncorrect;
    }



    private bool IsPlayerInventory(InventoryData inventoryData)
    {
        return inventoryData != null && inventoryData.inventoryType == InventoryType.Player;
    }

    private void LogPackageDetails(List<PackageDeliveryResult> results)
    {
        if (results == null)
            return;

        foreach (PackageDeliveryResult result in results)
        {
            if (result == null)
                continue;

            string containerId = result.inventoryData?.inventoryId ?? result.container?.name ?? "unknown container";
            Debug.Log($"Package in '{containerId}': item='{result.itemData?.itemId ?? "unknown"}', status='{result.status}', score={result.score}, playerPlaced={result.playerPlaced}", result.container);
        }
    }
}
