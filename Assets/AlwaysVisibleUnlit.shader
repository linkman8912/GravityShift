Shader "Custom/AlwaysVisibleUnlit" {
    Properties {
        _Color ("Color", Color) = (1,1,0,1)
    }
    SubShader {
        Tags { "Queue"="Overlay" "RenderType"="Opaque" }
        Pass {
            // Disable depth test and writing so this always draws on top.
            ZTest Always
            ZWrite Off
            Cull Off
            Lighting Off
            Fog { Mode Off }
            
            // Use a simple vertex/color pass.
            BindChannels {
                Bind "vertex", vertex
                Bind "color", color
            }
            // Set a fixed color.
            Color [_Color]
        }
    }
}
