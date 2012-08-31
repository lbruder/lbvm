using System;
using System.IO;
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
            const string schemeSource = "(define (fac n)                   " +
                                        "  (define (ifac acc i)            " +
                                        "    (if (= 0 i)                   " +
                                        "        acc                       " +
                                        "        (ifac (* acc i) (- i 1))))" +
                                        "  (ifac 1 n))                     " +
                                        "(fac 10)                          ";

            try
            {
                var assemblerSource = org.lb.lbvm.scheme.Compiler.Compile(schemeSource);
                var program = org.lb.lbvm.Assembler.Assemble(assemblerSource);
                WriteFile(program);

                var program2 = ReadFile();
                Disassemble(program2);
                Run(program2);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private static void WriteFile(org.lb.lbvm.Program program)
        {
            using (var stream = File.Create("test.lbvm"))
                program.WriteToStream(stream);
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
