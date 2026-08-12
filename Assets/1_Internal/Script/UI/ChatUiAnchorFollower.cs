using UnityEngine;

[ExecuteAlways]
public class ChatUiAnchorFollower : MonoBehaviour
{
    [SerializeField] private Transform worldAnchor;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private RectTransform canvasRoot;
    [SerializeField] private Vector2 screenOffset = Vector2.zero;
    [SerializeField] private bool hideWhenBehindCamera = true;
    [SerializeField] private bool clampToCanvas = true;
    [SerializeField] private Vector2 canvasPadding = new Vector2(24f, 24f);

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

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
            if (canvasGroup == null && Application.isPlaying)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (canvasRoot == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                canvasRoot = canvas.transform as RectTransform;
            }
        }

        if (worldCamera == null)
        {
            worldCamera = ChatUiAnchorUtility.FindCameraByName("Main_room");
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

        SetVisible(isInFront || !hideWhenBehindCamera);

        if (!isInFront && hideWhenBehindCamera)
        {
            return;
        }

        Vector2 targetScreenPosition = (Vector2)screenPoint + screenOffset;

        Camera canvasCamera = null;
        Canvas canvas = canvasRoot.GetComponent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            rectTransform.position = clampToCanvas ? ClampScreenPosition(targetScreenPosition) : targetScreenPosition;
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
