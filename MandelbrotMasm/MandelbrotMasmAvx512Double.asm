
; ---------- Include ----------

include MandelbrotMasm.inc

; ---------- Consts ----------

.CONST

Consts0To7  REAL8 0.0, 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0
Const4      REAL8 4.0
Const8      REAL8 8.0
Const1Int   DWORD 1


; ---------- Defines ----------

DefineGeneralPurposeRegisters

DefineVectorRegisters z

; Vector registers used only in AVX512 version...

vX0             EQU zmm16
vDxTimes8       EQU zmm17
vDy             EQU zmm18
vDyTimesDj      EQU zmm19

ymmConst1Int    EQU ymm20


; ---------- MandelbrotMasmAvx512DoubleCalculate PROC ----------

.CODE

PUBLIC MandelbrotMasmAvx512DoubleCalculate

MandelbrotMasmAvx512DoubleCalculate PROC

    PushNonvolatileRegisters

    LoadGeneralPurposeRegistersFromArgs

    mov             rI0,rRowSize                    ; rI0 = rowSize / 8
    sar             rI0,3

    vbroadcastsd    vTmpDx,[rArgs].Args.deltaX

    vbroadcastsd    vX0,[rArgs].Args.x0             ; vX0 = (x0, x0 + deltaX, ...)
    vfmadd231pd     vX0,vTmpDx,Consts0To7           ;

    vbroadcastsd    vTmp0,Const8                    ; vDxTimes8 = (8 * deltaX, ...)
    vmulpd          vDxTimes8,vTmp0,vTmpDx          ;

    vbroadcastsd    vDy,[rArgs].Args.deltaY         ; vDy = (deltaY, ...)

    vpbroadcastq    vTmp0,rDj                       ; vDyTimesDj = (deltaY * dj, ...)
    vcvtqq2pd       vTmp0,vTmp0                     ;
    vmulpd          vDyTimesDj,vTmp0,vDy            ;

    vpbroadcastd    ymmConst1Int,Const1Int          ; ymmConst1 = (1, ...)

    vbroadcastsd    vY,[rArgs].Args.y0              ; vY = (y0 + firstRow * deltaY, ...)
    vpbroadcastq    vTmp0,rFirstRow                 ;
    vcvtqq2pd       vTmp0,vTmp0                     ;
    vfmadd231pd     vY,vTmp0,vDy                    ;

	vaddpd          vYPlusDy,vY,vDy                 ; vYPlusDy = vY + vDy

    vbroadcastsd    vConst4,Const4                  ; vConst4 = (4.0, ...)

    ; ----- for j -----

loopJ:
                    
	vmovupd         vX,vX0

    ; ----- for i -----

	mov             rI,rI0

loopI:

	vpxord          vIt1,vIt1,vIt1          ; it = 0
	vpxord          vIt2,vIt2,vIt2          ; 
	vmovupd         vA1,vX                  ; a = x
	vmovupd         vA2,vX                  ;
	vmovupd         vB1,vY                  ; b = y
	vmovupd         vB2,vYPlusDy            ;

    ; ----- for n -----

	mov             rN,rMaxIterationsDiv2

loopN:

    MandelbrotIteration2 IncreaseIterationsZmm
    MandelbrotIteration2 IncreaseIterationsZmm

    kortestq        k1,k2
    cmovz           rN,rOne

	dec             rN
	jg              loopN 

	vmovdqu         YMMWORD PTR [rIterationsPtr],vIt1
	vmovdqu         YMMWORD PTR [rIterationsPtr + 4*rRowSize],vIt2

	add             rIterationsPtr,32
	vaddpd          vX,vX,vDxTimes8

	dec             rI
	jg              loopI

	lea             rIterationsPtr,[rIterationsPtr+4*rRowSizeTimesDjMinus1]
	vaddpd          vY,vY,vDyTimesDj
	vaddpd          vYPlusDy,vYPlusDy,vDyTimesDj

	sub             rJ,rDj
	jg              loopJ

    PopNonvolatileRegisters

	ret

MandelbrotMasmAvx512DoubleCalculate ENDP


; ---------- The End ----------

END
