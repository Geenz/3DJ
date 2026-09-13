#ifndef SURFEL_ATLAS_DECODE_INCLUDED
#define SURFEL_ATLAS_DECODE_INCLUDED

#include "UnityCG.cginc"

sampler2D _Meta;
sampler2D _MetaSplat;
float _Radius;
float _DepthOffset;
float _Cutoff;

#if defined(_DITHER_BLUENOISE) || defined(_DITHER_BAYER)
sampler2D _BlueNoise;
float4 _BlueNoise_TexelSize;
float _BlueNoiseMix;
float _BlueNoiseJitter;
#endif
float _Falloff;
float _FalloffMult;
float _FadeDistance;
float _FadeMultiplier;
float _LodDistance;
sampler2D _FalloffTex;
float _Shape;
float _Stride;

sampler2D _SurfelDepth;
float4 _SurfelDepth_TexelSize;
sampler2D _SurfelColor;
uniform sampler2D _Udon_3DJ_Data;
float4 _Udon_3DJ_Data_TexelSize;
uniform float _Udon_3DJ_PlaybackActive;

sampler2D _Strip3DJ;

#include "Dither3D/Dither3DInclude.cginc"

#define EMPTY_3DJ 0.007843138
#define COVERAGE_3DJ 0.03921569

#define SURFEL_3DJ_COLS 3
#define SURFEL_3DJ_ROWS 2
float2 Atlas3DJ() { return _SurfelDepth_TexelSize.zw; }
float2 Tile3DJ() { return _SurfelDepth_TexelSize.zw / float2(SURFEL_3DJ_COLS, SURFEL_3DJ_ROWS); }
float2 Uv3DJ(int texelX, int texelY) { return (float2(texelX, texelY) + 0.5) * _SurfelDepth_TexelSize.xy; }

