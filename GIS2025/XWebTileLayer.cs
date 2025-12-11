using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading;
using XGIS;

namespace GIS2025
{
    public class XWebTileLayer
    {
        private string _apiKey;
        private string _cacheDir;

        // URL 模板
        private const string UrlTemplate = "http://t0.tianditu.gov.cn/{0}_c/wmts?SERVICE=WMTS&REQUEST=GetTile&VERSION=1.0.0&LAYER={0}&STYLE=default&TILEMATRIXSET=c&FORMAT=tiles&TILEMATRIX={1}&TILEROW={2}&TILECOL={3}&tk={4}";

        // 内存缓存
        private Dictionary<string, Image> _memoryCache = new Dictionary<string, Image>();
        // 下载队列
        private HashSet<string> _downloading = new HashSet<string>();

        // 【新增】线程锁对象
        private readonly object _syncLock = new object();

        public bool IsVisible = true;

        public XWebTileLayer(string apiKey)
        {
            _apiKey = apiKey;
            _cacheDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tile_cache");
            if (!Directory.Exists(_cacheDir)) Directory.CreateDirectory(_cacheDir);
        }

        public void Draw(Graphics g, XView view)
        {
            if (!IsVisible) return;

            // 计算层级
            double resolution = 1.0 / (view.ToScreenPoint(new XVertex(1, 0)).X - view.ToScreenPoint(new XVertex(0, 0)).X);
            int zoom = (int)Math.Round(Math.Log(1.40625 / resolution, 2));
            zoom = Math.Max(1, Math.Min(18, zoom));

            double tileDeg = 360.0 / Math.Pow(2, zoom);

            // 计算索引范围
            double minX = view.CurrentMapExtent.GetMinX();
            double maxX = view.CurrentMapExtent.GetMaxX();
            double minY = view.CurrentMapExtent.GetMinY();
            double maxY = view.CurrentMapExtent.GetMaxY();

            int startCol = (int)Math.Floor((minX + 180.0) / tileDeg);
            int endCol = (int)Math.Floor((maxX + 180.0) / tileDeg);
            int startRow = (int)Math.Floor((90.0 - maxY) / tileDeg);
            int endRow = (int)Math.Floor((90.0 - minY) / tileDeg);

            // 循环绘制
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

            // ==========================================
            // 【修改点 1】 读取内存缓存时加锁
            // ==========================================
            lock (_syncLock)
            {
                if (_memoryCache.ContainsKey(tileKey))
                {
                    img = _memoryCache[tileKey];
                }
            }

            // 如果内存没有，尝试读磁盘
            if (img == null && File.Exists(localPath))
            {
                try
                {
                    using (FileStream fs = new FileStream(localPath, FileMode.Open, FileAccess.Read))
                    {
                        // 必须深拷贝一份图片，否则流关闭后图片会失效，或者被多线程占用
                        img = new Bitmap(Image.FromStream(fs));

                        // 【修改点 2】 写入内存缓存时加锁
                        lock (_syncLock)
                        {
                            if (!_memoryCache.ContainsKey(tileKey))
                                _memoryCache[tileKey] = img;
                        }
                    }
                }
                catch { }
            }

            // 如果磁盘也没有，下载
            if (img == null)
            {
                DownloadTileAsync(layerType, z, r, c, localPath);
                return;
            }

            // 绘制图片
            if (img != null)
            {
                double tileMinX = -180.0 + c * tileDeg;
                double tileMaxY = 90.0 - r * tileDeg;

                Point p1 = view.ToScreenPoint(new XVertex(tileMinX, tileMaxY));
                Point p2 = view.ToScreenPoint(new XVertex(tileMinX + tileDeg, tileMaxY - tileDeg));

                int width = Math.Abs(p2.X - p1.X) + 1;
                int height = Math.Abs(p2.Y - p1.Y) + 1;

                try
                {
                    // 这里的 img 虽然在内存里，但 Drawing 是线程安全的，只要不 dispose 就行
                    g.DrawImage(img, p1.X, p1.Y, width, height);
                }
                catch { }
            }
        }

        private void DownloadTileAsync(string layerType, int z, int r, int c, string localPath)
        {
            string key = $"{layerType}_{z}_{r}_{c}";

            // ==========================================
            // 【修改点 3】 检查下载队列时加锁
            // ==========================================
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

                    // 【新增调试代码】 
                    // 如果是第一张瓦片，把 URL 复制到剪贴板并弹窗告诉你
                    if (z == 1 || z == 2)
                    {
                        System.Windows.Forms.Clipboard.SetText(url); // 复制到剪贴板
                        System.Windows.Forms.MessageBox.Show("瓦片 URL 已复制到剪贴板：\n" + url);
                    }


                    using (WebClient wc = new WebClient())
                    {
                        wc.Headers.Add("User-Agent", "Mozilla/5.0");
                        byte[] data = wc.DownloadData(url);

                        using (MemoryStream ms = new MemoryStream(data))
                        {
                            // 保存到磁盘
                            Image img = Image.FromStream(ms);
                            img.Save(localPath);

                            // 存入内存 (加锁)
                            lock (_syncLock)
                            {
                                if (!_memoryCache.ContainsKey(key))
                                    _memoryCache[key] = new Bitmap(img);
                            }
                        }
                    }
                }
                catch
                {
                    // 下载失败忽略
                }
                finally
                {
                    // ==========================================
                    // 【修改点 4】 移除下载状态时加锁
                    // ==========================================
                    lock (_syncLock)
                    {
                        _downloading.Remove(key);
                    }
                }
            });
        }
    }
}