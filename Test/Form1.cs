using System;
using System.Diagnostics;
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
                                        "(fac 5)                           ";

            try
            {
                var assemblerSource = org.lb.lbvm.scheme.Compiler.Compile(textBox2.Text);
                textBox1.Text = string.Join("\r\n", assemblerSource);
                var program = org.lb.lbvm.Assembler.Assemble(assemblerSource);
                Disassemble(program);
                Run(program);
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

        private void Run(org.lb.lbvm.Program program)
        {
            var sw1 = new Stopwatch();
            sw1.Start();
            object o = program.Run();
            sw1.Stop();
            textBox1.Text += "\r\n" + o + "\r\n" + sw1.Elapsed;
            textBox1.Select(textBox1.Text.Length - 1, 0);
            textBox1.ScrollToCaret();
        }

        private void textBox2_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
                button1.PerformClick();
        }
    }
}

// ReSharper restore LocalizableElement
