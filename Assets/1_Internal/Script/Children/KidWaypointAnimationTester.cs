using System;
using System.Collections;
using System.Collections.Generic;
using GreekProject.Content;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator), typeof(NavMeshAgent))]
public class KidWaypointAnimationTester : MonoBehaviour
{
    public enum EmotionState
    {
        Stable,
        Happy,
        Anxious,
        Panic,
        Suspicious
    }

    private const string WalkPlaceLabel = "walk_place";
    private const string SitGroundLabel = "sit_ground";
    private const string EnterSofaLabel = "enter_sofa";
    private const string SitChairLabel = "sit_chair";
    private const string AnimatorLayerPrefix = "Base Layer.";

    [Header("Scene References")]
    [SerializeField] private WaypointGroup waypointGroup;
    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private KidDeviceUsageController deviceUsageController;
    [SerializeField, Tooltip("This Kid's prebuilt feed-cycle controller. Cleared after a successful guided-help action.")]
    private KidFeedCycleController feedCycleController;
    [SerializeField] private bool startOnPlay = true;

    [Header("Television Facing")]
    [SerializeField, Tooltip("Prebuilt TV transform used as the horizontal look target.")]
    private Transform televisionLookTarget;
    [SerializeField] private bool faceTelevisionWhileApproaching = true;
    [SerializeField, Min(0.1f)] private float televisionFacingApproachDistance = 1.5f;
    [SerializeField, Min(1f)] private float televisionTurnSpeed = 240f;

    [Header("Timing")]
    [SerializeField, Min(0.1f)] private float minActionDuration = 3f;
    [SerializeField, Min(0.1f)] private float maxActionDuration = 7f;
    [SerializeField, Tooltip("Visit activity waypoints in their Inspector order instead of choosing a random destination.")]
    private bool visitWaypointsInOrder = true;
    [SerializeField, Min(0), Tooltip("Starting offset in the ordered activity-waypoint list. Use a different value per Kid to avoid overlap.")]
    private int firstActivityWaypointIndex;
    [SerializeField, Min(1f)] private float travelTimeout = 20f;
    [SerializeField, Min(0f)] private float animationBlendTime = 0.2f;

    [Header("Waypoint Occupancy")]
    [SerializeField, Tooltip("Prevent this Kid from selecting a waypoint or chair already used or reserved by another Kid.")]
    private bool preventSharedPositions = true;
    [SerializeField, Min(0.1f), Tooltip("Minimum world-space separation between positions claimed by different Kids.")]
    private float reservedPositionRadius = 0.8f;
    [SerializeField, Min(0.1f), Tooltip("How long to wait before checking again when every suitable position is occupied.")]
    private float unavailablePositionRetryDelay = 1f;

    [Header("Video Emotion")]
    [SerializeField, Min(1)] private int brainrotViewsBeforeAnxiety = 3;
    [SerializeField, Min(1)] private int normalViewsToRecoverOneLevel = 2;
    [SerializeField, Min(1)] private int normalViewsBeforeHappy = 2;
    [SerializeField] private EmotionState currentEmotion = EmotionState.Stable;
    [SerializeField, Min(0)] private int brainrotExposure;
    [SerializeField, Min(0)] private int consecutiveNormalViews;

    [Header("Movement Animations")]
    [SerializeField] private string[] locomotionAnimations = { "Walking", "RunForward" };
    [SerializeField] private float walkSpeed = 3.5f;
    [SerializeField] private float runSpeed = 5f;

    [Header("Neutral - walk_place (Used By Random Test)")]
    [SerializeField] private string[] neutralStandingAnimations =
    {
        "Breathing Idle"
    };

    [Header("Emotional - Standing (Not Used By Random Test)")]
    [SerializeField] private string[] emotionalStandingAnimations =
    {
        "Panic",
        "AngryStandNormal",
        "AngryStandNormal_1",
        "Crying"
    };

    [Header("Neutral - sit_ground (Used By Random Test)")]
    [SerializeField] private string[] neutralGroundAnimations =
    {
        "SitGround",
        "SitGroundUsingPhone"
    };

