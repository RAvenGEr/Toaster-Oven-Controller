using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace OvenController
{
    class ProfileRunner
    {
        public ProfileRunner(OvenConnector connector)
        {
            running = false;
            this.connector = connector;

            modeOutputs = new bool[5, 4];
            for (int i = 0; i < 4; ++i)
            {
                for (int j = 0; j < 4; ++j)
                {
                    modeOutputs[i, j] = false;
                }
            }
            for (int i = 3; i < 5; ++i)
            {
                for (int j = 0; j < 4; ++j)
                {
                    modeOutputs[i, j] = true;
                }
            }

            modeOutputs[1, 0] = true;           
            modeOutputs[2, 0] = true;
            modeOutputs[2, 2] = true;
            modeOutputs[3, 3] = false;

            ovenMode = 0;
            runningMutex = new Mutex();
            runThread = new Thread(new ThreadStart(ThreadRun));
        }

        public void SetGraphPlot(Action<int[], int[], int> graphPlot)
        {
            this.plotGraph = graphPlot;
        }

        public void SetShowTarget(Action<int> showTarget)
        {
            this.showTarget = showTarget; 
        }

        public void SetProfile(TempStep[] tempSteps)
        {
            steps = tempSteps;
        }

        public void Stop()
        {
            connector.SetOvenElementOn(false);
            lock (runningMutex)
            {
                running = false;
            }
            runThread.Abort();
            connector.SetOvenElementOn(false);
        }

        public void Start()
        {
            lock (runningMutex)
            {
                if (running)
                {
                    return;
                }
                running = true;
            }
            runThread.Start();
        }

        private int TargetValue(int x2, int x3, int y1, int y2)
        {
            return y1 + (x3 * (y2 - y1)) / x2;
        }

        private void ThreadRun()
        {
            if (steps == null)
            {
                return;
            } 
            int sleepTime = 0;
            double cyclesPerSecond = 1000.0 / cycleTimeMs;
            int historySize = (int)(10 * cyclesPerSecond) + 1; // Have room for at minimum 10 seconds of extra samples.
            for (int i = 0; i < steps.Length; ++i)
            {
                historySize += (int)((steps[i].GetMaxDuration() * cyclesPerSecond) / 10); // Durations are in 10ths of second
            }
            tempHistory = new int[historySize];
            tempTarget = new int[historySize];
            historyCount = 0;
            currentTime = 0;
            currentStep = 0;
            stepElapsed = 0;
            stepHold = false;
            Stopwatch stopWatch = new Stopwatch();
            bool keepRunning;
            lock (runningMutex)
            {
                keepRunning = running;
            }
            stopWatch.Reset();
            ovenMode = 1;
            // Start at step 0
            ReadCurrentState();
            plotGraph(tempHistory, tempTarget, historyCount);
            //UpdateState();
            while (keepRunning)
            {
                for (int i = 0; i < 4; ++i)
                {
                    connector.SetOvenElementOn(modeOutputs[ovenMode, i]);
                    currentTime += cycleTimeTenths;
                    stepElapsed += cycleTimeTenths;
                    sleepTime = cycleTimeMs - (int)stopWatch.ElapsedMilliseconds;
                    if (sleepTime > 0)
                    {
                        Thread.Sleep(sleepTime);
                    }
                    stopWatch.Reset();
                    ReadCurrentState();
                    plotGraph(tempHistory, tempTarget, historyCount);
                }
                UpdateState();
                lock (runningMutex)
                {
                    keepRunning = running;
                }
            }
            // Turn off oven at end of profile
            connector.SetOvenElementOn(false);
        }

        private void ReadCurrentState()
        {
            
            currentTemp = connector.ReadTemperature();
            while (currentTemp == -1)
            {
                System.Threading.Thread.Sleep(10);
                currentTemp = connector.ReadTemperature();
            }
            tempHistory[historyCount] = currentTemp;
            tempTarget[historyCount] = steps[currentStep].targetAtTime(stepElapsed);
            if (showTarget != null)
            {
                showTarget(tempTarget[historyCount]);
            }
            if (currentTemp > steps[currentStep].GetTargetTemp())
            {
                stepHold = true;
            }
            ++historyCount;
        }

        private void UpdateState()
        {

            // See if we need to change to next step
            if ((stepElapsed > steps[currentStep].GetMinDuration() && currentTemp >= steps[currentStep].GetTargetTemp())
                || stepElapsed > steps[currentStep].GetMaxDuration())
            {
                // TODO: determine if temp is far too low to proceed.
                ++currentStep;
                stepElapsed = 0;
                stepHold = false;
                if (currentStep == steps.Length) {
                    lock (runningMutex)
                    {
                        running = false;
                    }
                    return;
                }
            }

            // Calculate current temp slope - average of last 4 temp changes
            int deltaTemp = 0;
            int pos = historyCount;
            for (int i = 0; i < 4; ++i)
            {
                --pos;
                deltaTemp += (tempHistory[pos] - tempHistory[pos - 1]);
            }
            // TODO: fix assumption here if necessary
            double measuredTempRate = deltaTemp / (4 * cycleTimeMs / 100);


            if (stepHold)
            {
                // Holding temperature.
                if (measuredTempRate < -0.0001 && ovenMode < 4)
                {
                    ++ovenMode;
                }
                else if (measuredTempRate > 0.0001 && ovenMode > 0)
                {
                    --ovenMode;
                }
                return;
            }

            int maxTarget = steps[currentStep].maxTargetAtTime(stepElapsed);
            int minTarget = steps[currentStep].minTargetAtTime(stepElapsed);
            double maxRate = steps[currentStep].maxTempRate();
            double minRate = steps[currentStep].minTempRate();

            if (maxRate < 0)
            {
                // Cooling down.
                double tempRate = maxRate;
                maxRate = minRate;
                minRate = tempRate;
                if (ovenMode > 1)
                {
                    ovenMode = 1; // Restrict to 50% duty cycle or less for cooling mode.
                }
            }

            if (currentTemp > maxTarget && measuredTempRate > minRate && ovenMode > 0)
            {
                // Turn down heat if we are above the maximum temp target and we are still heating.
                --ovenMode;
                if (measuredTempRate > maxRate && ovenMode > 0)
                {
                    --ovenMode;
                }
            }
            else if (currentTemp < minTarget && measuredTempRate < maxRate && ovenMode < 4)
            {
                // Turn up heat if we are below minimum temp target and are not heating at the maximum rate.
                ++ovenMode;
                if (measuredTempRate < minRate && ovenMode < 4)
                {
                    ++ovenMode;
                }
            }
            else if (measuredTempRate < minRate && ovenMode < 4)
            {
                ++ovenMode;
            }
            else if (measuredTempRate > maxRate && ovenMode > 0)
            {
                --ovenMode;
            }

        }

        private TempStep[] steps; // Stores the steps for the current profile.
        private const int cycleTimeMs = 500;
        private const int cycleTimeTenths = cycleTimeMs / 100;
        private bool running;
        private int currentStep; // Position in the current run.
        private int currentTime; // Current run time in 10ths of a second.
        private int currentTemp;
        private int stepElapsed;
        private bool stepHold; // Target temp reached, wait for minimum time to elapse before going to next step.
        private int[] tempHistory; // Stores temperature readings for current run.
        private int[] tempTarget; // Stores calculated set temperatures for current run.
        private int historyCount;
        private OvenConnector connector;
        private Action<int[], int[], int> plotGraph;
        private Action<int> showTarget;
        private Thread runThread;
        private Mutex runningMutex;
        private int ovenMode;
        private bool[,] modeOutputs;
    }
}
