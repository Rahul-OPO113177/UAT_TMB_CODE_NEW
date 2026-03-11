using Genesyslab.Platform.Voice.Protocols;
using System.Data;

namespace ServerCRM.Models
{
    public class AgentSession
    {
        public string? AgentId { get; set; }
        public string? DN { get; set; }
        public string? AgentName { get; set; }
        public TServerProtocol? TServerProtocol { get; set; }
        public bool IsRunning { get; set; }
        public ConnectionId? ConnID { get; set; }
        public ConnectionId? IVRConnID { get; set; }
        public string? ConforenceNumber { get; set; }
        public string? lblcallback { get; set; }
        
        public string? Location { get; set; }
        public string? OPOID { get; set; }
        public string? ProcessName { get; set; }
        public string? CampaignPhone { get; set; }
        public string? partyFirstPhone { get; set; }
        public string? CampaignMode { get; set; }
        public string? ProcessType { get; set; }
        public int? attempts { get; set; }
        public int? upcommingEvent { get; set; }
        public int? recordHandle { get; set; }
        public string? IsManual { get; set; }
        public string? CampaignName { get; set; }
        public string? CRMType { get; set; }

        public int? ocsApplicationID { get; set; }
        public  int requestID = 1;

        public string? HoldMusic_Path { get; set; }
        public bool? isbreak { get; set; }
        public string? MasterPhone { get; set; }
        public string? Prifix { get; set; }
        public  double MyCode = 0;
        public double PCBMyCode = 0;
        public bool isOnCall { get; set; }
        public bool isMarge { get; set; }
        public bool isConforence { get; set; }
        public int CurrentStatusID { get; set; }
        public int IsRedial { get; set; }

        public int IsAutoWrap { get; set; }
        public int AutoWrapTime { get; set; }
        public  string? DialAccess { get; set; }

        public string? GetnextDialAccess { get; set; }

        public string? StartTime { get; set; }
        public string? EndTime   { get; set; }
        public string? RecordingPath { get; set; }
        public string? distype { get; set; }
        public string? AgentGroup { get; set; }

        public string? finishCode { get; set; }
        public bool? IsManualDial { get; set; }
        public DataTable? dt_EntityType { get; set; }

        public object LockObj { get; } = new object();

        public string? CallUuid { get; set; }

        public string? Call_Type { get; set; }
    }
}
