using Silk.NET.Shaderc;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Shared.Rendering.SilkVulkan
{
    internal static unsafe class SilkVulkanShaderCompiler
    {
        public static byte[] Compile(string source, ShaderKind shaderKind, string name)
        {
            if (string.IsNullOrWhiteSpace(source))
                throw new ArgumentException("必须提供着色器源代码。", nameof(source));

            Shaderc shaderc = Shaderc.GetApi();
            Compiler* compiler = shaderc.CompilerInitialize();
            if (compiler == null)
                throw new InvalidOperationException("无法初始化 Shaderc。");

            CompileOptions* options = shaderc.CompileOptionsInitialize();
            if (options == null)
            {
                shaderc.CompilerRelease(compiler);
                throw new InvalidOperationException("无法初始化 Shaderc 编译选项。");
            }

            CompilationResult* result = null;
            try
            {
                shaderc.CompileOptionsSetTargetEnv(options, TargetEnv.Vulkan, 0);
                shaderc.CompileOptionsSetOptimizationLevel(options, OptimizationLevel.Performance);

                byte[] sourceBytes = Encoding.UTF8.GetBytes(source);
                fixed (byte* sourcePtr = sourceBytes)
                {
                    result = shaderc.CompileIntoSpv(compiler, sourcePtr, (nuint)sourceBytes.Length, shaderKind, name, "main", options);
                }

                if (result == null)
                    throw new InvalidOperationException($"Shaderc 未返回 {name} 的编译结果。");

                CompilationStatus status = shaderc.ResultGetCompilationStatus(result);
                if (status != CompilationStatus.Success)
                    throw new InvalidOperationException($"{name} 着色器编译失败：{shaderc.ResultGetErrorMessageS(result)}");

                nuint length = shaderc.ResultGetLength(result);
                byte* bytes = shaderc.ResultGetBytes(result);
                byte[] spirv = new byte[(int)length];
                Marshal.Copy((IntPtr)bytes, spirv, 0, spirv.Length);
                return spirv;
            }
            finally
            {
                if (result != null)
                    shaderc.ResultRelease(result);

                shaderc.CompileOptionsRelease(options);
                shaderc.CompilerRelease(compiler);
                shaderc.Dispose();
            }
        }
    }
}
