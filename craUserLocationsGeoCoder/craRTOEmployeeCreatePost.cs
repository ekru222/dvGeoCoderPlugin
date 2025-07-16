using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace craUserLocationsGeoCoder
{
    public class craRTOEmployeeCreatePost : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
            ITracingService tracing = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            Entity NewEmployee = (Entity)context.InputParameters["Target"];

            if (!NewEmployee.Contains("cra33_Latitude") || !NewEmployee.Contains("cra33_Longitude")) return;

            var personLat = NewEmployee.GetAttributeValue<double>("cra33_Latitude");
            var personLon = NewEmployee.GetAttributeValue<double>("cra33_Longitude");
            var personId = NewEmployee.Id;
            tracing.Trace($"Person ID = {personId}");
            Console.WriteLine($"Person ID = {personId}");

            var locationQuery = new QueryExpression("cr736_doioccupancybuildingslist")
            {
                ColumnSet = new ColumnSet("cr736_doioccupancybuildingslistid", "cr736_latitude", "cr736_longitude")
            };

            var allLocations = service.RetrieveMultiple(locationQuery);

            foreach (Entity location in allLocations.Entities)
            {
                var locationLat = location.GetAttributeValue<double>("cr736_latitude");
                var locationLon = location.GetAttributeValue<double>("cr736_longitude");

                var distance = DistanceCalculator.CalculateDistance(
                    personLat, personLon,
                    locationLat, locationLon);

                if (distance <= 50)
                {
                    var junction = new Entity("cra33_peoplebuildingjunction");
                    junction["cra33_employee"] = personId;
                    junction["cra33_building"] = location.Id;
                    junction["cra33_distance"] = distance;

                    service.Create(junction);
                }
            }
        }
    }

public static class DistanceCalculator
    {
        public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double earthRadius = 3959;

            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return earthRadius * c;
        }

        private static double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }
    }

}       
// just addding a comment to ensure I'm in the lastest git version.

