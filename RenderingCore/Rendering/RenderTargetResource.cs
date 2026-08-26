using System;

namespace Shared.Rendering
{
    public readonly struct RenderTargetResource
    {
        private RenderTargetResource(RenderTexture texture, RenderSurface surface)
        {
            Texture = texture;
            Surface = surface;
        }

        public RenderTexture Texture { get; }

        public RenderSurface Surface { get; }

        public bool IsValid => Texture.IsValid && Surface.IsValid;

        public static RenderTargetResource From(RenderTexture texture, RenderSurface surface)
        {
            if (!texture.IsValid)
                throw new ArgumentException("需要有效的纹理句柄。", nameof(texture));

            if (!surface.IsValid)
                throw new ArgumentException("需要有效的表面句柄。", nameof(surface));

            return new RenderTargetResource(texture, surface);
        }
    }
}
