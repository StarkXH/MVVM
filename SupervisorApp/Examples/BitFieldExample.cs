using SupervisorApp.Core.Devices;
using System;
using System.Collections.Generic;

/* BitFieldUtils.cs中各个方法的具体示例，帮助理解方法 */

namespace SupervisorApp.Examples
{
    // 示例：读取GPIO配置寄存器，解析GPIO0-GPIO3的模式设置
    public class GPIOConfigExample
    {
        public void DemonstrateExtractBitField()
        {
            // 假设从设备读取到的寄存器值 (4字节)
            // 0x000000A5 = 0000 0000 0000 0000 0000 0000 1010 0101
            // 二进制解析：
            // GPIO3: 10 (复用功能)
            // GPIO2: 01 (通用输出) 
            // GPIO1: 01 (通用输出)
            // GPIO0: 01 (通用输出)
            byte[] registerData = { 0xA5, 0x00, 0x00, 0x00 }; // 小端序

            // 定义GPIO0的位字段 (位0-1)
            var gpio0BitField = new BitField
            {
                Name = "GPIO0_MODE",
                BitPosition = 0,
                BitWidth = 2,
                Description = "GPIO0引脚模式",
                ValueMappings = new Dictionary<int, string>
            {
                { 0, "输入模式" },
                { 1, "通用输出模式" },
                { 2, "复用功能模式" },
                { 3, "模拟模式" }
            }
            };

            // 提取GPIO0的模式值
            uint gpio0Mode = BitFieldUtils.ExtractBitField(registerData, gpio0BitField);
            Console.WriteLine($"GPIO0模式: {gpio0Mode} ({gpio0BitField.ValueMappings[(int)gpio0Mode]})");
            // 输出: GPIO0模式: 1 (通用输出模式)

            // 定义GPIO1的位字段 (位2-3)
            var gpio1BitField = new BitField
            {
                Name = "GPIO1_MODE",
                BitPosition = 2,
                BitWidth = 2,
                Description = "GPIO1引脚模式",
                ValueMappings = gpio0BitField.ValueMappings // 相同的映射
            };

            uint gpio1Mode = BitFieldUtils.ExtractBitField(registerData, gpio1BitField);
            Console.WriteLine($"GPIO1模式: {gpio1Mode} ({gpio1BitField.ValueMappings[(int)gpio1Mode]})");
            // 输出: GPIO1模式: 1 (通用输出模式)

            // 定义GPIO2的位字段 (位4-5)  
            var gpio2BitField = new BitField
            {
                Name = "GPIO2_MODE",
                BitPosition = 4,
                BitWidth = 2,
                Description = "GPIO2引脚模式",
                ValueMappings = gpio0BitField.ValueMappings
            };

            uint gpio2Mode = BitFieldUtils.ExtractBitField(registerData, gpio2BitField);
            Console.WriteLine($"GPIO2模式: {gpio2Mode} ({gpio2BitField.ValueMappings[(int)gpio2Mode]})");
            // 输出: GPIO2模式: 1 (通用输出模式)

            // 定义GPIO3的位字段 (位6-7)
            var gpio3BitField = new BitField
            {
                Name = "GPIO3_MODE",
                BitPosition = 6,
                BitWidth = 2,
                Description = "GPIO3引脚模式",
                ValueMappings = gpio0BitField.ValueMappings
            };

            uint gpio3Mode = BitFieldUtils.ExtractBitField(registerData, gpio3BitField);
            Console.WriteLine($"GPIO3模式: {gpio3Mode} ({gpio3BitField.ValueMappings[(int)gpio3Mode]})");
            // 输出: GPIO3模式: 2 (复用功能模式)
        }
    }
    public class GPIOSetExample
    {
        public void DemonstrateSetBitField()
        {
            // 当前寄存器值: 0x000000A5
            byte[] currentData = { 0xA5, 0x00, 0x00, 0x00 };

            Console.WriteLine($"修改前: 0x{BitConverter.ToUInt32(currentData, 0):X8}");
            // 输出: 修改前: 0x000000A5

            // 定义GPIO1的位字段 (位2-3)
            var gpio1BitField = new BitField
            {
                Name = "GPIO1_MODE",
                BitPosition = 2,
                BitWidth = 2,
                Description = "GPIO1引脚模式"
            };

            // 将GPIO1从"通用输出模式"(01)改为"复用功能模式"(10)
            byte[] modifiedData = BitFieldUtils.SetBitField(currentData, gpio1BitField, 2);

            Console.WriteLine($"修改后: 0x{BitConverter.ToUInt32(modifiedData, 0):X8}");
            // 输出: 修改后: 0x000000A9

            // 验证修改结果
            // 原值: 1010 0101 (GPIO3=10, GPIO2=01, GPIO1=01, GPIO0=01)
            // 新值: 1010 1001 (GPIO3=10, GPIO2=01, GPIO1=10, GPIO0=01)
            //                                     ^^修改的位

            uint newGpio1Mode = BitFieldUtils.ExtractBitField(modifiedData, gpio1BitField);
            Console.WriteLine($"修改后GPIO1模式: {newGpio1Mode} (复用功能模式)");
        }
    }
    public class GPIOParseAllExample
    {
        public void DemonstrateParseAllBitFields()
        {
            // 创建GPIO配置寄存器映射
            var gpioModeRegister = new RegisterMap
            {
                Address = 0x40020000,
                Name = "GPIO_MODER",
                Type = RegisterType.Configuration,
                Size = 4,
                Access = RegisterAccess.ReadWrite,
                Description = "GPIO端口模式寄存器",
                BitFields = CreateGPIOBitFields()
            };

            // 从设备读取的数据: 0x000002A5
            // 二进制: 0000 0010 1010 0101
            // GPIO3=10(复用), GPIO2=01(输出), GPIO1=10(复用), GPIO0=01(输出)
            byte[] registerData = { 0xA5, 0x02, 0x00, 0x00 };

            // 解析所有位字段
            var bitFieldValues = BitFieldUtils.ParseAllBitFields(registerData, gpioModeRegister);

            Console.WriteLine("GPIO配置寄存器解析结果:");
            foreach (var kvp in bitFieldValues)
            {
                var bf = kvp.Value;
                Console.WriteLine($"{kvp.Key}: {bf.RawValue} ({bf.AsBinary}) - {bf.Description}");
            }

            // 输出结果:
            // GPIO0_MODE: 1 (01) - 通用输出模式
            // GPIO1_MODE: 2 (10) - 复用功能模式  
            // GPIO2_MODE: 1 (01) - 通用输出模式
            // GPIO3_MODE: 2 (10) - 复用功能模式
        }

