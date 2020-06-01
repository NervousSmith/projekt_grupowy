using Microsoft.ML.Data;
using Microsoft.ML.Transforms;
using System;
using System.Globalization;
using System.IO;

namespace projekt_grupowy
{
    public class SingleRow
    {
        public float first_event;

        private string receiver_zip;

        private string sender_zip;


        public float last_event;

        [ColumnName("Label")]
        public float time_diffrence = 0;

        public float distance = 0;

        private static string codesFileName = "Kody.csv";
        private string ZipCodesFilePath = Environment.CurrentDirectory + "\\" + codesFileName;
        public SingleRow(string first_event_time, string first_event_date, string receiver_zip, string sender_zip, string last_event_time, string last_event_date)
        {
            this.first_event = convertToUnixTime(first_event_date, first_event_time);
            this.receiver_zip = receiver_zip;
            this.sender_zip = sender_zip;
            this.distance = FindDistance(ZipCodesFilePath);
            this.last_event = convertToUnixTime(last_event_date, last_event_time);
            this.time_diffrence = last_event - first_event;
        }

        public SingleRow(string first_event_time, string first_event_date, string receiver_zip, string sender_zip)
        {
            this.first_event = convertToUnixTime(first_event_date, first_event_time);
            this.receiver_zip = receiver_zip;
            this.sender_zip = sender_zip;
            this.distance = FindDistance(ZipCodesFilePath);
            this.last_event = 0;
        }

        private float convertToUnixTime(string date, string time)
        {
            CultureInfo culture = new CultureInfo("en-US");
            string dateTimeToParse = date + " " + time;
            DateTime dateTime = DateTime.ParseExact(dateTimeToParse, "yyyy-MM-dd HH:mm:ss", culture, DateTimeStyles.None);
            DateTimeOffset toUnix = new DateTimeOffset(dateTime);
            return toUnix.ToUnixTimeSeconds();
        }

        public string ToString()
        {
            return "first_event " + first_event.ToString() + " " +
                "receiver_zip " + receiver_zip + " " +
                "sender_zip " + sender_zip + " " +
                "distance " + distance + " " +
                "last_event" + last_event.ToString();
        }

        private float CountDistance(double lat1, double lon1, double lat2, double lon2)
        {
            if ((lat1 == lat2) && (lon1 == lon2))
            {
                return 0;
            }
            else
            {
                double theta = lon1 - lon2;
                double dist = Math.Sin(Deg2Rad(lat1)) * Math.Sin(Deg2Rad(lat2)) + Math.Cos(Deg2Rad(lat1)) * Math.Cos(Deg2Rad(lat2)) * Math.Cos(Deg2Rad(theta));
                dist = Math.Acos(dist);
                dist = Rad2Deg(dist);
                dist = dist * 60 * 1.1515 * 1.609344;
                return (float.Parse(dist.ToString()));
            }
        }

        private double Deg2Rad(double deg)
        {
            return (deg * Math.PI / 180.0);
        }

        private double Rad2Deg(double rad)
        {
            return (rad / Math.PI * 180.0);
        }

        private float FindDistance(string csvFilePath)
        {
            StreamReader reader = new StreamReader(File.OpenRead(csvFilePath));
            float receiver_lat = 0;
            float receiver_lon = 0;
            float sender_lat = 0;
            float sender_lon = 0;
            float countedDistance = 1;
            bool foundRecevier = false;
            bool foundSender = false;

            while ((foundRecevier.Equals(false) && foundRecevier.Equals(false)))
            {
                string line = reader.ReadLine();
                if (!string.IsNullOrWhiteSpace(line))
                {
                    string[] values = line.Split(',');
                    if (foundRecevier.Equals(false))
                    {
                        if (values[0].Equals(receiver_zip))
                        {
                            receiver_lat = float.Parse(values[1]);
                            receiver_lon = float.Parse(values[2]);
                            foundRecevier = true;
                        }
                    }

                    if (foundSender.Equals(false))
                    {
                        if (values[0].Equals(sender_zip))
                        {
                            sender_lat = float.Parse(values[1]);
                            sender_lon = float.Parse(values[2]);
                            foundSender = true;
                        }
                    }
                    if (reader.EndOfStream)
                    {
                        foundRecevier = true;
                        foundSender = true;
                        countedDistance = 0;
                    }

                }
            }
            reader.Close();
            reader.Dispose();
            if (countedDistance != 0)
            {
                countedDistance = CountDistance(receiver_lat, receiver_lon, sender_lat, sender_lon);
            }
            return countedDistance;
        }

       
    }
}