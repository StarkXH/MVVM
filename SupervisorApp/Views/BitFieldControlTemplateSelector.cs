using SupervisorApp.ViewModels;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SupervisorApp.Views
{
    public class BitFieldControlTemplateSelector : DataTemplateSelector
    {
        public DataTemplate SingleBitTemplate { get; set; }
        public DataTemplate MultiBitTemplate { get; set; }
        public DataTemplate ValueMappingTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is BitFieldItemViewModel bitFieldVM)
            {
                // 如果有值映射，使用ComboBox
                if (bitFieldVM.BitField.ValueMappings?.Any() == true)
                {
                    return ValueMappingTemplate;
                }

                // 单个位使用CheckBox
                if (bitFieldVM.BitWidth == 1)
                {
                    return SingleBitTemplate;
                }

                // 多位使用DigitalFormatTextBox
                return MultiBitTemplate;
            }

            return base.SelectTemplate(item, container);
        }
    }
}
