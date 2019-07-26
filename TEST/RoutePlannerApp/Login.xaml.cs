using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Media.Animation;


namespace RoutePlannerApp
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        
        public static SqlConnection con = new SqlConnection(@"Data Source=.\SQLEXPRESS;Initial Catalog=MakingSpace;Integrated Security=True;User ID=sa;Password=Gabika25!");
        public static SqlCommand cmd;
        public static SqlDataAdapter dr;
        public static DataTable dt;

        public Login()
        {
            InitializeComponent();
            
        }

        private void Button1_Click(object sender, RoutedEventArgs e)

        {

            if (textBoxUserName.Text.Length == 0)
            {
                errormessage.Text = "Enter a User Name or Password that is invalid.";
                textBoxUserName.Focus();
            }
            else
            {
                string UserName = textBoxUserName.Text;
                string password = passwordBox1.Password;
              
                con.Open();
                SqlCommand cmd = new SqlCommand("Select * from Registration WHERE UserName='" + UserName + "'  and password='" + password + "'", con);
                cmd.CommandType = CommandType.Text;
                SqlDataAdapter adapter = new SqlDataAdapter();
                adapter.SelectCommand = cmd;
                DataSet dataSet = new DataSet();
                adapter.Fill(dataSet);
                if (dataSet.Tables[0].Rows.Count > 0)

                {
                
                    string username = dataSet.Tables[0].Rows[0]["FirstName"].ToString() + " " + dataSet.Tables[0].Rows[0]["LastName"].ToString();
                    // Open new Main Window
                    MainWindow mainWindow = new MainWindow();
                    mainWindow.Show();
                    
                }

                else

                {

                    errormessage.Text = "Sorry User Name not Found! Please enter existing User Name and password.";

                }

                con.Close();
                Close();
            }
            
        }

        private void ButtonRegister_Click(object sender, RoutedEventArgs e)
        {
               

               Registration registration = new Registration();
                
                registration.Show();

            Close();
        }

        private void R_registration_Closed(object sender, EventArgs e)
        {
            Close();
        }

        private void R_registration_ReadyToShow(object sender, EventArgs args)
        {
            #region Animate the splash screen fading

            Storyboard sb = new Storyboard();
            //
            DoubleAnimation da = new DoubleAnimation
            {
                From = 15,
                To = 0,
                Duration = new Duration(TimeSpan.FromSeconds(15))
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

        private void Sb_Completed(object sender, EventArgs e)
        {
            // When the splash screen fades out, we can show the main window:
            MainWindow main = new MainWindow();
            main.Show();
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void EnterPressed(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (textBoxUserName.Text.Length == 0)
                {
                    errormessage.Text = "Enter a User Name or Password that is invalid.";
                    textBoxUserName.Focus();
                }
                else
                {
                    string UserName = textBoxUserName.Text;
                    string password = passwordBox1.Password;

                    con.Open();
                    SqlCommand cmd = new SqlCommand("Select * from Registration WHERE UserName='" + UserName + "'  and password='" + password + "'", con);
                    cmd.CommandType = CommandType.Text;
                    SqlDataAdapter adapter = new SqlDataAdapter();
                    adapter.SelectCommand = cmd;
                    DataSet dataSet = new DataSet();
                    adapter.Fill(dataSet);
                    if (dataSet.Tables[0].Rows.Count > 0)

                    {

                        string username = dataSet.Tables[0].Rows[0]["FirstName"].ToString() + " " + dataSet.Tables[0].Rows[0]["LastName"].ToString();
                        // Open new Main Window
                        MainWindow mainWindow = new MainWindow();
                        mainWindow.Show();

                    }

                    else

                    {

                        errormessage.Text = "Sorry User Name not Found! Please enter existing User Name and password.";

                    }

                    con.Close();
                    Close();
                }
            }
        }
    }
}