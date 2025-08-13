using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using ComCls;

namespace UsrCtl {

    /// <summary>
    /// Interaction logic for DigitalFormatBox.xaml
    /// </summary>
    public partial class DigitalFormatTextBox : TextBox {

        #region Field
        private bool appInit = false;

        /* 记住上一次输入OK状态，编辑中 */
        private string lastLegalText = "";
        private int preSlcStr = 0;

        private bool mskTxtCng = false; // 代码更改文本，屏幕事件，让文本更改事件只受控于介面输入。

        private bool mskValCng = false; //
        #endregion

        #region Property
        /// <summary>
        /// CallOnceValueChangeEvent
        /// </summary>
        public bool MaskValueChangeEvent {
            //get { return mskValCng; }
            set { mskValCng = value; }
        }
        #endregion

        #region DependencyProperty
        #region MinValueProperty
        /// <summary>
        /// MinValue
        /// </summary>
        public ulong MinValue {
            get { return (ulong)GetValue(MinValueProperty); }
            set { SetValue(MinValueProperty, value); }
        }

        /// <summary>
        /// MinValueProperty
        /// </summary>
        public static readonly DependencyProperty MinValueProperty = DependencyProperty.Register(
            "MinValue",
            typeof(ulong),
            typeof(DigitalFormatTextBox),
            new FrameworkPropertyMetadata(
                ulong.MinValue,
                new PropertyChangedCallback(MinValuePropertyChangedCallback),
                new CoerceValueCallback(MinValueCoerceCallback)));

        /// <summary>
        /// MinValuePropertyChangedCallback
        /// </summary>
        private static void MinValuePropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            DigitalFormatTextBox obj = (DigitalFormatTextBox)sender;
            ulong minVal = (ulong)e.NewValue;
            if (obj.Value < minVal) {
                obj.Value = minVal;
            }
            obj.UpdateDisplay();
        }

        /// <summary>
        /// MaxValueCoerceCallback
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static object MinValueCoerceCallback(DependencyObject sender, object baseValue) {
            DigitalFormatTextBox obj = (DigitalFormatTextBox)sender;
            ulong newValue = (ulong)baseValue;
            if (newValue > obj.MaxValue) {
                return obj.MaxValue;
            }
            return newValue;
        }
        #endregion

        #region MaxValueProperty
        /// <summary>
        /// MaxValue
        /// </summary>
        public ulong MaxValue {
            get { return (ulong)GetValue(MaxValueProperty); }
            set { SetValue(MaxValueProperty, value); }
        }

        /// <summary>
        /// MaxValueProperty
        /// </summary>
        public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register(
            "MaxValue",
            typeof(ulong),
            typeof(DigitalFormatTextBox),
            new FrameworkPropertyMetadata(
                ulong.MaxValue,
                new PropertyChangedCallback(MaxValuePropertyChangedCallback),
                new CoerceValueCallback(MaxValueCoerceCallback)));

