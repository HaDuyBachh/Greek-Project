using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ChatUiAnchorUtility
{
    public static Camera FindCameraByName(string cameraName)
    {
        Camera[] cameras = Resources.FindObjectsOfTypeAll<Camera>();
        foreach (Camera camera in cameras)
        {
            if (camera != null && IsLoadedSceneObject(camera.gameObject) && string.Equals(camera.name, cameraName, StringComparison.OrdinalIgnoreCase))
            {
                return camera;
            }
        }

        return Camera.main;
    }

    public static GameObject FindLoadedSceneObject(string objectName)
    {
        GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject sceneObject in objects)
        {
            if (sceneObject != null && IsLoadedSceneObject(sceneObject) && string.Equals(sceneObject.name, objectName, StringComparison.OrdinalIgnoreCase))
            {
                return sceneObject;
            }
        }

        return null;
    }

    public static Transform FindAnchorForChild(string childName)
    {
        GameObject child = FindLoadedSceneObject(childName);
        if (child == null)
        {
            return null;
        }

        Transform anchor = FindChildRecursive(child.transform, "ui_anchor");
        if (anchor == null)
        {
            anchor = FindChildRecursive(child.transform, "UI_anchor");
        }

        if (anchor != null)
        {
            return anchor;
        }

        return FindTaggedAnchorUnder(child.transform, "ui_anchor");
    }

    public static RectTransform FindCanvasRoot(RectTransform childRect)
    {
        Canvas canvas = childRect != null ? childRect.GetComponentInParent<Canvas>(true) : null;
        return canvas != null ? canvas.transform as RectTransform : null;
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        foreach (Transform child in root)
        {
            if (string.Equals(child.name, childName, StringComparison.OrdinalIgnoreCase))
            {
                return child;
            }

            Transform result = FindChildRecursive(child, childName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private static Transform FindTaggedAnchorUnder(Transform root, string tagName)
    {
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (HasTag(child.gameObject, tagName))
            {
                return child;
            }
        }

        return null;
    }

    private static bool HasTag(GameObject gameObject, string tagName)
    {
        try
        {
            return gameObject.CompareTag(tagName);
        }
        catch (UnityException)
        {
            return false;
        }
    }

    private static bool IsLoadedSceneObject(GameObject gameObject)
    {
        Scene scene = gameObject.scene;
        return scene.IsValid() && scene.isLoaded;
    }
}
