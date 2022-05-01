// include only to avoid errors in IDE
#ifndef REAL_IS_HALF
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#endif


// Some laplacian kernels:
// https://legacy.imagemagick.org/Usage/convolve/#laplacian
/**
 * \brief This macro determines which convolution kernel to use.
 */
#define LAPLACIAN_KERNEL 30

/*
┌────┬────┬────┐    ┌─────┬─────┬─────┐    ┌────┬────┬────┐
│ 00 │ 01 │ 02 │    │  f0 │  f2 │  f3 │    │ f3 │ f2 │ f3 │
├────┼────┼────┤    └─────┴─────┴─────┘    ├────┼────┼────┤
│ 03 │ 04 │ 05 │      filter[3]            │ f2 │ f0 │ f2 │
├────┼────┼────┤      f0 = center          ├────┼────┼────┤
│ 06 │ 07 │ 08 │      f1 = edge            │ f3 │ f2 │ f3 │
└────┴────┴────┘      f2 = corner          └────┴────┴────┘
*/
real4 ApplyLaplacian_3x3(real4 sample_3x3[9], real filter[3])
{
    return filter[0] * (sample_3x3[4])
         + filter[1] * (sample_3x3[1] + sample_3x3[3] + sample_3x3[5] + sample_3x3[7])
         + filter[2] * (sample_3x3[0] + sample_3x3[2] + sample_3x3[6] + sample_3x3[8]);
}

/*
┌────┬────┬────┬────┬────┐                              ╔════╗••••••••••••••••••••
│ 00 │ 01 │ 02 │ 03 │ 04 │    filter[6]:                ║ f5 ║ f4 • f3 • f4 • f5 •
├────┼────┼────┼────┼────┤    only need 6 values        ╠════╬════╗•••••••••••••••
│ 05 │ 06 │ 07 │ 08 │ 09 │    f0 = center               ║ f4 ║ f2 ║ f1 • f2 • f4 •
├────┼────┼────┼────┼────┤    f1 = inner edge           ╠════╬════╬════╗••••••••••
│ 10 │ 11 │ 12 │ 13 │ 14 │    f2 = inner corner         ║ f3 ║ f1 ║ f0 ║ f1 • f3 •
├────┼────┼────┼────┼────┤                              ╚════╩════╩════╝••••••••••
│ 15 │ 16 │ 17 │ 18 │ 19 │    f3 = outer edge           • f4 • f2 • f1 • f2 • f4 •
├────┼────┼────┼────┼────┤    f4 = outer edge adjacent  ••••••••••••••••••••••••••
│ 20 │ 21 │ 22 │ 23 │ 24 │    f5 = outer corner         • f5 • f4 • f3 • f4 • f5 •
└────┴────┴────┴────┴────┘                              ••••••••••••••••••••••••••
*/
real4 ApplyLaplacian_5x5(real4 sample_5x5[25], real filter[6])
{
 const real4 center   = filter[0] * (sample_5x5[12]);
 const real4 edge_0   = filter[1] * (sample_5x5[7] + sample_5x5[11] + sample_5x5[13] + sample_5x5[17]);
 const real4 corner_0 = filter[2] * (sample_5x5[6] + sample_5x5[8] + sample_5x5[16] + sample_5x5[18]);

 const real4 edge_1a  = filter[3] * (sample_5x5[2] + sample_5x5[10] + sample_5x5[14] + sample_5x5[22]);
 const real4 edge_1b  = filter[4] * (sample_5x5[1] + sample_5x5[3] + sample_5x5[05] + sample_5x5[9] + sample_5x5[15] + sample_5x5[19] + sample_5x5[21] + sample_5x5[23]);
 const real4 corner_1 = filter[5] * (sample_5x5[0] + sample_5x5[4] + sample_5x5[20] + sample_5x5[24]);
    
 return
     center + edge_0 + corner_0
     + edge_1a + edge_1b + corner_1;
}

real4 ApplyLaplacian_7x7(real4 sample_7x7[49], real filter[10])
{
 const real4 center   = filter[0] * (sample_7x7[25]);
 const real4 edge_0   = filter[1] * (sample_7x7[18] + sample_7x7[24] + sample_7x7[26] + sample_7x7[32]);
 const real4 corner_0 = filter[2] * (sample_7x7[17] + sample_7x7[19] + sample_7x7[31] + sample_7x7[33]);
    
 const real4 edge_1a  = filter[3] * (sample_7x7[10] + sample_7x7[23] + sample_7x7[27] + sample_7x7[38]);
 const real4 edge_1b  = filter[4] * (sample_7x7[9] + sample_7x7[11] + sample_7x7[15] + sample_7x7[19] + sample_7x7[29] + sample_7x7[33] + sample_7x7[37] + sample_7x7[39]);
 const real4 corner_1 = filter[5] * (sample_7x7[8] + sample_7x7[12] + sample_7x7[36] + sample_7x7[40]);

 const real4 edge_2a  = filter[6] + (sample_7x7[3] + sample_7x7[21] + sample_7x7[27] + sample_7x7[45]);
 const real4 edge_2b  = filter[7] + (sample_7x7[2] + sample_7x7[4] + sample_7x7[14] + sample_7x7[20] + sample_7x7[28] + sample_7x7[34] + sample_7x7[44] + sample_7x7[46]);
 const real4 edge_2c  = filter[8] + (sample_7x7[1] + sample_7x7[5] + sample_7x7[7] + sample_7x7[13] + sample_7x7[35] + sample_7x7[41] + sample_7x7[43] + sample_7x7[47]);
 const real4 corner_2 = filter[9] + (sample_7x7[0] + sample_7x7[6] + sample_7x7[42] + sample_7x7[48]);
    
 return
     center + edge_0 + corner_0
     + edge_1a + edge_1b + corner_1
     + edge_2a + edge_2b + edge_2c + corner_2;
}

