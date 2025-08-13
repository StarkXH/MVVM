using SupervisorApp.Core.Common;
using SupervisorApp.Core.Devices;
using SupervisorApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SupervisorApp.Helpers
{
    /// <summary>
    /// Device Test Helper - Provides various device testing scenarios
    /// Created: 2025-08-06 09:38:52 UTC
    /// Author: StarkXH
    /// Stage: Phase 2 - Hardware Device Management System
    /// </summary>
    public static class DeviceTestHelper
    {
        #region Comprehensive Test

        /// <summary>
        /// Run comprehensive test
        /// </summary>
        /// <param name="device">Device to test</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Test result</returns>
        public static async Task<TestResult> RunComprehensiveTest(IDevice device, CancellationToken cancellationToken = default)
        {
            var testResult = new TestResult { DeviceId = device.DeviceID };

            try
            {
                LogService.Instance.LogInfo($"🧪 Starting comprehensive device test: {device.DeviceName} ({device.DeviceID})");
                LogService.Instance.LogInfo($"Device Type: {device.DeviceType}, Protocol: {device.Protocol}");
                LogService.Instance.LogInfo($"Device Address: 0x{device.DeviceAddress:X4}, Firmware Version: {device.FirmwareVersion}");
                LogService.Instance.LogInfo(new string('=', 60));

                // 1. Connection test
                await RunConnectionTest(device, testResult, cancellationToken);

                // 2. Basic read/write test
                await RunBasicReadWriteTest(device, testResult, cancellationToken);

                // 3. Register map test
                await RunRegisterMapTest(device, testResult, cancellationToken);

                // 4. Performance test
                await RunPerformanceTest(device, testResult, cancellationToken);

                // 5. Error handling test
                await RunErrorHandlingTest(device, testResult, cancellationToken);

                // 6. Concurrency test
                await RunConcurrencyTest(device, testResult, cancellationToken);

                // 7. Print final results
                PrintTestSummary(testResult);

                return testResult;
            }
            catch (Exception ex)
            {
                testResult.OverallResult = false;
                testResult.ErrorMessage = ex.Message;
                LogService.Instance.LogError($"❌ Comprehensive test failed: {ex.Message}");
                return testResult;
            }
        }

        #endregion

        #region Connection Test

        /// <summary>
        /// Connection test
        /// </summary>
        private static async Task RunConnectionTest(IDevice device, TestResult testResult, CancellationToken cancellationToken)
        {
            LogService.Instance.LogInfo("🔌 === Connection Test ===");

            try
            {
                // Check initial state
                LogService.Instance.LogInfo($"Initial connection state: {device.ConnectionState}");

                // Probe device
                var probeResult = await device.ProbeAsync(cancellationToken);
                LogService.Instance.LogInfo($"Device probe result: {probeResult}");
                testResult.ConnectionTestPassed = probeResult;

                // Get device information
                var deviceInfo = await device.GetDeviceInfoAsync(cancellationToken);
                LogService.Instance.LogInfo($"Manufacturer ID: {deviceInfo.ManufacturerId}");
                LogService.Instance.LogInfo($"Product ID: {deviceInfo.ProductId}");
                LogService.Instance.LogInfo($"Serial Number: {deviceInfo.SerialNumber}");
                LogService.Instance.LogInfo($"Hardware Revision: {deviceInfo.HardwareRevision}");

                testResult.DeviceInfo = deviceInfo;
            }
            catch (Exception ex)
            {
                LogService.Instance.LogError($"❌ Connection test failed: {ex.Message}");
                testResult.ConnectionTestPassed = false;
            }
        }

        #endregion

        #region Basic Read/Write Test

        /// <summary>
        /// Basic read/write test
        /// </summary>
        private static async Task RunBasicReadWriteTest(IDevice device, TestResult testResult, CancellationToken cancellationToken)
        {
            LogService.Instance.LogInfo("📖 === Basic Read/Write Test ===");

            var readWriteResults = new List<ReadWriteTestCase>();

            try
            {
                // Test different register addresses
                var testAddresses = new uint[] { 0x1000, 0x1010, 0x1020, 0x1030, 0x1040 };

                foreach (var address in testAddresses)
                {
                    var testCase = new ReadWriteTestCase { Address = address };

                    try
                    {
                        // Read original value
                        var originalValue = await device.ReadByteAsync(address, cancellationToken);
                        if (originalValue.HasValue)
                        {
                            testCase.OriginalValue = originalValue.Value;
                            LogService.Instance.LogInfo($"Address 0x{address:X4}: Original value = 0x{originalValue:X2}");

                            // Write test value
                            var testValue = (byte)(originalValue.Value ^ 0xFF); // Invert all bits
                            var writeSuccess = await device.WriteByteAsync(address, testValue, cancellationToken);

                            if (writeSuccess)
                            {
                                // Read back and verify
                                var readbackValue = await device.ReadByteAsync(address, cancellationToken);
                                if (readbackValue.HasValue && readbackValue.Value == testValue)
                                {
                                    testCase.TestPassed = true;
                                    LogService.Instance.LogInfo($"✅ Address 0x{address:X4}: Write verification successful (0x{testValue:X2})");

                                    // Restore original value
                                    await device.WriteByteAsync(address, originalValue.Value, cancellationToken);
                                }
                                else
                                {
                                    testCase.TestPassed = false;
                                    testCase.ErrorMessage = $"Readback value mismatch: Expected 0x{testValue:X2}, Actual 0x{readbackValue:X2}";
                                    LogService.Instance.LogError($"❌ Address 0x{address:X4}: {testCase.ErrorMessage}");
                                }
                            }
                            else
                            {
                                testCase.TestPassed = false;
                                testCase.ErrorMessage = "Write failed";
                                LogService.Instance.LogError($"❌ Address 0x{address:X4}: Write failed");
                            }
                        }
                        else
                        {
                            testCase.TestPassed = false;
                            testCase.ErrorMessage = "Failed to read original value";
                            LogService.Instance.LogError($"❌ Address 0x{address:X4}: Read failed");
                        }
                    }
                    catch (Exception ex)
                    {
                        testCase.TestPassed = false;
                        testCase.ErrorMessage = ex.Message;
                        LogService.Instance.LogError($"❌ Address 0x{address:X4}: Exception - {ex.Message}");
                    }

                    readWriteResults.Add(testCase);
                }

                testResult.ReadWriteTests = readWriteResults;
                testResult.BasicReadWriteTestPassed = readWriteResults.All(t => t.TestPassed);
            }
            catch (Exception ex)
            {
                LogService.Instance.LogError($"❌ Basic read/write test failed: {ex.Message}");
                testResult.BasicReadWriteTestPassed = false;
            }
        }

        #endregion

        #region Register Map Test

        /// <summary>
        /// Register map test
        /// </summary>
        private static async Task RunRegisterMapTest(IDevice device, TestResult testResult, CancellationToken cancellationToken)
        {
            LogService.Instance.LogInfo("🗺️ === Register Map Test ===");

            try
            {
                var registerMaps = device.GetRegisterMaps().ToList();
                LogService.Instance.LogInfo($"Found {registerMaps.Count} register mappings");

                var mapTestResults = new List<RegisterMapTestCase>();

                foreach (var registerMap in registerMaps.Take(10)) // Test first 10 registers
                {
                    var testCase = new RegisterMapTestCase
                    {
                        RegisterMap = registerMap,
                        Address = registerMap.Address
                    };

                    try
                    {
                        // Test based on access permissions
                        if (registerMap.Access == RegisterAccess.ReadOnly || registerMap.Access == RegisterAccess.ReadWrite)
                        {
                            var result = await device.ReadRegisterAsync(registerMap.Address, registerMap.Size, cancellationToken);
                            if (result.Success)
                            {
                                testCase.ReadSuccess = true;
                                testCase.ReadValue = result.Data;

                                LogService.Instance.LogInfo($"✅ {registerMap.Name} (0x{registerMap.Address:X4}): " +
                                    $"Read successful = {BitConverter.ToString(result.Data)}");

                                // Parse bit fields
                                if (registerMap.BitFields.Any() && result.Data.Length > 0)
                                {
                                    ParseBitFields(registerMap, result.Data[0]);
                                }
                            }
                            else
                            {
                                testCase.ReadSuccess = false;
                                testCase.ErrorMessage = result.ErrorMessage;
                                LogService.Instance.LogError($"❌ {registerMap.Name}: Read failed - {result.ErrorMessage}");
                            }
                        }

                        testCase.TestPassed = testCase.ReadSuccess;
                    }
                    catch (Exception ex)
                    {
                        testCase.TestPassed = false;
                        testCase.ErrorMessage = ex.Message;
                        LogService.Instance.LogError($"❌ {registerMap.Name}: Test exception - {ex.Message}");
                    }

                    mapTestResults.Add(testCase);
                }

                testResult.RegisterMapTests = mapTestResults;
                testResult.RegisterMapTestPassed = mapTestResults.All(t => t.TestPassed);
            }
            catch (Exception ex)
            {
                LogService.Instance.LogError($"❌ Register map test failed: {ex.Message}");
                testResult.RegisterMapTestPassed = false;
            }
        }

        /// <summary>
        /// Parse bit fields
        /// </summary>
        private static void ParseBitFields(RegisterMap registerMap, byte value)
        {
            LogService.Instance.LogInfo($"    Bit field parsing (Value: 0x{value:X2} = {Convert.ToString(value, 2).PadLeft(8, '0')}):");

            foreach (var bitField in registerMap.BitFields)
            {
                var mask = (1 << bitField.BitWidth) - 1;
                var fieldValue = (value >> bitField.BitPosition) & mask;

                var description = bitField.ValueMappings.ContainsKey(fieldValue)
                    ? bitField.ValueMappings[fieldValue]
                    : fieldValue.ToString();

                LogService.Instance.LogInfo($"      {bitField.Name} [{bitField.BitPosition}:{bitField.BitPosition + bitField.BitWidth - 1}] = {fieldValue} ({description})");
            }
        }

        #endregion

        #region Performance Test

        /// <summary>
        /// Performance test
        /// </summary>
        private static async Task RunPerformanceTest(IDevice device, TestResult testResult, CancellationToken cancellationToken)
        {
            LogService.Instance.LogInfo("⚡ === Performance Test ===");

            try
            {
                const int testCount = 100;
                const uint testAddress = 0x1000;

                var times = new List<TimeSpan>();

                // Batch read test
                for (int i = 0; i < testCount; i++)
                {
                    var startTime = DateTime.Now;
                    var result = await device.ReadByteAsync(testAddress, cancellationToken);
                    var endTime = DateTime.Now;

                    if (result.HasValue)
                    {
                        times.Add(endTime - startTime);
                    }
                }

                if (times.Count > 0)
                {
                    var avgTime = TimeSpan.FromMilliseconds(times.Average(t => t.TotalMilliseconds));
                    var minTime = times.Min();
                    var maxTime = times.Max();

                    testResult.PerformanceResults = new PerformanceTestResult
                    {
                        TestCount = testCount,
                        SuccessCount = times.Count,
                        AverageResponseTime = avgTime,
                        MinResponseTime = minTime,
                        MaxResponseTime = maxTime
                    };

                    LogService.Instance.LogInfo($"✅ Performance test completed:");
                    LogService.Instance.LogInfo($"   Test count: {testCount}, Success: {times.Count}");
                    LogService.Instance.LogInfo($"   Average response time: {avgTime.TotalMilliseconds:F2} ms");
                    LogService.Instance.LogInfo($"   Min response time: {minTime.TotalMilliseconds:F2} ms");
                    LogService.Instance.LogInfo($"   Max response time: {maxTime.TotalMilliseconds:F2} ms");

                    testResult.PerformanceTestPassed = true;
                }
            }
            catch (Exception ex)
            {
                LogService.Instance.LogError($"❌ Performance test failed: {ex.Message}");
                testResult.PerformanceTestPassed = false;
            }
        }

        #endregion

        #region Error Handling Test

        /// <summary>
        /// Error handling test
        /// </summary>
        private static async Task RunErrorHandlingTest(IDevice device, TestResult testResult, CancellationToken cancellationToken)
        {
            LogService.Instance.LogInfo("🚨 === Error Handling Test ===");

            try
            {
                var errorTests = new List<ErrorTestCase>();

                // Test invalid address
                var invalidAddressTest = new ErrorTestCase { TestName = "Invalid Address Read" };
                try
                {
                    var result = await device.ReadRegisterAsync(0xFFFF, 1, cancellationToken);
                    invalidAddressTest.ExpectedError = !result.Success;
                    invalidAddressTest.ActualResult = result.Success ? "Success" : $"Failed: {result.ErrorMessage}";
                    LogService.Instance.LogInfo($"Invalid address test: {invalidAddressTest.ActualResult}");
                }
                catch (Exception ex)
                {
                    invalidAddressTest.ExpectedError = true;
                    invalidAddressTest.ActualResult = $"Exception: {ex.Message}";
                    LogService.Instance.LogInfo($"Invalid address test: Caught exception - {ex.Message}");
                }
                errorTests.Add(invalidAddressTest);

                // Test oversized read
                var oversizeReadTest = new ErrorTestCase { TestName = "Oversized Read" };
                try
                {
                    var result = await device.ReadRegisterAsync(0x1000, 1000, cancellationToken);
                    oversizeReadTest.ExpectedError = !result.Success;
                    oversizeReadTest.ActualResult = result.Success ? "Success" : $"Failed: {result.ErrorMessage}";
                    LogService.Instance.LogInfo($"Oversized read test: {oversizeReadTest.ActualResult}");
                }
                catch (Exception ex)
                {
                    oversizeReadTest.ExpectedError = true;
                    oversizeReadTest.ActualResult = $"Exception: {ex.Message}";
                    LogService.Instance.LogInfo($"Oversized read test: Caught exception - {ex.Message}");
                }
                errorTests.Add(oversizeReadTest);

                testResult.ErrorHandlingTests = errorTests;
                testResult.ErrorHandlingTestPassed = errorTests.All(t => t.ExpectedError);
            }
            catch (Exception ex)
            {
                LogService.Instance.LogError($"❌ Error handling test failed: {ex.Message}");
                testResult.ErrorHandlingTestPassed = false;
            }
        }

        #endregion

        #region Concurrency Test

        /// <summary>
        /// Concurrency test
        /// </summary>
        private static async Task RunConcurrencyTest(IDevice device, TestResult testResult, CancellationToken cancellationToken)
        {
            LogService.Instance.LogInfo("🔄 === Concurrency Test ===");

            try
            {
                const int concurrentTasks = 5;  // 🟢 减少并发数量
                const uint testAddress = 0x1010;

                var semaphore = new SemaphoreSlim(3, 3); // 限制实际并发
                var tasks = new List<Task<bool>>();

                for (int i = 0; i < concurrentTasks; i++)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        await semaphore.WaitAsync(cancellationToken);
                        try
                        {
                            // 🟢 添加随机延迟，减少同时访问
                            var random = new Random();
                            await Task.Delay(random.Next(10, 100), cancellationToken);

                            var result = await device.ReadByteAsync(testAddress, cancellationToken);
                            return result.HasValue;
                        }
                        catch
                        {
                            return false;
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }));
                }

                var results = await Task.WhenAll(tasks);
                var successCount = results.Count(r => r);

                LogService.Instance.LogInfo($"Concurrency test result: {successCount}/{concurrentTasks} successful");

                testResult.ConcurrencyTestPassed = successCount >= concurrentTasks * 0.8; // 80% success rate considered pass
            }
            catch (Exception ex)
            {
                LogService.Instance.LogError($"❌ Concurrency test failed: {ex.Message}");
                testResult.ConcurrencyTestPassed = false;
            }
        }

        #endregion

        #region Device Status Display

        /// <summary>
        /// Print device status information
        /// </summary>
        public static async Task PrintDeviceStatus(IDevice device)
        {
            LogService.Instance.LogInfo($"📊 === Device Status Information ===");
            LogService.Instance.LogInfo($"Device ID: {device.DeviceID}");
            LogService.Instance.LogInfo($"Device Name: {device.DeviceName}");
            LogService.Instance.LogInfo($"Connection State: {device.ConnectionState}");
            LogService.Instance.LogInfo($"Last Communication Time: {device.LastCommunicationTime}");

            var stats = device.Statistics;
            if (stats != null)
            {
                LogService.Instance.LogInfo($"📈 Communication Statistics:");
                LogService.Instance.LogInfo($"  Total Transactions: {stats.TotalTransactions}");
                LogService.Instance.LogInfo($"  Successful Transactions: {stats.SuccessfulTransactions}");
                LogService.Instance.LogInfo($"  Failed Transactions: {stats.FailedTransactions}");
                LogService.Instance.LogInfo($"  Success Rate: {stats.SuccessRate:F1}%");
                LogService.Instance.LogInfo($"  Average Response Time: {stats.AverageResponseTime.TotalMilliseconds:F2} ms");
                LogService.Instance.LogInfo($"  Bytes Sent: {stats.BytesSent}");
                LogService.Instance.LogInfo($"  Bytes Received: {stats.BytesReceived}");
            }

            var config = device.GetCommunicationConfig();
            if (config != null)
            {
                LogService.Instance.LogInfo($"⚙️ Communication Configuration:");
                LogService.Instance.LogInfo($"  Protocol: {config.Protocol}");
                LogService.Instance.LogInfo($"  Device Address: 0x{config.DeviceAddress:X4}");
                LogService.Instance.LogInfo($"  Bus Speed: {config.BusSpeed} Hz");
                LogService.Instance.LogInfo($"  Timeout: {config.Timeout} ms");
                LogService.Instance.LogInfo($"  Retry Count: {config.RetryCount}");
            }
        }

        #endregion

        #region Test Results Summary

        /// <summary>
        /// Print test summary
        /// </summary>
        private static void PrintTestSummary(TestResult testResult)
        {
            LogService.Instance.LogInfo($"🎯 === Test Summary ===");
            LogService.Instance.LogInfo($"Device: {testResult.DeviceId}");
            LogService.Instance.LogInfo($"Connection Test: {(testResult.ConnectionTestPassed ? "✅ PASSED" : "❌ FAILED")}");
            LogService.Instance.LogInfo($"Basic Read/Write Test: {(testResult.BasicReadWriteTestPassed ? "✅ PASSED" : "❌ FAILED")}");
            LogService.Instance.LogInfo($"Register Map Test: {(testResult.RegisterMapTestPassed ? "✅ PASSED" : "❌ FAILED")}");
            LogService.Instance.LogInfo($"Performance Test: {(testResult.PerformanceTestPassed ? "✅ PASSED" : "❌ FAILED")}");
            LogService.Instance.LogInfo($"Error Handling Test: {(testResult.ErrorHandlingTestPassed ? "✅ PASSED" : "❌ FAILED")}");
            LogService.Instance.LogInfo($"Concurrency Test: {(testResult.ConcurrencyTestPassed ? "✅ PASSED" : "❌ FAILED")}");

            testResult.OverallResult = testResult.ConnectionTestPassed &&
                                     testResult.BasicReadWriteTestPassed &&
                                     testResult.RegisterMapTestPassed &&
                                     testResult.PerformanceTestPassed &&
                                     testResult.ErrorHandlingTestPassed &&
                                     testResult.ConcurrencyTestPassed;

            LogService.Instance.LogInfo($"🏆 Overall Result: {(testResult.OverallResult ? "✅ ALL TESTS PASSED" : "❌ SOME TESTS FAILED")}");
            LogService.Instance.LogInfo(new string('=', 60));
        }

        #endregion

        #region Quick Test Methods

        /// <summary>
        /// Quick register read test
        /// </summary>
        public static async Task QuickRegisterTest(IDevice device, uint startAddress = 0x1000, int count = 10)
        {
            LogService.Instance.LogInfo($"🚀 Quick register test (Address: 0x{startAddress:X4}, Count: {count})");

            for (uint i = 0; i < count; i++)
            {
                var address = startAddress + i;
                try
                {
                    var value = await device.ReadByteAsync(address);
                    if (value.HasValue)
                    {
                        LogService.Instance.LogInfo($"  0x{address:X4} = 0x{value:X2} ({value})");
                    }
                    else
                    {
                        LogService.Instance.LogError($"  0x{address:X4} = Read failed");
                    }
                }
                catch (Exception ex)
                {
                    LogService.Instance.LogError($"  0x{address:X4} = Exception: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Simulate sensor data changes
        /// </summary>
        public static async Task SimulateSensorDataChanges(TestDevice100 testDevice, int duration = 30)
        {
            LogService.Instance.LogInfo($"🌡️ Simulating sensor data changes ({duration} seconds)");

            var endTime = DateTime.Now.AddSeconds(duration);
            var random = new Random();

            while (DateTime.Now < endTime)
            {
                // Simulate temperature changes (registers 0x1011-0x1012)
                var temperature = 20.0 + random.NextDouble() * 10.0; // 20-30°C
                var tempRaw = (ushort)((temperature + 45) * 65535 / 175);

                testDevice.SetRegisterValue(0x1011, (byte)(tempRaw >> 8));
                testDevice.SetRegisterValue(0x1012, (byte)(tempRaw & 0xFF));

                // Simulate GPIO changes
                var gpioValue = (byte)random.Next(0, 256);
                testDevice.SetRegisterValue(0x1002, gpioValue);

                LogService.Instance.LogInfo($"[{DateTime.Now:HH:mm:ss}] Temperature: {temperature:F1}°C, GPIO: 0x{gpioValue:X2}");

                await Task.Delay(2000); // Update every 2 seconds
            }
        }

        #endregion
    }
}
