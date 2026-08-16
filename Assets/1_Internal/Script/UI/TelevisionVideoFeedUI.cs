using System;
using System.Collections.Generic;
using GreekProject.Content;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GreekProject.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("UI/Television Video Feed UI")]
    public sealed class TelevisionVideoFeedUI : MonoBehaviour
    {
        private const float SequenceFramesPerSecond = 10f;
        private const int SequenceColumns = 8;
        private const int SequenceRows = 8;
        private const int FramesPerSheet = SequenceColumns * SequenceRows;

        [Serializable]
        private sealed class CardSlot
        {
            public RectTransform root;
            public Button openButton;
            public Button moreButton;
            public Image thumbnail;
            public TextMeshProUGUI mockImageNumber;
            public TextMeshProUGUI title;
            public TextMeshProUGUI metadata;
            public TextMeshProUGUI duration;
            public RectTransform removedOverlay;
        }

        private sealed class FrameSequence
        {
            public Texture2D[] sheets;
            public int frameCount;

            public float Duration => frameCount / SequenceFramesPerSecond;
        }

        [Serializable]
        private sealed class FrameSequenceManifest
        {
            public FrameSequenceManifestEntry[] videos;
        }

        [Serializable]
        private sealed class FrameSequenceManifestEntry
        {
            public string stem;
            public int frameCount;
        }

        [Header("Video Data")]
        [SerializeField] private VideoLibraryData videoLibrary;
        [SerializeField, Range(1, 6)] private int visibleVideoCount = 6;
        [SerializeField] private bool randomizeInitialVideos = true;

        [Header("Prebuilt Feed References")]
        [SerializeField] private RectTransform feedRoot;
        [SerializeField] private CardSlot[] cardSlots = new CardSlot[6];

        [Header("Prebuilt Player References")]
        [SerializeField] private RectTransform playerRoot;
        [SerializeField] private Image playerThumbnail;
        [SerializeField] private TextMeshProUGUI playerMockImageNumber;
        [SerializeField] private RawImage playerVideoSurface;
        [SerializeField] private AspectRatioFitter playerVideoAspect;
        [SerializeField] private Button playerPlayPauseButton;
        [SerializeField] private PlaybackControlGraphic playerPlayPauseIcon;
        [SerializeField] private Slider playerProgress;
        [SerializeField] private Button playerCloseButton;

        [Header("Prebuilt Video Options References")]
        [SerializeField] private RectTransform videoOptionsBackdrop;
        [SerializeField] private Button videoOptionsBackdropButton;
        [SerializeField] private RectTransform videoOptionsPanel;
        [SerializeField] private Button suggestMoreButton;
        [SerializeField] private Button doNotSuggestButton;
        [SerializeField, Range(0.5f, 1f)] private float optionsPanelWidthRatio = 0.5f;
        [SerializeField, Range(0.25f, 0.75f)] private float optionsPanelHeightRatio = 0.5f;
        [SerializeField, Min(0f)] private float optionsPanelVerticalGap = 0.006f;

        [Header("TV Broadcast")]
        [SerializeField] private bool autoPlayWhenNotFocused = true;
        [SerializeField, Min(1f)] private float minimumSecondsBeforeRotation = 15f;
        [SerializeField, Min(1f)] private float maximumSecondsBeforeRotation = 15f;
        [SerializeField, Tooltip("Replace the complete six-card feed only after the timed broadcast interval.")]
        private bool replaceEntireFeedAfterTimedRotation = true;

        [Header("TV Layout - Edit Mode")]
        [SerializeField, Range(0.04f, 0.18f)] private float sidebarWidth = 0.075f;
        [SerializeField, Range(0.08f, 0.22f)] private float headerHeight = 0.14f;
        [SerializeField, Range(0f, 0.05f)] private float horizontalGap = 0.018f;
        [SerializeField, Range(0f, 0.08f)] private float verticalGap = 0.045f;
        [SerializeField] private Color screenColor = Color.white;
        [SerializeField] private Color cardColor = new Color(0.965f, 0.965f, 0.965f, 1f);
        [SerializeField] private Color primaryTextColor = new Color(0.06f, 0.06f, 0.06f, 1f);
        [SerializeField] private Color secondaryTextColor = new Color(0.36f, 0.36f, 0.36f, 1f);
        [SerializeField] private Color accentColor = new Color(1f, 0f, 0.12f, 1f);

        private readonly Dictionary<VideoLibraryData.VideoEntry, FrameSequence> frameSequences = new();
        private readonly List<VideoLibraryData.VideoEntry> visibleVideos = new();
        private readonly List<UnityAction> cardClickActions = new();
        private readonly List<Button> boundCardButtons = new();
        private readonly List<UnityAction> moreClickActions = new();
        private readonly List<Button> boundMoreButtons = new();
        private readonly HashSet<string> hiddenVideoIds = new(StringComparer.OrdinalIgnoreCase);
        private VideoLibraryData.VideoEntry activeVideo;
        private VideoLibraryData.VideoEntry optionsVideo;
        private FrameSequence activeSequence;
        private bool playbackRequested;
        private bool suppressProgressCallback;
        private bool interactionEnabled;
        private bool televisionFocused;
        private bool runtimeInitialized;
        private float sequenceTime;
        private float broadcastElapsedTime;
        private float secondsBeforeRotation = 10f;
        private int displayedSequenceFrame = -1;

        private void Awake()
        {
            if (!ValidatePrebuiltReferences())
            {
                enabled = false;
                return;
            }

            LoadFrameSequences();
            SelectInitialVideos();
            BindPrebuiltCards();
            BindPlayerControls();
            BindVideoOptionsControls();
            CloseVideoOptions();
            runtimeInitialized = true;
            EnsureBroadcastPlaying();
        }

        private void Update()
        {
            if (activeSequence == null || activeVideo == null)
            {
                return;
            }

            float duration = activeSequence.Duration;
            if (duration <= 0f)
            {
                return;
            }

            if (playbackRequested)
            {
                float deltaTime = Time.unscaledDeltaTime;
                sequenceTime += deltaTime;
                if (!televisionFocused)
                {
                    broadcastElapsedTime += deltaTime;
                }
                if (sequenceTime >= duration)
                {
                    sequenceTime = Mathf.Repeat(sequenceTime, duration);
                    displayedSequenceFrame = -1;
                }

                if (!televisionFocused && broadcastElapsedTime >= secondsBeforeRotation)
                {
                    CompleteCurrentBroadcast();
                    return;
                }
            }

            int frameIndex = Mathf.Min(Mathf.FloorToInt(sequenceTime * SequenceFramesPerSecond),
                activeSequence.frameCount - 1);
            ApplySequenceFrame(frameIndex);

            suppressProgressCallback = true;
            playerProgress.value = Mathf.Clamp01(sequenceTime / duration);
            suppressProgressCallback = false;
        }

        private void OnDestroy()
        {
            UnbindPrebuiltCards();

            playerPlayPauseButton?.onClick.RemoveListener(TogglePlayback);
            playerCloseButton?.onClick.RemoveListener(ClosePlayer);
            playerProgress?.onValueChanged.RemoveListener(SeekVideo);
            suggestMoreButton?.onClick.RemoveListener(CloseVideoOptions);
            doNotSuggestButton?.onClick.RemoveListener(HideSelectedVideo);
            videoOptionsBackdropButton?.onClick.RemoveListener(CloseVideoOptions);
            if (playerVideoSurface != null)
            {
                playerVideoSurface.texture = null;
            }
        }

        private bool ValidatePrebuiltReferences()
        {
            if (videoLibrary == null || feedRoot == null || cardSlots == null || cardSlots.Length < 6 ||
                playerRoot == null || playerThumbnail == null || playerMockImageNumber == null ||
                playerVideoSurface == null || playerVideoAspect == null || playerPlayPauseButton == null ||
                playerPlayPauseIcon == null || playerProgress == null || playerCloseButton == null ||
                videoOptionsBackdrop == null || videoOptionsBackdropButton == null || videoOptionsPanel == null ||
                suggestMoreButton == null || doNotSuggestButton == null)
            {
                Debug.LogError("Television Video Feed UI requires every prebuilt scene reference before Play.", this);
                return false;
            }

            for (int index = 0; index < 6; index++)
            {
                CardSlot slot = cardSlots[index];
                if (slot == null || slot.root == null || slot.openButton == null || slot.moreButton == null ||
                    slot.thumbnail == null ||
                    slot.mockImageNumber == null || slot.title == null || slot.metadata == null || slot.duration == null ||
                    slot.removedOverlay == null)
                {
                    Debug.LogError($"Television card slot {index + 1} is not fully assigned before Play.", this);
                    return false;
                }
            }

            return true;
        }

        private void SelectInitialVideos()
        {
            visibleVideos.Clear();
            List<VideoLibraryData.VideoEntry> candidates = new(videoLibrary.Videos);
            candidates.RemoveAll(video => video == null || IsHidden(video));
            if (randomizeInitialVideos)
            {
                Shuffle(candidates);
            }

            int count = Mathf.Min(visibleVideoCount, cardSlots.Length, candidates.Count);
            for (int index = 0; index < count; index++)
            {
                visibleVideos.Add(candidates[index]);
            }
        }

        private void BindPrebuiltCards()
        {
            UnbindPrebuiltCards();
            for (int index = 0; index < cardSlots.Length; index++)
            {
                CardSlot slot = cardSlots[index];
                bool hasVideo = index < visibleVideos.Count;
                VideoLibraryData.VideoEntry video = hasVideo ? visibleVideos[index] : null;
                bool hasVisibleSlot = hasVideo && index < visibleVideoCount;
                slot.root.gameObject.SetActive(hasVisibleSlot);
                if (!hasVisibleSlot)
                {
                    continue;
                }

                bool isRemoved = IsHidden(video);
                slot.removedOverlay.gameObject.SetActive(isRemoved);
                slot.removedOverlay.SetAsLastSibling();
                if (isRemoved)
                {
                    slot.openButton.interactable = false;
                    slot.moreButton.interactable = false;
                    continue;
                }

                ApplyVideoToSlot(slot, video);
                slot.openButton.interactable = interactionEnabled;
                slot.moreButton.interactable = interactionEnabled;
                UnityAction action = () => OpenVideo(video);
                RectTransform moreRect = slot.moreButton.transform as RectTransform;
                UnityAction moreAction = () => ShowVideoOptions(video, slot.root, moreRect);
                cardClickActions.Add(action);
                boundCardButtons.Add(slot.openButton);
                moreClickActions.Add(moreAction);
                boundMoreButtons.Add(slot.moreButton);
                slot.openButton.onClick.AddListener(action);
                slot.moreButton.onClick.AddListener(moreAction);
            }
        }

        private void UnbindPrebuiltCards()
        {
            for (int index = 0; index < cardClickActions.Count && index < boundCardButtons.Count; index++)
            {
                if (boundCardButtons[index] != null)
                {
                    boundCardButtons[index].onClick.RemoveListener(cardClickActions[index]);
                }
            }

            cardClickActions.Clear();
            boundCardButtons.Clear();

            for (int index = 0; index < moreClickActions.Count && index < boundMoreButtons.Count; index++)
            {
                if (boundMoreButtons[index] != null)
                {
                    boundMoreButtons[index].onClick.RemoveListener(moreClickActions[index]);
                }
            }

            moreClickActions.Clear();
            boundMoreButtons.Clear();
        }

        public void RandomizeVisibleVideos()
        {
            if (videoLibrary == null || cardSlots == null || cardSlots.Length == 0)
            {
                return;
            }

            HidePlayerWithoutReplacement();
            ReplaceEntireVisibleFeed();
            BindPrebuiltCards();
            EnsureBroadcastPlaying();
        }

        public void SetInteractionEnabled(bool enabledState)
        {
            interactionEnabled = enabledState;
            for (int index = 0; index < cardSlots.Length; index++)
            {
                CardSlot slot = cardSlots[index];
                VideoLibraryData.VideoEntry video = index < visibleVideos.Count ? visibleVideos[index] : null;
                bool cardCanInteract = enabledState && video != null && !IsHidden(video);
                if (slot?.openButton != null)
                {
                    slot.openButton.interactable = cardCanInteract;
                }

                if (slot?.moreButton != null)
                {
                    slot.moreButton.interactable = cardCanInteract;
                }
            }

            if (playerPlayPauseButton != null) playerPlayPauseButton.interactable = enabledState;
            if (playerProgress != null) playerProgress.interactable = enabledState;
            if (playerCloseButton != null) playerCloseButton.interactable = enabledState;
            if (suggestMoreButton != null) suggestMoreButton.interactable = enabledState;
            if (doNotSuggestButton != null) doNotSuggestButton.interactable = enabledState;
            if (videoOptionsBackdropButton != null) videoOptionsBackdropButton.interactable = enabledState;

            if (!enabledState)
            {
                CloseVideoOptions();
            }
        }

        public void SetTelevisionFocused(bool focused)
        {
            televisionFocused = focused;
            SetInteractionEnabled(focused);

            if (!focused && runtimeInitialized)
            {
                EnsureBroadcastPlaying();
            }
        }

        private void ApplyVideoToSlot(CardSlot slot, VideoLibraryData.VideoEntry video)
        {
            bool hasThumbnail = video.thumbnail != null;
            slot.thumbnail.sprite = video.thumbnail;
            slot.thumbnail.color = hasThumbnail ? Color.white : video.mockColor;
            slot.thumbnail.preserveAspect = hasThumbnail;
            slot.mockImageNumber.text = video.mockImageNumber.ToString("00");
            slot.mockImageNumber.gameObject.SetActive(!hasThumbnail);
            slot.title.text = video.title;
            slot.metadata.text = video.Metadata;
            slot.duration.text = GetDurationLabel(video);
        }

        private void BindPlayerControls()
        {
            playerPlayPauseButton.onClick.AddListener(TogglePlayback);
            playerCloseButton.onClick.AddListener(ClosePlayer);
            playerProgress.onValueChanged.AddListener(SeekVideo);
        }

        private void BindVideoOptionsControls()
        {
            suggestMoreButton.onClick.AddListener(CloseVideoOptions);
            doNotSuggestButton.onClick.AddListener(HideSelectedVideo);
            videoOptionsBackdropButton.onClick.AddListener(CloseVideoOptions);
        }

        private void ShowVideoOptions(VideoLibraryData.VideoEntry video, RectTransform card, RectTransform moreRect)
        {
            if (!interactionEnabled || video == null || card == null || moreRect == null || IsHidden(video))
            {
                return;
            }

            optionsVideo = video;
            PositionVideoOptions(card, moreRect);
            videoOptionsBackdrop.gameObject.SetActive(true);
            videoOptionsBackdrop.SetAsLastSibling();
            videoOptionsPanel.gameObject.SetActive(true);
            videoOptionsPanel.SetAsLastSibling();
        }

        private void CloseVideoOptions()
        {
            optionsVideo = null;
            if (videoOptionsBackdrop != null)
            {
                videoOptionsBackdrop.gameObject.SetActive(false);
            }

            if (videoOptionsPanel != null)
            {
                videoOptionsPanel.gameObject.SetActive(false);
            }
        }

        private void PositionVideoOptions(RectTransform card, RectTransform moreRect)
        {
            RectTransform panelParent = videoOptionsPanel.parent as RectTransform;
            if (panelParent == null)
            {
                return;
            }

            Vector3[] worldCorners = new Vector3[4];
            card.GetWorldCorners(worldCorners);
            Vector3 bottomLeft = panelParent.InverseTransformPoint(worldCorners[0]);
            Vector3 topLeft = panelParent.InverseTransformPoint(worldCorners[1]);
            Vector3 bottomRight = panelParent.InverseTransformPoint(worldCorners[3]);
            Vector3[] moreWorldCorners = new Vector3[4];
            moreRect.GetWorldCorners(moreWorldCorners);
            Vector3 moreBottomLeft = panelParent.InverseTransformPoint(moreWorldCorners[0]);
            Vector3 moreTopRight = panelParent.InverseTransformPoint(moreWorldCorners[2]);

            float cardWidth = Mathf.Abs(bottomRight.x - bottomLeft.x);
            float cardHeight = Mathf.Abs(topLeft.y - bottomLeft.y);
            float panelWidth = cardWidth * Mathf.Clamp(optionsPanelWidthRatio, 0.5f, 1f);
            float panelHeight = cardHeight * Mathf.Clamp(optionsPanelHeightRatio, 0.25f, 0.75f);
            Rect parentRect = panelParent.rect;
            panelWidth = Mathf.Min(panelWidth, parentRect.width);
            panelHeight = Mathf.Min(panelHeight, parentRect.height);

            float left = Mathf.Clamp(moreTopRight.x - panelWidth, parentRect.xMin, parentRect.xMax - panelWidth);
            float top = moreBottomLeft.y - optionsPanelVerticalGap;
            if (top - panelHeight < parentRect.yMin)
            {
                top = moreTopRight.y + optionsPanelVerticalGap + panelHeight;
            }

            top = Mathf.Clamp(top, parentRect.yMin + panelHeight, parentRect.yMax);
            videoOptionsPanel.anchorMin = new Vector2(0.5f, 0.5f);
            videoOptionsPanel.anchorMax = new Vector2(0.5f, 0.5f);
            videoOptionsPanel.pivot = new Vector2(0f, 1f);
            videoOptionsPanel.sizeDelta = new Vector2(panelWidth, panelHeight);
            videoOptionsPanel.anchoredPosition = new Vector2(left, top);
        }

        private void HideSelectedVideo()
        {
            VideoLibraryData.VideoEntry videoToHide = optionsVideo;
            CloseVideoOptions();
            if (videoToHide == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(videoToHide.id))
            {
                hiddenVideoIds.Add(videoToHide.id);
            }
            if (activeVideo == videoToHide)
            {
                HidePlayerWithoutReplacement();
            }

            BindPrebuiltCards();
            if (!televisionFocused)
            {
                EnsureBroadcastPlaying();
            }
        }

        private void OpenVideo(VideoLibraryData.VideoEntry video)
        {
            if (!interactionEnabled || video == null || IsHidden(video))
            {
                return;
            }

            StartVideo(video);
        }

        private void StartVideo(VideoLibraryData.VideoEntry video)
        {
            CloseVideoOptions();
            activeVideo = video;
            frameSequences.TryGetValue(video, out activeSequence);
            sequenceTime = 0f;
            broadcastElapsedTime = 0f;
            displayedSequenceFrame = -1;
            float minimumSeconds = Mathf.Max(1f, minimumSecondsBeforeRotation);
            float maximumSeconds = Mathf.Max(minimumSeconds, maximumSecondsBeforeRotation);
            secondsBeforeRotation = UnityEngine.Random.Range(minimumSeconds, maximumSeconds);
            playbackRequested = activeSequence != null;

            bool hasThumbnail = video.thumbnail != null;
            playerThumbnail.sprite = video.thumbnail;
            playerThumbnail.color = hasThumbnail ? Color.white : video.mockColor;
            playerThumbnail.preserveAspect = hasThumbnail;
            playerMockImageNumber.text = video.mockImageNumber.ToString("00");
            playerMockImageNumber.gameObject.SetActive(!hasThumbnail);
            playerThumbnail.enabled = activeSequence == null;
            playerVideoSurface.gameObject.SetActive(activeSequence != null);
            playerPlayPauseButton.interactable = interactionEnabled && activeSequence != null;
            playerPlayPauseIcon.IsPlaying = playbackRequested;
            suppressProgressCallback = true;
            playerProgress.value = 0f;
            suppressProgressCallback = false;
            playerVideoAspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            playerVideoAspect.aspectRatio = 16f / 9f;

            if (activeSequence != null)
            {
                ApplySequenceFrame(0);
            }

            playerRoot.gameObject.SetActive(true);
            playerRoot.SetAsLastSibling();
        }

        public void ClosePlayer()
        {
            HidePlayerWithoutReplacement();

            if (!televisionFocused)
            {
                EnsureBroadcastPlaying();
            }
        }

        private void CompleteCurrentBroadcast()
        {
            HidePlayerWithoutReplacement();
            if (replaceEntireFeedAfterTimedRotation)
            {
                ReplaceEntireVisibleFeed();
                BindPrebuiltCards();
            }

            // While focused, finishing a video returns to the six-card feed. Outside focus,
            // the television immediately starts another random frame-sequence broadcast.
            if (!televisionFocused)
            {
                EnsureBroadcastPlaying();
            }
        }

        private void HidePlayerWithoutReplacement()
        {
            activeVideo = null;
            activeSequence = null;
            playbackRequested = false;
            displayedSequenceFrame = -1;
            broadcastElapsedTime = 0f;
            playerPlayPauseIcon.IsPlaying = false;
            playerVideoSurface.texture = null;
            playerVideoSurface.gameObject.SetActive(false);
            playerRoot.gameObject.SetActive(false);
        }

        private void EnsureBroadcastPlaying()
        {
            if (!runtimeInitialized || !autoPlayWhenNotFocused || televisionFocused)
            {
                return;
            }

            if (activeVideo != null && activeSequence != null)
            {
                playbackRequested = true;
                playerPlayPauseIcon.IsPlaying = true;
                playerRoot.gameObject.SetActive(true);
                playerRoot.SetAsLastSibling();
                return;
            }

            List<VideoLibraryData.VideoEntry> playable = new();
            foreach (VideoLibraryData.VideoEntry video in visibleVideos)
            {
                if (video != null && !IsHidden(video) && frameSequences.ContainsKey(video))
                {
                    playable.Add(video);
                }
            }

            if (playable.Count == 0)
            {
                Debug.LogError("TV cannot start its 10 FPS broadcast because none of the six visible videos has frame data.", this);
                return;
            }

            StartVideo(playable[UnityEngine.Random.Range(0, playable.Count)]);
        }

        private void ReplaceEntireVisibleFeed()
        {
            if (videoLibrary == null)
            {
                return;
            }

            List<VideoLibraryData.VideoEntry> previousVideos = new(visibleVideos);
            List<VideoLibraryData.VideoEntry> eligibleVideos = new();
            HashSet<string> eligibleIds = new(StringComparer.OrdinalIgnoreCase);
            foreach (VideoLibraryData.VideoEntry candidate in videoLibrary.Videos)
            {
                if (candidate != null && !string.IsNullOrWhiteSpace(candidate.id) && !IsHidden(candidate) &&
                    frameSequences.ContainsKey(candidate) && eligibleIds.Add(candidate.id))
                {
                    eligibleVideos.Add(candidate);
                }
            }

            List<VideoLibraryData.VideoEntry> freshCandidates =
                eligibleVideos.FindAll(video => !previousVideos.Contains(video));
            Shuffle(freshCandidates);
            visibleVideos.Clear();
            int targetCount = Mathf.Min(visibleVideoCount, cardSlots.Length, eligibleVideos.Count);
            for (int index = 0; index < freshCandidates.Count && visibleVideos.Count < targetCount; index++)
            {
                visibleVideos.Add(freshCandidates[index]);
            }

            if (visibleVideos.Count < targetCount)
            {
                eligibleVideos.RemoveAll(visibleVideos.Contains);
                Shuffle(eligibleVideos);
                for (int index = 0; index < eligibleVideos.Count && visibleVideos.Count < targetCount; index++)
                {
                    visibleVideos.Add(eligibleVideos[index]);
                }
            }
        }

        private bool IsHidden(VideoLibraryData.VideoEntry video)
        {
            return video != null && !string.IsNullOrWhiteSpace(video.id) && hiddenVideoIds.Contains(video.id);
        }

        private void TogglePlayback()
        {
            if (activeSequence == null)
            {
                return;
            }

            playbackRequested = !playbackRequested;
            playerPlayPauseIcon.IsPlaying = playbackRequested;
        }

        private void SeekVideo(float normalizedTime)
        {
            if (suppressProgressCallback || activeSequence == null || activeSequence.Duration <= 0f)
            {
                return;
            }

            sequenceTime = Mathf.Clamp01(normalizedTime) * activeSequence.Duration;
            int frameIndex = Mathf.Min(Mathf.FloorToInt(sequenceTime * SequenceFramesPerSecond),
                activeSequence.frameCount - 1);
            ApplySequenceFrame(frameIndex);
        }

        private void ApplySequenceFrame(int frameIndex)
        {
            if (activeSequence == null || frameIndex == displayedSequenceFrame)
            {
                return;
            }

            int sheetIndex = frameIndex / FramesPerSheet;
            if (sheetIndex < 0 || sheetIndex >= activeSequence.sheets.Length)
            {
                return;
            }

            int frameInSheet = frameIndex % FramesPerSheet;
            int column = frameInSheet % SequenceColumns;
            int row = frameInSheet / SequenceColumns;
            Texture2D sheet = activeSequence.sheets[sheetIndex];
            float cellWidth = 1f / SequenceColumns;
            float cellHeight = 1f / SequenceRows;
            float insetX = 0.5f / sheet.width;
            float insetY = 0.5f / sheet.height;
            playerVideoSurface.texture = sheet;
            playerVideoSurface.uvRect = new Rect(
                column * cellWidth + insetX,
                (SequenceRows - row - 1) * cellHeight + insetY,
                cellWidth - insetX * 2f,
                cellHeight - insetY * 2f);
            displayedSequenceFrame = frameIndex;
        }

        private void LoadFrameSequences()
        {
            Dictionary<string, int> frameCounts = LoadFrameCounts();
            foreach (VideoLibraryData.VideoEntry video in videoLibrary.Videos)
            {
                string stem = video?.sourceStem;
                if (string.IsNullOrWhiteSpace(stem))
                {
                    continue;
                }

                Texture2D[] sheets = Resources.LoadAll<Texture2D>($"VideoFrames/{stem}");
                Array.Sort(sheets, (left, right) => string.CompareOrdinal(left.name, right.name));
                frameCounts.TryGetValue(stem, out int frameCount);
                if (sheets.Length > 0 && frameCount > 0)
                {
                    frameSequences[video] = new FrameSequence { sheets = sheets, frameCount = frameCount };
                }
            }
        }

        private static Dictionary<string, int> LoadFrameCounts()
        {
            Dictionary<string, int> result = new();
            TextAsset manifestAsset = Resources.Load<TextAsset>("VideoFrames/manifest");
            if (manifestAsset == null)
            {
                return result;
            }

            FrameSequenceManifest manifest = JsonUtility.FromJson<FrameSequenceManifest>(manifestAsset.text);
            if (manifest?.videos == null)
            {
                return result;
            }

            foreach (FrameSequenceManifestEntry entry in manifest.videos)
            {
                if (entry != null && !string.IsNullOrWhiteSpace(entry.stem) && entry.frameCount > 0)
                {
                    result[entry.stem] = entry.frameCount;
                }
            }

            return result;
        }

        private string GetDurationLabel(VideoLibraryData.VideoEntry video)
        {
            if (!frameSequences.TryGetValue(video, out FrameSequence sequence))
            {
                return video.duration;
            }

            int seconds = Mathf.CeilToInt(sequence.Duration);
            return $"{seconds / 60:00}:{seconds % 60:00}";
        }

        private static void Shuffle<T>(IList<T> items)
        {
            for (int index = items.Count - 1; index > 0; index--)
            {
                int swapIndex = UnityEngine.Random.Range(0, index + 1);
                (items[index], items[swapIndex]) = (items[swapIndex], items[index]);
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Apply Television Layout In Edit Mode")]
        public void ApplyTelevisionLayoutInEditMode()
        {
            if (Application.isPlaying)
            {
                return;
            }

            TelevisionVideoFeedLayoutEditor.Apply(this, sidebarWidth, headerHeight, horizontalGap, verticalGap,
                screenColor, cardColor, primaryTextColor, secondaryTextColor, accentColor);
        }
#endif
    }
}
