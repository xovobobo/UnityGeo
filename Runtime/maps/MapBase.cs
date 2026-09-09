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

        [Tooltip("Blocks: square grid around looking_tf. OrthoCamera: fill the camera FOV.")]
        public TileGenerationMode tileGenerationMode = TileGenerationMode.Blocks;

        [Tooltip("Camera used in OrthoCamera mode. If empty, uses looking_tf (or its child/parent) then Camera.main.")]
        public Camera tileCamera;

        [Range(0, 22)]
        public int zoom = 12;

        [Tooltip("Coarsest zoom. Used when even this zoom needs more tiles than maxVisibleTiles, the grid is clamped.")]
        [Range(0, 22)]
        public int minZoom = 12;

        [Tooltip("Finest zoom. Used when the FOV fits within maxVisibleTiles.")]
        [Range(0, 22)]
        public int maxZoom = 19;

        [Tooltip("Extra FOV margin so tiles do not pop in at the screen edge.")]
        [Range(0f, 1f)]
        public float viewPadding = 0.15f;

        [Tooltip("Tile budget. If zoom 19 needs more tiles than this to fill the FOV, drop to 18, then 17, ...")]
        public int maxVisibleTiles = 512;

        public int blocks = 4;
        public string cacheFolder = "";
        public bool store = false;
        public bool cacheGameobjects = false;

        public Material tilesMaterial;
        public bool addTilesCollider = false;

        [Header("Debug")]
        [Tooltip("Show live tile count and active zoom on screen.")]
        public bool debugTiles = false;
        #endregion

        private protected GameObject tiles;
        protected Dictionary<(int, int, int), MonoBehaviour> activeTiles = new Dictionary<(int, int, int), MonoBehaviour>();
        private readonly Dictionary<(int z, int tx), Transform> _tileParents = new();
        private readonly HashSet<(int, int, int)> _visibleKeys = new HashSet<(int, int, int)>();
        private readonly List<Vector3> _frustumPoints = new List<Vector3>();

        private Coroutine _initialGenerateCoroutine;
        private bool _initialGenerateRunning;
        private bool _loggedMissingCamera;
        private int _orthoLodZoom = -1;
        private readonly Dictionary<int, int> _debugZoomCounts = new Dictionary<int, int>();
        private readonly List<int> _debugZoomKeys = new List<int>();
        private GUIStyle _debugLabelStyle;

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
            _orthoLodZoom = -1;

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
            _initialGenerateCoroutine = tileGenerationMode == TileGenerationMode.OrthoCamera
                ? StartCoroutine(GenerateOrthoCameraOverFrames())
                : StartCoroutine(GenerateBlocksOverFrames());
        }

        protected virtual void OnValidate()
        {
            zoom = Mathf.Clamp(zoom, 0, 22);
            minZoom = Mathf.Clamp(minZoom, 0, 22);
            maxZoom = Mathf.Clamp(maxZoom, 0, 22);
            if (minZoom > maxZoom)
            {
                int tmp = minZoom;
                minZoom = maxZoom;
                maxZoom = tmp;
            }

            viewPadding = Mathf.Clamp(viewPadding, 0f, 1f);
            maxVisibleTiles = Mathf.Max(maxVisibleTiles, 1);
            blocks = Mathf.Max(blocks, 0);
        }

        public Camera ResolveTileCamera()
        {
            return OrthoCameraTileGenerator.ResolveCamera(tileCamera, looking_tf);
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

        private IEnumerator GenerateOrthoCameraOverFrames()
        {
            Camera cam;
            int minZ;
            int maxZ;
            float pad;
            int cap;

            lock (_stateLock)
            {
                cam = ResolveTileCamera();
                minZ = minZoom;
                maxZ = maxZoom;
                pad = viewPadding;
                cap = maxVisibleTiles;
            }

            if (cam == null)
            {
                if (!_loggedMissingCamera)
                {
                    Debug.LogWarning(
                        $"{name}: OrthoCamera tile mode needs tileCamera (or a Camera on looking_tf).");
                    _loggedMissingCamera = true;
                }

                _initialGenerateRunning = false;
                _initialGenerateCoroutine = null;
                yield break;
            }

            _loggedMissingCamera = false;
            _visibleKeys.Clear();
            _orthoLodZoom = OrthoCameraTileGenerator.CollectVisibleTiles(
                this, cam, minZ, maxZ, pad, cap, _orthoLodZoom, _frustumPoints, _visibleKeys);

            var keys = _visibleKeys.ToList();
            int spawnedThisFrame = 0;
            foreach (var key in keys)
            {
                if (this == null)
                    yield break;

                SpawnTile(key.Item2, key.Item3, key.Item1);
                spawnedThisFrame++;

                if (spawnedThisFrame >= InitialTilesPerFrame)
                {
                    spawnedThisFrame = 0;
                    yield return null;
                }
            }

            _initialGenerateRunning = false;
            _initialGenerateCoroutine = null;
        }

        protected virtual void Update()
        {
            // Wait until the initial grid is finished so CleanupOldTiles does not
            // destroy tiles that are still being spawned around LatOrigin.
            if (!IsInitialized || _initialGenerateRunning || !udpateDynamicTiles)
                return;

            lock (_stateLock)
            {
                if (!IsInitialized || _initialGenerateRunning)
                    return;

                if (tileGenerationMode == TileGenerationMode.OrthoCamera)
                    UpdateOrthoCameraTilesLocked();
                else if (looking_tf != null)
                    UpdateDynamicTilesLogicLocked();
            }
        }

        private void UpdateOrthoCameraTilesLocked()
        {
            var cam = ResolveTileCamera();
            if (cam == null)
            {
                if (!_loggedMissingCamera)
                {
                    Debug.LogWarning(
                        $"{name}: OrthoCamera tile mode needs tileCamera (or a Camera on looking_tf).");
                    _loggedMissingCamera = true;
                }

                return;
            }

            _loggedMissingCamera = false;
            _visibleKeys.Clear();
            _orthoLodZoom = OrthoCameraTileGenerator.CollectVisibleTiles(
                this, cam, minZoom, maxZoom, viewPadding, maxVisibleTiles, _orthoLodZoom,
                _frustumPoints, _visibleKeys);

            foreach (var key in _visibleKeys)
            {
                if (!activeTiles.ContainsKey(key))
                    SpawnTile(key.Item2, key.Item3, key.Item1);
            }

            if (!cacheGameobjects)
                CleanupOldTiles(_visibleKeys);
        }

        private void UpdateDynamicTilesLogicLocked()
        {
            var lla = GetLLAAtPosition(looking_tf.position);
            Tile tile_center = new Tile(lat: lla.x, lon: lla.y, zoom: zoom);

            int maxTiles = 1 << zoom;
            _visibleKeys.Clear();

            for (int x = -blocks; x <= blocks; x++)
            {
                for (int y = -blocks; y <= blocks; y++)
                {
                    int tx = ((tile_center.x + x) % maxTiles + maxTiles) % maxTiles;
                    int ty = ((tile_center.y + y) % maxTiles + maxTiles) % maxTiles;
                    var key = (zoom, tx, ty);
                    _visibleKeys.Add(key);

                    if (!activeTiles.ContainsKey(key))
                        SpawnTile(tx, ty, zoom);
                }
            }

            if (!cacheGameobjects)
                CleanupOldTiles(_visibleKeys);
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

        private void OnGUI()
        {
            if (!debugTiles)
                return;

            int total = 0;
            int minActiveZoom = int.MaxValue;
            int maxActiveZoom = int.MinValue;
            _debugZoomCounts.Clear();

            lock (_stateLock)
            {
                foreach (var pair in activeTiles)
                {
                    if (pair.Value == null)
                        continue;

                    total++;
                    int z = pair.Key.Item1;
                    _debugZoomCounts.TryGetValue(z, out int count);
                    _debugZoomCounts[z] = count + 1;
                    if (z < minActiveZoom) minActiveZoom = z;
                    if (z > maxActiveZoom) maxActiveZoom = z;
                }
            }

            string zoomLine;
            if (total == 0 || _debugZoomCounts.Count == 0)
            {
                zoomLine = tileGenerationMode == TileGenerationMode.OrthoCamera
                    ? $"Zoom: —  (range {minZoom}-{maxZoom})"
                    : $"Zoom: —  (set {zoom})";
            }
            else if (_debugZoomCounts.Count == 1)
            {
                zoomLine = $"Zoom: {minActiveZoom}";
            }
            else
            {
                _debugZoomKeys.Clear();
                foreach (var z in _debugZoomCounts.Keys)
                    _debugZoomKeys.Add(z);
                _debugZoomKeys.Sort();

                string breakdown = "";
                for (int i = 0; i < _debugZoomKeys.Count; i++)
                {
                    int z = _debugZoomKeys[i];
                    if (i > 0)
                        breakdown += "  ";
                    breakdown += $"{z}:{_debugZoomCounts[z]}";
                }

                zoomLine = $"Zoom: {minActiveZoom}-{maxActiveZoom}  ({breakdown})";
            }

            string mode = tileGenerationMode == TileGenerationMode.OrthoCamera ? "OrthoCamera" : "Blocks";
            string text = $"Tiles: {total}\n{zoomLine}\nMode: {mode}";

            var style = GetDebugLabelStyle();
            var rect = new Rect(12f, 12f, 560f, 110f);
            DrawOutlinedLabel(rect, text, style);
        }

        private GUIStyle GetDebugLabelStyle()
        {
            if (_debugLabelStyle != null)
                return _debugLabelStyle;

            _debugLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft
            };
            _debugLabelStyle.normal.textColor = Color.white;
            return _debugLabelStyle;
        }

        private static void DrawOutlinedLabel(Rect rect, string text, GUIStyle style)
        {
            Color color = style.normal.textColor;
            style.normal.textColor = Color.black;
            GUI.Label(new Rect(rect.x - 1f, rect.y, rect.width, rect.height), text, style);
            GUI.Label(new Rect(rect.x + 1f, rect.y, rect.width, rect.height), text, style);
            GUI.Label(new Rect(rect.x, rect.y - 1f, rect.width, rect.height), text, style);
            GUI.Label(new Rect(rect.x, rect.y + 1f, rect.width, rect.height), text, style);
            style.normal.textColor = color;
            GUI.Label(rect, text, style);
        }

    }
}
