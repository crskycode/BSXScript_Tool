using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSXScript_Tool
{
    static class Binary
    {
        public static string GetCString(byte[] data, int startIndex)
        {
            var j = startIndex;

            while (j + sizeof(short) < data.Length && BitConverter.ToUInt16(data, j) != 0)
                j += sizeof(short);

            var count = j - startIndex;

            if (count == 0)
                return string.Empty;

            return Encoding.Unicode.GetString(data, startIndex, count);
        }

        public static int GetAlignedValue(int value, int alignment)
        {
            return value + alignment - 1 & ~(alignment - 1);
        }
    }
}
