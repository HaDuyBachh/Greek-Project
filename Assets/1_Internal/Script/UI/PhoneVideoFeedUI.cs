using System.IO;
using System.Collections.Generic;
using GreekProject.Content;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GreekProject.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("UI/Phone Video Feed UI")]
    public sealed class PhoneVideoFeedUI : MonoBehaviour
    {
        private const string TemplateRootName = "VideoFeedTemplate";
        private const int TemplateVersion = 19;
        private const float SequenceFramesPerSecond = 10f;
        private const int SequenceColumns = 8;
        private const int SequenceRows = 8;
        private const int FramesPerSheet = SequenceColumns * SequenceRows;
        private static string VersionMarkerName => "__TemplateVersion_" + TemplateVersion;

        [Header("Video Card Layout (World Units)")]
        [SerializeField, Range(0.12f, 0.24f)] private float videoCardHeight = 0.162f;
        [SerializeField, Range(0.08f, 0.16f)] private float thumbnailHeight = 0.1225f;
        [SerializeField, Range(0f, 0.02f)] private float thumbnailTopPadding = 0.003f;
        [SerializeField, Range(0.10f, 0.16f)] private float videoInfoTop = 0.1275f;
        [SerializeField, Range(0.02f, 0.06f)] private float videoInfoHeight = 0.0325f;
        [SerializeField, Range(0f, 0.02f)] private float videoSpacing = 0.004f;
        [SerializeField, Range(0f, 0.08f)] private float horizontalInset = 0.018f;

        [Header("Video Text And Avatar")]
        [SerializeField, Range(0.015f, 0.04f)] private float avatarSize = 0.025f;
        [SerializeField, Range(0.005f, 0.015f)] private float titleFontSize = 0.009f;
        [SerializeField, Range(0.003f, 0.01f)] private float metadataFontSize = 0.0052f;

        [Header("Video Data")]
        [SerializeField] private VideoLibraryData videoLibrary;
        [SerializeField] private KidFocusCameraController kidFocusController;

        private sealed class FrameSequence
        {
            public Texture2D[] Sheets;
            public int FrameCount;

            public float Duration => FrameCount / SequenceFramesPerSecond;
        }

        [System.Serializable]
        private sealed class FrameSequenceManifest
        {
            public FrameSequenceManifestEntry[] videos;
        }

        [System.Serializable]
        private sealed class FrameSequenceManifestEntry
        {
            public string stem;
            public int frameCount;
        }

        private RectTransform viewerRoot;
        private Image viewerThumbnail;
        private TextMeshProUGUI viewerMockNumber;
        private TextMeshProUGUI viewerTitle;
        private TextMeshProUGUI viewerMetadata;
        private TextMeshProUGUI viewerDescription;
        private TextMeshProUGUI viewerChannel;
        private RawImage viewerVideoSurface;
        private Button playPauseButton;
        private PlaybackControlGraphic playPauseIcon;
        private AspectRatioFitter viewerVideoAspect;
        private Slider progressSlider;
        private VideoLibraryData.VideoEntry activeVideo;
        private bool suppressProgressCallback;
        private bool effectAppliedForCurrentPlay;
        private bool playbackRequested;
        private bool runtimeLibraryInitialized;
        private float sequenceTime;
        private float watchedPlaybackSeconds;
        private int displayedSequenceFrame = -1;
        private FrameSequence activeSequence;
        private readonly Dictionary<string, Sprite> runtimeThumbnails = new Dictionary<string, Sprite>();
        private readonly Dictionary<VideoLibraryData.VideoEntry, FrameSequence> frameSequences =
            new Dictionary<VideoLibraryData.VideoEntry, FrameSequence>();

        private static readonly Color ScreenColor = Hex("0F0F0F");
        private static readonly Color PanelColor = Hex("181818");
        private static readonly Color RaisedColor = Hex("272727");
        private static readonly Color TextColor = Hex("F5F5F5");
        private static readonly Color SecondaryTextColor = Hex("AAAAAA");
        private static readonly Color DividerColor = Hex("343434");
        private static readonly Color AccentColor = Hex("FF0033");
        private static readonly Color ThumbnailColor = Hex("F2F2F2");

        private void Awake()
        {
            if (Application.isPlaying)
            {
                InitializeRuntimeLibrary();
            }
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                InitializeRuntimeLibrary();
            }
        }

        private void Update()
        {
            if (viewerRoot != null && viewerRoot.gameObject.activeSelf &&
                kidFocusController != null && !kidFocusController.IsPhoneScreenVisible)
            {
                HideViewer();
                return;
            }

            UpdateVideoPlaybackUi();
        }

        private void OnDestroy()
        {
            ReleaseVideoOutput();
            frameSequences.Clear();
            foreach (Sprite sprite in runtimeThumbnails.Values)
            {
                if (sprite != null)
                {
                    Destroy(sprite.texture);
                    Destroy(sprite);
                }
            }

            runtimeThumbnails.Clear();
        }

        [ContextMenu("Rebuild Video Feed Template")]
        public void Rebuild()
        {
            Transform existing = transform.Find(TemplateRootName);
            if (existing != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(existing.gameObject);
                }
                else
                {
                    DestroyImmediate(existing.gameObject);
                }
            }

            Build();
        }

        private void Build()
        {
            RectTransform root = CreateRect(TemplateRootName, transform);
            Stretch(root);
            RectTransform versionMarker = CreateRect(VersionMarkerName, root);
            versionMarker.gameObject.SetActive(false);

            CreateImage("Background", root, ScreenColor, Vector2.zero, Vector2.one);
            RectTransform inputBlocker = CreateImage("PhoneInputBlocker", root, Color.clear, Vector2.zero, Vector2.one);
            inputBlocker.GetComponent<Image>().raycastTarget = true;
            BuildHeader(root);
            BuildCategories(root);
            BuildVideoScroll(root);
            BuildBottomNavigation(root);

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(gameObject);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
#endif
        }

        private static void BuildHeader(RectTransform parent)
        {
            RectTransform header = CreateImage("Header", parent, ScreenColor, new Vector2(0f, 0.915f), Vector2.one);

            RectTransform logo = CreateRect("Logo", header);
            SetAnchors(logo, new Vector2(0.025f, 0.18f), new Vector2(0.43f, 0.82f));

            RectTransform logoMark = CreateRoundedPanel("LogoMark", logo, AccentColor,
                new Vector2(0f, 0.14f), new Vector2(0.2f, 0.86f), 0.2f);
            CreateText("LogoInitial", logoMark, "U", TextColor, 0.0105f, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, FontStyles.Bold);
            TextMeshProUGUI logoText = CreateText("LogoText", logo, "UTube", TextColor, 0.0105f,
                TextAlignmentOptions.MidlineLeft, new Vector2(0.25f, 0f), Vector2.one, FontStyles.Bold);
            logoText.textWrappingMode = TextWrappingModes.NoWrap;
            logoText.overflowMode = TextOverflowModes.Ellipsis;

            RectTransform search = CreateRoundedPanel("SearchField", header, RaisedColor,
                new Vector2(0.46f, 0.23f), new Vector2(0.965f, 0.77f), 0.14f);
            TextMeshProUGUI placeholder = CreateText("Placeholder", search, "Search", SecondaryTextColor, 0.0075f,
                TextAlignmentOptions.MidlineLeft, new Vector2(0.09f, 0.1f), new Vector2(0.79f, 0.9f));
            placeholder.fontStyle = FontStyles.Italic;

            CreateText("SearchIcon", search, "O", TextColor, 0.0085f, TextAlignmentOptions.Center,
                new Vector2(0.82f, 0f), Vector2.one, FontStyles.Bold);
        }

        private static void BuildCategories(RectTransform parent)
        {
            RectTransform bar = CreateImage("CategoryBar", parent, ScreenColor, new Vector2(0f, 0.835f), new Vector2(1f, 0.915f));
            CreateCategory("All", bar, true, new Vector2(0.018f, 0.2f), new Vector2(0.238f, 0.8f));
            CreateCategory("Gaming", bar, false, new Vector2(0.25f, 0.2f), new Vector2(0.488f, 0.8f));
            CreateCategory("Live", bar, false, new Vector2(0.5f, 0.2f), new Vector2(0.738f, 0.8f));
            CreateCategory("Music", bar, false, new Vector2(0.75f, 0.2f), new Vector2(0.982f, 0.8f));
        }

        private static void CreateCategory(string label, RectTransform parent, bool selected, Vector2 anchorMin, Vector2 anchorMax)
        {
            RectTransform chip = CreateRoundedPanel(label, parent, selected ? TextColor : RaisedColor,
                anchorMin, anchorMax, 0.14f);
            CreateText("Label", chip, label, selected ? ScreenColor : TextColor, 0.0072f,
                TextAlignmentOptions.Center, Vector2.zero, Vector2.one, FontStyles.Bold);
        }

        private void BuildVideoScroll(RectTransform parent)
        {
            RectTransform viewport = CreateRect("VideoScroll", parent);
            SetAnchors(viewport, new Vector2(0f, 0.118f), new Vector2(1f, 0.835f));
            Image inputSurface = viewport.gameObject.AddComponent<Image>();
            inputSurface.color = Color.clear;
            inputSurface.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();

            ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.elasticity = 0.08f;
            scroll.scrollSensitivity = 0.035f;
            scroll.viewport = viewport;

            RectTransform content = CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = videoSpacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;

            if (videoLibrary != null)
            {
                BuildVideoCards(content);
                return;
            }

            CreateVideoCard(content, "Video 01", "Today's featured video collection", "Demo Channel · 209K views · 1 day ago", "12:08", Hex("F4F4F4"), Hex("E62117"));
            CreateVideoCard(content, "Video 02", "A longer video title to test the layout", "Demo Studio · 86K views · 3 days ago", "17:10", Hex("E5E7EB"), Hex("2563EB"));
            CreateVideoCard(content, "Video 03", "Discover new videos picked for you", "Daily Creator · 52K views · 1 week ago", "08:42", Hex("FFFFFF"), Hex("F59E0B"));
            CreateVideoCard(content, "Video 04", "Up next in your video feed", "Entertainment · 31K views · 2 weeks ago", "10:24", Hex("ECECEC"), Hex("10B981"));
        }

        private void CreateVideoCard(RectTransform parent, string objectName, string title, string description,
            string duration, Color thumbnailColor, Color avatarColor)
        {
            RectTransform card = CreateImage(objectName, parent, PanelColor, Vector2.zero, Vector2.one);
            LayoutElement element = card.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = videoCardHeight;
            element.minHeight = videoCardHeight;

            RectTransform thumbnail = CreateImage("Thumbnail", card, thumbnailColor,
                new Vector2(horizontalInset, 1f), new Vector2(1f - horizontalInset, 1f));
            thumbnail.pivot = new Vector2(0.5f, 1f);
            thumbnail.anchoredPosition = new Vector2(0f, -thumbnailTopPadding);
            thumbnail.sizeDelta = new Vector2(0f, thumbnailHeight);
            Image thumbnailImage = thumbnail.GetComponent<Image>();
            thumbnailImage.preserveAspect = false;

            RectTransform durationPanel = CreateRoundedPanel("Duration", thumbnail, new Color(0f, 0f, 0f, 0.78f),
                new Vector2(0.83f, 0.045f), new Vector2(0.968f, 0.19f), 0.24f);
            CreateText("Text", durationPanel, duration, TextColor, 0.006f, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, FontStyles.Bold);

            RectTransform videoInfo = CreateRect("VideoInfo", card);
            SetAnchors(videoInfo, new Vector2(horizontalInset, 1f), new Vector2(1f - horizontalInset, 1f));
            videoInfo.pivot = new Vector2(0.5f, 1f);
            videoInfo.anchoredPosition = new Vector2(0f, -videoInfoTop);
            videoInfo.sizeDelta = new Vector2(0f, videoInfoHeight);

            RectTransform avatar = CreateRoundedPanel("ChannelAvatar", videoInfo, avatarColor,
                new Vector2(0f, 1f), new Vector2(0f, 1f), 0.5f);
            avatar.pivot = new Vector2(0f, 1f);
            avatar.anchoredPosition = new Vector2(0.0004f, -0.0061f);
            avatar.sizeDelta = new Vector2(avatarSize, avatarSize);
            CreateText("Initial", avatar, "C", TextColor, 0.008f, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, FontStyles.Bold);

            TextMeshProUGUI titleText = CreateText("Title", videoInfo, title, TextColor, titleFontSize,
                TextAlignmentOptions.MidlineLeft, Vector2.one, Vector2.one, FontStyles.Bold);
            RectTransform titleRect = titleText.rectTransform;
            titleRect.pivot = Vector2.one;
            titleRect.anchoredPosition = new Vector2(0.0004f, -0.0045f);
            titleRect.sizeDelta = new Vector2(0.18073f, 0.0137f);
            titleText.textWrappingMode = TextWrappingModes.Normal;
            titleText.overflowMode = TextOverflowModes.Ellipsis;

            TextMeshProUGUI descriptionText = CreateText("Description", videoInfo, description, SecondaryTextColor, metadataFontSize,
                TextAlignmentOptions.MidlineLeft, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
            RectTransform descriptionRect = descriptionText.rectTransform;
            descriptionRect.pivot = new Vector2(1f, 0.5f);
            descriptionRect.anchoredPosition = new Vector2(0.0004f, -0.0045f);
            descriptionRect.sizeDelta = new Vector2(0.18073f, 0.01475f);
            descriptionText.textWrappingMode = TextWrappingModes.NoWrap;
            descriptionText.overflowMode = TextOverflowModes.Ellipsis;

            TextMeshProUGUI moreText = CreateText("More", videoInfo, ".\n.\n.", SecondaryTextColor, 0.0085f,
                TextAlignmentOptions.Center, new Vector2(0.94f, 0.5f), new Vector2(1f, 0.96f), FontStyles.Bold);
            moreText.lineSpacing = -70f;
            CreateImage("Divider", card, DividerColor, Vector2.zero, new Vector2(1f, 0.012f));
        }

        private void BuildVideoCards(RectTransform content)
        {
            int index = 1;
            foreach (VideoLibraryData.VideoEntry video in videoLibrary.Videos)
            {
                if (video == null)
                {
                    continue;
                }

                CreateVideoCard(content, $"Video {index:00}", video);
                index++;
            }
        }

        private void CreateVideoCard(RectTransform parent, string objectName, VideoLibraryData.VideoEntry video)
        {
            Color avatarColor = Color.Lerp(video.mockColor, ScreenColor, 0.25f);
            CreateVideoCard(parent, objectName, video.title, video.Metadata, video.duration, video.mockColor, avatarColor);

            RectTransform card = parent.GetChild(parent.childCount - 1) as RectTransform;
            if (card == null)
            {
                Debug.LogError($"Failed to create video card '{objectName}'.", this);
                return;
            }

            Image cardImage = card.GetComponent<Image>();
            if (cardImage == null)
            {
                Debug.LogError($"Video card '{objectName}' has no Image component.", card);
                return;
            }

            cardImage.raycastTarget = true;

            Button cardButton = card.gameObject.AddComponent<Button>();
            cardButton.targetGraphic = cardImage;
            cardButton.transition = Selectable.Transition.ColorTint;
            cardButton.colors = CreateButtonColors(PanelColor);
            cardButton.onClick.AddListener(() => ShowVideo(video));

            RectTransform thumbnail = card.Find("Thumbnail") as RectTransform;
            if (thumbnail == null)
            {
                Debug.LogError($"Video card '{objectName}' has no Thumbnail child.", card);
                return;
            }

            Image thumbnailImage = thumbnail.GetComponent<Image>();
            thumbnailImage.raycastTarget = true;
            cardButton.targetGraphic = thumbnailImage;
            TextMeshProUGUI mockNumber = CreateText("MockImageNumber", thumbnail,
                video.mockImageNumber.ToString("00"), new Color(1f, 1f, 1f, 0.9f), 0.035f,
                TextAlignmentOptions.Center, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.92f), FontStyles.Bold);
            mockNumber.rectTransform.SetAsFirstSibling();
            ApplyThumbnail(thumbnailImage, mockNumber, video);

            TextMeshProUGUI initial = card.Find("VideoInfo/ChannelAvatar/Initial")?.GetComponent<TextMeshProUGUI>();
            if (initial != null && !string.IsNullOrWhiteSpace(video.channel))
            {
                initial.text = video.channel.Substring(0, 1).ToUpperInvariant();
            }
        }

        private void InitializeRuntimeLibrary()
        {
            if (!Application.isPlaying || videoLibrary == null || runtimeLibraryInitialized)
            {
                return;
            }

            Transform template = transform.Find(TemplateRootName);
            RectTransform content = template != null
                ? template.Find("VideoScroll/Content") as RectTransform
                : null;
            if (content == null)
            {
                Debug.LogError("VideoFeedTemplate/VideoScroll/Content was not found. Rebuild the phone template from the Inspector.", this);
                return;
            }

            runtimeLibraryInitialized = true;

            Transform staleViewer = template.Find("VideoPlayerView");
            if (staleViewer != null)
            {
                staleViewer.gameObject.SetActive(false);
                Destroy(staleViewer.gameObject);
            }

            frameSequences.Clear();

            viewerRoot = null;
            viewerThumbnail = null;
            viewerMockNumber = null;
            viewerTitle = null;
            viewerMetadata = null;
            viewerDescription = null;
            viewerChannel = null;
            viewerVideoSurface = null;
            viewerVideoAspect = null;
            playPauseButton = null;
            playPauseIcon = null;
            progressSlider = null;
            activeVideo = null;
            activeSequence = null;
            playbackRequested = false;

            for (int index = content.childCount - 1; index >= 0; index--)
            {
                GameObject oldCard = content.GetChild(index).gameObject;
                oldCard.SetActive(false);
                Destroy(oldCard);
            }

            BuildVideoCards(content);
            LoadFrameSequences();
            EnsureViewer();
            if (viewerRoot != null)
            {
                viewerRoot.gameObject.SetActive(false);
            }
        }

        private void ShowVideo(VideoLibraryData.VideoEntry video)
        {
            if (video == null)
            {
                return;
            }

            EnsureViewer();
            if (viewerRoot == null)
            {
                return;
            }

            ApplyThumbnail(viewerThumbnail, viewerMockNumber, video);
            activeVideo = video;
            effectAppliedForCurrentPlay = false;
            watchedPlaybackSeconds = 0f;
            viewerTitle.text = video.title;
            viewerMetadata.text = $"{video.views} | {video.published} | {video.duration}";
            viewerDescription.text = video.description;
            viewerChannel.text = video.channel;
            viewerRoot.gameObject.SetActive(true);
            viewerRoot.SetAsLastSibling();
            LayoutViewerDetails();
            ConfigureFrameSequence(video);
        }

        private void HideViewer()
        {
            if (viewerRoot != null)
            {
                activeVideo = null;
                activeSequence = null;
                playbackRequested = false;
                UpdatePlayPauseIcon(false);
                viewerRoot.gameObject.SetActive(false);
            }
        }

        private void EnsureViewer()
        {
            if (viewerRoot != null)
            {
                return;
            }

            Transform template = transform.Find(TemplateRootName);
            if (template == null)
            {
                return;
            }

            viewerRoot = CreateImage("VideoPlayerView", template, ScreenColor, Vector2.zero, Vector2.one);
            viewerRoot.GetComponent<Image>().raycastTarget = true;

            RectTransform stage = CreateImage("VideoStage", viewerRoot, Color.black,
                new Vector2(0f, 0.67f), Vector2.one);
            RectTransform thumbnailRect = CreateImage("Thumbnail", stage, Color.white, Vector2.zero, Vector2.one);
            viewerThumbnail = thumbnailRect.GetComponent<Image>();
            viewerThumbnail.raycastTarget = false;
            RectTransform videoSurfaceRect = CreateRect("VideoSurface", stage);
            Stretch(videoSurfaceRect);
            viewerVideoSurface = videoSurfaceRect.gameObject.AddComponent<RawImage>();
            viewerVideoSurface.color = Color.white;
            viewerVideoSurface.raycastTarget = false;
            viewerVideoAspect = videoSurfaceRect.gameObject.AddComponent<AspectRatioFitter>();
            viewerVideoAspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            viewerVideoAspect.aspectRatio = 16f / 9f;
            viewerVideoSurface.gameObject.SetActive(false);
            viewerMockNumber = CreateText("MockImageNumber", thumbnailRect, "01", new Color(1f, 1f, 1f, 0.9f),
                0.052f, TextAlignmentOptions.Center, new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.94f), FontStyles.Bold);

            RectTransform progressTrack = CreateImage("ProgressTrack", stage, new Color(1f, 1f, 1f, 0.28f),
                new Vector2(0.03f, 0.025f), new Vector2(0.97f, 0.035f));
            progressTrack.GetComponent<Image>().raycastTarget = true;
            RectTransform progressFill = CreateImage("Progress", progressTrack, AccentColor, Vector2.zero, Vector2.one);
            RectTransform progressHandle = CreateRoundedPanel("Handle", progressTrack, AccentColor,
                new Vector2(0f, -1.4f), new Vector2(0f, 2.4f), 0.5f);
            progressHandle.sizeDelta = new Vector2(0.008f, 0f);
            progressSlider = progressTrack.gameObject.AddComponent<Slider>();
            progressSlider.fillRect = progressFill;
            progressSlider.handleRect = progressHandle;
            progressSlider.targetGraphic = progressTrack.GetComponent<Image>();
            progressSlider.direction = Slider.Direction.LeftToRight;
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 1f;
            progressSlider.value = 0f;
            progressSlider.onValueChanged.AddListener(SeekVideo);

            RectTransform playButton = CreateRect("PlayButton", stage);
            SetAnchors(playButton, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            playButton.sizeDelta = new Vector2(0.04f, 0.04f);
            playPauseIcon = playButton.gameObject.AddComponent<PlaybackControlGraphic>();
            playPauseIcon.raycastTarget = true;
            playPauseButton = playButton.gameObject.AddComponent<Button>();
            playPauseButton.targetGraphic = playPauseIcon;
            playPauseButton.transition = Selectable.Transition.None;
            playPauseButton.onClick.AddListener(TogglePlayback);

            RectTransform details = CreateImage("VideoDetails", viewerRoot, PanelColor,
                new Vector2(0f, 0.118f), new Vector2(1f, 0.67f));
            viewerTitle = CreateText("Title", details, string.Empty, TextColor, 0.0115f,
                TextAlignmentOptions.TopLeft, new Vector2(0.045f, 0.77f), new Vector2(0.955f, 0.94f), FontStyles.Bold);
            viewerTitle.textWrappingMode = TextWrappingModes.Normal;
            viewerTitle.overflowMode = TextOverflowModes.Ellipsis;

            viewerMetadata = CreateText("Metadata", details, string.Empty, SecondaryTextColor, 0.0065f,
                TextAlignmentOptions.MidlineLeft, new Vector2(0.045f, 0.68f), new Vector2(0.955f, 0.77f));
            viewerDescription = CreateText("Description", details, string.Empty, SecondaryTextColor, 0.0064f,
                TextAlignmentOptions.TopLeft, new Vector2(0.045f, 0.47f), new Vector2(0.955f, 0.65f));
            viewerDescription.textWrappingMode = TextWrappingModes.Normal;
            viewerDescription.overflowMode = TextOverflowModes.Ellipsis;

            CreateImage("ChannelDivider", details, DividerColor, new Vector2(0f, 0.35f), new Vector2(1f, 0.356f));
            RectTransform channelDot = CreateRoundedPanel("ChannelAvatar", details, AccentColor,
                new Vector2(0.045f, 0.16f), new Vector2(0.14f, 0.31f), 0.5f);
            CreateText("Initial", channelDot, "U", TextColor, 0.008f, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, FontStyles.Bold);
            viewerChannel = CreateText("Channel", details, string.Empty, TextColor, 0.0075f,
                TextAlignmentOptions.MidlineLeft, new Vector2(0.17f, 0.16f), new Vector2(0.72f, 0.31f), FontStyles.Bold);

            LayoutViewerDetails();

            RectTransform bottomBar = CreateImage("PlayerBottomBar", viewerRoot, ScreenColor,
                Vector2.zero, new Vector2(1f, 0.118f));
            CreateImage("TopDivider", bottomBar, DividerColor, new Vector2(0f, 0.97f), Vector2.one);
            RectTransform back = CreateRoundedPanel("Back", bottomBar, RaisedColor,
                new Vector2(0.045f, 0.18f), new Vector2(0.42f, 0.8f), 0.12f);
            RoundedRectGraphic backGraphic = back.GetComponent<RoundedRectGraphic>();
            backGraphic.raycastTarget = true;
            Button backButton = back.gameObject.AddComponent<Button>();
            backButton.targetGraphic = backGraphic;
            backButton.colors = CreateButtonColors(RaisedColor);
            backButton.onClick.AddListener(HideViewer);
            CreateText("Icon", back, "<", TextColor, 0.011f, TextAlignmentOptions.Center,
                new Vector2(0.04f, 0.05f), new Vector2(0.29f, 0.95f), FontStyles.Bold);
            CreateText("Label", back, "Back", TextColor, 0.0075f, TextAlignmentOptions.MidlineLeft,
                new Vector2(0.3f, 0.05f), new Vector2(0.94f, 0.95f), FontStyles.Bold);
        }

        private void ConfigureFrameSequence(VideoLibraryData.VideoEntry video)
        {
            frameSequences.TryGetValue(video, out activeSequence);
            sequenceTime = 0f;
            displayedSequenceFrame = -1;
            playbackRequested = activeSequence != null;
            suppressProgressCallback = true;
            progressSlider.value = 0f;
            suppressProgressCallback = false;
            playPauseButton.interactable = activeSequence != null;
            viewerVideoAspect.aspectRatio = 16f / 9f;

            if (activeSequence == null)
            {
                viewerVideoSurface.gameObject.SetActive(false);
                viewerThumbnail.enabled = true;
                UpdatePlayPauseIcon(false);
                Debug.LogError($"No frame sequence was loaded for video '{video.id}'.", this);
                return;
            }

            viewerThumbnail.enabled = false;
            viewerMockNumber.gameObject.SetActive(false);
            viewerVideoSurface.gameObject.SetActive(true);
            ApplySequenceFrame(0);
            UpdatePlayPauseIcon(true);
        }

        private void LoadFrameSequences()
        {
            Dictionary<string, int> frameCounts = LoadSequenceFrameCounts();
            foreach (VideoLibraryData.VideoEntry video in videoLibrary.Videos)
            {
                if (video == null)
                {
                    continue;
                }

                string stem = GetVideoStem(video);
                Texture2D[] sheets = Resources.LoadAll<Texture2D>($"VideoFrames/{stem}");
                System.Array.Sort(sheets, (left, right) => string.CompareOrdinal(left.name, right.name));
                frameCounts.TryGetValue(stem, out int frameCount);
                if (sheets.Length == 0 || frameCount <= 0)
                {
                    Debug.LogError($"Frame sequence for '{video.id}' was not found in Resources/VideoFrames/{stem}.", this);
                    continue;
                }

                frameSequences[video] = new FrameSequence
                {
                    Sheets = sheets,
                    FrameCount = frameCount
                };
            }

            Debug.Log($"Loaded {frameSequences.Count} phone image sequences before interaction.", this);
        }

        private Dictionary<string, int> LoadSequenceFrameCounts()
        {
            Dictionary<string, int> frameCounts = new Dictionary<string, int>();
            TextAsset manifestAsset = Resources.Load<TextAsset>("VideoFrames/manifest");
            if (manifestAsset == null)
            {
                Debug.LogError("Resources/VideoFrames/manifest.json was not found.", this);
                return frameCounts;
            }

            FrameSequenceManifest manifest = JsonUtility.FromJson<FrameSequenceManifest>(manifestAsset.text);
            if (manifest?.videos == null)
            {
                Debug.LogError("VideoFrames/manifest.json is invalid.", this);
                return frameCounts;
            }

            foreach (FrameSequenceManifestEntry entry in manifest.videos)
            {
                if (entry != null && !string.IsNullOrWhiteSpace(entry.stem) && entry.frameCount > 0)
                {
                    frameCounts[entry.stem] = entry.frameCount;
                }
            }

            return frameCounts;
        }

        private void LayoutViewerDetails()
        {
            if (viewerTitle == null)
            {
                return;
            }

            RectTransform details = viewerTitle.rectTransform.parent as RectTransform;
            SetTopRect(viewerTitle.rectTransform, 0.018f, 0.018f, 0.035f);
            SetTopRect(viewerMetadata.rectTransform, 0.018f, 0.058f, 0.015f);
            SetTopRect(viewerDescription.rectTransform, 0.018f, 0.081f, 0.035f);

            RectTransform divider = details.Find("ChannelDivider") as RectTransform;
            SetTopRect(divider, 0f, 0.13f, 0.002f);

            RectTransform avatar = details.Find("ChannelAvatar") as RectTransform;
            SetTopRect(avatar, 0.018f, 0.145f, 0.034f, 0.034f);
            SetTopRect(viewerChannel.rectTransform, 0.062f, 0.145f, 0.034f);
        }

        private static void SetTopRect(RectTransform rect, float left, float top, float height, float width = -1f)
        {
            if (rect == null)
            {
                return;
            }

            if (width >= 0f)
            {
                rect.anchorMin = Vector2.up;
                rect.anchorMax = Vector2.up;
                rect.pivot = Vector2.up;
                rect.anchoredPosition = new Vector2(left, -top);
                rect.sizeDelta = new Vector2(width, height);
                return;
            }

            rect.anchorMin = Vector2.up;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, -top - height);
            rect.offsetMax = new Vector2(-0.018f, -top);
        }

        private void TogglePlayback()
        {
            if (activeSequence == null)
            {
                return;
            }

            if (playbackRequested)
            {
                playbackRequested = false;
                UpdatePlayPauseIcon(false);
                return;
            }

            playbackRequested = true;
            if (sequenceTime >= activeSequence.Duration)
            {
                sequenceTime = 0f;
                effectAppliedForCurrentPlay = false;
                watchedPlaybackSeconds = 0f;
            }

            UpdatePlayPauseIcon(true);
        }

        private void UpdateVideoPlaybackUi()
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
                sequenceTime += Time.unscaledDeltaTime;
                watchedPlaybackSeconds += Time.unscaledDeltaTime;
                if (sequenceTime >= duration)
                {
                    sequenceTime %= duration;
                }
            }

            int frameIndex = Mathf.Min(
                Mathf.FloorToInt(sequenceTime * SequenceFramesPerSecond),
                activeSequence.FrameCount - 1);
            ApplySequenceFrame(frameIndex);

            float progress = Mathf.Clamp01(sequenceTime / duration);
            suppressProgressCallback = true;
            progressSlider.value = progress;
            suppressProgressCallback = false;

            if (watchedPlaybackSeconds >= duration * 0.8f)
            {
                ApplyVideoEffectOnce();
            }
        }

        private void SeekVideo(float normalizedTime)
        {
            if (suppressProgressCallback || activeSequence == null || activeSequence.Duration <= 0f)
            {
                return;
            }

            sequenceTime = Mathf.Clamp01(normalizedTime) * activeSequence.Duration;
            int frameIndex = Mathf.Min(
                Mathf.FloorToInt(sequenceTime * SequenceFramesPerSecond),
                activeSequence.FrameCount - 1);
            ApplySequenceFrame(frameIndex);
            viewerThumbnail.enabled = false;
            viewerMockNumber.gameObject.SetActive(false);
            viewerVideoSurface.gameObject.SetActive(true);
        }

        private void ApplySequenceFrame(int frameIndex)
        {
            if (activeSequence == null || frameIndex == displayedSequenceFrame)
            {
                return;
            }

            int sheetIndex = frameIndex / FramesPerSheet;
            if (sheetIndex < 0 || sheetIndex >= activeSequence.Sheets.Length)
            {
                return;
            }

            int frameInSheet = frameIndex % FramesPerSheet;
            int column = frameInSheet % SequenceColumns;
            int row = frameInSheet / SequenceColumns;
            Texture2D sheet = activeSequence.Sheets[sheetIndex];
            float cellWidth = 1f / SequenceColumns;
            float cellHeight = 1f / SequenceRows;
            float insetX = 0.5f / sheet.width;
            float insetY = 0.5f / sheet.height;
            viewerVideoSurface.texture = sheet;
            viewerVideoSurface.uvRect = new Rect(
                column * cellWidth + insetX,
                (SequenceRows - row - 1) * cellHeight + insetY,
                cellWidth - insetX * 2f,
                cellHeight - insetY * 2f);
            displayedSequenceFrame = frameIndex;
        }

        private void ApplyVideoEffectOnce()
        {
            if (effectAppliedForCurrentPlay || activeVideo == null)
            {
                return;
            }

            effectAppliedForCurrentPlay = true;
            kidFocusController?.RegisterViewedVideo(activeVideo.contentEffect);
        }

        private void UpdatePlayPauseIcon(bool isPlaying)
        {
            if (playPauseIcon != null)
            {
                playPauseIcon.IsPlaying = isPlaying;
            }
        }

        private void ReleaseVideoOutput()
        {
            if (viewerVideoSurface != null)
            {
                viewerVideoSurface.texture = null;
            }
        }

        private void ApplyThumbnail(Image image, TextMeshProUGUI mockNumber, VideoLibraryData.VideoEntry video)
        {
            Sprite thumbnail = LoadRuntimeThumbnail(video);
            bool hasSprite = thumbnail != null;
            image.sprite = thumbnail;
            image.color = hasSprite ? Color.white : video.mockColor;
            image.preserveAspect = hasSprite;
            mockNumber.gameObject.SetActive(!hasSprite);
            mockNumber.text = video.mockImageNumber.ToString("00");
        }

        private Sprite LoadRuntimeThumbnail(VideoLibraryData.VideoEntry video)
        {
            string stem = GetVideoStem(video);
            if (string.IsNullOrEmpty(stem))
            {
                return null;
            }

            if (runtimeThumbnails.TryGetValue(stem, out Sprite cached))
            {
                return cached;
            }

            string thumbnailStem = stem == "bainrot01" ? "brainrot01" : stem;
            string path = Path.Combine(
                Application.dataPath,
                "1_Internal",
                "Data",
                "Video_Processed",
                thumbnailStem + ".png");
            if (!File.Exists(path))
            {
                return null;
            }

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(path), false))
            {
                Destroy(texture);
                return null;
            }

            texture.name = thumbnailStem + "_RuntimeThumbnail";
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), 100f);
            sprite.name = thumbnailStem + "_RuntimeSprite";
            runtimeThumbnails[stem] = sprite;
            return sprite;
        }

        private static string GetVideoStem(VideoLibraryData.VideoEntry video)
        {
            return video?.id switch
            {
                "video-01" => "bainrot02",
                "video-02" => "bainrot06",
                "video-03" => "brainrot01",
                "video-04" => "brainrot03",
                "video-05" => "brainrot04",
                "video-06" => "brainrot05",
                "video-07" => "horror01",
                "video-08" => "horror02",
                "video-09" => "normal01",
                "video-10" => "normal02",
                "video-11" => "normal04",
                "video-12" => "normal05",
                _ => null
            };
        }

        private static ColorBlock CreateButtonColors(Color normal)
        {
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.Lerp(Color.white, normal, 0.08f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.45f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            return colors;
        }

        private static void BuildBottomNavigation(RectTransform parent)
        {
            RectTransform nav = CreateImage("BottomNavigation", parent, PanelColor, Vector2.zero, new Vector2(1f, 0.118f));
            CreateImage("TopDivider", nav, DividerColor, new Vector2(0f, 0.97f), Vector2.one);

            CreateNavItem(nav, 0, "Home", true, false);
            CreateNavItem(nav, 1, "Shorts", false, false);
            CreateNavItem(nav, 2, "Subscriptions", false, false);
            CreateNavItem(nav, 3, "You", false, true);
        }

        private static void CreateNavItem(RectTransform parent, int index, string label, bool selected, bool account)
        {
            float minX = index * 0.25f;
            RectTransform item = CreateRect(label, parent);
            SetAnchors(item, new Vector2(minX, 0f), new Vector2(minX + 0.25f, 0.96f));
            Color iconColor = account ? Hex("3B82F6") : selected ? TextColor : SecondaryTextColor;
            CreateNavGlyph(item, index, iconColor, account);
            CreateText("Label", item, label, selected ? TextColor : SecondaryTextColor, 0.0055f,
                TextAlignmentOptions.Center, new Vector2(0.02f, 0.06f), new Vector2(0.98f, 0.32f),
                selected ? FontStyles.Bold : FontStyles.Normal);
        }

        private static void CreateNavGlyph(RectTransform parent, int index, Color color, bool account)
        {
            if (account)
            {
                RectTransform avatar = CreateRoundedPanel("AccountAvatar", parent, color,
                    new Vector2(0.37f, 0.46f), new Vector2(0.63f, 0.76f), 0.5f);
                CreateText("Initial", avatar, "A", TextColor, 0.0075f, TextAlignmentOptions.Center,
                    Vector2.zero, Vector2.one, FontStyles.Bold);
                return;
            }

            if (index == 0)
            {
                RectTransform leftRoof = CreateImage("RoofLeft", parent, color,
                    new Vector2(0.38f, 0.63f), new Vector2(0.52f, 0.68f));
                leftRoof.localRotation = Quaternion.Euler(0f, 0f, 34f);
                RectTransform rightRoof = CreateImage("RoofRight", parent, color,
                    new Vector2(0.48f, 0.63f), new Vector2(0.62f, 0.68f));
                rightRoof.localRotation = Quaternion.Euler(0f, 0f, -34f);
                CreateImage("House", parent, color, new Vector2(0.41f, 0.47f), new Vector2(0.59f, 0.63f));
                return;
            }

            if (index == 1)
            {
                RectTransform shortTile = CreateRoundedPanel("ShortsTile", parent, color,
                    new Vector2(0.39f, 0.46f), new Vector2(0.61f, 0.76f), 0.3f);
                CreateText("Play", shortTile, ">", ScreenColor, 0.007f, TextAlignmentOptions.Center,
                    Vector2.zero, Vector2.one, FontStyles.Bold);
                return;
            }

            RectTransform screen = CreateImage("SubscriptionsScreen", parent, color,
                new Vector2(0.37f, 0.49f), new Vector2(0.63f, 0.74f));
            RectTransform inner = CreateImage("Inner", screen, PanelColor,
                new Vector2(0.1f, 0.14f), new Vector2(0.9f, 0.86f));
            CreateText("Play", inner, ">", color, 0.0055f, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, FontStyles.Bold);
            CreateImage("Stand", parent, color, new Vector2(0.43f, 0.45f), new Vector2(0.57f, 0.49f));
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static RectTransform CreateImage(string name, Transform parent, Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            RectTransform rect = CreateRect(name, parent);
            SetAnchors(rect, anchorMin, anchorMax);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private static RectTransform CreateRoundedPanel(string name, Transform parent, Color color,
            Vector2 anchorMin, Vector2 anchorMax, float radiusRatio)
        {
            RectTransform rect = CreateRect(name, parent);
            SetAnchors(rect, anchorMin, anchorMax);
            RoundedRectGraphic graphic = rect.gameObject.AddComponent<RoundedRectGraphic>();
            graphic.color = color;
            graphic.RadiusRatio = radiusRatio;
            graphic.ClipChildren = false;
            graphic.raycastTarget = false;
            return rect;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string value, Color color, float fontSize,
            TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax, FontStyles style = FontStyles.Normal)
        {
            RectTransform rect = CreateRect(name, parent);
            SetAnchors(rect, anchorMin, anchorMax);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.color = color;
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.enableAutoSizing = false;
            text.raycastTarget = false;
            text.margin = Vector4.zero;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            SetAnchors(rect, Vector2.zero, Vector2.one);
        }

        private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private static Color Hex(string value)
        {
            ColorUtility.TryParseHtmlString("#" + value, out Color color);
            return color;
        }
    }

}
