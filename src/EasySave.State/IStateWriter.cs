namespace EasySave.State;

public interface IStateWriter
{
    public void Update(StateEntry entry);
    public void MarkInactive(int backupId);
}
