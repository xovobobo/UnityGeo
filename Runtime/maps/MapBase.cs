using UnityEngine;

namespace CustomGeo
{
    public abstract class MapBase : MonoBehaviour
    {
        public double LatOrigin = 55.75706;
        public double LonOrigin = 48.7572;
        public string tilemapUrl = "https://127.0.0.1:4444/wmts/google_terrain/base_grid/{z}/{x}/{y}.png ";

        [Range(0, 22)]
        public int zoom = 12;
        public int blocks = 1;

        private protected GameObject tiles;

        public abstract void generateBlocks();

        void Start()
        {
            generateBlocks();
        }
    }
}
