bits 64
section .data
double_4: dq 2.0
double_12: dq 1.0
double_20: dq 4.0

section .text
global main
extern malloc
extern realloc
; LABEL: [LABEL: fact],
fact:
push rbp
mov rbp, rsp
; ARG_DECL: [VAR_TYPE: Number],[VARIABLE: x],
; LABEL: [LABEL: fact_if_0],
fact_if_0:
; PUSH: [VARIABLE: x],
movsd xmm8, xmm0
sub rsp, 8
movsd [rsp], xmm8
; PUSH: [NUMBER: 2],
movsd xmm8, qword [double_4]
sub rsp, 8
movsd [rsp], xmm8
; CMP_MORE: 
; CONDITIONAL_JUMP: [LABEL: fact_end_0],[VAR_TYPE: Number],
movsd xmm8, qword [rsp]
add rsp, 8
movsd xmm9, qword [rsp]
add rsp, 8
comisd xmm9, xmm8
ja fact_end_0
; LABEL: [LABEL: fact_then_0],
fact_then_0:
; PUSH: [VARIABLE: x],
movsd xmm8, xmm0
sub rsp, 8
movsd [rsp], xmm8
; JMP: [LABEL: fact_end_1],
jmp fact_end_1
; LABEL: [LABEL: fact_end_0],
fact_end_0:
; PUSH: [VARIABLE: x],
movsd xmm8, xmm0
sub rsp, 8
movsd [rsp], xmm8
; PUSH: [NUMBER: 1],
movsd xmm8, qword [double_12]
sub rsp, 8
movsd [rsp], xmm8
; SUB: 
movsd xmm8, qword [rsp]
add rsp, 8
movsd xmm9, qword [rsp]
add rsp, 8
subsd xmm9, xmm8
sub rsp, 8
movsd [rsp], xmm9
; CALL: [LABEL: fact],
movsd xmm8, xmm0
sub rsp, 8
movsd [rsp], xmm8
movsd xmm0, qword [rsp+8]
call fact
movsd xmm9, xmm0
movsd xmm0, qword [rsp]
add rsp, 8
add rsp, 8
sub rsp, 8
movsd [rsp], xmm9
; PUSH: [VARIABLE: x],
movsd xmm8, xmm0
sub rsp, 8
movsd [rsp], xmm8
; MUL: 
movsd xmm8, qword [rsp]
add rsp, 8
movsd xmm9, qword [rsp]
add rsp, 8
mulsd xmm9, xmm8
sub rsp, 8
movsd [rsp], xmm9
; LABEL: [LABEL: fact_end_1],
fact_end_1:
; RETURN: 
movsd xmm0, qword [rsp]
add rsp, 8
mov rsp, rbp
pop rbp
ret
; LABEL: [LABEL: main],
main:
; PUSH: [NUMBER: 4],
movsd xmm8, qword [double_20]
sub rsp, 8
movsd [rsp], xmm8
; CALL: [LABEL: fact],
movsd xmm8, xmm0
sub rsp, 8
movsd [rsp], xmm8
movsd xmm0, qword [rsp+8]
call fact
movsd xmm9, xmm0
movsd xmm0, qword [rsp]
add rsp, 8
add rsp, 8
sub rsp, 8
movsd [rsp], xmm9
; RETURN: 
movsd xmm0, qword [rsp]
add rsp, 8
cvttsd2si rax, xmm0
mov rdi, rax
mov rax, 60
syscall
; NOP: 
