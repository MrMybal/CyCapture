# Vérification des réglages par mode

Depuis la racine de CyCapture, sous Windows avec le SDK .NET 8 :

```powershell
dotnet run --project .\tests\CyCapture.SettingsChecks\CyCapture.SettingsChecks.csproj -c Debug -p:Platform=x64 -- .
```

Le programme vérifie la migration des anciennes préférences, la sérialisation,
l'indépendance des réglages Vidéo/Audio et des cadres Vidéo/GIF, les options
transmises à l'encodeur, le respect du Z-order par la sélection intelligente et
le regroupement des contrôles dans les quatre onglets. Il valide également la
détection des mises à jour, le choix du bon binaire avec/sans CyAnnota, le rejet
des URL non fiables, ainsi que la présence du numéro de version et du bouton de
mise à jour dans les réglages.
Il instancie également les deux fenêtres et génère des aperçus PNG dans
`artifacts/settings-checks/`.

Aucune capture n'est lancée. Les préférences réelles ne sont pas écrites et les
plugins externes ne sont pas chargés. Une fenêtre de test est brièvement créée
hors écran, sans activation ni entrée dans la barre des tâches, pour vérifier
la mise en page Avalonia.
