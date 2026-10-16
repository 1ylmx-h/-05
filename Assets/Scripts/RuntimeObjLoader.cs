using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace GlobalDeformationDemo
{
    public static class RuntimeObjLoader
    {
        private readonly struct VertexKey : IEquatable<VertexKey>
        {
            public readonly int Position;
            public readonly int TexCoord;
            public readonly int Normal;

            public VertexKey(int position, int texCoord, int normal)
            {
                Position = position;
                TexCoord = texCoord;
                Normal = normal;
            }

            public bool Equals(VertexKey other)
            {
                return Position == other.Position && TexCoord == other.TexCoord && Normal == other.Normal;
            }

            public override bool Equals(object obj)
            {
                return obj is VertexKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = Position;
                    hash = hash * 397 ^ TexCoord;
                    hash = hash * 397 ^ Normal;
                    return hash;
                }
            }
        }

        public static Mesh LoadFromFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("OBJ path is empty.", nameof(path));
            }
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("OBJ file was not found.", path);
            }
            return Parse(File.ReadAllText(path), Path.GetFileNameWithoutExtension(path));
        }

        public static Mesh Parse(string text, string meshName = "Imported OBJ")
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("OBJ content is empty.", nameof(text));
            }

            List<Vector3> sourcePositions = new List<Vector3>();
            List<Vector2> sourceTexCoords = new List<Vector2>();
            List<Vector3> sourceNormals = new List<Vector3>();
            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> texCoords = new List<Vector2>();
            List<Vector3> normals = new List<Vector3>();
            List<int> triangles = new List<int>();
            Dictionary<VertexKey, int> map = new Dictionary<VertexKey, int>();
            bool hasTexCoords = false;
            bool hasNormals = false;

            string[] lines = text.Replace("\r", string.Empty).Split('\n');
            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line[0] == '#')
                {
                    continue;
                }

                string[] parts = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                {
                    continue;
                }

                switch (parts[0])
                {
                    case "v" when parts.Length >= 4:
                        sourcePositions.Add(new Vector3(
                            -ParseFloat(parts[1]),
                            ParseFloat(parts[2]),
                            ParseFloat(parts[3])));
                        break;
                    case "vt" when parts.Length >= 3:
                        sourceTexCoords.Add(new Vector2(ParseFloat(parts[1]), ParseFloat(parts[2])));
                        break;
                    case "vn" when parts.Length >= 4:
                        sourceNormals.Add(new Vector3(
                            -ParseFloat(parts[1]),
                            ParseFloat(parts[2]),
                            ParseFloat(parts[3])).normalized);
                        break;
                    case "f" when parts.Length >= 4:
                    {
                        int[] face = new int[parts.Length - 1];
                        for (int index = 1; index < parts.Length; index++)
                        {
                            VertexKey key = ParseVertexKey(
                                parts[index],
                                sourcePositions.Count,
                                sourceTexCoords.Count,
                                sourceNormals.Count);

                            if (!map.TryGetValue(key, out int mappedIndex))
                            {
                                mappedIndex = vertices.Count;
                                map.Add(key, mappedIndex);
                                vertices.Add(sourcePositions[key.Position]);

                                if (key.TexCoord >= 0)
                                {
                                    texCoords.Add(sourceTexCoords[key.TexCoord]);
                                    hasTexCoords = true;
                                }
                                else
                                {
                                    texCoords.Add(Vector2.zero);
                                }

                                if (key.Normal >= 0)
                                {
                                    normals.Add(sourceNormals[key.Normal]);
                                    hasNormals = true;
                                }
                                else
                                {
                                    normals.Add(Vector3.zero);
                                }
                            }
                            face[index - 1] = mappedIndex;
                        }

                        for (int index = 1; index < face.Length - 1; index++)
                        {
                            triangles.Add(face[0]);
                            triangles.Add(face[index + 1]);
                            triangles.Add(face[index]);
                        }
                        break;
                    }
                }
            }

            if (vertices.Count == 0 || triangles.Count == 0)
            {
                throw new FormatException("OBJ does not contain renderable vertices and faces.");
            }

            Mesh mesh = new Mesh { name = meshName };
            if (vertices.Count > 65535)
            {
                mesh.indexFormat = IndexFormat.UInt32;
            }
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0, true);
            if (hasTexCoords)
            {
                mesh.SetUVs(0, texCoords);
            }
            if (hasNormals)
            {
                mesh.SetNormals(normals);
            }
            else
            {
                mesh.RecalculateNormals();
            }
            mesh.RecalculateBounds();
            return mesh;
        }

        private static VertexKey ParseVertexKey(
            string token,
            int positionCount,
            int texCoordCount,
            int normalCount)
        {
            string[] indices = token.Split('/');
            int position = ResolveIndex(indices[0], positionCount);
            int texCoord = indices.Length > 1 && indices[1].Length > 0
                ? ResolveIndex(indices[1], texCoordCount)
                : -1;
            int normal = indices.Length > 2 && indices[2].Length > 0
                ? ResolveIndex(indices[2], normalCount)
                : -1;
            return new VertexKey(position, texCoord, normal);
        }

        private static int ResolveIndex(string token, int count)
        {
            int raw = int.Parse(token, NumberStyles.Integer, CultureInfo.InvariantCulture);
            int result = raw > 0 ? raw - 1 : count + raw;
            if (result < 0 || result >= count)
            {
                throw new FormatException($"OBJ index {raw} is outside the available range {count}.");
            }
            return result;
        }

        private static float ParseFloat(string token)
        {
            return float.Parse(token, NumberStyles.Float, CultureInfo.InvariantCulture);
        }
    }
}
