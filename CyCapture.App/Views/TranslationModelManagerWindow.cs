using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using CyCapture.Models;
using CyCapture.Services;

namespace CyCapture.Views;

internal sealed class TranslationModelManagerWindow : Window
{
    private readonly Preferences _preferences;
    private readonly TranslationModelService _models;
    private readonly ComboBox _target;
    private readonly StackPanel _languagesPanel;
    private readonly TextBlock _status;
    private readonly TextBlock _installedDirections;
    private readonly ProgressBar _progress;
    private readonly Button _download;
    private readonly CancellationTokenSource _cancellation = new();
    private bool _working;

    internal TranslationModelManagerWindow(Preferences preferences, TranslationModelService models)
    {
        _preferences = preferences;
        _models = models;
        Title = UiLocalization.Text("CyCapture · Modèles de traduction", "CyCapture · Translation models");
        Width = 650;
        Height = 690;
        MinWidth = 520;
        MinHeight = 520;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.FromRgb(8, 15, 14));
        _target = new ComboBox { MinWidth = 210 };
        _target.ItemsSource = TranslationLanguageCatalog.Languages;
        _target.SelectedItem = TranslationLanguageCatalog.Find(preferences.TranslationTargetLanguage)
                               ?? TranslationLanguageCatalog.Find("fr");
        _languagesPanel = new StackPanel { Spacing = 5 };
        _status = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.FromRgb(157, 174, 170))
        };
        _installedDirections = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.FromRgb(127, 141, 138)),
            FontSize = 11
        };
        _progress = new ProgressBar { Height = 7, IsVisible = false };
        _download = new Button
        {
            Content = UiLocalization.Text("Télécharger les langues cochées", "Download selected languages"),
            Padding = new Thickness(16, 8),
            Margin = new Thickness(0, 0, 8, 0),
            Foreground = Brushes.White,
            Background = new SolidColorBrush(Color.FromRgb(45, 54, 53))
        };
        _download.Click += DownloadClick;
        var openFolder = new Button
        {
            Content = UiLocalization.Text("Ouvrir le dossier des modèles", "Open models folder"),
            Padding = new Thickness(16, 8),
            Margin = new Thickness(0, 0, 8, 0),
            Foreground = Brushes.White,
            Background = new SolidColorBrush(Color.FromRgb(45, 54, 53))
        };
        openFolder.Click += (_, _) =>
        {
            Directory.CreateDirectory(TranslationModelService.ModelsDirectory);
            Process.Start(new ProcessStartInfo("explorer.exe", TranslationModelService.ModelsDirectory) { UseShellExecute = true });
        };
        var close = new Button
        {
            Content = UiLocalization.Text("Fermer", "Close"),
            Padding = new Thickness(16, 8),
            Foreground = Brushes.White,
            Background = new SolidColorBrush(Color.FromRgb(45, 54, 53))
        };
        close.Click += (_, _) => Close();

        Content = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,Auto,*,Auto"),
            Children =
            {
                new Border
                {
                    Padding = new Thickness(22, 20, 22, 14),
                    Child = new StackPanel
                    {
                        Spacing = 6,
                        Children =
                        {
                            new TextBlock
                            {
                                Text = UiLocalization.Text("MODÈLES OCR ET TRADUCTION", "OCR AND TRANSLATION MODELS"),
                                FontSize = 11,
                                FontWeight = FontWeight.Bold,
                                Foreground = new SolidColorBrush(Color.FromRgb(155, 255, 40)),
                                LetterSpacing = 1.3
                            },
                            new TextBlock
                            {
                                Text = UiLocalization.Text(
                                    "Cochez autant de langues sources que nécessaire. Les modèles sont téléchargés une seule fois, puis tout fonctionne hors ligne.",
                                    "Select as many source languages as needed. Models are downloaded once, then everything works offline."),
                                TextWrapping = TextWrapping.Wrap,
                                Foreground = new SolidColorBrush(Color.FromRgb(157, 174, 170))
                            }
                        }
                    }
                },
                BuildTargetRow(),
                new Border
                {
                    [Grid.RowProperty] = 2,
                    Margin = new Thickness(22, 8),
                    Padding = new Thickness(12),
                    CornerRadius = new CornerRadius(10),
                    Background = new SolidColorBrush(Color.FromRgb(16, 23, 23)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(38, 51, 49)),
                    BorderThickness = new Thickness(1),
                    Child = new ScrollViewer { Content = _languagesPanel }
                },
                new Border
                {
                    [Grid.RowProperty] = 3,
                    Padding = new Thickness(22, 12, 22, 20),
                    Child = new StackPanel
                    {
                        Spacing = 10,
                        Children =
                        {
                            _installedDirections,
                            _status,
                            _progress,
                            new WrapPanel
                            {
                                Orientation = Orientation.Horizontal,
                                Children = { _download, openFolder, close }
                            }
                        }
                    }
                }
            }
        };
        PopulateLanguages();
        RefreshInstalledState();
        Closed += (_, _) =>
        {
            _cancellation.Cancel();
            _cancellation.Dispose();
        };
    }

    private Border BuildTargetRow()
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        grid.Children.Add(new StackPanel
        {
            Spacing = 4,
            Children =
            {
                new TextBlock
                {
                    Text = UiLocalization.Text("LANGUE DE DESTINATION", "TARGET LANGUAGE"),
                    FontSize = 10,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(180, 192, 189))
                },
                new TextBlock
                {
                    Text = UiLocalization.Text("Les langues cochées seront préparées vers cette langue.", "Selected source languages will be prepared for this target."),
                    Foreground = new SolidColorBrush(Color.FromRgb(127, 141, 138)),
                    FontSize = 11
                }
            }
        });
        Grid.SetColumn(_target, 1);
        grid.Children.Add(_target);
        return new Border { [Grid.RowProperty] = 1, Padding = new Thickness(22, 8), Child = grid };
    }

    private void PopulateLanguages()
    {
        _languagesPanel.Children.Clear();
        foreach (var language in TranslationLanguageCatalog.Languages)
        {
            var check = new CheckBox
            {
                Tag = language.Code,
                Content = language.DisplayName,
                Foreground = Brushes.White,
                IsChecked = language.Code.Equals(_preferences.TranslationSourceLanguage, StringComparison.OrdinalIgnoreCase)
                            || (_preferences.TranslationSourceLanguage == TranslationLanguageCatalog.AutomaticCode
                                && language.Code is "zh" or "zh_hant" or "en" or "ja" or "ko")
            };
            _languagesPanel.Children.Add(check);
        }
    }

    private async void DownloadClick(object? sender, Avalonia.Interactivity.RoutedEventArgs args)
    {
        if (_working || _target.SelectedItem is not TranslationLanguage target) return;
        var selected = _languagesPanel.Children.OfType<CheckBox>()
            .Where(check => check.IsChecked == true)
            .Select(check => TranslationLanguageCatalog.Find(check.Tag?.ToString()))
            .OfType<TranslationLanguage>()
            .ToArray();
        if (selected.Length == 0)
        {
            SetStatus(UiLocalization.Text("Cochez au moins une langue source.", "Select at least one source language."), true);
            return;
        }

        _working = true;
        _download.IsEnabled = false;
        _progress.IsVisible = true;
        _progress.IsIndeterminate = true;
        var reporter = new Progress<TranslationProgress>(ReportProgress);
        try
        {
            foreach (var language in selected)
            {
                _cancellation.Token.ThrowIfCancellationRequested();
                await _models.DownloadLanguagePairAsync(language.Code, target.Code, reporter, _cancellation.Token);
            }
            _preferences.TranslationTargetLanguage = target.Code;
            if (selected.Length == 1) _preferences.TranslationSourceLanguage = selected[0].Code;
            await _preferences.SaveAsync();
            SetStatus(UiLocalization.Text(
                $"{selected.Length} langue(s) prête(s). La traduction est maintenant disponible hors ligne.",
                $"{selected.Length} language(s) ready. Translation is now available offline."));
            RefreshInstalledState();
        }
        catch (OperationCanceledException)
        {
            SetStatus(UiLocalization.Text("Téléchargement annulé.", "Download canceled."), true);
        }
        catch (Exception error)
        {
            SetStatus(error.Message, true);
        }
        finally
        {
            _working = false;
            _download.IsEnabled = true;
            _progress.IsVisible = false;
        }
    }

    private void ReportProgress(TranslationProgress value)
    {
        if (!string.IsNullOrWhiteSpace(value.Message)) SetStatus(value.Message);
        if (value.Fraction is { } fraction)
        {
            _progress.IsIndeterminate = false;
            _progress.Minimum = 0;
            _progress.Maximum = 1;
            _progress.Value = Math.Clamp(fraction, 0, 1);
        }
        else
        {
            _progress.IsIndeterminate = true;
        }
    }

    private void RefreshInstalledState()
    {
        foreach (var check in _languagesPanel.Children.OfType<CheckBox>())
        {
            var code = check.Tag?.ToString() ?? string.Empty;
            var language = TranslationLanguageCatalog.Find(code);
            if (language is null) continue;
            check.Content = _models.IsOcrInstalled(code) ? $"✓ {language.DisplayName}" : language.DisplayName;
        }
        var directions = _models.InstalledDirections();
        _installedDirections.Text = directions.Count == 0
            ? UiLocalization.Text("Aucun modèle de traduction installé.", "No translation model installed.")
            : UiLocalization.Text(
                $"Directions installées : {string.Join(", ", directions)}",
                $"Installed directions: {string.Join(", ", directions)}");
    }

    private void SetStatus(string text, bool error = false)
    {
        _status.Text = text;
        _status.Foreground = new SolidColorBrush(error
            ? Color.FromRgb(255, 101, 115)
            : Color.FromRgb(157, 174, 170));
    }
}
