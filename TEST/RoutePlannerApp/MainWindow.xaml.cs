using System;
using System.Data.SqlClient;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.Net;
using System.Linq;
using log4net;
using System.Diagnostics;
using Microsoft.Maps.MapControl.WPF;
using System.Text.RegularExpressions;
using BingMapsRESTToolkit.Extensions;
using BingMapsRESTToolkit;
using System.Collections.Generic;
using System.Windows.Media;
using System.Configuration;

namespace RoutePlannerApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
        
    {
        #region Private Properties

        private string BingMapsKey = ConfigurationManager.AppSettings.Get("BingMapsKey");

        private string SessionKey;

        private Regex CoordinateRx = new Regex(@"^[\s\r\n\t]*(-?[0-9]{0,2}(\.[0-9]*)?)[\s\t]*,[\s\t]*(-?[0-9]{0,3}(\.[0-9]*)?)[\s\r\n\t]*$");

        #endregion

        //log4net 
        protected static readonly ILog log = LogManager.GetLogger(typeof(Application));
        
        public static SqlConnection con = new SqlConnection(@"Data Source=.\SQLEXPRESS;Initial Catalog=MakingSpace;Integrated Security=False;User ID=sa;Password=Gabika25!;Connect Timeout=30;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False");
        public static SqlCommand cmd;

        // Update database from Data Grid View
        public static SqlCommandBuilder cmdbld;
        public static SqlDataAdapter dr;
        public DataSet ds;
        public static DataTable dt;
        public static DataRowView drv;
        
        //get ip of the user device
        public enum NetworkInterfaceType { get, set }
        // IP address placeholder replaced with foreach on line 594
        //public static string CIP = "";

        public delegate void ReadyToShowDelegate(object sender, EventArgs args);

        public event ReadyToShowDelegate ReadyToShow;

        private DispatcherTimer timer;

        //Global connection settings for Appointments in database MakingSpace
        AppointmentsTableDataContext data = new AppointmentsTableDataContext(con);

        public MainWindow()
        {
            InitializeComponent();
            // for navigation
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(15);
            timer.Tick += new EventHandler(timer_Tick);
            timer.Start();
           
            //Set Empty the List of locations to the input panel on the Map tab item for route planning.
            InputTbx.Text = string.Empty;
            //Create the logfiles at startup
            log.Info("Application Started"+ "\t"+DateTime.Now);
            // start event log with 1 entry as a test
            Event_logging();

            mymap.CredentialsProvider = new ApplicationIdCredentialsProvider(BingMapsKey);
            mymap.CredentialsProvider.GetCredentials((c) =>
            {
                SessionKey = c.ApplicationId;
            });

            //Add some sample locations to the input panel.
            InputTbx.Text = "WN2 4AQ\nWN4 0LR\nWN4 9UU\nWN4 8SQ\nWN4 9SL\nWN2 4AQ";
        }

        #region enable log4net

        private void Window_Initialized(object sender, EventArgs e)
        {
            CreateFile();
        }
        // create a log file for log4net on c:\temp
        string dir = Environment.CurrentDirectory.ToString();
        string filepath = @"Resources\Logging\Log.Txt";
        FileStream fs;
        //StreamReader sr;

        //string filepath2 = @"Resources\Logging\Log4Net_Diagnostics.txt";
        public void CreateFile()
        {
            try
            {
                try
                {

                    // Create a new file 
                    using (fs = File.OpenWrite(filepath))
                    {
                        log4net.Config.XmlConfigurator.Configure(new System.IO.FileInfo(dir + "\\log4net_config.xml"));
                        log4net.Config.BasicConfigurator.Configure();

                        log.Info("this is from create file log");
                    }
                    fs.Close();
                }
                catch (Exception Ex)
                {
                    MessageBox.Show(Ex.Message.ToString());
                }

            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message.ToString());
                log.Error("Create File Action failed" + e.Message.ToString());
                fs.Close();
            }
            fs.Close();

        }

        public static void Event_logging()
        {
            // EventLog
            string sSource = "RoutePlanner";
            string sLog = "Application";
            string sEvent = "";

            try
            {
                if (!EventLog.SourceExists(sSource))
                    EventLog.CreateEventSource(sSource, sLog);

                EventLog.WriteEntry(sSource, sEvent);
                EventLog.WriteEntry(sSource, sEvent,
                EventLogEntryType.Information, 0001);

            }
            catch (System.Security.SecurityException ex)
            {
                MessageBox.Show(ex.Message.ToString());
                
            }


        }
        #endregion
        
        #region Navigation from Login-Registration-Login-MainWindow
        // Timer 
        void timer_Tick(object sender, EventArgs e)
        {
            timer.Stop();

            if (ReadyToShow != null)
            {
                ReadyToShow(this, null);
            }
        }
        #endregion
        
        #region Tab Navigation and Zoom in\out and Help Tab Button control starts here
        //Navigate to New Customer Tab from StartPage
        public void BtrNItem(object sender, RoutedEventArgs e)
        {
            int index = tabControl.SelectedIndex + 1;
            if (index < 0)
            {
                index = tabControl.SelectedIndex - 1;
            }
            tabControl.SelectedIndex = index;
            //this.Close();
        }
        //Navigate to Exisitng Customer from StartPage
        private void BtrEItem(object sender, RoutedEventArgs e)
        {
            //// using the New Customer Button on Customer tab to navigate to the next tab + 1, +2 .
            int index = tabControl.SelectedIndex + 2;
            if (index >= tabControl.Items.Count)
                index = 0;
            tabControl.SelectedIndex = index;
        }
        // Read file from text file path
        private async void HelpTextBlock(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = @"Resources\TextFiles\HelpTab_Help.txt";
                using (StreamReader sr = new StreamReader(path))
                {
                    String line = await sr.ReadToEndAsync();
                    ResultBlock.Text = line;

                }
            }
            catch (Exception ex)
            {
                //ResultBlock.Text = "Could not read the file";
                MessageBox.Show(ex.Message.ToString());
            }
        }
        // Clear the Resultblock.text content when the mouse leave event occour
        public void ResultBlock_Leave(object sender, RoutedEventArgs e)
        {
            //clear value in textblock
            ResultBlock.Text = string.Empty;
        }
        // Zoom in on the map with the slider
        public void ZoomUp(object sender, RoutedEventArgs e)
        {
            mymap.ZoomLevel = ((int)mymap.ZoomLevel) + 1;
        }
        // Zoom Out on the map with the slider
        public void ZoomDown(object sender, RoutedEventArgs e)
        {
            mymap.ZoomLevel = ((int)mymap.ZoomLevel) - 1;
        }

        #endregion

        #region Database Query starts here
        // New Customer to add to database on NC Tab SqlConnection
        public void AddItemToDataBase()
        {
            try
            {
                con = new SqlConnection(@"Data Source=.\SQLEXPRESS;Initial Catalog=MakingSpace;Integrated Security=True;User ID=sa;Password=Gabika25!");
                con.Open();
                SqlCommand cmd = new SqlCommand();
                cmd = new SqlCommand("INSERT INTO Customers VALUES (@CustID,@First_Name,@Last_Name,@Key_Safe_Number,@AddressL1,@AddressL2,@Post_Code,@City_Town,@Country,@Comments)", con);
                cmd.Parameters.AddWithValue("@CustID", CustID.Text.ToString().Trim());
                cmd.Parameters.AddWithValue("@First_Name", fName.Text.ToString().Trim());
                cmd.Parameters.AddWithValue("@Last_Name", lName.Text.ToString().Trim());
                cmd.Parameters.AddWithValue("@Key_Safe_Number", Key_SafeNo.Text.ToString().Trim());
                cmd.Parameters.AddWithValue("@AddressL1", aLine1.Text.ToString().Trim());
                cmd.Parameters.AddWithValue("@AddressL2", aLine2.Text.ToString().Trim());
                cmd.Parameters.AddWithValue("@Post_Code", pCode.Text.ToString().Trim());
                cmd.Parameters.AddWithValue("@City_Town", cTown.Text.ToString().Trim());
                cmd.Parameters.AddWithValue("@Country", country.Text.ToString().Trim());
                cmd.Parameters.AddWithValue("@Comments", Comments.Text.ToString().Trim());
                cmd.ExecuteNonQuery();
            }
            catch (SqlException e)
            {
                e.Message.ToString();
            }
            con.Close();
        }
        // Save Customer information to database 
        private void BtrSave(object sender, RoutedEventArgs e)
        {
            // update database with values using InsertInto statement
            AddItemToDataBase();
            MessageBox.Show("Record Has been Added to the database!");
        }
        #endregion

        #region Set Datagrid View on Existing Customer tab with SqlConnection
        private void FillDataGrid(object sender, RoutedEventArgs e)
        {
            con = new SqlConnection(@"Data Source=.\SQLEXPRESS;Initial Catalog=MakingSpace;Integrated Security=True;User ID=sa;Password=Gabika25!");
            con.Open();
            try
            {

                dt = new DataTable();

                dr = new SqlDataAdapter("SELECT  CustID,First_Name,Last_Name,Key_Safe_Number,AddressL1,AddressL2,Post_Code,City_Town,Country,Comments FROM Customers", con);

                cmdbld = new SqlCommandBuilder(dr);
                dr.Fill(dt);
                grdCustomers.ItemsSource = dt.DefaultView;
                grdCustomers.FontSize = 24;                                                                                                  
            }       
            catch (SqlException ex)
            {               

                MessageBox.Show(ex.InnerException.ToString());
            }
            SearchCustomer.Text = String.Empty;
            con.Close();
        }
        #endregion

        #region Read Customer Data to gridview
        // Search for user data in datagrid view 
        private void ReadCustomerData(string text)
        {
            
            try
            {
                con = new SqlConnection(@"Data Source=.\SQLEXPRESS;Initial Catalog=MakingSpace;Integrated Security=True;User ID=sa;Password=Gabika25!");

                string value = SearchCustomer.Text;
                con.Open();
                string queryString = "SELECT CustID,First_Name,Last_Name,Key_Safe_Number,AddressL1,AddressL2,Post_Code,City_Town,Country,Comments FROM Customers WHERE First_Name LIKE '%" + value + "%'";
                cmd = new SqlCommand(queryString, con);
                cmd.CommandTimeout = 600;
                cmd.ExecuteNonQuery();
                SqlDataAdapter dt = new SqlDataAdapter(cmd);
                DataTable ds = new DataTable();
                dt.Fill(ds);
                grdCustomers.ItemsSource = ds.DefaultView;
                grdCustomers.FontSize = 24;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            
            con.Close();
        }
        #endregion

        #region Existing Customer tab button click events
        // Search Customer button event
        private void SearchCustomer_TextChanged(object sender, TextChangedEventArgs e)
        {
            ReadCustomerData(SearchCustomer.Text);
        }
        //add selected customer to the text box list on map view
        private void Add_To_Map_Search_List(object sender, RoutedEventArgs e)
        {
            if (grdCustomers.SelectedItems.Count > 0)
            {
                for (int i = 0; i < grdCustomers.SelectedItems.Count; i++)
                {
                    DataRowView selectedFile = (DataRowView)grdCustomers.SelectedItems[i];
                    // CustID,First_Name,Last_Name,AddressL1,AddressL2,Post_Code,Key_Safe_Number,Comments
                    string str = Convert.ToString(selectedFile.Row.ItemArray[6]);
                    bool firstvalue = true;
                    if (!firstvalue)
                    {
                        InputTbx.Text = str;
                    }
                    else
                    {
                        InputTbx.Text += "\n"+str;
                    }
                   
                }
            }
        }
        //implements save changes to the datagrid view by the user.
        private void Save_Change_Click(object sender, RoutedEventArgs e)
        {
            SqlConnection con = new SqlConnection(@"Data Source=.\SQLEXPRESS;Initial Catalog=MakingSpace;Integrated Security=True;User ID=sa;Password=Gabika25!");
            con.Open();
            try
            {
               cmdbld = new SqlCommandBuilder(dr);
                dr.UpdateCommand = cmdbld.GetUpdateCommand();
                dr.Update(dt);

                MessageBox.Show("Changes have been saved ","Update",MessageBoxButton.OK,MessageBoxImage.Information);
                
            }
            catch (SqlException ex)
            {
                MessageBox.Show("\nError Message "+ex.InnerException.Message,"Error",MessageBoxButton.OK,MessageBoxImage.Error);
            }
            con.Close();
        }
        //Delete selected row from database on existing customer view
        private void Delete_Selected(object sender, RoutedEventArgs e)
        {
            con = new SqlConnection(@"Data Source=.\SQLEXPRESS;Initial Catalog=MakingSpace;Integrated Security=True;User ID=sa;Password=Gabika25!");
            con.Open();

            ds = new DataSet();
            //var rowIndex = datagridview2.Items.RemoveAt(datagridview2.);

            try
            {
                if (grdCustomers.SelectedIndex >= 1)
                {
                    dr = new SqlDataAdapter("SELECT * FROM Customers", con);
                    cmdbld = new SqlCommandBuilder(dr);
                    dr.MissingSchemaAction = MissingSchemaAction.AddWithKey;
                    dr.Fill(ds, "Customers");
                    //DataRow row1 = das.Tables["Customers"].Rows.Find(rowview);
                    DataRowView rowview = grdCustomers.SelectedItem as DataRowView;
                    rowview.Delete();
                    dr.Update(ds, "Customers");
                    grdCustomers.ItemsSource = ds.DefaultViewManager;
                    grdCustomers.FontSize = 24;
                }
                else
                {
                    MessageBox.Show("No more entries to remove" + "\n" + "Please reload the page or Add a New Enrty First!");
                }

            }
            catch (Exception ext)
            {
                MessageBox.Show(ext.InnerException.ToString());
            }
            con.Close();
        }
        #endregion

        #region Reset New Customer textbox

        private void Reset_Button(object sender, RoutedEventArgs e)

        {

            Reset2();

        }

        public void Reset2()

        {

            CustID.Text = "";

            fName.Text = "";

            lName.Text = "";

            aLine1.Text = "";

            aLine2.Text = "";

            pCode.Text = "";

            cTown.Text = "";

            country.Text = "";

            Key_SafeNo.Text ="";

            Comments.Text = "";

        }

        #endregion

        #region Idle time count and if mouse move login window will be showed...

        // set default time
        int totaltime = 0;

        //LASTINPUTINFO, GetLastInputInfo
        LASTINPUTINFO lastInputInf = new LASTINPUTINFO();
       

        public bool Button1_Click { get; private set; }

        //load the  library user32.dll to use with GetLastInputInfo.
        [DllImport("user32.dll")]
        public static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        // Using MarshalAS
        [StructLayout(LayoutKind.Sequential)]
        public struct LASTINPUTINFO
        {
            [MarshalAs(UnmanagedType.U4)]
            public int cbSize;
            [MarshalAs(UnmanagedType.U4)]
            public int dwTime;
        }

        //Subscripe to MainWindow Loaded event
        private void MainWindow_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            DispatcherTimer dt = new DispatcherTimer();
            dt.Tick += dispatcherTimer_Tick;
            dt.Interval = new TimeSpan(0, 0, 1);
            dt.Start();
       
        }

        // Display time
        public void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            DisplayTime();
        }

        // Function for last input 
        public int GetLastInputTime()
        {
            int idletime = 0;
            idletime = 0;
            lastInputInf.cbSize = Marshal.SizeOf(lastInputInf);
            lastInputInf.dwTime = 0;

            if (GetLastInputInfo(ref lastInputInf))
            {
                idletime = Environment.TickCount - lastInputInf.dwTime;
            }

            if (idletime != 300)
            {
                return idletime / 1000;
            }
            else
            {
                return 0;
            }
        }

        // DisplayTime 
        private void DisplayTime()
        {
            totaltime = GetLastInputTime();
            if (GetLastInputTime().Equals(1))
            {
                var idletime = "Inactivity time" + " " + GetLastInputTime().ToString() + " " + "seconds";

                Label1.Content = idletime;
            }
            else
            {
                var idletime = "Inactivity time" + " " + GetLastInputTime().ToString() + " " + "seconds";

                Label1.Content= idletime;
            }
        }

        //mouse move event
        private void Window_MouseMove(System.Object sender, System.Windows.Input.MouseEventArgs e)
        {

            if (totaltime >= 300)
            {
                timer.Stop();
                Login login = new Login();
                login.Show();
                timer.Start();
                Close();
            }
        }
        #endregion

        #region Provide IP address information from end client.
        
        private void GetIP()
        {

            string HostName = Dns.GetHostName();
            IPAddress[] ipaddress = Dns.GetHostAddresses(HostName);
            
            foreach (IPAddress ip4 in ipaddress.Where(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork))
            {
                
                lblCP.Content = "Computer Name" +"\n"+ HostName+"\n"+"IP address"+"\n"+ ip4.ToString();
            }

        }
        
        private void GetIPAddress(object sender, EventArgs e)
        {
            log.Info("Get IP address has run...");
            GetIP(); // call the above method and show ip address in the label called lblCP
        }

        //add in log out button 

        private void Logout_Button(object sender, EventArgs e)
        {
            log.Info("Application is closed");
            // Close any open stream and save changes.
            fs.Close();
            Application.Current.Shutdown();
        }
        #endregion

       #region Map Save Control and Date Picker to Update the Calendar with search results

        private void Save_Results_Map(object sender, RoutedEventArgs e)
        {
            int id = 0;
            string Distance_Covered = OutputTbx.Text.ToString();
            string Selected_Date = DatePicker.SelectedDate.Value.Date.ToShortDateString();
            string Appointments = InputTbx.Text.ToString();


            if (Selected_Date.Length == 0)
            {
                MessageBox.Show("Please select a date first and try again!");
            }
            else if (DatePicker.Text.ToString().Length > 0)
            {
                con = new SqlConnection(@"Data Source=.\SQLEXPRESS;Initial Catalog=MakingSpace;Integrated Security=False;User ID=sa;Password=Gabika25!;Connect Timeout=30;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False");
                con.Open();
                string sql = "select * from Appointments where IdVisit = (select max(IdVisit) from Appointments) order by IdVisit";
                using (con)
                {
                    SqlCommand cmd = new SqlCommand(sql, con);
                    //cmd.Parameters.AddWithValue("@id", 1);
                    try
                    {
                        id = (Int32)cmd.ExecuteScalar();
                        id++;
                        //MessageBox.Show(id.ToString());
                        cmd = new SqlCommand("INSERT INTO Appointments VALUES (@IdVisit,@DateVisited,@Appointments,@DistanceCovered)", con);
                        cmd.Parameters.AddWithValue("@IdVisit", id.ToString().Trim());
                        cmd.Parameters.AddWithValue("@DateVisited", Selected_Date.ToString().Trim());
                        cmd.Parameters.AddWithValue("@Appointments", Appointments.ToString().Trim());
                        cmd.Parameters.AddWithValue("@DistanceCovered", Distance_Covered.ToString().Trim());

                        cmd.ExecuteNonQuery();

                        ShowData();
                        grdCustomers.FontSize = 24;
                    }
                    catch (Exception ex)
                    {
                        if (id==0)
                        {
                            id++;
                            MessageBox.Show(id.ToString());
                            cmd = new SqlCommand("INSERT INTO Appointments VALUES (@IdVisit,@DateVisited,@Appointments,@DistanceCovered)", con);
                            cmd.Parameters.AddWithValue("@IdVisit", id.ToString().Trim());
                            cmd.Parameters.AddWithValue("@DateVisited", Selected_Date.ToString().Trim());
                            cmd.Parameters.AddWithValue("@Appointments", Appointments.ToString().Trim());
                            cmd.Parameters.AddWithValue("@DistanceCovered", Distance_Covered.ToString().Trim());

                            cmd.ExecuteNonQuery();

                            ShowData();


                        }
                        else
                        {
                            MessageBox.Show(ex.Message.ToString());
                        }
                    }
                }

            }
            else
            {
                MessageBox.Show("Invalid operation try again");
                log.Debug("Unable to load Data from Appointments datatable");
                ShowData();
            }
            con.Close();
           
        }

        private void ShowData()
        {
            try
            {
                con = new SqlConnection(@"Data Source=.\SQLEXPRESS;Initial Catalog=MakingSpace;Integrated Security=False;User ID=sa;Password=Gabika25!;Connect Timeout=30;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False");
                con.Open();
                cmd = new SqlCommand("Select * from Appointments", con);
                DataTable dt = new DataTable();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(dt);
                listview1.ItemsSource = dt.DefaultView;
                listview1.FontSize = 24;
            }
            catch (Exception ShowDataex)
            {
                MessageBox.Show(ShowDataex.Message.ToString());
                log.Debug("Unable to load Data from Appointments datatable"+ShowDataex.Message.ToString());
            }
        }

        // implement calendar selected date to show the selected day appointments

        private void Calendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            var Calendar_Selected_Date = Visit_Calendar.SelectedDate.Value.Date.ToShortDateString();
            //MessageBox.Show(Calendar_Selected_Date.Value.ToShortDateString());
            ListOfVisits.Clear();
            ListOfVisits.FontSize = 18;
            try
            {
                con = new SqlConnection(@"Data Source=.\SQLEXPRESS;Initial Catalog=MakingSpace;Integrated Security=False;User ID=sa;Password=Gabika25!;Connect Timeout=30;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False");
                con.Open();
                cmd = new SqlCommand("Select Appointments From Appointments Where DateVisited LIKE '%" + Calendar_Selected_Date + "%'", con);

                SqlDataReader result = cmd.ExecuteReader();
                while (result.Read())
                {
                    string visits = result[0].ToString();
                    string visits2 = visits + "\n";
                    ListOfVisits.AppendText(visits2);
                    
                }

            }
            catch (Exception ShowDataex)
            {
                MessageBox.Show(ShowDataex.Message.ToString());
                log.Debug("Unable to get Visit Details from Appointments datatable" + ShowDataex.Message.ToString());
            }
            con.Close();

        }

        private void ClearTextBlock(object sender, RoutedEventArgs e)
        {
            ListOfVisits.Clear();
        }

        private void Clear_Results_MapSearch(object sender, RoutedEventArgs e)
        {
            InputTbx.Clear();
            OutputTbx.Clear();
            DatePicker.Text = string.Empty;
        }


        #endregion

        private async void CalculateRouteBtn_Clicked(object sender, RoutedEventArgs e)
        {
            mymap.Children.Clear();
            OutputTbx.Text = string.Empty;
            LoadingBar.Visibility = Visibility.Visible;

            var waypoints = GetWaypoints();

            if (waypoints.Count < 2)
            {
                MessageBox.Show("Need a minimum of 2 waypoints to calculate a route.");
                return;
            }

           //var travelMode = (TravelModeType)Enum.Parse(typeof(TravelModeType), (string)(TravelModeTypeCbx.SelectedItem as ComboBoxItem).Content);
           //var tspOptimization = (TspOptimizationType)Enum.Parse(typeof(TspOptimizationType), (string)(TspOptimizationTypeCbx.SelectedItem as ComboBoxItem).Tag);
            try
            {
                //Calculate a route between the waypoints so we can draw the path on the map. 
                //var routeRequest = new RouteRequest()
                //{
                //    Waypoints = waypoints,

                //    ////Specify that we want the route to be optimized.
                //    //WaypointOptimization = tspOptimization,
                //    //WaypointOptimization = TspOptimizationType.TravelDistance,

                //    RouteOptions = new RouteOptions()
                //    {

                //        DistanceUnits = DistanceUnitType.Miles,
                //        TravelMode = TravelModeType.Driving,
                //        RouteAttributes = new List<RouteAttributeType>()
                //        {
                //            RouteAttributeType.RoutePath,
                //            RouteAttributeType.ExcludeItinerary
                //        },
                //        Optimize = RouteOptimizationType.TimeWithTraffic,
                //    },

                //    //When straight line distances are used, the distance matrix API is not used, so a session key can be used.
                //    BingMapsKey = BingMapsKey //(tspOptimization == TspOptimizationType.StraightLineDistance) ? SessionKey : 
                //};

                var routeRequest = new RouteRequest()
                {
                    RouteOptions = new RouteOptions()
                    {
                        Avoid = new List<AvoidType>()
                    {
                        AvoidType.MinimizeTolls
                    },
                        TravelMode = TravelModeType.Driving,
                        DistanceUnits = DistanceUnitType.Miles,
                        Heading = 45,
                        RouteAttributes = new List<RouteAttributeType>()
                    {
                        RouteAttributeType.RoutePath
                    },
                       
                    },
                    Waypoints = waypoints,
                BingMapsKey = BingMapsKey
                };
                
                //Only use traffic based routing when travel mode is driving.
                //if (routeRequest.RouteOptions.TravelMode == TravelModeType.Driving)
                //{
                //    routeRequest.RouteOptions.Optimize = RouteOptimizationType.Time;
                //}
                //else
                //{
                //    routeRequest.RouteOptions.Optimize = RouteOptimizationType.TimeWithTraffic;
                //}

                var r = await routeRequest.Execute();

                RenderRouteResponse(routeRequest, r);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }

            LoadingBar.Visibility = Visibility.Collapsed;

        }

        #region Private Methods

        /// <summary>
        /// Renders a route response on the map.
        /// </summary>
        private void RenderRouteResponse(RouteRequest routeRequest, Response response)
        {
            //Render the route on the map.
            if (response != null && response.ResourceSets != null && response.ResourceSets.Length > 0 &&
               response.ResourceSets[0].Resources != null && response.ResourceSets[0].Resources.Length > 0
               && response.ResourceSets[0].Resources[0] is Route)
            {
                var route = response.ResourceSets[0].Resources[0] as Route;

                var timeSpan = new TimeSpan(0, 0, (int)Math.Round(route.TravelDurationTraffic));

                if (timeSpan.Days > 0)
                {
                   //OutputTbx.Text = string.Format("Travel Time: {3} days {0} hr {1} min {2} sec\r\n", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds, timeSpan.Days);
                    OutputTbx.Text += string.Format("Distance Traveled: "+ route.TravelDistance.ToString()+"  miles");
                }
                else
                {
                   //OutputTbx.Text = string.Format("Travel Time: {0} hr {1} min {2} sec\r\n", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
                    OutputTbx.Text += string.Format("Distance Traveled: " + route.TravelDistance.ToString() + "  miles");
                }

                var routeLine = route.RoutePath.Line.Coordinates;
                var routePath = new LocationCollection();

                for (int i = 0; i < routeLine.Length; i++)
                {
                    routePath.Add(new Microsoft.Maps.MapControl.WPF.Location(routeLine[i][0], routeLine[i][1]));
                }

                var routePolyline = new MapPolyline()
                {
                    Locations = routePath,
                    Stroke = new SolidColorBrush(Colors.Red),
                    StrokeThickness = 3
                };

                mymap.Children.Add(routePolyline);

                var locs = new List<Microsoft.Maps.MapControl.WPF.Location>();

                //Create pushpins for the optimized waypoints.
                //The waypoints in the request were optimized for us.
                for (var i = 0; i < routeRequest.Waypoints.Count; i++)
                {
                    var loc = new Microsoft.Maps.MapControl.WPF.Location(routeRequest.Waypoints[i].Coordinate.Latitude, routeRequest.Waypoints[i].Coordinate.Longitude);

                    //Only render the last waypoint when it is not a round trip.
                    if (i < routeRequest.Waypoints.Count - 1)
                    {
                        mymap.Children.Add(new Pushpin()
                        {
                            Location = loc,
                            Content = i
                        });
                    }

                    locs.Add(loc);
                }

                mymap.SetView(locs, new Thickness(50), 0);
            }
            else if (response != null && response.ErrorDetails != null && response.ErrorDetails.Length > 0)
            {
                throw new Exception(String.Join("", response.ErrorDetails)+"Im on line 859");
            }
        }

        /// <summary>
        /// Gets the inputted waypoints.
        /// </summary>
        private List<SimpleWaypoint> GetWaypoints()
        {
            var places = InputTbx.Text.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            var waypoints = new List<SimpleWaypoint>();

            foreach (var p in places)
            {
                if (!string.IsNullOrWhiteSpace(p))
                {
                    var m = CoordinateRx.Match(p);

                    if (m.Success)
                    {
                        waypoints.Add(new SimpleWaypoint(double.Parse(m.Groups[1].Value), double.Parse(m.Groups[3].Value)));
                    }
                    else
                    {
                        waypoints.Add(new SimpleWaypoint(p));
                    }
                }
            }

            return waypoints;
        }

        #endregion
    }
}
