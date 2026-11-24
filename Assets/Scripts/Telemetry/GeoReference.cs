using System;
using UnityEngine;

/// <summary>A simulated GPS fix: latitude/longitude in degrees, altitude in metres above sea level.</summary>
[Serializable]
public struct GeoCoordinate
{
    public double latitude;
    public double longitude;
    public double altitudeMsl;

    public GeoCoordinate(double latitude, double longitude, double altitudeMsl)
    {
        this.latitude = latitude;
        this.longitude = longitude;
        this.altitudeMsl = altitudeMsl;
    }

    public override string ToString() => $"{latitude:F6}, {longitude:F6}, {altitudeMsl:F1} m";
}

/// <summary>
/// Ties the Unity world to a point on Earth so the drone can report GPS coordinates.
/// Convention: world +X = east, +Z = north, +Y = up, 1 unit = 1 metre.
/// Uses a flat-Earth (local tangent plane) approximation, which is accurate to well
/// under a metre over the few kilometres a mission covers.
/// </summary>
[Serializable]
public class GeoReference
{
    const double EarthRadius = 6378137.0; // WGS-84 equatorial radius, metres

    [Tooltip("Latitude of the world origin (degrees).")]
    public double originLatitude = 47.397742;

    [Tooltip("Longitude of the world origin (degrees).")]
    public double originLongitude = 8.545594;

    [Tooltip("Altitude above sea level of world Y = 0 (metres).")]
    public double originAltitudeMsl = 488.0;

    public GeoCoordinate ToGeo(Vector3 world)
    {
        double latRad = originLatitude * Math.PI / 180.0;
        double dLat = world.z / EarthRadius;
        double dLon = world.x / (EarthRadius * Math.Cos(latRad));
        return new GeoCoordinate(
            originLatitude + dLat * 180.0 / Math.PI,
            originLongitude + dLon * 180.0 / Math.PI,
            originAltitudeMsl + world.y);
    }

    public Vector3 ToWorld(GeoCoordinate geo)
    {
        double latRad = originLatitude * Math.PI / 180.0;
        double north = (geo.latitude - originLatitude) * Math.PI / 180.0 * EarthRadius;
        double east = (geo.longitude - originLongitude) * Math.PI / 180.0 * EarthRadius * Math.Cos(latRad);
        return new Vector3((float)east, (float)(geo.altitudeMsl - originAltitudeMsl), (float)north);
    }
}
