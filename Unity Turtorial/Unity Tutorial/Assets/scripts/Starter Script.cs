using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    [SerializeField] int myNumber;
    [SerializeField] string myName;
    [SerializeField] bool myChoice;
    int myOtherNumber;
    [SerializeField] GameObject myGate;
    [SerializeField] GameObject nameone;
    [SerializeField] GameObject nametwo;

 
    void Start()
    {
        myNumber = 3;
        myChoice = true;
    }

  
    void Update()
    {
        if (myNumber == 4 && myChoice == true)
        {
            myName = "Jimmy";
            myGate.SetActive(true);
            nameone.GetComponent<TMPro.TMP_Text>().text = " My Number: " + myNumber + "         My Name:" + myName;
            nametwo.GetComponent<TMPro.TMP_Text>().text = "________";
        }
        else
        {
            myName = "Fred";
            myGate.SetActive(false);
            nametwo.GetComponent<TMPro.TMP_Text>().text = "Fred is active";
            nameone.GetComponent<TMPro.TMP_Text>().text = "Jimmy is inactive";

        }
    }
}
