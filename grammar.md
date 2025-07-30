# GRAMMAR:
program        : function*

function       : 'f' id '=>' expression

expression     : if_expr
               | bin_expr
               | function_call
               | basic_expr
               | '(' expression ')'

if_expr        : 'if' expression 'then' expression 'else' expression

bin_expr       : expression bin_token expression

bin_token      : '+' | '-' | '/' | '*' | '==' | '!=' | '>=' | '<='

function_call  : id '(' (expression (',' expression)*)? ')'

basic_expr     : id | string | number