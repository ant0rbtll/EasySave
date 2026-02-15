namespace EasySave.UI.Tests;

public class MenuFactoryTests
{
    [Fact]
    public void CreateMainMenu_WhenNoJobs_IncludesCreateAndExcludesManage()
    {
        var factory = new MenuFactory();
        var created = false;
        var managed = false;
        var configured = false;
        var quit = false;

        var menu = factory.CreateMainMenu(
            currentJobCount: 0,
            onCreateJob: () => created = true,
            onManageJobs: () => managed = true,
            onConfigureParams: () => configured = true,
            onQuit: () => quit = true);

        Assert.Equal(new[] { LocalizationKey.menu_create, LocalizationKey.menu_params, LocalizationKey.menu_quit }, menu.Items);
        menu.Actions[0]();
        menu.Actions[1]();
        menu.Actions[2]();

        Assert.True(created);
        Assert.False(managed);
        Assert.True(configured);
        Assert.True(quit);
    }

    [Fact]
    public void CreateLocaleMenu_SortsCulturesAndAddsBack()
    {
        var factory = new MenuFactory();
        var selected = "";
        var backCalled = false;
        IReadOnlyDictionary<string, LocalizationKey> cultures = new Dictionary<string, LocalizationKey>
        {
            ["fr"] = LocalizationKey.config_locale_fr,
            ["en"] = LocalizationKey.config_locale_en
        };

        var menu = factory.CreateLocaleMenu(cultures, locale => selected = locale, () => backCalled = true);

        Assert.Equal(new[] { LocalizationKey.config_locale_en, LocalizationKey.config_locale_fr, LocalizationKey.back }, menu.Items);
        menu.Actions[0]();
        menu.Actions[2]();

        Assert.Equal("en", selected);
        Assert.True(backCalled);
    }

    [Fact]
    public void CreateParamsMenu_MapsEachAction()
    {
        var factory = new MenuFactory();
        var locale = false;
        var path = false;
        var format = false;
        var back = false;

        var menu = factory.CreateParamsMenu(
            () => locale = true,
            () => path = true,
            () => format = true,
            () => back = true);

        Assert.Equal(
            new[] { LocalizationKey.menu_params_locale, LocalizationKey.menu_params_log_path, LocalizationKey.menu_params_log_format, LocalizationKey.back },
            menu.Items);

        menu.Actions[0]();
        menu.Actions[1]();
        menu.Actions[2]();
        menu.Actions[3]();

        Assert.True(locale && path && format && back);
    }

    [Fact]
    public void CreateLogFormatMenu_UsesStringItemsAndActions()
    {
        var factory = new MenuFactory();
        var json = false;
        var xml = false;
        var back = false;

        var menu = factory.CreateLogFormatMenu("json", "xml", "back", () => json = true, () => xml = true, () => back = true);

        Assert.Equal(new[] { "json", "xml", "back" }, menu.ItemsAsStrings);

        menu.Actions[0]();
        menu.Actions[1]();
        menu.Actions[2]();

        Assert.True(json && xml && back);
    }

    [Fact]
    public void CreateJobsListMenu_CreatesEntriesAndCapturesJobs()
    {
        var factory = new MenuFactory();
        var jobs = new[]
        {
            new BackupJob { Id = 1, Name = "A" },
            new BackupJob { Id = 2, Name = "B" }
        };

        BackupJob? selected = null;
        var back = false;
        var menu = factory.CreateJobsListMenu(jobs, "Back", job => selected = job, () => back = true);

        Assert.Equal(new[] { "1 - A", "2 - B", "Back" }, menu.ItemsAsStrings);
        menu.Actions[1]();
        menu.Actions[2]();

        Assert.NotNull(selected);
        Assert.Equal(2, selected!.Id);
        Assert.True(back);
    }

    [Fact]
    public void CreateJobDetailsMenu_MapsJobActions()
    {
        var factory = new MenuFactory();
        var job = new BackupJob { Id = 7, Name = "Job7" };
        BackupJob? runJob = null;
        BackupJob? updateJob = null;
        BackupJob? deleteJob = null;
        var back = false;

        var menu = factory.CreateJobDetailsMenu(
            job,
            j => runJob = j,
            j => updateJob = j,
            j => deleteJob = j,
            () => back = true);

        Assert.Equal(
            new[] { LocalizationKey.menu_job_run, LocalizationKey.menu_job_update, LocalizationKey.menu_job_delete, LocalizationKey.back },
            menu.Items);

        menu.Actions[0]();
        menu.Actions[1]();
        menu.Actions[2]();
        menu.Actions[3]();

        Assert.Equal(job, runJob);
        Assert.Equal(job, updateJob);
        Assert.Equal(job, deleteJob);
        Assert.True(back);
    }

    [Fact]
    public void CreateJobUpdateMenu_UsesTypedFieldIdentifiers()
    {
        var factory = new MenuFactory();
        var job = new BackupJob { Id = 3, Name = "A" };
        var fields = new List<JobEditableField>();
        var saved = false;
        var backed = false;

        var menu = factory.CreateJobUpdateMenu(
            job,
            (_, field) => fields.Add(field),
            _ => saved = true,
            _ => backed = true);

        menu.Actions[0]();
        menu.Actions[1]();
        menu.Actions[2]();
        menu.Actions[3]();
        menu.Actions[4]();
        menu.Actions[5]();

        Assert.Equal(
            new[] { JobEditableField.Name, JobEditableField.Source, JobEditableField.Destination, JobEditableField.Type },
            fields);
        Assert.True(saved);
        Assert.True(backed);
    }
}
