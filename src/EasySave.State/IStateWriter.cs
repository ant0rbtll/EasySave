namespace EasySave.State;

public interface IStateWriter
{
    void Update(StateEntry entry);
    void MarkInactive(int backupId);
}
