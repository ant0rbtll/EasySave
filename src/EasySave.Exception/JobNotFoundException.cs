namespace EasySave.Exceptions
{
    public class JobNotFoundException : EasysaveDefaultException
    {
        public JobNotFoundException(int id) : base(
            Localization.LocalizationKey.error_job_not_found,
            [id.ToString()],
            "")
        {
        }
    }
}
