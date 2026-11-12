namespace GameSystem.Core.Text;

/// <summary>
/// VISITOR over the structured-text tree. One overload per concrete node type, so an operation over the
/// whole document (rendering, statistics, export...) lives in one class outside the tree, and adding a new
/// node type turns every visitor that forgot to handle it into a compile error instead of a runtime failure.
/// </summary>
public interface ITextVisitor
{
    void Visit(Root root);
    void Visit(Heading heading);
    void Visit(Paragraph paragraph);
    void Visit(Link link);
}
