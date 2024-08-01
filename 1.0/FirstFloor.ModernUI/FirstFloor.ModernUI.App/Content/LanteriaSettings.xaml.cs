using System.Windows.Controls;
using FirstFloor.ModernUI.Windows.Controls;


namespace FirstFloor.ModernUI.App.Content
{
    public partial class LanteriaSettings : UserControl
    {
        public LanteriaSettings()
        {
            InitializeComponent();
            sName.Text = Properties.Settings.Default.CoreSolution;
            sID.Text = Properties.Settings.Default.SolutionID.ToString();
            SiteFeature.Text = Properties.Settings.Default.SiteFeature;
            WebFeature.Text = Properties.Settings.Default.WebFeature;
            ContentFeature.Text = Properties.Settings.Default.ContentFeature;
            Path.Text = Properties.Settings.Default.Path;
        }

        private void Update_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                Properties.Settings.Default.CoreSolution = sName.Text;
                Properties.Settings.Default.SolutionID = new System.Guid(sID.Text);
                Properties.Settings.Default.SiteFeature = SiteFeature.Text;
                Properties.Settings.Default.WebFeature = WebFeature.Text;
                Properties.Settings.Default.ContentFeature = ContentFeature.Text;
                Properties.Settings.Default.Path = Path.Text;
                Properties.Settings.Default.Save();
                ModernDialog.ShowMessage("Settings have been saved ", "App Properties", System.Windows.MessageBoxButton.OK);
            }
            catch (System.Exception ex)
            {
                ModernDialog.ShowMessage(ex.Message, "Error", System.Windows.MessageBoxButton.OK);
            }
        }
    }
}
