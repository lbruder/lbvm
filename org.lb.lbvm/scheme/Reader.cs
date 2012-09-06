using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace org.lb.lbvm.scheme
{
    internal sealed class Reader
    {
        private StringReader expressionReader;

        public IEnumerable<object> ReadAll(string expression)
        {
            expressionReader = new StringReader(expression);
            while (expressionReader.Peek() != -1)
            {
                object read = Read();
                if (read == null) yield break;
                yield return read;
            }
        }

        private char Peek()
        {
            return (char)expressionReader.Peek();
        }

        private object Read()
        {
            SkipWhitespace();
            if (expressionReader.Peek() == -1) return null;
            char c = Peek();
            if (c == '#') return ReadSpecial();
            if (c == '\'')
            {
                expressionReader.Read();
                return new List<object> { runtime.Symbol.fromString("quote"), Read() };
            }
            if (c == '(') return ReadList();
            if (c == '"') return ReadString();
            return ReadSymbol();
        }

        private void SkipWhitespace()
        {
            while (Char.IsWhiteSpace(Peek())) expressionReader.Read();
        }

        private object ReadSpecial()
        {
            expressionReader.Read();
            char c = Peek();
            if (c == '\\') return ReadChar();
            return ReadSymbol("#");
        }

        private object ReadChar()
        {
            expressionReader.Read();
            char c = Peek();
            if (char.IsLetter(c))
            {
                string value = "";
                while (expressionReader.Peek() != -1 && Peek() != ')' && !Char.IsWhiteSpace(Peek())) value += (char)expressionReader.Read();
                if (value == "space") value = " ";
                else if (value == "newline") value = "\n";
                else if (value == "cr") value = "\r";
                else if (value == "tab") value = "\t";
                else if (value.Length > 1) throw new exceptions.ReaderException("Invalid character constant #\\" + value);
                return value[0];
            }
            return (char)expressionReader.Read();
        }

        private object ReadList()
        {
            List<object> ret = new List<object>();
            expressionReader.Read(); // Opening parenthesis
            SkipWhitespace();
            while (Peek() != ')')
            {
                if (expressionReader.Peek() == -1) throw new exceptions.ReaderException("Unexpected end of stream in reader");
                ret.Add(Read());
                SkipWhitespace();
            }
            expressionReader.Read(); // Closing parenthesis
            return ret;
        }

        private object ReadSymbol(string prefix = "")
        {
            string value = prefix;
            value += (char)expressionReader.Read();
            while (expressionReader.Peek() != -1 && Peek() != ')' && !Char.IsWhiteSpace(Peek())) value += (char)expressionReader.Read();

            double d;
            int i;
            if (value == "#t") return true;
            if (value == "#f") return false;
            bool hasDecimal = value.Contains(".");
            if (!hasDecimal && int.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out i)) return i;
            if (double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out d)) return d;
            return runtime.Symbol.fromString(value);
        }

        private object ReadString()
        {
            StringBuilder ret = new StringBuilder();
            expressionReader.Read(); // Opening quote
            while (Peek() != '"')
            {
                if (expressionReader.Peek() == -1) throw new exceptions.ReaderException("Unexpected end of stream in reader");
                char c = (char)expressionReader.Read();
                if (c == '\\')
                {
                    c = (char)expressionReader.Read();
                    if (c == 'n') c = '\n';
                }
                ret.Append(c);
            }
            expressionReader.Read(); // Closing quote
            return ret.ToString();
        }
    }
}