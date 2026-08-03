using UnityEngine;

namespace CustomGeo
{
    public class TileObjectEpsg3857 : TileBase
    {
        protected override void GenerateTile(MonoBehaviour parent)
        {
            var map = parent.GetComponent<MapEpsg3857>();
            if (!map)
                return;

            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.Euler(0, -180, 0);

            Tile tile = new(x, y, zoom);
            var bounds = tile.boundsEpsg3857();
            var origin = map.GetOriginEpsg3857();
            CreateMesh(
                new Vector3[4] {
                    GeoConverter.epsg3857ToUnity(bounds.Topleft, origin).Vector3f(),
                    GeoConverter.epsg3857ToUnity(bounds.Topright, origin).Vector3f(),
                    GeoConverter.epsg3857ToUnity(bounds.Bottom_right, origin).Vector3f(),
                    GeoConverter.epsg3857ToUnity(bounds.Bottom_left, origin).Vector3f()
                }
            );
        }
    }
}
