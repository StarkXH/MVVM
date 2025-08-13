using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoIoc
{
    public interface ITextService
    {
        string GetText();
    }

    class TextService : ITextService
    {
        public string GetText()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }
    }

}
