using System;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class KidCharacterInfoCards : MonoBehaviour
{
    [Serializable]
    public sealed class KidCardBinding
    {
        [Header("Kid")]
        public string kidId = "Kid1";
        public KidWaypointAnimationTester activityController;
        public KidDeviceUsageController deviceUsageController;

        [Header("Card Content")]
        public string displayName = "Noah";
        [TextArea] public string personality = "Calm and curious";
        [TextArea] public string specialTrait = "Phone explorer";

        [Header("Prebuilt Card UI")]
        public RectTransform cardRoot;
        public CanvasGroup cardCanvasGroup;
        public TMP_Text nameText;
        public TMP_Text personalityText;
        public TMP_Text moodText;
        public TMP_Text specialTraitText;

        [NonSerialized] public KidWaypointAnimationTester.EmotionState displayedEmotion;
        [NonSerialized] public KidDeviceUsageController.DeviceActivity displayedDeviceActivity;
        [NonSerialized] public bool hasDisplayedState;

        public bool IsValid => !string.IsNullOrWhiteSpace(kidId) &&
                               activityController != null && deviceUsageController != null &&
                               cardRoot != null && cardCanvasGroup != null && nameText != null &&
                               personalityText != null && moodText != null && specialTraitText != null;
    }

    [Header("Scene References")]
    [SerializeField] private KidFocusCameraController kidFocusController;
    [SerializeField] private TelevisionFocusCameraController televisionFocusController;
    [SerializeField] private Camera kidFocusCamera;

    [Header("Prebuilt Cards")]
    [SerializeField] private KidCardBinding[] kids = Array.Empty<KidCardBinding>();

    [Header("Visibility")]
    [SerializeField, Tooltip("Keep the information card hidden while the focused Kid's phone UI is open.")]
    private bool hideWhilePhoneVisible;

    [Header("Mood Text")]
    [SerializeField] private string stableMoodText = "Stable";
    [SerializeField] private string happyMoodText = "Happy";
    [SerializeField] private string suspiciousMoodText = "Suspicious";
    [SerializeField] private string anxiousMoodText = "Anxious";
    [SerializeField] private string panicMoodText = "Panic";
    [SerializeField] private string watchingPhoneSuffix = " / Phone";
    [SerializeField] private string watchingTelevisionSuffix = " / TV";
    [SerializeField] private bool appendCurrentDeviceToMood = true;

    private KidCardBinding visibleCard;

    private void Awake()
    {
        ValidatePrebuiltReferences();
        HideAllCards();
    }

    private void LateUpdate()
    {
        KidCardBinding nextCard = ResolveFocusedCard();
        if (visibleCard != nextCard)
        {
            SetCardVisible(visibleCard, false);
            visibleCard = nextCard;
            if (visibleCard != null)
            {
                visibleCard.hasDisplayedState = false;
                RefreshStaticText(visibleCard);
                RefreshMoodText(visibleCard, true);
                SetCardVisible(visibleCard, true);
            }
        }

        if (visibleCard != null)
        {
            RefreshMoodText(visibleCard, false);
        }
    }

    private KidCardBinding ResolveFocusedCard()
    {
        if (kidFocusController == null || !kidFocusController.IsFocusing ||
            kidFocusCamera == null || !kidFocusCamera.gameObject.activeInHierarchy ||
            (televisionFocusController != null && televisionFocusController.IsFocusing) ||
            (hideWhilePhoneVisible && kidFocusController.IsPhoneScreenVisible))
        {
            return null;
        }

        string selectedKidId = kidFocusController.SelectedKidId;
        if (string.IsNullOrWhiteSpace(selectedKidId) || kids == null)
        {
            return null;
        }

        foreach (KidCardBinding kid in kids)
        {
            if (kid != null && kid.IsValid &&
                string.Equals(kid.kidId, selectedKidId, StringComparison.OrdinalIgnoreCase))
            {
                return kid;
            }
        }

        return null;
    }

    private static void RefreshStaticText(KidCardBinding card)
    {
        card.nameText.text = card.displayName;
        card.personalityText.text = card.personality;
        card.specialTraitText.text = card.specialTrait;
    }

    private void RefreshMoodText(KidCardBinding card, bool force)
    {
        KidWaypointAnimationTester.EmotionState emotion = card.activityController.VisualEmotion;
        KidDeviceUsageController.DeviceActivity deviceActivity = ResolveDeviceActivity(card.deviceUsageController);
        if (!force && card.hasDisplayedState && card.displayedEmotion == emotion &&
            card.displayedDeviceActivity == deviceActivity)
        {
            return;
        }

        card.displayedEmotion = emotion;
        card.displayedDeviceActivity = deviceActivity;
        card.hasDisplayedState = true;

        string mood = ResolveMoodText(emotion);
        if (appendCurrentDeviceToMood)
        {
            if (deviceActivity == KidDeviceUsageController.DeviceActivity.Phone)
            {
                mood += watchingPhoneSuffix;
            }
            else if (deviceActivity == KidDeviceUsageController.DeviceActivity.Television)
            {
                mood += watchingTelevisionSuffix;
            }
        }

        card.moodText.text = mood;
    }

    private string ResolveMoodText(KidWaypointAnimationTester.EmotionState emotion)
    {
        switch (emotion)
        {
            case KidWaypointAnimationTester.EmotionState.Happy:
                return happyMoodText;
            case KidWaypointAnimationTester.EmotionState.Suspicious:
                return suspiciousMoodText;
            case KidWaypointAnimationTester.EmotionState.Anxious:
                return anxiousMoodText;
            case KidWaypointAnimationTester.EmotionState.Panic:
                return panicMoodText;
            default:
                return stableMoodText;
        }
    }

    private static KidDeviceUsageController.DeviceActivity ResolveDeviceActivity(
        KidDeviceUsageController deviceUsageController)
    {
        if (deviceUsageController == null)
        {
            return KidDeviceUsageController.DeviceActivity.None;
        }

        if (deviceUsageController.IsWatchingPhone)
        {
            return KidDeviceUsageController.DeviceActivity.Phone;
        }

        return deviceUsageController.IsWatchingTelevision
            ? KidDeviceUsageController.DeviceActivity.Television
            : KidDeviceUsageController.DeviceActivity.None;
    }

    private void HideAllCards()
    {
        visibleCard = null;
        if (kids == null)
        {
            return;
        }

        foreach (KidCardBinding kid in kids)
        {
            SetCardVisible(kid, false);
        }
    }

    private static void SetCardVisible(KidCardBinding card, bool visible)
    {
        if (card?.cardRoot == null || card.cardCanvasGroup == null)
        {
            return;
        }

        GameObject cardObject = card.cardRoot.gameObject;
        if (visible && !cardObject.activeSelf)
        {
            cardObject.SetActive(true);
        }

        card.cardCanvasGroup.alpha = visible ? 1f : 0f;
        card.cardCanvasGroup.interactable = false;
        card.cardCanvasGroup.blocksRaycasts = false;

        if (!visible && cardObject.activeSelf)
        {
            cardObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        HideAllCards();
    }

    private void ValidatePrebuiltReferences()
    {
        if (kidFocusController == null || televisionFocusController == null || kidFocusCamera == null ||
            kids == null || kids.Length != 3)
        {
            Debug.LogError("Kid Character Info Cards requires the focus controllers, Kid_Forcus camera, and exactly three prebuilt card bindings assigned before Play.", this);
            return;
        }

        foreach (KidCardBinding kid in kids)
        {
            if (kid == null || !kid.IsValid)
            {
                Debug.LogError("Every Kid Character Info Card must be fully assigned before Play.", this);
                return;
            }
        }
    }
}
