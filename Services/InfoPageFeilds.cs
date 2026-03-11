using Microsoft.AspNetCore.Mvc.Rendering;
using MySqlConnector;
using Newtonsoft.Json;
using ServerCRM.Models;
using ServerCRM.Models.InfoPage;
using ServerCRM.Models.Omni;
using System.Data;
using System.Diagnostics;
using System.DirectoryServices.Protocols;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection.Emit;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using static System.Net.WebRequestMethods;
using Jose;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Asn1.Ocsp;
using Microsoft.AspNetCore.SignalR;

namespace ServerCRM.Services
{
    public static class InfoPageFeilds
    {
        private readonly static string connectionString = "Data Source=172.24.11.82; Initial Catalog=CRM_Configuration; Uid=dba; Password=Opo@1234";


        public static string GetProcessType(string processName)
        {
            string processType = null;

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT ProcessType FROM Process_Master WHERE Process_Name = @ProcessName";

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ProcessName", processName);

                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        processType = result.ToString();
                    }
                }
            }

            return processType;
        }

        public static async Task InsertEmailIntoDatabaseAsync(string sender, string subject, string body, string isAttempt, string empCode, string date, string EMPCOde, string processName)
        {
            string ConnectionStringProcess = InfoPageFeilds.getConnectionstring(processName);
            var phoneNumber = ExtractPhoneNumber(body);

            using var connection = new MySqlConnection(ConnectionStringProcess);
            await connection.OpenAsync();

            using var command = new MySqlCommand("InsertEmail_CRM", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@p_Sender", ExtractEmailAddress(sender));
            command.Parameters.AddWithValue("@p_Subject", subject);
            command.Parameters.AddWithValue("@p_Body", body);
            command.Parameters.AddWithValue("@p_PhoneNumber", phoneNumber ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@p_IsAttempt", isAttempt);
            command.Parameters.AddWithValue("@p_EmpCode", empCode ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@p_CreateDate", DateTime.Now);

            await command.ExecuteNonQueryAsync();
        }
        public async static Task<List<EmailDto>> GetEmailsWithIsAttemptZeroAsync(string processName)
        {
            string ConnectionStringProcess = InfoPageFeilds.getConnectionstring(processName);
            var emailList = new List<EmailDto>();
            string connectionString = InfoPageFeilds.getConnectionstring(processName);

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = "SELECT Sender, Subject, Body FROM Emails WHERE IsAttempt = 0";

            using var command = new MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                emailList.Add(new EmailDto
                {
                    From = reader.GetString("Sender"),
                    Subject = reader.GetString("Subject"),
                    Body = reader.GetString("Body")
                });
            }

            return emailList;
        }
        public async static Task UpdateIsAttemptToOneAsync(string processName, string sender, string subject, string Reply, string EmpCode, string dispo, string subdispo, string subsubdispo, string Remark)
        {
            string connectionString = InfoPageFeilds.getConnectionstring(processName);

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string updateQuery = "UPDATE Emails SET IsAttempt = 1 , Reply =@Reply , EmpCode=@EmpCode  , Disposition=@Disposition , SubDisposition =@SubDisposition  , SubSubDisposition =@SubSubDisposition  , Remark =@Remark  WHERE Sender = @Sender AND Subject = @Subject AND IsAttempt = 0";
            using var command = new MySqlCommand(updateQuery, connection);
            command.Parameters.AddWithValue("@Sender", sender);
            command.Parameters.AddWithValue("@Subject", subject);
            command.Parameters.AddWithValue("@Reply", Reply);
            command.Parameters.AddWithValue("@EmpCode", EmpCode);
            command.Parameters.AddWithValue("@Disposition", dispo);
            command.Parameters.AddWithValue("@SubDisposition", subdispo);
            command.Parameters.AddWithValue("@SubSubDisposition", subsubdispo);
            command.Parameters.AddWithValue("@Remark", Remark);
            await command.ExecuteNonQueryAsync();
        }

        private static string ExtractEmailAddress(string input)
        {
            if (string.IsNullOrEmpty(input))
                return null;

            var match = Regex.Match(input, @"<([^>]+)>");
            if (match.Success)
                return match.Groups[1].Value;
            return input.Trim();
        }

        private static string ExtractPhoneNumber(string body)
        {
            var match = Regex.Match(body ?? "", @"(\+91[\s-]?\d{10})|(\d{10})");

            return match.Success ? match.Value : null;
        }

       


        public static void InsertHistory(Dictionary<string, object> data, string opoId, string processName, DateTime? startTime, DateTime? endTime, string disType, string recordingPath, string campaignPhone, string myCode, string finishCode, string connID, string BatchID, string campaignName , string AgentName)
        {
            try
            {
                string entity = data.ContainsKey("entity") ? data["entity"]?.ToString()?.Trim() : null;

                string partyId = data.ContainsKey("partyId") ? data["partyId"]?.ToString()?.Trim() : null;
                string disposition = data.ContainsKey("disposition") ? data["disposition"]?.ToString()?.Trim() : null;
                string subDisposition = data.ContainsKey("subDisposition") ? data["subDisposition"]?.ToString()?.Trim() : null;
                string callBackDateOutcome = data.ContainsKey("callBackDateOutcome") ? data["callBackDateOutcome"]?.ToString()?.Trim() : null;
                string remark = data.ContainsKey("remark") ? data["remark"]?.ToString()?.Trim() : null;
                string pcb = data.ContainsKey("dispTypeKey") ? data["dispTypeKey"]?.ToString()?.Trim() : null;
                string ConnectionStringProcess = InfoPageFeilds.getConnectionstring(processName);
                using (MySqlConnection conn = new MySqlConnection(ConnectionStringProcess))
                {
                    conn.Open();

                    using (MySqlCommand cmd = new MySqlCommand("UpdateInputMaster", conn))
                    {
                        cmd.CommandType = System.Data.CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("p_opoId", opoId);
                        cmd.Parameters.AddWithValue("p_endTime", endTime.HasValue ? endTime.Value : (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("p_campaignPhone", campaignPhone);
                        cmd.Parameters.AddWithValue("p_myCode", myCode);
                        cmd.Parameters.AddWithValue("p_disposition", disposition);
                        cmd.Parameters.AddWithValue("p_subDisposition", subDisposition);
                        cmd.Parameters.AddWithValue("p_remark", remark);
                        cmd.ExecuteNonQuery();
                    }
                }
                using (var conn = new MySqlConnection(ConnectionStringProcess))
                {
                    conn.Open();

                    using (var cmd = new MySqlCommand("InsertHistory", conn))
                    {
                        cmd.CommandType = System.Data.CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("p_MyCode", myCode ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("p_Phone", campaignPhone);
                        cmd.Parameters.AddWithValue("p_ConnectTime", startTime ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("p_DisconnectTime", endTime ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("p_DisposeTime",DateTime.Now);
                        cmd.Parameters.AddWithValue("p_Connid", connID);
                        cmd.Parameters.AddWithValue("p_AgentID", opoId);
                        cmd.Parameters.AddWithValue("p_DispoCode", data.ContainsKey("disposition") ? data["disposition"]?.ToString()?.Trim() : (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("p_SubDispoCode", data.ContainsKey("subDisposition") ? data["subDisposition"]?.ToString()?.Trim() : (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("p_SubSubDispoCode", data.ContainsKey("subSubDisposition") ? data["subSubDisposition"]?.ToString()?.Trim() : (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("p_Campaign_Name", campaignName ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("p_REMARKS", remark);
                        cmd.Parameters.AddWithValue("p_CALL_DATE", startTime);
                        cmd.Parameters.AddWithValue("p_CALL_TIME", startTime);
                        cmd.Parameters.AddWithValue("p_EmployeID", opoId);
                        cmd.Parameters.AddWithValue("p_AGENT_NAME", AgentName);
                        cmd.Parameters.AddWithValue("p_BATCHID", BatchID);
                        cmd.Parameters.AddWithValue("p_callbacktime", data.ContainsKey("callBackDateOutcome") && DateTime.TryParse(data["callBackDateOutcome"]?.ToString(), out DateTime cbTime) ? cbTime : (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("p_DisconnectType", disType ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("p_RecPath", recordingPath ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("p_partyId", partyId);
                        cmd.Parameters.AddWithValue("p_entity", entity);
                        cmd.ExecuteNonQuery();

                     
                    }

                    var keysToExclude = new HashSet<string>
                    {
                        "disposition",
                        "subDisposition",
                         "subSubDisposition",
                        "callBackDateOutcome",
                        "remark",
                        "dispTypeKey"
                    };

                    var filteredData = data
                        .Where(kvp => !keysToExclude.Contains(kvp.Key))
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    if (filteredData.Count == 0)
                    {
                        Console.WriteLine("No columns to update after filtering.");
                        return;
                    }


                    var setClauses = string.Join(", ", filteredData.Keys.Select(k => $"{k} = @{k}"));
                 
                    string sql = $"UPDATE History SET {setClauses} WHERE Connid = @ConnID";
                    using (var connection = new MySqlConnection(ConnectionStringProcess))
                    {
                        connection.Open();
                       
                        using (var command = new MySqlCommand(sql, connection))
                        {

                            foreach (var kvp in filteredData)
                            {
                                object value = kvp.Value;
                                
                                if (value is JsonElement jsonElement)
                                {
                                    value = InfoPageFeilds.ConvertJsonElement(jsonElement);
                                }
                                
                                command.Parameters.AddWithValue("@" + kvp.Key, value ?? DBNull.Value);
                            }
                            command.Parameters.AddWithValue("@ConnID", connID);
                            int rowsAffected = command.ExecuteNonQuery();
                            Console.WriteLine($"{rowsAffected} row(s) updated.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }



        private static object ConvertJsonElement(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    if (element.TryGetDateTime(out var dt))
                        return dt;
                    return element.GetString();
                case JsonValueKind.Number:
                    if (element.TryGetInt32(out var intValue))
                        return intValue;
                    if (element.TryGetInt64(out var longValue))
                        return longValue;
                    if (element.TryGetDecimal(out var decimalValue))
                        return decimalValue;
                    return element.GetDouble();
                case JsonValueKind.True:
                case JsonValueKind.False:
                    return element.GetBoolean();
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return DBNull.Value;
                default:
                    return element.ToString();
            }
        }
        public static List<Disposition> GetDispositionsAsync(string Proces, string empCode)
        {
            string connectionString = InfoPageFeilds.getConnectionstring(Proces);
            List<Disposition> dispositions = new List<Disposition>();
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand("GetDispositions", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@empCode", empCode);

                        using (var reader = cmd.ExecuteReader())
                        {
                            var schemaTable = reader.GetSchemaTable();
                            Dictionary<int, Disposition> dispositionMap = new Dictionary<int, Disposition>();

                            while (reader.Read())
                            {
                                int dispositionId = Convert.ToInt32(reader["DispositionCode"]);
                                if (!dispositionMap.TryGetValue(dispositionId, out var disposition))
                                {
                                    disposition = new Disposition
                                    {
                                        Id = dispositionId,
                                        Name = reader["Disposition"].ToString(),
                                        DISP_TYPE = reader["DISP_TYPE"].ToString(),
                                        SubDispositions = new List<SubDisposition>()
                                    };
                                    dispositionMap[dispositionId] = disposition;
                                }

                                int subDispositionId = 0;
                                bool hasSubDispositionCode = false;

                                if (reader.GetSchemaTable().AsEnumerable().Any(row => row["ColumnName"].ToString() == "SubDispositionCode"))
                                {
                                    hasSubDispositionCode = true;
                                    if (!reader.IsDBNull(reader.GetOrdinal("SubDispositionCode")))
                                    {
                                        subDispositionId = Convert.ToInt32(reader["SubDispositionCode"]);
                                    }
                                }

                                if (hasSubDispositionCode && subDispositionId != 0)
                                {
                                    var subDisposition = disposition.SubDispositions
                                        .FirstOrDefault(sd => sd.Id == subDispositionId);

                                    if (subDisposition == null)
                                    {
                                        subDisposition = new SubDisposition
                                        {
                                            Id = subDispositionId,
                                            Name = reader["SubDisposition"]?.ToString(),
                                            DISP_TYPE = reader["DISP_TYPE"].ToString(),
                                            SubSubDispositions = new List<SubSubDisposition>()
                                        };
                                        disposition.SubDispositions.Add(subDisposition);
                                    }


                                    bool hasSubSubDisposition = schemaTable.AsEnumerable().Any(row => row["ColumnName"].ToString() == "SubSubDisposition");
                                    bool hasSubSubDispositionCode = schemaTable.AsEnumerable().Any(row => row["ColumnName"].ToString() == "SubSubDispositionCode");

                                    string subSubDispositionValue = null;
                                    string subSubDispositionCodeStr = null;

                                    if (hasSubSubDisposition)
                                    {
                                        subSubDispositionValue = reader["SubSubDisposition"]?.ToString();
                                    }

                                    if (hasSubSubDispositionCode)
                                    {
                                        subSubDispositionCodeStr = reader["SubSubDispositionCode"]?.ToString();
                                    }

                                    if (!string.IsNullOrWhiteSpace(subSubDispositionValue) &&
                                        int.TryParse(subSubDispositionCodeStr, out int subSubDispositionId))
                                    {
                                        if (!subDisposition.SubSubDispositions.Any(ssd => ssd.Id == subSubDispositionId))
                                        {
                                            subDisposition.SubSubDispositions.Add(new SubSubDisposition
                                            {
                                                Id = subSubDispositionId,
                                                Name = subSubDispositionValue
                                            });
                                        }
                                    }
                                }
                            }

                            dispositions = dispositionMap.Values.ToList();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error fetching dispositions: " + ex.Message);
            }

            return dispositions;
        }

        public static string GetInfoPageFeildsnew(string Process, string MyCode)
        {
            List<FieldData> fieldDataList = new List<FieldData>();

            try
            {

                string mainConnectionString = connectionString;
                using (MySqlConnection conn = new MySqlConnection(mainConnectionString))
                {
                    conn.Open();


                    using (MySqlCommand cmd = new MySqlCommand("sp_GetFieldsByLOBTypeProd", conn)) 
                   
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@p_ProcessName", Process);
                        cmd.Parameters.AddWithValue("@p_MyCode", MyCode);


                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                FieldData data = new FieldData
                                {
                                    FieldType = reader["FieldType"]?.ToString(),
                                    CapturableField = reader["CapturableField"]?.ToString(),
                                    IsRequired = reader["IsRequired"]?.ToString(),
                                    SequenceWisefield = reader["SequenceWisefield"]?.ToString(),
                                    FieldName = reader["FieldName"]?.ToString(),
                                    isDependent = reader["isDependent"]?.ToString(),
                                    SourceTableName = reader["SourceTableName"]?.ToString(),
                                    IsfieldDependent = reader["IsfieldDependent"]?.ToString(),
                                    FieldDependetName = reader["FieldDependetName"]?.ToString(),
                                    Isinitaldisplay = reader["Isinitaldisplay"]?.ToString(),
                                    DisplaySource = reader["DisplaySource"]?.ToString(),
                                    DisplaySourceValue = reader["DisplaySourceValue"]?.ToString()
                                };

                                if (data.isDependent == "YES" && !string.IsNullOrEmpty(data.SourceTableName))
                                {
                                    data.DependentData = InfoPageFeilds.GetDependentData(data.SourceTableName, Process);
                                }

                                fieldDataList.Add(data);
                            }
                        }
                    }
                }


                var displayFields = fieldDataList
                    .Where(f => f.CapturableField == "Display" && !string.IsNullOrWhiteSpace(f.FieldName))
                    .ToList();

                if (displayFields.Count > 0)
                {
                    var columnNames = displayFields.Select(f => f.FieldName).Distinct().ToList();


                    string columnList = string.Join(",", columnNames.Select(c => $"`{c}`"));
                    string inputMasterConnectionString = InfoPageFeilds.getConnectionstring(Process);

                    string sqlQuery = $"SELECT {columnList} FROM Input_Master WHERE MYCODE = @MyCode LIMIT 1";

                    Dictionary<string, string> columnValues = new Dictionary<string, string>();


                    using (MySqlConnection conn2 = new MySqlConnection(inputMasterConnectionString))
                    {
                        conn2.Open();

                        using (MySqlCommand fetchCmd = new MySqlCommand(sqlQuery, conn2))
                        {
                            fetchCmd.Parameters.AddWithValue("@MyCode", MyCode);

                            using (MySqlDataReader reader = fetchCmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    foreach (var col in columnNames)
                                    {
                                        columnValues[col] = reader[col]?.ToString();
                                    }
                                }
                            }
                        }
                    }

                    foreach (var field in displayFields)
                    {
                        if (columnValues.TryGetValue(field.FieldName, out string value))
                        {
                            field.DisplaySourceValue = value;
                        }
                    }
                }


                var sortedFields = fieldDataList
                    .OrderBy(f =>
                    {
                        if (int.TryParse(f.SequenceWisefield, out int seq))
                            return seq;
                        return int.MaxValue;
                    })
                    .ToList();


                return JsonConvert.SerializeObject(sortedFields);
            }
            catch (Exception ex)
            {

                return JsonConvert.SerializeObject(new { error = "Failed to fetch data", details = ex.Message });
            }
        }

        public static void WarmUpMySqlConnection()
        {
            MySqlConnection conn = null;
            try
            {
                conn = new MySqlConnection(connectionString);
                conn.Open();  
                Console.WriteLine("MySQL warm-up connection opened successfully.");
            }
            catch (Exception ex)
            {
               
                Console.WriteLine("Warm-up DB failed: " + ex.Message);
            }
            finally
            {
                if (conn != null)
                {
                    conn.Close();   
                    Console.WriteLine("MySQL warm-up connection closed.");
                }
            }
        }


        public static string getConnectionstring(string Process)
        {
            string decryptedConn = "Data Source=172.24.11.82; Initial Catalog=CRM_Configuration; Uid=dba; Password=Opo@1234";

            string newConnString = "";
            using (MySqlConnection con = new MySqlConnection(decryptedConn))
            {
                con.Open();

                string query = @"
        CALL GetConnectionString_New(@ProcessId);";

                using (MySqlCommand cmd = new MySqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@ProcessId", Process);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string dbServer = reader.GetString("Process_DBServer");
                            string dbUserEnc = reader.GetString("Process_DBUserID");
                            string dbPassEnc = reader.GetString("Process_DBPassword");
                            string dbName = reader.GetString("Process_DBName");

                            string dbUser = DL_Encrpt.Decrypt(dbUserEnc);
                            string dbPass = DL_Encrpt.Decrypt(dbPassEnc);

                            newConnString = $"Server={dbServer};Database={dbName};User Id={dbUser};Password={dbPass};";
                        }
                    }
                }
            }

            return newConnString;


        }
        public static List<SelectListItem> GetDependentData(string sourceTableName, string Process)
        {

            List<SelectListItem> droplist = new List<SelectListItem>();

            string str = InfoPageFeilds.getConnectionstring(Process);


            string selectQuery = InfoPageFeilds.GetSelectQueryForTable(sourceTableName, Process);

            using (MySqlConnection con = new MySqlConnection(str))
            {
                con.Open();

                using (MySqlCommand cmd = new MySqlCommand(selectQuery, con))
                {
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {

                        var columnNames = Enumerable.Range(0, reader.FieldCount)
                                                    .Select(i => reader.GetName(i))
                                                    .ToList();

                        while (reader.Read())
                        {

                            if (columnNames.Contains("Name") && reader["Name"] != DBNull.Value)
                            {
                                droplist.Add(new SelectListItem
                                {
                                    Value = reader["Name"].ToString(),
                                    Text = reader["Name"].ToString()
                                });
                            }

                            else if (columnNames.Contains("Value") && columnNames.Contains("Text") &&
                                     reader["Value"] != DBNull.Value && reader["Text"] != DBNull.Value)
                            {
                                droplist.Add(new SelectListItem
                                {
                                    Value = reader["Value"].ToString(),
                                    Text = reader["Text"].ToString()
                                });
                            }
                        }
                    }
                }
            }



            return droplist;
        }

        private static string GetSelectQueryForTable(string sourceTableName, string database)
        {
            string checkColumnsQuery = $@"
    SELECT COLUMN_NAME
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = '{sourceTableName}'
    AND TABLE_SCHEMA = '{database}';";

            List<string> columns = new List<string>();


            using (MySqlConnection con = new MySqlConnection(connectionString))
            {
                con.Open();

                using (MySqlCommand cmd = new MySqlCommand(checkColumnsQuery, con))
                {
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            columns.Add(reader.GetString("COLUMN_NAME"));
                        }
                    }
                }
            }

            string selectQuery = "SELECT ";

            bool hasNameColumn = columns.Contains("Name");
            bool hasValueColumn = columns.Contains("Value");
            bool hasTextColumn = columns.Contains("Text");

            if (hasNameColumn)
            {
                selectQuery += "Name ";
            }

            if (hasValueColumn && hasTextColumn)
            {
                selectQuery += " Value, Text ";
            }

            selectQuery += $"FROM `{sourceTableName}`;";

            return selectQuery;
        }

        public static bool InsertEmailHistory(string email, string subject, string reply, string phoneNumber, string connid, string EmpCode, string processName)
        {
            try
            {
                string ConnectionStringProcess = InfoPageFeilds.getConnectionstring(processName);
                using (MySqlConnection conn = new MySqlConnection(ConnectionStringProcess))
                {
                    conn.Open();

                    using (MySqlCommand cmd = new MySqlCommand("InsertEmailHistory", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("p_Email", email);
                        cmd.Parameters.AddWithValue("p_Subject", subject);
                        cmd.Parameters.AddWithValue("p_Reply", reply);
                        cmd.Parameters.AddWithValue("p_PhoneNumber", phoneNumber);
                        cmd.Parameters.AddWithValue("p_connid", connid);
                        cmd.Parameters.AddWithValue("p_EmpCode", EmpCode);


                        int rowsAffected = cmd.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error inserting email log: " + ex.Message);
                return false;
            }
        }

        public static string GetHistoryDataByPhoneNumber(string phoneNumber, string processName)
        {
            try
            {
                string phone = phoneNumber.Substring(phoneNumber.Length - 10);

                string ConnectionStringProcess = InfoPageFeilds.getConnectionstring(processName);
                string storedProcedure = "GetHistoryDataByPhoneNumber";
                List<HistoryData> results = new List<HistoryData>();
                using (MySqlConnection connection = new MySqlConnection(ConnectionStringProcess))
                {
                    try
                    {

                        connection.Open();
                        using (MySqlCommand cmd = new MySqlCommand(storedProcedure, connection))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@inputPhoneNumber", phone);
                            using (MySqlDataReader reader = cmd.ExecuteReader())
                            {

                                while (reader.Read())
                                {
                                    var data = new HistoryData
                                    {
                                        Type = reader["Type"].ToString(),
                                        Date = reader["Date"].ToString(),
                                        Time = reader["Time"].ToString(),
                                        Disposition = reader["Disposition"].ToString(),
                                        SubDisposition = reader["SubDisposition"].ToString(),
                                        REMARKS = reader["REMARKS"].ToString(),
                                        callbacktime = reader["callbacktime"].ToString()
                                    };

                                    results.Add(data);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Return error message in case of failure
                        return JsonConvert.SerializeObject(new { error = ex.Message });
                    }
                }

                // Serialize the list of results to JSON and return it
                return JsonConvert.SerializeObject(results);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

        }


        public static async Task<string> GenerateTokenAsync(string scop, String tokanapi)
        {
            try
            {
                var tokenUrl = tokanapi;

                using (var client = new HttpClient())
                {

                    var clientId = "560b1535299e2d63d09b124b9932d9ed";
                    var clientSecret = "7254dc635610a8796c4fe52ed06d7dfc";

                    var authValue = Convert.ToBase64String(
                        Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}")
                    );

                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Basic", authValue);


                    var formData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("scope",scop)
            };

                    var content = new FormUrlEncodedContent(formData);

                    HttpResponseMessage response = await client.PostAsync(tokenUrl, content);
                    response.EnsureSuccessStatusCode();

                    var json = await response.Content.ReadAsStringAsync();

                    dynamic tokenObj = JsonConvert.DeserializeObject(json);

                    return tokenObj.access_token;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Token generation failed", ex);
            }
        }


        public static string EncryptWithCertificateJWE(string plainText)
        {
            string certBase64 = "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";

            var certificate = new X509Certificate2(Convert.FromBase64String(certBase64));

            using RSA rsa = certificate.GetRSAPublicKey();

            string jweToken = JWT.Encode(
                plainText,
                rsa,
                JweAlgorithm.RSA_OAEP_256,
                JweEncryption.A256GCM
            );

            return jweToken;
        }

       
        public static string DecryptTMBResponse(string jweResponse)
        {
            if (string.IsNullOrWhiteSpace(jweResponse))
                throw new ArgumentNullException(nameof(jweResponse));

            string pemPrivateKey = @"-----BEGIN PRIVATE KEY-----MIIEwAIBADANBgkqhkiG9w0BAQEFAASCBKowggSmAgEAAoIBAQDL4r6SvJhlDZSwt7BuYXJM6lHmX+R9NXHEg/lU/EOG0RgX0QjkQzGDJ3+R6ZPrMiTUrMx5/O1TkTPKWaH2sgrEj0/7wCCCouHehkDUB83LLdNU3YkfqvuggkEK3jTJoYlN8bxhJ1IeCNk+4/P5Vl1g8Cc68Lal/QZDJNvDwQqwGKLwTWYMr19mb0w9JCg1lHJ4YzZtUsH0vW5n8aoaC9GUGeNUHdQFuemniCKLSv7a0T7aAGOyfpdeeGyj/S+zAIPxG0EG3zozLTpBH/3ktQwPRshAx5Cm9JbiCkJ1iWuDBjFP4fzZiuGOyOrrhylAr+4+a25qQSUVobwxuy7xPTBRAgMBAAECggEBAMbzoMExVYguY2y6ImjjoBScBoVGB4GCuFxp59BdRUt2GAuNiB6tzs/LqDlq81Nrn/MEF5fmDnQgWahYJKrE+6roqcTgqxiu9rcczH/aiZ99PR4v+1GymE39LJj9Ugd7IK+1dvxa0U/LlKpA6F7jPsWMMsvZ/nEk4Yp9mhF/+vzvnmQUiufFog+br9XPBCf0CIc+cb9iUVeCWjCMr79Jtjvuvq3LcDQbk3rPDZ8gys7LYdq0cNhuGUJu4fc915w6xHn9oLgKGkEdVyUZP/zPL3rjhRUFi8nHUJBBww9cvbKe2dZqQYLMWsltqxS6EpCfNx0Ck8sCrJ9IhNHGQmNOWA0CgYEA1EYDVrLfF1snk7oNugq8lBep8HtQtN/C6V+AuS2IwMXj18y9jhyzpdQgJxsDAy33WOzUOUYFG0hdXWej+poIrG/OunEtwXGtvJ85oNHuns3ekttOsagUm2T3z0YC3bUR2+/+AUYtwqpIjMJiggrMys1tY7Sk35jQmGsY5xgDj1sCgYEA9eJpm7FqHamjbuXQ2F9vS2+r2NOUmK1xzd+FuEbPr6wtWRHs+QqND9II9a9627p4BUcyPoHzgunX3teZoBVJrXpVSykbWPuPYcLmSH1FnncCExJp/xdD47ZKYzNu8paV+bUTTaFXzmdhO+OOqkg1zYPCjI/+6pss9i4JaTUzWsMCgYEAoKxV/pvp7T3cGR9tIHLcBqRax2Iv1pjAafEV+BSVPIUNTtz0Zcsn189WfwMdJpz2amLoyGlNmDcQJJE8N4W8JvmCWvEsFw462VkUP7xnh+CAJlzFlgeJgY3NXSC7LqHN4NIpS0GZhY1q2NCRy6jtFlyj/iJP4cdDrPrzoIg2uZ0CgYEAkc7Mj236nlJtPyL64IRfTB2Ri0eUc6FOviWRd0BOgj8YuRvfKaNvGPWVKJQBx/DgoUih691F2NjwkQ3K5Noa0cucbrCWrgKm+PMJee0HbrvluAeQYZubP5pmrELgxOw1TVlqc/t8RUKar1f2ztV5SkCbCp2NLfQNCoMsQ34v0N8CgYEAvqxi6GDBy13QPPwy5Erf+9zXhhteJkYtUJKJlQn6qAiQ2sDzZXZ2kZdiinkL0ihz+qCmvsJZVlrMUvt7MjXpl27Nc60ezYnxtj4gnsMogSk3Wh0YVGs/NVPDTbVScn6W3sxkeF1YQEN59GN8ujWLuwevAbKhOI6GdgoshDVPsdE=-----END PRIVATE KEY-----";

            RSA rsaPrivate;

          
            using (var reader = new StringReader(pemPrivateKey))
            {
                var pemReader = new PemReader(reader);
                var keyObject = pemReader.ReadObject();

                RsaPrivateCrtKeyParameters rsaKey;

                if (keyObject is AsymmetricCipherKeyPair kp)
                    rsaKey = (RsaPrivateCrtKeyParameters)kp.Private;
                else if (keyObject is RsaPrivateCrtKeyParameters k)
                    rsaKey = k;
                else
                    throw new Exception("Unsupported private key format");

                rsaPrivate = RSA.Create();
                rsaPrivate.ImportParameters(DotNetUtilities.ToRSAParameters(rsaKey));
            }

         
            string decrypted = JWT.Decode(
                jweResponse,
                rsaPrivate,
                JweAlgorithm.RSA_OAEP_256,
                JweEncryption.A256GCM
            );

            return decrypted;
        }
        public class ApiResponse
        {
            public string Response { get; set; }
        }

        

        public static async Task<OracleSearchResponse?> SearchOracleByPhone(string phone)
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            if (string.IsNullOrWhiteSpace(phone))
                return null;

            string actualPhone = ExtractLast10Digits(phone);
            int maxRetries = 2;
            int attempt = 0;

            while (attempt < maxRetries)
            {
                attempt++;

                try
                {
                    using (HttpClient client = new HttpClient())
                    {
                      
                        string token = await GenerateTokenAsync(
                            "globalsearch",
                            "https://tmb.apiuat.tmbank.in/tmb-api-external/uat-ext/tmb_callcenter/oauth2/token"
                        );

                      
                        client.DefaultRequestHeaders.Clear();
                        client.DefaultRequestHeaders.Add("TMB-Client-Id", "560b1535299e2d63d09b124b9932d9ed");
                        client.DefaultRequestHeaders.Add("TMB-Client-Secret", "7254dc635610a8796c4fe52ed06d7dfc");
                        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                        client.DefaultRequestHeaders.Add("Preference", "transient");
                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));


                        var plainPayload = new
                        {
                            keywords = actualPhone, //"9095099669",
                            entities = new
                            {
                                Account = new
                                {
                                    fields = new[]
                                    {
                                "OrganizationName",
                                "PrimaryEmail.EmailAddress",
                                "PrimaryContact.PartyName",
                                "PrimaryContact.PartyId",
                                "PrimaryContact.PartyNumber"
                            }
                                },
                                Contact = new
                                {
                                    fields = new[]
                                    {
                                "PersonFirstName",
                                "PersonLastName",
                                "PartyId",
                                "PartyNumber"
                            }
                                }
                            }
                        };

                        string plainJson = JsonConvert.SerializeObject(plainPayload);

                       
                        string encryptedRequest = EncryptWithCertificateJWE(plainJson);

                        var requestBody = new { Request = encryptedRequest };
                        string jsonPayload = JsonConvert.SerializeObject(requestBody);
                        HttpContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                        string url = "https://tmb.apiuat.tmbank.in/tmb-api-external/uat-ext/cx/globalsearch";

                       
                        HttpResponseMessage response = await client.PostAsync(url, content);
                        string responseBody = await response.Content.ReadAsStringAsync();

                        CTIConnectionManager.LogToFile($"Attempt {attempt} - TMB RAW Response: {responseBody}", phone);

                        if (!response.IsSuccessStatusCode)
                        {
                            CTIConnectionManager.LogToFile($"Attempt {attempt} - API Error: {response.StatusCode}", phone);
                            if (attempt < maxRetries) continue; 
                            return null;
                        }

                      
                        var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(responseBody);
                        if (apiResponse == null || string.IsNullOrEmpty(apiResponse.Response))
                        {
                            CTIConnectionManager.LogToFile("Empty encrypted response", phone);
                            return null;
                        }

                      
                        string decryptedJson = DecryptTMBResponse(apiResponse.Response);
                        CTIConnectionManager.LogToFile("Decrypted Response: " + decryptedJson, phone);

                      
                        return JsonConvert.DeserializeObject<OracleSearchResponse>(decryptedJson);
                    }
                }
                catch (Exception ex)
                {
                    CTIConnectionManager.LogToFile($"Attempt {attempt} - SearchOracleByPhone API Error: {ex.Message}", phone);
                    if (attempt >= maxRetries)
                        return null;
                }
            }

            return null; 
        }




        public static string ExtractLast10Digits(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return string.Empty;

            try
            {
                var digitsOnly = new string(phone.Where(char.IsDigit).ToArray());

                if (digitsOnly.Length >= 10)
                {
                    return digitsOnly.Substring(digitsOnly.Length - 10);
                }
                return digitsOnly;
            }
            catch(Exception ex)
            {
                return string.Empty;
            }
                

        }

        public static async Task<OracleSearchResponse?> SearchPhoneOpenCxPage(String login_code, String Type, String phone, String Name)
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            AgentSession session = CTIConnectionManager.GetAgentSession(login_code);

            if (string.IsNullOrWhiteSpace(phone))
                return null;

            int maxRetries = 2;
            int attempt = 0;

            while (attempt < maxRetries)
            {
                attempt++;

                try
                {
                    using (HttpClient client = new HttpClient())
                    {
                        string Toakescop = Type == "Individual" ? "contact" : "account";

                        string tokanurl = Type == "Individual"
                            ? "https://tmb.apiuat.tmbank.in/tmb-api-external/uat-ext/tmb_callcenter/oauth2/token"
                            : "https://tmb.apiuat.tmbank.in/tmb-api-external/uat-ext/tmb_callcenter/oauth2/token";

                        string token = await GenerateTokenAsync(Toakescop, tokanurl);


                        client.DefaultRequestHeaders.Clear();
                        client.DefaultRequestHeaders.Add("TMB-Client-Id", "560b1535299e2d63d09b124b9932d9ed");
                        client.DefaultRequestHeaders.Add("TMB-Client-Secret", "7254dc635610a8796c4fe52ed06d7dfc");
                        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                        client.DefaultRequestHeaders.Add("Preference", "transient");
                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        object plainPayload;

                        if (Type == "Individual")
                        {
                            plainPayload = new
                            {
                                FirstName=Name,
                                RawMobileNumber=phone
                            };
                        }
                        else
                        {
                            plainPayload = new
                            {
                               OrganizationName= Name,
                               PhoneNumber= phone
                            };
                        }

                        string plainJson = JsonConvert.SerializeObject(plainPayload);


                        string encryptedRequest = EncryptWithCertificateJWE(plainJson);


                        var requestBody = new { Request = encryptedRequest };
                        string jsonPayload = JsonConvert.SerializeObject(requestBody);
                        HttpContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                        string url = Type == "Individual"
                               ? "https://tmb.apiuat.tmbank.in/tmb-api-external/uat-ext/cx/contact"
                               : "https://tmb.apiuat.tmbank.in/tmb-api-external/uat-ext/cx/account";

                        HttpResponseMessage response = await client.PostAsync(url, content);
                        string responseBody = await response.Content.ReadAsStringAsync();

                        CTIConnectionManager.LogToFile($"Attempt {attempt} - TMB RAW Response: {responseBody}", phone);

                        if (!response.IsSuccessStatusCode)
                        {
                            CTIConnectionManager.LogToFile($"Attempt {attempt} - API Error: {response.StatusCode}", phone);
                            if (attempt < maxRetries) continue;
                            return null;
                        }


                        var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(responseBody);
                        if (apiResponse == null || string.IsNullOrEmpty(apiResponse.Response))
                        {
                            CTIConnectionManager.LogToFile("Empty encrypted response", phone);
                            return null;
                        }


                        string decryptedJson = DecryptTMBResponse(apiResponse.Response);
                        CTIConnectionManager.LogToFile("Decrypted Response: " + decryptedJson, phone);

                        await CTIConnectionManager.HubContext.Clients.Group(session.AgentId).SendAsync("opencxpage", decryptedJson);

                        return JsonConvert.DeserializeObject<OracleSearchResponse>(decryptedJson);
                    }
                }
                catch (Exception ex)
                {
                    CTIConnectionManager.LogToFile($"Attempt {attempt} - SearchOracleByPhone API Error: {ex.Message}", phone);
                    if (attempt >= maxRetries)
                        return null;
                }
            }

            return null;
        }

        public static string RegisteredCustomer(string phone)
        {
            try
            {
                string actualPhone = ExtractLast10Digits(phone);
                string url = "http://172.24.11.91:8088/API/TMB_API/Registeredcustno";

                using (HttpClient client = new HttpClient())
                {
                    var json = "{\"mobNo\":\"" + actualPhone + "\"}";
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = client.PostAsync(url, content)
                                                         .GetAwaiter()
                                                         .GetResult();

                    response.EnsureSuccessStatusCode();

                    string result = response.Content
                                            .ReadAsStringAsync()
                                            .GetAwaiter()
                                            .GetResult();

                    return result;
                }
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { error = "Failed to fetch history data", details = ex.Message });
            }

        }
    }
}
