using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace BiometricFingerprintsAttendanceSystem;

public sealed class ViewLocator : IDataTemplate
{
    public Control Build(object? data)
    {
        if (data is null)
        {
            return new TextBlock { Text = "No view model" };
        }

        var viewModelType = data.GetType();
        var viewTypeName = viewModelType.FullName
            ?.Replace("ViewModels", "Views")
            ?.Replace("ViewModel", "View");

        if (viewTypeName is null)
        {
            return new TextBlock { Text = "View not found" };
        }

        var viewType = Type.GetType(viewTypeName);
        if (viewType is null)
        {
            return new TextBlock { Text = $"View not found: {viewTypeName}" };
        }

        return (Control)Activator.CreateInstance(viewType)!;
    }

    public bool Match(object? data) => data is ViewModels.ViewModelBase;
}
