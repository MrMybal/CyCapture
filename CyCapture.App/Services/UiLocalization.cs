using System.Globalization;
using Avalonia.Controls;
using Avalonia.LogicalTree;

namespace CyCapture.Services;

internal static class UiLocalization
{
    private static readonly IReadOnlyDictionary<string, string> FrenchToEnglish = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["CyCapture · Réglages"] = "CyCapture · Settings",
        ["CyCapture · Capture rapide"] = "CyCapture · Quick capture",
        ["CyCapture · Galerie"] = "CyCapture · Gallery",
        ["MISES À JOUR"] = "UPDATES",
        ["Vérifiez si une nouvelle version de CyCapture est disponible."] = "Check whether a new version of CyCapture is available.",
        ["Vérifier les mises à jour"] = "Check for updates",
        ["LANGUE DE L’APPLICATION"] = "APPLICATION LANGUAGE",
        ["Choisissez la langue de l’interface de CyCapture. Ce réglage est indépendant de la documentation."] = "Choose the CyCapture interface language. This setting is independent from the documentation.",
        ["LANGUE DE LA DOCUMENTATION"] = "DOCUMENTATION LANGUAGE",
        ["Guide CyDoc local en anglais et français, avec recherche, médias, schémas et dépannage."] = "Local CyDoc guide in English and French, with search, media, diagrams and troubleshooting.",
        ["Documentation / Aide"] = "Documentation / Help",
        ["RÉGLAGES PAR MODE"] = "SETTINGS BY MODE",
        ["Chaque onglet regroupe uniquement les options du type de capture indiqué."] = "Each tab contains only the options for its capture type.",
        ["Vidéo"] = "Video",
        ["IMAGE"] = "IMAGE",
        ["FORMAT"] = "FORMAT",
        ["PNG · sans perte"] = "PNG · lossless",
        ["JPEG · fichier réduit"] = "JPEG · smaller file",
        ["QUALITÉ JPEG"] = "JPEG QUALITY",
        ["Compacte · 40 %"] = "Compact · 40%",
        ["Équilibrée · 70 %"] = "Balanced · 70%",
        ["Haute · 90 %"] = "High · 90%",
        ["Le PNG est sans perte. Le réglage de qualité s’applique uniquement au JPEG."] = "PNG is lossless. The quality setting only applies to JPEG.",
        ["Copier les images dans le presse-papiers"] = "Copy images to the clipboard",
        ["Afficher les outils d’annotation rapide"] = "Show quick annotation tools",
        ["Dessinez sur l’écran figé, ajoutez des cadres, flèches ou textes, puis repassez en sélection pour capturer."] = "Draw on the frozen screen, add boxes, arrows or text, then switch back to selection to capture.",
        ["VIDÉO · MP4 / H.264"] = "VIDEO · MP4 / H.264",
        ["FLUIDITÉ"] = "FRAME RATE",
        ["24 images/s"] = "24 fps",
        ["30 images/s"] = "30 fps",
        ["60 images/s"] = "60 fps",
        ["DÉBIT VIDÉO"] = "VIDEO BITRATE",
        ["Très faible · 1 Mbit/s"] = "Very low · 1 Mbps",
        ["Compact · 2,5 Mbit/s"] = "Compact · 2.5 Mbps",
        ["Équilibré · 6 Mbit/s"] = "Balanced · 6 Mbps",
        ["Haute qualité · 12 Mbit/s"] = "High quality · 12 Mbps",
        ["SON DE LA VIDÉO"] = "VIDEO AUDIO",
        ["Capturer le son du système dans la vidéo"] = "Record system audio in the video",
        ["Ajouter le microphone à la vidéo"] = "Add microphone audio to the video",
        ["ENCODAGE DU SON"] = "AUDIO ENCODING",
        ["Compact · 96 kbit/s mono"] = "Compact · 96 kbps mono",
        ["Équilibré · 128 kbit/s stéréo"] = "Balanced · 128 kbps stereo",
        ["Haute qualité · 192 kbit/s stéréo"] = "High quality · 192 kbps stereo",
        ["Sans source cochée, la vidéo est silencieuse. Ces options ne modifient pas le mode Audio seul."] = "Without a selected source, the video is silent. These options do not change Audio-only mode.",
        ["Copier les vidéos dans le presse-papiers comme fichiers"] = "Copy videos to the clipboard as files",
        ["Afficher le cadre rouge pendant la vidéo"] = "Show the red frame while recording video",
        ["GIF · ANIMATION SANS SON"] = "GIF · SILENT ANIMATION",
        ["QUALITÉ ET FLUIDITÉ"] = "QUALITY AND FRAME RATE",
        ["Compacte · 8 i/s · 720 px max."] = "Compact · 8 fps · 720 px max.",
        ["Équilibrée · 10 i/s · 960 px max."] = "Balanced · 10 fps · 960 px max.",
        ["Haute · 12 i/s · 1280 px max."] = "High · 12 fps · 1280 px max.",
        ["Le profil règle la fluidité et la dimension maximale de l’animation. Les options vidéo et audio ne s’appliquent pas au GIF."] = "The profile controls frame rate and maximum animation size. Video and audio options do not apply to GIFs.",
        ["Afficher le cadre rouge pendant le GIF"] = "Show the red frame while recording a GIF",
        ["Copier les GIF dans le presse-papiers comme fichiers"] = "Copy GIFs to the clipboard as files",
        ["AUDIO SEUL · SANS IMAGE"] = "AUDIO ONLY · NO IMAGE",
        ["Ces réglages concernent uniquement le mode Audio. Le son des vidéos se règle dans l’onglet Vidéo."] = "These settings apply only to Audio mode. Video audio is configured in the Video tab.",
        ["Enregistrer le son du système"] = "Record system audio",
        ["Enregistrer le microphone"] = "Record the microphone",
        ["Ignorer le volume Windows et ajuster automatiquement le niveau enregistré"] = "Ignore Windows volume and adjust the recorded level automatically",
        ["Cette option amplifie uniquement la piste système dans le fichier final, sans modifier le volume réellement entendu. Une source Windows totalement muette ne produit aucun signal."] = "This option only boosts the system track in the final file without changing the volume you hear. A fully muted Windows source produces no signal.",
        ["Niveau automatique · ignorer le volume Windows"] = "Automatic level · ignore Windows volume",
        ["ENCODAGE DU FICHIER AUDIO"] = "AUDIO FILE ENCODING",
        ["Copier les captures audio dans le presse-papiers comme fichiers"] = "Copy audio captures to the clipboard as files",
        ["Traduction"] = "Translation",
        ["TRADUCTION SUR L’ÉCRAN · HORS LIGNE"] = "ON-SCREEN TRANSLATION · OFFLINE",
        ["Sélectionnez une zone, CyCapture reconnaît le texte puis affiche la traduction directement par-dessus l’image figée. Aucun texte ni aucune image n’est envoyé vers un service en ligne."] = "Select an area and CyCapture recognizes its text, then displays the translation directly over the frozen image. No text or image is sent to an online service.",
        ["LANGUE DU TEXTE"] = "TEXT LANGUAGE",
        ["TRADUIRE VERS"] = "TRANSLATE TO",
        ["Gérer et télécharger les modèles de langues…"] = "Manage and download language models…",
        ["Copier automatiquement le texte traduit dans le presse-papiers"] = "Automatically copy translated text to the clipboard",
        ["COMPORTEMENT DE LA TOUCHE IMPR ÉCRAN"] = "PRINT SCREEN KEY BEHAVIOR",
        ["Afficher le choix Image / Vidéo / GIF / Audio"] = "Show Image / Video / GIF / Audio choices",
        ["Afficher le choix Image / Vidéo / GIF / Audio / Traduction"] = "Show Image / Video / GIF / Audio / Translation choices",
        ["Capture d’image directe"] = "Direct image capture",
        ["Capture vidéo directe"] = "Direct video capture",
        ["Capture GIF directe"] = "Direct GIF capture",
        ["Capture audio directe"] = "Direct audio capture",
        ["Traduction directe d’une zone"] = "Direct area translation",
        ["Appui court : sélection image · maintien réglable : modes"] = "Short press: image selection · configurable hold: modes",
        ["Délai du maintien"] = "Hold delay",
        ["Les modes directs sautent la palette. Dans le mode hybride, un appui court ouvre la sélection intelligente d’image ; un maintien pendant le délai choisi ouvre la palette Image / Vidéo / GIF / Audio / Traduction."] = "Direct modes skip the palette. In hybrid mode, a short press opens smart image selection; holding for the configured delay opens the Image / Video / GIF / Audio / Translation palette.",
        ["DOSSIER DES CAPTURES"] = "CAPTURE FOLDER",
        ["Choisir"] = "Choose",
        ["Ouvrir"] = "Open",
        ["Séparer les captures par type (Images, Videos, GIFs et Audio)"] = "Separate captures by type (Images, Videos, GIFs and Audio)",
        ["Créer automatiquement un sous-dossier par jour"] = "Automatically create one subfolder per day",
        ["RÉGLAGES COMMUNS"] = "COMMON SETTINGS",
        ["SÉLECTION PAR DÉFAUT · IMAGE / VIDÉO / GIF"] = "DEFAULT SELECTION · IMAGE / VIDEO / GIF",
        ["SÉLECTION PAR DÉFAUT · IMAGE / VIDÉO / GIF / TRADUCTION"] = "DEFAULT SELECTION · IMAGE / VIDEO / GIF / TRANSLATION",
        ["Intelligente"] = "Smart",
        ["Fenêtre"] = "Window",
        ["Écran"] = "Screen",
        ["Zone libre"] = "Free region",
        ["Lancer CyCapture au démarrage de Windows"] = "Start CyCapture with Windows",
        ["CyCapture démarrera directement dans la zone de notification pour l’utilisateur actuel."] = "CyCapture will start directly in the notification area for the current user.",
        ["PLUGINS DE POST-TRAITEMENT"] = "POST-PROCESSING PLUGINS",
        ["Activez et configurez chaque extension indépendamment."] = "Enable and configure each extension independently.",
        ["Dossier des plugins"] = "Plugins folder",
        ["Fermer cette fenêtre laisse CyCapture dans la zone de notification."] = "Closing this window leaves CyCapture in the notification area.",
        ["Fermer"] = "Close",
        ["Choisissez un mode"] = "Choose a mode",
        ["Capture instantanée"] = "Instant capture",
        ["VIDÉO"] = "VIDEO",
        ["MP4 · son optionnel"] = "MP4 · optional audio",
        ["Animation directe"] = "Direct animation",
        ["AUDIO"] = "AUDIO",
        ["MP3 · système/micro"] = "MP3 · system/mic",
        ["TRADUIRE"] = "TRANSLATE",
        ["OCR · hors ligne"] = "OCR · offline",
        ["RÉGLAGES RAPIDES PAR MODE"] = "QUICK SETTINGS BY MODE",
        ["FORMAT IMAGE"] = "IMAGE FORMAT",
        ["QUALITÉ JPEG UNIQUEMENT"] = "JPEG QUALITY ONLY",
        ["FLUIDITÉ VIDÉO"] = "VIDEO FRAME RATE",
        ["ENCODAGE DU SON VIDÉO"] = "VIDEO AUDIO ENCODING",
        ["Son du système dans la vidéo"] = "System audio in the video",
        ["Microphone dans la vidéo"] = "Microphone in the video",
        ["QUALITÉ GIF · SANS SON"] = "GIF QUALITY · NO AUDIO",
        ["ENCODAGE AUDIO SEUL · SANS IMAGE"] = "AUDIO-ONLY ENCODING · NO IMAGE",
        ["SÉLECTION · IMAGE / VIDÉO / GIF"] = "SELECTION · IMAGE / VIDEO / GIF",
        ["SÉLECTION · IMAGE / VIDÉO / GIF / TRADUCTION"] = "SELECTION · IMAGE / VIDEO / GIF / TRANSLATION",
        ["Gérer les modèles…"] = "Manage models…",
        ["Les modèles manquants sont téléchargés au premier usage, puis l’OCR et la traduction fonctionnent entièrement hors ligne."] = "Missing models are downloaded on first use, then OCR and translation work entirely offline.",
        ["POST-TRAITEMENTS"] = "POST-PROCESSING",
        ["Galerie CyCapture"] = "CyCapture Gallery",
        ["CHARGEMENT DES CAPTURES…"] = "LOADING CAPTURES…",
        ["Actualiser"] = "Refresh",
        ["Ouvrir le dossier"] = "Open folder",
        ["Aujourd’hui"] = "Today",
        ["Hier"] = "Yesterday",
        ["Tout"] = "All",
        ["Aucune capture"] = "No captures",
        ["Vos prochaines images, vidéos, GIF et captures audio apparaîtront ici."] = "Your next images, videos, GIFs and audio captures will appear here.",
        ["Copier"] = "Copy",
        ["Afficher le fichier"] = "Show file",
        ["Fermer  ×"] = "Close  ×",
        ["LECTURE AUDIO"] = "AUDIO PLAYBACK",
        ["Pause"] = "Pause",
        ["Échap pour fermer · Flèches gauche/droite pour parcourir les captures"] = "Esc to close · Left/right arrows to browse captures",
        ["Capture rapide · Impr écran"] = "Quick capture · Print Screen",
        ["Capturer une image"] = "Capture an image",
        ["Démarrer une capture vidéo"] = "Start video capture",
        ["Créer un GIF"] = "Create a GIF",
        ["Démarrer une capture audio"] = "Start audio capture",
        ["Aucun enregistrement en cours"] = "No recording in progress",
        ["Ouvrir CyCapture · Galerie"] = "Open CyCapture · Gallery",
        ["Réglages…"] = "Settings…",
        ["Dossier des captures"] = "Captures folder",
        ["Quitter CyCapture"] = "Quit CyCapture",
        ["INSTALLÉE AVEC CYANNOTA"] = "INSTALLED WITH CYANNOTA",
        ["PORTABLE AVEC CYANNOTA"] = "PORTABLE WITH CYANNOTA",
        ["PORTABLE SANS CYANNOTA"] = "PORTABLE WITHOUT CYANNOTA"
    };

    private static readonly IReadOnlyDictionary<string, string> EnglishToFrench =
        FrenchToEnglish.ToDictionary(pair => pair.Value, pair => pair.Key, StringComparer.Ordinal);

    internal static string Language { get; private set; } = "fr";
    internal static bool IsFrench => Language == "fr";

    internal static void Use(string? language)
    {
        Language = Models.Preferences.NormalizeLanguage(language, "fr");
        var culture = CultureInfo.GetCultureInfo(Language == "fr" ? "fr-FR" : "en-US");
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }

    internal static string Text(string french, string english) => IsFrench ? french : english;

    internal static string Translate(string text)
    {
        var translations = IsFrench ? EnglishToFrench : FrenchToEnglish;
        return translations.TryGetValue(text, out var translated) ? translated : text;
    }

    internal static void ApplyTo(Control root)
    {
        foreach (var node in root.GetLogicalDescendants().Prepend(root))
        {
            if (node is Window window) window.Title = Translate(window.Title ?? string.Empty);
            if (node is TextBlock text && text.Text is { } value) text.Text = Translate(value);
            if (node is ContentControl content && content.Content is string label) content.Content = Translate(label);
            if (node is TabItem tab && tab.Header is string header) tab.Header = Translate(header);
            if (node is TextBox input && input.PlaceholderText is { } placeholder)
                input.PlaceholderText = Translate(placeholder);
        }
    }
}
