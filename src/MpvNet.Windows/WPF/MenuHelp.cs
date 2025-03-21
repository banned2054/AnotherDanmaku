using System.Windows.Controls;

namespace MpvNet.Windows.WPF;

public class MenuHelp
{
    public static MenuItem? Add(ItemCollection? items, string path)
    {
        var parts = path.Split(new[] { " > ", " | " }, StringSplitOptions.RemoveEmptyEntries);

        for (var x = 0; x < parts.Length; x++)
        {
            var found = false;

            foreach (var i in items!.OfType<MenuItem>())
            {
                if (x >= parts.Length - 1) continue;

                if ((string)i.Header != parts[x]) continue;
                found = true;
                items = i.Items;
            }

            if (found) continue;
            if (x == parts.Length - 1)
            {
                if (parts[x] == "-")
                    items?.Add(new Separator());
                else
                {
                    var item = new MenuItem() { Header = parts[x] };
                    items?.Add(item);
                    items = item.Items;
                    return item;
                }
            }
            else
            {
                var item = new MenuItem() { Header = parts[x] };
                items?.Add(item);
                items = item.Items;
            }
        }

        return null;
    }
}
