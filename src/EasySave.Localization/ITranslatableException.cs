using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.Localization
{
    public interface ITranslatableException
    {
        public LocalizationKey ErrorKey { get; }
        public List<string> Options { get; }
        public string Details { get; }
    }
}
