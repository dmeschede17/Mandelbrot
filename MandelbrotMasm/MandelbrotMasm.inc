
; ---------- Args STRUCT ----------

; Must match struct Args in MandelbrotMasm.cpp
Args STRUCT 8

    x0                      REAL8 ?
    y0                      REAL8 ?
    deltaX                  REAL8 ?
    deltaY                  REAL8 ?
    iterationsPtr0          QWORD ?     ; Pointer to 2-dimension DWORD iterations array
    rowSize                 QWORD ?     ; Number of DWORDs per row
    maxIterations           QWORD ?     ; Maximum number of iterations to calculate
    firstRowDiv2            QWORD ?
    numRows                 QWORD ?
    rowDeltaDiv2            QWORD ?

Args ENDS


; ---------- PushNonvolatileRegisters MACRO ----------

PushNonvolatileRegisters MACRO

    push            rdi
    push            rsi  
    push            rbx
    push		    r12
    push		    r13
    push		    r14
    push		    r15
    sub             rsp,0A0h  
    vzeroupper  
    vmovups         xmmword ptr [rsp+00h],xmm6
    vmovups         xmmword ptr [rsp+10h],xmm7
    vmovups         xmmword ptr [rsp+20h],xmm8
    vmovups         xmmword ptr [rsp+30h],xmm9
    vmovups         xmmword ptr [rsp+40h],xmm10
    vmovups         xmmword ptr [rsp+50h],xmm11
    vmovups         xmmword ptr [rsp+60h],xmm12
    vmovups         xmmword ptr [rsp+70h],xmm13
    vmovups         xmmword ptr [rsp+80h],xmm14
    vmovups         xmmword ptr [rsp+90h],xmm15

ENDM

; ---------- PopNonvolatileRegisters MACRO ----------

PopNonvolatileRegisters MACRO

    vmovups			xmm6,xmmword ptr [rsp+0h]  
    vmovups			xmm7,xmmword ptr [rsp+10h]  
    vmovups			xmm8,xmmword ptr [rsp+20h]  
    vmovups			xmm9,xmmword ptr [rsp+30h]  
    vmovups			xmm10,xmmword ptr [rsp+40h]  
    vmovups			xmm11,xmmword ptr [rsp+50h]  
    vmovups			xmm12,xmmword ptr [rsp+60h]  
    vmovups			xmm13,xmmword ptr [rsp+70h]  
    vmovups			xmm14,xmmword ptr [rsp+80h]  
    vmovups			xmm15,xmmword ptr [rsp+90h]  
    vzeroupper  
    add             rsp,0A0h
    pop             r15
    pop             r14
    pop             r13
    pop             r12
    pop             rbx  
    pop             rsi  
    pop             rdi  

ENDM

; ---------- DefineGeneralPurposeRegisters MACRO ----------

DefineGeneralPurposeRegisters MACRO

    rOne                    EQU rax

    rArgs	                EQU rcx

    rIterationsPtr          EQU rdi
    rRowSize                EQU rbx
    rMaxIterationsDiv2      EQU r8
    rFirstRow               EQU r9
    rDj				        EQU r10
    rRowSizeTimesDjMinus1   EQU r11
    rJ                      EQU r12

    rI0                     EQU r13

    rI			            EQU r14
    rN                      EQU r15

ENDM

; ---------- DefineVectorRegisters MACRO ----------

DefineVectorRegisters MACRO yOrZ : REQ

    vTmp0       EQU &yOrZ&mm0
    vTmp1       EQU &yOrZ&mm1
    vTmp2       EQU &yOrZ&mm2
    vTmp3       EQU &yOrZ&mm3

    vCmp1       EQU &yOrZ&mm4
    vCmp2       EQU &yOrZ&mm5
    vA1         EQU &yOrZ&mm6
    vB1         EQU	&yOrZ&mm7
    vA2         EQU &yOrZ&mm8
    vB2         EQU &yOrZ&mm9
    vX          EQU &yOrZ&mm10

    vY          EQU &yOrZ&mm11
    vYPlusDy    EQU	&yOrZ&mm12

    vConst4     EQU &yOrZ&mm13

    vIt1        EQU ymm14
    vIt2        EQU ymm15

    vTmpDx      EQU vA1             ; Used during setup
    vTmpDy      EQU vB1             ; Used during setup

    xmmTmp0     EQU xmm0

