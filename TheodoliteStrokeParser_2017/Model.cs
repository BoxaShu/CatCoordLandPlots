using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using App = Autodesk.AutoCAD.ApplicationServices;
using cad = Autodesk.AutoCAD.ApplicationServices.Application;
using Db = Autodesk.AutoCAD.DatabaseServices;
using Ed = Autodesk.AutoCAD.EditorInput;
using Gem = Autodesk.AutoCAD.Geometry;
using Rtm = Autodesk.AutoCAD.Runtime;

namespace CatCoordLandPlots
{
    public class Model
    {
        public static List<Curve> curveDict;
        public static double allArea; // общая площадь участков
        private static List<Point> pointDict;

        public static void Init()
        {

            allArea = 0.0;
            //Словарик с точками
            pointDict = new List<Point>() { new Point(double.MaxValue, double.MinValue, int.MaxValue) };
            //Словарик с выбранными линиями
            curveDict = new List<Curve>();
        }

        public static void OutPutData()
        {
            if (curveDict.Count > 0)
            {
                allArea = curveDict.Sum(s => s.AreaOutput);
                //View.TextToConsole(curveDict);
                View.TextToCSV(curveDict);
            }
        }


        public static bool GetData(Db.ObjectId ObjectId)
        {
            SettingsParser settings = SettingsParser.getInstance();

            Db.Database acCurDb = App.Application.DocumentManager.MdiActiveDocument.Database;

            using (Db.Transaction acTrans = acCurDb.TransactionManager.StartOpenCloseTransaction())
            {
                Db.Polyline acPLine = acTrans.GetObject(ObjectId, Db.OpenMode.ForRead) as Db.Polyline;

                //Блокируем повторный выбор линии
                if (curveDict.FirstOrDefault(s => s.ObjId == ObjectId) == null)
                {
                    Curve crv = new Curve(ObjectId);
                    crv.Number = settings.startNumberCurve;
                    crv.Area = round(acPLine.Area, settings.areaTolerance);
                    crv.AreaOutput = round(acPLine.Area * settings.allAreaTolerance, settings.coordinateTolerance);

                    settings.startNumberCurve++;

                    if (pointDict.Count > 0)
                        settings.startNumberPoint = pointDict.Count;


                    int countVertix = acPLine.NumberOfVertices;

                    for (int i = 0; i < countVertix; i++)
                    {

                        Gem.Point2d pt = acPLine.GetPoint2dAt(i);

                        int iMin = (i == 0) ? iMin = countVertix - 1 : iMin = i - 1;
                        int iMax = (i == countVertix - 1) ? iMax = 0 : iMax = i + 1;

                        //Поучаю все три точки угла


                        Gem.Point3d ptO = new Gem.Point3d(acPLine.GetPoint2dAt(i).X,
                                                            acPLine.GetPoint2dAt(i).Y, 0);
                        Gem.Point3d ptMin = new Gem.Point3d(acPLine.GetPoint2dAt(iMin).X,
                                                            acPLine.GetPoint2dAt(iMin).Y, 0);
                        Gem.Point3d ptMax = new Gem.Point3d(acPLine.GetPoint2dAt(iMax).X,
                                                            acPLine.GetPoint2dAt(iMax).Y, 0);

                        Gem.Vector3d vMin = ptO.GetVectorTo(ptMin);
                        Gem.Vector3d vMax = ptO.GetVectorTo(ptMax);

                        Gem.Vector3d vNormaliseMin = vMin / vMin.Length;
                        Gem.Vector3d vNormaliseMax = vMax / vMax.Length;

                        Gem.Vector3d vNormalise = vNormaliseMin + vNormaliseMax;
                        vNormalise = vNormalise / vNormalise.Length;

                        //Тут нужно проверять, попадает ли точка нормализованного вектора внутрь фигуры или наружу
                        using (Db.Ray cl = new Db.Ray())
                        {
                            cl.BasePoint = ptO + vNormalise;
                            cl.UnitDir = vNormalise;
                            Gem.Point3dCollection pnt3dCol = new Gem.Point3dCollection();
                            acPLine.IntersectWith(cl, Db.Intersect.OnBothOperands, pnt3dCol, IntPtr.Zero, IntPtr.Zero);
                            if ((pnt3dCol.Count % 2) != 0)
                            {
                                vNormalise = vNormalise * (-1);
                            }
                        }

                        int namb = 0; //Номер вершины
                        if (pointDict.FirstOrDefault(s => s.IsEqualTo(pt, settings.coordinateTolerance)) == null)
                        {
                            Point pnt = new Point(round(pt.X, settings.coordinateTolerance),
                                                  round(pt.Y, settings.coordinateTolerance),
                                                  settings.startNumberPoint);

                            //Добавляем нормализованный вектор биссектриссы угла
                            pnt.vNormalise = vNormalise;
                            //Добавляем в список точек
                            pointDict.Add(pnt);
                            //Добавляем в кривую
                            crv.Vertixs.Add(pnt);

                            namb = settings.startNumberPoint;
                            settings.startNumberPoint++;
                        }
                        else
                        {
                            //Добавляем в список вершин уже обраотанную точку 
                            crv.Vertixs.Add(pointDict.FirstOrDefault(s => s.IsEqualTo(pt, settings.coordinateTolerance)));
                        }
                    }

                    //Добавляем в список линий
                    curveDict.Add(crv);
                    //Вывожу данные линии в чертеж
                    View.TextToDwg(crv);
                    //View.AddText(crv.textPoint(), $"{crv.Number} \\P S={crv.Area} кв.м.");
                }
                acTrans.Commit();
            }
            return true;
        }




        /// <summary>Округление
        /// 
        /// </summary>
        /// <param name="numeral">Цифра для округления</param>
        /// <param name="accuracy">Точность (кратность)</param>
        /// <param name="down_middle_up">-1 = вниз, 0 = до ближайшего, 1 = вверх</param>
        /// <returns></returns>
        public static double round(double numeral, double accuracy, int down_middle_up = 0)
        {
            //double aaa = Math.Round(numeral % accuracy);

            double tmp = Math.Round((numeral % accuracy), 10);
            accuracy = Math.Abs(accuracy);
            if (Math.Round(numeral / accuracy, 10) == Math.Round(numeral / accuracy, 0)) { tmp = 0; }

            if (down_middle_up == 1)
            {
                if (tmp != 0) numeral += numeral > 0 ? (accuracy - tmp) : -tmp;
                numeral = Math.Round(numeral, 10);
            }
            if (down_middle_up == -1)
            {
                if (tmp != 0) numeral += numeral < 0 ? -(accuracy + tmp) : -tmp;
                numeral = Math.Round(numeral, 10);
            }

            if (down_middle_up == 0)
            {
                double num1 = numeral;
                double num2 = numeral;

                if (tmp != 0) num1 += num1 > 0 ? (accuracy - tmp) : -tmp;
                num1 = Math.Round(num1, 10);
                if (tmp != 0) num2 += num2 < 0 ? -(accuracy + tmp) : -tmp;
                num2 = Math.Round(num2, 10);
                numeral = Math.Round((num1 - numeral), 10) > Math.Round((numeral - num2), 10) ? num2 : num1;

            }
            return numeral;
        }
    }
}
