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

            transform.localPosition = new Vector3(parent.transform.localPosition.x, 0, parent.transform.localPosition.z);
            transform.localRotation = Quaternion.Euler(0, -180, 0);

            Tile tile = new(x, y, zoom);
            var bounds = tile.boundsEpsg3857();
            CreateMesh(
                new Vector3[4] {
                    GeoConverter.epsg3857ToUnity(bounds.Topleft, map.epsg3857_origin).Vector3f(),
                    GeoConverter.epsg3857ToUnity(bounds.Topright, map.epsg3857_origin).Vector3f(),
                    GeoConverter.epsg3857ToUnity(bounds.Bottom_right, map.epsg3857_origin).Vector3f(),
                    GeoConverter.epsg3857ToUnity(bounds.Bottom_left, map.epsg3857_origin).Vector3f()
                }
            );
        }
    }
}
