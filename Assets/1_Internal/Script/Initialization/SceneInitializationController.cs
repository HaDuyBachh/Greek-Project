using UnityEngine;

[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
public sealed class SceneInitializationController : MonoBehaviour
{
    [Header("Controllers")]
    [SerializeField] private KidFocusCameraController kidFocusController;
    [SerializeField] private ChatUIFollowController chatUiController;

    [Header("Cameras")]
    [SerializeField] private Camera overviewCamera;
    [SerializeField] private Camera focusCamera;
    [SerializeField] private bool startInOverview = true;

    [Header("Phone UI")]
    [SerializeField] private Transform phoneScreen;

    private void Awake()
    {
        if (kidFocusController == null)
        {
            Debug.LogError("SceneInitializer requires a KidFocusCameraController reference.", this);
            return;
        }

        kidFocusController.ConfigureSceneReferences(
            overviewCamera,
            focusCamera,
            phoneScreen,
            chatUiController);
    }

    private void Start()
    {
        if (startInOverview && kidFocusController != null)
        {
            kidFocusController.ShowOverview();
        }
    }
}
