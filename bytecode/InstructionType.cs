namespace Cobweb{
    public enum InstructionType
    {
        NOP,
        PUSH,
        ADD,
        SUB,
        MUL,
        DIV,
        CMP_MORE,
        CMP_LESS,
        CMP_EQ,
        CMP_NOTEQ,
        CMP_LESSEQ,
        CMP_MOREEQ,
        CALL,
        JMP,
        LABEL,
        CONDITIONAL_JUMP,
        ARG_DECL,
        RETURN,
        LIST_INIT,
        LIST_APPEND,
        LIST_EXPAND,
        INDEX,
    }
}