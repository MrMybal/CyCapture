using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CyCapture.Models;
using CyCapture.Platform.Windows;

namespace CyCapture.Views;

internal sealed class SelectionOverlayWindow : Window
{
    private readonly MonitorDescriptor _monitor;
    private readonly TextBlock _label;

    internal SelectionOverlayWindow(
        Bitmap screenshot,
        byte[]? frozenFramePng,
        MonitorDescriptor monitor,
        IReadOnlyList<SelectableRegion> regions,
        IReadOnlyList<SelectableWindowLayer> windowLayers,
        CaptureSelectionMode selectionMode,
        bool quickAnnotationsEnabled,
        QuickAnnotationSession annotationSession)
    {
        _monitor = monitor;
        Title = $"CyCapture · {SelectionModeLabel(selectionMode)}";
        Width = monitor.Bounds.Width / monitor.Scale;
        Height = monitor.Bounds.Height / monitor.Scale;
        Position = new PixelPoint(monitor.Bounds.X, monitor.Bounds.Y);
        WindowStartupLocation = WindowStartupLocation.Manual;
        WindowDecorations = Avalonia.Controls.WindowDecorations.None;
        CanResize = false;
        ShowInTaskbar = false;
        Topmost = true;
        Background = Brushes.Black;

        var surface = new SelectionSurface(
            screenshot,
            frozenFramePng,
            monitor,
            regions,
            windowLayers,
            selectionMode,
            quickAnnotationsEnabled,
            annotationSession);
        surface.SelectionCompleted += (_, selection) => SelectionCompleted?.Invoke(this, selection);

        _label = new TextBlock
        {
            Foreground = Brushes.White,
            FontSize = 12,
            FontWeight = FontWeight.Bold,
            IsHitTestVisible = false
        };
        var labelBorder = new Border
        {
            Name = "SelectionLabel",
            Background = new SolidColorBrush(Color.FromArgb(238, 8, 14, 14)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(155, 255, 40)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(9, 5),
            Child = _label,
            IsVisible = false,
            IsHitTestVisible = false
        };
        surface.HighlightChanged += (_, highlight) => UpdateLabel(highlight.Bounds, highlight.Label);

        var instructionText = new TextBlock
        {
            Text = SelectionInstruction(selectionMode),
            FontSize = 12,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brushes.White
        };
        var instruction = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 20, 0, 0),
            Padding = new Thickness(16, 9),
            CornerRadius = new CornerRadius(10),
            Background = new SolidColorBrush(Color.FromArgb(238, 8, 14, 14)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(120, 155, 255, 40)),
            BorderThickness = new Thickness(1),
            Child = instructionText,
            IsHitTestVisible = false
        };

        var annotationToolbar = BuildAnnotationToolbar(surface, instructionText, selectionMode);
        annotationToolbar.IsVisible = quickAnnotationsEnabled;
        if (!quickAnnotationsEnabled)
            instructionText.Text = SelectionInstruction(selectionMode);

        var canvas = new Canvas { IsHitTestVisible = false };
        canvas.Children.Add(labelBorder);
        var root = new Grid();
        root.Children.Add(surface);
        root.Children.Add(canvas);
        root.Children.Add(instruction);
        root.Children.Add(annotationToolbar);
        Content = root;

        Opened += (_, _) =>
        {
            NativeMethods.SetWindowDisplayAffinity(TryGetPlatformHandle()?.Handle ?? 0, NativeMethods.WdaExcludeFromCapture);
            surface.Focus();
        };
        KeyDown += (_, args) =>
        {
            if (args.Key == Key.Escape)
            {
                Canceled?.Invoke(this, EventArgs.Empty);
                args.Handled = true;
            }
            else if (args.Key == Key.Z && args.KeyModifiers.HasFlag(KeyModifiers.Control) && surface.AnnotationCount > 0)
            {
                surface.UndoAnnotation();
                args.Handled = true;
            }
        };

