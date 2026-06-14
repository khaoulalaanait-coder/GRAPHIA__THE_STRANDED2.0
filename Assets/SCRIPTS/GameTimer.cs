using UnityEngine;
using TMPro;

public class GameTimer : MonoBehaviour
{
    public float timeLimit = 180f;
    public TMP_Text timerText;
    private float timeLeft;
    private bool running = true;

    void Start()
    {
        timeLimit = Mathf.Max(timeLimit, 180f);
        timeLeft = timeLimit;
    }

    void Update()
    {
        if (!running) return;
        timeLeft -= Time.deltaTime;
        int seconds = Mathf.CeilToInt(timeLeft);
        timerText.text = seconds.ToString();
        if (timeLeft <= 0)
        {
            running = false;
            UnityEngine.SceneManagement.SceneManager
                .LoadScene(UnityEngine.SceneManagement.SceneManager
                .GetActiveScene().name);
        }
    }

    public void StopTimer() { running = false; }
}
