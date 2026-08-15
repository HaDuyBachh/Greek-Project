using System.Collections;
using GreekProject.UI;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class KidFeedCycleController : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private PhoneVideoFeedUI phoneFeed;

    [Header("Cycle")]
    [SerializeField] private bool startOnPlay = true;
    [SerializeField, Min(0.1f)] private float cycleIntervalSeconds = 5f;

    private Coroutine cycleRoutine;

    private void Start()
    {
        ValidateSceneReferences();
        if (startOnPlay)
        {
            cycleRoutine = StartCoroutine(CycleRoutine());
        }
    }

    private void OnDisable()
    {
        if (cycleRoutine != null)
        {
            StopCoroutine(cycleRoutine);
            cycleRoutine = null;
        }
    }

    [ContextMenu("Trigger Feed Cycle")]
    public void TriggerCycle()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        phoneFeed?.RefreshRandomVideos();
    }

    private IEnumerator CycleRoutine()
    {
        while (enabled)
        {
            float remaining = cycleIntervalSeconds;
            while (remaining > 0f)
            {
                remaining -= Time.deltaTime;
                yield return null;
            }

            TriggerCycle();
        }
    }

    private void ValidateSceneReferences()
    {
        if (phoneFeed == null)
        {
            Debug.LogError("Kid Feed Cycle Controller requires PhoneVideoFeedUI assigned before Play.", this);
        }

    }
}
