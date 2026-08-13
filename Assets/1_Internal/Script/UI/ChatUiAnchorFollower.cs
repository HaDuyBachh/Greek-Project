using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class ChatUiAnchorFollower : MonoBehaviour
{
    [SerializeField] private Transform worldAnchor;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private RectTransform canvasRoot;
    [SerializeField] private Vector2 screenOffset = Vector2.zero;
    [SerializeField] private bool hideWhenBehindCamera = true;
    [SerializeField] private bool clampToCanvas = true;
    [SerializeField] private Vector2 canvasPadding = new Vector2(24f, 24f);
    [SerializeField] private RectTransform screenOccluder;
    [SerializeField, Min(0f)] private float screenOccluderPadding = 8f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private readonly Vector3[] rectCorners = new Vector3[4];
    private readonly Vector3[] occluderCorners = new Vector3[4];

    public Transform WorldAnchor
    {
        get => worldAnchor;
        set => worldAnchor = value;
    }

    public Camera WorldCamera
    {
        get => worldCamera;
        set => worldCamera = value;
    }

    public RectTransform CanvasRoot
    {
        get => canvasRoot;
        set => canvasRoot = value;
    }

    public void SetScreenOccluder(RectTransform occluder, float padding)
    {
        screenOccluder = occluder;
        screenOccluderPadding = Mathf.Max(0f, padding);
    }

    private void Awake()
    {
        CacheComponents();
    }

    private void OnEnable()
    {
        CacheComponents();
        UpdatePosition();
    }

    private void LateUpdate()
    {
        UpdatePosition();
    }

    private void CacheComponents()
    {
        if (rectTransform == null)
        {
            rectTransform = transform as RectTransform;
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (canvasRoot == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                canvasRoot = canvas.transform as RectTransform;
            }
        }

    }

    private void UpdatePosition()
    {
        if (rectTransform == null || canvasRoot == null || worldAnchor == null || worldCamera == null)
        {
            return;
        }

        Vector3 screenPoint = worldCamera.WorldToScreenPoint(worldAnchor.position);
        bool isInFront = screenPoint.z > 0f;

        if (!isInFront && hideWhenBehindCamera)
        {
            SetVisible(false);
            return;
        }

        Vector2 targetScreenPosition = (Vector2)screenPoint + screenOffset;

        Camera canvasCamera = null;
        Canvas canvas = canvasRoot.GetComponent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            rectTransform.position = clampToCanvas ? ClampScreenPosition(targetScreenPosition) : targetScreenPosition;
            SetVisible(!OverlapsScreenOccluder(null));
            return;
        }

        if (canvas != null)
        {
            canvasCamera = canvas.worldCamera;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRoot,
            targetScreenPosition,
            canvasCamera,
            out Vector2 anchoredPosition);

        if (clampToCanvas)
        {
            anchoredPosition = ClampToCanvas(anchoredPosition);
        }

        rectTransform.anchoredPosition = anchoredPosition;
        SetVisible(!OverlapsScreenOccluder(canvasCamera));
    }

    private bool OverlapsScreenOccluder(Camera chatCanvasCamera)
    {
        if (screenOccluder == null || !screenOccluder.gameObject.activeInHierarchy)
        {
            return false;
        }

        Canvas occluderCanvas = screenOccluder.GetComponentInParent<Canvas>();
        Camera occluderCamera = occluderCanvas != null && occluderCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? occluderCanvas.worldCamera
            : null;

        Rect chatRect = GetScreenRect(rectTransform, chatCanvasCamera, rectCorners);
        Rect occluderRect = GetScreenRect(screenOccluder, occluderCamera, occluderCorners);
        occluderRect.xMin -= screenOccluderPadding;
        occluderRect.xMax += screenOccluderPadding;
        occluderRect.yMin -= screenOccluderPadding;
        occluderRect.yMax += screenOccluderPadding;
        return chatRect.Overlaps(occluderRect);
    }

    private static Rect GetScreenRect(RectTransform rect, Camera camera, Vector3[] corners)
    {
        rect.GetWorldCorners(corners);
        Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
        Vector2 max = min;

        for (int i = 1; i < corners.Length; i++)
        {
            Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, corners[i]);
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }

        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private Vector2 ClampScreenPosition(Vector2 screenPosition)
    {
        Rect ownRect = rectTransform.rect;

        float minX = canvasPadding.x + ownRect.width * rectTransform.pivot.x;
        float maxX = Screen.width - canvasPadding.x - ownRect.width * (1f - rectTransform.pivot.x);
        float minY = canvasPadding.y + ownRect.height * rectTransform.pivot.y;
        float maxY = Screen.height - canvasPadding.y - ownRect.height * (1f - rectTransform.pivot.y);

        if (minX <= maxX)
        {
            screenPosition.x = Mathf.Clamp(screenPosition.x, minX, maxX);
        }

        if (minY <= maxY)
        {
            screenPosition.y = Mathf.Clamp(screenPosition.y, minY, maxY);
        }

        return screenPosition;
    }

    private Vector2 ClampToCanvas(Vector2 anchoredPosition)
    {
        Rect canvasRect = canvasRoot.rect;
        Rect ownRect = rectTransform.rect;

        float minX = canvasRect.xMin + canvasPadding.x + ownRect.width * rectTransform.pivot.x;
        float maxX = canvasRect.xMax - canvasPadding.x - ownRect.width * (1f - rectTransform.pivot.x);
        float minY = canvasRect.yMin + canvasPadding.y + ownRect.height * rectTransform.pivot.y;
        float maxY = canvasRect.yMax - canvasPadding.y - ownRect.height * (1f - rectTransform.pivot.y);

        if (minX <= maxX)
        {
            anchoredPosition.x = Mathf.Clamp(anchoredPosition.x, minX, maxX);
        }

        if (minY <= maxY)
        {
            anchoredPosition.y = Mathf.Clamp(anchoredPosition.y, minY, maxY);
        }

        return anchoredPosition;
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }
    }
}
