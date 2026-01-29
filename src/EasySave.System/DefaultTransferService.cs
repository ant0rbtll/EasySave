namespace EasySave.System;

public class DefaultTransferService : ITransferService
{
    public TransferResult TransferFile(string src, string dst)
    {
        return new TransferResult();
    }
}
