using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

namespace RollerCoasterSim.Objects
{
    public class TreeObject
    {
        public List<Vector3> Positions { get; private set; } = new();
        public List<(float trunkHeight, float trunkRadius, float leavesHeight, float leavesRadius)> Scales { get; private set; } = new();
        public int? SelectedIndex { get; set; } = null;

        private int _treeTrunkVAO, _treeTrunkVBO, _treeTrunkIndexCount;
        private int _treeLeavesVAO, _treeLeavesVBO, _treeLeavesIndexCount;

        public void GeneratePositions(Func<float, float, float> getTerrainHeight, int count = 100)
        {
            var rand = new Random(42);
            Positions.Clear();
            Scales.Clear();
            var treeTypes = new[]
            {
                (height: 4.0f, trunkRadius: 0.15f, leavesHeight: 3.0f, leavesRadius: 0.8f, probability: 0.3f),
                (height: 2.5f, trunkRadius: 0.2f, leavesHeight: 2.0f, leavesRadius: 1.2f, probability: 0.5f),
                (height: 1.5f, trunkRadius: 0.1f, leavesHeight: 1.2f, leavesRadius: 1.5f, probability: 0.2f)
            };
            const float lakeRadius = 15.0f;
            for (int i = 0; i < count; i++)
            {
                float x = (float)(rand.NextDouble() * 180 - 90);
                float z = (float)(rand.NextDouble() * 180 - 90);
                float distanceFromLakeCenter = (float)Math.Sqrt(x * x + z * z);
                if (distanceFromLakeCenter < lakeRadius + 2.0f) { i--; continue; }
                if (MathF.Abs(x) < 10 && MathF.Abs(z) < 10) { i--; continue; }
                float y = getTerrainHeight(x, z);
                float r = (float)rand.NextDouble();
                float cumulative = 0;
                var selectedType = treeTypes[0];
                foreach (var type in treeTypes)
                {
                    cumulative += type.probability;
                    if (r <= cumulative) { selectedType = type; break; }
                }
                float heightVariation = 0.8f + (float)rand.NextDouble() * 0.4f;
                float radiusVariation = 0.9f + (float)rand.NextDouble() * 0.2f;
                Positions.Add(new Vector3(x, y, z));
                Scales.Add((selectedType.height * heightVariation, selectedType.trunkRadius * radiusVariation, selectedType.leavesHeight * heightVariation, selectedType.leavesRadius * radiusVariation));
            }
        }

