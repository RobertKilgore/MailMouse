using Unity.VisualScripting;
using UnityEngine;

public class MailboxDistanceCheck : MonoBehaviour
{
    public GameObject mailTruck;
    public GameObject thisMailbox;
    public GameObject targetMarker;
    [SerializeField] bool allowOpen = false;
    [SerializeField] float range;
   
    void Start()
    {
        targetMarker.SetActive(false);
    }

    void Update()
    {
          range = Vector3.Distance(mailTruck.transform.position, thisMailbox.transform.position);
            if (range < 7)
    {
            allowOpen = true;
            targetMarker.SetActive(true);

        }
            else
        {
            allowOpen = false;
            targetMarker.SetActive(false);
        }
    
      

    }
    
    
}
