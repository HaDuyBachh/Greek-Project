using UnityEngine;

public static class ChatUiAnchorUtility
{
    public static RectTransform FindCanvasRoot(RectTransform childRect)
    {
        Canvas canvas = childRect != null ? childRect.GetComponentInParent<Canvas>(true) : null;
        return canvas != null ? canvas.transform as RectTransform : null;
    }
}
