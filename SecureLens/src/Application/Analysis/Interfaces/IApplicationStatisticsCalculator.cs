﻿using SecureLens.Application.Analysis.Results;
using SecureLens.Core.Models;

namespace SecureLens.Application.Analysis.Interfaces
{
    public interface IApplicationStatisticsCalculator
    {
        Dictionary<string, ApplicationStatisticsResult> ComputeApplicationStatistics(List<CompletedUser> completedUsers, List<AdminByRequestSetting> settings);
    }
}