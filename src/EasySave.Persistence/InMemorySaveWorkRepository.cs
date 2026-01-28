using EasySave.Core;

namespace EasySave.Persistence;

public class InMemorySaveWorkRepository : ISaveWorkRepository
{
    public void Add(SaveWork job) { }
    public void Remove(int id) { }
    public SaveWork GetById(int id) => null!;
    public List<SaveWork> GetAll() => new();
    public int Count() => 0;
    public int MaxJobs() => 5;
}
