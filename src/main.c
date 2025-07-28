#include <stdio.h>
#include <stdlib.h>
#include <stdint.h>

typedef struct
{
    int pos;
    int line, column;
    char *src;
} lexer_t;

typedef enum
{
    ID,
    NUMBER,
    MINUS,
    PLUS,
    DIVIDE,
    MULTIPLY,
    ARROW,
    BOOLEQ,
    BOOLNOTEQ,
    NOT,
    SQUARE_OPEN,
    SQUARE_CLOSE,
    BRACE_OPEN,
    BRACE_CLOSE,
    BOOLMORE,
    BOOLLESS,
    BOOLLESSEQ,
    BOOLMOREEQ,
    STRING,
    COMMA,
    END_OF_FILE
} token_type;

typedef struct
{
    token_type type;
    char *text;
    int column, line;
} token_t;

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

#define LEXER_CURR (lexer->src[lexer->pos])

token_t lex(lexer_t *lexer)
{
    switch (LEXER_CURR)
    {
    case ' ':
    case '\n':
    case '\t':
    {
        lexer_next(lexer);
        return lex(lexer);
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
        return lex(lexer);
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
    }
}
int main(void)
{
}