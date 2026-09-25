using System;

namespace PdfSharpDslCore.Drawing
{
    [Flags]
    public enum DebugOptions
    {
        //8, 16, 32, 64, 128,256, 512,1024, 2048, 4096, 8192
        None = 0,
        DebugText = 1,
        DebugRect = 2,
        DebugRowTemplate = 4,
        DebugImage = 8,
        /// <summary>
        /// show rule on each newpage
        /// </summary>
        DebugRule = 16,
        /// <summary>
        /// show a light grid (every 50 points, labelled with its coordinates) on each page
        /// </summary>
        DebugGrid = 32,
        DebugAll = 8192,
    }
}