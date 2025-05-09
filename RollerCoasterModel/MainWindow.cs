using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using RollerCoasterSim.Camera;

namespace RollerCoasterSim
{
    public class MainWindow : GameWindow
    {
        private UICursor _uiCursor;
        private FreeCamera camera;
        private bool isDragging = false;

        public MainWindow(int width, int height, string title)
            : base(GameWindowSettings.Default, new NativeWindowSettings()
            {
                Size = new Vector2i(width, height),
                Title = title,
                Flags = ContextFlags.ForwardCompatible
            })
        {
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(Color4.CornflowerBlue);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            CursorState = CursorState.Normal;

            camera = new FreeCamera(new Vector3(0, 0, 3));
            _uiCursor = new UICursor("Assets/UI/hand_cursor.png");

            Renderer.Init();
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Matrix4 view = camera.GetViewMatrix();
            Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(70f), Size.X / (float)Size.Y, 0.1f, 100f);

            Renderer.Draw(view, projection);

            if (isDragging)
            {
                var mouse = MouseState.Position;
                _uiCursor.Draw(mouse, new Vector2(Size.X, Size.Y));
            }

            SwapBuffers();
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            if (!IsFocused)
                return;

            var input = KeyboardState;
            float scroll = MouseState.ScrollDelta.Y;
            if (scroll != 0)
                camera.Zoom(scroll);

            // Optional: allow WASD only when dragging
            if (isDragging)
            {
                float delta = (float)args.Time;
                Vector3 direction = Vector3.Zero;

                if (input.IsKeyDown(Keys.W)) direction += camera.Front;
                if (input.IsKeyDown(Keys.S)) direction -= camera.Front;
                if (input.IsKeyDown(Keys.A)) direction -= camera.Right;
                if (input.IsKeyDown(Keys.D)) direction += camera.Right;

                if (direction != Vector3.Zero)
                    camera.Move(Vector3.Normalize(direction), delta);
            }
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            if (isDragging)
            {
                camera.Look(e.Delta.X, -e.Delta.Y); // Invert Y if needed
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.Button == MouseButton.Left)
            {
                isDragging = true;
                CursorState = CursorState.Hidden;
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (e.Button == MouseButton.Left)
            {
                isDragging = false;
                CursorState = CursorState.Normal;
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            camera.Zoom(e.OffsetY);
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, Size.X, Size.Y);
        }
    }
}
