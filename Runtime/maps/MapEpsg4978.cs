using UnityEngine;
using Unity.Geospatial.HighPrecision;
using System.Collections.Generic;

namespace CustomGeo
{
    [RequireComponent(typeof(HPRoot))]
    public class MapEpsg4978 : MapBase
    {
        public double altOrigin = 220;

        [Header("Debug")]
        public UnityEngineDouble.Vector3d ecef_origin;
        public UnityEngineDouble.QuaternionD ecef_origin_rot;

        public UnityEngineDouble.Vector3d start_pose;

        public void Start()
        {
            generateBlocks();
        }

        public (UnityEngineDouble.Vector3d ecef_pose_origin, UnityEngineDouble.QuaternionD ecef_rot_origin) GetLocalTangent(UnityEngineDouble.Vector3d lla)
        {
            var yaw = UnityEngineDouble.QuaternionD.AngleAxis(-(90 - lla.y), UnityEngineDouble.Vector3d.up);
            var pitch = UnityEngineDouble.QuaternionD.AngleAxis(-(90 - lla.x), UnityEngineDouble.Vector3d.right);

            return (
                CustomGeo.GeoConverter.epsg4979_to_epsg4978(lla.x, lla.y, lla.z),
                UnityEngineDouble.QuaternionD.AngleAxis(180, UnityEngineDouble.Vector3d.up) * pitch * yaw
            );
        }


        public override void generateBlocks()
        {
            UnityEngineDouble.Vector3d epsg4979 = new UnityEngineDouble.Vector3d(LatOrigin, LonOrigin, altOrigin);
            (ecef_origin, ecef_origin_rot) = GetLocalTangent(epsg4979);

            CustomGeo.Tile tile_main = new CustomGeo.Tile(lat: LatOrigin, lon: LonOrigin, zoom: zoom);

            int maxTiles = 1 << zoom;

            HashSet<(int, int)> uniqueTiles = new HashSet<(int, int)>();

            for (int x = -blocks; x <= blocks; x++)
            {
                for (int y = -blocks; y <= blocks; y++)
                {
                    int tile_x = tile_main.x + x;
                    int tile_y = tile_main.y + y;

                    tile_x = ((tile_x % maxTiles) + maxTiles) % maxTiles;
                    tile_y = ((tile_y % maxTiles) + maxTiles) % maxTiles;
                    if (!uniqueTiles.Add((tile_x, tile_y)))
                    {
                        continue; // Skip duplicates
                    }

                    GameObject tile_object = new GameObject($"{zoom}/{tile_x}/{tile_y}");
                    TileObjectEpsg4978 tileScript = tile_object.AddComponent<TileObjectEpsg4978>();
                    tileScript.Initialize(tile_x, tile_y, zoom, this);
                }
            }
        }
    }
}
