using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameSessionRestart
{
    public static void RestartCurrentScene()
    {
        AudioListener.pause = false;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        GameplayRunState.ResetState();
        CameraSwitchTrigger.ResetState();

        if (InventoryManager.HasInstance)
        {
            InventoryManager.Instance.ResetState();
        }

        if (ObjectiveSystem.HasInstance)
        {
            ObjectiveSystem.Instance.ResetState();
        }

        Scene currentScene = SceneManager.GetActiveScene();
        if (!string.IsNullOrEmpty(currentScene.name))
        {
            SceneManager.LoadScene(currentScene.name);
            return;
        }

        if (!string.IsNullOrEmpty(currentScene.path))
        {
            SceneManager.LoadScene(currentScene.path);
        }
    }
}
