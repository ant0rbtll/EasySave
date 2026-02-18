namespace EasySave.UI.Tests;

public class JobsFlowServiceAdditionalTests
{
    [Fact]
    public void CreateBackupJob_WhenUserCancels_ReturnsToMainWithoutCreatingJob()
    {
        var app = CreateApplicationService(out _);
        var service = CreateService(app, out var menuService, out var inputService, out _, out _);
        var callbackCalled = false;

        inputService.StringAnswers.Enqueue(null);

        service.CreateBackupJob(() => callbackCalled = true);

        Assert.True(callbackCalled);
        Assert.Empty(app.GetAllJobs());
        Assert.Equal(0, menuService.WaitCalls);
    }

    [Fact]
    public void CreateBackupJob_WhenUserCancelsAtSource_ReturnsToMainWithoutCreatingJob()
    {
        var app = CreateApplicationService(out _);
        var service = CreateService(app, out var menuService, out var inputService, out _, out _);
        var callbackCalled = false;

        inputService.StringAnswers.Enqueue("Job");
        inputService.StringAnswers.Enqueue(null);

        service.CreateBackupJob(() => callbackCalled = true);

        Assert.True(callbackCalled);
        Assert.Empty(app.GetAllJobs());
        Assert.Equal(0, menuService.WaitCalls);
    }

    [Fact]
    public void CreateBackupJob_WhenUserCancelsAtDestination_ReturnsToMainWithoutCreatingJob()
    {
        var app = CreateApplicationService(out _);
        var service = CreateService(app, out var menuService, out var inputService, out _, out _);
        var callbackCalled = false;

        inputService.StringAnswers.Enqueue("Job");
        inputService.StringAnswers.Enqueue("S");
        inputService.StringAnswers.Enqueue(null);

        service.CreateBackupJob(() => callbackCalled = true);

        Assert.True(callbackCalled);
        Assert.Empty(app.GetAllJobs());
        Assert.Equal(0, menuService.WaitCalls);
    }

    [Fact]
    public void CreateBackupJob_WhenUserCancelsAtType_ReturnsToMainWithoutCreatingJob()
    {
        var app = CreateApplicationService(out _);
        var service = CreateService(app, out var menuService, out var inputService, out _, out _);
        var callbackCalled = false;

        inputService.StringAnswers.Enqueue("Job");
        inputService.StringAnswers.Enqueue("S");
        inputService.StringAnswers.Enqueue("D");
        inputService.BackupTypeAnswers.Enqueue(null);

        service.CreateBackupJob(() => callbackCalled = true);

        Assert.True(callbackCalled);
        Assert.Empty(app.GetAllJobs());
        Assert.Equal(0, menuService.WaitCalls);
    }


    [Fact]
    public void UpdateJobField_NameAction_ChangesNameInSession()
    {
        var app = CreateApplicationService(out _);
        app.CreateJob("A", "S", "D", BackupType.Complete);
        var service = CreateService(app, out var menuService, out var inputService, out _, out _);

        service.ShowJobsList(() => { });
        menuService.ShownMenuConfigs[0].Actions[0]();
        menuService.ShownMenuConfigs[1].Actions[1]();

        inputService.StringWithCurrentAnswers.Enqueue("Renamed");
        var firstUpdateMenu = menuService.ShownMenuConfigs.Last();
        firstUpdateMenu.Actions[0]();

        Assert.Equal("Renamed", app.GetAllJobs().Single().Name);
        Assert.Equal(LocalizationKey.menu_job_update, menuService.ShownMenuConfigs.Last().Label);
    }

    [Fact]
    public void UpdateJobField_TypeAction_ChangesTypeInSession()
    {
        var app = CreateApplicationService(out _);
        app.CreateJob("A", "S", "D", BackupType.Complete);
        var service = CreateService(app, out var menuService, out var inputService, out _, out _);

        service.ShowJobsList(() => { });
        menuService.ShownMenuConfigs[0].Actions[0]();
        menuService.ShownMenuConfigs[1].Actions[1]();

        inputService.BackupTypeWithCurrentAnswers.Enqueue(BackupType.Differential);
        menuService.ShownMenuConfigs.Last().Actions[3]();

        Assert.Equal(BackupType.Differential, app.GetAllJobs().Single().Type);
    }

