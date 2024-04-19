using Grapevine;
using Newtonsoft.Json;
using WEI;
using static WEI.ModuleHelpers;

namespace biostack_module
{
    [RestResource]
    public class BioStackRestServer
    {

        private readonly IRestServer _server;
        private BioStackActions _actions;
        public BioStackRestServer(IRestServer server)
        {
            _server = server;
            _actions = new BioStackActions(_server);
        }

        [RestRoute("Get", "/state")]
        public async Task State(IHttpContext context)
        {
            string state = GetModuleStatus(_server);
            Dictionary<string, string> response = new Dictionary<string, string>
            {
                ["State"] = state,
            };
            Console.WriteLine(state);
            await context.Response.SendResponseAsync(JsonConvert.SerializeObject(response));
        }

        [RestRoute("Get", "/about")]
        public async Task About(IHttpContext context)
        {
            // TODO
            await context.Response.SendResponseAsync(@"
                {
                    ""name"":""BioStack"",
                    ""model"":""BioTek BioStack Automated Plate Stacker"",
                    ""interface"":""wei_rest_node"",
                    ""version"":""0.1.0"",
                    ""description"":""Module for automating the BioStack plate stacker."",
                    ""actions"": [
                        {""name"":""send_next_plate"",""args"":[],""files"":[]},
                        {""name"":""retrieve_plate"",""args"":[],""files"":[]},
                        {""name"":""restack"",""args"":[],""files"":[]},
                        {""name"":""send_plate"",""args"":[],""files"":[]},
                        {""name"":""restack_all"",""args"":[],""files"":[]},
                    ],
                    ""resource_pools"":[]
                }"
            );
        }

        [RestRoute("Get", "/resources")]
        public async Task Resources(IHttpContext context)
        {
            // TODO
            await context.Response.SendResponseAsync("resources");
        }

        [RestRoute("Post", "/action")]
        [RestRoute("Get", "/action")]
        public async Task Action(IHttpContext context)
        {
            ActionRequest action;
            try
            {
                action = new ActionRequest(context);
            }
            catch (Exception ex)
            {
                // Problem with the request
                Console.WriteLine(ex.ToString());
                await ReturnResult(context, StepFailed($"Problem processing action request: {ex.Message})"));
                return;
            }

            try
            {
                GetActionLock(_server);

                // Action Definitions for the Module
                _actions.ActionHandler(ref action);

                ReleaseActionLock(_server);
            }
            catch (Exception ex)
            {
                // Unhandled exception while executing the action, module should ERROR
                UpdateModuleStatus(_server, ModuleStatus.ERROR);
                Console.WriteLine(ex.ToString());
                action.result = StepFailed("Step failed: " + ex.Message);
            }

            await action.ReturnResult();
        }
    }

}
