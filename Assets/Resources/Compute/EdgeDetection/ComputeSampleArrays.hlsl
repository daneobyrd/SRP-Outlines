#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
CBUFFER_START(cb)
float4 _Size; // x: src width, y: src height, zw: unused
uint _KernelType;
CBUFFER_END
uint sample_size = 9;

// Row 1

uint2 px00 = uint2(min(0u, _Size.x), min(0u, _Size.y));
uint2 px01 = uint2(min(0u, _Size.x), min(1u, _Size.y));
uint2 px02 = uint2(min(0u, _Size.x), min(2u, _Size.y));
uint2 px03 = uint2(min(0u, _Size.x), min(3u, _Size.y));
uint2 px04 = uint2(min(0u, _Size.x), min(4u, _Size.y));
uint2 px05 = uint2(min(0u, _Size.x), min(5u, _Size.y));
uint2 px06 = uint2(min(0u, _Size.x), min(6u, _Size.y));

// Row 2

uint2 px10 = uint2(min(1u, _Size.x), min(0u, _Size.y));
uint2 px11 = uint2(min(1u, _Size.x), min(1u, _Size.y));
uint2 px12 = uint2(min(1u, _Size.x), min(2u, _Size.y));
uint2 px13 = uint2(min(1u, _Size.x), min(3u, _Size.y));
uint2 px14 = uint2(min(1u, _Size.x), min(4u, _Size.y));
uint2 px15 = uint2(min(1u, _Size.x), min(5u, _Size.y));
uint2 px16 = uint2(min(1u, _Size.x), min(6u, _Size.y));

// Row 3
 
uint2 px20 = uint2(min(2u, _Size.x), min(0u, _Size.y));
uint2 px21 = uint2(min(2u, _Size.x), min(1u, _Size.y));
uint2 px22 = uint2(min(2u, _Size.x), min(2u, _Size.y));
uint2 px23 = uint2(min(2u, _Size.x), min(3u, _Size.y));
uint2 px24 = uint2(min(2u, _Size.x), min(4u, _Size.y));
uint2 px25 = uint2(min(2u, _Size.x), min(5u, _Size.y));
uint2 px26 = uint2(min(2u, _Size.x), min(6u, _Size.y));

// Row 4

uint2 px30 = uint2(min(3u, _Size.x), min(0u, _Size.y));
uint2 px31 = uint2(min(3u, _Size.x), min(1u, _Size.y));
uint2 px32 = uint2(min(3u, _Size.x), min(2u, _Size.y));
uint2 px33 = uint2(min(3u, _Size.x), min(3u, _Size.y));
uint2 px34 = uint2(min(3u, _Size.x), min(4u, _Size.y));
uint2 px35 = uint2(min(3u, _Size.x), min(5u, _Size.y));
uint2 px36 = uint2(min(3u, _Size.x), min(6u, _Size.y));

// Row 5
 
uint2 px40 = uint2(min(4u, _Size.x), min(0u, _Size.y));
uint2 px41 = uint2(min(4u, _Size.x), min(1u, _Size.y));
uint2 px42 = uint2(min(4u, _Size.x), min(2u, _Size.y));
uint2 px43 = uint2(min(4u, _Size.x), min(3u, _Size.y));
uint2 px44 = uint2(min(4u, _Size.x), min(4u, _Size.y));
uint2 px45 = uint2(min(4u, _Size.x), min(5u, _Size.y));
uint2 px46 = uint2(min(4u, _Size.x), min(6u, _Size.y));

// Row 6

uint2 px50 = uint2(min(5u, _Size.x), min(0u, _Size.y));
uint2 px51 = uint2(min(5u, _Size.x), min(1u, _Size.y));
uint2 px52 = uint2(min(5u, _Size.x), min(2u, _Size.y));
uint2 px53 = uint2(min(5u, _Size.x), min(3u, _Size.y));
uint2 px54 = uint2(min(5u, _Size.x), min(4u, _Size.y));
uint2 px55 = uint2(min(5u, _Size.x), min(5u, _Size.y));
uint2 px56 = uint2(min(5u, _Size.x), min(6u, _Size.y));

