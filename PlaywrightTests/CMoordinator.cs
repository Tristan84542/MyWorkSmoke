using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlaywrightTests
{
    public class CMCoordinator : CMParam
    {

        public static void WaitForStage(int stage)
        {
            while (true)
            {
                lock (typeof(CMCoordinator))
                {
                    if (currentStage == stage)
                        return;
                }
                Thread.Sleep(10);  // prevent tight loop
            }
        }

        public static void StageDone()
        {
            lock (typeof(CMCoordinator))
            {
                currentStage++;
            }
        }
    }

}
