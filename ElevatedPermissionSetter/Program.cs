using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ElevatedPermissionSetter
{
    class Program
    {
        static void Main(string[] args)
        {
            bool admin = args[0] == "ADMIN";
            string toAddToPath = args[1];
            EnvironmentVariableTarget target = admin ? EnvironmentVariableTarget.Machine : EnvironmentVariableTarget.User;

            string currentPath = Environment.GetEnvironmentVariable("PATH", target);
            if (!currentPath.Contains(toAddToPath))
            {
                currentPath += toAddToPath;
                Environment.SetEnvironmentVariable("PATH", currentPath, target);
            }

            for (int i = 2; i < args.Length; i++)
            {
                string[] split = args[i].Split(new char[] { ':' }, 2);
                if (split.Length != 2)
                {
                    continue;
                }
                Environment.SetEnvironmentVariable(split[0], split[1], target);
            }
        }
    }
}
