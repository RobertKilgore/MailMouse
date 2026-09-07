using Unity.VisualScripting;
using UnityEngine;

public class FadeInOut : MonoBehaviour
{
    [SerializeField] private GameObject fadeInAnimation;
    [SerializeField] private GameObject fadeOutAnimation;

    
    void FadeIn()
    {
        if(fadeInAnimation != null)
        {
        
        fadeInAnimation.SetActive(true);
        if(fadeOutAnimation != null)
        {
            fadeOutAnimation.SetActive(false);
        }

    }
    }
    void FadeOut()
    {
        if(fadeOutAnimation != null)
        {
            fadeOutAnimation.SetActive(true);
        }
        if(fadeInAnimation != null)
        {
            fadeInAnimation.SetActive(false);
        }   
    }
}
