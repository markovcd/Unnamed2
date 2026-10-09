namespace Ember;

/// <summary>Dumb stair-diver. Fights what touches it, otherwise walks downhill to the exit.</summary>
public static class Bot
{
    public static Command Next(Game g)
    {
        foreach (var (cmd, step) in Dir.All)
            if (g.MonsterAt(g.Player + step) is not null)
                return cmd;

        var toExit = g.Level.DistancesFrom(g.Level.Exit);
        return Dir.All
            .Where(d => g.Level.Walkable(g.Player + d.Step))
            .MinBy(d => toExit[g.Player.X + d.Step.X, g.Player.Y + d.Step.Y])
            .Cmd;
    }
}
