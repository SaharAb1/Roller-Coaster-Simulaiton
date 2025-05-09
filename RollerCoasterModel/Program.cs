using System;

namespace RollerCoasterSim
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            using (MainWindow window = new MainWindow(1280, 720, "🎢 Roller Coaster Simulator"))
            {
                window.Run();
            }
        }
    }
}
