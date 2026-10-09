namespace Ember;

public readonly record struct Pos(int X, int Y)
{
    public static Pos operator +(Pos a, Pos b) => new(a.X + b.X, a.Y + b.Y);

    /// <summary>Chebyshev distance: 1 means touching, diagonals included.</summary>
    public int Reach(Pos o) => Math.Max(Math.Abs(X - o.X), Math.Abs(Y - o.Y));

    public double Distance(Pos o) => Math.Sqrt((X - o.X) * (X - o.X) + (Y - o.Y) * (Y - o.Y));
}

public enum Command { Wait, Quit, North, South, East, West, NorthEast, NorthWest, SouthEast, SouthWest }

public static class Dir
{
    public static readonly (Command Cmd, Pos Step)[] All =
    [
        (Command.North, new(0, -1)),
        (Command.South, new(0, 1)),
        (Command.East, new(1, 0)),
        (Command.West, new(-1, 0)),
        (Command.NorthEast, new(1, -1)),
        (Command.NorthWest, new(-1, -1)),
        (Command.SouthEast, new(1, 1)),
        (Command.SouthWest, new(-1, 1)),
    ];

    public static Pos Step(this Command c) => All.FirstOrDefault(d => d.Cmd == c).Step;
}
