using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gem = Autodesk.AutoCAD.Geometry;

namespace CatCoordLandPlots
{
    public class Point
    {
        //Понимаю публичные поля это не самый лучший подход,
        //но тут пускай будут
        public bool isPrinted = false;

        public int Number;
        public Gem.Vector3d vNormalise;
        public double X;
        public double Y;

        public Point(double x, double y, int N)
        {
            X = x;
            Y = y;
            Number = N;
            vNormalise = new Gem.Vector3d(0, 0, 0);
            isPrinted = false;
        }

        public Point()
        {
            isPrinted = false;
        }

        //Просто сравнение координат точек с заданной точнстью
        //метод один, сигнатуры разные
        public bool IsEqualTo(Point a, double tolerance)
        {
            return IsEqualTo(new Gem.Point2d(a.X, a.Y), tolerance);
        }

        public bool IsEqualTo(Gem.Point2d a, double tolerance)
        {
            double[] dP = { X - a.X, Y - a.Y };
            double length_dP = Math.Sqrt(dP[0] * dP[0] + dP[1] * dP[1]);
            if (length_dP < tolerance)
                return true;
            else
                return false;
        }
    }
}
