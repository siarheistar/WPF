using Auth0.OidcClient;
using IdentityModel.OidcClient;
using IdentityModel.OidcClient.Browser;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Windows;
using System;
using trading_WPF;

namespace WPFAuthentication
{
    /// <summary>
    /// Interaction logic for MainWindowScreen.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Auth0Client client;

        public MainWindow()
        {
            InitializeComponent();
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
            extraParameters.Add("login_hint", "pasha.dublin@gmail.com");

            DisplayResult(await client.LoginAsync(extraParameters));
        }

        private void DisplayResult(LoginResult loginResult)
        {
            // Display error
            if (loginResult.IsError)
            {
               // resultTextBox.Text = loginResult.Error;
                throw new Exception(loginResult.Error);
            }

            //loginButton.Visibility = Visibility.Collapsed;

            //// Display result
            //StringBuilder sb = new StringBuilder();

            //sb.AppendLine("Tokens");
            //sb.AppendLine("------");
            //sb.AppendLine($"id_token: {loginResult.IdentityToken}");
            //sb.AppendLine($"access_token: {loginResult.AccessToken}");
            //sb.AppendLine($"refresh_token: {loginResult.RefreshToken}");
            //sb.AppendLine();

            //sb.AppendLine("Claims");
            //sb.AppendLine("------");
            //foreach (var claim in loginResult.User.Claims)
            //{
            //    sb.AppendLine($"{claim.Type}: {claim.Value}");
            //}

            //resultTextBox.Text = sb.ToString();

            MainWindowScreen mws = new MainWindowScreen();
            mws.LoginForm = this;
            mws.Show();
        }

        public async void Logout()
        {
            BrowserResultType browserResult = await client.LogoutAsync();

            if (browserResult != BrowserResultType.Success)
            {  
                throw new Exception(browserResult.ToString());
            }

            Login();
        }
    }
}