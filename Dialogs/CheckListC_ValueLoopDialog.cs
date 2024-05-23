using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class CheckListC_ValueLoopDialog : CancelAndHelpDialog
    {
        public CheckListC_ValueLoopDialog() : base(nameof(CheckListC_ValueLoopDialog))
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
            var serveyResponseBody = startingCommand.ServeyResponseBody;
            startingCommand.C_value = serveyResponseBody[startingCommand.c_value_index];
            startingCommand.c_value_index++;
            startingCommand.S_values = startingCommand.C_value.Sections;
            startingCommand.s_value_index = 0;
            return await stepContext.BeginDialogAsync(nameof(CheckListSectionsForLoopDialog), startingCommand, cancellationToken);        
        }

        private async Task<DialogTurnResult> SecondStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var startingCommand = (StartingCommand)stepContext.Options;
            if (startingCommand.c_value_index > startingCommand.ServeyResponseBody.Count - 1)
            {
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
            else
            {
                return await stepContext.ReplaceDialogAsync(InitialDialogId, startingCommand, cancellationToken);
            }
        }
       
    }
}