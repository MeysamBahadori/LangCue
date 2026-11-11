using System.Globalization;

namespace LangCue;

internal sealed record KeyboardLanguage(string Badge, bool IsRightToLeft)
{
    public static KeyboardLanguage FromLayout(IntPtr layout)
    {
        ushort languageId = unchecked((ushort)(layout.ToInt64() & 0xFFFF));

        try
        {
            CultureInfo culture = CultureInfo.GetCultureInfo(languageId);
            string code = culture.TwoLetterISOLanguageName;
            if (string.IsNullOrWhiteSpace(code) || code == "iv")
            {
                code = culture.ThreeLetterISOLanguageName;
            }

            // Keep the familiar Persian badge; use ISO language codes elsewhere.
            string badge = code.Equals("fa", StringComparison.OrdinalIgnoreCase)
                ? "فا"
                : code.ToUpperInvariant();

            return new KeyboardLanguage(badge, culture.TextInfo.IsRightToLeft);
        }
        catch (CultureNotFoundException)
        {
            // Unknown or custom layout: show a stable language identifier, don't
            // silently treat it as English or remove the indicator.
            return new KeyboardLanguage(languageId.ToString("X4"), false);
        }
    }
}
