using System.Collections.Generic;
using SupervisorApp.Core.Devices;

namespace SupervisorApp.Models
{
    /// <summary>
    /// VA79A寄存器映射
    /// </summary>
    public static class VA79ARegisterMaps
    {
        public static IEnumerable<RegisterMap> GetRegisterMaps()
        {
            var registerMaps = new List<RegisterMap>();

            return registerMaps;
        }

        private static IEnumerable<RegisterMap> Create00Registers()
        {
            yield return new RegisterMap()
            {
                Address = 0x00,
                Name = "Control Register1",
                Description = "Controls the operation of the device.",
                Type = RegisterType.Control,
                Access = RegisterAccess.ReadWrite,
                Size = 1,
                BitFields = new List<BitField>()
                {
                    new BitField
                    {
                        Name = "Contro Byte",
                        Description = "A5 -> Write Register to E-fuse; A4 -> Download E-fuse to Register(0x02 to 0x09)",
                        BitPosition = 0,
                        BitWidth = 8,
                    }
                }
            };


        }

        private static IEnumerable<RegisterMap> Create01Registers()
        {
            yield return new RegisterMap()
            {
                Address = 0x01,
                Name = "Control Register2",
                Description = "The number of times an E-fuse can be written to.",
                Type = RegisterType.Status,
                Access = RegisterAccess.ReadOnly,
                Size = 1,
                BitFields = new List<BitField>()
                {
                    new BitField
                    {
                        Name = "E-fuse Write Count",
                        Description = "The number of times the E-fuse can be written to.",
                        BitPosition = 0,
                        BitWidth = 8,
                        ValueMappings = new Dictionary<int, string>
                        {
                            { 0, "E-Fuse don't write" }, { 1, "E-Fuse write 1 time"}, 
                            { 2, "E-Fuse write 2 times" }, { 3, "Reserved"}
                        }
                    }
                }
            };
        }

        private static IEnumerable<RegisterMap> Create02Registers()
        {
            yield return new RegisterMap()
            {
                Address = 0x02,
                Name = "Control Register3",
                Description = "Controls the operation of the device.",
                Type = RegisterType.Control,
                Access = RegisterAccess.ReadWrite,
                Size = 1,
                BitFields = new List<BitField>()
                {
                    new BitField
                    {
                        Name = "CLKO_OCP_TH",
                        Description = "",
                        BitPosition = 5,
                        BitWidth = 3,
                    }
                }
            };
        }
    }
}