section .data
string_0: db "Hello, World", 0
 
num_format db "%.6f",10,0
str_format db "%s",10,0


section .text
global main
 
extern printf

print_num:
    push rbp 
    mov rbp, rsp

    sub rsp, 8

    mov rax, 1
    mov rdi, num_format
    movsd xmm0, [rbp + 16]
    call printf

    push qword 0
    
    mov rsp, rbp
    pop rbp
    ret
print_string:
    push rbp
    mov rbp, rsp

    sub rsp, 8

    mov rdi, str_format
    mov rsi, [rbp + 16]
    call printf

    push qword 0

    mov rsp, rbp
    pop rbp
    ret
main:
push rbp
mov rbp, rsp
push string_0
call print_string
movsd xmm0, qword [rsp]
cvtsd2si rax, xmm0
mov rdi, rax
mov rax, 60
syscall
