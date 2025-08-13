using System.Collections.Generic;
using SupervisorApp.Core.Devices;
//┌─────────────────────────────────────┐
//│ 硬件设备 / 模拟设备(TestDevice100)    │
//│ Dictionary<uint, byte> _virtualRegisters │ ← **真正的数据源**
//└─────────────────┬───────────────────┘
//                  │
//                  │ ReadRegisterAsync/WriteRegisterAsync
//                  ▼
//┌─────────────────────────────────────┐
//│ RegisterItemViewModel               │
//│ private byte _currentValue          │ ← **UI层缓存 * *
//└─────────────────┬───────────────────┘
//                  │
//                  │ UpdateBitFields()
//                  ▼
//┌─────────────────────────────────────┐
//│ BitFieldItemViewModel               │
//│ private uint _currentValue          │ ← **位字段缓存**
//└─────────────────────────────────────┘
/*读取流程：
1.	UI 触发读取 → RegisterItemViewModel.ReadValueAsync()
2.	调用设备接口 → device.ReadRegisterAsync()
3.	从 _virtualRegisters[address] 读取实际值
4.	更新 RegisterItemViewModel._currentValue
5.	自动更新所有 BitFieldItemViewModel 的值
 * 
 * 写入流程：
1.	UI 触发写入 → RegisterItemViewModel.WriteValueAsync()
2.	调用设备接口 → device.WriteRegisterAsync()
3.	写入到 _virtualRegisters[address]
4.	更新 RegisterItemViewModel._currentValue
5.	自动更新所有相关的位字段值
**/

namespace SupervisorApp.Models
{
    /// <summary>
    /// TestDevice100的寄存器映射定义
    /// 包含各种类型的寄存器用于测试可视化功能
    /// 时间：2025-08-06 08:26:00 UTC
    /// 作者：StarkXH
    /// </summary>
    public static class TestDevice100RegisterMaps
    {
        public static IEnumerable<RegisterMap> GetRegisterMaps()
        {
            var registerMaps = new List<RegisterMap>();

            // GPIO相关寄存器
            registerMaps.AddRange(CreateGPIORegisters());

            // 传感器相关寄存器  
            registerMaps.AddRange(CreateSensorRegisters());

            // 电机控制相关寄存器
            registerMaps.AddRange(CreateMotorRegisters());

            // 系统状态相关寄存器
            registerMaps.AddRange(CreateSystemRegisters());

            // 计数器和测试寄存器
            registerMaps.AddRange(CreateTestRegisters());

            return registerMaps;
        }

        #region GPIO寄存器组

