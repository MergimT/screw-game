using System.Collections.Generic;
using System.IO;
using ScrewGame.Contracts;

namespace ScrewGame.Persistence
{
    /// <summary>File storage using write-temp, flush, then rename-replace so each named record is all-or-nothing.</summary>
    public sealed class FileDurableStorage : IDurableStorage
    {
        private readonly string _root;

        public FileDurableStorage(string root)
        {
            _root = root;
            Directory.CreateDirectory(root);
        }

        private string PathOf(string name) => Path.Combine(_root, name);

        public bool TryRead(string name, out byte[] data)
        {
            var p = PathOf(name);
            if (!File.Exists(p)) { data = null; return false; }
            data = File.ReadAllBytes(p);
            return true;
        }

        public void WriteAtomic(string name, byte[] data)
        {
            var path = PathOf(name);
            var tmp = path + ".tmp";
            using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                fs.Write(data, 0, data.Length);
                fs.Flush(true);
            }
            if (File.Exists(path)) File.Replace(tmp, path, null);
            else File.Move(tmp, path);
        }

        public void Delete(string name)
        {
            var p = PathOf(name);
            if (File.Exists(p)) File.Delete(p);
        }
    }

    /// <summary>In-memory storage for tests and editor tools, with optional fault injection.</summary>
    public sealed class MemoryStorage : IDurableStorage
    {
        public readonly Dictionary<string, byte[]> Files = new Dictionary<string, byte[]>();
        public int FailWritesRemaining;
        public string FailOnlyName;

        public bool TryRead(string name, out byte[] data)
        {
            if (Files.TryGetValue(name, out var d)) { data = (byte[])d.Clone(); return true; }
            data = null;
            return false;
        }

        public void WriteAtomic(string name, byte[] data)
        {
            if (FailWritesRemaining > 0 && (FailOnlyName == null || FailOnlyName == name))
            {
                FailWritesRemaining--;
                throw new IOException("injected write failure: " + name);
            }
            Files[name] = (byte[])data.Clone();
        }

        public void Delete(string name) => Files.Remove(name);
    }
}
