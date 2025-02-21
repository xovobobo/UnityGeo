using Unity.Geospatial.HighPrecision;
using UnityEngine;


namespace CustomGeo
{
    [RequireComponent(typeof(HPTransform))]
    public class TileObjectEpsg4978 : TileBase
    {
        private MapEpsg4978 map_;

        public override void GenerateTile(MonoBehaviour parent)
        {
            map_ = parent.GetComponent<MapEpsg4978>();
            HPRoot hp_root = parent.GetComponent<HPRoot>();
            HPTransform hp_tf = GetComponent<HPTransform>();

            var hpposition = new Unity.Mathematics.double3(
                hp_root.transform.localPosition.x,
                0,
                hp_root.transform.localPosition.z
            );
            hp_tf.SetLocalPosition(hpposition);
            hp_tf.LocalRotation = new Quaternion(
                (float)map_.ecef_origin_rot.x,
                (float)map_.ecef_origin_rot.y,
                (float)map_.ecef_origin_rot.z,
                (float)map_.ecef_origin_rot.w
            );


            MeshFilter meshFilter = this.gameObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = this.gameObject.AddComponent<MeshRenderer>();
            meshRenderer.material = new Material(Shader.Find("Standard"));

            Mesh mesh = new Mesh();

            CustomGeo.Tile tile = new CustomGeo.Tile(x, y, zoom);
            var bounds = tile.boundsEpsg4978_2(map_.altOrigin);

            Vector3[] vertices = new Vector3[4] {
            CustomGeo.GeoConverter.ECEFToUnity(bounds.Topleft, map_.ecef_origin).Vector3f(),
            CustomGeo.GeoConverter.ECEFToUnity(bounds.Topright, map_.ecef_origin).Vector3f(),
            CustomGeo.GeoConverter.ECEFToUnity(bounds.Bottom_right, map_.ecef_origin).Vector3f(),
            CustomGeo.GeoConverter.ECEFToUnity(bounds.Bottom_left, map_.ecef_origin).Vector3f()
        };

            int[] triangles = new int[6]
            {
            0, 1, 2,
            2, 3, 0
            };

            Vector2[] uv = new Vector2[4]
            {
            new Vector2(0, 1),
            new Vector2(1, 1),
            new Vector2(1, 0),
            new Vector2(0, 0)
            };

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uv;
            mesh.RecalculateNormals();
            meshFilter.mesh = mesh;
            this.transform.parent = parent.transform;
            assignTexture();
        }
    }
}