        private static IEnumerable<RegisterMap> CreateGPIORegisters()
        {
            // GPIO配置寄存器
            yield return new RegisterMap
            {
                Address = 0x1000,
                Name = "GPIO_CONFIG",
                Description = "GPIO引脚方向配置 (0=输入, 1=输出)",
                Type = RegisterType.Control,
                Access = RegisterAccess.ReadWrite,
                Size = 1,
                BitFields = new List<BitField>
                {
                    new BitField { Name = "PIN0_DIR", Description = "PIN0方向", BitPosition = 0, BitWidth = 1,
                        ValueMappings = new Dictionary<int, string> { {0, "输入"}, {1, "输出"} } },
                    new BitField { Name = "PIN1_DIR", Description = "PIN1方向", BitPosition = 1, BitWidth = 1,
                        ValueMappings = new Dictionary<int, string> { {0, "输入"}, {1, "输出"} } },
                    new BitField { Name = "PIN2_DIR", Description = "PIN2方向", BitPosition = 2, BitWidth = 1,
                        ValueMappings = new Dictionary<int, string> { {0, "输入"}, {1, "输出"} } },
                    new BitField { Name = "PIN3_DIR", Description = "PIN3方向", BitPosition = 3, BitWidth = 1,
                        ValueMappings = new Dictionary<int, string> { {0, "输入"}, {1, "输出"} } },
                    new BitField { Name = "PIN4_7_DIR", Description = "PIN4-7方向", BitPosition = 4, BitWidth = 4,
                        ValueMappings = new Dictionary<int, string> { {0, "全部输入"}, {15, "全部输出"} } }
                }
            };

            // GPIO输出寄存器
            yield return new RegisterMap
            {
                Address = 0x1001,
                Name = "GPIO_OUTPUT",
                Description = "GPIO输出值设置",
                Type = RegisterType.Control,
                Access = RegisterAccess.ReadWrite,
                Size = 1,
                BitFields = new List<BitField>
                {
                    new BitField { Name = "PIN0_OUT", Description = "PIN0输出", BitPosition = 0, BitWidth = 1,
                        ValueMappings = new Dictionary<int, string> { {0, "低电平"}, {1, "高电平"} } },
                    new BitField { Name = "PIN1_OUT", Description = "PIN1输出", BitPosition = 1, BitWidth = 1,
                        ValueMappings = new Dictionary<int, string> { {0, "低电平"}, {1, "高电平"} } },
                    new BitField { Name = "PIN2_OUT", Description = "PIN2输出", BitPosition = 2, BitWidth = 1,
                        ValueMappings = new Dictionary<int, string> { {0, "低电平"}, {1, "高电平"} } },
                    new BitField { Name = "PIN3_OUT", Description = "PIN3输出", BitPosition = 3, BitWidth = 1,
                        ValueMappings = new Dictionary<int, string> { {0, "低电平"}, {1, "高电平"} } }
                }
            };

            // GPIO输入寄存器
            yield return new RegisterMap
            {
                Address = 0x1002,
                Name = "GPIO_INPUT",
                Description = "GPIO输入值读取",
                Type = RegisterType.Status,
                Access = RegisterAccess.ReadOnly,
                Size = 1,
                BitFields = new List<BitField>
                {
                    new BitField { Name = "PIN0_IN", Description = "PIN0输入", BitPosition = 0, BitWidth = 1,
                        ValueMappings = new Dictionary<int, string> { {0, "低电平"}, {1, "高电平"} } },
                    new BitField { Name = "PIN1_IN", Description = "PIN1输入", BitPosition = 1, BitWidth = 1,
                        ValueMappings = new Dictionary<int, string> { {0, "低电平"}, {1, "高电平"} } },
                    new BitField { Name = "PIN2_IN", Description = "PIN2输入", BitPosition = 2, BitWidth = 1,
                        ValueMappings = new Dictionary<int, string> { {0, "低电平"}, {1, "高电平"} } },
                    new BitField { Name = "PIN3_IN", Description = "PIN3输入", BitPosition = 3, BitWidth = 1,
                        ValueMappings = new Dictionary<int, string> { {0, "低电平"}, {1, "高电平"} } }
                }
            };
        }

        #endregion

        #region 传感器寄存器组

        private static IEnumerable<RegisterMap> CreateSensorRegisters()
        {
            // SHT30状态寄存器
            yield return new RegisterMap
            {
                Address = 0x1010,
                Name = "SHT30_STATUS",
                Description = "SHT30传感器状态",
                Type = RegisterType.Status,
                Access = RegisterAccess.ReadOnly,
                Size = 1,
                BitFields = new List<BitField>
                {
                    new BitField { Name = "TEMP_ALERT", Description = "温度报警", BitPosition = 0, BitWidth = 1,
                        ValueMappings = new Dictionary<int, string> { {0, "正常"}, {1, "温度异常"} } },
                    new BitField { Name = "HUM_ALERT", Description = "湿度报警", BitPosition = 1, BitWidth = 1,
                        ValueMappings = new Dictionary<int, string> { {0, "正常"}, {1, "湿度异常"} } },
                    new BitField { Name = "HEATER_ON", Description = "加热器状态", BitPosition = 2, BitWidth = 1,
                        ValueMappings = new Dictionary<int, string> { {0, "关闭"}, {1, "开启"} } },
                    new BitField { Name = "DATA_READY", Description = "数据就绪", BitPosition = 3, BitWidth = 1,
                        ValueMappings = new Dictionary<int, string> { {0, "未就绪"}, {1, "数据就绪"} } }
                }
            };

            // 温度数据高字节
            yield return new RegisterMap
            {
                Address = 0x1011,
                Name = "SHT30_TEMP_HIGH",
                Description = "温度数据高字节",
                Type = RegisterType.Data,
                Access = RegisterAccess.ReadOnly,
                Size = 1,
                BitFields = new List<BitField>
                {
                    new BitField { Name = "TEMP_HIGH", Description = "温度高8位", BitPosition = 0, BitWidth = 8 }
                }
            };

            // 温度数据低字节
            yield return new RegisterMap
            {
                Address = 0x1012,
                Name = "SHT30_TEMP_LOW",
                Description = "温度数据低字节",
                Type = RegisterType.Data,
                Access = RegisterAccess.ReadOnly,
                Size = 1,
                BitFields = new List<BitField>
                {
                    new BitField { Name = "TEMP_LOW", Description = "温度低8位", BitPosition = 0, BitWidth = 8 }
                }
            };

            // 湿度数据高字节
            yield return new RegisterMap
            {
                Address = 0x1013,
                Name = "SHT30_HUM_HIGH",
                Description = "湿度数据高字节",
                Type = RegisterType.Data,
                Access = RegisterAccess.ReadOnly,
                Size = 1,
                BitFields = new List<BitField>
                {
                    new BitField { Name = "HUM_HIGH", Description = "湿度高8位", BitPosition = 0, BitWidth = 8 }
                }
            };
        }

