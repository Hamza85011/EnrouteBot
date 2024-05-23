using Microsoft.Bot.Builder.Dialogs;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using Newtonsoft.Json;
using Microsoft.Bot.Builder.Dialogs.Choices;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class EnrouteDialog : CancelAndHelpDialog
    {
        
        private bool flag = false;
        public EnrouteDialog() : base(nameof(EnrouteDialog))
        {
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
                {
                StartStepAsync,
                MiddleStepAsync,
                ReturnFromDialogsAsync,
                GetInputFromSomeThingElse,
                //GetInputFromSomeThingElse
                }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);

        }
        private async Task<DialogTurnResult> StartStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            
            StartingCommand startingCommand = (StartingCommand)stepContext.Options;
            if (startingCommand.counter==0)
            {
                var response = await BotUtility.SendTicketRequest(startingCommand, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = JsonConvert.DeserializeObject<dynamic>(response.Content.ReadAsStringAsync(cancellationToken).Result);
                    var requestTypeId = responseBody["RequestTypeId"];
                    if (requestTypeId > 0)
                    {
                        startingCommand.User_id = responseBody.CreatedBy;
                        startingCommand.Ticket_dict1 = responseBody;
                        startingCommand.Ticket_dict = responseBody;
                        var name = ((string)(responseBody.RequesterPOCs)).Split(" ")[0];
                        var Choices = BotUtility.ChoiceList(new List<string> { "Reschedule Visit", "Cancel Visit", "Contact Technician", "Something Else" });
                        var Choices1 = new List<Choice>{
                        new Choice {Value = "Reprogramar visita", Synonyms=new List<string>{ "Reschedule visit" } },
                        new Choice {Value = "Cancelar visita", Synonyms=new List<string>{ "Cancel Visit" }},
                        new Choice{Value = "Técnico de contacto",Synonyms=new List<string>{ "Contact Technician" }},
                        new Choice{Value = "Algo más",Synonyms=new List<string>{ "Anything else" }}
                    };

                        if (startingCommand.lan == "en-US" || startingCommand.lan == "")
                        {
                            return await BotUtility.SendChoicePrompt(stepContext, $"Hi {name}! How are you? \r\n I am your virtual assistant, how may I help you?", Choices, cancellationToken);
                        }
                        else
                        {
                            return await BotUtility.SendChoicePrompt(stepContext, $"Hi {name}! How are you? \r\n I am your virtual assistant, how may I help you?", Choices1, cancellationToken);
                        }

                    }
                    else
                    {
                        return await BotUtility.SendErrorMessagewithend(stepContext, startingCommand, "[Status:0002]", nameof(StartDialog), cancellationToken);
                    }
                }
                else
                {
                    return await BotUtility.SendErrorMessagewithend(stepContext, startingCommand, "[Status:0002]", nameof(StartDialog), cancellationToken);
                }
            }
            else
            {
                return await stepContext.NextAsync(null,cancellationToken);
            }
            
        }

        private async Task<DialogTurnResult> MiddleStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            StartingCommand startingCommand = (StartingCommand)stepContext.Options;
            var userIntent = "";
            if (startingCommand.counter == 0)
            {
                startingCommand.counter += 1;
                userIntent = (stepContext.Result as FoundChoice)?.Value;
            }
            else
            {
                userIntent = startingCommand.UserIntent;
            }
            switch (userIntent.ToLower())
            {
                case "reschedule visit":
                    startingCommand.OptionSelected = "Reschedule";
                    return await stepContext.BeginDialogAsync(nameof(Reschedule_AppointmentDialog), startingCommand, cancellationToken);
                case "reprogramar visita":
                    startingCommand.OptionSelected = "Reschedule";
                    return await stepContext.BeginDialogAsync(nameof(Reschedule_AppointmentDialog), startingCommand, cancellationToken);

                case "cancel visit":
                    startingCommand.OptionSelected = "Cancellation";
                    return await stepContext.BeginDialogAsync(nameof(Cancel_AppointmentDialog), startingCommand, cancellationToken);

                case "cancelar visita":
                    startingCommand.OptionSelected = "Cancellation";
                    return await stepContext.BeginDialogAsync(nameof(Cancel_AppointmentDialog), startingCommand, cancellationToken);




                case "contact technician":
                    startingCommand.counter = 0;
                    startingCommand.UserIntent = null;
                    startingCommand.OptionSelected = "Contact Technician";
                    startingCommand.PrintStatement = "Connecting you to the Technician!";
                    return await stepContext.EndDialogAsync(startingCommand, cancellationToken);
                case "técnico de contacto":
                    startingCommand.counter = 0;
                    startingCommand.UserIntent = null;
                    startingCommand.OptionSelected = "Contact Technician";
                    startingCommand.PrintStatement = "Connecting you to the Technician!";
                    return await stepContext.EndDialogAsync(startingCommand, cancellationToken);





                case "something else":
                    startingCommand.OptionSelected = "something else";
                    return await BotUtility.SendTextPrompt(stepContext, "How may I help you?", cancellationToken);
                case "algo más":
                    startingCommand.OptionSelected = "something else";
                    return await BotUtility.SendTextPrompt(stepContext, "How may I help you?", cancellationToken);




                case "no, thanks":
                    startingCommand.counter = 0;
                    flag = true;
                    return await stepContext.BeginDialogAsync(nameof(RatingDialog), startingCommand, cancellationToken);

                    
                case "no, gracias":
                    startingCommand.counter = 0;
                    flag = true;
                    return await stepContext.BeginDialogAsync(nameof(RatingDialog), startingCommand, cancellationToken);


                default:
                    return await stepContext.ReplaceDialogAsync(nameof(StartDialog), startingCommand, cancellationToken);
            };
        }
        private async Task<DialogTurnResult> ReturnFromDialogsAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var startingCommand = (StartingCommand)stepContext.Options;
            
            if (startingCommand.OptionSelected == "something else")
            {
                startingCommand.Input = (string)stepContext.Result;
                if (startingCommand.end == "end")
                {
                    startingCommand.counter = 0;
                    return await stepContext.EndDialogAsync(startingCommand, cancellationToken);
                }
                startingCommand.Parent = "enroute";
                return await stepContext.BeginDialogAsync(nameof(Recognize_Intent_EnrouteDialog), startingCommand, cancellationToken);
            }
            
            if (stepContext.Result != null)
            {
                startingCommand = (StartingCommand)stepContext.Result;
                if (startingCommand.end == "end")
                {
                    startingCommand.counter = 0;
                    return await stepContext.EndDialogAsync(startingCommand, cancellationToken);
                }
                if (flag==true)
                {
                    flag = false;
                    return await stepContext.EndDialogAsync(startingCommand, cancellationToken);
                }
                return await stepContext.ReplaceDialogAsync(InitialDialogId, startingCommand, cancellationToken);
            }
           

           

            return await stepContext.ReplaceDialogAsync(nameof(StartDialog), startingCommand, cancellationToken);
        }
        



        private async Task<DialogTurnResult> GetInputFromSomeThingElse(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var startingCommand = (StartingCommand)stepContext.Result;
            if (startingCommand.end == "end")
            {
                startingCommand.counter = 0;
                return await stepContext.EndDialogAsync(startingCommand, cancellationToken);
            }
            return await stepContext.ReplaceDialogAsync(InitialDialogId, startingCommand, cancellationToken);
        }

    }
}
