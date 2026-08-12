using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using PhoneGUIandShutter;
using UnityEngine.Rendering;

namespace ImageLibrary
{
    // Manages the image library by loading images from the drive and displaying them
    public class ImageLibraryManager : MonoBehaviour
    {
        // Public UI elements
        public Image PreviewImage;
        public TextMeshProUGUI ImageTitleText;
        public Image ArrowLeft;
        public Image ArrowRight;
        public Sprite NoImagesAvailableImg;
        public MenuUiManager PhoneGUIandShutterManager;


        // Private variables
        private List<string> imageFiles;
        private List<Texture2D> texturesList;
        private int currentIndex = 0;

        // Colors for enabled and disabled arrows
        private readonly Color disabledColor = new Color(1f, 1f, 1f, 0.2f);
        private readonly Color enabledColor = new Color(1f, 1f, 1f, 1f);

        // Start is called before the first frame update
        void Start()
        {
            // Refresh the image list and display the first image if available
            RefreshImageList();
            if (imageFiles.Count > 0)
            {
                currentIndex = 0;
                DisplayImage(currentIndex, PreviewImage);
                UpdateArrows();
            }

            // Find the MenuUiManager if not assigned
            if (PhoneGUIandShutterManager == null)
            {
                PhoneGUIandShutterManager = FindObjectOfType<MenuUiManager>();
            }
        }

        // Refreshes the list of images from the specified directory
        public void RefreshImageList()
        {
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif

            // Specify the folder path relative to the "Resources" folder
            string folderPath = "RenderOutput";
            texturesList = new List<Texture2D>();
            imageFiles = new List<string>();

#if UNITY_EDITOR
            Texture[] textures = Resources.LoadAll<Texture>(folderPath);
            foreach (Texture texture in textures)
            {
                string texturePath = UnityEditor.AssetDatabase.GetAssetPath(texture);
                imageFiles.Add(texturePath);
                texturesList.Add(texture as Texture2D);
            }
#else
        string fullPath = Path.Combine(Application.dataPath, "Resources", folderPath);
        Debug.Log("Loading textures from: " + fullPath);

        if (Directory.Exists(fullPath))
        {
            string[] filePaths = Directory.GetFiles(fullPath, "*.png");

            foreach (string filePath in filePaths)
            {
                byte[] fileData = File.ReadAllBytes(filePath);
                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(fileData); // This will auto-resize the texture dimensions
                texturesList.Add(texture);
                imageFiles.Add(filePath);
            }

            Debug.Log("Loaded " + texturesList.Count + " textures.");
        }
        
#endif

            // Sort files by creation time descending
            imageFiles.Sort((f1, f2) => File.GetCreationTime(f2).CompareTo(File.GetCreationTime(f1)));
        }

        // Displays the next image in the list
        public void NextImage()
        {
            RefreshImageList();
            if (imageFiles.Count > 0)
            {
                currentIndex = Mathf.Min(currentIndex + 1, imageFiles.Count - 1);
                DisplayImage(currentIndex, PreviewImage);
                UpdateArrows();
            }
        }

        // Displays the previous image in the list
        public void PastImage()
        {
            RefreshImageList();
            if (imageFiles.Count > 0)
            {
                currentIndex = Mathf.Max(currentIndex - 1, 0);
                DisplayImage(currentIndex, PreviewImage);
                UpdateArrows();
            }
        }

        // Displays the latest image in the list
        public void DisplayLatestImage()
        {
            RefreshImageList();
            if (imageFiles.Count > 0)
            {
                currentIndex = 0;
                DisplayImage(currentIndex, PreviewImage);
                UpdateArrows();
            }
        }

        // Displays the image at the specified index in the given Image component
        public void DisplayImageInComponent(int index, Image imageComponent)
        {
            if (index >= 0 && index < imageFiles.Count)
            {
                string filePath = imageFiles[index];
                StartCoroutine(LoadImage(filePath, imageComponent));
                UpdateImageTitle(filePath);
            }
        }

        // Private method to display the image at the specified index
        private void DisplayImage(int index, Image imageComponent)
        {
            if (index >= 0 && index < imageFiles.Count)
            {
                string filePath = imageFiles[index];
                StartCoroutine(LoadImage(filePath, imageComponent));
                UpdateImageTitle(filePath);
            }
        }

        // Coroutine to load an image from a file and set it in an Image component
        private IEnumerator LoadImage(string filePath, Image imageComponent)
        {
            byte[] fileData = File.ReadAllBytes(filePath);
            Texture2D texture = new Texture2D(2, 2);

            // Load the image data into the texture
            if (texture.LoadImage(fileData))
            {
                // Create a sprite from the texture
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                imageComponent.sprite = sprite;

                // Set native size if the image is large
                RectTransform ImgTransform = imageComponent.GetComponent<RectTransform>();
                if (ImgTransform.sizeDelta.x > 512)
                {
                    imageComponent.SetNativeSize();
                }
            }
            else
            {
                Debug.LogError("Failed to load image from file: " + filePath);
            }

            yield return null;
        }

        // Updates the arrow colors based on the current index
        private void UpdateArrows()
        {
            ArrowLeft.color = (currentIndex == 0) ? disabledColor : enabledColor;
            ArrowRight.color = (currentIndex == imageFiles.Count - 1) ? disabledColor : enabledColor;
        }

        // Updates the image title based on the file name
        private void UpdateImageTitle(string filePath)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath); // Get filename without extension
            fileName = fileName.Replace(".", ":"); // Replace dots with colons
            ImageTitleText.text = fileName;
        }

        // Deletes the currently displayed photo
        public void DeleteCurrentPhoto()
        {
            if (currentIndex >= 0 && currentIndex < imageFiles.Count)
            {
                string filePathToDelete = imageFiles[currentIndex];
                File.Delete(filePathToDelete); // Delete the file

                RefreshImageList(); // Reload the image files list

                // Update current index and display the new current image
                if (imageFiles.Count > 0)
                {
                    currentIndex = Mathf.Max(currentIndex - 1, 0); // Move to an earlier photo if possible
                    DisplayImage(currentIndex, PreviewImage);
                    UpdateArrows();
                }
                else
                {
                    // Handle case where there are no more images
                    PreviewImage.sprite = NoImagesAvailableImg;
                    PreviewImage.SetNativeSize();
                    ImageTitleText.text = "";
                    Debug.Log("No more images found after deletion.");

                    // Clear the ImagePreviewSquare in MenuUiManager if available
                    if (PhoneGUIandShutterManager != null)
                    {
                        PhoneGUIandShutterManager.ClearImagePreviewSquare();
                    }
                }
            }
        }
    }
}
