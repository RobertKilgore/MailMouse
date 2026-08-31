using TMPro;
using UnityEngine;

public class StreetDisplayManager : MonoBehaviour
{
    [Header("Street Label")]
    [SerializeField] private TMP_Text streetNameText;

    [Header("Detection")]
    [SerializeField] private string carTag = "Player";

    private void Awake()
    {
        RegisterStreetColliders();
    }

    private void RegisterStreetColliders()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);

        foreach (Collider col in colliders)
        {
            if (col == null)
                continue;

            if (col.GetComponent<StreetAreaTrigger>() == null)
            {
                StreetAreaTrigger trigger = col.gameObject.AddComponent<StreetAreaTrigger>();
                trigger.Initialize(this);
            }

            col.isTrigger = true;
        }
    }

    public void ShowStreetName(string streetName)
    {
        if (streetNameText == null)
        {
            Debug.LogWarning("StreetDisplayManager: No TMP_Text reference assigned.", this);
            return;
        }

        streetNameText.text = streetName;
    }

    private bool IsCar(Collider other)
    {
        if (string.IsNullOrEmpty(carTag) == false && other.CompareTag(carTag))
            return true;

        if (other.GetComponent<CarController>() != null)
            return true;

        if (other.attachedRigidbody != null && other.attachedRigidbody.GetComponent<CarController>() != null)
            return true;

        if (other.GetComponentInParent<CarController>() != null)
            return true;

        return false;
    }

    private sealed class StreetAreaTrigger : MonoBehaviour
    {
        private StreetDisplayManager manager;

        public void Initialize(StreetDisplayManager displayManager)
        {
            manager = displayManager;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (manager == null || manager.IsCar(other) == false)
                return;

            manager.ShowStreetName(gameObject.name);
        }
    }
}
