using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AzureBillingAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using System.Net.Http.Formatting;
//using Microsoft.Rest.Azure.Authentication;

namespace AzureBillingAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BillingController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<BillingController> _logger;

        public BillingController(IConfiguration configuration, ILogger<BillingController> logger)
        {
            this.configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        public async Task GetUsageAsync()
        {
            //Get the AAD token to get authorized to make the call to the Usage API
            Token token = await GetAuthToken();

            /*Setup API call to Usage API
             Callouts:
             * See the App.config file for all AppSettings key/value pairs
             * You can get a list of offer numbers from this URL: http://azure.microsoft.com/en-us/support/legal/offer-details/
             * See the Azure Usage API specification for more details on the query parameters for this API.
             * The Usage Service/API is currently in preview; please use 2015-06-01-preview for api-version
             * Please see the readme if you are having problems configuring or authenticating: https://github.com/Azure-Samples/billing-dotnet-usage-api
             
            */
            // Build up the HttpWebRequest
            string requestURL;
            requestURL = String.Format("{0}/{1}/{2}/{3}",
                   configuration.GetValue<string>("AzureConfig:ARMBillingServiceURL"),
                   "subscriptions",
                   configuration.GetValue<string>("AzureConfig:SubscriptionID"),
                   "providers/Microsoft.Commerce/UsageAggregates?api-version=2015-06-01-preview&reportedstartTime=2020-04-09+00%3a00%3a00Z&reportedEndTime=2020-04-10+00%3a00%3a00Z");

            //requestURL = $"{configuration.GetValue<string>("AzureConfig:ARMBillingServiceURL"]}/subscriptions/{configuration.GetValue<string>("AzureConfig:SubscriptionID"]}/resourceGroups/{configuration.GetValue<string>("AzureConfig:ResourceGroupName"]}/providers/Microsoft.Compute/virtualMachines/{configuration.GetValue<string>("AzureConfig:VirtualMachineName"]}/providers/microsoft.insights/metrics?api-version=2018-01-01&metricnames=Percentage%20CPU&timespan=2020-01-01T03:00:00Z/2020-09-09T03:00:00Z";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requestURL);

            // Add the OAuth Authorization header, and Content Type header
            request.Headers.Add(HttpRequestHeader.Authorization, $"{token.TokenType} {token.AccessToken}");
            request.ContentType = "application/json";

            // Call the Usage API, dump the output to the console window
            try
            {
                // Call the REST endpoint
                Console.WriteLine("Calling Usage service...");
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Console.WriteLine(String.Format("Usage service response status: {0}", response.StatusDescription));
                Stream receiveStream = response.GetResponseStream();

                // Pipes the stream to a higher level stream reader with the required encoding format. 
                StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8);
                var usageResponse = readStream.ReadToEnd();
                Console.WriteLine("Usage stream received.  Press ENTER to continue with raw output.");
                Console.ReadLine();
                Console.WriteLine(usageResponse);
                Console.WriteLine("Raw output complete.  Press ENTER to continue with JSON output.");
                Console.ReadLine();

                // Convert the Stream to a strongly typed RateCardPayload object.  
                // You can also walk through this object to manipulate the individuals member objects. 
                UsagePayload payload = JsonConvert.DeserializeObject<UsagePayload>(usageResponse);
                Console.WriteLine(usageResponse.ToString());
                response.Close();
                readStream.Close();
                Console.WriteLine("JSON output complete.  Press ENTER to close.");
                Console.ReadLine();
            }
            catch (Exception e)
            {
                Console.WriteLine(String.Format("{0} \n\n{1}", e.Message, e.InnerException != null ? e.InnerException.Message : ""));
                Console.ReadLine();
            }
        }

        public async Task<Token> GetAuthToken() {

            using (var client = new HttpClient())
            {
                var formContent = new FormUrlEncodedContent(new Dictionary<string, string> { 
                    { "client_id", configuration.GetValue<string>("AzureConfig:ClientId") }, 
                    { "client_secret", configuration.GetValue<string>("AzureConfig:ClientSecret") },
                    { "resource", configuration.GetValue<string>("AzureConfig:Resource") },
                    { "grant_type", configuration.GetValue<string>("AzureConfig:GrantType") }
                });
                var tenantId = configuration.GetValue<string>("AzureConfig:TenantID");
                var response = await client.PostAsync($"https://login.microsoftonline.com/{tenantId}/oauth2/token", formContent);

                return await response.Content.ReadAsAsync<Token>();
            }
        }

        public async Task<string> GetOAuthTokenFromAADAsync()
        {
            var authenticationContext = new AuthenticationContext(String.Format("{0}/{1}",
                                                                    configuration.GetValue<string>("AzureConfig:ADALServiceURL"),
                                                                    configuration.GetValue<string>("AzureConfig:TenantDomain")));

            //Ask the logged in user to authenticate, so that this client app can get a token on his behalf
            var result = await authenticationContext.AcquireTokenAsync(String.Format("{0}/", configuration.GetValue<string>("AzureConfig:ARMBillingServiceURL")),
                                                            configuration.GetValue<string>("AzureConfig:ClientID"),
                                                            new Uri(configuration.GetValue<string>("AzureConfig:ADALRedirectURL")), new PlatformParameters(PromptBehavior.Auto, null));

            if (result == null)
            {
                throw new InvalidOperationException("Failed to obtain the JWT token");
            }

            return result.AccessToken;
        }
    }
}
