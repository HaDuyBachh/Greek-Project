using System;
using System.Collections.Generic;
using GreekProject.Content;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

[DisallowMultipleComponent]
public class KidFocusCameraController : MonoBehaviour
{
    [Serializable]
    public class KidFocusTarget
    {
        public string kidId;
        public Transform kidRoot;
        public Transform focusPoint;
        public Outline outline;
        public KidWaypointAnimationTester activityController;

        public bool IsValid => kidRoot != null && focusPoint != null;
    }

    [Header("Cameras")]
    [SerializeField] private Camera overviewCamera;
    [SerializeField] private Camera focusCamera;
    [SerializeField] private ChatUIFollowController chatUiController;

    [Header("Kids")]
    [SerializeField] private List<KidFocusTarget> kids = new List<KidFocusTarget>();

    [Header("Selection")]
    [SerializeField, Min(1f)] private float screenSelectionRadius = 140f;
    [SerializeField] private bool ignoreClicksOverUi = true;

    [Header("Focus Orbit")]
    [SerializeField, Min(0.1f)] private float focusDistance = 1.35f;
    [SerializeField] private float defaultOrbitPitch = 5f;
    [SerializeField] private Vector2 orbitPitchLimits = new Vector2(-15f, 55f);
    [SerializeField, Min(0.01f)] private float orbitSensitivity = 0.18f;
    [SerializeField, Min(0f)] private float positionSmoothTime = 0.12f;
    [SerializeField, Min(0f)] private float rotationSharpness = 18f;

    [Header("Overview Navigation")]
    [SerializeField, Min(1f)] private float minimumOverviewFov = 30f;
    [SerializeField, Min(0.001f)] private float wheelZoomSensitivity = 0.035f;
    [SerializeField, Min(0.001f)] private float pinchZoomSensitivity = 0.03f;
    [SerializeField, Min(0.001f)] private float panUnitsPerPixel = 0.008f;
    [SerializeField, Min(0f)] private float maximumPanDistance = 8f;
    [SerializeField, Min(0f)] private float overviewSmoothTime = 0.15f;

    [Header("Camera Collision")]
    [SerializeField] private Transform wallRoot;
    [SerializeField] private LayerMask cameraCollisionMask = 1 << 6;
    [SerializeField, Min(0.01f)] private float cameraCollisionRadius = 0.12f;
    [SerializeField, Min(0f)] private float cameraWallPadding = 0.05f;
    [SerializeField] private bool assignWallCollidersToLayer;

    [Header("Focus Phone Screen")]
    [SerializeField] private Transform phoneScreen;
    [SerializeField] private bool lockFocusWhilePhoneVisible = true;
    [SerializeField] private bool pauseKidActivityWhilePhoneVisible = true;
    [SerializeField, Min(0f)] private float phoneChatOcclusionPadding = 8f;
    [SerializeField] private float phoneHiddenLocalY = -1f;
    [SerializeField] private float phoneShownLocalY = 0f;
    [SerializeField, Min(0f)] private float phoneSlideSmoothTime = 0.25f;

    private KidFocusTarget selectedKid;
    private KidFocusTarget hoveredKid;
    private Vector3 followVelocity;
    private Vector3 overviewHomePosition;
    private Quaternion overviewHomeRotation;
    private Vector3 overviewPanOffset;
    private Vector3 overviewPositionVelocity;
    private float overviewHomeFov;
    private float targetOverviewFov;
    private float overviewFovVelocity;
    private float orbitYaw;
    private float orbitPitch;
    private float phoneTargetLocalY;
    private float phoneSlideVelocity;
    private bool isPhoneScreenVisible;

    public IReadOnlyList<KidFocusTarget> Kids => kids;
    public string SelectedKidId => selectedKid != null ? selectedKid.kidId : string.Empty;
    public bool IsFocusing => selectedKid != null;
    public bool IsPhoneScreenVisible => isPhoneScreenVisible;

    public void RegisterViewedVideo(VideoContentEffect effect)
    {
        selectedKid?.activityController?.ApplyViewedVideoEffect(effect);
    }

    private void Awake()
    {
        PrepareOutlines();
        PrepareCameraCollision();
        InitializeCameraState();
        InitializePhoneScreen();
    }

