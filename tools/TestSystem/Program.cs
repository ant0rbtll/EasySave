using EasySave.System;

IFileSystem fs = new DefaultFileSystem();
ITransferService ts = new DefaultTransferService(fs);

var src = Path.Combine(Path.GetTempPath(), "src.txt");
var dst = Path.Combine(Path.GetTempPath(), "dst.txt");

File.WriteAllText(src, "hello");
var r = ts.TransferFile(src, dst, true);

Console.WriteLine($"{r.IsSuccess} {r.FileSizeBytes} {r.TransferTimeMs} {r.ErrorCode}");