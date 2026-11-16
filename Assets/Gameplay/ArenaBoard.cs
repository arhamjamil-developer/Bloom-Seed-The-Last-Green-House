using System;
using UnityEngine;

namespace Bloombots.Gameplay
{
    internal enum PlotMark { None, Incoming, Legal, Blocked, Step, FirstThreat }

    internal sealed class ArenaBoard : IDisposable
    {
        public const int Lanes = 5;
        public const int Rows = 8;
        public const float Height = 12;
        private const float FarToNearWidth = .52f;

        private readonly Camera camera;
        private float centerX, farY = 2, nearY = -1.2f, nearWidth = 4.8f;
        private Rect fittedViewport;
        private float fittedAspect;
        private readonly Mesh grid;
        private readonly MeshRenderer renderer;
        private readonly MaterialPropertyBlock shading = new();
        private static readonly int Night = Shader.PropertyToID("_Night");
        private readonly Vector3[] vertices = new Vector3[Lanes * Rows * 4];
        private readonly Color32[] cellColors = new Color32[Lanes * Rows * 4];
        private readonly Vector2[] cellMarks = new Vector2[Lanes * Rows * 4];
        private readonly Vector4[] rootEdges = new Vector4[Lanes * Rows * 4];
        private bool dirty;

        public ArenaBoard(MeshFilter surface, Camera camera)
        {
            this.camera = camera;
            renderer = surface.GetComponent<MeshRenderer>();
            grid = new Mesh { name = "Perspective arena plots" };
            grid.MarkDynamic();
            surface.sharedMesh = grid;

            var uv = new Vector2[vertices.Length];
            var indices = new int[Lanes * Rows * 6];
            for (int row = 0; row < Rows; row++)
            for (int lane = 0; lane < Lanes; lane++)
            {
                int cell = row * Lanes + lane;
                int v = cell * 4;
                uv[v] = Vector2.zero;
                uv[v + 1] = Vector2.right;
                uv[v + 2] = Vector2.up;
                uv[v + 3] = Vector2.one;
                int t = cell * 6;
                indices[t] = v;
                indices[t + 1] = v + 2;
                indices[t + 2] = v + 1;
                indices[t + 3] = v + 1;
                indices[t + 4] = v + 2;
                indices[t + 5] = v + 3;
            }
            RebuildMesh();
            grid.uv = uv;
            grid.triangles = indices;
            grid.colors32 = cellColors;
            grid.uv2 = cellMarks;
            grid.SetUVs(2, rootEdges);
        }

        public static float WideCameraSize(float aspect) => Mathf.Max(Height * .5f, 2.65f / Mathf.Max(.1f, aspect));

        public bool Fit(Rect viewport)
        {
            if (viewport.width <= 0 || viewport.height <= 0 ||
                (viewport == fittedViewport && Mathf.Approximately(camera.aspect, fittedAspect)))
                return false;

            fittedViewport = viewport;
            fittedAspect = camera.aspect;
            float viewHeight = WideCameraSize(camera.aspect) * 2;
            float viewWidth = viewHeight * camera.aspect;
            centerX = (viewport.center.x - .5f) * viewWidth;
            nearWidth = viewport.width * .90f * viewWidth;
            nearY = (Mathf.Lerp(viewport.yMin, viewport.yMax, .175f) - .5f) * viewHeight;
            farY = (Mathf.Lerp(viewport.yMin, viewport.yMax, .82f) - .5f) * viewHeight;
            RebuildMesh();
            return true;
        }

        private void RebuildMesh()
        {
            for (int row = 0; row < Rows; row++)
            for (int lane = 0; lane < Lanes; lane++)
            {
                int v = (row * Lanes + lane) * 4;
                vertices[v] = World(lane, row + 1);
                vertices[v + 1] = World(lane + 1, row + 1);
                vertices[v + 2] = World(lane, row);
                vertices[v + 3] = World(lane + 1, row);
            }
            grid.vertices = vertices;
            grid.RecalculateBounds();
        }

        public static bool Contains(int lane, int row) => lane >= 0 && lane < Lanes && row >= 0 && row < Rows;

        public Vector3 World(float lane, float row)
        {
            float r = row / Rows;
            float t = r * FarToNearWidth / (1 + (FarToNearWidth - 1) * r);
            float span = Mathf.LerpUnclamped(FarToNearWidth, 1, t) * nearWidth;
            return new Vector3(centerX + (lane / Lanes - .5f) * span, Mathf.LerpUnclamped(farY, nearY, t), 0);
        }

        public static float DepthScale(float row) => Mathf.Lerp(.70f, 1f, Mathf.Clamp01(row / Rows));

        public bool TryCell(Vector2 screen, out Vector2Int cell)
        {
            cell = default;
            var ray = camera.ScreenPointToRay(screen);
            if (!new Plane(Vector3.forward, Vector3.zero).Raycast(ray, out float distance))
                return false;

            Vector3 point = ray.GetPoint(distance);
            float t = (point.y - farY) / (nearY - farY);
            if (t < 0 || t >= 1)
                return false;

            float r = t / (FarToNearWidth + (1 - FarToNearWidth) * t);
            float span = Mathf.LerpUnclamped(FarToNearWidth, 1, t) * nearWidth;
            cell = new Vector2Int(Mathf.FloorToInt(((point.x - centerX) / span + .5f) * Lanes), Mathf.FloorToInt(r * Rows));
            return Contains(cell.x, cell.y);
        }

        public void Paint(int lane, int row, Color32 color, PlotMark mark, Vector4 roots)
        {
            int i = (row * Lanes + lane) * 4;
            if (cellColors[i].Equals(color) && cellMarks[i].x == (int)mark && rootEdges[i] == roots)
                return;
            for (int corner = 0; corner < 4; corner++)
            {
                cellColors[i + corner] = color;
                cellMarks[i + corner] = new Vector2((int)mark, 0);
                rootEdges[i + corner] = roots;
            }
            dirty = true;
        }

        public void Draw(float night)
        {
            shading.SetFloat(Night, night);
            renderer.SetPropertyBlock(shading);
            if (!dirty)
                return;
            grid.colors32 = cellColors;
            grid.uv2 = cellMarks;
            grid.SetUVs(2, rootEdges);
            dirty = false;
        }

        public void Dispose()
        {
            UnityEngine.Object.Destroy(grid);
        }
    }
}
