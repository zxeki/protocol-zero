using Godot;

namespace ProtocolZero.Util;

public static class Assets
{
    static readonly Dictionary<string, Texture2D?> Cache = new();

    public static Texture2D? Load(string path)
    {
        if (Cache.TryGetValue(path, out var cached))
            return cached;

        string full = $"res://assets/{path}.png";
        Texture2D? tex = null;
        if (ResourceLoader.Exists(full))
            tex = GD.Load<Texture2D>(full);
        Cache[path] = tex;
        return tex;
    }

    public static Texture2D[] Frames(string dir, string row, int count)
    {
        var list = new List<Texture2D>(count);
        for (int i = 0; i < count; i++)
        {
            var tex = Load($"{dir}/{row}_{i}");
            if (tex == null)
                break;
            list.Add(tex);
        }
        return list.ToArray();
    }
}
