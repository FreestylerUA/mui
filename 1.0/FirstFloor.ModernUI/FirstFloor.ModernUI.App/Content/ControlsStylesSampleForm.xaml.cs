using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FirstFloor.ModernUI.App.Content
{
    /// <summary>
    /// Interaction logic for ControlsStylesSampleForm.xaml
    /// </summary>
    public partial class ControlsStylesSampleForm : UserControl
    {
        public ControlsStylesSampleForm()
        {
            InitializeComponent();

            this.Loaded += OnLoaded;
        }

        void OnLoaded(object sender, RoutedEventArgs e)
        {
            // select first control on the form
            Keyboard.Focus(this.TextFirstName);
        }
    }
}
