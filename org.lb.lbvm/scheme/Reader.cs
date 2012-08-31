using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace org.lb.lbvm.scheme
{
    public sealed class ReaderException : Exception
    {
        public ReaderException(string message)
            : base(message)
        {
        }
    }

    public sealed class Reader
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
            if (c == '\'')
            {
                expressionReader.Read();
                return new List<object> { new Symbol("quote"), Read() };
            }
            if (c == '(') return ReadList();
            if (c == '"') return ReadString();
            return ReadSymbol();
        }

        private void SkipWhitespace()
        {
            while (Char.IsWhiteSpace(Peek())) expressionReader.Read();
        }

        private object ReadList()
        {
            List<object> ret = new List<object>();
            expressionReader.Read(); // Opening parenthesis
            SkipWhitespace();
            while (Peek() != ')')
            {
                if (expressionReader.Peek() == -1) throw new ReaderException("Unexpected end of stream in reader");
                ret.Add(Read());
                SkipWhitespace();
            }
            expressionReader.Read(); // Closing parenthesis
            return ret;
        }

        private object ReadSymbol()
        {
            string value = "";
            value += (char)expressionReader.Read();
            while (expressionReader.Peek() != -1 && Peek() != ')' && !Char.IsWhiteSpace(Peek())) value += (char)expressionReader.Read();

            double d;
            int i;
            if (value == "#t") return true;
            if (value == "#f") return false;
            bool hasDecimal = value.Contains(".");
            if (!hasDecimal && int.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out i)) return i;
            if (double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out d)) return d;
            return new Symbol(value);
        }

        private object ReadString()
        {
            StringBuilder ret = new StringBuilder();
            expressionReader.Read(); // Opening quote
            while (Peek() != '"')
            {
                if (expressionReader.Peek() == -1) throw new ReaderException("Unexpected end of stream in reader");
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