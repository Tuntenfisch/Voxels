Shader "Voxels/Voxel"
{
    Properties
    {
        _MaterialBlendOffset ("Material Blend Offset", Range(0, 0.33)) = 0.25
        _MaterialBlendExponent ("Material Blend Exponent", Range(0.0, 8.0)) = 2.0

        [Space(20)]
        _TriplanarCoordinateScaling ("Triplanar Coordinate Scaling", Float) = 1.0
        _TriplanarBlendOffset ("Triplanar Blend Offset", Range(0, 0.5)) = 0.25
        _TriplanarBlendExponent ("Triplanar Blend Exponent", Range(0.0, 8.0)) = 2.0
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }

        HLSLINCLUDE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
        float _MaterialBlendOffset;
        float _MaterialBlendExponent;
        float _TriplanarCoordinateScaling;
        float _TriplanarBlendOffset;
        float _TriplanarBlendExponent;
        CBUFFER_END

        ENDHLSL

        Pass
        {

            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM

            #pragma vertex LitPassVertex
            #pragma geometry LitPassGeometry
            #pragma fragment LitPassFragment

            // Material Keywords
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF

            // URP Keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK

            // Unity Keywords
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct VertexPassInput
            {
                float4 positionOS : POSITION;
                float4 normalOS : NORMAL;
                uint materialIndex : TEXCOORD1;
                float2 lightmapUV : TEXCOORD2;
            };

            struct GeometryPassInput
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                uint materialIndex : TEXCOORD3;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 4);

                #if defined(_ADDITIONAL_LIGHTS_VERTEX)
                    half4 fogFactorAndVertexLight : TEXCOORD5;
                #else
                    half fogFactor : TEXCOORD5;
                #endif

                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    float4 shadowCoord : TEXCOORD6;
                #endif
            };

            struct FragmentPassInput
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                uint3 materialIndices : TEXCOORD3;
                float3 materialWeights : TEXCOORD4;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 5);

                #if defined(_ADDITIONAL_LIGHTS_VERTEX)
                    half4 fogFactorAndVertexLight : TEXCOORD6;
                #else
                    half fogFactor : TEXCOORD6;
                #endif

                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    float4 shadowCoord : TEXCOORD7;
                #endif
            };

            float cosOfHalfSharpFeatureAngle;
            TEXTURE2D_ARRAY(materialAlbedoTextureArray);
            TEXTURE2D_ARRAY(materialNormalTextureArray);
            TEXTURE2D_ARRAY(materialMOHSTextureArray);
            SAMPLER(sampler_linear_repeat);

            #include "Assets/Shaders/Voxels/Include/Triplanar.hlsl"

            GeometryPassInput LitPassVertex(VertexPassInput input)
            {
                GeometryPassInput output;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS.xyz);

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;

                half3 vertexLight = VertexLighting(positionInputs.positionWS, normalInputs.normalWS);
                half fogFactor = ComputeFogFactor(positionInputs.positionCS.z);

                output.normalWS = NormalizeNormalPerVertex(normalInputs.normalWS);
                output.materialIndex = input.materialIndex;

                OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);
                OUTPUT_SH(output.normalWS.xyz, output.vertexSH);

                #if defined(_ADDITIONAL_LIGHTS_VERTEX)
                    output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
                #else
                    output.fogFactor = fogFactor;
                #endif

                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    output.shadowCoord = GetShadowCoord(positionInputs);
                #endif

                return output;
            }

            [maxvertexcount(3)]
            void LitPassGeometry(triangle GeometryPassInput inputs[3], inout TriangleStream<FragmentPassInput> outputStream)
            {
                const float3x3 float3x3Identity = float3x3
                (
                    1.0f, 0.0f, 0.0f,
                    0.0f, 1.0f, 0.0f,
                    0.0f, 0.0f, 1.0f
                );

                uint3 materialIndices = float3(inputs[0].materialIndex, inputs[1].materialIndex, inputs[2].materialIndex);
                float3 faceNormalWS = normalize(cross(inputs[1].positionWS - inputs[0].positionWS, inputs[2].positionWS - inputs[0].positionWS));

                for (uint index = 0; index < 3; index++)
                {
                    GeometryPassInput input = inputs[index];
                    input.normalWS = dot(input.normalWS, faceNormalWS) <= cosOfHalfSharpFeatureAngle ? faceNormalWS : input.normalWS;

                    FragmentPassInput output;
                    output.positionCS = input.positionCS;
                    output.positionWS = input.positionWS;
                    output.normalWS = input.normalWS;
                    output.materialIndices = materialIndices;
                    output.materialWeights = float3x3Identity[index];

                    #if defined(LIGHTMAP_ON)
                        output.lightmapUV = input.lightmapUV;
                    #else
                        output.vertexSH = input.vertexSH;
                    #endif

                    #if defined(_ADDITIONAL_LIGHTS_VERTEX)
                        output.fogFactorAndVertexLight = input.fogFactorAndVertexLight;
                    #else
                        output.fogFactor = input.fogFactor;
                    #endif

                    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                        output.shadowCoord = input.shadowCoord;
                    #endif

                    outputStream.Append(output);
                }
                outputStream.RestartStrip();
            }

            SurfaceData CreateSurfaceData(FragmentPassInput input)
            {
                SurfaceData surfaceData = (SurfaceData)0;

                half4 albdeo = 0.0h;

                float3 materialWeights = input.materialWeights;
                materialWeights = saturate(materialWeights - _MaterialBlendOffset);
                materialWeights = pow(materialWeights, _MaterialBlendExponent);
                materialWeights /= dot(materialWeights, 1.0f);
                
                for (uint index = 0; index < 3; index++)
                {
                    half4 materialColor = TriplanarSampleTexture2DArray(input.positionWS, input.normalWS, materialAlbedoTextureArray, sampler_linear_repeat, input.materialIndices[index]);
                    albdeo += materialWeights[index] * materialColor;
                }

                surfaceData.albedo = albdeo.rgb;
                surfaceData.occlusion = 1.0h;

                return surfaceData;
            }

            InputData CreateInputData(FragmentPassInput input, half3 normalTS)
            {
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;

                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(inputData.positionWS);
                inputData.normalWS = input.normalWS;

                inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
                viewDirWS = SafeNormalize(viewDirWS);

                inputData.viewDirectionWS = viewDirWS;

                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    inputData.shadowCoord = input.shadowCoord;
                #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
                #else
                    inputData.shadowCoord = float4(0.0f, 0.0f, 0.0f, 0.0f);
                #endif

                #if defined(_ADDITIONAL_LIGHTS_VERTEX)
                    inputData.fogCoord = input.fogFactorAndVertexLight.x;
                    inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
                #else
                    inputData.fogCoord = input.fogFactor;
                    inputData.vertexLighting = half3(0.0h, 0.0h, 0.0h);
                #endif

                inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, inputData.normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUV);

                return inputData;
            }

            half4 LitPassFragment(FragmentPassInput input) : SV_Target
            {
                SurfaceData surfaceData = CreateSurfaceData(input);
                InputData inputData = CreateInputData(input, surfaceData.normalTS);

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);

                return color;
            }

            ENDHLSL

        }

        Pass
        {

            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            HLSLPROGRAM

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Assets/Shaders/Voxels/Include/ShadowCasterPass.hlsl"

            ENDHLSL

        }

        Pass
        {

            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ColorMask 0

            HLSLPROGRAM

            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #include "Assets/Shaders/Voxels/Include/DepthOnlyPass.hlsl"

            ENDHLSL

        }

        Pass
        {

            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            ZTest LEqual

            HLSLPROGRAM

            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            #include "Assets/Shaders/Voxels/Include/DepthNormalsPass.hlsl"
            
            ENDHLSL

        }
    }
}