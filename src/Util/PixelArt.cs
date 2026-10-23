using Godot;

namespace ProtocolZero.Util;

public sealed class PixelCanvas
{
    readonly int _w;
    readonly int _h;
    readonly Color[] _px;

    public PixelCanvas(int w, int h)
    {
        _w = w;
        _h = h;
        _px = new Color[w * h];
    }

    public void Set(int x, int y, Color c)
    {
        if (x >= 0 && x < _w && y >= 0 && y < _h)
            _px[y * _w + x] = c;
    }

    public void Rect(int x, int y, int w, int h, Color c)
    {
        for (int dy = 0; dy < h; dy++)
            for (int dx = 0; dx < w; dx++)
                Set(x + dx, y + dy, c);
    }

    public void FillCircle(float cx, float cy, float r, Color c)
    {
        int x0 = Mathf.Max(0, (int)(cx - r - 1));
        int x1 = Mathf.Min(_w - 1, (int)(cx + r + 1));
        int y0 = Mathf.Max(0, (int)(cy - r - 1));
        int y1 = Mathf.Min(_h - 1, (int)(cy + r + 1));
        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                float ddx = x + 0.5f - cx;
                float ddy = y + 0.5f - cy;
                if (ddx * ddx + ddy * ddy <= r * r)
                    Set(x, y, c);
            }
        }
    }

    public ImageTexture ToTexture(int scale = 1)
    {
        var img = Image.CreateEmpty(_w * scale, _h * scale, false, Image.Format.Rgba8);
        for (int y = 0; y < _h; y++)
        {
            for (int x = 0; x < _w; x++)
            {
                Color c = _px[y * _w + x];
                if (c.A <= 0f)
                    continue;
                for (int sy = 0; sy < scale; sy++)
                    for (int sx = 0; sx < scale; sx++)
                        img.SetPixel(x * scale + sx, y * scale + sy, c);
            }
        }
        return ImageTexture.CreateFromImage(img);
    }
}

public static class PixelArt
{
    static readonly Color Outline = new("#101318");
    static readonly Color ArmorDark = new("#262b34");
    static readonly Color ArmorMid = new("#39404c");
    static readonly Color StrikerRed = new("#d94a4a");
    static readonly Color Visor = new("#ff8866");
    static readonly Color GunMetal = new("#4c5563");

    static readonly Color VoltBlue = new("#4a9eff");
    static readonly Color VoltHair = new("#e8ecf5");
    static readonly Color VoltGlow = new("#9fc8ff");
    static readonly Color HawkTeal = new("#3ecfb2");
    static readonly Color HawkHood = new("#26796a");
    static readonly Color VexPink = new("#ff9ad5");
    static readonly Color VexMagenta = new("#e05ac8");
    static readonly Color TorqueAmber = new("#f0a030");
    static readonly Color TorqueDark = new("#8a5a20");

    public static ImageTexture BuildStriker()
    {
        var c = new PixelCanvas(22, 28);

        c.Rect(5, 0, 8, 8, Outline);
        c.Rect(6, 1, 6, 6, ArmorMid);
        c.Rect(9, 3, 3, 2, Visor);

        c.Rect(4, 8, 11, 9, Outline);
        c.Rect(5, 9, 9, 7, ArmorDark);
        c.Rect(6, 10, 7, 4, ArmorMid);
        c.Rect(8, 11, 3, 2, StrikerRed);

        c.Rect(2, 9, 3, 4, Outline);
        c.Rect(3, 10, 2, 2, ArmorMid);

        c.Rect(13, 12, 5, 3, Outline);
        c.Rect(14, 13, 3, 1, ArmorMid);
        c.Rect(16, 12, 6, 2, GunMetal);
        c.Rect(21, 12, 1, 2, StrikerRed);

        c.Rect(5, 17, 4, 11, Outline);
        c.Rect(6, 18, 2, 8, ArmorDark);
        c.Rect(10, 17, 4, 11, Outline);
        c.Rect(11, 18, 2, 8, ArmorDark);

        c.Rect(4, 25, 5, 3, Outline);
        c.Rect(9, 25, 5, 3, Outline);

        return c.ToTexture();
    }

    public static ImageTexture BuildWatcher()
    {
        var c = new PixelCanvas(16, 16);
        c.FillCircle(8f, 8f, 5.2f, Outline);
        c.FillCircle(8f, 8f, 4.2f, ArmorDark);
        c.Rect(6, 4, 4, 1, ArmorMid);
        c.Rect(3, 6, 3, 4, StrikerRed);
        c.Rect(3, 7, 2, 2, new Color("#ff6a4a"));
        c.Rect(3, 7, 1, 1, new Color("#ffd9c8"));
        c.Rect(8, 1, 1, 3, GunMetal);
        c.Set(8, 0, StrikerRed);
        c.Rect(12, 7, 2, 2, GunMetal);
        return c.ToTexture();
    }

