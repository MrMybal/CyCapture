using CyCapture.Models;
using System.Text;

namespace CyCapture.Services;

internal static class TranslationLanguageCatalog
{
    internal const string AutomaticCode = "auto";

    internal static IReadOnlyList<TranslationLanguage> Languages { get; } =
    [
        new("af", "Afrikaans", "Afrikaans", "afr"),
        new("ar", "Arabic", "Arabe", "ara"),
        new("az", "Azerbaijani", "Azéri", "aze"),
        new("be", "Belarusian", "Biélorusse", "bel"),
        new("bg", "Bulgarian", "Bulgare", "bul"),
        new("bn", "Bengali", "Bengali", "ben"),
        new("bs", "Bosnian", "Bosniaque", "bos"),
        new("ca", "Catalan", "Catalan", "cat"),
        new("cs", "Czech", "Tchèque", "ces"),
        new("da", "Danish", "Danois", "dan"),
        new("de", "German", "Allemand", "deu"),
        new("el", "Greek", "Grec", "ell"),
        new("en", "English", "Anglais", "eng"),
        new("es", "Spanish", "Espagnol", "spa"),
        new("et", "Estonian", "Estonien", "est"),
        new("eu", "Basque", "Basque", "eus"),
        new("fa", "Persian", "Persan", "fas"),
        new("fi", "Finnish", "Finnois", "fin"),
        new("fr", "French", "Français", "fra"),
        new("gl", "Galician", "Galicien", "glg"),
        new("gu", "Gujarati", "Gujarati", "guj"),
        new("he", "Hebrew", "Hébreu", "heb"),
        new("hi", "Hindi", "Hindi", "hin"),
        new("hr", "Croatian", "Croate", "hrv"),
        new("hu", "Hungarian", "Hongrois", "hun"),
        new("id", "Indonesian", "Indonésien", "ind"),
        new("is", "Icelandic", "Islandais", "isl"),
        new("it", "Italian", "Italien", "ita"),
        new("ja", "Japanese", "Japonais", "jpn"),
        new("kn", "Kannada", "Kannada", "kan"),
        new("ko", "Korean", "Coréen", "kor"),
        new("lt", "Lithuanian", "Lituanien", "lit"),
        new("lv", "Latvian", "Letton", "lav"),
        new("ml", "Malayalam", "Malayalam", "mal"),
        new("mr", "Marathi", "Marathi", "mar"),
        new("ms", "Malay", "Malais", "msa"),
        new("nb", "Norwegian Bokmål", "Norvégien bokmål", "nor"),
        new("nl", "Dutch", "Néerlandais", "nld"),
        new("no", "Norwegian", "Norvégien", "nor"),
        new("pl", "Polish", "Polonais", "pol"),
        new("pt", "Portuguese", "Portugais", "por"),
        new("ro", "Romanian", "Roumain", "ron"),
        new("ru", "Russian", "Russe", "rus"),
        new("sk", "Slovak", "Slovaque", "slk"),
        new("sl", "Slovenian", "Slovène", "slv"),
        new("sq", "Albanian", "Albanais", "sqi"),
        new("sr", "Serbian", "Serbe", "srp"),
        new("sv", "Swedish", "Suédois", "swe"),
        new("ta", "Tamil", "Tamoul", "tam"),
        new("te", "Telugu", "Télougou", "tel"),
        new("th", "Thai", "Thaï", "tha"),
        new("tl", "Filipino", "Filipino", "fil"),
        new("tr", "Turkish", "Turc", "tur"),
        new("ug", "Uyghur", "Ouïghour", "uig"),
        new("uk", "Ukrainian", "Ukrainien", "ukr"),
        new("ur", "Urdu", "Ourdou", "urd"),
        new("vi", "Vietnamese", "Vietnamien", "vie"),
        new("zh", "Chinese (simplified)", "Chinois simplifié", "chi_sim"),
        new("zh_hant", "Chinese (traditional)", "Chinois traditionnel", "chi_tra")
    ];

    internal static TranslationLanguage? Find(string? code) => Languages.FirstOrDefault(language =>
        language.Code.Equals(code, StringComparison.OrdinalIgnoreCase));

    internal static string NormalizeSource(string? code) =>
        string.Equals(code, AutomaticCode, StringComparison.OrdinalIgnoreCase)
            ? AutomaticCode
            : Find(code)?.Code ?? AutomaticCode;

    internal static string NormalizeTarget(string? code) => Find(code)?.Code ?? "fr";

    internal static string DisplayName(string code)
    {
        if (code.Equals(AutomaticCode, StringComparison.OrdinalIgnoreCase))
            return UiLocalization.Text("Détection automatique", "Automatic detection");
        return Find(code)?.DisplayName ?? code;
    }

    internal static string DetectFromText(string text, string latinFallback = "en")
    {
        var hasHan = false;
        foreach (var rune in text.EnumerateRunes())
        {
            var value = rune.Value;
            if (value is >= 0x3040 and <= 0x30ff) return "ja";
            if (value is >= 0xac00 and <= 0xd7af) return "ko";
            if (value is >= 0x4e00 and <= 0x9fff) hasHan = true;
            if (value is >= 0x0600 and <= 0x06ff) return "ar";
            if (value is >= 0x0590 and <= 0x05ff) return "he";
            if (value is >= 0x0400 and <= 0x04ff) return "ru";
            if (value is >= 0x0e00 and <= 0x0e7f) return "th";
            if (value is >= 0x0900 and <= 0x097f) return "hi";
        }
        return hasHan ? "zh" : NormalizeTarget(latinFallback);
    }
}
