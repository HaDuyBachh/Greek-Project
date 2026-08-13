using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GreekProject.UI
{
    [ExecuteAlways]
    [RequireComponent(typeof(CanvasRenderer))]
    [AddComponentMenu("UI/Rounded Rect Graphic")]
    public sealed class RoundedRectGraphic : MaskableGraphic
    {
        [SerializeField, Range(0f, 0.5f)] private float radiusRatio = 0.12f;
        [SerializeField, Min(1)] private int cornerSegments = 8;
        [SerializeField] private bool showMaskGraphic = true;
        [SerializeField] private bool clipChildren = true;

        public float RadiusRatio
        {
            get => radiusRatio;
            set
            {
                radiusRatio = Mathf.Clamp01(value);
                if (radiusRatio > 0.5f)
                {
                    radiusRatio = 0.5f;
                }

                SetVerticesDirty();
            }
        }

        public int CornerSegments
        {
            get => cornerSegments;
            set
            {
                cornerSegments = Mathf.Max(1, value);
                SetVerticesDirty();
            }
        }

        public bool ClipChildren
        {
            get => clipChildren;
            set
            {
                clipChildren = value;
                EnsureMask();
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            EnsureCompatibleMaterial();
            EnsureMask();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            EnsureCompatibleMaterial();
            ApplyMaskSettings();
            SetVerticesDirty();
        }
#endif

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect rect = GetPixelAdjustedRect();
            float halfWidth = rect.width * 0.5f;
            float halfHeight = rect.height * 0.5f;
            float radius = Mathf.Min(halfWidth, halfHeight) * radiusRatio * 2f;

            if (radius <= 0f)
            {
                AddRect(vh, rect);
                return;
            }

            var points = new List<Vector2>((cornerSegments + 1) * 4);
            AddCorner(points, new Vector2(rect.xMax - radius, rect.yMax - radius), radius, 0f, 90f);
            AddCorner(points, new Vector2(rect.xMin + radius, rect.yMax - radius), radius, 90f, 180f);
            AddCorner(points, new Vector2(rect.xMin + radius, rect.yMin + radius), radius, 180f, 270f);
            AddCorner(points, new Vector2(rect.xMax - radius, rect.yMin + radius), radius, 270f, 360f);

            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;
            vertex.position = rect.center;
            vh.AddVert(vertex);

            for (int i = 0; i < points.Count; i++)
            {
                vertex.position = points[i];
                vh.AddVert(vertex);
            }

            for (int i = 1; i <= points.Count; i++)
            {
                int next = i == points.Count ? 1 : i + 1;
                vh.AddTriangle(0, next, i);
            }
        }

        private void AddRect(VertexHelper vh, Rect rect)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;

            vertex.position = new Vector2(rect.xMin, rect.yMin);
            vh.AddVert(vertex);
            vertex.position = new Vector2(rect.xMin, rect.yMax);
            vh.AddVert(vertex);
            vertex.position = new Vector2(rect.xMax, rect.yMax);
            vh.AddVert(vertex);
            vertex.position = new Vector2(rect.xMax, rect.yMin);
            vh.AddVert(vertex);

            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }

        private void AddCorner(List<Vector2> points, Vector2 center, float radius, float startAngle, float endAngle)
        {
            for (int i = 0; i <= cornerSegments; i++)
            {
                float t = i / (float)cornerSegments;
                float angle = Mathf.Lerp(startAngle, endAngle, t) * Mathf.Deg2Rad;
                points.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
        }

        private void EnsureMask()
        {
            Mask mask = GetComponent<Mask>();
            if (clipChildren && mask == null)
            {
                mask = gameObject.AddComponent<Mask>();
            }

            if (mask != null)
            {
                mask.enabled = clipChildren;
                mask.showMaskGraphic = showMaskGraphic;
            }
        }

        private void ApplyMaskSettings()
        {
            Mask mask = GetComponent<Mask>();
            if (mask != null)
            {
                mask.enabled = clipChildren;
                mask.showMaskGraphic = showMaskGraphic;
            }
        }

        private void EnsureCompatibleMaterial()
        {
            if (material != null && !material.HasProperty("_Stencil"))
            {
                material = null;
            }
        }
    }
}
