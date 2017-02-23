using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Net.NetworkInformation;
using System.Net;
using System.Windows.Forms;
using FirstFloor.ModernUI.Windows.Controls;

namespace FirstFloor.ModernUI.App.Content
{
    public class site
    {
        public bool IsSelected { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
    }
   
    public partial class CloneSite : System.Windows.Controls.UserControl
    {
        public SharePoint SP = new SharePoint();
        public SPSolution custom;
        public string siteURL { get; set; }
        public int Port;
        public string WaName;
        private string eventLogMessage;
        public Uri newURL;
        public string username = Environment.UserDomainName + "\\" + Environment.UserName;
        public bool successStatus = true;
        public string filename;
        public Collection<SPWebApplication> selectedWebApps = new Collection<SPWebApplication>();
        public static bool PortInUse(int port)
        {
            bool inUse = false;
            IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] ipEndPoints = ipProperties.GetActiveTcpListeners();
            foreach (IPEndPoint endPoint in ipEndPoints)
            {
                if (endPoint.Port == port)
                {
                    inUse = true;
                    break;
                }
            }
            return inUse;
        }
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
        public CloneSite()
        {
            InitializeComponent();
            TextEvents.Text = eventLogMessage;
        }
        private void GetWebs_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                progressbar.Visibility = Visibility.Visible;
                Clone.Visibility = Visibility.Hidden;
                CreateWebApp.Visibility = Visibility.Hidden;
                Form.Visibility = Visibility.Hidden;
                Log.Visibility = Visibility.Hidden;
            });
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (o, ea) =>
            {
                List<site> sitesCollection = new List<site>();
                List<SPSite> spSites = SP.GetAllSPSites();
                foreach (var spsite in spSites)
                {
                    site s = new site();
                    s.IsSelected = false;
                    s.Title = spsite.WebApplication.Name;
                    s.Url = spsite.Url;
                    sitesCollection.Add(s);
                }
                //use the Dispatcher to delegate the listOfStrings collection back to the UI
                Dispatcher.Invoke(() => DG1.ItemsSource = sitesCollection);
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
                var selected = DG1.Items[rowindex] as site;
                siteURL = selected.Url;
                for (int i = 0; i < DG1.Items.Count; i++)
                {
                    if (rowindex != i)
                    {
                        var item = DG1.Items[i] as site;
                        item.IsSelected = false;
                    }
                }
                DG1.Items.Refresh();
                Clone.Visibility = Visibility.Visible;
            }
            else
            {
                Clone.Visibility = Visibility.Hidden;
            }
        }

        private void Clone_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                DG1.Visibility = Visibility.Hidden;
                Clone.Visibility = Visibility.Hidden;
                CreateWebApp.Visibility = Visibility.Visible;
                Form.Visibility = Visibility.Visible;
                Log.Visibility = Visibility.Visible;

            });
        }
        //take site collection backup and create new webapp
        private void Nex_Button_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PortNumber.Text) || string.IsNullOrWhiteSpace(NewWaName.Text))
            {
                ModernDialog.ShowMessage("Name and Port Number can't be empty! ", "Error", MessageBoxButton.OK);
                return;
            }
            if (PortInUse(Int32.Parse(PortNumber.Text)))
            {
                ModernDialog.ShowMessage("Specified port is already in use. Please chose different port!", "Error", MessageBoxButton.OK);
                return;
            }

            Dispatcher.Invoke(() =>
            {
                progressbar.Visibility = Visibility.Visible;
                CreateWebApp.Visibility = Visibility.Hidden;
                Form.Visibility = Visibility.Hidden;
            });
            BackgroundWorker worker_backup = new BackgroundWorker();
            //start clone process
            worker_backup.DoWork += (o, ea) =>
            {
                SPWebApplication wa = SPWebApplication.Lookup(new Uri(siteURL));
                SPSiteCollection sites = wa.Sites;
                Dispatcher.Invoke(() =>
                {
                    progressbar.Visibility = Visibility.Visible;
                    Log.Visibility = Visibility.Visible;
                });
                LogMessage("Taking backup of a site collection " + siteURL);
                try
                {
                    sites.Backup(siteURL, "C:\\Windows\\Temp\\spsite_backup.cmp", true);
                    LogMessage("\nBackup is stored at C:\\Windows\\Temp\\spsite_backup.cmp");
                }
                catch (Exception ex)
                {
                    ModernDialog.ShowMessage(ex.Message, "Exception", MessageBoxButton.OK);
                    return;
                }
                LogMessage("\nCreating new Web Application...");
                //create new web app
                try
                {
                    Dispatcher.Invoke(() =>
                    {
                        Port = Int32.Parse(PortNumber.Text);
                        WaName = NewWaName.Text;
                        GetWebs.Visibility = Visibility.Hidden;
                    });
                    SPSecurity.RunWithElevatedPrivileges(delegate ()
                    {
                        SPWebApplicationBuilder webAppBuilder = new SPWebApplicationBuilder(SPFarm.Local);
                        webAppBuilder.Port = Port;
                        webAppBuilder.ServerComment = WaName;
                        webAppBuilder.ApplicationPoolId = WaName + " - " + Port.ToString();
                        webAppBuilder.IdentityType = IdentityType.SpecificUser;
                        webAppBuilder.ManagedAccount = new SPFarmManagedAccountCollection(SPFarm.Local).FirstOrDefault();
                        webAppBuilder.DatabaseName = "Wss_Content_" + WaName.Replace(" ", string.Empty) + "_" + Port.ToString();
                        webAppBuilder.RootDirectory = new System.IO.DirectoryInfo("C:\\Inetpub\\wwwroot\\wss\\VirtualDirectories\\" + WaName.Replace(" ", string.Empty) + "_" + Port.ToString());
                        webAppBuilder.UseNTLMExclusively = true;
                        SPWebApplication newWebApp = webAppBuilder.Create();
                        newWebApp.Provision();
                        newURL = newWebApp.GetResponseUri(SPUrlZone.Default);
                        LogMessage("\nNew Web Application created.");
                        LogMessage("\nRestoring site collection to new Web Application...");
                    });
                        //restore site collection
                        SPWebApplication webApplication = SPWebApplication.Lookup(new Uri(newURL.AbsoluteUri));
                        webApplication.UseClaimsAuthentication = true;
                        webApplication.GrantAccessToProcessIdentity(username);
                        webApplication.Update();
                        SPSiteCollection sitecols = webApplication.Sites;
                        sitecols.Restore("/", "C:\\Windows\\Temp\\spsite_backup.cmp", true);
                        LogMessage("\nSite collection restored to URL: " + newURL.AbsoluteUri);
                        //solution deployment
                        LogMessage("\nSoltion deployment:");
                        LogMessage("\nDeploying SharePointLearningKit solution...");
                        selectedWebApps.Add(webApplication);
                        //SLK first
                        if (!SP.DeploySolution("sharepointlearningkit.wsp", selectedWebApps))
                        {
                            LogMessage("\nSharePointLearningKit solution is not deployed.");
                            LogMessage("\n" + SP.SoltionDeploymentStatus("sharepointlearningkit.wsp"));
                        }
                        else
                        {
                            LogMessage("\nSharePointLearningKit solution deployed.");
                            LogMessage("\n" + SP.SoltionDeploymentStatus("sharepointlearningkit.wsp"));
                        }
                        
                        LogMessage("\nDeploying Lanteria Core solution...");
                        //Lanteria core lanteria.effectivestaff.wsp
                        if (!SP.DeploySolution("lanteria.effectivestaff.wsp", selectedWebApps))
                        {
                            LogMessage("\nlanteria.effectivestaff.wsp solution is not deployed.");
                            LogMessage("\n" + SP.SoltionDeploymentStatus("lanteria.effectivestaff.wsp"));
                            successStatus = false;
                            return;
                        }
                        else
                        {
                            LogMessage("\nlanteria.effectivestaff.wsp solution deployed.");
                            LogMessage("\n" + SP.SoltionDeploymentStatus("lanteria.effectivestaff.wsp"));

                        }

                    //activate feature
                    LogMessage("\nActivating features");
                    LogMessage(SP.ActivateCoreFeatures("lanteria.effectivestaff.wsp", newURL.AbsoluteUri));
                    LogMessage("\nClone process ended.");

                }
                catch (Exception ex)
                {
                    ModernDialog.ShowMessage(ex.Message, "Exception", MessageBoxButton.OK);
                    LogMessage("\nError occured: " + ex.Message + "\n" + ex.InnerException);
                    successStatus = false;
                    return;
                }
            };
            worker_backup.RunWorkerCompleted += (o, ea) =>
            {
                Dispatcher.Invoke(() =>
                {
                    progressbar.Visibility = Visibility.Hidden;
                    Clone.Visibility = Visibility.Hidden;
                    GetWebs.Visibility = Visibility.Visible;

                });
                worker_backup.Dispose();
                if (successStatus == true)
                {
                    MessageBoxResult result = ModernDialog.ShowMessage("Would you like to deploy custom solution on clonned Web Application?", "Custom solution deployment", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.Yes)
                    {
                        FileOpen();
                    }
                }
            };

            worker_backup.RunWorkerAsync();
        }


        private void PortNumber_Validation(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);

        }
        private void WebAppName_Validation(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^a-zA-Z0-9]+");
            e.Handled = regex.IsMatch(e.Text);
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
                    if (SP.SolutionExist(fileDialog.SafeFileName))
                    {
                        ModernDialog.ShowMessage("Solution is allready present in farm solutions", "Error", MessageBoxButton.OK);
                        break;
                    }
                    else
                    {
                        CustomSolutionDeploy();
                        break;
                    }
                   
                case DialogResult.Cancel:
                default:
                    filename = null;
                    LogMessage("\nPlease select WSP file!");
                    break;
            }
        }

        private void CustomSolutionDeploy()
        {
            BackgroundWorker deploy_custom = new BackgroundWorker();
            //start clone process
            deploy_custom.DoWork += (o, ea) =>
            {
                Dispatcher.Invoke(() =>
                {
                    BtnFileOpen.Visibility = Visibility.Hidden;
                    GetWebs.Visibility = Visibility.Hidden;
                    TextEvents.Visibility = Visibility.Visible;
                    progressbar.Visibility = Visibility.Visible;
                });
                LogMessage("\nCustom solution selected: " + filename);
                LogMessage("\nStarting custom solution deployment...");
                SPSolution custom = SPFarm.Local.Solutions.Add(filename);
                if (!SP.DeploySolution(custom.Name, selectedWebApps))
                {
                    LogMessage("\nCustom solution is not deployed.");
                    LogMessage("\n" + SP.SoltionDeploymentStatus(custom.Name));
                    return;
                }
                else
                {
                    LogMessage("\nCustom solution deployed.");
                    LogMessage("\n" + SP.SoltionDeploymentStatus(custom.Name));
                }

                LogMessage("Activating features from custom solution...");
                LogMessage(SP.ActivateFeaturesFromCustomSolution(custom.Name, newURL.AbsoluteUri));
                LogMessage("\nDone");
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
