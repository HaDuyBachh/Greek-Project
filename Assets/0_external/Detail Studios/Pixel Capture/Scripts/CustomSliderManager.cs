using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Custom slider script to control the aperture of the camera
namespace CustomSlider
{

    [ExecuteInEditMode]
    public class CustomSliderManager : MonoBehaviour
    {
        // Delegate for value change event
        public delegate void ValueChangedEventHandler(float value);
        public event ValueChangedEventHandler OnValueChanged;

        // List to hold instantiated notch GameObjects
        public List<GameObject> InstantiatedNotches = new List<GameObject>();
        private int NotchInListComparedToVariable;
        public int NotchAmount;

        // References to various UI elements
        public GameObject Background, HorizontalLayoutGroup, Notch;
        RectTransform BackgroundRT, HorizontalLayoutGroupRT, NotchRT;
        float Backgroundwidth, HLGspacing, DesiredHorzizontalLayoutGroupWidth, NotchWidth, WidthFromMinToMaxValue, HorizontalLayoutGroupPosX;
        HorizontalOrVerticalLayoutGroup horizontalLayoutGroupComponent;
        private float previousValue = float.MinValue;

    

        // Update is called once per frame
        void Update()
        {
            // Check if the number of instantiated notches matches the desired amount
            NotchInListComparedToVariable = NotchAmount - InstantiatedNotches.Count;
            if (NotchInListComparedToVariable != 0)
            {
                RefreshNotches(NotchInListComparedToVariable);
            }

            // Get the HorizontalLayoutGroup component
            horizontalLayoutGroupComponent = HorizontalLayoutGroup.GetComponent<HorizontalLayoutGroup>();
            HorizontalLayoutGroupRT = HorizontalLayoutGroup.GetComponent<RectTransform>();

            // Set the width of the horizontal layout group
            SetHorizontalGroupWidth();

            // Calculate the width from min to max value
            WidthFromMinToMaxValue = (DesiredHorzizontalLayoutGroupWidth - Backgroundwidth) / 2;
            float sliderX = HorizontalLayoutGroupRT.anchoredPosition.x + WidthFromMinToMaxValue;

            // Calculate slider position from right to left
            sliderX = (WidthFromMinToMaxValue * 2) - sliderX;

            // Convert slider position to percentage
            int PercentageConversion = Mathf.RoundToInt(sliderX / (WidthFromMinToMaxValue * 2) * 100);
            PercentageConversion = Mathf.Clamp(PercentageConversion, 0, 100);

            // Trigger OnValueChanged event if the value has changed
            if (Mathf.Abs(previousValue - PercentageConversion) > 0.01f)
            {
                OnValueChanged?.Invoke(PercentageConversion);
                previousValue = PercentageConversion;
            }

        }

        // Refreshes the notches based on the difference between the desired and current amount
        public void RefreshNotches(int NILCTV)
        {
            // Add notches if the difference is positive
            if (NILCTV > 0)
            {
                for (int i = 0; i < NILCTV; i++)
                {
                    InstantiatedNotches.Add((GameObject)Instantiate(Notch, HorizontalLayoutGroup.transform));
                }
            }

            // Remove notches if the difference is negative
            if (NILCTV < 0)
            {
                for (int i = 0; i < (NILCTV * -1); i++)
                {
                    DestroyImmediate(InstantiatedNotches[InstantiatedNotches.Count - 1]);
                    InstantiatedNotches.RemoveAt(InstantiatedNotches.Count - 1);
                }
            }

            // Set the width of the horizontal layout group
            SetHorizontalGroupWidth();
        }

        // Sets the width of the horizontal layout group based on the number of notches and their spacing
        public void SetHorizontalGroupWidth()
        {
            // Get components
            horizontalLayoutGroupComponent = HorizontalLayoutGroup.GetComponent<HorizontalLayoutGroup>();
            BackgroundRT = Background.GetComponent<RectTransform>();
            NotchRT = InstantiatedNotches[0].GetComponent<RectTransform>();
            HorizontalLayoutGroupRT = HorizontalLayoutGroup.GetComponent<RectTransform>();

            // Calculate dimensions
            Backgroundwidth = BackgroundRT.sizeDelta.x;
            HLGspacing = horizontalLayoutGroupComponent.spacing;
            NotchWidth = NotchRT.sizeDelta.x;
            DesiredHorzizontalLayoutGroupWidth = Backgroundwidth + (HLGspacing + NotchWidth) * (NotchAmount - 1);

            // Set the width of the horizontal layout group
            HorizontalLayoutGroupRT.sizeDelta = new Vector2(DesiredHorzizontalLayoutGroupWidth, HorizontalLayoutGroupRT.sizeDelta.y);
        }
    }
}
