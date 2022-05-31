using Microsoft.Identity.Client;
using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Windows;

namespace MaterialDesign
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    /// 
   


    public partial class App : Application
    {
        static App()
        {
            _clientApp = PublicClientApplicationBuilder.Create(ClientId)
            .WithAuthority(AzureCloudInstance.AzurePublic, Tenant)
            .WithDefaultRedirectUri()
            .Build();
        }

        // Below are the clientId (Application Id) of your app registration and the tenant information. 
        // You have to replace:
        // - the content of ClientID with the Application Id for your app registration
        // - The content of Tenant by the information about the accounts allowed to sign-in in your application:
        //   - For Work or School account in your org, use your tenant ID, or domain
        //   - for any Work or School accounts, use organizations
        //   - for any Work or School accounts, or Microsoft personal account, use 2cd4783b-5f16-4b96-89ad-8c0292daf5fe
        //   - for Microsoft Personal account, use consumers
        private static string ClientId = "0aa20546-825a-4397-84e1-a884462f7af8";

        // Note: Tenant is important for the quickstart.
        private static string Tenant = "2cd4783b-5f16-4b96-89ad-8c0292daf5fe";
        private static IPublicClientApplication _clientApp;
        public static IPublicClientApplication PublicClientApp { get { return _clientApp; } }


        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                //First get the 'user-scoped' storage information location reference in the assembly
                IsolatedStorageFile isolatedStorage = IsolatedStorageFile.GetUserStoreForAssembly();
                //create a stream reader object to read content from the created isolated location
                StreamReader srReader = new StreamReader(new IsolatedStorageFileStream("configuration", FileMode.Open, isolatedStorage));

                string? EmailAddress = srReader.ReadLine();
                this.Properties["EmailAddress"] = EmailAddress;
                MessageBox.Show("Welcome, " + EmailAddress + "!");

                //close reader
                srReader?.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Please, Sign In!");
                //throw;
            }
        }

    }
}
