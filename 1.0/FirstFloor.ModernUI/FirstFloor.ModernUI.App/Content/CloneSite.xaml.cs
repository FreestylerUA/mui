using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using Microsoft.SharePoint.Administration.Claims;
using System.ComponentModel;
using System.Globalization;
using FirstFloor.ModernUI.Windows.Controls;
using System.Text.RegularExpressions;
using System.Net.NetworkInformation;
using System.Net;
using System.Threading;

namespace FirstFloor.ModernUI.App.Content
{
    public class webapp
    {
        public bool IsSelected { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
    }
    public class site
    {
        public bool IsSelected { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
    }

    public partial class CloneSite : UserControl
    {
        public string siteURL { get; set; }
        public int Port;
        public string WaName;
        private string eventLogMessage;
        public Uri newURL;
        public string username = Environment.UserDomainName + "\\" + Environment.UserName;
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

            if (this.TextEvents == null)
            {
                Dispatcher.Invoke((Action)(() => this.eventLogMessage += message));
            }
            else
            {
                Dispatcher.Invoke((Action)(() => this.TextEvents.AppendText(message)));
            }
        }

        public CloneSite()
        {
            InitializeComponent();
            this.TextEvents.Text = eventLogMessage;
        }

        public List<SPWebApplication> GetAllWebApplicationsInSPFarm()
        {
            var resultWebApplications = new List<SPWebApplication>();
            SPSecurity.RunWithElevatedPrivileges(delegate ()
            {
                SPServiceCollection services = SPFarm.Local.Services;
                foreach (SPService curService in services)
                {
                    if (curService is SPWebService)
                    {
                        var webService = (SPWebService)curService;
                        if (curService.TypeName.Equals("Microsoft SharePoint Foundation Web Application"))
                        {
                            webService = (SPWebService)curService;
                            SPWebApplicationCollection webApplications = webService.WebApplications;
                            foreach (SPWebApplication webApplication in webApplications)
                            {
                                if (webApplication != null)
                                {
                                    resultWebApplications.Add(webApplication);
                                }
                            }
                        }
                    }
                }
            });
            return resultWebApplications;
        }

        public List<SPSite> GetAllSPSites()
        {
            var resultSites = new List<SPSite>();
            SPSecurity.RunWithElevatedPrivileges(delegate ()
            {
                SPServiceCollection services = SPFarm.Local.Services;
                foreach (SPService curService in services)
                {
                    if (curService is SPWebService)
                    {
                        var webService = (SPWebService)curService;
                        if (curService.TypeName.Equals("Microsoft SharePoint Foundation Web Application"))
                        {
                            webService = (SPWebService)curService;
                            SPWebApplicationCollection webApplications = webService.WebApplications;
                            foreach (SPWebApplication webApplication in webApplications)
                            {
                                if (webApplication != null)
                                {
                                    foreach (SPSite site in webApplication.Sites)
                                    {
                                        resultSites.Add(site);
                                    }
                                }
                            }
                        }
                    }
                }
            });
            return resultSites;
        }

