using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using ImageLibrary;
//This is the main script it controls most functions in the GUI of the phone and also taking photos
namespace PhoneGUIandShutter
{
    public class MenuUiManager : MonoBehaviour
    {
        // Enable this setting if rendering to a texture
        [Header("Only enable the following setting if your rendering to a texture")]
        public bool CameraViewportRectManipulation = false;

        // Animators for UI elements
        [Header("Animators")]
        public Animator mainUiAnimator;
        public Animator AspectRatioTextAnimator;

        // Game objects used in the UI
        [Header("Objects")]
        public GameObject gridLinesObj;
        public GameObject[] blackBorders;
        public RectTransform cameraRenderTexRect;
        public Camera RenderCamera;
        public Image ImagePreviewSquare;

        // Manager for image library
        [Header("Managers")]
        public ImageLibraryManager imageLibraryManager;

        // Internal state variables
        private bool aspectToggle = false;
        private bool gridLinesState = false;
        private bool InTranstition = false;

        // Settings for aspect ratio transition
        [Header("4:3 <-> 16:9 transition settings")]
        public float AspectTransitionDuration = 0.5f;
        public float CamAspectAdjustment = 0.2f;

        // Flash settings
        private bool FlashEnabled = false;
        public Light FlashLight;

        private Canvas MainCanvas;

        private void Start()
        {
            // Ensure mainUiAnimator is initialized
            if (mainUiAnimator == null)
            {
                mainUiAnimator = GetComponent<Animator>();
            }

            // Ensure imageLibraryManager is initialized
            if (imageLibraryManager == null)
            {
                imageLibraryManager = FindObjectOfType<ImageLibraryManager>();
            }

            // Check if MainCanvas is assigned
            MainCanvas = GetComponent<Canvas>();
            if (MainCanvas == null)
            {
                // Warn if the canvas for the phone is not found
                Debug.LogWarning("The canvas for the phone cant be found, this means that the camera for the canvas may be null and clicks might not work.");
            }

            // Check if a world camera is assigned to the canvas in world space mode
            if (MainCanvas.worldCamera == null)
            {
                Debug.LogWarning("The canvas is set to world space but a camera is not set, finding cameras in the scene to handle click events. To disable this assign a camera to the canvas in canvas->Render Mode->Event Camera");

                // Get all cameras in the scene
                Camera[] allCameras = Camera.allCameras;

                // Loop through all cameras to find a suitable one for the canvas
                foreach (Camera cam in allCameras)
                {
                    // Check if the camera is not the RenderCamera
                    if (cam != RenderCamera)
                    {
                        // Assign the first camera that is not RenderCamera to MainCanvas.worldCamera
                        MainCanvas.worldCamera = cam;
                        break; // Break the loop once the first suitable camera is found and assigned
                    }
                }
            }

            // Get initial state of gridLinesObj
            gridLinesState = gridLinesObj.activeSelf;

            // Display the latest image when starting the game
            imageLibraryManager.DisplayLatestImage();
            imageLibraryManager.DisplayImageInComponent(0, ImagePreviewSquare);
        }

        // Toggle options menu visibility
        public void CollapseOptions()
        {
            bool optionsState = mainUiAnimator.GetBool("OptionsState");
            mainUiAnimator.SetBool("OptionsState", !optionsState);
        }

        // Toggle grid lines visibility
        public void ShowGridLines()
        {
            gridLinesState = !gridLinesState;
            gridLinesObj.SetActive(gridLinesState);
        }

        // Toggle aspect ratio between 4:3 and 16:9
        public void ToggleAspectRatio()
        {
            if (!InTranstition)
            {
                aspectToggle = !aspectToggle;
                StartCoroutine(InterpolateAspectRatio());
                StartCoroutine(InterpolateOpacity());
            }
        }

