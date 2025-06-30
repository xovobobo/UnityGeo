using UnityEngine;
using System.Collections.Generic;

namespace CustomGeo
{
    public class MapEpsg4978 : MapBase
    {
        public double altOrigin = 0;
        public Transform looking_tf;
        public bool udpateGravity = false;
        public bool generateTiles = false;

        [Header("Debug")]
        public UnityEngineDouble.Vector3d ecef_origin;
        public UnityEngineDouble.QuaternionD ecef_origin_rot;
        private Transform ecef_center_mass_;
        private bool inited_ = false;

        public void Start()
        {
            if (!inited_)
                GenerateOrigin();
            if (generateTiles)
                generateBlocks();
        }

        public void GenerateOrigin()
        {
            UnityEngineDouble.Vector3d epsg4979 = new UnityEngineDouble.Vector3d(LatOrigin, LonOrigin, altOrigin);
            (ecef_origin, ecef_origin_rot) = GetLocalTangent(epsg4979);
            inited_ = true;
        }

        public void Update()
        {
            if (udpateGravity && inited_)
            {
                var direction = ecef_center_mass_.transform.position - looking_tf.transform.position;
                var gravity = new UnityEngineDouble.Vector3d(direction.x, direction.y, direction.z).normalized * Physics.gravity.magnitude;
                Physics.gravity = gravity.Vector3f();
            }
        }

        public (UnityEngineDouble.Vector3d ecef_pose_origin, UnityEngineDouble.QuaternionD ecef_rot_origin) GetLocalTangent(UnityEngineDouble.Vector3d lla)
        {
            var yaw = UnityEngineDouble.QuaternionD.AngleAxis(90 + lla.y, UnityEngineDouble.Vector3d.up);
            var pitch = UnityEngineDouble.QuaternionD.AngleAxis(90 - lla.x, UnityEngineDouble.Vector3d.right);
            return (
                CustomGeo.GeoConverter.epsg4979_to_epsg4978(lla.x, lla.y, lla.z),
                pitch * yaw
            );
        }

        public override void generateBlocks()
        {
            if (generateTiles)
            {
                CustomGeo.Tile tile_main = new CustomGeo.Tile(lat: LatOrigin, lon: LonOrigin, zoom: zoom);
                tiles = new GameObject("tiles");
                tiles.transform.parent = this.transform;

                var ecef_0_0_0 = new UnityEngineDouble.Vector3d(this.transform.position.x, this.transform.position.y, this.transform.position.z);
                var unity_ecef_0_0_0 = CustomGeo.GeoConverter.ECEFToUnity(ecef_0_0_0, ecef_origin, ecef_origin_rot).Vector3f();

                GameObject center_mass = new GameObject("center_mass");
                ecef_center_mass_ = center_mass.transform;

                ecef_center_mass_.transform.position = unity_ecef_0_0_0;

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
                        tile_object.transform.parent = tiles.transform;

                        TileObjectEpsg4978 tileScript = tile_object.AddComponent<TileObjectEpsg4978>();
                        tileScript.Initialize(tile_x, tile_y, zoom, this);
                    }
                }
            }
        }
    }
}
