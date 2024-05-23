using Microsoft.Bot.Builder.Dialogs;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;
using System.Text;


namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class RatingDialog : CancelAndHelpDialog
    {
        private static readonly HttpClient client = new();
        public RatingDialog() : base(nameof(RatingDialog))
        {
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
                {
                ShowRatingCardStep,
                //GetRatingFromUserStep
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);

        }
        private async Task<DialogTurnResult> ShowRatingCardStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var startingCommand = (StartingCommand)stepContext.Options;
            startingCommand.PrintStatement = "Have a nice day.";
            return await stepContext.EndDialogAsync(startingCommand, cancellationToken);
            //return await BotUtility.SendRatingPrompt(stepContext, startingCommand.lan ,cancellationToken);
        }

        //private async Task<DialogTurnResult> GetRatingFromUserStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        //{
        //    var startingCommand = (StartingCommand)stepContext.Options;
        //    startingCommand.Rating_text = (string)stepContext.Result;

        //    switch(startingCommand.Rating_text.ToLower())
        //    {
        //        case "excellent":
        //            startingCommand.Rating = 5;
        //            break;

        //        case "good":
        //            startingCommand.Rating = 4;
        //            break;
                    
        //        case "well":
        //            startingCommand.Rating = 4;
        //            break;

        //        case "neutral":
        //            startingCommand.Rating = 3;
        //            break;

        //        case "bad":
        //            startingCommand.Rating = 2;
        //            break;
        //        case "a little":
        //            startingCommand.Rating = 2;
        //            break;

        //        case "very bad":
        //            startingCommand.Rating = 1;
        //            break;

        //        default:
        //            return await stepContext.ReplaceDialogAsync(InitialDialogId, startingCommand, cancellationToken);
        //    }

        //    string url = $"{startingCommand.Base_url}/avx2/ratings/save";
        //    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", startingCommand.Access_token);
        //    string requestBody = $"{{\"RatingId\":0, \"ModuleId\":{startingCommand.Module_id},\"FeatureId\":{startingCommand.Feature_id},\"InstanceId\":{startingCommand.Instance_id},\"Rating\":{startingCommand.Rating},\"Comments\":\"\",\"CreatedBy\":{startingCommand.User_id}";
        //    HttpContent content = new StringContent(requestBody, Encoding.UTF8, "application/json");
        //    HttpResponseMessage response = await client.PostAsync(url, content, cancellationToken);
        //    if(response.IsSuccessStatusCode)
        //    {
        //        var responseBody= JsonConvert.DeserializeObject<dynamic>(response.Content.ReadAsStringAsync(cancellationToken).Result);
        //        if(responseBody.ratingId > 0)
        //        {
        //            startingCommand.PrintStatement = "Thanks for your feedback!";

        //            return await stepContext.EndDialogAsync(startingCommand, cancellationToken);
        //        }
        //        else
        //        {
        //            return await stepContext.EndDialogAsync(startingCommand, cancellationToken);                }
        //        }
        //    else
        //    {
        //        return await BotUtility.SendErrorMessagewithend(stepContext, startingCommand, "[Status:0009]", nameof(StartDialog), cancellationToken);

        //    }
        //}
    }
}