    [Header("Emotional - Ground (Not Used By Random Test)")]
    [SerializeField] private string[] emotionalGroundAnimations =
    {
        "GroundPain"
    };

    [Header("Neutral - sit_chair (Used By Random Test)")]
    [SerializeField] private string[] neutralChairAnimations =
    {
        "SitChairIdle",
        "SitChairUsingPhone"
    };

    [Header("Emotional - Chair (Not Used By Random Test)")]
    [SerializeField] private string[] emotionalChairAnimations =
    {
        "SitChairFear",
        "SitChairYell"
    };

    public IReadOnlyList<string> EmotionalStandingAnimations => emotionalStandingAnimations;
    public IReadOnlyList<string> EmotionalGroundAnimations => emotionalGroundAnimations;
    public IReadOnlyList<string> EmotionalChairAnimations => emotionalChairAnimations;

    private readonly List<LabeledWaypoint> activityWaypoints = new List<LabeledWaypoint>();
    private readonly List<LabeledWaypoint> sofaEntrances = new List<LabeledWaypoint>();
    private readonly List<LabeledWaypoint> chairSeats = new List<LabeledWaypoint>();
    private static readonly List<KidWaypointAnimationTester> ActiveMovers = new List<KidWaypointAnimationTester>();

    private Coroutine testRoutine;
    private Coroutine guidedHelpRoutine;
    private LabeledWaypoint previousActivityWaypoint;
    private LabeledWaypoint currentChairSeat;
    private bool phonePauseRequested;
    private bool focusPauseRequested;
    private bool isTravelling;
    private bool emotionChangedWhilePaused;
    private bool videoSuspicionActive;
    private string currentAnimationState = string.Empty;
    private int nextActivityWaypointIndex;
    private LabeledWaypoint reservedDestination;
    private LabeledWaypoint reservedChairDestination;
    private bool reservedChairWillWatchTelevision;

    public bool IsPausedForPhone => phonePauseRequested;
    public bool IsPausedForFocus => focusPauseRequested;
    public bool IsActivityPaused => phonePauseRequested || focusPauseRequested;
    public bool IsTravelling => isTravelling;
    public EmotionState CurrentEmotion => currentEmotion;
    public EmotionState VisualEmotion => videoSuspicionActive ? EmotionState.Suspicious : currentEmotion;
    public int BrainrotExposure => brainrotExposure;
    public bool IsNegativeEmotion => currentEmotion == EmotionState.Anxious || currentEmotion == EmotionState.Panic;
    public bool IsAtVideoViewingLocation => !isTravelling &&
                                            (currentChairSeat != null ||
                                             (previousActivityWaypoint != null &&
                                              HasLabel(previousActivityWaypoint, SitGroundLabel)));
    public LabeledWaypoint CurrentChairSeat => currentChairSeat;
    public string CurrentAnimationState => currentAnimationState;
    public bool IsGuidedHelpActionActive => guidedHelpRoutine != null;

