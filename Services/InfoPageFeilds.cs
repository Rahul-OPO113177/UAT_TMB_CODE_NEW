using Microsoft.AspNetCore.Mvc.Rendering;
using MySqlConnector;
using Newtonsoft.Json;
using ServerCRM.Models;
using ServerCRM.Models.InfoPage;
using ServerCRM.Models.Omni;
using System.Data;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ServerCRM.Services
{
    public static class InfoPageFeilds
    {
        private readonly static string connectionString = "Data Source=20.20.20.82; Initial Catalog=CRM_Configuration; Uid=dba; Password=Opo@1234";


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

        public static void InsertHistory(Dictionary<string, object> data, string opoId, string processName, DateTime? startTime, DateTime? endTime, string disType, string recordingPath, string campaignPhone, string myCode, string finishCode, string connID, string BatchID, string campaignName)
        {
            try
            {
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
                        cmd.Parameters.AddWithValue("p_DisposeTime", endTime ?? (object)DBNull.Value);
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
                        cmd.Parameters.AddWithValue("p_AGENT_NAME", opoId);
                        cmd.Parameters.AddWithValue("p_BATCHID", BatchID);
                        cmd.Parameters.AddWithValue("p_callbacktime", data.ContainsKey("callBackDateOutcome") && DateTime.TryParse(data["callBackDateOutcome"]?.ToString(), out DateTime cbTime) ? cbTime : (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("p_DisconnectType", disType ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("p_RecPath", recordingPath ?? (object)DBNull.Value);

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

                    using (MySqlCommand cmd = new MySqlCommand("GetFieldDataForCBM", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@p_Process_Name", Process);

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



        public static string getConnectionstring(string Process)
        {
            string decryptedConn = "Data Source=20.20.20.82; Initial Catalog=CRM_Configuration; Uid=dba; Password=Opo@1234";

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


    }
}
