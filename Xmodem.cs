using System.Diagnostics;
using System.IO.Ports;

namespace ModemTerminal;

/// <summary>
/// XMODEM: suma kontrolna, CRC-16 (CCITT) oraz odbiór bloków 1K.
/// Ramka: SOH | nr | 255-nr | 128 B danych | suma (1 B) lub CRC (2 B)
/// </summary>
public static class XModem
{
    private const byte SOH = 0x01, STX = 0x02, EOT = 0x04, ACK = 0x06,
                       NAK = 0x15, CAN = 0x18, PAD = 0x1A;
    private const int MaxRetries = 10;

    // ---------- narzędzia ----------

    private static int Rd(SerialPort p, int ms)
    {
        p.ReadTimeout = ms;
        try { return p.ReadByte(); }
        catch (TimeoutException) { return -1; }
    }

    private static void Wr(SerialPort p, params byte[] b) => p.Write(b, 0, b.Length);

    private static void Purge(SerialPort p) { while (Rd(p, 200) >= 0) { } }

    private static void Abort(SerialPort p) { try { Wr(p, CAN, CAN, CAN); } catch { } }

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

    // ---------- nadawca ----------

    public static bool Send(SerialPort p, Stream src, Action<string> log, CancellationToken ct)
    {
        // czekamy na 'C' (tryb CRC) lub NAK (suma kontrolna) od odbiorcy
        bool crc;
        var sw = Stopwatch.StartNew();
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            if (sw.Elapsed.TotalSeconds > 60) { log("Brak sygnału startu od odbiorcy (timeout)."); return false; }
            int c = Rd(p, 1000);
            if (c == 'C') { crc = true; break; }
            if (c == NAK) { crc = false; break; }
            if (c == CAN) { log("Odbiorca anulował transmisję."); return false; }
        }
        log(crc ? "Start: tryb CRC-16." : "Start: tryb sumy kontrolnej.");

        var data = new byte[128];
        byte blk = 1;
        int sent = 0, n;

        while ((n = src.Read(data, 0, 128)) > 0)
        {
            if (n < 128) Array.Fill(data, PAD, n, 128 - n);

            var frame = new byte[3 + 128 + (crc ? 2 : 1)];
            frame[0] = SOH; frame[1] = blk; frame[2] = (byte)(255 - blk);
            Array.Copy(data, 0, frame, 3, 128);
            if (crc)
            {
                ushort c16 = Crc16(data, 128);
                frame[131] = (byte)(c16 >> 8); frame[132] = (byte)(c16 & 0xFF);
            }
            else frame[131] = Checksum(data, 128);

            bool ok = false;
            for (int t = 0; t < MaxRetries && !ok; t++)
            {
                ct.ThrowIfCancellationRequested();
                p.Write(frame, 0, frame.Length);
                int r = Rd(p, 10000);
                if (r == ACK) ok = true;
                else if (r == CAN) { log("Odbiorca anulował transmisję."); return false; }
                else log($"Blok {blk}: brak ACK (odp={r}), ponawiam ({t + 1}/{MaxRetries}).");
            }
            if (!ok) { Abort(p); log("Przerwano: zbyt wiele błędów."); return false; }

            blk++; sent += n;
            log($"Wysłano {sent} B");
        }

        for (int t = 0; t < MaxRetries; t++)
        {
            Wr(p, EOT);
            if (Rd(p, 5000) == ACK) { log("Transmisja zakończona."); return true; }
        }
        log("Brak ACK na EOT.");
        return false;
    }

    // ---------- odbiorca ----------

    public static bool Receive(SerialPort p, Stream dst, Action<string> log, CancellationToken ct)
    {
        p.DiscardInBuffer();
        bool crc = true;
        int hdr = -1;

        // handshake: 10 x 'C', potem przejście na NAK (suma kontrolna)
        for (int i = 0; i < 20 && hdr < 0; i++)
        {
            ct.ThrowIfCancellationRequested();
            if (i == 10) { crc = false; log("Brak odpowiedzi na 'C' – przechodzę na sumę kontrolną."); }
            Wr(p, crc ? (byte)'C' : NAK);
            int c = Rd(p, 3000);
            if (c == SOH || c == STX || c == EOT) hdr = c;
        }
        if (hdr < 0) { log("Nadawca nie rozpoczął transmisji."); return false; }

        var ms = new MemoryStream();
        byte expected = 1;
        int errors = 0;

        while (true)
        {
            ct.ThrowIfCancellationRequested();
            if (hdr == -2) hdr = Rd(p, 10000);

            if (hdr == -1)
            {
                if (++errors > MaxRetries) { Abort(p); log("Timeout – przerwano."); return false; }
                Wr(p, NAK); hdr = -2; continue;
            }
            if (hdr == EOT) { Wr(p, ACK); break; }
            if (hdr == CAN) { log("Nadawca anulował transmisję."); return false; }

            if (hdr != SOH && hdr != STX)
            {
                if (++errors > MaxRetries) { Abort(p); log("Zbyt wiele błędów."); return false; }
                Purge(p); Wr(p, NAK); hdr = -2; continue;
            }

            int size = hdr == STX ? 1024 : 128;
            int b = Rd(p, 1000), nb = Rd(p, 1000);
            bool good = b >= 0 && nb >= 0 && (b + nb) == 255;
            var data = new byte[size];
            for (int i = 0; i < size && good; i++)
            {
                int x = Rd(p, 1000);
                if (x < 0) good = false; else data[i] = (byte)x;
            }
            if (good)
            {
                if (crc)
                {
                    int hi = Rd(p, 1000), lo = Rd(p, 1000);
                    good = hi >= 0 && lo >= 0 && Crc16(data, size) == (ushort)((hi << 8) | lo);
                }
                else
                {
                    int s = Rd(p, 1000);
                    good = s >= 0 && Checksum(data, size) == (byte)s;
                }
            }
            if (good)
            {
                if (b == expected) { ms.Write(data, 0, size); expected++; errors = 0; log($"Odebrano {ms.Length} B"); }
                else if (b != (byte)(expected - 1)) good = false;   // duplikat jest OK, inny numer – błąd
            }

            if (good) Wr(p, ACK);
            else
            {
                if (++errors > MaxRetries) { Abort(p); log("Zbyt wiele błędów."); return false; }
                Purge(p); Wr(p, NAK);
            }
            hdr = -2;
        }

        // usuń dopełnienie 0x1A z końca pliku
        var buf = ms.ToArray();
        int len = buf.Length;
        while (len > 0 && buf[len - 1] == PAD) len--;
        dst.Write(buf, 0, len);
        log($"Odbiór zakończony: {len} B.");
        return true;
    }
}