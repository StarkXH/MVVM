using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

#if WINFORM
using System.Windows.Forms;
#elif WPF
using System.Windows;
using Microsoft.Win32;
#endif

namespace ComCls {

    /// <summary>
    /// 字符串转换，包括十六进制字符检查，格式化，转换等
    /// String Convert
    /// </summary>
    public class CnvLib {

        #region Variable Define
        /// <summary>
        /// Status
        /// </summary>
        [Flags]
        public enum CNV_STAT {
            OK = 0,

            /* 异常 */
            CATCH_EXCEPTION,

            /* 多层调用 */
            SYSTEM_ERROR,

            /* 其它 */
            ERR_INVALID_HANDLE,

            /* 字符为空 */
            ERR_ISEMPTY,
            ERR_ISNULL,

            /* 字符格式 */
            ERR_FORMAT,
            ERR_HEXFORMAT,
            ERR_BINFORMAT,
            ERR_DECFORMAT,

            /* 长度匹配 */
            ERR_LEN_MATCH,
            ERR_BIT_LEN_MATCH, // Bit length does not match exactly for Hex or Bin string convert to bytes  
            ERR_BYTE_LEN_MATCH, // Bytes number != String Byte Length

            /* 其它 */
            ERR_BYTE_LEN_OVER, // String length for bytes > Request bytes number
            ERR_ASSICFORMAT, // 不能显示的ASSIC字符
        }

        /// <summary>
        /// Specifies the format and precision of the digital numeric value
        /// </summary>
        public enum DspDgtFmt {// 显示值格式, 基于值的转换，最多8个字节，
            Dec, // Decimal 正整数，字符串是高位在前，字节数组是高位在后
            Hex, // Hexadecimal 字符串是高位在前（2个字符表示一个字节），字节数组是高位在后       
            Bin, // Binary 字符串是高位在前（8个字符表示一个字节），字节数组是高位在后
            Octal,
            Scientific,
            Engineering,
            RelativeTime,
            TimeAndDate,
            SI,
            NA = -1, // Not Applicable
        };

        /// <summary>
        /// display format for the string constant
        /// </summary>
        public enum DspStrSty {// Display String Style
            Nom, // ASSIC码的可显示字符，没有最大长度限制，C#是基于Unicod e码的字符串
            Hex, // 16进字符串，2个字符（高位在前）表示一个字节，没有最大长度限制
            Bsc, // Backslash ('\') Codes Display 
            Password,
            NA = -1, // Not Applicable
        };

        /// <summary>
        /// Decimal Display Format
        /// </summary>
        public enum DecimalFormat {
            U8,
            I8,
            U16,
            I16,
            U32,
            I32,
            U64,
            I64,
        }
        #endregion

        #region Chinese/English Error Message
        private readonly static string CATCH_EXCEPTION_CH = /* Ch */
            "字符串在格式化或转换过程中发生异常，请重试。";
        private readonly static string CATCH_EXCEPTION_EN = /* En */
            "An exception occurred during string conversion or formation. Please try again.";

        private readonly static string ERR_ISNULL_CH = /* Ch */
            "输入字符不能为空，请重试。";
        private readonly static string ERR_ISNULL_EN = /* En */
            "The input character cannot be empty. Please try again.";

        private readonly static string ERR_ISEMPTY_CH = /* Ch */
            "输入字符不能为空，请重试。";
        private readonly static string ERR_ISEMPTY_EN = /* En */
            "The input character cannot be empty. Please try again.";

        private readonly static string ERR_BITMATCH_CH = /* Ch */
            "输入字符长度错误，要求输入16进制，两字符一组。请检查后重试。";
        private readonly static string ERR_BITMATCH_EN =   /* En */
            "Input character length error, request input HEX character, A group of two characters. Please check and try again.";

        private readonly static string ERR_BYTENUMBER_CH = /* Ch */
            "输入字符长度错误，要求输入16进制，两字符一组。请检查后重试。";
        private readonly static string ERR_BYTENUMBER_EN =   /* En */
            "Input character length error, request input HEX character, A group of two characters. Please check and try again.";

        private readonly static string ERR_HEXFORMAT_CH = /* Ch */
            "在输入的字符中有非16进制的字符。请检查后重试。";
        private readonly static string ERR_HEXFORMAT_EN = /* En */
            "Can't be formatted to hex in write data character. Please check and try again.";

        private readonly static string ERR_DECFORMAT_CH = /* Ch */
            "在输入的字符中有非16进制的字符。请检查后重试。";
        private readonly static string ERR_DECFORMAT_EN = /* En */
            "Can't be formatted to hex in write data character. Please check and try again.";

        private readonly static string ERR_BINFORMAT_CH = /* Ch */
            "在输入的字符中有非16进制的字符。请检查后重试。";
        private readonly static string ERR_BINFORMAT_EN = /* En */
            "Can't be formatted to hex in write data character. Please check and try again.";

        private readonly static string ERR_ASSICFORMAT_CH = /* Ch */
            "在输入的字符中有非16进制的字符。请检查后重试。";
        private readonly static string ERR_ASSICFORMAT_EN = /* En */
            "Can't be formatted to hex in write data character. Please check and try again.";
        #endregion

