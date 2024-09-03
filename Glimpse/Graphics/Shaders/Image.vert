#version 330 core

layout (location = 0) in vec2 aPosition;
layout (location = 1) in vec2 aTexCoord;
layout (location = 2) in vec4 aTint;

out vec2 frag_TexCoord;
out vec4 frag_Tint;

uniform mat4 uTransform;

void main() {
    gl_Position = uTransform * vec4(aPosition, 0.0, 1.0);
    frag_TexCoord = aTexCoord;
    frag_Tint = aTint;
}