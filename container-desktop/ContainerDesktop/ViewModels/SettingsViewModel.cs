using ContainerDesktop.Abstractions;
using ContainerDesktop.Configuration;
using ContainerDesktop.UI.Wpf.Input;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace ContainerDesktop.ViewModels;

public class SettingsViewModel : NotifyObject
{
    private readonly IConfigurationService _configurationService;
    private ObservableCollection<SettingsCategory> _settingsCategories;
    private readonly Dictionary<string, ObservableCollection<SettingsCategory>> _settingsCategoriesMap;
    private string _selectedCategory;

    public SettingsViewModel(IConfigurationService configurationService)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        SaveChangesCommand = new DelegateCommand(SaveChanges, () => IsValid() && _configurationService.IsChanged());
        DiscardChangesCommand = new DelegateCommand(DiscardChanges, () => _configurationService.IsChanged());
        _configurationService.Configuration.PropertyChanged += (_,_) => UpdateButtonState();
        ConfigurationChangedEventManager.AddHandler(_configurationService, (_,_) => UpdateButtonState());
        Categories = ConfigurationCategories.All;
        _settingsCategoriesMap = LoadSettings();
        _selectedCategory = Categories.First();
        _settingsCategories = _settingsCategoriesMap[_selectedCategory];
    }

    public DelegateCommand SaveChangesCommand { get; }
    
    public DelegateCommand DiscardChangesCommand { get; }

    public IConfigurationObject SettingsObject => _configurationService.Configuration;

    public IReadOnlyCollection<string> Categories { get; set; }

    public string SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetValueAndNotify(ref _selectedCategory, value))
            {
                SettingsCategories = _settingsCategoriesMap[_selectedCategory];
            }
        }
    }

    public ObservableCollection<SettingsCategory> SettingsCategories 
    {
        get => _settingsCategories; 
        set => SetValueAndNotify(ref _settingsCategories, value); 
    } 

    private Dictionary<string, ObservableCollection<SettingsCategory>> LoadSettings()
    {
        var ret = new Dictionary<string, ObservableCollection<SettingsCategory>>();
        foreach (var categoryName in ConfigurationCategories.All)
        {
            var groupedByGroupName = SettingsProperty.CreateSettingsProperties(SettingsObject).Where(x => x.Category == categoryName).GroupBy(x => x.GroupName);
            var groups = new List<SettingsCategory>();
            foreach (var group in groupedByGroupName)
            {
                groups.Add(new SettingsCategory(group.Key, group.OrderBy(x => x.Order).ThenBy(x => x.DisplayName)));
            }
            ret.Add(categoryName, new ObservableCollection<SettingsCategory>(groups));
        }
        return ret;
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
