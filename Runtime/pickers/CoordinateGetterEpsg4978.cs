using UnityEngine;

namespace CustomGeo
{
    public class CoordinateGetter4978 : CoordinateGetterBase
    {
        public override UnityEngineDouble.Vector3d GetLLA()
        {
            var map4978 = map as MapEpsg4978;
            if (map4978 == null) return UnityEngineDouble.Vector3d.zero;
            return map4978.GetLLAAtPosition(this.transform.position);
        }

        public UnityEngineDouble.Vector3d GetLLAFromUnityPos(Vector3 pos)
        {
            var map4978 = map as MapEpsg4978;
            return map4978 != null ? map4978.GetLLAAtPosition(pos) : UnityEngineDouble.Vector3d.zero;
        }
    }
}
