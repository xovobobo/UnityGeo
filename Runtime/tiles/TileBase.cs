using UnityEngine;
using UnityEngine.Networking;

using System;
using System.IO;
using System.Collections;
using System.Threading.Tasks;

namespace CustomGeo
{
    public abstract class TileBase : MonoBehaviour
    {
        private const int MaxTextureOpsPerFrame = 2;
        private const int MaxConcurrentDownloads = 6;

        private static int _textureOpsFrame = -1;
        private static int _textureOpsThisFrame;
        private static int _activeDownloads;

        protected int x, y, zoom;
        protected abstract void GenerateTile(MonoBehaviour parent);

        private MapBase map;
        private string _url = null;
        private string _filePath = null;
        private string _cachePath = null;
        private bool _saveCache = false;

        private Texture2D _tileTexture;
        private Material _tileMaterial;
        private Mesh _tileMesh;
        private Coroutine _loadCoroutine;
        private UnityWebRequest _activeRequest;
        private bool _holdsDownloadSlot;

        public void Initialize(int x, int y, int zoom, MonoBehaviour parent)
        {
            this.x = x;
            this.y = y;
            this.zoom = zoom;
            map = parent.GetComponent<MapBase>();
            if (!map)
                return;

            _url = map.tilemapUrl.Replace("{z}", zoom.ToString()).Replace("{x}", x.ToString()).Replace("{y}", y.ToString());

            try
            {
                var cacheFolder = map.cacheFolder;
                if (!Directory.Exists(map.cacheFolder))
                    cacheFolder = $"{Application.dataPath}/{map.cacheFolder}";

                if (Directory.Exists(cacheFolder))
                {
                    _cachePath = cacheFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.AltDirectorySeparatorChar;
                    _filePath = _cachePath + $"{zoom}/{x}/{y}.png";
                    _saveCache = map.store;
                }
            }
            catch { }

            GenerateTile(parent);
        }

        protected void CreateMesh(Vector3[] vertices)
        {
            MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();

            _tileMesh = new Mesh { name = $"Tile_{zoom}_{x}_{y}" };
            meshFilter.sharedMesh = _tileMesh;

            int[] triangles = new int[6]
            {
                0, 1, 2,
                2, 3, 0
            };

            Vector2[] uv = new Vector2[4]
            {
                new (0, 1), new (1, 1),
                new (1, 0), new (0, 0)
            };

            _tileMesh.vertices = vertices;
            _tileMesh.triangles = triangles;
            _tileMesh.uv = uv;
            _tileMesh.RecalculateNormals();

            if (map.addTilesCollider)
            {
                MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = _tileMesh;
            }

            // Assign a temporary shared material so the renderer is valid;
            // the owned instance is created once the texture finishes loading.
            meshRenderer.sharedMaterial = map.tilesMaterial;

            _loadCoroutine = StartCoroutine(LoadTileTexture(meshRenderer, 3));
        }

        private Material CreateTileMaterial(Texture2D texture)
        {
            Material mat = new Material(map.tilesMaterial)
            {
                mainTexture = texture
            };
            return mat;
        }

        private void AssignTileTexture(MeshRenderer renderer, Texture2D texture)
        {
            if (renderer == null || texture == null)
            {
                if (texture != null)
                    Destroy(texture);
                return;
            }

            ReleaseOwnedResources(releaseMesh: false);

            _tileTexture = texture;
            _tileMaterial = CreateTileMaterial(texture);
            renderer.sharedMaterial = _tileMaterial;
        }

        private static IEnumerator WaitForTextureOpSlot()
        {
            while (true)
            {
                int frame = Time.frameCount;
                if (_textureOpsFrame != frame)
                {
                    _textureOpsFrame = frame;
                    _textureOpsThisFrame = 0;
                }

                if (_textureOpsThisFrame < MaxTextureOpsPerFrame)
                {
                    _textureOpsThisFrame++;
                    yield break;
                }

                yield return null;
            }
        }

        private IEnumerator AcquireDownloadSlot()
        {
            while (_activeDownloads >= MaxConcurrentDownloads)
                yield return null;

            _activeDownloads++;
            _holdsDownloadSlot = true;
        }

        private void ReleaseDownloadSlot()
        {
            if (!_holdsDownloadSlot)
                return;

            _holdsDownloadSlot = false;
            _activeDownloads = Math.Max(0, _activeDownloads - 1);
        }

