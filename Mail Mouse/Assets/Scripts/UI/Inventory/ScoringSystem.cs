using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles all scoring calculations for package deliveries.
/// Separates scoring logic from validation logic for better maintainability.
/// </summary>
public static class ScoringSystem
{
    /// <summary>
    /// Calculates a delivery score summary from a list of delivery results.
    /// </summary>
    public static InventoryValidationController.DeliveryScoreSummary GetDeliveryScoreSummary(List<InventoryValidationController.PackageDeliveryResult> results)
    {
        InventoryValidationController.DeliveryScoreSummary summary = new InventoryValidationController.DeliveryScoreSummary();

        if (results == null)
            return summary;

        summary.totalPackages = results.Count;

        foreach (InventoryValidationController.PackageDeliveryResult result in results)
        {
            if (result == null)
                continue;

            switch (result.status)
            {
                case InventoryValidationController.MailDeliveryStatus.DeliveredCorrect:
                    summary.correctPackages++;
                    break;
                case InventoryValidationController.MailDeliveryStatus.DeliveredIncorrect:
                    summary.incorrectPackages++;
                    break;
                case InventoryValidationController.MailDeliveryStatus.Undelivered:
                    summary.undeliveredPackages++;
                    break;
                case InventoryValidationController.MailDeliveryStatus.Unaccepted:
                    summary.unacceptedPackages++;
                    break;
            }

            summary.totalScore += result.score;
        }

        return summary;
    }

    /// <summary>
    /// Calculates the score for a single package based on its delivery status, size, and complexity.
    /// </summary>
    public static float CalculatePackageScore(InventoryValidationController.PackageDeliveryResult result)
    {
        if (result == null || result.itemData?.mailData == null)
            return 0f;

        float size = result.itemData.GetSize();
        float complexity = Mathf.Max(1, result.itemData.mailData.complexity);

        switch (result.status)
        {
            case InventoryValidationController.MailDeliveryStatus.Undelivered:
                return -1f;
            case InventoryValidationController.MailDeliveryStatus.DeliveredCorrect:
                return size * complexity;
            case InventoryValidationController.MailDeliveryStatus.DeliveredIncorrect:
                return -0.5f * size * complexity;
            case InventoryValidationController.MailDeliveryStatus.Unaccepted:
                return 0f;
            default:
                return 0f;
        }
    }

    /// <summary>
    /// Calculates the size of a package based on its shape definition.
    /// Size is the number of 'X' characters in the shape definition grid.
    /// Note: Use InventoryItemData.GetSize() for automatic caching.
    /// </summary>
    public static float GetPackageSize(InventoryItemData itemData)
    {
        if (itemData == null)
            return 1f;
        
        return itemData.GetSize();
    }
}
