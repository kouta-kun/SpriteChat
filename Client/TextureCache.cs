using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpriteChat
{
    class TextureCache
    {
        static Bitmap overworldSet = Client.Properties.Resources.Overworld;
        static Bitmap charSet = Client.Properties.Resources.character;
        public const int tilesize = 16;
        static SortedDictionary<int, Bitmap> overworldCache = new SortedDictionary<int, Bitmap>();
        static SortedDictionary<int, Bitmap> characterCache = new SortedDictionary<int, Bitmap>();

        public static Bitmap Overworld(int x)
        {
            if (overworldCache.ContainsKey(x))
                return overworldCache[x];
            int tileW = overworldSet.Width / tilesize;
            int tileH = overworldSet.Height / tilesize;
            int tileCount = tileW * tileH;
            if (x >= tileCount) return null;
            Bitmap subBitmap = new Bitmap(tilesize, tilesize);
            var g = Graphics.FromImage(subBitmap);
            g.DrawImage(overworldSet, new Rectangle(0, 0, tilesize, tilesize), (x % tileW) * tilesize, (x / tileW) * tilesize, tilesize, tilesize, GraphicsUnit.Pixel);
            g.Flush();
            g.Dispose();
            overworldCache[x] = subBitmap;
            return subBitmap;
        }
        public static Bitmap Char(int x)
        {
            if (characterCache.ContainsKey(x))
                return characterCache[x];
            int tileW = charSet.Width / tilesize;
            int tileH = (charSet.Height / tilesize) / 2;
            int tileCount = tileW * tileH;
            if (x >= tileCount) return null;
            Bitmap subBitmap = new Bitmap(tilesize, tilesize * 2);
            var g = Graphics.FromImage(subBitmap);
            g.DrawImage(charSet, new Rectangle(0, 0, tilesize, tilesize * 2), (x % tileW) * tilesize, (x / tileW) * tilesize * 2, tilesize, tilesize * 2, GraphicsUnit.Pixel);
            g.Flush();
            g.Dispose();
            characterCache[x] = subBitmap;
            return subBitmap;
        }
    }
}
