using ContainerDesktop.Abstractions;
using ContainerDesktop.Services;
using ContainerDesktop.UI.Wpf.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ContainerDesktop.ViewModels;

public class SettingsViewModel : NotifyObject
{
    private readonly IConfigurationService _configurationService;
    private IMenuItem _selectedMenuItem;
    private ObservableCollection<SettingsCategory> _settingsCategories;

    public SettingsViewModel(IConfigurationService configurationService)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        //MenuItems.Add(new Category 
        //{ 
        //    Name = "General", 
        //    Tooltip = "General settings", 
        //    Glyph = Symbol.Setting, 
        //    SettingsObject = configurationService.Configuration 
        //});
        SaveChangesCommand = new DelegateCommand(SaveChanges, IsValid);
        DiscardChangesCommand = new DelegateCommand(DiscardChanges);
        //SelectedMenuItem = MenuItems[0];
        LoadSettings();
    }

    public DelegateCommand SaveChangesCommand { get; }
    
    public DelegateCommand DiscardChangesCommand { get; }

    public ConfigurationObject SettingsObject => _configurationService.Configuration;
    
    //public ObservableCollection<IMenuItem> MenuItems { get; } = new ObservableCollection<IMenuItem>();
        
    public ObservableCollection<SettingsCategory> SettingsCategories 
    {
        get => _settingsCategories; 
        set => SetValueAndNotify(ref _settingsCategories, value); 
    } 

    //public IMenuItem SelectedMenuItem
    //{
    //    get => _selectedMenuItem;
    //    set
    //    {
    //        var oldValue = _selectedMenuItem;
    //        if (SetValueAndNotify(ref _selectedMenuItem, value))
    //        {
    //            if(oldValue is Category category && category.SettingsObject is INotifyPropertyChanged npc)
    //            {
    //                npc.PropertyChanged -= OnSelectedSettingsObjectPropertyChanged;
    //            }
    //            if (value is Category category2 && category2.SettingsObject is INotifyPropertyChanged npc2)
    //            {
    //                npc2.PropertyChanged += OnSelectedSettingsObjectPropertyChanged;
    //            }
    //            LoadSettings(value);
    //        }
    //    }
    //}

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

    protected void OnSelectedSettingsObjectPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        SaveChangesCommand.RaiseCanExecuteChanged();
    }
}
