using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace projekt_grupowy
{
    class DatePrediction
    {
        [ColumnName("Score")]
        public float time_prediction;

        public DateTime GivePredictedDate(float first_event)
        {
            float date = time_prediction + first_event;
            long dateConversion = Convert.ToInt64(date);
            var dateTime = DateTimeOffset.FromUnixTimeSeconds(dateConversion).DateTime.ToLocalTime();
            if(dateTime.DayOfWeek == DayOfWeek.Saturday)
            {
                dateTime.AddDays(2);
            }
            else if (dateTime.DayOfWeek == DayOfWeek.Sunday)
            {
                dateTime.AddDays(1);
            }
            return dateTime;
        }
    }

   
}