        private List<BitField> CreateGPIOBitFields()
        {
            var modeMapping = new Dictionary<int, string>
        {
            { 0, "输入模式" },
            { 1, "通用输出模式" },
            { 2, "复用功能模式" },
            { 3, "模拟模式" }
        };

            return new List<BitField>
        {
            new BitField { Name = "GPIO0_MODE", BitPosition = 0, BitWidth = 2, ValueMappings = modeMapping },
            new BitField { Name = "GPIO1_MODE", BitPosition = 2, BitWidth = 2, ValueMappings = modeMapping },
            new BitField { Name = "GPIO2_MODE", BitPosition = 4, BitWidth = 2, ValueMappings = modeMapping },
            new BitField { Name = "GPIO3_MODE", BitPosition = 6, BitWidth = 2, ValueMappings = modeMapping }
        };
        }
    }
    public class GPIOBuildExample
    {
        public void DemonstrateBuildDataWithBitFields()
        {
            // 原始数据：所有GPIO都是输入模式 (0x00000000)
            byte[] originalData = { 0x00, 0x00, 0x00, 0x00 };

            // 定义所有GPIO的位字段
            var bitFields = CreateGPIOBitFields();

            // 同时配置多个GPIO:
            // GPIO0 -> 通用输出模式 (1)
            // GPIO1 -> 复用功能模式 (2)  
            // GPIO2 -> 通用输出模式 (1)
            // GPIO3 -> 模拟模式 (3)
            var updates = new List<(BitField BitField, uint Value)>
        {
            (bitFields[0], 1), // GPIO0 = 输出模式
            (bitFields[1], 2), // GPIO1 = 复用功能 
            (bitFields[2], 1), // GPIO2 = 输出模式
            (bitFields[3], 3)  // GPIO3 = 模拟模式
        };

            // 构建新的寄存器数据
            byte[] newData = BitFieldUtils.BuildDataWithBitFields(originalData, updates);

            Console.WriteLine($"原始数据: 0x{BitConverter.ToUInt32(originalData, 0):X8}");
            Console.WriteLine($"新数据:   0x{BitConverter.ToUInt32(newData, 0):X8}");

            // 输出:
            // 原始数据: 0x00000000  
            // 新数据:   0x000000DD

            // 二进制验证: 1101 1101
            // GPIO3=11(模拟), GPIO2=01(输出), GPIO1=10(复用), GPIO0=01(输出) ✓

            // 验证每个GPIO的配置
            foreach (var (bitField, expectedValue) in updates)
            {
                uint actualValue = BitFieldUtils.ExtractBitField(newData, bitField);
                Console.WriteLine($"{bitField.Name}: 期望={expectedValue}, 实际={actualValue} " +
                                $"{(expectedValue == actualValue ? "✓" : "✗")}");
            }
        }