    [Fact]
    public void UpdateJobField_SourceAndDestinationActions_ChangeFields()
    {
        var app = CreateApplicationService(out _);
        app.CreateJob("A", "S", "D", BackupType.Complete);
        var service = CreateService(app, out var menuService, out var inputService, out _, out _);

        service.ShowJobsList(() => { });
        menuService.ShownMenuConfigs[0].Actions[0]();
        menuService.ShownMenuConfigs[1].Actions[1]();

        inputService.StringWithCurrentAnswers.Enqueue("S2");
        menuService.ShownMenuConfigs.Last().Actions[1]();
        inputService.StringWithCurrentAnswers.Enqueue("D2");
        menuService.ShownMenuConfigs.Last().Actions[2]();

        var job = app.GetAllJobs().Single();
        Assert.Equal("S2", job.Source);
        Assert.Equal("D2", job.Destination);
    }

    [Fact]
    public void UpdateJobField_WhenInputIsNull_KeepsOriginalValue()
    {
        var app = CreateApplicationService(out _);
        app.CreateJob("A", "S", "D", BackupType.Complete);
        var service = CreateService(app, out var menuService, out var inputService, out _, out _);

        service.ShowJobsList(() => { });
        menuService.ShownMenuConfigs[0].Actions[0]();
        menuService.ShownMenuConfigs[1].Actions[1]();

        inputService.StringWithCurrentAnswers.Enqueue(null);
        menuService.ShownMenuConfigs.Last().Actions[0]();

        Assert.Equal("A", app.GetAllJobs().Single().Name);
    }

    [Fact]
    public void UpdateJob_SaveAction_PersistsAndReturnsToJobsList()
    {
        var app = CreateApplicationService(out _);
        app.CreateJob("A", "S", "D", BackupType.Complete);
        var service = CreateService(app, out var menuService, out var inputService, out _, out _);

        service.ShowJobsList(() => { });
        menuService.ShownMenuConfigs[0].Actions[0]();
        menuService.ShownMenuConfigs[1].Actions[1]();
        inputService.StringWithCurrentAnswers.Enqueue("SavedName");
        menuService.ShownMenuConfigs.Last().Actions[0]();

        var updateMenuAfterEdit = menuService.ShownMenuConfigs.Last();
        updateMenuAfterEdit.Actions[4]();

        Assert.Equal("SavedName", app.GetAllJobs().Single().Name);
        Assert.Equal(1, menuService.WaitCalls);
        Assert.Equal(LocalizationKey.menu_manage_jobs, menuService.ShownMenuConfigs.Last().Label);
    }

    [Fact]
    public void ExitJobUpdate_WhenUnsavedAndSaveSelected_SavesAndReturnsToDetails()
    {
        var app = CreateApplicationService(out _);
        app.CreateJob("A", "S", "D", BackupType.Complete);
        var service = CreateService(app, out var menuService, out var inputService, out _, out _);
        menuService.ShowMenuResult = 0;

        service.ShowJobsList(() => { });
        menuService.ShownMenuConfigs[0].Actions[0]();
        menuService.ShownMenuConfigs[1].Actions[1]();
        inputService.StringWithCurrentAnswers.Enqueue("SavedOnExit");
        menuService.ShownMenuConfigs.Last().Actions[0]();

        menuService.ShownMenuConfigs.Last().Actions[5]();

        Assert.Equal("SavedOnExit", app.GetAllJobs().Single().Name);
        Assert.Equal(LocalizationKey.menu_job_details, menuService.ShownMenuConfigs.Last().Label);
        Assert.Equal(1, menuService.WaitCalls);
    }

