using UnityEngine;

[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
public sealed class SceneInitializationController : MonoBehaviour
{
    [Header("Controllers")]
    [SerializeField] private KidFocusCameraController kidFocusController;

    [Header("Startup")]
    [SerializeField] private bool startInOverview = true;

    private void Start()
    {
        if (startInOverview && kidFocusController != null)
        {
            kidFocusController.ShowOverview();
        }
    }
}
