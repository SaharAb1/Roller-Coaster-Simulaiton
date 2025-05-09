using System;
using System.IO;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using StbImageSharp;

namespace RollerCoasterSim
{
    public class UICursor
    {
        
        private int _textureId;
        private int _vao;
        private int _vbo;
        private Shader _shader;
        private int _width;
        private int _height;

        public UICursor(string imagePath)
        {
            LoadTexture(imagePath);
            SetupBuffers();
            _shader = new Shader("Shaders/ui.vert", "Shaders/ui.frag");
        }

        private void LoadTexture(string path)
        {
            using (var stream = File.OpenRead(path))
            {
                var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
                _width = image.Width;
                _height = image.Height;

                _textureId = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, _textureId);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, _width, _height, 0,
                    PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.BindTexture(TextureTarget.Texture2D, 0);
            }
        }

        private void SetupBuffers()
        {
            float[] vertices = {
                // Positions   // Texture Coords
                0f, 0f,        0f, 0f,
                1f, 0f,        1f, 0f,
                1f, 1f,        1f, 1f,
                0f, 1f,        0f, 1f
            };

            uint[] indices = {
                0, 1, 2,
                2, 3, 0
            };

            _vao = GL.GenVertexArray();
            _vbo = GL.GenBuffer();
            int ebo = GL.GenBuffer();

            GL.BindVertexArray(_vao);

            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

            int stride = 4 * sizeof(float);

            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, 0);
            GL.EnableVertexAttribArray(0);

            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 2 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            GL.BindVertexArray(0);
        }

        public void Draw(Vector2 position, Vector2 windowSize)
        {
            _shader.Use();

           float targetSize = 32f;

            Matrix4 model =
                Matrix4.CreateScale(targetSize, targetSize, 1.0f) *
                Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(180f)) * // 👈 flip 180°
                Matrix4.CreateTranslation(position.X, windowSize.Y - position.Y - targetSize, 0.0f);



            Matrix4 projection = Matrix4.CreateOrthographicOffCenter(0, windowSize.X, 0, windowSize.Y, -1.0f, 1.0f);

            _shader.SetMatrix4("model", model);
            _shader.SetMatrix4("projection", projection);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _textureId);
            _shader.SetInt("tex", 0);

            GL.BindVertexArray(_vao);
            GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
        }
    }
}
