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
        Panic
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
    [SerializeField] private bool startOnPlay = true;

    [Header("Timing")]
    [SerializeField, Min(0.1f)] private float minActionDuration = 3f;
    [SerializeField, Min(0.1f)] private float maxActionDuration = 7f;
    [SerializeField, Min(1f)] private float travelTimeout = 20f;
    [SerializeField, Min(0f)] private float animationBlendTime = 0.2f;

    [Header("Video Emotion")]
    [SerializeField, Min(1)] private int brainrotViewsBeforeAnxiety = 3;
    [SerializeField, Range(0f, 1f)] private float normalVideoHappyChance = 0.5f;
    [SerializeField] private EmotionState currentEmotion = EmotionState.Stable;
    [SerializeField, Min(0)] private int brainrotExposure;

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

    private Coroutine testRoutine;
    private LabeledWaypoint previousActivityWaypoint;
    private LabeledWaypoint currentChairSeat;
    private bool phonePauseRequested;
    private bool focusPauseRequested;
    private bool isTravelling;
    private bool emotionChangedWhilePaused;
    private string currentAnimationState = string.Empty;

    public bool IsPausedForPhone => phonePauseRequested;
    public bool IsPausedForFocus => focusPauseRequested;
    public bool IsActivityPaused => phonePauseRequested || focusPauseRequested;
    public bool IsTravelling => isTravelling;
    public EmotionState CurrentEmotion => currentEmotion;
    public int BrainrotExposure => brainrotExposure;
    public LabeledWaypoint CurrentChairSeat => currentChairSeat;
    public string CurrentAnimationState => currentAnimationState;

    private void Awake()
    {
        ResolveReferences();
        CacheWaypoints();

        if (animator != null)
        {
            animator.applyRootMotion = false;
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
    }

    [ContextMenu("Start Random Animation Test")]
    public void StartTesting()
    {
        if (!isActiveAndEnabled || testRoutine != null)
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

        isTravelling = false;
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
        switch (effect)
        {
            case VideoContentEffect.Horror:
                currentEmotion = EmotionState.Panic;
                break;

            case VideoContentEffect.Brainrot:
                brainrotExposure++;
                if (currentEmotion != EmotionState.Panic && brainrotExposure >= brainrotViewsBeforeAnxiety)
                {
                    currentEmotion = EmotionState.Anxious;
                }
                break;

            default:
                brainrotExposure = Mathf.Max(0, brainrotExposure - 1);
                currentEmotion = UnityEngine.Random.value < normalVideoHappyChance
                    ? EmotionState.Happy
                    : EmotionState.Stable;
                break;
        }

        emotionChangedWhilePaused |= IsActivityPaused;
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
                yield break;
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
                continue;
            }

            agent.isStopped = true;
            target.Arrive(gameObject);
            previousActivityWaypoint = target;

            if (HasLabel(target, EnterSofaLabel))
            {
                EnterNearestChair(target);
            }
            else if (HasLabel(target, SitGroundLabel))
            {
                LockToWaypoint(target);
                PlayAnimation(PickGroundAnimation());
            }
            else
            {
                transform.rotation = target.transform.rotation;
                PlayAnimation(PickStandingAnimation());
            }

            emotionChangedWhilePaused = false;

            yield return WaitForActionDuration(UnityEngine.Random.Range(minActionDuration, maxActionDuration));
        }

        testRoutine = null;
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

    private void EnterNearestChair(LabeledWaypoint entrance)
    {
        LabeledWaypoint chair = FindNearest(entrance.Position, chairSeats);
        if (chair == null)
        {
            Debug.LogWarning($"{name}: No sit_chair waypoint was found for {entrance.name}.", entrance);
            PlayAnimation(PickStandingAnimation());
            return;
        }

        currentChairSeat = chair;
        LockToWaypoint(chair);
        chair.Arrive(gameObject);
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

        return currentEmotion == EmotionState.Anxious
            ? PickValidAnimation(emotionalGroundAnimations, "GroundPain")
            : PickValidAnimation(neutralGroundAnimations, "SitGround");
    }

    private string PickChairAnimation()
    {
        if (currentEmotion == EmotionState.Panic)
        {
            return HasAnimation("SitChairFear") ? "SitChairFear" : PickValidAnimation(emotionalChairAnimations, "SitChairFear");
        }

        return currentEmotion == EmotionState.Anxious
            ? PickValidAnimation(emotionalChairAnimations, "SitChairFear")
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

        if (activityWaypoints.Count == 1)
        {
            return activityWaypoints[0];
        }

        LabeledWaypoint selected;
        int attempts = 0;

        do
        {
            selected = activityWaypoints[UnityEngine.Random.Range(0, activityWaypoints.Count)];
            attempts++;
        }
        while (selected == previousActivityWaypoint && attempts < 8);

        return selected;
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
    }
}