        private List<BitField> CreateGPIOBitFields()
        {
            return new List<BitField>
        {
            new BitField { Name = "GPIO0_MODE", BitPosition = 0, BitWidth = 2 },
            new BitField { Name = "GPIO1_MODE", BitPosition = 2, BitWidth = 2 },
            new BitField { Name = "GPIO2_MODE", BitPosition = 4, BitWidth = 2 },
            new BitField { Name = "GPIO3_MODE", BitPosition = 6, BitWidth = 2 }
        };
        }
    }
    public class BitOperationsExample
    {
        public void DemonstrateBitOperations()
        {
            // CreateBitMask示例
            Console.WriteLine("=== CreateBitMask示例 ===");

            // 创建GPIO1的掩码 (位2-3)
            uint gpio1Mask = BitFieldUtils.CreateBitMask(2, 2);
            Console.WriteLine($"GPIO1掩码: 0x{gpio1Mask:X8} (二进制: {Convert.ToString(gpio1Mask, 2).PadLeft(8, '0')})");
            // 输出: GPIO1掩码: 0x0000000C (二进制: 00001100)

            // 创建GPIO0的掩码 (位0-1)  
            uint gpio0Mask = BitFieldUtils.CreateBitMask(0, 2);
            Console.WriteLine($"GPIO0掩码: 0x{gpio0Mask:X8} (二进制: {Convert.ToString(gpio0Mask, 2).PadLeft(8, '0')})");
            // 输出: GPIO0掩码: 0x00000003 (二进制: 00000011)

            Console.WriteLine("\n=== IsBitSet示例 ===");

            // 检查中断使能位
            uint statusRegister = 0x85; // 1000 0101
            Console.WriteLine($"状态寄存器: 0x{statusRegister:X2} (二进制: {Convert.ToString(statusRegister, 2).PadLeft(8, '0')})");

            Console.WriteLine($"位0设置? {BitFieldUtils.IsBitSet(statusRegister, 0)}"); // True
            Console.WriteLine($"位1设置? {BitFieldUtils.IsBitSet(statusRegister, 1)}"); // False  
            Console.WriteLine($"位2设置? {BitFieldUtils.IsBitSet(statusRegister, 2)}"); // True
            Console.WriteLine($"位7设置? {BitFieldUtils.IsBitSet(statusRegister, 7)}"); // True

            Console.WriteLine("\n=== SetBit示例 ===");

            uint controlRegister = 0x00; // 初始全为0

            // 设置使能位 (位0)
            controlRegister = BitFieldUtils.SetBit(controlRegister, 0, true);
            Console.WriteLine($"设置位0后: 0x{controlRegister:X2} (二进制: {Convert.ToString(controlRegister, 2).PadLeft(8, '0')})");

            // 设置中断使能位 (位7)
            controlRegister = BitFieldUtils.SetBit(controlRegister, 7, true);
            Console.WriteLine($"设置位7后: 0x{controlRegister:X2} (二进制: {Convert.ToString(controlRegister, 2).PadLeft(8, '0')})");

            // 清除使能位 (位0)
            controlRegister = BitFieldUtils.SetBit(controlRegister, 0, false);
            Console.WriteLine($"清除位0后: 0x{controlRegister:X2} (二进制: {Convert.ToString(controlRegister, 2).PadLeft(8, '0')})");
        }
    }
    public class SHT30BitFieldExample
    {
        public void DemonstrateSHT30StatusRegister()
        {
            // SHT30状态寄存器 (16位)
            // 从传感器读取的实际数据: 0x8001
            // 二进制: 1000 0000 0000 0001
            byte[] sht30StatusData = { 0x01, 0x80 }; // 小端序

            var sht30StatusRegister = new RegisterMap
            {
                Address = 0xF32D,
                Name = "SHT30_STATUS",
                Type = RegisterType.Status,
                Size = 2,
                BitFields = new List<BitField>
            {
                new BitField
                {
                    Name = "TEMP_ALERT",
                    BitPosition = 0,
                    BitWidth = 1,
                    ValueMappings = new Dictionary<int, string> { {0, "正常"}, {1, "温度报警"} }
                },
                new BitField
                {
                    Name = "RH_ALERT",
                    BitPosition = 1,
                    BitWidth = 1,
                    ValueMappings = new Dictionary<int, string> { {0, "正常"}, {1, "湿度报警"} }
                },
                new BitField
                {
                    Name = "HEATER_STATUS",
                    BitPosition = 2,
                    BitWidth = 1,
                    ValueMappings = new Dictionary<int, string> { {0, "关闭"}, {1, "开启"} }
                },
                new BitField
                {
                    Name = "SYSTEM_RESET",
                    BitPosition = 7,
                    BitWidth = 1,
                    ValueMappings = new Dictionary<int, string> { {0, "无复位"}, {1, "检测到复位"} }
                },
                new BitField
                {
                    Name = "COMMAND_STATUS",
                    BitPosition = 10,
                    BitWidth = 1,
                    ValueMappings = new Dictionary<int, string> { {0, "空闲"}, {1, "命令执行中"} }
                }
            }
            };

            Console.WriteLine("=== SHT30状态寄存器解析 ===");
            Console.WriteLine($"原始数据: 0x{BitConverter.ToUInt16(sht30StatusData, 0):X4}");

            var statusBitFields = BitFieldUtils.ParseAllBitFields(sht30StatusData, sht30StatusRegister);

            foreach (var kvp in statusBitFields)
            {
                var bf = kvp.Value;
                Console.WriteLine($"{kvp.Key}: {bf.Description}");
            }

            // 输出结果:
            // TEMP_ALERT: 温度报警      (位0=1)
            // RH_ALERT: 正常           (位1=0)  
            // HEATER_STATUS: 关闭      (位2=0)
            // SYSTEM_RESET: 无复位     (位7=0)
            // COMMAND_STATUS: 空闲     (位10=0)

            // 检查是否有任何报警
            bool hasAlert = statusBitFields["TEMP_ALERT"].AsBool || statusBitFields["RH_ALERT"].AsBool;
            Console.WriteLine($"\n传感器报警状态: {(hasAlert ? "有报警" : "正常")}");
        }
    }
    public class MotorControllerExample
    {
        public void DemonstrateMotorControl()
        {
            Console.WriteLine("=== 电机控制器配置示例 ===");

            // 电机控制寄存器定义
            var motorControlRegister = new RegisterMap
            {
                Address = 0x1000,
                Name = "MOTOR_CTRL",
                Type = RegisterType.Control,
                Size = 2,
                BitFields = new List<BitField>
            {
                new BitField
                {
                    Name = "ENABLE",
                    BitPosition = 0,
                    BitWidth = 1,
                    ValueMappings = new Dictionary<int, string> { {0, "禁用"}, {1, "使能"} }
                },
                new BitField
                {
                    Name = "DIRECTION",
                    BitPosition = 1,
                    BitWidth = 1,
                    ValueMappings = new Dictionary<int, string> { {0, "正转"}, {1, "反转"} }
                },
                new BitField
                {
                    Name = "SPEED",
                    BitPosition = 2,
                    BitWidth = 4,
                    Description = "速度等级 (0-15)"
                },
                new BitField
                {
                    Name = "MODE",
                    BitPosition = 6,
                    BitWidth = 2,
                    ValueMappings = new Dictionary<int, string>
                    {
                        {0, "停止"}, {1, "恒速"}, {2, "加速"}, {3, "减速"}
                    }
                }
            }
            };

            // 1. 配置电机：使能、正转、速度10、恒速模式
            var motorConfig = new List<(BitField BitField, uint Value)>
        {
            (motorControlRegister.BitFields[0], 1),  // ENABLE = 1
            (motorControlRegister.BitFields[1], 0),  // DIRECTION = 0 (正转)
            (motorControlRegister.BitFields[2], 10), // SPEED = 10  
            (motorControlRegister.BitFields[3], 1)   // MODE = 1 (恒速)
        };

            byte[] configData = BitFieldUtils.BuildDataWithBitFields(new byte[2], motorConfig);

            Console.WriteLine($"电机配置数据: 0x{BitConverter.ToUInt16(configData, 0):X4}");
            // 计算结果: 0x006A
            // 二进制: 0000 0000 0110 1010
            // MODE=01(恒速), SPEED=1010(10), DIRECTION=0(正转), ENABLE=1(使能)

            // 2. 验证配置
            var configResult = BitFieldUtils.ParseAllBitFields(configData, motorControlRegister);
            Console.WriteLine("\n电机配置验证:");
            foreach (var kvp in configResult)
            {
                var bf = kvp.Value;
                if (bf.BitField.ValueMappings?.ContainsKey((int)bf.RawValue) == true)
                {
                    Console.WriteLine($"{kvp.Key}: {bf.Description}");
                }
                else
                {
                    Console.WriteLine($"{kvp.Key}: {bf.RawValue}");
                }
            }

            // 3. 动态修改：改为反转方向
            var directionBitField = motorControlRegister.BitFields[1];
            byte[] modifiedData = BitFieldUtils.SetBitField(configData, directionBitField, 1);

            Console.WriteLine($"\n修改方向后: 0x{BitConverter.ToUInt16(modifiedData, 0):X4}");

            uint newDirection = BitFieldUtils.ExtractBitField(modifiedData, directionBitField);
            Console.WriteLine($"新方向: {directionBitField.ValueMappings[(int)newDirection]}");
        }
    }
}