    public static ImageTexture BuildEnforcer()
    {
        var c = new PixelCanvas(22, 32);

        c.Rect(7, 0, 8, 6, Outline);
        c.Rect(8, 1, 6, 4, ArmorDark);
        c.Rect(9, 2, 4, 2, new Color("#ff5a3c"));

        c.Rect(3, 6, 16, 12, Outline);
        c.Rect(4, 7, 14, 10, ArmorDark);
        c.Rect(7, 9, 8, 6, ArmorMid);
        c.Rect(9, 11, 4, 3, StrikerRed);

        c.Rect(0, 7, 4, 7, Outline);
        c.Rect(1, 8, 2, 5, ArmorMid);
        c.Rect(18, 7, 4, 7, Outline);
        c.Rect(19, 8, 2, 5, ArmorMid);

        c.Rect(0, 14, 6, 4, Outline);
        c.Rect(1, 15, 4, 2, GunMetal);
        c.Rect(0, 15, 1, 2, new Color("#ff5a3c"));

        c.Rect(4, 18, 6, 14, Outline);
        c.Rect(5, 19, 4, 11, ArmorDark);
        c.Rect(12, 18, 6, 14, Outline);
        c.Rect(13, 19, 4, 11, ArmorDark);

        return c.ToTexture();
    }

    public static ImageTexture BuildHunterDrone()
    {
        var c = new PixelCanvas(16, 14);

        c.Rect(4, 0, 8, 1, GunMetal);
        c.Set(4, 0, StrikerRed);
        c.Set(11, 0, StrikerRed);

        c.Rect(7, 1, 2, 3, GunMetal);

        c.Rect(3, 4, 10, 7, Outline);
        c.Rect(4, 5, 8, 5, ArmorDark);
        c.Rect(5, 6, 5, 3, ArmorMid);
        c.Rect(4, 7, 3, 2, new Color("#ff5a3c"));
        c.Rect(4, 7, 1, 2, new Color("#ffd9c8"));

        c.Rect(11, 6, 5, 2, Outline);
        c.Rect(12, 7, 4, 1, GunMetal);

        c.Rect(4, 11, 3, 2, ArmorDark);
        c.Rect(9, 11, 3, 2, ArmorDark);

        return c.ToTexture();
    }

    public static ImageTexture BuildWarden()
    {
        var c = new PixelCanvas(36, 52);

        c.Rect(11, 0, 14, 9, Outline);
        c.Rect(12, 1, 12, 7, ArmorDark);
        c.Rect(14, 3, 8, 3, new Color("#ff5a3c"));
        c.Rect(13, 5, 10, 1, StrikerRed);

        c.Rect(6, 9, 24, 20, Outline);
        c.Rect(7, 10, 22, 18, ArmorDark);
        c.Rect(11, 13, 14, 11, ArmorMid);
        c.Rect(15, 16, 6, 5, new Color("#ff5a3c"));
        c.Rect(16, 17, 4, 3, new Color("#ffd9c8"));

        c.Rect(0, 10, 7, 12, Outline);
        c.Rect(1, 11, 5, 10, ArmorMid);
        c.Rect(29, 10, 7, 12, Outline);
        c.Rect(30, 11, 5, 10, ArmorMid);

        c.Rect(0, 20, 8, 5, Outline);
        c.Rect(1, 21, 6, 3, GunMetal);
        c.Rect(0, 22, 2, 2, new Color("#ff5a3c"));

        c.Rect(28, 20, 8, 5, Outline);
        c.Rect(29, 21, 6, 3, GunMetal);
        c.Rect(34, 22, 2, 2, new Color("#ff5a3c"));

        c.Rect(8, 29, 8, 19, Outline);
        c.Rect(9, 30, 6, 16, ArmorDark);
        c.Rect(20, 29, 8, 19, Outline);
        c.Rect(21, 30, 6, 16, ArmorDark);

        c.Rect(6, 48, 11, 4, Outline);
        c.Rect(19, 48, 11, 4, Outline);

        return c.ToTexture();
    }

    public static ImageTexture BuildVolt()
    {
        var c = new PixelCanvas(22, 28);

        c.Rect(5, 0, 8, 8, Outline);
        c.Rect(6, 1, 6, 3, VoltHair);
        c.Rect(6, 4, 6, 3, ArmorMid);
        c.Rect(9, 4, 3, 2, VoltBlue);

        c.Rect(4, 8, 11, 9, Outline);
        c.Rect(5, 9, 9, 7, ArmorDark);
        c.Rect(6, 10, 7, 4, ArmorMid);
        c.Rect(8, 11, 3, 2, VoltBlue);

        c.Rect(2, 9, 3, 4, Outline);
        c.Rect(3, 10, 2, 2, ArmorMid);

        c.Rect(13, 10, 4, 3, Outline);
        c.FillCircle(18.5f, 11.5f, 2.6f, VoltBlue);
        c.FillCircle(18.5f, 11.5f, 1.4f, VoltGlow);
        c.Set(19, 11, Colors.White);

        c.Rect(5, 17, 4, 11, Outline);
        c.Rect(6, 18, 2, 8, ArmorDark);
        c.Rect(10, 17, 4, 11, Outline);
        c.Rect(11, 18, 2, 8, ArmorDark);

        c.Rect(4, 25, 5, 3, Outline);
        c.Rect(9, 25, 5, 3, Outline);

        return c.ToTexture();
    }