struct appdata {
    float4 vertex : POSITION;
    float2 uv0    : TEXCOORD0;
    float2 uv1    : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f {
    float4 pos    : SV_POSITION;
    half2  corner : TEXCOORD0;
    half3  col    : TEXCOORD1;
    nointerpolation half weight : TEXCOORD2;
    float4 screenPos : TEXCOORD3;
    float4 grabPos : TEXCOORD4;
    half2  uv1    : TEXCOORD5;
    nointerpolation half hull : TEXCOORD6;
    float2 ditherUv : TEXCOORD7;
    UNITY_VERTEX_OUTPUT_STEREO
};

struct Surfel {
    bool   valid;
    float3 pObj;
    half2  corner;
    half2  uv1;
    half3  col;
    half   weight;
    half   hull;
    float2 ditherUv;
};

half3 OctDecode(half2 e) {
    e = e * 2.0 - 1.0;
    half3 n = half3(e, 1.0 - abs(e.x) - abs(e.y));

    if (n.z < 0) {
        half signX = -1;

        if (n.x >= 0) {
            signX = 1;
        }

        half signY = -1;

        if (n.y >= 0) {
            signY = 1;
        }

        n.xy = (1.0 - abs(n.yx)) * half2(signX, signY);
    }

    return normalize(n);
}

half2 OctEncode(half3 n) {
    n /= abs(n.x) + abs(n.y) + abs(n.z);
    half2 e = n.xy;

    if (n.z < 0) {
        half signX = -1;

        if (n.x >= 0) {
            signX = 1;
        }

        half signY = -1;

        if (n.y >= 0) {
            signY = 1;
        }

        e = (1 - abs(n.yx)) * half2(signX, signY);
    }

    return e * 0.5 + 0.5;
}

bool Empty3DJ(float raw) { return raw <= EMPTY_3DJ; }

float Depth3DJAt(int texelX, int texelY) { return tex2Dlod(_SurfelDepth, float4(Uv3DJ(texelX, texelY), 0, 0)).r; }

bool TexelToFace3DJ(int texelX, int texelY, out int k, out int tileU, out int tileV, out int tileW, out int tileH) {
    float2 t = Tile3DJ();
    tileW = (int)t.x;
    tileH = (int)t.y;

    if (tileW < 1 || tileH < 1) {
        k = tileU = tileV = 0;
        return false;
    }

    int tx = texelX / tileW;
    int ty = texelY / tileH;
    k = ty * SURFEL_3DJ_COLS + tx;
    tileU = texelX - tx * tileW;
    tileV = texelY - ty * tileH;
    return tx < SURFEL_3DJ_COLS && ty < SURFEL_3DJ_ROWS;
}

float Lin3DJ(float raw) { return 1.0 - raw; }

float3 Strip3DJ(float2 stripStart, int pixelSize) {
    float3 acc = 0;

    [unroll] for (int bit = 0; bit < 20; bit++) {
        float2 px = float2(stripStart.x + bit * (float)pixelSize, stripStart.y);
        float3 sample = tex2Dlod(_Udon_3DJ_Data, float4((px + 0.5) * _Udon_3DJ_Data_TexelSize.xy, 0, 0)).rgb;
        acc += step(0.5, sample) * exp2((float)bit);
    }

    return acc;
}

void Decode3DJTransform(out float3 djPosition, out float djRotationDeg, out float djScale) {
    int pixelSize = (int)(_Udon_3DJ_Data_TexelSize.z / 20.0);
    float stripX = (_Udon_3DJ_Data_TexelSize.z / 20.0) * 0.5;

    float3 rotationScale = Strip3DJ(float2(stripX, _Udon_3DJ_Data_TexelSize.w / 3.0), pixelSize);
    djRotationDeg = rotationScale.r / 100.0;
    djScale = rotationScale.g / 100.0;

    float3 position = Strip3DJ(float2(stripX, (_Udon_3DJ_Data_TexelSize.w / 3.0) * 2.0), pixelSize);
    djPosition = (position - 524288.0) / 100.0;
}

void Read3DJTransform(out float3 djPosition, out float djRotationDeg, out float djScale) {
    djPosition = tex2Dlod(_Strip3DJ, float4(0.25, 0.5, 0, 0)).xyz;
    float2 rotationScale = tex2Dlod(_Strip3DJ, float4(0.75, 0.5, 0, 0)).xy;
    djRotationDeg = rotationScale.x;
    djScale = rotationScale.y;
}

float Scale3DJ() { return tex2Dlod(_Strip3DJ, float4(0.75, 0.5, 0, 0)).y; }

void CubeFace(int face, float scale, out float3 right, out float3 up, out float3 fwd,
              out float3 camPos, out float w, out float h, out float d0) {
    float3 n;

    if (face == 0) {
        n = float3(0, -1, 0);
        fwd = float3(0, 1, 0);
        up = float3(0, 0, -1);
    } else if (face == 1) {
        n = float3(1, 0, 0);
        fwd = float3(-1, 0, 0);
        up = float3(0, 1, 0);
    } else if (face == 2) {
        n = float3(0, 0, 1);
        fwd = float3(0, 0, -1);
        up = float3(0, 1, 0);
    } else if (face == 3) {
        n = float3(0, 0, -1);
        fwd = float3(0, 0, 1);
        up = float3(0, 1, 0);
    } else if (face == 4) {
        n = float3(-1, 0, 0);
        fwd = float3(1, 0, 0);
        up = float3(0, 1, 0);
    } else {
        n = float3(0, 1, 0);
        fwd = float3(0, -1, 0);
        up = float3(0, 0, -1);
    }

    right = cross(up, fwd);
    camPos = n * (scale * 0.5);
    w = scale;
    h = scale;
    d0 = scale;
}

float3 Pos3DJ(int face, float scale, float u01, float v01, float lin) {
    float3 right, up, fwd, camPos;
    float w, h, d0;
    CubeFace(face, scale, right, up, fwd, camPos, w, h, d0);
    return camPos + right * ((u01 - 0.5) * w) + up * ((v01 - 0.5) * h) + fwd * (lin * d0);
}

int Contradictions3DJ(float3 P, int kSelf, float scale, bool useDepth, float tolerance) {
    float2 t = Tile3DJ();
    int tileW = (int)t.x;
    int tileH = (int)t.y;

    int contradictions = 0;

    [unroll] for (int f = 0; f < 6; f++) {
        if (f == kSelf) {
            continue;
        }

        float3 rightF, upF, fwdF, camPosF;
        float wF, hF, d0F;
        CubeFace(f, scale, rightF, upF, fwdF, camPosF, wF, hF, d0F);

        float3 d = P - camPosF;
        float x = dot(d, rightF);
        float y = dot(d, upF);
        float z = dot(d, fwdF);

        if (abs(x) >= wF * 0.5 || abs(y) >= hF * 0.5 || z < 0 || z > d0F) {
            continue;
        }

        int tx = f % SURFEL_3DJ_COLS;
        int ty = f / SURFEL_3DJ_COLS;
        int texelXF = tx * tileW + (int)((x / wF + 0.5) * tileW);
        int texelYF = ty * tileH + (int)((y / hF + 0.5) * tileH);
        float rawF = Depth3DJAt(texelXF, texelYF);

        if (Empty3DJ(rawF) || (useDepth && Lin3DJ(rawF) * d0F > z + tolerance)) {
            contradictions++;
        }
    }

    return contradictions;
}

float HullFactor3DJ(float3 P, int kSelf, float scale) {
    float2 t = Tile3DJ();
    int tileW = (int)t.x;
    int tileH = (int)t.y;

    float factor = 1.0;
    bool sighted = false;

    [unroll] for (int f = 0; f < 6; f++) {
        if (f == kSelf) {
            continue;
        }

        float3 rightF, upF, fwdF, camPosF;
        float wF, hF, d0F;
        CubeFace(f, scale, rightF, upF, fwdF, camPosF, wF, hF, d0F);

        float3 d = P - camPosF;
        float x = dot(d, rightF);
        float y = dot(d, upF);
        float z = dot(d, fwdF);

        if (abs(x) >= wF * 0.5 || abs(y) >= hF * 0.5 || z < 0 || z > d0F) {
            continue;
        }

        int tx = f % SURFEL_3DJ_COLS;
        int ty = f / SURFEL_3DJ_COLS;
        float sampleU = tx * tileW + (x / wF + 0.5) * tileW - 0.5;
        float sampleV = ty * tileH + (y / hF + 0.5) * tileH - 0.5;

        int xLo = tx * tileW;
        int xHi = xLo + tileW - 1;
        int yLo = ty * tileH;
        int yHi = yLo + tileH - 1;
        int texelX0 = clamp((int)floor(sampleU), xLo, xHi);
        int texelY0 = clamp((int)floor(sampleV), yLo, yHi);
        int texelX1 = clamp((int)floor(sampleU) + 1, xLo, xHi);
        int texelY1 = clamp((int)floor(sampleV) + 1, yLo, yHi);
        float wx = sampleU - floor(sampleU);
        float wy = sampleV - floor(sampleV);

        float occ00 = 1.0;

        if (Empty3DJ(Depth3DJAt(texelX0, texelY0))) {
            occ00 = 0.0;
        }

        float occ10 = 1.0;

        if (Empty3DJ(Depth3DJAt(texelX1, texelY0))) {
            occ10 = 0.0;
        }

        float occ01 = 1.0;

        if (Empty3DJ(Depth3DJAt(texelX0, texelY1))) {
            occ01 = 0.0;
        }

        float occ11 = 1.0;

        if (Empty3DJ(Depth3DJAt(texelX1, texelY1))) {
            occ11 = 0.0;
        }

        float occ_f = lerp(lerp(occ00, occ10, wx), lerp(occ01, occ11, wx), wy);

        if (sighted) {
            factor = min(factor, occ_f);
        } else {
            factor = occ_f;
        }

        sighted = true;
    }

    float result = 1.0;

    if (sighted) {
        result = factor;
    }

    return result;
}

float3 RotY3DJ(float3 p, float deg) {
    float s, c;
    sincos(radians(deg), s, c);
    return float3(c * p.x + s * p.z, p.y, c * p.z - s * p.x);
}

half Coverage(half2 corner, half2 uv1, half fall) {
    #ifdef _FALLOFFTEX_ON
    return (half)tex2D(_FalloffTex, (float2)uv1).r;
    #endif
    half m = max(abs(corner.x), abs(corner.y));
    half d2 = dot(corner, corner);

    if (_Shape > 0.5) {
        d2 = m * m;
    }

    half result = 0.0;

    if (d2 <= 1.0) {
        result = exp(-fall * d2);
    }

    return result;
}

half DitherThreshold3DJ(float4 screenPos) {
    float2 px = floor(screenPos.xy / screenPos.w * _ScreenParams.xy);
    int2 p = (int2)px & 3;
    static const half bayer[16] = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };
    half bayerT = (half)((bayer[p.y * 4 + p.x] + 0.5) / 16.0);

