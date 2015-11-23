using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using System.Threading.Tasks;
using Wapi.Entities;

namespace Wapi.Controllers
{
    [RoutePrefix("api/conditions")]
    public class DefaultController : ApiController
    {
        public HttpResponseMessage lastWundergroundResponse = null;

        string connectionString = CloudConfigurationManager.GetSetting("StorageConnectionString");

        /// <summary>
        /// Initializes the process to test grabbing some data from both apis and storing it
        /// </summary>
        /// <returns></returns>
        [Route("init")]
        [HttpGet]
        public async Task<IHttpActionResult> main()
        {
            CloudTable table = getWeatherTable();
            table.CreateIfNotExists();

            WUConditionsResponseEntity locationConditions = await getWundergroundDataTest();
            double latitude = locationConditions.current_observation.observation_location.latitude;
            double longitude = locationConditions.current_observation.observation_location.longitude;
            IEnumerable<OWStationsResponseEntity> stations = await getOpenWeatherData(latitude, longitude);

            List<double> temperatures = new List<double>();
            List<double> feelTemperatures = new List<double>();
            List<double> windSpeeds = new List<double>();

            //Add Wunderground's data
            temperatures.Add((5.0/9.0) * (locationConditions.current_observation.temp_f + 459.67));
            feelTemperatures.Add((5.0 / 9.0) * (locationConditions.current_observation.feelslike_f + 459.67));
            windSpeeds.Add(locationConditions.current_observation.wind_mph);

            //Add OpenWeather's data
            temperatures.AddRange(stations.Where(stationLocation => stationLocation.last != null && stationLocation.last.main != null).Select(stationLocation => stationLocation.last.main.temp));
            windSpeeds.AddRange(stations.Where(stationLocation => stationLocation.last != null && stationLocation.last.wind != null).Select(stationLocation => stationLocation.last.wind.speed));

            WeatherEntity weather = new WeatherEntity(latitude, longitude);
            weather.temperature = temperatures.Average();
            weather.feelTemperature = feelTemperatures.Average();
            weather.windSpeed = windSpeeds.Average();
            weather.stationsCount = stations.Count() + 1;

            TableOperation insertOperation = TableOperation.InsertOrReplace(weather);
            table.Execute(insertOperation);

            return Ok(weather);
        }

        /// <summary>
        /// Initializes the process to test grabbing some data from both apis and storing it
        /// </summary>
        /// <returns></returns>
        [Route("search")]
        public async Task<HttpResponseMessage> Get([FromUri]CoordinatesModel coordinates)
        {
            CloudTable table = getWeatherTable();

            WUConditionsResponseEntity locationConditions = await getWundergroundData(coordinates.latitude, coordinates.longitude);

            if (locationConditions.current_observation == null)
            {
                var message = string.Format("Unable to find location with latitude: {0} and longitude: {1}!", coordinates.latitude.ToString(), coordinates.longitude.ToString());
                HttpError err = new HttpError(message);
                return Request.CreateResponse(HttpStatusCode.NotFound, err);
            }

            double latitude = locationConditions.current_observation.observation_location.latitude;
            double longitude = locationConditions.current_observation.observation_location.longitude;
            IEnumerable<OWStationsResponseEntity> stations = await getOpenWeatherData(latitude, longitude);

            List<double> temperatures = new List<double>();
            List<double> feelTemperatures = new List<double>();
            List<double> windSpeeds = new List<double>();
            List<double> distances = new List<double>();

            //Add Wunderground's data
            temperatures.Add((5.0 / 9.0) * (locationConditions.current_observation.temp_f + 459.67));
            feelTemperatures.Add((5.0 / 9.0) * (locationConditions.current_observation.feelslike_f + 459.67));
            windSpeeds.Add(locationConditions.current_observation.wind_mph);

            //Add OpenWeather's data
            temperatures.AddRange(stations.Where(stationLocation => stationLocation.last != null && stationLocation.last.main != null).Select(stationLocation => stationLocation.last.main.temp));
            windSpeeds.AddRange(stations.Where(stationLocation => stationLocation.last != null && stationLocation.last.wind != null).Select(stationLocation => stationLocation.last.wind.speed));
            distances.AddRange(stations.Select(stationLocation => stationLocation.distance));

            WeatherEntity weather = new WeatherEntity(latitude, longitude);
            weather.temperature = temperatures.Average();
            weather.feelTemperature = feelTemperatures.Average();
            weather.windSpeed = windSpeeds.Average();
            weather.closestStationMiles = distances.Min();
            weather.stationsCount = stations.Count() + 1;

            TableOperation insertOperation = TableOperation.InsertOrReplace(weather);
            table.Execute(insertOperation);

            return Request.CreateResponse(HttpStatusCode.OK, weather);
        }

        /// <summary>
        /// Retrieves all of the conditions stored in the weather table
        /// </summary>
        /// <returns></returns>
        [Route("")]
        public IHttpActionResult Get()
        {
            CloudTable table = getWeatherTable();

            TableQuery<WeatherEntity> query = new TableQuery<WeatherEntity>();
            IEnumerable<WeatherEntity> conditions = table.ExecuteQuery(query);

            return Ok(conditions);
        }

