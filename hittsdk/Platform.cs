using System;
using Mono.Unix.Native;

namespace HITTSDK
{
    public static class Platform
    {
        public static bool IsOSX
        {
            get
            {
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    Utsname name = null;

                    if (Syscall.uname(out name) == 0)
                    {
                        return (name.sysname.ToLower() == "darwin");
                    }
                }

                return (false);
            }
        }
    }
}
