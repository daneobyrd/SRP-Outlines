int _kernel_size;
int _kernel_type;
#define LAPLACE_KERNEL(_kernel_type, _kernel_size) 
#ifdef LAPLACE_KERNEL(1, 3)

float laplace_filter[9] = {
     0, -1,  0,
    -1,  4, -1,
     0, -1,  0,
};
#elif LAPLACE_KERNEL(2, 3)

float laplace_filter[9] = {
    -1, -1, -1,
    -1,  8, -1,
    -1, -1, -1,
};

#elif LAPLACE_KERNEL(3, 25)

float laplace_filter[25] = {
    -1, -1, -1, -1, -1,
    -1, -1, -1, -1, -1,
    -1, -1, 24, -1, -1,
    -1, -1, -1, -1, -1,
    -1, -1, -1, -1, -1,
};

#elif LAPLACE_KERNEL(4, 5)
// 1D convolution kernel
float laplace_filter[5] = { 0.05, 0.25, 0.4, 0.25, 0.05 }

#endif