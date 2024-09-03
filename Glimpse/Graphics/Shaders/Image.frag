#version 330 core

in vec2 frag_TexCoord;
in vec4 frag_Tint;

out vec4 out_Color;

uniform sampler2D uTexture;
uniform vec4 uTint;

void main() {
    out_Color = texture(uTexture, frag_TexCoord) * frag_Tint * uTint;
}