using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.UI
{
    internal class CommandLineParser
    {

        public static int[] Parse(string[] args)
        {
            string input = args[0];
            List<int> numbers = new List<int>();

            if (input.Contains(';'))
            {
                string[] parts = input.Split(';');
                foreach (string part in parts)
                {
                    numbers.Add(int.Parse(part));
                }
            }
            else if (input.Contains('-'))
            {
                string[] parts = input.Split('-');
                Console.WriteLine(parts.Length);
                if (parts.Length != 2)
                {
                    throw new ArgumentException();
                }
                int start = int.Parse(parts[0]);
                int end = int.Parse(parts[1]);

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
