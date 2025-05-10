using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace RollerCoasterSim.Camera
{
    public class FreeCamera
    {
        private Vector3 _position;
        private Vector3 _front;
        private Vector3 _up;
        private Vector3 _right;
        private Vector3 _worldUp;

        private float _yaw;
        private float _pitch;
        private float _movementSpeed;
        private float _mouseSensitivity;

        public FreeCamera(Vector3 position)
        {
            _position = position;
            _worldUp = Vector3.UnitY;
            _yaw = -90.0f;
            _pitch = 0.0f;
            _front = Vector3.UnitZ;
            _movementSpeed = 2.5f;
            _mouseSensitivity = 0.1f;

            UpdateVectors();
        }

        public void Look(float xOffset, float yOffset, bool constrainPitch = true)
        {
            xOffset *= _mouseSensitivity;
            yOffset *= _mouseSensitivity;

            _yaw += xOffset;
            _pitch += yOffset;

            if (constrainPitch)
            {
                _pitch = Math.Clamp(_pitch, -89.0f, 89.0f);
            }

            UpdateVectors();
        }

        public void Move(Vector3 direction, float deltaTime)
        {
            float velocity = _movementSpeed * deltaTime;
            _position += direction * velocity;
        }

        public void Zoom(float amount)
        {
            float newDistance = (_position + _front * amount).Length;
            if (newDistance > 2f && newDistance < 20f)
            {
                _position += _front * amount;
            }
        }

        public Matrix4 GetViewMatrix()
        {
            return Matrix4.LookAt(_position, _position + _front, _up);
        }

        private void UpdateVectors()
        {
            Vector3 front;
            front.X = (float)Math.Cos(MathHelper.DegreesToRadians(_yaw)) * (float)Math.Cos(MathHelper.DegreesToRadians(_pitch));
            front.Y = (float)Math.Sin(MathHelper.DegreesToRadians(_pitch));
            front.Z = (float)Math.Sin(MathHelper.DegreesToRadians(_yaw)) * (float)Math.Cos(MathHelper.DegreesToRadians(_pitch));
            _front = Vector3.Normalize(front);

            _right = Vector3.Normalize(Vector3.Cross(_front, _worldUp));
            _up = Vector3.Normalize(Vector3.Cross(_right, _front));
        }

        public void SetFollowMode(Vector3 trainPosition, Vector3 trainDirection)
        {
            _position = trainPosition - trainDirection * 5.0f + Vector3.UnitY * 2.0f;
            _front = Vector3.Normalize(trainPosition - _position);
            UpdateVectors();
        }

        public void SetTopView(Vector3 trainPosition)
        {
            _position = trainPosition + Vector3.UnitY * 20.0f;
            _front = -Vector3.UnitY;
            UpdateVectors();
        }

        public void SetSideView(Vector3 trainPosition, Vector3 trainDirection)
        {
            Vector3 right = Vector3.Normalize(Vector3.Cross(trainDirection, Vector3.UnitY));
            _position = trainPosition + right * 10.0f;
            _front = Vector3.Normalize(trainPosition - _position);
            UpdateVectors();
        }

        public Vector3 Position => _position;
        public Vector3 Front => _front;
        public Vector3 Up => _up;
    }
} 