real4 ApplyLaplacian_9x9(real4 sample_9x9[81], real filter[10]) // outer rim is currently "0" padding
{
     real4 center   = filter[0] * (sample_9x9[40]);
     real4 edge_0   = filter[1] * (sample_9x9[31] + sample_9x9[39] + sample_9x9[41] + sample_9x9[49]);
     real4 corner_0 = filter[2] * (sample_9x9[30] + sample_9x9[32] + sample_9x9[48] + sample_9x9[50]);
    
     real4 edge_1a  = filter[3] * (sample_9x9[22] + sample_9x9[38] + sample_9x9[42] + sample_9x9[58]);
     real4 edge_1b  = filter[4] * (sample_9x9[21] + sample_9x9[23] + sample_9x9[29] + sample_9x9[33] + sample_9x9[47] + sample_9x9[51] + sample_9x9[57] + sample_9x9[59]);
     real4 corner_1 = filter[5] * (sample_9x9[20] + sample_9x9[24] + sample_9x9[56] + sample_9x9[60]);

     real4 edge_2a  = filter[6] + (sample_9x9[13] + sample_9x9[37] + sample_9x9[43] + sample_9x9[67]);
     real4 edge_2b  = filter[7] + (sample_9x9[12] + sample_9x9[14] + sample_9x9[28] + sample_9x9[34] + sample_9x9[46] + sample_9x9[52] + sample_9x9[66] + sample_9x9[68]);
     real4 edge_2c  = filter[8] + (sample_9x9[11] + sample_9x9[15] + sample_9x9[19] + sample_9x9[25] + sample_9x9[55] + sample_9x9[61] + sample_9x9[65] + sample_9x9[69]);
     real4 corner_2 = filter[9] + (sample_9x9[10] + sample_9x9[16] + sample_9x9[64] + sample_9x9[70]);

    //  real4 edge_3a  = 0 * (sample_9x9[4] + sample_9x9[36] + sample_9x9[44] + sample_9x9[76]);
    //  real4 edge_3b  = 0 * (sample_9x9[3] + sample_9x9[5] + sample_9x9[27] + sample_9x9[35] + sample_9x9[45] + sample_9x9[53] + sample_9x9[75] + sample_9x9[77]);
    //  real4 edge_3c  = 0 * (sample_9x9[2] + sample_9x9[6] + sample_9x9[18] + sample_9x9[26] + sample_9x9[54] + sample_9x9[62] + sample_9x9[74] + sample_9x9[78]);
    //  real4 edge_3d  = 0 * (sample_9x9[1] + sample_9x9[7] + sample_9x9[9] + sample_9x9[17] + sample_9x9[63] + sample_9x9[71] + sample_9x9[73] + sample_9x9[79]);
    //  real4 corner_3 = 0 * (sample_9x9[0] + sample_9x9[8] + sample_9x9[72] + sample_9x9[80]);

    return
        center + edge_0 + corner_0
        + edge_1a + edge_1b + corner_1
        + edge_2a + edge_2b + edge_2c + corner_2;
        // + edge_3a + edge_3b + edge_3c + edge_3d + corner_3;
}

#if LAPLACIAN_KERNEL == 30
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
static real laplacian_3x3_0[9] =
{
    -1, -1, -1,
    -1,  8, -1,
    -1, -1, -1,
};
/*
┌──────────────┬───────────────────────────────────────────────────────────────┐
│ Laplacian: 0 │ 3x3 Laplacian, with center:8 edge:-1 corner:-1                │
└──────────────┴───────────────────────────────────────────────────────────────┘
*/
static real laplace_3x3_0[] = {8, -1, -1};

static real laplacian_3x3_0a[9] =
{
     1,  1,  1,
     1, -8,  1,
     1,  1,  1,
};

#elif LAPLACIAN_KERNEL == 31

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
 static real laplacian_3x3_1[9] =
{
    0, -1,  0,
   -1,  4, -1,
    0, -1,  0,
};
/*
┌──────────────┬───────────────────────────────────────────────────────────────┐
│ Laplacian: 1 │ 3x3 Laplacian, with center:4 edge:-1 corner:0                 │
└──────────────┴───────────────────────────────────────────────────────────────┘
*/
 static real laplace_3x3_1[3] = { 4, -1, 0 };


