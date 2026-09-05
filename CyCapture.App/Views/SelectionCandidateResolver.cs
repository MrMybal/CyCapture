using CyCapture.Models;

namespace CyCapture.Views;

internal static class SelectionCandidateResolver
{
    internal static SelectableRegion? Resolve(
        IReadOnlyList<SelectableRegion> regions,
        IReadOnlyList<SelectableWindowLayer> windowLayers,
        int screenX,
        int screenY,
        CaptureSelectionMode selectionMode,
        IReadOnlyList<nint>? childWindowPath = null)
    {
        if (selectionMode is not (CaptureSelectionMode.Smart or CaptureSelectionMode.Window)) return null;

        var frontWindow = windowLayers
            .Where(item => item.Bounds.Contains(screenX, screenY))
            .MinBy(item => item.ZOrder);
        if (frontWindow is null) return null;

        var matches = regions
            .Where(item => item.RootWindowHandle == frontWindow.Handle)
            .Where(item => item.Bounds.Contains(screenX, screenY))
            .ToList();

        if (selectionMode == CaptureSelectionMode.Window)
            return matches.FirstOrDefault(item => item.Kind == SelectionKind.Window);

        if (childWindowPath is not null)
        {
            for (var index = childWindowPath.Count - 1; index >= 0; index--)
            {
                var childHandle = childWindowPath[index];
                var control = matches
                    .Where(item => item.Kind == SelectionKind.Control && item.Handle == childHandle)
                    .OrderByDescending(item => item.Priority)
                    .ThenBy(item => item.Bounds.Area)
                    .FirstOrDefault();
                if (control is not null) return control;
            }
        }

        return matches.FirstOrDefault(item => item.Kind == SelectionKind.Client)
               ?? matches.FirstOrDefault(item => item.Kind == SelectionKind.Window);
    }
}
