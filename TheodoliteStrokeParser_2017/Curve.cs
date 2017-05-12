using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Db = Autodesk.AutoCAD.DatabaseServices;
using Gem = Autodesk.AutoCAD.Geometry;

namespace CatCoordLandPlots
{
    public class Curve
    {
        //Понимаю публичные поля это не самый лучший подход,
        //но ...

        public int Number;
        public List<Point> Vertixs;
        public Db.ObjectId ObjId;
        public double Area;
        public double AreaOutput;
        public Curve(Db.ObjectId objId)
        {
            ObjId = objId;
            Vertixs = new List<Point>();
        }



        //Точка вывода текста с данными линии
        public Gem.Point2d textPoint()
        {
            Gem.Point2d p;
            if (Vertixs.Count < 3)
            {
                Gem.Vector2d dP = new Gem.Point2d(Vertixs[1].X, Vertixs[1].Y) - new Gem.Point2d(Vertixs[0].X, Vertixs[0].Y);
                Gem.Vector2d l = (dP.Length / 2) * (dP / dP.Length);
                p = new Gem.Point2d(Vertixs[0].X, Vertixs[0].Y) + l;
            }
            else
            {
                Gem.Vector2d dP = new Gem.Point2d(Vertixs[1].X, Vertixs[1].Y) - new Gem.Point2d(Vertixs[0].X, Vertixs[0].Y);
                Gem.Vector2d l = (dP.Length / 2) * (dP / dP.Length);
                Gem.Point2d P1 = new Gem.Point2d(Vertixs[0].X, Vertixs[0].Y) + l;
                dP = new Gem.Point2d(Vertixs[2].X, Vertixs[2].Y) - P1;
                l = (dP.Length / 2) * (dP / dP.Length);
                p = P1 + l;
            }
            return p;
        }

    }
}