        #region Check Format String
        /// <summary>
        /// 检查字符串是否符合数字字符，可以是正负小数
        /// 检查字符串是否符合整数字符，不能有小数点和正负号 
        /// 检查字符串是否符合十六进制字符，注意不能有空隔符
        /// 检查字符串是否符合十六进制字符，可以带有空隔符（被忽略）
        /// 检查字符串是否符合十六进制字符，并且可以转为十六进制数据，注意不能有空隔符
        /// 检查字符串是否符合十六进制字符，并且可以转为十六进制数据，可以带有空隔符（被忽略）
        /// 检查字符串是否符合十六进制字符转换为单字节数组的要求
        /// </summary>
        /// <param name="dspDgtFmt"></param>
        /// <param name="iStr"></param>
        /// <param name="reqBytNum"></param>
        /// <returns></returns>
        public static CNV_STAT IsFormatString(CnvLib.DspDgtFmt dspDgtFmt, string iStr, int reqBytNum = -1) {
            CNV_STAT STAT = CNV_STAT.OK;
            try {
                if (iStr == null) {
                    STAT |= CNV_STAT.ERR_ISNULL;
                    return STAT;
                }
                string vldStr = iStr.Replace(" ", "").Replace("\n", "").Replace("\r", "").Replace(",", "");
                if (vldStr == "") {
                    STAT |= CNV_STAT.ERR_ISEMPTY;
                    return STAT;
                }
                switch (dspDgtFmt) {
                    case DspDgtFmt.Hex: // 基于值的转换，最多8个字节，字符串是高位在前（2个字符表示一个字节），字节数组是高位在后，
                        for (int i = 0; i < vldStr.Length; i++) {
                            if ("0123456789abcdefABCDEF".IndexOf(vldStr.ElementAt(i)) < 0) {
                                STAT |= CNV_STAT.ERR_HEXFORMAT;
                                return STAT;
                            }
                        }
                        if (vldStr.Length % 2 != 0) {
                            STAT |= CNV_STAT.ERR_BIT_LEN_MATCH;
                        }
                        if (reqBytNum > 0) {
                            if (vldStr.Length / 2 != reqBytNum) {
                                STAT |= CNV_STAT.ERR_BYTE_LEN_MATCH;
                            }
                            if (vldStr.Length / 2 > reqBytNum) {
                                STAT |= CNV_STAT.ERR_BYTE_LEN_OVER;
                                return STAT;
                            }
                        }
                        return STAT;
                    case DspDgtFmt.Dec: // 基于值的转换，最多8个字节，正整数
                        for (int i = 0; i < vldStr.Length; i++) {
                            if ("0123456789".IndexOf(vldStr.ElementAt(i)) < 0) {
                                STAT |= CNV_STAT.ERR_DECFORMAT;
                                return STAT;
                            }
                        }
                        return CNV_STAT.OK;
                    case DspDgtFmt.Bin: // 基于值的转换，最多8个字节，字符串是高位在前（8个字符表示一个字节），字节数组是高位在后，
                        for (int i = 0; i < vldStr.Length; i++) {
                            if ("01".IndexOf(vldStr.ElementAt(i)) < 0) {
                                STAT |= CNV_STAT.ERR_BINFORMAT;
                                return STAT;
                            }
                        }
                        if (vldStr.Length % 8 != 0) {
                            STAT |= CNV_STAT.ERR_BIT_LEN_MATCH;
                        }
                        if (reqBytNum > 0) {
                            if (vldStr.Length / 8 != reqBytNum) {
                                STAT |= CNV_STAT.ERR_BYTE_LEN_MATCH;
                            }
                            if (vldStr.Length / 8 != reqBytNum) {
                                STAT |= CNV_STAT.ERR_BYTE_LEN_OVER;
                                return STAT;
                            }
                        }
                        return STAT;
                    default:
                        STAT |= CNV_STAT.ERR_INVALID_HANDLE;
                        return STAT;
                }
            } catch (Exception ex) {
                MessageBox.Show("Error message: " + ex.Message, "Exception occurred");
                return CNV_STAT.CATCH_EXCEPTION;
            }
        }

        public static CNV_STAT IsFormatChar(DspStrSty dspStrSty, char iChr) {
            CNV_STAT STAT = CNV_STAT.OK;
            try {
                switch (dspStrSty) {
                    case DspStrSty.Hex:
                        if ("0123456789abcdefABCDEF".IndexOf(iChr) < 0) {
                            STAT = CNV_STAT.ERR_HEXFORMAT;
                            return STAT;
                        }
                        return STAT;
                    case DspStrSty.Nom:
                        if (iChr >= 0x20 && iChr <= 0x7E) {
                            STAT |= CNV_STAT.ERR_ASSICFORMAT;
                            return STAT;
                        }
                        return STAT;
                    case DspStrSty.Bsc:
                        STAT |= CNV_STAT.ERR_INVALID_HANDLE;
                        return STAT;
                    default:
                        STAT |= CNV_STAT.ERR_INVALID_HANDLE;
                        return STAT;
                }
            } catch (Exception ex) {
                MessageBox.Show("Error message: " + ex.Message, "Exception occurred");
                return CNV_STAT.CATCH_EXCEPTION;
            }
        }

