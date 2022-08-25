/* 
*   NatML ARFoundation
*   Copyright (c) 2022 NatML Inc. All rights reserved.
*/

namespace NatML.Features {

    using System;
    using UnityEngine;
    using UnityEngine.XR.ARSubsystems;
    using Unity.Collections.LowLevel.Unsafe;
    using NatML.Internal;
    using Format = UnityEngine.XR.ARSubsystems.XRCpuImage.Format;

    /// <summary>
    /// ML augmented reality image feature.
    /// This feature will perform any necessary conversions to a model's desired input feature type.
    /// </summary>
    public sealed class MLXRCpuImageFeature : MLImageFeature, IMLEdgeFeature {

        #region --Client API--
        /// <summary>
        /// Create an augmented reality image feature from an ARFoundation `XRCpuImage`.
        /// </summary>
        /// <param name="image">Augmented reality image.</param>
        /// <param name="world">Whether AR image is from world-facing camera.</param>
        /// <param name="orientation">Image orientation. If `Unknown`, this will default to the screen orientation.</param>
        public MLXRCpuImageFeature (
            XRCpuImage image,
            bool world = true,
            ScreenOrientation orientation = 0
        ) : base(Convert(image, world, orientation), GetWidth(image, orientation), GetHeight(image, orientation)) { }
        #endregion


        #region --Operations--

        private static unsafe byte[] Convert (XRCpuImage image, bool world, ScreenOrientation orientation) {
            if (!image.valid)
                throw new ArgumentException(@"AR image is invalid", nameof(image));
            if (image.format != Format.AndroidYuv420_888 && image.format != Format.IosYpCbCr420_8BiPlanarFullRange)
                throw new ArgumentException($"AR image has invalid format: {image.format}", nameof(image));
            var planes = stackalloc void*[image.planeCount];
            var rows = stackalloc int[image.planeCount];
            var pixels = stackalloc int[image.planeCount];
            var pixelBuffer = new byte[image.width * image.height * 4];
            for (var i = 0; i < image.planeCount; ++i) {
                var plane = image.GetPlane(i);
                planes[i] = plane.data.GetUnsafeReadOnlyPtr();
                rows[i] = plane.rowStride;
                pixels[i] = plane.pixelStride;
            }
            NatMLARFoundation.CreateImageFeatureData(
                planes,
                image.planeCount,
                image.width,
                image.height,
                rows,
                pixels,
                (int)(orientation != 0 ? orientation : Screen.orientation),
                world,
                pixelBuffer,
                out var width,
                out var height
            );
            return pixelBuffer;
        }

        private static int GetWidth (XRCpuImage image, ScreenOrientation orientation) => (orientation != 0 ? orientation : Screen.orientation) switch {
            ScreenOrientation.Portrait              => image.height,
            ScreenOrientation.PortraitUpsideDown    => image.height,
            _                                       => image.width
        };

        private static int GetHeight (XRCpuImage image, ScreenOrientation orientation) => (orientation != 0 ? orientation : Screen.orientation) switch {
            ScreenOrientation.Portrait              => image.width,
            ScreenOrientation.PortraitUpsideDown    => image.width,
            _                                       => image.height
        };
        #endregion
    }
}