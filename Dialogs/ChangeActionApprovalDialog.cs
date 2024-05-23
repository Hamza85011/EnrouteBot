using Microsoft.Bot.Builder.Dialogs;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;
using System.Text;
using Microsoft.Bot.Builder.Dialogs.Choices;


namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class ChangeActionApprovalDialog : CancelAndHelpDialog
    {
        private readonly List<string> approval = new();
        private readonly List<int> status_ids = new();
        private static readonly HttpClient client = new();
        public ChangeActionApprovalDialog() : base(nameof(ChangeActionApprovalDialog))
        {
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
                {
                    StartStepAsync,
                    GetreponseAsync,
                    Callurl,
                    Replaceone,

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
                var requestTypeId = responseBody.TicketId;

                if (requestTypeId > 0)
                {
                    startingCommand.User_id = responseBody.CreatedBy;
                    startingCommand.Ticket_dict = responseBody;
                    startingCommand.Ticket_title = responseBody.Title;
                    startingCommand.Ticket_ref = responseBody.RefNo;
                    startingCommand.Ticket_status = responseBody.Approval;
                    startingCommand.Ticket_status_id = responseBody.ApprovalId;
                    startingCommand.Ticket_workflowid = responseBody.WorkflowId;
                    if (startingCommand.Ticket_dict.ApprovalId > 0)
                    {
                        startingCommand.Ticket_status = "Pending";
                    }
                    var url1 = String.Format("{0}/avx2/configuration/workflow/CheckUserAvailabilityForApproval?WorkflowId={1}&moduleId={2}&featureId={3}&instanceId={4}&userId={5}&mappingCategory=1", startingCommand.Base_url, startingCommand.Ticket_workflowid, startingCommand.Module_id, startingCommand.Feature_id, startingCommand.Instance_id, startingCommand.User_id);
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", startingCommand.Access_token);
                    HttpResponseMessage response1 = await client.GetAsync(url1, cancellationToken);
                    if (response1.IsSuccessStatusCode)
                    {
                        var responseBody1 = JsonConvert.DeserializeObject<dynamic>(response1.Content.ReadAsStringAsync(cancellationToken).Result);
                        if (startingCommand.Ticket_workflowid < 0 || responseBody1.ApprovalData.IsAvailable == "true")
                        {
                            startingCommand.User_is_available = responseBody1.ApprovalData.IsAvailable;
                            if (startingCommand.Ticket_status == "Pending")
                            {
                                var url2 = String.Format("{0}/avx2/definitions/getdefinitions?DefinitionKeyCode=TICKET_APPROVAL&UserId={1}", startingCommand.Base_url, startingCommand.User_id);
                                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", startingCommand.Access_token);
                                HttpResponseMessage response2 = await client.GetAsync(url2, cancellationToken);

                                if (response2.IsSuccessStatusCode)
                                {
                                    var responseBody2 = JsonConvert.DeserializeObject<dynamic>(response2.Content.ReadAsStringAsync(cancellationToken).Result);

                                    foreach (var content in responseBody2)
                                    {
                                        if (startingCommand.Ticket_status != content.Name)
                                        {
                                            // not use the defination use statuses approval or status id but not found check form sir
                                            approval.Add(content.Name);
                                            status_ids.Add(content.DefinationId);

                                        }
                                    }

                                    var message3 = String.Format("Approval required for \"{0}\"[{1}]", startingCommand.Ticket_dict.Title, startingCommand.Ticket_dict.RefNo);
                                    var approval_statuses = BotUtility.ChoiceList(approval);

                                    return await BotUtility.SendChoicePrompt(stepContext, message3, approval_statuses, cancellationToken);



                                }
                                else
                                {
                                    return await BotUtility.SendErrorMessage(stepContext, startingCommand, "[Status:0004]", nameof(StartDialog), cancellationToken);
                                }
                            }
                            else
                            {
                                return await BotUtility.SendMessage(stepContext, startingCommand, $"ticket is already {startingCommand.Ticket_status}", nameof(StartDialog), cancellationToken);
                            }
                        }
                        else
                        {
                            return await BotUtility.SendErrorMessage(stepContext, startingCommand, "[Status:0012]", nameof(StartDialog), cancellationToken);
                        }



                    }
                    else
                    {
                        return await BotUtility.SendErrorMessage(stepContext, startingCommand, "[Status:0012]", nameof(StartDialog), cancellationToken);
                    }


                }
                else
                {
                    return await BotUtility.SendErrorMessage(stepContext, startingCommand, "[Status:0002]", nameof(StartDialog), cancellationToken);
                }

            }
            else
            {
                return await BotUtility.SendErrorMessage(stepContext, startingCommand, "[Status:0002]", nameof(StartDialog), cancellationToken);
            }
        }


        private async Task<DialogTurnResult> GetreponseAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            StartingCommand startingCommand = (StartingCommand)stepContext.Options;
            var index = (int)(stepContext.Result as FoundChoice)?.Index;
            startingCommand.Selected_status_id = status_ids[index];
            startingCommand.Approval_status = approval[index];
            var message4 = "Any comments?";
            return await BotUtility.SendTextPrompt(stepContext, message4, cancellationToken);

        }

        private async Task<DialogTurnResult> Callurl(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            StartingCommand startingCommand = (StartingCommand)stepContext.Options;
            startingCommand.Comment = (string)stepContext.Result;
            DateTimeOffset start_date = DateTimeOffset.UtcNow.DateTime;
            var url3 = String.Format("{0}/api/Genaric/UpdateApprovalStatus", startingCommand.Base_url);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", startingCommand.Access_token);
            string requestBody = String.Format("{{ \"InstanceId\":{0},  \"OldStatusId\":{1},    \"NewStatusId\":{2},    \"OldStatus\":\"{3}\",    \"NewStatus\":\"{4}\",    \"UpdatedBy\":{5},    \"UpdatedOn\":\"{6}\",    \"Comments\":\"{7}\",    \"FeatureId\":{8},    \"ModuleId\": {9}}}", startingCommand.Instance_id, startingCommand.Ticket_status_id, startingCommand.Selected_status_id, startingCommand.Ticket_status, startingCommand.Approval_status, startingCommand.User_id, start_date, startingCommand.Comment, startingCommand.Feature_id, startingCommand.Module_id);
            HttpContent content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            HttpResponseMessage response3 = await client.PostAsync(url3, content, cancellationToken);
            if (response3.IsSuccessStatusCode)
            {
                var responseBody3 = JsonConvert.DeserializeObject<dynamic>(response3.Content.ReadAsStringAsync(cancellationToken).Result);
                var newstatus_id = responseBody3.NewStatusId;
                if (newstatus_id > 0)
                {
                    var message5 = String.Format("\"{0}\" [{1}] is {2}", startingCommand.Ticket_title, startingCommand.Ticket_ref, responseBody3.NewStatus);
                    return await BotUtility.SendMessage(stepContext, startingCommand, message5, nameof(RatingDialog), cancellationToken);
                }
                else
                {
                    return await BotUtility.SendErrorMessage(stepContext, startingCommand, "[Status:0013]", nameof(StartDialog), cancellationToken);
                }

            }
            else
            {
                return await BotUtility.SendErrorMessage(stepContext, startingCommand, "[Status:0013]", nameof(StartDialog), cancellationToken);
            }



        }
        private async Task<DialogTurnResult> Replaceone(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            StartingCommand startingCommand = (StartingCommand)stepContext.Options;
            return await stepContext.ReplaceDialogAsync(nameof(StartDialog), startingCommand, cancellationToken);

        }
       

    }
}
