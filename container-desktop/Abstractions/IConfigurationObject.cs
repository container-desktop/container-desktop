using System.ComponentModel;

namespace ContainerDesktop.Abstractions;

public interface IConfigurationObject : INotifyPropertyChanged, IDataErrorInfo
{
    bool IsValid { get; }
}
