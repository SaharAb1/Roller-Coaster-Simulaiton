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
        private FreeCamera camera;
        private Vector2 lastMousePos;
        private bool firstMove = true;

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

            CursorGrabbed = true;
            CursorState = CursorState.Grabbed;

            camera = new FreeCamera(new Vector3(0, 0, 3));

            Renderer.Init();
        }


        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Matrix4 view = camera.GetViewMatrix();
            Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(70f), Size.X / (float)Size.Y, 0.1f, 100f);

            Renderer.Draw(view, projection);
            GL.BindVertexArray(0);

            SwapBuffers();
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            if (!IsFocused)
                return;

            var input = KeyboardState;
            float delta = (float)args.Time;
            Vector3 direction = Vector3.Zero;

            if (input.IsKeyDown(Keys.W))
                direction += camera.Front;
            if (input.IsKeyDown(Keys.S))
                direction -= camera.Front;
            if (input.IsKeyDown(Keys.A))
                direction -= camera.Right;
            if (input.IsKeyDown(Keys.D))
                direction += camera.Right;

            if (direction != Vector3.Zero)
                camera.Move(Vector3.Normalize(direction), delta);
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            base.OnMouseMove(e);

            if (firstMove)
            {
                lastMousePos = e.Position;
                firstMove = false;
                return;
            }

            var delta = e.Delta;
            camera.Look(delta.X, delta.Y);
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, Size.X, Size.Y);
        }
    }
}