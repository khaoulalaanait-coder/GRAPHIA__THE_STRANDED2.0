using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class FuelTrigger : MonoBehaviour
{
    [SerializeField] private string puzzleSceneName = "PipePuzzle";
    [SerializeField] private string puzzleScenePath = "Assets/tower/Assets/Scenes/PipePuzzle.unity";
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private float interactDistance = 3f;

    private static bool puzzleLoadInProgress = false;
    private Transform player;
    private bool playerNear = false;
    private bool puzzleLoaded = false;

    void Start()
    {
        FindPlayer();

        if (promptText)
            promptText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (player == null)
            FindPlayer();

        if (player == null)
            return;

        float distance = Vector3.Distance(transform.position, player.position);
        playerNear = distance <= interactDistance;

        if (promptText)
            promptText.gameObject.SetActive(playerNear && !puzzleLoaded);

        if (playerNear && !puzzleLoaded && IsInteractPressed())
            OpenPuzzle();
    }

    public void OnPuzzleSolved()
    {
        puzzleLoaded = false;
        SceneManager.UnloadSceneAsync(puzzleSceneName);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            player = playerObject.transform;
    }

    private bool IsInteractPressed()
    {
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            return true;

        try
        {
            return Input.GetKeyDown(KeyCode.E);
        }
        catch (System.InvalidOperationException)
        {
            return false;
        }
    }

    private void OpenPuzzle()
    {
        if (puzzleLoadInProgress || SceneManager.GetSceneByName(puzzleSceneName).isLoaded)
            return;

        Debug.Log("Opening fuel puzzle: " + puzzleSceneName);
        puzzleLoadInProgress = true;
        puzzleLoaded = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(puzzleScenePath, LoadSceneMode.Additive);
        if (loadOperation == null)
        {
            Debug.LogError("Could not load puzzle scene. Check Build Settings entry: " + puzzleScenePath);
            puzzleLoadInProgress = false;
            puzzleLoaded = false;
            return;
        }

        loadOperation.completed += _ => puzzleLoadInProgress = false;
    }
}
