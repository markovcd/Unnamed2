using System.Text;

namespace Ember;

public static class Renderer
{
    const string Reset = "\e[0m";

    static readonly Dictionary<ItemKind, (char Glyph, (int R, int G, int B) Color)> ItemLook = new()
    {
        [ItemKind.Kindling] = ('*', (255, 200, 60)),
        [ItemKind.Draught] = ('!', (90, 200, 255)),
    };

    public static string Draw(Game g, bool color = true)
    {
        var lv = g.Level;
        var monsters = g.Monsters.Where(m => lv.Lit[m.Pos.X, m.Pos.Y]).ToDictionary(m => m.Pos);
        var items = g.Items.Where(i => lv.Lit[i.Pos.X, i.Pos.Y]).ToDictionary(i => i.Pos);

        var sb = new StringBuilder();
        string? current = null;

        void Put(char ch, (int R, int G, int B) c)
        {
            if (color)
            {
                var code = $"\e[38;2;{c.R};{c.G};{c.B}m";
                if (code != current) sb.Append(current = code);
            }
            sb.Append(ch);
        }

        for (var y = 0; y < Level.Height; y++)
        {
            for (var x = 0; x < Level.Width; x++)
            {
                var p = new Pos(x, y);
                if (p == g.Player) Put('@', (255, 245, 210));
                else if (lv.Lit[x, y] && monsters.TryGetValue(p, out var m)) Put(m.Kind.Glyph, (235, 70, 60));
                else if (lv.Lit[x, y] && items.TryGetValue(p, out var item)) Put(ItemLook[item.Kind].Glyph, ItemLook[item.Kind].Color);
                else if (lv.Lit[x, y]) PutTile(lv[p], Glow(p.Distance(g.Player), g.LightRadius));
                else if (lv.Seen[x, y]) PutTile(lv[p], (62, 56, 66));
                else Put(' ', (0, 0, 0));
            }
            sb.Append('\n');
        }

        void PutTile(Tile t, (int R, int G, int B) c) => Put(t switch
        {
            Tile.Wall => '#',
            Tile.Stairs => '>',
            _ => '·',
        }, t == Tile.Stairs ? (c.R, Math.Min(255, c.G + 90), Math.Min(255, c.B + 120)) : c);

        if (color) sb.Append(Reset);
        sb.Append($"HP {Bar(g.Hp, Game.MaxHp)} {Math.Max(g.Hp, 0),2}/{Game.MaxHp}   ");
        sb.Append($"Ember {Bar(g.Fuel, Game.MaxFuel)} {g.Fuel,3}   ");
        sb.Append($"Depth {g.Depth}/{Game.MaxDepth}   Kills {g.Kills}   Turn {g.Turn}\n");

        var log = g.Messages.TakeLast(4).ToList();
        for (var i = 0; i < 4; i++)
            sb.Append(i < 4 - log.Count ? "" : log[i - (4 - log.Count)]).Append('\n');

        return sb.ToString();
    }

    /// <summary>Hot near the player, cooling to red-brown at the edge of the light.</summary>
    static (int R, int G, int B) Glow(double dist, int radius)
    {
        var t = Math.Clamp(1 - dist / (radius + 1.5), 0, 1);
        return ((int)(70 + 185 * t), (int)(25 + 140 * t * t), (int)(15 + 50 * t * t * t));
    }

    static string Bar(int value, int max, int width = 10)
    {
        var filled = (int)Math.Ceiling((double)Math.Clamp(value, 0, max) * width / max);
        return new string('█', filled) + new string('░', width - filled);
    }
}
