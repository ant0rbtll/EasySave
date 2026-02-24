using EasySave.AppCommon.Tests.TestHelpers;
using EasySave.Exceptions;

namespace EasySave.AppCommon.Tests;

public class CompositeLoggerTests
{
    [Fact]
    public void Write_DelegatesToAllLoggers()
    {
        var spy1 = new SpyLogger();
        var spy2 = new SpyLogger();
        var spy3 = new SpyLogger();
        using var composite = new CompositeLogger(spy1, spy2, spy3);
        var entry = LogEntryFactory.Create();

        composite.Write(entry);

        Assert.Single(spy1.WrittenEntries);
        Assert.Single(spy2.WrittenEntries);
        Assert.Single(spy3.WrittenEntries);
        Assert.Same(entry, spy1.WrittenEntries[0]);
    }

    [Fact]
    public void Write_EmptyArray_DoesNotThrow()
    {
        using var composite = new CompositeLogger();
        var entry = LogEntryFactory.Create();

        var exception = Record.Exception(() => composite.Write(entry));

        Assert.Null(exception);
    }

    [Fact]
    public void Write_OneLoggerThrows_OtherLoggersStillCalled()
    {
        var throwing = new Mock<ILogger>();
        throwing.Setup(l => l.Write(It.IsAny<LogEntry>()))
            .Throws(new EasysaveDefaultException(Localization.LocalizationKey.error_unable_access_log, []));
        var spy = new SpyLogger();
        using var composite = new CompositeLogger(throwing.Object, spy);
        var entry = LogEntryFactory.Create();

        var ex = Assert.Throws<EasysaveDefaultException>(() => composite.Write(entry));

        Assert.Single(ex.Details);
        Assert.IsType<EasysaveDefaultException>(ex);
        Assert.Single(spy.WrittenEntries);
    }

    [Fact]
    public void Write_AllLoggersThrow_AggregatesAllExceptions()
    {
        var throwing1 = new Mock<ILogger>();
        throwing1.Setup(l => l.Write(It.IsAny<LogEntry>()))
            .Throws(new EasysaveDefaultException(Localization.LocalizationKey.error_unable_access_log, []));
        var throwing2 = new Mock<ILogger>();
        throwing2.Setup(l => l.Write(It.IsAny<LogEntry>()))
            .Throws(new EasysaveDefaultException(Localization.LocalizationKey.error_unable_access_log, []));
        using var composite = new CompositeLogger(throwing1.Object, throwing2.Object);
        var entry = LogEntryFactory.Create();

        var ex = Assert.Throws<EasysaveDefaultException>(() => composite.Write(entry));

        Assert.Equal("2", ex.Details);
    }

    [Fact]
    public void Dispose_DisposesDisposableLoggers()
    {
        var spy1 = new SpyLogger();
        var spy2 = new SpyLogger();
        var composite = new CompositeLogger(spy1, spy2);

        composite.Dispose();

        Assert.True(spy1.Disposed);
        Assert.True(spy2.Disposed);
    }

    [Fact]
    public void Dispose_SkipsNonDisposableLoggers()
    {
        var nonDisposable = new Mock<ILogger>();
        var spy = new SpyLogger();
        var composite = new CompositeLogger(nonDisposable.Object, spy);

        var exception = Record.Exception(() => composite.Dispose());

        Assert.Null(exception);
        Assert.True(spy.Disposed);
    }
}
