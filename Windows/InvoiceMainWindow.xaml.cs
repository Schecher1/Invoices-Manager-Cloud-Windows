﻿using InvoicesManager.Classes;
using InvoicesManager.Classes.Enums;
using InvoicesManager.Core;
using InvoicesManager.Models;
using InvoicesManager.Windows;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace InvoicesManager
{
    public partial class InvoiceMainWindow : Window
    {
        private string filterReference = String.Empty;
        private string filterInvoiceNumber = String.Empty;
        private string filterOrganization = "-1";
        private string filterDocumentType = "-1";
        private DateTime filterExhibitionDate = default;

        public InvoiceMainWindow()
        {
#if DEBUG
           // try { File.Delete(EnvironmentsVariable.PathData + EnvironmentsVariable.InvoicesJsonFileName); } catch  {}
#endif
            //load the window 
            InitializeComponent();
            //init threads
            InitThreads();
            //check for auto backup
            if (EnvironmentsVariable.CreateABackupEveryTimeTheProgramStarts)
            {
                Task.Run(() =>
                {
                    BackUpSystem.BackUp(Path.Combine(EnvironmentsVariable.PathBackUps, DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".bkup"), this);
                    BackUpSystem.CheckBackUpCount();
                });
            }

#if DEBUG
            //GenerateDebugDataRecords();
            //RefreshDataGridWithInit();
#endif
        }

        private void Window_Closed(object sender, EventArgs e)
            => EnvironmentsVariable.Window_Invoice_IsClosed = true;

        private void InitWindowsTheme()
        {
            //read the registry key
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            //get the value
            object value = key.GetValue("SystemUsesLightTheme");
            //set the theme
            if (Convert.ToInt32(value) == 0)
                EnvironmentsVariable.REGSystemUsesLightTheme = 0;
            else
                EnvironmentsVariable.REGSystemUsesLightTheme = 1;
        }
        
        private void GenerateDebugDataRecords()
        {
            Random r = new Random();
            string[] sampleOrganization = { "UPS", "MCDonalds", "Telekom", "DHL", "Amazon", "Apple", "Microsoft", "Google", "Facebook", "Twitter" };
            string[] sampleDocumenttype = { "Invoice", "Bill" };

            for (int i = 0; i < r.Next(35, 125); i++)
            {
                InvoiceModel invoice = new InvoiceModel();
                invoice.FileID = "test";
                invoice.CaptureDate = DateTime.Now.AddDays(r.Next(-100, 100));
                invoice.ExhibitionDate = DateTime.Now.AddDays(r.Next(-100, 100));
                invoice.Reference = "REF-" + r.Next(100000, 999999).ToString();
                invoice.DocumentType = sampleDocumenttype[r.Next(0, sampleDocumenttype.Length)];
                invoice.Organization = sampleOrganization[r.Next(0, sampleOrganization.Length)];
                invoice.InvoiceNumber = "INV-NR" + r.Next(100000, 999999).ToString();
                invoice.Tags = new string[] { "Tag1", "Tag2", "Tag3" };
                invoice.MoneyTotal = r.Next(100, 9999);
                invoice.ImportanceState = (ImportanceStateEnum)r.Next(0, 3);
                invoice.MoneyState = (MoneyStateEnum)r.Next(0, 2);
                invoice.PaidState = (PaidStateEnum)r.Next(0, 2);

                EnvironmentsVariable.AllInvoices.Add(invoice);
            }

            //InvoiceSystem.SaveIntoJsonFile();
        }

        private void InitThreads()
        {
            Thread _initInvoicesThread = new Thread(InvoiceSystem.Init);
            Thread _initOrganizationsThread = new Thread(ThreadTaskInitOrganization);
            Thread _initDocumentType = new Thread(ThreadTaskInitDocumentType);
            Thread _refreshDataGridThread = new Thread(ThreadTaskRefreshDataGrid);
            Thread _initNotebooks = new Thread(ThreadTaskInitNotebooks);

            _initInvoicesThread.Start();
            _initInvoicesThread.Join();

            _initOrganizationsThread.Start();
            _initOrganizationsThread.Join();

            _initDocumentType.Start();
            _initDocumentType.Join();

            _refreshDataGridThread.Start();
            _refreshDataGridThread.Join();

            _initNotebooks.Start();
            _initNotebooks.Join();
        }

        private void InitInvoices()
        {
            Thread _initInvoicesThread = new Thread(InvoiceSystem.Init);
            _initInvoicesThread.Priority = ThreadPriority.Highest;
            _initInvoicesThread.Start();
        }

        private void InitOrganization()
        {
            Thread _initOrganizationsThread = new Thread(ThreadTaskInitOrganization);
            _initOrganizationsThread.Priority = ThreadPriority.Normal;
            _initOrganizationsThread.Start();
        }

        private void InitDocumentType()
        {
            Thread _initDocumentType = new Thread(ThreadTaskInitDocumentType);
            _initDocumentType.Priority = ThreadPriority.Normal;
            _initDocumentType.Start();
        }

        private void RefreshDataGrid()
        {
            Thread _refreshDataGridThread = new Thread(ThreadTaskRefreshDataGrid);
            _refreshDataGridThread.Priority = ThreadPriority.Normal;
            _refreshDataGridThread.Start();
        }

        private void InitNotebooks()
        {
            Thread _initNotebooks = new Thread(ThreadTaskInitNotebooks);
            _initNotebooks.Priority = ThreadPriority.Normal;
            _initNotebooks.Start();
        }

        private void RefreshDataGridWithInit()
        {
            InitInvoices();
            InitOrganization();
            InitDocumentType();
            RefreshDataGrid();
        }

        private void ThreadTaskInitOrganization()
        {
            //sleep to wait for the init thread
            WaiterSystem.WaitUntilInvoiceInitFinish();

            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(()
                     => { Comb_Search_Organization.Items.Clear(); }));

            //it must be done, otherwise the exception will be thrown (
            //System.InvalidOperationException: "Collection was modified; enumeration operation may not execute".)
            List<InvoiceModel> allInvoices = new List<InvoiceModel>(EnvironmentsVariable.AllInvoices);

            foreach (var organization in allInvoices.Select(x => x.Organization).Distinct())
                    Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(()
                        => { Comb_Search_Organization.Items.Add(organization); }));
        }

        private void ThreadTaskInitDocumentType()
        {
            //sleep to wait for the init thread
            WaiterSystem.WaitUntilInvoiceInitFinish();

            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(()
                => { Comb_Search_DocumentType.Items.Clear(); }));

            foreach (var documenttype in EnvironmentsVariable.AllInvoices.Select(x => x.DocumentType).Distinct())
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(()
                    => { Comb_Search_DocumentType.Items.Add(documenttype); }));
        }

        private void ThreadTaskInitNotebooks()
        {
            NotebookSystem.Init();
        }

            private void ThreadTaskRefreshDataGrid()
        {
            //sleep to wait for the init thread
            WaiterSystem.WaitUntilInvoiceInitFinish();

            SortSystem sortSys = new SortSystem(EnvironmentsVariable.AllInvoices, filterReference, filterInvoiceNumber, filterOrganization, filterDocumentType , filterExhibitionDate);

            sortSys.Sort();

            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(()
                => { Dg_Invoices.Items.Clear(); }));
            
            foreach (var invoice in EnvironmentsVariable.FilteredInvoices)
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(()
                    => { Dg_Invoices.Items.Add(invoice); }));

            //set bottom status bar
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                string wordInvoice = Application.Current.Resources["invoices"] as string;
                var wordFrom = Application.Current.Resources["from"] as string;
                MsgBox_InvoiceCounter.Content = $"{wordInvoice}:  {EnvironmentsVariable.FilteredInvoices.Count} {wordFrom} {EnvironmentsVariable.AllInvoices.Count}";
            }));
        }
        
        private void DG_Invoices_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //happens sometimes, therefore this catch
            if (Dg_Invoices.SelectedItem == null)
                return;

            var cellInfo = Dg_Invoices.CurrentCell;
            var column = cellInfo.Column as DataGridBoundColumn;

            //if the colum is null (for whatever reason) then:
            //you clicked on a "open" hyperlink colum (it will be open the file)
            //      else
            //you copy the column value into you Clipboard
            if (column != null)
            {
                var element = new FrameworkElement() { DataContext = cellInfo.Item };
                BindingOperations.SetBinding(element, TagProperty, column.Binding);
                var cellValue = element.Tag;
                Clipboard.SetText(cellValue.ToString());

                //must, otherwise you save it in your clipboard and open the file
                return;
            }

            InvoiceModel invoice = Dg_Invoices.SelectedItem as InvoiceModel;

            //copy file to temp folder and open it then delete it
            string tempPath = Path.Combine(Path.GetTempPath(), invoice.FileID + ".pdf");
            string sourcePath = Path.Combine(EnvironmentsVariable.PathInvoices, invoice.FileID + ".pdf");
            File.Copy(sourcePath, tempPath, true);
            Process.Start(EnvironmentsVariable.PathPDFBrowser, tempPath);

            //this program has to wait, so the pdf browser can open it
            //otherwise he is faster with delete than the document can be displayed
            Thread.Sleep(1000);

            File.Delete(tempPath);
        }

        private void Bttn_BoardRefresh_Click(object sender, RoutedEventArgs e)
        {
            EnvironmentsVariable.InitWorkPath();
            RefreshDataGridWithInit();
        }

        private void Bttn_InvoiceAdd_Click(object sender, RoutedEventArgs e)
        {
            OpenInvoiceView(InvoiceViewModeEnum.InvoiceAdd);

            RefreshDataGridWithInit();
        }

        private void Bttn_InvoiceEdit_Click(object sender, RoutedEventArgs e)
        {
            if (Dg_Invoices.SelectedItem == null)
                return;

            OpenInvoiceView(InvoiceViewModeEnum.InvoiceEdit);

            RefreshDataGridWithInit();
        }

        private void Bttn_InvoiceRemove_Click(object sender, RoutedEventArgs e)
        {
            if (Dg_Invoices.SelectedItem == null)
                return;

            OpenInvoiceView(InvoiceViewModeEnum.InvoiceDelete);

            RefreshDataGridWithInit();
        }

        private void OpenInvoiceView(InvoiceViewModeEnum invoiceViewModeEnum)
        {
            InvoiceViewWindow invoiceViewWindow = new InvoiceViewWindow(invoiceViewModeEnum, (InvoiceModel)Dg_Invoices.SelectedItem);
            invoiceViewWindow.ShowDialog();
        }

        private void Bttn_InvoiceSaveAs_Click(object sender, RoutedEventArgs e)
        {
            if (Dg_Invoices.SelectedItem == null)
                return;

            InvoiceSaveAsWindow _invoiceSaveAsWindow = new InvoiceSaveAsWindow((InvoiceModel)Dg_Invoices.SelectedItem);
            _invoiceSaveAsWindow.ShowDialog();
        }

        private async void Bttn_BackUpCreate_Click(object sender, RoutedEventArgs e)
        {
            //can be removed, if the backup also include the notebook
            MessageBox.Show(Application.Current.Resources["warningNotebookNotIncludedInBackUp"] as string);

            SaveFileDialog sfg = new SaveFileDialog()
            {
                Filter = "BackUp-Datei (*.bkup)|*.bkup",
                RestoreDirectory = true
            };

            if (sfg.ShowDialog() == true)
            {
                await Task.Run(() =>
                {
                    if (BackUpSystem.BackUp(sfg.FileName, this))
                        MessageBox.Show(Application.Current.Resources["backUpSuccessfully"] as string);
                    else
                        MessageBox.Show(Application.Current.Resources["backUpFailed"] as string);
                });
            }
        }

        private async void Bttn_BackUpRestore_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog()
            {
                Filter = "BackUp-Datei (*.bkup)|*.bkup"
            };

            if (ofd.ShowDialog() == true)
            {
                await Task.Run(() =>
                {
                    if (!BackUpSystem.Restore(ofd.FileName, this))
                        MessageBox.Show(this.Resources["backUpFailedRestored"] as string);

                    RefreshDataGridWithInit();
                });
            }
        }

        private void Bttn_Settings_Click(object sender, RoutedEventArgs e)
        {
            SettingWindow _settingWindow = new SettingWindow();
            _settingWindow.ShowDialog();

            RefreshDataGridWithInit();
        }

        private void Bttn_About_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow _aboutWindow = new AboutWindow();
            _aboutWindow.ShowDialog();
        }

        private void Bttn_OpenNotebook_Click(object sender, RoutedEventArgs e)
        {
            NotebookWindow _notebookWindow = new NotebookWindow();
            _notebookWindow.Topmost = true;  
            _notebookWindow.Show();
        }


        private void Tb_Search_String_TextChanged(object sender, TextChangedEventArgs e)
        {
            filterReference = Tb_Search_String.Text == String.Empty ? String.Empty : Tb_Search_String.Text;
            RefreshDataGrid();
        }

        private void Tb_Search_InvoiceNumber_TextChanged(object sender, TextChangedEventArgs e)
        {
            filterInvoiceNumber = Tb_Search_InvoiceNumber.Text == String.Empty ? String.Empty : Tb_Search_InvoiceNumber.Text;
            RefreshDataGrid();
        }

        private void Comb_Search_Organization_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            filterOrganization = Comb_Search_Organization.SelectedIndex.ToString() == "-1" ? "-1" : Comb_Search_Organization.SelectedItem.ToString();
            RefreshDataGrid();
        }

        private void Comb_Search_Organization_Clear_Click(object sender, RoutedEventArgs e)
            => Comb_Search_Organization.SelectedIndex = -1;

        private void Comb_Search_DocumentType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            filterDocumentType = Comb_Search_DocumentType.SelectedIndex.ToString() == "-1" ? "-1" : Comb_Search_DocumentType.SelectedItem.ToString();
            RefreshDataGrid();
        }

        private void Comb_Search_DocumentType_Clear_Click(object sender, RoutedEventArgs e)
             => Comb_Search_DocumentType.SelectedIndex = -1;

        private void Dp_Search_ExhibitionDate_Clear_Click(object sender, RoutedEventArgs e)
           => Dp_Search_ExhibitionDate.SelectedDate = null;

        private void Dp_Search_ExhibitionDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            filterExhibitionDate = (DateTime)(Dp_Search_ExhibitionDate.SelectedDate == null ? default(DateTime) : Dp_Search_ExhibitionDate.SelectedDate);
            RefreshDataGrid();
        }

        public void ClearInfoProgressBar()
        {
            try
            {
                PB_InfoProgressBar.Dispatcher.Invoke(new Action(() =>
                {
                    PB_InfoProgressBar.Value = 0;
                }));
            }
            catch { }
        }
        
        public  void SetInfoProgressBarValue(int value)
        {
            try
            {
                PB_InfoProgressBar.Dispatcher.Invoke(new Action(() =>
                {
                    PB_InfoProgressBar.Value += value;
                }));
            }
            catch {}
        }
        
        public void SetInfoProgressMaxValue(int value)
        {
            try
            {
                PB_InfoProgressBar.Dispatcher.Invoke(new Action(() =>
                {
                    PB_InfoProgressBar.Maximum = value;
                }));
            }
            catch { }
        }
    }
}
