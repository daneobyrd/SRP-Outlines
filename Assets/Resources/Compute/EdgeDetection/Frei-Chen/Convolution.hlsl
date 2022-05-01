#ifndef CONVOLUTION_INCLUDED
#define CONVOLUTION_INCLUDED

/*
00, 01, 02, 03, 04, 05, 06, 07, 08,
09, 10, 11, 12, 13, 14, 15, 16, 17,
18, 19, 20, 21, 22, 23, 24, 25, 26,
27, 28, 29, 30, 31, 32, 33, 34, 35,
36, 37, 38, 39, 40, 41, 42, 43, 44,
45, 46, 47, 48, 49, 50, 51, 52, 53,
54, 55, 56, 57, 58, 59, 60, 61, 62,
63, 64, 65, 66, 67, 68, 69, 70, 71,
72, 73, 74, 75, 76, 77, 78, 79, 80
*/


/*
int4x4 select_i_9x9
{
     8, 10, 16, 20,
    24, 30, 32, 40,
    48, 50, 56, 60,
    64, 70, 72, 80
};

int select_i_9x9_difference[17]
{
    8,
    2, 6, 4,
    4, 6, 2,
    8,
    8,
    2, 6, 4,
    4, 6, 2,
    8
};

int4x4 select_i_9x9_multipleof
{
    8, 10, 8, 10,
    8, 10, 8, 10,
    8, 10, 8, 10,
    8, 10, 8, 10
};
*/


/*
┌─────
│ 00, 01, 02, 03,
  09, 10, 11, 12,
  18, 19, 20, 21,
  27, 28, 29, 30,
 */
int2 top_left_sample[16] =
{
    int2(-4, 4), int2(-3, 4), int2(-2, 4), int2(-1, 4),
    int2(-4, 3), int2(-3, 3), int2(-2, 3), int2(-1, 3),
    int2(-4, 2), int2(-3, 2), int2(-2, 2), int2(-1, 2),
    int2(-4, 1), int2(-3, 1), int2(-2, 1), int2(-1, 1),
};

/*
───────────────┐
05, 06, 07, 08,│
14, 15, 16, 17,│
23, 24, 25, 26,│
32, 33, 34, 35,│
 */
int2 top_right_sample[16] =
{
    int2(1, 4), int2(2, 4), int2(3, 4), int2(4, 4),
    int2(1, 3), int2(2, 3), int2(3, 3), int2(4, 3),
    int2(1, 2), int2(2, 2), int2(3, 2), int2(4, 2),
    int2(1, 1), int2(2, 1), int2(3, 1), int2(4, 1)
};

/*
│ 45, 46, 47, 48,
│ 54, 55, 56, 57,
│ 63, 64, 65, 66,
│ 72, 73, 74, 75,
└───────────────
*/
int2 bottom_left_sample[16] =
{
    int2(-4, -4), int2(-3, -4), int2(-2, -4), int2(-1, -4),
    int2(-4, -3), int2(-3, -3), int2(-2, -3), int2(-1, -3),
    int2(-4, -2), int2(-3, -2), int2(-2, -2), int2(-1, -2),
    int2(-4, -1), int2(-3, -1), int2(-2, -1), int2(-1, -1),
};

/*
50, 51, 52, 53,│
59, 60, 61, 62,│
68, 69, 70, 71,│
77, 78, 79, 80 │
───────────────┘
 */
int2 bottom_right_sample[16] =
{
    int2(1, -4), int2(2, -4), int2(3, -4), int2(4, -4),
    int2(1, -3), int2(2, -3), int2(3, -3), int2(4, -3),
    int2(1, -2), int2(2, -2), int2(3, -2), int2(4, -2),
    int2(1, -1), int2(2, -1), int2(3, -1), int2(4, -1)
};

int2 cross_sample[16] =
{
                                                        int2(0, 1),
                                                        int2(0, 2),
                                                        int2(0, 3),
                                                        int2(0, 4),
    int2(-4, 0), int2(-3, 0), int2(-2, 0), int2(-1, 0),             int2(1, 0),  int2(2, 0),  int2(3, 0),  int2(4, 0),
                                                        int2(0, -4),
                                                        int2(0, -3),
                                                        int2(0, -2),
                                                        int2(0, -1)
};

/*
float choose_9x9(in float pick_from[3])
{
    float j = 0;
    for (int i = 0; i < 80; i++)
    {
        bool mult_of_8 = fmod(i, 8) == 0;
        bool mult_of_10 = fmod(i, 10) == 0;
        int current_row = floor(i / 9);
        int next_row = ceil(i / 9);

        if (i != 40)
        {
            if (mult_of_10)
            {
                j = pick_from[0];
            }
            else if (mult_of_8)
            {
                j = pick_from[2];
            }
        }
        else
        {
            j = pick_from[1];
        }
    }
    return j;
}

void row_x_sample(out int2 new_sample_offset)
{
    for (int i = 0; i < 80; i++)
    {
        int current_row = floor(i / 9);
        sample_offsets[i + 1 / 9] * row_offsets[current_row];
    }
}
*/

#endif