using System;

/// <summary>
/// Serializable container for mail-related metadata attached to inventory items.
/// </summary>
[Serializable]
public class MailData
{
    /// <summary>
    /// Name of the mail recipient.
    /// </summary>
    public string recipient;

    /// <summary>
    /// Destination address text.
    /// </summary>
    public string address;

    /// <summary>
    /// Whether this item was placed by the player during runtime.
    /// </summary>
    public bool placedByPlayer;

    /// <summary>
    /// Complexity multiplier used for scoring deliveries.
    /// </summary>
    public float complexity = 1f;

    /// <summary>
    /// Optional package score used by later delivery/quality systems.
    /// </summary>
    public int packageScore;

    /// <summary>
    /// Optional package modifier metadata, such as fragile, priority, or bonus.
    /// </summary>
    public string packageModifier;

    /// <summary>
    /// Optional mail item name for tooltip and display purposes.
    /// </summary>
    public string name;
}