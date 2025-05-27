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

            // First hill with some Z variation
            _controlPoints.Add(new Vector3(10, 5, 5));

            // Loop with Z variation
            _controlPoints.Add(new Vector3(20, 10, 10));
            _controlPoints.Add(new Vector3(30, 10, -5));
            _controlPoints.Add(new Vector3(40, 5, -10));

            // Second hill with Z variation
            _controlPoints.Add(new Vector3(50, 8, -5));

            // Return to station with a curve
            _controlPoints.Add(new Vector3(60, 0, 0));
            _controlPoints.Add(new Vector3(30, 0, 0));
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
            t = t % 1.0f;
            int numSegments = _controlPoints.Count - 1;
            float segmentLength = 1.0f / numSegments;
            int segment = (int)(t / segmentLength);
            Vector3 p0 = _controlPoints[segment];
            Vector3 p1 = _controlPoints[(segment + 1) % _controlPoints.Count];
            return Vector3.Normalize(p1 - p0);
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
    Vector3 tangent = GetDirection(t);
    Vector3 binormal = Vector3.UnitY;

    // חישוב נורמל אורכי למשטח (up אמיתי)
    Vector3 normal = Vector3.Cross(tangent, binormal).Normalized();
    normal = Vector3.Cross(normal, tangent).Normalized();

    return normal;
}


        public List<Vector3> GetTrackPoints()
        {
            return _controlPoints;
        }

        public float GetTrackLength()
        {
            float length = 0;
            for (int i = 0; i < _controlPoints.Count; i++)
            {
                Vector3 p0 = _controlPoints[i];
                Vector3 p1 = _controlPoints[(i + 1) % _controlPoints.Count];
                length += Vector3.Distance(p0, p1);
            }
            return length;
        }
    }
} 