using System.Collections.Generic;
using StarterAssets;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float distance = 4.8f;
    [SerializeField] private float heightOffset = 1.6f;
    [SerializeField] private float shoulderOffset = 0.55f;
    [SerializeField] private float sensitivityX = 1.3f;
    [SerializeField] private float sensitivityY = 0.8f;
    [SerializeField] private float rotationSmoothTime = 0.04f;
    [SerializeField] private float positionSmoothTime = 0.06f;
    [SerializeField] private float pitchMin = -25f;
    [SerializeField] private float pitchMax = 55f;
    [SerializeField] private float collisionRadius = 0.25f;
    [SerializeField] private float collisionPadding = 0.15f;
    [SerializeField] private LayerMask collisionLayers = ~0;
    [SerializeField] private bool autoRecenter = true;
    [SerializeField] private float recenterDelay = 1.5f;
    [SerializeField] private float recenterSpeed = 2.5f;

    private float targetYaw;
    private float targetPitch = 10f;
    private float currentYaw;
    private float currentPitch = 10f;
    private float yawVelocity;
    private float pitchVelocity;
    private float timeSinceManualLook;
    private Vector3 positionVelocity;
    private StarterAssetsInputs input;

    private void Start()
    {
        GameplayCursorState.Apply();

        if (target == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                target = playerObject.transform;
        }

        if (target == null)
            return;

        input = target.GetComponent<StarterAssetsInputs>();
        targetYaw = target.eulerAngles.y;
        currentYaw = targetYaw;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        if (!GameplayCursorState.IsUiCursorRequested)
            ReadLookInput();

        SmoothRotation();
        MoveCamera();
    }

    private void ReadLookInput()
    {
        if (input == null)
            return;

        Vector2 look = input.look;
        bool hasManualLook = look.sqrMagnitude > 0.001f;

        if (hasManualLook)
        {
            targetYaw += look.x * sensitivityX;
            targetPitch -= look.y * sensitivityY;
            timeSinceManualLook = 0f;
        }
        else
        {
            timeSinceManualLook += Time.deltaTime;
        }

        if (autoRecenter && input.move.sqrMagnitude > 0.05f && timeSinceManualLook >= recenterDelay)
            targetYaw = Mathf.LerpAngle(targetYaw, target.eulerAngles.y, recenterSpeed * Time.deltaTime);

        targetPitch = Mathf.Clamp(targetPitch, pitchMin, pitchMax);
    }

    private void SmoothRotation()
    {
        currentYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref yawVelocity, rotationSmoothTime);
        currentPitch = Mathf.SmoothDamp(currentPitch, targetPitch, ref pitchVelocity, rotationSmoothTime);
    }

    private void MoveCamera()
    {
        Vector3 pivot = target.position + Vector3.up * heightOffset;
        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
        Vector3 shoulder = rotation * Vector3.right * shoulderOffset;
        Vector3 focusPoint = pivot + shoulder;
        Vector3 desiredPosition = focusPoint - rotation * Vector3.forward * distance;
        Vector3 resolvedPosition = ResolveCollision(focusPoint, desiredPosition);

        transform.position = Vector3.SmoothDamp(transform.position, resolvedPosition, ref positionVelocity, positionSmoothTime);
        transform.rotation = Quaternion.LookRotation(focusPoint - transform.position, Vector3.up);
    }

    private Vector3 ResolveCollision(Vector3 focusPoint, Vector3 desiredPosition)
    {
        Vector3 direction = desiredPosition - focusPoint;
        float desiredDistance = direction.magnitude;

        if (desiredDistance <= 0.001f)
            return desiredPosition;

        direction /= desiredDistance;

        if (Physics.SphereCast(
                focusPoint,
                collisionRadius,
                direction,
                out RaycastHit hit,
                desiredDistance,
                collisionLayers,
                QueryTriggerInteraction.Ignore))
        {
            float safeDistance = Mathf.Max(0.1f, hit.distance - collisionPadding);
            return focusPoint + direction * safeDistance;
        }

        return desiredPosition;
    }
}

public static class GameplayCursorState
{
    private static readonly HashSet<string> unlockReasons = new HashSet<string>();

    public static bool IsUiCursorRequested
    {
        get { return unlockReasons.Count > 0; }
    }

    public static void RequestUnlockedCursor(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return;

        unlockReasons.Add(reason);
        Apply();
    }

    public static void ReleaseUnlockedCursor(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return;

        unlockReasons.Remove(reason);
        Apply();
    }

    public static void Apply()
    {
        if (IsUiCursorRequested)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
