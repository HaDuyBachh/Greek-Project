using System.Collections;
using GreekProject.UI;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class KidFeedCycleController : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private PhoneVideoFeedUI phoneFeed;
    [SerializeField] private ChatUIFollowController chatUiController;
    [SerializeField] private string kidId = "Kid1";

    [Header("Cycle")]
    [SerializeField] private bool startOnPlay = true;
    [SerializeField, Min(0.1f)] private float cycleIntervalSeconds = 5f;
    [SerializeField, Min(0.1f)] private float emoteDisplaySeconds = 2f;

    [Header("Random Emotes")]
    [SerializeField] private Sprite[] randomEmotes;

    private Coroutine cycleRoutine;
    private Coroutine hideEmoteRoutine;

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

        HideCurrentEmote();
    }

    [ContextMenu("Trigger Feed And Emote Cycle")]
    public void TriggerCycle()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        phoneFeed?.RefreshRandomVideos();
        ShowRandomEmote();
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

    private void ShowRandomEmote()
    {
        if (chatUiController == null || randomEmotes == null || randomEmotes.Length == 0 ||
            !chatUiController.RequestChat(kidId, out ChatUIFollowController.ChatSlot slot) ||
            slot?.chatRoot == null || slot.emoteRoot == null)
        {
            return;
        }

        Image emoteImage = slot.emoteRoot.GetComponentInChildren<Image>(true);
        if (emoteImage == null)
        {
            Debug.LogError($"{slot.chatRoot.name}/Emote requires an Image child assigned before Play.", slot.chatRoot);
            return;
        }

        if (hideEmoteRoutine != null)
        {
            StopCoroutine(hideEmoteRoutine);
        }

        slot.chatRoot.gameObject.SetActive(true);
        SetActive(slot.talkRoot, false);
        SetActive(slot.videoRoot, false);
        SetActive(slot.emoteRoot, true);
        emoteImage.sprite = randomEmotes[Random.Range(0, randomEmotes.Length)];
        emoteImage.color = Color.white;
        emoteImage.preserveAspect = true;
        hideEmoteRoutine = StartCoroutine(HideEmoteAfterDelay());
    }

    private IEnumerator HideEmoteAfterDelay()
    {
        float remaining = emoteDisplaySeconds;
        while (remaining > 0f)
        {
            remaining -= Time.deltaTime;
            yield return null;
        }

        hideEmoteRoutine = null;
        chatUiController?.ReleaseChatUsedByKid(kidId);
    }

    private void HideCurrentEmote()
    {
        if (hideEmoteRoutine != null)
        {
            StopCoroutine(hideEmoteRoutine);
            hideEmoteRoutine = null;
        }

        chatUiController?.ReleaseChatUsedByKid(kidId);
    }

    private void ValidateSceneReferences()
    {
        if (phoneFeed == null)
        {
            Debug.LogError("Kid Feed Cycle Controller requires PhoneVideoFeedUI assigned before Play.", this);
        }

        if (chatUiController == null)
        {
            Debug.LogError("Kid Feed Cycle Controller requires ChatUIFollowController assigned before Play.", this);
        }

        if (randomEmotes == null || randomEmotes.Length == 0)
        {
            Debug.LogError("Kid Feed Cycle Controller requires at least one emote Sprite assigned before Play.", this);
        }
    }

    private static void SetActive(Component component, bool active)
    {
        if (component != null)
        {
            component.gameObject.SetActive(active);
        }
    }
}
