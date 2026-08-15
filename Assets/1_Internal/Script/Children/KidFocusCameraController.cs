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
    [SerializeField] private MainRoomCameraController mainRoomController;
    [SerializeField] private Camera focusCamera;
    [SerializeField] private ChatUIFollowController chatUiController;
    [SerializeField] private TelevisionFocusCameraController televisionFocusController;

    [Header("Kids")]
    [SerializeField] private List<KidFocusTarget> kids = new List<KidFocusTarget>();

    [Header("Selection")]
    [SerializeField, Min(1f)] private float screenSelectionRadius = 140f;
    [SerializeField] private bool ignoreClicksOverUi = true;
    [SerializeField, Tooltip("Pause the selected Kid's random activity while Kid_Forcus follows it.")]
    private bool pauseFocusedKidActivity = true;

    [Header("Focus Orbit")]
    [SerializeField, Min(0.1f)] private float focusDistance = 1.35f;
    [SerializeField] private float defaultOrbitPitch = 5f;
    [SerializeField] private Vector2 orbitPitchLimits = new Vector2(-15f, 55f);
    [SerializeField, Min(0.01f)] private float orbitSensitivity = 0.18f;
    [SerializeField, Min(0f)] private float positionSmoothTime = 0.12f;
    [SerializeField, Min(0f)] private float rotationSharpness = 18f;

    [Header("Focus Camera Collision")]
    [SerializeField] private LayerMask cameraCollisionMask = 1 << 6;
    [SerializeField, Min(0.01f)] private float cameraCollisionRadius = 0.12f;
    [SerializeField, Min(0f)] private float cameraWallPadding = 0.05f;

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
    private float orbitYaw;
    private float orbitPitch;
    private float phoneTargetLocalY;
    private float phoneSlideVelocity;
    private bool isPhoneScreenVisible;

    public IReadOnlyList<KidFocusTarget> Kids => kids;
    public string SelectedKidId => selectedKid != null ? selectedKid.kidId : string.Empty;
    public bool IsFocusing => selectedKid != null;
    public bool IsPhoneScreenVisible => isPhoneScreenVisible;
    private Camera OverviewCamera => mainRoomController != null ? mainRoomController.ControlledCamera : null;

    public void RegisterViewedVideo(VideoContentEffect effect)
    {
        selectedKid?.activityController?.ApplyViewedVideoEffect(effect);
    }

    private void Awake()
    {
        ValidateSceneReferences();
        PrepareOutlines();
        orbitPitch = Mathf.Clamp(defaultOrbitPitch, orbitPitchLimits.x, orbitPitchLimits.y);
        InitializePhoneScreen();
    }

    private void ValidateSceneReferences()
    {
        if (mainRoomController == null)
        {
            Debug.LogError("Kid_Forcus Controller requires a Main Room Controller reference assigned before Play.", this);
        }

        if (focusCamera == null)
        {
            Debug.LogError("Kid_Forcus Controller requires the Kid_Forcus camera assigned before Play.", this);
        }

        if (phoneScreen == null)
        {
            Debug.LogError("Kid_Forcus Controller requires PhoneScreen assigned before Play.", this);
        }

        if (televisionFocusController == null)
        {
            Debug.LogError("Kid_Forcus Controller requires TV_Forcus Controller assigned before Play.", this);
        }
    }

    private void Update()
    {
        if (televisionFocusController != null && televisionFocusController.IsFocusing)
        {
            SetHoveredKid(null);
            return;
        }

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
            UpdateFocusOrbitInput();
            return;
        }

        if (ignoreClicksOverUi && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            UpdateFocusOrbitInput();
            return;
        }

        Vector2 pointerPosition = Pointer.current.position.ReadValue();
        if (!IsFocusing && televisionFocusController != null &&
            televisionFocusController.TryFocusFromOverviewClick(pointerPosition))
        {
            return;
        }

        TryFocusAtScreenPosition(pointerPosition);
        UpdateFocusOrbitInput();
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
        if (televisionFocusController != null && televisionFocusController.IsFocusing)
        {
            return false;
        }

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
        if (televisionFocusController != null && televisionFocusController.IsFocusing)
        {
            return;
        }

        if (lockFocusWhilePhoneVisible && isPhoneScreenVisible)
        {
            return;
        }

        SetPhoneScreenVisible(false);
        SetFocusedKidActivityPaused(selectedKid, false);
        selectedKid = null;
        followVelocity = Vector3.zero;

        SetCameraActive(OverviewCamera, true);
        SetCameraActive(focusCamera, false);

        if (chatUiController != null && OverviewCamera != null)
        {
            chatUiController.SetProjectionCamera(OverviewCamera);
        }
    }

    public void PrepareForExternalCameraFocus()
    {
        SetPhoneScreenVisible(false);
        SetFocusedKidActivityPaused(selectedKid, false);
        selectedKid = null;
        followVelocity = Vector3.zero;
        SetHoveredKid(null);
        SetCameraActive(OverviewCamera, false);
        SetCameraActive(focusCamera, false);
    }

    public void RestoreOverviewAfterExternalFocus()
    {
        ShowOverview();
    }

    private void TryFocusAtScreenPosition(Vector2 screenPosition)
    {
        Camera selectionCamera = IsFocusing ? focusCamera : OverviewCamera;
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

        Camera selectionCamera = IsFocusing ? focusCamera : OverviewCamera;
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
        if (isChangingFocus)
        {
            SetFocusedKidActivityPaused(selectedKid, false);
        }

        selectedKid = kid;
        followVelocity = Vector3.zero;
        orbitYaw = 0f;
        orbitPitch = Mathf.Clamp(defaultOrbitPitch, orbitPitchLimits.x, orbitPitchLimits.y);

        if (isChangingFocus)
        {
            SetPhoneScreenVisible(false);
            SetFocusedKidActivityPaused(selectedKid, true);
        }

        LookAtSelectedKid(true);

        SetCameraActive(OverviewCamera, false);
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

    private void UpdateFocusOrbitInput()
    {
        bool pointerOverUi = ignoreClicksOverUi && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        if (pointerOverUi || !IsFocusing)
        {
            return;
        }

        Vector2 orbitDelta = GetSinglePointerDragDelta();
        orbitYaw += orbitDelta.x * orbitSensitivity;
        orbitPitch = Mathf.Clamp(
            orbitPitch + orbitDelta.y * orbitSensitivity,
            orbitPitchLimits.x,
            orbitPitchLimits.y);
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
        SetPhoneScreenVisible(false);
        SetFocusedKidActivityPaused(selectedKid, false);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            SetHoveredKid(null);
        }
    }

    private void InitializePhoneScreen()
    {
        phoneTargetLocalY = phoneHiddenLocalY;
        isPhoneScreenVisible = false;

        if (phoneScreen != null)
        {
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

    private void SetFocusedKidActivityPaused(KidFocusTarget kid, bool shouldPause)
    {
        if (pauseFocusedKidActivity && kid != null && kid.activityController != null)
        {
            kid.activityController.SetPausedForFocus(shouldPause);
        }
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
