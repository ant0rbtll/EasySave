using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.Core.Exceptions
{
    public class EasysaveDefaultException : SystemException
    {
        protected string _errorKey = string.Empty;
        protected List<string> _options = new();
        protected string _details = "";

        public EasysaveDefaultException(string errorKey, List<string> options, string details = "")
        {
            _errorKey = errorKey;
            _options = options;
            _details = details;
        }

        public string getTranslatedTexte()
        {
            throw new NotImplementedException();
        }
    }
}
