using CommunityToolkit.Mvvm.ComponentModel;
using MpvNet.Windows.UI;

namespace MpvNet.Windows.WPF.ViewModels;

public class NodeViewModel : ObservableObject
{
    private readonly TreeNode _node;

    private bool _isExpanded;
    private bool _isSelected;

    public NodeViewModel(TreeNode node) : this(node, null)
    {
    }

    public NodeViewModel(TreeNode node, NodeViewModel? parent)
    {
        _node  = node;
        Parent = parent;

        Children = new List<NodeViewModel>(
                                           _node.Children.Select(i => new NodeViewModel(i, this)).ToList());
    }

    public List<NodeViewModel> Children { get; }

    public string Name => _node.Name;

    public string Path
    {
        get
        {
            var path   = Name;
            var parent = Parent;

            while (!string.IsNullOrEmpty(parent?.Name))
            {
                path   = parent.Name + "/" + path;
                parent = parent.Parent;
            }

            return path;
        }
    }

    public NodeViewModel? Parent { get; }

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            SetProperty(ref _isExpanded, value);

            if (_isExpanded && Parent != null)
                Parent.IsExpanded = true;
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public bool NameContains(string text)
    {
        if (text == "")
            return false;

        return Name.IndexOf(text, StringComparison.InvariantCultureIgnoreCase) > -1;
    }
}
