using UnityEngine;

public class PuzzleTimer : MonoBehaviour
{
    public static PuzzleTimer instance;

    [Header("Timer")]
    public float timeLimit = 120f;
    public bool pauseWhenSolved = true;
    public bool restartAfterTimeout = false;
    public bool autoCreateIslandMusic = true;

    [Header("Puzzle")]
    public CrystalConnectionManager connectionManager;
    public bool clearColorsOnTimeout = true;
    public bool randomizeOnTimeout = false;

    [Header("Screen Display")]
    public bool drawOnScreen = true;
    public Vector2 screenPosition = new Vector2(24f, 24f);
    public Vector2 screenSize = new Vector2(220f, 44f);
    public int fontSize = 22;
    public Color normalColor = Color.white;
    public Color warningColor = new Color(1f, 0.25f, 0.15f);
    public float warningTime = 20f;

    [Header("Game Over")]
    public bool showGameOverScreen = true;
    public string gameOverTitle = "TIME IS OVER";
    public string restartButtonText = "Start Again";
    public Color overlayColor = new Color(0f, 0f, 0f, 0.68f);
    public Color titleColor = new Color(1f, 0.25f, 0.18f);
    public Color buttonColor = new Color(1f, 0.86f, 0.36f);

    [Header("Mistake Penalty")]
    public float mistakePenaltySeconds = 5f;
    public float penaltyFlashDuration = 1.1f;
    public Color penaltyFlashColor = new Color(1f, 0.05f, 0.02f);

    private float remainingTime;
    private bool running;
    private bool solved;
    private bool gameOver;
    private bool timerVisible;
    private float penaltyFlashTimer;
    private string penaltyFlashText = "";
    private CursorLockMode previousCursorLockState;
    private bool previousCursorVisible;
    private Texture2D buttonNormalTexture;
    private Texture2D buttonHoverTexture;
    private Texture2D buttonActiveTexture;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void Start()
    {
        if (connectionManager == null)
            connectionManager = FindFirstObjectByType<CrystalConnectionManager>();

        if (autoCreateIslandMusic && FindFirstObjectByType<IslandBackgroundMusic>() == null)
            gameObject.AddComponent<IslandBackgroundMusic>();

        ResetTimer();
        timerVisible = false;
        // running stays false — timer only starts when OnTowerComplete() is called
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void Update()
    {
        if (penaltyFlashTimer > 0f)
            penaltyFlashTimer -= Time.deltaTime;

        if (connectionManager != null && connectionManager.IsSolved())
        {
            solved = true;

            if (pauseWhenSolved)
                running = false;
        }

        if (!running || solved || gameOver)
            return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
            HandleTimeout();
    }

    [ContextMenu("Reset Timer")]
    public void ResetTimer()
    {
        remainingTime = Mathf.Max(0f, timeLimit);
        solved = false;
    }

    public void OnTowerComplete()
    {
        StartTimer();
    }

    public void OnPlayerReachedStonePuzzle()
    {
        timerVisible = true;
        StartTimer();
    }

    // Reveals the storm timer HUD without starting the countdown.
    public void OnEngineIsLastPiece()
    {
        timerVisible = true;
    }

    [ContextMenu("Start Timer")]
    public void StartTimer()
    {
        running = true;
        gameOver = false;
    }

    [ContextMenu("Stop Timer")]
    public void StopTimer()
    {
        running = false;
    }

    public void ApplyMistakePenalty()
    {
        ApplyMistakePenalty(mistakePenaltySeconds);
    }

    public void ApplyMistakePenalty(float seconds)
    {
        if (solved)
            return;

        float penalty = Mathf.Max(0f, seconds);
        remainingTime = Mathf.Max(0f, remainingTime - penalty);
        penaltyFlashText = "-" + Mathf.CeilToInt(penalty) + "s";
        penaltyFlashTimer = penaltyFlashDuration;

        if (remainingTime <= 0f)
            HandleTimeout();
    }

    private void HandleTimeout()
    {
        remainingTime = 0f;
        running = false;
        gameOver = true;
        penaltyFlashTimer = 0f;

        if (connectionManager != null)
        {
            if (clearColorsOnTimeout)
                connectionManager.ClearAllStoneColors();

            if (randomizeOnTimeout)
                connectionManager.RandomizeConnections();
        }

        if (showGameOverScreen)
        {
            UnlockCursorForGameOver();
            return;
        }

        ResetTimer();
        running = restartAfterTimeout;
    }

    private void OnGUI()
    {
        if (!drawOnScreen || !timerVisible)
            return;

        if (gameOver && showGameOverScreen)
        {
            DrawGameOverScreen();
            return;
        }

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = fontSize,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };

        style.normal.textColor = solved ? new Color(0.2f, 1f, 0.4f) : remainingTime <= warningTime ? warningColor : normalColor;

        string label = solved ? "GRID STABLE" : "STORM: " + FormatTime(remainingTime);
        GUI.Label(new Rect(screenPosition, screenSize), label, style);

        if (penaltyFlashTimer > 0f && !string.IsNullOrEmpty(penaltyFlashText))
        {
            GUIStyle penaltyStyle = new GUIStyle(style)
            {
                fontSize = fontSize + 6
            };
            penaltyStyle.normal.textColor = penaltyFlashColor;

            float x = screenPosition.x + screenSize.x + 8f;
            GUI.Label(new Rect(x, screenPosition.y, 90f, screenSize.y), penaltyFlashText, penaltyStyle);
        }
    }

