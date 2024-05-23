using Microsoft.Bot.Builder.Dialogs;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using Newtonsoft.Json;
using Microsoft.Bot.Builder.Dialogs.Choices;
using System.Xml.Linq;
using System;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class Cancel_AppointmentDialog : CancelAndHelpDialog
    {
        public Cancel_AppointmentDialog() : base(nameof(Cancel_AppointmentDialog))
        {
            
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
                {
                StartStepAsync,
                GetReasonFromUserStepAsync,
                GetCommentsFromUserStepAsync,
                GetChoiceFromUserStepAsync,
                //GetUserInputStepAsync
                }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);

        }
        private async Task<DialogTurnResult> StartStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            StartingCommand startingCommand = (StartingCommand)stepContext.Options;

            if ((string)startingCommand.Ticket_dict.RequesterKeyCode == "CLIENT")
            {
                startingCommand.Confirmation_type = 1;
                startingCommand.CreatedBy = startingCommand.Ticket_dict.RequesterPOCIds;
            }
            else
            {
                 startingCommand.Confirmation_type = 2;
                startingCommand.CreatedBy = startingCommand.Ticket_dict.AssigneeId;
            }
            var response = await BotUtility.SendScheduleReasonRequest(startingCommand, cancellationToken);
            var responseBody = response.Content.ReadAsStringAsync(cancellationToken).Result;
            
            
            if (response.IsSuccessStatusCode)
            {
               var responseObject = JsonConvert.DeserializeObject<dynamic>(responseBody);
                startingCommand.Reasons = new();
                startingCommand.Reason_ids = new();
                if (startingCommand.Reasons.Count == 0)
                {
                    foreach (var dictionary in responseObject)
                    {
                        startingCommand.Reason_ids.Add((int)dictionary["DefinationId"]);
                        startingCommand.Reasons.Add((string)dictionary["Name"]);
                    }
                }
                var reason_choices = BotUtility.ChoiceList(startingCommand.Reasons);
                return await BotUtility.SendChoicePrompt(stepContext, "Provide a reason for Cancellation?", reason_choices, cancellationToken);
            }
            else
            {
                return await BotUtility.SendErrorMessagewithend(stepContext, startingCommand, "[Status:0004]", nameof(StartDialog), cancellationToken);
            }
        }

        private async Task<DialogTurnResult> GetReasonFromUserStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            StartingCommand startingCommand = (StartingCommand)stepContext.Options;
            startingCommand.SelectedReason = (int)(stepContext.Result as FoundChoice)?.Index;
            return await BotUtility.SendTextPrompt(stepContext, "Any comments?", cancellationToken);
        }

        private async Task<DialogTurnResult> GetCommentsFromUserStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            StartingCommand startingCommand = (StartingCommand)stepContext.Options;
            startingCommand.Comment = (string)stepContext.Result;
            var response = await BotUtility.SendScheduleRequest(startingCommand, "CANCEL", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                startingCommand.Ticket_parent = "CANCEL";
                var scheduleConfirmResponse = await BotUtility.GetSchedulerConfirmation(startingCommand, cancellationToken);
                if (scheduleConfirmResponse.IsSuccessStatusCode)
                {
                    
                    await BotUtility.SendMessageWithoutReplace(stepContext, "Your current schedule has been cancelled successfully", cancellationToken);
                    startingCommand.Choice_prompt = BotUtility.ChoiceList(new List<string> { "Reschedule Visit", "Contact Technician", "Something Else", "No, Thanks" });
                    var Choice_prompt1 = new List<Choice>{
                        new Choice {Value = "Reprogramar visita", Synonyms=new List<string>{ "Reschedule Visit" } },
                        new Choice{Value = "Técnico de contacto",Synonyms=new List<string>{ "Contact Technician" }},
                        new Choice{Value = "Algo más",Synonyms=new List<string>{ "Anything else" }},
                        new Choice {Value = "No, gracias", Synonyms=new List<string>{ "No, Thanks" }},

                    };

                    if (startingCommand.lan == "en-US" || startingCommand.lan == "")
                    {
                        return await BotUtility.SendChoicePrompt(stepContext, "Is there anything else we can do for you ?", startingCommand.Choice_prompt, cancellationToken);
                    }
                    else
                    {
                        return await BotUtility.SendChoicePrompt(stepContext, "Is there anything else we can do for you ?", Choice_prompt1, cancellationToken);
                    }
                   
                 }
                else
                {
                    return await BotUtility.SendErrorMessagewithend(stepContext, startingCommand, "[Status:0008]", nameof(StartDialog), cancellationToken);
                }
            }
            else
            {
                return await BotUtility.SendErrorMessagewithend(stepContext, startingCommand, "[Status:0006]", nameof(StartDialog), cancellationToken);
            }
        }

        private async Task<DialogTurnResult> GetChoiceFromUserStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            StartingCommand startingCommand = (StartingCommand)stepContext.Options;
            startingCommand.UserIntent = (string)(stepContext.Result as FoundChoice)?.Value;
            return await stepContext.EndDialogAsync(startingCommand, cancellationToken);

            //switch (userIntent)
            //{
            //    case 0:
            //        startingCommand.OptionSelected = "Reschedule";
            //        return await stepContext.ReplaceDialogAsync(nameof(Reschedule_AppointmentDialog), startingCommand, cancellationToken);

            //    case 1:
            //        return await BotUtility.SendMessage(stepContext, startingCommand, "Connecting you to Technician!", nameof(StartDialog), cancellationToken);

            //    case 2:
            //        return await BotUtility.SendTextPrompt(stepContext, "How may I help you?", cancellationToken);

            //    case 3:
            //        return await stepContext.ReplaceDialogAsync(nameof(RatingDialog), startingCommand, cancellationToken);

            //    default:
            //        return await stepContext.ReplaceDialogAsync(nameof(StartDialog), startingCommand, cancellationToken);

            //}
        }

            //private async Task<DialogTurnResult> GetUserInputStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
            //{
            //StartingCommand startingCommand = (StartingCommand)stepContext.Options;
            //if (stepContext.Result != null)
            //    {
            //        startingCommand.Input = (string)stepContext.Result;
            //        return await stepContext.ReplaceDialogAsync(nameof(Recognize_Intent_EnrouteDialog), startingCommand, cancellationToken);
            //    }
            //return await stepContext.ReplaceDialogAsync(nameof(StartDialog), startingCommand, cancellationToken);
            //}
        

    }
}
