using Godot;
using ProtocolZero.Combat;
using ProtocolZero.Util;

namespace ProtocolZero.World;

public partial class Battlefield : Node2D
{
    public const float GroundY = 206f;
    public const int WavesPerSector = 5;

    public CombatManager? Combat;

    int _lastWave = -1;

    sealed class Palette
    {
        public Color Bg;
        public Color BgUpper;
        public Color SkylineA;
        public Color SkylineB;
        public Color MidWall;
        public Color MidWallEdge;
        public Color Floor;
        public Color FloorEdge;
        public Color NeonBlue;
        public Color NeonRed;
        public Color Pillar;
    }

    static readonly Palette[] Palettes =
    {
        new() // rooftop - violet night
        {
            Bg = new Color("#101319"),
            BgUpper = new Color("#171428"),
            SkylineA = new Color("#241a33"),
            SkylineB = new Color("#2d2140"),
            MidWall = new Color("#1a1730"),
            MidWallEdge = new Color("#35304d"),
            Floor = new Color("#191622"),
            FloorEdge = new Color("#3d3852"),
            NeonBlue = new Color("#6e3a8e"),
            NeonRed = new Color("#b03a7d"),
            Pillar = new Color("#24203a"),
        },
        new() // foundry - teal industrial
        {
            Bg = new Color("#0d1416"),
            BgUpper = new Color("#101b1e"),
            SkylineA = new Color("#16262b"),
            SkylineB = new Color("#1c3138"),
            MidWall = new Color("#131e21"),
            MidWallEdge = new Color("#2a4046"),
            Floor = new Color("#141d20"),
            FloorEdge = new Color("#39505a"),
            NeonBlue = new Color("#2e7d8e"),
            NeonRed = new Color("#2e8e7d"),
            Pillar = new Color("#1a2b2f"),
        },
        new() // docks - ember orange
        {
            Bg = new Color("#14100c"),
            BgUpper = new Color("#1b1510"),
            SkylineA = new Color("#2b2018"),
            SkylineB = new Color("#36281c"),
            MidWall = new Color("#211a13"),
            MidWallEdge = new Color("#43372a"),
            Floor = new Color("#1d1712"),
            FloorEdge = new Color("#4a3d2d"),
            NeonBlue = new Color("#8e6a2e"),
            NeonRed = new Color("#a84a2e"),
            Pillar = new Color("#2b2218"),
        },
    };

    static readonly (string Name, float X)[][] ArenaProps =
    {
        new[] { ("crate_orange", 24f), ("barrel_teal", 78f), ("crate_gray", 404f), ("barrel_red", 448f) },
        new[] { ("box_green", 22f), ("rail", 170f), ("bin_x2", 254f), ("barrel_rad", 444f) },
        new[] { ("pillar_teal", 40f), ("pot_orange", 92f), ("barrel_blue", 420f), ("bin_teal", 452f) },
    };

    static readonly (string A, string B)[] ArenaWallTiles =
    {
        ("panel_dark", "panel_bracket"),
        ("panel_frame", "panel_core"),
        ("panel_x", "panel_hazard"),
    };

    public static int ArenaIndex(int wave) => ((wave - 1) / WavesPerSector) % Palettes.Length;

    public override void _Process(double delta)
    {
        if (Combat == null || Combat.Wave == _lastWave)
            return;
        _lastWave = Combat.Wave;
        QueueRedraw();
    }

    public override void _Draw()
    {
        int arena = ArenaIndex(Combat?.Wave ?? 0);
        var p = Palettes[arena];
        var hazardA = new Color("#8a7434");
        var hazardB = new Color("#232731");

        DrawRect(new Rect2(0f, 0f, 480f, 270f), p.Bg);
        DrawRect(new Rect2(0f, 0f, 480f, 130f), p.BgUpper);

        float[] heights = { 40f, 62f, 50f, 78f, 36f, 58f, 84f, 46f, 66f, 38f, 72f, 54f, 44f, 80f, 48f };
        float x = 0f;
        for (int i = 0; i < heights.Length && x < 480f; i++)
        {
            float w = 32f + (i % 3) * 6f;
            float h = heights[i];
            DrawRect(new Rect2(x, GroundY - 30f - h, w, h), i % 2 == 0 ? p.SkylineA : p.SkylineB);
            if (i % 3 == 0)
                DrawRect(new Rect2(x + 4f, GroundY - 26f - h, 2f, 4f), p.NeonBlue);
            if (i % 4 == 1)
                DrawRect(new Rect2(x + w - 8f, GroundY - 20f - h, 2f, 4f), p.NeonRed);
            x += w + 4f;
        }

        DrawRect(new Rect2(0f, GroundY - 30f, 480f, 30f), p.MidWall);
        DrawRect(new Rect2(0f, GroundY - 30f, 480f, 1f), p.MidWallEdge);
        DrawWallTiles(arena);

        DrawRect(new Rect2(0f, GroundY, 480f, 270f - GroundY), p.Floor);
        DrawRect(new Rect2(0f, GroundY, 480f, 2f), p.FloorEdge);

        for (int i = 0; i < 30; i++)
            DrawRect(new Rect2(i * 16f, 262f, 16f, 8f), i % 2 == 0 ? hazardA : hazardB);
        DrawRect(new Rect2(0f, 260f, 480f, 2f), new Color("#141821"));

        DrawRect(new Rect2(150f, GroundY - 26f, 6f, 26f), p.Pillar);
        DrawRect(new Rect2(330f, GroundY - 26f, 6f, 26f), p.Pillar);

        foreach (var (name, propX) in ArenaProps[arena])
            DrawProp(name, propX);
    }

    void DrawWallTiles(int arena)
    {
        var (a, b) = ArenaWallTiles[arena];
        float x = 0f;
        int i = 0;
        while (x < 480f)
        {
            var tex = Assets.Load($"tile/{(i % 2 == 0 ? a : b)}");
            if (tex == null)
                return;
            var s = tex.GetSize();
            DrawTextureRect(tex, new Rect2(x, GroundY - s.Y, s.X, s.Y), false);
            x += s.X;
            i++;
        }
    }

    void DrawProp(string name, float propX)
    {
        var tex = Assets.Load($"props/{name}");
        if (tex == null)
            return;
        var s = tex.GetSize();
        DrawTextureRect(tex, new Rect2(propX, GroundY - s.Y, s.X, s.Y), false);
    }
}
