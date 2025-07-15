using System;

class Program
{
    static void Main()
    {
        // Test with known locations
        // Denver, CO to Colorado Springs, CO (about 70 miles)
        double denverLat = 39.7392;
        double denverLon = -104.9903;
        double springsLat = 38.8339;
        double springsLon = -104.8214;
        
        double distance = DistanceCalculator.CalculateDistance(denverLat, denverLon, springsLat, springsLon);
        
        Console.WriteLine($"Distance: {distance:F2} miles");
        
        // Test your 50-mile threshold
        Console.WriteLine($"Within 50 miles: {distance <= 50}");


    }
}

public static class DistanceCalculator
{
    public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadius = 3959; // miles
        
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