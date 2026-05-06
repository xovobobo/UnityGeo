using UnityEngine;

namespace CustomGeo
{
    public abstract class CoordinateGetterBase : MonoBehaviour
    {
        [Header("Settings")]
        public MapBase map;
        public bool autoUpdate = false;

        [Header("Common Result (LLA)")]
        public UnityEngineDouble.Vector3d lla;

        protected virtual void Update()
        {
            if (autoUpdate && map != null)
            {
                lla = GetLLA();
            }
        }

        public abstract UnityEngineDouble.Vector3d GetLLA();
    }
}
