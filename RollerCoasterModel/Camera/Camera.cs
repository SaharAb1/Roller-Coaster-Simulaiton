using OpenTK.Mathematics;

namespace RollerCoasterSim.Camera
{
    public class FreeCamera
    {
        public Vector3 Position;
        public Vector3 Front = -Vector3.UnitZ;
        public Vector3 Up = Vector3.UnitY;
        public Vector3 Right = Vector3.UnitX;

        private float pitch;
        private float yaw = -90f;

        private float speed = 3.0f;
        private float sensitivity = 0.2f;

        public FreeCamera(Vector3 position)
        {
            Position = position;
            UpdateVectors();
        }

        public void Move(Vector3 direction, float deltaTime)
        {
            Position += direction * speed * deltaTime;
        }

        public void Look(float deltaX, float deltaY)
        {
            yaw += deltaX * sensitivity;
            pitch -= deltaY * sensitivity;

            pitch = MathHelper.Clamp(pitch, -89f, 89f);
            UpdateVectors();
        }

        public Matrix4 GetViewMatrix()
        {
            return Matrix4.LookAt(Position, Position + Front, Up);
        }

        private void UpdateVectors()
        {
            Vector3 front;
            front.X = MathF.Cos(MathHelper.DegreesToRadians(yaw)) * MathF.Cos(MathHelper.DegreesToRadians(pitch));
            front.Y = MathF.Sin(MathHelper.DegreesToRadians(pitch));
            front.Z = MathF.Sin(MathHelper.DegreesToRadians(yaw)) * MathF.Cos(MathHelper.DegreesToRadians(pitch));
            Front = Vector3.Normalize(front);

            Right = Vector3.Normalize(Vector3.Cross(Front, Vector3.UnitY));
            Up = Vector3.Normalize(Vector3.Cross(Right, Front));
        }
    }
}
