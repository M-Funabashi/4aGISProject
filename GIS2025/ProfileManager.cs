using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using XGIS; // 引用底层库

namespace GIS2025
{
    public class ProfileManager
    {
        // 单例模式
        private static ProfileManager _instance;
        public static ProfileManager Instance
        {
            get
            {
                if (_instance == null) _instance = new ProfileManager();
                return _instance;
            }
        }

        public List<UserProfile> Users { get; set; } = new List<UserProfile>();

        // 全局存档路径
        private string ProfilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "profiles.json");

        private ProfileManager()
        {
            Load(); // 启动时自动加载
        }

        // ==========================================
        // 1. 全局存档管理 (profiles.json)
        // ==========================================
        public void Save()
        {
            try
            {
                string json = JsonConvert.SerializeObject(Users, Formatting.Indented);
                File.WriteAllText(ProfilePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存档案失败: " + ex.Message);
            }
        }

        public void Load()
        {
            if (!File.Exists(ProfilePath)) return;
            try
            {
                string json = File.ReadAllText(ProfilePath);
                var loadedUsers = JsonConvert.DeserializeObject<List<UserProfile>>(json);
                if (loadedUsers != null) Users = loadedUsers;
            }
            catch { }
        }

        // 【新增】 启动时重建所有轨迹 (复活红线)
        // 这个方法需要在 Form1 启动时调用，因为只有 Form1 才有 calculator
        public void RestoreGeometries(JourneyCalculator calculator)
        {
            foreach (var user in Users)
            {
                foreach (var archive in user.Archives)
                {
                    foreach (var trip in archive.Trips)
                    {
                        // 如果内存里没有几何信息（刚读取完），就重新计算
                        if (trip.Geometry == null)
                        {
                            trip.Geometry = calculator.ReconstructTrip(
                                trip.RouteName,
                                trip.Direction,
                                trip.StartStop,
                                trip.EndStop
                            );
                        }
                    }
                }
            }
        }


        public UserProfile GetOrCreateDefaultUser()
        {
            if (Users.Count == 0)
            {
                var user = new UserProfile("默认用户");
                user.Archives.Add(new DailyArchive(DateTime.Now.ToString("MM月dd日") + " 初次体验"));
                Users.Add(user);
                Save();
            }
            return Users[0];
        }

        // ==========================================
        // 2. .trj 文件导出功能
        // ==========================================
        // ==========================================
        // 2. .trj 文件导出功能 (已修复：包含方向)
        // ==========================================
        public void ExportTrj(DailyArchive archive, string filePath, string authorName)
        {
            try
            {
                TrjFileModel trj = new TrjFileModel
                {
                    ArchiveName = archive.Name,
                    CreateTime = DateTime.Now,
                    Author = authorName
                };

                for (int i = 0; i < archive.Trips.Count; i++)
                {
                    var trip = archive.Trips[i];

                    trj.Trips.Add(new TrjTripItem
                    {
                        Sequence = i + 1,
                        RouteName = trip.RouteName,
                        Direction = trip.Direction, // 【修复】现在可以正常导出方向了
                        StartStop = trip.StartStop,
                        EndStop = trip.EndStop
                    });
                }

                string json = JsonConvert.SerializeObject(trj, Formatting.Indented);
                File.WriteAllText(filePath, json);
                MessageBox.Show("导出成功！");
            }
            catch (Exception ex)
            {
                MessageBox.Show("导出失败: " + ex.Message);
            }
        }

        // ==========================================
        // 3. .trj 文件导入功能 (已修复：传入方向参数)
        // ==========================================
        public void ImportTrj(string filePath, UserProfile targetUser, JourneyCalculator calculator)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                TrjFileModel trj = JsonConvert.DeserializeObject<TrjFileModel>(json);

                if (trj == null) throw new Exception("文件格式错误");

                DailyArchive newArchive = new DailyArchive(trj.ArchiveName + " (导入)");

                int successCount = 0;
                foreach (var item in trj.Trips)
                {
                    // 1. 重建几何轨迹
                    XLineSpatial geometry = calculator.ReconstructTrip(
                        item.RouteName,
                        item.Direction,
                        item.StartStop,
                        item.EndStop
                    );

                    if (geometry != null)
                    {
                        // 2. 【修复报错】这里补充了 item.Direction 参数
                        var tripItem = new TripArchiveItem(
                            item.RouteName,
                            item.Direction,  // <--- 之前报错就是缺了这个
                            item.StartStop,
                            item.EndStop,
                            geometry
                        );
                        tripItem.Length = geometry.length;
                        newArchive.Trips.Add(tripItem);
                        successCount++;
                    }
                }

                targetUser.Archives.Add(newArchive);
                Save(); // 保存到 profiles.json

                MessageBox.Show($"导入成功！\n档案名：{newArchive.Name}\n成功还原行程：{successCount}/{trj.Trips.Count}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("导入失败: " + ex.Message);
            }
        }
    }
}