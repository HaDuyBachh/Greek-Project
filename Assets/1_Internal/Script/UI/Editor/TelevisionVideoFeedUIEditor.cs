#if UNITY_EDITOR
using GreekProject.UI;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TelevisionVideoFeedUI))]
internal sealed class TelevisionVideoFeedUIEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GUILayout.Space(8f);
        if (GUILayout.Button("Apply Television Layout"))
        {
            ((TelevisionVideoFeedUI)target).ApplyTelevisionLayoutInEditMode();
        }
    }
}
#endif
