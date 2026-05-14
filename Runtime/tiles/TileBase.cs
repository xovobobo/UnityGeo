using UnityEngine;
using UnityEngine.Networking;

using System.IO;
using System.Collections;

namespace CustomGeo
{
    public abstract class TileBase : MonoBehaviour
    {
        protected int x, y, zoom;
        protected abstract void GenerateTile(MonoBehaviour parent);

        private MapBase map;
        private string _url = null;
        private string _filePath = null;
        private string _cachePath = null;
        private bool _saveCache = false;

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
            meshRenderer.material = new Material(map.tilesMaterial);
            Mesh mesh = new();
            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;

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


            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uv;
            mesh.RecalculateNormals();

            if (map.addTilesCollider)
            {
                MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = mesh;
            }

            StartCoroutine(LoadTileTexture(gameObject, 3));
        }

        private Material CreateTileMaterial(Texture2D texture)
        {
            Material mat = new Material(map.tilesMaterial)
            {
                mainTexture = texture
            };
            return mat;
        }

        private IEnumerator LoadTileTexture(GameObject tileObject, int maxRetries)
        {
            // get texture from file
            if (File.Exists(_filePath))
            {
                Texture2D tileTexture = new(2, 2);
                tileTexture.wrapMode = TextureWrapMode.Clamp;
                yield return LoadTextureFromFile(_filePath, tileTexture);
                tileObject.GetComponent<Renderer>().material = CreateTileMaterial(tileTexture);
                yield break;
            }

            // get texture from url
            int attempt = 0;
            while (attempt < maxRetries)
            {
                using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(_url))
                {
                    yield return uwr.SendWebRequest();

                    if (uwr.result == UnityWebRequest.Result.Success)
                    {
                        Texture2D tileTexture = DownloadHandlerTexture.GetContent(uwr);
                        if (tileTexture)
                        {
                            tileTexture.wrapMode = TextureWrapMode.Clamp;
                            tileObject.GetComponent<Renderer>().material = CreateTileMaterial(tileTexture);

                            if (_saveCache)
                            {
                                yield return SaveTextureToFile(_filePath, tileTexture);
                            }
                        }
                        yield break;
                    }
                    else
                    {
                        Debug.LogWarning($"Attempt {attempt + 1} failed to load tile texture from {_url}: {uwr.error}");
                        attempt++;
                        yield return new WaitForSeconds(1);
                    }
                }
            }

            Debug.LogError($"Failed to load tile texture from {_url} after {maxRetries} attempts.");
        }

        private IEnumerator LoadTextureFromFile(string path, Texture2D texture)
        {
            bool success = false;
            try
            {
                byte[] fileData = File.ReadAllBytes(path);
                success = texture.LoadImage(fileData);
            }
            catch { }

            if (!success)
                Debug.LogWarning($"Can't load texture from file '{path}'.");

            yield return null;
        }

        private IEnumerator SaveTextureToFile(string path, Texture2D texture)
        {
            try
            {
                var dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            catch
            {
                Debug.LogWarning($"Can't save texture to file '{path}'.");
            }

            yield return null;
        }
    }
}
