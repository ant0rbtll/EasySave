namespace EasySave.System;

public interface ITransferService
{
    TransferResult TransferFile(string src, string dst);
}
