Shader "Bloombots/Arena/Effect Color"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Weave ("Rope weave", Range(0,1)) = 0
        _Cable ("Seed cable", Range(0,1)) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float _Weave;
                float _Cable;
            CBUFFER_END

            struct Vertex
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Vertex input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
                float width = abs(input.uv.y - .5) * 2;
                color.a *= 1 - smoothstep(.66, 1, width);
                half twist = .5 + .5 * sin(input.uv.x * 68 - input.uv.y * 9 - _Time.y * 16);
                color.rgb *= lerp(1, .74 + twist * .55 + (1 - width) * .24, _Weave);
                half pulse = pow(saturate(.5 + .5 * sin(input.uv.x * 21 - _Time.y * 5)), 5);
                half3 cable = lerp(color.rgb * (.72 + pulse * .50), half3(.12, .075, .036),
                    smoothstep(.46, .62, width));
                color.rgb = lerp(color.rgb, cable, _Cable);
                return color;
            }
            ENDHLSL
        }
    }
}
