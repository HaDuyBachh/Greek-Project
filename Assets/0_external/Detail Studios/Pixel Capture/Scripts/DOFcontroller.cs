using System;
using System.Collections; // Added to support IEnumerator
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using CustomSlider;
namespace CameraFocus
{

    //Simple script to control the aperture of the camera and the autofocus
    public class DOFcontroller : MonoBehaviour
    {
        // Public variables for f-stop settings
        public float FstopMinimum;
        public float FstopMaximum;
        private float CurrentFstop;
        private float SliderChangeFactor;

        // References to other components and UI elements
        public CustomSliderManager customSliderManager;
        public TextMeshProUGUI FstopText;
        public PostProcessVolume MainPostProcessingVolume;
        private DepthOfField depthOfField;

        [Header("AutoFocus")]
        public bool AutoFocusEnabled;
        public Transform PhoneRenderingCamera;
        public float FocusSmoothingSpeed = 1f; // Speed at which the focus adjusts to the new distance

        private float FocusDistance;
        private float targetFocusDistance;
        private Coroutine autoFocusCoroutine;

        // Start is called before the first frame update
        private void Start()
        {
            if (MainPostProcessingVolume == null)
            {
                MainPostProcessingVolume = GetComponent<PostProcessVolume>();
            }

            // Subscribe to the slider value changed event
            customSliderManager.OnValueChanged += OnSliderValueChanged;

            // Get DepthOfField component from the PostProcessVolume
            depthOfField = MainPostProcessingVolume.profile.GetSetting<DepthOfField>();

            // Check if DepthOfField component is found
            if (depthOfField == null)
            {
                Debug.LogError("Depth Of Field is not found in the Post Process Volume!");
                enabled = false;
                return;
            }
        }

        // Called when the script is destroyed
        private void OnDestroy()
        {
            // Unsubscribe from the slider value changed event
            customSliderManager.OnValueChanged -= OnSliderValueChanged;
        }

        // Method to handle slider value changes
        private void OnSliderValueChanged(float value)
        {
            // Calculate and set the current f-stop value based on the slider value
            Debug.Log("Slider value changed: " + value);
            SliderChangeFactor = value / 100;
            CurrentFstop = FstopMinimum + ((FstopMaximum - FstopMinimum) * SliderChangeFactor);
            Debug.Log("Changed F/stop to : " + CurrentFstop);
            depthOfField.aperture.value = CurrentFstop;

            // Update the f-stop text display
            float FstopRoundedValue = (float)Math.Round(CurrentFstop, 1); // Round to one decimal place
            FstopText.text = "f" + FstopRoundedValue.ToString();
        }

        // Update is called once per frame
        private void Update()
        {
            // Handle auto focus if enabled
            if (AutoFocusEnabled)
            {
                Ray ray = new Ray(PhoneRenderingCamera.position, PhoneRenderingCamera.forward);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    targetFocusDistance = hit.distance;
                    if (autoFocusCoroutine == null)
                    {
                        autoFocusCoroutine = StartCoroutine(SmoothFocusAdjustment());
                    }
                    Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.green);
                }
                else
                {
                    Debug.DrawRay(ray.origin, ray.direction * 1000, Color.red);
                }
            }
        }

        // Coroutine to smoothly adjust the focus distance
        private IEnumerator SmoothFocusAdjustment()
        {
            while (Math.Abs(FocusDistance - targetFocusDistance) > 0.01f)
            {
                // Smoothly interpolate focus distance
                FocusDistance = Mathf.Lerp(FocusDistance, targetFocusDistance, FocusSmoothingSpeed * Time.deltaTime);
                depthOfField.focusDistance.value = FocusDistance;
                yield return null;
            }
            // Set final focus distance
            FocusDistance = targetFocusDistance;
            depthOfField.focusDistance.value = FocusDistance;
            autoFocusCoroutine = null;
        }

        // Draw gizmos in the editor to visualize the focus distance
        private void OnDrawGizmos()
        {
            if (AutoFocusEnabled && PhoneRenderingCamera != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(PhoneRenderingCamera.position, PhoneRenderingCamera.position + PhoneRenderingCamera.forward * FocusDistance);
            }
        }
    }
}
