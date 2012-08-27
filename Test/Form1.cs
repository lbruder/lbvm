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
            try
            {
                AssembleAndWriteFile();
                var program = ReadFile();
                Disassemble(program);
                Run(program);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private static void AssembleAndWriteFile()
        {
            string[] testSource = {
                "JMP label4",
                "label0:", "ENTER 1 fac", "DEFINE n", "POP", "JMP label3",
                "label1:", "ENTER 3 ifac", "DEFINE ifac", "DEFINE i", "DEFINE acc", "POP", "PUSHINT 0", "GET i", "NUMEQUAL", "BFALSE label2", "GET acc", "RET",
                "label2:", "GET ifac", "GET acc", "GET i", "MUL", "GET i", "PUSHINT 1", "SUB", "GET ifac", "TAILCALL 3",
                "label3:", "GETLABEL label1", "DEFINE ifac", "GET ifac", "PUSHINT 1", "GET n", "GET ifac", "TAILCALL 3",
                "label4:", "GETLABEL label0", "DEFINE fac", "GET fac", "PUSHINT 10", "CALL 1", "END" };

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
