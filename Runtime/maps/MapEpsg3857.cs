using UnityEngine;

namespace CustomGeo
{
    public class MapEpsg3857 : MapBase
    {
        [Header("Debug")]
        public UnityEngineDouble.Vector2d epsg3857_origin;

        public override void generateBlocks(int layer)
        {
            Tile center_tile = new(lat: LatOrigin, lon: LonOrigin, zoom: zoom);
            epsg3857_origin = GeoConverter.epsg4326_to_epsg3857(LatOrigin, LonOrigin);

            for (int x = -blocks; x <= blocks; x++)
            {
                for (int y = -blocks; y <= blocks; y++)
                {
                    int tile_x = center_tile.x + x;
                    int tile_y = center_tile.y + y;

                    GameObject tile_object = new($"{tile_x}/{tile_y}/{zoom}");
                    tile_object.layer = layer;

                    var tileScript = tile_object.AddComponent<TileObjectEpsg3857>();
                    tileScript.Initialize(tile_x, tile_y, zoom, this);
                }
            }
        }
    }
}
