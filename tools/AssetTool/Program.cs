using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Text.Json;

namespace AssetTool;

static class Program
{
    static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  AssetTool sample <pack> <x,y> [<x,y>...]");
            Console.WriteLine("  AssetTool boxes <pack> <x,y,w,h> [minW minH minFill]");
            Console.WriteLine("  AssetTool extract <pack> <manifest.json> <outDir> [--no-trim]");
            Console.WriteLine("  AssetTool montage <manifest.json> <outDir> <out.png> [scale]");
            Console.WriteLine("  AssetTool frames <pack> <spec.json> <outManifest.json>");
            return 1;
        }

        try
        {
            return args[0] switch
            {
                "sample" => Sample(args),
                "boxes" => Boxes(args),
                "extract" => Extract(args),
                "montage" => Montage(args),
                "frames" => Frames(args),
                _ => Fail($"unknown command {args[0]}"),
            };
        }
        catch (Exception e)
        {
            Console.WriteLine($"error: {e.Message}");
            return 1;
        }
    }

    static int Fail(string msg)
    {
        Console.WriteLine(msg);
        return 1;
    }

    static int Sample(string[] args)
    {
        using var bmp = new Bitmap(args[1]);
        for (int i = 3; i < args.Length; i++)
        {
            var (x, y) = ParseXY(args[i]);
            var c = bmp.GetPixel(x, y);
            Console.WriteLine($"{args[i]} = #{c.R:X2}{c.G:X2}{c.B:X2}");
        }
        return 0;
    }

    // Background model: dark flat colors covering many pixels of the sheet are bg/panel flats.
    // Anything else (incl. outlines, text, borders) is content. Distance is a tight flat-match.
    static (bool[] mask, int w, int h) BuildMask(Bitmap bmp, int x0, int y0, int x1, int y1)
    {
        int w = bmp.Width, h = bmp.Height;
        var counts = new Dictionary<Color, long>();
        for (int y = 0; y < h; y += 2)
        for (int x = 0; x < w; x += 2)
        {
            var c = bmp.GetPixel(x, y);
            counts[Color.FromArgb(255, c.R, c.G, c.B)] = counts.GetValueOrDefault(c) + 1;
        }

        var bg = counts
            .Where(kv => kv.Value >= 400 && Math.Max(kv.Key.R, Math.Max(kv.Key.G, kv.Key.B)) <= 100)
            .Select(kv => kv.Key)
            .ToArray();
        if (bg.Length == 0)
            throw new InvalidOperationException("no background colors detected");

        var mask = new bool[w * h];
        for (int y = y0; y < y1; y++)
        for (int x = x0; x < x1; x++)
        {
            var c = bmp.GetPixel(x, y);
            mask[y * w + x] = !IsBackground(c, bg);
        }
        return (mask, w, h);
    }

    static bool IsBackground(Color c, Color[] bg)
    {
        foreach (var b in bg)
        {
            int dr = Math.Abs(c.R - b.R), dg = Math.Abs(c.G - b.G), db = Math.Abs(c.B - b.B);
            if (dr + dg + db <= 24)
                return true;
        }
        return false;
    }

    static int Boxes(string[] args)
    {
        using var bmp = new Bitmap(args[1]);
        var (rx, ry, rw, rh) = ParseRect(args[2]);
        int minW = args.Length > 3 ? int.Parse(args[3]) : 8;
        int minH = args.Length > 4 ? int.Parse(args[4]) : 12;
        double minFill = args.Length > 5 ? double.Parse(args[5]) : 0.05;

        var (mask, w, _) = BuildMask(bmp, rx, ry, rx + rw, ry + rh);
        var boxes = FindBoxes(mask, w, rx, ry, rw, rh, minW, minH, minFill);

        foreach (var b in boxes.OrderBy(b => b.y / 16).ThenBy(b => b.x))
            Console.WriteLine($"{{\"x\":{b.x},\"y\":{b.y},\"w\":{b.w},\"h\":{b.h}}}");
        return 0;
    }

    static List<(int x, int y, int w, int h, long px)> FindBoxes(
        bool[] mask, int w, int rx, int ry, int rw, int rh, int minW, int minH, double minFill)
    {
        var boxes = new List<(int x, int y, int w, int h, long px)>();
        var stack = new Stack<int>();
        var seen = new bool[mask.Length];

        for (int y = ry; y < ry + rh; y++)
        for (int x = rx; x < rx + rw; x++)
        {
            int idx = y * w + x;
            if (!mask[idx] || seen[idx])
                continue;

            int minX = x, maxX = x, minY = y, maxY = y;
            long px = 0;
            stack.Push(idx);
            seen[idx] = true;

            while (stack.Count > 0)
            {
                int cur = stack.Pop();
                int cx = cur % w, cy = cur / w;
                px++;
                if (cx < minX) minX = cx;
                if (cx > maxX) maxX = cx;
                if (cy < minY) minY = cy;
                if (cy > maxY) maxY = cy;

                TryPush(cur + 1, cx + 1, cy);
                TryPush(cur - 1, cx - 1, cy);
                TryPush(cur + w, cx, cy + 1);
                TryPush(cur - w, cx, cy - 1);

                void TryPush(int n, int nx, int ny)
                {
                    if (nx < rx || nx >= rx + rw || ny < ry || ny >= ry + rh)
                        return;
                    if (!mask[n] || seen[n])
                        return;
                    seen[n] = true;
                    stack.Push(n);
                }
            }

            int bw = maxX - minX + 1, bh = maxY - minY + 1;
            double fill = (double)px / (bw * bh);
            if (bw >= minW && bh >= minH && fill >= minFill)
                boxes.Add((minX, minY, bw, bh, px));
        }
        return boxes;
    }

    static int Extract(string[] args)
    {
        using var bmp = new Bitmap(args[1]);
        var entries = JsonSerializer.Deserialize<List<Entry>>(File.ReadAllText(args[2]))
            ?? throw new InvalidOperationException("bad manifest");
        string outDir = args[3];
        bool trim = !args.Contains("--no-trim");

        var (mask, w, _) = BuildMask(bmp, 0, 0, bmp.Width, bmp.Height);

        foreach (var e in entries)
        {
            int rx = e.Rect[0], ry = e.Rect[1], rw = e.Rect[2], rh = e.Rect[3];

            // Background reachable from the rect border becomes transparent; interior
            // pixels (dark armor near bg) stay opaque.
            var reachable = new bool[rw * rh];
            var queue = new Queue<int>();
            void Seed(int x, int y)
            {
                if (x < 0 || y < 0 || x >= rw || y >= rh)
                    return;
                int i = y * rw + x;
                if (reachable[i] || mask[(ry + y) * w + (rx + x)])
                    return;
                reachable[i] = true;
                queue.Enqueue(i);
            }
            for (int x = 0; x < rw; x++) { Seed(x, 0); Seed(x, rh - 1); }
            for (int y = 0; y < rh; y++) { Seed(0, y); Seed(rw - 1, y); }
            while (queue.Count > 0)
            {
                int i = queue.Dequeue();
                int x = i % rw, y = i / rw;
                Seed(x + 1, y); Seed(x - 1, y); Seed(x, y + 1); Seed(x, y - 1);
            }

            static bool Opaque(bool[] reachable, bool[] mask, int rx, int ry, int rw, int w, int x, int y)
                => mask[(ry + y) * w + (rx + x)] || !reachable[y * rw + x];

            int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
            if (trim)
            {
                for (int y = 0; y < rh; y++)
                for (int x = 0; x < rw; x++)
                {
                    if (!Opaque(reachable, mask, rx, ry, rw, w, x, y))
                        continue;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
                if (maxX < 0)
                {
                    Console.WriteLine($"  ! {e.Name}: empty region");
                    continue;
                }
            }
            else
            {
                minX = 0; minY = 0; maxX = rw - 1; maxY = rh - 1;
            }

            int cw = maxX - minX + 1, ch = maxY - minY + 1;
            using var outBmp = new Bitmap(cw, ch, PixelFormat.Format32bppArgb);
            for (int y = 0; y < ch; y++)
            for (int x = 0; x < cw; x++)
            {
                int sx = minX + x, sy = minY + y;
                if (Opaque(reachable, mask, rx, ry, rw, w, sx, sy))
                {
                    var c = bmp.GetPixel(rx + sx, ry + sy);
                    outBmp.SetPixel(x, y, Color.FromArgb(255, c.R, c.G, c.B));
                }
                else
                {
                    outBmp.SetPixel(x, y, Color.FromArgb(0, 0, 0, 0));
                }
            }

            string path = Path.Combine(outDir, e.Name.Replace('/', Path.DirectorySeparatorChar) + ".png");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            outBmp.Save(path, ImageFormat.Png);
            Console.WriteLine($"  {e.Name} -> {cw}x{ch}");
        }
        return 0;
    }

    static int Montage(string[] args)
    {
        var entries = JsonSerializer.Deserialize<List<Entry>>(File.ReadAllText(args[1]))
            ?? throw new InvalidOperationException("bad manifest");
        string outDir = args[2];
        string outPath = args[3];
        int scale = args.Length > 4 ? int.Parse(args[4]) : 2;

        const int cellW = 150, cellH = 120, labelH = 14, cols = 6;
        int rows = (entries.Count + cols - 1) / cols;
        using var sheet = new Bitmap(cols * cellW, rows * (cellH + labelH));
        using var g = Graphics.FromImage(sheet);
        g.Clear(Color.FromArgb(16, 20, 30));
        using var font = new Font(FontFamily.GenericSansSerif, 8f);
        using var brush = new SolidBrush(Color.FromArgb(200, 220, 235));

        for (int i = 0; i < entries.Count; i++)
        {
            string path = Path.Combine(outDir, entries[i].Name.Replace('/', Path.DirectorySeparatorChar) + ".png");
            if (!File.Exists(path))
                continue;
            using var tex = new Bitmap(path);
            int cx = (i % cols) * cellW;
            int cy = (i / cols) * (cellH + labelH);
            g.FillRectangle(new SolidBrush(Color.FromArgb(30, 36, 50)), cx + 1, cy + 1, cellW - 2, cellH - 2);
            float s = Math.Min((cellW - 8) / (float)tex.Width, (cellH - 8) / (float)tex.Height);
            s = Math.Min(s, scale);
            int dw = (int)(tex.Width * s), dh = (int)(tex.Height * s);
            g.DrawImage(tex, cx + (cellW - dw) / 2, cy + (cellH - dh) / 2, dw, dh);
            g.DrawString($"{entries[i].Name} {tex.Width}x{tex.Height}", font, brush, cx + 3, cy + cellH);
        }

        sheet.Save(outPath, ImageFormat.Png);
        Console.WriteLine($"montage -> {outPath} ({sheet.Width}x{sheet.Height})");
        return 0;
    }

    // Spec-driven frame extraction: scans row bands, assigns components to column
    // cells by centroid, unions per cell, then splits/merges to exactly N frames.
    static int Frames(string[] args)
    {
        using var bmp = new Bitmap(args[1]);
        var spec = JsonSerializer.Deserialize<FrameSpec>(File.ReadAllText(args[2]),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("bad spec");
        var entries = new List<Entry>();

        foreach (var set in spec.Sets)
        {
            int n = set.Frames;
            int bandX0 = set.Cols.Values.Min(c => c[0]);
            int bandX1 = set.Cols.Values.Max(c => c[1]);
            string baseName = set.Cols.Count == 1 ? set.Prefix : null!;

            foreach (var (rowKey, row) in set.Rows.OrderBy(kv => kv.Value[0]))
            {
                int ry = row[0], rh = row[1] - row[0];
                var (mask, w, _) = BuildMask(bmp, bandX0, ry, bandX1, ry + rh);
                var comps = FindBoxes(mask, w, bandX0, ry, bandX1 - bandX0, rh, 8, 12, 0.05);

                foreach (var (colKey, col) in set.Cols)
                {
                    double cx0 = col[0], cellW = (col[1] - col[0]) / (double)n;
                    var cells = new List<(int x, int y, int w, int h)>[n];
                    for (int i = 0; i < n; i++)
                        cells[i] = new List<(int x, int y, int w, int h)>();

                    foreach (var c in comps)
                    {
                        double mid = c.x + c.w / 2.0;
                        if (mid < cx0 || mid >= cx0 + cellW * n)
                            continue;
                        int ci = (int)((mid - cx0) / cellW);
                        if (ci >= 0 && ci < n)
                            cells[ci].Add((c.x, c.y, c.w, c.h));
                    }

                    var rects = new List<(int x, int y, int w, int h)>();
                    foreach (var cell in cells)
                    {
                        if (cell.Count == 0)
                            continue;
                        int mx0 = int.MaxValue, my0 = int.MaxValue, mx1 = -1, my1 = -1;
                        foreach (var r in cell)
                        {
                            if (r.x < mx0) mx0 = r.x;
                            if (r.y < my0) my0 = r.y;
                            if (r.x + r.w - 1 > mx1) mx1 = r.x + r.w - 1;
                            if (r.y + r.h - 1 > my1) my1 = r.y + r.h - 1;
                        }
                        rects.Add((mx0, my0, mx1 - mx0 + 1, my1 - my0 + 1));
                    }
                    rects.Sort((a, b) => a.x.CompareTo(b.x));

                    while (rects.Count > n)
                    {
                        int best = 0;
                        long bestGap = long.MaxValue;
                        for (int i = 0; i + 1 < rects.Count; i++)
                        {
                            long gap = (long)rects[i + 1].x - (rects[i].x + rects[i].w);
                            if (gap < bestGap) { bestGap = gap; best = i; }
                        }
                        var a = rects[best];
                        var b = rects[best + 1];
                        int ex1 = Math.Max(a.x + a.w, b.x + b.w) - a.x;
                        int ey1 = Math.Max(a.y + a.h, b.y + b.h) - a.y;
                        rects[best] = (a.x, a.y, ex1, ey1);
                        rects.RemoveAt(best + 1);
                    }

                    while (rects.Count < n)
                    {
                        int best = -1, bestW = 7;
                        for (int i = 0; i < rects.Count; i++)
                            if (rects[i].w > bestW) { bestW = rects[i].w; best = i; }
                        if (best < 0)
                            break;
                        var r = rects[best];
                        int half = r.w / 2;
                        rects[best] = (r.x, r.y, half, r.h);
                        rects.Insert(best + 1, (r.x + half, r.y, r.w - half, r.h));
                        rects.Sort((x, y) => x.x.CompareTo(y.x));
                    }

                    if (rects.Count != n)
                        Console.WriteLine($"  ! {set.Prefix}/{colKey}/{rowKey}: {rects.Count}/{n} frames");

                    string name0 = baseName ?? $"{set.Prefix}/{colKey}";
                    for (int i = 0; i < rects.Count; i++)
                    {
                        var r = rects[i];
                        entries.Add(new Entry { Name = $"{name0}/{rowKey}_{i}", Rect = new[] { r.x, r.y, r.w, r.h } });
                    }
                }
            }
        }

        File.WriteAllText(args[3],
            JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"{entries.Count} entries -> {args[3]}");
        return 0;
    }

    static (int x, int y, int w, int h) ParseRect(string s)
    {
        var p = s.Split(',').Select(int.Parse).ToArray();
        return (p[0], p[1], p[2], p[3]);
    }

    static (int x, int y) ParseXY(string s)
    {
        var p = s.Split(',').Select(int.Parse).ToArray();
        return (p[0], p[1]);
    }

    sealed class Entry
    {
        public string Name { get; set; } = "";
        public int[] Rect { get; set; } = Array.Empty<int>();
    }

    sealed class FrameSpec
    {
        public List<FrameSet> Sets { get; set; } = new();
    }

    sealed class FrameSet
    {
        public string Prefix { get; set; } = "";
        public Dictionary<string, int[]> Cols { get; set; } = new();
        public Dictionary<string, int[]> Rows { get; set; } = new();
        public int Frames { get; set; } = 4;
    }
}
