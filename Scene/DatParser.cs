using System.Globalization;
using System.Numerics;

namespace Graph3D.Scene;

public sealed class DatFormatException(string message) : Exception(message);

/// <summary>
/// Reads the scene format used by the original program. The file is a whitespace separated
/// stream of tokens (line breaks don't matter), keywords are case-insensitive and numbers always
/// use a decimal point, regardless of the Windows regional settings.
/// <code>
/// kamera  xk yk zk angle
/// kvader  x y z  dx dy dz  xangle yangle zangle
/// toroid  x y z  rin rout  xdetail ydetail  xangle yangle zangle
/// meshbs  x y z  xangle yangle zangle  (5×5 control points "x y z", row by row)
/// meshbe  x y z  xangle yangle zangle  (7×7 control points "x y z", row by row)
/// farba   r g b                           (0–255; colour of all following objects)
/// koniec                                  (optional end marker)
/// </code>
/// Text after <c>#</c> on a line is a comment. Objects before the first <c>farba</c> use the viewer's default colour.
/// </summary>
public static class DatParser
{
    private const int MaxSegments = 512;

    public static SceneDescription Load(string path) =>
        Parse(File.ReadAllText(path), Path.GetFileName(path));

    public static SceneDescription Parse(string text, string sourceName = "<input>")
    {
        var reader = new TokenReader(text, sourceName);
        CameraDef? camera = null;
        var primitives = new List<Primitive>();
        var color = Color.Empty;

        while (reader.TryNext(out var keyword))
        {
            switch (keyword.Text.ToLowerInvariant())
            {
                case "farba":
                case "color":
                case "colour":
                    color = Color.FromArgb(reader.Channel("red"), reader.Channel("green"), reader.Channel("blue"));
                    break;

                case "kamera":
                    camera = new CameraDef(
                        reader.Float("camera x"), reader.Float("camera y"),
                        reader.Float("camera distance"), reader.Float("camera angle"));
                    break;

                case "kvader":
                {
                    var position = reader.Vector("box position");
                    var size = reader.Vector("box size");
                    var rotation = reader.Vector("box rotation");
                    if (size.X <= 0 || size.Y <= 0 || size.Z <= 0)
                        throw reader.Error(keyword, "box size must be positive");
                    primitives.Add(new BoxDef(position, rotation, size) { Color = color });
                    break;
                }

                case "toroid":
                {
                    var position = reader.Vector("torus position");
                    float tube = reader.Float("torus inner radius");
                    float ring = reader.Float("torus outer radius");
                    int tubeSegments = reader.Int("torus x detail");
                    int ringSegments = reader.Int("torus y detail");
                    var rotation = reader.Vector("torus rotation");
                    if (tubeSegments is < 3 or > MaxSegments || ringSegments is < 3 or > MaxSegments)
                        throw reader.Error(keyword, $"torus detail must be between 3 and {MaxSegments}");
                    primitives.Add(new TorusDef(position, rotation, tube, ring, tubeSegments, ringSegments) { Color = color });
                    break;
                }

                case "meshbs":
                    primitives.Add(new BSplinePatchDef(
                        reader.Vector("B-spline position"), reader.Vector("B-spline rotation"),
                        reader.ControlGrid(BSplinePatchDef.GridSize, "B-spline control point")) { Color = color });
                    break;

                case "meshbe":
                    primitives.Add(new BezierPatchDef(
                        reader.Vector("Bézier position"), reader.Vector("Bézier rotation"),
                        reader.ControlGrid(BezierPatchDef.GridSize, "Bézier control point")) { Color = color });
                    break;

                case "koniec":
                case "end":
                    return new SceneDescription(camera, primitives);

                default:
                    throw reader.Error(keyword, $"unknown object '{keyword.Text}'");
            }
        }

        return new SceneDescription(camera, primitives);
    }

    private readonly record struct Token(string Text, int Line);

    private sealed class TokenReader
    {
        private readonly List<Token> tokens = [];
        private readonly string source;
        private int position;

        public TokenReader(string text, string source)
        {
            this.source = source;
            var lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                int comment = line.IndexOf('#');
                if (comment >= 0)
                    line = line[..comment];
                foreach (var part in line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
                    tokens.Add(new Token(part, i + 1));
            }
        }

        public bool TryNext(out Token token)
        {
            if (position < tokens.Count)
            {
                token = tokens[position++];
                return true;
            }
            token = default;
            return false;
        }

        private Token Next(string expected)
        {
            if (!TryNext(out var token))
                throw new DatFormatException($"{source}: unexpected end of file, expected {expected}.");
            return token;
        }

        public DatFormatException Error(Token token, string message) =>
            new($"{source}, line {token.Line}: {message}.");

        public float Float(string what)
        {
            var token = Next(what);
            if (!double.TryParse(token.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
                || !double.IsFinite(value))
                throw Error(token, $"expected a number for {what}, found '{token.Text}'");
            return (float)value;
        }

        public int Int(string what)
        {
            var token = Next(what);
            if (int.TryParse(token.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                return value;
            if (double.TryParse(token.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double d)
                && d == Math.Floor(d) && Math.Abs(d) < int.MaxValue)
                return (int)d;
            throw Error(token, $"expected a whole number for {what}, found '{token.Text}'");
        }

        public int Channel(string what)
        {
            int value = Int($"colour {what}");
            if (value is < 0 or > 255)
                throw Error(tokens[position - 1], $"colour {what} must be between 0 and 255");
            return value;
        }

        public Vector3 Vector(string what) =>
            new(Float(what + " (x)"), Float(what + " (y)"), Float(what + " (z)"));

        /// <summary>Reads n×n points. As in the original, the file lists rows (k) of columns (l) and they are stored as [l, k].</summary>
        public Vector3[,] ControlGrid(int n, string what)
        {
            var grid = new Vector3[n, n];
            for (int k = 0; k < n; k++)
                for (int l = 0; l < n; l++)
                    grid[l, k] = Vector($"{what} [{k},{l}]");
            return grid;
        }
    }
}
