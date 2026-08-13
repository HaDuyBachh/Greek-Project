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
            public string title;
            public string channel;
            public string description;
            public string views;
            public string published;
            public string duration;
            public string likes;
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
