using System;
using UnityEngine;

namespace GlobalDeformationDemo
{
    public static class DeformationMath
    {
        private const float Epsilon = 0.00001f;

        public static Vector3 Taper(Vector3 vertex, Bounds bounds, float amount)
        {
            float height = Mathf.Max(bounds.size.y, Epsilon);
            float t = Mathf.Clamp01((vertex.y - bounds.min.y) / height);
            float scale = Mathf.Max(0.05f, 1f + amount * t);
            Vector3 center = bounds.center;
            return new Vector3(
                center.x + (vertex.x - center.x) * scale,
                vertex.y,
                center.z + (vertex.z - center.z) * scale);
        }

        public static Vector3 Twist(Vector3 vertex, Bounds bounds, float totalAngleDegrees)
        {
            float height = Mathf.Max(bounds.size.y, Epsilon);
            float t = Mathf.Clamp01((vertex.y - bounds.min.y) / height);
            float angle = totalAngleDegrees * (t - 0.5f) * Mathf.Deg2Rad;
            float cosine = Mathf.Cos(angle);
            float sine = Mathf.Sin(angle);
            float x = vertex.x - bounds.center.x;
            float z = vertex.z - bounds.center.z;
            return new Vector3(
                bounds.center.x + x * cosine - z * sine,
                vertex.y,
                bounds.center.z + x * sine + z * cosine);
        }

        public static Vector3 Bend(Vector3 vertex, Bounds bounds, float totalAngleDegrees)
        {
            float angle = totalAngleDegrees * Mathf.Deg2Rad;
            if (Mathf.Abs(angle) < Epsilon)
            {
                return vertex;
            }

            float height = Mathf.Max(bounds.size.y, Epsilon);
            float radius = height / angle;
            float localY = vertex.y - bounds.min.y;
            float theta = localY / radius;
            float localX = vertex.x - bounds.center.x;
            float radialDistance = radius - localX;

            return new Vector3(
                bounds.center.x + radius - radialDistance * Mathf.Cos(theta),
                bounds.min.y + radialDistance * Mathf.Sin(theta),
                vertex.z);
        }

        public static Vector3[] CreateRegularLattice(Bounds bounds, int resolution)
        {
            resolution = Mathf.Clamp(resolution, 2, 6);
            Vector3[] lattice = new Vector3[resolution * resolution * resolution];
            for (int i = 0; i < resolution; i++)
            {
                float s = i / (float)(resolution - 1);
                for (int j = 0; j < resolution; j++)
                {
                    float t = j / (float)(resolution - 1);
                    for (int k = 0; k < resolution; k++)
                    {
                        float u = k / (float)(resolution - 1);
                        lattice[Index(i, j, k, resolution)] = new Vector3(
                            Mathf.Lerp(bounds.min.x, bounds.max.x, s),
                            Mathf.Lerp(bounds.min.y, bounds.max.y, t),
                            Mathf.Lerp(bounds.min.z, bounds.max.z, u));
                    }
                }
            }
            return lattice;
        }

        public static Vector3 FreeFormDeform(
            Vector3 vertex,
            Bounds originalBounds,
            Vector3[] lattice,
            int resolution)
        {
            if (lattice == null || lattice.Length != resolution * resolution * resolution)
            {
                throw new ArgumentException("FFD lattice size does not match its resolution.", nameof(lattice));
            }

            Vector3 size = originalBounds.size;
            float s = Normalize(vertex.x, originalBounds.min.x, size.x);
            float t = Normalize(vertex.y, originalBounds.min.y, size.y);
            float u = Normalize(vertex.z, originalBounds.min.z, size.z);
            int degree = resolution - 1;
            Vector3 result = Vector3.zero;

            for (int i = 0; i < resolution; i++)
            {
                float basisS = Bernstein(degree, i, s);
                for (int j = 0; j < resolution; j++)
                {
                    float basisT = Bernstein(degree, j, t);
                    for (int k = 0; k < resolution; k++)
                    {
                        float weight = basisS * basisT * Bernstein(degree, k, u);
                        result += lattice[Index(i, j, k, resolution)] * weight;
                    }
                }
            }
            return result;
        }

        public static int Index(int i, int j, int k, int resolution)
        {
            return (i * resolution + j) * resolution + k;
        }

        private static float Normalize(float value, float minimum, float length)
        {
            return Mathf.Clamp01((value - minimum) / Mathf.Max(length, Epsilon));
        }

        private static float Bernstein(int degree, int index, float value)
        {
            return Binomial(degree, index)
                * Mathf.Pow(value, index)
                * Mathf.Pow(1f - value, degree - index);
        }

        private static int Binomial(int n, int k)
        {
            k = Math.Min(k, n - k);
            int result = 1;
            for (int i = 1; i <= k; i++)
            {
                result = result * (n - k + i) / i;
            }
            return result;
        }
    }
}
