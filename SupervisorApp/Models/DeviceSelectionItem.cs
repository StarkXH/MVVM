using System;

namespace SupervisorApp.Models
{
    /// <summary>
    /// ???????
    /// </summary>
    public class DeviceSelectionItem
    {
        /// <summary>
        /// ??????
        /// </summary>
        public string DeviceType { get; set; }

        /// <summary>
        /// ??????
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// ????
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// ??????
        /// </summary>
        public string IconPath { get; set; }

        /// <summary>
        /// ?????
        /// </summary>
        public string Manufacturer { get; set; }

        /// <summary>
        /// ????
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// ???????
        /// </summary>
        public string Protocol { get; set; }

        /// <summary>
        /// ???????
        /// </summary>
        public bool IsRecommended { get; set; }

        /// <summary>
        /// ????
        /// </summary>
        public bool IsAvailable { get; set; } = true;

        /// <summary>
        /// ??????
        /// </summary>
        public string StatusMessage { get; set; }

        /// <summary>
        /// ????
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DeviceSelectionItem()
        {
        }

        public DeviceSelectionItem(string deviceType, string displayName, string description = null)
        {
            DeviceType = deviceType;
            DisplayName = displayName;
            Description = description ?? $"Device type: {deviceType}";
        }
    }
}