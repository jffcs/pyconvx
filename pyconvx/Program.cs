using System;
using System.IO;

namespace pyconv
{
    class Program
    {
        private const UInt32 pymagic = 0x0A0DF303;
        private const UInt32 timestamp = 0;
        private static BinaryReader br;
        private static BinaryWriter bw;
        static void Main(string[] args)
        {
            if (args.Length == 0)
                return;
            var inp = Path.GetFullPath(args[0]);
            if (!File.Exists(inp))
                return;
            string outp;

            if (args.Length >= 2)
                outp = args[1];
            else
                outp = Path.GetDirectoryName(inp) + "\\" + Path.GetFileNameWithoutExtension(inp) + "_cc" + Path.GetExtension(inp);
            br = new BinaryReader(new FileStream(inp, FileMode.Open));
            bw = new BinaryWriter(new FileStream(outp, FileMode.Create));
            bw.Write(pymagic);
            bw.Write(timestamp);
            convPyc();
            bw.Flush();
            bw.Close();
            br.Close();
        }
        private static void convPyc()
        {
            while (true)
            {
                if (pyObject.LoadNext(br, bw) == PyCodeObjectType.Exit)
                    break;
            }
        }
    }

}
