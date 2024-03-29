﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.RealSense.Windows
{
    using System;
    using System.Threading;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Component that captures and streams video and depth from an Intel RealSense camera.
    /// </summary>
    public class RealSenseSensor : ISourceComponent, IDisposable
    {
        private bool shutdown;
        private Pipeline pipeline;
        private RealSenseDevice device;
        private Thread thread;

        /// <summary>
        /// Initializes a new instance of the <see cref="RealSenseSensor"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline this component is a part of.</param>
        public RealSenseSensor(Pipeline pipeline)
        {
            this.shutdown = false;
            this.ColorImage = pipeline.CreateEmitter<Shared<Image>>(this, "ColorImage");
            this.DepthImage = pipeline.CreateEmitter<Shared<Image>>(this, "DepthImage");
            this.pipeline = pipeline;
        }

        /// <summary>
        /// Gets the emitter that generates RGB images from the RealSense depth camera.
        /// </summary>
        public Emitter<Shared<Image>> ColorImage { get; private set; }

        /// <summary>
        /// Gets the emitter that generates Depth images from the RealSense depth camera.
        /// </summary>
        public Emitter<Shared<Image>> DepthImage { get; private set; }

        /// <summary>
        /// Dispose method.
        /// </summary>
        public void Dispose()
        {
            if (this.device != null)
            {
                this.device.Dispose();
                this.device = null;
            }
        }

        /// <inheritdoc/>
        public void Start(Action<DateTime> notifyCompletionTime)
        {
            // notify that this is an infinite source component
            notifyCompletionTime(DateTime.MaxValue);

            this.device = new RealSenseDevice();
            this.thread = new Thread(new ThreadStart(this.ThreadProc));
            this.thread.Start();
        }

        /// <inheritdoc/>
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            if (this.thread != null)
            {
                this.shutdown = true;
                TimeSpan waitTime = new TimeSpan(0, 0, 1);
                if (this.thread.Join(waitTime) != true)
                {
                    this.thread.Abort();
                }
            }

            if (this.device != null)
            {
                this.device = null;
            }

            notifyCompleted();
        }

        private void ThreadProc()
        {
            Imaging.PixelFormat pixelFormat = Imaging.PixelFormat.BGR_24bpp;
            switch (this.device.GetColorBpp())
            {
                case 24:
                    pixelFormat = Imaging.PixelFormat.BGR_24bpp;
                    break;
                case 32:
                    pixelFormat = Imaging.PixelFormat.BGRX_32bpp;
                    break;
                default:
                    throw new NotSupportedException("Expected 24bpp or 32bpp image.");
            }

            var colorImage = ImagePool.GetOrCreate((int)this.device.GetColorWidth(), (int)this.device.GetColorHeight(), pixelFormat);
            uint colorImageSize = this.device.GetColorHeight() * this.device.GetColorStride();
            switch (this.device.GetDepthBpp())
            {
                case 16:
                    pixelFormat = Imaging.PixelFormat.Gray_16bpp;
                    break;
                case 8:
                    pixelFormat = Imaging.PixelFormat.Gray_8bpp;
                    break;
                default:
                    throw new NotSupportedException("Expected 8bpp or 16bpp image.");
            }

            var depthImage = ImagePool.GetOrCreate((int)this.device.GetDepthWidth(), (int)this.device.GetDepthHeight(), pixelFormat);
            uint depthImageSize = this.device.GetDepthHeight() * this.device.GetDepthStride();
            while (!this.shutdown)
            {
                this.device.ReadFrame(colorImage.Resource.ImageData, colorImageSize, depthImage.Resource.ImageData, depthImageSize);
                DateTime t = DateTime.Now;
                this.ColorImage.Post(colorImage, t);
                this.DepthImage.Post(depthImage, t);
            }
        }
    }
}
