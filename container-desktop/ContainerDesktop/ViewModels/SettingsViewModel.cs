using ContainerDesktop.Abstractions;
using ContainerDesktop.Services;
using ContainerDesktop.UI.Wpf.Input;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace ContainerDesktop.ViewModels;

public class SettingsViewModel : NotifyObject
{
    private readonly IConfigurationService _configurationService;
    private ObservableCollection<SettingsCategory> _settingsCategories;

    public SettingsViewModel(IConfigurationService configurationService)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        SaveChangesCommand = new DelegateCommand(SaveChanges, () => IsValid() && _configurationService.IsChanged());
        DiscardChangesCommand = new DelegateCommand(DiscardChanges, () => _configurationService.IsChanged());
        _configurationService.Configuration.PropertyChanged += (_,_) => UpdateButtonState();
        ConfigurationChangedEventManager.AddHandler(_configurationService, (_,_) => UpdateButtonState());
        LoadSettings();
    }

    public DelegateCommand SaveChangesCommand { get; }
    
    public DelegateCommand DiscardChangesCommand { get; }

    public ConfigurationObject SettingsObject => _configurationService.Configuration;
           
    public ObservableCollection<SettingsCategory> SettingsCategories 
    {
        get => _settingsCategories; 
        set => SetValueAndNotify(ref _settingsCategories, value); 
    } 

    private void LoadSettings()
    {
        if(SettingsObject != null)
        {
            var categories = new List<SettingsCategory>();
            var groupedProperties = SettingsProperty.CreateSettingsProperties(SettingsObject).GroupBy(x => x.Category);
            foreach (var settingsCategory in groupedProperties)
            {
                categories.Add(new SettingsCategory(settingsCategory.Key, settingsCategory.OrderBy(x => x.Order).ThenBy(x => x.DisplayName)));
            }
            SettingsCategories = new ObservableCollection<SettingsCategory>(categories.OrderBy(x => x.Name));
        }
    }

    private void SaveChanges()
    {
        _configurationService.Save();
    }

    private void DiscardChanges()
    {
        _configurationService.Load();
    }

    private bool IsValid()
    {
        if(SettingsObject != null)
        {
            var validationContext = new ValidationContext(SettingsObject);
            return Validator.TryValidateObject(SettingsObject, validationContext, null, true);
        }
        return false;
    }

    private void UpdateButtonState()
    {
        SaveChangesCommand.RaiseCanExecuteChanged();
        DiscardChangesCommand.RaiseCanExecuteChanged();
    }
}
