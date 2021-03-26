using System;
using System.IO;
using System.Text;
//using System.Linq;

namespace pyconv
{
    public enum PyCodeObjectType
    {
        NULL = 0x30,                     // 0
        NONE = 0x4E,                     // N
        FALSE = 0x46,                    // F
        TRUE = 0x54,                     // T
        STOPITER = 0x53,                 // S
        ELLIPSIS = 0x2E,                 // .
        INT = 0x69,                      // i
        INT64 = 0x49,                    // I
        FLOAT = 0x66,                    // f
        BINARY_FLOAT = 0x67,             // g
        COMPLEX = 0x78,                  // x
        BINARY_COMPLEX = 0x79,           // y
        LONG = 0x6C,                     // l
        STRING = 0x73,                   // s
        INTERNED = 0x74,                 // t
        STRINGREF = 0x52,                // R
        OBREF = 0x72,                    // r
        TUPLE = 0x28,                    // (
        LIST = 0x5B,                     // [
        DICT = 0x7B,                     // {
        CODE = 0x63,                     // c
        UNICODE = 0x75,                  // u
        UNKNOWN = 0x3F,                  // ?
        SET = 0x3C,                      // <
        FROZENSET = 0x3E,                // >
        ASCII = 0x61,                    // a
        ASCII_INTERNED = 0x41,           // A
        SMALL_TUPLE = 0x29,              // )
        SHORT_ASCII = 0x7A,              // z
        SHORT_ASCII_INTERNED = 0x5A,     // Z
        Exit = 0x0
    }

    class pyObject
    {
        public static double lastProgress = 0;
        public static DateTime t = DateTime.Now;

        public static void LoadSwapCode(BinaryReader read, BinaryWriter write)
        {
            byte o_type = read.ReadByte();
            if ((PyCodeObjectType)o_type != PyCodeObjectType.STRING)
            {
                read.BaseStream.Seek(-1, SeekOrigin.Current);
                LoadNext(read, write);
            }
            else
            {
                write.Write(o_type);
                int size = read.ReadInt32();
                write.Write(size);
                byte[,] swap_table = new byte[,] { // {REVELATION_OPCODE, PYTHON27_OPCODE}
                { 20, 0    }, { 21, 1  },   { 22, 2  },   { 23, 3  },   { 24, 4 },    { 25, 5 },
                { 9, 9     }, { 10, 10 },   { 11, 11 },   { 12, 12 },   { 13, 13 },   { 15, 15 },
                { 19, 19   }, { 0, 20  },   { 1, 21  },   { 2, 22  },   { 3, 23  },   { 4, 24 },
                { 5, 25    }, { 26, 26 },   { 27, 27 },   { 28, 28 },   { 29, 29 },   { 30, 30 },
                { 31, 31   }, { 32, 32 },   { 33, 33 },   { 40, 40 },   { 41, 41 },   { 42, 42 },
                { 43, 43   }, { 50, 50 },   { 51, 51 },   { 52, 52 },   { 53, 53 },   { 56, 54 },
                { 57, 55   }, { 54, 56 },   { 55, 57 },   { 60, 58 },   { 61, 59 },   { 58, 60 },
                { 59, 61   }, { 66, 62 },   { 67, 63 },   { 68, 64 },   { 65, 65 },   { 62, 66 },
                { 63, 67   }, { 64, 68 },   { 70, 70 },   { 71, 71 },   { 72, 72 },   { 73, 73 },
                { 74, 74   }, { 82, 75 },   { 83, 76 },   { 84, 77 },   { 85, 78 },   { 86, 79 },
                { 87, 80   }, { 81, 81 },   { 75, 82 },   { 76, 83 },   { 77, 84 },   { 78, 85 },
                { 79, 86   }, { 80, 87 },   { 88, 88 },   { 89, 89 },   { 93, 90 },   { 94, 91 },
                { 92, 92   }, { 90, 93 },   { 91, 94 },   { 103, 95 },  { 104, 96 },  { 105, 97 },
                { 106, 98  }, { 107, 99 },  { 100, 100 }, { 101, 101 }, { 102, 102 }, { 95, 103 },
                { 96, 104  }, { 97, 105 },  { 98, 106 },  { 99, 107 },  { 108, 108 }, { 109, 109 },
                { 110, 110 }, { 111, 111 }, { 119, 112 }, { 113, 113 }, { 112, 114 }, { 126, 115 },
                { 116, 116 }, { 114, 119 }, { 124, 120 }, { 125, 121 }, { 115, 122 }, { 120, 124 },
                { 121, 125 }, { 122, 126 }, { 130, 130 }, { 131, 131 }, { 132, 132 }, { 133, 133 },
                { 135, 134 }, { 134, 135 }, { 137, 136 }, { 136, 137 }, { 140, 140 }, { 141, 141 },
                { 142, 142 }, { 143, 143 }, { 145, 145 }, { 147, 146 }, { 146, 147 } };
                if (size < 0)
                    Console.WriteLine("Wrong code size");
                byte[] ops = read.ReadBytes(size > 0 ? size : -size);
                for (int i = 0; i < size; i++)
                {
                    byte op = ops[i];
                    bool findop = false;
                    for (int j = 0; j < swap_table.Length / 2; j++)
                    {
                        if (swap_table[j, 0] == op)
                        {
                            write.Write(swap_table[j, 1]);
                            findop = true;
                        }
                    }
                    if (!findop)
                        write.Write(op);
                    if (op >= 90) // has args
                    {
                        write.Write(ops[++i]);
                        write.Write(ops[++i]);
                    }
                }
            }
        }

