﻿using System;
using System.Diagnostics.CodeAnalysis;

namespace FoundaryMediaPlayer.Platforms.Windows.DirectX
{
    [Flags]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal enum CreateFlags 
    {
        D3DCREATE_FPU_PRESERVE = 0x00000002,
        D3DCREATE_MULTITHREADED = 0x00000004,
        D3DCREATE_PUREDEVICE = 0x00000010,
        D3DCREATE_SOFTWARE_VERTEXPROCESSING = 0x00000020,
        D3DCREATE_HARDWARE_VERTEXPROCESSING = 0x00000040,
        D3DCREATE_MIXED_VERTEXPROCESSING = 0x00000080,
        D3DCREATE_DISABLE_DRIVER_MANAGEMENT = 0x00000100,
        D3DCREATE_ADAPTERGROUP_DEVICE = 0x00000200,
        D3DCREATE_DISABLE_DRIVER_MANAGEMENT_EX = 0x00000400      
    }
}
