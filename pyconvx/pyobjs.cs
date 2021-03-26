using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace pyconv
{

    public enum PyCodeObjectType
    {
        NULL = '0', // 30
        NONE = 'N', // 4E
        FALSE = 'F', // 46
        TRUE = 'T', // 54
        STOPITER = 'S', // 53
        ELLIPSIS = '.', // 2E
        INT = 'i', // 69
        INT64 = 'I', // 49
        FLOAT = 'f', // 66
        BINARY_FLOAT = 'g', // 67
        COMPLEX = 'x', // 78
        BINARY_COMPLEX = 'y', // 79
        LONG = 'l', // 6C
        STRING = 's', // 73
        INTERNED = 't', // 74
        STRINGREF = 'R', // 52
        OBREF = 'r', // 72
        TUPLE = '(', // 28
        LIST = '[', // 5B
        DICT = '{', // 7B
        CODE = 'c', // 63
        CODE2 = 'C',   // 43
        UNICODE = 'u', // 75
        UNKNOWN = '?', // 3F
        SET = '<', // 3C
        FROZENSET = '>', // 3E
        ASCII = 'a', // 61
        ASCII_INTERNED = 'A', // 41
        SMALL_TUPLE = ')', // 29
        SHORT_ASCII = 'z', // 7A
        SHORT_ASCII_INTERNED = 'Z',  // 5A
    }

    interface pyEntity
    {
        byte[] raw_value { get; set; }
        PyCodeObjectType pyType { get; set; }
        void write_to_stream(BinaryWriter bw);
    }

    class pyC
    {
        public static List<pyEntity> refs = new List<pyEntity>();
        public static List<pyEntity> interns = new List<pyEntity>();
    }

    class pyDummy : pyEntity
    {
        public byte[] raw_value { get; set; }

        public PyCodeObjectType pyType { get; set; }

        public void write_to_stream(BinaryWriter bw)
        {
            bw.Write((byte)pyType);
        }
    }

    class pyString : pyEntity
    {
        public pyString(PyCodeObjectType ty)
        {
            __pytype = ty;
        }
        public byte[] raw_value { get; set; }
        private PyCodeObjectType __pytype = PyCodeObjectType.STRING;
        public PyCodeObjectType pyType { get { return __pytype; } set { __pytype = value; } }
        public static pyString Load(BinaryReader read, PyCodeObjectType type)
        {
            UInt32 length = 0;
            try { length = read.ReadUInt32(); }
            catch (EndOfStreamException) { return null; }
            if (length < 0) return null;
            pyString tmp_str = new pyString(type);
            if (length > 0)
            {
                try { tmp_str.raw_value = read.ReadBytes((int)length); }
                catch (EndOfStreamException) { return null; }
            }
            return tmp_str;
        }

        public void write_to_stream(BinaryWriter bw)
        {
            bw.Write((byte)pyType);
            bw.Write((UInt32)raw_value.Length);
            bw.Write(raw_value);
        }
    }



    class pyShortStr : pyEntity
    {
        public byte[] raw_value { get; set; }
        private PyCodeObjectType __pytype = PyCodeObjectType.SHORT_ASCII;
        public PyCodeObjectType pyType { get { return __pytype; } set { __pytype = value; } }
        public static pyShortStr Load(BinaryReader read, PyCodeObjectType type)
        {
            ushort length = 0;
            try { length = read.ReadUInt16(); }
            catch (EndOfStreamException) { return null; }
            if (length < 0) return null;
            pyShortStr tmp_str = new pyShortStr();
            tmp_str.__pytype = type;
            if (length > 0)
            {

                try { tmp_str.raw_value = read.ReadBytes((int)length); }
                catch (EndOfStreamException) { return null; }
            }
            return tmp_str;
        }
        public void write_to_stream(BinaryWriter bw)
        {
            bw.Write((byte)pyType);
            bw.Write((UInt16)raw_value.Length);
            bw.Write(raw_value);
        }
    }

    class pyFloat : pyEntity
    {
        public byte[] raw_value { get; set; }
        public PyCodeObjectType pyType { get { return PyCodeObjectType.FLOAT; } set { } }
        public static pyFloat Load(BinaryReader read)
        {
            byte len = 0;
            try { len = read.ReadByte(); }
            catch (EndOfStreamException) { return null; }
            if (len < 0) return null;
            pyFloat tmp = new pyFloat();
            try
            {
                tmp.raw_value = read.ReadBytes(len);
            }
            catch (EndOfStreamException) { return null; }
            return tmp;
        }
        public void write_to_stream(BinaryWriter bw)
        {
            bw.Write((byte)pyType);
            bw.Write((byte)raw_value.Length);
            bw.Write(raw_value);
        }
    }

    class pyBin : pyEntity
    {
        public byte[] raw_value { get; set; }
        public PyCodeObjectType pyType { get; set; }
        public static pyBin Load(BinaryReader read, PyCodeObjectType ty, int bytes)
        {
            try { return new pyBin() { pyType = ty, raw_value = read.ReadBytes(bytes) }; }
            catch (EndOfStreamException) { return null; }
        }
        public void write_to_stream(BinaryWriter bw)
        {
            bw.Write((byte)pyType);
            bw.Write(raw_value);
        }
    }

    class pyLong : pyEntity
    {
        public byte[] raw_value { get; set; }
        public int fsize = 0;
        public PyCodeObjectType pyType { get { return PyCodeObjectType.LONG; } set { } }
        public static pyLong Load(BinaryReader read)
        {
            int size = 0;
            try { size = read.ReadInt32(); }
            catch (EndOfStreamException) { return null; }
            var a_size = size >= 0 ? size : -size;
            List<byte> buf = new List<byte>();
            for (int i = 0; i < a_size; i++)
            {
                try
                {
                    var a = read.ReadBytes(2);
                    buf.Add(a[0]);
                    buf.Add(a[1]);
                }
                catch (EndOfStreamException) { return null; }
            }
            return new pyLong() { raw_value = buf.ToArray(), fsize = size };
        }

        public void write_to_stream(BinaryWriter bw)
        {
            bw.Write((byte)pyType);
            bw.Write(fsize);
            bw.Write(raw_value);
        }
    }

    class pyComplex : pyEntity
    {
        public byte[] raw_value { get; set; }
        public PyCodeObjectType pyType { get { return PyCodeObjectType.COMPLEX; } set { } }
        public static pyComplex Load(BinaryReader read)
        {
            int size = 0;
            try { size = read.ReadInt32(); }
            catch (EndOfStreamException) { return null; }
            var a_size = size >= 0 ? size : -size;
            try { return new pyComplex() { raw_value = read.ReadBytes(a_size) }; }
            catch (EndOfStreamException) { return null; }
        }

        public void write_to_stream(BinaryWriter bw)
        {
            bw.Write((byte)pyType);
            bw.Write(raw_value.Length);
            bw.Write(raw_value);
        }
    }

    class pyList : pyEntity
    {
        public byte[] raw_value { get; set; }
        public PyCodeObjectType pyType { get; set; }
        public List<pyEntity> objects = new List<pyEntity>();
        public void Add(pyEntity obj) { objects.Add(obj); }
        public static pyList Load(BinaryReader read, PyCodeObjectType type)
        {
            UInt32 size = 0;
            try { size = read.ReadUInt32(); }
            catch (EndOfStreamException) { return null; }

            pyList tmp = new pyList() { pyType = type };
            for (int i = 0; i < size; i++)
            {
                var obj = pyObject.Load(read);
                if (obj == null) break;
                tmp.Add(obj);
            }
            return tmp;
        }

        public void write_to_stream(BinaryWriter bw)
        {
            bw.Write((byte)pyType);
            bw.Write((uint)objects.Count);
            foreach (var item in objects)
            {
                item.write_to_stream(bw);
            }
        }
    }

    class pyDict : pyEntity
    {
        public byte[] raw_value { get; set; }
        public List<KeyValuePair<pyEntity, pyEntity>> value = new List<KeyValuePair<pyEntity, pyEntity>>();
        public PyCodeObjectType pyType { get { return PyCodeObjectType.DICT; } set { } }
        public void Add(KeyValuePair<pyEntity, pyEntity> obj) { value.Add(obj); }
        public static pyDict Load(BinaryReader read)
        {
            pyDict tmp = new pyDict();
            while (true)
            {
                var key = pyObject.Load(read);
                if (key.pyType == PyCodeObjectType.NULL || key == null) break;
                var val = pyObject.Load(read);
                tmp.Add(new KeyValuePair<pyEntity, pyEntity>(key, val));
                if (val == null) break;
            }
            return tmp;
        }

        public void write_to_stream(BinaryWriter bw)
        {
            bw.Write((byte)pyType);
            foreach (var item in value)
            {
                item.Key.write_to_stream(bw);
                item.Value.write_to_stream(bw);
            }
            bw.Write('0');
        }
    }

    class pyCode : pyEntity
    {
        public PyCodeObjectType pyType { get { return PyCodeObjectType.CODE; } set { } }
        public pyEntity parent { get; set; }
        public byte[] raw_value { get; set; }

        public UInt32 arg_count;
        public UInt32 num_locals;
        public UInt32 stack_size;
        public UInt32 flags;
        public pyEntity code;
        public pyEntity consts;
        public pyEntity names;
        public pyEntity varnames;
        public pyEntity freevars;
        public pyEntity cellvars;
        public pyEntity filename;
        public pyEntity name;
        public UInt32 firstline;
        public pyEntity lntable;
        public static pyCode Load(BinaryReader read)
        {
            return new pyCode()
            {
                // argcount
                arg_count = read.ReadUInt32(),
                // nlocals
                num_locals = read.ReadUInt32(),
                // stacksize
                stack_size = read.ReadUInt32(),
                //flags
                flags = read.ReadUInt32(),
                code = pyObject.Load(read),
                consts = pyObject.Load(read),
                names = pyObject.Load(read),
                varnames = pyObject.Load(read),
                freevars = pyObject.Load(read),
                cellvars = pyObject.Load(read),
                filename = pyObject.Load(read),
                name = pyObject.Load(read),
                // firstline
                firstline = read.ReadUInt32(),
                //lntable
                lntable = pyObject.Load(read)
            };
        }

        public void write_to_stream(BinaryWriter bw)
        {
            bw.Write((byte)pyType);
            bw.Write(arg_count);
            bw.Write(num_locals);
            bw.Write(stack_size);
            bw.Write(flags);
            write_code(bw);
            //code.write_to_stream(bw);
            consts.write_to_stream(bw);
            names.write_to_stream(bw);
            varnames.write_to_stream(bw);
            freevars.write_to_stream(bw);
            cellvars.write_to_stream(bw);
            filename.write_to_stream(bw);
            name.write_to_stream(bw);
            bw.Write(firstline);
            lntable.write_to_stream(bw);
        }

        private void write_code(BinaryWriter bw)
        {
            pyString _code = (pyString)code;
            bw.Write((byte)code.pyType);
            bw.Write((UInt32)_code.raw_value.Length);
            int f_pos = 0;
            while (f_pos < code.raw_value.Length)
            {
                byte b = remap_code(code.raw_value[f_pos++]);
                bw.Write(b);
                if (OpHasArgs(b))
                {
                    bw.Write(code.raw_value[f_pos++]);
                    bw.Write(code.raw_value[f_pos++]);
                }
            }
        }

        private byte remap_code(byte bcode)
        {
            byte[,] swap_table = new byte[,] { { 20, 0 }, { 21, 1 }, { 22, 2 }, { 23, 3 }, { 24, 4 }, { 25, 5 }, { 9, 9 }, { 10, 10 }, { 11, 11 }, { 12, 12 }, { 13, 13 }, { 15, 15 }, { 19, 19 }, { 0, 20 }, { 1, 21 }, { 2, 22 }, { 3, 23 }, { 4, 24 }, { 5, 25 }, { 26, 26 }, { 27, 27 }, { 28, 28 }, { 29, 29 }, { 30, 30 }, { 31, 31 }, { 32, 32 }, { 33, 33 }, { 40, 40 }, { 41, 41 }, { 42, 42 }, { 43, 43 }, { 50, 50 }, { 51, 51 }, { 52, 52 }, { 53, 53 }, { 56, 54 }, { 57, 55 }, { 54, 56 }, { 55, 57 }, { 60, 58 }, { 61, 59 }, { 58, 60 }, { 59, 61 }, { 66, 62 }, { 67, 63 }, { 68, 64 }, { 65, 65 }, { 62, 66 }, { 63, 67 }, { 64, 68 }, { 70, 70 }, { 71, 71 }, { 72, 72 }, { 73, 73 }, { 74, 74 }, { 82, 75 }, { 83, 76 }, { 84, 77 }, { 85, 78 }, { 86, 79 }, { 87, 80 }, { 81, 81 }, { 75, 82 }, { 76, 83 }, { 77, 84 }, { 78, 85 }, { 79, 86 }, { 80, 87 }, { 88, 88 }, { 89, 89 }, { 93, 90 }, { 94, 91 }, { 92, 92 }, { 90, 93 }, { 91, 94 }, { 103, 95 }, { 104, 96 }, { 105, 97 }, { 106, 98 }, { 107, 99 }, { 100, 100 }, { 101, 101 }, { 102, 102 }, { 95, 103 }, { 96, 104 }, { 97, 105 }, { 98, 106 }, { 99, 107 }, { 108, 108 }, { 109, 109 }, { 110, 110 }, { 111, 111 }, { 119, 112 }, { 113, 113 }, { 112, 114 }, { 126, 115 }, { 116, 116 }, { 114, 119 }, { 124, 120 }, { 125, 121 }, { 115, 122 }, { 120, 124 }, { 121, 125 }, { 122, 126 }, { 130, 130 }, { 131, 131 }, { 132, 132 }, { 133, 133 }, { 135, 134 }, { 134, 135 }, { 137, 136 }, { 136, 137 }, { 140, 140 }, { 141, 141 }, { 142, 142 }, { 143, 143 }, { 145, 145 }, { 147, 146 }, { 146, 147 } };
            for (int i = 0; i < swap_table.Length / 2; i++)
            {
                if (swap_table[i, 0] == bcode)
                {
                    return swap_table[i, 1];
                }
            }
            return bcode;
        }
        public static bool OpHasArgs(byte b)
        {
            return b > 89;
        }
    }

}