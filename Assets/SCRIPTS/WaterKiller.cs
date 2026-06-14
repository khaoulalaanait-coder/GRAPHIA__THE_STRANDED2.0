using UnityEngine;
using UnityEngine.SceneManagement;
using StarterAssets;

public class WaterDeath : MonoBehaviour
{
    [SerializeField] private float floatDuration = 3f;
    [SerializeField] private float drowningStartHeight = 28f;

    private Transform player;
    private ThirdPersonController playerController;
    private CharacterController characterController;
    private bool isDrowning = false;
    private float drowningTimer = 0f;

    void Start()
    {
        GameObject p = GameObject.FindWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerController = p.GetComponent<ThirdPersonController>();
            characterController = p.GetComponent<CharacterController>();
        }
    }

    void Update()
    {
        if (!isDrowning || player == null) return;

        // Float on water surface
        Vector3 pos = player.position;
        pos.y = 34f;
        player.position = pos;

        drowningTimer += Time.deltaTime;
        if (drowningTimer >= floatDuration)
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void OnTriggerEnter(Collider other)
    {
        TryStartDrowning(other);
    }

    void OnTriggerStay(Collider other)
    {
        TryStartDrowning(other);
    }

    private void TryStartDrowning(Collider other)
    {
        if (!other.CompareTag("Player") || isDrowning) return;

        if (other.transform.position.y > drowningStartHeight)
            return;

        isDrowning = true;
        drowningTimer = 0f;

        // Disable controller so we can move player freely
        playerController = other.GetComponent<ThirdPersonController>();
        if (playerController != null)
            playerController.enabled = false;

        characterController = other.GetComponent<CharacterController>();
        if (characterController != null)
            characterController.enabled = false;

        Debug.Log("Drowning started");
    }
}

