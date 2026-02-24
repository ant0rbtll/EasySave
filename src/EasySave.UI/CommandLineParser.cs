using EasySave.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EasySave.UI
{
    /// <summary>
    /// Parses command-line arguments to extract backup job identifiers.
    /// </summary>
    public class CommandLineParser
    {
        /// <summary>
        /// Parses the command-line arguments to extract job identifiers.
        /// Supports individual IDs (e.g., "1"), ranges (e.g., "1-3"), and semicolon-separated lists (e.g., "1;3;5").
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        /// <returns>An array of parsed job identifiers.</returns>
        public int[] Parse(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                throw new InvalidArgumentException(nameof(args),"The argument array must not be null or empty.");
            }
            string input = args[0];
            List<int> numbers = new List<int>();

            if (input.Contains(';'))
            {
                string[] parts = input.Split(';');

                if (parts.Any(string.IsNullOrWhiteSpace))
                {
                    throw new InvalidArgumentException(nameof(parts));
                }

                foreach (string part in parts)
                {
                    numbers.Add(int.Parse(part));
                }
            }
            else if (input.Contains('-'))
            {
                string[] parts = input.Split('-');
                if (parts.Length != 2 || parts.Any(string.IsNullOrWhiteSpace))
                {
                    throw new InvalidArgumentException(nameof(parts));
                }

                int start = int.Parse(parts[0]);
                int end = int.Parse(parts[1]);

                if (end < start)
                {
                    throw new InvalidArgumentException(nameof(parts));
                }

                for (int i = start; i <= end; i++)
                {
                    numbers.Add(i);
                }
            }
            else
            {
                numbers.Add(int.Parse(input));
            }
            return numbers.ToArray();
        }
    }
}
