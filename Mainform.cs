using System.IO.Ports;

namespace ModemTerminal
{
    public partial class MainForm : Form
    {
        private readonly ModemPort _modem = new();
        private CancellationTokenSource? _cts;
        private bool _online;
        private string _tail = "";

        public MainForm()
        {
            InitializeComponent();

            // lista portów (dane dynamiczne, dlatego nie w designerze)
            cbPort.Items.AddRange(SerialPort.GetPortNames().OrderBy(s => s).ToArray());
            if (cbPort.Items.Count > 0) cbPort.SelectedIndex = 0;

            _modem.TextReceived += OnText;
            FormClosing += (_, _) => { _cts?.Cancel(); _modem.Dispose(); };
            UpdateUi();
        }

        // ---------- port ----------

        private void btnOpen_Click(object? sender, EventArgs e)
        {
            if (_modem.IsOpen)
            {
                _modem.Close();
                _online = false;
                Log("[port zamknięty]\r\n");
                UpdateUi();
                return;
            }
            try
            {
                if (cbPort.SelectedItem is not string name || !int.TryParse(cbBaud.Text, out int baud))
                {
                    MessageBox.Show("Wybierz port i prędkość.");
                    return;
                }
                _modem.Open(name, baud, chkRts.Checked);
                Log($"[otwarto {name}, {baud} 8N1]\r\n");
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Błąd portu"); }
            UpdateUi();
        }

        private bool Ready()
        {
            if (_modem.IsOpen) return true;
            MessageBox.Show("Najpierw otwórz port.");
            return false;
        }

        private void Cmd(string c)
        {
            if (!Ready()) return;
            try { _modem.Command(c); Log($"> {c}\r\n"); }
            catch (Exception ex) { Log($"[błąd: {ex.Message}]\r\n"); }
        }

        // ---------- przyciski komend Hayesa ----------

        private void btnInit_Click(object? sender, EventArgs e) => Cmd("ATZ");
        private void btnDial_Click(object? sender, EventArgs e) => Cmd("ATDT" + txtPhone.Text.Trim());
        private void btnAnswer_Click(object? sender, EventArgs e) => Cmd("ATA");
        private void btnAuto_Click(object? sender, EventArgs e) => Cmd("ATS0=1");
        private void btnAto_Click(object? sender, EventArgs e) => Cmd("ATO");
        private void btnHang_Click(object? sender, EventArgs e) => Cmd("ATH");

        private async void btnEsc_Click(object? sender, EventArgs e)
        {
            if (!Ready()) return;
            Log("[+++]\r\n");
            try { await _modem.EscapeAsync(); }
            catch (Exception ex) { Log($"[błąd: {ex.Message}]\r\n"); }
        }

        // ---------- konwersacja ----------

        private void txtIn_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            if (!Ready()) return;
            string line = txtIn.Text;
            txtIn.Clear();
            try
            {
                if (_online)
                {
                    _modem.Send(line + "\r\n");   // tryb danych: rozmowa z drugim terminalem
                    Log($"« {line}\r\n");         // lokalne echo
                }
                else _modem.Command(line);        // tryb komend: komenda AT (echo robi modem)
            }
            catch (Exception ex) { Log($"[błąd: {ex.Message}]\r\n"); }
        }

        private void OnText(string s)
        {
            // śledzenie stanu połączenia po odpowiedziach modemu
            _tail += s;
            if (_tail.Length > 200) _tail = _tail[^200..];
            if (_tail.Contains("CONNECT")) { _online = true; _tail = ""; BeginInvoke(UpdateUi); }
            else if (_tail.Contains("NO CARRIER") || _tail.Contains("NO DIALTONE") || _tail.Contains("BUSY"))
            { _online = false; _tail = ""; BeginInvoke(UpdateUi); }

            BeginInvoke(() => Log(s.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n")));
        }

        // ---------- XMODEM ----------

        private void btnSendFile_Click(object? sender, EventArgs e) => Transfer(send: true);
        private void btnRecvFile_Click(object? sender, EventArgs e) => Transfer(send: false);
        private void btnCancel_Click(object? sender, EventArgs e) => _cts?.Cancel();

        private void Transfer(bool send)
        {
            if (!Ready()) return;

            string path;
            if (send)
            {
                using var dlg = new OpenFileDialog();
                if (dlg.ShowDialog() != DialogResult.OK) return;
                path = dlg.FileName;
            }
            else
            {
                using var dlg = new SaveFileDialog();
                if (dlg.ShowDialog() != DialogResult.OK) return;
                path = dlg.FileName;
            }

            _cts = new CancellationTokenSource();
            var ct = _cts.Token;
            SetBusy(true);
            Log(send ? "[XMODEM: wysyłanie]\r\n" : "[XMODEM: odbieranie]\r\n");

            _modem.Raw = true;                    // zatrzymaj odczyt tekstowy
            var port = _modem.Port;

            Task.Run(() =>
            {
                bool ok = false;
                try
                {
                    void Lg(string m) => BeginInvoke(() => Log("[XMODEM] " + m + "\r\n"));
                    if (send)
                    {
                        using var fs = File.OpenRead(path);
                        ok = XModem.Send(port, fs, Lg, ct);
                    }
                    else
                    {
                        using var ms = new MemoryStream();
                        ok = XModem.Receive(port, ms, Lg, ct);
                        if (ok) File.WriteAllBytes(path, ms.ToArray());
                    }
                }
                catch (OperationCanceledException)
                {
                    BeginInvoke(() => Log("[XMODEM anulowany]\r\n"));
                    try { port.Write(new byte[] { 0x18, 0x18, 0x18 }, 0, 3); } catch { }
                }
                catch (Exception ex) { BeginInvoke(() => Log($"[XMODEM błąd: {ex.Message}]\r\n")); }
                finally
                {
                    _modem.Raw = false;
                    BeginInvoke(() => { SetBusy(false); Log(ok ? "[transfer OK]\r\n" : "[transfer NIEUDANY]\r\n"); });
                }
            });
        }

        // ---------- UI ----------

        private void SetBusy(bool busy)
        {
            btnSendFile.Enabled = btnRecvFile.Enabled = !busy;
            btnCancel.Enabled = busy;
            txtIn.Enabled = !busy;
        }

        private void UpdateUi()
        {
            btnOpen.Text = _modem.IsOpen ? "Zamknij port" : "Otwórz port";
            lblStatus.Text = !_modem.IsOpen ? "Port zamknięty" : _online ? "ONLINE (tryb danych)" : "Tryb komend";
            lblStatus.ForeColor = _online ? Color.DarkGreen : Color.Black;
        }

        private void Log(string s) => txtLog.AppendText(s);
    }
}