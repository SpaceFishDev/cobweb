# Cobweb
Cobweb is a compiled functional programming language. The language has no mutable variables, no state, purely functional.

# Syntax:
```F#
f fib x =>  if x < 2 then
                x 
            else 
                fib(x - 2) + fib(x - 1)
f main => print_num(fib(5)) 
```
Syntax is quite simple.
# Features
- Currently no error handling and no typechecking.
- Type inference is implimented
- Full parsing and lexing
- Targets Elf x86_64
- Compiles directly to nasm assembly.