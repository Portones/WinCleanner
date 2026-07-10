namespace WinCleaner.Services.Interfaces
{
    public interface IScheduledMaintenanceService
    {
        bool IsTaskEnabled();
        string GetTaskNextRunTime();
        void EnableMaintenanceTask(string frequency, string dayOrMonthValue, string time);
        void DisableMaintenanceTask();
    }
}
