﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace Microsoft.BotBuilderSamples.Translation
{
    /// <summary>
    /// Middleware for translating text between the user and bot.
    /// Uses the Microsoft Translator Text API.
    /// </summary>
    public class TranslationMiddleware : IMiddleware
    {
        private readonly MicrosoftTranslator _translator;
        private readonly IStatePropertyAccessor<string> _languageStateProperty;
        protected readonly BotState ConversationState;
        protected readonly BotState UserState;

        /// <summary>
        /// Initializes a new instance of the <see cref="TranslationMiddleware"/> class.
        /// </summary>
        /// <param name="translator">Translator implementation to be used for text translation.</param>
        /// <param name="languageStateProperty">State property for current language.</param>
        public TranslationMiddleware(MicrosoftTranslator translator, UserState userState, ConversationState conversationState)
        {
            _translator = translator ?? throw new ArgumentNullException(nameof(translator));
            if(userState == null)
            {
                throw new ArgumentNullException(nameof(userState));
            }
            ConversationState = conversationState;
            UserState = userState;
            _languageStateProperty = userState.CreateProperty<string>("LanguagePreference");
        }

        /// <summary>
        /// Processes an incoming activity.
        /// </summary>
        /// <param name="turnContext">Context object containing information for a single turn of conversation with a user.</param>
        /// <param name="next">The delegate to call to continue the bot middleware pipeline.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            var translate = await ShouldTranslateAsync(turnContext, cancellationToken);
            await ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await UserState.SaveChangesAsync(turnContext, false, cancellationToken);
            if (translate)
            {
                if (turnContext.Activity.Type == ActivityTypes.Message)
                {
                    turnContext.Activity.Text = await _translator.TranslateAsync(turnContext.Activity.Text, TranslationSettings.DefaultLanguage, cancellationToken);
                }
            }
            turnContext.OnSendActivities(async (sendContext, activities, nextSend) =>
            {
                List<Task> translationTasks = new List<Task>();

                foreach (Activity currentActivity in activities)
                {
                    if (currentActivity.Type == ActivityTypes.Message && currentActivity.From.Id == turnContext.Activity.Recipient.Id)
                    {
                        // Translate messages sent by the bot to the user's language
                        string userLanguage = await _languageStateProperty.GetAsync(turnContext, () => TranslationSettings.DefaultLanguage) ?? TranslationSettings.DefaultLanguage;
                        bool shouldTranslate = userLanguage != TranslationSettings.DefaultLanguage;

                        if (shouldTranslate)
                        {
                            translationTasks.Add(TranslateMessageActivityAsync(currentActivity.AsMessageActivity(), userLanguage));
                        }
                    }
                }

                if (translationTasks.Any())
                {
                    await Task.WhenAll(translationTasks).ConfigureAwait(false);
                }

                return await nextSend();
            });
            //turnContext.OnSendActivities(async (turnContext, activities, next) =>
            //{
            //    string userLanguage = await _languageStateProperty.GetAsync(turnContext, () => TranslationSettings.DefaultLanguage) ?? TranslationSettings.DefaultLanguage;
            //    bool shouldTranslate = userLanguage != TranslationSettings.DefaultLanguage;

            //    // Translate messages sent to the user to user language
            //    if (shouldTranslate)
            //    {
            //        List<Task> tasks = new List<Task>();
            //        foreach (Activity currentActivity in activities.Where(a => a.Type == ActivityTypes.Message))
            //        {
            //            tasks.Add(TranslateMessageActivityAsync(currentActivity.AsMessageActivity(), userLanguage));
            //        }

            //        if (tasks.Any())
            //        {
            //            await Task.WhenAll(tasks).ConfigureAwait(false);
            //        }
            //    }

            //     return await next();
            //});

            turnContext.OnUpdateActivity(async (newContext, activity, nextUpdate) =>
            {
                string userLanguage = await _languageStateProperty.GetAsync(turnContext, () => TranslationSettings.DefaultLanguage) ?? TranslationSettings.DefaultLanguage;
                bool shouldTranslate = userLanguage != TranslationSettings.DefaultLanguage;

                // Translate messages sent to the user to user language
                if (activity.Type == ActivityTypes.Message)
                {
                    if (shouldTranslate)
                    {
                        await TranslateMessageActivityAsync(activity.AsMessageActivity(), userLanguage);
                    }
                }

                return await nextUpdate();
            });

            await next(cancellationToken).ConfigureAwait(false);
        }

        private async Task TranslateMessageActivityAsync(IMessageActivity activity, string targetLocale, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (activity.Type == ActivityTypes.Message)
            {
                activity.Text = await _translator.TranslateAsync(activity.Text, targetLocale);
            }
        }

        private async Task<bool> ShouldTranslateAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            string userLanguage = await _languageStateProperty.GetAsync(turnContext, () => TranslationSettings.DefaultLanguage, cancellationToken) ?? TranslationSettings.DefaultLanguage;
            return userLanguage != TranslationSettings.DefaultLanguage;
        }
    }
}
