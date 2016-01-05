using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkypeLight
{
    class ArduinoCom
    {
        private string comPort = "COM1";
        private int brightness;
        private string lastCommand;
        private SerialPort ComPort;
        private DateTime lastSendet;
        private bool blink;

        internal void setComPort(string comPort)
        {
            this.comPort = comPort;
        }

        internal void setBrightness(int brightness)
        {
            this.brightness = brightness;
            sendLastCommand();
        }

        internal void setBlink(bool blink)
        {
            this.blink = blink;
        }

        private void sendLastCommand()
        {
            sendCommand(this.lastCommand);
            throw new NotImplementedException();
        }

        private void sendCommand(string command)
        {
            lastCommand = command;
            open();
            ComPort.WriteLine(command);
            lastSendet = DateTime.Now;
            Debug.WriteLine(command);

            throw new NotImplementedException();
        }

        private void open()
        {
            if (ComPort == null)
            {
                ComPort = new SerialPort();
            }
            if (!ComPort.IsOpen)
            {
                if (comPort != null && !comPort.Equals(""))
                {
                    ComPort.PortName = comPort;
                    ComPort.BaudRate = 9600;
                    try
                    {
                        ComPort.Open();
                        ComPort.RtsEnable = false;
                        ComPort.DtrEnable = false;
                    }
                    catch (Exception e)
                    {
                    }
                }
            }
        }

        private void close()
        {
            if (ComPort != null && ComPort.IsOpen)
            {
                ComPort.Close();
            }
        }

        internal void sendColor(Color color)
        {
            open();
            String data = "#0,";
            byte red = color.R;
            data = data + red.ToString() + ",";
            byte green = color.G;
            data = data + green.ToString() + ",";
            byte blue = color.B;
            data = data + blue.ToString() + ",";
            if (blink)
            {
                data = data + "b";
            }
            data = data + "*";
            sendCommand(data);
        }
    }
}
