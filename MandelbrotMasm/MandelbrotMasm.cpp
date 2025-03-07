
#pragma pack(push, 8)

// Must match Args STRUCT in Mandelbrot.inc
struct Args
{
    __forceinline void Assign(double x0, double y0, double deltaX, double deltaY, __int32* iterationsPtr, __int64 rowSize, __int64 maxIterations, __int64 firstRowDiv2, __int64 numRows, __int64 rowDeltaDiv2)
    {
        this->x0 = x0;
        this->y0 = y0;
        this->deltaX = deltaX;
        this->deltaY = deltaY;
        this->iterationsPtr0 = iterationsPtr;
        this->rowSize = rowSize;
        this->maxIterations = maxIterations;
        this->firstRowDiv2 = firstRowDiv2;
        this->numRows = numRows;
        this->rowDeltaDiv2 = rowDeltaDiv2;
    }

    double x0;
    double y0;
    double deltaX;
    double deltaY;
    __int32* iterationsPtr0;
    __int64 rowSize;
    __int64 maxIterations;
    __int64 firstRowDiv2;
    __int64 numRows;
    __int64 rowDeltaDiv2;
};

#pragma pack(pop)

extern "C" void MandelbrotMasmAvx512DoubleCalculate(Args* args);
extern "C" void MandelbrotMasmFmaDoubleCalculate(Args* args);

public ref class MandelbrotMasm
{
public:
    static void Avx512DoubleCalculate(double x0, double y0, double deltaX, double deltaY, __int32* iterationsPtr, __int64 rowSize, __int64 maxIterations, __int64 firstRowDiv2, __int64 numRows, __int64 rowDeltaDiv2)
    {
        Args args;
        args.Assign(x0, y0, deltaX, deltaY, iterationsPtr, rowSize, maxIterations, firstRowDiv2, numRows, rowDeltaDiv2);
        MandelbrotMasmAvx512DoubleCalculate(&args);
    }

    static void FmaDoubleCalculate(double x0, double y0, double deltaX, double deltaY, __int32* iterationsPtr, __int64 rowSize, __int64 maxIterations, __int64 firstRowDiv2, __int64 numRows, __int64 rowDeltaDiv2)
    {
        Args args;
        args.Assign(x0, y0, deltaX, deltaY, iterationsPtr, rowSize, maxIterations, firstRowDiv2, numRows, rowDeltaDiv2);
        MandelbrotMasmFmaDoubleCalculate(&args);
    }
};
