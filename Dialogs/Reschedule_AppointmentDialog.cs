using Microsoft.Bot.Builder.Dialogs;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;
using Microsoft.Bot.Builder.Dialogs.Choices;
using System.Linq;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using System.Globalization;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class Reschedule_AppointmentDialog : CancelAndHelpDialog
    {
        private static readonly HttpClient client = new();
        private dynamic availableSlotsList;
        private dynamic slot_dict;
        private List<string> available_dates;
        private  List<string> available_times;
        private  List<int> availableSlotsListIndex;
        private  List<string> end_times;
        private readonly string no_slot;

        public Reschedule_AppointmentDialog() : base(nameof(Reschedule_AppointmentDialog))
        {
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
                {
                StartStepAsync,
                GetUserReasonStepAsync,
                GetUserCommentStepAsync,
                GetUserSlotConfirmationStepAsync,
                GetDateSlotFromUserStepAsync,
                GetTimeSlotFromUserStepAsync,
                ShowUserChoicesStepAsync,
                GetUserChoiceStepAsync,
                //GetUserInputStepAsync
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);

        }
        private async Task<DialogTurnResult> StartStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            StartingCommand startingCommand = (StartingCommand)stepContext.Options;
            available_dates = new();
            available_times = new();
            availableSlotsListIndex= new();
            end_times= new();

            var response = await BotUtility.GetTimeZone(startingCommand, startingCommand.lan, cancellationToken);

            //Check Response Code and Prompt User For Input
            if (response.IsSuccessStatusCode)
            {
                // Get Time Zone Name For Response
                string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                var responseObject = JsonConvert.DeserializeObject<dynamic>(responseBody);
                startingCommand.TimeZoneName = responseObject.timeZoneName;
                // Send Second Get Request
                var secondResponse = await BotUtility.SendScheduleReasonRequest(startingCommand, cancellationToken);
                if (secondResponse.IsSuccessStatusCode)
                {
                    var secondResponseBody = secondResponse.Content.ReadAsStringAsync(cancellationToken).Result;
                    var secondResponseObject = JsonConvert.DeserializeObject<dynamic>(secondResponseBody);

                    startingCommand.Reasons = new();
                    startingCommand.Reason_ids = new();
                    if (startingCommand.Reasons.Count == 0)
                    {
                        foreach (var dictionary in secondResponseObject)
                        {
                            startingCommand.Reason_ids.Add((int)dictionary.DefinationId);
                            startingCommand.Reasons.Add((string)dictionary.Name);
                        }
                    }
                    var reason_choices = BotUtility.ChoiceList(startingCommand.Reasons);
                    return await BotUtility.SendChoicePrompt(stepContext, "Provide a reason for re-schedule?", reason_choices, cancellationToken);
                    }
                else
                {
                    return await BotUtility.SendErrorMessagewithend(stepContext, startingCommand, "[Status:0004]", nameof(StartDialog), cancellationToken);
                }
            }
            else
            {
                return await BotUtility.SendErrorMessagewithend(stepContext, startingCommand, "[Status:0003]", nameof(StartDialog), cancellationToken);
            }
        }

        private async Task<DialogTurnResult> GetUserReasonStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            StartingCommand startingCommand = (StartingCommand)stepContext.Options;
            startingCommand.SelectedReason = (int)(stepContext.Result as FoundChoice)?.Index;

            return await BotUtility.SendTextPrompt(stepContext, "Any Comments", cancellationToken);
        }

        private async Task<DialogTurnResult> GetUserCommentStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            StartingCommand startingCommand = (StartingCommand)stepContext.Options;
            startingCommand.Comment = (string)stepContext.Result;
            if (startingCommand.Tenant_id == "2")
            {
                startingCommand.Profile_id = "4";
            }
            if (startingCommand.Tenant_id == "17")
            {
                startingCommand.Profile_id = "553";
            }
            var response = await BotUtility.GetAvailableSlots(startingCommand, cancellationToken);
            if (response.IsSuccessStatusCode )
            {
                var responseBody = response.Content.ReadAsStringAsync(cancellationToken).Result;
                var responseObject = JsonConvert.DeserializeObject<dynamic>(responseBody);
                var totalAvailbleSlots = responseObject.Data.TotalAvailbleSlots;
                if (totalAvailbleSlots > 0)
                {   
                    startingCommand.Slot_dict = responseObject.Data.Slots[0];
                    slot_dict = responseObject.Data.Slots;
                    availableSlotsList = responseObject.Data.Slots[0].AvailableSlots;
                    var first_slot = (DateTimeOffset)availableSlotsList[1].StartDateTime;
                    var next_first_slot = first_slot.ToString("MM/dd/yyyy HH:mm tt");
                    if (totalAvailbleSlots > 0)
                    {

                        startingCommand.Choice_prompt = BotUtility.ChoiceList(new List<string> { "Yes, confirm", "More options" });
                        
                        var Choice_prompt1 = new List<Choice>{
                            new Choice {Value = "Si confirmada", Synonyms=new List<string>{ "Yes, confirm" } },
                            new Choice {Value = "Mas opciones", Synonyms=new List<string>{ "More options" }},

                            };
                        if (startingCommand.lan == "en-US" || startingCommand.lan == "")
                        {
                            return await BotUtility.SendChoicePrompt(stepContext, $"Next available slot: {next_first_slot}.\r\nWould you like to confirm it?", startingCommand.Choice_prompt, cancellationToken);
                        }
                        else
                        {
                            return await BotUtility.SendChoicePrompt(stepContext, $"Next available slot: {next_first_slot}.\r\nWould you like to confirm it?", Choice_prompt1, cancellationToken);
                        }
                        
                    }
                    else
                    {
                        await BotUtility.SendMessageWithoutReplace(stepContext, "Sorry! There is no other slot available", cancellationToken);
                        return await stepContext.NextAsync(null, cancellationToken);

                    }
                }
                else
                {
                    return await BotUtility.SendErrorMessagewithend(stepContext, startingCommand, "[Status:0005]", nameof(StartDialog), cancellationToken);
                    }
            }
            else
            {
                return await BotUtility.SendErrorMessagewithend(stepContext, startingCommand, "[Status:0005]", nameof(StartDialog), cancellationToken);
                }
        }

        private async Task<DialogTurnResult> GetUserSlotConfirmationStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            StartingCommand startingCommand = (StartingCommand)stepContext.Options;
            if (no_slot == null)
            {
                var user_choice = (stepContext.Result as FoundChoice)?.Index;
                startingCommand.Slot_confirm = (user_choice == 0);
                if (startingCommand.Slot_confirm == true)
                {
                    var service = startingCommand.Ticket_dict.ServiceId;

                    if (service.Count > 1)
                    {
                        int maxExecutionTime = 0;
                        string maxExecutionUnit = "";

                        foreach (var serviceId in service)
                        {
                            var serviceid = (string)serviceId;
                            var Executionresponse = await BotUtility.GetExecutionTimeforloop(startingCommand, cancellationToken, serviceid);
                            if (Executionresponse.IsSuccessStatusCode)
                            {
                                var ExecutionresponseBody = await Executionresponse.Content.ReadAsStringAsync(cancellationToken);
                                var ExecutionresponseObject = JsonConvert.DeserializeObject<dynamic>(ExecutionresponseBody);
                                if ((string)ExecutionresponseObject[0].ExecutionTime != "")
                                {
                                    var executiontime = (int)ExecutionresponseObject[0].ExecutionTime;
                                    var executionunit = (string)ExecutionresponseObject[0].ExecutionUnit;

                                    if (executionunit != "Hour")
                                    {
                                        // Convert minutes to hours for comparison
                                        executiontime /= 60;
                                        executionunit = "Hour";
                                    }

                                    if (executiontime > maxExecutionTime)
                                    {
                                        maxExecutionTime = executiontime;
                                        maxExecutionUnit = executionunit;

                                    }
                                }
                            }
                        }
                        if (maxExecutionUnit != "")
                        {
                            if (startingCommand.lan == "en-US" || startingCommand.lan == "")
                            {
                                startingCommand.Slot = DateTimeOffset.Parse(((DateTimeOffset)slot_dict[0].AvailableSlots[0].StartDateTime).ToString("MM/dd/yyyy hh:mm tt"));
                                if (maxExecutionUnit == "Hour")
                                {
                                    startingCommand.End_date = DateTimeOffset.Parse(((DateTimeOffset)slot_dict[0].AvailableSlots[0].EndDateTime).ToString("MM/dd/yyyy hh:mm tt")).AddHours(maxExecutionTime);

                                }
                                else
                                {
                                    startingCommand.End_date = DateTimeOffset.Parse(((DateTimeOffset)slot_dict[0].AvailableSlots[0].EndDateTime).ToString("MM/dd/yyyy hh:mm tt")).AddMinutes(maxExecutionTime);
                                }


                                startingCommand.Slot1 = startingCommand.End_date.ToString("MM/dd/yyyy HH:mm:ss zzz");
                                startingCommand.End_date1 = startingCommand.End_date.ToString("MM/dd/yyyy HH:mm:ss zzz");
                            }
                            else
                            {
                                startingCommand.Slot = DateTimeOffset.Parse(((DateTimeOffset)slot_dict[0].AvailableSlots[0].StartDateTime).ToString("dd/MM/yyyy h:mm tt"));
                                if (maxExecutionUnit == "Hour")
                                {
                                    startingCommand.End_date = DateTimeOffset.Parse(((DateTimeOffset)slot_dict[0].AvailableSlots[0].EndDateTime).ToString("dd/MM/yyyy h:mm tt")).AddHours(maxExecutionTime);

                                }
                                else
                                {
                                    startingCommand.End_date = DateTimeOffset.Parse(((DateTimeOffset)slot_dict[0].AvailableSlots[0].EndDateTime).ToString("dd/MM/yyyy h:mm tt")).AddMinutes(maxExecutionTime);
                                }
                                startingCommand.Slot1 = ((DateTimeOffset)slot_dict[0].AvailableSlots[0].StartDateTime).ToString("MM/dd/yyyy HH:mm:ss zzz");
                                startingCommand.End_date1 = startingCommand.End_date.ToString("MM/dd/yyyy HH:mm:ss zzz");
                            }
                        }
                        else
                        {
                            if (startingCommand.lan == "en-US" || startingCommand.lan == "")
                            {
                                startingCommand.Slot = DateTimeOffset.Parse(((DateTimeOffset)slot_dict[0].AvailableSlots[0].StartDateTime).ToString("MM/dd/yyyy hh:mm tt"));
                                startingCommand.End_date = DateTimeOffset.Parse(((DateTimeOffset)slot_dict[0].AvailableSlots[0].EndDateTime).ToString("MM/dd/yyyy hh:mm tt")).AddHours(2);
                                startingCommand.Slot1 = ((DateTimeOffset)slot_dict[0].AvailableSlots[0].StartDateTime).ToString("MM/dd/yyyy HH:mm:ss zzz");
                                startingCommand.End_date1 = startingCommand.End_date.ToString("MM/dd/yyyy HH:mm:ss zzz");
                            }
                            else
                            {
                                startingCommand.Slot = DateTimeOffset.Parse(((DateTimeOffset)slot_dict[0].AvailableSlots[0].StartDateTime).ToString("dd/MM/yyyy h:mm tt"));
                                startingCommand.End_date = DateTimeOffset.Parse(((DateTimeOffset)slot_dict[0].AvailableSlots[0].EndDateTime).ToString("MM/dd/yyyy hh:mm tt")).AddHours(2);
                                startingCommand.Slot1 = ((DateTimeOffset)slot_dict[0].AvailableSlots[0].StartDateTime).ToString("MM/dd/yyyy HH:mm:ss zzz");
                                startingCommand.End_date1 = startingCommand.End_date.ToString("MM/dd/yyyy HH:mm:ss zzz");
                            }
                        }



                    }
                    else
                    {
                        var Executionresponse = await BotUtility.GetExecutionTime(startingCommand, cancellationToken);
                        if (Executionresponse.IsSuccessStatusCode)
                        {
                            var ExecutionresponseBody = Executionresponse.Content.ReadAsStringAsync(cancellationToken).Result;
                            var ExecutionresponseObject = JsonConvert.DeserializeObject<dynamic>(ExecutionresponseBody);
                            if ((string)ExecutionresponseObject[0].ExecutionTime != "")
                            {

                                var executiontime = (int)ExecutionresponseObject[0].ExecutionTime;
                                var executionunit = (string)ExecutionresponseObject[0].ExecutionUnit;
                                if (startingCommand.lan == "en-US" || startingCommand.lan == "")
                                {
                                    startingCommand.Slot = DateTimeOffset.Parse(((DateTimeOffset)slot_dict[0].AvailableSlots[0].StartDateTime).ToString("MM/dd/yyyy hh:mm tt"));
                                    if (executionunit == "Hour")
                                    {
                                        startingCommand.End_date = DateTimeOffset.Parse(((DateTimeOffset)slot_dict[0].AvailableSlots[0].EndDateTime).ToString("MM/dd/yyyy hh:mm tt")).AddHours(executiontime);

                                    }
                                    else
                                    {
                                        startingCommand.End_date = DateTimeOffset.Parse(((DateTimeOffset)slot_dict[0].AvailableSlots[0].EndDateTime).ToString("MM/dd/yyyy hh:mm tt")).AddMinutes(executiontime);
                                    }


                                    startingCommand.Slot1 = startingCommand.End_date.ToString("MM/dd/yyyy HH:mm:ss zzz");
                                    startingCommand.End_date1 = startingCommand.End_date.ToString("MM/dd/yyyy HH:mm:ss zzz");
                                }
                                else
                                {
                                    startingCommand.Slot = DateTimeOffset.Parse(((DateTimeOffset)slot_dict[0].AvailableSlots[0].StartDateTime).ToString("dd/MM/yyyy h:mm tt"));
                                    if (executionunit == "Hour")
                                    {
                                        startingCommand.End_date = DateTimeOffset.Parse(((DateTimeOffset)slot_dict[0].AvailableSlots[0].EndDateTime).ToString("dd/MM/yyyy h:mm tt")).AddHours(executiontime);

                                    }
                                    else
                                    {
                                        startingCommand.End_date = DateTimeOffset.Parse(((DateTimeOffset)slot_dict[0].AvailableSlots[0].EndDateTime).ToString("dd/MM/yyyy h:mm tt")).AddMinutes(executiontime);
                                    }
                                    startingCommand.Slot1 = ((DateTimeOffset)slot_dict[0].AvailableSlots[0].StartDateTime).ToString("MM/dd/yyyy HH:mm:ss zzz");
                                    startingCommand.End_date1 = startingCommand.End_date.ToString("MM/dd/yyyy HH:mm:ss zzz");
                                }
                            }
                            else
                            {
                                if (startingCommand.lan == "en-US" || startingCommand.lan == "")
                                {
                                    startingCommand.Slot = DateTimeOffset.Parse(((DateTimeOffset)slot_dict[0].AvailableSlots[0].StartDateTime).ToString("MM/dd/yyyy hh:mm tt"));
                                    startingCommand.End_date = DateTimeOffset.Parse(((DateTimeOffset)slot_dict[0].AvailableSlots[0].EndDateTime).ToString("MM/dd/yyyy hh:mm tt")).AddHours(2);
                                    startingCommand.Slot1 = startingCommand.End_date.ToString("MM/dd/yyyy HH:mm:ss zzz");
                                    startingCommand.End_date1 = startingCommand.End_date.ToString("MM/dd/yyyy HH:mm:ss zzz");
                                }
                                else
                                {
                                    startingCommand.Slot = DateTimeOffset.Parse(((DateTimeOffset)slot_dict[0].AvailableSlots[0].StartDateTime).ToString("dd/MM/yyyy h:mm tt"));
                                    startingCommand.End_date = DateTimeOffset.Parse(((DateTimeOffset)slot_dict[0].AvailableSlots[0].EndDateTime).ToString("MM/dd/yyyy hh:mm tt")).AddHours(2);
                                    startingCommand.Slot1 = ((DateTimeOffset)slot_dict[0].AvailableSlots[0].StartDateTime).ToString("MM/dd/yyyy HH:mm:ss zzz");
                                    startingCommand.End_date1 = startingCommand.End_date.ToString("MM/dd/yyyy HH:mm:ss zzz");
                                }
                            }

                        }
                    }



                    return await stepContext.NextAsync(null, cancellationToken);
                }
                else
                {
                    for (int i = 0; i < availableSlotsList.Count; i++)
                    {
                        availableSlotsListIndex.Add(i);
                        if (startingCommand.lan == "en-US" || startingCommand.lan == "")
                        {
                            available_dates.Add(((DateTimeOffset)availableSlotsList[i].StartDateTime).ToString("MM/dd/yyyy"));
                        }
                        else
                        {
                            available_dates.Add(((DateTimeOffset)availableSlotsList[i].StartDateTime).ToString("dd/M/yyyy"));
                        }

                            
                    }

                    available_dates = available_dates.Distinct().ToList();
                    var available_dates_choices = BotUtility.ChoiceList(available_dates);

                    return await BotUtility.SendChoicePrompt(stepContext, "Select a date", available_dates_choices, cancellationToken);
                }
            }
            else { return await stepContext.NextAsync(null, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> GetDateSlotFromUserStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            StartingCommand startingCommand = (StartingCommand)stepContext.Options;
            if (no_slot == null)
            {
                if (stepContext.Result != null)
                {

                    if (startingCommand.lan == "en-US" || startingCommand.lan == "")
                    {
                        startingCommand.Dateslot = (stepContext.Result as FoundChoice)?.Value;

                        DateTimeOffset start_date = DateTimeOffset.UtcNow.AddHours(-4).DateTime;
                        foreach (var availableslot in availableSlotsList)
                        {
                            var date = ((DateTimeOffset)availableslot["StartDateTime"]);
                            if (DateTimeOffset.Parse(date.ToString("MM/dd/yyyy")) == DateTimeOffset.Parse(startingCommand.Dateslot) && date > start_date)
                            {
                                available_times.Add(((DateTimeOffset)availableslot["StartDateTime"]).ToString("HH:mm tt"));
                                end_times.Add(((DateTimeOffset)availableslot["EndDateTime"]).ToString());
                            }
                        }
                        var available_times_choices = BotUtility.ChoiceList(available_times);
                        return await BotUtility.SendChoicePrompt(stepContext, "Select a time", available_times_choices, cancellationToken);
                    }
                    else
                    {
                        startingCommand.Dateslot = (stepContext.Result as FoundChoice)?.Value;
                        //ar dateslot = DateTimeOffset.Parse(startingCommand.Dateslot);
                        var dateslot = DateTimeOffset.Parse((stepContext.Result as FoundChoice)?.Value);
                        var dateslot1 = DateTimeOffset.Parse(dateslot.ToString("dd/M/yyyy"));
                        //string dateslot1 = startingCommand.Dateslot.ToString();
                        DateTimeOffset start_date = DateTimeOffset.UtcNow.AddHours(-4).DateTime;
                        var start_date1 = start_date.ToString("d/M/yyyy HH:mm:ss");
                        var start_date2 = DateTimeOffset.Parse(start_date1);
                        foreach (var availableslot in availableSlotsList)
                        {
                            //string format = "M/d/yyyy";
                            // var date = ((DateTimeOffset)availableslot["StartDateTime"]);
                            var date = (availableslot["StartDateTime"].ToString("d/M/yyyy HH:mm:ss"));
                            date = DateTimeOffset.Parse(date);
                            if ((DateTimeOffset.Parse(date.ToString("dd/M/yyyy")) == dateslot1 && date > start_date2))
                            {
                                available_times.Add(((DateTimeOffset)availableslot["StartDateTime"]).ToString("HH:mm tt"));
                                end_times.Add(((DateTimeOffset)availableslot["EndDateTime"]).ToString());
                            }
                        }
                        var available_times_choices = BotUtility.ChoiceList(available_times);
                        return await BotUtility.SendChoicePrompt(stepContext, "Select a time", available_times_choices, cancellationToken);
                    }
                    
                }
                else
                {
                    return await stepContext.NextAsync(null, cancellationToken);
                }
            }
            else
            {
                return await stepContext.NextAsync(null, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> GetTimeSlotFromUserStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            StartingCommand startingCommand = (StartingCommand)stepContext.Options;
            if (no_slot == null) { 
                if (stepContext.Result != null)
                {
                    startingCommand.Timeslot = (int)(stepContext.Result as FoundChoice)?.Index;
                    var service = startingCommand.Ticket_dict.ServiceId;
                    if (service.Count > 1)
                    {
                        int maxExecutionTime = 0;
                        string maxExecutionUnit = "";

                        foreach (var serviceId in service)
                        {
                            var serviceid = (string)serviceId;
                            var Executionresponse = await BotUtility.GetExecutionTimeforloop(startingCommand, cancellationToken, serviceid);
                            if (Executionresponse.IsSuccessStatusCode)
                            {
                                var ExecutionresponseBody = await Executionresponse.Content.ReadAsStringAsync(cancellationToken);
                                var ExecutionresponseObject = JsonConvert.DeserializeObject<dynamic>(ExecutionresponseBody);
                                if ((string)ExecutionresponseObject[0].ExecutionTime != "" && ExecutionresponseObject.Count > 0)
                                {
                                    var executiontime = (int)ExecutionresponseObject[0].ExecutionTime;
                                    var executionunit = (string)ExecutionresponseObject[0].ExecutionUnit;

                                    if (executionunit != "Hour")
                                    {
                                        // Convert minutes to hours for comparison
                                        executiontime /= 60;
                                    }

                                    if (executiontime > maxExecutionTime)
                                    {
                                        maxExecutionTime = executiontime;
                                        maxExecutionUnit = executionunit;

                                    }
                                }
                            }
                        }
                        if (maxExecutionUnit != "")
                        {
                            startingCommand.Slot = DateTimeOffset.Parse(String.Concat(startingCommand.Dateslot, " ", available_times[startingCommand.Timeslot].Split("+")[0]));
                            if (maxExecutionUnit == "Hour")
                            {
                                startingCommand.end11 = DateTimeOffset.Parse(String.Concat(startingCommand.Dateslot, " ", available_times[startingCommand.Timeslot].Split("+")[0])).AddHours(maxExecutionTime);

                            }
                            else
                            {
                                startingCommand.end11 = DateTimeOffset.Parse(String.Concat(startingCommand.Dateslot, " ", available_times[startingCommand.Timeslot].Split("+")[0])).AddHours(maxExecutionTime);
                            }
                            startingCommand.Slot1 = startingCommand.Slot.ToString("MM/dd/yyyy HH:mm:ss zzz");
                            startingCommand.End_date1 = startingCommand.end11.ToString("MM/dd/yyyy HH:mm:ss zzz");
                        }
                        else
                        {
                            startingCommand.Slot = DateTimeOffset.Parse(String.Concat(startingCommand.Dateslot, " ", available_times[startingCommand.Timeslot].Split("+")[0]));
                            startingCommand.end11 = DateTimeOffset.Parse(String.Concat(startingCommand.Dateslot, " ", available_times[startingCommand.Timeslot].Split("+")[0])).AddHours(2);
                            startingCommand.Slot1 = startingCommand.Slot.ToString("MM/dd/yyyy HH:mm:ss zzz");
                            startingCommand.End_date1 = startingCommand.end11.ToString("MM/dd/yyyy HH:mm:ss zzz");
                        }



                    }
                    else 
                    {

                        var Executionresponse = await BotUtility.GetExecutionTime(startingCommand, cancellationToken);
                        if (Executionresponse.IsSuccessStatusCode)
                        {
                            var ExecutionresponseBody = Executionresponse.Content.ReadAsStringAsync(cancellationToken).Result;
                            var ExecutionresponseObject = JsonConvert.DeserializeObject<dynamic>(ExecutionresponseBody);
                            if ((string)ExecutionresponseObject[0].ExecutionTime != "")
                            {
                                var executiontime = (int)ExecutionresponseObject[0].ExecutionTime;
                                var executionunit = (string)ExecutionresponseObject[0].ExecutionUnit;
                                startingCommand.Slot = DateTimeOffset.Parse(String.Concat(startingCommand.Dateslot, " ", available_times[startingCommand.Timeslot].Split("+")[0]));
                                if (executionunit == "Hour")
                                {
                                    startingCommand.end11 = DateTimeOffset.Parse(String.Concat(startingCommand.Dateslot, " ", available_times[startingCommand.Timeslot].Split("+")[0])).AddHours(executiontime);

                                }
                                else
                                {
                                    startingCommand.end11 = DateTimeOffset.Parse(String.Concat(startingCommand.Dateslot, " ", available_times[startingCommand.Timeslot].Split("+")[0])).AddMinutes(executiontime);
                                }
                                startingCommand.Slot1 = startingCommand.Slot.ToString("MM/dd/yyyy HH:mm:ss zzz");
                                startingCommand.End_date1 = startingCommand.end11.ToString("MM/dd/yyyy HH:mm:ss zzz");
                                //startingCommand.End_date = DateTimeOffset.Parse(end_times[startingCommand.Timeslot]);
                                //artingCommand.End_date1 = startingCommand.End_date.ToString("MM/dd/yyyy HH:mm:ss zzz");
                            }
                            else
                            {
                                //var executiontime = (int)ExecutionresponseObject[0].ExecutionTime;
                                startingCommand.Slot = DateTimeOffset.Parse(String.Concat(startingCommand.Dateslot, " ", available_times[startingCommand.Timeslot].Split("+")[0]));
                                startingCommand.end11 = DateTimeOffset.Parse(String.Concat(startingCommand.Dateslot, " ", available_times[startingCommand.Timeslot].Split("+")[0])).AddHours(2);
                                startingCommand.Slot1 = startingCommand.Slot.ToString("MM/dd/yyyy HH:mm:ss zzz");
                                startingCommand.End_date1 = startingCommand.end11.ToString("MM/dd/yyyy HH:mm:ss zzz");
                                //startingCommand.End_date = DateTimeOffset.Parse(end_times[startingCommand.Timeslot]);
                                //startingCommand.End_date1 = startingCommand.End_date.ToString("MM/dd/yyyy HH:mm:ss zzz");
                            }

                        }
                    }

                    
                    
                }
                if (startingCommand.lan == "en-US" || startingCommand.lan == "")
                {
                    startingCommand.Date = startingCommand.Slot.ToString().Split(" ")[0];
                }
                else
                {
                    startingCommand.Date = startingCommand.Slot.ToString("M/d/yyyy").Split(" ")[0];
                }
               
                startingCommand.Hour = (startingCommand.Slot.ToString().Split(" ")[1]).Split(":")[0];
                startingCommand.Minute = (startingCommand.Slot.ToString().Split(" ")[1]).Split(":")[1];

                var response = await BotUtility.SendScheduleRequest(startingCommand, "RESCHEDULE", cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var schedule_response = await BotUtility.GetSchedule(startingCommand, cancellationToken);
                    var responseBody = schedule_response.Content.ReadAsStringAsync(cancellationToken).Result;
                    var responseObject = JsonConvert.DeserializeObject<dynamic>(responseBody);

                    if (schedule_response.IsSuccessStatusCode && (string)responseObject.Message == "RPA Schedule enabled")
                    {
     
                        await BotUtility.SendMessageWithoutReplace(stepContext, $"Your schedule is confirmed to {startingCommand.Slot:MM/dd/yyyy HH:mm tt}.", cancellationToken);
                        return await stepContext.NextAsync(null, cancellationToken);
                    }
                    else
                    {
                        return await BotUtility.SendErrorMessagewithend(stepContext, startingCommand, "[Status:0007]", nameof(StartDialog), cancellationToken);                     
                    }
                }
                else
                {
                    return await BotUtility.SendErrorMessagewithend(stepContext, startingCommand, "[Status:0006]", nameof(StartDialog), cancellationToken);
                }
            }
        else{
        return await stepContext.NextAsync(null, cancellationToken);
         }

        }
        private async Task<DialogTurnResult> ShowUserChoicesStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            
            StartingCommand startingCommand = (StartingCommand)stepContext.Options;
            startingCommand.Choice_prompt = BotUtility.ChoiceList(new List<string> { "Cancel Visit", "Contact Technician", "No, Thanks", "Something Else" });
            var Choice_prompt1 = new List<Choice>{
                        new Choice {Value = "Cancelar visita", Synonyms=new List<string>{ "Cancel Visit" }},
                        new Choice{Value = "Técnico de contacto",Synonyms=new List<string>{ "Contact Technician" }},
                        new Choice {Value = "No, gracias", Synonyms=new List<string>{ "No, Thanks" } },
                        new Choice{Value = "Algo más",Synonyms=new List<string>{ "Anything else" }}

                    };
            if (startingCommand.lan == "en-US" || startingCommand.lan == "")
            {
                return await BotUtility.SendChoicePrompt(stepContext, "Is there anything else we can do for you?", startingCommand.Choice_prompt, cancellationToken);
            }
            else
            {
                return await BotUtility.SendChoicePrompt(stepContext, "Is there anything else we can do for you?", Choice_prompt1, cancellationToken);
            }
            
        }

        private async Task<DialogTurnResult> GetUserChoiceStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            StartingCommand startingCommand = (StartingCommand)stepContext.Options;
            startingCommand.UserIntent = (string)(stepContext.Result as FoundChoice)?.Value;
            return await stepContext.EndDialogAsync(startingCommand,cancellationToken);

            //var userIntent = (int)(stepContext.Result as FoundChoice)?.Index;

            //switch (userIntent)
            //{
            //    case 0:
            //        startingCommand.OptionSelected = "Cancellation";
            //        return await stepContext.ReplaceDialogAsync(nameof(Cancel_AppointmentDialog), startingCommand, cancellationToken);


            //    case 1:
            //        return await BotUtility.SendMessage(stepContext, startingCommand, "Connecting you to Technician!", nameof(StartDialog), cancellationToken);

            //    case 2:
            //        return await stepContext.ReplaceDialogAsync(nameof(RatingDialog), startingCommand, cancellationToken);

            //    case 3:
            //        return await BotUtility.SendTextPrompt(stepContext, "How may I help you?", cancellationToken);

            //    default:
            //        return await stepContext.ReplaceDialogAsync(nameof(StartDialog), startingCommand, cancellationToken);

            //}
        }

        //private async Task<DialogTurnResult> GetUserInputStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        //{
        //    StartingCommand startingCommand = (StartingCommand)stepContext.Options;
        //    if (stepContext.Result != null)
        //    {
        //        startingCommand.Input = (string)stepContext.Result;
        //        return await stepContext.ReplaceDialogAsync(nameof(Recognize_Intent_EnrouteDialog), startingCommand, cancellationToken);
        //    }
        //    return await stepContext.ReplaceDialogAsync(nameof(StartDialog), startingCommand, cancellationToken);
        //}

    }
}
