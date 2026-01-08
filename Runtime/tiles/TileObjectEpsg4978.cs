using UnityEngine;

namespace CustomGeo
{
    public class TileObjectEpsg4978 : TileBase
    {
        protected override void GenerateTile(MonoBehaviour parent)
        {
            var map = parent.GetComponent<MapEpsg4978>();
            if (!map)
                return;

            transform.localPosition = new Vector3(map.transform.position.x, map.transform.position.y, map.transform.position.z);
            transform.localRotation = Quaternion.identity;

            var tile = new Tile(x, y, zoom);
            var bounds = tile.boundsEpsg4978_2(map.altOrigin);
            CreateMesh(
                new Vector3[4] {
                    GeoConverter.ECEFToUnity(bounds.Topleft, map.ecef_origin, map.ecef_origin_rot).Vector3f(),
                    GeoConverter.ECEFToUnity(bounds.Topright, map.ecef_origin, map.ecef_origin_rot).Vector3f(),
                    GeoConverter.ECEFToUnity(bounds.Bottom_right, map.ecef_origin, map.ecef_origin_rot).Vector3f(),
                    GeoConverter.ECEFToUnity(bounds.Bottom_left, map.ecef_origin, map.ecef_origin_rot).Vector3f()
                }
            );
        }
    }
}
