using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using App = Autodesk.AutoCAD.ApplicationServices;
using Db = Autodesk.AutoCAD.DatabaseServices;
using Ed = Autodesk.AutoCAD.EditorInput;
using Gem = Autodesk.AutoCAD.Geometry;
using Rtm = Autodesk.AutoCAD.Runtime;
using Win = Autodesk.AutoCAD.Windows;
using System.Configuration;
using System.Reflection;

namespace CatCoordLandPlots
{
    public static class View
    {
        public static void TextToConsole(List<Curve> curveDict)
        {
            SettingsParser settings = SettingsParser.getInstance();
            Ed.Editor acEd = App.Application.DocumentManager.MdiActiveDocument.Editor;
            foreach (Curve i in curveDict)
            {
                acEd.WriteMessage($"\n №{ i.Number } Area: {i.Area}");
                foreach (Point p in i.Vertixs)
                {
                    acEd.WriteMessage($"\n -  { p.Number } {p.X.ToString(settings.coordinatFormat) } {p.Y.ToString(settings.coordinatFormat)}");
                }
                acEd.WriteMessage($"\n -  { i.Vertixs.First().Number } {i.Vertixs.First().X.ToString(settings.coordinatFormat) } {i.Vertixs.First().Y.ToString(settings.coordinatFormat)}");
            }
        }

        public static void TextToCSV(List<Curve> curveDict)
        {
            SettingsParser settings = SettingsParser.getInstance();

            //более правильный и дорогой вариант см. тут:
            //http://adn-cis.org/forum/index.php?topic=448.0

            App.Document acDoc = App.Application.DocumentManager.MdiActiveDocument;
                StringBuilder sb = new StringBuilder();

                string separateLine = ";;;;;;;;;;;;;";// разделительная строка

                // заголовок
                sb.AppendLine($"{separateLine}\n;;;;;Х;У;;;;;;;\n;;;1;2;3;4;5;;;;;;\n{separateLine}");

                //Основной цикл вывода списка линий
                foreach (Curve сurve in curveDict)
                {
                    bool isFirstLine = true;
                    //Цикл вывода вершин линий
                    foreach (Point point in сurve.Vertixs)
                    {
                        if (isFirstLine)
                        {
                            isFirstLine = false;
                            //Вывожу номер кривой вместе с координатами первой вершины
                            sb.AppendLine($";;;участок № { сurve.Number };{ point.Number };{point.Y.ToString(settings.coordinatFormat) };{point.X.ToString(settings.coordinatFormat)};;;;;;;");
                        }
                        else
                            //Вывожу все остальные вершины
                            sb.AppendLine($";;;;{ point.Number };{point.Y.ToString(settings.coordinatFormat) };{point.X.ToString(settings.coordinatFormat)};;;;;;;");

                    }

                    //Вывожу координаты первой точки кривой, а так же ее площадь
                    sb.AppendLine($";;;;{ сurve.Vertixs.First().Number };{сurve.Vertixs.First().Y.ToString(settings.coordinatFormat) };{сurve.Vertixs.First().X.ToString(settings.coordinatFormat)};S={сurve.AreaOutput} га;;;;;;");
                    sb.AppendLine(separateLine);
                }
                sb.AppendLine($";;;;;;Общая площадь:;{Model.allArea} га;;;;;;");



                //Должен быть выбор пользователя, при этом по умолчанию предложить каталог, в котором находится файл чертежа.
                Win.SaveFileDialog sFile = new Win.SaveFileDialog("Каталог координат земельных участков",
                    acDoc.Database.Filename + ".csv",
                    "csv",
                    "Сохранить данные",
                    Win.SaveFileDialog.SaveFileDialogFlags.NoUrls |
                    Win.SaveFileDialog.SaveFileDialogFlags.NoShellExtensions |
                    Win.SaveFileDialog.SaveFileDialogFlags.NoFtpSites |
                    Win.SaveFileDialog.SaveFileDialogFlags.DoNotWarnIfFileExist |
                    Win.SaveFileDialog.SaveFileDialogFlags.DoNotTransferRemoteFiles);

                try
                {
                    if (sFile.ShowDialog() == System.Windows.Forms.DialogResult.OK && sFile.Filename.Length > 0)
                    {
                        outToCSV(sb.ToString(), sFile.Filename);
                    }
                }
                catch (Rtm.Exception ex)
                {
                    //MessageBox.Show("Error", ex.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    acDoc.Editor.WriteMessage($"\nError! {ex.Message}");
                }
            
        }

        private static void outToCSV(string contents, string path)
        {
            SettingsParser settings = SettingsParser.getInstance();
            System.IO.File.WriteAllText(path, contents, Encoding.GetEncoding(1251));
            if (settings.startExcel) System.Diagnostics.Process.Start(path);
        }


        public static void TextToDwg(Curve curve)
        {
            SettingsParser settings = SettingsParser.getInstance();
            //AddText(curve.textPoint(), layerText, $":ЗУ{ curve.Number }\nS={curve.Area} кв.м");
            AddMLeader(curve.textPoint(),
                        settings.MarkerTransform,
                        settings._Scale.Marker,
                        settings.layerText,
                        settings.mleaderMarkerStyle,
                        settings.markerText.Replace("|n|", curve.Number.ToString()).Replace("|s|", curve.Area.ToString()));



            foreach (Point p in curve.Vertixs)
            {
                //AddText(new Gem.Point2d(p.X, p.Y), layerPointNumber, p.Number.ToString());
                if (!p.isPrinted)
                {
                    AddMLeader(new Gem.Point2d(p.X, p.Y),
                                (p.vNormalise * settings.CoordTransform),
                                settings._Scale.Coord,
                                settings.layerPointNumber,
                                settings.mleaderCoordStyle,
                                p.Number.ToString());

                    AddCircle(new Gem.Point2d(p.X, p.Y), settings.layerPoint);
                    p.isPrinted = true; //Вот так, легко и непринужденно MVC пошел по бороде с этим костыликом.
                }

                ////Времменная
                //Gem.Point3d pnt1 = new Gem.Point3d(p.X, p.Y, 0) + (p.vNormalise * -5);
                //AddCircle(new Gem.Point2d(pnt1.X, pnt1.Y), "0");
            }

        }

