using System;
using System.Collections.Generic;
using UnityEngine;

namespace CustomGeo
{
        /// <summary>
        /// Fills the camera FOV with a single zoom. Prefers a finer zoom while it fits
        /// the tile budget; drops coarser when it does not. Sticky zoom + hysteresis
        /// prevent flickering between e.g. 19 (12 tiles) and 18 (6 tiles).
        /// </summary>
    public static class OrthoCameraTileGenerator
    {
        private static readonly Vector2[] ViewportCorners =
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f)
        };

        public static Camera ResolveCamera(Camera tileCamera, Transform lookingTf)
        {
            if (tileCamera != null)
                return tileCamera;

            if (lookingTf != null)
            {
                var cam = lookingTf.GetComponent<Camera>();
                if (cam != null)
                    return cam;

                cam = lookingTf.GetComponentInChildren<Camera>();
                if (cam != null)
                    return cam;

                cam = lookingTf.GetComponentInParent<Camera>();
                if (cam != null)
                    return cam;
            }

            return Camera.main;
        }

        public static int CollectVisibleTiles(
            MapBase map,
            Camera camera,
            int minZoom,
            int maxZoom,
            float viewPadding,
            int maxTiles,
            int currentZoom,
            List<Vector3> frustumScratch,
            HashSet<(int z, int x, int y)> output)
        {
            output.Clear();
            if (map == null || camera == null)
                return currentZoom;

            minZoom = Mathf.Clamp(minZoom, 0, 22);
            maxZoom = Mathf.Clamp(maxZoom, 0, 22);
            if (minZoom > maxZoom)
            {
                int tmp = minZoom;
                minZoom = maxZoom;
                maxZoom = tmp;
            }

            viewPadding = Mathf.Max(viewPadding, 0f);
            maxTiles = Mathf.Max(maxTiles, 1);

            CollectFrustumGroundPoints(map, camera, viewPadding, frustumScratch);
            if (frustumScratch.Count == 0)
                return currentZoom;

            int chosenZoom = PickZoom(map, frustumScratch, minZoom, maxZoom, maxTiles, currentZoom);
            if (!TryGetTileRange(map, frustumScratch, chosenZoom, out int minTx, out int maxTx, out int minTy, out int maxTy))
                return chosenZoom;

            if (TileCount(minTx, maxTx, minTy, maxTy) > maxTiles)
                ClampTileRange(maxTiles, ref minTx, ref maxTx, ref minTy, ref maxTy);

            for (int x = minTx; x <= maxTx; x++)
            {
                for (int y = minTy; y <= maxTy; y++)
                {
                    if (output.Count >= maxTiles)
                        return chosenZoom;

                    output.Add((chosenZoom, Tile.Wrap(x, chosenZoom), Tile.Wrap(y, chosenZoom)));
                }
            }

            return chosenZoom;
        }

        private static int PickZoom(
            MapBase map,
            List<Vector3> groundPoints,
            int minZoom,
            int maxZoom,
            int maxTiles,
            int currentZoom)
        {
            int zoom = currentZoom;
            if (zoom < minZoom || zoom > maxZoom)
                zoom = FinestFittingZoom(map, groundPoints, minZoom, maxZoom, maxTiles);

            while (zoom > minZoom)
            {
                if (!TryGetTileRange(map, groundPoints, zoom, out int minTx, out int maxTx, out int minTy, out int maxTy))
                    break;

                if (TileCount(minTx, maxTx, minTy, maxTy) <= maxTiles)
                    break;

                zoom--;
            }

            int upLimit = FinerZoomThreshold(maxTiles);
            while (zoom < maxZoom)
            {
                if (!TryGetTileRange(map, groundPoints, zoom + 1, out int minTx, out int maxTx, out int minTy, out int maxTy))
                    break;

                if (TileCount(minTx, maxTx, minTy, maxTy) > upLimit)
                    break;

                zoom++;
            }

            return zoom;
        }

        private static int FinestFittingZoom(
            MapBase map,
            List<Vector3> groundPoints,
            int minZoom,
            int maxZoom,
            int maxTiles)
        {
            for (int z = maxZoom; z >= minZoom; z--)
            {
                if (!TryGetTileRange(map, groundPoints, z, out int minTx, out int maxTx, out int minTy, out int maxTy))
                    continue;

                if (TileCount(minTx, maxTx, minTy, maxTy) <= maxTiles)
                    return z;
            }

            return minZoom;
        }

        private static int FinerZoomThreshold(int maxTiles)
        {
            return Math.Max(1, (maxTiles * 3) / 4);
        }

        private static bool TryGetTileRange(
            MapBase map,
            List<Vector3> groundPoints,
            int zoom,
            out int minTx,
            out int maxTx,
            out int minTy,
            out int maxTy)
        {
            minTx = int.MaxValue;
            maxTx = int.MinValue;
            minTy = int.MaxValue;
            maxTy = int.MinValue;

            for (int i = 0; i < groundPoints.Count; i++)
            {
                var lla = map.GetLLAAtPosition(groundPoints[i]);
                var tile = new Tile(lla.x, lla.y, zoom);
                minTx = Math.Min(minTx, tile.x);
                maxTx = Math.Max(maxTx, tile.x);
                minTy = Math.Min(minTy, tile.y);
                maxTy = Math.Max(maxTy, tile.y);
            }

            return minTx <= maxTx && minTy <= maxTy;
        }

        private static long TileCount(int minTx, int maxTx, int minTy, int maxTy)
        {
            long spanX = maxTx - minTx + 1L;
            long spanY = maxTy - minTy + 1L;
            if (spanX <= 0 || spanY <= 0)
                return 0;

            return spanX * spanY;
        }

        private static void ClampTileRange(
            int maxTiles,
            ref int minTx,
            ref int maxTx,
            ref int minTy,
            ref int maxTy)
        {
            long count = TileCount(minTx, maxTx, minTy, maxTy);
            if (count <= maxTiles)
                return;

            int spanX = maxTx - minTx + 1;
            int spanY = maxTy - minTy + 1;
            int cx = (minTx + maxTx) / 2;
            int cy = (minTy + maxTy) / 2;

            float scale = Mathf.Sqrt(maxTiles / (float)count);
            int newSpanX = Mathf.Max(1, Mathf.RoundToInt(spanX * scale));
            int newSpanY = Mathf.Max(1, maxTiles / newSpanX);
            if (newSpanX * newSpanY > maxTiles)
                newSpanX = Mathf.Max(1, maxTiles / newSpanY);

            minTx = cx - newSpanX / 2;
            maxTx = minTx + newSpanX - 1;
            minTy = cy - newSpanY / 2;
            maxTy = minTy + newSpanY - 1;
        }

        private static void CollectFrustumGroundPoints(
            MapBase map,
            Camera camera,
            float padding,
            List<Vector3> points)
        {
            points.Clear();
            Plane ground = new Plane(map.transform.up, map.transform.position);
            float far = Mathf.Max(camera.farClipPlane, 50000f);

            for (int i = 0; i < ViewportCorners.Length; i++)
            {
                Vector2 c = ViewportCorners[i];
                var uv = new Vector3(
                    Mathf.Lerp(-padding, 1f + padding, c.x),
                    Mathf.Lerp(-padding, 1f + padding, c.y),
                    0f);

                Ray ray = camera.ViewportPointToRay(uv);
                if (ground.Raycast(ray, out float distance) && distance >= 0f && distance <= far)
                    points.Add(ray.GetPoint(distance));
            }

            if (points.Count >= 3)
                return;

            points.Clear();
            if (!TryGetNadir(camera, ground, far, out Vector3 nadir, out float height))
                return;

            AddFovRectangle(camera, ground.normal, nadir, height, padding, points);
        }

        private static bool TryGetNadir(Camera camera, Plane ground, float far, out Vector3 nadir, out float height)
        {
            Ray down = new Ray(camera.transform.position, -ground.normal);
            if (ground.Raycast(down, out float downDist) && downDist >= 0f && downDist <= far)
            {
                nadir = down.GetPoint(downDist);
                height = downDist;
                return height > 1e-4f;
            }

            Ray look = new Ray(camera.transform.position, camera.transform.forward);
            if (ground.Raycast(look, out float lookDist) && lookDist >= 0f && lookDist <= far)
            {
                nadir = look.GetPoint(lookDist);
                height = lookDist;
                return height > 1e-4f;
            }

            nadir = default;
            height = 0f;
            return false;
        }

        private static void AddFovRectangle(
            Camera camera,
            Vector3 groundUp,
            Vector3 nadir,
            float height,
            float padding,
            List<Vector3> points)
        {
            float pad = 1f + padding;
            float halfV;
            if (camera.orthographic)
                halfV = Mathf.Max(camera.orthographicSize, 0.01f) * pad;
            else
                halfV = height * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * pad;

            float halfH = halfV * Mathf.Max(camera.aspect, 0.01f);

            Vector3 right = Vector3.ProjectOnPlane(camera.transform.right, groundUp);
            if (right.sqrMagnitude < 1e-6f)
                right = Vector3.ProjectOnPlane(Vector3.right, groundUp);
            if (right.sqrMagnitude < 1e-6f)
                right = Vector3.ProjectOnPlane(Vector3.forward, groundUp);
            right.Normalize();
            Vector3 forward = Vector3.Cross(groundUp, right);
            if (forward.sqrMagnitude < 1e-6f)
                forward = Vector3.ProjectOnPlane(camera.transform.up, groundUp).normalized;
            else
                forward.Normalize();

            points.Add(nadir - right * halfH - forward * halfV);
            points.Add(nadir + right * halfH - forward * halfV);
            points.Add(nadir + right * halfH + forward * halfV);
            points.Add(nadir - right * halfH + forward * halfV);
        }
    }
}
