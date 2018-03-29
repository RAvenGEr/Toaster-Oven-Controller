using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;

namespace OvenController
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            settings = new TempSettings();
            grafx = BufferedGraphicsManager.Current.Allocate(tempGraph.CreateGraphics(), new Rectangle(tempGraph.Location, tempGraph.Size));
        }

        private TempSettings settings; // Encapsulates the settings database.

        private int errorCount; // store consecutive error count.

        private BufferedGraphics grafx;

        private OvenConnector connector;

        private ProfileRunner runner;

        private void comCombo_DropDown(object sender, EventArgs e)
        {
            // Add serial ports to comboBox
            String[] serialPorts = null;
            comCombo.Items.Clear();
            serialPorts = SerialPort.GetPortNames();
            foreach (String port in serialPorts)
            {
                comCombo.Items.Add(port);
            }
        }

        private void exitButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void connectButton_Click(object sender, EventArgs e)
        {
            // Verify a com port is entered in comCombo.
            if (comCombo.Text == "" || !comCombo.Text.ToLower().Contains("com"))
            {
                return;
            }
            if (connector != null)
            {
                connector.Disconnect();
            }
            connector = new OvenConnector(comCombo.Text, int.Parse(baudCombo.Text));
            // Check that port is open.
            if (connector.Connect())
            {
                connector.SetShowAction(ShowTemp);
                connectButton.Enabled = false;
                disconnectButton.Enabled = true;
                comCombo.Enabled = false;
                baudCombo.Enabled = false;
                onButton.Enabled = true;
                offButton.Enabled = true;
                startButton.Enabled = true;
            }
        }

        private void disconnectButton_Click(object sender, EventArgs e)
        {
            connector.Disconnect();
            connectButton.Enabled = true;
            disconnectButton.Enabled = false;
            comCombo.Enabled = true;
            baudCombo.Enabled = true;
            onButton.Enabled = false;
            offButton.Enabled = false;
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            // TODO: Load temp steps from database.
            //-------Reflow Profile--------------
            int startTemp = Math.Max(30, connector.ReadTemperature());
            TempStep[] steps = new TempStep[6];

            // Hard coded reflow profile
            steps[0] = new TempStep(400, 1700, 1380, 1800);
            steps[1] = new TempStep(1700, 2200, 310, 640);
            steps[2] = new TempStep(2200, 2430, 210, 280);
            steps[3] = new TempStep(2430, 2200, 210, 280);
            steps[4] = new TempStep(2200, 1500, 300, 450);
            steps[5] = new TempStep(1500, 300, 200, 300);

            // Start running.
            if (runner != null) {
                runner.Stop();
            }
            runner = new ProfileRunner(connector);
            runner.SetGraphPlot(PlotTempGraph);
            runner.SetShowTarget(ShowTarget);
            runner.SetProfile(steps);
            runner.Start();

            stopButton.Enabled = true;
            startButton.Enabled = false;
            disconnectButton.Enabled = false;
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            if (runner != null)
            {
                runner.Stop();
            }
            stopButton.Enabled = false;
            startButton.Enabled = true;
            disconnectButton.Enabled = true;
        }

        private void onButton_Click(object sender, EventArgs e)
        {
            // Ensure we are connected to board.
            if (connector != null && connector.IsConnected())
            {
                // Attempt to turn relay on.
                connector.SetOvenElementOn(true);
            }
        }

        private void offButton_Click(object sender, EventArgs e)
        {
            // Ensure we are connected to board.
            if (connector != null && connector.IsConnected())
            {
                // Attempt to turn relay off.
                connector.SetOvenElementOn(false);
            }
        }

        delegate void ShowTempCallback(int temp);

        private void ShowTemp(int temp)
        {
            if (this.tempTextBox.InvokeRequired)
            {
                ShowTempCallback d = new ShowTempCallback(ShowTemp);
                this.Invoke(d, new object[] { temp });
                return;
            }
            Double tempDec = temp / 10.0;
            tempTextBox.Text = tempDec.ToString("N1");
        }

        delegate void ShowTargetCallback(int temp);

        private void ShowTarget(int temp)
        {
            if (this.targetTextBox.InvokeRequired)
            {
                ShowTargetCallback d = new ShowTargetCallback(ShowTarget);
                this.Invoke(d, new object[] { temp });
                return;
            }
            Double tempDec = temp / 10.0;
            targetTextBox.Text = tempDec.ToString("N1");
        }

        delegate void PlotGraphCallback(int[] tempHistory, int[] tempTargets, int count);

        private void PlotTempGraph(int[] tempHistory, int[] tempTargets, int count)
        {
            if (this.tempGraph.InvokeRequired)
            {
                PlotGraphCallback d = new PlotGraphCallback(PlotTempGraph);
                this.Invoke(d, new object[] { tempHistory, tempTargets, count });
                return;
            }
            Graphics graphGraphic = grafx.Graphics;
            int height = tempGraph.Height;
            double yScale = height / 2600; // 0-260
            Pen pen = new Pen(Color.Green);
            Pen cPen = new Pen(Color.Blue);
            int x1, y1, x2, y2;
            int cy1, cy2;
            x1 = y1 = cy1 = tempGraph.Height;
            int historyCount = tempHistory.Length;
            graphGraphic.Clear(this.tempGraph.BackColor);
            double step = tempGraph.Width / (historyCount + 1.0);
            for (int i = 0; i < count; ++i )
            {
                x2 = (int)(i * step);
                y2 = tempGraph.Height - (int)(tempHistory[i] * yScale);
                cy2 = tempGraph.Height - (int)(tempTargets[i] * yScale);
                if (i > 1)
                {
                    graphGraphic.DrawLine(pen, x1, y1, x2, y2);
                    graphGraphic.DrawLine(cPen, x1, cy1, x2, cy2);
                }
                x1 = x2;
                y1 = y2;
                cy1 = cy2;
            }
            //graphGraphic.Dispose();
            grafx.Render();
        }

        private void tempUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (connector != null && connector.IsConnected() && startButton.Enabled)
            {
                connector.ReadTemperature();
            }
        }

    }
}
