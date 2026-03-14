using UnityEngine;

namespace CustomGeo
{
    public abstract class MapBase : MonoBehaviour
    {
        #region Inspector
        public double LatOrigin = 55.75706;
        public double LonOrigin = 48.7572;

        [Header("Tile")]
        public bool generateTiles = false;
        public bool store = false;
        public int tileObjectsLayer = 0;
        public string tilemapUrl = "http://server.arcgisonline.com/arcgis/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}";
        public string cacheFolder = "";
        [Range(0, 22)]
        public int zoom = 12;
        public int blocks = 4;

        #endregion

        private protected GameObject tiles;

        protected abstract void init();
        public abstract void generateBlocks(int layer);

        void Awake()
        {
            init();
            if (generateTiles)
                generateBlocks(tileObjectsLayer);
        }
    }
}
