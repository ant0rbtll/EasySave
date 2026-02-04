namespace EasySave.Persistence;

public interface IUserPreferencesRepository
{
    UserPreferences Load();
    void Save(UserPreferences preferences);
}
