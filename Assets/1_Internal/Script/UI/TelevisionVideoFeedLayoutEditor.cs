#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace GreekProject.UI
{
    internal static class TelevisionVideoFeedLayoutEditor
    {
        public static void Apply(TelevisionVideoFeedUI owner, float sidebarWidth, float headerHeight,
            float horizontalGap, float verticalGap, Color screenColor, Color cardColor,
            Color primaryTextColor, Color secondaryTextColor, Color accentColor)
        {
            Transform template = owner.transform.Find("VideoFeedTemplate");
            if (template == null)
            {
                Debug.LogError("TV ScreenMask requires a prebuilt VideoFeedTemplate before applying its layout.", owner);
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(template.gameObject, "Apply Television Video Feed Layout");
            SerializedObject serializedOwner = new(owner);
            SetObjectReference(serializedOwner, "feedRoot", template as RectTransform);

            ConfigureTheme(template, screenColor, cardColor, primaryTextColor, secondaryTextColor, accentColor);
            ConfigureHeader(template, sidebarWidth, headerHeight, screenColor, primaryTextColor);
            ConfigureSidebar(template, sidebarWidth, screenColor, primaryTextColor);
            ConfigureCards(serializedOwner, template, sidebarWidth, headerHeight, horizontalGap, verticalGap,
                cardColor, primaryTextColor, secondaryTextColor);
            ConfigurePlayer(serializedOwner, template, screenColor, primaryTextColor, accentColor);

            Transform optionsBackdrop = template.Find("VideoOptionsBackdrop");
            Transform optionsPanel = template.Find("VideoOptionsPanel");
            SetActive(optionsBackdrop, false);
            SetActive(optionsPanel, false);

            serializedOwner.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(owner);
            EditorSceneManager.MarkSceneDirty(owner.gameObject.scene);
        }

        private static void ConfigureTheme(Transform template, Color screenColor, Color cardColor,
            Color primaryTextColor, Color secondaryTextColor, Color accentColor)
        {
            SetGraphicColor(template.Find("Background"), screenColor);
            SetGraphicColor(template.Find("Header"), screenColor);
            SetGraphicColor(template.Find("CategoryBar"), screenColor);
            SetGraphicColor(template.Find("BottomNavigation"), screenColor);
            SetGraphicColor(template.Find("VideoPlayerView"), Color.black);
            SetGraphicColor(template.Find("VideoPlayerView/VideoStage"), Color.black);

            foreach (TextMeshProUGUI text in template.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                text.color = text.name is "Description" or "Metadata" ? secondaryTextColor : primaryTextColor;
                EditorUtility.SetDirty(text);
            }

            Transform logoMark = template.Find("Header/Logo/LogoMark");
            SetGraphicColor(logoMark, accentColor);
            SetText(template.Find("Header/Logo/LogoText"), "UTube", primaryTextColor, 0.021f);

            foreach (Transform child in template.Find("VideoScroll/Content"))
            {
                SetGraphicColor(child, cardColor);
            }
        }

        private static void ConfigureHeader(Transform template, float sidebarWidth, float headerHeight,
            Color screenColor, Color primaryTextColor)
        {
            RectTransform header = template.Find("Header") as RectTransform;
            SetRect(header, new Vector2(sidebarWidth, 1f - headerHeight), Vector2.one);

            RectTransform logo = template.Find("Header/Logo") as RectTransform;
            SetRect(logo, new Vector2(0.82f, 0.2f), new Vector2(0.985f, 0.85f));
            SetActive(template.Find("Header/SearchField"), false);

            RectTransform categories = template.Find("CategoryBar") as RectTransform;
            SetRect(categories, new Vector2(sidebarWidth, 1f - headerHeight), Vector2.one);
            Transform selected = categories?.Find("All");
            SetRect(selected as RectTransform, new Vector2(0.025f, 0.15f), new Vector2(0.34f, 0.82f));
            SetGraphicColor(selected, Color.clear);
            SetText(selected?.Find("Label"), "Recommended", primaryTextColor, 0.021f,
                TextAlignmentOptions.MidlineLeft);
            SetActive(categories?.Find("Gaming"), false);
            SetActive(categories?.Find("Live"), false);
            SetActive(categories?.Find("Music"), false);
            categories?.SetAsLastSibling();
            SetGraphicColor(categories, Color.clear);
        }

        private static void ConfigureSidebar(Transform template, float sidebarWidth, Color screenColor,
            Color primaryTextColor)
        {
            RectTransform sidebar = template.Find("BottomNavigation") as RectTransform;
            SetRect(sidebar, Vector2.zero, new Vector2(sidebarWidth, 1f));
            SetGraphicColor(sidebar, new Color(0.975f, 0.975f, 0.975f, 1f));

            Transform divider = sidebar?.Find("TopDivider");
            SetRect(divider as RectTransform, new Vector2(0.97f, 0f), Vector2.one);
            SetGraphicColor(divider, new Color(primaryTextColor.r, primaryTextColor.g, primaryTextColor.b, 0.12f));

            string[] itemNames = { "Home", "Shorts", "Subscriptions", "You" };
            float[] centers = { 0.73f, 0.55f, 0.37f, 0.14f };
            for (int index = 0; index < itemNames.Length; index++)
            {
                RectTransform item = sidebar?.Find(itemNames[index]) as RectTransform;
                SetRect(item, new Vector2(0.18f, centers[index] - 0.055f),
                    new Vector2(0.82f, centers[index] + 0.055f));
                SetActive(item?.Find("Label"), false);
            }
        }

        private static void ConfigureCards(SerializedObject owner, Transform template, float sidebarWidth,
            float headerHeight, float horizontalGap, float verticalGap, Color cardColor,
            Color primaryTextColor, Color secondaryTextColor)
        {
            RectTransform videoScroll = template.Find("VideoScroll") as RectTransform;
            SetRect(videoScroll, new Vector2(sidebarWidth + 0.015f, 0.035f),
                new Vector2(0.985f, 1f - headerHeight));
            ScrollRect scroll = videoScroll?.GetComponent<ScrollRect>();
            if (scroll != null)
            {
                scroll.enabled = false;
            }

            RectTransform content = template.Find("VideoScroll/Content") as RectTransform;
            SetRect(content, Vector2.zero, Vector2.one);
            if (content != null)
            {
                content.offsetMin = Vector2.zero;
                content.offsetMax = Vector2.zero;
            }

            VerticalLayoutGroup verticalLayout = content?.GetComponent<VerticalLayoutGroup>();
            ContentSizeFitter fitter = content?.GetComponent<ContentSizeFitter>();
            if (verticalLayout != null) verticalLayout.enabled = false;
            if (fitter != null) fitter.enabled = false;

            SerializedProperty slots = owner.FindProperty("cardSlots");
            slots.arraySize = 6;
            float columnWidth = (1f - horizontalGap * 2f) / 3f;
            float rowHeight = (1f - verticalGap) / 2f;

            for (int index = 0; index < 12; index++)
            {
                RectTransform card = content?.Find($"Video {index + 1:00}") as RectTransform;
                if (card == null)
                {
                    continue;
                }

                bool visible = index < 6;
                card.gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                int column = index % 3;
                int row = index / 3;
                float xMin = column * (columnWidth + horizontalGap);
                float xMax = xMin + columnWidth;
                float yMax = 1f - row * (rowHeight + verticalGap);
                float yMin = yMax - rowHeight;
                SetRect(card, new Vector2(xMin, yMin), new Vector2(xMax, yMax));
                SetGraphicColor(card, cardColor);

                LayoutElement layoutElement = card.GetComponent<LayoutElement>();
                if (layoutElement != null) layoutElement.enabled = false;
                SetActive(card.Find("Divider"), false);

                RectTransform thumbnail = card.Find("Thumbnail") as RectTransform;
                SetRect(thumbnail, new Vector2(0.015f, 0.34f), new Vector2(0.985f, 0.98f));
                Image thumbnailImage = thumbnail?.GetComponent<Image>();
                if (thumbnailImage != null) thumbnailImage.preserveAspect = true;

                RectTransform info = card.Find("VideoInfo") as RectTransform;
                SetRect(info, new Vector2(0.015f, 0.015f), new Vector2(0.985f, 0.32f));
                SetActive(info?.Find("ChannelAvatar"), false);
                SetActive(info?.Find("More"), false);

                TextMeshProUGUI title = info?.Find("Title")?.GetComponent<TextMeshProUGUI>();
                SetRect(title?.rectTransform, new Vector2(0f, 0.42f), Vector2.one);
                if (title != null)
                {
                    title.color = primaryTextColor;
                    title.fontSize = 0.0125f;
                    title.textWrappingMode = TextWrappingModes.Normal;
                    title.overflowMode = TextOverflowModes.Ellipsis;
                }

                TextMeshProUGUI metadata = info?.Find("Description")?.GetComponent<TextMeshProUGUI>();
                SetRect(metadata?.rectTransform, Vector2.zero, new Vector2(1f, 0.4f));
                if (metadata != null)
                {
                    metadata.color = secondaryTextColor;
                    metadata.fontSize = 0.009f;
                    metadata.textWrappingMode = TextWrappingModes.NoWrap;
                }

                TextMeshProUGUI duration = thumbnail?.Find("Duration/Text")?.GetComponent<TextMeshProUGUI>();
                if (duration != null)
                {
                    duration.color = Color.white;
                    duration.fontSize = 0.0085f;
                }

                SerializedProperty slot = slots.GetArrayElementAtIndex(index);
                SetRelativeReference(slot, "root", card);
                SetRelativeReference(slot, "openButton", card.GetComponent<Button>());
                SetRelativeReference(slot, "thumbnail", thumbnailImage);
                SetRelativeReference(slot, "mockImageNumber",
                    thumbnail?.Find("MockImageNumber")?.GetComponent<TextMeshProUGUI>());
                SetRelativeReference(slot, "title", title);
                SetRelativeReference(slot, "metadata", metadata);
                SetRelativeReference(slot, "duration", duration);
            }
        }

        private static void ConfigurePlayer(SerializedObject owner, Transform template, Color screenColor,
            Color primaryTextColor, Color accentColor)
        {
            RectTransform player = template.Find("VideoPlayerView") as RectTransform;
            RectTransform stage = player?.Find("VideoStage") as RectTransform;
            RectTransform details = player?.Find("VideoDetails") as RectTransform;
            SetRect(player, Vector2.zero, Vector2.one);
            SetRect(stage, Vector2.zero, Vector2.one);
            SetRect(details, Vector2.zero, Vector2.one);
            SetGraphicColor(player, screenColor);
            SetGraphicColor(stage, Color.black);

            Image detailsImage = details?.GetComponent<Image>();
            if (detailsImage != null)
            {
                detailsImage.color = Color.clear;
                detailsImage.raycastTarget = false;
            }

            RectTransform thumbnail = stage?.Find("Thumbnail") as RectTransform;
            RectTransform videoSurface = stage?.Find("VideoSurface") as RectTransform;
            RectTransform progress = stage?.Find("ProgressTrack") as RectTransform;
            RectTransform play = stage?.Find("PlayButton") as RectTransform;
            SetRect(thumbnail, Vector2.zero, Vector2.one);
            SetRect(videoSurface, Vector2.zero, Vector2.one);
            SetRect(progress, new Vector2(0.035f, 0.025f), new Vector2(0.965f, 0.038f));
            SetRect(play, new Vector2(0.47f, 0.43f), new Vector2(0.53f, 0.57f));
            SetGraphicColor(progress?.Find("Progress"), accentColor);
            SetGraphicColor(progress?.Find("Handle"), accentColor);

            List<Transform> detailChildren = new();
            if (details != null)
            {
                foreach (Transform child in details) detailChildren.Add(child);
            }

            foreach (Transform child in detailChildren)
            {
                child.gameObject.SetActive(child.name == "More");
            }

            TextMeshProUGUI closeText = details?.Find("More")?.GetComponent<TextMeshProUGUI>();
            Button closeButton = null;
            if (closeText != null)
            {
                closeText.text = "X";
                closeText.color = primaryTextColor;
                closeText.fontSize = 0.024f;
                closeText.raycastTarget = true;
                closeText.alignment = TextAlignmentOptions.Center;
                SetRect(closeText.rectTransform, new Vector2(0.94f, 0.88f), new Vector2(0.99f, 0.98f));
                closeButton = closeText.GetComponent<Button>();
                if (closeButton == null)
                {
                    closeButton = Undo.AddComponent<Button>(closeText.gameObject);
                }

                closeButton.targetGraphic = closeText;
                closeButton.transition = Selectable.Transition.ColorTint;
            }

            SetObjectReference(owner, "playerRoot", player);
            SetObjectReference(owner, "playerThumbnail", thumbnail?.GetComponent<Image>());
            SetObjectReference(owner, "playerMockImageNumber",
                thumbnail?.Find("MockImageNumber")?.GetComponent<TextMeshProUGUI>());
            SetObjectReference(owner, "playerVideoSurface", videoSurface?.GetComponent<RawImage>());
            SetObjectReference(owner, "playerVideoAspect", videoSurface?.GetComponent<AspectRatioFitter>());
            SetObjectReference(owner, "playerPlayPauseButton", play?.GetComponent<Button>());
            SetObjectReference(owner, "playerPlayPauseIcon", play?.GetComponent<PlaybackControlGraphic>());
            SetObjectReference(owner, "playerProgress", progress?.GetComponent<Slider>());
            SetObjectReference(owner, "playerCloseButton", closeButton);
            player?.gameObject.SetActive(false);
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            if (rect == null) return;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            EditorUtility.SetDirty(rect);
        }

        private static void SetGraphicColor(Transform target, Color color)
        {
            Graphic graphic = target?.GetComponent<Graphic>();
            if (graphic == null) return;
            graphic.color = color;
            EditorUtility.SetDirty(graphic);
        }

        private static void SetText(Transform target, string value, Color color, float fontSize,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            TextMeshProUGUI text = target?.GetComponent<TextMeshProUGUI>();
            if (text == null) return;
            text.text = value;
            text.color = color;
            text.fontSize = fontSize;
            text.alignment = alignment;
            EditorUtility.SetDirty(text);
        }

        private static void SetActive(Transform target, bool active)
        {
            if (target != null) target.gameObject.SetActive(active);
        }

        private static void SetObjectReference(SerializedObject owner, string propertyName, Object value)
        {
            SerializedProperty property = owner.FindProperty(propertyName);
            if (property != null) property.objectReferenceValue = value;
        }

        private static void SetRelativeReference(SerializedProperty owner, string propertyName, Object value)
        {
            SerializedProperty property = owner.FindPropertyRelative(propertyName);
            if (property != null) property.objectReferenceValue = value;
        }
    }
}
#endif
