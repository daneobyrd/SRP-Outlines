
// ┌─────┬─────┬─────┐
// │ p00 │ p01 │ p02 │
// ├─────┼─────┼─────┤
// │ p10 │ p11 │ p12 │
// ├─────┼─────┼─────┤
// │ p20 │ p21 │ p22 │
// └─────┴─────┴─────┘
void get_sample_3x3(float4 size, out uint2 sample_3x3[9]) {
    uint2 p00 = uint2(min(0u, size.x), min(0u, size.y));
    uint2 p01 = uint2(min(0u, size.x), min(1u, size.y));
    uint2 p02 = uint2(min(0u, size.x), min(2u, size.y));
    uint2 p10 = uint2(min(1u, size.x), min(0u, size.y));
    uint2 p11 = uint2(min(1u, size.x), min(1u, size.y));
    uint2 p12 = uint2(min(1u, size.x), min(2u, size.y));
    uint2 p20 = uint2(min(2u, size.x), min(0u, size.y));
    uint2 p21 = uint2(min(2u, size.x), min(1u, size.y));
    uint2 p22 = uint2(min(2u, size.x), min(2u, size.y));
    
    uint2 temp[9] = {
        p00, p01, p02,
        p10, p11, p12,
        p20, p21, p22
    };
    sample_3x3 = temp;
}

// ┌─────┬─────┬─────┬─────┬─────┐
// │ p00 │ p01 │ p02 │ p03 │ p04 │
// ├─────┼─────┼─────┼─────┼─────┤
// │ p10 │ p11 │ p12 │ p13 │ p14 │
// ├─────┼──── ╔═════╗ ────┼─────┤
// │ p20 │ p21 ║ p22 ║ p23 │ p24 │
// ├─────┼──── ╚═════╝ ────┼─────┤
// │ p30 │ p31 │ p32 │ p33 │ p34 │
// ├─────┼─────┼─────┼─────┼─────┤
// │ p40 │ p41 │ p42 │ p43 │ p44 │
// └─────┴─────┴─────┴─────┴─────┘
void get_sample_5x5(float4 size, out uint2 sample_5x5[25]) {
    uint2 p00 = uint2(min(0u, size.x), min(0u, size.y));
    uint2 p01 = uint2(min(0u, size.x), min(1u, size.y));
    uint2 p02 = uint2(min(0u, size.x), min(2u, size.y));
    uint2 p03 = uint2(min(0u, size.x), min(3u, size.y));
    uint2 p04 = uint2(min(0u, size.x), min(4u, size.y));

    uint2 p10 = uint2(min(1u, size.x), min(0u, size.y));
    uint2 p11 = uint2(min(1u, size.x), min(1u, size.y));
    uint2 p12 = uint2(min(1u, size.x), min(2u, size.y));
    uint2 p13 = uint2(min(1u, size.x), min(3u, size.y));
    uint2 p14 = uint2(min(1u, size.x), min(4u, size.y));
    
    uint2 p20 = uint2(min(2u, size.x), min(0u, size.y));
    uint2 p21 = uint2(min(2u, size.x), min(1u, size.y));
    uint2 p22 = uint2(min(2u, size.x), min(2u, size.y));
    uint2 p23 = uint2(min(2u, size.x), min(3u, size.y));
    uint2 p24 = uint2(min(2u, size.x), min(4u, size.y));
    
    uint2 p30 = uint2(min(3u, size.x), min(0u, size.y));
    uint2 p31 = uint2(min(3u, size.x), min(1u, size.y));
    uint2 p32 = uint2(min(3u, size.x), min(2u, size.y));
    uint2 p33 = uint2(min(3u, size.x), min(3u, size.y));
    uint2 p34 = uint2(min(3u, size.x), min(4u, size.y));
    
    uint2 p40 = uint2(min(4u, size.x), min(0u, size.y));
    uint2 p41 = uint2(min(4u, size.x), min(1u, size.y));
    uint2 p42 = uint2(min(4u, size.x), min(2u, size.y));
    uint2 p43 = uint2(min(4u, size.x), min(3u, size.y));
    uint2 p44 = uint2(min(4u, size.x), min(4u, size.y));
    
    uint2 temp[25] = {
        p00, p01, p02, p03, p04,
        p10, p11, p12, p13, p14,
        p20, p21, p22, p23, p24,
        p30, p31, p32, p33, p34,
        p40, p41, p42, p43, p44
    };
    sample_5x5 = temp;
}


