using OpenTK.Graphics.OpenGL4;
using System;
using System.IO;

namespace RollerCoasterSim
{
    public class Shader
    {
        public int Handle;

        public Shader(string vertexPath, string fragmentPath)
        {
            string vertexCode = File.ReadAllText(vertexPath);
            string fragmentCode = File.ReadAllText(fragmentPath);

            int vertex = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertex, vertexCode);
            GL.CompileShader(vertex);
            GL.GetShader(vertex, ShaderParameter.CompileStatus, out int vStatus);
            if (vStatus != (int)All.True)
                throw new Exception(GL.GetShaderInfoLog(vertex));

            int fragment = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragment, fragmentCode);
            GL.CompileShader(fragment);
            GL.GetShader(fragment, ShaderParameter.CompileStatus, out int fStatus);
            if (fStatus != (int)All.True)
                throw new Exception(GL.GetShaderInfoLog(fragment));

            Handle = GL.CreateProgram();
            GL.AttachShader(Handle, vertex);
            GL.AttachShader(Handle, fragment);
            GL.LinkProgram(Handle);

            GL.DetachShader(Handle, vertex);
            GL.DetachShader(Handle, fragment);
            GL.DeleteShader(vertex);
            GL.DeleteShader(fragment);
        }

        public void Use() => GL.UseProgram(Handle);
        public void SetMatrix4(string name, OpenTK.Mathematics.Matrix4 matrix)
            => GL.UniformMatrix4(GL.GetUniformLocation(Handle, name), false, ref matrix);
    }
}
