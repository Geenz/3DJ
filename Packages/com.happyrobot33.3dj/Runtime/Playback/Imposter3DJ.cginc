#ifndef IMPOSTER_3DJ_INCLUDED
#define IMPOSTER_3DJ_INCLUDED

#include "UnityCG.cginc"

uniform sampler2D _Udon_3DJ_Depth;
float4 _Udon_3DJ_Depth_TexelSize;
uniform sampler2D _Udon_3DJ_Color;
uniform sampler2D _Udon_3DJ_Data;
float4 _Udon_3DJ_Data_TexelSize;
uniform float _Udon_3DJ_PlaybackActive;

#define IMPOSTER_EMPTY_3DJ 0.007843138
#define IMPOSTER_COLS_3DJ 3
#define IMPOSTER_ROWS_3DJ 2

static const float3 faceFwd3DJ[6] = {
    float3(0, 1, 0),
    float3(-1, 0, 0),
    float3(0, 0, -1),
    float3(0, 0, 1),
    float3(1, 0, 0),
    float3(0, -1, 0)
};

static const float3 faceUp3DJ[6] = {
    float3(0, 0, -1),
    float3(0, 1, 0),
    float3(0, 1, 0),
    float3(0, 1, 0),
    float3(0, 1, 0),
    float3(0, 0, -1)
};

float3 ReadStrip3DJ(float rowY) {
    int pixelSize = (int)(_Udon_3DJ_Data_TexelSize.z / 20.0);
    float3 total = 0;

    [unroll] for (int bit = 0; bit < 20; bit++) {
        float2 pixel = float2((float)pixelSize * 0.5 + (float)(bit * pixelSize), rowY);
        float3 texelValue = tex2Dlod(_Udon_3DJ_Data, float4((pixel + 0.5) * _Udon_3DJ_Data_TexelSize.xy, 0, 0)).rgb;
        total += step(0.5, texelValue) * exp2((float)bit);
    }

    return total;
}

void DecodeTransform3DJ(out float3 position, out float rotationDeg, out float scale) {
    float firstRowY = _Udon_3DJ_Data_TexelSize.w / 3.0;
    float3 rotationScale = ReadStrip3DJ(firstRowY);
    float3 encodedPosition = ReadStrip3DJ(firstRowY * 2.0);
    rotationDeg = rotationScale.r / 100.0;
    scale = rotationScale.g / 100.0;
    position = (encodedPosition - 524288.0) / 100.0;
}

float3 RotateY3DJ(float3 position, float deg) {
    float sine;
    float cosine;
    sincos(radians(deg), sine, cosine);
    return float3(cosine * position.x + sine * position.z, position.y, cosine * position.z - sine * position.x);
}

void Face3DJ(int face, out float3 right, out float3 up, out float3 fwd) {
    fwd = faceFwd3DJ[face];
    up = faceUp3DJ[face];
    right = cross(up, fwd);
}

float2 FaceUv3DJ(int face, float3 cubeLocal, float scale, out float faceDepth) {
    float3 right;
    float3 up;
    float3 fwd;
    Face3DJ(face, right, up, fwd);

    float3 fromCamera = cubeLocal + fwd * (scale * 0.5);
    faceDepth = dot(fromCamera, fwd);

    float2 faceUv = float2(dot(fromCamera, right), dot(fromCamera, up)) / scale + 0.5;
    int2 tileSize = int2(_Udon_3DJ_Depth_TexelSize.zw / float2(IMPOSTER_COLS_3DJ, IMPOSTER_ROWS_3DJ));
    int2 tileOrigin = int2(face % IMPOSTER_COLS_3DJ, face / IMPOSTER_COLS_3DJ) * tileSize;
    int2 tileTexel = clamp(int2(faceUv * float2(tileSize)), int2(0, 0), tileSize - 1);
    return (float2(tileOrigin + tileTexel) + 0.5) * _Udon_3DJ_Depth_TexelSize.xy;
}

bool Solid3DJ(float3 cubeLocal, float scale, float tolerance, int carveViews) {
    int seeingPast = 0;

    [unroll] for (int faceIndex = 0; faceIndex < 6; faceIndex++) {
        float faceDepth;
        float2 atlasUv = FaceUv3DJ(faceIndex, cubeLocal, scale, faceDepth);
        float raw = tex2Dlod(_Udon_3DJ_Depth, float4(atlasUv, 0, 0)).r;
        bool seesPast = raw <= IMPOSTER_EMPTY_3DJ;

        if ((1.0 - raw) * scale > faceDepth + tolerance) {
            seesPast = true;
        }

        if (seesPast) {
            seeingPast++;
        }
    }

    return seeingPast < carveViews;
}

float3 Shade3DJ(float3 cubeLocal, float3 rayDir, float scale, float tolerance, float blendPower) {
    float3 colorSum = 0;
    float weightSum = 0;
    float3 closestColor = 0;
    float closestGap = 1e30;

    [unroll] for (int faceIndex = 0; faceIndex < 6; faceIndex++) {
        float faceDepth;
        float2 atlasUv = FaceUv3DJ(faceIndex, cubeLocal, scale, faceDepth);
        float raw = tex2Dlod(_Udon_3DJ_Depth, float4(atlasUv, 0, 0)).r;

        if (raw <= IMPOSTER_EMPTY_3DJ) {
            continue;
        }

        float3 faceColor = tex2Dlod(_Udon_3DJ_Color, float4(atlasUv, 0, 0)).rgb;
        float gap = faceDepth - (1.0 - raw) * scale;

        if (gap < closestGap) {
            closestGap = gap;
            closestColor = faceColor;
        }

        if (gap <= tolerance) {
            float3 faceRight;
            float3 faceUp;
            float3 faceFwd;
            Face3DJ(faceIndex, faceRight, faceUp, faceFwd);
            float weight = pow(saturate(dot(rayDir, faceFwd)), blendPower) + 1e-4;
            colorSum += faceColor * weight;
            weightSum += weight;
        }
    }

    float3 result = closestColor;

    if (weightSum > 0) {
        result = colorSum / weightSum;
    }

    return result;
}

#endif
