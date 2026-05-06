using UnityEngine;

namespace CustomGeo
{
    public class MapEpsg4978 : MapBase
    {
        [Header("Epsg4978 Specific")]
        public double altOrigin = 0;
        public bool udpateGravity = false;

        [Header("Debug")]
        public UnityEngineDouble.Vector3d ecef_origin;
        public UnityEngineDouble.QuaternionD ecef_origin_rot;
        private Transform ecef_center_mass_;

        protected override void Update()
        {
            base.Update();
            if (udpateGravity && looking_tf != null && ecef_center_mass_ != null)
            {
                var direction = ecef_center_mass_.position - looking_tf.position;
                Physics.gravity = direction.normalized * 9.81f;
            }
        }

        public override UnityEngineDouble.Vector3d GetLLAAtPosition(Vector3 worldPos)
        {
            var localPos = transform.InverseTransformPoint(worldPos);
            var p = new UnityEngineDouble.Vector3d(localPos.x, localPos.y, localPos.z);
            var position = ecef_origin_rot.Inverse() * p;

            var ecef = new UnityEngineDouble.Vector3d(
                ecef_origin.x + position.x,
                ecef_origin.y + position.z,
                ecef_origin.z + position.y
            );
            return GeoConverter.epsg4978_to_epsg4979(ecef.x, ecef.y, ecef.z);
        }

        public override void SpawnTile(int tx, int ty, int z)
        {
            GameObject tile_obj = new GameObject($"Tile_{z}_{tx}_{ty}");
            tile_obj.transform.parent = tiles.transform;
            tile_obj.layer = tileObjectsLayer;

            var tileScript = tile_obj.AddComponent<TileObjectEpsg4978>();
            tileScript.Initialize(tx, ty, z, this);
            activeTiles.Add((z, tx, ty), tileScript);
        }

        protected override void init()
        {
            UnityEngineDouble.Vector3d lla = new(LatOrigin, LonOrigin, altOrigin);

            var yaw = UnityEngineDouble.QuaternionD.AngleAxis(90 + lla.y, UnityEngineDouble.Vector3d.up);
            var pitch = UnityEngineDouble.QuaternionD.AngleAxis(90 - lla.x, UnityEngineDouble.Vector3d.right);
            ecef_origin = GeoConverter.epsg4979_to_epsg4978(lla.x, lla.y, lla.z);
            ecef_origin_rot = pitch * yaw;

            var unity_ecef_zero = GeoConverter.ECEFToUnity(new UnityEngineDouble.Vector3d(0,0,0), ecef_origin, ecef_origin_rot).Vector3f();
            ecef_center_mass_ = new GameObject("center_mass").transform;
            ecef_center_mass_.position = unity_ecef_zero;
            ecef_center_mass_.parent = transform;
        }
    }
}
