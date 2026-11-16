Shader "Bloombots/Arena/Perspective Plots"
{
    Properties
    {
        _Soil ("Soil", 2D) = "white" {}
        _Night ("Night amount", Range(0,1)) = 0
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

            TEXTURE2D(_Soil);
            SAMPLER(sampler_Soil);
            CBUFFER_START(UnityPerMaterial)
                float _Night;
            CBUFFER_END

            struct Vertex
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float2 marks : TEXCOORD1;
                half4 roots : TEXCOORD2;
                half4 state : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 state : COLOR;
                float2 soilUV : TEXCOORD1;
                float mark : TEXCOORD2;
                half4 roots : TEXCOORD3;
            };

            Varyings Vert(Vertex input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.state = input.state;
                output.soilUV = input.positionOS.xy * .21 + .5;
                output.mark = input.marks.x;
                output.roots = input.roots;
                return output;
            }

            half Stroke(float2 p, float2 a, float2 b, float width)
            {
                float2 axis = b - a;
                float distanceToLine = length(p - a - axis * saturate(dot(p - a, axis) / dot(axis, axis)));
                return 1 - smoothstep(width, width + .013, distanceToLine);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 corner = abs(input.uv - .5) - float2(.415, .394);
                float edge = length(max(corner, 0)) + min(max(corner.x, corner.y), 0) - .055;
                float aa = max(fwidth(edge), .002);
                half inside = 1 - smoothstep(-aa, aa, edge);
                half shadow = 1 - smoothstep(.002, .040 + aa, edge);
                half rim = smoothstep(-.052 - aa, -.052 + aa, edge) * inside;
                half bevel = lerp(.36, 1, smoothstep(.25, .85, input.uv.y));
                half3 soil = SAMPLE_TEXTURE2D(_Soil, sampler_Soil, frac(input.soilUV)).rgb;
                half state = input.state.a;
                half focus = smoothstep(.78, .92, state);
                half3 baseSoil = half3(.69, .41, .16) * (.55 + soil * .50);
                baseSoil *= lerp(.85, 1.13, input.uv.y);
                half3 tint = lerp(baseSoil, input.state.rgb * (.64 + soil * .27), state * .90);
                half3 lip = lerp(half3(.83, .49, .21), input.state.rgb * 1.25, state);
                tint = lerp(tint, lip, rim * bevel * (.72 + focus * .28));
                tint = lerp(tint, tint * half3(.72, .84, 1.16), _Night * .58 * (1 - focus * .55));
                tint = lerp(half3(.27, .15, .075), tint, inside);
                half alpha = shadow * .22 + inside * (.23 + state * .26) + rim * (.19 + focus * .12);
                float2 p = input.uv - .5;
                half horizontal = (1 - smoothstep(.017, .038, abs(p.y + sin(p.x * 16) * .017)))
                    * (p.x < 0 ? input.roots.x : input.roots.y);
                half vertical = (1 - smoothstep(.017, .038, abs(p.x + sin(p.y * 16) * .017)))
                    * (p.y > 0 ? input.roots.z : input.roots.w);
                half root = max(horizontal, vertical) * .83;
                tint = lerp(tint, lerp(half3(.82, .91, .31), half3(.47, .89, .53), _Night), root);
                alpha = max(alpha, root);

                half mark = 0;
                if (input.mark == 1 || input.mark == 5)
                {
                    float2 arrow = p + float2(0, .15);
                    float width = input.mark == 5 ? .027 : .018;
                    mark = Stroke(arrow, float2(-.15, .08), float2(0, -.04), width)
                        + Stroke(arrow, float2(0, -.04), float2(.15, .08), width);
                    mark *= input.mark == 5 ? .92 : .53;
                }
                else if (input.mark == 2 || input.mark == 3)
                {
                    float2 badge = p - float2(.24, -.21);
                    if (input.mark == 2)
                        mark = Stroke(badge, float2(-.085, 0), float2(-.026, -.049), .016)
                            + Stroke(badge, float2(-.026, -.049), float2(.083, .068), .016);
                    else
                        mark = Stroke(badge, float2(-.06, -.06), float2(.06, .06), .016)
                            + Stroke(badge, float2(-.06, .06), float2(.06, -.06), .016);
                    mark *= .76;
                }
                else if (input.mark == 4)
                {
                    float2 badge = abs(p);
                    mark = Stroke(badge, float2(.34, .17), float2(.34, .28), .018)
                        + Stroke(badge, float2(.34, .28), float2(.23, .28), .018);
                }
                tint = lerp(tint, half3(1, .95, .76), saturate(mark));
                alpha = max(alpha, saturate(mark));
                return half4(tint, saturate(alpha));
            }
            ENDHLSL
        }
    }
}