    public static ImageTexture BuildHawk()
    {
        var c = new PixelCanvas(22, 28);

        c.Rect(5, 0, 8, 8, Outline);
        c.Rect(6, 1, 6, 6, HawkHood);
        c.Rect(6, 4, 6, 3, ArmorDark);
        c.Rect(9, 4, 3, 2, HawkTeal);

        c.Rect(4, 8, 11, 9, Outline);
        c.Rect(5, 9, 9, 7, ArmorDark);
        c.Rect(6, 10, 7, 4, ArmorMid);
        c.Rect(8, 11, 3, 2, HawkTeal);

        c.Rect(2, 9, 3, 4, Outline);
        c.Rect(3, 10, 2, 2, ArmorMid);

        c.Rect(12, 11, 8, 4, Outline);
        c.Rect(13, 12, 7, 2, GunMetal);
        c.Rect(19, 11, 2, 4, Outline);
        c.Rect(20, 12, 2, 1, HawkTeal);
        c.Rect(14, 11, 2, 1, HawkTeal);

        c.Rect(5, 17, 4, 11, Outline);
        c.Rect(6, 18, 2, 8, ArmorDark);
        c.Rect(10, 17, 4, 11, Outline);
        c.Rect(11, 18, 2, 8, ArmorDark);

        c.Rect(4, 25, 5, 3, Outline);
        c.Rect(9, 25, 5, 3, Outline);

        return c.ToTexture();
    }

    public static ImageTexture BuildVex()
    {
        var c = new PixelCanvas(22, 28);

        c.Rect(5, 0, 8, 8, Outline);
        c.Rect(6, 1, 6, 3, VexPink);
        c.Rect(6, 4, 6, 3, ArmorMid);
        c.Rect(9, 4, 3, 2, VexMagenta);

        c.Rect(4, 8, 11, 9, Outline);
        c.Rect(5, 9, 9, 7, ArmorDark);
        c.Rect(6, 10, 7, 4, ArmorMid);
        c.Rect(7, 12, 6, 1, VexMagenta);
        c.Rect(9, 11, 2, 3, Colors.White);
        c.Rect(8, 12, 4, 1, Colors.White);

        c.Rect(2, 9, 3, 4, Outline);
        c.Rect(3, 10, 2, 2, ArmorMid);

        c.Rect(13, 11, 4, 3, Outline);
        c.Rect(16, 12, 4, 2, GunMetal);
        c.Rect(19, 12, 1, 2, VexMagenta);

        c.Rect(5, 17, 4, 11, Outline);
        c.Rect(6, 18, 2, 8, ArmorDark);
        c.Rect(10, 17, 4, 11, Outline);
        c.Rect(11, 18, 2, 8, ArmorDark);

        c.Rect(4, 25, 5, 3, Outline);
        c.Rect(9, 25, 5, 3, Outline);

        return c.ToTexture();
    }

    public static ImageTexture BuildTorque()
    {
        var c = new PixelCanvas(26, 30);

        c.Rect(7, 0, 9, 8, Outline);
        c.Rect(8, 1, 7, 6, ArmorMid);
        c.Rect(8, 1, 7, 2, TorqueAmber);
        c.Rect(11, 4, 3, 2, new Color("#ffd08a"));

        c.Rect(3, 8, 16, 12, Outline);
        c.Rect(4, 9, 14, 10, ArmorDark);
        c.Rect(7, 10, 8, 7, ArmorMid);
        c.Rect(9, 12, 5, 3, TorqueAmber);
        c.Rect(10, 13, 3, 1, new Color("#ffd08a"));

        c.Rect(0, 9, 4, 8, Outline);
        c.Rect(1, 10, 2, 6, ArmorMid);

        c.Rect(17, 6, 8, 18, Outline);
        c.Rect(18, 7, 6, 16, TorqueDark);
        c.Rect(18, 7, 2, 16, TorqueAmber);
        c.Rect(21, 8, 2, 14, TorqueAmber);
        c.Rect(19, 13, 3, 3, new Color("#ffd08a"));

        c.Rect(4, 20, 7, 10, Outline);
        c.Rect(5, 21, 5, 7, ArmorDark);
        c.Rect(12, 20, 7, 10, Outline);
        c.Rect(13, 21, 5, 7, ArmorDark);

        c.Rect(3, 27, 8, 3, Outline);
        c.Rect(11, 27, 8, 3, Outline);

        return c.ToTexture();
    }

    public static ImageTexture BuildBolt(Color core, Color trail)
    {
        var c = new PixelCanvas(7, 3);
        c.Rect(0, 1, 2, 1, new Color(trail, 0.45f));
        c.Rect(2, 1, 2, 1, trail);
        c.Rect(4, 0, 3, 3, core);
        return c.ToTexture();
    }
}
