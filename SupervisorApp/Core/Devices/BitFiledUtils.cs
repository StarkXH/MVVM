using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace SupervisorApp.Core.Devices
{
    /// <summary>
    /// 位字段操作工具类
    /// 时间：2025-08-05 02:27:00 UTC
    /// 作者：StarkXH
    /// </summary>
    public static class BitFieldUtils
    {
        #region 数据操作

        /// <summary>
        /// 从原始数据中提取位字段值
        /// </summary>
        /// <param name="rawData">原始数据</param>
        /// <param name="bitField">位字段定义</param>
        /// <returns>提取的值</returns>
        public static uint ExtractBitField(byte[] rawData, BitField bitField)
        {
            if (rawData == null || rawData.Length == 0)
                throw new ArgumentException("原始数据不能为空");

            // 将字节数组转换为整数（支持多字节寄存器）
            ulong value = 0;
            for (int i = 0; i < Math.Min(rawData.Length, 8); i++)
            {
                value |= (ulong)rawData[i] << (i * 8);
            }

            // 创建掩码
            ulong mask = (1UL << bitField.BitWidth) - 1;

            // 提取位字段值
            return (uint)((value >> bitField.BitPosition) & mask);
        }

        /// <summary>
        /// 将位字段值设置到原始数据中
        /// </summary>
        /// <param name="rawData">原始数据</param>
        /// <param name="bitField">位字段定义</param>
        /// <param name="value">要设置的值</param>
        /// <returns>修改后的数据</returns>
        public static byte[] SetBitField(byte[] rawData, BitField bitField, uint value)
        {
            if (rawData == null)
                throw new ArgumentException("原始数据不能为空");

            var result = new byte[rawData.Length];
            Array.Copy(rawData, result, rawData.Length);

            // 检查值是否超出位字段范围
            uint maxValue = (1U << bitField.BitWidth) - 1;
            if (value > maxValue)
                throw new ArgumentException($"值 {value} 超出位字段最大值 {maxValue}");

            // 将字节数组转换为整数
            ulong originalValue = 0;
            for (int i = 0; i < Math.Min(result.Length, 8); i++)
            {
                originalValue |= (ulong)result[i] << (i * 8);
            }

            // 清除目标位字段
            ulong clearMask = (1UL << bitField.BitWidth) - 1;
            clearMask = ~(clearMask << bitField.BitPosition);
            originalValue &= clearMask;

            // 设置新值
            ulong newValue = (ulong)value << bitField.BitPosition;
            originalValue |= newValue;

            // 将结果转换回字节数组
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = (byte)((originalValue >> (i * 8)) & 0xFF);
            }

            return result;
        }

        /// <summary>
        /// 解析寄存器中的所有位字段
        /// </summary>
        /// <param name="rawData">原始数据</param>
        /// <param name="registerMap">寄存器映射</param>
        /// <returns>位字段值字典</returns>
        public static Dictionary<string, BitFieldValue> ParseAllBitFields(byte[] rawData, RegisterMap registerMap)
        {
            var result = new Dictionary<string, BitFieldValue>();

            foreach (var bitField in registerMap.BitFields)
            {
                var value = ExtractBitField(rawData, bitField);

                result[bitField.Name] = new BitFieldValue
                {
                    BitField = bitField,
                    RawValue = value,
                    Description = bitField.ValueMappings.GetValueOrDefault((int)value, $"未知值: {value}"),
                    Timestamp = DateTime.Now
                };
            }

            return result;
        }

        /// <summary>
        /// 构建包含多个位字段修改的数据
        /// </summary>
        /// <param name="originalData">原始数据</param>
        /// <param name="bitFieldUpdates">位字段更新列表</param>
        /// <returns>修改后的数据</returns>
        public static byte[] BuildDataWithBitFields(byte[] originalData, IEnumerable<(BitField BitField, uint Value)> bitFieldUpdates)
        {
            var result = new byte[originalData.Length];
            Array.Copy(originalData, result, originalData.Length);

            foreach (var (bitField, value) in bitFieldUpdates)
            {
                result = SetBitField(result, bitField, value);
            }

            return result;
        }

        /// <summary>
        /// 创建位掩码
        /// </summary>
        /// <param name="bitPosition">位位置</param>
        /// <param name="bitWidth">位宽度</param>
        /// <returns>位掩码</returns>
        public static uint CreateBitMask(int bitPosition, int bitWidth)
        {
            if (bitWidth <= 0 || bitWidth > 32)
                throw new ArgumentException("位宽度必须在1-32之间");

            if (bitPosition < 0 || bitPosition > 31)
                throw new ArgumentException("位位置必须在0-31之间");

            uint mask = (1U << bitWidth) - 1;
            return mask << bitPosition;
        }

        /// <summary>
        /// 检查特定位是否设置
        /// </summary>
        /// <param name="value">要检查的值</param>
        /// <param name="bitPosition">位位置</param>
        /// <returns>位是否设置</returns>
        public static bool IsBitSet(uint value, int bitPosition)
        {
            return (value & (1U << bitPosition)) != 0;
        }

        /// <summary>
        /// 设置特定位
        /// </summary>
        /// <param name="value">原始值</param>
        /// <param name="bitPosition">位位置</param>
        /// <param name="setBit">是否设置位</param>
        /// <returns>修改后的值</returns>
        public static uint SetBit(uint value, int bitPosition, bool setBit)
        {
            if (setBit)
                return value | (1U << bitPosition);
            else
                return value & ~(1U << bitPosition);
        }

        #endregion

        #region Excel导入导出功能

        /// <summary>
        /// 将设备的所有寄存器位字段导出到Excel文件
        /// </summary>
        /// <param name="device">设备对象</param>
        /// <param name="filePath">Excel文件路径</param>
        /// <param name="includeCurrentValues">是否包含当前值</param>
        /// <returns>导出是否成功</returns>
        public static async Task<bool> SaveRegisterMapToExcelAsync(IDevice device, string filePath, bool includeCurrentValues = true)
        {
            try
            {
                var registerMaps = device.GetRegisterMaps().ToList();
                if (!registerMaps.Any())
                {
                    throw new InvalidOperationException("设备没有寄存器映射");
                }

                // 收集当前值（如果需要）
                var currentValues = new Dictionary<uint, byte>();
                if (includeCurrentValues)
                {
                    currentValues = await CollectCurrentRegisterValuesAsync(device, registerMaps);
                }

                // 创建Excel工作簿
                using var document = SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook);

                var workbookPart = document.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();

                var sheets = workbookPart.Workbook.AppendChild(new Sheets());

                // 创建样式表
                var stylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
                stylesPart.Stylesheet = CreateStylesheet();

                // 创建主工作表：寄存器概览
                CreateRegisterOverviewSheet(workbookPart, sheets, registerMaps, currentValues);

                // 为每个寄存器类型创建详细工作表
                var registerGroups = registerMaps.GroupBy(rm => rm.Type);
                foreach (var group in registerGroups)
                {
                    CreateRegisterDetailSheet(workbookPart, sheets, group.Key.ToString(), group.ToList(), currentValues);
                }

                // 创建位字段配置工作表
                CreateBitFieldConfigSheet(workbookPart, sheets, registerMaps, currentValues);

                // 保存工作簿
                workbookPart.Workbook.Save();

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存Excel文件失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 从Excel文件加载寄存器配置并应用到设备
        /// </summary>
        /// <param name="device">设备对象</param>
        /// <param name="filePath">Excel文件路径</param>
        /// <returns>加载结果</returns>
        public static async Task<ExcelLoadResult> LoadRegisterMapFromExcelAsync(IDevice device, string filePath)
        {
            var result = new ExcelLoadResult();

            try
            {
                if (!File.Exists(filePath))
                {
                    result.Success = false;
                    result.ErrorMessage = "Excel文件不存在";
                    return result;
                }

                using var document = SpreadsheetDocument.Open(filePath, false);
                var workbookPart = document.WorkbookPart;

                // 查找位字段配置工作表
                var configSheet = FindWorksheet(workbookPart, "BitField_Config");
                if (configSheet == null)
                {
                    result.Success = false;
                    result.ErrorMessage = "未找到位字段配置工作表";
                    return result;
                }

                // 解析配置数据
                var configData = ParseBitFieldConfigSheet(configSheet, workbookPart);

                // 验证配置数据
                var validationResult = ValidateConfigData(device, configData);
                result.ValidationErrors.AddRange(validationResult);

                if (validationResult.Any(v => v.IsError))
                {
                    result.Success = false;
                    result.ErrorMessage = "配置数据验证失败";
                    return result;
                }

                // 应用配置到设备
                var applyResult = await ApplyConfigToDeviceAsync(device, configData);
                result.Success = applyResult.Success;
                result.AppliedCount = applyResult.AppliedCount;
                result.ErrorMessage = applyResult.ErrorMessage;

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"加载Excel文件失败: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// 创建Excel模板文件
        /// </summary>
        /// <param name="device">设备对象</param>
        /// <param name="templatePath">模板文件路径</param>
        /// <returns>创建是否成功</returns>
        public static async Task<bool> CreateExcelTemplateAsync(IDevice device, string templatePath)
        {
            return await SaveRegisterMapToExcelAsync(device, templatePath, false);
        }

        #endregion

        #region Excel工作表创建方法

        private static void CreateRegisterOverviewSheet(WorkbookPart workbookPart, Sheets sheets,
            List<RegisterMap> registerMaps, Dictionary<uint, byte> currentValues)
        {
            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var sheetId = (uint)(sheets.ChildElements.Count + 1);

            var sheet = new Sheet()
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = sheetId,
                Name = "Register_Overview"
            };
            sheets.Append(sheet);

            var worksheetData = new Worksheet();
            var sheetData = new SheetData();

            // 创建标题行
            var headerRow = new Row() { RowIndex = 1 };
            headerRow.Append(
                CreateCell("A", 1, "Address", CellValues.String, 1),
                CreateCell("B", 1, "Name", CellValues.String, 1),
                CreateCell("C", 1, "Type", CellValues.String, 1),
                CreateCell("D", 1, "Size", CellValues.String, 1),
                CreateCell("E", 1, "Access", CellValues.String, 1),
                CreateCell("F", 1, "Value(Hex)", CellValues.String, 1),
                CreateCell("G", 1, "Value(Bin)", CellValues.String, 1),
                CreateCell("H", 1, "BitFields Count", CellValues.String, 1),
                CreateCell("I", 1, "Description", CellValues.String, 1)
            );
            sheetData.Append(headerRow);

            // 添加数据行
            uint rowIndex = 2;
            foreach (var regMap in registerMaps.OrderBy(rm => rm.Address))
            {
                var currentValue = currentValues.TryGetValue(regMap.Address, out byte value) ? value : (byte)0;
                var binaryValue = Convert.ToString(currentValue, 2).PadLeft(8, '0');
                // 格式化二进制显示，每4位添加空格
                var formattedBinary = $"{binaryValue.Substring(0, 4)} {binaryValue.Substring(4, 4)}";

                var dataRow = new Row() { RowIndex = rowIndex };
                dataRow.Append(
                    CreateCell("A", rowIndex, $"0x{regMap.Address:X4}", CellValues.String),
                    CreateCell("B", rowIndex, regMap.Name, CellValues.String),
                    CreateCell("C", rowIndex, regMap.Type.ToString(), CellValues.String),
                    CreateCell("D", rowIndex, regMap.Size.ToString(), CellValues.Number),
                    CreateCell("E", rowIndex, regMap.Access.ToString(), CellValues.String),
                    CreateCell("F", rowIndex, $"0x{currentValue:X2}", CellValues.String),
                    CreateCell("G", rowIndex, formattedBinary, CellValues.String),
                    CreateCell("H", rowIndex, regMap.BitFields.Count.ToString(), CellValues.Number),
                    CreateCell("I", rowIndex, regMap.Description ?? "", CellValues.String)
                );
                sheetData.Append(dataRow);
                rowIndex++;
            }

            worksheetData.Append(sheetData);
            worksheetPart.Worksheet = worksheetData;
        }

        private static void CreateBitFieldConfigSheet(WorkbookPart workbookPart, Sheets sheets,
            List<RegisterMap> registerMaps, Dictionary<uint, byte> currentValues)
        {
            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var sheetId = (uint)(sheets.ChildElements.Count + 1);

            var sheet = new Sheet()
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = sheetId,
                Name = "BitField_Config"
            };
            sheets.Append(sheet);

            var worksheetData = new Worksheet();
            var sheetData = new SheetData();

            // 创建说明行
            var instructionRow = new Row() { RowIndex = 1 };
            instructionRow.Append(CreateCell("A", 1, "BitField Config - Modify the 'New Value' column to configure the register", CellValues.String, 2));
            sheetData.Append(instructionRow);

            var emptyRow = new Row() { RowIndex = 2 };
            sheetData.Append(emptyRow);

            // 创建标题行
            var headerRow = new Row() { RowIndex = 3 };
            headerRow.Append(
                CreateCell("A", 3, "Register Address", CellValues.String, 1),
                CreateCell("B", 3, "Register Name", CellValues.String, 1),
                CreateCell("C", 3, "BitField Name", CellValues.String, 1),
                CreateCell("D", 3, "Bit Position", CellValues.String, 1),
                CreateCell("E", 3, "Bit Range", CellValues.String, 1),
                CreateCell("F", 3, "Value", CellValues.String, 1),
                CreateCell("G", 3, "Meaning", CellValues.String, 1),
                CreateCell("H", 3, "New Value", CellValues.String, 2), // 用户编辑列
                CreateCell("I", 3, "Optional Value", CellValues.String, 1),
                CreateCell("J", 3, "Description", CellValues.String, 1)
            );
            sheetData.Append(headerRow);

            // 添加位字段数据
            uint rowIndex = 4;
            foreach (var regMap in registerMaps.OrderBy(rm => rm.Address))
            {
                var currentRegValue = currentValues.TryGetValue(regMap.Address, out byte value) ? value : (byte)0;
                var regData = new byte[] { currentRegValue };

                foreach (var bitField in regMap.BitFields.OrderBy(bf => bf.BitPosition))
                {
                    var currentBitValue = ExtractBitField(regData, bitField);
                    var currentMeaning = bitField.ValueMappings?.GetValueOrDefault((int)currentBitValue, $"值: {currentBitValue}") ?? $"值: {currentBitValue}";
                    var availableValues = bitField.ValueMappings?.Any() == true
                        ? string.Join("; ", bitField.ValueMappings.Select(vm => $"{vm.Key}={vm.Value}"))
                        : $"0-{(1 << bitField.BitWidth) - 1}";

                    var dataRow = new Row() { RowIndex = rowIndex };
                    dataRow.Append(
                        CreateCell("A", rowIndex, $"0x{regMap.Address:X4}", CellValues.String),
                        CreateCell("B", rowIndex, regMap.Name, CellValues.String),
                        CreateCell("C", rowIndex, bitField.Name, CellValues.String),
                        CreateCell("D", rowIndex, bitField.BitPosition.ToString(), CellValues.Number),
                        CreateCell("E", rowIndex, bitField.BitWidth.ToString(), CellValues.Number),
                        CreateCell("F", rowIndex, currentBitValue.ToString(), CellValues.Number),
                        CreateCell("G", rowIndex, currentMeaning, CellValues.String),
                        CreateCell("H", rowIndex, currentBitValue.ToString(), CellValues.Number), // 用户可编辑
                        CreateCell("I", rowIndex, availableValues, CellValues.String),
                        CreateCell("J", rowIndex, bitField.Description ?? "", CellValues.String)
                    );
                    sheetData.Append(dataRow);
                    rowIndex++;
                }

                // 添加空行分隔不同寄存器
                var separatorRow = new Row() { RowIndex = rowIndex };
                sheetData.Append(separatorRow);
                rowIndex++;
            }

            worksheetData.Append(sheetData);
            worksheetPart.Worksheet = worksheetData;
        }

        private static void CreateRegisterDetailSheet(WorkbookPart workbookPart, Sheets sheets, string groupName, List<RegisterMap> registerMaps, Dictionary<uint, byte> currentValues)
        {
            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var sheetId = (uint)(sheets.ChildElements.Count + 1);

            var sheet = new Sheet()
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = sheetId,
                Name = $"{groupName}_Registers"
            };
            sheets.Append(sheet);

            var worksheetData = new Worksheet();
            var sheetData = new SheetData();

            // 标题行
            var titleRow = new Row() { RowIndex = 1 };
            titleRow.Append(CreateCell("A", 1, $"{groupName} Details", CellValues.String, 2));
            sheetData.Append(titleRow);

            uint rowIndex = 3;
            foreach (var regMap in registerMaps.OrderBy(rm => rm.Address))
            {
                // 寄存器基本信息
                var regInfoRow = new Row() { RowIndex = rowIndex };
                regInfoRow.Append(
                    CreateCell("A", rowIndex, "Register:", CellValues.String, 1),
                    CreateCell("B", rowIndex, $"{regMap.Name} (0x{regMap.Address:X4})", CellValues.String, 1),
                    CreateCell("C", rowIndex, regMap.Description ?? "", CellValues.String)
                );
                sheetData.Append(regInfoRow);
                rowIndex++;

                // 位字段标题
                var bitFieldHeaderRow = new Row() { RowIndex = rowIndex };
                bitFieldHeaderRow.Append(
                    CreateCell("B", rowIndex, "BitField", CellValues.String, 1),
                    CreateCell("C", rowIndex, "Position", CellValues.String, 1),
                    CreateCell("D", rowIndex, "Range", CellValues.String, 1),
                    CreateCell("E", rowIndex, "Value", CellValues.String, 1),
                    CreateCell("F", rowIndex, "Meaning", CellValues.String, 1),
                    CreateCell("G", rowIndex, "Description", CellValues.String, 1)
                );
                sheetData.Append(bitFieldHeaderRow);
                rowIndex++;

                // 位字段详情
                var currentRegValue = currentValues.TryGetValue(regMap.Address, out byte value) ? value : (byte)0;
                var regData = new byte[] { currentRegValue };

                foreach (var bitField in regMap.BitFields.OrderBy(bf => bf.BitPosition))
                {
                    var currentValue = ExtractBitField(regData, bitField);
                    var meaning = bitField.ValueMappings?.GetValueOrDefault((int)currentValue, $"值: {currentValue}") ?? $"值: {currentValue}";

                    var bitFieldRow = new Row() { RowIndex = rowIndex };
                    bitFieldRow.Append(
                        CreateCell("B", rowIndex, bitField.Name, CellValues.String),
                        CreateCell("C", rowIndex, $"{bitField.BitPosition}-{bitField.BitPosition + bitField.BitWidth - 1}", CellValues.String),
                        CreateCell("D", rowIndex, bitField.BitWidth.ToString(), CellValues.Number),
                        CreateCell("E", rowIndex, currentValue.ToString(), CellValues.Number),
                        CreateCell("F", rowIndex, meaning, CellValues.String),
                        CreateCell("G", rowIndex, bitField.Description ?? "", CellValues.String)
                    );
                    sheetData.Append(bitFieldRow);
                    rowIndex++;
                }

                // 添加空行
                var emptyRow = new Row() { RowIndex = rowIndex };
                sheetData.Append(emptyRow);
                rowIndex += 2;
            }

            worksheetData.Append(sheetData);
            worksheetPart.Worksheet = worksheetData;
        }

        #endregion

        #region Excel解析方法

        private static List<BitFieldConfigItem> ParseBitFieldConfigSheet(Worksheet worksheet, WorkbookPart workbookPart)
        {
            var configItems = new List<BitFieldConfigItem>();
            var sheetData = worksheet.GetFirstChild<SheetData>();
            var stringTable = workbookPart.GetPartsOfType<SharedStringTablePart>().FirstOrDefault()?.SharedStringTable;

            foreach (Row row in sheetData.Elements<Row>())
            {
                if (row.RowIndex <= 3) continue; // 跳过标题行

                var cells = row.Elements<Cell>().ToArray();
                if (cells.Length < 8) continue;

                try
                {
                    var addressText = GetCellValue(cells[0], stringTable);
                    var registerName = GetCellValue(cells[1], stringTable);
                    var bitFieldName = GetCellValue(cells[2], stringTable);
                    var newValueText = GetCellValue(cells[7], stringTable); // H列：新值

                    if (string.IsNullOrWhiteSpace(addressText) || string.IsNullOrWhiteSpace(newValueText))
                        continue;

                    // 解析地址
                    if (!TryParseAddress(addressText, out uint address))
                        continue;

                    // 解析新值
                    if (!uint.TryParse(newValueText, out uint newValue))
                        continue;

                    configItems.Add(new BitFieldConfigItem
                    {
                        RegisterAddress = address,
                        RegisterName = registerName,
                        BitFieldName = bitFieldName,
                        NewValue = newValue
                    });
                }
                catch (Exception)
                {
                    // 跳过解析错误的行
                    continue;
                }
            }

            return configItems;
        }

        private static bool TryParseAddress(string addressText, out uint address)
        {
            address = 0;

            if (string.IsNullOrWhiteSpace(addressText))
                return false;

            // 支持 0x1234 或 1234 格式
            if (addressText.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return uint.TryParse(addressText.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out address);
            }
            else
            {
                return uint.TryParse(addressText, out address);
            }
        }

        private static string GetCellValue(Cell cell, SharedStringTable stringTable)
        {
            if (cell?.CellValue == null) return string.Empty;

            var value = cell.CellValue.Text;

            if (cell.DataType?.Value == CellValues.SharedString)
            {
                if (int.TryParse(value, out int stringIndex))
                {
                    return stringTable?.ElementAt(stringIndex)?.InnerText ?? string.Empty;
                }
            }

            return value ?? string.Empty;
        }

        #endregion

        #region 验证和应用方法

        private static List<ValidationError> ValidateConfigData(IDevice device, List<BitFieldConfigItem> configData)
        {
            var errors = new List<ValidationError>();
            var registerMaps = device.GetRegisterMaps().ToDictionary(rm => rm.Address, rm => rm);

            foreach (var configItem in configData)
            {
                // 验证寄存器是否存在
                if (!registerMaps.TryGetValue(configItem.RegisterAddress, out var registerMap))
                {
                    errors.Add(new ValidationError
                    {
                        IsError = true,
                        Message = $"Register 0x{configItem.RegisterAddress:X4} does not exsit",
                        RegisterAddress = configItem.RegisterAddress,
                        BitFieldName = configItem.BitFieldName
                    });
                    continue;
                }

                // 验证位字段是否存在
                var bitField = registerMap.BitFields.FirstOrDefault(bf => bf.Name == configItem.BitFieldName);
                if (bitField == null)
                {
                    errors.Add(new ValidationError
                    {
                        IsError = true,
                        Message = $"BitField {configItem.BitFieldName} in Register {registerMap.Name} does not exsit",
                        RegisterAddress = configItem.RegisterAddress,
                        BitFieldName = configItem.BitFieldName
                    });
                    continue;
                }

                // 验证值范围
                var maxValue = (1U << bitField.BitWidth) - 1;
                if (configItem.NewValue > maxValue)
                {
                    errors.Add(new ValidationError
                    {
                        IsError = true,
                        Message = $"BitField {configItem.BitFieldName}'s value {configItem.NewValue} is out of range (0-{maxValue})",
                        RegisterAddress = configItem.RegisterAddress,
                        BitFieldName = configItem.BitFieldName
                    });
                    continue;
                }

                // 验证寄存器是否可写
                if (registerMap.Access == RegisterAccess.ReadOnly)
                {
                    errors.Add(new ValidationError
                    {
                        IsError = false, // 警告，不是错误
                        Message = $"Register {registerMap.Name} is Read-Only, unable to write",
                        RegisterAddress = configItem.RegisterAddress,
                        BitFieldName = configItem.BitFieldName
                    });
                }
            }

            return errors;
        }

        private static async Task<ApplyResult> ApplyConfigToDeviceAsync(IDevice device, List<BitFieldConfigItem> configData)
        {
            var result = new ApplyResult { Success = true };

            // 按寄存器地址分组
            var registerGroups = configData.GroupBy(ci => ci.RegisterAddress);
            var registerMaps = device.GetRegisterMaps().ToDictionary(rm => rm.Address, rm => rm);

            foreach (var group in registerGroups)
            {
                try
                {
                    var registerAddress = group.Key;
                    var registerMap = registerMaps[registerAddress];

                    // 如果寄存器是只读的，跳过
                    if (registerMap.Access == RegisterAccess.ReadOnly)
                        continue;

                    // 读取当前寄存器值
                    var readResult = await device.ReadRegisterAsync(registerAddress, registerMap.Size);
                    if (!readResult.Success)
                    {
                        result.ErrorMessage += $"Read register 0x{registerAddress:X4} failed: {readResult.ErrorMessage}; ";
                        continue;
                    }

                    var currentData = readResult.Data;
                    var newData = new byte[currentData.Length];
                    Array.Copy(currentData, newData, currentData.Length);

                    // 应用所有位字段修改
                    foreach (var configItem in group)
                    {
                        var bitField = registerMap.BitFields.FirstOrDefault(bf => bf.Name == configItem.BitFieldName);
                        if (bitField != null)
                        {
                            newData = SetBitField(newData, bitField, configItem.NewValue);
                        }
                    }

                    // 写入修改后的值
                    var writeSuccess = await device.WriteRegisterAsync(registerAddress, newData);
                    if (writeSuccess)
                    {
                        result.AppliedCount++;
                    }
                    else
                    {
                        result.ErrorMessage += $"Write register 0x{registerAddress:X4} failed; ";
                    }
                }
                catch (Exception ex)
                {
                    result.ErrorMessage += $"Handling regsiter 0x{group.Key:X4} failed: {ex.Message}; ";
                }
            }

            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                result.Success = false;
            }

            return result;
        }

        #endregion

        #region 辅助方法

        private static async Task<Dictionary<uint, byte>> CollectCurrentRegisterValuesAsync(IDevice device, List<RegisterMap> registerMaps)
        {
            var values = new Dictionary<uint, byte>();

            foreach (var regMap in registerMaps)
            {
                try
                {
                    if (regMap.Access != RegisterAccess.WriteOnly)
                    {
                        var result = await device.ReadRegisterAsync(regMap.Address, 1);
                        if (result.Success && result.Data?.Length > 0)
                        {
                            values[regMap.Address] = result.Data[0];
                        }
                    }
                }
                catch (Exception)
                {
                    // 忽略读取错误，使用默认值0
                    values[regMap.Address] = 0;
                }
            }

            return values;
        }

        private static Worksheet FindWorksheet(WorkbookPart workbookPart, string sheetName)
        {
            var sheet = workbookPart.Workbook.Descendants<Sheet>().FirstOrDefault(s => s.Name == sheetName);
            if (sheet == null) return null;

            var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id);
            return worksheetPart.Worksheet;
        }

        private static Cell CreateCell(string columnName, uint rowIndex, string value, CellValues dataType, uint? styleIndex = null)
        {
            var cell = new Cell()
            {
                CellReference = columnName + rowIndex,
                DataType = dataType,
                CellValue = new CellValue(value)
            };

            if (styleIndex.HasValue)
            {
                cell.StyleIndex = styleIndex.Value;
            }

            return cell;
        }

        private static Stylesheet CreateStylesheet()
        {
            var stylesheet = new Stylesheet();

            // 字体
            var fonts = new Fonts();
            fonts.Append(new Font()); // 默认字体
            fonts.Append(new Font(new Bold())); // 粗体字体
            fonts.Count = 2;

            // 填充
            var fills = new Fills();
            fills.Append(new Fill()); // 默认填充
            fills.Append(new Fill(new PatternFill() { PatternType = PatternValues.Gray125 })); // 灰色填充
            fills.Count = 2;

            // 边框
            var borders = new Borders();
            borders.Append(new Border()); // 默认边框
            borders.Count = 1;

            // 单元格格式
            var cellFormats = new CellFormats();
            cellFormats.Append(new CellFormat()); // 默认格式
            cellFormats.Append(new CellFormat() { FontId = 1, FillId = 1 }); // 标题格式
            cellFormats.Append(new CellFormat() { FontId = 1 }); // 粗体格式
            cellFormats.Count = 3;

            stylesheet.Append(fonts);
            stylesheet.Append(fills);
            stylesheet.Append(borders);
            stylesheet.Append(cellFormats);

            return stylesheet;
        }

        #endregion
    }

    #region 数据模型

    /// <summary>
    /// Excel加载结果
    /// </summary>
    public class ExcelLoadResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public int AppliedCount { get; set; }
        public List<ValidationError> ValidationErrors { get; set; } = new List<ValidationError>();
    }

    /// <summary>
    /// 位字段配置项
    /// </summary>
    public class BitFieldConfigItem
    {
        public uint RegisterAddress { get; set; }
        public string RegisterName { get; set; }
        public string BitFieldName { get; set; }
        public uint NewValue { get; set; }
    }

    /// <summary>
    /// 验证错误
    /// </summary>
    public class ValidationError
    {
        public bool IsError { get; set; } // true=错误, false=警告
        public string Message { get; set; }
        public uint RegisterAddress { get; set; }
        public string BitFieldName { get; set; }
    }

    /// <summary>
    /// 应用结果
    /// </summary>
    public class ApplyResult
    {
        public bool Success { get; set; }
        public int AppliedCount { get; set; }
        public string ErrorMessage { get; set; }
    }

    #endregion
}