    #ifdef _DITHER_BLUENOISE
    float2 nuv = px;

    if (_BlueNoiseJitter > 0.5) {
        nuv += floor(frac(floor(_Time.y * 60.0) * float2(0.7548776662, 0.5698402910)) * _BlueNoise_TexelSize.zw);
    }

    half blue = (half)tex2Dlod(_BlueNoise, float4((nuv + 0.5) * _BlueNoise_TexelSize.xy, 0, 0)).r;
    return (half)(lerp(bayerT, blue, _BlueNoiseMix) * (1.0 + _BlueNoiseMix));
    #else
    return bayerT;
    #endif
}

half DitherClip3DJ(v2f i, half alpha) {
    #ifdef _DITHER_FRACTAL
    return GetDither3D(i.ditherUv, i.screenPos, alpha) - 0.5;
    #else
    return alpha - DitherThreshold3DJ(i.screenPos);
    #endif
}

Surfel SurfelEmpty() {
    Surfel s;
    s.valid = false;
    s.pObj = 0;
    s.corner = 0;
    s.uv1 = 0;
    s.col = 0;
    s.weight = 0;
    s.hull = 0;
    s.ditherUv = 0;
    return s;
}

Surfel Reconstruct3DJ(appdata v, float push) {
    Surfel s = SurfelEmpty();

    half2 corner = (half2)v.vertex.xy;
    int texelX = (int)(v.uv0.x + 0.5);
    int texelY = (int)(v.uv0.y + 0.5);

    int k, tileU, tileV, tileW, tileH;

    if (!TexelToFace3DJ(texelX, texelY, k, tileU, tileV, tileW, tileH)) {
        return s;
    }

    float2 uv = Uv3DJ(texelX, texelY);
    float raw = tex2Dlod(_SurfelDepth, float4(uv, 0, 0)).r;

    if (Empty3DJ(raw)) {
        return s;
    }
    #ifndef _IGNOREGLOBALPLAYBACKCONTROL_ON
    if (_Udon_3DJ_PlaybackActive != 1.0) {
        return s;
    }
    #endif

    half4 colTex = (half4)tex2Dlod(_SurfelColor, float4(uv, 0, 0));
    half colorAlpha = colTex.a;
    half coverage = (half)0;

    if (raw > COVERAGE_3DJ) {
        coverage = (half)1;
    }

    if (colorAlpha > (half)COVERAGE_3DJ) {
        coverage = colorAlpha;
    }

    if (coverage <= 0) {
        return s;
    }

    half4 splat = (half4)tex2Dlod(_MetaSplat, float4(uv, 0, 0));

    if (splat.a <= 0) {
        return s;
    }

    float3 djPos;
    float djRot, djScale;
    Read3DJTransform(djPos, djRot, djScale);
    #ifdef _LOCKPOSITION_ON
    djPos = 0;
    #endif
    #ifdef _LOCKROTATION_ON
    djRot = 0;
    #endif

    float3 right, up, fwd, camPos;
    float w, h, d0;
    CubeFace(k, djScale, right, up, fwd, camPos, w, h, d0);
    float su = w / tileW;
    float sv = h / tileH;
    float3 pLocal = Pos3DJ(k, djScale, (tileU + 0.5) / tileW, (tileV + 0.5) / tileH, Lin3DJ(raw));

    float3 pCentre = RotY3DJ(pLocal, djRot) + djPos;
    float3 pWorld = mul(unity_ObjectToWorld, float4(pCentre, 1)).xyz;
    float lodDist = distance(pWorld, _WorldSpaceCameraPos);
    int lod = 0;

    if (lodDist > _LodDistance) {
        lod = (int)floor(log2(lodDist / _LodDistance)) + 1;
    }

    int lodStep = 1 << lod;
    int stride = (int)_Stride;

    if (((texelX / stride) % lodStep) != 0 || ((texelY / stride) % lodStep) != 0) {
        return s;
    }

    half4 meta = (half4)tex2Dlod(_Meta, float4(uv, 0, 0));
    half3 fwdH = (half3)fwd;
    half fx = (half)(_Radius * meta.b * _Stride * lodStep);
    half fy = (half)(_Radius * meta.a * _Stride * lodStep);
    half3 axisU = normalize((half3)right - fwdH * (half)splat.r) * fx;
    half3 axisV = normalize((half3)up - fwdH * (half)splat.g) * fy;

    float2 ditherUv = Uv3DJ(texelX, texelY) + corner * float2(fx / su, fy / sv) * _SurfelDepth_TexelSize.xy;

    float3 pCube = pLocal + (float3)(corner.x * axisU + corner.y * axisV);

    s.valid = true;
    s.pObj = RotY3DJ(pCube, djRot) + djPos;
    float eyeDepth = -UnityObjectToViewPos(float4(s.pObj, 1)).z;
    half t = (half)saturate((eyeDepth - _ProjectionParams.y) / _FadeDistance);
    half fade = (half)saturate(_FadeMultiplier * t + 1.0 - _FadeMultiplier);

    if (push != 0) {
        float3 camObj = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1)).xyz;
        s.pObj += normalize(s.pObj - camObj) * (max(fx, fy) * push);
    }

    s.corner = corner;
    s.uv1 = (half2)v.uv1;
    s.col = colTex.rgb;
    s.weight = splat.a * coverage * fade;
    s.hull = (half)splat.b;
    s.ditherUv = ditherUv;
    return s;
}

