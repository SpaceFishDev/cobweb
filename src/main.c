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
    FUNCTION,
    FARGS,
    EXPRESSION,
    PROGRAM,
    BASIC,
    UNARY,
    BRACE,
    BINEXPR,
    FUNCTION_CALL,
} node_type;

const char *node_type_to_string(node_type type)
{
    switch (type)
    {
    case NO_TYPE:
        return "NO_TYPE";
    case FUNCTION:
        return "FUNCTION";
    case FARGS:
        return "FARGS";
    case EXPRESSION:
        return "EXPRESSION";
    case PROGRAM:
        return "PROGRAM";
    case BASIC:
        return "BASIC";
    case UNARY:
        return "UNARY";
    case BRACE:
        return "BRACE";
    case BINEXPR:
        return "BINEXPR";
    case FUNCTION_CALL:
        return "FUNCTION_CALL";
    default:
        return "UNKNOWN";
    }
}

typedef struct node
{
    node_type type;
    token_t token;
    struct node *children;
    uint64_t num_child;
} node_t;

typedef struct
{
    token_t *tokens;
    uint64_t num_tok;
    uint64_t pos;
} parser_t;

#define PARSER_CURR ((parser->pos < parser->num_tok) ? parser->tokens[parser->pos] : (token_t){0, "", 0, 0})
#define NODE(type, token) ((node_t){type, token, 0})
void add_child(node_t *node, node_t child)
{
    if (!node->children)
    {
        node->children = malloc(sizeof(node_t));
    }
    node->children = realloc(node->children, sizeof(node_t) * (node->num_child + 1));
    node->children[node->num_child] = child;
    node->num_child++;
}
int binding_power(token_t token)
{
    switch (token.type)
    {
    case PLUS:
    case MINUS:
        return 1;
    case MULTIPLY:
    case DIVIDE:
        return 2;
    case BRACE_OPEN:
        return 3;
    case SQUARE_OPEN:
        return 3;
    default:
        return 0;
    }
}

void parser_next(parser_t *parser)
{
    parser->pos++;
}

bool expect(token_type type, parser_t *parser)
{
    return PARSER_CURR.type == type;
}

node_t parse_basic(parser_t *parser);

node_t parse_func_call(parser_t *parser)
{
    token_t f = PARSER_CURR;
    parser_next(parser);
    parser_next(parser);
    node_t fargs = NODE(FARGS, ((token_t){0, 0, 0, 0}));
    while (PARSER_CURR.type != BRACE_CLOSE)
    {
        node_t n = parse_basic(parser);
        add_child(&fargs, n);
    }
    parser_next(parser);
    node_t func_call = NODE(FUNCTION_CALL, f);
    add_child(&func_call, fargs);
    return func_call;
}

node_t null_denotation(parser_t *parser)
{
    switch (PARSER_CURR.type)
    {
    case ID:
    case NUMBER:
    {
        parser_next(parser);
        if (expect(BRACE_OPEN, parser))
        {
            parser->pos--;
            return parse_func_call(parser);
        }
        parser->pos--;
        node_t n = NODE(BASIC, PARSER_CURR);
        parser_next(parser);
        return n;
    }
    case NOT:
    case MINUS:
    {
        if (!expect(ID, parser) && !expect(NUMBER, parser))
        {
            return NODE(NO_TYPE, ((token_t){0, 0, 0, 0}));
        }
        node_t unary = NODE(UNARY, PARSER_CURR);
        parser_next(parser);
        node_t basic = NODE(BASIC, PARSER_CURR);
        add_child(&unary, basic);
        return unary;
    }
    case BRACE_OPEN:
    {
        node_t brace_node = NODE(BRACE, PARSER_CURR);
        parser_next(parser);
        node_t child = parse_basic(parser);
        if (!expect(BRACE_CLOSE, parser))
        {
            return NODE(NO_TYPE, ((token_t){0, 0, 0, 0}));
        }
        parser_next(parser);
        add_child(&brace_node, child);
        return brace_node;
    }
    }
    return (node_t){0, 0, 0, 0};
}
node_t parse_expr(parser_t *parser, int rbp);

