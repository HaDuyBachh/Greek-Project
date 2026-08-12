using System.IO;
using UnityEditor;
using UnityEngine;
using FiltersAndPostProcessing;

namespace CustomMenu
{
    public class UnityMenuFunctions : MonoBehaviour
    {
        // Add the Pixel Capture menu under Tools
        [MenuItem("Tools/Pixel Capture/Open Photos Folder")]
        private static void OpenPhotosFolder()
        {
            // Find the path to the RenderOutput folder within the project
            string outputPath = FindRenderOutputFolder();

            if (!string.IsNullOrEmpty(outputPath))
            {
                // Open the folder in the file explorer
                System.Diagnostics.Process.Start("explorer.exe", outputPath.Replace('/', '\\'));
                UnityEngine.Debug.Log("Opened Photos Folder: " + outputPath);
            }
            else
            {
                UnityEngine.Debug.LogError("RenderOutput folder not found in project.");
            }
        }

        // Method to find the RenderOutput folder path within the project
        private static string FindRenderOutputFolder()
        {
            string[] guids = AssetDatabase.FindAssets("t:Folder RenderOutput");

            if (guids.Length > 0)
            {
                string folderPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                return Path.Combine(Application.dataPath, folderPath.Substring("Assets/".Length));
            }

            return null;
        }

        [MenuItem("Tools/Pixel Capture/Add New Filter")]
        private static void AddNewFilter()
        {
            AddNewFilterWindow.ShowWindow();
        }
    }
}