        public static PyCodeObjectType LoadNext(BinaryReader read, BinaryWriter write)
        {
            try
            {
                byte o_type = read.ReadByte();
                var ctr = (PyCodeObjectType)o_type;
                switch (ctr)
                {
                    case PyCodeObjectType.STRINGREF:
                    case PyCodeObjectType.OBREF:
                    case PyCodeObjectType.INT:
                        {
                            write.Write(o_type);
                            write.Write(read.ReadBytes(4));
                            break;
                        }
                    case PyCodeObjectType.SHORT_ASCII:
                    case PyCodeObjectType.SHORT_ASCII_INTERNED:
                    case PyCodeObjectType.FLOAT:
                        {
                            write.Write(o_type);
                            byte ln = read.ReadByte();
                            write.Write(ln);
                            write.Write(read.ReadBytes(ln > 0 ? ln : -ln));
                            break;
                        }
                    case PyCodeObjectType.INT64:
                    case PyCodeObjectType.BINARY_FLOAT:
                    case PyCodeObjectType.BINARY_COMPLEX:
                        {
                            write.Write(o_type);
                            write.Write(read.ReadBytes(8));
                            break;
                        }
                    case PyCodeObjectType.COMPLEX:
                        {
                            write.Write(o_type);
                            int sx = read.ReadInt32();
                            write.Write(sx);
                            var _size = sx >= 0 ? sx : -sx;
                            write.Write(read.ReadBytes(_size));
                            break;
                        }
                    case PyCodeObjectType.LONG:
                        {
                            write.Write(o_type);
                            var l = read.ReadInt32();
                            write.Write(l);
                            write.Write(read.ReadBytes((l > 0 ? l : -l) * 2));
                            break;
                        }
                    case PyCodeObjectType.DICT:
                        {
                            write.Write(o_type);
                            while (true)
                            {
                                var b = LoadNext(read, write);
                                if (b == PyCodeObjectType.NONE) break;
                                LoadNext(read, write);
                            }
                            break;
                        }
                    case PyCodeObjectType.CODE:
                        {
                            write.Write(o_type);
                            write.Write(read.ReadInt32());     //argcount
                            write.Write(read.ReadInt32());     //nlocals
                            write.Write(read.ReadInt32());     //stacksize 
                            write.Write(read.ReadInt32());     //flags
                            LoadSwapCode(read, write);               //code section
                            LoadNext(read, write);               //consts section
                            LoadNext(read, write);               //names
                            LoadNext(read, write);               //varnames
                            LoadNext(read, write);               //freevars
                            LoadNext(read, write);               //cellvars
                            LoadNext(read, write);               //filename
                            LoadNext(read, write);               //name
                            write.Write(read.ReadBytes(4));      //firstline
                            LoadNext(read, write);               //lntable
                            break;
                        }
                    case PyCodeObjectType.ASCII:
                    case PyCodeObjectType.ASCII_INTERNED:
                    case PyCodeObjectType.INTERNED:
                    case PyCodeObjectType.STRING:
                    case PyCodeObjectType.UNICODE:
                        {
                            write.Write(o_type);
                            var l = read.ReadInt32();
                            write.Write(l);
                            write.Write(read.ReadBytes(l > 0 ? l : -l));
                            break;
                        }
                    case PyCodeObjectType.TUPLE:
                    case PyCodeObjectType.LIST:
                    case PyCodeObjectType.SET:
                    case PyCodeObjectType.FROZENSET:
                    case PyCodeObjectType.SMALL_TUPLE:
                        {
                            write.Write(o_type);
                            var size = ctr == PyCodeObjectType.SMALL_TUPLE ? read.ReadByte() : read.ReadInt32();
                            write.Write(size);
                            for (int i = 0; i < size; i++)
                            {
                                LoadNext(read, write);
                            }
                            break;
                        }
                    case PyCodeObjectType.NULL:
                    case PyCodeObjectType.NONE:
                    case PyCodeObjectType.FALSE:
                    case PyCodeObjectType.TRUE:
                    case PyCodeObjectType.STOPITER:
                    case PyCodeObjectType.ELLIPSIS:
                    case PyCodeObjectType.UNKNOWN:
                        write.Write(o_type);
                        break;
                    case PyCodeObjectType.Exit:
                        return PyCodeObjectType.Exit;
                    default:
                        write.Write(o_type);
                        Console.WriteLine("ERROR:" + o_type.ToString("X"));
                        return PyCodeObjectType.Exit;
                }
                return ctr;
            }
            catch (EndOfStreamException)
            {
                return PyCodeObjectType.Exit;
            }
        }
    }
}
