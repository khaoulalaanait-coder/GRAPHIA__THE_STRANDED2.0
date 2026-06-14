using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Setup")]
    public GameObject pipePrefab;
    public Transform pipesHolder;

    [Header("Empty Sprites - in order 00 to 15")]
    public Sprite[] emptySprites;

    [Header("Filled Sprites - in order 00 to 15")]
    public Sprite[] filledSprites;

    [Header("UI")]
    public TMP_Text messageText;
    public string nextSceneName = "safetowers";

    [Header("Start & End indexes")]
    public int startIndex = 0;
    public int endIndex = 15;

    private GameObject[] pipes;
    private int totalPipes = 0;
    private int correctedPipes = 0;
    private bool completed = false;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        if (messageText)
            messageText.text = "";

        totalPipes = emptySprites.Length;
        correctedPipes = 2;
        CreatePipes();
    }

    private void CreatePipes()
    {
        totalPipes = emptySprites.Length;
        pipes = new GameObject[totalPipes];

        for (int i = 0; i < totalPipes; i++)
        {
            GameObject go = Instantiate(pipePrefab, pipesHolder);
            go.name = "Pipe_" + i.ToString("00");

            Image img = go.GetComponent<Image>();
            if (img)
                img.sprite = emptySprites[i];

            PipeScript ps = go.GetComponent<PipeScript>();
            if (ps)
            {
                ps.emptySprite = emptySprites[i];
                ps.filledSprite = filledSprites[i];
                ps.correctRotation = 0f;

                if (i == 1 || i == 4 || i == 13)
                    ps.correctRotation2 = 180f;

                if (i == 6)
                {
                    ps.correctRotation2 = 90f;
                    ps.correctRotation3 = 180f;
                    ps.correctRotation4 = 270f;
                }

                ps.isStart = (i == startIndex);
                ps.isEnd = (i == endIndex);

                if (i == startIndex && img)
                    img.color = new Color(1f, 0.38f, 0.20f);
                else if (i == endIndex && img)
                    img.color = new Color(1f, 0.85f, 0.10f);
            }

            pipes[i] = go;
        }
    }

    public void CorrectMove()
    {
        if (completed)
            return;

        correctedPipes++;
        Debug.Log("Correct: " + correctedPipes + "/" + totalPipes);

        if (correctedPipes >= totalPipes)
            CompletePuzzle();
    }

    public void WrongMove()
    {
        if (completed)
            return;

        correctedPipes--;
    }

    private void CompletePuzzle()
    {
        completed = true;

        foreach (GameObject pipe in pipes)
        {
            if (pipe == null)
                continue;

            PipeScript ps = pipe.GetComponent<PipeScript>();
            if (ps != null)
                ps.ShowFilled();
        }

        PlayerData.puzzleSolved = true;
        PlayerData.hasFuel = true;
        PlayerData.UnlockJournalPage(2);
        if (JournalManager.instance != null)
            JournalManager.instance.UnlockPage(2);

        if (messageText)
            messageText.text = "The fuel is secured.";

        Invoke(nameof(LoadNext), 3f);
    }

    private void LoadNext()
    {
        PlayerData.puzzleSolved = true;
        PlayerData.hasFuel = true;
        PlayerData.UnlockJournalPage(2);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SceneManager.UnloadSceneAsync("PipePuzzle");
    }
}
