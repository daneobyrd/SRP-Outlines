// Some laplacian kernels: https://legacy.imagemagick.org/Usage/convolve/#laplacian

public class aplacianFilters
{
    private uint _arraySize;

    private float[] _laplacian;

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
    private float[] _laplacian3X3_0 =
    {
        -1f, -1f, -1f,
        -1f,  8f, -1f,
        -1f, -1f, -1f
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
    private float[] _laplacian3X3_1 =
    {
        0f, -1f, 0f,
        -1f, 4f, -1f,
        0f, -1f, 0f
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
    private readonly float[] _laplacian3X3_2 =
    {
        -2f, 1f, -2f,
        1f, 4f, 1f,
        -2f, 1f, -2f
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
    private float[] _laplacian3X3_3 =
    {
        1f, -2f, 1f,
        -2f, 4f, -2f,
        1f, -2f, 1f
    };

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
    private float[] _laplacian5X5_0 =
    {
        -4f, -1f, 0f, -1f, -4f,
        -1f,  2f, 3f,  2f, -1f,
         0f,  3f, 4f,  3f,  0f,
        -1f,  2f, 3f,  2f, -1f,
        -4f, -1f, 0f, -1f, -4f,
    };

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
    private float[] _laplacian5X5_1 =
    {
        -1f, -1f, -1f, -1f, -1f,
        -1f, -1f, -1f, -1f, -1f,
        -1f, -1f, 24f, -1f, -1f,
        -1f, -1f, -1f, -1f, -1f,
        -1f, -1f, -1f, -1f, -1f,
    };

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
    private float[] _laplacian7X7_0 =
    {
        -10f, -5f, -2f, -1f, -2f, -5f, -10f,
         -5f,  0f,  3f,  4f,  3f,  0f, -5f,
         -2f,  3f,  6f,  7f,  6f,  3f, -2f,
         -1f,  4f,  7f,  8f,  7f,  4f, -1f,
         -2f,  3f,  6f,  7f,  6f,  3f, -2f,
         -5f,  0f,  3f,  4f,  3f,  0f, -5f,
        -10f, -5f, -2f, -1f, -2f, -5f, -10f,
    };  
}       