using System;
using System.Collections.Generic;
using GreekProject.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class TelevisionFocusCameraController : MonoBehaviour
{
    [Serializable]
    public sealed class KidTelevisionTarget
    {
        public Transform kidRoot;
        [Tooltip("Screen-space click point for the Kid. Keep separate from the camera focus point.")]
        public Transform selectionPoint;
        public Transform focusPoint;
        public KidWaypointAnimationTester activityController;
        public KidDeviceUsageController deviceUsageController;
        public LabeledWaypoint[] televisionSeats;
        public string[] televisionAnimations = { "SitChairIdle" };

        public bool IsValid => kidRoot != null && selectionPoint != null && focusPoint != null &&
                               activityController != null && deviceUsageController != null;
    }

    [Header("Cameras")]
    [SerializeField] private MainRoomCameraController mainRoomController;
    [SerializeField] private KidFocusCameraController kidFocusController;
    [SerializeField] private Camera televisionCamera;

    [Header("Television Selection")]
    [SerializeField] private Transform televisionRoot;
    [SerializeField] private Renderer televisionRenderer;
    [SerializeField] private Outline televisionOutline;
    [SerializeField, Min(1f)] private float televisionSelectionRadius = 190f;

    [Header("Kids Watching Television")]
    [SerializeField] private List<KidTelevisionTarget> kids = new();
    [SerializeField, Min(1f)] private float kidSelectionRadius = 140f;

    [Header("Prebuilt TV UI")]
    [SerializeField] private Canvas televisionCanvas;
    [SerializeField] private GraphicRaycaster televisionRaycaster;
    [SerializeField] private TelevisionVideoFeedUI televisionFeed;

    private bool isFocusing;

    public bool IsFocusing => isFocusing;

    private Camera OverviewCamera => mainRoomController != null ? mainRoomController.ControlledCamera : null;

    private void Awake()
    {
        ValidateSceneReferences();
        SetTelevisionOutline(false);
        SetTelevisionInteractionEnabled(false);
    }

    private void Update()
    {
        if (!isFocusing)
        {
            UpdateTelevisionHover();
            return;
        }

        SetTelevisionOutline(false);

        bool escapePressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        bool rightClickPressed = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
        if (escapePressed || rightClickPressed)
        {
            ShowOverview();
        }
    }

    public bool TryFocusFromOverviewClick(Vector2 screenPosition)
    {
        Camera overviewCamera = OverviewCamera;
        if (isFocusing || overviewCamera == null || !overviewCamera.gameObject.activeInHierarchy)
        {
            return false;
        }

        float nearestDistance = float.PositiveInfinity;
        bool foundTarget = TryGetScreenDistance(overviewCamera, GetTelevisionSelectionPoint(), screenPosition,
            televisionSelectionRadius, ref nearestDistance);

        foreach (KidTelevisionTarget kid in kids)
        {
            if (!IsKidWatchingTelevision(kid))
            {
                continue;
            }

            foundTarget |= TryGetScreenDistance(overviewCamera, kid.selectionPoint.position, screenPosition,
                kidSelectionRadius, ref nearestDistance);
        }

        if (!foundTarget)
        {
            return false;
        }

        FocusTelevision();
        return true;
    }

    public void FocusTelevision()
    {
        if (isFocusing || televisionCamera == null)
        {
            return;
        }

        kidFocusController?.PrepareForExternalCameraFocus();
        SetTelevisionOutline(false);
        isFocusing = true;
        televisionCamera.gameObject.SetActive(true);
        SetTelevisionInteractionEnabled(true);
    }

    public void ShowOverview()
    {
        if (!isFocusing)
        {
            return;
        }

        isFocusing = false;
        SetTelevisionInteractionEnabled(false);
        if (televisionCamera != null)
        {
            televisionCamera.gameObject.SetActive(false);
        }

        kidFocusController?.RestoreOverviewAfterExternalFocus();
    }

    private bool IsKidWatchingTelevision(KidTelevisionTarget kid)
    {
        if (kid == null || !kid.IsValid || !kid.kidRoot.gameObject.activeInHierarchy ||
            kid.activityController.IsTravelling || !kid.deviceUsageController.IsWatchingTelevision)
        {
            return false;
        }

        LabeledWaypoint currentSeat = kid.activityController.CurrentChairSeat;
        if (currentSeat == null || kid.televisionSeats == null ||
            Array.IndexOf(kid.televisionSeats, currentSeat) < 0)
        {
            return false;
        }

        // A reaction animation does not stop the Kid from watching the current TV broadcast.
        // Device activity plus the occupied TV seat is the authoritative state here.
        return true;
    }

    private Vector3 GetTelevisionSelectionPoint()
    {
        if (televisionRenderer != null)
        {
            return televisionRenderer.bounds.center;
        }

        return televisionRoot != null ? televisionRoot.position : Vector3.zero;
    }

    private void UpdateTelevisionHover()
    {
        Camera overviewCamera = OverviewCamera;
        if (Mouse.current == null || overviewCamera == null ||
            !overviewCamera.gameObject.activeInHierarchy)
        {
            SetTelevisionOutline(false);
            return;
        }

        float nearestDistance = float.PositiveInfinity;
        bool isHovered = TryGetScreenDistance(overviewCamera, GetTelevisionSelectionPoint(),
            Mouse.current.position.ReadValue(), televisionSelectionRadius, ref nearestDistance);
        SetTelevisionOutline(isHovered);
    }

    private void SetTelevisionOutline(bool enabledState)
    {
        if (televisionOutline != null && televisionOutline.enabled != enabledState)
        {
            televisionOutline.enabled = enabledState;
        }
    }

    private static bool TryGetScreenDistance(Camera camera, Vector3 worldPosition, Vector2 pointerPosition,
        float selectionRadius, ref float nearestDistance)
    {
        Vector3 projected = camera.WorldToScreenPoint(worldPosition);
        if (projected.z <= 0f)
        {
            return false;
        }

        float distance = Vector2.Distance(pointerPosition, new Vector2(projected.x, projected.y));
        if (distance > selectionRadius || distance >= nearestDistance)
        {
            return false;
        }

        nearestDistance = distance;
        return true;
    }

    private void SetTelevisionInteractionEnabled(bool enabledState)
    {
        if (televisionRaycaster != null)
        {
            televisionRaycaster.enabled = enabledState;
        }

        televisionFeed?.SetTelevisionFocused(enabledState);
    }

    private void ValidateSceneReferences()
    {
        if (mainRoomController == null || kidFocusController == null || televisionCamera == null ||
            televisionRoot == null || televisionRenderer == null || televisionOutline == null ||
            televisionCanvas == null ||
            televisionRaycaster == null || televisionFeed == null)
        {
            Debug.LogError("TV_Forcus Controller requires all scene references assigned before Play.", this);
        }

        if (televisionCanvas != null && televisionCamera != null &&
            televisionCanvas.worldCamera != televisionCamera)
        {
            Debug.LogError("TV Screen Canvas World Camera must be assigned to TV_Forcus before Play.", televisionCanvas);
        }
    }
}
