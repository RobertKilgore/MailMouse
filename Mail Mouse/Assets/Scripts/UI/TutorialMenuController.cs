using UnityEngine;

public class TutorialMenuHandler : MonoBehaviour
{
    public static TutorialMenuHandler Instance { get; private set; }

    [Header("Tutorial UI")]
    [SerializeField] private MenuController tutorialMenu;

    public bool IsOpen => tutorialMenu != null && tutorialMenu.IsOpen;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
        {
            Debug.LogWarning("TutorialMenuHandler: duplicate handler found; disabling the duplicate component.", this);
            enabled = false;
        }
    }

    private void Start()
    {
        OpenTutorial();
    }

    public void OpenTutorial()
    {
        if (tutorialMenu == null)
        {
            Debug.LogError("TutorialMenuHandler: Tutorial Menu is not assigned.", this);
            return;
        }

        if (!tutorialMenu.Open())
            Debug.LogWarning($"TutorialMenuHandler: could not open tutorial menu '{tutorialMenu.gameObject.name}'. Check its MenuController priority and MenuManager state.", tutorialMenu);
    }

    public void CloseTutorial()
    {
        if (tutorialMenu == null || !tutorialMenu.IsOpen)
            return;

        tutorialMenu.Close();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
