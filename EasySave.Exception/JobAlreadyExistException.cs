namespace EasySave.Exceptions
{
    public class JobAlreadyExistException : EasysaveDefaultException
    {
        public JobAlreadyExistException(int id) : base(
            Localization.LocalizationKey.error_job_already_exist,
            [id.ToString()],
            "")
        {
        }
    }
}
