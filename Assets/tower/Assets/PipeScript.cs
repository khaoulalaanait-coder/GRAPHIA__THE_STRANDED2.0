using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PipeScript : MonoBehaviour, IPointerClickHandler
{
    [Header("Correct Rotation")]
    public float correctRotation = 0f;
    public float correctRotation2 = -1f;
    public float correctRotation3 = -1f;
    public float correctRotation4 = -1f;
    [Header("Sprites")]
    public Sprite emptySprite;
    public Sprite filledSprite;

    [Header("Start/End")]
    public bool isStart = false;
    public bool isEnd = false;

    private bool isPlaced = false;
    private Image image;
    private GameManager gameManager;

    void Awake()
    {
        image = GetComponent<Image>();
        gameManager = GameManager.instance;
    }

void Start()
{
    Cursor.lockState = CursorLockMode.None;
    Cursor.visible = true;
    if (isStart || isEnd)
    {
        isPlaced = true;
        return;
    }

    if (image && emptySprite) image.sprite = emptySprite;

    int[] wrongRots = { 90, 180, 270 };
    int rand = wrongRots[Random.Range(0, wrongRots.Length)];
    transform.eulerAngles = new Vector3(0f, 0f, rand);

    // Check if randomly already correct
    float currentZ = NormalizeAngle(transform.eulerAngles.z);
    float targetZ = NormalizeAngle(correctRotation);
    bool correct = Mathf.Abs(currentZ - targetZ) < 1f ||
                   (correctRotation2 >= 0 && Mathf.Abs(currentZ - NormalizeAngle(correctRotation2)) < 1f) ||
                   (correctRotation3 >= 0 && Mathf.Abs(currentZ - NormalizeAngle(correctRotation3)) < 1f) ||
                   (correctRotation4 >= 0 && Mathf.Abs(currentZ - NormalizeAngle(correctRotation4)) < 1f);

    if (correct)
    {
        isPlaced = true;
        gameManager.CorrectMove();
    }
}
    float NormalizeAngle(float angle)
    {
        angle = angle % 360f;
        if (angle < 0) angle += 360f;
        if (angle > 359f) angle = 0f;
        return angle;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isStart || isEnd) return;

        transform.Rotate(0f, 0f, 90f);
        float currentZ = NormalizeAngle(transform.eulerAngles.z);
        float targetZ = NormalizeAngle(correctRotation);
        float targetZ2 = correctRotation2 >= 0 ? NormalizeAngle(correctRotation2) : -1f;

bool correct = Mathf.Abs(currentZ - targetZ) < 1f || 
               (correctRotation2 >= 0 && Mathf.Abs(currentZ - NormalizeAngle(correctRotation2)) < 1f) ||
               (correctRotation3 >= 0 && Mathf.Abs(currentZ - NormalizeAngle(correctRotation3)) < 1f) ||
               (correctRotation4 >= 0 && Mathf.Abs(currentZ - NormalizeAngle(correctRotation4)) < 1f);
        if (correct && !isPlaced)
        {
            isPlaced = true;
            gameManager.CorrectMove();
        }
        else if (!correct && isPlaced)
        {
            isPlaced = false;
            gameManager.WrongMove();
        }

        Debug.Log("Z: " + currentZ + " | Target: " + targetZ + " | Correct: " + correct + " | Count: ");
    }

    public void ShowFilled()
    {
        if (image && filledSprite)
            image.sprite = filledSprite;
    }
}