using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using System.IO;

namespace FiltersAndPostProcessing
{
    public class AddNewFilterWindow : EditorWindow
    {
        private PostProcessProfile postProcessProfile;
        private string filterName = "";

        public static void ShowWindow()
        {
            GetWindow<AddNewFilterWindow>("Add New Filter");
        }

        private void OnGUI()
        {
            GUILayout.Label("Add New Filter", EditorStyles.boldLabel);

            // Display the field to select a Post Process Profile
            postProcessProfile = (PostProcessProfile)EditorGUILayout.ObjectField("Post Process Profile", postProcessProfile, typeof(PostProcessProfile), false);

            // Display the field to enter the filter name
            filterName = EditorGUILayout.TextField("Filter Name", filterName);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            // Button to clear the selected profile and filter name
            if (GUILayout.Button("Clear", GUILayout.Width(80)))
            {
                ClearProfile();
            }

            GUILayout.EndHorizontal();

            // Button to add the filter
            if (GUILayout.Button("Add Filter"))
            {
                AddFilter();
            }
        }

        private void ClearProfile()
        {
            // Clear the selected profile and filter name
            postProcessProfile = null;
            filterName = "";
        }

        private void AddFilter()
        {
            // Check if a Post Process Profile is selected
            if (postProcessProfile == null)
            {
                Debug.LogError("No Post Process Profile selected.");
                return;
            }

            // Check if a filter name is specified
            if (string.IsNullOrEmpty(filterName))
            {
                Debug.LogError("No filter name specified.");
                return;
            }

            // Check for Color Grading component in the selected profile
            var colorGrading = postProcessProfile.GetSetting<ColorGrading>();
            if (colorGrading == null)
            {
                Debug.LogError("The selected profile does not contain a Color Grading component.");
                return;
            }

            // Create a new profile to avoid modifying the original
            var newProfile = ScriptableObject.CreateInstance<PostProcessProfile>();

            // Add the Color Grading component to the new profile
            newProfile.AddSettings(colorGrading);

            // Find the Resources folder path dynamically
            string resourcesPath = FindResourcesFolder();
            if (string.IsNullOrEmpty(resourcesPath))
            {
                Debug.LogError("Resources folder not found in project.");
                return;
            }

            // Ensure the subdirectory for color filters exists
            string colorGradesPath = Path.Combine(resourcesPath, "Color Filters/Color grades");
            if (!Directory.Exists(colorGradesPath))
            {
                Directory.CreateDirectory(colorGradesPath);
            }

            // Save the new profile to the specified directory
            string assetPath = Path.Combine("Assets", colorGradesPath.Substring(Application.dataPath.Length + 1), filterName + ".asset");
            AssetDatabase.CreateAsset(newProfile, assetPath);
            AssetDatabase.SaveAssets();

            Debug.Log("New Color Grading profile added: " + assetPath);
        }

        private string FindResourcesFolder()
        {
            string[] guids = AssetDatabase.FindAssets("t:Folder Resources");

            if (guids.Length > 0)
            {
                string folderPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                return Path.Combine(Application.dataPath, folderPath.Substring("Assets/".Length));
            }

            return null;
        }
    }
}
