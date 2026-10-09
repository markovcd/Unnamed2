# Ember

A terminal roguelike where the light is your health bar's evil twin.

You carry a dying ember. It is your light: the more fuel, the farther you see. It burns down as you walk, and when it dies the dark starts biting. Feed it with kindling (`*`) and by killing things. Eight levels down is daylight.

```
dotnet run --project Ember              # play
dotnet run --project Ember -- --seed 7  # same dungeon every time
dotnet run --project Ember -- --demo    # watch a dumb bot dive and die
```

Move: arrows / `hjkl` / `wasd` / `yubn` (diagonals). Wait: `.` Quit: `q`. Stairs `>` take you down when you step on them. `!` heals.

.NET 10, C# 14, no dependencies. Recursive-shadowcasting FOV, rooms-and-tunnels dungeons, one BFS distance map drives every monster, true-color glow that cools toward the edge of your light.