        #endregion

        #region 电机控制寄存器组

        private static IEnumerable<RegisterMap> CreateMotorRegisters()
        {
            // 电机控制寄存器
            yield return new RegisterMap
            {
                Address = 0x1020,
                Name = "MOTOR_CONTROL",
                Description = "电机控制",
                Type = RegisterType.Control,
                Access = RegisterAccess.ReadWrite,
                Size = 1,
                BitFields = new List<BitField>
                {
                    new BitField { Name = "ENABLE", Description = "电机使能", BitPosition = 0, BitWidth = 1,
                        ValueMappings = new Dictionary<int, string> { {0, "禁用"}, {1, "使能"} } },
                    new BitField { Name = "DIRECTION", Description = "旋转方向", BitPosition = 1, BitWidth = 1,
                        ValueMappings = new Dictionary<int, string> { {0, "正转"}, {1, "反转"} } },
                    new BitField { Name = "BRAKE", Description = "制动", BitPosition = 2, BitWidth = 1,
                        ValueMappings = new Dictionary<int, string> { {0, "释放"}, {1, "制动"} } },
                    new BitField { Name = "MODE", Description = "控制模式", BitPosition = 4, BitWidth = 2,
                        ValueMappings = new Dictionary<int, string> {
                            {0, "位置模式"}, {1, "速度模式"}, {2, "力矩模式"}, {3, "保留"} } }
                }
            };

            // 电机速度设置
            yield return new RegisterMap
            {
                Address = 0x1021,
                Name = "MOTOR_SPEED",
                Description = "电机速度设置 (0-255)",
                Type = RegisterType.Control,
                Access = RegisterAccess.ReadWrite,
                Size = 1,
                BitFields = new List<BitField>
                {
                    new BitField { Name = "SPEED", Description = "速度值", BitPosition = 0, BitWidth = 8 }
                }
            };

            // 电机状态
            yield return new RegisterMap
            {
                Address = 0x1022,
                Name = "MOTOR_STATUS",
                Description = "电机状态",
                Type = RegisterType.Status,
                Access = RegisterAccess.ReadOnly,
                Size = 1,
                BitFields = new List<BitField>
                {
                    new BitField { Name = "RUNNING", Description = "运行状态", BitPosition = 0, BitWidth = 1,
                        ValueMappings = new Dictionary<int, string> { {0, "停止"}, {1, "运行"} } },
                    new BitField { Name = "FAULT", Description = "故障状态", BitPosition = 1, BitWidth = 1,
                        ValueMappings = new Dictionary<int, string> { {0, "正常"}, {1, "故障"} } },
                    new BitField { Name = "OVER_TEMP", Description = "过温保护", BitPosition = 2, BitWidth = 1,
                        ValueMappings = new Dictionary<int, string> { {0, "正常"}, {1, "过温"} } },
                    new BitField { Name = "OVER_CURRENT", Description = "过流保护", BitPosition = 3, BitWidth = 1,
                        ValueMappings = new Dictionary<int, string> { {0, "正常"}, {1, "过流"} } }
                }
            };
        }

        #endregion

        #region 系统寄存器组

