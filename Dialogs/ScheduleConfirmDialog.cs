using Microsoft.Bot.Builder.Dialogs;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;
using Microsoft.Bot.Builder.Dialogs.Choices;
using System.Xml.Linq;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class ScheduleConfirmDialog : CancelAndHelpDialog
    {
        public ScheduleConfirmDialog() : base(nameof(ScheduleConfirmDialog))
        {
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                StartStepAsync,
                GetCurrentScheduleConfirmationStepAsync,
                GetCancelReasonStepAsync
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);

        }

        private async Task<DialogTurnResult> StartStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            StartingCommand startingCommand = (StartingCommand)stepContext.Options;
            var response = await BotUtility.SendTicketRequest(startingCommand, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var responseBody = JsonConvert.DeserializeObject<dynamic>(response.Content.ReadAsStringAsync(cancellationToken).Result);
                if (responseBody.TicketId > 0)
                {
                    startingCommand.User_id = responseBody.CreatedBy;
                    startingCommand.Ticket_dict = responseBody;
                    if (responseBody.ScheduledStartDate != null)
                    {
                        startingCommand.Ticket_scheduler_id = responseBody.CalendarItemId;
                        startingCommand.Calendar_item_id = responseBody.CalendarItemId;
                        if (responseBody.RequesterKeyCode == "CLIENT")
                        {
                            startingCommand.Confirmation_type = 1;
                            startingCommand.CreatedBy = startingCommand.Ticket_dict.RequesterPOCIds;
                        }
                        else
                        {
                            startingCommand.Confirmation_type = 2;
                            startingCommand.CreatedBy = startingCommand.Ticket_dict.AssigneeId;
                        }

                        if (1 == 1)
                        {
                           if(startingCommand.Tenant_id=="17")
                           {
                                startingCommand.Choice_prompt = BotUtility.ChoiceList(new List<string> { "Re-Schedule", "Cancel Schedule", "No, Thanks"});
                                var Choice_prompt1 = new List<Choice>{
                                    new Choice{Value = "Reprogramar",Synonyms=new List<string>{ "Reschedule" }},
                                    new Choice {Value = "Cancelar programa", Synonyms=new List<string>{ "cancel program" }},
                                };
                                if (startingCommand.lan == "en-US" || startingCommand.lan == "")
                                {
                                    return await BotUtility.SendChoicePrompt(stepContext, $"Current Schedule: {(DateTimeOffset)startingCommand.Ticket_dict.ScheduledStartDate:MM/dd/yyyy h:mm tt}", startingCommand.Choice_prompt, cancellationToken);
                                }
                                else
                                {
                                    return await BotUtility.SendChoicePrompt(stepContext, $"Current Schedule: {(DateTimeOffset)startingCommand.Ticket_dict.ScheduledStartDate:MM/dd/yyyy h:mm tt}", Choice_prompt1, cancellationToken);
                                }
                            }
                           else
                           {
                                startingCommand.Choice_prompt = BotUtility.ChoiceList(new List<string> { "Re-Schedule", "Cancel Schedule", "Confirm Schedule" });
                                var Choice_prompt1 = new List<Choice>{
                                    new Choice{Value = "Reprogramar",Synonyms=new List<string>{ "Reschedule" }},
                                    new Choice {Value = "Cancelar programa", Synonyms=new List<string>{ "cancel program" }},
                                    new Choice {Value = "Confirmar horario", Synonyms=new List<string>{ "Confirm Schedule" } },
                                };
                                if (startingCommand.lan == "en-US" || startingCommand.lan == "")
                                {
                                    return await BotUtility.SendChoicePrompt(stepContext, $"{startingCommand.PhoneNumber};Current Schedule: {(DateTimeOffset)startingCommand.Ticket_dict.ScheduledStartDate:MM/dd/yyyy h:mm tt}", startingCommand.Choice_prompt, cancellationToken);
                                }
                                else
                                {
                                    return await BotUtility.SendChoicePrompt(stepContext, $"{startingCommand.PhoneNumber};Current Schedule: {(DateTimeOffset)startingCommand.Ticket_dict.ScheduledStartDate:MM/dd/yyyy h:mm tt}", Choice_prompt1, cancellationToken);
                                }
                            }
                           
                           
                        }

                    }
                    else
                    {
                        return await BotUtility.SendMessage(stepContext, startingCommand, "Ticket is not scheduled!", nameof(StartDialog), cancellationToken);
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


        private async Task<DialogTurnResult> GetCurrentScheduleConfirmationStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            StartingCommand startingCommand = (StartingCommand)stepContext.Options;
            startingCommand.Confirm = (int)(stepContext.Result as FoundChoice)?.Index;

            switch (startingCommand.Confirm)
            {
                case 2:
                    startingCommand.Ticket_parent = "SCHEDULE CONFIRM";
                    var response = await BotUtility.GetSchedulerConfirmation(startingCommand, cancellationToken);
                    if (response.IsSuccessStatusCode)
                    {
                        if (startingCommand.Tenant_id == "17")
                        {
                            startingCommand.PrintStatement = "Have a nice day.";
                        }
                        else
                        {
                            startingCommand.PrintStatement = "Thanks for Confirmation! \r\n Have a nice day.";
                        }
                        return await stepContext.NextAsync(startingCommand, cancellationToken);
                        //return await stepContext.BeginDialogAsync(nameof(RatingDialog), startingCommand, cancellationToken);
                    }
                    else
                    {
                        return await BotUtility.SendErrorMessagewithend(stepContext, startingCommand, "[Status:0008]", nameof(StartDialog), cancellationToken);
                    }

                case 1:
                    var reasonResponse = await BotUtility.SendScheduleReasonRequest(startingCommand, cancellationToken);
                    if (reasonResponse.IsSuccessStatusCode)
                    {
                        var responseContent = JsonConvert.DeserializeObject<dynamic>(reasonResponse.Content.ReadAsStringAsync(cancellationToken).Result);
                        List<Choice> reasons = new();
                        startingCommand.Reason_ids = new();
                        if (startingCommand.Reason_ids.Count == 0)
                        {
                            foreach (var value in responseContent)
                            {
                                startingCommand.Reason_ids.Add((int)value.DefinationId);
                                reasons.Add(new Choice { Value = (string)value.Name });
                            }
                        }
                        return await BotUtility.SendChoicePrompt(stepContext, "Provide a reason for cancellation", reasons, cancellationToken);
                    }
                    else
                    {
                        return await BotUtility.SendErrorMessagewithend(stepContext, startingCommand, "[Status:0004]", nameof(StartDialog), cancellationToken);
                    }
                case 0:
                    return await stepContext.BeginDialogAsync(nameof(ConfirmScheduleFuncDialog), startingCommand, cancellationToken);

                default:
                    return await stepContext.ReplaceDialogAsync(nameof(RatingDialog), startingCommand, cancellationToken);
            }

        }

        private async Task<DialogTurnResult> GetCancelReasonStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            StartingCommand startingCommand = (StartingCommand)stepContext.Options;
            switch (startingCommand.Confirm)
            {
                case 2:
                    startingCommand = (StartingCommand)stepContext.Result;
                    //if (startingCommand.end == "end")
                    //{
                    //    return await stepContext.EndDialogAsync(startingCommand, cancellationToken);
                    //}
                    return await stepContext.EndDialogAsync(startingCommand, cancellationToken);
                case 1:
                    startingCommand.SelectedReason = (int)(stepContext.Result as FoundChoice)?.Index;
                    var response = await BotUtility.GetSchedulerConfirmation(startingCommand, cancellationToken);
                    if (!response.IsSuccessStatusCode)
                    {
                        return await BotUtility.SendErrorMessagewithend(stepContext, startingCommand, "[Status:0008]", nameof(StartDialog), cancellationToken);

                    }
                    return await stepContext.BeginDialogAsync(nameof(RatingDialog), startingCommand, cancellationToken);
 
                case 0:
                   startingCommand=(StartingCommand)stepContext.Result;
                    if (startingCommand.end == "end")
                    {
                        return await stepContext.EndDialogAsync(startingCommand, cancellationToken);
                    }
                    return await stepContext.EndDialogAsync(startingCommand, cancellationToken);
            }
            return await stepContext.BeginDialogAsync(nameof(RatingDialog), startingCommand, cancellationToken);


        }
        private static async Task<DialogTurnResult> EndScheduleConfirm(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var startingCommand = (StartingCommand)stepContext.Result;
            if (startingCommand.end == "end")
            {
                return await stepContext.EndDialogAsync(startingCommand, cancellationToken);
            }
            return await stepContext.EndDialogAsync(startingCommand, cancellationToken);
        }

    }
}
