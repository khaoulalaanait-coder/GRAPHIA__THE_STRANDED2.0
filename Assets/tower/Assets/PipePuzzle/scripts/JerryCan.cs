using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class JerryCan : MonoBehaviour
{
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private Color highlightColor = new Color(1f, 0.78f, 0.12f, 1f);
    [SerializeField] private float highlightIntensity = 1.8f;
    [SerializeField] private float highlightPulseSpeed = 4f;

    private Transform player;
    private bool playerNear = false;
    private bool collected = false;
    private Renderer[] renderers;
    private Collider[] colliders;
    private Light highlightLight;
    private MaterialPropertyBlock highlightBlock;

    void Start()
    {
        FindPlayer();
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider>(true);
        highlightBlock = new MaterialPropertyBlock();
        CreateHighlightLight();

        if (promptText)
            promptText.gameObject.SetActive(false);

        SetJerryCanVisible(false);
    }

    void Update()
    {
        if (player == null)
            FindPlayer();

        if (!PlayerData.puzzleSolved || collected)
        {
            SetJerryCanVisible(false);
            if (promptText)
                promptText.gameObject.SetActive(false);
            return;
        }

        SetJerryCanVisible(true);
        UpdateHighlight();

        if (player == null)
            return;

        float distance = Vector3.Distance(transform.position, player.position);
        playerNear = distance <= interactDistance;

        if (promptText)
        {
            promptText.gameObject.SetActive(playerNear);
            promptText.text = PlayerData.hasFuel ? "The fuel is secured" : "Press E to collect fuel";
        }

        if (!PlayerData.hasFuel && playerNear && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            collected = true;
            PlayerData.hasFuel = true;
            if (promptText)
                promptText.text = "The fuel is secured";
            Invoke(nameof(HideJerryCan), 2f);
        }
    }

    private void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            player = playerObject.transform;
    }

    private void HideJerryCan()
    {
        SetJerryCanVisible(false);
        if (promptText)
            promptText.gameObject.SetActive(false);
    }

    private void SetJerryCanVisible(bool visible)
    {
        if (renderers != null)
        {
            foreach (Renderer rend in renderers)
            {
                if (rend != null)
                    rend.enabled = visible;
            }
        }

        if (colliders != null)
        {
            foreach (Collider col in colliders)
            {
                if (col != null)
                    col.enabled = visible;
            }
        }

        if (highlightLight != null)
            highlightLight.enabled = visible;
    }

    private void CreateHighlightLight()
    {
        GameObject lightObject = new GameObject("JerryCanHighlight");
        lightObject.transform.SetParent(transform, false);
        lightObject.transform.localPosition = Vector3.up * 0.8f;

        highlightLight = lightObject.AddComponent<Light>();
        highlightLight.type = LightType.Point;
        highlightLight.color = highlightColor;
        highlightLight.range = 4f;
        highlightLight.intensity = 0f;
        highlightLight.enabled = false;
    }

    private void UpdateHighlight()
    {
        float pulse = 0.65f + Mathf.PingPong(Time.time * highlightPulseSpeed, 0.35f);
        Color emission = highlightColor * (highlightIntensity * pulse);

        if (highlightLight != null)
            highlightLight.intensity = highlightIntensity * pulse;

        if (renderers == null || highlightBlock == null)
            return;

        foreach (Renderer rend in renderers)
        {
            if (rend == null)
                continue;

            rend.GetPropertyBlock(highlightBlock);
            highlightBlock.SetColor("_EmissionColor", emission);
            rend.SetPropertyBlock(highlightBlock);
        }
    }
}
