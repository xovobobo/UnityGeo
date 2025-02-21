using Unity.Geospatial.HighPrecision;
using UnityEngine;


namespace CustomGeo
{
    [RequireComponent(typeof(HPTransform))]
    public class CoordinateGetterEpsg3857 : MonoBehaviour
    {
        public MapEpsg3857 map;

        [Header("Debug")]
        public UnityEngineDouble.Vector2d espg3857;
        public UnityEngineDouble.Vector2d espg4326;

        private HPTransform hptf_;

        void Start()
        {
            hptf_ = GetComponent<HPTransform>();
        }

        void Update()
        {
            var local_pose = new UnityEngineDouble.Vector2d(hptf_.LocalPosition.x, hptf_.LocalPosition.z);
            espg3857 = map.epsg3857_origin + local_pose;

            espg4326 = CustomGeo.GeoConverter.epsg3857_to_epsg4326(espg3857);
            Debug.Log($"LLA: <a href=\"https://maps.google.com/?q={espg4326.x},{espg4326.y}&spn\">{espg4326.x} {espg4326.y}</a>");
        }
    }
}
