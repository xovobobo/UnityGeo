using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace CustomGeo
{
    public abstract class TileBase : MonoBehaviour
    {
        protected MapBase map_;
        public int x, y, zoom;

        public abstract void GenerateTile(MonoBehaviour parent);

        public void Initialize(int x, int y, int zoom, MonoBehaviour parent)
        {
            this.x = x;
            this.y = y;
            this.zoom = zoom;
            map_ = parent.GetComponent<MapBase>();
            GenerateTile(parent);
        }

        public void assignTexture()
        {
            string url = map_.tilemapUrl.Replace("{z}", zoom.ToString())
                                   .Replace("{x}", x.ToString())
                                   .Replace("{y}", y.ToString());

            StartCoroutine(LoadTileTexture(this.gameObject, url, 3));
        }

        private IEnumerator LoadTileTexture(GameObject tileObject, string url, int maxRetries)
        {
            int attempt = 0;
            while (attempt < maxRetries)
            {
                using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
                {
                    yield return uwr.SendWebRequest();

                    if (uwr.result == UnityWebRequest.Result.Success)
                    {
                        Texture2D tileTexture = DownloadHandlerTexture.GetContent(uwr);
                        Material tileMaterial = new Material(Shader.Find("Standard"))
                        {
                            mainTexture = tileTexture
                        };
                        tileMaterial.SetFloat("_Glossiness", 0.0f);
                        tileObject.GetComponent<Renderer>().material = tileMaterial;
                        yield break;
                    }
                    else
                    {
                        Debug.LogWarning($"Attempt {attempt + 1} failed to load tile texture from {url}: {uwr.error}");
                        attempt++;
                        yield return new WaitForSeconds(1);
                    }
                }
            }

            Debug.LogError($"Failed to load tile texture from {url} after {maxRetries} attempts.");
        }
    }
}
