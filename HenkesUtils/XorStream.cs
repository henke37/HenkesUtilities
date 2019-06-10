using System;
using System.IO;

namespace Henke37.IOUtils {
	public class XorStream : Stream {

		private Stream dataStream;
		private Stream keyStream;

		public XorStream(Stream dataStream, Stream keyStream) {
			this.dataStream = dataStream;
			if(!keyStream.CanRead) throw new ArgumentException("The keystream must be readable!", nameof(keyStream));
			this.keyStream = keyStream;
		}

		public XorStream(Stream dataStream, byte[] key) {
			this.dataStream = dataStream;
			keyStream = new MemoryStream(key, false);
		}

		public override bool CanRead => dataStream.CanRead;
		public override bool CanSeek => dataStream.CanSeek && keyStream.CanSeek;
		public override bool CanWrite => dataStream.CanWrite;

		public override long Length => dataStream.Length;

		public override long Position { get => dataStream.Position; set => Seek(value,SeekOrigin.Begin); }

		public override void Flush() {
			dataStream.Flush();
		}

		public override int Read(byte[] buffer, int offset, int count) {
			int bytesRead=dataStream.Read(buffer, offset, count);
			XorBuffer(buffer,offset, bytesRead);
			return bytesRead;
		}

		public override int ReadByte() {
			if(keyStream.Position == keyStream.Length) keyStream.Position = 0;
			return dataStream.ReadByte() ^ keyStream.ReadByte();
		}

		private void XorBuffer(byte[] buffer, int offset, int count) {
			int end = offset + count;

			for(int buffPos=offset;buffPos<end;++buffPos) {
				if(keyStream.Position == keyStream.Length) keyStream.Position = 0;
				buffer[buffPos] =(byte) (buffer[buffPos] ^ keyStream.ReadByte());
			}
		}

		public override long Seek(long offset, SeekOrigin origin) {
			if(origin==SeekOrigin.End) {
				offset += Length;
			} else if(origin==SeekOrigin.Current) {
				offset += dataStream.Position;
			}
			keyStream.Seek(offset % keyStream.Length, SeekOrigin.Begin);
			return dataStream.Seek(offset, SeekOrigin.Begin);
		}

		public override void SetLength(long value) {
			dataStream.SetLength(value);
		}

		public override void Write(byte[] buffer, int offset, int count) {
			XorBuffer(buffer, offset, count);
			dataStream.Write(buffer, offset, count);
		}

		public override void WriteByte(byte value) {
			if(keyStream.Position == keyStream.Length) keyStream.Position = 0;
			dataStream.WriteByte((byte)(value ^ keyStream.ReadByte()));
		}
	}
}
