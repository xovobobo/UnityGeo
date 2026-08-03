using UnityEngine;

namespace CustomGeo
{
    public class MapEpsg4978 : MapBase
    {
        private readonly object _originLock = new object();

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
            var (origin, rot) = GetOriginEcef();
            var position = rot.Inverse() * p;

            var ecef = new UnityEngineDouble.Vector3d(
                origin.x + position.x,
                origin.y + position.z,
                origin.z + position.y
            );
            return GeoConverter.epsg4978_to_epsg4979(ecef.x, ecef.y, ecef.z);
        }

        public override void SpawnTile(int tx, int ty, int z)
        {
            lock (StateLock)
            {
                var key = (z, tx, ty);
                if (activeTiles.ContainsKey(key)) return;

                Transform parentFolder = GetTileParent(z, tx);

                GameObject tile_obj = new GameObject($"{ty}");
                tile_obj.transform.SetParent(parentFolder, false);
                tile_obj.layer = tileObjectsLayer;

                var tileScript = tile_obj.AddComponent<TileObjectEpsg4978>();
                tileScript.Initialize(tx, ty, z, this);
                activeTiles.Add(key, tileScript);
            }
        }

        public void Reinitialize(double latOrigin, double lonOrigin, double alt)
        {
            lock (StateLock)
            {
                LatOrigin = latOrigin;
                LonOrigin = lonOrigin;
                altOrigin = alt;
                base.Reinitialize();
            }
        }

        protected override void init()
        {
            UnityEngineDouble.Vector3d lla = new(LatOrigin, LonOrigin, altOrigin);

            var yaw = UnityEngineDouble.QuaternionD.AngleAxis(90 + lla.y, UnityEngineDouble.Vector3d.up);
            var pitch = UnityEngineDouble.QuaternionD.AngleAxis(90 - lla.x, UnityEngineDouble.Vector3d.right);
            lock (_originLock)
            {
                ecef_origin = GeoConverter.epsg4979_to_epsg4978(lla.x, lla.y, lla.z);
                ecef_origin_rot = pitch * yaw;
            }

            var (origin, rot) = GetOriginEcef();
            var unity_ecef_zero = GeoConverter.ECEFToUnity(new UnityEngineDouble.Vector3d(0, 0, 0), origin, rot).Vector3f();
            if (ecef_center_mass_ == null)
                ecef_center_mass_ = new GameObject("center_mass").transform;
            ecef_center_mass_.position = unity_ecef_zero;
            ecef_center_mass_.parent = transform;
        }

        public (UnityEngineDouble.Vector3d origin, UnityEngineDouble.QuaternionD rot) GetOriginEcef()
        {
            lock (_originLock)
            {
                return (ecef_origin, ecef_origin_rot);
            }
        }

        public override Vector3 GetWorldPositionFromLLA(UnityEngineDouble.Vector3d lla)
        {
            UnityEngineDouble.Vector3d targetEcef = GeoConverter.epsg4979_to_epsg4978(lla.x, lla.y, lla.z);
            var (origin, rot) = GetOriginEcef();
            double posX = targetEcef.x - origin.x;
            double posZ = targetEcef.y - origin.y;
            double posY = targetEcef.z - origin.z;

            UnityEngineDouble.Vector3d position = new UnityEngineDouble.Vector3d(posX, posY, posZ);

            UnityEngineDouble.Vector3d localPosDouble = rot * position;

            Vector3 localPos = new Vector3((float)localPosDouble.x, (float)localPosDouble.y, (float)localPosDouble.z);
            return transform.TransformPoint(localPos);
        }
    }
}
