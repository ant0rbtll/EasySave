using EasySave.Core;

namespace EasySave.Persistence;

public interface ISaveWorkRepository
{
    const int DefaultMaxJobs = 5;

    void Add(SaveWork job);
    void Remove(int id);
    SaveWork GetById(int id);
    List<SaveWork> GetAll();
    int Count();
    int MaxJobs();
}
