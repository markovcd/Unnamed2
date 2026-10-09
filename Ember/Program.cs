using Ember;

var seed = args.SkipWhile(a => a != "--seed").Skip(1).Select(int.Parse).Cast<int?>().FirstOrDefault() ?? Random.Shared.Next();

if (args.Contains("--demo") || Console.IsInputRedirected)
{
    var bot = new Game(seed);
    while (bot.State == GameState.Playing && bot.Turn < 5000)
        bot.Do(Bot.Next(bot));

    Console.Write(Renderer.Draw(bot, color: false));
    Console.WriteLine(Summary(bot, seed));
    return;
}

var game = new Game(seed);
Console.TreatControlCAsInput = true;
Console.Write("\e[?1049h\e[?25l");
try
{
    while (game.State == GameState.Playing)
    {
        Console.Write("\e[H" + Renderer.Draw(game).Replace("\n", "\e[K\n") + "\e[J");
        Console.Write("\e[2mmove: arrows/hjkl/wasd/yubn   wait: .   quit: q\e[0m");
        if (Read() is { } cmd) game.Do(cmd);
    }
    Console.Write("\e[H" + Renderer.Draw(game).Replace("\n", "\e[K\n") + "\e[J");
    Console.ReadKey(true);
}
finally
{
    Console.Write("\e[?25h\e[?1049l");
}

Console.WriteLine(Summary(game, seed));

static Command? Read()
{
    var k = Console.ReadKey(true);
    if (k.Key == ConsoleKey.C && k.Modifiers.HasFlag(ConsoleModifiers.Control)) return Command.Quit;

    return k.Key switch
    {
        ConsoleKey.UpArrow => Command.North,
        ConsoleKey.DownArrow => Command.South,
        ConsoleKey.LeftArrow => Command.West,
        ConsoleKey.RightArrow => Command.East,
        _ => char.ToLower(k.KeyChar) switch
        {
            'k' or 'w' => Command.North,
            'j' or 's' => Command.South,
            'h' or 'a' => Command.West,
            'l' or 'd' => Command.East,
            'y' => Command.NorthWest,
            'u' => Command.NorthEast,
            'b' => Command.SouthWest,
            'n' => Command.SouthEast,
            '.' or ' ' => Command.Wait,
            'q' => Command.Quit,
            _ => null,
        },
    };
}

static string Summary(Game g, int seed) => g.State switch
{
    GameState.Won => $"The ember lives. {g.Turn} turns, {g.Kills} kills. (seed {seed})",
    GameState.Dead => $"Killed by {g.KilledBy} on depth {g.Depth}. {g.Turn} turns, {g.Kills} kills. (seed {seed})",
    _ => $"Gave up on depth {g.Depth} after {g.Turn} turns. (seed {seed})",
};
