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
    }
}