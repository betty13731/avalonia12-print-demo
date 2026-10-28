using Avalonia.Controls;
using Avalonia.Platform.Storage;
using AvaloniaNativePrintDemo.Models;
using AvaloniaNativePrintDemo.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AvaloniaNativePrintDemo.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public ObservableCollection<PrinterInfo> Printers { get; } = new();

    [ObservableProperty]
    private PrinterInfo? selectedPrinter;

    [ObservableProperty]
    private string? filePath;

    [ObservableProperty]
    private string status = "Готово";

    public MainWindowViewModel()
    {
        RefreshPrinters();
    }

    [RelayCommand]
    private void RefreshPrinters()
    {
        try
        {
            Printers.Clear();
            foreach (var p in PrintingService.GetPrinters())
                Printers.Add(p);

            SelectedPrinter = Printers.FirstOrDefault(x => x.IsDefault) ?? Printers.FirstOrDefault();
            Status = Printers.Count == 0
                ? "Принтеры не найдены"
                : $"Найдено принтеров: {Printers.Count}";

            PrintCommand.NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            Status = $"Ошибка получения принтеров: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task PickFileAsync()
    {
        try
        {
            var top = App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime d
                ? d.MainWindow
                : null;

            if (top is null)
            {
                Status = "Не удалось получить главное окно";
                return;
            }

            var files = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Выберите PDF файл",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("PDF") { Patterns = new[] { "*.pdf" } }
                }
            });

            var file = files.FirstOrDefault();
            if (file is null)
            {
                Status = "Выбор файла отменён";
                return;
            }

            FilePath = file.Path.LocalPath;
            Status = "Файл выбран";
        }
        catch (Exception ex)
        {
            Status = $"Ошибка выбора файла: {ex.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanPrint))]
    private async Task PrintAsync()
    {
        try
        {
            Status = "Отправка в печать...";
            await Task.Yield();

            PrintingService.PrintFile(FilePath!, SelectedPrinter?.Name);
            Status = "Задание отправлено в очередь печати";
        }
        catch (Exception ex)
        {
            Status = $"Ошибка печати: {ex.Message}";
        }
    }

    private bool CanPrint()
        => !string.IsNullOrWhiteSpace(FilePath) && File.Exists(FilePath);

    partial void OnFilePathChanged(string? value)
    {
        PrintCommand.NotifyCanExecuteChanged();
    }
}
