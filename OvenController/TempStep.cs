using System;
using System.Collections.Generic;
using System.Text;

namespace OvenController
{
    class TempStep
    {
        public TempStep(int startTemp, int targetTemp, int minDuration, int maxDuration)
        {
            this.startTemp = startTemp;
            this.targetTemp = targetTemp;
            this.minDuration = minDuration;
            this.maxDuration = maxDuration;
            this.tempDelta = targetTemp - startTemp;
            this.minRate =  (double)tempDelta / maxDuration;
            this.maxRate = (double)tempDelta / minDuration;
        }

        public int GetTargetTemp()
        {
            return targetTemp;
        }

        public int GetMinDuration()
        {
            return minDuration;
        }

        public int GetMaxDuration()
        {
            return maxDuration;
        }

        public double maxTempRate()
        {
            return maxRate;
        }

        public double minTempRate()
        {
            return minRate;
        }

        public int maxTargetAtTime(int time)
        {
            return startTemp + (int)(maxRate * time);
        }

        public int minTargetAtTime(int time)
        {
            return startTemp + (int)(minRate * time);
        }

        public int targetAtTime(int time)
        {
            // Average target temp
            double aveTempRate = (maxRate + minRate) / 2;
            return startTemp + (int)(aveTempRate * time);
        }

        private int startTemp;
        private int targetTemp;
        private int minDuration;
        private int maxDuration;
        private double maxRate;
        private double minRate;
        private int tempDelta;
    }
}
