using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System; 
using RollerCoasterSim.Objects;
// ImGui removed - using OpenGL-based UI instead

namespace RollerCoasterSim
{
    public enum CameraMode { Free, Follow, Top, Side }

    public class MainWindow : GameWindow
    {
        private RollerCoasterSim.Track.RollerCoasterTrack _track = null!;
        private RollerCoasterSim.Train.Train _train = null!;
        private RollerCoasterSim.Camera.FreeCamera _camera = null!;
        private CameraMode _cameraMode = CameraMode.Free;
        private Renderer _renderer = null!;
        private Vector2? _lastMousePos = null;
        private bool _isDraggingTree = false;
        private Vector3 _dragStartTreePos;
        private int? _draggedTreeIndex = null;
        
        // Enhanced camera and projection controls
        private bool _useOrthographicProjection = false;
        private bool _tKeyLastState = false;
        private bool _lKeyLastState = false;
        private bool _oKeyLastState = false;
        private bool _pKeyLastState = false;

        public MainWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
            Console.WriteLine("MainWindow: Initialized");
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
            Console.WriteLine("MainWindow: OnLoad");
            _track = new RollerCoasterSim.Track.RollerCoasterTrack();
            _train = new RollerCoasterSim.Train.Train(_track);
            _train.SetSpeed(15.0f);
            _camera = new RollerCoasterSim.Camera.FreeCamera(new Vector3(0, 10, 30));
            _renderer = new Renderer();
            _renderer.Init();
            
            Console.WriteLine("🎢 Enhanced Roller Coaster Simulator Loaded!");
            Console.WriteLine("📋 Controls:");
            Console.WriteLine("   WASD: Move camera");
            Console.WriteLine("   Mouse: Look around");
            Console.WriteLine("   Mouse Wheel: Zoom");
            Console.WriteLine("   1-4: Camera modes (Free/Follow/Top/Side)");
            Console.WriteLine("   T: Toggle track texture");
            Console.WriteLine("   L: Cycle light colors");
            Console.WriteLine("   O/P: Switch projection (Orthographic/Perspective)");
            Console.WriteLine("   I/K: Adjust light intensity");
            Console.WriteLine("   Left Click + Drag: Move trees");
            Console.WriteLine("");
            Console.WriteLine("🎮 UI Overlay: Press H to show/hide help");
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);
            _train.Update((float)args.Time);
            var input = KeyboardState;
            float delta = (float)args.Time;
            
