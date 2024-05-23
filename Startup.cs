
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.BotBuilderSamples.Bots;
using Microsoft.BotBuilderSamples.Dialogs;
using Microsoft.BotBuilderSamples.Translation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.BotBuilderSamples
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpClient().AddControllers().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.MaxDepth = HttpHelper.BotMessageSerializerSettings.MaxDepth;
            });

            // Create the Bot Framework Authentication to be used with the Bot Adapter.
            services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();

            // Create the Bot Adapter with error handling enabled.
            services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();

            // Create the storage we'll be using for User and Conversation state. (Memory is great for testing purposes.)
            services.AddSingleton<IStorage, MemoryStorage>();

            // Create the User state. (Used in this bot's Dialog implementation.)
            services.AddSingleton<UserState>();

            // Create the Conversation state. (Used by the Dialog system itself.)
            services.AddSingleton<ConversationState>();
            // Create the Microsoft Translator responsible for making calls to the Cognitive Services translation service

            services.AddSingleton<MicrosoftTranslator>();

            // Create the Translation Middleware that will be added to the middleware pipeline in the AdapterWithErrorHandler
            services.AddSingleton<TranslationMiddleware>();

            // Register LUIS recognizer
            //services.AddSingleton<FlightBookingRecognizer>();

            // Register the CheckDialog
            //services.AddSingleton<CheckDialog>();

            // Register the BookingDialog.
            //services.AddSingleton<BookingDialog>();

            // Register the StartDialog.
            services.AddSingleton<StartDialog>();

            // Register the EnrouteDialog.
            services.AddSingleton<EnrouteDialog>();

            // Register the ChangeActionApprovalDialog.
            services.AddSingleton<ChangeActionApprovalDialog>();

            // Register the ScheduleConfirmDialog.
            services.AddSingleton<ScheduleConfirmDialog>();

            // Register the CheckListDialog.
            services.AddSingleton<CheckListDialog>();

            // Register the Intent Recognition Dialog.
            //services.AddSingleton<IntentRecognitionDialog>();

            // Register the Rating Recognition Dialog.
            services.AddSingleton<RatingDialog>();

            // Register the Reschedule Appointment Dialog.
            services.AddSingleton<Reschedule_AppointmentDialog>();

            // Register the Cancel Appointment Dialog.
            services.AddSingleton<Cancel_AppointmentDialog>();

            // Register the Cancel Appointment Dialog.
            services.AddSingleton<Recognize_Intent_EnrouteDialog>();

            // Register the Cancel Appointment Dialog.
            services.AddSingleton<ScheduleConfirmDialog>();

            // Register the Cancel Appointment Dialog.
            services.AddSingleton<ConfirmScheduleFuncDialog>();

            // Register the Cancel Appointment Dialog.
            services.AddSingleton<ShowChoicePromptGetResponse>();

            // Register the Cancel Appointment Dialog.
            services.AddSingleton<CheckListSectionsForLoopDialog>();

            // Register the Cancel Appointment Dialog.
            services.AddSingleton<CheckListC_ValueLoopDialog>();

            // The MainDialog that will be run by the bot.
            services.AddSingleton<MainDialog>();
            // Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
            //services.AddTransient<IBot, MultiLingualBot>();

            //Create the bot as a transient.In this case the ASP Controller is expecting an IBot.
            services.AddTransient<IBot, DialogBot<MainDialog>>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDefaultFiles()
                .UseStaticFiles()
                .UseRouting()
                .UseAuthorization()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });

            // app.UseHttpsRedirection();
        }
    }
}
