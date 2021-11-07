// ---------------------------------------------------------------------------------------------------------------
// iq: https://www.shadertoy.com/view/4djyzK
// float2 p = (2.0 * uv.xy - _source_resolution) / _source_resolution.y;
// float2 e = float2(2.0 / _source_resolution.y, 0.0);
// float4 lap += (_source.SampleLevel(sampler_source(p + e.xy)));
// float4 lap + sampler_source(p - e.xy) + sampler_source(p + e.yx) + sampler_source(p - e.yx) - 4.0 * sampler_source(p)) / (e.x * e.x);

// ---------------------------------------------------------------------------------------------------------------
// https://www.shadertoy.com/view/MdlBz2
// float2 uv = id.xy / _source_resolution;
// float2 ps = 1.0 / _source_resolution; // pixel-size

// float4 acc;
// for (int i = 0; i < _sample_length, i++)
// {
// float2 d = float2(i % _kernel_size, i / _kernel_size) - float2(_kernel_size / 2, _kernel_size / 2);
// acc += sample_points[i] * _source.SampleLevel(sampler_source, uv + float2(d * ps), 0);
// }

// ---------------------------------------------------------------------------------------------------------------

// Sha sobel.compute
// float3 top_left      = _source.SampleLevel(sampler_source, uv + float2(-step.x,  step.y), 0).xyz;  // 1_1
// float3 center_left   = _source.SampleLevel(sampler_source, uv + float2(-step.x,     0.0), 0).xyz;  // 2_1
// float3 bottom_left   = _source.SampleLevel(sampler_source, uv + float2(-step.x, -step.y), 0).xyz;  // 3_1
// float3 top_center    = _source.SampleLevel(sampler_source, uv + float2(     0.,  step.y), 0).xyz;  // 1_2
// float3 bottom_center = _source.SampleLevel(sampler_source, uv + float2(     0., -step.y), 0).xyz;  // 3_2
// float3 top_right     = _source.SampleLevel(sampler_source, uv + float2( step.x,  step.y), 0).xyz;  // 1_3
// float3 center_right  = _source.SampleLevel(sampler_source, uv + float2( step.x,     0.0), 0).xyz;  // 2_3
// float3 buttom_right  = _source.SampleLevel(sampler_source, uv + float2( step.x, -step.y), 0).xyz;  // 3_3

// Sobel masks (see http://en.wikipedia.org/wiki/Sobel_operator)
//        | 1 0 -1 |     |-1 -2 -1 |
//    X = | 2 0 -2 |  Y =| 0  0  0 |
//        | 1 0 -1 |     | 1  2  1 |

//                                       Left                               -                  Right 
// float3 horizontal = top_left + 2.0 * center_left + bottom_left - (top_right + 2.0 * center_right + buttom_right);
//                                       Bottom                             -                  Top 
// float3 vertical = bottom_left + 2.0 * bottom_center + buttom_right - (top_left + 2.0 * top_center + top_right);

// float3 color = sqrt(horizontal * horizontal + vertical * vertical);    
// _result[id.xy] = float4(saturate(color.xyz), 1.);

// ---------------------------------------------------------------------------------------------------------------

// Source: https://mchouza.wordpress.com/2011/02/21/gpgpu-with-webgl-solving-laplaces-equation/
// wc is the color to the "west", ec is the color to the "east", ...
// float w_val = wc.r + wc.g / 255.0;
// float e_val = ec.r + ec.g / 255.0;
// ...
// float val = (w_val + e_val + n_val + s_val) / 4.0;
// float hi = val - (val % ( 1.0 / 255.0));
// float lo = (val - hi) * 255.0; 
//fragmentColor = float4(hi, lo, 0.0, 0.0);


/*      ┌─┬─┬─┬─┬─┬─┬─┬─┬─┬─┬─┬─┬─┬─┬─┬─┐
        ├─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┤
        ├─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┤
        ├─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┤
        ├─┼─┼─┼ ╔═╦═╦═╦═╦═╦═╦═╦═╗ ┼─┼─┼─┤
        ├─┼─┼─┼ ╠═╬═╬═╬═╬═╬═╬═╬═╣ ┼─┼─┼─┤
        ├─┼─┼─┼ ╠═╬═╬═╬═╬═╬═╬═╬═╣ ┼─┼─┼─┤
        ├─┼─┼─┼ ╠═╬═╬═╬═╬═╬═╬═╬═╣ ┼─┼─┼─┤
        ├─┼─┼─┼ ╠═╬═╬═╬═╬═╬═╬═╬═╣ ┼─┼─┼─┤
        ├─┼─┼─┼ ╠═╬═╬═╬═╬═╬═╬═╬═╣ ┼─┼─┼─┤
        ├─┼─┼─┼ ╠═╬═╬═╬═╬═╬═╬═╬═╣ ┼─┼─┼─┤
        ├─┼─┼─┼ ╠═╬═╬═╬═╬═╬═╬═╬═╣ ┼─┼─┼─┤
        ├─┼─┼─┼ ╚═╩═╩═╩═╩═╩═╩═╩═╝ ┼─┼─┼─┤
        ├─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┤
        ├─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┤
        ├─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┤
        └─┴─┴─┴─┴─┴─┴─┴─┴─┴─┴─┴─┴─┴─┴─┴─┘     */
