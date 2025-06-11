section .data
num_format db "%.6f",10,0
str_format db "%s",10,0
section .text
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