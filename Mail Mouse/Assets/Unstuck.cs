using UnityEngine;

public class Unstuck : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private float distanceInFrontOfMailbox = 2f;
    [SerializeField] private float verticalOffset = 0.5f;
    [SerializeField] private float rotationOffset = 0f;

    public void UnstuckCar()
    {
        if (player == null)
        {
            Debug.LogWarning("Unstuck: Player is not assigned.", this);
            return;
        }

        MailboxInteractable[] mailboxes = FindObjectsByType<MailboxInteractable>(FindObjectsSortMode.None);
        if (mailboxes.Length == 0)
        {
            Debug.LogWarning("Unstuck: No mailboxes found in the scene.", this);
            return;
        }

        MailboxInteractable nearestMailbox = mailboxes[0];
        float nearestDistanceSquared = (nearestMailbox.transform.position - player.transform.position).sqrMagnitude;

        for (int i = 1; i < mailboxes.Length; i++)
        {
            float distanceSquared = (mailboxes[i].transform.position - player.transform.position).sqrMagnitude;
            if (distanceSquared < nearestDistanceSquared)
            {
                nearestMailbox = mailboxes[i];
                nearestDistanceSquared = distanceSquared;
            }
        }

        Transform mailboxTransform = nearestMailbox.transform;
        Vector3 targetPosition = mailboxTransform.position + mailboxTransform.forward * distanceInFrontOfMailbox;
        targetPosition.y += verticalOffset;
        Quaternion targetRotation = Quaternion.LookRotation(-mailboxTransform.forward, Vector3.up)
            * Quaternion.Euler(0f, rotationOffset, 0f);

        Rigidbody playerRigidbody = player.GetComponent<Rigidbody>();
        if (playerRigidbody != null)
        {
            playerRigidbody.position = targetPosition;
            playerRigidbody.rotation = targetRotation;
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
            return;
        }

        player.transform.SetPositionAndRotation(
            targetPosition,
            targetRotation);
    }
    }

