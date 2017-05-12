using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;

using App = Autodesk.AutoCAD.ApplicationServices;
using Db = Autodesk.AutoCAD.DatabaseServices;
using Ed = Autodesk.AutoCAD.EditorInput;
using Gem = Autodesk.AutoCAD.Geometry;

namespace CatCoordLandPlots
{
    public class SettingsParser
    {
        //Сам синглтон
        private static SettingsParser instance;


        //Controller
        public  string allScale { get; private set; } = "";
        public  List<Scale> ScaleList { get; private set; } = new List<Scale>();

        //Model
        public  double coordinateTolerance { get; private set; } = 0.0001;
        public  double areaTolerance { get; private set; } = 1.0;
        public  double allAreaTolerance { get; private set; } = 1.0 / 10000;
        public  List<Point> pointDict { get; private set; }
        public  int startNumberPoint { get; set; } = 1;//Начальный номер вершины
        public  int startNumberCurve { get; set; } = 1; //Начальный номер выбранной линии


        //View
        //Формат вывода координат
        public  string coordinatFormat { get; private set; } = "#0.0000"; //формат вывода координат в CSV
        public  string layerText { get; private set; } = "ГИС_ЗУ_текст"; //слои
        public  string layerPoint { get; private set; } = "ГИС_ЗУ_точки";
        public  string layerPointNumber { get; private set; } = "ГИС_ЗУ_точки_номер";
        public  string mleaderCoordStyle { get; private set; } = "tspCoord"; //стиль мультивыноски координат
        public  string mleaderMarkerStyle { get; private set; } = "tspMarker"; //имя стиля выноски наименования участка
        public  Scale _Scale { get; set; } = new Scale();
        public  Gem.Vector3d MarkerTransform { get; private set; } = new Gem.Vector3d(10, -10, 0); //вектор смещения обозначения участка
        public  double CoordTransform { get; private set; } = 8; //Кратность смещения выноски координат
        public  string markerText { get; private set; } = "";//V_MarkerText
        public  bool startExcel { get; private set; } = true;//V_startExcel
        public  bool MTextMask { get; private set; } = true; //V_MTextMask
        public  double MTextMaskKoefficient { get; private set; } = 1.1; //V_MTextMaskKoefficient






        public static SettingsParser getInstance()
        {
            if (instance == null)
                instance = new SettingsParser();

            return instance;
        }

        public SettingsParser()
        {

        }


