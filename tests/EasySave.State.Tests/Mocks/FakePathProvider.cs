using EasySave.State.Configuration.Paths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasySave.Configuration;

public class FakePathProvider : IPathProvider
{
    public string GetDailyLogPath(DateTime date) => "fake-log.json";
    public string GetStatePath() => "fake-state.json";
    public string GetJobsConfigPath() => "fake-jobs.json";
}