#include "lexer.h"

char *get_fp(int argc, char **argv)
{
    for (int i = 0; i < argc; ++i)
    {
        if (!strcmp(argv[i], "-i"))
        {
            ++i;
            return argv[i];
        }
    }
    return "main.cbw";
}

char *read_src(char *path)
{
    FILE *source = fopen(path, "rb");
    uint64_t size = 0;
    fseek(source, 0, SEEK_END);
    size = ftell(source);
    fseek(source, 0, SEEK_SET);
    char *src = calloc(size + 1, 1);
    uint64_t _ = fread(src, size, 1, source);
    fclose(source);
    return src;
}

typedef enum
{
    NO_TYPE,
} node_type;

typedef struct node
{
    node_type type;
    token_t token;
    struct node *children;
} node_t;

typedef struct
{
    token_t *tokens;
    uint64_t num_tok;
    uint64_t pos;
} parser_t;

#define PARSER_CURR (parser->tokens[parser->pos])
#define NODE(type, token) ((node_t){type, token, 0})

node_t parse()
{
}

int main(int argc, char **argv)
{
    char *path = get_fp(argc, argv);
    char *src = read_src(path);
    lexer_t lexer = LEXER(src);
    token_t *tokens = calloc(1, sizeof(token_t));
    token_t tok = lex(&lexer);
    int i = 0;
    while (true)
    {
        printf("'%s': %d\n", tok.text, tok.type);
        tokens[i] = tok;
        tok = lex(&lexer);
        ++i;
        tokens = realloc(tokens, (i + 1) * sizeof(token_t));
        if (tok.type == END_OF_FILE)
        {
            break;
        }
    }
}