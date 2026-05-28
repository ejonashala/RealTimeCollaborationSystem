namespace RealTimeCollaborationSystem.Services.Interfaces
{
    public interface ITranslationService
    {
        string CurrentLanguage();
        string T(string key);
    }
}
