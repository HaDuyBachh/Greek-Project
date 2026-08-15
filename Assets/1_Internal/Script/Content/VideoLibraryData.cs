using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

namespace GreekProject.Content
{
    public enum VideoContentEffect
    {
        Normal,
        Brainrot,
        Horror
    }

    [CreateAssetMenu(fileName = "PhoneVideoLibrary", menuName = "Greek Project/Phone Video Library")]
    public sealed class VideoLibraryData : ScriptableObject
    {
        [Serializable]
        public sealed class VideoEntry
        {
            public string id;
            [Tooltip("File stem shared by Video_Processed and Resources/VideoFrames.")]
            public string sourceStem;
            public string title;
            [Tooltip("Uploader name displayed for this video.")]
            public string channel;
            [Tooltip("Optional uploader avatar. Leave empty to display the uploader initial.")]
            public Sprite channelAvatar;
            [Range(0, 11)] public int channelAvatarIndex;
            [Tooltip("Fallback logo color used when the uploader has no avatar sprite.")]
            public Color channelColor = new Color(1f, 0f, 0.2f, 1f);
            public string subscribers;
            public string description;
            public string views;
            public string published;
            public string duration;
            public string likes;
            public string comments;
            public Sprite thumbnail;
            public VideoClip videoClip;
            public RenderTexture outputTexture;
            public VideoContentEffect contentEffect;
            [Range(1, 99)] public int mockImageNumber = 1;
            public Color mockColor = Color.white;

            public string Metadata => $"{channel} | {views} | {published}";
        }

        [SerializeField] private List<VideoEntry> videos = new List<VideoEntry>();

        public IReadOnlyList<VideoEntry> Videos => videos;
    }
}
