using MpvNet.ExtensionMethod;
using System.Reflection;

namespace MpvNet;

public class ExtensionLoader
{
    public event Action<Exception>? UnhandledException;

    public readonly List<object?> Refs = new();

    private void LoadDll(string path)
    {
        if (!File.Exists(path))
            return;

        try
        {
            var asm  = Assembly.LoadFile(path);
            var type = asm.GetTypes().Where(typeof(IExtension).IsAssignableFrom).First();
            Refs.Add(Activator.CreateInstance(type));
        }
        catch (Exception ex)
        {
            UnhandledException?.Invoke(ex);
        }
    }

    public void LoadFolder(string path)
    {
        if (!Directory.Exists(path)) return;
        foreach (var dir in Directory.GetDirectories(path))
            LoadDll(dir.AddSep() + Path.GetFileName(dir) + ".dll");
    }
}

public interface IExtension
{
}
