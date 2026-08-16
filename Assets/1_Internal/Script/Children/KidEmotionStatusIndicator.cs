using System;
using GreekProject.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class KidEmotionStatusIndicator : MonoBehaviour
{
    [Header("Kid State")]
    [SerializeField] private string kidId = "Kid1";
    [SerializeField] private KidWaypointAnimationTester activityController;
    [SerializeField] private KidDeviceUsageController deviceUsageController;
    [SerializeField] private KidFocusCameraController kidFocusController;
    [SerializeField] private TelevisionFocusCameraController televisionFocusController;

    [Header("Projection")]
    [SerializeField] private Transform worldAnchor;
    [SerializeField] private Camera mainRoomCamera;
    [SerializeField] private Camera kidFocusCamera;
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform canvasRoot;
    [SerializeField] private RectTransform indicatorRoot;
    [SerializeField] private Vector2 screenOffset = new Vector2(0f, 95f);
    [SerializeField] private Vector2 deviceScreenOffset = new Vector2(0f, 35f);
    [SerializeField] private bool clampToCanvas = true;
    [SerializeField] private Vector2 canvasPadding = new Vector2(24f, 24f);

    [Header("Prebuilt UI")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RoundedRectGraphic background;
    [SerializeField] private Image arrowUpIcon;
    [SerializeField] private Image arrowDownIcon;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private RectTransform deviceIndicatorRoot;
    [SerializeField] private CanvasGroup deviceCanvasGroup;
    [SerializeField] private RoundedRectGraphic deviceBackground;
    [SerializeField] private TMP_Text deviceStatusText;

    [Header("Appearance")]
    [SerializeField] private string positiveText = "POSITIVE";
    [SerializeField] private string negativeText = "NEGATIVE";
    [SerializeField] private Color positiveColor = new Color(0.067f, 0.396f, 0.188f, 0.98f);
    [SerializeField] private Color negativeColor = new Color(0.545f, 0.118f, 0.176f, 0.98f);
    [SerializeField] private string watchingPhoneText = "WATCHING PHONE";
    [SerializeField] private string watchingTelevisionText = "WATCHING TV";
    [SerializeField] private Color watchingPhoneColor = new Color(0.055f, 0.286f, 0.627f, 0.98f);
    [SerializeField] private Color watchingTelevisionColor = new Color(0.345f, 0.129f, 0.592f, 0.98f);

    private KidWaypointAnimationTester.EmotionState displayedEmotion;
    private bool hasDisplayedEmotion;
    private KidDeviceUsageController.DeviceActivity displayedDeviceActivity;
    private bool hasDisplayedDeviceActivity;

    private void Awake()
    {
        ValidatePrebuiltReferences();
        SetVisible(false);
        RefreshAppearance(true);
        RefreshDeviceAppearance(true);
    }

    private void LateUpdate()
    {
        if (!ShouldBeVisible(out Camera projectionCamera))
        {
            SetVisible(false);
            return;
        }

        Vector3 screenPoint = projectionCamera.WorldToScreenPoint(worldAnchor.position);
        if (screenPoint.z <= 0f)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);
        RefreshAppearance(false);
        bool showDeviceStatus = RefreshDeviceAppearance(false);
        SetCanvasGroupVisible(deviceCanvasGroup, showDeviceStatus);
        Vector2 projectedPosition = new Vector2(screenPoint.x, screenPoint.y);
        SetScreenPosition(indicatorRoot, projectedPosition + screenOffset);
        if (showDeviceStatus)
        {
            SetScreenPosition(deviceIndicatorRoot, projectedPosition + deviceScreenOffset);
        }
    }

    private bool ShouldBeVisible(out Camera projectionCamera)
    {
        projectionCamera = null;
        if (televisionFocusController != null && televisionFocusController.IsFocusing)
        {
            return false;
        }

        bool isFocused = kidFocusController != null &&
                         string.Equals(kidFocusController.SelectedKidId, kidId, StringComparison.OrdinalIgnoreCase);
        if (isFocused && kidFocusCamera != null && kidFocusCamera.gameObject.activeInHierarchy)
        {
            projectionCamera = kidFocusCamera;
            return true;
        }

        bool isHovered = kidFocusController != null &&
                         string.Equals(kidFocusController.HoveredKidId, kidId, StringComparison.OrdinalIgnoreCase);
        if (isHovered && mainRoomCamera != null && mainRoomCamera.gameObject.activeInHierarchy)
        {
            projectionCamera = mainRoomCamera;
            return true;
        }

        return false;
    }

    private void SetScreenPosition(RectTransform root, Vector2 screenPosition)
    {
        if (canvas == null || canvasRoot == null || root == null)
        {
            return;
        }

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            if (clampToCanvas)
            {
                Vector2 halfSize = root.rect.size * (canvas.scaleFactor * 0.5f);
                screenPosition.x = Mathf.Clamp(screenPosition.x,
                    halfSize.x + canvasPadding.x,
                    Screen.width - halfSize.x - canvasPadding.x);
                screenPosition.y = Mathf.Clamp(screenPosition.y,
                    halfSize.y + canvasPadding.y,
                    Screen.height - halfSize.y - canvasPadding.y);
            }

            root.position = screenPosition;
            return;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRoot, screenPosition, canvas.worldCamera, out Vector2 localPoint))
        {
            return;
        }

        if (clampToCanvas)
        {
            Rect canvasRect = canvasRoot.rect;
            Vector2 halfSize = root.rect.size * 0.5f;
            localPoint.x = Mathf.Clamp(localPoint.x,
                canvasRect.xMin + halfSize.x + canvasPadding.x,
                canvasRect.xMax - halfSize.x - canvasPadding.x);
            localPoint.y = Mathf.Clamp(localPoint.y,
                canvasRect.yMin + halfSize.y + canvasPadding.y,
                canvasRect.yMax - halfSize.y - canvasPadding.y);
        }

        root.anchoredPosition = localPoint;
    }

    private bool RefreshDeviceAppearance(bool force)
    {
        KidDeviceUsageController.DeviceActivity activity = ResolveDeviceActivity();
        bool isWatching = activity != KidDeviceUsageController.DeviceActivity.None;
        if (!force && hasDisplayedDeviceActivity && activity == displayedDeviceActivity)
        {
            return isWatching;
        }

        displayedDeviceActivity = activity;
        hasDisplayedDeviceActivity = true;
        if (!isWatching)
        {
            return false;
        }

        bool watchingPhone = activity == KidDeviceUsageController.DeviceActivity.Phone;
        if (deviceBackground != null)
        {
            deviceBackground.color = watchingPhone ? watchingPhoneColor : watchingTelevisionColor;
        }

        if (deviceStatusText != null)
        {
            deviceStatusText.text = watchingPhone ? watchingPhoneText : watchingTelevisionText;
            deviceStatusText.color = Color.white;
        }

        return true;
    }

    private KidDeviceUsageController.DeviceActivity ResolveDeviceActivity()
    {
        if (deviceUsageController == null)
        {
            return KidDeviceUsageController.DeviceActivity.None;
        }

        if (deviceUsageController.IsWatchingPhone)
        {
            return KidDeviceUsageController.DeviceActivity.Phone;
        }

        return deviceUsageController.IsWatchingTelevision
            ? KidDeviceUsageController.DeviceActivity.Television
            : KidDeviceUsageController.DeviceActivity.None;
    }

    private void RefreshAppearance(bool force)
    {
        KidWaypointAnimationTester.EmotionState emotion = activityController != null
            ? activityController.CurrentEmotion
            : KidWaypointAnimationTester.EmotionState.Stable;
        if (!force && hasDisplayedEmotion && emotion == displayedEmotion)
        {
            return;
        }

        displayedEmotion = emotion;
        hasDisplayedEmotion = true;
        bool isNegative = emotion == KidWaypointAnimationTester.EmotionState.Anxious ||
                          emotion == KidWaypointAnimationTester.EmotionState.Panic;
        if (background != null)
        {
            background.color = isNegative ? negativeColor : positiveColor;
        }

        if (arrowUpIcon != null)
        {
            arrowUpIcon.color = Color.white;
            arrowUpIcon.gameObject.SetActive(!isNegative);
        }

        if (arrowDownIcon != null)
        {
            arrowDownIcon.color = Color.white;
            arrowDownIcon.gameObject.SetActive(isNegative);
        }

        if (statusText != null)
        {
            statusText.text = isNegative ? negativeText : positiveText;
            statusText.color = Color.white;
        }
    }

    private void SetVisible(bool visible)
    {
        SetCanvasGroupVisible(canvasGroup, visible);
        if (!visible)
        {
            SetCanvasGroupVisible(deviceCanvasGroup, false);
        }
    }

    private static void SetCanvasGroupVisible(CanvasGroup group, bool visible)
    {
        if (group == null)
        {
            return;
        }

        group.alpha = visible ? 1f : 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    private void ValidatePrebuiltReferences()
    {
        if (string.IsNullOrWhiteSpace(kidId) || activityController == null || deviceUsageController == null ||
            kidFocusController == null ||
            televisionFocusController == null || worldAnchor == null || mainRoomCamera == null ||
            kidFocusCamera == null || canvas == null || canvasRoot == null || indicatorRoot == null || canvasGroup == null ||
            background == null || arrowUpIcon == null || arrowDownIcon == null || statusText == null ||
            deviceIndicatorRoot == null || deviceCanvasGroup == null || deviceBackground == null ||
            deviceStatusText == null)
        {
            Debug.LogError("Kid Emotion Status Indicator requires all Kid, camera, sprite and prebuilt UI references assigned before Play.", this);
        }
    }
}