        // Coroutine to interpolate aspect ratio transition
        private IEnumerator InterpolateAspectRatio()
        {
            InTranstition = true;
            float targetHeight;
            float targetYPos;
            float targetFOV;
            Rect targetViewportRect;

            if (aspectToggle)
            {
                SetTargetAspectRatio(true, out targetHeight, out targetYPos, out targetFOV, out targetViewportRect);
            }
            else
            {
                SetTargetAspectRatio(false, out targetHeight, out targetYPos, out targetFOV, out targetViewportRect);
            }

            float initialHeight = cameraRenderTexRect.sizeDelta.y;
            float initialYPos = cameraRenderTexRect.anchoredPosition.y;
            float initialFOV = RenderCamera.fieldOfView;
            Rect initialViewportRect = RenderCamera.rect;

            Rect newViewportRect = initialViewportRect; // Initialize newViewportRect

            float startTime = Time.time;
            while (Time.time - startTime < AspectTransitionDuration)
            {
                float t = (Time.time - startTime) / AspectTransitionDuration;

                float newHeight = Mathf.Lerp(initialHeight, targetHeight, t);
                float newYPos = Mathf.Lerp(initialYPos, targetYPos, t);
                float newFOV = Mathf.Lerp(initialFOV, targetFOV, t);
                float newXPos = Mathf.Lerp(initialViewportRect.x, targetViewportRect.x, t);

                // Adjusting the aspect ratio of the camera viewport rect
                float newWidth = 1f - newXPos * 2f;
                newViewportRect = new Rect(newXPos, initialViewportRect.y, 1f, 1f);
                if (CameraViewportRectManipulation == true)
                {
                    cameraRenderTexRect.sizeDelta = new Vector2(cameraRenderTexRect.sizeDelta.x, newHeight);
                    cameraRenderTexRect.anchoredPosition = new Vector2(cameraRenderTexRect.anchoredPosition.x, newYPos);
                    RenderCamera.rect = newViewportRect;
                }
                RenderCamera.fieldOfView = newFOV;

                yield return null;
            }

            // Ensure final values are set after interpolation
            if (CameraViewportRectManipulation == true)
            {
                cameraRenderTexRect.sizeDelta = new Vector2(cameraRenderTexRect.sizeDelta.x, targetHeight);
                cameraRenderTexRect.anchoredPosition = new Vector2(cameraRenderTexRect.anchoredPosition.x, targetYPos);
                RenderCamera.rect = newViewportRect;
            }
            RenderCamera.fieldOfView = targetFOV;
            InTranstition = false;
        }

        // Set target aspect ratio values
        private void SetTargetAspectRatio(bool isAspectToggleOn, out float targetHeight, out float targetYPos, out float targetFOV, out Rect targetViewportRect)
        {
            AspectRatioTextAnimator.SetBool("16:9IsTrue", isAspectToggleOn);
            if (isAspectToggleOn)
            {
                targetHeight = 1920f;
                targetYPos = 0f;
                targetFOV = RenderCamera.fieldOfView * 1.33f;
                targetViewportRect = new Rect(CamAspectAdjustment, 0f, 0f, 0f);
            }
            else
            {
                targetHeight = 1440f;
                targetYPos = 105f;
                targetFOV = RenderCamera.fieldOfView * 0.75f;
                targetViewportRect = new Rect(0f, 0f, 0f, 0f);
            }
        }

