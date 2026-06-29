using UnityEngine;

/// <summary>
/// Simple controller for an end menu scene.
/// </summary>
public class EndMenuController : MonoBehaviour
{
    private SceneFlowManager sceneFlowManager;

    public void OnQuitButtonPressed()
    {
        if (sceneFlowManager == null)
            sceneFlowManager = SceneFlowManager.GetOrCreateInstance();

        if (sceneFlowManager != null)
            sceneFlowManager.QuitGame();
    }
    public void OnMainMenuButtonPressed()
    {
        if (sceneFlowManager == null)
            sceneFlowManager = SceneFlowManager.GetOrCreateInstance();

        if (sceneFlowManager != null)
            sceneFlowManager.LoadStartScene();
    }
}
