using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms; // 引用 MessageBox
using XGIS;

namespace GIS2025
{
    public class XWebTileLayer
    {
        private string _apiKey;
        private string _cacheDir;

        // URL 模板
        private const string UrlTemplate = "http://t0.tianditu.gov.cn/{0}_c/wmts?SERVICE=WMTS&REQUEST=GetTile&VERSION=1.0.0&LAYER={0}&STYLE=default&TILEMATRIXSET=c&FORMAT=tiles&TILEMATRIX={1}&TILEROW={2}&TILECOL={3}&tk={4}";

        private Dictionary<string, Image> _memoryCache = new Dictionary<string, Image>();
        private HashSet<string> _downloading = new HashSet<string>();
        private readonly object _syncLock = new object();

        public bool IsVisible = true;

        public XWebTileLayer(string apiKey)
        {
            _apiKey = apiKey;
            _cacheDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tile_cache");
            if (!Directory.Exists(_cacheDir)) Directory.CreateDirectory(_cacheDir);

            // 【关键修复 1】强制使用 TLS 1.2，否则天地图连不上
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        public void Draw(Graphics g, XView view)
        {
            if (!IsVisible) return;

            // 计算层级
            double resolution = 1.0 / (view.ToScreenPoint(new XVertex(1, 0)).X - view.ToScreenPoint(new XVertex(0, 0)).X);
            int zoom = (int)Math.Round(Math.Log(1.40625 / resolution, 2));
            zoom = Math.Max(1, Math.Min(18, zoom)); // 限制范围

            double tileDeg = 360.0 / Math.Pow(2, zoom);

            // 计算范围
            double minX = view.CurrentMapExtent.GetMinX();
            double maxX = view.CurrentMapExtent.GetMaxX();
            double minY = view.CurrentMapExtent.GetMinY();
            double maxY = view.CurrentMapExtent.GetMaxY();



            int startCol = (int)Math.Floor((minX + 180.0) / tileDeg);
            int endCol = (int)Math.Floor((maxX + 180.0) / tileDeg);
            int startRow = (int)Math.Floor((90.0 - maxY) / tileDeg);
            int endRow = (int)Math.Floor((90.0 - minY) / tileDeg);

            // 限制循环次数，防止一次请求太多卡死
            if ((endCol - startCol) * (endRow - startRow) > 200) return;

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

            lock (_syncLock)
            {
                if (_memoryCache.ContainsKey(tileKey)) img = _memoryCache[tileKey];
            }

            if (img == null && File.Exists(localPath))
            {
                try
                {
                    using (FileStream fs = new FileStream(localPath, FileMode.Open, FileAccess.Read))
                    {
                        img = new Bitmap(Image.FromStream(fs));
                        lock (_syncLock)
                        {
                            if (!_memoryCache.ContainsKey(tileKey)) _memoryCache[tileKey] = img;
                        }
                    }
                }
                catch { } // 文件可能损坏
            }

            if (img == null)
            {
                DownloadTileAsync(layerType, z, r, c, localPath);
                return;
            }

            // 绘制
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

        // 静态标志位，防止弹窗弹个没完
        private static bool _debugShown = false;

        private void DownloadTileAsync(string layerType, int z, int r, int c, string localPath)
        {
            string key = $"{layerType}_{z}_{r}_{c}";

            lock (_syncLock)
            {
                if (_downloading.Contains(key)) return;
                _downloading.Add(key);
            }

            ThreadPool.QueueUserWorkItem((state) =>
            {
                try
                {
                    string url = string.Format(UrlTemplate, layerType, z, r, c, _apiKey);

                    // 【调试代码】只要没弹过窗，就弹一次，不限层级
                    if (!_debugShown)
                    {
                        _debugShown = true; // 锁住，只弹一次
                        System.Windows.Forms.Clipboard.SetText(url);
                        System.Windows.Forms.MessageBox.Show("调试模式：已复制第一个瓦片URL到剪贴板！\n请去浏览器粘贴测试：\n" + url);
                    }

                    using (WebClient wc = new WebClient())
                    {
                        // 【关键修复 2】伪装浏览器 UA，否则 403 Forbidden
                        wc.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

                        byte[] data = wc.DownloadData(url);

                        using (MemoryStream ms = new MemoryStream(data))
                        {
                            // 【关键修复 3】验证是否为图片
                            // 如果 Key 错误，天地图会返回一段 XML 文本，Image.FromStream 会抛异常，
                            // 从而跳过下面的 Save，避免保存垃圾文件
                            Image img = Image.FromStream(ms);
                            img.Save(localPath);

                            lock (_syncLock)
                            {
                                if (!_memoryCache.ContainsKey(key))
                                    _memoryCache[key] = new Bitmap(img);
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // 如果下载失败，可以 Console.WriteLine(ex.Message);
                }
                finally
                {
                    lock (_syncLock) { _downloading.Remove(key); }
                }
            });
        }
    }
}