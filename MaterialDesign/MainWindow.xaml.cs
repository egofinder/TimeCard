using Microsoft.Identity.Client;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace MaterialDesign
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public class UserInfo
    {
        public string? EmailAddress { get; set; }
        public int Interval { get; set; } = 900;
        //public DateTime? ExpirationTime { get; set; }

    }

    public partial class MainWindow : Window
    {
        //Set the API Endpoint to Graph 'me' endpoint. 
        // To change from Microsoft public cloud to a national cloud, use another value of graphAPIEndpoint.
        // Reference with Graph endpoints here: https://docs.microsoft.com/graph/deployments#microsoft-graph-and-graph-explorer-service-root-endpoints
        string graphAPIEndpoint = "https://graph.microsoft.com/v1.0/me";

        //Set the scope for API call to user.read
        string[] scopes = new string[] { "user.read" };


        // Initialize HttpClient
        static readonly HttpClient client = new HttpClient() { Timeout = TimeSpan.FromSeconds(3) };

        static readonly JsonSerializerOptions _options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        //static string? userEmail = null;
        static UserInfo userInfo = new UserInfo()
        {

            EmailAddress = Application.Current.Properties["Email"]?.ToString()

        };
        //static DateTimeOffset? expirationTime = null;


        //First get the 'user-scoped' storage information location reference in the assembly
        static readonly IsolatedStorageFile isolatedStorage = IsolatedStorageFile.GetUserStoreForAssembly();
        //create a stream reader object to read content from the created isolated location


        System.Windows.Threading.DispatcherTimer screenshot = new System.Windows.Threading.DispatcherTimer();

        public MainWindow()
        {
            InitializeComponent();
            screenshot.Tick += new EventHandler(ScreenShot);
            screenshot.Interval += new TimeSpan(0, 0, userInfo.Interval);
        }


        public static void ScreenShot(object sender, EventArgs e)
        {
            try
            {
                int screenLeft = System.Windows.Forms.SystemInformation.VirtualScreen.Left;
                int screenTop = System.Windows.Forms.SystemInformation.VirtualScreen.Top;
                int screenWidth = System.Windows.Forms.SystemInformation.VirtualScreen.Width;
                int screenHeight = System.Windows.Forms.SystemInformation.VirtualScreen.Height;

                string date = DateTime.Now.ToString("yyyyMMdd");
                string time = DateTime.Now.ToString("HHmmss");

                using (Bitmap bmp = new Bitmap(screenWidth, screenHeight))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.CopyFromScreen(screenLeft, screenTop, 0, 0, bmp.Size);

                    }
                    if (!Directory.Exists(@"C:\PacBayData\"))
                    {
                        Directory.CreateDirectory(@"C:\PacBayData\");
                    }
                    string default2 = @"C:\PacBayData\";
                    string save = date + "_" + time + ".jpeg";
                    string savePath = default2 + save;
                    bmp.Save(savePath, ImageFormat.Jpeg);
                    //if (Globals.userlast != "Hannah" || Globals.userfirst != "Philip")
                    //{
                    //    ftpupload(save, savePath);
                    //}
                }
            }
            catch (Exception d)
            {
                MessageBox.Show(d.ToString());
            }
        }

        private async void SignIn_Click(object sender, RoutedEventArgs e)
        {
            AuthenticationResult? authResult = null;
            var app = App.PublicClientApp;

            IAccount? firstAccount;


            var accounts = await app.GetAccountsAsync();
            firstAccount = accounts.FirstOrDefault();


            try
            {
                authResult = await app.AcquireTokenSilent(scopes, firstAccount)
                    .ExecuteAsync();
            }
            catch (MsalUiRequiredException ex)
            {
                // A MsalUiRequiredException happened on AcquireTokenSilent. 
                // This indicates you need to call AcquireTokenInteractive to acquire a token
                System.Diagnostics.Debug.WriteLine($"MsalUiRequiredException: {ex.Message}");

                try
                {
                    authResult = await app.AcquireTokenInteractive(scopes)
                        .WithAccount(firstAccount)
                        .WithParentActivityOrWindow(new WindowInteropHelper(this).Handle) // optional, used to center the browser on the window
                        .WithPrompt(Prompt.SelectAccount)
                        .ExecuteAsync();
                }
                catch (MsalException msalex)
                {
                    MessageBox.Show($"Error Acquiring Token:{System.Environment.NewLine}{msalex}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error Acquiring Token Silently:{System.Environment.NewLine}{ex}");
                return;
            }

            if (authResult != null)
            {
                StreamWriter srWriter = new StreamWriter(new IsolatedStorageFileStream("configuration", FileMode.Create, isolatedStorage));

                //DisplayBasicTokenInfo(authResult);
                userInfo.EmailAddress = authResult.Account.Username;
                srWriter.WriteLine(userInfo.EmailAddress);
                srWriter.Flush();
                srWriter.Close();
                this.SignInButton.Visibility = Visibility.Collapsed;
                this.SignOutButton.Visibility = Visibility.Visible;
                this.ClockInButton.IsEnabled = true;
                this.ClockOutButton.IsEnabled = true;
                this.LunchButton.IsEnabled = true;
                GetUserInfo();
                screenshot.Start();

            }
        }

        private async void SignOut_Click(object sender, RoutedEventArgs e)
        {
            client.CancelPendingRequests();
            var accounts = await App.PublicClientApp.GetAccountsAsync();

            if (accounts.Any())
            {
                try
                {
                    await App.PublicClientApp.RemoveAsync(accounts.FirstOrDefault());
                    Result.Text = "User has signed-out";
                    screenshot.Stop();



                }
                catch (MsalException ex)
                {
                    Result.Text = $"Error signing-out user: {ex.Message}";
                }
            }

            isolatedStorage.DeleteFile("configuration");
            this.SignInButton.Visibility = Visibility.Visible;
            this.SignOutButton.Visibility = Visibility.Collapsed;
            this.ClockInButton.IsEnabled = false;
            this.ClockOutButton.IsEnabled = false;
            this.LunchButton.IsEnabled = false;

        }

        public async Task<string> GetHttpContentWithToken(string url, string token)
        {
            var httpClient = new System.Net.Http.HttpClient();
            System.Net.Http.HttpResponseMessage response;
            try
            {
                var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, url);
                //Add the token in Authorization header
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                response = await httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
                return content;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        public static async Task GetRequest(String uri)
        {

            // Call asynchronous network methods in a try/catch block to handle exceptions.
            try
            {
                HttpResponseMessage response = await client.GetAsync(uri);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                string responseCode = response.StatusCode.ToString();

                // Above three lines can be replaced with new helper method below
                // string responseBody = await client.GetStringAsync(uri);

                MessageBox.Show(responseBody);
                MessageBox.Show(responseCode);

            }
            catch (HttpRequestException e)
            {
                MessageBox.Show("Timecard Server is down.\nPlease, Contact Adminitrator!");
            }
        }

        public static async Task PostRequest(String uri)
        {
            try
            {
                var user = JsonSerializer.Serialize(userInfo);

                var request = new HttpRequestMessage(HttpMethod.Post, uri);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Content = new StringContent(user, Encoding.UTF8);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                string responseCode = response.StatusCode.ToString();
                string responseBody = await response.Content.ReadAsStringAsync();
                MessageBox.Show(responseBody, responseCode);

            }
            catch (HttpRequestException e)
            {
                MessageBox.Show("Timecard Server is down.\nPlease, Contact Adminitrator!");
            }
        }

        public static async Task UserInfoRequest(String uri)
        {

            try
            {
                var user = JsonSerializer.Serialize(userInfo);
                var request = new HttpRequestMessage(HttpMethod.Post, uri);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Content = new StringContent(user, Encoding.UTF8);

                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                //string responseCode = response.StatusCode.ToString();
                string responseBody = await response.Content.ReadAsStringAsync();

                userInfo = JsonSerializer.Deserialize<UserInfo>(responseBody, _options);

            }
            catch (HttpRequestException e)
            {
                MessageBox.Show("Timecard Server is down.\nPlease, Contact Adminitrator!");
            }
        }

        private void Lunch_Click(object sender, RoutedEventArgs e)
        {
            client.CancelPendingRequests();
            var uri = "http://localhost/timecard/";
            Task task = GetRequest(uri);
        }

        private void ClockIn_Click(object sender, RoutedEventArgs e)
        {
            client.CancelPendingRequests();
            try
            {
                var uri = "http://localhost/timecard/store?type=clockin";
                Task task = PostRequest(uri);

                Result.Text += $"Username: {userInfo.EmailAddress}" + Environment.NewLine;
                Result.Text += $"Screen Shot Intervals: {(int)userInfo.Interval}" + Environment.NewLine;
                if (userInfo.Interval > 0)
                {
                    screenshot.Interval = new TimeSpan(0, 0, (int)userInfo.Interval);
                }

            }
            catch
            {
                MessageBox.Show("Timecard Server is down.\nPlease, Contact Adminitrator!");

            }

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            try
            {
                StreamReader srReader = new StreamReader(new IsolatedStorageFileStream("configuration", FileMode.Open, isolatedStorage));
                userInfo.EmailAddress = Application.Current.Properties["EmailAddress"]?.ToString();

                this.SignInButton.Visibility = Visibility.Collapsed;
                this.SignOutButton.Visibility = Visibility.Visible;
                this.ClockInButton.IsEnabled = true;
                this.ClockOutButton.IsEnabled = true;
                this.LunchButton.IsEnabled = true;

                srReader.Close();
                GetUserInfo();
                screenshot.Interval += new TimeSpan(0, 0, userInfo.Interval);

                screenshot.Start();
            }
            catch
            {
            }

        }

        private void GetUserInfo()
        {
            var uri = "http://localhost/timecard/get-user-info";
            Task task = UserInfoRequest(uri);
        }
    }
}
