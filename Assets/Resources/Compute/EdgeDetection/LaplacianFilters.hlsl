// Some laplacian kernels:
// https://legacy.imagemagick.org/Usage/convolve/#laplacian

/*
┌──────────────┬───────────────────────────────────────────────────────────────────┐
│ Laplacian: 0 │ 3x3 Laplacian, with center:8 edge:-1 corner:-1                    │
├────┬────┬────┼───────────────────────────────────────────────────────────────────┤
│ -1 │ -1 │ -1 │ The 8 neighbor Laplacian.                                         │
├────┼────┼────┤ Probably the most common discrete Laplacian edge detection kernel.│
│ -1 │  8 │ -1 ├─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─┤
├────┼────┼────┤ Kernel "Laplacian" of size 3x3+1+1 with values from -1 to 8.      │
│ -1 │ -1 │ -1 │ Forming a output range from -8 to 8 (Zero-Summing).               │
└────┴────┴────┴───────────────────────────────────────────────────────────────────┘
*/
float laplace_3x3_0[3] = { 8, -1, -1 };
float laplacian_3x3_0[9] =
{
    -1, -1, -1,
    -1,  8, -1,
    -1, -1, -1,
};

/*
┌──────────────┬───────────────────────────────────────────────────────────────┐
│ Laplacian: 1 │ 3x3 Laplacian, with center:4 edge:-1 corner:0                 │
├────┬────┬────┼───────────────────────────────────────────────────────────────┤
│  0 │ -1 │  0 │ The 4 neighbour Laplacian.                                    │    
│────┼────┼────┤ Kernel "Laplacian" of size 3x3+1+1 with values from -1 to 4.  │
│ -1 │  4 │ -1 │ Forming an output range from -4 to 4 (Zero-Summing).          │
├────┼────┼────┤ The results are not as strong, but are often clearer than the │
│  0 │ -1 │  0 │ 8-neighbour laplacian.                                        │
└────┴────┴────┴───────────────────────────────────────────────────────────────┘
*/
float laplace_3x3_1[3] = { 4, -1, 0 };
float laplacian_3x3_1[9] =
{
    0, -1,  0,
   -1,  4, -1,
    0, -1,  0,
};

/*
┌──────────────┬───────────────────────────────────────────────────────────────┐
│ Laplacian: 2 │ 3x3 Laplacian, with center:4 edge:1 corner:-2                 │
├────┬────┬────┼───────────────────────────────────────────────────────────────┤
│ -2 │  1 │ -2 │ Kernel "Laplacian" of size 3x3+1+1 with values from -2 to 4.  │    
├────┼────┼────┤ Forming an output range from -8 to 8 (Zero-Summing).          │
│  1 │  4 │  1 │                                                               │
├────┼────┼────┤                                                               │
│ -2 │  1 │ -2 │                                                               │
└────┴────┴────┴───────────────────────────────────────────────────────────────┘
*/
float laplace_3x3_2[3] = { 4, 1, -2 };
float laplacian_3x3_2[9] =
{
    -2,  1, -2,
     1,  4,  1,
    -2,  1, -2
};

/*
┌──────────────┬───────────────────────────────────────────────────────────────┐
│ Laplacian: 3 │ 3x3 Laplacian, with center:4 edge:-2 corner:1                 │
├────┬────┬────┼───────────────────────────────────────────────────────────────┤
│  1 │ -2 │  1 │ Kernel "Laplacian" of size 3x3+1+1 with values from -2 to 4.  │
├────┼────┼────┤ Forming an output range from -8 to 8 (Zero-Summing).          │
│ -2 │  4 │ -2 │ This kernel highlights diagonal edges, and tends to make      │
├────┼────┼────┤ vertical and horizontal edges vanish. However you may need to │
│  1 │ -2 │  1 │ scale the results to see make any result visible.             │
└────┴────┴────┴───────────────────────────────────────────────────────────────┘
*/
float laplace_3x3_3[3] = { 4, -2, 1 };
/*float laplacian_3x3_3[9] =
{
    1, -2,  1,
   -2,  4, -2,
    1, -2,  1
};*/
/*
┌────────────────────────┐
│ Laplacian: 5           │
├────┬────┬────┬────┬────┼──────────────────────────────────────────────────────────────┐
│ -4 │ -1 │  0 │ -1 │ -4 │ Kernel "Laplacian" of size 5x5+2+2 with values from -4 to 4. │
├────┼────┼────┼────┼────┤ Forming a output range from -24 to 24 (Zero-Summing).        │
│ -1 │  2 │  3 │  2 │ -1 │                                                              │
├────┼────┼────┼────┼────┤ The rule-of-thumb with laplacian kernels is the larger       │
│  0 │  3 │  4 │  3 │  0 │ they are the cleaner the result, especially when errors      │
├────┼────┼────┼────┼────┤ are involved.                                                │
│ -1 │  2 │  3 │  2 │ -1 │                                                              │
├────┼────┼────┼────┼────┤ However you also get less detail.                            │
│ -4 │ -1 │  0 │ -1 │ -4 │                                                              │
└────┴────┴────┴────┴────┴──────────────────────────────────────────────────────────────┘
*/
float laplace_5x5_0[6] = { 4, 3, 2, 0, -1, -4 };
/*float laplacian_5x5_0[25] =
{
    -4, -1,  0, -1, -4,
    -1,  2,  3,  2, -1,
     0,  3,  4,  3,  0,
    -1,  2,  3,  2, -1,
    -4, -1,  0, -1, -4,
};*/

