using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Windows.Forms;

namespace SkypeLight
{
    public partial class settingsUI : Form
    {
        private Color lastColor;
        private bool doUpdate;
        private DateTime lastSendet;
        private int count;
        private bool showdate = false;

        private String ColorRed = "255,0,0";
        private String ColorYellow = "255,160,0";
        private String ColorGreen = "0,255,0";
        private String ColorBlue = "0,0,255";
        private String ColorWhite = "255,255,255";
        private String ColorBlack = "0,0,0";
        private String ColorError = "32,32,32";

        private ArduinoCom arduinoCom;

        private DateTime teamsLogChanged;
        private long teamsLogSize;

        public settingsUI()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.WindowLocation != null)
            {
                this.Location = Properties.Settings.Default.WindowLocation;
            }
            // getting all port names
            string[] names = SerialPort.GetPortNames();
            for (int i = 0; i < names.Length; i++)
            {
                cbComport.Items.Add(names[i]);
                Debug.WriteLine(names[i]);
            }
            arduinoCom = new ArduinoCom();

            String comportString = Properties.Settings.Default.Comport;
            Debug.WriteLine(comportString);
            if (comportString != null)
            {
                cbComport.Text = comportString;
                arduinoCom.setComPort(cbComport.Text);
            }

            doUpdate = true;

            timeBrightness.Value = Properties.Settings.Default.Time_Brightness;

            ColorRed = Properties.Settings.Default.ColorRed;
            ColorYellow = Properties.Settings.Default.ColorYellow;
            ColorGreen = Properties.Settings.Default.ColorGreen;
            ColorBlue = Properties.Settings.Default.ColorBlue;
            ColorWhite = Properties.Settings.Default.ColorWhite;
            ColorBlack = Properties.Settings.Default.ColorBlack;
            ColorError = Properties.Settings.Default.ColorError;
            checkBox1.Checked = Properties.Settings.Default.Show_Date;
        
            timer1.Enabled = true;