        /// <summary>
        /// 检查字符串是否符合数字字符，可以是正负小数
        /// 检查字符串是否符合整数字符，不能有小数点和正负号 
        /// 检查字符串是否符合十六进制字符，注意不能有空隔符
        /// 检查字符串是否符合十六进制字符，可以带有空隔符（被忽略）
        /// 检查字符串是否符合十六进制字符，并且可以转为十六进制数据，注意不能有空隔符
        /// 检查字符串是否符合十六进制字符，并且可以转为十六进制数据，可以带有空隔符（被忽略）
        /// 检查字符串是否符合十六进制字符转换为单字节数组的要求
        /// </summary>
        /// <param name="dspStrSty"></param>
        /// <param name="iStr"></param>
        /// <returns></returns>
        public static CNV_STAT IsFormatString(DspStrSty dspStrSty, string iStr) {
            CNV_STAT STAT = CNV_STAT.OK;
            try {
                if (iStr == null) {
                    STAT |= CNV_STAT.ERR_ISNULL;
                    return STAT;
                }
                if (iStr == "") {
                    STAT |= CNV_STAT.ERR_ISEMPTY;
                    return STAT;
                }
                string vldStr = iStr.Replace(" ", "").Replace("\n", "").Replace("\r", "").Replace(",", "");
                switch (dspStrSty) {
                    case DspStrSty.Hex: // 16进字符串，2个字符（高位在前）表示一个字节，没有最大长度限制
                        for (int i = 0; i < vldStr.Length; i++) {
                            if ("0123456789abcdefABCDEF".IndexOf(vldStr.ElementAt(i)) < 0) {
                                STAT = CNV_STAT.ERR_HEXFORMAT;
                                return STAT;
                            }
                        }
                        if (vldStr.Length % 2 != 0) {
                            STAT |= CNV_STAT.ERR_BIT_LEN_MATCH;
                        }
                        return STAT;
                    case DspStrSty.Nom: // ASSIC码的可显示字符，没有最大长度限制，C#是基于Unicode码的字符串
                        byte[] bytes = Encoding.Default.GetBytes(vldStr); ;
                        for (int i = 0; i < bytes.Length; i++) {
                            if (bytes[i] > 0x48 || bytes[i] < 0x30) {
                                STAT |= CNV_STAT.ERR_ASSICFORMAT;
                                return STAT;
                            }
                        }
                        return STAT;
                    case DspStrSty.Bsc:
                        STAT |= CNV_STAT.ERR_INVALID_HANDLE;
                        return STAT;
                    default:
                        STAT |= CNV_STAT.ERR_INVALID_HANDLE;
                        return STAT;
                }
            } catch (Exception ex) {
                MessageBox.Show("Error message: " + ex.Message, "Exception occurred");
                return CNV_STAT.CATCH_EXCEPTION;
            }
        }
        #endregion

        #region String Convert
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dspValFmt">Display array format</param>
        /// <param name="iArrByt">Input byte array</param>
        /// <param name="spcBtnByt">Space between byte</param>
        /// <returns></returns>
        public static CNV_STAT ValueConvertToString(CnvLib.DspDgtFmt dspValFmt, ulong iValue, int BitLen, ref string oStr, int spcBtnByt = 2) {
            //int val;
            //if (iArrByt == null) {
            /*    return CNV_STAT.ERR_ISNULL; */
            //}
            //if (iArrByt.Length == 0) {
            /*    return CNV_STAT.ERR_ISEMPTY; */
            //}
            //switch (dspValFmt) {
            /*    case DspDgtFmt.Hex: /* 基于值的转换，最多8个字节，字符串是高位在前（2个字符表示一个字节），字节数组是高位在后， */
            /*        val = 0; */
            /*        string fmt = "X" + (iArrByt.Length * 2).ToString(); */
            /*        for (int i = 0; i < iArrByt.Length; i++) { */
            /*            val |= iArrByt[i] << (8 * i); */
            /*        } */
            /*        oStr = Convert.ToString(val, 16).ToUpper(); */
            /*        while (oStr.Length < iArrByt.Length * 2) { */
            /*            oStr = "0" + oStr; */
            /*        } */
            /*        if (spcBtnByt > 0) { */
            /*            int spcLen = iArrByt.Length / spcBtnByt + (iArrByt.Length % spcBtnByt > 0 ? 1 : 0) - 1; */
            /*            for (int i = 0; i < spcLen; i++) { */
            /*                oStr = oStr.Insert(oStr.Length - i * 2 * spcBtnByt, " "); */
            /*            } */
            /*        } */
            /*        return CNV_STAT.OK; */
            /*    case DspDgtFmt.Dec: /* 基于值的转换，最多8个字节，正整数 */
            /*        val = 0; */
            /*        for (int i = 0; i < iArrByt.Length; i++) { */
            /*            val |= iArrByt[i] << (8 * i); */
            /*        } */
            /*        oStr = val.ToString(); */
            /*        return CNV_STAT.OK; */
            /*    case DspDgtFmt.Bin: /* 基于值的转换，最多8个字节，字符串是高位在前（8个字符表示一个字节），字节数组是高位在后， */
            /*        val = 0; */
            /*        for (int i = 0; i < iArrByt.Length; i++) { */
            /*            val |= iArrByt[i] << (8 * i); */
            /*        } */
            /*        oStr = Convert.ToString(val, 2); */
            /*        while (oStr.Length < iArrByt.Length * 8) { */
            /*            oStr = "0" + oStr; */
            /*        } */
            /*        if (spcBtnByt > 0) { */
            /*            int spcLen = iArrByt.Length / spcBtnByt + (iArrByt.Length % spcBtnByt > 0 ? 1 : 0) - 1; */
            /*            for (int i = 0; i < spcLen; i++) { */
            /*                oStr = oStr.Insert(oStr.Length - i * 8 * spcBtnByt, " "); */
            /*            } */
            /*        } */
            /*        return CNV_STAT.OK; */
            /*    default: */
            /*        return CNV_STAT.ERR_INVALID_HANDLE; */
            //}
            return CNV_STAT.OK;
        }

