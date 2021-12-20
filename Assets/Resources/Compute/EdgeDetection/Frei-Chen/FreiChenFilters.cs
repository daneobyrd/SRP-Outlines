﻿using UnityEngine;

namespace Resources.Compute.EdgeDetection.Frei_Chen
{
    public class FreiChenFilters : MonoBehaviour
    {
        /*           Frei-Chen g1
         *         ┌────┬────┬────┐
         *         │  1 │ √2 │  1 │
         *     1   ├────┼────┼────┤
         *   ───── │  0 │  0 │  0 │
         *   2√(2) ├────┼────┼────┤
         *         │ -1 │-√2 │ -1 │
         *         └────┴────┴────┘
         */
        public float[] frei_chen_g1 =
        {
            0.3535534f, 0.4999998f,    0.3535534f,
            0f,                 0f,            0f,
            -0.3535534f, -0.4999998f, -0.3535534f
        };

        /*           Frei-Chen g2
         *         ┌────┬────┬────┐
         *         │  1 │  0 │ -1 │
         *     1   ├────┼────┼────┤
         *   ───── │ √2 │  0 │-√2 │
         *   2√(2) ├────┼────┼────┤
         *         │  1 │  0 │ -1 │
         *         └────┴────┴────┘
         */
        public float[] freiChenG2 =
        {
            0.3535534f, 0f, -0.3535534f,
            0.4999998f, 0f, -0.4999998f,
            0.3535534f, 0f, -0.3535534f
        };

        /*           Frei-Chen g3
         *         ┌────┬────┬────┐
         *         │  0 │ -1 │ √2 │
         *     1   ├────┼────┼────┤
         *   ───── │  1 │  0 │ -1 │
         *   2√(2) ├────┼────┼────┤
         *         │-√2 │  1 │  0 │
         *         └────┴────┴────┘
         */
        public float[] frei_chen_g3 =
        {
            0f, -0.3535534f, 0.4999998f,
            0.3535534f, 0f, -0.3535534f,
            -0.4999998f, 0.3535534f, 0
        };

        /*           Frei-Chen g4                  
         *         ┌────┬────┬────┐              
         *         │ √2 │ -1 │  0 │              
         *     1   ├────┼────┼────┤              
         *   ───── │ -1 │  0 │  1 │              
         *   2√(2) ├────┼────┼────┤              
         *         │  0 │  1 │-√2 │              
         *         └────┴────┴────┘
         */
        public float[] frei_chen_g4 =
        {
            0.4999998f, -0.3535534f, 0f,
            -0.3535534f, 0f, 0.3535534f,
            0f, 0.3535534f, -0.4999998f
        };

        /*           Frei-Chen g5                  
         *         ┌────┬────┬────┐              
         *         │  0 │  1 │  0 │              
         *     1   ├────┼────┼────┤              
         *    ───  │ -1 │  0 │ -1 │              
         *     2   ├────┼────┼────┤              
         *         │  0 │  1 │  0 │              
         *         └────┴────┴────┘
         */
        public float[] frei_chen_g5 =
        {
            0.0f, 0.5f, 0.0f,
            -0.5f, 0.0f, -0.5f,
            0.0f, 0.5f, 0.0f
        };

        /*         Frei-Chen g6                  
         *       ┌────┬────┬────┐              
         *       │ -1 │  0 │  1 │              
         *   1   ├────┼────┼────┤              
         *  ───  │  0 │  0 │  0 │              
         *   2   ├────┼────┼────┤              
         *       │  1 │  0 │ -1 │              
         *       └────┴────┴────┘
         */
        public float[] frei_chen_g6 =
        {
            -0.5f, 0.0f, 0.5f,
            0.0f, 0.0f, 0.0f,
            0.5f, 0.0f, -0.5f
        };

        /*         Frei-Chen g7                  
         *       ┌────┬────┬────┐              
         *       │  1 │ -2 │  1 │              
         *   1   ├────┼────┼────┤              
         *  ───  │ -2 │  4 │ -2 │              
         *   6   ├────┼────┼────┤              
         *       │  1 │ -2 │  1 │              
         *       └────┴────┴────┘
         */
        public float[] frei_chen_g7 =
        {
            0.1666667f, -0.3333333f, 0.1666667f,
            -0.3333333f, 0.6666667f, -0.3333333f,
            0.1666667f, -0.3333333f, 0.1666667f
        };

        /*
         *         Frei-Chen g8                  
         *       ┌────┬────┬────┐              
         *       │ -2 │  1 │ -2 │              
         *   1   ├────┼────┼────┤              
         *  ───  │  1 │  4 │  1 │              
         *   6   ├────┼────┼────┤              
         *       │ -2 │  1 │ -2 │              
         *       └────┴────┴────┘              
         */
        public float[] frei_chen_g8 =
        {
            -0.3333333f, 0.1666667f, -0.3333333f,
            0.1666667f, 0.6666667f, 0.1666667f,
            -0.3333333f, 0.1666667f, -0.3333333f,
        };

        /*        Frei-Chen g9                  
         *      ┌────┬────┬────┐              
         *      │  1 │  1 │  1 │              
         *   1  ├────┼────┼────┤              
         *  ─── │  1 │  1 │  1 │              
         *   3  ├────┼────┼────┤              
         *      │  1 │  1 │  1 │              
         *      └────┴────┴────┘              
         */
        public float[] frei_chen_g9 =
        {
            0.3333333f, 0.3333333f, 0.3333333f,
            0.3333333f, 0.3333333f, 0.3333333f,
            0.3333333f, 0.3333333f, 0.3333333f
        };
    }
}