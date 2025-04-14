using UnityEngine;

namespace CustomGeo
{
    public class CoordinateGetterEpsg3857 : MonoBehaviour
    {
        public MapEpsg3857 map;

        [Header("Debug")]
        public UnityEngineDouble.Vector2d espg3857;
        public UnityEngineDouble.Vector2d espg4326;


        void Update()
        {
            Vector3 localPos = map.transform.InverseTransformPoint(this.transform.position);
            var p = new UnityEngineDouble.Vector2d(
                localPos.x,
                localPos.z
            );

            espg3857 = map.epsg3857_origin + p;

            espg4326 = CustomGeo.GeoConverter.epsg3857_to_epsg4326(espg3857);
            Debug.Log($"LLA: <a href=\"https://maps.google.com/?q={espg4326.x},{espg4326.y}&spn\">{espg4326.x} {espg4326.y}</a>");
        }
    }
}
