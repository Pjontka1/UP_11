using System.IO.Ports;

namespace ModemTerminal
{
    public partial class MainForm : Form
    {
        private static readonly string[] InitCommands = ["ATZ", "ATE1", "ATQ0", "ATV1", "ATX3"];

        private readonly ModemPort _modem = new();
        private CancellationTokenSource? _cts;
        private bool _carrier;
        private bool _dataMode;
        private bool _hangupPending;
        private bool _transferring;
        private bool _script;
        private string _acc = "";
        private (int Gen, TaskCompletionSource<HayesKind> Tcs)? _wait;
        private int _waitSerial;

        public MainForm()
        {
            InitializeComponent();

            cbPort.Items.AddRange(SerialPort.GetPortNames().OrderBy(s => s).ToArray());
            if (cbPort.Items.Count > 0) cbPort.SelectedIndex = 0;

            _modem.TextReceived += OnText;
            FormClosing += (_, _) => { _cts?.Cancel(); _modem.Dispose(); };
            UpdateUi();
        }

        // ---------- port ----------

        private void btnOpen_Click(object? sender, EventArgs e)
        {
            if (_script || _transferring) return;
            if (_modem.IsOpen)
            {
                _modem.Close();
                ResetLink();
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
                ResetLink();
                Log($"[otwarto {name}, {baud} 8N1]\r\n");
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Błąd portu"); }
            UpdateUi();
        }

        private void ResetLink()
        {
            _carrier = false;
            _dataMode = false;
            _hangupPending = false;
            _acc = "";
            _wait = null;
        }

        private bool Ready()
        {
            if (_modem.IsOpen) return true;
            MessageBox.Show("Najpierw otwórz port.");
            return false;
        }

        /// <summary>Komenda AT ma sens tylko w trybie komend, nie w trakcie rozmowy.</summary>
        private bool CanCommand()
        {
            if (!Ready()) return false;
            if (_script || _transferring) return false;
            if (_dataMode)
            {
                MessageBox.Show("Modem jest w trybie danych. Najpierw użyj +++.");
                return false;
            }
            return true;
        }

        private void Cmd(string c)
        {
            _modem.Command(c);
            Log($"> {c}\r\n");
        }

        private void RefreshControls()
        {
            bool busy = _transferring || _script;
            btnOpen.Enabled = !busy;
            btnSendFile.Enabled = btnRecvFile.Enabled = !busy;
            btnCancel.Enabled = _transferring;
            txtIn.Enabled = !busy;
            btnInit.Enabled = !busy;
            btnDial.Enabled = !busy;
            btnAnswer.Enabled = !busy;
            btnAuto.Enabled = !busy;
            btnEsc.Enabled = !busy;
            btnAto.Enabled = !busy;
            btnHang.Enabled = !busy;
        }

        // ---------- komendy Hayesa ----------

        private async void btnInit_Click(object? sender, EventArgs e)
        {
            if (!CanCommand()) return;
            _script = true;
            RefreshControls();
            try
            {
                foreach (string c in InitCommands)
                {
                    int timeout = c == "ATZ" ? 4000 : 2000;
                    HayesKind kind = await SendAndWait(c, timeout);
                    if (kind != HayesKind.Ok)
                    {
                        Log($"[inicjalizacja przerwana na {c}]\r\n");
                        break;
                    }
                }
            }
            catch (Exception ex) { Log($"[błąd: {ex.Message}]\r\n"); }
            finally
            {
                _script = false;
                RefreshControls();
            }
        }

        private async void btnDial_Click(object? sender, EventArgs e)
        {
            if (!CanCommand()) return;
            _script = true;
            RefreshControls();
            try
            {
                // Łącze bezpośrednie nie ma sygnału centrali (inaczej ATDT → NO DIALTONE).
                HayesKind kind = await SendAndWait("ATX3", 2000);
                if (kind == HayesKind.None) return;
                Cmd("ATDT" + txtPhone.Text.Trim());
            }
            catch (Exception ex) { Log($"[błąd: {ex.Message}]\r\n"); }
            finally
            {
                _script = false;
                RefreshControls();
            }
        }

        private void btnAnswer_Click(object? sender, EventArgs e)
        {
            if (!CanCommand()) return;
            try { Cmd("ATA"); }
            catch (Exception ex) { Log($"[błąd: {ex.Message}]\r\n"); }
        }

        private void btnAuto_Click(object? sender, EventArgs e)
        {
            if (!CanCommand()) return;
            try { Cmd("ATS0=1"); }
            catch (Exception ex) { Log($"[błąd: {ex.Message}]\r\n"); }
        }

        private void btnAto_Click(object? sender, EventArgs e)
        {
            if (!CanCommand()) return;
            try { Cmd("ATO"); }
            catch (Exception ex) { Log($"[błąd: {ex.Message}]\r\n"); }
        }

        private async void btnEsc_Click(object? sender, EventArgs e)
        {
            if (!Ready() || _script || _transferring) return;
            if (!_dataMode)
            {
                Log("[już tryb komend]\r\n");
                return;
            }
            _script = true;
            RefreshControls();
            Log("[+++]\r\n");
            try
            {
                await _modem.EscapeAsync();
                _dataMode = false;
                if (_carrier) Log("[tryb komend, połączenie utrzymane]\r\n");
                UpdateUi();
            }
            catch (Exception ex) { Log($"[błąd: {ex.Message}]\r\n"); }
            finally
            {
                _script = false;
                RefreshControls();
            }
        }

        private async void btnHang_Click(object? sender, EventArgs e)
        {
            if (!Ready() || _script || _transferring) return;
            _script = true;
            RefreshControls();
            try
            {
                if (_dataMode)
                {
                    Log("[+++]\r\n");
                    await _modem.EscapeAsync();
                    _dataMode = false;
                    UpdateUi();
                }
                _hangupPending = true;
                Cmd("ATH");
            }
            catch (Exception ex)
            {
                _hangupPending = false;
                Log($"[błąd: {ex.Message}]\r\n");
            }
            finally
            {
                _script = false;
                RefreshControls();
            }
        }

        private async Task<HayesKind> SendAndWait(string cmd, int timeoutMs)
        {
            int gen = ++_waitSerial;
            var tcs = new TaskCompletionSource<HayesKind>(TaskCreationOptions.RunContinuationsAsynchronously);
            _wait = (gen, tcs);
            try { Cmd(cmd); }
            catch (Exception ex)
            {
                if (_wait is { } current && current.Gen == gen) _wait = null;
                Log($"[błąd: {ex.Message}]\r\n");
                return HayesKind.None;
            }

            Task finished = await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs));
            if (_wait is { } still && still.Gen == gen) _wait = null;
            if (finished != tcs.Task)
            {
                Log($"[brak odpowiedzi na {cmd}]\r\n");
                return HayesKind.None;
            }
            return await tcs.Task;
        }

