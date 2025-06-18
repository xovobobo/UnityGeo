using UnityEngine;


namespace CustomGeo
{
    public class TileObjectEpsg4978 : TileBase
    {
        private new MapEpsg4978 map_;

        public override void GenerateTile(MonoBehaviour parent)
        {
            map_ = parent.GetComponent<MapEpsg4978>();
            Transform hp_tf = this.transform;

            hp_tf.localPosition = new Vector3(map_.transform.position.x, map_.transform.position.y, map_.transform.position.z);
            hp_tf.localRotation = Quaternion.identity;

            MeshFilter meshFilter = this.gameObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = this.gameObject.AddComponent<MeshRenderer>();

#if UNITY_PIPELINE_HDRP
   meshRenderer.material = new Material(Shader.Find("HDRP/Lit"));    
#else
            meshRenderer.material = new Material(Shader.Find("Standard"));
#endif


            Mesh mesh = new Mesh();

            CustomGeo.Tile tile = new CustomGeo.Tile(x, y, zoom);
            var bounds = tile.boundsEpsg4978_2(map_.altOrigin);

            Vector3[] vertices = new Vector3[4] {
                CustomGeo.GeoConverter.ECEFToUnity(bounds.Topleft, map_.ecef_origin, map_.ecef_origin_rot).Vector3f(),
                CustomGeo.GeoConverter.ECEFToUnity(bounds.Topright, map_.ecef_origin, map_.ecef_origin_rot).Vector3f(),
                CustomGeo.GeoConverter.ECEFToUnity(bounds.Bottom_right, map_.ecef_origin, map_.ecef_origin_rot).Vector3f(),
                CustomGeo.GeoConverter.ECEFToUnity(bounds.Bottom_left, map_.ecef_origin, map_.ecef_origin_rot).Vector3f()
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
            assignTexture();
        }
    }
}
