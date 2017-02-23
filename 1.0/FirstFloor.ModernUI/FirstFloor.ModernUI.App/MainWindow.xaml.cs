using FirstFloor.ModernUI.Presentation;
using FirstFloor.ModernUI.Windows.Controls;
using System.Windows.Media;

namespace FirstFloor.ModernUI.App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ModernWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            AppearanceManager.Current.AccentColor = Color.FromRgb(0xfa, 0x68, 0x00);
            AppearanceManager.Current.ThemeSource = AppearanceManager.DarkThemeSource;
        }
    }
}
