using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ClipHistory;

public sealed class ClipboardEntry
{
    public string Text { get; set; } = "";
    public DateTime Time { get; set; }
    public bool Pinned { get; set; }
}

public sealed class ClipboardHistory
{
    private readonly List<ClipboardEntry> _items = new();
    private readonly int _maxItems;
    private readonly string _filePath;
    private readonly object _lock = new();

    public event Action? Changed;

    public ClipboardHistory(int maxItems = 200)
    {
        _maxItems = maxItems;
        _filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ClipHistory", "history.json");
        Load();
    }

    public IReadOnlyList<ClipboardEntry> Items
    {
        get
        {
            lock (_lock)
            {
                return _items
                    .OrderByDescending(e => e.Pinned)
                    .ThenByDescending(e => e.Time)
                    .ToList();
            }
        }
    }

    public int PinnedCount
    {
        get { lock (_lock) return _items.Count(e => e.Pinned); }
    }

    public void Add(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        if (text.Length > 100_000) return;

        lock (_lock)
        {
            var existing = _items.FirstOrDefault(e => e.Text == text);
            if (existing != null)
            {
                existing.Time = DateTime.Now;
                // Pinned 状态保持不变
            }
            else
            {
                _items.Add(new ClipboardEntry { Text = text, Time = DateTime.Now });
            }

            // 淘汰：只淘汰"未收藏"的超额部分
            var nonPinned = _items
                .Where(e => !e.Pinned)
                .OrderByDescending(e => e.Time)
                .ToList();

            if (nonPinned.Count > _maxItems)
            {
                var toRemove = nonPinned.Skip(_maxItems).ToList();
                foreach (var e in toRemove) _items.Remove(e);
            }
        }

        Save();
        Changed?.Invoke();
    }

    public void TogglePin(ClipboardEntry entry)
    {
        lock (_lock)
        {
            var e = _items.FirstOrDefault(x => x.Text == entry.Text);
            if (e == null) return;
            e.Pinned = !e.Pinned;
        }
        Save();
        Changed?.Invoke();
    }

    public void Remove(ClipboardEntry entry)
    {
        lock (_lock) _items.RemoveAll(x => x.Text == entry.Text);
        Save();
        Changed?.Invoke();
    }

    public void Clear(bool keepPinned = true)
    {
        lock (_lock)
        {
            if (keepPinned) _items.RemoveAll(e => !e.Pinned);
            else _items.Clear();
        }
        Save();
        Changed?.Invoke();
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_filePath)) return;
            var json = File.ReadAllText(_filePath);
            var list = JsonSerializer.Deserialize<List<ClipboardEntry>>(json);
            if (list != null) _items.AddRange(list);
        }
        catch { }
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            List<ClipboardEntry> snapshot;
            lock (_lock) snapshot = _items.ToList();
            File.WriteAllText(_filePath,
                JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = false }));
        }
        catch { }
    }
}