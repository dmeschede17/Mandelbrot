
# Mandelbrot

## High performance Mandelbrot image generator 

Multi-threaded Mandelbrot image generator using .NET 9 AVX2 and AVX-512 SIMD intrinsics.

### Overview of solution projects

Project Name    | Description
--------------- | -----------
Mandelbrot      | WPF application for generating Mandelbrot images
MandelbrotLib   | Implementation of different multi-threaded variants of the Mandelbrot algorithm
MandelbrotMasm  | MASM implementation of the Mandelbrot algorithm (currently not used in the solution)

### Links

Link                                             | Url
------------------------------------------------ | -------------------------------------------------------------------------------------------
Mandelbrot Set (German) - Wikipedia              | https://de.wikipedia.org/wiki/Mandelbrot-Menge
Mandelbrot Set (English) - Wikipedia             | https://en.wikipedia.org/wiki/Mandelbrot_set
x86/x64 SIMD Instruction List (SSE to AVX512)    | https://www.officedaytime.com/simd512e/simd.html
Latency, Throughput, and Port Usage Information  | https://uops.info
Agner's Software Optimization Resources          | https://www.agner.org/optimize/
Overview of x64 ABI conventions                  | https://docs.microsoft.com/en-us/cpp/build/x64-software-conventions
Intel Intrinsics Guide                           | https://software.intel.com/sites/landingpage/IntrinsicsGuide/

### Calculation for one iteration

Let 

  c = x + yi 
  
be the constant defined by the pixel position and 

  Zn = a + bi
  
be the complex number of the current iteration.

First we need to calculate the square of the absolute value of Zn and check if it is less than 4:

  |Zn|^2 = a^2 + b^2 < 4

Then we can calculate the next iteration:

  Zn+1 = Zn^2 + c = (a + bi)^2 + x + yi = a^2 - b^2 + x + (2ab + y)i

#### FLOP (Floating Point Operations) count for one iteration

No  | Calculation       | FLOP count
--- | ----------------- | ----------
(1) | a^2 + b^2 < 4     | 4         
(2) | b = 2ab + y       | 3         
(3) | a = a^2 - b^2 + x | 4         

### Additional information for Rocket Lake CPU

The original implementation was done on a Rocket Lake CPU.

#### Rocket Lake AVX2 Port Usage For One Iteration

Calculation       | Instructions              | Port Usage     | Remark
----------------- | ------------------------- | -------------- | ------------------------
amnt2 = b*b+(a*a) | vmulpd, vfmadd213pd       | 2x p01         |
cmp = amnt2 < 4   | vcmppd                    | 1x p1          |
it = it - cmp     | vpsubq                    | 1x p15         |
b = (2a)*b+y      | vaddpd, vfmadd213pd       | 2x p01         |
a = a*a+(-b*b+x)  | vfnmadd213pd, vfmadd213pd | 2x p01         |
if !cmp break     | vpor, vmovmskpd           | 1x p015, 1x p0 | Only every 4th iteration

#### Rocket Lake AVX2 Latency, Throughput & Port Usage

https://uops.info/table.html?cb_lat=on&cb_tp=on&cb_uops=on&cb_ports=on&cb_RKL=on&cb_measurements=on&cb_doc=on&cb_avx=on&cb_avx2=on&cb_avx512=on&cb_fma=on

.NET Intrinsic          | Intel Intrinsic    | Instruction      | Latency | Throughput | Port Usage | uops.info Link
----------------------- | ------------------ | ---------------- | ------- | ---------- | ---------- | ---------------------------------------------------------
Avx.Add                 | _mm256_add_pd      | vaddpd           | 4       | 0.5        | p01        | https://uops.info/html-instr/VADDPD_YMM_YMM_YMM.html
Avx.Subtract            | _mm256_sub_pd      | vsubpd           | 4       | 0.5        | p01        | https://uops.info/html-instr/VSUBPD_YMM_YMM_YMM.html
Avx.Compare             | _mm256_cmp_pd      | vcmppd           | 4       | 0.5        | p01        | https://uops.info/html-instr/VCMPPD_YMM_YMM_YMM_I8.html
Avx.Multiply            | _mm256_mul_pd      | vmulpd           | 4       | 0.5        | p01        | https://uops.info/html-instr/VMULPD_YMM_YMM_YMM.html
Fma.MultiplyAdd         | _mm256_fmadd_pd    | vfmadd213pd, ... | 4       | 0.5        | p01        | https://uops.info/html-instr/VFMADD213PD_YMM_YMM_YMM.html
Avx2.Subtract           | _mm256_sub_epi64   | vpsubq           | 1       | 0.33       | p015       | https://uops.info/html-instr/VPSUBQ_YMM_YMM_YMM.html
Avx2.Or                 | _mm256_or_epi64    | vpor             | 1       | 0.33       | p015       | https://uops.info/html-instr/VPOR_YMM_YMM_YMM.html
Avx.MoveMask            | _mm256_movemask_pd | vmovmskpd        | <= 5    | 1          | p0         | https://uops.info/html-instr/VMOVMSKPD_R32_YMM.html
