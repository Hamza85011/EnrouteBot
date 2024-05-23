using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;


namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        // Dependency injection uses this constructor to instantiate MainDialog
        public MainDialog(StartDialog startDialog)
            : base(nameof(MainDialog))
        {
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(startDialog);
            AddDialog(new EnrouteDialog());
            AddDialog(new Reschedule_AppointmentDialog());
            AddDialog(new ScheduleConfirmDialog());
            AddDialog(new ConfirmScheduleFuncDialog());
            AddDialog(new Cancel_AppointmentDialog());
            AddDialog(new Recognize_Intent_EnrouteDialog());          
            AddDialog(new ChangeActionApprovalDialog());
            AddDialog(new CheckListDialog());
            AddDialog(new RatingDialog());
            AddDialog(new ShowChoicePromptGetResponse());
            AddDialog(new CheckListSectionsForLoopDialog());
            AddDialog(new CheckListC_ValueLoopDialog());
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                ActStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }
        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {    
                return await stepContext.ReplaceDialogAsync(nameof(StartDialog), new StartingCommand(), cancellationToken);
            
        }
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Result is string result)
            {
                return await BotUtility.SendMessageAndEndDialog(stepContext, result, cancellationToken);
                //return await BotUtility.SendMessage(stepContext, startingCommand, result, nameof(StartDialog), cancellationToken);
            }

            // Restart the main dialog with a different message the second time around
            return await stepContext.EndDialogAsync(null, cancellationToken);
            //return await BotUtility.SendMessage(stepContext, startingCommand, "What else can I do for you?", InitialDialogId, cancellationToken);
        }
       
    }
}