/*
Another 5x5
┌────┬────┬────┬────┬────┐
│ -1 │ -1 │ -1 │ -1 │ -1 │
├────┼────┼────┼────┼────┤
│ -1 │ -1 │ -1 │ -1 │ -1 │
├────┼────┼────┼────┼────┤
│ -1 │ -1 │-24 │ -1 │ -1 │
├────┼────┼────┼────┼────┤
│ -1 │ -1 │ -1 │ -1 │ -1 │
├────┼────┼────┼────┼────┤
│ -1 │ -1 │ -1 │ -1 │ -1 │
└────┴────┴────┴────┴────┘
*/
float laplace_5x5_1[6] = { 24, -1, -1, -1, -1, -1 };
float SafeSplitKernel5(float source_input[6], out float sqrt_input, out float result)
{
    sqrt_input = sqrt(abs(source_input[6]));

    result = pow(sqrt_input, 2)*sign(source_input[6]);
    return result;
}
/*float laplacian_5x5_1[25] =
{
    -1, -1, -1, -1, -1,
    -1, -1, -1, -1, -1,
    -1, -1, 24, -1, -1,
    -1, -1, -1, -1, -1,
    -1, -1, -1, -1, -1,
};*/

/*
┌──────────────┐
│ Laplacian: 7 │  
├────┬────┬────┼────┬────┬────┬────┬─────────────────────────────────────┐
│-10 │ -5 │ -2 │ -1 │ -2 │ -5 │-10 │ Kernel "Laplacian" of size 7x7+3+3  │
├────┼────┼────┼────┼────┼────┼────┤ with values from -10 to 8.          │
│ -5 │  0 │  3 │  4 │  3 │  0 │ -5 │                                     │
├────┼────┼────┼────┼────┼────┼────┤ Forming a output range from         │
│ -2 │  3 │  6 │  7 │  6 │  3 │ -2 │ -1e+02 to 1e+02 (Zero-Summing).     │
├────┼────┼────┼────┼────┼────┼────┼─────────────────────────────────────┘
│ -1 │  4 │  7 │  8 │  7 │  4 │ -1 │
├────┼────┼────┼────┼────┼────┼────┤
│ -2 │  3 │  6 │  7 │  6 │  3 │ -2 │
├────┼────┼────┼────┼────┼────┼────┤
│ -5 │  0 │  3 │  4 │  3 │  0 │ -5 │
├────┼────┼────┼────┼────┼────┼────┤
│-10 │ -5 │ -2 │ -1 │ -2 │ -5 │-10 │
└────┴────┴────┴────┴────┴────┴────┘
*/
float laplace_7x7_0[10] = { 8, 7, 6, 4, 3, 0, -1, -2, -5, -10 };
float SafeSplitKernel7(float source_input[10], out float sqrt_input, out float result)
{
    if (source_input == 0)
    {
        return *source_input;
    }
    sqrt_input = sqrt(abs(source_input[10]));

    result = pow(sqrt_input, 2)*sign(source_input[10]);
    return result;
}
/*float laplacian_7x7_0[49] =
{
    -10, -5, -2, -1, -2, -5, -10,
     -5,  0,  3,  4,  3,  0,  -5,
     -2,  3,  6,  7,  6,  3,  -2,
     -1,  4,  7,  8,  7,  4,  -1,
     -2,  3,  6,  7,  6,  3,  -2,
     -5,  0,  3,  4,  3,  0,  -5,
    -10, -5, -2, -1, -2, -5, -10,
};*/
// -9m -4, -1, 0, -1, -4, -9

float laplace_9x9_0[10] = { 8, 7, 6, 4, 3, 0, -1, -2, -5, -10 };
/*float laplacian_9x9_0[81] =
{
    0,   0,  0,  0,  0,  0,  0,   0,  0,
    0, -10, -5, -2, -1, -2, -5, -10,  0,
    0,  -5,  0,  3,  4,  3,  0,  -5,  0,
    0,  -2,  3,  6,  7,  6,  3,  -2,  0,
    0,  -1,  4,  7,  8,  7,  4,  -1,  0,
    0,  -2,  3,  6,  7,  6,  3,  -2,  0,
    0,  -5,  0,  3,  4,  3,  0,  -5,  0,
    0, -10, -5, -2, -1, -2, -5, -10,  0,
    0,   0,  0,  0,  0,  0,  0,   0,  0
};*/