using System;
using System.Runtime.InteropServices;

namespace HITTSDK
{
    public class DefaultAPI : IHITTAPI
    {
        protected log4net.ILog log = log4net.LogManager.GetLogger(typeof(DefaultAPI));

        [DllImport("h-ittsdk32.dll", EntryPoint="hitt_inspect")]
        private static extern APIStatus hitt_inspect32(byte[] bytes, ref uint id, ref Keys key_code);

        [DllImport("h-ittsdk64.dll", EntryPoint="hitt_inspect")]
        private static extern APIStatus hitt_inspect64(byte[] bytes, ref uint id, ref Keys key_code);

        private bool is64Bit;

        public DefaultAPI()
        {
            int ptrSize = Marshal.SizeOf(typeof(IntPtr)) * 8;
            log.DebugFormat("Using {0}-bit API", ptrSize);
            is64Bit = (ptrSize == 64);
        }

        public APIStatus inspect(byte[] bytes, ref uint id, ref Keys key_code)
        {
            if (is64Bit)
            {
                return (hitt_inspect64(bytes, ref id, ref key_code));
            }
            else {
                return (hitt_inspect32(bytes, ref id, ref key_code));
            }
        }
    }
}
