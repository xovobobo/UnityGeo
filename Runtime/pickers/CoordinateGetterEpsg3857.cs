using UnityEngine;

namespace CustomGeo
{
    public class CoordinateGetterEpsg3857 : CoordinateGetterBase
    {
        public override UnityEngineDouble.Vector3d GetLLA()
        {
            var map3857 = map as MapEpsg3857;
            if (map3857 == null) return UnityEngineDouble.Vector3d.zero;

            return map3857.GetLLAAtPosition(this.transform.position);
        }
    }
}