    [Fact]
    public void ExitJobUpdate_WhenUnsavedAndDiscardSelected_RestoresSnapshot()
    {
        var app = CreateApplicationService(out _);
        app.CreateJob("A", "S", "D", BackupType.Complete);
        var service = CreateService(app, out var menuService, out var inputService, out _, out _);
        menuService.ShowMenuResult = 1;

        service.ShowJobsList(() => { });
        menuService.ShownMenuConfigs[0].Actions[0]();
        menuService.ShownMenuConfigs[1].Actions[1]();
        inputService.StringWithCurrentAnswers.Enqueue("Discarded");
        menuService.ShownMenuConfigs.Last().Actions[0]();

        menuService.ShownMenuConfigs.Last().Actions[5]();

        Assert.Equal("A", app.GetAllJobs().Single().Name);
        Assert.Equal(LocalizationKey.menu_job_details, menuService.ShownMenuConfigs.Last().Label);
    }

    [Fact]
    public void ExitJobUpdate_WhenUnsavedAndBackSelected_ReturnsToUpdateMenu()
    {
        var app = CreateApplicationService(out _);
        app.CreateJob("A", "S", "D", BackupType.Complete);
        var service = CreateService(app, out var menuService, out var inputService, out _, out _);
        menuService.ShowMenuResult = 2;

        service.ShowJobsList(() => { });
        menuService.ShownMenuConfigs[0].Actions[0]();
        menuService.ShownMenuConfigs[1].Actions[1]();
        inputService.StringWithCurrentAnswers.Enqueue("Unsaved");
        menuService.ShownMenuConfigs.Last().Actions[0]();

        menuService.ShownMenuConfigs.Last().Actions[5]();

        Assert.Equal(LocalizationKey.menu_job_update, menuService.ShownMenuConfigs.Last().Label);
    }

    [Fact]
    public void ExitJobUpdate_WhenNoPendingChanges_ReturnsToDetails()
    {
        var app = CreateApplicationService(out _);
        app.CreateJob("A", "S", "D", BackupType.Complete);
        var service = CreateService(app, out var menuService, out _, out _, out _);

        service.ShowJobsList(() => { });
        menuService.ShownMenuConfigs[0].Actions[0]();
        menuService.ShownMenuConfigs[1].Actions[1]();

        menuService.ShownMenuConfigs.Last().Actions[5]();

        Assert.Equal(LocalizationKey.menu_job_details, menuService.ShownMenuConfigs.Last().Label);
    }

    [Fact]
    public void ExitJobUpdate_WhenSaveOnExitFails_ShowsErrorAndReturnsToUpdateMenu()
    {
        var app = CreateApplicationService(out _);
        app.CreateJob("A", "S", "D", BackupType.Complete);
        var service = CreateService(app, out var menuService, out var inputService, out var messageService, out _);
        menuService.ShowMenuResult = 0;

        service.ShowJobsList(() => { });
        menuService.ShownMenuConfigs[0].Actions[0]();
        var details = menuService.ShownMenuConfigs[1];
        details.Actions[1]();

        inputService.StringWithCurrentAnswers.Enqueue("WillFail");
        menuService.ShownMenuConfigs.Last().Actions[0]();

        app.RemoveJob(1);

        menuService.ShownMenuConfigs.Last().Actions[5]();

        Assert.Single(messageService.Errors);
        Assert.Equal(LocalizationKey.menu_job_update, menuService.ShownMenuConfigs.Last().Label);
    }

    [Fact]
    public void RunJob_FromDetails_UsesBackupEngine()
    {
        var app = CreateApplicationService(out var engine);
        app.CreateJob("A", "S", "D", BackupType.Complete);
        var service = CreateService(app, out var menuService, out _, out _, out _);

        service.ShowJobsList(() => { });
        menuService.ShownMenuConfigs[0].Actions[0]();
        menuService.ShownMenuConfigs[1].Actions[0]();

        Assert.Single(engine.ExecutedJobs);
        Assert.Equal(1, menuService.WaitCalls);
    }

    [Fact]
    public void RunJob_WhenEngineThrows_ShowsErrorAndStillWaits()
    {
        var repository = new InMemoryBackupJobRepository(new SequentialJobIdProvider());
        var app = new BackupApplicationService(repository, new ThrowingBackupEngine(), Moq.Mock.Of<IBackupJobStateService>());
        app.CreateJob("A", "S", "D", BackupType.Complete);
        var service = CreateService(app, out var menuService, out _, out var messageService, out _);

        service.ShowJobsList(() => { });
        menuService.ShownMenuConfigs[0].Actions[0]();
        menuService.ShownMenuConfigs[1].Actions[0]();

        Assert.Single(messageService.Errors);
        Assert.Equal(1, menuService.WaitCalls);
    }