node_t left_denotation(parser_t *parser, node_t *left)
{
    switch (PARSER_CURR.type)
    {
    case PLUS:
    case MINUS:
    case DIVIDE:
    case MULTIPLY:
    {
        token_t t = PARSER_CURR;
        parser_next(parser);
        int bp = binding_power(t);
        node_t right = parse_expr(parser, bp);
        node_t bin_expr = NODE(BINEXPR, t);
        add_child(&bin_expr, *left);
        add_child(&bin_expr, right);
        return bin_expr;
    }
    }
    return *left;
}

node_t parse_expr(parser_t *parser, int rbp)
{
    node_t left = null_denotation(parser);
    while (rbp < binding_power(PARSER_CURR))
    {
        left = left_denotation(parser, &left);
    }
    return left;
}

node_t parse_primary(parser_t *parser)
{
    node_t expr = NODE(EXPRESSION, ((token_t){0, 0, 0, 0}));
    add_child(&expr, parse_expr(parser, 0));
    return expr;
}

node_t parse_basic(parser_t *parser)
{
    switch (PARSER_CURR.type)
    {
    case ID:
    {
        if (!strcmp(PARSER_CURR.text, "if"))
        {
            printf("If is Not Implimented\n");
            return NODE(NO_TYPE, ((token_t){0, 0, 0, 0}));
        }
        if (!strcmp(PARSER_CURR.text, "f"))
        {
            parser_next(parser);
            if (!expect(ID, parser))
            {
                return NODE(NO_TYPE, ((token_t){0, 0, 0, 0}));
            }
            token_t name = PARSER_CURR;
            parser_next(parser);
            uint64_t n_args = 0;
            node_t fargs = NODE(FARGS, ((token_t){0, 0, 0, 0}));
            while (expect(ID, parser))
            {
                add_child(&fargs, parse_basic(parser));
            }
            if (!expect(ARROW, parser))
            {
                return NODE(NO_TYPE, ((token_t){0, 0, 0, 0}));
            }
            parser_next(parser);
            node_t body = parse_basic(parser);
            node_t func = NODE(FUNCTION, name);
            add_child(&func, fargs);
            add_child(&func, body);
            return func;
        }
        parser_next(parser);
        if (expect(ID, parser) || expect(ARROW, parser))
        {
            parser->pos--;
            node_t n = NODE(BASIC, PARSER_CURR);
            parser_next(parser);
            return n;
        }
        parser->pos--;
        return parse_primary(parser);
    }
    case NUMBER:
    {
        return parse_primary(parser);
    }
    }
    return NODE(NO_TYPE, ((token_t){0, 0, 0, 0}));
}

node_t parse(parser_t *parser)
{
    printf("%s\n", __func__);
    node_t program = NODE(PROGRAM, ((token_t){0, 0, 0, 0}));
    while (1)
    {
        node_t parsed = parse_basic(parser);
        if (parsed.type == NO_TYPE)
        {
            break;
        }
        add_child(&program, parsed);
        if (PARSER_CURR.type == END_OF_FILE)
        {
            break;
        }
    }
    return program;
}

void print_node(node_t node)
{
    printf("NODE:['%s': %s]\n", node.token.text, node_type_to_string(node.type));
}
void print_nodes(node_t start, int indent)
{
    for (int i = 0; i < indent; ++i)
    {
        printf("\t");
    }
    print_node(start);
    for (int i = 0; i < start.num_child; ++i)
    {
        print_nodes(start.children[i], indent + 1);
    }
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
    parser_t parser = (parser_t){tokens, i, 0};
    node_t tree = parse(&parser);
    print_nodes(tree, 0);
}