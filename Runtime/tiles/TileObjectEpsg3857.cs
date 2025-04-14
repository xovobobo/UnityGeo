using UnityEngine;

namespace CustomGeo
{
    public class TileObjectEpsg3857 : TileBase
    {
        private MapEpsg3857 map_;

        public override void GenerateTile(MonoBehaviour parent)
        {

            map_ = parent.GetComponent<MapEpsg3857>();
            Transform hp_root = parent.transform;
            Transform hp_tf = this.transform;


            var hpposition = new Vector3(
                hp_root.transform.localPosition.x,
                0,
                hp_root.transform.localPosition.z
            );


            transform.localPosition = hpposition;
            hp_tf.localRotation = Quaternion.Euler(0, -180, 0);

            MeshFilter meshFilter = this.gameObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = this.gameObject.AddComponent<MeshRenderer>();
            meshRenderer.material = new Material(Shader.Find("Standard"));

            Mesh mesh = new Mesh();

            CustomGeo.Tile tile = new CustomGeo.Tile(x, y, zoom);
            var bounds = tile.boundsEpsg3857();

            Vector3[] vertices = new Vector3[4] {
            CustomGeo.GeoConverter.epsg3857ToUnity(bounds.Topleft, map_.epsg3857_origin).Vector3f(),
            CustomGeo.GeoConverter.epsg3857ToUnity(bounds.Topright, map_.epsg3857_origin).Vector3f(),
            CustomGeo.GeoConverter.epsg3857ToUnity(bounds.Bottom_right, map_.epsg3857_origin).Vector3f(),
            CustomGeo.GeoConverter.epsg3857ToUnity(bounds.Bottom_left, map_.epsg3857_origin).Vector3f()
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