/*      ┌──┬──┬──┬──┬──┬──┬──┬──┬──┬──┬──┬──┬──┬──┬──┬──┐
        ├──┼──┼──┼──┼──┼──┼──┼──┼──┼──┼──┼──┼──┼──┼──┼──┤
        ├──┼──┼──┼──┼──┼──┼──┼──┼──┼──┼──┼──┼──┼──┼──┼──┤
        ├──┼──┼──┼──┼──┼──┼──┼──┼──┼──┼──┼──┼──┼──┼──┼──┤
        ├──┼──┼──┼─ ╔══╦══╦══╦══╦══╦══╦══╦══╗ ─┼──┼──┼──┤
        ├──┼──┼──┼─ ╠══╬══╬══╬══╬══╬══╬══╬══╣ ─┼──┼──┼──┤
        ├──┼──┼──┼─ ╠══╬══╬══╬══╬══╬══╬══╬══╣ ─┼──┼──┼──┤
        ├──┼──┼──┼─ ╠══╬══╬══╬══╬══╬══╬══╬══╣ ─┼──┼──┼──┤
        ├──┼──┼──┼─ ╠══╬══╬══╬══╬══╬══╬══╬══╣ ─┼──┼──┼──┤
        ├──┼──┼──┼─ ╠══╬══╬══╬══╬══╬══╬══╬══╣ ─┼──┼──┼──┤
        ├──┼──┼──┼─ ╠══╬══╬══╬══╬══╬══╬══╬══╣ ─┼──┼──┼──┤
        ├──┼──┼──┼─ ╠══╬══╬══╬══╬══╬══╬══╬══╣ ─┼──┼──┼──┤
        ├──┼──┼──┼─ ╚══╩══╩══╩══╩══╩══╩══╩══╝ ─┼──┼──┼──┤
        ├──┼──┼──┼──┼──┼──┼──┼──┼──┼──┼──┼──┼──┼──┼──┼──┤
        ├──┼──┼──┼──┼──┼──┼──┼──┼──┼──┼──┼──┼──┼──┼──┼──┤
        ├──┼──┼──┼──┼──┼──┼──┼──┼──┼──┼──┼──┼──┼──┼──┼──┤
        └──┴──┴──┴──┴──┴──┴──┴──┴──┴──┴──┴──┴──┴──┴──┴──┘       */

//  ┌─────┬─────┬─────┬─────┬─────┬─────┬─────┬─────┬─────┐
//  │ -4  │ -3  │ -2  │ -1  │  0  │ +1  │ +2  │ +3  │ +4  │
//  └─────┴─────┴─────┴─────┴─────┴─────┴─────┴─────┴─────┘
//  ┌─────┬─────┬─────┬─────┬─────┬─────┬─────┬─────┬─────┐
//  │  a  │  b  │  c  │  d  │  e  │  f  │  g  │  h  │  i  │
//  └─────┴─────┴─────┴─────┴─────┴─────┴─────┴─────┴─────┘
//     │     │     │     │  ┌─────┐  │     │     │     │
//     │     │     │     └─→│ d+f │←─┘     │     │     │
//     │     │     │        └─────┘        │     │     │
//     │     │     │        ┌─────┐        │     │     │
//     │     │     └──────→ │ c+g │ ←──────┘     │     │
//     │     │              └─────┘              │     │
//     │     │              ┌─────┐              │     │
//     │     └────────────→ │ b+h │ ←────────────┘     │
//     │                    └─────┘                    │
//     │                    ┌─────┐                    │
//     └──────────────────→ │ a+i │ ←──────────────────┘
//                          └─────┘

//         ┌───┐
//       ┌─┘ e └─┐
//     ┌─┘ d + f └─┐
//   ┌─┘ c   +   g └─┐
// ┌─┘ b     +     h └─┐
// │ a       +       i │
// └───────────────────┘