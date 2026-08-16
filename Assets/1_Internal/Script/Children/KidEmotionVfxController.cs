using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class KidEmotionVfxController : MonoBehaviour
{
    private enum DisplayCamera
    {
        None,
        MainRoom,
        KidFocus
    }

    [Serializable]
    public sealed class EmotionVfxPool
    {
        public KidWaypointAnimationTester.EmotionState emotion;
        public GameObject[] mainRoomVariants;
        public GameObject[] kidFocusVariants;
    }

    [Header("Kid State")]
    [SerializeField] private string kidId = "Kid1";
    [SerializeField] private KidWaypointAnimationTester activityController;
    [SerializeField] private KidFocusCameraController kidFocusController;
    [SerializeField] private TelevisionFocusCameraController televisionFocusController;

    [Header("Cameras")]
    [SerializeField] private Camera mainRoomCamera;
    [SerializeField] private Camera kidFocusCamera;
    [SerializeField] private Camera televisionCamera;

    [Header("Prebuilt VFX Anchors")]
    [SerializeField, Tooltip("Stable position at the top of the Kid's head. Display anchors are placed from this root toward the active camera.")]
    private Transform emotionHeadRoot;
    [SerializeField] private Transform mainRoomAnchor;
    [SerializeField] private Transform kidFocusAnchor;

    [Header("Preserve Scene Pose")]
    [SerializeField, Tooltip("Keep each emotion anchor at its pre-Play world offset and rotation while following the head root position.")]
    private bool preserveScenePose = true;

    [Header("Prebuilt Emotion Pools")]
    [SerializeField] private EmotionVfxPool[] emotionPools;

    [Header("Display Timing")]
    [SerializeField] private bool showImmediatelyWhenNegative = true;
    [SerializeField] private bool showImmediatelyWhenPositive;
    [SerializeField] private bool showImmediatelyWhenSuspicious = true;
    [SerializeField, Tooltip("Restart Suspicious VFX immediately with no hidden gap while the harmful video remains unresolved.")]
    private bool keepSuspiciousVfxContinuous = true;
    [SerializeField, Min(0.1f)] private float displayDurationSeconds = 3f;
    [Tooltip("Repeat interval for Stable and other non-negative, non-Happy states.")]
    [SerializeField, Min(0.1f)] private float positiveMinimumRepeatIntervalSeconds = 5f;
    [SerializeField, Min(0.1f)] private float positiveMaximumRepeatIntervalSeconds = 7f;
    [SerializeField, Min(0.1f)] private float happyMinimumRepeatIntervalSeconds = 5f;
    [SerializeField, Min(0.1f)] private float happyMaximumRepeatIntervalSeconds = 7f;
    [SerializeField, Min(0.1f)] private float negativeMinimumRepeatIntervalSeconds = 3f;
    [SerializeField, Min(0.1f)] private float negativeMaximumRepeatIntervalSeconds = 5f;
    [SerializeField] private bool avoidImmediateVariantRepeat = true;

    private DisplayCamera activeCamera;
    private KidWaypointAnimationTester.EmotionState displayedEmotion;
    private GameObject activeVfx;
    private float hideAtTime;
    private float showAgainAtTime;
    private int previousMainRoomVariant = -1;
    private int previousKidFocusVariant = -1;
    private Vector3 mainRoomSceneWorldOffset;
    private Vector3 kidFocusSceneWorldOffset;
    private Quaternion mainRoomSceneWorldRotation;
    private Quaternion kidFocusSceneWorldRotation;

    private void Awake()
    {
        ValidatePrebuiltReferences();
        HideAllPrebuiltVfx();
        CaptureSceneAnchorPoses();
        displayedEmotion = activityController != null
            ? activityController.VisualEmotion
            : KidWaypointAnimationTester.EmotionState.Stable;
        ScheduleNextDisplay();
    }

    private void LateUpdate()
    {
        DisplayCamera nextCamera = ResolveDisplayCamera();
        KidWaypointAnimationTester.EmotionState nextEmotion = activityController != null
            ? activityController.VisualEmotion
            : KidWaypointAnimationTester.EmotionState.Stable;

        PreserveSceneAnchorPoses();

        bool cameraChanged = nextCamera != activeCamera;
        bool emotionChanged = nextEmotion != displayedEmotion;
        if (cameraChanged || emotionChanged)
        {
            HideActiveVfx();
            activeCamera = nextCamera;
            displayedEmotion = nextEmotion;

            bool isSuspicious = displayedEmotion == KidWaypointAnimationTester.EmotionState.Suspicious;
            bool isNegative = IsNegative(displayedEmotion);
            bool showImmediately = isSuspicious
                ? showImmediatelyWhenSuspicious
                : isNegative
                    ? showImmediatelyWhenNegative
                    : showImmediatelyWhenPositive;
            if (activeCamera != DisplayCamera.None && showImmediately)
            {
                ShowCurrentEmotion();
            }
            else
            {
                ScheduleNextDisplay();
            }
        }

        if (activeCamera == DisplayCamera.None)
        {
            HideActiveVfx();
            return;
        }

        if (activeVfx != null && Time.time >= hideAtTime)
        {
            bool restartSuspiciousImmediately = keepSuspiciousVfxContinuous &&
                                                displayedEmotion ==
                                                KidWaypointAnimationTester.EmotionState.Suspicious;
            HideActiveVfx();
            if (restartSuspiciousImmediately)
            {
                ShowCurrentEmotion();
            }
        }

        if (activeVfx == null && Time.time >= showAgainAtTime)
        {
            ShowCurrentEmotion();
        }
    }

    private void OnDisable()
    {
        HideAllPrebuiltVfx();
        activeVfx = null;
        activeCamera = DisplayCamera.None;
    }

    [ContextMenu("Preview Current Emotion VFX")]
    public void PreviewCurrentEmotionVfx()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        HideActiveVfx();
        activeCamera = ResolveDisplayCamera();
        displayedEmotion = activityController != null
            ? activityController.VisualEmotion
            : KidWaypointAnimationTester.EmotionState.Stable;
        ShowCurrentEmotion();
    }

    private DisplayCamera ResolveDisplayCamera()
    {
        bool televisionIsVisible = (televisionFocusController != null && televisionFocusController.IsFocusing) ||
                                   IsCameraActive(televisionCamera);
        if (televisionIsVisible)
        {
            return DisplayCamera.None;
        }

        if (IsCameraActive(kidFocusCamera) && kidFocusController != null &&
            string.Equals(kidFocusController.SelectedKidId, kidId, StringComparison.OrdinalIgnoreCase))
        {
            return DisplayCamera.KidFocus;
        }

        return IsCameraActive(mainRoomCamera) ? DisplayCamera.MainRoom : DisplayCamera.None;
    }

    private void CaptureSceneAnchorPoses()
    {
        if (emotionHeadRoot == null)
        {
            return;
        }

        CaptureAnchorScenePose(mainRoomAnchor, out mainRoomSceneWorldOffset,
            out mainRoomSceneWorldRotation);
        CaptureAnchorScenePose(kidFocusAnchor, out kidFocusSceneWorldOffset,
            out kidFocusSceneWorldRotation);
    }

    private void PreserveSceneAnchorPoses()
    {
        if (!preserveScenePose || emotionHeadRoot == null)
        {
            return;
        }

        ApplyAnchorScenePose(mainRoomAnchor, mainRoomSceneWorldOffset, mainRoomSceneWorldRotation);
        ApplyAnchorScenePose(kidFocusAnchor, kidFocusSceneWorldOffset, kidFocusSceneWorldRotation);
    }

    private void CaptureAnchorScenePose(Transform displayAnchor, out Vector3 worldOffset,
        out Quaternion worldRotation)
    {
        if (displayAnchor == null)
        {
            worldOffset = Vector3.zero;
            worldRotation = Quaternion.identity;
            return;
        }

        worldOffset = displayAnchor.position - emotionHeadRoot.position;
        worldRotation = displayAnchor.rotation;
    }

    private void ApplyAnchorScenePose(Transform displayAnchor, Vector3 worldOffset,
        Quaternion worldRotation)
    {
        if (displayAnchor == null)
        {
            return;
        }

        displayAnchor.SetPositionAndRotation(emotionHeadRoot.position + worldOffset, worldRotation);
    }

    private void ShowCurrentEmotion()
    {
        if (activeCamera == DisplayCamera.None)
        {
            return;
        }

        EmotionVfxPool pool = FindPool(displayedEmotion);
        GameObject[] variants = activeCamera == DisplayCamera.MainRoom
            ? pool?.mainRoomVariants
            : pool?.kidFocusVariants;
        if (variants == null || variants.Length == 0)
        {
            ScheduleNextDisplay();
            return;
        }

        int previousIndex = activeCamera == DisplayCamera.MainRoom
            ? previousMainRoomVariant
            : previousKidFocusVariant;
        int selectedIndex = PickVariantIndex(variants, previousIndex);
        if (selectedIndex < 0)
        {
            ScheduleNextDisplay();
            return;
        }

        activeVfx = variants[selectedIndex];
        activeVfx.SetActive(false);
        activeVfx.SetActive(true);
        hideAtTime = Time.time + displayDurationSeconds;
        ScheduleNextDisplay();

        if (activeCamera == DisplayCamera.MainRoom)
        {
            previousMainRoomVariant = selectedIndex;
        }
        else
        {
            previousKidFocusVariant = selectedIndex;
        }
    }

    private EmotionVfxPool FindPool(KidWaypointAnimationTester.EmotionState emotion)
    {
        if (emotionPools == null)
        {
            return null;
        }

        foreach (EmotionVfxPool pool in emotionPools)
        {
            if (pool != null && pool.emotion == emotion)
            {
                return pool;
            }
        }

        return null;
    }

    private int PickVariantIndex(GameObject[] variants, int previousIndex)
    {
        int validCount = 0;
        for (int i = 0; i < variants.Length; i++)
        {
            if (variants[i] != null)
            {
                validCount++;
            }
        }

        if (validCount == 0)
        {
            return -1;
        }

        int target = UnityEngine.Random.Range(0, validCount);
        if (avoidImmediateVariantRepeat && validCount > 1)
        {
            int previousValidOrder = GetValidOrder(variants, previousIndex);
            if (target == previousValidOrder)
            {
                target = (target + UnityEngine.Random.Range(1, validCount)) % validCount;
            }
        }

        for (int i = 0; i < variants.Length; i++)
        {
            if (variants[i] == null)
            {
                continue;
            }

            if (target == 0)
            {
                return i;
            }

            target--;
        }

        return -1;
    }

    private static int GetValidOrder(GameObject[] variants, int index)
    {
        if (index < 0 || index >= variants.Length || variants[index] == null)
        {
            return -1;
        }

        int order = 0;
        for (int i = 0; i < index; i++)
        {
            if (variants[i] != null)
            {
                order++;
            }
        }

        return order;
    }

    private void ScheduleNextDisplay()
    {
        bool isNegative = IsNegative(displayedEmotion);
        bool isHappy = displayedEmotion == KidWaypointAnimationTester.EmotionState.Happy;
        float minimum = Mathf.Max(0.1f,
            isNegative
                ? negativeMinimumRepeatIntervalSeconds
                : isHappy
                    ? happyMinimumRepeatIntervalSeconds
                    : positiveMinimumRepeatIntervalSeconds);
        float maximum = Mathf.Max(minimum,
            isNegative
                ? negativeMaximumRepeatIntervalSeconds
                : isHappy
                    ? happyMaximumRepeatIntervalSeconds
                    : positiveMaximumRepeatIntervalSeconds);
        showAgainAtTime = Time.time + UnityEngine.Random.Range(minimum, maximum);
    }

    private static bool IsNegative(KidWaypointAnimationTester.EmotionState emotion)
    {
        return emotion == KidWaypointAnimationTester.EmotionState.Anxious ||
               emotion == KidWaypointAnimationTester.EmotionState.Panic;
    }

    private void HideActiveVfx()
    {
        if (activeVfx != null)
        {
            activeVfx.SetActive(false);
            activeVfx = null;
        }
    }

    private void HideAllPrebuiltVfx()
    {
        if (emotionPools == null)
        {
            return;
        }

        foreach (EmotionVfxPool pool in emotionPools)
        {
            if (pool == null)
            {
                continue;
            }

            SetAllInactive(pool.mainRoomVariants);
            SetAllInactive(pool.kidFocusVariants);
        }
    }

    private static void SetAllInactive(GameObject[] variants)
    {
        if (variants == null)
        {
            return;
        }

        foreach (GameObject variant in variants)
        {
            if (variant != null && variant.activeSelf)
            {
                variant.SetActive(false);
            }
        }
    }

    private static bool IsCameraActive(Camera cameraToCheck)
    {
        return cameraToCheck != null && cameraToCheck.gameObject.activeInHierarchy;
    }

    private void ValidatePrebuiltReferences()
    {
        if (string.IsNullOrWhiteSpace(kidId) || activityController == null || kidFocusController == null ||
            televisionFocusController == null || mainRoomCamera == null || kidFocusCamera == null ||
            televisionCamera == null || emotionHeadRoot == null || mainRoomAnchor == null || kidFocusAnchor == null)
        {
            Debug.LogError("Kid Emotion VFX requires all Kid, camera, controller and anchor references assigned before Play.", this);
        }

        foreach (KidWaypointAnimationTester.EmotionState emotion in
                 Enum.GetValues(typeof(KidWaypointAnimationTester.EmotionState)))
        {
            EmotionVfxPool pool = FindPool(emotion);
            if (pool == null || !HasVariant(pool.mainRoomVariants) || !HasVariant(pool.kidFocusVariants))
            {
                Debug.LogError($"Kid Emotion VFX requires Main_room and Kid_Forcus variants for {emotion} before Play.", this);
            }
        }
    }

    private static bool HasVariant(GameObject[] variants)
    {
        if (variants == null)
        {
            return false;
        }

        foreach (GameObject variant in variants)
        {
            if (variant != null)
            {
                return true;
            }
        }

        return false;
    }
}
