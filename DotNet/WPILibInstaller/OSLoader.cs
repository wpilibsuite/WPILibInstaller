using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WPILibInstaller
{
    public class OSLoader
    {
        private static bool Is64BitOs()
        {
            return IntPtr.Size != sizeof(int);
        }

        public static bool IsWindows()
        {
            return Path.DirectorySeparatorChar == '\\';
        }

        /// <summary>
        /// Gets the OS Type of the current running system.
        /// </summary>
        /// <returns></returns>
        public static OsType GetOsType()
        {
            if (IsWindows())
            {
                return Is64BitOs() ? OsType.Windows64 : OsType.Windows32;
            }
            else
            {
                Utsname uname;
                try
                {
                    Uname.uname(out uname);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return OsType.None;
                }

                bool mac = uname.sysname == "Darwin";

                //Check for Bitness
                if (Is64BitOs())
                {
                    //We are 64 bit.
                    if (mac) return OsType.MacOs64;
                    return OsType.Linux64;
                }
                else
                {
                    throw new Exception("Unsupported Operating System");
                }
            }
        }
    }
    /// <summary>
    /// Enumeration of the OS type for this system.
    /// </summary>
    public enum OsType
    {
        /// <summary>
        /// OS Type not found
        /// </summary>
        None,
        /// <summary>
        /// Windows 32 bit
        /// </summary>
        Windows32,
        /// <summary>
        /// Windows 64 bit
        /// </summary>
        Windows64,
        /// <summary>
        /// Linux 64 bit
        /// </summary>
        Linux64,
        /// <summary>
        /// Mac OS 64 bit
        /// </summary>
        MacOs64,
    }
}
