namespace Cobweb{
    public class Arithmetic
    {
        public ArithmeticType Type;
        public Node AssociatedNode;
        public Arithmetic(ArithmeticType type, Node asNode)
        {
            Type = type;
            AssociatedNode = asNode;
        }
    }
}