            // Camera mode switching
            if (input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.D1)) _cameraMode = CameraMode.Free;
            if (input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.D2)) _cameraMode = CameraMode.Follow;
            if (input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.D3)) _cameraMode = CameraMode.Top;
            if (input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.D4)) _cameraMode = CameraMode.Side;
            
            // Enhanced controls for projection switching and effects
            bool tKeyPressed = input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.T);
            bool lKeyPressed = input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.L);
            bool oKeyPressed = input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.O);
            bool pKeyPressed = input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.P);
            
            // Toggle track texture (T key)
            if (tKeyPressed && !_tKeyLastState)
            {
                _renderer.ToggleTrackTexture();
                Console.WriteLine($"🎨 Track texture: {(_renderer.TrackTextureEnabled ? "ON" : "OFF")}");
            }
            _tKeyLastState = tKeyPressed;
            
            // Cycle light color (L key)
            if (lKeyPressed && !_lKeyLastState)
            {
                _renderer.CycleLightColor();
                string[] lightNames = { "White", "Warm", "Cool", "Red" };
                Console.WriteLine($"💡 Light color: {lightNames[_renderer.LightColorIndex]}");
            }
            _lKeyLastState = lKeyPressed;
            
            // Switch to orthographic projection (O key)
            if (oKeyPressed && !_oKeyLastState)
            {
                _useOrthographicProjection = true;
                Console.WriteLine("📐 Switched to orthographic projection");
            }
            _oKeyLastState = oKeyPressed;
            
            // Switch to perspective projection (P key)
            if (pKeyPressed && !_pKeyLastState)
            {
                _useOrthographicProjection = false;
                Console.WriteLine("🎬 Switched to perspective projection");
            }
            _pKeyLastState = pKeyPressed;
            
            // Light intensity controls (I/K keys)
            if (input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.I))
            {
                _renderer.AdjustLightIntensity(0.1f);
                Console.WriteLine($"💡 Light intensity: {_renderer.LightIntensity:F1}x");
            }
            if (input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.K))
            {
                _renderer.AdjustLightIntensity(-0.1f);
                Console.WriteLine($"💡 Light intensity: {_renderer.LightIntensity:F1}x");
            }
            
            switch (_cameraMode)
            {
                case CameraMode.Free:
                    Vector3 direction = Vector3.Zero;
                    if (input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.W)) direction += _camera.Front;
                    if (input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.S)) direction -= _camera.Front;
                    if (input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.A)) direction -= Vector3.Normalize(Vector3.Cross(_camera.Front, Vector3.UnitY));
                    if (input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.D)) direction += Vector3.Normalize(Vector3.Cross(_camera.Front, Vector3.UnitY));
                    if (direction != Vector3.Zero)
                        _camera.Move(Vector3.Normalize(direction), delta);
                    
                    // Mouse wheel zoom
                    var mouse = MouseState;
                    if (Math.Abs(mouse.ScrollDelta.Y) > 0.1f)
                    {
                        _camera.Zoom(mouse.ScrollDelta.Y * 0.1f);
                    }
                    
                    // Tree drag logic
                    Vector2 mousePos = new Vector2(mouse.X, mouse.Y);
                    if (mouse.IsButtonPressed(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Left))
                    {
                        // Ray pick tree (stub: pick closest in screen space for now)
                        int? picked = null;
                        float minDist = 30f;
                        for (int i = 0; i < _renderer.Trees.Positions.Count; i++)
                        {
                            var world = _renderer.Trees.Positions[i];
                            var screen = WorldToScreen(world, _camera.GetViewMatrix(), GetProjectionMatrix(), Size.X, Size.Y);
                            float dist = (screen - mousePos).Length;
                            if (dist < minDist)
                            {
                                minDist = dist;
                                picked = i;
                            }
                        }
                        if (picked.HasValue)
                        {
                            _renderer.Trees.SelectedIndex = picked;
                            _isDraggingTree = true;
                            _draggedTreeIndex = picked;
                            _dragStartTreePos = _renderer.Trees.Positions[picked.Value];
                            _lastMousePos = mousePos;
                        }
                    }
                    else if (mouse.IsButtonDown(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Left) && _isDraggingTree && _draggedTreeIndex.HasValue)
                    {
                        // Drag selected tree using ground projection
                        if (_lastMousePos.HasValue)
                        {
                            var idx = _draggedTreeIndex.Value;
                            var lastScreen = _lastMousePos.Value;
                            var currScreen = mousePos;
                            var view = _camera.GetViewMatrix();
                            var proj = GetProjectionMatrix();
                            var lastWorld = ScreenToWorldOnGround(lastScreen, view, proj, Size.X, Size.Y);
                            var currWorld = ScreenToWorldOnGround(currScreen, view, proj, Size.X, Size.Y);
                            Vector3 deltaWorld = currWorld - lastWorld;
                            Vector3 newPos = _renderer.Trees.Positions[idx] + new Vector3(deltaWorld.X, 0, deltaWorld.Z);
                            newPos.Y = _renderer.GetTerrainHeight(newPos.X, newPos.Z);
                            _renderer.Trees.Positions[idx] = newPos;
                            _dragStartTreePos = newPos;
                            _lastMousePos = currScreen;
                        }
                    }
                    else
                    {
                        _isDraggingTree = false;
                        _renderer.Trees.SelectedIndex = null; // Remove highlight when not dragging
                        _draggedTreeIndex = null;
                        _lastMousePos = null;
                    }
                    // Only allow camera look if not dragging a tree
                    if (mouse.IsButtonDown(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Left) && !_isDraggingTree)
                        _camera.Look(MouseState.Delta.X, -MouseState.Delta.Y);
                    break;
                case CameraMode.Follow:
                    _camera.SetFollowMode(_train.GetPosition(), _train.GetDirection());
                    break;
                case CameraMode.Top:
                    _camera.SetTopView(_train.GetPosition());
                    break;
                case CameraMode.Side:
                    _camera.SetSideView(_train.GetPosition(), _train.GetDirection());
                    break;
            }
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            Matrix4 view = _camera.GetViewMatrix();
            Matrix4 projection = GetProjectionMatrix();
            _renderer.Draw(view, projection, _train);
            
            // Render simple UI overlay
            RenderUIOverlay();
            
            SwapBuffers();
        }

        private void RenderUIOverlay()
        {
            // Create a simple UI panel using modern OpenGL
            // We'll create a simple colored overlay rectangle
            
            // Save current OpenGL state
            GL.Disable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            
            // Create a simple UI shader for the overlay
            string vertexShaderSource = @"
                #version 330 core
                layout (location = 0) in vec3 aPos;
                layout (location = 1) in vec4 aColor;
                out vec4 ourColor;
                uniform mat4 projection;
                uniform mat4 model;
                void main()
                {
                    gl_Position = projection * model * vec4(aPos, 1.0);
                    ourColor = aColor;
                }";
                
            string fragmentShaderSource = @"
                #version 330 core
                in vec4 ourColor;
                out vec4 FragColor;
                void main()
                {
                    FragColor = ourColor;
                }";
            
            // Compile shaders
            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexShaderSource);
            GL.CompileShader(vertexShader);
            
            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragmentShaderSource);
            GL.CompileShader(fragmentShader);
            
            int uiShaderProgram = GL.CreateProgram();
            GL.AttachShader(uiShaderProgram, vertexShader);
            GL.AttachShader(uiShaderProgram, fragmentShader);
            GL.LinkProgram(uiShaderProgram);
            
            // Create UI panel vertices (simple rectangle with color)
            float[] uiVertices = {
                // Position (x, y, z)     // Color (r, g, b, a)
                10.0f, 10.0f, 0.0f,      0.1f, 0.1f, 0.2f, 0.9f,  // Top-left - Dark blue background
                350.0f, 10.0f, 0.0f,     0.1f, 0.1f, 0.2f, 0.9f,  // Top-right
                350.0f, 280.0f, 0.0f,    0.1f, 0.1f, 0.2f, 0.9f,  // Bottom-right
                10.0f, 280.0f, 0.0f,     0.1f, 0.1f, 0.2f, 0.9f   // Bottom-left
            };
            
            // Create border vertices
            float[] borderVertices = {
                // Position (x, y, z)     // Color (r, g, b, a)
                10.0f, 10.0f, 0.0f,      0.3f, 0.7f, 1.0f, 1.0f,  // Top-left - Blue border
                350.0f, 10.0f, 0.0f,     0.3f, 0.7f, 1.0f, 1.0f,  // Top-right
                350.0f, 15.0f, 0.0f,     0.3f, 0.7f, 1.0f, 1.0f,  // Top-right inner
                10.0f, 15.0f, 0.0f,      0.3f, 0.7f, 1.0f, 1.0f   // Top-left inner
            };
            
            // Create and bind VAO/VBO for UI panel
            int uiVAO = GL.GenVertexArray();
            int uiVBO = GL.GenBuffer();
            
            GL.BindVertexArray(uiVAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, uiVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, uiVertices.Length * sizeof(float), uiVertices, BufferUsageHint.StaticDraw);
            
            // Set up vertex attributes
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 7 * sizeof(float), 0);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 7 * sizeof(float), 3 * sizeof(float));
            
            // Set up orthographic projection for UI
            Matrix4 uiProjection = Matrix4.CreateOrthographicOffCenter(0, Size.X, Size.Y, 0, -1, 1);
            Matrix4 uiModel = Matrix4.Identity;
            
            // Use the UI shader
            GL.UseProgram(uiShaderProgram);
            GL.UniformMatrix4(GL.GetUniformLocation(uiShaderProgram, "projection"), false, ref uiProjection);
            GL.UniformMatrix4(GL.GetUniformLocation(uiShaderProgram, "model"), false, ref uiModel);
            
            // Draw UI panel background
            GL.DrawArrays(PrimitiveType.TriangleFan, 0, 4);
            
            // Draw border
            GL.BindBuffer(BufferTarget.ArrayBuffer, uiVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, borderVertices.Length * sizeof(float), borderVertices, BufferUsageHint.StaticDraw);
            GL.DrawArrays(PrimitiveType.TriangleFan, 0, 4);
            
            // Clean up
            GL.DeleteBuffer(uiVBO);
            GL.DeleteVertexArray(uiVAO);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);
            GL.DeleteProgram(uiShaderProgram);
            
            // Restore OpenGL state
            GL.Enable(EnableCap.DepthTest);
            GL.Disable(EnableCap.Blend);
            
            // Also print status to console for now
            Console.WriteLine($"🎢 Status: Camera={_cameraMode}, Projection={(_useOrthographicProjection ? "Ortho" : "Perspective")}, Texture={(_renderer.TrackTextureEnabled ? "ON" : "OFF")}, Light={_renderer.LightIntensity:F1}x");
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, Size.X, Size.Y);
        }

        protected override void OnUnload()
        {
            base.OnUnload();
            _renderer?.Dispose();
        }

        // Get projection matrix based on current mode
        private Matrix4 GetProjectionMatrix()
        {
            if (_useOrthographicProjection)
            {
                // Orthographic projection for top-down view
                float aspectRatio = Size.X / (float)Size.Y;
                float orthoSize = 50f; // Large enough to see the whole scene
                return Matrix4.CreateOrthographic(orthoSize * aspectRatio, orthoSize, 0.1f, 200f);
            }
            else
            {
                // Perspective projection for immersive view
                return Matrix4.CreatePerspectiveFieldOfView(
                    MathHelper.DegreesToRadians(70f),
                    Size.X / (float)Size.Y,
                    0.1f,
                    100f
                );
            }
        }

        // Utility: project world to screen
        private Vector2 WorldToScreen(Vector3 world, Matrix4 view, Matrix4 proj, int width, int height)
        {
            var clip = Vector4.TransformRow(new Vector4(world, 1.0f), view * proj);
            if (clip.W == 0) return new Vector2(-1000, -1000);
            var ndc = new Vector3(clip.X, clip.Y, clip.Z) / clip.W;
            float x = (ndc.X * 0.5f + 0.5f) * width;
            float y = (1.0f - (ndc.Y * 0.5f + 0.5f)) * height;
            return new Vector2(x, y);
        }

        // Utility: unproject screen to world on ground (Y=terrain)
        private Vector3 ScreenToWorldOnGround(Vector2 screen, Matrix4 view, Matrix4 proj, int width, int height)
        {
            // Convert screen to NDC
            float x = (2.0f * screen.X) / width - 1.0f;
            float y = 1.0f - (2.0f * screen.Y) / height;
            var ndcNear = new Vector4(x, y, -1.0f, 1.0f);
            var ndcFar = new Vector4(x, y, 1.0f, 1.0f);
            var invVP = Matrix4.Invert(view * proj);
            var worldNear = Vector4.TransformRow(ndcNear, invVP);
            var worldFar = Vector4.TransformRow(ndcFar, invVP);
            if (worldNear.W != 0) worldNear /= worldNear.W;
            if (worldFar.W != 0) worldFar /= worldFar.W;
            var rayOrigin = new Vector3(worldNear.X, worldNear.Y, worldNear.Z);
            var rayDir = Vector3.Normalize(new Vector3(worldFar.X, worldFar.Y, worldFar.Z) - rayOrigin);
            // Ray-plane intersection (Y=terrain)
            float t = 0;
            float yGround = _renderer.GetTerrainHeight(rayOrigin.X, rayOrigin.Z);
            if (Math.Abs(rayDir.Y) > 1e-4)
                t = (yGround - rayOrigin.Y) / rayDir.Y;
            var hit = rayOrigin + rayDir * t;
            hit.Y = _renderer.GetTerrainHeight(hit.X, hit.Z);
            return hit;
        }
    }
}
