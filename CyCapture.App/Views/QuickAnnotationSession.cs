using CyCapture.Models;

namespace CyCapture.Views;

internal sealed class QuickAnnotationSession
{
    private readonly List<QuickAnnotationElement> _annotations = [];

    internal bool AnnotationModeArmed { get; private set; }
    internal QuickAnnotationKind Tool { get; private set; } = QuickAnnotationKind.Freehand;
    internal string Color { get; private set; } = "#FF4657";
    internal string Text { get; private set; } = string.Empty;
    internal IReadOnlyList<QuickAnnotationElement> Annotations => _annotations;
    internal int AnnotationCount => _annotations.Count;

    internal event EventHandler? Changed;

    internal void SetAnnotationMode(bool enabled)
    {
        if (AnnotationModeArmed == enabled) return;
        AnnotationModeArmed = enabled;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    internal void SetTool(QuickAnnotationKind tool)
    {
        var changed = Tool != tool || !AnnotationModeArmed;
        Tool = tool;
        AnnotationModeArmed = true;
        if (changed) Changed?.Invoke(this, EventArgs.Empty);
    }

    internal void SetColor(string color)
    {
        if (Color.Equals(color, StringComparison.OrdinalIgnoreCase)) return;
        Color = color;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    internal void SetText(string text)
    {
        if (Text == text) return;
        Text = text;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    internal void Add(QuickAnnotationElement element)
    {
        _annotations.Add(element);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    internal void Undo()
    {
        if (_annotations.Count == 0) return;
        _annotations.RemoveAt(_annotations.Count - 1);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    internal void Clear()
    {
        if (_annotations.Count == 0) return;
        _annotations.Clear();
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
