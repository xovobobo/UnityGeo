using System;

namespace CustomGeo
{
    public class Tile
    {
        public int x;
        public int y;

        public int zoom;

        public GeoBounds2 boundsEpsg3857()
        {
            (double topleft_lat, double topleft_lon) = GeoConverter.TileToWorldPos(this.x, this.y, this.zoom);
            UnityEngineDouble.Vector2d top_left = GeoConverter.epsg4326_to_epsg3857(topleft_lat, topleft_lon);


            (double topright_lat, double topright_lon) = GeoConverter.TileToWorldPos(this.x + 1, this.y, this.zoom);
            UnityEngineDouble.Vector2d top_right = GeoConverter.epsg4326_to_epsg3857(topright_lat, topright_lon);


            (double bottom_right_lat, double bottom_right_lon) = GeoConverter.TileToWorldPos(this.x + 1, this.y + 1, this.zoom);
            UnityEngineDouble.Vector2d bottom_right = GeoConverter.epsg4326_to_epsg3857(bottom_right_lat, bottom_right_lon);

            (double bottom_left_lat, double bottom_left_lon) = GeoConverter.TileToWorldPos(this.x, this.y + 1, this.zoom);
            UnityEngineDouble.Vector2d bottom_left = GeoConverter.epsg4326_to_epsg3857(bottom_left_lat, bottom_left_lon);

            return new GeoBounds2(top_left, top_right, bottom_right, bottom_left);
        }

        public GeoBounds3 boundsEpsg4978_2(double alt)
        {
            (double topleft_lat, double topleft_lon) = GeoConverter.TileToWorldPos(this.x, this.y, this.zoom);
            UnityEngineDouble.Vector3d top_left = GeoConverter.epsg4979_to_epsg4978(topleft_lat, topleft_lon, alt);


            (double topright_lat, double topright_lon) = GeoConverter.TileToWorldPos(this.x + 1, this.y, this.zoom);
            UnityEngineDouble.Vector3d top_right = GeoConverter.epsg4979_to_epsg4978(topright_lat, topright_lon, alt);


            (double bottom_right_lat, double bottom_right_lon) = GeoConverter.TileToWorldPos(this.x + 1, this.y + 1, this.zoom);
            UnityEngineDouble.Vector3d bottom_right = GeoConverter.epsg4979_to_epsg4978(bottom_right_lat, bottom_right_lon, alt);

            (double bottom_left_lat, double bottom_left_lon) = GeoConverter.TileToWorldPos(this.x, this.y + 1, this.zoom);
            UnityEngineDouble.Vector3d bottom_left = GeoConverter.epsg4979_to_epsg4978(bottom_left_lat, bottom_left_lon, alt);

            return new GeoBounds3(top_left, top_right, bottom_right, bottom_left);
        }

        public Tile(int x, int y, int zoom)
        {
            this.x = x;
            this.y = y;
            this.zoom = zoom;
        }

        public Tile(double lat, double lon, int zoom)
        {
            (double x_tile, double y_tile) = GeoConverter.WorldToTilePos(lon, lat, zoom);

            this.x = (int)Math.Truncate(x_tile);
            this.y = (int)Math.Truncate(y_tile);
            this.zoom = zoom;
        }

        public Tile GetRightTile()
        {
            return new Tile(x + 1, y, zoom);
        }

        public Tile GetLeftTile()
        {
            return new Tile(x - 1, y, zoom);
        }

        public Tile GetTopTile()
        {
            return new Tile(x, y - 1, zoom);
        }

        public Tile GetBottomTile()
        {
            return new Tile(x, y + 1, zoom);
        }

        /// <summary>
        /// Parent tile at zoom-1. A parent covers exactly four children at zoom+1.
        /// </summary>
        public Tile GetParent()
        {
            if (zoom <= 0)
                return this;

            return new Tile(x >> 1, y >> 1, zoom - 1);
        }

        /// <summary>
        /// Child at zoom+1. <paramref name="i"/> and <paramref name="j"/> are 0 or 1.
        /// </summary>
        public Tile GetChild(int i, int j)
        {
            return new Tile((x << 1) + (i & 1), (y << 1) + (j & 1), zoom + 1);
        }

        public void GetLatLonBounds(out double north, out double south, out double west, out double east)
        {
            (north, west) = GeoConverter.TileToWorldPos(x, y, zoom);
            (south, east) = GeoConverter.TileToWorldPos(x + 1, y + 1, zoom);
        }

        public static int Wrap(int index, int zoomLevel)
        {
            int maxTiles = 1 << zoomLevel;
            if (maxTiles <= 0)
                return 0;

            return ((index % maxTiles) + maxTiles) % maxTiles;
        }
    }
}