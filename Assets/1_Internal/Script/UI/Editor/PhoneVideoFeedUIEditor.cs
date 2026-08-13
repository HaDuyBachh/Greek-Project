#if UNITY_EDITOR
using GreekProject.UI;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PhoneVideoFeedUI))]
internal sealed class PhoneVideoFeedUIEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(8f);
        if (GUILayout.Button("Rebuild Video Feed"))
        {
            ((PhoneVideoFeedUI)target).Rebuild();
        }
    }
}
#endif
