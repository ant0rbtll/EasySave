namespace EasySave.UI.Tests;

public class JobsFlowServiceTests
{
    [Fact]
    public void GetJobCount_ReturnsCurrentRepositoryCount()
    {
        var applicationService = CreateApplicationService();
        applicationService.CreateJob("Job1", "S", "D", BackupType.Complete);

        var service = CreateService(applicationService, out _, out _, out _, out _);

        Assert.Equal(1, service.GetJobCount());
    }

    [Fact]
    public void CreateBackupJob_CreatesJobAndCallsBackToMainMenu()
    {
        var applicationService = CreateApplicationService();
        var service = CreateService(applicationService, out var menuService, out var inputService, out _, out _);

        inputService.StringAnswers.Enqueue("JobA");
        inputService.StringAnswers.Enqueue("Source");
        inputService.StringAnswers.Enqueue("Destination");
        inputService.BackupTypeAnswers.Enqueue(BackupType.Complete);

        var callbackCalled = false;
        service.CreateBackupJob(() => callbackCalled = true);

        Assert.True(callbackCalled);
        Assert.Single(applicationService.GetAllJobs());
        Assert.Equal(1, menuService.WaitCalls);
        Assert.Contains(LocalizationKey.menu_create, menuService.DisplayedLabels);
    }

    [Fact]
    public void ShowJobsList_DisplaysJobsMenuAndNavigatesToJobDetails()
    {
        var applicationService = CreateApplicationService();
        applicationService.CreateJob("A", "S1", "D1", BackupType.Complete);
        applicationService.CreateJob("B", "S2", "D2", BackupType.Differential);

        var service = CreateService(applicationService, out var menuService, out _, out _, out _);

        service.ShowJobsList(() => { });

        var listMenu = Assert.Single(menuService.ShownMenuConfigs);
        Assert.Equal(LocalizationKey.menu_manage_jobs, listMenu.Label);
        Assert.NotNull(listMenu.ItemsAsStrings);
        Assert.Equal(3, listMenu.ItemsAsStrings!.Length);

        listMenu.Actions[0]();

        Assert.Equal(2, menuService.ShownMenuConfigs.Count);
        Assert.Equal(LocalizationKey.menu_job_details, menuService.ShownMenuConfigs[1].Label);
    }

    private static JobsFlowService CreateService(
        BackupApplicationService applicationService,
        out FakeMenuService menuService,
        out FakeConsoleInputService inputService,
        out FakeConsoleMessageService messageService,
        out FakeConsoleAdapter consoleAdapter)
    {
        menuService = new FakeMenuService();
        inputService = new FakeConsoleInputService();
        messageService = new FakeConsoleMessageService();
        consoleAdapter = new FakeConsoleAdapter();

        var menuFactory = new MenuFactory();

        return new JobsFlowService(
            applicationService,
            menuService,
            menuFactory,
            messageService,
            inputService,
            consoleAdapter,
            new JobEditSessionService());
    }

    private static BackupApplicationService CreateApplicationService()
    {
        var repository = new InMemoryBackupJobRepository(new SequentialJobIdProvider());
        var backupEngine = new FakeBackupEngine();
        return new BackupApplicationService(repository, backupEngine);
    }
}
