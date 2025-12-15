using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using XGIS;

namespace GIS2025
{
    public class ProfileManager
    {
        private static ProfileManager _instance;
        public static ProfileManager Instance
        {
            get
            {
                if (_instance == null) _instance = new ProfileManager();
                return _instance;
            }
        }

        // 根目录：程序运行目录/user
        private string UserRootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "user");

        // 当前登录的用户
        public UserProfile CurrentUser { get; private set; }

        // 全局用户列表缓存
        public List<UserProfile> Users { get; private set; } = new List<UserProfile>();

        private ProfileManager()
        {
            Init();
        }

        // 初始化：确保根目录存在
        public void Init()
        {
            if (!Directory.Exists(UserRootPath))
            {
                Directory.CreateDirectory(UserRootPath);
            }
            // 初始化时加载一次列表
            LoadUserList();
        }

        // 用户管理，用于登录界面显示
        public List<UserProfile> LoadUserList()
        {
            var users = new List<UserProfile>();
            if (!Directory.Exists(UserRootPath)) return users;

            // 扫描 user 下的所有子文件夹
            var dirs = Directory.GetDirectories(UserRootPath);
            foreach (var dir in dirs)
            {
                string infoPath = Path.Combine(dir, "info.json");
                if (File.Exists(infoPath))
                {
                    try
                    {
                        string json = File.ReadAllText(infoPath);
                        var user = JsonConvert.DeserializeObject<UserProfile>(json);
                        if (user != null) users.Add(user);
                    }
                    catch {}
                }
            }
            users.Sort((a, b) => string.Compare(a.Name, b.Name)); // 首字母排序
            this.Users = users;
            return users;
        }

