using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Wapi.Entities
{

    public class OWStationsResponseEntity
    {
        public Station station { get; set; }
        public double distance { get; set; }
        public Last last { get; set; }

        public class Coord
        {
            public double lon { get; set; }
            public double lat { get; set; }
        }

        public class Station
        {
            public string name { get; set; }
            public int type { get; set; }
            public int status { get; set; }
            public int user_id { get; set; }
            public int id { get; set; }
            public Coord coord { get; set; }
        }

        public class Main
        {
            public double temp { get; set; }
            public double humidity { get; set; }
            public double pressure { get; set; }
        }

        public class Wind
        {
            public double speed { get; set; }
            public int deg { get; set; }
            public double? gust { get; set; }
        }

        public class Visibility
        {
            public int distance { get; set; }
            public int prefix { get; set; }
        }

        public class Calc
        {
            public double dewpoint { get; set; }
            public double humidex { get; set; }
        }

        public class Cloud
        {
            public int distance { get; set; }
            public string condition { get; set; }
        }

        public class Rain
        {
            public int __invalid_name__1h { get; set; }
            public double __invalid_name__24h { get; set; }
            public double today { get; set; }
        }

        public class Weather
        {
            public object precipitation { get; set; }
            public string descriptor { get; set; }
            public object intensity { get; set; }
        }

        public class Last
        {
            public Main main { get; set; }
            public int dt { get; set; }
            public Wind wind { get; set; }
            public Visibility visibility { get; set; }
            public Calc calc { get; set; }
            public List<Cloud> clouds { get; set; }
            public Rain rain { get; set; }
            public List<Weather> weather { get; set; }
        }

    }

}