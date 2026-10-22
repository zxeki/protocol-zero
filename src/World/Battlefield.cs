using Godot;

namespace ProtocolZero.World;

public partial class Battlefield : Node2D
{
    public const float GroundY = 232f;

    public override void _Draw()
    {
        var bg = new Color("#101319");
        var bgUpper = new Color("#141824");
        var skylineA = new Color("#1a2029");
        var skylineB = new Color("#1e2530");
        var midWall = new Color("#181d26");
        var midWallEdge = new Color("#2a3240");
        var floor = new Color("#1c212b");
        var floorEdge = new Color("#39424f");
        var hazardA = new Color("#8a7434");
        var hazardB = new Color("#232731");
        var crate = new Color("#2c333f");
        var crateEdge = new Color("#3d4655");
        var pillar = new Color("#242b36");
        var neonBlue = new Color("#2e6e7d");
        var neonRed = new Color("#7d2e3a");

        DrawRect(new Rect2(0f, 0f, 480f, 270f), bg);
        DrawRect(new Rect2(0f, 0f, 480f, 130f), bgUpper);

        float[] heights = { 40f, 62f, 50f, 78f, 36f, 58f, 84f, 46f, 66f, 38f, 72f, 54f, 44f, 80f, 48f };
        float x = 0f;
        for (int i = 0; i < heights.Length && x < 480f; i++)
        {
            float w = 32f + (i % 3) * 6f;
            float h = heights[i];
            DrawRect(new Rect2(x, GroundY - 30f - h, w, h), i % 2 == 0 ? skylineA : skylineB);
            if (i % 3 == 0)
                DrawRect(new Rect2(x + 4f, GroundY - 26f - h, 2f, 4f), neonBlue);
            if (i % 4 == 1)
                DrawRect(new Rect2(x + w - 8f, GroundY - 20f - h, 2f, 4f), neonRed);
            x += w + 4f;
        }

        DrawRect(new Rect2(0f, GroundY - 30f, 480f, 30f), midWall);
        DrawRect(new Rect2(0f, GroundY - 30f, 480f, 1f), midWallEdge);

        DrawRect(new Rect2(36f, GroundY - 20f, 20f, 20f), crate);
        DrawRect(new Rect2(36f, GroundY - 20f, 20f, 1f), crateEdge);
        DrawRect(new Rect2(60f, GroundY - 14f, 14f, 14f), crate);
        DrawRect(new Rect2(60f, GroundY - 14f, 14f, 1f), crateEdge);

        DrawRect(new Rect2(0f, GroundY, 480f, 270f - GroundY), floor);
        DrawRect(new Rect2(0f, GroundY, 480f, 2f), floorEdge);

        for (int i = 0; i < 30; i++)
            DrawRect(new Rect2(i * 16f, 262f, 16f, 8f), i % 2 == 0 ? hazardA : hazardB);
        DrawRect(new Rect2(0f, 260f, 480f, 2f), new Color("#141821"));

        DrawRect(new Rect2(150f, GroundY - 26f, 6f, 26f), pillar);
        DrawRect(new Rect2(330f, GroundY - 26f, 6f, 26f), pillar);
    }
}
