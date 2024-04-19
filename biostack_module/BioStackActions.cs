using Grapevine;
using WEI;
using static WEI.ModuleHelpers;

namespace biostack_module
{
    internal class BioStackActions
    {

        private readonly IRestServer server;
        private BioStackDriver biostack_driver;

        public BioStackActions(IRestServer server)
        {
            this.server = server;
            this.biostack_driver = server.Locals.GetAs<BioStackDriver>("biostack_driver");
        }

        public void ActionHandler(ref ActionRequest action)
        {
            switch (action.name)
            {
                case "ping":
                    action.result = StepSucceded("pong");
                    break;
                case "send_next_plate":
                    SendNextPlate(ref action);
                    break;
                case "retrieve_plate":
                    RetrievePlate(ref action);
                    break;
                case "restack":
                    RestackPlate(ref action);
                    break;
                case "send_plate":
                    break;
                case "restack_all":
                    break;
                default:
                    Console.WriteLine("Unknown action: " + action.name);
                    action.result = StepFailed("Unknown action: " + action.name);
                    break;
            }
        }

        public void SendNextPlate(ref ActionRequest action)
        {
            biostack_driver.InProgress = true;
            biostack_driver.PrintResponse(biostack_driver.stacker.SendNextPlateToCarrier());
            if (!biostack_driver.CheckAction())
            {
                action.result = StepFailed($"Error while sending plate to carrier, return code {biostack_driver.action_return_code}");
                return;
            }
            biostack_driver.InProgress = true;
            biostack_driver.PrintResponse(biostack_driver.stacker.SendPlateToInstrument());
            if (!biostack_driver.CheckAction())
            {
                action.result = StepFailed($"Error while sending plate to instrument, return code {biostack_driver.action_return_code}");
                return;
            }

            action.result = StepSucceded("Sent next plate to instrument");
        }

        public void RetrievePlate(ref ActionRequest action)
        {
            biostack_driver.InProgress = true;
            biostack_driver.PrintResponse(biostack_driver.stacker.TransferPlateToOutStack());
            if (!biostack_driver.CheckAction())
            {
                action.result = StepFailed($"Error while retrieving plate, return code {biostack_driver.action_return_code}");
                return;
            }
            Thread.Sleep(5000); // Action Complete Event gets fired too early for this call, so we wait a lil bit
            action.result = StepSucceded("Retrieved plate from instrument");
        }

        public void RestackPlate(ref ActionRequest action)
        {
            biostack_driver.InProgress = true;
            biostack_driver.PrintResponse(biostack_driver.stacker.FastTransferPlateFromOutToIn());
            if (!biostack_driver.CheckAction())
            {
                action.result = StepFailed($"Error while retrieving plate, return code {biostack_driver.action_return_code}");
                return;
            }
            action.result = StepSucceded("Restacked plate from input to output");
        }
    }
}
