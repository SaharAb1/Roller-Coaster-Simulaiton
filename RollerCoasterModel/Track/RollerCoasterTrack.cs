using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

namespace RollerCoasterSim.Track
{
    public class RollerCoasterTrack
    {
        private List<Vector3> controlPoints;
        private const int SEGMENTS_PER_CURVE = 20;
        private const float TRACK_WIDTH = 1.0f;
        private const float TRACK_HEIGHT = 0.2f;

        public RollerCoasterTrack()
        {
            // Create a simple loop track
            controlPoints = new List<Vector3>
            {
                new Vector3(0, 0, 0),      // Start
                new Vector3(10, 5, 0),     // First hill
                new Vector3(0, 10, 10),    // Loop
                new Vector3(-10, 5, 0),    // Second hill
                new Vector3(0, 0, 0)       // End
            };
        }

        public Vector3 GetPosition(float t)
        {
            // Ensure t is between 0 and 1
            t = Math.Clamp(t, 0, 1);
            
            // Calculate which curve segment we're on
            float segmentLength = 1.0f / (controlPoints.Count - 1);
            int segment = (int)(t / segmentLength);
            if (segment >= controlPoints.Count - 3) segment = controlPoints.Count - 4; // Clamp to valid range
            float localT = (t - segment * segmentLength) / segmentLength;
            
            // Get control points for this segment
            Vector3 p0 = controlPoints[segment];
            Vector3 p1 = controlPoints[segment + 1];
            Vector3 p2 = controlPoints[(segment + 2) % controlPoints.Count];
            Vector3 p3 = controlPoints[(segment + 3) % controlPoints.Count];
            
            // Calculate position using Catmull-Rom spline
            return CalculateCatmullRomPoint(localT, p0, p1, p2, p3);
        }

        public Vector3 GetDirection(float t)
        {
            // Calculate direction using finite differences
            float delta = 0.001f;
            Vector3 pos1 = GetPosition(t - delta);
            Vector3 pos2 = GetPosition(t + delta);
            return Vector3.Normalize(pos2 - pos1);
        }

        public Vector3 GetNormal(float t)
        {
            // Calculate normal using cross product of direction and up vector
            Vector3 direction = GetDirection(t);
            Vector3 up = Vector3.UnitY;
            return Vector3.Normalize(Vector3.Cross(direction, up));
        }

        private Vector3 CalculateCatmullRomPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            // Catmull-Rom matrix coefficients
            float a0 = -0.5f * t3 + t2 - 0.5f * t;
            float a1 = 1.5f * t3 - 2.5f * t2 + 1.0f;
            float a2 = -1.5f * t3 + 2.0f * t2 + 0.5f * t;
            float a3 = 0.5f * t3 - 0.5f * t2;

            return a0 * p0 + a1 * p1 + a2 * p2 + a3 * p3;
        }

        public List<Vector3> GetTrackPoints()
        {
            List<Vector3> points = new List<Vector3>();
            int totalSegments = (controlPoints.Count - 1) * SEGMENTS_PER_CURVE;
            
            for (int i = 0; i <= totalSegments; i++)
            {
                float t = (float)i / totalSegments;
                points.Add(GetPosition(t));
            }
            
            return points;
        }

        public float GetTrackLength()
        {
            List<Vector3> points = GetTrackPoints();
            float length = 0;
            
            for (int i = 1; i < points.Count; i++)
            {
                length += Vector3.Distance(points[i - 1], points[i]);
            }
            
            return length;
        }
    }
} 