// ┌─────┬─────┬─────┬─────┬─────┬─────┬─────┐
// │ p00 │ p01 │ p02 │ p03 │ p04 │ p05 │ p06 │
// ├─────┼─────┼─────┼─────┼─────┼─────┼─────┤
// │ p10 │ p11 │ p12 │ p13 │ p14 │ p15 │ p16 │
// ├─────┼─────┼─────┼─────┼─────┼─────┼─────┤
// │ p20 │ p21 │ p22 │ p23 │ p24 │ p25 │ p26 │
// ├───────────┼──── ╔═════╗ ────┼─────┼─────┤
// │ p30 │ p31 │ p32 ║ p33 ║ p34 │ p35 │ p36 │
// ├─────┼─────┼──── ╚═════╝ ────┼─────┼─────┤
// │ p40 │ p41 │ p42 │ p43 │ p44 │ p45 │ p45 │
// ├─────┼─────┼─────┼─────┼─────┼─────┼─────┤
// │ p50 │ p51 │ p52 │ p53 │ p54 │ p55 │ p56 │
// ├─────┼─────┼─────┼─────┼─────┼─────┼─────┤
// │ p60 │ p61 │ p62 │ p63 │ p64 │ p65 │ p66 │
// └─────┴─────┴─────┴─────┴─────┴─────┴─────┘
void get_sample_7x7(float4 size, out uint2 sample_7x7[49])
{
    // Row 1
    uint2 p00 = uint2(min(0u, size.x), min(0u, size.y));
    uint2 p01 = uint2(min(0u, size.x), min(1u, size.y));
    uint2 p02 = uint2(min(0u, size.x), min(2u, size.y));
    uint2 p03 = uint2(min(0u, size.x), min(3u, size.y));
    uint2 p04 = uint2(min(0u, size.x), min(4u, size.y));
    uint2 p05 = uint2(min(0u, size.x), min(5u, size.y));
    uint2 p06 = uint2(min(0u, size.x), min(6u, size.y));
    // Row 2
    uint2 p10 = uint2(min(1u, size.x), min(0u, size.y));
    uint2 p11 = uint2(min(1u, size.x), min(1u, size.y));
    uint2 p12 = uint2(min(1u, size.x), min(2u, size.y));
    uint2 p13 = uint2(min(1u, size.x), min(3u, size.y));
    uint2 p14 = uint2(min(1u, size.x), min(4u, size.y));
    uint2 p15 = uint2(min(1u, size.x), min(5u, size.y));
    uint2 p16 = uint2(min(1u, size.x), min(6u, size.y));
    // Row 3
    uint2 p20 = uint2(min(2u, size.x), min(0u, size.y));
    uint2 p21 = uint2(min(2u, size.x), min(1u, size.y));
    uint2 p22 = uint2(min(2u, size.x), min(2u, size.y));
    uint2 p23 = uint2(min(2u, size.x), min(3u, size.y));
    uint2 p24 = uint2(min(2u, size.x), min(4u, size.y));
    uint2 p25 = uint2(min(2u, size.x), min(5u, size.y));
    uint2 p26 = uint2(min(2u, size.x), min(6u, size.y));
    // Row 4
    uint2 p30 = uint2(min(3u, size.x), min(0u, size.y));
    uint2 p31 = uint2(min(3u, size.x), min(1u, size.y));
    uint2 p32 = uint2(min(3u, size.x), min(2u, size.y));
    uint2 p33 = uint2(min(3u, size.x), min(3u, size.y));
    uint2 p34 = uint2(min(3u, size.x), min(4u, size.y));
    uint2 p35 = uint2(min(3u, size.x), min(5u, size.y));
    uint2 p36 = uint2(min(3u, size.x), min(6u, size.y));
    // Row 5
    uint2 p40 = uint2(min(4u, size.x), min(0u, size.y));
    uint2 p41 = uint2(min(4u, size.x), min(1u, size.y));
    uint2 p42 = uint2(min(4u, size.x), min(2u, size.y));
    uint2 p43 = uint2(min(4u, size.x), min(3u, size.y));
    uint2 p44 = uint2(min(4u, size.x), min(4u, size.y));
    uint2 p45 = uint2(min(4u, size.x), min(5u, size.y));
    uint2 p46 = uint2(min(4u, size.x), min(6u, size.y));
    // Row 6
    uint2 p50 = uint2(min(5u, size.x), min(0u, size.y));
    uint2 p51 = uint2(min(5u, size.x), min(1u, size.y));
    uint2 p52 = uint2(min(5u, size.x), min(2u, size.y));
    uint2 p53 = uint2(min(5u, size.x), min(3u, size.y));
    uint2 p54 = uint2(min(5u, size.x), min(4u, size.y));
    uint2 p55 = uint2(min(5u, size.x), min(5u, size.y));
    uint2 p56 = uint2(min(5u, size.x), min(6u, size.y));
    // Row 7
    uint2 p60 = uint2(min(6u, size.x), min(0u, size.y));
    uint2 p61 = uint2(min(6u, size.x), min(1u, size.y));
    uint2 p62 = uint2(min(6u, size.x), min(2u, size.y));
    uint2 p63 = uint2(min(6u, size.x), min(3u, size.y));
    uint2 p64 = uint2(min(6u, size.x), min(4u, size.y));
    uint2 p65 = uint2(min(6u, size.x), min(5u, size.y));
    uint2 p66 = uint2(min(6u, size.x), min(6u, size.y));

    uint2 temp[49] = {
        p00, p01, p02, p03, p04, p05, p06,
        p10, p11, p12, p13, p14, p15, p16,
        p20, p21, p22, p23, p24, p25, p26,
        p30, p31, p32, p33, p34, p35, p36,
        p40, p41, p42, p43, p44, p45, p46,
        p50, p51, p52, p53, p54, p55, p56,
        p60, p61, p62, p63, p64, p65, p66
    };
    sample_7x7 = temp;
}
