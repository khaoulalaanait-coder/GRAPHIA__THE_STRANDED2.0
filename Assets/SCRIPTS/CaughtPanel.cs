using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class CaughtPanel : MonoBehaviour
{
    public GameObject caughtPanel;
    
    public string retryScenePath = "Assets/Scenes/level1_maze.unity";

    public void ShowCaught()
    {
        if (caughtPanel != null)
            caughtPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
    }

    public void TryAgain()
    {
        Time.timeScale = 1f;

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        SceneManager.LoadScene(retryScenePath);
    }
}
