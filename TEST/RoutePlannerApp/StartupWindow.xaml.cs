using System.Windows;

namespace RoutePlannerApp
{
    /// <summary>
    /// Interaction logic for StartupWindowxaml.xaml
    /// </summary>
    public partial class StartupWindow : Window
    {
       
        public StartupWindow()
        {
            InitializeComponent();
        }

        private void NextWindow(object sender, RoutedEventArgs e)
        {
            //Login login = new Login();
            //login.Show();
            Login login = new Login();
            login.Show();
            Close();
        }
    }
    
}
