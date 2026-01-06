Shader "Custom/VertexColorUnlitURP"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)

        [Toggle] _VertexJitter("PS1 Vertex Jitter", Float) = 1
        _JitterResolution("Jitter Resolution (XY)", Vector) = (1920,1080,0,0)
        _JitterPixelScale("Jitter Pixel Scale", Float) = 3

        [Toggle] _AffineMapping("Affine Texture Mapping", Float) = 0
        _AffineBlend("Affine Blend", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Name "Unlit"
            Tags { "LightMode"="SRPDefaultUnlit" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // `noperspective` helps approximate PS1-style affine texturing by disabling
            // perspective-correct interpolation for a varying.
            #if defined(SHADER_API_GLES) || defined(SHADER_API_GLES3)
                #define AFFINE_QUALIFIER
            #else
                #define AFFINE_QUALIFIER noperspective
            #endif

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _Color;

                float _VertexJitter;
                float4 _JitterResolution;
                float _JitterPixelScale;

                float _AffineMapping;
                float _AffineBlend;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                AFFINE_QUALIFIER float2 uvAffine : TEXCOORD1;
                half4 color : COLOR;

                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes v)
            {
                Varyings o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float4 positionHCS = TransformObjectToHClip(v.positionOS.xyz);

                // PS1-style "vertex snapping" approximation: quantize screen-space (NDC) XY.
                // This produces classic jitter as the camera moves.
                if (_VertexJitter > 0.5)
                {
                    float2 res = max(_JitterResolution.xy, 1.0.xx);
                    float pixelScale = max(_JitterPixelScale, 1.0);

                    // NDC ranges [-1,1]. A full screen width is 2.0 units.
                    // factor = pixels-per-half-screen (with optional coarser stepping via pixelScale)
                    float2 factor = max(res / (2.0 * pixelScale), 1.0.xx);
                    float2 ndc = positionHCS.xy / positionHCS.w;

                    ndc = floor(ndc * factor + 0.5) / factor;
                    positionHCS.xy = ndc * positionHCS.w;
                }

                o.positionHCS = positionHCS;
                float2 uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.uv = uv;
                o.uvAffine = uv;
                o.color = v.color * _Color; // vertex color (optional tint)
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float affineOn = step(0.5, _AffineMapping);
                float affineBlend = saturate(_AffineBlend) * affineOn;
                float2 uv = lerp(i.uv, i.uvAffine, affineBlend);

                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
                return tex * i.color;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}