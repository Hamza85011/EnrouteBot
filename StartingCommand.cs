using Microsoft.Bot.Builder.Dialogs.Choices;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Microsoft.BotBuilderSamples
{
    public class StartingCommand
    {
        public string Base_url { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
        public string Gmap_api_key { get; set; }
        public string Encrypted_key { get; set; }
        public string PhoneNumber { get; set; }
        // lan
        public string lan { get; set; }
        public string Command { get; set; }
        public string Module_id { get; set; }
        public string Feature_id { get; set; }
        public string Instance_id { get; set; }
        public string Tenant_id{ get; set; }
        public string Profile_id{ get; set; }
        public string Arrival_time { get; set; }
        public string User_id { get; set; }
        public string Access_token { get; set; }
        public string SurveyId { get; set; }
        public string Input { get; set; }
        public dynamic Ticket_dict { get; set; }
        public dynamic Ticket_dict1 { get; set; }
        public string OptionSelected { get; set; }
        public string TimeZoneName { get; set; }
        public string Comment { get; set; }
        public string JobId { get; set; }
        public dynamic Slot_dict { get; set; }
        public bool Slot_confirm { get; set; }
        public DateTimeOffset Slot { get; set; }
        public DateTimeOffset end11 { get; set; }
        public string Slot1 { get; set; }
        public string Dateslot { get; set; }
        public int Timeslot { get; set; }
        public DateTimeOffset End_date { get; set; }
        public string End_date1 { get; set; }
        public string Date { get; set; }
        public string Hour { get; set; }
        public string Minute { get; set; }
        public int SelectedReason { get; set; }
        public List<Choice> Choice_prompt { get; set; }
        public List<Choice> Spanish_Choice_prompt { get; set; }
        public int Confirmation_type { get; set; }
        public string CreatedBy { get; set; }
        public int Confirm_intent { get; set; }
        public string Parent { get; set; }
        public int Ticket_scheduler_id { get; set; }
        public int Calendar_item_id { get; set; }
        public int Confirm { get; set;}
        public string Rating_text { get; set; }
        public int Rating { get; set; }
        public int ServiceSelected { get; set; }
        public int Flag { get; set; }
        public dynamic Ticket { get; set; }
        public int Request_type_id { get; set; }
        public string Item_id { get; set; }
        public string Request_type { get; set; }
        // for service dialog
        public int Item_type_id { get;set; }
       
       
        // for approval Dialog
        public string Ticket_title { get; set; }
        public string Ticket_ref { get; set; }
        public string Ticket_status { get; set; }
        public int Ticket_status_id { get; set; }
        public int Ticket_workflowid { get; set; }
        public string User_is_available { get; set; }
        public int Selected_status_id { get; set; }
        public string Approval_status { get; set; }

        // for CheckList Dialog
        public dynamic Response { get; set; }
        public string Signature { get; set; }
        public int c_value_index { get; set; }
        public int s_value_index { get; set; }
        public int q_value_index { get; set; }
        //public string prompt { get; set; }
        //public List<Choice> response_choices { get; set; }  
        public dynamic ServeyResponseBody { get; set; }
        public dynamic Q_values { get; set;}
        public dynamic C_value { get; set;}
        public dynamic S_values { get; set;}

        // for status dialog
        public HttpResponseMessage Status { get; set; }
        public string Feedback { get; set; }

        // for Service Dialog
        public string Ticket_parent { get; set; }

        // for Cancel and Reschedule Appointment
        public List<int> Reason_ids { get; set; }
        public List<string> Reasons { get; set; }
        // for saving intent for end dialog
        public string UserIntent { get; set; }
        public string PrintStatement{ get; set; }
        

        // for the error ending
        public string end { get; set; }
        public int counter { get; set; }


    }
}
