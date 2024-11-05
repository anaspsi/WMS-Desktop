﻿using com.citizen.sdk.LabelPrint;
using IniParser;
using IniParser.Model;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Contexts;
using System.Windows.Forms;

namespace SMTCSHARP
{
    public partial class FCombineRMLabel : Form
    {
        string itemcode = "";
        string itemValue = "";
        string itemname = "";
        string itemqty = "";
        string itemlotno = "";
        string msupqty = "";
        string mrackcd = "";

        string mretitemcd = "";
        string mretqty = "";
        string mretlot = "";
        string mretitemnm = "";
        string mUniqueCode = "";
        string OldUniqueCode = "";

        bool isScanQR = false;

        string mServerApi = "";

        string LCRPortName = string.Empty;
        string LCRBaudRate = string.Empty;
        bool isLCRConnected = false;

        private RS_232C_USB comm;
        string meas = string.Empty;

        public FCombineRMLabel()
        {
            InitializeComponent();
        }

        void initcolumn()
        {
            //DG Joined Label
            dGV_lbljoin.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dGV_lbljoin.ColumnCount = 6;
            dGV_lbljoin.Columns[0].Name = "Item Code";
            dGV_lbljoin.Columns[0].Width = 200;
            dGV_lbljoin.Columns[1].Name = "Qty";
            dGV_lbljoin.Columns[1].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            dGV_lbljoin.Columns[1].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dGV_lbljoin.Columns[2].Name = "Lot No";
            dGV_lbljoin.Columns[2].Width = 250;
            dGV_lbljoin.Columns[3].Width = 250;
            dGV_lbljoin.Columns[3].Name = "Item Name";
            dGV_lbljoin.Columns[4].Name = "Old Uniquekey";
            dGV_lbljoin.Columns[5].Name = "Value";


            dgvLogs.ColumnCount = 2;
            dgvLogs.Columns[0].Name = "Time";
            dgvLogs.Columns[0].Width = 250;
            dgvLogs.Columns[1].Name = "Value";
        }

        void printsmtlabel()
        {
            RegistryKey ckrk = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\" + Application.ProductName);
                       
            PSIPrinter PSIprinter = new PSIPrinter();
            Double doublItemValue = Convert.ToDouble(itemValue);
            Dictionary<string, string> datanya = new Dictionary<string, string>();
            datanya.Add("rackCode", mrackcd);
            datanya.Add("itemQty", mretqty);
            datanya.Add("itemCode", mretitemcd.Trim());
            datanya.Add("itemLot", mretlot.Trim());
            datanya.Add("itemKey", mUniqueCode);
            datanya.Add("itemName", mretitemnm.Trim());
            datanya.Add("mretrohs", "1");
            datanya.Add("itemValue", doublItemValue.ToString("N3"));
            PSIprinter.setData(datanya);
            PSIprinter.print(ckrk.GetValue("PRINTER_DEFAULT_BRAND").ToString().ToLower());
        }

        private void FCombineRMLabel_Load(object sender, EventArgs e)
        {
            initcolumn();
            ShowConfig();

            loadLCRConfig();            
        }

