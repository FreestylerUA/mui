using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.SharePoint.Administration;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Forms;
using FirstFloor.ModernUI.Windows.Controls;
using System.Text.RegularExpressions;
using System.Linq;
using Microsoft.SharePoint;

namespace FirstFloor.ModernUI.App.Content
{

    public partial class UpdateCustom : System.Windows.Controls.UserControl
    {
        public UpdateCustom()
        {
            InitializeComponent();
        }

        public class webapp
        {
            public bool IsSelected { get; set; }
            public string Title { get; set; }
            public string Url { get; set; }
        }
        public class siteCol
        {
            public bool IsSelected { get; set; }
            public string Url { get; set; }
        }
        public string webURL { get; set; }
        public string siteURL { get; set; }
        public string filename;
        public string solution;
        private string eventLogMessage;
        private void LogMessage(string message, params object[] o)
        {
            message = string.Format(CultureInfo.CurrentUICulture, message, o);

            if (TextEvents == null)
            {
                Dispatcher.Invoke(() =>
                {
                    eventLogMessage += message;
                });
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    TextEvents.AppendText(message);
                    TextEvents.Focus();
                    TextEvents.CaretIndex = TextEvents.Text.Length;
                    TextEvents.ScrollToEnd();
                });
            }
        }
        public SharePoint SP = new SharePoint();

        private void GetWebs_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                progressbar.Visibility = Visibility.Visible;

            });
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (o, ea) =>
            {
                List<webapp> webApplications = new List<webapp>();
                List<SPWebApplication> webApps = SP.GetAllWebs();
                foreach (var web in webApps)
                {
                    webapp w = new webapp();
                    w.IsSelected = false;
                    w.Title = web.Name;
                    w.Url = web.GetResponseUri(SPUrlZone.Default).AbsoluteUri;
                    webApplications.Add(w);
                }
                //use the Dispatcher to delegate the listOfStrings collection back to the UI
                Dispatcher.Invoke(() => DG1.ItemsSource = webApplications);
            };
            worker.RunWorkerCompleted += (o, ea) =>
            {
                Dispatcher.Invoke(() =>
                {
                    progressbar.Visibility = Visibility.Hidden;
                    DG1.Visibility = Visibility.Visible;
                    lblWebApps.Visibility = Visibility.Visible;
                    Log.Visibility = Visibility.Hidden;
                    BtnFileOpen.Visibility = Visibility.Hidden;
                });
                worker.Dispose();
            };
            Dispatcher.Invoke(() =>
            {
                progressbar.Visibility = Visibility.Visible;
                DG1.Visibility = Visibility.Hidden;
                lblWebApps.Visibility = Visibility.Hidden;
            });
            worker.RunWorkerAsync();
        }
        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            var currentCB = (System.Windows.Controls.CheckBox)sender;
            if (currentCB.IsChecked == true)
            {
                int rowindex = DG1.SelectedIndex;
                var selected = DG1.Items[rowindex] as webapp;
                webURL = selected.Url;
                for (int i = 0; i < DG1.Items.Count; i++)
                {
                    if (rowindex != i)
                    {
                        var item = DG1.Items[i] as webapp;
                        item.IsSelected = false;
                    }
                }
                if (DG1.CancelEdit())
                {
                    DG1.Items.Refresh();
                    List<siteCol> siteColls = new List<siteCol>();
                    List<SPSite> siteCollections = SP.GetAllSPSites().Where(s => s.WebApplication.GetResponseUri(0).AbsoluteUri == webURL).ToList();
                    foreach (var site in siteCollections)
                    {
                        siteCol s = new siteCol();
                        s.IsSelected = false;
                        s.Url = site.Url;
                        siteColls.Add(s);
                    }
                    DG2.ItemsSource = siteColls;
                    DG2.Visibility = Visibility.Visible;
                    lblSiteCols.Visibility = Visibility.Visible;
                }
            }
            else
            {
                DG2.Visibility = Visibility.Hidden;
                lblSiteCols.Visibility = Visibility.Hidden;
            }
        }
        private void CheckBox1_Click(object sender, RoutedEventArgs e)
        {
            var currentCB = (System.Windows.Controls.CheckBox)sender;
            if (currentCB.IsChecked == true)
            {
                BtnFileOpen.Visibility = Visibility.Visible;
                int rowindex = DG2.SelectedIndex;
                var selected = DG2.Items[rowindex] as siteCol;
                siteURL = selected.Url;
                for (int i = 0; i < DG2.Items.Count; i++)
                {
                    if (rowindex != i)
                    {
                        var item = DG2.Items[i] as siteCol;
                        item.IsSelected = false;
                    }
                }
                if (DG2.CancelEdit())
                {
                    DG2.Items.Refresh();
                }
            }
            else
            {
                siteURL = string.Empty;
                BtnFileOpen.Visibility = Visibility.Hidden;
            }
        }
        private void BtnFileOpen_Click(object sender, RoutedEventArgs e)
        {
            FileOpen();
        }
        private void FileOpen()
        {
            var fileDialog = new OpenFileDialog();
            fileDialog.Filter = "Solution files|*.wsp";
            var result = fileDialog.ShowDialog();
            switch (result)
            {
                case DialogResult.OK:
                    filename = fileDialog.FileName;
                    solution = fileDialog.SafeFileName;
                    //DG1.Visibility = Visibility.Hidden;
                    Log.Visibility = Visibility.Visible;
                    LogMessage("\nWeb Application selected: " + webURL);
                    LogMessage("\nCustom solution selected: " + filename);
                    MessageBoxResult alert = ModernDialog.ShowMessage("Deploy solution " + solution + " on selected Web Application and Site?", "Custom solution deployment", MessageBoxButton.YesNo);


                    if (alert == MessageBoxResult.Yes)
                     {
                         CustomSolutionDeploy();
                     }
                    break;
                case DialogResult.Cancel:
                default:
                    filename = null;
                    Log.Visibility = Visibility.Visible;
                    LogMessage("\nPlease select WSP file!");
                    break;
            }
        }

        private void CustomSolutionDeploy()
        {
            BackgroundWorker deploy_custom = new BackgroundWorker();
            deploy_custom.DoWork += (o, ea) =>
            {
                Dispatcher.Invoke(() =>
                {
                    GetWebs.Visibility = Visibility.Hidden;
                    BtnFileOpen.Visibility = Visibility.Hidden;
                    progressbar.Visibility = Visibility.Visible;
                });
                Collection<SPWebApplication> selectedWebApps = new Collection<SPWebApplication>();
                SPWebApplication webApplication = SPWebApplication.Lookup(new Uri(webURL));
                selectedWebApps.Add(webApplication);
                SPSite selectedSiteCollection  = webApplication.Sites.Where(s => s.Url == siteURL).FirstOrDefault();

                if (SPFarm.Local.Solutions.Any(x => x.Name.ToLower() == solution.ToLower()))
                {
                    //do solution retract
                    LogMessage("\n" + SP.DeactivateFeaturesFromCustomSolution(solution, siteURL));
                    LogMessage("\n" + SP.RetractSolution(solution));
                    LogMessage("\n" + SP.AddSolution(filename));
                }
                if (!SP.DeploySolution(solution, selectedWebApps))
                {
                    LogMessage("\nDeployment error: " + SP.SolutionDeploymentStatus(solution));
                }
                else
                {
                    LogMessage("\nCustom solution is deployed.");
                    LogMessage("\nActivating features...");
                    LogMessage(SP.ActivateFeaturesFromCustomSolution(solution, siteURL));
                 }
            };
            deploy_custom.RunWorkerCompleted += (o, ea) =>
            {
                Dispatcher.Invoke(() =>
                {
                    progressbar.Visibility = Visibility.Hidden;
                    GetWebs.Visibility = Visibility.Visible;
                });
            };
            deploy_custom.RunWorkerAsync();
        }
    }
}
