﻿using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace SecureLens
{
    public class AdminByRequestClient
    {
        private static readonly HttpClient Client = new HttpClient();
        private readonly string BaseUrlAudit;
        private readonly string BaseUrlInventory;
        private readonly string ApiKey;
        private readonly Dictionary<string, string> Headers;
        private readonly string StartDate;
        private readonly string EndDate;
        private readonly string Status;
        private readonly string Take;
        private readonly string WantGroups;
        private List<AdminByRequestSetting> Settings;
        
        public AdminByRequestClient(string apiKey)
        {
            BaseUrlInventory = "https://dc1api.adminbyrequest.com/inventory";
            BaseUrlAudit = "https://dc1api.adminbyrequest.com/auditlog";
            ApiKey = apiKey;
            StartDate = DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd");
            EndDate = DateTime.Now.ToString("yyyy-MM-dd");
            Status = "Finished";
            Take = "100";
            WantGroups = "1";
            Headers = new Dictionary<string, string>
            {
                { "apikey", ApiKey }
            };
            Settings = new List<AdminByRequestSetting>();
        }
        
        public void CreateSetting(string name, List<string> groups)
        {
            // Create a new setting and add it to the Settings list
            AdminByRequestSetting setting = new AdminByRequestSetting(name, groups);
            this.Settings.Add(setting);
        }
        
        /// <summary>
        /// Fetches inventory data asynchronously from Admin By Request API.
        /// </summary>
        public async Task<List<InventoryLogEntry>> FetchInventoryDataAsync()
        {
            
            var queryParams = new Dictionary<string, string>
            {
                { "take", Take },
                { "wantgroups", WantGroups }
            };

            var url = $"{BaseUrlInventory}?{BuildQueryString(queryParams)}";

            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    foreach (var header in Headers)
                        request.Headers.Add(header.Key, header.Value);

                    request.Headers.Accept.Clear();
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var response = await Client.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();

                        try
                        {
                            var inventoryData = JsonConvert.DeserializeObject<List<InventoryLogEntry>>(content);
                            DataSanitizer.SanitizeInventoryLogs(inventoryData);
                            return inventoryData;
                        }
                        catch (JsonException e)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"JSON decode error while fetching inventory: {e.Message}");
                            Console.ResetColor();
                            return new List<InventoryLogEntry>();
                        }
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Failed to fetch inventory data. Status Code: {(int)response.StatusCode} ({response.ReasonPhrase})");
                        Console.ResetColor();
                        return new List<InventoryLogEntry>();
                    }
                }
            }
            catch (HttpRequestException e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Request error while fetching inventory data: {e.Message}");
                Console.ResetColor();
                return new List<InventoryLogEntry>();
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"An unexpected error occurred: {e.Message}");
                Console.ResetColor();
                return new List<InventoryLogEntry>();
            }
        }

        /// <summary>
        /// Fetches audit log entries asynchronously from Admin By Request API
        /// </summary>
        public async Task<List<AuditLogEntry>> FetchAuditLogsAsync(Dictionary<string, string> @params)
        {
            var url = $"{BaseUrlAudit}?{BuildQueryString(@params)}";

            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    foreach (var header in Headers)
                        request.Headers.Add(header.Key, header.Value);

                    request.Headers.Accept.Clear();
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var response = await Client.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();

                        try
                        {
                            var auditLogs = JsonConvert.DeserializeObject<List<AuditLogEntry>>(content);
                            DataSanitizer.SanitizeAuditLogs(auditLogs);
                            return auditLogs;
                        }
                        catch (JsonException e)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"JSON decode error while fetching audit logs: {e.Message}");
                            Console.ResetColor();
                            return new List<AuditLogEntry>();
                        }
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Failed to fetch audit logs. Status Code: {(int)response.StatusCode} ({response.ReasonPhrase})");
                        Console.ResetColor();

                        var errorContent = await response.Content.ReadAsStringAsync();
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Response: {errorContent}");
                        Console.ResetColor();

                        return new List<AuditLogEntry>();
                    }
                }
            }
            catch (HttpRequestException e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Request error while fetching audit logs: {e.Message}");
                Console.ResetColor();
                return new List<AuditLogEntry>();
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"An unexpected error occurred: {e.Message}");
                Console.ResetColor();
                return new List<AuditLogEntry>();
            }
        }

        /// <summary>
        /// Loads "cached_inventory.json" from the local file system and deserializes it into a List of InventoryLogEntry.
        /// </summary>
        public List<InventoryLogEntry> LoadCachedInventoryData(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"File not found: {filePath}");
                Console.ResetColor();
                return new List<InventoryLogEntry>();
            }

            try
            {
                var json = File.ReadAllText(filePath);
                var inventoryData = JsonConvert.DeserializeObject<List<InventoryLogEntry>>(json);

                // Optionally sanitize
                DataSanitizer.SanitizeInventoryLogs(inventoryData);

                return inventoryData;
            }
            catch (JsonException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"JSON parse error for cached inventory: {ex.Message}");
                Console.ResetColor();
                return new List<InventoryLogEntry>();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error reading cached inventory file: {ex.Message}");
                Console.ResetColor();
                return new List<InventoryLogEntry>();
            }
        }

        /// <summary>
        /// Loads "cached_auditlogs.json" from the local file system and deserializes it into a List of AuditLogEntry.
        /// </summary>
        public List<AuditLogEntry> LoadCachedAuditLogs(string filePath)
        {

            if (!File.Exists(filePath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"File not found: {filePath}");
                Console.ResetColor();
                return new List<AuditLogEntry>();
            }

            try
            {
                var json = File.ReadAllText(filePath);
                var auditLogs = JsonConvert.DeserializeObject<List<AuditLogEntry>>(json);

                // Optionally sanitize
                DataSanitizer.SanitizeAuditLogs(auditLogs);
                return auditLogs;
            }
            catch (JsonException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"JSON parse error for cached audit logs: {ex.Message}");
                Console.ResetColor();
                return new List<AuditLogEntry>();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error reading cached audit log file: {ex.Message}");
                Console.ResetColor();
                return new List<AuditLogEntry>();
            }
        }

        /// <summary>
        /// Builds a query string from a dictionary of parameters.
        /// </summary>
        private string BuildQueryString(Dictionary<string, string> parameters)
        {
            var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
            foreach (var param in parameters)
            {
                query[param.Key] = param.Value;
            }
            return query.ToString();
        }
    }
}
