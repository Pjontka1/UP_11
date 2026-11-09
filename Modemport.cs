using System;
using System.IO.Ports;
using System.Text;
using System.Threading.Tasks;

namespace ModemTerminal
{
    /// <summary>
    /// Opakowanie portu COM: konfiguracja 8N1, wysyłanie komend AT,
    /// sekwencja ESC (+++) oraz tryb "Raw" na czas transmisji XMODEM
    /// (wtedy zdarzenie tekstowe nie zabiera bajtów protokołu).
    /// </summary>
    public class ModemPort : IDisposable
    {
        private SerialPort _port;

        /// <summary>true = bajty zostają w buforze portu (używa ich XMODEM).</summary>
        public volatile bool Raw;

        public event Action<string> TextReceived;

        public bool IsOpen
        {
            get { return _port != null && _port.IsOpen; }
        }

        public SerialPort Port
        {
            get { return _port ?? throw new InvalidOperationException("Port nie jest otwarty."); }
        }

        public void Open(string name, int baud, bool rtsCts)
        {
            Close();
            var p = new SerialPort(name, baud, Parity.None, 8, StopBits.One)
            {
                Encoding = Encoding.GetEncoding(28591),   // ISO-8859-1: 1 bajt = 1 znak
                Handshake = rtsCts ? Handshake.RequestToSend : Handshake.None,
                DtrEnable = true,                         // DTR musi być aktywny, inaczej modem nie działa
                ReadTimeout = 1000,
                WriteTimeout = 2000
            };
            if (!rtsCts) p.RtsEnable = true;

            p.DataReceived += (sender, e) =>
            {
                if (Raw) return;
                try
                {
                    string s = p.ReadExisting();
                    if (s.Length > 0 && TextReceived != null) TextReceived(s);
                }
                catch { /* port zamknięty w trakcie odczytu */ }
            };

            p.Open();
            _port = p;
        }

        public void Close()
        {
            try { if (_port != null && _port.IsOpen) _port.Close(); } catch { }
            if (_port != null) _port.Dispose();
            _port = null;
        }

        /// <summary>Wysyła tekst dokładnie tak, jak jest podany.</summary>
        public void Send(string text)
        {
            Port.Write(text);
        }

        /// <summary>Komenda AT zakończona CR.</summary>
        public void Command(string cmd)
        {
            Port.Write(cmd + "\r");
        }

        /// <summary>
        /// Sekwencja ucieczki: cisza (guard time) + "+++" + cisza.
        /// Bez Entera! Modem przechodzi do trybu komend, połączenie trwa (ATO wraca do danych).
        /// </summary>
        public async Task EscapeAsync(int guardMs = 1200)
        {
            await Task.Delay(guardMs);
            Port.Write("+++");
            await Task.Delay(guardMs);
        }

        public void Dispose()
        {
            Close();
        }
    }
}
