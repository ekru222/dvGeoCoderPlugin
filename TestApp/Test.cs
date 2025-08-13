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
    public void craUserLocationTestPluginLogic()
    {
        // Arrange
        var context = new Mock<IPluginExecutionContext>();
        var serviceFactory = new Mock<IOrganizationServiceFactory>();
        var service = new Mock<IOrganizationService>();
        var serviceProvider = new Mock<IServiceProvider>();
        var tracing = new Mock<ITracingService>();
        
        // Setup the service provider to return our mocked services
        serviceProvider.Setup(s => s.GetService(typeof(IPluginExecutionContext)))
                      .Returns(context.Object);
        serviceProvider.Setup(s => s.GetService(typeof(IOrganizationServiceFactory)))
                      .Returns(serviceFactory.Object);
        serviceProvider.Setup(s => s.GetService(typeof(ITracingService)))
                      .Returns(tracing.Object);
        
        // Setup mocks with realistic data
        context.Setup(c => c.InputParameters).Returns(new ParameterCollection());
        context.Setup(c => c.UserId).Returns(Guid.NewGuid());
        context.Setup(c => c.MessageName).Returns("Create");
        context.Setup(c => c.PrimaryEntityName).Returns("cra33_employee");
        context.Setup(c => c.Stage).Returns(40); // Post-operation stage
        
        serviceFactory.Setup(f => f.CreateOrganizationService(It.IsAny<Guid>()))
                     .Returns(service.Object);
        
        // Setup a target entity with the latitude and longitude your plugin needs
        var targetEntity = new Entity("cra33_employee", Guid.NewGuid());
        targetEntity["cra33_Latitude"] = 40.7128;  // Example: NYC latitude
        targetEntity["cra33_Longitude"] = -74.0060; // Example: NYC longitude
        context.Setup(c => c.InputParameters).Returns(new ParameterCollection 
        { 
            { "Target", targetEntity } 
        });

        // Read CSV and convert to EntityCollection
        // testing new var csvPath below var csvPath = Path.Combine(Directory.GetCurrentDirectory(), "buildings.csv"); 
        var csvPath = "buildings.csv"; // Just the filename
        var csvData = ParseCsv(csvPath);
        var fakeBuildings = new EntityCollection();

        foreach (dynamic row in csvData)
        {
            var building = new Entity("cr736_doioccupancybuildingslist", Guid.NewGuid());

            // Convert dynamic to dictionary for easier property checking
            var rowDict = (IDictionary<string, object>)row;

            if (rowDict.ContainsKey("latitude"))
                building["cr736_latitude"] = Convert.ToDouble(rowDict["latitude"]);

            if (rowDict.ContainsKey("longitude"))
                building["cr736_longitude"] = Convert.ToDouble(rowDict["longitude"]);

            if (rowDict.ContainsKey("name") && rowDict["name"] != null)
                building["cr736_name"] = rowDict["name"].ToString();

            fakeBuildings.Entities.Add(building);
        }

        // Mock the service to return CSV data
        service.Setup(s => s.RetrieveMultiple(It.IsAny<QueryExpression>()))
               .Returns(fakeBuildings);
        
        // Act
        var plugin = new craRTOEmployeeCreatePost();
        
        // This shouldn't throw an exception
        plugin.Execute(serviceProvider.Object);
        
        // Assert
        // Verify that your plugin called the organization service as expected
        service.Verify(s => s.RetrieveMultiple(It.IsAny<QueryExpression>()), Times.Once);
        
        // Verify junction records were created (you can adjust this based on your CSV data)
        service.Verify(s => s.Create(It.Is<Entity>(e => e.LogicalName == "cra33_peoplebuildingjunction")), Times.AtLeastOnce);
        
        // You can also verify context was accessed
        context.Verify(c => c.InputParameters, Times.AtLeastOnce);
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
        context.Setup(c => c.UserId).Returns(testUserId);
        
        var targetEntity = new Entity("cra33_employee", Guid.NewGuid());
        targetEntity["cra33_Latitude"] = 41.8781;  // Example: Chicago latitude
        targetEntity["cra33_Longitude"] = -87.6298; // Example: Chicago longitude
        
        context.Setup(c => c.InputParameters).Returns(new ParameterCollection 
        { 
            { "Target", targetEntity } 
        });
        
        serviceFactory.Setup(f => f.CreateOrganizationService(testUserId))
                     .Returns(service.Object);

        // Read CSV and convert to EntityCollection
        // Read CSV and convert to EntityCollection
        var csvPath = Path.Combine(Directory.GetCurrentDirectory(), "buildings.csv");
        var csvData = ParseCsv(csvPath);
        var fakeBuildings = new EntityCollection();

        foreach (dynamic row in csvData)
        {
            var building = new Entity("cr736_doioccupancybuildingslist", Guid.NewGuid());

            // Convert dynamic to dictionary for easier property checking
            var rowDict = (IDictionary<string, object>)row;

            if (rowDict.ContainsKey("latitude"))
                building["cr736_latitude"] = Convert.ToDouble(rowDict["latitude"]);

            if (rowDict.ContainsKey("longitude"))
                building["cr736_longitude"] = Convert.ToDouble(rowDict["longitude"]);

            if (rowDict.ContainsKey("name") && rowDict["name"] != null)
                building["cr736_name"] = rowDict["name"].ToString();

            fakeBuildings.Entities.Add(building);
        }

        service.Setup(s => s.RetrieveMultiple(It.IsAny<QueryExpression>()))
               .Returns(fakeBuildings);
        
        // Act
        var plugin = new craRTOEmployeeCreatePost();
        plugin.Execute(serviceProvider.Object);
        
        // Assert
        // Verify the service was created with the correct user ID
        serviceFactory.Verify(f => f.CreateOrganizationService(testUserId), Times.Once);
        
        // Verify buildings were queried
        service.Verify(s => s.RetrieveMultiple(It.IsAny<QueryExpression>()), Times.Once);
        
        // Add more specific assertions based on your plugin's logic
    }
    
    [TestMethod]
    [ExpectedException(typeof(InvalidPluginExecutionException))]
    public void craUserLocationTestPluginLogic_ThrowsExceptionForInvalidData()
    {
        // Test error handling - this test expects an exception to be thrown
        var context = new Mock<IPluginExecutionContext>();
        var serviceFactory = new Mock<IOrganizationServiceFactory>();
        var service = new Mock<IOrganizationService>();
        var serviceProvider = new Mock<IServiceProvider>();
        
        serviceProvider.Setup(s => s.GetService(typeof(IPluginExecutionContext)))
                      .Returns(context.Object);
        serviceProvider.Setup(s => s.GetService(typeof(IOrganizationServiceFactory)))
                      .Returns(serviceFactory.Object);
        
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
        plugin.Execute(serviceProvider.Object);
        
        // Assert - Exception is expected, so test passes if exception is thrown
    }
}