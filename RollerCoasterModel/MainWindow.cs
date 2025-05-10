using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System;

namespace RollerCoasterSim
{
    public enum CameraMode { Free, Follow, Top, Side }

    public class MainWindow : GameWindow
    {
        private RollerCoasterSim.Track.RollerCoasterTrack _track = null!;
        private RollerCoasterSim.Train _train = null!;
        private RollerCoasterSim.Camera.FreeCamera _camera = null!;
        private CameraMode _cameraMode = CameraMode.Free;

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
            _train = new RollerCoasterSim.Train(_track);
            _train.SetSpeed(5.0f);
            _camera = new RollerCoasterSim.Camera.FreeCamera(new Vector3(0, 10, 30));
            RollerCoasterSim.Renderer.Init();
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
                    if (MouseState.IsButtonDown(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Left))
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
            RollerCoasterSim.Renderer.Draw(view, projection, _train);
            SwapBuffers();
        }
    }
}