void ApplyCollapse(inout v2f o) {
    o.pos = float4(2, 2, 2, 1);
    o.corner = 0;
    o.col = 0;
    o.weight = 0;
    o.screenPos = 0;
    o.grabPos = 0;
    o.uv1 = 0;
    o.hull = 0;
    o.ditherUv = 0;
}

v2f VertCommon(appdata v, float push) {
    v2f o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    Surfel s = Reconstruct3DJ(v, push);

    if (!s.valid) {
        ApplyCollapse(o);
        return o;
    }

    o.pos = UnityObjectToClipPos(float4(s.pObj, 1));
    o.screenPos = ComputeScreenPos(o.pos);
    o.grabPos = ComputeGrabScreenPos(o.pos);
    o.corner = s.corner;
    o.col = s.col;
    o.weight = s.weight;
    o.uv1 = s.uv1;
    o.hull = s.hull;
    o.ditherUv = s.ditherUv;
    return o;
}

half4 fragDepth(v2f i) : SV_Target {
    half cov = Coverage(i.corner, i.uv1, (half)_Falloff);
    half alpha = (half)saturate(cov * i.weight * _FalloffMult);
    clip(cov * i.hull - 1e-4);

    if (i.hull >= 1.0) {
        clip(DitherClip3DJ(i, alpha));
    } else {
        clip(alpha - _Cutoff);
    }

    return half4(i.col, 1);
}

#endif
