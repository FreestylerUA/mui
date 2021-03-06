﻿using System;
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

namespace FirstFloor.ModernUI.App.Content
{

    public partial class DeployCustom : System.Windows.Controls.UserControl
    {
        public DeployCustom()
        {
            InitializeComponent();
        }

        public class webapp
        {
            public bool IsSelected { get; set; }
            public string Title { get; set; }
            public string Url { get; set; }
        }
        public string webURL { get; set; }
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
                    Log.Visibility = Visibility.Hidden;
                    BtnFileOpen.Visibility = Visibility.Hidden;
                });
                worker.Dispose();
            };
            Dispatcher.Invoke(() =>
            {
                progressbar.Visibility = Visibility.Visible;
                DG1.Visibility = Visibility.Hidden;
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
                    BtnFileOpen.Visibility = Visibility.Visible;
                }


            }
            else
            {
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
                    DG1.Visibility = Visibility.Hidden;
                    Log.Visibility = Visibility.Visible;
                    LogMessage("\nWeb Application selected: " + webURL);
                    LogMessage("\nCustom solution selected: " + filename);
                    try
                    {
                        SPFarm.Local.Solutions.Add(filename);
                    }
                    catch (Exception ex)
                    {
                        ModernDialog.ShowMessage(ex.Message, "Error", MessageBoxButton.OK);
                        string findGuid = ex.Message;
                        MatchCollection guids = Regex.Matches(findGuid, @"(\{){0,1}[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}(\}){0,1}");
                        for (int i = 0; i < guids.Count; i++)
                        {
                            string Match = guids[i].Value;
                            SPSolution sol = SP.GetSolution(Match);
                            if (!sol.Deployed)
                            {
                                MessageBoxResult rmsol = ModernDialog.ShowMessage("Solution "+ sol.Name+" is not deployed.\nRemove?", "", MessageBoxButton.YesNo);
                                if (rmsol == MessageBoxResult.Yes)
                                {
                                    SPFarm.Local.Solutions.Remove(sol.Id);
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else
                            {
                                SPWebApplication wa = sol.DeployedWebApplications.FirstOrDefault();
                            }
                        }
                    }
                    LogMessage("\n" + SP.AddSolution(filename));
                    MessageBoxResult alert = ModernDialog.ShowMessage("Deploy solution " + solution + " on selected Web Application?", "Custom solution deployment", MessageBoxButton.YesNo);
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
                if (!SP.DeploySolution(solution, selectedWebApps))
                {
                    LogMessage("\nDeployment error: " + SP.SoltionDeploymentStatus(solution));
                }
                else
                {
                    LogMessage("\nCustom solution is deployed.");
                    LogMessage("\nActivating features...");
                    LogMessage(SP.ActivateFeaturesFromCustomSolution(solution, webURL));
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
