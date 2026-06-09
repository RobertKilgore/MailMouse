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
}