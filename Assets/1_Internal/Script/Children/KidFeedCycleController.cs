using System;
using System.Collections.Generic;
using GreekProject.Content;
using GreekProject.UI;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public sealed class KidFeedCycleController : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private VideoLibraryData videoLibrary;
    [SerializeField] private KidWaypointAnimationTester activityController;
    [SerializeField] private KidFocusCameraController kidFocusController;
    [SerializeField] private KidDeviceUsageController deviceUsageController;

    [Header("Device-specific Hidden Videos")]
    [SerializeField, FormerlySerializedAs("phoneFeed"),
     Tooltip("Shared camera-facing Phone UI presenter only. This Kid owns its feed, blacklist and timer below.")]
    private PhoneVideoFeedUI phonePresenter;
    [SerializeField, Tooltip("TV blacklist. Assign only for Kids that can watch Television.")]
    private TelevisionVideoFeedUI televisionFeed;
    [SerializeField, Tooltip("Skip a video before it can apply an effect when it is hidden on the device currently being watched.")]
    private bool skipVideosHiddenOnCurrentDevice = true;

    [Header("Independent Phone Feed")]
    [SerializeField, Min(1)] private int phoneVisibleVideoCount = 6;
    [SerializeField] private bool randomizeInitialPhoneFeed = true;
    [SerializeField] private bool autoRefreshPhoneFeed = true;
    [SerializeField, Min(0.1f)] private float minimumPhoneRefreshSeconds = 5f;
    [SerializeField, Min(0.1f)] private float maximumPhoneRefreshSeconds = 5f;
    [SerializeField] private bool balancePhoneHarmfulContent = true;
    [SerializeField, Min(1)] private int phoneNormalVideosPerHarmfulVideo = 3;
    [SerializeField, Range(1, 3)] private int maximumPhoneHarmfulVideos = 3;
    [SerializeField, Range(0f, 1f), Tooltip("Chance for a six-card Phone feed to receive one additional harmful video, without exceeding the hard limit.")]
    private float phoneExtraHarmfulVideoChance = 0.4f;

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
    [SerializeField, Min(0.1f),
     Tooltip("A Normal TV broadcast counts as watched after this many seconds, even when its metadata duration is longer.")]
    private float maximumTelevisionNormalWatchSeconds = 9f;

    [Header("Harmful Video Suspicion")]
    [SerializeField, FormerlySerializedAs("showSuspicionBeforeEveryVideo"),
     Tooltip("Show Suspicious only while the tracked video is Brainrot or Horror. Normal videos can never enable this visual state.")]
    private bool showSuspicionBeforeHarmfulVideo = true;
    [SerializeField, Min(0.1f)] private float minimumSuspicionSeconds = 2f;
    [SerializeField, Min(0.1f)] private float maximumSuspicionSeconds = 4f;

    [Header("Harmful Content Delay")]
    [SerializeField, Min(0.1f), Tooltip("Horror becomes Panic only after this much actual consumption, never during suspicion.")]
    private float horrorConsumptionSecondsBeforeEffect = 3f;

    [Header("Harmful Content Intervention")]
    [SerializeField, Tooltip("Give the player time to mark a Brainrot/Horror video as Don't recommend before it counts against the Kid.")]
    private bool useHarmfulInterventionWindow = true;
    [SerializeField, FormerlySerializedAs("panicAfterUnresolvedSuspicion"),
     Tooltip("Keep the same harmful video active and looping throughout its Suspicious intervention window.")]
    private bool loopHarmfulVideoDuringSuspicion = true;
    [SerializeField, Min(1f)] private float minimumHarmfulSuspicionSeconds = 8f;
    [SerializeField, Min(1f)] private float maximumHarmfulSuspicionSeconds = 9f;
    [SerializeField, Tooltip("Inclusive unresolved-video threshold. Scene uses 2-2: the second missed harmful video causes Panic.")]
    private Vector2Int harmfulVideosBeforeNegativeRange = new Vector2Int(2, 2);
    [SerializeField, Min(1), Tooltip("Total fully watched Normal videos required to clear harmful progress back to 0/2.")]
    private int normalVideosToClearHarmfulCounter = 8;

    private int currentVideoIndex;
    private float watchedSeconds;
    private float requiredWatchSeconds;
    private float suspicionSeconds;
    private float requiredSuspicionSeconds;
    private bool currentVideoStarted;
    private bool suspicionCompleted;
    private bool currentEffectApplied;
    private int unresolvedHarmfulVideos;
    private int requiredHarmfulVideosBeforeNegative;
    private int normalVideosTowardHarmfulReset;
    private VideoLibraryData.VideoEntry trackedVideo;
    private VideoLibraryData.VideoEntry completedTelevisionVideo;
    private readonly List<VideoLibraryData.VideoEntry> phoneVisibleVideos = new();
    private readonly HashSet<string> hiddenPhoneVideoIds = new(StringComparer.OrdinalIgnoreCase);
    private bool phoneFeedInitialized;
    private float phoneRefreshElapsedSeconds;
    private float secondsBeforePhoneRefresh;
    private int phoneFeedRevision;

    public int CurrentVideoIndex => currentVideoIndex;
    public VideoLibraryData.VideoEntry CurrentVideo => GetCurrentVideo();
    public bool IsWatching => currentVideoStarted && CanWatchNow();
    public string KidId => kidId;
    public int UnresolvedHarmfulVideos => unresolvedHarmfulVideos;
    public int RequiredHarmfulVideosBeforeNegative => requiredHarmfulVideosBeforeNegative;
    public int NormalVideosTowardHarmfulReset => normalVideosTowardHarmfulReset;
    public IReadOnlyList<VideoLibraryData.VideoEntry> PhoneVisibleVideos => phoneVisibleVideos;
    public int PhoneFeedRevision => phoneFeedRevision;
    public VideoLibraryData.VideoEntry CurrentPhoneVideo
    {
        get
        {
            if (deviceUsageController == null || !deviceUsageController.IsWatchingPhone)
            {
                return null;
            }

            return currentVideoStarted && trackedVideo != null && !IsPhoneVideoHidden(trackedVideo)
                ? trackedVideo
                : GetCurrentVideo();
        }
    }
    public float CurrentPhonePlaybackSeconds => currentVideoStarted && trackedVideo != null &&
                                                !IsPhoneVideoHidden(trackedVideo)
        ? suspicionSeconds + watchedSeconds
        : 0f;

    public void ClearHarmfulProgressAfterGuidedHelp()
    {
        unresolvedHarmfulVideos = 0;
        normalVideosTowardHarmfulReset = 0;
        requiredHarmfulVideosBeforeNegative = ResolveHarmfulVideoThreshold();
        completedTelevisionVideo = null;
        ResetCurrentVideoProgress();
        SetSuspicionVisual(false);
    }

    private void Awake()
    {
        EnsurePhoneFeedInitialized();
    }

    private void Start()
    {
        ValidateSceneReferences();
        ResetSequence();
    }

    private void Update()
    {
        UpdateIndependentPhoneFeed();

        if (!startOnPlay)
        {
            SetSuspicionVisual(false);
            return;
        }

        bool canWatchNow = CanWatchNow();
        SetSuspicionVisual(currentVideoStarted && !suspicionCompleted &&
                           IsHarmful(trackedVideo) && CanShowSuspicionNow());
        if (!canWatchNow)
        {
            return;
        }

        if (!TryGetEligibleCurrentVideo(out VideoLibraryData.VideoEntry video))
        {
            return;
        }

        bool watchingTelevision = deviceUsageController != null &&
                                  deviceUsageController.IsWatchingTelevision;
        if (!watchingTelevision)
        {
            completedTelevisionVideo = null;
        }
        else if (completedTelevisionVideo == video)
        {
            return;
        }
        else
        {
            completedTelevisionVideo = null;
        }

        if (currentVideoStarted && trackedVideo != video)
        {
            ResetCurrentVideoProgress();
        }

        bool isHarmfulVideo = IsHarmful(video);
        if (isHarmfulVideo && activityController.IsProtectedFromHarmfulContent)
        {
            ResetCurrentVideoProgress();
            return;
        }

        if (!currentVideoStarted)
        {
            currentVideoStarted = true;
            trackedVideo = video;
            requiredWatchSeconds = ResolveWatchSeconds(video);
            watchedSeconds = 0f;
            suspicionSeconds = 0f;
            bool requiresHarmfulSuspicion = isHarmfulVideo &&
                                             (showSuspicionBeforeHarmfulVideo ||
                                              useHarmfulInterventionWindow);
            requiredSuspicionSeconds = requiresHarmfulSuspicion
                ? ResolveSuspicionSeconds(video)
                : 0f;
            suspicionCompleted = !requiresHarmfulSuspicion;
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
            if (isHarmfulVideo && useHarmfulInterventionWindow)
            {
                RegisterUnresolvedHarmfulVideo(video);
                CompleteCurrentVideo(video);
            }
            return;
        }

        if (isHarmfulVideo && useHarmfulInterventionWindow)
        {
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

        RegisterCompletedNormalVideo(video);
        ApplyCurrentEffect(video);
        CompleteCurrentVideo(video);
    }

    [ContextMenu("Reset Sequential Video List")]
    public void ResetSequence()
    {
        int count = GetCurrentFeedCount();
        currentVideoIndex = count > 0 ? Mathf.Clamp(firstVideoIndex, 0, count - 1) : 0;
        currentVideoStarted = false;
        suspicionCompleted = false;
        currentEffectApplied = false;
        watchedSeconds = 0f;
        suspicionSeconds = 0f;
        requiredWatchSeconds = 0f;
        requiredSuspicionSeconds = 0f;
        unresolvedHarmfulVideos = 0;
        requiredHarmfulVideosBeforeNegative = ResolveHarmfulVideoThreshold();
        normalVideosTowardHarmfulReset = 0;
        trackedVideo = null;
        completedTelevisionVideo = null;
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
        if (currentVideoStarted && trackedVideo != null && deviceUsageController != null &&
            deviceUsageController.IsWatchingPhone && !IsPhoneVideoHidden(trackedVideo))
        {
            return trackedVideo;
        }

        if (deviceUsageController != null && deviceUsageController.IsWatchingTelevision &&
            televisionFeed != null)
        {
            return televisionFeed.CurrentBroadcastVideo;
        }

        IReadOnlyList<VideoLibraryData.VideoEntry> feed = GetCurrentFeed();
        if (feed == null || currentVideoIndex < 0 || currentVideoIndex >= feed.Count)
        {
            return null;
        }

        return feed[currentVideoIndex];
    }

    private IReadOnlyList<VideoLibraryData.VideoEntry> GetCurrentFeed()
    {
        if (deviceUsageController != null)
        {
            if (deviceUsageController.IsWatchingPhone && phonePresenter != null)
            {
                EnsurePhoneFeedInitialized();
                return phoneVisibleVideos;
            }

            if (deviceUsageController.IsWatchingTelevision && televisionFeed != null)
            {
                return televisionFeed.VisibleVideos;
            }
        }

        return videoLibrary?.Videos;
    }

    private int GetCurrentFeedCount()
    {
        if (deviceUsageController != null && deviceUsageController.IsWatchingTelevision &&
            televisionFeed != null)
        {
            return televisionFeed.CurrentBroadcastVideo != null ? 1 : 0;
        }

        IReadOnlyList<VideoLibraryData.VideoEntry> feed = GetCurrentFeed();
        return feed?.Count ?? 0;
    }

    private bool TryGetEligibleCurrentVideo(out VideoLibraryData.VideoEntry video)
    {
        video = null;
        int count = GetCurrentFeedCount();
        if (count == 0)
        {
            return false;
        }

        for (int inspected = 0; inspected < count; inspected++)
        {
            video = GetCurrentVideo();
            if (video != null && !IsHiddenOnCurrentDevice(video))
            {
                return true;
            }

            ResetCurrentVideoProgress();
            if (!TryAdvanceVideoIndex())
            {
                video = null;
                return false;
            }
        }

        video = null;
        SetSuspicionVisual(false);
        return false;
    }

    private bool IsHiddenOnCurrentDevice(VideoLibraryData.VideoEntry video)
    {
        if (!skipVideosHiddenOnCurrentDevice || video == null || deviceUsageController == null)
        {
            return false;
        }

        if (deviceUsageController.IsWatchingPhone)
        {
            return IsPhoneVideoHidden(video);
        }

        return deviceUsageController.IsWatchingTelevision &&
               televisionFeed != null && televisionFeed.IsVideoHiddenForKid(video);
    }

    private void ApplyCurrentEffect(VideoLibraryData.VideoEntry video)
    {
        if (currentEffectApplied || video == null || activityController == null)
        {
            return;
        }

        if (IsHarmful(video) && activityController.IsProtectedFromHarmfulContent)
        {
            return;
        }

        currentEffectApplied = true;
        activityController.ApplyViewedVideoEffect(video.contentEffect);
    }

    private void RegisterUnresolvedHarmfulVideo(VideoLibraryData.VideoEntry video)
    {
        if (currentEffectApplied || video == null || activityController == null)
        {
            return;
        }

        if (activityController.IsProtectedFromHarmfulContent)
        {
            return;
        }

        currentEffectApplied = true;
        int previousHarmfulVideos = unresolvedHarmfulVideos;
        unresolvedHarmfulVideos = Mathf.Min(
            unresolvedHarmfulVideos + 1, requiredHarmfulVideosBeforeNegative);
        if (previousHarmfulVideos >= requiredHarmfulVideosBeforeNegative ||
            unresolvedHarmfulVideos < requiredHarmfulVideosBeforeNegative)
        {
            return;
        }

        activityController.ApplyUnresolvedHarmfulContentPanic();
    }

    private void RegisterCompletedNormalVideo(VideoLibraryData.VideoEntry video)
    {
        if (video == null || IsHarmful(video) || unresolvedHarmfulVideos <= 0)
        {
            return;
        }

        normalVideosTowardHarmfulReset++;
        if (normalVideosTowardHarmfulReset < normalVideosToClearHarmfulCounter)
        {
            return;
        }

        unresolvedHarmfulVideos = 0;
        normalVideosTowardHarmfulReset = 0;
        requiredHarmfulVideosBeforeNegative = ResolveHarmfulVideoThreshold();
    }

    private void AdvanceToNextVideo()
    {
        int count = GetCurrentFeedCount();
        if (count == 0)
        {
            return;
        }

        if (!TryAdvanceVideoIndex())
        {
            ResetCurrentVideoProgress();
            return;
        }

        ResetCurrentVideoProgress();
    }

    private void CompleteCurrentVideo(VideoLibraryData.VideoEntry video)
    {
        if (deviceUsageController != null && deviceUsageController.IsWatchingTelevision)
        {
            completedTelevisionVideo = video;
            ResetCurrentVideoProgress();
            return;
        }

        AdvanceToNextVideo();
    }

    private bool TryAdvanceVideoIndex()
    {
        int count = GetCurrentFeedCount();
        if (count == 0)
        {
            return false;
        }

        if (currentVideoIndex + 1 < count)
        {
            currentVideoIndex++;
            return true;
        }

        if (loopLibrary)
        {
            currentVideoIndex = 0;
            return true;
        }

        startOnPlay = false;
        return false;
    }

    private void ResetCurrentVideoProgress()
    {
        currentVideoStarted = false;
        suspicionCompleted = false;
        currentEffectApplied = false;
        watchedSeconds = 0f;
        suspicionSeconds = 0f;
        requiredSuspicionSeconds = 0f;
        trackedVideo = null;
        SetSuspicionVisual(false);
    }

    private float ResolveWatchSeconds(VideoLibraryData.VideoEntry video)
    {
        float resolvedSeconds;
        if (useVideoMetadataDuration && video != null && TryParseDuration(video.duration, out float seconds))
        {
            resolvedSeconds = Mathf.Max(minimumWatchSeconds, seconds);
        }
        else
        {
            resolvedSeconds = Mathf.Max(minimumWatchSeconds, fallbackWatchSeconds);
        }

        bool watchingNormalTelevision = video != null && !IsHarmful(video) &&
                                        deviceUsageController != null &&
                                        deviceUsageController.IsWatchingTelevision;
        return watchingNormalTelevision
            ? Mathf.Min(resolvedSeconds, Mathf.Max(0.1f, maximumTelevisionNormalWatchSeconds))
            : resolvedSeconds;
    }

    private float ResolveSuspicionSeconds(VideoLibraryData.VideoEntry video)
    {
        if (loopHarmfulVideoDuringSuspicion && useHarmfulInterventionWindow &&
            IsHarmful(video))
        {
            float harmfulMinimum = Mathf.Max(1f, minimumHarmfulSuspicionSeconds);
            float harmfulMaximum = Mathf.Max(harmfulMinimum, maximumHarmfulSuspicionSeconds);
            return UnityEngine.Random.Range(harmfulMinimum, harmfulMaximum);
        }

        float minimum = Mathf.Max(0.1f, minimumSuspicionSeconds);
        float maximum = Mathf.Max(minimum, maximumSuspicionSeconds);
        return UnityEngine.Random.Range(minimum, maximum);
    }

    private int ResolveHarmfulVideoThreshold()
    {
        int minimum = Mathf.Max(1, harmfulVideosBeforeNegativeRange.x);
        int maximum = Mathf.Max(minimum, harmfulVideosBeforeNegativeRange.y);
        return UnityEngine.Random.Range(minimum, maximum + 1);
    }

    private static bool IsHarmful(VideoLibraryData.VideoEntry video)
    {
        return video != null && video.contentEffect != VideoContentEffect.Normal;
    }

    public void EnsurePhoneFeedInitialized()
    {
        if (phoneFeedInitialized || videoLibrary == null || deviceUsageController == null ||
            !deviceUsageController.CanUsePhone)
        {
            return;
        }

        SelectIndependentPhoneFeed(null, randomizeInitialPhoneFeed);
        phoneFeedInitialized = true;
        ScheduleNextPhoneRefresh();
    }

    public bool IsPhoneVideoHidden(VideoLibraryData.VideoEntry video)
    {
        return video != null && !string.IsNullOrWhiteSpace(video.id) &&
               hiddenPhoneVideoIds.Contains(video.id);
    }

    public void HidePhoneVideo(VideoLibraryData.VideoEntry video)
    {
        if (video == null || string.IsNullOrWhiteSpace(video.id) ||
            !hiddenPhoneVideoIds.Add(video.id))
        {
            return;
        }

        phoneFeedRevision++;
    }

    [ContextMenu("Refresh This Kid Phone Feed")]
    public void RefreshPhoneFeedNow()
    {
        EnsurePhoneFeedInitialized();
        if (!phoneFeedInitialized)
        {
            return;
        }

        List<VideoLibraryData.VideoEntry> previous = new(phoneVisibleVideos);
        SelectIndependentPhoneFeed(previous, true);
        ScheduleNextPhoneRefresh();
    }

    private void UpdateIndependentPhoneFeed()
    {
        EnsurePhoneFeedInitialized();
        if (!phoneFeedInitialized || !autoRefreshPhoneFeed || IsThisKidPhoneOpen())
        {
            return;
        }

        phoneRefreshElapsedSeconds += Time.unscaledDeltaTime;
        if (phoneRefreshElapsedSeconds >= secondsBeforePhoneRefresh)
        {
            RefreshPhoneFeedNow();
        }
    }

    private bool IsThisKidPhoneOpen()
    {
        return kidFocusController != null && kidFocusController.IsPhoneScreenVisible &&
               string.Equals(kidFocusController.SelectedKidId, kidId,
                   StringComparison.OrdinalIgnoreCase);
    }

    private void ScheduleNextPhoneRefresh()
    {
        float minimum = Mathf.Max(0.1f, minimumPhoneRefreshSeconds);
        float maximum = Mathf.Max(minimum, maximumPhoneRefreshSeconds);
        phoneRefreshElapsedSeconds = 0f;
        secondsBeforePhoneRefresh = UnityEngine.Random.Range(minimum, maximum);
    }

    private void SelectIndependentPhoneFeed(
        IList<VideoLibraryData.VideoEntry> previousVideos, bool shuffle)
    {
        phoneVisibleVideos.Clear();
        if (videoLibrary == null)
        {
            return;
        }

        HashSet<VideoLibraryData.VideoEntry> previous = previousVideos != null
            ? new HashSet<VideoLibraryData.VideoEntry>(previousVideos)
            : new HashSet<VideoLibraryData.VideoEntry>();
        List<VideoLibraryData.VideoEntry> freshNormal = new();
        List<VideoLibraryData.VideoEntry> freshHarmful = new();
        List<VideoLibraryData.VideoEntry> fallbackNormal = new();
        List<VideoLibraryData.VideoEntry> fallbackHarmful = new();

        foreach (VideoLibraryData.VideoEntry video in videoLibrary.Videos)
        {
            if (video == null || string.IsNullOrWhiteSpace(video.id) || IsPhoneVideoHidden(video))
            {
                continue;
            }

            bool harmful = IsHarmful(video);
            bool wasPreviouslyVisible = previous.Contains(video);
            List<VideoLibraryData.VideoEntry> destination = harmful
                ? wasPreviouslyVisible ? fallbackHarmful : freshHarmful
                : wasPreviouslyVisible ? fallbackNormal : freshNormal;
            destination.Add(video);
        }

        if (shuffle)
        {
            ShufflePhoneVideos(freshNormal);
            ShufflePhoneVideos(freshHarmful);
            ShufflePhoneVideos(fallbackNormal);
            ShufflePhoneVideos(fallbackHarmful);
        }

        freshNormal.AddRange(fallbackNormal);
        freshHarmful.AddRange(fallbackHarmful);
        int targetCount = Mathf.Min(phoneVisibleVideoCount,
            freshNormal.Count + freshHarmful.Count);
        if (!balancePhoneHarmfulContent)
        {
            freshNormal.AddRange(freshHarmful);
            if (shuffle)
            {
                ShufflePhoneVideos(freshNormal);
            }

            AddPhoneVideos(freshNormal, targetCount);
            phoneFeedRevision++;
            return;
        }

        int ratioSize = Mathf.Max(2, phoneNormalVideosPerHarmfulVideo + 1);
        int desiredHarmful = Mathf.RoundToInt(targetCount / (float)ratioSize);
        desiredHarmful = Mathf.Clamp(desiredHarmful, 1,
            Mathf.Min(maximumPhoneHarmfulVideos, targetCount));
        if (desiredHarmful < Mathf.Min(maximumPhoneHarmfulVideos, targetCount) &&
            UnityEngine.Random.value < phoneExtraHarmfulVideoChance)
        {
            desiredHarmful++;
        }

        AddPhoneVideos(freshNormal, targetCount - desiredHarmful);
        int countBeforeHarmful = phoneVisibleVideos.Count;
        AddPhoneVideos(freshHarmful, desiredHarmful);
        int harmfulAdded = phoneVisibleVideos.Count - countBeforeHarmful;
        if (phoneVisibleVideos.Count < targetCount)
        {
            AddPhoneVideos(freshNormal, targetCount - phoneVisibleVideos.Count);
            int harmfulCapacity = Mathf.Max(0, maximumPhoneHarmfulVideos - harmfulAdded);
            AddPhoneVideos(freshHarmful, Mathf.Min(
                targetCount - phoneVisibleVideos.Count, harmfulCapacity));
        }

        if (shuffle)
        {
            ShufflePhoneVideos(phoneVisibleVideos);
        }

        phoneFeedRevision++;
    }

    private void AddPhoneVideos(List<VideoLibraryData.VideoEntry> source, int maximumToAdd)
    {
        int count = Mathf.Min(maximumToAdd, source.Count);
        for (int index = 0; index < count; index++)
        {
            phoneVisibleVideos.Add(source[index]);
        }

        if (count > 0)
        {
            source.RemoveRange(0, count);
        }
    }

    private static void ShufflePhoneVideos<T>(IList<T> videos)
    {
        for (int index = videos.Count - 1; index > 0; index--)
        {
            int swapIndex = UnityEngine.Random.Range(0, index + 1);
            (videos[index], videos[swapIndex]) = (videos[swapIndex], videos[index]);
        }
    }

    private void SetSuspicionVisual(bool shouldShow)
    {
        if (activityController != null)
        {
            bool isTrackedHarmfulVideo = currentVideoStarted && trackedVideo != null &&
                                         IsHarmful(trackedVideo) &&
                                         !IsHiddenOnCurrentDevice(trackedVideo);
            activityController.SetVideoSuspicion(shouldShow && isTrackedHarmfulVideo);
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

        if (skipVideosHiddenOnCurrentDevice && deviceUsageController != null &&
            ((deviceUsageController.CanUsePhone && phonePresenter == null) ||
             (deviceUsageController.CanUseTelevision && televisionFeed == null)))
        {
            Debug.LogError("Kid Sequential Video Viewer requires its prebuilt Phone presenter and/or TV feed reference for every supported device.", this);
        }
    }

    private void OnValidate()
    {
        minimumWatchSeconds = Mathf.Max(0.1f, minimumWatchSeconds);
        fallbackWatchSeconds = Mathf.Max(minimumWatchSeconds, fallbackWatchSeconds);
        minimumSuspicionSeconds = Mathf.Max(0.1f, minimumSuspicionSeconds);
        maximumSuspicionSeconds = Mathf.Max(minimumSuspicionSeconds, maximumSuspicionSeconds);
        horrorConsumptionSecondsBeforeEffect = Mathf.Max(0.1f, horrorConsumptionSecondsBeforeEffect);
        minimumHarmfulSuspicionSeconds = Mathf.Max(1f, minimumHarmfulSuspicionSeconds);
        maximumHarmfulSuspicionSeconds = Mathf.Max(
            minimumHarmfulSuspicionSeconds, maximumHarmfulSuspicionSeconds);
        maximumTelevisionNormalWatchSeconds = Mathf.Max(0.1f, maximumTelevisionNormalWatchSeconds);
        harmfulVideosBeforeNegativeRange.x = Mathf.Max(2, harmfulVideosBeforeNegativeRange.x);
        harmfulVideosBeforeNegativeRange.y = Mathf.Max(
            harmfulVideosBeforeNegativeRange.x, harmfulVideosBeforeNegativeRange.y);
        normalVideosToClearHarmfulCounter = Mathf.Max(1, normalVideosToClearHarmfulCounter);
        phoneVisibleVideoCount = Mathf.Max(1, phoneVisibleVideoCount);
        minimumPhoneRefreshSeconds = Mathf.Max(0.1f, minimumPhoneRefreshSeconds);
        maximumPhoneRefreshSeconds = Mathf.Max(
            minimumPhoneRefreshSeconds, maximumPhoneRefreshSeconds);
        phoneNormalVideosPerHarmfulVideo = Mathf.Max(1, phoneNormalVideosPerHarmfulVideo);
        maximumPhoneHarmfulVideos = Mathf.Clamp(maximumPhoneHarmfulVideos, 1, 3);
        phoneExtraHarmfulVideoChance = Mathf.Clamp01(phoneExtraHarmfulVideoChance);
    }
}
