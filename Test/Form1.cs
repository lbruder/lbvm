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
            try
            {
                textBox1.Text = "";
                string[] assemblerSource = null;
                org.lb.lbvm.Program program = null;
                Measure("Compiler", () => assemblerSource = org.lb.lbvm.scheme.Compiler.Compile(textBox2.Text));
                //textBox1.Text = string.Join("\r\n", assemblerSource);
                Measure("Assembler", () => program = org.lb.lbvm.Assembler.Assemble(assemblerSource));
                //WriteFile(program);
                object result = null;
                Measure("Runtime", () => result = program.Run());
                textBox1.Text += "\r\n" + result + "\r\n" + result.GetType();
            }
            catch (Exception ex)
            {
                textBox1.Text += "\r\n" + ex.Message;
            }
        }

        private void Measure(string name, Action f)
        {
            var sw = new Stopwatch();
            sw.Start();
            f();
            sw.Stop();
            textBox1.Text += name + ": " + sw.Elapsed + "\r\n";
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

        private void textBox2_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
                button1.PerformClick();
        }
    }
}

// ReSharper restore LocalizableElement
