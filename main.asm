section .data
float_4: dq 2.0
float_12: dq 1.0
float_20: dq 5.0
 
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
fact:
push rbp
mov rbp, rsp
fact_if_0:
movsd xmm0, qword [rbp + 16]
sub rsp, 8
movsd [rsp], xmm0
movsd xmm0, qword [float_4]
sub rsp, 8
movsd [rsp], xmm0
movsd xmm0, qword [rsp]
add rsp, 8
movsd xmm1, qword [rsp]
add rsp, 8
ucomisd xmm1, xmm0
ja fact_end_0
fact_then_0:
movsd xmm0, qword [rbp + 16]
sub rsp, 8
movsd [rsp], xmm0
jmp fact_end_1
fact_end_0:
movsd xmm0, qword [rbp + 16]
sub rsp, 8
movsd [rsp], xmm0
movsd xmm0, qword [float_12]
sub rsp, 8
movsd [rsp], xmm0
movsd xmm0, qword [rsp]
add rsp, 8
movsd xmm1, qword [rsp]
add rsp, 8
subsd xmm1, xmm0
sub rsp, 8
movsd [rsp], xmm1
call fact
add rsp, 8
sub rsp, 8
movsd [rsp], xmm0

movsd xmm0, qword [rbp + 16]
sub rsp, 8
movsd [rsp], xmm0
movsd xmm0, qword [rsp]
add rsp, 8
movsd xmm1, qword [rsp]
add rsp, 8
mulsd xmm1, xmm0
sub rsp, 8
movsd [rsp], xmm1
fact_end_1:
movsd xmm0, [rsp]
mov rsp, rbp
pop rbp
ret
main:
push rbp
mov rbp, rsp
movsd xmm0, qword [float_20]
sub rsp, 8
movsd [rsp], xmm0
call fact
add rsp, 8
sub rsp, 8
movsd [rsp], xmm0

call print_num
add rsp, 8
sub rsp, 8
movsd [rsp], xmm0

movsd xmm0, qword [rsp]
cvtsd2si rax, xmm0
mov rdi, rax
mov rax, 60
syscall
