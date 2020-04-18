using System;
using System.Drawing;

namespace SpriteChat.Common
{
    public class Point
    {
        public double x;
        public double y;

        public Point(int v1, int v2)
        {
            x = v1;
            y = v2;
        }

        public double Distance(Point otherPoint)
        {
            return Math.Sqrt(Math.Pow(x - otherPoint.x, 2) + Math.Pow(y - otherPoint.y, 2));
        }

        public bool CanSee(Point otherPoint)
        {
            return otherPoint.x >= 0 && otherPoint.y >= 0 && Distance(otherPoint) < 5;
        }

    }
}
