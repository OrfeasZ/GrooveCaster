using System;
using ServiceStack.DataAnnotations;

namespace GrooveCaster.Models
{
    public class StatisticsUnit
    {
        public enum UnitType
        {
            String,
            Double,
            Integer,
            Bool
        }

        [PrimaryKey]
        public String ID { get { return Key + "/" + Date; } }

        [Index]
        public String Key { get; set; }

        [Index]
        public DateTime Date { get; set; }

        public String StringValue { get; set; }

        public double DoubleValue { get; set; }

        public Int64 IntegerValue { get; set; }

        public bool BoolValue { get; set; }

        public UnitType Type { get; set; }
    }
}
