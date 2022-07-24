sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
float3 uColor;
float3 uSecondaryColor;
float uOpacity;
float uSaturation;
float uRotation;
float uTime;
float4 uSourceRect;
float2 uWorldPosition;
float uDirection;
float3 uLightSource;
float2 uImageSize0;
float2 uImageSize1;
matrix uWorldViewProjection;
float4 uShaderSpecificData;

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;
    float4 pos = mul(input.Position, uWorldViewProjection);
    output.Position = pos;
    
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;

    return output;
}

float4 StarColorFunction(float2 coords)
{
    float4 c1 = tex2D(uImage1, coords + float2(sin(uTime * 0.12) * 0.5, uTime * 0.03));
    float4 c2 = tex2D(uImage1, coords + float2(uTime * -0.019, sin(uTime * -0.09 + 0.754) * 0.6));
    return pow(c1 + c2, 1.6);
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 coords = input.TextureCoordinates;
    float3 from = float3(uTime * 0.01 + 1, uTime * 0.03 + 0.5, 0.5);
    float4 color = StarColorFunction(coords * float2(1, 0.1)) * input.Color;
    
    float bloomPulse = sin(uTime * 7.1 - coords.x * 12.55) * 0.5 + 0.5;
    float opacity = pow(sin(3.141 * coords.y), 4 - bloomPulse * 2);
    
    return color * opacity * 1.6;
}

technique Technique1
{
    pass TrailPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