    public void ConfigureSceneReferences(
        Camera overview,
        Camera focus,
        Transform phone,
        ChatUIFollowController chatController)
    {
        overviewCamera = overview;
        focusCamera = focus;
        phoneScreen = phone;
        chatUiController = chatController;
    }

    private void Update()
    {
        bool phoneHasFocusLock = lockFocusWhilePhoneVisible && isPhoneScreenVisible;
        if (phoneHasFocusLock)
        {
            SetHoveredKid(null);

            if ((Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
                (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame))
            {
                SetPhoneScreenVisible(false);
                return;
            }

            if (WasPhoneTogglePressed())
            {
                SetPhoneScreenVisible(false);
            }

            return;
        }

        UpdateHoveredKid();

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ShowOverview();
            return;
        }

        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            ShowOverview();
            return;
        }

        if (IsFocusing && WasPhoneTogglePressed())
        {
            SetPhoneScreenVisible(!isPhoneScreenVisible);
            return;
        }

        if (Pointer.current == null || !Pointer.current.press.wasPressedThisFrame)
        {
            UpdateCameraInput();
            return;
        }

        if (ignoreClicksOverUi && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            UpdateCameraInput();
            return;
        }

        TryFocusAtScreenPosition(Pointer.current.position.ReadValue());
        UpdateCameraInput();
    }

    private bool WasPhoneTogglePressed()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }

        return keyboard.spaceKey.wasPressedThisFrame;
    }

    private void LateUpdate()
    {
        UpdateOverviewCamera();
        UpdatePhoneScreen();

        if (selectedKid == null || !selectedKid.IsValid || focusCamera == null)
        {
            return;
        }

        Vector3 focusOrigin = selectedKid.focusPoint.position;
        Vector3 targetPosition = ResolveFocusCameraCollision(
            focusOrigin,
            GetFocusCameraPosition(selectedKid.focusPoint));

        Vector3 nextPosition;
        if (positionSmoothTime <= 0f)
        {
            nextPosition = targetPosition;
        }
        else
        {
            nextPosition = Vector3.SmoothDamp(
                focusCamera.transform.position,
                targetPosition,
                ref followVelocity,
                positionSmoothTime);
        }

        focusCamera.transform.position = ResolveFocusCameraCollision(focusOrigin, nextPosition);

        LookAtSelectedKid(rotationSharpness <= 0f);
    }

    public bool FocusKid(string kidId)
    {
        if (lockFocusWhilePhoneVisible && isPhoneScreenVisible)
        {
            return false;
        }

        foreach (KidFocusTarget kid in kids)
        {
            if (kid != null && kid.IsValid && string.Equals(kid.kidId, kidId, StringComparison.OrdinalIgnoreCase))
            {
                FocusKid(kid);
                return true;
            }
        }

        return false;
    }

    public void ShowOverview()
    {
        if (lockFocusWhilePhoneVisible && isPhoneScreenVisible)
        {
            return;
        }

        selectedKid = null;
        followVelocity = Vector3.zero;
        SetPhoneScreenVisible(false);

        SetCameraActive(overviewCamera, true);
        SetCameraActive(focusCamera, false);

        if (chatUiController != null && overviewCamera != null)
        {
            chatUiController.SetProjectionCamera(overviewCamera);
        }
    }

    private void TryFocusAtScreenPosition(Vector2 screenPosition)
    {
        Camera selectionCamera = IsFocusing ? focusCamera : overviewCamera;
        KidFocusTarget nearestKid = FindKidAtScreenPosition(selectionCamera, screenPosition);

        if (nearestKid != null)
        {
            FocusKid(nearestKid);
        }
    }

    private void UpdateHoveredKid()
    {
        if (Pointer.current == null)
        {
            SetHoveredKid(null);
            return;
        }

        Camera selectionCamera = IsFocusing ? focusCamera : overviewCamera;
        SetHoveredKid(FindKidAtScreenPosition(selectionCamera, Pointer.current.position.ReadValue()));
    }

    private KidFocusTarget FindKidAtScreenPosition(Camera selectionCamera, Vector2 screenPosition)
    {
        if (selectionCamera == null)
        {
            return null;
        }

        KidFocusTarget nearestKid = null;
        float nearestDistance = screenSelectionRadius;

        foreach (KidFocusTarget kid in kids)
        {
            if (kid == null || !kid.IsValid || !kid.kidRoot.gameObject.activeInHierarchy)
            {
                continue;
            }

            Vector3 projectedPoint = selectionCamera.WorldToScreenPoint(kid.focusPoint.position);
            if (projectedPoint.z <= 0f)
            {
                continue;
            }

            float distance = Vector2.Distance(screenPosition, new Vector2(projectedPoint.x, projectedPoint.y));
            if (distance <= nearestDistance)
            {
                nearestDistance = distance;
                nearestKid = kid;
            }
        }

        return nearestKid;
    }

    private void SetHoveredKid(KidFocusTarget kid)
    {
        if (hoveredKid == kid)
        {
            return;
        }

        if (hoveredKid != null && hoveredKid.outline != null)
        {
            hoveredKid.outline.enabled = false;
        }

        hoveredKid = kid;

        if (hoveredKid != null && hoveredKid.outline != null)
        {
            hoveredKid.outline.enabled = true;
        }
    }

    private void FocusKid(KidFocusTarget kid)
    {
        if (lockFocusWhilePhoneVisible && isPhoneScreenVisible)
        {
            return;
        }

        bool isChangingFocus = selectedKid != kid;
        selectedKid = kid;
        followVelocity = Vector3.zero;
        orbitYaw = 0f;
        orbitPitch = Mathf.Clamp(defaultOrbitPitch, orbitPitchLimits.x, orbitPitchLimits.y);

        if (isChangingFocus)
        {
            SetPhoneScreenVisible(false);
        }

        LookAtSelectedKid(true);

        SetCameraActive(overviewCamera, false);
        SetCameraActive(focusCamera, true);

        if (chatUiController != null)
        {
            chatUiController.SetProjectionCamera(focusCamera);
        }
    }

    private Vector3 GetFocusCameraPosition(Transform focusPoint)
    {
        Vector3 baseDirection = Vector3.ProjectOnPlane(focusPoint.forward, Vector3.up).normalized;
        if (baseDirection.sqrMagnitude < 0.0001f)
        {
            baseDirection = Vector3.forward;
        }

        Vector3 horizontalDirection = Quaternion.AngleAxis(orbitYaw, Vector3.up) * baseDirection;
        Vector3 orbitRight = Vector3.Cross(Vector3.up, horizontalDirection).normalized;
        Vector3 orbitDirection = Quaternion.AngleAxis(-orbitPitch, orbitRight) * horizontalDirection;
        return focusPoint.position + orbitDirection.normalized * focusDistance;
    }

    private void LookAtSelectedKid(bool snap)
    {
        Vector3 lookDirection = selectedKid.focusPoint.position - focusCamera.transform.position;
        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
            float blend = snap ? 1f : 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);
            focusCamera.transform.rotation = Quaternion.Slerp(focusCamera.transform.rotation, targetRotation, blend);
        }
    }

    private void UpdateCameraInput()
    {
        bool pointerOverUi = ignoreClicksOverUi && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        if (pointerOverUi)
        {
            return;
        }

        if (IsFocusing)
        {
            Vector2 orbitDelta = GetSinglePointerDragDelta();
            orbitYaw += orbitDelta.x * orbitSensitivity;
            orbitPitch = Mathf.Clamp(
                orbitPitch + orbitDelta.y * orbitSensitivity,
                orbitPitchLimits.x,
                orbitPitchLimits.y);
            return;
        }

        UpdateOverviewZoom();

        if (targetOverviewFov >= overviewHomeFov - 0.1f)
        {
            overviewPanOffset = Vector3.zero;
            return;
        }

        Vector2 panDelta = GetSinglePointerDragDelta();
        if (panDelta.sqrMagnitude <= 0f || overviewCamera == null)
        {
            return;
        }

        Vector3 planeRight = Vector3.ProjectOnPlane(overviewCamera.transform.right, Vector3.up).normalized;
        Vector3 planeForward = Vector3.ProjectOnPlane(overviewCamera.transform.forward, Vector3.up).normalized;
        float zoomScale = targetOverviewFov / Mathf.Max(overviewHomeFov, 0.01f);
        overviewPanOffset += (-planeRight * panDelta.x - planeForward * panDelta.y) * panUnitsPerPixel * zoomScale;
        overviewPanOffset = Vector3.ClampMagnitude(overviewPanOffset, maximumPanDistance);
    }

    private void UpdateOverviewZoom()
    {
        if (overviewCamera == null)
        {
            return;
        }

        float zoomDelta = 0f;
        if (Mouse.current != null)
        {
            zoomDelta += Mouse.current.scroll.ReadValue().y * wheelZoomSensitivity;
        }

        if (TryGetPinchDelta(out float pinchDelta))
        {
            zoomDelta += pinchDelta * pinchZoomSensitivity;
        }

        float minimumFov = Mathf.Min(minimumOverviewFov, overviewHomeFov);
        targetOverviewFov = Mathf.Clamp(
            targetOverviewFov - zoomDelta,
            minimumFov,
            overviewHomeFov);
    }

    private Vector2 GetSinglePointerDragDelta()
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

    private bool TryGetPinchDelta(out float pinchDelta)
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

    private int CountPressedTouches()
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

    private void UpdateOverviewCamera()
    {
        if (overviewCamera == null)
        {
            return;
        }

        targetOverviewFov = Mathf.Clamp(
            targetOverviewFov,
            Mathf.Min(minimumOverviewFov, overviewHomeFov),
            overviewHomeFov);
        overviewPanOffset = Vector3.ClampMagnitude(overviewPanOffset, maximumPanDistance);

        if (targetOverviewFov >= overviewHomeFov - 0.001f)
        {
            targetOverviewFov = overviewHomeFov;
            overviewPanOffset = Vector3.zero;
        }

        Vector3 targetPosition = overviewHomePosition + overviewPanOffset;
        Vector3 nextPosition;
        if (overviewSmoothTime <= 0f)
        {
            nextPosition = targetPosition;
            overviewCamera.fieldOfView = targetOverviewFov;
        }
        else
        {
            nextPosition = Vector3.SmoothDamp(
                overviewCamera.transform.position,
                targetPosition,
                ref overviewPositionVelocity,
                overviewSmoothTime);
            overviewCamera.fieldOfView = Mathf.SmoothDamp(
                overviewCamera.fieldOfView,
                targetOverviewFov,
                ref overviewFovVelocity,
                overviewSmoothTime);
        }

        Vector3 collisionSafePosition = ResolveMovementCollision(overviewCamera.transform.position, nextPosition);
        overviewCamera.transform.position = collisionSafePosition;
        overviewCamera.fieldOfView = Mathf.Clamp(
            overviewCamera.fieldOfView,
            Mathf.Min(minimumOverviewFov, overviewHomeFov),
            overviewHomeFov);

        if (collisionSafePosition != nextPosition)
        {
            overviewPanOffset = Vector3.ClampMagnitude(collisionSafePosition - overviewHomePosition, maximumPanDistance);
            overviewPositionVelocity = Vector3.zero;
        }

        if (targetOverviewFov == overviewHomeFov &&
            Vector3.SqrMagnitude(overviewCamera.transform.position - overviewHomePosition) < 0.000001f &&
            Mathf.Abs(overviewCamera.fieldOfView - overviewHomeFov) < 0.001f)
        {
            overviewCamera.transform.position = overviewHomePosition;
            overviewCamera.fieldOfView = overviewHomeFov;
            overviewPositionVelocity = Vector3.zero;
            overviewFovVelocity = 0f;
        }

        overviewCamera.transform.rotation = overviewHomeRotation;
    }

    private Vector3 ResolveFocusCameraCollision(Vector3 origin, Vector3 desiredPosition)
    {
        Vector3 offset = desiredPosition - origin;
        float distance = offset.magnitude;
        if (distance <= 0.0001f)
        {
            return desiredPosition;
        }

        if (Physics.SphereCast(
                origin,
                cameraCollisionRadius,
                offset / distance,
                out RaycastHit hit,
                distance,
                cameraCollisionMask,
                QueryTriggerInteraction.Ignore))
        {
            float safeDistance = Mathf.Max(0.01f, hit.distance - cameraWallPadding);
            return origin + offset.normalized * safeDistance;
        }

        return desiredPosition;
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
                cameraCollisionRadius,
                movement / distance,
                out RaycastHit hit,
                distance,
                cameraCollisionMask,
                QueryTriggerInteraction.Ignore))
        {
            float safeDistance = Mathf.Max(0f, hit.distance - cameraWallPadding);
            return currentPosition + movement.normalized * safeDistance;
        }

        return desiredPosition;
    }

    private void PrepareOutlines()
    {
        foreach (KidFocusTarget kid in kids)
        {
            if (kid == null || kid.kidRoot == null)
            {
                continue;
            }

            if (kid.outline != null)
            {
                kid.outline.enabled = false;
            }
        }
    }

    private void OnDisable()
    {
        SetHoveredKid(null);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            SetHoveredKid(null);
        }
    }

    private void InitializeCameraState()
    {
        orbitPitch = Mathf.Clamp(defaultOrbitPitch, orbitPitchLimits.x, orbitPitchLimits.y);

        if (overviewCamera == null)
        {
            return;
        }

        overviewHomePosition = overviewCamera.transform.position;
        overviewHomeRotation = overviewCamera.transform.rotation;
        overviewHomeFov = overviewCamera.fieldOfView;
        targetOverviewFov = overviewHomeFov;
    }

    private void PrepareCameraCollision()
    {
        if (!assignWallCollidersToLayer || wallRoot == null)
        {
            return;
        }

        int mask = cameraCollisionMask.value;
        if (mask == 0 || (mask & (mask - 1)) != 0)
        {
            Debug.LogWarning("Camera Collision Mask must contain exactly one layer when Assign Wall Colliders To Layer is enabled.", this);
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

    private void InitializePhoneScreen()
    {
        phoneTargetLocalY = phoneHiddenLocalY;
        isPhoneScreenVisible = false;

        if (phoneScreen != null)
        {
            Canvas phoneCanvas = phoneScreen.GetComponent<Canvas>();
            if (phoneCanvas != null && focusCamera != null)
            {
                phoneCanvas.worldCamera = focusCamera;
            }

            Vector3 localPosition = phoneScreen.localPosition;
            localPosition.y = phoneTargetLocalY;
            phoneScreen.localPosition = localPosition;

            RectTransform phoneOccluder = phoneScreen.Find("ScreenMask") as RectTransform;
            if (phoneOccluder == null)
            {
                phoneOccluder = phoneScreen as RectTransform;
            }
            if (chatUiController != null && phoneOccluder != null)
            {
                foreach (ChatUIFollowController.ChatSlot chat in chatUiController.Chats)
                {
                    ChatUiAnchorFollower follower = chat?.chatRoot != null
                        ? chat.chatRoot.GetComponent<ChatUiAnchorFollower>()
                        : null;
                    follower?.SetScreenOccluder(phoneOccluder, phoneChatOcclusionPadding);
                }
            }
        }

    }

    public void SetPhoneScreenVisible(bool isVisible)
    {
        bool nextVisibleState = isVisible && IsFocusing;
        if (isPhoneScreenVisible != nextVisibleState &&
            pauseKidActivityWhilePhoneVisible &&
            selectedKid != null &&
            selectedKid.activityController != null)
        {
            selectedKid.activityController.SetPausedForPhone(nextVisibleState);
        }

        isPhoneScreenVisible = nextVisibleState;
        phoneTargetLocalY = isPhoneScreenVisible
            ? phoneShownLocalY
            : phoneHiddenLocalY;
        phoneSlideVelocity = 0f;
    }

    private void UpdatePhoneScreen()
    {
        if (phoneScreen == null)
        {
            return;
        }

        Vector3 localPosition = phoneScreen.localPosition;
        if (phoneSlideSmoothTime <= 0f)
        {
            localPosition.y = phoneTargetLocalY;
        }
        else
        {
            localPosition.y = Mathf.SmoothDamp(
                localPosition.y,
                phoneTargetLocalY,
                ref phoneSlideVelocity,
                phoneSlideSmoothTime);

            if (Mathf.Abs(localPosition.y - phoneTargetLocalY) < 0.001f)
            {
                localPosition.y = phoneTargetLocalY;
                phoneSlideVelocity = 0f;
            }
        }

        phoneScreen.localPosition = localPosition;
    }

    private static void SetCameraActive(Camera cameraToSet, bool isActive)
    {
        if (cameraToSet != null && cameraToSet.gameObject.activeSelf != isActive)
        {
            cameraToSet.gameObject.SetActive(isActive);
        }
    }
}
