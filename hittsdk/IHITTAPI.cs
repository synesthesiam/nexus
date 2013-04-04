using System;

namespace HITTSDK
{
    public interface IHITTAPI
    {
        APIStatus inspect(byte[] bytes, ref uint id, ref Keys key_code);
    }
}
