using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Reflection;

using App = Autodesk.AutoCAD.ApplicationServices;
using Db = Autodesk.AutoCAD.DatabaseServices;
using Ed = Autodesk.AutoCAD.EditorInput;
using Rtm = Autodesk.AutoCAD.Runtime;

[assembly: Rtm.CommandClass(typeof(CatCoordLandPlots.Controller))]

namespace CatCoordLandPlots
{

    // Using powershell move to the folder containing your 
    // project files and enter the command
    //(dir -r -include   *.cs,*.vb | select-string . ).count
    // на 2017-05-03 получилось ~1250 строчек всего.



    public class Controller : Rtm.IExtensionApplication
    {

        

        /// <summary>
        /// Загрузка библиотеки
        /// http://through-the-interface.typepad.com/through_the_interface/2007/03/getting_the_lis.html
        /// </summary>
        #region 
        public void Initialize()
        {
            String assemblyFileFullName = GetType().Assembly.Location;
            String assemblyName = System.IO.Path.GetFileName(
                                                      GetType().Assembly.Location);

            // Just get the commands for this assembly
            App.DocumentCollection dm = App.Application.DocumentManager;
            Assembly asm = Assembly.GetExecutingAssembly();
            Ed.Editor acEd = dm.MdiActiveDocument.Editor;

            // Сообщаю о том, что произведена загрузка сборки 
            //и указываю полное имя файла,
            // дабы было видно, откуда она загружена
            acEd.WriteMessage(string.Format("\n{0} {1} {2}.\n{3}: {4}\n{5}\n",
                      "Assembly", assemblyName, "Loaded",
                      "Assembly File:", assemblyFileFullName,
                       "Copyright © Владимир Шульжицкий, 2017"));


            //Вывожу список комманд определенных в библиотеке
            acEd.WriteMessage("\nStart list of commands: \n\n");

            string[] cmds = GetCommands(asm, false);
            foreach (string cmd in cmds)
                acEd.WriteMessage(cmd + "\n");

            acEd.WriteMessage("\n\nEnd list of commands.\n");
        }

        public void Terminate()
        {
            Console.WriteLine("finish!");
        }

        /// <summary>
        /// Получение списка комманд определенных в сборке
        /// </summary>
        /// <param name="asm"></param>
        /// <param name="markedOnly"></param>
        /// <returns></returns>
        private static string[] GetCommands(Assembly asm, bool markedOnly)
        {
            StringCollection sc = new StringCollection();
            object[] objs =
              asm.GetCustomAttributes(
                typeof(Rtm.CommandClassAttribute),
                true
              );
            Type[] tps;
            int numTypes = objs.Length;
            if (numTypes > 0)
            {
                tps = new Type[numTypes];
                for (int i = 0; i < numTypes; i++)
                {
                    Rtm.CommandClassAttribute cca =
                      objs[i] as Rtm.CommandClassAttribute;
                    if (cca != null)
                    {
                        tps[i] = cca.Type;
                    }
                }
            }
            else
            {
                // If we're only looking for specifically
                // marked CommandClasses, then use an
                // empty list
                if (markedOnly)
                    tps = new Type[0];
                else
                    tps = asm.GetExportedTypes();
            }
            foreach (Type tp in tps)
            {
                MethodInfo[] meths = tp.GetMethods();
                foreach (MethodInfo meth in meths)
                {
                    objs =
                      meth.GetCustomAttributes(
                        typeof(Rtm.CommandMethodAttribute),
                        true
                      );
                    foreach (object obj in objs)
                    {
                        Rtm.CommandMethodAttribute attb =
                          (Rtm.CommandMethodAttribute)obj;
                        sc.Add(attb.GlobalName);
                    }
                }
            }
            string[] ret = new string[sc.Count];
            sc.CopyTo(ret, 0);

            return ret;
        }
        #endregion


        // Make sure that a block with this name exists in the drawing
        //а тут описание зачем нужны группы команд
        //http://spiderinnet1.typepad.com/blog/2013/01/autocad-net-command-group-and-command.html
        //http://www.theswamp.org/index.php?topic=38615.0
        // тут ветка с ошибкой локализации
        const String cmdNamespace = "BoxaShu";

        //Тут собственно определена основная команда
        [Rtm.CommandMethod(cmdNamespace,
            "CatCoordLandPlots",
            null,
            Rtm.CommandFlags.Modal |
            Rtm.CommandFlags.NoPaperSpace |
            Rtm.CommandFlags.NoBlockEditor)]

        static public void mainTheodoliteStrokeParser()
        {
            // Получение текущего документа и базы данных
            App.Document acDoc = App.Application.DocumentManager.MdiActiveDocument;
            if (acDoc == null) return;

            SettingsParser settings = SettingsParser.getInstance();
            if(settings.Update()) return;


            Ed.Editor acEd = acDoc.Editor;

            //тут запросить масштаб
            //Предполагается всего 4 вида стандартных масштабов - 1:500, 1:1000, 1:2000, 1:5000.
            Ed.PromptKeywordOptions pKeyOpts = new Ed.PromptKeywordOptions("");
            pKeyOpts.Message = "\nEnter an option: 1/";

            foreach (Scale i in settings.ScaleList)
                pKeyOpts.Keywords.Add(i.Number.ToString());

            pKeyOpts.AllowNone = false;
            pKeyOpts.AppendKeywordsToMessage = true;

            Ed.PromptResult pKeyRes = acDoc.Editor.GetKeywords(pKeyOpts);
            if (pKeyRes.Status != Ed.PromptStatus.OK) return;

            settings._Scale = settings.ScaleList.FirstOrDefault(s => s.Number == int.Parse(pKeyRes.StringResult));
            Model.Init();

            bool goOn = true; //Продолжать ли выбор
            do
            {
                Ed.PromptEntityOptions opt = new Ed.PromptEntityOptions("\n Select polyline: ");
                opt.AllowNone = false;
                opt.AllowObjectOnLockedLayer = false;
                opt.SetRejectMessage("\nNot a pline try again: ");
                opt.AddAllowedClass(typeof(Db.Polyline), true);

                Ed.PromptEntityResult res = acEd.GetEntity(opt);

                goOn = (res.Status == Ed.PromptStatus.OK) ? Model.GetData(res.ObjectId) : false;

            } while (goOn);

            Model.OutPutData();
        }


        /// <summary>
        /// Открытие шаблона чертежа
        /// </summary>
        [Rtm.CommandMethod(cmdNamespace,
                            "OpenCatCoordLandPlotsTamplate",
                            null,
                            Rtm.CommandFlags.Session)]
        public void bx_OpenTamplate()
        {

            App.Document doc = App.Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            string strFileName = CheckLocalRepository.GetRepository();
            App.DocumentCollection acDocMgr = App.Application.DocumentManager;

            if (System.IO.File.Exists(strFileName))
            {
                //Для 2011 автокада
#if acad2011
                acDocMgr.Open(strFileName, false);
#endif
                //Для 2012 и выше
#if !acad2011

                App.DocumentCollectionExtension.Open(acDocMgr, strFileName, false);
#endif
            }
            else
            {
                acDocMgr.MdiActiveDocument.Editor.
                    WriteMessage($"Файла репозитория { strFileName} не существует.");
            }
        }


    }
}
