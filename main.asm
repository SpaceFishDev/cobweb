section .data
double_8: dq 2.0
double_9: dq 3.0
double_10: dq 5.0

section .text
global main
add:
push rbp
mov rbp, rsp
movsd xmm8, xmm1
sub rsp, 8
movsd [rsp], xmm8
movsd xmm8, xmm0
sub rsp, 8
movsd [rsp], xmm8
movsd xmm8, qword [rsp]
add rsp, 8
movsd xmm9, qword [rsp]
add rsp, 8
addsd xmm9, xmm8
sub rsp, 8
movsd [rsp], xmm9
movsd xmm0, qword [rsp]
add rsp, 8
mov rsp, rbp
pop rbp
ret
main:
movsd xmm8, qword [double_8]
sub rsp, 8
movsd [rsp], xmm8
movsd xmm8, qword [double_9]
sub rsp, 8
movsd [rsp], xmm8
movsd xmm8, qword [double_10]
sub rsp, 8
movsd [rsp], xmm8
movsd xmm8, qword [rsp]
add rsp, 8
movsd xmm9, qword [rsp]
add rsp, 8
mulsd xmm9, xmm8
sub rsp, 8
movsd [rsp], xmm9
movsd xmm8, xmm0
sub rsp, 8
movsd [rsp], xmm8
movsd xmm8, xmm1
sub rsp, 8
movsd [rsp], xmm8
movsd xmm0, qword [rsp+16]
movsd xmm1, qword [rsp+24]
call add
movsd xmm2, qword [rsp]
add rsp, 8
movsd xmm1, qword [rsp]
add rsp, 8
add rsp, 16
sub rsp, 8
movsd [rsp], xmm9
movsd xmm0, qword [rsp]
add rsp, 8
cvttsd2si rax, xmm0
mov rdi, rax
mov rax, 60
syscall