namespace EasySave.System;

public class DefaultTransferService : ITransferService
{
    public Backup.TransferResult TransferFile(string src, string dst)
    {
        return new Backup.TransferResult();
    }
}
