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

        public Auth0Window()
        {
            InitializeComponent();
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
            extraParameters.Add("login_hint", EmailSelector); 

            DisplayResult(await client.LoginAsync(extraParameters));
        }

        private void DisplayResult(LoginResult loginResult)
        {
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

    }

}
