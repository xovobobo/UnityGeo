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

            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            var tile = new Tile(x, y, zoom);
            var bounds = tile.boundsEpsg4978_2(map.altOrigin);
            var (origin, rot) = map.GetOriginEcef();
            CreateMesh(
                new Vector3[4] {
                    GeoConverter.ECEFToUnity(bounds.Topleft, origin, rot).Vector3f(),
                    GeoConverter.ECEFToUnity(bounds.Topright, origin, rot).Vector3f(),
                    GeoConverter.ECEFToUnity(bounds.Bottom_right, origin, rot).Vector3f(),
                    GeoConverter.ECEFToUnity(bounds.Bottom_left, origin, rot).Vector3f()
                }
            );
        }
    }
}
