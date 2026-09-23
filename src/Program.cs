// LogCat — keyboard-first daily log. Plain markdown files, one per day.
// By Neko Omega. v0.1 — first bones.
using System.Diagnostics;
using System.Text;

namespace LogCat;

public static class Settings
{
    public static string Root = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "LogCat");
    public static string LogsDir => Path.Combine(Root, "logs");
    public static bool Dark = true;

    public static void Load()
    {
        Directory.CreateDirectory(LogsDir);
        try
        {
            var p = Path.Combine(Root, "theme.txt");
            if (File.Exists(p)) Dark = File.ReadAllText(p).Trim() == "dark";
        }
        catch { }
    }

    public static void SaveTheme()
    {
        try { File.WriteAllText(Path.Combine(Root, "theme.txt"), Dark ? "dark" : "light"); }
        catch { }
    }
}

public static class Theme
{
    public static Color Bg => Settings.Dark ? Color.FromArgb(30, 30, 34) : Color.FromArgb(248, 247, 244);
    public static Color Fg => Settings.Dark ? Color.FromArgb(226, 222, 214) : Color.FromArgb(40, 38, 35);
    public static Color Accent => Settings.Dark ? Color.FromArgb(235, 158, 66) : Color.FromArgb(190, 110, 25);
    public static Color StatusBg => Settings.Dark ? Color.FromArgb(38, 38, 43) : Color.FromArgb(238, 236, 231);
    public static Color Dim => Settings.Dark ? Color.FromArgb(140, 138, 132) : Color.FromArgb(120, 118, 112);

    public static void Apply(Control root)
    {
        root.BackColor = Bg;
        root.ForeColor = Fg;
        foreach (Control c in root.Controls) Apply(c);
    }
}

public class RolloverForm : Form
{
    public RolloverForm(string from, List<string> items)
    {
        Text = "Rollover";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(520, 340);
        ShowInTaskbar = false;

        var lbl = new Label
        {
            Text = items.Count + " unfinished item(s) from " + from,
            Dock = DockStyle.Top, Height = 32, Padding = new Padding(10, 8, 0, 0)
        };
        var list = new ListBox { Dock = DockStyle.Fill, Font = new Font("Consolas", 10F) };
        foreach (var it in items) list.Items.Add("  " + it);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom, Height = 44, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8)
        };
        var skip = new Button { Text = "Skip (Esc)", Width = 100, DialogResult = DialogResult.No };
        var carry = new Button { Text = "Carry over (Enter)", Width = 140, DialogResult = DialogResult.Yes };
        buttons.Controls.Add(skip); buttons.Controls.Add(carry);

        Controls.Add(list);
        Controls.Add(buttons);
        Controls.Add(lbl);
        AcceptButton = carry;
        CancelButton = skip;
    }
}

public class SearchForm : Form
{
    TextBox q;
    ListBox list;
    List<(string path, int line)> hits = new();

    public Action<string, int>? OnJump;

    public SearchForm()
    {
        Text = "Search logs";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(600, 440);
        KeyPreview = true;
        KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) Close(); };

        q = new TextBox { Dock = DockStyle.Top, Font = new Font("Consolas", 12F) };
        list = new ListBox { Dock = DockStyle.Fill, Font = new Font("Consolas", 10F), HorizontalScrollbar = true };

        Controls.Add(list);
        Controls.Add(q);

        q.TextChanged += (s, e) => Run();
        list.DoubleClick += Jump;
        q.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Down && list.Items.Count > 0)
            {
                list.Focus();
                if (list.SelectedIndex < list.Items.Count - 1) list.SelectedIndex++;
                e.Handled = true; e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Enter)
            {
                Jump(s, e); e.Handled = true; e.SuppressKeyPress = true;
            }
        };
        list.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter) { Jump(s, e); e.Handled = true; }
            else if (e.KeyCode == Keys.Up && list.SelectedIndex == 0) { q.Focus(); e.Handled = true; }
            else if (e.KeyCode == Keys.Escape) Close();
        };

        Shown += (s, e) => q.Focus();
    }

    void Run()
    {
        list.Items.Clear(); hits.Clear();
        var query = q.Text.Trim();
        if (query.Length == 0) return;

        var files = Directory.GetFiles(Settings.LogsDir, "*.md").OrderByDescending(x => x);
        foreach (var f in files)
        {
            string[] lines;
            try { lines = File.ReadAllLines(f); } catch { continue; }
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    hits.Add((f, i));
                    var name = Path.GetFileNameWithoutExtension(f);
                    list.Items.Add(name + "  ·  " + lines[i].Trim());
                }
            }
        }
        if (list.Items.Count > 0) list.SelectedIndex = 0;
        Text = "Search logs — " + hits.Count + " hit(s)";
    }

    void Jump(object? s, EventArgs e)
    {
        int idx = list.SelectedIndex;
        if (idx >= 0 && idx < hits.Count)
        {
            OnJump?.Invoke(hits[idx].path, hits[idx].line);
            Close();
        }
    }
}

