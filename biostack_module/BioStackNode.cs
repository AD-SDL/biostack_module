using Grapevine;
using McMaster.Extensions.CommandLineUtils;
using WEI;
using static WEI.ModuleHelpers;

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

        [Option(Description = "The COM Port to use when communicating with the BioStack", ShortName = "c")]
        public short stackerComPort { get; } = 5;


        public string state = ModuleStatus.INIT;
        private readonly IRestServer server = RestServerBuilder.UseDefaults().Build();
        private readonly BioStackDriver biostack_driver;

        public BioStackNode()
        {
            biostack_driver = new(server);
        }

        private void OnExecute()
        {
            try
            {
                RunServer();
                biostack_driver.InitializePlateStacker(Simulate, stackerComPort);
                UpdateModuleStatus(server, ModuleStatus.IDLE);
            }
            catch (Exception ex)
            {
                // Even if we can't connect to the device, keep the REST Server going
                Console.WriteLine(ex.ToString());
                UpdateModuleStatus(server, ModuleStatus.ERROR);
            }
            Console.WriteLine("Press enter to stop the server");
            Console.ReadLine();
            deconstruct();
        }
        public void deconstruct()
        {
            Console.WriteLine("Exiting...");
            try { server.Stop(); } catch (Exception ex) { Console.Write(ex.ToString()); }
            // Any Device specific cleanup goes here
            if (!Simulate)
            {
                biostack_driver.stacker.CloseComPort();
            }
            Console.WriteLine("Exited...");
        }

        private void RunServer()
        {
            server.Prefixes.Clear();
            server.Prefixes.Add("http://" + Hostname + ":" + Port.ToString() + "/");
            server.Locals.TryAdd("state", state);
            server.Locals.TryAdd("biostack_driver", biostack_driver);
            server.Start();
        }
    }
}