        // ---------- konwersacja i odpowiedzi modemu ----------

        private void txtIn_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            if (!Ready() || _script || _transferring) return;
            string line = txtIn.Text;
            txtIn.Clear();
            try
            {
                if (_dataMode)
                {
                    _modem.Send(line + "\r\n");
                    Log($"« {line}\r\n");
                }
                else
                {
                    string cmd = line.Trim();
                    if (cmd.Length == 0) return;
                    if (cmd.StartsWith("ATH", StringComparison.OrdinalIgnoreCase))
                        _hangupPending = true;
                    _modem.Command(cmd);
                    Log($"> {cmd}\r\n");
                }
            }
            catch (Exception ex) { Log($"[błąd: {ex.Message}]\r\n"); }
        }

        private void OnText(string s)
        {
            if (IsDisposed) return;
            try
            {
                BeginInvoke(() =>
                {
                    Log(s.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n"));
                    Accept(s);
                });
            }
            catch (InvalidOperationException) { /* formularz już zamknięty */ }
        }

        private void Accept(string chunk)
        {
            _acc += chunk;
            while (true)
            {
                int i = _acc.IndexOfAny(['\r', '\n']);
                if (i < 0) break;
                string line = _acc[..i].Trim();
                _acc = _acc[(i + 1)..];
                if (_acc.StartsWith('\n') || _acc.StartsWith('\r'))
                    _acc = _acc[1..];
                if (line.Length > 0) OnLine(line);
            }
            if (_acc.Length > 400) _acc = _acc[^400..];
        }

        private void OnLine(string line)
        {
            if (line.StartsWith("AT", StringComparison.OrdinalIgnoreCase)) return;

            HayesKind kind = HayesResult.Classify(line, numeric: !_dataMode);
            if (kind == HayesKind.None) return;

            bool loss = kind is HayesKind.NoCarrier or HayesKind.NoDialTone
                or HayesKind.Busy or HayesKind.NoAnswer;

            if (_dataMode)
            {
                if (loss)
                {
                    _carrier = false;
                    _dataMode = false;
                    _hangupPending = false;
                    UpdateUi();
                }
            }
            else
            {
                switch (kind)
                {
                    case HayesKind.Connect:
                        _carrier = true;
                        _dataMode = true;
                        _hangupPending = false;
                        UpdateUi();
                        break;
                    case HayesKind.NoCarrier:
                    case HayesKind.NoDialTone:
                    case HayesKind.Busy:
                    case HayesKind.NoAnswer:
                        _carrier = false;
                        _dataMode = false;
                        _hangupPending = false;
                        UpdateUi();
                        break;
                    case HayesKind.Ok when _hangupPending:
                        _carrier = false;
                        _dataMode = false;
                        _hangupPending = false;
                        UpdateUi();
                        break;
                }
            }

            if (kind is HayesKind.Ok or HayesKind.Error or HayesKind.Connect
                || loss)
                _wait?.Tcs.TrySetResult(kind);
        }

        // ---------- XMODEM ----------

        private void btnSendFile_Click(object? sender, EventArgs e) => Transfer(send: true);
        private void btnRecvFile_Click(object? sender, EventArgs e) => Transfer(send: false);
        private void btnCancel_Click(object? sender, EventArgs e) => _cts?.Cancel();

        private void Transfer(bool send)
        {
            if (!Ready() || _transferring || _script) return;

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
            _transferring = true;
            RefreshControls();
            Log(send ? "[XMODEM: wysyłanie]\r\n" : "[XMODEM: odbieranie]\r\n");

            try { _modem.BeginBinary(); }
            catch (Exception ex)
            {
                _transferring = false;
                RefreshControls();
                Log($"[XMODEM błąd: {ex.Message}]\r\n");
                return;
            }

            Task.Run(() =>
            {
                bool ok = false;
                string end = "[transfer NIEUDANY]\r\n";
                try
                {
                    void Lg(string m) => BeginInvoke(() => Log("[XMODEM] " + m + "\r\n"));
                    if (send)
                    {
                        using var fs = File.OpenRead(path);
                        ok = XModem.Send(_modem, fs, fs.Length, Lg, ct);
                    }
                    else
                    {
                        using var ms = new MemoryStream();
                        ok = XModem.Receive(_modem, ms, Lg, ct);
                        if (ok) File.WriteAllBytes(path, ms.ToArray());
                    }
                    if (ok) end = "[transfer OK]\r\n";
                }
                catch (OperationCanceledException)
                {
                    end = "[XMODEM anulowany]\r\n";
                    try { _modem.Write([0x18, 0x18, 0x18], 0, 3); } catch { }
                }
                catch (Exception ex) { end = $"[XMODEM błąd: {ex.Message}]\r\n"; }
                finally
                {
                    try { _modem.EndBinary(); } catch { }
                    BeginInvoke(() =>
                    {
                        _transferring = false;
                        RefreshControls();
                        Log(end);
                    });
                }
            });
        }

        // ---------- UI ----------

        private void UpdateUi()
        {
            btnOpen.Text = _modem.IsOpen ? "Zamknij port" : "Otwórz port";
            if (!_modem.IsOpen)
            {
                lblStatus.Text = "Port zamknięty";
                lblStatus.ForeColor = Color.Black;
            }
            else if (_dataMode)
            {
                lblStatus.Text = "ONLINE (tryb danych)";
                lblStatus.ForeColor = Color.DarkGreen;
            }
            else if (_carrier)
            {
                lblStatus.Text = "ONLINE (tryb komend)";
                lblStatus.ForeColor = Color.DarkOrange;
            }
            else
            {
                lblStatus.Text = "Tryb komend";
                lblStatus.ForeColor = Color.Black;
            }
            RefreshControls();
        }

        private void Log(string s) => txtLog.AppendText(s);
    }
}
