using TMPro;
using System.Collections.Generic;
using UnityEngine;

public class StreetDisplayManager : MonoBehaviour
{
    [Header("Street Label")]
    [SerializeField] private TMP_Text streetNameText;

    [Header("Detection")]
    [SerializeField] private string carTag = "Player";
    [SerializeField] private float streetCheckInterval = 0.1f;

    private readonly List<StreetAreaTrigger> streetTriggers = new List<StreetAreaTrigger>();
    private Collider[] carColliders;
    private float nextStreetCheckTime;
    private string currentStreetName;

    private void Awake()
    {
        RegisterStreetColliders();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextStreetCheckTime)
            return;

        nextStreetCheckTime = Time.unscaledTime + Mathf.Max(0.02f, streetCheckInterval);
        UpdateCurrentStreet();
    }

    private void RegisterStreetColliders()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);

        foreach (Collider col in colliders)
        {
            if (col == null)
                continue;

            StreetAreaTrigger trigger = col.GetComponent<StreetAreaTrigger>();
            if (trigger == null)
            {
                trigger = col.gameObject.AddComponent<StreetAreaTrigger>();
            }

            trigger.Initialize(this, col);
            streetTriggers.Add(trigger);

            col.isTrigger = true;
        }
    }

    private void UpdateCurrentStreet()
    {
        EnsureCarColliders();
        if (carColliders == null || carColliders.Length == 0)
            return;

        StreetAreaTrigger bestTrigger = null;
        float bestOverlap = 0f;

        foreach (StreetAreaTrigger trigger in streetTriggers)
        {
            if (trigger == null || trigger.Collider == null || trigger.Collider.enabled == false)
                continue;

            float overlap = trigger.CalculateOverlap(carColliders);
            if (overlap > bestOverlap)
            {
                bestOverlap = overlap;
                bestTrigger = trigger;
            }
        }

        if (bestTrigger != null && bestTrigger.StreetName != currentStreetName)
        {
            currentStreetName = bestTrigger.StreetName;
            ShowStreetName(currentStreetName);
        }
    }

    private void EnsureCarColliders()
    {
        if (carColliders != null && carColliders.Length > 0 && carColliders[0] != null)
            return;

        CarController car = FindFirstObjectByType<CarController>();
        if (car == null)
            return;

        carColliders = car.GetComponentsInChildren<Collider>();
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
        private Collider streetCollider;

        public Collider Collider => streetCollider;
        public string StreetName => gameObject.name;

        public void Initialize(StreetDisplayManager displayManager, Collider sourceCollider)
        {
            manager = displayManager;
            streetCollider = sourceCollider;
        }

        public float CalculateOverlap(Collider[] vehicleColliders)
        {
            if (manager == null || streetCollider == null)
                return 0f;

            float overlap = 0f;
            foreach (Collider vehicleCollider in vehicleColliders)
            {
                if (vehicleCollider == null || vehicleCollider == streetCollider || vehicleCollider.isTrigger)
                    continue;

                Bounds streetBounds = streetCollider.bounds;
                Bounds vehicleBounds = vehicleCollider.bounds;
                float overlapX = Mathf.Min(streetBounds.max.x, vehicleBounds.max.x) - Mathf.Max(streetBounds.min.x, vehicleBounds.min.x);
                float overlapY = Mathf.Min(streetBounds.max.y, vehicleBounds.max.y) - Mathf.Max(streetBounds.min.y, vehicleBounds.min.y);
                float overlapZ = Mathf.Min(streetBounds.max.z, vehicleBounds.max.z) - Mathf.Max(streetBounds.min.z, vehicleBounds.min.z);

                if (overlapX > 0f && overlapY > 0f && overlapZ > 0f)
                    overlap += overlapX * overlapY * overlapZ;
            }

            return overlap;
        }
    }
}
