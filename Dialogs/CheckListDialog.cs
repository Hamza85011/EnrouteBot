using Microsoft.Bot.Builder.Dialogs;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;


namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class CheckListDialog : CancelAndHelpDialog
    {
        private static readonly HttpClient client = new();
        public CheckListDialog() : base(nameof(CheckListDialog))
        {
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
                {
                StartStepAsync,
                FinalStepAsync
                }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);

        }
        private async Task<DialogTurnResult> StartStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var startingCommand = (StartingCommand)stepContext.Options;
            var response = await BotUtility.SendTicketRequest(startingCommand, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var responseBody = JsonConvert.DeserializeObject<dynamic>(response.Content.ReadAsStringAsync(cancellationToken).Result);
                if (responseBody.TicketId > 0)
                {
                    startingCommand.User_id = responseBody.CreatedBy;
                    startingCommand.Ticket_dict = responseBody;
                    var serveyUrl = $"{startingCommand.Base_url}/swi/Survey/Document?Ids={startingCommand.SurveyId}&UserId={startingCommand.User_id}";
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", startingCommand.Access_token);
                    HttpResponseMessage serveyResponse = await client.GetAsync(serveyUrl, cancellationToken);

                    if (serveyResponse.IsSuccessStatusCode)
                    {
                        var serveyResponseBody = JsonConvert.DeserializeObject<dynamic>(serveyResponse.Content.ReadAsStringAsync(cancellationToken).Result);
                        await BotUtility.SendMessageWithoutReplace(stepContext, $"{startingCommand.PhoneNumber};We appreciate your feedback for \"{startingCommand.Ticket_dict.Title}\" [{startingCommand.Ticket_dict.RefNo}]", cancellationToken);

                        startingCommand.ServeyResponseBody = serveyResponseBody;
                        startingCommand.c_value_index = 0;
                        return await stepContext.BeginDialogAsync(nameof(CheckListC_ValueLoopDialog), startingCommand, cancellationToken);
                        
                    }
                    else
                    {
                        return await BotUtility.SendErrorMessage(stepContext, startingCommand, "[Status:0010]", nameof(StartDialog), cancellationToken);
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

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var startingCommand = (StartingCommand)stepContext.Options;
            startingCommand.PrintStatement = "Thank you for your precious time";
            return await stepContext.EndDialogAsync(startingCommand, cancellationToken);
        }


     }
}

