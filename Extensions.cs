using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSXScript_Tool
{
    static class Extensions
    {
        public static void Reset(this MemoryStream stream)
        {
            stream.Position = 0;
            stream.SetLength(0);
        }
    }
}