        /// <summary>
        /// Attemps to delete the condition stored on the weather table
        /// </summary>
        /// <param name="coordinates"></param>
        /// <returns></returns>
        [Route("")]
        public IHttpActionResult Delete([FromUri]CoordinatesModel coordinates)
        {

            CloudTable table = getWeatherTable();

            // Create a retrieve operation that expects a customer entity.
            TableOperation retrieveOperation = TableOperation.Retrieve<WeatherEntity>(coordinates.latitude.ToString(), coordinates.longitude.ToString());

            // Execute the operation.
            TableResult retrievedResult = table.Execute(retrieveOperation);

            // Assign the result to a CustomerEntity.
            WeatherEntity deleteEntity = (WeatherEntity)retrievedResult.Result;

            // Create the Delete TableOperation.
            if (deleteEntity != null)
            {
                TableOperation deleteOperation = TableOperation.Delete(deleteEntity);

                // Execute the operation.
                table.Execute(deleteOperation);

                return Ok(deleteEntity);
            }

            return BadRequest();
        }

        [Route("response/last")]
        [HttpGet]
        public IHttpActionResult getLastWundergroundResponse()
        {
            return Ok(lastWundergroundResponse);
        }

        /// <summary>
        /// Retrieves Data from the Wunderground's Api
        /// </summary>
        /// <returns></returns>
        private async Task<WUConditionsResponseEntity> getWundergroundData(double latitude, double longitude)
        {
            HttpResponseMessage response = null;
            string apiKey = CloudConfigurationManager.GetSetting("WundergroundApiKey");

            using (var client = new HttpClient())
            {
                try
                {
                    client.BaseAddress = new Uri("http://api.wunderground.com/");
                    client.DefaultRequestHeaders.Accept.Clear();
                    //client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    response = await client.GetAsync(string.Format("api/{0}/conditions/q/{1},{2}.json", apiKey, latitude.ToString(), longitude.ToString()));
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Unable to retrieve data from Wunderground!");
                    }
                    lastWundergroundResponse = response;

                }
                catch (Exception ex)
                {
                    throw ex;
                }
                
            }

            return await response.Content.ReadAsAsync<WUConditionsResponseEntity>(); ;
        }

        /// <summary>
        /// Retrieves Data from the Wunderground's Api
        /// </summary>
        /// <returns></returns>
        private async Task<WUConditionsResponseEntity> getWundergroundDataTest()
        {
            HttpResponseMessage response = null;
            string apiKey = CloudConfigurationManager.GetSetting("WundergroundApiKey");

            using (var client = new HttpClient())
            {
                try
                {
                    client.BaseAddress = new Uri("http://api.wunderground.com/");
                    client.DefaultRequestHeaders.Accept.Clear();
                    //client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    response = await client.GetAsync(string.Format("api/{0}/conditions/q/CA/San_Francisco.json", apiKey));

                    if (!response.IsSuccessStatusCode)
                    {
                        lastWundergroundResponse = response;
                        Console.WriteLine("Unable to retrieve data from Wunderground!");
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }

            }

            return await response.Content.ReadAsAsync<WUConditionsResponseEntity>(); ;
        }

        /// <summary>
        /// Retrieves Data from the Open Weather Api
        /// </summary>
        /// <returns></returns>
        private async Task<IEnumerable<OWStationsResponseEntity>> getOpenWeatherData(double lattitude, double longitude)
        {
            HttpResponseMessage response = null;
            string apiKey = CloudConfigurationManager.GetSetting("OpenWeatherMapApiKey");

            using (var client = new HttpClient())
            {
                try
                {
                    client.BaseAddress = new Uri("http://api.openweathermap.org/");
                    client.DefaultRequestHeaders.Accept.Clear();
                    //client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    response = await client.GetAsync(string.Format("data/2.5/station/find?lat={0}&lon={1}&cnt=30&APPID={2}", lattitude.ToString(), longitude.ToString(), apiKey));

                    if (!response.IsSuccessStatusCode)
                    {
                        lastWundergroundResponse = response;
                        Console.WriteLine("Unable to retrieve data from OpenWeatherData!");
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }

            }

            return await response.Content.ReadAsAsync<IEnumerable<OWStationsResponseEntity>>();
        }

        private async Task<IEnumerable<OWStationsResponseEntity>> getOpenWeatherStationData(decimal lattitude, decimal longitude)
        {
            HttpResponseMessage response = null;
            string apiKey = CloudConfigurationManager.GetSetting("OpenWeatherMapApiKey");

            using (var client = new HttpClient())
            {
                try
                {
                    client.BaseAddress = new Uri("http://api.openweathermap.org/");
                    client.DefaultRequestHeaders.Accept.Clear();
                    //client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    response = await client.GetAsync(string.Format("data/2.5/station/find?lat={0}&lon={1}&cnt=30&APPID={2}", lattitude.ToString(), longitude.ToString(), apiKey));

                    if (!response.IsSuccessStatusCode)
                    {
                        lastWundergroundResponse = response;
                        Console.WriteLine("Unable to retrieve data from OpenWeatherData!");
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }

            }

            return await response.Content.ReadAsAsync<IEnumerable<OWStationsResponseEntity>>();
        }

        /// <summary>
        /// Retrieves the Weather Table
        /// </summary>
        /// <returns></returns>
        private CloudTable getWeatherTable()
        {
            // Retrieve the storage account from the connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting("StorageConnectionString"));

            // Create the table client.
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            CloudTable table = tableClient.GetTableReference("weather");
            table.CreateIfNotExists();

            // Create the CloudTable that represents the "weather" table.
            return table;
        }
    }
}
