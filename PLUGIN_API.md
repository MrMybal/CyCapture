# API de plugins CyCapture

CyCapture découvre les DLL placées dans `%LOCALAPPDATA%\CyCapture\Plugins`. Un post-traitement implémente `ICapturePostProcessor`. Il peut aussi implémenter `ICapturePluginMetadata` pour exposer une description, des réglages et une option globale dans l’interface.

```csharp
using CyCapture.Models;
using CyCapture.Services;

public sealed class ExamplePostProcessor : ICapturePostProcessor, ICapturePluginMetadata
{
    private string _destination = string.Empty;

    public string Id => "example-post-edit";
    public string Name => "Example Post Edit";
    public string Description => "Envoie la capture vers un outil de post-édition.";
    public bool EnabledByDefault => false;

    // Une valeur non vide ajoute automatiquement cette case au tray et à la palette rapide.
    public string? QuickAccessLabel => "PostEdit with Example";

    public IReadOnlyList<CapturePluginSettingDefinition> Settings =>
    [
        new("destination", "Dossier de destination", CapturePluginSettingKind.Text),
        new("openAfter", "Ouvrir après traitement", CapturePluginSettingKind.Boolean, "true"),
        new("profile", "Profil", CapturePluginSettingKind.Choice, "Standard", ["Rapide", "Standard", "Qualité"])
    ];

    public void ApplySettings(IReadOnlyDictionary<string, string> values)
    {
        _destination = values.GetValueOrDefault("destination", string.Empty);
    }

    public ValueTask ProcessAsync(CaptureArtifact artifact, CancellationToken cancellationToken)
    {
        // Le plugin est appelé uniquement lorsqu’il est activé.
        return ValueTask.CompletedTask;
    }
}
```

## Comportement de l’hôte

- l’état activé et les valeurs sont stockés dans `%LOCALAPPDATA%\CyCapture\plugin-settings.json` ;
- les réglages `Text`, `Boolean` et `Choice` sont générés automatiquement dans la fenêtre Réglages ;
- `QuickAccessLabel` ajoute une case synchronisée dans le menu tray et la palette Image/Vidéo/GIF/Audio ;
- `CaptureArtifact.Mode` indique `Image`, `Video`, `Gif` ou `Audio` ; un plugin peut ignorer explicitement les formats qu’il ne sait pas traiter ;
- une exception de chargement ou de configuration désactive uniquement le plugin concerné et est écrite dans `Plugins\plugin-errors.log` ;
- une exception pendant le post-traitement n’annule pas la capture et est écrite dans `cycapture-plugin-errors.log` à côté du média.

Cette API reste générique. L’intégration fournie avec CyAnnota est un post-processeur CyCapture isolé qui précharge la distribution Windows embarquée, attend sa première fenêtre si nécessaire, puis lui transmet directement le chemin du média terminé. Elle n’utilise pas le protocole `cyannota://`, ne charge aucune DLL et ne modifie aucun fichier source du projet CyAnnota.