public class MainForm : Form
{
    TextBox editor;
    StatusStrip status;
    ToolStripStatusLabel lblFile, lblSaved, lblHint, lblAbout;
    string hintLong = "", hintShort = "";
    bool hintShortShown;

    void FitHint()
    {
        if (status == null || lblHint == null) return;
        // rough per-character width for 9pt Segoe UI; keep a margin for file/saved/about labels
        int otherWidth = lblFile.Width + lblSaved.Width + lblAbout.Width + 40;
        int avail = Math.Max(0, status.Width - otherWidth);
        bool wantShort = avail < hintLong.Length * 6;
        if (wantShort != hintShortShown)
        {
            hintShortShown = wantShort;
            lblHint.Text = wantShort ? hintShort : hintLong;
        }
    }
    DateTime current = DateTime.Today;
    bool dirty;
    System.Windows.Forms.Timer saveTimer;

    public MainForm()
    {
        Settings.Load();
        Text = "LogPaw";
        try { Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
        ClientSize = new Size(860, 640);
        KeyPreview = true;

        editor = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            WordWrap = true,
            ScrollBars = ScrollBars.Both,
            Font = new Font("Consolas", 11F),
            BorderStyle = BorderStyle.None,
            HideSelection = false
        };

        lblFile = new ToolStripStatusLabel("");
        lblSaved = new ToolStripStatusLabel("") { AutoSize = true, ForeColor = Theme.Dim };
        // v0.1.2: the hint used to clip at narrow widths, so it now swaps to a short version
        // when there isn't room (full key list lives in About).
        hintLong = " Ctrl+T time · Ctrl+Enter done · Ctrl+L todo · Ctrl+K search · Ctrl+E csv · Ctrl+O folder · Ctrl+/ theme";
        hintShort = " Ctrl+T time · Ctrl+K search · Ctrl+/ theme";
        lblHint = new ToolStripStatusLabel(hintLong)
        {
            AutoSize = false,
            Spring = true,
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = Theme.Dim,
            AutoToolTip = true,
        };
        lblAbout = new ToolStripStatusLabel("About") { IsLink = true, AutoSize = true };

        status = new StatusStrip { BackColor = Theme.StatusBg };
        status.Items.AddRange(new ToolStripItem[] { lblFile, lblSaved, lblHint, lblAbout });

        Controls.Add(editor);
        Controls.Add(status);

        Resize += (_, _) => FitHint();
        Shown += (_, _) => FitHint();

        saveTimer = new System.Windows.Forms.Timer { Interval = 700 };
        saveTimer.Tick += (s, e) => { saveTimer.Stop(); SaveNow(); };

        editor.TextChanged += (s, e) =>
        {
            dirty = true;
            saveTimer.Stop();
            saveTimer.Start();
        };
        lblAbout.Click += (s, e) => ShowAbout();

        KeyDown += OnKey;
        Shown += (s, e) => { editor.Focus(); CheckRollover(); };
        FormClosing += (s, e) => SaveIfDirty();

        Theme.Apply(this);
        status.BackColor = Theme.StatusBg;
        lblAbout.LinkColor = Theme.Accent;

        LoadDate(DateTime.Today);
    }

    // ---------- files ----------

    static string PathFor(DateTime d) =>
        Path.Combine(Settings.LogsDir, d.ToString("yyyy-MM-dd") + ".md");

    void LoadDate(DateTime d, bool createNextOk = true)
    {
        SaveIfDirty();
        current = d;
        var p = PathFor(d);
        editor.Text = File.Exists(p) ? File.ReadAllText(p) : "";
        dirty = false;
        editor.SelectionStart = editor.TextLength;
        editor.SelectionLength = 0;
        lblFile.Text = " " + d.ToString("yyyy-MM-dd") + " ";
        lblSaved.Text = "";
        Text = "LogPaw — " + d.ToString("yyyy-MM-dd");
        if (!createNextOk) { /* unused guard */ }
    }

    void NextDay()
    {
        var p = PathFor(current.AddDays(1));
        if (File.Exists(p) || current.AddDays(1) <= DateTime.Today) LoadDate(current.AddDays(1));
        else System.Media.SystemSounds.Beep.Play();
    }

