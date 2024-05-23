using Microsoft.Bot.Builder.Dialogs;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Text;
using Microsoft.Bot.Builder.Dialogs.Choices;
using System.Xml.Linq;
using System;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class Recognize_Intent_EnrouteDialog : CancelAndHelpDialog
    {
        private readonly HttpClient client = new();
        private string topIntent;
        private double confidenceScore;
        public Recognize_Intent_EnrouteDialog() : base(nameof(Recognize_Intent_EnrouteDialog))
        {
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
                {
                StartStepAsync,
                GetUserChoiceStepAsync,
                RepeatCurrentDialogStepAsync
                }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);

        }
        private async Task<DialogTurnResult> StartStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
           
            StartingCommand startingCommand = (StartingCommand)stepContext.Options;
            //startingCommand.Choice_prompt = BotUtility.ChoiceList(new List<string> { "I confirm", "No" });
            //startingCommand.Spanish_Choice_prompt = new List<Choice>{
            //      new Choice {Value = "Confirmo", Synonyms=new List<string>{ "Confirm" } },
            //      new Choice {Value = "No", Synonyms=new List<string>{ "No" }}

            //};
            if (startingCommand.lan == "en-US" || startingCommand.lan == "")
            {
                startingCommand.Choice_prompt = BotUtility.ChoiceList(new List<string> { "I, confirm", "No" });
            }
            else
            {
                startingCommand.Choice_prompt= new List<Choice>{
                  new Choice {Value = "Confirmo", Synonyms=new List<string>{ "Confirm" } },
                  new Choice {Value = "No", Synonyms=new List<string>{ "No" }}

                };
            }

            startingCommand.Comment = startingCommand.Input;

            var url = "https://aircod-bot.cognitiveservices.azure.com/language/:analyze-conversations?api-version=2022-10-01-preview";
            var requestBody = $"{{\"kind\":\"Conversation\",\"analysisInput\":{{\"conversationItem\":{{\"id\":\"1\",\"text\":\"{startingCommand.Input}\",\"modality\":\"text\",\"language\":\"en\",\"participantId\":\"1\"}}}},\"parameters\":{{\"projectName\":\"aircod-fieldforce\",\"verbose\":true,\"deploymentName\":\"spanish001\",\"stringIndexType\":\"TextElement_V8\"}}}}";
            HttpContent content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "3b07c86283464803834dcaf1e65e1fd6");
            client.DefaultRequestHeaders.Add("Apim-Request-Id", "4ffcac1c-b2fc-48ba-bd6d-b69d9942995a");
            HttpResponseMessage response = await client.PostAsync(url, content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = response.Content.ReadAsStringAsync(cancellationToken).Result;
                var responseObject = JsonConvert.DeserializeObject<dynamic>(responseBody);
                confidenceScore = responseObject["result"]["prediction"]["intents"][0]["confidenceScore"];
                topIntent = responseObject["result"]["prediction"]["topIntent"];
                if (confidenceScore > 0.80)
                {
                    switch (topIntent)
                    {
                        case "reschedule an appointment":
                            startingCommand.OptionSelected = "Reschedule";

                            return await BotUtility.SendChoicePrompt(stepContext, $"Please confirm if you want to reschedule the visit for \"{startingCommand.Ticket_dict.Title}\"?", startingCommand.Choice_prompt ,cancellationToken);
                           
                        case "cancel an appointment":
                            startingCommand.OptionSelected = "Cancellation";
                            return await BotUtility.SendChoicePrompt(stepContext, $"Please confirm if you want to cancel the visit for \"{startingCommand.Ticket_dict["Title"]}\"?", startingCommand.Choice_prompt, cancellationToken);

                        case "None":
                            return await BotUtility.SendTextPrompt(stepContext, "Sorry, I cannot help you in this.\r\nLet me know if there is anything else I can do for you?", cancellationToken);

                        default:
                            return await stepContext.ReplaceDialogAsync(InitialDialogId, startingCommand, cancellationToken);
                        }
                }

                else
                {
                    if (startingCommand.Parent == "start")
                    {
                        return await stepContext.EndDialogAsync(null, cancellationToken);
                    }
                    else
                    {
                        return await BotUtility.SendTextPrompt(stepContext, "Sorry I didn't get it.\r\nWould you please ask again?", cancellationToken);
                    }
                }
            }
            else
            {
                if (startingCommand.Parent == "start")
                {
                    return await stepContext.EndDialogAsync(null, cancellationToken);
                }
                else
                {
                    return await BotUtility.SendTextPrompt(stepContext, "Sorry I didn't get it.\r\nWould you please ask again?", cancellationToken);
                }
            }
        }

        private async Task<DialogTurnResult> GetUserChoiceStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            StartingCommand startingCommand = (StartingCommand)stepContext.Options;
            if (confidenceScore > 0.80)
            {
                switch (topIntent)
                {
                    case "reschedule an appointment":
                        startingCommand.Confirm_intent = (int)(stepContext.Result as FoundChoice)?.Index;
                        if (startingCommand.Confirm_intent == 0)
                        {
                            return await stepContext.BeginDialogAsync(nameof(Reschedule_AppointmentDialog), startingCommand, cancellationToken);
                        }
                        else
                        {
                            return await BotUtility.SendTextPrompt(stepContext, "How may I help you?", cancellationToken);
                        }

                    case "cancel an appointment":
                        startingCommand.Confirm_intent = (int)(stepContext.Result as FoundChoice)?.Index;
                        if (startingCommand.Confirm_intent == 0)
                        {
                            return await stepContext.BeginDialogAsync(nameof(Cancel_AppointmentDialog), startingCommand, cancellationToken);
                        }
                        else
                        {
                            return await BotUtility.SendTextPrompt(stepContext, "How may I help you?", cancellationToken);
                        }
                    case "None":
                        startingCommand.Input = (string)stepContext.Result; 
                        return await stepContext.ReplaceDialogAsync(InitialDialogId, startingCommand, cancellationToken);


                    default:
                        return await stepContext.ReplaceDialogAsync(InitialDialogId, startingCommand, cancellationToken);
                }
            }
            else
            {
                startingCommand.Input = (string)stepContext.Result;
                return await stepContext.ReplaceDialogAsync(InitialDialogId, startingCommand, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> RepeatCurrentDialogStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            var startingCommand = (StartingCommand)stepContext.Options;
            if (startingCommand.Confirm_intent == 0)
            {
                startingCommand = (StartingCommand)stepContext.Result;
                return await stepContext.EndDialogAsync(startingCommand, cancellationToken);
            }

            startingCommand.Input = (string)stepContext.Result;
            return await stepContext.ReplaceDialogAsync(InitialDialogId, startingCommand, cancellationToken);
            
        }


    }
}
