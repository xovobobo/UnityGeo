using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CustomGeo
{
    public abstract class MapBase : MonoBehaviour
    {
        private const int InitialTilesPerFrame = 12;

        private readonly object _stateLock = new object();
        protected object StateLock => _stateLock;

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

        public Material tilesMaterial;
        public bool addTilesCollider = false;
        #endregion

        private protected GameObject tiles;
        protected Dictionary<(int, int, int), MonoBehaviour> activeTiles = new Dictionary<(int, int, int), MonoBehaviour>();
        private readonly Dictionary<(int z, int tx), Transform> _tileParents = new();

        private Coroutine _initialGenerateCoroutine;
        private bool _initialGenerateRunning;

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

        private void OnDestroy()
        {
            StopInitialGeneration();
        }

        public void InitMap()
        {
            StopInitialGeneration();

            lock (_stateLock)
            {
                ClearTilesLocked();

                init();
                SetupTilesContainer();

                IsInitialized = true;
            }

            if (generateTiles)
                StartInitialGeneration();
        }

        public void Reinitialize(double? latOrigin = null, double? lonOrigin = null)
        {
            StopInitialGeneration();

            lock (_stateLock)
            {
                if (latOrigin.HasValue) LatOrigin = latOrigin.Value;
                if (lonOrigin.HasValue) LonOrigin = lonOrigin.Value;

                ClearTilesLocked();
                init();
                SetupTilesContainer();
                IsInitialized = true;
            }

            if (generateTiles)
                StartInitialGeneration();
        }


        public void ClearTiles()
        {
            StopInitialGeneration();

            lock (_stateLock)
            {
                ClearTilesLocked();
            }
        }

        private void ClearTilesLocked()
        {
            foreach (var tile in activeTiles.Values)
            {
                if (tile != null) Destroy(tile.gameObject);
            }
            activeTiles.Clear();
            _tileParents.Clear();

            if (tiles != null)
            {
                Destroy(tiles);
                tiles = null;
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

        private void StopInitialGeneration()
        {
            if (_initialGenerateCoroutine != null)
            {
                StopCoroutine(_initialGenerateCoroutine);
                _initialGenerateCoroutine = null;
            }

            _initialGenerateRunning = false;
        }

        private void StartInitialGeneration()
        {
            StopInitialGeneration();
            _initialGenerateRunning = true;
            _initialGenerateCoroutine = StartCoroutine(GenerateBlocksOverFrames());
        }

        /// <summary>
        /// Spawns the initial tile grid across multiple frames to avoid a main-thread hitch
        /// on scene activation (blocks=12 => 625 tiles).
        /// </summary>
        private IEnumerator GenerateBlocksOverFrames()
        {
            int centerX;
            int centerY;
            int z;
            int b;

            lock (_stateLock)
            {
                var tileCenter = new Tile(LatOrigin, LonOrigin, zoom);
                centerX = tileCenter.x;
                centerY = tileCenter.y;
                z = zoom;
                b = blocks;
            }

            int spawnedThisFrame = 0;

            for (int x = -b; x <= b; x++)
            {
                for (int y = -b; y <= b; y++)
                {
                    if (this == null)
                        yield break;

                    SpawnTile(centerX + x, centerY + y, z);
                    spawnedThisFrame++;

                    if (spawnedThisFrame >= InitialTilesPerFrame)
                    {
                        spawnedThisFrame = 0;
                        yield return null;
                    }
                }
            }

            _initialGenerateRunning = false;
            _initialGenerateCoroutine = null;
        }

        protected virtual void Update()
        {
            // Wait until the initial grid is finished so CleanupOldTiles does not
            // destroy tiles that are still being spawned around LatOrigin.
            if (!IsInitialized || _initialGenerateRunning || !udpateDynamicTiles || looking_tf == null)
                return;

            lock (_stateLock)
            {
                if (IsInitialized && !_initialGenerateRunning)
                    UpdateDynamicTilesLogicLocked();
            }
        }

        private void UpdateDynamicTilesLogicLocked()
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
                        if (int.TryParse(zFolder.name, out int z) && int.TryParse(xFolder.name, out int tx))
                            _tileParents.Remove((z, tx));
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

        protected Transform GetTileParent(int z, int tx)
        {
            var key = (z, tx);
            if (_tileParents.TryGetValue(key, out var cached) && cached != null)
                return cached;

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

            _tileParents[key] = xFolder;
            return xFolder;
        }

    }
}
