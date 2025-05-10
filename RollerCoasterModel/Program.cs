// Program.cs
using System;
using System.Globalization;
using OpenTK.Windowing.Desktop;

namespace RollerCoasterSim
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Set culture to invariant to avoid OpenTK Vector3.ToString() crash
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

            var nativeWindowSettings = new NativeWindowSettings()
            {
                Size = new OpenTK.Mathematics.Vector2i(1280, 720),
                Title = "🎉 Roller Coaster Simulator",
                // This is needed to run on macos
                Flags = OpenTK.Windowing.Common.ContextFlags.ForwardCompatible,
            };

            using (var window = new MainWindow(new GameWindowSettings(), nativeWindowSettings))
            {
                window.Run();
            }
        }
    }
}