#elif LAPLACIAN_KERNEL == 32

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
 static real laplacian_3x3_2[9] =
{
    -2,  1, -2,
     1,  4,  1,
    -2,  1, -2
};

/*
┌──────────────┬───────────────────────────────────────────────────────────────┐
│ Laplacian: 2 │ 3x3 Laplacian, with center:4 edge:1 corner:-2                 │
└──────────────┴───────────────────────────────────────────────────────────────┘
*/
 static real laplace_3x3_2[3] = { 4, 1, -2 };

#elif LAPLACIAN_KERNEL == 33

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
 static real laplacian_3x3_3[9] =
{
    1, -2,  1,
   -2,  4, -2,
    1, -2,  1
};

/*
┌──────────────┬───────────────────────────────────────────────────────────────┐
│ Laplacian: 3 │ 3x3 Laplacian, with center:4 edge:-2 corner:1                 │
└──────────────┴───────────────────────────────────────────────────────────────┘
*/
 static real laplace_3x3_3[3] = { 4, -2, 1 };

/*
┌────┬────┬────┐    ┌─────┬─────┬─────┐    ┌────┬────┬────┐
│ 00 │ 01 │ 02 │    │  f0 │  f2 │  f3 │    │ f3 │ f2 │ f3 │
├────┼────┼────┤    └─────┴─────┴─────┘    ├────┼────┼────┤
│ 03 │ 04 │ 05 │      filter[3]            │ f2 │ f0 │ f2 │
├────┼────┼────┤      f0 = center          ├────┼────┼────┤
│ 06 │ 07 │ 08 │      f1 = edge            │ f3 │ f2 │ f3 │
└────┴────┴────┘      f2 = corner          └────┴────┴────┘
*/
real4 ApplyLaplacian_3x3(real4 sample_3x3[9])
{
    return laplace_3x3_3[0] * (sample_3x3[4])
         + laplace_3x3_3[1] * (sample_3x3[1] + sample_3x3[3] + sample_3x3[5] + sample_3x3[7])
         + laplace_3x3_3[2] * (sample_3x3[0] + sample_3x3[2] + sample_3x3[6] + sample_3x3[8]);
}

#elif LAPLACIAN_KERNEL == 50

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
 static real laplacian_5x5_0[25] =
{
    -4, -1,  0, -1, -4,
    -1,  2,  3,  2, -1,
     0,  3,  4,  3,  0,
    -1,  2,  3,  2, -1,
    -4, -1,  0, -1, -4,
};

 static real laplace_5x5_0[6] = { 4, 3, 2, 0, -1, -4 };


#elif LAPLACIAN_KERNEL == 51

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
real laplace_5x5_1_SafeSqrt[6] = {4.89897948, 1, 1, 1, 1, 1};
// void SafeSplitKernel5(real source_input[(int)6], real sqrt_input[(int)6], real out result)
// {
//     result = 0;
//     // sqrt_input = sqrt(abs(source_input[6]));
//     for (int i = 0; i < 6; i++)
//     {
//         result[i] = pow(sqrt_input[i], 2) * sign(source_input[i]);
//     }
// }


 static real laplace_5x5_1[6] = { 24, -1, -1, -1, -1, -1 };

 static real laplacian_5x5_1[25] =
{
    -1, -1, -1, -1, -1,
    -1, -1, -1, -1, -1,
    -1, -1, 24, -1, -1,
    -1, -1, -1, -1, -1,
    -1, -1, -1, -1, -1,
};

#elif LAPLACIAN_KERNEL == 70

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
 static real laplace_7x7_0[10] = { 8, 7, 6, 4, 3, 0, -1, -2, -5, -10 };
// real SafeSplitKernel7(real source_input[10], out real sqrt_input, out real result)
// {
//     if ((int)source_input == 0)
//     {
//         return *source_input;
//     }
//     sqrt_input = sqrt(abs(source_input[10]));
//
//     result = pow(sqrt_input, 2)*sign(source_input[10]);
//     return result;
// }

 static real laplacian_7x7_0[49] =
{
    -10, -5, -2, -1, -2, -5, -10,
     -5,  0,  3,  4,  3,  0,  -5,
     -2,  3,  6,  7,  6,  3,  -2,
     -1,  4,  7,  8,  7,  4,  -1,
     -2,  3,  6,  7,  6,  3,  -2,
     -5,  0,  3,  4,  3,  0,  -5,
    -10, -5, -2, -1, -2, -5, -10,
};
// -9 -4, -1, 0, -1, -4, -9


#elif LAPLACIAN_KERNEL == 90

 static real laplace_9x9_0[10] = { 8, 7, 6, 4, 3, 0, -1, -2, -5, -10 };

 static real laplacian_9x9_0[81] =
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
};
#else

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
static real laplacian_3x3_0[9] =
{
 -1, -1, -1,
 -1,  8, -1,
 -1, -1, -1,
};
/*
┌──────────────┬───────────────────────────────────────────────────────────────┐
│ Laplacian: 0 │ 3x3 Laplacian, with center:8 edge:-1 corner:-1                │
└──────────────┴───────────────────────────────────────────────────────────────┘
*/
static real laplace_3x3_0[] = {8, -1, -1};

#endif