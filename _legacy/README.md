# Legacy version

This folder keeps the earlier program exactly as it was found, for reference. It is not part of the
modern build.

## Where it comes from

Graph3D started in the mid-1990s as a university project for a 3D computer graphics course. The goal
was to show how 3D images are produced from scratch, without a graphics library: the course covered
shading, transformations (rotate, pan) and display models (points, wireframe, filled faces).
The original was a DOS program in C/C++ for SuperVGA, with direct keyboard access through inline assembly.

What survives is a later, quick port of that program to C# / Windows Forms (.NET 6). The original
C++ is still in [`_Graphics.cs`](_Graphics.cs) as comments (the scene loader, the menu, the keyboard
handling and the rotation keys). This is not the final version of the project: that one was lost, and
it mostly fixed errors rather than adding features.

For an assessment of the project as coursework (coverage of the brief, strengths, weaknesses and an
estimated grade), see [`docs/EVALUATION.md`](../docs/EVALUATION.md).

## What it does

Five display modes, each building on the previous one, as in the original console menu:

| Button | Original menu item |
|--------|--------------------|
| Body   | 1 – points |
| Ciary  | 2 – all edges (wireframe) |
| Plochy | 3 – front-facing faces, constant shading |
| Phong  | 4 – mode 3 plus the painter's algorithm (depth sorting). The "Phong" label was added by the port; it is not Phong shading. |
| Gouraud | 5 – painter's algorithm with Gouraud shading |

Everything is computed by hand: box, torus, B-spline and Bézier surfaces, rotations, face and vertex
normals, back-face culling, depth sorting, flat and Gouraud shading, and the scanline triangle fill.
Identifiers and comments are in Slovak (`body` = points, `hrany` = edges, `plochy` = faces,
`kvader` = box, `kamera` = camera, `koniec` = end).

## Files

| File | |
|------|-|
| `_Graphics.cs` | The whole renderer, plus the original C++ in comments |
| `Form1.cs`, `Form1.Designer.cs`, `Form1.resx`, `Program.cs` | Windows Forms shell added by the port |
| `Graph3D6.0.csproj`, `Graph3D6.0.sln` | Project files of the port (.NET 6) |
| `TOR.DAT` | The scene the program loads: three interlocked tori |
| `data/TOR - close camera.dat` | The same scene with the camera closer (`zk` = 3), found in an old build folder |
| `data/TOR - with box.dat` | Coarser tori plus a rotated box (`kvader`), found in an old build folder |

## Running it

It still builds with a current .NET SDK, but runs on the .NET 6 runtime, which is out of support:

```
dotnet build -c Release Graph3D6.0.csproj
```

The program reads `tor.dat` from the current folder at start-up, so copy `TOR.DAT` next to the
executable (or start it from this folder). Only `kamera`, `kvader` and `toroid` lines are read in this
version; the B-spline and Bézier objects exist only in the commented-out C++ loader.

Known problems of the port: the program can crash when an object leaves the window (no bounds check),
and reading numbers depends on the Windows regional settings (it fails where the decimal separator is
a comma). The modern version in the repository root fixes both.
