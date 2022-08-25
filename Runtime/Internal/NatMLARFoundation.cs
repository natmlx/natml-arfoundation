/* 
*   NatML ARFoundation
*   Copyright (c) 2022 NatML Inc. All rights reserved.
*/

namespace NatML.Internal {

    using System.Runtime.InteropServices;

    public static class NatMLARFoundation {

        public const string Assembly =
        #if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        @"__Internal";
        #else
        @"NatMLARFoundation";
        #endif


        #region --NMLFeature--
        [DllImport(Assembly, EntryPoint = @"NMLCreateARFoundationImageFeatureData")]
        public static unsafe extern void CreateImageFeatureData (
            void** planes,
            int planeCount,
            int width,
            int height,
            int* rows,
            int* pixels,
            int orientation,
            bool world,
            [Out] byte[] pixelBuffer,
            out int dstWidth,
            out int dstHeight
        );
        #endregion
    }
}