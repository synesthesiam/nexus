using System;
using System.Runtime.InteropServices;

namespace HITTSDK
{
	public class MacAPI : IHITTAPI
	{

		[DllImport("h-ittsdkwrapper.dll")]
        private static extern APIStatus hitt_inspect(byte[] bytes, ref uint id, ref Keys key_code);
		
		public APIStatus inspect(byte[] bytes, ref uint id, ref Keys key_code)
		{
			return (hitt_inspect(bytes, ref id, ref key_code));
		}
	}
}
