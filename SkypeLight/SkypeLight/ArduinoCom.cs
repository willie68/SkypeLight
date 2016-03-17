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
        static byte[] digitToSegment = { 0x3F, 0x06, 0x5B, 0x4F, 0x66, 0x6D, 0x7D, 0x07, 0x7F, 0x6F, 0x77, 0x7C, 0x39, 0x47, 0x79, 0x71 };
        private string comPort = "COM1";
        private int displayBrightness;
        private string lastCommand;
        private SerialPort ComPort;
        private DateTime lastSendet;
        private bool colorBlink;
        private bool dotBlink;
        private byte[] digits = new byte[4];

        public void setComPort(string comPort)
        {
            this.comPort = comPort;
            if (IsOpen())
            {
                close();
            }
        }

        private bool IsOpen()
        {
            if (ComPort == null)
            {
                return false;
            }
            else
            {
                return ComPort.IsOpen;
            }
        }

        private void sendLastCommand()
        {
            sendCommand(this.lastCommand);
            throw new NotImplementedException();
        }

        private void sendCommand(string command)
        {
            lastCommand = command;
            try
            {
                open();
                ComPort.WriteLine(command);
                String line = "";
                while (ComPort.BytesToRead > 0)
                {
                    line = line + (char)ComPort.ReadChar();
                }
                lastSendet = DateTime.Now;
                Debug.WriteLine(command);
                Debug.WriteLine(line);
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine("can't connet to skypelight.");
            }
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

        // accessing the color changer
        public void sendColor(Color color)
        {
            String data = "#0,";
            byte red = color.R;
            data = data + red.ToString() + ",";
            byte green = color.G;
            data = data + green.ToString() + ",";
            byte blue = color.B;
            data = data + blue.ToString();
            if (colorBlink)
            {
                data = data + ",b";
            }
            data = data + "*";
            sendCommand(data);
        }

        public void setColorBlink(bool blink)
        {
            this.colorBlink = blink;
        }


        // accessing the skypelight display
        public void setDisplayBrightness(int brightness)
        {
            this.displayBrightness = brightness;
            sendDisplayCommand();
        }

        public void setShowDots(bool showDots)
        {
            if (showDots)
            {
                digits[1] = (byte)(digits[1] | 128);
            }
            else
            {
                digits[1] = (byte)(digits[1] & 127);
            }
        }

        public void setDotBlink(bool blink)
        {
            this.dotBlink = blink;
        }

        private void sendDisplayCommand()
        {
            open();
            string data = "d";
            data = data + displayBrightness.ToString();
            data = data + ",";
            data = data + digits[0].ToString() + ",";
            byte digit1 = (byte)(digits[1] | 128);
            data = data + digit1.ToString() + ",";

            data = data + digits[2].ToString() + ",";
            data = data + digits[3].ToString();
            if (dotBlink)
            {
                data = data + ",b";
            }
            data = data + "*";
            sendCommand(data);
        }

        public void setSegment(int index, byte digit)
        {
            if ((index > 0) && (index < 5))
            {
                digits[index - 1] = digit;
            }
        }

        public void setSegments(byte digit1, byte digit2, byte digit3, byte digit4)
        {
            setSegment(1, digit1);
            setSegment(2, digit2);
            setSegment(3, digit3);
            setSegment(4, digit4);
            sendDisplayCommand();
        }

        public void setDigits(byte digit1, byte digit2, byte digit3, byte digit4, bool showDots)
        {
            setDigit(1, digit1);
            setDigit(2, digit2);
            setDigit(3, digit3);
            setDigit(4, digit4);
            setShowDots(showDots);
            sendDisplayCommand();
        }

        public void setDigit(int index, byte digit)
        {
            setSegment(index, digitToSegment[digit % 16]);
        }

    }
}
