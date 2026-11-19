# Evaluation of the original Graph3D project

An assessment of the mid-1990s university project kept in [`_legacy/`](../_legacy/). Its purpose was
to prove an understanding of how 3D images are produced, without OpenGL or any other graphics library.

| | |
|-|-|
| **Course** | 3D computer graphics |
| **Assignment focus** | Shading, transformations (pan, rotate), display models (points, wireframe, filled faces) |
| **Original platform** | DOS, C/C++, SuperVGA |
| **Version reviewed** | The last surviving version, a pre-final one; the final version mainly fixed errors |
| **Source** | [`_Graphics.cs`](../_legacy/_Graphics.cs): a C# port, with the original C++ preserved as comments |

## Verdict

**Estimated grade: A**

Measured against its brief, the project covers every required topic, all written by hand, and adds
spline surfaces that go beyond what most such projects include. Its weak points are robustness and
code quality, not understanding.

## Measured against the brief

| Topic | Status | How it is covered |
|-------|--------|-------------------|
| Display models | Covered | Five modes that build on each other: points, wireframe, front-facing faces, faces sorted with the painter's algorithm, and Gouraud-shaded faces. |
| Shading | Covered | Flat and Gouraud shading from face and vertex normals, with a per-face flag that keeps boxes sharp-edged and curved surfaces smooth. |
| Transformations | Covered | Interactive rotation about all three axes, pan and zoom. Each rotation is a rotation matrix written out as formulas. |

Perspective projection and clipping were not part of the assignment. The program uses a simple
parallel projection, which is enough to display the scene. The scene format already reserves a camera
angle (`kamera.uhol`) that the program reads but does not use, which suggests a perspective view was
planned for later.

## The display modes

The original console menu offered five modes, in an order that works like a lesson plan, each adding
one technique to the previous:

1. Points
2. All edges (wireframe)
3. Front-facing faces with constant shading
4. Mode 3 plus the painter's algorithm (depth sorting)
5. Painter's algorithm with Gouraud shading

The Windows Forms port labels the button for mode 4 "Phong". The program does not implement Phong
shading; mode 4 is constant shading with depth sorting.

## Coverage of the rendering pipeline

Each stage in the order a frame passes through it.

| Stage | Status | Notes |
|-------|--------|-------|
| 1. Modelling | Done | Box, torus, B-spline and Bézier surfaces |
| 2. Transformations | Done | Hand-written rotations about Z, Y and X; pan and zoom |
| 3. Projection | Outside the brief | Slanted parallel projection |
| 4. Back-face culling | Partial | Works, but assumes a view straight along Z |
| 5. Clipping | Outside the brief | Off-screen pixels are left to the graphics library |
| 6. Visibility | Partial | Painter's algorithm; no z-buffer |
| 7. Normals | Done | Face normals by cross product, averaged per vertex |
| 8. Shading | Done | Flat and Gouraud; one light at the viewer |
| 9. Rasterization | Done | Own scanline triangle fill, with precision issues |
| 10. Display | Done | Page flipping for flicker-free animation |

## What the code demonstrates

| Topic | Evidence in the code |
|-------|----------------------|
| Mesh representation | Vertices, edges and triangles kept as separate lists (`body`, `hrany`, `plochy`) |
| Parametric shapes | Box, torus, bicubic B-spline (5×5 control net) and Bézier surface (7×7, four joined patches) |
| Transformations | Each rotation is one rotation matrix written out with sin/cos; normals are rotated together with the points. Pan and zoom through the camera offsets. |
| Projection | Parallel projection `x + z/4`, `y + z/4`, scaled by `100/zk` |
| Back-face culling | Faces are drawn only when `norz > 0` |
| Hidden surfaces | Painter's algorithm: sort by depth (`Usporiadaj`), draw back to front |
| Face normals | Cross product of two edges, then normalised (`normalizuj`) |
| Vertex normals | Average of the neighbouring face normals (`dajnormalybodom`) |
| Flat shading | Brightness from the angle between the face and a light at the viewer |
| Gouraud shading | Brightness per vertex, blended along the edges and then along each row |
| Sharp and smooth edges | Per-face flag `zaobli`: boxes stay sharp, curved shapes are smooth |
| Triangle filling | Corners sorted by height, the triangle split into two flat-edged halves, filled row by row |
| Smooth animation | Drawing to a hidden screen page, then swapping pages (`vpage` / `apage`) |
| Keyboard control | Reading the keyboard port `60h` directly with inline assembly, typical of 1990s DOS |

### Spline surfaces

