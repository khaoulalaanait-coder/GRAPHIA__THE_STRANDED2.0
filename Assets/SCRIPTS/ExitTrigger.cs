using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitTrigger : MonoBehaviour
{
    public string escapedScenePath = "Assets/Scenes/youescaped.unity";
    public float fallbackEscapeDistance = 1.2f;

    private bool escaped = false;
    private Transform player;

    void Awake()
    {
        if (escapedScenePath == "Assets/youescaped.unity")
            escapedScenePath = "Assets/Scenes/youescaped.unity";

        Collider trigger = GetComponent<Collider>();
        if (trigger != null)
            trigger.isTrigger = true;
    }

    void Start()
    {
        ResolvePlayer();
    }

    void ResolvePlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            player = playerObject.transform;
    }

    void Update()
    {
        if (escaped)
            return;

        if (player == null)
            ResolvePlayer();
        if (player == null)
            return;

        Collider trigger = GetComponent<Collider>();
        if (trigger == null)
            return;

        Bounds bounds = trigger.bounds;
        bool touchingExitZone = bounds.Contains(player.position);
        bool reachedExitWall = player.position.x >= bounds.center.x - fallbackEscapeDistance;
        bool withinExitWidth = player.position.z >= bounds.min.z && player.position.z <= bounds.max.z;
        bool fellJustOutsideExit = player.position.y < -1f && reachedExitWall;

        if (touchingExitZone || (reachedExitWall && withinExitWidth) || (fellJustOutsideExit && withinExitWidth))
            Escape();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.transform.root.CompareTag("Player"))
            Escape();
    }

    void Escape()
    {
        if (escaped)
            return;

        escaped = true;
        Time.timeScale = 1f;

        GameTimer timer = FindFirstObjectByType<GameTimer>();
        if (timer != null)
            timer.StopTimer();

        SceneManager.LoadScene(escapedScenePath);
    }
}
