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
        protected int _errorCode = 0;
        protected string _message = string.Empty;
        protected string[] _options = [];

        public EasysaveDefaultException(string message,string errorKey, int errorCode ,string[] options)
        {
            _errorKey = errorKey;
            _errorCode = errorCode;
           _message = message;
            _options = options;

        }

        public string getTranslatedTexte()
        {
            throw new NotImplementedException();
        }
    }
}
