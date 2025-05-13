using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using RollerCoasterSim.Track;
using RollerCoasterSim.Train;

namespace RollerCoasterSim
{
    public class Renderer
    {
        private int _shaderProgram;
        private int _trackVAO;
        private int _trainVAO;
        private int _trackVBO;
        private int _trainVBO;
        private List<Vector3> _trackVertices;
        private List<Vector3> _trainVertices;
        private int _trackIndexCount;
        private int _groundVAO;
        private int _groundIndexCount;
        private Vector3 _lightPos = new Vector3(10f, 10f, 10f);
        private Vector3 _lightColor = new Vector3(1f, 1f, 1f);
        private int _skyVAO;
        private int _skyIndexCount;
        private List<Vector3> _treePositions = new List<Vector3>();
        private int _treeTrunkVAO;
        private int _treeTrunkVBO;
        private int _treeLeavesVAO;
        private int _treeLeavesVBO;
        private int _treeTrunkIndexCount;
        private int _treeLeavesIndexCount;
        private int _debugFrameCount = 0;
        private int _supportVAO;
        private int _supportIndexCount;
        private List<Vector3> _supportPositions = new List<Vector3>();
        private RollerCoasterTrack _track;
        private List<(float trunkHeight, float trunkRadius, float leavesHeight, float leavesRadius)> _treeScales = new();
        private int _platformVAO;
        private int _platformIndexCount;
        private int _fencePostVAO;
        private int _fencePostIndexCount;
        private int _fenceRailVAO;
        private int _fenceRailIndexCount;
        private int _stationVAO;
        private int _stationIndexCount;
        private int _trackEBO;
        private int _trainEBO;
        private int _sunVAO;
        private int _sunIndexCount;

