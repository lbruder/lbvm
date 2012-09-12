using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                textBox1.Text = "";
                var program = org.lb.lbvm.Program.FromSchemeSource(textBox2.Text);
                program.OnPrint += (s, ev) => Display(ev.Value);
                var sw = new Stopwatch();
                sw.Start();
                object result = program.Run(1, false, "asd", 1.23, new List<int> { 1, 2, 3, 4, 5});
                sw.Stop();
                Display(result + "\n" + result.GetType() + "\nRuntime: " + sw.Elapsed + "\n");
            }
            catch (Exception ex)
            {
                Display(ex.Message + "\n");
            }
        }

        private void Display(string value)
        {
            textBox1.Text += value.Replace("\r", "").Replace("\n", "\r\n");
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
