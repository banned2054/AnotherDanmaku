namespace MpvNet.Windows.UI;

public class TreeNode
{
    private readonly List<TreeNode> _children = new();

    public IList<TreeNode> Children => _children;

    public string Name { get; set; } = "";
}
