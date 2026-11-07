using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using CyCapture.Platform.Windows;
using CyCapture.Services;

namespace CyCapture.Views;

internal sealed class UpdateAvailableWindow : Window
{
    internal UpdateAvailableWindow(UpdateCheckResult update)
    {
        Title = UiLocalization.Text("Mise à jour CyCapture", "CyCapture update");
        Width = 460;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        CanResize = false;
        ShowInTaskbar = true;
        Topmost = true;
        WindowsWindowAppearance.Attach(this);

        var accept = new Button
        {
            Content = update.DownloadUrl is null
                ? UiLocalization.Text("Voir la release", "View release")
                : UiLocalization.Text("Obtenir la mise à jour", "Get update"),
            Padding = new Thickness(18, 9)
        };
        accept.Click += (_, _) =>
        {
            Accepted?.Invoke(this, EventArgs.Empty);
            Close();
        };
        var later = new Button
        {
            Content = UiLocalization.Text("Plus tard", "Later"),
            Padding = new Thickness(18, 9)
        };
        later.Click += (_, _) => Close();

        Content = new Border
        {
            Padding = new Thickness(24),
            Background = new SolidColorBrush(Color.FromRgb(10, 15, 15)),
            Child = new StackPanel
            {
                Spacing = 14,
                Children =
                {
                    new TextBlock
                    {
                        Text = UiLocalization.Text("MISE À JOUR DISPONIBLE", "UPDATE AVAILABLE"),
                        FontSize = 10,
                        FontWeight = FontWeight.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(155, 255, 40)),
                        LetterSpacing = 1.4
                    },
                    new TextBlock
                    {
                        Text = $"CyCapture {update.LatestVersion:3}",
                        FontSize = 24,
                        FontWeight = FontWeight.Black
                    },
                    new TextBlock
                    {
                        Text = UiLocalization.Text(
                            $"Vous utilisez la version {AppBuildInfo.DisplayVersion}. Voulez-vous ouvrir la mise à jour maintenant ?",
                            $"You are using version {AppBuildInfo.DisplayVersion}. Do you want to open the update now?"),
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = new SolidColorBrush(Color.FromRgb(180, 192, 189))
                    },
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Spacing = 10,
                        Children = { later, accept }
                    }
                }
            }
        };
    }

    internal event EventHandler? Accepted;
}
