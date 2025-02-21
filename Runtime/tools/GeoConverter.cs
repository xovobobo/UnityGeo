using System;

namespace CustomGeo
{
    public class GeoConverter
    {
        public const double EARTH_RADIUS = 6378137.0;
        public static (double lat, double lon) TileToWorldPos(double tile_x, double tile_y, int zoom)
        {
            double n = Math.PI - ((2.0 * Math.PI * tile_y) / Math.Pow(2.0, zoom));
            double lon = (tile_x / Math.Pow(2.0, zoom) * 360.0) - 180.0;
            double lat = 180.0 / Math.PI * Math.Atan(Math.Sinh(n));
            return (lat, lon);
        }

        public static (double lat, double lon) TileToWorldPos(int tile_x, int tile_y, int zoom)
        {
            double n = Math.PI - ((2.0 * Math.PI * tile_y) / Math.Pow(2.0, zoom));
            double lon = (tile_x / Math.Pow(2.0, zoom) * 360.0) - 180.0;
            double lat = 180.0 / Math.PI * Math.Atan(Math.Sinh(n));
            return (lat, lon);
        }

        public static UnityEngineDouble.Vector2d epsg4326_to_epsg3857(double lat, double lon)
        {
            double lat_rad = lat * Math.PI / 180;
            double lon_rad = lon * Math.PI / 180;

            return new UnityEngineDouble.Vector2d(
                EARTH_RADIUS * lon_rad,
                EARTH_RADIUS * Math.Log(Math.Tan(Math.PI / 4 + lat_rad / 2))
            );
        }

        public static UnityEngineDouble.Vector2d epsg3857_to_epsg4326(UnityEngineDouble.Vector2d pose)
        {
            double lat = (Math.Atan(Math.Exp(pose.y / EARTH_RADIUS)) * 2 - Math.PI / 2) * 180 / Math.PI;
            double lon = pose.x / EARTH_RADIUS * 180 / Math.PI;
            return new UnityEngineDouble.Vector2d(lat, lon);
        }


        public static UnityEngineDouble.Vector3d epsg4979_to_epsg4978(double lat, double lon, double alt)
        {
            const double A = 6378137.0; // Semi-major axis
            const double B = 6356752.314245; // Semi-minor axis
            const double E2 = (A * A - B * B) / (A * A);

            double lat_rad = lat * Math.PI / 180;
            double lon_rad = lon * Math.PI / 180;

            double N = A / Math.Sqrt(1 - E2 * Math.Pow(Math.Sin(lat_rad), 2));

            double X = (N + alt) * Math.Cos(lat_rad) * Math.Cos(lon_rad);
            double Y = (N + alt) * Math.Cos(lat_rad) * Math.Sin(lon_rad);
            double Z = (Math.Pow(B, 2) / Math.Pow(A, 2) * N + alt) * Math.Sin(lat_rad);
            return new UnityEngineDouble.Vector3d(X, Y, Z);
        }

        public static UnityEngineDouble.Vector3d ECEFToUnity(UnityEngineDouble.Vector3d pose, UnityEngineDouble.Vector3d ecef_origin_ref)
        {
            return new UnityEngineDouble.Vector3d(
                pose.x - ecef_origin_ref.x,
                pose.z - ecef_origin_ref.z,
                pose.y - ecef_origin_ref.y
            );
        }

        public static UnityEngineDouble.Vector3d epsg3857ToUnity(UnityEngineDouble.Vector2d pose, UnityEngineDouble.Vector2d origin_ref)
        {
            return new UnityEngineDouble.Vector3d(
                origin_ref.x - pose.x,
                0,
                origin_ref.y - pose.y
            );
        }

        public static UnityEngineDouble.Vector3d epsg4978_to_epsg4979(double x, double y, double z)
        {
            double a = 6378137; // radius
            double e = 8.1819190842622e-2;  // eccentricity
            double asq = Math.Pow(a, 2);
            double esq = Math.Pow(e, 2);

            double b = Math.Sqrt(asq * (1 - esq));
            double bsq = Math.Pow(b, 2);
            double ep = Math.Sqrt((asq - bsq) / bsq);
            double p = Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2));
            double th = Math.Atan2(a * z, b * p);

            double lon = Math.Atan2(y, x);
            double lat = Math.Atan2((z + Math.Pow(ep, 2) * b * Math.Pow(Math.Sin(th), 3)), (p - esq * a * Math.Pow(Math.Cos(th), 3)));
            double N = a / (Math.Sqrt(1 - esq * Math.Pow(Math.Sin(lat), 2)));
            double alt = p / Math.Cos(lat) - N;

            lon = lon % (2 * Math.PI);

            lat = lat * 180 / Math.PI;
            lon = lon * 180 / Math.PI;
            return new UnityEngineDouble.Vector3d(lat, lon, alt);
        }

        public static (double x, double y) WorldToTilePos(double lon, double lat, int zoom)
        {
            double x = (lon + 180.0) / 360.0 * (1 << zoom);
            double y = (1.0 - Math.Log(Math.Tan(lat * Math.PI / 180.0) + 1.0 / Math.Cos(lat * Math.PI / 180.0)) / Math.PI) / 2.0 * (1 << zoom);
            return (x, y);
        }
    }
}