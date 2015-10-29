using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cudafy;
using Cudafy.Host;
using Cudafy.Translator;

namespace SwarmVision.Hardware
{
    public class GPU
    {
        private static GPGPU _gpu;
        private static bool _useGPU = true;

        /// <summary>
        /// TODO: MAKE THIS BASED ON HARDWARE DIAGNOSTICS
        /// </summary>
        public static bool UseGPU
        {
            get { return _useGPU; }
        }

        public static GPGPU Current
        {
            get
            {
                if (_gpu == null)
                {
                    CudafyModes.Target = eGPUType.OpenCL;
                    CudafyModes.DeviceId = 1;

                    CudafyTranslator.Language = CudafyModes.Target == eGPUType.OpenCL
                                                    ? eLanguage.OpenCL
                                                    : eLanguage.Cuda;

                    _gpu = CudafyHost.GetDevice(CudafyModes.Target);


                    var km = CudafyModule.TryDeserialize("Kernels");

                    if (km == null || !km.TryVerifyChecksums())
                    {
                        km = CudafyTranslator.Cudafy(typeof(Kernels));
                        km.Serialize("Kernels");
                    }

                    _gpu.LoadModule(km);
                }

                return _gpu;
            }
        }
    }
}
