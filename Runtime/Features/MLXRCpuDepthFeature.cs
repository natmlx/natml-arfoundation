/*
*   NatML ARFoundation
*   Copyright © 2023 NatML Inc. All rights reserved.
*/

namespace NatML.Features {

    using System;
    using System.Runtime.CompilerServices;
    using UnityEngine;
    using UnityEngine.XR.ARSubsystems;
    using Types;
    using Unity.Collections.LowLevel.Unsafe;
    using static Unity.Mathematics.math;
    using Format = UnityEngine.XR.ARSubsystems.XRCpuImage.Format;

    /// <summary>
    /// ML augmented reality depth feature.
    /// This feature cannot be used directly for inference.
    /// Instead, it is used by predictors that require depth data for their computations.
    /// </summary>
    public sealed class MLXRCpuDepthFeature : MLDepthFeature {

        #region --Client API--
        /// <summary>
        /// Create an AR depth image feature.
        /// </summary>
        /// <param name="image">Augmented reality image.</param>
        /// <param name="camera">AR session camera.</param>
        /// <param name="orientation">Image orientation. If `Unknown`, this will default to the screen orientation.</param>
        public MLXRCpuDepthFeature (
            XRCpuImage image,
            Camera camera,
            ScreenOrientation orientation = 0
        ) : base(GetType(image, orientation)) {
            if (image.format != Format.DepthUint16 && image.format != Format.DepthFloat32)
                throw new ArgumentException($"AR depth image has invalid format: {image.format}", nameof(image));
            this.image = image;
            this.camera = camera;
            this.orientation = orientation;
            this.rotation = GetRotation(orientation);
        }

        /// <summary>
        /// Sample the depth feature at a given point.
        /// If the point is out of bounds, `-1` is returned.
        /// </summary>
        /// <param name="point">Point to sample in normalized viewport coordinates.</param>
        /// <returns>Depth in meters.</returns>
        public override float Sample (Vector2 point) {
            var s = float2(image.width, image.height); // use unoriented size
            var uv = float2(point.x, point.y);
            var t = Mathf.Deg2Rad * rotation;
            var T = mul(float2x2(cos(t), -sin(t), sin(t), cos(t)), float2x2(1f, 0f, 0f, -1f));
            var uv_r = mul(T, uv - 0.5f) + 0.5f;
            var xy = int2(uv_r * s);
            if (xy.x < 0 || xy.x >= image.width || xy.y < 0 || xy.y >= image.height)
                return -1;
            switch (image.format) {
                case Format.DepthFloat32:   return Sample<float>(xy.x, xy.y);
                case Format.DepthUint16:    return 0.001f * Sample<ushort>(xy.x, xy.y);
                default:                    throw new InvalidOperationException($"Cannot sample depth because image has invalid format: {image.format}");
            }
        }

        /// <summary>
        /// Project a 2D point into 3D space using depth.
        /// </summary>
        /// <param name="point">Point to transform in normalized viewport coordinates.</param>
        /// <returns>Projected point in 3D space.</param>
        public override Vector3 ViewportToWorldPoint (Vector2 point) { // CHECK // Camera aspect scaling
            var depth = Sample(point);
            var viewport = new Vector3(point.x, point.y, depth);
            var world = camera.ViewportToWorldPoint(viewport);
            return world;
        }
        #endregion


        #region --Operations--
        private readonly XRCpuImage image;
        private readonly Camera camera;
        private readonly ScreenOrientation orientation;
        private readonly float rotation;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe T Sample<T> (int x, int y) where T : unmanaged {
            var plane = image.GetPlane(0);
            var idx = y * plane.rowStride + x * plane.pixelStride;
            var data = (byte*)plane.data.GetUnsafeReadOnlyPtr();
            var sample = *(T*)&data[idx];
            return sample;
        }

        private static float GetRotation (ScreenOrientation orientation) => orientation switch {
            ScreenOrientation.LandscapeLeft         => 0f,
            ScreenOrientation.Portrait              => -90f,
            ScreenOrientation.LandscapeRight        => -180f,
            ScreenOrientation.PortraitUpsideDown    => -270f,
            _                                       => 0f,
        };

        private static MLImageType GetType (XRCpuImage image, ScreenOrientation orientation) {
            orientation = orientation != 0 ? orientation : Screen.orientation;
            var portrait = orientation == ScreenOrientation.Portrait || orientation == ScreenOrientation.PortraitUpsideDown;
            var width = portrait ? image.height : image.width;
            var height = portrait ? image.width : image.height;
            return new MLImageType(width, height, 1);
        }
        #endregion
    }
}