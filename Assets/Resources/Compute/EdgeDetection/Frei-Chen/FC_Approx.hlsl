﻿// https://www.rasterGrid.com/bloG/2011/01/frei-chen-edGe-detector/

static float3x3 G0G1 =
{
    0.7071067812, 1,  0,
    0.5,          0, -0.5,
    0,           -1, -0.7071067812
};

static float3x3 G2G3 =
{
     0.5,    -0.7071067812,	 0.5,
     0,	      0,         	 0,
    -0.5,	  0.7071067812,	-0.5
};

static float3x3 G4G5 =
{
    -0.5, 0.5,  0.5,
    -0.5, 0.0, -0.5,
     0.5, 0.5, -0.5
};

/*         Frei-Chen G6                  
 *       ┌────┬────┬────┐              
 *       │  1 │ -2 │  1 │              
 *   1   ├────┼────┼────┤              
 *  ───  │ -2 │  4 │ -2 │              
 *   6   ├────┼────┼────┤              
 *       │  1 │ -2 │  1 │              
 *       └────┴────┴────┘
 */
static float3x3 G6G7 =
{
    -0.1666666,	-0.1666666,	-0.1666666,
    -0.1666666,	1.3333334,	-0.1666666,
    -0.1666666,	-0.1666666,	-0.1666666
};

/*        Frei-Chen G8                  
 *      ┌────┬────┬────┐              
 *      │  1 │  1 │  1 │              
 *   1  ├────┼────┼────┤              
 *  ─── │  1 │  1 │  1 │              
 *   3  ├────┼────┼────┤              
 *      │  1 │  1 │  1 │              
 *      └────┴────┴────┘              
 */
static float3x3 G8m_ =
{
    1.540440081, 0.6262265188,	0.8333333,
    0.8333333,	 0.3333333,	   -0.1666667,
    -0.1666667,	 0.0404400812, -0.8737734812
};

static float3x3 m_ =
{
    1.2071068, 1,          1,
    0.5,       0,         -0.5,
   -0.5,      -0.2928932, -1.2071068
};


/**
 * \brief Sum of Frei-Chen filters G4 through G8
 */
static float3x3 s_ =
{
    -0.3333333, 0.6666667,  0.6666667,
    -0.3333333, 1.6666667, -0.3333333,
     0.6666667, 0.6666667, -0.3333333
};

// Sum of G4 through G8 + M
static float3x3 s_m =
{
    0.8737734812,	0.9595599,	1.1666667,
    0.1666667,	1.6666667,	-0.8333333,
    0.1666667,	0.3737734,	-1.540440081,
};

// M = I²(G₀² + G₁² + G₂² + G₃²)
// G₀-G₃
static float3x3 sq_G0G3 =
{
    0.5,	0.75,	0.5,
    0.5,	0,	    0.5,
    0.5,	0.75,	0.5
};

// S = sq_G4G8 + M
static float3x3 sq_G4G8 =
{
    0.5,	0.5,	0.5,
    0.5,	1,  	0.5,
    0.5,	0.5,	0.5
};