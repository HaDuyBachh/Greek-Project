using System;
using GreekProject.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class KidEmotionStatusIndicator : MonoBehaviour
{
    [Serializable]
    private sealed class KidBinding
    {
        public string kidId = "Kid1";
        public KidWaypointAnimationTester activityController;
        public KidDeviceUsageController deviceUsageController;
        public Transform worldAnchor;
    }

    [Header("Kid State")]
    [SerializeField] private KidBinding[] kids;
    [SerializeField, HideInInspector] private string kidId = "Kid1";
    [SerializeField, HideInInspector] private KidWaypointAnimationTester activityController;
    [SerializeField, HideInInspector] private KidDeviceUsageController deviceUsageController;
    [SerializeField] private KidFocusCameraController kidFocusController;
    [SerializeField] private TelevisionFocusCameraController televisionFocusController;

    [Header("Projection")]
    [SerializeField, HideInInspector] private Transform worldAnchor;
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
    private KidBinding activeKid;

    private void Awake()
    {
        ValidatePrebuiltReferences();
        SetVisible(false);
        RefreshAppearance(true);
        RefreshDeviceAppearance(true);
    }

    private void LateUpdate()
    {
        if (!TryResolveVisibleKid(out KidBinding visibleKid, out Camera projectionCamera))
        {
            activeKid = null;
            SetVisible(false);
            return;
        }

        if (activeKid != visibleKid)
        {
            activeKid = visibleKid;
            hasDisplayedEmotion = false;
            hasDisplayedDeviceActivity = false;
        }

        Vector3 screenPoint = projectionCamera.WorldToScreenPoint(visibleKid.worldAnchor.position);
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

    private bool TryResolveVisibleKid(out KidBinding visibleKid, out Camera projectionCamera)
    {
        visibleKid = null;
        projectionCamera = null;
        if (televisionFocusController != null && televisionFocusController.IsFocusing)
        {
            return false;
        }

        string selectedKidId = kidFocusController != null ? kidFocusController.SelectedKidId : string.Empty;
        visibleKid = FindKid(selectedKidId);
        if (visibleKid != null && kidFocusCamera != null && kidFocusCamera.gameObject.activeInHierarchy)
        {
            projectionCamera = kidFocusCamera;
            return true;
        }

        string hoveredKidId = kidFocusController != null ? kidFocusController.HoveredKidId : string.Empty;
        visibleKid = FindKid(hoveredKidId);
        if (visibleKid != null && mainRoomCamera != null && mainRoomCamera.gameObject.activeInHierarchy)
        {
            projectionCamera = mainRoomCamera;
            return true;
        }

        visibleKid = null;
        return false;
    }

    private KidBinding FindKid(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        if (kids != null)
        {
            foreach (KidBinding candidate in kids)
            {
                if (candidate != null &&
                    string.Equals(candidate.kidId, id, StringComparison.OrdinalIgnoreCase))
                {
                    return candidate;
                }
            }
        }

        if (string.Equals(kidId, id, StringComparison.OrdinalIgnoreCase))
        {
            return new KidBinding
            {
                kidId = kidId,
                activityController = activityController,
                deviceUsageController = deviceUsageController,
                worldAnchor = worldAnchor
            };
        }

        return null;
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
        KidDeviceUsageController.DeviceActivity activity = ResolveDeviceActivity(activeKid);
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

    private static KidDeviceUsageController.DeviceActivity ResolveDeviceActivity(KidBinding kid)
    {
        KidDeviceUsageController device = kid?.deviceUsageController;
        if (device == null)
        {
            return KidDeviceUsageController.DeviceActivity.None;
        }

        if (device.IsWatchingPhone)
        {
            return KidDeviceUsageController.DeviceActivity.Phone;
        }

        return device.IsWatchingTelevision
            ? KidDeviceUsageController.DeviceActivity.Television
            : KidDeviceUsageController.DeviceActivity.None;
    }

    private void RefreshAppearance(bool force)
    {
        KidWaypointAnimationTester activity = activeKid?.activityController;
        KidWaypointAnimationTester.EmotionState emotion = activity != null
            ? activity.CurrentEmotion
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
        if (!HasValidKidBindings() || kidFocusController == null ||
            televisionFocusController == null || mainRoomCamera == null ||
            kidFocusCamera == null || canvas == null || canvasRoot == null || indicatorRoot == null || canvasGroup == null ||
            background == null || arrowUpIcon == null || arrowDownIcon == null || statusText == null ||
            deviceIndicatorRoot == null || deviceCanvasGroup == null || deviceBackground == null ||
            deviceStatusText == null)
        {
            Debug.LogError("Kid Emotion Status Indicator requires all Kid, camera, sprite and prebuilt UI references assigned before Play.", this);
        }
    }

    private bool HasValidKidBindings()
    {
        if (kids != null && kids.Length > 0)
        {
            foreach (KidBinding kid in kids)
            {
                if (kid == null || string.IsNullOrWhiteSpace(kid.kidId) || kid.activityController == null ||
                    kid.deviceUsageController == null || kid.worldAnchor == null)
                {
                    return false;
                }
            }

            return true;
        }

        return !string.IsNullOrWhiteSpace(kidId) && activityController != null &&
               deviceUsageController != null && worldAnchor != null;
    }
}
