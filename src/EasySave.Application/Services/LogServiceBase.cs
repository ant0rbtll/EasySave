using EasySave.Application.Readers;
using EasySave.Configuration;
using EasySave.Core;
using EasySave.Core.Exceptions;
using EasySave.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.Application.Services
{
    public abstract class LogServiceBase
    {
        protected readonly IPathProvider _pathProvider;
        protected readonly IReadOnlyDictionary<LogFormat, ILogReader> _readerByFormat;
        public LogServiceBase(IPathProvider pathProvider, IEnumerable<ILogReader> readers)
        {
            EasysaveDefaultException.ThrowIfNull(pathProvider);
            EasysaveDefaultException.ThrowIfNull(readers);

            _pathProvider = pathProvider;

            var grouped = readers.GroupBy(static r => r.Format).ToList();
            var duplicate = grouped.FirstOrDefault(static g => g.Count() > 1);
            if (duplicate is not null)
            {
                throw new EasysaveDefaultException("error_multiple_readers_registred", [duplicate.Key.ToString()]);
            }

            _readerByFormat = grouped.ToDictionary(static g => g.Key, static g => g.Single());
        }
    }
}
