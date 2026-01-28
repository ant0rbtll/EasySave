using EasySave.Backup;

namespace EasySave.System;

public interface ITransferService
{
    TransferResult TransferFile(string src, string dst);
}
