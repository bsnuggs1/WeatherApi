using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.WindowsAzure.Storage.Table;

namespace Wapi.Entities
{

    public class CoordinatesModel
    {
        public double longitude { get; set; }
        public double latitude { get; set; }

        public override string ToString()
        {
            return longitude.ToString() + " - " + latitude.ToString();
        }
    }

    public class WeatherEntity: TableEntity
    {
        public CoordinatesModel coordinates
        {
            get
            {
                return new CoordinatesModel() { longitude = Convert.ToDouble(this.RowKey), latitude = Convert.ToDouble(this.PartitionKey) };
            }
        }
        public double temperature { get; set; }
        public double temperature_f {
            get
            {
                return temperature * (9.0/5.0) - 459.67;
            }
            set
            {
                temperature = (5.0 / 9.0) * (value + 459.67);
            }
        }
        public double temperature_c
        {
            get
            {
                return temperature - 273.15;
            }
            set
            {
                temperature = value + 273.15;
            }
        }
        public double feelTemperature { get; set; }
        public double windSpeed { get; set; }
        public double closestStationMiles { get; set; }
        public int stationsCount { get; set; }

        public WeatherEntity()
        {
        }

        public WeatherEntity(double latitude, double longitude)
        {
            this.PartitionKey = latitude.ToString();
            this.RowKey = longitude.ToString();
        }

    }

}