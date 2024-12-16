﻿using System.Diagnostics;
using System.Text;
using SecureLens.Logging;

namespace SecureLens.Data.Stragies;

public class LiveAdQueryStrategy : IAdQueryStrategy
{
    private readonly ILogger _logger;

    public LiveAdQueryStrategy(ILogger logger)
    {
        _logger = logger;
    }

    public List<string> QueryAdGroup(string groupName)
    {
        try
        {
            var cmd = $@"Get-ADGroupMember -Identity ""{groupName}"" -Recursive | Select-Object -ExpandProperty SamAccountName";

            var psi = new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = "-NoProfile -Command \"" + cmd + "\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            using var proc = new Process() { StartInfo = psi };
            proc.Start();

            string stdout = proc.StandardOutput.ReadToEnd();
            string stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExit();

            if (!string.IsNullOrWhiteSpace(stdout))
            {
                var lines = stdout.Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                return lines.ToList();
            }
            else
            {
                string errorMsg = stderr.Trim();
                if (errorMsg.Contains("Cannot find an object with identity", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning($"AD group '{groupName}' not found in AD.");
                }
                else
                {
                    _logger.LogError($"Failed to get AD group details for '{groupName}'. Error: {errorMsg}");
                }
                return new List<string>();
            }
        }
        catch (Exception e)
        {
            _logger.LogError($"Error querying AD group '{groupName}': {e}");
            return new List<string>();
        }
    }

    public HashSet<string> QueryAdGroupMembers(IEnumerable<string> groupNames)
    {
        var allMembers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int groupsNotFound = 0;

        foreach (var group in groupNames)
        {
            var members = QueryAdGroup(group);
            if (members.Count == 0) groupsNotFound++;
            foreach (var m in members) allMembers.Add(m);
        }

        if (groupsNotFound > 0)
        {
            _logger.LogWarning($"{groupsNotFound} groups not found or had errors.");
        }
        return allMembers;
    }
}
