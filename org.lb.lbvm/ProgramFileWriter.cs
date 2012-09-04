using System.IO;
using System.Linq;

namespace org.lb.lbvm
{
    internal sealed class ProgramFileWriter
    {
        private readonly Program program;
        private Stream data;
        private byte checksum1;
        private byte checksum2;

        public ProgramFileWriter(Program program)
        {
            this.program = program;
        }

        public void Write(Stream destination)
        {
            this.data = destination;
            checksum1 = 0;
            checksum2 = 0;
            WriteHeader();
            WriteBytecode();
            WriteSymbolTable();
            WriteFooter();
        }

        private void WriteHeader()
        {
            writeString("LBVM");
            writeByte(1);
            writeByte(0);
            writeByte(0);
            writeByte(0);
        }

        private void writeString(string value)
        {
            foreach (char c in value)
                writeByte((byte)c);
        }

        private void writeByte(byte b)
        {
            data.WriteByte(b);
            AddByteToChecksums(b);
        }

        private void AddByteToChecksums(byte b)
        {
            checksum1 += b;
            checksum2 ^= b;
        }

        private void WriteBytecode()
        {
            writeByte(1);
            WriteByteBlock(program.Bytecode);
        }

        private void WriteByteBlock(byte[] block)
        {
            writeInt(block.Length);
            foreach (byte b in block)
                writeByte(b);
        }

        private void writeInt(int b)
        {
            for (int i = 0; i < 4; ++i)
            {
                writeByte((byte)(b % 256));
                b /= 256;
            }
        }

        private void WriteSymbolTable()
        {
            writeByte(2);
            writeInt(program.SymbolTable.Sum(s => s.Length + 8));
            int i = 0;
            foreach (var s in program.SymbolTable)
            {
                writeInt(i);
                writeInt(s.Length);
                writeString(s);
                ++i;
            }
        }

        private void WriteFooter()
        {
            byte[] block = new[] { checksum1, checksum2 };
            writeByte(0xff);
            WriteByteBlock(block);
        }
    }
}
