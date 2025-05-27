using OpenTK.Mathematics;
using RollerCoasterSim.Track;
using System.Collections.Generic;

namespace RollerCoasterSim.Train
{
    public class Train
    {
        private RollerCoasterTrack track;
        private List<TrainCar> cars;
        private float speed = 50.0f;
        private const float CAR_SPACING = 0.02f; // Reduced from 0.05f to 0.02f (2% of track length)

        public Train(RollerCoasterTrack track, int numCars = 3)
        {
            this.track = track;
            cars = new List<TrainCar>();
            
            // Create cars with initial spacing
            float trackLength = track.GetTrackLength();
            float spacing = CAR_SPACING * trackLength;
            
            for (int i = 0; i < numCars; i++)
            {
                float initialParam = (i * spacing) / trackLength;
                cars.Add(new TrainCar(track, initialParam));
            }
        }

        public void Update(float deltaTime)
        {
            foreach (var car in cars)
            {
                car.Update(deltaTime);
            }
        }

        public Vector3 GetPosition() => cars[0].GetPosition();
        public Vector3 GetDirection() => cars[0].GetDirection();
        public Vector3 GetNormal() => cars[0].GetNormal();
        public float GetTrackParameter() => cars[0].GetTrackParameter();
        public void SetSpeed(float newSpeed)
        {
            speed = newSpeed;
            foreach (var car in cars)
            {
                car.SetSpeed(newSpeed);
            }
        }

        public List<TrainCar> GetCars() => cars;
    }
}
