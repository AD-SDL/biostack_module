using BTIAUTOSTACKERLib;
using Grapevine;
using WEI;
using static WEI.ModuleHelpers;

namespace biostack_module
{
    internal class BioStackDriver
    {
        private readonly IRestServer server;
        public BTIAutoStacker stacker = new();
        public bool InProgress = false;
        public short action_return_code = 1;
        public BioStackDriver(IRestServer server)
        {
            this.server = server;
            InProgress = false;
        }

        public void InitializePlateStacker(bool simulate, short stackerComPort)
        {
            if (simulate)
            {
                stacker.EnableSimulation(1);
            }
            else
            {
                stacker.SetComPort(stackerComPort);
                stacker.OpenComPort(stackerComPort);
            }
            stacker.ActionIsCompleted += StackerActionCompleteHandler;
            Console.Write("Communications Test (1 means OK): ");
            Console.WriteLine(stacker.TestCommunicationWithoutDialog());
            PrintResponse(stacker.IdentifyConfiguredInstrument(0));
            PrintSystemStatus();
        }

        public bool CheckAction()
        {
            int timeoutInSeconds = 60;
            int elapsedTimeInSeconds = 0;
            DateTime startTime = DateTime.Now;

            while (InProgress && elapsedTimeInSeconds < timeoutInSeconds)
            {
                Thread.Sleep(1000);
                elapsedTimeInSeconds = (int)(DateTime.Now - startTime).TotalSeconds;
            }
            if (InProgress || action_return_code != 1)
            {
                UpdateModuleStatus(server, ModuleStatus.ERROR);
                return false;
            }
            return true;
        }

        public void StackerActionCompleteHandler(short nMessageObject, short nReturnCode)
        {
            Console.WriteLine("---------------");
            Console.WriteLine("ACTION COMPLETE");
            Console.Write("Message Object: ");
            PrintResponse(nMessageObject);
            Console.Write("Return Code: ");
            PrintResponse(nReturnCode);
            Console.WriteLine("---------------");
            action_return_code = nReturnCode;
            InProgress = false;
        }

        public void PrintResponse(short response_code)
        {
            if (response_code > 1)
            {
                Console.WriteLine(response_code.ToString("X4"));
            }
            else
            {
                Console.WriteLine(response_code.ToString());
            }
        }

        public void PrintSystemStatus()
        {
            Console.Write("System Status: ");
            PrintResponse(stacker.GetSystemStatus());
        }
    }
}
