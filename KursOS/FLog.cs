using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace KursOS
{
    public partial class FLog : Form
    {
        public FLog()
        {
            InitializeComponent();
        }

        private void BRem_Click(object sender, EventArgs e)
        {
            TBLog.Text = null;
            TBPass.Text = null;
        }

        private void BOK_Click(object sender, EventArgs e)
        {
            string Password = null;
            if(TBLog.Text.Length == 0 || TBPass.Text.Length == 0)
                MessageBox.Show("Заполните поля", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            else
            {
                if (File.Exists("../../UsrList.sys"))
                {
                    FileStream UsrList = new FileStream("../../UsrList.sys", FileMode.Open, FileAccess.Read);
                    StreamReader reader = new StreamReader(UsrList);
                    do
                    {
                        if (reader.ReadLine() == "[" + TBLog.Text + "]")
                        {
                            Password = reader.ReadLine();
                            break;
                        }
                    } while (!reader.EndOfStream);
                    if (Password == null)
                        MessageBox.Show("Такого пользователя не существует", "Ошибка авторизации", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    else
                    {
                        if (Password == TBPass.Text)
                        {
                            reader.Close();
                            MainWindow MW = new MainWindow(this, TBLog.Text);
                            Visible = false;
                            TBPass.Clear();
                            MW.Show();//Вход выполнен
                        }
                        else
                            MessageBox.Show("Неверный пароль", "Ошибка авторизации", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    }
                }
                else
                {
                    MessageBox.Show("Не найден файл базы данных пользоватлей", "Критическая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Application.Exit();
                }
            }
        }
    }
}
