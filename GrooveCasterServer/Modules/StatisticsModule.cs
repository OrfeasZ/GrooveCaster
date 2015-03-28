using System;
using System.Collections.Generic;
using System.Text;
using GrooveCaster.Managers;
using GrooveCaster.Models;
using Nancy;
using Nancy.Security;
using Newtonsoft.Json;

namespace GrooveCaster.Modules
{
    public class StatisticsModule : NancyModule
    {
        public StatisticsModule()
        {
            this.RequiresAuthentication();

            Get["/stats"] = p_Parameters =>
            {
                return "";
            };

            Get["/stats/listeners/{period}.json"] = p_Parameters =>
            {
                String s_Period = p_Parameters.period;

                var s_Units = new List<StatisticsUnit>();

                switch (s_Period)
                {
                    case "day":
                        s_Units = StatisticsManager.GetUnits("lsnr", DateTime.UtcNow.AddDays(-1));
                        break;

                    case "week":
                        s_Units = StatisticsManager.GetUnits("lsnr", DateTime.UtcNow.AddDays(-7));
                        break;

                    case "month":
                        s_Units = StatisticsManager.GetUnits("lsnr", DateTime.UtcNow.AddMonths(-1));
                        break;

                    case "year":
                        s_Units = StatisticsManager.GetUnits("lsnr", DateTime.UtcNow.AddYears(-1));
                        break;
                }

                var s_Serialized = JsonConvert.SerializeObject(s_Units);
                var s_Encoded = Encoding.UTF8.GetBytes(s_Serialized);

                return new Response()
                {
                    ContentType = "application/json",
                    Contents = p_Writer => p_Writer.Write(s_Encoded, 0, s_Encoded.Length)
                };
            };
        }
    }
}
