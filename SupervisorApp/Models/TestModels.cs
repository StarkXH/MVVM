using SupervisorApp.Core.Devices;
using System;
using System.Collections.Generic;

namespace SupervisorApp.Test
{
    /// <summary>
    /// 测试结果
    /// </summary>
    public class TestResult
    {
        public string DeviceId { get; set; }
        public bool OverallResult { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime TestStartTime { get; set; } = DateTime.Now;
        public DateTime TestEndTime { get; set; }
        public TimeSpan TestDuration => TestEndTime - TestStartTime;

        // 各项测试结果
        public bool ConnectionTestPassed { get; set; }
        public bool BasicReadWriteTestPassed { get; set; }
        public bool RegisterMapTestPassed { get; set; }
        public bool PerformanceTestPassed { get; set; }
        public bool ErrorHandlingTestPassed { get; set; }
        public bool ConcurrencyTestPassed { get; set; }

        // 详细测试数据
        public DeviceInfo DeviceInfo { get; set; }
        public List<ReadWriteTestCase> ReadWriteTests { get; set; } = new List<ReadWriteTestCase>();
        public List<RegisterMapTestCase> RegisterMapTests { get; set; } = new List<RegisterMapTestCase>();
        public List<ErrorTestCase> ErrorHandlingTests { get; set; } = new List<ErrorTestCase>();
        public PerformanceTestResult PerformanceResults { get; set; }
    }

    /// <summary>
    /// 读写测试案例
    /// </summary>
    public class ReadWriteTestCase
    {
        public uint Address { get; set; }
        public byte OriginalValue { get; set; }
        public bool TestPassed { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// 寄存器映射测试案例
    /// </summary>
    public class RegisterMapTestCase
    {
        public uint Address { get; set; }
        public RegisterMap RegisterMap { get; set; }
        public bool ReadSuccess { get; set; }
        public byte[] ReadValue { get; set; }
        public bool TestPassed { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// 错误测试案例
    /// </summary>
    public class ErrorTestCase
    {
        public string TestName { get; set; }
        public bool ExpectedError { get; set; }
        public string ActualResult { get; set; }
    }

    /// <summary>
    /// 性能测试结果
    /// </summary>
    public class PerformanceTestResult
    {
        public int TestCount { get; set; }
        public int SuccessCount { get; set; }
        public TimeSpan AverageResponseTime { get; set; }
        public TimeSpan MinResponseTime { get; set; }
        public TimeSpan MaxResponseTime { get; set; }
        public double SuccessRate => TestCount > 0 ? (double)SuccessCount / TestCount * 100 : 0;
    }
}
