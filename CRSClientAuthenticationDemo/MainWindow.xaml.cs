using CRSClientAuthenticationDemo.Models;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace CRSClientAuthenticationDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly HttpClient httpClient = new HttpClient();
        public static string EncryptedKey { get; private set; }
        public static string AuthToken { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var macAddress = GetMacAddress();
                EncryptedKey = await FetchEncryptedKey(macAddress);
                LoadingLabel.Content = "Initialization Complete!";
            }
            catch (Exception ex)
            {
                LoadingLabel.Content = "Error initializing: " + ex.Message;
            }
            finally
            {
                LoadingBar.IsIndeterminate = false;
                LoadingBar.Visibility = Visibility.Collapsed;
                ValidateKeyButton.Visibility = Visibility.Visible;
            }
        }

        private async void SecureCallButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var response = await CallSecureEndpoint();
                LoadingLabel.Content = response;
            }
            catch (Exception ex)
            {
                LoadingLabel.Content = "Error Calling: " + ex.Message;
            }
        }

        private async void ValidateKeyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AuthToken = await ValidateEncryptedKey(EncryptedKey);
                LoadingLabel.Content = "Validation Complete!";
            }
            catch (Exception ex)
            {
                LoadingLabel.Content = "Error validating: " + ex.Message;
            }
            finally
            {
                ValidateKeyButton.Visibility = Visibility.Collapsed;
                SecureCallButton.Visibility = Visibility.Visible;
            }
        }

        private string GetMacAddress()
        {
            var nic = NetworkInterface.GetAllNetworkInterfaces()[0];
            return nic.GetPhysicalAddress().ToString();
        }

        private async Task<string> FetchEncryptedKey(string macAddress)
        {
            var timer = Stopwatch.StartNew();
            var apiUrl = "https://localhost:7121/api/token/generate";

            var requestPayload = new GenerateTokenRequest
            {
                MacAddress = macAddress
            };

            var jsonPayload = JsonConvert.SerializeObject(requestPayload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(apiUrl, content);

            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            dynamic responseObject = JsonConvert.DeserializeObject(responseJson);
            timer.Stop();
            var remainingTime = 3000 - (int)timer.ElapsedMilliseconds;
            await Task.Delay(remainingTime > 0 ? remainingTime : 0);

            return responseObject.encryptedToken;
        }

        private async Task<string> ValidateEncryptedKey(string key)
        {
            var apiUrl = "https://localhost:7121/api/token/validate";

            var requestPayload = new ValidateTokenRequest
            {
                EncryptedToken = key
            };

            var jsonPayload = JsonConvert.SerializeObject(requestPayload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(apiUrl, content);

            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            dynamic responseObject = JsonConvert.DeserializeObject(responseJson);
            return responseObject.accessToken;
        }

        private async Task<string> CallSecureEndpoint()
        {
            using(var httpLocalClient = new HttpClient())
            {
                var apiUrl = "https://localhost:7121/api/crs/secure";

                httpLocalClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + AuthToken);
                var response = await httpLocalClient.GetAsync(apiUrl);

                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();
                return responseString;
            }
        }
    }
}