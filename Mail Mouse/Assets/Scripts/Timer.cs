using UnityEngine;
using TMPro;

public class Timer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;
    private SceneFlowManager sceneFlowManager;
    [SerializeField] private float timeRemaining = 300f;
    private bool isTimerActive = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
   private void Start()
    {
        isTimerActive = true;

    }

    // Update is called once per frame
    private void Update()
    {
        if (isTimerActive)
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
                DisplayTime(timeRemaining);
            }
            else
            {
                Debug.Log("OUT OF TIME!");
                OnEndGame();
                timeRemaining = 0;
                isTimerActive = false;
            }

        }
    }

    public void OnEndGame()
    {
        if (sceneFlowManager == null)
            sceneFlowManager = SceneFlowManager.GetOrCreateInstance();

        if (sceneFlowManager != null)
            sceneFlowManager.LoadEndScene();
    }
    private void DisplayTime(float timeToDisplay)
    {
        // Calculate minutes and seconds
        float minutes = Mathf.FloorToInt(timeToDisplay / 60);
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);

        // Formats the string to display always two digits (e.g., 05:09)
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}
