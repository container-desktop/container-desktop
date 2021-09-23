namespace ContainerDesktop.Common.DesiredStateConfiguration
{
    public interface IApplicationContext
    {
        int ExitCode { get; set; }
        void QuitApplication();
    }
}