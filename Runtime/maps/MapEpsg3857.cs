using UnityEngine;

namespace CustomGeo
{
    public class MapEpsg3857 : MapBase
    {
        private readonly object _originLock = new object();

        [Header("Debug")]
        public UnityEngineDouble.Vector2d epsg3857_origin;

        protected override void init()
        {
            lock (_originLock)
            {
                epsg3857_origin = GeoConverter.epsg4326_to_epsg3857(LatOrigin, LonOrigin);
            }
        }

        public UnityEngineDouble.Vector2d GetOriginEpsg3857()
        {
            // Value-type copy: callers get a consistent snapshot.
            lock (_originLock)
            {
                return epsg3857_origin;
            }
        }

        public override UnityEngineDouble.Vector3d GetLLAAtPosition(Vector3 worldPos)
        {
            Vector3 localPos = transform.InverseTransformPoint(worldPos);
            var p = new UnityEngineDouble.Vector2d(localPos.x, localPos.z);
            var origin = GetOriginEpsg3857();
            var currentEpsg3857 = origin + p;

            var lla2d = GeoConverter.epsg3857_to_epsg4326(currentEpsg3857);
            return new UnityEngineDouble.Vector3d(lla2d.x, lla2d.y, 0);
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

                var tileScript = tile_obj.AddComponent<TileObjectEpsg3857>();
                tileScript.Initialize(tx, ty, z, this);
                activeTiles.Add(key, tileScript);
            }
        }

        public override Vector3 GetWorldPositionFromLLA(UnityEngineDouble.Vector3d lla)
        {
            UnityEngineDouble.Vector2d target3857 = GeoConverter.epsg4326_to_epsg3857(lla.x, lla.y);
            var origin = GetOriginEpsg3857();
            double offsetX = target3857.x - origin.x;
            double offsetZ = target3857.y - origin.y;
            Vector3 localPos = new Vector3((float)offsetX, (float)lla.z, (float)offsetZ);
            return transform.TransformPoint(localPos);
        }

    }
}
