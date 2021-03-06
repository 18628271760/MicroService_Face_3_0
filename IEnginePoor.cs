﻿using System;
using System.Collections.Concurrent;

namespace MicroService_Face_3_0
{
    public interface IEnginePoor
    {
        public ConcurrentQueue<IntPtr> FaceEnginePoor { get; set; }
        public ConcurrentQueue<IntPtr> IDEnginePoor { get; set; }
        public ConcurrentQueue<IntPtr> AIEnginePoor { get; set; }
        public IntPtr GetEngine(ConcurrentQueue<IntPtr> queue);
        public void PutEngine(ConcurrentQueue<IntPtr> queue, IntPtr item);
    }
}