    [Fact]
    public void DeleteJob_WhenRepositoryThrows_ShowsError()
    {
        var app = CreateApplicationService(out _);
        app.CreateJob("A", "S", "D", BackupType.Complete);
        var service = CreateService(app, out var menuService, out _, out var messageService, out var console);

        service.ShowJobsList(() => { });
        menuService.ShownMenuConfigs[0].Actions[0]();

        app.RemoveJob(1);
        console.EnqueueKey(ConsoleKey.Y, 'y');
        menuService.ShownMenuConfigs[1].Actions[2]();

        Assert.Single(messageService.Errors);
    }

    [Fact]
    public void DeleteJob_WhenConfirmedWithEnter_RemovesJob()
    {
        var app = CreateApplicationService(out _);
        app.CreateJob("A", "S", "D", BackupType.Complete);
        var service = CreateService(app, out var menuService, out _, out _, out var console);

        service.ShowJobsList(() => { });
        menuService.ShownMenuConfigs[0].Actions[0]();
        console.EnqueueKey(ConsoleKey.Enter, '\n');
        menuService.ShownMenuConfigs[1].Actions[2]();

        Assert.Empty(app.GetAllJobs());
    }

    [Fact]
    public void SaveJobUpdate_WhenRepositoryThrows_ShowsErrorAndReturnsToList()
    {
        var app = CreateApplicationService(out _);
        app.CreateJob("A", "S", "D", BackupType.Complete);
        var service = CreateService(app, out var menuService, out var inputService, out var messageService, out _);

        service.ShowJobsList(() => { });
        menuService.ShownMenuConfigs[0].Actions[0]();
        menuService.ShownMenuConfigs[1].Actions[1]();
        inputService.StringWithCurrentAnswers.Enqueue("NewName");
        menuService.ShownMenuConfigs.Last().Actions[0]();

        app.RemoveJob(1);
        menuService.ShownMenuConfigs.Last().Actions[4]();

        Assert.Single(messageService.Errors);
        Assert.Equal(LocalizationKey.menu_manage_jobs, menuService.ShownMenuConfigs.Last().Label);
    }

    [Fact]
    public void ShowJobDetails_RenderHeader_WritesAllDisplayedFields()
    {
        var app = CreateApplicationService(out _);
        app.CreateJob("A", "S", "D", BackupType.Complete);
        var service = CreateService(app, out var menuService, out _, out var messageService, out _);

        service.ShowJobsList(() => { });
        menuService.ShownMenuConfigs[0].Actions[0]();
        var detailsMenu = menuService.ShownMenuConfigs[1];
        detailsMenu.RenderHeader!.Invoke();

        Assert.Contains(messageService.Writes, call => call.Key == LocalizationKey.backupjob_id);
        Assert.Contains(messageService.Writes, call => call.Key == LocalizationKey.backupjob_name);
        Assert.Contains(messageService.Writes, call => call.Key == LocalizationKey.backupjob_source);
        Assert.Contains(messageService.Writes, call => call.Key == LocalizationKey.backupjob_destination);
        Assert.Contains(messageService.Writes, call => call.Key == LocalizationKey.backupjob_type);
    }

    private static BackupApplicationService CreateApplicationService(out FakeBackupEngine engine)
    {
        var repository = new InMemoryBackupJobRepository(new SequentialJobIdProvider());
        engine = new FakeBackupEngine();
        return new BackupApplicationService(repository, engine, Moq.Mock.Of<IBackupJobStateService>());
    }

    private sealed class ThrowingBackupEngine : IBackupEngine
    {
        public Task Execute(
            BackupJob job,
            CancellationToken cancellationToken = default,
            BackupExecutionContext? executionContext = null)
        {
            throw new InvalidOperationException("boom");
        }
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

        return new JobsFlowService(
            applicationService,
            menuService,
            new MenuFactory(),
            messageService,
            inputService,
            consoleAdapter,
            new JobEditSessionService());
    }
}
