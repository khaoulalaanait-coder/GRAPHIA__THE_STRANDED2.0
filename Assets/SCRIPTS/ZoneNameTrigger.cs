using UnityEngine;

public class ZoneNameTrigger : MonoBehaviour
{
    [SerializeField] private string zoneName = "Beach";
    [SerializeField] private float detectionRadius = 12f;
    [SerializeField] private bool repeatWhenReEntering = true;

    private Transform player;
    private bool playerInside;

    private void Reset()
    {
        Collider trigger = GetComponent<Collider>();
        if (trigger != null)
            trigger.isTrigger = true;
    }

    private void Start()
    {
        FindPlayer();
    }

    private void Update()
    {
        if (player == null)
            FindPlayer();

        if (player == null)
            return;

        bool insideRadius = Vector3.Distance(transform.position, player.position) <= detectionRadius;

        if (insideRadius && !playerInside)
        {
            playerInside = true;
            NavigationGuideUI.ShowZoneName(zoneName);
        }
        else if (!insideRadius && playerInside && repeatWhenReEntering)
        {
            playerInside = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = true;
        NavigationGuideUI.ShowZoneName(zoneName);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player") || !repeatWhenReEntering)
            return;

        playerInside = false;
    }

    private void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            player = playerObject.transform;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.3f);
        Gizmos.DrawSphere(transform.position, detectionRadius);
    }
}
