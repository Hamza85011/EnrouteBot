using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Microsoft.BotBuilderSamples
{
    public static class BotUtility
    {
        private static readonly HttpClient client = new();
        public static async Task<DialogTurnResult> SendErrorMessagewithend(WaterfallStepContext stepContext, StartingCommand startingCommand, string status_code, string replace_dialog, CancellationToken cancellationToken)
        {
            string message = "Something went wrong " + status_code + "\r\n Our team will get back to you in a while";
            var promptMessage = MessageFactory.Text(message, message, InputHints.ExpectingInput);
            await stepContext.Context.SendActivityAsync(promptMessage, cancellationToken);
            startingCommand.PrintStatement= "";
            startingCommand.end = "end";
            return await stepContext.EndDialogAsync(startingCommand, cancellationToken);
        }
        public static async Task<DialogTurnResult> SendErrorMessage(WaterfallStepContext stepContext, StartingCommand startingCommand, string status_code, string replace_dialog, CancellationToken cancellationToken)
        {
            string message = "Something went wrong " + status_code + "\r\n Our team will get back to you in a while";
            var promptMessage = MessageFactory.Text(message, message, InputHints.ExpectingInput);
            await stepContext.Context.SendActivityAsync(promptMessage, cancellationToken);
            return await stepContext.ReplaceDialogAsync(replace_dialog, startingCommand, cancellationToken);
        }

        public static async Task<DialogTurnResult> SendMessage(WaterfallStepContext stepContext, StartingCommand startingCommand, string msg, string replace_dialog, CancellationToken cancellationToken)
        {
            string message = msg;
            var promptMessage = MessageFactory.Text(message, message, InputHints.ExpectingInput);
            await stepContext.Context.SendActivityAsync(promptMessage, cancellationToken);
            return await stepContext.ReplaceDialogAsync(replace_dialog, startingCommand, cancellationToken);
        }

        public static async Task<DialogTurnResult> SendMessageAndEndDialog(WaterfallStepContext stepContext, string msg, CancellationToken cancellationToken)
        {
            var promptMessage = MessageFactory.Text(msg, msg, InputHints.ExpectingInput);
            await stepContext.Context.SendActivityAsync(promptMessage, cancellationToken);
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
        public static async Task<ResourceResponse> SendMessageWithoutReplace(WaterfallStepContext stepContext, string msg, CancellationToken cancellationToken)
        {
            string message = msg;
            var promptMessage = MessageFactory.Text(message, message, InputHints.ExpectingInput);
            return await stepContext.Context.SendActivityAsync(promptMessage, cancellationToken);
        }

        public static async Task<DialogTurnResult> SendTextPrompt(WaterfallStepContext stepContext, string msg, CancellationToken cancellationToken)
        {
            var promptMessage = MessageFactory.Text(msg, msg, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        public static async Task<DialogTurnResult> SendChoicePrompt(WaterfallStepContext stepContext, string msg, List<Choice> choices, CancellationToken cancellationToken)
        {
            var promptMessage = MessageFactory.Text(msg, msg, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions { Prompt = promptMessage, Choices = choices }, cancellationToken);
        }

        public static async Task<DialogTurnResult> SendRatingPrompt(WaterfallStepContext stepContext, string lan, CancellationToken cancellationToken)
        {
            if (lan == "en-US" || lan == "")
            {
                var adaptiveCardJson = File.ReadAllText("Cards/AdaptiveCard.json");
                var adaptiveCardAttachment = new Attachment
                {
                    ContentType = "application/vnd.microsoft.card.adaptive",
                    Content = JsonConvert.DeserializeObject(adaptiveCardJson)
                };
                var reply = MessageFactory.Attachment(adaptiveCardAttachment);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = (Activity)reply }, cancellationToken);
            }
            else
            {
                var adaptiveCardJson = File.ReadAllText("Cards/AdaptiveCardspanish.json");
                var adaptiveCardAttachment = new Attachment
                {
                    ContentType = "application/vnd.microsoft.card.adaptive",
                    Content = JsonConvert.DeserializeObject(adaptiveCardJson)
                };
                var reply = MessageFactory.Attachment(adaptiveCardAttachment);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = (Activity)reply }, cancellationToken);
            }
           
            
        }

        public static async Task<HttpResponseMessage> SendTicketRequest(StartingCommand startingCommand, CancellationToken cancellationToken)
        {
            string url;
            if (startingCommand.Ticket_parent == "service")
            {
                url = $"{startingCommand.Base_url}/avx2/tickets/ticket/{startingCommand.Instance_id}";
            }
            else
            {
                url = $"{startingCommand.Base_url}/avx2/tickets/ticket/{startingCommand.Instance_id}/{startingCommand.User_id}";
            }

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", startingCommand.Access_token);
            HttpResponseMessage response = await client.GetAsync(url, cancellationToken);
            return response;
        }

        public static async Task<HttpResponseMessage> SendScheduleReasonRequest(StartingCommand startingCommand, CancellationToken cancellationToken)
        {
            var url = $"{startingCommand.Base_url}/avx2/definitions/getdefinitions?DefinitionKeyCode=SCHEDULE_REASONS&UserId={startingCommand.User_id}";
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", startingCommand.Access_token);
            HttpResponseMessage response = await client.GetAsync(url, cancellationToken);
            return response;
        }

        public static async Task<HttpResponseMessage> SendScheduleRequest(StartingCommand startingCommand, string request_type, CancellationToken cancellationToken)
        {
            var date = "";
            if (startingCommand.lan == "en-US" || startingCommand.lan == "")
            {
                date = DateTimeOffset.UtcNow.ToString();
            }
            else
            {
                date = DateTimeOffset.UtcNow.ToString("MM/dd/yyyy");
            }
            string requestBody;
            if (request_type == "CANCEL")
            {
                requestBody = String.Format("{{\"InstanceId\":{0},  \"RequestId\": 0,  \"ModuleId\": {1}, \"FeatureId\": {2}, \"RequestType\": \"CANCEL\",   \"Date\": \"{3}\",   \"TimeSlot\": \"{4}\",   \"Hours\": \"\",   \"Minutes\": \"\",   \"TimeZone\": \"PKT\",   \"ReasonId\": {5},   \"Reason\": \"{6}\",   \"Comments\": \"{7}\",\"UserId\": {8}, \"TenantId\": null}}", startingCommand.Instance_id, startingCommand.Module_id, startingCommand.Feature_id, date, date, startingCommand.Reason_ids[startingCommand.SelectedReason], startingCommand.Reasons[startingCommand.SelectedReason], startingCommand.Comment, startingCommand.User_id);
            }
            else if (request_type == "RESCHEDULE")
            {
                requestBody = String.Format("{{\"InstanceId\":{0},  \"RequestId\": 0,  \"ModuleId\": \"{1}\",    \"FeatureId\": \"{2}\", \"RequestType\": \"RESCHEDULE\",   \"Date\": \"{3}\",   \"TimeSlot\": \"{4}\",   \"Hours\": \"{5}\",   \"Minutes\": \"{6}\",   \"TimeZone\": \"PKT\",   \"ReasonId\": \"{7}\",   \"Reason\": \"{8}\",   \"Comments\": \"{9}\",\"UserId\": \"{10}\", \"TenantId\": null}}", startingCommand.Instance_id, startingCommand.Module_id, startingCommand.Feature_id, startingCommand.Date, startingCommand.Slot1,startingCommand.Hour, startingCommand.Minute, startingCommand.Reason_ids[startingCommand.SelectedReason], startingCommand.Reasons[startingCommand.SelectedReason], startingCommand.Comment, startingCommand.User_id);
            }
            else
            {
                requestBody = String.Format("{{\"InstanceId\":{0},  \"RequestId\": 0,  \"ModuleId\": \"{1}\",    \"FeatureId\": \"{2}\", \"RequestType\": \"RESCHEDULE\",   \"Date\": \"{3}\",   \"TimeSlot\": \"{4}\",   \"Hours\": \"{5}\",   \"Minutes\": \"{6}\",   \"TimeZone\": \"PKT\",   \"ReasonId\": \"\",   \"Reason\": \"\",   \"Comments\": \"{7}\",\"UserId\": \"{8}\", \"TenantId\": null}}", startingCommand.Instance_id, startingCommand.Module_id, startingCommand.Feature_id, startingCommand.Date, startingCommand.Slot1, startingCommand.Hour, startingCommand.Minute, startingCommand.Comment, startingCommand.User_id);

            }
            var url = String.Format("{0}/swi/Users/ScheduleRequest", startingCommand.Base_url);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", startingCommand.Access_token);
            HttpContent content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync(url, content, cancellationToken);
            return response;
        }


        public static async Task<HttpResponseMessage> GetAvailableSlots(StartingCommand startingCommand, CancellationToken cancellationToken)
        {

            var start_date1 = DateTimeOffset.UtcNow.AddHours(-4).DateTime;
            var start_date = start_date1.ToString("M/d/yyyy HH:mm:ss");
            var end_date1 = start_date1.AddDays(7).AddSeconds(-1);
            var end_date= end_date1.ToString("M/d/yyyy HH:mm:ss");
            
            startingCommand.JobId = String.Concat(startingCommand.Ticket_dict.ServiceId[0], startingCommand.Ticket_dict.IssueIds);
            
           
            
            var requestTypeId = startingCommand.Ticket_dict.RequestTypeId;
            var itemCategoryIds = startingCommand.Ticket_dict.ItemCategoryIds;
            var cityId = startingCommand.Ticket_dict.CityId;

            var url = String.Format("{0}/avx2/calendar/setting/getavailableslots", startingCommand.Base_url);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", startingCommand.Access_token);
            string requestBody = $"{{\"JobIds\":{startingCommand.JobId},  \"ProfileId\": {startingCommand.Profile_id},  \"StartDateTime\": \"{start_date}\",    \"EndDateTime\": \"{end_date}\", \"JobTypeIds\": \"{requestTypeId}\",   \"JobCategoryIds\": \"{itemCategoryIds}\",   \"Resources\": \"\",   \"IsGIS\": false,   \"MarketIds\": \"\",   \"IsAddress\": true,   \"CityIds\": \"{cityId}\",   \"Latitude\": \"\",   \"Longitude\": \"\",\"HashCode\": \"\", \"GridNumber\": \"\", \"TimeZoneIdentifier\": \"{startingCommand.TimeZoneName}\", \"IsGetScheduleOnly\": false,   \"IsSortDescending\": false}}";
            HttpContent content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync(url, content, cancellationToken);
            return response;
        }

        public static async Task<HttpResponseMessage> GetSchedulerConfirmation(StartingCommand startingCommand, CancellationToken cancellationToken)
        {
            string requestBody;
            if (startingCommand.Ticket_parent == "CANCEL")
            {
                requestBody = $"{{\"CalendarItemId\": {(int)startingCommand.Ticket_dict.CalendarItemId},    \"InviteeType\": {startingCommand.Confirmation_type},    \"ConfirmationType\": 3,    \"ReasonId\": {startingCommand.Reason_ids[startingCommand.SelectedReason]},    \"Comment\": \"{startingCommand.Comment}\",    \"CreatedOn\": \"{DateTimeOffset.UtcNow}\",    \"CreatedBy\": {startingCommand.CreatedBy}  }}";

            }
            else if (startingCommand.Ticket_parent == "SCHEDULE CONFIRM")
            {
                requestBody = $"{{ \"CalendarItemId\": {startingCommand.Ticket_dict.CalendarItemId},    \"InviteeType\": {startingCommand.Confirmation_type},    \"ConfirmationType\": 2,    \"ReasonId\": null,    \"Comment\": \"{startingCommand.Comment}\",    \"CreatedOn\": \"{DateTimeOffset.UtcNow}\",    \"CreatedBy\": {startingCommand.CreatedBy}}}";

            }
            else
            {
                requestBody = $"{{ \"calendaritemid\": {startingCommand.Ticket_dict.CalendarItemId},    \"InviteeType\": {startingCommand.Confirmation_type},    \"ConfirmationType\": 3,    \"reasonid\": {startingCommand.Reason_ids[startingCommand.SelectedReason]},    \"comment\": \"{startingCommand.Comment}\",    \"createdon\": \"{DateTimeOffset.UtcNow}\",    \"createdby\": {startingCommand.CreatedBy}}}";

            }
            string url = $"{startingCommand.Base_url}/avx2/calendaritem/schedulerconfirmation";
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Authorization", startingCommand.Access_token);
            HttpContent content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync(url, content, cancellationToken);
            return response;
        }

        public static async Task<HttpResponseMessage> GetSchedule(StartingCommand startingCommand, CancellationToken cancellationToken)
        {

            var schedule_url = String.Format("{0}/avx2/calendar/calendaritem/schedule", startingCommand.Base_url);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", startingCommand.Access_token);

            var calenderItemId = (int)startingCommand.Ticket_dict.CalendarItemId;
            var assigneeId = (int)startingCommand.Ticket_dict.AssigneeId;
            string schedule_requestBody = $"{{\"CalendarItemId\": {calenderItemId},       \"CalendarItemTypeId\": 1,       \"ModuleId\": {startingCommand.Module_id},       \"FeatureId\": {startingCommand.Feature_id},       \"InstanceId\":{startingCommand.Instance_id},       \"OrganizerId\": {startingCommand.User_id},       \"AssigneeId\": {assigneeId},       \"StartDate\": \"{startingCommand.Slot1}\",       \"EndDate\": \"{startingCommand.End_date1}\",       \"TargetDate\": \"{startingCommand.End_date1}\",       \"ScheduleType\": \"Schedule\",       \"IsRPAScheduled\": false,       \"ReccurenceRule\": \"\" , \"TimeZoneStandardName\": \"{startingCommand.TimeZoneName}\" }}";
            HttpContent schedule_content = new StringContent(schedule_requestBody, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync(schedule_url, schedule_content, cancellationToken);
            return response;
        }

        public static async Task<HttpResponseMessage> GetTimeZone(StartingCommand startingCommand,string lan ,CancellationToken cancellationToken)
        {
            long ticks = DateTime.UtcNow.Ticks;
            TimeSpan duration = TimeSpan.FromTicks(ticks);
            double minutes = duration.TotalMinutes;
            string url;
            if (lan == "en-US")
            {
                
                 url = $"https://maps.googleapis.com/maps/api/timezone/json?location={startingCommand.Ticket_dict.Latitude},{startingCommand.Ticket_dict.Longitude}&timestamp={minutes}&key={startingCommand.Gmap_api_key}";
              
            }
            else
            {
                 url = String.Format("https://maps.googleapis.com/maps/api/timezone/json?location={0},{1}&timestamp={2}&key={3}",
                startingCommand.Ticket_dict.Latitude.ToString().Replace(",", "."),
                startingCommand.Ticket_dict.Longitude.ToString().Replace(",", "."),
                minutes.ToString().Replace(",", "."),
                startingCommand.Gmap_api_key);
            }
            HttpResponseMessage response = await client.GetAsync(url, cancellationToken);
            return response;

        }
        public static async Task<HttpResponseMessage> GetExecutionTimeforloop(StartingCommand startingCommand, CancellationToken cancellationToken,string serviceid)
        {
            var url = $"{startingCommand.Base_url}/avx2/customfields/GetCFMultiInstanceData?moduleId=1&featureId=126&instancesId={serviceid}";
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", startingCommand.Access_token);
            HttpResponseMessage response = await client.GetAsync(url, cancellationToken);
            return response;
        }
        public static async Task<HttpResponseMessage> GetExecutionTime(StartingCommand startingCommand, CancellationToken cancellationToken)
        {
            var url = $"{startingCommand.Base_url}/avx2/customfields/GetCFMultiInstanceData?moduleId=1&featureId=126&instancesId={startingCommand.Ticket_dict.ServiceId[0]}";
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", startingCommand.Access_token);
            HttpResponseMessage response = await client.GetAsync(url, cancellationToken);
            return response;
        }

        public static async Task<HttpResponseMessage> GetPhoneByUserId(StartingCommand startingCommand, CancellationToken cancellationToken)
        {

            var url = $"{startingCommand.Base_url}/avx2/UserDashboard/GetResourcesById?resourceId={startingCommand.User_id}";
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", startingCommand.Access_token);
            HttpResponseMessage response = await client.GetAsync(url, cancellationToken);
            return response;
        }

        public static List<Choice> ChoiceList(List<string> strings)
        {
            List<Choice> choices = new();
            foreach (var value in strings)
            {
                choices.Add(new Choice { Value = value});
            }
            return choices;
        }


        public static string Encrypt(string plainText, string key)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);



            using (System.Security.Cryptography.Aes aes = System.Security.Cryptography.Aes.Create())
            {
                aes.Key = keyBytes;
                aes.Padding = PaddingMode.PKCS7;
                aes.Mode = CipherMode.CBC;
                aes.GenerateIV(); // Generate a random IV



                byte[] encryptedBytes;



                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                {
                    byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);



                    using (MemoryStream msEncrypt = new MemoryStream())
                    {
                        // Write the IV to the beginning of the stream
                        msEncrypt.Write(aes.IV, 0, aes.IV.Length);



                        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            csEncrypt.Write(plainBytes, 0, plainBytes.Length);
                            csEncrypt.FlushFinalBlock();
                        }



                        encryptedBytes = msEncrypt.ToArray();
                    }
                }



                return Convert.ToBase64String(encryptedBytes);
            }
        }
    }
}
