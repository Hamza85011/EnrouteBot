using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;
using System.IO;
using System;
using System.Text;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class StartDialog : CancelAndHelpDialog
    {
            private bool flag = false;
            private const string StartStepMsgText = "";
            private static readonly HttpClient client = new();

            public StartDialog(): base(nameof(StartDialog))
            {
                AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
                {
                StartStepAsync,
                ChildDialogStepAsync,
                FinalStepAsync,
                }));

                // The initial child Dialog to run.
                InitialDialogId = nameof(WaterfallDialog);
            }
            
            private async Task<DialogTurnResult> StartStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
            {
                var startingCommand = (StartingCommand)stepContext.Options;

                // Get Token API Details From AppSettings.json
                var base_url_path = Directory.GetCurrentDirectory() + "\\appsettings.json";
                string base_url_json = File.ReadAllText(base_url_path);
                var obj = JsonConvert.DeserializeObject<dynamic>(base_url_json);
                startingCommand.Base_url = obj.base_url;
                startingCommand.Name = obj.name;
                startingCommand.Password = obj.password;
                startingCommand.Gmap_api_key = obj.gmap_api_key;
                startingCommand.Encrypted_key = obj.encrypted_key;
                startingCommand.end = "";
                startingCommand.counter = 0;
            startingCommand.lan = (string)stepContext.Context.Activity.Locale;
            if (stepContext.Context.Activity.Locale == "en-US" || stepContext.Context.Activity.Locale == "es-es" || stepContext.Context.Activity.Locale == "es")
            {
                startingCommand.lan = stepContext.Context.Activity.Locale;
            }
            else
            {
                startingCommand.lan = "en-US";
            }

            var url = String.Format("{0}/token", startingCommand.Base_url);
            string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(startingCommand.Name + ":" + startingCommand.Password));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            HttpResponseMessage response = await client.GetAsync(url, cancellationToken);
            //var encryptedPassword = BotUtility.Encrypt(startingCommand.Password, startingCommand.Encrypted_key);
            //var url = $"https://trial.airviewx.com:49232/swi/device/login?username={startingCommand.Name}&password={encryptedPassword}";
            //HttpResponseMessage response = await client.PostAsync(url, null, cancellationToken);


            //Check Response Code and Prompt User For Input
            if (response.IsSuccessStatusCode)
            {
                    var responseString = JsonConvert.DeserializeObject<dynamic>(response.Content.ReadAsStringAsync(cancellationToken).Result);
                    startingCommand.Access_token = responseString.access_token;
                    return await BotUtility.SendTextPrompt(stepContext, StartStepMsgText, cancellationToken);
            }
            else
            {
                return await BotUtility.SendErrorMessage(stepContext, startingCommand, "[Status:0001]", InitialDialogId, cancellationToken);
            }
        }
        private async Task<DialogTurnResult> ChildDialogStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get Response From Previous Step of Waterfall Method
            var startingCommand = (StartingCommand)stepContext.Options;
            startingCommand.Input = (string)stepContext.Result;
            startingCommand.Input = (string)stepContext.Context.Activity.Text;
            try
                {
                    var destinationDict = JsonConvert.DeserializeObject<dynamic>(startingCommand.Input);
                    startingCommand.Command = destinationDict.command;
                    startingCommand.Module_id = destinationDict.module_id;
                    startingCommand.Feature_id = destinationDict.feature_id;
                    startingCommand.Instance_id = destinationDict.instance_id;
                    startingCommand.User_id = destinationDict.user_id;
                    startingCommand.Tenant_id= destinationDict.tenant_id;
                    //var language = destinationDict.locale;
                    //if (language == "en-US" || language == "es-es" || language == "es")
                    //{
                    //    startingCommand.lan = language;
                    //}
                    //else
                    //{
                    //    startingCommand.lan = "en-US";
                    //}
                    var response = await BotUtility.GetPhoneByUserId(startingCommand, cancellationToken);
                    if (response.IsSuccessStatusCode)
                    {
                    var phoneInfo = JsonConvert.DeserializeObject<dynamic>(response.Content.ReadAsStringAsync(cancellationToken).Result);
                    startingCommand.PhoneNumber = phoneInfo[0].WhatsappNumber;
                    }
                    else
                    {
                    return await BotUtility.SendErrorMessage(stepContext, startingCommand, "[Status:0001]", InitialDialogId, cancellationToken);
                    }
                   
                    switch (startingCommand.Command.ToLower())
                        {
                        case "enroute":
                            startingCommand.Arrival_time = destinationDict.arrival_time;
                            return await stepContext.BeginDialogAsync(nameof(EnrouteDialog), startingCommand, cancellationToken);

                        case "approval":
                            return await stepContext.ReplaceDialogAsync(nameof(ChangeActionApprovalDialog), startingCommand, cancellationToken);

                        case "scheduleconfirm":
                            return await stepContext.BeginDialogAsync(nameof(ScheduleConfirmDialog), startingCommand, cancellationToken);

                        case "checklist":
                            startingCommand.SurveyId = destinationDict.survey_id;
                            return await stepContext.BeginDialogAsync(nameof(CheckListDialog), startingCommand, cancellationToken);

                        case "intent":
                            startingCommand.Input = destinationDict.msg;
                            startingCommand.Parent = "start";
                            return await stepContext.ReplaceDialogAsync(nameof(Recognize_Intent_EnrouteDialog), startingCommand, cancellationToken);

                        case "signature":
                            flag = true;
                            string messageText = "Signature Required!";
                            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
                            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);

                        case "rating":
                            return await stepContext.BeginDialogAsync(nameof(RatingDialog), startingCommand, cancellationToken);

                        default:
                            flag = true;
                            startingCommand.Parent = "start";
                            return await stepContext.BeginDialogAsync(nameof(Recognize_Intent_EnrouteDialog), startingCommand, cancellationToken);
                    };
                }
                catch
                {
                    return await BotUtility.SendMessageAndEndDialog(stepContext, "We are having a problem \r\n Our team will get back to you.", cancellationToken);
                }
        }


        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (flag == false)
            {
                var startingCommand = (StartingCommand)stepContext.Result;
                if(startingCommand.PrintStatement=="")
                {
                    return await stepContext.EndDialogAsync(null, cancellationToken);
                }
                await stepContext.Context.SendActivityAsync(startingCommand.PrintStatement, cancellationToken: cancellationToken);
                startingCommand.PrintStatement = "";
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
            else
            {
                flag = false;
                if (stepContext.Result != null)
                {
                    var message = (string)stepContext.Result;
                    await stepContext.Context.SendActivityAsync(message, cancellationToken: cancellationToken);
                    return await stepContext.EndDialogAsync(null, cancellationToken);
                }
                else
                {
                    return await stepContext.EndDialogAsync(null, cancellationToken);
                }
            }
        }

    }
}    

