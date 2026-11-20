# Legacy version

This folder keeps the earlier versions of the program exactly as they were found, for reference. They
are not part of the modern build.

## Where it comes from

Graph3D started in 1995 as a university project for a 3D computer graphics course. The goal
was to show how 3D images are produced from scratch, without a graphics library: the course covered
shading, transformations (rotate, pan) and display models (points, wireframe, filled faces).

Two versions survive:

- [`original/`](original/): the original DOS program in C++ for 15-bit colour SuperVGA, with direct
  keyboard access through inline assembly. Source, the built program and its scene files; see its
  [README](original/README.md).
- This folder: a later, quick port of that program to C# / Windows Forms (.NET 6). Parts of the
  original C++ are still in [`_Graphics.cs`](_Graphics.cs) as comments (the scene loader, the menu,
  the keyboard handling and the rotation keys).

For an assessment of the project as coursework (coverage of the brief, strengths, weaknesses and an
estimated grade), see [`docs/EVALUATION.md`](../docs/EVALUATION.md).

## What the port does

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
| `original/` | The original DOS program: C++ source, `grafika.exe` and scene files |
| `_Graphics.cs` | The whole renderer of the port, plus parts of the original C++ in comments |
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
reading numbers depends on the Windows regional settings (it fails where the decimal separator is
a comma), and mode 3 draws the scene at a slightly different size (it scales by 110 instead of 100).
The modern version in the repository root fixes all three.
