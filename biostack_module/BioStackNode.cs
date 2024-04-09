using Grapevine;
using McMaster.Extensions.CommandLineUtils;
using BTIAUTOSTACKERLib;

namespace biostack_module
{
    class BioStackNode
    {

        public static int Main(string[] args) => CommandLineApplication.Execute<BioStackNode>(args);

        [Option(Description = "Server Hostname")]
        public string Hostname { get; set; } = "+";

        [Option(Description = "Server Port")]
        public int Port { get; } = 2000;

        [Option(Description = "Whether or not to simulate the instrument")]
        public bool Simulate { get; } = false;

        public string state = ModuleStatus.INIT;
        private IRestServer server;
        private BTIAutoStacker bTIAutoStacker = new BTIAutoStacker();
        private short stackerComPort = 5;


        public void deconstruct()
        {
            Console.WriteLine("Exiting...");
            server.Stop();
            if (!Simulate)
            {
                bTIAutoStacker.CloseComPort();
            }
            Console.WriteLine("Exited...");
        }

        private void OnExecute()
        {
            InitializePlateStacker();

            server = RestServerBuilder.UseDefaults().Build();
            string server_url = "http://" + Hostname + ":" + Port.ToString() + "/";
            Console.WriteLine(server_url);
            server.Prefixes.Clear();
            server.Prefixes.Add(server_url);
            server.Locals.TryAdd("state", state);
            try
            {
                //server.Start();
                Console.WriteLine("Press enter to stop the server");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                deconstruct();
            }
        }

