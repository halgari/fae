using System.IO;
using System.Text;

namespace Wyld
{
   public class LineNumberingReader : StreamReader
    {
        public int Line { get; private set; } = 1;
        public int Column { get; private set; }= 1;
        
        public LineNumberingReader(Stream stream) : base(stream)
        {
        }

        public LineNumberingReader(Stream stream, bool detectEncodingFromByteOrderMarks) : base(stream, detectEncodingFromByteOrderMarks)
        {
        }

        public LineNumberingReader(Stream stream, Encoding encoding) : base(stream, encoding)
        {
        }

        public LineNumberingReader(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks) : base(stream, encoding, detectEncodingFromByteOrderMarks)
        {
        }

        public LineNumberingReader(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize) : base(stream, encoding, detectEncodingFromByteOrderMarks, bufferSize)
        {
        }

        public LineNumberingReader(Stream stream, Encoding? encoding = null, bool detectEncodingFromByteOrderMarks = true, int bufferSize = -1, bool leaveOpen = false) : base(stream, encoding, detectEncodingFromByteOrderMarks, bufferSize, leaveOpen)
        {
        }

        public LineNumberingReader(string path) : base(path)
        {
        }

        public LineNumberingReader(string path, bool detectEncodingFromByteOrderMarks) : base(path, detectEncodingFromByteOrderMarks)
        {
        }

        public LineNumberingReader(string path, Encoding encoding) : base(path, encoding)
        {
        }

        public LineNumberingReader(string path, Encoding encoding, bool detectEncodingFromByteOrderMarks) : base(path, encoding, detectEncodingFromByteOrderMarks)
        {
        }

        public LineNumberingReader(string path, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize) : base(path, encoding, detectEncodingFromByteOrderMarks, bufferSize)
        {
        }

        public override int Read()
        {
            var ch = base.Read();
            if (ch == '\n')
            {
                Line += 1;
                Column = 0;
            }
            else
            {
                Column += 1;
            }

            return ch;
        }

        public char? ReadChar()
        {
            var val = Read();
            return val == -1 ? (char?) null : (char) val;
        }

        public char? PeekChar()
        {
            var val = Peek();
            return val == -1 ? (char?) null : (char) val;
        }
        
    }
}