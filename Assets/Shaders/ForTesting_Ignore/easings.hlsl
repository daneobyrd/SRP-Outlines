#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"

// Sine

float easeInSine(in float x)
{
    return 1 - cos((x * PI) / 2);
}

float easeOutSine(in float x)
{
    return sin((x * PI) / 2);
}

float easeInOutSine(in float x)
{
    return -(cos(PI * x) - 1) / 2;
}

// Cubic

float easeInCubic(in float x)
{
    return x * x * x;
}

float easeOutCubic(in float x)
{
    return 1 - pow(1 - x, 3);
}

float easeInOutCubic(in float x)
{
    return x < 0.5 ? 4 * x * x * x : 1 - pow(-2 * x + 2, 3) / 2;
}

// Quint

float easeInQuint(in float x)
{
    return x * x * x * x * x;
}

float easeOutQuint(in float x)
{
    return 1 - pow(1 - x, 5);
}

float easeInOutQuint(in float x)
{
    return x < 0.5 ? 16 * x * x * x * x * x : 1 - pow(-2 * x + 2, 5) / 2;
}

// Circle

float easeInCirc(in float x)
{
    return 1 - sqrt(1 - pow(x, 2));
}

float easeOutCirc(in float x)
{
    return sqrt(1 - pow(x - 1, 2));
}

// Elastic

float easeInElastic(in float x)
{
    const float c4 = (2 * PI) / 3;

    return x == 0
            ? 0
            : x == 1
            ? 1
            : -pow(2, 10 * x - 10) * sin((x * 10 - 10.75) * c4);
}

float easeOutElastic(in float x)
{
    const float c4 = (2 * PI) / 3;

    return x == 0
            ? 0
            : x == 1
            ? 1
            : pow(2, -10 * x) * sin((x * 10 - 0.75) * c4) + 1;
}

float easeInOutElastic(in float x)
{
    const float c5 = (2 * PI) / 4.5;

    return
        x == 0
            ? 0
            : x == 1
            ? 1
            : x < 0.5
            ? -(pow(2, 20 * x - 10) * sin((20 * x - 11.125) * c5)) / 2
            : (pow(2, -20 * x + 10) * sin((20 * x - 11.125) * c5)) / 2 + 1;
}

// Quad

float easeInQuad(in float x)
{
    return x * x;
}

float easeOutQuad(in float x)
{
    return 1 - (1 - x) * (1 - x);
}

float easeInOutQuad(in float x)
{
    return x < 0.5 ? 2 * x * x : 1 - pow(-2 * x + 2, 2) / 2;

}

// Quart

float easeInQuart(in float x)
{
    return x * x * x * x;

}

float easeOutQuart(in float x)
{
    return 1 - pow(1 - x, 4);

}

float easeInOutQuart(in float x)
{
    return x < 0.5 ? 8 * x * x * x * x : 1 - pow(-2 * x + 2, 4) / 2;

}

// Exponential

float easeInExpo(in float x)
{
    return x == 0 ? 0 : pow(2, 10 * x - 10);

}

float easeOutExpo(in float x)
{
    return x == 1 ? 1 : 1 - pow(2, -10 * x);

}

float easeInOutExpo(in float x)
{
    return x == 0
      ? 0
      : x == 1
      ? 1
      : x < 0.5 ? pow(2, 20 * x - 10) / 2
      : (2 - pow(2, -20 * x + 10)) / 2;

}

// Back

float easeInBack(in float x)
{
    const float c1 = 1.70158;
    const float c3 = c1 + 1;

    return c3 * x * x * x - c1 * x * x;

}

float easeOutBack(in float x)
{
    const float c1 = 1.70158;
    const float c3 = c1 + 1;

    return 1 + c3 * pow(x - 1, 3) + c1 * pow(x - 1, 2);

}

float easeInOutBack(in float x)
{
    const float c1 = 1.70158;
    const float c2 = c1 * 1.525;

    return x < 0.5
      ? (pow(2 * x, 2) * ((c2 + 1) * 2 * x - c2)) / 2
      : (pow(2 * x - 2, 2) * ((c2 + 1) * (x * 2 - 2) + c2) + 2) / 2;

}

// Bounce

float easeOutBounce(in float x)
{
    const float n1 = 7.5625;
    const float d1 = 2.75;

    if (x < 1 / d1) {
        return n1 * x * x;
    } else if (x < 2 / d1) {
        return n1 * (x -= 1.5 / d1) * x + 0.75;
    } else if (x < 2.5 / d1) {
        return n1 * (x -= 2.25 / d1) * x + 0.9375;
    } else {
        return n1 * (x -= 2.625 / d1) * x + 0.984375;
    }
}

float easeInBounce(in float x)
{
    return 1 - easeOutBounce(1 - x);
}

float easeInOutBounce(in float x) {
    return x < 0.5
      ? (1 - easeOutBounce(1 - 2 * x)) / 2
      : (1 + easeOutBounce(2 * x - 1)) / 2;
}