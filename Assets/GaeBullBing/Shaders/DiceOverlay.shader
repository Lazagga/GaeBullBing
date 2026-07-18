Shader "GaeBullBing/DiceOverlay"
{
    Properties { [MainColor] _BaseColor("Color", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Overlay+100" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            // The dice are placed closer to the camera than the 2D board.
            // Normal depth testing keeps the cube's own faces from drawing through each other.
            ZWrite On
            ZTest LEqual
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionHCS : SV_POSITION; };
            CBUFFER_START(UnityPerMaterial) half4 _BaseColor; CBUFFER_END
            Varyings vert(Attributes input) { Varyings o; o.positionHCS = TransformObjectToHClip(input.positionOS.xyz); return o; }
            half4 frag(Varyings input) : SV_Target { return _BaseColor; }
            ENDHLSL
        }
    }
}
