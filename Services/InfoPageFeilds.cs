using Microsoft.AspNetCore.Mvc.Rendering;
using MySqlConnector;
using Newtonsoft.Json;
using ServerCRM.Models.InfoPage;
using System.Data;
using System.Diagnostics;
using System.Reflection.Emit;

namespace ServerCRM.Services
{
    public static class InfoPageFeilds
    {
        private readonly static string connectionString = "Data Source=20.20.20.82; Initial Catalog=CRM_Configuration; Uid=dba; Password=Opo@1234";
        public static List<Disposition> GetDispositionsAsync(string empCode)
        {
            string connectionString = InfoPageFeilds.getConnectionstring("CBM");
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

                        using (var reader =  cmd.ExecuteReader()) 
                        {
                            Disposition currentDisposition = null;
                            while ( reader.Read()) 
                            {
                                string dispositionCode = reader.GetString("DispositionCode");

                                if (currentDisposition == null || currentDisposition.Id != Convert.ToInt32(dispositionCode))
                                {
                                    if (currentDisposition != null)
                                    {
                                        dispositions.Add(currentDisposition);
                                    }

                                    currentDisposition = new Disposition
                                    {
                                        Id = Convert.ToInt32(dispositionCode),
                                        Name = reader.GetString("Disposition"),
                                        DISP_TYPE = reader.GetString("DISP_TYPE"),
                                        SubDispositions = new List<SubDisposition>()
                                    };
                                }

                                var subDisposition = new SubDisposition
                                {
                                    Id = Convert.ToInt32(reader.GetString("SubDispositionCode")),
                                    Name = reader.GetString("SubDisposition")
                                };

                                currentDisposition.SubDispositions.Add(subDisposition);
                            }
                            if (currentDisposition != null)
                            {
                                dispositions.Add(currentDisposition);
                            }
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


        public static string GetInfoPageFeilds(string Process, string MyCode)
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

    }
}
