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

        // 根目录：程序运行目录/user
        private string UserRootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "user");

        // 当前登录的用户 (全局指针)
        public UserProfile CurrentUser { get; private set; }

        // ★★★ 新增：全局用户列表缓存，供外部访问（如删除用户时） ★★★
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

        // ==========================================
        // 1. 用户管理 (登录/注册/列表)
        // ==========================================

        /// <summary>
        /// 获取所有用户列表 (只读取 info.json，不加载行程)
        /// 用于登录界面显示
        /// </summary>
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
                    catch { /* 文件损坏跳过 */ }
                }
            }
            users.Sort((a, b) => string.Compare(a.Name, b.Name)); // 首字母排序 A-Z
            // ★★★ 更新全局 Users 属性，防止外部调用空指针 ★★★
            this.Users = users;
            return users;
        }

        /// <summary>
        /// 创建新用户
        /// </summary>
        /// <returns>成功返回 true，用户名重复返回 false</returns>
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
                AvatarPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "pic", "chr", avatarFileName),
                TotalDistance = 0
            };

            SaveUserInfo(newUser);

            // 更新缓存列表
            this.Users.Add(newUser);

            return true;
        }

        /// <summary>
        /// 登录用户：加载 info 和所有 .trj 档案
        /// </summary>
        public void LoginUser(string name)
        {
            string userDir = Path.Combine(UserRootPath, name);
            string infoPath = Path.Combine(userDir, "info.json");

            if (!File.Exists(infoPath)) throw new Exception($"用户 {name} 数据丢失！");

            // 1. 加载基本信息
            string json = File.ReadAllText(infoPath);
            CurrentUser = JsonConvert.DeserializeObject<UserProfile>(json);

            // 确保列表初始化
            CurrentUser.Archives = new List<DailyArchive>();

            // 2. 扫描并加载所有 .trj 文件
            var trjFiles = Directory.GetFiles(userDir, "*.trj");
            foreach (var file in trjFiles)
            {
                try
                {
                    string trjJson = File.ReadAllText(file);
                    // 这里直接反序列化为 DailyArchive
                    // 几何红线需要在 UI 层调用 RestoreGeometries 复活
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

        // ==========================================
        // 2. 存档操作 (保存/更新)
        // ==========================================

        // ★★★ 新增：兼容 Save() 调用 ★★★
        public void Save()
        {
            SaveCurrentUser();
        }

        /// <summary>
        /// 保存单个档案到 .trj 文件
        /// </summary>
        public void SaveArchive(DailyArchive archive)
        {
            if (CurrentUser == null) return;

            string userDir = Path.Combine(UserRootPath, CurrentUser.Name);
            // 简单处理文件名非法字符
            string safeArchiveName = string.Join("_", archive.Name.Split(Path.GetInvalidFileNameChars()));
            string filePath = Path.Combine(userDir, safeArchiveName + ".trj");

            try
            {
                // 1. 保存档案文件
                // 注意：Geometry 被标记为 JsonIgnore，所以只存文本
                string json = JsonConvert.SerializeObject(archive, Formatting.Indented);
                File.WriteAllText(filePath, json);

                // 2. 更新用户的总里程并保存 info.json
                // ★ 改为调用内部辅助方法，修复报错
                CurrentUser.TotalDistance = CalculateTotalKm(CurrentUser);
                SaveUserInfo(CurrentUser);
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存失败: " + ex.Message);
            }
        }

        /// <summary>
        /// 仅保存用户信息 (info.json)
        /// </summary>
        private void SaveUserInfo(UserProfile user)
        {
            string userDir = Path.Combine(UserRootPath, user.Name);
            string infoPath = Path.Combine(userDir, "info.json");

            // 序列化 UserProfile 时，Archives 属性有 [JsonIgnore]，所以不会被存进去
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

        /// <summary>
        /// 更新用户信息（支持改名和换头像）
        /// </summary>
        public bool UpdateUser(UserProfile user, string newName, string newAvatarFileName)
        {
            try
            {
                // 1. 如果改名了，需要处理文件夹重命名
                if (user.Name != newName)
                {
                    // 检查新名字是否冲突
                    string newPath = Path.Combine(UserRootPath, newName);
                    if (Directory.Exists(newPath)) return false; // 名字已存在

                    // 执行文件夹重命名
                    string oldPath = Path.Combine(UserRootPath, user.Name);
                    Directory.Move(oldPath, newPath);

                    // 更新内存中的名字
                    user.Name = newName;
                }

                // 2. 更新头像路径
                user.AvatarPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "pic", "chr", newAvatarFileName);

                // 3. 保存新的 info.json
                SaveUserInfo(user); // 确保 SaveUserInfo 方法存在且可用

                return true;
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("更新失败：" + ex.Message);
                return false;
            }
        }


        // ==========================================
        // 3. 辅助功能
        // ==========================================

        /// <summary>
        /// 复活几何红线 (登录后必须调用此方法，否则地图上没线)
        /// </summary>
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

                        // 顺便校准一下长度
                        if (trip.Geometry != null)
                        {
                            // ★ 修正：BasicClasses 计算的是度，乘以 111 才是 km
                            // 如果原来的代码写 111000 那是转成米，但 UI 显示的是 km
                            //trip.Length = trip.Geometry.length * 111.0;
                        }
                    }
                }
            }
            // ★ 修复报错：使用内部计算方法
            CurrentUser.TotalDistance = CalculateTotalKm(CurrentUser);

            // 顺便保存一下最新的 info.json
            SaveCurrentUser();
        }

        // ==========================================
        // 4. 新增：档案高级操作与辅助
        // ==========================================

        /// <summary>
        /// 获取档案的物理文件路径
        /// </summary>
        public string GetArchiveFilePath(DailyArchive archive)
        {
            if (CurrentUser == null) return null;
            string userDir = Path.Combine(UserRootPath, CurrentUser.Name);
            // 保持和 SaveArchive 一致的文件名生成逻辑
            string safeName = string.Join("_", archive.Name.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(userDir, safeName + ".trj");
        }

        /// <summary>
        /// 重命名档案
        /// </summary>
        public bool RenameArchive(DailyArchive archive, string newName)
        {
            if (CurrentUser == null) return false;

            // 1. 获取旧路径
            string oldPath = GetArchiveFilePath(archive);

            // 2. 预判新路径
            string userDir = Path.Combine(UserRootPath, CurrentUser.Name);
            string safeNewName = string.Join("_", newName.Split(Path.GetInvalidFileNameChars()));
            string newPath = Path.Combine(userDir, safeNewName + ".trj");

            // 3. 检查重名
            if (File.Exists(newPath)) return false;

            try
            {
                // 4. 物理重命名 (如果文件存在)
                if (File.Exists(oldPath))
                {
                    File.Move(oldPath, newPath);
                }

                // 5. 更新内存对象的属性
                archive.Name = newName;

                // 6. 保存内容 (确保文件内部的 Name 字段也更新，且写入新路径)
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

            // 1. 删除文件
            if (File.Exists(filePath)) File.Delete(filePath);

            // 2. 内存移除
            CurrentUser.Archives.Remove(archive);

            // 3. 更新统计
            // ★ 修复报错
            CurrentUser.TotalDistance = CalculateTotalKm(CurrentUser);
            SaveUserInfo(CurrentUser);
        }

        // ★★★ 新增：内部辅助方法，计算总里程 (替代 RecalculateTotalDistance) ★★★
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

        // ★★★ 新增：供导入 .trj 使用的方法 ★★★
        public void ImportTrj(string filePath, UserProfile targetUser, JourneyCalculator calculator)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                DailyArchive archive = JsonConvert.DeserializeObject<DailyArchive>(json);

                if (archive != null)
                {
                    // 重新生成 ID 防止冲突
                    //archive.ID = Guid.NewGuid().ToString();

                    // 添加到目标用户
                    targetUser.Archives.Add(archive);

                    // 临时登录该用户以保存 (如果不是当前用户)
                    // 或者手动构建路径保存
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
                        // 只是保存一下用户信息（更新里程需要下次登录计算，或者这里手动算）
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

        // ★★★ 新增：导出 .trj 使用的方法 ★★★
        public void ExportTrj(DailyArchive archive, string savePath, string authorName)
        {
            // TrjFileModel model = new TrjFileModel(); ... 
            // 如果为了简单，直接拷贝文件内容即可，或者序列化当前对象
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