
; ---------- Include ----------

include MandelbrotMasm.inc

; ---------- Consts ----------

.CONST

Consts0To3              REAL8 0.0, 1.0, 2.0, 3.0
Const4                  REAL8 4.0
IterationsPermuteMask   DWORD 0, 2, 4, 6, 0, 0, 0, 0


; ---------- Options & Defines ----------

OPTION AVXENCODING:NO_EVEX

DefineGeneralPurposeRegisters

DefineVectorRegisters y


; ---------- MandelbrotMasmFmaDoubleCalculate PROC ----------

.CODE

PUBLIC MandelbrotMasmFmaDoubleCalculate

MandelbrotMasmFmaDoubleCalculate PROC

    LOCAL lX0           : YMMWORD
    LOCAL lDxTimes4     : YMMWORD
    LOCAL lDy           : YMMWORD
    LOCAL lDyTimesDj    : YMMWORD

    PushNonvolatileRegisters

    LoadGeneralPurposeRegistersFromArgs

    mov             rI0,rRowSize                    ; rI0 = rowSize / 8
    sar             rI0,2

    vbroadcastsd    vTmpDx,[rArgs].Args.deltaX

    vbroadcastsd    vTmp0,[rArgs].Args.x0           ; lX0 = (x0, x0 + deltaX, ...)
    vfmadd231pd     vTmp0,vTmpDx,Consts0To3         ;
    vmovupd         lX0,vTmp0                       ;

    vbroadcastsd    vTmp0,Const4                    ; lDxTimes4 = (4 * deltaX, ...)
    vmulpd          vTmp0,vTmp0,vTmpDx              ;
    vmovupd         lDxTimes4,vTmp0                 ;

    vbroadcastsd    vTmpDy,[rArgs].Args.deltaY      ; lDy = (deltaY, ...)
    vmovupd         lDy,vTmpDy                      ;

    cvtsi2sd        xmmTmp0,rDj                     ; lDyTimesDj = (deltaY * dj, ...)
    vbroadcastsd    vTmp0,xmmTmp0                   ;
    vmulpd          vTmp0,vTmpDy,vTmp0              ;
    vmovupd         lDyTimesDj,vTmp0                ;

    vbroadcastsd    vY,[rArgs].Args.y0              ; vY = (y0 + firstRow * deltaY, ...)
    cvtsi2sd        xmmTmp0,rFirstRow               ;
    vbroadcastsd    vTmp0,xmmTmp0                   ;
    vfmadd231pd     vY,vTmp0,vTmpDy                 ;

    vaddpd          vYPlusDy,vY,vTmpDy              ; vYPlusDy = vY + lDy

    vbroadcastsd    vConst4,Const4                  ; vConst4 = (4.0, ...)

    ; ----- for j -----

loopJ:
                    
    vmovupd         vX,lX0

    ; ----- for i -----

    mov             rI,rI0

loopI:

    vpxor           vIt1,vIt1,vIt1                  ; it = 0
    vpxor           vIt2,vIt2,vIt2                  ;
    vmovaps         vA1,vX                          ; a = x
    vmovaps         vA2,vX                          ;
    vmovaps         vB1,vY                          ; b = y
    vmovaps         vB1,vYPlusDy                    ;

    ; ----- for n -----

    mov             rN,rMaxIterationsDiv2

loopN:

    MandelbrotIteration2 IncreaseIterationsYmm
    MandelbrotIteration2 IncreaseIterationsYmm

    vpor            vCmp1,vCmp1,vCmp2
    vmovmskpd       eax,vCmp1
    test            al,0Fh
    cmovz           rN,rOne

    dec             rN
    jg              loopN 

    vmovups         ymm2,IterationsPermuteMask
    vpermd          ymm0,ymm2,vIt1
    vpermd          ymm1,ymm2,vIt2
    vmovdqu         XMMWORD PTR [rIterationsPtr],xmm0
    vmovdqu         XMMWORD PTR [rIterationsPtr + 4*rRowSize],xmm0

    add             rIterationsPtr,16
    vaddpd          vX,vX,lDxTimes4

    dec             rI
    jg              loopI

    lea             rIterationsPtr,[rIterationsPtr+4*rRowSizeTimesDjMinus1]
    vaddpd          vY,vY,lDyTimesDJ
    vaddpd          vYPlusDy,vYPlusDy,lDyTimesDJ

    sub             rJ,rDj
    jg              loopJ

    PopNonvolatileRegisters

    ret

MandelbrotMasmFmaDoubleCalculate ENDP


; ---------- The End ----------

END
