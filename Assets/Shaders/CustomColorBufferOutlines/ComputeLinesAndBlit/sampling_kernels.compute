#ifndef SAMPLING_KERNELS_INCLUDED
#define SAMPLING_KERNELS_INCLUDED

//These are some different kernel arrays I gathered from online. I plan on using a gaussian pyramid + laplace kernel for edge detection.


static float2 sample_points[9] = {
    float2(-1, 1), float2(0, 1), float2(1, 1),
    float2(-1, 0), float2(0, 0), float2(1, 0),
    float2(-1, -1), float2(0, -1), float2(1, -1),
};

#define LAPLACE_KERNEL 0
#if LAPLACE_KERNEL == 0

static float laplace_matrix[9] = {
    0, -1, 0,
    -1, 4, -1,
    0, -1, 0,
};

#elif LAPLACE_KERNEL == 1

static float laplace_matrix[9] = {
    -1, -1, -1,
    -1,  8, -1,
    -1, -1, -1,
};

#elif LAPLACE_KERNEL == 2
    
static float laplace_matrix[25] = {
    -1, -1, -1, -1, -1,
    -1, -1, -1, -1, -1,
    -1, -1, 24, -1, -1,
    -1, -1, -1, -1, -1,
    -1, -1, -1, -1, -1,
};

#elif LAPLACE_KERNEL == 3
// 1D convolution kernel
static float laplace_matrix[5] = { 0.05, 0.25, 0.4, 0.25, 0.05 }

#endif


#define GAUSSIAN_KERNEL 0
#if GAUSSIAN_KERNEL == 0

// Discrete approximation to Gaussian function with standard dev=1.0 
static float gaussian_matrix[25] = {
    1, 4, 7, 4, 1,
    4, 16, 26, 16, 4,
    7, 26, 41, 26, 7,
    1, 4, 7, 4, 1,
    4, 16, 26, 16, 4
};
#elif GAUSSIAN_KERNEL == 1
// https://homepages.inf.ed.ac.uk/rbf/HIPR2/gsmooth.htm
// 1D convolution kernel 
static float gaussian_matrix[7] = {
    .006, .061, .242, .383, .242, .061, .006
};
#endif

// https://github.com/Unity-Technologies/Graphics/blob/master/com.unity.render-pipelines.universal/Shaders/PostProcessing/GaussianDepthOfField.shader
#define BLUR_KERNEL 0

#if BLUR_KERNEL == 0

// Offsets & coeffs for optimized separable bilinear 3-tap gaussian (5-tap equivalent)
const static int kTapCount = 3;
const static float kOffsets[] = {
    -1.33333333,
    0.00000000,
    1.33333333
};
const static half kCoeffs[] = {
    0.35294118,
    0.29411765,
    0.35294118
};

#elif BLUR_KERNEL == 1

// Offsets & coeffs for optimized separable bilinear 5-tap gaussian (9-tap equivalent)
const static int kTapCount = 5;
const static float kOffsets[] = {
    -3.23076923,
    -1.38461538,
     0.00000000,
     1.38461538,
     3.23076923
};
const static half kCoeffs[] = {
    0.07027027,
    0.31621622,
    0.22702703,
    0.31621622,
    0.07027027
};

#endif
#endif
