using Unity.Geospatial.HighPrecision;
using UnityEngine;


namespace CustomGeo
{
    [RequireComponent(typeof(HPTransform))]
    public class CoordinateGetter4978 : MonoBehaviour
    {
        public MapEpsg4978 map;


        [Header("Debug")]
        public UnityEngineDouble.Vector3d epsg4978;
        public UnityEngineDouble.Vector3d epsg4979;

        private HPTransform hptf_;

        private void Start()
        {
            hptf_ = GetComponent<HPTransform>();
        }

        private UnityEngineDouble.Vector3d ConvertUnityToECEF(UnityEngineDouble.Vector3d unityPose)
        {
            var position = map.ecef_origin_rot.Inverse() * unityPose;

            return new UnityEngineDouble.Vector3d(
                map.ecef_origin.x + position.x,
                map.ecef_origin.y + position.z,
                map.ecef_origin.z + position.y
            );

        }

        void Update()
        {
            var p = new UnityEngineDouble.Vector3d(
                hptf_.LocalPosition.x,
                hptf_.LocalPosition.y,
                hptf_.LocalPosition.z
            );

            epsg4978 = ConvertUnityToECEF(p);

            epsg4979 = CustomGeo.GeoConverter.epsg4978_to_epsg4979(epsg4978.x, epsg4978.y, epsg4978.z);
            Debug.Log($"LLA: <a href=\"https://maps.google.com/?q={epsg4979.x},{epsg4979.y}&spn\">{epsg4979.x} {epsg4979.y} {epsg4979.z}</a>");
        }
    }
}