        void ShowConfig()
        {

            var parser = new FileIniDataParser();
            IniData data = parser.ReadFile("config.ini");
            mServerApi = data["SERVER"]["ADDRESS"];
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                if (textBox1.Text.Contains("|"))
                {

                    isScanQR = true;
                    // parse qr code
                    string[] QRArray = textBox1.Text.ToUpper().Split('|');

                    int strLength3N1 = 0;
                    switch (QRArray[0].Substring(0, 2).ToString())
                    {
                        case "Z3":
                            strLength3N1 = QRArray[0].Length - 4;
                            itemcode = QRArray[0].Substring(4, strLength3N1);
                            break;
                        case "3N":
                            strLength3N1 = QRArray[0].Length - 3;
                            itemcode = QRArray[0].Substring(3, strLength3N1);
                            break;
                        default:
                            itemcode = QRArray[0];
                            break;
                    }

                    string[] Array3N2;

                    if (QRArray.Length == 4)
                    {
                        // mungkin ini logic jadul terkait label jadul

                        txtlotno.Text = QRArray[2];
                    }
                    else
                    {
                        OldUniqueCode = QRArray[2];

                        Array3N2 = QRArray[1].Split(' ');
                        switch (QRArray[1].Substring(0, 3).ToString())
                        {
                            case "3N2":
                                if (Array3N2[1].All(char.IsNumber))
                                {
                                    itemqty = Array3N2[1];
                                    itemlotno = Array3N2[2];
                                }
                                break;

                            default:
                                if (Array3N2[0].All(char.IsNumber))
                                {
                                    itemlotno = Array3N2[1];
                                }
                                break;
                        }
                    }
                }
                else
                {
                    isScanQR = false;
                    if (textBox1.Text.Length > 3)
                    {
                        if (textBox1.Text.Substring(0, 3) != "3N1")
                        {
                            MessageBox.Show("Unknown Format C3 Label");
                            textBox1.Text = "";
                            return;
                        }
                    }
                    else
                    {
                        MessageBox.Show("Unknown Format C3 Label.");
                        return;
                    }
                    if (textBox1.Text.Contains(" "))
                    {
                        string[] an1 = textBox1.Text.Split(' ');
                        msupqty = an1[1];
                        int strleng = an1[0].Length - 3;
                        itemcode = an1[0].Substring(3, strleng);
                    }
                    else
                    {
                        int strleng = textBox1.Text.Length - 3;
                        itemcode = textBox1.Text.Substring(3, strleng);
                        msupqty = "";
                    }
                }

                textBox1.Text = itemcode;

                if (dGV_lbljoin.Rows.Count > 0)
                {
                    string currentItemcode = "";
                    foreach (DataGridViewRow row in dGV_lbljoin.Rows)
                    {
                        currentItemcode = row.Cells[0].Value.ToString();
                    }
                    if (!currentItemcode.Equals(itemcode))
                    {
                        MessageBox.Show("Could not join different Item Code");
                        return;
                    }
                }

                using (WebClient wc = new WebClient())
                {
                    try
                    {
                        var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(itemcode);
                        string url = String.Format(mServerApi + "/item/{0}/location", Convert.ToBase64String(plainTextBytes));
                        var res = wc.DownloadString(url);

                        JObject res_jes = JObject.Parse(res);
                        string sts = (string)res_jes["status"][0]["cd"];
                        if (sts.Equals("0"))
                        {
                            MessageBox.Show((string)res_jes["status"][0]["msg"]);
                            textBox1.Text = "";
                        }
                        else
                        {
                            textBox1.ReadOnly = true;
                            txtlotno.Focus();
                            itemname = (string)res_jes["data"][0]["SPTNO"];
                            txtMin.Text = (string)res_jes["data"][0]["STDMIN"];
                            txtMax.Text = (string)res_jes["data"][0]["STDMAX"];
                            meas = (string)res_jes["data"][0]["MEAS"];

                            if (isScanQR)
                            {
                                txtValue.Focus();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }
        }

        private void btnNew_Click(object sender, EventArgs e)
        {

            textBox1.ReadOnly = false;
            textBox1.Text = "";
            textBox1.Focus();
            txtlotno.ReadOnly = false;
            txtlotno.Text = "";

            itemcode = "";
            itemqty = "";
            itemlotno = "";
            itemname = "";
            dGV_lbljoin.Rows.Clear();

            mretitemcd = "";
            mretqty = "";
            mretlot = "";
            mretitemnm = "";

            meas = string.Empty;
        }

        private void txtlotno_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                if (txtlotno.Text.Length > 3)
                {
                    if (txtlotno.Text.Substring(0, 3) != "3N2")
                    {
                        MessageBox.Show("Unknown Format C3 Label");
                        txtlotno.Text = "";
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("Unknown Format C3 Label.");
                    return;
                }

                string[] mthis_ar = txtlotno.Text.Split(' ');
                if (msupqty != "")
                {
                    itemqty = msupqty;

                    txtlotno.ReadOnly = true;
                    txtlotno.Text = mthis_ar[1];
                    itemlotno = mthis_ar[1];
                }
                else
                {
                    if (mthis_ar[1].All(char.IsNumber))
                    {
                        itemqty = mthis_ar[1];
                        itemlotno = mthis_ar[2];
                        txtlotno.ReadOnly = true;
                    }
                }

                txtValue.Focus();
            }
        }

        void addToGrid()
        {
            //validate difference itemcode                                    
            if (dGV_lbljoin.Rows.Count > 0)
            {
                string currentItemcode = "";
                string _uniqueKey = String.Empty;
                foreach (DataGridViewRow row in dGV_lbljoin.Rows)
                {
                    if (row.Cells[4].Value != null && OldUniqueCode.Length > 3)
                    {
                        _uniqueKey = row.Cells[4].Value.ToString();
                        if (_uniqueKey.Equals(OldUniqueCode))
                        {
                            textBox1.ReadOnly = false;
                            textBox1.Text = "";
                            textBox1.Focus();
                            txtlotno.ReadOnly = false;
                            txtlotno.Text = "";
                            txtValue.Text = "";
                            txtValueStatus.Text = "";

                            itemcode = "";
                            itemqty = "";
                            itemlotno = "";
                            itemname = "";
                            OldUniqueCode = "";
                            MessageBox.Show("The label is already scanned");
                            return;
                        }
                    }

                    currentItemcode = row.Cells[0].Value.ToString();
                }
                if (currentItemcode.Equals(itemcode))
                {
                    dGV_lbljoin.Rows.Add(itemcode, itemqty, itemlotno, itemname, OldUniqueCode, itemValue);
                }
                else
                {
                    MessageBox.Show("Could not join different Item Code");
                }
            }
            else
            {
                dGV_lbljoin.Rows.Add(itemcode, itemqty, itemlotno, itemname, OldUniqueCode, itemValue);
            }
            textBox1.ReadOnly = false;
            textBox1.Text = "";
            textBox1.Focus();
            txtlotno.ReadOnly = false;
            txtlotno.Text = "";
            txtValue.Text = "";
            txtValueStatus.Text = "";

            itemcode = "";
            itemqty = "";
            itemlotno = "";
            itemname = "";
            OldUniqueCode = "";
            itemValue = "";
        }

        private void btnSaveCombine_Click(object sender, EventArgs e)
        {
            if (dGV_lbljoin.Rows.Count > 1)
            {
                if (MessageBox.Show("Are You sure ?", "", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == DialogResult.Yes)
                {
                    string itmcode_print = "";
                    string itmname_print = "";
                    string itmcode = "";
                    string qtybefore = "";
                    string lotno = "";
                    string uniqueKeyBefore = "";
                    string itmValue = "";

                    foreach (DataGridViewRow row in dGV_lbljoin.Rows)
                    {
                        itmcode_print = row.Cells[0].Value.ToString();
                        itmcode += "item[]=" + row.Cells[0].Value.ToString() + "&";
                        qtybefore += "qty[]=" + row.Cells[1].Value.ToString() + "&";
                        lotno += "lotNumber[]=" + row.Cells[2].Value.ToString() + "&";
                        uniqueKeyBefore += "oldUniqueKey[]=" + row.Cells[4].Value.ToString() + "&";
                        itmValue += "itemValue[]=" + row.Cells[5].Value.ToString() + "&";
                        itmname_print = row.Cells[3].Value.ToString();
                    }
                    using (WebClient wc = new WebClient())
                    {
                        try
                        {
                            wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                            string url = mServerApi + "/label/combine-raw-material";
                            string myparam = String.Format("{0}{1}{2}{3}{4}userId={5}&machineName={6}",
                                itmcode,
                                lotno,
                                qtybefore,
                                uniqueKeyBefore,
                                itmValue,
                                ASettings.getmyuserid(),
                                Environment.MachineName.ToString()
                                );
                            myparam = myparam.Replace("+", "%2B");
                            string res = wc.UploadString(url, myparam);

                            JObject res_jes = JObject.Parse(res);
                            string sts = (string)res_jes["status"][0]["cd"];
                            string msg = (string)res_jes["status"][0]["msg"];

                            MessageBox.Show(msg);
                            if (sts.Equals("1"))
                            {
                                mretitemcd = itmcode_print;
                                mretqty = (string)res_jes["data"][0]["NEWQTY"];
                                mretlot = (string)res_jes["data"][0]["NEWLOT"];
                                mretitemnm = itmname_print;
                                mUniqueCode = (string)res_jes["data"][0]["SER_ID"];
                                mrackcd = (string)res_jes["data"][0]["rackCode"];
                                itemValue = (string)res_jes["data"][0]["NEWVALUE"];
                                printsmtlabel();
                                dGV_lbljoin.Rows.Clear();
                            }

                            textBox1.Focus();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }
                    }
                }
            }
        }

        private void btnreturnprint_Click(object sender, EventArgs e)
        {
            if (mretitemcd.Length == 0 ||
            mretqty.Length == 0 ||
            mretlot.Length == 0 ||
            mretitemnm.Length == 0)
            {
                MessageBox.Show("Nothing to be printed");
                return;
            }
            printsmtlabel();
        }

        private void btnCancelScan_Click(object sender, EventArgs e)
        {
            int ttlrows = dGV_lbljoin.Rows.Count;
            if (ttlrows > 0)
            {
                if (MessageBox.Show("Cancel last scan ?", "", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == DialogResult.Yes)
                {
                    dGV_lbljoin.Rows.Remove(dGV_lbljoin.Rows[ttlrows - 1]);
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            panelExport.Visible = true;
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            panelExport.Visible = false;
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(String.Format("{0}/report/join-reels?dateFrom={1}&dateTo={2}",
                mServerApi,
                DTPFrom.Value.ToString("yyyy-MM-dd"),
                DTPTo.Value.ToString("yyyy-MM-dd"))
                );
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            panel1.Visible = true;
            linkLabel1.Visible = false;
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            panel1.Visible = false;
            linkLabel1.Visible = true;
        }

        private void btnClearLogs_Click(object sender, EventArgs e)
        {
            dgvLogs.Rows.Clear();
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            loadLCRConfig();

            try
            {
                if (!isLCRConnected)
                {
                    comm = new RS_232C_USB();
                    if (comm.OpenInterface(LCRPortName, LCRBaudRate) == false)
                    {
                        return;
                    }
                    btnConnect.Text = "Disconnect";
                    lblPortStatus.Text = string.Format("Connected to {0}", LCRPortName);
                    txtValue.Focus();
                }
                else
                {
                    btnConnect.Text = "Connect";
                    lblPortStatus.Text = "....";
                    comm.CloseInterface();
                }

                isLCRConnected = !isLCRConnected;

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        void loadLCRConfig()
        {
            try
            {
                RegistryKey ckrk = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\" + Application.ProductName);
                LCRPortName = ckrk.GetValue("LCR_PORT").ToString();
                LCRBaudRate = ckrk.GetValue("LCR_BAUD_RATE").ToString();
            }
            catch (Exception ex)
            {
                RegistryKey rk = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\" + Application.ProductName);
                rk.SetValue("LCR_PORT", "");
                rk.SetValue("LCR_BAUD_RATE", "9600");

                LCRBaudRate = "9600";
                MessageBox.Show("Go to Tools > Settings > [LCR Meter]");
            }
        }

        private void txtValue_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                if (textBox1.ReadOnly && dgvLogs.Rows.Count > 3)
                {
                    Double StdMin = Convert.ToDouble(txtMin.Text);
                    Double StdMax = Convert.ToDouble(txtMax.Text);
                    Double ValCal = Convert.ToDouble(txtValue.Text);
                    toolTip1.SetToolTip(txtValueStatus, string.Format("last value {0}", txtValue.Text));
                    if (ValCal >= StdMin && ValCal <= StdMax)
                    {
                        itemValue = txtValue.Text;
                        addToGrid();
                        dgvLogs.Rows.Clear();
                        txtValueStatus.ForeColor = Color.Green;
                        txtValueStatus.Text = "OK";
                    }
                    else
                    {
                        txtValueStatus.ForeColor = Color.Red;
                        txtValueStatus.Text = "NG";
                        toolTip1.ToolTipTitle = "Information";
                        dgvLogs.Rows.Clear();
                        txtValue.Text = "";

                        startFromScanLabel();
                    }
                }
            }
        }

        void startFromScanLabel()
        {
            textBox1.ReadOnly = false;
            textBox1.Text = "";
            textBox1.Focus();

            txtlotno.ReadOnly = false;
            txtlotno.Text = "";

            txtMin.Text = "";
            txtMax.Text = "";

            textBox1.Focus();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (isLCRConnected && txtValue.Focused)
            {
                if (dgvLogs.Rows.Count <= 5)
                {
                    comm.SendQueryMsg("*TRG;:MEASure?", 1000);
                    string receivedMessage = comm.MsgBuf;
                    if (receivedMessage.Length > 0)
                    {
                        if (!receivedMessage.Substring(0, 1).Equals("-"))
                        {
                            string[] LCRDataArr = receivedMessage.Split(',');

                            double LCRval;
                            if (meas.Equals("PF") || meas.Equals("UF"))
                            {
                                // FOR CAPACITOR
                                LCRval = Convert.ToDouble(LCRDataArr[1]);
                                double measValC = meas.Equals("PF") ? 1E-12 : 1E-06; // initial value                           
                                if (LCRval > 1E-12)
                                {
                                    LCRval /= measValC;
                                }
                            }
                            else
                            {
                                // FOR RESISTOR
                                double MeasVal = 0;
                                LCRval = Convert.ToDouble(LCRDataArr[0]);
                                switch (meas)
                                {
                                    case "MOHM":
                                        MeasVal = 1E+06;
                                        break;
                                    case "KOHM":
                                        MeasVal = 1E+03;
                                        break;
                                    case "OHM":
                                        MeasVal = 1;
                                        break;
                                }
                                if (LCRval < 50E+6)
                                {
                                    LCRval /= MeasVal;
                                }
                            }

                            dgvLogs.Rows.Add(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), LCRval);
                        }
                    }
                }
            }

            if (dgvLogs.Rows.Count > 5)
            {
                txtValue.Text = dgvLogs.Rows[2].Cells[1].Value.ToString();
                txtValue.Focus();
                SendKeys.Send("{ENTER}");

            }
        }

        private void FCombineRMLabel_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (isLCRConnected)
            {
                comm.CloseInterface();
            }
        }
    }
}
