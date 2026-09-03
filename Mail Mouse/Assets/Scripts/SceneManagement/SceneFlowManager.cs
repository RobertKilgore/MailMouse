using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central scene flow manager for start, gameplay, and end scenes.
/// Use this to keep scene transitions configurable and easy to change later.
/// </summary>
public class SceneFlowManager : MonoBehaviour
{
    public static SceneFlowManager Instance { get; private set; }

    [Header("Scene Configuration")]
    [Tooltip("Scene name used for the start menu.")]
    public string startSceneName = "Start Menu";

    [Tooltip("Scene name used for gameplay.")]
    public string gameplaySceneName = "Map Design";

    [Tooltip("Scene name used for the end game screen.")]
    public string endSceneName = "End Menu";

    [Tooltip("Scene shown when the saved access code is missing or invalid.")]
    public string accessCodeSceneName = "Access Code";

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

        AccessControlManager.GetOrCreateInstance().ValidateAtBoot(HandleBootAccessCheck);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    public void LoadStartScene()
    {
        LoadScene(startSceneName);
    }

    public void LoadGameplayScene()
    {
        LoadScene(gameplaySceneName);
    }

    private void HandleBootAccessCheck(bool accessGranted)
    {
        if (!accessGranted && SceneManager.GetActiveScene().name != accessCodeSceneName)
            LoadScene(accessCodeSceneName);
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
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyCursorForScene(scene.name);
    }

    private void ApplyCursorForScene(string sceneName)
    {
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
        else
        {
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
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
