using System;

namespace SplashDemo
{
    /// <summary>
    /// 启动画面配置类
    /// </summary>
    public class SplashConfiguration
    {
        /// <summary>
        /// 最小显示时间（毫秒）
        /// </summary>
        public int MinimumDisplayTime { get; set; } = 2000;

        /// <summary>
        /// 是否启用动画效果
        /// </summary>
        public bool EnableAnimations { get; set; } = true;

        /// <summary>
        /// 是否显示详细的加载进度
        /// </summary>
        public bool ShowDetailedProgress { get; set; } = true;

        /// <summary>
        /// 应用程序名称
        /// </summary>
        public string ApplicationName { get; set; } = "Evaluation Kit GUI";

        /// <summary>
        /// 应用程序描述
        /// </summary>
        public string ApplicationDescription { get; set; } = "Silergy Evaluation Platform";

        /// <summary>
        /// 版本号
        /// </summary>
        public string Version { get; set; } = "Version 1.0.0";

        /// <summary>
        /// 版权信息
        /// </summary>
        public string CopyrightText { get; set; } = "© 2025 Silergy Corp. All Rights Reserved.";

        /// <summary>
        /// 主题颜色（十六进制格式）
        /// </summary>
        public string PrimaryColor { get; set; } = "#FF007ACC";

        /// <summary>
        /// 次要颜色（十六进制格式）
        /// </summary>
        public string SecondaryColor { get; set; } = "#FF005CB9";

        /// <summary>
        /// 是否允许用户取消启动（Esc键）
        /// </summary>
        public bool AllowUserCancel { get; set; } = true;

        /// <summary>
        /// 错误重试次数
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// 获取默认配置实例
        /// </summary>
        public static SplashConfiguration Default => new SplashConfiguration();

        /// <summary>
        /// 从配置文件加载设置（预留接口）
        /// </summary>
        public static SplashConfiguration LoadFromConfig()
        {
            // 这里可以从app.config或其他配置源加载设置
            // 目前返回默认配置
            return Default;
        }
    }
}