        private IEnumerator LoadTileTexture(MeshRenderer renderer, int maxRetries)
        {
            // Spread work across frames: Update may spawn hundreds of tiles at once.
            yield return null;

            if (this == null || renderer == null)
                yield break;

            // Prefer disk cache. Existence check runs off the main thread.
            if (!string.IsNullOrEmpty(_filePath))
            {
                bool fileExists = false;
                yield return RunOffMainThread(() => fileExists = File.Exists(_filePath));

                if (this == null || renderer == null)
                    yield break;

                if (fileExists)
                {
                    Texture2D tileTexture = new(2, 2);
                    tileTexture.wrapMode = TextureWrapMode.Clamp;
                    yield return LoadTextureFromFileAsync(_filePath, tileTexture);

                    if (this == null || renderer == null)
                    {
                        Destroy(tileTexture);
                        yield break;
                    }

                    AssignTileTexture(renderer, tileTexture);
                    yield break;
                }
            }

            // get texture from url
            int attempt = 0;
            while (attempt < maxRetries)
            {
                if (this == null)
                    yield break;

                yield return AcquireDownloadSlot();
                if (this == null)
                {
                    ReleaseDownloadSlot();
                    yield break;
                }

                _activeRequest = UnityWebRequestTexture.GetTexture(_url);
                yield return _activeRequest.SendWebRequest();

                // StopCoroutine skips iterator finally/using — request lives in _activeRequest
                // until we clear it here, or OnDestroy calls DisposeActiveRequest().
                var uwr = _activeRequest;
                _activeRequest = null;
                ReleaseDownloadSlot();

                if (uwr == null)
                    yield break;

                Texture2D tileTexture = null;
                string error = null;
                bool success = false;

                // No yield inside try/finally: Dispose must run even if coroutine is stopped later.
                try
                {
                    if (this != null && renderer != null &&
                        uwr.result == UnityWebRequest.Result.Success)
                    {
                        tileTexture = DownloadHandlerTexture.GetContent(uwr);
                        success = tileTexture != null;
                    }
                    else if (uwr.result != UnityWebRequest.Result.Success)
                    {
                        error = uwr.error;
                    }
                }
                finally
                {
                    uwr.Dispose();
                }

                if (this == null || renderer == null)
                {
                    if (tileTexture != null)
                        Destroy(tileTexture);
                    yield break;
                }

                if (success)
                {
                    tileTexture.wrapMode = TextureWrapMode.Clamp;
                    AssignTileTexture(renderer, tileTexture);

                    if (_saveCache && !string.IsNullOrEmpty(_filePath))
                        yield return SaveTextureToFileAsync(_filePath, tileTexture);

                    yield break;
                }

                Debug.LogWarning($"Attempt {attempt + 1} failed to load tile texture from {_url}: {error}");
                attempt++;
                yield return new WaitForSeconds(1);
            }

            Debug.LogError($"Failed to load tile texture from {_url} after {maxRetries} attempts.");
        }

        private static IEnumerator RunOffMainThread(Action action)
        {
            Exception error = null;
            var task = Task.Run(() =>
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    error = e;
                }
            });

            while (!task.IsCompleted)
                yield return null;

            if (error != null)
                Debug.LogWarning($"Background tile IO failed: {error.Message}");
        }

        private IEnumerator LoadTextureFromFileAsync(string path, Texture2D texture)
        {
            byte[] fileData = null;
            Exception error = null;

            var task = Task.Run(() =>
            {
                try
                {
                    fileData = File.ReadAllBytes(path);
                }
                catch (Exception e)
                {
                    error = e;
                }
            });

            while (!task.IsCompleted)
                yield return null;

            if (this == null)
                yield break;

            if (error != null || fileData == null || fileData.Length == 0)
            {
                Debug.LogWarning($"Can't load texture from file '{path}'.");
                yield break;
            }

            // PNG decode / GPU upload must happen on the main thread — budget it per frame.
            yield return WaitForTextureOpSlot();
            if (this == null)
                yield break;

            if (!texture.LoadImage(fileData))
                Debug.LogWarning($"Can't decode texture from file '{path}'.");
        }

        private IEnumerator SaveTextureToFileAsync(string path, Texture2D texture)
        {
            if (texture == null || string.IsNullOrEmpty(path))
                yield break;

            yield return WaitForTextureOpSlot();
            if (this == null || texture == null)
                yield break;

            byte[] encoded;
            try
            {
                encoded = texture.EncodeToPNG();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Can't encode texture for '{path}': {e.Message}");
                yield break;
            }

            if (encoded == null || encoded.Length == 0)
                yield break;

            yield return RunOffMainThread(() =>
            {
                var dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllBytes(path, encoded);
            });
        }

        private void DisposeActiveRequest()
        {
            if (_activeRequest == null)
                return;

            // Abort in-flight download so native buffers are released promptly.
            if (!_activeRequest.isDone)
                _activeRequest.Abort();

            _activeRequest.Dispose();
            _activeRequest = null;
        }

        private void ReleaseOwnedResources(bool releaseMesh)
        {
            if (_tileMaterial != null)
            {
                Destroy(_tileMaterial);
                _tileMaterial = null;
            }

            if (_tileTexture != null)
            {
                Destroy(_tileTexture);
                _tileTexture = null;
            }

            if (releaseMesh && _tileMesh != null)
            {
                Destroy(_tileMesh);
                _tileMesh = null;
            }
        }

        private void OnDestroy()
        {
            if (_loadCoroutine != null)
            {
                StopCoroutine(_loadCoroutine);
                _loadCoroutine = null;
            }

            // Must dispose explicitly: StopCoroutine does not run iterator finally/using.
            DisposeActiveRequest();
            ReleaseDownloadSlot();
            ReleaseOwnedResources(releaseMesh: true);
        }
    }
}
