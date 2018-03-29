using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OvenController
{
    class OvenConnector
    {
        public OvenConnector(string serialPort, int baud)
        {
            mcuSerialPort = new System.IO.Ports.SerialPort(serialPort, baud);
            onState = false;
            stateChangeTime = 0;
        }

        public bool IsConnected()
        {
            return mcuSerialPort.IsOpen;
        }

        public void SetShowAction(Action<int> show)
        {
            this.showTemp = show;
        }

        public bool Connect()
        {
            // Open port
            mcuSerialPort.Open();
            int count = 0;
            // Wait up to 400ms for port to open.
            while (!mcuSerialPort.IsOpen && count < 200)
            {
                System.Threading.Thread.Sleep(2);
                ++count;
            }
            // Set read timeout - approx time for four chars.
            if (mcuSerialPort.BaudRate == 9600)
            {
                mcuSerialPort.ReadTimeout = 6;
            }
            else if (mcuSerialPort.BaudRate == 4800)
            {
                mcuSerialPort.ReadTimeout = 10;
            }
            else
            {
                mcuSerialPort.ReadTimeout = 20;
            }
            return mcuSerialPort.IsOpen;
        }

        public void Disconnect()
        {
            mcuSerialPort.Close();
        }

        public int ReadTemperature()
        {
            try
            {
                mcuSerialPort.Write("c");
                // Wait for response from board.
                System.Threading.Thread.Sleep(15);
                string received = "";
                if (ReceiveData(ref received))
                {
                    // Received response is in 10ths of a degree celsius.
                    try
                    {
                        int temp = int.Parse(received);
                        if (showTemp != null)
                        {
                            showTemp(temp);
                        }
                        return temp;
                    }
                    catch (FormatException)
                    {
                        // Error converting response to value.
                    }
                }
            }
            catch (Exception)
            {

            }
            return -1;
        }


        private bool ReceiveData(ref string received)
        {
            try
            {
                string receiveString = mcuSerialPort.ReadTo("$");
                // find the # that signals the start of a value.
                int pos = receiveString.IndexOf('#');
                received = receiveString.Substring(pos + 1);
                return true;

            }
            catch (Exception e)
            {

            }
            return false;
        }

        public bool SetOvenElementOn(bool state)
        {
            string received = "";
            try
            {
                mcuSerialPort.Write(state ? "1" : "2");
            }
            catch (InvalidOperationException)
            {
                // Return false as port not avaliable.
                return false;
            }
            // sleep for response.
            System.Threading.Thread.Sleep(25);
            if (ReceiveData(ref received) && received.Contains(state ? "ON" : "OFF"))
            {
                onState = state;
                return true;
            }

            return false;
        }

        private Action<int> showTemp;
        private bool onState; // Current state of oven relay.
        private System.IO.Ports.SerialPort mcuSerialPort;
        private int stateChangeTime;
    }
}
