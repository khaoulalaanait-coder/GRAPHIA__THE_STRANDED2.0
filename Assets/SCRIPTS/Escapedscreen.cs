using UnityEngine;
using UnityEngine.SceneManagement;

public class Escapedscreen : MonoBehaviour
{
    [SerializeField]
    private string nextSceneName = "Assets/Island_V2.unity";

    void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void LoadLevel2()
    {
        SceneManager.LoadScene(nextSceneName);
    }
}