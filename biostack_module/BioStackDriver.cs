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
        public bool IsCarrierInputOccupied = false;
        public bool IsCarrierOutputOccupied = false;
        public bool IsInstrumentOccupied = false;
        public BioStackDriver(IRestServer server)
        {
            this.server = server;
            InProgress = false;
        }

        public void InitializePlateStacker(bool simulate, short stackerComPort)
        {
            Console.WriteLine("Initializing Device");
            if (simulate)
            {
                Console.WriteLine("Note: Simulation Mode Enabled, no commands will actually be sent to device");
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
            Console.Write("Identifying Configured Insrument: ");
            PrintResponse(stacker.IdentifyConfiguredInstrument(0));
            PrintSystemStatus();

            var system_status = stacker.GetSystemStatus();
            if (system_status != 1)
            {
                Console.WriteLine($"System Status is not OK, trying to home...");
                InProgress = true;
                stacker.HomeAllAxes();
                if (!CheckAction())
                {
                    throw new Exception($"Error: tried to Home on initialize due to not OK system status, but failed with error code {FormatResponseCode(action_return_code)}. Current system status is {FormatResponseCode(stacker.GetSystemStatus())}");
                }
            }
            UpdateKnownPlatePositions();
            Console.WriteLine("Successfully initialized instrument");
        }

        public void UpdateKnownPlatePositions()
        {
            byte plate_positions = 0;
            stacker.GetKnownPlatePositions(ref plate_positions);
            IsCarrierInputOccupied = (plate_positions & 0x01) == 0x01;
            IsCarrierOutputOccupied = (plate_positions & 0x02) == 0x02;
            IsInstrumentOccupied = (plate_positions & 0x04) == 0x04;
            PrintPlatePositions();
        }

        public void PrintPlatePositions()
        {
            Console.WriteLine("=====================");
            Console.WriteLine("Plate Positions");
            Console.WriteLine("---------------------");
            Console.WriteLine($"Carrier Input: {IsCarrierInputOccupied}");
            Console.WriteLine($"Carrier Output: {IsCarrierOutputOccupied}");
            Console.WriteLine($"Instrument: {IsInstrumentOccupied}");
            Console.WriteLine("=====================");
        }

        public bool CheckAction()
        {
            int timeoutInSeconds = 30;
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

        public string FormatResponseCode(short response_code)
        {
            if (response_code > 1)
            {
                return response_code.ToString("X4");
            }
            else
            {
                return response_code.ToString();
            }
        }

        public void PrintResponse(short response_code)
        {
            Console.WriteLine(FormatResponseCode(response_code));
        }

        public void PrintSystemStatus()
        {
            Console.Write("System Status: ");
            PrintResponse(stacker.GetSystemStatus());
        }
    }
}
