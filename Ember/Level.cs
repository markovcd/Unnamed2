namespace Ember;

public enum Tile : byte { Wall, Floor, Stairs }

public readonly record struct Rect(int X, int Y, int W, int H)
{
    public Pos Center => new(X + W / 2, Y + H / 2);

    public bool Touches(Rect o, int pad) =>
        X - pad < o.X + o.W && o.X - pad < X + W && Y - pad < o.Y + o.H && o.Y - pad < Y + H;

    public IEnumerable<Pos> Cells()
    {
        for (var y = Y; y < Y + H; y++)
            for (var x = X; x < X + W; x++)
                yield return new Pos(x, y);
    }
}

public sealed class Level
{
    public const int Width = 64, Height = 24;

    public Tile[,] Tiles { get; } = new Tile[Width, Height];
    public bool[,] Seen { get; } = new bool[Width, Height];
    public bool[,] Lit { get; } = new bool[Width, Height];
    public List<Rect> Rooms { get; } = [];
    public Pos Start { get; private set; }
    public Pos Exit { get; private set; }

    public static bool InBounds(Pos p) => p.X >= 0 && p.Y >= 0 && p.X < Width && p.Y < Height;

    public Tile this[Pos p] => InBounds(p) ? Tiles[p.X, p.Y] : Tile.Wall;

    public bool Walkable(Pos p) => this[p] != Tile.Wall;

    public static Level Generate(Random rng)
    {
        var lv = new Level();

        for (var tries = 0; tries < 300 && lv.Rooms.Count < 10; tries++)
        {
            var w = rng.Next(5, 13);
            var h = rng.Next(4, 8);
            var room = new Rect(rng.Next(1, Width - w - 1), rng.Next(1, Height - h - 1), w, h);
            if (lv.Rooms.Any(o => o.Touches(room, 1))) continue;

            foreach (var c in room.Cells()) lv.Tiles[c.X, c.Y] = Tile.Floor;
            lv.Rooms.Add(room);
        }

        for (var i = 1; i < lv.Rooms.Count; i++)
            lv.Tunnel(lv.Rooms[i - 1].Center, lv.Rooms[i].Center, rng);

        lv.Start = lv.Rooms[0].Center;
        lv.Exit = lv.Rooms[^1].Center;
        lv.Tiles[lv.Exit.X, lv.Exit.Y] = Tile.Stairs;
        return lv;
    }

    void Tunnel(Pos a, Pos b, Random rng)
    {
        var corner = rng.Next(2) == 0 ? new Pos(b.X, a.Y) : new Pos(a.X, b.Y);
        Carve(a, corner);
        Carve(corner, b);
    }

    void Carve(Pos from, Pos to)
    {
        var p = from;
        while (true)
        {
            Tiles[p.X, p.Y] = Tile.Floor;
            if (p == to) return;
            p = new Pos(p.X + Math.Sign(to.X - p.X), p.Y + Math.Sign(to.Y - p.Y));
        }
    }

    /// <summary>Steps from <paramref name="origin"/> to every reachable tile, 8-way. int.MaxValue when unreachable.</summary>
    public int[,] DistancesFrom(Pos origin)
    {
        var dist = new int[Width, Height];
        for (var x = 0; x < Width; x++)
            for (var y = 0; y < Height; y++)
                dist[x, y] = int.MaxValue;

        var queue = new Queue<Pos>();
        dist[origin.X, origin.Y] = 0;
        queue.Enqueue(origin);

        while (queue.TryDequeue(out var p))
        {
            foreach (var (_, step) in Dir.All)
            {
                var n = p + step;
                if (!Walkable(n) || dist[n.X, n.Y] != int.MaxValue) continue;
                dist[n.X, n.Y] = dist[p.X, p.Y] + 1;
                queue.Enqueue(n);
            }
        }

        return dist;
    }
}