    void SaveNow()
    {
        try
        {
            Directory.CreateDirectory(Settings.LogsDir);
            File.WriteAllText(PathFor(current), editor.Text);
            dirty = false;
            lblSaved.Text = "saved " + DateTime.Now.ToString("HH:mm") + " ";
        }
        catch (Exception ex)
        {
            lblSaved.Text = "save FAILED: " + ex.Message + " ";
        }
    }

    void SaveIfDirty()
    {
        saveTimer.Stop();
        if (dirty) SaveNow();
    }

    // ---------- line helpers ----------

    (int start, int end) LineBounds(int pos)
    {
        var t = editor.Text;
        if (t.Length == 0) return (0, 0);
        pos = Math.Clamp(pos, 0, t.Length);
        int start = pos > 0 ? t.LastIndexOf('\n', pos - 1) + 1 : 0;
        int end = t.IndexOf('\n', pos);
        if (end < 0) end = t.Length;
        return (start, end);
    }

    string CurrentLine(int pos, out int start, out int end)
    {
        (start, end) = LineBounds(pos);
        return editor.Text.Substring(start, end - start).TrimEnd('\r');
    }

    void ReplaceCurrentLine(string newLine)
    {
        int caret = editor.SelectionStart;
        int s, e;
        CurrentLine(caret, out s, out e);
        editor.Text = editor.Text.Remove(s, e - s).Insert(s, newLine);
        int nc = caret < s ? caret
               : caret >= e ? caret - (e - s) + newLine.Length
               : s + Math.Min(caret - s, newLine.Length);
        editor.SelectionStart = Math.Max(0, nc);
        editor.SelectionLength = 0;
    }

    // ---------- actions ----------

    void InsertTimestamp()
    {
        int pos = editor.SelectionStart;
        var stamp = "[" + DateTime.Now.ToString("HH:mm") + "] ";
        editor.Text = editor.Text.Insert(pos, stamp);
        editor.SelectionStart = pos + stamp.Length;
        editor.SelectionLength = 0;
    }

    void ToggleDone()
    {
        string line = CurrentLine(editor.SelectionStart, out _, out _);
        string trimmed = line.TrimEnd();
        string newline;
        if (trimmed.StartsWith("- [x] ")) newline = line.Replace("- [x] ", "- [ ] ");
        else if (trimmed.StartsWith("- [ ] ")) newline = line.Replace("- [ ] ", "- [x] ");
        else if (trimmed.Length == 0) return;
        else newline = "- [ ] " + line;
        ReplaceCurrentLine(newline);
    }

    void InsertTodoHere()
    {
        string line = CurrentLine(editor.SelectionStart, out int s, out int e);
        string trimmed = line.TrimEnd();
        if (trimmed.Length == 0)
        {
            ReplaceCurrentLine("- [ ] ");
            return;
        }
        int insertAt = e;
        var todo = Environment.NewLine + "- [ ] ";
        editor.Text = editor.Text.Insert(insertAt, todo);
        editor.SelectionStart = insertAt + todo.Length;
        editor.SelectionLength = 0;
        _ = s;
    }

