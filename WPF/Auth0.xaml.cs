using Auth0.OidcClient;
using IdentityModel.OidcClient;
using IdentityModel.OidcClient.Browser;
using System.Collections.Generic;
using System.Configuration;
using System.Windows;
using System;
using trading_WPF;
using MySql.Data.MySqlClient;
using System.Data;

namespace WPFAuthentication
{
    public partial class Auth0Window : Window
    {
        public Auth0Client client;

        private readonly MySqlConnection connection;
        private readonly string connectionString;

        public string EmailSelector { get; set; }
        public object SelectedEmail { get; private set; }
        private string email;
        public Auth0Window()
        {

            InitializeComponent();
            connectionString = ConfigurationManager.ConnectionStrings["ALGOTRADE_Local"].ConnectionString;
            connection = new MySqlConnection(connectionString);
            //ShowUsers();
            EmailSelector = ConfigurationManager.AppSettings["EmailSelector"];
            Hide();
            Login();
        }

        private async void Login()
        {
            string domain = ConfigurationManager.AppSettings["Auth0:Domain"];
            string clientId = ConfigurationManager.AppSettings["Auth0:ClientId"];

            client = new Auth0Client(new Auth0ClientOptions
            {
                Domain = domain,
                ClientId = clientId
            });

            var extraParameters = new Dictionary<string, string>();
            extraParameters.Add("connection", "google-oauth2");
            extraParameters.Add("login_hint", EmailSelector); //SelectedEmail.ToString()

            DisplayResult(await client.LoginAsync(extraParameters));
        }

        private void DisplayResult(LoginResult loginResult)
        {// Display error

            
            if (!loginResult.IsError)
            {
                //MessageBox.Show(loginResult.Error.ToString());
                MainWindowScreen mws = new MainWindowScreen();
                mws.LoginForm = this;
                mws.Show();
            }
            else if(loginResult.Error.ToString().Equals("unauthorized"))
            {
                MessageBox.Show(loginResult.Error.ToString());
                Hide();
                Login();
            }
            else if (loginResult.IsError)
            {
                MessageBox.Show(loginResult.Error.ToString());
                Close();
            }
                            
        }

        public async void Logout()
        {
            BrowserResultType browserResult = await client.LogoutAsync();

            if (browserResult != BrowserResultType.Success)
            {
                throw new Exception(browserResult.ToString());
            }
            SelectedEmail = "";
            Hide();
            Login();

        }



        //private void ShowUsers()
        //{
        //    string query = "SELECT email FROM algotrade.users where status = 1;";
        //    MySqlCommand sqlCommand = new MySqlCommand(query, connection);
        //    MySqlDataAdapter MysqlDataAdapter = new MySqlDataAdapter(sqlCommand);

        //    using (MysqlDataAdapter)
        //    {
        //        try
        //        {
        //            sqlCommand.Parameters.AddWithValue("@email", SelectedEmail);
        //            DataTable EmailTable = new DataTable();
        //            MysqlDataAdapter.Fill(EmailTable);
        //            SelectedEmail = "@email";
        //            //Log(msg: "finished", guiUpdate: () =>
        //            {
        //                UserList.DisplayMemberPath = "email";
        //                UserList.SelectedValuePath = "email";
        //                UserList.ItemsSource = EmailTable.DefaultView;
        //            }
        //            //);

        //            Hide();
        //            Login();
        //        }
        //        catch (Exception e)
        //        {
        //            MessageBox.Show(e.ToString());

        //        }
        //        finally
        //        {
        //            connection.Close();
        //        }
        //    }


        //}

        //private void ListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        //{
        //    ShowUsers();


        //}
    }

}
