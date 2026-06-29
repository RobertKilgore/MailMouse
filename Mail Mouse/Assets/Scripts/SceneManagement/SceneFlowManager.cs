using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central scene flow manager for start, gameplay, and end scenes.
/// Use this to keep scene transitions configurable and easy to change later.
/// </summary>
public class SceneFlowManager : MonoBehaviour
{
    public static SceneFlowManager Instance { get; private set; }

    [Header("Scene Names")]
    [Tooltip("Scene name used for the start menu.")]
    public string startSceneName = "Start Menu";

    [Tooltip("Scene name used for gameplay.")]
    public string gameplaySceneName = "Gameplay";

    [Tooltip("Scene name used for the end game screen.")]
    public string endSceneName = "End Menu";

    public static SceneFlowManager GetOrCreateInstance()
    {
        if (Instance != null)
            return Instance;

        Instance = FindFirstObjectByType<SceneFlowManager>(FindObjectsInactive.Include);
        if (Instance != null)
            return Instance;

        GameObject managerObject = new GameObject("SceneFlowManager");
        Instance = managerObject.AddComponent<SceneFlowManager>();
        DontDestroyOnLoad(managerObject);
        return Instance;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    public void LoadStartScene()
    {
        LoadScene(startSceneName);
    }

    public void LoadGameplayScene()
    {
        LoadScene(gameplaySceneName);
    }

    public void LoadEndScene()
    {
        LoadScene(endSceneName);
    }

    public void RestartGameplayScene()
    {
        LoadScene(gameplaySceneName);
    }

    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("SceneFlowManager: scene name is empty.");
            return;
        }

        Debug.Log($"SceneFlowManager: loading scene '{sceneName}'...");
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);

        // Ensure cursor state is appropriate after loading menu or gameplay scenes.
        // For menu scenes we'll make the cursor visible and confined so it doesn't leave the editor/game window.
        if (sceneName == startSceneName || sceneName == endSceneName)
        {
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
        }
        else if (sceneName == gameplaySceneName)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        Debug.Log("SceneFlowManager: quitting play mode.");
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Debug.Log("SceneFlowManager: quitting application.");
        Application.Quit();
#endif
    }
}
