Shader "HydrostaticSilence/VoidMedium"
{
    Properties
    {
        [Header(Base)]
        _BaseColor ("Base Color", Color) = (0.005, 0.005, 0.012, 1)
        _Intensity ("Overall Intensity", Range(0, 1)) = 0.3
        
        [Header(Deep Layer - Slow Movement)]
        _DeepScale ("Deep Noise Scale", Range(0.5, 5)) = 1.2
        _DeepSpeed ("Deep Noise Speed", Range(0, 0.1)) = 0.008
        _DeepStrength ("Deep Layer Strength", Range(0, 0.03)) = 0.008
        
        [Header(Surface Layer - Faster Texture)]
        _SurfaceScale ("Surface Noise Scale", Range(1, 20)) = 6.0
        _SurfaceSpeed ("Surface Noise Speed", Range(0, 0.3)) = 0.04
        _SurfaceStrength ("Surface Layer Strength", Range(0, 0.02)) = 0.004
        
        [Header(Pressure Pulses)]
        _PulseFrequency ("Pulse Frequency", Range(0, 2)) = 0.15
        _PulseStrength ("Pulse Strength", Range(0, 0.05)) = 0.01
        _PulseWidth ("Pulse Width", Range(0.01, 0.5)) = 0.15
        
        [Header(Wake Entity)]
        _WakeActive ("Wake Visible", Range(0, 1)) = 0
        _WakePosition ("Wake Position", Vector) = (0.5, 0.8, 0, 0)
        _WakeSize ("Wake Size", Range(0.05, 0.5)) = 0.15
        _WakeSpeed ("Wake Travel Speed", Range(0, 0.2)) = 0.03
        
        [Header(Horror Escalation)]
        _Unease ("Unease Level", Range(0, 1)) = 0
        _Appetite ("Appetite", Range(0, 1)) = 0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }
        
        Pass
        {
            Name "VoidMedium"
            Tags { "LightMode" = "UniversalForward" }
            
            Cull Back
            ZWrite On
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Intensity;
                
                float _DeepScale;
                float _DeepSpeed;
                float _DeepStrength;
                
                float _SurfaceScale;
                float _SurfaceSpeed;
                float _SurfaceStrength;
                
                float _PulseFrequency;
                float _PulseStrength;
                float _PulseWidth;
                
                float _WakeActive;
                float4 _WakePosition;
                float _WakeSize;
                float _WakeSpeed;
                
                float _Unease;
                float _Appetite;
            CBUFFER_END
            
            // Hash-based noise — no texture needed
            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }
            
            // Smooth value noise
            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f); // smoothstep
                
                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            
            // Fractal noise — multiple octaves for organic feel
            float fbm(float2 p, int octaves)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                
                for (int i = 0; i < octaves; i++)
                {
                    value += amplitude * valueNoise(p * frequency);
                    frequency *= 2.0;
                    amplitude *= 0.5;
                }
                
                return value;
            }
            
            // Pressure pulse — radial wave expanding from center
            float pressurePulse(float2 uv, float time)
            {
                float dist = length(uv - 0.5);
                float wave = frac(time * _PulseFrequency);
                float pulse = 1.0 - saturate(abs(dist - wave) / _PulseWidth);
                pulse = pulse * pulse; // sharpen falloff
                // Fade out as wave expands
                pulse *= 1.0 - wave;
                return pulse * _PulseStrength;
            }
            
            // Wake entity — a disturbance that moves with purpose
            float wakeEntity(float2 uv, float time)
            {
                // Wake travels across the viewport
                float2 wakeCenter = _WakePosition.xy;
                wakeCenter.x += sin(time * _WakeSpeed * 3.14) * 0.3;
                wakeCenter.y -= time * _WakeSpeed;
                wakeCenter = frac(wakeCenter); // wrap around
                
                float dist = length(uv - wakeCenter);
                
                // Core displacement
                float core = exp(-dist * dist / (_WakeSize * _WakeSize * 0.5));
                
                // Trailing wake — extends behind the entity
                float2 trailDir = normalize(uv - wakeCenter + 0.001);
                float trail = exp(-dist * dist / (_WakeSize * _WakeSize * 2.0));
                trail *= saturate(dot(trailDir, float2(0, 1))); // trail goes upward (behind direction of travel)
                
                return (core * 0.7 + trail * 0.3) * _WakeActive;
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float time = _Time.y;
                
                // === DEEP LAYER ===
                // Slow, large-scale movement. The medium breathing.
                float2 deepUV = uv * _DeepScale;
                deepUV += float2(time * _DeepSpeed, time * _DeepSpeed * 0.7);
                float deepNoise = fbm(deepUV, 4);
                
                // Unease makes the deep layer more active
                float deepContrib = deepNoise * _DeepStrength * (1.0 + _Unease * 3.0);
                
                // === SURFACE LAYER ===
                // Faster, finer texture. The medium's skin.
                float2 surfUV = uv * _SurfaceScale;
                surfUV += float2(time * _SurfaceSpeed * 0.6, time * _SurfaceSpeed);
                float surfNoise = fbm(surfUV, 3);
                
                float surfContrib = surfNoise * _SurfaceStrength * (1.0 + _Unease * 2.0);
                
                // === PRESSURE PULSE ===
                float pulse = pressurePulse(uv, time);
                // More frequent pulses as appetite increases
                float pulse2 = pressurePulse(uv, time * 1.7 + 3.14);
                pulse = lerp(pulse, pulse + pulse2, _Appetite);
                
                // === WAKE ENTITY ===
                float wake = wakeEntity(uv, time);
                // Wake adds a brighter disturbance — something moving through
                float wakeContrib = wake * 0.02 * _WakeActive;
                
                // === COMBINE ===
                float totalBrightness = deepContrib + surfContrib + pulse + wakeContrib;
                
                // Appetite adds a barely perceptible warm shift
                // The void gets the tiniest hint of color when it's hungry
                float3 hungerTint = float3(0.015, 0.005, 0.0) * _Appetite;
                
                float3 color = _BaseColor.rgb + totalBrightness + hungerTint;
                
                // Clamp to very dark range — nothing here should ever be bright
                color = clamp(color, 0.0, 0.04);
                
                // Final intensity control
                color *= _Intensity;
                
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