    private void Awake()
    {
        ResolveReferences();
        CacheWaypoints();
        nextActivityWaypointIndex = Mathf.Max(0, firstActivityWaypointIndex);

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }
    }

    private void OnEnable()
    {
        if (!ActiveMovers.Contains(this))
        {
            ActiveMovers.Add(this);
        }
    }

    private void Start()
    {
        if (startOnPlay)
        {
            StartTesting();
        }
    }

    private void OnDisable()
    {
        StopTesting();
        ClearPendingReservations();
        ActiveMovers.Remove(this);
    }

    private void OnDestroy()
    {
        ActiveMovers.Remove(this);
    }

    [ContextMenu("Start Random Animation Test")]
    public void StartTesting()
    {
        if (!isActiveAndEnabled || testRoutine != null || guidedHelpRoutine != null)
        {
            return;
        }

        ResolveReferences();
        CacheWaypoints();

        if (animator == null || agent == null || waypointGroup == null || activityWaypoints.Count == 0)
        {
            Debug.LogWarning($"{name}: Random waypoint test is missing Animator, NavMeshAgent, WaypointGroup, or activity waypoints.", this);
            return;
        }

        testRoutine = StartCoroutine(TestRoutine());
    }

    [ContextMenu("Stop Random Animation Test")]
    public void StopTesting()
    {
        if (testRoutine != null)
        {
            StopCoroutine(testRoutine);
            testRoutine = null;
        }

        if (guidedHelpRoutine != null)
        {
            StopCoroutine(guidedHelpRoutine);
            guidedHelpRoutine = null;
        }

        isTravelling = false;
    }

    public bool TryStartGuidedHelpAction(
        string destinationLabel,
        string[] actionAnimations,
        float actionDurationSeconds)
    {
        ResolveReferences();
        CacheWaypoints();

        if (!isActiveAndEnabled || animator == null || agent == null || waypointGroup == null ||
            feedCycleController == null ||
            string.IsNullOrWhiteSpace(destinationLabel) || actionAnimations == null ||
            actionAnimations.Length == 0)
        {
            Debug.LogWarning($"{name}: Guided help action is missing its prebuilt feed, waypoint, or animation assignment.", this);
            return false;
        }

        string actionAnimation = PickValidAnimation(actionAnimations, actionAnimations[0]);
        LabeledWaypoint destination = FindNearestAvailableWaypointByLabel(destinationLabel);
        if (destination == null || string.IsNullOrWhiteSpace(actionAnimation))
        {
            Debug.LogWarning($"{name}: No available '{destinationLabel}' waypoint or valid guided-help animation was found.", this);
            return false;
        }

        if (testRoutine != null)
        {
            StopCoroutine(testRoutine);
            testRoutine = null;
        }

        if (guidedHelpRoutine != null)
        {
            StopCoroutine(guidedHelpRoutine);
            guidedHelpRoutine = null;
        }

        ClearPendingReservations();
        reservedDestination = destination;
        guidedHelpRoutine = StartCoroutine(GuidedHelpRoutine(
            destination,
            actionAnimation,
            Mathf.Max(1f, actionDurationSeconds)));
        return true;
    }

    public void SetPausedForPhone(bool shouldPause)
    {
        phonePauseRequested = shouldPause;
        ApplyReleasedPauseState();
    }

    public void SetPausedForFocus(bool shouldPause)
    {
        focusPauseRequested = shouldPause;
        ApplyReleasedPauseState();
    }

    private void ApplyReleasedPauseState()
    {
        if (!IsActivityPaused && emotionChangedWhilePaused && !isTravelling)
        {
            emotionChangedWhilePaused = false;
            PlayCurrentEmotionAnimation();
        }
    }

    public void ApplyViewedVideoEffect(VideoContentEffect effect)
    {
        EmotionState previousEmotion = currentEmotion;

        switch (effect)
        {
            case VideoContentEffect.Horror:
                currentEmotion = EmotionState.Panic;
                consecutiveNormalViews = 0;
                break;

            case VideoContentEffect.Brainrot:
                brainrotExposure++;
                consecutiveNormalViews = 0;
                if (currentEmotion == EmotionState.Happy)
                {
                    currentEmotion = EmotionState.Stable;
                }

                if (currentEmotion != EmotionState.Panic && brainrotExposure >= brainrotViewsBeforeAnxiety)
                {
                    currentEmotion = EmotionState.Anxious;
                }
                break;

            default:
                brainrotExposure = Mathf.Max(0, brainrotExposure - 1);
                consecutiveNormalViews++;

                if (currentEmotion == EmotionState.Panic &&
                    consecutiveNormalViews >= normalViewsToRecoverOneLevel)
                {
                    currentEmotion = EmotionState.Anxious;
                    consecutiveNormalViews = 0;
                }
                else if (currentEmotion == EmotionState.Anxious && brainrotExposure == 0 &&
                         consecutiveNormalViews >= normalViewsToRecoverOneLevel)
                {
                    currentEmotion = EmotionState.Stable;
                    consecutiveNormalViews = 0;
                }
                else if (currentEmotion == EmotionState.Stable &&
                         consecutiveNormalViews >= normalViewsBeforeHappy)
                {
                    currentEmotion = EmotionState.Happy;
                    consecutiveNormalViews = 0;
                }
                break;
        }

        if (currentEmotion == previousEmotion)
        {
            return;
        }

        if (IsActivityPaused)
        {
            emotionChangedWhilePaused = true;
        }
        else if (!isTravelling)
        {
            PlayCurrentEmotionAnimation();
        }
    }

    public void ApplyUnresolvedHarmfulContentPanic()
    {
        ApplyViewedVideoEffect(VideoContentEffect.Horror);
    }

    public void SetVideoSuspicion(bool shouldShow)
    {
        videoSuspicionActive = shouldShow;
    }

    private void PlayCurrentEmotionAnimation()
    {
        if (currentChairSeat != null)
        {
            PlayAnimation(PickChairAnimation());
        }
        else if (previousActivityWaypoint != null && HasLabel(previousActivityWaypoint, SitGroundLabel))
        {
            PlayAnimation(PickGroundAnimation());
        }
        else
        {
            PlayAnimation(PickStandingAnimation());
        }
    }

    [ContextMenu("Refresh Waypoints")]
    public void CacheWaypoints()
    {
        activityWaypoints.Clear();
        sofaEntrances.Clear();
        chairSeats.Clear();

        if (waypointGroup == null)
        {
            return;
        }

        waypointGroup.RefreshList();

        foreach (LabeledWaypoint waypoint in waypointGroup.Waypoints)
        {
            if (waypoint == null)
            {
                continue;
            }

            if (HasLabel(waypoint, WalkPlaceLabel) || HasLabel(waypoint, SitGroundLabel) || HasLabel(waypoint, EnterSofaLabel))
            {
                activityWaypoints.Add(waypoint);
            }

            if (HasLabel(waypoint, EnterSofaLabel))
            {
                sofaEntrances.Add(waypoint);
            }
            else if (HasLabel(waypoint, SitChairLabel))
            {
                chairSeats.Add(waypoint);
            }
        }
    }

    private IEnumerator TestRoutine()
    {
        while (enabled)
        {
            yield return WaitWhileActivityPaused();

            LabeledWaypoint target = PickActivityWaypoint();
            if (target == null)
            {
                yield return WaitForActionDuration(unavailablePositionRetryDelay);
                continue;
            }

            PrepareToTravel();

            string locomotion = PickValidAnimation(locomotionAnimations, "Walking");
            agent.speed = string.Equals(locomotion, "RunForward", StringComparison.Ordinal) ? runSpeed : walkSpeed;
            PlayAnimation(locomotion);

            if (!TrySetDestination(target.Position))
            {
                isTravelling = false;
                if (IsActivityPaused)
                {
                    PlayCurrentEmotionAnimation();
                }

                Debug.LogWarning($"{name}: Cannot find NavMesh near waypoint {target.name} ({target.Label}).", target);
                ClearPendingReservations();
                yield return null;
                continue;
            }

            isTravelling = true;
            yield return WaitForArrival(target);
            isTravelling = false;

            if (!agent.enabled || !agent.isOnNavMesh || agent.pathPending || agent.remainingDistance > GetArrivalDistance(target) + 0.1f)
            {
                if (agent.enabled && agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                }

                if (IsActivityPaused)
                {
                    PlayCurrentEmotionAnimation();
                }

                Debug.LogWarning($"{name}: Timed out while travelling to {target.name} ({target.Label}).", target);
                ClearPendingReservations();
                continue;
            }

            agent.isStopped = true;
            target.Arrive(gameObject);
            previousActivityWaypoint = target;

            if (HasLabel(target, EnterSofaLabel))
            {
                EnterReservedChair(target);
            }
            else if (HasLabel(target, SitGroundLabel))
            {
                LockToWaypoint(target);
                deviceUsageController?.BeginGroundActivity();
                if (deviceUsageController != null && deviceUsageController.IsWatchingTelevision)
                {
                    FaceTelevisionImmediately();
                }
                PlayAnimation(PickGroundAnimation());
            }
            else
            {
                transform.rotation = target.transform.rotation;
                PlayAnimation(PickStandingAnimation());
            }

            ClearPendingReservations();

            emotionChangedWhilePaused = false;

            yield return WaitForActionDuration(UnityEngine.Random.Range(minActionDuration, maxActionDuration));
        }

        testRoutine = null;
    }

    private IEnumerator GuidedHelpRoutine(
        LabeledWaypoint destination,
        string actionAnimation,
        float actionDurationSeconds)
    {
        PrepareToTravel();

        string locomotion = PickValidAnimation(locomotionAnimations, "Walking");
        agent.speed = string.Equals(locomotion, "RunForward", StringComparison.Ordinal)
            ? runSpeed
            : walkSpeed;
        PlayAnimation(locomotion);

        if (!TrySetDestination(destination.Position))
        {
            Debug.LogWarning($"{name}: Cannot find NavMesh near guided-help waypoint {destination.name} ({destination.Label}).", destination);
            FinishGuidedHelpAction(false);
            yield break;
        }

        isTravelling = true;
        yield return WaitForArrival(destination);
        isTravelling = false;

        if (!agent.enabled || !agent.isOnNavMesh || agent.pathPending ||
            agent.remainingDistance > GetArrivalDistance(destination) + 0.1f)
        {
            if (agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }

            Debug.LogWarning($"{name}: Timed out while travelling to guided-help waypoint {destination.name} ({destination.Label}).", destination);
            FinishGuidedHelpAction(false);
            yield break;
        }

        agent.isStopped = true;
        destination.Arrive(gameObject);
        previousActivityWaypoint = destination;
        currentChairSeat = null;
        LockToWaypoint(destination);
        ClearPendingReservations();
        PlayAnimation(actionAnimation);

        float remaining = actionDurationSeconds;
        while (remaining > 0f && enabled)
        {
            remaining -= Time.deltaTime;
            yield return null;
        }

        FinishGuidedHelpAction(true);
    }

    private void FinishGuidedHelpAction(bool wasCompleted)
    {
        isTravelling = false;
        ClearPendingReservations();
        guidedHelpRoutine = null;

        if (wasCompleted)
        {
            feedCycleController?.ClearHarmfulProgressAfterGuidedHelp();
            brainrotExposure = 0;
            consecutiveNormalViews = 0;
            videoSuspicionActive = false;
            currentEmotion = EmotionState.Happy;
            emotionChangedWhilePaused = false;
        }

        PlayCurrentEmotionAnimation();

        if (startOnPlay && isActiveAndEnabled)
        {
            StartTesting();
        }
    }

    private IEnumerator WaitWhileActivityPaused()
    {
        while (IsActivityPaused)
        {
            yield return null;
        }
    }

    private IEnumerator WaitForActionDuration(float duration)
    {
        float remaining = duration;
        while (remaining > 0f)
        {
            if (!IsActivityPaused)
            {
                remaining -= Time.deltaTime;
            }

            yield return null;
        }
    }

    private void PrepareToTravel()
    {
        deviceUsageController?.EndDeviceActivity();
        Vector3 departurePosition = transform.position;
        Quaternion departureRotation = transform.rotation;

        if (currentChairSeat != null)
        {
            LabeledWaypoint exit = FindNearest(currentChairSeat.Position, sofaEntrances);
            if (exit != null)
            {
                departurePosition = exit.Position;
                departureRotation = exit.transform.rotation;
            }

            currentChairSeat = null;
        }

        if (agent.enabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.enabled = false;
        }

        if (NavMesh.SamplePosition(departurePosition, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            transform.SetPositionAndRotation(hit.position, departureRotation);
        }
        else
        {
            transform.SetPositionAndRotation(departurePosition, departureRotation);
        }

        agent.enabled = true;
        agent.isStopped = false;
    }

    private bool TrySetDestination(Vector3 targetPosition)
    {
        if (!agent.enabled || !agent.isOnNavMesh)
        {
            return false;
        }

        if (!NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 2f, agent.areaMask))
        {
            return false;
        }

        agent.isStopped = false;
        return agent.SetDestination(hit.position);
    }

    private IEnumerator WaitForArrival(LabeledWaypoint target)
    {
        float elapsed = 0f;

        while (elapsed < travelTimeout)
        {
            UpdateTelevisionFacingWhileApproaching();

            if (agent.enabled && agent.isOnNavMesh && !agent.pathPending &&
                agent.pathStatus != NavMeshPathStatus.PathInvalid &&
                agent.remainingDistance <= GetArrivalDistance(target))
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void EnterReservedChair(LabeledWaypoint entrance)
    {
        LabeledWaypoint chair = reservedChairDestination;
        if (chair == null)
        {
            Debug.LogWarning($"{name}: No unoccupied sit_chair waypoint was reserved for {entrance.name}.", entrance);
            PlayAnimation(PickStandingAnimation());
            return;
        }

        currentChairSeat = chair;
        LockToWaypoint(chair);
        chair.Arrive(gameObject);
        deviceUsageController?.BeginChairActivity();
        if (deviceUsageController != null && deviceUsageController.IsWatchingTelevision)
        {
            FaceTelevisionImmediately();
        }
        PlayAnimation(PickChairAnimation());
    }

    private string PickStandingAnimation()
    {
        if (currentEmotion == EmotionState.Panic)
        {
            return HasAnimation("Panic") ? "Panic" : PickValidAnimation(emotionalStandingAnimations, "Panic");
        }

        return currentEmotion == EmotionState.Anxious
            ? PickValidAnimation(emotionalStandingAnimations, "AngryStandNormal")
            : PickValidAnimation(neutralStandingAnimations, "Breathing Idle");
    }

    private string PickGroundAnimation()
    {
        if (currentEmotion == EmotionState.Panic)
        {
            return HasAnimation("GroundPain") ? "GroundPain" : PickValidAnimation(emotionalGroundAnimations, "GroundPain");
        }

        if (currentEmotion == EmotionState.Anxious)
        {
            return PickValidAnimation(emotionalGroundAnimations, "GroundPain");
        }

        string deviceAnimation = deviceUsageController?.ResolveNeutralGroundAnimation();
        return HasAnimation(deviceAnimation)
            ? deviceAnimation
            : PickValidAnimation(neutralGroundAnimations, "SitGround");
    }

    private string PickChairAnimation()
    {
        if (currentEmotion == EmotionState.Panic)
        {
            return HasAnimation("SitChairFear") ? "SitChairFear" : PickValidAnimation(emotionalChairAnimations, "SitChairFear");
        }

        if (currentEmotion == EmotionState.Anxious)
        {
            return PickValidAnimation(emotionalChairAnimations, "SitChairFear");
        }

        string deviceAnimation = deviceUsageController?.ResolveNeutralChairAnimation();
        return HasAnimation(deviceAnimation)
            ? deviceAnimation
            : PickValidAnimation(neutralChairAnimations, "SitChairIdle");
    }

    private void LockToWaypoint(LabeledWaypoint waypoint)
    {
        if (agent.enabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.enabled = false;
        }

        transform.SetPositionAndRotation(waypoint.Position, waypoint.transform.rotation);
    }

    private LabeledWaypoint PickActivityWaypoint()
    {
        if (activityWaypoints.Count == 0)
        {
            return null;
        }

        if (visitWaypointsInOrder)
        {
            for (int offset = 0; offset < activityWaypoints.Count; offset++)
            {
                int index = (nextActivityWaypointIndex + offset) % activityWaypoints.Count;
                LabeledWaypoint candidate = activityWaypoints[index];
                if (candidate == null || candidate == previousActivityWaypoint || !TryReserveDestination(candidate))
                {
                    continue;
                }

                nextActivityWaypointIndex = (index + 1) % activityWaypoints.Count;
                return candidate;
            }
        }

        int randomStartIndex = UnityEngine.Random.Range(0, activityWaypoints.Count);
        for (int offset = 0; offset < activityWaypoints.Count; offset++)
        {
            LabeledWaypoint candidate = activityWaypoints[(randomStartIndex + offset) % activityWaypoints.Count];
            if (candidate != null && candidate != previousActivityWaypoint && TryReserveDestination(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private bool TryReserveDestination(LabeledWaypoint candidate)
    {
        if (!IsPositionAvailable(candidate))
        {
            return false;
        }

        LabeledWaypoint chair = null;
        if (HasLabel(candidate, EnterSofaLabel))
        {
            chair = FindNearestAvailable(candidate.Position, chairSeats);
            if (chair == null)
            {
                return false;
            }
        }

        reservedDestination = candidate;
        reservedChairDestination = chair;
        bool televisionGroundActivity = HasLabel(candidate, SitGroundLabel) &&
                                         deviceUsageController != null &&
                                         deviceUsageController.WatchesTelevisionWhenSittingOnGround;
        bool televisionChairActivity = chair != null && deviceUsageController != null &&
                                        deviceUsageController.NextChairActivity ==
                                        KidDeviceUsageController.DeviceActivity.Television;
        reservedChairWillWatchTelevision = televisionGroundActivity || televisionChairActivity;
        return true;
    }

    private LabeledWaypoint FindNearestAvailable(Vector3 origin, List<LabeledWaypoint> candidates)
    {
        LabeledWaypoint nearest = null;
        float nearestDistance = float.PositiveInfinity;

        foreach (LabeledWaypoint candidate in candidates)
        {
            if (candidate == null || !IsPositionAvailable(candidate))
            {
                continue;
            }

            float distance = (candidate.Position - origin).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearest = candidate;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    private LabeledWaypoint FindNearestAvailableWaypointByLabel(string label)
    {
        for (int pass = 0; pass < 2; pass++)
        {
            bool allowCurrentPosition = pass == 1;
            LabeledWaypoint nearest = null;
            float nearestDistance = float.PositiveInfinity;

            foreach (LabeledWaypoint candidate in waypointGroup.Waypoints)
            {
                if (candidate == null || !HasLabel(candidate, label) ||
                    !IsPositionAvailable(candidate) ||
                    (!allowCurrentPosition && candidate == previousActivityWaypoint))
                {
                    continue;
                }

                float distance = (candidate.Position - transform.position).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearest = candidate;
                    nearestDistance = distance;
                }
            }

            if (nearest != null)
            {
                return nearest;
            }
        }

        return null;
    }

    private bool IsPositionAvailable(LabeledWaypoint candidate)
    {
        if (!preventSharedPositions || candidate == null)
        {
            return true;
        }

        ActiveMovers.RemoveAll(mover => mover == null);
        foreach (KidWaypointAnimationTester other in ActiveMovers)
        {
            if (other == this || !other.preventSharedPositions)
            {
                continue;
            }

            float separation = Mathf.Max(reservedPositionRadius, other.reservedPositionRadius);
            float separationSquared = separation * separation;
            if (IsClaimNear(candidate.Position, other.reservedDestination, separationSquared) ||
                IsClaimNear(candidate.Position, other.reservedChairDestination, separationSquared) ||
                IsClaimNear(candidate.Position, other.previousActivityWaypoint, separationSquared) ||
                IsClaimNear(candidate.Position, other.currentChairSeat, separationSquared) ||
                (!other.isTravelling &&
                 (other.transform.position - candidate.Position).sqrMagnitude < separationSquared))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsClaimNear(Vector3 position, LabeledWaypoint claim, float separationSquared)
    {
        return claim != null && (claim.Position - position).sqrMagnitude < separationSquared;
    }

    private void ClearPendingReservations()
    {
        reservedDestination = null;
        reservedChairDestination = null;
        reservedChairWillWatchTelevision = false;
        if (agent != null)
        {
            agent.updateRotation = true;
        }
    }

    private void LateUpdate()
    {
        if (!isTravelling && deviceUsageController != null &&
            deviceUsageController.IsWatchingTelevision)
        {
            RotateTowardTelevision(televisionTurnSpeed * Time.deltaTime);
        }
    }

    private void UpdateTelevisionFacingWhileApproaching()
    {
        if (!faceTelevisionWhileApproaching || !reservedChairWillWatchTelevision ||
            televisionLookTarget == null || agent == null || !agent.enabled ||
            !agent.isOnNavMesh || agent.pathPending ||
            agent.remainingDistance > televisionFacingApproachDistance)
        {
            return;
        }

        agent.updateRotation = false;
        RotateTowardTelevision(televisionTurnSpeed * Time.deltaTime);
    }

    private void FaceTelevisionImmediately()
    {
        RotateTowardTelevision(360f);
    }

    private void RotateTowardTelevision(float maximumDegrees)
    {
        if (televisionLookTarget == null)
        {
            return;
        }

        Vector3 direction = televisionLookTarget.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation,
            Mathf.Max(0f, maximumDegrees));
    }

    private string PickValidAnimation(string[] candidates, string fallback)
    {
        if (candidates != null && candidates.Length > 0)
        {
            int startIndex = UnityEngine.Random.Range(0, candidates.Length);

            for (int offset = 0; offset < candidates.Length; offset++)
            {
                string candidate = candidates[(startIndex + offset) % candidates.Length];
                if (HasAnimation(candidate))
                {
                    return candidate;
                }
            }
        }

        return HasAnimation(fallback) ? fallback : string.Empty;
    }

    private void PlayAnimation(string stateName)
    {
        if (string.IsNullOrWhiteSpace(stateName) || animator == null)
        {
            return;
        }

        currentAnimationState = stateName;
        animator.CrossFadeInFixedTime(Animator.StringToHash(AnimatorLayerPrefix + stateName), animationBlendTime);
    }

    private bool HasAnimation(string stateName)
    {
        return animator != null && !string.IsNullOrWhiteSpace(stateName) &&
               animator.HasState(0, Animator.StringToHash(AnimatorLayerPrefix + stateName));
    }

    private float GetArrivalDistance(LabeledWaypoint waypoint)
    {
        return Mathf.Max(0.2f, agent.stoppingDistance, waypoint.ArriveRadius);
    }

    private void ResolveReferences()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }

    }

    private static bool HasLabel(LabeledWaypoint waypoint, string label)
    {
        return string.Equals(waypoint.Label, label, StringComparison.OrdinalIgnoreCase);
    }

    private static LabeledWaypoint FindNearest(Vector3 origin, List<LabeledWaypoint> candidates)
    {
        LabeledWaypoint nearest = null;
        float nearestDistance = float.PositiveInfinity;

        foreach (LabeledWaypoint candidate in candidates)
        {
            if (candidate == null)
            {
                continue;
            }

            float distance = (candidate.Position - origin).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearest = candidate;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    private void OnValidate()
    {
        minActionDuration = Mathf.Max(0.1f, minActionDuration);
        maxActionDuration = Mathf.Max(minActionDuration, maxActionDuration);
        travelTimeout = Mathf.Max(1f, travelTimeout);
        animationBlendTime = Mathf.Max(0f, animationBlendTime);
        walkSpeed = Mathf.Max(0.1f, walkSpeed);
        runSpeed = Mathf.Max(walkSpeed, runSpeed);
        brainrotViewsBeforeAnxiety = Mathf.Max(1, brainrotViewsBeforeAnxiety);
        normalViewsToRecoverOneLevel = Mathf.Max(1, normalViewsToRecoverOneLevel);
        normalViewsBeforeHappy = Mathf.Max(1, normalViewsBeforeHappy);
        firstActivityWaypointIndex = Mathf.Max(0, firstActivityWaypointIndex);
        reservedPositionRadius = Mathf.Max(0.1f, reservedPositionRadius);
        unavailablePositionRetryDelay = Mathf.Max(0.1f, unavailablePositionRetryDelay);
        televisionFacingApproachDistance = Mathf.Max(0.1f, televisionFacingApproachDistance);
        televisionTurnSpeed = Mathf.Max(1f, televisionTurnSpeed);
    }
}