ENDM


; ---------- LoadGeneralPurposeRegistersFromArgs MACRO ----------

LoadGeneralPurposeRegistersFromArgs MACRO

    mov             rOne,1                                          ; rOne = 1

    mov             rRowSize,[rArgs].Args.rowSize                   ; rRowSize = rowSize

    mov             rMaxIterationsDiv2,[rArgs].Args.maxIterations   ; rMaxIterationsDiv2 = maxIterations / 2
    sar             rMaxIterationsDiv2,1

    mov             rFirstRow,[rArgs].Args.firstRowDiv2             ; rFirstRow = 2 * firstRowDiv2
    sal             rFirstRow,1                                     ;

    mov             rIterationsPtr,rRowSize                         ; rIterationsPtr = iterationsPtr0 + 4 * firstRow * rowSize
    imul            rIterationsPtr,rFirstRow                        ;
    sal             rIterationsPtr,2                                ;
    add             rIterationsPtr,[rArgs].Args.iterationsPtr0      ;

    mov             rJ,[rArgs].Args.numRows                         ; rJ = numRows - firstRow
    sub             rJ,rFirstRow                                    ;

    mov             rDj,[rArgs].Args.rowDeltaDiv2                   ; rDj = 2 * rowDeltaDiv2
    sal             rDj,1             

    mov             rRowSizeTimesDjMinus1,rDj                       ; rRowSizeTimesDjMinus1 = rowSize * (dJ - 1)
    dec             rRowSizeTimesDjMinus1                           ;
    imul            rRowSizeTimesDjMinus1,rRowSize                  ;

ENDM


; ---------- IncreaseIterationsYmm MACRO ----------

IncreaseIterationsYmm MACRO index : REQ

    vcmplt_oqpd     vCmp&index&,vCmp&index&,vConst4                 ; it += cmp < 4.0
    vpsubq          vIt&index&,vIt&index&,vCmp&index&               ;

ENDM


; ---------- IncreaseIterationsZmm MACRO ----------

IncreaseIterationsZmm MACRO index : REQ

    vcmplt_oqpd     k&index&,vCmp&index&,vConst4                    ; it += cmp < 4.0
    vpaddd          vIt&index&{k&index&},vIt&index&,ymmConst1Int    ;

ENDM


; ---------- MandelbrotIteration2 MACRO ----------

MandelbrotIteration2 MACRO increaseIterationsMacro

    ; c = x + yi
    ; Zn = a + bi

    ; Zn+1 = Zn^2 + c = (a + bi)^2 + x + yi = a^2 - b^2 + x + (2ab + y)i 

    ; Calculate first Zn+1...

    vmovupd         vTmp0,vB1               ; tmp0 = -b*b + x
    vfnmadd213pd    vTmp0,vTmp0,vX          ;

    vmulpd          vCmp1,vA1,vA1           ; cmp = a*a + b*b
    vfmadd231pd     vCmp1,vB1,vB1

    vaddpd          vTmp1,vA1,vA1           ; tmp1 = 2*a

    increaseIterationsMacro 1

    vfmadd213pd     vB1,vTmp1,vY            ; b_n+1 = b * (2*a) + y
    vfmadd213pd     vA1,vA1,vTmp0           ; a_n+1 = a * a + (-b*b+x)

    ; Calculate second Zn+1...

    vmovupd         vTmp2,vB2               ; tmp2 = -b*b + x
    vfnmadd213pd    vTmp2,vTmp2,vX          ;

    vmulpd          vCmp2,vA2,vA2           ; cmp = a*a + b*b
    vfmadd231pd     vCmp2,vB2,vB2

    vaddpd          vTmp3,vA2,vA2           ; tmp3 = 2*a

    increaseIterationsMacro 2

    vfmadd213pd     vB2,vTmp3,vYPlusDy      ; b_n+1 = b * (2*a) + y
    vfmadd213pd     vA2,vA2,vTmp2           ; a_n+1 = a * a + (-b*b+x)

ENDM
