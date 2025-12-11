using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms; // 必须引用
using XGIS;

namespace GIS2025
{
    public class XWebTileLayer
    {
        private string _apiKey;
        private string _cacheDir;

        // URL 模板 (已修正为 t0)
        private const string UrlTemplate = "http://t0.tianditu.gov.cn/{0}_c/wmts?SERVICE=WMTS&REQUEST=GetTile&VERSION=1.0.0&LAYER={0}&STYLE=default&TILEMATRIXSET=c&FORMAT=tiles&TILEMATRIX={1}&TILEROW={2}&TILECOL={3}&tk={4}";

        private Dictionary<string, Image> _memoryCache = new Dictionary<string, Image>();
        private HashSet<string> _downloading = new HashSet<string>();
        private readonly object _syncLock = new object();

        // 静态标记
        private static bool _debugCoordShown = false;
        private static bool _debugUrlShown = false;

        public bool IsVisible = true;

        public XWebTileLayer(string apiKey)
        {
            _apiKey = apiKey;
            _cacheDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tile_cache");
            if (!Directory.Exists(_cacheDir)) Directory.CreateDirectory(_cacheDir);

            // 强制 TLS 1.2
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        public void Draw(Graphics g, XView view)
        {
            if (!IsVisible) return;

            // 调试弹窗 1：检查坐标 (只弹一次)
            if (!_debugCoordShown)
            {
                _debugCoordShown = true;
                double minX = view.CurrentMapExtent.GetMinX();
                double minY = view.CurrentMapExtent.GetMinY();
                // 暂时注释掉坐标弹窗，以免干扰，如果你还需要看坐标，可以取消注释
                // MessageBox.Show($"坐标检查:\nX: {minX}\nY: {minY}", "调试");
            }

            // ... (计算层级和索引的代码与之前一致) ...
            double resolution = 1.0 / (view.ToScreenPoint(new XVertex(1, 0)).X - view.ToScreenPoint(new XVertex(0, 0)).X);
            int zoom = (int)Math.Round(Math.Log(1.40625 / resolution, 2));
            zoom = Math.Max(1, Math.Min(18, zoom));

            double tileDeg = 360.0 / Math.Pow(2, zoom);
            double mapMinX = view.CurrentMapExtent.GetMinX();
            double mapMaxX = view.CurrentMapExtent.GetMaxX();
            double mapMinY = view.CurrentMapExtent.GetMinY();
            double mapMaxY = view.CurrentMapExtent.GetMaxY();

            int startCol = (int)Math.Floor((mapMinX + 180.0) / tileDeg);
            int endCol = (int)Math.Floor((mapMaxX + 180.0) / tileDeg);
            int startRow = (int)Math.Floor((90.0 - mapMaxY) / tileDeg);
            int endRow = (int)Math.Floor((90.0 - mapMinY) / tileDeg);

            if ((endCol - startCol) * (endRow - startRow) > 500) return;

            for (int r = startRow; r <= endRow; r++)
            {
                for (int c = startCol; c <= endCol; c++)
                {
                    DrawTile(g, view, "vec", zoom, r, c, tileDeg);
                    DrawTile(g, view, "cva", zoom, r, c, tileDeg);
                }
            }
        }

        private void DrawTile(Graphics g, XView view, string layerType, int z, int r, int c, double tileDeg)
        {
            string tileKey = $"{layerType}_{z}_{r}_{c}";
            string localPath = Path.Combine(_cacheDir, tileKey + ".png");
            Image img = null;

            lock (_syncLock) { if (_memoryCache.ContainsKey(tileKey)) img = _memoryCache[tileKey]; }

            if (img == null && File.Exists(localPath))
            {
                try
                {
                    using (FileStream fs = new FileStream(localPath, FileMode.Open, FileAccess.Read))
                    {
                        img = new Bitmap(Image.FromStream(fs));
                        lock (_syncLock) { if (!_memoryCache.ContainsKey(tileKey)) _memoryCache[tileKey] = img; }
                    }
                }
                catch { }
            }

            if (img == null)
            {
                DownloadTileAsync(layerType, z, r, c, localPath);
                return;
            }

            if (img != null)
            {
                double tileMinX = -180.0 + c * tileDeg;
                double tileMaxY = 90.0 - r * tileDeg;
                Point p1 = view.ToScreenPoint(new XVertex(tileMinX, tileMaxY));
                Point p2 = view.ToScreenPoint(new XVertex(tileMinX + tileDeg, tileMaxY - tileDeg));
                int width = Math.Abs(p2.X - p1.X) + 1;
                int height = Math.Abs(p2.Y - p1.Y) + 1;
                try { g.DrawImage(img, p1.X, p1.Y, width, height); } catch { }
            }
        }

        private void DownloadTileAsync(string layerType, int z, int r, int c, string localPath)
        {
            string key = $"{layerType}_{z}_{r}_{c}";
            lock (_syncLock) { if (_downloading.Contains(key)) return; _downloading.Add(key); }

            ThreadPool.QueueUserWorkItem((state) =>
            {
                try
                {
                    string url = string.Format(UrlTemplate, layerType, z, r, c, _apiKey);


                    using (WebClient wc = new WebClient())
                    {
                        wc.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                        byte[] data = wc.DownloadData(url);

                        using (MemoryStream ms = new MemoryStream(data))
                        {
                            Image img = Image.FromStream(ms);
                            img.Save(localPath);
                            lock (_syncLock) { if (!_memoryCache.ContainsKey(key)) _memoryCache[key] = new Bitmap(img); }
                        }
                    }
                }
                catch (Exception)
                {
                    // 默默失败，不报错
                }
                finally
                {
                    lock (_syncLock) { _downloading.Remove(key); }
                }
            });
        }
    }
}