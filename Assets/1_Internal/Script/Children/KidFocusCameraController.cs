using System;
using System.Collections.Generic;
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

        public bool IsValid => kidRoot != null && focusPoint != null;
    }

    [Header("Cameras")]
    [SerializeField] private Camera overviewCamera;
    [SerializeField] private Camera focusCamera;
    [SerializeField] private ChatUIFollowController chatUiController;

    [Header("Kids")]
    [SerializeField] private List<KidFocusTarget> kids = new List<KidFocusTarget>();
    [SerializeField] private bool autoDiscoverKids = true;
    [SerializeField] private string focusPointTag = "focus";

    [Header("Selection")]
    [SerializeField, Min(1f)] private float screenSelectionRadius = 140f;
    [SerializeField] private bool ignoreClicksOverUi = true;

    [Header("Hover Outline")]
    [SerializeField] private bool addMissingOutline = true;
    [SerializeField] private Outline.Mode defaultOutlineMode = Outline.Mode.OutlineAll;
    [SerializeField] private Color defaultOutlineColor = new Color(0.84f, 1f, 0.02f, 1f);
    [SerializeField, Range(0f, 10f)] private float defaultOutlineWidth = 2f;

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
    [SerializeField] private string wallLayerName = "CameraCollision";
    [SerializeField] private LayerMask cameraCollisionMask = 1 << 6;
    [SerializeField, Min(0.01f)] private float cameraCollisionRadius = 0.12f;
    [SerializeField, Min(0f)] private float cameraWallPadding = 0.05f;
    [SerializeField] private bool assignWallCollidersToLayer = true;

    [Header("Focus Phone Screen")]
    [SerializeField] private Transform phoneScreen;
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

    private void Awake()
    {
        ResolveReferences();

        if (autoDiscoverKids)
        {
            DiscoverKids();
        }

        ResolveFocusPoints();
        PrepareOutlines();
        PrepareCameraCollision();
        InitializeCameraState();
        InitializePhoneScreen();
        ShowOverview();
    }

    private void Update()
    {
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

        if (IsFocusing && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            SetPhoneScreenVisible(!isPhoneScreenVisible);
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

    private void ResolveReferences()
    {
        if (overviewCamera == null)
        {
            overviewCamera = ChatUiAnchorUtility.FindCameraByName("Main_room");
        }

        if (focusCamera == null)
        {
            focusCamera = ChatUiAnchorUtility.FindCameraByName("Kid_Forcus");
        }

        if (chatUiController == null)
        {
            chatUiController = FindFirstObjectByType<ChatUIFollowController>();
        }
    }

    private void DiscoverKids()
    {
        if (chatUiController == null)
        {
            return;
        }

        foreach (ChatUIFollowController.KidChatBinding chatKid in chatUiController.Kids)
        {
            if (chatKid == null || chatKid.kidRoot == null || ContainsKid(chatKid.kidRoot))
            {
                continue;
            }

            kids.Add(new KidFocusTarget
            {
                kidId = chatKid.kidId,
                kidRoot = chatKid.kidRoot,
                focusPoint = FindFocusPoint(chatKid.kidRoot),
                outline = chatKid.kidRoot.GetComponent<Outline>()
            });
        }
    }

    private void ResolveFocusPoints()
    {
        foreach (KidFocusTarget kid in kids)
        {
            if (kid == null || kid.kidRoot == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(kid.kidId))
            {
                kid.kidId = kid.kidRoot.name;
            }

            if (kid.focusPoint == null)
            {
                kid.focusPoint = FindFocusPoint(kid.kidRoot);
            }
        }
    }

    private void PrepareOutlines()
    {
        foreach (KidFocusTarget kid in kids)
        {
            if (kid == null || kid.kidRoot == null)
            {
                continue;
            }

            if (kid.outline == null)
            {
                kid.outline = kid.kidRoot.GetComponent<Outline>();
            }

            if (kid.outline == null && addMissingOutline)
            {
                kid.outline = kid.kidRoot.gameObject.AddComponent<Outline>();
                kid.outline.OutlineMode = defaultOutlineMode;
                kid.outline.OutlineColor = defaultOutlineColor;
                kid.outline.OutlineWidth = defaultOutlineWidth;
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

    private Transform FindFocusPoint(Transform kidRoot)
    {
        foreach (Transform child in kidRoot.GetComponentsInChildren<Transform>(true))
        {
            if ((!string.IsNullOrWhiteSpace(focusPointTag) && child.CompareTag(focusPointTag)) ||
                string.Equals(child.name, "focus_point", StringComparison.OrdinalIgnoreCase))
            {
                return child;
            }
        }

        return null;
    }

    private bool ContainsKid(Transform kidRoot)
    {
        foreach (KidFocusTarget kid in kids)
        {
            if (kid != null && kid.kidRoot == kidRoot)
            {
                return true;
            }
        }

        return false;
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
        if (wallRoot == null)
        {
            GameObject wallObject = ChatUiAnchorUtility.FindLoadedSceneObject("Walls_01");
            if (wallObject != null)
            {
                wallRoot = wallObject.transform;
            }
        }

        int wallLayer = LayerMask.NameToLayer(wallLayerName);
        if (wallLayer < 0)
        {
            Debug.LogWarning($"Camera collision layer '{wallLayerName}' does not exist.", this);
            return;
        }

        cameraCollisionMask = 1 << wallLayer;
        if (!assignWallCollidersToLayer || wallRoot == null)
        {
            return;
        }

        foreach (Collider wallCollider in wallRoot.GetComponentsInChildren<Collider>(true))
        {
            wallCollider.gameObject.layer = wallLayer;
        }
    }

    private void InitializePhoneScreen()
    {
        if (phoneScreen == null && focusCamera != null)
        {
            foreach (Transform child in focusCamera.GetComponentsInChildren<Transform>(true))
            {
                if (string.Equals(child.name, "PhoneScreen", StringComparison.OrdinalIgnoreCase))
                {
                    phoneScreen = child;
                    break;
                }
            }
        }

        phoneTargetLocalY = phoneHiddenLocalY;
        isPhoneScreenVisible = false;

        if (phoneScreen != null)
        {
            Vector3 localPosition = phoneScreen.localPosition;
            localPosition.y = phoneHiddenLocalY;
            phoneScreen.localPosition = localPosition;
        }
    }

    public void SetPhoneScreenVisible(bool isVisible)
    {
        isPhoneScreenVisible = isVisible && IsFocusing;
        phoneTargetLocalY = isPhoneScreenVisible ? phoneShownLocalY : phoneHiddenLocalY;
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
