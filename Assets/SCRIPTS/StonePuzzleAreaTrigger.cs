using UnityEngine;

public class StonePuzzleAreaTrigger : MonoBehaviour
{
    [SerializeField] private PuzzleTimer puzzleTimer;
    [SerializeField] private bool triggerOnce = true;

    private bool triggered;

    private void Reset()
    {
        Collider trigger = GetComponent<Collider>();
        if (trigger != null)
            trigger.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggerOnce && triggered)
            return;

        if (!other.CompareTag("Player"))
            return;

        if (puzzleTimer == null)
            puzzleTimer = FindFirstObjectByType<PuzzleTimer>();

        if (puzzleTimer == null)
            return;

        triggered = true;
        puzzleTimer.OnPlayerReachedStonePuzzle();
    }
}
