using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace CustomGeo
{
    public abstract class MapBase : MonoBehaviour
    {
        #region Inspector
        [Header("Initialization")]
        public bool autoInit = true;

        [Header("Origin Settings")]
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
        public bool cacheGameobjects = false;
        #endregion

        private protected GameObject tiles;
        protected Dictionary<(int, int, int), MonoBehaviour> activeTiles = new Dictionary<(int, int, int), MonoBehaviour>();

        public bool IsInitialized { get; private set; }

        protected abstract void init();
        public abstract void SpawnTile(int x, int y, int z);
        public abstract Vector3 GetWorldPositionFromLLA(UnityEngineDouble.Vector3d lla);
        public abstract UnityEngineDouble.Vector3d GetLLAAtPosition(Vector3 worldPos);

        protected virtual void Awake()
        {
            if (enabled && autoInit)
            {
                InitMap();
            }
        }

        public void InitMap()
        {
            ClearTiles();

            init();
            SetupTilesContainer();

            if (generateTiles)
                generateBlocksStatic();

            IsInitialized = true;
        }


        public void ClearTiles()
        {
            foreach (var tile in activeTiles.Values)
            {
                if (tile != null) Destroy(tile.gameObject);
            }
            activeTiles.Clear();

            if (tiles != null)
            {
                foreach (Transform child in tiles.transform)
                    Destroy(child.gameObject);
            }

            IsInitialized = false;
        }

        private void SetupTilesContainer()
        {
            if (tiles == null)
            {
                tiles = new GameObject("tiles");
                tiles.transform.parent = transform;
                tiles.transform.localPosition = Vector3.zero;
                tiles.layer = tileObjectsLayer;
            }
        }

        protected virtual void Update()
        {
            if (IsInitialized && udpateDynamicTiles && looking_tf != null)
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

            if (!cacheGameobjects)
                CleanupOldTiles(frameVisibleKeys);
        }

        private void CleanupOldTiles(HashSet<(int, int, int)> visibleKeys)
        {
            var keysToRemove = activeTiles.Keys.Where(k => !visibleKeys.Contains(k)).ToList();

            foreach (var key in keysToRemove)
            {
                if (activeTiles.TryGetValue(key, out var tileScript) && tileScript != null)
                {
                    Destroy(tileScript.gameObject);
                }
                activeTiles.Remove(key);
            }

            if (tiles == null) return;

            foreach (Transform zFolder in tiles.transform)
            {
                for (int i = zFolder.childCount - 1; i >= 0; i--)
                {
                    Transform xFolder = zFolder.GetChild(i);
                    if (xFolder.childCount == 0 || IsAllChildrenDestroying(xFolder))
                    {
                        Destroy(xFolder.gameObject);
                    }
                }

                if (zFolder.childCount == 0 || IsAllChildrenDestroying(zFolder))
                {
                    Destroy(zFolder.gameObject);
                }
            }
        }

        private bool IsAllChildrenDestroying(Transform parent)
        {
            foreach (Transform child in parent)
            {

                if (child.childCount > 0) return false;
                bool isAliveTile = activeTiles.Values.Any(v => v != null && v.transform == child);
                if (isAliveTile) return false;
            }
            return true;
        }



        private void generateBlocksStatic()
        {
            Tile tile_center = new Tile(LatOrigin, LonOrigin, zoom);
            for (int x = -blocks; x <= blocks; x++)
            {
                for (int y = -blocks; y <= blocks; y++)
                {
                    SpawnTile(tile_center.x + x, tile_center.y + y, zoom);
                }
            }
        }

        protected Transform GetTileParent(int z, int tx)
        {
            string zName = z.ToString();
            Transform zoomFolder = tiles.transform.Find(zName);
            if (zoomFolder == null)
            {
                zoomFolder = new GameObject(zName).transform;
                zoomFolder.SetParent(tiles.transform, false);
            }

            string xName = tx.ToString();
            Transform xFolder = zoomFolder.Find(xName);
            if (xFolder == null)
            {
                xFolder = new GameObject(xName).transform;
                xFolder.SetParent(zoomFolder, false);
            }

            return xFolder;
        }

    }
}
