#ifndef RIGHTWALK_PLAYER_IDENTITY_INCLUDED
#define RIGHTWALK_PLAYER_IDENTITY_INCLUDED
sampler2D _IdentityTexture;
float _IdentityEnabled;
float _SpawnProtectionAlpha;
fixed4 PlayerIdentitySample(fixed4 original,float2 uv)
{
    if(_IdentityEnabled>.5)
    {
        // RGB replacement only: original alpha/pose clipping stay authoritative.
        original.rgb=tex2D(_IdentityTexture,uv).rgb;
    }
    original.a*=_SpawnProtectionAlpha;
    return original;
}
#endif