        // 创建新用户
        public bool CreateUser(string name, string avatarFileName)
        {
            string safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars())); // 去除非法字符
            string userDir = Path.Combine(UserRootPath, safeName);

            if (Directory.Exists(userDir)) return false; // 用户已存在

            Directory.CreateDirectory(userDir);

            // 构建新用户对象
            UserProfile newUser = new UserProfile(safeName)
            {
                // 存完整路径
                AvatarPath = Path.Combine("data", "pic", "chr", avatarFileName),
                TotalDistance = 0
            };

            SaveUserInfo(newUser);
            // 更新缓存列表
            this.Users.Add(newUser);
            return true;
        }

        // 登录用户：加载info和所有.trj档案
        public void LoginUser(string name)
        {
            string userDir = Path.Combine(UserRootPath, name);
            string infoPath = Path.Combine(userDir, "info.json");

            if (!File.Exists(infoPath)) throw new Exception($"用户 {name} 数据丢失");

            // 加载基本信息
            string json = File.ReadAllText(infoPath);
            CurrentUser = JsonConvert.DeserializeObject<UserProfile>(json);
            CurrentUser.Archives = new List<DailyArchive>();

            // 扫描并加载所有.trj文件
            var trjFiles = Directory.GetFiles(userDir, "*.trj");
            foreach (var file in trjFiles)
            {
                try
                {
                    string trjJson = File.ReadAllText(file);
                    var archive = JsonConvert.DeserializeObject<DailyArchive>(trjJson);

                    if (archive != null)
                    {
                        CurrentUser.Archives.Add(archive);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"档案 {Path.GetFileName(file)} 读取失败: {ex.Message}");
                }
            }
        }

        // 存档操作
        public void Save()
        {
            SaveCurrentUser();
        }

        // 保存单个档案到.trj文件
        public void SaveArchive(DailyArchive archive)
        {
            if (CurrentUser == null) return;

            string userDir = Path.Combine(UserRootPath, CurrentUser.Name);
            // 简单处理文件名非法字符
            string safeArchiveName = string.Join("_", archive.Name.Split(Path.GetInvalidFileNameChars()));
            string filePath = Path.Combine(userDir, safeArchiveName + ".trj");

            try
            {
                // 保存档案文件
                string json = JsonConvert.SerializeObject(archive, Formatting.Indented);
                File.WriteAllText(filePath, json);
                // 更新用户的总里程并保存 info.json
                CurrentUser.TotalDistance = CalculateTotalKm(CurrentUser);
                SaveUserInfo(CurrentUser);
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存失败: " + ex.Message);
            }
        }

        // 仅保存用户信息
        private void SaveUserInfo(UserProfile user)
        {
            string userDir = Path.Combine(UserRootPath, user.Name);
            string infoPath = Path.Combine(userDir, "info.json");
            string json = JsonConvert.SerializeObject(user, Formatting.Indented);
            File.WriteAllText(infoPath, json);
        }

        public void SaveCurrentUser()
        {
            if (CurrentUser != null)
            {
                SaveUserInfo(CurrentUser);
            }
        }

        // 更新用户信息
        public bool UpdateUser(UserProfile user, string newName, string newAvatarFileName)
        {
            try
            {
                // 如果改名了，文件夹重命名
                if (user.Name != newName)
                {
                    // 检查新名字是否冲突
                    string newPath = Path.Combine(UserRootPath, newName);
                    if (Directory.Exists(newPath)) return false; 
                    // 执行文件夹重命名
                    string oldPath = Path.Combine(UserRootPath, user.Name);
                    Directory.Move(oldPath, newPath);
                    // 更新内存中的名字
                    user.Name = newName;
                }

                // 更新头像路径
                user.AvatarPath = Path.Combine("data", "pic", "chr", newAvatarFileName);

                // 保存新的 info.json
                SaveUserInfo(user); 

                return true;
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("更新失败：" + ex.Message);
                return false;
            }
        }

        // 辅助功能
        // 复活几何红线，登录后调用此方法
        public void RestoreGeometries(JourneyCalculator calculator)
        {
            if (CurrentUser == null) return;

            foreach (var archive in CurrentUser.Archives)
            {
                foreach (var trip in archive.Trips)
                {
                    // 如果内存里没有几何信息（刚从 .trj 读取完），就重新计算
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
            CurrentUser.TotalDistance = CalculateTotalKm(CurrentUser);
            // 保存最新的 info.json
            SaveCurrentUser();
        }

        // 档案高级操作与辅助
        // 获取档案的物理文件路径
        public string GetArchiveFilePath(DailyArchive archive)
        {
            if (CurrentUser == null) return null;
            string userDir = Path.Combine(UserRootPath, CurrentUser.Name);
            // 保持和 SaveArchive 一致的文件名生成逻辑
            string safeName = string.Join("_", archive.Name.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(userDir, safeName + ".trj");
        }

        // 重命名档案
        public bool RenameArchive(DailyArchive archive, string newName)
        {
            if (CurrentUser == null) return false;

            // 获取旧路径
            string oldPath = GetArchiveFilePath(archive);

            // 预判新路径
            string userDir = Path.Combine(UserRootPath, CurrentUser.Name);
            string safeNewName = string.Join("_", newName.Split(Path.GetInvalidFileNameChars()));
            string newPath = Path.Combine(userDir, safeNewName + ".trj");

            // 检查重名
            if (File.Exists(newPath)) return false;

            try
            {
                // 物理重命名
                if (File.Exists(oldPath))
                {
                    File.Move(oldPath, newPath);
                }

                // 更新内存对象的属性
                archive.Name = newName;

                // 保存内容
                SaveArchive(archive);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("重命名失败: " + ex.Message);
                return false;
            }
        }

        // 删除档案功能
        public void DeleteArchive(DailyArchive archive)
        {
            if (CurrentUser == null) return;

            string userDir = Path.Combine(UserRootPath, CurrentUser.Name);
            string safeArchiveName = string.Join("_", archive.Name.Split(Path.GetInvalidFileNameChars()));
            string filePath = Path.Combine(userDir, safeArchiveName + ".trj");

            // 删除文件
            if (File.Exists(filePath)) File.Delete(filePath);

            // 内存移除
            CurrentUser.Archives.Remove(archive);

            // 更新统计
            CurrentUser.TotalDistance = CalculateTotalKm(CurrentUser);
            SaveUserInfo(CurrentUser);
        }

        // 内部辅助方法，计算总里程
        private double CalculateTotalKm(UserProfile user)
        {
            if (user == null || user.Archives == null) return 0;
            double total = 0;
            foreach (var archive in user.Archives)
            {
                if (archive.Trips != null)
                {
                    foreach (var trip in archive.Trips)
                    {
                        total += trip.Length;
                    }
                }
            }
            return total;
        }

        // 导入 .trj
        public void ImportTrj(string filePath, UserProfile targetUser, JourneyCalculator calculator)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                DailyArchive archive = JsonConvert.DeserializeObject<DailyArchive>(json);

                if (archive != null)
                {
                    // 添加到目标用户
                    targetUser.Archives.Add(archive);

                    // 手动构建路径保存
                    string userDir = Path.Combine(UserRootPath, targetUser.Name);
                    string safeName = string.Join("_", archive.Name.Split(Path.GetInvalidFileNameChars()));
                    string destPath = Path.Combine(userDir, safeName + ".trj");

                    File.WriteAllText(destPath, JsonConvert.SerializeObject(archive, Formatting.Indented));

                    // 如果是当前用户，需要计算几何红线
                    if (CurrentUser != null && targetUser.Name == CurrentUser.Name)
                    {
                        RestoreGeometries(calculator);
                    }
                    else
                    {
                        targetUser.TotalDistance = CalculateTotalKm(targetUser);
                        SaveUserInfo(targetUser);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("导入 .trj 失败: " + ex.Message);
            }
        }

        // 导出 .trj
        public void ExportTrj(DailyArchive archive, string savePath, string authorName)
        {
            try
            {
                string json = JsonConvert.SerializeObject(archive, Formatting.Indented);
                File.WriteAllText(savePath, json);
                MessageBox.Show("导出成功！");
            }
            catch (Exception ex)
            {
                MessageBox.Show("导出失败: " + ex.Message);
            }
        }
    }
}