        void UpdateLabel(PixelBounds? bounds, string text)
        {
            if (bounds is null)
            {
                labelBorder.IsVisible = false;
                return;
            }

            var localX = (bounds.X - _monitor.Bounds.X) / _monitor.Scale;
            var localY = (bounds.Y - _monitor.Bounds.Y) / _monitor.Scale;
            _label.Text = $"{text}  ·  {bounds.Width} × {bounds.Height}";
            Canvas.SetLeft(labelBorder, Math.Clamp(localX, 8, Math.Max(8, Width - 380)));
            Canvas.SetTop(labelBorder, localY < 52 ? localY + 8 : localY - 38);
            labelBorder.IsVisible = true;
        }
    }

    internal event EventHandler<CaptureSelection>? SelectionCompleted;
    internal event EventHandler? Canceled;

    private static Border BuildAnnotationToolbar(
        SelectionSurface surface,
        TextBlock instructionText,
        CaptureSelectionMode selectionMode)
    {
        var annotationText = new TextBox
        {
            Width = 140,
            Height = 32,
            PlaceholderText = "Texte à ajouter…",
            VerticalContentAlignment = VerticalAlignment.Center,
            IsVisible = false
        };

        var tools = new Dictionary<QuickAnnotationKind, Button>();
        Button CompactButton(string label, string tooltip)
        {
            var button = new Button
            {
                Content = label,
                MinWidth = 32,
                Height = 32,
                Padding = new Thickness(8, 3),
                FontWeight = FontWeight.SemiBold
            };
            ToolTip.SetTip(button, tooltip);
            return button;
        }

        Button ToolButton(string label, string tooltip, QuickAnnotationKind tool)
        {
            var button = CompactButton(label, tooltip);
            button.Click += (_, _) => SelectTool(tool);
            tools[tool] = button;
            return button;
        }

        var directButton = CompactButton("⌖", "Mode sélection : choisissez ensuite ce que vous voulez capturer");
        directButton.Click += (_, _) => SelectDirectCapture();
        var freehandButton = ToolButton("✎", "Dessin libre", QuickAnnotationKind.Freehand);
        var rectangleButton = ToolButton("□", "Cadre", QuickAnnotationKind.Rectangle);
        var arrowButton = ToolButton("→", "Flèche", QuickAnnotationKind.Arrow);
        var textButton = ToolButton("T", "Texte", QuickAnnotationKind.Text);

        var colorRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center
        };
        var colors = new Dictionary<string, Button>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in new[] { "#FF4657", "#9BFF28", "#FFD84A", "#FFFFFF", "#6CE5FF" })
        {
            var colorButton = new Button
            {
                Width = 20,
                Height = 20,
                Padding = new Thickness(0),
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(Color.Parse(value)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(70, 82, 80)),
                BorderThickness = new Thickness(1)
            };
            colorButton.Click += (_, _) => SelectColor(value);
            colors[value] = colorButton;
            colorRow.Children.Add(colorButton);
        }

        var undoButton = CompactButton("↶", "Annuler la dernière annotation (Ctrl+Z)");
        undoButton.Click += (_, _) => surface.UndoAnnotation();
        var clearButton = CompactButton("×", "Effacer toutes les annotations");
        clearButton.Click += (_, _) => surface.ClearAnnotations();

        var separator = new Border
        {
            Width = 1,
            Height = 24,
            Margin = new Thickness(2, 0),
            Background = new SolidColorBrush(Color.FromRgb(61, 79, 75)),
            VerticalAlignment = VerticalAlignment.Center
        };

        var content = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 5,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                directButton,
                separator,
                freehandButton,
                rectangleButton,
                arrowButton,
                textButton,
                annotationText,
                colorRow,
                undoButton,
                clearButton
            }
        };
        var toolbar = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(8),
            Padding = new Thickness(6),
            CornerRadius = new CornerRadius(9),
            Background = new SolidColorBrush(Color.FromArgb(226, 8, 14, 14)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(61, 79, 75)),
            BorderThickness = new Thickness(1),
            Child = content
        };

        annotationText.TextChanged += (_, _) => surface.AnnotationText = annotationText.Text ?? string.Empty;
        surface.AnnotationHint += (_, message) => instructionText.Text = message;
        surface.AnnotationStateChanged += (_, _) => RefreshToolbarState();

        RefreshToolbarState();
        return toolbar;

        void SelectTool(QuickAnnotationKind tool)
        {
            surface.AnnotationTool = tool;
            if (tool == QuickAnnotationKind.Text)
                annotationText.Focus();
            else
                surface.Focus();
        }

        void SelectDirectCapture()
        {
            surface.AnnotationModeArmed = false;
            surface.Focus();
        }

        void SelectColor(string color)
        {
            surface.AnnotationColor = color;
            if (surface.AnnotationTool == QuickAnnotationKind.Text && surface.AnnotationModeArmed)
                annotationText.Focus();
            else
                surface.Focus();
        }

        void RefreshToolbarState()
        {
            var annotationMode = surface.AnnotationModeArmed;
            foreach (var item in tools)
            {
                var active = annotationMode && item.Key == surface.AnnotationTool;
                item.Value.Background = new SolidColorBrush(active ? Color.FromRgb(61, 86, 67) : Color.FromRgb(36, 44, 43));
                item.Value.BorderBrush = new SolidColorBrush(active ? Color.FromRgb(155, 255, 40) : Color.FromRgb(57, 70, 68));
                item.Value.BorderThickness = new Thickness(1);
            }

            directButton.Background = new SolidColorBrush(annotationMode ? Color.FromRgb(36, 44, 43) : Color.FromRgb(61, 86, 67));
            directButton.BorderBrush = new SolidColorBrush(annotationMode ? Color.FromRgb(57, 70, 68) : Color.FromRgb(155, 255, 40));
            directButton.BorderThickness = new Thickness(1);
            annotationText.IsVisible = annotationMode && surface.AnnotationTool == QuickAnnotationKind.Text;
            if (annotationText.Text != surface.AnnotationText)
                annotationText.Text = surface.AnnotationText;

            foreach (var item in colors)
            {
                var active = item.Key.Equals(surface.AnnotationColor, StringComparison.OrdinalIgnoreCase);
                item.Value.BorderBrush = new SolidColorBrush(active ? Colors.White : Color.FromRgb(70, 82, 80));
                item.Value.BorderThickness = new Thickness(active ? 3 : 1);
            }

            instructionText.Text = annotationMode
                ? surface.AnnotationCount switch
                {
                    0 => "Mode annotation multi-écrans · Dessinez, puis revenez sur ⌖",
                    1 => "1 annotation · Revenez sur ⌖ pour sélectionner la capture",
                    _ => $"{surface.AnnotationCount} annotations · Revenez sur ⌖ pour sélectionner la capture"
                }
                : surface.AnnotationCount switch
                {
                    0 => DirectCaptureInstruction(selectionMode),
                    1 => "Mode sélection · 1 annotation sera incluse dans la capture",
                    _ => $"Mode sélection · {surface.AnnotationCount} annotations seront incluses dans la capture"
                };
        }
    }

    private static string SelectionModeLabel(CaptureSelectionMode mode) => mode switch
    {
        CaptureSelectionMode.Window => "Sélection de fenêtre",
        CaptureSelectionMode.Screen => "Sélection d’écran",
        CaptureSelectionMode.Region => "Zone libre",
        _ => "Sélection intelligente"
    };

    private static string SelectionInstruction(CaptureSelectionMode mode) => mode switch
    {
        CaptureSelectionMode.Window => "Cliquez la fenêtre à capturer · Échap pour annuler",
        CaptureSelectionMode.Screen => "Cliquez l’écran à capturer · Échap pour annuler",
        CaptureSelectionMode.Region => "Glissez pour dessiner la zone à capturer · Échap pour annuler",
        _ => "Cliquez une fenêtre ou un contenu · Glissez pour une zone libre · Échap pour annuler"
    };

    private static string DirectCaptureInstruction(CaptureSelectionMode mode) =>
        $"{SelectionInstruction(mode)} · Outil ✓ = capture directe";
}
