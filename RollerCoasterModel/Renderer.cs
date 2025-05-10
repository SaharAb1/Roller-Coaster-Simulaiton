using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace RollerCoasterSim
{
    public static class Renderer
    {
        private static int _shader;
        private static int _trackVAO;
        private static int _trainVAO;
        private static int _trackIndexCount;
        private static int _groundVAO;
        private static int _groundIndexCount;
        private static Vector3 _lightPos = new Vector3(10f, 10f, 10f);
        private static Vector3 _lightColor = new Vector3(1f, 1f, 1f);

        public static void Init()
        {
            Console.WriteLine("Renderer.Init: Starting initialization");
            
            // Load shaders
            _shader = LoadShaders("Shaders/shader.vert", "Shaders/shader.frag");
            if (_shader == -1)
            {
                Console.WriteLine("Renderer.Init: Failed to load shaders");
                return;
            }

            CreateGroundMesh();
            CreateTrackMesh();
            CreateTrainMesh();

            Console.WriteLine("Renderer.Init: Initialization complete");
        }

        private static void CreateGroundMesh()
        {
            // Large quad at y = -1
            float size = 100f;
            List<float> vertices = new List<float>
            {
                -size, -1f, -size,
                 size, -1f, -size,
                 size, -1f,  size,
                -size, -1f,  size
            };
            List<uint> indices = new List<uint> { 0, 1, 2, 0, 2, 3 };
            _groundVAO = GL.GenVertexArray();
            GL.BindVertexArray(_groundVAO);
            int vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * sizeof(float), vertices.ToArray(), BufferUsageHint.StaticDraw);
            int ebo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Count * sizeof(uint), indices.ToArray(), BufferUsageHint.StaticDraw);
            int stride = 3 * sizeof(float);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
            GL.EnableVertexAttribArray(0);
            // Do NOT enable or set up attributes 1 and 2 for the ground
            _groundIndexCount = indices.Count;
        }

        private static void CreateTrackMesh()
        {
            Console.WriteLine("Renderer.CreateTrackMesh: Starting tube track mesh creation");

            // Tube parameters
            const float tubeRadius = 0.2f;
            const int tubeSegments = 32; // around
            const int pathSegments = 200; // along

            // Get points along the track (sampled evenly in t)
            var track = new Track.RollerCoasterTrack();
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

        private static void CreateTrainMesh()
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

        private static int LoadShaders(string vertexPath, string fragmentPath)
        {
            Console.WriteLine($"Loading shaders: {vertexPath}, {fragmentPath}");
            
            // Read shader source
            string vertexShaderSource = File.ReadAllText(vertexPath);
            string fragmentShaderSource = File.ReadAllText(fragmentPath);

            // Create and compile vertex shader
            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexShaderSource);
            GL.CompileShader(vertexShader);

            // Check for vertex shader compilation errors
            GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out int success);
            if (success == 0)
            {
                string infoLog = GL.GetShaderInfoLog(vertexShader);
                Console.WriteLine($"Vertex shader compilation error: {infoLog}");
                return -1;
            }

            // Create and compile fragment shader
            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragmentShaderSource);
            GL.CompileShader(fragmentShader);

            // Check for fragment shader compilation errors
            GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out success);
            if (success == 0)
            {
                string infoLog = GL.GetShaderInfoLog(fragmentShader);
                Console.WriteLine($"Fragment shader compilation error: {infoLog}");
                return -1;
            }

            // Create shader program
            int program = GL.CreateProgram();
            GL.AttachShader(program, vertexShader);
            GL.AttachShader(program, fragmentShader);
            GL.LinkProgram(program);

            // Check for linking errors
            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out success);
            if (success == 0)
            {
                string infoLog = GL.GetProgramInfoLog(program);
                Console.WriteLine($"Shader program linking error: {infoLog}");
                return -1;
            }

            // Clean up
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

            Console.WriteLine("Shader program created successfully");
            return program;
        }

        public static void Draw(Matrix4 view, Matrix4 projection, Train train)
        {
            if (_shader == -1)
            {
                Console.WriteLine("Renderer.Draw: Shader not initialized");
                return;
            }
            GL.UseProgram(_shader);
            int viewLoc = GL.GetUniformLocation(_shader, "view");
            int projLoc = GL.GetUniformLocation(_shader, "projection");
            int lightPosLoc = GL.GetUniformLocation(_shader, "lightPos");
            int lightColorLoc = GL.GetUniformLocation(_shader, "lightColor");
            int viewPosLoc = GL.GetUniformLocation(_shader, "viewPos");
            int objectColorLoc = GL.GetUniformLocation(_shader, "objectColor");
            if (viewLoc == -1 || projLoc == -1 || lightPosLoc == -1 || lightColorLoc == -1 || viewPosLoc == -1 || objectColorLoc == -1)
            {
                Console.WriteLine("Renderer.Draw: Failed to get uniform locations");
                return;
            }
            GL.UniformMatrix4(viewLoc, false, ref view);
            GL.UniformMatrix4(projLoc, false, ref projection);
            GL.Uniform3(lightPosLoc, _lightPos);
            GL.Uniform3(lightColorLoc, _lightColor);
            Vector3 viewPos = new Vector3(view.M14, view.M24, view.M34);
            GL.Uniform3(viewPosLoc, viewPos);
            // Draw ground
            GL.BindVertexArray(_groundVAO);
            Matrix4 groundModel = Matrix4.Identity;
            int modelLoc = GL.GetUniformLocation(_shader, "model");
            GL.UniformMatrix4(modelLoc, false, ref groundModel);
            GL.Uniform3(objectColorLoc, new Vector3(0.2f, 0.6f, 0.2f)); // green
            GL.DrawElements(PrimitiveType.Triangles, _groundIndexCount, DrawElementsType.UnsignedInt, 0);
            // Draw track
            GL.BindVertexArray(_trackVAO);
            Matrix4 trackModel = Matrix4.Identity;
            GL.UniformMatrix4(modelLoc, false, ref trackModel);
            GL.Uniform3(objectColorLoc, new Vector3(0.7f, 0.7f, 0.7f));
            GL.DrawElements(PrimitiveType.Triangles, _trackIndexCount, DrawElementsType.UnsignedInt, 0);
            // Draw train
            Vector3 position = train.GetPosition();
            Vector3 direction = train.GetDirection();
            Vector3 normal = train.GetNormal();
            Vector3 right = Vector3.Normalize(Vector3.Cross(normal, direction));
            Matrix4 rotation = new Matrix4(
                new Vector4(right, 0),
                new Vector4(normal, 0),
                new Vector4(-direction, 0),
                new Vector4(0, 0, 0, 1)
            );
            Matrix4 trainModel = rotation * Matrix4.CreateTranslation(position) * Matrix4.CreateScale(0.5f);
            GL.UniformMatrix4(modelLoc, false, ref trainModel);
            GL.Uniform3(objectColorLoc, new Vector3(1.0f, 0.0f, 0.0f));
            GL.DrawElements(PrimitiveType.Triangles, 18, DrawElementsType.UnsignedInt, 0);
        }
    }
}
