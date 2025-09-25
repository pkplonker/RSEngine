using Silk.NET.OpenGL;
using StbImageSharp;
using System;
using System.Collections.Generic;
using System.IO;
using Engine.Logging;

namespace Editor
{
    public class IconLoader
    {
        private static GL gl;
        private static Dictionary<string, uint> textureCache = new ();

        public static void Init(GL glContext)
        {
            IconLoader.gl = glContext;
        }

        public static unsafe IntPtr LoadIcon(string? path)
        {
            ArgumentNullException.ThrowIfNull(gl);

            if (textureCache.TryGetValue(path, out uint cachedTextureHandle))
            {
                return new IntPtr(cachedTextureHandle);
            }

            uint textureHandle = gl.GenTexture();
            gl.BindTexture(TextureTarget.Texture2D, textureHandle);

            try
            {
                var img = ImageResult.FromMemory(File.ReadAllBytes(path), ColorComponents.RedGreenBlueAlpha);
                fixed (byte* ptr = img.Data)
                {
                    gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, (uint)img.Width,
                        (uint)img.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error loading texture: {ex}");
                return IntPtr.Zero;
            }

            SetTextureParameters();

            gl.GenerateMipmap(TextureTarget.Texture2D);

            textureCache[path] = textureHandle;

            return new IntPtr(textureHandle);
        }

        private static void SetTextureParameters()
        {
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.LinearMipmapLinear);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, 0);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, 8);
        }

        public static void DeleteTexture(string? path)
        {
            if (textureCache.TryGetValue(path, out uint textureHandle))
            {
                gl.DeleteTexture(textureHandle);
                textureCache.Remove(path);
            }
        }
    }
}