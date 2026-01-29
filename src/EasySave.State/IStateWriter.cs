namespace EasySave.State;

public interface IStateWriter
{
    public void  Update(StateEntry entry);
    public void MarckInnactiv(int backupId);
}
