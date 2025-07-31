#ifndef LEXER_H
#define LEXER_H
#include <stdio.h>
#include <stdlib.h>
#include <stdint.h>
#include <string.h>
#include <stdbool.h>

typedef struct
{
    int pos;
    int line, column;
    char *src;
} lexer_t;

typedef enum
{
    END_OF_FILE,
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
} token_type;

typedef struct
{
    token_type type;
    char *text;
    int column, line;
} token_t;

#define LEXER_CURR ((lexer->pos < strlen(lexer->src)) ? lexer->src[lexer->pos] : 0)
#define LEXER(src) ((lexer_t){0, 1, 0, src})

void lexer_next(lexer_t *lexer);

bool is_letter(char c);

bool is_number(char c);

token_t lex_id(lexer_t *lexer);

token_t lex_num(lexer_t *lexer);

token_t lex(lexer_t *lexer);

#endif