namespace EasySave.System;

public interface ITransferService
{
    TransferResult TransferFile(string sourcePath, string destinationPath, bool overwrite);
}