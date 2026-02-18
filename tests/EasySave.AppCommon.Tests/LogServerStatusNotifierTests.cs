namespace EasySave.AppCommon.Tests;

public class LogServerStatusNotifierTests
{
    [Fact]
    public void InitialState_IsServerReachable_ReturnsTrue()
    {
        var notifier = new LogServerStatusNotifier();

        Assert.True(notifier.IsServerReachable);
    }

    [Fact]
    public void ReportStatus_SameAsCurrent_DoesNotFireEvent()
    {
        var notifier = new LogServerStatusNotifier();
        var fired = new List<bool>();
        notifier.ServerStatusChanged += status => fired.Add(status);

        notifier.ReportStatus(true); // same as initial

        Assert.Empty(fired);
    }

    [Fact]
    public void ReportStatus_TransitionToFalse_FiresEventWithFalse()
    {
        var notifier = new LogServerStatusNotifier();
        var fired = new List<bool>();
        notifier.ServerStatusChanged += status => fired.Add(status);

        notifier.ReportStatus(false);

        Assert.Single(fired);
        Assert.False(fired[0]);
        Assert.False(notifier.IsServerReachable);
    }

    [Fact]
    public void ReportStatus_TransitionToTrue_FiresEventWithTrue()
    {
        var notifier = new LogServerStatusNotifier();
        notifier.ReportStatus(false); // go down first
        var fired = new List<bool>();
        notifier.ServerStatusChanged += status => fired.Add(status);

        notifier.ReportStatus(true);

        Assert.Single(fired);
        Assert.True(fired[0]);
        Assert.True(notifier.IsServerReachable);
    }

    [Fact]
    public void ReportStatus_RepeatedSameValue_FiresOnlyOnce()
    {
        var notifier = new LogServerStatusNotifier();
        var fired = new List<bool>();
        notifier.ServerStatusChanged += status => fired.Add(status);

        notifier.ReportStatus(false);
        notifier.ReportStatus(false);
        notifier.ReportStatus(false);

        Assert.Single(fired);
    }

    [Fact]
    public void ReportStatus_MultipleTransitions_FiresForEachTransition()
    {
        var notifier = new LogServerStatusNotifier();
        var fired = new List<bool>();
        notifier.ServerStatusChanged += status => fired.Add(status);

        notifier.ReportStatus(false); // true -> false
        notifier.ReportStatus(true);  // false -> true
        notifier.ReportStatus(false); // true -> false

        Assert.Equal(3, fired.Count);
        Assert.Equal(new[] { false, true, false }, fired);
    }
}
