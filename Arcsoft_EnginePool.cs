using System;
using System.Collections.Concurrent;

namespace MicroService_Face_3_0
{
    public class Arcsoft_EnginePool:IProcess<IntPtr>
    {
        public int EngineNums { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public bool IsImageMode { get; set; }
        public Func<string, string, bool, IntPtr> ProcessFunc { get; set; }
        private ConcurrentBag<IntPtr> objects;

        public Arcsoft_EnginePool(int engineNums, string key, string value, bool isImageMode, Func<string, string, bool, IntPtr> fun)
        {
            try
            {
                objects = new ConcurrentBag<IntPtr>();
                EngineNums = engineNums;
                Key = key;
                Value = value;
                IsImageMode = isImageMode;
                ProcessFunc = fun;
                int status = InitEnginePool();
                if(status!=0)
                {
                    throw new Exception("引擎池初始化失败！");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"ArcSoft_EnginePool-->ArcSoft_EnginePool exception as: {ex}");
            }
        }

        public int InitEnginePool()
        {
            try
            {
                for (int index = 0; index < EngineNums; index++)
                {
                    IntPtr enginePtr = ProcessFunc(Key, Value, IsImageMode);
                    PutObject(enginePtr);
                }
                return 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"InitEnginePool--> exception {ex}");
            }
        }

        public IntPtr GetObject()
        {
            IntPtr item;
            if (objects.TryTake(out item))
            {
                return item;
            }
            else
            {
                return IntPtr.Zero;
            }
        }

        public void PutObject(IntPtr item)
        {
            objects.Add(item);
        }
    }
}
