﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using SAPINT.Gui.Main;

namespace SAPINT.Gui
{
    static class Program
    {

        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        public static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            log4net.Config.XmlConfigurator.Configure(new FileInfo("log4net.config"));
            log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
            log.Info("程序开始，加载SAP配置！！");

            //  SAPINT.SapConfig.SAPConfigFromFile.LoadSAPAllConfig();
            SAPINT.SapConfig.SAPConfigFromFile.LoadSAPClientConfig();
            Application.Run(new FormMain());
        }

       
    }
}
