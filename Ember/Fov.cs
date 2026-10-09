namespace Ember;

/// <summary>Recursive shadowcasting. Marks Lit and Seen on the level.</summary>
public static class Fov
{
    static readonly int[][] Mult =
    [
        [1, 0, 0, -1, -1, 0, 0, 1],
        [0, 1, -1, 0, 0, -1, 1, 0],
        [0, 1, 1, 0, 0, -1, -1, 0],
        [1, 0, 0, 1, -1, 0, 0, -1],
    ];

    public static void Compute(Level lv, Pos o, int radius)
    {
        Array.Clear(lv.Lit);
        Mark(lv, o);
        for (var oct = 0; oct < 8; oct++)
            Cast(lv, o, 1, 1.0, 0.0, radius, Mult[0][oct], Mult[1][oct], Mult[2][oct], Mult[3][oct]);
    }

    static void Mark(Level lv, Pos p)
    {
        lv.Lit[p.X, p.Y] = true;
        lv.Seen[p.X, p.Y] = true;
    }

    static void Cast(Level lv, Pos o, int row, double start, double end, int radius, int xx, int xy, int yx, int yy)
    {
        if (start < end) return;

        var newStart = 0.0;
        for (var i = row; i <= radius; i++)
        {
            int dx = -i - 1, dy = -i;
            var blocked = false;

            while (dx <= 0)
            {
                dx++;
                var p = new Pos(o.X + dx * xx + dy * xy, o.Y + dx * yx + dy * yy);
                double lSlope = (dx - 0.5) / (dy + 0.5), rSlope = (dx + 0.5) / (dy - 0.5);

                if (start < rSlope) continue;
                if (end > lSlope) break;

                var inBounds = Level.InBounds(p);
                if (inBounds && dx * dx + dy * dy <= radius * radius) Mark(lv, p);

                var wall = !inBounds || lv[p] == Tile.Wall;
                if (blocked)
                {
                    if (wall) { newStart = rSlope; continue; }
                    blocked = false;
                    start = newStart;
                }
                else if (wall && i < radius)
                {
                    blocked = true;
                    Cast(lv, o, i + 1, start, lSlope, radius, xx, xy, yx, yy);
                    newStart = rSlope;
                }
            }

            if (blocked) break;
        }
    }
}
