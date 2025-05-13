using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

namespace RollerCoasterSim.Track
{
    public class RollerCoasterTrack
    {
        private List<Vector3> _controlPoints;
        private float _trackLength;
        private const int SEGMENTS_PER_CURVE = 20;
        private const float TRACK_WIDTH = 1.0f;
        private const float TRACK_HEIGHT = 0.2f;

        public RollerCoasterTrack()
        {
            _controlPoints = new List<Vector3>();
            CreateRealisticTrack();
        }

        private void CreateRealisticTrack()
        {
            // Station (start/end)
            _controlPoints.Add(new Vector3(0, 0, 0)); // Station

            // First hill
            _controlPoints.Add(new Vector3(10, 5, 0));

            // Loop
            _controlPoints.Add(new Vector3(20, 10, 0));
            _controlPoints.Add(new Vector3(30, 10, 0));
            _controlPoints.Add(new Vector3(40, 5, 0));

            // Second hill
            _controlPoints.Add(new Vector3(50, 8, 0));

            // Return to station
            _controlPoints.Add(new Vector3(60, 0, 0));
            _controlPoints.Add(new Vector3(0, 0, 0)); // Back to station
        }

        public Vector3 GetPosition(float t)
        {
            t = t % 1.0f;
            int numSegments = _controlPoints.Count - 1;
            float segmentLength = 1.0f / numSegments;
            int segment = (int)(t / segmentLength);
            float localT = (t - segment * segmentLength) / segmentLength;
            Vector3 p0 = _controlPoints[segment];
            Vector3 p1 = _controlPoints[(segment + 1) % _controlPoints.Count];
            return Vector3.Lerp(p0, p1, localT);
        }

        public Vector3 GetDirection(float t)
        {
            float delta = 0.01f;
            Vector3 pos1 = GetPosition(t);
            Vector3 pos2 = GetPosition((t + delta) % 1.0f);
            return Vector3.Normalize(pos2 - pos1);
        }

        public float GetBankAngle(float t)
        {
            // Simple banking: bank into turns
            Vector3 dir = GetDirection(t);
            float angle = MathHelper.RadiansToDegrees((float)System.Math.Atan2(dir.X, dir.Z));
            return angle * 0.5f;
        }

        public Vector3 GetNormal(float t)
        {
            // Calculate normal using cross product of direction and up vector
            Vector3 direction = GetDirection(t);
            Vector3 up = Vector3.UnitY;
            return Vector3.Normalize(Vector3.Cross(direction, up));
        }

        public List<Vector3> GetTrackPoints()
        {
            return _controlPoints;
        }

        public float GetTrackLength()
        {
            return _trackLength;
        }
    }
} 