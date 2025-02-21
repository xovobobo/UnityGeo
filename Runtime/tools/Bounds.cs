namespace CustomGeo
{
    public class GeoBounds2
    {
        public UnityEngineDouble.Vector2d Topleft { get; set; }
        public UnityEngineDouble.Vector2d Topright { get; set; }
        public UnityEngineDouble.Vector2d Bottom_right { get; set; }
        public UnityEngineDouble.Vector2d Bottom_left { get; set; }
        public GeoBounds2(UnityEngineDouble.Vector2d topleft, UnityEngineDouble.Vector2d topright, UnityEngineDouble.Vector2d bottom_right, UnityEngineDouble.Vector2d bottom_left)
        {
            Topleft = topleft;
            Topright = topright;
            Bottom_right = bottom_right;
            Bottom_left = bottom_left;
        }
    }

    public class GeoBounds3
    {
        public UnityEngineDouble.Vector3d Topleft { get; set; }
        public UnityEngineDouble.Vector3d Topright { get; set; }
        public UnityEngineDouble.Vector3d Bottom_right { get; set; }
        public UnityEngineDouble.Vector3d Bottom_left { get; set; }
        public GeoBounds3(UnityEngineDouble.Vector3d topleft, UnityEngineDouble.Vector3d topright, UnityEngineDouble.Vector3d bottom_right, UnityEngineDouble.Vector3d bottom_left)
        {
            Topleft = topleft;
            Topright = topright;
            Bottom_right = bottom_right;
            Bottom_left = bottom_left;
        }
    }
}