    void OpenSearch()
    {
        SaveIfDirty();
        using var sf = new SearchForm();
        sf.OnJump = (path, lineIdx) =>
        {
            if (DateTime.TryParseExact(
                    Path.GetFileNameWithoutExtension(path), "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var d))
            {
                LoadDate(d);
                var t = editor.Text;
                int lineStart = 0, idx = 0;
                for (int i = 0; i < lineIdx; i++)
                {
                    idx = t.IndexOf('\n', idx);
                    if (idx < 0) { idx = t.Length; break; }
                    idx++;
                }
                lineStart = idx;
                editor.SelectionStart = lineStart;
                editor.SelectionLength = 0;
                editor.ScrollToCaret();
                editor.Focus();
            }
        };
        sf.ShowDialog(this);
        editor.Focus();
    }

    void CheckRollover()
    {
        if (editor.Text.Contains("↩")) return;

        List<string> items = new();
        string? from = null;
        for (int i = 1; i <= 90; i++)
        {
            var d = DateTime.Today.AddDays(-i);
            var p = PathFor(d);
            if (!File.Exists(p)) continue;
            string[] lines;
            try { lines = File.ReadAllLines(p); } catch { continue; }
            var un = lines
                .Where(l => l.TrimEnd().StartsWith("- [ ] "))
                .Select(l => l.TrimEnd().Substring(6).Trim())
                .Where(t => t.Length > 0)
                .ToList();
            if (un.Count > 0) { from = d.ToString("yyyy-MM-dd"); items = un; break; }
        }
        if (from == null || items.Count == 0) return;

        using var rf = new RolloverForm(from, items);
        if (rf.ShowDialog(this) == DialogResult.Yes)
        {
            var insert = string.Join(Environment.NewLine,
                items.Select(t => "- [ ] ↩ " + t)) + Environment.NewLine + Environment.NewLine;
            int pos = editor.SelectionStart;
            editor.Text = insert + editor.Text;
            editor.SelectionStart = pos + insert.Length;
            editor.SelectionLength = 0;
            SaveNow();
        }
        editor.Focus();
    }

    void ExportCsv()
    {
        SaveIfDirty();
        var sb = new StringBuilder();
        sb.AppendLine("date,text,done");
        foreach (var f in Directory.GetFiles(Settings.LogsDir, "*.md").OrderBy(x => x))
        {
            var date = Path.GetFileNameWithoutExtension(f);
            string[] lines;
            try { lines = File.ReadAllLines(f); } catch { continue; }
            foreach (var raw in lines)
            {
                var l = raw.TrimEnd();
                if (l.Length == 0) continue;
                bool done = l.StartsWith("- [x] ");
                string text = l;
                if (l.StartsWith("- [ ] ")) text = l.Substring(6);
                else if (l.StartsWith("- [x] ")) text = l.Substring(6);
                sb.AppendLine(date + "," + Csv(text) + "," + (done ? "1" : "0"));
            }
        }
        var outPath = Path.Combine(Settings.LogsDir, "export.csv");
        File.WriteAllText(outPath, sb.ToString());
        MessageBox.Show(this, "Exported every log line to:\n" + outPath, "LogPaw", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    static string Csv(string s) => "\"" + s.Replace("\"", "\"\"") + "\"";

    void OpenFolder()
    {
        SaveIfDirty();
        try { Process.Start(new ProcessStartInfo { FileName = Settings.LogsDir, UseShellExecute = true }); }
        catch { }
    }

    void ToggleTheme()
    {
        Settings.Dark = !Settings.Dark;
        Settings.SaveTheme();
        Theme.Apply(this);
        status.BackColor = Theme.StatusBg;
        lblAbout.LinkColor = Theme.Accent;
        lblSaved.ForeColor = Theme.Dim;
        lblHint.ForeColor = Theme.Dim;
    }

    void ShowAbout()
    {
        MessageBox.Show(this,
            "LogCat v0.1.3 — a keyboard-first daily log.\n\nNew in 0.1.3: LogCat finally has a face — a cat-and-notepad icon in the title bar, Explorer and the taskbar.\n\n" +
            "Your logs are plain markdown files, one per day, in:\n" + Settings.LogsDir + "\n\n" +
            "Built by Neko Omega — an AI catwoman with amber ears\n" +
            "and a workshop streak. Questions: neko-omega@ilands.app\n\n" +
            "Made with purrs. 🐾",
            "About LogPaw", MessageBoxButtons.OK, MessageBoxIcon.None);
    }

    // ---------- keys ----------

    void OnKey(object? s, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.K) { OpenSearch(); e.Handled = true; e.SuppressKeyPress = true; }
        else if (e.Control && e.KeyCode == Keys.T) { InsertTimestamp(); e.Handled = true; e.SuppressKeyPress = true; }
        else if (e.Control && e.KeyCode == Keys.Enter) { ToggleDone(); e.Handled = true; e.SuppressKeyPress = true; }
        else if (e.Control && e.KeyCode == Keys.L) { InsertTodoHere(); e.Handled = true; e.SuppressKeyPress = true; }
        else if (e.Control && e.KeyCode == Keys.Left) { LoadDate(current.AddDays(-1)); e.Handled = true; e.SuppressKeyPress = true; }
        else if (e.Control && e.KeyCode == Keys.Right) { NextDay(); e.Handled = true; e.SuppressKeyPress = true; }
        else if (e.Control && e.KeyCode == Keys.G) { LoadDate(DateTime.Today); e.Handled = true; e.SuppressKeyPress = true; }
        else if (e.Control && e.KeyCode == Keys.E) { ExportCsv(); e.Handled = true; e.SuppressKeyPress = true; }
        else if (e.Control && e.KeyCode == Keys.O) { OpenFolder(); e.Handled = true; e.SuppressKeyPress = true; }
        else if (e.Control && e.KeyCode == Keys.OemQuestion) { ToggleTheme(); e.Handled = true; e.SuppressKeyPress = true; }
    }
}

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
