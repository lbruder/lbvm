using System;
using System.Collections.Generic;
using System.IO;

namespace org.lb.lbvm
{
    internal sealed class ProgramFileReader
    {
        private readonly Stream data;
        private readonly Program file;
        private bool DoneReading;
        private byte checksum1;
        private byte checksum2;
        int version;
        private byte[] bytecode;
        private readonly List<string> symbolTable = new List<string>();

        public Program ProgramFile { get { return file; } }

        public ProgramFileReader(Stream data)
        {
            DoneReading = false;
            this.data = data;
            this.file = Read();
        }

        private Program Read()
        {
            ReadFileHeader();
            while (!DoneReading) ReadBlock();
            string[] symbolTableArray = new string[symbolTable.Count];
            symbolTable.CopyTo(symbolTableArray);
            return new Program(version, bytecode, symbolTableArray);
        }

        private void ReadFileHeader()
        {
            if (readString(4) != "LBVM") throw new FileLoadException("Invalid data format");
            version = ReadByte();
            if (version != 1) throw new FileLoadException("Unsupported data file version " + file.Version);
            ReadByte();
            ReadByte();
            ReadByte();
        }

        private void ReadBlock()
        {
            byte cs1 = checksum1;
            byte cs2 = checksum2;

            int i = ReadByte();
            int dataSize = ReadInt();
            byte[] contents = ReadBytes(dataSize);

            if (i == 1) DecodeCodeBlock(contents);
            else if (i == 2) DecodeSymbolTableBlock(contents);
            else if (i == 255) DecodeFooterBlock(contents, cs1, cs2);
            else throw new FileLoadException("Invalid block type " + i);
        }

        private void DecodeCodeBlock(byte[] contents)
        {
            bytecode = contents;
        }

        private void DecodeSymbolTableBlock(byte[] contents)
        {
            char[] ca = new char[contents.Length];
            Array.Copy(contents, ca, contents.Length);

            int pos = 0;
            while (pos < contents.Length)
            {
                int symbolNumber = BitConverter.ToInt32(contents, pos);
                int symbolLength = BitConverter.ToInt32(contents, pos + 4);
                string symbolValue = new String(ca, pos + 8, symbolLength);
                if (symbolNumber != symbolTable.Count) throw new FileLoadException("Invalid symbol table entry");
                symbolTable.Add(symbolValue);
                pos += 8 + symbolLength;
            }
        }

        private void DecodeFooterBlock(byte[] contents, byte cs1, byte cs2)
        {
            DoneReading = true;
            if (contents.Length != 2 || contents[0] != cs1 || contents[1] != cs2)
                throw new FileLoadException("Invalid data checksum");
        }

        private byte ReadByte()
        {
            int b = data.ReadByte();
            if (b == -1) throw new FileLoadException("Unexpected end of data stream");
            AddByteToChecksums((byte)b);
            return (byte)b;
        }

        private byte[] ReadBytes(int dataSize)
        {
            byte[] buf = new byte[dataSize];
            if (data.Read(buf, 0, dataSize) != dataSize) throw new FileLoadException("Unexpected end of data stream");
            foreach (byte b in buf) AddByteToChecksums(b);
            return buf;
        }

        private void AddByteToChecksums(byte b)
        {
            checksum1 += b;
            checksum2 ^= b;
        }

        private int ReadInt()
        {
            return BitConverter.ToInt32(ReadBytes(4), 0);
        }

        private string readString(int length)
        {
            char[] cs = new char[length];
            byte[] bs = ReadBytes(length);
            Array.Copy(bs, cs, length);
            return new string(cs);
        }
    }
}