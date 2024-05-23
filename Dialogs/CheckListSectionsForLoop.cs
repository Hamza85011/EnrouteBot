using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;


namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class CheckListSectionsForLoopDialog : CancelAndHelpDialog
    {
        private readonly HttpClient client = new();
        private dynamic s_value;

        public CheckListSectionsForLoopDialog() : base(nameof(CheckListSectionsForLoopDialog))
        {

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                FirstStepAsync,
                SecondStepAsync
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);

        }

        private async Task<DialogTurnResult> FirstStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var startingCommand = (StartingCommand)stepContext.Options;
            var s_values = startingCommand.S_values;
            s_value = s_values[startingCommand.s_value_index];
            startingCommand.s_value_index++;
            startingCommand.Q_values = s_value.QuestionList;
            startingCommand.q_value_index = 0;
            return await stepContext.BeginDialogAsync(nameof(ShowChoicePromptGetResponse), startingCommand, cancellationToken);
        }

        private async Task<DialogTurnResult> SecondStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var startingCommand = (StartingCommand)stepContext.Options;
            if (startingCommand.s_value_index > startingCommand.S_values.Count - 1)
            {
                // End the dialog when all questions have been asked
                //await stepContext.Context.SendActivityAsync(startingCommand, cancellationToken);
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
            else
            {
                return await stepContext.ReplaceDialogAsync(InitialDialogId, startingCommand, cancellationToken);
            }
        }
        
    }
}