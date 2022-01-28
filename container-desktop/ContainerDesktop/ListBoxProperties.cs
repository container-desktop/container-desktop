using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace ContainerDesktop;

public class ListBoxProperties : DependencyObject
{
    private static readonly Dictionary<ListBox, bool> _isUpdating = new();
    public static readonly DependencyProperty SelectedItemsProperty = DependencyProperty.RegisterAttached("SelectedItems", typeof(object), typeof(ListBoxProperties), new PropertyMetadata(SelectedItemsPropertyChanged));
    public static readonly DependencyProperty IsAttachedProperty = DependencyProperty.RegisterAttached("IsAttached", typeof(bool), typeof(ListBoxProperties), new PropertyMetadata { DefaultValue = false, PropertyChangedCallback = IsAttachedPropertyChanged });
    
    public static object GetSeletedItems(DependencyObject d) => d.GetValue(SelectedItemsProperty);

    public static void SetSelectedItems(DependencyObject d, object value) => d.SetValue(SelectedItemsProperty, value);

    public static bool GetIsAttached(DependencyObject d) => (bool)d.GetValue(IsAttachedProperty);
    
    public static void SetIsAttached(DependencyObject d, bool value) => d.SetValue(IsAttachedProperty, value);

    private static void IsAttachedPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ListBox lb)
        {
            if ((bool)e.NewValue)
            {
                lb.SelectionChanged += SelectionChanged;
                _isUpdating[lb] = false;
            }
            else
            {
                lb.SelectionChanged -= SelectionChanged;
                _isUpdating.Remove(lb);
            }
        }
    }

    private static void SelectedItemsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ListBox lb && e.NewValue is IEnumerable l)
        {
            try
            {
                _isUpdating[lb] = true;
                foreach (var item in l)
                {
                    lb.SelectedItems.Add(item);
                }
            }
            finally
            {
                _isUpdating[lb] = false;
            }
        }
    }

    private static void SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if(sender is ListBox lb && !_isUpdating[lb])
        {
            var selectedItems = GetSeletedItems(lb);
            if (selectedItems != null)
            {
                var addMethod = selectedItems.GetType().GetMethod("Add");
                var removeMethod = selectedItems.GetType().GetMethod("Remove");
                if (addMethod != null)
                {
                    foreach (var addedItem in e.AddedItems)
                    {
                        addMethod.Invoke(selectedItems, new[] { addedItem });
                    }
                }
                if(removeMethod != null)
                { 
                    foreach (var removedItem in e.RemovedItems)
                    {
                        removeMethod.Invoke(selectedItems, new[] { removedItem });
                    }
                }
            }
        }
    }
}
