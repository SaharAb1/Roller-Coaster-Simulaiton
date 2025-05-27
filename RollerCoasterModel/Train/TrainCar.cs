using OpenTK.Mathematics;
using RollerCoasterSim.Track;

namespace RollerCoasterSim.Train
{
    public class TrainCar
    {
        private RollerCoasterTrack track;
        private float speed;
        private float trackParameter;
        private Vector3 position;
        private Vector3 direction;
        private Vector3 normal;

        public TrainCar(RollerCoasterTrack track, float initialTrackParameter = 0.0f)
        {
            this.track = track;
            this.trackParameter = initialTrackParameter;
            this.speed = 30.0f;
            UpdatePosition();
        }

        public void Update(float deltaTime)
        {
            float trackLength = track.GetTrackLength();
            float adjustedSpeed = speed;
            if (trackParameter < 0.1f || trackParameter > 0.9f)
            {
                adjustedSpeed *= 0.5f;
            }
            
            trackParameter += (adjustedSpeed * deltaTime) / trackLength;

            if (trackParameter >= 1.0f)
                trackParameter -= 1.0f;

            UpdatePosition();
        }

        private void UpdatePosition()
        {
            position = track.GetPosition(trackParameter);
            direction = track.GetDirection(trackParameter);
            normal = track.GetNormal(trackParameter);
            
            position += normal * 0.1f;
        }

        public Vector3 GetPosition() => position;
        public Vector3 GetDirection() => direction;
        public Vector3 GetNormal() => normal;
        public float GetTrackParameter() => trackParameter;
        public void SetSpeed(float newSpeed) => speed = newSpeed;
    }
} 