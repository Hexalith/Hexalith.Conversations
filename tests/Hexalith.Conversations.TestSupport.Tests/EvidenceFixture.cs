using System.Security.Cryptography;
using System.Text;

namespace Hexalith.Conversations.TestSupport.Tests;

internal sealed class EvidenceFixture : IDisposable
{
    internal EvidenceFixture()
    {
        Root = Path.Combine(Path.GetTempPath(), "conversations-evidence-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
    }

    internal string Root { get; }

    internal string Write(string path, string content)
    {
        string full = Path.Combine(Root, path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content, new UTF8Encoding(false));
        return full;
    }

    internal static string Hash(string path) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));

    internal static string FindRepositoryRoot()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "Hexalith.Conversations.slnx"))) return directory.FullName;
        throw new DirectoryNotFoundException("The Conversations repository root was not found.");
    }

    public void Dispose() => GitFixture.DeleteTree(Root);
}
