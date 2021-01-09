namespace Fae.Runtime
{
    public struct Result<T>
    {
        public T Value;
        public IStruct Effect;
        public IStruct Code;
    }
}