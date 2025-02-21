using UnityEngine;
using Unity.Geospatial.HighPrecision;

namespace CustomGeo
{
    [RequireComponent(typeof(HPRoot))]
    public class MapEpsg3857 : MapBase
    {
        [Header("Debug")]
        public UnityEngineDouble.Vector2d epsg3857_origin;

        public void Start()
        {
            generateBlocks();
        }

        public override void generateBlocks()
        {
            CustomGeo.Tile center_tile = new CustomGeo.Tile(lat: LatOrigin, lon: LonOrigin, zoom: zoom);
            epsg3857_origin = CustomGeo.GeoConverter.epsg4326_to_epsg3857(LatOrigin, LonOrigin);

            for (int x = -blocks; x <= blocks; x++)
            {
                for (int y = -blocks; y <= blocks; y++)
                {
                    int tile_x = center_tile.x + x;
                    int tile_y = center_tile.y + y;

                    GameObject tile_object = new GameObject($"{tile_x}/{tile_y}/{zoom}");
                    TileObjectEpsg3857 tileScript = tile_object.AddComponent<TileObjectEpsg3857>();
                    tileScript.Initialize(tile_x, tile_y, zoom, this);
                }
            }
        }
    }
}
