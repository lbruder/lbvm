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
                Measure("Assembler", () => program = org.lb.lbvm.Assembler.Assemble(assemblerSource));
                //Print(string.Join("\r\n", assemblerSource));
                program.OnPrint += (s, ev) => Display(ev.Value);
                //WriteFile(program);
                object result = null;
                Measure("Runtime", () => result = program.Run());
                Print("");
                Print(result.ToString());
                Print(result.GetType().ToString());
            }
            catch (Exception ex)
            {
                Print(ex.Message);
            }
        }

        private void Print(string value)
        {
            Display(value + "\r\n");
        }

        private void Display(string value)
        {
            textBox1.Text += value;
            textBox1.Select(textBox1.Text.Length - 1, 0);
            textBox1.ScrollToCaret();
        }

        private void Measure(string name, Action f)
        {
            var sw = new Stopwatch();
            sw.Start();
            f();
            sw.Stop();
            Print(name + ": " + sw.Elapsed);
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
            Print(t);
        }

        private void textBox2_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
                button1.PerformClick();
        }
    }
}

// ReSharper restore LocalizableElement
