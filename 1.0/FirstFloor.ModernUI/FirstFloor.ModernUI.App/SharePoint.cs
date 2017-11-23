using System;
using System.Collections.Generic;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using System.Collections.ObjectModel;
using System.Threading;
using System.Linq;
using System.Text;
using Microsoft.SharePoint.Administration.Claims;
using FirstFloor.ModernUI.App.Properties;

namespace FirstFloor.ModernUI.App
{
    public class SharePoint
    {
        public Exception exception
        {
            get; set;
        }
        public SPFarm farm = SPFarm.Local;
        public List<SPWebApplication> GetWebsFromSolution(string sName)
        {
            List<SPWebApplication> AllWebs = new List<SPWebApplication>();
            SPSolution solution = farm.Solutions[sName];
            SPSecurity.RunWithElevatedPrivileges(delegate ()
            {
                foreach (SPWebApplication webApplication in solution.DeployedWebApplications)
                {
                    AllWebs.Add(webApplication);
                }
            });
            return AllWebs;
        }
        public List<SPWebApplication> GetAllWebs()
        {
            List<SPWebApplication> AllWebs = new List<SPWebApplication>();
            SPSecurity.RunWithElevatedPrivileges(delegate ()
            {
                SPServiceCollection services = farm.Services;
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
                                    AllWebs.Add(webApplication);
                                }
                            }
                        }
                    }
                }
            });
            return AllWebs;
        }
        public List<SPSite> GetAllSPSites()
        {
            List<SPSite> AllSites = new List<SPSite>();
            SPSecurity.RunWithElevatedPrivileges(delegate ()
            {
                SPServiceCollection services = farm.Services;
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
                                        AllSites.Add(site);
                                    }
                                }
                            }
                        }
                    }
                }
            });
            return AllSites;
        }
        public SPSolution GetCoreSolution()
        {
            SPSolution solution = farm.Solutions[Settings.Default.CoreSolution];
            if (solution == null)
            {
                solution = farm.Solutions[Settings.Default.SolutionID];
            }
            return solution;
        }
        public SPSolution GetSolution(string sName)
        {
            SPSolution solution = farm.Solutions[sName];
            if (solution == null)
            {
                solution = farm.Solutions[new Guid(sName)];
            }
            return solution;
        }
        public bool DeploySolution(string sName, Collection<SPWebApplication> WebApps)
        {
            SPSolution solution = farm.Solutions[sName];
            solution.Deploy(DateTime.Now, true, WebApps, true);
            bool deployed = solution.Deployed;
            while (!deployed)
            {
                Thread.Sleep(1000);
                deployed = solution.Deployed;
            }

            bool jobexists = solution.JobExists;
            while (jobexists)
            {
                Thread.Sleep(1000);
                jobexists = solution.JobExists;
            }
            return solution.Deployed;
        }
        public string RetractCoreSolution ()
        {
            StringBuilder log = new StringBuilder();
            SPSolution solution = GetCoreSolution();
            if (solution != null)
            {
                if (solution.DeployedWebApplications.Count > 0)
                {
                    solution.RetractLocal(solution.DeployedWebApplications);
                }
                SPFarm.Local.Solutions.Remove(solution.Id);
                log.Append("Solution has been removed, please select new WSP file.");
            }
            return log.ToString();
        }
        public string SoltionDeploymentStatus(string sName)
        {
            SPSolution solution = farm.Solutions[sName];
            return solution.LastOperationDetails;
        }
        public bool SolutionExist(string sName)
        {
            SPSolution solution = farm.Solutions[sName];
            if (solution != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool SolutionDeployed(string sName)
        {
            SPSolution solution = farm.Solutions[sName];
            return solution.Deployed;
        }
        public string AddSolution(string path)
        {
            try
            {
                SPSolution solution = farm.Solutions.Add(path);
                return "Solution " + solution.Name + " added"+"\nSelect web applications and click Deploy.";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        public List<SPFeatureDefinition> GetFeaturesInSolution(string sName)
        {
            SPSolution solution = farm.Solutions[sName];
            List<SPFeatureDefinition> solutionfeatures = farm.FeatureDefinitions.Where(fd => fd.SolutionId == solution.Id).ToList();
            return solutionfeatures;
        }
        public bool ActivateSiteFeature(SPFeatureDefinition feature, string siteurl)
        {
            using (SPSite siteCollection = new SPSite(siteurl))
            {
                try
                {
                    siteCollection.Features.Add(feature.Id);
                    return true;
                }
                catch (Exception ex)
                {
                    exception = ex;
                    return false;
                }
            }
        }
        public bool ActivateWebFeature(SPFeatureDefinition feature, string weburl)
        {
            using (SPSite siteCollection = new SPSite(weburl))
            {
                SPWeb web = siteCollection.OpenWeb();
                try
                {
                    web.Features.Add(feature.Id);
                    return true;
                }
                catch (Exception ex)
                {
                    exception = ex;
                    return false;
                }
            }
        }
        public string ActivateFeaturesFromCustomSolution(string sName, string webURL)
        {
            StringBuilder log = new StringBuilder();
            List<SPFeatureDefinition> features = GetFeaturesInSolution(sName);
            SPFeatureDefinition sitefeature = features.FirstOrDefault(sf => sf.Scope.Equals(SPFeatureScope.Site));
            SPFeatureDefinition contentfeature = features.FirstOrDefault(sf => sf.Scope.Equals(SPFeatureScope.Web) && (sf.ActivationDependencies.Count > 0 || sf.DisplayName.ToLower().Contains("content")));
            SPFeatureDefinition webfeature = features.FirstOrDefault(sf => sf.Scope.Equals(SPFeatureScope.Web) && (contentfeature == null || contentfeature.Id != sf.Id));
            if (sitefeature != null)
            {
                if (ActivateSiteFeature(sitefeature, webURL))
                {
                    log.Append("\nSite feature " + sitefeature.DisplayName + " activated");
                }
                else
                {
                    log.Append("\nError activating Site feature " + sitefeature.DisplayName + "\n" + exception);
                 }
            }
            if (webfeature != null)
            {
                if (ActivateWebFeature(webfeature, webURL + "es/"))
                {
                    log.Append("\nWeb feature " + webfeature.DisplayName + " activated");
                }
                else
                {
                    log.Append("\nError activating Web feature " + webfeature.DisplayName + "\n" + exception);
                }
            }
            if (contentfeature != null)
            {
                if (ActivateWebFeature(contentfeature, webURL + "es/"))
                {
                    log.Append("\nContent feature " + contentfeature.DisplayName + " activated");
                }
                else
                {
                    log.Append("\nError activating Content feature " + contentfeature.DisplayName + "\n" + exception);

                }
            }
            return log.ToString();
        }
        public string ActivateCoreFeatures(string webURL)
        {
            StringBuilder log = new StringBuilder();
            SPFeatureDefinitionCollection collFeatureDefinitions = SPFarm.Local.FeatureDefinitions;
            SPFeatureDefinition siteFeature = collFeatureDefinitions.SingleOrDefault(sf => sf.DisplayName.Equals(Settings.Default.SiteFeature));
            SPFeatureDefinition webFeature = collFeatureDefinitions.SingleOrDefault(sf => sf.DisplayName.Equals(Settings.Default.WebFeature));
            SPFeatureDefinition sqlFeature = collFeatureDefinitions.SingleOrDefault(sf => sf.DisplayName.Equals(Settings.Default.SqlFeature));
            SPFeatureDefinition contentFeature = collFeatureDefinitions.SingleOrDefault(sf => sf.DisplayName.Equals(Settings.Default.ContentFeature));
            //feature activation
            log.Append("\nActivating Lanteria core features...");
            using (SPSite siteCollection = new SPSite(webURL))
            {
                try
                {
                    siteCollection.Features.Add(siteFeature.Id);
                    log.Append("\nFeature Lanteria.ES.SharePoint_LanteriaSite activated");
                }
                catch (Exception ex)
                {
                    log.Append("\nError occured during site feature activation: " + ex.Message);
                }

            }
            using (SPSite siteCollection = new SPSite(webURL + "es/"))
            {
                SPWeb newWeb = siteCollection.OpenWeb();
                try
                {
                    newWeb.Features.Add(sqlFeature.Id);
                    log.Append("\nFeature Lanteria.ES.SharePoint_LanteriaSQL activated");
                }
                catch (Exception ex)
                {
                    log.Append("\nError occured during feature activation: " + ex.Message);
                }
                try
                {
                    newWeb.Features.Add(webFeature.Id);
                    log.Append("\nFeature Lanteria.ES.SharePoint_LanteriaWeb activated");
                }
                catch (Exception ex)
                {
                    log.Append("\nError occured during feature activation: " + ex.Message);
                }
                try
                {
                    newWeb.Features.Add(contentFeature.Id);
                    log.Append("\nFeature Lanteria.ES.SharePoint_LanteriaContent activated");
                }
                catch (Exception ex)
                {
                    log.Append("\nError occured during feature activation: " + ex.Message);
                }
            }
            return log.ToString();
        }
        public string DeActivateCoreFeatures(string webURL)
        {
            StringBuilder log = new StringBuilder();
            SPFeatureDefinitionCollection collFeatureDefinitions = SPFarm.Local.FeatureDefinitions;
            SPFeatureDefinition siteFeature = collFeatureDefinitions.SingleOrDefault(sf => sf.DisplayName.Equals(Settings.Default.SiteFeature));
            SPFeatureDefinition webFeature = collFeatureDefinitions.SingleOrDefault(sf => sf.DisplayName.Equals(Settings.Default.WebFeature));
            SPFeatureDefinition sqlFeature = collFeatureDefinitions.SingleOrDefault(sf => sf.DisplayName.Equals(Settings.Default.SqlFeature));
            SPFeatureDefinition contentFeature = collFeatureDefinitions.SingleOrDefault(sf => sf.DisplayName.Equals(Settings.Default.ContentFeature));
            //disable features first
            log.Append("\nDisabling features...");
            using (SPSite siteCollection = new SPSite(webURL + "es/"))
            {
                SPWeb newWeb = siteCollection.OpenWeb();
                SPGroup esHR = newWeb.Site.RootWeb.Groups["ES HR"];
                SPUser sysacc = newWeb.Site.RootWeb.EnsureUser("SHAREPOINT\\system");
                SPClaimProviderManager cpm = SPClaimProviderManager.Local;
                string username = Environment.UserDomainName + "\\" + Environment.UserName;
                SPClaim userClaim = cpm.ConvertIdentifierToClaim(username, SPIdentifierTypes.WindowsSamAccountName);
                SPUser me = newWeb.Site.RootWeb.EnsureUser(userClaim.ToEncodedString());
                esHR.AddUser(sysacc);
                esHR.AddUser(me);
                newWeb.Site.RootWeb.Update();
                try
                {
                    newWeb.Features.Remove(contentFeature.Id, true);
                }
                catch (Exception ex)
                {
                    log.Append("Error occured during feature deactivation: " + ex.Message);
                }
                try
                {
                    newWeb.Features.Remove(webFeature.Id, true);
                }
                catch (Exception ex)
                {
                    log.Append("Error occured during feature deactivation: " + ex.Message);
                }
                try
                {
                    newWeb.Features.Remove(sqlFeature.Id, true);
                }
                catch (Exception ex)
                {
                    log.Append("Error occured during feature deactivation: " + ex.Message);
                }
            }

            using (SPSite siteCollection = new SPSite(webURL))
            {
                try
                {
                    siteCollection.Features.Remove(siteFeature.Id, true);
                }
                catch (Exception ex)
                {
                    log.Append("Error occured during feature deactivation: " + ex.Message);
                }
            }
            return log.ToString();
        }
        public bool IsSolutionDeployedToWeb(string sName, SPWebApplication web)
        {
            SPSolution solution = farm.Solutions[sName];
            if (solution != null && solution.DeployedWebApplications.Contains(web))
                {
                    return true;
                }
            else
            {
                return false;
            }
        }
    }


}

