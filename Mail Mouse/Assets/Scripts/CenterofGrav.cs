using UnityEngine;


public class CenterofGrav : MonoBehaviour
{
    public Transform centerOfGravity;
    private Rigidbody rb;


    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (centerOfGravity != null && rb != null)
        {
            rb.centerOfMass = centerOfGravity.localPosition;
        }
    }

}
