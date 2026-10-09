namespace Ember;

public enum GameState { Playing, Dead, Won, Quit }
public enum ItemKind { Kindling, Draught }

public sealed record Item(Pos Pos, ItemKind Kind);

public sealed record Species(char Glyph, string Name, int Hp, int MinDmg, int MaxDmg, int MinDepth, bool Erratic = false);

public sealed class Monster(Species kind, Pos pos)
{
    public Species Kind { get; } = kind;
    public Pos Pos { get; set; } = pos;
    public int Hp { get; set; } = kind.Hp;
}

public sealed class Game
{
    public const int MaxDepth = 8;
    public const int MaxHp = 20;
    public const int MaxFuel = 100;

    static readonly Species[] Bestiary =
    [
        new('r', "rat", 3, 1, 2, 1),
        new('b', "bat", 4, 1, 3, 1, Erratic: true),
        new('g', "ghoul", 9, 2, 4, 3),
        new('W', "wraith", 14, 3, 6, 5),
    ];

    readonly Random rng;
    int[,] dist = new int[Level.Width, Level.Height];

    public Level Level { get; private set; } = null!;
    public GameState State { get; private set; } = GameState.Playing;
    public string? KilledBy { get; private set; }
    public Pos Player { get; private set; }
    public int Depth { get; private set; }
    public int Hp { get; private set; } = MaxHp;
    public int Fuel { get; private set => field = Math.Clamp(value, 0, MaxFuel); } = 60;
    public int Turn { get; private set; }
    public int Kills { get; private set; }
    public List<Monster> Monsters { get; } = [];
    public List<Item> Items { get; } = [];
    public List<string> Messages { get; } = [];

    /// <summary>The ember is the light. It burns down, and the dark bites.</summary>
    public int LightRadius => Fuel == 0 ? 1 : Math.Clamp(2 + Fuel / 12, 2, 9);

    public Game(int seed)
    {
        rng = new Random(seed);
        Say("You carry a dying ember. Find the stairs. Feed it.");
        Descend();
    }

    public Monster? MonsterAt(Pos p) => Monsters.FirstOrDefault(m => m.Pos == p);

    /// <returns>True if the command used up a turn.</returns>
    public bool Do(Command cmd)
    {
        if (State != GameState.Playing) return false;
        if (cmd == Command.Quit) { State = GameState.Quit; return false; }

        var step = cmd.Step();
        if (step != default)
        {
            var to = Player + step;
            if (MonsterAt(to) is { } m) Attack(m);
            else if (Level.Walkable(to))
            {
                Player = to;
                PickUp();
                if (Level[Player] == Tile.Stairs) { Descend(); return true; }
            }
            else return false;
        }

        Tick();
        return true;
    }

    void Attack(Monster m)
    {
        var dmg = rng.Next(2, 5);
        m.Hp -= dmg;
        if (m.Hp > 0) { Say($"You hit the {m.Kind.Name} ({dmg})."); return; }

        Monsters.Remove(m);
        Kills++;
        Fuel += 4;
        Say($"You snuff out the {m.Kind.Name}. The ember drinks.");
    }

    void PickUp()
    {
        if (Items.FirstOrDefault(i => i.Pos == Player) is not { } item) return;
        Items.Remove(item);

        switch (item.Kind)
        {
            case ItemKind.Kindling:
                Fuel += 25;
                Say("You feed the ember. It flares.");
                break;
            case ItemKind.Draught:
                Hp = Math.Min(MaxHp, Hp + 8);
                Say("You drink the draught. Warmth.");
                break;
        }
    }

    void Descend()
    {
        Depth++;
        if (Depth > MaxDepth)
        {
            State = GameState.Won;
            Say("You carry the ember into daylight.");
            return;
        }

        if (Depth > 1)
        {
            Fuel += 15;
            Say($"Deeper. Depth {Depth}.");
        }

        Level = Level.Generate(rng);
        Player = Level.Start;
        Monsters.Clear();
        Items.Clear();
        Populate();
        Fov.Compute(Level, Player, LightRadius);
    }

    void Populate()
    {
        var spots = new Queue<Pos>(Level.Rooms.Skip(1)
            .SelectMany(r => r.Cells())
            .Where(p => p != Level.Exit)
            .OrderBy(_ => rng.Next()));

        var kinds = Bestiary.Where(s => s.MinDepth <= Depth).ToArray();
        for (var i = 0; i < Math.Min(3 + Depth, 12) && spots.TryDequeue(out var p); i++)
            Monsters.Add(new Monster(kinds[rng.Next(kinds.Length)], p));

        var draughts = Depth >= 4 ? 2 : 1;
        for (var i = 0; i < 3 + draughts && spots.TryDequeue(out var p); i++)
            Items.Add(new Item(p, i < 3 ? ItemKind.Kindling : ItemKind.Draught));
    }

    void Tick()
    {
        Turn++;
        BurnEmber();
        dist = Level.DistancesFrom(Player);

        foreach (var m in Monsters.ToList())
        {
            Act(m);
            if (State != GameState.Playing) return;
        }

        Fov.Compute(Level, Player, LightRadius);
    }

    void BurnEmber()
    {
        var before = Fuel;
        if (Turn % 2 == 0) Fuel--;

        if (before > 20 && Fuel <= 20) Say("Your ember gutters.");
        if (before > 0 && Fuel == 0) Say("The ember dies. The dark presses in.");

        if (Fuel == 0 && Turn % 3 == 0)
        {
            Say("The dark gnaws at you.");
            Hurt(1, "the dark");
        }
    }

    void Act(Monster m)
    {
        var d = dist[m.Pos.X, m.Pos.Y];
        if (d > 12) { if (rng.Next(4) == 0) Wander(m); return; }
        if (m.Kind.Erratic && rng.NextDouble() < 0.4) { Wander(m); return; }
        if (m.Pos.Reach(Player) == 1) Strike(m);
        else Chase(m);
    }

    void Strike(Monster m)
    {
        if (rng.NextDouble() >= 0.75) { Say($"The {m.Kind.Name} misses."); return; }
        var dmg = rng.Next(m.Kind.MinDmg, m.Kind.MaxDmg + 1);
        Say($"The {m.Kind.Name} hits you ({dmg}).");
        Hurt(dmg, $"a {m.Kind.Name}");
    }

    void Chase(Monster m)
    {
        var best = m.Pos;
        foreach (var (_, step) in Dir.All)
        {
            var p = m.Pos + step;
            if (Free(p) && dist[p.X, p.Y] < dist[best.X, best.Y]) best = p;
        }
        m.Pos = best;
    }

    void Wander(Monster m)
    {
        var p = m.Pos + Dir.All[rng.Next(Dir.All.Length)].Step;
        if (Free(p)) m.Pos = p;
    }

    bool Free(Pos p) => Level.Walkable(p) && p != Player && MonsterAt(p) is null;

    void Hurt(int dmg, string by)
    {
        Hp -= dmg;
        if (Hp > 0) return;
        State = GameState.Dead;
        KilledBy = by;
        Say("You die.");
    }

    void Say(string msg) => Messages.Add(msg);
}
