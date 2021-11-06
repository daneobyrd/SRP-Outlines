// Some laplacian kernels: https://legacy.imagemagick.org/Usage/convolve/#laplacian

// 1D convolution kernel
    float laplacian_5x1_0[5] = {0.05, 0.25, 0.4, 0.25, 0.05};

//─┬────────────────────────┬─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
// │ Laplacian: 0 (default) │  3x3 Laplacian, with center:8 edge:-1 corner:-1
// ├────────────────────────┴──────────────────────────────────────┐
// │ Kernel "Laplacian" of size 3x3+1+1 with values from -1 to 8.  │
// │ Forming a output range from -8 to 8 (Zero-Summing).           │
// └───────────────────────────────────────────────────────────────┘
    float laplacian_3x3_3[9] = {
        -1, -1, -1,
        -1,  8, -1,
        -1, -1, -1,
    };
    // Sometimes a Laplacian, whether it is a discrete Laplacian, as in the last example,
    // or a generated 'LoG' or 'DoG' produces a result that is more complex than is desired.
    // In such cases, generating an unbiased image, (without any Output Bias) will work better.
     
    // https://legacy.imagemagick.org/Usage/convolve/face_laplacian_0.png
    // https://legacy.imagemagick.org/Usage/convolve/face_laplacian_positives.png
    // https://legacy.imagemagick.org/Usage/convolve/face_laplacian_negatives.png
 
//─┬──────────────┬───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
// │ Laplacian: 1 │  The 4 neighbour Laplacian. Also very commonly used.
// ├──────────────┴────────────────────────────────────────────────┐
// │ Kernel "Laplacian" of size 3x3+1+1 with values from -1 to 4.  │
// │ Forming a output range from -4 to 4 (Zero-Summing).           │
// └───────────────────────────────────────────────────────────────┘
    float laplacian_3x3_0[9] = {
        0, -1, 0,
        -1, 4, -1,
        0, -1, 0,
    };
    // The results are not as strong, but are often clearer than the 8-neighbour laplacian.

//─┬──────────────┬───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
// │ Laplacian: 2 │  3x3 Laplacian, with center:4 edge:1 corner:-2
// ├──────────────┴────────────────────────────────────────────────┐
// │ Kernel "Laplacian" of size 3x3+1+1 with values from -2 to 4.  │
// │ Forming a output range from -8 to 8 (Zero-Summing).           │
// └───────────────────────────────────────────────────────────────┘
    float laplacian_3x3_1[9] = {
        -2,  1, -2,
         1,  4,  1,
        -2,  1, -2
    };
 
//─┬──────────────┬───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
// │ Laplacian: 3 │  3x3 Laplacian, with center:4 edge:-2 corner:1 
// ├──────────────┴────────────────────────────────────────────────┐
// │ Kernel "Laplacian" of size 3x3+1+1 with values from -2 to 4.  │
// │ Forming a output range from -8 to 8 (Zero-Summing).           │
// └───────────────────────────────────────────────────────────────┘
    float laplacian_3x3_2[9] = {
         1, -2,  1,
        -2,  4, -2,
         1, -2,  1
    };
    // This kernel highlights diagonal edges, and tends to make vertical and horizontal edges vanish.
    // However you may need to scale the results to see make any result visible.
 
//─┬──────────────┬───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
// │ Laplacian: 5 │
// ├──────────────┴────────────────────────────────────────────────┐
// │ Kernel "Laplacian" of size 5x5+2+2 with values from -4 to 4.  │
// │ Forming a output range from -24 to 24 (Zero-Summing).         │
// └───────────────────────────────────────────────────────────────┘
    float laplacian_5x5_0[25] = {
        -4, -1,  0, -1, -4,
        -1,  2,  3,  2, -1,
         0,  3,  4,  3,  0,
        -1,  2,  3,  2, -1,
        -4, -1,  0, -1, -4,
    };
    // The rule-of-thumb with laplacian kernels is the larger they are the cleaner the result, especially when errors are involved.
    // However you also get less detail.

    // Another 5x5
    float laplacian_5x5_1[25] = {
        -1, -1, -1, -1, -1,
        -1, -1, -1, -1, -1,
        -1, -1, 24, -1, -1,
        -1, -1, -1, -1, -1,
        -1, -1, -1, -1, -1,
    };

//─┬──────────────┬───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
// │ Laplacian: 7 │
// ├──────────────┴────────────────────────────────────────────────┐
// │ Kernel "Laplacian" of size 7x7+3+3 with values from -10 to 8. │
// │ Forming a output range from -1e+02 to 1e+02 (Zero-Summing).   │
// └───────────────────────────────────────────────────────────────┘
    float laplacian_7x7_0[49] = {
        -10, -5, -2, -1, -2, -5, -10,
         -5,  0,  3,  4,  3,  0,  -5,
         -2,  3,  6,  7,  6,  3,  -2,
         -1,  4,  7,  8,  7,  4,  -1,
         -2,  3,  6,  7,  6,  3,  -2,
         -5,  0,  3,  4,  3,  0,  -5,
        -10, -5, -2, -1, -2, -5, -10,
    };

// For copy/paste
// ┌────┬────┐
// │    │    │
// ├────┼────┤
// │    │    │
// └────┴────┘
