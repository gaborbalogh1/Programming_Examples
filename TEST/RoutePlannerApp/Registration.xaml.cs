using System;
using System.Windows;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using System.IO;

namespace RoutePlannerApp
{
    /// <summary>
    /// Interaction logic for Registration.xaml
    /// </summary>
    public partial class Registration : Window
    {

        public static SqlConnection con = new SqlConnection(@"Data Source=.\SQLEXPRESS;Initial Catalog=MakingSpace;Integrated Security=False;User ID=sa;Password=Gabika25!");
        public static SqlCommand cmd;
        public static SqlDataAdapter dr;
        public static DataTable dt;


        private MainWindow mainWindow;

        public delegate void ReadyToShowDelegate(object sender, EventArgs args);

        public event ReadyToShowDelegate ReadyToShow;

        private DispatcherTimer timer;

        public Registration()
        {
            InitializeComponent();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(60);
            timer.Tick += new EventHandler(timer_Tick);
            timer.Start();

            mainWindow = new MainWindow(); 

            mainWindow.ReadyToShow += new MainWindow.ReadyToShowDelegate(MainWindow_ReadyToShow); 

            mainWindow.Closed += new EventHandler(MainWindow_Closed);
            timer.Stop();
        }

      //  When done, it raises a custom event to let the splash screen know that its time is up.
        void timer_Tick(object sender, EventArgs e)
        {

            timer.Stop();

            if (ReadyToShow != null)
            {
                ReadyToShow(this, null);
            }
        }

        //Log on link not working yet missing second project to be called log in ...
        private void Login_Click(object sender, RoutedEventArgs e)

        {

            Login login = new Login();

            login.Show();

            Close();

        }
        #region Registratation for Kasia app
        private void Reset_Button(object sender, RoutedEventArgs e)

        {

            Reset();

        }

        public void Reset()

        {

            textBoxFirstName.Text = "";

            textBoxLastName.Text = "";

            textBoxUserName.Text = "";

            passwordBox1.Password = "";

            passwordBoxConfirm.Password = "";

        }

        private void Cancel(object sender, RoutedEventArgs e)

        {
            Close();
            Application.Current.Shutdown();
            //(Parent as Grid).Children.Remove(this);

        }

        private void Submit(object sender, RoutedEventArgs e)

        {

            if (textBoxUserName.Text.Length == 0)

            {

                errormessage.Text = "User Name can not be blank or User Name not found! ";

                textBoxUserName.Focus();

            }
            else

            {

                string firstname = textBoxFirstName.Text;

                string lastname = textBoxLastName.Text;

                string UserName = textBoxUserName.Text;

                string password = passwordBox1.Password;

                if (passwordBox1.Password.Length == 0)

                {

                    errormessage.Text = "Enter password.";

                    passwordBox1.Focus();

                }

                else if (passwordBoxConfirm.Password.Length == 0)

                {

                    errormessage.Text = "Enter Confirm password.";

                    passwordBoxConfirm.Focus();

                }

                else if (passwordBox1.Password != passwordBoxConfirm.Password)

                {

                    errormessage.Text = "Password dont match, try again !";

                    passwordBoxConfirm.Focus();

                }

                else

                {
                    try
                    {
                        con.Open();

                        SqlCommand cmd = new SqlCommand("Insert into Registration (FirstName,LastName,UserName,Password) values('" + firstname + "','" + lastname + "','" + UserName + "','" + password + "')", con);

                        cmd.CommandType = CommandType.Text;

                        cmd.ExecuteNonQuery();

                        con.Close();

                        errormessage.Text = "Registration Complete press login to proceed to the Login Page!";
                    }
                    catch (Exception r)
                    {

                        MessageBox.Show(r.Message);
                    }
                    errormessage.Text = "";

                    Reset();

                }

            }

        }
        #endregion

        #region Navigation to MainWindow once the registration is complete

        void MainWindow_ReadyToShow(object sender, EventArgs args)
        {
            // When the main window is done with its time-consuming tasks, 
            // hide the splash screen and show the main window.

            #region Animate the splash screen fading

            Storyboard sb = new Storyboard();
            //
            DoubleAnimation da = new DoubleAnimation
            {
                From = 60,
                To = 0,
                Duration = new Duration(TimeSpan.FromSeconds(60))
            };
            //
            Storyboard.SetTarget(da, this);
            Storyboard.SetTargetProperty(da, new PropertyPath(OpacityProperty));
            sb.Children.Add(da);
            //
            sb.Completed += new EventHandler(Sb_Completed);
            //
            sb.Begin();

            #endregion // Animate the splash screen fading
        }

        void Sb_Completed(object sender, EventArgs e)
        {
            // When the splash screen fades out, we can show the main window:
            mainWindow.Visibility = System.Windows.Visibility.Visible;
        }

        void MainWindow_Closed(object sender, EventArgs e)
        {
            // When the MainWindow is closed, the app does not exit: SplashScreen is its real "main" window.
            // This handler ensures that the MainWindow closing works as expected: exit from teh app.

            this.Close();
        }
        #endregion

        private void Stop_timer(object sender, RoutedEventArgs e)
        {
            timer.Stop();
        }
    }
}
