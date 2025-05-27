namespace Cobweb{
    public enum NodeType
    {
        no_type,
        program,
        function,
        expr,
        literal,
        binexpr,
        function_call,
        _if,
        _else,
        brace,
        func_def__args,
        index,
        list_initializer,
    }
}