using System.Collections.Generic;
using UnityEngine;

namespace ZeroHero
{
    // Character sprites use a shared, limited palette.
    public sealed class ZeroHeroArt
    {
        public readonly Sprite square, disc, ring, hero, skull, eye, brute, crown, coin;
        readonly List<Object> owned = new List<Object>();
        public static readonly Color Gain = Hex("A3B879"), Pink = Hex("D1735E"), Gold = Hex("D2AB62"), Ink = Hex("171B16");
        public static Color Hex(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out Color c); return c; }

        public ZeroHeroArt()
        {
            square = Shape(4, (x, y) => Color.white);
            disc = Shape(64, (x, y) => Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f)) < 31 ? Color.white : Color.clear);
            ring = Shape(128, (x, y) => { float d = Vector2.Distance(new Vector2(x, y), new Vector2(63.5f, 63.5f)); return d > 60 && d < 63 ? Color.white : Color.clear; });
            var palette = new Dictionary<char, Color> {
                ['#'] = Hex("151A15"), ['w'] = Hex("E4DAC0"), ['s'] = Hex("8F9680"),
                ['m'] = Hex("AA684A"), ['d'] = Hex("684633"), ['p'] = Hex("CFA58B"), ['r'] = Hex("965A42"),
                ['g'] = Gold, ['b'] = Hex("646E63"), ['v'] = Hex("BEC3A4")
            };
            hero = Pixels(new[] {
                "......mmmm......", ".....mdddm......", "....######......", "...#wwwwww#.....",
                "...#wwwwwws#....", "...#w###wss#....", "...#wm#mws#.....", "....#wwws#......",
                "..ddd#ss#ddd....", ".dmm#wwws#mmd...", ".dm#wwwwws#md.w.", ".dmm#wwss#mm.w.w",
                ".dmm#ssss#mm..w.", "..dm#wwss#md..w.", "..dmm####mmd..w.", "...mm#..#mm..ggg",
                "....#s..s#....g.", "...#ss..ss#.....", "...###..###.....", "................"
            }, palette);
            skull = Pixels(new[] {
                "................", ".....######.....", "...##pppppp##...", "..#pppppppppp#..",
                "..#pppppppppr#..", "..#p##ppp##pr#..", "..#p##ppp##pr#..", "...#ppp#pppr#...",
                "....#pppppr#....", "....#p#p#pr#....", ".....#####......", "....rrrrrr......",
                "...rrprrprr.....", "...rrr..rrr.....", "....r....r......", "................"
            }, palette);
            eye = Pixels(new[] {
                "................", ".......b........", "......bvb.......", ".....bvvvb......",
                "....bvvvvvb.....", "...bvvvvvvvb....", "..bvv#####vvb...", ".bvv#wwmww#vvb..",
                "..bv#wm#mwwvb...", "...vvwwmwwvv....", "....bvvvvvb.....", ".....bvvvb......",
                "......bvb.......", ".......b........", "................", "................"
            }, palette);
            brute = Pixels(new[] {
                "...gg....gg.....", "...grg..grg.....", "...#rrrrrr#.....", "..#rrrrrrrr#....",
                ".#rrgggrgggr#...", ".#rrr#rr#rrr#...", "..#rrrrrrrr#....", "...#r####r#.....",
                ".###rrrrrr###...", "#rrr#rrrr#rrr#..", "#rrr#rrrr#rrr#..", "#rrr#rrrr#rrr#..",
                ".###rrrrrr###...", "...#rr##rr#.....", "...#rr##rr#.....", "...#######......"
            }, palette);
            crown = Pixels(new[] {
                "..g....g....g...", "..gg..ggg..gg...", "..ggrgggrgggg...", "..ggggggggggg...",
                "...#########....", "..#bbbbbbbbb#...", ".#bbvvvvvvvbb#..", ".#bvggvvvggvb#..",
                ".#bv##vvv##vb#..", "..#vvvvvvvvv#...", "...#vv###vv#....", "..##vvvvvvv##...",
                ".#bbbvvvvvbbb#..", "#bbbbbvvvbbbbb#.", "#bbbbbbvbbbbbb#.", "###bbb###bbb###.",
                "..#bbb#.#bbb#...", "..#####.#####..."
            }, palette);
            coin = Pixels(new[] { "..ggg...", ".gwwwg..", "gwgggwg.", "gwgwgwg.", "gwgggwg.", ".gwwwg..", "..ggg...", "........" }, palette);
        }

        Sprite Shape(int size, System.Func<int, int, Color> pixel)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "Zero shape" };
            var colors = new Color[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) colors[y * size + x] = pixel(x, y);
            tex.SetPixels(colors); tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * .5f, size);
            owned.Add(tex); owned.Add(sprite); return sprite;
        }

        Sprite Pixels(string[] rows, Dictionary<char, Color> palette)
        {
            int w = rows[0].Length, h = rows.Length;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "Zero original pixel art" };
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
                tex.SetPixel(x, h - y - 1, x < rows[y].Length && palette.TryGetValue(rows[y][x], out Color c) ? c : Color.clear);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), Vector2.one * .5f, 16);
            owned.Add(tex); owned.Add(sprite); return sprite;
        }

        public void Dispose() { foreach (var item in owned) Object.Destroy(item); owned.Clear(); }
    }
}
