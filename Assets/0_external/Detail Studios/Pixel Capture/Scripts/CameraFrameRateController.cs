using UnityEngine;
using System.Collections;

namespace PerformanceOptimizations
{

    // Simple frame rate lock script to improve performance by controlling the rendering rate of the camera
    public class CameraFrameRateController : MonoBehaviour
    {
        // Reference to the camera whose frame rate we want to control
        public Camera phoneCamera;
        // Desired frame rate for the camera
        public float phoneCameraFrameRate = 60f;

        // Interval between each frame based on the desired frame rate
        private float phoneCameraFrameInterval;

        // Start is called before the first frame update
        void Start()
        {
            // If the phoneCamera reference is not set, try to get the Camera component attached to this GameObject
            if (phoneCamera == null)
            {
                phoneCamera = GetComponent<Camera>();
            }

            // Disable the phone camera's automatic rendering to manually control it
            phoneCamera.enabled = false;

            // Calculate the interval between each frame for the phone camera
            phoneCameraFrameInterval = 1f / phoneCameraFrameRate;

            // Start the coroutine to render the phone camera at a fixed rate
            StartCoroutine(RenderPhoneCameraAtFixedRate());
        }

        // Coroutine to render the phone camera at a fixed rate
        IEnumerator RenderPhoneCameraAtFixedRate()
        {
            // Infinite loop to continuously render the camera at the desired frame rate
            while (true)
            {
                // Render the phone camera manually
                phoneCamera.Render();

                // Wait for the next frame interval before rendering again
                yield return new WaitForSeconds(phoneCameraFrameInterval);
            }
        }
    }
}
