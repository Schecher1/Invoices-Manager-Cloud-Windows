﻿using InvoicesManager.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace InvoicesManager.Classes
{
    public class EnvironmentsVariable
    {
        public static List<InvoiceModel> allInvoices = new List<InvoiceModel>();
        public static List<InvoiceModel> filteredInvoices = new List<InvoiceModel>();
        public static volatile bool isInvoiceInitFinish = false;
        public static string PathPDFBrowser = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe";
        public static string PathData = @$"{Environment.CurrentDirectory}\data\";
        public static string PathInvoices = @$"{Environment.CurrentDirectory}\data\invoices\";
        public static string InvoicesJsonFileName = "Invoices.json";
        public static string ConfigJsonFileName = "Config.json";
        public static string UILanguage = "English";
        public static string[] UILanguages = { "English", "German" };
        public const string PROGRAM_VERSION = "1.1.3.2";
        //0 = dark mode  | 1 = white mode
        public static int REGSystemUsesLightTheme = 1;


        public static void InitWorkPath()
        {
            //create/check the need folders and files
            Directory.CreateDirectory(EnvironmentsVariable.PathData);
            Directory.CreateDirectory(EnvironmentsVariable.PathInvoices);
            if (!File.Exists(EnvironmentsVariable.PathData + EnvironmentsVariable.InvoicesJsonFileName))
                File.WriteAllText(EnvironmentsVariable.PathData + EnvironmentsVariable.InvoicesJsonFileName, "[]");
            if (!File.Exists(EnvironmentsVariable.PathData + EnvironmentsVariable.ConfigJsonFileName))
                File.WriteAllText(EnvironmentsVariable.PathData + EnvironmentsVariable.ConfigJsonFileName, "[]");
        }
    }
}
