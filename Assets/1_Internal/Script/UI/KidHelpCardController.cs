using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class KidHelpCardController : MonoBehaviour
{
    public enum HelpAction
    {
        Informational,
        DeepBreath,
        TakeScreenBreak,
        QuietPlace,
        HugFavorite,
        RelaxingMusic
    }

    [Serializable]
    public sealed class HelpCardBinding
    {
        public string cardId;
        public RectTransform cardRoot;
        public Button button;
        public RectTransform applicationPreviewRoot;
        public CanvasGroup applicationPreviewCanvasGroup;
        public HelpAction action;
        public bool simulateAction;
        [Tooltip("Prebuilt waypoint label used by the selected Kid. No scene object is created at runtime.")]
        public string destinationLabel = "walk_place";
        [Min(1f)] public float actionDurationSeconds = 20f;
        public string[] animationStates = Array.Empty<string>();

        [NonSerialized] public float verticalVelocity;

        public bool IsValid => !string.IsNullOrWhiteSpace(cardId) &&
                               cardRoot != null && button != null;
    }

    [Header("Scene References")]
    [SerializeField] private KidFocusCameraController kidFocusController;
    [SerializeField] private TelevisionFocusCameraController televisionFocusController;
    [SerializeField] private Camera kidFocusCamera;

    [Header("Prebuilt Help Card UI")]
    [SerializeField] private RectTransform cardArea;
    [SerializeField] private CanvasGroup cardAreaCanvasGroup;
    [SerializeField] private HelpCardBinding[] cards = Array.Empty<HelpCardBinding>();

    [Header("Visibility")]
    [SerializeField] private bool hideWhilePhoneVisible = true;
    [SerializeField, Tooltip("Keep every card hidden until the reveal key is pressed while Kid_Forcus is active.")]
    private bool requireRevealKey = true;
    [SerializeField] private Key revealKey = Key.T;
    [SerializeField] private bool hideAgainWhenKidFocusEnds = true;

    [Header("Peek And Hover")]
    [SerializeField, Tooltip("Bottom-anchored Y used when only the card header should peek above the screen edge.")]
    private float collapsedAnchoredY = -520f;
    [SerializeField, Tooltip("Bottom-anchored Y used while the pointer is over the card.")]
    private float expandedAnchoredY = 18f;
    [SerializeField, Min(0f)] private float hoverSmoothTime = 0.12f;

    [Header("Applied Card Preview")]
    [SerializeField, Min(0.1f)] private float previewDurationSeconds = 1.6f;
    [SerializeField, Min(0.01f)] private float previewStartScale = 0.72f;
    [SerializeField, Min(0.01f)] private float previewEndScale = 1.28f;
    [SerializeField, Range(0f, 0.95f)] private float previewFadeStartNormalized = 0.35f;

    private bool cardsVisible;
    private bool headersRevealed;
    private Coroutine appliedPreviewRoutine;

    private void Awake()
    {
        ValidatePrebuiltReferences();
        BindPrebuiltButtons();
        SetCardsVisible(false);
        HideAllApplicationPreviews();
        SnapAllCards(collapsedAnchoredY);
    }

    private void LateUpdate()
    {
        bool canShow = CanShowCards();
        if (!canShow && hideAgainWhenKidFocusEnds)
        {
            headersRevealed = false;
        }

        if (!canShow && appliedPreviewRoutine != null)
        {
            StopCoroutine(appliedPreviewRoutine);
            appliedPreviewRoutine = null;
            HideAllApplicationPreviews();
        }

        if (canShow && Keyboard.current != null &&
            revealKey != Key.None &&
            Keyboard.current[revealKey].wasPressedThisFrame)
        {
            headersRevealed = !headersRevealed;
        }

        bool shouldShow = canShow && (!requireRevealKey || headersRevealed);
        if (cardsVisible != shouldShow)
        {
            SetCardsVisible(shouldShow);
        }

        if (!cardsVisible)
        {
            return;
        }

        Vector2 pointerPosition = Pointer.current != null
            ? Pointer.current.position.ReadValue()
            : new Vector2(float.MinValue, float.MinValue);

        for (int index = 0; index < cards.Length; index++)
        {
            HelpCardBinding card = cards[index];
            if (card?.cardRoot == null)
            {
                continue;
            }

            bool hovered = Pointer.current != null &&
                           RectTransformUtility.RectangleContainsScreenPoint(
                               card.cardRoot, pointerPosition, null);
            float targetY = hovered ? expandedAnchoredY : collapsedAnchoredY;
            Vector2 anchoredPosition = card.cardRoot.anchoredPosition;
            if (hoverSmoothTime <= 0f)
            {
                anchoredPosition.y = targetY;
                card.verticalVelocity = 0f;
            }
            else
            {
                anchoredPosition.y = Mathf.SmoothDamp(
                    anchoredPosition.y,
                    targetY,
                    ref card.verticalVelocity,
                    hoverSmoothTime,
                    Mathf.Infinity,
                    Time.unscaledDeltaTime);
            }

            card.cardRoot.anchoredPosition = anchoredPosition;
        }
    }

    private bool CanShowCards()
    {
        return kidFocusController != null && kidFocusController.IsFocusing &&
               kidFocusCamera != null && kidFocusCamera.gameObject.activeInHierarchy &&
               (televisionFocusController == null || !televisionFocusController.IsFocusing) &&
               (!hideWhilePhoneVisible || !kidFocusController.IsPhoneScreenVisible);
    }

    private void BindPrebuiltButtons()
    {
        if (cards == null)
        {
            return;
        }

        foreach (HelpCardBinding card in cards)
        {
            if (card?.button == null)
            {
                continue;
            }

            HelpCardBinding capturedCard = card;
            card.button.interactable = card.simulateAction;
            if (card.simulateAction)
            {
                card.button.onClick.AddListener(() => ExecuteCard(capturedCard));
            }
        }
    }

    private void ExecuteCard(HelpCardBinding card)
    {
        if (card == null || !card.simulateAction || kidFocusController == null)
        {
            return;
        }

        KidWaypointAnimationTester selectedKid =
            kidFocusController.SelectedKidActivityController;
        if (selectedKid == null)
        {
            return;
        }

        kidFocusController.SetPhoneScreenVisible(false);
        if (!selectedKid.TryStartGuidedHelpAction(
            card.destinationLabel,
            card.animationStates,
            card.actionDurationSeconds))
        {
            return;
        }

        headersRevealed = false;
        SetCardsVisible(false);
        ShowApplicationPreview(card);
    }

    private void ShowApplicationPreview(HelpCardBinding card)
    {
        if (card?.applicationPreviewRoot == null ||
            card.applicationPreviewCanvasGroup == null)
        {
            return;
        }

        if (appliedPreviewRoutine != null)
        {
            StopCoroutine(appliedPreviewRoutine);
        }

        HideAllApplicationPreviews();
        appliedPreviewRoutine = StartCoroutine(AnimateApplicationPreview(card));
    }

    private IEnumerator AnimateApplicationPreview(HelpCardBinding card)
    {
        GameObject previewObject = card.applicationPreviewRoot.gameObject;
        previewObject.SetActive(true);
        card.applicationPreviewCanvasGroup.alpha = 1f;
        card.applicationPreviewRoot.localScale = Vector3.one * previewStartScale;

        float elapsed = 0f;
        while (elapsed < previewDurationSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(elapsed / previewDurationSeconds);
            float eased = 1f - Mathf.Pow(1f - normalized, 3f);
            card.applicationPreviewRoot.localScale = Vector3.one *
                                                     Mathf.Lerp(previewStartScale, previewEndScale, eased);

            float fadeProgress = Mathf.InverseLerp(
                previewFadeStartNormalized, 1f, normalized);
            card.applicationPreviewCanvasGroup.alpha = 1f - fadeProgress;
            yield return null;
        }

        card.applicationPreviewCanvasGroup.alpha = 0f;
        previewObject.SetActive(false);
        appliedPreviewRoutine = null;
    }

    private void HideAllApplicationPreviews()
    {
        if (cards == null)
        {
            return;
        }

        foreach (HelpCardBinding card in cards)
        {
            if (card?.applicationPreviewRoot == null ||
                card.applicationPreviewCanvasGroup == null)
            {
                continue;
            }

            card.applicationPreviewCanvasGroup.alpha = 0f;
            card.applicationPreviewRoot.localScale = Vector3.one * previewStartScale;
            card.applicationPreviewRoot.gameObject.SetActive(false);
        }
    }

    private void SetCardsVisible(bool visible)
    {
        cardsVisible = visible;
        if (cardArea == null || cardAreaCanvasGroup == null)
        {
            return;
        }

        GameObject areaObject = cardArea.gameObject;
        if (visible && !areaObject.activeSelf)
        {
            areaObject.SetActive(true);
        }

        cardAreaCanvasGroup.alpha = visible ? 1f : 0f;
        cardAreaCanvasGroup.interactable = visible;
        cardAreaCanvasGroup.blocksRaycasts = visible;

        if (!visible)
        {
            SnapAllCards(collapsedAnchoredY);
            if (areaObject.activeSelf)
            {
                areaObject.SetActive(false);
            }
        }
    }

    private void SnapAllCards(float anchoredY)
    {
        if (cards == null)
        {
            return;
        }

        foreach (HelpCardBinding card in cards)
        {
            if (card?.cardRoot == null)
            {
                continue;
            }

            Vector2 position = card.cardRoot.anchoredPosition;
            position.y = anchoredY;
            card.cardRoot.anchoredPosition = position;
            card.verticalVelocity = 0f;
        }
    }

    private void OnDisable()
    {
        if (appliedPreviewRoutine != null)
        {
            StopCoroutine(appliedPreviewRoutine);
            appliedPreviewRoutine = null;
        }

        headersRevealed = false;
        HideAllApplicationPreviews();
        SetCardsVisible(false);
    }

    private void OnValidate()
    {
        hoverSmoothTime = Mathf.Max(0f, hoverSmoothTime);
        previewDurationSeconds = Mathf.Max(0.1f, previewDurationSeconds);
        previewStartScale = Mathf.Max(0.01f, previewStartScale);
        previewEndScale = Mathf.Max(0.01f, previewEndScale);
        if (cards == null)
        {
            return;
        }

        foreach (HelpCardBinding card in cards)
        {
            if (card != null)
            {
                card.actionDurationSeconds = Mathf.Max(1f, card.actionDurationSeconds);
            }
        }
    }

    private void ValidatePrebuiltReferences()
    {
        if (kidFocusController == null || televisionFocusController == null ||
            kidFocusCamera == null || cardArea == null ||
            cardAreaCanvasGroup == null || cards == null || cards.Length != 8)
        {
            Debug.LogError("Kid Help Cards requires both focus controllers, Kid_Forcus camera, its prebuilt card area, and exactly eight cards assigned before Play.", this);
            return;
        }

        foreach (HelpCardBinding card in cards)
        {
            if (card == null || !card.IsValid ||
                (card.simulateAction &&
                 (string.IsNullOrWhiteSpace(card.destinationLabel) ||
                  card.animationStates == null || card.animationStates.Length == 0 ||
                  card.applicationPreviewRoot == null ||
                  card.applicationPreviewCanvasGroup == null)))
            {
                Debug.LogError("Every Kid Help Card must have its prebuilt UI assigned; simulated cards also require a destination label and animation states before Play.", this);
                return;
            }
        }
    }
}
