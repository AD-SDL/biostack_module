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
            Console.WriteLine($"Started handling action: {action.name}; {action.args}");
            if (biostack_driver.InProgress)
            {
                action.result = StepFailed("Instrument action in progress");
                return;
            }
            biostack_driver.UpdateKnownPlatePositions();
            switch (action.name)
            {
                case "calibration":
                    biostack_driver.stacker.BTIAutoCalibrationSupport();
                    action.result = StepSucceeded("pong");
                    break;
                case "set_output_position":
                    SetPlateOutputPosition(ref action);
                    break;
                case "move_claw":
                    MoveClaw(ref action);
                    break;
                case "home":
                    action.result = Home();
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
                    SendNextPlate(ref action);
                    break;
                case "restack_all":
                    RestackAllPlates(ref action);
                    break;
                default:
                    Console.WriteLine("Unknown action: " + action.name);
                    action.result = StepFailed("Unknown action: " + action.name);
                    break;
            }
            Console.WriteLine($"Finished handling action: {action.name}; {action.args}");
            biostack_driver.UpdateKnownPlatePositions();
        }

        public void MoveClaw(ref ActionRequest action)
        {
            biostack_driver.InProgress = true;
            short response = biostack_driver.stacker.MoveDeviceNSteps(0, Convert.ToInt32(action.args["steps"]));
            biostack_driver.PrintResponse(response);
            action.result = biostack_driver.CheckAction() ? StepSucceeded("Moved Claw Successfully") : StepFailed($"Error while moving claw, return code: {biostack_driver.FormatResponseCode(biostack_driver.action_return_code)}");
        }

        public void SetPlateOutputPosition(ref ActionRequest action)
        {
            Thread.Sleep(500);
            short response = biostack_driver.stacker.SaveInstrumentInterfacePos();
            Thread.Sleep(500);
            biostack_driver.PrintResponse(response);
            action.result = (response == 1) ? StepSucceeded("Saved Output Position") : StepFailed($"Error while saving output position, return code: {biostack_driver.FormatResponseCode(response)}");
        }

        public void Home()
        {
            biostack_driver.InProgress = true;
            short response = biostack_driver.stacker.HomeAllAxes();
            biostack_driver.PrintResponse(response);
            return biostack_driver.CheckAction() ? StepSucceeded("Homed BioStack Successfully") : StepFailed($"Error while homing, return code: {biostack_driver.FormatResponseCode(biostack_driver.action_return_code)}");
        }

        public void SendNextPlate(ref ActionRequest action)
        {
            if (biostack_driver.IsInstrumentOccupied)
            {
                RetrievePlate(ref action);
                if (!CheckActionSuccess(ref action)) return;
            }
            biostack_driver.InProgress = true;
            biostack_driver.PrintResponse(biostack_driver.stacker.SendNextPlateToCarrier());
            if (!biostack_driver.CheckAction())
            {
                action.result = StepFailed($"Error while sending plate to carrier, return code {biostack_driver.FormatResponseCode(biostack_driver.action_return_code)}");
                return;
            }
            biostack_driver.InProgress = true;
            biostack_driver.PrintResponse(biostack_driver.stacker.SendPlateToInstrument());
            if (!biostack_driver.CheckAction())
            {
                action.result = StepFailed($"Error while sending plate to instrument, return code {biostack_driver.FormatResponseCode(biostack_driver.action_return_code)}");
                return;
            }
            Thread.Sleep(5000);

            action.result = StepSucceeded("Sent next plate to instrument");
        }

        public void SendPlate(ref ActionRequest action)
        {
            // TODO: is there a way to do this without sending all plates to the instrument?
            var plate_index = (int) action.args["plate_index"];
            RestackAllPlates(ref action);
            if (!CheckActionSuccess(ref action)) return;
            for (int i = 0; i < plate_index; i++) {
                //BypassPlate(ref action);
                SendNextPlate(ref action);
                if (!CheckActionSuccess(ref action)) return;
                RetrievePlate(ref action);
                if (!CheckActionSuccess(ref action)) return;
            }
            action.result = StepSucceeded($"Sent {plate_index}-th plate to instrument");
        }

        public void BypassPlate(ref ActionRequest action)
        {
            // TODO: is this doable?
            biostack_driver.InProgress = true;
            biostack_driver.PrintResponse(biostack_driver.stacker.SendNextPlateToCarrier());
            if (!biostack_driver.CheckAction())
            {
                action.result = StepFailed($"Error while sending plate to carrier, return code {biostack_driver.FormatResponseCode(biostack_driver.action_return_code)}");
                return;
            }
            biostack_driver.InProgress = true;
            biostack_driver.PrintResponse(biostack_driver.stacker.TransferPlateToOutStack());
            if (!biostack_driver.CheckAction())
            {
                action.result = StepFailed($"Error while transferring plate to Out Stack, return code {biostack_driver.FormatResponseCode(biostack_driver.action_return_code)}");
                return;
            }

            action.result = StepSucceeded("Bypassed plate");
        }

        public void RetrievePlate(ref ActionRequest action)
        {
            // TODO: Gracefully handle case where there's no plate in the instrument
            biostack_driver.InProgress = true;
            biostack_driver.PrintResponse(biostack_driver.stacker.TransferPlateToOutStack());
            if (!biostack_driver.CheckAction())
            {
                action.result = StepFailed($"Error while retrieving plate, return code {biostack_driver.FormatResponseCode(biostack_driver.action_return_code)}");
                return;
            }
            Thread.Sleep(5000); // Action Complete Event gets fired too early for this call, so we wait a lil bit
            action.result = StepSucceeded("Retrieved plate from instrument");
        }

        public void RestackPlate(ref ActionRequest action)
        {
            // TODO: Gracefully handle case where there's no plates in the carrier or output stack
            biostack_driver.InProgress = true;
            biostack_driver.PrintResponse(biostack_driver.stacker.FastTransferPlateFromOutToIn());
            if (!biostack_driver.CheckAction())
            {
                action.result = StepFailed($"Error while retrieving plate, return code {biostack_driver.FormatResponseCode(biostack_driver.action_return_code)}");
                return;
            }
            action.result = StepSucceeded("Restacked plate from input to output");
        }

        public void RestackAllPlates(ref ActionRequest action)
        {
            // TODO: Gracefully handle case where there's no plates in the instrument, carrier, or output stack
            if (biostack_driver.IsInstrumentOccupied)
            {
                RetrievePlate(ref action);
                if (!CheckActionSuccess(ref action)) return;
            }
            biostack_driver.UpdateKnownPlatePositions();
            RestackPlate(ref action);
            if (!CheckActionSuccess(ref action)) return;
            biostack_driver.stacker.UpdateKnownPlatePositions()
            while(biostack_driver.IsCarrierOutputOccupied)
            {
                RestackPlate(ref action);
                if (!CheckActionSuccess(ref action)) return;
                biostack_driver.stacker.UpdateKnownPlatePositions()
            }
            if (!CheckActionSuccess(ref action)) return;
            action.result = StepSucceeded("Restacked all plates");
        }
    }
}
