using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace CustomGeo
{
    public abstract class MapBase : MonoBehaviour
    {
        #region Inspector
        public double LatOrigin = 55.75706;
        public double LonOrigin = 48.7572;

        [Header("Tile Settings")]
        public bool generateTiles = false;
        public bool udpateDynamicTiles = false;
        public Transform looking_tf;
        public int tileObjectsLayer = 0;
        public string tilemapUrl = "http://server.arcgisonline.com/arcgis/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}";
        [Range(0, 22)]
        public int zoom = 12;
        public int blocks = 4;
        public string cacheFolder = "";
        public bool store = false;
        #endregion

        private protected GameObject tiles;
        protected Dictionary<(int, int, int), MonoBehaviour> activeTiles = new Dictionary<(int, int, int), MonoBehaviour>();

        protected abstract void init();
        public abstract void SpawnTile(int x, int y, int z);
        public abstract UnityEngineDouble.Vector3d GetLLAAtPosition(Vector3 worldPos);

        void Awake()
        {
            init();
            if (tiles == null)
            {
                tiles = new GameObject("tiles");
                tiles.transform.parent = transform;
            }
            if (generateTiles)
                generateBlocksStatic();
        }

        protected virtual void Update()
        {
            if (udpateDynamicTiles && looking_tf != null)
                UpdateDynamicTilesLogic();
        }

        private void UpdateDynamicTilesLogic()
        {
            var lla = GetLLAAtPosition(looking_tf.position);
            Tile tile_center = new Tile(lat: lla.x, lon: lla.y, zoom: zoom);

            int maxTiles = 1 << zoom;
            HashSet<(int, int, int)> frameVisibleKeys = new HashSet<(int, int, int)>();

            for (int x = -blocks; x <= blocks; x++)
            {
                for (int y = -blocks; y <= blocks; y++)
                {
                    int tx = ((tile_center.x + x) % maxTiles + maxTiles) % maxTiles;
                    int ty = ((tile_center.y + y) % maxTiles + maxTiles) % maxTiles;
                    var key = (zoom, tx, ty);
                    frameVisibleKeys.Add(key);

                    if (!activeTiles.ContainsKey(key))
                        SpawnTile(tx, ty, zoom);
                }
            }
            CleanupOldTiles(frameVisibleKeys);
        }

        private void CleanupOldTiles(HashSet<(int, int, int)> visibleKeys)
        {
            var keysToRemove = activeTiles.Keys.Where(k => !visibleKeys.Contains(k)).ToList();
            foreach (var key in keysToRemove)
            {
                Destroy(activeTiles[key].gameObject);
                activeTiles.Remove(key);
            }
        }

        private void generateBlocksStatic()
        {
            Tile tile_center = new Tile(LatOrigin, LonOrigin, zoom);
            for (int x = -blocks; x <= blocks; x++)
                for (int y = -blocks; y <= blocks; y++)
                    SpawnTile(tile_center.x + x, tile_center.y + y, zoom);
        }
    }
}
