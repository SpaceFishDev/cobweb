#include "lexer.h"

void lexer_next(lexer_t *lexer)
{
    lexer->column++;
    if (lexer->src[lexer->pos] == '\n')
    {
        lexer->line++;
        lexer->column = 0;
    }
    lexer->pos++;
}

bool is_letter(char c)
{
    return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
}
bool is_number(char c)
{
    return c >= '0' && c <= '9';
}

token_t lex_id(lexer_t *lexer)
{
    int p = lexer->pos;
    lexer_next(lexer);
    while (is_letter(LEXER_CURR) || LEXER_CURR == '_')
    {
        lexer_next(lexer);
    }
    int len = lexer->pos - p;
    char *text = calloc(len + 1, 1);
    lexer->pos = p;
    int i = 0;
    while (is_letter(LEXER_CURR) || LEXER_CURR == '_')
    {
        text[i] = LEXER_CURR;
        lexer_next(lexer);
        ++i;
    }
    return (token_t){ID, text, lexer->column, lexer->line};
}

token_t lex_num(lexer_t *lexer)
{
    int p = lexer->pos;
    bool decimal = false;
    while (is_number(LEXER_CURR) || (!decimal && LEXER_CURR == '.'))
    {
        if (LEXER_CURR == '.')
        {
            decimal = true;
        }
        lexer_next(lexer);
    }
    int len = lexer->pos - p;
    lexer->pos = p;
    char *text = calloc(len + 1, 1);
    decimal = false;
    int i = 0;
    while (i < len)
    {
        text[i] = LEXER_CURR;
        lexer_next(lexer);
        ++i;
    }
    return (token_t){NUMBER, text, lexer->column, lexer->line};
}

token_t lex(lexer_t *lexer)
{
    if (is_letter(LEXER_CURR) || LEXER_CURR == '_')
    {
        return lex_id(lexer);
    }
    if (is_number(LEXER_CURR))
    {
        return lex_num(lexer);
    }
    switch (LEXER_CURR)
    {
    case ' ':
    case '\n':
    case '\t':
    {
        lexer_next(lexer);
        return lex(lexer);
    }
    case '"':
    {
        lexer_next(lexer);
        int p = lexer->pos;
        while (LEXER_CURR != '"')
        {
            lexer_next(lexer);
        }
        int len = lexer->pos - p;
        lexer->pos = p;
        int i = 0;
        char *text = calloc(len + 1, 1);
        while (LEXER_CURR != '"')
        {
            text[i] = LEXER_CURR;
            ++i;
            lexer_next(lexer);
        }
        return (token_t){STRING, text, lexer->column, lexer->line};
    }
    case '(':
    {
        token_t res = (token_t){BRACE_OPEN, "(", lexer->column, lexer->line};
        lexer_next(lexer);
        return res;
    }
    case ')':
    {
        token_t res = (token_t){BRACE_CLOSE, ")", lexer->column, lexer->line};
        lexer_next(lexer);
        return res;
    }
    case '[':
    {
        token_t res = (token_t){SQUARE_OPEN, "[", lexer->column, lexer->line};
        lexer_next(lexer);
        return res;
    }
    case ']':
    {
        token_t res = (token_t){SQUARE_CLOSE, "]", lexer->column, lexer->line};
        lexer_next(lexer);
        return res;
    }
    case '-':
    {
        token_t res = (token_t){MINUS, "-", lexer->column, lexer->line};
        lexer_next(lexer);
        return res;
    }
    case '+':
    {
        token_t res = (token_t){PLUS, "+", lexer->column, lexer->line};
        lexer_next(lexer);
        return res;
    }
    case '*':
    {
        token_t res = (token_t){MULTIPLY, "*", lexer->column, lexer->line};
        lexer_next(lexer);
        return res;
    }
    case '/':
    {
        token_t res = (token_t){DIVIDE, "/", lexer->column, lexer->line};
        lexer_next(lexer);
        return res;
    }
    case '=':
    {
        lexer_next(lexer);
        if (LEXER_CURR == '>')
        {
            token_t res = (token_t){ARROW, "=>", lexer->column, lexer->line};
            lexer_next(lexer);
            return res;
        }
        if (LEXER_CURR == '=')
        {
            token_t res = (token_t){BOOLEQ, "==", lexer->column, lexer->line};
            lexer_next(lexer);
            return res;
        }
        return lex(lexer);
    }
    case '!':
    {
        lexer_next(lexer);
        if (LEXER_CURR == '=')
        {
            token_t res = (token_t){BOOLNOTEQ, "!=", lexer->column, lexer->line};
            lexer_next(lexer);
            return res;
        }
        return (token_t){NOT, "!", lexer->column, lexer->line};
    }
    case '>':
    {
        lexer_next(lexer);
        if (LEXER_CURR == '=')
        {
            token_t res = (token_t){BOOLMOREEQ, ">=", lexer->column, lexer->line};
            lexer_next(lexer);
            return res;
        }
        return (token_t){BOOLMORE, ">", lexer->column, lexer->line};
    }
    case '<':
    {
        lexer_next(lexer);
        if (LEXER_CURR == '=')
        {
            token_t res = (token_t){BOOLLESSEQ, "<=", lexer->column, lexer->line};
            lexer_next(lexer);
            return res;
        }
        return (token_t){BOOLLESS, "<", lexer->column, lexer->line};
    }
    case '#':
    {
        lexer_next(lexer);
        while (LEXER_CURR != '\n' && LEXER_CURR != '#')
        {
            lexer_next(lexer);
        }
        lexer_next(lexer);
        return lex(lexer);
    }
    }
    return (token_t){END_OF_FILE, "\0", lexer->column, lexer->line};
}