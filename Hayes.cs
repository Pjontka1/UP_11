namespace ModemTerminal;

public enum HayesKind
{
    None,
    Ok,
    Connect,
    Ring,
    NoCarrier,
    NoDialTone,
    Busy,
    NoAnswer,
    Error
}

/// <summary>
/// Rozpoznaje odpowiedzi modemu w trybie słownym (ATV1) i cyfrowym (ATV0).
/// Linia musi być już obcięta ze znaków CR/LF.
/// </summary>
public static class HayesResult
{
    /// <param name="numeric">
    /// Kody cyfrowe (ATV0) mają sens tylko w trybie komend.
    /// W trybie danych linia „3” to tekst rozmowy, nie NO CARRIER.
    /// </param>
    public static HayesKind Classify(string line, bool numeric)
    {
        if (string.IsNullOrWhiteSpace(line)) return HayesKind.None;
        line = line.Trim();

        if (line.Equals("OK", StringComparison.OrdinalIgnoreCase)) return HayesKind.Ok;
        if (line.Equals("ERROR", StringComparison.OrdinalIgnoreCase)) return HayesKind.Error;
        if (line.Equals("RING", StringComparison.OrdinalIgnoreCase)) return HayesKind.Ring;
        if (line.StartsWith("CONNECT", StringComparison.OrdinalIgnoreCase)) return HayesKind.Connect;
        if (line.Equals("NO CARRIER", StringComparison.OrdinalIgnoreCase)) return HayesKind.NoCarrier;
        if (line.Equals("NO DIALTONE", StringComparison.OrdinalIgnoreCase)
            || line.Equals("NO DIAL TONE", StringComparison.OrdinalIgnoreCase))
            return HayesKind.NoDialTone;
        if (line.Equals("BUSY", StringComparison.OrdinalIgnoreCase)) return HayesKind.Busy;
        if (line.Equals("NO ANSWER", StringComparison.OrdinalIgnoreCase)) return HayesKind.NoAnswer;

        if (numeric && IsDigits(line) && int.TryParse(line, out int code))
        {
            return code switch
            {
                0 => HayesKind.Ok,
                2 => HayesKind.Ring,
                3 => HayesKind.NoCarrier,
                4 => HayesKind.Error,
                6 => HayesKind.NoDialTone,
                7 => HayesKind.Busy,
                8 => HayesKind.NoAnswer,
                // Pozostałe kody liczbowe to CONNECT z prędkością (1, 5, 10, 11, …).
                _ => HayesKind.Connect
            };
        }

        return HayesKind.None;
    }

    private static bool IsDigits(string s)
    {
        if (s.Length == 0) return false;
        foreach (char c in s)
            if (c is < '0' or > '9') return false;
        return true;
    }
}
