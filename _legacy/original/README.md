# The original DOS program

The C++ source of the university project, as it was written, together with the DOS program built from
it and the scene files it was tested with. It was written in 1995; the file dates (13 October 1999)
come from a later copy.

The source files are kept byte for byte (Slovak identifiers and comments, CRLF line endings).

## Files

| File | |
|------|-|
| `pgraf.cpp` | The program: scene loader, mesh building, the menu, all five display modes and the keyboard loop |
| `deklar.h` | Data types and helpers: face normals (`normalizuj`), torus, B-spline and Bézier surfaces, depth sorting (`Usporiadaj`), vertex normals (`dajnormalybodom`), video mode selection |
| `grafika.exe` | The 16-bit DOS program built from this source |
| `grafika.prj` | Turbo C++ project file |
| `tor.dat` | The three interlocked tori |
| `poku.dat` | One coarse torus |
| `scena.dat`, `ss.dat` | Boxes |
| `smesh.dat` | One B-spline surface |
| `s.dat`, `sc.dat` | Boxes, tori and B-spline surfaces together |

Not included: the SciTech SuperVGA Kit (`svga.h`, `pmode.h`, `debug.h`, `svga.lib`, `pmode.lib`), a
third-party library that the program uses to set the video mode, switch screen pages and draw pixels
and lines. Everything above that level is the project's own code. The kit is linked into
`grafika.exe`, so the program runs without it.

## Running it

`grafika.exe` needs DOS and a VESA-compatible graphics card with a 15-bit (32,768 colour) mode.
DOSBox provides both, so it should run there:

```
grafika
```

1. Type the name of a scene file, for example `tor.dat`.
2. Choose a display mode from the menu:

   | Key | Menu item | |
   |-----|-----------|-|
   | 1 | body | points |
   | 2 | vsetky hrany | all edges |
   | 3 | privratene plochy konstantne tienovane | front-facing faces, constant shading |
   | 4 | (3) plus maliarov algoritmus | mode 3 plus the painter's algorithm |
   | 5 | Privratene plochy maliarov algoritmus Gouraudovo tienovanie | painter's algorithm with Gouraud shading |
   | 6 | koniec | quit |

3. Choose a video mode from the list of 15-bit modes the card offers (`0`, `1`, …).

| Keys | Action |
|------|--------|
| ↑ ↓ | Rotate about X |
| Home, Page Up | Rotate about Y |
| ← → | Rotate about Z |
| W A S D | Pan |
| F, G | Zoom in, zoom out |
| Esc | Back to the menu |

## Building it

It was built with Borland Turbo C++ 3 for DOS and the SciTech SuperVGA Kit (16-bit Borland
libraries), with the kit in `..\SVGAKIT`. See `grafika.prj` for the paths.

## Compared with the C# port

The C# port in [`_legacy/`](../) was made later from this program. It differs in these points:

- The port labels mode 4 "Phong". The original menu has no Phong shading; mode 4 is constant shading
  plus depth sorting.
- The port scales mode 3 by `(long)(110/zk)`; the original uses `100/zk` in every mode.
- The original keeps screen coordinates as floating-point numbers while filling triangles; the port
  rounds them to whole pixels first.
- The original loads B-spline and Bézier surfaces; in the port that part of the loader is commented out.
- The original asks for the scene file; the port always reads `tor.dat`.

`tor.dat` here uses a ring detail of 14 × 15 for the first torus and 14 × 14 for the other two. The
`TOR.DAT` files elsewhere in the repository use 14 × 15 for all three.