        private static void AddMLeader(Gem.Point2d pnt, Gem.Vector3d otstup,
                                        double scale, string layer,
                                        string mleaderStyleName, string str)
        {

            SettingsParser settings = SettingsParser.getInstance();

            App.Document acDoc = App.Application.DocumentManager.MdiActiveDocument;
            Db.Database acCurDb = acDoc.Database;
            using (Db.Transaction acTrans = acCurDb.TransactionManager.StartOpenCloseTransaction())
            {
                Db.BlockTable acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId,
                                            Db.OpenMode.ForRead) as Db.BlockTable;
                Db.BlockTableRecord acBlkTblRec = acTrans.GetObject(acBlkTbl[Db.BlockTableRecord.ModelSpace],
                                                  Db.OpenMode.ForWrite) as Db.BlockTableRecord;
                Db.MLeader acML = new Db.MLeader();
                acML.SetDatabaseDefaults();
                acML.Layer = layer;
                acML.MLeaderStyle = CheckLocalRepository.GetIDbyName<Db.MLeaderStyle>(acCurDb, mleaderStyleName);
                acML.ContentType = Db.ContentType.MTextContent;
                Db.MText mText = new Db.MText();
                mText.SetDatabaseDefaults();
                mText.Contents = str;
                mText.TextHeight = scale;
                mText.BackgroundFill = settings.MTextMask;
                mText.UseBackgroundColor = settings.MTextMask;


                if (settings.MTextMask)
                    mText.BackgroundScaleFactor = settings.MTextMaskKoefficient;

                mText.Location = (new Gem.Point3d(pnt.X, pnt.Y, 0) + otstup);
                acML.MText = mText;
                int idx = acML.AddLeaderLine(new Gem.Point3d(pnt.X, pnt.Y, 0));

                acML.Scale = 1;


                acBlkTblRec.AppendEntity(acML);
                acTrans.AddNewlyCreatedDBObject(acML, true);
                acTrans.Commit();
            }
        }



        private static void AddCircle(Gem.Point2d pnt, string layer)
        {
            App.Document acDoc = App.Application.DocumentManager.MdiActiveDocument;
            Db.Database acCurDb = acDoc.Database;

            // старт транзакции
            using (Db.Transaction acTrans = acCurDb.TransactionManager.StartOpenCloseTransaction())
            {
                // Открытие таблицы Блоков для чтения
                Db.BlockTable acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId,
                                            Db.OpenMode.ForRead) as Db.BlockTable;

                // Открытие записи таблицы Блоков пространства Модели для записи
                Db.BlockTableRecord acBlkTblRec = acTrans.GetObject(acBlkTbl[Db.BlockTableRecord.ModelSpace],
                                                  Db.OpenMode.ForWrite) as Db.BlockTableRecord;

                Db.Circle acCircle = new Db.Circle();
                acCircle.SetDatabaseDefaults();
                acCircle.Center = new Gem.Point3d(pnt.X, pnt.Y, 0);
                acCircle.Radius = SettingsParser.getInstance()._Scale.Circle;
                acCircle.Layer = layer;
                // Добавление нового объекта в запись таблицы блоков и в транзакцию
                acBlkTblRec.AppendEntity(acCircle);
                acTrans.AddNewlyCreatedDBObject(acCircle, true);
                // Сохранение нового объекта в базе данных
                acTrans.Commit();
            }
        }

        private static void AddText(Gem.Point2d pnt, string layer, string str)
        {
            

            App.Document acDoc = App.Application.DocumentManager.MdiActiveDocument;
            Db.Database acCurDb = acDoc.Database;

            // старт транзакции
            using (Db.Transaction acTrans = acCurDb.TransactionManager.StartOpenCloseTransaction())
            {
                // Открытие таблицы Блоков для чтения
                Db.BlockTable acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId, Db.OpenMode.ForRead) as Db.BlockTable;

                // Открытие записи таблицы Блоков пространства Модели для записи
                Db.BlockTableRecord acBlkTblRec = acTrans.GetObject(acBlkTbl[Db.BlockTableRecord.ModelSpace],
                                                   Db.OpenMode.ForWrite) as Db.BlockTableRecord;

                Db.MText acMText = new Db.MText();
                acMText.SetDatabaseDefaults();
                acMText.Location = new Gem.Point3d(pnt.X, pnt.Y, 0);
                acMText.Contents = str;
                acMText.Height = SettingsParser.getInstance()._Scale.Coord;
                acMText.Color = Autodesk.AutoCAD.Colors.
                                Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                acMText.Layer = layer;
                acMText.SetDatabaseDefaults();
                // Добавление нового объекта в запись таблицы блоков и в транзакцию
                acBlkTblRec.AppendEntity(acMText);
                acTrans.AddNewlyCreatedDBObject(acMText, true);
                // Сохранение нового объекта в базе данных
                acTrans.Commit();
            }
        }
    }
}
