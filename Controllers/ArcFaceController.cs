using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using MicroService_Face_3_0.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace MicroService_Face_3_0.Controllers
{
    /// <summary>
    /// 虹软人脸SDK3.0版本应用。
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ArcFaceController : ControllerBase
    {
        private IConfiguration Configuration { get; }
        private IEnginePoor FaceProcess { get; }
        private float idMixLevel = 0.82f;
        private int maxProcessTime = 5;


        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="configuration">配置依赖注入。</param>
        /// <param name="process">引擎池依赖注入。</param>
        public ArcFaceController(IConfiguration configuration, IEnginePoor process)
        {
            Configuration = configuration;
            FaceProcess = process;
            float.TryParse(Configuration.GetSection("AppSettings:IDMixLevel").Value, out idMixLevel);
            int.TryParse(Configuration.GetSection("AppSettings:MaxProcessTime").Value, out maxProcessTime);
        }


        ///// <summary>
        ///// 检查SDK信息。
        ///// </summary>
        ///// <returns>SDK信息的详细信息。</returns>
        //[HttpGet]
        //[Route("LisenceCheck")]
        //public IActionResult LisenceCheck()
        //{
        //    IntPtr engine = FaceProcess.GetEngine(FaceProcess.FaceEnginePoor);
        //    IntPtr pASFActiveFileInfo = Marshal.AllocHGlobal(Marshal.SizeOf<ASF_ActiveFileInfo>());
        //    try
        //    {
        //        int status = Arcsoft_Face_3_0.ASFGetActiveFileInfo(pASFActiveFileInfo);
        //        if (status == 0)
        //        {
        //            ASF_ActiveFileInfo activeFileInfo = Marshal.PtrToStructure<ASF_ActiveFileInfo>(pASFActiveFileInfo);
        //            DateTime startTime = TimeHelper.ConvertIntDatetime(int.Parse(Marshal.PtrToStringAnsi(activeFileInfo.startTime)));
        //            DateTime endTime = TimeHelper.ConvertIntDatetime(int.Parse(Marshal.PtrToStringAnsi(activeFileInfo.endTime)));
        //            string platform = Marshal.PtrToStringAnsi(activeFileInfo.platform);
        //            string sdkType = Marshal.PtrToStringAnsi(activeFileInfo.sdkType);
        //            string appId = Marshal.PtrToStringAnsi(activeFileInfo.appId);
        //            string sdkKey = Marshal.PtrToStringAnsi(activeFileInfo.sdkKey);
        //            string sdkVersion = Marshal.PtrToStringAnsi(activeFileInfo.sdkVersion);
        //            string fileVersion = Marshal.PtrToStringAnsi(activeFileInfo.fileVersion);

        //            Dictionary<string, string> fileInfor = new Dictionary<string, string>();
        //            fileInfor.Add("startTime", startTime.ToString());
        //            fileInfor.Add("endTime", endTime.ToString());
        //            fileInfor.Add("platform", platform);
        //            fileInfor.Add("sdkType", sdkType);
        //            fileInfor.Add("appId", appId);
        //            fileInfor.Add("sdkKey", sdkKey);
        //            fileInfor.Add("sdkVersion", sdkVersion);
        //            fileInfor.Add("fileVersion", fileVersion);
        //            string jObject = JsonConvert.SerializeObject(fileInfor);
        //            return Ok(jObject);
        //        }
        //        else
        //        {
        //            throw new Exception("LisenceCheck failed!");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(ex);
        //    }
        //    finally
        //    {
        //        Console.WriteLine($"engine={engine}");
        //        FaceProcess.PutEngine(FaceProcess.FaceEnginePoor, engine);
        //        Marshal.FreeHGlobal(pASFActiveFileInfo);
        //    }
        //}


        /// <summary>
        /// 对比两张照片中人脸的相似度，每张照片最大10M。
        /// </summary>
        /// <param name="faceA">照片1。</param>
        /// <param name="faceB">照片2。</param>
        /// <returns>true 代表检测成功，false表示失败。msg 中有详细结果（true情况是相似度，false情况是错误信息）。</returns>
        [HttpPost]
        [Route("CompareTwoFaces")]
        [DisableRequestSizeLimit]
        public IActionResult CompareTwoFaces(IFormFile faceA, IFormFile faceB)
        {
            IntPtr engine = FaceProcess.GetEngine(FaceProcess.FaceEnginePoor);
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CustomResult faceResult = new CustomResult();
            Tuple<bool, IntPtr, string> faceAResult = new Tuple<bool, IntPtr, string>(false, IntPtr.Zero, null);
            Tuple<bool, IntPtr, string> faceBResult = new Tuple<bool, IntPtr, string>(false, IntPtr.Zero, null);

            //调用引擎池逻辑！
            var task = Task.Run(() =>
            {
                while (engine == IntPtr.Zero)
                {
                    Task.Delay(10);
                    if (tokenSource.Token.IsCancellationRequested)
                    {
                        throw new Exception("等待引擎超时！");
                    }
                    engine = FaceProcess.GetEngine(FaceProcess.FaceEnginePoor);
                }
                using (var ms = new MemoryStream())
                {
                    faceA.CopyTo(ms);
                    faceAResult = Arcsoft_Face_Action.TryExtractSingleFaceFeature(ms, 10, engine);
                    if (!faceAResult.Item1)
                    {
                        faceResult.Success = false;
                        faceResult.msg = faceAResult.Item3;
                        return;
                    }
                }
                using (var ms = new MemoryStream())
                {
                    faceB.CopyTo(ms);
                    faceBResult = Arcsoft_Face_Action.TryExtractSingleFaceFeature(ms, 10, engine);
                    if (!faceBResult.Item1)
                    {
                        faceResult.Success = false;
                        faceResult.msg = faceBResult.Item3;
                        return;
                    }
                }
                float result = 0;
                int compareStatus = Arcsoft_Face_3_0.ASFFaceFeatureCompare(engine, faceAResult.Item2, faceBResult.Item2, ref result, ASF_CompareModel.ASF_LIFE_PHOTO);
                if (compareStatus == 0)
                {
                    faceResult.Success = true;
                    faceResult.msg = $"相似度: {result} 接客引擎：{engine}";
                }
                else
                {
                    faceResult.Success = false;
                    faceResult.msg = $"compareStatus error code = {compareStatus} 接客引擎：{engine}";
                }
            }, tokenSource.Token);

            //响应时间控制
            try
            {
                int timeLast = maxProcessTime * 1000;
                while (timeLast > 0)
                {
                    Task.Delay(100).Wait();
                    timeLast = timeLast - 100;
                    if (task.IsCompletedSuccessfully)
                    {
                        return Ok(JsonConvert.SerializeObject(faceResult));
                    }
                }
                tokenSource.Cancel();
                return Ok(JsonConvert.SerializeObject(faceResult));
            }
            catch (Exception ex)
            {
                faceResult.Success = false;
                faceResult.msg = ex.Message;
                return Ok(JsonConvert.SerializeObject(faceResult));
            }
            finally
            {
                FaceProcess.PutEngine(FaceProcess.FaceEnginePoor, engine);
                Marshal.FreeHGlobal(faceAResult.Item2);
                Marshal.FreeHGlobal(faceBResult.Item2);
                tokenSource.Dispose();
                GC.Collect();
            }
        }


        /// <summary>
        /// 对比人脸和证件照片的相似的，用于1:1对比，每张照片最大10M。
        /// </summary>
        /// <param name="facePhoto">人脸照片。</param>
        /// <param name="idPhoto">证件照片。</param>
        /// <returns>true 代表检测成功，false表示失败。msg 中有详细结果（true情况是相似度，false情况是错误信息）。</returns>
        [HttpPost]
        [Route("CheckID")]
        [DisableRequestSizeLimit]
        public IActionResult CheckID(IFormFile facePhoto, IFormFile idPhoto)
        {
            CustomResult checkResult = new CustomResult(); 
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            IntPtr engine = FaceProcess.GetEngine(FaceProcess.IDEnginePoor);
            Tuple<bool, IntPtr, string> faceResult = new Tuple<bool, IntPtr, string>(false, IntPtr.Zero, null);
            Tuple<bool, IntPtr, string> idResult = new Tuple<bool, IntPtr, string>(false, IntPtr.Zero, null);

            var task = Task.Run(() =>
            {
                while (engine == IntPtr.Zero)
                {
                    Task.Delay(10);
                    if (tokenSource.Token.IsCancellationRequested)
                    {
                        throw new Exception("等待引擎超时！");
                    }
                    engine = FaceProcess.GetEngine(FaceProcess.IDEnginePoor);
                }
                using (var ms = new MemoryStream())
                {
                    facePhoto.CopyTo(ms);
                    faceResult = Arcsoft_Face_Action.TryExtractSingleFaceFeature(ms, 10, engine);
                    if (!faceResult.Item1)
                    {
                        checkResult.Success = false;
                        checkResult.msg = faceResult.Item3;
                        return;
                    }
                }
                using (var ms = new MemoryStream())
                {
                    idPhoto.CopyTo(ms);
                    idResult = Arcsoft_Face_Action.TryExtractSingleFaceFeature(ms, 10, engine);
                    if (!idResult.Item1)
                    {
                        checkResult.Success = false;
                        checkResult.msg = idResult.Item3;
                        return;
                    }
                }
                float confidence = 0.0f;
                int status = Arcsoft_Face_3_0.ASFFaceFeatureCompare(engine, faceResult.Item2, idResult.Item2, ref confidence, ASF_CompareModel.ASF_ID_PHOTO);          
                if (confidence > idMixLevel)
                {
                    checkResult.Success = true;
                    checkResult.msg = $"相似度：{confidence} 接客引擎：{engine}";
                }
                else
                {
                    checkResult.Success = false;
                    checkResult.msg = $"相似度不足！接客引擎：{engine}";
                }
            }, tokenSource.Token);
            //Monitor
            try
            {
                int timeLast = maxProcessTime * 1000;
                while (timeLast > 0)
                {
                    Task.Delay(100).Wait();
                    timeLast = timeLast - 100;
                    if (task.IsCompletedSuccessfully)
                    {
                        return Ok(JsonConvert.SerializeObject(checkResult));
                    }
                }
                tokenSource.Cancel();
                return Ok(JsonConvert.SerializeObject(faceResult));
            }
            catch (Exception ex)
            {
                checkResult.Success = false;
                checkResult.msg = ex.Message;
                return Ok(JsonConvert.SerializeObject(checkResult));
            }
            finally
            {
                FaceProcess.PutEngine(FaceProcess.IDEnginePoor, engine);
                Marshal.FreeHGlobal(faceResult.Item2);
                Marshal.FreeHGlobal(idResult.Item2);
                tokenSource.Dispose();
                GC.Collect();
            }
        }


        /// <summary>
        /// RGB活体检测，用于检测图片中人脸时候是首次成像（非翻拍），每张照片最大10M。
        /// </summary>
        /// <param name="facePhoto">人脸照片</param>
        /// <returns> true 代表检测成功，false表示失败。msg 中有详细结果（true情况是活体检测结果，false情况是错误信息）。</returns>
        [HttpPost]
        [Route("CheckAliveFace")]
        [DisableRequestSizeLimit]      
        public IActionResult CheckAliveFace(IFormFile facePhoto)
        {
            CustomResult checkResult = new CustomResult();
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            IntPtr engine = FaceProcess.GetEngine(FaceProcess.FaceEnginePoor);

            //活体检验核心流程。
            var task = Task.Run(() =>
            {
                while (engine == IntPtr.Zero)
                {
                    Task.Delay(10);
                    if (tokenSource.Token.IsCancellationRequested)
                    {
                        throw new Exception("等待引擎超时！");
                    }
                    engine = FaceProcess.GetEngine(FaceProcess.FaceEnginePoor);
                }
                using (var ms = new MemoryStream())
                {
                    facePhoto.CopyTo(ms);
                    Tuple<bool,string> aliveResult = Arcsoft_Face_Action.IsAliveFace(ms, 10, engine);
                    checkResult.Success = aliveResult.Item1;
                    checkResult.msg = $"活体检验结果：{aliveResult.Item2}  接客引擎：{engine}";
                    return;
                }
            }, tokenSource.Token);
            //监控流程。
            try
            {
                int timeLast = maxProcessTime * 1000;
                while (timeLast > 0)
                {
                    Task.Delay(100).Wait();
                    timeLast = timeLast - 100;
                    if (task.IsCompletedSuccessfully)
                    {
                        return Ok(JsonConvert.SerializeObject(checkResult));
                    }
                }
                tokenSource.Cancel();
                return Ok(JsonConvert.SerializeObject(checkResult));
            }
            catch (Exception ex)
            {
                checkResult.Success = false;
                checkResult.msg = ex.Message;
                return Ok(JsonConvert.SerializeObject(checkResult));
            }
            finally
            {
                FaceProcess.PutEngine(FaceProcess.FaceEnginePoor, engine);
                tokenSource.Dispose();
                GC.Collect();
            }
        }


        /// <summary>
        /// 检测人脸的3D角度(包括横滚角,偏航角,俯仰角),每张照片最大10M。
        /// </summary>
        /// <param name="facePhoto">人脸照片</param>
        /// <returns>true 代表检测成功，false表示失败。msg 中有详细结果（true情况是3D角度检测结果，false情况是错误信息）。</returns>
        [HttpPost]
        [Route("CheckFace3DAngle")]
        [DisableRequestSizeLimit]
        public IActionResult CheckFace3DAngle(IFormFile facePhoto)
        {
            CustomResult checkResult = new CustomResult();
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            IntPtr engine = FaceProcess.GetEngine(FaceProcess.FaceEnginePoor);
            //3D角度检测核心流程。
            var task = Task.Run(() =>
            {
                while (engine == IntPtr.Zero)
                {
                    Task.Delay(10);
                    if (tokenSource.Token.IsCancellationRequested)
                    {
                        throw new Exception("等待引擎超时！");
                    }
                    engine = FaceProcess.GetEngine(FaceProcess.FaceEnginePoor);
                }
                using (var ms = new MemoryStream())
                {
                    facePhoto.CopyTo(ms);
                    Tuple<bool, string> angleResult = Arcsoft_Face_Action.GetFace3DAngle(ms, 10, engine);
                    checkResult.Success = angleResult.Item1;
                    checkResult.msg = angleResult.Item2;
                    return;
                }
            }, tokenSource.Token);
            //监控流程。
            try
            {
                int timeLast = maxProcessTime * 1000;
                while (timeLast > 0)
                {
                    Task.Delay(100).Wait();
                    timeLast = timeLast - 100;
                    if (task.IsCompletedSuccessfully)
                    {
                        return Ok(JsonConvert.SerializeObject(checkResult));
                    }
                }
                tokenSource.Cancel();
                return Ok(JsonConvert.SerializeObject(checkResult));
            }
            catch (Exception ex)
            {
                checkResult.Success = false;
                checkResult.msg = ex.Message;
                return Ok(JsonConvert.SerializeObject(checkResult));
            }
            finally
            {
                FaceProcess.PutEngine(FaceProcess.FaceEnginePoor, engine);
                tokenSource.Dispose();
                GC.Collect();
            }
        }


        /// <summary>
        /// AI算命（包括性别，年龄),每张照片最大10M。
        /// </summary>
        /// <returns>true 代表检测成功，false表示失败。msg 中有详细结果（true情况是算命结果，false情况是错误信息）。</returns>
        [HttpPost]
        [Route("AIFortuneTelling")]
        [DisableRequestSizeLimit]
        public IActionResult AIFortuneTelling(IFormFile facePhoto)

        {
            CustomResult checkResult = new CustomResult();
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            IntPtr engine = FaceProcess.GetEngine(FaceProcess.AIEnginePoor);
            //AI算命核心流程。
            var task = Task.Run(() =>
            {
                while (engine == IntPtr.Zero)
                {
                    Task.Delay(10);
                    if (tokenSource.Token.IsCancellationRequested)
                    {
                        throw new Exception("等待引擎超时！");
                    }
                    engine = FaceProcess.GetEngine(FaceProcess.AIEnginePoor);
                }
                using (var ms = new MemoryStream())
                {
                    facePhoto.CopyTo(ms);
                    Tuple<bool, string> angleResult = Arcsoft_Face_Action.AIFortuneTelling(ms, 10, engine);
                    checkResult.Success = angleResult.Item1;
                    checkResult.msg = angleResult.Item2;
                    return;
                }
            }, tokenSource.Token);
            //监控流程。
            try
            {
                int timeLast = maxProcessTime * 1000;
                while (timeLast > 0)
                {
                    Task.Delay(100).Wait();
                    timeLast = timeLast - 100;
                    if (task.IsCompletedSuccessfully)
                    {
                        return Ok(JsonConvert.SerializeObject(checkResult));
                    }
                }
                tokenSource.Cancel();
                return Ok(JsonConvert.SerializeObject(checkResult));
            }
            catch (Exception ex)
            {
                checkResult.Success = false;
                checkResult.msg = ex.Message;
                return Ok(JsonConvert.SerializeObject(checkResult));
            }
            finally
            {
                FaceProcess.PutEngine(FaceProcess.AIEnginePoor, engine);
                tokenSource.Dispose();
                GC.Collect();
            }
        }
    }
}