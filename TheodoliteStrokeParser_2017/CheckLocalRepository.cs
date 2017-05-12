using System;
using System.IO;
using System.Reflection;
using System.Linq;

using App = Autodesk.AutoCAD.ApplicationServices;
using Db = Autodesk.AutoCAD.DatabaseServices;
using Ed = Autodesk.AutoCAD.EditorInput;
using Rtm = Autodesk.AutoCAD.Runtime;

namespace CatCoordLandPlots
{
    /// <summary>
    /// Класс для создания и синхронизации локального репозитория
    /// </summary>
    class CheckLocalRepository
    {
        public static string GetRepository()
        {
            string path = WriteResourceToFile("", "");
            return path;
        }

        /// <summary>
        /// Пишу файл из ресурсов в то же место, где лежит библитека
        /// http://adn-cis.org/forum/index.php?topic=3125.msg12780#msg12780
        /// </summary>
        private static string WriteResourceToFile(string path, string fileName)
        {


            if (path.Trim() == "")
                path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().
                                                                      Location);

            if (fileName.Trim() == "")
                fileName = "dwgResources.dwg";

            fileName = Path.Combine(path, fileName);

            if (!System.IO.File.Exists(fileName))
            {

                byte[] array = global::CatCoordLandPlots.Properties.Resources.dwgResources;

                // Попробуем записать файл репозитория
                try
                {
                    var fs = new FileStream(fileName, FileMode.Create);
                    fs.Write(array, 0, array.Length);
                    fs.Close();
                }
                catch (Exception ex)
                {
                    // В случае ошибки обнуляем путь до файла
                    fileName = string.Empty;
                    App.Application.DocumentManager.
                        MdiActiveDocument.Editor.
                        WriteMessage(
                        "\nError during clone repository file from programm resource: "
                        + ex.Message);
                }
            }

            return fileName;
        } //WriteResourceToFile



        internal static Db.ObjectId GetIDbyName<T>(Db.Database db, string strName)
        {

            Db.ObjectId RecId = Db.ObjectId.Null;
            using (Db.Transaction tr1 = db.TransactionManager.StartTransaction())
            {

                if (typeof(T) == typeof(Db.MLeaderStyle))
                {
                    Db.DBDictionary mlstyles = (Db.DBDictionary)tr1.GetObject(
                                                    db.MLeaderStyleDictionaryId,
                                                    Db.OpenMode.ForRead);

                    RecId = (mlstyles.Contains(strName)) ? mlstyles.GetAt(strName) : Db.ObjectId.Null;
                }
                else if (typeof(T) == typeof(Db.LayerTableRecord))
                {
                    Db.LayerTable layerTable;
                    layerTable = (Db.LayerTable)tr1.GetObject(db.LayerTableId,
                                                              Db.OpenMode.ForRead);

                    RecId = (layerTable.Has(strName)) ? layerTable[strName] : Db.ObjectId.Null;
                }
                tr1.Commit();
            }
            // Все еще можно получить Db.ObjectId.Null для передачи.
            return RecId;
        }


        internal static bool CloneFromRepository<T>(Db.Database db,
                                              string strName,
                                              string RepositorySourcePath)
        {
            bool returnValue = false;
            using (Db.Database dbSource = new Db.Database(false, true))
            {
                try
                {
                    dbSource.ReadDwgFile(RepositorySourcePath,
                                         System.IO.FileShare.Read,
                                          true,
                                          null);
                }
                catch (Autodesk.AutoCAD.Runtime.Exception ex)
                {
                    App.Application.DocumentManager.MdiActiveDocument.Editor.
                        WriteMessage("\nError reading file repository: " +
                        ex.Message);
                    return returnValue;
                }

                Db.ObjectIdCollection sourceIds = new Db.ObjectIdCollection();

                using (Db.Transaction tr1 = dbSource.TransactionManager.
                                                            StartTransaction())
                {
                    Db.ObjectId destDbMsId = Db.ObjectId.Null;
                    if (typeof(T) == typeof(Db.MLeaderStyle))
                        destDbMsId = db.MLeaderStyleDictionaryId;
                    else if (typeof(T) == typeof(Db.LayerTableRecord))
                        destDbMsId = db.LayerTableId;

                    Db.ObjectId destId = GetIDbyName<T>(dbSource, strName);

                    if (destId != Db.ObjectId.Null && destDbMsId != Db.ObjectId.Null)
                    {
                        sourceIds.Add(destId);
                        if (sourceIds.Count > 0)
                        {
                            Db.IdMapping mapping = new Db.IdMapping();
                            try
                            {
                                //Клонируем с заменой
                                dbSource.WblockCloneObjects(sourceIds,
                                    destDbMsId,
                                    mapping,
                                    Db.DuplicateRecordCloning.Replace,
                                    false);

                                returnValue = true;
                            }
                            catch (Rtm.Exception ex)
                            {
                                App.Application.DocumentManager.MdiActiveDocument.Editor.
                                    WriteMessage(@"\nError during clone from the repository: "
                                    + ex.Message);
                            }
                        }
                    }
                    else
                        App.Application.DocumentManager.MdiActiveDocument.Editor.
                            WriteMessage("\nError during clone from repository. " +
                            "At the repository there is no Object with the specified name!");

                    tr1.Commit();
                }
            }

            return returnValue;
        } //CloneFromRepository
    }
}

