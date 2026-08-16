using System;
using GreekProject.Content;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class KidFeedCycleController : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private VideoLibraryData videoLibrary;
    [SerializeField] private KidWaypointAnimationTester activityController;
    [SerializeField] private KidFocusCameraController kidFocusController;
    [SerializeField] private KidDeviceUsageController deviceUsageController;

    [Header("Kid")]
    [SerializeField] private string kidId = "Kid1";
    [SerializeField] private bool startOnPlay = true;
    [SerializeField] private bool pauseWhileKidIsFocused = true;

    [Header("Sequential Video Viewing")]
    [SerializeField, Min(0)] private int firstVideoIndex;
    [SerializeField] private bool loopLibrary = true;
    [SerializeField] private bool requireWatchingAnimation;
    [SerializeField] private string[] watchingAnimations =
    {
        "SitGroundUsingPhone",
        "SitChairUsingPhone",
        "SitChairIdle"
    };

    [Header("Watch Duration")]
    [SerializeField] private bool useVideoMetadataDuration = true;
    [SerializeField, Min(0.1f)] private float minimumWatchSeconds = 2f;
    [SerializeField, Min(0.1f)] private float fallbackWatchSeconds = 6f;

    [Header("Pre-watch Suspicion")]
    [SerializeField] private bool showSuspicionBeforeEveryVideo = true;
    [SerializeField, Min(0.1f)] private float minimumSuspicionSeconds = 2f;
    [SerializeField, Min(0.1f)] private float maximumSuspicionSeconds = 4f;

    [Header("Harmful Content Delay")]
    [SerializeField, Min(0.1f), Tooltip("Horror becomes Panic only after this much actual consumption, never during suspicion.")]
    private float horrorConsumptionSecondsBeforeEffect = 3f;

    private int currentVideoIndex;
    private float watchedSeconds;
    private float requiredWatchSeconds;
    private float suspicionSeconds;
    private float requiredSuspicionSeconds;
    private bool currentVideoStarted;
    private bool suspicionCompleted;
    private bool currentEffectApplied;

    public int CurrentVideoIndex => currentVideoIndex;
    public VideoLibraryData.VideoEntry CurrentVideo => GetCurrentVideo();
    public bool IsWatching => currentVideoStarted && CanWatchNow();

    private void Start()
    {
        ValidateSceneReferences();
        ResetSequence();
    }

    private void Update()
    {
        if (!startOnPlay)
        {
            SetSuspicionVisual(false);
            return;
        }

        bool canWatchNow = CanWatchNow();
        SetSuspicionVisual(currentVideoStarted && !suspicionCompleted && CanShowSuspicionNow());
        if (!canWatchNow)
        {
            return;
        }

        VideoLibraryData.VideoEntry video = GetCurrentVideo();
        if (video == null)
        {
            return;
        }

        if (!currentVideoStarted)
        {
            currentVideoStarted = true;
            requiredWatchSeconds = ResolveWatchSeconds(video);
            watchedSeconds = 0f;
            suspicionSeconds = 0f;
            requiredSuspicionSeconds = ResolveSuspicionSeconds();
            suspicionCompleted = !showSuspicionBeforeEveryVideo;
            currentEffectApplied = false;
            SetSuspicionVisual(!suspicionCompleted);
        }

        if (!suspicionCompleted)
        {
            suspicionSeconds += Time.deltaTime;
            if (suspicionSeconds < requiredSuspicionSeconds)
            {
                return;
            }

            suspicionCompleted = true;
            SetSuspicionVisual(false);
            return;
        }

        watchedSeconds += Time.deltaTime;
        if (video.contentEffect == VideoContentEffect.Horror &&
            watchedSeconds >= Mathf.Min(requiredWatchSeconds,
                Mathf.Max(0.1f, horrorConsumptionSecondsBeforeEffect)))
        {
            ApplyCurrentEffect(video);
        }

        if (watchedSeconds < requiredWatchSeconds)
        {
            return;
        }

        ApplyCurrentEffect(video);
        AdvanceToNextVideo();
    }

    [ContextMenu("Reset Sequential Video List")]
    public void ResetSequence()
    {
        int count = videoLibrary != null && videoLibrary.Videos != null
            ? videoLibrary.Videos.Count
            : 0;
        currentVideoIndex = count > 0 ? Mathf.Clamp(firstVideoIndex, 0, count - 1) : 0;
        currentVideoStarted = false;
        suspicionCompleted = false;
        currentEffectApplied = false;
        watchedSeconds = 0f;
        suspicionSeconds = 0f;
        requiredWatchSeconds = 0f;
        requiredSuspicionSeconds = 0f;
        SetSuspicionVisual(false);
    }

    private bool CanWatchNow()
    {
        if (videoLibrary == null || activityController == null || videoLibrary.Videos.Count == 0)
        {
            return false;
        }

        if (deviceUsageController == null ||
            (!deviceUsageController.IsWatchingPhone && !deviceUsageController.IsWatchingTelevision))
        {
            return false;
        }

        if (pauseWhileKidIsFocused && kidFocusController != null &&
            string.Equals(kidFocusController.SelectedKidId, kidId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!requireWatchingAnimation)
        {
            return activityController.IsAtVideoViewingLocation;
        }

        string currentAnimation = activityController.CurrentAnimationState;
        if (watchingAnimations == null)
        {
            return false;
        }

        foreach (string animationName in watchingAnimations)
        {
            if (!string.IsNullOrWhiteSpace(animationName) &&
                string.Equals(animationName, currentAnimation, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private bool CanShowSuspicionNow()
    {
        if (activityController == null || deviceUsageController == null ||
            (!deviceUsageController.IsWatchingPhone && !deviceUsageController.IsWatchingTelevision))
        {
            return false;
        }

        if (!requireWatchingAnimation)
        {
            return activityController.IsAtVideoViewingLocation;
        }

        string currentAnimation = activityController.CurrentAnimationState;
        if (watchingAnimations == null)
        {
            return false;
        }

        foreach (string animationName in watchingAnimations)
        {
            if (!string.IsNullOrWhiteSpace(animationName) &&
                string.Equals(animationName, currentAnimation, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private VideoLibraryData.VideoEntry GetCurrentVideo()
    {
        if (videoLibrary == null || videoLibrary.Videos == null ||
            currentVideoIndex < 0 || currentVideoIndex >= videoLibrary.Videos.Count)
        {
            return null;
        }

        return videoLibrary.Videos[currentVideoIndex];
    }

    private void ApplyCurrentEffect(VideoLibraryData.VideoEntry video)
    {
        if (currentEffectApplied || video == null || activityController == null)
        {
            return;
        }

        currentEffectApplied = true;
        activityController.ApplyViewedVideoEffect(video.contentEffect);
    }

    private void AdvanceToNextVideo()
    {
        int count = videoLibrary != null ? videoLibrary.Videos.Count : 0;
        if (count == 0)
        {
            return;
        }

        if (currentVideoIndex + 1 < count)
        {
            currentVideoIndex++;
        }
        else if (loopLibrary)
        {
            currentVideoIndex = 0;
        }
        else
        {
            startOnPlay = false;
        }

        currentVideoStarted = false;
        suspicionCompleted = false;
        currentEffectApplied = false;
        watchedSeconds = 0f;
        suspicionSeconds = 0f;
        requiredSuspicionSeconds = 0f;
        SetSuspicionVisual(false);
    }

    private float ResolveWatchSeconds(VideoLibraryData.VideoEntry video)
    {
        if (useVideoMetadataDuration && video != null && TryParseDuration(video.duration, out float seconds))
        {
            return Mathf.Max(minimumWatchSeconds, seconds);
        }

        return Mathf.Max(minimumWatchSeconds, fallbackWatchSeconds);
    }

    private float ResolveSuspicionSeconds()
    {
        float minimum = Mathf.Max(0.1f, minimumSuspicionSeconds);
        float maximum = Mathf.Max(minimum, maximumSuspicionSeconds);
        return UnityEngine.Random.Range(minimum, maximum);
    }

    private void SetSuspicionVisual(bool shouldShow)
    {
        if (activityController != null)
        {
            activityController.SetVideoSuspicion(shouldShow);
        }
    }

    private void OnDisable()
    {
        SetSuspicionVisual(false);
    }

    private static bool TryParseDuration(string value, out float seconds)
    {
        seconds = 0f;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string[] parts = value.Split(':');
        if (parts.Length != 2 || !int.TryParse(parts[0], out int minutes) ||
            !int.TryParse(parts[1], out int remainingSeconds))
        {
            return false;
        }

        seconds = Mathf.Max(0, minutes) * 60f + Mathf.Max(0, remainingSeconds);
        return seconds > 0f;
    }

    private void ValidateSceneReferences()
    {
        if (videoLibrary == null || activityController == null || kidFocusController == null ||
            deviceUsageController == null)
        {
            Debug.LogError("Kid Sequential Video Viewer requires its library, Kid activity and focus controller assigned before Play.", this);
        }
    }

    private void OnValidate()
    {
        minimumWatchSeconds = Mathf.Max(0.1f, minimumWatchSeconds);
        fallbackWatchSeconds = Mathf.Max(minimumWatchSeconds, fallbackWatchSeconds);
        minimumSuspicionSeconds = Mathf.Max(0.1f, minimumSuspicionSeconds);
        maximumSuspicionSeconds = Mathf.Max(minimumSuspicionSeconds, maximumSuspicionSeconds);
        horrorConsumptionSecondsBeforeEffect = Mathf.Max(0.1f, horrorConsumptionSecondsBeforeEffect);
    }
}