        /// <summary>
        /// MaxValuePropertyChangedCallback
        /// </summary>
        private static void MaxValuePropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            DigitalFormatTextBox obj = (DigitalFormatTextBox)sender;
            ulong maxVal = (ulong)e.NewValue;
            if (obj.Value > maxVal) {
                obj.Value = maxVal;
            }
            obj.UpdateDisplay();
        }

        /// <summary>
        /// MaxValueCoerceCallback
        /// </summary>
        public static object MaxValueCoerceCallback(DependencyObject sender, object baseValue) {
            DigitalFormatTextBox obj = (DigitalFormatTextBox)sender;
            ulong newValue = (ulong)baseValue;
            if (newValue < obj.MinValue) {
                return obj.MinValue;
            }
            return newValue;
        }
        #endregion

        #region ByteLengthProperty
        /// <summary>
        /// ByteLength
        /// </summary>
        public ulong ByteLength {
            set { SetValue(ByteLengthProperty, value); }
        }

        /// <summary>
        /// ByteLengthProperty
        /// </summary>
        public static readonly DependencyProperty ByteLengthProperty = DependencyProperty.Register(
            "ByteLength",
            typeof(ulong),
            typeof(DigitalFormatTextBox),
            new FrameworkPropertyMetadata(
                ulong.MinValue,
                new PropertyChangedCallback(ByteLengthPropertyChangedCallback),
                new CoerceValueCallback(ValueCoerceCallback)));

        /// <summary>
        /// ByteLengthPropertyChangedCallback
        /// </summary>
        private static void ByteLengthPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            DigitalFormatTextBox obj = (DigitalFormatTextBox)sender;
            ulong newVal = (ulong)e.NewValue;
            switch (newVal) {
                case 0: obj.MaxValue = 0x00; break;
                case 1: obj.MaxValue = 0xFF; break;
                case 2: obj.MaxValue = 0xFFFF; break;
                case 3: obj.MaxValue = 0xFFFFFF; break;
                case 4: obj.MaxValue = 0xFFFFFFFF; break;
                case 5: obj.MaxValue = 0xFFFFFFFFFF; break;
                case 6: obj.MaxValue = 0xFFFFFFFFFFFF; break;
                case 7: obj.MaxValue = 0xFFFFFFFFFFFFFF; break;
                case 8: obj.MaxValue = 0xFFFFFFFFFFFFFFFF; break;
                default: obj.MaxValue = 0x00; break;
            }
        }

        /// <summary>
        /// ByteLengthValidateCallback
        /// </summary>
        public static bool ByteLengthValidateCallback(object value) {
            double val = (double)value;
            return !val.Equals(double.NegativeInfinity) &&
                !val.Equals(double.PositiveInfinity);
        }
        #endregion

        #region ValueProperty
        /// <summary>
        /// Value
        /// </summary>
        public ulong Value {
            get { return (ulong)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        /// <summary>
        /// ValueProperty
        /// </summary>
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            "Value",
            typeof(ulong),
            typeof(DigitalFormatTextBox),
            new FrameworkPropertyMetadata(
                ulong.MinValue,
                new PropertyChangedCallback(ValuePropertyChangedCallback),
                new CoerceValueCallback(ValueCoerceCallback)),
            new ValidateValueCallback(ValueValidateCallback));

        /// <summary>
        /// ValuePropertyChangedCallback
        /// </summary>
        private static void ValuePropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            DigitalFormatTextBox obj = (DigitalFormatTextBox)sender;
            ulong oldVal = (ulong)e.OldValue;
            ulong newVal = (ulong)e.NewValue;

            if (!obj.mskValCng) {
                obj.OnValueChangedEvent(oldVal, newVal); // 调用值更改事件
            }
            obj.UpdateDisplay(); // 在属性值更新后，更新显示
        }

        /// <summary>
        /// ValueCoerceCallback
        /// </summary>
        public static object ValueCoerceCallback(DependencyObject sender, object baseValue) {
            DigitalFormatTextBox obj = (DigitalFormatTextBox)sender;
            ulong newValue = (ulong)baseValue;
            if (newValue < obj.MinValue) {
                return obj.MinValue;
            }
            if (newValue > obj.MaxValue) {
                return obj.MaxValue;
            }
            return newValue;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool ValueValidateCallback(object value) {
            return true;
        }
        #endregion

        #region DigitalFormatProperty
        /// <summary>
        /// DigitalFormat
        /// </summary>
        public CnvLib.DspDgtFmt DigitalFormat {
            get { return (CnvLib.DspDgtFmt)GetValue(DigitalFormatProperty); }
            set { SetValue(DigitalFormatProperty, value); }
        }

        /// <summary>
        /// DigitalFormatProperty
        /// </summary>
        public static readonly DependencyProperty DigitalFormatProperty = DependencyProperty.Register(
            "DigitalFormat",
            typeof(CnvLib.DspDgtFmt),
            typeof(DigitalFormatTextBox),
            new FrameworkPropertyMetadata(
                CnvLib.DspDgtFmt.Dec,
                new PropertyChangedCallback(DigitalFormatPropertyChangedCallback)),
            new ValidateValueCallback(ValidateValueCallback));

        /// <summary>
        /// DigitalFormatPropertyChangedCallback
        /// </summary>
        private static void DigitalFormatPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            DigitalFormatTextBox obj = (DigitalFormatTextBox)sender;

            //CnvLib.DspDgtFmt newDgtFmt = (CnvLib.DspDgtFmt)e.NewValue;
            obj.UpdateDisplay();
        }

        /// <summary>
        /// ValidateValueCallback
        /// </summary>
        private static bool ValidateValueCallback(object value) {
            return true;
        }
        #endregion

        #region DecimalFormat
        public CnvLib.DecimalFormat DecimalFormat {
            get { return (CnvLib.DecimalFormat)GetValue(DecimalFormatProperty); }
            set { SetValue(DecimalFormatProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DecimalFormat.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DecimalFormatProperty =
            DependencyProperty.Register(
                "DecimalFormat",
                typeof(CnvLib.DecimalFormat),
                typeof(DigitalFormatTextBox),
                 new PropertyMetadata(CnvLib.DecimalFormat.U64, new PropertyChangedCallback(DecimalFormatPropertyChangedCallback)));

        /// <summary>
        /// DecimalFormatPropertyChangedCallback
        /// </summary>
        private static void DecimalFormatPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            DigitalFormatTextBox obj = (DigitalFormatTextBox)sender;
            obj.UpdateDisplay();
        }
        #endregion

        #region ReadOnlyProperty
        /// <summary>
        /// ReadOnly
        /// </summary>
        public bool ReadOnly {
            get { return (bool)GetValue(ReadOnlyProperty); }
            set { SetValue(ReadOnlyProperty, value); }
        }

        /// <summary>
        /// ReadOnly
        /// </summary>
        public static readonly DependencyProperty ReadOnlyProperty = DependencyProperty.Register(
            "ReadOnly",
            typeof(bool),
            typeof(DigitalFormatTextBox),
            new PropertyMetadata(false, new PropertyChangedCallback(ReadOnlyPropertyChangedCallback)));

        /// <summary>
        /// ReadOnlyPropertyChangedCallback
        /// </summary>
        private static void ReadOnlyPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            DigitalFormatTextBox obj = (DigitalFormatTextBox)sender;
            bool newRdOly = (bool)e.NewValue;
            obj.IsReadOnly = newRdOly;
            obj.Background = newRdOly ? new SolidColorBrush(Color.FromArgb(0x10, 0x00, 0x00, 0x00)) : Brushes.White; // 
        }
        #endregion

        #region IsNaNProperty
        /// <summary>
        /// IsNaN
        /// </summary>
        public bool IsNaN {
            get { return (bool)GetValue(IsNaNProperty); }
            set { SetValue(IsNaNProperty, value); }
        }

        /// <summary>
        /// IsNaNProperty
        /// </summary>
        public static readonly DependencyProperty IsNaNProperty = DependencyProperty.Register(
            "IsNaN",
            typeof(bool),
            typeof(DigitalFormatTextBox),
            new PropertyMetadata(false, new PropertyChangedCallback(IsNaNPropertyChangedCallback)));

        /// <summary>
        /// IsNaNPropertyChangedCallback
        /// </summary>
        private static void IsNaNPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            DigitalFormatTextBox obj = (DigitalFormatTextBox)sender;
            bool newVal = (bool)e.NewValue;
            if (newVal) { obj.Text = "-"; }
        }
        #endregion
        #endregion DependencyProperty

        #region Constructor
        /// <summary>
        /// DigitalFormatTextBox
        /// </summary>
        public DigitalFormatTextBox() {
            CharacterCasing = CharacterCasing.Upper; // Digital Format is Upper
            InputMethod.SetPreferredImeState(this, InputMethodState.Off); // Close Input Method
            FontFamily = new FontFamily("Consolas");
            FontSize = 13;
            TextAlignment = TextAlignment.Center;
            TextWrapping = TextWrapping.Wrap;
            Margin = new Thickness(0);
            appInit = true;
        }

        /// <summary>
        /// DigitalFormatTextBox
        /// </summary>
        static DigitalFormatTextBox() {


        }
        #endregion

        #region Event
        #region ValueChangedEvent
        /// <summary>
        /// ValueChangedEvent
        /// </summary>
        public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(
            "ValueChanged",
            RoutingStrategy.Bubble,
            typeof(RoutedPropertyChangedEventHandler<ulong>), typeof(DigitalFormatTextBox));

        /// <summary>
        /// ValueChanged
        /// </summary>
        public event RoutedPropertyChangedEventHandler<ulong> ValueChanged {
            add { this.AddHandler(ValueChangedEvent, value); }
            remove { this.RemoveHandler(ValueChangedEvent, value); }
        }

        /// <summary>
        /// OnValueChangedEvent
        /// </summary>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        protected virtual void OnValueChangedEvent(ulong oldValue, ulong newValue) {
            RoutedPropertyChangedEventArgs<ulong> arg =
                new RoutedPropertyChangedEventArgs<ulong>(oldValue, newValue, ValueChangedEvent);
            this.RaiseEvent(arg);
        }
        #endregion
        #endregion

        #region UI Control Event
        /// <summary>
        /// Input Char/Paste Text/Load Config Format
        /// </summary>
        protected override void OnTextChanged(TextChangedEventArgs e) {
            if (!appInit | mskTxtCng) {
                return;
            }
            if (IsNaN) {
                mskTxtCng = true;
                Text = "-";
                mskTxtCng = false;
                return;
            }
            string vldStr = Text.Replace(" ", "");
            if (!string.IsNullOrEmpty(vldStr)) {
                switch (DigitalFormat) {
                    case CnvLib.DspDgtFmt.Hex:
                        for (int i = 0; i < vldStr.Length; i++) {
                            if ("0123456789abcdefABCDEF".IndexOf(vldStr.ElementAt(i)) < 0) {
                                mskTxtCng = true;
                                Text = lastLegalText;
                                mskTxtCng = false;
                                SelectionStart = preSlcStr;
                                return;
                            }
                        }
                        break;
                    case CnvLib.DspDgtFmt.Dec:
                        for (int i = 0; i < vldStr.Length; i++) {
                            bool u = DecimalFormat == CnvLib.DecimalFormat.U8 || DecimalFormat == CnvLib.DecimalFormat.U16 || DecimalFormat == CnvLib.DecimalFormat.U32 || DecimalFormat == CnvLib.DecimalFormat.U64;
                            if ((u ? "0123456789" : "-0123456789").IndexOf(vldStr.ElementAt(i)) < 0) {
                                mskTxtCng = true;
                                Text = lastLegalText;
                                mskTxtCng = false;
                                SelectionStart = preSlcStr;
                                return;
                            }
                        }
                        break;
                    case CnvLib.DspDgtFmt.Bin:
                        for (int i = 0; i < vldStr.Length; i++) {
                            if ("01".IndexOf(vldStr.ElementAt(i)) < 0) {
                                mskTxtCng = true;
                                Text = lastLegalText;
                                mskTxtCng = false;
                                SelectionStart = preSlcStr;
                                return;
                            }
                        }
                        break;
                    default:
                        return;
                }
            }
            lastLegalText = vldStr;
            preSlcStr = SelectionStart;
        }

        /// <summary>
        /// Format Display
        /// </summary>
        protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e) {
            try {
                if (IsNaN) {
                    mskTxtCng = true;
                    Text = "-";
                    mskTxtCng = false;
                    return;
                }
                string vldStr = Text.Replace(" ", "");
                if (string.IsNullOrEmpty(vldStr)) {
                    UpdateDisplay(); // 用原来的属性值更新显示
                    return;
                }
                ulong newVal;
                switch (DigitalFormat) {
                    case CnvLib.DspDgtFmt.Hex:
                        for (int i = 0; i < vldStr.Length; i++) {
                            if ("0123456789abcdefABCDEF".IndexOf(vldStr.ElementAt(i)) < 0) {
                                UpdateDisplay(); // 用原来的属性值更新显示
                                return;
                            }
                        }
                        newVal = Convert.ToUInt64(vldStr, 16);
                        break;
                    case CnvLib.DspDgtFmt.Dec:
                        for (int i = 0; i < vldStr.Length; i++) {
                            bool u = DecimalFormat == CnvLib.DecimalFormat.U8 || DecimalFormat == CnvLib.DecimalFormat.U16 || DecimalFormat == CnvLib.DecimalFormat.U32 || DecimalFormat == CnvLib.DecimalFormat.U64;
                            if ((u ? "0123456789" : "-0123456789").IndexOf(vldStr.ElementAt(i)) < 0) {
                                UpdateDisplay(); // 用原来的属性值更新显示
                                return;
                            }
                        }
                        switch (DecimalFormat) {
                            case CnvLib.DecimalFormat.U8: newVal = Convert.ToUInt64(vldStr); break;
                            case CnvLib.DecimalFormat.I8: newVal = (byte)Convert.ToInt64(vldStr); break;
                            case CnvLib.DecimalFormat.U16: newVal = Convert.ToUInt64(vldStr); break;
                            case CnvLib.DecimalFormat.I16: newVal = (ushort)Convert.ToInt64(vldStr); break;
                            case CnvLib.DecimalFormat.U32: newVal = Convert.ToUInt64(vldStr); break;
                            case CnvLib.DecimalFormat.I32: newVal = (uint)Convert.ToInt64(vldStr); break;
                            case CnvLib.DecimalFormat.U64: newVal = Convert.ToUInt64(vldStr); break;
                            case CnvLib.DecimalFormat.I64: newVal = (ulong)Convert.ToInt64(vldStr); break;
                            default: newVal = Convert.ToUInt64(vldStr); break;
                        }
                        break;
                    case CnvLib.DspDgtFmt.Bin:
                        for (int i = 0; i < vldStr.Length; i++) {
                            if ("01".IndexOf(vldStr.ElementAt(i)) < 0) {
                                UpdateDisplay(); // 用原来的属性值更新显示
                                return;
                            }
                        }
                        newVal = Convert.ToUInt64(vldStr, 2);
                        break;
                    default:
                        return;
                }
                if (newVal < MinValue || newVal > MaxValue) {
                    UpdateDisplay(); // 用原来的属性值更新显示
                    MessageBox.Show("The input value is out of range");
                    return;
                }
                ulong oldVal = Value; // 值更改事件的老值参数
                Value = newVal; // 更新属性值，在事件之前，可能用到此属性在事件中
                //if (oldVal != Value) {

                //}
                OnValueChangedEvent(oldVal, newVal); // 调用值更改事件
                UpdateDisplay(); // 在属性值更新后，更新显示
            } catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
        }
        #endregion

        #region Private Function
        /// <summary>
        /// UpdateDisplay
        /// </summary>
        private void UpdateDisplay() {
            if (IsNaN) {
                mskTxtCng = true;
                Text = "-";
                mskTxtCng = false;
                return;
            }
            MaxLength = GetUlongMaxLength(DigitalFormat, MaxValue);
            string str;
            switch (DigitalFormat) {
                case CnvLib.DspDgtFmt.Dec:
                    switch (DecimalFormat) {
                        case CnvLib.DecimalFormat.U8: str = ((byte)Value).ToString(); break;
                        case CnvLib.DecimalFormat.I8: str = ((sbyte)Value).ToString(); break;
                        case CnvLib.DecimalFormat.U16: str = ((ushort)Value).ToString(); break;
                        case CnvLib.DecimalFormat.I16: str = ((short)Value).ToString(); break;
                        case CnvLib.DecimalFormat.U32: str = ((int)Value).ToString(); break;
                        case CnvLib.DecimalFormat.I32: str = ((uint)Value).ToString(); break;
                        case CnvLib.DecimalFormat.U64: str = ((ulong)Value).ToString(); break;
                        case CnvLib.DecimalFormat.I64: str = ((long)Value).ToString(); break;
                        default: str = ((ulong)Value).ToString(); break;
                    }
                    break;
                case CnvLib.DspDgtFmt.Hex:
                    str = Value.ToString("X").ToUpper();
                    while (str.Length < MaxLength) {
                        str = "0" + str;
                    }
                    break;
                case CnvLib.DspDgtFmt.Bin:
                    str = UlongToBinSring(Value, MaxLength);
                    break;
                default:
                    return;
            }
            lastLegalText = str;
            preSlcStr = SelectionStart;
            mskTxtCng = true;
            Text = str;
            mskTxtCng = false;
            SelectionStart = str.Length;
        }

        /// <summary>
        /// GetUlongMaxLength
        /// </summary>
        /// <param name="dgtFmt"></param>
        /// <param name="maxValue"></param>
        /// <returns></returns>
        private int GetUlongMaxLength(CnvLib.DspDgtFmt dgtFmt, ulong maxValue) {
            int maxLen = 0;
            switch (DigitalFormat) {
                case CnvLib.DspDgtFmt.Dec:
                    switch (DecimalFormat) {
                        case CnvLib.DecimalFormat.U8: maxLen = 10; break;
                        case CnvLib.DecimalFormat.I8: maxLen = 10; break;
                        case CnvLib.DecimalFormat.U16: maxLen = 10; break;
                        case CnvLib.DecimalFormat.I16: maxLen = 10; break;
                        case CnvLib.DecimalFormat.U32: maxLen = 10; break;
                        case CnvLib.DecimalFormat.I32: maxLen = 10; break;
                        case CnvLib.DecimalFormat.U64: maxLen = 10; break;
                        case CnvLib.DecimalFormat.I64: maxLen = 10; break;
                        default: maxLen = 10; break;
                    }
                    break;
                case CnvLib.DspDgtFmt.Hex:
                    for (int i = 0; i < 17; i++) {
                        maxLen = i;
                        if (MaxValue >> 4 * i == 0) {
                            break;
                        }
                    }
                    break;
                case CnvLib.DspDgtFmt.Bin:
                    for (int i = 0; i < 64; i++) {
                        maxLen = i;
                        if (MaxValue >> i == 0) {
                            break;
                        }
                    }
                    break;
                default:
                    break;
            }
            return maxLen;
        }

        /// <summary>
        /// UlongToBinSring
        /// </summary>
        /// <param name="Value"></param>
        /// <param name="MaxLength"></param>
        /// <returns></returns>
        private string UlongToBinSring(ulong Value, int MaxLength) {
            string str = "";
            for (int i = 0; i < MaxLength; i++) {
                str = ((Value & 1UL << i) > 0 ? "1" : "0") + str;
            }
            return str;
        }
        #endregion

    }
}

