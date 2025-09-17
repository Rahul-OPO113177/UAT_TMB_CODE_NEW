using Microsoft.AspNetCore.SignalR;
using MySqlConnector;
using ServerCRM.Models;
using ServerCRM.Models.CTI;
using System.Collections.Concurrent;

namespace ServerCRM.Services
{
    public static class AgentStatusMapper
    {
        private static readonly ConcurrentDictionary<string, AgentStat> AgentStats = new();

        public static readonly Dictionary<int, string> StatusMap = new()
        {
            [0] = "",
            [1] = "WAITING",
            [2] = "DIALING",
            [3] = "TALKING",
            [4] = "WRAPING",
            [5] = "TEA BREAK",
            [6] = "LUNCH BREAK",
            [7] = "TRAINING BREAK",
            [8] = "QUALITY BREAK",
            [9] = "BIO BREAK",
            [10] = "HOLD",
            [11] = "LOGOUT",
            [12] = "Emergency",
            [13] = "MANUAL DIALING",
            [14] = "Backend_Work BREAK",
            [15] = "Back_to_School BREAK",
            [16] = "CM_Feedback BREAK",
            [17] = "Dialer_NonTech_DownTime BREAK",
            [18] = "Dailer_Tech_DownTime BREAK",
            [19] = "Floor_Help BREAK",
            [20] = "Health_Activities BREAK",
            [21] = "Scheduled BREAK",
            [22] = "Team_Huddle BREAK",
            [23] = "Tech_DownTime BREAK",
            [24] = "Townhall BREAK",
            [25] = "Unwell BREAK",
            [26] = "TL Feedback BREAK",
            [27] = "Vat BREAK"
        };

        public static void UpdateAgentStatus(int statusId, AgentSession session, IHubContext<CtiHub> hubContext)
        {
            var agentId = session.AgentId;
            var now = DateTime.UtcNow;

            var agentStat = AgentStats.GetOrAdd(agentId, id => new AgentStat
            {
                AgentId = id,
                CurrentStatusID = statusId,
                StatusStartTime = now
            });

            if (agentStat.StatusStartTime != default && agentStat.CurrentStatusID != 0)
            {
                var history = new AgentStatusHistory
                {
                    AgentId = agentStat.AgentId,
                    StatusId = agentStat.CurrentStatusID,
                    StatusLabel = StatusMap.GetValueOrDefault(agentStat.CurrentStatusID, "UNKNOWN"),
                    StartTime = agentStat.StatusStartTime,
                    EndTime = now
                };

                SaveStatusHistoryToDatabase(history);
                agentStat.StatusHistory.Add(history);
            }

           
            agentStat.CurrentStatusID = statusId;
            agentStat.StatusStartTime = now;

            CTIConnectionManager.UpdateAgentStatusApi(session.AgentId);
            session.CurrentStatusID = statusId;
            if (hubContext != null && StatusMap.TryGetValue(statusId, out var statusLabel))
            {
                 hubContext.Clients.Group(session.AgentId).SendAsync("ReceiveStatus", statusLabel);
            }
        }

        public static void SaveStatusHistoryToDatabase(AgentStatusHistory history)
        {
            try
            {
                const string connectionString = "server=20.20.20.82;user=dba;password=Opo@1234;database=DATAMART;";

                using var connection = new MySqlConnection(connectionString);
                using var command = new MySqlCommand(@"
            INSERT INTO AgentStatusHistories (AgentId, StatusId, StatusLabel, StartTime, EndTime , CreateDate)
            VALUES (@AgentId, @StatusId, @StatusLabel, @StartTime, @EndTime , @CreateTime)", connection);

                command.Parameters.AddWithValue("@AgentId", history.AgentId);
                command.Parameters.AddWithValue("@StatusId", history.StatusId);
                command.Parameters.AddWithValue("@StatusLabel", history.StatusLabel);
                command.Parameters.AddWithValue("@StartTime", history.StartTime);
                command.Parameters.AddWithValue("@EndTime", history.EndTime);
                command.Parameters.AddWithValue("@CreateTime", history.EndTime);

                connection.Open();
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving history: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                throw;
            }
        }

    }


}
