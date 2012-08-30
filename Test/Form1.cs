using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

// ReSharper disable LocalizableElement

namespace Test
{
    public sealed partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                CompileFromScheme();
                //AssembleAndWriteFile();
                //var program = ReadFile();
                //Disassemble(program);
                //Run(program);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private org.lb.lbvm.Program CompileFromScheme()
        {
            const string schemeSource = "(define (fac n)                   " +
                                        "  (define (ifac acc i)            " +
                                        "    (if (= 0 i)                   " +
                                        "        acc                       " +
                                        "        (ifac (* acc i) (- i 1))))" +
                                        "  (ifac 1 n))                     " +
                                        "(fac 5)                           ";

            textBox1.Text = string.Join("\r\n", org.lb.lbvm.scheme.Compiler.Compile(schemeSource));
            return null;
        }

        private static void AssembleAndWriteFile()
        {
            string[] testSource = {
                "FUNCTION fac n",
                "FUNCTION ifac acc i &closingover ifac","PUSHINT 0","PUSHVAR i","NUMEQUAL","BFALSE label0","PUSHVAR acc","RET",
                "label0:","PUSHVAR ifac","PUSHVAR acc","PUSHVAR i","MUL","PUSHVAR i","PUSHINT 1","SUB","TAILCALL 2","ENDFUNCTION",
                "PUSHVAR ifac","PUSHINT 1","PUSHVAR n","TAILCALL 2","ENDFUNCTION",
                "PUSHVAR fac","PUSHINT 5","CALL 1","END"
            };

            using (var stream = File.Create("test.lbvm"))
                org.lb.lbvm.Assembler.Assemble(testSource).ToStream(stream);
        }

        private static org.lb.lbvm.Program ReadFile()
        {
            using (var stream = File.OpenRead("test.lbvm"))
                return org.lb.lbvm.Program.FromStream(stream);
        }

        private void Disassemble(org.lb.lbvm.Program program)
        {
            int offset = 0;
            string t = "";
            while (offset < program.Statements.Length)
            {
                var s = program.Statements[offset];
                t += string.Format("0x{0:x4}: {1}\r\n", offset, s);
                offset += s.Length;
            }
            textBox1.Text = t;
        }

        private static void Run(org.lb.lbvm.Program program)
        {
            var sw1 = new System.Diagnostics.Stopwatch();
            sw1.Start();
            object o = program.Run();
            sw1.Stop();
            MessageBox.Show(sw1.Elapsed + "  --  " + o);
        }
    }
}

// ReSharper restore LocalizableElement
