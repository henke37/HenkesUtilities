using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HenkesUtils {
    public static class ExtensionFunctions {

        public static bool TrySet<TKey,TValue>(this Dictionary<TKey,TValue> d, TKey Key, TValue Value) {
            if(d.ContainsKey(Key)) return false;
            d[Key] = Value;
            return true;
        }

        public static List<Int32> ReadInt32Array(this BinaryReader r, int count) {
            var l = new List<Int32>(count);
            for(var i=0;i<count;++i) {
                l.Add(r.ReadInt32());
            }
            return l;
        }

        public static List<Int16> ReadInt16Array(this BinaryReader r, int count) {
            var l = new List<Int16>(count);
            for(var i = 0; i < count; ++i) {
                l.Add(r.ReadInt16());
            }
            return l;
        }

        public static List<UInt32> ReadUInt32Array(this BinaryReader r, int count) {
            var l = new List<UInt32>(count);
            for(var i = 0; i < count; ++i) {
                l.Add(r.ReadUInt32());
            }
            return l;
        }

        public static List<UInt16> ReadUInt16Array(this BinaryReader r, int count) {
            var l = new List<UInt16>(count);
            for(var i = 0; i < count; ++i) {
                l.Add(r.ReadUInt16());
            }
            return l;
        }

        public static string Read4C(this BinaryReader r) {
            return new string(r.ReadChars(4));
        }

        public static string ReadUTFString(this BinaryReader r, int bytesToRead, bool terminated=true) {
            var ba = r.ReadBytes(bytesToRead);
            return new String(Encoding.UTF8.GetChars(ba, 0, bytesToRead - (terminated?1:0)));
        }

        public static string ReadAsUTF8(this Stream s) {
            var ba = new byte[s.Length];
            s.Read(ba, 0, (int)s.Length);
            return new string(Encoding.UTF8.GetChars(ba));
        }

		public static SubStream ReadSubStream(this BinaryReader r, int bytesToRead) {
			var ss = new SubStream(r.BaseStream, r.BaseStream.Position, bytesToRead);
			r.Skip(bytesToRead);
			return ss;
		}

        public static void Skip(this BinaryReader r, int bytesToSkip) {
            r.BaseStream.Seek(bytesToSkip, SeekOrigin.Current);
        }

        public static void Seek(this BinaryReader r, int offset) {
            r.BaseStream.Seek(offset, SeekOrigin.Begin);
        }

        public static void Seek(this BinaryReader r, int offset, SeekOrigin seekOrigin) {
            r.BaseStream.Seek(offset, seekOrigin);
        }

        public static void WriteLenPrefixedUTFString(this BinaryWriter w, string str) {
            var ba = Encoding.UTF8.GetBytes(str + "\0");
            w.Write((int)ba.Length);
            w.Write(ba);
        }

        public static long BytesLeft(this BinaryReader r) {
            return r.BaseStream.Length - r.BaseStream.Position;
        }

        public static string ReadNullTerminatedUTF8String(this BinaryReader r) {
            var l = new List<Byte>();
            for(; ; ) {
                byte b = r.ReadByte();
                if(b == 0) break;
                l.Add(b);
            }
            return new string(Encoding.UTF8.GetChars(l.ToArray()));
        }

		public static uint Read3ByteUInt(this BinaryReader r) {
			uint value = r.ReadByte();

			value += (uint)(r.ReadUInt16() << 8);

			return value;
		}
	}
}