            rbBlack.Checked = true;
            cbBlink.Checked = false;
            lastColor = Color.Black;
            connectSkype();
        }

        private void connectSkype()
        {
            bool skypeAvailible = checkSkype();
            skypeAvailible = skypeAvailible && !cbManual.Checked;

            groupBox1.Enabled = !skypeAvailible;
            cbBlink.Enabled = !skypeAvailible;
            toolStripMenuItem1.Visible = !skypeAvailible;
            toolStripMenuItem2.Visible = !skypeAvailible;
            toolStripMenuItem3.Visible = !skypeAvailible;
            toolStripMenuItem4.Visible = !skypeAvailible;
            toolStripMenuItem5.Visible = !skypeAvailible;
            toolStripMenuItem6.Visible = !skypeAvailible;
            toolStripMenuItem7.Visible = !skypeAvailible;
        }

        private bool checkSkype()
        {
            return checkAvailability();
        }

        private bool checkAvailability()
        {
            if (!cbManual.Checked)
            {
                string logFile = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                logFile = Path.Combine(logFile, "Microsoft\\Teams\\logs.txt");
                FileInfo fi = new FileInfo(logFile);
                if (teamsLogChanged != null)
                {
                    if (teamsLogChanged.Equals(fi.LastWriteTime) && (teamsLogSize == fi.Length)) {
                        Debug.WriteLine(DateTime.Now.ToLongTimeString() + ": availability not changed");
                        return true;
                    }
                }
                Debug.WriteLine(DateTime.Now.ToLongTimeString() + ": update availability");
                teamsLogChanged = fi.LastWriteTime;
                teamsLogSize = fi.Length;
                string lastLine = "";
                const Int32 BufferSize = 128;
                try
                {
                    using (FileStream fileStream = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize))
                        {
                            String line;
                            while ((line = streamReader.ReadLine()) != null)
                            {
                                if ((line.IndexOf("StatusIndicatorStateService:") > -1) && (line.IndexOf("Added") > -1))
                                {
                                    lastLine = line;
                                }
                            }
                        }
                    }
                    int pos = lastLine.IndexOf("Added") + "Added".Length;
                    int pos2 = lastLine.IndexOf("(", pos);
                    lastLine = lastLine.Substring(pos, pos2 - pos).Trim().ToLower();
                    switch (lastLine)
                    {
                        case "available":
                            rbGreen.Checked = true;
                            cbBlink.Checked = false;
                            break;
                        case "busy":
                        case "inameeting":
                            rbRed.Checked = true;
                            cbBlink.Checked = false;
                            break;
                        case "berightback":
                        case "away":
                            rbYellow.Checked = true;
                            cbBlink.Checked = false;
                            break;
                        case "donotdisturb":
                        case "onthephone":
                            rbRed.Checked = true;
                            cbBlink.Checked = true;
                            break;
                        case "offline":
                            rbBlack.Checked = true;
                            cbBlink.Checked = false;
                            break;
                        default:
                            break;
                    }
                    Debug.WriteLine(String.Join(Environment.NewLine, lastLine));
                }
                catch (Exception e)
                {
                    Console.WriteLine("error reading teams log file: " + e.Message);
                    return false;
                }
            }
            return true;
        }

        private void rbRed_CheckedChanged(object sender, EventArgs e)
        {
            if (rbRed.Checked)
            {
                sendColor(Color.Red);
            }
        }

        private void rbYellow_CheckedChanged(object sender, EventArgs e)
        {
            if (rbYellow.Checked)
            {
                sendColor(Color.Yellow);
            }
        }

        private void rbGreen_CheckedChanged(object sender, EventArgs e)
        {
            if (rbGreen.Checked)
            {
                sendColor(Color.Green);
            }
        }

        private void rbBlack_CheckedChanged(object sender, EventArgs e)
        {
            if (rbBlack.Checked)
            {
                sendColor(Color.Black);
            }
        }

        private void sendColor(Color color)
        {
            bool blink = cbBlink.Checked;
            arduinoCom.setColorBlink(blink);
            lastColor = color;
            Icon icon;
            String title = "SkypeLight: ";
            arduinoCom.sendColor(color);
            if (color.Equals(Color.Black))
            {
                icon = Icon.FromHandle(((Bitmap)imageList1.Images[0]).GetHicon());
                title = title + "Aus";
            }
            else if (color.Equals(Color.Blue))
            {
                icon = Icon.FromHandle(((Bitmap)imageList1.Images[0]).GetHicon());
                title = title + "Blau";
            }
            else if (color.Equals(Color.Green))
            {
                icon = Icon.FromHandle(((Bitmap)imageList1.Images[2]).GetHicon());
                title = title + "Grün";
            }
            else if (color.Equals(Color.Red))
            {
                icon = Icon.FromHandle(((Bitmap)imageList1.Images[0]).GetHicon());
                title = title + "Rot";
            }
            else if (color.Equals(Color.Yellow))
            {
                icon = Icon.FromHandle(((Bitmap)imageList1.Images[1]).GetHicon());
                title = title + "Gelb";
            }
            else if (color.Equals(Color.White))
            {
                icon = Icon.FromHandle(((Bitmap)imageList1.Images[0]).GetHicon());
                title = title + "Weiß";
            }
            else
            {
                // kein connect
                icon = Icon.FromHandle(((Bitmap)imageList1.Images[0]).GetHicon());
                title = title + "?";
                blink = true;
            }

            notifyIcon1.Icon = icon;
            this.Icon = icon;
            notifyIcon1.Text = title;
            lastSendet = DateTime.Now;
        }

        private void sendDate()
        {
            if (Properties.Settings.Default.Show_Date)
            {

                DateTime now = DateTime.Now;
                arduinoCom.setDisplayBrightness((int)timeBrightness.Value);

                int day = now.Day;
                int month = now.Month;

                byte dayTens = (byte)(day / 10);
                byte dayOnes = (byte)(day % 10);

                byte monthTens = (byte)(month / 10);
                byte monthOnes = (byte)(month % 10);

                arduinoCom.setDigits(dayTens, dayOnes, monthTens, monthOnes, true);
            }
        }

        private void sendTime()
        {
            DateTime now = DateTime.Now;
            arduinoCom.setDisplayBrightness((int)timeBrightness.Value);
            arduinoCom.setDotBlink(true);

            int hour = now.Hour;
            int min = now.Minute;

            byte hourTens = (byte)(hour / 10);
            byte hourOnes = (byte)(hour % 10);

            byte minTens = (byte)(min / 10);
            byte minOnes = (byte)(min % 10);

            arduinoCom.setDigits(hourTens, hourOnes, minTens, minOnes, true);
        }

        private void cbBlink_CheckedChanged(object sender, EventArgs e)
        {
            sendColor(lastColor);
        }

        private void cbComport_SelectedIndexChanged(object sender, EventArgs e)
        {
            arduinoCom.setComPort(cbComport.Text);
            Properties.Settings.Default.Comport = cbComport.Text;
            Properties.Settings.Default.Save();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            if (doUpdate || ((count % 6000) == 0))
            {
                arduinoCom.sendDateTime(now);
                Debug.WriteLine(now.ToLongTimeString() + ": set datetime");
            }
            if (doUpdate)
            {
                checkAvailability();
                doUpdate = false;
            }
            count++;
            if (lastSendet == null)
            {
                lastSendet = DateTime.Now;
            }

            if (Properties.Settings.Default.Show_Date)
            {
                if (showdate)
                {
                    DateTime updatetime = now.AddSeconds(-9.0);
                    if (updatetime > lastSendet)
                    {
                        sendDate();
                        lastSendet = DateTime.Now;
                        showdate = false;
                    }
                }
                else
                {
                    DateTime updatetime = now.AddSeconds(-1.0);
                    if (updatetime > lastSendet)
                    {
                        sendTime();
                        lastSendet = DateTime.Now;
                        showdate = true;
                    }
                }
            }
            else
            {
                DateTime updatetime = now.AddSeconds(-10.0);
                if (updatetime > lastSendet)
                {
                    sendTime();
                    lastSendet = DateTime.Now;
                }
                if (now.AddSeconds(-30.0) > lastSendet)
                {
                    Debug.WriteLine("Timer 1: resend color");
                    sendColor(lastColor);
                    lastSendet = DateTime.Now;
                }
            }
            if ((count % 100) == 0)
            {
                connectSkype();
                SkypeInfo info = arduinoCom.getInfo();
                lblInfo.Text = String.Format("{0}°C  {1,0}%", info.temperature, info.humidity);
            }
        }

        private void rbBlue_CheckedChanged(object sender, EventArgs e)
        {
            if (rbBlue.Checked)
            {
                sendColor(Color.Blue);
            }
        }

        private void rbWhite_CheckedChanged(object sender, EventArgs e)
        {
            if (rbWhite.Checked)
            {
                sendColor(Color.White);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Copy window location to app settings
            Properties.Settings.Default.WindowLocation = this.Location;
            Properties.Settings.Default.Save();
        }

        private void toolStripMenuItem7_Click(object sender, EventArgs e)
        {

        }

        private void toolStripMenuItem8_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            rbRed.Checked = true;
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            rbYellow.Checked = true;
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            rbGreen.Checked = true;
        }

        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            rbBlue.Checked = true;
        }

        private void toolStripMenuItem5_Click(object sender, EventArgs e)
        {
            rbWhite.Checked = true;
        }

        private void toolStripMenuItem6_Click(object sender, EventArgs e)
        {
            rbBlack.Checked = true;
        }

        private void wiederherstellenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            if ((this.Top < 0) || (this.Left < 0))
            {
                this.Top = 0;
                this.Left = 0;
            };
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (FormWindowState.Minimized == this.WindowState)
            {
                this.Hide();
            }
        }

        private void cbManual_CheckedChanged(object sender, EventArgs e)
        {
            connectSkype();
        }

        private void timeBrightness_ValueChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.Time_Brightness = (int)timeBrightness.Value;
            Properties.Settings.Default.Save();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.Show_Date = checkBox1.Checked;
            Properties.Settings.Default.Save();
        }
    }
}