// Row 7

uint2 px60 = uint2(min(6u, _Size.x), min(0u, _Size.y));
uint2 px61 = uint2(min(6u, _Size.x), min(1u, _Size.y));
uint2 px62 = uint2(min(6u, _Size.x), min(2u, _Size.y));
uint2 px63 = uint2(min(6u, _Size.x), min(3u, _Size.y));
uint2 px64 = uint2(min(6u, _Size.x), min(4u, _Size.y));
uint2 px65 = uint2(min(6u, _Size.x), min(5u, _Size.y));
uint2 px66 = uint2(min(6u, _Size.x), min(6u, _Size.y));

// ┌──────┬──────┬──────┐
// │ px00 │ px01 │ px02 │
// ├──────┼──────┼──────┤
// │ px10 │ px11 │ px12 │
// ├──────┼──────┼──────┤
// │ px20 │ px21 │ px22 │
// └──────┴──────┴──────┘
uint2 sample_3x3[9] {
    px00, px01, px02,
    px10, px11, px12,
    px20, px21, px22,
};

// ┌──────┬──────┬──────┬──────┬──────┐
// │ px00 │ px01 │ px02 │ px03 │ px04 │
// ├──────┼──────┼──────┼──────┼──────┤
// │ px10 │ px11 │ px12 │ px13 │ px14 │
// ├──────┼───── ╔══════╗ ─────┼──────┤
// │ px20 │ px21 ║ px22 ║ px23 │ px24 │
// ├──────┼───── ╚══════╝ ─────┼──────┤
// │ px30 │ px31 │ px32 │ px33 │ px34 │
// ├──────┼──────┼──────┼──────┼──────┤
// │ px40 │ px41 │ px42 │ px43 │ px44 │
// └──────┴──────┴──────┴──────┴──────┘
uint2 sample_5x5[25] = {
    px00, px01, px02, px03, px04,
    px10, px11, px12, px13, px14,
    px20, px21, px22, px23, px24,
    px30, px31, px32, px33, px34,
    px40, px41, px42, px43, px44,
};

// ┌──────┬──────┬──────┬──────┬──────┬──────┬──────┐
// │ px00 │ px01 │ px02 │ px03 │ px04 │ px05 │ px06 │
// ├──────┼──────┼──────┼──────┼──────┼──────┼──────┤
// │ px10 │ px11 │ px12 │ px13 │ px14 │ px15 │ px16 │
// ├──────┼──────┼──────┼──────┼──────┼──────┼──────┤
// │ px20 │ px21 │ px22 │ px23 │ px24 │ px25 │ px26 │
// ├──────┼──────┼───── ╔══════╗ ─────┼──────┼──────┤
// │ px30 │ px31 │ px32 ║ px33 ║ px34 │ px35 │ px36 │
// ├──────┼──────┼───── ╚══════╝ ─────┼──────┼──────┤
// │ px40 │ px41 │ px42 │ px43 │ px44 │ px45 │ px45 │
// ├──────┼──────┼──────┼──────┼──────┼──────┼──────┤
// │ px50 │ px51 │ px52 │ px53 │ px54 │ px55 │ px56 │
// ├──────┼──────┼──────┼──────┼──────┼──────┼──────┤
// │ px60 │ px61 │ px62 │ px63 │ px64 │ px65 │ px66 │
// └──────┴──────┴──────┴──────┴──────┴──────┴──────┘
uint2 sample_7x7[49]
{
    px00, px01, px02, px03, px04, px05, px06,
    px10, px11, px12, px13, px14, px15, px16,
    px20, px21, px22, px23, px24, px25, px26,
    px30, px31, px32, px33, px34, px35, px36,
    px40, px41, px42, px43, px44, px45, px46,
    px50, px51, px52, px53, px54, px55, px56,
    px60, px61, px62, px63, px64, px65, px66,
};

