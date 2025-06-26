using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using RollerCoasterSim.Track;
using RollerCoasterSim.Train;
using RollerCoasterSim.Objects;

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
        private int _skyVBO;
        private int _skyEBO;
        private int _skyIndexCount;
        private List<Vector3> _treePositions = new List<Vector3>();
        private int _treeTrunkVAO;
        private int _treeTrunkVBO;
        private int _treeLeavesVAO;
        private int _treeLeavesVBO;
        private int _treeTrunkIndexCount;
        private int _treeLeavesIndexCount;
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
        private int _groundTexture;
        private int _rockVAO;
        private int _rockVBO;
        private int _rockEBO;
        private int _rockIndexCount;
        private List<(Vector3 position, float scale, float rotation, Vector3 color)> _rockPositions = new List<(Vector3, float, float, Vector3)>();
        private int _leafVAO;
        private int _leafVBO;
        private int _leafEBO;
        private int _leafIndexCount;
        private List<(Vector3 position, float rotation, float scale, float alpha)> _leafParticles = new List<(Vector3, float, float, float)>();
        private float _leafTime = 0;
        private int _trackTexture;
        private int _waterVAO;
        private int _waterVBO;
        private int _waterEBO;
        private int _waterIndexCount;
        private float _waterTime = 0;
        public TreeObject Trees { get; private set; } = new TreeObject();

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
                CreateSkyMesh();
                GenerateRockPositions();
                CreateRockMesh();
                CreateLeafMesh();
                GenerateLeafParticles(100);
                CreateWaterMesh();

                // Set up enhanced lighting
                GL.UseProgram(_shaderProgram);
                // Position the sun higher and more to the side for stronger shadows
                GL.Uniform3(GL.GetUniformLocation(_shaderProgram, "lightPos"), new Vector3(100, 150, 100));
                // Increase light intensity for stronger contrast
                GL.Uniform3(GL.GetUniformLocation(_shaderProgram, "lightColor"), new Vector3(1.5f, 1.5f, 1.5f));
                // Reduce ambient light for stronger shadows
                GL.Uniform1(GL.GetUniformLocation(_shaderProgram, "ambientStrength"), 0.15f);
                // Increase specular highlights for more contrast
                GL.Uniform1(GL.GetUniformLocation(_shaderProgram, "specularStrength"), 1.2f);

                // Enable depth testing
                GL.Enable(EnableCap.DepthTest);
                GL.DepthFunc(DepthFunction.Less);

                // Create track texture
                _trackTexture = CreateTrackTexture();

                Trees.GeneratePositions(GetTerrainHeight);
                Trees.CreateMesh();

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
                out float SkyGradient;
                out vec3 ReflectionCoord;

                uniform mat4 model;
                uniform mat4 view;
                uniform mat4 projection;
                uniform bool isWater;

                void main()
                {
                    FragPos = vec3(model * vec4(aPosition, 1.0));
                    Normal = mat3(transpose(inverse(model))) * aNormal;
                    TexCoord = aTexCoord;
                    SkyGradient = (aPosition.y + 1.0) * 0.5;
                    
                    // Calculate reflection coordinates for water
                    if (isWater) {
                        vec3 viewDir = normalize(FragPos - vec3(0.0, 0.0, 0.0));
                        ReflectionCoord = reflect(viewDir, Normal);
                    }
                    
                    gl_Position = projection * view * model * vec4(aPosition, 1.0);
                }
            ";

            // Enhanced fragment shader with water reflections
            string fragmentShaderSource = @"
                #version 330 core
                in vec3 FragPos;
                in vec3 Normal;
                in vec2 TexCoord;
                in float SkyGradient;
                in vec3 ReflectionCoord;

                out vec4 FragColor;

                uniform vec3 lightPos;
                uniform vec3 lightColor;
                uniform vec3 viewPos;
                uniform vec3 objectColor;
                uniform bool isSky;
                uniform bool isSun;
                uniform bool isWater;
                uniform float ambientStrength;
                uniform float specularStrength;
                uniform sampler2D texture0;
                uniform bool useTexture;
                uniform float alpha = 1.0;
                uniform float time;

                // Function to simulate atmospheric scattering
                vec3 calculateSkyColor(float height, vec3 viewDir)
                {
                    // Zenith color (top of sky)
                    vec3 zenithColor = vec3(0.4, 0.6, 0.8);
                    
                    // Horizon color (bottom of sky)
                    vec3 horizonColor = vec3(0.7, 0.8, 0.9);
                    
                    // Sun position influence
                    vec3 sunDir = normalize(vec3(1.0, 1.0, 1.0));
                    float sunInfluence = pow(max(dot(viewDir, sunDir), 0.0), 32.0);
                    
                    // Calculate base gradient
                    vec3 baseColor = mix(horizonColor, zenithColor, height);
                    
                    // Add sun glow
                    vec3 sunGlow = vec3(1.0, 0.9, 0.7) * sunInfluence * 0.5;
                    
                    // Add atmospheric scattering effect
                    float scatter = pow(1.0 - height, 2.0) * 0.5;
                    vec3 scatterColor = vec3(0.8, 0.9, 1.0) * scatter;
                    
                    // Combine all effects
                    return baseColor + sunGlow + scatterColor;
                }

                void main()
                {
                    if (isSky)
                    {
                        vec3 viewDir = normalize(FragPos);
                        vec3 skyColor = calculateSkyColor(SkyGradient, viewDir);
                        
                        // Add subtle noise for more natural look
                        float noise = fract(sin(dot(TexCoord, vec2(12.9898, 78.233))) * 43758.5453);
                        skyColor += vec3(noise * 0.02);
                        
                        FragColor = vec4(skyColor, 1.0);
                        return;
                    }

                    if (isSun)
                    {
                        FragColor = vec4(objectColor, 1.0);
                        return;
                    }

                    if (isWater)
                    {
                        // Calculate water color with reflection
                        vec3 viewDir = normalize(viewPos - FragPos);
                        vec3 reflectDir = reflect(-viewDir, Normal);
                        
                        // Add some wave movement
                        float wave = sin(FragPos.x * 0.1 + time) * 0.5 + 0.5;
                        wave *= sin(FragPos.z * 0.1 + time * 0.8) * 0.5 + 0.5;
                        
                        // Mix base water color with reflection
                        vec3 waterColor = mix(objectColor, vec3(0.8, 0.9, 1.0), wave * 0.3);
                        
                        // Add specular highlight
                        vec3 lightDir = normalize(lightPos - FragPos);
                        float spec = pow(max(dot(reflectDir, lightDir), 0.0), 32.0);
                        vec3 specular = specularStrength * spec * lightColor;
                        
                        // Combine colors
                        vec3 finalColor = waterColor + specular;
                        
                        FragColor = vec4(finalColor, alpha);
                        return;
                    }

                    // Regular object rendering
                    vec3 ambient = ambientStrength * lightColor;
                    vec3 norm = normalize(Normal);
                    vec3 lightDir = normalize(lightPos - FragPos);
                    float diff = max(dot(norm, lightDir), 0.0);
                    vec3 diffuse = diff * lightColor * 1.5;

                    vec3 viewDir = normalize(viewPos - FragPos);
                    vec3 reflectDir = reflect(-lightDir, norm);
                    float spec = pow(max(dot(viewDir, reflectDir), 0.0), 128.0);
                    vec3 specular = specularStrength * spec * lightColor;

                    float distance = length(lightPos - FragPos);
                    float attenuation = 1.0 / (1.0 + 0.14 * distance + 0.07 * distance * distance);

                    vec3 result = (ambient + (diffuse + specular) * attenuation) * objectColor;
                    
                    if (useTexture)
                    {
                        vec4 texColor = texture(texture0, TexCoord);
                        result *= texColor.rgb;
                    }
                    
                    result = pow(result, vec3(1.0/2.4));
                    
                    FragColor = vec4(result, alpha);
                }
            ";

            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexShaderSource);
            GL.CompileShader(vertexShader);

            // Check for vertex shader compilation errors
            GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out int success);
            if (success == 0)
            {
                string infoLog = GL.GetShaderInfoLog(vertexShader);
                Console.WriteLine($"ERROR::SHADER::VERTEX::COMPILATION_FAILED\n{infoLog}");
            }

            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragmentShaderSource);
            GL.CompileShader(fragmentShader);

            // Check for fragment shader compilation errors
            GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out success);
            if (success == 0)
            {
                string infoLog = GL.GetShaderInfoLog(fragmentShader);
                Console.WriteLine($"ERROR::SHADER::FRAGMENT::COMPILATION_FAILED\n{infoLog}");
            }

            int shaderProgram = GL.CreateProgram();
            GL.AttachShader(shaderProgram, vertexShader);
            GL.AttachShader(shaderProgram, fragmentShader);
            GL.LinkProgram(shaderProgram);

            // Check for linking errors
            GL.GetProgram(shaderProgram, GetProgramParameterName.LinkStatus, out success);
            if (success == 0)
            {
                string infoLog = GL.GetProgramInfoLog(shaderProgram);
                Console.WriteLine($"ERROR::SHADER::PROGRAM::LINKING_FAILED\n{infoLog}");
            }

            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

            return shaderProgram;
        }

        private void CreateGroundMesh()
        {
            // Large grid with undulating terrain and texture coordinates
            float size = 100f;
            int grid = 100;
            List<float> vertices = new List<float>();
            List<uint> indices = new List<uint>();

            // Create ground texture
            int groundTexture = CreateGroundTexture();

            for (int i = 0; i <= grid; i++)
            {
                for (int j = 0; j <= grid; j++)
                {
                    float x = -size + 2 * size * i / grid;
                    float z = -size + 2 * size * j / grid;
                    float y = GetTerrainHeight(x, z);
                    
                    // Position
                    vertices.Add(x); vertices.Add(y); vertices.Add(z);
                    
                    // Normal (calculate based on terrain)
                    Vector3 normal = CalculateTerrainNormal(x, z);
                    vertices.Add(normal.X); vertices.Add(normal.Y); vertices.Add(normal.Z);
                    
                    // Texture coordinates (repeat texture multiple times)
                    vertices.Add((float)i / 10); vertices.Add((float)j / 10);
                }
            }

            // Generate indices
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

            // Store texture ID
            _groundTexture = groundTexture;
        }

        private int CreateGroundTexture()
        {
            const int textureSize = 512;
            byte[] textureData = new byte[textureSize * textureSize * 4]; // RGBA

            // Create a procedural grass/dirt texture
            Random random = new Random(42); // Fixed seed for consistency
            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    int index = (y * textureSize + x) * 4;
                    
                    // Base grass color
                    float r = 0.4f + (float)random.NextDouble() * 0.2f; // Green variations
                    float g = 0.6f + (float)random.NextDouble() * 0.2f; // Green variations
                    float b = 0.2f + (float)random.NextDouble() * 0.1f; // Dark green/blue

                    // Add some dirt patches
                    if (random.NextDouble() < 0.1f) // 10% chance of dirt
                    {
                        r = 0.6f + (float)random.NextDouble() * 0.2f; // Brown
                        g = 0.4f + (float)random.NextDouble() * 0.2f; // Brown
                        b = 0.2f + (float)random.NextDouble() * 0.1f; // Dark brown
                    }

                    textureData[index] = (byte)(r * 255);
                    textureData[index + 1] = (byte)(g * 255);
                    textureData[index + 2] = (byte)(b * 255);
                    textureData[index + 3] = 255; // Alpha
                }
            }

            int texture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, texture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, textureSize, textureSize, 0, PixelFormat.Rgba, PixelType.UnsignedByte, textureData);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            return texture;
        }

        private Vector3 CalculateTerrainNormal(float x, float z)
        {
            float epsilon = 0.1f;
            float hL = GetTerrainHeight(x - epsilon, z);
            float hR = GetTerrainHeight(x + epsilon, z);
            float hD = GetTerrainHeight(x, z - epsilon);
            float hU = GetTerrainHeight(x, z + epsilon);

            Vector3 normal = new Vector3(
                hL - hR,
                2.0f * epsilon,
                hD - hU
            );
            return Vector3.Normalize(normal);
        }

        private void DrawGround()
        {
            GL.BindVertexArray(_groundVAO);
            Matrix4 groundModel = Matrix4.Identity;
            GL.UniformMatrix4(GL.GetUniformLocation(_shaderProgram, "model"), false, ref groundModel);
            
            // Enable texture
            GL.Uniform1(GL.GetUniformLocation(_shaderProgram, "useTexture"), 1);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _groundTexture);
            GL.Uniform1(GL.GetUniformLocation(_shaderProgram, "texture0"), 0);
            
            // Set base color (will be multiplied with texture)
            GL.Uniform3(GL.GetUniformLocation(_shaderProgram, "objectColor"), new Vector3(1.0f, 1.0f, 1.0f));
            
            GL.DrawElements(PrimitiveType.Triangles, _groundIndexCount, DrawElementsType.UnsignedInt, 0);
            
            // Disable texture
            GL.Uniform1(GL.GetUniformLocation(_shaderProgram, "useTexture"), 0);
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
            
            // Create a modern, aerodynamic train mesh
            const float carLength = 2.5f;
            const float carWidth = 1.2f;
            const float carHeight = 1.0f;
            const float frontHeight = 1.2f;
            const float wheelRadius = 0.35f;  // Increased wheel size
            const float wheelWidth = 0.4f;    // Increased wheel width
            const float rodRadius = 0.12f;    // Thick black rod radius
            const float rodLength = 1.0f;     // Rod length between carriages

            List<float> vertices = new List<float>();
            List<uint> indices = new List<uint>();

            // Main body vertices
            vertices.AddRange(new float[] {
                // Front face (curved)
                0, frontHeight, -carWidth/2, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f,
                carLength * 0.2f, carHeight, -carWidth/2, 0.0f, 0.0f, 1.0f, 0.2f, 0.0f,
                carLength * 0.2f, 0, -carWidth/2, 0.0f, 0.0f, 1.0f, 0.2f, 1.0f,
                0, 0, -carWidth/2, 0.0f, 0.0f, 1.0f, 0.0f, 1.0f,

                // Back face
                0, carHeight, carWidth/2, 0.0f, 0.0f, -1.0f, 0.0f, 0.0f,
                carLength, carHeight, carWidth/2, 0.0f, 0.0f, -1.0f, 1.0f, 0.0f,
                carLength, 0, carWidth/2, 0.0f, 0.0f, -1.0f, 1.0f, 1.0f,
                0, 0, carWidth/2, 0.0f, 0.0f, -1.0f, 0.0f, 1.0f,

                // Top face (curved)
                0, frontHeight, -carWidth/2, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f,
                carLength, carHeight, -carWidth/2, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f,
                carLength, carHeight, carWidth/2, 0.0f, 1.0f, 0.0f, 1.0f, 1.0f,
                0, carHeight, carWidth/2, 0.0f, 1.0f, 0.0f, 0.0f, 1.0f,

                // Left face (curved)
                0, frontHeight, -carWidth/2, -1.0f, 0.0f, 0.0f, 0.0f, 0.0f,
                0, carHeight, carWidth/2, -1.0f, 0.0f, 0.0f, 1.0f, 0.0f,
                0, 0, carWidth/2, -1.0f, 0.0f, 0.0f, 1.0f, 1.0f,
                0, 0, -carWidth/2, -1.0f, 0.0f, 0.0f, 0.0f, 1.0f,

                // Right face (curved)
                carLength, carHeight, -carWidth/2, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f,
                carLength, carHeight, carWidth/2, 1.0f, 0.0f, 0.0f, 1.0f, 0.0f,
                carLength, 0, carWidth/2, 1.0f, 0.0f, 0.0f, 1.0f, 1.0f,
                carLength, 0, -carWidth/2, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f,

                // Front windshield (angled)
                carLength * 0.2f, carHeight, -carWidth/2, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f,
                carLength * 0.4f, frontHeight, -carWidth/2, 0.0f, 0.0f, 1.0f, 1.0f, 0.0f,
                carLength * 0.4f, carHeight, -carWidth/2, 0.0f, 0.0f, 1.0f, 1.0f, 1.0f,
                carLength * 0.2f, carHeight, -carWidth/2, 0.0f, 0.0f, 1.0f, 0.0f, 1.0f,

                // Front left wheel
                carLength * 0.2f, -wheelRadius, -carWidth/2 - wheelWidth/2, 0.0f, -1.0f, 0.0f, 0.0f, 0.0f,
                carLength * 0.2f, -wheelRadius, -carWidth/2 + wheelWidth/2, 0.0f, -1.0f, 0.0f, 1.0f, 0.0f,
                carLength * 0.2f + wheelRadius, -wheelRadius, -carWidth/2 + wheelWidth/2, 0.0f, -1.0f, 0.0f, 1.0f, 1.0f,
                carLength * 0.2f + wheelRadius, -wheelRadius, -carWidth/2 - wheelWidth/2, 0.0f, -1.0f, 0.0f, 0.0f, 1.0f,

                // Front right wheel
                carLength * 0.2f, -wheelRadius, carWidth/2 - wheelWidth/2, 0.0f, -1.0f, 0.0f, 0.0f, 0.0f,
                carLength * 0.2f, -wheelRadius, carWidth/2 + wheelWidth/2, 0.0f, -1.0f, 0.0f, 1.0f, 0.0f,
                carLength * 0.2f + wheelRadius, -wheelRadius, carWidth/2 + wheelWidth/2, 0.0f, -1.0f, 0.0f, 1.0f, 1.0f,
                carLength * 0.2f + wheelRadius, -wheelRadius, carWidth/2 - wheelWidth/2, 0.0f, -1.0f, 0.0f, 0.0f, 1.0f,

                // Back left wheel
                carLength * 0.8f, -wheelRadius, -carWidth/2 - wheelWidth/2, 0.0f, -1.0f, 0.0f, 0.0f, 0.0f,
                carLength * 0.8f, -wheelRadius, -carWidth/2 + wheelWidth/2, 0.0f, -1.0f, 0.0f, 1.0f, 0.0f,
                carLength * 0.8f + wheelRadius, -wheelRadius, -carWidth/2 + wheelWidth/2, 0.0f, -1.0f, 0.0f, 1.0f, 1.0f,
                carLength * 0.8f + wheelRadius, -wheelRadius, -carWidth/2 - wheelWidth/2, 0.0f, -1.0f, 0.0f, 0.0f, 1.0f,

                // Back right wheel
                carLength * 0.8f, -wheelRadius, carWidth/2 - wheelWidth/2, 0.0f, -1.0f, 0.0f, 0.0f, 0.0f,
                carLength * 0.8f, -wheelRadius, carWidth/2 + wheelWidth/2, 0.0f, -1.0f, 0.0f, 1.0f, 0.0f,
                carLength * 0.8f + wheelRadius, -wheelRadius, carWidth/2 + wheelWidth/2, 0.0f, -1.0f, 0.0f, 1.0f, 1.0f,
                carLength * 0.8f + wheelRadius, -wheelRadius, carWidth/2 - wheelWidth/2, 0.0f, -1.0f, 0.0f, 0.0f, 1.0f
            });

            // Generate indices for all faces
            for (int i = 0; i < 10; i++) // 10 faces (6 car faces + 4 wheel faces)
            {
                int baseIndex = i * 4;
                indices.AddRange(new uint[] {
                    (uint)baseIndex, (uint)(baseIndex + 1), (uint)(baseIndex + 2),
                    (uint)baseIndex, (uint)(baseIndex + 2), (uint)(baseIndex + 3)
                });
            }

            // Create and bind VAO
            _trainVAO = GL.GenVertexArray();
            GL.BindVertexArray(_trainVAO);

            // Create and bind VBO
            _trainVBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _trainVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * sizeof(float), vertices.ToArray(), BufferUsageHint.StaticDraw);

            // Create and bind EBO
            _trainEBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _trainEBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Count * sizeof(uint), indices.ToArray(), BufferUsageHint.StaticDraw);

            // Set up vertex attributes
            int stride = 8 * sizeof(float);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, 6 * sizeof(float));
            GL.EnableVertexAttribArray(2);

            // Unbind VAO
            GL.BindVertexArray(0);

            Console.WriteLine($"Renderer.CreateTrainMesh: Train mesh creation complete with {vertices.Count/8} vertices and {indices.Count/3} triangles");
        }

        public void Draw(Matrix4 view, Matrix4 projection, RollerCoasterSim.Train.Train train)
        {
            try
            {
                // Update animation time
                _leafTime += 0.016f;
                _waterTime += 0.01f;
                
                // Clear the screen with sky color first
                DrawSky();

                GL.UseProgram(_shaderProgram);

                // Set view and projection matrices
                GL.UniformMatrix4(GL.GetUniformLocation(_shaderProgram, "view"), false, ref view);
                GL.UniformMatrix4(GL.GetUniformLocation(_shaderProgram, "projection"), false, ref projection);

                // Set view position for lighting
                Vector3 viewPos = new Vector3(view.Row3.X, view.Row3.Y, view.Row3.Z);
                GL.Uniform3(GL.GetUniformLocation(_shaderProgram, "viewPos"), viewPos);

                // Reset special rendering flags
                GL.Uniform1(GL.GetUniformLocation(_shaderProgram, "isSun"), 0);
                GL.Uniform1(GL.GetUniformLocation(_shaderProgram, "isSky"), 0);

                // Draw sun
                DrawSun();

                // Draw ground
                DrawGround();

                // Draw water
                DrawWater();

                // Draw rocks
                DrawRocks();

                // Draw track
                DrawTrack();

                // Draw train
                DrawTrain(train);

                // Draw trees
                Trees.Draw(_shaderProgram);

                // Draw platform
                DrawPlatform();

                // Draw fence
                DrawFence();

                // Draw station
                DrawStation();

                // Draw leaves and grass after ground but before other elements
                DrawLeavesAndGrass();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in Renderer.Draw: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private void DrawTrack()
        {
            GL.BindVertexArray(_trackVAO);
            Matrix4 trackModel = Matrix4.CreateTranslation(0, 1.5f, 0);
            GL.UniformMatrix4(GL.GetUniformLocation(_shaderProgram, "model"), false, ref trackModel);
            
            // Enable texture
            GL.Uniform1(GL.GetUniformLocation(_shaderProgram, "useTexture"), 1);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _trackTexture);
            GL.Uniform1(GL.GetUniformLocation(_shaderProgram, "texture0"), 0);
            
            // Set base color (will be multiplied with texture)
            GL.Uniform3(GL.GetUniformLocation(_shaderProgram, "objectColor"), new Vector3(0.8f, 0.8f, 0.8f));
            
            GL.DrawElements(PrimitiveType.Triangles, _trackIndexCount, DrawElementsType.UnsignedInt, 0);
            
            // Disable texture
            GL.Uniform1(GL.GetUniformLocation(_shaderProgram, "useTexture"), 0);
        }

        private void DrawTrain(RollerCoasterSim.Train.Train train)
        {
            const float carLength = 2.5f;
            const float rodRadius = 0.12f;
            const float rodOffset = 0.1f;  // How close the rod is to the carriage
            const float rodLength = 1.2f;  // Increased rod length
            const float VERTICAL_OFFSET = 1.5f;  // Height above track

            var cars = train.GetCars();
            List<Vector3> carCenters = new List<Vector3>();

            // Bind the train VAO once
            GL.BindVertexArray(_trainVAO);

            // Draw each car
            for (int i = 0; i < cars.Count; i++)
            {
                var car = cars[i];
                Vector3 position = car.GetPosition();
                Vector3 direction = car.GetDirection().Normalized();
                Vector3 trackNormal = car.GetNormal().Normalized();
                
                // Calculate the right vector perpendicular to both direction and track normal
                Vector3 right = Vector3.Cross(direction, trackNormal).Normalized();
                // Calculate up vector to ensure proper orientation
                Vector3 up = Vector3.Cross(right, direction).Normalized();

                // Determine if we're at the bottom of the loop (when track normal points mostly downward)
                bool isAtBottom = trackNormal.Y < -0.7f;  // Threshold for considering it the bottom of the loop

                carCenters.Add(position);

                // Create rotation matrix that aligns with track orientation
                // Flip the train only when at the bottom of the loop
                Matrix4 rotation = new Matrix4(
                    new Vector4(right.X, right.Y, right.Z, 0),
                    new Vector4(isAtBottom ? -up.X : up.X, isAtBottom ? -up.Y : up.Y, isAtBottom ? -up.Z : up.Z, 0),
                    new Vector4(-direction.X, -direction.Y, -direction.Z, 0),
                    new Vector4(0, 0, 0, 1)
                );

                // Add offset to position wheels above the track surface
                Vector3 adjustedPosition = position + trackNormal * VERTICAL_OFFSET;
                Matrix4 modelMatrix = rotation * Matrix4.CreateTranslation(adjustedPosition);
                
                GL.UniformMatrix4(GL.GetUniformLocation(_shaderProgram, "model"), false, ref modelMatrix);

                // Draw black car
                GL.Uniform3(GL.GetUniformLocation(_shaderProgram, "objectColor"), new Vector3(0.1f, 0.1f, 0.1f)); // Pure black
                GL.DrawElements(PrimitiveType.Triangles, 60, DrawElementsType.UnsignedInt, 0);
            }
            GL.BindVertexArray(0);

            // Draw rods between cars
            for (int i = 0; i < cars.Count - 1; i++)
            {
                Vector3 back = carCenters[i] - cars[i].GetDirection() * (carLength/2 - rodOffset) + cars[i].GetNormal() * VERTICAL_OFFSET;
                Vector3 front = carCenters[i + 1] + cars[i + 1].GetDirection() * (carLength/2 - rodOffset) + cars[i + 1].GetNormal() * VERTICAL_OFFSET;
                Vector3 rodColor = new Vector3(0.1f, 0.1f, 0.1f);  // Black color for rods
                DrawCylinder(back, front, rodRadius, rodColor);
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
            // Disable depth writing but keep depth testing
            GL.DepthMask(false);
            
            // Clear the background with a base sky color
            GL.ClearColor(0.4f, 0.6f, 0.8f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            
            // Draw gradient sky
            GL.Uniform1(GL.GetUniformLocation(_shaderProgram, "isSky"), 1);
            GL.BindVertexArray(_skyVAO);
            
            Matrix4 skyModel = Matrix4.Identity;
            GL.UniformMatrix4(GL.GetUniformLocation(_shaderProgram, "model"), false, ref skyModel);
            GL.DrawElements(PrimitiveType.Triangles, _skyIndexCount, DrawElementsType.UnsignedInt, 0);
            
            GL.Uniform1(GL.GetUniformLocation(_shaderProgram, "isSky"), 0);
            
            // Re-enable depth writing
            GL.DepthMask(true);
        }

        // Helper to draw a simple white cloud billboard (quad)
        private void DrawCloudBillboard(Vector3 center, float size, float alpha)
        {
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.Disable(EnableCap.DepthTest); // Temporarily disable depth testing
            Vector3 right = new Vector3(1, 0, 0) * size * 0.5f;
            Vector3 up = new Vector3(0, 1, 0) * size * 0.5f;
            Vector3 p0 = center - right - up;
            Vector3 p1 = center + right - up;
            Vector3 p2 = center + right + up;
            Vector3 p3 = center - right + up;
            float[] vertices = new float[] {
                p0.X, p0.Y, p0.Z, 0, 0,
                p1.X, p1.Y, p1.Z, 1, 0,
                p2.X, p2.Y, p2.Z, 1, 1,
                p3.X, p3.Y, p3.Z, 0, 1
            };
            uint[] indices = new uint[] { 0, 1, 2, 0, 2, 3 };
            int vao = GL.GenVertexArray();
            int vbo = GL.GenBuffer();
            int ebo = GL.GenBuffer();
            GL.BindVertexArray(vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.DynamicDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.DynamicDraw);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(2);
            GL.Uniform3(GL.GetUniformLocation(_shaderProgram, "objectColor"), new Vector3(1.0f, 1.0f, 1.0f));
            GL.Uniform1(GL.GetUniformLocation(_shaderProgram, "alpha"), 1.0f); // Fully opaque
            GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
            GL.DeleteBuffer(vbo);
            GL.DeleteBuffer(ebo);
            GL.DeleteVertexArray(vao);
            GL.Enable(EnableCap.DepthTest); // Re-enable depth testing
            GL.Disable(EnableCap.Blend);
        }

        // Update DrawSun to make clouds more visible
        private void DrawSun()
        {
            GL.Uniform1(GL.GetUniformLocation(_shaderProgram, "isSun"), 1);
            GL.BindVertexArray(_sunVAO);
            Vector3 sunPos = new Vector3(100, 60, 100);
            Matrix4 sunModel = Matrix4.CreateTranslation(sunPos);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            Matrix4 glowModel = Matrix4.CreateScale(4.0f) * sunModel;
            GL.UniformMatrix4(GL.GetUniformLocation(_shaderProgram, "model"), false, ref glowModel);
            GL.Uniform3(GL.GetUniformLocation(_shaderProgram, "objectColor"), new Vector3(1.0f, 0.95f, 0.5f));
            GL.Uniform1(GL.GetUniformLocation(_shaderProgram, "alpha"), 0.35f);
            GL.DrawElements(PrimitiveType.Triangles, _sunIndexCount, DrawElementsType.UnsignedInt, 0);
            GL.Uniform1(GL.GetUniformLocation(_shaderProgram, "alpha"), 1.0f);
            Matrix4 sunSolidModel = Matrix4.CreateScale(1.5f) * sunModel;
            GL.UniformMatrix4(GL.GetUniformLocation(_shaderProgram, "model"), false, ref sunSolidModel);
            GL.Uniform3(GL.GetUniformLocation(_shaderProgram, "objectColor"), new Vector3(1.0f, 0.95f, 0.0f));
            GL.DrawElements(PrimitiveType.Triangles, _sunIndexCount, DrawElementsType.UnsignedInt, 0);
            
            GL.Uniform1(GL.GetUniformLocation(_shaderProgram, "isSun"), 0);
            GL.Disable(EnableCap.Blend);
        }

        private void GenerateTreePositions(int count = 100)
        {
            var rand = new System.Random(42);
            _treePositions.Clear();
            _treeScales.Clear();
            
            // Define tree types
            var treeTypes = new[]
            {
                // Pine trees (tall, narrow)
                (height: 4.0f, trunkRadius: 0.15f, leavesHeight: 3.0f, leavesRadius: 0.8f, color: new Vector3(0.1f, 0.4f, 0.1f), probability: 0.3f),
                // Deciduous trees (medium, rounded)
                (height: 2.5f, trunkRadius: 0.2f, leavesHeight: 2.0f, leavesRadius: 1.2f, color: new Vector3(0.2f, 0.5f, 0.2f), probability: 0.5f),
                // Bushes (small, wide)
                (height: 1.5f, trunkRadius: 0.1f, leavesHeight: 1.2f, leavesRadius: 1.5f, color: new Vector3(0.15f, 0.45f, 0.15f), probability: 0.2f)
            };

            const float lakeRadius = 15.0f; // Match the lake radius from CreateWaterMesh

            for (int i = 0; i < count; i++)
            {
                float x = (float)(rand.NextDouble() * 180 - 90);
                float z = (float)(rand.NextDouble() * 180 - 90);
                
                // Check if position is within lake area
                float distanceFromLakeCenter = (float)Math.Sqrt(x * x + z * z);
                if (distanceFromLakeCenter < lakeRadius + 2.0f) // Add 2.0f buffer zone around lake
                {
                    i--; // Try again
                    continue;
                }
                
                // Avoid placing trees too close to the track center
                if (MathF.Abs(x) < 10 && MathF.Abs(z) < 10) 
                {
                    i--; 
                    continue;
                }
                
                float y = GetTerrainHeight(x, z);
                
                // Select tree type based on probability
                float r = (float)rand.NextDouble();
                float cumulative = 0;
                var selectedType = treeTypes[0];
                foreach (var type in treeTypes)
                {
                    cumulative += type.probability;
                    if (r <= cumulative)
                    {
                        selectedType = type;
                        break;
                    }
                }
                
                // Add some random variation to the tree dimensions
                float heightVariation = 0.8f + (float)rand.NextDouble() * 0.4f; // 0.8 to 1.2
                float radiusVariation = 0.9f + (float)rand.NextDouble() * 0.2f; // 0.9 to 1.1
                
                _treePositions.Add(new Vector3(x, y, z));
                _treeScales.Add((
                    trunkHeight: selectedType.height * heightVariation,
                    trunkRadius: selectedType.trunkRadius * radiusVariation,
                    leavesHeight: selectedType.leavesHeight * heightVariation,
                    leavesRadius: selectedType.leavesRadius * radiusVariation
                ));
            }
        }

        private void CreateTreeMesh()
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
            // Pine tree leaves (cone)
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

        private void CreateSkyMesh()
        {
            // Create a large hemisphere for the sky
            const int segments = 32;
            const float radius = 1000.0f;  // Very large radius to create the sky dome effect
            List<float> vertices = new List<float>();
            List<uint> indices = new List<uint>();

            // Generate hemisphere vertices (inside out)
            for (int i = 0; i <= segments; i++)
            {
                float theta = i * MathF.PI * 2 / segments;
                for (int j = 0; j <= segments/2; j++)  // Only half the vertical segments for hemisphere
                {
                    float phi = j * MathF.PI / segments;
                    float x = -radius * MathF.Sin(phi) * MathF.Cos(theta); // Negative to make it inside out
                    float y = -radius * MathF.Cos(phi);                    // Negative to make it inside out
                    float z = -radius * MathF.Sin(phi) * MathF.Sin(theta); // Negative to make it inside out
                    
                    // Position
                    vertices.Add(x); vertices.Add(y); vertices.Add(z);
                    
                    // Normal (pointing inward)
                    vertices.Add(-x/radius); vertices.Add(-y/radius); vertices.Add(-z/radius);
                    
                    // Texture coordinates and gradient factor
                    vertices.Add((float)i / segments); vertices.Add((float)j / (segments/2));
                }
            }

            // Generate indices (reversed winding order)
            for (int i = 0; i < segments; i++)
            {
                for (int j = 0; j < segments/2; j++)  // Only half the vertical segments
                {
                    uint a = (uint)(i * (segments/2 + 1) + j);
                    uint b = (uint)((i + 1) * (segments/2 + 1) + j);
                    uint c = (uint)((i + 1) * (segments/2 + 1) + (j + 1));
                    uint d = (uint)(i * (segments/2 + 1) + (j + 1));
                    indices.Add(a); indices.Add(c); indices.Add(b); // Reversed winding order
                    indices.Add(a); indices.Add(d); indices.Add(c); // Reversed winding order
                }
            }

            // Create and bind VAO
            _skyVAO = GL.GenVertexArray();
            GL.BindVertexArray(_skyVAO);

            // Create and bind VBO
            _skyVBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _skyVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * sizeof(float), vertices.ToArray(), BufferUsageHint.StaticDraw);

            // Create and bind EBO
            _skyEBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _skyEBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Count * sizeof(uint), indices.ToArray(), BufferUsageHint.StaticDraw);

            // Set up vertex attributes
            int stride = 8 * sizeof(float);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, 6 * sizeof(float));
            GL.EnableVertexAttribArray(2);

            _skyIndexCount = indices.Count;

            // Unbind VAO
            GL.BindVertexArray(0);
            
            Console.WriteLine($"Created sky mesh with {vertices.Count/8} vertices and {indices.Count/3} triangles");
        }

        public float GetTerrainHeight(float x, float z)
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

        // Helper to draw a cylinder between two points
        private void DrawCylinder(Vector3 start, Vector3 end, float radius, Vector3 color)
        {
            const int segments = 20;
            List<float> vertices = new List<float>();
            List<uint> indices = new List<uint>();
            Vector3 axis = end - start;
            float height = axis.Length;
            axis.Normalize();
            // Find a vector perpendicular to axis
            Vector3 up = Math.Abs(Vector3.Dot(axis, Vector3.UnitY)) < 0.99f ? Vector3.UnitY : Vector3.UnitZ;
            Vector3 side = Vector3.Cross(axis, up).Normalized();
            Vector3 up2 = Vector3.Cross(side, axis).Normalized();
            // Generate circle points
            for (int i = 0; i <= segments; i++)
            {
                float theta = i * MathF.PI * 2 / segments;
                float x = MathF.Cos(theta) * radius;
                float y = MathF.Sin(theta) * radius;
                Vector3 offset = side * x + up2 * y;
                // Bottom circle
                vertices.Add((start + offset).X); vertices.Add((start + offset).Y); vertices.Add((start + offset).Z);
                vertices.Add(axis.X); vertices.Add(axis.Y); vertices.Add(axis.Z);
                vertices.Add(0); vertices.Add(0);
                // Top circle
                vertices.Add((end + offset).X); vertices.Add((end + offset).Y); vertices.Add((end + offset).Z);
                vertices.Add(axis.X); vertices.Add(axis.Y); vertices.Add(axis.Z);
                vertices.Add(1); vertices.Add(1);
            }
            // Indices
            for (int i = 0; i < segments; i++)
            {
                int b0 = i * 2;
                int t0 = b0 + 1;
                int b1 = ((i + 1) % (segments + 1)) * 2;
                int t1 = b1 + 1;
                indices.Add((uint)b0); indices.Add((uint)t0); indices.Add((uint)t1);
                indices.Add((uint)b0); indices.Add((uint)t1); indices.Add((uint)b1);
            }
            // Upload and draw (immediate mode, not efficient but fine for a few rods)
            int vao = GL.GenVertexArray();
            int vbo = GL.GenBuffer();
            int ebo = GL.GenBuffer();
            GL.BindVertexArray(vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * sizeof(float), vertices.ToArray(), BufferUsageHint.DynamicDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Count * sizeof(uint), indices.ToArray(), BufferUsageHint.DynamicDraw);
            int stride = 8 * sizeof(float);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, 6 * sizeof(float));
            GL.EnableVertexAttribArray(2);
            GL.Uniform3(GL.GetUniformLocation(_shaderProgram, "objectColor"), color);
            Matrix4 model = Matrix4.Identity;
            GL.UniformMatrix4(GL.GetUniformLocation(_shaderProgram, "model"), false, ref model);
            GL.DrawElements(PrimitiveType.Triangles, indices.Count, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
            GL.DeleteBuffer(vbo);
            GL.DeleteBuffer(ebo);
            GL.DeleteVertexArray(vao);
        }

        private void GenerateRockPositions(int count = 200)
        {
            var rand = new System.Random(43); // Different seed from trees
            _rockPositions.Clear();

            for (int i = 0; i < count; i++)
            {
                float x = (float)(rand.NextDouble() * 180 - 90);
                float z = (float)(rand.NextDouble() * 180 - 90);
                
                // Avoid placing rocks too close to the track center
                if (MathF.Abs(x) < 8 && MathF.Abs(z) < 8) { i--; continue; }
                
                float y = GetTerrainHeight(x, z);
                
                // Random scale between 0.2 and 0.8
                float scale = 0.2f + (float)rand.NextDouble() * 0.6f;
                
                // Random rotation around Y axis
                float rotation = (float)(rand.NextDouble() * MathHelper.TwoPi);

                // Generate a stable color for this rock
                float colorSeed = (float)rand.NextDouble();
                Vector3 color;
                if (colorSeed < 0.5f) // Light gray rocks
                {
                    float gray = 0.7f + (float)rand.NextDouble() * 0.1f; // 0.7 to 0.8
                    color = new Vector3(gray, gray, gray);
                }
                else // Light brown rocks
                {
                    float r = 0.7f + (float)rand.NextDouble() * 0.1f; // 0.7 to 0.8
                    float g = 0.6f + (float)rand.NextDouble() * 0.1f; // 0.6 to 0.7
                    float b = 0.5f + (float)rand.NextDouble() * 0.1f; // 0.5 to 0.6
                    color = new Vector3(r, g, b);
                }
                
                _rockPositions.Add((new Vector3(x, y, z), scale, rotation, color));
            }
        }

        private void CreateRockMesh()
        {
            const int segments = 8;
            List<float> vertices = new List<float>();
            List<uint> indices = new List<uint>();

            // Create a slightly irregular rock shape
            for (int i = 0; i <= segments; i++)
            {
                float theta = i * MathF.PI * 2 / segments;
                for (int j = 0; j <= segments; j++)
                {
                    float phi = j * MathF.PI / segments;
                    
                    // Add some irregularity to the radius
                    float radius = 1.0f + 0.2f * MathF.Sin(theta * 2) * MathF.Cos(phi * 2);
                    
                    float x = radius * MathF.Sin(phi) * MathF.Cos(theta);
                    float y = radius * MathF.Cos(phi);
                    float z = radius * MathF.Sin(phi) * MathF.Sin(theta);
                    
                    // Position
                    vertices.Add(x); vertices.Add(y); vertices.Add(z);
                    
                    // Normal (normalized position)
                    Vector3 normal = Vector3.Normalize(new Vector3(x, y, z));
                    vertices.Add(normal.X); vertices.Add(normal.Y); vertices.Add(normal.Z);
                    
                    // Texture coordinates
                    vertices.Add((float)i / segments); vertices.Add((float)j / segments);
                }
            }

            // Generate indices
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

            _rockVAO = GL.GenVertexArray();
            GL.BindVertexArray(_rockVAO);

            _rockVBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _rockVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * sizeof(float), vertices.ToArray(), BufferUsageHint.StaticDraw);

            _rockEBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _rockEBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Count * sizeof(uint), indices.ToArray(), BufferUsageHint.StaticDraw);

            int stride = 8 * sizeof(float);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, 6 * sizeof(float));
            GL.EnableVertexAttribArray(2);

            _rockIndexCount = indices.Count;
        }

        private void DrawRocks()
        {
            GL.BindVertexArray(_rockVAO);
            
            foreach (var (position, scale, rotation, color) in _rockPositions)
            {
                // Create transformation matrix
                Matrix4 model = Matrix4.CreateScale(scale) * 
                               Matrix4.CreateRotationY(rotation) * 
                               Matrix4.CreateTranslation(position);
                
                GL.UniformMatrix4(GL.GetUniformLocation(_shaderProgram, "model"), false, ref model);
                
                // Use the pre-generated stable color
                GL.Uniform3(GL.GetUniformLocation(_shaderProgram, "objectColor"), color);
                
                GL.DrawElements(PrimitiveType.Triangles, _rockIndexCount, DrawElementsType.UnsignedInt, 0);
            }
        }

        private void CreateLeafMesh()
        {
            // Create a heart-shaped leaf
            List<float> vertices = new List<float>();
            List<uint> indices = new List<uint>();

            // Heart-shaped leaf vertices
            vertices.AddRange(new float[] {
                // Position (3), Normal (3), TexCoord (2)
                // Stem point
                0.0f,  -0.5f, 0.0f,  0.0f,  1.0f,  0.0f,  0.5f, 0.0f,  // Stem
                
                // Left side of heart
                -0.4f, -0.3f, 0.0f,  0.0f,  1.0f,  0.0f,  0.2f, 0.2f,  // Left curve start
                -0.5f, 0.0f, 0.0f,  0.0f,  1.0f,  0.0f,  0.0f, 0.4f,  // Left top
                -0.3f, 0.2f, 0.0f,  0.0f,  1.0f,  0.0f,  0.3f, 0.6f,  // Left curve end
                
                // Right side of heart
                0.4f, -0.3f, 0.0f,  0.0f,  1.0f,  0.0f,  0.8f, 0.2f,  // Right curve start
                0.5f, 0.0f, 0.0f,  0.0f,  1.0f,  0.0f,  1.0f, 0.4f,  // Right top
                0.3f, 0.2f, 0.0f,  0.0f,  1.0f,  0.0f,  0.7f, 0.6f,  // Right curve end
                
                // Center point
                0.0f, 0.3f, 0.0f,  0.0f,  1.0f,  0.0f,  0.5f, 0.8f,  // Center top
            });

            // Indices for the heart shape (multiple triangles)
            indices.AddRange(new uint[] {
                0, 1, 2,  // Left side triangle 1
                0, 2, 3,  // Left side triangle 2
                0, 4, 5,  // Right side triangle 1
                0, 5, 6,  // Right side triangle 2
                3, 6, 7,  // Top center triangle
            });

            _leafVAO = GL.GenVertexArray();
            GL.BindVertexArray(_leafVAO);

            _leafVBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _leafVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * sizeof(float), vertices.ToArray(), BufferUsageHint.StaticDraw);

            _leafEBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _leafEBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Count * sizeof(uint), indices.ToArray(), BufferUsageHint.StaticDraw);

            int stride = 8 * sizeof(float);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, 6 * sizeof(float));
            GL.EnableVertexAttribArray(2);

            _leafIndexCount = indices.Count;
        }

        private void GenerateLeafParticles(int count)
        {
            var rand = new Random(44);
            _leafParticles.Clear();

            // Generate leaves around each tree
            foreach (var treePos in _treePositions)
            {
                // Number of leaves per tree (random between 3-8)
                int leavesPerTree = rand.Next(3, 9);
                
                for (int i = 0; i < leavesPerTree; i++)
                {
                    // Random position within a radius around the tree
                    float angle = (float)(rand.NextDouble() * MathHelper.TwoPi);
                    float radius = 1.0f + (float)rand.NextDouble() * 2.0f; // 1-3 units from tree
                    float x = treePos.X + radius * (float)Math.Cos(angle);
                    float z = treePos.Z + radius * (float)Math.Sin(angle);
                    float y = GetTerrainHeight(x, z) + 0.1f; // Slightly above ground
                    
                    // Random rotation and scale
                    float rotation = (float)(rand.NextDouble() * MathHelper.TwoPi);
                    float scale = 0.3f + (float)rand.NextDouble() * 0.3f; // Smaller scale for more delicate look
                    float alpha = 0.7f + (float)rand.NextDouble() * 0.3f;
                    
                    _leafParticles.Add((new Vector3(x, y, z), rotation, scale, alpha));
                }
            }
        }

        private void DrawLeavesAndGrass()
        {
            // Enable blending for transparent effects
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            
            // Draw leaves (static)
            GL.BindVertexArray(_leafVAO);
            foreach (var (position, rotation, scale, alpha) in _leafParticles)
            {
                Matrix4 model = Matrix4.CreateScale(scale) *
                               Matrix4.CreateRotationY(rotation) *
                               Matrix4.CreateTranslation(position);
                
                GL.UniformMatrix4(GL.GetUniformLocation(_shaderProgram, "model"), false, ref model);
                GL.Uniform3(GL.GetUniformLocation(_shaderProgram, "objectColor"), new Vector3(0.2f, 0.5f, 0.2f));
                GL.Uniform1(GL.GetUniformLocation(_shaderProgram, "alpha"), alpha);
                
                GL.DrawElements(PrimitiveType.Triangles, _leafIndexCount, DrawElementsType.UnsignedInt, 0);
            }
            
            GL.Disable(EnableCap.Blend);
        }

        private int CreateTrackTexture()
        {
            const int textureSize = 512;
            byte[] textureData = new byte[textureSize * textureSize * 4]; // RGBA

            // Create a procedural metallic texture with noise
            Random random = new Random(46); // Fixed seed for consistency
            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    int index = (y * textureSize + x) * 4;
                    
                    // Base metallic color (dark gray)
                    float baseColor = 0.2f;
                    
                    // Add noise pattern
                    float noise = (float)random.NextDouble() * 0.1f;
                    
                    // Add some directional highlights
                    float highlight = MathF.Sin((float)x / textureSize * MathF.PI * 4) * 0.05f;
                    
                    // Add some grain
                    float grain = ((float)random.NextDouble() - 0.5f) * 0.02f;
                    
                    // Combine effects
                    float finalColor = baseColor + noise + highlight + grain;
                    finalColor = Math.Clamp(finalColor, 0.0f, 1.0f);
                    
                    // Set RGB to the same value for metallic look
                    textureData[index] = (byte)(finalColor * 255);
                    textureData[index + 1] = (byte)(finalColor * 255);
                    textureData[index + 2] = (byte)(finalColor * 255);
                    textureData[index + 3] = 255; // Alpha
                }
            }

            int texture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, texture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, textureSize, textureSize, 0, PixelFormat.Rgba, PixelType.UnsignedByte, textureData);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            return texture;
        }

        // Add this helper function before CreateCloudTexture()
        private float SmoothStep(float edge0, float edge1, float x)
        {
            // Scale, bias and saturate x to 0..1 range
            x = Math.Clamp((x - edge0) / (edge1 - edge0), 0.0f, 1.0f);
            // Evaluate polynomial
            return x * x * (3 - 2 * x);
        }

        // Update the cloud texture generation to use our SmoothStep function
        private int CreateCloudTexture()
        {
            const int textureSize = 512;
            byte[] textureData = new byte[textureSize * textureSize * 4]; // RGBA

            // Create a procedural cloud texture
            Random random = new Random(47); // Fixed seed for consistency
            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    int index = (y * textureSize + x) * 4;
                    
                    // Create base noise
                    float noise = 0;
                    float scale = 1.0f;
                    float amplitude = 1.0f;
                    
                    // Multiple layers of noise for more natural clouds
                    for (int i = 0; i < 4; i++)
                    {
                        float nx = x * scale / textureSize;
                        float ny = y * scale / textureSize;
                        noise += amplitude * (float)random.NextDouble();
                        scale *= 2.0f;
                        amplitude *= 0.5f;
                    }
                    
                    // Normalize noise
                    noise = noise / 1.875f; // Sum of amplitudes
                    
                    // Create soft edges using our SmoothStep function
                    float alpha = SmoothStep(0.2f, 0.8f, noise);
                    
                    // Set color (white with slight blue tint)
                    textureData[index] = 255;     // R
                    textureData[index + 1] = 255; // G
                    textureData[index + 2] = 255; // B
                    textureData[index + 3] = (byte)(alpha * 255); // A
                }
            }

            int texture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, texture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, textureSize, textureSize, 0, PixelFormat.Rgba, PixelType.UnsignedByte, textureData);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            return texture;
        }

        private void CreateWaterMesh()
        {
            // Create an irregular lake shape
            const float baseRadius = 15.0f; // Base radius of the lake
            const int segments = 64; // Increased segments for smoother irregular shape
            List<float> vertices = new List<float>();
            List<uint> indices = new List<uint>();

            // Center vertex (slightly below ground level)
            vertices.AddRange(new float[] {
                0.0f, -0.2f, 0.0f,  0.0f, 1.0f, 0.0f,  0.5f, 0.5f, // Center point
            });

            // Generate vertices around the irregular circle
            var rand = new Random(42); // Fixed seed for consistent shape
            for (int i = 0; i <= segments; i++)
            {
                float angle = (float)i / segments * MathHelper.TwoPi;
                
                // Create irregular radius using multiple sine waves
                float radiusVariation = 
                    baseRadius * (1.0f + 0.15f * (float)Math.Sin(angle * 2)) + // Major variation
                    baseRadius * 0.1f * (float)Math.Sin(angle * 5) + // Medium variation
                    baseRadius * 0.05f * (float)Math.Sin(angle * 8); // Small detail variation
                
                float x = radiusVariation * (float)Math.Cos(angle);
                float z = radiusVariation * (float)Math.Sin(angle);
                
                // Get terrain height at this point
                float terrainHeight = GetTerrainHeight(x, z);
                
                // Create a smooth transition from water to land
                float distanceFromCenter = (float)Math.Sqrt(x * x + z * z);
                float transitionStart = baseRadius * 0.8f;
                float transitionWidth = baseRadius * 0.2f;
                
                float blendFactor = 0.0f;
                if (distanceFromCenter > transitionStart)
                {
                    blendFactor = Math.Min(1.0f, (distanceFromCenter - transitionStart) / transitionWidth);
                }
                
                // Interpolate between water height and terrain height
                float y = -0.2f + (terrainHeight + 0.2f) * blendFactor;
                
                // Add some small random height variation for more natural look
                y += (float)(rand.NextDouble() - 0.5) * 0.05f;
                
                float u = 0.5f + 0.5f * (float)Math.Cos(angle);
                float v = 0.5f + 0.5f * (float)Math.Sin(angle);

                vertices.AddRange(new float[] {
                    x, y, z,  0.0f, 1.0f, 0.0f,  u, v,
                });
            }

            // Generate indices for triangles
            for (int i = 0; i < segments; i++)
            {
                indices.Add(0); // Center point
                indices.Add((uint)(i + 1));
                indices.Add((uint)(i + 2));
            }

            _waterVAO = GL.GenVertexArray();
            GL.BindVertexArray(_waterVAO);

            _waterVBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _waterVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * sizeof(float), vertices.ToArray(), BufferUsageHint.StaticDraw);

            _waterEBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _waterEBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Count * sizeof(uint), indices.ToArray(), BufferUsageHint.StaticDraw);

            int stride = 8 * sizeof(float);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, 6 * sizeof(float));
            GL.EnableVertexAttribArray(2);

            _waterIndexCount = indices.Count;

            // Generate rocks around the lake
            GenerateLakeRocks(baseRadius, segments);
        }

        private void GenerateLakeRocks(float baseRadius, int segments)
        {
            var rand = new Random(45); // Different seed from other rocks
            const int rocksPerSegment = 2; // Reduced rocks per segment for more natural distribution
            const float rockOffset = 0.1f; // Keep rocks very close to edge
            const float rockVariation = 0.1f; // Increased variation for more natural look

            // First pass: Place rocks along the irregular shoreline
            for (int i = 0; i < segments; i++)
            {
                float baseAngle = (float)i / segments * MathHelper.TwoPi;
                
                // Use the same radius variation as the water for consistency
                float radiusVariation = 
                    baseRadius * (1.0f + 0.15f * (float)Math.Sin(baseAngle * 2)) +
                    baseRadius * 0.1f * (float)Math.Sin(baseAngle * 5) +
                    baseRadius * 0.05f * (float)Math.Sin(baseAngle * 8);
                
                for (int j = 0; j < rocksPerSegment; j++)
                {
                    // Calculate rock position with variation
                    float angle = baseAngle + (float)rand.NextDouble() * (MathHelper.TwoPi / segments);
                    float distance = radiusVariation + rockOffset + (float)rand.NextDouble() * rockVariation;
                    float x = distance * (float)Math.Cos(angle);
                    float z = distance * (float)Math.Sin(angle);
                    float y = GetTerrainHeight(x, z);

                    // Vary rock sizes more naturally
                    float scale = 0.25f + (float)rand.NextDouble() * 0.3f;
                    
                    // Random rotation around Y axis
                    float rotation = (float)(rand.NextDouble() * MathHelper.TwoPi);

                    // Generate a stable color for this rock
                    float colorSeed = (float)rand.NextDouble();
                    Vector3 color;
                    if (colorSeed < 0.5f) // Light gray rocks
                    {
                        float gray = 0.7f + (float)rand.NextDouble() * 0.1f;
                        color = new Vector3(gray, gray, gray);
                    }
                    else // Light brown rocks
                    {
                        float r = 0.7f + (float)rand.NextDouble() * 0.1f;
                        float g = 0.6f + (float)rand.NextDouble() * 0.1f;
                        float b = 0.5f + (float)rand.NextDouble() * 0.1f;
                        color = new Vector3(r, g, b);
                    }

                    _rockPositions.Add((new Vector3(x, y, z), scale, rotation, color));
                }
            }

            // Second pass: Add some rocks slightly inside the lake for a more natural look
            for (int i = 0; i < segments/3; i++) // Add fewer inner rocks
            {
                float angle = (float)i / (segments/3) * MathHelper.TwoPi;
                float radiusVariation = 
                    baseRadius * (1.0f + 0.15f * (float)Math.Sin(angle * 2)) +
                    baseRadius * 0.1f * (float)Math.Sin(angle * 5) +
                    baseRadius * 0.05f * (float)Math.Sin(angle * 8);
                    
                float distance = radiusVariation * 0.8f; // Slightly inside the main shape
                float x = distance * (float)Math.Cos(angle);
                float z = distance * (float)Math.Sin(angle);
                float y = GetTerrainHeight(x, z);

                float scale = 0.2f + (float)rand.NextDouble() * 0.15f; // Smaller inner rocks
                float rotation = (float)(rand.NextDouble() * MathHelper.TwoPi);

                // Use slightly darker colors for inner rocks
                Vector3 color = new Vector3(0.6f, 0.5f, 0.4f);
                _rockPositions.Add((new Vector3(x, y, z), scale, rotation, color));
            }
        }

        private void DrawWater()
        {
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            
            GL.BindVertexArray(_waterVAO);
            
            // Create water transformation matrix - no additional translation needed
            Matrix4 waterModel = Matrix4.Identity;
            GL.UniformMatrix4(GL.GetUniformLocation(_shaderProgram, "model"), false, ref waterModel);
            
            // Set water color (slight blue tint)
            GL.Uniform3(GL.GetUniformLocation(_shaderProgram, "objectColor"), new Vector3(0.2f, 0.4f, 0.8f));
            
            // Set water transparency
            GL.Uniform1(GL.GetUniformLocation(_shaderProgram, "alpha"), 0.7f);
            
            // Set water flag and time for animation
            GL.Uniform1(GL.GetUniformLocation(_shaderProgram, "isWater"), 1);
            GL.Uniform1(GL.GetUniformLocation(_shaderProgram, "time"), _waterTime);
            
            // Draw water plane
            GL.DrawElements(PrimitiveType.Triangles, _waterIndexCount, DrawElementsType.UnsignedInt, 0);
            
            // Reset water flag
            GL.Uniform1(GL.GetUniformLocation(_shaderProgram, "isWater"), 0);
            
            GL.Disable(EnableCap.Blend);
        }

        public void Dispose()
        {
            // Clean up sky resources
            if (_skyVAO != 0)
            {
                GL.DeleteVertexArray(_skyVAO);
                _skyVAO = 0;
            }
            if (_skyVBO != 0)
            {
                GL.DeleteBuffer(_skyVBO);
                _skyVBO = 0;
            }
            if (_skyEBO != 0)
            {
                GL.DeleteBuffer(_skyEBO);
                _skyEBO = 0;
            }
            // Clean up rock resources
            if (_rockVAO != 0)
            {
                GL.DeleteVertexArray(_rockVAO);
                _rockVAO = 0;
            }
            if (_rockVBO != 0)
            {
                GL.DeleteBuffer(_rockVBO);
                _rockVBO = 0;
            }
            if (_rockEBO != 0)
            {
                GL.DeleteBuffer(_rockEBO);
                _rockEBO = 0;
            }
            // Clean up leaf resources
            if (_leafVAO != 0)
            {
                GL.DeleteVertexArray(_leafVAO);
                _leafVAO = 0;
            }
            if (_leafVBO != 0)
            {
                GL.DeleteBuffer(_leafVBO);
                _leafVBO = 0;
            }
            if (_leafEBO != 0)
            {
                GL.DeleteBuffer(_leafEBO);
                _leafEBO = 0;
            }
            // Clean up track texture
            if (_trackTexture != 0)
            {
                GL.DeleteTexture(_trackTexture);
                _trackTexture = 0;
            }
            // Clean up water resources
            if (_waterVAO != 0)
            {
                GL.DeleteVertexArray(_waterVAO);
                _waterVAO = 0;
            }
            if (_waterVBO != 0)
            {
                GL.DeleteBuffer(_waterVBO);
                _waterVBO = 0;
            }
            if (_waterEBO != 0)
            {
                GL.DeleteBuffer(_waterEBO);
                _waterEBO = 0;
            }
            // ... clean up other resources ...
        }

        // Update DrawBirds to make birds more visible
        private void DrawBirds()
        {
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.Disable(EnableCap.DepthTest); // Temporarily disable depth testing

            // Define bird positions and movement - now at train depth
            Vector3[] birdPositions = new Vector3[]
            {
                new Vector3(0, 15, 0),     // Above the track
                new Vector3(15, 18, 0),    // Right side
                new Vector3(-15, 16, 0)    // Left side
            };

            // Draw each bird as a larger triangle
            foreach (var pos in birdPositions)
            {
                float[] vertices = new float[]
                {
                    pos.X, pos.Y, pos.Z,
                    pos.X + 2.0f, pos.Y + 2.0f, pos.Z,  // Made triangles larger
                    pos.X - 2.0f, pos.Y + 2.0f, pos.Z   // Made triangles larger
                };

                uint[] indices = new uint[] { 0, 1, 2 };

                int vao = GL.GenVertexArray();
                int vbo = GL.GenBuffer();
                int ebo = GL.GenBuffer();

                GL.BindVertexArray(vao);
                GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
                GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.DynamicDraw);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
                GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.DynamicDraw);

                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
                GL.EnableVertexAttribArray(0);

                GL.Uniform3(GL.GetUniformLocation(_shaderProgram, "objectColor"), new Vector3(0.0f, 0.0f, 0.0f)); // Black color for birds
                GL.Uniform1(GL.GetUniformLocation(_shaderProgram, "alpha"), 1.0f); // Fully opaque

                GL.DrawElements(PrimitiveType.Triangles, 3, DrawElementsType.UnsignedInt, 0);

                GL.BindVertexArray(0);
                GL.DeleteBuffer(vbo);
                GL.DeleteBuffer(ebo);
                GL.DeleteVertexArray(vao);
            }

            GL.Enable(EnableCap.DepthTest); // Re-enable depth testing
            GL.Disable(EnableCap.Blend);
        }
    }
}