        /// <summary>
        /// Bytes Convert to String
        /// </summary>
        /// <param name="iDspStrSty">Display array format</param>
        /// <param name="iArrByt">Input byte array</param>
        /// <param name="iSpcBtnByt">Space between byte</param>
        /// <returns></returns>
        public static string BytesConvertToString(DspStrSty iDspStrSty, byte[] iArrByt, int iSpcBtnByt = 2) {
            if (iArrByt == null) {
                return null;
            }
            if (iArrByt.Length == 0) {
                return "";
            }
            string str = "";
            switch (iDspStrSty) {
                case DspStrSty.Hex: // 16进字符串，2个字符（高位在前）表示一个字节，没有最大长度限制
                    for (int i = 0; i < iArrByt.Length; i++) {
                        bool b = iSpcBtnByt > 0 && (i + 1) % iSpcBtnByt == 0 && i + 1 != iArrByt.Length;
                        str += iArrByt[i].ToString("X2") + (b ? " " : "");
                    }
                    return str;
                case DspStrSty.Nom: // ASSIC码的可显示字符，没有最大长度限制，C#是基于Unicode码的字符串

                    BytesConvertToASICCString(iArrByt, ref str);
                    return str;
                default:
                    return str;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dspValFmt"></param>
        /// <param name="iStr"></param>
        /// <param name="bytArr"></param>
        /// <returns></returns>
        public static CNV_STAT StringConvertToBytes(DspDgtFmt dspValFmt, string iStr, ref byte[] bytArr) {
            CNV_STAT STAT = CNV_STAT.OK;
            ulong value; // 用于Hex, Dec, Bin三种模式，最多8个字节
            if (iStr == null) {
                STAT |= CNV_STAT.ERR_ISNULL;
                return STAT;
            }
            string vldStr = iStr.Replace(" ", "").Replace("\n", "").Replace("\r", "").Replace(",", "");
            if (vldStr.Length == 0) {
                STAT |= CNV_STAT.ERR_ISEMPTY;
                return STAT;
            }
            switch (dspValFmt) {
                case DspDgtFmt.Hex: // 基于值的转换，最多8个字节，字符串是高位在前（2个字符表示一个字节），字节数组是高位在后，
                    for (int i = 0; i < vldStr.Length; i++) {
                        if ("0123456789abcdefABCDEF".IndexOf(vldStr.ElementAt(i)) < 0) {
                            STAT |= CNV_STAT.ERR_HEXFORMAT;
                            return STAT;
                        }
                    }
                    if (bytArr == null || bytArr.Length == 0) {
                        /* 自适应字节数量 */
                        if (vldStr.Length % 2 != 0) {
                            STAT = CNV_STAT.ERR_BIT_LEN_MATCH;
                        }
                        int bytNum = vldStr.Length / 2 + (vldStr.Length % 2 > 0 ? 1 : 0);
                        bytArr = new byte[bytNum];
                    } else {
                        /* 要求字节数量 */
                        if (vldStr.Length / 2 != bytArr.Length) {
                            STAT |= CNV_STAT.ERR_BYTE_LEN_OVER;
                            return STAT;
                        }
                    }
                    value = Convert.ToUInt64(vldStr, 16);
                    for (int i = 0; i < bytArr.Length; i++) {
                        bytArr[i] = (byte)(value >> 8 * i);
                    }
                    return STAT;
                case DspDgtFmt.Dec: // 基于值的转换，最多8个字节，正整数，字符串是高位在前，字节数组是高位在后，
                    for (int i = 0; i < vldStr.Length; i++) {
                        if ("0123456789".IndexOf(vldStr.ElementAt(i)) < 0) {
                            STAT |= CNV_STAT.ERR_DECFORMAT;
                            return STAT;
                        }
                    }
                    value = Convert.ToUInt64(vldStr); // 用最大的位数进行转换
                    if (bytArr == null || bytArr.Length == 0) {
                        int bytNum = 0;
                        ulong t = value;
                        while (t % 256 > 0) {
                            bytNum++;
                            t /= 256;
                        }
                        bytArr = new byte[bytNum];
                    }
                    for (int i = 0; i < bytArr.Length; i++) {
                        bytArr[i] = (byte)(value >> 8 * i);
                    }
                    return STAT;
                case DspDgtFmt.Bin: // 基于值的转换，最多8个字节，字符串是高位在前（8个字符表示一个字节），字节数组是高位在后，
                    for (int i = 0; i < vldStr.Length; i++) {
                        if ("01".IndexOf(vldStr.ElementAt(i)) < 0) {
                            STAT |= CNV_STAT.ERR_BINFORMAT;
                            return STAT;
                        }
                    }
                    if (vldStr.Length % 8 != 0) {
                        STAT |= CNV_STAT.ERR_BIT_LEN_MATCH;
                    }

                    if (bytArr == null || bytArr.Length == 0) {
                        int bytNum = vldStr.Length / 8 + (vldStr.Length % 8 > 0 ? 1 : 0);
                        bytArr = new byte[bytNum];
                    } else {
                        if (vldStr.Length / 8 != bytArr.Length) {
                            STAT |= CNV_STAT.ERR_BYTE_LEN_OVER;
                        }
                        if (vldStr.Length / 8 > bytArr.Length) {
                            STAT |= CNV_STAT.ERR_BYTE_LEN_MATCH;
                            return STAT;
                        }
                    }
                    value = Convert.ToUInt64(vldStr, 2);
                    for (int i = 0; i < bytArr.Length / 2; i++) {
                        bytArr[i] = (byte)(value >> 8 * i);
                    }
                    return STAT;
                default:
                    STAT |= CNV_STAT.ERR_INVALID_HANDLE;
                    return STAT;
            }
        }
        public static CNV_STAT StringConvertToBytes(DspStrSty DspStrSty, string iStr, ref byte[] bytArr) {
            CNV_STAT STAT = CNV_STAT.OK;
            if (iStr == null) {
                STAT |= CNV_STAT.ERR_ISNULL;
                return STAT;
            }
            switch (DspStrSty) {
                case DspStrSty.Hex: // 16进字符串，2个字符（高位在前）表示一个字节，没有最大长度限制
                    string vldStr = iStr.Replace(" ", "").Replace("\n", "").Replace("\r", "").Replace(",", "");
                    if (vldStr.Length == 0) {
                        STAT |= CNV_STAT.ERR_ISEMPTY;
                        return STAT;
                    }
                    int strLen = vldStr.Length;
                    for (int i = 0; i < strLen; i++) {
                        if ("0123456789abcdefABCDEF".IndexOf(vldStr.ElementAt(i)) < 0) {
                            STAT |= CNV_STAT.ERR_HEXFORMAT;
                            return STAT;
                        }
                    }
                    if (strLen % 2 != 0) {
                        STAT |= CNV_STAT.ERR_BIT_LEN_MATCH;
                    }
                    if (bytArr == null || bytArr.Length == 0) {
                        int bytNum = strLen / 2 + (strLen % 2 > 0 ? 1 : 0);
                        bytArr = new byte[bytNum];
                    } else {
                        if (strLen / 2 != bytArr.Length) {
                            STAT |= CNV_STAT.ERR_BYTE_LEN_MATCH;
                        }
                        if (strLen / 2 > bytArr.Length) {
                            STAT |= CNV_STAT.ERR_BYTE_LEN_OVER;
                            return STAT;
                        }
                    }
                    for (int i = 0; i < bytArr.Length; i++) {
                        int subLn = strLen - i * 2 >= 2 ? 2 : 1;
                        string subStr = vldStr.Substring(i * 2, subLn);
                        bytArr[i] = Convert.ToByte(subStr, 16);
                    }
                    return STAT;
                case DspStrSty.Nom: // ASSIC码的可显示字符，没有最大长度限制，C#是基于Unicode码的字符串
                    if (iStr.Length == 0) {
                        STAT |= CNV_STAT.ERR_ISEMPTY;
                        return STAT;
                    } else {
                        if (bytArr == null || bytArr.Length == 0) {
                            bytArr = new byte[iStr.Length];
                        } else if (iStr.Length > bytArr.Length) {
                            STAT |= CNV_STAT.ERR_BYTE_LEN_OVER;
                            return STAT;
                        } else if (iStr.Length != bytArr.Length) {
                            STAT |= CNV_STAT.ERR_BYTE_LEN_MATCH;
                        }
                    }
                    STAT |= ASICCStringConvertToBytes(iStr, ref bytArr);
                    return STAT;
                default:
                    STAT |= CNV_STAT.ERR_INVALID_HANDLE;
                    return STAT;
            }
        }
        #endregion

        #region String Format
        /// <summary>
        /// 提取有效的数字字符，去掉空隔符，回车符，换行符，分隔符，退格符
        /// </summary>
        /// <param name="iString"></param>
        /// <returns></returns>
        public static string ExtractValidNumberCharacters(string iString) {
            if (iString == null) { return null; }
            if (iString == "") { return ""; }
            return iString.Replace(" ", "").Replace("\n", "").Replace("\r", "").Replace(",", "").ToUpper();
        }

        /// <summary>
        /// 光标位置重新调整
        /// </summary>
        /// <param name="iString"></param>
        /// <param name="iSelectionStart"></param>
        /// <param name="iByteSize"></param>
        /// <returns></returns>
        public static int RedressSelectionStartPosition(DspStrSty dspStrSty, string iString, int iSelectionStart) {
            if (iString == null) {
                return 0;
            }
            if (iString == "") {
                return 0;
            }
            if (iSelectionStart <= 0) {
                return 0;
            }
            switch (dspStrSty) {
                case DspStrSty.Nom:
                    return iSelectionStart;
                case DspStrSty.Hex:
                    int vldLen = ExtractValidNumberCharacters(iString.Substring(0, iSelectionStart)).Length;
                    return vldLen + vldLen / 4;
                case DspStrSty.Bsc:
                    return iSelectionStart;
                case DspStrSty.Password:
                    break;
                case DspStrSty.NA:
                    return iSelectionStart;
            }
            return iSelectionStart;
        }

        /// <summary>
        /// 呈现十六进制字符串，一般是两位（四个字符）加一空隔符
        /// </summary>
        /// <param name="iString"></param>
        /// <param name="iSeparationLength">Separation length</param>
        /// <returns></returns>
        public static string InsertSpaceToHexString(string iString, int iSeparationLength) {
            try {
                if (iString == null) {
                    return null;
                }
                if (iString == "") {
                    return "";
                }
                string str = "";
                for (int i = 0; i < iString.Length; i++) {
                    str += iString.ElementAt(i);
                    if ((i > 0) & ((i + 1) % (iSeparationLength * 2) == 0)) {
                        str += " ";
                    }
                }
                return str;
            } catch (Exception) {
                return null;
            }
        }

        /// <summary>
        /// 8位字节数组转Unicode字符串，BYTE数据长度等于字符串长度
        /// 此函用于转换读取的数据（RS232，IIC，SPI的ASCII码）给UI显示（Unicode）
        /// </summary>
        /// <param name="iBytes"></param>
        /// <returns></returns>
        public static CNV_STAT BytesConvertToASICCString(byte[] iBytes, ref string oString) {
            try {
                if (iBytes == null) {
                    return CNV_STAT.ERR_ISNULL;
                }
                if (iBytes.Length == 0) {
                    return CNV_STAT.ERR_ISEMPTY;
                }
                //return Encoding.ASCII.GetString(iBytes); // 如果Unicode字符中有非ASICC字符，则会有信息丢失，BYTE最大值为0x3F
                string str = Encoding.Default.GetString(iBytes);
                oString = str.Replace("\0", "\uFFFD");
                return CNV_STAT.OK;
            } catch (Exception ex) {
                MessageBox.Show("Error message: " + ex.Message, "Exception occurred");
                return CNV_STAT.CATCH_EXCEPTION;
            }
        }

        /// <summary>
        /// Unicode字符串转8位字节数组，BYTE数据长度等于字符串长度
        /// 此函用于转换UI显示（Unicode）到发送的数据（RS232，IIC，SPI的ASCII码）
        /// </summary>
        /// <param name="iString"></param>
        /// <returns></returns>
        public static CNV_STAT ASICCStringConvertToBytes(string iString, ref byte[] oBytes) {
            try {
                if (iString == null) {
                    return CNV_STAT.ERR_ISNULL;
                }
                if (iString == "") {
                    return CNV_STAT.ERR_ISEMPTY;
                }
                //return Encoding.ASCII.GetBytes(iString); // 如果Unicode字符中有非ASICC字符，则会有信息丢失，BYTE最大值为0x3F
                iString = iString.Replace("\uFFFD", "\0");
                oBytes = Encoding.Default.GetBytes(iString);
                return CNV_STAT.OK;
            } catch (Exception ex) {
                MessageBox.Show("Error message: " + ex.Message, "Exception occurred");
                return CNV_STAT.CATCH_EXCEPTION;
            }
        }

        public static byte[] MaxValueBytes(int bytesNumber) {
            switch (bytesNumber) {
                case 1: return new byte[1] { 0xFF };
                case 2: return new byte[2] { 0xFF, 0xFF };
                case 3: return new byte[3] { 0xFF, 0xFF, 0xFF };
                case 4: return new byte[4] { 0xFF, 0xFF, 0xFF, 0xFF };
                default: return new byte[0];
            }
        }

        public static int MaxValue(int bytesNumber) {
            switch (bytesNumber) {
                case 1: return unchecked((int)0x000000FF);
                case 2: return unchecked((int)0x0000FFFF);
                case 3: return unchecked((int)0x00FFFFFF);
                case 4: return unchecked((int)0xFFFFFFFF);
                default: return 0;
            }
        }

        public static DspDgtFmt GetDspValFmt(string formatString) {
            switch (formatString)// NA, Hex, Dec, Bin
            {

                case "Hex": return DspDgtFmt.Hex;
                case "Dec": return DspDgtFmt.Dec;
                case "Bin": return DspDgtFmt.Bin;
                case "N/A":
                default: return DspDgtFmt.NA; // Not Applicable
            }
        }

        public static DspStrSty GetDspArrFmt(string formatString) {
            switch (formatString)// NA, Hex, Char
            {
                case "Hex": return DspStrSty.Hex;
                case "Char": return DspStrSty.Nom;
                case "N/A":
                default: return DspStrSty.NA; // Not Applicable
            }
        }
        #endregion

        #region 为方便十六进制和正常显示转换的调用代码
        /// <summary>
        /// StringConvertToNormalString
        /// </summary>
        /// <param name="iHexStr">Hex String</param>
        /// <param name="oNomStr">Normal String</param>
        /// <returns></returns>
        public static CNV_STAT HexStringConvertToNormalString(string iHexStr, ref string oNomStr) {
            byte[] bytes = null;
            CNV_STAT STAT = StringConvertToBytes(DspStrSty.Hex, iHexStr, ref bytes);
            if (STAT != CNV_STAT.OK) {
                return STAT;
            }
            oNomStr = BytesConvertToString(DspStrSty.Nom, bytes);
            return CNV_STAT.OK;
        }

        /// <summary>
        /// NormalStringConvertToHexString
        /// </summary>
        /// <param name="iNomStr">Normal String</param>
        /// <param name="oHexStr">Hex String</param>
        /// <returns></returns>
        public static CNV_STAT NormalStringConvertToHexString(string iNomStr, ref string oHexStr) {
            byte[] bytes = null;
            CNV_STAT STAT = StringConvertToBytes(DspStrSty.Nom, iNomStr, ref bytes);
            if (STAT != CNV_STAT.OK) {
                return STAT;
            }
            oHexStr = BytesConvertToString(DspStrSty.Hex, bytes);
            return CNV_STAT.OK;
        }
        #endregion

        #region 为方便SMBus操作的数据转换
        /// <summary>
        /// 十六进制字符串转为1BYTE
        /// </summary>
        /// <param name="iString"></param>
        /// <param name="oByte"></param>
        /// <returns></returns>
        public static CNV_STAT StringConvertToByte(DspDgtFmt dspDgtFmt, string iString, ref byte oByte) {
            byte[] bytes = new byte[1];
            CNV_STAT STAT = StringConvertToBytes(dspDgtFmt, iString, ref bytes);
            if (STAT != CNV_STAT.OK) {
                return STAT;
            }
            oByte = bytes[0];
            return CNV_STAT.OK;
        }
        /// <summary>
        /// 十六进制字符串转为2BYTE(WORD)
        /// </summary>
        /// <param name="iString"></param>
        /// <param name="oByteL">LSB</param>
        /// <param name="oByteM">MSB</param>
        /// <returns></returns>
        public static CNV_STAT StringConvertToWord(DspDgtFmt dspDgtFmt, string iString, ref byte oByteL, ref byte oByteM) {
            byte[] bytes = new byte[2];
            CNV_STAT STAT = StringConvertToBytes(dspDgtFmt, iString, ref bytes);
            if (STAT != CNV_STAT.OK) {
                return STAT;
            }
            oByteL = bytes[0];
            oByteM = bytes[1];
            return CNV_STAT.OK;
        }
        /// <summary>
        /// 十六进制字符串转为4BYTE
        /// </summary>
        /// <param name="iString"></param>
        /// <param name="oByte1">LSB</param>
        /// <param name="oByte2"></param>
        /// <param name="oByte3"></param>
        /// <param name="oByte4">MSB</param>
        /// <returns></returns>
        public static CNV_STAT StringConvertToFourByte(DspDgtFmt dspDgtFmt, string iString, ref byte oByte1, ref byte oByte2, ref byte oByte3, ref byte oByte4) {
            byte[] bytes = new byte[4];
            CNV_STAT STAT = StringConvertToBytes(dspDgtFmt, iString, ref bytes);
            if (STAT != CNV_STAT.OK) {
                return STAT;
            }
            oByte1 = bytes[0];
            oByte2 = bytes[1];
            oByte3 = bytes[2];
            oByte4 = bytes[3];
            return CNV_STAT.OK;
        }

        /// <summary>
        /// 十六进制字符串转为8BYTE
        /// </summary>
        /// <param name="iString"></param>
        /// <param name="oByte1">LSB</param>
        /// <param name="oByte2"></param>
        /// <param name="oByte3"></param>
        /// <param name="oByte4"></param>
        /// <param name="oByte5"></param>
        /// <param name="oByte6"></param>
        /// <param name="oByte7"></param>
        /// <param name="oByte8">MSB</param>
        /// <returns></returns>
        public static CNV_STAT StringConvertToEightByte(DspDgtFmt dspDgtFmt, string iString, ref byte oByte1, ref byte oByte2, ref byte oByte3, ref byte oByte4, ref byte oByte5, ref byte oByte6, ref byte oByte7, ref byte oByte8) {
            byte[] bytes = new byte[8];
            CNV_STAT STAT = StringConvertToBytes(dspDgtFmt, iString, ref bytes);
            if (STAT != CNV_STAT.OK) {
                return STAT;
            }
            oByte1 = bytes[0];
            oByte2 = bytes[1];
            oByte3 = bytes[2];
            oByte4 = bytes[3];
            oByte5 = bytes[4];
            oByte6 = bytes[5];
            oByte7 = bytes[6];
            oByte8 = bytes[7];
            return CNV_STAT.OK;
        }

        /// <summary>
        /// 1BYTE转为十六进制字符串
        /// </summary>
        /// <param name="iBytes"></param>
        /// <returns></returns>
        public static CNV_STAT ByteConvertToString(CnvLib.DspDgtFmt dspDgtFmt, byte iByte, ref string oStr) {
            ulong val = iByte;
            CNV_STAT STAT = ValueConvertToString(dspDgtFmt, val, 1 * 8, ref oStr);
            if (STAT != CNV_STAT.OK) {
                return STAT;
            }
            return CNV_STAT.OK;
        }

        /// <summary>
        /// 2BYTE(WORD)转为十六进制字符串
        /// </summary>
        /// <param name="iBytes"></param>
        /// <returns></returns>
        public static CNV_STAT WordByteConvertToString(CnvLib.DspDgtFmt dspDgtFmt, byte iByteL, byte iByteM, ref string oStr) {
            ulong val = (ulong)iByteM << 08 | iByteL;
            CNV_STAT STAT = ValueConvertToString(dspDgtFmt, val, 2 * 8, ref oStr);
            if (STAT != CNV_STAT.OK) {
                return STAT;
            }
            return CNV_STAT.OK;
        }

        /// <summary>
        /// 4BYTE转为十六进制字符串
        /// </summary>
        /// <param name="iBytes"></param>
        /// <returns></returns>
        public static CNV_STAT ForeByteConvertToString(CnvLib.DspDgtFmt dspDgtFmt, byte iByte1, byte iByte2, byte iByte3, byte iByte4, ref string oStr) {
            ulong val = (ulong)iByte4 << 24 | (ulong)iByte3 << 16 | (ulong)iByte2 << 08 | iByte1;
            CNV_STAT STAT = ValueConvertToString(dspDgtFmt, val, 4 * 8, ref oStr);
            if (STAT != CNV_STAT.OK) {
                return STAT;
            }
            return CNV_STAT.OK;
        }

        /// <summary>
        /// 8BYTE转为十六进制字符串
        /// </summary>
        /// <param name="iBytes"></param>
        /// <returns></returns>
        public static CNV_STAT EightByteConvertToString(CnvLib.DspDgtFmt dspDgtFmt, byte iByte1, byte iByte2, byte iByte3, byte iByte4, byte iByte5, byte iByte6, byte iByte7, byte iByte8, ref string oStr) {
            ulong val = (ulong)iByte8 << 56 | (ulong)iByte7 << 48 | (ulong)iByte6 << 40 | (ulong)iByte5 << 32 | (ulong)iByte4 << 24 | (ulong)iByte3 << 16 | (ulong)iByte2 << 08 | iByte1;
            CNV_STAT STAT = ValueConvertToString(dspDgtFmt, val, 8 * 8, ref oStr);
            if (STAT != CNV_STAT.OK) {
                return STAT;
            }
            return CNV_STAT.OK;
        }
        #endregion

        #region Exception Handle
        private readonly static bool chs = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "zh"; // 是否为中文

        /// <summary>
        /// ExceptionMessage
        /// </summary>
        /// <param name="iType"></param>
        /// <param name="iException"></param>
        public static void ExceptionMessage(CNV_STAT iState) {
            string cpt = "User Configuration Exception";
            string pcsMsg = "ExceptionCode: 0x" + ((uint)iState).ToString("X8") + "\r\n\r\n";
            if ((iState & CNV_STAT.SYSTEM_ERROR) != 0) {
                pcsMsg += "SYSTEM_ERROR and ";
                pcsMsg += (iState & CNV_STAT.CATCH_EXCEPTION) != 0 ? (chs ? CATCH_EXCEPTION_CH : CATCH_EXCEPTION_EN) : "";
            } else {
                pcsMsg += (iState & CNV_STAT.ERR_ISNULL) != 0 ? (chs ? ERR_ISNULL_CH : ERR_ISNULL_EN) : "";
                pcsMsg += (iState & CNV_STAT.ERR_ISEMPTY) != 0 ? (chs ? ERR_ISEMPTY_CH : ERR_ISEMPTY_EN) : "";
                pcsMsg += (iState & CNV_STAT.ERR_BIT_LEN_MATCH) != 0 ? (chs ? ERR_BITMATCH_CH : ERR_BITMATCH_EN) : "";
                pcsMsg += (iState & CNV_STAT.ERR_BYTE_LEN_OVER) != 0 ? (chs ? ERR_BYTENUMBER_CH : ERR_BYTENUMBER_EN) : "";
                pcsMsg += (iState & CNV_STAT.ERR_HEXFORMAT) != 0 ? (chs ? ERR_HEXFORMAT_CH : ERR_HEXFORMAT_EN) : "";
                pcsMsg += (iState & CNV_STAT.ERR_BINFORMAT) != 0 ? (chs ? ERR_BINFORMAT_CH : ERR_BINFORMAT_EN) : "";
                pcsMsg += (iState & CNV_STAT.ERR_DECFORMAT) != 0 ? (chs ? ERR_DECFORMAT_CH : ERR_DECFORMAT_EN) : "";
                pcsMsg += (iState & CNV_STAT.ERR_ASSICFORMAT) != 0 ? (chs ? ERR_ASSICFORMAT_CH : ERR_ASSICFORMAT_EN) : "";
            }
            MessageBox.Show(pcsMsg, cpt);

            //MessageBox.Show("粘贴数据含有非法字符，只能包含数字0-9,大写英文字母A-F,小写英文字母a-f以及空格！", "非法的粘贴", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //MessageBox.Show("粘贴数据含有非法字符，只能包含ASCII码字符！", "非法的粘贴", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        #endregion

    }
}
