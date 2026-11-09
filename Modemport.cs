using System.IO.Ports;
using System.Text;

namespace ModemTerminal;

/// <summary>
/// Port COM, 8N1. Jeden wątek czyta port: w trybie tekstowym oddaje znaki
/// (Windows-1250), w trybie binarnym składa kolejkę dla XMODEM.
/// Dzięki temu transfer nie gubi bajtów na zdarzeniu DataReceived.
/// </summary>
public class ModemPort : IDisposable, IModemLink
{
    private static readonly Encoding Text = new Cp1250Encoding();

    private readonly object _bufLock = new();
    private readonly LinkedList<byte> _raw = new();
    private readonly object _writeLock = new();

    private SerialPort? _port;
    private CancellationTokenSource? _readCts;
    private Task? _readTask;
    private int _epoch;
    private int _needDrain;
    private int _drainDone;
    private bool _binary;

    public event Action<string>? TextReceived;

    public bool IsOpen => _port?.IsOpen == true;

    public void Open(string name, int baud, bool rtsCts)
    {
        Close();
        var p = new SerialPort(name, baud, Parity.None, 8, StopBits.One)
        {
            Encoding = Text,
            Handshake = rtsCts ? Handshake.RequestToSend : Handshake.None,
            DtrEnable = true,
            ReadTimeout = 200,
            WriteTimeout = 2000
        };
        if (!rtsCts) p.RtsEnable = true;
        p.Open();
        _port = p;
        _readCts = new CancellationTokenSource();
        var ct = _readCts.Token;
        _readTask = Task.Run(() => ReadLoop(p, ct));
    }

    public void Close()
    {
        try { _readCts?.Cancel(); } catch { }
        lock (_bufLock)
        {
            _binary = false;
            _raw.Clear();
            Monitor.PulseAll(_bufLock);
        }
        lock (_writeLock)
        {
            try { if (_port?.IsOpen == true) _port.Close(); } catch { }
            try { _port?.Dispose(); } catch { }
            _port = null;
        }
        try { _readTask?.Wait(1500); } catch { }
        _readTask = null;
        try { _readCts?.Dispose(); } catch { }
        _readCts = null;
    }

    /// <summary>
    /// Od tej chwili bajty idą do XMODEM. Czeka, aż wątek czytający wyrzuci
    /// to, co już leżało w buforze portu (np. końcówkę „CONNECT”).
    /// </summary>
    public void BeginBinary()
    {
        int gen;
        lock (_bufLock)
        {
            _binary = true;
            _raw.Clear();
            gen = _epoch + 1;
            Volatile.Write(ref _epoch, gen);
            Volatile.Write(ref _needDrain, gen);
            Monitor.PulseAll(_bufLock);
        }
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (Volatile.Read(ref _drainDone) < gen && sw.ElapsedMilliseconds < 1500)
            Thread.Sleep(15);
    }

    /// <summary>Wraca do rozmowy. Bajty, które zostały w kolejce, trafiają do logu.</summary>
    public void EndBinary()
    {
        byte[] rest;
        lock (_bufLock)
        {
            _binary = false;
            rest = _raw.ToArray();
            _raw.Clear();
        }
        if (rest.Length > 0)
            TextReceived?.Invoke(Text.GetString(rest));
    }

    public void Send(string text)
    {
        byte[] bytes = Text.GetBytes(text);
        Write(bytes, 0, bytes.Length);
    }

    /// <summary>Komenda AT zakończona CR.</summary>
    public void Command(string cmd) => Send(cmd + "\r");

    /// <summary>
    /// Sekwencja ucieczki: cisza (guard time) + "+++" + cisza.
    /// Bez Entera. Modem przechodzi do trybu komend, połączenie trwa.
    /// </summary>
    public async Task EscapeAsync(int guardMs = 1200)
    {
        await Task.Delay(guardMs);
        Send("+++");
        await Task.Delay(guardMs);
    }

    public int ReadByte(int timeoutMs, CancellationToken ct)
    {
        lock (_bufLock)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (_raw.Count == 0)
            {
                ct.ThrowIfCancellationRequested();
                int left = timeoutMs - (int)sw.ElapsedMilliseconds;
                if (left <= 0) return -1;
                Monitor.Wait(_bufLock, Math.Min(left, 100));
            }
            byte b = _raw.First!.Value;
            _raw.RemoveFirst();
            return b;
        }
    }

    public void Write(byte[] data, int offset, int count)
    {
        lock (_writeLock)
        {
            SerialPort p = _port ?? throw new InvalidOperationException("Port nie jest otwarty.");
            p.Write(data, offset, count);
        }
    }

    public void DiscardInput()
    {
        // Nie wołamy SerialPort.DiscardInBuffer: drugi wątek jest w Read
        // i takie wywołanie potrafi je przerwać na stałe.
        lock (_bufLock)
        {
            Volatile.Write(ref _epoch, Volatile.Read(ref _epoch) + 1);
            _raw.Clear();
            Monitor.PulseAll(_bufLock);
        }
    }

    public void Unget(byte value)
    {
        lock (_bufLock)
        {
            _raw.AddFirst(value);
            Monitor.PulseAll(_bufLock);
        }
    }

    public void Dispose() => Close();

    private void ReadLoop(SerialPort p, CancellationToken ct)
    {
        var buf = new byte[1024];
        while (!ct.IsCancellationRequested)
        {
            if (Volatile.Read(ref _needDrain) != Volatile.Read(ref _drainDone))
            {
                if (!DrainStale(p, buf)) break;
                continue;
            }

            int seen = Volatile.Read(ref _epoch);
            int n;
            try
            {
                n = p.Read(buf, 0, buf.Length);
            }
            catch (TimeoutException)
            {
                continue;
            }
            catch
            {
                break;
            }
            if (n <= 0) continue;
            if (Volatile.Read(ref _epoch) != seen
                || Volatile.Read(ref _needDrain) != Volatile.Read(ref _drainDone))
                continue;

            var chunk = new byte[n];
            Buffer.BlockCopy(buf, 0, chunk, 0, n);

            bool asBinary;
            lock (_bufLock)
            {
                if (Volatile.Read(ref _epoch) != seen) continue;
                asBinary = _binary;
                if (asBinary)
                {
                    foreach (byte b in chunk) _raw.AddLast(b);
                    Monitor.PulseAll(_bufLock);
                }
            }
            if (!asBinary && chunk.Length > 0)
                TextReceived?.Invoke(Text.GetString(chunk));
        }
    }

    /// <summary>Zwraca false, gdy port został zamknięty.</summary>
    private bool DrainStale(SerialPort p, byte[] buf)
    {
        int n;
        try
        {
            n = p.Read(buf, 0, buf.Length);
        }
        catch (TimeoutException)
        {
            n = 0;
        }
        catch
        {
            return false;
        }

        lock (_bufLock)
        {
            _raw.Clear();
            Monitor.PulseAll(_bufLock);
        }

        int pending = 0;
        try { pending = p.BytesToRead; } catch { pending = 0; }
        if (n == 0 || pending == 0)
            Volatile.Write(ref _drainDone, Volatile.Read(ref _needDrain));
        return true;
    }
}
