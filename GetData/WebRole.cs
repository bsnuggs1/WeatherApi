using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace GetData
{
    public class WebRole : RoleEntryPoint
    {
        public override bool OnStart()
        {
            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            return base.OnStart();
        }

        public async Task<HttpResponseMessage> getWundergroundData()
        {
            HttpResponseMessage response = null;
            string apiKey = CloudConfigurationManager.GetSetting("WundergroundApiKey");

            using (var client = new HttpClient())
            {
                try
                {
                    client.BaseAddress = new Uri("http://api.wunderground.com/");
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    response = await client.GetAsync(string.Format("api/{0}/conditions/q/CA/San_Francisco.json", apiKey));

                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Unable to retrieve data from Wunderground!");
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }

            }

            return response;
        }
    }
}
