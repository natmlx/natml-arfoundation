/* 
*   NatML ARFoundation
*   Copyright © 2023 NatML Inc. All rights reserved.
*/

namespace NatML.Features {

    using System;
    using UnityEngine;
    using UnityEngine.XR.ARSubsystems;
    using Unity.Collections.LowLevel.Unsafe;
    using Internal;
    using Types;
    using Format = UnityEngine.XR.ARSubsystems.XRCpuImage.Format;

    /// <summary>
    /// </summary>
    public static class MLXRExtensions {

        #region --Client API--
        /// <summary>
        /// Copy image data from an ARFoundation image.
        /// </summary>
        /// <param name="feature">Image feature to copy data into.</param>
        /// <param name="image">AR image.</param>
        /// <param name="world">Whether AR image is from world-facing camera.</param>
        /// <param name="orientation">Image orientation. If `Unknown`, this will default to the screen orientation.</param>
        public static unsafe void CopyFrom (this MLImageFeature feature, XRCpuImage image, bool world = true, ScreenOrientation orientation = 0) {
            // Check size // This implicitly checks whether the image is valid.
            var (width, height) = image.GetFeatureSize(orientation);
            if (feature.width != width || feature.height != height)
                throw new ArgumentException($"Feature has incorrect size", nameof(image));
            // Check format
            if (image.format != Format.AndroidYuv420_888 && image.format != Format.IosYpCbCr420_8BiPlanarFullRange)
                throw new ArgumentException($"AR image has invalid format: {image.format}", nameof(image));
            // Populate data
            var planes = stackalloc void*[image.planeCount];
            var rows = stackalloc int[image.planeCount];
            var pixels = stackalloc int[image.planeCount];
            for (var i = 0; i < image.planeCount; ++i) {
                var plane = image.GetPlane(i);
                planes[i] = plane.data.GetUnsafeReadOnlyPtr();
                rows[i] = plane.rowStride;
                pixels[i] = plane.pixelStride;
            }
            // Copy
            fixed (void* dst = feature)
                NatMLARFoundation.CreateImageFeatureData(
                    planes,
                    image.planeCount,
                    image.width,
                    image.height,
                    rows,
                    pixels,
                    (int)(orientation != 0 ? orientation : Screen.orientation),
                    world,
                    dst,
                    out var _,
                    out var _
                );
        }

        /// <summary>
        /// Get the ML feature type for a given AR image.
        /// </summary>
        /// <param name="image">AR image.</param>
        /// <param name="orientation">Image orientation. If `Unknown`, this will default to the screen orientation.</param>
        /// <returns>Feature type for image.</returns>
        public static MLImageType GetFeatureType (this XRCpuImage image, ScreenOrientation orientation = 0) {
            // Check valid
            if (!image.valid)
                throw new ArgumentException(@"AR image is invalid", nameof(image));
            // Create type
            var (width, height) = image.GetFeatureSize(orientation);
            var type = image.GetFeatureType();
            var result = new MLImageType(width, height, image.planeCount, type);
            // Return
            return result;
        }
        #endregion


        #region --Operations--
        /// <summary>
        /// Get the image feature size for a given AR image.
        /// Note that the image MUST be valid.
        /// </summary>
        /// <param name="image">AR image.</param>
        /// <param name="orientation">Image orientation. If `Unknown`, this will default to the screen orientation.</param>
        /// <returns>Image feature size.</returns>
        private static (int width, int height) GetFeatureSize (this XRCpuImage image, ScreenOrientation orientation = 0) => (orientation != 0 ? orientation : Screen.orientation) switch {
            ScreenOrientation.Portrait              => (image.height, image.width),
            ScreenOrientation.PortraitUpsideDown    => (image.height, image.width),
            _                                       => (image.width, image.height),
        };

        /// <summary>
        /// Get the image feature data type for a given AR image.
        /// Note that the image MUST be valid.
        /// </summary>
        /// <param name="image">AR image.</param>
        /// <returns>Image feature data type.</returns>
        private static Type GetFeatureType (this XRCpuImage image) => image.format switch {
            Format.AndroidYuv420_888                => typeof(byte),
            Format.DepthFloat32                     => typeof(float),
            Format.DepthUint16                      => typeof(ushort),
            Format.IosYpCbCr420_8BiPlanarFullRange  => typeof(byte),
            Format.OneComponent8                    => typeof(byte),
            _                                       => null,
        };
        #endregion
    }
}