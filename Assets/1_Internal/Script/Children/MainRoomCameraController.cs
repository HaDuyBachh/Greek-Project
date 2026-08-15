using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

[DisallowMultipleComponent]
public sealed class MainRoomCameraController : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera controlledCamera;
    [SerializeField] private bool ignoreInputOverUi = true;

    [Header("Zoom")]
    [Tooltip("Smallest field of view Main_room can reach when zooming in.")]
    [SerializeField, Min(1f)] private float minimumFov = 30f;
    [Tooltip("Base FOV change produced by the raw mouse-wheel input.")]
    [SerializeField, Min(0.001f)] private float wheelZoomSensitivity = 0.035f;
    [Tooltip("Overall mouse-wheel zoom speed. Increase this value to zoom faster.")]
    [SerializeField, Min(0.01f)] private float wheelZoomSpeedMultiplier = 2.5f;
    [Tooltip("FOV change per pixel of two-finger pinch movement.")]
    [SerializeField, Min(0.001f)] private float pinchZoomSensitivity = 0.03f;
    [Tooltip("Time for Main_room to reach the requested FOV. Set to 0 for immediate zoom.")]
    [SerializeField, Min(0f)] private float zoomSmoothTime = 0.06f;

    [Header("Pan")]
    [Tooltip("World-space pan distance produced by one pixel of pointer drag.")]
    [SerializeField, Min(0.001f)] private float panUnitsPerPixel = 0.008f;
    [Tooltip("Maximum distance Main_room can pan away from its scene-authored position.")]
    [SerializeField, Min(0f)] private float maximumPanDistance = 8f;
    [Tooltip("Time for Main_room to smooth its pan movement. Set to 0 for immediate movement.")]
    [SerializeField, Min(0f)] private float panSmoothTime = 0.15f;

    [Header("Original View Limits")]
    [Tooltip("Keeps zoom-out and pan inside the area visible from Main_room before Play.")]
    [SerializeField] private bool limitToOriginalView = true;
    [Tooltip("World-space Y of the floor used to calculate the original camera view bounds.")]
    [SerializeField] private float navigationPlaneY;

    [Header("Collision")]
    [SerializeField] private Transform wallRoot;
    [SerializeField] private LayerMask collisionMask = 1 << 6;
    [SerializeField, Min(0.01f)] private float collisionRadius = 0.12f;
    [SerializeField, Min(0f)] private float wallPadding = 0.05f;
    [SerializeField] private bool assignWallCollidersToLayer;

    private Vector3 homePosition;
    private Quaternion homeRotation;
    private float homeFov;
    private Vector3 panOffset;
    private Vector3 positionVelocity;
    private float targetFov;
    private float fovVelocity;

    public Camera ControlledCamera => controlledCamera;

    private void Awake()
    {
        PrepareCollisionLayer();
        CaptureSceneAuthoredState();
    }

    private void Update()
    {
        if (controlledCamera == null || !controlledCamera.gameObject.activeInHierarchy)
        {
            return;
        }

        if (ignoreInputOverUi && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        UpdateZoomInput();
        UpdatePanInput();
    }

    private void LateUpdate()
    {
        if (controlledCamera == null)
        {
            return;
        }

        UpdateCameraTransform();
    }

    private void UpdateZoomInput()
    {
        float zoomDelta = 0f;
        if (Mouse.current != null)
        {
            zoomDelta += Mouse.current.scroll.ReadValue().y * wheelZoomSensitivity * wheelZoomSpeedMultiplier;
        }

        if (TryGetPinchDelta(out float pinchDelta))
        {
            zoomDelta += pinchDelta * pinchZoomSensitivity;
        }

        targetFov = Mathf.Clamp(targetFov - zoomDelta, Mathf.Min(minimumFov, homeFov), homeFov);
    }

    private void UpdatePanInput()
    {
        if (targetFov >= homeFov - 0.1f)
        {
            panOffset = Vector3.zero;
            return;
        }

        Vector2 panDelta = GetSinglePointerDragDelta();
        if (panDelta.sqrMagnitude <= 0f)
        {
            return;
        }

        Vector3 planeRight = Vector3.ProjectOnPlane(controlledCamera.transform.right, Vector3.up).normalized;
        Vector3 planeForward = Vector3.ProjectOnPlane(controlledCamera.transform.forward, Vector3.up).normalized;
        float zoomScale = targetFov / Mathf.Max(homeFov, 0.01f);
        panOffset += (-planeRight * panDelta.x - planeForward * panDelta.y) * panUnitsPerPixel * zoomScale;
        panOffset = Vector3.ClampMagnitude(panOffset, maximumPanDistance);
        ClampPanToOriginalView();
    }

    private void UpdateCameraTransform()
    {
        targetFov = Mathf.Clamp(targetFov, Mathf.Min(minimumFov, homeFov), homeFov);
        panOffset = Vector3.ClampMagnitude(panOffset, maximumPanDistance);
        ClampPanToOriginalView();

        if (targetFov >= homeFov - 0.001f)
        {
            targetFov = homeFov;
            panOffset = Vector3.zero;
        }

        Vector3 targetPosition = homePosition + panOffset;
        Vector3 nextPosition = panSmoothTime <= 0f
            ? targetPosition
            : Vector3.SmoothDamp(controlledCamera.transform.position, targetPosition, ref positionVelocity, panSmoothTime);

        if (zoomSmoothTime <= 0f)
        {
            controlledCamera.fieldOfView = targetFov;
            fovVelocity = 0f;
        }
        else
        {
            controlledCamera.fieldOfView = Mathf.SmoothDamp(
                controlledCamera.fieldOfView,
                targetFov,
                ref fovVelocity,
                zoomSmoothTime);
        }

        Vector3 collisionSafePosition = ResolveMovementCollision(controlledCamera.transform.position, nextPosition);
        controlledCamera.transform.position = collisionSafePosition;
        controlledCamera.fieldOfView = Mathf.Clamp(
            controlledCamera.fieldOfView,
            Mathf.Min(minimumFov, homeFov),
            homeFov);

        if (collisionSafePosition != nextPosition)
        {
            panOffset = Vector3.ClampMagnitude(collisionSafePosition - homePosition, maximumPanDistance);
            positionVelocity = Vector3.zero;
        }

        if (targetFov == homeFov &&
            Vector3.SqrMagnitude(controlledCamera.transform.position - homePosition) < 0.000001f &&
            Mathf.Abs(controlledCamera.fieldOfView - homeFov) < 0.001f)
        {
            controlledCamera.transform.position = homePosition;
            controlledCamera.fieldOfView = homeFov;
            positionVelocity = Vector3.zero;
            fovVelocity = 0f;
        }

        controlledCamera.transform.rotation = homeRotation;
    }

    private void ClampPanToOriginalView()
    {
        if (!limitToOriginalView || controlledCamera == null)
        {
            return;
        }

        if (!TryGetViewBoundsOnNavigationPlane(homePosition, homeFov, out Vector4 homeBounds) ||
            !TryGetViewBoundsOnNavigationPlane(homePosition, targetFov, out Vector4 zoomBounds))
        {
            return;
        }

        Vector3 planeRight = Vector3.ProjectOnPlane(homeRotation * Vector3.right, Vector3.up).normalized;
        Vector3 planeForward = Vector3.ProjectOnPlane(homeRotation * Vector3.forward, Vector3.up).normalized;
        if (planeRight.sqrMagnitude < 0.0001f || planeForward.sqrMagnitude < 0.0001f)
        {
            return;
        }

        float rightOffset = Vector3.Dot(panOffset, planeRight);
        float forwardOffset = Vector3.Dot(panOffset, planeForward);
        rightOffset = Mathf.Clamp(rightOffset, homeBounds.x - zoomBounds.x, homeBounds.y - zoomBounds.y);
        forwardOffset = Mathf.Clamp(forwardOffset, homeBounds.z - zoomBounds.z, homeBounds.w - zoomBounds.w);
        panOffset = planeRight * rightOffset + planeForward * forwardOffset;
    }

    private bool TryGetViewBoundsOnNavigationPlane(Vector3 cameraPosition, float fieldOfView, out Vector4 bounds)
    {
        bounds = default;
        float halfHeight = Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad);
        float halfWidth = halfHeight * controlledCamera.aspect;
        Vector3 planeRight = Vector3.ProjectOnPlane(homeRotation * Vector3.right, Vector3.up).normalized;
        Vector3 planeForward = Vector3.ProjectOnPlane(homeRotation * Vector3.forward, Vector3.up).normalized;

        float minRight = float.PositiveInfinity;
        float maxRight = float.NegativeInfinity;
        float minForward = float.PositiveInfinity;
        float maxForward = float.NegativeInfinity;

        for (int y = -1; y <= 1; y += 2)
        {
            for (int x = -1; x <= 1; x += 2)
            {
                Vector3 localDirection = new Vector3(x * halfWidth, y * halfHeight, 1f).normalized;
                Vector3 worldDirection = homeRotation * localDirection;
                if (Mathf.Abs(worldDirection.y) < 0.0001f)
                {
                    return false;
                }

                float distanceToPlane = (navigationPlaneY - cameraPosition.y) / worldDirection.y;
                if (distanceToPlane <= 0f)
                {
                    return false;
                }

                Vector3 point = cameraPosition + worldDirection * distanceToPlane;
                float right = Vector3.Dot(point, planeRight);
                float forward = Vector3.Dot(point, planeForward);
                minRight = Mathf.Min(minRight, right);
                maxRight = Mathf.Max(maxRight, right);
                minForward = Mathf.Min(minForward, forward);
                maxForward = Mathf.Max(maxForward, forward);
            }
        }

        bounds = new Vector4(minRight, maxRight, minForward, maxForward);
        return true;
    }

    private Vector3 ResolveMovementCollision(Vector3 currentPosition, Vector3 desiredPosition)
    {
        Vector3 movement = desiredPosition - currentPosition;
        float distance = movement.magnitude;
        if (distance <= 0.0001f)
        {
            return desiredPosition;
        }

        if (Physics.SphereCast(
                currentPosition,
                collisionRadius,
                movement / distance,
                out RaycastHit hit,
                distance,
                collisionMask,
                QueryTriggerInteraction.Ignore))
        {
            float safeDistance = Mathf.Max(0f, hit.distance - wallPadding);
            return currentPosition + movement.normalized * safeDistance;
        }

        return desiredPosition;
    }

    private static Vector2 GetSinglePointerDragDelta()
    {
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            return Mouse.current.delta.ReadValue();
        }

        if (Touchscreen.current != null && CountPressedTouches() == 1)
        {
            return Touchscreen.current.primaryTouch.delta.ReadValue();
        }

        return Vector2.zero;
    }

    private static bool TryGetPinchDelta(out float pinchDelta)
    {
        pinchDelta = 0f;
        if (Touchscreen.current == null)
        {
            return false;
        }

        TouchControl firstTouch = null;
        TouchControl secondTouch = null;
        foreach (TouchControl touch in Touchscreen.current.touches)
        {
            if (!touch.press.isPressed)
            {
                continue;
            }

            if (firstTouch == null)
            {
                firstTouch = touch;
            }
            else
            {
                secondTouch = touch;
                break;
            }
        }

        if (firstTouch == null || secondTouch == null)
        {
            return false;
        }

        Vector2 firstPosition = firstTouch.position.ReadValue();
        Vector2 secondPosition = secondTouch.position.ReadValue();
        Vector2 firstPrevious = firstPosition - firstTouch.delta.ReadValue();
        Vector2 secondPrevious = secondPosition - secondTouch.delta.ReadValue();
        pinchDelta = Vector2.Distance(firstPosition, secondPosition) - Vector2.Distance(firstPrevious, secondPrevious);
        return true;
    }

    private static int CountPressedTouches()
    {
        int count = 0;
        foreach (TouchControl touch in Touchscreen.current.touches)
        {
            if (touch.press.isPressed)
            {
                count++;
            }
        }

        return count;
    }

    private void CaptureSceneAuthoredState()
    {
        if (controlledCamera == null)
        {
            return;
        }

        homePosition = controlledCamera.transform.position;
        homeRotation = controlledCamera.transform.rotation;
        homeFov = controlledCamera.fieldOfView;
        targetFov = homeFov;
    }

    private void PrepareCollisionLayer()
    {
        if (!assignWallCollidersToLayer || wallRoot == null)
        {
            return;
        }

        int mask = collisionMask.value;
        if (mask == 0 || (mask & (mask - 1)) != 0)
        {
            Debug.LogWarning("Collision Mask must contain exactly one layer when Assign Wall Colliders To Layer is enabled.", this);
            return;
        }

        int wallLayer = 0;
        while ((mask >>= 1) != 0)
        {
            wallLayer++;
        }

        foreach (Collider wallCollider in wallRoot.GetComponentsInChildren<Collider>(true))
        {
            wallCollider.gameObject.layer = wallLayer;
        }
    }
}
