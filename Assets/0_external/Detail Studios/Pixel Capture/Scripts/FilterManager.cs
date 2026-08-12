using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using TMPro; // Include this to work with TextMeshPro
namespace FiltersAndPostProcessing
{
    public class FilterManager : MonoBehaviour
    {
        [Header("Add your filter profiles here")]
        public PostProcessProfile[] FilterProfiles;
        [Header("The base profile that color profiles will be added to")]
        public PostProcessVolume FilterPostvolme;
        [Header("Here is where you add the filter text object that shows which filter is selected (TMP)")]
        public GameObject FilterText;
        int SelectedFilter;
        private Coroutine textCoroutine;

        private void Start()
        {
            FilterText.SetActive(false);
            // Ensure the CameraPostVolume is assigned
            if (FilterPostvolme == null)
            {
                FilterPostvolme = gameObject.GetComponent<PostProcessVolume>();
            }
            else
            {
                Debug.LogError("CameraPostVolume is not set!");
            }

            // Load all filter profiles from the specified directory
            FilterProfiles = Resources.LoadAll<PostProcessProfile>("Color Filters/Color grades");

            if (FilterProfiles.Length == 0)
            {
                Debug.LogError("No filter profiles found in the specified directory!");
            }
        }

        public void CycleFilterProfiles()
        {
            if (FilterProfiles == null || FilterProfiles.Length == 0)
            {
                Debug.LogError("FilterProfiles array is empty or null.");
                return;
            }

            if (SelectedFilter >= FilterProfiles.Length)
            {
                SelectedFilter = 0; // Reset to the first filter if we've cycled through all filters
            }

            FilterPostvolme.profile = FilterProfiles[SelectedFilter];
            Debug.Log("Filter changed to: " + FilterProfiles[SelectedFilter].name);

            // Update the filter text
            if (textCoroutine != null)
            {
                StopCoroutine(textCoroutine);
            }
            textCoroutine = StartCoroutine(UpdateFilterText(FilterProfiles[SelectedFilter].name));

            // Increment the selected filter index
            SelectedFilter++;
        }

        private IEnumerator UpdateFilterText(string filterName)
        {
            // Enable the text object
            FilterText.SetActive(true);

            // Get the TextMeshPro component and update the text
            TextMeshProUGUI tmp = FilterText.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = filterName;
            }
            else
            {
                Debug.LogError("No TextMeshProUGUI component found on FilterText object.");
            }

            // Wait for 0.2 seconds
            yield return new WaitForSeconds(0.3f);

            // Disable the text object
            FilterText.SetActive(false);
        }
    }
}
