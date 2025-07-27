using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System; 
using RollerCoasterSim.Objects;

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
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);
            _train.Update((float)args.Time);
            var input = KeyboardState;
            float delta = (float)args.Time;
            if (input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.D1)) _cameraMode = CameraMode.Free;
            if (input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.D2)) _cameraMode = CameraMode.Follow;
            if (input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.D3)) _cameraMode = CameraMode.Top;
            if (input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.D4)) _cameraMode = CameraMode.Side;
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
                    // Tree drag logic
                    var mouse = MouseState;
                    Vector2 mousePos = new Vector2(mouse.X, mouse.Y);
                    if (mouse.IsButtonPressed(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Left))
                    {
                        // Ray pick tree (stub: pick closest in screen space for now)
                        int? picked = null;
                        float minDist = 30f;
                        for (int i = 0; i < _renderer.Trees.Positions.Count; i++)
                        {
                            var world = _renderer.Trees.Positions[i];
                            var screen = WorldToScreen(world, _camera.GetViewMatrix(), Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(70f), Size.X / (float)Size.Y, 0.1f, 100f), Size.X, Size.Y);
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
                            var proj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(70f), Size.X / (float)Size.Y, 0.1f, 100f);
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
            Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(70f),
                Size.X / (float)Size.Y,
                0.1f,
                100f
            );
            _renderer.Draw(view, projection, _train);
            SwapBuffers();
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, Size.X, Size.Y);
        }

        protected override void OnUnload()
        {
            base.OnUnload();
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