        public bool Update()
        {
            
            //Получаю конфигурационный файл
            var config = ConfigurationManager.OpenExeConfiguration(
            Assembly.GetExecutingAssembly().Location);
            var userSection = (ClientSettingsSection)config.
                GetSection("applicationSettings/TheodoliteStrokeParser.Properties.Settings");


            //Настройки Controller

            allScale = userSection.Settings.Get("С_ScaleListParam").Value.ValueXml.InnerText;
            //На всякий случай страхуюсь от невозможности прочитать
            allScale = (allScale != null & allScale.Length < 3) ? "500;0.5;1.5;1.5|1000;1;3;4|2000;2;6;8|5000;6;15;16" : allScale;

            List<string> strAllScale = (from q in allScale.Split('|') select q).ToList();
            ScaleList.Clear();
            foreach (var i in strAllScale)
            {
                List<string> str = i.Split(';').ToList();

                Scale s = new Scale();
                s.Number = int.Parse(str[0]);
                s.Circle = double.Parse(str[1]);
                s.Coord = double.Parse(str[2]);
                s.Marker = double.Parse(str[3]);

                ScaleList.Add(s);
            }





            //Настройки Model
         double _coordinateTolerance;
         double _areaTolerance;
         double _allAreaTolerance;
         int _startNumberPoint ;
         int _startNumberCurve ;

        _coordinateTolerance = double.TryParse(userSection.Settings.Get("M_СoordinateTolerance")
                                    .Value.ValueXml.InnerText, out _coordinateTolerance) ? _coordinateTolerance : 0.0001;

            _areaTolerance = double.TryParse(userSection.Settings.Get("M_AreaTolerance")
                            .Value.ValueXml.InnerText, out _areaTolerance) ? _areaTolerance : 1;

            _allAreaTolerance = double.TryParse(userSection.Settings.Get("M_AllAreaTolerance")
                                .Value.ValueXml.InnerText, out _allAreaTolerance) ? _allAreaTolerance : 0.0001;

            //Начальный номер вершины
            _startNumberPoint = int.TryParse(userSection.Settings.Get("M_StartNumberPoint")
                                .Value.ValueXml.InnerText, out _startNumberPoint) ? _startNumberPoint : 1;

            //Начальный номер выбранной линии
            _startNumberCurve = int.TryParse(userSection.Settings.Get("M_StartNumberCurve")
                                .Value.ValueXml.InnerText, out _startNumberCurve) ? _startNumberCurve : 1;

            coordinateTolerance = _coordinateTolerance;
            areaTolerance = _areaTolerance;
            allAreaTolerance = _allAreaTolerance;
            startNumberPoint = _startNumberPoint;
            startNumberCurve = _startNumberCurve;






            //Настройки View


            coordinatFormat = userSection.Settings.Get("V_CoordinatFormat").Value.ValueXml.InnerText;
            coordinatFormat = (coordinatFormat != null & coordinatFormat.Length < 1) ? "#0.0000" : coordinatFormat;

            layerText = userSection.Settings.Get("V_LayerText").Value.ValueXml.InnerText;
            layerText = (layerText != null & layerText.Length < 1) ? "ГИС_ЗУ_текст" : layerText;

            layerPoint = userSection.Settings.Get("V_LayerPoint").Value.ValueXml.InnerText;
            layerPoint = (layerPoint != null & layerPoint.Length < 1) ? "ГИС_ЗУ_точки" : layerPoint;


            layerPointNumber = userSection.Settings.Get("V_LayerPointNumber").Value.ValueXml.InnerText;
            layerPointNumber = (layerPointNumber != null & layerPointNumber.Length < 1) ? "ГИС_ЗУ_точки_номер" : layerPointNumber;

            mleaderCoordStyle = userSection.Settings.Get("V_MleaderCoordStyle").Value.ValueXml.InnerText;
            mleaderCoordStyle = (mleaderCoordStyle != null & mleaderCoordStyle.Length < 1) ? "tspCoord" : mleaderCoordStyle;

            mleaderMarkerStyle = userSection.Settings.Get("V_MleaderMarkerStyle").Value.ValueXml.InnerText;
            mleaderMarkerStyle = (mleaderMarkerStyle != null & mleaderMarkerStyle.Length < 1) ? "tspMarker" : mleaderMarkerStyle;

            double _CoordTransform;
            _CoordTransform = double.TryParse(userSection.Settings.Get("V_CoordTransform").Value.ValueXml.InnerText, out _CoordTransform) ? _CoordTransform : 8;
            CoordTransform=_CoordTransform;

            double MarkerTransformX = double.TryParse(userSection.Settings.Get("V_MarkerTransformX").Value.ValueXml.InnerText, out MarkerTransformX) ? MarkerTransformX : 0;
            double MarkerTransformY = double.TryParse(userSection.Settings.Get("V_MarkerTransformY").Value.ValueXml.InnerText, out MarkerTransformY) ? MarkerTransformY : 0;
            MarkerTransform = (MarkerTransformX != 0 & MarkerTransformY != 0) ? new Gem.Vector3d(MarkerTransformX, MarkerTransformY, 0) : MarkerTransform;

            //BaseCircleRadius = double.TryParse(userSection.Settings.Get("V_BaseCircleRadius").Value.ValueXml.InnerText, out BaseCircleRadius) ? BaseCircleRadius : 1;

            markerText = userSection.Settings.Get("V_MarkerText").Value.ValueXml.InnerText;
            markerText = (markerText != null & markerText.Length < 1) ? @":ЗУ|n| \P S= |s| кв.м" : markerText;

            bool _startExcel;
            _startExcel = bool.TryParse(userSection.Settings.Get("V_startExcel").Value.ValueXml.InnerText, out _startExcel) ? _startExcel : true;
            startExcel = _startExcel;

            bool _MTextMask;
            _MTextMask = bool.TryParse(userSection.Settings.Get("V_MTextMask").Value.ValueXml.InnerText, out _MTextMask) ? _MTextMask : true;
            MTextMask = _MTextMask;

            double _MTextMaskKoefficient;
            _MTextMaskKoefficient = double.TryParse(userSection.Settings.Get("V_MTextMaskKoefficient").Value.ValueXml.InnerText, out _MTextMaskKoefficient) ? _MTextMaskKoefficient : 1.1;
            MTextMaskKoefficient = _MTextMaskKoefficient;

            //_Scale = scale;

            //тут подготавливаем окружение для работы и загружаем стили мультивыноски
            App.Document acDoc = App.Application.DocumentManager.MdiActiveDocument;
            Db.Database acCurDb = acDoc.Database;
            Ed.Editor acEd = acDoc.Editor;

            bool returnBool = false;

            //Копируем из репозитория слои
            if (!CheckLocalRepository.CloneFromRepository<Db.LayerTableRecord>(acCurDb, layerText, CheckLocalRepository.GetRepository()))
            {
                acEd.WriteMessage($"\n Error! Can not clone Layer \"{layerText}\" from repository! Layer for Marker = \"0\"");
                layerText = "0";
            }

            if (!CheckLocalRepository.CloneFromRepository<Db.LayerTableRecord>(acCurDb, layerPoint, CheckLocalRepository.GetRepository()))
            {
                acEd.WriteMessage($"\n Error! Can not clone Layer \"{layerPoint}\" from repository! Layer for Circle = \"0\"");
                layerPoint = "0";
            }
            if (!CheckLocalRepository.CloneFromRepository<Db.LayerTableRecord>(acCurDb, layerPointNumber, CheckLocalRepository.GetRepository()))
            {
                acEd.WriteMessage($"\n Error! Can not clone Layer \"{layerPointNumber}\" from repository! Layer for Point Marker = \"0\"");
                layerPointNumber = "0";
            }


            //Копируем из репозитория стили МультиВыноски
            if (!CheckLocalRepository.CloneFromRepository<Db.MLeaderStyle>(acCurDb, mleaderCoordStyle, CheckLocalRepository.GetRepository()))
            {
                returnBool = false;
                acEd.WriteMessage($"\n Error! Can not clone MLeader Style \"{mleaderCoordStyle}\" from repository! ");
            }


            if (!CheckLocalRepository.CloneFromRepository<Db.MLeaderStyle>(acCurDb, mleaderMarkerStyle, CheckLocalRepository.GetRepository()))
            {
                acEd.WriteMessage($"\n Error! Can not clone MLeader Style \"{mleaderMarkerStyle}\" from repository! ");
                returnBool = false;
            }

            return returnBool;
        }
    }
}
