﻿using Microsoft.Win32;
using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Text;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SMTCSHARP
{
    public partial class FLOGIN : Form
    {
        FRM_MAIN fmain;
        bool islogedin = false;
        public FLOGIN()
        {
            InitializeComponent();
        }

        private void txtusername_TextChanged(object sender, EventArgs e)
        {

        }

        string getHashSHA256(string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            SHA256Managed managed = new SHA256Managed();
            byte[] hash = managed.ComputeHash(bytes);
            string hashStr = string.Empty;
            foreach (byte x in hash)
            {
                hashStr += String.Format("{0:x2}", x);
            }
            return hashStr;
        }

        private void txtusername_Enter(object sender, EventArgs e)
        {
            txtusername.ForeColor = Color.Black;
            if (txtusername.Text == "UserID")
            {
                txtusername.Text = "";
            }
        }

        private void txtusername_Leave(object sender, EventArgs e)
        {
            if (txtusername.Text.Trim() == "")
            {
                txtusername.Text = "UserID";
                txtusername.ForeColor = Color.Silver;
            }
        }

        private void btntest_Click(object sender, EventArgs e)
        {
            pictureBox1.InitialImage = null;
            showImageInfo("Please \n wait...");
            pictureBox1.BackColor = Color.LightSkyBlue;
            backgroundWorker1.RunWorkerAsync();

        }

        private void showImageInfo(string thetext)
        {
            Bitmap bm = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            using (Graphics g = Graphics.FromImage(bm))
            {
                using (SolidBrush myBrush = new SolidBrush(Color.Black))
                {
                    using (Font myFont = new Font("Consolas", 8))
                    {
                        g.TextRenderingHint = TextRenderingHint.AntiAlias;
                        g.DrawString(thetext, myFont, myBrush, 0, 0);
                        pictureBox1.Image = bm;
                    }
                }
            }
        }

        private void FLOGIN_Load(object sender, EventArgs e)
        {

            RegistryKey ckrk = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\" + Application.ProductName);
            if (ckrk == null)
            {
                savesetting();
            }
            else
            {
                txts_server.Text = ckrk.GetValue("SERVER").ToString();
                txts_db.Text = ckrk.GetValue("DB").ToString();
                txts_user.Text = ckrk.GetValue("USER").ToString();
                txts_pw.Text = ckrk.GetValue("PW").ToString();
                ASettings.setmys_server(txts_server.Text);
                ASettings.setmys_db(txts_db.Text);
                ASettings.setmys_user(txts_user.Text);
                ASettings.setmys_pw(txts_pw.Text);
            }
            ActiveControl = txtusername;
            this.Text = string.Concat(this.Text, " ", ASettings.getVersion());
        }

        void savesetting()
        {
            RegistryKey rk = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\" + Application.ProductName);
            rk.SetValue("SERVER", txts_server.Text);
            rk.SetValue("DB", txts_db.Text);
            rk.SetValue("USER", txts_user.Text);
            rk.SetValue("PW", txts_pw.Text);

            ASettings.setmys_server(txts_server.Text);
            ASettings.setmys_db(txts_db.Text);
            ASettings.setmys_user(txts_user.Text);
            ASettings.setmys_pw(txts_pw.Text);
        }

        void setstatesettingRO_ctl(bool pstate)
        {
            txts_server.ReadOnly = pstate;
            txts_db.ReadOnly = pstate;
            txts_user.ReadOnly = pstate;
            txts_pw.ReadOnly = pstate;
        }

        private void btnsave_Click(object sender, EventArgs e)
        {
            if (btnsave.Text == "Edit")
            {
                setstatesettingRO_ctl(false);
                btnsave.Text = "Save";
                txts_server.Focus();
            }
            else
            {
                btnsave.Text = "Edit";
                savesetting();
                setstatesettingRO_ctl(true);
            }
        }

        private void btnsignin_Click(object sender, EventArgs e)
        {
            lblinfo.Text = "Please wait ...";
            ASettings.setmyuserid(txtusername.Text);
            ASettings.setmypw(txtpassword.Text);
            string constr = String.Format(ASettings.getconstr(), txts_server.Text, txts_db.Text, txts_user.Text, txts_pw.Text);
            int i = 0;
            bool isvalid = false;
            btnsignin.Enabled = false;
            Task.Factory.StartNew(() =>
            {
                SqlConnection conn = new SqlConnection(constr);
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT MSTEMP_ID, concat(MSTEMP_FNM, ' ', MSTEMP_LNM) FULLNAME,MSTEMP_FNM,MSTEMP_GRP, MSTEMP_PW FROM MSTEMP_TBL WHERE MSTEMP_ID='" + txtusername.Text + "'", conn);
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {

                    if (dr["MSTEMP_PW"].ToString() == getHashSHA256(txtpassword.Text))
                    {
                        ASettings.setmyuser(dr["FULLNAME"].ToString());
                        ASettings.setmygroup(dr["MSTEMP_GRP"].ToString());
                        ASettings.setmyuserfname(dr["MSTEMP_FNM"].ToString());
                        isvalid = true;
                        islogedin = true;
                        ASettings.setmyrunsess(true);
                    }
                    i++;
                }
                dr.Close();
                conn.Close();
            }).ContinueWith(task =>
            {
                if (i > 0)
                {
                    if (isvalid)
                    {
                        lblinfo.Text = "go";
                        txtpassword.Text = "";
                        txtusername.Text = "";
                        this.Hide();
                        FormCollection fc = Application.OpenForms;
                        bool isviewed = false;
                        foreach (Form frm in fc)
                        {
                            if (frm.Name == "FRM_MAIN")
                            {
                                isviewed = true;
                            }
                        }
                        if (isviewed == false && ASettings.getmyrunsess())
                        {
                            fmain = new FRM_MAIN();
                            fmain.ShowDialog();
                        }
                    }
                    else
                    {
                        lblinfo.Text = "Invalid password";
                    }
                }
                else
                {
                    lblinfo.Text = "Failed";
                }
                btnsignin.Enabled = true;
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void backgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            string constr = String.Format(ASettings.getconstr(), txts_server.Text, txts_db.Text, txts_user.Text, txts_pw.Text);
            using (SqlConnection conn = new SqlConnection(constr))
            {
                try
                {
                    conn.Open();
                    pictureBox1.BackColor = Color.SeaGreen;
                    showImageInfo("Success");
                    SetText("Success");
                }
                catch (SqlException exx)
                {
                    showImageInfo("Sorry");
                    pictureBox1.BackColor = Color.DarkRed;
                    SetText(exx.Message);
                }

            }
        }

        delegate void SetTextCallback(string text);
        delegate void SetTextCallbacklog(string text);

        private void SetText(string text)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (this.pictureBox1.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                toolTip1.SetToolTip(pictureBox1, text);
                //this.textBox1.Text = text;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (islogedin)
            {

            }
            if (!ASettings.getmyrunsess())
            {
                islogedin = false;
                if (!this.Visible)
                {
                    this.Visible = true;
                }

            }
        }

        private void txtpassword_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                btnsignin.Focus();
            }
        }

        private void txtusername_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                txtpassword.Focus();
            }
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedIndex == 1)
            {
                panel1.Visible = true;
                textBox1.Text = "";
            }
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                if (textBox1.Text.Equals("setSetting"))
                {
                    panel1.Visible = false;
                }
                else
                {
                    MessageBox.Show("Not authorized", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

        }
    }
}