        // Coroutine to interpolate opacity during aspect ratio transition
        private IEnumerator InterpolateOpacity()
        {
            float elapsedTime = 0f;
            float initialOpacity;
            float targetOpacity;

            if (aspectToggle)
            {
                initialOpacity = 1f;
                targetOpacity = 0f;
            }
            else
            {
                initialOpacity = 0f;
                targetOpacity = 1f;
            }

            while (elapsedTime < AspectTransitionDuration)
            {
                float t = elapsedTime / AspectTransitionDuration;

                // Interpolate opacity for each black border
                foreach (GameObject border in blackBorders)
                {
                    Image image = border.GetComponent<Image>();
                    if (image != null)
                    {
                        Color color = image.color;
                        color.a = Mathf.Lerp(initialOpacity, targetOpacity, t);
                        image.color = color;
                    }
                }

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // Ensure final opacity values are set after interpolation
            foreach (GameObject border in blackBorders)
            {
                Image image = border.GetComponent<Image>();
                if (image != null)
                {
                    Color color = image.color;
                    color.a = targetOpacity;
                    image.color = color;
                }
            }
        }

        // Toggle flash for taking pictures
        public void ToggleFlash()
        {
            FlashEnabled = !FlashEnabled;
        }

        // Start flash sequence and capture picture
        public void TakePicture()
        {
            if (FlashEnabled)
            {
                StartCoroutine(FlashAndCaptureSequence());
            }
            else
            {
                CapturePicture();
            }
        }

        // Coroutine for flash sequence when taking pictures
        private IEnumerator FlashAndCaptureSequence()
        {
            FlashLight.enabled = true;
            yield return new WaitForSeconds(1f);

            FlashLight.enabled = false;
            yield return new WaitForSeconds(0.15f);

            FlashLight.enabled = true;
            CapturePicture();

            yield return new WaitForSeconds(0.3f);
            FlashLight.enabled = false;
        }

        // Capture the picture from RenderCamera and save as file
        private void CapturePicture()
        {
            // Create render texture with correct settings
            int targetWidth = 1080;
            int targetHeight = aspectToggle ? 1920 : 1440;
            RenderTexture renderTexture = new RenderTexture(targetWidth, targetHeight, 24);

            // Save the current camera settings
            RenderTexture currentRenderTexture = RenderCamera.targetTexture;
            RenderTexture currentActiveTexture = RenderTexture.active;
            Rect currentRect = RenderCamera.rect;

            // Set the camera to render to the new render texture
            RenderCamera.targetTexture = renderTexture;
            RenderCamera.rect = new Rect(0, 0, 1, 1);

            // Render the camera's view
            RenderCamera.Render();

            // Create a new Texture2D with the same dimensions as the render texture
            Texture2D texture = new Texture2D(targetWidth, targetHeight, TextureFormat.RGB24, false);

            // Read the render texture contents into the Texture2D
            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
            texture.Apply();

            // Determine the path to the "Resources" folder
            string resourcesFolderPath = FindResourcesFolderPath();

            if (resourcesFolderPath == null)
            {
                Debug.LogError("Failed to find the 'Resources' folder.");
                return;
            }

            // Specify the subfolder name where images will be saved
            string subfolderName = "RenderOutput";
            string directoryPath = Path.Combine(resourcesFolderPath, subfolderName);

            // Create the subfolder if it doesn't exist
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            // Save the image as a file
            #if UNITY_EDITOR
                string fileName = DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss") + ".png";
            #else
                string fileName = DateTime.Now.ToString("yyyyMMdHHmmss") + ".png";
            #endif
            string filePath = Path.Combine(directoryPath, fileName);
            byte[] bytes = texture.EncodeToPNG();
            File.WriteAllBytes(filePath, bytes);

            // Refresh image list and display the latest image
            imageLibraryManager.DisplayLatestImage();
            imageLibraryManager.DisplayImageInComponent(0, ImagePreviewSquare);

            // Restore the camera settings
            RenderCamera.targetTexture = currentRenderTexture;
            RenderCamera.rect = currentRect;
            RenderTexture.active = currentActiveTexture;

            // Clean up
            renderTexture.Release();
            Destroy(texture);
        }
        private string FindResourcesFolderPath()
        {
            // Get the path to the "Assets" folder
            string assetsPath = Application.dataPath;

            // Navigate up to find the "Resources" folder within the project structure
            DirectoryInfo assetsDir = new DirectoryInfo(assetsPath);
            DirectoryInfo projectDir = assetsDir.Parent; // Assuming Assets is directly under the project root

            if (projectDir != null)
            {
                // Check if the "Resources" folder exists within the project directory
                DirectoryInfo[] resourcesDirs = projectDir.GetDirectories("Resources", SearchOption.AllDirectories);

                if (resourcesDirs.Length > 0)
                {
                    // Return the path to the first found "Resources" folder
                    return resourcesDirs[0].FullName;
                }
            }

            // Return null if "Resources" folder is not found
            return null;
        }
        // Clear the image preview square
        public void ClearImagePreviewSquare()
        {
            ImagePreviewSquare.sprite = null;
        }
    }
}
