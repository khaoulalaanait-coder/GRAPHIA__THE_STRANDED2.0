using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;

public class TowerTrigger : MonoBehaviour
{
    [SerializeField] private string puzzleSceneName = "PipePuzzle";
    [SerializeField] private GameObject proximityCanvas;
    [SerializeField] private float promptDistance = 6f;

    private static bool puzzleLoadInProgress = false;
    private Transform player;
    private bool isNearTower = false;
    private bool puzzleLoaded = false;

    private void Start()
    {
        FindPlayer();
        SetPromptText();

        if (proximityCanvas != null)
            proximityCanvas.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isNearTower = true;
            if (proximityCanvas != null)
                proximityCanvas.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isNearTower = false;
            if (proximityCanvas != null)
                proximityCanvas.SetActive(false);
        }
    }

    private void Update()
    {
        UpdateNearTowerByDistance();

        if (isNearTower && Keyboard.current.eKey.wasPressedThisFrame && !puzzleLoaded)
        {
            if (PlayerData.puzzleSolved || PlayerData.hasFuel)
                return;

            if (puzzleLoadInProgress || SceneManager.GetSceneByName(puzzleSceneName).isLoaded)
                return;

            Debug.Log("Near tower, E pressed: " + Keyboard.current.eKey.wasPressedThisFrame);
            puzzleLoadInProgress = true;
            puzzleLoaded = true;
            if (proximityCanvas != null)
                proximityCanvas.SetActive(false);

            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(puzzleSceneName, LoadSceneMode.Additive);
            if (loadOperation == null)
            {
                puzzleLoadInProgress = false;
                puzzleLoaded = false;
                Debug.LogError("Could not load puzzle scene: " + puzzleSceneName);
                return;
            }

            loadOperation.completed += _ => puzzleLoadInProgress = false;
        }
    }

    private void UpdateNearTowerByDistance()
    {
        if (player == null)
            FindPlayer();

        if (player == null)
            return;

        bool nearByDistance = Vector3.Distance(transform.position, player.position) <= promptDistance;
        isNearTower = isNearTower || nearByDistance;

        if (proximityCanvas != null)
            proximityCanvas.SetActive(isNearTower && !puzzleLoaded && !PlayerData.puzzleSolved && !PlayerData.hasFuel);

        if (!nearByDistance)
            isNearTower = false;
    }

    private void SetPromptText()
    {
        if (proximityCanvas == null)
            return;

        TMP_Text tmpText = proximityCanvas.GetComponentInChildren<TMP_Text>(true);
        if (tmpText != null)
            tmpText.text = "[E] to collect Fuel";

        Text legacyText = proximityCanvas.GetComponentInChildren<Text>(true);
        if (legacyText != null)
            legacyText.text = "[E] to collect Fuel";
    }

    private void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            player = playerObject.transform;
    }
}
