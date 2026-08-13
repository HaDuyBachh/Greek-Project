using UnityEngine;
using UnityEngine.UI;

namespace GreekProject.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    [AddComponentMenu("UI/Playback Control Graphic")]
    public sealed class PlaybackControlGraphic : MaskableGraphic
    {
        [SerializeField] private Color backgroundColor = new(0f, 0f, 0f, 0.72f);
        [SerializeField] private Color iconColor = Color.white;
        [SerializeField, Range(16, 64)] private int circleSegments = 36;
        [SerializeField] private bool isPlaying;

        public bool IsPlaying
        {
            get => isPlaying;
            set
            {
                if (isPlaying == value)
                {
                    return;
                }

                isPlaying = value;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect rect = GetPixelAdjustedRect();
            Vector2 center = rect.center;
            float radius = Mathf.Min(rect.width, rect.height) * 0.5f;
            AddCircle(vh, center, radius, backgroundColor);

            if (isPlaying)
            {
                float barWidth = radius * 0.25f;
                float barHeight = radius * 0.92f;
                float gap = radius * 0.18f;
                AddQuad(vh, new Rect(center.x - gap - barWidth, center.y - barHeight * 0.5f, barWidth, barHeight), iconColor);
                AddQuad(vh, new Rect(center.x + gap, center.y - barHeight * 0.5f, barWidth, barHeight), iconColor);
                return;
            }

            float triangleHeight = radius * 1.05f;
            float triangleWidth = radius * 0.9f;
            AddTriangle(vh,
                new Vector2(center.x - triangleWidth * 0.36f, center.y - triangleHeight * 0.5f),
                new Vector2(center.x - triangleWidth * 0.36f, center.y + triangleHeight * 0.5f),
                new Vector2(center.x + triangleWidth * 0.64f, center.y),
                iconColor);
        }

        private void AddCircle(VertexHelper vh, Vector2 center, float radius, Color32 vertexColor)
        {
            int centerIndex = vh.currentVertCount;
            AddVertex(vh, center, vertexColor);

            for (int index = 0; index <= circleSegments; index++)
            {
                float angle = index / (float)circleSegments * Mathf.PI * 2f;
                AddVertex(vh, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, vertexColor);
            }

            for (int index = 0; index < circleSegments; index++)
            {
                vh.AddTriangle(centerIndex, centerIndex + index + 1, centerIndex + index + 2);
            }
        }

        private static void AddQuad(VertexHelper vh, Rect rect, Color32 vertexColor)
        {
            int start = vh.currentVertCount;
            AddVertex(vh, new Vector2(rect.xMin, rect.yMin), vertexColor);
            AddVertex(vh, new Vector2(rect.xMin, rect.yMax), vertexColor);
            AddVertex(vh, new Vector2(rect.xMax, rect.yMax), vertexColor);
            AddVertex(vh, new Vector2(rect.xMax, rect.yMin), vertexColor);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start + 2, start + 3, start);
        }

        private static void AddTriangle(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color32 vertexColor)
        {
            int start = vh.currentVertCount;
            AddVertex(vh, a, vertexColor);
            AddVertex(vh, b, vertexColor);
            AddVertex(vh, c, vertexColor);
            vh.AddTriangle(start, start + 1, start + 2);
        }

        private static void AddVertex(VertexHelper vh, Vector2 position, Color32 vertexColor)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = vertexColor;
            vh.AddVert(vertex);
        }
    }
}
