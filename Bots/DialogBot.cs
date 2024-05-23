// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.BotBuilderSamples.Dialogs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.BotBuilderSamples.Bots
{
    // This IBot implementation can run any type of Dialog. The use of type parameterization is to allows multiple different bots
    // to be run at different endpoints within the same project. This can be achieved by defining distinct Controller types
    // each with dependency on distinct IBot types, this way ASP Dependency Injection can glue everything together without ambiguity.
    // The ConversationState is used by the Dialog system. The UserState isn't, however, it might have been used in a Dialog implementation,
    // and the requirement is that all BotState objects are saved at the end of a turn.
    public class DialogBot<T> : ActivityHandler
        where T : Dialog
    {

        //private const string EnglishEnglish = "en";
        //private const string EnglishSpanish = "es";
        //private const string SpanishEnglish = "in";
        //private const string SpanishSpanish = "it";
        private  string local;

        private readonly IStatePropertyAccessor<string> _languagePreference;
        protected readonly Dialog Dialog;
        protected readonly BotState ConversationState;
        protected readonly BotState UserState;
        protected readonly ILogger Logger;
        private string lang = "";

        public DialogBot(ConversationState conversationState, UserState userState, T dialog, ILogger<DialogBot<T>> logger)
        {
            ConversationState = conversationState;
            UserState = userState;
            Dialog = dialog;
            Logger = logger;
            _languagePreference = userState.CreateProperty<string>("LanguagePreference");
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            await base.OnTurnAsync(turnContext, cancellationToken);

            // Save any state changes that might have occurred during the turn.
            await ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await UserState.SaveChangesAsync(turnContext, false, cancellationToken);
        }
        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                // Greet anyone that was not the target (recipient) of this message.
                // To learn more about Adaptive Cards, see https://aka.ms/msbot-adaptivecards for more details.
                if (member.Id != turnContext.Activity.Recipient.Id)
                {

                    // var welcomeCard = CreateAdaptiveCardAttachment();
                    // var response = MessageFactory.Attachment(welcomeCard, ssml: "Welcome to Bot Framework!");
                    // await turnContext.SendActivityAsync(response, cancellationToken);
                    //var currentLang = turnContext.Activity.Text.ToLower();
                    //var destinationDict = JsonConvert.DeserializeObject<dynamic>(currentLang);
                    //var local = destinationDict.locale;
                    //var local = turnContext.Activity.GetLocale();
                    //await turnContext.SendActivityAsync(local);
                    //if (local == "en-US" || local == "es-es" || local == "es")
                    //{
                    //    lang = local;
                    //}
                    //else
                    //{
                    // lang = "en";
                    //}
                    //if (local == "en-US" || local == "")
                    //{
                        lang = "en";
                    //}
                    //else
                    //{
                    //    lang = "es";
                    //}

                    // If the user requested a language change through the suggested actions with values "es" or "en",
                    // simply change the user's language preference in the user state.
                    // The translation middleware will catch this setting and translate both ways to the user's
                    // selected language.
                    // If Spanish was selected by the user, the reply below will actually be shown in spanish to the user.
                    
                    await _languagePreference.SetAsync(turnContext, lang, cancellationToken);
                    
                    Logger.LogInformation("Running dialog with Message Activity.");
                    await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>("DialogState"), cancellationToken);
                }
            }
        }
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {

            //if (IsLanguageChangeRequested(turnContext.Activity.Text))
            //{
            //    var currentLang = turnContext.Activity.Text.ToLower();
            //    var lang1 = currentLang == EnglishEnglish || currentLang == SpanishEnglish ? EnglishEnglish : EnglishSpanish;

            //    // If the user requested a language change through the suggested actions with values "es" or "en",
            //    // simply change the user's language preference in the user state.
            //    // The translation middleware will catch this setting and translate both ways to the user's
            //    // selected language.
            //    // If Spanish was selected by the user, the reply below will actually be shown in spanish to the user.
            //    await _languagePreference.SetAsync(turnContext, lang, cancellationToken);
            //    var reply = MessageFactory.Text($"Your current language code is: {lang}");
            //    await turnContext.SendActivityAsync(reply, cancellationToken);

            //    // Save the user profile updates into the user state.
            //    //await _userState.SaveChangesAsync(turnContext, false, cancellationToken);

            //}
            
            try
            {
                var local = turnContext.Activity.Locale;
                if (local == "en" || local == "es-es" || local == "es")
                {
                    lang = local;
                }
                else
                {
                    lang = "en";
                }
            }
            catch
            {
                lang = "en";
            }
               // _numberOfJobRetriesService.UpdateValue(Convert.ToString(locale));
               

                // If the user requested a language change through the suggested actions with values "es" or "en",
                // simply change the user's language preference in the user state.
                // The translation middleware will catch this setting and translate both ways to the user's
                // selected language.
                // If Spanish was selected by the user, the reply below will actually be shown in spanish to the user.
                await _languagePreference.SetAsync(turnContext, lang, cancellationToken);

         
               
            
                      
            //var local = turnContext.Activity.Locale;
            //await turnContext.SendActivityAsync(local);
            //if (local == "en-US" || local == "")
            //{
            //    lang = "en";
            //}
            //else
            //{
            //    lang = "es";
            //}

            Logger.LogInformation("Running dialog with Message Activity.");

            // Run the Dialog with the new message Activity.
            await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>("DialogState"), cancellationToken);
        }

        //private static bool IsLanguageChangeRequested(string utterance)
        //{
        //    if (string.IsNullOrEmpty(utterance))
        //    {
        //        return false;
        //    }

        //    utterance = utterance.ToLower().Trim();
        //    return utterance == EnglishSpanish || utterance == EnglishEnglish
        //        || utterance == SpanishSpanish || utterance == SpanishEnglish;
        //}

    }
}
