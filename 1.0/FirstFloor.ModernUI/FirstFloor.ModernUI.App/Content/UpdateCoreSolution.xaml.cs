using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.SharePoint.Administration;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Forms;
using FirstFloor.ModernUI.Windows.Controls;
using FirstFloor.ModernUI.App.Properties;
using System.Text.RegularExpressions;
using System.Linq;

namespace FirstFloor.ModernUI.App.Content
{
    public partial class UpdateCoreSolution : System.Windows.Controls.UserControl
    {
        public UpdateCoreSolution()
        {
            InitializeComponent();
        }
        public class webapp
        {
            public bool IsSelected { get; set; }
            public string Title { get; set; }
            public string Url { get; set; }
            public bool IsDeployed { get; set; }
        }

        public string webURL { get; set; }
        public string filename;
        public string solution;
        public SPSolution coresolution;
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
        public Collection<SPWebApplication> selectedWebApps = new Collection<SPWebApplication>();
        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            var currentCB = (System.Windows.Controls.CheckBox)sender;
            if (currentCB.IsChecked == true)
            {
                int rowindex = DG1.SelectedIndex;
                var selected = DG1.Items[rowindex] as webapp;
                webURL = selected.Url;
                SPWebApplication webApplication = SPWebApplication.Lookup(new Uri(webURL));
                selectedWebApps.Add(webApplication);
                if (DG1.CancelEdit())
                {
                    DG1.Items.Refresh();
                }
            }
            if (currentCB.IsChecked != true)
            {
                int rowindex = DG1.SelectedIndex;
                var selected = DG1.Items[rowindex] as webapp;
                webURL = selected.Url;
                SPWebApplication webApplication = SPWebApplication.Lookup(new Uri(webURL));
                selectedWebApps.Remove(webApplication);
                if (DG1.CancelEdit())
                {
                    DG1.Items.Refresh();
                }
            }
        }

        private void GetWebs_Click(object sender, RoutedEventArgs e)
        {
            selectedWebApps.Clear();
            /*var settigs = Settings.Default;
            coresolution = SP.GetCoreSolution();
            if (coresolution == null)
            {
                ModernDialog.ShowMessage("Core solution not found!\nCheck app settings!", "Error", MessageBoxButton.OK);
                return;
            }*/

            Dispatcher.Invoke(() =>
            {
                progressbar.Visibility = Visibility.Visible;
                DG1.Visibility = Visibility.Hidden;

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
                    w.IsDeployed = SP.IsSolutionDeployedToWeb(Settings.Default.CoreSolution, web);
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
                    Log.Visibility = Visibility.Visible;
                });
                worker.Dispose();
            };
            worker.RunWorkerAsync();
        }

        private void BtnRetract_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                Log.Visibility = Visibility.Visible;
                progressbar.Visibility = Visibility.Visible;
                DG1.Visibility = Visibility.Hidden;

            });
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (o, ea) =>
            {
                //deactivate features
                var WebApps = SP.GetWebsFromSolution(Settings.Default.CoreSolution);
                if (WebApps.Any())
                {
                    foreach (SPWebApplication web in WebApps)
                    {
                        LogMessage(SP.DeActivateCoreFeatures(web.GetResponseUri(SPUrlZone.Default).AbsoluteUri));
                    }
                    //do solution retract
                    LogMessage("\n" + SP.RetractCoreSolution());
                    selectedWebApps.Clear();
                }
                else { LogMessage("\nSolution is not deployed to any Web Application!\nBut you can deploy it using buttons 3 and 4."); }
            };
            worker.RunWorkerCompleted += (o, ea) =>
            {
                Dispatcher.Invoke(() =>
                {
                    progressbar.Visibility = Visibility.Hidden;
                    DG1.Visibility = Visibility.Hidden;
                    Log.Visibility = Visibility.Visible;
                    // BtnFileOpen.Visibility = Visibility.Hidden;
                });
                worker.Dispose();
            };
            worker.RunWorkerAsync();
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
                    Log.Visibility = Visibility.Visible;
                    LogMessage("\nAdding core solution to the solution store...");
                    try
                    {
                        Dispatcher.Invoke(() =>
                        {
                            progressbar.Visibility = Visibility.Visible;
                            DG1.Visibility = Visibility.Hidden;

                        });
                        BackgroundWorker worker = new BackgroundWorker();
                        worker.DoWork += (o, ea) =>
                        {
                            LogMessage("\n" + SP.AddSolution(filename));
                        };
                        worker.RunWorkerCompleted += (o, ea) =>
                        {
                            Dispatcher.Invoke(() =>
                            {
                                progressbar.Visibility = Visibility.Hidden;
                                DG1.Visibility = Visibility.Visible;
                            });
                            worker.Dispose();
                        };
                        worker.RunWorkerAsync();
                    }
                    catch (Exception ex)
                    {
                        ModernDialog.ShowMessage(ex.Message, "Error", MessageBoxButton.OK);
                    }
                    break;
                case DialogResult.Cancel:
                default:
                    filename = null;
                    LogMessage("\nPlease select WSP file!");
                    break;
            }
        }

        private void BtnDeploy_Click(object sender, RoutedEventArgs e)
        {
            if (selectedWebApps.Count == 0)
            {
                ModernDialog.ShowMessage("Please select at leas one Web Application!", "Error", MessageBoxButton.OK);
                return;
            }
            Dispatcher.Invoke(() =>
            {
                progressbar.Visibility = Visibility.Visible;
                DG1.Visibility = Visibility.Hidden;
            });
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (o, ea) =>
            {
                solution = SP.GetCoreSolution().Name;
                LogMessage("\nDeploying...");
                if (!SP.DeploySolution(solution, selectedWebApps))
                {
                    LogMessage("\nDeployment error: " + SP.SoltionDeploymentStatus(solution));
                }
                else
                {
                    LogMessage("\nCore solution is deployed.");
                    foreach (SPWebApplication web in SP.GetCoreSolution().DeployedWebApplications)
                    {
                        LogMessage(SP.ActivateCoreFeatures(web.GetResponseUri(SPUrlZone.Default).AbsoluteUri));
                    }
                    LogMessage("\nOperation completed successfully!");

                }
            };
            worker.RunWorkerCompleted += (o, ea) =>
            {
                Dispatcher.Invoke(() =>
                {
                    progressbar.Visibility = Visibility.Hidden;
                    DG1.Visibility = Visibility.Hidden;
                });
                worker.Dispose();
            };
            worker.RunWorkerAsync();

        }
    }
}
