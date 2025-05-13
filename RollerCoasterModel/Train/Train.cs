using OpenTK.Mathematics;
using RollerCoasterSim.Track;

namespace RollerCoasterSim.Train
{
    public class Train
    {
        private RollerCoasterTrack track;
        private float speed = 5.0f;
        private float trackParameter = 0.0f;
        private Vector3 position;
        private Vector3 direction;
        private Vector3 normal;

        public Train(RollerCoasterTrack track)
        {
            this.track = track;
            UpdatePosition();
        }

        public void Update(float deltaTime)
        {
            // Update track parameter based on speed and track length
            float trackLength = track.GetTrackLength();
            trackParameter += (speed * deltaTime) / trackLength;
            
            // Wrap around when we complete the track
            if (trackParameter >= 1.0f)
            {
                trackParameter -= 1.0f;
            }
            
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            position = track.GetPosition(trackParameter);
            direction = track.GetDirection(trackParameter);
            normal = track.GetNormal(trackParameter);
        }

        public Vector3 GetPosition() => position;
        public Vector3 GetDirection() => direction;
        public Vector3 GetNormal() => normal;
        public float GetSpeed() => speed;
        public float GetTrackParameter() => trackParameter;

        public void SetSpeed(float newSpeed)
        {
            speed = Math.Clamp(newSpeed, 0.0f, 20.0f);
        }
    }
}
