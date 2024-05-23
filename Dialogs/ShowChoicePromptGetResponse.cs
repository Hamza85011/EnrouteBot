using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using System;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class ShowChoicePromptGetResponse : CancelAndHelpDialog
    {
        private readonly HttpClient client= new();
        private string value = "";
        private dynamic q_value;
        private List<int> response_ids;
        private List<string> responses;
        public ShowChoicePromptGetResponse() : base(nameof(ShowChoicePromptGetResponse))
        {

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                FirstStepAsync,
                SecondStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);

        }

        private async Task<DialogTurnResult> FirstStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var startingCommand = (StartingCommand)stepContext.Options;
            response_ids = new();
            responses = new();
            var q_values = startingCommand.Q_values;
            
            if (q_values.Count > 0)
            {
            q_value = q_values[startingCommand.q_value_index];
            startingCommand.q_value_index++;
            value = q_value.QuestionType;

            switch (value)
            {
                case "Single Select":

                    if (responses.Count == 0)
                    {
                        foreach (var responseValue in q_value.Responses)
                        {
                            response_ids.Add((int)responseValue.ResponseId);
                            responses.Add((string)responseValue.ResponseText);
                        }
                    }
                    var responses_choices = BotUtility.ChoiceList(responses);
                    var prompt = $"{q_value.Question}";
                    var promptMessage = MessageFactory.Text(prompt);
                    return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions { Prompt = promptMessage, Choices = responses_choices }, cancellationToken);

                case "Single Line":
                    return await BotUtility.SendTextPrompt(stepContext, $"{q_value.Question}", cancellationToken);

                case "Signature":
                    return await BotUtility.SendTextPrompt(stepContext, "Signature Required!", cancellationToken);

                case "Multi Line":
                    return await BotUtility.SendTextPrompt(stepContext, $"{q_value.Question}", cancellationToken);

                case "Rating":
                    return await BotUtility.SendTextPrompt(stepContext, $"{q_value.Question}", cancellationToken);

                default:
                    return await stepContext.ReplaceDialogAsync(InitialDialogId, startingCommand, cancellationToken);
            }
            }
            else
            {
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
        }
        private async Task<DialogTurnResult> SecondStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var startingCommand = (StartingCommand)stepContext.Options;           
            var url = $"{startingCommand.Base_url}/swi/Survey/SurveyResponse";
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", startingCommand.Access_token);
            string requestBody = "";

            switch (value)
            {
                case "Single Select":
                    startingCommand.Response = (int)(stepContext.Result as FoundChoice)?.Index;
                    requestBody = $"[{{ \"azimuth\":\"\", \"isAnswered\":1, \"isGPS\":1, \"iterationId\":0, \"mapImage\":\"\", \"mapZoom\":\"\", \"maxValue\":null, \"minValue\":null, \"pIterationId\":0 , \"questionId\":\"{q_value.SiteQuestionId}\", \"questionType\":\"{q_value.QuestionType}\", \"responseId\":\"{response_ids[startingCommand.Response]}\", \"responseText\":\"{responses[startingCommand.Response]}\", \"responseValue\":\"{responses[startingCommand.Response]}\", \"sectionId\":\"{q_value.SiteSectionId}\", \"signature\":null, \"siteId\":\"{q_value.SiteId}\", \"surveyId\":\"{startingCommand.SurveyId}\"}}]\r\n";
                    break;

                case "Signature":
                    startingCommand.Response = (string)stepContext.Result;
                    requestBody = $"[{{\"azimuth\":\"\", \"isAnswered\":1, \"isGPS\":1, \"iterationId\":0, \"mapImage\":\"\", \"mapZoom\":\"\", \"maxValue\":null, \"minValue\":null, \"pIterationId\":0 , \"questionId\":\"{q_value.SiteQuestionId}\", \"questionType\":\"{q_value.QuestionType}\", \"responseId\":0, \"responseText\":\"\", \"responseValue\":\"\", \"sectionId\":\"{q_value.SiteSectionId}\", \"signature\":\"{startingCommand.Response}\", \"siteId\":\"{startingCommand.C_value.SiteId}\", \"surveyId\":\"{startingCommand.SurveyId}\"}}]\r\n";
                    break;

                case "Single Line":
                    startingCommand.Response = (string)stepContext.Result;
                    requestBody = $"[{{\"answer\":\"{startingCommand.Response}\", \"azimuth\":\"\", \"isAnswered\":1, \"isGPS\":1, \"iterationId\":0, \"mapImage\":\"\", \"mapZoom\":\"\", \"maxValue\":null, \"minValue\":null, \"pIterationId\":0 , \"questionId\":\"{q_value.SiteQuestionId}\", \"questionType\":\"{q_value.QuestionType}\", \"responseId\":0, \"responseText\":\"{startingCommand.Response}\", \"responseValue\":\"{startingCommand.Response}\", \"sectionId\":\"{q_value.SiteSectionId}\", \"signature\":null, \"siteId\":\"{startingCommand.C_value.SiteId}\", \"surveyId\":\"{startingCommand.SurveyId}\"}}]\r\n";
                    break;

                case "Multi Line":
                    //startingCommand.Response = (string)(stepContext.Result as FoundChoice)?.Value;
                    startingCommand.Response = (string)stepContext.Result;
                    requestBody = $"[{{\"answer\":\"{startingCommand.Response}\", \"azimuth\":\"\", \"isAnswered\":1, \"isGPS\":1, \"iterationId\":0, \"mapImage\":\"\", \"mapZoom\":\"\", \"maxValue\":null, \"minValue\":null, \"pIterationId\":0 , \"questionId\":\"{q_value.SiteQuestionId}\", \"questionType\":\"{q_value.QuestionType}\", \"responseId\":0, \"responseText\":\"{startingCommand.Response}\", \"responseValue\":\"{startingCommand.Response}\", \"sectionId\":\"{q_value.SiteSectionId}\", \"signature\":null, \"siteId\":\"{startingCommand.C_value.SiteId}\", \"surveyId\":\"{startingCommand.SurveyId}\"}}]\r\n";
                    break;

                case "Rating":
                    startingCommand.Response = (string)stepContext.Result;
                    requestBody = $"[{{\"answer\":\"{startingCommand.Response}\", \"azimuth\":\"\", \"isAnswered\":1, \"isGPS\":1, \"iterationId\":0, \"mapImage\":\"\", \"mapZoom\":\"\", \"maxValue\":null, \"minValue\":null, \"pIterationId\":0 , \"questionId\":\"{q_value.SiteQuestionId}\", \"questionType\":\"{q_value.QuestionType}\", \"responseId\":0, \"responseText\":\"{startingCommand.Response}\", \"responseValue\":\"{startingCommand.Response}\", \"sectionId\":\"{q_value.SiteSectionId}\", \"signature\":null, \"siteId\":\"{startingCommand.C_value.SiteId}\", \"surveyId\":\"{startingCommand.SurveyId}\"}}]";
                    break;
            }

            HttpContent content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync(url, content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = JsonConvert.DeserializeObject<dynamic>(response.Content.ReadAsStringAsync(cancellationToken).Result);
                if ((string)responseBody.Status != "success")
                {
                    return await BotUtility.SendErrorMessage(stepContext, startingCommand, "[Status:0011]", nameof(StartDialog), cancellationToken);
                }
            }
            if (startingCommand.q_value_index >= startingCommand.Q_values.Count)
            {
                // End the dialog when all questions have been asked
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
            else
            {
                return await stepContext.ReplaceDialogAsync(InitialDialogId, startingCommand, cancellationToken);
            }
            
        }

    }
}