        private void StartProcess(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke((Action)(() => {
                progressbar.Visibility = Visibility.Visible;
                Clone.Visibility = Visibility.Hidden;
                CreateWebApp.Visibility = Visibility.Hidden;
                Form.Visibility = Visibility.Hidden;
                Log.Visibility = Visibility.Hidden;
            }));
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (o, ea) =>
            {
                /* List<webapp> ownCollection = new List<webapp>();
                 List<SPWebApplication> webApplications = GetAllWebApplicationsInSPFarm();
                 foreach (var wa in webApplications)
                 {
                     webapp n = new webapp();
                     n.IsSelected = false;
                     n.Title = wa.Name;
                     n.Url = wa.GetResponseUri(SPUrlZone.Default).AbsoluteUri;
                     ownCollection.Add(n);
                 }*/
                List<site> sitesCollection = new List<site>();
                List<SPSite> spSites = GetAllSPSites();
                foreach (var spsite in spSites)
                {
                    site s = new site();
                    s.IsSelected = false;
                    s.Title = spsite.WebApplication.Name;
                    s.Url = spsite.Url;
                    sitesCollection.Add(s);
                }
                //use the Dispatcher to delegate the listOfStrings collection back to the UI
                Dispatcher.Invoke((Action)(() => DG1.ItemsSource = sitesCollection));
            };
            worker.RunWorkerCompleted += (o, ea) =>
            {
              Dispatcher.Invoke((Action)(() => 
                {
                    progressbar.Visibility = Visibility.Hidden;
                    DG1.Visibility = Visibility.Visible;
                }));
            worker.Dispose();
            };
            Dispatcher.Invoke((Action)(() =>
                {
                    progressbar.Visibility = Visibility.Visible;
                    DG1.Visibility = Visibility.Hidden;
                }));
            worker.RunWorkerAsync();
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            var currentCB = (CheckBox)sender;
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
            Dispatcher.Invoke((Action)(() =>
            {
                DG1.Visibility = Visibility.Hidden;
                Clone.Visibility = Visibility.Hidden;
                CreateWebApp.Visibility = Visibility.Visible;
                Form.Visibility = Visibility.Visible;
                Log.Visibility = Visibility.Visible;

            }));
        }
        //take site collection backup and create new webapp
        private void Nex_Button_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PortNumber.Text) || string.IsNullOrWhiteSpace(NewWaName.Text))
            {
                MessageBox.Show("Name and Port Number can't be empty! ", "Exception", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (PortInUse(Int32.Parse(PortNumber.Text)))
            {
                MessageBox.Show("Specified port is already in use. Please chose different port!", "Exception", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Dispatcher.Invoke((Action)(() =>
            {
                progressbar.Visibility = Visibility.Visible;
                CreateWebApp.Visibility = Visibility.Hidden;
                Form.Visibility = Visibility.Hidden;
            }));
            BackgroundWorker worker_backup = new BackgroundWorker();
            worker_backup.DoWork += (o, ea) =>
            {
                SPWebApplication wa = SPWebApplication.Lookup(new Uri(siteURL));
                SPSiteCollection sites = wa.Sites;
                Dispatcher.Invoke((Action)(() =>
                {
                    progressbar.Visibility = Visibility.Visible;
                    Log.Visibility = Visibility.Visible;
                }));
                LogMessage("Taking backup of a site collection " + siteURL);
                try
                {
                   sites.Backup(siteURL, "C:\\Windows\\Temp\\spsite_backup.cmp", true);
                   LogMessage("\nBackup is stored at C:\\Windows\\Temp\\spsite_backup.cmp");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("A handled exception just occurred: " + ex.Message, "Exception", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                LogMessage("\nCreating new Web Application...");
                //create new web app
                try
                {
                    Dispatcher.Invoke((Action)(() =>
                    {
                        Port = Int32.Parse(PortNumber.Text);
                        WaName = NewWaName.Text;
                    }));
                    SPSecurity.RunWithElevatedPrivileges(delegate ()
                    {
                        SPWebApplicationBuilder webAppBuilder = new SPWebApplicationBuilder(SPFarm.Local);
                        webAppBuilder.Port = Port;
                        webAppBuilder.ServerComment = WaName;
                        webAppBuilder.ApplicationPoolId = WaName + " - " + Port.ToString();
                        webAppBuilder.IdentityType = IdentityType.SpecificUser;
                        webAppBuilder.ManagedAccount = new SPFarmManagedAccountCollection(SPFarm.Local).FirstOrDefault();
                        webAppBuilder.DatabaseName = "Wss_Content_" + WaName.Replace(" ", string.Empty)+"_"+Port.ToString();
                        webAppBuilder.RootDirectory = new System.IO.DirectoryInfo("C:\\Inetpub\\wwwroot\\wss\\VirtualDirectories\\" + WaName.Replace(" ", string.Empty) + "_" + Port.ToString());
                        webAppBuilder.UseNTLMExclusively = true;
                        SPWebApplication newWebApp = webAppBuilder.Create();
                        newWebApp.Provision();
                        newURL = newWebApp.GetResponseUri(SPUrlZone.Default);
                        LogMessage("\nNew Web Application created.");
                        LogMessage("\nRestoring site collection to new Web Application...");
                        //restore site collection
                        SPWebApplication webApplication = SPWebApplication.Lookup(new Uri(newURL.AbsoluteUri));
                        webApplication.GrantAccessToProcessIdentity(username);
                        SPSiteCollection sitecols = webApplication.Sites;
                        sitecols.Restore("/", "C:\\Windows\\Temp\\spsite_backup.cmp", true);
                        LogMessage("\nSite collection restored to URL: "+ newURL.AbsoluteUri);
                        //solution deployment
                        LogMessage("\nSoltion deployment:");
                        LogMessage("\nDeploying SharePointLearningKit solution...");
                        Collection<SPWebApplication> selectedWebApps =  new Collection<SPWebApplication>();
                        selectedWebApps.Add(webApplication);
                         //SLK first
                        SPSolution slk = SPFarm.Local.Solutions["sharepointlearningkit.wsp"];
                        slk.Deploy(DateTime.Now, true, selectedWebApps, false);
                        bool slkdeployed = slk.Deployed;
                        while (!slkdeployed)
                       {
                            Thread.Sleep(1000);
                            slkdeployed = slk.Deployed;
                        }

                        bool slkjobexists = slk.JobExists;
                        while (slkjobexists)
                        {
                            Thread.Sleep(1000);
                            slkjobexists = slk.JobExists;
                        }
                        LogMessage("\nSharePointLearningKit solution deployed.");
                        LogMessage("\nDeploying Lanteria Core solution...");
                        //Lanteria core lanteria.effectivestaff.wsp
                        SPSolution lanteria = SPFarm.Local.Solutions["lanteria.effectivestaff.wsp"];
                        lanteria.Deploy(DateTime.Now, true, selectedWebApps, false);
                        bool lanteriadeployed = lanteria.Deployed;
                        while (!lanteriadeployed)
                        {
                            Thread.Sleep(1000);
                            lanteriadeployed = lanteria.Deployed;
                        }

                        bool lanteriajobexists = lanteria.JobExists;
                        while (lanteriajobexists)
                        {
                            Thread.Sleep(1000);
                            lanteriajobexists = lanteria.JobExists;
                        }
                        LogMessage("\nLanteria Core solution deployed.");
                    });
                    //get features
                    SPFeatureDefinitionCollection collFeatureDefinitions = SPFarm.Local.FeatureDefinitions;
                        SPFeatureDefinition siteFeature = collFeatureDefinitions.SingleOrDefault(sf => sf.DisplayName.Equals("Lanteria.ES.SharePoint_LanteriaSite"));
                        SPFeatureDefinition webFeature = collFeatureDefinitions.SingleOrDefault(sf => sf.DisplayName.Equals("Lanteria.ES.SharePoint_LanteriaWeb"));
                        SPFeatureDefinition sqlFeature = collFeatureDefinitions.SingleOrDefault(sf => sf.DisplayName.Equals("Lanteria.ES.SharePoint_lanteriaSQL"));
                        SPFeatureDefinition contentFeature = collFeatureDefinitions.SingleOrDefault(sf => sf.DisplayName.Equals("Lanteria.ES.SharePoint_LanteriaContent"));
                        //disable features first
                        LogMessage("\nDisabling features...");
                        using (SPSite siteCollection = new SPSite(newURL.AbsoluteUri+"/es"))
                        {
                        SPWeb newWeb = siteCollection.OpenWeb();
                        SPGroup esHR = newWeb.Site.RootWeb.Groups["ES HR"];
                        SPUser sysacc = newWeb.Site.RootWeb.EnsureUser("SHAREPOINT\\system");
                        SPClaimProviderManager cpm = SPClaimProviderManager.Local;
                        SPClaim userClaim = cpm.ConvertIdentifierToClaim(username, SPIdentifierTypes.WindowsSamAccountName);
                        SPUser me = newWeb.Site.RootWeb.EnsureUser(userClaim.ToEncodedString());
                        esHR.AddUser(sysacc);
                        esHR.AddUser(me);
                        newWeb.Site.RootWeb.Update();
                        try
                            {
                                newWeb.Features.Remove(contentFeature.Id, true);
                                newWeb.Features.Remove(webFeature.Id, true);
                                newWeb.Features.Remove(sqlFeature.Id, true);
                            }
                        catch (Exception ex)
                            {
                                LogMessage("Error uccured during feature deactivation: " + ex.Message);
                            }
                        }
                        using (SPSite siteCollection = new SPSite(newURL.AbsoluteUri))
                        {
                            siteCollection.Features.Remove(siteFeature.Id, true);
                        }
                        //feature activation
                        LogMessage("\nActivating Lanteria core features...");
                        using (SPSite siteCollection = new SPSite(newURL.AbsoluteUri))
                        {
                            siteCollection.Features.Add(siteFeature.Id);
                            LogMessage("\nFeature Lanteria.ES.SharePoint_LanteriaSite activaed");
                        }
                        using (SPSite siteCollection = new SPSite(newURL.AbsoluteUri + "/es"))
                        {
                            SPWeb newWeb = siteCollection.OpenWeb();
                            try
                            {
                                newWeb.Features.Add(sqlFeature.Id);
                                LogMessage("\nFeature Lanteria.ES.SharePoint_LanteriaSQL activaed");
                                newWeb.Features.Add(webFeature.Id);
                                LogMessage("\nFeature Lanteria.ES.SharePoint_LanteriaWeb activaed");
                                newWeb.Features.Add(contentFeature.Id);
                                LogMessage("\nFeature Lanteria.ES.SharePoint_LanteriaContent activaed");
                            }
                            catch (Exception ex)
                            {
                                LogMessage("\nError uccured during feature activation: " + ex.InnerException.InnerException);
                            }
                        }
                    
                }
                catch (Exception ex)
                {
                    MessageBox.Show("A handled exception just occurred: " + ex.Message, "Exception", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
            };
            worker_backup.RunWorkerCompleted += (o, ea) =>
            {
                Dispatcher.Invoke((Action)(() =>
                {
                    progressbar.Visibility = Visibility.Hidden;
                    Clone.Visibility = Visibility.Hidden;
                }));
                worker_backup.Dispose();
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
    }
}
