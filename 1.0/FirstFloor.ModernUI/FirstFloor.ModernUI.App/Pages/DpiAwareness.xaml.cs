using System.Windows.Controls;

namespace FirstFloor.ModernUI.App.Pages
{
    /// <summary>
    /// Interaction logic for DpiAwareness.xaml
    /// </summary>
    public partial class DpiAwareness : UserControl
    {
        public DpiAwareness()
        {
            InitializeComponent();

            this.DataContext = new DpiAwarenessViewModel();
        }
    }
}