        private void InitializePlateStacker()
        {
            if (Simulate)
            {
                bTIAutoStacker.EnableSimulation(1);
            }
            else
            {
                bTIAutoStacker.SetComPort(stackerComPort);
                bTIAutoStacker.OpenComPort(stackerComPort);
            }
            bTIAutoStacker.ActionIsCompleted += StackerActionCompleteHandler;
            Console.Write("Communications Test (1 means OK): ");
            Console.WriteLine(bTIAutoStacker.TestCommunicationWithoutDialog());
            PrintResponse(bTIAutoStacker.IdentifyConfiguredInstrument(0));
            PrintSystemStatus();

            // Test Actions
            state = ModuleStatus.BUSY;
            //Console.Write("Get Known Plate Positions: ");
            //byte plateByte = 0;
            //bTIAutoStacker.GetKnownPlatePositions(ref plateByte);
            //Console.WriteLine(plateByte);

            bTIAutoStacker.BTIAutoCalibrationSupport();

            PrintResponse(bTIAutoStacker.HomeAllAxes());
            while (state == ModuleStatus.BUSY)
            {
                Thread.Sleep(1000);
            }

            PrintResponse(bTIAutoStacker.ClearPlateIDBarcode());
            Console.WriteLine(bTIAutoStacker.GetLatestBarcodeValue());


            state = ModuleStatus.BUSY;
            PrintResponse(bTIAutoStacker.SendNextPlateToCarrier());
            while (state == ModuleStatus.BUSY)
            {
                Thread.Sleep(1000);
            }

            state = ModuleStatus.BUSY;
            PrintResponse(bTIAutoStacker.SendPlateToInstrument());
            while (state == ModuleStatus.BUSY)
            {
                Thread.Sleep(1000);
            }
            Thread.Sleep(5000);
            Console.WriteLine(bTIAutoStacker.GetLatestBarcodeValue());
   

            state = ModuleStatus.BUSY;
            PrintResponse(bTIAutoStacker.TransferPlateToOutStack());
            while (state == ModuleStatus.BUSY)
            {
                Thread.Sleep(1000);
            }

            state = ModuleStatus.BUSY;
            PrintResponse(bTIAutoStacker.SendPlateToInstrument());
            while (state == ModuleStatus.BUSY)
            {
                Thread.Sleep(1000);
            }
            Thread.Sleep(5000);
            Console.WriteLine(bTIAutoStacker.GetLatestBarcodeValue());


            state = ModuleStatus.BUSY;
            PrintResponse(bTIAutoStacker.TransferPlateToOutStack());
            while (state == ModuleStatus.BUSY)
            {
                Thread.Sleep(1000);
            }

            state = ModuleStatus.BUSY;
            PrintResponse(bTIAutoStacker.SendPlateToInstrument());
            while (state == ModuleStatus.BUSY)
            {
                Thread.Sleep(1000);
            }
            Thread.Sleep(5000);
            Console.WriteLine(bTIAutoStacker.GetLatestBarcodeValue());


            state = ModuleStatus.BUSY;
            PrintResponse(bTIAutoStacker.TransferPlateToOutStack());
            while (state == ModuleStatus.BUSY)
            {
                Thread.Sleep(1000);
            }


            state = ModuleStatus.BUSY;
            PrintResponse(bTIAutoStacker.FastTransferPlateFromOutToIn());
            while (state == ModuleStatus.BUSY)
            {
                Thread.Sleep(1000);
            }

            state = ModuleStatus.BUSY;
            PrintResponse(bTIAutoStacker.FastTransferPlateFromOutToIn());
            while (state == ModuleStatus.BUSY)
            {
                Thread.Sleep(1000);
            }

            state = ModuleStatus.BUSY;
            PrintResponse(bTIAutoStacker.FastTransferPlateFromOutToIn());
            while (state == ModuleStatus.BUSY)
            {
                Thread.Sleep(1000);
            }

            //state = ModuleStatus.BUSY;
            //PrintResponse(bTIAutoStacker.SendPlateToInstrument());
            //while (state == ModuleStatus.BUSY)
            //{
            //    Thread.Sleep(1000);
            //}

            //state = ModuleStatus.BUSY;
            //PrintResponse(bTIAutoStacker.PresentPlateOnCarrier());
            //while (state == ModuleStatus.BUSY)
            //{
            //    Thread.Sleep(1000);
            //}

            //state = ModuleStatus.BUSY;
            //PrintResponse(bTIAutoStacker.TransferPlateToOutStack());
            //while (state == ModuleStatus.BUSY)
            //{
            //    Thread.Sleep(1000);
            //}


            //state = ModuleStatus.BUSY;
            //PrintResponse(bTIAutoStacker.SendNextDestPlateToCarrier());
            //while (state == ModuleStatus.BUSY)
            //{
            //    Thread.Sleep(1000);
            //}



            //state = ModuleStatus.BUSY;
            //PrintResponse(bTIAutoStacker.PlateToInputStackFromExtend());
            //while (state == ModuleStatus.BUSY)
            //{
            //    Thread.Sleep(1000);
            //}

            //state = ModuleStatus.BUSY;
            //PrintResponse(bTIAutoStacker.PlateFromInstrumentToClaw());
            //while (state == ModuleStatus.BUSY)
            //{
            //    Thread.Sleep(1000);
            //}

            //state = ModuleStatus.BUSY;
            //PrintResponse(bTIAutoStacker.PlateFromClawToInstrument());
            //while (state == ModuleStatus.BUSY)
            //{
            //    Thread.Sleep(1000);
            //}


            //state = ModuleStatus.BUSY;
            //PrintResponse(bTIAutoStacker.PresentPlateOnCarrier());
            //while (state == ModuleStatus.BUSY)
            //{
            //    Thread.Sleep(1000);
            //}

            //state = ModuleStatus.BUSY;
            //PrintResponse(bTIAutoStacker.TransferPlateFromInstrToExtend());
            //while (state == ModuleStatus.BUSY)
            //{
            //    Thread.Sleep(1000);
            //}
        }

        private void StackerActionCompleteHandler(short nMessageObject, short nReturnCode)
        {
            Console.WriteLine("---------------");
            Console.WriteLine("ACTION COMPLETE");
            Console.Write("Message Object: ");
            PrintResponse(nMessageObject);
            Console.Write("Return Code: ");
            PrintResponse(nReturnCode);
            //byte plateByte = 0;
            //bTIAutoStacker.GetKnownPlatePositions(ref plateByte);
            //Console.WriteLine(plateByte);
            Console.WriteLine("---------------");
            state = ModuleStatus.IDLE;
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
            PrintResponse(bTIAutoStacker.GetSystemStatus());
        }
    }
}