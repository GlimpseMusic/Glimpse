#version 330 core

in vec2 frag_TexCoord;
in vec4 frag_Color;

out vec4 out_Color;

uniform sampler2D uTexture;

void main() {
    out_Color = texture(uTexture, frag_TexCoord) * frag_Color;
}