        public void Init()
        {
            Console.WriteLine("Renderer.Init: Starting initialization");
            try
            {
                // Initialize shader program
                _shaderProgram = CreateShaderProgram();

                // Initialize track geometry
                _trackVertices = new List<Vector3>();
                _trackVAO = GL.GenVertexArray();
                _trackVBO = GL.GenBuffer();

                // Initialize train geometry
                _trainVertices = new List<Vector3>();
                _trainVAO = GL.GenVertexArray();
                _trainVBO = GL.GenBuffer();

                // Create train mesh
                CreateTrainMesh();

                // Initialize track
                _track = new RollerCoasterTrack();

                // Create meshes
                CreateGroundMesh();
                CreateTrackMesh();
                GenerateTreePositions();
                CreateTreeMesh();
                CreateSunMesh();
                // GenerateSupportPositions();
                // CreateSupportMesh();
                // CreatePlatformMesh();
                // CreateFenceMesh();
                // CreateStationMesh();

                // Set up lighting
                GL.UseProgram(_shaderProgram);
                GL.Uniform3(GL.GetUniformLocation(_shaderProgram, "lightPos"), new Vector3(50, 50, 50));
                GL.Uniform3(GL.GetUniformLocation(_shaderProgram, "lightColor"), new Vector3(1, 1, 1));
                GL.Uniform3(GL.GetUniformLocation(_shaderProgram, "viewPos"), new Vector3(0, 10, 20));

                // Enable depth testing
                GL.Enable(EnableCap.DepthTest);
                GL.DepthFunc(DepthFunction.Less);

                Console.WriteLine("Renderer.Init: Initialization completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in Renderer.Init: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private int CreateShaderProgram()
        {
            // Vertex shader
            string vertexShaderSource = @"
                #version 330 core
                layout (location = 0) in vec3 aPosition;
                layout (location = 1) in vec3 aNormal;
                layout (location = 2) in vec2 aTexCoord;

                out vec3 FragPos;
                out vec3 Normal;
                out vec2 TexCoord;

                uniform mat4 model;
                uniform mat4 view;
                uniform mat4 projection;

                void main()
                {
                    FragPos = vec3(model * vec4(aPosition, 1.0));
                    Normal = mat3(transpose(inverse(model))) * aNormal;
                    TexCoord = aTexCoord;
                    gl_Position = projection * view * model * vec4(aPosition, 1.0);
                }
            ";

            // Fragment shader
            string fragmentShaderSource = @"
                #version 330 core
                in vec3 FragPos;
                in vec3 Normal;
                in vec2 TexCoord;

                out vec4 FragColor;

                uniform vec3 lightPos;
                uniform vec3 lightColor;
                uniform vec3 viewPos;
                uniform vec3 objectColor;
                uniform bool isSun;

                void main()
                {
                    if (isSun)
                    {
                        FragColor = vec4(objectColor, 1.0);
                        return;
                    }

                    // Ambient
                    float ambientStrength = 0.1;
                    vec3 ambient = ambientStrength * lightColor;

                    // Diffuse
                    vec3 norm = normalize(Normal);
                    vec3 lightDir = normalize(lightPos - FragPos);
                    float diff = max(dot(norm, lightDir), 0.0);
                    vec3 diffuse = diff * lightColor;

                    // Specular
                    float specularStrength = 0.5;
                    vec3 viewDir = normalize(viewPos - FragPos);
                    vec3 reflectDir = reflect(-lightDir, norm);
                    float spec = pow(max(dot(viewDir, reflectDir), 0.0), 32);
                    vec3 specular = specularStrength * spec * lightColor;

                    vec3 result = (ambient + diffuse + specular) * objectColor;
                    FragColor = vec4(result, 1.0);
                }
            ";

            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexShaderSource);
            GL.CompileShader(vertexShader);

            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragmentShaderSource);
            GL.CompileShader(fragmentShader);

            int shaderProgram = GL.CreateProgram();
            GL.AttachShader(shaderProgram, vertexShader);
            GL.AttachShader(shaderProgram, fragmentShader);
            GL.LinkProgram(shaderProgram);

            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

            return shaderProgram;
        }

        private void CreateGroundMesh()
        {
            // Large grid with undulating terrain, with normals
            float size = 100f;
            int grid = 100;
            List<float> vertices = new List<float>();
            List<uint> indices = new List<uint>();
            for (int i = 0; i <= grid; i++)
            {
                for (int j = 0; j <= grid; j++)
                {
                    float x = -size + 2 * size * i / grid;
                    float z = -size + 2 * size * j / grid;
                    float y = GetTerrainHeight(x, z);
                    vertices.Add(x); vertices.Add(y); vertices.Add(z);
                    vertices.Add(0); vertices.Add(1); vertices.Add(0); // Upward normal
                    vertices.Add((float)i / grid); vertices.Add((float)j / grid); // Dummy texcoord
                }
            }
            for (int i = 0; i < grid; i++)
            {
                for (int j = 0; j < grid; j++)
                {
                    uint a = (uint)(i * (grid + 1) + j);
                    uint b = (uint)((i + 1) * (grid + 1) + j);
                    uint c = (uint)((i + 1) * (grid + 1) + (j + 1));
                    uint d = (uint)(i * (grid + 1) + (j + 1));
                    indices.Add(a); indices.Add(b); indices.Add(c);
                    indices.Add(a); indices.Add(c); indices.Add(d);
                }
            }
            _groundVAO = GL.GenVertexArray();
            GL.BindVertexArray(_groundVAO);
            int vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * sizeof(float), vertices.ToArray(), BufferUsageHint.StaticDraw);
            int ebo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Count * sizeof(uint), indices.ToArray(), BufferUsageHint.StaticDraw);
            int stride = 8 * sizeof(float);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, 6 * sizeof(float));
            GL.EnableVertexAttribArray(2);
            _groundIndexCount = indices.Count;
        }

        private void CreateTrackMesh()
        {
            Console.WriteLine("Renderer.CreateTrackMesh: Starting tube track mesh creation");

            // Tube parameters
            const float tubeRadius = 0.2f;
            const int tubeSegments = 32; // around
            const int pathSegments = 200; // along

            // Get points along the track (sampled evenly in t)
            var track = new RollerCoasterTrack();
            List<Vector3> points = new List<Vector3>();
            for (int i = 0; i < pathSegments; i++)
            {
                float t = (float)i / pathSegments;
                points.Add(track.GetPosition(t));
            }
            int n = points.Count;
            Console.WriteLine($"Track mesh: {n} path points, {tubeSegments} tube segments");
            if (n < 2) return;

            List<float> vertices = new List<float>();
            List<uint> indices = new List<uint>();

            // For each point, compute tangent and normal
            for (int i = 0; i < n; i++)
            {
                Vector3 p = points[i];
                Vector3 tangent = (i < n - 1) ? Vector3.Normalize(points[(i + 1) % n] - p) : Vector3.Normalize(p - points[i - 1]);
                Vector3 up = Vector3.UnitY;
                Vector3 normal = Vector3.Normalize(Vector3.Cross(tangent, up));
                Vector3 binormal = Vector3.Normalize(Vector3.Cross(tangent, normal));

                for (int j = 0; j < tubeSegments; j++)
                {
                    float theta = (float)j / tubeSegments * MathHelper.TwoPi;
                    float cos = (float)Math.Cos(theta);
                    float sin = (float)Math.Sin(theta);
                    Vector3 offset = normal * cos * tubeRadius + binormal * sin * tubeRadius;
                    Vector3 pos = p + offset;
                    Vector3 norm = Vector3.Normalize(offset);
                    // Position (3), Normal (3), TexCoord (2)
                    vertices.Add(pos.X); vertices.Add(pos.Y); vertices.Add(pos.Z);
                    vertices.Add(norm.X); vertices.Add(norm.Y); vertices.Add(norm.Z);
                    vertices.Add((float)j / tubeSegments); vertices.Add((float)i / n);
                }
            }

            // Create indices (closed loop)
            for (int i = 0; i < n; i++)
            {
                int nextI = (i + 1) % n;
                for (int j = 0; j < tubeSegments; j++)
                {
                    int nextJ = (j + 1) % tubeSegments;
                    uint a = (uint)(i * tubeSegments + j);
                    uint b = (uint)(nextI * tubeSegments + j);
                    uint c = (uint)(nextI * tubeSegments + nextJ);
                    uint d = (uint)(i * tubeSegments + nextJ);
                    // Two triangles per quad
                    indices.Add(a); indices.Add(b); indices.Add(c);
                    indices.Add(a); indices.Add(c); indices.Add(d);
                }
            }

            Console.WriteLine($"Track mesh: {vertices.Count / 8} vertices, {indices.Count / 3} triangles");

            _trackVAO = GL.GenVertexArray();
            GL.BindVertexArray(_trackVAO);

            int vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * sizeof(float), vertices.ToArray(), BufferUsageHint.StaticDraw);

            int ebo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Count * sizeof(uint), indices.ToArray(), BufferUsageHint.StaticDraw);

            int stride = 8 * sizeof(float);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, 6 * sizeof(float));
            GL.EnableVertexAttribArray(2);

            _trackIndexCount = indices.Count;
            Console.WriteLine("Renderer.CreateTrackMesh: Tube track mesh creation complete");
        }

        private void CreateTrainMesh()
        {
            Console.WriteLine("Renderer.CreateTrainMesh: Starting train mesh creation");
            
            // Create a simple train mesh
            const float carLength = 2.0f;
            const float carWidth = 1.0f;
            const float carHeight = 0.8f;
            const float wheelRadius = 0.2f;

            List<float> vertices = new List<float>();
            List<uint> indices = new List<uint>();

            // Car body
            vertices.AddRange(new float[] {
                // Front face
                0, carHeight, -carWidth/2, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f,
                carLength, carHeight, -carWidth/2, 0.0f, 0.0f, 1.0f, 1.0f, 0.0f,
                carLength, 0, -carWidth/2, 0.0f, 0.0f, 1.0f, 1.0f, 1.0f,
                0, 0, -carWidth/2, 0.0f, 0.0f, 1.0f, 0.0f, 1.0f,

                // Back face
                0, carHeight, carWidth/2, 0.0f, 0.0f, -1.0f, 0.0f, 0.0f,
                carLength, carHeight, carWidth/2, 0.0f, 0.0f, -1.0f, 1.0f, 0.0f,
                carLength, 0, carWidth/2, 0.0f, 0.0f, -1.0f, 1.0f, 1.0f,
                0, 0, carWidth/2, 0.0f, 0.0f, -1.0f, 0.0f, 1.0f,

                // Top face
                0, carHeight, -carWidth/2, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f,
                carLength, carHeight, -carWidth/2, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f,
                carLength, carHeight, carWidth/2, 0.0f, 1.0f, 0.0f, 1.0f, 1.0f,
                0, carHeight, carWidth/2, 0.0f, 1.0f, 0.0f, 0.0f, 1.0f
            });

            // Generate indices for all faces
            for (int i = 0; i < 3; i++) // 3 faces
            {
                int baseIndex = i * 4;
                indices.AddRange(new uint[] {
                    (uint)baseIndex, (uint)(baseIndex + 1), (uint)(baseIndex + 2),
                    (uint)baseIndex, (uint)(baseIndex + 2), (uint)(baseIndex + 3)
                });
            }

            _trainVAO = GL.GenVertexArray();
            GL.BindVertexArray(_trainVAO);

            int vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * sizeof(float), vertices.ToArray(), BufferUsageHint.StaticDraw);

            int ebo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Count * sizeof(uint), indices.ToArray(), BufferUsageHint.StaticDraw);

            int stride = 8 * sizeof(float);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, 6 * sizeof(float));
            GL.EnableVertexAttribArray(2);

            Console.WriteLine("Renderer.CreateTrainMesh: Train mesh creation complete");
        }

        public void Draw(Matrix4 view, Matrix4 projection, RollerCoasterSim.Train.Train train)
        {
            try
            {
                GL.UseProgram(_shaderProgram);

                // Set view and projection matrices
                GL.UniformMatrix4(GL.GetUniformLocation(_shaderProgram, "view"), false, ref view);
                GL.UniformMatrix4(GL.GetUniformLocation(_shaderProgram, "projection"), false, ref projection);

                // At the start of Draw(), before any drawing:
                GL.Uniform1(GL.GetUniformLocation(_shaderProgram, "isSun"), 0);

                // Draw ground
                DrawGround();

                // Draw track
                DrawTrack();

                // Draw train
                DrawTrain(train);

                // Draw supports
                DrawSupports();

                // Draw trees
                DrawTrees();

                // Draw platform
                DrawPlatform();

                // Draw fence
                DrawFence();

                // Draw station
                DrawStation();

                // Draw sky
                DrawSky();

                // Draw sun
                DrawSun();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in Renderer.Draw: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private void DrawGround()
        {
            GL.BindVertexArray(_groundVAO);
            Matrix4 groundModel = Matrix4.Identity;
            GL.UniformMatrix4(GL.GetUniformLocation(_shaderProgram, "model"), false, ref groundModel);
            GL.Uniform3(GL.GetUniformLocation(_shaderProgram, "objectColor"), new Vector3(0.6f, 0.3f, 0.1f)); // Brown color
            GL.DrawElements(PrimitiveType.Triangles, _groundIndexCount, DrawElementsType.UnsignedInt, 0);
        }

        private void DrawTrack()
        {
            GL.BindVertexArray(_trackVAO);
            // Raise the track above the terrain
            Matrix4 trackModel = Matrix4.CreateTranslation(0, 1.5f, 0);
            GL.UniformMatrix4(GL.GetUniformLocation(_shaderProgram, "model"), false, ref trackModel);
            GL.Uniform3(GL.GetUniformLocation(_shaderProgram, "objectColor"), new Vector3(0.7f, 0.7f, 0.7f)); // Gray color
            GL.DrawElements(PrimitiveType.Triangles, _trackIndexCount, DrawElementsType.UnsignedInt, 0);

            // Draw shadow based on sun position
            Vector3 sunPos = new Vector3(50, 50, 50);
            Vector3 sunDir = Vector3.Normalize(sunPos);
            Matrix4 shadowModel = GetShadowMatrix(sunDir) * Matrix4.CreateTranslation(0, 1.5f, 0);
            GL.UniformMatrix4(GL.GetUniformLocation(_shaderProgram, "model"), false, ref shadowModel);
            GL.Uniform3(GL.GetUniformLocation(_shaderProgram, "objectColor"), new Vector3(0.1f, 0.1f, 0.1f)); // Dark shadow
            GL.DrawElements(PrimitiveType.Triangles, _trackIndexCount, DrawElementsType.UnsignedInt, 0);
        }

        private void DrawTrain(RollerCoasterSim.Train.Train train)
        {
            // Implementation of DrawTrain method
        }

        private void DrawSupports()
        {
            // Implementation of DrawSupports method
        }

        private void DrawTrees()
        {
            for (int i = 0; i < _treePositions.Count; i++)
            {
                Vector3 pos = _treePositions[i];
                // Draw trunk
                GL.BindVertexArray(_treeTrunkVAO);
                Matrix4 model = Matrix4.CreateTranslation(pos);
                GL.UniformMatrix4(GL.GetUniformLocation(_shaderProgram, "model"), false, ref model);
                GL.Uniform3(GL.GetUniformLocation(_shaderProgram, "objectColor"), new Vector3(0.5f, 0.25f, 0.1f)); // Brown
                GL.DrawElements(PrimitiveType.Triangles, _treeTrunkIndexCount, DrawElementsType.UnsignedInt, 0);
                // Draw leaves
                GL.BindVertexArray(_treeLeavesVAO);
                GL.UniformMatrix4(GL.GetUniformLocation(_shaderProgram, "model"), false, ref model);
                GL.Uniform3(GL.GetUniformLocation(_shaderProgram, "objectColor"), new Vector3(0.1f, 0.5f, 0.1f)); // Green
                GL.DrawElements(PrimitiveType.Triangles, _treeLeavesIndexCount, DrawElementsType.UnsignedInt, 0);

                // Draw shadow based on sun position
                Vector3 sunPos = new Vector3(50, 50, 50);
                Vector3 sunDir = Vector3.Normalize(sunPos);
                Vector3 shadowPos = new Vector3(pos.X, pos.Y, pos.Z);
                Matrix4 shadowModel = GetShadowMatrix(sunDir) * Matrix4.CreateTranslation(shadowPos);
                GL.UniformMatrix4(GL.GetUniformLocation(_shaderProgram, "model"), false, ref shadowModel);
                GL.Uniform3(GL.GetUniformLocation(_shaderProgram, "objectColor"), new Vector3(0.1f, 0.1f, 0.1f)); // Dark shadow
                GL.DrawElements(PrimitiveType.Triangles, _treeTrunkIndexCount, DrawElementsType.UnsignedInt, 0);
            }
        }

        private void DrawPlatform()
        {
            // Implementation of DrawPlatform method
        }

        private void DrawFence()
        {
            // Implementation of DrawFence method
        }

        private void DrawStation()
        {
            // Implementation of DrawStation method
        }

        private void DrawSky()
        {
            // Implementation of DrawSky method
        }

        private void DrawSun()
        {
            GL.Uniform1(GL.GetUniformLocation(_shaderProgram, "isSun"), 1);
            GL.BindVertexArray(_sunVAO);
            Matrix4 sunModel = Matrix4.CreateTranslation(50, 50, 50);
            // Draw glow (bigger, semi-transparent)
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            Matrix4 glowModel = Matrix4.CreateScale(1.7f) * sunModel;
            GL.UniformMatrix4(GL.GetUniformLocation(_shaderProgram, "model"), false, ref glowModel);
            GL.Uniform3(GL.GetUniformLocation(_shaderProgram, "objectColor"), new Vector3(1.0f, 0.95f, 0.3f));
            GL.DrawElements(PrimitiveType.Triangles, _sunIndexCount, DrawElementsType.UnsignedInt, 0);
            GL.Disable(EnableCap.Blend);
            // Draw sun (solid)
            GL.UniformMatrix4(GL.GetUniformLocation(_shaderProgram, "model"), false, ref sunModel);
            GL.Uniform3(GL.GetUniformLocation(_shaderProgram, "objectColor"), new Vector3(1.0f, 0.9f, 0.0f));
            GL.DrawElements(PrimitiveType.Triangles, _sunIndexCount, DrawElementsType.UnsignedInt, 0);
            GL.Uniform1(GL.GetUniformLocation(_shaderProgram, "isSun"), 0);
        }

        private void GenerateTreePositions(int count = 100)
        {
            var rand = new System.Random(42);
            _treePositions.Clear();
            for (int i = 0; i < count; i++)
            {
                float x = (float)(rand.NextDouble() * 180 - 90);
                float z = (float)(rand.NextDouble() * 180 - 90);
                // Avoid placing trees too close to the track center
                if (MathF.Abs(x) < 10 && MathF.Abs(z) < 10) { i--; continue; }
                float y = GetTerrainHeight(x, z);
                _treePositions.Add(new Vector3(x, y, z));
            }
        }

        private void CreateTreeMesh()
        {
            // Trunk: simple cylinder
            const int segments = 12;
            const float trunkHeight = 2.0f;
            const float trunkRadius = 0.2f;
            List<float> trunkVertices = new List<float>();
            List<uint> trunkIndices = new List<uint>();
            for (int i = 0; i <= segments; i++)
            {
                float theta = i * MathF.PI * 2 / segments;
                float x = MathF.Cos(theta) * trunkRadius;
                float z = MathF.Sin(theta) * trunkRadius;
                trunkVertices.Add(x); trunkVertices.Add(0); trunkVertices.Add(z);
                trunkVertices.Add(0.5f); trunkVertices.Add(0.25f); trunkVertices.Add(0.1f); // normal (approx)
                trunkVertices.Add(0); trunkVertices.Add(0);
                trunkVertices.Add(x); trunkVertices.Add(trunkHeight); trunkVertices.Add(z);
                trunkVertices.Add(0.5f); trunkVertices.Add(0.25f); trunkVertices.Add(0.1f);
                trunkVertices.Add(0); trunkVertices.Add(1);
            }
            for (int i = 0; i < segments; i++)
            {
                uint baseIdx = (uint)(i * 2);
                trunkIndices.Add(baseIdx); trunkIndices.Add(baseIdx + 1); trunkIndices.Add(baseIdx + 2);
                trunkIndices.Add(baseIdx + 1); trunkIndices.Add(baseIdx + 3); trunkIndices.Add(baseIdx + 2);
            }
            _treeTrunkVAO = GL.GenVertexArray();
            _treeTrunkVBO = GL.GenBuffer();
            GL.BindVertexArray(_treeTrunkVAO);
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

            // Leaves: simple cone
            List<float> leavesVertices = new List<float>();
            List<uint> leavesIndices = new List<uint>();
            float leavesHeight = 2.5f;
            float leavesRadius = 1.0f;
            // Tip of cone
            leavesVertices.Add(0); leavesVertices.Add(trunkHeight + leavesHeight); leavesVertices.Add(0);
            leavesVertices.Add(0.1f); leavesVertices.Add(0.5f); leavesVertices.Add(0.1f);
            leavesVertices.Add(0.5f); leavesVertices.Add(1.0f);
            for (int i = 0; i <= segments; i++)
            {
                float theta = i * MathF.PI * 2 / segments;
                float x = MathF.Cos(theta) * leavesRadius;
                float z = MathF.Sin(theta) * leavesRadius;
                leavesVertices.Add(x); leavesVertices.Add(trunkHeight); leavesVertices.Add(z);
                leavesVertices.Add(0.1f); leavesVertices.Add(0.5f); leavesVertices.Add(0.1f);
                leavesVertices.Add((float)i / segments); leavesVertices.Add(0);
            }
            for (int i = 1; i <= segments; i++)
            {
                leavesIndices.Add(0); leavesIndices.Add((uint)i); leavesIndices.Add((uint)(i + 1));
            }
            _treeLeavesVAO = GL.GenVertexArray();
            _treeLeavesVBO = GL.GenBuffer();
            GL.BindVertexArray(_treeLeavesVAO);
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

        private void CreateSunMesh()
        {
            const int segments = 32;
            const float radius = 5.0f;
            List<float> vertices = new List<float>();
            List<uint> indices = new List<uint>();
            for (int i = 0; i <= segments; i++)
            {
                float theta = i * MathF.PI * 2 / segments;
                for (int j = 0; j <= segments; j++)
                {
                    float phi = j * MathF.PI / segments;
                    float x = radius * MathF.Sin(phi) * MathF.Cos(theta);
                    float y = radius * MathF.Cos(phi);
                    float z = radius * MathF.Sin(phi) * MathF.Sin(theta);
                    vertices.Add(x); vertices.Add(y); vertices.Add(z);
                    vertices.Add(x / radius); vertices.Add(y / radius); vertices.Add(z / radius); // Normal
                    vertices.Add((float)i / segments); vertices.Add((float)j / segments); // TexCoord
                }
            }
            for (int i = 0; i < segments; i++)
            {
                for (int j = 0; j < segments; j++)
                {
                    uint a = (uint)(i * (segments + 1) + j);
                    uint b = (uint)((i + 1) * (segments + 1) + j);
                    uint c = (uint)((i + 1) * (segments + 1) + (j + 1));
                    uint d = (uint)(i * (segments + 1) + (j + 1));
                    indices.Add(a); indices.Add(b); indices.Add(c);
                    indices.Add(a); indices.Add(c); indices.Add(d);
                }
            }
            _sunVAO = GL.GenVertexArray();
            GL.BindVertexArray(_sunVAO);
            int vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * sizeof(float), vertices.ToArray(), BufferUsageHint.StaticDraw);
            int ebo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Count * sizeof(uint), indices.ToArray(), BufferUsageHint.StaticDraw);
            int stride = 8 * sizeof(float);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, 6 * sizeof(float));
            GL.EnableVertexAttribArray(2);
            _sunIndexCount = indices.Count;
        }

        private float GetTerrainHeight(float x, float z)
        {
            // Simple undulating terrain using sine/cosine
            return -1.0f + 2.0f * MathF.Sin(0.05f * x) * MathF.Cos(0.05f * z);
        }

        // Utility function for shadow projection matrix
        private Matrix4 GetShadowMatrix(Vector3 sunDir)
        {
            // Project onto y=0 plane along sunDir
            float dx = sunDir.X;
            float dy = sunDir.Y;
            float dz = sunDir.Z;
            // Avoid division by zero
            if (MathF.Abs(dy) < 1e-4f) dy = 1e-4f;
            return new Matrix4(
                1,   -dx/dy, 0, 0,
                0,     0,    0, 0,
                0,   -dz/dy, 1, 0,
                0,     0.01f, 0, 1 // Offset slightly above ground
            );
        }
    }
}