        private static IEnumerable<RegisterMap> CreateSystemRegisters()
        {
            // 系统状态寄存器
            yield return new RegisterMap
            {
                Address = 0x1030,
                Name = "SYSTEM_STATUS",
                Description = "系统状态",
                Type = RegisterType.Status,
                Access = RegisterAccess.ReadOnly,
                Size = 1,
                BitFields = new List<BitField>
                {
                    new BitField { Name = "POWER_GOOD", Description = "电源状态", BitPosition = 0, BitWidth = 1,
                        ValueMappings = new Dictionary<int, string> { {0, "电源异常"}, {1, "电源正常"} } },
                    new BitField { Name = "WATCHDOG", Description = "看门狗状态", BitPosition = 1, BitWidth = 1,
                        ValueMappings = new Dictionary<int, string> { {0, "正常"}, {1, "触发"} } },
                    new BitField { Name = "COMM_ERROR", Description = "通信错误", BitPosition = 2, BitWidth = 1,
                        ValueMappings = new Dictionary<int, string> { {0, "正常"}, {1, "通信错误"} } },
                    new BitField { Name = "INIT_DONE", Description = "初始化完成", BitPosition = 3, BitWidth = 1,
                        ValueMappings = new Dictionary<int, string> { {0, "未完成"}, {1, "已完成"} } }
                }
            };

            // 系统配置寄存器
            yield return new RegisterMap
            {
                Address = 0x1031,
                Name = "SYSTEM_CONFIG",
                Description = "系统配置",
                Type = RegisterType.Control,
                Access = RegisterAccess.ReadWrite,
                Size = 1,
                BitFields = new List<BitField>
                {
                    new BitField { Name = "DEBUG_MODE", Description = "调试模式", BitPosition = 0, BitWidth = 1,
                        ValueMappings = new Dictionary<int, string> { {0, "关闭"}, {1, "开启"} } },
                    new BitField { Name = "AUTO_SAVE", Description = "自动保存", BitPosition = 1, BitWidth = 1,
                        ValueMappings = new Dictionary<int, string> { {0, "禁用"}, {1, "启用"} } },
                    new BitField { Name = "BAUD_RATE", Description = "波特率", BitPosition = 4, BitWidth = 3,
                        ValueMappings = new Dictionary<int, string> {
                            {0, "9600"}, {1, "19200"}, {2, "38400"}, {3, "57600"}, {4, "115200"} } }
                }
            };
        }

        #endregion

        #region 测试寄存器组

        private static IEnumerable<RegisterMap> CreateTestRegisters()
        {
            // 计数器寄存器
            yield return new RegisterMap
            {
                Address = 0x1040,
                Name = "COUNTER",
                Description = "8位计数器 (自动递增)",
                Type = RegisterType.Data,
                Access = RegisterAccess.ReadOnly,
                Size = 1,
                BitFields = new List<BitField>
                {
                    new BitField { Name = "COUNT", Description = "计数值", BitPosition = 0, BitWidth = 8 }
                }
            };

            // 测试数据寄存器
            yield return new RegisterMap
            {
                Address = 0x1041,
                Name = "TEST_DATA",
                Description = "测试数据寄存器",
                Type = RegisterType.Data,
                Access = RegisterAccess.ReadWrite,
                Size = 1,
                BitFields = new List<BitField>
                {
                    new BitField { Name = "LOW_NIBBLE", Description = "低4位", BitPosition = 0, BitWidth = 4 },
                    new BitField { Name = "HIGH_NIBBLE", Description = "高4位", BitPosition = 4, BitWidth = 4 }
                }
            };

            // 标志寄存器
            yield return new RegisterMap
            {
                Address = 0x1042,
                Name = "FLAGS",
                Description = "各种标志位",
                Type = RegisterType.Status,
                Access = RegisterAccess.ReadWrite,
                Size = 1,
                BitFields = new List<BitField>
                {
                    new BitField { Name = "FLAG_A", Description = "标志A", BitPosition = 0, BitWidth = 1,
                        ValueMappings = new Dictionary<int, string> { {0, "清除"}, {1, "设置"} } },
                    new BitField { Name = "FLAG_B", Description = "标志B", BitPosition = 1, BitWidth = 1,
                        ValueMappings = new Dictionary<int, string> { {0, "清除"}, {1, "设置"} } },
                    new BitField { Name = "FLAG_C", Description = "标志C", BitPosition = 2, BitWidth = 1,
                        ValueMappings = new Dictionary<int, string> { {0, "清除"}, {1, "设置"} } },
                    new BitField { Name = "PRIORITY", Description = "优先级", BitPosition = 4, BitWidth = 2,
                        ValueMappings = new Dictionary<int, string> {
                            {0, "低"}, {1, "中"}, {2, "高"}, {3, "紧急"} } }
                }
            };
        }

        #endregion
    }
}