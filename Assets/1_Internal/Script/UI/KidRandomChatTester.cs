using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-50)]
[DisallowMultipleComponent]
public class KidRandomChatTester : MonoBehaviour
{
    public enum ChatContentType
    {
        Auto,
        Talk,
        Emote,
        Video
    }

    [Serializable]
    public class ChatMoment
    {
        public ChatContentType contentType;
        public string text;
        public Sprite[] emotes;
        public bool videoAvailable;
    }

    [SerializeField] private ChatUIFollowController chatController;
    [SerializeField] private string kidId = "Kid1";

    [Header("Timing")]
    [SerializeField, Min(0f)] private float initialDelay = 1f;
    [SerializeField] private Vector2 silentDurationRange = new Vector2(2f, 5f);
    [SerializeField] private Vector2 visibleDurationRange = new Vector2(2.5f, 4.5f);

    [Header("Test Content")]
    [SerializeField] private ChatMoment[] moments;
    [SerializeField, Min(0f)] private float talkWeight = 1f;
    [SerializeField, Min(0f)] private float emoteWeight = 3f;
    [SerializeField, Min(0f)] private float videoWeight = 1f;

    private Coroutine testRoutine;
    private ChatUIFollowController.ChatSlot currentSlot;
    private string lastChatId;

    private void Start()
    {
        if (chatController == null)
        {
            chatController = FindFirstObjectByType<ChatUIFollowController>();
        }

        if (chatController != null && moments != null && moments.Length > 0)
        {
            testRoutine = StartCoroutine(RandomChatLoop());
        }
    }

    private IEnumerator RandomChatLoop()
    {
        yield return new WaitForSeconds(initialDelay);

        while (enabled)
        {
            yield return new WaitForSeconds(RandomDuration(silentDurationRange));

            if (!chatController.RequestRandomChat(kidId, lastChatId, out currentSlot))
            {
                yield return null;
                continue;
            }

            ShowMoment(currentSlot, moments[UnityEngine.Random.Range(0, moments.Length)]);
            yield return new WaitForSeconds(RandomDuration(visibleDurationRange));
            ReleaseCurrentChat();
        }
    }

    private void ShowMoment(ChatUIFollowController.ChatSlot slot, ChatMoment moment)
    {
        if (slot == null || moment == null)
        {
            return;
        }

        ChatContentType contentType = ResolveContentType(slot, moment);
        bool showTalk = contentType == ChatContentType.Talk;
        bool showEmote = contentType == ChatContentType.Emote;
        bool showVideo = contentType == ChatContentType.Video;

        if (slot.talkRoot != null)
        {
            slot.talkRoot.gameObject.SetActive(showTalk);
            TMP_Text talk = slot.talkRoot.GetComponentInChildren<TMP_Text>(true);
            if (showTalk && talk != null)
            {
                talk.text = moment.text;
            }
        }

        if (slot.emoteRoot != null)
        {
            slot.emoteRoot.gameObject.SetActive(showEmote);
            Image emoteImage = slot.emoteRoot.GetComponentInChildren<Image>(true);
            if (showEmote && emoteImage != null)
            {
                emoteImage.sprite = GetRandomEmote(moment);
                emoteImage.preserveAspect = true;
            }
        }

        if (slot.videoRoot != null)
        {
            slot.videoRoot.gameObject.SetActive(showVideo);
        }
    }

    private ChatContentType ResolveContentType(
        ChatUIFollowController.ChatSlot slot,
        ChatMoment moment)
    {
        if (moment.contentType != ChatContentType.Auto &&
            IsContentAvailable(slot, moment, moment.contentType))
        {
            return moment.contentType;
        }

        float availableTalkWeight = IsContentAvailable(slot, moment, ChatContentType.Talk)
            ? Mathf.Max(0f, talkWeight)
            : 0f;
        float availableEmoteWeight = IsContentAvailable(slot, moment, ChatContentType.Emote)
            ? Mathf.Max(0f, emoteWeight)
            : 0f;
        float availableVideoWeight = IsContentAvailable(slot, moment, ChatContentType.Video)
            ? Mathf.Max(0f, videoWeight)
            : 0f;
        float totalWeight = availableTalkWeight + availableEmoteWeight + availableVideoWeight;

        if (totalWeight <= 0f)
        {
            return ChatContentType.Talk;
        }

        float roll = UnityEngine.Random.value * totalWeight;
        if (roll < availableEmoteWeight)
        {
            return ChatContentType.Emote;
        }

        roll -= availableEmoteWeight;
        if (roll < availableTalkWeight)
        {
            return ChatContentType.Talk;
        }

        return ChatContentType.Video;
    }

    private static bool IsContentAvailable(
        ChatUIFollowController.ChatSlot slot,
        ChatMoment moment,
        ChatContentType contentType)
    {
        switch (contentType)
        {
            case ChatContentType.Talk:
                return slot.talkRoot != null && !string.IsNullOrWhiteSpace(moment.text);
            case ChatContentType.Emote:
                return slot.emoteRoot != null && moment.emotes != null && moment.emotes.Length > 0;
            case ChatContentType.Video:
                return slot.videoRoot != null && moment.videoAvailable;
            default:
                return false;
        }
    }

    private static Sprite GetRandomEmote(ChatMoment moment)
    {
        if (moment.emotes == null || moment.emotes.Length == 0)
        {
            return null;
        }

        return moment.emotes[UnityEngine.Random.Range(0, moment.emotes.Length)];
    }

    private static float RandomDuration(Vector2 range)
    {
        float minimum = Mathf.Max(0f, Mathf.Min(range.x, range.y));
        float maximum = Mathf.Max(minimum, Mathf.Max(range.x, range.y));
        return UnityEngine.Random.Range(minimum, maximum);
    }

    private void OnDisable()
    {
        if (testRoutine != null)
        {
            StopCoroutine(testRoutine);
            testRoutine = null;
        }

        ReleaseCurrentChat();
    }

    private void ReleaseCurrentChat()
    {
        if (chatController != null && currentSlot != null &&
            string.Equals(currentSlot.ActiveUserKidId, kidId, StringComparison.OrdinalIgnoreCase))
        {
            lastChatId = currentSlot.chatId;
            chatController.ReleaseChat(currentSlot.chatId);
        }

        currentSlot = null;
    }
}
