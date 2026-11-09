using System.Diagnostics;
using System.Text;

namespace ModemTerminal;

/// <summary>
/// Bajty w obie strony. ReadByte ma wrócić najpóźniej po timeoutMs
/// (albo rzucić, gdy token jest anulowany).
/// </summary>
public interface IModemLink
{
    int ReadByte(int timeoutMs, CancellationToken ct);
    void Write(byte[] data, int offset, int count);
    void DiscardInput();
    void Unget(byte value);
}

/// <summary>
/// XMODEM: suma kontrolna, CRC-16 (CCITT) oraz odbiór bloków 1K.
/// Ramka: SOH | nr | 255-nr | 128 B danych | suma (1 B) lub CRC (2 B).
/// Blok nr 0 (tylko między kopiami tego programu) niesie dokładną długość
/// pliku, więc końcowe bajty 0x1A nie są obcinane. Nadawca klasyczny,
/// który zaczyna od bloku 1, jest przyjmowany po staremu.
/// </summary>
public static class XModem
{
    private const byte SOH = 0x01, STX = 0x02, EOT = 0x04, ACK = 0x06,
                       NAK = 0x15, CAN = 0x18, PAD = 0x1A;
    private const int MaxRetries = 10;

    private static int Rd(IModemLink p, int ms, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            int left = ms - (int)sw.ElapsedMilliseconds;
            if (left <= 0) return -1;
            int b = p.ReadByte(Math.Min(100, left), ct);
            if (b >= 0) return b;
        }
    }

    private static void Wr(IModemLink p, params byte[] b) => p.Write(b, 0, b.Length);

    private static void Purge(IModemLink p, CancellationToken ct)
    {
        while (Rd(p, 200, ct) >= 0) { }
    }

    private static void Abort(IModemLink p)
    {
        try { Wr(p, CAN, CAN, CAN); } catch { }
    }

    public static ushort Crc16(byte[] d, int len)
    {
        ushort crc = 0;
        for (int i = 0; i < len; i++)
        {
            crc ^= (ushort)(d[i] << 8);
            for (int b = 0; b < 8; b++)
                crc = (crc & 0x8000) != 0 ? (ushort)((crc << 1) ^ 0x1021) : (ushort)(crc << 1);
        }
        return crc;
    }

    private static byte Checksum(byte[] d, int len)
    {
        int s = 0;
        for (int i = 0; i < len; i++) s += d[i];
        return (byte)s;
    }

    private static byte[] Build(byte blk, byte[] data, bool crc)
    {
        var frame = new byte[3 + 128 + (crc ? 2 : 1)];
        frame[0] = SOH;
        frame[1] = blk;
        frame[2] = (byte)(255 - blk);
        Array.Copy(data, 0, frame, 3, 128);
        if (crc)
        {
            ushort c16 = Crc16(data, 128);
            frame[131] = (byte)(c16 >> 8);
            frame[132] = (byte)c16;
        }
        else frame[131] = Checksum(data, 128);
        return frame;
    }

    /// <summary>
    /// Czeka na 'C' albo NAK, potem przez chwilę zjada kolejne znaki startu,
    /// które odbiorca zdążył wysłać, zanim nadawca ruszył.
    /// </summary>
    private static bool WaitForStart(IModemLink p, out bool crc, Action<string> log, CancellationToken ct)
    {
        crc = true;
        var sw = Stopwatch.StartNew();
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            if (sw.Elapsed.TotalSeconds > 60)
            {
                log("Brak sygnału startu od odbiorcy (timeout).");
                return false;
            }
            int c = Rd(p, 1000, ct);
            if (c != 'C' && c != NAK)
            {
                if (c == CAN) { log("Odbiorca anulował transmisję."); return false; }
                continue;
            }

            crc = c == 'C';
            while (true)
            {
                int extra = Rd(p, 200, ct);
                if (extra < 0) break;
                if (extra == 'C') { crc = true; continue; }
                if (extra == NAK) { crc = false; continue; }
                if (extra == CAN) { log("Odbiorca anulował transmisję."); return false; }
                p.Unget((byte)extra);
                break;
            }
            log(crc ? "Start: tryb CRC-16." : "Start: tryb sumy kontrolnej.");
            return true;
        }
    }

    private static bool SendFrame(IModemLink p, byte blk, byte[] data, bool crc, Action<string> log, CancellationToken ct)
    {
        byte[] frame = Build(blk, data, crc);
        for (int t = 0; t < MaxRetries; t++)
        {
            ct.ThrowIfCancellationRequested();
            p.Write(frame, 0, frame.Length);
            int r = Rd(p, 10000, ct);
            if (r == ACK) return true;
            if (r == CAN)
            {
                log("Odbiorca anulował transmisję.");
                return false;
            }
            log($"Blok {blk}: brak ACK (odp={r}), ponawiam ({t + 1}/{MaxRetries}).");
        }
        Abort(p);
        log("Przerwano: zbyt wiele błędów.");
        return false;
    }

    public static bool Send(IModemLink p, Stream src, long length, Action<string> log, CancellationToken ct)
    {
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
        if (!WaitForStart(p, out bool crc, log, ct)) return false;

        var header = new byte[128];
        byte[] lenBytes = Encoding.ASCII.GetBytes(length.ToString());
        Array.Copy(lenBytes, header, lenBytes.Length);
        if (!SendFrame(p, 0, header, crc, log, ct)) return false;

        var data = new byte[128];
        byte blk = 1;
        long sent = 0;
        while (sent < length)
        {
            int n = src.Read(data, 0, 128);
            if (n <= 0) break;
            if (n < 128) Array.Fill(data, PAD, n, 128 - n);
            if (!SendFrame(p, blk, data, crc, log, ct)) return false;
            int used = (int)Math.Min(n, length - sent);
            sent += used;
            blk++;
            log($"Wysłano {sent} B");
        }

        for (int t = 0; t < MaxRetries; t++)
        {
            ct.ThrowIfCancellationRequested();
            Wr(p, EOT);
            if (Rd(p, 5000, ct) == ACK)
            {
                log("Transmisja zakończona.");
                return true;
            }
        }
        log("Brak ACK na EOT.");
        return false;
    }

    public static bool Receive(IModemLink p, Stream dst, Action<string> log, CancellationToken ct)
    {
        p.DiscardInput();
        bool crc = true;
        int hdr = -1;

        for (int i = 0; i < 20 && hdr < 0; i++)
        {
            ct.ThrowIfCancellationRequested();
            if (i == 10) { crc = false; log("Brak odpowiedzi na 'C' – przechodzę na sumę kontrolną."); }
            Wr(p, crc ? (byte)'C' : NAK);
            int c = Rd(p, 3000, ct);
            if (c == SOH || c == STX || c == EOT) hdr = c;
        }
        if (hdr < 0) { log("Nadawca nie rozpoczął transmisji."); return false; }

        var ms = new MemoryStream();
        byte expected = 1;
        int errors = 0;
        long? exact = null;

        while (true)
        {
            ct.ThrowIfCancellationRequested();
            if (hdr == -2) hdr = Rd(p, 10000, ct);

            if (hdr == -1)
            {
                if (++errors > MaxRetries) { Abort(p); log("Timeout – przerwano."); return false; }
                Wr(p, NAK);
                hdr = -2;
                continue;
            }
            if (hdr == EOT)
            {
                Wr(p, ACK);
                break;
            }
            if (hdr == CAN) { log("Nadawca anulował transmisję."); return false; }

            if (hdr != SOH && hdr != STX)
            {
                if (++errors > MaxRetries) { Abort(p); log("Zbyt wiele błędów."); return false; }
                Purge(p, ct);
                Wr(p, NAK);
                hdr = -2;
                continue;
            }

            int size = hdr == STX ? 1024 : 128;
            int b = Rd(p, 1000, ct), nb = Rd(p, 1000, ct);
            bool good = b >= 0 && nb >= 0 && (b + nb) == 255;
            var data = new byte[size];
            for (int i = 0; i < size && good; i++)
            {
                int x = Rd(p, 1000, ct);
                if (x < 0) good = false;
                else data[i] = (byte)x;
            }
            if (good)
            {
                if (crc)
                {
                    int hi = Rd(p, 1000, ct), lo = Rd(p, 1000, ct);
                    good = hi >= 0 && lo >= 0 && Crc16(data, size) == (ushort)((hi << 8) | lo);
                }
                else
                {
                    int s = Rd(p, 1000, ct);
                    good = s >= 0 && Checksum(data, size) == (byte)s;
                }
            }
            if (good)
            {
                if (b == 0 && expected == 1 && size == 128)
                {
                    if (TryParseLength(data, out long len))
                    {
                        exact = len;
                        errors = 0;
                        log($"Rozmiar pliku: {len} B");
                    }
                    else good = false;
                }
                else if (b == expected)
                {
                    int take = size;
                    if (exact is long total)
                    {
                        long remain = total - ms.Length;
                        take = remain <= 0 ? 0 : (int)Math.Min(size, remain);
                    }
                    if (take > 0) ms.Write(data, 0, take);
                    expected++;
                    errors = 0;
                    log($"Odebrano {ms.Length} B");
                }
                else if (b != (byte)(expected - 1))
                    good = false;
            }

            if (good) Wr(p, ACK);
            else
            {
                if (++errors > MaxRetries) { Abort(p); log("Zbyt wiele błędów."); return false; }
                Purge(p, ct);
                Wr(p, NAK);
            }
            hdr = -2;
        }

        var buf = ms.ToArray();
        int outLen = buf.Length;
        if (exact is long want)
        {
            if (outLen != want)
            {
                log($"Niepełny plik: {outLen} z {want} B.");
                return false;
            }
        }
        else
        {
            while (outLen > 0 && buf[outLen - 1] == PAD) outLen--;
        }
        dst.Write(buf, 0, outLen);
        log($"Odbiór zakończony: {outLen} B.");
        return true;
    }

    private static bool TryParseLength(byte[] data, out long length)
    {
        length = 0;
        if (data.Length == 0 || data[0] is < (byte)'0' or > (byte)'9') return false;
        int i = 0;
        while (i < data.Length && data[i] is >= (byte)'0' and <= (byte)'9')
        {
            length = length * 10 + (data[i] - '0');
            if (length < 0) return false;
            i++;
        }
        return i > 0 && (i == data.Length || data[i] == 0);
    }
}
