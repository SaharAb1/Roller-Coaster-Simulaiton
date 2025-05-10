# Roller Coaster Simulator

A 3D roller coaster simulation in C# using OpenTK and ImGui.NET.

## Features
- **Spline-based tube track**: Realistic roller coaster track generated as a smooth tube using Catmull-Rom splines.
- **Animated train**: Train moves smoothly along the track, oriented to the spline.
- **Camera controls**: Free, follow, top, and side views. Move with WASD + mouse (hold left button).
- **ImGui.NET UI**: Sidebar for train speed, camera mode, and statistics.
- **Extensible**: Ready for terrain/background, supports, and more.
- **Cross-platform**: Works on macOS, Windows, and Linux (OpenTK).

## Controls
- **WASD + Mouse (hold left button)**: Move/look (Free camera)
- **1**: Free camera
- **2**: Follow train
- **3**: Top view
- **4**: Side view
- **ImGui UI**: Use sidebar to change camera mode and train speed

## How to Build & Run
1. **Install [.NET 6+ SDK](https://dotnet.microsoft.com/download)**
2. **Clone this repo**
3. **Run:**
   ```
   dotnet run
   ```

## Inspiration
- Based on [this blog post](https://www.gamedev.net/blogs/entry/2263600-roller-coaster-tycoon-3d-track-splines/) and similar YouTube demos.

## Roadmap
- [ ] Add terrain/ground mesh
- [ ] Add supports and cross-ties
- [ ] Multiple train cars
- [ ] More track editing and UI features

---

**Enjoy your ride!** 