The spline surfaces are the strongest part of the project. The two-direction formula is evaluated by
hand (the `Vynasob*` functions) with the correct uniform B-spline weights, divided by 36: that is 1/6
for each direction, which is right. The Bézier surface is four cubic patches sharing edges, and the
sampling skips the rows that would otherwise be duplicated at the seams. Most basic courses stop at
boxes and spheres.

The torus routine closely follows the well-known torus example from the OpenGL Programming Guide (the
"Red Book"); a loop over `k` whose first pass is overwritten shows where it came from. Only the torus
formula is shared, so this does not weaken the project.

## Weaknesses

In order of importance for this assignment.

1. **The painter's algorithm fails on the program's own scene.** Sorting whole triangles by average
   depth cannot handle shapes that pass through each other, and `TOR.DAT` is three linked rings. A
   z-buffer would solve it, at a memory cost that was significant under real-mode DOS, so the choice is
   understandable for the time. The sort is an insertion sort into a fixed array of 1,300 entries: slow,
   and able to overflow.

2. **Transformations are formulas, not composed matrices.** Each rotation is a rotation matrix written
   out by hand, which shows the mathematics, but matrices are never stored or multiplied together. Each
   key press rotates the stored coordinates in place (cos 0.1 = 0.995, sin 0.1 = 0.09983), so shapes
   slowly drift over a long session. Keeping the original model plus one accumulated orientation matrix
   would avoid the drift and demonstrate matrix composition, which the brief names.

3. **Culling does not match the projection.** The test assumes the viewer looks straight along Z, but
   the slanted projection looks along roughly (−¼, −¼, 1). Faces near an object's outline can be
   wrongly hidden or wrongly shown.

4. **Minimal lighting model.** One light at the viewer (brightness = `norz`), no ambient light, no
   highlights and one colour ramp. Enough to show flat and Gouraud shading, but little beyond
   "brightness = cos θ".

5. **Precision problems when filling triangles.** Coordinates are cut to whole pixels first, edges are
   recalculated with a division on every row instead of being stepped, and there is no rule for pixels
   exactly on a shared edge, so thin gaps can appear between neighbouring triangles. Triangles one pixel
   tall or less are skipped.

6. **Fragile surface topology.** Spline surfaces are sampled with `u += 0.2`, which depends on
   floating-point addition landing exactly on 1.0. Triangles are then built from hard-coded vertex
   numbers such as `65 + ii + j*5`. One sample more or less and the mesh is silently wrong.

7. **No bounds checks.** Arrays have fixed sizes (10,000) and are never checked, so a larger scene
   overwrites memory instead of failing with a message.

8. **Inconsistent scale between modes.** Mode 3 scales by `(long)(110/zk)`, using 110 instead of 100
   and rounding to a whole number, while the other modes use `100/zk`. The picture changes size when
   switching to mode 3.

## Code quality

The code is typical student C/C++ of its time:

- global variables throughout, and one long `main` held together with `goto`;
- heavy copy-paste: six near-identical `Vynasob*` functions, rotation loops repeated for each shape
  type, and the triangle fill written four times;
- Slovak identifiers and comments.

It works, but would be hard to extend; clean architecture was not expected from this kind of
assignment. A comment on the vertex-normal pass, *"toto je straaasne pomale"* ("this is terribly
slow"), shows the author was aware of its O(V·F) cost.

## Issues specific to the C# port

The Windows Forms port of the DOS program adds problems of its own:

- the button for mode 4 is labelled "Phong";
- `DirectBitmap.SetPixel` has no bounds check, so zooming in can crash the program;
- the sharp faces in mode 5 mix `SetPixel` with `DrawLine` using a leftover pen colour;
- reading the `.dat` file depends on the Windows regional settings and fails where the decimal
  separator is a comma;
- the B-spline and Bézier objects cannot be loaded, because that part of the loader remains in the
  commented-out C++.

## Conclusion

For a course on shading, transformations and display models, completed without a graphics library,
the project deserves an A. Every topic in the brief is implemented by hand and works: five display
models from points to Gouraud shading, flat and Gouraud shading with sharp and smooth edges, and
interactive rotation, pan and zoom. The spline surfaces go beyond what most such projects include.

A strict examiner could mark it down for the depth-sorting failures on its own linked-rings scene and
for applying rotations as formulas rather than composed matrices. Two questions that would test the
author's understanding:

- Why do the tori sometimes overlap wrongly in mode 4, and how would a z-buffer fix it?
- How would you combine the rotations into one matrix, and why would that stop the shapes drifting?
