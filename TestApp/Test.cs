using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.IO;
using CsvHelper;
using System.Globalization;
using System.Linq;
using craUserLocationsGeoCoder;
using System.Collections.Generic; // Add this for IEnumerable<dynamic>
using System.Reflection;



// Example method to parse CSV using CsvHelper


[TestClass]
public class craUserLocationTest
{
  
    
    private IEnumerable<dynamic> ParseCsv(string csvPath)
    {
        using (var reader = new StreamReader(csvPath))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            var records = csv.GetRecords<dynamic>().ToList();
            return records;
        }
    }

    
    [TestMethod]
    public void craUserLocationTestPluginLogic_WithSpecificScenario()
    {
        // Arrange - Test a specific scenario with CSV data
        var context = new Mock<IPluginExecutionContext>();
        var serviceFactory = new Mock<IOrganizationServiceFactory>();
        var service = new Mock<IOrganizationService>();
        var serviceProvider = new Mock<IServiceProvider>();
        var tracing = new Mock<ITracingService>();

        // Setup service provider
        serviceProvider.Setup(s => s.GetService(typeof(IPluginExecutionContext)))
                      .Returns(context.Object);
        serviceProvider.Setup(s => s.GetService(typeof(IOrganizationServiceFactory)))
                      .Returns(serviceFactory.Object);
        serviceProvider.Setup(s => s.GetService(typeof(ITracingService)))
                      .Returns(tracing.Object);

        // Setup specific test data
        var testUserId = new Guid("12345678-1234-1234-1234-123456789012");
        var employeeId = Guid.NewGuid();
        context.Setup(c => c.UserId).Returns(testUserId);

        var targetEntity = new Entity("cra33_employee", employeeId);
        targetEntity["cra33_Latitude"] = 39.7941;  // Springfield, IL latitude
        targetEntity["cra33_Longitude"] = -89.6395; // Springfield longitude

        context.Setup(c => c.InputParameters).Returns(new ParameterCollection
    {
        { "Target", targetEntity }
    });

        serviceFactory.Setup(f => f.CreateOrganizationService(testUserId))
                     .Returns(service.Object);

        // Read CSV and convert to EntityCollection (ALL buildings, no filtering here)
        var csvPath = Path.Combine(Directory.GetCurrentDirectory(), "buildings.csv");
        var csvData = ParseCsv(csvPath);
        var fakeBuildings = new EntityCollection();

        foreach (dynamic row in csvData)
        {
            var building = new Entity("cr736_doioccupancybuildingslist", Guid.NewGuid());
            var rowDict = (IDictionary<string, object>)row;

            if (rowDict.ContainsKey("Latitude") && rowDict["Latitude"] != null)
            {
                var latString = rowDict["Latitude"].ToString().Trim();
                if (double.TryParse(latString, out double latitude))
                {
                    building["cr736_latitude"] = latitude;
                }
            }

            if (rowDict.ContainsKey("Longitude") && rowDict["Longitude"] != null)
            {
                var lngString = rowDict["Longitude"].ToString().Trim();
                if (double.TryParse(lngString, out double longitude))
                {
                    building["cr736_longitude"] = longitude;
                }
            }

            if (rowDict.ContainsKey("Building Name") && rowDict["Building Name"] != null)
                building["cr736_name"] = rowDict["Building Name"].ToString();

            fakeBuildings.Entities.Add(building);
        }

        // Mock the service to return ALL CSV data (plugin will do the filtering)
        service.Setup(s => s.RetrieveMultiple(It.IsAny<QueryExpression>()))
               .Returns(fakeBuildings);

        // Track which junction records would be created
        var createdJunctions = new List<Entity>();
        service.Setup(s => s.Create(It.IsAny<Entity>()))
               .Callback<Entity>(entity => createdJunctions.Add(entity));

        // Act
        var plugin = new craRTOEmployeeCreatePost();
        plugin.Execute(serviceProvider.Object);

        // Assert
        service.Verify(s => s.RetrieveMultiple(It.IsAny<QueryExpression>()), Times.Once);

        // Debug: Show which buildings were within 50 miles (created junction records)
        Console.WriteLine($"Buildings within 50 miles of Springfield, IL:");
        foreach (var junction in createdJunctions)
        {
            var distance = junction.GetAttributeValue<double>("cra33_distance");
            Console.WriteLine($"Junction created - Distance: {distance:F2} miles ");
        }

        // Verify that at least some junction records were created (assuming your CSV has buildings within 50 miles of Springfield)
        Assert.IsTrue(createdJunctions.Count > 0, "Expected at least one building within 50 miles");

        // Verify all created junctions have distance <= 50
        Assert.IsTrue(createdJunctions.All(j => j.GetAttributeValue<double>("cra33_distance") <= 50),
                      "All junction records should be within 50 miles");
    }

    [TestMethod]
    //[ExpectedException(typeof(InvalidPluginExecutionException))]
    public void craUserLocationTestPluginLogic_ThrowsExceptionForInvalidData()
    {
        // Test error handling - this test expects an exception to be thrown
        var context = new Mock<IPluginExecutionContext>();
        var serviceFactory = new Mock<IOrganizationServiceFactory>();
        var service = new Mock<IOrganizationService>();
        var serviceProvider = new Mock<IServiceProvider>();
        var tracing = new Mock<ITracingService>();

        serviceProvider.Setup(s => s.GetService(typeof(IPluginExecutionContext)))
                      .Returns(context.Object);
        serviceProvider.Setup(s => s.GetService(typeof(IOrganizationServiceFactory)))
                      .Returns(serviceFactory.Object);
        serviceProvider.Setup(s => s.GetService(typeof(ITracingService)))
                      .Returns(tracing.Object);

        // Setup invalid data - missing required latitude/longitude
        var invalidEntity = new Entity("cra33_employee", Guid.NewGuid());
        // Don't set latitude/longitude - this should cause early return
        context.Setup(c => c.InputParameters).Returns(new ParameterCollection 
        { 
            { "Target", invalidEntity } 
        });
        
        serviceFactory.Setup(f => f.CreateOrganizationService(It.IsAny<Guid>()))
                     .Returns(service.Object);
        
        // Act - This should throw an exception
        var plugin = new craRTOEmployeeCreatePost();

        // Assert - Exception is expected, so test passes if exception is thrown

       Assert.Throws<InvalidPluginExecutionException>(() => plugin.Execute(serviceProvider.Object));


        }
    }

