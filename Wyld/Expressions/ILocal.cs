namespace Wyld.Expressions
{
    public interface ILocal : IExpression
    {
        public string Name { get; }
    }
}