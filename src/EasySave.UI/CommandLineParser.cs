using System;
using System.Collections.Generic;
using System.Linq;

namespace EasySave.UI
{
    public class CommandLineParser
    {

        /// <summary>
        /// Parse the arguments for list the job's ids
        /// </summary>
        /// <param name="args"> The args of the command</param>
        /// <returns>The complete list of job's index</returns>
        public int[] Parse(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                throw new ArgumentException("The argument array must not be null or empty.", nameof(args));
            }
            string input = args[0];
            List<int> numbers = new List<int>();

            if (input.Contains(';'))
            {
                string[] parts = input.Split(';');

                if (parts.Any(string.IsNullOrWhiteSpace))
                {
                    throw new ArgumentException();
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
                    throw new ArgumentException();
                }
                
                int start = int.Parse(parts[0]);
                int end = int.Parse(parts[1]);

                if (end < start)
                {
                    throw new ArgumentException();
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