        public void CreateMesh()
        {
            const int segments = 12;
            List<float> trunkVertices = new List<float>();
            List<uint> trunkIndices = new List<uint>();
            List<float> leavesVertices = new List<float>();
            List<uint> leavesIndices = new List<uint>();

            // Create base trunk vertices (will be scaled per tree)
            for (int i = 0; i <= segments; i++)
            {
                float theta = i * MathF.PI * 2 / segments;
                float x = MathF.Cos(theta);
                float z = MathF.Sin(theta);
                // Bottom vertices
                trunkVertices.Add(x); trunkVertices.Add(0); trunkVertices.Add(z);
                trunkVertices.Add(0.5f); trunkVertices.Add(0.25f); trunkVertices.Add(0.1f); // normal
                trunkVertices.Add((float)i / segments); trunkVertices.Add(0);
                // Top vertices
                trunkVertices.Add(x); trunkVertices.Add(1); trunkVertices.Add(z);
                trunkVertices.Add(0.5f); trunkVertices.Add(0.25f); trunkVertices.Add(0.1f); // normal
                trunkVertices.Add((float)i / segments); trunkVertices.Add(1);
            }
            // Create trunk indices
            for (int i = 0; i < segments; i++)
            {
                uint baseIdx = (uint)(i * 2);
                trunkIndices.Add(baseIdx); trunkIndices.Add(baseIdx + 1); trunkIndices.Add(baseIdx + 2);
                trunkIndices.Add(baseIdx + 1); trunkIndices.Add(baseIdx + 3); trunkIndices.Add(baseIdx + 2);
            }
            // Create base leaves vertices (will be scaled per tree)
            for (int i = 0; i <= segments; i++)
            {
                float theta = i * MathF.PI * 2 / segments;
                float x = MathF.Cos(theta);
                float z = MathF.Sin(theta);
                // Bottom vertices
                leavesVertices.Add(x); leavesVertices.Add(0); leavesVertices.Add(z);
                leavesVertices.Add(0.1f); leavesVertices.Add(0.5f); leavesVertices.Add(0.1f); // normal
                leavesVertices.Add((float)i / segments); leavesVertices.Add(0);
                // Top vertices (point)
                leavesVertices.Add(0); leavesVertices.Add(1); leavesVertices.Add(0);
                leavesVertices.Add(0.1f); leavesVertices.Add(0.5f); leavesVertices.Add(0.1f); // normal
                leavesVertices.Add(0.5f); leavesVertices.Add(1);
            }
            // Create leaves indices
            for (int i = 0; i < segments; i++)
            {
                uint baseIdx = (uint)(i * 2);
                leavesIndices.Add(baseIdx); leavesIndices.Add(baseIdx + 1); leavesIndices.Add((uint)((i + 1) % segments) * 2);
            }
            // Create and bind VAOs
            _treeTrunkVAO = GL.GenVertexArray();
            _treeLeavesVAO = GL.GenVertexArray();
            // Set up trunk VAO
            GL.BindVertexArray(_treeTrunkVAO);
            _treeTrunkVBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _treeTrunkVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, trunkVertices.Count * sizeof(float), trunkVertices.ToArray(), BufferUsageHint.StaticDraw);
            int trunkStride = 8 * sizeof(float);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, trunkStride, 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, trunkStride, 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, trunkStride, 6 * sizeof(float));
            GL.EnableVertexAttribArray(2);
            _treeTrunkIndexCount = trunkIndices.Count;
            int trunkEBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, trunkEBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, trunkIndices.Count * sizeof(uint), trunkIndices.ToArray(), BufferUsageHint.StaticDraw);
            // Set up leaves VAO
            GL.BindVertexArray(_treeLeavesVAO);
            _treeLeavesVBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _treeLeavesVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, leavesVertices.Count * sizeof(float), leavesVertices.ToArray(), BufferUsageHint.StaticDraw);
            int leavesStride = 8 * sizeof(float);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, leavesStride, 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, leavesStride, 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, leavesStride, 6 * sizeof(float));
            GL.EnableVertexAttribArray(2);
            _treeLeavesIndexCount = leavesIndices.Count;
            int leavesEBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, leavesEBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, leavesIndices.Count * sizeof(uint), leavesIndices.ToArray(), BufferUsageHint.StaticDraw);
        }

        public void Draw(int shaderProgram)
        {
            for (int i = 0; i < Positions.Count; i++)
            {
                Vector3 pos = Positions[i];
                var scale = Scales[i];
                // Draw trunk
                GL.BindVertexArray(_treeTrunkVAO);
                Matrix4 trunkModel = Matrix4.CreateScale(scale.trunkRadius, scale.trunkHeight, scale.trunkRadius) * Matrix4.CreateTranslation(pos);
                GL.UniformMatrix4(GL.GetUniformLocation(shaderProgram, "model"), false, ref trunkModel);
                GL.Uniform3(GL.GetUniformLocation(shaderProgram, "objectColor"), new Vector3(0.5f, 0.25f, 0.1f));
                GL.DrawElements(PrimitiveType.Triangles, _treeTrunkIndexCount, DrawElementsType.UnsignedInt, 0);
                // Draw leaves
                GL.BindVertexArray(_treeLeavesVAO);
                Matrix4 leavesModel = Matrix4.CreateScale(scale.leavesRadius, scale.leavesHeight, scale.leavesRadius) * Matrix4.CreateTranslation(pos + new Vector3(0, scale.trunkHeight, 0));
                GL.UniformMatrix4(GL.GetUniformLocation(shaderProgram, "model"), false, ref leavesModel);
                Vector3 leavesColor = (scale.trunkHeight > 3.5f) ? new Vector3(0.1f, 0.4f, 0.1f) : (scale.trunkHeight > 2.0f) ? new Vector3(0.2f, 0.5f, 0.2f) : new Vector3(0.15f, 0.45f, 0.15f);
                if (SelectedIndex.HasValue && SelectedIndex.Value == i)
                    leavesColor = new Vector3(1.0f, 1.0f, 0.0f); // Highlight yellow
                GL.Uniform3(GL.GetUniformLocation(shaderProgram, "objectColor"), leavesColor);
                GL.DrawElements(PrimitiveType.Triangles, _treeLeavesIndexCount, DrawElementsType.UnsignedInt, 0);
                // Draw shadow (optional)
            }
        }

        public void MoveSelected(Vector3 delta)
        {
            if (SelectedIndex.HasValue && SelectedIndex.Value >= 0 && SelectedIndex.Value < Positions.Count)
            {
                Positions[SelectedIndex.Value] += delta;
            }
        }
    }
} 