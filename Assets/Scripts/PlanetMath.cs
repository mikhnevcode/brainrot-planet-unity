using UnityEngine;

namespace BrainrotPlanet
{
    public static class PlanetMath
    {
        // A larger planet makes boosts cover less of the total circumference.
        public const float PreviousRadius = 14f;
        public const float Radius = 24f;
        public const float TraversalScale = PreviousRadius / Radius;
        public const int Width = 256;
        public const int Height = 128;
        public const int CellCount = Width * Height;

        public static Vector3 FromLonLat(float u, float v)
        {
            float lon = (u - .5f) * 2f * Mathf.PI;
            float lat = (v - .5f) * Mathf.PI;
            return new Vector3(Mathf.Cos(lat) * Mathf.Cos(lon), Mathf.Sin(lat),
                               Mathf.Cos(lat) * Mathf.Sin(lon));
        }

        public static Vector2 ToUV(Vector3 point)
        {
            Vector3 n = point.normalized;
            float u = Mathf.Atan2(n.z, n.x) / (2f * Mathf.PI) + .5f;
            return new Vector2(Mathf.Repeat(u, 1f), Mathf.Clamp01(Mathf.Asin(n.y) / Mathf.PI + .5f));
        }

        public static int ToCell(Vector3 point)
        {
            Vector2 uv = ToUV(point);
            int x = Mathf.Clamp((int)(uv.x * Width), 0, Width - 1);
            int y = Mathf.Clamp((int)(uv.y * Height), 0, Height - 1);
            return y * Width + x;
        }
        public static Vector3 CellDirection(int cell)
        {
            return FromLonLat(((cell % Width) + .5f) / Width,
                              ((cell / Width) + .5f) / Height);
        }
        public static float CellAreaWeight(int cell)
        {
            float v = ((cell / Width) + .5f) / Height;
            return Mathf.Max(.001f, Mathf.Sin(v * Mathf.PI));
        }
        public static Vector3 Tangent(Vector3 at, Vector3 goal)
        {
            return Vector3.ProjectOnPlane(goal - at, at.normalized).normalized;
        }
        public static Vector3 RandomAround(Vector3 direction, float radians)
        {
            Vector3 axis = Vector3.Cross(direction, Vector3.up);
            if (axis.sqrMagnitude < .05f) axis = Vector3.Cross(direction, Vector3.right);
            axis.Normalize();
            axis = Quaternion.AngleAxis(Random.Range(0f, 360f), direction) * axis;
            return (Quaternion.AngleAxis(radians * Mathf.Rad2Deg, axis) * direction).normalized;
        }
    }
}
