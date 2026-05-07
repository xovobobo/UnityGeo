using UnityEngine;

namespace CustomGeo
{
    public class MapEpsg3857 : MapBase
    {
        [Header("Debug")]
        public UnityEngineDouble.Vector2d epsg3857_origin;

        protected override void init()
        {
            epsg3857_origin = GeoConverter.epsg4326_to_epsg3857(LatOrigin, LonOrigin);
        }

        public override UnityEngineDouble.Vector3d GetLLAAtPosition(Vector3 worldPos)
        {
            Vector3 localPos = transform.InverseTransformPoint(worldPos);
            var p = new UnityEngineDouble.Vector2d(localPos.x, localPos.z);
            var currentEpsg3857 = epsg3857_origin + p;

            var lla2d = GeoConverter.epsg3857_to_epsg4326(currentEpsg3857);
            return new UnityEngineDouble.Vector3d(lla2d.x, lla2d.y, 0);
        }

        public override void SpawnTile(int tx, int ty, int z)
        {
            var key = (z, tx, ty);
            if (activeTiles.ContainsKey(key)) return;

            Transform parentFolder = GetTileParent(z, tx);

            GameObject tile_obj = new GameObject($"{ty}");
            tile_obj.transform.parent = parentFolder;
            tile_obj.layer = tileObjectsLayer;

            var tileScript = tile_obj.AddComponent<TileObjectEpsg3857>();
            tileScript.Initialize(tx, ty, z, this);
            activeTiles.Add(key, tileScript);
        }

        public override Vector3 GetWorldPositionFromLLA(UnityEngineDouble.Vector3d lla)
        {
            UnityEngineDouble.Vector2d target3857 = GeoConverter.epsg4326_to_epsg3857(lla.x, lla.y);
            double offsetX = target3857.x - epsg3857_origin.x;
            double offsetZ = target3857.y - epsg3857_origin.y;
            Vector3 localPos = new Vector3((float)offsetX, (float)lla.z, (float)offsetZ);
            return transform.TransformPoint(localPos);
        }

    }
}