    private void DrawGameOverScreen()
    {
        GUI.color = overlayColor;
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float panelWidth = Mathf.Min(420f, Screen.width - 48f);
        float panelHeight = 190f;
        float panelX = (Screen.width - panelWidth) * 0.5f;
        float panelY = (Screen.height - panelHeight) * 0.5f;

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 34,
            fontStyle = FontStyle.Bold
        };
        titleStyle.normal.textColor = titleColor;

        GUIStyle subtitleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 18,
            fontStyle = FontStyle.Bold
        };
        subtitleStyle.normal.textColor = Color.white;

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 22,
            fontStyle = FontStyle.Bold
        };
        buttonStyle.normal.textColor = Color.black;
        buttonStyle.hover.textColor = Color.black;
        buttonStyle.active.textColor = Color.black;
        buttonStyle.normal.background = GetTexture(ref buttonNormalTexture, buttonColor);
        buttonStyle.hover.background = GetTexture(ref buttonHoverTexture, new Color(1f, 0.95f, 0.5f));
        buttonStyle.active.background = GetTexture(ref buttonActiveTexture, new Color(0.92f, 0.68f, 0.18f));

        GUI.Label(new Rect(panelX, panelY, panelWidth, 56f), gameOverTitle, titleStyle);
        GUI.Label(new Rect(panelX, panelY + 58f, panelWidth, 38f), "The island grid collapsed.", subtitleStyle);

        if (GUI.Button(new Rect(panelX + 90f, panelY + 118f, panelWidth - 180f, 52f), restartButtonText, buttonStyle))
            RestartPuzzle();
    }

    private void RestartPuzzle()
    {
        Time.timeScale = 1f;
        gameOver = false;
        solved = false;
        RestoreCursorAfterGameOver();

        if (connectionManager != null)
        {
            connectionManager.ClearAllStoneColors();
            connectionManager.RandomizeConnections();
        }

        ResetTimer();
        running = true;
    }

    private void UnlockCursorForGameOver()
    {
        previousCursorLockState = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
    }

    private void RestoreCursorAfterGameOver()
    {
        Cursor.lockState = previousCursorLockState;
        Cursor.visible = previousCursorVisible;
    }

    private Texture2D GetTexture(ref Texture2D texture, Color color)
    {
        if (texture != null)
            return texture;

        texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    private string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, seconds));
        int minutes = totalSeconds / 60;
        int secondsPart = totalSeconds % 60;
        return minutes.ToString("00") + ":" + secondsPart.ToString("00");
    }
}
